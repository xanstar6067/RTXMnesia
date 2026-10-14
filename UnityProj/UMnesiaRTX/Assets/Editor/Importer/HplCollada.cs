using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// A .dae read the way HPL2 reads it (MeshLoaderCollada*.cpp), for building meshes that sit exactly where
    /// the engine puts them. All vectors and matrices are in HPL2 space (Unity's Matrix4x4 only as a container;
    /// the math is the same, column vectors).
    /// - Z_UP: vertex positions/normals (x, y, z) → (-x, z, y); node translations and scales (x, z, y);
    ///   rotate axes and matrix elements as written (GetVectorPos, GetVectorPosFromPtr, GetMatrix).
    /// - &lt;unit&gt; is only used for files from the "FBX COLLADA exporter" (MeshLoaderColladaLoader.cpp).
    /// - Node transform = product of its elements in order; scale is left out for _collider/_charcollider/_area
    ///   nodes (LoadColladaScene). A sub mesh's own node scale is baked into its vertices and removed from the
    ///   node, so it is not inherited by child nodes (LoadMesh "Clear scale on all joint nodes").
    /// - Only the first &lt;triangles&gt;/&lt;polylist&gt;/&lt;polygons&gt; of a geometry is loaded (one material),
    ///   geometries named _ref/_bb/_ps/_sound or without normals (or tex coords, unless named "_…") are skipped,
    ///   tex coords use the last TEXCOORD input. Each geometry becomes one sub mesh at the first node using it,
    ///   named after the node.
    /// </summary>
    public class HplCollada
    {
        public class Node
        {
            public string Id = "", Name = "", Sid = "";
            public Node Parent;
            public readonly List<Node> Children = new();
            /// <summary>m_mtxTransform: the node's elements (scale left out on collider/area nodes).</summary>
            public Matrix4x4 Local = Matrix4x4.identity;
            /// <summary>m_mtxWorldTransform: parents × Local (used for collider shapes).</summary>
            public Matrix4x4 World = Matrix4x4.identity;
            /// <summary>mvScale: the node's last scale element.</summary>
            public Vector3 Scale = Vector3.one;
            /// <summary>Url of instance_geometry/light/controller (without '#').</summary>
            public string Source = "";
            public bool SourceIsController;
            public string InstanceMaterial = "";
        }

        public class Geometry
        {
            public string Id = "", Name = "", Material = "";
            /// <summary>After the HPL2 vertex split; positions/normals converted for Z_UP, tex coords as in the file.</summary>
            public Vector3[] Positions, Normals;
            public Vector2[] TexCoords;
            public int[] Indices;
        }

        public string File;
        public bool ZUp;
        public float UnitScale = 1f;
        public string AuthoringTool = "";
        public readonly List<Node> Roots = new();
        public readonly List<Node> Nodes = new();
        /// <summary>Loaded geometries in file order (skipped ones are absent).</summary>
        public readonly List<Geometry> Geometries = new();
        public bool HasSkin;
        public readonly List<string> Warnings = new();

        const string Ns = "http://www.collada.org/2005/11/COLLADASchema";

        public static HplCollada Load(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            var c = new HplCollada { File = path };
            var root = doc.DocumentElement;

            var asset = Child(root, "asset");
            if (asset != null)
            {
                c.ZUp = Child(asset, "up_axis")?.InnerText.Trim() == "Z_UP";
                var contributor = Child(asset, "contributor");
                c.AuthoringTool = (Child(contributor ?? asset, "authoring_tool")?.InnerText ?? "").Trim();
                var unit = Child(asset, "unit");
                if (unit != null && c.AuthoringTool == "FBX COLLADA exporter" && TryFloat(unit.GetAttribute("meter"), out float meter))
                    c.UnitScale = meter;
            }

            c.HasSkin = root.GetElementsByTagName("skin", Ns).Count > 0;

            foreach (var lib in Children(root, "library_geometries").Concat(
                         Children(root, "library").Where(l => l.GetAttribute("type") == "GEOMETRY")))
                foreach (var g in Children(lib, "geometry"))
                    c.LoadGeometry(g);

            // The scene the file instantiates, or the first one.
            var scenes = Children(root, "library_visual_scenes").SelectMany(l => Children(l, "visual_scene")).ToList();
            string sceneUrl = (Child(Child(root, "scene"), "instance_visual_scene")?.GetAttribute("url") ?? "").TrimStart('#');
            var scene = scenes.FirstOrDefault(s => s.GetAttribute("id") == sceneUrl) ?? scenes.FirstOrDefault()
                        ?? Child(root, "scene"); // COLLADA 1.3 stores nodes directly in scene.
            if (scene != null)
                foreach (var n in Children(scene, "node"))
                    c.Roots.Add(c.LoadNode(n, null));
            return c;
        }

        /// <summary>GetNodeFromSource: the first node (depth first) instancing this geometry.</summary>
        public Node NodeFor(Geometry g) => Nodes.FirstOrDefault(n => n.Source == g.Id && !n.SourceIsController)
                                           ?? Nodes.FirstOrDefault(n => n.Source == g.Id);

        public static bool IsColliderName(string nodeName)
        {
            string n = nodeName.ToLowerInvariant();
            return n.StartsWith("_collider") || n.StartsWith("_charcollider");
        }

        // ------------------------------------------------------------------ scene

        Node LoadNode(XmlElement e, Node parent)
        {
            var node = new Node { Parent = parent, Id = e.GetAttribute("id"), Name = e.GetAttribute("name") };
            node.Sid = e.HasAttribute("sid") ? e.GetAttribute("sid") : node.Id;
            Nodes.Add(node);

            var instance = Child(e, "instance_geometry") ?? Child(e, "instance_light") ?? Child(e, "instance_controller") ?? Child(e, "instance");
            if (instance != null)
            {
                node.Source = instance.GetAttribute("url").TrimStart('#');
                node.SourceIsController = instance.LocalName == "instance_controller";
                var bind = Child(Child(Child(instance, "bind_material"), "technique_common"), "instance_material");
                if (bind != null) node.InstanceMaterial = bind.GetAttribute("target").TrimStart('#');
            }

            string lower = node.Name.ToLowerInvariant();
            bool noScale = lower.Length > 1 && (lower.Substring(1).StartsWith("collider") || lower.Substring(1).StartsWith("charcollider") ||
                                                lower.Substring(1).StartsWith("area"));
            Matrix4x4 m = Matrix4x4.identity;
            foreach (XmlNode child in e.ChildNodes)
            {
                if (child is not XmlElement t || t.FirstChild is not XmlText) continue;
                float[] v = Floats(t.InnerText);
                switch (t.LocalName)
                {
                    case "translate":
                        m *= Matrix4x4.Translate(PosFromPtr(v));
                        break;
                    case "rotate":
                        if (v.Length >= 4) m *= Matrix4x4.Rotate(Quaternion.AngleAxis(v[3], new Vector3(v[0], v[1], v[2])));
                        break;
                    case "scale":
                        node.Scale = PosFromPtr(v);
                        if (!noScale) m *= Matrix4x4.Scale(node.Scale);
                        break;
                    case "matrix":
                        if (v.Length >= 16)
                        {
                            var mm = new Matrix4x4();
                            for (int r = 0; r < 4; r++)
                                for (int col = 0; col < 4; col++) mm[r, col] = v[r * 4 + col];
                            m *= mm;
                        }
                        break;
                }
            }
            node.Local = m;
            node.World = parent != null ? parent.World * m : m;

            foreach (var n in Children(e, "node")) node.Children.Add(LoadNode(n, node));
            return node;
        }

        Vector3 PosFromPtr(float[] v)
        {
            float x = v.Length > 0 ? v[0] : 0f, y = v.Length > 1 ? v[1] : 0f, z = v.Length > 2 ? v[2] : 0f;
            return ZUp ? new Vector3(x, z, y) : new Vector3(x, y, z);
        }

        Vector3 VertexPos(Vector3 v) => ZUp ? new Vector3(-v.x, v.z, v.y) : v;

        // ------------------------------------------------------------------ geometry

        class Source
        {
            public string Id;
            public string Semantic = "";
            public bool InVertex;
            public Vector3[] Data;
        }

        void LoadGeometry(XmlElement g)
        {
            var mesh = Child(g, "mesh");
            if (mesh == null) return;

            var geo = new Geometry { Id = g.GetAttribute("id"), Name = g.GetAttribute("name") };
            if (geo.Name == "")
            {
                geo.Name = geo.Id;
                int dash = geo.Name.LastIndexOf('-');
                if (dash > 0) geo.Name = geo.Name.Substring(0, dash);
            }
            if (geo.Name.StartsWith("_ref") || geo.Name.StartsWith("_bb") || geo.Name.StartsWith("_ps") || geo.Name.StartsWith("_sound"))
                return;

            var sources = new List<Source>();
            foreach (var s in Children(mesh, "source")) sources.Add(new Source { Id = s.GetAttribute("id"), Data = LoadVertexData(s) });

            var vertices = Child(mesh, "vertices");
            if (vertices == null) { Warnings.Add($"{geo.Name}: no vertices"); return; }
            foreach (var input in Children(vertices, "input"))
            {
                string semantic = input.GetAttribute("semantic");
                string src = input.GetAttribute("source").TrimStart('#');
                var s = sources.FirstOrDefault(x => x.Id == src);
                if (s == null) continue;
                if (semantic == "POSITION") s.Id = vertices.GetAttribute("id");
                else { s.Semantic = semantic; s.InVertex = true; }
            }

            var tris = Child(mesh, "triangles") ?? Child(mesh, "polylist") ?? Child(mesh, "polygons");
            if (tris == null) { Warnings.Add($"{geo.Name}: no polygons"); return; }
            geo.Material = tris.GetAttribute("material").TrimStart('#');

            int elements = 0;
            int posArray = -1, normArray = -1, texArray = -1, posIdx = -1, normIdx = -1, texIdx = -1;
            foreach (var input in Children(tris, "input"))
            {
                string semantic = input.GetAttribute("semantic");
                string src = input.GetAttribute("source").TrimStart('#');
                int idx = int.TryParse(input.GetAttribute("idx"), out int i0) ? i0 : int.TryParse(input.GetAttribute("offset"), out int i1) ? i1 : -1;
                int array = sources.FindIndex(x => x.Id == src);
                if (semantic == "VERTEX") { posIdx = idx; posArray = array; }
                else if (semantic == "NORMAL") { normIdx = idx; normArray = array; }
                else if (semantic == "TEXCOORD") { texIdx = idx; texArray = array; }
                elements = Math.Max(elements, idx + 1);
            }
            for (int i = 0; i < sources.Count; i++)
            {
                if (!sources[i].InVertex) continue;
                if (sources[i].Semantic == "NORMAL") { normArray = i; normIdx = posIdx; }
                else if (sources[i].Semantic == "TEXCOORD") { texArray = i; texIdx = posIdx; }
            }
            if (posArray < 0) { Warnings.Add($"{geo.Name}: no positions"); return; }
            if (normArray < 0) { Warnings.Add($"{geo.Name}: no normals (skipped, as HPL2)"); return; }
            if (texArray < 0 && !geo.Name.StartsWith("_")) { Warnings.Add($"{geo.Name}: no tex coords (skipped, as HPL2)"); return; }

            Vector3[] pos = sources[posArray].Data.Select(VertexPos).ToArray();
            Vector3[] norm = sources[normArray].Data.Select(VertexPos).ToArray();
            Vector3[] tex = texArray >= 0 ? sources[texArray].Data : null;

            // Triangle corners (pos, norm, tex indices), as many full triangles as the <p> lists hold.
            var corners = new List<(int v, int n, int t)>();
            foreach (var p in Children(tris, "p"))
            {
                int[] ind = Ints(p.InnerText);
                int triangles = elements > 0 ? ind.Length / (3 * elements) : 0;
                for (int tri = 0; tri < triangles; tri++)
                    for (int k = 0; k < 3; k++)
                    {
                        int b = tri * 3 * elements + k * elements;
                        corners.Add((ind[b + posIdx], normIdx >= 0 ? ind[b + normIdx] : 0, texIdx >= 0 ? ind[b + texIdx] : 0));
                    }
            }

            // SplitVertices: vertex i first takes slot i, other combos are appended.
            var outPos = new List<Vector3>(pos);
            var outNorm = new List<Vector3>(new Vector3[pos.Length]);
            var outTex = new List<Vector2>(new Vector2[pos.Length]);
            var used = new List<(int n, int t, int slot)>[pos.Length];
            var indices = new int[corners.Count];
            for (int i = 0; i < corners.Count; i++)
            {
                var (v, n, t) = corners[i];
                if (v < 0 || v >= pos.Length) { indices[i] = 0; continue; }
                Vector3 nn = n >= 0 && n < norm.Length ? norm[n] : Vector3.up;
                Vector2 tt = tex != null && t >= 0 && t < tex.Length ? new Vector2(tex[t].x, tex[t].y) : Vector2.zero;
                if (used[v] == null)
                {
                    used[v] = new List<(int, int, int)> { (n, t, v) };
                    outNorm[v] = nn;
                    outTex[v] = tt;
                    indices[i] = v;
                    continue;
                }
                int slot = -1;
                foreach (var u in used[v]) if (u.n == n && u.t == t) { slot = u.slot; break; }
                if (slot < 0)
                {
                    slot = outPos.Count;
                    used[v].Add((n, t, slot));
                    outPos.Add(pos[v]);
                    outNorm.Add(nn);
                    outTex.Add(tt);
                }
                indices[i] = slot;
            }

            geo.Positions = outPos.ToArray();
            geo.Normals = outNorm.ToArray();
            geo.TexCoords = outTex.ToArray();
            geo.Indices = indices;
            Geometries.Add(geo);
        }

        static Vector3[] LoadVertexData(XmlElement source)
        {
            var technique = Child(source, "technique_common") ?? Child(source, "technique");
            var accessor = Child(technique, "accessor");
            var data = Child(source, "float_array") ?? Child(source, "array");
            if (accessor == null || data == null) return Array.Empty<Vector3>();
            int stride = Math.Max(1, int.TryParse(accessor.GetAttribute("stride"), out int s) ? s : 1);
            int count = int.TryParse(accessor.GetAttribute("count"), out int c) ? c : 0;
            float[] f = Floats(data.InnerText);
            var result = new Vector3[count];
            for (int i = 0; i < count && (i + 1) * stride <= f.Length; i++)
                result[i] = new Vector3(f[i * stride], stride > 1 ? f[i * stride + 1] : 0f, stride > 2 ? f[i * stride + 2] : 0f);
            return result;
        }

        // ------------------------------------------------------------------ xml helpers

        static XmlElement Child(XmlElement e, string name)
        {
            if (e == null) return null;
            foreach (XmlNode n in e.ChildNodes)
                if (n is XmlElement c && c.LocalName == name) return c;
            return null;
        }

        static IEnumerable<XmlElement> Children(XmlElement e, string name)
        {
            if (e == null) yield break;
            foreach (XmlNode n in e.ChildNodes)
                if (n is XmlElement c && c.LocalName == name) yield return c;
        }

        static readonly char[] Separators = { ' ', '\t', '\r', '\n' };

        static float[] Floats(string text)
        {
            var parts = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) TryFloat(parts[i], out result[i]);
            return result;
        }

        static int[] Ints(string text)
        {
            var parts = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++) int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]);
            return result;
        }

        static bool TryFloat(string s, out float f) =>
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
    }
}
