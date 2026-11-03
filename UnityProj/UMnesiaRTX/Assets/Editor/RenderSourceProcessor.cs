using System.Collections.Generic;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// <see cref="AmnesiaRenderSources"/> on the copy of every level scene before the build (and Play) combines its
    /// static objects: afterwards a renderer's mesh is "Combined Mesh (root: scene)" and the bug report (F1) could not
    /// name the model. The .dae comes from the mesh asset's path under Assets/AmnesiaImported/Models.
    /// </summary>
    public sealed class RenderSourceProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var level = FindLevel(scene);
            if (level == null) return;
            var renderers = new List<Renderer>();
            var meshes = new List<string>();
            var fileIndices = new List<int>();
            var files = new List<string>();
            var fileIndex = new Dictionary<string, int>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh;
                    if (mesh == null || !filter.TryGetComponent<Renderer>(out var renderer)) continue;
                    string file = ModelFile(AssetDatabase.GetAssetPath(mesh));
                    int index = -1;
                    if (file != null && !fileIndex.TryGetValue(file, out index))
                    {
                        index = files.Count;
                        files.Add(file);
                        fileIndex[file] = index;
                    }
                    renderers.Add(renderer);
                    meshes.Add(mesh.name);
                    fileIndices.Add(index);
                }
            var sources = level.gameObject.AddComponent<AmnesiaRenderSources>();
            sources.renderers = renderers.ToArray();
            sources.meshes = meshes.ToArray();
            sources.fileIndices = fileIndices.ToArray();
            sources.files = files.ToArray();
        }

        static AmnesiaLevel FindLevel(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<AmnesiaLevel>(out var level)) return level;
            return null;
        }

        /// <summary>"Assets/AmnesiaImported/Models/a/b_meshes.asset" (or b.prefab, b.dae) → "a/b.dae"; null outside the models.</summary>
        internal static string ModelFile(string assetPath)
        {
            string root = AmnesiaPaths.ModelsRoot + "/";
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(root)) return null;
            string path = assetPath.Substring(root.Length);
            foreach (var suffix in new[] { "_meshes.asset", ".prefab", ".dae" })
                if (path.EndsWith(suffix)) return path.Substring(0, path.Length - suffix.Length) + ".dae";
            return null;
        }
    }
}
