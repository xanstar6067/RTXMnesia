using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.Importer
{
    // Independent check against the MSH v7 caches (HplMsh). Never modifies the game cache.
    public static class HplMeshAudit
    {
        // Explicit HPL2 Rz*Ry*Rx, independent of HplSpace.Rotation/Unity Euler conversion.
        static Matrix4x4 Placement(Vector3 p, Vector3 r, Vector3 s)
        {
            float x = Mathf.Cos(r.x), y = Mathf.Cos(r.y), z = Mathf.Cos(r.z);
            float a = Mathf.Sin(r.x), b = Mathf.Sin(r.y), c = Mathf.Sin(r.z);
            var m = Matrix4x4.identity;
            m.m00 = z * y; m.m01 = z * b * a - c * x; m.m02 = z * b * x + c * a;
            m.m10 = c * y; m.m11 = c * b * a + z * x; m.m12 = c * b * x - z * a;
            m.m20 = -b; m.m21 = y * a; m.m22 = y * x;
            m.SetColumn(3, new Vector4(p.x, p.y, p.z, 1));
            return HplModelBuilder.ToUnity(m * Matrix4x4.Scale(s));
        }

        static float Error(Matrix4x4 a, Matrix4x4 b)
        {
            float error = 0;
            for (int i = 0; i < 16; i++) error = Mathf.Max(error, Mathf.Abs(a[i] - b[i]));
            return error;
        }

        static int AuditPlacement(string map, GameObject root, StringBuilder report)
        {
            var doc = new XmlDocument();
            doc.Load(Path.Combine(AmnesiaPaths.SourceRoot, map));
            var contents = doc.DocumentElement.Child("MapData").Child("MapContents");
            int instances = 0, submeshes = 0, failures = 0, colliders = 0;
            float max = 0;
            var cache = new Dictionary<string, Dictionary<string, Matrix4x4>>();
            var staticFiles = contents.Child("FileIndex_StaticObjects").Children().ToDictionary(e => e.Int("Id"), e => e.Str("Path"));
            var entityFiles = contents.Child("FileIndex_Entities").Children().ToDictionary(e => e.Int("Id"), e => e.Str("Path"));
            void Check(string name, Matrix4x4 actual, Matrix4x4 expected)
            {
                float error = Error(actual, expected);
                max = Mathf.Max(max, error);
                if (error > 0.001f) { failures++; report.AppendLine($"PLACEMENT {name}: error={error:G6}"); }
            }
            foreach (var e in contents.Child("StaticObjects").Children())
            {
                var instance = root.transform.Find("StaticObjects/" + e.Str("Name"));
                if (instance == null) { failures++; continue; }
                instances++;
                var placement = Placement(e.Vec3("WorldPos"), e.Vec3("Rotation"), e.Vec3("Scale", Vector3.one));
                Check(instance.name, instance.localToWorldMatrix, placement);
                string file = instance.GetComponent<AmnesiaModel>().file;
                string wanted = staticFiles[e.Int("FileIndex")];
                if (!string.Equals(file.Replace('\\', '/'), wanted.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                { failures++; report.AppendLine($"WRONG RESOURCE {instance.name}: {file}, expected {wanted}"); }
                if (!cache.TryGetValue(file, out var nodes))
                {
                    HplMsh.Read(Path.ChangeExtension(Path.Combine(AmnesiaPaths.SourceRoot, file), ".msh"), out nodes);
                    cache[file] = nodes;
                }
                foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!nodes.TryGetValue(filter.name, out var node)) { failures++; continue; }
                    submeshes++;
                    Check(instance.name + "/" + filter.name, filter.transform.localToWorldMatrix, placement * HplModelBuilder.ToUnity(node));
                    var collider = filter.GetComponent<MeshCollider>();
                    if (collider != null && collider.enabled)
                    {
                        colliders++;
                        if (collider.sharedMesh != filter.sharedMesh) { failures++; report.AppendLine("COLLISION MESH " + instance.name); }
                    }
                }
            }
            foreach (var e in contents.Child("Entities").Children().Where(e => e.Name == "Entity"))
            {
                var instance = root.transform.Find("Entities/" + e.Str("Name"));
                if (instance == null) { failures++; continue; }
                instances++;
                var placement = Placement(e.Vec3("WorldPos"), e.Vec3("Rotation"), Vector3.one);
                Check(instance.name, instance.localToWorldMatrix, placement);
                string entityFile = instance.GetComponent<AmnesiaEntity>().file;
                if (!string.Equals(entityFile.Replace('\\', '/'), entityFiles[e.Int("FileIndex")].Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                { failures++; report.AppendLine("WRONG ENTITY RESOURCE " + instance.name); }
                var ent = HplEntityFile.Load(Path.Combine(AmnesiaPaths.SourceRoot, entityFile));
                var scale = e.Vec3("Scale", Vector3.one);
                var filters = instance.GetComponentsInChildren<MeshFilter>(true).ToList();
                foreach (var sub in ent.SubMeshes)
                {
                    var filter = filters.FirstOrDefault(f => f.name == sub.Name);
                    if (filter == null) { failures++; report.AppendLine("MISSING ENTITY SUBMESH " + instance.name + "/" + sub.Name); continue; }
                    filters.Remove(filter);
                    submeshes++;
                    Check(instance.name + "/" + sub.Name, filter.transform.localToWorldMatrix,
                        placement * Placement(Vector3.Scale(sub.Position, scale), sub.Rotation, Vector3.Scale(sub.Scale, scale)));
                }
                var bodies = instance.GetComponentsInChildren<AmnesiaBody>(true).ToList();
                foreach (var body in ent.Bodies)
                {
                    var component = bodies.FirstOrDefault(b => b.name == body.Name);
                    var bodyTransform = component != null ? component.transform : null;
                    if (bodyTransform == null) { failures++; report.AppendLine("MISSING BODY " + instance.name + "/" + body.Name); continue; }
                    bodies.Remove(component);
                    Check(instance.name + "/" + body.Name, bodyTransform.localToWorldMatrix,
                        placement * Placement(Vector3.Scale(body.Position, scale), body.Rotation, Vector3.one));
                }
            }
            report.AppendLine($"Scene: instances={instances}, submeshMatrices={submeshes}, sharedMeshColliders={colliders}, failures={failures}, maxMatrixError={max:G6}");
            return failures;
        }

        static int AuditResourceLookup(StringBuilder report)
        {
            var index = new ResourceIndex(AmnesiaPaths.SourceRoot);
            int count = 0, failures = 0;
            // Regression: every ambiguous model, its old exporter path, and its bare name near that model.
            foreach (var group in index.AllWithExtension(".dae").GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                foreach (string file in group)
                {
                    string relative = Path.GetRelativePath(AmnesiaPaths.SourceRoot, file).Replace('\\', '/');
                    foreach (var reference in new[] { relative, "Q:/old_machine/redist/" + relative, Path.GetFileName(file) })
                    {
                        count++;
                        if (!string.Equals(Path.GetFullPath(index.Find(reference, file) ?? "."), Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase))
                        { failures++; report.AppendLine("RESOURCE LOOKUP " + reference); }
                    }
                }
            report.AppendLine($"Resource regression: checks={count}, failures={failures}");
            return failures;
        }

        [MenuItem("Tools/RTXMnesia/Import/Audit level meshes against MSH")]
        public static void Run()
        {
            string map = ImportMenu.Arg("-amnesiaMap", ImportMenu.TestMap);
            string scenePath = "Assets/AmnesiaImported/Levels/" + Path.ChangeExtension(map, ".unity");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != scenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(scenePath);
            }
            var models = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AmnesiaModel>(true))
                .Select(m => m.file).Distinct().OrderBy(s => s).ToList();
            var report = new StringBuilder();
            int checkedMeshes = 0, failures = 0, missing = 0, matrixDifferences = 0;
            float maxPosition = 0, maxMatrix = 0;
            foreach (string file in models)
            {
                string dae = Path.Combine(AmnesiaPaths.SourceRoot, file);
                string msh = Path.ChangeExtension(dae, ".msh");
                if (!File.Exists(msh)) { missing++; report.AppendLine("NO CACHE " + file); continue; }
                var reference = HplMsh.Read(msh, out var nodes);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HplModelBuilder.PrefabPathFor(dae));
                var filters = prefab.GetComponentsInChildren<MeshFilter>(true).ToList();
                foreach (var sub in reference)
                {
                    var filter = filters.FirstOrDefault(f => f.name == sub.Name);
                    if (filter == null) { failures++; report.AppendLine($"MISSING {file}: {sub.Name}"); continue; }
                    filters.Remove(filter);
                    checkedMeshes++;
                    var mesh = filter.sharedMesh;
                    var actual = mesh.vertices;
                    var expected = sub.Positions;
                    var indices = mesh.triangles;
                    float posError = 0, matrixError = 0;
                    // HPL's vertex splitting order is retained by our parser.
                    if (actual.Length != expected.Length || indices.Length != sub.Indices.Length)
                    {
                        failures++;
                        report.AppendLine($"TOPOLOGY {file}: {sub.Name} vertices {actual.Length}/{expected.Length}, indices {indices.Length}/{sub.Indices.Length}");
                        continue;
                    }
                    for (int v = 0; v < actual.Length; v++)
                        posError = Mathf.Max(posError, (actual[v] - new Vector3(-expected[v].x, expected[v].y, expected[v].z)).magnitude);
                    var expectedMatrix = HplModelBuilder.ToUnity(nodes[sub.Name]);
                    var actualMatrix = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                    for (int j = 0; j < 16; j++) matrixError = Mathf.Max(matrixError, Mathf.Abs(expectedMatrix[j] - actualMatrix[j]));
                    maxPosition = Mathf.Max(maxPosition, posError);
                    maxMatrix = Mathf.Max(maxMatrix, matrixError);
                    int windingErrors = 0;
                    // HPL2 stores (c,b,a), our builder (a,c,b): same oriented triangle with a different
                    // starting corner. Compare cyclic permutations, never an unoriented set of indices.
                    for (int j = 0; j < indices.Length; j += 3)
                    {
                        bool equal = false;
                        for (int shift = 0; shift < 3; shift++)
                            if (indices[j] == sub.Indices[j + shift] &&
                                indices[j + 1] == sub.Indices[j + (shift + 1) % 3] &&
                                indices[j + 2] == sub.Indices[j + (shift + 2) % 3]) equal = true;
                        if (!equal) windingErrors++;
                    }
                    if (matrixError > 0.001f)
                    {
                        matrixDifferences++;
                        report.AppendLine($"PREFAB MATRIX DIFF {file}: {sub.Name} error={matrixError:G6} (entity overrides are checked separately in scene)");
                    }
                    if (posError > 0.001f || windingErrors > 0)
                    {
                        failures++;
                        var ix = sub.Indices;
                        float normalDot = Vector3.Dot(Vector3.Cross(expected[ix[1]] - expected[ix[0]], expected[ix[2]] - expected[ix[0]]), sub.Normals[ix[0]]);
                        report.AppendLine($"DIFF {file}: {sub.Name} vertex={posError:G6} matrix={matrixError:G6} indices={windingErrors}/{indices.Length} actual={string.Join(",", indices.Take(6))} reference={string.Join(",", ix.Take(6))} refNormalDot={normalDot:G6}");
                    }
                }
            }
            report.Insert(0, $"Models={models.Count}, submeshes={checkedMeshes}, geometryFailures={failures}, prefabMatrixDifferences={matrixDifferences}, missingCaches={missing}, maxVertexError={maxPosition:G6}, maxPrefabMatrixError={maxMatrix:G6}\n");
            failures += AuditPlacement(map, scene.GetRootGameObjects().First(g => g.GetComponent<AmnesiaLevel>() != null), report);
            failures += AuditResourceLookup(report);
            string output = Path.Combine(Application.dataPath, "../Logs/HplMeshAudit.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, report.ToString());
            Debug.Log(report.ToString());
            if (Application.isBatchMode && (failures > 0 || missing > 0)) EditorApplication.Exit(1);
        }
    }
}
