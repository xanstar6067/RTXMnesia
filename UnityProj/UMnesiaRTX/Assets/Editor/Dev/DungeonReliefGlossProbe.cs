using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RTXMnesia.Importer;
using RTXMnesia.World;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// The "not glass" yardstick of the dungeon relief: dev_room samples under one raking light, the same distance and angles
    /// for each — the approved castle wall (stand sample 5) and the four dungeonbase samples. The specular part of a pixel is the
    /// frame minus the frame with the specular lobe removed (the sample's materials as LitSpecular with F0 0; in the RT mode
    /// this also removes the ray-traced reflections), in a linear float buffer. Faces and joints come from each material's
    /// stone_mask.png (tools/Build-ManualRelief.py) drawn on the sample with the same UVs. Raster and RT reflections "Medium".
    /// Report and small previews: Logs/EnhancedRelief/Dungeon/Gloss. The scene is not saved.
    /// Batch: -batchmode -quit (no -nographics), -executeMethod RTXMnesia.EditorDev.DungeonReliefGlossProbe.RunBatch.
    /// </summary>
    public static class DungeonReliefGlossProbe
    {
        const int Width = 960, Height = 540;
        static string Output => Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief", "Dungeon", "Gloss");

        sealed class Sample
        {
            public string name;
            public Renderer[] renderers;
            public Func<Material, bool> measured;
            public Texture2D stoneMask;
            public Vector3[] points;
            public string[] pointNames;
        }

        public static void RunBatch()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("The probe renders: run without -nographics.");
            Directory.CreateDirectory(Output);
            var scene = EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
            var created = new List<Object>();
            var report = new StringBuilder();
            try
            {
                var samples = FindSamples(created);
                foreach (var light in Object.FindObjectsByType<AmnesiaLight>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) light.gameObject.SetActive(false);
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) light.enabled = false;

                var camera = new GameObject("Gloss probe camera").AddComponent<Camera>(); created.Add(camera.gameObject);
                camera.nearClipPlane = .02f; camera.farClipPlane = 60; camera.fieldOfView = 30;
                var data = camera.gameObject.AddComponent<HDAdditionalCameraData>();
                data.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                var lamp = new GameObject("Gloss probe light").AddComponent<Light>(); created.Add(lamp.gameObject);
                lamp.type = LightType.Spot; lamp.range = 10; lamp.spotAngle = 90; lamp.shadows = LightShadows.Soft;
                lamp.gameObject.AddComponent<HDAdditionalLightData>(); lamp.lightUnit = LightUnit.Candela; lamp.intensity = 150;
                var volume = ReflectionVolume(created);

                // Texture upload as the editor snapshots do (ImportMenu.RenderToFile), and an overview of the stand.
                camera.transform.position = new Vector3(DungeonReliefDevRoomStand.FirstX + DungeonReliefDevRoomStand.Step * 1.5f, 2.2f, 7.5f);
                camera.transform.LookAt(new Vector3(camera.transform.position.x, 2, DungeonReliefDevRoomStand.FrontWall));
                lamp.transform.position = camera.transform.position; lamp.transform.rotation = camera.transform.rotation;
                camera.fieldOfView = 60;
                ImportMenu.RenderToFile(camera, Path.Combine(Output, "overview.png"));
                camera.fieldOfView = 30;

                report.AppendLine("Gloss probe " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                report.AppendLine("light: spot 150 cd at P + (-0.5, 0.9, 0.35) (raking from the upper left inside the module niche, ~19 deg; the dungeon 'high' point under the lintel: y mirrored); views: front P + (0, 0, 1.4), glancing P + (0.45, -0.7, 0.55) (near the mirror direction); FOV 30");
                report.AppendLine("columns: diffuse mean | specular mean | specular p95 | specular / diffuse, faces (stone_mask > 0.75) and joints (< 0.25), luminance in exposed linear units x 1000");
                foreach (bool rt in new[] { false, true })
                {
                    volume.SetActive(rt);
                    data.customRenderingSettings = true;
                    SetFrame(data, FrameSettingsField.RayTracing, rt);
                    SetFrame(data, FrameSettingsField.SSR, rt);
                    report.AppendLine(rt ? "\n== RT reflections (Medium)" : "\n== Raster");
                    foreach (var sample in samples)
                        for (int p = 0; p < sample.points.Length; p++)
                        {
                            var point = sample.points[p];
                            // The upper point is under the module's lintel: the light comes from below there.
                            float up = sample.pointNames[p] == "high" ? -1 : 1;
                            lamp.transform.position = point + new Vector3(-.5f, .9f * up, .35f); lamp.transform.LookAt(point);
                            foreach (var (view, offset) in new[] { ("front", new Vector3(0, 0, 1.4f)), ("glancing", new Vector3(.45f, -.7f * up, .55f)) })
                            {
                                camera.transform.position = point + offset; camera.transform.LookAt(point);
                                string label = $"{sample.name} {sample.pointNames[p]} {view}";
                                var line = Measure(camera, sample, rt ? 24 : 6, Path.Combine(Output, $"{sample.name}_{sample.pointNames[p]}_{view}_{(rt ? "rt" : "raster")}.png"), created);
                                report.AppendLine($"{label,-36} {line}");
                            }
                        }
                }
                report.AppendLine("\nMaterials:");
                foreach (var sample in samples)
                    foreach (var material in sample.renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null && sample.measured(m)).Distinct())
                        report.AppendLine($"{sample.name,-12} {material.name}: MaterialID {material.GetFloat("_MaterialID")}, F0 {(material.GetFloat("_MaterialID") == (float)MaterialId.LitSpecular ? material.GetColor("_SpecularColor").linear.r.ToString("0.####") + " (linear) x map" : "0.04")}, " +
                            $"smoothness remap {material.GetFloat("_SmoothnessRemapMin")}..{material.GetFloat("_SmoothnessRemapMax")}, AO remap {material.GetFloat("_AORemapMin")}..{material.GetFloat("_AORemapMax")}, " +
                            $"displacement {material.GetFloat("_DisplacementMode")}, mask {material.GetTexture("_MaskMap")?.name}");
                File.WriteAllText(Path.Combine(Output, "report.txt"), report.ToString());
                Debug.Log("[gloss probe]\n" + report);
            }
            finally
            {
                foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            }
        }

        static List<Sample> FindSamples(List<Object> created)
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Transform Find(string name) => all.FirstOrDefault(t => t.name == name) ?? throw new InvalidOperationException("Missing dev_room sample: " + name);
            Texture2D Mask(string material)
            {
                string file = Path.Combine(ManualWallReliefProbe.MapsFor(material), "stone_mask.png");
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(file), false)) throw new InvalidOperationException("Cannot read " + file);
                created.Add(texture);
                return texture;
            }
            var samples = new List<Sample>();
            // Stand sample 5: the approved castle wall at the game's 5.1 cm spacing, plane z 1.2, facing +Z.
            var castle = Find("castlebase_wall MANUAL wall_game");
            samples.Add(new Sample { name = "castle5", renderers = castle.GetComponentsInChildren<Renderer>(), measured = _ => true,
                stoneMask = Mask("static_objects/castlebase/wall/castlebase_wall"), points = new[] { new Vector3(castle.position.x, castle.position.y, castle.position.z) },
                pointNames = new[] { "mid" } });
            var dungeonMask = Mask("static_objects/dungeonbase/dungeonbase");
            static bool Atlas(Material m) => m.GetTexture("_BaseColorMap")?.name.StartsWith("dungeonbase__", StringComparison.Ordinal) == true;
            Vector3[] points = null;
            foreach (string file in DungeonReliefDevRoomStand.Files)
            {
                var module = Find(DungeonReliefDevRoomStand.ModulePrefix + file);
                var renderers = module.GetComponentsInChildren<Renderer>();
                if (points == null)
                {
                    // The classic module's surface at its centre line: lower rubble and the upper frame.
                    var colliders = renderers.Select(r => { var c = r.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = r.GetComponent<MeshFilter>().sharedMesh; return c; }).ToList();
                    Physics.SyncTransforms();
                    float x = renderers.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; }).center.x;
                    points = new[] { 1f, 3.1f }.Select(y => Physics.Raycast(new Vector3(x, y, 6), Vector3.back, out var hit, 10) ? hit.point
                        : throw new InvalidOperationException("No module surface at y " + y)).ToArray();
                    foreach (var c in colliders) Object.DestroyImmediate(c);
                }
                float dx = module.position.x - Find(DungeonReliefDevRoomStand.ModulePrefix + "classic").position.x;
                samples.Add(new Sample { name = "dungeon_" + file, renderers = renderers, measured = Atlas, stoneMask = dungeonMask,
                    points = points.Select(p => p + Vector3.right * dx).ToArray(), pointNames = new[] { "low", "high" } });
            }
            return samples;
        }

        static GameObject ReflectionVolume(List<Object> created)
        {
            var go = new GameObject("Gloss probe RT reflections"); created.Add(go);
            var volume = go.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 3000;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); created.Add(profile);
            volume.sharedProfile = profile;
            // As HplRayTracing.ApplyVolume, reflections "Medium".
            var reflection = profile.Add<ScreenSpaceReflection>();
            reflection.SetAllOverridesTo(true);
            reflection.enabled.Override(true);
            reflection.tracing.Override(RayCastingMode.RayTracing);
            reflection.mode.Override(RayTracingMode.Performance);
            reflection.quality.Override(ScalableSettingLevelParameter.LevelCount);
            reflection.minSmoothness = .45f; reflection.smoothnessFadeStart = .55f; reflection.rayLength = 25;
            reflection.fullResolution = true; reflection.denoise = true; reflection.denoiserRadius = .75f;
            go.SetActive(false);
            return go;
        }

        static void SetFrame(HDAdditionalCameraData data, FrameSettingsField field, bool on)
        {
            data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
            data.renderingPathCustomFrameSettings.SetEnabled(field, on);
        }

        /// <summary>Full frame, frame without specular, mask and coverage of the sample → one report line.</summary>
        static string Measure(Camera camera, Sample sample, int frames, string preview, List<Object> created)
        {
            var original = sample.renderers.Select(r => r.sharedMaterials).ToArray();
            var full = Render(camera, frames);
            var noSpecular = new Dictionary<Material, Material>();
            Material Diffuse(Material m)
            {
                if (m == null) return null;
                if (noSpecular.TryGetValue(m, out var found)) return found;
                var copy = new Material(m) { name = m.name + " no specular" };
                copy.SetFloat("_MaterialID", (float)MaterialId.LitSpecular);
                copy.SetColor("_SpecularColor", Color.black); copy.SetTexture("_SpecularColorMap", null);
                HDMaterial.ValidateMaterial(copy);
                created.Add(copy);
                return noSpecular[m] = copy;
            }
            for (int i = 0; i < sample.renderers.Length; i++) sample.renderers[i].sharedMaterials = original[i].Select(Diffuse).ToArray();
            var diffuse = Render(camera, frames);

            // Mask and coverage: only the sample, unlit; the measured materials show the stone mask, the rest stays black.
            var hidden = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.enabled && !sample.renderers.Contains(r)).ToList();
            foreach (var r in hidden) r.enabled = false;
            Material Unlit(Material m, Texture texture, Color color)
            {
                var unlit = new Material(Shader.Find("HDRP/Unlit")); created.Add(unlit);
                unlit.SetTexture("_UnlitColorMap", texture); unlit.SetColor("_UnlitColor", color);
                if (m != null && m.HasProperty("_BaseColorMap")) unlit.SetTextureScale("_UnlitColorMap", m.GetTextureScale("_BaseColorMap"));
                if (m != null && m.HasProperty("_BaseColorMap")) unlit.SetTextureOffset("_UnlitColorMap", m.GetTextureOffset("_BaseColorMap"));
                HDMaterial.ValidateMaterial(unlit);
                return unlit;
            }
            for (int i = 0; i < sample.renderers.Length; i++)
                sample.renderers[i].sharedMaterials = original[i].Select(m => m != null && sample.measured(m) ? Unlit(m, sample.stoneMask, Color.white) : Unlit(m, null, Color.black)).ToArray();
            var mask = Render(camera, 2);
            for (int i = 0; i < sample.renderers.Length; i++)
                sample.renderers[i].sharedMaterials = original[i].Select(m => m != null && sample.measured(m) ? Unlit(m, null, Color.white) : Unlit(m, null, Color.black)).ToArray();
            var coverage = Render(camera, 2);
            foreach (var r in hidden) r.enabled = true;
            for (int i = 0; i < sample.renderers.Length; i++) sample.renderers[i].sharedMaterials = original[i];

            static float Y(float[] c, int p) => .2126f * c[p * 4] + .7152f * c[p * 4 + 1] + .0722f * c[p * 4 + 2];
            var faces = new List<(float d, float s)>(); var joints = new List<(float d, float s)>();
            for (int p = 0; p < Width * Height; p++)
            {
                if (coverage[p * 4] < .5f) continue;
                float m = mask[p * 4], d = Y(diffuse, p), s = Mathf.Max(0, Y(full, p) - d);
                if (m > .75f) faces.Add((d, s)); else if (m < .25f) joints.Add((d, s));
            }
            SavePreview(full, preview);
            string Stats(List<(float d, float s)> values)
            {
                if (values.Count == 0) return "no pixels".PadRight(36);
                var spec = values.Select(v => v.s).OrderBy(v => v).ToArray();
                float dm = values.Average(v => v.d), sm = spec.Average(), p95 = spec[(int)(spec.Length * .95f)];
                return $"{dm * 1000,6:0.0} {sm * 1000,6:0.00} {p95 * 1000,6:0.00} {(dm > 0 ? sm / dm * 100 : 0),5:0.0}% ({values.Count,6} px)";
            }
            return "faces " + Stats(faces) + " | joints " + Stats(joints);
        }

        static float[] Render(Camera camera, int frames)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            camera.targetTexture = rt;
            try
            {
                for (int i = 0; i < frames; i++)
                {
                    camera.Render();
                    AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                }
                var request = AsyncGPUReadback.Request(rt);
                request.WaitForCompletion();
                if (request.hasError) throw new InvalidOperationException("Readback failed.");
                var halves = request.GetData<ushort>();
                var result = new float[halves.Length];
                for (int i = 0; i < halves.Length; i++) result[i] = Mathf.HalfToFloat(halves[i]);
                return result;
            }
            finally { camera.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); }
        }

        /// <summary>A small display-referred preview: value^(1/2.2), clamped.</summary>
        static void SavePreview(float[] linear, string file)
        {
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var pixels = new Color32[Width * Height];
            for (int p = 0; p < pixels.Length; p++)
            {
                byte C(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(Mathf.Max(0, v), 1 / 2.2f) * 255), 0, 255);
                pixels[p] = new Color32(C(linear[p * 4]), C(linear[p * 4 + 1]), C(linear[p * 4 + 2]), 255);
            }
            image.SetPixels32(pixels); image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }
    }
}
