using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/06_distillery.hps: the blocked pulley and heavy hatch,
    /// cellar voices, door scares, howls, roses and the grunt glimpse.
    /// Journal: docs/ScriptPort/ch01/06_distillery.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/06_distillery.map")]
    public class Distillery : HplScript
    {
        ////////////////////////
        //BEGIN MOANING HORROR//
        /*Start the creak sounds + ps when entering basement, stop them when leaving the basement/area
         */
        void FuncMoanHorrors(string asParent, string asChild, int alState)
        {
            if(asChild == "AreaBeginAnimal") {
                AddTimer("animalTimer", 2.5f, nameof(FuncMoanTimer));

                AddTimer("scareSoundx1", 1.0f, nameof(TimerStopScareSound));
                AddTimer("scareSoundx", 4.0f, nameof(TimerStopScareSound));

                PlaySoundAtEntity("scareSoundx", "scare_whine_loop.snt", "HorrorMoan_8", 2.0f, false);
                StartScreenShake(0.008f, 0.0f, 2.0f, 4.0f);
                PlaySoundAtEntity("scareSoundx2", "guardian_idle.snt", "HorrorMoan_8", 2.0f, false);
                FadeImageTrailTo(1.0f, 0.01f);

                /*DEBUG
                 */
                AddDebugMessage("Begin animal sounds", true);

                //return;
            }

            /*DEBUG
             */
            AddDebugMessage("Begin moaning sounds", true);
        }
        void TimerStopScareSound(string asTimer)
        {
            if(asTimer == "scareSoundx1")
                GiveSanityDamage(10.0f, true);
            else{
                FadeImageTrailTo(0.0f, 0.1f);
                StopSound(asTimer, 4.0f);
                StopSound(asTimer+"2", 4.0f);
            }
        }
        /*Random placment of creak at random time
         */
        void FuncMoanTimer(string asTimer)
        {


            int iMoan = (int)RandFloat(7, 14);
            float fMoan = RandFloat(5.5f,15.5f);

            if(asTimer == "moanTimer") {
                PlaySoundAtEntity("moanSound"+iMoan, "scare_male_terrified.snt", "HorrorMoan_"+iMoan, 0.0f, false);

                AddTimer("moanTimer", 6.5f+fMoan, nameof(FuncMoanTimer));
            } else if(asTimer == "childTimer") {
                iMoan = (int)RandFloat(7, 14);

                PlaySoundAtEntity("childSound"+iMoan, "scare_baby_cry.snt", "HorrorMoan_"+iMoan, 0.0f, false);

                AddTimer("childTimer", 8.5f+fMoan, nameof(FuncMoanTimer));
            } else if(asTimer == "animalTimer") {
                iMoan = (int)RandFloat(7, 14);

                PlaySoundAtEntity("animalSound"+iMoan, "scare_animal_squeal.snt", "HorrorMoan_"+iMoan, 0.0f, false);

                AddTimer("animalTimer", 5.5f+fMoan, nameof(FuncMoanTimer));
            }

            /*DEBUG
             */
            AddDebugMessage("Now moaning in area: "+iMoan, false);
        }
        //END MOANING HORROR//
        //////////////////////


        ///////////////////////////////
        //BEGIN CREAKING WOOD CEILING//
        /*Random placment of creak at random time
         */
        void FuncCreakTimer(string asTimer)
        {
            int iCreak = (int)RandFloat(8, 13);
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


        ///////////////////////////////
        //BEGIN BARELL BLCOKING DOOR

        void InteractBarrelBlock(string asEntity)
        {
            SetSwingDoorLocked("cellar_wood01_1", false, false);

            SetEntityPlayerInteractCallback("barrel01_12", "", true);
            SetEntityPlayerInteractCallback("barrel01_13", "", true);
        }

        void CollideBlockingBarellsArea(string asParent, string asChild, int alState)
        {
            return; //disabled, doing on interact instead.
            SetSwingDoorLocked("cellar_wood01_1", false, false);
        }


        //END BARELL BLCOKING DOOR
        ///////////////////////////////

        ///////////////////////////////
        //BEGIN BREAK PULLEY BLOCKER
        void InteractCrank(string asEntity)
        {
            SetMessage("Ch01Level06", "CrankStuck", 0);
            AddQuest("06BlockedPulley", "06BlockedPulley");

            CreateParticleSystemAtEntity("smoke", "ps_dust_falling_small.ps", "AreaPulleysmoke", false);

            AddTimer(asEntity, 1.5f, nameof(TimerInteractCrank));
        }
        void TimerInteractCrank(string asTimer)
        {
            SetEntityPlayerInteractCallback(asTimer, nameof(InteractCrank), true);
        }

        void BreakPulleyBlocker(string asEntity, string asType)
        {
            if(asType == "Break")
            {
                GiveSanityBoostSmall();

                SetEntityActive("AreaInPulley", false);

                SetWheelStuckState("crank_iron_1", 0, true);

                RemoveTimer("crank_iron_1");
                SetEntityPlayerInteractCallback("crank_iron_1", "", false);

                CompleteQuest("06BlockedPulley", "06BlockedPulley");

                FadeLightTo("PointLight_38", 0,0,0,0, -1, 2);
                FadeLightTo("PointLight_39", 0,0,0,0, -1, 2);
            }
        }

        /*Message and effect to hint about hatch wheight.
         */
        void InteractHatch(string asEntity)
        {
            SetMessage("Ch01Level06", "HatchHeavy", 0);
            PlaySoundAtEntity("hatchs", "door_level_wood_close", "hatch_large_1", 0.0f, false);
            AddPropImpulse("hatch_large_1", 0.0f, 10.0f, 0.0f, "World");

            AddTimer("hatchs", 0.15f, nameof(TimerHatchOn));
            AddTimer("hatchon", 1.0f, nameof(TimerHatchOn));
        }
        void TimerHatchOn(string asTimer)
        {
            if(asTimer == "hatchs"){
                StopSound("hatchs", 0.5f);
                AddPropImpulse("hatch_large_1", 0.0f, 10.0f, 0.0f, "World");
            }

            else
                SetEntityPlayerInteractCallback("hatch_large_1", nameof(InteractHatch), true);
        }

        /*If player goes out of area pulley bloc break count back to 0 + hint message
         */
        void CollideInPulley(string asParent, string asChild, int alState)
        {
            if(alState == 1){
                AddDebugMessage("Player in area!", false);
            }
            else{
                SetLocalVarInt("PulleyBreak", 0);

                if(GetLocalVarInt("HoldPulley") == 1)
                    SetMessage("Ch01Level06", "PulleyBlockSlip", 0);

                SetLocalVarInt("HoldPulley", 0);

                AddDebugMessage("Player out of area!", false);
            }
        }

        /*Break pully block when moved in/out of area 3 times
         */
        void CollidePulleyBlock(string asParent, string asChild, int alState)
        {
            AddLocalVarInt("PulleyBreak", 1);

            if(GetLocalVarInt("PulleyBreak") == 3)
            {
                SetPropHealth(asParent, 0);

                //Stop player input for half a second, otherwise it is easy for view to fly off! (player sweeps with mouse)
                SetPlayerActive(false);
                AddTimer("playeractive", 0.3f, nameof(TimerMakePlayerActive));
            }

            AddDebugMessage("Pulley in/out area!", false);
        }

        void TimerMakePlayerActive(string asTimer)
        {
            SetPlayerActive(true);
        }

        /*If player holds pulleyblock and leaves area, set var to display hint message
        */
        void InteractPulley(string asEntity)
        {
            SetLocalVarInt("HoldPulley", 1);
        }
        //END BREAK PULLEY BLOCKER
        ///////////////////////////////

        ///////////////////////////////
        //BEGIN EMPRY STORAGE DOOR OPENS

        void EnterEmptyStorageDoorOpen(string asParent, string asChild, int alState)
        {
            if(GetSwingDoorClosed("cellar_wood01_5")==false) return;

            SetSwingDoorDisableAutoClose("cellar_wood01_5", true);
            SetSwingDoorClosed("cellar_wood01_5", false, false);

            RemoveEntityCollideCallback(asParent, asChild);

            PlaySoundAtEntity("enemy", "guardian_idle", "cellar_wood01_5", 0, false);
            PlaySoundAtEntity("doorsqueak", "01_door", "cellar_wood01_5", 0.3f, false);
            GiveSanityDamage(15, true);

            AddBodyImpulse("cellar_wood01_5_Body_1", 0,0,1.0f, "Local");
            for(int i=0; i<30; ++i)
            {
                AddTimer("cellar_wood01_5_Body_1", i * 0.1f, nameof(TimerOpenDoor));
            }

            AddTimer("react_scare", 0.7f, nameof(TimerStopLookAt));
            AddTimer("react_breath", 2.5f, nameof(TimerStopLookAt));
            AddTimer("react_breath", 4.5f, nameof(TimerStopLookAt));

            AddDebugMessage("Empty storage event", false);
        }

        void TimerOpenDoor(string asTimer)
        {
            AddBodyForce(asTimer, 0,0,40, "Local");
        }


        //BEGIN END STORAGE DOOR OPENS
        ///////////////////////////////



        ///////////////////////////////
        //BEGIN BOTTLE ROOM SCRATCHES

        void EnterBottleRoomWallScratch(string asParent, string asChild, int alState)
        {
            SetSwingDoorClosed("cellar_wood01_3", true, true);

            PlaySoundAtEntity("enemy", "enemy/grunt/amb_idle",  "cellar_wood01_3", 0, false);

            GiveSanityDamage(10, true);

            AddTimer("impact_wood_heavy_high.snt", 1.5f, nameof(TimerWallScratchSound));
            AddTimer("impact_wood_heavy_high.snt", 2.5f, nameof(TimerWallScratchSound));
            AddTimer("impact_wood_heavy_high.snt", 3.2f, nameof(TimerWallScratchSound));
            AddTimer("enemy", 3.0f, nameof(TimerWallScratchSound));

            AddTimer("react_scare", 0.5f, nameof(TimerStopLookAt));
            AddTimer("react_breath", 2.5f, nameof(TimerStopLookAt));

            AddTimer("NextDoor", 6.5f, nameof(TimerWallScratchNextDoor));
        }



        void TimerWallScratchNextDoor(string asTimer)
        {
            PlaySoundAtEntity("break","break_wood.snt", "cellar_wood01_6", 0, false);
            SetSwingDoorClosed("cellar_wood01_6", true, true);

            AddTimer("react_scare", 0.5f, nameof(TimerStopLookAt));
            AddTimer("react_breath", 2.5f, nameof(TimerStopLookAt));
        }

        void TimerWallScratchSound(string asTimer)
        {
            PlaySoundAtEntity("enemy", asTimer,  "cellar_wood01_3", 0, false);

            if(asTimer == "enemy")
                StopSound("enemy", 1.0f);
        }

        //BEGIN BOTTLE ROOM SCRATCHES
        ///////////////////////////////


        ///////////////////////////////
        //BEGIN HOWLING

        void EnterHowlArea(string asParent, string asChild, int alState)
        {
            int lIdx = GetLocalVarInt("HowlCount") + 1;
            PlaySoundAtEntity("howl","guardian_distant"+lIdx, "Player", 0, false);

            AddDebugMessage("Howl "+lIdx, false);

            AddLocalVarInt("HowlCount", 1);

            AddTimer("StopRadialBlur", 4.5f, nameof(TimerStopRadialBlur));
            AddTimer("react_scare", 1.0f, nameof(TimerStopLookAt));
            AddTimer("react_breath", 3.0f, nameof(TimerStopLookAt));

            StartScreenShake(lIdx*0.01f, 0.0f, 2.0f, 4.0f);
            FadeLightTo("BoxLight_1", lIdx*0.05f, 0.0f, lIdx*0.01f, -1, -1, 4.0f);

            SetRadialBlurStartDist(0.2f);
            FadeRadialBlurTo(0.09f, 0.015f);

            //If this is the area into last room, then remove some stuff related to blocked door.
            if(asChild == "HowlArea_2")
            {
                SetEntityPlayerInteractCallback("cellar_wood01_1", "", true);
                CompleteQuest("06BlockedDoor", "06BlockedDoor");
                SetEntityActive("block_box_1", false);
            }
        }

        void TimerStopRadialBlur(string asTimer)
        {
            AddDebugMessage("Stopping howl blur.", false);
            FadeRadialBlurTo(0, 0.02f);
        }

        //END HOWLING
        ///////////////////////////////


        ///////////////////////////////
        //BEGIN ENEMY GLIMPSE

        void EnterEnemyGlimpse(string asParent, string asChild, int alState)
        {
            SetEntityActive("grunt_normal_1", true);
            PlaySoundAtEntity("grunta","grunt/amb_idle", "GrunNoise", 0, false);
            StartPlayerLookAt("grunt_normal_1", 3, 3, "");
            PlayMusic("00_event_gallery.ogg", false, 1, 0.2f, 8, false);
            AddTimer("StopLook", 1.0f, nameof(TimerStopLookAt));
            AddTimer("react_scare.snt", 0.5f, nameof(TimerStopLookAt));
            AddTimer("react_breath.snt", 2.5f, nameof(TimerStopLookAt));

            CheckPoint("check01", "PlayerStartArea_6", nameof(CheckPoint01), "Hints", "DeathGrunt_06_Distillery_01");
        }
        void TimerStopLookAt(string asTimer)
        {
            if(StringContains(asTimer,"react_")){
                PlayGuiSound(asTimer, 0.7f);

                if(asTimer == "react_scare")
                    GiveSanityDamage(10.0f, true);

                return;
            }

            StopPlayerLookAt();
        }

        void CollideEnemyHint(string asParent, string asChild, int alState)
        {
            GiveHint("EnemyTip02", "Hints", "EnemyTip02", 0);
        }

        void EnemyDisabled(string asEnemy, string asType)
        {
            AddDebugMessage("Disabling enemy", false);
            if(GetEntitiesCollide(asEnemy, "AreaGruntGone"))
            {
                AddDebugMessage("Leaving track behind", false);
                SetPropActiveAndFade("grunt_gone_*", true, 0.5f);
                PlaySoundAtEntity("grunt_closes_door", "close_door.snt", "GruntGoneDoorSound", 0, false);
            }
        }

        void CheckPoint01(string asName, int alCount)
        {

        }

        //END ENEMY GLIMPSE
        ///////////////////////////////

        void CollideAreaRoses(string asParent, string asChild, int alState)
        {
            CreateParticleSystemAtEntity("bedvoidroses", "ps_rose_petals_wind.ps", "AreaRoses", false);
            PlayGuiSound("general_wind_whirl6.ogg", 0.3f);

            AddDebugMessage("Flow roses", false);
        }

        void EntityCallDiary(string asEntity, string type)
        {
            //GiveSanityBoostSmall();
        }

        void CollideAreaRope(string asParent, string asChild, int alState)
        {
            int iSound = RandInt(0, 1);
            float fFade= RandFloat(0.0f, 0.3f);

            if(iSound == 1) PlaySoundAtEntity("ropes", "06_rope_loose", "AreaRope_3", fFade, false);
            else PlaySoundAtEntity("ropes", "06_rope_strain", "AreaRope_3", fFade, false);


            //Remove interact on hatch when having opened the hatch.
            //Done here and not on break if player would try to open hatch by hand
            AddLocalVarInt("CountTurn", 1);

            if(GetLocalVarInt("CountTurn") == 4){
                SetEntityPlayerInteractCallback("hatch_large_1", "", true);
                RemoveTimer("hatchs");
                RemoveTimer("hatchon");
            }
        }
        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("Distillery");

            ////////////////////
            // Hub start sanity boost
            GiveSanityBoostSmall();


            //----COLLIDE CALLBACKS----//
            AddEntityCollideCallback("Player", "AreaBeginAnimal", nameof(FuncMoanHorrors), true, 1);

            AddEntityCollideCallback("BlockingBarellsArea", "barrel01_12", nameof(CollideBlockingBarellsArea), false, -1);

            AddEntityCollideCallback("Player","EmptyStorageDoorOpen", nameof(EnterEmptyStorageDoorOpen), false, 1);
            AddEntityCollideCallback("Player","BottleRoomWallScratch", nameof(EnterBottleRoomWallScratch), true, 1);

            AddEntityCollideCallback("Player","EnemyGlimpse", nameof(EnterEnemyGlimpse), true, 1);
            AddEntityCollideCallback("Player", "AreaEnemyHint", nameof(CollideEnemyHint), false, 1); //Display hint about enemies

            for(int i=1; i<=3; ++i)
                AddEntityCollideCallback("Player","HowlArea_"+i, nameof(EnterHowlArea), true, 1);

            AddEntityCollideCallback("Player", "AreaInPulley", nameof(CollideInPulley), false, 0);
            AddEntityCollideCallback("wood_blocking_pulley_1", "AreaPullPulley", nameof(CollidePulleyBlock), false, 0);

            AddEntityCollideCallback("Player", "AreaRoses_1", nameof(CollideAreaRoses), true, 1);

            AddEntityCollideCallback("crank_iron_1", "AreaRope_1", nameof(CollideAreaRope), false, 1);
            AddEntityCollideCallback("crank_iron_1", "AreaRope_2", nameof(CollideAreaRope), false, 1);

            //----ENEMY INIT----//
            //AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_46", 0, "");
            AddEnemyPatrolNode("grunt_normal_1", "PathNodeArea_78", 0, "");

            //----ENTITY INIT----//
            AddTimer("childTimer", 1.5f, nameof(FuncMoanTimer));
            float fMoan = RandFloat(5.5f,15.5f);
            float fCreak = RandFloat(4.5f,14.5f);
            AddTimer("moanTimer", 4.5f+fMoan, nameof(FuncMoanTimer));
            AddTimer("creak", 2.5f+fCreak, nameof(FuncCreakTimer));


            InteractConnectPropWithRope("CrankRope", "crank_iron_1", "Rope01", false, 1,1,1, false, 0);
            for(int i=1;i<4;i++) InteractConnectPropWithRope("CrankRope"+i, "crank_iron_1", "RopeArea_"+i, false, 1,1,1, false, 0);


            //----FIX CONTAINER----//
            if(HasItem("chemical_container_1"))
                RemoveItem("chemical_container_1");

            if(HasItem("chemical_container_2"))
                RemoveItem("chemical_container_2");

            if(!HasItem("empty_container"))
            {
                GiveItemFromFile("empty_container", "chemical_container.ent");
            }

            //----QUEST INIT----//
            //SetNumberOfQuestsInMap(1);


            ///////////////////////////////
            // Debug
            if(ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");
            }


            ///////////////////////////////
            // Fog init
            SetFogProperties(4, 16, 1, true);
            SetFogColor(0.12f, 0.14f, 0.18f, 0.7f);
            SetFogActive(true);
        }

        void InteractLastDoor(string asEntity)
        {
            SetMessage("Ch01Level06", "DoorBlocked", 0);
            AddQuest("06BlockedDoor", "06BlockedDoor");
        }

        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            //----PRELOADING----//
            PreloadSound("scare_whine_loop"); PreloadSound("scare_male_terrified"); PreloadSound("scare_baby_cry"); PreloadSound("scare_animal_squeal");
            PreloadSound("scare_wood_creak_mix"); PreloadSound("guardian_idle"); PreloadSound("01_door"); PreloadSound("enemy/grunt/amb_idle");
            PreloadSound("break_wood"); PreloadSound("impact_wood_heavy_high"); PreloadSound("guardian_distant1"); PreloadSound("guardian_distant2");
            PreloadSound("guardian_distant3");

            PreloadParticleSystem("ps_dust_falling_small_thin");


            //----AUDIO----//
            PlayMusic("06_amb.ogg", true, 1, 5.0f, 0, true);


            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            //////////////////////
            //Load Screen Setup
            SetupLoadScreen("LoadingText", "Ch01_WaterLurker01", 1, "game_loading_water.jpg");
        }

        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);
    }
}
