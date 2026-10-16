using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/12_storage.hps: drill and explosive mixture, cave-in,
    /// darkness, altar events, enemies and checkpoints.
    /// Journal: docs/ScriptPort/ch01/12_storage.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/12_storage.map")]
    public class Storage : HplScript
    {
        ////////////////////////////
        //BEGIN HAND DRILL & EPOXY//
        /*Count the number of parts the player has found for the hand drill
         */
        void PickDrill(string asEntity, string asType)
        {
            if(asEntity == "hand_drill_part03") AddTimer(nameof(TimerDrillEnemy), 1.0f, nameof(TimerDrillEnemy));

            AddGlobalVarInt("DrillParts", 1);

            if(GetGlobalVarInt("DrillParts") == 2) GiveHint("combinehint", "Hints", "CombineHint", 0);

            AddDebugMessage("DrillParts = "+GetGlobalVarInt("DrillParts"), false);

            CheckPoint("check_enemy2","PlayerStartCheckEnemy2", nameof(CheckPointEnemy2), "Hints", "DeathGrunt_12_Storage_02");
        }

        void TimerDrillEnemy(string asTimer)
        {
            SetGlobalVarInt("EnemySeenInBackHallLevel", 1);    //Change music in level 09

            SetEntityActive("grunt_normal_2", true);
            AddEnemyPatrolNode("grunt_normal_2", "PathNodeArea_28", 5, "");
            AddEnemyPatrolNode("grunt_normal_2", "PathNodeArea_223", 0, "");

            AddTimer("scare", 1.0f, nameof(TimerPlayerReactions));
            AddTimer("breath", 3.0f, nameof(TimerPlayerReactions));
            AddTimer("breathl", 5.0f, nameof(TimerPlayerReactions));
        }

        void DisableEnemy(string asEntity, string asType)
        {
            AddDebugMessage("Enemy disabled: "+asEntity, false);

            if(GetEntitiesCollide("grunt_normal_2", "AreaGruntLeave"))
            {
                AddDebugMessage(asEntity + " is in leave area, spawning trail!", false);
                PlaySoundAtEntity("doorclose", "close_door", "cellar_wood01_9", 0, false);

                SetPropActiveAndFade("grunt_track*", true, 0.25f);
                SetPropActiveAndFade("blood_spatter*", true, 0.25f);
            }
        }


        /*If interacting with epoxy containers, it only says valve is stuck
         */
        void InteractEpoxy(string asEntity)
        {
            if(GetLocalVarInt("GotEpoxy") == 1) return;

            if(GetLocalVarInt(asEntity) == 1)
                SetMessage("Ch01Level12", "EpoxyHoleHands", 0);
            else {
                SetMessage("Ch01Level12", "EpoxyStuck", 0);
                PlaySoundAtEntity("stuck_"+asEntity, "12_valve_stuck", asEntity, 0, false);
            }

            AddTimer(asEntity, 1.0f, nameof(TimerInteractEpoxy));
        }
        void TimerInteractEpoxy(string asTimer)
        {
            SetEntityPlayerInteractCallback(asTimer, nameof(InteractEpoxy), true);
        }

        /*Use the final drill on the epoxy containers to drill a hole
         */
        void UseDrillOnEpoxy(string asItem, string asEntity)
        {
            if(GetLocalVarInt("GotEpoxy") == 1) return;

            if(GetLocalVarInt(asEntity) != 0){
                SetMessage("Ch01Level12", "AlreadyDrilled", 0);
                return;
            }

            SetEntityActive(asEntity+"_area_2", true);
            AddUseItemCallback("usecontainer1" + asEntity, "empty_container", asEntity + "_area_2", nameof(UseEmptyContainerOnEpoxy), false);
            AddUseItemCallback("usehalfcontainer1" + asEntity, "epoxy_container01_2", asEntity + "_area_2", nameof(UseHalfContainerOnEpoxy), false);
            AddUseItemCallback("usehalfcontainer2" + asEntity, "epoxy_container02_2", asEntity + "_area_2", nameof(UseHalfContainerOnEpoxy), false);

            SetEntityActive(asEntity+"_drill", true);

            if(asEntity == "epoxy_container02")
                RotatePropToSpeed(asEntity+"_drill", 8.0f, 8.0f, 1.0f, 0, 0, true, asEntity+"_drill_area");
            else
                RotatePropToSpeed(asEntity+"_drill", 8.0f, 8.0f, 0, 0, 1.0f, true, asEntity+"_drill_area");

            StartPlayerLookAt(asEntity+"_area_1", 2, 2, "");

            if(StringContains(asEntity, "01")) SetLocalVarString("WhatEpoxyEffect", "drill01");
            else if(StringContains(asEntity, "02")) SetLocalVarString("WhatEpoxyEffect", "drill02");

            CreateParticleSystemAtEntity("ps_"+GetLocalVarString("WhatEpoxyEffect"), "ps_dust_drilling.ps", asEntity+"_area_1", false);
            PlaySoundAtEntity("s_"+GetLocalVarString("WhatEpoxyEffect"), "12_drill", asEntity+"_area_1", 1, false);

            SetLocalVarInt(asEntity, 1);

            AddTimer("stoplook", 1.5f, nameof(TimerDrill));

            AddTimer(asEntity, 2, nameof(TimerDrill));
            AddTimer(asEntity+"_drill", 1.5f, nameof(TimerDrill));

            AddDebugMessage("DRILLING A HOLE", false);
        }
        void TimerDrill(string asTimer)
        {
            if(asTimer == "stoplook"){
                StopPlayerLookAt();
                return;
            }
            else if(StringContains(asTimer, "_drill")){
                if(asTimer == "epoxy_container02_drill")
                    RotatePropToSpeed(asTimer, 4.0f, 4.0f, 1.0f, 0, 0, false, asTimer+"_area");
                else
                    RotatePropToSpeed(asTimer, 4.0f, 4.0f, 0, 0, 1.0f, false, asTimer+"_area");

                StopSound("s_"+GetLocalVarString("WhatEpoxyEffect"), 1.0f);
                return;
            }

            SetPropActiveAndFade(asTimer+"_drill", false, 0.5f);

            CreateParticleSystemAtEntity("ps2_"+GetLocalVarString("WhatEpoxyEffect"), "ps_liquid_epoxy.ps", asTimer+"_area_1", true);
            CreateParticleSystemAtEntity("ps3_"+GetLocalVarString("WhatEpoxyEffect"), "ps_liquid_epoxy_splatt.ps", asTimer+"_area_2", true);

            PlaySoundAtEntity("s2_"+GetLocalVarString("WhatEpoxyEffect"), "12_epoxy_flow", asTimer+"_area_1", 0, true);
        }
        void UseDrillPartOnEpoxy(string asItem, string asEntity)
        {
            SetMessage("Ch01Level12", "UsingDrillPart", 0);
        }

        /*Use the chemical container on epoxy containers to fill it will liquid
         */
        void UseEmptyContainerOnEpoxy(string asItem, string asEntity)
        {
            if(GetLocalVarInt("GotEpoxy") == 1) return;

            /*Container has been drilled so OK to place container
             */

            string sEntity = StringSub(asEntity, 0, 17);
            AddDebugMessage(sEntity, false);

            if(GetLocalVarInt(sEntity) == 1){
                if(GetTimerTimeLeft(sEntity)>0){
                    SetMessage("Ch01Level12", "NotDoneDrilling", 0);
                    return;
                }

                RemoveItem(asItem);

                SetEntityActive(sEntity+"_area_2", false);

                SetEntityActive(sEntity+"_1", true);

                StartPlayerLookAt(sEntity+"_area_2", 2, 2, "");

                DestroyParticleSystem("ps3_"+GetLocalVarString("WhatEpoxyEffect"));

                StopSound("s2_"+GetLocalVarString("WhatEpoxyEffect"), 2.0f);

                PlaySoundAtEntity("s3_"+GetLocalVarString("WhatEpoxyEffect"), "12_epoxy_fill", sEntity+"_area_1", 1.0f, false);
                PlaySoundAtEntity("s3p_"+GetLocalVarString("WhatEpoxyEffect"), "puzzle_place_jar", sEntity+"_area_2", 0.1f, false);

                AddTimer(sEntity, 2, nameof(TimerHalfFill));

                SetLocalVarInt(sEntity, 2);
                SetLocalVarInt("BottleNr", 1);
            }
            else if(GetLocalVarInt(sEntity) == 0){
                SetMessage("Ch01Level12", "ContainerNoUse", 0);    //Message that no use placing container as can't get to the content.
            }
        }
        /*Switch the container to half filled item containter
         */
        void TimerHalfFill(string asTimer)
        {
            StopPlayerLookAt();

            SetEntityActive(asTimer+"_"+GetLocalVarInt("BottleNr"), false);

            SetLocalVarInt("BottleNr", 2);

            SetEntityActive(asTimer+"_"+GetLocalVarInt("BottleNr"), true);

            DestroyParticleSystem("ps2_"+GetLocalVarString("WhatEpoxyEffect"));

            CreateParticleSystemAtEntity("ps4_"+GetLocalVarString("WhatEpoxyEffect"), "ps_liquid_epoxy_drip.ps", asTimer+"_area_1", true);
            PlaySoundAtEntity("s4_"+GetLocalVarString("WhatEpoxyEffect"), "12_epoxy_drip", asTimer+"_area_1", 1, true);
        }

        /*If player has filled the container from one epoxy and does not use it on the same epoxy, fill it
         */
        void UseHalfContainerOnEpoxy(string asItem, string asEntity)
        {
            if(GetLocalVarInt("GotEpoxy") == 1) return;

            string sEntity = StringSub(asEntity, 0, 17);

            /*Only place container if it is not the same epoxy as already used
             */
            if((asItem == "epoxy_container01_2" || asItem == "epoxy_container02_2") && GetLocalVarInt(asEntity) != 2){
                if(GetLocalVarInt(sEntity) == 1){
                    if(GetTimerTimeLeft(sEntity)>0){
                    SetMessage("Ch01Level12", "NotDoneDrilling", 0);
                    return;
                    }

                    RemoveItem(asItem);

                    SetEntityActive(sEntity+"_area_2", false);

                    SetEntityActive(sEntity+"_3", true);

                    StartPlayerLookAt(sEntity+"_area_2", 2, 2, "");

                    DestroyParticleSystem("ps3_"+GetLocalVarString("WhatEpoxyEffect"));

                    StopSound("s2_"+GetLocalVarString("WhatEpoxyEffect"), 2.0f);

                    PlaySoundAtEntity("s3_"+GetLocalVarString("WhatEpoxyEffect"), "12_epoxy_fill", sEntity+"_area_1", 1.0f, false);
                    PlaySoundAtEntity("s3p_"+GetLocalVarString("WhatEpoxyEffect"), "puzzle_place_jar", sEntity+"_area_2", 0.2f, false);

                    AddTimer(sEntity, 2, nameof(TimerFullFill));

                    SetLocalVarInt("BottleNr", 3);

                    SetLocalVarInt("GotEpoxy", 1);

                } else SetMessage("Ch01Level12", "NoHole", 0);
            }
            else
                SetMessage("Ch01Level12", "ContainerAlreadyUsed", 0);
        }
        /*Switch the half-full container to a completely filled item container
         */
        void TimerFullFill(string asTimer)
        {
            StopPlayerLookAt();

            SetEntityActive(asTimer+"_"+GetLocalVarInt("BottleNr"), false);

            SetLocalVarInt("BottleNr", 4);

            SetEntityActive(asTimer+"_"+GetLocalVarInt("BottleNr"), true);

            DestroyParticleSystem("ps2_"+GetLocalVarString("WhatEpoxyEffect"));

            CreateParticleSystemAtEntity("ps4_"+GetLocalVarString("WhatEpoxyEffect"), "ps_liquid_epoxy_drip.ps", asTimer+"_area_1", true);
            PlaySoundAtEntity("s4_"+GetLocalVarString("WhatEpoxyEffect"), "12_epoxy_drip", asTimer+"_area_1", 1, true);
        }


        /*Give Sanity Push
         */
        void PickEpoxy(string asEntity, string asType)
        {
            GiveSanityBoostSmall();

            PlayMusic("12_puzzle_epoxy.ogg", false, 1.0f, 0.0f, 10, true);

            AddTimer("mmmmonsterkill", 2.0f, nameof(TimerMmMonster));
        }
        void TimerMmMonster(string asTimer)
        {
            StartScreenShake(0.02f, 0.25f, 0.75f, 1.5f);
            PlaySoundAtEntity("warning", "12_warn.snt", "ScriptArea_2", 0.0f, false);
            AddTimer("scare", 1.0f, nameof(TimerPlayerReactions));
            AddTimer("breath", 3.0f, nameof(TimerPlayerReactions));
            AddTimer("breathl", 5.0f, nameof(TimerPlayerReactions));
        }
        //END HAND DRILL & EPOXY//
        //////////////////////////


        ////////////////
        //BEGIN CAVEIN//
        /*Place the final epoxy at the cave in
         */
        void UseEpoxyOnCavein(string asItem, string asEntity)
        {
            RemoveItem(asItem);

            GiveHint("run!", "Ch01Level12", "HintRunAway", 0);

            StartPlayerLookAt("AreaLookAt_1", 2, 2, "");

            AddTimer("crouch", 0.5f, nameof(TimerStopLookCaveIn));
            AddTimer("place", 1.0f, nameof(TimerStopLookCaveIn));
            AddTimer("stoplook", 1.2f, nameof(TimerStopLookCaveIn));
        }
        void TimerStopLookCaveIn(string asTimer)
        {
            if(asTimer == "crouch"){
                MovePlayerHeadPos(0.0f, -0.5f, 0.0f, 1.0f, 0.1f);
                PlaySoundAtEntity("crouchs", "player_climb", "Player", 0.5f, false);
            }
            else if(asTimer == "place"){
                SetEntityActive("explosive_container", true);
                PlaySoundAtEntity("placejar", "puzzle_place_jar", "explosive_container", 0, false);
            }
            else if(asTimer == "stoplook"){
                PlaySoundAtEntity("crouchs", "player_crouch", "Player", 0.0f, false);
                MovePlayerHeadPos(0.0f, 0.0f, 0.0f, 1.5f, 0.05f);
                StopPlayerLookAt();
            }
        }

        void InteractEpoxyByCave(string asEntity)
        {
            SetMessage("Ch01Level12", "TouchEpoxy", 0);
        }

        void DaBigBoom(string asEntity, string asType)
        {
            AddTimer("sound", 1, nameof(TimerBigBoomEvent));
            AddTimer("1", 4, nameof(TimerBigBoomEvent));

            FadeLightTo("DangerLight", 1, 0, 0, 1, 4, 4);
        }
        /*After a second from throwing something at the container it explodes
         */
         void TimerBigBoomEvent(string asTimer)
        {
            if(asTimer == "sound"){
                PlaySoundAtEntity("epoxyignite", "12_epoxy_blow", "cave_in_1", 0, false);
                StartScreenShake(0.01f, 0, 0, 2.9f);
                return;
            }

            SetEntityActive("explosive_container", false);

            SetPropHealth("cave_in_1", 0);

            StartScreenShake(0.08f, 2.5f, 0, 1.0f);

            FadeImageTrailTo(0.5f, 1);

            FadeLightTo("BoomLight", 1, 1, 1, 1, 8, 0.1f);
            FadeLightTo("DangerLight", 0, 0, 0, 0, 0, 0.5f);

            switch(GetLocalVarInt("BoomPlayer")){
                case 0:
                    //GivePlayerDamage(25, "BloodSplat", true, false);
                    //AddDebugMessage("Player got damage: -25", false);
                break;
                case 1:
                    GivePlayerDamage(50, "BloodSplat", true, false);
                    AddDebugMessage("Player got damage: -50", false);
                break;
                case 2:
                    GivePlayerDamage(200, "BloodSplat", true, false);
                    AddDebugMessage("Player got damage: -200", false);
                break;
            }

            PlayMusic("12_puzzle_cavein.ogg", false, 1, 0.0f, 10, true);

            AddTimer("cquest", 2, nameof(TimerCompleteQuest));
            AddTimer("light", 0.75f, nameof(TimerCompleteQuest));
        }

        void TimerCompleteQuest(string asTimer)
        {
            if(asTimer == "light"){
                FadeLightTo("BoomLight", 0, 0, 0, 0, 0, 1.25f);
                return;
            }

            CompleteQuest("12Cavein", "12Cavein");

            FadeImageTrailTo(0.0f, 1);

            GiveSanityBoost();
        }

        /*Check what area the player is in during the explosion to deal a certain amount of damage
         */
        void CollideBoomArea(string asParent, string asChild, int alState)
        {
            if(asChild == "AreaBoomDeath") SetLocalVarInt("BoomPlayer", 2);
            else if(asChild == "AreaBoomEscape") SetLocalVarInt("BoomPlayer", 0);
            else SetLocalVarInt("BoomPlayer", 1);

            AddDebugMessage("Player to get damage: "+GetLocalVarInt("BoomPlayer"), false);
        }
        //END CAVEIN//
        //////////////


        //////////////////////
        //BEGIN ROOM OF GIRL//
        /*Turn of lantern and do effect when entering room with altar lamp
         */
        void CollideLightsOut(string asParent, string asChild, int alState)
        {
            SetLanternActive(false, true);
            SetLanternDisabled(true);

            PlaySoundAtEntity("eek", "scare_whine_loop3", "Player", 0.5f, false);
            PlaySoundAtEntity("screaaaam", "12_girl_scream.snt", "Player", 1.0f, false);

            StartScreenShake(0.5f, 0.01f, 0.1f, 0.02f);

            PlayMusic("12_event_blood.ogg", false, 1.0f, 0.0f, 10, true);

            CreateParticleSystemAtEntity("petals1", "ps_rose_petals.ps", "AreaPedals_1", false);

            RemoveTimer(nameof(TimerGirlCry));

            AddTimer("lights1", 0.05f, nameof(TimerLightsOut));
            AddTimer("lights2", 0.2f, nameof(TimerLightsOut));
            AddTimer("lights3", 0.6f, nameof(TimerLightsOut));
            AddTimer("lights4", 1.2f, nameof(TimerLightsOut));
            AddTimer("lights5", 1.5f, nameof(TimerLightsOut));
            AddTimer("lights6", 2.5f, nameof(TimerLightsOut));
            AddTimer("lightssps", 4.0f, nameof(TimerLightsOut));
            AddTimer("lightsq", 10, nameof(TimerLightsOut));
        }
        /*Shake, show blood, scream for the event
         */
        void TimerLightsOut(string asTimer)
        {
            if(asTimer == "lights1"){
                CreateParticleSystemAtEntity("petals2", "ps_rose_petals.ps", "AreaPedals_2", false);
                StartPlayerLookAt("AreaLightsOut", 2.0f, 2.0f, "");
                MovePlayerHeadPos(0.0f, -1.0f, 0.0f, 0.5f, 0.5f);
                GiveSanityDamage(20.0f, false);
                FadeRadialBlurTo(0.1f, 0.025f);
                SetRadialBlurStartDist(0.2f);
                SetLampLit("altar_lamp_1", true, true);
                PlaySoundAtEntity("thunmp", "scare_thump_flash", "Player", 0.0f, false);
            }
            else if(asTimer == "lights2"){
                CreateParticleSystemAtEntity("petals3", "ps_rose_petals.ps", "AreaPedals_3", false);
                CreateParticleSystemAtEntity("petals31", "ps_rose_petals.ps", "AreaPedals_1", false);
                SetLanternDisabled(false);
                PlaySoundAtEntity("thunmp", "scare_tingeling", "Player", 0.0f, false);
                StartScreenShake(0.7f, 0.1f, 0.05f, 0.02f);
                StopMusic(8.0f, 10);
            }
            else if(asTimer == "lights3"){
                CreateParticleSystemAtEntity("petals4", "ps_rose_petals.ps", "AreaPedals_4", false);
                PlaySoundAtEntity("scare1", "react_scare", "Player", 0.5f, false);
                StartScreenShake(1.0f, 0.01f, 0.05f, 0.1f);
            }
            else if(asTimer == "lights4"){
                CreateParticleSystemAtEntity("petals5", "ps_rose_petals.ps", "AreaPedals_2", false);
                PlaySoundAtEntity("creak", "00_creak", "Player", 2, false);
                StartScreenShake(0.3f, 0.01f, 0.05f, 0.02f);
                MovePlayerHeadPos(0.0f, 0.0f, 0.0f, 0.2f, 0.2f);
            }
            else if(asTimer == "lights5"){
                CreateParticleSystemAtEntity("petals41", "ps_rose_petals.ps", "AreaPedals_4", false);
                StartScreenShake(0.7f, 0.02f, 0.05f, 0.05f);
                PlaySoundAtEntity("scare2", "react_scare", "Player", 0.75f, false);
            }
            else if(asTimer == "lights6"){
                StartScreenShake(0.7f, 0.02f, 0.05f, 0.05f);
                PlaySoundAtEntity("scare1", "react_scare", "Player", 0.5f, false);
                FadeSepiaColorTo(0, 0.1f);
                FadeRadialBlurTo(0, 0.1f);
                StopPlayerLookAt();
                StopSound("eek", 1.0f);
            }
            else if(asTimer == "lightssps"){
                for(int i=1;i<=2;i++) DestroyParticleSystem("BloodParticle_"+i);
                for(int i=1;i<=4;i++) StopSound("BloodSound_"+i, 0.0f);
                StopSound("creak", 4.0f);
            }
            //else if(asTimer == "lightsq"){ CompleteQuest("12Trail","12Trail");}
        }

        void InteractCloth(string asEntity)
        {
            PlayGuiSound("12_cloth.snt", 0.9f);
            //AddTimer(asEntity, 1.0f, "TimerClothOn");

            //if(GetLocalVarInt("TouchedCloth") == 1) return;

            StartEffectEmotionFlash("Ch01Level12", "SignGirlCloth", "ui_insanity_touch.snt");

            //SetLocalVarInt("TouchedCloth", 1);

            //SetMessage("Ch01Level12", "SignGirlCloth", 0);
        }
        void TimerClothOn(string asTimer)
        {
            SetEntityPlayerInteractCallback(asTimer, nameof(InteractCloth), true);
        }
        //END ROOM OF GIRL//
        ////////////////////


        /////////////////////
        //BEGIN MISC EVENTS//
        /*Activate an enemy that patrols the epoxy container room
         */
        void CollideEnemy1(string asParent, string asChild, int alState)
        {
            CheckPoint("check_enemy1","PlayerStartCheckEnemy1", nameof(CheckPointEnemy1), "Hints", "DeathGrunt_12_Storage_01");

            TimerEnemy1("");
        }
        void TimerEnemy1(string asTimer)
        {
            SetGlobalVarInt("EnemySeenInBackHallLevel", 1);    //Change music in level 09

            SetEntityActive("grunt_normal_1", true);
            ClearEnemyPatrolNodes("grunt_normal_1");

            AddTimer("scare", 1.0f, nameof(TimerPlayerReactions));
            AddTimer("breath", 3.0f, nameof(TimerPlayerReactions));
            AddTimer("breathl", 5.0f, nameof(TimerPlayerReactions));

            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_44", 8, "IdleExtra1");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_27", 0, "");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_159", 0, "");
        }

        /*When finding the final rod activate an enemy that patrols the altar lamp room
         */
        void PickRod(string asEntity, string asType)
        {
            AddGlobalVarInt("GuidingRod", 1);
            if(GetGlobalVarInt("GuidingRod") == 1) return;

            CompleteQuest("12Darkness", "12Darkness");    //Remove darkness clue from journal as player has "completed" level and will not need that hint any longer

            GiveSanityBoostSmall();

            // Set up stuff for 09 visions
            AddDebugMessage("Setting level completion", false);
            AddDebugMessage("CompletedLevel_10_11: " + GetGlobalVarInt("CompletedLevel_10_11"), false);
            AddDebugMessage("(Before) CompletedLevel_12: " + GetGlobalVarInt("CompletedLevel_12"), false);

            if(GetGlobalVarInt("CompletedLevel_12")==0)
                SetGlobalVarInt("CompletedLevel_12", GetGlobalVarInt("CompletedLevel_10_11")+1);

            AddDebugMessage("(After) CompletedLevel_12: " + GetGlobalVarInt("CompletedLevel_12"), false);

            /*An enemy will appear when almost at the exit of the level
             */
            AddEntityCollideCallback("Player", "AreaEnemy_1", nameof(CollideEnemy1), true, 0);

            AddTimer(nameof(TimerRodGrunt), 2.0f, nameof(TimerRodGrunt));

            SetEntityActive("HoleDudeStart", true);    //HoleMonster on way back

            CheckPoint("check_enemy3","PlayerStartCheckEnemy3", nameof(CheckPointEnemy3), "Hints", "DeathGrunt_12_Storage_03");

            //Grunt in room, is in room, disabled or not activated, so remove that event now when activating a new grunt + open door to make it appear grunt there left
            if(GetLocalVarInt("GruntInRoomEvent") != 2){
                SetLocalVarInt("GruntInRoomEvent", 3);
                SetEntityActive("AreaGruntRoomActivate", false);
                SetEntityActive("AreaGruntRoom", false);
                SetEntityActive("AreaCloseDoor", false);

                SetSwingDoorClosed("cellar_wood01_7", false, false);
                SetSwingDoorDisableAutoClose("cellar_wood01_7", true);
                AddPropImpulse("cellar_wood01_7", -2, 0, 0, "World");
            }
        }
        void TimerRodGrunt(string asTimer)
        {
            if(GetLocalVarInt("GruntInRoomEvent") == 2) return;    //Do not do this grunt if grunt in room is active and outside of room

            AddPropImpulse("cellar_wood01_7", -2, 0, 0, "World");

            SetLocalVarInt("RodGruntActive", 1);

            SetEntityActive("grunt_normal_3", true);

            AddEnemyPatrolNode("grunt_normal_3", "PathNodeArea_123", 2, "");
            AddEnemyPatrolNode("grunt_normal_3", "PathNodeArea_122", 0, "");
            AddEnemyPatrolNode("grunt_normal_3", "PathNodeArea_83", 4, "IdleExtra1");
            AddEnemyPatrolNode("grunt_normal_3", "PathNodeArea_79", 1, "");

            AddTimer("scare", 1.0f, nameof(TimerPlayerReactions));
            AddTimer("breath", 3.0f, nameof(TimerPlayerReactions));
            AddTimer("breathl", 5.0f, nameof(TimerPlayerReactions));
        }
        /*Door that is always locked
         */
        void AlwaysLocked(string asEntity)
        {
            SetMessage("LevelDoors", "LockedForever", 0);
        }
        void CollideGiveQuest(string asParent, string asChild, int alState)
        {
            AddQuest("12Cavein","12Cavein");
        }
        void VisionOverGiveQuest()
        {
            //AddTimer("TimerGiveQuest", 2.0f, "TimerGiveQuest");
            AddTimer(nameof(TimerGirlCry), 5.0f, nameof(TimerGirlCry));
        }
        void TimerGiveQuest(string asTimer)
        {
            //AddQuest("12Trail","12Trail");
        }
        void TimerGirlCry(string asTimer)
        {
            PlaySoundAtEntity("AreaGirlSound_5Cry", "12_girl_cry", "AreaGirlSound_"+RandInt(1, 5), 0.1f, false);

            AddTimer(nameof(TimerGirlCry), RandInt(15, 30), nameof(TimerGirlCry));
        }
        //END MISC EVENTS//
        ///////////////////


        ///////////////////
        //BEGIN EXTRA EVENTS//

        void CollideEnemy2Extra(string asParent, string asChild, int alState)
        {
            SetEntityActive("grunt_normal_2", true);
            ClearEnemyPatrolNodes("grunt_normal_2");

            AddEnemyPatrolNode("grunt_normal_2", "PathNodeArea_131", 5, "");
            if(GetLocalVarInt("Enemy2Extra_Count")==0)
                AddEnemyPatrolNode("grunt_normal_2", "PathNodeArea_23", 2, "");
            AddEnemyPatrolNode("grunt_normal_2", "PathNodeArea_1", 0, "");

            AddLocalVarInt("Enemy2Extra_Count", 1);

            AddTimer("scare", 1.0f, nameof(TimerPlayerReactions));
            AddTimer("breath", 3.0f, nameof(TimerPlayerReactions));
            AddTimer("breathl", 5.0f, nameof(TimerPlayerReactions));
        }


        void CollideEnemy3Extra(string asParent, string asChild, int alState)
        {
            SetEntityActive("grunt_normal_3_extra", true);
            ClearEnemyPatrolNodes("grunt_normal_3_extra");

            AddEnemyPatrolNode("grunt_normal_3_extra", "PathNodeArea_124", 5, "");
            AddEnemyPatrolNode("grunt_normal_3_extra", "PathNodeArea_126", 2, "");
            AddEnemyPatrolNode("grunt_normal_3_extra", "PathNodeArea_76", 0, "");

            AddTimer("scare", 1.0f, nameof(TimerPlayerReactions));
            AddTimer("breath", 3.0f, nameof(TimerPlayerReactions));
            AddTimer("breathl", 5.0f, nameof(TimerPlayerReactions));
        }




        void CollideEnemy1Extra(string asParent, string asChild, int alState)
        {
            SetEntityActive("grunt_normal_1_extra", true);
            ClearEnemyPatrolNodes("grunt_normal_1_extra");

            AddEnemyPatrolNode("grunt_normal_1_extra", "PathNodeArea_43", 5, "");
            AddEnemyPatrolNode("grunt_normal_1_extra", "PathNodeArea_30", 5, "");
            AddEnemyPatrolNode("grunt_normal_1_extra", "PathNodeArea_184", 0, "");

            AddTimer("scare", 1.0f, nameof(TimerPlayerReactions));
            AddTimer("breath", 3.0f, nameof(TimerPlayerReactions));
            AddTimer("breathl", 5.0f, nameof(TimerPlayerReactions));
        }

        //END EXTRA EVENTS//
        ///////////////////


        ///////////////////
        //BEGIN CHECK POINTS//

        //Enemy that appears when player picks drill part 3
        void CheckPointEnemy2(string asName, int alCount)
        {
            ResetProp("cellar_wood01_5");

            if(alCount <=1)
                AddEntityCollideCallback("Player", "AreaEnemy2Extra", nameof(CollideEnemy2Extra), true, 1);
        }

        //Enemy appears when player picks both rods
        void CheckPointEnemy3(string asName, int alCount)
        {
            ResetProp("cellar_wood01_8");

            if(alCount <=1)
                AddEntityCollideCallback("Player", "AreaEnemy3Extra", nameof(CollideEnemy3Extra), true, 1);
        }


        //Enemy appears when player has picked both and is near end.
        void CheckPointEnemy1(string asName, int alCount)
        {
            ResetProp("cellar_wood01_5");

            if(alCount <=0)
            {
                AddEntityCollideCallback("Player", "AreaEnemy_1", nameof(CollideEnemy1), true, 0);
            }
            else if(alCount == 1)
            {
                AddEntityCollideCallback("Player", "AreaEnemy1Extra", nameof(CollideEnemy1Extra), true, 1);
            }
        }




        //END CHECK POINTS//
        ///////////////////

        //////////////////////////////
        //BEGIN NOISE AREAS
        /*Change the volume of the noise when entering different areas, depending on
         *nr or lit lamp entities.
        */
        void CollideAreaNoise(string asParent, string asChild, int alState)
        {
            if(alState == 1){
                SetLocalVarInt(asChild, (int)GetLocalVarFloat("f"+asChild)); // AngelScript truncates.

                AddDebugMessage("Noise value: "+GetLocalVarInt(asChild), false);

                RemoveTimer("sanitydamage");

                //Start sanity damage if no or few lights lit
                if(GetLocalVarInt(asChild) <= 2)
                    AddTimer("sanitydamage", 0.1f, nameof(TimerSanityDamage));

                //If many lights lit not even then sound is played
                if(GetLocalVarInt(asChild) >= 4) return;

                PlaySoundAtEntity("noise_"+asChild, "12_noise_"+GetLocalVarInt(asChild), asChild, 1.0f, true);
            }
            else if(alState == -1){
                StopSound("noise_"+asChild, 2.0f);
                RemoveTimer("sanitydamage");
            }
        }

        /*Epoxy room and small rooms around
         */
        void EntityCallAreaNoise_1(string asEntity, string type)
        {
            UpdateNoiseValue("AreaNoise_1", 0.6f);
        }
        /*First rooms upstairs before cavein
         */
        void EntityCallAreaNoise_2(string asEntity, string type)
        {
            UpdateNoiseValue("AreaNoise_2", 0.6f);
        }
        /*Large area after cavein, all the way up to the wall with the window
         */
        void EntityCallAreaNoise_3(string asEntity, string type)
        {
            UpdateNoiseValue("AreaNoise_3", 0.67f);
        }
        /*Corridor and girl altar room
         */
        void EntityCallAreaNoise_4(string asEntity, string type)
        {
            UpdateNoiseValue("AreaNoise_4", 1.2f);
        }

        /*Update the noise value and sound depending on area and lamp entity lit
         */
        void UpdateNoiseValue(string sArea, float fValue)
        {
            AddLocalVarFloat("f"+sArea, fValue);
            AddDebugMessage("Noise float value: "+Str(GetLocalVarFloat("f"+sArea)), false);

            SetLocalVarInt(sArea, (int)GetLocalVarFloat("f"+sArea)); // AngelScript truncates.
            AddDebugMessage("Noise value: "+GetLocalVarInt(sArea), false);

            StopSound("noise_"+sArea, 1.0f);

            //If a few lights lit, stop sanity damage
            if(GetLocalVarInt(sArea) >= 2){
                RemoveTimer("sanitydamage");
                CompleteQuest("12Darkness", "12Darkness");
            }

            //If many lights lit, stop the sound too
            if(GetLocalVarInt(sArea) >= 4) return;

            PlaySoundAtEntity("noise_"+sArea, "12_noise_"+GetLocalVarInt(sArea), sArea, 0.5f, true);
        }

        /*The small amount of sanity damage given in the darkness areas while no or very few lamps lit
         */
        void TimerSanityDamage(string asTimer)
        {
            GiveSanityDamage(0.3f, false);
            AddTimer(asTimer, 5.0f, nameof(TimerSanityDamage));
            AddDebugMessage("sanity drain from darkness", false);

            AddLocalVarInt("CountSanityDamage", 1);

            if(GetLocalVarInt("CountSanityDamage") == 2){
                AddQuest("12Darkness", "12Darkness");
                //SetMessage("Ch01Level12", "DarknessComment", 0);
            }

            if(GetLocalVarInt("CountSanityDamage") == 10 && !QuestIsCompleted("12Darkness"))
                GiveHint("darkness", "Ch01Level12", "DarknessHint", 0);
        }


        /////////////////////////////
        //BEGIN HOLE DUDE
        void CollideHoleDude(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("warns", "04_scream.snt", "HoleDude", 0.0f, false);
            CreateParticleSystemAtEntity("warmps", "ps_cave_monster_scream.ps", "HoleDude", false);
            PlaySoundAtEntity("rumble", "03_rock_move", "HoleDude", 0.0f, false);
            StopSound("Sound_9", 5.0f);

            SetEntityActive("rockthing_*", true);

            StartScreenShake(0.01f, 0.5f, 0.25f, 1.25f);

            AddTimer("scare", 0.5f, nameof(TimerPlayerReactions));
            AddTimer("breath", 2.5f, nameof(TimerPlayerReactions));
            AddTimer("breathl", 4.5f, nameof(TimerPlayerReactions));

            AddTimer("mloop", 4.0f, nameof(TimerHoleMonster));
        }
        void TimerHoleMonster(string asTimer)
        {
            PlaySoundAtEntity("idlemonster", "04_breath.snt", "HoleDude", 0.0f, false);
            StartScreenShake(0.005f, 0.25f, 2.25f, 1.5f);
            AddTimer("mloop", 5.0f, nameof(TimerHoleMonster));
        }

        /////////////////////////////
        //BEING PLAYER REACTION
        void TimerPlayerReactions(string asTimer)
        {
            if(asTimer == "scare"){
                PlayGuiSound("react_scare", 0.7f);
                GiveSanityDamage(10.0f, false);
                FadeSepiaColorTo(0.5f, 0.025f);
                FadeRadialBlurTo(0.1f, 0.025f);
                SetRadialBlurStartDist(0.2f);
            }
            else if(asTimer == "breath"){
                PlayGuiSound("react_breath", 0.7f);
                FadeSepiaColorTo(0, 0.1f);
                FadeRadialBlurTo(0, 0.1f);
            }
            else if(asTimer == "breathl"){
                PlayGuiSound("react_breath", 0.4f);
            }
        }


        ////////////////////////////////////////
        //Chain sounds in girl slaughtered area
        void CollideAreaChain(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("chains", "general_chain_rattle_single.snt", asChild, RandFloat(0.0f, 4.0f), false);
        }

        void CollideChainEndWithAreaChain(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("chains", "general_chain_rattle_single.snt", asChild, RandFloat(0.0f, 4.0f), false);
        }

        //////////////////////////////////
        //Grunt in a room event
        //Player touches the door to the room the first time and grunt event is setup
        void InteractGruntDoor(string asEntity)
        {
            if(GetLocalVarInt("RodGruntActive") == 1) return;    //Do not do this event if player picked rod and grunt from that event is active

            SetLocalVarInt("GruntInRoomEvent", 1);
        }

        //Area for door that checks if it opens or closes
        void CollideAreaCloseDoor(string asParent, string asChild, int alState)
        {
            if(GetLocalVarInt("GruntInRoomEvent") != 1) return;

            //Door opens, make grunt active and normal
            if(alState == -1){
                AddEnemyPatrolNode("grunt_in_room", "PathNodeArea_94", 0.0f, "");
                AddEnemyPatrolNode("grunt_in_room", "PathNodeArea_84", 0.0f, "");
                SetEntityActive("grunt_in_room", true);

                CheckPoint("roomgrunt", "PlayerStartArea_7", nameof(CPCallGruntRoom), "", "");

                StopBangDoorEvent();
            }

            //Door closes, make grunt fictional but not if player is in room too
            else{
                if(GetLocalVarInt("PlayerInGruntArea") != 1)
                    StartBangDoorEvent();
            }
        }

        //Check if grunt/player is in the room
        void CollideAreaGruntRoom(string asParent, string asChild, int alState)
        {
            //If player in room, set var to make sure the grunt will not disable if door is closed.
            if(asParent == "Player")
                SetLocalVarInt("PlayerInGruntArea", alState);

            //Grunt leaves room, so all possible events are shutdown and grunt is just a normal chasing grunt happy with no worries in the world.
            if(asParent == "grunt_in_room" && alState == -1){
                SetLocalVarInt("GruntInRoomEvent", 2);
                SetLocalVarInt("GruntBreakDoor", 3);
                StopBangDoorEvent();
                AddDebugMessage("Grunt left room", false);
            }
        }

        //Check if player is close to the door outside of the room
        void CollideAreaGruntRoomActivate(string asParent, string asChild, int alState)
        {
            if(GetLocalVarInt("GruntInRoomEvent") != 1) return;

            if(alState == 1)
                StartBangDoorEvent();

            else
                StopBangDoorEvent();
        }

        //Player comes closer to door, begin the banging
        void StartBangDoorEvent()
        {
            ClearEnemyPatrolNodes("grunt_in_room");

            SetEntityActive("grunt_in_room", false);

            AddTimer(nameof(TimerEBangOnDoor), RandFloat(1.0f, 2.0f), nameof(TimerEBangOnDoor));
        }

        //Player goes away, remove banging
        void StopBangDoorEvent()
        {
            RemoveTimer(nameof(TimerEBangOnDoor));
            SetLocalVarInt(nameof(TimerEBangOnDoor), 0);

            //Event has occured 3 times, remove all, let grunt stay active and do not do event any more
            if(GetLocalVarInt("GruntBreakDoor") == 3){
                SetEntityActive("AreaGruntRoomActivate", false);
                SetEntityActive("AreaGruntRoom", false);
                SetEntityActive("AreaCloseDoor", false);

                if(GetLocalVarInt("GruntInRoomEvent") != 3){
                    ClearEnemyPatrolNodes("grunt_in_room");
                    AddEnemyPatrolNode("grunt_in_room", "PathNodeArea_94", 0.0f, "");
                    AddEnemyPatrolNode("grunt_in_room", "PathNodeArea_84", 0.0f, "");
                    SetEntityActive("grunt_in_room", true);
                }
            }
        }

        //The grunt noise and bang on door
        void TimerEBangOnDoor(string asTimer)
        {
            string sEvent = asTimer;    //Do not edit, sets timer loop function name.
            AddLocalVarInt(sEvent, 1);    //Do not edit, steps through timer loop events.
            bool bPauseAtStep = false;    //Do not edit, to pause or end the timer loop at specified step.

            float fEventSpeed = 0.5f;    //Set to preferred default time for the timer loop.

            switch(GetLocalVarInt(sEvent)){
                //////////
                //Grunt screams
                case 1:
                    BangOnDoorEffects("grunt", "grunt/notice.snt", false);
                break;

                //////////
                //Grunt bang on door 1
                case 2:
                    BangOnDoorEffects("bang1", "lurker_hit_wood.snt", true);
                    fEventSpeed = RandFloat(0.2f, 0.8f);
                break;

                //////////
                //Grunt bang on door 2
                case 3:
                    BangOnDoorEffects("bang2", "lurker_hit_wood.snt", true);
                    fEventSpeed = RandFloat(0.2f, 0.8f);
                break;

                //////////
                //Grunt bang on door 3
                case 4:
                    BangOnDoorEffects("bang3", "lurker_hit_wood.snt", true);
                    AddLocalVarInt("GruntBreakDoor", 1);

                    if(GetLocalVarInt("GruntBreakDoor") == 3)
                        StopBangDoorEvent();
                break;


                //////////
                //End timer when no more steps in the event found.
                default:
                    bPauseAtStep = true;
                break;
            }

            if(!bPauseAtStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }

        //Effects for each bang
        void BangOnDoorEffects(string sName, string sSound, bool bParticle)
        {
            PlaySoundAtEntity(sName, sSound, "cellar_wood01_7", 0.0f, false);

            if(bParticle)
                CreateParticleSystemAtEntity("ps"+sName, "ps_hit_wood.ps", "cellar_wood01_7", false);
        }

        //Set variable to allow rod grunt to activate.
        void EntityCallGruntRoomDisabled(string asEntity, string type)
        {
            AddDebugMessage("Grunt from room disabled.", false);

            SetLocalVarInt("GruntInRoomEvent", 3);
        }

        //Checkpoint from room grunt
        void CPCallGruntRoom(string asName, int alCount)
        {
            SetLocalVarInt("GruntInRoomEvent", 3);
            ResetProp("cellar_wood01_7");

            SetEntityActive("AreaGruntBoo", true);

            AddEntityCollideCallback("Player", "AreaGruntBoo", nameof(CollideAreaGruntBoo), true, 1);
        }
        void CollideAreaGruntBoo(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("boo", "grunt/amb_idle.snt", "AreaGruntBoo_1", 0.0f, false);

            AddTimer("scare", 1.0f, nameof(TimerPlayerReactions));
            AddTimer("breath", 3.0f, nameof(TimerPlayerReactions));
            AddTimer("breathl", 5.0f, nameof(TimerPlayerReactions));
        }


        //BEGIN ALTAR FIRE//
        void CollideAreaFire(string asParent, string asChild, int alState)
        {
            if(alState == 1) AddTimer("burn", 0.1f, nameof(TimerBurnFire));
            else RemoveTimer("burn");
        }
        void InteractFire(string asEntity)
        {
            TimerBurnFire("touch");
        }
        void TimerInteractFire(string asTimer)
        {
            SetEntityPlayerInteractCallback("AreaFire", nameof(InteractFire), true);
        }
        void TimerBurnFire(string asTimer)
        {
            if(asTimer == "touch"){
                PlayGuiSound("15_player_burn.snt", 1.0f);
                GivePlayerDamage(5.0f, "BloodSplat", false,false);
                AddTimer(nameof(InteractFire), 1.0f, nameof(TimerInteractFire));
                return;
            }

            PlayGuiSound("15_player_burn.snt", 1.0f);
            GivePlayerDamage(5.0f, "BloodSplat", false,false);

            AddTimer("burn", 1.0f, nameof(TimerBurnFire));
        }
        //END ALTAR FIRE//


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("Storage");

            //---- COLLIDE CALLBACKS ----//
            AddEntityCollideCallback("Player", "AreaFire", nameof(CollideAreaFire), false, 0);

            AddEntityCollideCallback("Player", "AreaGiveQuest", nameof(CollideGiveQuest), true, 1);

            AddEntityCollideCallback("Player", "AreaBoomDamage", nameof(CollideBoomArea), false, 1);
            AddEntityCollideCallback("Player", "AreaBoomDeath", nameof(CollideBoomArea), false, 1);
            AddEntityCollideCallback("Player", "AreaBoomEscape", nameof(CollideBoomArea), false, 1);

            AddEntityCollideCallback("Player", "AreaLightsOut", nameof(CollideLightsOut), true, 1);

            AddEntityCollideCallback("Player", "HoleDudeStart", nameof(CollideHoleDude), true, 1);

            //AddEntityCollideCallback("grunt_normal_2", "AreaGruntLeave", "CollideGruntLeave", false, 0);

            for(int i=1;i<6;i++) AddEntityCollideCallback("Player", "AreaNoise_"+i, nameof(CollideAreaNoise), false, 0);

            for(int i=1;i<=8;i++)
            {
                AddEntityCollideCallback("Player", "AreaChain_"+i, nameof(CollideAreaChain), false, 1);
                AddEntityCollideCallback("Invisible_box_mass_2_"+i, "AreaChain_"+i, nameof(CollideChainEndWithAreaChain), false, 0);
            }

            AddEntityCollideCallback("Player", "AreaGruntRoom", nameof(CollideAreaGruntRoom), false, 0);
            AddEntityCollideCallback("grunt_in_room", "AreaGruntRoom", nameof(CollideAreaGruntRoom), false, 0);
            AddEntityCollideCallback("Player", "AreaGruntRoomActivate", nameof(CollideAreaGruntRoomActivate), false, 0);
            AddEntityCollideCallback("cellar_wood01_7", "AreaCloseDoor", nameof(CollideAreaCloseDoor), false, 0);

            //---- INTERACT CALLBACKS ----//
            for(int i=1;i<3;i++) AddUseItemCallback("usedrillpart", "hand_drill_part0"+i, "epoxy_container01", nameof(UseDrillPartOnEpoxy), false);
            for(int i=1;i<3;i++) AddUseItemCallback("usedrillpart", "hand_drill_part0"+i, "epoxy_container02", nameof(UseDrillPartOnEpoxy), false);
            AddUseItemCallback("usedrill1", "handdrill", "epoxy_container01", nameof(UseDrillOnEpoxy), false);
            AddUseItemCallback("usedrill2", "handdrill", "epoxy_container02", nameof(UseDrillOnEpoxy), false);

            AddUseItemCallback("usecontainer1", "empty_container", "epoxy_container01", nameof(UseEmptyContainerOnEpoxy), false);
            AddUseItemCallback("usecontainer2", "empty_container", "epoxy_container02", nameof(UseEmptyContainerOnEpoxy), false);

            AddUseItemCallback("usecontainerhalf11", "epoxy_container01_2", "epoxy_container01", nameof(UseHalfContainerOnEpoxy), false);
            AddUseItemCallback("usecontainerhalf12", "epoxy_container01_2", "epoxy_container02", nameof(UseHalfContainerOnEpoxy), false);

            AddUseItemCallback("usecontainerhalf21", "epoxy_container02_2", "epoxy_container01", nameof(UseHalfContainerOnEpoxy), false);
            AddUseItemCallback("usecontainerhalf22", "epoxy_container02_2", "epoxy_container02", nameof(UseHalfContainerOnEpoxy), false);

            AddUseItemCallback("placeepoxy", "epoxy_container01_4", "cave_in_1", nameof(UseEpoxyOnCavein), false);
            AddUseItemCallback("placeepoxy", "epoxy_container02_4", "cave_in_1", nameof(UseEpoxyOnCavein), false);

            SetNumberOfQuestsInMap(2);

            /*SetFogProperties(8, 32, 1, false);
            SetFogColor(0, 0, 0, 1.0f);
            SetFogActive(true);*/

            if(ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                for(int i=0;i<10;i++){
                    GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");
                    GiveItemFromFile("coin_"+i, "coins_large.ent");
                }
                GiveItemFromFile("empty_container", "chemical_container.ent");

                /*For script testing without the need to assemble all parts and things
                 */
                GiveItemFromFile("epoxy_container01_4", "chemical_container_epoxy.ent");
                GiveItem("handdrill", "Puzzle", "drill", "hand_drill.tga", 0);

                //AddEntityCollideCallback("Player", "AreaEnemy_1", "CollideEnemy1", true, 0);
            }
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            PreloadSound("close_door"); PreloadSound("12_valve_stuck"); PreloadSound("12_drill"); PreloadSound("12_epoxy_flow");
            PreloadSound("12_epoxy_fill"); PreloadSound("puzzle_place_jar"); PreloadSound("12_epoxy_drip"); PreloadSound("player_climb");
            PreloadSound("player_crouch"); PreloadSound("12_epoxy_blow"); PreloadSound("scare_whine_loop3"); PreloadSound("scare_thump_flash");
            PreloadSound("scare_tingeling"); PreloadSound("react_scare"); PreloadSound("00_creak");
            PreloadSound("general_chain_rattle_single");  PreloadSound("12_girl_cry");  PreloadSound("12_event_blood"); PreloadSound("12_girl_scream");
            PreloadSound("12_noise_1");  PreloadSound("12_noise_2"); PreloadSound("12_noise_2"); PreloadSound("12_noise_3");
            PreloadSound("12_noise_4"); PreloadSound("04_scream"); PreloadSound("03_rock_move"); PreloadSound("04_breath");

            PreloadParticleSystem("ps_dust_drilling"); PreloadParticleSystem("ps_liquid_epoxy");
            PreloadParticleSystem("ps_liquid_epoxy_splatt"); PreloadParticleSystem("ps_liquid_epoxy_drip");

            PlayMusic("12_amb", true, 1.0f, 4.0f, 0, true);

            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            RemoveTimer("mloop");
        }

        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);
    }
}
