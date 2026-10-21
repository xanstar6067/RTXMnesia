using RTXMnesia.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// The diffusion profile of lit candle wax in the enhanced lighting (<see cref="HplEnhancedLighting.CandleWaxProfile"/>):
    /// a thin object letting the flame's light through warm. Its hash and transmission fields are set only by the editor,
    /// so the asset (our data, in git) is created here when it is missing.
    /// </summary>
    [InitializeOnLoad]
    static class EnsureCandleWaxProfile
    {
        public const string AssetPath = "Assets/Resources/" + HplEnhancedLighting.CandleWaxProfile + ".asset";

        static EnsureCandleWaxProfile() => EditorApplication.delayCall += Ensure;

        public static void Ensure()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(AssetPath);
            if (existing != null)
            {
                // HDRP gives the profile its hash when the asset is loaded, in memory only: a material refers to
                // the profile by that hash, so it must be on disk.
                if (System.IO.File.ReadAllText(AssetPath).Contains("hash: 0"))
                {
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[RTXMnesia] Saved the hash of " + AssetPath);
                }
                return;
            }
            var profile = ScriptableObject.CreateInstance<DiffusionProfileSettings>();
            AssetDatabase.CreateAsset(profile, AssetPath);
            var so = new SerializedObject(profile);
            // Wax lets red through furthest: a warm glow near the flame that fades down the candle.
            so.FindProperty("profile.scatteringDistance").colorValue = new Color(1f, 0.55f, 0.25f);
            so.FindProperty("profile.transmissionTint").colorValue = new Color(1f, 0.7f, 0.4f);
            so.FindProperty("profile.transmissionMode").enumValueIndex = 1; // DiffusionProfile.TransmissionMode.ThinObject (internal)
            so.FindProperty("profile.thicknessRemap").vector2Value = new Vector2(0f, 5f);
            so.FindProperty("profile.worldScale").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[RTXMnesia] Created " + AssetPath);
            Ensure();
        }

        /// <summary>Batch: -executeMethod RTXMnesia.EditorDev.EnsureCandleWaxProfile.RunBatch (-quit).</summary>
        public static void RunBatch() => Ensure();
    }
}
