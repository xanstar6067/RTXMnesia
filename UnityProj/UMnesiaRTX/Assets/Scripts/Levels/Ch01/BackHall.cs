using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/09_back_hall.hps: hub visions, fountain corruption and elevator preparation.
    /// Journal: docs/ScriptPort/ch01/09_back_hall.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/09_back_hall.map")]
    public class BackHall : HplScript
    {
        ////////////////////////////
        // Distant Howl - When player has everything needed to repair the elevator
        void TimerDistantHowl(string asTimer)
        {
            AddTimer("scare", 1.0f, nameof(TimerPlayerReact));
            AddTimer("breath", 3.0f, nameof(TimerPlayerReact));
            AddTimer("breathl", 5.0f, nameof(TimerPlayerReact));

            PlaySoundAtEntity("howl","guardian_distant3","Player", 0, false);
            AddDebugMessage("Distant howling...", false);
        }


        ////////////////////////////
        // Guardian Near - When player exits machine room and got the machine running
        void TimerGuardianNearEffect(string asTimer)
        {
            FadeOut(0.3f);
            AddTimer("TeleportHowl", 0.5f, nameof(TimerGuardianNearHowl));
            AddTimer("TeleportDone", 1.3f, nameof(TimerGuardianNearDone));

            AddTimer("scare", 0.3f, nameof(TimerPlayerReact));
            AddTimer("breath", 2.0f, nameof(TimerPlayerReact));
            AddTimer("breathl", 4.0f, nameof(TimerPlayerReact));
            AddTimer("breathl", 7.0f, nameof(TimerPlayerReact));

            PlaySoundAtEntity("stomp","scare_wall_stomp","Player", 0, false);

            PlayMusic("23_amb02.ogg", true, 1, 1, 0, true);
            PlayMusic("05_event_falling.ogg", false, 0.8f, 0.1f, 5, false);

            StartEffectFlash(0.2f, 0.1f,0.3f);

            FadeGlobalSoundVolume(0,3);
            StartScreenShake(0.1f, 4.7f, 0.05f, 0.5f);

            FadePlayerFOVMulTo(0.5f, 3);
        }

        void TimerGuardianNearHowl(string asTimer)
        {
            PlaySoundAtEntity("howl","guardian_distant3","Player", 0, false);

            FadeLightTo("BoxLight_2",0.2f, 0,0.05f,0, -1, 0.1f);

            for(int i=1; i<=6; ++i)
                CreateParticleSystemAtEntity("fog", "ps_scare_dust.ps", "SlineFog_"+i, false);

            for(int i=2; i<=5; ++i)
                SetLampLit("chandelier_simple_"+i, false, true);
            for(int i=5; i<=10; ++i)
                SetLampLit("candlestick_wall_"+i, false, true);
        }

        void TimerGuardianNearDone(string asTimer)
        {
            StopMusic(5.0f, 5);

            FadeGlobalSoundVolume(1,1);
            FadeIn(0.7f);

            FadePlayerFOVMulTo(1, 1);

            SetEntityActive("Even02Slime*", true);

            for(int i=3; i<=9; ++i)
                SetEntityActive("SlimeDamageArea_"+i, true);

            PlaySoundAtEntity("slimes2_1", "slime_loop", "Even02Slime_1", 2.0f, true);
            PlaySoundAtEntity("slimes2_2", "slime_loop", "Even02Slime_5", 2.0f, true);
            PlaySoundAtEntity("slimes2_3", "slime_loop", "Even02Slime_15", 2.0f, true);
            PlaySoundAtEntity("slimes2_4", "slime_loop", "Even02Slime_25", 2.0f, true);
            PlaySoundAtEntity("slimes2_5", "slime_loop", "Even02Slime_18", 2.0f, true);
        }

        ////////////////////////////
        // Start elevator
        void InteractElevatorLever(string asEntity)
        {
            if(GetGlobalVarInt("ElevatorMachineRunning")==0)
            {
                SetMessage("Ch01Level09", "ElevatorNotWorking", -1);
                AddQuest("09StartElevator", "09StartElevator");
            }
        }

        void PullElevatorLever(string asEntity, int alState)
        {
            if(alState != -1) return;

            SetLeverStuckState(asEntity, -1, true);
            SetSwingDoorLocked("elevator_door_1", true, true);

            PlaySoundAtEntity("activate","14_elevator_activate","Player", 0, false);

            AddTimer("StartElevator", 2.0f, nameof(TimerStartElevator));
        }

        void TimerStartElevator(string asTimer)
        {
            PlaySoundAtEntity("run","elevator_move_normal.snt","Player", 2, false);

            StartScreenShake(0.01f, 3, 1,0);
            ChangeMap("14_elevator.map", "PlayerStartArea_1", "","");
        }


        void CollideFlashBack(string asParent, string asChild, int alState)
        {
            AddTimer("trail", 1.0f, nameof(TimerCreateTrail));

            StopMusic(4.0f, 0);
        }
        void TimerCreateTrail(string asTimer)
        {
            AddLocalVarInt("Trail", 1);

            int iTrail = GetLocalVarInt("Trail");

            switch(iTrail) {
                case 1:
                    CreateParticleSystemAtEntity("rosesonmybed", "ps_rose_petals_wind.ps", "AreaTrail_1", false);
                    PlayGuiSound("general_wind_whirl6.ogg", 0.5f);
                    FadeLightTo("PointLight_30", 0.8f, 0.2f, 0.4f, -1, -1, 2);
                    //CreateParticleSystemAtEntity("trail"+iTrail, "ps_trail_large.ps", "AreaTrail_"+iTrail, false);
                break;
                case 2:
                    FadeLightTo("PointLight_30", 0.0f, 0.0f, 0.0f, -1, -1, 2);
                    StartPlayerLookAt("AreaTrail_1", 2, 2, "");
                    //MakeTrailEffect();
                break;
                case 3:
                    CreateParticleSystemAtEntity("rosesonmybed", "ps_rose_petals_wind.ps", "AreaTrail_3", false);
                    PlayGuiSound("general_wind_whirl6.ogg", 0.5f);
                    FadeLightTo("PointLight_30", 0.8f, 0.2f, 0.4f, -1, -1, 2);
                    AddTimer("push1", 0.3f, nameof(TimerPushElevatorDoors));
                    AddTimer("push2", 0.6f, nameof(TimerPushElevatorDoors));
                    AddTimer("push3", 0.9f, nameof(TimerPushElevatorDoors));
                    AddTimer("push4", 1.2f, nameof(TimerPushElevatorDoors));
                    AddTimer("push5", 1.5f, nameof(TimerPushElevatorDoors));
                    PlaySoundAtEntity("creak", "09_gate_open.snt", "elevator_door_1", 1.5f, false);
                    StopPlayerLookAt();
                    //MakeTrailEffect();
                break;
                case 4:
                    FadeLightTo("PointLight_30", 0.0f, 0.0f, 0.0f, -1, -1, 2);
                    StopSound("creak", 3.0f);
                    //MakeTrailEffect();
                break;
                case 5:
                    FadeLightTo("PointLight_30", 0.8f, 0.2f, 0.4f, -1, -1, 2);
                    //MakeTrailEffect();
                break;
                case 6:
                    FadeLightTo("PointLight_30", 0.0f, 0.0f, 0.0f, -1, -1, 6);
                    //CreateParticleSystemAtEntity("trail"+iTrail, "ps_trail_large.ps", "AreaTrail_"+iTrail, false);
                    AddTimer("trailkill", 20, nameof(TimerKillTrail));
                break;
            }

            if(iTrail < 6) AddTimer("trail", 2, nameof(TimerCreateTrail));
        }
        void MakeTrailEffect()
        {
            CreateParticleSystemAtEntity("trail"+GetLocalVarInt("Trail"), "ps_trail_flow.ps", "AreaTrail_"+GetLocalVarInt("Trail"), false);
        }
        /*Remove the trail when flashback done and some time has passed
         */
        void TimerKillTrail(string asTimer)
        {
            if(GetLocalVarInt("EventBloodFountain")==1)
                PlayMusic("01_amb_darkness", true, 1, 6, 0, true);
            else
                PlayMusic("09_amb_safe", true, 1, 6, 0, true);

            //DestroyParticleSystem("trail"+GetLocalVarInt("Trail"));

            //AddLocalVarInt("Trail", -1);

            //if(GetLocalVarInt("Trail") > 0) AddTimer("trailkill", 1, "TimerKillTrail");
        }

        void TimerPushElevatorDoors(string asTimer)
        {
            AddPropForce("elevator_door_1", -50.0f, 0, 0, "world");
        }
        ////////////////////////////
        // Machine room door locked

        void InteractMachineRoomDoor(string asEntity)
        {
            if(!HasItem("key_jar_1_item"))
                AddQuest("MachineRoomDoorLocked", "09UnlockMachineRoomDoor");
            else
                AddTimer("hintdelay", 1.5f, nameof(TimerDelayUnLockHint));    //If player has key when interacting with door, do not give quest, only message to use key
        }
        void TimerDelayUnLockHint(string asTimer)
        {
            SetMessage("Ch01Level02", "InteractDoorHaveKey", 0);
            SetEntityPlayerInteractCallback("level_wood_4", nameof(InteractMachineRoomDoor), true);
        }
        void UnlockMachineRoomDoor(string asItem, string asEntity)
        {
            RemoveTimer("hintdelay"); //Just so no quest is added if player is quick to use key

            PlaySoundAtEntity("unlock", "unlock_door", "Player", 0, false);
            SetLevelDoorLocked("level_wood_4", false);

            CompleteQuest("MachineRoomDoorLocked", "09UnlockMachineRoomDoor");

            RemoveItem("key_jar_1_item");
        }


        ///////////////////////////
        //PLAYER REACTIONS
        void TimerPlayerReact(string asTimer)
        {
            if(asTimer == "scare"){
                PlayGuiSound("react_scare", 1.0f);
                SetRadialBlurStartDist(0.3f);
                FadeRadialBlurTo(0.1f, 0.05f);
                StartScreenShake(0.02f, 0.5f, 1.0f, 2.0f);
            }
            else if(asTimer == "breath"){
                PlayGuiSound("react_breath", 0.9f);
                FadeRadialBlurTo(0.0f, 0.02f);
            }
            else if(asTimer == "breathl"){
                PlayGuiSound("react_breath", 0.5f);
            }
        }

        //Interact fountain
        void InteractFountainWater(string asEntity)
        {
            PlaySoundAtEntity("splashs", "impact_water_low.snt", asEntity, 0.0f, false);
            CreateParticleSystemAtEntity("splashps", "ps_impact_water_low.ps", asEntity, false);
            //SetMessage("Ch01Level09", "SignFontain", 0);

            AddTimer(asEntity, 1.0f, nameof(TimerFountainWater));
        }
        void TimerFountainWater(string asTimer)
        {
            SetEntityPlayerInteractCallback(asTimer, nameof(InteractFountainWater), true);
        }

        //Push door open to elevator room, to lure the player towars vision
        void CollideAreaShowDoor(string asParent, string asChild, int alState)
        {
            StartPlayerLookAt("castle_1", 2.0f, 2.0f, "");
            PlaySoundAtEntity("wind", "09_wind_whirl.snt", "AreaPushDoor", 0.0f, false);
            PlaySoundAtEntity("creak", "09_door_creak.snt", "castle_1", 0.5f, false);
            CreateParticleSystemAtEntity("windps", "ps_dust_push", "AreaPushDoor", false);

            SetSwingDoorClosed("castle_1", false, false);
            SetSwingDoorDisableAutoClose("castle_1", true);
            AddPropImpulse("castle_1", -2.0f, 0.0f, 0.0f, "World");

            AddTimer("pushagaindamnit", 0.3f, nameof(TimerSecondBreakfast));
            AddTimer("StopShow", 1.0f, nameof(TimerStopShow));
        }
        void TimerSecondBreakfast(string asTimer)
        {
            AddPropImpulse("castle_1", -2.0f, 0.0f, 0.0f, "World");
        }
        void TimerStopShow(string asTimer)
        {
            StopSound("creak", 1.0f);
            StopPlayerLookAt();
        }

        /////////////////////////////////////////
        // HERBERT VISION AREAS

        // Vision 1 (Random_Mirage01)
        void CollideWithHerbertVision_1()
        {
            SetGlobalVarInt("LastHerbertVisionTriggered", 1);

            SetUpHerbertVisionAreas();
        }

        // Vision 2 (Random_Mirage02)
        void CollideWithHerbertVision_2()
        {
            SetGlobalVarInt("LastHerbertVisionTriggered", 2);

            SetUpHerbertVisionAreas();
        }

        void SetUpHerbertVisionAreas()
        {
            AddDebugMessage("Vision_triggered:" + GetLocalVarInt("Vision_1_triggered"), false);
            AddDebugMessage("CompletedLevel_10_11:" + GetGlobalVarInt("CompletedLevel_10_11"), false);
            AddDebugMessage("CompletedLevel_12:" + GetGlobalVarInt("CompletedLevel_12"), false);

            // Initial setup : all disabled
            SetEntityActive("HerbertVisionArea_1_1", false);
            SetEntityActive("HerbertVisionArea_1_2", false);
            SetEntityActive("HerbertVisionArea_2_1", false);
            SetEntityActive("HerbertVisionArea_2_2", false);

            int lLastVisionTriggered = GetGlobalVarInt("LastHerbertVisionTriggered");
            // No visions have been triggered
            if(lLastVisionTriggered==0)
            {
                AddDebugMessage("Setting up vision 1", false);

                if(GetGlobalVarInt("CompletedLevel_10_11")==1)
                {
                    AddDebugMessage("Level 10 or 11 completed first, activating fountain area", false);
                    SetEntityActive("HerbertVisionArea_1_1", true);
                }
                else if(GetGlobalVarInt("CompletedLevel_12")==1)
                {
                    if(GetGlobalVarInt("GuidingRod")==3 && GetGlobalVarInt("PickedMachineInstructions")==1) return;    //To not collide with guardian event

                    AddDebugMessage("Level 12 completed first, activating 12 entrance area", false);
                    SetEntityActive("HerbertVisionArea_2_1", true);
                }
            }
            // First vision was triggered
            else if(lLastVisionTriggered==1)
            {
                AddDebugMessage("Setting up vision 2", false);

                if(GetGlobalVarInt("CompletedLevel_10_11")==2)
                {
                    AddDebugMessage("Level 10 or 11 completed second, activating fountain area", false);
                    SetEntityActive("HerbertVisionArea_1_2", true);
                }
                else if(GetGlobalVarInt("CompletedLevel_12")==2)
                {
                    if(GetGlobalVarInt("GuidingRod")==3 && GetGlobalVarInt("PickedMachineInstructions")==1) return;    //To not collide with guardian event
                    AddDebugMessage("Level 12 completed second, activating 12 entrance area", false);
                    SetEntityActive("HerbertVisionArea_2_2", true);
                }
            }
        }
        // END HERBERT VISION AREAS
        /////////////////////////////////////////

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("BackHall");

            ////////////////////
            // Hub start sanity boost
            GiveSanityBoost();

            ///////////////////////////////
            //Callbacks
            AddEntityCollideCallback("Player", "AreaFlashTrigger", nameof(CollideFlashBack), true, 1);

            AddUseItemCallback(nameof(UnlockMachineRoomDoor), "key_jar_1_item", "level_wood_4", nameof(UnlockMachineRoomDoor), true);

            AddEntityCollideCallback("Player", "AreaShowDoor", nameof(CollideAreaShowDoor), true, 1);

            ///////////////////////////////
            // Init Variables
            SetLocalVarInt("EventBloodFountain", 0);
            SetLocalVarInt("EventGuardianNear", 0);

            //////////////////////
            //Load Screen Setup
            // Setup here as it must only happen once.
            SetupLoadScreen("LoadingText", "Ch01_Diary03_", 6, "game_loading_trunk.jpg");


            ///////////////////////////////
            // Particle System
            CreateParticleSystemAtEntity("FountainPourWater", "ps_childsnake_water_stream.ps", "AreaFountainWater", true);

            ///////////////////////////////
            // Debug
            if(ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

                //GiveItemFromFile("key_jar_1_item", "key_study.ent");

                //- Start Blood fountain event
                //SetGlobalVarInt("GuidingRod", 3);
                //SetGlobalVarInt("PickedMachineInstructions", 1);

                //SetGlobalVarInt("CompletedLevel_10_11", 1);
                //SetGlobalVarInt("CompletedLevel_12", 1);
                //- Start the final howl event
                //SetGlobalVarInt("ElevatorMachineRunning", 1);

                //SetGlobalVarInt("EnemySeenInBackHallLevel",1);
            }
            else
            {
                PlayMusic("08_amb_success", false, 1, 1, 10, false);
            }
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            ///////////////////////////////////////
            // Guardian is near
            // - Check if elevator machine is running
            if(    GetGlobalVarInt("ElevatorMachineRunning")!=0 &&
                GetLocalVarInt("EventGuardianNear")==0)
            {
                SetLocalVarInt("EventGuardianNear", 1);

                SetSwingDoorLocked("castle_1", true, false);
                SetLevelDoorLocked("level_wood_4", true);

                AddTimer("GuardianNear", 2.1f, nameof(TimerGuardianNearEffect));

                SetLeverStuckState("elevator_lever_1", 0, false);
            }

            ///////////////////////////////////////
            // Blood in fountain event
            // - Check if player has picked up all machine guiding rods
            if(    GetGlobalVarInt("GuidingRod")==3 &&
                GetGlobalVarInt("PickedMachineInstructions")==1 &&
                GetLocalVarInt("EventBloodFountain")==0)
            {
                SetLocalVarInt("EventBloodFountain", 1);
                AddDebugMessage("Event Blood Fountain active", false);

                SetEntityActive("fountain_childsnake_1", false);
                SetEntityActive("childsnake_floor_water_normal_1", false);

                SetEntityActive("fountain_childsnake_blood_1", true);
                SetEntityActive("childsnake_floor_water_blood_1", true);

                SetEntityActive("SlimeDamageArea_1",true);
                SetEntityActive("SlimeDamageArea_2",true);
                SetEntityActive("SlimeDamageArea_10", true);

                SetEntityActive("Even01Slime*", true);
                PlaySoundAtEntity("slimes1", "slime_loop", "Even01Slime02_2", 0.0f, true);
                PlaySoundAtEntity("slimes2", "slime_loop", "Even01Slime08_6", 0.0f, true);
                PlaySoundAtEntity("slimes3", "slime_loop", "Even01Slime04_1", 0.0f, true);
                PlaySoundAtEntity("slimes4", "slime_loop", "Even01Slime08_4", 0.0f, true);
                PlaySoundAtEntity("slimes5", "slime_loop", "Even01Slime04_5", 0.0f, true);
                PlaySoundAtEntity("slimes6", "slime_loop", "Even01Slime02_4", 0.0f, true);

                FadeLightTo("BoxLight_2",0.17f,0.1f,0.22f,-1, -1, 0.1f);

                FadeOut(0.0f);
                FadeIn(4.0f);

                // New Particle System at fountain
                DestroyParticleSystem("FountainPourWater");
                CreateParticleSystemAtEntity("FountainPourBlood", "ps_childsnake_blood_stream.ps", "AreaFountainWater", true);

                for(int i=1;i<=4;i++)SetEntityActive("ScriptArea_"+i, false); //Remove interact water areas

                AddTimer("DistantHowl", 1.0f, nameof(TimerDistantHowl));
            }

            ///////////////////////////////////////
            // Bloody trail at Guest Room door
            // - Check if a grunt was activated in Guest Room
            if(GetGlobalVarInt("GruntLeftGuestRoom")==1)
            {
                SetPropActiveAndFade("grunt_trail_*", true, 0);
            }

            ////////////////////////////////////////////////////////
            // Set up Herbert visions
            SetUpHerbertVisionAreas();

            ////////////////////////////////////////////////////////
            // Set up correct music to play
            if(GetLocalVarInt("EventGuardianNear")==1)
            {
                //PlayMusic("07_amb", true, 1, 4, 0, true);
            }
            else if(GetLocalVarInt("EventBloodFountain")==1)
            {
                PlayMusic("01_amb_darkness", true, 1, 4, 0, true);
            }
            else if(GetGlobalVarInt("EnemySeenInBackHallLevel")==1)
            {
                PlayMusic("02_amb_safe.ogg", true, 1, 4, 0, true);
            }
            else
            {
                PlayMusic("09_amb_safe", true, 1, 4, 0, true);
            }

            PreloadParticleSystem("ps_scare_dust"); PreloadParticleSystem("ps_trail_large"); PreloadParticleSystem("ps_trail_flow");

            PreloadSound("guardian_distant3"); PreloadSound("scare_wall_stomp"); PreloadSound("14_elevator_activate");

            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            StopMusic(5, 0);
        }
    }
}
