using RTXMnesia.Dynamics;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>cLuxPlayerState_UseItem: shared occluded focus ray, item cursor, one attempt on either mouse button.</summary>
    public sealed class PlayerStateUseItem : PlayerStateNormal
    {
        public HplGame.Item Item { get; private set; }
        protected override string FocusItemName => Item?.name;
        float MinDistance => Player.gameConfig != null ? Player.gameConfig.GetFloat("Player_Interaction", "MinUseItemDistance", 0) : 0;
        bool InRange => Player.EntityInFocus != null && Player.FocusDistance < Mathf.Max(Player.EntityInFocus.MaxFocusDistance, MinDistance);

        /// <summary>
        /// cLuxPlayerState_UseItem::OnDrawCrossHair: the icon glows over any prop but an item, and over other entities
        /// only with a use-item callback (props need none). Distance check uses ≤ like the draw code.
        /// </summary>
        public bool Highlight
        {
            get
            {
                var entity = Player.EntityInFocus;
                if (entity == null || Player.FocusDistance > Mathf.Max(entity.MaxFocusDistance, MinDistance)) return false;
                if (entity is HplProp) return entity is not Props.PropItem;
                return HplMap.Current?.Script?.HasUseItemCallback(Item?.name, entity.Name) == true;
            }
        }

        /// <summary>mFlashOscill: the glow's brightness (0..1 at 1.5 per second), drawn additively three times.</summary>
        public float FlashValue => _flashOscill.val;
        Ui.HplHud.LinearOscillation _flashOscill = new(0f, 1f, 0f, 1.5f, 1.5f);

        public PlayerStateUseItem(AmnesiaPlayer player) : base(player, PlayerStateType.UseItem) { }
        public override void OnEnterState(PlayerStateType previous)
        {
            base.OnEnterState(previous);
            Item = PlayerStateVars.InventoryItem;
            PlayerStateVars.InventoryItem = null;
        }
        public override void OnLeaveState(PlayerStateType next)
        {
            Item = null;
            base.OnLeaveState(next);
        }
        protected override bool ShowOutlineOnEntity(HplEntity entity, HplPhysicsBody body) => false;
        public override void Update(float timeStep)
        {
            if (Item == null || !HplGame.Items.Contains(Item)) { Player.States.ChangeState(PlayerStateType.Normal); return; }
            base.Update(timeStep);
            _flashOscill.Update(timeStep);
        }
        public override FocusCrosshair? GetCrosshair() => null;
        public override bool OnDoAction(PlayerAction action, bool pressed)
        {
            if (pressed && (action == PlayerAction.Interact || action == PlayerAction.Attack))
            {
                var map = HplMap.Current;
                if (map != null && Item != null && HplGame.Items.Contains(Item))
                {
                    string error = !InRange ? "UseItemHasNoObject" : map.Script?.TryUseItem(Item.name, Player.EntityInFocus.Name) == true ? "" : "UseItemDoesNotWork";
                    if (error != "") map.hud.Messages.SetMessage(map.hud.Translate("Inventory", error), 0);
                }
                Player.States.ChangeState(PlayerStateType.Normal);
                return false;
            }
            return true;
        }
        public override string Describe() => "UseItem " + (Item?.name ?? "-") + " focus " + (Player.EntityInFocus?.Name ?? "-");
    }
}
