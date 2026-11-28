using System;
using System.Collections;
using System.IO;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>One startup gate before logos/title UI. Only raster states, no compute or ray tracing dispatches.</summary>
    public sealed class HplShaderWarmupScreen : MonoBehaviour
    {
        public static bool CheckedThisRun { get; private set; }
        public static bool Active { get; private set; }
        public Canvas Canvas { get; private set; }
        HplGuiText _text;
        RawImage _fill;
        bool _russian;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRun() { CheckedThisRun = false; Active = false; }

        public static IEnumerator Prepare(HplMap map)
        {
            // Editor play and background checks do not alter the player's completion receipt.
            if (CheckedThisRun || Application.isEditor || Sound.HplBatchAudio.IsCheckRun) yield break;
            CheckedThisRun = true;
            var manifest = Resources.Load<HplShaderWarmupManifest>(HplShaderWarmupManifest.ResourceName);
            if (manifest == null || manifest.collection == null || manifest.collection.variantCount == 0)
            {
                Debug.LogWarning("[ShaderWarmup] No raster collection in this build; startup warmup skipped.");
                yield break;
            }
            string driver = HplVideoMemory.DriverVersion;
            string device = $"{SystemInfo.graphicsDeviceVendorID}:{SystemInfo.graphicsDeviceID}:{SystemInfo.graphicsDeviceName}";
            string fingerprint = HplShaderWarmupCache.Fingerprint(manifest.revision, driver, device,
                SystemInfo.graphicsDeviceType + ":" + SystemInfo.graphicsDeviceVersion, Application.unityVersion);
            string path = Path.Combine(Application.persistentDataPath, "ShaderCache", "raster-warmup.json");
            bool force = Array.IndexOf(Environment.GetCommandLineArgs(), "-rebuildShaders") >= 0;
            // A missing driver identity cannot safely authorize skipping after a driver update.
            if (!force && driver.Length > 0 && HplShaderWarmupCache.Matches(path, fingerprint))
            {
                Debug.Log("[ShaderWarmup] Raster shaders already prepared for this driver and revision.");
                yield break;
            }
            var screen = map.gameObject.AddComponent<HplShaderWarmupScreen>();
            // Also invalidate a previous successful receipt before a forced retry of the same fingerprint.
            try { File.Delete(path); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Debug.LogWarning("[ShaderWarmup] Cannot invalidate previous receipt: " + e.Message); }
            screen.Build(map);
            var collection = Instantiate(manifest.collection);
            collection.graphicsDeviceType = SystemInfo.graphicsDeviceType;
            collection.runtimePlatform = Application.platform;
            collection.qualityLevelName = QualitySettings.names[QualitySettings.GetQualityLevel()];
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0;
            Active = true;
            JobHandle job = default;
            int stalledBatches = 0;
            try
            {
                // Present the screen before any potentially expensive native work.
                yield return null;
                yield return null;
                while (!collection.isWarmedUp)
                {
                    int previousCount = collection.completedWarmupCount;
                    Exception failure = null;
                    try { job = collection.WarmUpProgressively(32); JobHandle.ScheduleBatchedJobs(); }
                    catch (Exception e) { failure = e; }
                    if (failure != null)
                    {
                        Debug.LogWarning("[ShaderWarmup] Raster warmup failed: " + failure.Message);
                        break;
                    }
                    while (!job.IsCompleted) { screen.Draw(collection); yield return null; }
                    job.Complete();
                    stalledBatches = collection.completedWarmupCount > previousCount ? 0 : stalledBatches + 1;
                    if (stalledBatches >= 4)
                    {
                        Debug.LogWarning("[ShaderWarmup] Driver made no warmup progress; continuing without a completion receipt.");
                        break;
                    }
                    screen.Draw(collection);
                    yield return null;
                }
                if (collection.isWarmedUp)
                {
                    // Write only after every warmup job has completed. A killed process will retry next time.
                    try { if (driver.Length > 0) HplShaderWarmupCache.Save(path, fingerprint); }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    { Debug.LogWarning("[ShaderWarmup] Cannot save completion receipt: " + e.Message); }
                    Debug.Log($"[ShaderWarmup] Prepared {collection.variantCount} raster variants, {collection.totalGraphicsStateCount} states; driver={driver}.");
                    screen.Draw(collection);
                    yield return null;
                }
            }
            finally
            {
                job.Complete();
                Active = false;
                Time.timeScale = previousTimeScale;
                Destroy(collection);
                if (screen != null) { Destroy(screen.Canvas.gameObject); Destroy(screen); }
            }
        }

        public void Build(HplMap map)
        {
            _russian = map.language == null || map.language.language != "english";
            var root = new GameObject("Shader preparation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            Canvas = root.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 30000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);
            scaler.matchWidthOrHeight = 1;
            var back = Image(root.transform, "Black", Color.black);
            back.rectTransform.anchorMin = Vector2.zero;
            back.rectTransform.anchorMax = Vector2.one;
            back.rectTransform.offsetMin = back.rectTransform.offsetMax = Vector2.zero;
            var area = new GameObject("800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(root.transform, false);
            area.anchorMin = area.anchorMax = area.pivot = Vector2.one * 0.5f;
            area.sizeDelta = new Vector2(800, 600);
            var bar = Image(area, "Progress track", new Color(0.13f, 0.12f, 0.11f));
            Place(bar.rectTransform, new Rect(170, 316, 460, 3));
            _fill = Image(area, "Progress", new Color(0.7f, 0.62f, 0.46f));
            Place(_fill.rectTransform, new Rect(170, 316, 0, 3));
            var text = new GameObject("Text", typeof(RectTransform));
            text.transform.SetParent(area, false);
            Place(text.GetComponent<RectTransform>(), new Rect(0, 0, 800, 600));
            _text = text.AddComponent<HplGuiText>();
            var menu = Resources.Load<HplMenuAssets>(HplMenuAssets.ResourceName);
            _text.font = menu != null ? menu.guiFont : map.hud.defaultFont;
            _text.raycastTarget = false;
            Draw(null);
        }

        public void Draw(GraphicsStateCollection collection)
        {
            int total = collection != null ? collection.totalGraphicsStateCount : 0;
            int done = collection != null ? collection.completedWarmupCount : 0;
            float progress = total > 0 ? Mathf.Clamp01((float)done / total) : 0;
            DrawProgress(progress);
        }

        public void DrawProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            _fill.rectTransform.sizeDelta = new Vector2(460 * progress, 3);
            _text.Clear();
            _text.DrawFont(_russian ? "Компиляция шейдеров" : "Compiling shaders", new Vector2(400, 268), Vector2.one * 24, Color.white, HplFontAlign.Center);
            _text.DrawFont($"{Mathf.FloorToInt(progress * 100)}%", new Vector2(400, 337), Vector2.one * 18, new Color(0.8f, 0.78f, 0.74f), HplFontAlign.Center);
            _text.DrawFont(_russian ? "Подготовка графики перед запуском игры." : "Preparing graphics before starting the game.", new Vector2(400, 393), Vector2.one * 16, Color.gray, HplFontAlign.Center);
            _text.DrawFont(_russian ? "После обновления видеодрайвера подготовка потребуется снова." : "Preparation is needed again after a graphics driver update.", new Vector2(400, 416), Vector2.one * 14, Color.gray, HplFontAlign.Center);
        }

        static RawImage Image(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Place(RectTransform rect, Rect r)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(r.x, -r.y);
            rect.sizeDelta = r.size;
        }
    }
}
