using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>Silent editor-only integration check and camera route audit; never builds a player.</summary>
    [InitializeOnLoad]
    public static class BenchmarkPreview
    {
        const string Pending = "RTXMnesia.BenchmarkPreview";
        static HplGameUi s_owner;
        static HplMap s_origin;
        static string s_preferences;
        static float s_health, s_sanity;
        static int s_stage, s_lastScene = -1, s_lastShot = -1, s_errors;
        static bool s_enemyChecked;
        static RenderTexture s_target;
        static double s_start;
        static readonly List<string> Report = new();
        static readonly HashSet<string> Hits = new();
        static readonly string Folder = Path.GetFullPath("Logs/BenchmarkCheck");
        static string ReportPath => Path.Combine(Folder, SessionState.GetBool(Pending + ".title", false) ? "title_report.txt" : "report.txt");

        static BenchmarkPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    EditorApplication.isPaused = false;
                    s_start = EditorApplication.timeSinceStartup;
                    s_stage = 0; s_errors = 0; s_lastScene = s_lastShot = -1;
                    s_enemyChecked = false;
                    Report.Clear(); Hits.Clear();
                    EditorApplication.update += Update;
                }
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    EditorApplication.update -= Update;
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(Pending + ".exit", 1));
                }
            };
        }

        public static void RunBatch() => Run(false);
        public static void RunTitleBatch() => Run(true);

        static void Run(bool title)
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Benchmark preview is batch-only");
            Directory.CreateDirectory(Folder);
            AudioListener.volume = 0;
            EditorSceneManager.OpenScene(title ? HplGameUi.MainMenuScene : HplBenchmarkRoute.All[1].ScenePath);
            var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
            map.runScript = false; map.disableAutoSaves = true; map.disableEnemyAI = true; map.disableInsanity = true;
            SessionState.SetBool(Pending, true);
            SessionState.SetBool(Pending + ".title", title);
            SessionState.SetInt(Pending + ".exit", 1);
            EditorApplication.EnterPlaymode();
        }

        static void Check(bool condition, string message)
        {
            Report.Add((condition ? "PASS " : "FAIL ") + message);
            if (!condition) s_errors++;
        }

        static void Update()
        {
            AudioListener.volume = 0;
            if (EditorApplication.timeSinceStartup - s_start > 900) { Check(false, "Timeout"); Finish(); return; }
            try
            {
                if (s_stage == 0)
                {
                    s_origin = HplMap.Current;
                    bool title = SessionState.GetBool(Pending + ".title", false);
                    if (s_origin == null) return;
                    s_owner = title ? UnityEngine.Object.FindAnyObjectByType<HplMainMenu>()?.Ui : s_origin.GameUi;
                    if (s_owner == null || !title && s_origin.player?.Body == null) return;
                    s_preferences = PlayerPrefs.GetString(HplUserSettings.PrefsKey, "");
                    s_health = s_origin.player != null ? s_origin.player.health : 0;
                    s_sanity = s_origin.player != null ? s_origin.player.sanity : 0;
                    s_owner.Settings.masterVolume = 0;
                    s_owner.Pause(); s_owner.OpenOptions(HplPauseMenu.GraphicsTabIndex);
                    s_owner.DraftSettings.masterVolume = 0;
                    // Exercise the real menu control, including an unapplied graphics draft.
                    s_owner.DraftSettings.lightingSet = 1; s_owner.DraftSettings.textures = 1;
                    s_owner.DraftSettings.rtPathTracing = 0;
                    Check(s_owner.Choose(MenuControl.Benchmark), "Graphics button starts the tour");
                    Check(HplBenchmark.Active, "Benchmark active");
                    s_stage = title ? 2 : 1;
                    return;
                }
                var test = UnityEngine.Object.FindAnyObjectByType<HplBenchmark>();
                if (test != null && test.Map != null)
                {
                    if (s_target == null) s_target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    test.Map.PlayerCamera.targetTexture = s_target;
                }
                if (s_stage == 1)
                {
                    if (test == null)
                    {
                        Check(HplMap.Current == s_origin && s_origin.gameObject.activeInHierarchy, "Original scene restored");
                        Check(s_owner.IsPaused && s_owner.ShowingOptions, "Returned to open options");
                        Check(s_owner.DraftSettings.lightingSet == 1, "Unapplied draft preserved");
                        Check(s_origin.player.health == s_health && s_origin.player.sanity == s_sanity, "Player state preserved");
                        Check(ReferenceEquals(typeof(HplEnhancedLighting).GetField("s_map", BindingFlags.NonPublic | BindingFlags.Static)
                            .GetValue(null), s_origin), "Enhanced lighting reattached to original map");
                        Check(PlayerPrefs.GetString(HplUserSettings.PrefsKey, "") == s_preferences, "Preferences never written");
                        Check(s_lastScene == 5, "All six scenes completed");
                        s_owner.Choose(MenuControl.Benchmark);
                        s_stage = 2;
                        return;
                    }
                    if (!test.IsPlaying || test.Map == null) return;
                    if (test.SceneIndex != s_lastScene)
                    {
                        s_lastScene = test.SceneIndex; s_lastShot = -1;
                        Check(test.Map.GameUi == null && !test.Map.runScript && test.Map.disableAutoSaves, "World isolated " + s_lastScene);
                        if (s_lastScene == 2 || s_lastScene == 3)
                            foreach (var entity in test.Map.Entities)
                                if (entity is Props.PropLamp lamp && (lamp.Component.file ?? "").Replace('\\', '/').StartsWith("entities/lamp/", StringComparison.OrdinalIgnoreCase))
                                    Check(lamp.Lit, "Practical light lit " + lamp.Name);
                        if (s_lastScene == 1)
                        {
                            Check((test.Map.GetEntityByName("candlestick01_1") as Props.PropLamp)?.Lit == true, "Stove room candle lit");
                            var stove = test.Map.GetEntityByName("stove_metal_1");
                            Check(stove != null && stove.Active && stove.GameObject.activeInHierarchy,
                                "Stove remains visible: its SwingDoor type must not hide the entire stove");
                            var cabinet = test.Map.GetEntityByName("cabinet_simple_1");
                            Check(cabinet != null && cabinet.Active, "Furniture with hinged doors remains present");
                        }
                        Audit(test);
                    }
                    int shot = Mathf.FloorToInt(test.SceneTime / 10);
                    if (shot != s_lastShot)
                    {
                        s_lastShot = shot;
                        Capture(test, shot);
                    }
                    if (test.SceneIndex == 5 && test.SceneTime > 17 && !s_enemyChecked)
                    {
                        s_enemyChecked = true;
                        var brute = test.Map.GetEntityByName("servant_brute_1") as Enemies.HplEnemy;
                        Check(brute != null && brute.Active && brute.CurrentAnimationName == "Walk", "Brute walks past the observer");
                        Check(test.Map.player.Terror > 0, "Terror audio envelope active");
                        Check(test.Map.effects.RadialBlur.Size > 0, "Enemy presence screen effect active");
                        Check(test.Map.sound.SoundEntities.Count > 0 && test.Map.sound.Entries.Count > 0, "World and enemy sound logic active while output muted");
                    }
                    if (Array.IndexOf(Environment.GetCommandLineArgs(), "-benchmarkQuick") >= 0 && test.SceneIndex != 5)
                    {
                        float duration = HplBenchmarkRoute.All[test.SceneIndex].Duration;
                        if (test.SceneTime > 1 && test.SceneTime < duration / 2 - 1)
                            Seek(test, duration / 2);
                        else if (test.SceneTime > duration / 2 + 1 && test.SceneTime < duration - 2)
                            Seek(test, duration - 1);
                    }
                }
                else if (s_stage == 2)
                {
                    if (test == null) { Check(false, "Cancellation test did not start"); Finish(); return; }
                    if (!test.IsPlaying) return;
                    // Same cancellation accumulator as keyboard Update; short press must not cancel.
                    CancelInput(test, true, 0.25f);
                    Check(!test.CancelRequested, "Brief Esc does not cancel");
                    CancelInput(test, false, 0.1f);
                    CancelInput(test, true, HplBenchmark.CancelHoldSeconds);
                    Check(test.CancelRequested, "Held Esc cancels");
                    s_stage = 3;
                }
                else if (s_stage == 3 && test == null)
                {
                    Check(HplMap.Current == s_origin && s_owner.ShowingOptions, "Cancellation restores options and map");
                    Check(PlayerPrefs.GetString(HplUserSettings.PrefsKey, "") == s_preferences, "Cancellation preserves preferences");
                    Finish();
                }
            }
            catch (Exception e) { Check(false, e.ToString()); Finish(); }
        }

        static void CancelInput(HplBenchmark test, bool held, float dt) => typeof(HplBenchmark)
            .GetMethod("UpdateCancellation", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(test, new object[] { held, true, dt });

        static void Seek(HplBenchmark test, float time)
        {
            typeof(HplBenchmark).GetProperty("SceneTime").SetValue(test, time);
            typeof(HplBenchmark).GetField("_sceneStartedAt", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(test, Time.realtimeSinceStartupAsDouble - time);
        }

        static void Audit(HplBenchmark test)
        {
            var route = HplBenchmarkRoute.All[test.SceneIndex];
            Physics.SyncTransforms();
            int collisions = 0;
            Vector3 previous = default;
            for (float t = 0; t <= route.Duration; t += 0.1f)
            {
                route.Evaluate(t, out var eye, out _);
                Vector3 point = HplSpace.Position(eye);
                var colliders = new List<Collider>(Physics.OverlapSphere(point, 0.12f, ~0, QueryTriggerInteraction.Ignore));
                if (t > 0 && point != previous)
                    foreach (var hit in Physics.SphereCastAll(previous, 0.12f, (point - previous).normalized, (point - previous).magnitude, ~0, QueryTriggerInteraction.Ignore))
                        colliders.Add(hit.collider);
                foreach (var collider in colliders)
                {
                    if (collider.gameObject.scene != test.Map.gameObject.scene || collider.GetComponentInParent<Player.HplCharacterBody>() != null) continue;
                    string key = test.SceneIndex + ":" + collider.name;
                    if (Hits.Add(key)) Report.Add($"ROUTE HIT scene={test.SceneIndex} t={t:F1} hpl={eye} collider={collider.name} parent={collider.transform.parent?.name}");
                    collisions++;
                }
                previous = point;
            }
            Check(collisions == 0, "Camera clearance " + route.map + " contacts=" + collisions);
            if (test.SceneIndex == 5) AuditEnemy(test);
            File.WriteAllLines(ReportPath, Report);
        }

        static void AuditEnemy(HplBenchmark test)
        {
            var enemy = test.Map.GetEntityByName("servant_brute_1") as Enemies.HplEnemy;
            Vector3 size = enemy.Body.Size;
            int contacts = 0;
            var names = new HashSet<string>();
            for (float t = 8; t <= 25; t += 0.05f)
            {
                Vector3 feet = HplSpace.Position(HplBenchmark.EnemyFeet(t));
                // Leave clearance under the soles for the floor, test the entire torso/head width.
                var half = size * 0.5f; half.y -= 0.08f;
                Vector3 center = feet + Vector3.up * (size.y * 0.5f + 0.08f);
                foreach (var collider in Physics.OverlapBox(center, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (collider.gameObject.scene != test.Map.gameObject.scene || collider.GetComponentInParent<Player.HplCharacterBody>() != null) continue;
                    // Respect the HPL character collision mask: decorative floor ivy is deliberately
                    // non-colliding and must not be treated as an obstacle for a walking enemy.
                    var body = collider.GetComponentInParent<AmnesiaBody>();
                    if (body != null && !body.collideCharacter) continue;
                    contacts++;
                    if (names.Add(collider.name + collider.transform.parent?.name))
                        Report.Add($"ENEMY HIT t={t:F2} feet={feet} collider={collider.name} parent={collider.transform.parent?.name}");
                }
            }
            Check(contacts == 0, "Brute full body clearance contacts=" + contacts);
        }

        static void Capture(HplBenchmark test, int shot)
        {
            var camera = test.Map.PlayerCamera;
            var active = RenderTexture.active;
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            try
            {
                typeof(HplBenchmark).GetMethod("ApplyCamera", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(test, null);
                camera.Render();
                RenderTexture.active = s_target; image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(Folder, $"scene_{test.SceneIndex}_{shot}.png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        static void Finish()
        {
            EditorApplication.update -= Update;
            Report.Add(s_errors == 0 ? "PASS" : "FAIL " + s_errors);
            File.WriteAllLines(ReportPath, Report);
            if (s_target != null) UnityEngine.Object.DestroyImmediate(s_target);
            SessionState.SetInt(Pending + ".exit", s_errors == 0 ? 0 : 1);
            EditorApplication.ExitPlaymode();
        }
    }
}
