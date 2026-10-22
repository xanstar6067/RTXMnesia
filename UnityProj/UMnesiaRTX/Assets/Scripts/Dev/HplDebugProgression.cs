using System;
using System.Collections.Generic;
using RTXMnesia.ScriptApi;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Dev
{
    /// <summary>Entry equipment, not the original scripts' debug branches (those also solve local puzzles).</summary>
    public static class HplDebugProgression
    {
        public readonly struct Item
        {
            public readonly string Name, File;
            public Item(string name, string file) { Name = name; File = file; }
        }

        public sealed class Stage
        {
            public string ScenePath, Label, Variant = "Первый вход", StartArea = "";
            public readonly List<Item> Items = new();
            public readonly Dictionary<string, string> Globals = new(StringComparer.Ordinal);
            public string MapName => HplMapChange.FileToMapName(ScenePath);
        }

        static void Add(Stage stage, string name, string file) => stage.Items.Add(new Item(name, file + ".ent"));
        static void Needle(Stage s) => Add(s, "hollow_needle_1", "hollow_needle");
        static void Drill(Stage s) => Add(s, "handdrill", "hand_drill");
        // This combination has no .ent; its inventory definition is created by InventoryScript.CombineHammer.
        static void Hammer(Stage s) => Add(s, "stone_hammer_chipper", "stone_hammer_chipper");
        static void Jar(Stage s) => Add(s, "glass_container_1", "glass_container");
        static void Orbs(Stage s, int first, int last)
        {
            string[] files = { "crimson", "indigo", "turquoise", "scarlet", "emerald", "obsidian" };
            for (int i = first; i <= last; i++) Add(s, "OrbPiece_" + i, "orbpiece_" + files[i - 1]);
        }

        /// <summary>All enabled map scenes, including Justine and technical maps; no source files in a player.</summary>
        public static List<Stage> AvailableStages()
        {
            var paths = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.Contains("/Levels/maps/")) paths.Add(path);
            }
            paths.Sort(StringComparer.Ordinal);
            var result = new List<Stage>();
            foreach (string path in paths)
            {
                result.Add(Create(path));
                string map = HplMapChange.FileToMapName(path);
                if (map == "02_entrance_hall")
                {
                    result.Add(Create(path, "Ключ из архивов"));
                    result.Add(Create(path, "Кислота из лаборатории"));
                }
                if (map == "04_wine_cellar_lab") result.Add(Create(path, "С реагентами из винного погреба"));
                if (map == "09_back_hall") result.Add(Create(path, "Ключ из комнаты Даниэля"));
                if (map == "16_cistern_entrance") result.Add(Create(path, "После пульта и цистерны"));
                if (map == "21_torture_nave") result.Add(Create(path, "С осколками и ингредиентами"));
                if (map == "22_torture_chancel") result.Add(Create(path, "С шестью осколками"));
            }
            return result;
        }

        public static Stage Create(string path, string variant = "Первый вход")
        {
            var s = new Stage { ScenePath = path, Variant = variant };
            string map = s.MapName;
            s.Label = (path.Contains("/ptest/") ? "Justine / " : path.Contains("/dev/") ? "Тест / " : "TDD / ") + map;
            if (path.Contains("/ptest/") || path.Contains("/dev/")) return s;

            // Durable equipment carried between chapters. Consumed keys / local puzzle rewards are excluded.
            if (string.CompareOrdinal(map, "08_") >= 0 && string.CompareOrdinal(map, "25_") < 0) Needle(s);
            if (string.CompareOrdinal(map, "13_") >= 0 && string.CompareOrdinal(map, "25_") < 0) Drill(s);
            if (string.CompareOrdinal(map, "15_prison_north") >= 0 && map != "15_prison_south" && string.CompareOrdinal(map, "27_") < 0) Hammer(s);
            if (string.CompareOrdinal(map, "16_") >= 0 && string.CompareOrdinal(map, "25_") < 0) Jar(s);

            switch (map)
            {
                case "02_entrance_hall":
                    if (variant == "Ключ из архивов")
                    {
                        Add(s, "key_study_1", "key_study"); s.Globals["key_study_1"] = "key_study_1";
                    }
                    if (variant == "Кислота из лаборатории")
                    {
                        Add(s, "chemical_container_2", "chemical_container_full");
                        s.Globals["chemical_container_2"] = "chemical_container_2"; s.Globals["BeenToLab"] = "1";
                    }
                    break;
                case "04_wine_cellar_lab":
                    if (variant != "Первый вход")
                    {
                        // The empty vessel belongs to this map and must still be picked up here.
                        Add(s, "Chemical_1", "flask01_aqua_regia"); Add(s, "Chemical_2", "flask01_calamine");
                        Add(s, "Chemical_3", "flask01_cuprite"); Add(s, "Chemical_4", "flask01_orpiment");
                        s.Globals["ChemJars"] = "4"; s.Globals["key_study_1"] = "key_study_1";
                    }
                    break;
                case "09_back_hall":
                    if (variant != "Первый вход") Add(s, "key_jar_1_item", "key_study");
                    break;
                case "12_storage": Add(s, "empty_container", "chemical_container"); break;
                case "13_machine_room":
                    for (int i = 1; i <= 3; i++) Add(s, "guiding_rod0" + i, "guiding_rod0" + i);
                    s.Globals["PickedMachineInstructions"] = "1";
                    break;
                case "14_elevator": s.Globals["ElevatorMachineRunning"] = "1"; break;
                case "16_cistern_entrance":
                    if (variant != "Первый вход")
                    {
                        Add(s, "crank_wood", "crank_wood_no_mount_item");
                        s.Globals["ControlRoomEastSideWorking"] = "1"; s.Globals["WaterHasBeenDrained"] = "1";
                    }
                    break;
                case "18_cistern": Add(s, "crank_wood", "crank_wood_no_mount_item"); break;
                case "19_morgue": Add(s, "crank_wood", "crank_wood_no_mount_item"); s.Globals["WaterHasBeenDrained"] = "1"; break;
                case "20_sewer": s.Globals["WaterHasBeenDrained"] = "1"; s.Globals["PlayerHasAntidote"] = "1"; break;
                case "21_torture_nave":
                    if (variant != "Первый вход")
                    {
                        Orbs(s, 1, 6); Add(s, "poison_gland", "poison_gland");
                        Add(s, "fresh_meat_remains_1", "fresh_meat_remains");
                        s.Items.RemoveAll(i => i.Name == "glass_container_1");
                        Add(s, "glass_container_blood", "glass_container_blood");
                        s.Globals["ChoirOrbCount"] = "3"; s.Globals["TranseptOrbCount"] = "3";
                        s.Globals["IngredientCount"] = "3"; s.Globals["PlayerHasVisitedLevel22"] = "1";
                    }
                    break;
                case "22_torture_chancel":
                    if (variant != "Первый вход")
                    {
                        Orbs(s, 1, 6); s.Globals["ChoirOrbCount"] = "3"; s.Globals["TranseptOrbCount"] = "3";
                    }
                    break;
                case "24_torture_choir_west": case "24_torture_choir_east":
                    Orbs(s, 1, 3); Add(s, "string_pile_2", "string_pile");
                    s.Globals["TranseptOrbCount"] = "3";
                    break;
                case "25_cell_tunnels":
                    Orbs(s, 1, 6); Jar(s);
                    s.Globals["ChoirOrbCount"] = "3"; s.Globals["TranseptOrbCount"] = "3";
                    break;
                case "26_torture_nave_redux": case "26_zimmerman_vision":
                    // Keep the six pieces from the previous chapter for the final mechanism.
                    Orbs(s, 1, 6); Jar(s); s.Globals["AgrippaActivatedIn21"] = "1";
                    s.Globals["ChoirOrbCount"] = "3"; s.Globals["TranseptOrbCount"] = "3";
                    break;
                case "27_torture_chancel_redux":
                    Orbs(s, 1, 6); Add(s, "agrippa_head_1", "agrippa_head");
                    s.Globals["ChoirOrbCount"] = "3"; s.Globals["TranseptOrbCount"] = "3";
                    break;
                case "28_inner_sanctum": case "29_orb_chamber":
                    Add(s, "agrippa_head_1", "agrippa_head");
                    s.Globals["OrbsAddedCount"] = "6"; s.Globals["TarOnPedestal"] = "1";
                    break;
            }
            return s;
        }
    }
}
