using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>cLuxPlayerFlashback lifecycle checks with the imported hub, real voices and script callbacks; no scene saves.</summary>
    public static class FlashbackTests
    {
        sealed class CallbackScript : HplScript
        {
            public readonly List<string> callbacks = new();
            public bool cleanState = true;
            public bool FlashbackActive => GetFlashbackIsActive();
            public void FirstOver() => Over("first");
            public void SecondOver() => Over("second");
            void Over(string name)
            {
                cleanState &= !GetFlashbackIsActive() && Player.eventMoveSpeedMul == 1f && Player.eventRunSpeedMul == 1f;
                callbacks.Add(name);
            }
        }

        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot,
                "maps/main/ch01/02_entrance_hall.map")), OpenSceneMode.Single);
            var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
            var player = UnityEngine.Object.FindAnyObjectByType<AmnesiaPlayer>();
            player.readDevices = false;
            Call(player, "Awake");
            Call(player, "Start");
            map.Build();
            var script = new CallbackScript();
            typeof(HplScript).GetProperty(nameof(HplScript.Map)).SetValue(script, map);
            typeof(HplMap).GetProperty(nameof(HplMap.Script)).SetValue(map, script);
            map.runScript = true;
            map.effects.ResetAll();

            var lines = new List<string>();
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                lines.Add("PASS: " + message);
            }
            const float dt = 1f / 60f;
            var helper = player.Flashback;
            var effects = map.effects;
            void Step()
            {
                helper.Update(dt);
                effects.Step(dt);
                map.sound.Step(dt);
            }
            void Steps(int count) { for (int i = 0; i < count; i++) Step(); }
            int Finish()
            {
                int steps = 0;
                while (helper.Active && steps < 120 * 60) { Step(); steps++; }
                if (helper.Active) throw new InvalidOperationException("Flashback did not finish within 120 s");
                return steps;
            }

            var assets = Resources.Load<HplFlashbackAssets>(HplFlashbackAssets.ResourceName);
            const string file = "CH01L02_JourneyToSanctum01";
            var data = assets.Find(file);
            Check(assets.flashbacks.Count == 37 && data != null && data.voices.Count == 7, "Imported 37 flashbacks and all seven hub voices");
            Check(ReferenceEquals(data, assets.Find("flashbacks/ch01l02_journeytosanctum01.FLASH")), "Lookup ignores path, extension and case");
            Check(assets.Find("__missing_flashback__") == null, "Missing flashback lookup fails explicitly");
            helper.Disabled = true;
            helper.Start(file, "");
            Check(!helper.Active && map.sound.GlobalVolume.GetEntry(1, false) == null, "Debug disable flag ignores starts without side effects");
            helper.Disabled = false;

            var area = (HplArea)map.GetEntityByName("AreaFlashBack");
            player.PlaceAt(new Vector3(-33.25f, 0.05f, 9.75f), 0f);
            // No PhysX sync: this catches a stale Collider.bounds pre-test of the moved character.
            area.UpdateLogic(dt * 0.5f);
            Check(area.Active && !helper.Active, "Partial initialization step does not trigger the area");
            area.UpdateLogic(dt);
            Check(!area.Active && helper.Active && script.FlashbackActive, "Area uses the current character shape, deactivates and starts the flashback");
            Check(player.eventMoveSpeedMul == 1f && effects.Flash.Alpha == 0f, "Flash and movement change wait for the initial delay");
            Steps(40);
            Check(effects.Flash.Alpha > 0f && Mathf.Approximately(player.eventMoveSpeedMul,
                map.gameConfig.GetFloat("Player_General", "FlashbackMoveSpeedMul")), "Initial flash applies the configured movement multiplier");
            Steps(80);
            Check(effects.Sepia.Amount > 0f && effects.RadialBlur.Size > 0f && effects.PlayVoice.Active, "Delayed sepia, blur and voices start");
            Check(Mathf.Approximately(map.sound.GetGlobalVolume(SoundEntryType.World),
                map.gameConfig.GetFloat("Player_General", "FlashbackWorldSoundVolume")), "World volume reaches the configured destination");
            float sanity = player.sanity;
            player.InDarkness.Update(dt);
            Check(player.sanity == sanity, "Darkness does not drain sanity during the flashback");
            Finish();
            Steps(250);
            Check(!script.FlashbackActive && !area.Active && effects.Sepia.Amount == 0f && effects.RadialBlur.Size == 0f,
                "Flashback ends, the area stays off and post effects fade out");
            Check(map.sound.GlobalVolume.GetEntry(1, false) == null && map.sound.GetGlobalVolume(SoundEntryType.World) == 1f,
                "World volume returns to one and removes only the flashback request");

            helper.Start(file, nameof(CallbackScript.FirstOver));
            helper.Start(file, nameof(CallbackScript.SecondOver));
            Finish();
            Check(script.callbacks.Count == 1 && script.callbacks[0] == "first" && script.cleanState,
                "First callback runs once after active state and movement are restored");
            Steps(359);
            Check(!helper.Active && script.callbacks.Count == 1, "Queued flashback stays inactive for the six-second gap");
            Steps(2);
            Check(helper.Active, "Queued flashback starts after six seconds");
            Finish();
            Check(script.callbacks.Count == 2 && script.callbacks[1] == "second" && script.cleanState,
                "Queued callback runs once in FIFO order with restored state");
            Steps(250);

            var voice = effects.PlayVoice;
            var entry = map.sound.PlayGuiStream(data.voices[0].voiceSound, true, 1f);
            Set(voice, "_effectEntry", entry);
            Set(voice, "_effectEntryId", entry.Id);
            Check(voice.VoiceDonePlaying() && map.sound.IsValid(entry, entry.Id), "Effect sound alone does not hold VoiceDonePlaying open");
            Set(voice, "_voiceEntry", entry);
            Set(voice, "_voiceEntryId", entry.Id);
            entry.Stop();
            map.sound.Step(dt);
            Check(!map.sound.IsValid(entry, entry.Id) && !voice.VoiceDonePlaying(), "Expired voice pointer still blocks completion until voice update clears it");
            voice.Reset();

            player.scriptMoveSpeedMul = 0.8f;
            player.scriptRunSpeedMul = 0.7f;
            player.eventMoveSpeedMul = 0.6f;
            player.eventRunSpeedMul = 0.5f;
            Check(Mathf.Approximately(player.MoveSpeedMul, 0.48f) && Mathf.Approximately(player.RunSpeedMul, 0.35f),
                "Event multipliers compose with script multipliers");
            player.scriptMoveSpeedMul = player.scriptRunSpeedMul = player.eventMoveSpeedMul = player.eventRunSpeedMul = 1f;
            helper.Start(file, "");
            helper.Update(0.5f);
            helper.Update(dt);
            Check(player.eventMoveSpeedMul == 1f, "Original strict flash-delay boundary is preserved at exactly zero");
            helper.Reset();
            effects.ResetAll();

            string report = $"PASS: {lines.Count} flashback lifecycle checks (area, real voices, queue, callbacks, volume and original quirks)";
            lines.Add(report);
            File.WriteAllLines(Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "FlashbackTests.txt"), lines);
            Debug.Log(report);
        }

        static void Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
