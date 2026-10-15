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
        public bool Highlight => InRange && (Player.EntityInFocus is HplProp && Player.EntityInFocus is not Props.PropItem ||
            HplMap.Current?.Script?.HasUseItemCallback(Item?.name, Player.EntityInFocus.Name) == true);

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
