using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Soft shadows (PCSS) of the enhanced lighting are HDRP's "High" punctual shadow filtering, a global shader keyword
    /// that <see cref="RTXMnesia.World.HplEnhancedShadows"/> switches in game. A build keeps only the shadow variants of the
    /// HDRP assets it includes, and the project's asset filters at "Medium" (the classic look). So a copy of the asset
    /// with "High" filtering, labelled HDRP:IncludeInBuild (the global settings include assets by that label), brings the
    /// PCSS variants into the build. It is never assigned: everything else equals the project's asset, so it adds no
    /// other variants. Kept in sync on editor load and before each build.
    /// </summary>
    [InitializeOnLoad]
    public sealed class EnsurePcssVariants : IPreprocessBuildWithReport
    {
        const string MainPath = "Assets/Settings/HDRP_RenderPipeline.asset";
        const string VariantsPath = "Assets/Settings/HDRP_PcssVariants.asset";
        const string Label = "HDRP:IncludeInBuild";
        const string FilteringPath = "m_RenderPipelineSettings.hdShadowInitParams.punctualShadowFilteringQuality";

        static EnsurePcssVariants() => EditorApplication.delayCall += () => Ensure();

        // Before HDRP collects the assets whose variants it keeps.
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!Ensure()) throw new BuildFailedException("[RTXMnesia] The PCSS variants asset could not be made (see the log).");
        }

        /// <summary>Creates or updates the copy; false if the project's asset is missing.</summary>
        public static bool Ensure()
        {
            var main = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(MainPath);
            if (main == null) { Debug.LogError("[RTXMnesia] " + MainPath + " not found: no PCSS variants in builds"); return false; }
            var variants = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(VariantsPath);
            bool created = variants == null;
            if (created)
            {
                variants = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                AssetDatabase.CreateAsset(variants, VariantsPath);
            }

            var wanted = Object.Instantiate(main);
            wanted.name = variants.name;
            var serialized = new SerializedObject(wanted);
            serialized.FindProperty(FilteringPath).intValue = (int)HDShadowFilteringQuality.High;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            bool changed = created || EditorJsonUtility.ToJson(wanted) != EditorJsonUtility.ToJson(variants);
            if (changed)
            {
                EditorUtility.CopySerialized(wanted, variants);
                EditorUtility.SetDirty(variants);
                Debug.Log("[RTXMnesia] PCSS variants asset updated: " + VariantsPath);
            }
            Object.DestroyImmediate(wanted);

            var labels = AssetDatabase.GetLabels(variants);
            if (System.Array.IndexOf(labels, Label) < 0)
            {
                var list = new System.Collections.Generic.List<string>(labels) { Label };
                AssetDatabase.SetLabels(variants, list.ToArray());
                changed = true;
            }
            if (changed) AssetDatabase.SaveAssetIfDirty(variants);
            if (GraphicsSettings.defaultRenderPipeline == variants || QualitySettings.renderPipeline == variants)
                Debug.LogError("[RTXMnesia] " + VariantsPath + " must not be assigned: it only carries the PCSS shader variants");
            return true;
        }

        [MenuItem("Tools/RTXMnesia/Update PCSS variants asset")]
        static void Menu() => Ensure();

        /// <summary>Batch: -executeMethod RTXMnesia.EditorDev.EnsurePcssVariants.RunBatch.</summary>
        public static void RunBatch() => EditorApplication.Exit(Ensure() ? 0 : 1);
    }
}
