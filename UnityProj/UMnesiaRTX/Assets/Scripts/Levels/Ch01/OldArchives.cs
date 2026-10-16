using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/01_old_archives.hps: the dust door, the closing door scare with the critters, the
    /// ghost in the abandoned study (books, lantern), the random head sway, the lantern and oil, the trail quest,
    /// the secret passage behind the shelf and the tremble before the last room.
    /// Journal: docs/ScriptPort/ch01/01_old_archives.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/01_old_archives.map")]
    public class OldArchives : HplScript
    {
        /////////////////////////////////
        //BEGIN OPENING DOORS WITH DUST//

        void CollideOpeningDustDoor(string asParent, string asChild, int alState)
        {
            StopRandomLook(); //De-activate the spinning head

            CreateParticleSystemAtEntity("PSDoor_1", "ps_dust_paper_blow.ps", "AreaDoor_1", false);
            CreateParticleSystemAtEntity("PSDoor_2", "ps_dust_push.ps", "AreaDoor_1", false);

            PlaySoundAtEntity("SoundDoor_1", "scare_wind_reverse", "AreaDoor_1", 1.0f, false);
            PlaySoundAtEntity("creaking_door", "joint_door_move_special.snt", "Door_1", 1.0f / 2.5f, false);
            PlaySoundAtEntity("SoundBong", "scare_tingeling.snt", "Player", 0.0f, false);

            AddTimer("opendoor_1", 0.25f, nameof(TimerOpenDoor01));
            AddTimer("lightsout", 1, nameof(TimerOpenDoor01));
            AddTimer("stopeffect", 2, nameof(TimerOpenDoor01));

            StartScreenShake(0.007f, 2, 0.25f, 1);

            FadePlayerFOVMulTo(1.5f, 0.5f);

            // For CollideBeginSwirl
            PlaySoundAtEntity("SoundFeet1", "01_tiny1", "AreaBeginSwirl", 15.0f, false);

            // DEBUG
            AddDebugMessage("The door at " + asChild + " opens with dust", true);
        }

        void TimerOpenDoor01(string asTimer)
        {
            if (asTimer == "stopeffect")
            {
                FadePlayerFOVMulTo(1, 1);
                PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.75f, false);
                AddTimer("lookloop", 1, nameof(TimerRandomLook)); //Re-activate the spinning head
                return;
            }

            if (asTimer == "lightsout")
            {
                for (int i = 1; i <= 4; i++) SetLampLit("torch_static01_" + i, false, true);
                for (int i = 1; i <= 4; i++) FadeLightTo("LightOff_" + i, 0, 0, 0, 0, 0, 1.5f);
                PlaySoundAtEntity("breath2", "react_breath.snt", "Player", 1.0f / 1, false);
                return;
            }

            PlaySoundAtEntity("Wind", "general_wind_whirl", "Player", 2, false);
            PlaySoundAtEntity("scare", "react_scare.snt", "Player", 0.75f, false);

            StopSound("creaking_door", 1.0f);

            PlayMusic("01_amb_darkness.ogg", true, 1.0f, 0, 0, true);

            CreateParticleSystemAtEntity("PSDoor_3", "ps_dust_push.ps", "AreaDoor_2", false);
            CreateParticleSystemAtEntity("PSDoor_4", "ps_dust_push.ps", "AreaDoor_3", false);

            SetSwingDoorClosed("Door_1", false, false);
            SetSwingDoorDisableAutoClose("Door_1", true);

            AddTimer("Door_1", 0.01f, nameof(TimerSwingDoor));

            GiveSanityDamage(10, true);
        }

        void TimerSwingDoor(string asTimer)
        {
            if (GetLocalVarInt("SwingDoor") == 10)
            {
                SetLocalVarInt("SwingDoor", 0);
                return;
            }

            if (asTimer == "Door_1") AddPropForce(asTimer, 160.0f, 0, 0, "World");
            else AddPropForce(asTimer, -60.0f, 0, 0, "World");

            AddLocalVarInt("SwingDoor", 1);

            AddTimer(asTimer, 0.03f, nameof(TimerSwingDoor));

            AddDebugMessage("Swing: " + GetLocalVarInt("SwingDoor"), false);
        }
        //END OPENING DOORS WITH DUST//

        ////////////////////////////
        //START CLOSING DOOR SCARE//
        // Player in area in abandoned storage and door closes + scare event

        void CollideBeginSwirl(string asParent, string asChild, int alState)
        {
            StopRandomLook(); //De-activate the spinning head

            FadeImageTrailTo(1.7f, 1.5f);

            //INCREASE THE INTENSITY AND VOLUME OF THE RUNNING FEET SOUND
            StartScreenShake(0.01f, 1, 0.1f, 0.5f);

            PlaySoundAtEntity("SoundFeet2", "01_tiny2", "AreaFeet_1", 3, false);

            AddTimer("swirl", 0.5f, nameof(TimerSwirlHorror));
        }

        void TimerSwirlHorror(string asTimer)
        {
            AddLocalVarInt("VarSwirl", 1); //What step to play in the event
            float fSpeedWhirl = 0.5f; //The default time between steps in an event

            switch (GetLocalVarInt("VarSwirl"))
            {
                case 1:
                    PlaySoundAtEntity("SoundFeet3", "01_tiny3", "AreaFeet_2", 3, false);
                    PlaySoundAtEntity("SoundBong2", "scare_tingeling_rev2.snt", "Player", 0.0f, false);
                    fSpeedWhirl = 3.0f;
                    break;
                case 2:
                    PlaySoundAtEntity("SoundFeet4", "01_tiny4", "AreaFeet_3", 1, false);
                    SetPropActiveAndFade("cockroach_*", true, 0.5f);
                    break;
                case 3:
                    FadePlayerAspectMulTo(2, 0.05f);
                    FadeImageTrailTo(1.7f, 1.5f);
                    PlaySoundAtEntity("scare", "react_scare.snt", "Player", 0.5f, false);
                    PlayMusic("01_event_critters.ogg", false, 1, 0, 10, false);
                    GiveSanityDamage(10, true);
                    break;
                case 4:
                    fSpeedWhirl = 0.15f;
                    break;
                case 5:
                    PlaySoundAtEntity("scare", "react_scare.snt", "Player", 0.6f, false);
                    fSpeedWhirl = 2.5f;
                    break;
                case 6:
                    break;
                case 7:
                    for (int i = 1; i <= 4; i++) StopSound("SoundFeet" + i, 2);
                    PlaySoundAtEntity("breath2", "react_breath.snt", "Player", 0.7f, false);
                    fSpeedWhirl = 1.0f;
                    break;
                case 8:
                    FadeImageTrailTo(1.6f, 3.0f);
                    FadePlayerAspectMulTo(2, 0.01f);
                    FadeOut(20);
                    StopMusic(10, 0);
                    FadeGlobalSoundSpeed(0.4f, 10);
                    FadeGlobalSoundVolume(0.1f, 10);
                    StartScreenShake(0.02f, 0.1f, 2, 5);
                    PlaySoundAtEntity("rumbleoo", "00_loop", "Player", 4.0f, false);
                    PlaySoundAtEntity("scare3", "react_sigh.snt", "Player", 0.8f, false);
                    AddTimer("breath", 1, nameof(TimerBreath));
                    SetPlayerRunSpeedMul(0);
                    SetPlayerMoveSpeedMul(0.75f);
                    fSpeedWhirl = 1.0f;
                    break;
                case 9:
                    SetPlayerMoveSpeedMul(0.5f);
                    PlaySoundAtEntity("faint", "00_faint", "Player", 8, false);
                    fSpeedWhirl = 1.0f;
                    break;
                case 10:
                    SetPropActiveAndFade("cockroach_*", false, 1);
                    PlaySoundAtEntity("scare3", "react_sigh.snt", "Player", 0.8f, false);
                    SetPlayerMoveSpeedMul(0.75f);
                    FadeIn(5);
                    FadeImageTrailTo(0, 0.1f);
                    FadePlayerAspectMulTo(1, 0.05f);
                    FadeGlobalSoundSpeed(1, 5);
                    FadeGlobalSoundVolume(1, 5);
                    StopSound("rumbleoo", 5.0f);
                    PlayMusic("01_amb_darkness.ogg", true, 1.0f, 0, 0, true);
                    AddTimer("lookloop", 3, nameof(TimerRandomLook));
                    RemoveTimer("breath");
                    break;
            }

            if (GetLocalVarInt("VarSwirl") < 10) AddTimer("swirl", fSpeedWhirl, nameof(TimerSwirlHorror));
        }

        void TimerBreath(string asTimer)
        {
            PlaySoundAtEntity("breath", "react_breath_no3d", "AreaDoor_1", 1, false);

            AddTimer("breath", 3, nameof(TimerBreath));
        }
        //END CLOSING DOOR SCARE//

        /////////////////////
        //BEGIN BEGIN GHOST//
        // In aban study, steps is seen and book thrown.

        void CollideBeginGhost(string asParent, string asChild, int alState)
        {
            StopRandomLook(); //De-activate the spinning head
            PlaySoundAtEntity("SoundScratch_5", "scare_scratch", "AreaGhostWind", 2, false);
            PlaySoundAtEntity("whirly", "01_whirl.snt", "Player", 4, false);
            CreateParticleSystemAtEntity("PSGhostWind", "ps_dust_whirl_large.ps", "AreaGhostWind", false);
            AddTimer("ghost", 0.2f, nameof(TimerGhost));
            RemoveTimer("wall_scrape");
        }

        int iLoopGhost = 1; //If a step should loop, 1 as it is used for selecting what area to begin footsteps in

        void TimerGhost(string asTimer)
        {
            AddLocalVarInt("VarGhost", 1); //What step to play in the event
            float fSpeedGhost = 0.5f; //The default time between steps in an event

            switch (GetLocalVarInt("VarGhost"))
            {
                case 1:
                    FadeOut(20);
                    PlaySoundAtEntity("SoundScratch_2", "scare_scratch_intense", "AreaScratch_1", 4, false);
                    break;
                case 2:
                    GiveSanityDamage(10, false);
                    PlaySoundAtEntity("SoundFear2", "scare_male_terrified.snt", "AreaGhostStep_1", 0, false);
                    PlaySoundAtEntity("breath2", "react_breath.snt", "Player", 0.6f, false);
                    PlaySoundAtEntity("DustBoom1", "scare_wall_stomp.snt", "AreaDustBoom_1", 0.5f, false);
                    AddPropForce("ghostbook_1", 50, 100, 50, "world");
                    CreateParticleSystemAtEntity("PSDustBoom1", "ps_dust_impact_vert.ps", "AreaDustBoom_1", false);
                    StartScreenShake(0.01f, 0.1f, 0.5f, 0.9f);
                    break;
                case 3:
                    MovePlayerHeadPos(0, -1.3f, 0, 0.1f, 0.5f);
                    PlaySoundAtEntity("scare2", "react_scare.snt", "Player", 0.4f, false);
                    FadePlayerAspectMulTo(2, 0.02f);
                    FadeImageTrailTo(1.7f, 1.1f);
                    break;
                case 4:
                    PlaySoundAtEntity("DustBoom2", "scare_wall_stomp.snt", "AreaDustBoom_2", 0.25f, false);
                    PlaySoundAtEntity("SoundFear3x", "scare_male_terrified.snt", "AreaGhostStep_1", 1, false);
                    AddPropForce("ghostbook_2", 50, 100, 70, "world");
                    AddPropForce("ghostbook_3", 50, 100, 60, "world");
                    CreateParticleSystemAtEntity("PSDustBoom2", "ps_dust_impact_vert.ps", "AreaDustBoom_2", false);
                    StartScreenShake(0.02f, 0.1f, 0.5f, 0.9f);
                    PlaySoundAtEntity("SoundScratch_3", "scare_scratch", "AreaScratch_2", 2, false);
                    break;
                case 5:
                    MovePlayerHeadPos(0, 0, 0, 0.1f, 0.5f);
                    FadeIn(5);
                    PlaySoundAtEntity("breath2", "react_breath.snt", "Player", 0.2f, false);
                    FadeImageTrailTo(1.3f, 1.1f);
                    PlaySoundAtEntity("SoundGhostScream", "scare_ghost.snt", "AreaGhostStep_1", 0.0f, false);
                    break;
                case 6:
                    PlaySoundAtEntity("SoundFear4x", "scare_male_terrified5.snt", "AreaGhostStep_1", 1, false);
                    PlaySoundAtEntity("breath4", "react_breath.snt", "Player", 0.3f, false);
                    FadeImageTrailTo(1.6f, 1.1f);
                    fSpeedGhost = 1.5f;
                    break;
                case 7:
                    PlaySoundAtEntity("scare3", "react_scare.snt", "Player", 0.2f, false);
                    PlaySoundAtEntity("DustBoom4", "scare_wall_stomp.snt", "AreaDustBoom_4", 0, false);
                    CreateParticleSystemAtEntity("PSDustBoom4", "ps_dust_impact.ps", "AreaDustBoom_4", false);
                    StartScreenShake(0.04f, 0.1f, 0.5f, 0.9f);
                    AddPropForce("lantern", 20, 220, 40, "world");
                    StartPlayerLookAt("lantern", 2, 2, "");
                    FadeOut(15);
                    MovePlayerHeadPos(0, -1.3f, 0, 0.05f, 0.5f);
                    fSpeedGhost = 1.5f;
                    break;
                case 8:
                    PlaySoundAtEntity("DustBoom3", "scare_wall_stomp.snt", "AreaDustBoom_3", 0.1f, false);
                    CreateParticleSystemAtEntity("PSDustBoom3", "ps_dust_impact_vert.ps", "AreaDustBoom_3", false);
                    AddPropForce("ghostbook_4", 30, 120, 70, "world");
                    StartScreenShake(0.03f, 0.1f, 0.5f, 0.9f);
                    break;
                case 9:
                    StopPlayerLookAt();
                    StartScreenShake(0.02f, 0.1f, 2, 2);
                    AddTimer("wall_scrape", RandFloat(3.0f, 15.0f), nameof(TimerWallScrape));
                    PlaySoundAtEntity("breath3", "react_breath.snt", "Player", 0.5f, false);
                    for (int i = 1; i <= 5; i++) StopSound("SoundScratch_" + i, 4.0f);
                    StopSound("whirly", 4.0f);
                    FadePlayerAspectMulTo(1, 0.02f);
                    FadeImageTrailTo(0.0f, 1.1f);
                    StopMusic(8, 10);
                    fSpeedGhost = 1.5f;
                    break;
                case 10:
                    FadeIn(5);
                    MovePlayerHeadPos(0, 0, 0, 0.1f, 0.5f);
                    PlaySoundAtEntity("breath3", "react_breath.snt", "Player", 0.7f, false);
                    break;
            }

            if (GetLocalVarInt("VarGhost") < 10) AddTimer("ghost", fSpeedGhost, nameof(TimerGhost));
        }
        //END BEGIN GHOST//

        //////////////////////////
        //BEGIN RANDOM LOOK SPIN//
        // Player has a bit of a random head during the level

        bool bRoll = true;

        void TimerRandomLook(string asTimer)
        {
            int iLook = (int)RandFloat(1, 5);

            if (iLook > 4)
            {
                SetPlayerRunSpeedMul(0.5f);
                SetPlayerMoveSpeedMul(1);
                FadePlayerRollTo(0, 0.5f, 1);
                FadePlayerFOVMulTo(1, 1);
                FadeImageTrailTo(0, 1.5f);

                AddTimer("lookloop", RandFloat(6.0f, 8.0f), nameof(TimerRandomLook));
                return;
            }

            if (iLook == 1 || iLook == 3)
                PlaySoundAtEntity("sigh", "react_sigh.snt", "Player", 1.0f / 0.75f, false);

            FadePlayerFOVMulTo(RandFloat(0.8f, 1.2f), RandFloat(0.06f, 0.09f));

            SetPlayerMoveSpeedMul(RandFloat(0.75f, 0.95f));
            SetPlayerRunSpeedMul(RandFloat(0.3f, 0.5f));

            FadeImageTrailTo(RandFloat(0.4f, 0.75f), RandFloat(0.75f, 1.0f));

            bRoll = bRoll == false ? true : false;

            if (bRoll)
                FadePlayerRollTo(RandFloat(2, 8), RandFloat(0.075f, 0.35f), RandFloat(0.35f, 0.85f));
            else
                FadePlayerRollTo(RandFloat(-2, -8), RandFloat(0.05f, 0.25f), RandFloat(0.3f, 0.75f));

            AddTimer("lookloop", RandFloat(3.0f, 6.0f), nameof(TimerRandomLook));
        }

        void TimerRandomLook02(string asTimer)
        {
            int iLook = (int)RandFloat(1, 5);

            if (iLook > 4)
            {
                SetPlayerRunSpeedMul(0.5f);
                SetPlayerMoveSpeedMul(1);
                FadePlayerRollTo(0, 0.5f, 1);
                FadePlayerFOVMulTo(1, 1);
                FadeImageTrailTo(0, 1.5f);

                AddTimer("lookloop2", RandFloat(6.0f, 8.0f), nameof(TimerRandomLook02));
                return;
            }

            if (iLook == 1 || iLook == 3)
                PlaySoundAtEntity("sigh", "react_sigh.snt", "Player", 1.0f / 0.5f, false);

            FadePlayerFOVMulTo(RandFloat(0.8f, 1.2f), RandFloat(0.06f, 0.09f));

            SetPlayerMoveSpeedMul(RandFloat(0.85f, 1.00f));
            SetPlayerRunSpeedMul(RandFloat(0.5f, 0.7f));

            FadeImageTrailTo(RandFloat(0.1f, 0.4f), RandFloat(0.85f, 1.0f));

            bRoll = bRoll == false ? true : false;

            if (bRoll)
                FadePlayerRollTo(RandFloat(1, 4), RandFloat(0.075f, 0.15f), RandFloat(0.35f, 0.6f));
            else
                FadePlayerRollTo(RandFloat(-1, -4), RandFloat(0.05f, 0.1f), RandFloat(0.3f, 0.5f));

            AddTimer("lookloop2", RandFloat(3.0f, 6.0f), nameof(TimerRandomLook02));
        }

        void StopRandomLook()
        {
            RemoveTimer("lookloop");

            SetPlayerMoveSpeedMul(1);
            FadePlayerFOVMulTo(1, 0.5f);
            FadeImageTrailTo(0, 1.0f);
            FadePlayerRollTo(0, 0.5f, 2);
        }
        //END RANDOM LOOK SPING//

        ////////////////////////////
        //BEGIN PICK LANTERN & OIL//

        void PickLanterAndOil(string asEntity, string asType)
        {
            if (GetLocalVarInt("DoCreakPartOfOpenDoor") == 1)
                DoDoorOpening(1);
            else
                DoSimpleDoorOpening(); //No creak or music or swing open on door.

            if (asEntity == "lantern")
                SetPlayerLampOil(25.0f);

            // DEBUG
            AddDebugMessage("Picked " + asEntity, true);
        }

        void DoSimpleDoorOpening()
        {
            RemoveTimer("lookloop");
            AddTimer("lookloop2", 6, nameof(TimerRandomLook02));

            SetSwingDoorLocked("Door_3", false, false);

            SetEntityPlayerInteractCallback("Door_3", "", true);
        }

        void DoDoorOpening(int iDelay)
        {
            RemoveTimer("lookloop");
            AddTimer("lookloop2", 6, nameof(TimerRandomLook02));
            AddTimer("delayeffect", iDelay, nameof(TimerDelayEffect));

            SetSwingDoorLocked("Door_3", false, false);
            SetSwingDoorClosed("Door_3", false, false);
            SetSwingDoorDisableAutoClose("Door_3", true);

            PlaySoundAtEntity("grunt", "01_idle.snt", "AreaGrunt", 0.0f, false);

            SetEntityPlayerInteractCallback("Door_3", "", true);
        }

        void TimerDelayEffect(string asTimer)
        {
            PlayMusic("10_puzzle01.ogg", false, 0.7f, 2, 0, false);

            PlayGuiSound("unlock_door.snt", 0.5f);

            PlaySoundAtEntity("SoundDoorCreak", "01_door.snt", "Door_3", 0.0f, false);
            StartScreenShake(0.01f, 1, 0.5f, 1);

            AddTimer("Door_3", 0.01f, nameof(TimerSwingDoor));
        }
        //END PICK LANTERN & OIL//

        ////////////////////////
        //BEGIN COMPLETE TRAIL//

        void CollideCompleteTrail(string asParent, string asChild, int alState)
        {
            for (int i = 1; i <= 3; i++) SetPropActiveAndFade("cockroache_" + i, true, 0.5f);

            CompleteQuest("00Trail", "00Trail");

            PlayMusic("15_puzzle_hole.ogg", false, 1.0f, 0.5f, 5, false);

            RemoveTimer("lookloop2");

            StopRandomLook();

            StartPlayerLookAt("note_generic_1", 2.0f, 2.0f, "");

            AddTimer("stoplook", 3.0f, nameof(TimerStopLook));
        }

        void TimerStopLook(string asTimer)
        {
            StopPlayerLookAt();
        }
        //END COMPLETE TRAIL//

        ///////////////////////
        //BEGIN EFFECT TIMERS//

        void TimerWallScrape(string asTimer)
        {
            int iArea = (int)RandFloat(1, 6);

            PlaySoundAtEntity("wall_scrape" + iArea, "scare_wall_crawl_single.snt", "AreaWallScrape_" + iArea, 0.0f, false);

            AddTimer("wall_scrape", RandFloat(3.0f, 15.0f), nameof(TimerWallScrape));
        }

        void TimerBeginWind(string asTimer)
        {
            PlaySoundAtEntity("BeginWindSound", "general_wind_whirl.snt", "Player", 4.0f, false);
        }
        //END EFFECT TIMERS//

        ///////////////////
        //BEGIN LAST WIND//

        void CollideLastWind(string asParent, string asChild, int alState)
        {
            CreateParticleSystemAtEntity("PSLastWind", "ps_dust_push.ps", "AreaWindLast", false);
            CreateParticleSystemAtEntity("PSLastWind2", "ps_dust_paper_blow.ps", "AreaWindLast", false);
            PlaySoundAtEntity("LastWindSound", "general_wind_whirl.snt", "AreaWindLast", 3.0f, false);
        }
        //END LAST WIND//

        //////////////////////////
        //BEGIN OPEN SECRET DOOR//

        void PickNoteToSelf(string entity, string type)
        {
            PlayMusic("01_paper_self.ogg", false, 0.7f, 0, 10, false);
        }

        void TimerHint(string asTimer)
        {
            GiveSanityBoostSmall();
            GiveHint("sanity", "Hints", "SanityAdd", 0);
        }

        void CreateDust(string asConnectionName, string asMainEntity, string asConnectEntity, int alState)
        {
            if (GetLocalVarInt("DoSecretDoorEffects") == 1) return;

            CreateParticleSystemAtEntity("PSDoorDust", "ps_dust_falling_door.ps", "secret_door", false);

            AddTimer("sucess_music", 2, nameof(TimerMusic));

            AddTimer("Hint", 0.5f, nameof(TimerHint));

            StartScreenShake(0.006f, 0.5f, 2.0f, 2.0f);

            SetLocalVarInt("DoSecretDoorEffects", 1);
        }

        void TimerMusic(string asTimer)
        {
            PlayMusic("01_puzzle_passage.ogg", false, 0.7f, 0, 10, false);
        }
        //END OPEN SECRET DOOR//

        /////////////////////
        //BEGIN MISC THINGS//

        void CollideHintLean(string asParent, string asChild, int alState)
        {
            GiveHint("lean", "Hints", "LeanHint", 0);
        }

        void CollideTremble(string asParent, string asChild, int alState)
        {
            // Turn off rain for the level
            StopSound("amb_2", 0.0f); StopSound("amb_3", 0.0f); StopSound("amb_4", 0.0f); StopSound("amb_8", 0.0f);
            DestroyParticleSystem("ParticleSystem_41"); DestroyParticleSystem("ParticleSystem_40");

            // All below + Timer is for shake events when entering the second last room
            PlaySoundAtEntity("rumble3", "general_rock_rumble_no3d", "Player", 15.0f, false);
            PlaySoundAtEntity("breath", "react_breath.snt", "Player", 2.0f, false);
            MovePlayerHeadPos(0.5f, -0.5f, 0.5f, 0.1f, 0.01f);

            StartScreenShake(0.03f, 1.0f, 4.0f, 8.0f);

            for (int i = 1; i < 6; i++) CreateParticleSystemAtEntity("dust" + i, "ps_dust_falling_door", "AreaTrembleFall_" + i, false);

            AddPropForce("chandelier_simple_short_1", -75.0f, 0, 75.0f, "world");
            AddPropForce("chandelier_simple_short_4", 75.0f, 0, -75.0f, "world");

            AddTimer("stoprumb", 6.0f, nameof(TimerStopRumble));
            AddTimer("force1", 1.0f, nameof(TimerStopRumble));
            AddTimer("force", 2.0f, nameof(TimerStopRumble));
            AddTimer("force", 3.0f, nameof(TimerStopRumble));
            AddTimer("force", 4.0f, nameof(TimerStopRumble));
        }

        void TimerStopRumble(string asTimer)
        {
            if (asTimer == "force" || asTimer == "force1")
            {
                if (asTimer == "force1") FadeImageTrailTo(1.0f, 6.0f);

                // The original declares i with its own (uninitialized, 0) value: i = 0 == 150 ? -150 : 150, always 150.
                int i = 0;
                i = i == 150 ? -150 : 150;

                AddPropForce("chandelier_simple_short_1", i, 0, -i, "world");
                AddPropForce("chandelier_simple_short_4", -i, 0, i, "world");

                if (i == -150) PlaySoundAtEntity("chain", "general_chain_rattle_single", "chandelier_simple_short_1", 0.5f, false);
                else PlaySoundAtEntity("chain", "general_chain_rattle_single", "chandelier_simple_short_4", 0.5f, false);

                AddDebugMessage("I is: " + i, false);

                return;
            }
            PlaySoundAtEntity("breath2", "react_breath.snt", "Player", 1.0f, false);
            MovePlayerHeadPos(0, 0, 0, 0.2f, 0.01f);
            FadeImageTrailTo(0.0f, 12.0f);
            StopSound("rumble3", 10.0f);
        }
        //END MISC THINGS//

        //SEEN THE MAGIC OPENING DOOR
        void CollideAreaCollideSeenDoor(string asParent, string asChild, int alState)
        {
            SetLocalVarInt("DoCreakPartOfOpenDoor", 1);
        }

        //Locked door message
        void InteractLockedDoor(string asEntity)
        {
            SetMessage("Ch01Level03", "DoorLocked", 0);

            PlayGuiSound("locked_door.snt", 0.8f);

            AddTimer(asEntity, 1.0f, nameof(TimerDoorMessageOnAgain));
        }

        void TimerDoorMessageOnAgain(string asTimer)
        {
            SetEntityPlayerInteractCallback(asTimer, nameof(InteractLockedDoor), true);
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("ArchivesOld");

            //----COLLIDE CALLBACKS----//
            AddEntityCollideCallback("Player", "AreaTriggerDoor_1", nameof(CollideOpeningDustDoor), true, 1); //Door that swings open 1st corridor
            AddEntityCollideCallback("Player", "AreaBeginSwirl", nameof(CollideBeginSwirl), true, 1); //Swril and horror in room behind swing door
            AddEntityCollideCallback("Player", "AreaTriggerGhost", nameof(CollideBeginGhost), true, 1); //The steps and throwing book
            AddEntityCollideCallback("Player", "AreaTriggerTrail", nameof(CollideCompleteTrail), true, 1); //Complete the follow trail quest
            AddEntityCollideCallback("Player", "AreaTriggerLastWind", nameof(CollideLastWind), true, 1); //A last wind puff at end of hall
            AddEntityCollideCallback("Player", "AreaHintLean", nameof(CollideHintLean), true, 1); //Enable a hint to help finding the lever part
            AddEntityCollideCallback("Player", "AreaCollideTremble", nameof(CollideTremble), true, 1);
            AddEntityCollideCallback("Player", "AreaCollideSeenDoor", nameof(CollideAreaCollideSeenDoor), true, 1);

            //----ENTITY INIT----//
            CreateParticleSystemAtEntity("BeginWindPS", "ps_dust_push.ps", "AreaBeginWind", false);

            //----ITEM CALLBACKS----//
            SetSwingDoorLocked("shelf_secret_door_joint_1", true, false);
            SetEntityInteractionDisabled("shelf_secret_door_joint_1", true);

            //----CONNECT CALLBACKS----//
            // Secret hole in wall
            ConnectEntities("door_connection", //Name of connection
                "secret_lever", //Parent entity (Affects)
                "secret_door", //Child entity (Affected)
                false, //Invert the state sent
                1, //States used (0=both), checked before invertion.
                nameof(CreateDust)); //callback

            ConnectEntities("shelf_connection", //Name of connection
                "secret_lever", //Parent entity (Affects)
                "secret_shelf", //Child entity (Affected)
                false, //Invert the state sent
                1, //States used (0=both), checked before invertion.
                ""); //callback

            UnBlockHint("SanityAdd"); UnBlockHint("LanternNoItem"); UnBlockHint("LanternNoOil"); UnBlockHint("LeanHint");
            UnBlockHint("PushHint"); UnBlockHint("ThrowHint"); UnBlockHint("EntitySlide"); UnBlockHint("EntityLever");
            UnBlockHint("SanityHit");

            //----QUEST INIT----//

            SetNumberOfQuestsInMap(2);

            //----TIMER INIT----//
            AddTimer("lookloop", 0.1f, nameof(TimerRandomLook)); //Re-activate the spinning head
            AddTimer("wall_scrape", 0.1f, nameof(TimerWallScrape)); //Random scrapings in the walls
            AddTimer("BeginWindTimer", 0.75f, nameof(TimerBeginWind));

            //----DEBUG----//
            if (ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
            }
        }

        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            //----PRELOADING----//
            PreloadSound("scare_wind_reverse"); PreloadSound("joint_door_move_special"); PreloadSound("scare_tingeling"); PreloadSound("01_tiny1");
            PreloadSound("react_breath"); PreloadSound("general_wind_whirl"); PreloadSound("react_scare"); PreloadSound("scare_slam_door");
            PreloadSound("01_tiny2"); PreloadSound("01_tiny3"); PreloadSound("scare_whine_loop"); PreloadSound("scare_tingeling_rev2");
            PreloadSound("01_tiny4"); PreloadSound("scare_thump_flash"); PreloadSound("00_loop"); PreloadSound("react_sigh");
            PreloadSound("00_faint"); PreloadSound("react_breath_no3d"); PreloadSound("scare_scratch"); PreloadSound("01_whirl");
            PreloadSound("scare_scratch_intense"); PreloadSound("scare_male_terrified"); PreloadSound("scare_wall_stomp"); PreloadSound("scare_ghost");
            PreloadSound("01_door"); PreloadSound("scare_wall_crawl_single"); PreloadSound("general_rock_rumble_no3d");

            PreloadParticleSystem("ps_dust_paper_blow"); PreloadParticleSystem("ps_dust_push"); PreloadParticleSystem("ps_dust_ghost");
            PreloadParticleSystem("ps_dust_whirl_large"); PreloadParticleSystem("ps_dust_impact_vert"); PreloadParticleSystem("ps_dust_impact");
            PreloadParticleSystem("ps_dust_falling_door");

            ClearSavedMaps();

            //----AUDIO----//
            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        }
    }
}
