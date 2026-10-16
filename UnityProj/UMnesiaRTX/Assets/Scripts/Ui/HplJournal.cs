using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using UnityEngine;

namespace RTXMnesia.Ui
{
    public enum JournalState { Main, Notes, Diaries, QuestLog, OpenNote, OpenDiary, Narrated }
    public enum JournalCommand { Notes, Diaries, Quests, Back, Previous, Next, Entry }

    /// <summary>cLuxJournal: collections live in HplGame; pages, navigation and narration belong to this session.</summary>
    public sealed class HplJournal
    {
        public sealed class Page { public string voice = ""; public readonly List<string> rows = new(); }
        public sealed class ListEntry
        {
            public HplGame.JournalEntry entry;
            public readonly List<string> rows = new();
            public float y;
        }
        public readonly HplJournalAssets Assets;
        public readonly HplJournalView View;
        public JournalState State { get; private set; }
        public HplGame.JournalEntry CurrentEntry { get; private set; }
        public string Header { get; private set; } = "";
        public bool OpenedFromInventory { get; private set; }
        public bool ForceInstantExit { get; private set; }
        public int PageIndex { get; private set; }
        public int Selected { get; private set; }
        public readonly List<Page> Pages = new();
        public float StateOpacity { get; private set; }
        public bool IsReading => State >= JournalState.OpenNote;
        public bool IsNarrated => State == JournalState.Narrated;
        public float TextWidth => F("NoteTextWidth", 450);
        public float RowDistance => F("NoteRowDist", 22);
        public Vector2 FontSize => V("NoteFontSize", Vector2.one * 20);
        public int MaxRows => Mathf.Max(1, _map.menuConfig != null ? _map.menuConfig.GetInt("Journal", "NoteMaxPageRows", 18) : 18);
        public IReadOnlyList<ListEntry> VisibleEntries => _lists.TryGetValue(State, out var list) && list.Count > 0 ? list[PageIndex] : EmptyList;
        public int PageCount => IsReading ? Pages.Count : _lists.TryGetValue(State, out var list) ? list.Count : 0;
        public bool HasPrevious => !IsNarrated && PageIndex > 0;
        public bool HasNext => !IsNarrated && PageIndex + 1 < PageCount;
        public SoundEntry Narration => _voice;
        readonly HplMap _map;
        readonly Dictionary<JournalState, List<List<ListEntry>>> _lists = new();
        readonly Dictionary<JournalState, int> _listPages = new();
        static readonly List<ListEntry> EmptyList = new();
        SoundEntry _voice;
        int _voiceId;

        public HplJournal(HplMap map)
        {
            _map = map;
            Assets = Resources.Load<HplJournalAssets>(HplJournalAssets.ResourceName);
            View = new HplJournalView(map, this);
        }

        public string T(string entry) => _map.hud.Translate("Journal", entry);
        public float F(string key, float fallback) => _map.menuConfig != null ? _map.menuConfig.GetFloat("Journal", key, fallback) : fallback;
        public Vector2 V(string key, Vector2 fallback) => _map.menuConfig != null ? _map.menuConfig.GetVector2("Journal", key, fallback) : fallback;

        public void Open(bool fromInventory, bool forceInstantExit, JournalState state)
        {
            StopVoice();
            OpenedFromInventory = fromInventory;
            ForceInstantExit = forceInstantExit;
            CurrentEntry = null;
            BuildLists();
            ChangeState(state);
            View.SetVisible(true);
            Sound("journal_open");
        }
        public void Close() { StopVoice(); View.SetVisible(false); }
        public void Tick(float dt)
        {
            StateOpacity = Mathf.MoveTowards(StateOpacity, 1, dt * 2);
            if (_map.effects != null && _map.effects.SanityGainFlash.Active) _map.effects.SanityGainFlash.Update(dt);
            if (IsNarrated && !_map.GameUi.IsClosing && (_voice == null || !_map.sound.IsValid(_voice, _voiceId))) ExitPressed(false);
        }
        public void ChangeState(JournalState state)
        {
            StopVoice();
            State = state;
            StateOpacity = 0;
            Selected = 0;
            if (!IsReading) PageIndex = _listPages.TryGetValue(state, out int page) ? page : 0;
        }
        public void OpenEntry(HplGame.JournalEntry entry, bool narrated)
        {
            if (entry == null || !HplGame.JournalEntries.Contains(entry)) return;
            CurrentEntry = entry;
            entry.read = true;
            HplGame.LastReadEntry = entry;
            ChangeState(narrated ? JournalState.Narrated : entry.type == "Diary" ? JournalState.OpenDiary : JournalState.OpenNote);
            Header = T(entry.NameEntry);
            string text = _map.hud.ParseString(T(entry.TextEntry));
            Pages.Clear();
            Pages.AddRange(BuildPages(Assets != null ? Assets.textFont : _map.hud.defaultFont, TextWidth, MaxRows, text, narrated));
            SetPage(0);
        }

        /// <summary>Original voice tags start narration segments; ordinary reading hides them and paginates at 18 rows.</summary>
        public static List<Page> BuildPages(HplFont font, float width, int maxRows, string text, bool narrated)
        {
            var pages = new List<Page>();
            Page Current()
            {
                if (pages.Count == 0) pages.Add(new Page());
                return pages[pages.Count - 1];
            }
            void Row(string row)
            {
                var page = Current();
                if (!narrated && page.rows.Count >= Math.Max(1, maxRows)) { page = new Page(); pages.Add(page); }
                page.rows.Add(row);
            }
            // Commands occupy their own row in the language files; split before wrapping so long voice paths
            // cannot be broken by the bitmap font's word wrapper.
            string[] tokens = Regex.Split((text ?? "").Replace("\r", ""), @"(\[voice\s+[^\]]+\]|\[new_page\])");
            bool afterCommand = false;
            foreach (string token in tokens)
            {
                if (token.StartsWith("[voice ", StringComparison.Ordinal))
                {
                    if (narrated)
                    {
                        if (pages.Count == 1 && pages[0].rows.TrueForAll(string.IsNullOrWhiteSpace) && pages[0].voice == "") pages.Clear();
                        pages.Add(new Page { voice = token.Substring(7, token.Length - 8).Trim() });
                    }
                    else if (pages.Count > 0) Row("");
                    afterCommand = true;
                }
                else if (token == "[new_page]")
                {
                    if (!narrated) pages.Add(new Page());
                    afterCommand = true;
                }
                else
                {
                    string part = token;
                    if (afterCommand && part.StartsWith("\n", StringComparison.Ordinal)) part = part.Substring(1);
                    if (part.EndsWith("\n", StringComparison.Ordinal)) part = part.Substring(0, part.Length - 1);
                    if (part == "") continue;
                    var rows = font != null ? font.GetWordWrapRows(width, Vector2.one * 20, part) : new List<string>(part.Split('\n'));
                    foreach (string row in rows) Row(row);
                    afterCommand = false;
                }
            }
            if (pages.Count == 0) pages.Add(new Page());
            return pages;
        }

        void BuildLists()
        {
            _lists.Clear(); _listPages.Clear();
            var notes = HplGame.JournalEntries.FindAll(e => e.type == "Note");
            var diaries = new List<HplGame.JournalEntry>();
            var groups = new List<string>();
            foreach (var e in HplGame.JournalEntries) if (e.type == "Diary" && !groups.Contains(e.textEntry)) groups.Add(e.textEntry);
            foreach (string group in groups) diaries.AddRange(HplGame.JournalEntries.FindAll(e => e.type == "Diary" && e.textEntry == group));
            void Entries(JournalState state, List<HplGame.JournalEntry> entries)
            {
                var pages = new List<List<ListEntry>>(); _lists[state] = pages;
                float y = 100;
                foreach (var e in entries)
                {
                    if (pages.Count == 0 || y > 450) { pages.Add(new List<ListEntry>()); y = 100; }
                    pages[pages.Count - 1].Add(new ListEntry { entry = e, y = y });
                    y += 30;
                }
                _listPages[state] = Mathf.Max(0, pages.Count - 1);
            }
            Entries(JournalState.Notes, notes); Entries(JournalState.Diaries, diaries);
            var quests = new List<List<ListEntry>>(); _lists[JournalState.QuestLog] = quests;
            float questY = 100;
            for (int i = HplGame.Quests.Count - 1; i >= 0; i--)
            {
                var quest = HplGame.Quests[i]; if (!quest.active) continue;
                string text = _map.hud.ParseString(T(quest.textEntry));
                var rows = Assets != null && Assets.textFont != null ? Assets.textFont.GetWordWrapRows(500, new Vector2(21, 19), text) : new List<string>(text.Split('\n'));
                if (quests.Count == 0 || (questY > 200 && questY + (rows.Count - 1) * 21 > 450)) { quests.Add(new List<ListEntry>()); questY = 100; }
                var entry = new ListEntry { y = questY }; entry.rows.AddRange(rows); quests[quests.Count - 1].Add(entry);
                questY += rows.Count * 21 + 30;
            }
            _listPages[JournalState.QuestLog] = Mathf.Max(0, quests.Count - 1);
        }
        public void SetPage(int page)
        {
            if (page < 0 || page >= PageCount) return;
            PageIndex = page;
            if (!IsReading) _listPages[State] = page;
            else if (IsNarrated)
            {
                StopVoice();
                string voice = Pages[page].voice;
                _voice = voice != "" ? _map.sound?.PlayGuiStream(voice, false, 1) : null;
                if (_voice != null) _voiceId = _voice.Id;
            }
        }
        public void TurnPage(int direction)
        {
            if (IsNarrated || (direction < 0 ? !HasPrevious : !HasNext)) return;
            SetPage(PageIndex + direction); Sound("journal_page");
        }
        public void ExitPressed(bool instant)
        {
            if (_map.GameUi == null || _map.GameUi.IsClosing) return;
            if (IsNarrated && PageIndex + 1 < Pages.Count) SetPage(PageIndex + 1);
            else if (instant || ForceInstantExit)
            {
                // cLuxJournal::ExitPressed: every direct exit (J / M / N / Tab or a finished read) gives the hint.
                StopVoice(); _map.GameUi.CloseJournal(); Sound("journal_close");
                _map.hud.Hints.Add("RecentlyReadText", _map.hud.Translate("Hints", "RecentlyReadText"), -1);
            }
            else if (State == JournalState.Main)
            {
                StopVoice(); _map.GameUi.CloseJournal(); Sound("journal_close");
            }
            else ChangeState(State == JournalState.OpenNote ? JournalState.Notes : IsReading ? JournalState.Diaries : JournalState.Main);
        }
        public void Activate(JournalCommand command, HplGame.JournalEntry entry = null)
        {
            switch (command)
            {
                case JournalCommand.Notes: ChangeState(JournalState.Notes); break;
                case JournalCommand.Diaries: ChangeState(JournalState.Diaries); break;
                case JournalCommand.Quests: ChangeState(JournalState.QuestLog); break;
                case JournalCommand.Previous: TurnPage(-1); break;
                case JournalCommand.Next: TurnPage(1); break;
                case JournalCommand.Back: ExitPressed(false); break;
                case JournalCommand.Entry: OpenEntry(entry, false); break;
            }
        }
        public void Select(int index) => Selected = index;
        public void Navigate(int delta) { int count = View.ButtonCount; if (count > 0) Selected = (Mathf.Max(0, Selected) + count + delta) % count; }
        void StopVoice()
        {
            if (_voice != null && _map.sound != null && _map.sound.IsValid(_voice, _voiceId)) _voice.FadeOut(3);
            _voice = null;
        }
        void Sound(string file) => _map.sound?.PlayGuiSoundData(file, SoundEntryType.Gui);
    }
}
