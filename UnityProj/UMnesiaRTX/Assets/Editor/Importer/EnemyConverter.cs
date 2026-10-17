using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RTXMnesia.Enemies;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Enemy data into the ignored Resources/HplEnemies.asset (<see cref="HplEnemyAssets"/>):
    /// - the animations of every entities/enemy/**/*.ent (cEntityLoader_Object: the .dae_anim files are whole Collada
    ///   files of the rig, copied as .dae next to the model and imported as legacy clips named as the .ent's Animation);
    /// - the AI node connection files of the maps (maps/**/*.nodes, cAINodeContainer::LoadFromFile reads them when they
    ///   are newer than the map, which is so for every shipped one).
    /// Maps need no re-import; the level import runs this too.
    /// </summary>
    public static class EnemyConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + HplEnemyAssets.ResourceName + ".asset";

        [MenuItem("Tools/RTXMnesia/Import/Enemies (animations, AI nodes)")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(new ResourceIndex(AmnesiaPaths.SourceRoot), log);
            log.Save("Enemies");
            if (log.Errors > 0) throw new InvalidOperationException("Enemy import failed; see AmnesiaImport.log");
        }

        public static HplEnemyAssets Convert(ResourceIndex index, ImportLog log)
        {
            var assets = AssetDatabase.LoadAssetAtPath<HplEnemyAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplEnemyAssets>();

            assets.enemies.Clear();
            string enemyRoot = Path.Combine(AmnesiaPaths.SourceRoot, "entities/enemy");
            foreach (string ent in Directory.EnumerateFiles(enemyRoot, "*.ent", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var enemy = ConvertEnemy(index, ent, log);
                    if (enemy != null) assets.enemies.Add(enemy);
                }
                catch (Exception ex) { log.Error($"{AmnesiaPaths.ToSourceRelative(ent)}: {ex.Message}"); }
            }

            // The other entities with animations (NPC, Object, critters): the same data for the props' mesh animations
            // (cEntityLoader_Object plays the first one).
            string entitiesRoot = Path.Combine(AmnesiaPaths.SourceRoot, "entities");
            foreach (string ent in Directory.EnumerateFiles(entitiesRoot, "*.ent", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                if (Path.GetFullPath(ent).StartsWith(Path.GetFullPath(enemyRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                string text = File.ReadAllText(ent);
                if (!text.Contains("<Animations") || !text.Contains("<Animation ")) continue;
                try
                {
                    var animated = ConvertEnemy(index, ent, log, missingIsWarning: true);
                    if (animated != null && animated.animations.Count > 0) assets.enemies.Add(animated);
                }
                catch (Exception ex) { log.Error($"{AmnesiaPaths.ToSourceRelative(ent)}: {ex.Message}"); }
            }

            assets.nodeFiles.Clear();
            string mapsRoot = Path.Combine(AmnesiaPaths.SourceRoot, "maps");
            foreach (string file in Directory.EnumerateFiles(mapsRoot, "*.nodes", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                assets.nodeFiles.Add(new HplEnemyAssets.NodeFile { name = Path.GetFileNameWithoutExtension(file), xml = File.ReadAllText(file) });

            // cMapHandlerSoundCallback: the Sound elements of sounds/EnemySounds.dat.
            assets.hearableSounds.Clear();
            string dat = Path.Combine(AmnesiaPaths.SourceRoot, "sounds/EnemySounds.dat");
            if (File.Exists(dat))
            {
                var datDoc = new XmlDocument();
                datDoc.Load(dat);
                foreach (XmlElement e in datDoc.DocumentElement.ChildNodes.OfType<XmlElement>()) assets.hearableSounds.Add(e.GetAttribute("name"));
            }
            else log.Error("Missing sounds/EnemySounds.dat");

            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Enemies: {string.Join(", ", assets.enemies.Select(e => $"{e.name} ({e.animations.Count} animations)"))}; " +
                     $"node files {string.Join(", ", assets.nodeFiles.Select(n => n.name))}");
            return assets;
        }

        /// <summary>
        /// HPL2 names the bones by the Collada node id (cColladaNode::msId); Unity's import names the objects by the node
        /// name, which may repeat (agrippa: ids CenterNeck_CenterHead and CenterNeck_CenterHead_CenterHead are both named
        /// CenterHead). The path of names from the visual scene keeps the id → object link.
        /// </summary>
        static void ReadNodePaths(string dae, HplEnemyAssets.Enemy enemy)
        {
            enemy.nodeIds.Clear();
            enemy.nodePaths.Clear();
            var doc = new XmlDocument();
            doc.Load(dae);
            var scene = doc.GetElementsByTagName("visual_scene").OfType<XmlElement>().FirstOrDefault();
            if (scene == null) return;
            void Walk(XmlElement node, string parentPath)
            {
                string name = node.GetAttribute("name");
                if (name == "") name = node.GetAttribute("id");
                string path = parentPath == "" ? name : parentPath + "/" + name;
                string id = node.GetAttribute("id");
                if (id != "" && !enemy.nodeIds.Contains(id))
                {
                    enemy.nodeIds.Add(id);
                    enemy.nodePaths.Add(path);
                }
                foreach (XmlElement child in node.ChildNodes.OfType<XmlElement>().Where(c => c.Name == "node")) Walk(child, path);
            }
            foreach (XmlElement top in scene.ChildNodes.OfType<XmlElement>().Where(c => c.Name == "node")) Walk(top, "");
        }

        /// <param name="missingIsWarning">Other entities: a missing animation file only warns (the critter spider's are
        /// not in the game data; HPL2 skips them with a warning too).</param>
        static HplEnemyAssets.Enemy ConvertEnemy(ResourceIndex index, string ent, ImportLog log, bool missingIsWarning = false)
        {
            var doc = new XmlDocument();
            doc.Load(ent);
            var modelData = doc.DocumentElement.Child("ModelData");
            var animations = modelData?.Child("Animations");
            var enemy = new HplEnemyAssets.Enemy { name = Path.GetFileNameWithoutExtension(ent) };
            if (animations == null) return enemy;

            string mesh = modelData.Child("Mesh")?.Str("Filename") ?? "";
            string dae = index.Find(mesh, ent);
            if (dae == null)
            {
                log.Error($"{AmnesiaPaths.ToSourceRelative(ent)}: model '{mesh}' not found");
                return enemy;
            }
            if (!ModelConverter.IsConverted(dae)) ModelConverter.ConvertModels(new[] { dae }, "Enemy model");
            ReadNodePaths(dae, enemy);
            string animFolder = Path.GetDirectoryName(ModelConverter.AssetPathFor(dae)).Replace('\\', '/') + "/animations";
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(animFolder));

            var list = new List<(XmlElement element, string asset)>();
            foreach (XmlElement a in animations.ChildNodes.OfType<XmlElement>())
            {
                // cEntityLoader_Object: a file name without folder is next to the .ent, otherwise searched by name.
                string file = a.Str("File");
                string source = Path.GetDirectoryName(file).Length <= 1
                    ? Path.Combine(Path.GetDirectoryName(ent), file)
                    : index.Find(file, ent);
                if (source == null || !File.Exists(source))
                {
                    string message = $"{AmnesiaPaths.ToSourceRelative(ent)}: animation file missing: {file}";
                    if (missingIsWarning) log.Warn(message); else log.Error(message);
                    continue;
                }
                string asset = animFolder + "/" + Path.GetFileNameWithoutExtension(source) + ".dae";
                string target = AmnesiaPaths.ToAbsolute(asset);
                ColladaAnimFix.CopyFixed(source, target);
                list.Add((a, asset));
            }
            AssetDatabase.Refresh();
            foreach (var (_, asset) in list.Distinct()) AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);

            foreach (var (a, asset) in list)
            {
                string name = a.Str("Name");
                var source = AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>()
                                          .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                if (source == null)
                {
                    log.Error($"{asset}: Unity imported no animation clip");
                    continue;
                }
                string clipPath = animFolder + "/" + name + ".anim";
                var clip = Object.Instantiate(source);
                clip.name = name;
                clip.legacy = true;
                AssetDatabase.DeleteAsset(clipPath);
                AssetDatabase.CreateAsset(clip, clipPath);

                var anim = new HplEnemyAssets.Animation
                {
                    name = name,
                    clip = clip,
                    speed = a.Float("Speed", 1f),
                    specialEventTime = a.Float("SpecialEventTime", 0f),
                };
                foreach (XmlElement e in a.ChildNodes.OfType<XmlElement>())
                    anim.events.Add(new HplEnemyAssets.AnimationEvent { time = e.Float("Time"), type = e.Str("Type"), value = e.Str("Value") });
                enemy.animations.Add(anim);
            }
            AssetDatabase.SaveAssets();
            log.Info($"Enemy {enemy.name}: {string.Join(", ", enemy.animations.Select(x => $"{x.name} {x.clip.length:0.00}s"))}");
            return enemy;
        }
    }
}
