using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.World
{
    /// <summary>
    /// Glowing materials light their surroundings through RTGI (enhanced lighting with ray-traced GI). HPL2's
    /// illumination is a screen value (HDRP: <see cref="HplLighting.UnitRadiance"/> nits at white): RTGI already takes it,
    /// but a few nits light nothing around. Raising it would clip the coloured glow to white on screen (there is no
    /// tone mapping), so each renderer of a material of the table gets a copy that only RTGI sees: on
    /// <see cref="HplRayTracing.GiOnlyLayer"/> (the cameras, RTAO, RT reflections and path tracing leave it out), without
    /// shadows, its emission × the table's gain, its mesh pushed 3 mm out along the normals so that the GI rays hit the
    /// copy rather than the original under it. The copy follows the original: visible / hidden, its materials (texture
    /// set, a lamp or prop's emission amount). Windows and lamps are not in the table: they have lights with shadows.
    /// The table: Resources/HplEnhancedLighting/Emitters.json.
    /// </summary>
    public static class HplEnhancedEmission
    {
        public const string TableResource = HplEnhancedLighting.ResourceFolder + "Emitters";
        /// <summary>How far the copy's surface lies outside the original's (mesh units).</summary>
        public const float Offset = 0.003f;

        [Serializable]
        sealed class Table
        {
            public Emitter[] materials;
        }

        [Serializable]
        sealed class Emitter
        {
            /// <summary>The classic material's name (the .mat file without its extension).</summary>
            public string name;
            /// <summary>Multiplier of the material's emission for the GI.</summary>
            public string gain;
            /// <summary>Why: not read, for the reader of the file.</summary>
            public string note;
        }

        static Dictionary<string, float> s_gains;
        static readonly List<GameObject> s_copies = new();
        static readonly Dictionary<Mesh, Mesh> s_shells = new();
        static readonly Dictionary<(Material source, float gain), Material> s_materials = new();

        /// <summary>Debug (the probe's "noemit" mode): no copies.</summary>
        public static bool Enabled = true;
        /// <summary>Debug (the probe's "emitx" mode): all gains × this.</summary>
        public static float GainScale = 1f;
        /// <summary>Renderers with a GI copy (checks).</summary>
        public static int Copies => s_copies.Count;
        /// <summary>Renderers of the table left out because they are static batched (their mesh is the batch's).</summary>
        public static int SkippedBatched { get; private set; }
        /// <summary>Copies on the original mesh (unreadable: not pushed out, the GI rays hit either surface; checks).</summary>
        public static int NotPushed { get; private set; }

        static Dictionary<string, float> Gains
        {
            get
            {
                if (s_gains != null) return s_gains;
                s_gains = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                var text = Resources.Load<TextAsset>(TableResource);
                if (text == null) { HplLog.PortError("Enhanced lighting: no Resources/" + TableResource); return s_gains; }
                try
                {
                    var table = JsonUtility.FromJson<Table>(text.text);
                    foreach (var e in table?.materials ?? Array.Empty<Emitter>())
                        if (!string.IsNullOrEmpty(e.name) && float.TryParse(e.gain, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float gain) && gain > 0f)
                            s_gains[e.name.Trim()] = gain;
                }
                catch (ArgumentException e)
                {
                    HplLog.PortError($"Enhanced lighting {TableResource}: {e.Message}");
                }
                return s_gains;
            }
        }

        /// <summary>The copies for the renderers under <paramref name="root"/> (the enhanced lighting is applied with RTGI).</summary>
        public static void Apply(Component root)
        {
            Revert();
            var gains = Gains;
            if (!Enabled || gains.Count == 0) return;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.gameObject.layer == HplRayTracing.GiOnlyLayer || HplEnhancedLighting.IsWax(renderer)) continue;
                if (renderer.rayTracingMode == UnityEngine.Experimental.Rendering.RayTracingMode.Off) continue;
                if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;
                var materials = renderer.sharedMaterials;
                var slotGains = new float[materials.Length];
                bool any = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var classic = Classic(renderer, i);
                    if (classic != null && gains.TryGetValue(classic.name, out float gain)) { slotGains[i] = gain * GainScale; any = true; }
                }
                if (!any) continue;
                if (renderer.isPartOfStaticBatch) { SkippedBatched++; continue; }

                var go = new GameObject("GI emission") { hideFlags = HideFlags.DontSave, layer = HplRayTracing.GiOnlyLayer };
                go.SetActive(false);
                go.transform.SetParent(renderer.transform, false);
                var shell = Shell(filter.sharedMesh);
                if (shell == filter.sharedMesh) NotPushed++;
                go.AddComponent<MeshFilter>().sharedMesh = shell;
                var copy = go.AddComponent<MeshRenderer>();
                copy.shadowCastingMode = ShadowCastingMode.Off;
                copy.receiveShadows = false;
                copy.lightProbeUsage = LightProbeUsage.Off;
                copy.reflectionProbeUsage = ReflectionProbeUsage.Off;
                copy.renderingLayerMask = renderer.renderingLayerMask;
                copy.rayTracingMode = renderer.rayTracingMode;
                var follow = go.AddComponent<HplGiEmissionCopy>();
                follow.source = renderer;
                follow.gains = slotGains;
                follow.Sync();
                go.SetActive(true);
                s_copies.Add(go);
            }
        }

        /// <summary>The classic material of a renderer's slot (under a prop's emission copy or an enhanced texture set).</summary>
        static Material Classic(Renderer renderer, int slot)
        {
            if (renderer.TryGetComponent(out HplEmission emission) && emission.ClassicSource(slot) is { } source) return source;
            var materials = renderer.sharedMaterials;
            return slot < materials.Length ? HplEnhancedTextures.ClassicMaterial(materials[slot]) : null;
        }

        /// <summary>The mesh pushed out along its normals (shared by the copies of one mesh); the mesh itself when unreadable.</summary>
        static Mesh Shell(Mesh mesh)
        {
            if (s_shells.TryGetValue(mesh, out var shell) && shell != null) return shell;
            if (!mesh.isReadable) return mesh;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            if (normals.Length != vertices.Length) return mesh;
            for (int i = 0; i < vertices.Length; i++) vertices[i] += normals[i] * Offset;
            shell = UnityEngine.Object.Instantiate(mesh);
            shell.name = mesh.name + " (GI emission)";
            shell.hideFlags = HideFlags.DontSave;
            shell.vertices = vertices;
            shell.RecalculateBounds();
            s_shells[mesh] = shell;
            return shell;
        }

        /// <summary>The copy's material of a slot: the original's with its emission × gain (shared between copies).</summary>
        internal static Material Material(Material source, float gain)
        {
            if (source == null || gain <= 0f) return source;
            if (!s_materials.TryGetValue((source, gain), out var material) || material == null)
            {
                material = new Material(source) { name = source.name + " (GI emission)", hideFlags = HideFlags.DontSave };
                s_materials[(source, gain)] = material;
            }
            return material;
        }

        /// <summary>Removes the copies (classic lighting, no RTGI, the map leaves).</summary>
        public static void Revert()
        {
            foreach (var go in s_copies)
                if (go != null)
                {
                    // Destroy waits for the end of the frame; inactive, the copy is out of the acceleration structure now.
                    go.SetActive(false);
                    UnityEngine.Object.Destroy(go);
                }
            s_copies.Clear();
            foreach (var shell in s_shells.Values) if (shell != null) UnityEngine.Object.Destroy(shell);
            s_shells.Clear();
            foreach (var material in s_materials.Values) if (material != null) UnityEngine.Object.Destroy(material);
            s_materials.Clear();
            SkippedBatched = 0;
            NotPushed = 0;
        }
    }

    /// <summary>A GI copy of <see cref="HplEnhancedEmission"/>: follows its original's visibility and materials.</summary>
    public sealed class HplGiEmissionCopy : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_EmissiveColor");
        public Renderer source;
        public float[] gains;
        MeshRenderer _renderer;
        Material[] _sources = Array.Empty<Material>();

        void LateUpdate()
        {
            if (source == null) { Destroy(gameObject); return; }
            Sync();
        }

        public void Sync()
        {
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            if (_renderer.enabled != source.enabled) _renderer.enabled = source.enabled;
            var materials = source.sharedMaterials;
            bool changed = materials.Length != _sources.Length;
            for (int i = 0; !changed && i < materials.Length; i++) changed = materials[i] != _sources[i];
            if (changed)
            {
                _sources = materials;
                var own = new Material[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                    own[i] = HplEnhancedEmission.Material(materials[i], i < gains.Length ? gains[i] : 0f);
                _renderer.sharedMaterials = own;
            }
            // The prop's emission amount (a lamp put out, a script's fade) is in its material's color.
            var copies = _renderer.sharedMaterials;
            for (int i = 0; i < materials.Length && i < copies.Length; i++)
            {
                if (i >= gains.Length || gains[i] <= 0f || materials[i] == null || copies[i] == null || !materials[i].HasProperty(ColorId)) continue;
                Color target = materials[i].GetColor(ColorId) * gains[i];
                if (copies[i].GetColor(ColorId) != target) copies[i].SetColor(ColorId, target);
            }
        }
    }
}
