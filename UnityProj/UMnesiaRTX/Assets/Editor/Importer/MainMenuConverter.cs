using System;
using System.Collections.Generic;
using System.IO;
using RTXMnesia.Ui;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    public static class MainMenuConverter
    {
        [MenuItem("Tools/RTXMnesia/Import/Justine menu and build scenes")]
        public static void ConvertJustineBatch()
        {
            LevelConverter.ConvertMenuBackground(Path.Combine(AmnesiaPaths.SourceRoot, HplCampaign.Justine.MenuMap));
            ConfigureBuildScenes();
        }
        [MenuItem("Tools/RTXMnesia/Import/Main menu and build scenes")]
        public static void ConvertBatch()
        {
            string source = Path.Combine(AmnesiaPaths.SourceRoot, "main_menu/main/menu_bg.map");
            string menu = LevelConverter.ConvertMenuBackground(source);
            if (menu != HplGameUi.MainMenuScene) throw new InvalidOperationException("Unexpected menu scene path: " + menu);
            if (!File.Exists(AmnesiaPaths.ToAbsolute(HplGameUi.FirstLevelScene)))
                LevelConverter.Convert(Path.Combine(AmnesiaPaths.SourceRoot, "maps/main/ch01/00_rainy_hall.map"));
            ConfigureBuildScenes();
            Debug.Log("Main menu imported and configured as first build scene; new game loads 00_rainy_hall.");
        }

        // Fresh imports can receive new GUIDs; resolve them each time instead of committing resource metadata.
        // The title screen first, then the first map and every other imported game map (map changes find them by name).
        public static void ConfigureBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            var added = new HashSet<string>();
            void Add(string path, bool enabled)
            {
                if (added.Add(path)) scenes.Add(new EditorBuildSettingsScene(path, enabled));
            }
            if (File.Exists(AmnesiaPaths.ToAbsolute(HplGameUi.MainMenuScene))) Add(HplGameUi.MainMenuScene, true);
            if (File.Exists(AmnesiaPaths.ToAbsolute(HplGameUi.FirstLevelScene))) Add(HplGameUi.FirstLevelScene, true);
            if (File.Exists(AmnesiaPaths.ToAbsolute(HplCampaign.Justine.MenuScene))) Add(HplCampaign.Justine.MenuScene, true);
            string maps = AmnesiaPaths.ToAbsolute(LevelConverter.LevelsRoot + "/maps");
            if (Directory.Exists(maps))
            {
                var files = Directory.GetFiles(maps, "*.unity", SearchOption.AllDirectories);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string file in files) Add(Path.GetRelativePath(AmnesiaPaths.ProjectRoot, file).Replace('\\', '/'), true);
            }
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!File.Exists(AmnesiaPaths.ToAbsolute(scene.path))) continue;
                Add(scene.path, scene.path == "Assets/Scenes/SampleScene.unity" ? false : scene.enabled);
            }
            // Unity 6 can route `scenes` through a local build profile. Persist the shared project list too,
            // so a fresh checkout / another build profile starts at the same title screen.
            EditorBuildSettings.globalScenes = scenes.ToArray();
            EditorBuildSettings.scenes = scenes.ToArray();
            var profile = UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();
            if (profile != null && profile.overrideGlobalScenes)
            {
                profile.scenes = scenes.ToArray();
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
