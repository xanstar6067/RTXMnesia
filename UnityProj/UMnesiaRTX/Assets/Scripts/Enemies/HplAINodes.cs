using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Enemies
{
    /// <summary>Flags of cAINodeContainer::FreePath (tAIFreePathFlag).</summary>
    [Flags]
    public enum AIFreePathFlag
    {
        None = 0,
        SkipStatic = 0x1,
        SkipDynamic = 0x2,
        SkipVolatile = 0x4,
    }

    /// <summary>cAINodeEdge.</summary>
    public struct AINodeEdge
    {
        public float distance, sqrDistance;
        public AINode node;
    }

    /// <summary>
    /// cAINode: a path node (PathNode area). The position is in HPL2 space (x mirrored against Unity), so the grid,
    /// the iteration order and every distance of the container are those of the original.
    /// </summary>
    public sealed class AINode
    {
        public string Name { get; internal set; }
        public int Id { get; internal set; }
        /// <summary>HPL2 space.</summary>
        public Vector3 HplPosition { get; internal set; }
        /// <summary>Unity space.</summary>
        public Vector3 Position => HplSpace.Position(HplPosition);
        /// <summary>Creation order (std::set&lt;cAINode*&gt; orders by address; nodes are allocated in this order).</summary>
        internal int Index;
        internal readonly List<AINodeEdge> Edges = new();

        public int EdgeCount => Edges.Count;
        public AINodeEdge GetEdge(int i) => Edges[i];

        internal void AddEdge(AINode node)
        {
            Edges.Add(new AINodeEdge
            {
                node = node,
                distance = Vector3.Distance(HplPosition, node.HplPosition),
                sqrDistance = (HplPosition - node.HplPosition).sqrMagnitude,
            });
        }
    }

    /// <summary>
    /// cAINodeContainer (AINodeContainer.cpp): the nodes of a type with a collide size, a grid for lookups by
    /// position, free path tests with rays and the connections (loaded from the map's .nodes file or compiled).
    /// Positions passed in and out are Unity space unless named Hpl.
    /// </summary>
    public sealed class AINodeContainer
    {
        public string Name { get; }
        public string NodeName { get; }
        /// <summary>The collide size (x, y, z).</summary>
        public Vector3 CollideSize { get; }

        public int MaxEdges = 5, MinEdges = 2;
        public float MaxEdgeDistance = 3f, MaxHeight = 0.1f;
        public bool NodeIsAtCenter = true;
        const int NodesPerGrid = 6;

        readonly List<AINode> _nodes = new();
        readonly Dictionary<string, AINode> _byName = new(StringComparer.Ordinal);
        readonly Dictionary<int, AINode> _byId = new();

        Vector2Int _gridMapSize;
        Vector2 _gridSize, _minGridPos, _maxGridPos;
        List<AINode>[] _grids = Array.Empty<List<AINode>>();

        static readonly RaycastHit[] Hits = new RaycastHit[64];

        public AINodeContainer(string name, string nodeName, Vector3 collideSize)
        {
            Name = name;
            NodeName = nodeName;
            CollideSize = collideSize;
        }

        public int NodeCount => _nodes.Count;
        public AINode GetNode(int i) => _nodes[i];

        /// <summary>cAINodeContainer::AddNode, position in HPL2 space. Maps keep the first node of a name / id.</summary>
        public void AddNodeHpl(string name, int id, Vector3 hplPosition)
        {
            var node = new AINode { Name = name, Id = id, HplPosition = hplPosition, Index = _nodes.Count };
            _nodes.Add(node);
            _byName.TryAdd(name, node);
            _byId.TryAdd(id, node);
        }

        public AINode GetNodeFromName(string name) => name != null && _byName.TryGetValue(name, out var n) ? n : null;

        public AINode GetNodeFromId(int id) => _byId.TryGetValue(id, out var n) ? n : null;

        // ------------------------------------------------------------------ grid

        /// <summary>cAINodeContainer::BuildNodeGridMap.</summary>
        public void BuildNodeGridMap()
        {
            if (_nodes.Count == 0) return;
            Vector2 min = new(_nodes[0].HplPosition.x, _nodes[0].HplPosition.z), max = min;
            for (int i = 1; i < _nodes.Count; i++)
            {
                Vector3 p = _nodes[i].HplPosition;
                if (min.x > p.x) min.x = p.x;
                if (min.y > p.z) min.y = p.z;
                if (max.x < p.x) max.x = p.x;
                if (max.y < p.z) max.y = p.z;
            }
            _minGridPos = min;
            _maxGridPos = max;

            int gridNum = (int)(Mathf.Sqrt(_nodes.Count / (float)NodesPerGrid) + 0.5f) + 1;
            _gridMapSize = new Vector2Int(gridNum, gridNum);
            _grids = new List<AINode>[(gridNum + 1) * (gridNum + 1)];
            for (int i = 0; i < _grids.Length; i++) _grids[i] = new List<AINode>();

            _gridSize = _maxGridPos - _minGridPos;
            _gridSize.x /= _gridMapSize.x;
            _gridSize.y /= _gridMapSize.y;

            foreach (var node in _nodes)
            {
                Vector2 local = new Vector2(node.HplPosition.x, node.HplPosition.z) - _minGridPos;
                Vector2Int pos = Vector2Int.zero;
                if (_gridSize.x > 0f) pos.x = (int)(local.x / _gridSize.x);
                if (_gridSize.y > 0f) pos.y = (int)(local.y / _gridSize.y);
                _grids[pos.y * (_gridMapSize.x + 1) + pos.x].Add(node);
            }
        }

        // C++ int conversion of a float division (0/0 = NaN gives INT_MIN there, then clamped to 0).
        static int ToInt(float f) => float.IsNaN(f) ? int.MinValue : f >= int.MaxValue ? int.MaxValue : f <= int.MinValue ? int.MinValue : (int)f;

        Vector2Int GetGridPosFromLocal(Vector2 local)
        {
            var pos = new Vector2Int(ToInt(local.x / _gridSize.x), ToInt(local.y / _gridSize.y));
            if (pos.x < 0) pos.x = 0;
            if (pos.y < 0) pos.y = 0;
            if (pos.x > _gridMapSize.x) pos.x = _gridMapSize.x;
            if (pos.y > _gridMapSize.y) pos.y = _gridMapSize.y;
            return pos;
        }

        /// <summary>
        /// cAINodeIterator: the nodes of the grid cells around a position (the radius is not checked, some nodes lie
        /// outside it), cell by cell in x then z. HPL2 space.
        /// </summary>
        public IEnumerable<AINode> NodesNearHpl(Vector3 hplPosition, float radius)
        {
            if (_grids.Length == 0) yield break;
            Vector2 local = new Vector2(hplPosition.x, hplPosition.z) - _minGridPos;
            Vector2Int start = GetGridPosFromLocal(local - new Vector2(radius, radius));
            Vector2Int end = GetGridPosFromLocal(local + new Vector2(radius, radius));
            for (int y = start.y; y <= end.y; y++)
                for (int x = start.x; x <= end.x; x++)
                {
                    var list = _grids[y * (_gridMapSize.x + 1) + x];
                    for (int i = 0; i < list.Count; i++) yield return list[i];
                }
        }

        // ------------------------------------------------------------------ free path

        static readonly Vector2[] PosAdds = { new(0, 0), new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        /// <summary>
        /// cAINodeContainer::FreePath (HPL2 space): up to 5 rays (center, sides at 40% of the width, up and down at 40%
        /// of the height) between the collider centers; any body that the flags do not skip blocks the path.
        /// </summary>
        public bool FreePathHpl(Vector3 hplStart, Vector3 hplEnd, int rayNum, AIFreePathFlag flags)
        {
            if (rayNum < 0 || rayNum > 5) rayNum = 5;
            Vector3 forward = (hplEnd - hplStart).normalized;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(forward, up);
            Vector3 startCenter = NodeIsAtCenter ? hplStart : hplStart + new Vector3(0f, CollideSize.y / 2f, 0f);
            Vector3 endCenter = NodeIsAtCenter ? hplEnd : hplEnd + new Vector3(0f, CollideSize.y / 2f, 0f);
            float halfWidth = CollideSize.x * 0.4f;
            float halfHeight = CollideSize.y * 0.4f;

            for (int i = 0; i < rayNum; i++)
            {
                Vector3 add = right * (PosAdds[i].x * halfWidth) + up * (PosAdds[i].y * halfHeight);
                if (RayBlocked(HplSpace.Position(startCenter + add), HplSpace.Position(endCenter + add), flags)) return false;
            }
            return true;
        }

        /// <summary>cAINodeRayCallback: a ray (Unity space) hits a body that is not skipped.</summary>
        static bool RayBlocked(Vector3 start, Vector3 end, AIFreePathFlag flags)
        {
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 1e-6f) return false;
            int mask = Physics.DefaultRaycastLayers | (1 << HplPhysics.CharacterLayer);
            int count = Physics.RaycastNonAlloc(start, delta / length, Hits, length, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!SkipBody(Hits[i].collider, flags)) return true;
            return false;
        }

        /// <summary>cAINodeRayCallback::BeforeIntersect: true when the body does not count.</summary>
        static bool SkipBody(Collider c, AIFreePathFlag flags)
        {
            bool character = c.gameObject.layer == HplPhysics.CharacterLayer;
            var body = character ? null : HplPhysics.BodyOf(c);
            if (body != null && !body.collideCharacter) return true;
            float mass = body != null ? body.mass : 0f;
            if ((flags & AIFreePathFlag.SkipStatic) != 0 && mass == 0f && !character) return true;
            if ((flags & AIFreePathFlag.SkipDynamic) != 0 && (mass > 0f || character)) return true;
            if ((flags & AIFreePathFlag.SkipVolatile) != 0 && body != null && body.isVolatile) return true;
            return false;
        }

        // ------------------------------------------------------------------ connections

        /// <summary>
        /// cAINodeContainer::Compile: each node connects to the nodes of its grid neighborhood within 2 × max edge
        /// distance, at most MaxHeight apart and with a free path; the edges are sorted by distance, cut to MaxEdges and
        /// to MaxEdgeDistance once MinEdges are kept.
        /// </summary>
        public void Compile()
        {
            BuildNodeGridMap();
            foreach (var node in _nodes)
            {
                foreach (var end in NodesNearHpl(node.HplPosition, MaxEdgeDistance * 1.5f))
                {
                    if (end == node) continue;
                    float dist = Vector3.Distance(node.HplPosition, end.HplPosition);
                    if (dist > MaxEdgeDistance * 2f) continue;
                    float height = Mathf.Abs(node.HplPosition.y - end.HplPosition.y);
                    if (height <= MaxHeight && FreePathHpl(node.HplPosition, end.HplPosition, -1, AIFreePathFlag.SkipDynamic | AIFreePathFlag.SkipVolatile))
                        node.AddEdge(end);
                }
                // std::sort is not stable; equal distances keep their order here.
                var sorted = new List<AINodeEdge>(node.Edges);
                sorted.Sort((a, b) => a.distance.CompareTo(b.distance));
                node.Edges.Clear();
                node.Edges.AddRange(sorted);
                if (MaxEdges > 0 && node.Edges.Count > MaxEdges) node.Edges.RemoveRange(MaxEdges, node.Edges.Count - MaxEdges);
                for (int i = 0; i < node.Edges.Count; i++)
                {
                    if (node.Edges[i].distance > MaxEdgeDistance && i >= MinEdges)
                    {
                        node.Edges.RemoveRange(i, node.Edges.Count - i);
                        break;
                    }
                }
            }
        }

        /// <summary>cAINodeContainer::LoadFromFile: the grid, then the edges of each node by id, the edge nodes by name.</summary>
        public void LoadFromXml(string xml)
        {
            BuildNodeGridMap();
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            foreach (XmlNode n in doc.DocumentElement.ChildNodes)
            {
                if (n is not XmlElement nodeElem || nodeElem.Name != "Node") continue;
                var node = GetNodeFromId(HplMath.Atoi(nodeElem.GetAttribute("ID")));
                foreach (XmlNode e in nodeElem.ChildNodes)
                {
                    if (e is not XmlElement edgeElem || edgeElem.Name != "Edge") continue;
                    var edgeNode = GetNodeFromName(edgeElem.GetAttribute("Node"));
                    float distance = HplMath.Atof(edgeElem.GetAttribute("Distance"));
                    // A node missing in the map would crash HPL2; such files do not ship.
                    if (node == null || edgeNode == null) continue;
                    node.Edges.Add(new AINodeEdge { node = edgeNode, distance = distance, sqrDistance = distance * distance });
                }
            }
        }
    }

    /// <summary>
    /// cAStarHandler (AStar.cpp): a direct free path needs no nodes; otherwise A* from the nodes reachable from the
    /// start to any node reachable from the goal (within 2 × max edge distance and 1.5 × max height). Cost = distance ×
    /// (1 + height change of the edge), heuristic = distance to the goal. The open list is searched linearly and in
    /// node order on equal cost, as std::set&lt;cAStarNode*&gt; ordered by node address.
    /// </summary>
    public sealed class AStarHandler
    {
        sealed class AStarNode
        {
            public float cost, distance;
            public AStarNode parent;
            public AINode node;
        }

        readonly AINodeContainer _container;
        readonly SortedDictionary<int, AStarNode> _open = new();
        readonly HashSet<int> _closed = new();
        readonly HashSet<AINode> _goalNodes = new();
        Vector3 _goal;
        AStarNode _goalNode;
        public int MaxIterations = -1;

        public AStarHandler(AINodeContainer container) => _container = container;

        /// <summary>
        /// cAStarHandler::GetPath (HPL2 space). True when a path exists; the nodes go into the list from the goal
        /// end to the first node (the list is used from the back). A direct path leaves the list empty.
        /// </summary>
        public bool GetPathHpl(Vector3 start, Vector3 goal, List<AINode> nodeList)
        {
            float maxHeight = _container.MaxHeight * 1.5f;
            float height = Mathf.Abs(start.y - goal.y);
            if (height <= maxHeight && _container.FreePathHpl(start, goal, -1, AIFreePathFlag.SkipDynamic))
            {
                _goalNode = null;
                return true;
            }

            _open.Clear();
            _closed.Clear();
            _goalNodes.Clear();
            _goalNode = null;
            _goal = goal;

            float maxDist = _container.MaxEdgeDistance * 2f;
            foreach (var node in _container.NodesNearHpl(start, maxDist))
            {
                float h = Mathf.Abs(start.y - node.HplPosition.y);
                float dist = Vector3.Distance(start, node.HplPosition);
                if (dist < maxDist && h <= maxHeight && _container.FreePathHpl(start, node.HplPosition, -1, AIFreePathFlag.SkipDynamic))
                    AddOpenNode(node, null, dist);
            }
            foreach (var node in _container.NodesNearHpl(goal, maxDist))
            {
                float h = Mathf.Abs(goal.y - node.HplPosition.y);
                float dist = Vector3.Distance(goal, node.HplPosition);
                if (dist < maxDist && h <= maxHeight && _container.FreePathHpl(goal, node.HplPosition, -1, AIFreePathFlag.SkipDynamic))
                    _goalNodes.Add(node);
            }

            IterateAlgorithm();

            if (_goalNode == null) return false;
            if (nodeList != null)
                for (var n = _goalNode; n != null; n = n.parent) nodeList.Add(n.node);
            return true;
        }

        void IterateAlgorithm()
        {
            int iterations = 0;
            while (_open.Count > 0 && (MaxIterations < 0 || iterations < MaxIterations))
            {
                var best = GetBestNode();
                if (_goalNodes.Contains(best.node))
                {
                    _goalNode = best;
                    break;
                }
                for (int i = 0; i < best.node.EdgeCount; i++)
                {
                    var edge = best.node.GetEdge(i);
                    AddOpenNode(edge.node, best, best.distance + edge.distance);
                }
                iterations++;
            }
        }

        void AddOpenNode(AINode node, AStarNode parent, float distance)
        {
            if (_closed.Contains(node.Index) || _open.ContainsKey(node.Index)) return;
            var n = new AStarNode { node = node, parent = parent, distance = distance };
            n.cost = Cost(distance, node, parent) + Vector3.Distance(node.HplPosition, _goal);
            _open.Add(node.Index, n);
        }

        AStarNode GetBestNode()
        {
            AStarNode best = null;
            foreach (var n in _open.Values)
                if (best == null || best.cost > n.cost) best = n;
            _open.Remove(best.node.Index);
            _closed.Add(best.node.Index);
            return best;
        }

        static float Cost(float distance, AINode node, AStarNode parent)
        {
            if (parent == null) return distance;
            return distance * (1f + Mathf.Abs(node.HplPosition.y - parent.node.HplPosition.y));
        }
    }
}
