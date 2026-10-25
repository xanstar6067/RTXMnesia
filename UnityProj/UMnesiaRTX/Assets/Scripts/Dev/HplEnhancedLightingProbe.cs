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
using RTXMnesia.Player;
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
        static readonly Dictionary<string, Color32[]> s_pixels = new();
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

        // Preview output is silent; keep sound logic/timing running and never save a volume setting.
        void Awake() => AudioListener.volume = 0f;
        void LateUpdate() => AudioListener.volume = 0f;

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
            float savedVolume = user.masterVolume;
            user.masterVolume = 0f;
            AudioListener.volume = 0f;
            var report = new StringBuilder();
            report.AppendLine("Silent preview: AudioListener.volume = 0; saved user volume is not changed.");
            Directory.CreateDirectory(folder);
            string defaultModes = string.IsNullOrEmpty(modes) ? "classic_raster,classic_rt,enhanced_raster,enhanced_rt" : modes;
            // Flicker switches lights off at random: the modes would not be comparable.
            foreach (var light in AmnesiaLight.All) if (light != null && !light.handHeld) light.SetFlickerActive(false);
            foreach (string lamp in (lit ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (map.GetEntityByName(lamp.Trim()) is Props.PropLamp p) p.SetLit(true, false);
                else Debug.LogWarning("[EnhancedLighting] no lamp " + lamp);
            }
            // "nostress": capture ordinary setting changes without the synchronous verification cycles first.
            // Useful when checking whether a render artifact comes from the verification stress or normal switching.
            if (!defaultModes.Contains("nostress"))
            {
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
                VerifyPrisonSouthEvents(map, user, report);
                VerifyPrisonNorthEvents(map, user, report);
                VerifyCisternEntranceState(map, user, report);
                VerifyControlRoomState(map, user, report);
                VerifyCisternState(map, user, report);
                var morgueCheck = VerifyMorgueState(map, user, report);
                while (morgueCheck.MoveNext()) yield return morgueCheck.Current;
                // Diagnostic comparison with this map's overlay temporarily absent; generic switching checks still run.
                if (defaultModes.Contains("baseline")) report.AppendLine("Baseline diagnostic: map-specific overlay assertions skipped.");
                else VerifySewerLighting(map, user, report);
                VerifyNaveLighting(map, user, report);
                VerifyChancelLighting(map, user, report);
                VerifyTranseptLighting(map, user, report);
                VerifyChoirWestLighting(map, user, report);
                VerifyChoirEastLighting(map, user, report);
                VerifyCellTunnelsLighting(map, user, report);
                VerifyNaveReduxLighting(map, user, report);
                VerifyZimmermanLighting(map, user, report);
                VerifyChancelReduxLighting(map, user, report);
                VerifyInnerSanctumLighting(map, user, report);
                VerifyOrbChamberLighting(map, user, report);
                VerifyJustineCellsLighting(map, user, report);
                VerifyJustineLibraryLighting(map, user, report);
            }
            else report.AppendLine("Snapshot diagnostic: synchronous switching stress checks skipped; normal view modes still switch settings.");
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
                    // "nopcss" / "nocontact": shadow maps without the soft / contact shadows of the enhanced lighting.
                    s.softShadows = !mode.Contains("nopcss");
                    s.contactShadows = !mode.Contains("nocontact");
                    // "nolanternshadow": the lantern without its shadow map (with "lantern" in the name, it is held).
                    s.lanternShadow = !mode.Contains("nolanternshadow");
                    // "nocache": every shadow map every frame (no HplShadowCache).
                    s.shadowCache = !mode.Contains("nocache");
                    // "ssgi": screen space GI, medium ("ssgi1" / "ssgi3": low / high); only without RTGI. Off otherwise.
                    s.ssgi = !mode.Contains("ssgi") ? 0 : mode.Contains("ssgi1") ? 1 : mode.Contains("ssgi3") ? 3 : 2;
                    s.textures = mode.Contains("classictex") ? 0 : mode.Contains("tex") ? 1 : user.textures;
                    HplEnhancedRelief.SeparateWalls = !mode.Contains("nolift");
                    // "noemit": without the GI copies of glowing materials (what their glow adds).
                    HplEnhancedEmission.Enabled = !mode.Contains("noemit");
                    HplEnhancedEmission.GainScale = mode.Contains("emitx") ? 30f : 1f;
                    ApplyPrisonNorthCheckpointPreview(map, mode);
                    ApplyCisternEntranceWaterPreview(map, mode);
                    ApplyCisternLightingPreview(map, mode);
                    ApplyMorgueLightingPreview(map, mode);
                    ApplyNaveLightingPreview(map, mode);
                    ApplyChancelLightingPreview(map, mode);
                    ApplyTranseptLightingPreview(map, mode);
                    ApplyChoirWestLightingPreview(map, mode);
                    ApplyChoirEastLightingPreview(map, mode);
                    ApplyCellTunnelsLightingPreview(map, mode);
                    ApplyNaveReduxLightingPreview(map, mode);
                    ApplyZimmermanLightingPreview(map, mode);
                    ApplyChancelReduxLightingPreview(map, mode);
                    ApplyInnerSanctumLightingPreview(map, mode);
                    ApplyOrbChamberLightingPreview(map, mode);
                    ApplyJustineCellsLightingPreview(map, mode);
                    ApplyJustineLibraryLightingPreview(map, mode);
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
                    player.MoveState.SetCrouch(mode.Contains("crouch"));
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
                    // "moved": halfway, the shadow casting entity model nearest to the target moves 0.4 m sideways
                    // (the cached shadow maps around it must follow; compare with "moved_nocache").
                    Transform movedObject = null;
                    Vector3 movedFrom = default;
                    int redraws = 0;
                    HplShadowCache.MoverCounts = new Dictionary<string, int>();
                    for (int i = 0; i < frames; i++)
                    {
                        if (i == frames / 2 && mode.Contains("moved"))
                        {
                            float best = float.MaxValue;
                            foreach (var entity in map.GetComponentsInChildren<AmnesiaEntity>())
                                foreach (var r in entity.GetComponentsInChildren<MeshRenderer>())
                                {
                                    if (r.shadowCastingMode == ShadowCastingMode.Off || r.isPartOfStaticBatch) continue;
                                    float d = r.bounds.SqrDistance(target);
                                    if (d < best) { best = d; movedObject = r.transform; }
                                }
                            if (movedObject != null)
                            {
                                movedFrom = movedObject.position;
                                movedObject.position += camera.transform.right * 0.4f;
                                report.AppendLine($"    moved {movedObject.parent?.name}/{movedObject.name} by 0.4 m");
                            }
                        }
                        yield return null;
                        redraws += HplShadowCache.Redraws;
                        camera.Render();
                        AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                    }
                    double ms = watch.Elapsed.TotalMilliseconds / frames;
                    if (movedObject != null) movedObject.position = movedFrom;
                    var movers = new List<KeyValuePair<string, int>>(HplShadowCache.MoverCounts);
                    movers.Sort((a, b) => b.Value.CompareTo(a.Value));
                    HplShadowCache.MoverCounts = null;
                    if (HplShadowCache.CachedLights > 0)
                    {
                        report.AppendLine($"    shadow cache movers: {string.Join(", ", movers.GetRange(0, Math.Min(8, movers.Count)).ConvertAll(m => m.Key + " " + m.Value))}");
                        report.AppendLine($"    shadow cache no room: {string.Join(", ", HplShadowCache.FallbackNames)}");
                        var init = ((UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).currentPlatformRenderPipelineSettings.hdShadowInitParams;
                        var cachedNames = new List<string>();
                        foreach (var l in FindObjectsByType<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>(FindObjectsSortMode.None))
                            if (l.shadowUpdateMode == UnityEngine.Rendering.HighDefinition.ShadowUpdateMode.OnDemand)
                                cachedNames.Add($"{l.name} {l.GetComponent<Light>().type} {(l.shadowResolution.useOverride ? "override " + l.shadowResolution.@override : "level " + l.shadowResolution.level)} placed {UnityEngine.Rendering.HighDefinition.HDCachedShadowManager.instance.LightHasBeenPlacedInAtlas(l)}");
                        report.AppendLine($"    cached atlas {init.cachedPunctualLightShadowAtlas}, punctual tiers {init.shadowResolutionPunctual[0]}/{init.shadowResolutionPunctual[1]}/{init.shadowResolutionPunctual[2]}/{init.shadowResolutionPunctual[3]}; cached: {string.Join(", ", cachedNames)}");
                    }
                    foreach (var l in hidden) if (l != null) l.enabled = true;
                    var previous = RenderTexture.active;
                    RenderTexture.active = rt;
                    var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    RenderTexture.active = previous;
                    camera.targetTexture = null;
                    double sum = 0;
                    var pixels = image.GetPixels32();
                    foreach (var c in pixels) sum += (c.r + c.g + c.b) / 3.0;
                    // "nocache" / "again": against the same mode with the shadow cache / rendered before (the noise of particles), pixel by pixel.
                    s_pixels[name + "|" + mode] = pixels;
                    string twin = mode.Contains("_nocache") ? mode.Replace("_nocache", "") : mode.Contains("_again") ? mode.Replace("_again", "") : null;
                    if (twin != null && s_pixels.TryGetValue(name + "|" + twin, out var cached) && cached.Length == pixels.Length)
                    {
                        double diff = 0; int max = 0, over = 0;
                        for (int k = 0; k < pixels.Length; k++)
                        {
                            int d = Mathf.Max(Mathf.Abs(pixels[k].r - cached[k].r), Mathf.Abs(pixels[k].g - cached[k].g), Mathf.Abs(pixels[k].b - cached[k].b));
                            diff += d; max = Mathf.Max(max, d); if (d > 8) over++;
                        }
                        report.AppendLine($"    vs {twin}: mean abs {diff / pixels.Length:0.000}, max {max}, pixels > 8: {over}");
                    }
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
                    if (mode.Contains("rtat"))
                    {
                        // Renderers at the view's target and how the ray tracing structure takes them (path tracing
                        // draws only that structure).
                        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                        {
                            if (r.bounds.SqrDistance(target) > 0.3f * 0.3f) continue;
                            var names = new List<string>();
                            foreach (var m in r.sharedMaterials)
                                if (m != null) names.Add($"{m.name} ({m.shader.name}; {string.Join(" ", m.shaderKeywords)}; queue {m.renderQueue})");
                            report.AppendLine($"    at {r.name} under {r.transform.parent?.name}: rt={r.rayTracingMode} enabled={r.enabled} layer={r.gameObject.layer} " +
                                              $"batched={r.isPartOfStaticBatch} shadows={r.shadowCastingMode} mesh={r.GetComponent<MeshFilter>()?.sharedMesh?.name}: {string.Join(" | ", names)}");
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
                                  $"RT={HplRayTracing.Active} SSGI={HplRayTracing.ScreenGi} shadow cache={HplShadowCache.CachedLights} (redraws {redraws}, no room {HplShadowCache.Fallbacks}) traced={HplRayTracing.TracedLights}/{HplRayTracing.ShadowedLights}, shared walls separated={HplEnhancedRelief.SeparatedWallCount} decals={HplEnhancedRelief.LiftedDecalCount}, gi emission copies={HplEnhancedEmission.Copies} (static batched {HplEnhancedEmission.SkippedBatched}, not pushed {HplEnhancedEmission.NotPushed})";
                    report.AppendLine(line);
                    report.AppendLine($"    camera hpl={HplSpace.Position(camera.transform.position)}; silent volume={AudioListener.volume:0.00}");
                    Debug.Log("[EnhancedLighting] " + line);
                    // Whose shadows show in this view (HplLightAnalysis; the raster modes do not run it, so it runs here,
                    // a few estimates to settle the smoothing).
                    for (int pass = 0; pass < 4; pass++)
                    {
                        HplLightAnalysis.Invalidate();
                        HplLightAnalysis.Update(camera);
                    }
                    var ranked = HplLightAnalysis.Ranked();
                    var shares = new StringBuilder();
                    for (int i = 0; i < ranked.Count && i < 12; i++)
                        shares.Append(i > 0 ? ", " : "").Append($"{ranked[i].light.name} {ranked[i].importance:0.000}");
                    string analysis = $"light analysis: {HplLightAnalysis.Points} points, {HplLightAnalysis.Rays} rays, {HplLightAnalysis.Milliseconds:0.00} ms; " +
                                      $"{ranked.Count} lights seen: {shares}";
                    report.AppendLine("    " + analysis);
                    Debug.Log($"[EnhancedLighting] {name} {mode} {analysis}");
                    // Daniel's light level here: HPL2's light radii and the visible light (the darkness by the visible light).
                    player.LightLevel.Measure(false, out float classicNormal, out float classicExt);
                    player.LightLevel.Measure(true, out float visibleNormal, out float visibleExt);
                    string sensor = $"darkness sensor: classic {classicNormal:0.000}/{classicExt:0.000}, visible {visibleNormal:0.000}/{visibleExt:0.000} " +
                                    $"(normal/extended; darkness light below 0.2, sanity drain at 0.15 and below), in use {(PlayerLightLevel.VisibleOn ? "visible" : "classic")}";
                    report.AppendLine("    " + sensor);
                    Debug.Log($"[EnhancedLighting] {name} {mode} {sensor}");
                    if (!float.IsFinite(visibleNormal) || !float.IsFinite(visibleExt) || visibleNormal < 0f || visibleExt < visibleNormal)
                        throw new InvalidOperationException($"darkness sensor: bad visible levels {visibleNormal}/{visibleExt}");
                    if (mode.Contains("lantern") && visibleNormal < 1f)
                        throw new InvalidOperationException($"darkness sensor: the lit lantern must give at least 1, got {visibleNormal}");
                    // Every light off in this frame (the map turns some back on later): only the lantern is left.
                    var turnedOff = new List<Light>();
                    var boxesOff = new List<AmnesiaLight>();
                    foreach (var light in AmnesiaLight.All)
                    {
                        if (light == null || light.handHeld) continue;
                        if (light.box) { if (light.Visible) { light.SetVisible(false); boxesOff.Add(light); } }
                        else if (light.TryGetComponent<Light>(out var l) && l.enabled) { l.enabled = false; turnedOff.Add(l); }
                    }
                    player.LightLevel.Measure(true, out float darkNormal, out float darkExt);
                    foreach (var l in turnedOff) l.enabled = true;
                    foreach (var light in boxesOff) light.SetVisible(true);
                    float lanternLevel = player.Lantern != null && player.Lantern.Active ? 1f : 0f;
                    report.AppendLine($"    darkness sensor with every light off: {darkNormal:0.000}/{darkExt:0.000} (lantern {lanternLevel})");
                    if (Mathf.Abs(darkNormal - lanternLevel) > 0.001f || Mathf.Abs(darkExt - lanternLevel) > 0.001f)
                        throw new InvalidOperationException($"darkness sensor: every light off, still {darkNormal}/{darkExt}");
                    // The lights near the view as this mode draws them.
                    foreach (var light in AmnesiaLight.All)
                    {
                        if (light == null || light.box || !light.TryGetComponent<Light>(out var l) || !l.enabled || l.intensity <= 0f) continue;
                        if (Vector3.Distance(light.transform.position, feet) > l.range + 2f) continue;
                        bool traced = light.TryGetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>(out var hd) && hd.useRayTracedShadows;
                        report.AppendLine($"    {light.name}: {l.type} {l.intensity:0.##} {l.lightUnit} range={l.range:0.##} shadows={l.shadows} " +
                                          $"traced={traced} share={HplLightAnalysis.Importance(light):0.000} shape={l.shapeRadius:0.###} added={light.enhancedAdded} hand={light.handHeld} hpl={HplSpace.Position(light.transform.position)}");
                    }
                }
            }
            HplEnhancedRelief.SeparateWalls = true;
            HplEnhancedEmission.Enabled = true;
            HplEnhancedEmission.GainScale = 1f;
            yield return SetLantern(map, false);
            yield return VerifyThunder(map, user, report);
            HplGraphics.Apply(user);
            if (AudioListener.volume != 0f || HplUserSettings.ReadSaved().masterVolume != savedVolume)
                throw new InvalidOperationException("Silent preview changed saved volume or enabled audio output");
            report.AppendLine("Silent preview PASS: output stayed at zero and saved user volume is unchanged.");
            File.WriteAllText(Path.Combine(folder, mapName + "_report.txt"), report.ToString());
        }

        /// <summary>Original light state must survive repeated enhanced/classic changes, without a scene reload.</summary>
        static void VerifySwitching(HplUserSettings user, StringBuilder report)
        {
            string overlay = HplEnhancedLighting.ResourceFolder + Path.ChangeExtension(HplMap.Current.mapFile.Replace('\\', '/'), null);
            if (Resources.Load<TextAsset>(overlay) == null) return;
            var classic = user.Copy();
            classic.lightingSet = 0;
            classic.ssgi = 0;
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

        static void VerifyPrisonSouthEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/15_prison_south.map", StringComparison.Ordinal)) return;
            var events = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "CPLight", "LightAreaFlick_1", "LightAreaFlick_2", "LightAreaFlick_3", "LightAreaFlick_4", "PointLight_4", "PointLight_6", "PointLight_38" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Prison south: missing event light " + name);
                events.Add((light, light.Save()));
            }
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            var classic = user.Copy(); classic.lightingSet = 0;
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, bool visible, float intensity)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                    bool accent = events.Exists(e => e.light == light) || light.name == "PointLight_18";
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f), light.Visible, accent ? light.GetComponent<Light>().intensity : -1f));
                }
                var burning = new List<bool>();
                foreach (var (lamp, _) in lamps) burning.Add(lamp.Lit);
                for (int mode = 0; mode < 4; mode++)
                {
                    var s = classic.Copy(); s.lightingSet = mode == 3 ? 0 : 1; s.lighting = mode == 2 ? 0 : 1; s.rtGi = mode == 1 ? 0 : 2;
                    HplGraphics.Apply(s);
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.Visible != e.visible ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity) > 1e-4f * Mathf.Max(1f, e.intensity)))
                            throw new InvalidOperationException("Prison south: event/gameplay light changed " + e.light.name);
                    for (int i = 0; i < lamps.Count; i++)
                    {
                        var lamp = lamps[i].lamp; float intensity = 0f;
                        foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) intensity += rendered.intensity;
                        if (lamp.Lit != burning[i] || (burning[i] ? intensity <= 0f : intensity > 0.001f))
                            throw new InvalidOperationException("Prison south: switching relit/extinguished lamp " + lamp.Component.name);
                    }
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                            throw new InvalidOperationException("Prison south: added roof light affects darkness");
                }
            }
            try
            {
                for (int stage = 0; stage < 4; stage++)
                {
                    HplGraphics.Apply(classic);
                    foreach (var (light, saved) in events)
                    {
                        bool torch = light.name is "PointLight_4" or "PointLight_6";
                        Color on = torch ? new Color(0.35f, 0.25f, 0.1f, saved.color.a) : light.name == "PointLight_38" ? new Color(0.8f, 0.4f, 0.2f, 1f) : Color.white;
                        light.SetColor(stage == 0 ? saved.color : stage == 1 ? on : stage == 2 ? on * 0.5f : torch ? on : new Color(0f, 0f, 0f, light.name == "PointLight_38" ? 0f : 1f));
                    }
                    VerifyModes();
                }
                // Actual short checkpoint fade must continue through a full set of lighting switches.
                var flash = map.GetLight("CPLight"); flash.SetColor(new Color(0f, 0f, 0f, 1f));
                flash.FadeTo(Color.white, flash.CurrentRadius, 0.1f); flash.UpdateLight(0.05f);
                VerifyModes(); flash.UpdateLight(0.05f);
                if (Mathf.Abs(flash.CurrentColor.r - 1f) > 1e-5f) throw new InvalidOperationException("Prison south: checkpoint fade interrupted");
                // The breeze extinguishes four groups in order; a change of rendering mode must not relight them.
                foreach (var (lamp, _) in lamps)
                    if (lamp.Component.name.StartsWith("torch_static0b", StringComparison.Ordinal)) { lamp.SetLit(true, false); lamp.UpdateLogic(0f); }
                VerifyModes();
                for (int group = 1; group <= 4; group++)
                {
                    foreach (var (lamp, _) in lamps)
                        if (lamp.Component.name.StartsWith("torch_static0b" + group + "_1", StringComparison.Ordinal)) { lamp.SetLit(false, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                }
                foreach (var pair in new[] { ("torch_static0b4_12", "SpotLight_7"), ("torch_static01_5", "PointLight_30"),
                    ("torch_static01_6", "PointLight_31"), ("torch_static01_7", "PointLight_28"), ("torch_static01_8", "PointLight_29"),
                    ("torch_static01_9", "PointLight_27"), ("torch_static01_10", "PointLight_26"), ("torch_static0b4_11", "PointLight_25"),
                    ("torch_static0b3_12", "PointLight_24"), ("torch_static0b2_13", "PointLight_23"), ("torch_static0b1_14", "PointLight_32"),
                    ("torch_static0b2_15", "PointLight_22"), ("torch_static0b2_16", "PointLight_21"), ("torch_static0b3_17", "PointLight_20"),
                    ("torch_static0b4_18", "PointLight_16") })
                {
                    var lamp = (Props.PropLamp)map.GetEntityByName(pair.Item1); var fill = map.GetLight(pair.Item2);
                    foreach (bool burning in new[] { false, true })
                    {
                        HplGraphics.Apply(classic); lamp.SetLit(burning, false); lamp.UpdateLogic(0f);
                        Color color = fill.CurrentColor; float intensity = fill.GetComponent<Light>().intensity;
                        if (!burning && color != fill.diffuseColor) throw new InvalidOperationException("Prison south: connected torch leaves colour after extinction");
                        for (int mode = 0; mode < 4; mode++)
                        {
                            var s = classic.Copy(); s.lightingSet = mode == 3 ? 0 : 1; s.lighting = mode == 2 ? 0 : 1; s.rtGi = mode == 1 ? 0 : 2;
                            HplGraphics.Apply(s);
                            float expected = intensity * (mode == 3 ? 1f : mode == 0 ? 0f : 0.6f);
                            if (fill.CurrentColor != color || Mathf.Abs(fill.GetComponent<Light>().intensity - expected) > 1e-4f * Mathf.Max(1f, intensity))
                                throw new InvalidOperationException("Prison south: torch loses GI fallback " + pair);
                        }
                    }
                }
                string line = "Prison south events PASS: eight scripted flash/reveal/torch accents and a continuing checkpoint fade, nine breeze torches extinguished in four groups, all 15 lamp connections including SpotLight_7, original sampled gameplay light and lamp states survive RTGI, no-GI, raster and classic; roof areas do not affect darkness.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in events) light.Restore(saved);
                foreach (var (lamp, burning) in lamps) { lamp.SetLit(burning, false); lamp.UpdateLogic(0f); }
                HplGraphics.Apply(user);
            }
        }

        static void VerifyPrisonNorthEvents(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/15_prison_north.map", StringComparison.Ordinal)) return;
            var events = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "AcidLight", "PointLight_14", "CPLight", "CPLight02", "LightSiime_1", "LightSiime_2", "LightSiime_3", "LightSiime_4", "LightSiime_5" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Prison north: missing event light " + name);
                events.Add((light, light.Save()));
            }
            var lamps = new List<(Props.PropLamp lamp, bool lit, bool active)>();
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit, lamp.Active));
            var holders = new List<(HplEntity entity, bool active)>();
            for (int i = 1; i <= 4; i++)
            {
                var holder = map.GetEntityByName("torch_empty_" + i);
                if (holder == null) throw new InvalidOperationException("Prison north: missing checkpoint holder");
                holders.Add((holder, holder.Active));
            }
            var classic = user.Copy(); classic.lightingSet = 0;
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, bool visible, float intensity)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                    bool accent = events.Exists(e => e.light == light) || light.name is "PointLight_8" or "SpotLight_4";
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f), light.Visible,
                        accent ? light.GetComponent<Light>().intensity : -1f));
                }
                var burning = new List<bool>(); var active = new List<bool>(); var empty = new List<bool>();
                foreach (var (lamp, _, _) in lamps) { burning.Add(lamp.Lit); active.Add(lamp.Active); }
                foreach (var (holder, _) in holders) empty.Add(holder.Active);
                for (int mode = 0; mode < 4; mode++)
                {
                    var s = classic.Copy(); s.lightingSet = mode == 3 ? 0 : 1; s.lighting = mode == 2 ? 0 : 1; s.rtGi = mode == 1 ? 0 : 2;
                    HplGraphics.Apply(s);
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.Visible != e.visible ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity) > 1e-4f * Mathf.Max(1f, e.intensity)) ||
                            (e.light.box && e.light.enhancedIntensity != 1f))
                            throw new InvalidOperationException("Prison north: acid/slime/box/gameplay light changed " + e.light.name);
                    for (int i = 0; i < lamps.Count; i++)
                    {
                        var lamp = lamps[i].lamp; float intensity = 0f; int ownedLights = 0;
                        foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) { intensity += rendered.intensity; ownedLights++; }
                        if (lamp.Lit != burning[i] || lamp.Active != active[i] ||
                            (ownedLights > 0 && (burning[i] ? intensity <= 0f : intensity > 0.001f)))
                            throw new InvalidOperationException("Prison north: checkpoint lamp state lost " + lamp.Name);
                        if (mode != 3 && lamp.Active && lamp.Lit && lamp.Name.StartsWith("torch_cp_", StringComparison.Ordinal))
                            foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                                if (own.TryGetComponent<Light>(out _) && own.enhancedCandela <= 0f)
                                    throw new InvalidOperationException("Prison north: activated checkpoint torch misses physical flame rule");
                    }
                    for (int i = 0; i < holders.Count; i++)
                        if (holders[i].entity.Active != empty[i]) throw new InvalidOperationException("Prison north: empty holder visibility changed");
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                            throw new InvalidOperationException("Prison north: added roof light affects darkness");
                }
            }
            try
            {
                for (int stage = 0; stage < 4; stage++)
                {
                    HplGraphics.Apply(classic);
                    foreach (var (light, saved) in events)
                    {
                        bool slime = light.name.StartsWith("LightSiime_", StringComparison.Ordinal);
                        Color on = slime ? new Color(0.2f, 0.04f, 0.08f, 1f) : light.name == "AcidLight" ? new Color(0f, 1f, 0.4f, 1f) :
                            light.name == "PointLight_14" ? new Color(0f, 1f, 0.3f, 1f) : Color.white;
                        light.SetColor(stage == 0 ? saved.color : stage == 1 ? on : stage == 2 ? on * 0.5f : slime ? on :
                            new Color(0f, 0f, 0f, light.name.StartsWith("CPLight", StringComparison.Ordinal) ? 1f : 0f));
                    }
                    VerifyModes();
                }
                var acid = map.GetLight("AcidLight"); acid.SetColor(new Color(0f, 0f, 0f, 0f));
                acid.FadeTo(new Color(0f, 1f, 0.4f, 1f), acid.CurrentRadius, 2f); acid.UpdateLight(1f);
                VerifyModes(); acid.UpdateLight(1f);
                if (Mathf.Abs(acid.CurrentColor.g - 1f) > 1e-5f) throw new InvalidOperationException("Prison north: acid fade interrupted");
                // CP03 activates real torches in place of the empty holders and ignites the two northern torches.
                foreach (var (lamp, _, _) in lamps)
                {
                    if (lamp.Name.StartsWith("torch_cp_", StringComparison.Ordinal)) lamp.SetActive(true);
                    if (lamp.Name.StartsWith("torch_cp_", StringComparison.Ordinal) || lamp.Name is "torch_static01_17" or "torch_static01_18")
                    { lamp.SetLit(true, false); lamp.UpdateLogic(0f); }
                }
                foreach (var (holder, _) in holders) holder.SetActive(false);
                VerifyModes();
                foreach (var pair in new[] { ("torch_static01_1", "SpotLight_7"), ("torch_static01_6", "PointLight_17"),
                    ("torch_static01_7", "PointLight_20"), ("torch_static01_8", "PointLight_21"), ("torch_static01_11", "PointLight_37"),
                    ("torch_static01_12", "PointLight_36"), ("torch_static01_13", "PointLight_31"), ("torch_static01_14", "PointLight_28"),
                    ("torch_static01_15", "PointLight_29"), ("torch_static01_16", "PointLight_30"), ("torch_static01_17", "PointLight_38"),
                    ("torch_static01_18", "PointLight_35"), ("torch_static01_19", "PointLight_32"), ("torch_static01_20", "PointLight_33"),
                    ("torch_static01_21", "PointLight_34"), ("torch_static01_22", "PointLight_27"), ("torch_static01_23", "PointLight_26"),
                    ("torch_static01_24", "PointLight_25"), ("torch_static01_25", "PointLight_24"), ("torch_cp_1", "cp_light_03_1"),
                    ("torch_cp_2", "cp_light_03_2"), ("torch_cp_3", "cp_light_03_4"), ("torch_cp_4", "cp_light_03_2") })
                {
                    var lamp = (Props.PropLamp)map.GetEntityByName(pair.Item1); var fill = map.GetLight(pair.Item2);
                    foreach (bool lit in new[] { false, true })
                    {
                        HplGraphics.Apply(classic); lamp.SetLit(lit, false); lamp.UpdateLogic(0f);
                        Color color = fill.CurrentColor; float intensity = fill.GetComponent<Light>().intensity;
                        if (!lit && color != fill.diffuseColor) throw new InvalidOperationException("Prison north: extinguished torch leaves connected colour");
                        for (int mode = 0; mode < 4; mode++)
                        {
                            var s = classic.Copy(); s.lightingSet = mode == 3 ? 0 : 1; s.lighting = mode == 2 ? 0 : 1; s.rtGi = mode == 1 ? 0 : 2;
                            HplGraphics.Apply(s);
                            float expected = intensity * (mode == 3 ? 1f : mode == 0 ? 0f : 0.6f);
                            if (fill.CurrentColor != color || Mathf.Abs(fill.GetComponent<Light>().intensity - expected) > 1e-4f * Mathf.Max(1f, intensity))
                                throw new InvalidOperationException("Prison north: connected torch loses GI fallback " + pair);
                        }
                    }
                }
                string line = "Prison north events PASS: nine acid/lock/slime/checkpoint lights, continuing acid fade, acid caustics, CP03 activation of four torches/empty-holder replacement and ignition of torches 17/18, all 23 connections (22 targets, original shared CP target retained), box ambience and sampled gameplay light survive RTGI, no-GI, raster and classic; roof areas do not affect darkness.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in events) light.Restore(saved);
                foreach (var (lamp, lit, active) in lamps) { lamp.SetLit(lit, false); lamp.UpdateLogic(0f); lamp.SetActive(active); }
                foreach (var (holder, active) in holders) holder.SetActive(active);
                HplGraphics.Apply(user);
            }
        }

        // A staged CP03 view, without enemy activation, checkpoints or gameplay timers.
        static void ApplyPrisonNorthCheckpointPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/15_prison_north.map", StringComparison.Ordinal) || !mode.Contains("cp03")) return;
            for (int i = 1; i <= 4; i++)
            {
                var lamp = (Props.PropLamp)map.GetEntityByName("torch_cp_" + i);
                lamp.SetActive(true); lamp.SetLit(true, false); lamp.UpdateLogic(0f);
                map.GetEntityByName("torch_empty_" + i).SetActive(false);
            }
            foreach (string name in new[] { "torch_static01_17", "torch_static01_18" })
            { var lamp = (Props.PropLamp)map.GetEntityByName(name); lamp.SetLit(true, false); lamp.UpdateLogic(0f); }
        }

        static readonly string[] CisternWaterEntities = { "water_plane", "water_flood_block", "LiquidArea_1", "AreaDeepWater",
            "AreaDeepWater_1", "AreaDeepWater_2", "AreaDeepWater_3", "AreaDeepWater_4", "AreaDeepWater_5", "AreaDeepWaterSafe" };

        static void VerifyCisternEntranceState(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/16_cistern_entrance.map", StringComparison.Ordinal)) return;
            var classic = user.Copy(); classic.lightingSet = 0; HplGraphics.Apply(classic);
            var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float accent)>();
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || light.handHeld || light.enhancedAdded) continue;
                Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                    light.name is "PointLight_14" or "PointLight_29" ? light.GetComponent<Light>().intensity : -1f));
            }
            var water = new List<(HplEntity entity, bool active)>();
            foreach (string name in CisternWaterEntities)
            {
                var entity = map.GetEntityByName(name);
                if (entity == null) throw new InvalidOperationException("Cistern entrance: missing water entity " + name);
                water.Add((entity, entity.Active));
            }
            var mechanisms = new List<(HplProp prop, Matrix4x4 pose)>();
            foreach (string name in new[] { "bridge_metal_vert_1", "bridge_metal_vert_2", "ladder_static_1" })
            {
                var prop = map.GetEntityByName(name) as HplProp;
                if (prop?.MainBody == null) throw new InvalidOperationException("Cistern entrance: missing mechanism " + name);
                mechanisms.Add((prop, prop.MainBody.HplMatrix));
            }
            try
            {
                foreach (bool flooded in new[] { true, false })
                {
                    foreach (var (entity, _) in water) entity.SetActive(flooded);
                    for (int mode = 0; mode < 4; mode++)
                    {
                        var s = classic.Copy(); s.lightingSet = mode == 3 ? 0 : 1; s.lighting = mode == 2 ? 0 : 1; s.rtGi = mode == 1 ? 0 : 2;
                        HplGraphics.Apply(s);
                        foreach (var e in sources)
                            if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                                Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                                (e.accent >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.accent) > 1e-4f * Mathf.Max(1f, e.accent)) ||
                                (e.light.name == "BoxLight_2" && e.light.enhancedIntensity != 1f))
                                throw new InvalidOperationException("Cistern entrance: gameplay/lever/side ambient light changed " + e.light.name);
                        foreach (var (entity, _) in water)
                            if (entity.Active != flooded) throw new InvalidOperationException("Cistern entrance: water state lost " + entity.Name);
                        foreach (var (prop, pose) in mechanisms)
                            if (prop.MainBody.HplMatrix != pose) throw new InvalidOperationException("Cistern entrance: lighting moved mechanism " + prop.Name);
                        foreach (var light in AmnesiaLight.All)
                            if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                                throw new InvalidOperationException("Cistern entrance: roof area affects gameplay darkness");
                    }
                }
                string line = $"Cistern entrance state PASS: {sources.Count} original lights keep colour, radius and sampled gameplay illumination; two lever accents and side box ambience preserved; ten wet/drained entity states and bridge/ladder poses survive RTGI, no-GI, raster and classic; roof area does not affect darkness.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (entity, active) in water) entity.SetActive(active);
                HplGraphics.Apply(user);
            }
        }

        static void ApplyCisternEntranceWaterPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/16_cistern_entrance.map", StringComparison.Ordinal)) return;
            foreach (string name in CisternWaterEntities) map.GetEntityByName(name).SetActive(!mode.Contains("drained"));
        }

        static void VerifyControlRoomState(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/17_control_room.map", StringComparison.Ordinal)) return;
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            var pipes = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (string name in new[] { "PointLight_playpipesmall_part01", "PointLight_playpipehigh_part02", "PointLight_playpipelarge_part03" })
            {
                var light = map.GetLight(name);
                if (light == null) throw new InvalidOperationException("Control room: missing pipe light " + name);
                pipes.Add((light, light.Save()));
            }
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 20) throw new InvalidOperationException("Control room: expected 20 lamps");
            HplUserSettings Mode(int mode)
            {
                var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; return s;
            }
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float accent)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                    bool accent = light.name.StartsWith("PointLight_playpipe", StringComparison.Ordinal) || light.name is "PointLight_1" or "PointLight_25";
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        accent ? light.GetComponent<Light>().intensity : -1f));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    HplGraphics.Apply(Mode(mode));
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.accent >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.accent) > 1e-4f * Mathf.Max(1f, e.accent)))
                            throw new InvalidOperationException("Control room: pipe/accent/gameplay light changed " + e.light.name);
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                            throw new InvalidOperationException("Control room: sky source affects gameplay darkness");
                }
            }
            try
            {
                // Real .5 s RemovePipeLight fades must continue through all five rendering modes.
                VerifyModes();
                foreach (var (light, saved) in pipes)
                {
                    light.SetColor(saved.color); light.FadeTo(new Color(0f, 0f, 0f, 0f), saved.radius, 0.5f);
                    light.UpdateLight(0.25f); VerifyModes(); light.UpdateLight(0.25f); VerifyModes();
                    if (light.CurrentColor.maxColorComponent != 0f || light.GetComponent<Light>().intensity > 0.001f)
                        throw new InvalidOperationException("Control room: pipe cue was resurrected or its fade interrupted");
                }
                foreach (var pair in new[] { ("hanging_lantern_wall_1", "PointLight_37"), ("hanging_lantern_wall_3", "PointLight_36"),
                    ("candle_floor_2", "SpotLight_10"), ("candle_floor_3", "PointLight_20"),
                    ("candle_floor_5", "PointLight_21"), ("candle_floor_8", "SpotLight_12") })
                {
                    var lamp = (Props.PropLamp)map.GetEntityByName(pair.Item1); var fill = map.GetLight(pair.Item2);
                    foreach (bool lit in new[] { false, true })
                    {
                        HplGraphics.Apply(classic); lamp.SetLit(lit, false); lamp.UpdateLogic(0f);
                        Color color = fill.CurrentColor; float intensity = fill.GetComponent<Light>().intensity;
                        if (!lit && color != fill.diffuseColor) throw new InvalidOperationException("Control room: lamp contribution remains after extinguishing");
                        for (int mode = 0; mode < 5; mode++)
                        {
                            HplGraphics.Apply(Mode(mode)); float expected = intensity * (mode == 4 ? 1f : mode is 0 or 2 ? 0f : 0.6f);
                            if (fill.CurrentColor != color || Mathf.Abs(fill.GetComponent<Light>().intensity - expected) > 1e-4f * Mathf.Max(1f, intensity))
                                throw new InvalidOperationException("Control room: connected light loses GI fallback " + pair);
                        }
                    }
                }
                foreach (bool lit in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) { lamp.SetLit(lit, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                    for (int mode = 0; mode < 5; mode++)
                    {
                        HplGraphics.Apply(Mode(mode));
                        foreach (var (lamp, _) in lamps)
                        {
                            float intensity = 0f;
                            foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                                if (own.TryGetComponent<Light>(out var rendered)) intensity += rendered.intensity;
                            if (lamp.Lit != lit || (lit ? intensity <= 0f : intensity > 0.001f))
                                throw new InvalidOperationException("Control room: lamp ignition lost " + lamp.Name);
                        }
                    }
                }
                string line = "Control room state PASS: three continuing .5 s pipe fades remain black after switching; two blue interaction accents, 20 lamps off/on, all six lamp connections and sampled original gameplay light survive RTGI, RT no-GI, SSGI, raster no-GI and classic; added shaft lights do not affect darkness.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in pipes) light.Restore(saved);
                foreach (var (lamp, lit) in lamps) { lamp.SetLit(lit, false); lamp.UpdateLogic(0f); }
                HplGraphics.Apply(user);
            }
        }

        static readonly int[][] CisternTorchGroups = { new[] { 10, 4, 3, 13 }, new[] { 11, 2, 9, 7, 12 }, new[] { 6, 5, 8 } };
        static readonly string[][] CisternFillGroups = { new[] { "PointLight_14", "PointLight_7", "PointLight_17" },
            new[] { "PointLight_6", "PointLight_13", "PointLight_11", "PointLight_16" },
            new[] { "SpotLight_11", "PointLight_10", "PointLight_15", "PointLight_12" } };
        static readonly int[][] CisternWaterGroups = { new[] { 8 }, new[] { 4, 2, 3 }, new[] { 7, 5, 6 } };
        static readonly Color CisternCyan = new(0.025f, 0.15f, 0.18f, 1f), CisternWaterCyan = new(0.05f, 0.15f, 0.2f, 0f);

        static void VerifyCisternState(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/18_cistern.map", StringComparison.Ordinal)) return;
            var savedLights = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) savedLights.Add((light, light.Save()));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 13) throw new InvalidOperationException("Cistern: expected 13 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            var box = map.GetLight("BoxLight_1");
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * 0.3f;
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        light.TryGetComponent<Light>(out var rendered) ? rendered.intensity : -1f));
                }
                var burning = new List<bool>(); foreach (var (lamp, _) in lamps) burning.Add(lamp.Lit);
                for (int mode = 0; mode < 5; mode++)
                {
                    var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                    s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; HplGraphics.Apply(s);
                    foreach (var e in sources)
                    {
                        float factor = mode == 4 ? 1f : e.light.name == "SpotLight_11" ? 0f :
                            e.light.name is "PointLight_8" or "PointLight_9" ? 0.3f :
                            e.light.name.StartsWith("PointLight_", StringComparison.Ordinal) ? (mode is 0 or 2 ? 0f : 0.6f) : 1f;
                        bool mapLight = e.light.name.StartsWith("PointLight_", StringComparison.Ordinal) || e.light.name.StartsWith("SpotLight_", StringComparison.Ordinal);
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (mapLight && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * factor) > 1e-4f * Mathf.Max(1f, e.intensity)) ||
                            (e.light.box && e.light.enhancedIntensity != 1f))
                            throw new InvalidOperationException("Cistern: valve/caustic/ambient/gameplay state changed " + e.light.name);
                    }
                    for (int i = 0; i < lamps.Count; i++)
                    {
                        var lamp = lamps[i].lamp; float intensity = 0f;
                        foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) intensity += rendered.intensity;
                        if (lamp.Lit != burning[i] || (burning[i] ? intensity <= 0f : intensity > 0.001f))
                            throw new InvalidOperationException("Cistern: extinguished torch was resurrected " + lamp.Name);
                    }
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                            throw new InvalidOperationException("Cistern: sky source affects gameplay darkness");
                }
            }
            void FadeAndSwitch(AmnesiaLight light, Color target, float duration)
            {
                light.FadeTo(target, light.CurrentRadius, duration); light.UpdateLight(duration * 0.5f);
                VerifyModes(); light.UpdateLight(duration * 0.5f); VerifyModes();
                if (light.CurrentColor != target) throw new InvalidOperationException("Cistern: ongoing fade interrupted " + light.name);
            }
            try
            {
                VerifyModes();
                for (int group = 0; group < 3; group++)
                {
                    foreach (int number in CisternTorchGroups[group])
                    { var lamp = (Props.PropLamp)map.GetEntityByName("torch_static01_" + number); lamp.SetLit(false, false); lamp.UpdateLogic(0f); }
                    foreach (string name in CisternFillGroups[group]) FadeAndSwitch(map.GetLight(name), new Color(0f, 0f, 0f, 0f), 2f);
                    foreach (int number in CisternWaterGroups[group])
                    {
                        var light = map.GetLight("SpotLight_" + number); var color = CisternWaterCyan; color.a = light.CurrentColor.a;
                        FadeAndSwitch(light, color, 2f);
                    }
                    float level = group == 0 ? 0.8f : group == 1 ? 0.6f : 0.3f;
                    FadeAndSwitch(box, new Color(CisternCyan.r * level, CisternCyan.g * level, CisternCyan.b * level, 1f), 0.5f);
                }
                FadeAndSwitch(box, new Color(0.2f, 0.1f, 0.02f, box.CurrentColor.a), 4f);
                box.SetColor(CisternCyan); VerifyModes(); // Checkpoint / OnLeave restores cyan without reigniting torches.
                string line = "Cistern state PASS: three valve groups extinguish 12/13 torches; 11 fill/cone fades and seven caustic colour fades continue for 2 s through switching; all ten water projectors keep rendering, pump ambient .8/.6/.3 fades for .5 s, guardian amber for 4 s and checkpoint cyan survive RTGI, no-GI, SSGI, raster and classic; original gameplay light is preserved and sky light is excluded.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (light, saved) in savedLights) light.Restore(saved);
                foreach (var (lamp, lit) in lamps) { lamp.SetLit(lit, false); lamp.UpdateLogic(0f); }
                HplGraphics.Apply(user);
            }
        }

        // Lighting endpoints only; no pumping, monsters, checkpoint, water movement or story timers.
        static void ApplyCisternLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/18_cistern.map", StringComparison.Ordinal)) return;
            int stage = mode.Contains("pump3") || mode.Contains("guardian") ? 3 : mode.Contains("pump2") ? 2 : mode.Contains("pump1") ? 1 : 0;
            for (int group = 0; group < 3; group++)
            {
                bool off = group < stage;
                foreach (int number in CisternTorchGroups[group])
                { var lamp = (Props.PropLamp)map.GetEntityByName("torch_static01_" + number); lamp.SetLit(!off, false); lamp.UpdateLogic(0f); }
                foreach (string name in CisternFillGroups[group])
                { var light = map.GetLight(name); light.SetColor(off ? new Color(0f, 0f, 0f, 0f) : light.diffuseColor); }
                foreach (int number in CisternWaterGroups[group])
                { var light = map.GetLight("SpotLight_" + number); var color = CisternWaterCyan; color.a = light.diffuseColor.a; light.SetColor(off ? color : light.diffuseColor); }
            }
            float level = stage == 0 ? 1f : stage == 1 ? 0.8f : stage == 2 ? 0.6f : 0.3f;
            map.GetLight("BoxLight_1").SetColor(mode.Contains("guardian") ? new Color(0.2f, 0.1f, 0.02f, 1f) :
                new Color(CisternCyan.r * level, CisternCyan.g * level, CisternCyan.b * level, 1f));
        }

        static void SetMorgueRoomLights(HplMap map, bool inside)
        {
            void Set(int number, Color color) => map.GetLight("SpotLight_" + number).SetColor(color);
            Set(1, inside ? new Color(.8f, .7f, .6f, .5f) : new Color(0f, 0f, 0f, 0f));
            Set(8, inside ? new Color(.6f, .6f, .5f, .6f) : new Color(0f, 0f, 0f, 0f));
            Set(2, inside ? new Color(0f, 0f, 0f, 0f) : new Color(.3f, .3f, .2f, 1f));
            foreach (int number in new[] { 3, 4, 5, 6, 7, 10 })
            {
                var light = map.GetLight("SpotLight_" + number);
                float level = inside ? number == 7 ? .4f : number == 10 ? 0f : .3f : number == 10 ? .4f : 0f;
                light.SetColor(new Color(level, level, number == 10 ? level * .75f : level, light.CurrentColor.a));
            }
        }

        // Only the light endpoints of CollideLights and CheckPoint02, without triggering AI or story timers.
        static void ApplyMorgueLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/19_morgue.map", StringComparison.Ordinal)) return;
            foreach (string name in new[] { "candlestick_wall_9", "candlestick_wall_25", "candlestick_wall_28", "candlestick_wall_30",
                "candlestick_wall_32", "candlestick_wall_35", "candle_floor_small_1", "candlestick_wall_12", "candlestick_wall_14", "candlestick_wall_13", "candlestick_wall_15" })
            {
                var lamp = (Props.PropLamp)map.GetEntityByName(name);
                lamp.SetLit(mode.Contains("morguelit"), false); lamp.UpdateLogic(0f);
            }
            foreach (int number in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 10 })
            { var light = map.GetLight("SpotLight_" + number); light.SetColor(light.diffuseColor); }
            if (mode.Contains("morgueinside") || mode.Contains("morgueoutside"))
                SetMorgueRoomLights(map, mode.Contains("morgueinside"));
            var red = map.GetLight("PointLight_24");
            red.SetColor(mode.Contains("morguered") ? new Color(.6f, 0f, 0f, .55f) : red.diffuseColor);
        }

        static IEnumerator VerifyMorgueState(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/19_morgue.map", StringComparison.Ordinal)) yield break;
            var savedLights = new List<(AmnesiaLight light, AmnesiaLight.SaveData saved)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) savedLights.Add((light, light.Save()));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 17) throw new InvalidOperationException("Morgue: expected 17 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            HplUserSettings Mode(int mode)
            {
                var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; return s;
            }
            float Factor(string name, int mode)
            {
                if (mode == 4 || name is "PointLight_2" or "PointLight_24") return 1f;
                if (name == "PointLight_14") return mode is 0 or 2 ? .35f : .6f;
                if (name is "SpotLight_1" or "SpotLight_2" or "SpotLight_8" or "SpotLight_10") return 0f;
                if (name is "PointLight_6" or "PointLight_34" or "PointLight_35" or "PointLight_38" or "PointLight_44" or "PointLight_45" or "PointLight_51" or "PointLight_52" or "PointLight_32" or "PointLight_62") return .3f;
                if (name is "PointLight_13" or "PointLight_26" or "PointLight_31" or "PointLight_40" or "PointLight_59" or "PointLight_64" or "PointLight_65") return .7f;
                return name.StartsWith("PointLight_", StringComparison.Ordinal) ? (mode is 0 or 2 ? 0f : .6f) : 1f;
            }
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * .3f;
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        light.TryGetComponent<Light>(out var rendered) ? rendered.intensity : -1f));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    HplGraphics.Apply(Mode(mode));
                    foreach (var e in sources)
                    {
                        bool standalone = e.light.name.StartsWith("PointLight_", StringComparison.Ordinal) || e.light.name.StartsWith("SpotLight_", StringComparison.Ordinal);
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (standalone && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * Factor(e.light.name, mode)) > 1e-4f * Mathf.Max(1f, e.intensity)) ||
                            (e.light.box && e.light.enhancedIntensity != 1f))
                            throw new InvalidOperationException("Morgue: scripted/caustic/gameplay light changed " + e.light.name);
                    }
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f;
                        foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) value += rendered.intensity;
                        if (lamp.Lit ? value <= 0f : value > .001f) throw new InvalidOperationException("Morgue: lamp state lost " + lamp.Name);
                    }
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                            throw new InvalidOperationException("Morgue: sky affects gameplay darkness");
                }
            }
            try
            {
                VerifyModes();
                foreach (bool lit in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) { lamp.SetLit(lit, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                }
                foreach (var pair in new[] { ("candlestick_wall_9", "PointLight_42"), ("candlestick_wall_25", "PointLight_17"),
                    ("candlestick_wall_28", "PointLight_16"), ("candlestick_wall_30", "PointLight_18"),
                    ("candlestick_wall_32", "PointLight_25"), ("candlestick_wall_35", "PointLight_19"),
                    ("candlestick_wall_12", "PointLight_36"), ("candlestick_wall_14", "PointLight_37"), ("candlestick_wall_15", "PointLight_11") })
                {
                    var lamp = (Props.PropLamp)map.GetEntityByName(pair.Item1); var fill = map.GetLight(pair.Item2);
                    foreach (bool lit in new[] { false, true })
                    {
                        lamp.SetLit(lit, false); lamp.UpdateLogic(0f);
                        if (!lit && fill.CurrentColor != fill.diffuseColor) throw new InvalidOperationException("Morgue: connected fill remains after extinguishing " + pair);
                        if (lit && fill.CurrentColor.maxColorComponent <= fill.diffuseColor.maxColorComponent) throw new InvalidOperationException("Morgue: missing lamp contribution " + pair);
                        VerifyModes();
                    }
                }
                // The source .map references absent PointLight_41 from candlestick_wall_13. Preserve this existing quirk.
                if (map.GetLight("PointLight_41") != null) throw new InvalidOperationException("Morgue: unexpected invented connection target");
                var red = map.GetLight("PointLight_24"); var target = new Color(.6f, 0f, 0f, .55f);
                red.FadeTo(target, red.CurrentRadius, 1f); red.UpdateLight(.5f); VerifyModes(); red.UpdateLight(.5f); VerifyModes();
                if (red.CurrentColor != target) throw new InvalidOperationException("Morgue: checkpoint fade interrupted");
                foreach (var (lamp, lit) in lamps) { lamp.SetLit(lit, false); lamp.UpdateLogic(0f); }
                foreach (bool inside in new[] { true, false, true })
                {
                    SetMorgueRoomLights(map, inside); VerifyModes();
                    for (int mode = 0; mode < 5; mode++)
                    {
                        HplGraphics.Apply(Mode(mode)); yield return null; yield return null;
                        int count = 0;
                        foreach (var added in AmnesiaLight.All)
                        {
                            if (added == null || !added.enhancedAdded || !added.TryGetComponent<HplEnhancedLightFollow>(out var follow)) continue;
                            count++; var rendered = added.GetComponent<Light>(); var c = follow.source.CurrentColor; var original = follow.source.diffuseColor;
                            float level = Mathf.GammaToLinearSpace(Mathf.Max(c.r, c.g, c.b));
                            float basis = Mathf.GammaToLinearSpace(Mathf.Max(original.r, original.g, original.b));
                            float expected = follow.source.Visible ? follow.intensity * level / basis : 0f;
                            if (Mathf.Abs(rendered.intensity - expected) > 1e-4f * Mathf.Max(1f, expected) || rendered.enabled != (expected > 0f))
                                throw new InvalidOperationException("Morgue: sky ignores CollideLights " + added.name);
                        }
                        if (count != (mode == 4 ? 0 : 3)) throw new InvalidOperationException("Morgue: sky sources accumulate or disappear");
                    }
                }
                var settings = Mode(0); HplGraphics.Apply(settings);
                var source = map.GetLight("SpotLight_1"); source.SetVisible(false); yield return null; yield return null;
                foreach (var added in AmnesiaLight.All)
                    if (added != null && added.enhancedAdded && added.TryGetComponent<HplEnhancedLightFollow>(out var follow) && follow.source == source && added.GetComponent<Light>().enabled)
                        throw new InvalidOperationException("Morgue: sky ignores source visibility");
                string line = "Morgue state PASS: 17 lamps off/on, nine valid .6 lamp connections (source map has one missing PointLight_41), one-second checkpoint red fade, both CollideLights room endpoints and all five water caustics survive RTGI, RT no-GI, SSGI, raster no-GI and classic; three sky areas follow brightness/visibility without accumulating or affecting original gameplay darkness.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, lit) in lamps) { lamp.SetLit(lit, false); lamp.UpdateLogic(0f); }
                foreach (var (light, saved) in savedLights) light.Restore(saved);
                HplGraphics.Apply(user);
            }
        }

        static void VerifySewerLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/20_sewer.map", StringComparison.Ordinal)) return;
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp) throw new InvalidOperationException("Sewer: source map unexpectedly has a lamp");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float accent)>();
            var caustics = new HashSet<string>();
            foreach (int number in new[] { 8, 9, 10, 11, 12, 13, 14, 17, 18, 19, 20, 22, 23, 28, 29 })
                caustics.Add("SpotLight_" + number);
            try
            {
                HplGraphics.Apply(classic);
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * .3f;
                    bool accent = caustics.Contains(light.name) || light.name is "PointLight_3" or "PointLight_41" or "PointLight_42" or "PointLight_43" or "PointLight_44";
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        accent ? light.GetComponent<Light>().intensity : -1f));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                    s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; HplGraphics.Apply(s);
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.accent >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.accent) > 1e-4f * Mathf.Max(1f, e.accent)) ||
                            (e.light.box && e.light.enhancedIntensity != 1f))
                            throw new InvalidOperationException("Sewer: caustic/accent/ambient/gameplay light changed " + e.light.name);
                    int count = 0;
                    foreach (var light in AmnesiaLight.All)
                    {
                        if (light == null || !light.enhancedAdded) continue;
                        count++;
                        if (light.LightLevelAt(light.transform.position, 0f) != 0f)
                            throw new InvalidOperationException("Sewer: sky source affects gameplay darkness");
                    }
                    if (count != (mode == 4 ? 0 : 35)) throw new InvalidOperationException("Sewer: added sky sources accumulate or disappear");
                }
                string line = "Sewer lighting PASS: no lamp entities or scripted light changes; 15 water caustics, five coloured accents and map-wide ambient retain rendering across RTGI, RT no-GI, SSGI, raster no-GI and classic; original colours, radii and sampled gameplay light are preserved, 35 sky areas do not accumulate or affect gameplay darkness.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally { HplGraphics.Apply(user); }
        }

        static readonly string[] NaveBlackoutLamps = { "torch_static01_40", "torch_static01_44", "torch_static01_22",
            "torch_static01_43", "torch_static01_47", "chandelier_simple_2", "hanging_lantern_ceiling_8", "hanging_lantern_ceiling_5" };
        static readonly string[] NaveBlackoutLights = { "PointLight_83", "PointLight_77", "SpotLight_6", "SpotLight_9",
            "PointLight_78", "PointLight_82", "PointLight_12", "PointLight_94", "PointLight_87", "PointLight_90", "PointLight_91", "PointLight_92" };
        static readonly HashSet<string> NaveProtectedLights = new() { "PointLight_4", "PointLight_8", "PointLight_44", "PointLight_45",
            "PointLight_46", "PointLight_59", "PointLight_64", "PointLight_69", "PointLight_72", "PointLight_73", "PointLight_85", "PointLight_86", "PointLight_95", "PointLight_97" };

        // Light endpoints only: no cell event timers, door impulses, NPC voices, sanity effects or AI.
        static void ApplyNaveLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/21_torture_nave.map", StringComparison.Ordinal)) return;
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool on = mode.Contains("navelit") || lamp.Component.GetVariable("Lit") != "false";
                    if (mode.Contains("naveblackout") && Array.IndexOf(NaveBlackoutLamps, lamp.Name) >= 0) on = false;
                    lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                }
            foreach (string name in NaveBlackoutLights)
            { var light = map.GetLight(name); light.SetColor(mode.Contains("naveblackout") ? new Color(0f, 0f, 0f, 0f) : light.diffuseColor); }
            for (int i = 1; i <= 8; i++)
            {
                var light = map.GetLight("light_prison_" + i);
                light.SetColor(mode.Contains("navecell" + i) ? new Color(.9f, .15f, .05f, 1f) : light.diffuseColor);
            }
        }

        static void VerifyNaveLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/21_torture_nave.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) saved.Add((light, light.Save()));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 64) throw new InvalidOperationException("Nave: expected 64 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            HplUserSettings Mode(int mode)
            {
                var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; return s;
            }
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float accent)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    var sample = light.transform.position + light.transform.forward * .3f;
                    bool accent = NaveProtectedLights.Contains(light.name) || light.name.StartsWith("light_prison_", StringComparison.Ordinal);
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        accent ? light.GetComponent<Light>().intensity : -1f));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    HplGraphics.Apply(Mode(mode));
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.accent >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.accent) > 1e-4f * Mathf.Max(1f, e.accent)) ||
                            (e.light.box && e.light.enhancedIntensity != 1f))
                            throw new InvalidOperationException("Nave: cell/candle/slime/residual/gameplay light changed " + e.light.name);
                    int noLight = 0;
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f; int ownCount = 0;
                        foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) { ownCount++; value += rendered.intensity; }
                        if (ownCount == 0) { noLight++; continue; }
                        if (lamp.Lit ? value <= 0f : value > .001f) throw new InvalidOperationException("Nave: lamp state lost " + lamp.Name);
                    }
                    if (noLight != 11) throw new InvalidOperationException("Nave: no_light candles unexpectedly acquired/lost owned lights");
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded && light.LightLevelAt(light.transform.position, 0f) != 0f)
                            throw new InvalidOperationException("Nave: sky changes legacy HPL2 light level");
                }
            }
            void FadeAndSwitch(AmnesiaLight light, Color target, float duration)
            {
                light.FadeTo(target, light.CurrentRadius, duration); light.UpdateLight(duration * .5f); VerifyModes();
                light.UpdateLight(duration * .5f); VerifyModes();
                if (light.CurrentColor != target) throw new InvalidOperationException("Nave: fade interrupted " + light.name);
            }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { true, false })
                {
                    foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                }
                int effective = 0, missing = 0, inert = 0;
                foreach (var (lamp, _) in lamps)
                {
                    string name = lamp.Component.GetVariable("ConnectedLight"); if (string.IsNullOrEmpty(name)) continue;
                    var fill = map.GetLight(name);
                    if (fill == null) { missing++; continue; }
                    bool hasLight = lamp.Component.GetComponentsInChildren<AmnesiaLight>(true).Length > 0;
                    if (!hasLight)
                    {
                        inert++; Color original = fill.CurrentColor;
                        foreach (bool on in new[] { true, false }) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); VerifyModes(); }
                        if (fill.CurrentColor != original) throw new InvalidOperationException("Nave: inert no_light connection changed colour");
                        continue;
                    }
                    effective++;
                    foreach (bool on in new[] { true, false })
                    {
                        HplGraphics.Apply(classic); lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                        Color color = fill.CurrentColor; float intensity = fill.GetComponent<Light>().intensity;
                        if (!on && color != fill.diffuseColor) throw new InvalidOperationException("Nave: extinguished contribution remains " + lamp.Name);
                        if (on && color == fill.diffuseColor) throw new InvalidOperationException("Nave: missing connected contribution " + lamp.Name);
                        for (int mode = 0; mode < 5; mode++)
                        {
                            HplGraphics.Apply(Mode(mode));
                            float factor = mode == 4 ? 1f : name == "SpotLight_3" ? 0f : mode is 0 or 2 ? 0f : .6f;
                            if (fill.CurrentColor != color || Mathf.Abs(fill.GetComponent<Light>().intensity - intensity * factor) > 1e-4f * Mathf.Max(1f, intensity))
                                throw new InvalidOperationException("Nave: connected fill loses GI fallback " + lamp.Name);
                        }
                    }
                }
                if (effective != 29 || missing != 1 || inert != 1 || map.GetLight("PointLight_40") != null)
                    throw new InvalidOperationException($"Nave: unexpected source connections {effective}/{missing}/{inert}");
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (string name in NaveBlackoutLamps) { var lamp = (Props.PropLamp)map.GetEntityByName(name); lamp.SetLit(false, false); lamp.UpdateLogic(0f); }
                foreach (string name in NaveBlackoutLights) FadeAndSwitch(map.GetLight(name), new Color(0f, 0f, 0f, 0f), 1f);
                for (int i = 1; i <= 8; i++)
                {
                    var light = map.GetLight("light_prison_" + i);
                    FadeAndSwitch(light, new Color(.9f, .15f, .05f, 1f), .2f);
                    FadeAndSwitch(light, new Color(0f, 0f, 0f, 0f), .4f);
                }
                string line = "Nave state PASS: 64 lamp states (53 with owned lights, 11 no_light candles), 29 effective connections, original missing PointLight_40 and inert no_light connection retained; eight lamps extinguished, 12 continuing one-second blackout fades and eight .2/.4-second red cell pulses survive RTGI, RT no-GI, SSGI, raster no-GI and classic; residual PointLight_8, candle/slime accents, four boxes and legacy HPL2 light levels are preserved. Visible-darkness readings are checked separately per view.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (light, data) in saved) light.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static readonly string[] ChancelBarrierLights = { "PointLight_26", "PointLight_27", "PointLight_28", "SpotLight_1", "SpotLight_4" };

        // Light endpoints only. The sixth orb piece is a debug-only path in this map; no assembly/progression is changed.
        static void ApplyChancelLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/22_torture_chancel.map", StringComparison.Ordinal)) return;
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool on = lamp.Component.GetVariable("Lit") != "false";
                    if (lamp.Name == "orb_pedestal_1" && mode.Contains("chancelorb")) on = true;
                    if (lamp.Name == "hanging_lantern_ceiling_3" && mode.Contains("chancelwestlit")) on = true;
                    if (mode.Contains("chancelalloff")) on = false;
                    lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                    foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) own.SetFlickerActive(false);
                }
            foreach (string name in ChancelBarrierLights)
            {
                var light = map.GetLight(name); bool low = mode.Contains("chancelflickerlow");
                light.SetFlickerActive(false);
                light.SetColor(low ? light.flickerOffColor : light.diffuseColor);
                light.SetRadius(low ? light.flickerOffRadius : light.radius);
            }
        }

        static void VerifyChancelLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/22_torture_chancel.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) saved.Add((light, light.Save()));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 102) throw new InvalidOperationException("Chancel: expected 102 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            HplUserSettings Mode(int mode)
            {
                var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; return s;
            }
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float accent, bool flicker)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    var sample = light.transform.position + light.transform.forward * .3f;
                    var parent = light.GetComponentInParent<AmnesiaEntity>();
                    bool accent = Array.IndexOf(ChancelBarrierLights, light.name) >= 0 || (parent != null &&
                        (parent.entityType != "Lamp" || parent.name == "orb_pedestal_1"));
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        accent ? light.GetComponent<Light>().intensity : -1f, light.FlickerActive));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    HplGraphics.Apply(Mode(mode));
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.FlickerActive != e.flicker ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.accent >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.accent) > 1e-4f * Mathf.Max(1f, e.accent)) ||
                            (e.light.box && e.light.enhancedIntensity != 1f))
                            throw new InvalidOperationException("Chancel: barrier/orb/flicker/ambient/legacy light changed " + e.light.name);
                    int noLight = 0;
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f; int ownCount = 0;
                        foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) { ownCount++; value += rendered.intensity; }
                        if (ownCount == 0) { noLight++; continue; }
                        if (lamp.EffectsAlpha > 0f ? value <= 0f : value > .001f)
                            throw new InvalidOperationException("Chancel: lamp effect state lost " + lamp.Name);
                    }
                    if (noLight != 64) throw new InvalidOperationException("Chancel: expected 64 candles without owned lights");
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded) throw new InvalidOperationException("Chancel: unexpected added sky light");
                }
            }
            float OwnIntensity(Props.PropLamp lamp)
            {
                float result = 0f;
                foreach (var light in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                    if (light.TryGetComponent<Light>(out var rendered)) result += rendered.intensity;
                return result;
            }
            void AdvanceLamp(Props.PropLamp lamp, float step)
            {
                lamp.UpdateLogic(step);
                foreach (var light in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) light.UpdateLight(step);
            }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                }
                var linked = (Props.PropLamp)map.GetEntityByName("hanging_lantern_ceiling_3"); var fill = map.GetLight("PointLight_7");
                foreach (bool on in new[] { false, true })
                {
                    HplGraphics.Apply(classic); linked.SetLit(on, false); linked.UpdateLogic(0f);
                    Color color = fill.CurrentColor; float intensity = fill.GetComponent<Light>().intensity;
                    if (on ? color == fill.diffuseColor : color != fill.diffuseColor) throw new InvalidOperationException("Chancel: broken lantern connection");
                    for (int mode = 0; mode < 5; mode++)
                    {
                        HplGraphics.Apply(Mode(mode)); float factor = mode == 4 ? 1f : mode is 0 or 2 ? 0f : .6f;
                        if (fill.CurrentColor != color || Mathf.Abs(fill.GetComponent<Light>().intensity - intensity * factor) > 1e-4f * Mathf.Max(1f, intensity))
                            throw new InvalidOperationException("Chancel: connected fill loses GI fallback");
                    }
                }
                var orb = (Props.PropLamp)map.GetEntityByName("orb_pedestal_1");
                orb.SetLit(false, false); orb.UpdateLogic(0f); orb.SetLit(true, true); AdvanceLamp(orb, .5f); VerifyModes();
                HplGraphics.Apply(classic); float half = OwnIntensity(orb); AdvanceLamp(orb, .5f); VerifyModes();
                HplGraphics.Apply(classic); if (half <= 0f || OwnIntensity(orb) <= half) throw new InvalidOperationException("Chancel: pedestal ignition fade interrupted");
                orb.SetLit(false, true); AdvanceLamp(orb, .5f); VerifyModes(); AdvanceLamp(orb, .5f); VerifyModes();
                if (OwnIntensity(orb) > .001f) throw new InvalidOperationException("Chancel: pedestal extinguishing fade interrupted");
                foreach (string name in ChancelBarrierLights)
                {
                    var light = map.GetLight(name); light.SetFlickerActive(true); var colours = new HashSet<Color>(); var radii = new HashSet<float>();
                    for (int step = 0; step < 4; step++) { light.UpdateLight(1f); colours.Add(light.CurrentColor); radii.Add(light.CurrentRadius); VerifyModes(); }
                    if (colours.Count < 2 || (name.StartsWith("SpotLight_", StringComparison.Ordinal) && radii.Count < 2))
                        throw new InvalidOperationException("Chancel: barrier flicker stopped " + name);
                    light.SetFlickerActive(false);
                }
                string line = "Chancel state PASS: 102 lamp states (38 with owned light, 64 no_light candles), western lantern connection and GI fallback, continuing one-second pedestal ignition/extinguishing fades (debug sixth-piece path), five live barrier flicker colour/radius states and ambient survive RTGI, RT no-GI, SSGI, raster no-GI and classic; legacy HPL2 light is preserved and no sky sources are added. Visible-darkness readings are checked separately per view.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (light, data) in saved) light.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static void ApplyTranseptLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/23_torture_transept.map", StringComparison.Ordinal)) return;
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool on = mode.Contains("transeptlit") || lamp.Component.GetVariable("Lit") != "false";
                    if (mode.Contains("transeptoff")) on = false;
                    lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                    foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) own.SetFlickerActive(false);
                }
        }

        static void VerifyTranseptLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/23_torture_transept.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) saved.Add((light, light.Save()));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 25) throw new InvalidOperationException("Transept: expected 25 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            HplUserSettings Mode(int mode)
            {
                var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; return s;
            }
            float Factor(string name, int mode)
            {
                if (mode == 4) return 1f;
                if (name.StartsWith("SpotLight_", StringComparison.Ordinal)) return 0f;
                if (name is "PointLight_1" or "PointLight_2" or "PointLight_3" or "PointLight_4" or "PointLight_5" or "PointLight_6" or
                    "PointLight_20" or "PointLight_29" or "PointLight_30" or "PointLight_31" or "PointLight_32" or "PointLight_36" or
                    "PointLight_38" or "PointLight_40" or "PointLight_41") return 1f;
                if (name is "PointLight_33" or "PointLight_34" or "PointLight_35" or "PointLight_39") return .7f;
                if (name.StartsWith("PointLight_", StringComparison.Ordinal) && int.TryParse(name.Substring(11), out int number) && number >= 7 && number <= 19) return .3f;
                return mode is 0 or 2 ? 0f : .6f;
            }
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity, bool standalone)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * .3f;
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        light.GetComponent<Light>().intensity, light.GetComponentInParent<AmnesiaEntity>() == null));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    HplGraphics.Apply(Mode(mode));
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.standalone && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * Factor(e.light.name, mode)) > 1e-4f * Mathf.Max(1f, e.intensity)))
                            throw new InvalidOperationException("Transept: accent/connected fill/legacy light changed " + e.light.name);
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f; int ownCount = 0;
                        foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) { ownCount++; value += rendered.intensity; }
                        if (ownCount == 0 || (lamp.EffectsAlpha > 0f ? value <= 0f : value > .001f))
                            throw new InvalidOperationException("Transept: lamp state lost " + lamp.Name);
                    }
                    int sky = 0;
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded)
                        {
                            sky++;
                            if (light.LightLevelAt(light.transform.position, 0f) != 0f)
                                throw new InvalidOperationException("Transept: sky contributes to legacy HPL2 darkness");
                        }
                    if (sky != (mode == 4 ? 0 : 13)) throw new InvalidOperationException("Transept: sky sources accumulate or disappear");
                }
            }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                }
                int connections = 0;
                foreach (var (lamp, _) in lamps)
                {
                    string target = lamp.Component.GetVariable("ConnectedLight");
                    if (string.IsNullOrEmpty(target)) continue;
                    connections++; var fill = map.GetLight(target);
                    if (fill == null) throw new InvalidOperationException("Transept: missing connection " + target);
                    foreach (bool on in new[] { false, true })
                    {
                        lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                        if (on ? fill.CurrentColor.maxColorComponent <= fill.diffuseColor.maxColorComponent : fill.CurrentColor != fill.diffuseColor)
                            throw new InvalidOperationException("Transept: broken lamp connection " + lamp.Name);
                        VerifyModes();
                    }
                }
                if (connections != 12) throw new InvalidOperationException("Transept: expected twelve connections");
                var candle = (Props.PropLamp)map.GetEntityByName("candlestick_wall_1");
                void Advance(float step)
                {
                    candle.UpdateLogic(step);
                    foreach (var own in candle.Component.GetComponentsInChildren<AmnesiaLight>(true)) own.UpdateLight(step);
                }
                candle.SetLit(false, false); candle.UpdateLogic(0f); candle.SetLit(true, true); Advance(.5f); VerifyModes();
                float half = candle.EffectsAlpha; Advance(.5f); VerifyModes();
                if (half <= 0f || candle.EffectsAlpha <= half) throw new InvalidOperationException("Transept: candle ignition fade interrupted");
                candle.SetLit(false, true); Advance(.5f); VerifyModes(); Advance(.5f); VerifyModes();
                if (candle.EffectsAlpha > .001f) throw new InvalidOperationException("Transept: candle extinguishing fade interrupted");
                string line = "Transept state PASS: 25 lamp states, twelve connected fills with GI fallback, continuing one-second candle ignition/extinguishing and fifteen blood/torture/interaction accents survive RTGI, RT no-GI, SSGI, raster no-GI and classic. Thirteen window sources do not accumulate or affect legacy HPL2 darkness; visible-darkness readings are checked separately per view.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (light, data) in saved) light.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static readonly string[] ChoirWestOutsideLights = { "PointLight_11", "PointLight_10", "PointLight_9", "PointLight_8", "PointLight_4", "PointLight_5", "PointLight_6", "PointLight_7", "PointLight_12" };
        static readonly string[] ChoirWestOutsideLamps = {
            "candle_floor_no_light_21", "candle_floor_no_light_22", "candle_floor_no_light_23", "candle_floor_no_light_24", "candle_floor_7",
            "candle_floor_no_light_25", "candle_floor_no_light_26", "candle_floor_no_light_27", "candle_floor_no_light_28", "candle_floor_no_light_32",
            "candle_floor_no_light_36", "candle_floor_no_light_29", "candle_floor_2", "candle_floor_no_light_31", "candle_floor_no_light_33",
            "candle_floor_no_light_34", "candle_floor_no_light_35", "candle_floor_no_light_51", "candle_floor_no_light_50", "candle_floor_no_light_49",
            "candle_floor_no_light_48", "candle_floor_no_light_47", "candle_floor_no_light_46", "candle_floor_8", "candle_floor_no_light_52",
            "candle_floor_no_light_45", "candle_floor_no_light_44", "candle_floor_no_light_43" };

        static void SetChoirWestOutside(HplMap map, bool on)
        {
            foreach (string name in ChoirWestOutsideLights) map.GetLight(name).SetVisible(on);
            foreach (string name in ChoirWestOutsideLamps) ((Props.PropLamp)map.GetEntityByName(name)).SetLit(on, false);
        }

        static void ApplyChoirWestLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/24_torture_choir_west.map", StringComparison.Ordinal)) return;
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp) { lamp.SetLit(true, false); lamp.UpdateLogic(0f); }
            bool vision = mode.Contains("choirvision");
            SetChoirWestOutside(map, !vision);
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                    foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) own.SetFlickerActive(false);
            map.GetLight("PointLight_knife").SetColor(mode.Contains("choirknife") ? new Color(.8f, .1f, .1f, .7f) : Color.clear);
            map.GetLight("PointLight_chest").SetColor(mode.Contains("choirchest") ? new Color(.2f, .8f, .2f, .7f) : Color.clear);
            map.GetLight("PointLight_Blood").SetColor(mode.Contains("choirblood") ? new Color(.8f, .1f, .1f, .7f) : Color.clear);
            // Light endpoints only: no prisoner, dialogue, inventory, effects, door impulses or quest progression.
        }

        static void VerifyChoirWestLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/24_torture_choir_west.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) saved.Add((light, light.Save()));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 101) throw new InvalidOperationException("Choir west: expected 101 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            HplUserSettings Mode(int mode)
            {
                var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; return s;
            }
            float Factor(string name, int mode)
            {
                if (mode == 4) return 1f;
                if (name.StartsWith("SpotLight_", StringComparison.Ordinal)) return 0f;
                if (name is "PointLight_3" or "PointLight_4" or "PointLight_8" or "PointLight_9" or "PointLight_10" or "PointLight_11") return .7f;
                if (name is "PointLight_13" or "PointLight_14" or "PointLight_15" or "PointLight_16") return mode is 0 or 2 ? 0f : .6f;
                return 1f;
            }
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity, bool visible, bool standalone)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * .3f;
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        light.GetComponent<Light>().intensity, light.Visible, light.GetComponentInParent<AmnesiaEntity>() == null));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    HplGraphics.Apply(Mode(mode));
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.Visible != e.visible ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                            (e.standalone && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * Factor(e.light.name, mode)) > 1e-4f * Mathf.Max(1f, e.intensity)))
                            throw new InvalidOperationException("Choir west: ritual/visibility/legacy light changed " + e.light.name);
                    int noLight = 0;
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f; int ownCount = 0;
                        foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (own.TryGetComponent<Light>(out var rendered)) { ownCount++; value += rendered.intensity; }
                        if (ownCount == 0) { noLight++; continue; }
                        if (lamp.Lit ? value <= 0f : value > .001f) throw new InvalidOperationException("Choir west: lamp state lost " + lamp.Name);
                    }
                    if (noLight != 79) throw new InvalidOperationException("Choir west: expected 79 candles without owned lights");
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded) throw new InvalidOperationException("Choir west: unexpected sky source");
                }
            }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                }
                foreach (bool on in new[] { false, true, false, true })
                {
                    SetChoirWestOutside(map, on);
                    foreach (string name in ChoirWestOutsideLights)
                        if (map.GetLight(name).Visible != on) throw new InvalidOperationException("Choir west: outside visibility failed");
                    foreach (string name in ChoirWestOutsideLamps)
                        if (((Props.PropLamp)map.GetEntityByName(name)).Lit != on) throw new InvalidOperationException("Choir west: outside candle failed");
                    VerifyModes();
                }
                foreach (var pair in new[] { ("PointLight_knife", new Color(.8f, .1f, .1f, .7f)),
                    ("PointLight_chest", new Color(.2f, .8f, .2f, .7f)), ("PointLight_Blood", new Color(.8f, .1f, .1f, .7f)) })
                {
                    var light = map.GetLight(pair.Item1); light.SetColor(Color.clear);
                    light.FadeTo(pair.Item2, light.CurrentRadius, 1f); light.UpdateLight(.5f); VerifyModes();
                    if (light.CurrentColor == Color.clear || light.CurrentColor == pair.Item2) throw new InvalidOperationException("Choir west: missing fade midpoint");
                    light.UpdateLight(.5f); VerifyModes();
                    if (light.CurrentColor != pair.Item2) throw new InvalidOperationException("Choir west: ritual fade interrupted");
                    light.FadeTo(Color.clear, light.CurrentRadius, 1f); light.UpdateLight(.5f); VerifyModes(); light.UpdateLight(.5f); VerifyModes();
                    if (light.CurrentColor != Color.clear) throw new InvalidOperationException("Choir west: ritual fade-out interrupted");
                }
                string line = "Choir west state PASS: 101 lamps (22 owned sources, 79 no_light candles), nine outside lights and 28 vision candles, six continuing one-second knife/chest/blood fades survive RTGI, RT no-GI, SSGI, raster no-GI and classic. Original HPL2 light/visibility are preserved; no sky sources added. Visible-darkness checked separately per view.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (light, data) in saved) light.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static void ApplyChoirEastLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/24_torture_choir_east.map", StringComparison.Ordinal)) return;
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool on = mode.Contains("choirlit") || lamp.Component.GetVariable("Lit") != "false";
                    lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                    foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) own.SetFlickerActive(false);
                }
            foreach (string name in new[] { "PointLight_5", "PointLight_7", "PointLight_8", "PointLight_9" })
            {
                var light = map.GetLight(name);
                light.SetColor(name == "PointLight_5" ? (mode.Contains("choirpoison") ? new Color(0f, .6f, .8f, .8f) : light.diffuseColor) :
                    mode.Contains("choirorbs") ? Color.clear : light.diffuseColor);
            }
        }

        static void VerifyChoirEastLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/24_torture_choir_east.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) saved.Add((light, light.Save()));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 47) throw new InvalidOperationException("Choir east: expected 47 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            HplUserSettings Mode(int mode)
            {
                var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; return s;
            }
            float Factor(string name, int mode)
            {
                if (mode == 4 || name == "BoxLight_1") return 1f;
                if (name is "SpotLight_3" or "SpotLight_4" or "SpotLight_5" or "SpotLight_6" or "SpotLight_7") return 1f;
                if (name.StartsWith("SpotLight_", StringComparison.Ordinal)) return 0f;
                if (name is "PointLight_3" or "PointLight_5" or "PointLight_6" or "PointLight_7" or "PointLight_8" or "PointLight_9" or "PointLight_11" or "PointLight_17") return 1f;
                if (name is "PointLight_46" or "PointLight_47" or "PointLight_48" or "PointLight_49" or "PointLight_51" or "PointLight_52" or "PointLight_53" or "PointLight_80" or "PointLight_81") return .3f;
                if (name.StartsWith("PointLight_", StringComparison.Ordinal) && int.TryParse(name.Substring(11), out int number) &&
                    (number is 18 or 19 or 20 || number >= 55 && number <= 79)) return 1f;
                return mode is 0 or 2 ? 0f : .6f;
            }
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity, bool standalone)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * .3f;
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        light.TryGetComponent<Light>(out var rendered) ? rendered.intensity : -1f, light.GetComponentInParent<AmnesiaEntity>() == null));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    HplGraphics.Apply(Mode(mode));
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f || (e.light.box && e.light.enhancedIntensity != 1f) ||
                            (e.standalone && e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * Factor(e.light.name, mode)) > 1e-4f * Mathf.Max(1f, e.intensity)))
                            throw new InvalidOperationException("Choir east: accent/caustic/lamp fill/legacy light changed " + e.light.name);
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f; int own = 0;
                        foreach (var light in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (light.TryGetComponent<Light>(out var rendered)) { own++; value += rendered.intensity; }
                        if (own == 0 || (lamp.Lit ? value <= 0f : value > .001f)) throw new InvalidOperationException("Choir east: lamp state lost " + lamp.Name);
                    }
                    int sky = 0;
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded)
                        {
                            sky++;
                            if (light.LightLevelAt(light.transform.position, 0f) != 0f) throw new InvalidOperationException("Choir east: sky contributes to legacy darkness");
                        }
                    if (sky != (mode == 4 ? 0 : 5)) throw new InvalidOperationException("Choir east: sky sources accumulate or disappear");
                }
            }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                }
                int connections = 0; var targets = new HashSet<string>();
                foreach (var (lamp, _) in lamps)
                {
                    string target = lamp.Component.GetVariable("ConnectedLight");
                    if (string.IsNullOrEmpty(target)) continue;
                    connections++; targets.Add(target); var fill = map.GetLight(target);
                    if (fill == null) throw new InvalidOperationException("Choir east: missing connection " + target);
                    foreach (bool on in new[] { false, true })
                    {
                        lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                        if (on ? fill.CurrentColor.maxColorComponent <= fill.diffuseColor.maxColorComponent : fill.CurrentColor != fill.diffuseColor)
                            throw new InvalidOperationException("Choir east: broken lamp connection " + lamp.Name);
                        VerifyModes();
                    }
                }
                if (connections != 27 || targets.Count != 19) throw new InvalidOperationException("Choir east: expected 27 links with 19 targets");
                void Fade(AmnesiaLight light, Color target)
                {
                    Color start = light.CurrentColor; light.FadeTo(target, light.CurrentRadius, 2f); light.UpdateLight(1f); VerifyModes();
                    if (light.CurrentColor == start || light.CurrentColor == target) throw new InvalidOperationException("Choir east: missing fade midpoint");
                    light.UpdateLight(1f); VerifyModes();
                    if (light.CurrentColor != target) throw new InvalidOperationException("Choir east: scripted fade interrupted");
                }
                var poison = map.GetLight("PointLight_5"); poison.SetColor(Color.clear); Fade(poison, new Color(0f, .6f, .8f, .8f)); Fade(poison, Color.clear);
                foreach (string name in new[] { "PointLight_7", "PointLight_8", "PointLight_9" })
                { var light = map.GetLight(name); light.SetColor(light.diffuseColor); Fade(light, Color.clear); }
                string line = "Choir east state PASS: 47 lamp states, 27 connections with 19 targets (shared targets checked per lamp), five continuing two-second poison/orb fades, five caustic projectors and ambient survive RTGI, RT no-GI, SSGI, raster no-GI and classic. Five added apertures do not accumulate or affect legacy HPL2 darkness; visible-darkness checked separately per view.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (light, data) in saved) light.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static readonly string[] CellTunnelsGuardian = { "PointLight_10", "PointLight_11", "PointLight_12", "PointLight_13", "PointLight_14", "PointLight_15", "PointLight_16", "PointLight_17", "PointLight_18", "PointLight_19", "PointLight_22", "PointLight_23", "PointLight_24", "PointLight_38", "PointLight_39", "PointLight_40", "PointLight_53", "PointLight_54", "PointLight_59", "PointLight_60", "PointLight_61", "PointLight_62", "PointLight_63", "PointLight_64", "PointLight_65", "PointLight_66", "PointLight_67", "PointLight_68", "PointLight_69", "PointLight_7", "PointLight_71", "PointLight_72", "PointLight_74", "PointLight_75", "PointLight_76", "PointLight_77", "PointLight_78", "PointLight_79", "PointLight_8", "PointLight_80", "PointLight_81", "PointLight_9" };
        static readonly string[] CellTunnelsCheckpointPoints = { "PointLight_7", "PointLight_8", "PointLight_10", "PointLight_11", "PointLight_12", "PointLight_16", "PointLight_17", "PointLight_22", "PointLight_23" };
        static readonly string[] CellTunnelsCheckpointSpots = { "SpotLight_14", "SpotLight_9", "SpotLight_8", "SpotLight_7", "SpotLight_3", "SpotLight_2", "SpotLight_4", "SpotLight_5", "SpotLight_6" };

        static void ApplyCellTunnelsLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/25_cell_tunnels.map", StringComparison.Ordinal)) return;
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded && light.GetComponentInParent<AmnesiaEntity>() == null) light.SetColor(light.diffuseColor);
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool off = mode.Contains("cellstairsout") && (lamp.Name is "torch_static01_14" or "torch_static01_15" or "torch_static01_16");
                    lamp.SetLit(!off, false); lamp.UpdateLogic(0f);
                    foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) own.SetFlickerActive(false);
                }
            if (mode.Contains("cellguardian"))
                foreach (string name in CellTunnelsGuardian) map.GetLight(name).SetColor(new Color(1f, 0f, .2f, .6f));
            if (mode.Contains("cellcheckpoint"))
            {
                foreach (string name in CellTunnelsCheckpointPoints) map.GetLight(name).SetColor(new Color(0f, 0f, .1f, 0f));
                foreach (string name in CellTunnelsCheckpointSpots) map.GetLight(name).SetColor(Color.clear);
            }
            if (mode.Contains("cellstairsout"))
                foreach (string name in new[] { "PointLight_25", "PointLight_26", "PointLight_27" }) map.GetLight(name).SetColor(Color.clear);
            if (mode.Contains("celltools")) map.GetLight("PointLight_21").SetColor(new Color(.9f, .8f, .3f, .8f));
            if (mode.Contains("celljar")) map.GetLight("PointLight_58").SetColor(new Color(.3f, .4f, 1f, .8f));
            if (mode.Contains("cellending")) map.GetLight("PointLight_1").SetColor(new Color(1f, 0f, 0f, 1f));
            // Lighting endpoints only: no chase, damage, death, item pickup, cutscene, timer or quest progression.
        }

        static void VerifyCellTunnelsLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/25_cell_tunnels.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) saved.Add((light, light.Save()));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 11) throw new InvalidOperationException("Cell tunnels: expected 11 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            HplUserSettings Mode(int mode)
            {
                var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; return s;
            }
            float Factor(string name, int mode)
            {
                if (mode == 4) return 1f;
                if (name == "BoxLight_1") return mode is 0 or 2 ? .6f : .8f;
                if (name == "BoxLight_2") return 1f;
                if (name.StartsWith("SpotLight_", StringComparison.Ordinal) && int.Parse(name.Substring(10), CultureInfo.InvariantCulture) <= 16) return 0f;
                if (name.StartsWith("SpotLight_", StringComparison.Ordinal) || Array.IndexOf(CellTunnelsGuardian, name) >= 0 ||
                    name is "PointLight_1" or "PointLight_20" or "PointLight_21" or "PointLight_28" or "PointLight_58") return 1f;
                if (!name.StartsWith("PointLight_", StringComparison.Ordinal)) return 1f; // Player's auxiliary darkness light is not map fill.
                int number = int.Parse(name.Substring(11), CultureInfo.InvariantCulture);
                if (number >= 32 && number <= 52) return .3f;
                if (number == 73) return .6f;
                return mode is 0 or 2 ? 0f : .6f;
            }

            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity, bool standalone)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * .3f;
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        light.TryGetComponent<Light>(out var rendered) ? rendered.intensity : -1f, light.GetComponentInParent<AmnesiaEntity>() == null));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    HplGraphics.Apply(Mode(mode));
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f || (e.light.box && Mathf.Abs(e.light.enhancedIntensity - Factor(e.light.name, mode)) > 1e-5f) ||
                            (e.standalone && e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * Factor(e.light.name, mode)) > 1e-4f * Mathf.Max(1f, e.intensity)))
                            throw new InvalidOperationException("Cell tunnels: accent/caustic/lamp fill/legacy light changed " + e.light.name);
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f; int own = 0;
                        foreach (var light in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (light.TryGetComponent<Light>(out var rendered)) { own++; value += rendered.intensity; }
                        if (own == 0 || (lamp.Lit ? value <= 0f : value > .001f)) throw new InvalidOperationException("Cell tunnels: lamp state lost " + lamp.Name);
                    }
                    int sky = 0;
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded)
                        {
                            sky++;
                            if (light.TryGetComponent<HplEnhancedLightFollow>(out var follow))
                            {
                                // Execute the normal lifecycle synchronously, between graphics-setting changes.
                                follow.SendMessage("Start"); follow.SendMessage("LateUpdate");
                                Color c = follow.source.CurrentColor, original = follow.source.diffuseColor;
                                float expected = 150f * Mathf.GammaToLinearSpace(Mathf.Max(c.r, c.g, c.b)) / Mathf.GammaToLinearSpace(Mathf.Max(original.r, original.g, original.b));
                                var rendered = light.GetComponent<Light>();
                                if (Mathf.Abs(rendered.intensity - expected) > .001f || rendered.enabled != (expected > 0f))
                                    throw new InvalidOperationException("Cell tunnels: replacement shaft does not follow checkpoint fade " + light.name);
                            }
                            if (light.LightLevelAt(light.transform.position, 0f) != 0f) throw new InvalidOperationException("Cell tunnels: sky contributes to legacy darkness");
                        }
                    if (sky != (mode == 4 ? 0 : 18)) throw new InvalidOperationException("Cell tunnels: sky sources accumulate or disappear");
                }
            }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                }
                int connections = 0; var targets = new HashSet<string>();
                foreach (var (lamp, _) in lamps)
                {
                    string target = lamp.Component.GetVariable("ConnectedLight");
                    if (string.IsNullOrEmpty(target)) continue;
                    connections++; targets.Add(target); var fill = map.GetLight(target);
                    if (fill == null) throw new InvalidOperationException("Cell tunnels: missing connection " + target);
                    foreach (bool on in new[] { false, true })
                    {
                        lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                        if (on ? fill.CurrentColor.maxColorComponent <= fill.diffuseColor.maxColorComponent : fill.CurrentColor != fill.diffuseColor)
                            throw new InvalidOperationException("Cell tunnels: broken lamp connection " + lamp.Name);
                        VerifyModes();
                    }
                }
                if (connections != 1 || targets.Count != 1) throw new InvalidOperationException("Cell tunnels: expected one torch link to PointLight_15");
                void FadeBatch(string[] names, Func<string, Color> target, float duration)
                {
                    var starts = new Dictionary<string, Color>();
                    foreach (string name in names)
                    { var light = map.GetLight(name); starts[name] = light.CurrentColor; light.FadeTo(target(name), light.CurrentRadius, duration); light.UpdateLight(duration * .5f); }
                    VerifyModes();
                    foreach (string name in names)
                    {
                        var light = map.GetLight(name);
                        if (starts[name] != target(name) && (light.CurrentColor == starts[name] || light.CurrentColor == target(name)))
                            throw new InvalidOperationException("Cell tunnels: missing fade midpoint " + name);
                        light.UpdateLight(duration * .5f);
                    }
                    VerifyModes();
                    foreach (string name in names) if (map.GetLight(name).CurrentColor != target(name)) throw new InvalidOperationException("Cell tunnels: fade interrupted " + name);
                }
                FadeBatch(CellTunnelsGuardian, _ => new Color(1f, 0f, .2f, .6f), 1f);
                FadeBatch(CellTunnelsGuardian, name => map.GetLight(name).diffuseColor, .2f);
                FadeBatch(CellTunnelsCheckpointPoints, _ => new Color(0f, 0f, .1f, 0f), .1f);
                FadeBatch(CellTunnelsCheckpointSpots, _ => Color.clear, .1f);
                // The script's reset affects exactly these 16 points; other guardian red lights go dark.
                string[] blue = { "PointLight_7", "PointLight_14", "PointLight_8", "PointLight_18", "PointLight_9", "PointLight_10", "PointLight_11", "PointLight_12", "PointLight_13", "PointLight_15", "PointLight_17", "PointLight_23", "PointLight_24", "PointLight_22", "PointLight_16", "PointLight_19" };
                FadeBatch(blue, _ => new Color(.1f, .15f, .2f, 0f), .2f);
                foreach (string name in new[] { "PointLight_21", "PointLight_58" })
                {
                    map.GetLight(name).SetColor(Color.clear);
                    FadeBatch(new[] { name }, _ => name == "PointLight_21" ? new Color(.9f, .8f, .3f, .8f) : new Color(.3f, .4f, 1f, .8f), 1f);
                    FadeBatch(new[] { name }, _ => Color.clear, 1f);
                }
                foreach (string name in new[] { "torch_static01_14", "torch_static01_15", "torch_static01_16" })
                {
                    var lamp = (Props.PropLamp)map.GetEntityByName(name); lamp.SetLit(true, false); lamp.UpdateLogic(0f); lamp.SetLit(false, true);
                    foreach (float step in new[] { .5f, .5f })
                    {
                        lamp.UpdateLogic(step);
                        foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) own.UpdateLight(step);
                    }
                }
                FadeBatch(new[] { "PointLight_25", "PointLight_26", "PointLight_27" }, _ => Color.clear, 1f);
                FadeBatch(new[] { "PointLight_1" }, _ => new Color(1f, 0f, 0f, 1f), 4f);
                string line = "Cell tunnels state PASS: 11 lamp states, one torch/PointLight_15 link, 42 continuing guardian red-wave and reset fades, 16 blue reset points, nine checkpoint point and nine cone fades (including their physical shaft followers), three staircase lamps/fills, four one-second item/puzzle fades and four-second cell-ending red light survive RTGI, RT no-GI, SSGI, raster no-GI and classic. Eighteen added sources do not accumulate or affect legacy HPL2 darkness; visible darkness checked per view.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (light, data) in saved) light.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static readonly HashSet<string> NaveReduxProtectedLights = new() {
            "PointLight_2", "PointLight_4", "PointLight_5", "PointLight_6", "PointLight_7", "PointLight_8", "PointLight_11", "PointLight_12", "PointLight_13",
            "PointLight_16", "PointLight_17", "PointLight_18", "PointLight_19", "PointLight_20", "PointLight_21", "PointLight_22", "PointLight_23", "PointLight_24",
            "PointLight_26", "PointLight_27", "PointLight_28", "PointLight_64", "PointLight_69", "PointLight_72", "PointLight_73", "PointLight_85", "PointLight_86"
        };
        static readonly HashSet<string> NaveReduxNavigationLights = new() {
            "PointLight_3", "PointLight_9", "PointLight_10", "PointLight_15", "PointLight_29", "PointLight_33", "PointLight_36",
            "PointLight_40", "PointLight_54", "PointLight_66", "PointLight_71", "PointLight_75", "PointLight_76", "PointLight_79", "PointLight_81", "PointLight_98", "PointLight_99"
        };

        static void ApplyNaveReduxLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/26_torture_nave_redux.map", StringComparison.Ordinal)) return;
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded && light.GetComponentInParent<AmnesiaEntity>() == null) light.SetColor(light.diffuseColor);
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool on = mode.Contains("reduxalllit") || (!mode.Contains("reduxalloff") && lamp.Component.GetVariable("Lit") != "false");
                    lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                    foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) own.SetFlickerActive(false);
                }
            if (mode.Contains("reduxingredients"))
            {
                map.GetLight("PointLight_20").SetColor(new Color(.5f, .3f, .3f, .5f));
                map.GetLight("PointLight_24").SetColor(new Color(.8f, .1f, .1f, .5f));
                map.GetLight("PointLight_23").SetColor(new Color(.3f, .5f, .8f, .5f));
            }
            if (mode.Contains("reduxstringout")) map.GetLight("PointLight_23").SetColor(Color.clear);
            if (mode.Contains("reduxpoison")) map.GetLight("PointLight_12").SetColor(new Color(0f, .6f, .8f, .8f));
            // Light endpoints only. No ingredient activation/pickup, fungus break, machine use, Agrippa event or story timers.
        }

        static void VerifyNaveReduxLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/26_torture_nave_redux.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) saved.Add((light, light.Save()));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 42) throw new InvalidOperationException("Nave redux: expected 42 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            HplUserSettings Mode(int mode)
            {
                var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; return s;
            }
            float Factor(string name, int mode)
            {
                if (mode == 4 || name.StartsWith("BoxLight_", StringComparison.Ordinal)) return 1f;
                if (name.StartsWith("SpotLight_", StringComparison.Ordinal)) return 0f;
                if (NaveReduxProtectedLights.Contains(name)) return 1f;
                if (NaveReduxNavigationLights.Contains(name)) return .7f;
                if (name is "PointLight_34" or "PointLight_35" or "PointLight_37" or "PointLight_41" or "PointLight_42") return .3f;
                if (!name.StartsWith("PointLight_", StringComparison.Ordinal)) return 1f;
                return mode is 0 or 2 ? 0f : .6f;
            }
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity, bool standalone)>();
                foreach (var light in AmnesiaLight.All)
                {
                    if (light == null || light.handHeld || light.enhancedAdded) continue;
                    Vector3 sample = light.transform.position + light.transform.forward * .3f;
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        light.TryGetComponent<Light>(out var rendered) ? rendered.intensity : -1f, light.GetComponentInParent<AmnesiaEntity>() == null));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    HplGraphics.Apply(Mode(mode));
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f || (e.light.box && Mathf.Abs(e.light.enhancedIntensity - Factor(e.light.name, mode)) > 1e-5f) ||
                            (e.standalone && e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * Factor(e.light.name, mode)) > 1e-4f * Mathf.Max(1f, e.intensity)))
                            throw new InvalidOperationException("Nave redux: accent/caustic/lamp fill/legacy light changed " + e.light.name);
                    int noLight = 0;
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f; int own = 0;
                        foreach (var light in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true))
                            if (light.TryGetComponent<Light>(out var rendered)) { own++; value += rendered.intensity; }
                        if (own == 0) { noLight++; continue; }
                        if (lamp.Lit ? value <= 0f : value > .001f) throw new InvalidOperationException("Nave redux: lamp state lost " + lamp.Name);
                    }
                    if (noLight != 15) throw new InvalidOperationException("Nave redux: expected 15 candles without owned light");
                    int sky = 0;
                    foreach (var light in AmnesiaLight.All)
                        if (light != null && light.enhancedAdded)
                        {
                            sky++;
                            if (light.LightLevelAt(light.transform.position, 0f) != 0f) throw new InvalidOperationException("Nave redux: sky contributes to legacy darkness");
                        }
                    if (sky != (mode == 4 ? 0 : 2)) throw new InvalidOperationException("Nave redux: sky sources accumulate or disappear");
                }
            }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                    VerifyModes();
                }
                int effective = 0, inert = 0;
                foreach (var (lamp, _) in lamps)
                {
                    string target = lamp.Component.GetVariable("ConnectedLight");
                    if (string.IsNullOrEmpty(target)) continue;
                    var fill = map.GetLight(target);
                    if (fill == null) throw new InvalidOperationException("Nave redux: missing connection " + target);
                    bool hasLight = lamp.Component.GetComponentsInChildren<AmnesiaLight>(true).Length > 0;
                    if (hasLight) effective++; else inert++;
                    Color before = fill.CurrentColor;
                    foreach (bool on in new[] { false, true })
                    {
                        lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                        if (hasLight ? (on ? fill.CurrentColor == fill.diffuseColor : fill.CurrentColor != fill.diffuseColor) : fill.CurrentColor != before)
                            throw new InvalidOperationException("Nave redux: broken effective/inert lamp link " + lamp.Name);
                        VerifyModes();
                    }
                }
                if (effective != 1 || inert != 3) throw new InvalidOperationException("Nave redux: expected one effective and three inert no_light links");
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                void Fade(string name, Color target, float duration)
                {
                    var light = map.GetLight(name); Color start = light.CurrentColor;
                    light.FadeTo(target, light.CurrentRadius, duration); light.UpdateLight(duration * .5f); VerifyModes();
                    if (start != target && (light.CurrentColor == start || light.CurrentColor == target)) throw new InvalidOperationException("Nave redux: missing fade midpoint " + name);
                    light.UpdateLight(duration * .5f); VerifyModes();
                    if (light.CurrentColor != target) throw new InvalidOperationException("Nave redux: ingredient fade interrupted " + name);
                }
                Fade("PointLight_20", new Color(.5f, .3f, .3f, .5f), 1f);
                Fade("PointLight_24", new Color(.8f, .1f, .1f, .5f), 1f);
                Fade("PointLight_23", new Color(.3f, .5f, .8f, .5f), 1f);
                Fade("PointLight_23", Color.clear, 2f);
                Fade("PointLight_12", new Color(0f, .6f, .8f, .8f), 2f);
                Fade("PointLight_12", Color.clear, 2f);
                string line = "Nave redux state PASS: 42 lamp states (27 with owned lights, 15 no_light candles), one effective and three authored inert connections, six continuing one-/two-second blood/string/poison-gland fades survive RTGI, RT no-GI, SSGI, raster no-GI and classic. All original colours, radii, seven boxes and legacy HPL2 light are preserved; two added shafts do not accumulate or affect legacy darkness. Visible darkness checked separately per view.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (light, data) in saved) light.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static void ApplyZimmermanLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/26_zimmerman_vision.map", StringComparison.Ordinal)) return;
            // Endpoints of the authored effects only; no dialogue, rain sound, timers, movement locks or map change.
            map.effects.Sepia.FadeTo(mode.Contains("zimmervision") ? .5f : 0f, 100f);
            map.effects.Sepia.Update(1f);
            map.effects.RadialBlur.SetBlurStartDist(.2f);
            map.effects.RadialBlur.FadeTo(mode.Contains("zimmervision") ? .1f : 0f, 100f);
            map.effects.RadialBlur.Update(1f);
        }

        static void VerifyZimmermanLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/26_zimmerman_vision.map", StringComparison.Ordinal)) return;
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            var originals = new List<(AmnesiaLight light, Color color, float radius, bool visible, Vector3 position, Vector3 sample, float level, float intensity)>();
            HplGraphics.Apply(classic);
            int standalone = 0;
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || light.handHeld || light.enhancedAdded) continue;
                if (light.GetComponentInParent<AmnesiaEntity>() == null && !light.excludeFromLightLevel) standalone++;
                Vector3 sample = light.transform.position + light.transform.forward * .3f;
                originals.Add((light, light.CurrentColor, light.CurrentRadius, light.Visible, light.transform.position, sample,
                    light.LightLevelAt(sample, 0f), light.TryGetComponent<Light>(out var l) ? l.intensity : -1f));
            }
            if (standalone != 6) throw new InvalidOperationException("Zimmerman: expected six original map lights");
            foreach (var entity in map.Entities) if (entity is Props.PropLamp) throw new InvalidOperationException("Zimmerman: unexpected lamp");
            float sepia = map.effects.Sepia.Amount, blur = map.effects.RadialBlur.Size, startDist = map.effects.RadialBlur.StartDist;
            try
            {
                foreach (bool vision in new[] { false, true })
                {
                    ApplyZimmermanLightingPreview(map, vision ? "zimmervision" : "normal");
                    for (int cycle = 0; cycle < 2; cycle++)
                        for (int mode = 0; mode < 5; mode++)
                        {
                            var settings = classic.Copy(); settings.lightingSet = mode == 4 ? 0 : 1; settings.lighting = mode < 2 ? 1 : 0;
                            settings.rtGi = mode == 0 ? 2 : 0; settings.ssgi = mode == 2 ? 2 : 0;
                            HplGraphics.Apply(settings); HplEnhancedLighting.Refresh();
                            foreach (var e in originals)
                            {
                                float factor = mode == 4 || e.light.excludeFromLightLevel ? 1f : e.light.name == "SpotLight_1" ? 0f : mode is 0 or 2 ? 0f : .6f;
                                if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.Visible != e.visible ||
                                    e.light.transform.position != e.position || Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f ||
                                    (e.light.box && Mathf.Abs(e.light.enhancedIntensity - factor) > 1e-5f) ||
                                    (e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * factor) > 1e-4f * Mathf.Max(1f, e.intensity)))
                                    throw new InvalidOperationException("Zimmerman: original state/fallback/legacy light changed " + e.light.name);
                            }
                            if (map.effects.Sepia.Amount != (vision ? .5f : 0f) || map.effects.RadialBlur.Size != (vision ? .1f : 0f) || map.effects.RadialBlur.StartDist != .2f)
                                throw new InvalidOperationException("Zimmerman: changing graphics changed vision effects");
                            int sky = 0;
                            foreach (var light in AmnesiaLight.All)
                                if (light != null && light.enhancedAdded)
                                {
                                    sky++;
                                    var rendered = light.GetComponent<Light>();
                                    if (rendered.type != LightType.Rectangle || rendered.intensity != 3f || rendered.range != 120f || rendered.shadows == LightShadows.None || light.LightLevelAt(light.transform.position, 0f) != 0f)
                                        throw new InvalidOperationException("Zimmerman: invalid sky / legacy contribution");
                                }
                            if (sky != (mode == 4 ? 0 : 1)) throw new InvalidOperationException("Zimmerman: sky accumulates/disappears");
                        }
                }
                string line = "Zimmerman state PASS: six static map lights, no lamps or scripted light changes; two cycles through RTGI, RT no-GI, SSGI, raster no-GI and classic with/without authored sepia .5 and radial blur .1 preserve original colours/radii/visibility/legacy light and effect endpoints. One soft sky does not accumulate or affect legacy darkness. Story lantern and sanity drain are disabled by the original script; snapshots do not run story timers or map transitions.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                map.effects.Sepia.FadeTo(sepia, 100f); map.effects.Sepia.Update(1f);
                map.effects.RadialBlur.FadeTo(blur, 100f); map.effects.RadialBlur.Update(1f); map.effects.RadialBlur.SetBlurStartDist(startDist);
                HplGraphics.Apply(user);
            }
        }

        static readonly string[] ChancelReduxBarrierLights = {
            "barrier_pointlight_1", "barrier_pointlight_2", "barrier_pointlight_3", "barrier_spotlight_1",
            "barrier_pointlight_weak_1", "barrier_pointlight_weak_2", "barrier_pointlight_weak_3", "barrier_spotlight_weak_1"
        };

        static bool ChancelReduxExplosionCandle(Props.PropLamp lamp) => lamp.Name.StartsWith("candlestick_floor", StringComparison.Ordinal) || lamp.Name.StartsWith("candle_floor_", StringComparison.Ordinal);

        static void ApplyChancelReduxLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/27_torture_chancel_redux.map", StringComparison.Ordinal)) return;
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded && light.GetComponentInParent<AmnesiaEntity>() == null)
                { light.SetFlickerActive(false); light.SetColor(light.diffuseColor); light.SetRadius(light.radius); }
            bool blown = mode.Contains("reduxburst") || mode.Contains("reduxafterburst");
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool on = mode.Contains("reduxalllit") || lamp.Component.GetVariable("Lit") != "false";
                    if (mode.Contains("reduxorb") && lamp.Name == "orb_pedestal_1") on = true;
                    if (mode.Contains("reduxwestlit") && lamp.Name == "hanging_lantern_ceiling_3") on = true;
                    if (mode.Contains("reduxalloff") || blown && (ChancelReduxExplosionCandle(lamp) || lamp.Name == "orb_pedestal_1")) on = false;
                    lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                    foreach (var own in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) own.SetFlickerActive(false);
                }
            if (mode.Contains("reduxlow"))
                foreach (string name in ChancelReduxBarrierLights)
                { var l = map.GetLight(name); l.SetColor(l.flickerOffColor); l.SetRadius(l.flickerOffRadius); }
            if (mode.Contains("reduxweak") || mode.Contains("reduxbarrierout"))
            {
                for (int i = 1; i <= 3; i++) { var l = map.GetLight("barrier_pointlight_" + i); l.SetColor(Color.clear); l.SetRadius(0f); }
                var spot = map.GetLight("barrier_spotlight_1"); spot.SetColor(new Color(.4f, .4f, .6f, spot.CurrentColor.a)); spot.SetRadius(8f);
            }
            if (mode.Contains("reduxbarrierout"))
                foreach (string name in new[] { "barrier_pointlight_weak_1", "barrier_pointlight_weak_2", "barrier_pointlight_weak_3", "barrier_spotlight_weak_1" })
                { var l = map.GetLight(name); l.SetColor(Color.clear); l.SetRadius(0f); }
            if (blown)
            {
                var orb = map.GetLight("OrbLight"); orb.SetColor(mode.Contains("reduxafterburst") ? Color.clear : new Color(.3f, .6f, 1f, 1f)); orb.SetRadius(12f);
            }
            // Light endpoints only: preserve original weak-light GameObject inactivity, meshes, collision, timers and puzzle progression.
        }

        static void VerifyChancelReduxLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/27_torture_chancel_redux.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data, bool active)>();
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded) saved.Add((light, light.Save(), light.gameObject.activeSelf));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 53) throw new InvalidOperationException("Chancel redux: expected 53 lamps");
            if (map.GetLight("barrier_spotlight_2") != null || map.GetLight("barrier_spotlight_weak_2") != null)
                throw new InvalidOperationException("Chancel redux: authored missing spots unexpectedly appeared");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            float Factor(string name, int mode)
            {
                if (mode == 4 || name.StartsWith("barrier_", StringComparison.Ordinal) || name is "BoxLight_1" or "OrbLight" or "PointLight_2" or "PointLight_16") return 1f;
                if (name is "SpotLight_2" or "SpotLight_3") return 0f;
                if (name is "PointLight_1" or "PointLight_7" or "PointLight_21") return mode is 0 or 2 ? 0f : .6f;
                return 1f;
            }
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity, bool flicker)>();
                // Include the four initially inactive weak lights: setting changes must not activate or mutate them.
                foreach (var (light, _, active) in saved)
                {
                    if (light.gameObject.activeSelf != active) throw new InvalidOperationException("Chancel redux: original inactive object activated " + light.name);
                    Vector3 sample = light.transform.position + light.transform.forward * .3f;
                    sources.Add((light, light.CurrentColor, light.CurrentRadius, sample, light.LightLevelAt(sample, 0f),
                        light.GetComponentInParent<AmnesiaEntity>() == null && light.TryGetComponent<Light>(out var l) ? l.intensity : -1f, light.FlickerActive));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    var settings = classic.Copy(); settings.lightingSet = mode == 4 ? 0 : 1; settings.lighting = mode < 2 ? 1 : 0;
                    settings.rtGi = mode == 0 ? 2 : 0; settings.ssgi = mode == 2 ? 2 : 0; HplGraphics.Apply(settings);
                    foreach (var e in sources)
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.FlickerActive != e.flicker ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f || e.light.box && e.light.enhancedIntensity != 1f ||
                            e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * Factor(e.light.name, mode)) > 1e-4f * Mathf.Max(1f, e.intensity))
                            throw new InvalidOperationException("Chancel redux: original state/barrier/fallback/legacy light changed " + e.light.name);
                    int noLight = 0;
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f; int own = 0;
                        foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) if (l.TryGetComponent<Light>(out var rendered)) { own++; value += rendered.intensity; }
                        if (own == 0) { noLight++; continue; }
                        if (lamp.EffectsAlpha > 0f ? value <= 0f : value > .001f) throw new InvalidOperationException("Chancel redux: lamp effects lost " + lamp.Name);
                    }
                    if (noLight != 22) throw new InvalidOperationException("Chancel redux: expected 22 no_light candles");
                    foreach (var l in AmnesiaLight.All) if (l != null && l.enhancedAdded) throw new InvalidOperationException("Chancel redux: unexpected sky source");
                }
            }
            void Advance(Props.PropLamp lamp, float step)
            {
                lamp.UpdateLogic(step); foreach (var light in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) light.UpdateLight(step);
            }
            void Fade(string name, Color target, float radius, float duration)
            {
                var l = map.GetLight(name); l.SetFlickerActive(false); l.SetVisible(true); Color start = l.CurrentColor; float oldRadius = l.CurrentRadius;
                l.FadeTo(target, radius, duration); l.UpdateLight(duration * .5f); VerifyModes();
                if (l.CurrentColor == target && l.CurrentRadius == radius && (start != target || oldRadius != radius)) throw new InvalidOperationException("Chancel redux: missing fade midpoint " + name);
                l.UpdateLight(duration * .5f); VerifyModes();
                if (l.CurrentColor != target || l.CurrentRadius != radius) throw new InvalidOperationException("Chancel redux: fade interrupted " + name);
            }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true }) { foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); } VerifyModes(); }
                var linked = (Props.PropLamp)map.GetEntityByName("hanging_lantern_ceiling_3"); var fill = map.GetLight("PointLight_7");
                foreach (bool on in new[] { false, true })
                {
                    linked.SetLit(on, false); linked.UpdateLogic(0f);
                    if (on ? fill.CurrentColor == fill.diffuseColor : fill.CurrentColor != fill.diffuseColor) throw new InvalidOperationException("Chancel redux: broken ceiling-lantern link");
                    VerifyModes();
                }
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                var orb = (Props.PropLamp)map.GetEntityByName("orb_pedestal_1");
                orb.SetLit(true, true); Advance(orb, .5f); VerifyModes(); Advance(orb, .5f); VerifyModes();
                orb.SetLit(false, true); Advance(orb, .5f); VerifyModes(); Advance(orb, .5f); VerifyModes();
                foreach (string name in ChancelReduxBarrierLights)
                {
                    var l = map.GetLight(name); l.SetFlickerActive(true); var colours = new HashSet<Color>(); var radii = new HashSet<float>();
                    for (int step = 0; step < 4; step++) { l.UpdateLight(1f); colours.Add(l.CurrentColor); radii.Add(l.CurrentRadius); VerifyModes(); }
                    if (colours.Count < 2 || name.StartsWith("barrier_spotlight", StringComparison.Ordinal) && radii.Count < 2) throw new InvalidOperationException("Chancel redux: barrier flicker stopped " + name);
                    l.SetFlickerActive(false);
                }
                for (int i = 1; i <= 3; i++) Fade("barrier_pointlight_" + i, Color.clear, 0f, .5f);
                var strong = map.GetLight("barrier_spotlight_1"); Fade(strong.name, new Color(.4f, .4f, .6f, strong.CurrentColor.a), 8f, .5f);
                for (int i = 1; i <= 3; i++) Fade("barrier_pointlight_weak_" + i, Color.clear, 0f, .5f);
                Fade("barrier_spotlight_weak_1", Color.clear, 0f, .5f);
                int blown = 0;
                foreach (var (lamp, _) in lamps) if (ChancelReduxExplosionCandle(lamp)) { blown++; lamp.SetLit(false, false); lamp.UpdateLogic(0f); }
                if (blown != 26 || orb.Lit) throw new InvalidOperationException("Chancel redux: expected four floor stands and 22 candles blown out, pedestal off");
                VerifyModes(); Fade("OrbLight", new Color(.3f, .6f, 1f, 1f), 12f, .2f); Fade("OrbLight", Color.clear, 12f, 2f);
                string line = "Chancel redux state PASS: 53 lamp states (31 with owned light, 22 no_light), nearby lantern link, pedestal ignition/extinguishing, eight live barrier flicker colour/radius states and ten continuing barrier/orb fades survive RTGI, RT no-GI, SSGI, raster no-GI and classic. Four weak light objects remain inactive; the two missing spot 2 names remain absent. Twenty-six candles and pedestal extinguish; residual strong spot 1, original colours/radii/box/legacy HPL2 light are retained. No sky added; visible darkness checked per view.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (light, data, _) in saved) light.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static bool InnerSanctumStoryLight(string name) => name.StartsWith("Altar_", StringComparison.Ordinal) ||
            name.StartsWith("slime_entry_", StringComparison.Ordinal) || name is "PointLight_7" or "PointLight_11_slime";

        // Light-only snapshots: do not run damage, knife interactions, door collapse or progression callbacks.
        static void ApplyInnerSanctumLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/28_inner_sanctum.map", StringComparison.Ordinal)) return;
            bool touch = mode.Contains("sanctouch"), ground = mode.Contains("sancground"), hint = mode.Contains("sanchint");
            bool wall = mode.Contains("sancwall"), allOff = mode.Contains("sancalloff");
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool on = !allOff && !(lamp.Name.StartsWith("Altar_", StringComparison.Ordinal) && (touch || ground || hint || wall) &&
                        (!lamp.Name.Contains("_Light2_") || ground || hint || wall));
                    lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                    foreach (var light in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) light.SetFlickerActive(false);
                }
            foreach (var light in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!light.handHeld && !light.enhancedAdded && light.GetComponentInParent<AmnesiaEntity>() == null)
                { light.SetFlickerActive(false); light.SetColor(light.diffuseColor); light.SetRadius(light.radius); }
            void Set(string name, Color color) => map.GetLight(name).SetColor(color);
            for (int i = 1; i <= 5; i++)
            {
                string name = $"slime_entry_{i}_4"; Set(name, new Color(.6f, .1f, 0f, map.GetLight(name).CurrentColor.a));
                if (mode.Contains("sancslime")) { Set(name, new Color(.7f, 0f, .2f, 1f)); map.GetLight(name).SetRadius(4f); }
            }
            map.GetLight("PointLight_11_slime").SetRadius(mode.Contains("sanccollapse") ? 9f : 0f);
            map.GetLight("PointLight_7").SetRadius(mode.Contains("sanccollapse") ? 0f : 9f);
            for (int i = 1; i <= 2; i++)
            {
                string prefix = "Altar_" + i;
                if (touch || ground || hint || wall)
                {
                    Set(prefix + "_AltarLight", new Color(.6f, 0f, 0f, 1f)); Set(prefix + "_GroundLight", new Color(.8f, 0f, 0f, 1f));
                    foreach (int p in new[] { 1, 2 }) { var l = map.GetLight(prefix + "_LightPoint_" + p); l.SetColor(new Color(.15f, .05f, 0f, l.CurrentColor.a)); }
                }
                if (ground || hint || wall)
                {
                    Set(prefix + "_AltarLight", Color.clear); Set(prefix + "_GroundLight", new Color(.6f, 0f, 0f, 1f));
                    Set(prefix + "_HintLight", new Color(hint ? .9f : .3f, 0f, 0f, 1f));
                    var l = map.GetLight(prefix + "_LightPoint_3"); l.SetColor(new Color(.15f, .05f, 0f, l.CurrentColor.a));
                }
                if (wall)
                {
                    Set(prefix + "_WallLight", new Color(mode.Contains("sancwallout") ? 0f : .5f, 0f, 0f, 1f));
                    Set(prefix + "_GroundLight", Color.clear); Set(prefix + "_HintLight", Color.clear);
                }
            }
        }

        static void VerifyInnerSanctumLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/28_inner_sanctum.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data, bool active)>();
            foreach (var l in map.GetComponentsInChildren<AmnesiaLight>(true)) if (!l.handHeld && !l.enhancedAdded) saved.Add((l, l.Save(), l.gameObject.activeSelf));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 52) throw new InvalidOperationException("Inner sanctum: expected 52 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            int fades = 0, links = 0;
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, Vector3 sample, float level, float intensity, bool flicker)>();
                foreach (var (l, _, active) in saved)
                {
                    if (l.gameObject.activeSelf != active) throw new InvalidOperationException("Inner sanctum: active state changed " + l.name);
                    Vector3 sample = l.transform.position + l.transform.forward * .3f;
                    sources.Add((l, l.CurrentColor, l.CurrentRadius, sample, l.LightLevelAt(sample, 0f),
                        l.GetComponentInParent<AmnesiaEntity>() == null && l.TryGetComponent<Light>(out var rendered) ? rendered.intensity : -1f, l.FlickerActive));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                    s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; HplGraphics.Apply(s);
                    foreach (var e in sources)
                    {
                        float factor = mode == 4 || InnerSanctumStoryLight(e.light.name) ? 1f : mode is 0 or 2 ? 0f : .6f;
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.FlickerActive != e.flicker ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f || e.light.box && e.light.enhancedIntensity != 1f ||
                            e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * factor) > 1e-4f * Mathf.Max(1f, e.intensity))
                            throw new InvalidOperationException("Inner sanctum: source/story/fallback/legacy light changed " + e.light.name);
                    }
                    int noLight = 0;
                    foreach (var (lamp, _) in lamps)
                    {
                        float value = 0f; int own = 0;
                        foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) if (l.TryGetComponent<Light>(out var rendered)) { own++; value += rendered.intensity; }
                        if (own == 0) { noLight++; continue; }
                        if (lamp.EffectsAlpha > 0f ? value <= 0f : value > .001f) throw new InvalidOperationException("Inner sanctum: lamp effects lost " + lamp.Name);
                    }
                    if (noLight != 10) throw new InvalidOperationException("Inner sanctum: expected ten no_light candles");
                    foreach (var l in AmnesiaLight.All) if (l != null && l.enhancedAdded) throw new InvalidOperationException("Inner sanctum: unexpected sky source");
                }
            }
            void Advance(Props.PropLamp lamp, float dt)
            { lamp.UpdateLogic(dt); foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) l.UpdateLight(dt); }
            void Fade(string name, Color target, float radius, float duration)
            {
                var l = map.GetLight(name); l.SetFlickerActive(false); l.SetVisible(true); Color start = l.CurrentColor; float oldRadius = l.CurrentRadius;
                l.FadeTo(target, radius, duration); l.UpdateLight(duration * .5f); VerifyModes();
                if (l.CurrentColor == target && l.CurrentRadius == radius && (start != target || oldRadius != radius)) throw new InvalidOperationException("Inner sanctum: missing fade midpoint " + name);
                l.UpdateLight(duration * .5f); VerifyModes();
                if (l.CurrentColor != target || l.CurrentRadius != radius) throw new InvalidOperationException("Inner sanctum: fade interrupted " + name);
                fades++;
            }
            void ColourFade(string name, float red, float green, float blue, float alpha, float duration)
            { var l = map.GetLight(name); Fade(name, new Color(red, green, blue, alpha < 0f ? l.CurrentColor.a : alpha), l.CurrentRadius, duration); }
            void Extinguish(string group)
            {
                foreach (var (lamp, _) in lamps) if (lamp.Name.StartsWith(group, StringComparison.Ordinal)) lamp.SetLit(false, true);
                foreach (var (lamp, _) in lamps) if (lamp.Name.StartsWith(group, StringComparison.Ordinal)) Advance(lamp, .5f);
                VerifyModes();
                foreach (var (lamp, _) in lamps) if (lamp.Name.StartsWith(group, StringComparison.Ordinal)) Advance(lamp, .5f);
                VerifyModes();
            }
            try
            {
                ApplyInnerSanctumLightingPreview(map, "initial");
                VerifyModes();
                foreach (bool on in new[] { false, true }) { foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); } VerifyModes(); }
                foreach (var (lamp, _) in lamps)
                {
                    string name = lamp.Component.GetVariable("ConnectedLight");
                    if (string.IsNullOrEmpty(name) || lamp.Component.GetComponentsInChildren<AmnesiaLight>(true).Length == 0) continue;
                    var target = map.GetLight(name);
                    foreach (bool on in new[] { false, true })
                    {
                        lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                        if (on ? target.CurrentColor == target.diffuseColor : target.CurrentColor != target.diffuseColor) throw new InvalidOperationException("Inner sanctum: broken lamp link " + lamp.Name);
                        VerifyModes();
                    }
                    links++;
                }
                for (int i = 1; i <= 5; i++) ColourFade($"slime_entry_{i}_4", .7f, 0f, .2f, 1f, 1f);
                Fade("PointLight_11_slime", map.GetLight("PointLight_11_slime").CurrentColor, 9f, 1f);
                Fade("PointLight_7", map.GetLight("PointLight_7").CurrentColor, 0f, 1f);
                for (int i = 1; i <= 2; i++)
                {
                    string prefix = "Altar_" + i;
                    Extinguish(prefix + "_Light_");
                    ColourFade(prefix + "_AltarLight", .6f, 0f, 0f, 1f, 1f); ColourFade(prefix + "_GroundLight", .8f, 0f, 0f, 1f, 1f);
                    for (int p = 1; p <= 2; p++) ColourFade(prefix + "_LightPoint_" + p, .15f, .05f, 0f, -1f, 1f);
                    Extinguish(prefix + "_Light2_");
                    ColourFade(prefix + "_LightPoint_3", .15f, .05f, 0f, -1f, 1f);
                    ColourFade(prefix + "_AltarLight", 0f, 0f, 0f, 0f, 1f); ColourFade(prefix + "_GroundLight", .6f, 0f, 0f, 1f, 1f);
                    ColourFade(prefix + "_HintLight", .3f, 0f, 0f, 1f, .5f); ColourFade(prefix + "_HintLight", .9f, 0f, 0f, -1f, 2f);
                    ColourFade(prefix + "_HintLight", .3f, 0f, 0f, -1f, .5f); ColourFade(prefix + "_WallLight", .5f, 0f, 0f, 1f, 2f);
                    ColourFade(prefix + "_GroundLight", 0f, 0f, 0f, 0f, 2f); ColourFade(prefix + "_HintLight", 0f, 0f, 0f, 0f, 2f);
                    ColourFade(prefix + "_WallLight", 0f, 0f, 0f, 1f, 2f);
                }
                if (fades != 35 || links != 10) throw new InvalidOperationException("Inner sanctum: incomplete fade/link coverage");
                string line = "Inner sanctum state PASS: 52 lamps (42 with owned light, ten no_light), ten effective links to four altar fills, all-lamp shutdown/relight, altar lamp effect fades and 35 continuing colour/radius fades survive RTGI, RT no-GI, SSGI, raster no-GI and classic. Slime entry, corridor swap, both altar/ground/hint pulses and wall boxes retain full authored state and legacy HPL2 light. No sky added; visible darkness checked per view. Light-only checks do not execute story progression.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (light, data, _) in saved) light.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static readonly string[] OrbChamberTorches = { "PointLight_9", "PointLight_8", "PointLight_10", "PointLight_11", "PointLight_12", "PointLight_19", "PointLight_14", "PointLight_15" };
        static readonly string[] OrbChamberBeams = { "PointLight_28", "PointLight_29", "PointLight_30", "PointLight_33", "PointLight_38", "PointLight_40" };
        static readonly HashSet<string> OrbChamberFills = new() { "PointLight_16", "PointLight_17", "PointLight_20", "PointLight_21", "PointLight_23", "PointLight_27", "PointLight_48", "PointLight_57", "PointLight_63", "PointLight_64", "PointLight_71", "PointLight_72", "PointLight_73", "PointLight_74", "PointLight_99" };

        static void ApplyOrbChamberLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/29_orb_chamber.map", StringComparison.Ordinal)) return;
            bool slime = mode.Contains("orbslime"), torchOut = slime || mode.Contains("orbtorchout");
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool on = mode.Contains("orballlit") || lamp.Component.GetVariable("Lit") != "false";
                    if (mode.Contains("orballoff") || torchOut && lamp.Name.StartsWith("torch_floor_", StringComparison.Ordinal)) on = false;
                    lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                    foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) l.SetFlickerActive(false);
                }
            foreach (var l in map.GetComponentsInChildren<AmnesiaLight>(true))
                if (!l.handHeld && !l.enhancedAdded && l.GetComponentInParent<AmnesiaEntity>() == null)
                { l.StopFading(); l.SetFlickerActive(false); l.SetColor(l.diffuseColor); l.SetRadius(l.radius); }
            if (mode.Contains("orbflash")) map.GetLight("PointLight_47").SetColor(new Color(.2f, .2f, 1f, 1f));
            if (mode.Contains("orbrodsout") || mode.Contains("orbportal")) foreach (string name in OrbChamberBeams) map.GetLight(name).SetColor(Color.clear);
            if (mode.Contains("orbportal")) map.GetLight("PointLight_32").SetColor(mode.Contains("orbportalout") ? Color.clear : new Color(.3f, .8f, 1f, 1f));
            if (torchOut) foreach (string name in OrbChamberTorches) map.GetLight(name).SetColor(Color.clear);
            if (slime)
            {
                foreach (string name in new[] { "PointLight_18", "PointLight_2" }) { map.GetLight(name).SetColor(new Color(1f, 0f, .2f, 1f)); map.GetLight(name).SetRadius(6f); }
                map.GetLight("PointLight_32").SetColor(new Color(1f, 0f, .2f, 1f));
                foreach (string name in new[] { "PointLight_28", "PointLight_29" }) map.GetLight(name).SetColor(new Color(1f, 0f, .2f, .7f));
            }
        }

        static void VerifyOrbChamberLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/29_orb_chamber.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data, bool active)>();
            foreach (var l in map.GetComponentsInChildren<AmnesiaLight>(true)) if (!l.handHeld && !l.enhancedAdded) saved.Add((l, l.Save(), l.gameObject.activeSelf));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 32) throw new InvalidOperationException("Orb chamber: expected 32 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            int fades = 0;
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, bool visible, bool flicker, Vector3 position, Vector3 sample, float level, float intensity)>();
                foreach (var (l, _, active) in saved)
                {
                    if (l.gameObject.activeSelf != active) throw new InvalidOperationException("Orb chamber: active state changed " + l.name);
                    Vector3 sample = l.transform.position + l.transform.forward * .3f;
                    sources.Add((l, l.CurrentColor, l.CurrentRadius, l.Visible, l.FlickerActive, l.transform.position, sample, l.LightLevelAt(sample, 0f),
                        l.GetComponentInParent<AmnesiaEntity>() == null && l.TryGetComponent<Light>(out var rendered) ? rendered.intensity : -1f));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                    s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; HplGraphics.Apply(s);
                    int duplicates = 0;
                    foreach (var e in sources)
                    {
                        bool selected = map.GetLight(e.light.name) == e.light;
                        int torch = selected ? Array.IndexOf(OrbChamberTorches, e.light.name) : -1;
                        float factor = mode == 4 || !selected ? 1f : torch >= 0 ? 4f : e.light.name == "SpotLight_25" ? 0f : OrbChamberFills.Contains(e.light.name) ? (mode is 0 or 2 ? 0f : .6f) : 1f;
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.Visible != e.visible || e.light.FlickerActive != e.flicker ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f || e.light.box && e.light.enhancedIntensity != 1f ||
                            e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * factor) > 1e-4f * Mathf.Max(1f, e.intensity))
                            throw new InvalidOperationException("Orb chamber: source/fade/fallback/legacy light changed " + e.light.name);
                        if (e.intensity >= 0f)
                        {
                            Vector3 expected = mode != 4 && torch >= 0 ? ((Props.PropLamp)map.GetEntityByName("torch_floor_no_light_" + (torch + 1))).Component.transform.position + Vector3.up * 1.25611f : e.position;
                            if ((e.light.transform.position - expected).sqrMagnitude > 1e-7f) throw new InvalidOperationException("Orb chamber: torch/duplicate placement changed " + e.light.name);
                            if (!selected) duplicates++;
                        }
                    }
                    if (duplicates != 25) throw new InvalidOperationException("Orb chamber: expected 25 later duplicate sources");
                    int noLight = 0;
                    foreach (var (lamp, _) in lamps)
                    {
                        int own = 0; float value = 0f;
                        foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) if (l.TryGetComponent<Light>(out var rendered)) { own++; value += rendered.intensity; }
                        if (own == 0) { noLight++; continue; }
                        if (lamp.EffectsAlpha > 0f ? value <= 0f : value > .001f) throw new InvalidOperationException("Orb chamber: lamp effects lost " + lamp.Name);
                    }
                    if (noLight != 8) throw new InvalidOperationException("Orb chamber: expected eight no_light torches");
                    int added = 0;
                    foreach (var l in AmnesiaLight.All) if (l != null && l.enhancedAdded) { added++; if (l.LightLevelAt(l.transform.position, 0f) != 0f) throw new InvalidOperationException("Orb chamber: sky affects legacy gameplay light"); }
                    if (added != (mode == 4 ? 0 : 4)) throw new InvalidOperationException("Orb chamber: window light accumulation / visibility");
                }
            }
            void Fade(string name, Color target, float radius, float duration)
            {
                var l = map.GetLight(name); l.SetFlickerActive(false); l.SetVisible(true); Color start = l.CurrentColor; float oldRadius = l.CurrentRadius;
                l.FadeTo(target, radius, duration); l.UpdateLight(duration * .5f); VerifyModes();
                if (l.CurrentColor == target && l.CurrentRadius == radius && (start != target || oldRadius != radius)) throw new InvalidOperationException("Orb chamber: missing midpoint " + name);
                l.UpdateLight(duration * .5f); VerifyModes();
                if (l.CurrentColor != target || l.CurrentRadius != radius) throw new InvalidOperationException("Orb chamber: fade interrupted " + name);
                fades++;
            }
            void ColourFade(string name, Color color, float duration) => Fade(name, color, map.GetLight(name).CurrentRadius, duration);
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true }) { foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); } VerifyModes(); }
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (float speed in new[] { 1f, 1.5f, 2f, 4f })
                { ColourFade("PointLight_47", new Color(.2f, .2f, 1f, 1f), 1f / speed); ColourFade("PointLight_47", Color.clear, 1f / speed); }
                foreach (string name in new[] { "PointLight_26", "PointLight_32", "BoxLight_2", "BoxLight_3" })
                {
                    var l = map.GetLight(name); l.SetFlickerActive(true); var colours = new HashSet<Color>();
                    for (int step = 0; step < 8; step++) { l.UpdateLight(.2f); colours.Add(l.CurrentColor); VerifyModes(); }
                    if (colours.Count < 2) throw new InvalidOperationException("Orb chamber: live flicker stopped " + name);
                    l.SetFlickerActive(false); l.StopFading(); l.SetColor(l.diffuseColor); l.SetRadius(l.radius);
                }
                // Rod order differs from the portal's list: all three two-light groups still extinguish.
                foreach (string name in new[] { "PointLight_29", "PointLight_40", "PointLight_38", "PointLight_28", "PointLight_33", "PointLight_30" }) ColourFade(name, Color.clear, 1f);
                foreach (string name in OrbChamberBeams) { var l = map.GetLight(name); l.SetColor(l.diffuseColor); ColourFade(name, Color.clear, 1f); }
                ColourFade("PointLight_32", new Color(.3f, .8f, 1f, 1f), 3f); ColourFade("PointLight_32", Color.clear, 2f);
                foreach (var (lamp, _) in lamps) if (lamp.Name.StartsWith("torch_floor_", StringComparison.Ordinal)) { lamp.SetLit(false, true); lamp.UpdateLogic(.5f); }
                VerifyModes();
                foreach (var (lamp, _) in lamps) if (lamp.Name.StartsWith("torch_floor_", StringComparison.Ordinal)) lamp.UpdateLogic(.5f);
                foreach (string name in OrbChamberTorches) ColourFade(name, Color.clear, 1f);
                Fade("PointLight_18", new Color(1f, 0f, .2f, 1f), 6f, 3f); ColourFade("PointLight_32", new Color(1f, 0f, .2f, 1f), 3f);
                foreach (string name in new[] { "PointLight_28", "PointLight_29" }) ColourFade(name, new Color(1f, 0f, .2f, .7f), 3f);
                Fade("PointLight_2", new Color(1f, 0f, .2f, 1f), 6f, 3f);
                if (fades != 35) throw new InvalidOperationException("Orb chamber: incomplete fade coverage");
                string line = "Orb chamber state PASS: 32 lamp states (24 with owned light, eight no_light torches), eight map sources moved to real flames with authored extinction, four live flickers and 35 continuing countdown/rod/portal/torch/red-slime fades across RTGI, RT no-GI, SSGI, raster no-GI and classic. All 25 later duplicate sources retain placement and intensity; five boxes and original RGBA/radii/visibility/legacy HPL2 light survive. Four window lights do not accumulate or enter legacy light. Light-only checks do not run endings or credits.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (l, data, _) in saved) l.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static readonly HashSet<string> CellsNiches = new() { "PointLight_1", "PointLight_2", "PointLight_6", "PointLight_8", "PointLight_10", "PointLight_12", "PointLight_14", "PointLight_16", "PointLight_18", "PointLight_20", "PointLight_30" };
        static readonly HashSet<string> CellsAccents = new() { "PointLight_39", "PointLight_41", "PointLight_54", "PointLight_56", "PointLight_57", "PointLight_58" };

        static void ApplyJustineCellsLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/ptest/01_cells.map", StringComparison.Ordinal)) return;
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    lamp.SetLit(!mode.Contains("cellsalloff") && (mode.Contains("cellsalllit") || lamp.Component.GetVariable("Lit") != "false"), false);
                    lamp.UpdateLogic(0f);
                    foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) l.SetFlickerActive(false);
                }
            var signal = map.GetLight("PointLight_58"); signal.StopFading(); signal.SetFlickerActive(false);
            signal.SetColor(signal.diffuseColor); signal.SetRadius(signal.radius); signal.SetVisible(!mode.Contains("cellslever"));
        }

        static void VerifyJustineCellsLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/ptest/01_cells.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data, bool active)>();
            foreach (var l in map.GetComponentsInChildren<AmnesiaLight>(true)) if (!l.handHeld && !l.enhancedAdded) saved.Add((l, l.Save(), l.gameObject.activeSelf));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 70) throw new InvalidOperationException("Justine cells: expected 70 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, bool visible, bool flicker, Vector3 sample, float level, float intensity)>();
                foreach (var (l, _, active) in saved)
                {
                    if (l.gameObject.activeSelf != active) throw new InvalidOperationException("Justine cells: active state changed " + l.name);
                    Vector3 sample = l.transform.position + l.transform.forward * .3f;
                    sources.Add((l, l.CurrentColor, l.CurrentRadius, l.Visible, l.FlickerActive, sample, l.LightLevelAt(sample, 0f),
                        l.GetComponentInParent<AmnesiaEntity>() == null && l.TryGetComponent<Light>(out var rendered) ? rendered.intensity : -1f));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                    s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; HplGraphics.Apply(s);
                    foreach (var e in sources)
                    {
                        string name = e.light.name;
                        float factor = mode == 4 || e.light.box || CellsAccents.Contains(name) || name == "SpotLight_18" ? 1f :
                            name.StartsWith("SpotLight_", StringComparison.Ordinal) ? 0f : name == "PointLight_40" ? 4f : CellsNiches.Contains(name) ? .3f : name == "PointLight_33" ? .7f : mode is 0 or 2 ? 0f : .6f;
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.Visible != e.visible || e.light.FlickerActive != e.flicker ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f || e.light.box && e.light.enhancedIntensity != 1f ||
                            e.intensity >= 0f && Mathf.Abs(e.light.GetComponent<Light>().intensity - e.intensity * factor) > 1e-4f * Mathf.Max(1f, e.intensity))
                            throw new InvalidOperationException("Justine cells: source/visibility/fallback/legacy light changed " + name);
                    }
                    int noLight = 0;
                    foreach (var (lamp, _) in lamps)
                    {
                        int own = 0; float value = 0f;
                        foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) if (l.TryGetComponent<Light>(out var rendered)) { own++; value += rendered.intensity; }
                        if (own == 0) { noLight++; continue; }
                        if (lamp.EffectsAlpha > 0f ? value <= 0f : value > .001f) throw new InvalidOperationException("Justine cells: lamp effects lost " + lamp.Name);
                    }
                    if (noLight != 44) throw new InvalidOperationException("Justine cells: expected 43 no_light candles and stove without owned light");
                    int added = 0;
                    foreach (var l in AmnesiaLight.All) if (l != null && l.enhancedAdded) { added++; if (l.LightLevelAt(l.transform.position, 0f) != 0f) throw new InvalidOperationException("Justine cells: sky affects legacy light"); }
                    if (added != (mode == 4 ? 0 : 11)) throw new InvalidOperationException("Justine cells: aperture light accumulation");
                }
            }
            void Advance(Props.PropLamp lamp, float dt) { lamp.UpdateLogic(dt); foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) l.UpdateLight(dt); }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true }) { foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); } VerifyModes(); }
                foreach (bool on in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) lamp.SetLit(on, true);
                    foreach (var (lamp, _) in lamps) Advance(lamp, .5f); VerifyModes();
                    foreach (var (lamp, _) in lamps) Advance(lamp, .5f); VerifyModes();
                }
                var candle = (Props.PropLamp)map.GetEntityByName("candle_floor_6"); var connected = map.GetLight("PointLight_55");
                foreach (bool on in new[] { false, true })
                {
                    candle.SetLit(on, false); candle.UpdateLogic(0f);
                    if (on ? connected.CurrentColor == connected.diffuseColor : connected.CurrentColor != connected.diffuseColor) throw new InvalidOperationException("Justine cells: prisoner candle connection lost");
                    VerifyModes();
                }
                var signal = map.GetLight("PointLight_58"); signal.SetFlickerActive(true); var colours = new HashSet<Color>();
                for (int step = 0; step < 8; step++) { signal.UpdateLight(1f); colours.Add(signal.CurrentColor); VerifyModes(); }
                if (colours.Count < 2) throw new InvalidOperationException("Justine cells: red mechanism flicker stopped");
                foreach (bool visible in new[] { false, true }) { signal.SetVisible(visible); VerifyModes(); }
                string line = "Justine cells state PASS: 70 lamps (26 with owned light, 43 no_light candles and stove without owned light; initially 68 on, two off), all-lamp shutdown/relight and effect fades, prisoner candle link to point 55, live red point 58 flicker and scripted visibility switch survive RTGI, RT no-GI, SSGI, raster no-GI and classic. Original RGBA/radii/visibility/active states, ten boxes and legacy HPL2 light retained. Stove map point remains direct fire with GI. Eleven aperture lights do not accumulate or enter legacy light. Light-only checks do not execute prisoner harm, ladders, suitors, perma death or progression.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (l, data, _) in saved) l.Restore(data);
                HplGraphics.Apply(user);
            }
        }

        static readonly HashSet<string> LibraryProtected = new() { "PointLight_3", "SpotLight_4", "PointLight_2", "PointLight_9", "PointLight_43", "PointLight_44", "SpotLight_2", "SpotLight_no_stencil", "SpotLight_stencil01", "SpotLight_stencil02", "SpotLight_stencil03", "SpotLight_stencil04" };

        static void ApplyJustineLibraryLightingPreview(HplMap map, string mode)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/ptest/02_library.map", StringComparison.Ordinal)) return;
            foreach (var entity in map.Entities)
                if (entity is Props.PropLamp lamp)
                {
                    bool wind = mode.Contains("libwind") && (lamp.Name == "torch_static01_4" || lamp.Name == "torch_static01_6" || lamp.Name == "torch_static01_8");
                    lamp.SetLit(!wind && !mode.Contains("liballoff") && (mode.Contains("liballlit") || lamp.Component.GetVariable("Lit") != "false" || mode.Contains("libbox") && lamp.Name == "light_box_1"), false);
                    lamp.UpdateLogic(0f);
                    foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) l.SetFlickerActive(false);
                }
            foreach (string name in LibraryProtected)
            {
                var l = map.GetLight(name); l.StopFading(); l.SetFlickerActive(false); l.SetColor(l.diffuseColor); l.SetRadius(l.radius);
                if (mode.Contains("libshelf") && name == "PointLight_44") { l.SetColor(Color.clear); l.SetRadius(0f); }
                if (!mode.Contains("libbox")) continue;
                if (name is "PointLight_2" or "PointLight_9" or "SpotLight_2") l.SetColor(mode.Contains("libauxoff") ? Color.clear : new Color(.3f, .3f, 1f, .4f));
                if (name.StartsWith("SpotLight_", StringComparison.Ordinal) && name != "SpotLight_2" &&
                    name == (mode.Contains("libbox1") ? "SpotLight_stencil01" : mode.Contains("libbox2") ? "SpotLight_stencil02" : mode.Contains("libbox3") ? "SpotLight_stencil03" : mode.Contains("libbox4") ? "SpotLight_stencil04" : "SpotLight_no_stencil"))
                    l.SetColor(new Color(.5f, .6f, .9f, .5f));
            }
        }

        static void VerifyJustineLibraryLighting(HplMap map, HplUserSettings user, StringBuilder report)
        {
            if (!map.mapFile.Replace('\\', '/').EndsWith("/ptest/02_library.map", StringComparison.Ordinal)) return;
            var saved = new List<(AmnesiaLight light, AmnesiaLight.SaveData data, bool active)>();
            foreach (var l in map.GetComponentsInChildren<AmnesiaLight>(true)) if (!l.handHeld && !l.enhancedAdded) saved.Add((l, l.Save(), l.gameObject.activeSelf));
            var lamps = new List<(Props.PropLamp lamp, bool lit)>();
            foreach (var entity in map.Entities) if (entity is Props.PropLamp lamp) lamps.Add((lamp, lamp.Lit));
            if (lamps.Count != 57) throw new InvalidOperationException("Justine library: expected 57 lamps");
            var classic = user.Copy(); classic.lightingSet = 0; classic.ssgi = 0;
            void VerifyModes()
            {
                HplGraphics.Apply(classic);
                var sources = new List<(AmnesiaLight light, Color color, float radius, bool visible, bool flicker, Vector3 sample, float level, float intensity, Texture cookie, float fov, float aspect, bool shadows)>();
                foreach (var (l, _, active) in saved)
                {
                    if (l.gameObject.activeSelf != active) throw new InvalidOperationException("Justine library: active state changed " + l.name);
                    Vector3 sample = l.transform.position + l.transform.forward * .3f;
                    l.TryGetComponent<Light>(out var rendered);
                    sources.Add((l, l.CurrentColor, l.CurrentRadius, l.Visible, l.FlickerActive, sample, l.LightLevelAt(sample, 0f),
                        l.GetComponentInParent<AmnesiaEntity>() == null && rendered != null ? rendered.intensity : -1f,
                        rendered != null ? rendered.cookie : null, l.spotFov, l.spotAspect, l.castShadows));
                }
                for (int mode = 0; mode < 5; mode++)
                {
                    var s = classic.Copy(); s.lightingSet = mode == 4 ? 0 : 1; s.lighting = mode < 2 ? 1 : 0;
                    s.rtGi = mode == 0 ? 2 : 0; s.ssgi = mode == 2 ? 2 : 0; HplGraphics.Apply(s);
                    foreach (var e in sources)
                    {
                        string name = e.light.name;
                        float factor = mode == 4 ? 1f : name == "PointLight_9" ? .15f : name == "SpotLight_2" ? .3f : e.light.box || LibraryProtected.Contains(name) ? 1f :
                            name.StartsWith("SpotLight_", StringComparison.Ordinal) ? 0f : name is "PointLight_34" or "PointLight_36" ? .3f :
                            name is "PointLight_15" or "PointLight_16" ? .7f : mode is 0 or 2 ? 0f : .6f;
                        e.light.TryGetComponent<Light>(out var rendered);
                        if (e.light.CurrentColor != e.color || e.light.CurrentRadius != e.radius || e.light.Visible != e.visible || e.light.FlickerActive != e.flicker ||
                            e.light.spotFov != e.fov || e.light.spotAspect != e.aspect || e.intensity >= 0f && e.light.castShadows != e.shadows || rendered != null && rendered.cookie != e.cookie ||
                            Mathf.Abs(e.light.LightLevelAt(e.sample, 0f) - e.level) > 1e-5f || e.light.box && e.light.enhancedIntensity != 1f ||
                            e.intensity >= 0f && Mathf.Abs(rendered.intensity - e.intensity * factor) > 1e-4f * Mathf.Max(1f, e.intensity))
                            throw new InvalidOperationException("Justine library: source/cookie/fallback/legacy light changed " + name);
                    }
                    int empty = 0;
                    foreach (var (lamp, _) in lamps)
                    {
                        int own = 0; float value = 0f;
                        foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) if (l.TryGetComponent<Light>(out var rendered)) { own++; value += rendered.intensity; }
                        if (own == 0) { empty++; if (lamp.Name != "light_box_1") throw new InvalidOperationException("Justine library: unexpected empty lamp"); continue; }
                        if (lamp.EffectsAlpha > 0f ? value <= 0f : value > .001f) throw new InvalidOperationException("Justine library: lamp effects lost " + lamp.Name);
                    }
                    if (empty != 1) throw new InvalidOperationException("Justine library: expected one empty projector lamp");
                    int added = 0;
                    foreach (var l in AmnesiaLight.All) if (l != null && l.enhancedAdded) { added++; if (l.LightLevelAt(l.transform.position, 0f) != 0f) throw new InvalidOperationException("Justine library: sky affects legacy light"); }
                    if (added != (mode == 4 ? 0 : 2)) throw new InvalidOperationException("Justine library: aperture accumulation");
                }
            }
            void Advance(Props.PropLamp lamp, float dt) { lamp.UpdateLogic(dt); foreach (var l in lamp.Component.GetComponentsInChildren<AmnesiaLight>(true)) l.UpdateLight(dt); }
            void Fade(AmnesiaLight l, Color target, float radius, float seconds)
            {
                Color start = l.CurrentColor; float from = l.CurrentRadius; l.FadeTo(target, radius, seconds); l.UpdateLight(seconds * .5f);
                Color half = Color.Lerp(start, target, .5f); float halfRadius = Mathf.Lerp(from, radius, .5f);
                VerifyModes();
                if ((new Vector4(l.CurrentColor.r-half.r,l.CurrentColor.g-half.g,l.CurrentColor.b-half.b,l.CurrentColor.a-half.a)).sqrMagnitude > 1e-6f || Mathf.Abs(l.CurrentRadius-halfRadius) > .001f) throw new InvalidOperationException("Justine library: live fade midpoint lost " + l.name);
                l.UpdateLight(seconds * .5f); VerifyModes();
                if (l.CurrentColor != target || Mathf.Abs(l.CurrentRadius-radius) > .001f) throw new InvalidOperationException("Justine library: live fade endpoint lost " + l.name);
            }
            try
            {
                VerifyModes();
                foreach (bool on in new[] { false, true }) { foreach (var (lamp, _) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); } VerifyModes(); }
                foreach (bool on in new[] { false, true })
                {
                    foreach (var (lamp, _) in lamps) lamp.SetLit(on, true);
                    foreach (var (lamp, _) in lamps) Advance(lamp, .5f); VerifyModes();
                    foreach (var (lamp, _) in lamps) Advance(lamp, .5f); VerifyModes();
                }
                int links = 0;
                foreach (var (lamp, _) in lamps)
                {
                    string name = lamp.Component.GetVariable("ConnectedLight"); if (string.IsNullOrEmpty(name)) continue;
                    links++; var l = map.GetLight(name);
                    foreach (bool on in new[] { false, true })
                    {
                        lamp.SetLit(on, false); lamp.UpdateLogic(0f);
                        if (on ? l.CurrentColor == l.diffuseColor : l.CurrentColor != l.diffuseColor) throw new InvalidOperationException("Justine library: lamp connection lost " + lamp.Name);
                        VerifyModes();
                    }
                }
                if (links != 14) throw new InvalidOperationException("Justine library: expected 14 lamp links");
                foreach (string name in new[] { "torch_static01_6", "torch_static01_4", "torch_static01_8" })
                {
                    var lamp = (Props.PropLamp)map.GetEntityByName(name); lamp.SetLit(false, true); Advance(lamp, .5f); VerifyModes(); Advance(lamp, .5f); VerifyModes();
                }
                foreach (string name in new[] { "PointLight_2", "PointLight_9", "SpotLight_2", "SpotLight_no_stencil", "SpotLight_stencil01", "SpotLight_stencil02", "SpotLight_stencil03", "SpotLight_stencil04" })
                {
                    var l = map.GetLight(name); Color target = name is "PointLight_2" or "PointLight_9" or "SpotLight_2" ? new Color(.3f,.3f,1f,.4f) : new Color(.5f,.6f,.9f,.5f);
                    Fade(l, target, l.CurrentRadius, 1f); Fade(l, Color.clear, l.CurrentRadius, 1f);
                }
                Fade(map.GetLight("PointLight_44"), Color.clear, 0f, 4f);
                string line = "Justine library state PASS: 57 lamps (56 with owned light, empty light-box lamp; initially 30 on, 27 off), all-lamp shutdown/relight and effect fades, all 14 effective lamp connections and three scripted wind-extinguished torches survive RTGI, RT no-GI, SSGI, raster no-GI and classic. Projector three auxiliary and five stencil/no-stencil lights preserve cookies/frusta/RGBA/radii through live fade midpoints/endpoints; enhanced blue wash is scaled to .15 (point 9) / .3 (spot 2) without scaling the five projections; shelf point 44 four-second fade to zero radius survives switching. Original active/visibility states, nine boxes and legacy HPL2 light retained. Two aperture lights do not accumulate or enter legacy light; prisoner hatch is enclosed, not sky. Light-only checks do not execute stencil inventory, lever choices, prisoner harm, enemy chase, perma death or progression.";
                report.AppendLine(line); Debug.Log("[EnhancedLighting] " + line);
            }
            finally
            {
                foreach (var (lamp, on) in lamps) { lamp.SetLit(on, false); lamp.UpdateLogic(0f); }
                foreach (var (l, data, _) in saved) l.Restore(data);
                HplGraphics.Apply(user);
            }
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
