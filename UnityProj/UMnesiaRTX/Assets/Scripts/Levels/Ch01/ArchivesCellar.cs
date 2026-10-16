using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/07_archives_cellar.hps: the flooded archives transition,
    /// timed gates, water lurker events, body parts and checkpoint resets.
    /// Journal: docs/ScriptPort/ch01/07_archives_cellar.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/07_archives_cellar.map")]
    public class ArchivesCellar : HplScript
    {


        ////////////////////////////
        // Start the first water lurker
        void CollideActiveWaterLurker(string asParent, string asChild, int alState)
        {
            SetEntityActive("waterlurker_1", true);

            AddTimer("scare", 0.5f, nameof(TimerPlayerReact));
            AddTimer("breath", 2.5f, nameof(TimerPlayerReact));
            AddTimer("breath", 4.5f, nameof(TimerPlayerReact));
        }

        void CollideActiveWaterLurker2(string asParent, string asChild, int alState)
        {
            SetEntityActive("waterlurker_3", true);

            AddTimer("scare", 0.5f, nameof(TimerPlayerReact));
            AddTimer("breath", 2.5f, nameof(TimerPlayerReact));
            AddTimer("breath", 4.5f, nameof(TimerPlayerReact));
        }


        ////////////////////////////
        // Player enters the teleport area
        void CollideTeleportToDarkSide(string asParent, string asChild, int alState)
        {
            FadeOut(0.3f);

            AddTimer("scare", 0.3f, nameof(TimerPlayerReact));
            AddTimer("breath", 2.0f, nameof(TimerPlayerReact));
            AddTimer("breathl", 4.0f, nameof(TimerPlayerReact));
            AddTimer("breathl", 6.0f, nameof(TimerPlayerReact));

            AddTimer("TeleportHowl", 0.5f, nameof(TimerTeleportHowl));
            AddTimer("TeleportDone", 3.5f, nameof(TimerTelportDone));

            PlaySoundAtEntity("stomp","scare_wall_stomp","Player", 0, false);
            PlaySoundAtEntity("darkamb","07_amb_breath","Player", 5, true);
            PlaySoundAtEntity("wateramb", "ambience_water_no3d.snt", "sound_idle_1", 5, true);

            FadePlayerFOVMulTo(4.0f, 4.0f);
            SetRadialBlurStartDist(0.1f);
            FadeRadialBlurTo(1.0f, 5.0f);

            StartEffectFlash(0.2f, 0.1f,0.3f);

            FadeGlobalSoundVolume(0, 0.3f);
            StopMusic(0.3f, 0);
            StartScreenShake(0.1f, 4.7f, 0.05f, 0.5f);

            FadePlayerFOVMulTo(0.5f, 3);
        }

        void TimerPlayerReact(string asTimer)
        {
            if(asTimer == "scare"){
                PlayGuiSound("react_scare", 1.0f);
            }
            else if(asTimer == "breath"){
                PlayGuiSound("react_breath", 0.8f);
            }
            else if(asTimer == "breathl"){
                PlayGuiSound("react_breath", 0.5f);
            }
        }

        void TimerTeleportHowl(string asTimer)
        {
            PlaySoundAtEntity("howl","guardian_activated.snt","Player", 0, false);
            SetPlayerActive(false);
            SetFogActive(true);
        }

        void TimerTelportDone(string asTimer)
        {
            TeleportPlayer("PlayerStartArea_3");
            FadeGlobalSoundVolume(1,2);
            SetPlayerActive(true);
            FadePlayerFOVMulTo(1.0f, 0.5f);
            FadeRadialBlurTo(0.0f, 1.0f);
            FadeIn(1.3f);

            FadePlayerFOVMulTo(1, 1);

            StartUpDarkSide();
        }


        ////////////////////////////
        // Start up all needed in dark side
        void StartUpDarkSide()
        {
            CheckPoint("check01","PlayerStartArea_3", nameof(CheckPoint01), "Hints", "DeathWaterLurker01");

            PlayMusic("07_amb.ogg", true, 1, 4.0f, 0, true);
        }


        ////////////////////////////
        // Open first gate with lever
        void GateLeverPulled(string asConnectionName, string asMainEntity, string asConnectEntity, int alState)
        {
            SetLeverStuckState(asMainEntity, -1, true);

            SetLocalVarInt("castle_portcullis_1", 1);
            SetEntityPlayerInteractCallback("castle_portcullis_1", "", true);

            StartScreenShake(0.01f, 0, 0.5f, 1.0f);
            PlaySoundAtEntity("clonk", "07_lever", asMainEntity, 0, false);
            PlaySoundAtEntity("clonk2", "07_lever2", asMainEntity, 0, false);
            PlaySoundAtEntity("ticking1", "07_tick", asMainEntity, 1, true);
            PlaySoundAtEntity("ticking2", "07_tick", asConnectEntity, 1, true);

            float fTime = 42.5f + GetLocalVarFloat("TimeAdd");
            AddTimer("Gate1Down", fTime, nameof(TimerFirstGateGoesDown));
            AddTimer("clonk2", 0.5f, nameof(TimerStopLeverSound));
            AddLocalVarFloat("TimeAdd", 2.5f);

            AddDebugMessage("Lever pulled to: "+alState + " time: "+Str(fTime), false);

        }
        void TimerStopLeverSound(string asTimer)
        {
            StopSound(asTimer, 1.0f);
        }
        void TimerFirstGateGoesDown(string asTimer)
        {
            SetMoveObjectState("castle_portcullis_1", 0);
            SetLeverStuckState("lever_simple01_1", 0, true);

            StopSound("ticking1", 0.5f);
            StopSound("ticking2", 0.5f);

            //TODO: Play *clonk* sound?
            SetEntityPlayerInteractCallback("castle_portcullis_1", nameof(InteractGate), true);
            SetLocalVarInt("castle_portcullis_1", 0);

            AddDebugMessage("Gate goes down!", false);
        }

        void CollidePastFirstGate(string asParent, string asChild, int alState)
        {
            //Make the gate came down instantly!
            RemoveTimer("Gate1Down");
            StopSound("ticking1", 0.5f);
            StopSound("ticking2", 0.5f);

            SetMoveObjectStateExt("castle_portcullis_1", 0, 3, 4, 0.1f, true);

            CompleteQuest("07Gate", "07Gate");

            SetEntityPlayerInteractCallback("castle_portcullis_1", "", true);
            SetLocalVarInt("castle_portcullis_1", 1);

            //Turn off first lurker and active new!
            SetEntityActive("waterlurker_1", false);
            SetEntityActive("waterlurker_3", false);
            SetEntityActive("waterlurker_2", true);

            // Check point
            CheckPoint("check02","PlayerStartArea_x4", nameof(CheckPoint02), "Hints", "DeathWaterLurker02");
        }

        ////////////////////////////
        // Gate close effect
        void CollideGateWithWater(string asParent, string asChild, int alState)
        {
            CreateParticleSystemAtEntity("splash", "ps_impact_water_high", asParent, false);
            PlaySoundAtEntity("splash", "impact_water_med", asParent, 0, false);
        }


        ////////////////////////////
        // Start the final special lurker
        void CollideActiveWaterLurker3(string asParent, string asChild, int alState)
        {
            AddTimer("destroydoor1", 0, nameof(TimerDestroyDoor));
            AddTimer("destroydoor1", 1, nameof(TimerDestroyDoor));
            AddTimer("destroydoor1", 2, nameof(TimerDestroyDoor));
            AddTimer("startlastlurker", 2, nameof(TimerActivateLastLurker));
        }

        void TimerDestroyDoor(string asTimer)
        {
            AddPropHealth("cellar_wood01_2", -35.0f);

            PlaySoundAtEntity("roar", "water_lurker_hunt", "cellar_wood01_2", 0, false);
            PlaySoundAtEntity("break", "break_wood_metal", "cellar_wood01_2", 0, false);
        }

        void TimerActivateLastLurker(string asTimer)
        {
            SetEntityActive("waterlurker_4", true);
        }

        ////////////////////////////
        // Open door out of level.
        void UseHollowNeedleOnDoor(string asItem, string asEntity)
        {
            SetLevelDoorLocked(asEntity, false);
            PlaySoundAtEntity("unlock", "07_pick_lock", "Player", 0, false);
            PlayMusic("02_puzzle.ogg", false, 0.7f, 0, 10, false);
            GiveSanityBoostSmall();
        }


        void TimerEntranceHowl(string asTimer)
        {
            PlaySoundAtEntity("howl", "guardian_distant2.snt", "waterlurker_1", 1, false);
        }

        void TimerRandomGuardian(string asTimer)
        {
            int iRand = (int)RandFloat(1,8);

            PlaySoundAtEntity("idle"+iRand, "guardian_idle", "sound_idle_"+iRand, 0, false);

            AddTimer("idle", RandFloat(10,30), nameof(TimerRandomGuardian));
        }


        ////////////////////////////
        // Create all the body parts
        void CreateOrResetEntity(string asName, string asFile, string asArea)
        {
            if(GetEntityExists(asName)==false)
            {
                CreateEntityAtArea(asName, asFile, asArea, true);
            }
            else
            {
                ResetProp(asName);
            }
        }

        void CreateAndResetBodyParts()
        {
            for(int i=1; i<=4; ++i)
            {
                CreateOrResetEntity("FoodArm"+i, "/organic/corpse_male/corpse_male_arm.ent", "AreaArm_"+i);
            }
            for(int i=1; i<=3; ++i)
            {
                CreateOrResetEntity("FoodLeg"+i, "/organic/corpse_male/corpse_male_leg.ent", "AreaLeg_"+i);
            }
            for(int i=1; i<=2; ++i)
            {
                CreateOrResetEntity("FoodTorso"+i, "/organic/corpse_male/corpse_male_torso.ent", "AreaTorso_"+i);
            }

        }

        ////////////////////////////
        // CheckPoints
        void CheckPoint01(string asName, int alCount)
        {
            SetPlayerHealth(RandFloat(80, 95));

            RemoveTimer("Gate1Down");
            TimerFirstGateGoesDown("Gate1Down");

            AddLocalVarFloat("TimeAdd", 3);

            if(alCount < 1)
            {
                AddEntityCollideCallback("Player","ActiveWaterLurker", nameof(CollideActiveWaterLurker), true, 0);
            }
            else if(alCount == 1)
            {
                AddEntityCollideCallback("Player", "ActiveWaterLurker2", nameof(CollideActiveWaterLurker2), true, 0);
            }
        }

        void CheckPoint02(string asName, int alCount)
        {
            SetPlayerHealth(RandFloat(80, 95));

            SetWheelAngle("valve_iron_1", 0, false);
            SetMoveObjectState("castle_portcullis_2", 0); //Need to do this since wheel only updates on interaction!
            ResetProp("cellar_wood01_2");

            CreateAndResetBodyParts();

            if(alCount < 1)
            {
                SetEntityActive("waterlurker_2", true);
            }
            else if(alCount == 1)
            {
                AddEntityCollideCallback("Player", "ActivateLurker3", nameof(CollideActiveWaterLurker3), true, 0);
            }
        }


        ///////////////////////////
        //INTERACT SECURITY GATES
        void InteractGate(string asEntity)
        {
            PlayGuiSound("17_attach_crank", 0.5f);
            SetMessage("Ch01Level07", "SecurityGate", 0);

            SetMoveObjectState(asEntity, 0.05f);

            AddTimer(asEntity, 0.4f, nameof(TimerCloseGate));
            AddTimer("sound", 0.8f, nameof(TimerCloseGate));
        }
        void TimerCloseGate(string asTimer)
        {
            if(asTimer == "sound"){
                PlayGuiSound("close_gate.ogg", 0.5f);
            } else{
                SetMoveObjectState(asTimer, 0.00f);

                if(GetLocalVarInt(asTimer) != 1)
                    SetEntityPlayerInteractCallback(asTimer, nameof(InteractGate), true);
            }

            if(asTimer == "castle_portcullis_1")
                AddQuest("07Gate", "07Gate");
        }
        void InteractWheel(string asEntity)
        {
            SetEntityPlayerInteractCallback("castle_portcullis_2", "", true);
        }


        ////////////////////////////
        //MISC STUFF
        void InteractSlimeExit(string asEntity)
        {
            PlayGuiSound("slime_create.snt", 0.8f);

            SetMessage("Ch01Level07", "DoorBlockedSlime", 0);

            AddQuest("07PushOnward", "07PushOnward");

            AddTimer("turnonagain", 1.0f, nameof(TimerSlimeExit));
        }
        void TimerSlimeExit(string asTimer)
        {
            SetEntityPlayerInteractCallback("ScriptArea_1", nameof(InteractSlimeExit), true);
        }
        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("ArchivesCellar");

            ///////////////////////////////
            // Create all body parts
            CreateAndResetBodyParts();

            ///////////////////////////////
            // Vars
            SetLocalVarFloat("TimeAdd", 0); //Time add for lever gate

            ///////////////////////////////
            // Set up areas
            AddEntityCollideCallback("Player","TeleportToDarkSideArea", nameof(CollideTeleportToDarkSide), true, 0);
            AddEntityCollideCallback("Player","ActiveWaterLurker", nameof(CollideActiveWaterLurker), true, 0);
            AddEntityCollideCallback("Player","PastFirstGate", nameof(CollidePastFirstGate), true, 0);

            AddEntityCollideCallback("GateCloseEffect_1", "castle_portcullis_1", nameof(CollideGateWithWater), false, 1);
            AddEntityCollideCallback("GateCloseEffect_2", "castle_portcullis_2", nameof(CollideGateWithWater), false, 1);


            AddTimer("EntranceHowl", 2, nameof(TimerEntranceHowl));
            AddTimer("idle", RandFloat(8,16), nameof(TimerRandomGuardian));

            ///////////////////////////////
            // Callbacks
            AddUseItemCallback("UnlockDoor", "hollow_needle_1", "level_wood_1", nameof(UseHollowNeedleOnDoor), false);

            ///////////////////////////////
            // Connections
            ConnectEntities("FirstGate","lever_simple01_1","castle_portcullis_1", true, -1, nameof(GateLeverPulled));

            InteractConnectPropWithMoveObject("SecondGate","valve_iron_1", "castle_portcullis_2",true, false, 0);

            SetEntityCustomFocusCrossHair("ScriptArea_1", "Default");

            SetFogActive(false);

            ///////////////////////////////
            // Clear previous save maps
            ClearSavedMaps();

            ///////////////////////////////
            // Debug
            if(ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                StartUpDarkSide();
            }
        }

        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            PlayMusic("01_amb_darkness.ogg", true,1.0f, 2.0f, 0, true);

            PreloadSound("scare_wall_stomp"); PreloadSound("guardian_distant3"); PreloadSound("gameplay_tick_long_dist");
            PreloadSound("ater_lurker_hunt"); PreloadSound("break_wood_metal"); PreloadSound("unlock_door"); PreloadSound("guardian_activated");
            PreloadSound("guardian_idle");

            PreloadParticleSystem("ps_impact_water_high");


            ///////////////////
            // Auto save
            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            SetPlayerCrouching(false); //It looks strange when starting level 8 crouching in water.

            CompleteQuest("07PushOnward", "07PushOnward");

            //////////////////////
            //Load Screen Setup
            SetupLoadScreen("LoadingText", "Ch01_WaterLurker02", 1, "game_loading_water.jpg");
        }

        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);
    }
}
