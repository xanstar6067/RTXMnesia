#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Editor-only Play snapshots of the enhanced lighting (RTXMnesia.EditorDev.EnhancedLightingPreview): each view
    /// (player feet and a point to look at, HPL2 map coordinates) is rendered with the classic and the enhanced
    /// lighting, rasterized and ray traced, so the overlay of a map can be compared and tuned. The user's settings are
    /// only applied in memory, never saved.
    /// </summary>
    public sealed class HplEnhancedLightingProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.EnhancedLightingPreview.ExitCode";
        /// <summary>"name:fx fy fz tx ty tz[|mode,mode];..." — feet and target in HPL2 coordinates, optionally the view's own modes.</summary>
        public string views;
        /// <summary>
        /// Comma separated: classic_raster, classic_rt, enhanced_raster, enhanced_rt; "lantern" in a mode — the lantern
        /// in the hand, "noshadow" — without its ray-traced shadows (enhanced_rt_lantern, enhanced_rt_lantern_noshadow).
        /// </summary>
        public string modes;
        /// <summary>Lamps lit before the views (comma separated entity names).</summary>
        public string lit;
        public string folder, mapName;
        public int width = 1600, height = 900;

        IEnumerator Start()
        {
            var run = Run();
            while (true)
            {
                bool more = false, failed = false; object current = null;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception exception) { Debug.LogException(exception); failed = true; }
                if (failed) { Finish(1); yield break; }
                if (!more) break;
                yield return current;
            }
            Finish(0);
        }

        IEnumerator Run()
        {
            Application.runInBackground = true;
            yield return new WaitForSecondsRealtime(1f);
            var map = HplMap.Current;
            if (map == null || map.player == null) throw new InvalidOperationException("no map");
            var player = map.player;
            player.readDevices = false;
            if (map.GameUi != null && map.GameUi.IsPaused) map.GameUi.Resume();
            var user = HplUserSettings.ReadSaved();
            var report = new StringBuilder();
            Directory.CreateDirectory(folder);
            string defaultModes = string.IsNullOrEmpty(modes) ? "classic_raster,classic_rt,enhanced_raster,enhanced_rt" : modes;
            // Flicker switches lights off at random: the modes would not be comparable.
            foreach (var light in AmnesiaLight.All) if (light != null && !light.handHeld) light.SetFlickerActive(false);
            foreach (string lamp in (lit ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (map.GetEntityByName(lamp.Trim()) is Props.PropLamp p) p.SetLit(true, false);
                else Debug.LogWarning("[EnhancedLighting] no lamp " + lamp);
            }
            VerifySwitching(user, report);
            VerifyEntranceHallEvents(map, user, report);
            VerifyArchivesEvents(map, user, report);
            VerifyLabEvents(map, user, report);
            VerifyWineCellarEvents(map, user, report);
            VerifyDistilleryEvents(map, user, report);
            VerifyArchivesCellarState(map, user, report);
            VerifyCellarMazeState(map, user, report);
            VerifyBackHallEvents(map, user, report);
            VerifyDanielsRoomLamps(map, user, report);
            var studyCheck = VerifyStudyEvents(map, user, report);
            while (studyCheck.MoveNext()) yield return studyCheck.Current;
            VerifyStorageEvents(map, user, report);
            VerifyMachineRoomEvents(map, user, report);
            VerifyElevatorEvents(map, user, report);
            bool previewFog = map.FogActive;

            foreach (var spec in (views ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                int bar = spec.IndexOf('|');
                string view = bar >= 0 ? spec.Substring(0, bar) : spec;
                string[] modeList = (bar >= 0 ? spec.Substring(bar + 1) : defaultModes).Split(',', StringSplitOptions.RemoveEmptyEntries);
                int colon = view.IndexOf(':');
                string name = colon > 0 ? view.Substring(0, colon).Trim() : "view";
                var v = (colon > 0 ? view.Substring(colon + 1) : view).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                float F(int i) => float.Parse(v[i], CultureInfo.InvariantCulture);
                Vector3 feet = HplSpace.Position(new Vector3(F(0), F(1), F(2)));
                Vector3 target = HplSpace.Position(new Vector3(F(3), F(4), F(5)));

                foreach (string mode in modeList)
                {
                    var s = user.Copy();
                    s.lightingSet = mode.StartsWith("enhanced", StringComparison.Ordinal) ? 1 : 0;
                    s.lighting = mode.Contains("_rt") ? 1 : 0;
                    s.dlssMode = 0;
                    s.antialiasing = (int)HplAntialiasing.Taa;
                    // "_pt": path tracing, 64 samples (accumulated over the frames below; it has no custom pass: no box light, no fog).
                    s.rtPathTracing = mode.Contains("_pt") ? 1 : 0;
                    s.rtGi = mode.Contains("nogi") ? 0 : 2;
                    s.rtLanternShadows = !mode.Contains("noshadow");
                    if (mode.Contains("tex")) s.textures = 1;
                    HplEnhancedRelief.SeparateWalls = !mode.Contains("nolift");
                    // "noemit": without the GI copies of glowing materials (what their glow adds).
                    HplEnhancedEmission.Enabled = !mode.Contains("noemit");
                    HplEnhancedEmission.GainScale = mode.Contains("emitx") ? 30f : 1f;
                    HplGraphics.Apply(s);
                    ApplyElevatorPreviewState(map, name);
                    map.SetFog(active: mode.Contains("nofog") ? false : previewFog);
                    HplEnhancedRelief.SeparateSharedWalls();
                    yield return SetLantern(map, mode.Contains("lantern"));
                    var hidden = mode.Contains("only") ? HideOthers(map) : new List<Light>();
                    if (mode.Contains("nofill")) HideFills(hidden);
                    if (mode.Contains("cull0"))
                        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                            if (r.transform.parent != null && r.transform.parent.name.StartsWith("wall_default", StringComparison.Ordinal))
                                foreach (var m in r.materials) { m.SetFloat("_CullMode", 0); m.SetFloat("_CullModeForward", 0); }
                    map.effects.Fade.FadeIn(0f);
                    player.active = true;
                    Vector3 dir0 = target - feet;
                    player.PlaceAt(feet, Mathf.Atan2(dir0.x, dir0.z));
                    yield return new WaitForSecondsRealtime(0.3f);
                    Vector3 dir = target - player.LogicCameraPosition;
                    float yaw = Mathf.Atan2(dir.x, dir.z), pitch = -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude);
                    player.AddLook(Mathf.DeltaAngle(player.Yaw * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg) * Mathf.Deg2Rad, pitch - player.Pitch);
                    yield return new WaitForSecondsRealtime(0.3f);

                    var camera = map.PlayerCamera;
                    var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    camera.targetTexture = rt;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    int frames = s.rtPathTracing > 0 ? 160 : 40;
                    for (int i = 0; i < frames; i++)
                    {
                        yield return null;
                        camera.Render();
                        AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                    }
                    double ms = watch.Elapsed.TotalMilliseconds / frames;
                    foreach (var l in hidden) if (l != null) l.enabled = true;
                    var previous = RenderTexture.active;
                    RenderTexture.active = rt;
                    var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    RenderTexture.active = previous;
                    camera.targetTexture = null;
                    double sum = 0;
                    foreach (var c in image.GetPixels32()) sum += (c.r + c.g + c.b) / 3.0;
                    double mean = sum / (rt.width * rt.height);
                    string file = Path.Combine(folder, $"{mapName}_{name}_{mode}.png");
                    File.WriteAllBytes(file, image.EncodeToPNG());
                    Destroy(image);
                    rt.Release();
                    Destroy(rt);
                    if (mode.Contains("mats"))
                    {
                        // Which materials the view's target shows, and whether they have enhanced textures.
                        var seen = new HashSet<string>();
                        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                        {
                            if (r.bounds.SqrDistance(target) > 2.5f * 2.5f) continue;
                            foreach (var m in r.sharedMaterials)
                            {
                                if (m == null) continue;
                                var classic = HplEnhancedTextures.ClassicMaterial(m);
                                bool has = HplEnhancedTextures.ResolveMaterial(classic, true) != classic;
                                string key = $"{classic.name} enhancedNow={m != classic} hasEnhanced={has} shader={m.shader.name} smooth={(m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : -1):0.00} height={m.IsKeywordEnabled("_HEIGHTMAP")}";
                                if (seen.Add(key)) report.AppendLine($"    mat {r.transform.parent?.name}: {key}");
                            }
                        }
                    }
                    if (mode.Contains("walls"))
                    {
                        // The castle wall modules near the target: where each side's surface lies (enhanced relief vs flat).
                        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                        {
                            if (r.bounds.SqrDistance(target) > 3f * 3f) continue;
                            var m = r.sharedMaterial != null ? HplEnhancedTextures.ClassicMaterial(HplEnhancedRelief.SurfaceMaterial(r.sharedMaterial)) : null;
                            if (m == null || !m.name.StartsWith("castlebase_wall", StringComparison.Ordinal)) continue;
                            Vector3 a = HplSpace.Position(r.bounds.min), b = HplSpace.Position(r.bounds.max);
                            report.AppendLine($"    wall {r.transform.parent?.name}/{r.name} mesh={r.GetComponent<MeshFilter>()?.sharedMesh?.name} hpl {Vector3.Min(a, b)}..{Vector3.Max(a, b)}");
                        }
                    }
                    if (name == "P")
                    {
                        // Debug: what is between the torch and the room.
                        var torch = HplSpace.Position(new Vector3(2.05f, 0.25f, 18.99f));
                        var hits = Physics.RaycastAll(torch, (feet + Vector3.up * 1.5f - torch).normalized, 6f, ~0, QueryTriggerInteraction.Ignore);
                        foreach (var h in hits)
                        {
                            var r = h.collider.GetComponentInParent<Renderer>() ?? h.collider.GetComponentInChildren<Renderer>();
                            report.AppendLine($"    ray hit {h.collider.name} d={h.distance:0.00} renderer={(r != null ? r.name + " " + r.shadowCastingMode + " underMap=" + r.transform.IsChildOf(map.transform) : "none")}");
                        }
                        int[] counts = new int[4]; int outside = 0;
                        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) { counts[(int)r.shadowCastingMode]++; if (!r.transform.IsChildOf(map.transform)) outside++; }
                        report.AppendLine($"    renderers off/on/two/only = {string.Join("/", counts)}, outside map {outside}");
                        foreach (var px in new[] { new Vector2(980, 330), new Vector2(880, 215), new Vector2(960, 260) })
                        {
                            var ray = camera.ViewportPointToRay(new Vector3(px.x / 1600f, 1f - px.y / 900f, 0));
                            if (!Physics.Raycast(ray, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) { report.AppendLine($"    px {px}: no hit"); continue; }
                            report.AppendLine($"    px {px}: hit {hit.collider.transform.parent?.name}/{hit.collider.name} hpl={HplSpace.Position(hit.point)} n={hit.normal} toTorch={(torch - hit.point).normalized}");
                            var seg = torch - hit.point;
                            foreach (var h in Physics.RaycastAll(hit.point + seg.normalized * 0.005f, seg.normalized, seg.magnitude - 0.01f, ~0, QueryTriggerInteraction.Collide))
                                report.AppendLine($"        between: {h.collider.transform.parent?.name}/{h.collider.name} layer={h.collider.gameObject.layer} trigger={h.collider.isTrigger} hpl={HplSpace.Position(h.point)}");
                            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                                if (r.bounds.IntersectRay(new Ray(hit.point + seg.normalized * 0.005f, seg.normalized), out float d) && d < seg.magnitude)
                                    report.AppendLine($"        renderer on segment: {r.transform.parent?.name}/{r.name} cast={r.shadowCastingMode} mat={r.sharedMaterial?.name} d={d:0.00}");
                        }
                        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                        {
                            var p = r.transform.parent;
                            if (p == null || (p.name != "wall_default_11" && p.name != "wall_default_246" && p.name != "wall_default_12" && p.name != "wall_default_247")) continue;
                            var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                            if (mesh == null) continue;
                            Vector3 n = Vector3.zero; var nm = mesh.normals; var vs = mesh.vertices;
                            float minX = float.MaxValue, maxX = float.MinValue;
                            for (int i = 0; i < nm.Length; i++)
                            {
                                n += r.transform.TransformDirection(nm[i]);
                                float x = HplSpace.Position(r.transform.TransformPoint(vs[i])).x;
                                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                            }
                            var tri = mesh.triangles;
                            Vector3 wn = Vector3.zero;
                            for (int i = 0; i < tri.Length; i += 3)
                            {
                                Vector3 a = r.transform.TransformPoint(vs[tri[i]]), b = r.transform.TransformPoint(vs[tri[i + 1]]), c = r.transform.TransformPoint(vs[tri[i + 2]]);
                                wn += Vector3.Cross(b - a, c - a);
                            }
                            report.AppendLine($"    {p.name}/{r.name}: mean vertex normal (unity) {n.normalized}, winding normal (unity, Unity front = clockwise) {(-wn).normalized}, hpl x {minX:0.00}..{maxX:0.00}, tris {tri.Length / 3}");
                        }
                        var slab = new Bounds();
                        slab.SetMinMax(HplSpace.Position(new Vector3(3.4f, -0.5f, 17f)), HplSpace.Position(new Vector3(1.9f, 1.5f, 20.5f)));
                        slab.SetMinMax(Vector3.Min(slab.min, slab.max), Vector3.Max(slab.min, slab.max));
                        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                            if (r.bounds.Intersects(slab))
                                report.AppendLine($"    in wall: {r.transform.parent?.name}/{r.name} {r.GetType().Name} cast={r.shadowCastingMode} mat={r.sharedMaterial?.name} shader={r.sharedMaterial?.shader.name} hplMin={HplSpace.Position(r.bounds.min)} hplMax={HplSpace.Position(r.bounds.max)}");
                        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                            if (r.shadowCastingMode == ShadowCastingMode.Off && r.bounds.SqrDistance(torch) < 4f)
                                report.AppendLine($"    off near torch: {r.transform.parent?.name}/{r.name} mat={r.sharedMaterial?.name} shader={r.sharedMaterial?.shader.name} size={r.bounds.size}");
                    }
                    string line = $"{name} {mode}: mean {mean:0.0}, {ms:0.0} ms, enhanced applied={HplEnhancedLighting.Applied} " +
                                  $"changed={HplEnhancedLighting.ChangedLights} added={HplEnhancedLighting.AddedLights} wax={HplEnhancedLighting.WaxCandles} (not generated {HplEnhancedLighting.WaxWithoutGenerated}), " +
                                  $"RT={HplRayTracing.Active} traced={HplRayTracing.TracedLights}/{HplRayTracing.ShadowedLights}, shared walls separated={HplEnhancedRelief.SeparatedWallCount} decals={HplEnhancedRelief.LiftedDecalCount}, gi emission copies={HplEnhancedEmission.Copies} (static batched {HplEnhancedEmission.SkippedBatched}, not pushed {HplEnhancedEmission.NotPushed})";
                    report.AppendLine(line);
                    Debug.Log("[EnhancedLighting] " + line);
                    // The lights near the view as this mode draws them.
                    foreach (var light in AmnesiaLight.All)
                    {
                        if (light == null || light.box || !light.TryGetComponent<Light>(out var l) || !l.enabled || l.intensity <= 0f) continue;
                        if (Vector3.Distance(light.transform.position, feet) > l.range + 2f) continue;
                        bool traced = light.TryGetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>(out var hd) && hd.useRayTracedShadows;
                        report.AppendLine($"    {light.name}: {l.type} {l.intensity:0.##} {l.lightUnit} range={l.range:0.##} shadows={l.shadows} " +
                                          $"traced={traced} shape={l.shapeRadius:0.###} added={light.enhancedAdded} hand={light.handHeld} hpl={HplSpace.Position(light.transform.position)}");
                    }
                }
            }
            HplEnhancedRelief.SeparateWalls = true;
            HplEnhancedEmission.Enabled = true;
            HplEnhancedEmission.GainScale = 1f;
            yield return SetLantern(map, false);
            yield return VerifyThunder(map, user, report);
            HplGraphics.Apply(user);
            File.WriteAllText(Path.Combine(folder, mapName + "_report.txt"), report.ToString());
        }

        /// <summary>Original light state must survive repeated enhanced/classic changes, without a scene reload.</summary>
        static void VerifySwitching(HplUserSettings user, StringBuilder report)
        {
            string overlay = HplEnhancedLighting.ResourceFolder + Path.ChangeExtension(HplMap.Current.mapFile.Replace('\\', '/'), null);
            if (Resources.Load<TextAsset>(overlay) == null) return;
            var classic = user.Copy();
            classic.lightingSet = 0;
            HplGraphics.Apply(classic);
            var originals = new List<(AmnesiaLight data, Vector3 position, Quaternion rotation,
                float intensity, float range, Color color, bool shadows)>();
            foreach (var data in AmnesiaLight.All)
                if (data != null && !data.handHeld && !data.enhancedAdded && data.TryGetComponent<Light>(out var light))
                    originals.Add((data, data.transform.localPosition, data.transform.localRotation,
                        light.intensity, light.range, data.CurrentColor, data.castShadows));

            void Require(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException("Lighting switch: " + message);
            }
            // No frames elapse inside this check: moving lamps and flicker cannot mask a failed restoration.
            for (int cycle = 0; cycle < 3; cycle++)
            {
                var enhanced = classic.Copy();
                enhanced.lightingSet = 1;
                enhanced.lighting = cycle == 1 ? 0 : 1;
                enhanced.rtGi = cycle == 2 ? 0 : 2;
                HplGraphics.Apply(enhanced);
                Require(HplEnhancedLighting.Applied, "overlay not applied");
                int added = HplEnhancedLighting.AddedLights;
                HplEnhancedLighting.Refresh();
                Require(HplEnhancedLighting.AddedLights == added, "added lights accumulate on refresh");
                HplGraphics.Apply(classic);
                Require(!HplEnhancedLighting.Applied && HplEnhancedLighting.AddedLights == 0 &&
                    HplEnhancedLighting.ChangedLights == 0 && HplEnhancedLighting.WaxCandles == 0,
                    "enhanced objects remain in classic mode");
                foreach (var o in originals)
                {
                    var light = o.data.GetComponent<Light>();
                    Require(Vector3.Distance(o.data.transform.localPosition, o.position) < 1e-5f &&
                        Quaternion.Angle(o.data.transform.localRotation, o.rotation) < 0.01f &&
                        Mathf.Abs(light.intensity - o.intensity) < 1e-4f * Mathf.Max(1f, o.intensity) &&
                        Mathf.Abs(light.range - o.range) < 1e-5f && o.data.CurrentColor == o.color &&
                        o.data.castShadows == o.shadows && !o.data.classicLocalPose.HasValue &&
                        o.data.enhancedIntensity == 1f && o.data.enhancedRange == 0f && o.data.enhancedCandela == 0f,
                        "classic state not restored: " + o.data.name);
                }
            }
            string line = $"Switch verification PASS: {originals.Count} original lights restored over 3 cycles (RTGI, raster, RT without GI); refresh does not accumulate lights.";
            report.AppendLine(line);
            Debug.Log("[EnhancedLighting] " + line);
        }

        static void VerifyEntranceHallEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/02_entrance_hall.map", StringComparison.Ordinal)) return;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (var light in AmnesiaLight.All)
                if (light != null && (light.name.StartsWith("PointLightx", StringComparison.Ordinal) ||
                    light.name is "PointLightAcid" or "PointLight_2" or "PointLight_15" or "BoxLight_2"))
                    states.Add((light, light.Save()));
            if (states.Count != 19) throw new InvalidOperationException("Entrance hall: expected 19 event lights including the box");
            try
            {
                // Endpoints of the script's radius/colour fades, including lights initially black or radius zero.
                foreach (var (light, saved) in states)
                {
                    light.SetColor(light.box ? new Color(0.15f, 0.03f, 0.06f, 0f) :
                        light.name == "PointLight_15" ? new Color(0.2f, 0f, 0.05f, 1f) :
                        light.name == "PointLight_2" ? new Color(0.6f, 0.3f, 0.6f, 0.5f) : saved.color);
                    light.SetRadius(light.box ? saved.radius : 3f);
                }
                var classic = user.Copy();
                classic.lightingSet = 0;
                HplGraphics.Apply(classic);
                var expected = new List<(AmnesiaLight light, Color color, float intensity, float level)>();
                foreach (var (light, _) in states)
                    expected.Add((light, light.CurrentColor, light.box ? 0f : light.GetComponent<Light>().intensity,
                        light.LightLevelAt(light.transform.position + Vector3.up * 0.2f, 0f)));
                foreach (int lighting in new[] { 1, 0, 1 })
                {
                    var settings = classic.Copy();
                    settings.lightingSet = lighting;
                    HplGraphics.Apply(settings);
                    foreach (var e in expected)
                        if (e.light.CurrentColor != e.color || (!e.light.box &&
                            (e.light.CurrentRadius != 3f || e.light.GetComponent<Light>().range != 3f ||
                             Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity) > 1e-4f)) ||
                            Mathf.Abs(e.light.LightLevelAt(e.light.transform.position + Vector3.up * 0.2f, 0f) - e.level) > 1e-5f)
                            throw new InvalidOperationException("Entrance hall event light changed by overlay: " + e.light.name);
                }
                string line = "Entrance hall events PASS: 19 guardian/slime/acid/trail/box lights retain scripted colour/radius endpoints, rendered intensity and gameplay light level through switching.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                HplGraphics.Apply(user);
            }
        }

        static void VerifyArchivesEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/03_archives.map", StringComparison.Ordinal)) return;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "OrbLight", "PointLight_2", "PointLight_4", "PointLight_30", "BoxLight_4" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Archives: missing event light " + name);
                states.Add((light, light.Save()));
            }
            try
            {
                foreach (float orbRadius in new[] { 0f, 20f })
                {
                    foreach (var (light, saved) in states)
                    {
                        light.SetColor(light.name == "OrbLight" ? Color.white :
                            light.box ? new Color(1f, 0f, 0.2f, 1f) : Color.black);
                        light.SetRadius(light.name == "OrbLight" ? orbRadius :
                            light.name == "PointLight_30" ? 0f : saved.radius);
                    }
                    foreach (int lighting in new[] { 1, 0, 1, 0 })
                    {
                        var settings = user.Copy();
                        settings.lightingSet = lighting;
                        HplGraphics.Apply(settings);
                        foreach (var (light, saved) in states)
                        {
                            Color color = light.name == "OrbLight" ? Color.white :
                                light.box ? new Color(1f, 0f, 0.2f, 1f) : Color.black;
                            float radius = light.name == "OrbLight" ? orbRadius :
                                light.name == "PointLight_30" ? 0f : saved.radius;
                            if (light.CurrentColor != color || light.CurrentRadius != radius ||
                                (!light.box && color == Color.black && light.GetComponent<Light>().intensity > 0.001f) ||
                                (light.name == "OrbLight" && (light.enhancedIntensity != 1f || light.enhancedRange != 0f)))
                                throw new InvalidOperationException("Archives event state changed by overlay: " + light.name);
                        }
                    }
                }
                var lamp = map.GetEntityByName("hanging_lantern_wall_1") as Props.PropLamp;
                var connected = map.GetLight("PointLight_13");
                if (lamp == null || connected == null) throw new InvalidOperationException("Archives: missing lamp connection");
                bool wasLit = lamp.Lit;
                try
                {
                    lamp.SetLit(true, false);
                    lamp.UpdateLogic(0f);
                    Color onColor = connected.CurrentColor;
                    if (Mathf.Max(onColor.r, onColor.g, onColor.b) <= 0f)
                        throw new InvalidOperationException("Archives: lit lamp does not feed its connected fill");
                    foreach (int gi in new[] { 2, 0 })
                    {
                        var settings = user.Copy();
                        settings.lightingSet = 1; settings.lighting = 1; settings.rtGi = gi;
                        HplGraphics.Apply(settings);
                        lamp.UpdateLogic(0f);
                        float intensity = connected.GetComponent<Light>().intensity;
                        if (connected.CurrentColor != onColor || (gi > 0 ? intensity > 0.001f : intensity <= 0f))
                            throw new InvalidOperationException("Archives: overlay breaks connected lamp contribution");
                    }
                    lamp.SetLit(false, false);
                    lamp.UpdateLogic(0f);
                    if (connected.CurrentColor != connected.diffuseColor)
                        throw new InvalidOperationException("Archives: extinguished lamp leaves connected light behind");
                }
                finally { lamp.SetLit(wasLit, false); lamp.UpdateLogic(0f); }
                string line = "Archives events PASS: orb radius 0/20, corridor/passage fade endpoints, pink box and a connected lamp survive switching; lamp fill is suppressed with GI and returns without GI.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                HplGraphics.Apply(user);
            }
        }

        static void VerifyLabEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/04_wine_cellar_lab.map", StringComparison.Ordinal)) return;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "Valve_1_Light", "Valve_2_Light", "Valve_3_Light", "Valve_4_Light",
                "LightChemical_1", "LightChemical_2", "LightChemical_3", "LightChemical_4", "LightBurn", "LightAcid", "LightBurn_1" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Lab: missing machine light " + name);
                states.Add((light, light.Save()));
            }
            try
            {
                foreach (bool running in new[] { true, false })
                {
                    foreach (var (light, saved) in states)
                    {
                        light.SetColor(light.name == "LightBurn" ? new Color(saved.color.r, running ? 1f : 0.3f, saved.color.b, saved.color.a) :
                            light.name == "LightBurn_1" ? new Color(0.45f, 0.425f, 0.4f, saved.color.a) : saved.color);
                        light.SetRadius(light.name == "LightBurn_1" ? saved.radius : !running ? 0f :
                            light.name.StartsWith("Valve_", StringComparison.Ordinal) ? 0.4f :
                            light.name.StartsWith("LightChemical_", StringComparison.Ordinal) ? 0.5f :
                            light.name == "LightBurn" ? 3f : 0.3f);
                    }
                    var classic = user.Copy(); classic.lightingSet = 0;
                    HplGraphics.Apply(classic);
                    var expected = new List<(AmnesiaLight light, Color color, float radius, float intensity)>();
                    foreach (var (light, _) in states)
                        expected.Add((light, light.CurrentColor, light.CurrentRadius, light.GetComponent<Light>().intensity));
                    foreach (int mode in new[] { 1, 0, 1 })
                    {
                        var settings = classic.Copy(); settings.lightingSet = mode;
                        HplGraphics.Apply(settings);
                        foreach (var e in expected)
                        {
                            float factor = mode == 1 && e.light.name == "LightBurn_1" ? 0.6f : 1f;
                            if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * factor) > 1e-4f * Mathf.Max(1f, e.intensity))
                                throw new InvalidOperationException("Lab machine state changed by overlay: " + e.light.name);
                        }
                    }
                }
                string line = "Lab events PASS: 11 valve/chemical/burner/acid/ambience lights retain running and extinguished endpoints through switching; scripted ambience keeps its enhanced 0.6 multiplier.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                HplGraphics.Apply(user);
            }
        }

        static void VerifyWineCellarEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/05_wine_cellar.map", StringComparison.Ordinal)) return;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            for (int i = 0; i < 8; i++)
            {
                string name = i < 4 ? "LoftLight_" + (i + 1) : "PointLight_" + (i + 27);
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Wine cellar: missing event light " + name);
                states.Add((light, light.Save()));
            }
            try
            {
                foreach (bool active in new[] { true, false })
                {
                    foreach (var (light, saved) in states)
                    {
                        bool loft = light.name.StartsWith("LoftLight_", StringComparison.Ordinal);
                        light.SetColor(loft || active ? saved.color : Color.black);
                        light.SetRadius(loft ? (active ? 7f : 0f) : saved.radius);
                    }
                    var classic = user.Copy(); classic.lightingSet = 0;
                    HplGraphics.Apply(classic);
                    var expected = new List<(AmnesiaLight light, Color color, float radius, float intensity, float level)>();
                    foreach (var (light, _) in states)
                        expected.Add((light, light.CurrentColor, light.CurrentRadius, light.GetComponent<Light>().intensity,
                            light.LightLevelAt(light.transform.position + Vector3.down * 0.2f, 0f)));
                    foreach (int mode in new[] { 1, 0, 1 })
                    {
                        var settings = classic.Copy(); settings.lightingSet = mode;
                        HplGraphics.Apply(settings);
                        foreach (var e in expected)
                            if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity) > 1e-4f * Mathf.Max(1f, e.intensity) ||
                                Mathf.Abs(e.light.LightLevelAt(e.light.transform.position + Vector3.down * 0.2f, 0f) - e.level) > 1e-5f)
                                throw new InvalidOperationException("Wine cellar event changed by overlay: " + e.light.name);
                    }
                }
                string line = "Wine cellar events PASS: 8 pickup/loft lights retain colour, radius (loft 0/7), rendered intensity and gameplay level across switching; chemical fade endpoints remain black.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                HplGraphics.Apply(user);
            }
        }

        static void VerifyDistilleryEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/06_distillery.map", StringComparison.Ordinal)) return;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "PointLight_38", "PointLight_39", "BoxLight_1" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Distillery: missing event light " + name);
                states.Add((light, light.Save()));
            }
            try
            {
                // BreakPulleyBlocker fade endpoints and all three EnterHowlArea box colours.
                for (int stage = 0; stage <= 3; stage++)
                {
                    foreach (var (light, saved) in states)
                        light.SetColor(light.box ? new Color(stage * 0.05f, 0f, stage * 0.01f, saved.color.a) :
                            stage == 0 ? saved.color : new Color(0f, 0f, 0f, 0f));
                    var classic = user.Copy(); classic.lightingSet = 0;
                    HplGraphics.Apply(classic);
                    var expected = new List<(AmnesiaLight light, Color color, float radius, float intensity, float level)>();
                    foreach (var (light, _) in states)
                        expected.Add((light, light.CurrentColor, light.CurrentRadius,
                            light.box ? 0f : light.GetComponent<Light>().intensity,
                            light.LightLevelAt(light.transform.position + Vector3.down * 0.2f, 0f)));
                    foreach (int mode in new[] { 1, 0, 1 })
                    {
                        var settings = classic.Copy(); settings.lightingSet = mode;
                        HplGraphics.Apply(settings);
                        foreach (var e in expected)
                            if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                (!e.light.box && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity) > 1e-4f * Mathf.Max(1f, e.intensity)) ||
                                Mathf.Abs(e.light.LightLevelAt(e.light.transform.position + Vector3.down * 0.2f, 0f) - e.level) > 1e-5f)
                                throw new InvalidOperationException("Distillery event changed by overlay: " + e.light.name);
                    }
                }
                foreach (var pair in new[] { ("torch_static01_14", "PointLight_36"), ("hanging_lantern_ceiling_1", "PointLight_35") })
                {
                    var lamp = map.GetEntityByName(pair.Item1) as Props.PropLamp;
                    var connected = map.GetLight(pair.Item2);
                    if (lamp == null || connected == null) throw new InvalidOperationException("Distillery: missing lamp connection");
                    bool wasLit = lamp.Lit;
                    try
                    {
                        lamp.SetLit(true, false); lamp.UpdateLogic(0f);
                        Color onColor = connected.CurrentColor;
                        if (Mathf.Max(onColor.r, onColor.g, onColor.b) <= 0f)
                            throw new InvalidOperationException("Distillery: lamp does not feed connected fill");
                        foreach (int gi in new[] { 2, 0 })
                        {
                            var settings = user.Copy(); settings.lightingSet = 1; settings.lighting = 1; settings.rtGi = gi;
                            HplGraphics.Apply(settings); lamp.UpdateLogic(0f);
                            float intensity = connected.GetComponent<Light>().intensity;
                            if (connected.CurrentColor != onColor || (gi > 0 ? intensity > 0.001f : intensity <= 0f))
                                throw new InvalidOperationException("Distillery: connected fill lost its GI fallback");
                        }
                        lamp.SetLit(false, false); lamp.UpdateLogic(0f);
                        if (connected.CurrentColor != connected.diffuseColor)
                            throw new InvalidOperationException("Distillery: extinguished lamp leaves fill behind");
                    }
                    finally { lamp.SetLit(wasLit, false); lamp.UpdateLogic(0f); }
                }
                string line = "Distillery events PASS: pulley accent fade endpoints and three howl box colours survive switching; torch and hanging lantern connections keep colour, GI suppression and non-GI fallback, and extinguish cleanly.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                HplGraphics.Apply(user);
            }
        }

        static void VerifyArchivesCellarState(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/07_archives_cellar.map", StringComparison.Ordinal)) return;
            bool fog = map.FogActive;
            var classic = user.Copy(); classic.lightingSet = 0;
            HplGraphics.Apply(classic);
            var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float causticIntensity)>();
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || light.handHeld || light.enhancedAdded) continue;
                Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                bool caustic = light.name.StartsWith("SpotLight_", StringComparison.Ordinal) &&
                    light.name != "SpotLight_1" && light.name != "SpotLight_19";
                sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                    caustic ? light.GetComponent<Light>().intensity : -1f));
            }
            try
            {
                foreach (bool flooded in new[] { false, true })
                {
                    map.SetFog(active: flooded);
                    for (int mode = 0; mode < 4; mode++)
                    {
                        var settings = classic.Copy(); settings.lightingSet = mode == 3 ? 0 : 1;
                        settings.lighting = mode == 2 ? 0 : 1; settings.rtGi = mode == 1 ? 0 : 2;
                        HplGraphics.Apply(settings);
                        if (map.FogActive != flooded) throw new InvalidOperationException("Archives cellar: switching changed teleport fog state");
                        foreach (var e in sources)
                            if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                                (e.causticIntensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.causticIntensity) > 1e-4f * Mathf.Max(1f, e.causticIntensity)))
                                throw new InvalidOperationException("Archives cellar: gameplay/caustic state changed: " + e.light.name);
                        foreach (var light in AmnesiaLight.All)
                            if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                                throw new InvalidOperationException("Archives cellar: sky source affects gameplay light level");
                    }
                }
                string line = $"Archives cellar state PASS: {sources.Count} original lights keep colour, radius and sampled gameplay level; 15 caustic intensities and dry/flooded fog states survive RTGI, no-GI, raster and classic switching; added sky lights do not affect gameplay.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally { map.SetFog(active: fog); HplGraphics.Apply(user); }
        }

        static void VerifyCellarMazeState(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/08_cellar_maze.map", StringComparison.Ordinal)) return;
            var classic = user.Copy(); classic.lightingSet = 0;
            HplGraphics.Apply(classic);
            var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float protectedIntensity)>();
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || light.handHeld || light.enhancedAdded) continue;
                Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                bool preserve = light.name.StartsWith("SpotLight_", StringComparison.Ordinal) &&
                    light.name != "SpotLight_2" && light.name != "SpotLight_4" ||
                    light.name is "PointLight_25" or "PointLight_27" or "PointLight_28";
                sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                    preserve ? light.GetComponent<Light>().intensity : -1f));
            }
            try
            {
                for (int mode = 0; mode < 4; mode++)
                {
                    var settings = classic.Copy(); settings.lightingSet = mode == 3 ? 0 : 1;
                    settings.lighting = mode == 2 ? 0 : 1; settings.rtGi = mode == 1 ? 0 : 2;
                    HplGraphics.Apply(settings);
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.protectedIntensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.protectedIntensity) > 1e-4f * Mathf.Max(1f, e.protectedIntensity)))
                            throw new InvalidOperationException("Cellar maze: gameplay/water/organic light changed: " + e.light.name);
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                            throw new InvalidOperationException("Cellar maze: aperture affects gameplay light level");
                }
                string line = $"Cellar maze state PASS: {sources.Count} original lights keep colour, radius and sampled gameplay level; 8 caustics and 4 organic point/spot intensities survive RTGI, no-GI, raster and classic switching; added aperture lights do not affect gameplay.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally { HplGraphics.Apply(user); }
        }

        static void VerifyBackHallEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/09_back_hall.map", StringComparison.Ordinal)) return;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "BoxLight_2", "PointLight_30" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Back hall: missing event light " + name);
                states.Add((light, light.Save()));
            }
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            for (int i = 0; i < 10; i++)
            {
                string name = i < 4 ? "chandelier_simple_" + (i + 2) : "candlestick_wall_" + (i + 1);
                var lamp = map.GetEntityByName(name) as Props.PropLamp;
                if (lamp == null) throw new InvalidOperationException("Back hall: missing guardian lamp " + name);
                lamps.Add((lamp, lamp.Lit));
            }
            try
            {
                foreach (var (lamp, _) in lamps) { lamp.SetLit(false, false); lamp.UpdateLogic(0f); }
                for (int stage = 0; stage < 3; stage++)
                {
                    foreach (var (light, saved) in states)
                        light.SetColor(light.box ? stage == 0 ? saved.color : stage == 1 ?
                            new Color(0.2f, 0f, 0.05f, 0f) : new Color(0.17f, 0.1f, 0.22f, saved.color.a) :
                            stage == 1 ? new Color(0.8f, 0.2f, 0.4f, saved.color.a) : saved.color);
                    var classic = user.Copy(); classic.lightingSet = 0;
                    HplGraphics.Apply(classic);
                    var expected = new List<(AmnesiaLight light, Color color, float radius, float intensity, float level)>();
                    foreach (var (light, _) in states)
                        expected.Add((light, light.CurrentColor, light.CurrentRadius,
                            light.box ? 0f : light.GetComponent<Light>().intensity,
                            light.LightLevelAt(light.transform.position + Vector3.down * 0.2f, 0f)));
                    for (int mode = 0; mode < 4; mode++)
                    {
                        var settings = classic.Copy(); settings.lightingSet = mode == 3 ? 0 : 1;
                        settings.lighting = mode == 2 ? 0 : 1; settings.rtGi = mode == 1 ? 0 : 2;
                        HplGraphics.Apply(settings);
                        foreach (var e in expected)
                            if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                (!e.light.box && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity) > 1e-4f * Mathf.Max(1f, e.intensity)) ||
                                Mathf.Abs(e.light.LightLevelAt(e.light.transform.position + Vector3.down * 0.2f, 0f) - e.level) > 1e-5f)
                                throw new InvalidOperationException("Back hall: event light changed by switching: " + e.light.name);
                        foreach (var (lamp, _) in lamps)
                        {
                            if (lamp.Lit) throw new InvalidOperationException("Back hall: guardian lamp reignited: " + lamp.Name);
                            var ownLights = lamp.Component.GetComponentsInChildren<AmnesiaLight>(true);
                            if (ownLights.Length == 0) throw new InvalidOperationException("Back hall: guardian lamp has no light: " + lamp.Name);
                            foreach (var light in ownLights)
                                if (light.TryGetComponent<Light>(out var rendered) && rendered.intensity > 0.001f)
                                    throw new InvalidOperationException("Back hall: extinguished lamp still emits: " + light.name);
                        }
                    }
                }
                var connectedLamp = map.GetEntityByName("candlestick_floor_1") as Props.PropLamp;
                var connected = map.GetLight("PointLight_13");
                if (connectedLamp == null || connected == null) throw new InvalidOperationException("Back hall: missing floor lamp connection");
                bool wasLit = connectedLamp.Lit;
                try
                {
                    connectedLamp.SetLit(true, false); connectedLamp.UpdateLogic(0f);
                    Color onColor = connected.CurrentColor;
                    if (Mathf.Max(onColor.r, onColor.g, onColor.b) <= 0f) throw new InvalidOperationException("Back hall: connected fill receives no lamp colour");
                    foreach (int gi in new[] { 2, 0 })
                    {
                        var settings = user.Copy(); settings.lightingSet = 1; settings.lighting = 1; settings.rtGi = gi;
                        HplGraphics.Apply(settings); connectedLamp.UpdateLogic(0f);
                        float intensity = connected.GetComponent<Light>().intensity;
                        if (connected.CurrentColor != onColor || (gi > 0 ? intensity > 0.001f : intensity <= 0f))
                            throw new InvalidOperationException("Back hall: connected lamp lost GI fallback");
                    }
                    connectedLamp.SetLit(false, false); connectedLamp.UpdateLogic(0f);
                    if (connected.CurrentColor != connected.diffuseColor) throw new InvalidOperationException("Back hall: lamp leaves connected fill after extinction");
                }
                finally { connectedLamp.SetLit(wasLit, false); connectedLamp.UpdateLogic(0f); }
                string line = "Back hall events PASS: blue/guardian/blood-fountain box colours and black/pink trail endpoints survive RTGI, no-GI, raster and classic switching; 10 guardian lamps remain extinguished; floor lamp connection keeps colour, GI suppression and non-GI fallback.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                foreach (var (lamp, lit) in lamps) { lamp.SetLit(lit, false); lamp.UpdateLogic(0f); }
                HplGraphics.Apply(user);
            }
        }

        static void VerifyDanielsRoomLamps(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/10_daniels_room.map", StringComparison.Ordinal)) return;
            var classic = user.Copy(); classic.lightingSet = 0;
            HplGraphics.Apply(classic);
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 19) throw new InvalidOperationException("Daniels room: expected 19 lamps");
            try
            {
                // Test the three connected fills independently; shared connections retain their original update order.
                foreach (var pair in new[] { ("bonfire_3", "PointLight_1"), ("bonfire_2", "PointLight_2"), ("candlestick_wall_5", "PointLight_3") })
                {
                    var lamp = map.GetEntityByName(pair.Item1) as Props.PropLamp;
                    var connected = map.GetLight(pair.Item2);
                    if (lamp == null || connected == null) throw new InvalidOperationException("Daniels room: missing connection " + pair);
                    bool wasLit = lamp.Lit;
                    try
                    {
                        foreach (bool burning in new[] { false, true })
                        {
                            HplGraphics.Apply(classic);
                            lamp.SetLit(burning, false); lamp.UpdateLogic(0f);
                            Color expected = connected.CurrentColor;
                            if (burning ? Mathf.Max(expected.r, expected.g, expected.b) <= Mathf.Max(connected.diffuseColor.r, connected.diffuseColor.g, connected.diffuseColor.b) : expected != connected.diffuseColor)
                                throw new InvalidOperationException("Daniels room: lamp connection does not follow ignition " + lamp.Name);
                            var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level)>();
                            foreach (var source in AmnesiaLight.All)
                            {
                                if (source == null || source.handHeld || source.enhancedAdded) continue;
                                Vector3 sample = source.transform.position + source.transform.forward * 0.3f;
                                sources.Add((source, source.CurrentColor, source.CurrentRadius, sample, source.LightLevelAt(sample, 0f)));
                            }
                            for (int mode = 0; mode < 4; mode++)
                            {
                                var settings = classic.Copy(); settings.lightingSet = mode == 3 ? 0 : 1;
                                settings.lighting = mode == 2 ? 0 : 1; settings.rtGi = mode == 1 ? 0 : 2;
                                HplGraphics.Apply(settings);
                                if (lamp.Lit != burning || connected.CurrentColor != expected)
                                    throw new InvalidOperationException("Daniels room: switching changed lamp state " + lamp.Name);
                                float intensity = connected.GetComponent<Light>().intensity;
                                if (mode == 0 ? intensity > 0.001f : intensity <= 0f)
                                    throw new InvalidOperationException("Daniels room: connected fill lost GI suppression/fallback " + connected.name);
                                foreach (var e in sources)
                                    if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                        Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f)
                                        throw new InvalidOperationException("Daniels room: gameplay light changed " + e.light.name);
                                foreach (var source in AmnesiaLight.All)
                                    if (source != null && source.enhancedAdded && source.LightLevelAt(source.transform.position, 0f) != 0f)
                                        throw new InvalidOperationException("Daniels room: window affects gameplay light");
                                foreach (var (other, _) in lamps)
                                {
                                    if (other.Lit) continue;
                                    foreach (var own in other.Component.GetComponentsInChildren<AmnesiaLight>(true))
                                        if (own.TryGetComponent<Light>(out var rendered) && rendered.intensity > 0.001f)
                                            throw new InvalidOperationException("Daniels room: extinguished lamp emits " + other.Name);
                                }
                            }
                        }
                    }
                    finally { lamp.SetLit(wasLit, false); lamp.UpdateLogic(0f); }
                }
                string line = "Daniels room lamps PASS: ignition/extinction of both fireplaces and the closet wall candle preserves connected colour, GI suppression, non-GI fallback and sampled gameplay light in RTGI, RT without GI, raster and classic; extinguished lamps stay dark; added windows do not affect gameplay.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, lit) in lamps) { lamp.SetLit(lit, false); lamp.UpdateLogic(0f); }
                HplGraphics.Apply(user);
            }
        }

        static IEnumerator VerifyStudyEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/11_study.map", StringComparison.Ordinal)) yield break;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "SpotLight_4", "SpotLight_8", "PointLight_34" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Study: missing event light " + name);
                states.Add((light, light.Save()));
            }
            var windows = new List<(HplEntity entity, bool active)>();
            foreach (string name in new[] { "windowb_1", "windowb_2", "mansionbase_large_window_breakable_1" })
            {
                var entity = map.GetEntityByName(name);
                if (entity == null) throw new InvalidOperationException("Study: missing window " + name);
                windows.Add((entity, entity.Active));
            }
            var classic = user.Copy(); classic.lightingSet = 0;
            try
            {
                // Endpoint geometry states: intact, cracked with breakable glass, and open. No prop is destroyed.
                for (int stage = 0; stage < 3; stage++)
                {
                    windows[0].entity.SetActive(stage == 0);
                    windows[1].entity.SetActive(stage != 0);
                    windows[2].entity.SetActive(stage == 1);
                    foreach (var (light, saved) in states)
                    {
                        light.SetColor(light.name == "SpotLight_4" ? stage == 2 ? new Color(0.4f, 0.5f, 0.9f, saved.color.a) : saved.color :
                            stage == 2 ? new Color(0f, 0f, 0f, light.name == "SpotLight_8" ? 0f : saved.color.a) : saved.color);
                        light.SetRadius(light.name == "PointLight_34" && stage == 2 ? 0f : saved.radius);
                    }
                    HplGraphics.Apply(classic);
                    var expected = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity)>();
                    foreach (var (light, _) in states)
                    {
                        Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                        expected.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f), light.GetComponent<Light>().intensity));
                    }
                    for (int mode = 0; mode < 4; mode++)
                    {
                        var settings = classic.Copy(); settings.lightingSet = mode == 3 ? 0 : 1;
                        settings.lighting = mode == 2 ? 0 : 1; settings.rtGi = mode == 1 ? 0 : 2;
                        HplGraphics.Apply(settings);
                        yield return null;
                        yield return null; // Follow components have started and evaluated their source colours.
                        foreach (var e in expected)
                            if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                                Mathf.Abs(e.light.GetComponent<Light>().intensity - (mode != 3 && e.light.name != "PointLight_34" ? 0f : e.intensity)) > 1e-4f * Mathf.Max(1f, e.intensity))
                                throw new InvalidOperationException("Study: window/fade state changed " + e.light.name);
                        if (windows[0].entity.Active != (stage == 0) || windows[1].entity.Active != (stage != 0) || windows[2].entity.Active != (stage == 1))
                            throw new InvalidOperationException("Study: switching changed window geometry state");
                        int follows = 0;
                        foreach (var light in AmnesiaLight.All)
                        {
                            if (light == null || !light.enhancedAdded) continue;
                            if (light.LightLevelAt(light.transform.position, 0f) != 0f) throw new InvalidOperationException("Study: added sky affects gameplay");
                            if (!light.TryGetComponent<HplEnhancedLightFollow>(out var follow)) continue;
                            bool shouldEmit = follow.source.name == "SpotLight_8" ? stage != 2 : stage == 2;
                            var rendered = light.GetComponent<Light>();
                            if (rendered.enabled != shouldEmit || Mathf.Abs(rendered.intensity - (shouldEmit ? follow.intensity : 0f)) > 0.01f)
                                throw new InvalidOperationException("Study: window does not follow event source " + follow.source.name);
                            follows++;
                        }
                        if (follows != (mode == 3 ? 0 : 2)) throw new InvalidOperationException("Study: missing/accumulated window followers");
                    }
                }
                var lamp = map.GetEntityByName("candlestick_floor_5") as Props.PropLamp;
                var fill = map.GetLight("PointLight_11");
                if (lamp == null || fill == null) throw new InvalidOperationException("Study: missing northern floor candle connection");
                bool wasLit = lamp.Lit;
                try
                {
                    foreach (bool burning in new[] { false, true })
                    {
                        lamp.SetLit(burning, false); lamp.UpdateLogic(0f);
                        Color color = fill.CurrentColor;
                        foreach (int gi in new[] { 2, 0 })
                        {
                            var settings = classic.Copy(); settings.lightingSet = 1; settings.lighting = 1; settings.rtGi = gi;
                            HplGraphics.Apply(settings);
                            float value = fill.GetComponent<Light>().intensity;
                            if (fill.CurrentColor != color || (gi > 0 || !burning ? value > 0.001f : value <= 0f))
                                throw new InvalidOperationException("Study: connected fill loses ignition/GI state");
                        }
                    }
                }
                finally { lamp.SetLit(wasLit, false); lamp.UpdateLogic(0f); }
                string line = "Study events PASS: intact/cracked/open window states survive RTGI, no-GI, raster and classic; two glass areas follow SpotLight_8/4, northern PointLight_34 keeps fade-to-black/radius-zero and gameplay level; northern floor candle keeps connection/GI fallback; added sky does not affect gameplay.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                foreach (var (entity, active) in windows) entity.SetActive(active);
                HplGraphics.Apply(user);
            }
        }

        static void VerifyStorageEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/12_storage.map", StringComparison.Ordinal)) return;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "DangerLight", "BoomLight" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Storage: missing explosion light " + name);
                states.Add((light, light.Save()));
            }
            var altar = map.GetEntityByName("altar_lamp_1") as Props.PropLamp;
            if (altar == null) throw new InvalidOperationException("Storage: missing altar lamp");
            bool altarLit = altar.Lit, disabled = map.player.lanternDisabled;
            var classic = user.Copy(); classic.lightingSet = 0;
            try
            {
                // Initial state, red explosive warning, white flash with the altar lit, and extinguished explosion.
                for (int stage = 0; stage < 4; stage++)
                {
                    HplGraphics.Apply(classic);
                    foreach (var (light, saved) in states)
                    {
                        bool warning = light.name == "DangerLight" && stage == 1;
                        bool flash = light.name == "BoomLight" && stage == 2;
                        light.SetColor(stage == 0 ? saved.color : warning ? Color.red : flash ? Color.white : new Color(0f, 0f, 0f, 0f));
                        light.SetRadius(stage == 0 ? saved.radius : warning ? 4f : flash ? 8f : 0f);
                    }
                    altar.SetLit(stage >= 2, false); altar.UpdateLogic(0f);
                    map.player.lanternDisabled = stage == 1;
                    var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float eventIntensity)>();
                    foreach (var light in AmnesiaLight.All)
                    {
                        if (light == null || light.handHeld || light.enhancedAdded) continue;
                        Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                        bool explosion = light.name is "BoomLight" or "DangerLight";
                        sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f), explosion ? light.GetComponent<Light>().intensity : -1f));
                    }
                    for (int mode = 0; mode < 4; mode++)
                    {
                        var settings = classic.Copy(); settings.lightingSet = mode == 3 ? 0 : 1;
                        settings.lighting = mode == 2 ? 0 : 1; settings.rtGi = mode == 1 ? 0 : 2;
                        HplGraphics.Apply(settings);
                        foreach (var e in sources)
                            if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                                (e.eventIntensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.eventIntensity) > 1e-4f * Mathf.Max(1f, e.eventIntensity)))
                                throw new InvalidOperationException("Storage: explosion/gameplay light changed " + e.light.name);
                        if (altar.Lit != (stage >= 2) || map.player.lanternDisabled != (stage == 1))
                            throw new InvalidOperationException("Storage: switching changed altar/lantern lock state");
                        float ownIntensity = 0f;
                        foreach (var own in altar.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) ownIntensity += rendered.intensity;
                        if (stage >= 2 ? ownIntensity <= 0f : ownIntensity > 0.001f)
                            throw new InvalidOperationException("Storage: altar lamp ignition/extinction lost");
                        foreach (var light in AmnesiaLight.All)
                            if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                                throw new InvalidOperationException("Storage: window affects gameplay darkness");
                    }
                }
                foreach (var pair in new[] { ("torch_static01_5", "PointLight_12"), ("candle_floor_14", "PointLight_11") })
                {
                    var lamp = map.GetEntityByName(pair.Item1) as Props.PropLamp;
                    var fill = map.GetLight(pair.Item2);
                    if (lamp == null || fill == null) throw new InvalidOperationException("Storage: missing lamp connection " + pair);
                    bool wasLit = lamp.Lit;
                    try
                    {
                        foreach (bool burning in new[] { false, true })
                        {
                            HplGraphics.Apply(classic); lamp.SetLit(burning, false); lamp.UpdateLogic(0f);
                            Color color = fill.CurrentColor; float original = fill.GetComponent<Light>().intensity;
                            if (!burning && color != fill.diffuseColor) throw new InvalidOperationException("Storage: extinguished lamp leaves connected colour");
                            foreach (int gi in new[] { 2, 0 })
                            {
                                var settings = classic.Copy(); settings.lightingSet = 1; settings.lighting = 1; settings.rtGi = gi;
                                HplGraphics.Apply(settings);
                                if (fill.CurrentColor != color || Mathf.Abs(fill.GetComponent<Light>().intensity - (gi > 0 ? 0f : original * 0.6f)) > 1e-4f * Mathf.Max(1f, original))
                                    throw new InvalidOperationException("Storage: connected lamp loses GI fallback " + pair);
                            }
                        }
                    }
                    finally { lamp.SetLit(wasLit, false); lamp.UpdateLogic(0f); }
                }
                string line = "Storage events PASS: red warning/white flash/black radius-zero explosion endpoints, altar ignition and lantern lock survive RTGI, no-GI, raster and classic; original sampled gameplay light is preserved; torch/candle connections keep colour and GI fallback; added windows do not affect darkness.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                altar.SetLit(altarLit, false); altar.UpdateLogic(0f);
                map.player.lanternDisabled = disabled;
                HplGraphics.Apply(user);
            }
        }

        static void VerifyMachineRoomEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/13_machine_room.map", StringComparison.Ordinal)) return;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "PointLight_4", "PointLight_5", "PointLight_6", "PointLight_7",
                "SlimeLight_1", "SlimeLight_2", "SlimeLight_3", "SlimeLight_4", "SlimeLight_5", "SlimeLight_6" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Machine room: missing event light " + name);
                states.Add((light, light.Save()));
            }
            var burner = map.GetEntityByName("13_burner") as Props.PropLamp;
            if (burner == null) throw new InvalidOperationException("Machine room: missing burner");
            bool burnerLit = burner.Lit;
            var classic = user.Copy(); classic.lightingSet = 0;
            try
            {
                // Endpoints and middle of the two slime radius fades and blue puzzle-marker colour fades.
                // Burner ignition/failure is tested without starting machinery or changing puzzle progression.
                for (int stage = 0; stage < 3; stage++)
                {
                    HplGraphics.Apply(classic);
                    foreach (var (light, saved) in states)
                    {
                        bool slime = light.name.StartsWith("SlimeLight_", StringComparison.Ordinal);
                        light.SetColor(slime ? saved.color : saved.color * (1f - stage * 0.5f));
                        light.SetRadius(slime ? stage * 2.5f : saved.radius);
                    }
                    burner.SetLit(stage == 1, false); burner.UpdateLogic(0f);
                    var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float eventIntensity)>();
                    foreach (var light in AmnesiaLight.All)
                    {
                        if (light == null || light.handHeld || light.enhancedAdded) continue;
                        Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                        bool eventLight = light.name.StartsWith("SlimeLight_", StringComparison.Ordinal) ||
                            light.name is "PointLight_4" or "PointLight_5" or "PointLight_6" or "PointLight_7";
                        sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                            eventLight ? light.GetComponent<Light>().intensity : -1f));
                    }
                    for (int mode = 0; mode < 4; mode++)
                    {
                        var settings = classic.Copy(); settings.lightingSet = mode == 3 ? 0 : 1;
                        settings.lighting = mode == 2 ? 0 : 1; settings.rtGi = mode == 1 ? 0 : 2;
                        HplGraphics.Apply(settings);
                        foreach (var e in sources)
                            if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                                (e.eventIntensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.eventIntensity) > 1e-4f * Mathf.Max(1f, e.eventIntensity)) ||
                                (e.light.box && e.light.enhancedIntensity != 1f))
                                throw new InvalidOperationException("Machine room: event/box/gameplay light changed " + e.light.name);
                        float ownIntensity = 0f;
                        foreach (var own in burner.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) ownIntensity += rendered.intensity;
                        if (burner.Lit != (stage == 1) || (stage == 1 ? ownIntensity <= 0f : ownIntensity > 0.001f))
                            throw new InvalidOperationException("Machine room: burner ignition/failure state lost");
                    }
                }
                foreach (var pair in new[] { ("hanging_lantern_wall_3", "PointLight_18"), ("hanging_lantern_ceiling_7", "PointLight_26"),
                    ("hanging_lantern_ceiling_8", "PointLight_16"), ("hanging_lantern_ceiling_12", "PointLight_2"),
                    ("hanging_lantern_ceiling_15", "PointLight_24"), ("hanging_lantern_ceiling_17", "PointLight_17"),
                    ("hanging_lantern_ceiling_13", "PointLight_3"), ("hanging_lantern_ceiling_14", "PointLight_1") })
                {
                    var lamp = map.GetEntityByName(pair.Item1) as Props.PropLamp;
                    var fill = map.GetLight(pair.Item2);
                    if (lamp == null || fill == null) throw new InvalidOperationException("Machine room: missing lamp connection " + pair);
                    bool wasLit = lamp.Lit;
                    try
                    {
                        foreach (bool burning in new[] { false, true })
                        {
                            HplGraphics.Apply(classic); lamp.SetLit(burning, false); lamp.UpdateLogic(0f);
                            Color color = fill.CurrentColor; float original = fill.GetComponent<Light>().intensity;
                            if (!burning && color != fill.diffuseColor) throw new InvalidOperationException("Machine room: extinguished lantern leaves connected colour");
                            for (int mode = 0; mode < 4; mode++)
                            {
                                var settings = classic.Copy(); settings.lightingSet = mode == 3 ? 0 : 1;
                                settings.lighting = mode == 2 ? 0 : 1; settings.rtGi = mode == 1 ? 0 : 2;
                                HplGraphics.Apply(settings);
                                float expected = original * (mode == 3 ? 1f : mode == 0 ? 0f : 0.6f);
                                if (fill.CurrentColor != color || Mathf.Abs(fill.GetComponent<Light>().intensity - expected) > 1e-4f * Mathf.Max(1f, original))
                                    throw new InvalidOperationException("Machine room: connected lantern loses GI fallback " + pair);
                            }
                        }
                    }
                    finally { lamp.SetLit(wasLit, false); lamp.UpdateLogic(0f); }
                }
                string line = "Machine room events PASS: six slime radius fades (0/2.5/5), four blue puzzle-marker colour fades (full/half/black), burner ignition/failure and eight lantern connections survive RTGI, no-GI, raster and classic; all original sampled gameplay light and six ambient boxes preserved.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                burner.SetLit(burnerLit, false); burner.UpdateLogic(0f);
                HplGraphics.Apply(user);
            }
        }

        static void VerifyElevatorEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/14_elevator.map", StringComparison.Ordinal)) return;
            var states = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "PointLight_1", "PointLight_3", "PointLight_4" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Elevator: missing event light " + name);
                states.Add((light, light.Save()));
            }
            var lamp = map.GetEntityByName("elevator_lamp_1") as Props.PropLamp;
            if (lamp == null) throw new InvalidOperationException("Elevator: missing cabin lamp");
            bool wasLit = lamp.Lit;
            var classic = user.Copy(); classic.lightingSet = 0;
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, bool visible, float eventIntensity)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + Vector3.down * 0.2f;
                    bool eventLight = states.Exists(e => e.light == light);
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f), light.Visible,
                        eventLight ? light.GetComponent<Light>().intensity : -1f));
                }
                bool burning = lamp.Lit;
                for (int mode = 0; mode < 4; mode++)
                {
                    var settings = classic.Copy(); settings.lightingSet = mode == 3 ? 0 : 1;
                    settings.lighting = mode == 2 ? 0 : 1; settings.rtGi = mode == 1 ? 0 : 2;
                    HplGraphics.Apply(settings);
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.Visible != e.visible ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.eventIntensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.eventIntensity) > 1e-4f * Mathf.Max(1f, e.eventIntensity)))
                            throw new InvalidOperationException("Elevator: scripted colour/visibility/gameplay light changed " + e.light.name);
                    float ownIntensity = 0f;
                    foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                        if (own.TryGetComponent<Light>(out var rendered)) ownIntensity += rendered.intensity;
                    if (lamp.Lit != burning || (burning ? ownIntensity <= 0f : ownIntensity > 0.001f))
                        throw new InvalidOperationException("Elevator: cabin lamp state lost");
                }
            }
            try
            {
                // Endpoints of the light changes, without starting the ride or changing maps.
                foreach (string state in new[] { "initial", "amber", "red", "stopped", "falling", "impact" })
                {
                    HplGraphics.Apply(classic);
                    ApplyElevatorPreviewState(map, state);
                    VerifyModes();
                }
                // Switching must preserve the fade itself, not just its end colour.
                var cabin = map.GetLight("PointLight_1");
                cabin.SetColor(new Color(0f, 0f, 0f, 1f));
                foreach (Color destination in new[] { new Color(0.6f, 0.3f, 0f, 1f), new Color(0.2f, 0f, 0f, 1f) })
                {
                    Color start = cabin.CurrentColor;
                    cabin.FadeTo(destination, cabin.CurrentRadius, 2f);
                    for (int quarter = 1; quarter <= 4; quarter++)
                    {
                        VerifyModes();
                        cabin.UpdateLight(0.5f);
                        Color expected = Color.Lerp(start, destination, quarter * 0.25f);
                        Color actual = cabin.CurrentColor;
                        if (Mathf.Max(Mathf.Abs(actual.r - expected.r), Mathf.Abs(actual.g - expected.g), Mathf.Abs(actual.b - expected.b)) > 1e-5f)
                            throw new InvalidOperationException("Elevator: switching interrupted cabin colour fade");
                    }
                }
                string line = "Elevator events PASS: initial/amber/red/stop/fall/impact light states, cabin lamp ignition/extinction, both scrape visibility flags and live amber/red fades survive RTGI, no-GI, raster and classic; original HDRP event intensity and sampled gameplay light preserved.";
                report.AppendLine(line);
                Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in states) light.Restore(saved);
                lamp.SetLit(wasLit, false); lamp.UpdateLogic(0f);
                HplGraphics.Apply(user);
            }
        }

        // Editor-only staged snapshots of the cabin; no gameplay timers, movement or chapter transition are run.
        // View names amber*, red*, stopped*, falling*, impact* select the matching script light state.
        static void ApplyElevatorPreviewState(HplMap map, string view)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/14_elevator.map", StringComparison.Ordinal)) return;
            bool amber = view.StartsWith("amber", StringComparison.Ordinal);
            bool red = view.StartsWith("red", StringComparison.Ordinal) || view.StartsWith("stopped", StringComparison.Ordinal) ||
                view.StartsWith("falling", StringComparison.Ordinal) || view.StartsWith("impact", StringComparison.Ordinal);
            var lamp = (Props.PropLamp)map.GetEntityByName("elevator_lamp_1");
            lamp.SetLit(!amber, false); lamp.UpdateLogic(0f);
            var cabin = map.GetLight("PointLight_1");
            cabin.SetColor(amber ? new Color(0.6f, 0.3f, 0f, 1f) : red ? new Color(0.2f, 0f, 0f, 1f) : cabin.diffuseColor);
            bool scraping = !view.StartsWith("stopped", StringComparison.Ordinal) && !view.StartsWith("impact", StringComparison.Ordinal);
            map.GetLight("PointLight_3").SetVisible(scraping);
            map.GetLight("PointLight_4").SetVisible(scraping);
        }

        static IEnumerator VerifyThunder(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/00_rainy_hall.map", StringComparison.Ordinal)) yield break;
            var source = map.GetLight("spotthunder_1");
            var fill = map.GetLight("PointLight_25");
            Color original = source.CurrentColor, originalFill = fill.CurrentColor;
            var settings = user.Copy();
            settings.lightingSet = 1;
            settings.lighting = 1;
            settings.rtGi = 2;
            HplGraphics.Apply(settings);
            yield return null;
            // Added objects use DontSave and are excluded from FindObjectsByType; use the runtime light registry.
            var added = AmnesiaLight.All.Find(l => l != null && l.enhancedAdded &&
                l.TryGetComponent<HplEnhancedLightFollow>(out var f) && f.source == source);
            var follow = added != null ? added.GetComponent<HplEnhancedLightFollow>() : null;
            if (follow == null) throw new InvalidOperationException("No window follows spotthunder_1");
            var window = follow.GetComponent<Light>();
            float initial = window.intensity;
            source.FadeTo(Color.white, source.CurrentRadius, 0.1f);
            yield return new WaitForSecondsRealtime(0.3f);
            if (window.intensity <= initial) throw new InvalidOperationException("Window does not brighten during thunder fade");
            source.FadeTo(Color.black, source.CurrentRadius, 0.1f);
            fill.FadeTo(Color.black, fill.CurrentRadius, 0.1f);
            yield return new WaitForSecondsRealtime(0.3f);
            if (window.enabled || window.intensity > 0.001f || fill.GetComponent<Light>().intensity > 0.001f)
                throw new InvalidOperationException("Window or suppressed fill ignores fade to black");
            settings.lightingSet = 0;
            HplGraphics.Apply(settings);
            if (source.CurrentColor != Color.black || fill.CurrentColor != Color.black || source.GetComponent<Light>().intensity > 0.001f)
                throw new InvalidOperationException("Switching to classic loses script fade state");
            settings.lightingSet = 1;
            HplGraphics.Apply(settings);
            yield return null;
            source.SetColor(original);
            yield return null;
            source.SetVisible(false);
            yield return null;
            foreach (var data in AmnesiaLight.All)
                if (data != null && data.enhancedAdded && data.TryGetComponent<HplEnhancedLightFollow>(out var f) &&
                    f.source == source && f.GetComponent<Light>().enabled)
                    throw new InvalidOperationException("Window ignores source visibility");
            source.SetVisible(true);
            source.SetColor(original);
            fill.SetColor(originalFill);
            string line = "Thunder verification PASS: window follows bright/black fades and visibility; classic/enhanced switching preserves faded script lights.";
            report.AppendLine(line);
            Debug.Log("[EnhancedLighting] " + line);
        }

        /// <summary>Mode "only": every light off except the lit lamps (-lightLit) and the hand — one candle in a dark room.</summary>
        List<Light> HideOthers(HplMap map)
        {
            var keep = new HashSet<AmnesiaLight>();
            foreach (string lamp in (lit ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (map.GetEntityByName(lamp.Trim()) is { } entity)
                    keep.UnionWith(entity.GameObject.GetComponentsInChildren<AmnesiaLight>(true));
            var hidden = new List<Light>();
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || light.handHeld || keep.Contains(light) || !light.TryGetComponent<Light>(out var l) || !l.enabled) continue;
                l.enabled = false;
                hidden.Add(l);
            }
            return hidden;
        }

        /// <summary>Mode "nofill": the fill lights of the enhanced lighting (map lights without a source) off — is RTGI enough?</summary>
        static void HideFills(List<Light> hidden)
        {
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || !light.noTracedShadows || light.enhancedAdded || light.handHeld || light.box) continue;
                if (!light.TryGetComponent<Light>(out var l) || !l.enabled) continue;
                l.enabled = false;
                hidden.Add(l);
            }
        }

        /// <summary>The lantern in the hand (the item is given first) or put away; waits for the draw / holster.</summary>
        static IEnumerator SetLantern(HplMap map, bool on)
        {
            var player = map.player;
            if (player.lanternActive == on) yield break;
            if (on && HplGame.GetItem("lantern") == null)
            {
                var bank = Resources.Load<HplInventoryAssets>(HplInventoryAssets.ResourceName);
                var item = bank.Item("lantern.ent");
                HplGame.AddItem(map, "lantern", HplGame.ItemType(item.type), item.subType, item.image, item.amount, "", "", out _);
            }
            player.lampOil = Mathf.Max(player.lampOil, 50f);
            player.Lantern.SetActive(on, true);
            yield return new WaitForSecondsRealtime(1.8f);
        }

        void Finish(int code)
        {
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
