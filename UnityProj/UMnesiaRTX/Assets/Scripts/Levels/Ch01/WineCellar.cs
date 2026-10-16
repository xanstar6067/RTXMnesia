using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/05_wine_cellar.hps: the four chemical jars, falling debris and barrel,
    /// the ceiling corpses, door and bottle scares, alternate escape and grunt encounter.
    /// Journal: docs/ScriptPort/ch01/05_wine_cellar.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/05_wine_cellar.map")]
    public class WineCellar : HplScript
    {
        //////////////////////
        //BEGIN BLOCKED DOOR//
        /*Move wooden barrel to unlock door
         */
        void FuncUnlockDoor(string asParent, string asChild, int alState)
        {
            SetSwingDoorLocked("cellar_wood01_3", false, false);

            /*DEBUG
             */
            AddDebugMessage("UnlockDoor!", true);
        }
        //END BLOCKED DOOR//
        ////////////////////


        ////////////////////////////////
        //BEGIN MOANING & CREAK HORROR//
        /*Start the creak sounds + ps when entering basement, stop them when leaving the basement/area
         */
        void FuncMoanHorrors(string asParent, string asChild, int alState)
        {
            float fMoan = RandFloat(5.5f,15.5f);

            AddTimer("moanTimer", 4.5f+fMoan, nameof(FuncMoanTimer));
            AddTimer("stepTimer", 4.5f, nameof(FuncMoanTimer));

            PlaySoundAtEntity("moanSoundx", "scare_male_terrified5.snt", "Player", 0.0f, false);
            AddTimer("sanity", 0.7f, nameof(TimerSanity));

            /*DEBUG
             */
            AddDebugMessage("Begin moaning sounds", true);
        }
        /*Random placment of moan at random time
         */
        void FuncMoanTimer(string asTimer)
        {
            int iMoan = (int)RandFloat(1, 9);
            float fMoan = RandFloat(5.5f,15.5f);

            if(asTimer == "moanTimer") {
                PlaySoundAtEntity("moanSound"+iMoan, "scare_male_terrified.snt", "HorrorMoan_"+iMoan, 0.0f, false);

                AddTimer("moanTimer", 6.5f+fMoan, nameof(FuncMoanTimer));

            } else if(asTimer == "stepTimer") {
                iMoan = (int)RandFloat(1, 6);

                PlaySoundAtEntity("stepSound"+iMoan, "scare_steps_big.snt", "HorrorMoan_"+iMoan, 0.0f, false);

                AddTimer("stepTimer", 7.5f+fMoan, nameof(FuncMoanTimer));
            }

            /*DEBUG
             */
            AddDebugMessage("Now moaning in area: "+iMoan, false);
        }
        /*Random placment of creak at random time
         */
        void CreakTimer(string asTimer)
        {
            int iCreak = (int)RandFloat(1, 12);
            float fCreak = RandFloat(1.5f,7.5f);

            //ADD GRUNTING FROM GRUNT ENEMY
            PlaySoundAtEntity("creakSound"+iCreak, "scare_wood_creak_mix", "HorrorCreak_"+iCreak, 0.0f, false);
            CreateParticleSystemAtEntity("creakPS"+iCreak, "ps_dust_falling_small", "HorrorCreak_"+iCreak, false);

            AddTimer("creak", 0.5f+fCreak, nameof(CreakTimer));

            /*DEBUG
             */
            AddDebugMessage("Now creaking in area: "+iCreak, false);
        }
        void TimerSanity(string asTimer)
        {
            GiveSanityDamage(10, true);
        }
        //END MOANING & CREAK HORROR//
        //////////////////////////////


        /////////////////////////////////////////////////////////////////
        //BEGIN GRUNT ACTIVATION/DEACTIVATION(GRUNTS REMOVED FROM LEVEL//
        /*When picking oil in barrel room slam door !PICK CHEMICAL INSTEAD!
         */
        void PickOil(string asEntity, string asType)
        {
            //SetSwingDoorClosed("cellar_wood01_4", false, false);
            //SetSwingDoorDisableAutoClose("cellar_wood01_4", true);

            //AddPropImpulse("cellar_wood01_4", 2.0f, 0, 0, "World");

            /*if(!GetSwingDoorClosed("cellar_wood01_1"))
                PlaySoundAtEntity("SlamDoor","scare_slam_door.snt", "cellar_wood01_1", 0, false);

            SetSwingDoorClosed("cellar_wood01_1", true, true);
            PlaySoundAtEntity("gruntgruff", "amb_idle", "AreaGruntSound1", 0.0f, false);

            AddTimer("sanity", 1, "TimerSanity");

            PlaySoundAtEntity("PlayerScare","scare_tingeling.snt", "Player", 0, false);*/
        }
        /*Second grunt encounter
         */
        void CollideActivateGrunt(string asParent, string asChild, int alState)
        {
            SetSwingDoorClosed("cellar_wood01_1", false, false);
            SetSwingDoorDisableAutoClose("cellar_wood01_1", true);

            AddPropImpulse("cellar_wood01_1", -2.0f, 0, 0, "World");
        }
        //END GRUNT ACTIVATION/DEACTIVATION//
        /////////////////////////////////////


        //////////////////////////////////////////////////
        //BEGIN DEBRIS FALL DOWN AND LOCK PLAYER IN ROOM//
        /*Three areas decide which barrel on the loft should fall down on players head during debris event
         */
        void CollideBlockDoor(string asParent, string asChild, int alState)
        {
            SetLocalVarString("WhatBarrel", asChild);
        }
        /*When Picking the chem jar(it was a coin bag ...) in the room begin the debris event
         */
        void PickCoin(string asEntity, string asType)
        {
            SetSwingDoorClosed("cellar_wood01_7", true, false);
            AddPropImpulse("wood_beam_1", 1.7f, 0, 0, "World");
            AddPropImpulse("wood_beam_2", -1.7f, 0, 0, "World");

            SetEntityActive("AreaGiveSanity", true);

            AddTimer("activaterocks", 1.0f, nameof(TimerActiveRocks));
            AddTimer("TimerRockEvents", 0.1f, nameof(TimerRockEvents));

            AddGlobalVarInt("ChemJars",1);

            SetEntityActive("InsanityArea_2", true);

            SetEntityActive("AreaBlockDoor_1", false);
            SetEntityActive("AreaBlockDoor_2", false);
            SetEntityActive("AreaBlockDoor_3", false);

            SetPlayerCrouchDisabled(true);
            SetPlayerJumpDisabled(true);

            FadeLightTo("PointLight_31", 0,0,0,0, -1, 1);

            PlaySoundAtEntity("CaveInMonsterSound", "04_warn", "Player", 0, false);
        }
        void TimerActiveRocks(string asTimer)
        {
            for(int i=1;i<=5;i++) SetEntityActive("Rock_"+i, true);
        }
        void TimerRockEvents(string asTimer)
        {
            /*Configurables
             */
            int iMaxEventStep = 16;		//How many steps there are in the switch event
            float fEventSpeed = 0.5f;	//The default time between steps in an event

            /*Helpers - Do not edit
             */
            string sEvent = asTimer;	//Using first timer name for variable, timer name & callback for the timer that loops
            AddLocalVarInt(sEvent, 1);	//What step to play in the event

            switch(GetLocalVarInt(sEvent)){
                case 1:
                    PlayMusic("05_event_falling.ogg", false, 0.7f, 0, 10, false);
                    PlaySoundAtEntity("wind", "01_whirl", "Player", 2.0f, false);
                    PlaySoundAtEntity("rumble", "general_rock_rumble", "Player", 1.0f, false);
                    StartScreenShake(0.1f, 4, 0.75f,2.25f);
                    CreateParticleSystemAtEntity("windPS", "ps_dust_whirl_large", "Player", false);
                    SetPlayerMoveSpeedMul(0.5f);
                    SetPlayerRunSpeedMul(0);
                    SetPlayerLookSpeedMul(0.8f);
                break;
                case 2:
                    SetPlayerMoveSpeedMul(0.25f);
                    SetPlayerLookSpeedMul(0.6f);

                break;
                case 3:
                    SetLampLit("candlestick02_1", false, true);
                    SetLampLit("torch_static01_1", false, true);

                    //FadeLightTo("PointLight_21", 0, 0, 0, 0, 0, 12);
                    SetPlayerMoveSpeedMul(0.1f);
                    SetPlayerLookSpeedMul(0.4f);
                    PlaySoundAtEntity("breakrock", "05_rock_fall", "cellar_wood01_7", 0, false);
                    PlaySoundAtEntity("breakrock2", "05_rock_fall_2", "cellar_wood01_7", 0, false);
                    CreateParticleSystemAtEntity("rockPS", "ps_break_mansionbase_wall", "cellar_wood01_7", false);
                    StartPlayerLookAt("cellar_wood01_7", 5, 5, "");
                break;
                case 4:
                    StopSound("wind", 4);
                    StopSound("rumble", 4);
                    SetPlayerLookSpeedMul(0.2f);
                    PlaySoundAtEntity("player_breath", "react_breath", "Player", 0, false);
                break;
                case 5:
                    SetPlayerLookSpeedMul(0.1f);
                    PlaySoundAtEntity("barrelroll", "roll_wood", "Barrel"+GetLocalVarString("WhatBarrel"), 0.1f, false);
                break;
                case 6:
                    StartPlayerLookAt("Barrel"+GetLocalVarString("WhatBarrel"), 4, 4, "");
                    AddPropImpulse("Barrel"+GetLocalVarString("WhatBarrel"), 0, 0, -2.5f, "World");
                    StopSound("barrelroll", 1);
                break;
                case 7:
                    PlaySoundAtEntity("player_shock", "react_scare", "Player", 0, false);
                    GiveSanityDamage(10, false);
                    fEventSpeed = 0.55f;
                break;
                case 8:
                    PlaySoundAtEntity("player_hit", "player_falldamage_max", "Player", 0, false);
                    AddPlayerHealth(-20);
                    StopMusic(6, 0);
                    StopSound("amb_sound1", 6);
                    StopSound("amb_sound2", 6);
                    FadeOut(1.0f);
                    fEventSpeed = 0.15f;

                    SetLampLit("torch_static01_3", false, true);
                    SetLampLit("candlestick02_3", false, true);
                break;
                case 9:
                    PlaySoundAtEntity("player_fall_to_ground", "player_bodyfall", "Player", 0, false);
                    MovePlayerHeadPos(0, -1.1f, 0, 8, 0.5f);
                    StartPlayerLookAt("candlestick02_1", 3, 3, "");
                    FadePlayerRollTo(-60, 5, 5);


                    SetLanternActive(false, false);
                    SetLanternDisabled(true);

                    for(int i=1;i<=3;i++) AddPropImpulse("BarrelAreaBlockDoor_"+i, 1, 2, 2, "World");
                    fEventSpeed = 2.0f;
                break;
                case 10:
                    SetPlayerActive(false);
                    PlaySoundAtEntity("blackcloud", "05_cloud_swarm", "AreaBlockDoor_2", 24, false);
                    PlaySoundAtEntity("creak", "00_creak", "AreaBlockDoor_2", 4, false);

                    PlaySoundAtEntity("CaveInMonsterSound", "04_warn", "Player", 0, false);

                    SetEntityInteractionDisabled("BarrelAreaBlockDoor_1", false);
                    SetEntityInteractionDisabled("BarrelAreaBlockDoor_2", false);
                    SetEntityInteractionDisabled("BarrelAreaBlockDoor_3", false);

                    fEventSpeed = 4.0f;
                break;
                case 11:
                    FadeSepiaColorTo(0.5f, 0.025f);
                    FadeRadialBlurTo(0.1f, 0.025f);
                    SetRadialBlurStartDist(0.2f);
                    //SetLampLit("candlestick02_1", true, false);
                    StopSound("blackcloud", 12);
                    StopSound("creak", 6);

                    for(int i=1;i<=3;i++) AddPropImpulse("BarrelAreaBlockDoor_"+i, 0, 0.1f, 0, "World");	//To fix hanging in mid air

                    fEventSpeed = 3.0f;
                break;
                case 12:
                    FadeIn(1.5f);
                    SetPlayerActive(true);

                    SetPlayerLookSpeedMul(0.2f);
                    PlaySoundAtEntity("reactpant", "react_pant", "AreaBlockDoor_2", 0.5f, false);
                    fEventSpeed = 3.0f;
                break;
                case 13:
                    SetPlayerLookSpeedMul(0.4f);
                    PlayMusic("04_amb", true, 1, 10, 0, true);
                    PlaySoundAtEntity("amb_sound1", "05_amb1", "AreaAmb", 10, true);
                    PlaySoundAtEntity("amb_sound2", "05_amb2", "AreaAmb", 10, true);
                    PlaySoundAtEntity("player_get_up", "player_climb", "Player", 0, false);
                    FadePlayerRollTo(0, 80, 80);
                    MovePlayerHeadPos(0, 0, 0, 1, 0.5f);
                    FadeSepiaColorTo(0, 0.01f);
                    FadeRadialBlurTo(0, 0.01f);
                    SetPlayerMoveSpeedMul(0.4f);
                    SetPlayerRunSpeedMul(0.4f);

                    SetLanternDisabled(false);

                    SetPlayerCrouchDisabled(false);
                    SetPlayerJumpDisabled(false);

                    fEventSpeed = 1.0f;
                break;
                case 14:
                    SetPlayerLookSpeedMul(0.6f);
                    SetPlayerMoveSpeedMul(0.6f);
                    SetPlayerRunSpeedMul(0.6f);
                    fEventSpeed = 1.0f;
                break;
                case 15:
                    StopPlayerLookAt();
                    SetPlayerLookSpeedMul(0.8f);
                    SetPlayerMoveSpeedMul(0.8f);
                    SetPlayerRunSpeedMul(0.8f);
                    fEventSpeed = 1.0f;
                break;
                case 16:
                    SetPlayerLookSpeedMul(1.0f);
                    SetPlayerMoveSpeedMul(1.0f);
                    SetPlayerRunSpeedMul(1.0f);
                break;
            }

            if(GetLocalVarInt(sEvent) <= iMaxEventStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }
        /*The player can attach a ladder to get up on the loft
         */
        void CollideStickLadder(string asArea, string asBody)
        {
            return; //Disable event as you can't crouch on ladder any longer.

            SetEntityActive("LadderArea_1", true);
        }
        /*When on the loft begin a strange event with light
         */
        void CollideLoft(string asParent, string asChild, int alState)
        {
            return; //Disable event as you can't crouch on ladder any longer.

            AddTimer("L1", 1, nameof(TimerLoft));

            StartScreenShake(0.01f, 6, 1.5f,2.5f);
            PlaySoundAtEntity("cloud", "05_cloud", "Player", 6, false);
            PlaySoundAtEntity("orb", "03_orb_loop", "AreaLoftEnd", 6, false);

            /*DEBUG
             */
            AddDebugMessage("Collide "+asChild, false);
        }
        void TimerLoft(string asTimer)
        {
            if(asTimer == "L1"){
                FadeRadialBlurTo(0.1f, 0.025f);
                SetRadialBlurStartDist(0.2f);
                FadeLightTo("LoftLight_1", -1, -1, -1, -1, 7, 1.25f);
                AddTimer("L2", 1, nameof(TimerLoft));
            }
            else if(asTimer == "L2"){
                FadeLightTo("LoftLight_2", -1, -1, -1, -1, 7, 1.25f);
                FadeLightTo("LoftLight_1", -1, -1, -1, -1, 0,8);
                AddTimer("L3", 1, nameof(TimerLoft));
            }
            else if(asTimer == "L3"){
                FadeLightTo("LoftLight_3", -1, -1, -1, -1, 7, 1.25f);
                FadeLightTo("LoftLight_2", -1, -1, -1, -1, 0, 6);
                AddTimer("L4", 1, nameof(TimerLoft));
            }
            else if(asTimer == "L4"){
                StopSound("cloud", 6);
                StopSound("orb", 8);
                FadeLightTo("LoftLight_4", -1, -1, -1, -1, 7, 1.25f);
                FadeLightTo("LoftLight_3", -1, -1, -1, -1, 0, 8);
                AddTimer("L5", 1, nameof(TimerLoft));
            }
            else if(asTimer == "L5"){
                FadeLightTo("LoftLight_4", -1, -1, -1, -1, 0, 8);
                FadeRadialBlurTo(0, 0.04f);

                CreateParticleSystemAtEntity("bottlePS", "ps_ghost_release", "AreaLoftEnd", false);
                PlaySoundAtEntity("bottle", "03_in_a_bottle", "AreaLoftEnd", 0, false);
            }

            /*DEBUG
             */
            AddDebugMessage("Loft Timer "+asTimer, false);
        }
        /*Give a hint about push force when touching debris
         */
        void GiveHintPush(string asEntity)
        {
            GiveHint("PushHint", "Hints", "PushHint", 0);
        }
        void CollideGiveSanity(string asParent, string asChild, int alState)
        {
            GiveSanityBoostSmall();
        }
        //END DEBRIS FALL DOWN AND LOCK PLAYER IN ROOM//
        ////////////////////////////////////////////////


        void PickChemical(string asEntity, string asType)
        {
            //Turn off a light used to make the chemical more visible.
            if(asEntity == "Chemical_1") FadeLightTo("PointLight_32", 0,0,0,0, -1, 2);
            if(asEntity == "Chemical_3") FadeLightTo("PointLight_33", 0,0,0,0, -1, 2);
            if(asEntity == "Chemical_4") FadeLightTo("PointLight_34", 0,0,0,0, -1, 2);


            if(asEntity == "Chemical_3")
            {
                //if(!GetSwingDoorClosed("cellar_wood01_1"))
                    //PlaySoundAtEntity("SlamDoor","scare_slam_door.snt", "cellar_wood01_1", 0, false);

                //SetSwingDoorClosed("cellar_wood01_1", true, true);

                if(!GetSwingDoorClosed("cellar_wood01_1")){
                    PlaySoundAtEntity("SlamDoor","joint_door_move.snt", "cellar_wood01_1", 0.5f, false);

                    AddDebugMessage("Hey!", false);

                    AddTimer("doorforce1", 0.2f, nameof(TimerDoorForce));
                    AddTimer("doorforce2", 0.4f, nameof(TimerDoorForce));
                    AddTimer("doorforce3", 0.6f, nameof(TimerDoorForce));
                    AddTimer("doorforce4", 0.8f, nameof(TimerDoorForce));
                    AddTimer("doorforce5", 1.0f, nameof(TimerDoorForce));
                }

                PlaySoundAtEntity("gruntgruff", "amb_idle", "AreaGruntSound1", 0.0f, false);

                AddTimer("sanity", 1, nameof(TimerSanity));

                PlaySoundAtEntity("PlayerScare","scare_tingeling.snt", "Player", 0, false);
            }

            AddGlobalVarInt("ChemJars",1);

            CompleteQuest("04ChemicalsMoved", "04ChemicalsMoved");

            if(GetGlobalVarInt("ChemJars") == 4){
                PlayMusic("02_puzzle.ogg", false, 0.7f, 0, 10, false);
                GiveSanityBoost();
            }
        }
        void TimerDoorForce(string asTimer)
        {
            AddPropForce("cellar_wood01_1", 200.0f, 0.0f, 200.0f, "World");

            AddDebugMessage("Hey!", false);
            if(asTimer == "doorforce5")
                StopSound("SlamDoor", 1.0f);
        }
        void PickNoteTrans(string asEntity, string asType)
        {
            PlayMusic("05_paper_transformation.ogg", false, 0.7f, 0, 10, false);
        }


        ////////////////////////////////////////////////////////////
        //BEGIN CHAINS AND HANGING CORPSES(CARCASS AS PLACEHOLDERS//
        /*Create a blood drip particle and play a blood drip sound a bit later
         */
        void TimerBloodDrop(string asTimer)
        {
            if(asTimer == "AreaEndBlood_1" || asTimer == "AreaEndBlood_2" || asTimer == "AreaEndBlood_3" ){
                PlaySoundAtEntity("blooddrop", "general_blood_drop", asTimer, 0, false);
                AddTimer("AreaBeginBlood_"+GetLocalVarInt("Blood"), 0.1f, nameof(TimerBloodDrop));
            }
            else{
                CreateParticleSystemAtEntity("PSblood", "ps_blood_drop.ps", asTimer, false);
                AddTimer("AreaEndBlood_"+GetLocalVarInt("Blood"), 2.5f, nameof(TimerBloodDrop));
            }

            int iBlood = (int)RandFloat(1,3);

            SetLocalVarInt("Blood", iBlood);
        }
        /*Give the chains a bit of a push to make them swagel in air
        */
        void TimerChain(string asTimer)
        {
            for(int i=1;i<=6;i++)
                AddPropForce("invisible_body_"+i, RandFloat(100,300), 0, RandFloat(100,300), "Local");

            AddTimer(asTimer, 5, asTimer);
        }
        /*An area to activate the lookat area in ceiling
         */
        void CollideActiveCeiling(string asParent, string asChild, int alState)
        {
            if(alState == 1) SetEntityActive("AreaCeiling", true);
            else if(alState == -1) SetEntityActive("AreaCeiling", false);
        }
        /*When looking at ceiling and seeing corpses, loose sanity
         */
        void LookAtCeiling(string entity, int alState)
        {
            if(alState == 1){
                FadeSepiaColorTo(0.75f, 0.3f);
                GiveSanityDamage(10, false);
                PlaySoundAtEntity("whine", "05_whine", "Player", 4, false);
            }
            else if(alState == -1){
                FadeSepiaColorTo(0, 0.3f);
                StopSound("whine", 4);
                SetEntityActive("AreaCeiling", false);
                SetEntityActive("AreaCeilingActivate", false);
            }
        }
        //END CHAINS AND HANGING CORPSES(CARCASS AS PLACEHOLDERS//
        //////////////////////////////////////////////////////////


        //////////////////////////////////////
        //BEGIN EVENT WITH BOTTLES AND STEPS//
        void CollideAreaRemoveDoorEvent(string asParent, string asChild, int alState)
        {
            SetEntityPlayerInteractCallback("cellar_wood01_3", "", false);
        }
        /*Interact door and rumble is played behind it
         */
        void InteractDoorWithGruntBehind(string asEntity)
        {
            if(GetLocalVarInt("HeardRumble") >= 1){
                SetEntityActive("AreaOtherEscape", true);
                SetEntityActive("AreaHoleEvents", false);
                AddEntityCollideCallback("Player", "AreaOtherEscape", nameof(CollideAreaOtherEscape), true, 1);
            }

            AddLocalVarInt("HeardRumble", 1);

            PlaySoundAtEntity("gruntmumble", "amb_idle", "AreaBottleEvent", 0, false);
            PlaySoundAtEntity("bottlevent", "05_event_bottles", "AreaBottleEvent", 0, false);
            AddTimer("sanity", 0.75f, nameof(TimerGiveSanityDamage));
        }
        /*When going the other way, some rumble for the room on other side
         */
        void InteractDoorToHole(string asEntity)
        {
            AddLocalVarInt("HeardRumble", 1);

            if(GetLocalVarInt("HeardRumble") <= 1) return;

            PlaySoundAtEntity("steps1", "scare_wood_creak_walk", "HoleParticle", 0, false);

            AddTimer("step1", 0.3f, nameof(TimerPlaySteps));
            AddTimer("step2", 0.6f, nameof(TimerPlaySteps));
            AddTimer("step3", 0.9f, nameof(TimerPlaySteps));
        }
        void TimerPlaySteps(string asTimer)
        {
            if(asTimer == "step1") SetEntityActive("wood_box01_19", true);
            else if(asTimer == "step2") SetEntityActive("wood_box01_20", true);
            else SetEntityActive("wood_box01_21", true);
        }
        /*As looking in the room with a hole in the ceiling, debris fall down
         */
        void CollideHoleEvents(string asParent, string asChild, int alState)
        {
            AddLocalVarInt("HeardRumble", 1);

            if(GetLocalVarInt("HeardRumble") <= 2) return;

            for(int i=1;i<=3;i++) SetEntityActive("rock_small_"+i, true);
            CreateParticleSystemAtEntity("PShole", "ps_dust_falling_hole.ps", "HoleParticle", false);
            PlaySoundAtEntity("scratches", "05_wall_scratch", "HoleParticle", 0, false);

            SetEntityPlayerInteractCallback("cellar_wood01_3", "", true);

            AddTimer("sanity", 1.5f, nameof(TimerGiveSanityDamage));
        }
        void TimerGiveSanityDamage(string asTimer)
        {
            GiveSanityDamage(10, true);
        }
        //Alternate escape route
        void CollideAreaOtherEscape(string asParent, string asChild, int alState)
        {
            RemoveEntityCollideCallback("AreaUnlockDoor", "BarrelBlock");

            SetSwingDoorClosed("cellar_wood01_6", false, false);
            SetSwingDoorDisableAutoClose("cellar_wood01_6", true);

            SetSwingDoorLocked("cellar_wood01_3", false, false);
            SetSwingDoorDisableAutoClose("cellar_wood01_3", true);
            SetSwingDoorClosed("cellar_wood01_3", false, false);

            AddTimer("cellar_wood01_3", 0.1f, nameof(TimerPushTheDumbDoor02)); AddTimer("cellar_wood01_3", 0.2f, nameof(TimerPushTheDumbDoor02));

            AddPropImpulse("cellar_wood01_6", 2.0f, 0, 0, "World");
            AddPropImpulse("BarrelBlock", 0.0f, 16.0f, -16.0f, "World");

            PlaySoundAtEntity("closedoorsound", "close_door.snt", "cellar_wood01_6", 0, false);
        }
        void TimerPushTheDumbDoor02(string asTimer)
        {
            AddPropImpulse(asTimer, 0, 0, -2.0f, "World");
            AddDebugMessage("Push door!", false);
        }
        //END EVENT WITH BOTTLES AND STEPS//
        ////////////////////////////////////


        //////////////////////////////
        //BEGIN BANG AND PAIN SOUNDS//
        void CollideBangDoor(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("bangs", "05_event_door_bang", "cellar_wood01_5", 1, false);
            PlaySoundAtEntity("whineaa", "scare_whine_loop", "Player", 0.5f, false);

            FadeSepiaColorTo(0.75f, 0.3f);
            FadeImageTrailTo(1.5f, 0.5f);

            AddTimer("bang1", 0.6f, nameof(TimerBangDoor));
            AddTimer("bang2", 1.6f, nameof(TimerBangDoor));
            AddTimer("bang3", 2.5f, nameof(TimerBangDoor));
            AddTimer("bang4", 3.0f, nameof(TimerBangDoor));
        }
        void TimerBangDoor(string asTimer)
        {
            if(asTimer == "bang1"){
                AddPropImpulse("cellar_wood01_5", 0, 0, 5, "World");
                PlaySoundAtEntity("scare", "react_scare", "Player", 0.25f, false);
                CreateParticleSystemAtEntity("bang1", "ps_dust_impact_vert.ps", "cellar_wood01_5", false);
                StartScreenShake(0.008f, 0.5f, 0.1f,0.3f);
                GiveSanityDamage(10, true);
            }
            else if(asTimer == "bang2") {
                AddPropImpulse("cellar_wood01_5", 0, 0, 5, "World");
                CreateParticleSystemAtEntity("bang1", "ps_dust_impact_vert.ps", "cellar_wood01_5", false);
                StartScreenShake(0.008f, 0.5f, 0.1f,0.3f);
            }
            else if(asTimer == "bang3") {
                AddPropImpulse("cellar_wood01_5", 0, 0, 5, "World");
                PlaySoundAtEntity("scare", "react_breath", "Player", 0.5f, false);
                CreateParticleSystemAtEntity("bang1", "ps_dust_impact_vert.ps", "cellar_wood01_5", false);
                StartScreenShake(0.008f, 0.5f, 0.1f,0.3f);
            }
            else {
                SetSwingDoorLocked("cellar_wood01_5", false, false);
                FadeSepiaColorTo(0, 0.3f);
                FadeImageTrailTo(0, 0.1f);
                StopSound("whineaa", 4);
            }
        }
        //END BANG AND PAIN SOUNDS//
        ////////////////////////////


        /////////////////////
        //BEGIN ENEMY EVENT//
        void PickEnemy(string asEntity, string asType)
        {
            if(GetLocalVarInt("EnemyEventDone") == 1) return;
            if(GetGlobalVarInt("IGF")==1) return;

            SetEntityActive("AreaOtherEscape", false);

            AddEntityCollideCallback("Player", "AreaEnemyEvent_2", nameof(CollideActivateEnemy), true, 1);

            SetSwingDoorDisableAutoClose("cellar_wood01_4", true);
            SetSwingDoorClosed("cellar_wood01_4", false, false);

            AddPropImpulse("cellar_wood01_4", 2.0f, 0, 0, "World");

            PlaySoundAtEntity("monster", "grunt/amb_idle", "torch_static01_7", 0.25f, false);

            SetEntityActive("grunt_normal_1", true);

            AddTimer("enemy1", 0.2f, nameof(TimerEnemy));

            SetEntityPlayerLookAtCallback("AreaHoleEvents", nameof(LookAtEnemy), true);

            SetPlayerRunSpeedMul(0.6f);
            SetPlayerMoveSpeedMul(0.8f);

            SetLocalVarInt("EnemyEventDone", 1);
        }
        void LookAtEnemy(string asEntity, int alState)
        {
            RemoveEntityCollideCallback("Player", "AreaEnemyEvent_2");

            AddTimer("enemy2", 1.5f, nameof(TimerEnemy));
            AddTimer("enemy3", 5, nameof(TimerEnemy));
            AddTimer("enemy4", 10, nameof(TimerEnemy));
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_3", 0, "");

            SetEntityPlayerLookAtCallback("AreaHoleEvents", "", true);
        }
        void CollideActivateEnemy(string asParent, string asChild, int alState)
        {
            SetEntityPlayerLookAtCallback("AreaHoleEvents", "", true);

            AddTimer("enemy2", 1.5f, nameof(TimerEnemy));
            AddTimer("enemy3", 5, nameof(TimerEnemy));
            AddTimer("enemy4", 10, nameof(TimerEnemy));
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_3", 0, "");
        }

        void TimerEnemy(string asTimer)
        {
            if(asTimer == "speed"){
                SetPlayerRunSpeedMul(1);
                SetPlayerMoveSpeedMul(1);
                //FadePlayerFOVMulTo(1, 0.01f);
                //FadeRadialBlurTo(0, 0.1f);
            }
            else if(asTimer == "enemy1"){
                PlayMusic("05_event_steps.ogg", false, 0.8f, 1, 10, false);
                PlaySoundAtEntity("scare", "react_scare", "Player", 0.5f, false);
                //StartPlayerLookAt("grunt_normal_1", 5, 5, "");
                //FadePlayerFOVMulTo(2, 0.01f);
                //FadeRadialBlurTo(0.1f, 0.025f);
                //SetRadialBlurStartDist(0.2f);
                SetPlayerRunSpeedMul(0.4f);
                SetPlayerMoveSpeedMul(0.6f);
            }
            else if(asTimer == "enemy2"){
                //StopPlayerLookAt();
                PlaySoundAtEntity("react", "react_breath", "Player", 0.6f, false);
                SetPlayerRunSpeedMul(0.2f);
                SetPlayerMoveSpeedMul(0.4f);
            }
            else if(asTimer == "enemy3"){
                SetPlayerRunSpeedMul(0.4f);
                SetPlayerMoveSpeedMul(0.6f);
            }
            else{
                SetEntityActive("AreaEnemyEvent_3", false);	//If player throw box at enemy, enemy will chase player until player leaves level
                SetPlayerRunSpeedMul(1);
                SetPlayerMoveSpeedMul(1);
                //FadePlayerFOVMulTo(1, 0.01f);
                //FadeRadialBlurTo(0, 0.1f);
            }
        }
        void CollideDeactivateEnemy(string asParent, string asChild, int alState)
        {
            SetPlayerRunSpeedMul(0.6f);
            SetPlayerMoveSpeedMul(0.8f);

            SetEntityActive("grunt_normal_1", false);
            RemoveEntityCollideCallback("AreaUnlockDoor", "BarrelBlock");

            SetSwingDoorClosed("cellar_wood01_4", true, false);
            PlaySoundAtEntity("cellar_wood01_4slam", "scare_slam_door", "cellar_wood01_4", 0.0f, false);
            GiveSanityDamage(10, false);

            SetSwingDoorClosed("cellar_wood01_6", false, false);
            SetSwingDoorDisableAutoClose("cellar_wood01_6", true);

            SetSwingDoorLocked("cellar_wood01_3", false, false);
            SetSwingDoorDisableAutoClose("cellar_wood01_3", true);
            SetSwingDoorClosed("cellar_wood01_3", false, false);

            AddTimer("speed", 2, nameof(TimerEnemy));

            RemoveTimer("enemy4");

            AddTimer("cellar_wood01_3", 0.1f, nameof(TimerPushTheDumbDoor)); AddTimer("cellar_wood01_3", 0.2f, nameof(TimerPushTheDumbDoor));

            AddPropImpulse("cellar_wood01_6", 2.0f, 0, 0, "World");
            AddPropImpulse("BarrelBlock", 0.0f, 16.0f, -16.0f, "World");

            PlaySoundAtEntity("closedoorsound", "close_door.snt", "cellar_wood01_6", 0, false);
            PlaySoundAtEntity("react2", "react_breath", "Player", 0.8f, false);
        }
        void TimerPushTheDumbDoor(string asTimer)
        {
            AddPropImpulse(asTimer, 0, 0, -2.0f, "World");
            AddDebugMessage("Push door!", false);
        }
        void CollideEnemyHint(string asParent, string asChild, int alState)
        {
            GiveHint("EnemyTip01", "Hints", "EnemyTip01", 0);
        }

        //END ENEMY EVENT//
        ///////////////////


        //BEING MISC//
        void InteractTouchIt(string asEntity)
        {
            PlayGuiSound("impact_organic_low2.ogg", 0.5f);

            //SetMessage("Ch01Level05", "TouchIt", 0);

            AddTimer(asEntity, 1.0f, nameof(TimerTouchIt));
        }
        void TimerTouchIt(string asTimer)
        {
            SetEntityPlayerInteractCallback(asTimer, nameof(InteractTouchIt), true);
        }
        //END MISC//

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("WineCellar");

            //----COLLIDE CALLBACKS----//
            AddEntityCollideCallback("Player", "AreaRemoveDoorEvent", nameof(CollideAreaRemoveDoorEvent), false, 1);
            AddEntityCollideCallback("AreaUnlockDoor", "BarrelBlock", nameof(FuncUnlockDoor), true, -1);	//Barrel blocking door, move it out area to unlock door
            AddEntityCollideCallback("Player", "AreaBeginMoan", nameof(FuncMoanHorrors), true, 1);	//Big area in first room to trigger moans
            for(int i=1;i<=3;i++) AddEntityCollideCallback("Player", "AreaBlockDoor_"+i, nameof(CollideBlockDoor), false, 1);	//Three areas for barrel selection
            AddEntityCollideCallback("Player", "AreaLoft", nameof(CollideLoft), true, 1);	//Trigger event on loft
            AddEntityCollideCallback("Player", "AreaCeilingActivate", nameof(CollideActiveCeiling), false, 0);	//Activate lookat area for corpses in ceiling
            AddEntityCollideCallback("Player", "AreaHoleEvents", nameof(CollideHoleEvents), true, 1);	//Debris and particles from ceiling hole
            AddEntityCollideCallback("Player", "AreaBangDoor", nameof(CollideBangDoor), true, 1);	//Begin the door bang and pain event
            AddEntityCollideCallback("Player", "AreaEnemyHint", nameof(CollideEnemyHint), false, 1);	//Display hint about enemies
            AddEntityCollideCallback("Player", "AreaGiveSanity", nameof(CollideGiveSanity), true, 1);	//Display hint about enemies

            AddEntityCollideCallback("grunt_normal_1", "AreaEnemyEvent_3", nameof(CollideDeactivateEnemy), true, 1);	//Begin the door bang and pain event

            SetEntityPlayerLookAtCallback("AreaCeiling", nameof(LookAtCeiling), false);
            SetEntityPlayerInteractCallback("cellar_wood01_3", nameof(InteractDoorWithGruntBehind), true);
            SetEntityPlayerInteractCallback("cellar_wood01_4", nameof(InteractDoorToHole), true);

            // ENTITY INIT
            SetEntityInteractionDisabled("BarrelAreaBlockDoor_1", true);
            SetEntityInteractionDisabled("BarrelAreaBlockDoor_2", true);
            SetEntityInteractionDisabled("BarrelAreaBlockDoor_3", true);

            //---- ENEMY INIT ----//



            //----SOUND INIT----//
            PlaySoundAtEntity("amb_sound1", "05_amb1", "AreaAmb", 10, true);	//So that the sound can be faded out/in
            PlaySoundAtEntity("amb_sound2", "05_amb2", "AreaAmb", 10, true);	//So that the sound can be faded out/in


            //----ROPE CREATION----//
            /*Hanging chains in room with corpses in ceiling
             */
            /*for(int i=1;i<=6;i++) CreateRope("Rope0"+i, "RopeStart_"+i, "RopeEnd_"+i, "", "invisible_body_"+i+"_Body_1", //StartBody, EndBody
                0.5, 4.5, 0.2, 0.001, 100, 1, 					//Min total length, Max total length, Segment size, Damping, Strength, Stiffness
                "textures/ropes/chain.mat", 0.025, 0.5, 0.5, //Material, Radius, LengthTileAmount, LengthTileSize
                "",	0.49, 0.45,									//Sound, Sound start speed, Sound stop speed
                true, 5, 3 										//Automove, Automove acc, Automove max speed
                );*/

            //---- TIMER INIT ----//
            AddTimer("TimerChain", 1, nameof(TimerChain));	//Push chains to make them swagel a bit
            AddTimer("AreaBeginBlood_1", 0.1f, nameof(TimerBloodDrop));	//Drip blood from corpses
            AddTimer("creak", 1, nameof(CreakTimer));		//Creaking from the floor above

            //----QUEST INIT----//
            SetNumberOfQuestsInMap(0);

            //----VARIABLES ----//
            SetLocalVarString("WhatBarrel", "AreaBlockDoor_2");	//A default barrel selected for debris event, incase player does not touch an area
            SetGlobalVarInt("PlayerBeenInLevel05",1);

            //---- FOG SETUP ----//
            SetFogActive(true);
            SetFogColor(0.15f, 0.16f, 0.18f, 1);
            SetFogProperties(6, 22, 1, false);

            //Remove the dakrness hint, so the player can get it once more!
            RemoveHint("DarknessDecrease");

            /* TODO: This stays commented just in case it is needed in the future (read comment below)
            // Disable interaction with barrels, just in case the player tries to bring em down before triggering the event (Luis: I managed to do this and we really dont want the player to watch in fear a barrel lying still on the floor :P)
            SetEntityInteractionDisabled("BarrelAreaBlockDoor_1", true);
            SetEntityInteractionDisabled("BarrelAreaBlockDoor_2", true);
            SetEntityInteractionDisabled("BarrelAreaBlockDoor_3", true);
            */

            if(ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

                //SetEntityActive("FlashbackArea_1", false);
            }
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            //----PRELOADING----//
            PreloadSound("scare_male_terrified5"); PreloadSound("scare_male_terrified"); PreloadSound("scare_steps_big"); PreloadSound("scare_wood_creak_mix");
            PreloadSound("scare_tingeling"); PreloadSound("01_whirl"); PreloadSound("general_rock_rumble"); PreloadSound("05_rock_fall");
            PreloadSound("05_rock_fall_2"); PreloadSound("scare_slam_door"); PreloadSound("react_breath"); PreloadSound("roll_wood");
            PreloadSound("react_scare"); PreloadSound("player_falldamage_max"); PreloadSound("player_bodyfall"); PreloadSound("05_cloud_swarm");
            PreloadSound("00_creak"); PreloadSound("react_pant"); PreloadSound("05_amb1"); PreloadSound("05_amb2");
            PreloadSound("player_climb"); PreloadSound("05_cloud"); PreloadSound("03_orb_loop"); PreloadSound("03_in_a_bottle");
            PreloadSound("general_blood_drop"); PreloadSound("05_whine"); PreloadSound("05_event_bottles"); PreloadSound("scare_wood_creak_walk");
            PreloadSound("05_event_door_bang"); PreloadSound("05_wall_scratch"); PreloadSound("scare_whine_loop"); PreloadSound("close_door");
            PreloadSound("grunt/amb_idle");

            PreloadParticleSystem("ps_dust_falling_small"); PreloadParticleSystem("ps_dust_whirl_large"); PreloadParticleSystem("ps_break_mansionbase_wall");
            PreloadParticleSystem("ps_ghost_release"); PreloadParticleSystem("ps_blood_drop"); PreloadParticleSystem("ps_dust_falling_hole");
            PreloadParticleSystem("ps_dust_impact_vert");


            //----AUDIO----//
            PlayMusic("04_amb", true, 1, 5, 0, true);
            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            SetEntityActive("grunt_normal_1", false);	//Just incase player would be very very fast and throw a box at enemy during event.

            //////////////////////
            //Load Screen Setup
            SetupLoadScreen("LoadingText", "Ch01_Diary02_", 4, "game_loading_desert.jpg");
        }
    }
}
