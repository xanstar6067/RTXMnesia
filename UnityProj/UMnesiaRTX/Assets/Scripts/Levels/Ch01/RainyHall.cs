using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/00_rainy_hall.hps: intro flashbacks, waking up, thunder, the narrow halls, the
    /// gallery and the gust door. Journal: docs/ScriptPort/ch01/00_rainy_hall.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/00_rainy_hall.map")]
    public class RainyHall : HplScript
    {
        static string F(float x) => x.ToString(CultureInfo.InvariantCulture);

        ////////////////////////
        // GENERAL
        ////////////////////////

        void PlayEffectVoice(string asEntryBase, int alStartIdx, int alEndIdx, string asCallback)
        {
            for (int i = alStartIdx; i <= alEndIdx; ++i)
            {
                string sEntry = asEntryBase;
                if (i < 10) sEntry += "0";
                sEntry += i;

                AddEffectVoice(sEntry, "", "Flashbacks", sEntry, false, "", 0, 0);
            }

            SetEffectVoiceOverCallback(asCallback);
        }

        ////////////////////////
        // INTRO FLASHBACKS
        ////////////////////////

        float gfIntroSequenceGlobalVolume = 0.3f;
        float gfIntroSequenceFadeOutTime = 3;

        void TimerStartGame(string asTimer)
        {
            PlayMusic("23_amb.ogg", true, 0.7f, 1, 0, false);

            AddTimer("IntroSequence", 2, nameof(TimerIntroSequence));
        }

        void ResumeIntroSequence()
        {
            AddTimer("IntroSequence", 0.1f, nameof(TimerIntroSequence));
        }

        void TimerIntroSequence(string asTimer)
        {
            int lState = GetLocalVarInt("IntroSequenceState");
            AddLocalVarInt("IntroSequenceState", 1);
            float fNextEventTime = 1.0f;
            bool bPause = false;

            ////////////////////////
            // 0: Fade In #1
            if (lState == 0)
            {
                FadeIn(3);
                SetPlayerActive(true);

                SetPlayerMoveSpeedMul(0.35f);
                SetPlayerRunSpeedMul(0);

                FadeImageTrailTo(2, 1);
                FadeSepiaColorTo(1, 1);

                FadePlayerRollTo(12, 0.3f, 0.7f);
                FadePlayerFOVMulTo(0.7f, 0.05f);

                FadeGlobalSoundVolume(gfIntroSequenceGlobalVolume, 3);

                PlayGuiSound("fb_sfx_00_daniel.ogg", 1.0f);
                PlayEffectVoice("CH01L00_DanielsMind01_", 1, 1, nameof(IntroSequenceVoiceOver));
                PlayEffectVoice("CH01L00_DanielsMind02_", 1, 1, nameof(IntroSequenceVoiceOver));
                PlayEffectVoice("CH01L00_DanielsMind03_", 1, 1, nameof(IntroSequenceVoiceOver));
                PlayEffectVoice("CH01L00_DanielsMind04_", 1, 1, nameof(IntroSequenceVoiceOver));

                fNextEventTime = 5.0f;
            }
            ////////////////////////
            // 1: Fade Out #1
            else if (lState == 1)
            {
                FadeOut(gfIntroSequenceFadeOutTime);
                FadeGlobalSoundVolume(0, gfIntroSequenceFadeOutTime);

                fNextEventTime = gfIntroSequenceFadeOutTime + 0.1f;
            }
            ////////////////////////
            // 2: Fade In #2
            else if (lState == 2)
            {
                FadeIn(3);
                TeleportPlayer("IntroStart_2");

                FadePlayerRollTo(-12, 0.3f, 0.8f);
                FadePlayerFOVMulTo(1.2f, 0.03f);

                FadeGlobalSoundVolume(gfIntroSequenceGlobalVolume, 3);

                fNextEventTime = 5.0f;
            }
            ////////////////////////
            // 3: Fade Out #2
            else if (lState == 3)
            {
                FadeOut(gfIntroSequenceFadeOutTime);
                FadeGlobalSoundVolume(0, gfIntroSequenceFadeOutTime);

                fNextEventTime = gfIntroSequenceFadeOutTime + 0.1f;
            }
            ////////////////////////
            // 4: Fade In #3
            else if (lState == 4)
            {
                FadeIn(3);
                TeleportPlayer("IntroStart_3");

                FadePlayerRollTo(12, 0.4f, 0.5f);
                FadePlayerFOVMulTo(0.5f, 0.05f);

                FadeGlobalSoundVolume(gfIntroSequenceGlobalVolume, 3);

                fNextEventTime = 5.0f;
            }
            ////////////////////////
            // 5: Fade Out #3
            else if (lState == 5)
            {
                FadeOut(gfIntroSequenceFadeOutTime);
                FadeGlobalSoundVolume(0, gfIntroSequenceFadeOutTime);

                fNextEventTime = gfIntroSequenceFadeOutTime + 0.1f;
            }
            ////////////////////////
            // 6: Fade In #4
            else if (lState == 6)
            {
                FadeIn(3);
                TeleportPlayer("IntroStart_4");

                FadePlayerRollTo(-12, 0.4f, 0.8f);
                FadePlayerFOVMulTo(1.3f, 0.1f);

                FadeGlobalSoundVolume(gfIntroSequenceGlobalVolume, 3);

                fNextEventTime = 6.0f;
            }
            ////////////////////////
            // 7: Fade Out #4
            else if (lState == 7)
            {
                FadeOut(gfIntroSequenceFadeOutTime);
                FadeGlobalSoundVolume(0, gfIntroSequenceFadeOutTime);

                AddTimer("WakeUpPlayer", gfIntroSequenceFadeOutTime + 4, nameof(TimerWakeUpPlayer));
                AddTimer("activefix", gfIntroSequenceFadeOutTime, nameof(TimerActiveFix));
                bPause = true;
                return;
            }

            AddDebugMessage("Event:" + lState + " Time:" + F(fNextEventTime), false);

            if (bPause == false)
                AddTimer("IntroSequence", fNextEventTime, nameof(TimerIntroSequence));
        }

        void TimerActiveFix(string asTimer)
        {
            SetPlayerActive(false);
        }

        void IntroSequenceVoiceOver()
        {
            ResumeIntroSequence();
        }

        void TimerWakeUpPlayer(string asTimer)
        {
            TeleportPlayer("PlayerStartArea_1");

            StopMusic(3, 0);

            PlayGuiSound("player_bodyfall5.ogg", 1);

            FadeImageTrailTo(0, 1);
            FadeSepiaColorTo(0, 1);

            FadePlayerRollTo(0, 1, 3);
            FadePlayerFOVMulTo(1, 1);

            FadeGlobalSoundVolume(1, 3);
            FadeGlobalSoundSpeed(1, 3);

            SetInventoryDisabled(false);

            SetEntityActive("narrowpassout", true);
            SetEntityActive("gallerydoor", true);
            SetEntityActive("areanarrowhalls", true);
            SetEntityActive("areaendthunder", true);
            SetEntityActive("areabeginthunder", true);
            SetEntityActive("areagustdoor", true);
            SetEntityActive("AreaDust3", true);
            SetEntityActive("AreaFaint", true);

            SetPlayerJumpDisabled(true);
            SetPlayerCrouchDisabled(true);
            SetPlayerRunSpeedMul(0);
            FadePlayerRollTo(75, 10, 100);                // roll, speed mul, max speed (degrees)
            MovePlayerHeadPos(-0.2f, -1.3f, 0, 10, 0.5f); // x, y, z, speed, slow down distance
            StartPlayerLookAt("Arealook1", 10, 100, "");  // area, speed mul, max speed, callback

            AddTimer("blackout", 9.0f, nameof(TimerBlackOut));
            AddTimer("forhelbeep", 5.2f, nameof(TimerBeginSoundsIntro));
        }

        ////////////////////////
        // WAKE UP
        ////////////////////////

        void TimerBeginSoundsIntro(string asTimer)
        {
            PlaySoundAtEntity("Rain", "general_rain_m.snt", "AreaThunder", 4, true);
            PlaySoundAtEntity("Eerie", "ambience_wind_eerie_no3d.snt", "AreaThunder", 4, true);
            PlaySoundAtEntity("Hollow", "ambience_hollow_tinker.snt", "AreaThunder", 4, true);
            PlaySoundAtEntity("Flow", "12_epoxy_flow.snt", "AreaFlowWater", 12, true);
        }

        // Player wakes up on the floor and slowly gets up; when up the first quest is given.
        void TimerBlackOut(string asTimer)
        {
            AddLocalVarInt("BlackoutStep", 1); // what step to play in the event
            float fEventSpeed = 0.5f;          // the default time between steps in an event

            switch (GetLocalVarInt("BlackoutStep"))
            {
                case 1:
                    StartPlayerLookAt("Arealook2", 0.1f, 0.1f, "");
                    FadeIn(4);
                    FadeImageTrailTo(2, 1);
                    AddTimer("rose", 0.5f, nameof(TimerRose));
                    SetPlayerActive(true);
                    ShowPlayerCrossHairIcons(true);
                    SetPlayerMoveSpeedMul(0.05f);
                    SetPlayerLookSpeedMul(0.05f);
                    fEventSpeed = 3.0f;
                    break;
                case 2:
                    FadePlayerRollTo(85, 1, 1);
                    break;
                case 3:
                    StartPlayerLookAt("Arealook3", 0.1f, 0.1f, "");
                    break;
                case 4:
                    FadeImageTrailTo(0, 1);
                    FadePlayerRollTo(65, 1, 1);
                    AddTimer("thunder", 1, nameof(TimerThunder));
                    break;
                case 5:
                    PlaySoundAtEntity("sigh", "react_sigh.snt", "Player", 1.0f / 2, false);
                    FadeOut(2);
                    fEventSpeed = 1.5f;
                    break;
                case 6:
                    FadePlayerRollTo(85, 1, 4);
                    StartPlayerLookAt("Arealook1", 0.1f, 0.1f, "");
                    break;
                case 7:
                    FadeImageTrailTo(1.8f, 1.5f);
                    FadePlayerFOVMulTo(1.25f, 0.01f);
                    break;
                case 8:
                    FadePlayerRollTo(45, 1, 2);
                    FadeIn(2);
                    fEventSpeed = 1.5f;
                    break;
                case 9:
                    StartPlayerLookAt("Arealook2", 0.1f, 0.1f, "");
                    break;
                case 10:
                    FadePlayerRollTo(15, 1, 2);
                    FadePlayerFOVMulTo(0.75f, 0.01f);
                    break;
                case 11:
                    PlaySoundAtEntity("sigh", "react_sigh.snt", "Player", 1.0f / 1.5f, false);
                    FadeOut(1);
                    StartPlayerLookAt("Arealook3", 1, 1, "");
                    FadePlayerRollTo(50, 1, 2);
                    fEventSpeed = 2.0f;
                    break;
                case 12:
                    SetPlayerMoveSpeedMul(0.1f);
                    SetPlayerLookSpeedMul(0.1f);
                    StartPlayerLookAt("Arealook1", 1, 1, "");
                    FadePlayerFOVMulTo(1.1f, 0.01f);
                    FadeImageTrailTo(0, 1.5f);
                    fEventSpeed = 1.5f;
                    break;
                case 13:
                    SetPlayerMoveSpeedMul(0.2f);
                    SetPlayerLookSpeedMul(0.2f);
                    FadePlayerRollTo(-15, 1, 2);
                    FadeIn(1);
                    StartPlayerLookAt("Arealook4", 2, 2, "");
                    fEventSpeed = 2.0f;
                    break;
                case 14:
                    SetPlayerMoveSpeedMul(0.3f);
                    SetPlayerLookSpeedMul(0.4f);
                    FadePlayerRollTo(-30, 10, 60);
                    MovePlayerHeadPos(0, 0, 0, 1, 0.5f);
                    StartPlayerLookAt("Arealook3", 1, 1, "");
                    FadePlayerFOVMulTo(0.9f, 0.01f);
                    FadeImageTrailTo(1.5f, 2);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    break;
                case 15:
                    SetPlayerMoveSpeedMul(0.4f);
                    SetPlayerLookSpeedMul(0.6f);
                    FadePlayerRollTo(10, 10, 20);
                    MovePlayerHeadPos(0, -0.5f, 0, 1, 0.5f);
                    break;
                case 16:
                    SetPlayerMoveSpeedMul(0.5f);
                    SetPlayerLookSpeedMul(0.8f);
                    FadePlayerRollTo(0, 10, 60);
                    MovePlayerHeadPos(0, 0, 0, 1, 0.5f);
                    StartPlayerLookAt("Arealook4", 2, 2, "");
                    FadePlayerFOVMulTo(1, 0.01f);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    fEventSpeed = 2.0f;
                    break;
                case 17:
                    SetPlayerJumpDisabled(false);
                    SetPlayerCrouchDisabled(false);
                    SetPlayerMoveSpeedMul(0.6f);
                    SetPlayerLookSpeedMul(1.0f);
                    FadeImageTrailTo(0, 0.2f);
                    StopPlayerLookAt();
                    PlaySoundAtEntity("sigh", "react_sigh.snt", "Player", 1.0f / 1, false);
                    AddTimer("lookloop", RandFloat(2.0f, 6.0f), nameof(TimerRandomLook)); // activate the spinning head
                    fEventSpeed = 1.0f;
                    break;
                case 18:
                    SetEntityActive("AreaQuest", true);
                    break;
            }

            if (GetLocalVarInt("BlackoutStep") < 19) AddTimer("blackout", fEventSpeed, nameof(TimerBlackOut));
        }

        void TimerRose(string asTimer)
        {
            CreateParticleSystemAtEntity("rose", "ps_rose_petals.ps", "AreaRose", false);
        }

        ////////////////////////////////////
        // BEGIN THUNDER
        ////////////////////////////////////

        // Random lightning and thunder at the start of the level.
        void TimerThunder(string asTimer)
        {
            AddLocalVarInt("ThunderStep", 1);             // what step to play in the event
            float fEventSpeed = RandFloat(0.05f, 0.15f);  // the default time between steps in an event

            switch (GetLocalVarInt("ThunderStep"))
            {
                case 1: ThunderLights(2, 0.05f); break;
                case 2: ThunderLights(1, 0.05f); break;
                case 3: ThunderLights(3, 0.05f); break;
                case 4: ThunderLights(1, 0.05f); break;
                case 5: ThunderLights(2, 0.05f); break;
                case 6: ThunderLights(3, 0.05f); break;
                case 7:
                    ThunderLights(1, 0.3f);
                    PlaySoundAtEntity("Thunder", "general_thunder.snt", "AreaThunder", 0.0f, false);
                    break;
            }

            if (GetLocalVarInt("ThunderStep") < 8) AddTimer("thunder", fEventSpeed, nameof(TimerThunder));
            else
            {
                SetLocalVarInt("ThunderStep", 0);

                AddTimer("thunder", RandFloat(10.0f, 30.0f), nameof(TimerThunder));
            }
        }

        void ThunderLights(int alIntensity, float afFade)
        {
            // Skip parts of a flash every now and then, but not the first strong light and not the last "back to
            // normal" one. (As written the condition is always true, and RandFloat(1, 3) is 1 almost never.)
            if (RandFloat(1, 3) == 1 && (GetLocalVarInt("ThunderStep") != 3 || GetLocalVarInt("ThunderStep") != 7)) return;

            float fF = 0.2f;

            switch (alIntensity)
            {
                case 1:
                    for (int i = 0; i <= 5; i++) FadeLightTo("spotthunder_" + i, 0.52f, 0.55f, 0.6f, 0.45f, -1, afFade - 0.04f);
                    for (int i = 0; i <= 7; i++) FadeLightTo("pointthunder_" + i, 0.32f, 0.35f, 0.4f, 0.2f, -1, afFade - 0.025f);
                    for (int i = 0; i <= 3; i++) FadeLightTo("ambthunder_" + i, 0.2f, 0.25f, 0.35f, -1, -1, afFade);
                    break;
                case 2:
                    for (int i = 0; i <= 5; i++) FadeLightTo("spotthunder_" + i, 0.82f + fF, 0.85f + fF, 0.9f + fF, 0.9f + fF, -1, afFade - 0.04f);
                    for (int i = 0; i <= 7; i++) FadeLightTo("pointthunder_" + i, 0.72f + fF, 0.75f + fF, 0.8f + fF, 0.4f + fF, -1, afFade - 0.025f);
                    for (int i = 0; i <= 3; i++) FadeLightTo("ambthunder_" + i, 0.25f + fF, 0.3f + fF, 0.4f + fF, -1, -1, afFade);
                    break;
                case 3:
                    for (int i = 0; i <= 5; i++) FadeLightTo("spotthunder_" + i, 0.92f + fF, 0.95f + fF, 1 + fF, 1 + fF, -1, afFade - 0.04f);
                    for (int i = 0; i <= 7; i++) FadeLightTo("pointthunder_" + i, 0.82f + fF, 0.85f + fF, 0.9f + fF, 0.5f + fF, -1, afFade - 0.025f);
                    for (int i = 0; i <= 3; i++) FadeLightTo("ambthunder_" + i, 0.3f + fF, 0.35f + fF, 0.45f + fF, -1, -1, afFade);
                    break;
            }
        }

        // End the thunder loop when in hall 3.
        void CollideAreaThunder(string asParent, string asChild, int alState)
        {
            if (asChild == "areabeginthunder")
            {
                AddTimer("thunder", RandFloat(1, 5), nameof(TimerThunder));

                AddEntityCollideCallback("Player", "areaendthunder", nameof(CollideAreaThunder), true, 1);
            }
            else
            {
                RemoveTimer("thunder");

                ThunderLights(1, 0.3f);

                AddEntityCollideCallback("Player", "areabeginthunder", nameof(CollideAreaThunder), true, 1);
            }
        }

        //////////////////////////////////////
        // BEGIN NARROW HALLS
        //////////////////////////////////////

        // Begin events in the narrow hallway.
        void CollideNarrowHalls(string asParent, string asChild, int alState)
        {
            if (alState == 1)
            {
                PlaySoundAtEntity("creak", "00_creak.snt", "Player", 3, false);

                for (int i = 1; i <= 9; i++) CreateParticleSystemAtEntity("AreaHallPS_" + i, "ps_dust_falling_narrow", "AreaHallPS_" + i, false);

                FadePlayerAspectMulTo(1.7f, 0.03f);

                FadeImageTrailTo(1.5f, 1.25f);

                AddTimer("hallway", RandFloat(0.1f, 1.5f), nameof(HallwayEvents));
                RemoveTimer("lookloop");
                StopRandomLook();

                if (GetLocalVarInt("HallwaySteps") == 0)
                    AddTimer("steps", 0.5f, nameof(HallwaySteps));
            }
            else
            {
                StopSound("creak", 3);

                for (int i = 1; i <= 9; i++) DestroyParticleSystem("AreaHallPS_" + i);

                SetPlayerMoveSpeedMul(1);

                FadePlayerAspectMulTo(1, 0.3f);

                FadeImageTrailTo(0.0f, 1.5f);

                AddTimer("lookloop", RandFloat(2.0f, 6.0f), nameof(TimerRandomLook));
                RemoveTimer("hallway");

                SetLocalVarInt("HallwayStep", 0);
                SetLocalVarInt("InitUniqueRandom", 0);
            }
        }

        // Footsteps that run away when entering the hallway.
        float fStep = 0.0f;
        float fLoop = 0.4f;

        void HallwaySteps(string asTimer)
        {
            AddLocalVarInt("HallwaySteps", 1);

            if (GetLocalVarInt("HallwaySteps") <= 5)
            {
                PlaySoundAtEntity("step", "scare_walk_hallway.snt", "Arealookhall3", fStep += 0.03f, false);
                AddTimer("steps", fLoop -= 0.04f, nameof(HallwaySteps));
            }
            else if (GetLocalVarInt("HallwaySteps") >= 6 && GetLocalVarInt("HallwaySteps") <= 10)
            {
                PlaySoundAtEntity("step", "scare_walk_hallway.snt", "Arealookhall3", fStep += 0.01f, false);
                AddTimer("steps", fLoop += 0.1f, nameof(HallwaySteps));
            }

            AddDebugMessage("Step!" + F(fStep), false);
        }

        // Random events before passing out, swinging chandeliers.
        bool bSwing = true;

        void HallwayEvents(string asTimer)
        {
            SetLocalVarInt("HallwayStep", (int)RandFloat(1, 4)); // what step to play in the event

            float fEventSpeed = 2.0f; // the default time between steps in an event

            bSwing = bSwing == false ? true : false;

            if (bSwing)
            {
                for (int i = 0; i <= 110; i++)
                {
                    AddBodyForce("chand1_Body_3", i * 0.04f, 0, i * 0.04f, "World");
                    AddBodyForce("chand2_Body_3", i * 0.04f, 0, i * 0.04f, "World");
                    AddBodyForce("chandelier_simple_3_Body_3", i * 0.04f, 0, i * 0.04f, "World");
                }
            }
            else
            {
                for (int i = 0; i <= 110; i++)
                {
                    AddBodyForce("chand1_Body_3", -i * 0.04f, 0, -i * 0.04f, "World");
                    AddBodyForce("chand2_Body_3", -i * 0.04f, 0, -i * 0.04f, "World");
                    AddBodyForce("chandelier_simple_3_Body_3", -i * 0.04f, 0, -i * 0.04f, "World");
                }
            }

            switch (GetLocalVarInt("HallwayStep"))
            {
                case 1:
                    PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.3f, false);
                    SetPlayerMoveSpeedMul(0.85f);
                    StartScreenShake(0.002f, 1, 0.5f, 0.5f);
                    break;
                case 2:
                    PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.4f, false);
                    SetPlayerMoveSpeedMul(0.8f);
                    StartScreenShake(0.004f, 1, 0.5f, 0.5f);
                    break;
                case 3:
                    PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.7f, false);
                    SetPlayerMoveSpeedMul(0.75f);
                    StartScreenShake(0.005f, 1, 0.5f, 0.5f);
                    break;
                case 4:
                    PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.5f, false);
                    SetPlayerMoveSpeedMul(0.7f);
                    StartScreenShake(0.006f, 1, 0.5f, 0.5f);
                    break;
            }

            AddTimer("hallway", fEventSpeed, nameof(HallwayEvents));
        }

        // In the middle of the passage a super scary event occurs and the player passes out.
        void CollideAreaNarrowPassOut(string asParent, string asChild, int alState)
        {
            SetEntityActive("areanarrowhalls", false); // turn off the area that triggers the hallway spooks

            StopRandomLook(); // de-activate the spinning head

            SetPlayerJumpDisabled(true);
            SetPlayerCrouchDisabled(true);

            PlaySoundAtEntity("scare", "react_scare.snt", "Player", 1.0f / 1, false);
            PlaySoundAtEntity("cuts", "00_cuts_3d.snt", "Arealookhall3_1", 1.0f / 4, false);
            StopSound("creak", 3);

            StartScreenShake(0.01f, 0.45f, 0.1f, 0.75f);

            FadeImageTrailTo(1.7f, 1.0f);
            FadePlayerFOVMulTo(1.5f, 0.5f);

            AddTimer("narrowpass", 3, nameof(NarrowPassOutEvents));

            // Turn off hallway events.
            FadePlayerAspectMulTo(1, 0.1f);
            RemoveTimer("hallway");
        }

        void NarrowPassOutEvents(string asTimer)
        {
            AddLocalVarInt("HallPassOutStep", 1); // what step to play in the event
            float fEventSpeed = 0.5f;             // the default time between steps in an event

            switch (GetLocalVarInt("HallPassOutStep"))
            {
                case 1:
                    PlayMusic("00_event_hallway", false, 1, 1, 10, false);
                    PlaySoundAtEntity("scare", "react_scare.snt", "Player", 0, false);
                    StartPlayerLookAt("Arealookhall1", 0.5f, 0.5f, "");
                    SetPlayerMoveSpeedMul(0.7f);
                    SetPlayerLookSpeedMul(0.7f);
                    fEventSpeed = 1.0f;
                    break;
                case 2:
                    PlaySoundAtEntity("loop", "00_loop.snt", "Player", 1.0f / 0.05f, false);
                    SetPlayerMoveSpeedMul(0.6f);
                    SetPlayerLookSpeedMul(0.6f);
                    SetPlayerCrouching(false);
                    MovePlayerHeadPos(0, -1.3f, 0, 0.75f, 0.5f);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    break;
                case 3:
                    PlaySoundAtEntity("scare", "react_scare.snt", "Player", 1.0f / 0.5f, false);
                    SetPlayerMoveSpeedMul(0.5f);
                    SetPlayerLookSpeedMul(0.5f);
                    FadePlayerRollTo(30, 2, 10);
                    break;
                case 4:
                    FadeImageTrailTo(0.0f, 0.5f);
                    SetPlayerMoveSpeedMul(0.8f);
                    SetPlayerLookSpeedMul(0.8f);
                    FadePlayerFOVMulTo(1, 1);
                    MovePlayerHeadPos(0, 0, 0, 2, 0.5f);
                    fEventSpeed = 1.0f;
                    break;
                case 5:
                    SetPlayerMoveSpeedMul(0.4f);
                    SetPlayerLookSpeedMul(0.4f);
                    StartPlayerLookAt("Arealookhall2", 1, 1, "");
                    MovePlayerHeadPos(0, -1.3f, 0, 5, 0.25f);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.75f, false);
                    fEventSpeed = 1.0f;
                    break;
                case 6:
                    PlaySoundAtEntity("scare", "react_scare.snt", "Player", 1.0f / 1, false);
                    SetPlayerMoveSpeedMul(0.3f);
                    SetPlayerLookSpeedMul(0.3f);
                    FadePlayerRollTo(-30, 10, 20);
                    break;
                case 7:
                    SetPlayerMoveSpeedMul(0.6f);
                    SetPlayerLookSpeedMul(0.6f);
                    StartPlayerLookAt("Arealookhall1", 2, 2, "");
                    FadePlayerRollTo(30, 20, 40);
                    MovePlayerHeadPos(-1, -1.3f, 0, 1, 0.25f);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    break;
                case 8:
                    SetPlayerMoveSpeedMul(0.4f);
                    SetPlayerLookSpeedMul(0.4f);
                    FadeOut(3);
                    FadePlayerRollTo(75, 15, 30);
                    StopPlayerLookAt();
                    break;
                case 9:
                    for (int i = 1; i <= 9; i++) DestroyParticleSystem("AreaHallPS_" + i);
                    GiveSanityDamage(10, false);
                    PlaySoundAtEntity("faint", "00_faint.snt", "Player", 1.0f / 1, false);
                    SetPlayerMoveSpeedMul(0.2f);
                    SetPlayerLookSpeedMul(0.2f);
                    AddTimer("narrowpass2", 0.1f, nameof(NarrowPassOutEvents02));
                    SetLocalVarInt("HallPassOutStep", 0);
                    return;
            }

            if (GetLocalVarInt("HallPassOutStep") < 10) AddTimer("narrowpass", fEventSpeed, nameof(NarrowPassOutEvents));
        }

        // The player wakes up again and can continue onwards.
        void NarrowPassOutEvents02(string asTimer)
        {
            AddLocalVarInt("HallPassOutStep", 1); // what step to play in the event
            float fEventSpeed = 0.5f;             // the default time between steps in an event

            switch (GetLocalVarInt("HallPassOutStep"))
            {
                case 1:
                    FadeImageTrailTo(1.8f, 2.0f);
                    StopSound("loop", 3);
                    FadeIn(1);
                    FadePlayerRollTo(30, 2, 10);
                    break;
                case 2:
                    StartPlayerLookAt("Arealookhall1", 0.1f, 0.1f, "");
                    MovePlayerHeadPos(0, -1.0f, 0, 0.5f, 0.5f);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    break;
                case 3:
                    SetPlayerLookSpeedMul(0.3f);
                    SetPlayerMoveSpeedMul(0.3f);
                    break;
                case 4:
                    SetPlayerLookSpeedMul(0.4f);
                    SetPlayerMoveSpeedMul(0.4f);
                    MovePlayerHeadPos(0, 0, 0, 2, 0.5f);
                    break;
                case 5:
                    SetPlayerLookSpeedMul(0.5f);
                    SetPlayerMoveSpeedMul(0.5f);
                    MovePlayerHeadPos(0, -1.3f, 0, 3, 0.5f);
                    PlaySoundAtEntity("bodyfall", "player_bodyfall.snt", "Player", 1.0f / 1, false);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    StartPlayerLookAt("Arealookhall3", 1.8f, 1.8f, "");
                    FadePlayerRollTo(-30, 3, 10);
                    break;
                case 6:
                    SetPlayerLookSpeedMul(0.6f);
                    SetPlayerMoveSpeedMul(0.7f);
                    FadePlayerRollTo(0, 14, 35);
                    break;
                case 7:
                    SetPlayerLookSpeedMul(0.7f);
                    SetPlayerMoveSpeedMul(0.7f);
                    StartPlayerLookAt("Arealookhall1", 5.1f, 5.1f, "");
                    break;
                case 8:
                    SetPlayerLookSpeedMul(0.8f);
                    SetPlayerMoveSpeedMul(0.8f);
                    MovePlayerHeadPos(0, 0, 0, 4, 0.5f);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    StartPlayerLookAt("Arealookhall3", 5.1f, 5.1f, "");
                    break;
                case 9:
                    SetPlayerLookSpeedMul(0.9f);
                    SetPlayerMoveSpeedMul(0.9f);
                    FadeImageTrailTo(0, 0.2f);
                    StopPlayerLookAt();
                    break;
                case 10:
                    SetPlayerLookSpeedMul(1.0f);
                    SetPlayerMoveSpeedMul(1.0f);
                    PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 1, false);
                    fEventSpeed = 2.0f;
                    break;
                case 11:
                    AddTimer("lookloop", RandFloat(3.0f, 6.0f), nameof(TimerRandomLook)); // re-activate the spinning head
                    PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.75f, false);
                    fEventSpeed = 2.0f;
                    break;
                case 12:
                    SetPlayerJumpDisabled(false);
                    SetPlayerCrouchDisabled(false);
                    PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.5f, false);
                    fEventSpeed = 2.0f;
                    break;
            }
            if (GetLocalVarInt("HallPassOutStep") < 13) AddTimer("narrowpass2", fEventSpeed, nameof(NarrowPassOutEvents02));
        }

        void TimerPlayerInactive(string asTimer)
        {
            SetPlayerActive(false);
        }

        ///////////////////////////////////
        // BEGIN GALLERY DOOR SWING OPEN
        ///////////////////////////////////

        // When in hall 4 the gallery door swings open slowly.
        void CollideAreaGalleryDoor(string asParent, string asChild, int alState)
        {
            SetSwingDoorClosed("door_gallery", false, false);
            SetSwingDoorDisableAutoClose("door_gallery", true);

            PlaySoundAtEntity("creaking_door", "joint_door_move_special.snt", "door_gallery", 1.0f / 0.7f, false);

            AddTimer("door_gallery", 0.01f, nameof(TimerSwingDoor));

            AddDebugMessage("Boho, the gallery door creaks open.", false);

            AddTimer("stopcreak", 2.0f, nameof(TimerStopCreak));
        }

        void TimerSwingDoor(string asTimer)
        {
            if (GetLocalVarInt("SwingDoor") == 10)
            {
                SetLocalVarInt("SwingDoor", 0);
                return;
            }

            if (asTimer == "door_gallery") AddPropForce(asTimer, 70.0f, 0, 0, "World");
            else AddPropForce(asTimer, -95.0f, 0, 0, "World");

            AddLocalVarInt("SwingDoor", 1);

            AddTimer(asTimer, 0.03f, nameof(TimerSwingDoor));

            AddDebugMessage("Swing: " + GetLocalVarInt("SwingDoor"), false);
        }

        void TimerStopCreak(string asTimer)
        {
            if (asTimer == "scare2")
            {
                PlaySoundAtEntity("scare", "react_scare.snt", "Player", 1.0f / 1, false);
                return;
            }
            if (asTimer == "scare")
            {
                PlaySoundAtEntity("scare", "react_scare.snt", "Player", 0, false);
                PlaySoundAtEntity("loop", "00_loop.snt", "Player", 1.0f / 0.1f, false);
                return;
            }
            if (asTimer == "breath1")
            {
                PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 1, false);
                return;
            }
            if (asTimer == "breath2")
            {
                PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.9f, false);
                return;
            }
            if (asTimer == "breath3")
            {
                PlaySoundAtEntity("breath", "react_breath.snt", "Player", 1.0f / 0.8f, false);
                AddTimer("lookloop", RandFloat(2.0f, 3.0f), nameof(TimerRandomLook)); // re-activate the spinning head
                FadePlayerFOVMulTo(1.0f, 0.1f);
                StopSound("loop", 1);
                StartScreenShake(0.01f, 0.25f, 0.01f, 0.5f);
                return;
            }
            if (asTimer == "lightsout")
            {
                SetLampLit("chandelier_nice_1", false, true);
                SetLampLit("candlestick_floor_2", false, true);
                FadeLightTo("PointLight_25", 0, 0, 0, 0, 0, 1);
                FadePlayerFOVMulTo(1.5f, 0.25f);
                return;
            }

            if (asTimer == "sanity")
            {
                GiveSanityDamage(10, true);
                return;
            }

            StopSound("creaking_door", 0.4f);
        }

        // In the gallery the lights go out and the door slams shut; leaving it, the door slams shut and locks.
        void CollideAreaGallery(string asParent, string asChild, int alState)
        {
            if (asChild == "gallery_2")
            {
                return;
            }

            UnBlockHint("DarknessDecrease");

            StopRandomLook(); // de-activate the spinning head

            PlayMusic("00_event_gallery", false, 1, 1, 10, false);
            PlaySoundAtEntity("windgal", "general_wind_whirl.snt", "gallery_1", 0.0f, false);
            CreateParticleSystemAtEntity("psgal", "ps_dust_whirl.ps", "gallery_1", false);

            StartScreenShake(0.01f, 0.25f, 0.01f, 0.5f);

            AddEntityCollideCallback("Player", "gallery_2", nameof(CollideAreaGallery), true, 1);

            AddTimer("sanity", 0.7f, nameof(TimerStopCreak));
            AddTimer("lightsout", 0.8f, nameof(TimerStopCreak));
            AddTimer("scare", 1.2f, nameof(TimerStopCreak));
            AddTimer("breath1", 2.0f, nameof(TimerStopCreak));
            AddTimer("breath2", 5.0f, nameof(TimerStopCreak));
            AddTimer("breath3", 8.0f, nameof(TimerStopCreak));
        }

        ////////////////////////
        // GUST DOOR OPEN
        ////////////////////////

        // When entering hall 3 the door to the couch room is opened by a gust of wind.
        void CollideAreaGustDoor(string asParent, string asChild, int alState)
        {
            StartScreenShake(0.003f, 0.25f, 1, 1);

            PlaySoundAtEntity("gust_door", "general_wind_whirl.snt", "door_gust", 1.0f / 3, false);

            CreateParticleSystemAtEntity("gust", "ps_dust_push", "AreaDust_4", false);

            SetSwingDoorClosed("door_gust", false, false);
            SetSwingDoorDisableAutoClose("door_gust", true);

            AddTimer("gusttimer", 0.4f, nameof(TimerGustDoor));

            AddDebugMessage("Uhm, the door gust opened?", false);
        }

        void TimerGustDoor(string asTimer)
        {
            PlaySoundAtEntity("scare", "react_scare.snt", "Player", 0, false);
            PlaySoundAtEntity("creaking_door", "joint_door_move_special.snt", "door_gust", 1.0f / 1.8f, false);

            GiveSanityDamage(5, true);

            AddTimer("breath1", 2.0f, nameof(TimerStopCreak));
            AddTimer("breath2", 5.0f, nameof(TimerStopCreak));
            AddTimer("breath3", 8.0f, nameof(TimerStopCreak));

            AddTimer("door_gust", 0.3f, nameof(TimerSwingDoor));

            StopSound("creaking_door", 0.4f);
        }

        /////////////////////////////////
        // BEGIN GUST FLOWS
        /////////////////////////////////

        // Some gusts appear in the early parts of the level and one at the very end.
        void CollideAreaGust(string asParent, string asChild, int alState)
        {
            if (asChild == "AreaDust1")
            {
                int makeyourmagicpick = (int)RandFloat(2, 3);
                if (makeyourmagicpick == 2) AddEntityCollideCallback("Player", "AreaDust2", nameof(CollideAreaGust), true, 1);
                else AddEntityCollideCallback("Player", "AreaDust3", nameof(CollideAreaGust), true, 1);
            }

            if (asChild == "AreaDustEnd")
            {
                CreateParticleSystemAtEntity(asChild, "ps_dust_whirl", asChild + "_ps", false);
                UnBlockHint("DarknessDecrease");
            }
            else CreateParticleSystemAtEntity(asChild, "ps_dust_push", asChild + "_ps", false);

            PlaySoundAtEntity(asChild + "s", "general_wind_whirl.snt", asChild + "_ps", 1.0f / 0.5f, false);
        }

        /////////////////////////////
        // PLAYER ALMOST FAINT
        /////////////////////////////

        void CollideAreaFaint(string asParent, string asChild, int alState)
        {
            StopRandomLook(); // de-activate the spinning head

            SetPlayerJumpDisabled(true);
            SetPlayerCrouchDisabled(true);

            AddTimer("TimerFaintEvents", 0.5f, nameof(TimerFaintEvents));

            FadePlayerFOVMulTo(1.5f, 0.25f);

            AddDebugMessage("Oh my, I think I'm almost going to pass out, almost...", false);
        }

        void TimerFaintEvents(string asTimer)
        {
            // Configurables
            int iMaxEventStep = 8;     // how many steps there are in the switch event
            float fEventSpeed = 1.0f;  // the default time between steps in an event

            // Helpers: the timer name is the variable, the timer and the callback of the loop.
            string sEvent = asTimer;
            AddLocalVarInt(sEvent, 1); // what step to play in the event

            switch (GetLocalVarInt(sEvent))
            {
                case 1:
                    StartPlayerLookAt("Areafaintlook_1", 0.1f, 0.1f, "");
                    SetPlayerMoveSpeedMul(0.85f);
                    StartScreenShake(0.002f, 1, 0.5f, 0.5f);
                    PlaySoundAtEntity("loop", "00_loop.snt", "Player", 1.0f / 0.1f, false);
                    PlaySoundAtEntity("faint", "00_faint.snt", "Player", 1.0f / 0.08f, false);
                    break;
                case 2:
                    MovePlayerHeadPos(-0.25f, -0.5f, 0, 1, 0.5f);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    FadePlayerRollTo(-50, 3, 5);
                    SetPlayerMoveSpeedMul(0.75f);
                    StartScreenShake(0.004f, 1, 0.5f, 0.5f);
                    break;
                case 3:
                    FadeOut(3);
                    MovePlayerHeadPos(0.25f, -0.3f, 0, 1, 0.5f);
                    StartPlayerLookAt("Areafaintlook_2", 0.3f, 0.3f, "");
                    SetPlayerMoveSpeedMul(0.7f);
                    StartScreenShake(0.005f, 1, 0.5f, 0.5f);
                    break;
                case 4:
                    FadePlayerRollTo(50, 5, 5);
                    SetPlayerMoveSpeedMul(0.5f);
                    StartScreenShake(0.006f, 1, 0.5f, 0.5f);
                    PlaySoundAtEntity("bodyfall", "player_bodyfall.snt", "Player", 1.0f / 0.3f, false);
                    break;
                case 5:
                    FadeIn(1.5f);
                    MovePlayerHeadPos(0, -0.8f, 0, 0.75f, 0.5f);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    StartPlayerLookAt("Areafaintlook_2", 0.1f, 0.1f, "");
                    SetPlayerMoveSpeedMul(0.3f);
                    break;
                case 6:
                    StopSound("faint", 0.2f);
                    FadeOut(3);
                    FadePlayerRollTo(-25, 3, 5);
                    SetPlayerMoveSpeedMul(0.4f);
                    break;
                case 7:
                    StartPlayerLookAt("Areafaintlook_1", 0.2f, 0.2f, "");
                    SetPlayerMoveSpeedMul(0.7f);
                    FadePlayerFOVMulTo(1.0f, 0.1f);
                    break;
                case 8:
                    StopSound("loop", 0.2f);
                    FadeIn(1.5f);
                    MovePlayerHeadPos(0, 0, 0, 1, 0.5f);
                    PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
                    FadePlayerRollTo(0, 2, 4);
                    StopPlayerLookAt();
                    SetPlayerMoveSpeedMul(1);
                    AddTimer("lookloop", RandFloat(2.0f, 6.0f), nameof(TimerRandomLook)); // re-activate the spinning head

                    // Reset player input
                    SetPlayerJumpDisabled(false);
                    SetPlayerCrouchDisabled(false);
                    break;
            }

            if (GetLocalVarInt(sEvent) <= iMaxEventStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }

        //////////////////////////
        // RANDOM LOOK SPIN
        //////////////////////////

        // The player has a bit of a random head during the level.
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

            FadePlayerFOVMulTo(RandFloat(0.7f, 1.3f), RandFloat(0.05f, 0.1f));

            SetPlayerMoveSpeedMul(RandFloat(0.6f, 0.9f));
            SetPlayerRunSpeedMul(RandFloat(0.2f, 0.4f));

            FadeImageTrailTo(RandFloat(0.75f, 1.0f), RandFloat(0.75f, 1.25f));

            bRoll = bRoll == false ? true : false;

            if (bRoll)
                FadePlayerRollTo(RandFloat(4, 12), RandFloat(0.075f, 0.35f), RandFloat(0.55f, 1.15f));
            else
                FadePlayerRollTo(RandFloat(-4, -12), RandFloat(0.05f, 0.25f), RandFloat(0.5f, 1));

            AddTimer("lookloop", RandFloat(3.0f, 6.0f), nameof(TimerRandomLook));
        }

        void StopRandomLook()
        {
            RemoveTimer("lookloop");

            SetPlayerMoveSpeedMul(1);
            FadePlayerFOVMulTo(1, 1);
            FadeImageTrailTo(0, 1.0f);
            FadePlayerRollTo(0, 0.5f, 2);
        }

        /////////////////////////////
        // INTERACT LARGE GATE
        /////////////////////////////

        // Not connected to anything in this map (there is no "LargeGate" entity).
        void InteractLargeGate(string asEntity)
        {
            PlaySoundAtEntity("guardboo", "guardian_distant1", "AreaLargeGate", 3.0f, false);
            PlaySoundAtEntity("thunderboo", "general_thunder.snt", "AreaLargeGate", 0.0f, false);

            PlayGuiSound("close_gate.ogg", 0.4f);

            AddTimer("1", 0.4f, nameof(TimerLargeGate));
            AddTimer("2", 0.1f, nameof(TimerLargeGate));
            AddTimer("3", 0.6f, nameof(TimerLargeGate));
            AddTimer("4", 2.0f, nameof(TimerLargeGate));
        }

        void TimerLargeGate(string asTimer)
        {
            if (asTimer == "1")
            {
                PlayMusic("01_event_critters", false, 0.7f, 1.0f, 10, false);
                StartScreenShake(0.005f, 2.0f, 1.0f, 1.0f);
                GiveSanityDamage(10.0f, true);
            }
            else if (asTimer == "2")
            {
            }
            else if (asTimer == "3")
            {
                PlayGuiSound("react_scare", 0.5f);
            }
            else if (asTimer == "4")
            {
                StopMusic(6.0f, 10);
                StopSound("guardboo", 2.0f);
                StopSound("thunderboo", 4.0f);

                SetEntityPlayerInteractCallback("LargeGate", nameof(InteractLargeGate02), true);
            }
        }

        void InteractLargeGate02(string asEntity)
        {
        }

        /////////////////////////////
        // MISC
        /////////////////////////////

        void LookAtQuest(string asEntity, int alState)
        {
            AddQuest("00Trail", "00Trail");
        }

        void CollideAreaQuest(string asParent, string asChild, int alState)
        {
            AddQuest("00Trail", "00Trail");
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("RainyHall");

            //----COLLIDE CALLBACKS----//
            AddEntityCollideCallback("Player", "areanarrowhalls", nameof(CollideNarrowHalls), false, 0);      // begin/end narrow hall effects
            AddEntityCollideCallback("Player", "areaendthunder", nameof(CollideAreaThunder), true, 1);        // begin/end thunder loop
            AddEntityCollideCallback("Player", "narrowpassout", nameof(CollideAreaNarrowPassOut), true, 1);   // player passes out in the hallway
            AddEntityCollideCallback("Player", "gallerydoor", nameof(CollideAreaGalleryDoor), true, 1);       // gallery door creaks open
            AddEntityCollideCallback("Player", "gallery", nameof(CollideAreaGallery), true, 1);               // gallery door slams behind the player
            AddEntityCollideCallback("Player", "areagustdoor", nameof(CollideAreaGustDoor), true, 1);         // gust swings the door open in hall 3
            AddEntityCollideCallback("Player", "AreaDust1", nameof(CollideAreaGust), true, -1);               // area that starts a gust blow
            AddEntityCollideCallback("Player", "AreaDustEnd", nameof(CollideAreaGust), true, -1);             // area that starts a gust at the end
            AddEntityCollideCallback("Player", "AreaFaint", nameof(CollideAreaFaint), true, 1);               // player almost faints in hall 2
            AddEntityCollideCallback("Player", "AreaQuest", nameof(CollideAreaQuest), true, -1);

            //---- INTRO SETTINGS ----//
            if (ScriptDebugOn() == false)
            {
                FadeGlobalSoundVolume(0, 0.01f);
                FadeGlobalSoundSpeed(0.7f, 0.01f);
                SetPlayerActive(false);
                ShowPlayerCrossHairIcons(false);
                SetInventoryDisabled(true);
                SetPlayerJumpDisabled(true);
                SetPlayerCrouchDisabled(true);
                FadeOut(0);

                // disable some event areas
                SetEntityActive("narrowpassout", false);
                SetEntityActive("gallerydoor", false);
                SetEntityActive("areanarrowhalls", false);
                SetEntityActive("areaendthunder", false);
                SetEntityActive("areabeginthunder", false);
                SetEntityActive("areagustdoor", false);
                SetEntityActive("AreaDust3", false);
                SetEntityActive("AreaFaint", false);

                // Teleport player to first start pos
                TeleportPlayer("IntroStart_1");

                AddTimer("StartGame", 1, nameof(TimerStartGame));
            }

            //----QUEST INIT----//
            SetPlayerLampOil(0); // so that the player has 0 lamp oil when finding the lantern in level 1
            SetNumberOfQuestsInMap(0);

            BlockHint("SanityAdd"); BlockHint("LanternNoItem"); BlockHint("LanternNoOil"); BlockHint("LeanHint");
            BlockHint("PushHint"); BlockHint("ThrowHint"); BlockHint("EntityGrab02"); BlockHint("EntityPush");
            BlockHint("EntitySlide"); BlockHint("EntityLever"); BlockHint("DarknessDecrease"); BlockHint("SanityHit");

            //----INSANITY----//
            SetInsanitySetEnabled("Ch02", false);
            SetInsanitySetEnabled("Ch03", false);

            //----DEBUG----//
            if (ScriptDebugOn())
            {
                AddDebugMessage("Intro disabled as debug is active.", false);

                // LEVEL TESTING
                AddTimer("lookloop", RandFloat(2.0f, 6.0f), nameof(TimerRandomLook));
                AddTimer("thunder", 1, nameof(TimerThunder));
                SetPlayerRunSpeedMul(0);
            }
        }

        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            //----PRELOADING----//
            PreloadSound("react_sigh"); PreloadSound("react_breath"); PreloadSound("player_climb"); PreloadSound("general_thunder");
            PreloadSound("00_creak"); PreloadSound("scare_walk_hallway"); PreloadSound("00_cuts"); PreloadSound("react_scare");
            PreloadSound("00_loop"); PreloadSound("player_bodyfall"); PreloadSound("00_faint"); PreloadSound("scare_slam_door");
            PreloadSound("ambience_wind_eerie_no3d"); PreloadSound("ambience_hollow_tinker"); PreloadSound("react_pant"); PreloadSound("joint_door_move_special");
            PreloadSound("general_wind_whirl"); PreloadSound("00_laugh"); PreloadSound("general_rain_m");

            PreloadParticleSystem("ps_dust_falling_narrow"); PreloadParticleSystem("ps_dust_push"); PreloadParticleSystem("ps_dust_whirl");
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            // Load screen setup
            SetupLoadScreen("LoadingText", "Ch01_Beginning", 5, "game_loading_rose.jpg");
        }
    }
}
