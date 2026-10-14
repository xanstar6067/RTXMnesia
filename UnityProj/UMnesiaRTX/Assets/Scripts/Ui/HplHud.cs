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
    /// scaled with the window height. Drawn with Unity UI (overlay canvas, not saved with the scene) and the
    /// built-in font until the game's bitmap fonts are converted (step 6, menus).
    /// </summary>
    public class HplHud : MonoBehaviour
    {
        [Tooltip("menu.cfg Messages QuestAddedIcon, imported.")]
        public Texture2D questAddedIcon;
        public bool showSubtitles = true;
        public bool showHints = true;

        public readonly MessageHandler Messages = new();
        public readonly HintHandler Hints = new();

        HplMap _map;
        Canvas _canvas;
        Text _subtitles, _message, _hintTitle, _hintText, _emotionText;
        RawImage _questIcon, _questGlow;
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

            // Voice subtitles: rows above y = 580, left aligned at 400 - width/2 (cLuxEffect_PlayVoice::OnDraw).
            string voice = effects != null && effects.PlayVoice.Active && showSubtitles ? effects.PlayVoice.CurrentText : "";
            SetText(_subtitles, voice, 1f);

            float messageAlpha = Messages.Alpha;
            SetText(_message, messageAlpha > 0f ? Messages.Text : "", messageAlpha);

            bool hint = Hints.Active && Hints.Alpha > 0f;
            float glow = Hints.Oscillation * 0.5f;
            SetText(_hintTitle, hint ? " " + Translate("Hints", "HINT:") : "", Hints.Alpha, new Color(1f, glow, glow));
            SetText(_hintText, hint ? " " + Hints.Text : "", Hints.Alpha);

            var emotion = effects != null ? effects.EmotionFlash : null;
            SetText(_emotionText, emotion != null && emotion.Active && emotion.TextAlpha > 0f ? emotion.Text : "", emotion?.TextAlpha ?? 0f, Color.black);

            float quest = Messages.QuestAlpha;
            _questIcon.enabled = _questGlow.enabled = quest > 0f && questAddedIcon != null;
            _questIcon.color = new Color(1f, 1f, 1f, quest);
            float glowAlpha = quest * Messages.QuestOscillation;
            _questGlow.color = new Color(glowAlpha, glowAlpha, glowAlpha, 1f);
        }

        static void SetText(Text text, string value, float alpha, Color? color = null)
        {
            text.enabled = !string.IsNullOrEmpty(value) && alpha > 0f;
            if (!text.enabled) return;
            text.text = value;
            Color c = color ?? Color.white;
            c.a = Mathf.Clamp01(alpha);
            text.color = c;
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

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            float voiceRow = _voiceFontSize.y + 2f;
            _subtitles = CreateText(screen, "Subtitles", font, (int)_voiceFontSize.y, TextAnchor.LowerLeft,
                                    new Rect(400f - _voiceRowWidth * 0.5f, 580f - voiceRow * 6f, _voiceRowWidth, voiceRow * 6f));
            _message = CreateText(screen, "Message", font, (int)Messages.fontSize.y, TextAnchor.UpperCenter, new Rect(50f, 345f, 700f, 200f));
            _hintTitle = CreateText(screen, "Hint title", font, (int)Hints.fontSize.y, TextAnchor.UpperCenter, new Rect(60f, Hints.yPos, 680f, Hints.fontSize.y + 4f));
            _hintText = CreateText(screen, "Hint", font, (int)Hints.fontSize.y, TextAnchor.UpperCenter,
                                   new Rect(60f, Hints.yPos + Hints.fontSize.y + 3f, 680f, 120f));
            _emotionText = CreateText(screen, "Emotion flash", font, 20, TextAnchor.MiddleCenter, new Rect(150f, 200f, 500f, 200f));
            foreach (var t in new[] { _subtitles, _message, _hintTitle, _hintText })
            {
                var shadow = t.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
                shadow.effectDistance = new Vector2(1f, -1f);
            }

            // Quest added: icon in the bottom right corner, 20 px from the edges, with an additive glow.
            Vector2 iconSize = questAddedIcon != null ? new Vector2(questAddedIcon.width, questAddedIcon.height) : new Vector2(64f, 64f);
            var iconRect = new Rect(800f - iconSize.x - 20f, 600f - iconSize.y - 20f, iconSize.x, iconSize.y);
            _questIcon = CreateImage(screen, "Quest added", iconRect, null);
            _questGlow = CreateImage(screen, "Quest added glow", iconRect, AdditiveUiMaterial());
            Refresh();
        }

        static Text CreateText(RectTransform parent, string name, Font font, int size, TextAnchor anchor, Rect virtualRect)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, virtualRect);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.enabled = false;
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
                        // TODO step 6 (sound): Messages QuestAddedSound when crossing 0.1.
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
