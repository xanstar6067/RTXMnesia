using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// The configurable actions of cLuxInputHandler (LuxInputHandler.cpp gvPlayerActions with mbConfigurable) and
    /// their two inputs, written as in default_user_keys.cfg: "Keyboard.W", "MouseButton.LeftMouse". Defaults come
    /// from default_user_keys.cfg (the map's keys config); the user's choice (the key config screen) is kept in
    /// PlayerPrefs. Gamepad inputs are not used (no gamepad support), and "Lean" (lean with the mouse while held,
    /// left Alt in HPL2) is not ported, so it is not listed.
    /// </summary>
    public static class HplKeyBindings
    {
        public enum Category { Movement, Actions, Misc }

        public sealed class Action
        {
            public string name;
            public Category category;
            public string primary = "", secondary = "";
        }

        const string PrefsKey = "RTXMnesia.KeyBindings.v1";

        // The order of gvPlayerActions within each category.
        static readonly (string name, Category category, string primary, string secondary)[] Defaults =
        {
            ("Forward", Category.Movement, "Keyboard.W", ""),
            ("Backward", Category.Movement, "Keyboard.S", ""),
            ("Right", Category.Movement, "Keyboard.D", ""),
            ("Left", Category.Movement, "Keyboard.A", ""),
            ("LeanRight", Category.Movement, "Keyboard.E", ""),
            ("LeanLeft", Category.Movement, "Keyboard.Q", ""),
            ("Run", Category.Movement, "Keyboard.LeftShift", ""),
            ("Crouch", Category.Movement, "Keyboard.LeftControl", ""),
            ("Jump", Category.Movement, "Keyboard.Space", ""),
            ("Attack", Category.Actions, "MouseButton.RightMouse", ""),
            ("Interact", Category.Actions, "MouseButton.LeftMouse", ""),
            ("Rotate", Category.Actions, "Keyboard.R", ""),
            ("Lantern", Category.Actions, "Keyboard.F", ""),
            ("Inventory", Category.Misc, "Keyboard.Tab", ""),
            ("Journal", Category.Misc, "Keyboard.J", ""),
            ("QuestLog", Category.Misc, "Keyboard.M", ""),
            ("RecentText", Category.Misc, "Keyboard.N", ""),
            ("CrosshairToggle", Category.Misc, "Keyboard.X", ""),
        };

        static List<Action> s_actions;
        static Dictionary<string, Action> s_byName;
        static HplConfig s_defaultsConfig;

        [Serializable]
        sealed class SavedBinding { public string name, primary, secondary; }
        [Serializable]
        sealed class SavedBindings { public List<SavedBinding> actions = new(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_actions = null;
            s_byName = null;
            s_defaultsConfig = null;
        }

        /// <summary>The default inputs from default_user_keys.cfg (call when a map with a keys config starts).</summary>
        public static void SetDefaultsConfig(HplConfig keysConfig)
        {
            if (keysConfig == null || s_defaultsConfig == keysConfig) return;
            s_defaultsConfig = keysConfig;
            s_actions = null;
        }

        public static IReadOnlyList<Action> Actions
        {
            get
            {
                Ensure();
                return s_actions;
            }
        }

        public static Action Get(string name)
        {
            Ensure();
            return name != null && s_byName.TryGetValue(name, out var a) ? a : null;
        }

        /// <summary>The default inputs of an action (default_user_keys.cfg, else the built-in table); gamepad inputs dropped.</summary>
        public static (string primary, string secondary) DefaultInputs(string name)
        {
            foreach (var d in Defaults)
            {
                if (d.name != name) continue;
                string p = d.primary, s = d.secondary;
                if (s_defaultsConfig != null)
                {
                    p = s_defaultsConfig.GetString(name, "Primary", p);
                    s = s_defaultsConfig.GetString(name, "Secondary", s);
                }
                return (Usable(p) ? p : "", Usable(s) ? s : "");
            }
            return ("", "");
        }

        static bool Usable(string input) => !string.IsNullOrEmpty(input) && (input.StartsWith("Keyboard.", StringComparison.Ordinal) ||
                                                                              input.StartsWith("MouseButton.", StringComparison.Ordinal));

        static void Ensure()
        {
            if (s_actions != null) return;
            s_actions = new List<Action>();
            s_byName = new Dictionary<string, Action>(StringComparer.Ordinal);
            foreach (var d in Defaults)
            {
                var (p, s) = DefaultInputs(d.name);
                var a = new Action { name = d.name, category = d.category, primary = p, secondary = s };
                s_actions.Add(a);
                s_byName[a.name] = a;
            }
            try
            {
                if (PlayerPrefs.HasKey(PrefsKey))
                {
                    var saved = JsonUtility.FromJson<SavedBindings>(PlayerPrefs.GetString(PrefsKey));
                    if (saved != null)
                        foreach (var b in saved.actions)
                            if (b != null && b.name != null && s_byName.TryGetValue(b.name, out var a))
                            {
                                a.primary = b.primary == "" || Usable(b.primary) ? b.primary ?? "" : a.primary;
                                a.secondary = b.secondary == "" || Usable(b.secondary) ? b.secondary ?? "" : a.secondary;
                            }
                }
            }
            catch (ArgumentException) { Debug.LogWarning("[RTXMnesia] Invalid key bindings; using the defaults."); }
        }

        /// <summary>The new inputs of all actions (cLuxMainMenu_KeyConfig::ApplyChanges), saved for the next start.</summary>
        public static void Apply(IEnumerable<(string name, string primary, string secondary)> bindings)
        {
            Ensure();
            foreach (var (name, primary, secondary) in bindings)
                if (s_byName.TryGetValue(name, out var a))
                {
                    a.primary = primary ?? "";
                    a.secondary = secondary ?? "";
                }
            var saved = new SavedBindings();
            foreach (var a in s_actions) saved.actions.Add(new SavedBinding { name = a.name, primary = a.primary, secondary = a.secondary });
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(saved));
            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------------ input state

        public static bool IsHeld(string action) => Check(action, c => c.isPressed, WheelHeld);
        public static bool WasPressed(string action) => Check(action, c => c.wasPressedThisFrame, WheelPressed);
        public static bool WasReleased(string action) => Check(action, c => c.wasReleasedThisFrame, _ => false);

        static bool Check(string action, Func<ButtonControl, bool> test, Func<string, bool> wheel)
        {
            var a = Get(action);
            if (a == null) return false;
            return CheckInput(a.primary, test, wheel) || CheckInput(a.secondary, test, wheel);
        }

        static bool CheckInput(string input, Func<ButtonControl, bool> test, Func<string, bool> wheel)
        {
            if (string.IsNullOrEmpty(input)) return false;
            if (input == "MouseButton.WheelUp" || input == "MouseButton.WheelDown") return wheel(input);
            var control = Control(input);
            return control != null && test(control);
        }

        static bool WheelHeld(string input) => WheelPressed(input);

        static bool WheelPressed(string input)
        {
            var mouse = Mouse.current;
            if (mouse == null) return false;
            float y = mouse.scroll.ReadValue().y;
            return input == "MouseButton.WheelUp" ? y > 0f : y < 0f;
        }

        /// <summary>The Input System control of an HPL2 input name, null if unknown or the device is missing.</summary>
        public static ButtonControl Control(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            if (input.StartsWith("Keyboard.", StringComparison.Ordinal))
            {
                var keyboard = Keyboard.current;
                if (keyboard == null || !KeyFromName(input.Substring(9), out Key key)) return null;
                return keyboard[key];
            }
            if (input.StartsWith("MouseButton.", StringComparison.Ordinal))
            {
                var mouse = Mouse.current;
                if (mouse == null) return null;
                return input.Substring(12) switch
                {
                    "LeftMouse" => mouse.leftButton,
                    "RightMouse" => mouse.rightButton,
                    "MiddleMouse" => mouse.middleButton,
                    "Button6" => mouse.backButton,
                    "Button7" => mouse.forwardButton,
                    _ => null,
                };
            }
            return null;
        }

        /// <summary>The input pressed this frame, in HPL2 notation (the key config's waiting input), else null.</summary>
        public static string PressedInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
                foreach (var pair in KeyNames)
                    if (keyboard[pair.Value].wasPressedThisFrame) return "Keyboard." + pair.Key;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame) return "MouseButton.LeftMouse";
                if (mouse.rightButton.wasPressedThisFrame) return "MouseButton.RightMouse";
                if (mouse.middleButton.wasPressedThisFrame) return "MouseButton.MiddleMouse";
                if (mouse.backButton.wasPressedThisFrame) return "MouseButton.Button6";
                if (mouse.forwardButton.wasPressedThisFrame) return "MouseButton.Button7";
                float y = mouse.scroll.ReadValue().y;
                if (y > 0f) return "MouseButton.WheelUp";
                if (y < 0f) return "MouseButton.WheelDown";
            }
            return null;
        }

        /// <summary>The ButtonNames entry of an input ("Keyboard.LeftShift" → "LeftShift").</summary>
        public static string ButtonName(string input) =>
            string.IsNullOrEmpty(input) ? "" : input.Substring(input.IndexOf('.') + 1);

        static bool KeyFromName(string name, out Key key) => KeyNames.TryGetValue(name, out key);

        // HPL2 key names (Keyboard.cpp StringToKey) → Input System keys (US layout positions).
        static readonly Dictionary<string, Key> KeyNames = BuildKeyNames();

        static Dictionary<string, Key> BuildKeyNames()
        {
            var d = new Dictionary<string, Key>(StringComparer.Ordinal);
            for (char c = 'A'; c <= 'Z'; c++) d[c.ToString()] = Key.A + (c - 'A');
            for (int i = 0; i <= 9; i++)
            {
                d[i.ToString()] = i == 0 ? Key.Digit0 : Key.Digit1 + (i - 1);
                d["KP_" + i] = i == 0 ? Key.Numpad0 : Key.Numpad1 + (i - 1);
            }
            for (int i = 1; i <= 12; i++) d["F" + i] = Key.F1 + (i - 1);
            d["BackSpace"] = Key.Backspace;
            d["Tab"] = Key.Tab;
            d["Return"] = Key.Enter;
            d["Pause"] = Key.Pause;
            d["Space"] = Key.Space;
            d["Quote"] = Key.Quote;
            d["Comma"] = Key.Comma;
            d["Minus"] = Key.Minus;
            d["Period"] = Key.Period;
            d["Slash"] = Key.Slash;
            d["SemiColon"] = Key.Semicolon;
            d["Equals"] = Key.Equals;
            d["LeftBracket"] = Key.LeftBracket;
            d["BackSlash"] = Key.Backslash;
            d["RightBracket"] = Key.RightBracket;
            d["BackQuote"] = Key.Backquote;
            d["Delete"] = Key.Delete;
            d["KP_Period"] = Key.NumpadPeriod;
            d["KP_Divide"] = Key.NumpadDivide;
            d["KP_Multiply"] = Key.NumpadMultiply;
            d["KP_Minus"] = Key.NumpadMinus;
            d["KP_Plus"] = Key.NumpadPlus;
            d["KP_Enter"] = Key.NumpadEnter;
            d["KP_Equals"] = Key.NumpadEquals;
            d["Up"] = Key.UpArrow;
            d["Down"] = Key.DownArrow;
            d["Right"] = Key.RightArrow;
            d["Left"] = Key.LeftArrow;
            d["Insert"] = Key.Insert;
            d["Home"] = Key.Home;
            d["End"] = Key.End;
            d["PageUp"] = Key.PageUp;
            d["PageDown"] = Key.PageDown;
            d["NumLock"] = Key.NumLock;
            d["CapsLock"] = Key.CapsLock;
            d["ScrollLock"] = Key.ScrollLock;
            d["RightShift"] = Key.RightShift;
            d["LeftShift"] = Key.LeftShift;
            d["RightControl"] = Key.RightCtrl;
            d["LeftControl"] = Key.LeftCtrl;
            d["RightAlt"] = Key.RightAlt;
            d["LeftAlt"] = Key.LeftAlt;
            d["LeftSuper"] = Key.LeftMeta;
            d["RightSuper"] = Key.RightMeta;
            d["Print"] = Key.PrintScreen;
            d["Menu"] = Key.ContextMenu;
            return d;
        }
    }
}
