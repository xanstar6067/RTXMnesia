using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using RTXMnesia.Dev;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.EditorDev
{
    /// <summary>Regenerates an ignored animation gallery entirely from source/. No Amnesia content enters git.</summary>
    public static class DevRoomBuilder
    {
        public const string MapFile = "maps/dev/dev_room.map";
        public const string ScenePath = LevelConverter.LevelsRoot + "/maps/dev/dev_room.unity";
        const int Columns = 6;
        const float Spacing = 7f;

        [MenuItem("Tools/RTXMnesia/Dev Room/Create or rebuild dev_room")]
        public static void RunBatch()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var specimens = FindSpecimens();
            var document = new XmlDocument();
            var level = Add(document, "Level");
            var data = Add(level, "MapData", ("FogActive", "false"));
            var contents = Add(data, "MapContents");
            var files = Add(contents, "FileIndex_Entities", ("NumOfFiles", specimens.Count.ToString(CultureInfo.InvariantCulture)));
            var entities = Add(contents, "Entities");
            Add(entities, "Area", ("Name", "PlayerStartArea_1"), ("AreaType", "PlayerStart"),
                ("WorldPos", "17.5 0.05 3"), ("Scale", "1 1 1"));
            for (int i = 0; i < specimens.Count; i++)
            {
                var specimen = specimens[i];
                Add(files, "File", ("Id", i.ToString(CultureInfo.InvariantCulture)), ("Path", specimen.sourceFile));
                specimen.position = new Vector3((i % Columns - (Columns - 1) * 0.5f) * Spacing, 0.05f, 8 + i / Columns * Spacing);
                var e = Add(entities, "Entity", ("ID", (i + 1).ToString(CultureInfo.InvariantCulture)),
                    ("Name", specimen.entityName), ("FileIndex", i.ToString(CultureInfo.InvariantCulture)),
                    ("WorldPos", Vec(HplSpace.Position(specimen.position))), ("Scale", "1 1 1"));
                var vars = Add(e, "UserVariables");
                Add(vars, "Var", ("Name", "DisableTriggers"), ("Value", "true"));
                Add(vars, "Var", ("Name", "RandomizeAnimationStart"), ("Value", "false"));
            }
            LevelConverter.ConvertGenerated(document, MapFile, root => Finish(root, specimens));
            AssetDatabase.Refresh();
            Debug.Log($"[dev_room] Created {ScenePath}: {specimens.Count} specimens. F3 opens the animation workbench.");
        }

        public static List<HplDevRoom.Station> FindSpecimens()
        {
            var entries = new List<HplDevRoom.Station>();
            var index = new ResourceIndex(AmnesiaPaths.SourceRoot);
            foreach (string file in Directory.EnumerateFiles(Path.Combine(AmnesiaPaths.SourceRoot, "entities"), "*.ent", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(Path.Combine(AmnesiaPaths.SourceRoot, "models"), "*.ent", SearchOption.AllDirectories))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var doc = new XmlDocument();
                doc.Load(file);
                var root = doc.DocumentElement;
                string type = root.SelectSingleNode("UserDefinedVariables")?.Attributes?["EntityType"]?.Value ?? "";
                bool clips = root.SelectSingleNode("ModelData/Animations/Animation") != null;
                if (!clips) clips = EnemyConverter.HasEmbeddedAnimation(index, file);
                bool rigged = root.SelectNodes("ModelData/Bones/Bone").Count > 1;
                if (!clips && !rigged && !type.StartsWith("Enemy_", StringComparison.Ordinal) && !type.StartsWith("Critter", StringComparison.Ordinal)) continue;
                string category = type.StartsWith("Enemy_", StringComparison.Ordinal) ? "Враги" : type == "NPC" ? "Персонажи"
                    : type.StartsWith("Critter", StringComparison.Ordinal) ? "Мелкие существа" : clips ? "Анимированные объекты" : "Скелеты и физика";
                string relative = AmnesiaPaths.ToSourceRelative(file);
                entries.Add(new HplDevRoom.Station { sourceFile = relative, category = category });
            }
            // Representative joint / scripted mechanisms, in addition to every rig and clip-bearing entity.
            string[] mechanisms = {
                "entities/door/castle/castle.ent",
                "entities/gameplay/valve_iron/valve_iron.ent",
                "entities/special/elevator_machine/elevator_machine_main_lever.ent",
            };
            foreach (string relative in mechanisms)
                if (File.Exists(Path.Combine(AmnesiaPaths.SourceRoot, relative)))
                    entries.Add(new HplDevRoom.Station { sourceFile = relative, category = "Механизмы" });
            // One example of each remaining mechanical type, selected deterministically from the catalogue.
            foreach (string type in new[] { "SwingDoor", "Lever", "MultiSlider", "MoveObject" })
            {
                if (entries.Any(e => HplEntityFile.Load(Path.Combine(AmnesiaPaths.SourceRoot, e.sourceFile)).EntityType == type)) continue;
                string match = Directory.EnumerateFiles(Path.Combine(AmnesiaPaths.SourceRoot, "entities"), "*.ent", SearchOption.AllDirectories)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(f => HplEntityFile.Load(f).EntityType == type);
                if (match != null) entries.Add(new HplDevRoom.Station { sourceFile = AmnesiaPaths.ToSourceRelative(match), category = "Механизмы" });
            }
            entries = entries.OrderBy(e => e.category == "Враги" ? 0 : e.category == "Персонажи" ? 1 : e.category == "Анимированные объекты" ? 2 : 3)
                .ThenBy(e => e.sourceFile, StringComparer.OrdinalIgnoreCase).ToList();
            for (int i = 0; i < entries.Count; i++) entries[i].entityName = $"{i + 1:00}_{Path.GetFileNameWithoutExtension(entries[i].sourceFile)}";
            return entries;
        }

        static void Finish(GameObject root, List<HplDevRoom.Station> specimens)
        {
            var map = root.GetComponent<HplMap>();
            map.runScript = false;
            map.disableAutoSaves = map.disableInsanity = map.disableEnemyAI = true;
            var room = root.AddComponent<HplDevRoom>();
            room.stations = specimens;
            int rows = Mathf.CeilToInt(specimens.Count / (float)Columns);
            float depth = rows * Spacing + 10f, width = Columns * Spacing + 4f;
            string folder = Path.ChangeExtension(ScenePath, null);
            Material Material(string name, Color color)
            {
                string path = folder + "/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find("HDRP/Lit"));
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", 0.15f);
                EditorUtility.SetDirty(material);
                return material;
            }
            var floor = Material("DevFloor", new Color(0.17f, 0.19f, 0.21f));
            var wall = Material("DevWalls", new Color(0.32f, 0.34f, 0.37f));
            var blue = Material("DevStandBlue", new Color(0.08f, 0.19f, 0.29f));
            var gold = Material("DevStandGold", new Color(0.28f, 0.19f, 0.08f));
            var geometry = new GameObject("Test room").transform;
            geometry.SetParent(root.transform, false);
            void Box(string name, Vector3 position, Vector3 size, Material material)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(geometry, false);
                go.transform.position = position;
                go.transform.localScale = size;
                go.GetComponent<Renderer>().sharedMaterial = material;
            }
            Box("Floor", new Vector3(0, -0.2f, depth / 2), new Vector3(width, 0.4f, depth), floor);
            Box("Left wall", new Vector3(-width / 2, 3, depth / 2), new Vector3(0.4f, 6, depth), wall);
            Box("Right wall", new Vector3(width / 2, 3, depth / 2), new Vector3(0.4f, 6, depth), wall);
            Box("Back wall", new Vector3(0, 3, depth), new Vector3(width, 6, 0.4f), wall);
            Box("Front wall", new Vector3(0, 3, 0), new Vector3(width, 6, 0.4f), wall);
            Box("Ceiling", new Vector3(0, 6.2f, depth / 2), new Vector3(width, 0.4f, depth), wall);
            foreach (var station in specimens)
                Box("Stand " + station.entityName, station.position + Vector3.down * 0.035f, new Vector3(4.5f, 0.03f, 4.5f),
                    station.category == "Враги" ? gold : blue);

            var ambient = new GameObject("Inspection ambient").AddComponent<AmnesiaLight>();
            ambient.transform.SetParent(root.transform, false);
            ambient.transform.position = new Vector3(0, 3, depth / 2);
            ambient.box = true;
            ambient.boxSize = new Vector3(width + 2, 12, depth + 2);
            ambient.diffuseColor = new Color(0.55f, 0.55f, 0.55f, 1);
            HplBoxLightPass.Register(ambient);
            var key = new GameObject("Inspection key light").AddComponent<Light>();
            key.transform.SetParent(root.transform, false);
            key.transform.rotation = Quaternion.Euler(55, -25, 0);
            key.type = LightType.Directional;
            key.lightUnit = UnityEngine.Rendering.LightUnit.Lux;
            key.intensity = 100;
            // Inspection lights deliberately illuminate through the enclosure, so the roof never blacks out
            // specimens. Gameplay lighting / shadows are tested on the original maps.
            key.shadows = LightShadows.None;
            key.gameObject.AddComponent<HDAdditionalLightData>();
            var fill = new GameObject("Inspection fill light").AddComponent<Light>();
            fill.transform.SetParent(root.transform, false);
            fill.transform.rotation = Quaternion.Euler(25, 155, 0);
            fill.type = LightType.Directional;
            fill.lightUnit = UnityEngine.Rendering.LightUnit.Lux;
            fill.intensity = 60;
            fill.shadows = LightShadows.None;
            fill.gameObject.AddComponent<HDAdditionalLightData>();
            AssetDatabase.SaveAssets();
        }

        static string Vec(Vector3 v) => FormattableString.Invariant($"{v.x} {v.y} {v.z}");

        static XmlElement Add(XmlNode parent, string name, params (string name, string value)[] attributes)
        {
            var doc = parent as XmlDocument ?? parent.OwnerDocument;
            var element = doc.CreateElement(name);
            foreach (var (key, value) in attributes) element.SetAttribute(key, value);
            parent.AppendChild(element);
            return element;
        }
    }
}
