using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/13_machine_room.hps: cogs, coal, guiding rods, numerical levers and guardian exit.
    /// Journal: docs/ScriptPort/ch01/13_machine_room.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/13_machine_room.map")]
    public class MachineRoom : HplScript
    {
        /////////////////////////
        //BEGIN MACHINE PUZZLES//
        /*Begin three wheels
         */
        void AttachWheel(string asArea, string asBody)
        {
            AddTimer(asArea, 0.5f, nameof(TimerWheel));

            AddLocalVarInt("WheelOK", 1);
            AddLocalVarInt(asArea, 1);    //asChild in CollideWrongWheel, to not display message as spoke is taken

            //DO PARTICLE
            SetLocalVarString("WheelSound", asBody);
            PlaySoundAtEntity(GetLocalVarString("WheelSound"), "scrape_metal.snt", asArea, 0.1f, false);
            PlaySoundAtEntity(GetLocalVarString("WheelSound"), "13_attach_wheel.snt", asArea, 0.2f, false);

            AddDebugMessage("Attach "+asArea+"_dyn", false);

            SetEntityActive("AreaSpokes", false); //Turns off the missing spokes message.
        }
        void TimerWheel(string asTimer)
        {
            StopSound(GetLocalVarString("WheelSound"), 0.5f);

            SetEntityActive(asTimer+"_dyn", false);
            SetEntityActive(asTimer+"_mounted", true);
        }
        /*Message that it is not the correct spoke for the wheel
         */
        void CollideWrongWheel(string asParent, string asChild, int alState)
        {
            if(GetLocalVarInt(asChild) != 1){
                SetMessage("Ch01Level13", "WheelNoFit", 0);

                PlaySoundAtEntity("clank", "impact_metal_low", asChild, 0.05f, false);
            }
        }

        /*Begin fuel and burn
         */
        void CollideCoalInBurner(string asParent, string asChild, int alState)
        {
            SetEntityInteractionDisabled(asParent, true);

            /*Enough coal in the chamber, fade out coal if more added
             */
            if(GetLocalVarInt("CoalOK") > 2){
                AddTimer(asParent, 1, nameof(TimerBurnerFull));

                SetPropEffectActive(asParent, true, true);

                SetMessage("Ch01Level13", "TooMuchCoal", 0);
                return;
            }

            AddLocalVarInt("CoalOK", 1);

            SetLocalVarString("CoalOnFire"+GetLocalVarInt("CoalOK"), asParent);

            if(GetLocalVarInt("CoalOK") == 3)    SetMessage("Ch01Level13", "EnoughCoal", 0);

            AddDebugMessage("Coal In Burner", false);
        }
        void TimerBurnerFull(string asTimer)
        {
            SetPropActiveAndFade(asTimer, false, 5);
        }
        /*If three coal in chamber, ignite them else message about too little coal
         */
        void PullCoalLever(string asEntity, int alState)
        {
            if(alState == -1){
                AddDebugMessage("Lever Min", false);
            }
            else if (alState == 1){
                PlaySoundAtEntity("BurnerSoundTest", "13_ignite", "AreaBurnerParticle", 0, false);

                if(GetLocalVarInt("CoalOK") < 3){
                    SetLampLit("13_burner", true, true);

                    PlaySoundAtEntity("BurnerSoundFail", "general_fire_burning", "AreaBurnerParticle", 0.2f, false);

                    AddTimer("fail", 0.2f, nameof(TimerBurnerFailed));
                    AddTimer(asEntity, 1, nameof(TimerBurnerFailed));

                    SetLeverStuckState(asEntity, 2, false);
                    return;
                }

                SetLeverStuckState(asEntity, 1, false);

                SetLampLit("13_burner", true, true);

                PlaySoundAtEntity("BurnerSound", "general_fire_burning", "AreaBurnerParticle", 1, true);

                for(int i=1;i<=3;i++){
                    string sTemp = i.ToString(CultureInfo.InvariantCulture);

                    AddTimer(sTemp, RandFloat(1.0f, 1.0f+i), nameof(TimerBurnerStarted));
                }

                AddDebugMessage("Lever Max", false);
            }
        }
        void TimerBurnerFailed(string asTimer)
        {
            StopSound("BurnerSoundFail", 2);
            SetLampLit("13_burner", false, true);

            if(asTimer == "coal_lever"){
                SetLeverStuckState(asTimer, 0, false);

                if(GetLocalVarInt("CoalOK") == 0) SetMessage("Ch01Level13", "TooLittleCoal", 0);
                else SetMessage("Ch01Level13", "TooLittleCoal02", 0);
            }
        }
        void TimerBurnerStarted(string asTimer)
        {
            if(asTimer == "1" || asTimer == "2" || asTimer == "3" ){
                SetPropEffectActive(GetLocalVarString("CoalOnFire"+asTimer), true, true);
                return;
            }
        }

        /*Begin the three guiding rods
         */
        void UseRod(string asItem, string asEntity)
        {
            /*If rod in slot already, do not enter a second
             */
            if(GetLocalVarInt(asEntity) == 1){
                SetMessage("Ch01Level13", "SlotTaken", 0);

                PlaySoundAtEntity("clank", "impact_generic_hard_low", asEntity, 0.05f, false);
                return;
            }

            /*If rod and slot correct, set rod string to _correct, else _error
             */
            if(asItem == "guiding_rod01" && asEntity == "ra01"){
                SetLocalVarString(asItem, asEntity+"_correct");
            }
            else if(asItem == "guiding_rod02" && asEntity == "ra02"){
                SetLocalVarString(asItem, asEntity+"_correct");
            }
            else if(asItem == "guiding_rod03" && asEntity == "ra03"){
                SetLocalVarString(asItem, asEntity+"_correct");
            } else {
                SetLocalVarString(asItem, asEntity+"_error");
            }

            /*Remove item from inventory, create item at position in world and set area taken
             */
            RemoveItem(asItem);

            CreateEntityAtArea(asItem, asItem, "magic_"+asEntity, true);

            PlaySoundAtEntity("s"+asItem, "13_rod_in", "magic_"+asEntity, 0, false);

            SetEntityCallbackFunc(asItem, nameof(PickRod));

            SetLocalVarInt(asEntity, 1);

            AddLocalVarInt("RodsInPosition", 1);

            /*If all three rods are correct end the puzzle
             */
            if(GetLocalVarString("guiding_rod01") == "ra01_correct" && GetLocalVarString("guiding_rod02") == "ra02_correct" &&
                GetLocalVarString("guiding_rod03") == "ra03_correct"){

                SetMessage("Ch01Level13", "SlotsCorrect", 0);

                SetLocalVarInt("GuideOK", 1);

                PlaySoundAtEntity("done"+asItem, "13_press_done", "magic_"+asEntity, 0, false);

                for(int i=1;i<=6;i++){
                    CreateParticleSystemAtEntity("PressP"+i, "ps_steam.ps", "PressParticles_"+i, true);
                    PlaySoundAtEntity("PressS"+i, "13_steam", "PressParticles_"+i, 0.5f, true);
                }

                for(int i=1;i<=3;i++)
                    SetEntityInteractionDisabled("guiding_rod0"+i, true);

                return;
            }

            /*Incorrect rod positions*/
            if(GetLocalVarInt("RodsInPosition") == 3){
                PlaySoundAtEntity("error1", "13_press_fail", "magic_"+asEntity, 0, false);
                PlaySoundAtEntity("error2", "17_attach_crank.snt", "magic_"+asEntity, 0.5f, false);
            }

            AddDebugMessage(asItem+" Value: "+GetLocalVarString(asItem), false);
        }
        void PickRod(string asEntity, string asType)
        {
            /*If a rod is removed, set the area to empty
             */
            if(StringContains(GetLocalVarString(asEntity), "ra01")){
                SetLocalVarInt("ra01", 0);
                PlaySoundAtEntity("s"+asEntity, "13_rod_out", "ra01", 0, false);
                CreateParticleSystemAtEntity("p"+asEntity, "ps_steam_puff.ps", "ra01", false);
            }
            else if(StringContains(GetLocalVarString(asEntity), "ra02")){
                SetLocalVarInt("ra02", 0);
                PlaySoundAtEntity("s"+asEntity, "13_rod_out", "ra02", 0, false);
                CreateParticleSystemAtEntity("p"+asEntity, "ps_steam_puff.ps", "ra02", false);
            }
            else if(StringContains(GetLocalVarString(asEntity), "ra03")){
                SetLocalVarInt("ra03", 0);
                PlaySoundAtEntity("s"+asEntity, "13_rod_out", "ra03", 0, false);
                CreateParticleSystemAtEntity("p"+asEntity, "ps_steam_puff.ps", "ra03", false);
            }

            AddLocalVarInt("RodsInPosition", -1);

            SetLocalVarString(asEntity, "_error");

            AddDebugMessage(asEntity+" Value: "+GetLocalVarString(asEntity), false);
        }

        /*Begin the numerical levers
         */
        void SixLevers(string asEntity, int alState)
        {
            /*Set values depending on up/down postion or 0 for middle
             */
            if(alState == -1){
                if(asEntity == "machine_lever_1") SetLocalVarInt("up_"+asEntity, 3);
                else if(asEntity == "machine_lever_2") SetLocalVarInt("up_"+asEntity, 3);
                else if(asEntity == "machine_lever_3") SetLocalVarInt("up_"+asEntity, 5);
                else if(asEntity == "machine_lever_4") SetLocalVarInt("up_"+asEntity, 1);
                else if(asEntity == "machine_lever_5") SetLocalVarInt("up_"+asEntity, 2);
                else if(asEntity == "machine_lever_6") SetLocalVarInt("up_"+asEntity, 4);

                CheckValue(asEntity);

                AddDebugMessage("Lever Min & up value: " + GetLocalVarInt("up_value"), false);
            }
            else if(alState == 1){
                if(asEntity == "machine_lever_1") SetLocalVarInt("down_"+asEntity, 1);
                else if(asEntity == "machine_lever_2") SetLocalVarInt("down_"+asEntity, 5);
                else if(asEntity == "machine_lever_3") SetLocalVarInt("down_"+asEntity, 6);
                else if(asEntity == "machine_lever_4") SetLocalVarInt("down_"+asEntity, 5);
                else if(asEntity == "machine_lever_5") SetLocalVarInt("down_"+asEntity, 2);
                else if(asEntity == "machine_lever_6") SetLocalVarInt("down_"+asEntity, 2);

                CheckValue(asEntity);

                AddDebugMessage("Lever Max & down value: " + GetLocalVarInt("down_value"), false);
            }
            else if(alState == 0){
                SetLocalVarInt("up_"+asEntity, 0);
                SetLocalVarInt("down_"+asEntity, 0);

                AddDebugMessage("Lever Mid", false);
            }

            /*All rods correct
             */
            if(GetLocalVarInt("up_value") == 8 && GetLocalVarInt("down_value") == 8){
                SetMessage("Ch01Level13", "EvenFlow", 0);

                SetLocalVarInt("SticksOK", 1);

                for(int i=1;i<7;i++)
                    SetLeverStuckState("machine_lever_"+i, GetLeverState("machine_lever_"+i), true);

                for(int i=1;i<=6;i++){
                    CreateParticleSystemAtEntity("FlowP"+i, "ps_steam.ps", "FlowParticles_"+i, true);
                    PlaySoundAtEntity("FlowS"+i, "13_steam", "FlowParticles_"+i, 0.5f, true);
                }

                PlaySoundAtEntity("done"+asEntity, "13_flow_done", asEntity, 0.0f, false);
            }
        }
        /*Set value to 0 then add all the values from the levers to see sum of lever positions
         */
        void CheckValue(string sEntity)
        {
            PlaySoundAtEntity("s"+sEntity, "lever_mech_min_max", sEntity, 0.0f, false);

            SetLocalVarInt("up_value", 0);
            SetLocalVarInt("down_value", 0);

            for(int i=1;i<=6;i++){
                AddLocalVarInt("up_value", GetLocalVarInt("up_machine_lever_"+i));
                AddLocalVarInt("down_value", GetLocalVarInt("down_machine_lever_"+i));
            }
        }

        /*Begin start the machine
         */
         void PullStartLever(string asEntity, int alState)
        {
            if(alState == -1) AddDebugMessage("Lever Min", false);
            else if (alState == 1) StartMachine(asEntity);
        }
        /*If all settings correct start the machine, if not do a hickup start
         */
        void StartMachine(string sEntity)
        {
            PlaySoundAtEntity("s1"+sEntity, "13_ignite", sEntity, 0.2f, false);
            PlaySoundAtEntity("s2"+sEntity, "13_machine_fail", sEntity, 0.2f, false);

            if(GetLocalVarInt("WheelOK") == 3 && GetLocalVarInt("CoalOK") == 3 && GetLocalVarInt("GuideOK") == 1 && GetLocalVarInt("SticksOK") == 1)
            {
                for(int i=1;i<=6;i++){
                    CreateParticleSystemAtEntity("DoneP"+i, "ps_steam.ps", "DoneParticles_"+i, true);
                    PlaySoundAtEntity("DoneS"+i, "13_steam", "DoneParticles_"+i, 0.5f, true);
                }
                for(int i=1;i<=8;i++)
                    CreateParticleSystemAtEntity("SteamP"+i, "ps_steam_cloud.ps", "SteamCloud_"+i, true);

                CompleteQuest("09StartElevator", "09StartElevator");
                SetGlobalVarInt("ElevatorMachineRunning", 1);

                AddEntityCollideCallback("Player", "AreaHowl_1", nameof(CollideExitHowls), true, 1);
                AddEntityCollideCallback("Player", "AreaHowl_2", nameof(CollideExitHowls), true, 1);

                StartScreenShake(0.03f, 2.0f, 0.5f,1.5f);

                PlaySoundAtEntity("s3"+sEntity, "13_machine_run", sEntity, 2, true);
                PlaySoundAtEntity("s31"+sEntity, "13_machine_extra", sEntity, 2, true);
                PlaySoundAtEntity("s4"+sEntity, "13_whomp", sEntity, 4, true);

                PlayMusic("13_puzzle_machine.ogg", false, 1.0f, 0.5f, 10, false);

                GiveSanityBoost();

                RunWheels(1.0f,-1.0f);

                SetLeverStuckState(sEntity, 1, false);
            }
            else
            {
                StartScreenShake(0.01f, 1.0f, 0.25f,0.5f);

                AddTimer("stopwheels", 1, nameof(TimerMachineError));

                RunWheels(1.0f,-1.0f);
            }
        }
        void TimerMachineError(string asTimer)
        {
            SetMessage("Ch01Level13", "MachineNotFixed", 0);

            RunWheels(0.0f,0.0f);
        }
        void RunWheels(float fSpeed1, float fSpeed2)
        {
            RotatePropToSpeed("elevator_machine_cog_small_2", 1, fSpeed2*3, 0, 0, 1, false, "");
            RotatePropToSpeed("elevator_machine_cog_small_1", 1, fSpeed1*3, 0, 0, 1, false, "");
            RotatePropToSpeed("elevator_machine_cog_small_3", 1, fSpeed2*3, 1, 0, 0, false, "");
            RotatePropToSpeed("elevator_machine_cog_main_1", 1, fSpeed1, 1, 0, 0, false, "");

            if(GetLocalVarInt("WheelOK") != 3) return;

            RotatePropToSpeed("cogwheel01_mounted", 1, fSpeed2, 0, 0, 1, false, "");
            RotatePropToSpeed("cogwheel02_mounted", 1, fSpeed1, 0, 0, 1, false, "");
            RotatePropToSpeed("cogwheel03_mounted", 1, fSpeed2, 0, 0, 1, false, "");
        }
        //END MACHINE PUZZLES//
        ///////////////////////


        ////////////////////
        //BEGIN EXIT HOWLS//
        void CollideExitHowls(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("howl"+asChild, "guardian_distant3.snt", "Player", 0, false);

            AddTimer(nameof(TimerEHowl), 0.5f, nameof(TimerEHowl));

            SetPlayerMoveSpeedMul(0.8f);
            SetPlayerRunSpeedMul(0.75f);
            FadeImageTrailTo(1.0f,1.0f);

            /*First collide after steam room exit
             */
            if(asChild == "AreaHowl_1"){
                PlaySoundAtEntity("slimeloop1", "slime_loop", "slime_pile_2", 3, true);
                PlaySoundAtEntity("slimeloop2", "slime_loop", "slime_6way_2", 4, true);
                PlaySoundAtEntity("slimeloop3", "slime_loop", "slime_pile_large_3", 6, true);
                PlaySoundAtEntity("amb_guard", "amb_guardian", "FlowParticles_4", 3, true);

                SetLocalVarFloat("fFade", 0.5f);

                StopSound("Sound_2", 5.0f);

                /*SetSwingDoorLocked("cellar_wood01_3", true, true);

                if(!GetSwingDoorClosed("cellar_wood01_3"))
                    PlaySoundAtEntity("slammerdammer", "scare_slam_door", "cellar_wood01_3", 0.0f, false);*/

                StartScreenShake(0.01f, 0.75f, 0.25f,0.5f);

                SetEntityActive("SlimeDamageArea_*", true);

                for(int i=1;i<=3;i++) FadeLightTo("SlimeLight_"+i, -1, -1, -1, -1, 5, 5);

                string[] aSlime1 = {"slime_6way_1", "slime_6way_2", "slime_6way_3", "slime_6way_4",
                                    "slime_3way_1", "slime_3way_2", "slime_pile_1", "slime_pile_2",
                                    "slime_pile_large_1", "slime_pile_large_2", "slime_pile_large_3", "slime_anim_ceiling_1",
                                    "slime_egg_1", "slime_egg_2", "slime_egg_3", "slime_egg_4",
                                    "slime_anim_wall_1", "slime_anim_wall_2"};

                for(int i=0;i<=aSlime1.Length;i++)
                {
                    // The out-of-range index silently aborts this AngelScript call.
                    if(i == aSlime1.Length) return;
                    AddTimer(aSlime1[i], RandFloat(0.1f, 0.5f), nameof(TimerSlimeAppear));
                }

            /*Second collide, stairs before exit
             */
            } else {
                PlaySoundAtEntity("slimeloop4", "slime_loop", "slime_egg_7", 3, true);
                PlaySoundAtEntity("slimeloop5", "slime_loop", "slime_6way_7", 4, true);
                PlaySoundAtEntity("slimeloop6", "slime_loop", "slime_pile_large_7", 6, true);

                SetLocalVarFloat("fFade", 0.5f);

                StartScreenShake(0.01f, 0.75f, 0.25f,0.5f);

                for(int i=4;i<=6;i++) FadeLightTo("SlimeLight_"+i, -1, -1, -1, -1, 5, 5);

                string[] aSlime2 = {"slime_pile_large_4", "slime_pile_large_5", "slime_pile_large_6", "slime_pile_large_7",
                                    "slime_6way_5", "slime_6way_6", "slime_6way_7", "slime_6way_8",
                                    "slime_3way_3", "slime_3way_4", "slime_3way_5", "slime_anim_ceiling_2",
                                    "slime_anim_wall_3", "slime_egg_5", "slime_egg_6", "slime_egg_7",};

                for(int i=0;i<=aSlime2.Length;i++)
                {
                    // The out-of-range index silently aborts this AngelScript call.
                    if(i == aSlime2.Length) return;
                    AddTimer(aSlime2[i], RandFloat(0.75f, 1.25f), nameof(TimerSlimeAppear));
                }
            }
        }
        /*Fade in slime at random speed and play sounds if true
         */
        void TimerSlimeAppear(string asTimer)
        {
            SetPropActiveAndFade(asTimer, true, RandFloat(1.0f,3.0f));

            float fPlaySound = RandFloat(0.7f,1.3f);

            if(fPlaySound < 1)
                AddTimer(asTimer, 1.0f, nameof(TimerSlimeSound));
        }
        void TimerSlimeSound(string asTimer)
        {
            PlaySoundAtEntity("s"+asTimer, "slime_create.snt", asTimer, GetLocalVarFloat("fFade"), false);

            AddLocalVarFloat("fFade", -0.05f);

            AddDebugMessage("Fade: "+Str(GetLocalVarFloat("fFade")), false);
        }
        /*Event effect steps for the howl
         */
        void TimerEHowl(string asTimer)
        {
            int iMaxEventStep = 8;
            float fEventSpeed = 0.5f;
            string sEvent = asTimer;

            AddLocalVarInt(sEvent, 1);

            switch(GetLocalVarInt(sEvent)){
                case 1:
                    StartScreenShake(0.05f, 5.0f, 1.0f,2.5f);
                break;
                case 2:
                    FadePlayerFOVMulTo(2, 0.08f);
                    SetPlayerMoveSpeedMul(0.7f);
                    SetPlayerRunSpeedMul(0.5f);
                    GiveSanityDamage(20, false);
                break;
                case 3:
                    SetPlayerMoveSpeedMul(0.6f);
                    SetPlayerRunSpeedMul(0.25f);
                    PlaySoundAtEntity("aah", "react_scare.snt", "Player", 0.3f, false);
                    fEventSpeed = 2.5f;
                break;
                case 4:
                    PlaySoundAtEntity("s"+asTimer, "react_breath.snt", "Player", 0.0f, false);
                    FadePlayerFOVMulTo(1, 0.5f);
                    fEventSpeed = 1.0f;
                break;
                case 5:
                    PlaySoundAtEntity("s"+asTimer, "react_breath.snt", "Player", 0.1f, false);
                    fEventSpeed = 1.5f;
                break;
                case 6:
                    PlaySoundAtEntity("s"+asTimer, "react_breath.snt", "Player", 0.3f, false);
                    SetPlayerMoveSpeedMul(0.7f);
                    SetPlayerRunSpeedMul(0.5f);
                    fEventSpeed = 1.5f;
                break;
                case 7:
                    SetPlayerMoveSpeedMul(0.85f);
                    SetPlayerRunSpeedMul(0.75f);
                break;
                case 8:
                    PlaySoundAtEntity("s"+asTimer, "react_breath.snt", "Player", 0.6f, false);
                    SetPlayerMoveSpeedMul(1.0f);
                    SetPlayerRunSpeedMul(1.0f);
                    FadeImageTrailTo(0,2);
                break;
            }

            if(GetLocalVarInt(sEvent) <= iMaxEventStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }
        //END EXIT HOWLS//
        //////////////////


        ////////////////////////
        //BEGIN VARIOUS THINGS//
        /*Various drips and drops throughout the level, only sound
         */
        void TimerDrop(string asTimer)
        {
            string[] aDropPlaces = {"PressParticles_1","PressParticles_2","PressParticles_3","PressParticles_4","PressParticles_5","PressParticles_6",
                                    "FlowParticles_1","FlowParticles_2","FlowParticles_3","FlowParticles_4","FlowParticles_5","FlowParticles_6",
                                    "DoneParticles_1","DoneParticles_2","DoneParticles_3","DoneParticles_4","DoneParticles_5","DoneParticles_6" };

            asTimer = aDropPlaces[RandInt(0, 17)];
                PlaySoundAtEntity(asTimer, "general_water_drip_single2", asTimer, 0, false);

            AddTimer(asTimer, RandFloat(0.25f, 1.0f), nameof(TimerDrop));
        }
        //END VARIOUS THINGS//
        //////////////////////

        ////////////////////////
        //COG WHEEL PICKING //

        void InteractDynCogWheel(string asEntity)
        {
            if(asEntity=="cogwheel01_dyn") FadeLightTo("PointLight_4", 0,0,0,0, -1, 3);
            if(asEntity=="cogwheel02_dyn") FadeLightTo("PointLight_6", 0,0,0,0, -1, 3);
            if(asEntity=="cogwheel03_dyn") FadeLightTo("PointLight_5", 0,0,0,0, -1, 3);
            if(asEntity=="start_lever") FadeLightTo("PointLight_7", 0,0,0,0, -1, 3);
        }


        //COG WHEEL PICKING //
        ////////////////////////

        ////////////////////////
        //COG WHEEL PICKING //
        void InteractAreaSpokes(string asEntity)
        {
            SetMessage("Ch01Level13", "MissingCogWheelsOnSpokes", -1);
        }

        //COG WHEEL PICKING //
        ////////////////////////

        //PICKING DIARIES
        void EntityCallPickDiary(string asEntity, string type)
        {
            AddLocalVarInt("DiaryNr", 1);

            if(GetLocalVarInt("DiaryNr") == 1)
                PlayMusic("17_paper_herbert01.ogg", false, 0.6f, 0.5f, 10, false);
            if(GetLocalVarInt("DiaryNr") == 2)
                PlayMusic("17_paper_herbert02.ogg", false, 0.6f, 0.5f, 10, false);
            if(GetLocalVarInt("DiaryNr") == 3)
                PlayMusic("26_paper_daniel01.ogg", false, 0.6f, 0.5f, 10, false);
        }

        //Guardian amb
        void TimerGuardianAmb(string asTimer)
        {
            PlaySoundAtEntity("guradianamb", "13_idle.snt", asTimer+RandInt(1,4), RandFloat(0.0f, 5.0f), false);

            AddTimer(asTimer, RandFloat(15.0f, 30.0f), nameof(TimerGuardianAmb));

            AddDebugMessage("Guard Sound", false);
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("MachineRoom");

            //---- COLLIDE CALLBACKS ----//
            AddEntityCollideCallback("cogwheel01_dyn", "cogwheel02", nameof(CollideWrongWheel), false, 1);
            AddEntityCollideCallback("cogwheel01_dyn", "cogwheel03", nameof(CollideWrongWheel), false, 1);
            AddEntityCollideCallback("cogwheel02_dyn", "cogwheel01", nameof(CollideWrongWheel), false, 1);
            AddEntityCollideCallback("cogwheel02_dyn", "cogwheel03", nameof(CollideWrongWheel), false, 1);
            AddEntityCollideCallback("cogwheel03_dyn", "cogwheel01", nameof(CollideWrongWheel), false, 1);
            AddEntityCollideCallback("cogwheel03_dyn", "cogwheel02", nameof(CollideWrongWheel), false, 1);
            for(int i=1;i<=12;i++){
                AddEntityCollideCallback("coal_"+i, "AreaBurner", nameof(CollideCoalInBurner), true, 1);
                SetPropEffectActive("coal_"+i, false, false);
            }

            //---- INTERACT CALLBACKS ----//
            for(int i=1;i<=3;i++) for(int j=1;j<=3;j++)
                AddUseItemCallback("guiding_rod0"+i+"0"+j, "guiding_rod0"+i, "ra0"+j, nameof(UseRod), false);

            //---- MISC SECTUP ----//
            SetNumberOfQuestsInMap(1);

            AddTimer("BeginDrop", 0.1f, nameof(TimerDrop));
            AddTimer("ScriptArea_", RandFloat(10.0f, 15.0f), nameof(TimerGuardianAmb));

            if(ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

                /*For script testing
                 */
                GiveItemFromFile("guiding_rod01", "guiding_rod01.ent");
                GiveItemFromFile("guiding_rod02", "guiding_rod02.ent");
                GiveItemFromFile("guiding_rod03", "guiding_rod03.ent");

                AddNote("CH01L11_ElevatorMachine", "Note");

                //SetLocalVarInt("WheelOK", 3);
                //SetLocalVarInt("CoalOK",3);
                //SetLocalVarInt("GuideOK",1);
                //SetLocalVarInt("SticksOK",1);

                /*for(int i=1;i<=6;i++){
                    CreateParticleSystemAtEntity("PressP"+i, "ps_steam.ps", "PressParticles_"+i, false);
                    CreateParticleSystemAtEntity("FlowP"+i, "ps_steam.ps", "FlowParticles_"+i, false);
                    CreateParticleSystemAtEntity("DoneP"+i, "ps_steam.ps", "DoneParticles_"+i, false);
                } */

                //AddEntityCollideCallback("Player", "AreaHowl_1", "CollideExitHowls", true, 1);
                //AddEntityCollideCallback("Player", "AreaHowl_2", "CollideExitHowls", true, 1);
            }
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            PreloadSound("scrape_metal"); PreloadSound("impact_metal_low"); PreloadSound("13_ignite"); PreloadSound("general_fire_burning");
            PreloadSound("impact_generic_hard_low"); PreloadSound("13_rod_in"); PreloadSound("13_press_done"); PreloadSound("13_steam");
            PreloadSound("13_rod_out"); PreloadSound("13_flow_done"); PreloadSound("lever_mech_min_max"); PreloadSound("13_machine_fail");
            PreloadSound("13_machine_run"); PreloadSound("13_whomp"); PreloadSound("guardian_distant3"); PreloadSound("slime_loop");
            PreloadSound("scare_slam_door"); PreloadSound("amb_guardian"); PreloadSound("slime_create"); PreloadSound("react_breath");
            PreloadSound("react_scare");

            PreloadParticleSystem("ps_steam"); PreloadParticleSystem("ps_steam_puff"); PreloadParticleSystem("ps_steam_cloud");

            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {

        }

        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);
    }
}
