using System;
using System.Collections.Generic;
using System.IO;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// HUD images of the hurt and damage effects: cLuxPlayerHurt's noise (graphics/hud/hud_hurt_noise0N.dds) and
    /// cLuxPlayerHudEffect::LoadDamageData's splashes (graphics/hud/damage_&lt;name&gt;N.tga while the file exists).
    /// </summary>
    public static class HudEffectsConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplHudEffects.asset";
        const int NoiseCount = 8;

        [MenuItem("Tools/RTXMnesia/Import/HUD effect resources")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(log);
            log.Save("HUD effect resources");
            if (log.Errors > 0) throw new InvalidOperationException("HUD effect import failed; see AmnesiaImport.log");
        }

        public static HplHudEffectAssets Convert(ImportLog log)
        {
            var assets = AssetDatabase.LoadAssetAtPath<HplHudEffectAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplHudEffectAssets>();

            assets.hurtNoise = new Texture2D[NoiseCount];
            for (int i = 0; i < NoiseCount; i++)
                assets.hurtNoise[i] = Gui($"graphics/hud/hud_hurt_noise0{i}.dds", log);
            assets.bloodSplat = Damage("bloodsplat", log);
            assets.claws = Damage("claws", log);
            assets.slash = Damage("slash", log);
            assets.guiShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/HplGui.shader");
            if (assets.guiShader == null) log.Error("Missing Assets/Shaders/HplGui.shader");

            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"HUD effects: {NoiseCount} hurt noise images, damage splashes bloodsplat {assets.bloodSplat.Length}, " +
                     $"claws {assets.claws.Length}, slash {assets.slash.Length}");
            return assets;
        }

        static Texture2D[] Damage(string name, ImportLog log)
        {
            var list = new List<Texture2D>();
            for (int i = 0; File.Exists(Path.Combine(AmnesiaPaths.SourceRoot, $"graphics/hud/damage_{name}{i}.tga")); i++)
            {
                var texture = Gui($"graphics/hud/damage_{name}{i}.tga", log);
                if (texture != null) list.Add(texture);
            }
            return list.ToArray();
        }

        /// <summary>A GUI image (TextureImportRules: clamp; the HUD tiles the noise with repeat at runtime).</summary>
        static Texture2D Gui(string relative, ImportLog log)
        {
            string file = Path.Combine(AmnesiaPaths.SourceRoot, relative);
            if (!File.Exists(file))
            {
                log.Error("Missing HUD effect image: " + relative);
                return null;
            }
            string path = TextureConverter.Convert(file, TextureRole.Gui, null, false, out bool written);
            if (written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) log.Error("HUD effect image was not imported: " + relative);
            return texture;
        }
    }
}
