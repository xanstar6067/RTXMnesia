using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Generates raster PSOs from imported material states without executing level scripts or shaders.
    /// Rebuilt automatically before a player build when shader/material/pipeline inputs change.
    /// </summary>
    public sealed class ShaderWarmupBuilder : IPreprocessBuildWithReport
    {
        public const string Root = "Assets/AmnesiaImported/ShaderWarmup";
        public const string ManifestPath = "Assets/AmnesiaImported/Resources/HplShaderWarmup.asset";
        const string CollectionPath = Root + "/Raster.asset";
        const string Revision = "raster-generated-v3";
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report) => Ensure();

        [MenuItem("Tools/RTXMnesia/Prepare raster shader collection")]
        public static void Ensure()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before generating shader states.");
            var materials = new SortedDictionary<string, Material>(StringComparer.Ordinal);
            var owned = new List<Material>();
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/AmnesiaImported" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    // Test/preview materials do not belong to the game.
                    if (path.Contains("/Preview/") || path.Contains("/ShaderWarmup/")) continue;
                    // Reading .mat state without resolving texture references keeps a build prepass small even
                    // with gigabytes of enhanced textures. Generated subassets still use Unity's serializer.
                    var sources = path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)
                        ? new[] { ReadMaterial(path) } : AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray();
                    foreach (var source in sources)
                    {
                        if (source == null) continue;
                        string key = MaterialKey(source);
                        if (materials.ContainsKey(key)) { if (path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) Object.DestroyImmediate(source); continue; }
                        var copy = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
                        if (path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) Object.DestroyImmediate(source);
                        // Preserve keywords, culling/blending/depth state, but avoid loading full game textures onto GPU.
                        foreach (string property in copy.GetTexturePropertyNames()) copy.SetTexture(property, null);
                        materials.Add(key, copy);
                        owned.Add(copy);
                    }
                }
                // Runtime-created effects have no .mat asset, so include their shaders explicitly.
                foreach (string path in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/Shaders" }).Select(AssetDatabase.GUIDToAssetPath))
                {
                    var material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(path)) { hideFlags = HideFlags.HideAndDontSave };
                    owned.Add(material);
                    materials.TryAdd(MaterialKey(material), material);
                }
                var ui = new Material(Shader.Find("UI/Default")) { hideFlags = HideFlags.HideAndDontSave };
                owned.Add(ui);
                materials.TryAdd(MaterialKey(ui), ui);
                foreach (var source in materials.Values.Where(m => m.shader.name == "UI/Default" || m.shader.name == "Hidden/RTXMnesia/HplGui").ToArray())
                    foreach (string[] keywords in new[] { new[] { "UNITY_UI_CLIP_RECT" }, new[] { "UNITY_UI_ALPHACLIP" }, new[] { "UNITY_UI_CLIP_RECT", "UNITY_UI_ALPHACLIP" } })
                    {
                        var material = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
                        foreach (string keyword in keywords) material.EnableKeyword(keyword);
                        owned.Add(material);
                        materials.TryAdd(MaterialKey(material), material);
                    }
                // Ordinary HDRP fullscreen shaders referenced by the pipeline resources (never compute/DXR).
                var pipelineResources = AssetDatabase.LoadAssetAtPath<Object>("Assets/HDRPDefaultResources/HDRenderPipelineGlobalSettings.asset");
                if (pipelineResources != null)
                {
                    var property = new SerializedObject(pipelineResources).GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue is not Shader shader) continue;
                        string shaderPath = AssetDatabase.GetAssetPath(shader);
                        if (shaderPath.Contains("/Editor/") || shader.name.Contains("Debug") || shader.name.Contains("Error")) continue;
                        var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                        owned.Add(material);
                        materials.TryAdd(MaterialKey(material), material);
                    }
                }
                if (materials.Count < 2) throw new BuildFailedException("No imported game materials for shader warmup.");
                string revision = SourceRevision(materials);
                var existing = AssetDatabase.LoadAssetAtPath<HplShaderWarmupManifest>(ManifestPath);
                if (existing != null && existing.revision == revision && existing.collection != null && existing.collection.totalGraphicsStateCount > 0)
                {
                    Debug.Log("[ShaderWarmup] Raster collection is current.");
                    return;
                }
                Debug.Log($"[ShaderWarmup] Generating {materials.Count} distinct material states.");
                var collection = Generate(materials.Values.ToArray());
                try
                {
                    Directory.CreateDirectory(Root);
                    Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath));
                    AssetDatabase.Refresh();
                    var saved = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(CollectionPath);
                    if (saved == null) { AssetDatabase.CreateAsset(collection, CollectionPath); saved = collection; }
                    else EditorUtility.CopySerialized(collection, saved);
                    var manifest = existing;
                    if (manifest == null)
                    {
                        manifest = ScriptableObject.CreateInstance<HplShaderWarmupManifest>();
                        AssetDatabase.CreateAsset(manifest, ManifestPath);
                    }
                    manifest.collection = saved;
                    manifest.revision = revision;
                    EditorUtility.SetDirty(saved);
                    EditorUtility.SetDirty(manifest);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[ShaderWarmup] Generated {saved.variantCount} raster variants / {saved.totalGraphicsStateCount} states from {materials.Count} material states.");
                }
                finally { if (!AssetDatabase.Contains(collection)) Object.DestroyImmediate(collection); }
            }
            finally
            {
                foreach (var material in owned) Object.DestroyImmediate(material);
                EditorUtility.ClearProgressBar();
            }
        }

        static Material ReadMaterial(string path)
        {
            string text = File.ReadAllText(path);
            string guid = Regex.Match(text, @"m_Shader: \{fileID: \d+, guid: ([0-9a-f]+)").Groups[1].Value;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
            if (shader == null) throw new BuildFailedException("Cannot resolve shader in " + path);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            string keywords = Regex.Match(text, @"m_ValidKeywords:([^\r\n]*[\r\n]+(?:  -[^\r\n]*[\r\n]+)*)").Groups[1].Value;
            material.shaderKeywords = Regex.Matches(keywords, @"(?m)^  - (\S+)").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
            foreach (Match match in Regex.Matches(text, @"(?m)^    - (\w+): ([-+0-9.eE]+)\r?$"))
                if (material.HasProperty(match.Groups[1].Value))
                    material.SetFloat(match.Groups[1].Value, float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
            var queue = Regex.Match(text, @"m_CustomRenderQueue: (-?\d+)");
            if (queue.Success) material.renderQueue = int.Parse(queue.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            material.enableInstancing = text.Contains("m_EnableInstancingVariants: 1");
            material.doubleSidedGI = text.Contains("m_DoubleSidedGI: 1");
            string passes = Regex.Match(text, @"disabledShaderPasses:([^\r\n]*[\r\n]+(?:  -[^\r\n]*[\r\n]+)*)").Groups[1].Value;
            foreach (Match match in Regex.Matches(passes, @"(?m)^  - (\S+)")) material.SetShaderPassEnabled(match.Groups[1].Value, false);
            return material;
        }

        static string MaterialKey(Material m) => m.shader.name + ":" + string.Join(",", m.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal)) +
            $":{m.renderQueue}:{m.enableInstancing}:{m.doubleSidedGI}:" +
            string.Join(",", new[] { "_CullMode", "_CullModeForward", "_Cull", "_ZWrite", "_ZTest", "_SrcBlend", "_DstBlend", "_SurfaceType", "_BlendOp", "_AlphaSrcBlend", "_AlphaDstBlend", "_StencilRef", "_StencilWriteMask", "_StencilRefDepth", "_StencilWriteMaskDepth", "_StencilRefGBuffer", "_StencilWriteMaskGBuffer" }
                .Select(p => m.HasProperty(p) ? m.GetFloat(p).ToString(System.Globalization.CultureInfo.InvariantCulture) : "")) + ":" +
            string.Join(",", Enumerable.Range(0, m.passCount).Select(p => m.GetShaderPassEnabled(m.GetPassName(p)) ? "1" : "0"));

        static string SourceRevision(SortedDictionary<string, Material> materials)
        {
            var text = new StringBuilder(Revision).AppendLine(Application.unityVersion);
            foreach (var pair in materials) text.AppendLine(pair.Key);
            foreach (string path in materials.Values.Select(m => AssetDatabase.GetAssetPath(m.shader)).Where(p => p != "").Distinct().OrderBy(p => p, StringComparer.Ordinal))
                text.Append(path).AppendLine(AssetDatabase.GetAssetDependencyHash(path).ToString());
            foreach (string path in AssetDatabase.FindAssets("t:RenderPipelineAsset t:RenderPipelineGlobalSettings", new[] { "Assets" })
                         .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal))
                text.Append(path).AppendLine(AssetDatabase.GetAssetDependencyHash(path).ToString());
            // Pipeline/package updates can change internal fullscreen shaders even when materials stay the same.
            text.Append(File.ReadAllText("Packages/packages-lock.json"));
            return HplShaderWarmupCache.Hash(text.ToString());
        }

        static GraphicsStateCollection Generate(Material[] materials)
        {
            // Unity 6.6 can generate states directly from mesh/material pairs, without tracing a player.
            // Generate several common HDRP attachment layouts; keep only raster passes appropriate to each.
            var result = new GraphicsStateCollection
            {
                graphicsDeviceType = GraphicsDeviceType.Direct3D12,
                runtimePlatform = RuntimePlatform.WindowsPlayer,
                qualityLevelName = QualitySettings.names[QualitySettings.GetQualityLevel()],
            };
            var mesh = new Mesh { name = "Raster warmup vertex layout" };
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            mesh.normals = Enumerable.Repeat(Vector3.forward, 3).ToArray();
            mesh.tangents = Enumerable.Repeat(new Vector4(1, 0, 0, 1), 3).ToArray();
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up };
            mesh.triangles = new[] { 0, 1, 2 };
            var variants = new List<GraphicsStateCollection.ShaderVariant>();
            var states = new List<GraphicsStateCollection.GraphicsState>();
            try
            {
                var layouts = new[]
                {
                    new[] { GraphicsFormat.R16G16B16A16_SFloat },
                    new[] { GraphicsFormat.B10G11R11_UFloatPack32 },
                    new[] { GraphicsFormat.R8G8B8A8_SRGB },
                    new[] { GraphicsFormat.R8_UNorm },
                    Array.Empty<GraphicsFormat>(),
                    new[] { GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.B10G11R11_UFloatPack32 },
                    new[] { GraphicsFormat.R16G16_SFloat, GraphicsFormat.R8G8B8A8_UNorm },
                };
                var globalSets = new[]
                {
                    new[] { "DECALS_OFF", "SCREEN_SPACE_SHADOWS_OFF", "PUNCTUAL_SHADOW_MEDIUM", "DIRECTIONAL_SHADOW_MEDIUM", "AREA_SHADOW_MEDIUM", "USE_FPTL_LIGHTLIST" },
                    new[] { "DECALS_4RT", "SCREEN_SPACE_SHADOWS_OFF", "PUNCTUAL_SHADOW_MEDIUM", "DIRECTIONAL_SHADOW_MEDIUM", "AREA_SHADOW_MEDIUM", "USE_CLUSTERED_LIGHTLIST" },
                    new[] { "DECALS_4RT", "SCREEN_SPACE_SHADOWS_ON", "PUNCTUAL_SHADOW_HIGH", "DIRECTIONAL_SHADOW_MEDIUM", "AREA_SHADOW_MEDIUM", "USE_CLUSTERED_LIGHTLIST" },
                };
                for (int layout = 0; layout < layouts.Length; layout++)
                {
                    bool depth = layout != 3 && layout != 2;
                    var formats = layouts[layout].Concat(depth ? new[] { GraphicsFormat.D32_SFloat_S8_UInt } : Array.Empty<GraphicsFormat>()).ToArray();
                    using var attachments = new Unity.Collections.NativeArray<AttachmentDescriptor>(formats.Select(f => new AttachmentDescriptor(f)).ToArray(), Unity.Collections.Allocator.Temp);
                    var subpass = new SubPassDescriptor();
                    subpass.colorOutputs = new AttachmentIndexArray(layouts[layout].Length);
                    for (int a = 0; a < layouts[layout].Length; a++) subpass.colorOutputs[a] = a;
                    using var subpasses = new Unity.Collections.NativeArray<SubPassDescriptor>(new[] { subpass }, Unity.Collections.Allocator.Temp);
                    foreach (var globals in globalSets)
                    {
                        var temporary = new GraphicsStateCollection();
                        try
                        {
                            temporary.AddGraphicsStates(Enumerable.Repeat(mesh, materials.Length).ToArray(), materials,
                                globals.Select(GlobalKeyword.Create).ToArray(), 1, attachments, subpasses, 0, depth ? formats.Length - 1 : -1);
                            temporary.GetVariants(variants);
                            foreach (var variant in variants)
                            {
                                string mode = PassMode(variant.shader, variant.passId);
                                if (mode == null || mode.IndexOf("RayTracing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    mode.IndexOf("DXR", StringComparison.OrdinalIgnoreCase) >= 0 || mode == "META" || mode == "LightTransport" ||
                                    mode == "Picking" || mode == "SceneSelectionPass" || mode == "FullScreenDebug") continue;
                                if (layout == 5 && mode != "GBuffer" || layout != 5 && mode == "GBuffer") continue;
                                if (layout == 4 && mode != "ShadowCaster" && mode != "DepthOnly") continue;
                                if (layout != 4 && mode == "ShadowCaster") continue;
                                if (layout == 6 && mode != "MotionVectors" && mode != "DepthForwardOnly") continue;
                                result.AddVariant(variant.shader, variant.passId, variant.keywords);
                                temporary.GetGraphicsStatesForVariant(variant, states);
                                foreach (var state in states)
                                {
                                    result.AddGraphicsStateForVariant(variant, state);
                                    // Fullscreen passes use SV_VertexID; UI uses Canvas's packed vertex stream.
                                    if (variant.shader.name.StartsWith("Hidden/", StringComparison.Ordinal))
                                    {
                                        var fullscreen = state;
                                        fullscreen.vertexAttributes = Array.Empty<VertexAttributeDescriptor>();
                                        result.AddGraphicsStateForVariant(variant, fullscreen);
                                    }
                                    if (variant.shader.name == "UI/Default" || variant.shader.name == "Hidden/RTXMnesia/HplGui")
                                    {
                                        var uiState = state;
                                        uiState.vertexAttributes = new[]
                                        {
                                            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                                            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
                                            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
                                        };
                                        result.AddGraphicsStateForVariant(variant, uiState);
                                    }
                                    var reflected = state;
                                    reflected.negativeScale = true;
                                    result.AddGraphicsStateForVariant(variant, reflected);
                                }
                            }
                        }
                        finally { Object.DestroyImmediate(temporary); }
                    }
                }
                if (result.totalGraphicsStateCount == 0) throw new BuildFailedException("Raster generation produced no graphics states.");
                return result;
            }
            catch { Object.DestroyImmediate(result); throw; }
            finally { Object.DestroyImmediate(mesh); }
        }

        internal static string PassMode(Shader shader, PassIdentifier id)
        {
            for (int sub = 0; sub < shader.subshaderCount; sub++)
                for (int pass = 0; pass < shader.GetPassCountInSubshader(sub); pass++)
                    if (id.Equals(new PassIdentifier((uint)sub, (uint)pass)))
                        return shader.FindPassTagValue(sub, pass, new ShaderTagId("LightMode")).name;
            return null;
        }

        public static void RunBatch()
        {
            try { Ensure(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
