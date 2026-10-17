using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Imports HPL2 .dae models into Assets/AmnesiaImported/Models (same relative path), after converting the
    /// materials they use:
    /// - static meshes (no skin): built by <see cref="HplModelBuilder"/> as the engine loads them → &lt;path&gt;.prefab;
    /// - skinned meshes: the .dae is copied and Unity's Collada import reads it (<see cref="ModelImportRules"/>).
    /// The .msh files next to them are HPL2's binary cache of the same meshes and are not needed.
    /// </summary>
    public static class ModelConverter
    {
        static readonly Dictionary<string, bool> Skinned = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Skinned and animated models go through Unity's import to preserve bones and animation bindings.</summary>
        public static bool UsesUnityImport(string daeAbsolute)
        {
            if (!Skinned.TryGetValue(daeAbsolute, out bool skinned))
            {
                var info = ColladaInfo.Load(daeAbsolute);
                Skinned[daeAbsolute] = skinned = info.HasSkin || info.HasAnimation;
            }
            return skinned;
        }

        static string ImportedDaePath(string daeAbsolute) =>
            AmnesiaPaths.ModelsRoot + "/" + AmnesiaPaths.ToSourceRelative(daeAbsolute);

        public static string AssetPathFor(string daeAbsolute) =>
            UsesUnityImport(daeAbsolute) ? ImportedDaePath(daeAbsolute) : HplModelBuilder.PrefabPathFor(daeAbsolute);

        /// <summary>The model exists and was made from the current .dae with the current rules.</summary>
        public static bool IsConverted(string daeAbsolute) =>
            UsesUnityImport(daeAbsolute)
                ? File.Exists(AmnesiaPaths.ToAbsolute(ImportedDaePath(daeAbsolute)))
                : HplModelBuilder.IsUpToDate(daeAbsolute);

        public static void ConvertModels(IReadOnlyList<string> daeFiles, string title)
        {
            var log = new ImportLog();
            ModelImportRules.ResetIndex();
            var index = new ResourceIndex(AmnesiaPaths.SourceRoot);

            // 1. Materials first: model import remaps to them, so they must exist already.
            var matFiles = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string dae in daeFiles)
            {
                string rel = AmnesiaPaths.ToSourceRelative(dae);
                try
                {
                    foreach (string texture in ColladaInfo.Load(dae).MaterialTextures.Values.Distinct())
                    {
                        string mat = index.Find(Path.ChangeExtension(texture, ".mat"), dae);
                        if (mat != null) matFiles.Add(mat);
                        // Often a placeholder on collider nodes (never rendered); real misses are reported after import.
                        else log.Info($"{rel}: no .mat for texture '{texture}'");
                    }
                }
                catch (Exception e) { log.Error($"{rel}: {e.Message}"); }
            }
            MaterialConverter.ConvertMaterials(matFiles.ToList(), false, title + " — materials");

            // 2a. Static meshes: built like the engine loads them.
            int built = 0;
            foreach (string dae in daeFiles.Where(d => !UsesUnityImport(d)))
            {
                try
                {
                    if (HplModelBuilder.Build(dae, index, log) != null) built++;
                    else log.Error($"{AmnesiaPaths.ToSourceRelative(dae)}: prefab not saved");
                }
                catch (Exception e) { log.Error($"{AmnesiaPaths.ToSourceRelative(dae)}: {e.Message}"); }
            }
            log.Info($"Built static models: {built}");

            // 2b. Skinned meshes: copy and (re)import, so remaps to newly created materials are applied.
            var assetPaths = new List<string>();
            foreach (string dae in daeFiles.Where(UsesUnityImport))
            {
                string assetPath = ImportedDaePath(dae);
                string target = AmnesiaPaths.ToAbsolute(assetPath);
                if (!File.Exists(target) || File.GetLastWriteTimeUtc(dae) > File.GetLastWriteTimeUtc(target))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(dae, target, true);
                }
                assetPaths.Add(assetPath);
            }
            AssetDatabase.Refresh();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in assetPaths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // 3. Report what came out.
            foreach (string path in assetPaths)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) { log.Error($"{path}: Unity failed to import the model"); continue; }
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || !AssetDatabase.GetAssetPath(material).StartsWith(AmnesiaPaths.MaterialsRoot))
                            log.Warn($"{path}: node '{renderer.name}' uses unconverted material '{(material != null ? material.name : "null")}'");
            }

            log.Info($"Models: {assetPaths.Count}, materials: {matFiles.Count}");
            foreach (string path in Environment.GetCommandLineArgs().SkipWhile(a => a != "-describe").Skip(1).Take(1)
                         .SelectMany(a => a.Split(',')))
                log.Info(DescribeHierarchy(AssetDatabase.LoadAssetAtPath<GameObject>(AmnesiaPaths.ModelsRoot + "/" + path)));
            log.Save(title);
        }

        /// <summary>Text dump of a model: nodes, components, materials. For checking import results in the log.</summary>
        public static string DescribeHierarchy(GameObject root)
        {
            if (root == null) return "(null)";
            var sb = new StringBuilder($"Hierarchy of {root.name}:\n");
            void Walk(Transform t, int depth)
            {
                sb.Append(' ', depth * 2).Append(t.name)
                  .Append($" pos={t.localPosition} rot={t.localEulerAngles} scale={t.localScale}");
                foreach (var c in t.GetComponents<Component>())
                    if (c is not Transform) sb.Append(" [").Append(c.GetType().Name).Append(']');
                var r = t.GetComponent<Renderer>();
                if (r != null) sb.Append(" materials: ").Append(string.Join(", ", r.sharedMaterials.Select(m => m != null ? m.name : "null")));
                var f = t.GetComponent<MeshFilter>();
                if (f != null && f.sharedMesh != null) sb.Append($" mesh: {f.sharedMesh.name} ({f.sharedMesh.vertexCount} v, bounds {f.sharedMesh.bounds})");
                sb.Append('\n');
                foreach (Transform child in t) Walk(child, depth + 1);
            }
            Walk(root.transform, 0);
            return sb.ToString();
        }
    }
}
