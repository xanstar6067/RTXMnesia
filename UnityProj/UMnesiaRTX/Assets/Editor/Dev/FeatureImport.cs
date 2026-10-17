using System;
using System.Collections.Generic;
using System.IO;
using RTXMnesia.Importer;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Re-imports only what the 6.5 additions need without re-importing maps: the water materials (HplWater.shader,
    /// cube maps), the sounds (commentary streams), the pre menu and the commentary data.
    /// -batchmode -nographics -quit -executeMethod RTXMnesia.EditorDev.FeatureImport.RunBatch
    /// </summary>
    public static class FeatureImport
    {
        public static void RunBatch()
        {
            if (Environment.GetCommandLineArgs() is var args && Array.IndexOf(args, "-onlyMenus") >= 0)
            {
                PreMenuConverter.ConvertBatch();
                CommentaryConverter.ConvertBatch();
                return;
            }
            var water = new List<string>();
            foreach (string file in Directory.EnumerateFiles(AmnesiaPaths.SourceRoot, "*.mat", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "custom_stories")) continue;
                string text = File.ReadAllText(file);
                if (text.IndexOf("Type=\"water\"", StringComparison.OrdinalIgnoreCase) >= 0) water.Add(file);
            }
            MaterialConverter.ConvertMaterials(water, false, $"Water materials ({water.Count})");
            ImportMenu.ConvertSoundsBatch();
            PreMenuConverter.ConvertBatch();
            CommentaryConverter.ConvertBatch();
        }
    }
}
