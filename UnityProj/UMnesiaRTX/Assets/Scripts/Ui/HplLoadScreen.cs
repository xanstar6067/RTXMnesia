using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// cLuxLoadScreenHandler (LuxLoadScreenHandler.cpp): the screen between two maps. SetupLoadScreen picks a text
    /// category / entry (with alRandomNum numbered entries one at random, not repeating until all were shown) and an
    /// image. DrawGameScreen shows it while the next map loads; GameScreenLoadDone keeps it until the text had time to
    /// be read (GetStringDuration × TextDurationMul minus the load time; Exit / Jump / Attack / Interact / Inventory
    /// skip it), the image and the red "Loading" fade (0.33 / s) while the text slides to TextAloneY, then the screen
    /// fades to black in FadeOutTime and the game goes on with the end sound. The new map does not update meanwhile.
    /// Layout: menu.cfg LoadScreen on the 800×600 virtual screen. The object survives the scene switch.
    /// </summary>
    public sealed class HplLoadScreen : MonoBehaviour
    {
        // cLuxLoadScreenHandler::SetupLoadText: one handler for the whole game, reset with a new game.
        static string s_textCat = "", s_textEntry = "", s_textImage = "";
        static int s_textRandomNum, s_prevTextNumBitFlags;

        public static void SetupLoadText(string cat, string entry, int randomNum, string image)
        {
            cat ??= "";
            entry ??= "";
            image ??= "";
            if (s_textCat == cat && s_textEntry == entry && s_textImage == image && s_textRandomNum == randomNum) return;
            s_textCat = cat;
            s_textEntry = entry;
            s_textImage = image;
            s_textRandomNum = randomNum;
            s_prevTextNumBitFlags = 0;
        }

        /// <summary>cLuxLoadScreenHandler::Reset (new game).</summary>
        public static void ResetText()
        {
            s_textCat = s_textEntry = s_textImage = "";
            s_textRandomNum = s_prevTextNumBitFlags = 0;
        }

        public static HplLoadScreen Current { get; private set; }

        /// <summary>The screen covers the game (loading, or the text after loading): the map does not update.</summary>
        public static bool Active => Current != null;

        /// <summary>The game's music while no map holds it (the scenes switch).</summary>
        public Sound.HplMusicHandler Music;

        public bool Loaded => _state == State.Game;
        public string TextEntry => _textEntry;
        public float LoadingAlpha => _loadingAlpha;
        public float Alpha => _alpha;
        public float ExtraTime => _extraTime;

        enum State { Loading, Game }

        State _state;
        HplLoadScreenAssets _assets;
        HplLanguage _language;
        HplMap _map;
        string _textCat = "", _textEntry = "", _endSound = "";
        Texture2D _image;
        float _loadingAlpha = 1f, _alpha = 1f, _extraTime;
        Vector2 _loadingFontSize, _textFontSize;
        float _loadingY, _textWithImageY, _textAloneY, _imageY, _textMaxWidth, _fadeOutTime, _textDurationMul;
        HplHud.TextDuration _duration;
        RawImage _picture, _fade;
        HplGuiText _text;

        /// <summary>cLuxLoadScreenHandler::DrawGameScreen: the screen of the text set up by the map being left.</summary>
        public static HplLoadScreen Show(HplMap from)
        {
            if (Current != null) Destroy(Current.gameObject);
            var go = new GameObject("HPL Load Screen");
            if (Application.isPlaying) DontDestroyOnLoad(go);
            var screen = go.AddComponent<HplLoadScreen>();
            screen.Init(from);
            Current = screen;
            return screen;
        }

        void Init(HplMap from)
        {
            _assets = Resources.Load<HplLoadScreenAssets>(from.Campaign.ResourceName(HplLoadScreenAssets.ResourceName));
            if (_assets == null) Debug.LogError("[RTXMnesia] Load screen resources missing (run Import > Level or Import > Load screen resources)");
            _language = from.language;
            var menu = from.menuConfig;
            _loadingFontSize = menu != null ? menu.GetVector2("LoadScreen", "LoadingFontSize") : Vector2.zero;
            _textFontSize = menu != null ? menu.GetVector2("LoadScreen", "TextFontSize") : Vector2.zero;
            _loadingY = menu != null ? menu.GetFloat("LoadScreen", "LoadingY") : 0f;
            _textWithImageY = menu != null ? menu.GetFloat("LoadScreen", "TextWithImageY") : 0f;
            _textAloneY = menu != null ? menu.GetFloat("LoadScreen", "TextAloneY") : 0f;
            _imageY = menu != null ? menu.GetFloat("LoadScreen", "ImageY") : 0f;
            _textMaxWidth = menu != null ? menu.GetFloat("LoadScreen", "TextMaxWidth") : 0f;
            _fadeOutTime = menu != null ? menu.GetFloat("LoadScreen", "FadeOutTime") : 0f;
            _textDurationMul = menu != null ? menu.GetFloat("LoadScreen", "TextDurationMul") : 0f;
            _duration = new HplHud.TextDuration
            {
                startTime = menu != null ? menu.GetFloat("General", "TextDuration_StartTime", 1.5f) : 1.5f,
                minTime = menu != null ? menu.GetFloat("General", "TextDuration_MinTime", 2.5f) : 2.5f,
                charTime = menu != null ? menu.GetFloat("General", "TextDuration_CharTime", 0.07f) : 0.07f,
            };

            _textCat = s_textCat;
            _textEntry = GetGameScreenTextEntry();
            _image = _assets != null ? _assets.Texture(s_textImage) : null;
            if (_image == null && s_textImage != "") HplLog.Error($"Could not load load screen image '{s_textImage}'");
            _loadingAlpha = 1f;
            _alpha = 1f;
            _state = State.Loading;
            BuildUi();
            Refresh();
        }

        /// <summary>cLuxLoadScreenHandler::GetGameScreenTextEntry: a numbered entry not shown since all were.</summary>
        static string GetGameScreenTextEntry()
        {
            if (s_textCat == "" || s_textEntry == "") return "";
            string entry = s_textEntry;
            if (s_textRandomNum > 1)
            {
                int num = -1;
                int idx = HplMath.RandRectl(0, s_textRandomNum - 1);
                for (int i = 0; i < s_textRandomNum; i++)
                {
                    if (((1 << idx) & s_prevTextNumBitFlags) == 0)
                    {
                        num = idx;
                        break;
                    }
                    if (++idx >= s_textRandomNum) idx = 0;
                }
                // All taken: start over with a random one.
                if (num < 0)
                {
                    s_prevTextNumBitFlags = 0;
                    num = HplMath.RandRectl(0, s_textRandomNum - 1);
                }
                s_prevTextNumBitFlags |= 1 << num;
                entry += num < 9 ? "0" + (num + 1) : HplMath.ToString(num + 1);
            }
            return entry;
        }

        /// <summary>cLuxLoadScreenHandler::GameScreenLoadDone: the new map is in; wait for the text, then fade.</summary>
        public void LoadDone(HplMap map, string endSound, float loadTime)
        {
            _map = map;
            _alpha = 1f;
            _extraTime = 0f;
            _loadingAlpha = 1f;
            _state = State.Game;
            _endSound = endSound ?? "";
            if (_textEntry != "")
            {
                float wanted = _duration.Of(Translate(_textCat, _textEntry)) * _textDurationMul;
                if (wanted > loadTime) _extraTime = wanted - loadTime;
            }
            Time.timeScale = 0f;
            if (map.player != null) map.player.SetUiInputBlocked(true);
        }

        /// <summary>cLuxLoadScreenHandler::ExitPressed: skips the text time.</summary>
        public void ExitPressed()
        {
            if (_state == State.Game) _extraTime = 0f;
        }

        void Update()
        {
            float timeStep = Time.unscaledDeltaTime;
            Music?.Update(timeStep);
            if (_state == State.Game)
            {
                // cLuxInputHandler::UpdateLoadScreenInput
                var keyboard = Keyboard.current;
                var mouse = Mouse.current;
                if ((keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)) ||
                    (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)))
                    ExitPressed();
                // Engine modules (the sound) go on while the load screen container is current.
                if (_map != null && _map.sound != null) _map.sound.Step(timeStep);
                UpdateGameState(timeStep);
            }
            if (this != null) Refresh();
        }

        /// <summary>cLuxLoadScreenHandler::UpdateGameState.</summary>
        public void UpdateGameState(float timeStep)
        {
            if (_loadingAlpha > 0f) _loadingAlpha = Mathf.Max(0f, _loadingAlpha - timeStep * 0.33f);
            if (_extraTime > 0f)
            {
                _extraTime -= timeStep;
                return;
            }
            _alpha -= timeStep * (1f / _fadeOutTime);
            if (_alpha < 0f)
            {
                _alpha = 0f;
                Exit();
            }
        }

        /// <summary>cLuxLoadScreenHandler::Exit: back to the game container, the end sound plays.</summary>
        void Exit()
        {
            Current = null;
            Time.timeScale = 1f;
            if (_map != null)
            {
                if (_map.player != null)
                {
                    _map.player.SetUiInputBlocked(false);
                    Player.AmnesiaPlayer.LockCursor(true);
                }
                _map.sound?.PlayGuiSoundData(_endSound, Sound.SoundEntryType.Gui);
            }
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        string Translate(string cat, string entry) => _language != null ? _language.Translate(cat, entry) : entry;

        // ------------------------------------------------------------------ drawing

        void BuildUi()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // Black over the whole window, the 800×600 virtual screen centered on it.
            var background = CreateImage(transform, "Background", null);
            Stretch(background.rectTransform);
            background.color = Color.black;
            var screen = new GameObject("Virtual 800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            screen.SetParent(transform, false);
            screen.anchorMin = screen.anchorMax = new Vector2(0.5f, 0.5f);
            screen.sizeDelta = new Vector2(800f, 600f);

            _picture = CreateImage(screen, "Image", _image);
            if (_image != null) Place(_picture.rectTransform, new Rect(400f - _image.width / 2f, _imageY, _image.width, _image.height));
            _picture.enabled = _image != null;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(screen, false);
            Place((RectTransform)textGo.transform, new Rect(0f, 0f, 800f, 600f));
            _text = textGo.AddComponent<HplGuiText>();
            _text.font = _assets != null ? _assets.defaultFont : null;
            _text.raycastTarget = false;

            _fade = CreateImage(transform, "Fade", null);
            Stretch(_fade.rectTransform);
        }

        /// <summary>cLuxLoadScreenHandler::DrawGameState / DrawGameScreen.</summary>
        void Refresh()
        {
            if (_text == null) return;
            _text.Clear();
            if (_picture.enabled) _picture.color = new Color(1f, 1f, 1f, _loadingAlpha);
            string loading = Translate("General", "Loading");
            if (_textEntry != "")
            {
                List<string> rows = _text.font != null ? _text.font.GetWordWrapRows(_textMaxWidth, _textFontSize, Translate(_textCat, _textEntry)) : new List<string>();
                float t = _loadingAlpha * _loadingAlpha * (3f - _loadingAlpha * 2f); // cMath::SmoothCurve
                var pos = new Vector2(400f - _textMaxWidth / 2f, _textWithImageY * t + _textAloneY * (1f - t));
                foreach (string row in rows)
                {
                    _text.DrawFont(row, pos, _textFontSize, Color.white);
                    pos.y += _textFontSize.y + 2f;
                }
                _text.DrawFont(loading, new Vector2(400f, _loadingY), _loadingFontSize, new Color(1f, 0f, 0f, _loadingAlpha), HplFontAlign.Center);
            }
            else _text.DrawFont(loading, new Vector2(400f, 300f - _loadingFontSize.y / 2f), _loadingFontSize, Color.white, HplFontAlign.Center);
            _fade.color = new Color(0f, 0f, 0f, 1f - _alpha);
        }

        static RawImage CreateImage(Transform parent, string name, Texture texture)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Rect in virtual 800×600 coordinates (y down) → anchored to the top left of the virtual screen.</summary>
        static void Place(RectTransform rect, Rect r)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(r.x, -r.y);
            rect.sizeDelta = r.size;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
