using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTXMnesia.Importer;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    /// <summary>Repair/audit generated maps after old model rebuilds invalidated mesh local IDs.</summary>
    public static class ModelReferenceTests
    {
        static readonly string[] Examples =
        {
            "entities/furniture/table_wood02/table_wood02.dae", "entities/storage/shelf02/shelf02.dae",
            "entities/item/note_letter/note_letter.dae", "entities/desktop/paper01/paper01.dae"
        };

        public static void RepairAllBatch()
        {
            string root = AmnesiaPaths.ImportedAssetsRoot + "/Levels";
            var maps = Directory.GetFiles(AmnesiaPaths.ToAbsolute(root), "*.unity", SearchOption.AllDirectories)
                .Select(file => Path.ChangeExtension(Path.GetRelativePath(AmnesiaPaths.ToAbsolute(root), file), ".map").Replace('\\', '/'))
                .Where(map => File.Exists(Path.Combine(AmnesiaPaths.SourceRoot, map)))
                .OrderBy(map => map.EndsWith("01_old_archives.map") ? "" : map, StringComparer.Ordinal).ToArray();
            if (Environment.GetCommandLineArgs().Contains("-repairArchivesOnly")) maps = maps.Take(1).ToArray();
            int failures = 0, renderers = 0;
            bool bulk = !Environment.GetCommandLineArgs().Contains("-repairArchivesOnly");
            if (bulk)
            {
                var log = new ImportLog();
                EnemyConverter.Convert(new ResourceIndex(AmnesiaPaths.SourceRoot), log);
                log.Save("Shared animations for model reference repair");
                LevelConverter.ReuseAnimationBank = true;
            }
            try
            {
                foreach (string map in maps)
                {
                    Debug.Log($"[ModelReferences] Repairing {map}");
                    string source = Path.Combine(AmnesiaPaths.SourceRoot, map);
                    string scene = map.Contains("main_menu/") ? LevelConverter.ConvertMenuBackground(source) : LevelConverter.Convert(source);
                    EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                    failures += Audit(map, out int count);
                    renderers += count;
                    Debug.Log($"[ModelReferences] Repaired {map}: {count} mesh renderers, total failures {failures}");
                }
                if (bulk && File.Exists(AmnesiaPaths.ToAbsolute(DevRoomBuilder.ScenePath)))
                {
                    DevRoomBuilder.RunBatch();
                    failures += Audit("dev_room", out int count);
                    renderers += count;
                }
                if (bulk) HandsConverter.ConvertBatch();
            }
            finally { LevelConverter.ReuseAnimationBank = false; }
            Debug.Log($"[ModelReferences] {(failures == 0 ? "PASS" : "FAIL")}: {maps.Length} maps, {renderers} mesh renderers, {failures} missing references");
            if (failures > 0) throw new InvalidOperationException("Missing mesh references remain after map repair");
            RunBatch();
        }

        public static void RunBatch()
        {
            var log = new ImportLog();
            var index = new ResourceIndex(AmnesiaPaths.SourceRoot);
            int checks = 0;
            foreach (string file in Examples)
            {
                string source = Path.Combine(AmnesiaPaths.SourceRoot, file);
                var prefab = HplModelBuilder.Build(source, index, log);
                string path = HplModelBuilder.PrefabPathFor(source);
                var before = MeshIds(prefab);
                if (before.Count == 0) throw new InvalidOperationException(file + ": no persisted meshes");
                prefab = HplModelBuilder.Build(source, index, log);
                var after = MeshIds(prefab);
                if (!before.OrderBy(p => p.Key).SequenceEqual(after.OrderBy(p => p.Key)))
                    throw new InvalidOperationException(file + ": GUID/local ID changed on rebuild");
                // A disk reimport must retain sub-assets, not only keep temporary in-memory meshes alive.
                string meshes = Path.ChangeExtension(path, null) + "_meshes.asset";
                AssetDatabase.ImportAsset(meshes, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!before.OrderBy(p => p.Key).SequenceEqual(MeshIds(prefab).OrderBy(p => p.Key)))
                    throw new InvalidOperationException(file + ": mesh references lost after disk reimport");
                checks += before.Count * 2;
            }
            Debug.Log($"[ModelReferences] PASS: {checks} mesh identity/persistence checks, {Examples.Length} models rebuilt twice");
        }

        public static void AuditAllBatch()
        {
            string root = AmnesiaPaths.ImportedAssetsRoot + "/Levels";
            int maps = 0, renderers = 0, missing = 0;
            foreach (string file in Directory.GetFiles(AmnesiaPaths.ToAbsolute(root), "*.unity", SearchOption.AllDirectories).OrderBy(f => f))
            {
                string path = root + "/" + Path.GetRelativePath(AmnesiaPaths.ToAbsolute(root), file).Replace('\\', '/');
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                missing += Audit(path, out int count);
                renderers += count;
                maps++;
                Debug.Log($"[ModelReferences] Disk audit {path}: {count} mesh renderers");
            }
            Debug.Log($"[ModelReferences] {(missing == 0 ? "PASS" : "FAIL")}: disk audit, {maps} scenes, {renderers} mesh renderers, {missing} missing references");
            if (missing > 0) throw new InvalidOperationException("Missing mesh/material references in disk audit");
        }

        public static void VerifyBatch()
        {
            RunBatch();
            DecalMaterialTests.RunBatch();
            AuditAllBatch();
            BuildCheck.RunBatch();
        }

        static Dictionary<string, string> MeshIds(GameObject root)
        {
            var result = new Dictionary<string, string>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || mesh.vertexCount == 0 || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long id))
                    throw new InvalidOperationException(root.name + "/" + filter.name + ": missing/non-persistent mesh");
                result.Add(filter.name, guid + ":" + id);
            }
            return result;
        }

        static int Audit(string name, out int count)
        {
            count = 0;
            int missing = 0;
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                var mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                count++;
                if (mesh != null && mesh.vertexCount > 0 && renderer.sharedMaterials.All(m => m != null)) continue;
                missing++;
                Debug.LogError($"[ModelReferences] {name}: {renderer.name} has missing mesh/material (model {renderer.GetComponentInParent<AmnesiaModel>()?.file})");
            }
            return missing;
        }
    }
}
