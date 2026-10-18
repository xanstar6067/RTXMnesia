#if UNITY_EDITOR
using System.Collections;
using System.IO;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>Editor-only Play-mode screenshot probe; excluded from player builds.</summary>
    public sealed class HplMenuPreviewProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;
            var map = HplMap.Current;
            if (map == null || map.GameUi == null) { Finish(1); yield break; }
            map.effects.ResetAll();
            map.GameUi.Pause();
            // Batch has no Game view framebuffer. Render the real player loop and UI into a camera target.
            var camera = map.PlayerCamera;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            camera.targetTexture = target;
            Canvas menuCanvas = null;
            foreach (var canvas in map.GetComponentsInChildren<Canvas>())
                if (canvas.name == "HPL game menu") menuCanvas = canvas;
            if (menuCanvas == null) { Finish(1); yield break; }
            menuCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            menuCanvas.worldCamera = camera;
            menuCanvas.planeDistance = 1f;
            // Keep UI out of HDRP's scene capture; runtime ScreenSpaceOverlay likewise renders after effects.
            foreach (var child in menuCanvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            camera.cullingMask &= ~(1 << 31);
            yield return new WaitForSecondsRealtime(1.3f);
            Canvas.ForceUpdateCanvases();
            int vertices = 0;
            foreach (var text in map.GetComponentsInChildren<HplGuiText>())
            {
                var mesh = text.canvasRenderer != null ? text.canvasRenderer.GetMesh() : null;
                if (mesh != null) vertices += mesh.vertexCount;
            }
            if (vertices == 0) { Debug.LogError("Menu has no rendered text geometry"); Finish(1); yield break; }
            string folder = Path.GetFullPath("Logs/MenuCheck");
            Directory.CreateDirectory(folder);
            string pausePath = Path.Combine(folder, "Pause.png");
            Capture(target, pausePath, camera, menuCanvas);
            map.GameUi.OpenOptions();
            yield return new WaitForSecondsRealtime(0.3f);
            string optionsPath = Path.Combine(folder, "Options.png");
            Capture(target, optionsPath, camera, menuCanvas);
            for (int tab = 1; tab < 4; tab++)
            {
                map.GameUi.OpenOptions(tab);
                yield return new WaitForSecondsRealtime(0.2f);
                Capture(target, Path.Combine(folder, $"Options_{tab}.png"), camera, menuCanvas);
            }
            Debug.Log($"PASS: Play-mode menu, {vertices} text vertices; screenshots {folder}");
            int graphicsFailures = 0;
            yield return GraphicsOptions(map, target, camera, menuCanvas, folder, f => graphicsFailures = f);
            if (graphicsFailures > 0) { Finish(1); yield break; }
            map.GameUi.Resume();
            // Synthetic contents for visual checks only. This batch never saves a scene or a game.
            map.player.active = true;
            // The first map intentionally blocks inventory throughout its introduction.
            if (map.Script != null) typeof(HplScript).GetProperty(nameof(HplScript.InventoryDisabled)).SetValue(map.Script, false);
            map.player.health = 65;
            map.player.sanity = 45;
            map.player.lampOil = 42;
            map.player.tinderboxes = 7;
            HplGame.Items.Clear();
            var inventory = map.GameUi.Inventory;
            foreach (string file in new[] { "lantern.ent", "potion_health.ent", "potion_health.ent", "potion_sanity.ent", "potion_oil.ent", "key_laboratory.ent", "glass_container.ent" })
            {
                var definition = inventory.Assets.Item(file);
                if (definition == null) { Debug.LogError("Preview item missing: " + file); Finish(1); yield break; }
                HplGame.AddItem(map, file, definition.type, definition.subType, definition.image, definition.amount, "", "", out _);
            }
            if (!map.GameUi.OpenInventory()) { Debug.LogError("Inventory refused to open"); Finish(1); yield break; }
            Canvas inventoryCanvas = null;
            foreach (var canvas in map.GetComponentsInChildren<Canvas>()) if (canvas.name == "HPL inventory") inventoryCanvas = canvas;
            if (inventoryCanvas == null) { Debug.LogError("Inventory canvas missing"); Finish(1); yield break; }
            inventoryCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            inventoryCanvas.worldCamera = camera;
            inventoryCanvas.planeDistance = 1;
            foreach (var child in inventoryCanvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            yield return new WaitForSecondsRealtime(1.3f);
            inventory.Select(1);
            inventory.View.Refresh(1, Vector2.zero);
            Capture(target, Path.Combine(folder, "Inventory.png"), camera, inventoryCanvas);
            inventory.SetMessage(inventory.T("CombinationDoesNotWork"), -1);
            inventory.View.Refresh(1, Vector2.zero);
            Capture(target, Path.Combine(folder, "InventoryMessage.png"), camera, inventoryCanvas);
            map.GameUi.Resume();
            Debug.Log("PASS: Play-mode inventory screenshots");
            HplGame.JournalEntries.Clear(); HplGame.Quests.Clear();
            var note = HplGame.AddJournalEntry("Note", "CH01L03_VillageTales", "journal/default_note");
            var diary = HplGame.AddJournalEntry("Diary", "CH01L03_Daniel", "journal/diary_page");
            HplGame.AddQuest("preview", "00Trail");
            var journal = map.GameUi.Journal;
            if (!map.GameUi.OpenJournal()) { Debug.LogError("Journal refused to open"); Finish(1); yield break; }
            var journalCanvas = journal.View.Canvas;
            journalCanvas.renderMode = RenderMode.ScreenSpaceCamera; journalCanvas.worldCamera = camera; journalCanvas.planeDistance = 1;
            foreach (var child in journalCanvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            yield return new WaitForSecondsRealtime(0.8f);
            journal.View.Refresh(1, Vector2.zero); Capture(target, Path.Combine(folder, "JournalMain.png"), camera, journalCanvas);
            foreach (var state in new[] { JournalState.Notes, JournalState.Diaries, JournalState.QuestLog })
            {
                journal.ChangeState(state); yield return new WaitForSecondsRealtime(0.6f);
                journal.View.Refresh(1, Vector2.zero); Capture(target, Path.Combine(folder, "Journal" + state + ".png"), camera, journalCanvas);
            }
            journal.OpenEntry(note, false); yield return new WaitForSecondsRealtime(0.6f);
            journal.View.Refresh(1, Vector2.zero); Capture(target, Path.Combine(folder, "JournalNote.png"), camera, journalCanvas);
            journal.TurnPage(1); yield return null;
            journal.View.Refresh(1, Vector2.zero); Capture(target, Path.Combine(folder, "JournalNotePage2.png"), camera, journalCanvas);
            journal.OpenEntry(diary, true); yield return new WaitForSecondsRealtime(0.6f);
            journal.View.Refresh(1, Vector2.zero); Capture(target, Path.Combine(folder, "JournalNarrated.png"), camera, journalCanvas);
            map.GameUi.Resume(); Debug.Log("PASS: Play-mode journal screenshots");
            camera.targetTexture = null;
            target.Release();
            Destroy(target);
            Finish(0);
        }

        /// <summary>
        /// The graphics tab: its drop-down lists, a disabled choice, picking choices, applying them (quality settings,
        /// camera anti-aliasing, shadows, the settings volume, blur flags) and putting the user's settings back.
        /// </summary>
        IEnumerator GraphicsOptions(HplMap map, RenderTexture target, Camera camera, Canvas menuCanvas, string folder, System.Action<int> result)
        {
            int failures = 0;
            void Check(bool ok, string what)
            {
                if (ok) Debug.Log("PASS: graphics options: " + what);
                else { failures++; Debug.LogError("FAIL: graphics options: " + what); }
            }
            var ui = map.GameUi;
            string savedPrefs = PlayerPrefs.HasKey(HplUserSettings.PrefsKey) ? PlayerPrefs.GetString(HplUserSettings.PrefsKey) : null;
            var original = ui.Settings.Copy();

            ui.OpenOptions(1);
            yield return null;
            bool allCombos = true;
            foreach (var c in new[] { MenuControl.Lighting, MenuControl.Textures, MenuControl.Resolution, MenuControl.ScreenMode, MenuControl.FpsLimit,
                                      MenuControl.TextureQuality, MenuControl.Anisotropy, MenuControl.Antialiasing, MenuControl.Shadows,
                                      MenuControl.VSync, MenuControl.Bloom, MenuControl.ImageTrail, MenuControl.RadialBlur, MenuControl.GammaLess })
            {
                bool found = false;
                for (int i = 0; i < ui.Menu.ButtonCount; i++) found |= ui.Menu.ControlAt(i) == c;
                if (!found) { allCombos = false; Debug.LogError("Graphics tab misses " + c); }
            }
            Check(allCombos, "every list, check box and the gamma are on the tab");

            ui.Choose(MenuControl.Resolution);
            yield return null;
            Check(ui.OpenCombo != null && ui.OpenCombo.control == MenuControl.Resolution, "the resolution list opens");
            Capture(target, Path.Combine(folder, "Options_1_Resolution.png"), camera, menuCanvas);
            ui.ChooseComboItem(-1);
            ui.Choose(MenuControl.Cancel);
            ui.OpenOptions(1);
            yield return null;

            ui.Choose(MenuControl.Lighting);
            ui.ChooseComboItem(1);
            if (SystemInfo.supportsRayTracing)
                Check(ui.OpenCombo == null && ui.DraftSettings.lighting == 1, "ray tracing lighting picked (DXR supported), the list closes");
            else
                Check(ui.OpenCombo != null && ui.DraftSettings.lighting == 0, "ray tracing lighting cannot be picked without DXR, the list stays open");
            if (ui.OpenCombo == null) ui.Choose(MenuControl.Lighting);
            ui.ChooseComboItem(0);
            Check(ui.OpenCombo == null && ui.DraftSettings.lighting == 0, "classic lighting picked, the list closes");
            ui.Choose(MenuControl.FpsLimit);
            yield return null;
            Capture(target, Path.Combine(folder, "Options_1_FpsLimit.png"), camera, menuCanvas);
            ui.ChooseComboItem(2);
            ui.Choose(MenuControl.Antialiasing); ui.ChooseComboItem((int)Hpl.HplAntialiasing.Fxaa);
            ui.Choose(MenuControl.TextureQuality); ui.ChooseComboItem(1);
            ui.Choose(MenuControl.Anisotropy); ui.ChooseComboItem(4);
            ui.Choose(MenuControl.Shadows); ui.ChooseComboItem(0);
            if (ui.DraftSettings.imageTrail) ui.Choose(MenuControl.ImageTrail);
            if (ui.DraftSettings.radialBlur) ui.Choose(MenuControl.RadialBlur);
            var d = ui.DraftSettings;
            Check(d.fpsLimit == 120 && d.antialiasing == (int)Hpl.HplAntialiasing.Fxaa && d.textureQuality == 1 && d.anisotropy == 4 &&
                  d.shadows == 0 && !d.imageTrail && !d.radialBlur, "choices change the draft");
            yield return null;
            Capture(target, Path.Combine(folder, "Options_1_Changed.png"), camera, menuCanvas);
            ui.Choose(MenuControl.Apply);
            yield return null;
            yield return null;
            var hd = camera.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
            Check(hd != null && hd.antialiasing == UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing,
                  "the player camera renders with FXAA");
            Check(QualitySettings.globalTextureMipmapLimit == 1 && QualitySettings.anisotropicFiltering == AnisotropicFiltering.ForceEnable &&
                  Application.targetFrameRate == 120, "texture quality, 16x anisotropy and the FPS limit applied");
            bool shadowsOff = true;
            int casting = 0;
            foreach (var light in World.AmnesiaLight.All)
                if (light.castShadows && light.TryGetComponent<Light>(out var l)) { casting++; shadowsOff &= l.shadows == LightShadows.None; }
            Check(shadowsOff, $"shadows off ({casting} shadow casting lights)");
            Check(!Hpl.HplGraphics.ImageTrail && !Hpl.HplGraphics.RadialBlur, "image trail and radial blur hidden");
            var volume = Hpl.HplGraphics.SettingsVolume;
            Check(volume != null && volume.isActiveAndEnabled && volume.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.MotionBlur>(out var blur) && blur.intensity.value == 0f,
                  "the settings volume turns HDRP motion blur off");

            // The RTX tab: DLSS mode and model; with DLSS on the graphics tab's anti-aliasing shows "DLSS".
            ui.OpenOptions(Ui.HplPauseMenu.OptionsTabs - 1);
            yield return null;
            bool rtxControls = false, presetList = false;
            for (int i = 0; i < ui.Menu.ButtonCount; i++)
            {
                rtxControls |= ui.Menu.ControlAt(i) == MenuControl.DlssMode;
                presetList |= ui.Menu.ControlAt(i) == MenuControl.DlssPreset;
            }
            Check(rtxControls && presetList, "the RTX tab has the DLSS mode and model lists");
            Capture(target, Path.Combine(folder, "Options_RTX_Off.png"), camera, menuCanvas);
            if (Hpl.HplDlss.Available)
            {
                ui.Choose(MenuControl.DlssMode);
                yield return null;
                int rows = 0;
                for (int i = 0; i < ui.Menu.ButtonCount; i++) if (ui.Menu.ControlAt(i) == MenuControl.ComboItem) rows++;
                Check(rows == 6, $"the open DLSS list is drawn on the RTX tab ({rows} of 6 choices)");
                Capture(target, Path.Combine(folder, "Options_RTX_ModeList.png"), camera, menuCanvas);
                ui.ChooseComboItem((int)Hpl.HplDlssMode.Performance);
                Check(ui.DraftSettings.dlssMode == (int)Hpl.HplDlssMode.Performance, "DLSS Performance picked");
                ui.Choose(MenuControl.DlssPreset);
                yield return null;
                Capture(target, Path.Combine(folder, "Options_RTX_Models.png"), camera, menuCanvas);
                ui.ChooseComboItem(0);
                ui.Choose(MenuControl.Apply);
                for (int i = 0; i < 5; i++) yield return null;
                Check(Hpl.HplDlss.Active && hd.allowDynamicResolution && hd.antialiasing == UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.None,
                      "DLSS applied: the player camera allows dynamic resolution, its own anti-aliasing is off");
                var hdCamera = UnityEngine.Rendering.HighDefinition.HDCamera.GetOrCreate(camera);
                Check(hdCamera.actualWidth < camera.pixelWidth, $"DLSS renders below the output size ({hdCamera.actualWidth} of {camera.pixelWidth})");
                ui.OpenOptions(Ui.HplPauseMenu.OptionsTabs - 1);
                yield return null;
                Capture(target, Path.Combine(folder, "Options_RTX_Performance.png"), camera, menuCanvas);
                ui.OpenOptions(1);
                yield return null;
                var aa = HplGraphicsOptions.Items(MenuControl.Antialiasing, ui.Settings, ui.Menu.Translate, out _);
                Check(aa.Count == 1 && aa[0].label == "DLSS" && !aa[0].enabled, "the graphics tab's anti-aliasing shows DLSS and cannot be changed");
                Capture(target, Path.Combine(folder, "Options_1_DlssAA.png"), camera, menuCanvas);
            }
            else Debug.Log("DLSS is not available on this GPU: the RTX tab shows why");

            // Ray tracing lighting: its settings appear on the RTX tab.
            if (SystemInfo.supportsRayTracing)
            {
                ui.OpenOptions(1);
                yield return null;
                ui.Choose(MenuControl.Lighting);
                ui.ChooseComboItem(1);
                // The checks above turned the shadows off.
                ui.Choose(MenuControl.Shadows);
                ui.ChooseComboItem(Hpl.HplGraphics.ShadowsHigh);
                ui.Choose(MenuControl.RtxTab);
                yield return null;
                bool rtShadows = false, rtAo = false, rtReflections = false, rtGi = false, rtAll = false, rtLantern = false;
                for (int i = 0; i < ui.Menu.ButtonCount; i++)
                {
                    rtShadows |= ui.Menu.ControlAt(i) == MenuControl.RtShadows;
                    rtAo |= ui.Menu.ControlAt(i) == MenuControl.RtAo;
                    rtReflections |= ui.Menu.ControlAt(i) == MenuControl.RtReflections;
                    rtGi |= ui.Menu.ControlAt(i) == MenuControl.RtGi;
                    rtAll |= ui.Menu.ControlAt(i) == MenuControl.RtAllShadows;
                    rtLantern |= ui.Menu.ControlAt(i) == MenuControl.RtLanternShadows;
                }
                Check(rtShadows && rtAo && rtReflections && rtGi && rtAll && rtLantern, "with ray tracing lighting the RTX tab has the lists of RT shadows, shadows from all lights, lantern shadows, RTAO, reflections and RTGI");
                ui.Choose(MenuControl.RtShadows);
                yield return null;
                int rows = 0;
                for (int i = 0; i < ui.Menu.ButtonCount; i++) if (ui.Menu.ControlAt(i) == MenuControl.ComboItem) rows++;
                Check(rows == Hpl.HplRayTracing.ShadowLights.Length, $"the open RT shadows list is drawn ({rows} of {Hpl.HplRayTracing.ShadowLights.Length} choices)");
                Capture(target, Path.Combine(folder, "Options_RTX_RtShadowsList.png"), camera, menuCanvas);
                ui.ChooseComboItem(1);
                Check(ui.DraftSettings.rtShadows == 1, "RT shadows Low picked");
                ui.Choose(MenuControl.Apply);
                for (int i = 0; i < 5; i++) yield return null;
                int traced = 0;
                foreach (var light in World.AmnesiaLight.All)
                    if (light != null && light.TryGetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>(out var hdl) && hdl.useRayTracedShadows) traced++;
                Check(Hpl.HplRayTracing.Active && traced > 0 && traced <= Hpl.HplRayTracing.ShadowLights[1], $"ray tracing applied: {traced} lights with ray-traced shadows (at most {Hpl.HplRayTracing.ShadowLights[1]})");
                ui.OpenOptions(Ui.HplPauseMenu.OptionsTabs - 1);
                yield return null;
                Capture(target, Path.Combine(folder, "Options_RTX_RtShadows.png"), camera, menuCanvas);
            }

            // The user's settings back (applied and saved as they were).
            original.Apply(map);
            ui.Settings = original;
            if (savedPrefs != null) PlayerPrefs.SetString(HplUserSettings.PrefsKey, savedPrefs); else PlayerPrefs.DeleteKey(HplUserSettings.PrefsKey);
            PlayerPrefs.Save();
            bool restored = true;
            foreach (var light in World.AmnesiaLight.All)
                if (light.castShadows && light.TryGetComponent<Light>(out var l)) restored &= l.shadows != LightShadows.None == (original.shadows > 0);
            Check(restored && Hpl.HplGraphics.ImageTrail == original.imageTrail, "the user's settings restored");
            if (original.lighting == 0)
            {
                bool rtOff = !Hpl.HplRayTracing.Active;
                foreach (var light in World.AmnesiaLight.All)
                    if (light != null && light.TryGetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>(out var hdl)) rtOff &= !hdl.useRayTracedShadows;
                Check(rtOff, "classic lighting back: no ray-traced shadows");
            }
            ui.OpenOptions(0);
            result(failures);
        }

        static void Capture(RenderTexture target, string path, Camera camera, Canvas menuCanvas)
        {
            // Batch has no overlay framebuffer. The existing snapshot helper composites the real Canvas
            // meshes / font atlases after the GPU-rendered world, just as it does for HUD snapshots.
            Canvas.ForceUpdateCanvases();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            System.Type.GetType("RTXMnesia.EditorDev.HudSnapshot, Assembly-CSharp-Editor")
                .GetMethod("DrawCanvas").Invoke(null, new object[] { image, menuCanvas, camera });
            File.WriteAllBytes(path, image.EncodeToPNG());
            Destroy(image);
            RenderTexture.active = previous;
        }

        static void Finish(int code)
        {
            UnityEditor.SessionState.SetInt("RTXMnesia.MenuPreview.ExitCode", code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
