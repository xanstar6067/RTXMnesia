#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Dev
{
    /// <summary>Editor-only Play smoke test of the real startup scene and two new-game loads.</summary>
    public sealed class HplMainMenuPreviewProbe : MonoBehaviour
    {
        int _checks;
        Camera _camera;
        Canvas _canvas;
        RenderTexture _target;
        string Folder => Path.GetFullPath("Logs/MainMenuCheck");

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
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
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); _checks++; }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Folder);
            // Saves of this run in their own empty folder (the title shows Continue / Load game only with saves).
            string saves = Path.Combine(Folder, "Saves");
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
            RTXMnesia.Save.HplSaveHandler.FolderOverride = saves;
            yield return new WaitForSecondsRealtime(1.2f);
            var map = HplMap.Current;
            var title = map.GetComponent<HplMainMenu>(); var ui = title.Ui;
            Check(SceneManager.GetActiveScene().path == HplGameUi.MainMenuScene && map.menuBackground && map.player == null && map.Script == null, "Startup is not the decorative main menu");
            Check(ui.IsTitleScreen && !map.IsMenuPaused && map.GameTime > 0 && Time.timeScale == 1, "Title world does not update independently of UI");
            Check(map.sound.Music.CurrentSongName == "game_menu.ogg" && !map.sound.MenuPaused, "Original title music did not start");
            CaptureSetup(map.PlayerCamera, ui.Menu.Canvas);
            yield return new WaitForSecondsRealtime(0.3f); Capture("MainMenu.png");
            Check(ui.Choose(MenuControl.Options), "Cannot open title options");
            yield return new WaitForSecondsRealtime(0.2f); Capture("MainMenuOptions.png"); ui.Choose(MenuControl.Cancel);
            Check(ui.Choose(MenuControl.NewGame) && ui.PendingConfirmation == MenuControl.ConfirmNewGame, "New game confirmation missing");
            yield return new WaitForSecondsRealtime(0.2f); Capture("NewGameConfirmation.png");
            var oldSound = map.sound;
            HplGame.Items.Add(new HplGame.Item { name = "old_inventory" }); HplGame.SetGlobalVar("old_progress", "1");
            HplGame.AddJournalEntry("Note", "old_note", "journal/default_note");
            if (map.Entities.Count > 0) PlayerStateVars.InteractProp = map.Entities[0] as HplProp;
            CaptureCleanup();
            Check(ui.Choose(MenuControl.ConfirmNewGame) && ui.IsTransitioning && !ui.Choose(MenuControl.NewGame), "New game transition is not protected from repeated input");
            double deadline = Time.realtimeSinceStartupAsDouble + 45;
            while (SceneManager.GetActiveScene().path != HplGameUi.FirstLevelScene && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            yield return null; yield return null;
            map = HplMap.Current;
            Check(map != null && !map.menuBackground && map.player != null && map.Script != null && SceneManager.GetActiveScene().path == HplGameUi.FirstLevelScene, "New game did not load 00_rainy_hall with its script");
            Check(!HplGame.GlobalVars.ContainsKey("old_progress") && HplGame.Items.Count == 0 && HplGame.JournalEntries.Count == 0 && PlayerStateVars.InteractProp == null, "New game retained old progression / interaction state");
            Check(oldSound == null && Time.timeScale == 1 && !map.IsMenuPaused && !map.player.UiInputBlocked, "New game retained old sound world or menu pause");
            Check(!map.player.active, "New game bypassed the original introduction");
            ui = map.GameUi; ui.Pause(); yield return new WaitForSecondsRealtime(1.1f);
            Check(ui.Choose(MenuControl.ReturnTitle) && ui.PendingConfirmation == MenuControl.ConfirmReturnTitle, "In-game menu has no confirmed return to title");
            CaptureSetup(map.PlayerCamera, ui.Menu.Canvas);
            yield return new WaitForSecondsRealtime(0.2f); Capture("ReturnToMenuConfirmation.png");
            HplGame.SetGlobalVar("return_cancel", "keep"); ui.Choose(MenuControl.Cancel);
            Check(HplGame.GetGlobalVar("return_cancel") == "keep" && map.IsMenuPaused, "Cancel return discarded the current game");
            ui.Choose(MenuControl.ReturnTitle); CaptureCleanup(); ui.Choose(MenuControl.ConfirmReturnTitle);
            deadline = Time.realtimeSinceStartupAsDouble + 45;
            while (SceneManager.GetActiveScene().path != HplGameUi.MainMenuScene && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            yield return null; yield return null;
            map = HplMap.Current; title = map != null ? map.GetComponent<HplMainMenu>() : null; ui = title != null ? title.Ui : null;
            Check(ui != null && ui.IsTitleScreen && Time.timeScale == 1 && !map.IsMenuPaused && !HplGame.GlobalVars.ContainsKey("return_cancel"), "Return did not unload gameplay / restore title clock");
            CaptureSetup(map.PlayerCamera, ui.Menu.Canvas); yield return new WaitForSecondsRealtime(1.2f); Capture("ReturnedMainMenu.png"); CaptureCleanup();
            HplGame.SetGlobalVar("second_game", "1"); ui.Choose(MenuControl.NewGame); ui.Choose(MenuControl.ConfirmNewGame);
            deadline = Time.realtimeSinceStartupAsDouble + 45;
            while (SceneManager.GetActiveScene().path != HplGameUi.FirstLevelScene && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            yield return null; yield return null;
            Check(SceneManager.GetActiveScene().path == HplGameUi.FirstLevelScene && HplMap.Current.player != null && !HplGame.GlobalVars.ContainsKey("second_game") && HplGame.Items.Count == 0 && HplGame.JournalEntries.Count == 0 && Time.timeScale == 1, "Second new game did not start cleanly");

            // Exit and save → Continue (cLuxMainMenu eLuxMainMenuExit_QuitAndSave, then ContinueGame → AutoLoad).
            map = HplMap.Current;
            yield return new WaitForSecondsRealtime(2f);
            HplGame.SetGlobalVar("saved_progress", "7"); map.SetVar("saved_local", "3"); map.player.health = 61f;
            Vector3 savedFeet = map.player.Body.FeetPosition;
            ui = map.GameUi; ui.Pause(); yield return new WaitForSecondsRealtime(1.1f);
            Check(ui.Choose(MenuControl.ExitAndSave) && ui.PendingConfirmation == MenuControl.ConfirmExitAndSave, "In-game menu has no confirmed exit and save");
            CaptureSetup(map.PlayerCamera, ui.Menu.Canvas);
            yield return new WaitForSecondsRealtime(0.2f); Capture("ExitAndSaveConfirmation.png"); CaptureCleanup();
            ui.Choose(MenuControl.ConfirmExitAndSave);
            Check(RTXMnesia.Save.HplSaveHandler.SaveFileExists(map.Campaign), "Exit and save wrote no save");
            deadline = Time.realtimeSinceStartupAsDouble + 45;
            while (SceneManager.GetActiveScene().path != HplGameUi.MainMenuScene && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            yield return null; yield return null;
            map = HplMap.Current; ui = map.GetComponent<HplMainMenu>().Ui;
            Check(ui.Menu.ControlAt(0) == MenuControl.Continue && ui.Menu.ControlAt(2) == MenuControl.LoadGame, "Title after saving has no Continue / Load game");
            CaptureSetup(map.PlayerCamera, ui.Menu.Canvas); yield return new WaitForSecondsRealtime(1.2f); Capture("MainMenuContinue.png");
            Check(ui.Choose(MenuControl.LoadGame) && ui.Menu.ControlAt(0) == MenuControl.LoadSlot, "Load game window lists no save");
            ui.Choose(MenuControl.LoadSlot);
            yield return new WaitForSecondsRealtime(0.2f); Capture("LoadGame.png"); ui.Choose(MenuControl.Cancel); CaptureCleanup();
            Check(ui.Choose(MenuControl.Continue) && ui.PendingConfirmation == MenuControl.ConfirmContinue, "Continue confirmation missing");
            ui.Choose(MenuControl.ConfirmContinue);
            deadline = Time.realtimeSinceStartupAsDouble + 45;
            while (SceneManager.GetActiveScene().path != HplGameUi.FirstLevelScene && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            yield return null; yield return null;
            map = HplMap.Current;
            Check(map != null && map.player != null && SceneManager.GetActiveScene().path == HplGameUi.FirstLevelScene, "Continue did not load the saved map");
            Check(HplGame.GetGlobalVar("saved_progress") == "7" && map.GetVar("saved_local") == "3", "Continue lost global / local variables");
            Check(Mathf.Abs(map.player.health - 61f) < 1f && Vector3.Distance(map.player.Body.FeetPosition, savedFeet) < 0.1f,
                  $"Continue lost the player (health {map.player.health:0}, at {map.player.Body.FeetPosition} vs {savedFeet})");
            Check(Time.timeScale == 1 && !map.IsMenuPaused && !RTXMnesia.Save.HplSaveHandler.LoadPending, "Loaded game is paused or the save still pending");
            string report = $"PASS: {_checks} Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game, exit and save, continue, load game); screenshots {Folder}";
            File.WriteAllText(Path.Combine(Folder, "PlayTests.txt"), report); Debug.Log(report);
        }
        void CaptureSetup(Camera camera, Canvas canvas)
        {
            _camera = camera; _canvas = canvas;
            _target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); _target.Create(); camera.targetTexture = _target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            camera.cullingMask &= ~(1 << 31);
        }
        void Capture(string name)
        {
            Canvas.ForceUpdateCanvases(); var previous = RenderTexture.active; RenderTexture.active = _target;
            var image = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0); image.Apply();
            Type.GetType("RTXMnesia.EditorDev.HudSnapshot, Assembly-CSharp-Editor").GetMethod("DrawCanvas").Invoke(null, new object[] { image, _canvas, _camera });
            File.WriteAllBytes(Path.Combine(Folder, name), image.EncodeToPNG()); Destroy(image); RenderTexture.active = previous;
        }
        void CaptureCleanup()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) { _target.Release(); Destroy(_target); }
            _target = null;
        }
        void Finish(int code)
        {
            CaptureCleanup(); RTXMnesia.Save.HplSaveHandler.FolderOverride = null; UnityEditor.SessionState.SetInt("RTXMnesia.MainMenuPreview.ExitCode", code); UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
