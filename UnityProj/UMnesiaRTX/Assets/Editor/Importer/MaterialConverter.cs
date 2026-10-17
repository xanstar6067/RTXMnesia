using System;
using System.Collections.Generic;
using System.IO;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Converts HPL2 .mat files into HDRP materials under Assets/AmnesiaImported/Materials (same relative path).
    ///
    /// Mapping:
    /// - SolidDiffuse → HDRP/Lit, Specular Color workflow (Amnesia specular is a grey intensity + power):
    ///   Diffuse → base color, NMap → normal, Specular → specular color map (intensity) + mask map (smoothness),
    ///   Height → pixel displacement (POM), Alpha → alpha clipping, Illumination → emission.
    /// - Translucent Add → HDRP/Unlit additive; Alpha / PremulAlpha → HDRP/Lit transparent.
    ///   Mul / MulX2 (no HDRP blend mode) → Assets/Shaders/HplMultiply.shader, also for decals.
    /// - Decal → HDRP/Decal (mesh decals), Add → HDRP/Unlit additive.
    /// - Water → Assets/Shaders/HplWater.shader (water_surface_frag.glsl): refraction, waves, cube map or world
    ///   reflection (HplWaterReflection), fog.
    /// - CubeMap of other materials and CubeMapAlpha are ignored: HDRP reflection probes / ray-traced reflections replace them.
    /// Existing materials are updated in place so prefabs and scenes keep their references.
    /// </summary>
    public static class MaterialConverter
    {
        // Tuning of the Blinn-Phong → PBR conversion. Change, then re-run the material import (textures stay).
        const float SpecularIntensityToF0 = 0.08f;  // F0 = spec map intensity × this (dielectrics are ~0.04).
        const float NoSpecularF0 = 0.02f;           // Materials without a specular map had no highlights at all.
        const float NoSpecularSmoothness = 0.2f;
        const float HeightScaleToPomCm = 100f;      // HeightMapScale (UV units) → HDRP POM amplitude in cm.
        const float AlphaCutoff = 0.5f;
        // HPL2 writes emission to the screen as is: scaled to the level exposure (HDRP Lit emission is exposed).
        static float EmissiveNits => HplLighting.UnitRadiance;

        // Values of HDRP's internal SurfaceType / BlendingMode / DisplacementMode enums.
        const float HdrpTransparent = 1f;
        const float HdrpPixelDisplacement = 2f;
        enum HdrpBlend { Alpha = 0, Additive = 1, Premultiply = 4 }

        static readonly int BaseColorMap = Shader.PropertyToID("_BaseColorMap");
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        struct TextureJob
        {
            public string Dds;
            public TextureRole Role;
            public string AlphaDds;
        }

        class Plan
        {
            public AmnesiaMaterial Source;
            public string AssetPath;
            public readonly Dictionary<TextureRole, TextureJob> Jobs = new();
        }

        public static string AssetPathFor(string matAbsolute) =>
            AmnesiaPaths.ToAssetPath(AmnesiaPaths.MaterialsRoot, AmnesiaPaths.ToSourceRelative(matAbsolute)) + ".mat";

        /// <summary>Converts the given .mat files (absolute paths) and all textures they use.</summary>
        public static void ConvertMaterials(IReadOnlyList<string> matFiles, bool forceTextures, string title)
        {
            var log = new ImportLog();
            var index = new ResourceIndex(AmnesiaPaths.SourceRoot);
            var plans = new List<Plan>();

            foreach (string matFile in matFiles)
            {
                try { plans.Add(MakePlan(matFile, index, log)); }
                catch (Exception e) { log.Error($"{AmnesiaPaths.ToSourceRelative(matFile)}: {e.Message}"); }
            }

            var textures = ConvertTextures(plans, forceTextures, log);
            BuildMaterials(plans, textures, log);

            log.Info($"Materials: {plans.Count}, textures: {textures.Count}");
            log.Save(title);
        }

        static Plan MakePlan(string matFile, ResourceIndex index, ImportLog log)
        {
            var mat = AmnesiaMaterial.Load(matFile);
            string rel = AmnesiaPaths.ToSourceRelative(matFile);
            var plan = new Plan { Source = mat, AssetPath = AssetPathFor(matFile) };

            string Resolve(string unit)
            {
                string reference = mat.Texture(unit);
                if (reference == null) return null;
                string found = index.Find(reference, matFile);
                if (found == null) log.Warn($"{rel}: {unit} texture '{reference}' not found");
                return found;
            }

            string diffuse = Resolve("Diffuse");
            string alpha = Resolve("Alpha");
            if (diffuse != null) plan.Jobs[TextureRole.Albedo] = new TextureJob { Dds = diffuse, Role = TextureRole.Albedo, AlphaDds = alpha };
            Add(plan, Resolve("NMap"), TextureRole.Normal);
            string spec = Resolve("Specular");
            Add(plan, spec, TextureRole.Specular);
            Add(plan, spec, TextureRole.Mask);
            Add(plan, Resolve("Height"), TextureRole.Height);
            Add(plan, Resolve("Illumination"), TextureRole.Emissive);

            if (mat.Type == AmnesiaMaterial.Kind.Unknown)
                log.Warn($"{rel}: unknown material type '{mat.TypeName}', converted as SolidDiffuse");
            if (mat.Type == AmnesiaMaterial.Kind.Water) Add(plan, Resolve("CubeMap"), TextureRole.CubeMap);
            else if (mat.Texture("CubeMap") != null)
                log.Info($"{rel}: CubeMap ignored (HDRP reflections replace it)");
            if (mat.Texture("DissolveAlpha") != null)
                log.Info($"{rel}: DissolveAlpha ignored (TODO when dissolve effects are ported)");
            return plan;
        }

        static void Add(Plan plan, string dds, TextureRole role)
        {
            if (dds != null) plan.Jobs[role] = new TextureJob { Dds = dds, Role = role };
        }

        /// <summary>Writes PNGs for all jobs, imports them, returns output asset path per job key.</summary>
        static Dictionary<string, string> ConvertTextures(List<Plan> plans, bool force, ImportLog log)
        {
            var results = new Dictionary<string, string>();
            var unique = new Dictionary<string, TextureJob>();
            foreach (var plan in plans)
                foreach (var job in plan.Jobs.Values)
                    unique[Key(job)] = job;

            int done = 0, written = 0;
            try
            {
                foreach (var pair in unique)
                {
                    var job = pair.Value;
                    if (EditorUtility.DisplayCancelableProgressBar("Amnesia: textures",
                            $"{AmnesiaPaths.ToSourceRelative(job.Dds)} ({job.Role})", (float)done++ / unique.Count))
                        throw new OperationCanceledException("Texture conversion cancelled");
                    try
                    {
                        results[pair.Key] = TextureConverter.Convert(job.Dds, job.Role, job.AlphaDds, force, out bool w);
                        if (w) written++;
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        log.Error($"{AmnesiaPaths.ToSourceRelative(job.Dds)} ({job.Role}): {e.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            log.Info($"Textures: {unique.Count} needed, {written} (re)written");
            if (written > 0)
            {
                EditorUtility.DisplayProgressBar("Amnesia: textures", $"Unity is importing {written} textures...", 1f);
                try { AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); }
                finally { EditorUtility.ClearProgressBar(); }
            }
            return results;
        }

        static string Key(TextureJob job) => job.Dds + "|" + job.Role + "|" + job.AlphaDds;

        static void BuildMaterials(List<Plan> plans, Dictionary<string, string> textures, ImportLog log)
        {
            Texture2D Tex(Plan plan, TextureRole role)
            {
                if (!plan.Jobs.TryGetValue(role, out var job) || !textures.TryGetValue(Key(job), out string path)) return null;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) log.Error($"Converted texture missing in AssetDatabase: {path}");
                return tex;
            }

            // Folders must be known to the AssetDatabase before CreateAsset inside StartAssetEditing.
            foreach (var plan in plans)
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(plan.AssetPath)));
            AssetDatabase.Refresh();

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < plans.Count; i++)
                {
                    var plan = plans[i];
                    EditorUtility.DisplayProgressBar("Amnesia: materials", plan.AssetPath, (float)i / plans.Count);
                    try
                    {
                        Cubemap Cube()
                        {
                            if (!plan.Jobs.TryGetValue(TextureRole.CubeMap, out var job) || !textures.TryGetValue(Key(job), out string path)) return null;
                            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                            if (cube == null) log.Error($"Converted cube map missing in AssetDatabase: {path}");
                            return cube;
                        }
                        var material = plan.Source.Type == AmnesiaMaterial.Kind.Water
                            ? CreateWater(plan.Source, role => Tex(plan, role), Cube())
                            : CreateMaterial(plan.Source, role => Tex(plan, role), log);
                        SaveInPlace(material, plan.AssetPath);
                    }
                    catch (Exception e)
                    {
                        log.Error($"{plan.AssetPath}: {e.Message}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();
        }

        static Material CreateMaterial(AmnesiaMaterial src, Func<TextureRole, Texture2D> tex, ImportLog log)
        {
            string rel = AmnesiaPaths.ToSourceRelative(src.SourcePath);
            string blend = (src.BlendMode ?? "").ToLowerInvariant();

            switch (src.Type)
            {
                case AmnesiaMaterial.Kind.Decal when blend is "mul" or "mulx2":
                    return CreateMultiply(tex(TextureRole.Albedo), blend == "mulx2", true);

                case AmnesiaMaterial.Kind.Decal when blend == "add":
                    log.Info($"{rel}: additive decal drawn as unlit additive (HPL2 adds to the albedo)");
                    return CreateUnlitTransparent(tex(TextureRole.Albedo), HdrpBlend.Additive);

                case AmnesiaMaterial.Kind.Decal:
                    return CreateDecal(tex);

                case AmnesiaMaterial.Kind.Translucent when blend == "add":
                    return CreateUnlitTransparent(tex(TextureRole.Albedo), HdrpBlend.Additive);

                case AmnesiaMaterial.Kind.Translucent when blend is "mul" or "mulx2":
                    return CreateMultiply(tex(TextureRole.Albedo), blend == "mulx2", false);

                case AmnesiaMaterial.Kind.Translucent:
                {
                    var m = CreateLit(src, tex);
                    m.SetFloat("_SurfaceType", HdrpTransparent);
                    m.SetFloat("_BlendMode", (float)(blend == "premulalpha" ? HdrpBlend.Premultiply : HdrpBlend.Alpha));
                    HDMaterial.ValidateMaterial(m);
                    return m;
                }

                default:
                    return CreateLit(src, tex);
            }
        }

        /// <summary>
        /// cMaterialType_Water (MaterialType_Water.cpp): the variables as HPL2 sets them on the program —
        /// t = time × WaveSpeed, amplitude × 0.04, frequency × 10. The reflection is the cube map if the material has
        /// one, else the world reflection (a mirror camera, HplWaterReflection); HasReflection false: the fake shading.
        /// </summary>
        public static Material CreateWater(AmnesiaMaterial src, Func<TextureRole, Texture2D> tex, Cubemap cube)
        {
            var m = new Material(FindShader("RTXMnesia/HplWater"));
            var albedo = tex(TextureRole.Albedo);
            if (albedo != null) m.SetTexture(BaseColorMap, albedo);
            var normal = tex(TextureRole.Normal);
            if (normal != null) m.SetTexture("_NormalMap", normal);
            bool hasReflection = src.Bool("HasReflection", true);
            float mode = !hasReflection ? 0f : cube != null ? 1f : 2f;
            if (cube != null) m.SetTexture("_CubeMap", cube);
            m.SetFloat("_ReflectionMode", mode);
            m.SetFloat("_RefractionScale", src.Float("RefractionScale", 0.1f));
            m.SetFloat("_FresnelBias", src.Float("FrenselBias", 0.2f));
            m.SetFloat("_FresnelPow", src.Float("FrenselPow", 8f));
            m.SetFloat("_WaveSpeed", src.Float("WaveSpeed", 1f));
            m.SetFloat("_WaveAmplitude", src.Float("WaveAmplitude", 1f) * 0.04f);
            m.SetFloat("_WaveFreq", src.Float("WaveFreq", 1f) * 10f);
            float fadeStart = src.Float("ReflectionFadeStart", 0f), fadeEnd = src.Float("ReflectionFadeEnd", 0f);
            m.SetVector("_ReflectionFade", new Vector4(fadeStart, fadeEnd - fadeStart, fadeEnd > 0f ? 1f : 0f, fadeEnd));
            m.SetFloat("_OcclusionCullReflection", src.Bool("OcclusionCullWorldReflection", true) ? 1f : 0f);
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        static Material CreateLit(AmnesiaMaterial src, Func<TextureRole, Texture2D> tex)
        {
            var m = new Material(FindShader("HDRP/Lit"));
            m.SetFloat("_MaterialID", (float)MaterialId.LitSpecular);

            var albedo = tex(TextureRole.Albedo);
            if (albedo != null) m.SetTexture(BaseColorMap, albedo);
            m.SetColor(BaseColor, Color.white);

            var normal = tex(TextureRole.Normal);
            if (normal != null)
            {
                m.SetTexture("_NormalMap", normal);
                m.SetFloat("_NormalScale", 1f);
            }

            var specular = tex(TextureRole.Specular);
            var mask = tex(TextureRole.Mask);
            if (specular != null && mask != null)
            {
                m.SetTexture("_SpecularColorMap", specular);
                m.SetColor("_SpecularColor", Color.white * SpecularIntensityToF0);
                m.SetTexture("_MaskMap", mask);
                m.SetFloat("_SmoothnessRemapMin", 0f);
                m.SetFloat("_SmoothnessRemapMax", 1f);
            }
            else
            {
                m.SetColor("_SpecularColor", Color.white * NoSpecularF0);
                m.SetFloat("_Smoothness", NoSpecularSmoothness);
            }

            var height = tex(TextureRole.Height);
            if (height != null)
            {
                float amplitudeCm = src.Float("HeightMapScale", 0.05f) * HeightScaleToPomCm;
                m.SetTexture("_HeightMap", height);
                m.SetFloat("_DisplacementMode", HdrpPixelDisplacement);
                m.SetFloat("_HeightPoMAmplitude", amplitudeCm);
                // What the HDRP inspector derives for pixel displacement: surface = top of the height map.
                m.SetFloat("_HeightAmplitude", amplitudeCm * 0.01f);
                m.SetFloat("_HeightCenter", 1f);
            }

            var emissive = tex(TextureRole.Emissive);
            if (emissive != null)
            {
                m.SetTexture("_EmissiveColorMap", emissive);
                HDMaterial.SetUseEmissiveIntensity(m, true);
                m.SetColor("_EmissiveColorLDR", Color.white);
                HDMaterial.SetEmissiveIntensity(m, EmissiveNits, EmissiveIntensityUnit.Nits);
            }

            // Amnesia alpha testing: an Alpha texture unit (or UseAlpha) rejects pixels by the diffuse alpha.
            if (tex(TextureRole.Albedo) != null && (src.Texture("Alpha") != null || (src.UseAlpha && src.Type == AmnesiaMaterial.Kind.Solid)))
            {
                HDMaterial.SetAlphaCutoff(m, AlphaCutoff);
                HDMaterial.SetAlphaClipping(m, true);
            }

            HDMaterial.ValidateMaterial(m);
            return m;
        }

        static Material CreateUnlitTransparent(Texture2D albedo, HdrpBlend blending)
        {
            var m = new Material(FindShader("HDRP/Unlit"));
            if (albedo != null) m.SetTexture("_UnlitColorMap", albedo);
            SetUnlitColor(m, Color.white);
            m.SetFloat("_SurfaceType", HdrpTransparent);
            m.SetFloat("_BlendMode", (float)blending);
            m.SetFloat("_DoubleSidedEnable", 1f);
            HDMaterial.ValidateMaterial(m);
            return m;
        }

        /// <summary>
        /// Mul / MulX2 blending (Assets/Shaders/HplMultiply.shader). Decals draw before other transparent objects,
        /// as HPL2 draws them into the G-buffer, and keep its alpha test (RenderDecals: limit 0.01).
        /// </summary>
        static Material CreateMultiply(Texture2D albedo, bool x2, bool decal)
        {
            var m = new Material(FindShader(MultiplyShader));
            if (albedo != null) m.SetTexture(BaseColorMap, albedo);
            m.SetColor(BaseColor, Color.white);
            m.SetFloat("_HplScale", x2 ? 2f : 1f);
            m.SetFloat("_HplAlphaLimit", decal ? 0.01f : 0f);
            m.renderQueue = decal ? (int)RenderQueue.Transparent - 100 : (int)RenderQueue.Transparent;
            return m;
        }

        public const string MultiplyShader = "RTXMnesia/HplMultiply";

        static Material CreateDecal(Func<TextureRole, Texture2D> tex)
        {
            var m = new Material(FindShader("HDRP/Decal"));
            var albedo = tex(TextureRole.Albedo);
            var normal = tex(TextureRole.Normal);
            if (albedo != null) m.SetTexture(BaseColorMap, albedo);
            m.SetFloat("_AffectAlbedo", albedo != null ? 1f : 0f);
            if (normal != null) m.SetTexture("_NormalMap", normal);
            m.SetFloat("_AffectNormal", normal != null ? 1f : 0f);
            HDMaterial.ValidateMaterial(m);
            return m;
        }

        /// <summary>
        /// HDRP/Unlit color that shows the HPL2 color <paramref name="hplColor"/> on screen. HDRP does not expose
        /// unlit color (ShaderPassForwardUnlit.hlsl: color × _DeExposureMultiplier = 1), so it is used as is.
        /// </summary>
        public static void SetUnlitColor(Material m, Color hplColor) => m.SetColor("_UnlitColor", hplColor);

        static Shader FindShader(string name) =>
            Shader.Find(name) ?? throw new InvalidOperationException($"Shader '{name}' not found (is HDRP installed?)");

        /// <summary>Creates the asset, or overwrites an existing one keeping its GUID.</summary>
        static void SaveInPlace(Material material, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(material, existing);
                existing.name = Path.GetFileNameWithoutExtension(assetPath);
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(material);
                return;
            }
            AssetDatabase.CreateAsset(material, assetPath);
        }
    }
}
