using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// The pause / title menu of <see cref="HplGameUi"/> (cLuxMainMenu): the main list, confirmations and the options
    /// with their draft settings. Mouse, arrows / Tab and Enter / Space choose controls; <see cref="HplPauseMenu"/> draws.
    /// </summary>
    public sealed class HplMenuScreen
    {
        public MenuControl PendingConfirmation { get; private set; } = MenuControl.ConfirmQuit;
        public bool ConfirmingQuit { get; private set; }
        public bool ShowingOptions { get; private set; }
        public HplUserSettings DraftSettings { get; private set; }

        readonly HplGameUi _ui;
        readonly HplMap _map;
        readonly HplPauseMenu _menu;
        int _selected, _optionsTab;
        MenuControl? _dragSlider;

        public HplMenuScreen(HplGameUi ui, HplPauseMenu menu)
        {
            _ui = ui;
            _map = ui.Map;
            _menu = menu;
        }

        /// <summary>Back to the main list (also after a confirmation, options or a failed scene load).</summary>
        public void Show()
        {
            Reset();
            _selected = 0;
            _menu.SetVisible(true);
            Refresh();
        }

        public void Reset()
        {
            ConfirmingQuit = false;
            ShowingOptions = false;
            DraftSettings = null;
            _dragSlider = null;
        }

        /// <summary>cLuxMainMenu::ExitPressed: closes a window, or the menu itself in game.</summary>
        public void Escape()
        {
            if (ConfirmingQuit || ShowingOptions) Show();
            else if (!_ui.IsTitleScreen) _ui.BeginClose();
        }

        public void Refresh() => _menu.Refresh(ConfirmingQuit, ShowingOptions, _optionsTab, DraftSettings ?? _ui.Settings,
            _selected, _ui.MenuOpacity, HplGameUi.PointerPosition, PendingConfirmation);

        public void Update(Keyboard keyboard)
        {
            // Keep the UI cursor available even if the editor or a focus transition relocks it.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !_menu.HasCursor;
            var mouse = Mouse.current;
            Refresh();
            if (_ui.IsClosing) return;
            if (mouse != null && ShowingOptions) DragSlider(mouse);
            int hovered = mouse != null ? _menu.HitTest(mouse.position.ReadValue()) : -1;
            if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0f && hovered >= 0) _selected = hovered;
            if (keyboard != null)
            {
                int count = _menu.ButtonCount;
                if (keyboard.upArrowKey.wasPressedThisFrame) _selected = (_selected + count - 1) % count;
                if (keyboard.downArrowKey.wasPressedThisFrame) _selected = (_selected + 1) % count;
                if (keyboard.tabKey.wasPressedThisFrame) _selected = (_selected + count + (keyboard.shiftKey.isPressed ? -1 : 1)) % count;
                if (ShowingOptions && keyboard.leftArrowKey.wasPressedThisFrame) AdjustSelected(-1);
                if (ShowingOptions && keyboard.rightArrowKey.wasPressedThisFrame) AdjustSelected(1);
            }
            Refresh();

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && hovered >= 0) Activate(hovered);
            else if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                Activate(_selected);
        }

        void DragSlider(Mouse mouse)
        {
            if (mouse.leftButton.wasPressedThisFrame && _menu.TrySlider(mouse.position.ReadValue(), out var slider)) _dragSlider = slider;
            if (_dragSlider.HasValue && mouse.leftButton.isPressed)
            {
                float value = _menu.SliderValue(_dragSlider.Value, mouse.position.ReadValue());
                if (_dragSlider == MenuControl.VolumeSlider) DraftSettings.masterVolume = Mathf.Round(value * 100) / 100f;
                else DraftSettings.mouseSensitivity = Mathf.Round((0.2f + value * 4.8f) * 10) / 10f;
            }
            if (!mouse.leftButton.isPressed) _dragSlider = null;
        }

        public void OpenOptions(int tab = -1)
        {
            if (tab >= 0) _optionsTab = Mathf.Clamp(tab, 0, 3);
            ShowingOptions = true;
            _dragSlider = null;
            DraftSettings = _ui.Settings.Copy();
            _selected = _optionsTab;
            Refresh();
        }

        public bool Choose(MenuControl control)
        {
            for (int i = 0; i < _menu.ButtonCount; i++)
                if (_menu.ControlAt(i) == control) { Activate(i); return true; }
            return false;
        }

        void AdjustSelected(int direction)
        {
            var control = _menu.ControlAt(_selected);
            if (control >= MenuControl.GameTab && control <= MenuControl.InputTab)
            {
                _optionsTab = (_optionsTab + 4 + direction) % 4;
                _selected = _optionsTab;
                _dragSlider = null;
                Refresh();
            }
            else if (control >= MenuControl.Subtitles && control <= MenuControl.SmoothMouse) Activate(_selected, direction);
        }

        void Activate(int index, int direction = 0)
        {
            if (index < 0 || index >= _menu.ButtonCount) return;
            var control = _menu.ControlAt(index);
            // Arrows on a < or > button move the value their way; a click moves it the button's way.
            int less = direction == 0 ? -1 : direction, more = direction == 0 ? 1 : direction;
            switch (control)
            {
                case MenuControl.Resume: _ui.BeginClose(); break;
                case MenuControl.Options: OpenOptions(); break;
                case MenuControl.Quit:
                case MenuControl.NewGame:
                case MenuControl.ReturnTitle:
                    PendingConfirmation = control == MenuControl.NewGame ? MenuControl.ConfirmNewGame
                        : control == MenuControl.ReturnTitle ? MenuControl.ConfirmReturnTitle : MenuControl.ConfirmQuit;
                    ConfirmingQuit = true;
                    _selected = 1;
                    break;
                case MenuControl.ConfirmNewGame: LoadScene(HplGameUi.FirstLevelScene, true); break;
                case MenuControl.ConfirmReturnTitle: LoadScene(HplGameUi.MainMenuScene, false); break;
                case MenuControl.Cancel: Show(); break;
                case MenuControl.Apply:
                    _ui.Settings = DraftSettings.Copy();
                    _ui.Settings.Apply(_map);
                    _ui.Settings.Save();
                    Show();
                    break;
                case MenuControl.GameTab: case MenuControl.GraphicsTab: case MenuControl.SoundTab: case MenuControl.InputTab:
                    _optionsTab = (int)control - (int)MenuControl.GameTab;
                    _selected = _optionsTab;
                    break;
                case MenuControl.Subtitles: DraftSettings.subtitles = !DraftSettings.subtitles; break;
                case MenuControl.Hints: DraftSettings.hints = !DraftSettings.hints; break;
                case MenuControl.Crosshair: DraftSettings.crosshair = !DraftSettings.crosshair; break;
                case MenuControl.Fullscreen: DraftSettings.fullscreen = !DraftSettings.fullscreen; break;
                case MenuControl.VSync: DraftSettings.vSync = !DraftSettings.vSync; break;
                case MenuControl.InvertMouse: DraftSettings.invertMouse = !DraftSettings.invertMouse; break;
                case MenuControl.SmoothMouse: DraftSettings.smoothMouse = !DraftSettings.smoothMouse; break;
                case MenuControl.VolumeLess: DraftSettings.masterVolume = Mathf.Clamp01(DraftSettings.masterVolume + 0.05f * less); break;
                case MenuControl.VolumeMore: DraftSettings.masterVolume = Mathf.Clamp01(DraftSettings.masterVolume + 0.05f * more); break;
                case MenuControl.SensitivityLess: DraftSettings.mouseSensitivity = Mathf.Clamp(DraftSettings.mouseSensitivity + 0.1f * less, 0.2f, 5f); break;
                case MenuControl.SensitivityMore: DraftSettings.mouseSensitivity = Mathf.Clamp(DraftSettings.mouseSensitivity + 0.1f * more, 0.2f, 5f); break;
                case MenuControl.ConfirmQuit:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
            if (!_ui.IsTransitioning) Refresh();
        }

        /// <summary>A scene that cannot be loaded is a build setup error: logged, and the menu goes back to its list.</summary>
        void LoadScene(string path, bool newGame)
        {
            if (!_ui.BeginSceneLoad(path, newGame)) Show();
        }
    }
}
