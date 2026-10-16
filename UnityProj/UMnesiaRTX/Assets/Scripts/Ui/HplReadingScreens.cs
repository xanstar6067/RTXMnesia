using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// Inventory and journal screens of <see cref="HplGameUi"/>: opening rules, the switch between them inside one
    /// pause and their input (LuxInputHandler.cpp UpdateInventoryInput / UpdateJournalInput). While open, the sound
    /// clocks keep running (ambience and music go on) and the world stays frozen.
    /// </summary>
    public sealed class HplReadingScreens
    {
        public readonly HplInventory Inventory;
        public readonly HplJournal Journal;

        readonly HplGameUi _ui;
        readonly HplMap _map;

        public HplReadingScreens(HplGameUi ui)
        {
            _ui = ui;
            _map = ui.Map;
            Inventory = new HplInventory(_map);
            Journal = new HplJournal(_map);
        }

        /// <summary>
        /// cLuxInputHandler::UpdateGamePlayerInput: player menus only for a live, active player whose state allows
        /// them (not on a ladder), not while an effect pauses the player (emotion flash) and not when the script
        /// disabled the inventory.
        /// </summary>
        public bool CanOpen()
        {
            var player = _map.player;
            return player != null && player.active && !player.IsDead &&
                   (_map.effects == null || !_map.effects.PlayerIsPaused) &&
                   _map.Script?.InventoryDisabled != true && player.States?.CurrentType != PlayerStateType.Ladder;
        }

        public bool HasCursor(UiScreen screen) => screen == UiScreen.Journal ? Journal.View.HasCursor : Inventory.View.HasCursor;

        // ------------------------------------------------------------------ inventory

        public bool OpenInventory()
        {
            if (_ui.IsPaused || !CanOpen()) return false;
            _ui.EnterScreen(UiScreen.Inventory);
            Inventory.Open();
            Inventory.View.Refresh(_ui.MenuOpacity, HplGameUi.PointerPosition);
            return true;
        }

        /// <summary>cLuxInventory::ExitPressed: an open message goes first.</summary>
        public void CloseInventory()
        {
            if (!_ui.InventoryOpen) return;
            if (Inventory.MessageActive) Inventory.DismissMessage();
            else _ui.BeginClose();
        }

        // ------------------------------------------------------------------ journal

        public bool OpenJournal(JournalState state = JournalState.Main, bool fromInventory = false, bool force = false, bool instantExit = false)
        {
            if (_ui.JournalOpen) return true;
            if (_map.player == null || (_ui.IsPaused && !_ui.InventoryOpen) || (!force && !CanOpen())) return false;
            bool returnToInventory = _ui.InventoryOpen && fromInventory;
            if (_ui.InventoryOpen)
            {
                Inventory.Close();
                _ui.SwitchReadingScreen(UiScreen.Journal);
            }
            else _ui.EnterScreen(UiScreen.Journal);
            if (_map.player.States?.CurrentType == PlayerStateType.UseItem) _map.player.States.ChangeState(PlayerStateType.Normal);
            // The quest log key sets ForceInstantExit (UpdateGamePlayerInput).
            Journal.Open(returnToInventory, instantExit || state == JournalState.QuestLog, state);
            Journal.View.Refresh(_ui.MenuOpacity, HplGameUi.PointerPosition);
            return true;
        }

        public bool OpenJournalEntry(HplGame.JournalEntry entry, bool narrated, bool instantExit)
        {
            if (entry == null || !OpenJournal(fromInventory: _ui.InventoryOpen, force: true, instantExit: instantExit)) return false;
            Journal.OpenEntry(entry, narrated);
            return true;
        }

        /// <summary>cLuxJournal::OpenLastReadText.</summary>
        public bool OpenRecentText()
        {
            if (!CanOpen() || HplGame.LastReadEntry == null) return false;
            return OpenJournalEntry(HplGame.LastReadEntry, false, true);
        }

        /// <summary>cLuxJournal::Update when faded out after being opened from the inventory.</summary>
        public void ReturnToInventory()
        {
            Journal.Close();
            _ui.SwitchReadingScreen(UiScreen.Inventory);
            _ui.InventoryFromJournal = true;
            Inventory.Open();
            Inventory.View.Refresh(_ui.MenuOpacity, HplGameUi.PointerPosition);
        }

        public void CloseViews()
        {
            Inventory.Close();
            Journal.Close();
        }

        public void DestroyViews()
        {
            Inventory.View.Destroy();
            Journal.View.Destroy();
        }

        // ------------------------------------------------------------------ input

        public void Update(Keyboard keyboard)
        {
            float dt = Time.unscaledDeltaTime;
            _map.sound?.StepMenu(dt);
            Cursor.lockState = CursorLockMode.None;
            if (_ui.JournalOpen)
            {
                Journal.Tick(dt);
                Cursor.visible = !Journal.View.HasCursor;
                UpdateJournalInput(keyboard);
            }
            else
            {
                Inventory.Tick(dt);
                Cursor.visible = !Inventory.View.HasCursor;
                UpdateInventoryInput(keyboard);
            }
        }

        void UpdateInventoryInput(Keyboard keyboard)
        {
            var mouse = Mouse.current;
            Vector2 position = HplGameUi.PointerPosition;
            if (!_ui.IsClosing)
            {
                if (keyboard != null)
                {
                    if (keyboard.leftArrowKey.wasPressedThisFrame) Inventory.Navigate(-1, 0);
                    if (keyboard.rightArrowKey.wasPressedThisFrame) Inventory.Navigate(1, 0);
                    if (keyboard.upArrowKey.wasPressedThisFrame) Inventory.Navigate(0, -1);
                    if (keyboard.downArrowKey.wasPressedThisFrame) Inventory.Navigate(0, 1);
                    if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) Inventory.UseSelected();
                }
                if (mouse != null)
                {
                    int hit = Inventory.View.HitTest(position);
                    if (mouse.delta.ReadValue().sqrMagnitude > 0) Inventory.Select(hit);
                    if (Inventory.MessageActive)
                    {
                        // Any mouse button only closes the message (UpdateInventoryInput).
                        if (mouse.leftButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
                            Inventory.DismissMessage();
                    }
                    else
                    {
                        if (mouse.leftButton.wasPressedThisFrame) Inventory.PointerDown(hit, Inventory.View.VirtualPoint(position));
                        if (mouse.leftButton.wasReleasedThisFrame) Inventory.PointerUp(hit);
                    }
                }
            }
            Inventory.View.Refresh(_ui.MenuOpacity, position);
        }

        void UpdateJournalInput(Keyboard keyboard)
        {
            var mouse = Mouse.current;
            Vector2 position = HplGameUi.PointerPosition;
            if (!_ui.IsClosing)
            {
                if (keyboard != null)
                {
                    if (keyboard.upArrowKey.wasPressedThisFrame) Journal.Navigate(-1);
                    if (keyboard.downArrowKey.wasPressedThisFrame) Journal.Navigate(1);
                    if (keyboard.leftArrowKey.wasPressedThisFrame) Journal.TurnPage(-1);
                    if (keyboard.rightArrowKey.wasPressedThisFrame) NextOrActivate(() => Journal.TurnPage(1));
                    if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                        NextOrActivate(() => Journal.View.Activate(Journal.Selected));
                }
                if (mouse != null)
                {
                    int hit = Journal.View.HitTest(position);
                    if (mouse.delta.ReadValue().sqrMagnitude > 0) Journal.Select(hit);
                    if (mouse.leftButton.wasPressedThisFrame) NextOrActivate(() => Journal.View.Activate(hit));
                    if (mouse.rightButton.wasPressedThisFrame) Journal.ExitPressed(false);
                    float scroll = mouse.scroll.ReadValue().y;
                    if (scroll != 0) Journal.TurnPage(scroll < 0 ? 1 : -1);
                }
            }
            Journal.View.Refresh(_ui.MenuOpacity, position);
        }

        /// <summary>A narrated read has no arrows: any advance goes to the next voice segment (or exits after the last).</summary>
        void NextOrActivate(System.Action action)
        {
            if (Journal.IsNarrated) Journal.ExitPressed(false);
            else action();
        }
    }
}
