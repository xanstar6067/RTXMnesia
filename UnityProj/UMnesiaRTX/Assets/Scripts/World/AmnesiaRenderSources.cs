using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// The source of each model renderer of a level scene: the mesh name and the HPL model file (.dae, relative to the
    /// game folder). Written on the scene copy before static batching (Editor RenderSourceProcessor), which replaces
    /// the meshes by "Combined Mesh (root: scene)"; read by the bug report (F1).
    /// </summary>
    public sealed class AmnesiaRenderSources : MonoBehaviour
    {
        public Renderer[] renderers = System.Array.Empty<Renderer>();
        public string[] meshes = System.Array.Empty<string>();
        /// <summary>Index into <see cref="files"/>, -1 — unknown.</summary>
        public int[] fileIndices = System.Array.Empty<int>();
        public string[] files = System.Array.Empty<string>();

        Dictionary<Renderer, int> _lookup;

        public static bool TryFind(Renderer renderer, out string mesh, out string file)
        {
            mesh = file = null;
            if (renderer == null) return false;
            foreach (var sources in FindObjectsByType<AmnesiaRenderSources>())
            {
                if (sources.gameObject.scene != renderer.gameObject.scene) continue;
                if (sources._lookup == null)
                {
                    sources._lookup = new Dictionary<Renderer, int>(sources.renderers.Length);
                    for (int i = 0; i < sources.renderers.Length; i++)
                        if (sources.renderers[i] != null) sources._lookup[sources.renderers[i]] = i;
                }
                if (!sources._lookup.TryGetValue(renderer, out int index)) continue;
                mesh = sources.meshes[index];
                int f = sources.fileIndices[index];
                file = f >= 0 ? sources.files[f] : null;
                return true;
            }
            return false;
        }
    }
}
