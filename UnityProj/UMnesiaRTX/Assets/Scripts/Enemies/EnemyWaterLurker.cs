using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Enemies
{
    /// <summary>
    /// The water lurker (cLuxEnemy_WaterLurker, LuxEnemy_WaterLurker.cpp, EntityType Enemy_WaterLurker): an invisible
    /// enemy without animations (its mesh is hidden) that hunts the player while his feet are within
    /// PlayerDetectionHeight of its own (in the water), splashing as it moves; investigates loud sounds, goes for
    /// food (IsFood objects in its height) and eats it, pauses after an attack, breaks doors. No sanity loss while not
    /// hunting. Every state handler is a line by line port of the original's state machine.
    /// </summary>
    public sealed class EnemyWaterLurker : HplEnemy
    {
        enum Splash { Walk, Run, Eat, Attack }

        float _playerDetectionHeight, _eatDamage, _afterAttackPauseTime;
        readonly string[] _splashPS = new string[4], _splashSound = new string[4];

        public EnemyWaterLurker(AmnesiaEntity entity) : base(entity)
        {
            CausesSanityDecrease = false;
            CausesSanityDecreaseAsDefault = false;
        }

        public float PlayerDetectionHeight => _playerDetectionHeight;

        /// <summary>cLuxEnemyLoader_WaterLurker::LoadVariables (preloading is not needed).</summary>
        protected override void LoadVariables()
        {
            _playerDetectionHeight = VarFloat("PlayerDetectionHeight", 0.6f);
            _eatDamage = VarFloat("EatDamage");
            _afterAttackPauseTime = VarFloat("AfterAttackPauseTime");
            _splashPS[(int)Splash.Walk] = VarString("SplashPS_Walk");
            _splashPS[(int)Splash.Run] = VarString("SplashPS_Run");
            _splashPS[(int)Splash.Eat] = VarString("SplashPS_Eat");
            _splashPS[(int)Splash.Attack] = VarString("SplashPS_Attack");
            _splashSound[(int)Splash.Walk] = VarString("SplashSound_Walk");
            _splashSound[(int)Splash.Run] = VarString("SplashSound_Run");
            _splashSound[(int)Splash.Eat] = VarString("SplashSound_Eat");
            _splashSound[(int)Splash.Attack] = VarString("SplashSound_Attack");
        }

        /// <summary>cLuxEnemyLoader_WaterLurker::LoadInstanceVariables: a positive PlayerDetectionHeight of the map wins.</summary>
        protected override void LoadInstanceVariables()
        {
            string s = InstVar("PlayerDetectionHeight");
            float height = s != null ? HplMath.Atof(s) : 0f;
            if (height > 0f) _playerDetectionHeight = height;
        }

        /// <summary>OnSetupAfterLoad / OnSetActiveEnemySpecific: the mesh stays invisible.</summary>
        protected override void OnSetupAfterLoad() => HideMesh();
        protected override void OnSetActiveEnemySpecific(bool active) => HideMesh();

        void HideMesh()
        {
            foreach (var r in GameObject.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }

        Sound.HplMusicHandler Music => Map.sound?.Music;

        void DebugMessage(string text)
        {
            if (Map.logDebugMessages) Debug.Log("[Script debug] " + text);
        }

        void HearSound(EnemyStateMessage message)
        {
            if (message.customFloat > HearVolume)
            {
                ChangeState(EnemyState.Investigate);
                TempPos = message.customVector;
                TempVal = message.customFloat;
            }
        }

        protected override bool StateEventImplement(int state, EnemyStateEvent ev, EnemyStateMessage message)
        {
            bool Msg(EnemyMessage type) => ev == EnemyStateEvent.Message && message != null && message.type == type;
            bool Enter = ev == EnemyStateEvent.Enter, Leave = ev == EnemyStateEvent.Leave, Update = ev == EnemyStateEvent.Update;

            switch (state)
            {
                // ---------------------------------------------------------- Default
                case -1:
                    if (Msg(EnemyMessage.PlayerDetected))
                    {
                        ChangeState(EnemyState.Hunt);
                        return true;
                    }
                    if (Msg(EnemyMessage.TakeHit)) return true; // does nothing
                    if (Msg(EnemyMessage.PlayerDead))
                    {
                        ChangeState(EnemyState.Idle);
                        return true;
                    }
                    if (Msg(EnemyMessage.FoodInRange))
                    {
                        var node = Pathfinder.GetNodeAtPos(message.customVector, 0f, 1.5f, true, true, true, null);
                        if (node != null)
                        {
                            DebugMessage("Found food!");
                            TempPos = node.Position;
                            ChangeState(EnemyState.Search);
                            Music?.RemoveEnemy(EnemyMusic.Attack, this);
                        }
                        else DebugMessage("Found food but no node..");
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Idle
                case (int)EnemyState.Idle:
                    if (Enter)
                    {
                        ChangeSoundState(EnemySoundState.Idle);
                        return true;
                    }
                    if (Msg(EnemyMessage.SoundHeard))
                    {
                        HearSound(message);
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Go Home
                case (int)EnemyState.GoHome:
                    if (Enter)
                    {
                        ChangeSoundState(EnemySoundState.Idle);
                        var node = Pathfinder.GetNodeAtPos(GetPlayerFeetPos(), 0f, 6f, false, false, true, null);
                        Pathfinder.MoveTo(node != null ? node.Position : StartPosition);
                        SendMessage(EnemyMessage.TimeOut_2, 0.1f, true);
                        return true;
                    }
                    if (Update)
                    {
                        if (Mover.StuckCounter > 0.9f)
                        {
                            ChangeState(EnemyState.Idle);
                            Music?.RemoveEnemy(EnemyMusic.Attack, this);
                        }
                        return true;
                    }
                    if (Msg(EnemyMessage.SoundHeard))
                    {
                        HearSound(message);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        SplashWater(Splash.Walk);
                        SendMessage(EnemyMessage.TimeOut_2, 1f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        ChangeState(EnemyState.Idle);
                        Music?.RemoveEnemy(EnemyMusic.Attack, this);
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Investigate (heard a sound)
                case (int)EnemyState.Investigate:
                    if (Enter)
                    {
                        DebugMessage("Sound Heard! Vol: " + HplMath.ToString(TempVal));
                        var node = Pathfinder.GetNodeAtPos(TempPos, 0f, 10f, true, true, true, null, 1);
                        if (node != null) Pathfinder.MoveTo(node.Position);
                        else
                        {
                            DebugMessage("Could not find node near sound!");
                            Pathfinder.MoveTo(TempPos);
                        }
                        SendMessage(EnemyMessage.TimeOut_2, 0.1f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        ChangeState(EnemyState.Idle);
                        return true;
                    }
                    if (Update)
                    {
                        if (Mover.StuckCounter > 0.9f) ChangeState(EnemyState.Idle);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        SplashWater(Splash.Walk);
                        SendMessage(EnemyMessage.TimeOut_2, 1f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.SoundHeard))
                    {
                        // A louder sound: a node near the previous position again (the original does not take the new one).
                        if (Pathfinder.IsMoving && message.customFloat > TempVal)
                        {
                            var node = Pathfinder.GetNodeAtPos(TempPos, 0f, 10f, true, true, true, null, 1);
                            if (node != null) Pathfinder.MoveTo(node.Position);
                            TempVal = message.customFloat;
                        }
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Search (scavenge for food)
                case (int)EnemyState.Search:
                    if (Enter)
                    {
                        ChangeSoundState(EnemySoundState.Idle);
                        Pathfinder.MoveTo(TempPos);
                        SendMessage(EnemyMessage.TimeOut_2, 0.1f, true);
                        return true;
                    }
                    if (Update) return true;
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        SplashWater(Splash.Walk);
                        SendMessage(EnemyMessage.TimeOut_2, 1f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        DebugMessage("to eat state!\n");
                        ChangeState(EnemyState.Eat);
                        return true;
                    }
                    if (Msg(EnemyMessage.FoodInRange)) return true;
                    return false;

                // ---------------------------------------------------------- Eat
                case (int)EnemyState.Eat:
                    if (Enter)
                    {
                        ChangeSoundState(EnemySoundState.Idle);
                        SendMessage(EnemyMessage.TimeOut, 0.1f, true);
                        TempPos = Body.FeetPosition;
                        Pathfinder.Stop();
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        // Search for food and eat it.
                        var food = GetClosestFood(3.5f, _playerDetectionHeight);
                        if (food != null)
                        {
                            var foodBody = food.PhysicsBodies[0];
                            Vector3 toFeet = (Body.FeetPosition - foodBody.Position).normalized;
                            foodBody.AddImpulse(toFeet * 2.5f);
                            SplashWater(Splash.Eat);
                            food.GiveDamage(_eatDamage, 10);
                            DebugMessage("took bite!!");
                            DebugMessage("Health: " + HplMath.ToString(food.Health));
                            SendMessage(EnemyMessage.TimeOut, 1.2f, true);
                            TempPos = foodBody.Position;
                        }
                        else
                        {
                            DebugMessage("No more food!!");
                            ChangeState(EnemyState.GoHome);
                        }
                        return true;
                    }
                    if (Update)
                    {
                        // Move forward.
                        if (Dist2D(TempPos) > Body.Size.x * 0.75f) Mover.MoveToPos(TempPos);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected))
                    {
                        // Only hunt if the player is really close.
                        if (DistToPlayer2D() < 2f) ChangeState(EnemyState.Hunt);
                        else PlayerDetected = false;
                        return true;
                    }
                    if (Msg(EnemyMessage.FoodInRange)) return true;
                    return false;

                // ---------------------------------------------------------- Hunt
                case (int)EnemyState.Hunt:
                    if (Enter)
                    {
                        if (!PlayerIsDetected())
                        {
                            ChangeState(EnemyState.GoHome);
                            CausesSanityDecrease = false;
                        }
                        else if (PreviousState == EnemyState.AttackMeleeShort) ChangeState(EnemyState.HuntPause);
                        else
                        {
                            ChangeSoundState(EnemySoundState.Hunt);
                            SetMoveSpeed(EnemyMoveSpeed.Run);
                            SendMessage(EnemyMessage.TimeOut, 0.1f, true);
                            SendMessage(EnemyMessage.TimeOut_2, 0.1f, true);
                            Music?.AddEnemy(EnemyMusic.Attack, this);
                            CausesSanityDecrease = true;
                        }
                        return true;
                    }
                    if (Leave)
                    {
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        if (Mover.GetWantedSpeedAmount() > 0.2f) SplashWater(Splash.Run);
                        SendMessage(EnemyMessage.TimeOut_2, 0.6f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        // Update the player position.
                        Pathfinder.MoveTo(Map.player.Body.FeetPosition);
                        SendMessage(EnemyMessage.TimeOut, 0.4f, true);
                        if (!PlayerIsDetected())
                        {
                            ChangeState(EnemyState.GoHome);
                            CausesSanityDecrease = false;
                        }
                        return true;
                    }
                    if (Update)
                    {
                        if (DistToPlayer() < NormalAttackDistance) ChangeState(EnemyState.AttackMeleeShort);
                        else if (StuckAtDoor)
                        {
                            TempPos = StuckDoor != null ? StuckDoor.AttachTransform.position : TempPos;
                            ChangeState(EnemyState.BreakDoor);
                        }
                        return true;
                    }
                    if (Msg(EnemyMessage.FoodInRange)) return true;
                    return false;

                // ---------------------------------------------------------- Hunt Pause
                case (int)EnemyState.HuntPause:
                    if (Enter)
                    {
                        Pathfinder.Stop();
                        SendMessage(EnemyMessage.TimeOut, _afterAttackPauseTime, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        ChangeState(EnemyState.Hunt);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.FoodInRange)) return true;
                    return false;

                // ---------------------------------------------------------- Break door
                case (int)EnemyState.BreakDoor:
                    if (Enter)
                    {
                        Pathfinder.Stop();
                        SendMessage(EnemyMessage.TimeOut, 0.01f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        Attack(NormalAttackSize, BreakDoorAttackDamage);
                        SendMessage(EnemyMessage.TimeOut_2, HplMath.RandRectf(0.5f, 1.5f), true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        // The broken door check is commented out in the original.
                        ChangeState(EnemyState.Hunt);
                        return true;
                    }
                    if (Update)
                    {
                        Mover.TurnToPos(TempPos);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.FoodInRange)) return true;
                    return false;

                // ---------------------------------------------------------- Attack Short
                case (int)EnemyState.AttackMeleeShort:
                    if (Enter)
                    {
                        SendMessage(EnemyMessage.TimeOut, 0.3f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        ChangeState(PreviousState);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        SplashWater(Splash.Attack);
                        Attack(NormalAttackSize, NormalAttackDamage);
                        SendMessage(EnemyMessage.TimeOut_2, 0.5f, true);
                        Pathfinder.Stop();
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.FoodInRange)) return true;
                    return false;

                // ---------------------------------------------------------- Dead (just in case)
                case (int)EnemyState.Dead:
                    if (Enter)
                    {
                        ChangeSoundState(EnemySoundState.Silent);
                        Pathfinder.Stop();
                        Map.player?.RemoveTerrorEnemy(this);
                        Music?.RemoveEnemy(EnemyMusic.Attack, this);
                        Body.SetBodyActive(false);
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationOver) || Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.HelpMe)) return true;
                    return false;

                default:
                    return UndefinedState(state);
            }
        }

        /// <summary>cLuxEnemy_WaterLurker::InRangeOfFood: the food body within the detection height of the feet.</summary>
        public override bool InRangeOfFood(HplPhysicsBody foodBody) =>
            Mathf.Abs(foodBody.Position.y - Body.FeetPosition.y) <= _playerDetectionHeight;

        /// <summary>cLuxEnemy_WaterLurker::SplashWater: the type's particle system 70 cm above the feet and its sound.</summary>
        void SplashWater(Splash type)
        {
            string ps = _splashPS[(int)type];
            if (ps != "" && Map.ParticleWorld != null)
            {
                var system = Map.ParticleWorld.Create("LurkerSplash", ps);
                if (system != null) system.LocalMatrix = Matrix4x4.Translate(Body.FeetPosition + new Vector3(0f, 0.7f, 0f));
            }
            PlaySound(_splashSound[(int)type]);
        }

        /// <summary>The player's feet within the detection height of the lurker's.</summary>
        protected override bool PlayerIsDetected() => AbsHeightDistToPlayer() <= _playerDetectionHeight;

        protected override float GetDamageMul(float amount, int strength)
        {
            if (CurrentState == EnemyState.Idle || CurrentState == EnemyState.Wait || CurrentState == EnemyState.Patrol ||
                CurrentState == EnemyState.Investigate)
                return 1f;
            if (CurrentState == EnemyState.Hurt) return 0.5f;
            return 0.2f;
        }
    }
}
