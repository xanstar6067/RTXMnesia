using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// cLuxCredits (LuxCredits.cpp): StartCredits switches to the "Credits" container — the map, its script and the game
    /// modules stop, only the engine (sound entries, the music) goes on — and plays the ending song through cMusicHandler
    /// directly. The text (word wrapped to 750 at 17, rows starting with '*' are headers) scrolls up at ScrollSpeed; near
    /// its end the song stops, then a fade to black, a fade in to "The End" (with the secret code of the ending for the
    /// main game), and after Exit the last fade returns to the main menu with the game reset. Exit / Jump / Attack /
    /// Interact / Inventory skip the scroll or leave "The End", after at least 3 s of each. Layout: menu.cfg Credits on
    /// the 800×600 virtual screen.
    /// </summary>
    public sealed class HplCredits : MonoBehaviour
    {
        // cLuxCredits::mKeyBuffer: filled by AddKeyPart (Justine); its check is commented out in HPL2.
        static readonly List<int> s_keyParts = new();

        public static HplCredits Current { get; private set; }
        /// <summary>The credits container is current: the map does not update.</summary>
        public static bool Active => Current != null;
        public static IReadOnlyList<int> KeyParts => s_keyParts;

        public int State => _state;
        public float Time => _time;
        public float FadeAlpha => _fadeAlpha;
        public float YPos => _yPos;
        public int RowCount => _rows.Count;
        public bool ReturningToMenu => _returning;

        HplMap _map;
        HplLanguage _language;
        HplFont _fontNormal, _fontHeader;
        Vector2 _normalFontSize, _headerFontSize, _theEndFontSize;
        float _scrollSpeed, _fadeSpeed;
        string _music = "";
        bool _loopMusic, _returning;
        int _endNum;
        List<string> _rows = new();
        float _yPos = 600f, _time, _fadeAlpha;
        int _state;
        HplGuiText _textNormal, _textHeader;
        RawImage _fade;

        /// <summary>cLuxCredits::AddKeyPart.</summary>
        public static void AddKeyPart(int key) => s_keyParts.Add(key);

        /// <summary>cLuxCredits::Reset of the key buffer (new game).</summary>
        public static void ResetKeyParts() => s_keyParts.Clear();

        /// <summary>cLuxScriptHandler::StartCredits: Setup, then the Credits container.</summary>
        public static HplCredits Start(HplMap map, string music, bool loopMusic, string textCat, string textEntry, int endNum)
        {
            if (Current != null) Destroy(Current.gameObject);
            var go = new GameObject("HPL Credits");
            var credits = go.AddComponent<HplCredits>();
            credits.Init(map);
            credits.Setup(music, loopMusic, textCat, textEntry, endNum);
            credits.OnEnterContainer();
            Current = credits;
            return credits;
        }

        void Init(HplMap map)
        {
            _map = map;
            _language = map.language;
            var menu = map.menuConfig;
            _normalFontSize = menu != null ? menu.GetVector2("Credits", "NormalFontSize") : Vector2.zero;
            _headerFontSize = menu != null ? menu.GetVector2("Credits", "HeaderFontSize") : Vector2.zero;
            _theEndFontSize = menu != null ? menu.GetVector2("Credits", "TheEndFontSize") : Vector2.zero;
            _scrollSpeed = menu != null ? menu.GetFloat("Credits", "ScrollSpeed") : 0f;
            _fadeSpeed = menu != null ? menu.GetFloat("Credits", "FadeSpeed") : 0f;
            // cLuxCredits::LoadFonts: NormalFont game_default.fnt, HeaderFont menu.fnt (the converted fonts of the load
            // screen and the menu).
            var load = Resources.Load<HplLoadScreenAssets>(map.Campaign.ResourceName(HplLoadScreenAssets.ResourceName));
            var menus = Resources.Load<HplMenuAssets>(map.Campaign.ResourceName(HplMenuAssets.ResourceName));
            _fontNormal = load != null ? load.defaultFont : null;
            _fontHeader = menus != null ? menus.menuFont : null;
            if (_fontNormal == null || _fontHeader == null) Debug.LogError("[RTXMnesia] Credits fonts missing (run Import > Load screen resources / Main menu)");
            BuildUi();
        }

        /// <summary>cLuxCredits::Setup.</summary>
        void Setup(string music, bool loopMusic, string textCat, string textEntry, int endNum)
        {
            _music = music ?? "";
            _loopMusic = loopMusic;
            _endNum = endNum;
            string text = _language != null ? _language.Translate(textCat, textEntry) : textEntry;
            _rows = _fontNormal != null ? _fontNormal.GetWordWrapRows(750f, new Vector2(17f, 17f), text) : new List<string>();
        }

        /// <summary>cLuxCredits::OnEnterContainer (+ the game container stopping: no world update, no game input).</summary>
        void OnEnterContainer()
        {
            UnityEngine.Time.timeScale = 0f;
            if (_map.player != null) _map.player.SetUiInputBlocked(true);
            if (_map.hud != null && _map.hud.Canvas != null) _map.hud.Canvas.enabled = false;
            if (_music != "")
            {
                _map.sound?.EnsureMusic();
                _map.sound?.Music.PlayDirect(_music, 1f, 1f, _loopMusic, false);
            }
        }

        void Update()
        {
            float timeStep = UnityEngine.Time.unscaledDeltaTime;
            if (!_returning)
            {
                // cLuxInputHandler::UpdateCreditsInput
                var keyboard = Keyboard.current;
                var mouse = Mouse.current;
                if ((keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)) ||
                    (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)))
                    ExitPressed();
                Step(timeStep);
            }
            _map?.sound?.StepEngine(timeStep);
            Refresh();
        }

        /// <summary>cLuxCredits::Update.</summary>
        public void Step(float timeStep)
        {
            _time += timeStep;
            if (_state == 0 || _state == 1)
            {
                _yPos -= timeStep * _scrollSpeed;
                // Is the text over? (Its own row heights 17 / 19, empty rows twice: not the drawn ones.)
                float y = _yPos;
                foreach (string row in _rows)
                {
                    int size = row.Length > 0 && row[0] == '*' ? 1 : 0;
                    float add = size == 1 ? 19f : 17f;
                    if (row.Length <= 1) y += add;
                    y += add;
                }
                // Almost done, fade out the song.
                if (y < -120f) StopMusic();
                // Finished, fade out.
                if (y < -280f && _state == 0)
                {
                    _time = 0f;
                    _state++;
                }
                if (_state == 0) return;
            }
            if (_state == 1 || _state == 4)
            {
                _fadeAlpha += timeStep * _fadeSpeed;
                if (_fadeAlpha > 1f)
                {
                    _fadeAlpha = 1f;
                    // The last fade out ends the game.
                    if (_state == 4) ReturnToMainMenu();
                    else _state++;
                }
                return;
            }
            if (_state == 2)
            {
                _fadeAlpha -= timeStep * _fadeSpeed;
                if (_fadeAlpha < 0f)
                {
                    _fadeAlpha = 0f;
                    _state++;
                }
            }
        }

        /// <summary>cLuxCredits::ExitPressed: only during the text roll or "The End", each shown for at least 3 s.</summary>
        public void ExitPressed()
        {
            if (_state != 0 && _state != 3) return;
            if (_time < 3f) return;
            _state++;
            _time = 0f;
            if (_state == 1) StopMusic();
        }

        void StopMusic() => _map?.sound?.Music?.StopDirect(0.1f);

        /// <summary>
        /// The game reset and the main menu (BroadcastMessageToAll(Reset), DrawMenuScreen, SetContainer("MainMenu")):
        /// the campaign's menu scene loads like a return from the pause menu, which resets the game data.
        /// </summary>
        void ReturnToMainMenu()
        {
            _returning = true;
            ResetKeyParts();
            var ui = _map != null ? _map.GameUi : null;
            if (ui == null || !ui.BeginSceneLoad(_map.Campaign.MenuScene, false)) Debug.LogError("[RTXMnesia] Credits could not return to the main menu");
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

            // Black over the whole window (the background), the 800×600 virtual screen centered on it, then the fade
            // (z 20, over the text at z 10).
            var background = CreateImage(transform, "Background");
            Stretch(background.rectTransform);
            background.color = Color.black;
            var screen = new GameObject("Virtual 800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            screen.SetParent(transform, false);
            screen.anchorMin = screen.anchorMax = new Vector2(0.5f, 0.5f);
            screen.sizeDelta = new Vector2(800f, 600f);
            _textNormal = CreateText(screen, "Text", _fontNormal);
            _textHeader = CreateText(screen, "Headers", _fontHeader);
            _fade = CreateImage(transform, "Fade");
            Stretch(_fade.rectTransform);
        }

        /// <summary>cLuxCredits::OnDraw.</summary>
        void Refresh()
        {
            if (_textNormal == null) return;
            _textNormal.Clear();
            _textHeader.Clear();
            _fade.color = new Color(0f, 0f, 0f, _fadeAlpha);
            if (_state == 0 || _state == 1)
            {
                float[] rowAdd = { _normalFontSize.y + 2f, _headerFontSize.y + 2f };
                float y = _yPos;
                foreach (string row in _rows)
                {
                    int size = 0;
                    // An empty row advances twice (here and below).
                    if (row.Length <= 1) y += rowAdd[size];
                    else
                    {
                        if (row[0] == '*') size = 1;
                        if (y >= -rowAdd[size])
                        {
                            if (y > 600f) continue;
                            float alpha = y / 300f;
                            if (alpha > 1f) alpha = 2f - alpha;
                            if (size == 0) _textNormal.DrawFont(row, new Vector2(400f, y), _normalFontSize, new Color(1f, 1f, 1f, alpha), HplFontAlign.Center);
                            else _textHeader.DrawFont(row.Substring(1), new Vector2(400f, y), _headerFontSize, new Color(0.8f, 0.8f, 0.8f, alpha), HplFontAlign.Center);
                        }
                    }
                    y += rowAdd[size];
                }
            }
            else if (_state >= 2)
            {
                _textHeader.DrawFont(Translate("General", "TheEnd"), new Vector2(400f, 290f), _theEndFontSize, Color.white, HplFontAlign.Center);
                // The secret code of the ending (not in Justine).
                if (_endNum >= 0 && _endNum <= 2 && !_map.Campaign.IsJustine)
                {
                    string code = _endNum == 0 ? "tyr299" : _endNum == 1 ? "odn314" : "lke271";
                    _textNormal.DrawFont(code, new Vector2(400f, 560f), new Vector2(15f, 15f), new Color(0.65f, 0.65f, 0.65f, 1f), HplFontAlign.Center);
                }
            }
        }

        static HplGuiText CreateText(Transform parent, string name, HplFont font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 600f);
            var text = go.AddComponent<HplGuiText>();
            text.font = font;
            text.raycastTarget = false;
            return text;
        }

        static RawImage CreateImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>cLuxCredits::AppLostInputFocus / AppGotInputFocus.</summary>
        void OnApplicationFocus(bool focused)
        {
            var music = _map != null && _map.sound != null ? _map.sound.Music : null;
            if (music == null) return;
            if (focused) music.Resume();
            else music.Pause();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
