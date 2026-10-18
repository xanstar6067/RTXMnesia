using System;
using System.IO;
using System.Linq;
using RTXMnesia.Save;
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
            // An empty save folder: the title shows Continue / Load game only with saves.
            string saves = Path.GetFullPath("Logs/MainMenuCheck/Saves");
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
            RTXMnesia.Save.HplSaveHandler.FolderOverride = saves;
            // Profiles in a folder of the run; the user's preferences in PlayerPrefs are restored afterwards.
            string profiles = Path.GetFullPath("Logs/MainMenuCheck/Profiles");
            if (Directory.Exists(profiles)) Directory.Delete(profiles, true);
            string[] prefKeys = { HplUserSettings.PrefsKey, "RTXMnesia.KeyBindings.v1", "RTXMnesia.LastProfile" };
            var savedPrefs = prefKeys.Select(k => PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null).ToArray();
            bool hardMode = HplDifficulty.HardMode;
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
                Check(ui.Menu.ButtonCount == 5 && ui.Menu.ControlAt(0) == MenuControl.NewGame && ui.Menu.ControlAt(1) == MenuControl.Justine &&
                      ui.Menu.ControlAt(2) == MenuControl.Options && ui.Menu.ControlAt(3) == MenuControl.ChangeProfile && ui.Menu.ControlAt(4) == MenuControl.Quit,
                      "Title controls are not New Game / Justine / Options / Change profile / Exit");
                Check(!ui.Choose(MenuControl.Resume) && !ui.Choose(MenuControl.ReturnTitle), "Title exposes in-game actions");
                HplGame.SetGlobalVar("title_cancel", "keep");
                // cLuxMainMenu_StartGame: the mode window first (normal chosen), then the question.
                var screen = ui.MenuScreen;
                Check(ui.Choose(MenuControl.NewGame) && screen.ShowingStartGame && !screen.Extras.hardMode && !ui.ConfirmingQuit, "New game did not open the start game window in normal mode");
                Check(ui.Choose(MenuControl.StartHard) && screen.Extras.hardMode && ui.Choose(MenuControl.StartNormal) && !screen.Extras.hardMode, "Mode buttons do not choose");
                ui.Choose(MenuControl.StartHard);
                Check(ui.Choose(MenuControl.StartGameStart) && ui.ConfirmingQuit && ui.PendingConfirmation == MenuControl.ConfirmNewGame && !ui.IsTransitioning, "New game bypassed confirmation");
                Check(ui.Choose(MenuControl.Cancel) && screen.ShowingStartGame && !ui.ConfirmingQuit && screen.Extras.hardMode, "Cancel of the question did not return to the start game window");
                Check(ui.Choose(MenuControl.Cancel) && !screen.ShowingStartGame && ui.Menu.ControlAt(0) == MenuControl.NewGame, "Cancel did not close the start game window");
                Check(HplGame.GetGlobalVar("title_cancel") == "keep" && !ui.IsTransitioning && HplDifficulty.HardMode == hardMode, "Cancel new game changed progression");
                Check(HplDifficulty.Text(ui.Menu.Translate, "HardModeSaveCost").Contains("огнив") && HplDifficulty.Text(ui.Menu.Translate, "HardModeSavePopup").Contains("#"),
                      "Hard mode texts are not the Russian ones");
                Check(ui.Choose(MenuControl.Options) && ui.ShowingOptions, "Options did not open on title screen");
                float sensitivity = ui.Settings.mouseSensitivity; ui.DraftSettings.mouseSensitivity = 4.7f;
                Check(ui.Choose(MenuControl.Cancel) && ui.Settings.mouseSensitivity == sensitivity, "Cancel on title screen applied draft preferences");
                Check(ui.Choose(MenuControl.Quit) && ui.PendingConfirmation == MenuControl.ConfirmQuit, "Exit did not use its own confirmation"); ui.Choose(MenuControl.Cancel);
                ui.Pause(); Check(ui.IsTitleScreen && !map.IsMenuPaused && Time.timeScale == 0.75f, "Title pause action changed world state");
                // With a save: Continue / Start game / Load game / Options / Exit (cLuxMainMenu::CreateTopMenuGui).
                string folder = RTXMnesia.Save.HplSaveHandler.Folder(map.Campaign);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "AutoSave_DanielsRoom_2026_10_17_1_2_3_0.sav"), "{}");
                ui.Choose(MenuControl.Options); ui.Choose(MenuControl.Cancel); // back to the list: the save check runs again
                Check(ui.Menu.ButtonCount == 7 && ui.Menu.ControlAt(0) == MenuControl.Continue && ui.Menu.ControlAt(1) == MenuControl.NewGame &&
                      ui.Menu.ControlAt(2) == MenuControl.LoadGame, "Title with a save is not Continue / New Game / Load Game / Justine / Options / Change profile / Exit");
                Check(ui.Choose(MenuControl.Continue) && ui.PendingConfirmation == MenuControl.ConfirmContinue && !ui.IsTransitioning, "Continue bypassed its confirmation");
                ui.Choose(MenuControl.Cancel);
                Check(ui.Choose(MenuControl.LoadGame) && ui.Menu.ControlAt(0) == MenuControl.LoadSlot && !ui.IsTransitioning, "Load game did not list the save");
                Check(ui.Choose(MenuControl.LoadOk) && !ui.ConfirmingQuit, "Load game confirmed with nothing selected");
                ui.Choose(MenuControl.Cancel);
                Check(ui.Menu.ControlAt(0) == MenuControl.Continue, "Cancel did not return from the load window");
                var scenes = EditorBuildSettings.scenes;
                Check(scenes.Length >= 2 && scenes[0].enabled && scenes[0].path == HplGameUi.MainMenuScene && scenes[1].enabled && scenes[1].path == HplGameUi.FirstLevelScene, "Build scene order does not start at title menu / include the first map");
                var globalScenes = EditorBuildSettings.globalScenes;
                Check(globalScenes.Length >= 2 && globalScenes[0].path == HplGameUi.MainMenuScene && globalScenes[1].path == HplGameUi.FirstLevelScene, "Shared scene list still points at SampleScene");
                Check(File.Exists(HplGameUi.MainMenuScene) && File.Exists(HplGameUi.FirstLevelScene), "Required scene missing");

                // Profiles (cLuxMainMenu_Profile): a fresh title screen without a chosen profile starts with them.
                UnityEngine.Object.DestroyImmediate(ui);
                HplProfiles.RootOverride = profiles;
                HplProfiles.ResetForTests();
                HplMenuScreen.RequireProfileInBatch = true;
                foreach (var key in prefKeys) PlayerPrefs.DeleteKey(key);
                HplKeyBindings.Reload();
                RTXMnesia.Save.HplSaveHandler.FolderOverride = null;
                ui = root.AddComponent<HplGameUi>(); ui.Init(map, true);
                screen = ui.MenuScreen;
                Check(screen.ShowingProfiles && screen.EnteringName && screen.Extras.nameText == "Новый игрок" && screen.Extras.nameSelected,
                      "No profiles: the enter name window with the default name did not open");
                Check(screen.CapturingInput, "Typing a name does not take the keys");
                Check(ui.Choose(MenuControl.NameCreate) && !screen.EnteringName && screen.Extras.profiles.Count == 1 && screen.Extras.selectedProfile == 0 &&
                      HplProfiles.Exists("Новый игрок"), "Create did not make the profile");
                Check(!HplProfiles.ChosenThisRun && HplProfiles.Current == null, "Creating chose the profile");
                Check(ui.Choose(MenuControl.ProfileCreate) && screen.EnteringName && ui.Choose(MenuControl.NameCreate) && ui.ConfirmingQuit &&
                      ui.PendingConfirmation == MenuControl.MessageOk && screen.EnteringName, "An existing name did not show the message");
                Check(ui.Choose(MenuControl.MessageOk) && !ui.ConfirmingQuit && screen.EnteringName, "OK did not return to the name window");
                screen.Extras.nameText = "   ";
                Check(ui.Choose(MenuControl.NameCreate) && ui.PendingConfirmation == MenuControl.MessageOk, "An empty name was accepted");
                ui.Choose(MenuControl.MessageOk);
                screen.Extras.nameText = "  Второй  ";
                Check(ui.Choose(MenuControl.NameCreate) && screen.Extras.profiles.Count == 2 && screen.Extras.profiles[screen.Extras.selectedProfile] == "Второй",
                      "Names are not trimmed / the new profile is not selected");
                Check(ui.Choose(MenuControl.ProfileClose) && ui.ConfirmingQuit && ui.PendingConfirmation == MenuControl.ConfirmQuit && screen.ShowingProfiles,
                      "Closing without a profile did not ask to quit");
                Check(ui.Choose(MenuControl.Cancel) && !ui.ConfirmingQuit && screen.ShowingProfiles, "No did not return to the profiles");
                Check(ui.Choose(MenuControl.ProfileSelect) && !screen.ShowingProfiles && HplProfiles.ChosenThisRun && HplProfiles.Current == "Второй",
                      "Select did not choose the profile");
                Check(ui.Menu.ControlAt(0) == MenuControl.NewGame, "The title list did not come back after choosing");
                Check(RTXMnesia.Save.HplSaveHandler.Folder(map.Campaign) == Path.Combine(profiles, "Второй", "Saves", "main"), "Saves are not in the profile");
                // Per profile options: the second one turns subtitles off, the first keeps them.
                ui.Choose(MenuControl.Options);
                ui.DraftSettings.subtitles = false;
                ui.Choose(MenuControl.Apply);
                Check(File.Exists(Path.Combine(profiles, "Второй", HplUserSettings.ProfileFile)) &&
                      File.ReadAllText(Path.Combine(profiles, "Второй", HplUserSettings.ProfileFile)).Contains("\"subtitles\": false"), "Options were not written to the profile");
                HplKeyBindings.Apply(new[] { ("Lantern", "Keyboard.G", "") });
                Check(File.ReadAllText(Path.Combine(profiles, "Второй", HplKeyBindings.ProfileFile)).Contains("Keyboard.G"), "Keys were not written to the profile");
                Check(ui.Choose(MenuControl.ChangeProfile) && screen.ShowingProfiles && screen.Extras.profiles[screen.Extras.selectedProfile] == "Второй",
                      "Change profile did not open the list on the current profile");
                ui.Choose(MenuControl.ProfileRow); // row 0 selected
                Check(screen.Extras.selectedProfile == 0 && ui.Choose(MenuControl.ProfileRow) && !screen.ShowingProfiles && HplProfiles.Current == "Новый игрок",
                      "A second click on the selected row did not choose it");
                Check(ui.Settings.subtitles && HplKeyBindings.Actions.First(a => a.name == "Lantern").primary == "Keyboard.F",
                      "The first profile did not keep its own options and keys");
                // Delete (cLuxMainMenu_Profile::PressDeleteProfile): the second, then the one in use.
                ui.Choose(MenuControl.ChangeProfile);
                screen.Extras.selectedProfile = 1;
                Check(ui.Choose(MenuControl.ProfileDelete) && ui.PendingConfirmation == MenuControl.ConfirmProfileDelete, "Delete did not ask");
                Check(ui.Choose(MenuControl.ConfirmProfileDelete) && !HplProfiles.Exists("Второй") && screen.Extras.profiles.Count == 1 && screen.Extras.selectedProfile == 0,
                      "Delete did not remove the profile / select the row above");
                ui.Choose(MenuControl.ProfileDelete); ui.Choose(MenuControl.ConfirmProfileDelete);
                Check(!HplProfiles.ChosenThisRun && HplProfiles.Current == null && screen.Extras.profiles.Count == 0 && screen.Extras.selectedProfile == -1,
                      "Deleting the profile in use did not unset it");
                Check(ui.Choose(MenuControl.ProfileDelete) && ui.PendingConfirmation == MenuControl.MessageOk, "Delete without a selection did not show the message");
                ui.Choose(MenuControl.MessageOk);
                string report = $"PASS: {checks} main menu model checks (localization, controls, confirmation, start game window, preferences, live world, continue / load game, build scene order, profiles).";
                Directory.CreateDirectory("Logs/MainMenuCheck"); File.WriteAllText("Logs/MainMenuCheck/Tests.txt", report); Debug.Log(report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root); Time.timeScale = scale; Cursor.lockState = cursorLock; Cursor.visible = cursorVisible;
                RTXMnesia.Save.HplSaveHandler.FolderOverride = null;
                HplProfiles.RootOverride = null;
                HplProfiles.ResetForTests();
                HplMenuScreen.RequireProfileInBatch = false;
                HplDifficulty.HardMode = hardMode;
                for (int i = 0; i < prefKeys.Length; i++)
                    if (savedPrefs[i] != null) PlayerPrefs.SetString(prefKeys[i], savedPrefs[i]); else PlayerPrefs.DeleteKey(prefKeys[i]);
                PlayerPrefs.Save();
                HplKeyBindings.Reload();
                HplGame.Reset();
            }
        }
    }
}
