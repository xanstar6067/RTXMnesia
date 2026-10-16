using RTXMnesia.Hpl;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Enemies
{
    /// <summary>
    /// The grunt and the brute (cLuxEnemy_Grunt, LuxEnemy_Grunt.cpp, EntityType Enemy_Grunt): idle until the player comes
    /// in range (activation sound once in 5 s), waiting with idle extras or patrolling the script's nodes, investigating
    /// loud sounds, alert when the player is detected (walks to him, breaks doors at full terror), search around him,
    /// hunt (runs, calls others for help, short and launch attacks, pause after a hit), hurt flinch, death animation.
    /// Every state handler is a line by line port of the original's state machine.
    /// </summary>
    public sealed class EnemyGrunt : HplEnemy
    {
        string _noticeSound = "", _giveUpNoticeSound = "", _enabledSound = "";
        float _groggyDamageCount, _alertToHuntDistance, _alertToInstantHuntDistance, _huntPauseMinTime, _huntPauseMaxTime;
        float _increaseAlertSpeedDistance, _increasedAlertSpeedMul, _alertRunTowardsToHuntLimit, _alertRunTowardsCheckDistance;
        float _idleExtraTimeMin = 2f, _idleExtraTimeMax = 6f;
        int _idleExtraNum = 3;
        float _waitTime, _alertRunTowardsCount;

        public EnemyGrunt(AmnesiaEntity entity) : base(entity)
        {
            AlignEntityWithGroundRay = true;
        }

        /// <summary>cLuxEnemyLoader_Grunt::LoadVariables.</summary>
        protected override void LoadVariables()
        {
            _noticeSound = VarString("NoticeSound");
            _giveUpNoticeSound = VarString("GiveUpNoticeSound");
            _enabledSound = VarString("EnabledSound");
            _groggyDamageCount = VarFloat("GroggyDamageCount");
            _alertToHuntDistance = VarFloat("AlertToHuntDistance");
            _alertToInstantHuntDistance = VarFloat("AlertToInstantHuntDistance");
            _huntPauseMinTime = VarFloat("HuntPauseMinTime");
            _huntPauseMaxTime = VarFloat("HuntPauseMaxTime");
            _increaseAlertSpeedDistance = VarFloat("IncreaseAlertSpeedDistance");
            _increasedAlertSpeedMul = VarFloat("IncreasedAlertSpeedMul");
            _alertRunTowardsToHuntLimit = VarFloat("AlertRunTowardsToHuntLimit");
            _alertRunTowardsCheckDistance = VarFloat("AlertRunTowardsCheckDistance");
            _idleExtraTimeMin = VarFloat("IdleExtraTimeMin", 2f);
            _idleExtraTimeMax = VarFloat("IdleExtraTimeMax", 6f);
            _idleExtraNum = VarInt("IdleExtraNum", 3);
        }

        Sound.HplMusicHandler Music => Map.sound?.Music;
        Player.AmnesiaPlayer ThePlayer => Map.player;

        void DebugMessage(string text)
        {
            if (Map.logDebugMessages) Debug.Log("[Script debug] " + text);
        }

        /// <summary>Stuck at a door: its position, then break it (the closed check is commented out in the original).</summary>
        void BreakStuckDoor()
        {
            TempPos = StuckDoor != null ? StuckDoor.AttachTransform.position : TempPos;
            ChangeState(EnemyState.BreakDoor);
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
                    if (Msg(EnemyMessage.Reset))
                    {
                        Music?.RemoveEnemy(EnemyMusic.Attack, this);
                        ThePlayer?.RemoveTerrorEnemy(this);
                        ChangeState(EnemyState.Wait);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected))
                    {
                        ChangeState(EnemyState.Alert);
                        return true;
                    }
                    if (Msg(EnemyMessage.TakeHit))
                    {
                        DamageCount += message.customFloat;
                        if (DamageCount > _groggyDamageCount)
                        {
                            ChangeState(EnemyState.Hurt);
                            DamageCount = 0f;
                        }
                        return true;
                    }
                    // Out of range after having been in: turned off.
                    if (Msg(EnemyMessage.PlayerOutOfRange))
                    {
                        SetActive(false);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDead))
                    {
                        ChangeState(EnemyState.Idle);
                        return true;
                    }
                    if (Msg(EnemyMessage.HelpMe))
                    {
                        ShowPlayerPosition();
                        ChangeState(EnemyState.Hunt);
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Idle
                case (int)EnemyState.Idle:
                    if (Enter)
                    {
                        PlayerInRangeFlag = false; // new checks are made in case the player is near
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerInRange))
                    {
                        DebugMessage($"Enemy {Name} enabled!");
                        if (HplGame.EnemyActivateSoundAllowed)
                        {
                            HplGame.SetEnemyActivateSoundMade();
                            PlaySound(_enabledSound);
                        }
                        ChangeState(PatrolNodes.Count == 0 ? EnemyState.Wait : EnemyState.Patrol);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected)) return true;
                    return false;

                // ---------------------------------------------------------- Go Home
                case (int)EnemyState.GoHome:
                    if (Enter)
                    {
                        Pathfinder.MoveTo(StartPosition);
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        ChangeState(EnemyState.Idle);
                        return true;
                    }
                    if (Msg(EnemyMessage.SoundHeard))
                    {
                        HearSound(message);
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Wait
                case (int)EnemyState.Wait:
                    if (Enter)
                    {
                        if (_waitTime <= 0f) SendMessage(EnemyMessage.TimeOut, HplMath.RandRectf(1f, 3f), true);
                        else SendMessage(EnemyMessage.TimeOut, _waitTime, true);
                        _waitTime = 0f;
                        SendMessage(EnemyMessage.TimeOut_2, HplMath.RandRectf(_idleExtraTimeMin, _idleExtraTimeMax), true);
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        if (PreviousState == EnemyState.Search)
                        {
                            FadeOutCurrentAnim(0.2f);
                            ChangeState(EnemyState.Search);
                        }
                        else if (PatrolNodes.Count > 0)
                        {
                            FadeOutCurrentAnim(0.2f);
                            ChangeState(EnemyState.Patrol);
                        }
                        else SendMessage(EnemyMessage.TimeOut, HplMath.RandRectf(3f, 5f), true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        PlayAnim("IdleExtra" + HplMath.ToString(HplMath.RandRectl(1, _idleExtraNum)), false, 0.3f);
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationOver))
                    {
                        SendMessage(EnemyMessage.TimeOut_2, HplMath.RandRectf(2f, 6f), true);
                        return true;
                    }
                    if (Msg(EnemyMessage.SoundHeard))
                    {
                        HearSound(message);
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Patrol
                case (int)EnemyState.Patrol:
                    if (Enter)
                    {
                        ChangeSoundState(EnemySoundState.Idle);
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        PatrolUpdateGoal();
                        return true;
                    }
                    if (Update)
                    {
                        if (StuckAtDoor) BreakStuckDoor();
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        PatrolEndOfPath();
                        return true;
                    }
                    if (Msg(EnemyMessage.SoundHeard))
                    {
                        HearSound(message);
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Investigate
                case (int)EnemyState.Investigate:
                    if (Enter)
                    {
                        ChangeSoundState(EnemySoundState.Alert);
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        Pathfinder.Stop();
                        PlayAnim("Notice" + HplMath.ToString(HplMath.RandRectl(1, 2)), false, 0.3f);
                        PlaySound(_noticeSound);
                        FovMul = 4f; // hearing a sound makes the enemy extra alert
                        DebugMessage("Sound Heard! Vol: " + HplMath.ToString(TempVal));
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationOver))
                    {
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        var node = Pathfinder.GetNodeAtPos(TempPos, 0f, 10f, true, true, true, null, 1);
                        if (node != null) Pathfinder.MoveTo(node.Position);
                        else
                        {
                            DebugMessage("Could not find node near sound!");
                            Pathfinder.MoveTo(TempPos);
                        }
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        PlaySound(_giveUpNoticeSound);
                        ChangeState(PreviousState == EnemyState.Search ? EnemyState.Search : EnemyState.Patrol);
                        return true;
                    }
                    if (Update)
                    {
                        if (StuckAtDoor) BreakStuckDoor();
                        return true;
                    }
                    if (Leave)
                    {
                        FovMul = 1f;
                        FadeOutCurrentAnim(0.2f);
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        return true;
                    }
                    if (Msg(EnemyMessage.SoundHeard))
                    {
                        // A louder sound than the previous one: go there instead (at the old position, as in the original).
                        if (Pathfinder.IsMoving && message.customFloat > TempVal)
                        {
                            var node = Pathfinder.GetNodeAtPos(TempPos, 0f, 10f, true, true, true, null, 1);
                            if (node != null) Pathfinder.MoveTo(node.Position);
                            TempVal = message.customFloat;
                        }
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Alert
                case (int)EnemyState.Alert:
                    if (Enter)
                    {
                        ChangeSoundState(EnemySoundState.Alert);
                        SendMessage(EnemyMessage.TimeOut, 0.3f, true);
                        Pathfinder.MoveTo(LastKnownPlayerPos);
                        ThePlayer.AddTerrorEnemy(this);
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        ForwardSpeed *= 1.2f;
                        FovMul = 4f;
                        _alertRunTowardsCount = 0f;
                        return true;
                    }
                    if (Leave)
                    {
                        FovMul = 1f;
                        if (NextState != EnemyState.BreakDoor)
                        {
                            SetMoveSpeed(EnemyMoveSpeed.Walk);
                            ThePlayer.RemoveTerrorEnemy(this);
                        }
                        return true;
                    }
                    if (Update)
                    {
                        float distToPlayer = DistToPlayer();
                        // The player moving toward the enemy
                        if (distToPlayer < _alertRunTowardsCheckDistance) _alertRunTowardsCount += GetPlayerMovementTowardEnemyAmount();
                        else _alertRunTowardsCount -= 1f;
                        if (_alertRunTowardsCount < 0f) _alertRunTowardsCount = 0f;

                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        if (distToPlayer > _increaseAlertSpeedDistance) ForwardSpeed *= 1.2f * _increasedAlertSpeedMul;
                        else ForwardSpeed *= 1.2f;

                        if (ThePlayer.Terror >= 1f && StuckAtDoor) BreakStuckDoor();
                        else if (!PlayerIsDetected())
                        {
                            if (ThePlayer.Terror < 1f) ChangeState(EnemyState.Search);
                        }
                        else if ((ThePlayer.Terror >= 1f && (distToPlayer > _alertToHuntDistance || _alertRunTowardsCount > _alertRunTowardsToHuntLimit)) ||
                                 distToPlayer < _alertToInstantHuntDistance)
                        {
                            ThePlayer.SetTerror(1f);
                            ChangeState(EnemyState.Hunt);
                        }
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        float distToPlayer = DistToPlayer();
                        // Path ended with the player not seen, or stuck (should only happen at a distance).
                        if (!PlayerIsDetected() || (message.customInt == 1 && distToPlayer > 5f)) ChangeState(EnemyState.Search);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        if (PlayerIsDetected()) Pathfinder.MoveTo(LastKnownPlayerPos);
                        SendMessage(EnemyMessage.TimeOut, 0.3f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TakeHit))
                    {
                        ChangeState(EnemyState.Hurt);
                        Music?.RemoveEnemy(EnemyMusic.Search, this);
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Search
                case (int)EnemyState.Search:
                    if (Enter)
                    {
                        ChangeSoundState(EnemySoundState.Alert);
                        SendMessage(EnemyMessage.TimeOut, PlayerSearchTime, true);
                        SendMessage(EnemyMessage.TimeOut_2, HplMath.RandRectf(0f, 1f), true);
                        Music?.RemoveEnemy(EnemyMusic.Attack, this);
                        Music?.AddEnemy(EnemyMusic.Search, this);
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        return true;
                    }
                    if (Leave)
                    {
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        return true;
                    }
                    if (Update)
                    {
                        if (StuckAtDoor) BreakStuckDoor();
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        Pathfinder.Stop();
                        SendMessage(EnemyMessage.TimeOut_2, HplMath.RandRectf(1f, 3f), true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        var node = Pathfinder.GetNodeAtPos(ThePlayer.Body.FeetPosition, 4f, 12f, false, false, true, null);
                        if (node != null) Pathfinder.MoveTo(node.Position);
                        else ChangeState(EnemyState.Patrol);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        ChangeState(EnemyState.Patrol);
                        Music?.RemoveEnemy(EnemyMusic.Search, this);
                        return true;
                    }
                    if (Msg(EnemyMessage.SoundHeard))
                    {
                        HearSound(message);
                        return true;
                    }
                    return false;

                // ---------------------------------------------------------- Hunt
                case (int)EnemyState.Hunt:
                    if (Enter)
                    {
                        if (AttackHitCounter >= 1)
                        {
                            AttackHitCounter = 0;
                            Music?.RemoveEnemy(EnemyMusic.Search, this);
                            ChangeState(EnemyState.HuntPause);
                        }
                        else
                        {
                            ChangeSoundState(EnemySoundState.Hunt);
                            SetMoveSpeed(EnemyMoveSpeed.Run);
                            SendMessage(EnemyMessage.TimeOut, 0.1f, true);
                            FovMul = 4f;
                            Music?.RemoveEnemy(EnemyMusic.Search, this);
                            Music?.AddEnemy(EnemyMusic.Attack, this);
                            ThePlayer.AddTerrorEnemy(this);
                            Pathfinder.MoveTo(LastKnownPlayerPos);
                            SendMessage(EnemyMessage.TimeOut_2, 0.6f, true);
                        }
                        return true;
                    }
                    if (Leave)
                    {
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        FovMul = 1f;
                        return true;
                    }
                    if (Update)
                    {
                        float distToPlayer = DistToPlayer();
                        if (CanSeePlayer && distToPlayer < NormalAttackDistance) ChangeState(EnemyState.AttackMeleeShort);
                        else if (StuckAtDoor) BreakStuckDoor();
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        // Close enough for the launch attack
                        float dist = DistToPlayer();
                        if (CanSeePlayer && dist > NormalAttackDistance && dist < NormalAttackDistance * 2f && Mover.StuckCounter < 0.5f)
                            ChangeState(EnemyState.AttackMeleeLong);
                        SendMessage(EnemyMessage.TimeOut_2, 0.6f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        float distToPlayer = DistToPlayer();
                        if (message.customInt == 1 && distToPlayer >= NormalAttackDistance)
                        {
                            // Path ended because stuck
                            if (!PlayerIsDetected()) SendMessage(EnemyMessage.TimeOut_3, 1.5f, true);
                            else ChangeState(EnemyState.HuntWander);
                        }
                        else
                        {
                            Pathfinder.MoveTo(LastKnownPlayerPos);
                            if (!PlayerIsDetected()) SendMessage(EnemyMessage.TimeOut_3, 1.5f, true);
                        }
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        // Update the path and call for help
                        Map.BroadcastEnemyMessage(EnemyMessage.HelpMe, true, Body.Position, ActivationDistance * 0.5f, 0f, false, Body.FeetPosition);
                        Pathfinder.MoveTo(LastKnownPlayerPos);
                        SendMessage(EnemyMessage.TimeOut, 0.2f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_3))
                    {
                        // Lost the player: extra time to catch up
                        if (!PlayerIsDetected())
                        {
                            ThePlayer.RemoveTerrorEnemy(this);
                            Music?.RemoveEnemy(EnemyMusic.Attack, this);
                            ChangeState(EnemyState.Search);
                        }
                        else SendMessage(EnemyMessage.TimeOut, 0.2f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.HelpMe)) return true;
                    return false;

                // ---------------------------------------------------------- Hunt Wander (hunt, but to a reachable node nearby)
                case (int)EnemyState.HuntWander:
                    if (Enter)
                    {
                        Mover.ResetStuckCounter();
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        ForwardSpeed *= 1.2f;
                        FovMul = 4f;
                        var node = Pathfinder.GetNodeAtPos(Body.FeetPosition, 2f, 5f, false, true, true, null);
                        if (node != null) Pathfinder.MoveTo(node.Position);
                        else ChangeState(EnemyState.Hunt);
                        return true;
                    }
                    if (Leave)
                    {
                        FovMul = 1f;
                        return true;
                    }
                    if (Msg(EnemyMessage.EndOfPath))
                    {
                        ChangeState(EnemyState.Hunt);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.HelpMe)) return true;
                    return false;

                // ---------------------------------------------------------- Hunt Pause (walk slowly toward the player)
                case (int)EnemyState.HuntPause:
                    if (Enter)
                    {
                        SendMessage(EnemyMessage.TimeOut_2, HplMath.RandRectf(_huntPauseMinTime, _huntPauseMaxTime), true);
                        Pathfinder.Stop();
                        SetMoveSpeed(EnemyMoveSpeed.Walk);
                        ForwardSpeed *= 0.6f;
                        FovMul = 4f;
                        SendMessage(EnemyMessage.TimeOut, 0.2f, true);
                        return true;
                    }
                    if (Leave)
                    {
                        FovMul = 1f;
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        if (DistToPlayer2D() > 2f) Pathfinder.MoveTo(ThePlayer.Body.FeetPosition);
                        if (CanSeePlayer) SendMessage(EnemyMessage.TimeOut, 0.2f, true);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut_2))
                    {
                        ChangeState(EnemyState.Alert);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.HelpMe)) return true;
                    return false;

                // ---------------------------------------------------------- Break door
                case (int)EnemyState.BreakDoor:
                    if (Enter)
                    {
                        Pathfinder.Stop();
                        PlayAnim("BreakDoor", false, 0.3f);
                        FovMul = 4f;
                        return true;
                    }
                    if (Leave)
                    {
                        AttackHitCounter = 0; // no pause when returning from breaking a door
                        FovMul = 1f;
                        return true;
                    }
                    if (Update)
                    {
                        Mover.TurnToPos(TempPos);
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationOver))
                    {
                        if (PlayerIsDetected()) ChangeState(EnemyState.Alert);
                        else if (StuckDoor is PropSwingDoor door && door.IsBroken)
                            ChangeState(PreviousState == EnemyState.Hurt ? EnemyState.Hunt : PreviousState);
                        else PlayAnim("BreakDoor", false, 0.3f);
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationSpecialEvent))
                    {
                        Attack(NormalAttackSize, BreakDoorAttackDamage);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.HelpMe)) return true;
                    return false;

                // ---------------------------------------------------------- Attack Short
                case (int)EnemyState.AttackMeleeShort:
                    if (Enter)
                    {
                        Pathfinder.Stop();
                        int num = HplMath.RandRectl(1, 2);
                        PlayAnim("SwingClaws0" + HplMath.ToString(num), false, 0.3f);
                        FovMul = 4f;
                        return true;
                    }
                    if (Leave)
                    {
                        FovMul = 1f;
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationOver))
                    {
                        ChangeState(PreviousState == EnemyState.Hurt ? EnemyState.Hunt : PreviousState);
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationSpecialEvent))
                    {
                        Attack(NormalAttackSize, NormalAttackDamage);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.HelpMe)) return true;
                    return false;

                // ---------------------------------------------------------- Attack Launch
                case (int)EnemyState.AttackMeleeLong:
                    if (Enter)
                    {
                        Pathfinder.Stop();
                        SetMoveSpeed(EnemyMoveSpeed.Run);
                        ForwardSpeed *= 1.5f;
                        PlayAnim("SwingLaunch", false, 0.3f);
                        TempIntVal = 0;
                        FovMul = 4f;
                        return true;
                    }
                    if (Leave)
                    {
                        FovMul = 1f;
                        SetMoveSpeed(EnemyMoveSpeed.Run);
                        return true;
                    }
                    if (Update)
                    {
                        if (TempIntVal == 0) Mover.MoveToPos(ThePlayer.Body.FeetPosition);
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationSpecialEvent))
                    {
                        TempIntVal = 1;
                        Attack(NormalAttackSize, NormalAttackDamage);
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationOver))
                    {
                        ChangeState(PreviousState);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.HelpMe)) return true;
                    return false;

                // ---------------------------------------------------------- Hurt
                case (int)EnemyState.Hurt:
                    if (Enter)
                    {
                        Pathfinder.Stop();
                        PlayAnim("Flinch", false, 0.5f);
                        SendMessage(EnemyMessage.TimeOut, 0.2f, true);
                        FovMul = 4f;
                        return true;
                    }
                    if (Leave)
                    {
                        FovMul = 1f;
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationOver))
                    {
                        ChangeState(PlayerIsDetected() ? EnemyState.Hunt : EnemyState.Alert);
                        return true;
                    }
                    if (Msg(EnemyMessage.TimeOut))
                    {
                        if (HplMath.RandRectl(0, 1) == 0 && DistToPlayer() < NormalAttackDistance * 1.3f) ChangeState(EnemyState.AttackMeleeShort);
                        return true;
                    }
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.HelpMe)) return true;
                    return false;

                // ---------------------------------------------------------- Dead (temporary in the original: no ragdoll)
                case (int)EnemyState.Dead:
                    if (Enter)
                    {
                        Pathfinder.Stop();
                        PlayAnim("Dead", false, 0.3f, false, 1f, false, true, false);
                        ThePlayer.RemoveTerrorEnemy(this);
                        Music?.RemoveEnemy(EnemyMusic.Attack, this);
                        Body.SetBodyActive(false);
                        return true;
                    }
                    if (Msg(EnemyMessage.AnimationOver)) return true;
                    if (Msg(EnemyMessage.PlayerDetected) || Msg(EnemyMessage.HelpMe)) return true;
                    return false;

                default:
                    return UndefinedState(state);
            }
        }

        /// <summary>cLuxEnemy_Grunt::PlayerIsDetected: seen, or closer than the body width and in the FOV.</summary>
        protected override bool PlayerIsDetected()
        {
            if (CanSeePlayer) return true;
            return DistToPlayer() < Body.Size.x && PlayerInFov();
        }

        protected override void OnDisableTriggers()
        {
            if (CurrentState == EnemyState.Idle) return;
            ChangeState(EnemyState.Patrol);
        }

        protected override float GetDamageMul(float amount, int strength)
        {
            if (CurrentState == EnemyState.Idle || CurrentState == EnemyState.Wait || CurrentState == EnemyState.Patrol || CurrentState == EnemyState.Investigate)
                return 1f;
            if (CurrentState == EnemyState.Hurt) return 0.5f;
            return 0.2f;
        }

        void PatrolUpdateGoal()
        {
            if (PatrolNodes.Count == 0)
            {
                ChangeState(EnemyState.Wait);
                return;
            }
            var patrolNode = GetCurrentPatrolNode();
            Pathfinder.MoveTo(patrolNode.node.Position);
        }

        /// <summary>cLuxEnemy_Grunt::PatrolEndOfPath: at the last node, unseen and far, the enemy goes away (OnAutoDisabled).</summary>
        void PatrolEndOfPath()
        {
            if (IsAtLastPatrolNode())
            {
                if (!IsSeenByPlayer && DistToPlayer() > 10f && AutoRemoveAtPathEnd)
                {
                    SetActive(false);
                    RunCallbackFunc("OnAutoDisabled");
                    return;
                }
            }
            var node = GetCurrentPatrolNode();
            _waitTime = node != null ? node.waitTime : 0f;
            ChangeState(EnemyState.Wait);
            IncCurrentPatrolNode(true);
        }
    }
}
