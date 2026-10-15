using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels
{
    /// <summary>Manual port of maps/main/inventory.hps. Shared inventory callbacks, not a level script.</summary>
    public sealed class InventoryScript : HplScript
    {
        public override void OnStart()
        {
            void Pair(string name, string a, string b, string function, bool once = false) => AddCombineCallback(name, a, b, function, once);
            for (int a = 1; a <= 4; a++)
            {
                for (int b = a + 1; b <= 4; b++) Pair($"chem{a}chem{b}", $"Chemical_{a}", $"Chemical_{b}", nameof(CombineChemicals));
                Pair($"chem{a}jar", $"Chemical_{a}", "chemical_container_1", nameof(CombineChemicalsAndJar));
            }
            for (int a = 1; a <= 3; a++)
                for (int b = a + 1; b <= 3; b++) Pair($"drill_{a}_{b}", $"hand_drill_part0{a}", $"hand_drill_part0{b}", nameof(CombineDrill));
            Pair("stonechipperhammer", "stone_chipper_1", "stone_hammer_1", nameof(CombineHammer));
            Pair("coppertubeandneedle", "copper_tube_1", "hollow_needle_1", nameof(CombineCopperTubeAndNeedle));
            Pair("containerandstring", "string_pile_2", "glass_container_1", nameof(CombineContainerAndString));
            for (int a = 1; a <= 6; a++)
            {
                Pair("tarandorbpiece" + a, "OrbPiece_" + a, "bucket_of_tar_1", nameof(CombineOrbPieceAndTar));
                for (int b = a + 1; b <= 6; b++) Pair($"orbandorbpiece{a}{b}", "OrbPiece_" + a, "OrbPiece_" + b, nameof(CombineOrbPieceAndOrbPiece));
            }
            Pair("sawonmeat", "bone_saw_1", "fresh_meat_remains_1", nameof(CombineSawIngr));
            Pair("sawonbone", "bone_saw_1", "fresh_meat_2", nameof(CombineSawIngr));
            Pair("sawonhead", "bone_saw_1", "agrippa_head_1", nameof(CombineSawIngr));
            Pair("sawongland", "bone_saw_1", "poison_gland", nameof(CombineSawIngr));
            Pair("glandonbone", "poison_gland", "fresh_meat_remains_1", nameof(CombineMiscIngr));
            string[] ingredients = { "poison_gland", "fresh_meat_remains_1" };
            for (int i = 0; i < ingredients.Length; i++)
            {
                Pair("blood_n_ingredient" + i, "glass_container_blood", ingredients[i], nameof(CombineBloodAndIngredient));
                for (int j = 1; j <= 3; j++) Pair($"blood_n_ingredient{i}_{j}", "glass_container_mix_notdone_" + j, ingredients[i], nameof(CombineNotdoneMixAndIngredient));
            }
            Pair("test", "hand01", "hand02", nameof(TestFunc), true);
            Pair("makekey", "hand1", "hand2", nameof(GauntletCombineHands), true);
        }

        void Message(string entry, float time = -1f) => SetInventoryMessage("Inventory", entry, time);
        void CombineChemicals(string a, string b) => Message("CombineLevel05Chemicals", 0);
        void CombineChemicalsAndJar(string a, string b) => Message("CombineLevel05Chemicals", 0);
        void CombineOrbPieceAndTar(string a, string b) => Message("CombineLevel22OrbPieceAndTar");
        void CombineOrbPieceAndOrbPiece(string a, string b) => Message("CombineLevel22OrbAndOrb");
        void CombineBloodAndIngredient(string a, string b) => Message("CombineLevel26BloodAndIngredient");
        void CombineNotdoneMixAndIngredient(string a, string b) => Message("CombineLevel26NotdoneMixAndIngredient");
        void CombineMiscIngr(string a, string b) => Message(GetGlobalVarInt("WeyerNoteIsPicked") == 1 ? "CombineLevel2126Ingredients02" : "CombineLevel2126Ingredients");
        void CombineSawIngr(string a, string b)
        {
            // Preserve the script's independent if / if-else (head is overwritten by the bone message).
            if (b == "agrippa_head_1") Message("CombineLevel26SawHead");
            if (b == "poison_gland") Message("CombineLevel26SawGland");
            else Message("CombineLevel26SawBone");
        }
        void CombineDrill(string a, string b)
        {
            if (GetGlobalVarInt("DrillParts") != 3) { Message("CombineLevel12Error"); return; }
            PlayGuiSound("12_make_drill", 1f);
            Player.AddSanity(10f);
            for (int i = 1; i <= 3; i++) RemoveItem("hand_drill_part0" + i);
            GiveItem("handdrill", "Puzzle", "hand_drill", "hand_drill.tga", 0);
        }
        void CombineHammer(string a, string b)
        {
            PlayGuiSound("15_make_hammer", 1f);
            Player.AddSanity(10f);
            Replace(a, b, "stone_hammer_chipper", "stone_hammer_chipper.tga");
        }
        void CombineCopperTubeAndNeedle(string a, string b)
        {
            PlayGuiSound("15_make_hammer", 0.7f);
            Replace(a, b, "copper_tube_and_needle", "copper_tube_and_needle.tga");
        }
        void CombineContainerAndString(string a, string b)
        {
            PlayGuiSound("26_string_on_glass.ogg", 0.8f);
            RemoveItem(a); RemoveItem(b);
            GiveItem("container_and_string", "Puzzle", "container_and_string", "glass_container_string.tga", 0);
        }
        void Replace(string a, string b, string name, string image)
        {
            RemoveItem(a); RemoveItem(b);
            GiveItem(name, "Puzzle", name, image, 0);
        }
        void TestFunc(string a, string b) { RemoveItem(a); SetSwingDoorLocked("door01", false, true); }
        void GauntletCombineHands(string a, string b)
        {
            RemoveItem(a); RemoveItem(b);
            GiveItem("key", "Puzzle", "item_hand", "_temp_hand.tga", 0);
        }
    }
}
