using System.Collections.Generic;
using System.IO;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// The diffusion profile of lit candle wax in the enhanced lighting (<see cref="HplEnhancedLighting.CandleWaxProfile"/>):
    /// a thin object letting the flame's light through warm. Its hash and transmission fields are set only by the editor,
    /// so the asset (our data, in git) is created here when it is missing.
    ///
    /// Also the wax materials of the lamps (<see cref="HplEnhancedLighting.WaxMaterialFolder"/> in the ignored
    /// Assets/AmnesiaImported/Resources): a material made at runtime has no shader variant in a player (HDRP/Lit's
    /// transmission is a shader_feature), so the glow of candle wax worked only in the editor. One per material of the
    /// lamp models (no lamp .ent overrides its submesh materials) and per enhanced texture set; made on editor load when
    /// missing and before each build.
    /// </summary>
    [InitializeOnLoad]
    public sealed class EnsureCandleWaxProfile : IPreprocessBuildWithReport
    {
        public const string AssetPath = "Assets/Resources/" + HplEnhancedLighting.CandleWaxProfile + ".asset";
        public const string LampModels = "Assets/AmnesiaImported/Models/entities/lamp";
        public const string WaxFolder = "Assets/AmnesiaImported/Resources/" + HplEnhancedLighting.WaxMaterialFolder;

        static EnsureCandleWaxProfile() => EditorApplication.delayCall += () =>
        {
            Ensure();
            if (AssetDatabase.IsValidFolder(LampModels) && !AssetDatabase.IsValidFolder(WaxFolder.TrimEnd('/'))) BuildWaxMaterials();
        };

        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            Ensure();
            BuildWaxMaterials();
        }

        public static void Ensure()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(AssetPath);
            if (existing != null)
            {
                // HDRP gives the profile its hash when the asset is loaded, in memory only: a material refers to
                // the profile by that hash, so it must be on disk.
                if (File.ReadAllText(AssetPath).Contains("hash: 0"))
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

        /// <summary>Creates or updates (keeping the GUIDs) the wax material of every lamp model material.</summary>
        public static void BuildWaxMaterials()
        {
            if (!AssetDatabase.IsValidFolder(LampModels)) return;
            var profile = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(AssetPath);
            if (profile == null) { Debug.LogError("[RTXMnesia] No " + AssetPath + ": no wax materials"); return; }
            var enhancedOf = new Dictionary<Material, Material>();
            var catalog = Resources.Load<HplTextureSet>(HplTextureSet.ResourceName);
            if (catalog != null)
                foreach (var entry in catalog.entries)
                    if (entry.classic != null && entry.enhanced != null) enhancedOf[entry.classic] = entry.enhanced;
            var sources = new HashSet<Material>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { LampModels }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                foreach (var r in prefab.GetComponentsInChildren<MeshRenderer>(true))
                    foreach (var m in r.sharedMaterials)
                        if (m != null && m.shader != null && m.shader.name == "HDRP/Lit") sources.Add(m);
            }
            Directory.CreateDirectory(WaxFolder);
            var names = new HashSet<string>();
            int written = 0;
            foreach (var classic in sources)
            {
                if (!names.Add(classic.name)) Debug.LogWarning("[RTXMnesia] Two lamp materials named " + classic.name + ": one wax material");
                written += Write(classic, HplEnhancedLighting.WaxMaterialName(classic, false), profile);
                if (enhancedOf.TryGetValue(classic, out var enhanced))
                    written += Write(enhanced, HplEnhancedLighting.WaxMaterialName(classic, true), profile);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[RTXMnesia] Candle wax materials: {sources.Count} lamp materials, {written} written to {WaxFolder}");
        }

        static int Write(Material source, string name, DiffusionProfileSettings profile)
        {
            var wax = HplEnhancedLighting.MakeWaxMaterial(source);
            wax.name = name;
            string path = WaxFolder + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(wax, path);
                existing = wax;
            }
            else
            {
                EditorUtility.CopySerialized(wax, existing);
                existing.name = name;
                Object.DestroyImmediate(wax);
            }
            // On an asset the profile is also kept as a reference (the build takes the profile with the material).
            HDMaterial.SetDiffusionProfile(existing, profile);
            EditorUtility.SetDirty(existing);
            return 1;
        }

        [MenuItem("Tools/RTXMnesia/Build candle wax materials")]
        static void Menu() => BuildWaxMaterials();

        /// <summary>Batch: -batchmode -nographics -quit -executeMethod RTXMnesia.EditorDev.EnsureCandleWaxProfile.RunBatch.</summary>
        public static void RunBatch()
        {
            Ensure();
            BuildWaxMaterials();
        }
    }
}
