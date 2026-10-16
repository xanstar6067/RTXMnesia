using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/04_wine_cellar_lab.hps: the acid machine and its chemical jars,
    /// the collapsing stairs and replacement platforms, the hole and cave creature events.
    /// Journal: docs/ScriptPort/ch01/04_wine_cellar_lab.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/04_wine_cellar_lab.map")]
    public class WineCellarLab : HplScript
    {
        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);

        ////////////////////////
        //BEGIN MOANING HORROR//
        /*Start the creak sounds + ps when entering basement, stop them when leaving the basement/area
         */
        void FuncMoanHorrors(string asParent, string asChild, int alState)
        {
            float fMoan = RandFloat(5.5f,15.5f);

            AddTimer("moanTimer", 4.5f+fMoan, nameof(FuncMoanTimer));

            AddTimer("childTimer", 0.5f+fMoan, nameof(FuncMoanTimer));

            /*DEBUG
             */
            AddDebugMessage("Begin moaning sounds", true);
        }
        /*Random placment of creak at random time
         */
        void FuncMoanTimer(string asTimer)
        {
            int iMoan = (int)RandFloat(2, 6);
            float fMoan = RandFloat(5.5f,15.5f);

            if(asTimer == "moanTimer") {
                PlaySoundAtEntity("moanSound"+iMoan, "scare_male_terrified.snt", "HorrorMoan_"+iMoan, 0.0f, false);

                AddTimer("moanTimer", 6.5f+fMoan, nameof(FuncMoanTimer));
            }
            else if(asTimer == "childTimer") {
                iMoan = (int)RandFloat(2, 6);

                PlaySoundAtEntity("childSound"+iMoan, "scare_baby_cry.snt", "HorrorMoan_"+iMoan, 0.0f, false);

                AddTimer("childTimer", 8.5f+fMoan, nameof(FuncMoanTimer));
            }

            /*DEBUG
             */
            AddDebugMessage("Now moaning in area: "+iMoan, false);
        }
        //END MOANING HORROR//
        //////////////////////


        ///////////////////////////////
        //BEGIN CREAKING WOOD CEILING//
        /*Start the creak sounds + ps when entering basement, stop them when leaving the basement/area
         */
        void FuncCreakHorrors(string asParent, string asChild, int alState)
        {
            if(asChild == "EnterCreakOnArea") {
                float fCreak = RandFloat(0.5f,6.5f);

                AddTimer("creak", 2.5f+fCreak, nameof(FuncCreakTimer));

                AddEntityCollideCallback("Player", "EnterCreakOffArea", nameof(FuncCreakHorrors), true, 1);

            } else {
                RemoveTimer("creak");

                AddEntityCollideCallback("Player", "EnterCreakOnArea", nameof(FuncCreakHorrors), true, 1);
            }

            /*DEBUG
             */
            AddDebugMessage("Player in/out creak: "+asChild, true);
        }
        /*Random placment of creak at random time
         */
        void FuncCreakTimer(string asTimer)
        {
            int iCreak = (int)RandFloat(1, 7);
            float fCreak = RandFloat(4.5f,14.5f);

            CreateParticleSystemAtEntity("creakPS"+iCreak, "ps_dust_falling_small_thin.ps", "HorrorCreak_"+iCreak, false);

            PlaySoundAtEntity("creakSound"+iCreak, "scare_wood_creak_mix.snt", "HorrorCreak_"+iCreak, 0.0f, false);

            AddTimer("creak", 5.5f+fCreak, nameof(FuncCreakTimer));

            /*DEBUG
             */
            AddDebugMessage("Now creaking in area: "+iCreak+" Next creak in: "+Str(5.5f+fCreak), true);
        }
        //END CREAKING WOOD CEILING//
        /////////////////////////////


        //////////////////////
        //BEGIN BREAK STAIRS//
        void CollideBreakStairs(string asParent, string asChild, int alState)
        {
            if(GetGlobalVarString("key_study_1") != "key_study_1") return;

            BreakMyStairs();
        }
        void BreakMyStairs()
        {
            if(GetLocalVarInt("StairsWentDownTheDrain") == 1) return;

            SetEntityActive("cave_in_1", false);
            SetEntityActive("cavedx_*", true);

            SetEntityActive("AreaQuestStairs", true);

            PlaySoundAtEntity("monster_break","04_break.snt", "AreaCaveMonster", 0, false);

            CreateParticleSystemAtEntity("stairsPS", "ps_dust_break_stair.ps", "AreaStairsParticle", false);

            PlaySoundAtEntity("rocks2", "04_rock_break.snt", "AreaCaveMonster", 1.0f, false);

            PlayMusic("04_event_stairs.ogg", false, 1.0f, 0.5f, 10, false);
            StartScreenShake(0.05f, 0.5f, 1.5f,1.0f);
            SetPlayerMoveSpeedMul(0.8f);
            SetPlayerRunSpeedMul(0.5f);
            MovePlayerHeadPos(0.0f, -0.4f, 0.0f, 0.2f, 0.1f);
            SetPropHealth("stairs_wood_1", 0);
            AddTimer("sanity", 1, nameof(TimerSanity));
            AddTimer("sanity2", 2.5f, nameof(TimerSanity));
            AddTimer("sanity3", 4, nameof(TimerSanity));
            AddTimer("sanity4", 5, nameof(TimerSanity));
            AddTimer("sanity5", 7, nameof(TimerSanity));

            SetEntityPlayerInteractCallback("PlatformDynStickLift_1", nameof(GiveStackHint), true);
            SetEntityPlayerInteractCallback("PlatformDynStickLift_2", nameof(GiveStackHint), true);

            SetEntityActive("StickLift_1", true);

            SetLocalVarInt("StairsWentDownTheDrain", 1);

            RemoveTimer("rockbreath");

            //Turn off some lights!
            SetLampLit("torch_static01_16", false, true);
            SetLampLit("torch_static01_13", false, true);
            SetLampLit("torch_static01_12", false, true);
            SetLampLit("torch_static01_14", false, true);
        }
        void TimerSanity(string asTimer)
        {
            if(asTimer == "sanity"){
                PlaySoundAtEntity("stairsS", "break_stairs.snt", "AreaStairsParticle", 0.5f, false);
                GiveSanityDamage(10, true);
                FadeSepiaColorTo(0.5f, 0.025f);
                FadeRadialBlurTo(0.1f, 0.025f);
                SetRadialBlurStartDist(0.2f);
                MovePlayerHeadPos(0.0f, 0, 0.0f, 1, 0.1f);
                SetPlayerMoveSpeedMul(0.6f);
                SetPlayerRunSpeedMul(0.4f);
            }
            else if(asTimer == "sanity2"){
                FadeImageTrailTo(1.0f, 0.1f);
                PlayGuiSound("react_scare", 0.8f);
                SetPlayerMoveSpeedMul(0.7f);
                SetPlayerRunSpeedMul(0.6f);
            }
            else if(asTimer == "sanity3"){
                PlaySoundAtEntity("rocks", "04_rock.snt", "AreaCaveMonster", 0.0f, false);
                SetPlayerMoveSpeedMul(0.8f);
                SetPlayerRunSpeedMul(0.7f);
            }
            else if(asTimer == "sanity4"){
                PlaySoundAtEntity("rocks3", "04_rock.snt", "AreaCaveMonster", 0.0f, false);
                PlaySoundAtEntity("monster_break","04_break02.snt", "AreaCaveMonster", 0, false);
                PlayGuiSound("react_breath", 0.7f);
                FadeSepiaColorTo(0, 0.1f);
                FadeRadialBlurTo(0, 0.1f);
                SetPlayerMoveSpeedMul(0.9f);
                SetPlayerRunSpeedMul(0.8f);
            }
            else if(asTimer == "sanity5"){
                FadeImageTrailTo(0.0f, 1.0f);
                PlayGuiSound("react_breath", 0.5f);
                SetPlayerMoveSpeedMul(1.0f);
                SetPlayerRunSpeedMul(1.0f);
            }
        }

        void GiveStackHint(string entity)
        {
            GiveHint("stackhint", "Hints", "StickyArea", 0);
        }

        /*Extra space to stick the one for the ladder area
         */
        void CollideStickLiftExtra(string asParent, string asChild, int alState)
        {
            string sArea = StringSub(asChild, 0, 11);

            AddDebugMessage("Name: " + sArea, false);

            if(GetLocalVarString("StickOneTime") == sArea) return;
            SetLocalVarString("StickOneTime", sArea);

            SetEntityActive(sArea, false);

            AddTimer(asParent+"_Body_1", 0.35f, nameof(TimerChangeEntity));
            AddTimer(sArea, 0.35f, nameof(TimerChangeEntity));
        }

        void CollideStickLift(string asArea, string asBody)
        {
            /*Only allow one board to be stuck to an area
             */
            if(GetLocalVarString("StickOneTime") == asArea) return;
            SetLocalVarString("StickOneTime", asArea);

            /*Remove object with grab and activate object with static and joint
             */
            AddTimer(asBody, 0.35f, nameof(TimerChangeEntity));
            AddTimer(asArea, 0.35f, nameof(TimerChangeEntity));
        }
        void TimerChangeEntity(string asTimer)
        {
            if(asTimer == "PlatformDynStickLift_1_Body_1") SetEntityActive("PlatformDynStickLift_1", false);
            else if(asTimer == "PlatformDynStickLift_2_Body_1") SetEntityActive("PlatformDynStickLift_2", false);

            if(asTimer == "StickLift_2") SetEntityActive("LadderArea_2", true);

            SetEntityActive("Platform"+asTimer, true);
        }

        void CollideQuestStairs(string asParent, string asChild, int alState)
        {
            AddQuest("04Stairs", "04Stairs");

            SetEntityActive("AreaQuestStairs_2", true);
        }
        void CollideQuestStairs02(string asParent, string asChild, int alState)
        {
            CompleteQuest("04Stairs", "04Stairs");
        }
        //END BREAK STAIRS//
        ////////////////////


        //////////////////////
        //BEGIN ACID MACHINE//
        /*Use jar on machine to place it under pipe
         */
        void ItemJarOnMachine(string asItem, string asEntity)
        {
            SetEntityActive("chemical_container_static_1", true);

            RemoveItem(asItem);

            PlaySoundAtEntity("PlaceJar","puzzle_place_jar", "AreaCompleteSuccess", 0, false);

            SetLocalVarInt("chemical_container_static_1", 1);
        }
        /*Use chemicals on machine or valves to add it to bottles
         */
        void AddChemical(string asItem, string asEntity)
        {
            RemoveItem(asItem);

            AddLocalVarInt("ChemicalsInMachine", 1);

            SetEntityActive("JarEmpty"+asItem, false);
            SetEntityActive("Jar"+asItem, true);

            PlaySoundAtEntity(asItem+"Sound","puzzle_add_chemical.snt", asEntity, 1, false);
            FadeLightTo("Light"+asItem, -1, -1, -1, -1, 0.5f, 0.1f);

            //Moved sanity boost to first instead of last, migth be better to boost early if player didn't get all 4.
            if(GetLocalVarInt("ChemicalsInMachine") == 1)
                GiveSanityBoostSmall();

            if(GetLocalVarInt("ChemicalsInMachine") == 3 )
                BreakMyStairs();

            if(GetLocalVarInt("ChemicalsInMachine") == 4){
                CompleteQuest("04FindChemicals","04FindChemicals");
                UnBlockHint("EntityWheel");
                for(int i=1;i<=4;i++)
                    SetEntityPlayerInteractCallback("Valve_"+i, nameof(InteractAcidMachine02), true);
                SetLocalVarInt("DoBigFeet", 0);
            }
        }
        /*When touching the vales
         */
        void InteractAcidMachine(string asEntity)
        {
            if(GetLocalVarInt("ChemicalsInMachine") != 4)
            {
                AddQuest("04FindChemicals","04FindChemicals");
                SetMessage("Ch01Level04", "InteractAcidMachineNoChem",-1);
            }
            else if(GetLocalVarInt("ChemicalsInMachine") == 4)
            {
                SetWheelStuckState("ValveIgnite", 0, false);
            }
        }
        void InteractAcidMachine02(string asEntity)
        {
            if(GetLocalVarInt("ValveIgniteFirst") == 0)
            {
                SetMessage("Ch01Level03", "MachineNeedsToStart", 0);
                SetLocalVarInt("ValveIgniteFirst", 1);
                for(int i=1;i<=4;i++)
                    SetEntityPlayerInteractCallback("Valve_"+i, nameof(InteractAcidMachine), false);
            }
        }
        /*Moving the big valve will turn on the burners and ready the machine
         */
        void InteractTurnOnAcidMachine(string asName, string asMainEntity, string asConnectEntity, int alState)
        {
            AddDebugMessage("Connect "+asMainEntity+" and "+asConnectEntity+" state:"+alState, false);

            if(alState == 1)
            {
                for(int i=1;i<=4;i++) {
                    CreateParticleSystemAtEntity(asName+"Fire"+i, "ps_fire_candle.ps", "Valve_"+i+"_AreaBottle", false);

                    FadeLightTo("Valve_"+i+"_Light", -1, -1, -1, -1, 0.4f, 1);

                    SetWheelStuckState("Valve_"+i, 0, true);
                }

                SetLocalVarInt("ValveIgniteFirst", 1);

                FadeLightTo("LightBurn", -1, -1, -1, -1, 3.0f, 2);

                PlaySoundAtEntity("IgniteSound", "general_fire_burning_low", "Valve_1_AreaBottle", 1, false);

                SetWheelStuckState("ValveIgnite", 1, true);
            }
            else if(alState == -1)
            {
                for(int i=1;i<=4;i++){
                    DestroyParticleSystem(asName+"Fire"+i);

                    FadeLightTo("Valve_"+i+"_Light", -1, -1, -1, -1, 0, 1);

                    SetWheelStuckState("Valve_"+i, -1, true);
                }

                FadeLightTo("LightBurn", -1, -1, -1, -1, 0, 2);

                StopSound("IgniteSound", 1);
            }
        }
        /*Turning the four valves on/off to try and get the sequence right
         */
        void InteractTurnValve(string asName, string asMainEntity, string asConnectEntity, int alState)
        {
            /*START WHAT HAPPENS WHEN VAVLES ARE TURNED ON
             */
            if(alState == 1) {

                 /*Mark Valve as on
                  */
                //SetLocalVarInt(asMainEntity, 1);
                AddLocalVarInt("ValvesNrOn", 1);

                AddDebugMessage("ValvesNrOn: "+GetLocalVarInt("ValvesNrOn")+" And Feet "+GetLocalVarInt("DoBigFeet"), false);

                if(GetLocalVarInt("ValvesNrOn") == 0){
                    SetWheelStuckState("ValveIgnite", -1, true);

                    PlaySoundAtEntity("FinalBoil","puzzle_boil.snt", "Valve_4_AreaValve", 1, false);

                    CreateParticleSystemAtEntity("FinalSteam", "ps_acid_machine_bubble_large02.ps", "Valve_4_AreaValve", false);
                    CreateParticleSystemAtEntity("FinalFlow", "ps_acid_machine_bubble_end.ps", "AreaCompleteSuccess", false);

                    FadeLightTo("LightBurn", -1, 1, -1, -1, 3.0f, 5);
                    FadeLightTo("LightAcid", -1, -1, -1, -1, 0.3f, 2);

                    AddTimer("done", 2, nameof(TimerAcidDone));

                } else DoEffectLarge(asName, asConnectEntity);

                /*Only an event, nothing puzzle related
                 */
                if(GetLocalVarInt("ValvesNrOn") == -2 && GetLocalVarInt("DoBigFeet") == 0){
                    AddTimer("Feet_1", 2.5f, nameof(TimerBigFeet));
                    PlaySoundAtEntity("bang","general_thunder.snt", "Player", 0, false);
                    PlaySoundAtEntity("biggus","04_big_feet.snt", "Player", 0, false);
                    SetLocalVarInt("DoBigFeet", 1);
                }

                /*If three vavles on, reset the machine
                 */
                /*if((GetLocalVarInt("Valve_1")+GetLocalVarInt("Valve_2")+GetLocalVarInt("Valve_3")+GetLocalVarInt("Valve_4")) > 2 ) {
                    for(int i=1;i<=4;i++) SetWheelStuckState("Valve_"+i, -1, true);

                    AddTimer("release", 0.1f, "TimerReleaseValves");
                    return;
                }*/

                /*If the valve turned on is the correct order move ahead, if not hint of wrong order
                 */
                /*if(asMainEntity == "Valve_1"){
                    if(GetLocalVarInt("RoadToSuccess") == 0) DoEffectLarge(asName, asConnectEntity);
                    else DoEffectSmall(asName, asConnectEntity);
                }
                else if(asMainEntity == "Valve_2"){
                    if(GetLocalVarInt("RoadToSuccess") == 1) DoEffectLarge(asName, asConnectEntity);
                    else DoEffectSmall(asName, asConnectEntity);
                }
                else if(asMainEntity == "Valve_3"){
                    if(GetLocalVarInt("RoadToSuccess") == 3){
                        DoEffectLarge(asName, asConnectEntity);

                        //This has nothing to do with puzzle, it's a pure event in level triggered by how far puzzle completed
                        if(GetLocalVarInt("DoBigFeet") != 1){ AddTimer("Feet_1", 2.5f, "TimerBigFeet"); PlaySoundAtEntity("bang","general_thunder.snt", "Player", 0, false);
                            PlaySoundAtEntity("biggus","04_big_feet.snt", "Player", 0, false); SetLocalVarInt("DoBigFeet", 1);
                        }
                    }
                    else DoEffectSmall(asName, asConnectEntity);
                }
                else if(asMainEntity == "Valve_4"){
                    if(GetLocalVarInt("RoadToSuccess") == 5) DoEffectLarge(asName, asConnectEntity);
                    else DoEffectSmall(asName, asConnectEntity);
                }*/
            }

            /*START WHAT HAPPENS WHEN VAVLES ARE TURNED OFF
             */
            else if(alState == -1){

                 /*Mark valve as off
                  */
                //SetLocalVarInt(asMainEntity, 0);

                AddLocalVarInt("ValvesNrOn", -1);

                DestroyParticleSystem(asName+"PSteam");
                StopSound(asName+"SBoil",1); StopSound(asName+"SSteam",1);

                /*If valved turned off in the right order, allow for attempt at turning the next valve in the correct order
                 */
                /*if(asMainEntity == "Valve_1" && GetLocalVarInt("RoadToSuccess") == 2) AddLocalVarInt("RoadToSuccess", 1);

                else if(asMainEntity == "Valve_2" && GetLocalVarInt("RoadToSuccess") == 4) AddLocalVarInt("RoadToSuccess", 1);

                else if(asMainEntity == "Valve_3" && GetLocalVarInt("RoadToSuccess") == 6){ //Full sequence correct, spit out the chemical substance
                    SetWheelStuckState("ValveIgnite", -1, true);

                    PlaySoundAtEntity("FinalBoil","puzzle_boil.snt", "Valve_4_AreaValve", 1, false);

                    CreateParticleSystemAtEntity("FinalSteam", "ps_acid_machine_bubble_large02.ps", "Valve_4_AreaValve", false);
                    CreateParticleSystemAtEntity("FinalFlow", "ps_acid_machine_bubble_end.ps", "AreaCompleteSuccess", false);

                    FadeLightTo("LightBurn", -1, 1, -1, -1, 3.0f, 5);
                    FadeLightTo("LightAcid", -1, -1, -1, -1, 0.3f, 2);

                    AddTimer("done", 2, "TimerAcidDone");
                }
                else if(asMainEntity == "Valve_4" && GetLocalVarInt("RoadToSuccess") == 6) AddLocalVarInt("RoadToSuccess", -1);*/

                /*If all valves are off, reset the machine
                 */
                /*if((GetLocalVarInt("Valve_1")+GetLocalVarInt("Valve_2")+GetLocalVarInt("Valve_3")+GetLocalVarInt("Valve_4")) == 0 )
                    SetLocalVarInt("RoadToSuccess", 0);*/
            }

            AddDebugMessage("Rate of success "+GetLocalVarInt("RoadToSuccess"), false);
            AddDebugMessage(asMainEntity+" is turned to "+alState, true);
        }
        /*Large effects as correct valve rotated
         */
        void DoEffectLarge(string asName, string asWhere)
        {
            CreateParticleSystemAtEntity(asName+"PSteam", "ps_acid_machine_bubble_large.ps", asWhere, false);

            PlaySoundAtEntity(asName+"SBoil","puzzle_boil_low.snt", asWhere, 1, false);
            PlaySoundAtEntity(asName+"SSteam","puzzle_gas.snt", asWhere, 1, false);

            AddLocalVarInt("RoadToSuccess", 1);
        }
        /*Small effects as incorrect valve rotated
         */
        void DoEffectSmall(string asName, string asWhere)
        {
            CreateParticleSystemAtEntity(asName+"PSteam", "ps_acid_machine_bubble_small.ps", asWhere, false);

            PlaySoundAtEntity(asName+"SBoil","puzzle_boil_low.snt", asWhere, 1, false);
        }
        /*When reseting machine on more than 2 valves turned, this timer turns it on again
         */
        void TimerReleaseValves(string asTimer)
        {
            for(int i=1;i<=4;i++) SetWheelStuckState("Valve_"+i, 0, false);

            SetLocalVarInt("RoadToSuccess", 0);
        }
        /*The acid spit on success, if no jar present the acid will just spill and it is possible to try again
         */
        void TimerAcidDone(string asTimer)
        {
            DestroyParticleSystem("Part4PSteam");
            DestroyParticleSystem("FinalSteam");

            PlaySoundAtEntity("AcidDone","puzzle_acid", "AreaCompleteSuccess", 0, false);

            StopSound("FinalBoil",1);

            FadeLightTo("LightAcid", -1, -1, -1, -1, 0, 4);
            FadeLightTo("LightBurn", -1, 0.3f, -1, -1, 0, 3);

            SetWheelStuckState("ValveIgnite", 0, true);

            if(GetLocalVarInt("chemical_container_static_1") == 1){
                SetEntityActive("chemical_container_static_1", false);
                SetEntityActive("chemical_container_2", true);
                //SetPropActiveAndFade("chemical_container_static_1", false, 0.5f);
                //SetPropActiveAndFade("chemical_container_2", true, 0.5f);

                PlaySoundAtEntity("AcidSuccess","puzzle_acid_success", "AreaCompleteSuccess", 0, false);
                PlayMusic("04_puzzle_acid.ogg", false, 0.7f, 0.5f, 10, false);
                //GiveSanityBoostSmall();

                SetWheelStuckState("ValveIgnite", -1, true);

            }
            else {
                PlaySoundAtEntity("AcidFail","puzzle_acid_fail", "AreaCompleteSuccess", 0, false);
                SetMessage("Ch03Level26", "NoContainerBelowSqueezer", 0);
            }
        }
        void PickFinalAcid(string asEntity, string asType)
        {
            GiveSanityBoostSmall();

            SetGlobalVarString(asEntity, asEntity);

            AddTimer("Thunder", 0.75f, nameof(TimerEnterClank));
        }
        void EntityCallPickEmptyChem(string asEntity, string type)
        {
            GiveSanityBoostSmall();
        }

        void EntityCallPickNote(string asEntity, string type)
        {
            AddQuest("04FindChemicals","04FindChemicals");
        }
        void EntityCallPickNote02(string asEntity, string type)
        {
            AddQuest("04ChemicalsMoved","04ChemicalsMoved");
        }
        //END ACID MACHINE//
        ////////////////////


        ///////////////////////////
        //BEGIN LOOKING DOWN HOLE//
        void CollideActiveHole(string asParent, string asChild, int alState)
        {
            SetEntityActive("AreaLookHole", true);
        }
        void LookAtHole(string entity, int alState)
        {
            if(alState != 1) return;

            PlayMusic("04_event_hole.ogg", false, 0.7f, 0.5f, 10, false);

            AddTimer("TimerLookHole", 0.1f, nameof(TimerLookHole));
        }
        void TimerLookHole(string asTimer)
        {
            /*Configurables
             */
            int iMaxEventStep = 12;		//How many steps there are in the switch event
            float fEventSpeed = 1.0f;	//The default time between steps in an event

            /*Helpers - Do not edit
             */
            string sEvent = asTimer;	//Using first timer name for variable, timer name & callback for the timer that loops
            AddLocalVarInt(sEvent, 1);	//What step to play in the event

            /*Steps in the event
             */
            switch(GetLocalVarInt(sEvent)){
                case 1:
                    PlaySoundAtEntity("HoleCry","04_hole_squeal", "AreaHoleEffects", 0, false);
                    StartScreenShake(0.004f, 6, 2, 2);
                    GiveSanityDamage(10.0f, true);
                break;
                case 2:
                    PlaySoundAtEntity("HoleWater","04_water_puff.snt", "AreaHoleEffects", 0, false);
                    CreateParticleSystemAtEntity("HolePS1", "waterlurker_run_splash.ps", "AreaHoleEffects", false);
                    fEventSpeed = 0.25f;
                break;
                case 3:
                    PlaySoundAtEntity("breath4","react_breath", "Player", 0.25f, false);
                    PlaySoundAtEntity("HoleWater2","04_water_puff.snt", "AreaHoleEffects", 0, false);
                    fEventSpeed = 0.5f;
                break;
                case 4:
                    PlaySoundAtEntity("HoleWater3","04_water_puff.snt", "AreaHoleEffects", 0, false);
                    CreateParticleSystemAtEntity("HolePS2", "waterlurker_run_splash.ps", "AreaHoleEffects", false);
                break;
                case 5:
                    PlaySoundAtEntity("HoleWater4","04_water_puff.snt", "AreaHoleEffects", 0, false);
                    fEventSpeed = 0.5f;
                break;
                case 6:
                    PlaySoundAtEntity("breath1","react_breath", "Player", 0.4f, false);
                    PlaySoundAtEntity("HoleWater5","04_water_puff.snt", "AreaHoleEffects", 0.4f, false);
                    CreateParticleSystemAtEntity("HolePS3", "waterlurker_run_splash.ps", "AreaHoleEffects", false);
                    fEventSpeed = 0.25f;
                break;
                case 7:
                    PlaySoundAtEntity("HoleWater6","04_water_puff.snt", "AreaHoleEffects", 0.75f, false);
                    fEventSpeed = 0.75f;
                break;
                case 8:
                    PlaySoundAtEntity("HoleWater7","04_water_puff.snt", "AreaHoleEffects", 1, false);
                break;
                case 9:
                    PlaySoundAtEntity("breath3","react_breath", "Player", 0.6f, false);
                    PlaySoundAtEntity("HoleWater8","04_water_puff.snt", "AreaHoleEffects", 0.4f, false);
                    CreateParticleSystemAtEntity("HolePS3", "waterlurker_run_splash.ps", "AreaHoleEffects", false);
                    fEventSpeed = 0.25f;
                break;
                case 10:
                    PlaySoundAtEntity("HoleWater9","04_water_puff.snt", "AreaHoleEffects", 0.75f, false);
                    fEventSpeed = 0.75f;
                break;
                case 11:
                    PlaySoundAtEntity("HoleWater10","04_water_puff.snt", "AreaHoleEffects", 1, false);
                break;
                case 12:
                    PlaySoundAtEntity("breath2","react_breath", "Player", 1, false);
                break;
            }

            if(GetLocalVarInt(sEvent) <= iMaxEventStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }
        //END LOOKING DOWN HOLE//
        /////////////////////////



        ///////////////////////
        //BEGIN SPIDER CAVEIN//
        void CollideActivateSpider(string asParent, string asChild, int alState)
        {
            SetEntityActive("AreaBeginSpider_1", true);
            AddDebugMessage("In Area 1", false);

            SetLocalVarFloat("RockBreath", 2.0f);

            AddTimer("rockbreath", 0.2f, nameof(TimerRockBreath));
        }
        void TimerRockBreath(string asTimer)
        {
            if(GetLocalVarFloat("RockBreath") > 0.5f){
                AddLocalVarFloat("RockBreath", -0.2f);
                AddLocalVarFloat("RockShake", 0.001f);
            }

            PlaySoundAtEntity("monster_breath","04_breath.snt", "AreaCaveMonster", GetLocalVarFloat("RockBreath"), false);

            StartScreenShake(GetLocalVarFloat("RockShake"), 0.5f, 2.0f, 1.0f);
            CreateParticleSystemAtEntity("breathps", "ps_cave_monster_breath", "AreaCaveMonster", false);

            AddTimer("rockbreath", 5, nameof(TimerRockBreath));

            AddDebugMessage("Value: "+Str(GetLocalVarFloat("RockBreath")), false);
        }
        void LookAtCave(string asEntity, int alState)
        {
            PlaySoundAtEntity("monster_scream","04_warn.snt", "AreaCaveMonster", 0, false);

            StartScreenShake(0.007f, 2.0f, 0.5f, 2.0f);

            CreateParticleSystemAtEntity("breathps", "ps_cave_monster_warn.ps", "AreaCaveMonster", false);

            AddTimer("monster1", 0.5f, nameof(TimerRockMonster));
            AddTimer("monster2", 2.0f, nameof(TimerRockMonster));
            AddTimer("monster3", 3.0f, nameof(TimerRockMonster));
            AddTimer("monster4", 5.0f, nameof(TimerRockMonster));
        }
        void TimerRockMonster(string asTimer)
        {
            if(asTimer == "monster1"){
                StartPlayerLookAt("AreaCaveMonster", 1.0f, 1.0f, "");
                PlayGuiSound("react_scare", 0.6f);
                GiveSanityDamage(5.0f, false);
                FadeRadialBlurTo(0.05f, 0.025f);
                SetRadialBlurStartDist(0.1f);
            }
            else if(asTimer == "monster2"){
                StopPlayerLookAt();
            }
            else if(asTimer == "monster3"){
                PlayGuiSound("react_creath", 0.7f);
                FadeRadialBlurTo(0, 0.1f);
            }
            else{
                PlayGuiSound("react_creath", 0.5f);
            }
        }
        void CollideScreamCave(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("monster_scream","04_scream.snt", "AreaCaveMonster", 0, false);

            StartScreenShake(0.02f, 2.0f, 0.5f, 2.0f);

            CreateParticleSystemAtEntity("breathps", "ps_cave_monster_scream.ps", "AreaCaveMonster", false);

            AddTimer("scream1", 0.5f, nameof(TimerCaveScream));
            AddTimer("scream2", 1.0f, nameof(TimerCaveScream));
            AddTimer("scream3", 3.0f, nameof(TimerCaveScream));
            AddTimer("scream4", 5.0f, nameof(TimerCaveScream));
        }
        void TimerCaveScream(string asTimer)
        {
            if(asTimer == "scream1"){
                PlayGuiSound("react_scare", 0.8f);
                GiveSanityDamage(10.0f, false);
                FadeSepiaColorTo(0.5f, 0.025f);
                FadeRadialBlurTo(0.1f, 0.025f);
                SetRadialBlurStartDist(0.2f);
            }
            else if(asTimer == "scream2"){

            }
            else if(asTimer == "scream3"){
                PlayGuiSound("react_creath", 0.8f);
                FadeSepiaColorTo(0, 0.1f);
                FadeRadialBlurTo(0, 0.1f);
            }
            else{
                PlayGuiSound("react_creath", 0.6f);
            }
        }
        //END SPIDER CAVEIN//
        /////////////////////


        //////////////////
        //BEGIN BIG FEET//
        void TimerBigFeet(string asTimer)
        {
            if(asTimer == "Feet_1"){
                AddTimer("Feet_2", 1, nameof(TimerBigFeet));
                AddTimer("Feet_3", 2.5f, nameof(TimerBigFeet));
                AddTimer("Feet_4", 4, nameof(TimerBigFeet));
                AddTimer("Feet_5", 5, nameof(TimerBigFeet));

                DoSteps(0.007f,1.0f,0.75f,0.3f,asTimer);
            }
            else if(asTimer == "Feet_2"){
                GiveSanityDamage(10.0f, true);
                DoSteps(0.008f,0.3f,0.8f,0.3f,asTimer);
            }
            else if(asTimer == "Feet_3") DoSteps(0.01f,0.5f,0.9f,0.4f,asTimer);
            else if(asTimer == "Feet_4") DoSteps(0.008f,0.7f,0.7f,0.3f,asTimer);
            else if(asTimer == "Feet_5") DoSteps(0.007f,1.0f,1.5f,3.0f,asTimer);
        }
        void DoSteps(float fShake, float fFade, float fShakeL, float fShakeF, string asWhere)
        {
            StartScreenShake(fShake, fShakeL, 0.15f, fShakeF);
            PlaySoundAtEntity("sigh"+asWhere,"react_breath.snt", "Player", fFade, false);
            CreateParticleSystemAtEntity("step"+asWhere, "ps_dust_falling_big_feet.ps", "Area"+asWhere, false);
        }
        //END BIG FEET//
        ////////////////


        //////////////////////////////
        //BEGIN ENTER LEVEL CLANKS//
        void TimerEnterClank(string asTimer)
        {
            if(asTimer == "Clank_1"){
                PlaySoundAtEntity("clanks","04_enter_clank.snt", "HorrorCreak_6", 0.5f, false);
                FadeImageTrailTo(1.5f, 2);

                AddTimer("Clank_2", 0.75f, nameof(TimerEnterClank));
                AddTimer("Clank_3", 2.25f, nameof(TimerEnterClank));
            }
            else if(asTimer == "Clank_2"){
                PlaySoundAtEntity("beath1","react_scare.snt", "Player", 0.75f, false);
                GiveSanityDamage(10, true);
            }
            else if(asTimer == "Clank_3"){
                PlaySoundAtEntity("beath2","react_scare.snt", "Player", 1.25f, false);
                FadeImageTrailTo(0.0f, 2);
            }

            else if(asTimer == "Thunder"){
                PlaySoundAtEntity("thunder","general_thunder.snt", "Player", 0, false);
            }

        }
        //END ENTER LEVEL CLANKS//
        //////////////////////////


        void EntityCallLamp(string asEntity, string type)
        {
            AddLocalVarFloat("AmbLight", 0.1f);
            float fLight = GetLocalVarFloat("AmbLight");

            FadeLightTo("LightBurn_1", 0.15f+fLight, 0.125f+fLight, 0.1f+fLight, -1, -1, 1.0f);
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("WineCellarLab");

            //----COLLIDE CALLBACKS----//
            AddEntityCollideCallback("Player", "EnterCreakOnArea", nameof(FuncCreakHorrors), true, 1);
            AddEntityCollideCallback("Player", "AreaBreakStairs", nameof(CollideBreakStairs), false, 1);
            AddEntityCollideCallback("Player", "AreaLookHoleActive", nameof(CollideActiveHole), true, 1);
            AddEntityCollideCallback("Player", "AreaBeginSpider", nameof(CollideActivateSpider), true, 1);
            AddEntityCollideCallback("Player", "AreaQuestStairs", nameof(CollideQuestStairs), true, 1);
            AddEntityCollideCallback("Player", "AreaQuestStairs_2", nameof(CollideQuestStairs02), true, 1);
            AddEntityCollideCallback("Player", "AreaScreamCave", nameof(CollideScreamCave), true, 1);

            AddEntityCollideCallback("PlatformDynStickLift_1", "StickLift_2_Extra", nameof(CollideStickLiftExtra), true, 1);
            AddEntityCollideCallback("PlatformDynStickLift_2", "StickLift_2_Extra", nameof(CollideStickLiftExtra), true, 1);


            //----ENTITY INIT----//
            for(int i=1;i<=4;i++) {
                ConnectEntities("Part_"+i, "Valve_"+i, "Valve_"+i+"_AreaValve", false, 0, nameof(InteractTurnValve));

                SetWheelStuckState("Valve_"+i, -1, false);
            }
            ConnectEntities("Ignite", "ValveIgnite", "Valve_1_AreaBottle", false, 0, nameof(InteractTurnOnAcidMachine));

            SetEntityActive("chemical_container_static_1", false);
            SetEntityActive("chemical_container_2", false);

            SetWheelStuckState("ValveIgnite", -1, false);

            AddUseItemCallback("placebottle", "chemical_container_1", "AreaUseMachine", nameof(ItemJarOnMachine), true);
            AddUseItemCallback("placebottle2", "chemical_container_1", "AreaPlaceJar", nameof(ItemJarOnMachine), true);
            AddUseItemCallback("placebottle3", "chemical_container_1", "ValveIgnite", nameof(ItemJarOnMachine), true);

            for(int i=1;i<=4;i++) {
                AddUseItemCallback("placebottlev"+i, "chemical_container_1", "Valve_"+i, nameof(ItemJarOnMachine), true);
                AddUseItemCallback("Chemicalvi_"+i, "Chemical_"+i, "ValveIgnite", nameof(AddChemical), true);
                AddUseItemCallback("Chemicala_"+i, "Chemical_"+i, "AreaUseMachine", nameof(AddChemical), true);
                AddUseItemCallback("Chemicalm_"+i, "Chemical_"+i, "acid_machine_1", nameof(AddChemical), true);

                for(int j=1;j<=4;j++)
                    AddUseItemCallback("Chemicalv_"+i, "Chemical_"+i, "Valve_"+j, nameof(AddChemical), true);
            }

            SetEntityPlayerInteractCallback("ValveIgnite", nameof(InteractAcidMachine), false);
            for(int i=1;i<=4;i++) SetEntityPlayerInteractCallback("Valve_"+i, nameof(InteractAcidMachine), false);

            SetEntityPlayerLookAtCallback("AreaLookHole", nameof(LookAtHole), true);

            BlockHint("EntityWheel");

            //----QUEST INIT----//
            SetNumberOfQuestsInMap(1);

            FuncMoanHorrors("", "AreaBeginChild", 1);

            SetGlobalVarInt("BeenToLab", 1);

            if(ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                GiveItemFromFile("Chemical_1", "flask01_aqua_regia.ent");
                GiveItemFromFile("Chemical_2", "flask01_calamine.ent");
                GiveItemFromFile("Chemical_3", "flask01_cuprite.ent");
                GiveItemFromFile("Chemical_4", "flask01_orpiment.ent");

                for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

                //SetWheelStuckState("ValveIgnite", 0, false);	//Normally stuck until all 4 chemicals in machine
            }

            SetFogProperties(4, 16, 1, true);
            SetFogColor(0.12f, 0.14f, 0.18f, 0.7f);
            SetFogActive(true);
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            //----PRELOADING----//
            PreloadSound("scare_male_terrified"); PreloadSound("scare_baby_cry"); PreloadSound("scare_wood_creak_mix"); PreloadSound("break_stairs");
            PreloadSound("puzzle_place_jar"); PreloadSound("puzzle_add_chemical"); PreloadSound("general_fire_burning_low"); PreloadSound("general_thunder");
            PreloadSound("04_big_feet"); PreloadSound("puzzle_boil"); PreloadSound("puzzle_gas"); PreloadSound("puzzle_boil_low");
            PreloadSound("puzzle_acid"); PreloadSound("puzzle_acid_success"); PreloadSound("puzzle_acid_fail"); PreloadSound("04_hole_squeal");
            PreloadSound("04_water_puff"); PreloadSound("react_breath"); PreloadSound("01_tiny2"); PreloadSound("spider_die");
            PreloadSound("04_enter_clank"); PreloadSound("react_scare");

            PreloadParticleSystem("ps_dust_falling_small_thin"); PreloadParticleSystem("ps_dust_break_stair"); PreloadParticleSystem("ps_acid_machine_bubble_large02");
            PreloadParticleSystem("ps_acid_machine_bubble_end"); PreloadParticleSystem("ps_acid_machine_bubble_large"); PreloadParticleSystem("ps_acid_machine_bubble_small");
            PreloadParticleSystem("waterlurker_walk_splash"); PreloadParticleSystem("ps_dust_falling_big_feet"); PreloadParticleSystem("ps_dust_falling_small_thin");

            SetWheelStuckState("ValveIgnite", -1, false);

            //----AUDIO----//
            PlayMusic("18_amb", true, 1, 5, 0, true);

            //---- TIMER INIT ----//
            if(GetLocalVarInt("PlayIntro") == 0){
                SetLocalVarInt("PlayIntro", 1);

                AutoSave();

                AddTimer("Clank_1", 0.25f, nameof(TimerEnterClank));
            }
            else AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {

        }
    }
}
