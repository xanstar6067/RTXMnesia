using System.Collections.Generic;
using System.Globalization;
using RTXMnesia.Hpl;
using UnityEngine;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// The options window: seven tabs (game, display, graphics, image, ray tracing, sound, input) whose contents scroll
    /// in a clipped area. The content is laid out in two columns of cells with section headers; items outside the area
    /// are not drawn but stay in the button list (keyboard navigation scrolls to them).
    /// </summary>
    public sealed partial class HplPauseMenu
    {
        /// <summary>The tabs of the options window.</summary>
        public const int OptionsTabs = 7;
        public const int GameTabIndex = 0, DisplayTabIndex = 1, GraphicsTabIndex = 2, ImageTabIndex = 3, RayTabIndex = 4, SoundTabIndex = 5, InputTabIndex = 6;

        static readonly Rect OptionsWindow = new(60, 70, 680, 460);
        /// <summary>The scrolling area under the tabs (virtual 800 × 600 pixels).</summary>
        static readonly Rect Viewport = new(84, 158, 612, 318);
        const float ColX0 = 100, ColX1 = 400, ColW = 250, FullW = 550, RowGap = 6, ScrollStep = 40;
        static readonly Color Grey = new(0.5f, 0.5f, 0.5f, 1);

        RectTransform _scrollRoot;
        HplGuiText _scrollText;
        readonly List<RawImage> _scrollPieces = new();
        int _scrollPieceIndex;
        /// <summary>Drawing into the scrolling area; the item is outside it (nothing is drawn, the button stays).</summary>
        bool _inScroll, _skipDraw;
        float _scroll, _contentHeight, _lay, _rowH;
        int _col;
        /// <summary>The button index → its full rectangle (not clipped to the area), buttons of the scrolling area only.</summary>
        readonly Dictionary<int, Rect> _scrollFull = new();
        readonly HashSet<int> _disabledButtons = new(), _noNavButtons = new();
        Rect _scrollTrack, _scrollThumb;

        public Rect ViewportRect => Viewport;
        public float ScrollMax => Mathf.Max(0, _contentHeight - Viewport.height);
        public float ScrollStepSize => ScrollStep;
        public bool IsDisabledAt(int index) => _disabledButtons.Contains(index);
        /// <summary>Keyboard navigation skips disabled buttons and the scroll bar arrows.</summary>
        public bool SkipNavAt(int index) => _disabledButtons.Contains(index) || _noNavButtons.Contains(index);
        public bool TryGetScrollRect(int index, out Rect rect) => _scrollFull.TryGetValue(index, out rect);

        public bool TryScrollTrack(Vector2 position) => _scrollTrack.width > 0 && _scrollTrack.Contains(VirtualPoint(position));

        /// <summary>The scroll position (0..1) for the pointer on the scroll bar, the thumb centered on it.</summary>
        public float ScrollTrackValue(Vector2 position)
        {
            float range = _scrollTrack.height - _scrollThumb.height;
            return range <= 0 ? 0 : Mathf.Clamp01((VirtualPoint(position).y - _scrollTrack.y - _scrollThumb.height / 2) / range);
        }

        void CreateScrollLayer(HplFont font)
        {
            var go = new GameObject("Options scroll area", typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(_screen, false);
            _scrollRoot = go.GetComponent<RectTransform>();
            Place(_scrollRoot, Viewport);
            var textGo = new GameObject("Options scroll text", typeof(RectTransform));
            textGo.transform.SetParent(_scrollRoot, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _scrollText = textGo.AddComponent<HplGuiText>();
            _scrollText.font = font;
            _scrollText.raycastTarget = false;
        }

        void ScrollPiece(string type, Rect rect, Color color)
        {
            if (_scrollPieceIndex == _scrollPieces.Count)
            {
                var created = Image(_scrollRoot, "Options scroll piece");
                created.transform.SetSiblingIndex(_scrollText.transform.GetSiblingIndex());
                _scrollPieces.Add(created);
            }
            var target = _scrollPieces[_scrollPieceIndex++];
            target.uvRect = new Rect(0, 0, 1, 1);
            var piece = type != null && _assets != null ? _assets.Find(type) : default;
            target.texture = piece.texture != null ? piece.texture : Texture2D.whiteTexture;
            target.color = (piece.texture != null ? piece.color * color : color) * new Color(1, 1, 1, _opacity);
            target.enabled = true;
            Place(target.rectTransform, new Rect(rect.position - Viewport.position, rect.size));
        }

        static Rect ClipToViewport(Rect r)
        {
            float x0 = Mathf.Max(r.x, Viewport.x), y0 = Mathf.Max(r.y, Viewport.y), x1 = Mathf.Min(r.xMax, Viewport.xMax), y1 = Mathf.Min(r.yMax, Viewport.yMax);
            return x1 > x0 && y1 > y0 ? Rect.MinMaxRect(x0, y0, x1, y1) : Rect.zero;
        }

        /// <summary>Registers a button: a disabled one gets an empty rectangle, one in the scrolling area is clipped to it.</summary>
        int AddButton(Rect rect, MenuControl action, int data = -1)
        {
            int index = _buttons.Count;
            _buttons.Add((_disabled ? Rect.zero : _inScroll ? ClipToViewport(rect) : rect, action));
            _buttonData.Add(data);
            if (_inScroll) _scrollFull[index] = rect;
            if (_disabled) _disabledButtons.Add(index);
            return index;
        }

        /// <summary>Gives the last button another (larger) pressable rectangle.</summary>
        void SetLastHit(Rect rect)
        {
            int index = _buttons.Count - 1;
            _buttons[index] = (_disabled ? Rect.zero : _inScroll ? ClipToViewport(rect) : rect, _buttons[index].control);
            if (_inScroll) _scrollFull[index] = rect;
        }

        // ------------------------------------------------------------------ layout of the scrolling area

        float Top(float contentY) => Viewport.y + 8 + contentY - _scroll;

        void BeginScroll()
        {
            _inScroll = true;
            _lay = _rowH = 0;
            _col = 0;
            _skipDraw = false;
        }

        void EndScroll()
        {
            NextRow();
            _contentHeight = _lay + 16;
            _inScroll = _skipDraw = false;
        }

        void NextRow()
        {
            if (_col == 0 && _rowH == 0) return;
            _lay += _rowH + RowGap;
            _rowH = 0;
            _col = 0;
        }

        /// <summary>The next cell of the two columns (top left corner on screen); a second cell completes the row.</summary>
        Vector2 Cell(float height)
        {
            float x = _col == 0 ? ColX0 : ColX1, y = Top(_lay);
            _rowH = Mathf.Max(_rowH, height);
            _skipDraw = y + height < Viewport.y || y > Viewport.yMax;
            if (++_col == 2) NextRow();
            return new Vector2(x, y);
        }

        /// <summary>A row of the full width.</summary>
        Vector2 FullRow(float height)
        {
            NextRow();
            float y = Top(_lay);
            _rowH = height;
            _skipDraw = y + height < Viewport.y || y > Viewport.yMax;
            NextRow();
            return new Vector2(ColX0, y);
        }

        void Section(string title)
        {
            NextRow();
            if (_lay > 0) _lay += 6;
            var p = FullRow(30);
            Draw(_guiText, title, p, Vector2.one * 18, Color.white);
            Piece(null, new Rect(p.x, p.y + 24, FullW, 1), new Color(1, 1, 1, 0.3f));
        }

        List<string> Wrap(string text, float width, Vector2 size)
        {
            var rows = new List<string>();
            foreach (string line in text.Split('\n'))
            {
                if (_guiText.font != null) rows.AddRange(_guiText.font.GetWordWrapRows(width, size, line));
                else rows.Add(line);
            }
            return rows;
        }

        void Note(string text, bool dim = true)
        {
            var size = Vector2.one * 15;
            var rows = Wrap(text, FullW, size);
            var p = FullRow(rows.Count * 18 + 4);
            for (int i = 0; i < rows.Count; i++) Draw(_guiText, rows[i], new Vector2(p.x, p.y + i * 18), size, dim ? Grey : Color.white);
        }

        void ScrollCombo(MenuControl control, HplUserSettings settings, bool disabled = false)
        {
            var p = Cell(45);
            _disabled = disabled;
            Combo(p.x, p.y, control, settings);
            _disabled = false;
        }

        /// <summary>A line of information in a cell: a label and its text under it.</summary>
        void ScrollInfo(string label, string value)
        {
            var p = Cell(45);
            Draw(_guiText, label, p, Vector2.one * 16, Color.white);
            Draw(_guiText, value, p + new Vector2(0, 21), Vector2.one * 16, Grey);
        }

        /// <summary>A check box cell; <paramref name="alignWithCombo"/> puts it at the height of a neighbouring list's box.</summary>
        void ScrollToggle(string label, bool value, MenuControl control, bool disabled = false, bool alignWithCombo = false)
        {
            var p = Cell(alignWithCombo ? 45 : 28);
            if (alignWithCombo) p.y += 21;
            _disabled = disabled;
            Piece(value ? "CheckBoxEnabledChecked" : "CheckBoxEnabledUnchecked", new Rect(p.x, p.y + 1, 16, 16), disabled ? Grey : Color.white);
            Button(new Rect(p.x + 24, p.y, ColW - 24, 20), label, control, null, Vector2.one * 16, HplFontAlign.Left);
            SetLastHit(new Rect(p.x - 4, p.y - 2, ColW + 8, 24));
            _disabled = false;
        }

        void ScrollAdjust(string label, string value, float fraction, MenuControl less, MenuControl more, MenuControl slider, bool disabled = false)
        {
            var p = Cell(48);
            _disabled = disabled;
            SmallAdjust(p.x, p.y, label, value, fraction, less, more, slider, ColW);
            _disabled = false;
        }

        void ScrollButton(string label, MenuControl control, bool disabled = false)
        {
            var p = Cell(30);
            _disabled = disabled;
            SkinButton(new Rect(p.x, p.y, ColW, 28), label, control);
            _disabled = false;
        }

        /// <summary>The scroll bar (the skin's vertical slider) right of the area; none when everything fits.</summary>
        void ScrollBar()
        {
            _scrollTrack = _scrollThumb = Rect.zero;
            if (ScrollMax <= 0.5f) return;
            var up = new Rect(Viewport.xMax + 2, Viewport.y, 18, 18);
            var down = new Rect(up.x, Viewport.yMax - 18, 18, 18);
            _scrollTrack = new Rect(up.x, up.yMax, 18, down.y - up.yMax);
            float height = Mathf.Max(24, _scrollTrack.height * Viewport.height / _contentHeight);
            float t = Mathf.Clamp01(_scroll / ScrollMax);
            _scrollThumb = new Rect(up.x + 3, _scrollTrack.y + t * (_scrollTrack.height - height), 12, height);
            Piece("SliderVertBackground", _scrollTrack, Color.white);
            Piece(null, _scrollThumb, new Color(1, 1, 1, 0.6f));
            Piece("SliderVertArrowUp", up, Color.white);
            Piece("SliderVertArrowDown", down, Color.white);
            _noNavButtons.Add(AddButton(up, MenuControl.ScrollUp));
            _noNavButtons.Add(AddButton(down, MenuControl.ScrollDown));
        }

        // ------------------------------------------------------------------ the window

        string OwnText(string ru, string en) => HdrText(ru, en);

        void Options(int tab, HplUserSettings settings)
        {
            Window(OptionsWindow);
            Draw(_guiText, T("MainMenu", "Options", "Настройки"), new Vector2(95, 50), Vector2.one * 25, Color.white);
            string[] keys = { "OptionsGame", null, "OptionsGraphics", null, null, "OptionsSound", "OptionsInput" };
            string[] names = { "Игра", OwnText("Дисплей", "Display"), "Графика", OwnText("Изображение", "Image"), OwnText("Трассировка", "Ray tracing"), "Звук", "Управление" };
            for (int i = 0; i < OptionsTabs; i++)
            {
                var rect = new Rect(72 + i * 94, 117, 90, 28);
                string label = keys[i] != null ? T("MainMenu", keys[i], names[i]) : names[i];
                // A long name gets a smaller font instead of running into the next tab.
                float fontSize = 16;
                float length = _guiText.font != null ? _guiText.font.GetLength(Vector2.one * fontSize, label) : 0;
                if (length > rect.width - 4) fontSize = Mathf.Floor(fontSize * (rect.width - 4) / length);
                Button(rect, label, (MenuControl)((int)MenuControl.GameTab + i), null, Vector2.one * fontSize);
                if (i == tab) Piece(null, new Rect(rect.x, rect.yMax + 3, rect.width, 1), new Color(1, 1, 1, 0.7f));
            }
            BeginScroll();
            switch (tab)
            {
                case GameTabIndex: GameTabItems(settings); break;
                case DisplayTabIndex: DisplayTabItems(settings); break;
                case GraphicsTabIndex: GraphicsTabItems(settings); break;
                case ImageTabIndex: ImageTabItems(settings); break;
                case RayTabIndex: RayTabItems(settings); break;
                case SoundTabIndex: SoundTabItems(settings); break;
                default: InputTabItems(settings); break;
            }
            EndScroll();
            ScrollBar();
            SkinButton(new Rect(550, 485, 80, 30), T("MainMenu", "OK", "OK"), MenuControl.Apply);
            SkinButton(new Rect(640, 485, 95, 30), T("MainMenu", "Cancel", "Отмена"), MenuControl.Cancel);
        }

        void GameTabItems(HplUserSettings settings)
        {
            ScrollToggle(T("OptionsMenu", "ShowSubtitles", "Субтитры"), settings.subtitles, MenuControl.Subtitles);
            ScrollToggle(T("OptionsMenu", "ShowHints", "Подсказки"), settings.hints, MenuControl.Hints);
            ScrollToggle(T("OptionsMenu", "ShowCrosshair", "Прицел"), settings.crosshair, MenuControl.Crosshair);
            // menu.cfg Options ShowCommentary shows the developer commentary option.
            if (_map.menuConfig == null || _map.menuConfig.GetString("Options", "ShowCommentary", "false") == "true")
                ScrollToggle(T("OptionsMenu", "Commentary", "Комментарии"), settings.commentary, MenuControl.Commentary);
        }

        void DisplayTabItems(HplUserSettings settings)
        {
            Section(OwnText("Экран", "Screen"));
            ScrollCombo(MenuControl.Resolution, settings);
            ScrollCombo(MenuControl.ScreenMode, settings);
            // The frame rate cap does nothing with VSync on.
            ScrollCombo(MenuControl.FpsLimit, settings, settings.vSync);
            ScrollToggle(T("OptionsMenu", "VSync", "Вертикальная синхронизация"), settings.vSync, MenuControl.VSync, alignWithCombo: true);
            // The SDR gamma is a separate setting from the HDR brightness: always editable, but it only applies to SDR output.
            string gammaLabel = T("OptionsMenu", "Gamma", "Гамма");
            if (HplHdr.Active) gammaLabel += HdrText(" (только SDR)", " (SDR only)");
            ScrollAdjust(gammaLabel, settings.gamma.ToString("0.00", CultureInfo.InvariantCulture),
                (settings.gamma - HplUserSettings.MinGamma) / (HplUserSettings.MaxGamma - HplUserSettings.MinGamma),
                MenuControl.GammaLess, MenuControl.GammaMore, MenuControl.GammaSlider);

            Section("HDR");
            ScrollCombo(MenuControl.HdrMode, settings);
            ScrollInfo(HdrText("Состояние", "State"), HdrStatus());
            bool off = settings.hdrMode == 0;
            ScrollAdjust(HdrText("Пиковая яркость", "Peak luminance"), settings.hdrPeak.ToString("0") + " nit",
                (settings.hdrPeak - HplHdr.MinPeak) / (HplHdr.MaxPeak - HplHdr.MinPeak), MenuControl.HdrPeakLess, MenuControl.HdrPeakMore, MenuControl.HdrPeakSlider, off);
            ScrollAdjust(HdrText("Яркость белого / UI", "Paper white / UI"), settings.hdrPaperWhite.ToString("0") + " nit",
                (settings.hdrPaperWhite - HplHdr.MinWhite) / (HplHdr.MaxWhite - HplHdr.MinWhite), MenuControl.HdrWhiteLess, MenuControl.HdrWhiteMore, MenuControl.HdrWhiteSlider, off);
            ScrollAdjust(HdrText("Уровень чёрного", "Black level"), settings.hdrBlack.ToString("0.000", CultureInfo.InvariantCulture) + " nit",
                settings.hdrBlack / HplHdr.MaxBlack, MenuControl.HdrBlackLess, MenuControl.HdrBlackMore, MenuControl.HdrBlackSlider, off);
            ScrollAdjust(HdrText("Глубина теней", "Shadow depth"), settings.hdrShadows.ToString("0.00", CultureInfo.InvariantCulture),
                (settings.hdrShadows - HplHdr.MinShadows) / (HplHdr.MaxShadows - HplHdr.MinShadows), MenuControl.HdrShadowsLess, MenuControl.HdrShadowsMore, MenuControl.HdrShadowsSlider, off);
            ScrollButton(HdrText("Данные дисплея", "Use display values"), MenuControl.HdrDetect, off);
            ScrollButton(HdrText("По умолчанию", "Defaults"), MenuControl.HdrDefaults);
            ScrollButton(HdrText("Калибровка HDR…", "HDR calibration…"), MenuControl.HdrCalibrate, off);
        }

        string HdrStatus() => HplHdr.Pending ? HdrText("Переключение…", "Switching…") : HplHdr.Active
            ? HdrText("HDR активен", "HDR active") : HplHdr.Available ? HdrText("Вывод SDR", "SDR output") : HdrText("HDR недоступен", "HDR unavailable");

        void GraphicsTabItems(HplUserSettings settings)
        {
            Section("Освещение");
            ScrollCombo(MenuControl.Lighting, settings);
            Note("«Улучшенное» освещение переставляет источники света карт (пока готовы не все карты). Трассировка лучей включается на вкладке «Трассировка» и работает поверх любого освещения.");
            Section("Текстуры");
            ScrollCombo(MenuControl.Textures, settings);
            ScrollCombo(MenuControl.TextureQuality, settings);
            ScrollCombo(MenuControl.TextureMemoryBudget, settings);
            ScrollCombo(MenuControl.Anisotropy, settings);
            Section("Тени и эффекты");
            ScrollCombo(MenuControl.Shadows, settings);
            // Own text: the game's Russian "Bloom" entry reads "Потускнение" (dimming), the opposite of the effect.
            ScrollToggle("Свечение (Bloom)", settings.bloom, MenuControl.Bloom, alignWithCombo: true);
            ScrollToggle(T("OptionsMenu", "ImageTrail", "След при движении"), settings.imageTrail, MenuControl.ImageTrail);
            ScrollToggle(T("OptionsMenu", "RadialBlur", "Радиальное размытие"), settings.radialBlur, MenuControl.RadialBlur);
        }

        void ImageTabItems(HplUserSettings settings)
        {
            Section("Сглаживание и масштабирование");
            ScrollCombo(MenuControl.ImageMethod, settings);
            ScrollCombo(MenuControl.DlssPreset, settings, !HplGraphicsOptions.DlssOn(settings));
            string info;
            if (!HplDlss.Available) info = "DLSS недоступен: нужна видеокарта NVIDIA RTX и её драйвер.";
            else if (!HplGraphicsOptions.DlssOn(settings)) info = "DLSS и DLAA выбираются в списке «Метод».";
            else if (HplDlss.Active && HplDlss.Mode == (HplDlssMode)settings.dlssMode && HplDlss.RenderSize(out var render, out var output))
                info = "Рендер " + render.x + " x " + render.y + " / Вывод " + output.x + " x " + output.y;
            else info = "Применится после «OK».";
            Note(info);
            Section("Генерация кадров");
            Note(HplDlss.Available ? FrameGenerationNote : "Генерация кадров доступна на видеокартах NVIDIA RTX 40 и 50.");
        }

        void RayTabItems(HplUserSettings settings)
        {
            bool supported = SystemInfo.supportsRayTracing;
            bool on = supported && settings.lighting == 1;
            Section("Трассировка лучей");
            ScrollToggle("Трассировка лучей", on, MenuControl.RtEnabled, !supported);
            Note(!supported ? "Недоступна: нужны DirectX 12 и видеокарта с трассировкой лучей."
                : on ? "Работает поверх выбранного освещения." : "Выключена. Включите, чтобы открыть настройки ниже.");
            Section("Тени");
            ScrollCombo(MenuControl.RtShadows, settings, !on);
            ScrollCombo(MenuControl.RtAllShadows, settings, !on || settings.rtShadows == 0);
            ScrollCombo(MenuControl.RtLanternShadows, settings, !on);
            Section("Освещение и отражения");
            ScrollCombo(MenuControl.RtAo, settings, !on);
            ScrollCombo(MenuControl.RtReflections, settings, !on);
            ScrollCombo(MenuControl.RtGi, settings, !on);
            Section("Режимы");
            ScrollCombo(MenuControl.RtPathTracing, settings, !on);
            if (on && settings.rtPathTracing > 0) Note(PathTracingNote);
            if (on)
                Note(!HplRayTracing.Applied(settings) ? "Применится после «OK»."
                    : settings.rtShadows == 0 ? "Тени: картами теней."
                    : "С трассировкой: " + HplRayTracing.TracedLights + " из " + HplRayTracing.ShadowedLights + " источников с тенями в кадре" + (HplRayTracing.TracedHandLights > 0 ? " + фонарь." : "."));
            if (supported) Note(CompareHint);
        }

        void SoundTabItems(HplUserSettings settings)
        {
            ScrollAdjust(T("OptionsMenu", "Volume", "Общая громкость"), Mathf.RoundToInt(settings.masterVolume * 100) + "%", settings.masterVolume,
                MenuControl.VolumeLess, MenuControl.VolumeMore, MenuControl.VolumeSlider);
        }

        void InputTabItems(HplUserSettings settings)
        {
            ScrollToggle(T("OptionsMenu", "InvertMouse", "Инверсия мыши"), settings.invertMouse, MenuControl.InvertMouse);
            ScrollToggle(T("OptionsMenu", "SmoothMouse", "Сглаживание мыши"), settings.smoothMouse, MenuControl.SmoothMouse);
            ScrollAdjust(T("OptionsMenu", "MouseSensitivity", "Чувствительность мыши"), settings.mouseSensitivity.ToString("0.0", CultureInfo.InvariantCulture),
                (settings.mouseSensitivity - 0.2f) / 4.8f, MenuControl.SensitivityLess, MenuControl.SensitivityMore, MenuControl.SensitivitySlider);
            // cLuxMainMenu_Options: the key config button under the input settings.
            ScrollButton(T("OptionsMenu", "KeyConfigButton", "Конфигурация клавиш"), MenuControl.KeyConfig);
        }

        /// <summary>What the path tracer of HDRP does not give (HplRayTracing.PathTracingSamples).</summary>
        const string PathTracingNote = "Чисто, пока в кадре всё стоит; без окружающего света и тумана.";
        /// <summary>Frame generation: Unity has none of its own; NVIDIA's driver one is turned on in the NVIDIA App.</summary>
        const string FrameGenerationNote = "NVIDIA Smooth Motion включается в NVIDIA App для UMnesiaRTX.exe (RTX 40 и 50); в игре своей генерации кадров нет.";
        /// <summary>The comparison key (Effects.HplRtxCompare).</summary>
        const string CompareHint = "В игре клавиша C включает и выключает трассировку лучей (сохраняется).";

        // The calibration patch is drawn in the camera buffer, through a hole in the UI background.
        // Putting it in an ordinary UI texture would clamp its luminance to Paper White / Max Nits.
        static readonly Rect HdrPatch = new(420, 224, 250, 150);
        string HdrText(string ru, string en) => _map.language != null &&
            _map.language.language != null && _map.language.language.StartsWith("eng", System.StringComparison.OrdinalIgnoreCase) ? en : ru;

        /// <summary>The HDR calibration window: the patch in the hole of the background, the sliders and the pattern choice.</summary>
        void HdrCalibration(HplUserSettings settings)
        {
            Window(new Rect(90, 70, 620, 460), true);
            Draw(_guiText, HdrText("Калибровка HDR", "HDR calibration"), new Vector2(125, 50), Vector2.one * 25, Color.white);
            Hdr(settings);
            SkinButton(new Rect(605, 485, 95, 30), HdrText("Закрыть", "Close"), MenuControl.HdrCalibrationClose);
        }

        void Hdr(HplUserSettings settings)
        {
            _shade.color = Color.clear;
            Draw(_guiText, HdrStatus(), new Vector2(130, 165), Vector2.one * 17, Color.white);
            SmallAdjust(130, 200, HdrText("Пиковая яркость", "Peak luminance"), settings.hdrPeak.ToString("0") + " nit",
                (settings.hdrPeak - HplHdr.MinPeak) / (HplHdr.MaxPeak - HplHdr.MinPeak), MenuControl.HdrPeakLess, MenuControl.HdrPeakMore, MenuControl.HdrPeakSlider);
            SmallAdjust(130, 256, HdrText("Яркость белого / UI", "Paper white / UI"), settings.hdrPaperWhite.ToString("0") + " nit",
                (settings.hdrPaperWhite - HplHdr.MinWhite) / (HplHdr.MaxWhite - HplHdr.MinWhite), MenuControl.HdrWhiteLess, MenuControl.HdrWhiteMore, MenuControl.HdrWhiteSlider);
            SmallAdjust(130, 312, HdrText("Уровень чёрного", "Black level"), settings.hdrBlack.ToString("0.000", CultureInfo.InvariantCulture) + " nit",
                settings.hdrBlack / HplHdr.MaxBlack, MenuControl.HdrBlackLess, MenuControl.HdrBlackMore, MenuControl.HdrBlackSlider);
            SmallAdjust(130, 368, HdrText("Глубина теней", "Shadow depth"), settings.hdrShadows.ToString("0.00", CultureInfo.InvariantCulture),
                (settings.hdrShadows - HplHdr.MinShadows) / (HplHdr.MaxShadows - HplHdr.MinShadows), MenuControl.HdrShadowsLess, MenuControl.HdrShadowsMore, MenuControl.HdrShadowsSlider);
            Button(new Rect(125, 424, 240, 23), HdrText("Данные дисплея", "Use display values"), MenuControl.HdrDetect, null, Vector2.one * 16);
            Button(new Rect(125, 452, 240, 23), HdrText("По умолчанию", "Defaults"), MenuControl.HdrDefaults, null, Vector2.one * 16);
            string[] names = { HdrText("Пик", "Peak"), HdrText("Белый", "White"), HdrText("Чёрный", "Black") };
            Button(new Rect(410, 194, 270, 23), "< " + names[HplHdr.CalibrationPattern] + " >", MenuControl.HdrPreview, null, Vector2.one * 16);
            string[] help = {
                HdrText("Повышайте пик до исчезновения\nкреста, затем немного уменьшите.\nОтключите динамический тонмап\nдисплея для точной калибровки.", "Raise peak until the cross disappears,\nthen lower it slightly. Disable the\ndisplay's dynamic tone mapping\nfor accurate calibration."),
                HdrText("Белое поле и текст должны быть\nкомфортными в вашем освещении.\nЭта настройка также влияет\nна общую яркость сцены.", "Keep the white tile and text\ncomfortable in your room lighting.\nThis also controls the overall\nbrightness of the scene."),
                HdrText("Начните с нуля. Крест должен\nбыть едва различим на чёрном.\nНа OLED обычно оставляйте 0.\nНе высветляйте тёмные комнаты.", "Start at zero. The cross should be\nbarely visible against black.\nFor OLED, usually leave at zero.\nAvoid lifting dark rooms.") };
            string hint = HplHdr.Active ? help[HplHdr.CalibrationPattern] :
                HdrText("Образец SDR — только иллюстрация.\nВключите HDR в Windows и игре.\nВ редакторе нужен DX12;\nдля DX11 проверяйте сборку.", "SDR illustration, not HDR calibration.\nEnable HDR in Windows and the game.\nEditor HDR requires DX12;\nuse a standalone player for DX11.");
            string[] lines = hint.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                Draw(_guiText, lines[i], new Vector2(420, 388 + i * 18), Vector2.one * 13, Color.white);
            float width = Screen.height > 0 ? Screen.width * 600f / Screen.height : 800;
            HplHdr.CalibrationRect = new Rect((HdrPatch.x + (width - 800) * 0.5f) / width, HdrPatch.y / 600,
                HdrPatch.width / width, HdrPatch.height / 600);
            HplHdr.CalibrationVisible = _root.activeInHierarchy && _opacity > 0.99f;
        }

        /// <summary>
        /// cLuxMainMenu_KeyConfig::CreateGui (620 × 460 window): the category selector, the action / primary /
        /// secondary headers (font × 1.25), a row per action of the category (inputs 140 × 16, 10 apart, primary at
        /// 240, secondary 150 further), empty or shared inputs red, the tooltip of the selected row, "Default keys",
        /// OK and Cancel. A slot waiting for an input shows "...".
        /// </summary>
        void KeyConfig(HplKeyConfig keys)
        {
            var window = new Rect(90, 70, 620, 460);
            Window(window);
            Draw(_guiText, T("KeyConfig", "KeyConfigTitle", "Конфигурация клавиш"), new Vector2(125, 50), Vector2.one * 25, Color.white);
            var header = Vector2.one * 18 * 1.25f;
            var size = Vector2.one * 16;
            float x = window.x + 30, y = window.y + 85;
            string category = T("KeyConfig", "Category", "Категория");
            Draw(_guiText, category, new Vector2(x, y), header, Color.white);
            float comboX = x + (_guiText.font != null ? _guiText.font.GetLength(header, category) : 100) + 10;
            string[] categories = { T("KeyConfig", "Movement", "Движение"), T("KeyConfig", "Actions", "Действия"), T("KeyConfig", "Misc", "Разное") };
            var combo = new Rect(comboX, y, 175, 25);
            Piece(null, combo, new Color(0, 0, 0, 0.5f));
            Button(combo, categories[Mathf.Clamp(keys.Category, 0, 2)] + "  >", MenuControl.KeyCategory);

            x += 15;
            y += 25 + 5;
            Draw(_guiText, T("KeyConfig", "HeaderAction", "Действие"), new Vector2(x, y), header, Color.white);
            Draw(_guiText, T("KeyConfig", "HeaderPrimary", "Основная"), new Vector2(x + 240 + 70, y), header, Color.white, HplFontAlign.Center);
            Draw(_guiText, T("KeyConfig", "HeaderSecondary", "Дополнительная"), new Vector2(x + 390 + 70, y), header, Color.white, HplFontAlign.Center);
            y += header.y + 10;

            int selectedEntry = -1;
            foreach (int i in keys.EntriesOf(keys.Category))
            {
                var e = keys.Entries[i];
                Draw(_guiText, T("Actions", e.name, e.name), new Vector2(x, y), size, Color.white);
                for (int s = 0; s < 2; s++)
                {
                    var rect = new Rect(x + (s == 0 ? 240 : 390), y, 140, 16);
                    bool waiting = keys.WaitingEntry == i && keys.WaitingSlot == s;
                    Piece(null, rect, waiting ? new Color(1, 1, 1, 0.25f) : new Color(0, 0, 0, 0.45f));
                    string name = HplKeyBindings.ButtonName(e.inputs[s]);
                    string label = waiting ? "..." : name == "" ? "" : T("ButtonNames", name, name);
                    int index = _buttons.Count;
                    _buttons.Add((rect, MenuControl.KeySlot));
                    _buttonData.Add(i * 2 + s);
                    var color = e.invalid[s] ? Color.red : index == _selected ? Highlight : Color.white;
                    Draw(_guiText, label, new Vector2(rect.center.x, rect.y), size, color, HplFontAlign.Center);
                    if (index == _selected) selectedEntry = i;
                }
                y += 16 + 10;
            }
            string tip = selectedEntry >= 0 ? T("Actions", keys.Entries[selectedEntry].name + "Tooltip", "") : "";

            // Tip label (400 × 30, black 0.5 background) and the buttons along the bottom.
            float bottom = window.y + window.height - 25 - 10;
            if (tip != "")
            {
                Piece(null, new Rect(window.x + 40, bottom, 400, 30), new Color(0, 0, 0, 0.5f));
                var rows = _guiText.font != null ? _guiText.font.GetWordWrapRows(396, Vector2.one * 14, tip) : new List<string> { tip };
                for (int r = 0; r < rows.Count && r < 2; r++) Draw(_guiText, rows[r], new Vector2(window.x + 42, bottom + r * 15), Vector2.one * 14, Color.white);
            }
            string defaults = T("KeyConfig", "SetDefault", "Задать клавиши по умолчанию");
            float defaultsWidth = (_guiText.font != null ? _guiText.font.GetLength(Vector2.one * 18, defaults) : 200) + 20;
            SkinButton(new Rect(window.x + 40, bottom - 35, defaultsWidth, 25), defaults, MenuControl.KeyDefaults);
            float okX = window.x + window.width - 80 * 2 - 3 - 5;
            SkinButton(new Rect(okX, bottom, 80, 30), T("Global", "OK", "OK"), MenuControl.KeyOk);
            SkinButton(new Rect(okX + 83, bottom, 80, 30), T("Global", "Cancel", "Отмена"), MenuControl.Cancel);
        }

        /// <summary>A closed drop-down list (cWidgetComboBox): label, background with the choice and the arrow button.</summary>
        void Combo(float x, float y, MenuControl control, HplUserSettings settings)
        {
            var size = Vector2.one * 16;
            Draw(_guiText, HplGraphicsOptions.Label(control, Translate), new Vector2(x, y), size, _disabled ? Grey : Color.white);
            var items = HplGraphicsOptions.Items(control, settings, Translate, out int current);
            var rect = new Rect(x, y + 19, ColW, 22);
            _comboRects[control] = rect;
            Piece(null, rect, new Color(0, 0, 0, 0.5f));
            Piece("ComboBoxBackground", rect, Color.white);
            Piece("ComboBoxButtonIcon", new Rect(rect.xMax - 18, rect.y + 2, 18, 18), _disabled ? Grey : Color.white);
            Button(new Rect(rect.x + 4, rect.y + 2, rect.width - 22, 18), items.Count > 0 ? items[current].label : "", control, null, size, HplFontAlign.Left);
            SetLastHit(rect);
        }

        /// <summary>A slider: label and value, then the less button, the slider and the more button.</summary>
        void SmallAdjust(float x, float y, string label, string value, float fraction, MenuControl less, MenuControl more, MenuControl slider, float width = 230)
        {
            var size = Vector2.one * 16;
            var tint = _disabled ? Grey : Color.white;
            Draw(_guiText, label, new Vector2(x, y), size, tint);
            Draw(_guiText, value, new Vector2(x + width, y), size, tint, HplFontAlign.Right);
            float barY = y + 22;
            Button(new Rect(x, barY, 20, 20), "<", less, null, size);
            Button(new Rect(x + width - 20, barY, 20, 20), ">", more, null, size);
            var bar = new Rect(x + 26, barY + 7, width - 52, 6);
            Piece("SliderHoriBackground", bar, tint);
            Piece("SliderHoriArrowUp", new Rect(bar.x + Mathf.Clamp01(fraction) * (bar.width - 12), barY + 2, 12, 18), tint);
            var hit = new Rect(bar.x, barY - 2, bar.width, 24);
            if (_inScroll) hit = ClipToViewport(hit);
            if (!_disabled && hit.width > 0) _sliders.Add((hit, slider));
        }
    }
}
