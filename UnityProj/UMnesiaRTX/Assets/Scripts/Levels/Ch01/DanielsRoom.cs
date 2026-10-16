using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/10_daniels_room.hps: crowbar door, hidden key, grunt events and checkpoints.
    /// Journal: docs/ScriptPort/ch01/10_daniels_room.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/10_daniels_room.map")]
    public class DanielsRoom : HplScript
    {
        void BreakBottle(string asEntity, string asType)
        {
            PlayMusic("10_puzzle02.ogg", false, 0.7f, 0.1f, 10, false);
            GiveSanityBoostSmall();

            CompleteQuest("10HiddenObject", "10HiddenObject");

            AddTimer("startlook", 0.2f, nameof(TimerStopLook));
            AddTimer("stoplook", 1.0f, nameof(TimerStopLook));

            AddDebugMessage("Break Bottle!", false);

            AddDebugMessage("Setting level completion", false);
            AddDebugMessage("CompletedLevel_12: " + GetGlobalVarInt("CompletedLevel_12"), false);
            AddDebugMessage("(Before) CompletedLevel_10_11: " + GetGlobalVarInt("CompletedLevel_10_11"), false);

            if(GetGlobalVarInt("CompletedLevel_10_11")==0)
                SetGlobalVarInt("CompletedLevel_10_11", GetGlobalVarInt("CompletedLevel_12")+1);

            AddDebugMessage("(After) CompletedLevel_10_11: " + GetGlobalVarInt("CompletedLevel_10_11"), false);
        }
        void TimerStopLook(string asTimer)
        {
            if(asTimer == "stoplook")
                StopPlayerLookAt();
            else
                StartPlayerLookAt("key_jar_1_item", 5.0f, 5.0f, "");
        }

        void InteractClosedDoor(string asEntity)
        {
            AddTimer("doorm", 0.5f, nameof(TimerDoorMessage));
        }
        void TimerDoorMessage(string asTimer)
        {
            SetMessage("Ch01Level10", "DoorLocked", 0);
            AddQuest("10Door", "10Door");
        }
        ////////////////////////////
        //BEGIN GRUNT ENTERS LEVEL//
        //When grunt about to leave the closet, he gets a new path directly to exit door.
        void CollideAreaGruntInCloset(string asParent, string asChild, int alState)
        {
            AddDebugMessage("Grunt is state: " + alState, false);

            GiveEnemyExitPath();

            SetLocalVarInt("GruntLeftCloset", 1);
        }
        void GiveEnemyExitPath()
        {
            AddDebugMessage("Exit Grunt changes", false);

            ClearEnemyPatrolNodes("grunt_normal_1");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_28", 0, "");
        }


        void CollideAreaGrunt02(string asParent, string asChild, int alState)
        {
            if(GetLocalVarInt("GruntLeftCloset") == 1) return;

            AddDebugMessage("Area 02 Grunt changes", false);

            ClearEnemyPatrolNodes("grunt_normal_1");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_7", 3, "");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_31", 1, "");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_27", 0.5f, "");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_31", 0, "");    //To avoid the opened door, which can make him wanna break it.
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_30", 0, "");
        }
        void CollideAreaGrunt(string asParent, string asChild, int alState)
        {
            PlayMusic("10_event_coming.ogg", false, 1, 0.1f, 10, false);

            PlaySoundAtEntity("grunt","grunt/amb_idle.snt", "chest_of_drawers_nice_2", 3, false);
            PlaySoundAtEntity("doorgrunt","10_close_door.snt", "level_wood_1", 1, false);

            CheckPoint("check01","PlayerStartArea_2", nameof(CheckPoint01), "Hints", "DeathGrunt_10_Daniels_Room_01");

            AddTimer("grunt1", 0.75f, nameof(TimerActivateGrunt));
            AddTimer("grunt2", 1.25f, nameof(TimerActivateGrunt));
            AddTimer("exiton", 5.0f, nameof(TimerExitGrunt));
        }
        void TimerActivateGrunt(string asTimer)
        {
            if(asTimer == "grunt2"){
                GiveHint("hidehint", "Hints", "HideHint", 0);
                return;
            }
            AddEntityCollideCallback("Player", "AreaCheckPlayer", nameof(CollideAreaCheckPlayer), false, 0);
            AddEntityCollideCallback("Player", "AreaCheckPlayer2", nameof(CollideAreaCheckPlayer), false, 0);

            PlaySoundAtEntity("scare","react_scare.snt", "Player", 0.5f, false);
            GiveSanityDamage(10.0f, true);

            SetEntityActive("grunt_normal_1", true);

            SetGlobalVarInt("EnemySeenInBackHallLevel",1);//tells 09 to play other music!

            AddDebugMessage("Area 01 Grunt changes", false);

            ClearEnemyPatrolNodes("grunt_normal_1");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_7", 3, "");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_14", 1, "");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_16", 0.5f, "");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_30", 0, "");

            AddEntityCollideCallback("Player", "AreaGrunt_2", nameof(CollideAreaGrunt02), true, 1);
        }

        void TimerExitGrunt(string asTimer)
        {
            AddEntityCollideCallback("grunt_normal_1", "AreaGruntDeactivate", nameof(CollideAreaGruntDeactivate), false, 1);
        }

        void CollideAreaCheckPlayer(string asParent, string asChild, int alState)
        {
            AddDebugMessage("In Area: "+asChild+" State: "+alState, false);

            if(alState == 1) SetLocalVarInt("PlayerInArea", alState);
            else SetLocalVarInt("PlayerInArea", alState);
        }
        void CollideAreaGruntDeactivate(string asParent, string asChild, int alState)
        {
            if(GetLocalVarInt("PlayerInArea") == 1){
                SetEntityActive("grunt_normal_1", false);
                PlaySoundAtEntity("doorgruntleave","10_open_door.snt", "level_wood_1", 0.5f, false);

                SetDeathHint("", "");

                SetGlobalVarInt("GruntLeftGuestRoom", 1);
            }
            else{
                ClearEnemyPatrolNodes("grunt_normal_1");
                AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_32", 0, "");
                AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_28", 0, "");
            }
        }
        //END GRUNT ENTERS LEVEL//
        //////////////////////////


        ////////////////////
        //BEGIN BREAK DOOR//


        void UseCrowbarOnDoor(string asItem, string asEntity)
        {
            AddTimer(asEntity, 0.2f, nameof(TimerSwitchShovel));
            PlaySoundAtEntity("pickupcrow","player_crouch.snt", "Player", 0.05f, false);

            //Remove callback incase player never touched door
            SetEntityPlayerInteractCallback("mansion_1", "", true);
            SetEntityPlayerInteractCallback("AreaUseCrowbar", "", true);

            RemoveItem(asItem);
        }
        void TimerSwitchShovel(string asTimer)
        {
            PlaySoundAtEntity("attachshovel","puzzle_place_jar.snt", asTimer, 0, false);

            SetEntityActive("crowbar_joint_1", true);
        }
        void CollideAreaBreakDoor(string asParent, string asChild, int alState)
        {
            GiveSanityBoostSmall();

            PlayMusic("10_puzzle01.ogg", false, 0.7f, 0.1f, 10, false);

            SetSwingDoorLocked("mansion_1", false, false);
            SetSwingDoorDisableAutoClose("mansion_1", true);
            SetSwingDoorClosed("mansion_1", false,false);

            PlaySoundAtEntity("break","break_wood_metal", "AreaBreakEffect", 0, false);
            CreateParticleSystemAtEntity("breakps", "ps_hit_wood", "AreaBreakEffect", false);
            AddPropImpulse("mansion_1", -3, 0, 0, "world");

            SetEntityActive("crowbar_joint_1", false);
            SetEntityActive("crowbar_dyn_1", true);

            AddTimer("pushdoor", 0.1f, nameof(TimerPushDoor));
            AddTimer("voice2", 1, nameof(TimerDanielVoices));

            CompleteQuest("10Door", "10Door");

            AddDebugMessage("Break door!", false);
        }
        void TimerPushDoor(string asTimer)
        {
            AddPropImpulse("mansion_1", -1, 2, -4, "world");
            AddTimer("doorclose", 1.1f, nameof(TimerDoorCanClose));
        }

        void TimerDoorCanClose(string asTimer)
        {
            SetSwingDoorDisableAutoClose("mansion_1", false);
        }

        //END BREAK DOOR//
        //////////////////

        void TimerDanielVoices(string asTimer)
        {
            if(asTimer == "voice1") SetEntityActive("FlashbackArea_1", true);
            else if(asTimer == "voice2") SetEntityActive("FlashbackArea_2", true);
            else SetEntityActive("FlashbackArea_3", true);
        }
        void VoiceOver1()
        {
            //AddTimer("voice2", 5, "TimerDanielVoices");
        }
        void VoiceOver2()
        {
            //AddTimer("voice3", 7, "TimerDanielVoices");
            SetLocalVarInt("VisionOver", 1);
            AddQuest("10HiddenObject", "10HiddenObject");
        }

        void DiaryCall(string asItem, int lDiaryIdx)
        {
            PlayMusic("10_paper_daniel0"+lDiaryIdx, false, 1.0f, 0.2f, 10, true);
        }

        /////////////////////
        //BEGIN CHECK POINT//

        void CheckPoint01(string asName, int alCount)
        {
            ResetProp("mansion_1");
            ResetProp("cabinet_simple_1");
            ResetProp("cabinet_simple_2");

            SetEntityActive("AreaGruntDeactivate", false);
            AddTimer("areaonagain", 5.0f, nameof(TimerAreaOnAgain));

            if(alCount < 1)
            {
                SetEntityActive("grunt_normal_1", true);

                AddDebugMessage("CP Grunt changes", false);

                ClearEnemyPatrolNodes("grunt_normal_1");
                AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_13", 3, "");
                AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_14", 2,"");
                AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_30", 0, "");

                CheckPoint("check02","PlayerStartArea_3", nameof(CPCall02), "", "");

                AddEntityCollideCallback("grunt_normal_1", "AreaGruntInCloset", nameof(CollideAreaGruntInCloset), true, -1);
            }


        }
        void TimerAreaOnAgain(string asTimer)
        {
            SetEntityActive("AreaGruntDeactivate", true);
        }


        void CPCall02(string asName, int alCount)
        {
            StartPlayerLookAt("CheckPointLookAt", 10.0f, 10.0f, "");

            AddTimer("stoplook", 1.0f, nameof(TimerStopLookCP));

            SetEntityPlayerLookAtCallback("CPParticle", nameof(LookAtCorpse), true);

            CreateParticleSystemAtEntity("smoke", "ps_area_fog.ps", "CPParticle", false);
            CreateParticleSystemAtEntity("smoke2", "ps_area_fog.ps", "CPParticle", false);

            SetEntityActive("corpse_pile2_1", true);
        }
        void TimerStopLookCP(string asTimer)
        {
            StopPlayerLookAt();
        }
        void LookAtCorpse(string asEntity, int alState)
        {
            PlaySoundAtEntity("pooff", "03_in_a_bottle.snt", "bonfire_3", 0.0f, false);
            PlaySoundAtEntity("pooff2", "03_rock_move.snt", "bonfire_3", 0.0f, false);
            SetPropActiveAndFade("corpse_pile2_1", false, 3);
            DestroyParticleSystem("smoke");
            DestroyParticleSystem("smoke2");
        }
        //END CHECK POINT//
        /////////////////////

        //////////////////////////////
        //START LOOK AT PAINTING HINT
        void LookAtPainting(string asEntity, int alState)
        {
            if(GetLocalVarInt("VisionOver") != 1) return;

            for(int i=1;i<4;i++)
                CreateParticleSystemAtEntity("dustceiling_"+i, "ps_dust_falling_small_thin.ps", "ScriptArea_"+i, false);

            PlaySoundAtEntity("rumble", "10_rock_move", "painting03_dynamic_1", 0.25f, false);

            AddTimer("impulse", 0.5f, nameof(TimerShakePainting));
            AddTimer("impulse", 0.9f, nameof(TimerShakePainting));
            AddTimer("impulse", 1.4f, nameof(TimerShakePainting));
            AddTimer("impulse", 1.6f, nameof(TimerShakePainting));

            AddTimer("shake", 0.3f, nameof(TimerShakePainting));

            SetLocalVarInt("VisionOver", 2);

            AddDebugMessage("Spooky painting", false);
        }
        void TimerShakePainting(string asTimer)
        {
            if(asTimer == "shake")
                StartScreenShake(0.008f, 0.1f, 0.25f, 0.65f);
            else
                AddPropImpulse("painting03_dynamic_1", 0.5f, 1.0f, 0.5f, "");
        }
        void CollideMovePainting(string asParent, string asChild, int alState)
        {
            AddTimer("voice3", 0.2f, nameof(TimerDanielVoices));

            AddDebugMessage("Painting pulled!", false);
        }
        //END LOOK AT PAINTING HINT
        ///////////////////////////


        //touch bed for flashback
        void InteractAreaBed(string asEntity)
        {
            SetEntityActive("AreaBed_1", true);
        }

        //Sound on grunt auto remove
        void EntityCallGrunt(string asEntity, string type)
        {
            PlaySoundAtEntity("doorgruntleave","10_open_door.snt", "level_wood_1", 0.5f, false);
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("DanielsRoom");

            //----COLLIDE CALLBACKS----//
            AddEntityCollideCallback("Player", "AreaGrunt", nameof(CollideAreaGrunt), true, 1);
            AddEntityCollideCallback("crowbar_joint_1", "BreakDoor", nameof(CollideAreaBreakDoor), true, 1);
            AddEntityCollideCallback("ScriptArea_4", "painting03_dynamic_1", nameof(CollideMovePainting), true, -1);

            AddEntityCollideCallback("grunt_normal_1", "AreaGruntInCloset", nameof(CollideAreaGruntInCloset), true, -1);

            AddUseItemCallback("crowbarondoor", "crowbar_1", "mansion_1",nameof(UseCrowbarOnDoor), true);
            AddUseItemCallback("crowbaronframe", "crowbar_1", "AreaUseCrowbar", nameof(UseCrowbarOnDoor), true);


            //----ROOM MESS SETUP----//
            AddBodyImpulse("chest_of_drawers_simple_2_Body_1", 0.25f, 0, 0, "world");
            AddBodyImpulse("chest_of_drawers_simple_2_Body_2", 0.75f, 0, 0, "world");
            AddBodyImpulse("chest_of_drawers_simple_2_Body_3", 0.5f, 0, 0, "world");
            AddBodyImpulse("chest_of_drawers_simple_1_Body_1", -0.5f, 0, 0, "world");
            AddBodyImpulse("chest_of_drawers_simple_1_Body_2", -1, 0, 0, "world");

            AddBodyImpulse("work_desk_1_Body1", 0, 0, -2, "world");
            AddBodyImpulse("work_desk_1_Body2", 0, 0, -3, "world");
            AddBodyImpulse("work_desk_1_Body3", 0, 0, -1, "world");
            AddBodyImpulse("work_desk_2_Body3", 0, 0, -0.5f, "world");
            AddBodyImpulse("work_desk_2_Body1", 0, 0, -0.7f, "world");

            AddTimer("voice1", 1, nameof(TimerDanielVoices));


            if(ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");
            }
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            PlayMusic("10_amb", true, 0.7f, 5, 0, true);

            PreloadSound("grunt/amb_idle"); PreloadSound("door_level_wood_close"); PreloadSound("react_scare"); PreloadSound("door_level_wood_open");
            PreloadSound("puzzle_place_jar"); PreloadSound("break_wood_metal");

            PreloadParticleSystem("ps_hit_wood");

            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            SetEntityActive("grunt_normal_1", false);

            //////////////////////
            //Load Screen Setup
            SetupLoadScreen("LoadingText", "Ch01_Diary04_", 5, "game_loading_hand.jpg");
        }
    }
}
