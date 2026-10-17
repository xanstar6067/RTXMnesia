using System;
using System.Collections.Generic;
using System.Globalization;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// cLuxPreMenu (LuxPreMenu.cpp): before the title screen of a run, the sections of config/pre_menu.cfg — the
    /// Frictional and publisher logos (2 s each), then on the first start only the welcome texts on their
    /// backgrounds with the music, the gamma section (preview image × 1.25, value label, slider 0.3–2 in 0.05
    /// steps; the gamma applies at once) and "Continue". Each section fades in from black (1 s), shows for its time
    /// (sections with text or gamma wait for Continue), fades out (1 s; Esc / Enter: 0.5 s if skipping is allowed).
    /// Images keep their ratio filling the height. The first start flag (cLuxBase::RaiseFirstStartFlag) is a
    /// PlayerPrefs key here.
    /// </summary>
    public sealed class HplPreMenu : MonoBehaviour
    {
        const string FirstStartKey = "RTXMnesia.FirstStartDone";
        const float GammaMin = 0.3f, GammaMax = 2f, GammaStep = 0.05f;

        enum State { Initial, FadeIn, ShowSection, FadeOut, FastFadeOut, Final }

        /// <summary>Shown once per run, before the first title screen.</summary>
        public static bool ShownThisRun { get; private set; }
        /// <summary>Batch tools skip the pre menu unless a check asks for it.</summary>
        public static bool AllowInBatchMode;
        public static bool ShouldShow => !ShownThisRun && Application.isPlaying && (!Application.isBatchMode || AllowInBatchMode) &&
                                         Resources.Load<HplPreMenuAssets>(HplPreMenuAssets.ResourceName) != null;
        public static bool IsFirstStart => PlayerPrefs.GetInt(FirstStartKey, 0) == 0;

        HplMap _map;
        Action _onDone;
        HplPreMenuAssets _assets;
        HplMenuAssets _menu;
        GameObject _root;
        RectTransform _screen;
        RawImage _back, _background, _fade, _gammaPreview, _cursor;
        HplGuiText _text;
        readonly List<RawImage> _pieces = new();
        int _pieceIndex;
        Material _gammaMaterial;
        bool _firstStart;

        State _state = State.Initial;
        int _sectionIndex = -1;
        HplPreMenuAssets.Section _section;
        float _alphaFade, _timer;
        int _gammaSlider, _gammaSliderMax;
        bool _dragGamma, _exited;
        Rect _continueRect, _sliderRect;

        public int SectionIndex => _sectionIndex;
        public bool Done => _exited;
        public string StateName => _state.ToString();
        public float AlphaFade => _alphaFade;
        public float GammaValue => GammaMin + (GammaMax - GammaMin) * _gammaSlider / Mathf.Max(1, _gammaSliderMax);
        public HplPreMenuAssets.Section CurrentSection => _section;
        /// <summary>The overlay canvas (checks render it with a camera).</summary>
        public Canvas Canvas => _root != null ? _root.GetComponent<Canvas>() : null;

        public void Begin(HplMap map, Action onDone)
        {
            _map = map;
            _onDone = onDone;
            ShownThisRun = true;
            _firstStart = IsFirstStart;
            _assets = Resources.Load<HplPreMenuAssets>(HplPreMenuAssets.ResourceName);
            _menu = Resources.Load<HplMenuAssets>(map.Campaign.ResourceName(HplMenuAssets.ResourceName));
            _gammaSliderMax = Mathf.RoundToInt((GammaMax - GammaMin) / GammaStep);
            HplUserSettings.Gamma = HplUserSettings.SavedGamma();
            SetGammaValueToInput(HplUserSettings.Gamma);
            BuildUi();
            _state = State.Initial;
            _alphaFade = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
        }

        // ------------------------------------------------------------------ logic

        void Update()
        {
            if (_assets == null || _exited) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            UpdateInput();
            UpdateActions(dt);
            UpdateState();
            Draw();
            if (_state == State.Final) Exit();
        }

        void UpdateInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            // cLuxInputHandler::UpdatePreMenuInput: Exit or UIPrimary → ButtonPressed.
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                ButtonPressed();
            bool gui = GuiActive;
            if (!gui || _state != State.ShowSection && _state != State.FadeIn) return;
            if (_section.hasGammaSettings && keyboard != null)
            {
                // Gamma_UIArrowPressed: left / right move the slider a step.
                if (keyboard.leftArrowKey.wasPressedThisFrame) SetGammaSlider(_gammaSlider - 1);
                if (keyboard.rightArrowKey.wasPressedThisFrame) SetGammaSlider(_gammaSlider + 1);
            }
            if (mouse == null) return;
            Vector2 p = VirtualPoint(mouse.position.ReadValue());
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (_continueRect.Contains(p)) ContinuePressed();
                else if (_section.hasGammaSettings && _sliderRect.Contains(p)) _dragGamma = true;
            }
            if (_dragGamma && mouse.leftButton.isPressed)
            {
                float t = Mathf.Clamp01((p.x - _sliderRect.x) / Mathf.Max(1f, _sliderRect.width));
                SetGammaSlider(Mathf.RoundToInt(t * _gammaSliderMax));
            }
            if (!mouse.leftButton.isPressed) _dragGamma = false;
        }

        bool GuiActive => _section != null && (_section.texts.Count > 0 || _section.hasGammaSettings);

        /// <summary>cLuxPreMenu::ButtonPressed: a fast fade out unless the section forbids skipping.</summary>
        public void ButtonPressed()
        {
            if (_section != null && !_section.allowSkipping) return;
            _state = State.FastFadeOut;
        }

        /// <summary>Continue_Pressed.</summary>
        public void ContinuePressed() => _state = State.FadeOut;

        /// <summary>cLuxPreMenu::SetGammaValueToInput / Gamma_ChangeValue: the slider position and the gamma at once.</summary>
        void SetGammaValueToInput(float gamma)
        {
            gamma = Mathf.Clamp(gamma, GammaMin, GammaMax);
            _gammaSlider = Mathf.RoundToInt((gamma - GammaMin) * _gammaSliderMax / (GammaMax - GammaMin));
        }

        public void SetGammaSlider(int value)
        {
            value = Mathf.Clamp(value, 0, _gammaSliderMax);
            if (value == _gammaSlider) return;
            _gammaSlider = value;
            HplUserSettings.Gamma = GammaValue;
            HplUserSettings.SaveGamma(GammaValue);
        }

        void UpdateActions(float dt)
        {
            switch (_state)
            {
                case State.Initial: _alphaFade = 0f; break;
                case State.FadeIn: _alphaFade = _alphaFade > 0f ? _alphaFade - dt : 0f; break;
                case State.FadeOut: _alphaFade = _alphaFade < 1f ? _alphaFade + dt : 1f; break;
                case State.FastFadeOut: _alphaFade = _alphaFade < 1f ? _alphaFade + dt * 2f : 1f; break;
                case State.ShowSection: _timer -= dt; break;
            }
            // The fade only reaches exactly 0 / 1 in the next step, as the original.
        }

        void UpdateState()
        {
            switch (_state)
            {
                case State.Initial:
                    _state = State.FadeOut;
                    break;
                case State.FadeIn:
                    if (_alphaFade <= 0f && _alphaFade == 0f)
                    {
                        _timer = _section.time;
                        _state = State.ShowSection;
                    }
                    break;
                case State.FadeOut:
                case State.FastFadeOut:
                    if (_alphaFade == 1f) NextSection();
                    break;
                case State.ShowSection:
                    if (_section.texts.Count == 0 && !_section.hasGammaSettings && _timer <= 0f) _state = State.FadeOut;
                    break;
            }
        }

        void NextSection()
        {
            _sectionIndex++;
            // Not the first start: sections only shown on the first start are skipped.
            if (!_firstStart)
                while (_sectionIndex < _assets.sections.Count && _assets.sections[_sectionIndex].showFirstStartOnly) _sectionIndex++;
            if (_sectionIndex >= _assets.sections.Count)
            {
                _state = State.Final;
                _section = null;
                return;
            }
            _state = State.FadeIn;
            _section = _assets.sections[_sectionIndex];
            if (_section.music != "" && _map != null && _map.sound != null)
            {
                _map.sound.EnsureMusic();
                float fadeSpeed = _section.musicFadeTime == 0f ? 100f : 1f / _section.musicFadeTime;
                _map.sound.Music.PlayDirect(_section.music, _section.musicVolume, fadeSpeed, true, false);
            }
        }

        void Exit()
        {
            if (_exited) return;
            _exited = true;
            if (_firstStart)
            {
                PlayerPrefs.SetInt(FirstStartKey, 1);
                PlayerPrefs.Save();
            }
            if (_root != null) Destroy(_root);
            var done = _onDone;
            _onDone = null;
            done?.Invoke();
            Destroy(this);
        }

        // ------------------------------------------------------------------ drawing

        void BuildUi()
        {
            _root = new GameObject("HPL pre menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _root.hideFlags = HideFlags.DontSave;
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.matchWidthOrHeight = 1f;
            _back = FullScreen("Black (z -2)");
            _back.color = Color.black;
            _screen = new GameObject("Virtual 800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            _screen.SetParent(_root.transform, false);
            _screen.anchorMin = _screen.anchorMax = _screen.pivot = Vector2.one * 0.5f;
            _screen.sizeDelta = new Vector2(800f, 600f);
            _background = Image(_screen, "Section background");
            _gammaPreview = Image(_screen, "Gamma preview");
            var hudAssets = Resources.Load<HplHudEffectAssets>(HplHudEffectAssets.ResourceName);
            Shader guiShader = _assets.guiShader != null ? _assets.guiShader : hudAssets != null ? hudAssets.guiShader : null;
            if (guiShader != null)
            {
                _gammaMaterial = new Material(guiShader) { hideFlags = HideFlags.DontSave, name = "Pre menu gamma preview" };
                _gammaPreview.material = _gammaMaterial;
            }
            var go = new GameObject("Pre menu text", typeof(RectTransform));
            go.transform.SetParent(_screen, false);
            Place(go.GetComponent<RectTransform>(), new Rect(0f, 0f, 800f, 600f));
            _text = go.AddComponent<HplGuiText>();
            _text.font = _menu != null ? _menu.guiFont : _map.hud.defaultFont;
            _text.raycastTarget = false;
            _fade = FullScreen("Fade (z 2)");
            _cursor = Image(_root.transform, "Pre menu cursor");
            var pointer = _menu != null ? _menu.Find("PointerNormal").texture : null;
            _cursor.texture = pointer;
            _cursor.enabled = false;
        }

        void Draw()
        {
            _text.Clear();
            _pieceIndex = 0;
            _continueRect = _sliderRect = default;
            _fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(_alphaFade));
            _fade.transform.SetAsLastSibling();

            var background = _section != null ? _section.image : null;
            _background.enabled = background != null || _section != null;
            if (_section != null)
            {
                if (background != null)
                {
                    // Correct ratio, always filling the height.
                    float ratioMul = (float)background.width / background.height / (4f / 3f);
                    _background.texture = background;
                    _background.color = Color.white;
                    Place(_background.rectTransform, new Rect(400f * (1f - ratioMul), 0f, 800f * ratioMul, 600f));
                }
                else
                {
                    // No file: a rect of the section color.
                    _background.texture = Texture2D.whiteTexture;
                    _background.color = _section.color;
                    Place(_background.rectTransform, new Rect(-400f, 0f, 1600f, 600f));
                }
            }

            bool gui = GuiActive;
            if (gui)
            {
                foreach (var t in _section.texts)
                {
                    string text = _map.hud.TryTranslate(t.category, t.entry, out var s) ? s : t.entry;
                    var size = t.fontSize == Vector2.zero ? Vector2.one * 14f : t.fontSize;
                    var rows = _text.font != null ? _text.font.GetWordWrapRows(t.frameSize.x, size, text) : new List<string> { text };
                    float x = t.align == HplFontAlign.Center ? t.position.x + t.frameSize.x / 2f : t.align == HplFontAlign.Right ? t.position.x + t.frameSize.x : t.position.x;
                    for (int i = 0; i < rows.Count; i++)
                        _text.DrawFont(rows[i], new Vector2(x, t.position.y + i * size.y), size, t.color, t.align);
                }

                // Continue: (font length + 20, font height) + 10, centered, 10 above the bottom.
                string label = _map.hud.TryTranslate("PreMenu", "Continue", out var c) ? c : "Continue";
                var buttonFont = Vector2.one * 14f;
                float length = (_text.font != null ? _text.font.GetLength(buttonFont, label) : 80f) + 20f;
                var buttonSize = new Vector2(length, buttonFont.y) + Vector2.one * 10f;
                _continueRect = new Rect((800f - buttonSize.x) * 0.5f, 600f - buttonSize.y - 10f, buttonSize.x, buttonSize.y);
                Vector2 pointer = Mouse.current != null ? VirtualPoint(Mouse.current.position.ReadValue()) : Vector2.zero;
                bool hover = _continueRect.Contains(pointer);
                Piece(hover ? "ButtonDownBackground" : "ButtonUpBackground", _continueRect, Color.white);
                _text.DrawFont(label, new Vector2(_continueRect.center.x, _continueRect.y + 5f), buttonFont, Color.white, HplFontAlign.Center);
            }

            bool gamma = gui && _section.hasGammaSettings && _assets.gammaImage != null;
            _gammaPreview.enabled = gamma;
            if (gamma)
            {
                var previewSize = new Vector2(_assets.gammaImage.width, _assets.gammaImage.height) * 1.25f;
                // The group encloses the preview, label (+10) and slider (+25, height 25); centered, 20 lower.
                float groupHeight = previewSize.y + 10f + 25f + 25f;
                var origin = new Vector2(400f, 300f) - new Vector2(previewSize.x, groupHeight) * 0.5f + new Vector2(0f, 20f);
                _gammaPreview.texture = _assets.gammaImage;
                if (_gammaMaterial != null) _gammaMaterial.SetFloat("_HplGamma", GammaValue);
                Place(_gammaPreview.rectTransform, new Rect(origin.x, origin.y, previewSize.x, previewSize.y));
                string gammaLabel = (_map.hud.TryTranslate("OptionsMenu", "Gamma", out var g) ? g : "Gamma") + ": " +
                                    GammaValue.ToString("0.00", CultureInfo.InvariantCulture);
                _text.DrawFont(gammaLabel, new Vector2(origin.x, origin.y + previewSize.y + 10f), Vector2.one * 14f, Color.white);
                // Slider: arrows of SliderButtonSize 18 at the ends, the handle 1/4 of the range wide.
                _sliderRect = new Rect(origin.x, origin.y + previewSize.y + 35f, previewSize.x, 25f);
                Piece("SliderHoriArrowUp", new Rect(_sliderRect.x, _sliderRect.y, 18f, 25f), Color.white);
                Piece("SliderHoriArrowDown", new Rect(_sliderRect.xMax - 18f, _sliderRect.y, 18f, 25f), Color.white);
                var track = new Rect(_sliderRect.x + 18f, _sliderRect.y, _sliderRect.width - 36f, 25f);
                Piece("SliderHoriBackground", track, Color.white);
                int barSize = Mathf.RoundToInt(0.25f * _gammaSliderMax);
                float handleWidth = track.width * barSize / (_gammaSliderMax + barSize);
                float handleX = track.x + (track.width - handleWidth) * _gammaSlider / Mathf.Max(1, _gammaSliderMax);
                // The handle: background with the 2 px borders and corners of the skin (DrawBordersAndCorners).
                var handle = new Rect(handleX, track.y, handleWidth, track.height);
                Piece("SliderHandleBackground", handle, Color.white);
                Piece("SliderHandleBorderLeft", new Rect(handle.x, handle.y + 2f, 2f, handle.height - 4f), Color.white);
                Piece("SliderHandleBorderRight", new Rect(handle.xMax - 2f, handle.y + 2f, 2f, handle.height - 4f), Color.white);
                Piece("SliderHandleBorderUp", new Rect(handle.x + 2f, handle.y, handle.width - 4f, 2f), Color.white);
                Piece("SliderHandleBorderDown", new Rect(handle.x + 2f, handle.yMax - 2f, handle.width - 4f, 2f), Color.white);
                Piece("SliderHandleCornerLU", new Rect(handle.x, handle.y, 2f, 2f), Color.white);
                Piece("SliderHandleCornerRU", new Rect(handle.xMax - 2f, handle.y, 2f, 2f), Color.white);
                Piece("SliderHandleCornerLD", new Rect(handle.x, handle.yMax - 2f, 2f, 2f), Color.white);
                Piece("SliderHandleCornerRD", new Rect(handle.xMax - 2f, handle.yMax - 2f, 2f, 2f), Color.white);
            }

            for (int i = _pieceIndex; i < _pieces.Count; i++) _pieces[i].enabled = false;
            _text.transform.SetAsLastSibling();
            _fade.transform.SetAsLastSibling();

            // The mouse is drawn only while the GUI is active.
            _cursor.enabled = gui && _cursor.texture != null && Mouse.current != null;
            Cursor.visible = gui && _cursor.texture == null;
            if (_cursor.enabled)
            {
                Vector2 p = VirtualPoint(Mouse.current.position.ReadValue());
                _cursor.transform.SetParent(_screen, false);
                Place(_cursor.rectTransform, new Rect(p.x, p.y, _cursor.texture.width, _cursor.texture.height));
                _cursor.transform.SetAsLastSibling();
            }
        }

        void Piece(string type, Rect rect, Color color)
        {
            if (_pieceIndex == _pieces.Count) _pieces.Add(Image(_screen, "Skin piece"));
            var target = _pieces[_pieceIndex++];
            var piece = _menu != null && type != null ? _menu.Find(type) : default;
            target.texture = piece.texture != null ? piece.texture : Texture2D.whiteTexture;
            target.color = piece.texture != null ? piece.color * color : new Color(0.3f, 0.3f, 0.3f, 1f) * color;
            target.enabled = true;
            target.transform.SetSiblingIndex(_text.transform.GetSiblingIndex());
            Place(target.rectTransform, rect);
        }

        Vector2 VirtualPoint(Vector2 position)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen, position, null, out var p);
            return new Vector2(p.x + 400f, 300f - p.y);
        }

        RawImage FullScreen(string name)
        {
            var image = Image(_root.transform, name);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            return image;
        }

        static RawImage Image(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.raycastTarget = false;
            return image;
        }

        static void Place(RectTransform rect, Rect r)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(r.x, -r.y);
            rect.sizeDelta = r.size;
        }

        void OnDestroy()
        {
            if (_gammaMaterial != null) Destroy(_gammaMaterial);
            if (_root != null) Destroy(_root);
        }
    }
}
