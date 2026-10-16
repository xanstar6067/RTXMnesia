using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Enemies
{
    /// <summary>
    /// cLuxEnemyPathfinder (LuxEnemyPathfinder.cpp): the A* path to a goal over the map's node container of the enemy's
    /// file, followed node by node from the back of the list (a node is reached when it is inside a box 1.3 × the body
    /// around it); stuck at a node (the distance grew on average over 150 steps while close) skips it; a mover stuck at
    /// its max ends the path with custom value 1. Node lookups by position with distance and free path tests.
    /// </summary>
    public sealed class EnemyPathfinder
    {
        readonly HplEnemy _enemy;
        readonly EnemyMover _mover;
        AStarHandler _aStar;
        public AINodeContainer NodeContainer { get; private set; }

        public bool IsMoving { get; private set; }
        Vector3 _moveGoalPos;
        /// <summary>The path, the next node at the back (tAINodeList used with back / pop_back).</summary>
        readonly List<AINode> _pathNodes = new();
        readonly List<float> _pathNodeDistances = new();

        public EnemyPathfinder(HplEnemy enemy, EnemyMover mover)
        {
            _enemy = enemy;
            _mover = mover;
        }

        public IReadOnlyList<AINode> PathNodes => _pathNodes;
        public int PathNodeCount => _pathNodes.Count;
        public Vector3 FinalGoalPos => _moveGoalPos;

        /// <summary>
        /// cLuxEnemyPathfinder::AfterWorldLoad: cWorld::CreateAINodeContainer(file name, "Default", body size, nodes at
        /// the feet, edges 2..6, max edge distance 5, max height 0.41).
        /// </summary>
        public void AfterWorldLoad()
        {
            string type = System.IO.Path.GetFileNameWithoutExtension(_enemy.FileName ?? "");
            NodeContainer = _enemy.Map.CreateAINodeContainer(type, "Default", _enemy.Body.Size, false, 2, 6, 5f, 0.41f);
            if (NodeContainer == null) HplLog.Error($"No node container found for enemy '{_enemy.Name}'");
            _aStar = NodeContainer != null ? new AStarHandler(NodeContainer) : null;
        }

        /// <summary>cLuxEnemyPathfinder::MoveTo: moving toward the goal; the A* path from the feet (+1 cm) when there are nodes.</summary>
        public bool MoveTo(Vector3 pos)
        {
            var body = _enemy.Body;
            _pathNodeDistances.Clear();
            _pathNodes.Clear();
            IsMoving = true;
            Vector3 start = body.Position;
            _moveGoalPos = pos;
            if (_aStar == null) return false;
            if (NodeContainer == null || !NodeContainer.NodeIsAtCenter) start -= new Vector3(0f, body.Size.y / 2f, 0f);
            start.y += 0.01f;
            return _aStar.GetPathHpl(HplSpace.Position(start), HplSpace.Position(_moveGoalPos), _pathNodes);
        }

        public void Stop()
        {
            IsMoving = false;
            _pathNodes.Clear();
            _pathNodeDistances.Clear();
        }

        /// <summary>
        /// cLuxEnemyPathfinder::GetNodeAtPos: among the nodes near the position within [min, max] distance (and not the
        /// goal of another enemy's path), the closest or a random one, optionally with a free path from the position and
        /// from the enemy. The original passes the ray count as the free path flags too: −1 (all rays) skips every body,
        /// so those tests always pass; 1 skips only static bodies.
        /// </summary>
        public AINode GetNodeAtPos(Vector3 pos, float minDistance, float maxDistance, bool getClosest, bool posToNodeFreePathCheck,
                                   bool enemyToNodeFreePathCheck, AINode skipNode, int freePathRayNum = -1, bool skipUsedNodes = true)
        {
            if (NodeContainer == null) return null;
            float maxDistSqr = maxDistance * maxDistance;
            float minDistSqr = minDistance * minDistance;
            // cMath::RandRectl(0, nodes - 1): drawn and unused in the original.
            HplMath.RandRectl(0, NodeContainer.NodeCount - 1);

            float closestDistSqr = maxDistSqr * 10f;
            AINode ret = null;
            var inDistance = new List<AINode>();
            Vector3 hplPos = HplSpace.Position(pos);
            Vector3 hplEnemy = HplSpace.Position(_enemy.Body.Position);
            var flags = (AIFreePathFlag)freePathRayNum;

            foreach (var node in NodeContainer.NodesNearHpl(hplPos, maxDistance))
            {
                if (node == skipNode) continue;
                if (skipUsedNodes && _enemy.Map.AINodeIsUsedAsGoal(node)) continue;
                float distSqr = (node.HplPosition - hplPos).sqrMagnitude;
                if (distSqr > maxDistSqr || distSqr < minDistSqr) continue;
                if (getClosest && distSqr > closestDistSqr) continue;
                if (posToNodeFreePathCheck && !NodeContainer.FreePathHpl(hplPos, node.HplPosition, freePathRayNum, flags)) continue;
                if (enemyToNodeFreePathCheck && !NodeContainer.FreePathHpl(hplEnemy, node.HplPosition, freePathRayNum, flags)) continue;
                if (!getClosest) inDistance.Add(node);
                ret = node;
                closestDistSqr = distSqr;
            }
            if (!getClosest && inDistance.Count > 0) return inDistance[HplMath.RandRectl(0, inDistance.Count - 1)];
            return ret;
        }

        public void OnUpdate(float timeStep) => UpdateMoving();

        /// <summary>The node walked to now, or the goal.</summary>
        public Vector3 GetNextGoalPos() => _pathNodes.Count == 0 ? _moveGoalPos : _pathNodes[_pathNodes.Count - 1].Position;

        /// <summary>The first node of the path is a goal used by AINodeIsUsedAsGoal (the list front).</summary>
        public AINode PathFront => _pathNodes.Count > 0 ? _pathNodes[0] : null;

        void UpdateMoving()
        {
            if (!IsMoving) return;
            var body = _enemy.Body;
            Vector3 size = body.Size;
            var tempBV = new Bounds(body.Position - new Vector3(0f, size.y * 0.1f, 0f),
                                    new Vector3(size.x * 1.3f, size.y * 1.2f + 0.45f, size.z * 1.3f));

            Vector3 goal = _pathNodes.Count == 0 ? _moveGoalPos : _pathNodes[_pathNodes.Count - 1].Position;
            _mover.MoveToPos(goal);

            bool stuckAtNode = false;
            float nodeDist = (goal - body.FeetPosition).sqrMagnitude;
            _pathNodeDistances.Add(nodeDist);
            if (_pathNodeDistances.Count > 150)
            {
                _pathNodeDistances.RemoveAt(0);
                float avg = 0f;
                float previous = _pathNodeDistances[0];
                // The original subtracts the first distance from each following one (not the step changes).
                for (int i = 1; i < _pathNodeDistances.Count; i++) avg += _pathNodeDistances[i] - previous;
                if (avg > 0f && nodeDist < size.x * 1.5f) stuckAtNode = true;
            }

            if (stuckAtNode || tempBV.Contains(goal))
            {
                if (_pathNodes.Count == 0)
                {
                    IsMoving = false;
                    _enemy.SendMessage(EnemyMessage.EndOfPath);
                }
                else _pathNodes.RemoveAt(_pathNodes.Count - 1);
                _pathNodeDistances.Clear();
            }
            else if (_mover.StuckCounterIsAtMax)
            {
                _mover.ResetStuckCounter();
                _pathNodes.Clear();
                _pathNodeDistances.Clear();
                _enemy.SendMessage(EnemyMessage.EndOfPath, 0f, false, Vector3.zero, 0f, 1);
            }
        }
    }
}
