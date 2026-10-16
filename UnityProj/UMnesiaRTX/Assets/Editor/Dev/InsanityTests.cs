using System;
using System.IO;
using System.Linq;
using RTXMnesia.Effects;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Insanity events (cLuxInsanityHandler, cLuxArea_Insanity) on the imported 04_wine_cellar_lab, edit mode with the
    /// map stepping like PlayerSimulation and no level script: the imported events, sets and sanity limits, the four
    /// event types (bugs, particles, sound stream with fade and subtitle, steps), the wait after an event, an insanity
    /// area starting an event and the script functions. Batch: -batchmode -nographics -quit -executeMethod
    /// RTXMnesia.EditorDev.InsanityTests.RunBatch. Result in Logs/AmnesiaImport.log.
    /// </summary>
    public static class InsanityTests
    {
        const string MapFile = "maps/main/ch01/04_wine_cellar_lab.map";

        [MenuItem("Tools/RTXMnesia/Tests/Insanity events (04_wine_cellar_lab)")]
        public static void RunBatch()
        {
            var log = new ImportLog();
            int checks = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks++;
                log.Info("ok: " + message);
            }
            var simulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            bool failed = false;
            try
            {
                var (map, player) = Open();
                var assets = HplInsanityAssets.Instance;
                Check(assets != null && assets.events.Count == 54 && assets.GetImage("insanity_bug01.tga") != null,
                      $"misc/main_sanity_events.cfg imported ({assets?.events.Count} events, bug image)");
                HplInsanity.ResetSession();
                HplInsanity.Update(map, 0f);
                Check(HplInsanity.Events.Count == 54, "54 events loaded");
                var swarm = HplInsanity.Events.FirstOrDefault(e => e.Name == "Bugs_Swarm");
                Check(swarm != null && swarm.Set == "Default" && Mathf.Approximately(swarm.MaxSanity, 50f), "Bugs_Swarm: set Default, MaxSanity 50");

                // Sets: what 00_rainy_hall does.
                CallScript(map, "SetInsanitySetEnabled", "Ch02", false);
                CallScript(map, "SetInsanitySetEnabled", "Ch03", false);
                Check(HplInsanity.DisabledSets.Count() == 2, "SetInsanitySetEnabled disables Ch02 and Ch03");

                player.sanity = 100f;
                Check(!HplInsanity.NewEventIsPossible(map), "no event above MaxSanity_LongWait (65)");
                player.sanity = 40f;
                Check(HplInsanity.NewEventIsPossible(map), "an event is possible at sanity 40");

                // Random events: never of a disabled set, never above the sanity.
                for (int i = 0; i < 40; i++)
                {
                    HplInsanity.StartEvent(map);
                    var e = HplInsanity.CurrentEvent;
                    if (e == null) continue;
                    Check(e.Set != "Ch02" && e.Set != "Ch03" && e.MaxSanity >= 40f, $"random event {i}: {e.Name} ({e.Set}, max {e.MaxSanity})");
                }
                HplInsanity.StopCurrentEvent();
                Check(!(bool)CallScript(map, "InsanityEventIsActive"), "StopCurrentInsanityEvent / InsanityEventIsActive");

                // Bugs
                CallScript(map, "StartInsanityEvent", "Bugs_Swarm");
                var bugs = HplInsanity.CurrentEvent as InsanityEventBugs;
                Check(bugs != null && bugs.Bugs.Count == 7 && (bool)CallScript(map, "InsanityEventIsActive"), "StartInsanityEvent Bugs_Swarm: 7 bugs");
                Check(bugs.Bugs.All(b => b.sizeMul >= 0.8f && b.sizeMul <= 1f), "bug sizes 0.8-1 (the file's mfMaxSizeMul typo)");
                Check(AnyEntry(map, "insanity_bug3"),
                      "the swarm's loop sound plays");
                float d0 = bugs.Bugs.Average(b => Vector2.Distance(b.pos, new Vector2(400f, 300f)));
                Step(map, 3f);
                float d1 = bugs.Bugs.Average(b => Vector2.Distance(b.pos, new Vector2(400f, 300f)));
                Check(bugs.Bugs.All(b => b.alpha > 0f) && d1 < d0, $"bugs fade in and crawl toward the center ({d0:0} → {d1:0} px)");
                float t = 0f;
                while (t < 30f && HplInsanity.CurrentEvent != null) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(HplInsanity.CurrentEvent == null && t > 14f, $"the swarm ends after the duration and fade out ({t + 3f:0.0} s)");
                Check(Mathf.Abs(HplInsanity.NewEventCount - 180f) < 1f, $"then the wait for sanity 40: TimeBetween_MedWait ({HplInsanity.NewEventCount:0})");
                Check(!HplInsanity.NewEventIsPossible(map), "no new event while waiting");

                // Sound stream with a red fade
                HplInsanity.StartEvent(map, "Stream_Monster_Roar01");
                var roar = HplInsanity.CurrentEvent as InsanityEventSoundStream;
                Check(roar != null && roar.FadeScreen && roar.FadeColor == new Color(1f, 0f, 0f, 0.8f), "Stream_Monster_Roar01: red fade");
                Step(map, 2.2f);
                Check(roar.FadeAlpha > 0.99f && AnyEntry(map, "insanity_monster_roar01"), $"fade in over 2 s and the roar plays ({roar.FadeAlpha:0.00})");
                t = 0f;
                while (t < 20f && HplInsanity.CurrentEvent != null) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(HplInsanity.CurrentEvent == null && roar.FadeAlpha <= 0f, $"after the roar the fade goes out and the event ends ({t:0.0} s more)");

                // Daniel's ramble: subtitle, voice file found through the "daniel/" folder of the file name
                HplInsanity.StartEvent(map, "Stream_Voice_Daniel_Ch01_01");
                var ramble = HplInsanity.CurrentEvent as InsanityEventSoundStream;
                Step(map, 0.1f);
                Check(ramble != null && ramble.Subtitle.Length > 10 && ramble.Subtitle != "CH01_DanielRamble_01" && AnyEntry(map, "CH01_DanielRamble_01"),
                      $"Daniel's ramble: voice and subtitle \"{ramble?.Subtitle}\"");
                HplInsanity.StopCurrentEvent();

                // Particles
                int systems = map.ParticleWorld.Systems.Count;
                HplInsanity.StartEvent(map, "Particles_Flies_Short");
                Step(map, 0.5f);
                var flies = map.ParticleWorld.Systems.LastOrDefault();
                Check(map.ParticleWorld.Systems.Count == systems + 1 && flies != null &&
                      Vector3.Distance(flies.LocalMatrix.GetPosition(), map.PlayerCamera.transform.position) < 0.01f && AnyEntry(map, "general_flies"),
                      "Particles_Flies_Short: flies at the camera and their sound");
                t = 0f;
                while (t < 15f && HplInsanity.CurrentEvent != null) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(HplInsanity.CurrentEvent == null && t > 11f && t < 12.5f, $"the flies end after 12 s ({t + 0.5f:0.0} s)");

                // Steps
                int sounds = map.sound.SoundEntities.Count(s => s.Name == "insanity_step");
                HplInsanity.StartEvent(map, "Steps_Muffled");
                int stepSounds = 0;
                t = 0f;
                while (t < 10f && HplInsanity.CurrentEvent != null)
                {
                    int before = map.sound.SoundEntities.Count(s => s.Name == "insanity_step");
                    Step(map, 1f / 60f);
                    t += 1f / 60f;
                    if (map.sound.SoundEntities.Count(s => s.Name == "insanity_step") > before) stepSounds++;
                }
                Check(stepSounds == 7 && HplInsanity.CurrentEvent == null, $"Steps_Muffled: 7 steps around the player ({stepSounds}, {t:0.0} s)");

                // Insanity area: the player inside, sanity low, no wait → an event within the check time.
                HplInsanity.Reset();
                var area = map.GetEntityByName("InsanityArea_1") as HplArea;
                Check(area != null && area.AreaType == "Insanity" && area.Active, "InsanityArea_1 is an insanity area");
                player.PlaceAt(area.Component.transform.position, 0f);
                Step(map, 0.5f);
                t = 0f;
                while (t < 3f && HplInsanity.CurrentEvent == null) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(HplInsanity.CurrentEvent != null && area.Active,
                      $"standing in it starts {HplInsanity.CurrentEvent?.Name} within {t:0.0} s (check every 0-1 s: the IsanityArea_CheckTimeMax typo); AutoDisable false keeps it on");
                HplInsanity.Reset();
                Check(HplInsanity.CurrentEvent == null && HplInsanity.DisabledSets.Count() == 2, "Reset ends the event and keeps the disabled sets");

                log.Info($"PASS: {checks} insanity checks (import, sets, sanity limits, random pick, bugs, wait, sound stream fade, " +
                         "subtitle, particles, steps, insanity area, script functions)");
                log.Save("Insanity tests");
                Debug.Log($"PASS: {checks} insanity checks");
            }
            catch (Exception ex)
            {
                log.Error($"FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
                log.Save("Insanity tests");
                Debug.LogError($"FAIL: {ex.Message}");
                failed = true;
            }
            finally
            {
                Physics.simulationMode = simulationMode;
                HplInsanity.ResetSession();
                HplGame.Reset();
            }
            if (failed && Application.isBatchMode) EditorApplication.Exit(1);
        }

        static bool AnyEntry(HplMap map, string clipName) =>
            map.sound.Entries.Any(e => e.Alive && e.Clip != null && e.Clip.name.IndexOf(clipName, StringComparison.OrdinalIgnoreCase) >= 0);

        static (HplMap map, AmnesiaPlayer player) Open()
        {
            string scenePath = LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, MapFile));
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            HplMath.Seed(1);
            HplGame.Reset();
            var player = UnityEngine.Object.FindAnyObjectByType<AmnesiaPlayer>();
            Physics.SyncTransforms();
            player.readDevices = false;
            Invoke(player, "Awake");
            Invoke(player, "Start");
            var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
            map.runScript = false;
            map.Build();
            map.Begin();
            return (map, player);
        }

        static void Step(HplMap map, float seconds)
        {
            int steps = Mathf.Max(1, Mathf.RoundToInt(seconds / Time.fixedDeltaTime));
            for (int i = 0; i < steps; i++) map.Step(Time.fixedDeltaTime);
        }

        static object CallScript(HplMap map, string function, params object[] args)
        {
            var method = map.Script.GetType().GetMethod(function, System.Reflection.BindingFlags.Instance |
                                                                   System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return method.Invoke(map.Script, args);
        }

        static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic)?.Invoke(target, null);
    }
}
