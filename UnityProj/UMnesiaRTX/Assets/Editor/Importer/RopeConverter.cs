using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RTXMnesia.World;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Rope resources for cLuxAreaRopeLoader / cLuxRope::AfterWorldLoad: the RopeFile settings (misc/*.rope) and the
    /// rope materials that the Rope areas of the maps name, by their reference text. The materials are converted
    /// like any .mat. The asset keeps the entries of maps imported earlier; the menu / batch scans every map.
    /// </summary>
    public static class RopeConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplRopes.asset";

        [MenuItem("Tools/RTXMnesia/Import/Rope resources")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            var maps = Directory.GetFiles(Path.Combine(AmnesiaPaths.SourceRoot, "maps"), "*.map", SearchOption.AllDirectories);
            Convert(new ResourceIndex(AmnesiaPaths.SourceRoot), log, maps);
            log.Save("Rope resources");
            if (log.Errors > 0) throw new InvalidOperationException("Rope import failed; see AmnesiaImport.log");
        }

        /// <summary>The rope areas of the given maps (absolute paths) → their files and materials, added to the asset.</summary>
        public static HplRopeAssets Convert(ResourceIndex index, ImportLog log, IEnumerable<string> mapFiles)
        {
            var assets = AssetDatabase.LoadAssetAtPath<HplRopeAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplRopeAssets>();

            var materialRefs = new List<(string reference, string near)>();
            int ropes = 0;
            foreach (string mapFile in mapFiles)
            {
                foreach (var vars in RopeAreas(mapFile, log))
                {
                    ropes++;
                    string ropeFile = HplVariable.Find(vars, "RopeFile") ?? "";
                    string material = HplVariable.Find(vars, "Material") ?? "";
                    if (ropeFile != "")
                    {
                        var attributes = LoadRopeFile(index, ropeFile, mapFile, log, out string ropePath);
                        assets.files.RemoveAll(f => f.reference == ropeFile);
                        if (attributes != null)
                        {
                            assets.files.Add(new HplRopeAssets.RopeFile { reference = ropeFile, attributes = attributes });
                            // cLuxAreaRopeLoader: a loaded file replaces the area's variables.
                            material = HplVariable.Find(attributes, "Material") ?? "";
                            if (material != "") materialRefs.Add((material, ropePath));
                            continue;
                        }
                    }
                    if (material != "") materialRefs.Add((material, mapFile));
                }
            }

            // The materials: converted when missing, then stored by reference.
            var resolved = new List<(string reference, string file)>();
            foreach (var (reference, near) in materialRefs.Distinct())
            {
                string file = index.Find(reference, near);
                if (file == null)
                {
                    log.Warn($"Rope material '{reference}' not found");
                    continue;
                }
                resolved.Add((reference, file));
            }
            var missing = resolved.Select(r => r.file).Distinct()
                .Where(f => !File.Exists(AmnesiaPaths.ToAbsolute(MaterialConverter.AssetPathFor(f)))).ToList();
            if (missing.Count > 0) MaterialConverter.ConvertMaterials(missing, false, "Rope materials");
            foreach (var (reference, file) in resolved)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialConverter.AssetPathFor(file));
                if (material == null)
                {
                    log.Error($"Rope material '{reference}' was not converted");
                    continue;
                }
                assets.materials.RemoveAll(m => m.reference == reference);
                assets.materials.Add(new HplRopeAssets.RopeMaterial { reference = reference, material = material });
            }

            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Ropes: {ropes} rope areas, {assets.files.Count} settings files, {assets.materials.Count} materials");
            return assets;
        }

        /// <summary>The variables of every &lt;Area AreaType="Rope"&gt; of a map file (streamed: maps are large).</summary>
        static IEnumerable<List<HplVariable>> RopeAreas(string mapFile, ImportLog log)
        {
            var result = new List<List<HplVariable>>();
            try
            {
                using var reader = XmlReader.Create(mapFile, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element || reader.Name != "Area" || reader.GetAttribute("AreaType") != "Rope") continue;
                    var vars = new List<HplVariable>();
                    using (var sub = reader.ReadSubtree())
                    {
                        while (sub.Read())
                            if (sub.NodeType == XmlNodeType.Element && sub.Name == "Var")
                                vars.Add(new HplVariable(sub.GetAttribute("Name") ?? "", sub.GetAttribute("Value") ?? ""));
                    }
                    result.Add(vars);
                }
            }
            catch (Exception e)
            {
                log.Error($"{AmnesiaPaths.ToSourceRelative(mapFile)}: rope areas could not be read ({e.Message})");
            }
            return result;
        }

        static List<HplVariable> LoadRopeFile(ResourceIndex index, string reference, string near, ImportLog log, out string path)
        {
            path = index.Find(reference, near);
            if (path == null)
            {
                log.Warn($"Rope file '{reference}' not found (the rope uses its area's variables, as HPL2)");
                return null;
            }
            try
            {
                var doc = new XmlDocument();
                doc.Load(path);
                var attributes = new List<HplVariable>();
                foreach (XmlAttribute a in doc.DocumentElement.Attributes) attributes.Add(new HplVariable(a.Name, a.Value));
                return attributes;
            }
            catch (Exception e)
            {
                log.Warn($"Rope file '{reference}' could not be read ({e.Message})");
                return null;
            }
        }
    }
}
