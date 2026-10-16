using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RTXMnesia.Effects;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    public static class JournalTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        sealed class FixtureScript : HplScript
        {
            public int callbackIndex;
            public void Block(bool blocked) => SetInventoryDisabled(blocked);
            public void Note() => AddNote("CH01L03_VillageTales", "journal/default_note");
            public void Diary() => AddDiary("CH01L03_Daniel", "journal/diary_page");
            void Suppress(string entity, int index) { callbackIndex = index; ReturnOpenJournal(false); }
        }

        [MenuItem("Tools/RTXMnesia/Tests/Journal")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run journal checks outside Play mode.");
            float scale = Time.timeScale; var cursorLock = Cursor.lockState; bool cursorVisible = Cursor.visible;
            var previousMap = HplMap.Current;
            var root = new GameObject("Journal checks") { hideFlags = HideFlags.HideAndDontSave };
            var bank = ScriptableObject.CreateInstance<HplSoundBank>();
            var clip = AudioClip.Create("journal_test", 8820, 1, 44100, false);
            int checks = 0;
            void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
            void FinishClose(HplGameUi ui) => typeof(HplGameUi).GetMethod("FinishClosing", Private).Invoke(ui, null);
            static void Set(object target, string name, object value) => target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(target, value);
            HplGame.Reset();
            try
            {
                var map = root.AddComponent<HplMap>(); typeof(HplMap).GetProperty(nameof(HplMap.Current)).SetValue(null, map);
                map.gameConfig = AssetDatabase.LoadAssetAtPath<HplConfig>("Assets/AmnesiaImported/Config/game.asset");
                map.menuConfig = AssetDatabase.LoadAssetAtPath<HplConfig>("Assets/AmnesiaImported/Config/menu.asset");
                map.keysConfig = AssetDatabase.LoadAssetAtPath<HplConfig>("Assets/AmnesiaImported/Config/default_user_keys.asset");
                map.language = AssetDatabase.LoadAssetAtPath<HplLanguage>("Assets/AmnesiaImported/Lang/russian.asset");
                map.player = root.AddComponent<AmnesiaPlayer>(); map.player.readDevices = false;
                map.hud = root.AddComponent<HplHud>();
                var assets = Resources.Load<HplJournalAssets>(HplJournalAssets.ResourceName);
                Check(assets != null && assets.textFont != null && assets.menuFont != null, "Journal resource bank / fonts missing");
                map.hud.defaultFont = map.hud.hintsFont = map.hud.focusFont = assets.textFont; map.hud.Init(map);
                map.sound = root.AddComponent<HplSoundHandler>(); map.sound.bank = bank; map.sound.EnsureMusic();
                foreach (var voice in map.language.voices) bank.clips.Add(new HplSoundBank.Clip { name = voice.name.ToLowerInvariant(), path = Path.ChangeExtension(voice.file, null).ToLowerInvariant(), clip = clip });
                foreach (string name in new[] { "journal_open", "journal_close", "journal_page", "journal_test" })
                {
                    bank.clips.Add(new HplSoundBank.Clip { name = name, path = name, clip = clip });
                    bank.soundEntities.Add(new HplSoundBank.SoundEntityData { name = name, path = name, main = new[] { name }, loop = false, use3D = false });
                }
                map.effects = root.AddComponent<HplEffects>(); map.effects.Init(map);
                var script = new FixtureScript(); Set(script, nameof(HplScript.Map), map); Set(map, nameof(HplMap.Script), script);
                var ui = root.AddComponent<HplGameUi>(); ui.Init(map); Set(map, nameof(HplMap.GameUi), ui);
                var journal = ui.Journal;
                Check(assets.Texture("bg_main.tga") != null && assets.Texture("default_note_large.tga") != null && assets.Texture("note_return.tga") != null, "Journal graphics incomplete");

                var d1 = HplGame.AddJournalEntry("Diary", "A", "journal/diary_page");
                var other = HplGame.AddJournalEntry("Diary", "B", "journal/diary_page");
                var d2 = HplGame.AddJournalEntry("Diary", "A", "journal/diary_page");
                Check(d1.number == 1 && other.number == 1 && d2.number == 2 && d2.NameEntry == "Diary_A_Name2", "Diary numbering is not per series / one-based");
                Check(d1.ImageFile(false).EndsWith("journal/diary_page_large.tga") && d1.ImageFile(true).EndsWith("diary_page_icon.tga"), "Journal image suffixes incorrect");
                HplGame.Reset();
                script.Note(); script.Diary();
                Check(HplGame.JournalEntries.Count == 2 && !ui.IsPaused, "AddNote / AddDiary APIs should store entries without opening journal");
                HplGame.Reset();
                HplGame.AddItem(map, "note", "Note", "note", "journal/default_note", -1, "CH01L03_VillageTales", "", out bool removeNote);
                Check(removeNote && HplGame.Items.Count == 0 && HplGame.JournalEntries.Count == 1 && !ui.IsPaused, "Hidden note pickup should store data without a slot or screen");
                HplGame.AddItem(map, "diary", "Diary", "diary", "journal/diary_page", 1, "CH01L03_Daniel", "Suppress", out bool removeDiary);
                Check(removeDiary && script.callbackIndex == 1 && !ui.IsPaused && HplGame.LastReadEntry.number == 1 && !HplGame.LastReadEntry.read, "ReturnOpenJournal(false) or diary callback index incorrect");
                Time.timeScale = 0.8f;
                HplGame.AddItem(map, "diary2", "Diary", "diary", "journal/diary_page", 1, "CH01L03_Daniel", "", out _);
                Check(ui.JournalOpen && ui.IsPaused && journal.IsNarrated && journal.ForceInstantExit && journal.CurrentEntry.number == 2, "Diary pickup did not open narration / reset callback suppression");
                Check(journal.CurrentEntry.read && HplGame.LastReadEntry == journal.CurrentEntry && map.player.UiInputBlocked && Time.timeScale == 0, "Read / last text state or world pause incorrect");
                Check(journal.Pages.Count > 1 && journal.Pages.All(p => p.voice != "") && journal.Narration != null && !journal.Narration.IsPaused, "Narration tags / sound not loaded");
                Check(journal.Pages.All(p => p.rows.All(r => !r.Contains("[voice") && !r.Contains("[new_page]"))), "Journal displays command tags");
                var firstVoice = journal.Narration;
                journal.ExitPressed(true);
                Check(journal.PageIndex == 1 && !ui.IsClosing && journal.Narration != firstVoice, "Exit during narrated diary must advance a segment before closing");
                for (int i = 0; i < 100 && !ui.IsClosing; i++) { map.sound.StepMenu(0.25f); journal.Tick(0.25f); }
                Check(ui.IsClosing, "Narration did not advance automatically to completion"); FinishClose(ui);
                Check(!ui.IsPaused && Time.timeScale == 0.8f && !map.player.UiInputBlocked, "Journal exit did not restore time scale / input");

                var pages = HplJournal.BuildPages(assets.textFont, 450, 2, "one\ntwo\nthree\n[new_page]\nfour", false);
                Check(pages.Count == 3 && pages[0].rows.SequenceEqual(new[] { "one", "two" }) && pages[1].rows.Contains("three") && pages[2].rows.Contains("four"), "Row limit / explicit page breaks incorrect");
                pages = HplJournal.BuildPages(assets.textFont, 450, 18, "[voice very_long_voice_name.ogg]\none\n[voice next.ogg]\ntwo", true);
                Check(pages.Count == 2 && pages[0].voice == "very_long_voice_name.ogg" && pages[1].voice == "next.ogg", "Narration command parsing incorrect");

                script.Block(true); Check(!ui.OpenJournal() && !ui.OpenRecentText(), "InventoryDisabled does not protect journal shortcuts"); script.Block(false);
                map.player.active = false; Check(!ui.OpenJournal(), "Journal ignores script player lock"); map.player.active = true;
                Check(ui.OpenRecentText() && !journal.IsNarrated && journal.ForceInstantExit && journal.CurrentEntry.number == 2, "Recent text must reopen silently and close directly");
                journal.ExitPressed(false); Check(ui.IsClosing, "Recent text back did not close directly"); FinishClose(ui);
                ui.OpenJournal(); journal.ChangeState(JournalState.Notes);
                var note = HplGame.JournalEntries.Find(e => e.type == "Note"); journal.OpenEntry(note, false);
                Check(note.read && journal.State == JournalState.OpenNote && journal.Pages.Count > 1, "Ordinary note did not paginate / mark read");
                journal.TurnPage(1); Check(journal.PageIndex == 1, "Reading page navigation failed");
                journal.ExitPressed(false); Check(journal.State == JournalState.Notes && !ui.IsClosing, "Reader back must return to notes");
                journal.ExitPressed(false); Check(journal.State == JournalState.Main, "List back must return to journal main"); ui.Resume();

                // cLuxJournal::ExitPressed: the RecentlyReadText hint on every direct exit, none when backing out of the main page.
                map.hud.Hints.Update(100); map.hud.Hints.Remove("RecentlyReadText");
                ui.OpenJournal(); journal.ExitPressed(false); FinishClose(ui);
                Check(!HplGame.HintsGiven.Contains("RecentlyReadText"), "Backing out of the journal main page gave the recent text hint");
                ui.OpenJournal(); journal.ChangeState(JournalState.Notes); journal.ExitPressed(true); FinishClose(ui);
                Check(HplGame.HintsGiven.Contains("RecentlyReadText") && !ui.IsPaused, "Direct journal exit (J / M / N / Tab) did not give the recent text hint");
                map.hud.Hints.Update(100);

                HplGame.Quests.Clear(); HplGame.AddQuest("active", "00Trail"); HplGame.AddQuest("done", "00Trail"); HplGame.DisableQuest("done");
                ui.OpenJournal(JournalState.QuestLog);
                Check(journal.VisibleEntries.Count == 1 && journal.VisibleEntries[0].rows.Count > 0 && journal.ForceInstantExit, "Quest log did not filter completed entries / open directly"); ui.Resume();
                for (int i = 0; i < 13; i++) HplGame.AddJournalEntry("Note", "CH01L03_VillageTales", "journal/default_note");
                ui.OpenJournal(); journal.ChangeState(JournalState.Notes);
                Check(journal.PageCount == 2 && journal.PageIndex == 1 && journal.VisibleEntries.Count == 2, "Journal list pagination / newest-page default incorrect"); ui.Resume();

                map.effects.PlayVoice.AddVoice("journal_test", "", "world voice", false, Vector3.zero, 0, 0); map.effects.PlayVoice.Update(0);
                var worldVoice = (SoundEntry)typeof(HplEffects.PlayVoiceEffect).GetField("_voiceEntry", Private).GetValue(map.effects.PlayVoice);
                var ambient = map.sound.PlayGui("journal_test", true, 1, SoundEntryType.World);
                Check(ui.OpenInventory(), "Cannot open inventory for journal transition");
                ui.Inventory.PointerDown(HplInventory.Journal, Vector2.zero);
                Check(ui.JournalOpen && !ui.InventoryOpen && journal.OpenedFromInventory && worldVoice.IsPaused && !map.sound.MenuPaused, "Inventory → journal transition lost sound policy");
                map.sound.StepMenu(0.1f); map.sound.StepMenu(0.1f);
                Check(ambient.ElapsedTime > 0 && worldVoice.ElapsedTime == 0, "Journal must advance ambience while pausing the world voice");
                journal.ExitPressed(true); FinishClose(ui);
                Check(ui.InventoryOpen && ui.InventoryFromJournal && !ui.JournalOpen && ui.IsPaused && Time.timeScale == 0 && worldVoice.IsPaused && map.player.UiInputBlocked, "Returning to inventory lost fade state or briefly resumed gameplay / voices");
                ui.Resume(); Check(Time.timeScale == 0.8f && !worldVoice.IsPaused, "Final inventory exit lost pre-journal state");

                ui.OpenJournal(); journal.Tick(1); journal.View.Refresh(1, Vector2.zero); Canvas.ForceUpdateCanvases();
                int vertices = 0;
                foreach (var text in journal.View.Canvas.GetComponentsInChildren<HplGuiText>()) vertices += text.canvasRenderer.GetMesh()?.vertexCount ?? 0;
                Check(vertices > 0, "Journal text produced no Canvas geometry");
                typeof(HplGameUi).GetMethod("OnDisable", Private).Invoke(ui, null);
                Check(!ui.IsPaused && Time.timeScale == 0.8f && !map.player.UiInputBlocked, "Disable left journal paused");
                HplGame.Reset(); Check(HplGame.LastReadEntry == null && HplGame.JournalEntries.Count == 0, "New game retained journal / recent text state");
                string report = $"PASS: {checks} journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).";
                Directory.CreateDirectory("Logs/JournalCheck"); File.WriteAllText("Logs/JournalCheck/Tests.txt", report); Debug.Log(report);
            }
            finally
            {
                root.GetComponent<HplGameUi>()?.Resume();
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(bank); UnityEngine.Object.DestroyImmediate(clip);
                Time.timeScale = scale; Cursor.lockState = cursorLock; Cursor.visible = cursorVisible;
                HplGame.Reset(); typeof(HplMap).GetProperty(nameof(HplMap.Current)).SetValue(null, previousMap);
            }
        }
    }
}
