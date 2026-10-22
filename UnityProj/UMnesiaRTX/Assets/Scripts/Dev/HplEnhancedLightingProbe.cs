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
                    s.rtPathTracing = 0;
                    s.rtLanternShadows = !mode.Contains("noshadow");
                    if (mode.Contains("tex")) s.textures = 1;
                    HplGraphics.Apply(s);
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
                    const int frames = 40;
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
                                  $"RT={HplRayTracing.Active} traced={HplRayTracing.TracedLights}/{HplRayTracing.ShadowedLights}";
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
            yield return SetLantern(map, false);
            HplGraphics.Apply(user);
            File.WriteAllText(Path.Combine(folder, mapName + "_report.txt"), report.ToString());
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
