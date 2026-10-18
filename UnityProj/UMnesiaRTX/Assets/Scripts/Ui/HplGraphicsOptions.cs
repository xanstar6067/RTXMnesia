using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>A choice of a drop-down list; a disabled one (a feature still to come) is shown but cannot be picked.</summary>
    public readonly struct HplComboItem
    {
        public readonly string label;
        public readonly bool enabled;
        public HplComboItem(string label, bool enabled = true) { this.label = label; this.enabled = enabled; }
    }

    /// <summary>
    /// The drop-down lists of the graphics options: their choices and how a choice changes the draft settings.
    /// Texts come from the game's localization (OptionsMenu); what the original menu does not have — the lighting and
    /// texture sets, the screen modes, the FPS limit and the anti-aliasing methods — has its own Russian text.
    /// </summary>
    public static class HplGraphicsOptions
    {
        /// <summary>Translation: category, entry, fallback text.</summary>
        public delegate string Translate(string category, string entry, string fallback);

        public static bool IsCombo(MenuControl c) => c >= MenuControl.Lighting && c <= MenuControl.RtGi;

        /// <summary>DLSS is chosen and runs on this GPU: it replaces the anti-aliasing of the graphics tab.</summary>
        public static bool DlssOn(HplUserSettings s) => s.dlssMode != (int)HplDlssMode.Off && HplDlss.Available;

        public static List<HplComboItem> Items(MenuControl control, HplUserSettings s, Translate t, out int current)
        {
            var items = new List<HplComboItem>();
            current = 0;
            switch (control)
            {
                case MenuControl.Lighting:
                    items.Add(new HplComboItem("Классическое"));
                    // Its settings are on the RTX tab; DXR needs DirectX 12 and a GPU with ray tracing.
                    items.Add(new HplComboItem("Трассировка лучей", SystemInfo.supportsRayTracing));
                    current = s.lighting;
                    break;
                case MenuControl.Textures:
                    items.Add(new HplComboItem("Классические"));
                    items.Add(new HplComboItem("Улучшенные (в разработке)", false));
                    current = s.textures;
                    break;
                case MenuControl.Resolution:
                {
                    var sizes = ResolutionChoices(s, out var chosen);
                    foreach (var size in sizes) items.Add(new HplComboItem(size.x + " x " + size.y));
                    current = sizes.IndexOf(chosen);
                    break;
                }
                case MenuControl.ScreenMode:
                    items.Add(new HplComboItem("Полноэкранный"));
                    items.Add(new HplComboItem("Окно без рамки"));
                    items.Add(new HplComboItem("Оконный"));
                    current = s.screenMode;
                    break;
                case MenuControl.FpsLimit:
                    foreach (int fps in HplGraphics.FpsLimits) items.Add(new HplComboItem(fps > 0 ? fps.ToString() : "Без ограничений"));
                    current = Array.IndexOf(HplGraphics.FpsLimits, s.fpsLimit);
                    break;
                case MenuControl.TextureQuality:
                    items.Add(new HplComboItem(t("OptionsMenu", "Low", "Низкое")));
                    items.Add(new HplComboItem(t("OptionsMenu", "Medium", "Среднее")));
                    items.Add(new HplComboItem(t("OptionsMenu", "High", "Высокое")));
                    current = s.textureQuality;
                    break;
                case MenuControl.Anisotropy:
                    foreach (int level in HplGraphics.AnisotropyLevels) items.Add(new HplComboItem(level > 1 ? level + "x" : t("OptionsMenu", "Off", "Выкл")));
                    current = s.anisotropy;
                    break;
                case MenuControl.Antialiasing when DlssOn(s):
                    // DLSS (RTX tab) does the anti-aliasing; turned off there, the earlier choice comes back.
                    items.Add(new HplComboItem("DLSS", false));
                    break;
                case MenuControl.Antialiasing:
                    items.Add(new HplComboItem(t("OptionsMenu", "Off", "Выкл")));
                    items.Add(new HplComboItem("FXAA"));
                    items.Add(new HplComboItem("SMAA"));
                    items.Add(new HplComboItem("TAA"));
                    current = s.antialiasing;
                    break;
                case MenuControl.Shadows:
                    items.Add(new HplComboItem(t("OptionsMenu", "Off", "Выкл")));
                    items.Add(new HplComboItem(t("OptionsMenu", "Low", "Низкое")));
                    items.Add(new HplComboItem(t("OptionsMenu", "Medium", "Среднее")));
                    items.Add(new HplComboItem(t("OptionsMenu", "High", "Высокое")));
                    current = s.shadows;
                    break;
                case MenuControl.DlssMode:
                {
                    bool available = HplDlss.Available;
                    items.Add(new HplComboItem(t("OptionsMenu", "Off", "Выкл")));
                    items.Add(new HplComboItem("DLAA (без масштабирования)", available));
                    items.Add(new HplComboItem("Качество", available));
                    items.Add(new HplComboItem("Баланс", available));
                    items.Add(new HplComboItem("Производительность", available));
                    items.Add(new HplComboItem("Макс. производительность", available));
                    current = s.dlssMode;
                    break;
                }
                case MenuControl.DlssPreset:
                {
                    // NVIDIA offers each model only for some modes; the others are shown grey.
                    uint allowed = HplDlss.AvailablePresets((HplDlssMode)s.dlssMode);
                    var presets = HplDlss.Presets();
                    foreach (uint p in presets)
                        items.Add(new HplComboItem(p == 0 ? "Авто (по выбору NVIDIA)" : PresetLabel(p), p == 0 || (allowed & p) != 0));
                    current = presets.IndexOf((uint)s.dlssPreset);
                    break;
                }
                case MenuControl.RtShadows:
                    items.Add(new HplComboItem(t("OptionsMenu", "Off", "Выкл")));
                    for (int i = 1; i < HplRayTracing.ShadowLights.Length; i++)
                        items.Add(new HplComboItem(RtLevel(i, t) + " (до " + HplRayTracing.ShadowLights[i] + " источников)"));
                    current = s.rtShadows;
                    break;
                case MenuControl.RtAo:
                    items.Add(new HplComboItem(t("OptionsMenu", "Off", "Выкл")));
                    for (int i = 1; i < HplRayTracing.AoLevels; i++) items.Add(new HplComboItem(RtLevel(i, t)));
                    current = s.rtAo;
                    break;
                case MenuControl.RtReflections:
                    items.Add(new HplComboItem(t("OptionsMenu", "Off", "Выкл")));
                    for (int i = 1; i < HplRayTracing.ReflectionLevels; i++) items.Add(new HplComboItem(RtLevel(i, t)));
                    current = s.rtReflections;
                    break;
                case MenuControl.RtGi:
                    items.Add(new HplComboItem(t("OptionsMenu", "Off", "Выкл")));
                    for (int i = 1; i < HplRayTracing.GiLevels; i++) items.Add(new HplComboItem(RtLevel(i, t)));
                    current = s.rtGi;
                    break;
            }
            current = Mathf.Clamp(current, 0, Mathf.Max(0, items.Count - 1));
            return items;
        }

        /// <summary>The label above a drop-down list.</summary>
        public static string Label(MenuControl control, Translate t) => control switch
        {
            MenuControl.Lighting => "Освещение",
            MenuControl.Textures => "Текстуры",
            MenuControl.Resolution => t("OptionsMenu", "Resolution", "Разрешение"),
            MenuControl.ScreenMode => t("OptionsMenu", "Screen", "Экран"),
            MenuControl.FpsLimit => "Ограничение FPS",
            MenuControl.TextureQuality => t("OptionsMenu", "TexQuality", "Качество текстур"),
            MenuControl.Anisotropy => t("OptionsMenu", "Anisotropy", "Анизотропная фильтрация"),
            MenuControl.Antialiasing => t("OptionsMenu", "FullscreenSmooth", "Сглаживание"),
            MenuControl.Shadows => t("OptionsMenu", "Shadows", "Тени"),
            MenuControl.DlssMode => "NVIDIA DLSS",
            MenuControl.DlssPreset => "Модель DLSS",
            MenuControl.RtShadows => "Тени (трассировка)",
            MenuControl.RtGi => "Глобальное освещение (RTGI)",
            MenuControl.RtReflections => "Отражения (трассировка)",
            MenuControl.RtAo => "Затенение (RTAO)",
            _ => "",
        };

        /// <summary>A quality step of a ray tracing effect: low, medium, high, ultra.</summary>
        static string RtLevel(int level, Translate t) => level switch
        {
            1 => t("OptionsMenu", "Low", "Низкое"),
            2 => t("OptionsMenu", "Medium", "Среднее"),
            3 => t("OptionsMenu", "High", "Высокое"),
            _ => "Ультра",
        };

        /// <summary>A DLSS model: its letter and generation (F — the last CNN model, J/K — transformer, L/M — transformer 2).</summary>
        static string PresetLabel(uint preset)
        {
            string letter = HplDlss.PresetLetter(preset);
            string kind = letter switch
            {
                "A" or "B" or "C" or "D" or "E" or "F" => "CNN",
                "J" or "K" => "трансформер",
                "L" or "M" => "трансформер 2",
                _ => "",
            };
            return kind.Length > 0 ? letter + " (" + kind + ")" : letter;
        }

        /// <summary>Picks a choice of the list (disabled choices are ignored).</summary>
        public static void Set(MenuControl control, HplUserSettings s, int index, Translate t)
        {
            var items = Items(control, s, t, out _);
            if (index < 0 || index >= items.Count || !items[index].enabled) return;
            switch (control)
            {
                case MenuControl.Lighting: s.lighting = index; break;
                case MenuControl.Textures: s.textures = index; break;
                case MenuControl.Resolution:
                {
                    var sizes = ResolutionChoices(s, out _);
                    s.resolutionWidth = sizes[index].x;
                    s.resolutionHeight = sizes[index].y;
                    break;
                }
                case MenuControl.ScreenMode: s.screenMode = index; break;
                case MenuControl.FpsLimit: s.fpsLimit = HplGraphics.FpsLimits[index]; break;
                case MenuControl.TextureQuality: s.textureQuality = index; break;
                case MenuControl.Anisotropy: s.anisotropy = index; break;
                case MenuControl.Antialiasing: s.antialiasing = index; break;
                case MenuControl.Shadows: s.shadows = index; break;
                case MenuControl.DlssMode:
                    s.dlssMode = index;
                    // A model NVIDIA does not offer for the new mode falls back to its default.
                    if (s.dlssPreset != 0 && (HplDlss.AvailablePresets((HplDlssMode)index) & (uint)s.dlssPreset) == 0) s.dlssPreset = 0;
                    break;
                case MenuControl.DlssPreset: s.dlssPreset = (int)HplDlss.Presets()[index]; break;
                case MenuControl.RtShadows: s.rtShadows = index; break;
                case MenuControl.RtAo: s.rtAo = index; break;
                case MenuControl.RtReflections: s.rtReflections = index; break;
                case MenuControl.RtGi: s.rtGi = index; break;
            }
        }

        /// <summary>The display's sizes plus the chosen one (0 × 0 — the desktop) if the display does not list it.</summary>
        static List<Vector2Int> ResolutionChoices(HplUserSettings s, out Vector2Int chosen)
        {
            var sizes = HplGraphics.Resolutions();
            chosen = s.resolutionWidth > 0 ? new Vector2Int(s.resolutionWidth, s.resolutionHeight) : HplGraphics.DesktopResolution();
            if (!sizes.Contains(chosen)) sizes.Insert(0, chosen);
            return sizes;
        }

        /// <summary>The next enabled choice in a direction (arrows on a closed list); the same one if there is none.</summary>
        public static int Step(List<HplComboItem> items, int current, int direction)
        {
            for (int i = current + direction; i >= 0 && i < items.Count; i += direction)
                if (items[i].enabled) return i;
            return current;
        }
    }
}
