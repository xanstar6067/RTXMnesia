using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// Runtime UI owner. MainMenu's in-game pause is separate from SetPlayerActive / effect pauses.
    /// Runs before device input so neither opening nor closing a screen leaks a click into the world.
    /// Inventory / Journal will have their own sound policy (LuxInventory / LuxJournal::OnEnterContainer).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class HplGameUi : MonoBehaviour
    {
        public bool IsPaused { get; private set; }
        public bool ConfirmingQuit { get; private set; }
        /// <summary>Real render time excluding menu pauses, including pauses with no rendered frames.</summary>
        public double RenderTime => (IsPaused ? _pauseStartedAt : Time.realtimeSinceStartupAsDouble) - _pausedDuration;

        HplMap _map;
        HplPauseMenu _menu;
        float _previousTimeScale;
        CursorLockMode _previousCursorLock;
        bool _previousCursorVisible, _hudWasEnabled;
        int _selected;
        double _pauseStartedAt, _pausedDuration;

        public void Init(HplMap map)
        {
            if (_map != null) return;
            _map = map;
            _menu = new HplPauseMenu(map, transform);
        }

        void Update()
        {
            if (_map == null || _menu == null || _map.player == null || !_map.player.readDevices) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (!IsPaused) Pause();
                else if (ConfirmingQuit) ShowPauseMenu();
                else Resume();
                return;
            }
            if (!IsPaused) return;

            // Keep the UI cursor available even if the editor or a focus transition relocks it.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var mouse = Mouse.current;
            int hovered = mouse != null ? _menu.HitTest(mouse.position.ReadValue()) : -1;
            if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0f && hovered >= 0) _selected = hovered;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
                    keyboard.tabKey.wasPressedThisFrame) _selected = 1 - _selected;
            }
            _menu.Refresh(ConfirmingQuit, _selected);

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && hovered >= 0) Activate(hovered);
            else if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                Activate(_selected);
        }

        public void Pause()
        {
            if (IsPaused || _map == null || _menu == null) return;
            _previousTimeScale = Time.timeScale;
            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _hudWasEnabled = _map.hud != null && _map.hud.Canvas != null && _map.hud.Canvas.enabled;
            _pauseStartedAt = Time.realtimeSinceStartupAsDouble;
            IsPaused = true;
            _map.player?.SetUiInputBlocked(true);
            _map.sound?.SetMenuPaused(true);
            // Stops Unity's automatic physics too; HplMap also guards explicit Step calls.
            Time.timeScale = 0f;
            if (_map.hud != null && _map.hud.Canvas != null) _map.hud.Canvas.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowPauseMenu();
        }

        public void Resume()
        {
            if (!IsPaused) return;
            _pausedDuration += Time.realtimeSinceStartupAsDouble - _pauseStartedAt;
            IsPaused = false;
            ConfirmingQuit = false;
            Time.timeScale = _previousTimeScale;
            if (_map != null)
            {
                if (_map.player != null) _map.player.SetUiInputBlocked(false);
                if (_map.sound != null) _map.sound.SetMenuPaused(false);
                if (_map.hud != null && _map.hud.Canvas != null) _map.hud.Canvas.enabled = _hudWasEnabled;
            }
            _menu?.SetVisible(false);
            Cursor.lockState = _previousCursorLock;
            Cursor.visible = _previousCursorVisible;
        }

        void ShowPauseMenu()
        {
            ConfirmingQuit = false;
            _selected = 0;
            _menu.SetVisible(true);
            _menu.Refresh(false, _selected);
        }

        void Activate(int index)
        {
            if (!ConfirmingQuit)
            {
                if (index == 0) Resume();
                else
                {
                    ConfirmingQuit = true;
                    _selected = 1;
                    _menu.Refresh(true, _selected);
                }
            }
            else if (index == 1) ShowPauseMenu();
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void OnApplicationFocus(bool focused)
        {
            // Focus returning never resumes gameplay without a deliberate action.
            if (!focused && _map != null && _map.player != null && _map.player.readDevices) Pause();
        }

        void OnDisable()
        {
            // Includes stopping Play, disabling the level and unloading the scene.
            Resume();
        }

        void OnDestroy() => _menu?.Destroy();
    }
}
