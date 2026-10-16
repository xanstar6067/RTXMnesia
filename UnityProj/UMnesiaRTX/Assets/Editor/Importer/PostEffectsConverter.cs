using System;
using System.IO;
using RTXMnesia.Effects;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>Post effect textures (cLuxPostEffect_Insanity): the insanity amplitude and zoom maps from shaders/.</summary>
    public static class PostEffectsConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplPostEffects.asset";
        const int AmpMapCount = 3;

        [MenuItem("Tools/RTXMnesia/Import/Post effect resources")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(log);
            log.Save("Post effect resources");
            if (log.Errors > 0) throw new InvalidOperationException("Post effect import failed; see AmnesiaImport.log");
        }

        public static HplPostEffectAssets Convert(ImportLog log)
        {
            var assets = AssetDatabase.LoadAssetAtPath<HplPostEffectAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplPostEffectAssets>();
            assets.insanityAmpMaps = new Texture2D[AmpMapCount];
            for (int i = 0; i < AmpMapCount; i++)
                assets.insanityAmpMaps[i] = Lookup($"shaders/posteffect_insanity_ampmap{i}.jpg", log);
            assets.insanityZoomMap = Lookup("shaders/posteffect_insanity_zoom.jpg", log);
            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Post effects: {AmpMapCount} insanity amplitude maps, zoom map {(assets.insanityZoomMap != null ? "ok" : "missing")}");
            return assets;
        }

        static Texture2D Lookup(string relative, ImportLog log)
        {
            string file = Path.Combine(AmnesiaPaths.SourceRoot, relative);
            if (!File.Exists(file))
            {
                log.Error("Missing post effect texture: " + relative);
                return null;
            }
            string path = TextureConverter.Convert(file, TextureRole.Lookup, null, false, out bool written);
            if (written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) log.Error("Post effect texture was not imported: " + relative);
            return texture;
        }
    }
}
