using System.Collections.Generic;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// The draft of the key config window (cLuxMainMenu_KeyConfig / cLuxInputMenuEntry): the inputs of every
    /// configurable action by category, the slot waiting for an input, the marking of empty and shared inputs.
    /// Rules of the original: an input the action already has in its other slot is not taken; empty and duplicate
    /// inputs are checked only within a category, and the first action of a category is never marked empty (the
    /// check starts from the second entry); OK with invalid inputs asks first.
    /// </summary>
    public sealed class HplKeyConfig
    {
        public sealed class Entry
        {
            public string name;
            public HplKeyBindings.Category category;
            public readonly string[] inputs = new string[2];
            public readonly bool[] invalid = new bool[2];
        }

        public readonly List<Entry> Entries = new();
        public int Category { get; set; }
        /// <summary>The entry (index in <see cref="Entries"/>) and slot waiting for an input, -1: none.</summary>
        public int WaitingEntry { get; private set; } = -1;
        public int WaitingSlot { get; private set; }
        /// <summary>The frame the waiting began: its own click is not the new input.</summary>
        public int WaitingFrame { get; private set; }
        public bool HasInvalidInputs { get; private set; }
        public bool IsWaiting => WaitingEntry >= 0;

        /// <summary>cLuxMainMenu_KeyConfig::SetInitialValues: the current bindings.</summary>
        public void Begin()
        {
            Entries.Clear();
            foreach (var a in HplKeyBindings.Actions)
            {
                var e = new Entry { name = a.name, category = a.category };
                e.inputs[0] = a.primary;
                e.inputs[1] = a.secondary;
                Entries.Add(e);
            }
            Category = 0;
            WaitingEntry = -1;
            Check();
        }

        public IEnumerable<int> EntriesOf(int category)
        {
            for (int i = 0; i < Entries.Count; i++)
                if ((int)Entries[i].category == category) yield return i;
        }

        /// <summary>cLuxMainMenu_KeyConfig::SetDefaultValues.</summary>
        public void SetDefaults()
        {
            foreach (var e in Entries)
            {
                var (p, s) = HplKeyBindings.DefaultInputs(e.name);
                e.inputs[0] = p;
                e.inputs[1] = s;
            }
            WaitingEntry = -1;
            Check();
        }

        public void StartWaiting(int entry, int slot, int frame)
        {
            if (IsWaiting) return;
            WaitingEntry = entry;
            WaitingSlot = slot;
            WaitingFrame = frame;
        }

        public void CancelWaiting() => WaitingEntry = -1;

        /// <summary>cLuxInputMenuEntry::SaveSubAction: the input goes to the waiting slot unless the action already has it.</summary>
        public void Assign(string input)
        {
            if (!IsWaiting) return;
            var e = Entries[WaitingEntry];
            if (e.inputs[0] != input && e.inputs[1] != input) e.inputs[WaitingSlot] = input;
            WaitingEntry = -1;
            Check();
        }

        /// <summary>cLuxMainMenu_KeyConfig::CheckEmptyAndDuplicateInputs.</summary>
        public void Check()
        {
            HasInvalidInputs = false;
            foreach (var e in Entries) e.invalid[0] = e.invalid[1] = false;
            for (int cat = 0; cat < 3; cat++)
            {
                var list = new List<Entry>();
                foreach (int i in EntriesOf(cat)) list.Add(Entries[i]);
                for (int i = 0; i < list.Count; i++)
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        var a = list[i];
                        var b = list[j];
                        if (string.IsNullOrEmpty(b.inputs[0]) && string.IsNullOrEmpty(b.inputs[1]))
                        {
                            HasInvalidInputs = true;
                            b.invalid[0] = b.invalid[1] = true;
                            continue;
                        }
                        for (int s = 0; s < 2; s++)
                        {
                            if (!string.IsNullOrEmpty(a.inputs[s]) && (a.inputs[s] == b.inputs[0] || a.inputs[s] == b.inputs[1]))
                            {
                                HasInvalidInputs = true;
                                a.invalid[s] = true;
                            }
                            if (!string.IsNullOrEmpty(b.inputs[s]) && (b.inputs[s] == a.inputs[0] || b.inputs[s] == a.inputs[1]))
                            {
                                HasInvalidInputs = true;
                                b.invalid[s] = true;
                            }
                        }
                    }
            }
        }

        /// <summary>cLuxMainMenu_KeyConfig::ApplyChanges.</summary>
        public void Apply()
        {
            var list = new List<(string, string, string)>();
            foreach (var e in Entries) list.Add((e.name, e.inputs[0] ?? "", e.inputs[1] ?? ""));
            HplKeyBindings.Apply(list);
        }
    }
}
