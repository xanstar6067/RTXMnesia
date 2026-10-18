using RTXMnesia.Dynamics;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_OilBarrel (LuxProp_OilBarrel.cpp): interaction tops up the lantern oil. The barrel's oil is
    /// calculated on the first look or use: 10 + what the player lacks of 100 counting the lantern and the oil
    /// potions in the inventory (0..90). A small rest under 1 empties it. Hard mode (always 10) is not ported.
    /// </summary>
    public sealed class PropOilBarrel : HplProp
    {
        string _fillSound = "", _emptySound = "";

        // Saved (cLuxProp_OilBarrel_SaveData).
        bool _amountCalculated;
        float _oilAmount;

        public PropOilBarrel(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        public float OilAmount => _oilAmount;
        public bool AmountCalculated => _amountCalculated;

        protected override void LoadVariables()
        {
            _fillSound = VarString("FillSound", "");
            _emptySound = VarString("EmptySound", "");
        }

        public override bool CanInteract(HplPhysicsBody body) => true;

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            CalculateOilAmount();
            var player = Map.player;
            var hud = Map.hud;
            float oil = player.lampOil;

            // Empty barrel or full lantern.
            if (_oilAmount <= 0f)
            {
                PlaySound("EmptyBarell", _emptySound, true, false);
                hud.Messages.SetMessage(hud.Translate("Game", "OilBarrel_Empty"), 0f);
                return false;
            }
            if (oil > 99f)
            {
                hud.Messages.SetMessage(hud.Translate("Game", "OilBarrel_LanternFull"), 0f);
                return false;
            }

            oil += _oilAmount;
            float fillAmount;
            if (oil > 100f)
            {
                fillAmount = _oilAmount - (oil - 100f);
                _oilAmount = oil - 100f;
                if (_oilAmount < 1f) _oilAmount = 0f; // very little left: empty
                oil = 100f;
            }
            else
            {
                fillAmount = _oilAmount;
                _oilAmount = 0f;
            }
            player.SetLampOil(oil);
            PlaySound("FillLantern", _fillSound, true, false);

            string entry;
            if (oil >= 100f) entry = "OilBarrel_Refill_Full";
            else if (fillAmount < 25f) entry = "OilBarrel_Refill_Amount_025";
            else if (fillAmount < 50f) entry = "OilBarrel_Refill_Amount_050";
            else if (fillAmount < 75f) entry = "OilBarrel_Refill_Amount_075";
            else entry = "OilBarrel_Refill_Amount_100";
            hud.Messages.SetMessage(hud.Translate("Game", entry), 0f);
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) => FocusCrosshair.Grab;

        public override string GetFocusText()
        {
            CalculateOilAmount();
            string entry;
            if (_oilAmount <= 0f) entry = "OilBarrel_FocusText_Empty";
            else if (_oilAmount < 25f) entry = "OilBarrel_FocusText_025";
            else if (_oilAmount < 50f) entry = "OilBarrel_FocusText_050";
            else if (_oilAmount < 75f) entry = "OilBarrel_FocusText_075";
            else entry = "OilBarrel_FocusText_100";
            return Map.hud.Translate("Game", entry);
        }

        void CalculateOilAmount()
        {
            if (_amountCalculated) return;
            // HARDMODE: always 10.
            if (HplDifficulty.HardMode)
            {
                _oilAmount = 10f;
                _amountCalculated = true;
                return;
            }
            _oilAmount = 10f; // base amount
            float totalOil = Map.player.lampOil;
            foreach (var item in HplGame.Items)
                if (item.type == "LampOil") totalOil += item.amount * item.count;
            float add = Mathf.Clamp(100f - totalOil, 0f, 90f);
            _oilAmount += add;
            _amountCalculated = true;
        }
    }
}
