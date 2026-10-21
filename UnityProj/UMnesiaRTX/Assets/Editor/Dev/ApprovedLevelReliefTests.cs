using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    /// <summary>Integration checks on the actual castle wall module and its instances in Old Archives.</summary>
    [InitializeOnLoad]
    public static class ApprovedLevelReliefTests
    {
        public const string ScenePath = "Assets/AmnesiaImported/Levels/maps/main/ch01/01_old_archives.unity";
        static string Folder => Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief");
        const string Pending = "RTXMnesia.ApprovedLevelRelief.Pending", ReturnScene = Pending + ".Scene";
        static int s_phase, s_exit;
        static double s_next, s_deadline;
        static HplUserSettings s_saved;
        static string s_prefs;
        static MeshRenderer s_wall;
        static Mesh s_mesh;
        static Material[] s_materials;
        static Dictionary<MeshCollider, Mesh> s_colliders;
        static Camera s_camera;
        static Vector3 s_point, s_normal;
        static ApprovedLevelReliefTests()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                { s_phase = 0; s_next = EditorApplication.timeSinceStartup + 2; s_deadline = s_next + 120; s_exit = 1; }
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    string path = SessionState.GetString(ReturnScene, "");
                    if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    if (Application.isBatchMode) EditorApplication.Exit(s_exit);
                }
            };
            EditorApplication.update += UpdatePlay;
        }

        public static void RunPlayBatch()
        {
            Directory.CreateDirectory(Folder);
            SessionState.SetString(ReturnScene, SceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Object.FindAnyObjectByType<HplMap>().runScript = false;
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }

        public static void BuildGameBatch()
        {
            DevRoomReliefBuilder.RefreshStandReflectanceBatch();
            DevRoomReliefBuilder.CheckBatch();
            EnhancedReliefTests.RunBatch();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) EnhancedReliefTests.RunRenderBatch();
            string path = ImportMenu.Arg("-buildOutput", Path.Combine(AmnesiaPaths.ProjectRoot, "Builds", "ApprovedRelief", "UMnesiaRTX.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                target = BuildTarget.StandaloneWindows64,
                locationPathName = path,
                options = BuildOptions.None,
            });
            Require(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded, "normal game release built: " + path);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), "ReliefBuild.txt"),
                $"Approved stone and boards; built {DateTime.UtcNow:O}\n" +
                $"Normal release, {report.summary.totalSize} bytes, {report.summary.totalErrors} errors\n" +
                File.ReadAllText(Path.Combine(Folder, "ApprovedLevel.txt")));
        }

        public static void RebuildGameBatch()
        {
            EnhancedReliefConverter.ConvertApprovedBatch();
            BuildGameBatch();
        }

        static void UpdatePlay()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                Require(EditorApplication.timeSinceStartup < s_deadline, "actual level Play watchdog");
                var scene = SceneManager.GetActiveScene();
                switch (s_phase)
                {
                    case 0:
                        Debug.Log($"[ApprovedLevelRelief] startup: textures={HplGraphics.Textures}, active meshes={HplEnhancedRelief.ActiveMeshCount}");
                        s_saved = HplUserSettings.ReadSaved(); s_prefs = PlayerPrefs.GetString(HplUserSettings.PrefsKey, "");
                        var settings = HplUserSettings.ReadSaved(); settings.textures = 0; settings.lighting = 0; HplGraphics.Apply(settings);
                        var map = HplMap.Current; map.player.readDevices = false; map.effects.Fade.FadeIn(0);
                        s_wall = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
                            .Where(r => r.TryGetComponent<MeshFilter>(out var f) && AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("/castlebase/wall/default_meshes.asset", StringComparison.Ordinal))
                            .OrderBy(r => r.bounds.SqrDistance(map.PlayerCamera.transform.position)).First();
                        s_mesh = s_wall.GetComponent<MeshFilter>().sharedMesh; s_materials = s_wall.sharedMaterials;
                        s_colliders = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshCollider>(true)).ToDictionary(c => c, c => c.sharedMesh);
                        Require(!s_wall.isPartOfStaticBatch, "actual level Play: wall remains replaceable after scene processing");
                        var face = Face(s_wall, s_mesh); s_point = face.point; s_normal = face.normal;
                        map.PlayerCamera.tag = "Untagged"; map.PlayerCamera.enabled = false;
                        s_camera = new GameObject("Approved Play camera").AddComponent<Camera>(); s_camera.tag = "MainCamera"; s_camera.nearClipPlane = .01f;
                        s_camera.gameObject.AddComponent<HDAdditionalCameraData>(); s_camera.transform.position = s_point + s_normal; s_camera.transform.LookAt(s_point);
                        settings.textures = 1; HplGraphics.Apply(settings);
                        break;
                    case 1:
                        var entry = HplEnhancedRelief.Find(s_wall, s_mesh, s_wall.sharedMaterials);
                        Require(entry != null && entry.slots.Any(s => s.normalResource == "HplApprovedRelief/castlebase_wall_normal"), "actual level Play: approved wall profile selected");
                        Require(s_wall.GetComponent<MeshFilter>().sharedMesh == Resources.Load<Mesh>(entry.detailedResource), "actual level Play: near camera uses detailed wall");
                        s_camera.transform.position = s_point + s_normal * 80;
                        break;
                    case 2:
                        var far = HplEnhancedRelief.Find(s_wall, s_mesh, s_wall.sharedMaterials);
                        Require(s_wall.GetComponent<MeshFilter>().sharedMesh == Resources.Load<Mesh>(far.coarseResource), "actual level Play: distant wall uses coarse mesh");
                        s_camera.transform.position = s_point + s_normal * .08f; s_camera.transform.LookAt(s_point);
                        break;
                    case 3:
                        var close = HplEnhancedRelief.Find(s_wall, s_mesh, s_wall.sharedMaterials);
                        Require(s_wall.GetComponent<MeshFilter>().sharedMesh == Resources.Load<Mesh>(close.detailedResource), "actual level Play: extreme approach restores detailed geometry");
                        Require(s_colliders.All(p => p.Key.sharedMesh == p.Value), "actual level Play: approach retains original collision meshes");
                        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                            ImportMenu.RenderToFile(s_camera, Path.Combine(Folder, "archives_play_extreme_close.png"));
                        var classic = HplUserSettings.ReadSaved(); classic.textures = 0; classic.lighting = 0; HplGraphics.Apply(classic);
                        Require(s_wall.GetComponent<MeshFilter>().sharedMesh == s_mesh && s_wall.sharedMaterials.SequenceEqual(s_materials), "actual level Play: exact classic mesh and materials restored");
                        Require(PlayerPrefs.GetString(HplUserSettings.PrefsKey, "") == s_prefs, "actual level Play: saved preferences untouched");
                        HplGraphics.Apply(s_saved); s_saved = null;
                        File.WriteAllText(Path.Combine(Folder, "ApprovedPlay.txt"), "PLAY PASS: Old Archives startup, approved profile, detailed/coarse LOD, extreme approach, unchanged collision meshes/preferences, classic restoration\n");
                        s_exit = 0; EditorApplication.ExitPlaymode(); return;
                }
                s_phase++; s_next = EditorApplication.timeSinceStartup + 2;
            }
            catch (Exception e)
            {
                Debug.LogException(e); File.WriteAllText(Path.Combine(Folder, "ApprovedPlay.txt"), "FAIL\n" + e);
                if (s_saved != null) HplGraphics.Apply(s_saved);
                s_saved = null; s_exit = 1; EditorApplication.ExitPlaymode();
            }
        }
        static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            Debug.Log("[ApprovedLevelRelief] " + message);
        }

        public static void RunBatch()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Folder);
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                var originals = renderers.Where(r => r.TryGetComponent<MeshFilter>(out _)).ToDictionary(r => r, r => r.GetComponent<MeshFilter>().sharedMesh);
                HplEnhancedTextures.ApplyScene(scene, true);
                int targets = 0;
                foreach (var renderer in originals.Keys)
                {
                    if (renderer.GetComponentInParent<Rigidbody>(true) != null) continue;
                    var classic = renderer.sharedMaterials.Select(HplEnhancedTextures.ClassicMaterial).ToArray();
                    if (!classic.Any(m => m != null && m.HasProperty("_BaseColorMap") && m.GetTexture("_BaseColorMap")?.name == "castlebase_wall__albedo")) continue;
                    if (!AssetDatabase.GetAssetPath(originals[renderer]).Contains("/castlebase/wall/", StringComparison.Ordinal)) continue;
                    var entry = HplEnhancedRelief.Find(renderer, originals[renderer], classic);
                    Require(entry != null, renderer.transform.parent.name + "/" + renderer.name + ": castle wall has a catalog entry");
                    for (int i = 0; i < classic.Length; i++)
                        if (classic[i] != null && classic[i].HasProperty("_BaseColorMap") && classic[i].GetTexture("_BaseColorMap")?.name == "castlebase_wall__albedo")
                            Require(entry.slots[i].normalResource == "HplApprovedRelief/castlebase_wall_normal" &&
                                renderer.sharedMaterials[i].GetTexture("_NormalMap") == Resources.Load<Texture2D>(entry.slots[i].normalResource),
                                renderer.name + ": approved profile takes precedence over legacy relief");
                    Require(renderer.GetComponent<MeshFilter>().sharedMesh != originals[renderer], renderer.name + ": visual mesh replaced");
                    targets++;
                }
                Require(targets > 50, "Old Archives: " + targets + " castle wall instances verified");
                var wall = originals.Keys.First(r =>
                    AssetDatabase.GetAssetPath(originals[r]).EndsWith("/castlebase/wall/default_meshes.asset", StringComparison.Ordinal));
                var original = originals[wall];
                var entryWall = HplEnhancedRelief.Find(wall, original, wall.sharedMaterials);
                var detail = Resources.Load<Mesh>(entryWall.detailedResource);
                wall.GetComponent<MeshFilter>().sharedMesh = detail;
                var face = Face(wall, original);
                var positions = detail.vertices; var uv = detail.uv;
                var heights = Enumerable.Range(0, positions.Length).Where(i => uv[i].x > .04f && uv[i].x < .66f && uv[i].y > .68f && uv[i].y < .95f)
                    .Select(i => Vector3.Dot(wall.transform.TransformPoint(positions[i]) - face.point, face.normal)).ToArray();
                float range = heights.Max() - heights.Min();
                Require(range > .025f && range < .06f, "actual castle wall depth range: " + range.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + " m");
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    var camera = new GameObject("Approved wall inspection").AddComponent<Camera>(); camera.tag = "MainCamera";
                    camera.nearClipPlane = .02f; camera.farClipPlane = 80; camera.fieldOfView = 60;
                    var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>(); hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                    hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color; hd.backgroundColorHDR = Color.black;
                    camera.transform.position = face.point + face.normal * 1.1f + Vector3.Cross(Vector3.up, face.normal) * .7f;
                    camera.transform.LookAt(face.point);
                    var volume = new GameObject("Relief inspection exposure").AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10000;
                    var profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
                    var exposure = profile.Add<Exposure>(true); exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(1);
                    profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                    profile.Add<VisualEnvironment>(true).skyType.Override(0);
                    var light = new GameObject("Inspection light").AddComponent<Light>(); light.type = LightType.Point; light.range = 8;
                    light.transform.position = face.point + face.normal * .7f - Vector3.Cross(Vector3.up, face.normal) * 1.1f + Vector3.up * .25f;
                    light.gameObject.AddComponent<HDAdditionalLightData>(); light.lightUnit = LightUnit.Candela; light.intensity = 60; light.shadows = LightShadows.Soft;
                    foreach (var r in renderers) r.enabled = r == wall;
                    RenderPair(camera, wall, original, "archives_wall_module");
                    foreach (var r in renderers) r.enabled = true;
                    RenderPair(camera, wall, original, "archives_wall_level");
                    volume.sharedProfile = null; Object.DestroyImmediate(profile);
                }
                File.WriteAllText(Path.Combine(Folder, "ApprovedLevel.txt"), $"PASS: {targets} approved castle wall instances; depth range {range:F4} m; actual level and module rendered when GPU available\n");
            }
            finally
            {
                HplEnhancedTextures.Shutdown();
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void RenderPair(Camera camera, MeshRenderer renderer, Mesh original, string stem)
        {
            var filter = renderer.GetComponent<MeshFilter>(); var detail = filter.sharedMesh;
            Color[] before = null, after = null;
            filter.sharedMesh = original;
            ImportMenu.RenderToFile(camera, Path.Combine(Folder, stem + "_0_flat.png"), t => before = t.GetPixels());
            filter.sharedMesh = detail;
            ImportMenu.RenderToFile(camera, Path.Combine(Folder, stem + "_1_approved.png"), t => after = t.GetPixels());
            Require(before.Zip(after, (a, b) => Mathf.Abs(a.r-b.r) + Mathf.Abs(a.g-b.g) + Mathf.Abs(a.b-b.b)).Average() > .002f, stem + ": visible geometry changes under identical lighting/materials");
        }

        static (Vector3 point, Vector3 normal) Face(Renderer renderer, Mesh mesh)
        {
            var triangles = mesh.triangles; var p = mesh.vertices; var uv = mesh.uv; float area = -1; int best = -1;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                var centerUv = (uv[triangles[t]] + uv[triangles[t+1]] + uv[triangles[t+2]]) / 3;
                if (centerUv.x < .025f || centerUv.x > .68f || centerUv.y < .66f || centerUv.y > .975f) continue;
                float a = Vector3.Cross(p[triangles[t+1]]-p[triangles[t]], p[triangles[t+2]]-p[triangles[t]]).sqrMagnitude;
                if (a > area) { area = a; best = t; }
            }
            Require(best >= 0, "module has a face in the approved stone atlas island");
            var point = (p[triangles[best]] + p[triangles[best+1]] + p[triangles[best+2]]) / 3;
            var normal = renderer.localToWorldMatrix.inverse.transpose.MultiplyVector(mesh.normals[triangles[best]]).normalized;
            return (renderer.transform.TransformPoint(point), normal);
        }
    }
}
