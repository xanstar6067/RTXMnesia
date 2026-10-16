using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>eLuxPlayerState (LuxTypes.h). HandObject (melee / tinderbox hand objects) is not ported.</summary>
    public enum PlayerStateType { Normal, HandObject, UseItem, InteractGrab, InteractPush, InteractSlide, InteractSwingDoor, InteractLever, InteractWheel, Ladder }

    /// <summary>eLuxPlayerAction (input actions the states react to).</summary>
    public enum PlayerAction { Interact, Attack, Ignite, Holster, Lantern }

    /// <summary>cLuxPlayerStateVars: what the next interact state works on.</summary>
    public static class PlayerStateVars
    {
        public static HplPhysicsBody InteractBody;
        public static HplProp InteractProp;
        /// <summary>The focus point, HPL2 space.</summary>
        public static Vector3 InteractPos;
        public static HplGame.Item InventoryItem;
        /// <summary>The ladder area and the body position to attach at (cLuxPlayerStateVars::SetupLadder).</summary>
        public static HplArea Ladder;
        public static Vector3 LadderStartPos;

        public static void Reset()
        {
            InteractBody = null; InteractProp = null; InventoryItem = null; InteractPos = Vector3.zero;
            Ladder = null; LadderStartPos = Vector3.zero;
        }

        public static void SetupLadder(HplArea ladder, Vector3 startPos)
        {
            Ladder = ladder;
            LadderStartPos = startPos;
        }

        public static void SetupInteraction(HplPhysicsBody body, Vector3 hplInteractPos)
        {
            InteractBody = body;
            if (body?.UserData is HplProp prop) InteractProp = prop;
            InteractPos = hplInteractPos;
        }
    }

    /// <summary>
    /// iLuxPlayerState (LuxPlayerState.h): the player's interaction mode. Input hooks return true to let the move
    /// state / camera handle the input too. <see cref="GetCrosshair"/>: null draws none.
    /// </summary>
    public abstract class PlayerState
    {
        protected readonly AmnesiaPlayer Player;
        public readonly PlayerStateType Type;
        public PlayerStateType PreviousState = PlayerStateType.Normal;

        protected PlayerState(AmnesiaPlayer player, PlayerStateType type)
        {
            Player = player;
            Type = type;
        }

        public abstract void OnEnterState(PlayerStateType previous);
        public abstract void OnLeaveState(PlayerStateType next);
        public virtual void Update(float timeStep) { }
        public virtual void PostUpdate(float timeStep) { }
        public virtual bool OnDoAction(PlayerAction action, bool pressed) => true;
        public virtual void OnScroll(float amount) { }
        public virtual bool OnMove(CharDir dir, float mul) => true;
        public virtual bool OnAddYaw(float amount) => true;
        public virtual bool OnAddPitch(float amount) => true;
        public virtual bool OnRun(bool pressed) => true;
        public virtual bool OnCrouch(bool pressed) => true;
        public virtual bool OnJump(bool pressed) => true;
        public virtual FocusCrosshair? GetCrosshair() => FocusCrosshair.Default;
        public virtual void OnDestroyEntity(HplEntity entity) { }
        /// <summary>Debug line for the simulation log.</summary>
        public virtual string Describe() => Type.ToString();
        /// <summary>iLuxPlayerState::AllowLantern (false only on a ladder).</summary>
        public virtual bool AllowLantern => true;
        /// <summary>iLuxPlayerState::AllowBuoyancy (false for the prop in the player's hands).</summary>
        public virtual bool AllowBuoyancy(Dynamics.HplPhysicsBody body) => true;
    }

    /// <summary>cLuxPlayer::ChangeState and the state list.</summary>
    public sealed class PlayerStates
    {
        readonly Dictionary<PlayerStateType, PlayerState> _states = new();
        readonly AmnesiaPlayer _player;

        public PlayerState Current { get; private set; }
        public PlayerStateType CurrentType => Current.Type;

        public PlayerStates(AmnesiaPlayer player)
        {
            _player = player;
            Add(new PlayerStateNormal(player));
            Add(new PlayerStateUseItem(player));
            Add(new PlayerStateInteractGrab(player));
            Add(new PlayerStateInteractPush(player));
            Add(new PlayerStateInteractSlide(player));
            Add(new PlayerStateInteractSwingDoor(player));
            Add(new PlayerStateInteractLever(player));
            Add(new PlayerStateInteractWheel(player));
            Add(new PlayerStateLadder(player));
            Current = _states[PlayerStateType.Normal];
        }

        void Add(PlayerState state) => _states[state.Type] = state;

        public T Get<T>(PlayerStateType type) where T : PlayerState => _states.TryGetValue(type, out var s) ? s as T : null;

        public void ChangeState(PlayerStateType type)
        {
            if (Current.Type == type) return;
            if (!_states.TryGetValue(type, out var next))
            {
                Debug.LogWarning($"[RTXMnesia] Player state {type} is not ported yet");
                return;
            }
            Current.OnLeaveState(type);
            var previous = Current.Type;
            Current = next;
            Current.PreviousState = previous;
            Current.OnEnterState(previous);
        }
    }
}
