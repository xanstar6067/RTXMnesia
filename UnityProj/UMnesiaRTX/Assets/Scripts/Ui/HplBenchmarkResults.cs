using System;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>Transient results view on the benchmark canvas. No preferences or files.</summary>
    public sealed class HplBenchmarkResults : IDisposable
    {
        public enum Choice { None, Repeat, Back }
        static readonly Color Gold = new(232f / 255, 201f / 255, 28f / 255);
        static readonly Rect RepeatButton = new(205, 564, 180, 30), BackButton = new(415, 564, 180, 30);
        readonly GameObject _root;
        readonly HplGuiText _text;
        readonly bool _english;
        readonly Texture2D _graph;
        readonly RawImage _repeat, _back;
        readonly HplBenchmarkStats.Metrics _total;
        readonly HplBenchmarkStats.Metrics[] _scenes;
        readonly string[] _system;
        readonly HplUserSettings _settings;
        readonly int _dropped;
        readonly double[] _boundaries;
        readonly double _scale;
        int _selected;

        public static string SceneTitle(int index, bool english) => !english ? HplBenchmarkRoute.All[index].title : index switch
        {
            0 => "Fountain hall", 1 => "Old archives: stove", 2 => "Archives", 3 => "Machine room", 4 => "Cistern", _ => "Choir"
        };
        string T(string ru, string en) => _english ? en : ru;
        string Number(double n) => HplBenchmarkStats.Valid(n) ? n.ToString("0.0") : T("н/д", "n/a");

        /// <summary>Capture while the last benchmark camera still exists (DLSS render size is camera-owned).</summary>
        public static string[] CaptureSystem(HplUserSettings settings, bool english, Camera camera)
        {
            string T(string ru, string en) => english ? en : ru;
            bool dx12 = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12;
            string size = camera != null ? camera.pixelWidth + " x " + camera.pixelHeight : Screen.width + " x " + Screen.height;
            string render = settings.dlssMode == 0 ? size : T("н/д", "n/a");
            if (HplDlss.RenderSize(out var r, out var o)) { render = r.x + " x " + r.y; size = o.x + " x " + o.y; }
            string dlss = ((HplDlssMode)settings.dlssMode) switch
            {
                HplDlssMode.Off => T("выкл", "off"), HplDlssMode.Dlaa => "DLAA",
                HplDlssMode.Quality => T("Качество", "Quality"), HplDlssMode.Balanced => T("Баланс", "Balanced"),
                HplDlssMode.Performance => T("Производительность", "Performance"), _ => T("Макс. производительность", "Ultra Performance")
            };
            string Level(int n) => n switch { 0 => T("выкл", "off"), 1 => T("низкое", "low"), 2 => T("среднее", "medium"), 3 => T("высокое", "high"), _ => T("ультра", "ultra") };
            bool rt = settings.lighting != 0 && dx12 && HplRayTracing.Supported;
            string Rt(int n) => Level(rt ? n : 0);
            return new[]
            {
                SystemInfo.graphicsDeviceName + " | " + SystemInfo.graphicsDeviceVersion,
                SystemInfo.processorType + " | " + (SystemInfo.systemMemorySize / 1024f).ToString("0.0") + T(" ГБ ОЗУ | ", " GB RAM | ") + SystemInfo.operatingSystem,
                (dx12 ? "DX12" : SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ? "DX11" : SystemInfo.graphicsDeviceType.ToString())
                    + T(" | Вывод: ", " | Output: ") + size + T(" | Рендер: ", " | Render: ") + render + " | DLSS: " + dlss,
                T("Освещение: ", "Lighting: ") + (settings.lightingSet == 0 ? T("классическое", "classic") : T("улучшенное", "enhanced"))
                    + T(" | Текстуры: ", " | Textures: ") + (settings.textures == 0 ? T("классические", "classic") : T("улучшенные", "enhanced"))
                    + " | RT: " + T("тени ", "shadows ") + Rt(settings.shadows == 0 ? 0 : settings.rtShadows) + ", AO " + Rt(settings.rtAo)
                    + T(", отражения ", ", reflections ") + Rt(settings.rtReflections) + ", GI " + Rt(settings.rtGi)
                    + (settings.rtPathTracing > 0 && rt ? " | PT: " + HplRayTracing.PathTracingSamples[settings.rtPathTracing] : "")
            };
        }

        public HplBenchmarkResults(Canvas canvas, HplFont font, HplBenchmarkStats stats, HplUserSettings settings, bool english, string[] system)
        {
            _english = english; _settings = settings; _system = system; _dropped = stats.DroppedFrames;
            _total = stats.Calculate();
            _scenes = new HplBenchmarkStats.Metrics[HplBenchmarkRoute.All.Length];
            _boundaries = new double[_scenes.Length + 1];
            for (int i = 0; i < _scenes.Length; i++) { _scenes[i] = stats.Calculate(i); _boundaries[i + 1] = _boundaries[i] + _scenes[i].Seconds; }
            _root = new GameObject("Benchmark results", typeof(RectTransform));
            _root.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)_root.transform; rect.sizeDelta = new Vector2(800, 600);
            Image("Black background", new Rect(0, 0, 800, 600), Color.black);
            _scale = Math.Max(20, _total.P999Ms * 1.2);
            _graph = Graph(stats, _total.Seconds, _scale, _boundaries);
            Image("Frame time min-max graph", new Rect(55, 132, 700, 180), Color.white).texture = _graph;
            _repeat = Image("Repeat button", RepeatButton, Color.black);
            _back = Image("Back button", BackButton, Color.black);
            var go = new GameObject("Results text", typeof(RectTransform), typeof(HplGuiText));
            go.transform.SetParent(_root.transform, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 600);
            _text = go.GetComponent<HplGuiText>(); _text.font = font; _text.raycastTarget = false;
            Draw();
        }

        RawImage Image(string name, Rect rect, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage)); go.transform.SetParent(_root.transform, false);
            var image = go.GetComponent<RawImage>(); image.raycastTarget = false; image.texture = Texture2D.whiteTexture; image.color = color;
            image.rectTransform.sizeDelta = rect.size;
            image.rectTransform.anchoredPosition = new Vector2(rect.center.x - 400, 300 - rect.center.y);
            return image;
        }

        public Choice UpdateInput()
        {
            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.escapeKey.wasPressedThisFrame) return Choice.Back;
                if (keys.tabKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame)
                { _selected = 1 - _selected; Draw(); }
                if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame)
                    return _selected == 0 ? Choice.Repeat : Choice.Back;
            }
            var mouse = Mouse.current;
            if (mouse != null && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_root.transform, mouse.position.ReadValue(), null, out var p))
            {
                p = new Vector2(p.x + 400, 300 - p.y);
                int hover = RepeatButton.Contains(p) ? 0 : BackButton.Contains(p) ? 1 : -1;
                if (hover >= 0 && mouse.delta.ReadValue().sqrMagnitude > 0 && _selected != hover) { _selected = hover; Draw(); }
                if (hover >= 0 && mouse.leftButton.wasPressedThisFrame) return hover == 0 ? Choice.Repeat : Choice.Back;
            }
            return Choice.None;
        }

        void Label(string text, float x, float y, float size = 13, Color? color = null, HplFontAlign align = HplFontAlign.Left)
        {
            // Device and driver strings can be arbitrarily long. Fit each line inside the virtual screen.
            if (_text.font != null)
            {
                float width = _text.font.GetLength(Vector2.one * size, text);
                float available = align == HplFontAlign.Center ? 730 : align == HplFontAlign.Right ? x - 30 : 770 - x;
                if (width > available) size *= available / width;
            }
            _text.DrawFont(text, new Vector2(x, y), Vector2.one * size, color ?? Color.white, align);
        }

        void Draw()
        {
            _text.Clear();
            Label(T("Результаты теста производительности", "Performance test results"), 400, 10, 22, Gold, HplFontAlign.Center);
            Label(T("Средний", "Average") + " " + Number(_total.AverageFps) + " FPS     1% low: " + Number(_total.Low1Fps)
                + "     0.1% low: " + Number(_total.Low01Fps), 400, 42, 25, null, HplFontAlign.Center);
            Label(T("Медиана / P99: ", "Median / P99: ") + Number(_total.MedianMs) + " / " + Number(_total.P99Ms)
                + T(" мс, рывков: ", " ms, stutters: ") + _total.Stutters, 400, 76, 15, null, HplFontAlign.Center);
            string limit = _total.Bottleneck switch
            {
                HplBenchmarkStats.Limit.Gpu => T("видеокарта", "GPU"), HplBenchmarkStats.Limit.Cpu => T("процессор", "CPU"),
                HplBenchmarkStats.Limit.Balanced => T("примерно поровну", "balanced"), _ => T("н/д", "n/a")
            };
            Label(T("Ограничение: ", "Bottleneck: ") + limit + " | P95: " + Number(_total.P95Ms) + T(" мс | Макс.: ", " ms | Max: ")
                + Number(_total.MaxMs) + T(" мс", " ms"), 400, 97, 13, null, HplFontAlign.Center);
            Label(T("Время кадра, мс: min / max", "Frame time, ms: min / max"), 55, 115, 12);
            Label("0 - " + _total.Seconds.ToString("0.0") + T(" с", " s"), 755, 115, 12, null, HplFontAlign.Right);
            Label("0", 50, 301, 11, null, HplFontAlign.Right);
            Label(_scale.ToString("0.0"), 50, 132, 11, null, HplFontAlign.Right);
            foreach (int fps in new[] { 60, 30 })
                if (1000.0 / fps < _scale)
                    // At a large Y scale the two reference lines are only a few pixels apart.
                    // Separate their labels horizontally so neither hides the other.
                    Label(fps + " FPS", fps == 60 ? 690 : 751, (float)(312 - 180 * 1000.0 / fps / _scale) - 12, 11, Gold, HplFontAlign.Right);
            for (int i = 0; i < _scenes.Length; i++)
                Label((i + 1).ToString(), (float)(55 + 700 * (_boundaries[i] + _boundaries[i + 1]) / 2 / Math.Max(_total.Seconds, 0.001)), 315, 12, Gold, HplFontAlign.Center);
            Label(T("Сцена", "Scene"), 40, 337, 13, Gold);
            Label(T("Ср. FPS", "Avg FPS"), 425, 337, 13, Gold, HplFontAlign.Right);
            Label("1% low", 535, 337, 13, Gold, HplFontAlign.Right);
            Label(T("P99 мс", "P99 ms"), 645, 337, 13, Gold, HplFontAlign.Right);
            Label(T("Рывки", "Stutters"), 755, 337, 13, Gold, HplFontAlign.Right);
            for (int i = 0; i < _scenes.Length; i++)
            {
                float y = 357 + i * 16; var m = _scenes[i];
                Label((i + 1) + ": " + SceneTitle(i, _english), 40, y);
                Label(Number(m.AverageFps), 425, y, 13, null, HplFontAlign.Right);
                Label(Number(m.Low1Fps), 535, y, 13, null, HplFontAlign.Right);
                Label(Number(m.P99Ms), 645, y, 13, null, HplFontAlign.Right);
                Label(m.Stutters.ToString(), 755, y, 13, null, HplFontAlign.Right);
            }
            for (int i = 0; i < _system.Length; i++) Label(_system[i], 40, 458 + i * 15, 11);
            string warning = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ? T("DX11: трассировка недоступна. ", "DX11: ray tracing unavailable. ") : "";
            if (_settings.vSync) warning += T("Включена вертикальная синхронизация: возможен потолок FPS. ", "VSync enabled: FPS may be capped. ");
            else if (_settings.fpsLimit > 0) warning += T("Ограничение FPS: ", "FPS limit: ") + _settings.fpsLimit + T(". Возможен потолок FPS. ", ". Results may be capped. ");
            if (_dropped > 0) warning += T("Буфер заполнен, пропущено кадров: ", "Buffer full, frames omitted: ") + _dropped;
            Label(warning, 40, 522, 11, Gold);
            Label(T("Кадры генерации драйвера (Smooth Motion) не учитываются.", "Driver-generated frames (Smooth Motion) are not counted."), 40, 539, 12);
            _repeat.color = _selected == 0 ? new Color(0.22f, 0.18f, 0.1f) : new Color(0.09f, 0.08f, 0.06f);
            _back.color = _selected == 1 ? new Color(0.22f, 0.18f, 0.1f) : new Color(0.09f, 0.08f, 0.06f);
            Label(T("Повторить (Enter)", "Repeat (Enter)"), 295, 568, 18, _selected == 0 ? Gold : Color.white, HplFontAlign.Center);
            Label(T("Назад (Esc)", "Back (Esc)"), 505, 568, 18, _selected == 1 ? Gold : Color.white, HplFontAlign.Center);
        }

        static Texture2D Graph(HplBenchmarkStats stats, double seconds, double scale, double[] boundaries)
        {
            const int w = 700, h = 180;
            var pixels = new Color32[w * h];
            var min = new double[w]; var max = new double[w];
            for (int x = 0; x < w; x++) min[x] = double.PositiveInfinity;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(18, 20, 22, 255);
            int Y(double ms) => Mathf.Clamp((int)(ms / scale * (h - 1)), 0, h - 1);
            double elapsed = 0;
            for (int i = 0; i < stats.Count; i++)
            {
                var f = stats[i];
                int a = Mathf.Clamp((int)(elapsed / Math.Max(seconds, 0.001) * w), 0, w - 1);
                elapsed += f.seconds;
                int b = Mathf.Clamp((int)Math.Ceiling(elapsed / Math.Max(seconds, 0.001) * w) - 1, a, w - 1);
                for (int x = a; x <= b; x++) { min[x] = Math.Min(min[x], f.seconds * 1000); max[x] = Math.Max(max[x], f.seconds * 1000); }
            }
            foreach (double ms in new[] { 1000.0 / 60, 1000.0 / 30 })
                if (ms < scale) for (int x = 0; x < w; x++) pixels[Y(ms) * w + x] = new Color32(124, 107, 34, 255);
            for (int i = 1; i < boundaries.Length - 1; i++)
            {
                int x = Mathf.Clamp((int)(boundaries[i] / Math.Max(seconds, 0.001) * w), 0, w - 1);
                for (int y = 0; y < h; y++) pixels[y * w + x] = new Color32(83, 79, 68, 255);
            }
            for (int x = 0; x < w; x++)
                if (max[x] > 0)
                {
                    for (int y = Y(min[x]); y <= Y(max[x]); y++) pixels[y * w + x] = new Color32(206, 196, 157, 255);
                    if (max[x] > scale) pixels[(h - 1) * w + x] = new Color32(255, 100, 70, 255);
                }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Benchmark frame times", filterMode = FilterMode.Point };
            texture.SetPixels32(pixels); texture.Apply();
            return texture;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_graph);
            UnityEngine.Object.Destroy(_root);
        }
    }
}
