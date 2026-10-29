using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// cLuxCompletionCountHandler (LuxCompletionCountHandler.cpp): the completion values of game.cfg [Quest] and the
    /// counter shown when the completion of the map grows: fades in after the delay, counts the shown percentage up
    /// by 3 % a second (the distance term of the original is commented out), stays 2 s, fades out. Drawn by the HUD
    /// at (400, 20), size 21, "12.5%" right of the center and " Completed" left of it (menu.cfg CompletionCounter
    /// font). Frictional left the counter switched off (Update, OnDraw and ShowCompletionIncrease return at once):
    /// <see cref="ShowCounter"/> turns it on here; the counting itself is the same either way.
    /// </summary>
    public sealed class HplCompletionCount
    {
        /// <summary>Opt in to the counter and level door percentages; hidden by default, as in the original game.</summary>
        public static bool ShowCounter = false;

        public readonly int QuestValue, ItemValue, NoteValue, DiaryValue, FlashbackValue, ChestValue;

        bool _active;
        float _alpha, _delay, _newAmount, _displayedAmount, _disableDelay;

        public float Alpha => _alpha;
        public float Percent => _displayedAmount * 100f;
        public bool Active => _active;

        public HplCompletionCount(HplConfig gameConfig)
        {
            if (gameConfig == null) return;
            QuestValue = gameConfig.GetInt("Quest", "QuestCompletionValue", 0);
            ItemValue = gameConfig.GetInt("Quest", "ItemCompletionValue", 0);
            NoteValue = gameConfig.GetInt("Quest", "NoteCompletionValue", 0);
            DiaryValue = gameConfig.GetInt("Quest", "DiaryCompletionValue", 0);
            FlashbackValue = gameConfig.GetInt("Quest", "FlashbackCompletionValue", 0);
            ChestValue = gameConfig.GetInt("Quest", "ChestCompletionValue", 0);
        }

        public void Reset()
        {
            _active = false;
            _alpha = 0f;
            _delay = 0f;
            _newAmount = 0f;
            _displayedAmount = 0f;
        }

        public void Update(float timeStep)
        {
            if (!ShowCounter) return;
            if (!_active)
            {
                _alpha = Mathf.Max(0f, _alpha - 0.5f * timeStep);
                return;
            }
            if (_delay > 0f)
            {
                _delay -= timeStep;
                return;
            }
            _alpha = Mathf.Min(1f, _alpha + 1.2f * timeStep);
            if (_alpha > 0.2f)
            {
                if (_displayedAmount < _newAmount)
                {
                    float distance = 0f; // (mfNewAmount - mfDisplayedAmount) is commented out in the original
                    _displayedAmount += (distance + 0.001f) * 30f * timeStep;
                    if (_displayedAmount > _newAmount) _displayedAmount = _newAmount;
                }
                else
                {
                    _disableDelay -= timeStep;
                    if (_disableDelay <= 0f) _active = false;
                }
            }
        }

        /// <summary>cLuxCompletionCountHandler::ShowCompletionIncrease (fractions 0..1).</summary>
        public void ShowCompletionIncrease(float previous, float next, float delay)
        {
            if (!ShowCounter || previous == next) return;
            _active = true;
            // If the count is already shown, the start value is kept.
            if (_alpha <= 0f)
            {
                _delay = delay;
                _displayedAmount = previous;
            }
            _newAmount = next;
            _disableDelay = 2f;
        }
    }
}
