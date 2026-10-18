#if UNITY_EDITOR || RTXMNESIA_RENDER_TEST
using System;
using System.Collections;
using System.IO;
using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.Dev
{
    /// <summary>Only present in a dedicated render-test build; never compiled into the normal game.</summary>
    public sealed class HplDecalRenderProbe : MonoBehaviour
    {
        public Camera captureCamera;
        public Renderer overlay;
        public Material[] materials; // Legacy/fixed pairs: HDRP decal, alpha transparent, premultiplied transparent.
        public MeshRenderer[] furniture;

        IEnumerator Start()
        {
            Application.runInBackground = true;
            // Disable temporal jitter in this process only; never save or overwrite the user's settings.
            var settings = HplUserSettings.ReadSaved();
            settings.antialiasing = (int)HplAntialiasing.Off;
            HplGraphics.Apply(settings);
            string[] args = Environment.GetCommandLineArgs();
            int arg = Array.IndexOf(args, "-decalOutput");
            string folder = arg >= 0 && arg + 1 < args.Length ? args[arg + 1] : Application.persistentDataPath + "/DecalCheck";
            Directory.CreateDirectory(folder);
            var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            captureCamera.targetTexture = rt;
            var images = new Color[materials.Length + 1][];
            for (int i = 0; i < images.Length; i++)
            {
                overlay.enabled = i > 0;
                if (i > 0) overlay.sharedMaterial = materials[i - 1];
                for (int frame = 0; frame < 40; frame++) yield return null;
                // A hidden batch player may skip the normal presentation loop; render the off-screen camera explicitly.
                for (int frame = 0; frame < 30; frame++)
                {
                    captureCamera.Render();
                    AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                }
                var request = AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32);
                while (!request.done) yield return null;
                if (request.hasError) { Debug.LogError("[DecalPlayer] GPU readback failed"); Application.Quit(1); yield break; }
                var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                texture.SetPixelData(request.GetData<byte>(), 0);
                texture.Apply();
                images[i] = texture.GetPixels();
                File.WriteAllBytes(Path.Combine(folder, i == 0 ? "0_baseline.png" : i + "_" + materials[i - 1].name + ".png"), texture.EncodeToPNG());
                Destroy(texture);
            }
            int failures = 0;
            foreach (var renderer in furniture)
            {
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                bool pass = mesh != null && mesh.vertexCount > 0 && renderer.isVisible;
                if (!pass) failures++;
                Debug.Log($"[DecalPlayer] {(pass ? "PASS" : "FAIL")} furniture {renderer.name}: mesh={mesh?.name}, vertices={mesh?.vertexCount}, visible={renderer.isVisible}");
            }
            for (int i = 0; i < materials.Length; i += 2)
            {
                float legacy = Difference(images[0], images[i + 1]), current = Difference(images[0], images[i + 2]);
                bool pass = legacy > 0.002f && current < 0.001f;
                if (!pass) failures++;
                Debug.Log($"[DecalPlayer] {(pass ? "PASS" : "FAIL")} {materials[i + 1].name}: legacy={legacy:F6}, fixed={current:F6}");
            }
            captureCamera.targetTexture = null;
            rt.Release();
            Debug.Log($"[DecalPlayer] {(failures == 0 ? "PASS" : "FAIL")}: {materials.Length / 2} player render comparisons, {failures} failures");
            Application.Quit(failures == 0 ? 0 : 1);
        }

        static float Difference(Color[] a, Color[] b)
        {
            double sum = 0;
            int count = 0;
            for (int y = 210; y < 330; y++)
                for (int x = 380; x < 580; x++)
                {
                    int i = y * 960 + x;
                    sum += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b);
                    count += 3;
                }
            return (float)(sum / count);
        }
    }
}
#endif
