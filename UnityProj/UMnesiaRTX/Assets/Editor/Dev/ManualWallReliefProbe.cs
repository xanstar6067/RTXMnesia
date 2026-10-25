using System;
using System.IO;
using System.Linq;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    /// <summary>Probe of the castle wall relief built by tools/Build-ManualRelief.py (stone mask from the
    /// original HPL normal map). Renders flat / current approved / new on the views of
    /// ApprovedLevelReliefTests. Builds the new mesh and material in memory only: the relief catalog,
    /// Resources and the scene stay untouched.</summary>
    public static class ManualWallReliefProbe
    {
        static string Folder => Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief", "ManualWall");
        static string Maps => MapsFor("static_objects/castlebase/wall/castlebase_wall");
        /// <summary>Output folder of tools/Build-ManualRelief.py for a material path without extension.</summary>
        internal static string MapsFor(string material) => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(AmnesiaPaths.SourceRoot)), "sourceRTXRemix",
            material, ImportMenu.Arg("-reliefVariant", "manual-relief-v1"));

        [Serializable] internal class Info
        {
            public float[] height_encoding_metres, rows_from_top, ends;
            public float gap, chamfer, joint_depth, board_variation, tile_metres;
            public bool over_creases;
            public float mesh_tolerance_metres;
        }

        public static void RunBatch()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("The probe renders: run without -nographics.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Folder);
            Texture2D height = null, normal = null, mask = null;
            Mesh probe = null;
            Material[] probeMaterials = null;
            VolumeProfile profile = null;
            try
            {
                var info = JsonUtility.FromJson<Info>(File.ReadAllText(Path.Combine(Maps, "relief.json")));
                float lo = info.height_encoding_metres[0], hi = info.height_encoding_metres[1];

                var scene = EditorSceneManager.OpenScene(ApprovedLevelReliefTests.ScenePath, OpenSceneMode.Single);
                // After the scene load: opening a scene unloads unreferenced runtime textures.
                height = Load("height.png", false); normal = Load("normal.png", true); mask = Load("mask.png", true);
                var renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                var originals = renderers.Where(r => r.TryGetComponent<MeshFilter>(out _)).ToDictionary(r => r, r => r.GetComponent<MeshFilter>().sharedMesh);
                HplEnhancedTextures.ApplyScene(scene, true);
                var wall = originals.Keys.First(r =>
                    AssetDatabase.GetAssetPath(originals[r]).EndsWith("/castlebase/wall/default_meshes.asset", StringComparison.Ordinal));
                var original = originals[wall];
                var filter = wall.GetComponent<MeshFilter>();
                // The scene may have picked the distant LOD; compare with the detailed approved mesh.
                var entry = HplEnhancedRelief.Find(wall, original, wall.sharedMaterials);
                var current = Resources.Load<Mesh>(entry.detailedResource);
                var currentMaterials = wall.sharedMaterials;
                var classic = currentMaterials.Select(HplEnhancedTextures.ClassicMaterial).ToArray();
                bool IsWall(Material m) => m != null && m.HasProperty("_BaseColorMap") && m.GetTexture("_BaseColorMap")?.name == "castlebase_wall__albedo";

                var surfaces = new HplReliefMeshBuilder.Surface[original.subMeshCount];
                probeMaterials = (Material[])currentMaterials.Clone();
                for (int i = 0; i < surfaces.Length; i++)
                {
                    if (!IsWall(classic[i])) continue;
                    var scale = currentMaterials[i].GetTextureScale("_BaseColorMap"); var offset = currentMaterials[i].GetTextureOffset("_BaseColorMap");
                    // The maps carry the full stone shape: vertex normals stay those of the wall plane.
                    surfaces[i] = new HplReliefMeshBuilder.Surface { uvScale = scale, uvOffset = offset, geometryNormals = false,
                        heightMetres = uv => Mathf.Lerp(lo, hi, height.GetPixelBilinear(uv.x, uv.y).r) };
                    var material = new Material(currentMaterials[i]) { name = "castlebase_wall (manual relief probe)" };
                    material.SetTexture("_NormalMap", normal); material.SetFloat("_NormalScale", 1);
                    material.SetTexture("_MaskMap", mask);
                    material.SetTexture("_SpecularColorMap", null); material.SetColor("_SpecularColor", new Color(.04f, .04f, .04f, 1));
                    material.SetFloat("_DisplacementMode", 0);
                    HDMaterial.ValidateMaterial(material);
                    probeMaterials[i] = material;
                }
                if (surfaces.All(s => s == null)) throw new InvalidOperationException("Wall module has no castlebase_wall slot.");
                // Same density search as the importer, so both reliefs get the same triangle budget.
                float spacing = .01f;
                for (int attempt = 0; probe == null; attempt++)
                {
                    try { probe = HplReliefMeshBuilder.Build(original, surfaces, HplReliefSet.ScaleOf(wall.transform), spacing); }
                    catch (InvalidOperationException e) when (e.Message.StartsWith("Relief triangle budget", StringComparison.Ordinal) && attempt < 8) { spacing *= 1.5f; }
                }
                Debug.Log($"[ManualWallRelief] probe mesh, spacing {spacing * 100:F1} cm: {probe.vertexCount} vertices, {Enumerable.Range(0, probe.subMeshCount).Sum(s => (long)probe.GetIndexCount(s)) / 3} triangles; current: {current.vertexCount} vertices, {Enumerable.Range(0, current.subMeshCount).Sum(s => (long)current.GetIndexCount(s)) / 3} triangles, module {Vector3.Scale(original.bounds.size, HplReliefSet.ScaleOf(wall.transform))}");

                var face = ApprovedLevelReliefTests.Face(wall, original);
                var inspection = ApprovedLevelReliefTests.Inspection(face.point, face.normal);
                var camera = inspection.camera; profile = inspection.profile;
                var far = camera.transform.position;
                var right = Vector3.Cross(Vector3.up, face.normal);
                void Render(string stem)
                {
                    filter.sharedMesh = original; wall.sharedMaterials = currentMaterials;
                    ImportMenu.RenderToFile(camera, Path.Combine(Folder, stem + "_0_flat.png"));
                    filter.sharedMesh = current;
                    ImportMenu.RenderToFile(camera, Path.Combine(Folder, stem + "_1_current.png"));
                    filter.sharedMesh = probe; wall.sharedMaterials = probeMaterials;
                    ImportMenu.RenderToFile(camera, Path.Combine(Folder, stem + "_2_manual.png"));
                    filter.sharedMesh = current; wall.sharedMaterials = currentMaterials;
                }
                foreach (var r in renderers) r.enabled = r == wall;
                Render("module");
                foreach (var r in renderers) r.enabled = true;
                Render("level");
                // A grazing close-up shows silhouettes and the joints' self-shadowing.
                camera.transform.position = face.point + face.normal * .35f + right * .55f + Vector3.up * .1f;
                camera.transform.LookAt(face.point - right * .2f);
                Render("grazing");
                camera.transform.position = far;
                File.WriteAllText(Path.Combine(Folder, "Result.txt"), $"Rendered module/level/grazing x flat/current/manual from {Maps}\n");
            }
            finally
            {
                HplEnhancedTextures.Shutdown();
                if (probeMaterials != null) foreach (var m in probeMaterials.Where(m => m != null && m.name.EndsWith("(manual relief probe)", StringComparison.Ordinal))) Object.DestroyImmediate(m);
                foreach (Object o in new Object[] { probe, height, normal, mask, profile }) if (o != null) Object.DestroyImmediate(o);
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static Texture2D Load(string file, bool mips)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mips, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8, hideFlags = HideFlags.HideAndDontSave };
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.Combine(Maps, file)), false)) throw new InvalidOperationException("Cannot read " + file);
            return texture;
        }
    }
}
