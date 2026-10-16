using System;
using System.IO;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    public static class MainMenuTests
    {
        [MenuItem("Tools/RTXMnesia/Tests/Main menu")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run title menu model checks outside Play mode.");
            float scale = Time.timeScale; var cursorLock = Cursor.lockState; bool cursorVisible = Cursor.visible;
            var root = new GameObject("Main menu checks") { hideFlags = HideFlags.HideAndDontSave };
            int checks = 0;
            void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
            try
            {
                var map = root.AddComponent<HplMap>(); map.menuBackground = true;
                map.hud = root.AddComponent<HplHud>();
                map.language = AssetDatabase.LoadAssetAtPath<RTXMnesia.Hpl.HplLanguage>("Assets/AmnesiaImported/Lang/russian.asset");
                map.menuConfig = AssetDatabase.LoadAssetAtPath<RTXMnesia.Hpl.HplConfig>("Assets/AmnesiaImported/Config/menu.asset");
                var bank = Resources.Load<HplMenuAssets>(HplMenuAssets.ResourceName);
                map.hud.defaultFont = map.hud.hintsFont = map.hud.focusFont = bank.guiFont; map.hud.Init(map);
                Check(bank.titleLogo != null && AssetDatabase.GetAssetPath(bank.titleLogo).Contains("/lang/rus/"), "Title logo did not use Russian language resources");
                Time.timeScale = 0.75f;
                var ui = root.AddComponent<HplGameUi>(); ui.Init(map, true);
                Check(ui.IsTitleScreen && ui.IsPaused && !map.IsMenuPaused && Time.timeScale == 0.75f, "Title controller paused the decorative world / changed the clock");
                Check(ui.Inventory == null && ui.Journal == null && map.player == null, "Title screen created gameplay systems / player");
                Check(ui.Menu.ButtonCount == 3 && ui.Menu.ControlAt(0) == MenuControl.NewGame, "Title controls are not New Game / Options / Exit");
                Check(!ui.Choose(MenuControl.Resume) && !ui.Choose(MenuControl.ReturnTitle), "Title exposes in-game actions");
                HplGame.SetGlobalVar("title_cancel", "keep");
                Check(ui.Choose(MenuControl.NewGame) && ui.ConfirmingQuit && ui.PendingConfirmation == MenuControl.ConfirmNewGame && !ui.IsTransitioning, "New game bypassed confirmation");
                Check(ui.Choose(MenuControl.Cancel) && HplGame.GetGlobalVar("title_cancel") == "keep" && !ui.IsTransitioning, "Cancel new game changed progression");
                Check(ui.Choose(MenuControl.Options) && ui.ShowingOptions, "Options did not open on title screen");
                float sensitivity = ui.Settings.mouseSensitivity; ui.DraftSettings.mouseSensitivity = 4.7f;
                Check(ui.Choose(MenuControl.Cancel) && ui.Settings.mouseSensitivity == sensitivity, "Cancel on title screen applied draft preferences");
                Check(ui.Choose(MenuControl.Quit) && ui.PendingConfirmation == MenuControl.ConfirmQuit, "Exit did not use its own confirmation"); ui.Choose(MenuControl.Cancel);
                ui.Pause(); Check(ui.IsTitleScreen && !map.IsMenuPaused && Time.timeScale == 0.75f, "Title pause action changed world state");
                var scenes = EditorBuildSettings.scenes;
                Check(scenes.Length >= 2 && scenes[0].enabled && scenes[0].path == HplGameUi.MainMenuScene && scenes[1].enabled && scenes[1].path == HplGameUi.FirstLevelScene, "Build scene order does not start at title menu / include the first map");
                var globalScenes = EditorBuildSettings.globalScenes;
                Check(globalScenes.Length >= 2 && globalScenes[0].path == HplGameUi.MainMenuScene && globalScenes[1].path == HplGameUi.FirstLevelScene, "Shared scene list still points at SampleScene");
                Check(File.Exists(HplGameUi.MainMenuScene) && File.Exists(HplGameUi.FirstLevelScene), "Required scene missing");
                string report = $"PASS: {checks} main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).";
                Directory.CreateDirectory("Logs/MainMenuCheck"); File.WriteAllText("Logs/MainMenuCheck/Tests.txt", report); Debug.Log(report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root); Time.timeScale = scale; Cursor.lockState = cursorLock; Cursor.visible = cursorVisible;
                HplGame.Reset();
            }
        }
    }
}
