using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Finds enhanced (PBRify) materials that lost the relief or the gloss of the classic one, like the flesh `spiderdetails`
    /// (bug report 2026-11-21 10-48-37; docs/EnhancedTextures.md, «Проверка поверхности»). For every catalog pair the normal
    /// and mask maps are read on the GPU at 1024² (mips, as HDRP sees them at a distance; in-memory shader <see cref="BlitShader"/>) and compared:
    /// relief — mean tilt of the tangent normal × _NormalScale (with the classic detail normal; POM materials too); gloss — specular peak F0·D_GGX(smoothness), weighted by the
    /// classic F0. A material is suspect when the enhanced relief is under <see cref="ReliefLoss"/> of the classic one or
    /// the enhanced peak is under <see cref="ShineLoss"/> of a glossy classic (not for matte-by-rule materials). Then the level scenes are searched for a
    /// surface with each suspect and a player spot looking at it (feet on the floor, line of sight by the colliders).
    /// Output: Logs/SurfaceAudit/report.txt and views.tsv (map, view, feet and target in .map coordinates) for
    /// tools/Run-SurfaceAuditShots.ps1. Batch: -batchmode -quit (GPU: without -nographics); -auditTop n (default 24).
    /// </summary>
    public static class SurfaceAudit
    {
        public const float ReliefLoss = 0.3f, ShineLoss = 0.4f;
        const float MinClassicTilt = 0.08f, MinClassicGloss = 0.3f, EyeHeight = 1.6f;
        const int Size = 1024;
        static string Folder => Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "SurfaceAudit");
        static Material s_blit;
        /// <summary>Reads a texture at a chosen mip into a float target as HDRP/Lit sees it: pass 0 — tangent normal xy
        /// (UnpackNormalmapRGorAG: x *= w, for BC5, DXT5nm and RGB), pass 1 — raw sample (mask, specular, albedo alpha).</summary>
        const string BlitShader = @"Shader ""Hidden/RTXMnesia/SurfaceAudit""
{
    Properties { _MainTex (""Texture"", 2D) = ""white"" {} _Lod (""Lod"", Float) = 0 }
    SubShader
    {
        ZTest Always Cull Off ZWrite Off
        CGINCLUDE
        #include ""UnityCG.cginc""
        sampler2D _MainTex;
        float _Lod;
        struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
        v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }
        ENDCG
        // 0: tangent normal xy as HDRP unpacks it (UnpackNormalmapRGorAG: x *= w, works for BC5, DXT5nm and RGB).
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i) : SV_Target
            {
                float4 c = tex2Dlod(_MainTex, float4(i.uv, 0, _Lod));
                c.x *= c.w;
                float2 xy = c.xy * 2 - 1;
                return float4(xy, 0, 1);
            }
            ENDCG
        }
        // 1: raw sample (mask, specular colour, albedo alpha).
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i) : SV_Target { return tex2Dlod(_MainTex, float4(i.uv, 0, _Lod)); }
            ENDCG
        }
    }
}";

        sealed class Row
        {
            public string relative;
            public Material classic;
            public bool classicSurface, cutout, height, matte;
            public float tiltC, tiltE, glossC, glossE, shineC, shineE, f0, detail;
            public float Relief => tiltC > 1e-4f ? tiltE / tiltC : 1f;
            public float Shine => shineC > 1e-4f ? shineE / shineC : 1f;
            // POM shifts the texture but not the shading normal: flat-lit faces read flat whatever the parallax (reports
            // 2026-11-27, 04_wine_cellar_lab / 03_archives), so POM materials are compared too. The enhanced tilt includes the
            // classic detail normal (EnhancedClassicDetail) blended as HDRP does.
            public bool ReliefSuspect => tiltC >= MinClassicTilt && Relief < ReliefLoss;
            // Matte materials (fabrics, no Specular map) are matte on purpose (docs/EnhancedTextures.md, «Матовые материалы»).
            public bool ShineSuspect => !matte && glossC >= MinClassicGloss && Shine < ShineLoss;
            public bool Suspect => ReliefSuspect || ShineSuspect;
            // Order: both lost (as the flesh), gloss lost, relief lost; inside a group the stronger loss first.
            public int Group => ReliefSuspect && ShineSuspect ? 0 : ShineSuspect ? 1 : 2;
            public float Score => Mathf.Min(ReliefSuspect ? Relief / ReliefLoss : 9f, ShineSuspect ? Shine / ShineLoss : 9f);
            public int scenes, renderers;
            public string map, view, place;
        }

        [MenuItem("Tools/RTXMnesia/Dev/Surface audit (enhanced relief and gloss)")]
        public static void RunBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-auditTop");
            int top = at >= 0 ? int.Parse(args[at + 1], CultureInfo.InvariantCulture) : 24;
            Directory.CreateDirectory(Folder);
            // In memory only, for this run: with this shader as a project asset the editor GPU device was removed (887a0006)
            // on every RT view with the classic textures (2026-11-22, both probes; without the file they pass).
            var shader = ShaderUtil.CreateShaderAsset(BlitShader, false);
            s_blit = new Material(shader);
            var catalog = AssetDatabase.LoadAssetAtPath<HplTextureSet>(EnhancedMaterialConverter.CatalogPath);
            var rows = new List<Row>();
            int n = 0;
            foreach (var entry in catalog.entries)
            {
                if (entry.classic == null || string.IsNullOrEmpty(entry.enhancedResource)) continue;
                var enhanced = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + entry.enhancedResource + ".mat");
                if (enhanced == null) continue;
                string relative = AssetDatabase.GetAssetPath(entry.classic).Substring(AmnesiaPaths.MaterialsRoot.Length + 1);
                rows.Add(Measure(relative, entry.classic, enhanced));
                if (++n % 50 == 0) { EditorUtility.UnloadUnusedAssetsImmediate(); Debug.Log($"[SurfaceAudit] measured {n}/{catalog.entries.Length}"); }
            }
            var suspects = rows.Where(r => r.Suspect && !r.classicSurface).OrderBy(r => r.Group).ThenBy(r => r.Score).ToList();
            Locate(suspects);
            WriteReport(rows, suspects, top);
            UnityEngine.Object.DestroyImmediate(s_blit);
            UnityEngine.Object.DestroyImmediate(shader);
            Debug.Log($"[SurfaceAudit] PASS: {rows.Count} pairs, {suspects.Count} suspects, {suspects.Count(s => s.view != null)} with a place; Logs/SurfaceAudit/report.txt");
        }

        static Row Measure(string relative, Material classic, Material enhanced)
        {
            var row = new Row { relative = relative, classic = classic, classicSurface = EnhancedMaterialConverter.ClassicSurface(relative) };
            row.cutout = classic.GetFloat("_AlphaCutoffEnable") > 0;
            Color[] alpha = row.cutout && classic.GetTexture("_BaseColorMap") != null ? Read(classic.GetTexture("_BaseColorMap"), 1) : null;
            float cutoff = classic.GetFloat("_AlphaCutoff");
            bool Covered(int i) => alpha == null || alpha[i].a >= cutoff;
            float[] f0 = SpecularF0(classic);
            row.f0 = Mean(f0, Covered);

            Texture normalE = enhanced.GetTexture("_NormalMap"), maskE = enhanced.GetTexture("_MaskMap");
            float remapMaxE = enhanced.GetFloat("_SmoothnessRemapMax"), remapMinE = enhanced.GetFloat("_SmoothnessRemapMin");
            if (row.classicSurface)
            {
                // The PBRify maps this material would have without the rule (calibration: the flesh must come out suspect).
                string albedo = AssetDatabase.GetAssetPath(enhanced.GetTexture("_BaseColorMap"));
                normalE = AssetDatabase.LoadAssetAtPath<Texture2D>(albedo.Replace("__albedo", "__normal"));
                maskE = AssetDatabase.LoadAssetAtPath<Texture2D>(albedo.Replace("__albedo", "__mask"));
                remapMinE = 0f; remapMaxE = 1f;
            }
            else
            {
                row.height = enhanced.GetTexture("_HeightMap") != null && enhanced.GetFloat("_DisplacementMode") > 0;
                row.matte = remapMaxE <= EnhancedMaterialConverter.MatteSmoothnessMax + 1e-3f;
            }
            row.tiltC = Tilt(classic.GetTexture("_NormalMap"), classic.GetFloat("_NormalScale"), Covered);
            var detail = row.classicSurface ? null : enhanced.GetTexture("_DetailMap");
            row.detail = detail != null ? enhanced.GetFloat("_DetailNormalScale") : 0f;
            row.tiltE = Tilt(normalE, row.classicSurface ? 1f : enhanced.GetFloat("_NormalScale"), Covered, detail, row.detail);
            float[] sC = Smoothness(classic, classic.GetTexture("_MaskMap"), classic.GetFloat("_SmoothnessRemapMin"), classic.GetFloat("_SmoothnessRemapMax"));
            float[] sE = Smoothness(enhanced, maskE, remapMinE, remapMaxE);
            (row.glossC, row.shineC) = Gloss(sC, f0, Covered);
            (row.glossE, row.shineE) = Gloss(sE, f0, Covered);
            return row;
        }

        static Color[] Read(Texture texture, int pass)
        {
            var rt = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            s_blit.SetFloat("_Lod", Mathf.Max(0f, Mathf.Log(texture.width / (float)Size, 2f)));
            Graphics.Blit(texture, rt, s_blit, pass);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var read = new Texture2D(Size, Size, TextureFormat.RGBAFloat, false, true);
            read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            var pixels = read.GetPixels();
            UnityEngine.Object.DestroyImmediate(read);
            return pixels;
        }

        static float Tilt(Texture normal, float scale, Func<int, bool> covered, Texture detail = null, float detailScale = 0f)
        {
            if (normal == null) return 0f;
            var p = Read(normal, 0);
            // HDRP detail map: x = A, y = G (UnpackNormalAG), mask 1 (PBRify Mask B).
            var d = detail != null && detailScale > 0f ? Read(detail, 1) : null;
            double sum = 0; int count = 0;
            for (int i = 0; i < p.Length; i++)
            {
                if (!covered(i)) continue;
                var xy = new Vector2(p[i].r, p[i].g) * scale;
                if (d != null) xy = BlendRnm(xy, new Vector2(d[i].a * 2f - 1f, d[i].g * 2f - 1f) * detailScale);
                sum += Mathf.Min(1f, xy.magnitude); count++;
            }
            return count > 0 ? (float)(sum / count) : 0f;
        }

        /// <summary>BlendNormalRNM of HDRP (CommonMaterial.hlsl) on unpacked xy, z reconstructed; returns the result's xy / z.</summary>
        static Vector2 BlendRnm(Vector2 a, Vector2 b)
        {
            var n1 = new Vector3(a.x, a.y, Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Min(1f, a.sqrMagnitude))));
            var n2 = new Vector3(b.x, b.y, Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Min(1f, b.sqrMagnitude)))).normalized;
            var t = n1 + Vector3.forward;
            var u = new Vector3(-n2.x, -n2.y, n2.z);
            var r = (t * Vector3.Dot(t, u) / t.z - u).normalized;
            return new Vector2(r.x, r.y);
        }

        static float[] SpecularF0(Material m)
        {
            var color = m.GetColor("_SpecularColor");
            float k = (color.r + color.g + color.b) / 3f;
            var map = m.GetTexture("_SpecularColorMap");
            var f0 = new float[Size * Size];
            if (map == null) { for (int i = 0; i < f0.Length; i++) f0[i] = k; return f0; }
            var p = Read(map, 1);
            for (int i = 0; i < f0.Length; i++) f0[i] = k * (p[i].r + p[i].g + p[i].b) / 3f;
            return f0;
        }

        static float[] Smoothness(Material m, Texture mask, float min, float max)
        {
            var s = new float[Size * Size];
            if (mask == null) { float v = m.GetFloat("_Smoothness"); for (int i = 0; i < s.Length; i++) s[i] = v; return s; }
            var p = Read(mask, 1);
            for (int i = 0; i < s.Length; i++) s[i] = Mathf.Lerp(min, max, p[i].a);
            return s;
        }

        /// <summary>F0-weighted mean smoothness and the mean GGX peak F0·D(n·h = 1) = F0 / (π α²), α = (1 − s)².</summary>
        static (float gloss, float shine) Gloss(float[] s, float[] f0, Func<int, bool> covered)
        {
            double ws = 0, w = 0, shine = 0; int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (!covered(i)) continue;
                float a = Mathf.Max(0.03f, (1f - s[i]) * (1f - s[i]));
                ws += f0[i] * s[i]; w += f0[i];
                shine += f0[i] / (Mathf.PI * a * a);
                count++;
            }
            return (w > 0 ? (float)(ws / w) : 0f, count > 0 ? (float)(shine / count) : 0f);
        }

        static float Mean(float[] v, Func<int, bool> covered)
        {
            double sum = 0; int count = 0;
            for (int i = 0; i < v.Length; i++) if (covered(i)) { sum += v[i]; count++; }
            return count > 0 ? (float)(sum / count) : 0f;
        }

        static void Locate(List<Row> suspects)
        {
            if (suspects.Count == 0) return;
            var byMaterial = suspects.ToDictionary(r => r.classic);
            string levelRoot = AmnesiaPaths.ImportedAssetsRoot + "/Levels";
            var scenes = Directory.GetFiles(AmnesiaPaths.ToAbsolute(levelRoot), "*.unity", SearchOption.AllDirectories)
                .Select(p => Path.GetRelativePath(AmnesiaPaths.ToAbsolute(levelRoot), p).Replace('\\', '/'))
                .Where(p => p.StartsWith("maps/main/") || p.StartsWith("maps/ptest/")).OrderBy(p => p, StringComparer.Ordinal).ToList();
            var usedScenes = new Dictionary<Row, HashSet<string>>();
            foreach (string relative in scenes)
            {
                EditorSceneManager.OpenScene(levelRoot + "/" + relative, OpenSceneMode.Single);
                Physics.SyncTransforms();
                string map = Path.ChangeExtension(relative, ".map");
                foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    var materials = renderer.sharedMaterials;
                    for (int sub = 0; sub < materials.Length; sub++)
                    {
                        if (materials[sub] == null || !byMaterial.TryGetValue(materials[sub], out var row)) continue;
                        row.renderers++;
                        if (!usedScenes.TryGetValue(row, out var set)) usedScenes[row] = set = new HashSet<string>();
                        set.Add(map);
                        if (row.view != null || filter == null || filter.sharedMesh == null || sub >= filter.sharedMesh.subMeshCount) continue;
                        if (FindView(renderer, filter.sharedMesh, sub, out var feet, out var target))
                        {
                            row.map = map;
                            row.view = "s" + (suspects.IndexOf(row) + 1).ToString("00") + "_" + Safe(Path.GetFileNameWithoutExtension(row.relative));
                            row.place = $"{F(HplSpace.Position(feet))} {F(HplSpace.Position(target))}";
                        }
                    }
                }
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
            foreach (var p in usedScenes) p.Key.scenes = p.Value.Count;
        }

        static bool FindView(MeshRenderer renderer, Mesh mesh, int sub, out Vector3 feet, out Vector3 target)
        {
            feet = target = default;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.GetTriangles(sub);
            if (vertices.Length == 0 || triangles.Length == 0) return false;
            var world = renderer.localToWorldMatrix;
            var normalMatrix = world.inverse.transpose;
            var order = Enumerable.Range(0, triangles.Length / 3).Select(t =>
            {
                Vector3 a = world.MultiplyPoint3x4(vertices[triangles[t * 3]]), b = world.MultiplyPoint3x4(vertices[triangles[t * 3 + 1]]),
                        c = world.MultiplyPoint3x4(vertices[triangles[t * 3 + 2]]);
                return (t, area: Vector3.Cross(b - a, c - a).magnitude * 0.5f, centre: (a + b + c) / 3f);
            }).Where(x => x.area > 0.01f).OrderByDescending(x => x.area).Take(6);
            foreach (var tri in order)
            {
                Vector3 n = normals.Length == vertices.Length
                    ? normalMatrix.MultiplyVector(normals[triangles[tri.t * 3]] + normals[triangles[tri.t * 3 + 1]] + normals[triangles[tri.t * 3 + 2]]).normalized
                    : Vector3.up;
                Vector3 p = tri.centre + n * 0.01f;
                var flat = new Vector3(n.x, 0f, n.z);
                var dirs = flat.magnitude > 0.3f
                    ? new[] { 0f, 35f, -35f }.Select(a => Quaternion.Euler(0f, a, 0f) * flat.normalized)
                    : Enumerable.Range(0, 8).Select(k => Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward);
                foreach (float d in new[] { 1.2f, 1.7f, 2.4f, 3.2f })
                    foreach (var dir in dirs)
                    {
                        Vector3 probe = p + dir * d;
                        probe.y = Mathf.Min(probe.y, p.y + 1f) + 0.5f;
                        if (!Physics.Raycast(probe, Vector3.down, out var floor, 4.5f, ~0, QueryTriggerInteraction.Ignore) || floor.normal.y < 0.7f) continue;
                        Vector3 eye = floor.point + Vector3.up * EyeHeight;
                        if (Physics.CheckCapsule(floor.point + Vector3.up * 0.45f, eye, 0.3f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        Vector3 toEye = eye - p;
                        float dist = toEye.magnitude;
                        if (dist > 4.5f || Vector3.Dot(n, toEye / dist) < 0.3f) continue;
                        if (Physics.Raycast(eye, -toEye / dist, out var hit, dist - 0.08f, ~0, QueryTriggerInteraction.Ignore) &&
                            !hit.collider.transform.IsChildOf(renderer.transform) && !(renderer.transform.parent != null && hit.collider.transform.IsChildOf(renderer.transform.parent)) &&
                            Vector3.Distance(hit.point, p) > 0.35f) continue;
                        feet = floor.point;
                        target = tri.centre;
                        return true;
                    }
            }
            return false;
        }

        static void WriteReport(List<Row> rows, List<Row> suspects, int top)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Surface audit {DateTime.Now:yyyy-MM-dd HH:mm}: {rows.Count} enhanced pairs, maps read at {Size}² (mips).");
            sb.AppendLine($"relief = mean normal tilt enhanced / classic (suspect < {ReliefLoss} when classic tilt >= {MinClassicTilt});");
            sb.AppendLine($"shine = mean GGX peak F0·D enhanced / classic (suspect < {ShineLoss} when classic gloss >= {MinClassicGloss}).");
            sb.AppendLine();
            sb.AppendLine("Calibration (ClassicSurfaceMaterials, measured with their PBRify maps):");
            foreach (var r in rows.Where(r => r.classicSurface)) sb.AppendLine("  " + Line(r));
            sb.AppendLine();
            sb.AppendLine($"Suspects: {suspects.Count} (relief and gloss {suspects.Count(r => r.Group == 0)}, gloss {suspects.Count(r => r.Group == 1)}, relief {suspects.Count(r => r.Group == 2)}), worst first; shots for the first {top} with a place:");
            foreach (var r in suspects)
                sb.AppendLine("  " + Line(r) + $" | scenes {r.scenes}, renderers {r.renderers}" + (r.view != null ? $" | {r.view} @ {r.map}: {r.place}" : " | no place found"));
            sb.AppendLine();
            sb.AppendLine("Strongest gains (enhanced much bumpier or glossier; information only):");
            foreach (var r in rows.Where(r => !r.classicSurface && (r.Relief > 3f && r.tiltE > 0.15f || r.Shine > 5f && r.glossE > 0.5f))
                         .OrderByDescending(r => Mathf.Max(r.Relief, r.Shine / 2f)).Take(25))
                sb.AppendLine("  " + Line(r));
            File.WriteAllText(Path.Combine(Folder, "report.txt"), sb.ToString());
            File.WriteAllLines(Path.Combine(Folder, "views.tsv"), suspects.Where(r => r.view != null).Take(top).Select(r => $"{r.map}\t{r.view}\t{r.place}\t{r.relative}"));
        }

        static string Line(Row r) =>
            $"{r.relative}: relief {r.Relief:0.00} (tilt {r.tiltC:0.000} → {r.tiltE:0.000}){(r.ReliefSuspect ? " LOSS" : "")}, " +
            $"shine {r.Shine:0.00} (gloss {r.glossC:0.00} → {r.glossE:0.00}, F0 {r.f0:0.0000}){(r.ShineSuspect ? " LOSS" : "")}{(r.cutout ? ", cutout" : "")}{(r.height ? ", POM" : "")}{(r.detail > 0f ? $", classic detail {r.detail:0.00}" : "")}{(r.matte ? ", matte" : "")}";

        static string F(Vector3 v) => string.Join(" ", new[] { v.x, v.y, v.z }.Select(x => x.ToString("0.00", CultureInfo.InvariantCulture)));
        static string Safe(string s) => new string(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
    }
}
