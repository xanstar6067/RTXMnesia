using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    /// <summary>Before generating the dungeonbase relief for the game: the mesh variants EnhancedReliefConverter would
    /// make (same identity: mesh, object scale, slot textures), instances per map, relief area, and a real build of the
    /// most used variants (triangles, bytes, seconds) to extrapolate the catalogue. Writes nothing but the report.</summary>
    public static class DungeonReliefBudgetProbe
    {
        const string Material = "static_objects/dungeonbase/dungeonbase.mat";

        sealed class Variant
        {
            public Mesh mesh; public Vector3 scale; public bool[] displaced; public float area; public int instances;
            public HashSet<string> maps = new();
        }

        public static void RunBatch()
        {
            var classic = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.MaterialsRoot + "/" + Material);
            var albedo = classic.GetTexture("_BaseColorMap");
            var variants = new Dictionary<string, Variant>();
            var lines = new List<string>();
            string[] scenes = Directory.GetFiles(AmnesiaPaths.ToAbsolute(LevelConverter.LevelsRoot), "*.unity", SearchOption.AllDirectories)
                .Select(p => LevelConverter.LevelsRoot + "/" + Path.GetRelativePath(AmnesiaPaths.ToAbsolute(LevelConverter.LevelsRoot), p).Replace('\\', '/'))
                .Where(p => p.Contains("/maps/main/") || p.Contains("/maps/ptest/") || p.Contains("/main_menu/")).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            foreach (string path in scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int count = 0; var mapVariants = new HashSet<string>(); float mapArea = 0;
                foreach (var renderer in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)))
                {
                    if (renderer.GetComponentInParent<Rigidbody>(true) != null || !renderer.TryGetComponent<MeshFilter>(out var filter)) continue;
                    var mesh = filter.sharedMesh; var materials = renderer.sharedMaterials;
                    if (mesh == null || materials.Length != mesh.subMeshCount) continue;
                    var scale = HplReliefSet.ScaleOf(renderer.transform);
                    if (Mathf.Min(scale.x, scale.y, scale.z) < 0.0001f) continue;
                    var m = renderer.localToWorldMatrix;
                    if (Mathf.Abs(Vector3.Dot(m.GetColumn(0).normalized, m.GetColumn(1).normalized)) > 0.001f ||
                        Mathf.Abs(Vector3.Dot(m.GetColumn(0).normalized, m.GetColumn(2).normalized)) > 0.001f ||
                        Mathf.Abs(Vector3.Dot(m.GetColumn(1).normalized, m.GetColumn(2).normalized)) > 0.001f) continue;
                    var displaced = materials.Select(x => x != null && x.HasProperty("_BaseColorMap") && x.GetTexture("_BaseColorMap") == albedo &&
                        x.shader.name == "HDRP/Lit" && x.GetFloat("_SurfaceType") == 0 && x.GetFloat("_AlphaCutoffEnable") == 0 && x.GetFloat("_UVBase") == 0).ToArray();
                    if (!displaced.Any(d => d)) continue;
                    var key = new StringBuilder(AssetDatabase.GetAssetPath(mesh) + ":" + mesh.name + "|" + scale.ToString("F4"));
                    foreach (var x in materials)
                    {
                        var t = x != null && x.HasProperty("_BaseColorMap") ? x.GetTexture("_BaseColorMap") : null;
                        key.Append("|" + (t != null ? t.name + x.GetTextureScale("_BaseColorMap").ToString("F4") + x.GetTextureOffset("_BaseColorMap").ToString("F4") : "-"));
                    }
                    string id = key.ToString();
                    if (!variants.TryGetValue(id, out var variant))
                    {
                        variant = variants[id] = new Variant { mesh = mesh, scale = scale, displaced = displaced };
                        var vertices = mesh.vertices.Select(v => Vector3.Scale(v, scale)).ToArray();
                        for (int s = 0; s < mesh.subMeshCount; s++)
                        {
                            if (!displaced[s]) continue;
                            var tris = mesh.GetTriangles(s);
                            for (int i = 0; i < tris.Length; i += 3)
                                variant.area += Vector3.Cross(vertices[tris[i + 1]] - vertices[tris[i]], vertices[tris[i + 2]] - vertices[tris[i]]).magnitude * .5f;
                        }
                    }
                    variant.instances++; variant.maps.Add(Path.GetFileNameWithoutExtension(path));
                    mapVariants.Add(id); mapArea += variant.area; count++;
                }
                if (count > 0) lines.Add($"{Path.GetFileNameWithoutExtension(path)}: {count} instances, {mapVariants.Count} variants, {mapArea:F0} m2 relief");
                EditorUtility.UnloadUnusedAssetsImmediate();
            }

            // Real builds: the game's settings (1 cm, 120k budget; coarse 9 cm, 24k), on the most used variants.
            var info = JsonUtility.FromJson<ApprovedReliefProfilesInfo>(File.ReadAllText(Path.Combine(ApprovedReliefProfiles.ManualFolder(Material), "relief.json")));
            var data = ApprovedReliefProfiles.Prepare(Material);
            var sample = variants.Values.OrderByDescending(v => v.instances).Take(8)
                .Concat(variants.Values.OrderByDescending(v => v.area).Take(4)).Distinct().ToList();
            // Modes: uniform (the stand's sample 3) and adaptive tolerances; height error of the mesh at triangle
            // centroids and edge midpoints against the maps (0.5 mm sampling of the true field is the vertex values).
            var modes = ImportMenu.Arg("-tolerances", "0,0.001,0.002").Split(',').Select(t => float.Parse(t, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            int budgetArg = int.Parse(ImportMenu.Arg("-budget", "120000"));
            foreach (float tolerance in modes)
            {
                long sampleBytes = 0, sampleTriangles = 0; double seconds = 0; var errors = new List<float>();
                foreach (var v in sample)
                {
                    var surfaces = v.displaced.Select(d => d ? new HplReliefMeshBuilder.Surface { heightMetres = data.height, geometryNormals = false,
                        fadeAtCreases = !info.over_creases, tolerance = tolerance } : null).ToArray();
                    var watch = Stopwatch.StartNew();
                    var detail = Build(v.mesh, surfaces, v.scale, .01f, budgetArg, out float spacing);
                    var coarse = Build(v.mesh, surfaces, v.scale, .09f, 24000, out _);
                    watch.Stop(); seconds += watch.Elapsed.TotalSeconds;
                    long bytes = Bytes(detail) + Bytes(coarse);
                    long triangles = Enumerable.Range(0, detail.subMeshCount).Sum(s => (long)detail.GetIndexCount(s) / 3);
                    var uvs = new List<Vector2>(); detail.GetUVs(0, uvs);
                    var own = new List<float>();
                    for (int s = 0; s < detail.subMeshCount; s++)
                    {
                        if (!v.displaced[s]) continue;
                        var tris = detail.GetTriangles(s);
                        for (int i = 0; i < tris.Length; i += 3)
                        {
                            Vector2 a = uvs[tris[i]], b = uvs[tris[i + 1]], c = uvs[tris[i + 2]];
                            float ha = data.height(a), hb = data.height(b), hc = data.height(c);
                            own.Add(Mathf.Abs(data.height((a + b + c) / 3) - (ha + hb + hc) / 3));
                            own.Add(Mathf.Abs(data.height((a + b) / 2) - (ha + hb) / 2));
                            own.Add(Mathf.Abs(data.height((b + c) / 2) - (hb + hc) / 2));
                        }
                    }
                    own.Sort(); errors.AddRange(own);
                    sampleBytes += bytes; sampleTriangles += triangles;
                    lines.Add($"BUILD tol {tolerance * 1000:F1} mm {v.mesh.name} x{v.scale.x:F2}: {v.instances} inst, {v.area:F1} m2 -> {triangles:N0} tris, min edge {spacing * 100:F1} cm " +
                              $"(coarse {Enumerable.Range(0, coarse.subMeshCount).Sum(s => (long)coarse.GetIndexCount(s) / 3):N0}), {bytes / 1048576f:F1} MB, {watch.Elapsed.TotalSeconds:F1} s, " +
                              $"error p50 {own[own.Count / 2] * 1000:F2} p95 {own[own.Count * 95 / 100] * 1000:F2} p99 {own[own.Count * 99 / 100] * 1000:F2} mm");
                    Object.DestroyImmediate(detail); Object.DestroyImmediate(coarse);
                }
                errors.Sort();
                lines.Insert(1, $"tol {tolerance * 1000:F1} mm, budget {budgetArg}: sample {sample.Count} variants -> {sampleTriangles:N0} tris, {sampleBytes / 1048576f:F0} MB, {seconds:F0} s; " +
                                $"estimate all {variants.Count}: {sampleBytes / 1048576f / sample.Count * variants.Count:F0} MB, {seconds / sample.Count * variants.Count / 60:F0} min; " +
                                $"error p95 {errors[errors.Count * 95 / 100] * 1000:F2} p99 {errors[errors.Count * 99 / 100] * 1000:F2} mm");
            }
            float totalArea = variants.Values.Sum(v => v.area);
            int totalInstances = variants.Values.Sum(v => v.instances);
            lines.Insert(0, $"dungeonbase: {totalInstances} instances, {variants.Count} variants, unique relief {totalArea:F0} m2");
            var small = variants.Values.Where(v => v.area < 2).ToList();
            lines.Insert(1, $"variants by area: <2 m2 {small.Count}, 2-8 m2 {variants.Values.Count(v => v.area >= 2 && v.area < 8)}, >=8 m2 {variants.Values.Count(v => v.area >= 8)}");
            foreach (var v in variants.Values.OrderByDescending(v => v.instances).Take(30))
                lines.Add($"  {v.mesh.name} scale {v.scale.x:F2},{v.scale.y:F2},{v.scale.z:F2}: {v.instances} inst on {v.maps.Count} maps, {v.area:F1} m2");
            string report = Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief", "Dungeon", "Budget.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            File.WriteAllLines(report, lines);
            UnityEngine.Debug.Log("[DungeonReliefBudget] " + string.Join("\n", lines.Take(4)));
        }

        [Serializable] class ApprovedReliefProfilesInfo { public bool over_creases; }

        static Mesh Build(Mesh mesh, HplReliefMeshBuilder.Surface[] surfaces, Vector3 scale, float spacing, int budget, out float used)
        {
            for (int i = 0; ; i++)
            {
                try { used = spacing; return HplReliefMeshBuilder.Build(mesh, surfaces, scale, spacing, budget); }
                catch (InvalidOperationException e) when (e.Message.StartsWith("Relief triangle budget:", StringComparison.Ordinal) && i < 8) { spacing *= 1.5f; }
            }
        }

        static long Bytes(Mesh mesh)
        {
            long vertex = 0;
            for (int s = 0; s < mesh.vertexBufferCount; s++) vertex += (long)mesh.GetVertexBufferStride(s) * mesh.vertexCount;
            long index = Enumerable.Range(0, mesh.subMeshCount).Sum(s => (long)mesh.GetIndexCount(s)) * (mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32 ? 4 : 2);
            return vertex + index;
        }
    }
}
