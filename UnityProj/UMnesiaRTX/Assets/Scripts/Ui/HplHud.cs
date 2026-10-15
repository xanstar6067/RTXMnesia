using System.Collections.Generic;
using RTXMnesia.Effects;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// Game HUD texts: cLuxMessageHandler (SetMessage, quest added icon), cLuxHintHandler (hints) and the subtitles
    /// of cLuxEffect_PlayVoice / the emotion flash text. Positions are HPL2's 800×600 virtual screen, centered and
    /// scaled with the window height. Drawn with Unity UI (overlay canvas, not saved with the scene); texts in the game
    /// bitmap fonts (<see cref="HplFont"/>, <see cref="HplGuiText"/>) laid out like the original OnDraw code.
    /// </summary>
    public class HplHud : MonoBehaviour
    {
        [Tooltip("menu.cfg Messages QuestAddedIcon, imported.")]
        public Texture2D questAddedIcon;
        public bool showSubtitles = true;
        public bool showHints = true;
        [Tooltip("graphics/hud/hud_crosshair_*.tga by FocusCrosshair: default, grab, push, ignite, pick, leveldoor, ladder.")]
        public Texture2D[] crosshairs = new Texture2D[7];
        [Tooltip("User setting ShowCrossHair: off shows the crosshair only over things that can be interacted with.")]
        public bool showCrossHair = true;

        public readonly MessageHandler Messages = new();
        public readonly HintHandler Hints = new();

        /// <summary>The overlay canvas (simulation snapshots draw it through the camera).</summary>
        public Canvas Canvas => _canvas;

        /// <summary>The text layers in drawing order (for tools).</summary>
        public IEnumerable<HplGuiText> TextLayers => new[] { _focusText, _gameText, _hintText };

        /// <summary>Text draw calls of the frame per layer (for tools).</summary>
        public string DescribeText() =>
            $"text: game {(_gameText != null ? _gameText.CommandCount : -1)} hint {(_hintText != null ? _hintText.CommandCount : -1)} focus {(_focusText != null ? _focusText.CommandCount : -1)}" +
            $" fonts {(defaultFont != null ? defaultFont.name : "-")}/{(hintsFont != null ? hintsFont.name : "-")} canvas {(_canvas != null ? _canvas.renderMode.ToString() : "-")}";

        [Tooltip("game_default.fnt: messages, voice subtitles, emotion flash text.")]
        public HplFont defaultFont;
        [Tooltip("menu.cfg Hints DefaultFont (game_hints.fnt).")]
        public HplFont hintsFont;
        [Tooltip("game.cfg Player_General FocusTextFont (game_default.fnt).")]
        public HplFont focusFont;

        HplMap _map;
        Canvas _canvas;
        HplGuiText _gameText, _hintText, _focusText;
        RawImage _questIcon, _questGlow, _crosshair;
        Vector2 _voiceFontSize = new(18f, 18f);
        float _voiceRowWidth = 600f;
        bool _initialized;

        public void Init(HplMap map)
        {
            if (_initialized) return;
            _initialized = true;
            _map = map;

            var menu = map.menuConfig;
            var game = map.gameConfig;
            var text = new TextDuration
            {
                startTime = menu != null ? menu.GetFloat("General", "TextDuration_StartTime", 1.5f) : 1.5f,
                minTime = menu != null ? menu.GetFloat("General", "TextDuration_MinTime", 2.5f) : 2.5f,
                charTime = menu != null ? menu.GetFloat("General", "TextDuration_CharTime", 0.07f) : 0.07f,
            };
            Messages.duration = Hints.duration = text;
            Messages.fontSize = menu != null ? menu.GetVector2("Messages", "GameMessageFontSize", new Vector2(18f, 18f)) : new Vector2(18f, 18f);
            Messages.QuestAddedSound = menu != null ? menu.GetString("Messages", "QuestAddedSound", "") : "";
            Hints.fontSize = menu != null ? menu.GetVector2("Hints", "FontSize", new Vector2(15f, 15f)) : new Vector2(15f, 15f);
            Hints.yPos = menu != null ? menu.GetFloat("Hints", "YPos", 40f) : 40f;
            Hints.textDurationMul = menu != null ? menu.GetFloat("Hints", "TextDurationMul", 1.5f) : 1.5f;
            if (game != null)
            {
                _voiceFontSize = game.GetVector2("Effects", "VoiceTextFontSize", _voiceFontSize);
                _voiceRowWidth = game.GetFloat("Effects", "VoiceTextRowWidth", _voiceRowWidth);
            }
            Hints.Active = showHints;
            Messages.parse = Hints.parse = ParseString;

            BuildUi();
        }

        /// <summary>
        /// cLuxHelpFuncs::ParseString: "$Name" commands (letters after '$', ended by any other character) become
        /// text: $ButAction → the keys of the action (default_user_keys.cfg, names from category ButtonNames),
        /// $Look → "Mouse". A command at the very end of the text is dropped, as in the original.
        /// </summary>
        public string ParseString(string input)
        {
            if (string.IsNullOrEmpty(input) || input.IndexOf('$') < 0) return input ?? "";
            var output = new System.Text.StringBuilder(input.Length);
            var command = new System.Text.StringBuilder();
            bool parseVar = false;
            foreach (char c in input)
            {
                if (parseVar)
                {
                    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    {
                        command.Append(c);
                        continue;
                    }
                    output.Append(ParseStringCommand(command.ToString()));
                    output.Append(c);
                    parseVar = false;
                }
                else if (c == '$')
                {
                    parseVar = true;
                    command.Clear();
                }
                else output.Append(c);
            }
            return output.ToString();
        }

        string ParseStringCommand(string command)
        {
            if (command.StartsWith("But", System.StringComparison.Ordinal))
            {
                string action = command.Substring(3);
                var keys = _map != null ? _map.keysConfig : null;
                var names = new List<string>();
                foreach (string slot in new[] { "Primary", "Secondary" })
                {
                    // "Keyboard.J", "MouseButton.LeftMouse"; gamepad bindings are not used.
                    string binding = keys != null ? keys.GetString(action, slot) : "";
                    if (string.IsNullOrEmpty(binding) || binding.StartsWith("Gamepad", System.StringComparison.Ordinal)) continue;
                    string key = binding.Substring(binding.IndexOf('.') + 1);
                    names.Add(TryTranslate("ButtonNames", key, out string translated) ? translated : key);
                }
                if (names.Count > 0) return string.Join(" / ", names);
                HplLog.Error($"String parser could not find action '{action}' invalid!");
                return "BADACTION";
            }
            if (command == "Look") return Translate("Hints", "Mouse");
            HplLog.Error($"String parser command '{command}' invalid!");
            return "BADCOMMAND";
        }

        /// <summary>Message and hint logic, in the map step.</summary>
        public void Step(float timeStep)
        {
            Messages.Update(timeStep);
            Hints.Active = showHints;
            Hints.Update(timeStep);
        }

        public string Translate(string category, string entry) =>
            _map != null && _map.language != null ? _map.language.Translate(category, entry) : entry;

        public bool TryTranslate(string category, string entry, out string text)
        {
            text = "";
            return _map != null && _map.language != null && _map.language.TryTranslate(category, entry, out text);
        }

        // ------------------------------------------------------------------ drawing

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (_canvas == null) return;
            var effects = _map != null ? _map.effects : null;
            _gameText.Clear();
            _hintText.Clear();
            _focusText.Clear();

            // cLuxEffect_PlayVoice::OnDraw: rows ending at y = 580, left aligned at 400 - width / 2.
            string voice = effects != null && effects.PlayVoice.Active && showSubtitles ? effects.PlayVoice.CurrentText : "";
            if (!string.IsNullOrEmpty(voice) && defaultFont != null)
            {
                var rows = defaultFont.GetWordWrapRows(_voiceRowWidth, _voiceFontSize, voice);
                var pos = new Vector2(400f - _voiceRowWidth / 2f, 580f - rows.Count * (_voiceFontSize.y + 2f));
                foreach (string row in rows)
                {
                    _gameText.DrawFont(row, pos, _voiceFontSize, Color.white);
                    pos.y += _voiceFontSize.y + 2f;
                }
            }

            // cLuxMessageHandler::DrawMessage: centered rows from y = 345 (rows of 700).
            float messageAlpha = Messages.Alpha;
            if (messageAlpha > 0f && defaultFont != null)
            {
                var pos = new Vector2(400f, 345f);
                foreach (string row in defaultFont.GetWordWrapRows(700f, Messages.fontSize, Messages.Text))
                {
                    _gameText.DrawFont(row, pos, Messages.fontSize, new Color(1f, 1f, 1f, messageAlpha), HplFontAlign.Center);
                    pos.y += Messages.fontSize.y + 2f;
                }
            }

            // cLuxHintHandler::DrawHintText: " HINT:" glowing red, then one centered row or left aligned rows of 680.
            if (Hints.Active && Hints.Alpha > 0f && hintsFont != null)
            {
                float glow = Hints.Oscillation * 0.5f;
                Vector2 size = Hints.fontSize;
                _hintText.DrawFont(" " + Translate("Hints", "HINT:"), new Vector2(400f, Hints.yPos), size, new Color(1f, glow, glow, Hints.Alpha), HplFontAlign.Center);
                const float maxWidth = 680f;
                float startY = Hints.yPos + size.y + 3f;
                var rows = hintsFont.GetWordWrapRows(maxWidth, size, Hints.Text);
                var color = new Color(1f, 1f, 1f, Hints.Alpha);
                if (rows.Count == 1) _hintText.DrawFont(" " + Hints.Text, new Vector2(400f, startY), size, color, HplFontAlign.Center);
                else
                    for (int i = 0; i < rows.Count; i++)
                        _hintText.DrawFont(" " + rows[i], new Vector2(400f - maxWidth * 0.5f, startY + i * (size.y + 2f)), size, color);
            }

            // cLuxEffect_EmotionFlash::OnDraw: black text on the white flash, one centered row or rows from x = 150.
            var emotion = effects != null ? effects.EmotionFlash : null;
            if (emotion != null && emotion.Active && emotion.TextAlpha > 0f && defaultFont != null)
            {
                var size = new Vector2(20f, 20f);
                var rows = defaultFont.GetWordWrapRows(500f, size, emotion.Text);
                float y = 300f - (size.y + 2f) * 0.5f * rows.Count;
                var color = new Color(0f, 0f, 0f, emotion.TextAlpha);
                if (rows.Count == 1) _gameText.DrawFont(rows[0], new Vector2(400f, y), size, color, HplFontAlign.Center);
                else
                    foreach (string row in rows)
                    {
                        _gameText.DrawFont(row, new Vector2(150f, y), size, color);
                        y += size.y + 2f;
                    }
            }

            float quest = Messages.QuestAlpha;
            _questIcon.enabled = _questGlow.enabled = quest > 0f && questAddedIcon != null;
            _questIcon.color = new Color(1f, 1f, 1f, quest);
            float glowAlpha = quest * Messages.QuestOscillation;
            _questGlow.color = new Color(glowAlpha, glowAlpha, glowAlpha, 1f);

            RefreshCrosshair();
        }

        /// <summary>cLuxPlayer::CanDrawCrossHair + DrawHud: the state's crosshair, the focus text fading.</summary>
        void RefreshCrosshair()
        {
            var player = _map != null ? _map.player : null;
            Texture2D texture = null;
            if (player != null && player.States != null && player.showCrossHairIcons)
            {
                bool draw = showCrossHair || player.States.CurrentType == Player.PlayerStateType.UseItem;
                if (!draw)
                {
                    var e = player.EntityInFocus;
                    draw = e != null && e.CanInteract(player.BodyInFocus) && player.FocusDistance <= e.MaxFocusDistance;
                }
                var kind = draw ? player.States.Current.GetCrosshair() : null;
                if (kind.HasValue && (int)kind.Value < crosshairs.Length) texture = crosshairs[(int)kind.Value];
                if (draw && player.States.Current is Player.PlayerStateUseItem useItem) texture = _map.GameUi?.Inventory?.Icon(useItem.Item);
            }
            // The fade is drawn in HPL2's HUD set above the crosshair and focus text (z 3.2 vs 1): it darkens them.
            float fade = _map != null && _map.effects != null ? Mathf.Clamp01(_map.effects.Fade.Alpha) : 0f;
            _crosshair.enabled = texture != null && fade < 1f;
            if (texture != null)
            {
                _crosshair.texture = texture;
                _crosshair.color = new Color(1f - fade, 1f - fade, 1f - fade, 1f);
                if (player.States.Current is Player.PlayerStateUseItem useItem && useItem.Highlight)
                    _crosshair.color = new Color(1f - fade, 1f - fade, 1f - fade, 0.75f + 0.25f * Mathf.Sin(Time.time * 3));
                Place(_crosshair.rectTransform, new Rect((800f - texture.width) * 0.5f, (600f - texture.height) * 0.5f, texture.width, texture.height));
            }

            string focus = "";
            float alpha = 0f;
            if (player != null)
            {
                focus = player.FocusText != "" ? player.FocusText : player.LastFocusText;
                alpha = player.FocusTextAlpha;
            }
            // cLuxPlayer::DrawHud: centered rows of 500 at size 22 from y = 450, 24 apart.
            if (!string.IsNullOrEmpty(focus) && alpha > 0f && focusFont != null)
            {
                var rows = focusFont.GetWordWrapRows(500f, new Vector2(22f, 22f), focus);
                for (int i = 0; i < rows.Count; i++)
                    _focusText.DrawFont(rows[i], new Vector2(400f, 450f + i * 24f), new Vector2(22f, 22f),
                                        new Color(1f - fade, 1f - fade, 1f - fade, Mathf.Clamp01(alpha)), HplFontAlign.Center);
            }
        }

        void BuildUi()
        {
            var go = new GameObject("HPL HUD") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // The 800×600 virtual screen, centered: (0, 0) is its top left corner like HPL2's HUD set.
            var screen = new GameObject("Virtual 800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            screen.SetParent(go.transform, false);
            screen.anchorMin = screen.anchorMax = new Vector2(0.5f, 0.5f);
            screen.sizeDelta = new Vector2(800f, 600f);

            if (defaultFont == null || hintsFont == null || focusFont == null)
                HplLog.Warn("HUD fonts missing: re-import the level to convert the game fonts");

            // Quest added: icon in the bottom right corner, 20 px from the edges, with an additive glow.
            Vector2 iconSize = questAddedIcon != null ? new Vector2(questAddedIcon.width, questAddedIcon.height) : new Vector2(64f, 64f);
            var iconRect = new Rect(800f - iconSize.x - 20f, 600f - iconSize.y - 20f, iconSize.x, iconSize.y);
            _questIcon = CreateImage(screen, "Quest added", iconRect, null);
            _questGlow = CreateImage(screen, "Quest added glow", iconRect, AdditiveUiMaterial());

            // cLuxPlayer::DrawHud: the crosshair image centered at its size, focus text rows from y = 450.
            _crosshair = CreateImage(screen, "Crosshair", new Rect(384f, 284f, 32f, 32f), null);

            // Text layers in the HUD set's depth order: focus text (z 1), messages / voices / emotion text (4), hints (20).
            _focusText = CreateText(screen, "Focus text", focusFont);
            _gameText = CreateText(screen, "Game text", defaultFont);
            _hintText = CreateText(screen, "Hint text", hintsFont);
            Refresh();
        }

        /// <summary>A text layer over the whole virtual screen.</summary>
        static HplGuiText CreateText(RectTransform parent, string name, HplFont font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, new Rect(0f, 0f, 800f, 600f));
            var text = go.AddComponent<HplGuiText>();
            text.font = font;
            text.raycastTarget = false;
            return text;
        }

        RawImage CreateImage(RectTransform parent, string name, Rect virtualRect, Material material)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, virtualRect);
            var image = go.AddComponent<RawImage>();
            image.texture = questAddedIcon;
            image.raycastTarget = false;
            if (material != null) image.material = material;
            image.enabled = false;
            return image;
        }

        /// <summary>Rect in virtual 800×600 coordinates (y down) → anchored to the top left of the virtual screen.</summary>
        static void Place(RectTransform rect, Rect r)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(r.x, -r.y);
            rect.sizeDelta = r.size;
        }

        static Material _additive;

        static Material AdditiveUiMaterial()
        {
            if (_additive != null) return _additive;
            var shader = Shader.Find("UI/Default");
            if (shader == null) return null;
            _additive = new Material(shader) { hideFlags = HideFlags.DontSave, name = "HUD additive" };
            _additive.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            _additive.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            return _additive;
        }

        // ================================================================== logic

        /// <summary>cLuxHelpFuncs::GetStringDuration (menu.cfg General).</summary>
        public struct TextDuration
        {
            public float startTime, minTime, charTime;

            public float Of(string text) => Mathf.Max(minTime, startTime + (text?.Length ?? 0) * charTime);
        }

        /// <summary>cLinearOscillation: goes between min and max at the up / down speeds.</summary>
        public struct LinearOscillation
        {
            public float min, max, val, upSpeed, downSpeed, add;

            public LinearOscillation(float min, float max, float val, float upSpeed, float downSpeed)
            {
                this.min = min;
                this.max = max;
                this.val = val;
                this.upSpeed = Mathf.Abs(upSpeed);
                this.downSpeed = -Mathf.Abs(downSpeed);
                add = this.upSpeed;
            }

            public void Update(float timeStep)
            {
                val += add * timeStep;
                if (add > 0f)
                {
                    if (val >= max)
                    {
                        val = max;
                        add = downSpeed;
                    }
                }
                else if (val <= min)
                {
                    val = min;
                    add = upSpeed;
                }
            }
        }

        /// <summary>cLuxMessageHandler: the center message and the quest added icon.</summary>
        public sealed class MessageHandler
        {
            public TextDuration duration;
            public Vector2 fontSize = new(18f, 18f);
            public System.Func<string, string> parse;
            /// <summary>menu.cfg Messages QuestAddedSound, played by the map's sound handler.</summary>
            public string QuestAddedSound = "";
            public RTXMnesia.Sound.HplSoundHandler Sound;
            public string Text { get; private set; } = "";
            public float Alpha { get; private set; }
            public float QuestAlpha { get; private set; }
            public bool QuestActive { get; private set; }
            public float QuestOscillation => _questOscill.val;

            float _time, _questTime;
            LinearOscillation _questOscill = new(0f, 1f, 0f, 1f, 1f);

            public void SetMessage(string text, float time)
            {
                Text = parse != null ? parse(text ?? "") : text ?? "";
                Alpha = 0f;
                _time = time <= 0f ? duration.Of(Text) : time;
            }

            public void StartQuestAddedMessage()
            {
                if (QuestActive) return;
                QuestActive = true;
                QuestAlpha = 0f;
                _questTime = 0f;
            }

            public void Update(float timeStep)
            {
                if (_time > 0f)
                {
                    _time -= timeStep;
                    Alpha = Mathf.Min(1f, Alpha + timeStep);
                }
                else if (Alpha > 0f)
                {
                    Alpha = Mathf.Max(0f, Alpha - timeStep * 1.5f);
                }

                if (QuestActive)
                {
                    if (QuestAlpha < 0.1f)
                    {
                        QuestAlpha += timeStep;
                        // Played here so that it does not come at once (the original's comment).
                        if (QuestAlpha >= 0.1f && Sound != null) Sound.PlayGuiSoundData(QuestAddedSound, RTXMnesia.Sound.SoundEntryType.Gui);
                    }
                    else if (QuestAlpha < 1f)
                    {
                        QuestAlpha = Mathf.Min(1f, QuestAlpha + timeStep);
                    }
                    else
                    {
                        _questTime += timeStep;
                        if (_questTime > 3f) QuestActive = false;
                    }
                }
                else if (QuestAlpha > 0f)
                {
                    QuestAlpha = Mathf.Max(0f, QuestAlpha - timeStep);
                }
                if (QuestAlpha > 0f) _questOscill.Update(timeStep);
            }
        }

        /// <summary>cLuxHintHandler: one hint at a time, each name only once, blocked names never (lists in <see cref="HplGame"/>).</summary>
        public sealed class HintHandler
        {
            public TextDuration duration;
            public Vector2 fontSize = new(15f, 15f);
            public float yPos = 40f, textDurationMul = 1.5f;
            public bool Active = true;
            public System.Func<string, string> parse;
            public string Text { get; private set; } = "";
            public float Alpha { get; private set; }
            public float Oscillation => _oscill.val;

            float _showTextCount;
            LinearOscillation _oscill = new(0f, 1f, 0f, 1f, 1f);

            public bool Add(string name, string message, float timeShown)
            {
                if (_showTextCount > 0f) return false;
                if (HplGame.HintsBlocked.Contains(name)) return false;
                if (!HplGame.HintsGiven.Add(name)) return false;

                Text = parse != null ? parse(message ?? "") : message ?? "";
                _showTextCount = timeShown <= 0f ? duration.Of(Text) * textDurationMul : timeShown;
                Alpha = 0f;
                return true;
            }

            public bool Remove(string name) => HplGame.HintsGiven.Remove(name);

            public void Update(float timeStep)
            {
                if (!Active) return;
                if (Alpha > 0f) _oscill.Update(timeStep);

                if (_showTextCount <= 0f && Alpha > 0f)
                {
                    Alpha = Mathf.Max(0f, Alpha - 0.5f * timeStep);
                    return;
                }
                if (_showTextCount > 0f)
                {
                    Alpha = Mathf.Min(1f, Alpha + 1.2f * timeStep);
                    _showTextCount -= timeStep;
                }
            }
        }
    }
}
