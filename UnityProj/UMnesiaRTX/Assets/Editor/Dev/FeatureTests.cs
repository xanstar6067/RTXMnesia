using System;
using System.IO;
using System.Linq;
using RTXMnesia.Effects;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.Save;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// The 6.5 (c) additions on imported maps, edit mode with the map stepping and no level script:
    /// 11_study — sign area, oil barrel, emotion stone, developer commentary icon, SetPropActiveAndFade (dissolve
    /// copy and fade in); 02_entrance_hall — completion amounts, counter, level door percentage; 20_sewer — the
    /// water wheel cog panels (multi sliders) with the level callback; key bindings and the key config draft;
    /// water materials and the world reflection surfaces of 07_archives_cellar; pre menu resources.
    /// Batch: -batchmode -nographics -quit -executeMethod RTXMnesia.EditorDev.FeatureTests.RunBatch.
    /// Result in Logs/AmnesiaImport.log.
    /// </summary>
    public static class FeatureTests
    {
        static readonly string[] PrefKeys = { "RTXMnesia.KeyBindings.v1", "RTXMnesia.UserSettings.v1" };

        [MenuItem("Tools/RTXMnesia/Tests/Signs, barrels, stones, sliders, fade, completion, keys, water")]
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
            var savedPrefs = PrefKeys.Select(k => PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null).ToArray();
            HplSaveHandler.FolderOverride = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "FeatureCheck"));
            bool failed = false;
            try
            {
                HplGame.Reset();
                HplInsanity.ResetSession();
                Study(Check);
                Completion(Check);
                Sliders(Check);
                Keys(Check);
                Water(Check);
                PreMenu(Check);
                LeanAndOil(Check);
                log.Info($"PASS: {checks} feature checks");
                log.Save("Feature tests (signs, barrels, stones, sliders, fade, completion, keys, water)");
                Debug.Log($"PASS: {checks} feature checks");
            }
            catch (Exception ex)
            {
                log.Error($"FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
                log.Save("Feature tests (signs, barrels, stones, sliders, fade, completion, keys, water)");
                Debug.LogError($"FAIL: {ex.Message}");
                failed = true;
            }
            finally
            {
                Physics.simulationMode = simulationMode;
                for (int i = 0; i < PrefKeys.Length; i++)
                {
                    if (savedPrefs[i] != null) PlayerPrefs.SetString(PrefKeys[i], savedPrefs[i]);
                    else PlayerPrefs.DeleteKey(PrefKeys[i]);
                }
                PlayerPrefs.Save();
                HplUserSettings.ShowCommentary = false;
                HplSaveHandler.FolderOverride = null;
                HplSavedMaps.Reset();
                HplInsanity.ResetSession();
                HplGame.Reset();
            }
            if (failed && Application.isBatchMode) EditorApplication.Exit(1);
        }

        // ================================================================== 11_study

        static void Study(Action<bool, string> check)
        {
            var (map, player) = Open("maps/main/ch01/11_study.map");
            var hud = map.hud;

            // Sign areas: the focus text, no interaction of their own.
            var sign = map.Entities.OfType<HplArea>().First(a => a.AreaType == "Sign");
            string signText = sign.GetFocusText();
            check(sign.CanInteract(null) && !sign.OnInteract(null, Vector3.zero) && Mathf.Approximately(sign.MaxFocusDistance, 3.6f) &&
                  signText != "" && sign.GetFocusCrosshair(null, Vector3.zero) == FocusCrosshair.Default,
                  $"sign {sign.Name}: focus text \"{signText}\" within Sign_MaxFocusDist 3.6, default crosshair");

            // Oil barrel: 10 + what is missing of 100 (lantern + potions, at most 90), calculated once.
            var barrel = (PropOilBarrel)map.GetEntityByName("oil_barrel_1");
            HplGame.Items.Clear();
            player.lampOil = 40f;
            string focus = barrel.GetFocusText();
            check(barrel.OilAmount == 70f && focus == hud.Translate("Game", "OilBarrel_FocusText_075"),
                  $"barrel: 10 + 60 missing = 70 oil, focus \"{focus}\"");
            barrel.OnInteract(null, Vector3.zero);
            check(player.lampOil == 100f && barrel.OilAmount == 10f && hud.Messages.Text == hud.Translate("Game", "OilBarrel_Refill_Full"),
                  "refill: lantern full, 10 left in the barrel, \"full\" message");
            player.lampOil = 99.5f;
            barrel.OnInteract(null, Vector3.zero);
            check(barrel.OilAmount == 10f && hud.Messages.Text == hud.Translate("Game", "OilBarrel_LanternFull"), "a lantern over 99: \"lantern full\", nothing taken");
            player.lampOil = 50f;
            barrel.OnInteract(null, Vector3.zero);
            check(player.lampOil == 60f && barrel.OilAmount == 0f && hud.Messages.Text == hud.Translate("Game", "OilBarrel_Refill_Amount_025"),
                  "the rest of 10: lantern 60, barrel empty, \"less than 25\" message");
            bool emptyResult = barrel.OnInteract(null, Vector3.zero);
            check(!emptyResult && hud.Messages.Text == hud.Translate("Game", "OilBarrel_Empty") &&
                  barrel.GetFocusText() == hud.Translate("Game", "OilBarrel_FocusText_Empty"), "empty barrel: message and focus text");
            check(barrel.GetFocusCrosshair(null, Vector3.zero) == FocusCrosshair.Grab, "barrel crosshair Grab");

            // Developer commentary.
            var icon = map.Entities.OfType<PropCommentaryIcon>().First();
            check(!icon.Active && icon.Type == HplEntityType.CommentaryIcon && icon.Talker != "" && icon.SoundFile.EndsWith(".ogg"),
                  $"commentary icon {icon.Name} off by default ({icon.Talker}: {icon.Topic}, {icon.SoundFile})");
            HplUserSettings.ShowCommentary = true;
            Step(map, 1f / 60f);
            check(icon.Active && icon.GetFocusText() == icon.Talker + "\n\"" + icon.Topic + "\"", "the commentary option turns the icons on; focus text talker / topic");
            var cog = icon.GameObject.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "cog");
            Quaternion cog0 = cog != null ? cog.localRotation : Quaternion.identity;
            icon.OnInteract(null, Vector3.zero);
            Step(map, 2f); // world volume 1 → 0.15 at 0.5 / s
            var commentary = map.effects.PlayCommentary;
            check(commentary.Active && icon.PlayingSound && commentary.IconId == icon.Id &&
                  map.ParticleWorld.Systems.Any(s => s.Name == "_commentary_icon_ps"), "interaction plays the commentary, with the icon's particle system");
            check(map.sound.GetGlobalVolume(RTXMnesia.Sound.SoundEntryType.World) < 0.2f, $"world sounds go to 0.15 at 0.5 / s ({map.sound.GetGlobalVolume(RTXMnesia.Sound.SoundEntryType.World):0.00})");
            check(cog != null && Quaternion.Angle(cog.localRotation, cog0) > 1f, "the cog turns while it plays");
            icon.OnInteract(null, Vector3.zero);
            Step(map, 2.5f);
            check(!commentary.Active && !icon.PlayingSound && map.sound.GetGlobalVolume(RTXMnesia.Sound.SoundEntryType.World) > 0.95f &&
                  Quaternion.Angle(cog.localRotation, cog0) < 0.5f, "interacting again stops it: volumes back, the cog back to rest");
            HplUserSettings.ShowCommentary = false;
            Step(map, 1f / 60f);
            check(!icon.Active, "the option off turns the icons off");

            // SetPropActiveAndFade: off leaves a dissolving copy, on fades the model in.
            CallScript(map, "SetPropActiveAndFade", "oil_barrel_1", false, 1f);
            check(!barrel.Active && map.DissolveEntities.Count == 1, "off: the prop goes, a dissolve copy of its model stays");
            Step(map, 0.5f);
            var dissolveRenderers = map.transform.Find("HplDissolve (runtime)").GetComponentsInChildren<MeshRenderer>();
            check(dissolveRenderers.Length > 0 && dissolveRenderers.All(r => r.sharedMaterials.All(m => m.GetFloat("_SurfaceType") == 1f)) &&
                  Mathf.Abs(map.DissolveEntities[0].Alpha - 0.5f) < 0.05f, $"halfway: transparent copy at coverage {map.DissolveEntities[0].Alpha:0.00}");
            Step(map, 0.6f);
            check(map.DissolveEntities.Count == 0, "after the fade time the copy is gone");
            var renderer = barrel.GameObject.GetComponentsInChildren<MeshRenderer>(true).First(r => r.sharedMaterial != null && r.sharedMaterial.shader.name == "HDRP/Lit");
            var original = renderer.sharedMaterial;
            CallScript(map, "SetPropActiveAndFade", "oil_barrel_1", true, 2f);
            Step(map, 1f);
            check(barrel.Active && Mathf.Abs(barrel.MeshFadeAlpha - 0.5f) < 0.05f && renderer.sharedMaterial != original && renderer.sharedMaterial.name.EndsWith("(fade)"),
                  $"on: the model fades in (coverage {barrel.MeshFadeAlpha:0.00}, transparent material)");
            Step(map, 1.1f);
            check(barrel.MeshFadeAlpha >= 1f && renderer.sharedMaterial == original, "faded in: the opaque material is back");

            // Emotion stone (last: it pauses the player).
            var stone = (PropEmotionStone)map.GetEntityByName("emotion_stone_1");
            check(stone.CanInteract(null) && stone.GetFocusCrosshair(null, Vector3.zero) == FocusCrosshair.Grab, "emotion stone: interactable, Grab crosshair");
            stone.OnInteract(null, Vector3.zero);
            var flash = map.effects.EmotionFlash;
            check(flash.Active && flash.Text == hud.Translate("EmotionStone", "Alexander02"), $"the emotion flash with the stone's text \"{flash.Text}\"");
        }

        // ================================================================== 02_entrance_hall: completion

        static void Completion(Action<bool, string> check)
        {
            var (map, _) = Open("maps/main/ch01/02_entrance_hall.map");
            var count = map.hud.Completion;
            check(count != null && count.QuestValue == 30 && count.ItemValue == 5 && count.NoteValue == 10 && count.FlashbackValue == 10,
                  "game.cfg [Quest] completion values");
            map.NumberOfQuests = 2;
            typeof(HplMap).GetMethod("CalculateTotalCompletionAmount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(map, null);
            int items = map.Entities.OfType<PropItem>().Sum(i => i.ItemType == "Puzzle" ? 5 : i.ItemType is "Note" or "Diary" ? 10 : 0);
            int flashbacks = map.Entities.OfType<HplArea>().Count(a => a.AreaType == "Flashback") * 10;
            check(map.TotalCompletionAmount == 60 + items + flashbacks && map.TotalCompletionAmount > 60,
                  $"total: 2 quests × 30 + items / notes {items} + flashbacks {flashbacks} = {map.TotalCompletionAmount}");
            CallScript(map, "CompleteQuest", "TestQuest", "TestQuest");
            check(map.CurrentCompletionAmount == 30 && count.Active, "a completed quest adds 30, the counter starts (6 s delay)");
            Step(map, 5f);
            check(count.Alpha == 0f, "nothing shown during the delay");
            Step(map, 3f);
            float expected = 30f / map.TotalCompletionAmount * 100f;
            check(count.Alpha > 0.9f && count.Percent > 0f && count.Percent <= expected + 0.01f, $"then it fades in and counts up ({count.Percent:0.0}% of {expected:0.0}%)");
            Step(map, 1f + expected / 3f + 2.5f);
            check(!count.Active, "it hides 2 s after reaching the value");

            HplSavedMaps.SaveMap(map);
            var door = map.Entities.OfType<PropLevelDoor>().First();
            typeof(PropLevelDoor).GetField("_mapFile", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(door, "maps/main/ch01/02_entrance_hall.map");
            string text = door.GetFocusText();
            string percent = (int)expected + "% " + map.hud.Translate("CompletionCount", "Completed");
            check(text.EndsWith(percent), $"level door focus text with the saved completion: \"{text.Replace('\n', '|')}\"");
        }

        // ================================================================== 20_sewer: multi sliders

        static void Sliders(Action<bool, string> check)
        {
            var (map, _) = Open("maps/main/ch02/20_sewer.map");
            map.runScript = true; // the change state callback; OnStart did not run
            var panel = (PropMultiSlider)map.GetEntityByName("water_wheel_cog_panel_1");
            check(panel != null && panel.NumOfStates == 3 && Mathf.Approximately(panel.StatePosition(0), -0.425f) &&
                  Mathf.Approximately(panel.StatePosition(1), 0f) && Mathf.Approximately(panel.StatePosition(2), 0.425f),
                  "cog panel: 3 states over the slider range -0.425..0.425");
            check(panel.GetSlideData() != null && Mathf.Approximately(panel.GetSlideData().slideMaxSpeed, 10f) &&
                  panel.GetFocusCrosshair(panel.MainBody ?? panel.PhysicsBodies.First(b => b.Mass > 0f), Vector3.zero) == FocusCrosshair.Grab,
                  "slide data of the .ent, Grab crosshair");
            Step(map, 1f);
            check(panel.CurrentState >= 0, $"near a state at start: current state {panel.CurrentState}");
            CallScript(map, "SetMultiSliderStuckState", "water_wheel_cog_panel_1", 2, true);
            var joint = panel.Joints[0];
            check(panel.StuckState == 2 && panel.CurrentState == 2 && Mathf.Approximately(joint.MinDistance, 0.375f) && Mathf.Approximately(joint.MaxDistance, 0.425f) &&
                  map.Vars.TryGetValue("water_wheel_cog_panel_1", out var v2) && v2 == "2",
                  $"SetMultiSliderStuckState 2: the end state 5 cm inwards only, ChangeStateCallback CogPanelChange (local var 2) — " +
                  $"stuck {panel.StuckState} state {panel.CurrentState} limits {joint.MinDistance:0.000}..{joint.MaxDistance:0.000} var " +
                  (map.Vars.TryGetValue("water_wheel_cog_panel_1", out var dbg) ? dbg : "none"));
            Step(map, 2f);
            check(panel.Distance > 0.37f && panel.Distance < 0.43f, $"stuck at the end ({panel.Distance:0.000})");
            CallScript(map, "SetMultiSliderStuckState", "water_wheel_cog_panel_1", 1, true);
            check(Mathf.Approximately(joint.MinDistance, -0.05f) && Mathf.Approximately(joint.MaxDistance, 0.05f) && map.Vars["water_wheel_cog_panel_1"] == "1",
                  "stuck in the middle state: ±5 cm");
            Step(map, 2f); // the limits bring the slide to the middle
            CallScript(map, "SetMultiSliderStuckState", "water_wheel_cog_panel_1", -1, false);
            Step(map, 3f);
            check(panel.StuckState == -1 && Mathf.Approximately(joint.MinDistance, -0.425f) && Mathf.Abs(panel.Distance) < 0.06f && panel.CurrentState == 1,
                  $"not stuck: full range, the auto move keeps it at state 1 ({panel.Distance:0.000})");
            CallScript(map, "SetMultiSliderCallback", "water_wheel_cog_panel_1", "");
            CallScript(map, "SetMultiSliderStuckState", "water_wheel_cog_panel_1", 0, false);
            check(map.Vars["water_wheel_cog_panel_1"] == "1", "SetMultiSliderCallback \"\" removes the callback");
        }

        // ================================================================== keys

        static void Keys(Action<bool, string> check)
        {
            PlayerPrefs.DeleteKey("RTXMnesia.KeyBindings.v1");
            HplKeyBindings.Apply(Array.Empty<(string, string, string)>()); // loads the defaults
            check(HplKeyBindings.Get("Forward").primary == "Keyboard.W" && HplKeyBindings.Get("Interact").primary == "MouseButton.LeftMouse" &&
                  HplKeyBindings.Get("CrosshairToggle").primary == "Keyboard.X" && HplKeyBindings.Get("Lean") == null &&
                  HplKeyBindings.Actions.Count == 18, "18 configurable actions with the default_user_keys.cfg inputs (no gamepad, no Lean)");
            var config = new HplKeyConfig();
            config.Begin();
            check(!config.HasInvalidInputs && config.EntriesOf(0).Count() == 9 && config.EntriesOf(1).Count() == 4 && config.EntriesOf(2).Count() == 5,
                  "key config: movement 9, actions 4, misc 5, nothing invalid");
            int jump = config.Entries.FindIndex(e => e.name == "Jump");
            int forward = config.Entries.FindIndex(e => e.name == "Forward");
            config.StartWaiting(jump, 0, 0);
            config.Assign("Keyboard.W");
            check(config.HasInvalidInputs && config.Entries[jump].invalid[0] && config.Entries[forward].invalid[0], "W for jump too: both marked");
            config.StartWaiting(jump, 1, 0);
            config.Assign("Keyboard.W");
            check(config.Entries[jump].inputs[1] == "", "an input the action already has is not taken again");
            config.SetDefaults();
            check(!config.HasInvalidInputs && config.Entries[jump].inputs[0] == "Keyboard.Space", "defaults restore the inputs");
            config.StartWaiting(jump, 1, 0);
            config.Assign("MouseButton.MiddleMouse");
            config.Apply();
            check(HplKeyBindings.Get("Jump").secondary == "MouseButton.MiddleMouse", "OK applies the draft");
            var saved = PlayerPrefs.GetString("RTXMnesia.KeyBindings.v1");
            check(saved.Contains("MiddleMouse"), "and saves it");
            config.SetDefaults();
            config.Apply();
        }

        // ================================================================== water

        static void Water(Action<bool, string> check)
        {
            Material Mat(string rel) => AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.MaterialsRoot + "/" + rel + ".mat");
            var sewer = Mat("textures/water/water_sewer");
            var cellar = Mat("textures/water/water_archivecellar");
            var blood = Mat("textures/water/water_blood_hole");
            check(sewer != null && sewer.shader.name == "RTXMnesia/HplWater" && sewer.GetFloat("_ReflectionMode") == 1f && sewer.GetTexture("_CubeMap") is Cubemap,
                  "water_sewer: HplWater with its cube map reflection");
            check(cellar != null && cellar.GetFloat("_ReflectionMode") == 2f && Mathf.Approximately(cellar.GetVector("_ReflectionFade").w, 8f),
                  "water_archivecellar: world reflection, faded out by 8 m");
            check(blood != null && blood.GetFloat("_ReflectionMode") == 0f && Mathf.Approximately(blood.GetFloat("_WaveAmplitude"), 0.7f * 0.04f),
                  "water_blood_hole: no reflection (fake shading), amplitude × 0.04");
            foreach (string shaderPath in new[] { "Assets/Shaders/HplWater.shader", "Assets/Shaders/HplScreenEffects.shader", "Assets/Shaders/HplGui.shader" })
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
                check(shader != null && !ShaderUtil.ShaderHasError(shader), $"{shaderPath} compiles without errors");
            }

            var (map, _) = Open("maps/main/ch01/07_archives_cellar.map");
            var reflection = HplWaterReflection.Create(map.GetComponentsInChildren<MeshRenderer>(true), map.transform);
            check(reflection != null && reflection.SurfaceCount >= 1, $"07: one-sided water surfaces with world reflection ({reflection?.SurfaceCount})");
            UnityEngine.Object.DestroyImmediate(reflection.gameObject);
        }

        // ================================================================== pre menu

        static void PreMenu(Action<bool, string> check)
        {
            var assets = Resources.Load<HplPreMenuAssets>(HplPreMenuAssets.ResourceName);
            check(assets != null && assets.sections.Count == 8 && assets.gammaImage != null && assets.guiShader != null,
                  "pre menu resources: 8 sections, gamma image, GUI shader");
            check(assets.sections[0].image != null && assets.sections[0].time == 2f && !assets.sections[0].showFirstStartOnly &&
                  assets.sections.Skip(2).All(s => s.showFirstStartOnly && s.texts.Count == 1) &&
                  assets.sections.Count(s => s.hasGammaSettings) == 1 && assets.sections[2].music == "12_amb.ogg",
                  "two logos, six first start sections with text, one with gamma, music on the first welcome");
            var welcome = assets.sections[2].texts[0];
            check(welcome.category == "PreMenu" && welcome.entry == "WelcomeMessage01" && welcome.frameSize == new Vector2(640f, 300f) &&
                  welcome.fontSize == new Vector2(18f, 18f), "welcome text: PreMenu / WelcomeMessage01 in a 640 × 300 frame, size 18");
        }

        // ================================================================== 01_old_archives: lean, lamp oil

        static void LeanAndOil(Action<bool, string> check)
        {
            var (map, player) = Open("maps/main/ch01/01_old_archives.map");
            Step(map, 0.5f);

            // cLuxPlayerLean: the head shape ignores character bodies (abCollideCharacter = false), the own body too.
            for (int i = 0; i < 60; i++) { player.held = default; player.held.leanRight = true; map.Step(Time.fixedDeltaTime); }
            float right = player.Lean.Movement, rightRoll = player.Roll;
            for (int i = 0; i < 90; i++) { player.held = default; player.held.leanLeft = true; map.Step(Time.fixedDeltaTime); }
            float left = player.Lean.Movement, leftRoll = player.Roll;
            player.held = default;
            Step(map, 1f);
            check(right > 0.3f && rightRoll < -0.05f && left < -0.3f && leftRoll > 0.05f && player.Lean.Movement == 0f,
                  $"lean: E {right:0.00} m roll {rightRoll * Mathf.Rad2Deg:0.0}°, Q {left:0.00} m roll {leftRoll * Mathf.Rad2Deg:0.0}°, released → 0");

            // Lamp oil: the map's three potions (two inactive in the map, as in the original), an item of type LampOil.
            var potions = map.Entities.OfType<PropItem>().Where(p => p.Name.StartsWith("potion_oil", StringComparison.Ordinal)).ToList();
            var active = potions.Where(p => p.Active).ToList();
            check(potions.Count == 3 && potions.All(p => p.ItemType == "LampOil") && active.Count == 1 && active[0].Name == "potion_oil_2" &&
                  active[0].GameObject.activeInHierarchy && active[0].GameObject.GetComponentsInChildren<MeshRenderer>().Any(r => r.enabled),
                  $"oil potions: {potions.Count} of type LampOil, active and drawn: {string.Join(", ", active.Select(p => p.Name))}");

            HplGame.Reset();
            HplGame.AddItem(map, "lantern", "Lantern", "Lantern", "", 1f, "", "", out _);
            var potion = active[0];
            check(potion.CanInteract(null) && potion.OnInteract(null, Vector3.zero), "potion_oil_2 picked up");
            Step(map, 0.1f);
            var oil = HplGame.Items.FirstOrDefault(i => i.type == "LampOil");
            check(oil != null && oil.amount > 0f && potion.DestroyMe, $"inventory: LampOil x{oil?.count} amount {oil?.amount}, prop removed");

            var inventory = new HplInventory(map);
            try
            {
                player.SetLampOil(20f);
                float amount = oil.amount;
                check(inventory.Use(oil) && Mathf.Approximately(player.lampOil, Mathf.Min(100f, 20f + amount)) && !HplGame.Items.Contains(oil),
                      $"using the oil: lamp oil 20 → {player.lampOil:0} (+{amount}), the potion used up");
            }
            finally { inventory.View.Destroy(); }
            HplGame.Reset();
        }

        // ================================================================== helpers

        static (HplMap map, AmnesiaPlayer player) Open(string mapFile)
        {
            string scenePath = LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, mapFile));
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            HplMath.Seed(1);
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
                                                                   System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                                                        null, args.Select(a => a.GetType()).ToArray(), null);
            if (method == null) throw new InvalidOperationException($"no script function {function}");
            return method.Invoke(map.Script, args);
        }

        static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic)?.Invoke(target, null);
    }
}
