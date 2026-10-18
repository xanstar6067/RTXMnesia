using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTXMnesia.Dynamics;
using RTXMnesia.Player;
using RTXMnesia.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Builds a prefab from a non-skinned .dae exactly as HPL2 loads it (<see cref="HplCollada"/>, MeshLoaderCollada.cpp,
    /// WorldLoaderHplMap.cpp CreateStaticObjectEntity): one GameObject per Collada node with the engine's node
    /// transform, the sub mesh (vertices with the node's own scale baked in) on the node holding the geometry, and
    /// static-object collision:
    /// - "_collider_&lt;box|sphere|capsule|cylinder&gt;" nodes → primitive colliders (children of the root, at the node's
    ///   world transform + bounds centre); "_charcollider_…" the same, for characters only (HPL2: SetCollide(false):
    ///   no contacts with bodies, rays skip them; here on <see cref="HplPhysicsWorld.NoCollideLayer"/>);
    /// - "_collider_mesh…" / "_charcollider_mesh…" → invisible mesh colliders;
    /// - other sub meshes get a mesh collider unless the model has a "_collider…" mesh or a normal primitive collider
    ///   belongs to the sub mesh (group = parent node name, or the first sub mesh).
    /// Space: HPL2 matrices M become Unity ones as S·M·S (S = mirror X), vertices as S·v; triangles are reversed
    /// to stay front facing after the mirror. Output: Models/&lt;path&gt;.prefab + &lt;path&gt;_meshes.asset.
    /// </summary>
    public static class HplModelBuilder
    {
        /// <summary>Bump when the output changes: prefabs of older versions are rebuilt.</summary>
        public const int Version = 6;

        static readonly Matrix4x4 Mirror = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));

        public static string PrefabPathFor(string daeAbsolute) =>
            Path.ChangeExtension(AmnesiaPaths.ModelsRoot + "/" + AmnesiaPaths.ToSourceRelative(daeAbsolute), ".prefab");

        static string MeshAssetPathFor(string daeAbsolute) =>
            Path.ChangeExtension(PrefabPathFor(daeAbsolute), null) + "_meshes.asset";

        public static bool IsUpToDate(string daeAbsolute)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPathFor(daeAbsolute));
            if (prefab == null || !prefab.TryGetComponent(out AmnesiaModel model) || model.builderVersion != Version) return false;
            return File.GetLastWriteTimeUtc(AmnesiaPaths.ToAbsolute(PrefabPathFor(daeAbsolute))) >= File.GetLastWriteTimeUtc(daeAbsolute);
        }

        /// <summary>Unity matrix of an HPL2 one (mirror X on both sides).</summary>
        public static Matrix4x4 ToUnity(Matrix4x4 hpl) => Mirror * hpl * Mirror;

        public static GameObject Build(string daeAbsolute, ResourceIndex index, ImportLog log)
        {
            string rel = AmnesiaPaths.ToSourceRelative(daeAbsolute);
            var dae = HplCollada.Load(daeAbsolute);
            var info = ColladaInfo.Load(daeAbsolute);
            foreach (string w in dae.Warnings) log.Info($"{rel}: {w}");

            string prefabPath = PrefabPathFor(daeAbsolute);
            string meshPath = MeshAssetPathFor(daeAbsolute);
            Directory.CreateDirectory(Path.GetDirectoryName(AmnesiaPaths.ToAbsolute(prefabPath)));

            var root = new GameObject(Path.GetFileNameWithoutExtension(daeAbsolute));
            var model = root.AddComponent<AmnesiaModel>();
            model.file = rel;
            model.builderVersion = Version;

            // Sub meshes: one per loaded geometry, at the first node using it, named after that node.
            // Primitive collider nodes only give shapes, they are no sub meshes.
            var subMeshes = new List<(HplCollada.Geometry geo, HplCollada.Node node)>();
            var colliderNodes = new List<(HplCollada.Geometry geo, HplCollada.Node node)>();
            foreach (var geo in dae.Geometries)
            {
                var node = dae.NodeFor(geo);
                if (node == null) { log.Warn($"{rel}: no node for geometry '{geo.Name}'"); continue; }
                (IsPrimitiveCollider(node.Name) ? colliderNodes : subMeshes).Add((geo, node));
            }
            var subMeshNames = new HashSet<string>(subMeshes.Select(s => s.node.Name));

            // Node hierarchy (CreateHierarchyNodes + "clear scale" of sub mesh nodes).
            var objects = new Dictionary<HplCollada.Node, GameObject>();
            void CreateNode(HplCollada.Node node, Transform parent)
            {
                var go = new GameObject(node.Name);
                go.transform.SetParent(parent, false);
                Matrix4x4 local = node.Local;
                local.SetColumn(3, new Vector4(local.m03 * dae.UnitScale, local.m13 * dae.UnitScale, local.m23 * dae.UnitScale, 1f));
                var sub = subMeshes.FirstOrDefault(s => s.node.Name == node.Name);
                if (sub.node != null) local *= Matrix4x4.Scale(Inverse(sub.node.Scale));
                SetLocalMatrix(go.transform, ToUnity(local), log, $"{rel} node '{node.Name}'");
                objects[node] = go;
                foreach (var child in node.Children) CreateNode(child, go.transform);
            }
            foreach (var r in dae.Roots) CreateNode(r, root.transform);

            // Primitive collider shapes (MeshLoaderCollada.cpp LoadMesh), attached to the sub mesh named like the
            // parent node, or to the first sub mesh.
            var primitiveGroups = new HashSet<string>(); // sub meshes that have a normal (non-char) primitive collider
            foreach (var (geo, node) in colliderNodes)
            {
                string[] parts = node.Name.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                string group = node.Parent != null ? node.Parent.Name : "";
                string owner = group != "" ? group : subMeshes.Count > 0 ? subMeshes[0].node.Name : null;
                if (owner == null || !subMeshNames.Contains(owner))
                {
                    log.Info($"{rel}: collider '{node.Name}' has no sub mesh '{group}' (dropped, as HPL2)");
                    continue;
                }
                BuildPrimitiveCollider(geo, node, parts[1].ToLowerInvariant(), root.transform);
                if (parts[0].ToLowerInvariant() == "collider") primitiveGroups.Add(owner);
            }

            bool hasColliderMesh = subMeshes.Any(s => s.node.Name.StartsWith("_collider"));
            var meshes = new List<Mesh>();
            foreach (var (geo, node) in subMeshes)
            {
                bool isCollider = HplCollada.IsColliderName(node.Name);
                var go = objects[node];
                if (go.TryGetComponent(out MeshFilter _))
                {
                    // A second geometry on the same node (rare): its own object at the same place.
                    var extra = new GameObject(node.Name);
                    extra.transform.SetParent(go.transform, false);
                    go = extra;
                }
                var mesh = BuildMesh(geo, node, dae.UnitScale);
                mesh.name = node.Name;
                meshes.Add(mesh);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;

                if (isCollider) continue; // "_collider_mesh" / "_charcollider_mesh": collider only, added below
                string materialRef = node.InstanceMaterial != "" ? node.InstanceMaterial : geo.Material;
                var material = FindMaterial(info, materialRef, daeAbsolute, index);
                if (material != null)
                {
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                }
                else if (info.MaterialTextures.ContainsKey(materialRef))
                    log.Warn($"{rel}: sub mesh '{node.Name}' material '{materialRef}' not converted");
                // No texture: HPL2 gives the sub mesh no material and never draws it.
            }

            ApplyMeshCache(daeAbsolute, root, rel, log);

            // Mesh colliders (CreateStaticObjectEntity).
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.sharedMesh.triangles.Length == 0) continue;
                string name = filter.gameObject.name;
                bool colliderMesh = HplCollada.IsColliderName(name);
                bool create = colliderMesh || (!hasColliderMesh && !primitiveGroups.Contains(name));
                if (!create) continue;
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                if (IsCharColliderName(name)) filter.gameObject.layer = HplPhysicsWorld.NoCollideLayer;
            }

            // Save meshes and prefab.
            AssetDatabase.DeleteAsset(meshPath);
            if (meshes.Count > 0)
            {
                var container = new Mesh { name = root.name + " meshes" };
                AssetDatabase.CreateAsset(container, meshPath);
                foreach (var m in meshes) AssetDatabase.AddObjectToAsset(m, container);
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// The game draws the .msh cache, not the .dae (<see cref="HplMsh"/>). Some caches were written by another
        /// version of the Collada loader: the same vertices with another sub mesh matrix (zimmarman_ground.dae: the x of
        /// its Z_UP node translations negated, while zimmarman_shrubb_dead.dae matches MeshLoaderCollada of the source).
        /// Where a sub mesh's vertices match the cache, the cache's matrix wins; children keep their place.
        /// </summary>
        static void ApplyMeshCache(string daeAbsolute, GameObject root, string rel, ImportLog log)
        {
            string msh = Path.ChangeExtension(daeAbsolute, ".msh");
            if (!File.Exists(msh)) return;
            List<HplMsh.SubMesh> cache;
            Dictionary<string, Matrix4x4> nodes;
            try { cache = HplMsh.Read(msh, out nodes); }
            catch (Exception e) { log.Info($"{rel}: mesh cache not compared ({e.Message})"); return; }
            var filters = root.GetComponentsInChildren<MeshFilter>(true).ToList();
            foreach (var sub in cache)
            {
                var filter = filters.FirstOrDefault(f => f.name == sub.Name);
                if (filter == null || filter.sharedMesh == null) continue;
                filters.Remove(filter);
                var vertices = filter.sharedMesh.vertices;
                if (sub.Positions == null || vertices.Length != sub.Positions.Length) continue;
                bool sameVertices = true;
                for (int v = 0; v < vertices.Length && sameVertices; v++)
                    sameVertices = (vertices[v] - new Vector3(-sub.Positions[v].x, sub.Positions[v].y, sub.Positions[v].z)).sqrMagnitude <= 1e-6f;
                if (!sameVertices) continue;
                var t = filter.transform;
                Matrix4x4 expected = ToUnity(nodes[sub.Name]);
                Matrix4x4 actual = root.transform.worldToLocalMatrix * t.localToWorldMatrix;
                float error = 0f;
                for (int j = 0; j < 16; j++) error = Mathf.Max(error, Mathf.Abs(expected[j] - actual[j]));
                if (error <= 0.001f) continue;
                var children = new List<Transform>();
                foreach (Transform child in t) children.Add(child);
                foreach (var child in children) child.SetParent(root.transform, true);
                SetLocalMatrix(t, t.parent.worldToLocalMatrix * root.transform.localToWorldMatrix * expected, log, $"{rel} sub mesh '{sub.Name}' from the mesh cache");
                foreach (var child in children) child.SetParent(t, true);
                log.Info($"{rel}: sub mesh '{sub.Name}' placed as in the mesh cache (matrix differs by {error:G4})");
            }
        }

        /// <summary>WorldLoaderHplMap: a char collider body has SetCollide(false), only the character is stopped by it.</summary>
        public static bool IsCharColliderName(string name) => name.StartsWith("_charcollider", StringComparison.OrdinalIgnoreCase);

        static bool IsPrimitiveCollider(string name)
        {
            if (!HplCollada.IsColliderName(name)) return false;
            string[] parts = name.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 && parts[1].ToLowerInvariant() != "mesh";
        }

        static Vector3 Inverse(Vector3 s) => new(s.x != 0f ? 1f / s.x : 0f, s.y != 0f ? 1f / s.y : 0f, s.z != 0f ? 1f / s.z : 0f);

        /// <summary>Vertices scaled by the node's own scale (and unit), mirrored; normals by the inverse scale.</summary>
        static Mesh BuildMesh(HplCollada.Geometry geo, HplCollada.Node node, float unitScale)
        {
            Vector3 scale = node.Scale * unitScale;
            Vector3 normalScale = Inverse(node.Scale);
            int n = geo.Positions.Length;
            var vertices = new Vector3[n];
            var normals = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 p = Vector3.Scale(geo.Positions[i], scale);
                vertices[i] = new Vector3(-p.x, p.y, p.z);
                Vector3 nn = Vector3.Scale(geo.Normals[i], normalScale).normalized;
                normals[i] = new Vector3(-nn.x, nn.y, nn.z);
            }
            var triangles = new int[geo.Indices.Length - geo.Indices.Length % 3];
            for (int i = 0; i < triangles.Length; i += 3)
            {
                triangles[i] = geo.Indices[i];
                triangles[i + 1] = geo.Indices[i + 2];
                triangles[i + 2] = geo.Indices[i + 1];
            }
            var mesh = new Mesh { indexFormat = n > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = geo.TexCoords;
            mesh.triangles = triangles;
            if (geo.TexCoords != null && triangles.Length > 0) mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// LoadMesh collider: bounds of the node's vertices; offset = node world transform moved to the bounds
        /// centre (× node scale); size × node scale; capsule/cylinder: radius = half X size, height = Y size, along Y.
        /// </summary>
        static void BuildPrimitiveCollider(HplCollada.Geometry geo, HplCollada.Node node, string type, Transform root)
        {
            if (geo.Positions.Length == 0) return;
            var b = new Bounds(geo.Positions[0], Vector3.zero);
            foreach (var p in geo.Positions) b.Encapsulate(p);

            Matrix4x4 offset = node.World;
            Vector3 centre = offset.GetColumn(3);
            centre += offset.MultiplyVector(Vector3.Scale(b.center, node.Scale));
            offset.SetColumn(3, new Vector4(centre.x, centre.y, centre.z, 1f));
            Vector3 size = Vector3.Scale(b.size, node.Scale);

            var go = new GameObject(node.Name);
            go.transform.SetParent(root, false);
            SetLocalMatrix(go.transform, ToUnity(offset), null, null);
            if (IsCharColliderName(node.Name)) go.layer = HplPhysicsWorld.NoCollideLayer;
            switch (type)
            {
                case "box":
                    go.AddComponent<BoxCollider>().size = Abs(size);
                    break;
                case "sphere":
                    go.AddComponent<SphereCollider>().radius = Mathf.Abs(size.x * 0.5f);
                    break;
                case "capsule":
                    var capsule = go.AddComponent<CapsuleCollider>();
                    capsule.direction = 1;
                    capsule.radius = Mathf.Abs(size.x * 0.5f);
                    capsule.height = Mathf.Abs(size.y);
                    break;
                case "cylinder":
                    var cylinder = go.AddComponent<MeshCollider>();
                    cylinder.sharedMesh = CylinderMesh(Mathf.Abs(size.x * 0.5f), Mathf.Abs(size.y));
                    cylinder.convex = true;
                    break;
                default:
                    go.AddComponent<BoxCollider>().size = Abs(size);
                    break;
            }
        }

        static readonly Dictionary<(float, float), Mesh> Cylinders = new();

        static Mesh CylinderMesh(float radius, float height)
        {
            // Stored with the prefab's meshes would need the container; a shared asset per size keeps it simple.
            string path = AmnesiaPaths.ModelsRoot + "/_cylinders.asset";
            var key = (Mathf.Round(radius * 1000f) / 1000f, Mathf.Round(height * 1000f) / 1000f);
            if (Cylinders.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            var container = AssetDatabase.LoadMainAssetAtPath(path);
            string name = $"cylinder_{key.Item1:0.###}x{key.Item2:0.###}";
            mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m => m.name == name);
            if (mesh == null)
            {
                mesh = HplCharacterBody.CreateCylinderMesh(key.Item1, key.Item2, 16);
                mesh.name = name;
                if (container == null)
                {
                    Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(AmnesiaPaths.ModelsRoot));
                    AssetDatabase.CreateAsset(mesh, path);
                }
                else AssetDatabase.AddObjectToAsset(mesh, container);
            }
            Cylinders[key] = mesh;
            return mesh;
        }

        static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        static Material FindMaterial(ColladaInfo info, string materialRef, string daeAbsolute, ResourceIndex index)
        {
            if (string.IsNullOrEmpty(materialRef) || !info.MaterialTextures.TryGetValue(materialRef, out string texture)) return null;
            string matFile = index.Find(Path.ChangeExtension(texture, ".mat"), daeAbsolute);
            return matFile == null ? null : AssetDatabase.LoadAssetAtPath<Material>(MaterialConverter.AssetPathFor(matFile));
        }

        /// <summary>
        /// Sets a transform from a matrix: TRS with a negative X scale for mirroring matrices. Shear cannot be
        /// represented; when the result differs from the matrix it is logged.
        /// </summary>
        public static void SetLocalMatrix(Transform t, Matrix4x4 m, ImportLog log, string what)
        {
            Vector3 c0 = m.GetColumn(0), c1 = m.GetColumn(1), c2 = m.GetColumn(2);
            var s = new Vector3(c0.magnitude, c1.magnitude, c2.magnitude);
            if (m.determinant < 0f) s.x = -s.x;
            Quaternion r = Quaternion.identity;
            if (Mathf.Abs(s.x) > 1e-8f && s.y > 1e-8f && s.z > 1e-8f)
                r = Quaternion.LookRotation(c2 / s.z, c1 / s.y);
            t.localPosition = m.GetColumn(3);
            t.localRotation = r;
            t.localScale = s;

            if (log == null) return;
            Matrix4x4 check = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
            float error = 0f;
            for (int i = 0; i < 12; i++) error = Mathf.Max(error, Mathf.Abs(check[i] - m[i]));
            if (error > 1e-3f) log.Warn($"{what}: transform has shear, approximated (error {error:0.###})");
        }
    }
}
