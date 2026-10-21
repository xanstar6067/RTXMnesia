using System;
using System.Collections;
using System.IO;
using System.Linq;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Dev
{
    /// <summary>Opt-in verification of the normal release EXE: -reliefCheck absolute-output-directory.</summary>
    public sealed class HplReliefPlayerCheck : MonoBehaviour
    {
        string output, preferences;
        HplUserSettings saved;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Startup()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-reliefCheck");
            if (index < 0 || index + 1 >= args.Length) return;
            var go = new GameObject("Release relief verification"); DontDestroyOnLoad(go);
            go.AddComponent<HplReliefPlayerCheck>().output = Path.GetFullPath(args[index + 1]);
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            var run = Run(); int code = 0;
            while (true)
            {
                object current = null; bool more;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception e) { Debug.LogException(e); File.WriteAllText(Path.Combine(output, "Result.txt"), "FAIL\n" + e); code = 1; break; }
                if (!more) break;
                yield return current;
            }
            SceneManager.sceneLoaded -= OnLoaded;
            if (saved != null) HplGraphics.Apply(saved);
            Application.Quit(code);
        }

        static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            Debug.Log("[ReliefPlayerCheck] " + message);
        }
        static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var map in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HplMap>(true)))
            { map.runScript = false; map.disableAutoSaves = true; }
        }

        IEnumerator Run()
        {
            Application.runInBackground = true;
            saved = HplUserSettings.ReadSaved(); preferences = PlayerPrefs.GetString(HplUserSettings.PrefsKey, "");
            Debug.Log("[ReliefPlayerCheck] saved textures=" + saved.textures);
            var settings = HplUserSettings.ReadSaved(); settings.textures = 1; settings.lighting = 0;
            settings.screenMode = 2; settings.resolutionWidth = 1280; settings.resolutionHeight = 720; settings.dlssMode = 0;
            HplGraphics.Apply(settings);
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("01_old_archives");
            yield return new WaitForSecondsRealtime(1);
            var scene = SceneManager.GetActiveScene();
            Check(HplEnhancedRelief.ActiveMeshCount > 150, "release scene automatically receives enhanced geometry");
            var map = HplMap.Current; map.player.readDevices = false;
            HplEnhancedTextures.ApplyScene(scene, false);
            var wall = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).First(r =>
                r.transform.parent != null && r.transform.parent.name.StartsWith("wall_default_", StringComparison.Ordinal) &&
                !r.transform.parent.name.Contains("extension", StringComparison.Ordinal) && r.sharedMaterials.Any(m => m != null &&
                m.HasProperty("_BaseColorMap") && m.GetTexture("_BaseColorMap")?.name == "castlebase_wall__albedo"));
            var filter = wall.GetComponent<MeshFilter>(); var original = filter.sharedMesh; var materials = wall.sharedMaterials;
            var colliders = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshCollider>(true)).ToDictionary(c => c, c => c.sharedMesh);
            Check(!wall.isPartOfStaticBatch, "release wall excluded from static batching");
            map.PlayerCamera.tag = "Untagged"; map.PlayerCamera.enabled = false;
            var camera = new GameObject("Release inspection camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.nearClipPlane = .01f;
            camera.gameObject.AddComponent<HDAdditionalCameraData>();
            var p = original.vertices; var uv = original.uv; var triangles = original.triangles;
            int t = Enumerable.Range(0, triangles.Length / 3).Select(i => i * 3).First(i =>
                Enumerable.Range(0, 3).All(j => uv[triangles[i+j]].x >= .001f && uv[triangles[i+j]].x < .72f && uv[triangles[i+j]].y > .64f));
            Vector3 point = wall.transform.TransformPoint((p[triangles[t]] + p[triangles[t+1]] + p[triangles[t+2]]) / 3);
            Vector3 normal = wall.localToWorldMatrix.inverse.transpose.MultiplyVector(original.normals[triangles[t]]).normalized;
            camera.transform.position = point + normal; camera.transform.LookAt(point);
            HplEnhancedTextures.ApplyScene(scene, true);
            var entry = HplEnhancedRelief.Find(wall, original, wall.sharedMaterials);
            Check(entry != null && entry.slots.Any(s => s.normalResource == "HplApprovedRelief/castlebase_wall_normal"), "release selects approved stone profile");
            Check(wall.sharedMaterials.Where((m, i) => !string.IsNullOrEmpty(entry.slots[i].normalResource)).All(m =>
                m.IsKeywordEnabled("_MATERIAL_FEATURE_SPECULAR_COLOR") && !m.IsKeywordEnabled("_PIXEL_DISPLACEMENT")), "release enables cavity reflectance and disables duplicate POM");
            yield return new WaitForSecondsRealtime(.6f);
            Check(filter.sharedMesh == Resources.Load<Mesh>(entry.detailedResource), "release near LOD is detailed");
            camera.transform.position = point + normal * 80;
            yield return new WaitForSecondsRealtime(.6f);
            Check(filter.sharedMesh == Resources.Load<Mesh>(entry.coarseResource), "release far LOD is coarse");
            camera.transform.position = point + normal * .08f;
            yield return new WaitForSecondsRealtime(.6f);
            Check(filter.sharedMesh == Resources.Load<Mesh>(entry.detailedResource), "release extreme close LOD is detailed");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = rt;
                for (int frame = 0; frame < 4; frame++) { camera.Render(); AsyncGPUReadback.Request(rt).WaitForCompletion(); }
                camera.targetTexture = null; rt.Release(); Destroy(rt);
            }
            Check(colliders.All(pair => pair.Key.sharedMesh == pair.Value), "release retains original collision meshes during extreme approach");
            HplEnhancedTextures.ApplyScene(scene, false);
            Check(filter.sharedMesh == original && wall.sharedMaterials.SequenceEqual(materials), "release restores exact classic mesh/materials");
            Check(PlayerPrefs.GetString(HplUserSettings.PrefsKey, "") == preferences, "release leaves saved user preferences untouched");
            File.WriteAllText(Path.Combine(output, "Result.txt"), $"RELEASE PASS: Old Archives, approved geometry, cavity specular workflow, both LODs, extreme close GPU rendering ({timer.ElapsedMilliseconds} ms for four frames), unchanged colliders/preferences, classic restoration\n");
        }
    }
}
