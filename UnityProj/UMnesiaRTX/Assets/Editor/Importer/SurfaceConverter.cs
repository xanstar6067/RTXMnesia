using System.IO;
using System.Xml;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// materials.cfg → <see cref="HplSurfaces"/> in Assets/AmnesiaImported/Physics/surfaces.asset with one
    /// PhysicsMaterial per surface (sub assets). Existing assets are updated in place, so references stay valid.
    /// Friction and elasticity combine like cPhysicsMaterialNewton::UpdateMaterials: the larger mode of the two
    /// materials wins (Average &lt; Min &lt; Multiply &lt; Max, the same order Unity uses).
    /// </summary>
    public static class SurfaceConverter
    {
        public const string SourceFile = "materials.cfg";
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Physics/surfaces.asset";

        public static HplSurfaces Convert(ImportLog log)
        {
            string file = Path.Combine(AmnesiaPaths.SourceRoot, SourceFile);
            if (!File.Exists(file))
            {
                log.Error($"Physics materials not found: {file}");
                return null;
            }
            var doc = new XmlDocument();
            doc.Load(file);

            var asset = AssetDatabase.LoadAssetAtPath<HplSurfaces>(AssetPath);
            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<HplSurfaces>();
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            var oldMaterials = new System.Collections.Generic.Dictionary<string, PhysicsMaterial>();
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetPath))
                if (o is PhysicsMaterial pm) oldMaterials[pm.name] = pm;

            asset.surfaces.Clear();
            foreach (var e in doc.DocumentElement.Children())
            {
                if (e.Name != "Material") continue;
                string name = e.Str("Name");
                if (name == "") continue;
                var s = new HplSurfaces.Surface
                {
                    name = name,
                    // Physics.cpp LoadSurfaceData defaults.
                    elasticity = e.Float("Elasticity", 0.5f),
                    kineticFriction = e.Float("KineticFriction", 0.3f),
                    staticFriction = e.Float("StaticFriction", 0.3f),
                    priority = e.Int("Priority"),
                    elasticityMode = e.Str("ElasticityMode", "Average"),
                    frictionMode = e.Str("FrictionMode", "Average"),
                    stepType = e.Str("StepType"),
                    scrapeSoundName = e.Str("ScrapeSoundName"),
                    rollSoundName = e.Str("RollSoundName"),
                };
                foreach (var c in e.Children())
                {
                    var effect = new HplSurfaces.Effect
                    {
                        minSpeed = c.Float("MinSpeed"),
                        soundName = c.Str("SoundName"),
                        psName = c.Str("PSName"),
                        psPrio = c.Int("PSPrio", 10),
                    };
                    if (c.Name == "Impact") s.impacts.Add(effect);
                    else if (c.Name == "Hit") s.hits.Add(effect);
                }

                if (!oldMaterials.TryGetValue(name, out var pm))
                {
                    pm = new PhysicsMaterial(name);
                    AssetDatabase.AddObjectToAsset(pm, asset);
                }
                oldMaterials.Remove(name);
                pm.dynamicFriction = s.kineticFriction;
                pm.staticFriction = s.staticFriction;
                pm.bounciness = s.elasticity;
                pm.frictionCombine = HplSurfaces.Combine(s.frictionMode);
                pm.bounceCombine = HplSurfaces.Combine(s.elasticityMode);
                EditorUtility.SetDirty(pm);
                s.physicsMaterial = pm;
                asset.surfaces.Add(s);
            }
            foreach (var stale in oldMaterials.Values) Object.DestroyImmediate(stale, true);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            log.Info($"Physics materials {SourceFile} → {AssetPath} ({asset.surfaces.Count})");
            return asset;
        }
    }
}
