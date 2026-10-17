using System.IO;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// GPU check of HplWater.shader (edit mode, needs a GPU: -batchmode -quit, no -nographics): a lit checker floor
    /// under a water quad, rendered without water and with three imported water materials (cube map reflection,
    /// world reflection — no mirror camera in edit mode, so none —, fake shading). Logs/WaterCheck/*.png and the mean
    /// brightness of the water area in Editor.log.
    /// -executeMethod RTXMnesia.EditorDev.WaterRenderCheck.RunBatch
    /// </summary>
    public static class WaterRenderCheck
    {
        public static void RunBatch()
        {
            string folder = Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "WaterCheck");
            Directory.CreateDirectory(folder);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            var checker = new Texture2D(256, 256, TextureFormat.RGBA32, true);
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                    checker.SetPixel(x, y, ((x / 32) + (y / 32)) % 2 == 0 ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.15f, 0.3f, 0.6f));
            checker.Apply();
            var floorMaterial = new Material(Shader.Find("HDRP/Lit"));
            floorMaterial.SetTexture("_BaseColorMap", checker);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.localScale = new Vector3(8f, 0.2f, 8f);
            floor.transform.position = new Vector3(0f, -0.6f, 0f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.position = new Vector3(0.5f, 0f, 0.5f);
            pole.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
            var poleMaterial = new Material(Shader.Find("HDRP/Lit"));
            poleMaterial.SetColor("_BaseColor", new Color(0.9f, 0.2f, 0.1f));
            pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;

            var water = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            water.transform.localScale = new Vector3(6f, 6f, 1f);
            water.transform.position = new Vector3(0f, 0f, 0f);

            var camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            camera.transform.position = new Vector3(0f, 2.2f, -3.2f);
            camera.transform.LookAt(new Vector3(0f, -0.3f, 0.5f));

            void Render(string name, Material material)
            {
                water.SetActive(material != null);
                if (material != null) water.GetComponent<Renderer>().sharedMaterial = material;
                string file = Path.Combine(folder, name + ".png");
                ImportMenu.RenderToFile(camera, file, image =>
                {
                    Color sum = Color.black;
                    int n = 0;
                    for (int y = 300; y < 700; y += 4)
                        for (int x = 660; x < 1260; x += 4) { sum += image.GetPixel(x, y); n++; }
                    Debug.Log($"[WaterCheck] {name}: mean {sum / n}");
                });
            }

            Render("0_no_water", null);
            // World reflection: the mirror camera rendered once for this view (static scene).
            var cellar = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.MaterialsRoot + "/textures/water/water_archivecellar.mat");
            water.SetActive(true);
            water.GetComponent<Renderer>().sharedMaterial = cellar;
            var reflection = World.HplWaterReflection.Create(new[] { water.GetComponent<MeshRenderer>() }, null);
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = rt;
            bool rendered = reflection != null && reflection.RenderFor(camera);
            camera.targetTexture = null;
            Debug.Log($"[WaterCheck] world reflection surface {(reflection != null ? reflection.SurfaceCount : 0)}, rendered {rendered}");
            Render("water_archivecellar_world_reflection", cellar);
            // The mirror image alone: Fresnel bias 1 shows the full reflection.
            var fullReflection = new Material(cellar);
            fullReflection.SetFloat("_FresnelBias", 1f);
            Render("water_world_reflection_fresnel1", fullReflection);
            if (reflection != null) Object.DestroyImmediate(reflection.gameObject);
            rt.Release();
            foreach (string rel in new[] { "textures/water/water_sewer", "textures/water/water_archivecellar", "textures/water/water_blood_hole", "textures/water/water_acid" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.MaterialsRoot + "/" + rel + ".mat");
                Render(Path.GetFileName(rel), material);
            }
        }
    }
}
