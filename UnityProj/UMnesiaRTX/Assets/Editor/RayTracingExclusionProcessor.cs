using RTXMnesia.Hpl;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// HplRayTracing.Exclude on the copy of every scene before the build (and Play) combines its static objects: once
    /// combined, a renderer no longer knows its own mesh, and a flat transparent decal (a blood trail tilted a few
    /// degrees) could not be told from a solid object any more. At runtime Exclude runs again for what comes later.
    /// </summary>
    public sealed class RayTracingExclusionProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) HplRayTracing.Exclude(renderer);
        }
    }
}
