using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Shaders the runtime finds by name (Shader.Find) get into a player only when an included asset references them or
    /// they are "Always Included". Nothing references the box light and effect renderer shaders (their materials are made
    /// at runtime), so a build had no box lights, outlines or item flashes. Keeps every such shader in the list, on
    /// editor load and before each build.
    /// </summary>
    [InitializeOnLoad]
    public sealed class EnsureRuntimeShaders : IPreprocessBuildWithReport
    {
        static readonly string[] Names =
        {
            "Hidden/RTXMnesia/HplBoxLight",
            "Hidden/RTXMnesia/HplEffectRenderer",
            "Hidden/RTXMnesia/HplFogArea",
            "Hidden/RTXMnesia/HplScreenEffects",
            "Hidden/RTXMnesia/HplGui",
        };

        static EnsureRuntimeShaders() => EditorApplication.delayCall += () => Ensure();

        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!Ensure()) throw new BuildFailedException("[RTXMnesia] A shader found by name at runtime is missing (see the log).");
        }

        /// <summary>Adds the missing shaders to Always Included Shaders; false if one cannot be found.</summary>
        public static bool Ensure()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset"));
            var list = settings.FindProperty("m_AlwaysIncludedShaders");
            var present = new HashSet<Object>();
            for (int i = 0; i < list.arraySize; i++) present.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);
            bool ok = true;
            foreach (string name in Names)
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogError("[RTXMnesia] Shader " + name + " not found: it cannot be included in builds"); ok = false; continue; }
                if (present.Contains(shader)) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                Debug.Log("[RTXMnesia] Always Included Shaders: added " + name);
            }
            if (settings.ApplyModifiedPropertiesWithoutUndo()) AssetDatabase.SaveAssets();
            return ok;
        }

        [MenuItem("Tools/RTXMnesia/Include runtime shaders in builds")]
        static void Menu() => Ensure();

        /// <summary>Batch: -executeMethod RTXMnesia.EditorDev.EnsureRuntimeShaders.RunBatch.</summary>
        public static void RunBatch() => EditorApplication.Exit(Ensure() ? 0 : 1);
    }
}
