using System;
using System.Collections.Generic;
using System.IO;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    public static class MainMenuConverter
    {
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
        public static void ConfigureBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene> {
                new(HplGameUi.MainMenuScene, true), new(HplGameUi.FirstLevelScene, true),
            };
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == HplGameUi.MainMenuScene || scene.path == HplGameUi.FirstLevelScene) continue;
                scenes.Add(new EditorBuildSettingsScene(scene.path, scene.path == "Assets/Scenes/SampleScene.unity" ? false : scene.enabled));
            }
            // Unity 6 can route `scenes` through a local build profile. Persist the shared project list too,
            // so a fresh checkout / another build profile starts at the same title screen.
            EditorBuildSettings.globalScenes = scenes.ToArray();
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
