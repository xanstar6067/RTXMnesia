using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using RTXMnesia.Hpl;
using RTXMnesia.Particles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// The game's particle systems → <see cref="HplParticleBank"/> in Assets/AmnesiaImported/Particles/particles.asset:
    /// every .ps of particles/ parsed like cParticleEmitterData_UserData::LoadFromElement (cString parsing: a vector
    /// with the wrong number of values takes the default, booleans are "true" or false), and the materials of the
    /// emitters as RTXMnesia/HplParticle materials in Particles/Materials/&lt;source path&gt;.mat (Translucent:
    /// diffuse × vertex color, the blend mode, DepthTest). Material frames: MaterialNum &gt; 1 loads name01, name02, ...
    /// </summary>
    public static class ParticleConverter
    {
        public const string ParticlesRoot = AmnesiaPaths.ImportedAssetsRoot + "/Particles";
        public const string BankPath = ParticlesRoot + "/particles.asset";
        const string MaterialsRoot = ParticlesRoot + "/Materials";
        public const string ParticleShader = "RTXMnesia/HplParticle";

        public static HplParticleBank Convert(ImportLog log)
        {
            string folder = Path.Combine(AmnesiaPaths.SourceRoot, "particles");
            if (!Directory.Exists(folder))
            {
                log.Error($"Particle folder not found: {folder}");
                return null;
            }
            var index = new ResourceIndex(AmnesiaPaths.SourceRoot);
            var files = new List<string>(Directory.EnumerateFiles(folder, "*.ps", SearchOption.AllDirectories));
            files.Sort(StringComparer.OrdinalIgnoreCase);

            // Parse first, then convert all materials the emitters use in one go.
            var systems = new List<(HplParticleBank.SystemData data, List<string[]> materialFiles)>();
            var materialFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // .mat → asset path
            foreach (string file in files)
            {
                try
                {
                    var parsed = LoadPs(file, index, log);
                    systems.Add(parsed);
                    foreach (var frames in parsed.materialFiles)
                        foreach (string mat in frames)
                            if (mat != null) materialFiles[mat] = MaterialAssetPath(mat);
                }
                catch (Exception e)
                {
                    log.Warn($"{AmnesiaPaths.ToSourceRelative(file)}: {e.Message}");
                }
            }

            var materials = ConvertMaterials(materialFiles, index, log);

            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(ParticlesRoot));
            var bank = AssetDatabase.LoadAssetAtPath<HplParticleBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<HplParticleBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }
            bank.systems.Clear();
            int emitters = 0;
            foreach (var (data, frames) in systems)
            {
                for (int i = 0; i < data.emitters.Count; i++)
                {
                    var list = new List<Material>();
                    foreach (string mat in frames[i])
                        if (mat != null && materials.TryGetValue(mat, out var m) && m != null) list.Add(m);
                    var e = data.emitters[i];
                    e.materials = list.ToArray();
                    // mfFrameStep / mfMaxFrameTime over the materials that loaded.
                    e.frameStep = list.Count > 0 ? 1f / (e.frameStep / list.Count) : 0f;
                    e.maxFrameTime = list.Count - 0.0001f;
                    emitters++;
                }
                bank.systems.Add(data);
            }
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            log.Info($"Particle bank {BankPath}: {bank.systems.Count} systems, {emitters} emitters, {materials.Count} materials");
            return bank;
        }

        static string MaterialAssetPath(string matAbsolute) =>
            AmnesiaPaths.ToAssetPath(MaterialsRoot, AmnesiaPaths.ToSourceRelative(matAbsolute)) + ".mat";

        // ------------------------------------------------------------------ .ps

        static (HplParticleBank.SystemData data, List<string[]> materialFiles) LoadPs(string file, ResourceIndex index, ImportLog log)
        {
            var doc = new XmlDocument();
            doc.Load(file);
            var root = doc.DocumentElement;
            if (root == null || root.Name != "ParticleSystem") throw new InvalidDataException("no ParticleSystem element");

            string rel = AmnesiaPaths.ToSourceRelative(file);
            var data = new HplParticleBank.SystemData
            {
                name = Path.GetFileNameWithoutExtension(rel).ToLowerInvariant(),
                path = Path.ChangeExtension(rel, null).Replace('\\', '/').ToLowerInvariant(),
            };
            var frames = new List<string[]>();
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is not XmlElement e || e.Name != "ParticleEmitter") continue;
                data.emitters.Add(LoadEmitter(e, file, index, log, out string[] mats));
                frames.Add(mats);
            }
            if (data.emitters.Count == 0) log.Warn($"{rel}: no emitters");
            return (data, frames);
        }

        /// <summary>cParticleEmitterData_UserData::LoadFromElement with its defaults.</summary>
        static HplParticleBank.EmitterData LoadEmitter(XmlElement e, string psFile, ResourceIndex index, ImportLog log, out string[] materials)
        {
            string rel = AmnesiaPaths.ToSourceRelative(psFile);
            var d = new HplParticleBank.EmitterData
            {
                name = Str(e, "Name", ""),
                beam = Str(e, "PEType", "normal").ToLowerInvariant() == "beam",
                maxParticleNum = Int(e, "MaxParticleNum", 1),
                respawn = Bool(e, "Respawn", false),
                particlesPerSecond = Float(e, "ParticlesPerSecond", 1f),
                startTimeOffset = Float(e, "StartTimeOffset", 0f),
                warmUpTime = Float(e, "WarmUpTime", 0f),
                warmUpStepsPerSec = Float(e, "WarmUpStepsPerSec", 60f),
                minPauseLength = Float(e, "MinPauseLength", 0f),
                maxPauseLength = Float(e, "MaxPauseLength", 0f),
                minPauseInterval = Float(e, "MinPauseInterval", 0f),
                maxPauseInterval = Float(e, "MaxPauseInterval", 0f),
                posOffset = Vec3(e, "PosOffset", Vector3.zero),
                angleOffset = Vec3(e, "AngleOffset", Vector3.zero) * Mathf.Deg2Rad,
                subDiv = Vec2l(e, "SubDiv", Vector2Int.one),
                subDivType = Str(e, "SubDivType", "random").ToLowerInvariant() == "animation" ? PESubDivType.Animation : PESubDivType.Random,

                startPosType = StartPosType(e, "StartPosType"),
                minStartPos = Vec3(e, "MinStartPos", Vector3.zero),
                maxStartPos = Vec3(e, "MaxStartPos", Vector3.zero),
                minStartAngles = Vec2(e, "MinStartAngles", Vector2.zero) * Mathf.Deg2Rad,
                maxStartAngles = Vec2(e, "MaxStartAngles", Vector2.zero) * Mathf.Deg2Rad,
                minStartRadius = Float(e, "MinStartRadius", 0f),
                maxStartRadius = Float(e, "MaxStartRadius", 0f),

                startVelType = StartPosType(e, "StartVelType"),
                minStartVel = Vec3(e, "MinStartVel", Vector3.zero),
                maxStartVel = Vec3(e, "MaxStartVel", Vector3.zero),
                minStartVelAngles = Vec2(e, "MinStartVelAngles", Vector2.zero) * Mathf.Deg2Rad,
                maxStartVelAngles = Vec2(e, "MaxStartVelAngles", Vector2.zero) * Mathf.Deg2Rad,
                minStartVelSpeed = Float(e, "MinStartVelSpeed", 0f),
                maxStartVelSpeed = Float(e, "MaxStartVelSpeed", 0f),
                minSpeedMultiply = Float(e, "MinSpeedMultiply", 0f),
                maxSpeedMultiply = Float(e, "MaxSpeedMultiply", 0f),
                minStartAcc = Vec3(e, "MinStartAcc", Vector3.zero),
                maxStartAcc = Vec3(e, "MaxStartAcc", Vector3.zero),
                minVelMaximum = Float(e, "MinVelMaximum", 0f),
                maxVelMaximum = Float(e, "MaxVelMaximum", 0f),
                usesDirection = Bool(e, "UsesDirection", false),
                gravityType = Str(e, "GravityType", "none").ToLowerInvariant() switch
                {
                    "vector" => PEGravityType.Vector,
                    "center" => PEGravityType.Center,
                    _ => PEGravityType.None,
                },
                gravityAcc = Vec3(e, "GravityAcc", Vector3.zero),
                coordSystem = Str(e, "CoordSystem", "world").ToLowerInvariant() == "local" ? PECoordSystem.Local : PECoordSystem.World,
                usePartSpin = Bool(e, "UsePartSpin", false),
                partSpinType = Str(e, "PartSpinType", "constant").ToLowerInvariant() == "movement" ? PEPartSpinType.Movement : PEPartSpinType.Constant,
                minSpinRange = Float(e, "MinSpinRange", 0f),
                maxSpinRange = Float(e, "MaxSpinRange", 0f),
                useRevolution = Bool(e, "UseRevolution", false),
                minRevVel = Vec3(e, "MinRevVel", Vector3.zero),
                maxRevVel = Vec3(e, "MaxRevVel", Vector3.zero),

                minLifeSpan = Float(e, "MinLifeSpan", 0f),
                maxLifeSpan = Float(e, "MaxLifeSpan", 0f),

                drawType = Str(e, "DrawType", "point").ToLowerInvariant() switch
                {
                    "line" => PEDrawType.Line,
                    "axis" => PEDrawType.Axis,
                    _ => PEDrawType.Point,
                },
                minStartSize = Vec2(e, "MinStartSize", Vector2.one),
                maxStartSize = Vec2(e, "MaxStartSize", Vector2.one),
                startRelSize = Float(e, "StartRelSize", 0f),
                middleRelSize = Float(e, "MiddleRelSize", 0f),
                middleRelSizeTime = Float(e, "MiddleRelSizeTime", 0f),
                middleRelSizeLength = Float(e, "MiddleRelSizeLength", 0f),
                endRelSize = Float(e, "EndRelSize", 0f),
                multiplyRGBWithAlpha = Bool(e, "MultiplyRGBWithAlpha", false),

                minStartColor = Col(e, "MinStartColor", Color.white),
                maxStartColor = Col(e, "MaxStartColor", Color.white),
                startRelColor = Col(e, "StartRelColor", Color.white),
                middleRelColor = Col(e, "MiddleRelColor", Color.white),
                middleRelColorTime = Float(e, "MiddleRelColorTime", 0f),
                middleRelColorLength = Float(e, "MiddleRelColorLength", 0f),
                endRelColor = Col(e, "EndRelColor", Color.white),

                collides = Bool(e, "Collides", false),
                minBounceAmount = Float(e, "MinBounceAmount", 0f),
                maxBounceAmount = Float(e, "MaxBounceAmount", 0f),
                minCollisionMax = Int(e, "MinCollisionMax", 0),
                maxCollisionMax = Int(e, "MaxCollisionMax", 0),
                collisionUpdateRate = Int(e, "CollisionUpdateRate", 0),
            };

            // Material frames; the animation length is kept in frameStep until the materials are known.
            int materialNum = Int(e, "MaterialNum", 1);
            d.frameStep = Float(e, "AnimationLength", 1f);
            string material = Str(e, "Material", "");
            var names = new List<string>();
            if (materialNum <= 1) names.Add(Path.ChangeExtension(material, "mat"));
            else
                for (int i = 1; i < materialNum + 1; ++i)
                    names.Add(Path.ChangeExtension(material + (i > 9 ? i.ToString() : "0" + i), "mat"));
            materials = new string[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                materials[i] = index.Find(names[i], psFile);
                if (materials[i] == null) log.Warn($"{rel}: material '{names[i]}' not found");
            }
            if (d.beam) log.Info($"{rel}: beam emitter '{d.name}' (HPL2 does not draw beams either)");
            return d;
        }

        // cString parsing (String.cpp): an absent attribute takes the default.
        static string Str(XmlElement e, string name, string fallback) => e.HasAttribute(name) ? e.GetAttribute(name) : fallback;
        static int Int(XmlElement e, string name, int fallback) => e.HasAttribute(name) ? HplMath.Atoi(e.GetAttribute(name)) : fallback;
        static float Float(XmlElement e, string name, float fallback) => e.HasAttribute(name) ? HplMath.Atof(e.GetAttribute(name)) : fallback;
        static bool Bool(XmlElement e, string name, bool fallback) =>
            e.HasAttribute(name) ? e.GetAttribute(name).ToLowerInvariant() == "true" : fallback;

        static readonly char[] Separators = { ' ', '\n', '\r', '\t', ',' };

        static float[] Floats(XmlElement e, string name)
        {
            if (!e.HasAttribute(name)) return null;
            string[] parts = e.GetAttribute(name).Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var v = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) v[i] = HplMath.Atof(parts[i]);
            return v;
        }

        static Vector2 Vec2(XmlElement e, string name, Vector2 fallback)
        {
            var v = Floats(e, name);
            return v != null && v.Length == 2 ? new Vector2(v[0], v[1]) : fallback;
        }

        static Vector3 Vec3(XmlElement e, string name, Vector3 fallback)
        {
            var v = Floats(e, name);
            return v != null && v.Length == 3 ? new Vector3(v[0], v[1], v[2]) : fallback;
        }

        static Vector2Int Vec2l(XmlElement e, string name, Vector2Int fallback)
        {
            if (!e.HasAttribute(name)) return fallback;
            string[] parts = e.GetAttribute(name).Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 ? new Vector2Int(HplMath.Atoi(parts[0]), HplMath.Atoi(parts[1])) : fallback;
        }

        static Color Col(XmlElement e, string name, Color fallback)
        {
            var v = Floats(e, name);
            return v != null && v.Length == 4 ? new Color(v[0], v[1], v[2], v[3]) : fallback;
        }

        static PEStartPosType StartPosType(XmlElement e, string name) =>
            Str(e, name, "box").ToLowerInvariant() == "sphere" ? PEStartPosType.Sphere : PEStartPosType.Box;

        // ------------------------------------------------------------------ materials

        /// <summary>Translucent .mat → RTXMnesia/HplParticle material (existing assets are updated in place).</summary>
        static Dictionary<string, Material> ConvertMaterials(Dictionary<string, string> files, ResourceIndex index, ImportLog log)
        {
            var result = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            var shader = Shader.Find(ParticleShader);
            if (shader == null)
            {
                log.Error($"Shader '{ParticleShader}' not found");
                return result;
            }

            var sources = new Dictionary<string, (AmnesiaMaterial mat, string texture)>();
            int written = 0;
            foreach (var pair in files)
            {
                string rel = AmnesiaPaths.ToSourceRelative(pair.Key);
                try
                {
                    var mat = AmnesiaMaterial.Load(pair.Key);
                    string texture = null;
                    string diffuse = mat.Texture("Diffuse");
                    string dds = diffuse != null ? index.Find(diffuse, pair.Key) : null;
                    if (diffuse != null && dds == null) log.Warn($"{rel}: Diffuse texture '{diffuse}' not found");
                    if (dds != null)
                    {
                        texture = TextureConverter.Convert(dds, TextureRole.Albedo, null, false, out bool w);
                        if (w) written++;
                    }
                    if (mat.Type != AmnesiaMaterial.Kind.Translucent)
                        log.Info($"{rel}: {mat.TypeName} material on a particle system, drawn as translucent");
                    if (mat.Variables.TryGetValue("Refraction", out string refraction) && refraction.ToLowerInvariant() == "true")
                        log.Info($"{rel}: refraction not ported (drawn without it)");
                    sources[pair.Key] = (mat, texture);
                }
                catch (Exception e)
                {
                    log.Error($"{rel}: {e.Message}");
                }
            }
            if (written > 0) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var pair in sources)
            {
                string assetPath = files[pair.Key];
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(assetPath)));
            }
            AssetDatabase.Refresh();

            foreach (var pair in sources)
            {
                var (src, texturePath) = pair.Value;
                string assetPath = files[pair.Key];
                var m = new Material(shader);
                if (texturePath != null) m.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                SetBlend(m, src.BlendMode);
                bool depthTest = !src.Variables.TryGetValue("DepthTest", out string dt) || dt.ToLowerInvariant() == "true";
                m.SetFloat("_ZTest", (float)(depthTest ? CompareFunction.LessEqual : CompareFunction.Always));
                m.renderQueue = (int)RenderQueue.Transparent;
                result[pair.Key] = SaveInPlace(m, assetPath);
            }
            return result;
        }

        /// <summary>cMaterialManager::GetBlendMode (unknown or missing: Add) and the GL blend function of each mode.</summary>
        public static void SetBlend(Material m, string blendMode)
        {
            (int mode, BlendMode src, BlendMode dst) = (blendMode ?? "Add").ToLowerInvariant() switch
            {
                "mul" => (1, BlendMode.DstColor, BlendMode.Zero),
                "mulx2" => (2, BlendMode.DstColor, BlendMode.Zero),
                "alpha" => (3, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha),
                "premulalpha" => (4, BlendMode.One, BlendMode.OneMinusSrcAlpha),
                _ => (0, BlendMode.One, BlendMode.One),
            };
            m.SetFloat("_HplBlendMode", mode);
            m.SetFloat("_SrcBlend", (float)src);
            m.SetFloat("_DstBlend", (float)dst);
        }

        static Material SaveInPlace(Material material, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, assetPath);
                return material;
            }
            EditorUtility.CopySerialized(material, existing);
            existing.name = Path.GetFileNameWithoutExtension(assetPath);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
