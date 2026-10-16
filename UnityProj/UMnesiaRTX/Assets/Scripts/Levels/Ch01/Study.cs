using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/11_study.hps: window and forest events, guiding rod and fall checkpoints.
    /// Journal: docs/ScriptPort/ch01/11_study.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/11_study.map")]
    public class Study : HplScript
    {
        //////////////////////
        //BEGIN BREAK WINDOW//
        void BreakWindow(string asEntity, string asType)
        {
            PlayMusic("10_puzzle02.ogg", false, 0.3f, 0.1f, 10, false);

            FadeLightTo("SpotLight_8", 0, 0, 0, 0, -1, 0.5f);
            FadeLightTo("SpotLight_4", 0.4f, 0.5f, 0.9f, -1, -1, 0.5f);

            PlaySoundAtEntity("birdsfly", "general_birds_flee", "AreaBirds", 0, false);

            CompleteQuest("11Path", "11Path");

            AddTimer("save", 1, nameof(TimerBreakWindow));
        }
        void TimerBreakWindow(string asTimer)
        {
            GiveSanityBoostSmall();
        }

        void InteractWindow(string asEntity)
        {
            PlayGuiSound("impact_glass_low.snt", 0.7f);

            if(GetTimerTimeLeft("WindowMessagePaused")==0)
            {
                SetMessage("Ch01Level11", "WornWindow", 0);
                PlayGuiSound("11_glass_crack.snt", 0.3f);

                AddTimer("WindowMessagePaused", 1, "");
            }

            AddLocalVarInt("WindowHitCount", 1);
            AddTimer("WindowHitCount", 0.5f, nameof(TimerDecreaseVar));

            if(GetLocalVarInt("WindowHitCount")==3)
            {
                SetPropHealth("mansionbase_large_window_breakable_1", 0);
            }

            //AddTimer("glass", 0.1f, "TimerWindow");
            //AddTimer("backon", 1.0f, "TimerWindow");
        }
        void TimerDecreaseVar(string asTimer)
        {
            AddLocalVarInt(asTimer, -1);
        }
        //void TimerWindow(string &in asTimer)
        //{
        //    SetEntityPlayerInteractCallback("mansionbase_large_window_breakable_1", "InteractWindow", true);

        //    if(asTimer == "glass")
        //}
        //END BREAK WINDOW//
        ////////////////////


        ///////////////////////
        //BEGIN FALL TO DEATH//
        void CollideFallDeath(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("fallaaaaaah", "11_fall", "Player", 0, false);
            PlaySoundAtEntity("fallaaaaaah2", "scare_male_terrified5", "Player", 0, false);

            FadeOut(1);

            AddTimer("death1", 0.5f, nameof(TimerFallDeath));
            AddTimer("death2", 1, nameof(TimerFallDeath));
        }
        void TimerFallDeath(string asTimer)
        {
            if(asTimer == "death1"){
                PlaySoundAtEntity("bump1", "player_bodyfall", "Player", 0, false);
                return;
            }

            DisableDeathStartSound();
            AddPlayerHealth(-200);

            PlaySoundAtEntity("pain", "player_falldamage_max", "Player", 0, false);
            PlaySoundAtEntity("bump2", "player_bodyfall", "Player", 0, false);
        }
        //END FALL TO DEATH//
        /////////////////////



        ///////////////////////
        //BEGIN BONES IN DESK//
        void CollideAreaDesk(string asParent, string asChild, int alState)
        {
            float fMin = -300.0f;
            float fMax = -500.0f;

            GiveSanityDamage(15, true);

            for(int i=1;i<7;i++)
                AddPropForce("skull_"+i, RandFloat(fMin, fMax), 0, RandFloat(0, -fMax), "World");

            AddPropForce("bone_1", RandFloat(fMin, fMax), 0, RandFloat(0, -fMax), "World");
            AddPropForce("bone_2", RandFloat(fMin, fMax), 0, RandFloat(0, -fMax), "World");

            PlayMusic("04_event_stairs", false, 1, 0.25f, 0, false);

            AddTimer("react", 0.5f, nameof(TimerPlayerReact));
        }
        void TimerPlayerReact(string asTimer)
        {
            PlaySoundAtEntity("react", "react_breath", "Player", 0.1f, false);
        }
        //END BONES IN DESK//
        /////////////////////


        //////////////////////
        //BEGIN FOREST THING//
        void CollideForestThings(string asParent, string asChild, int alState)
        {

            if(GetLocalVarInt("ForestThing") == 0){
                PlaySoundAtEntity("uhoh1", "11_forest_minor.snt", "AreaFallDeath", 0, false);
                StartScreenShake(0.01f, 1.5f, 2,3);

                AddPropForce("falling_tree_1", 0, 0, 1000, "world");

                AddTimer("freeze", 3.0f, nameof(TimerFreezeTree));
                AddTimer("sanity1", 2, nameof(TimerPlayerSanity));
                AddTimer("scare", 2, nameof(TimerPlayerReact02));

                AddLocalVarInt("ForestThing", 1);

                AddTimer("breath", 3.5f, nameof(TimerPlayerReact02));
                AddTimer("breath", 5.5f, nameof(TimerPlayerReact02));
            }
            else if(GetLocalVarInt("ForestThing") == 1 && GetLocalVarInt("HasManual") == 1){
                PlaySoundAtEntity("uhoh2", "11_forest_major.snt", "AreaFallDeath", 0, false);

                StartScreenShake(0.03f, 2, 2,3);

                SetPropStaticPhysics("falling_tree_1", false);
                AddPropForce("falling_tree_1", 0, 0, 1000, "world");

                AddTimer("removet", 10.0f, nameof(TimerFreezeTree));
                AddTimer("sanity2", 1.5f, nameof(TimerPlayerSanity));
                AddTimer("scare", 1.5f, nameof(TimerPlayerReact02));

                AddLocalVarInt("ForestThing", 1);

                AddTimer("breath", 3.5f, nameof(TimerPlayerReact02));
                AddTimer("breath", 5.5f, nameof(TimerPlayerReact02));
            }
        }
        void TimerPlayerSanity(string asTimer)
        {
            if(asTimer == "sanity1") GiveSanityDamage(5, true);
            else GiveSanityDamage(15, true);
        }
        void TimerPlayerReact02(string asTimer)
        {
            if(asTimer == "scare"){
                PlayGuiSound("react_scare", 0.7f);

                if(GetLocalVarInt("ForestThing") == 1 && GetLocalVarInt("HasManual") == 1)
                     PlayMusic("11_event_tree.ogg", false, 1.0f, 0.0f, 10, true);
            }
            else if(asTimer == "breath")
                PlayGuiSound("react_breath", 0.7f);
        }
        void TimerFreezeTree(string asTimer)
        {
            SetPropStaticPhysics("falling_tree_1", true);

            if(asTimer == "removet")
                SetEntityActive("falling_tree_1", false);
        }

        void CheckHaveAllItems()
        {
            AddLocalVarInt("ItemCount", 1);
            if(GetLocalVarInt("ItemCount")==2)
            {
                PlayMusic("10_puzzle01.ogg", false, 0.3f, 0.1f, 10, false);
                GiveSanityBoostSmall();

                AddDebugMessage("Setting level completion", false);
                AddDebugMessage("CompletedLevel_12: " + GetGlobalVarInt("CompletedLevel_12"), false);
                AddDebugMessage("(Before) CompletedLevel_10_11: " + GetGlobalVarInt("CompletedLevel_10_11"), false);

                if(GetGlobalVarInt("CompletedLevel_10_11")==0)
                    SetGlobalVarInt("CompletedLevel_10_11", GetGlobalVarInt("CompletedLevel_12")+1);

                AddDebugMessage("(After) CompletedLevel_10_11: " + GetGlobalVarInt("CompletedLevel_10_11"), false);
            }
        }

        void GotManual(string asEntity, string asType)
        {
            SetGlobalVarInt("PickedMachineInstructions", 1);
            AddLocalVarInt("HasManual", 1);
            CheckHaveAllItems();
        }

        void GotRod(string asEntity, string asType)
        {
            AddGlobalVarInt("GuidingRod", 1);
            CheckHaveAllItems();
        }

        //END FOREST THING//
        ////////////////////

        //////////////////////
        //BEGIN AGRIPPA NOTE//

        void PickAgrippaNote(string asEntityName, string asType)
        {
            PlayMusic("11_paper_howl.ogg", false, 1.0f, 0, 10, false);
        }

        //END AGRIPPA NOTE//
        //////////////////////


        //////////////////////
        //BEGIN CHECKPOINTS//

        void CollideCheckSouth(string asParent, string asChild, int alState)
        {
            CheckPoint("check01","PlayerStartCheckSouth", nameof(CheckPointSouth),"Hints", "DeathFall");
        }

        void CollideCheckNorth(string asParent, string asChild, int alState)
        {
            CheckPoint("check02","PlayerStartCheckNorth", nameof(CheckPointNorth),"Hints", "DeathFall");
        }

        //Southern part
        void CheckPointSouth(string asName, int alCount)
        {
            FadeIn(0.2f);
            if(alCount >=2)
            {
                TeleportPlayer("PlayerStartCheckSouthEasy");
            }
        }

        //North
        void CheckPointNorth(string asName, int alCount)
        {
            FadeIn(0.2f);
            if(alCount >=2)
            {
                TeleportPlayer("PlayerStartCheckNorthEasy");
            }
        }


        //END CHECKPOINTS//
        //////////////////////


        //BEGIN BREAK WINDOW
        void TimerBreakWindowWithNoFullSearch(string asTimer)
        {
            AddDebugMessage("Timer did it!", false);

            SetLocalVarInt("SeenAllRooms", 3);
            CheckIfAllRoomSearched();

            SetEntityActive("AreaBreakWindowOnClose", false);
        }

        void CollideAreaBreakWindow(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("brum", "11_forest_minor.snt", "AreaGlassEffects", 0.0f, false);
            PlaySoundAtEntity("guard", "04_warn.snt", "AreaGlassEffects", 0.0f, false);

            CreateParticleSystemAtEntity("puff", "ps_cave_monster_warn.ps", "AreaGlassEffects_3", false);

            StartPlayerLookAt("AreaGlassEffects", 4.0f, 4.0f, "");

            AddTimer("glass1", 1.5f, nameof(TimerGlassEffects));
            AddTimer("glass2", 0.5f, nameof(TimerGlassEffects));
            AddTimer("glass3", 1.2f, nameof(TimerGlassEffects));
            AddTimer("glass4", 1.5f, nameof(TimerGlassEffects));

            AddTimer("scare", 1.75f, nameof(TimerPlayerReact02));
            AddTimer("breath", 3.5f, nameof(TimerPlayerReact02));
            AddTimer("breath", 5.5f, nameof(TimerPlayerReact02));

            SetEntityActive("AreaBreakWindow_1", false);
            SetEntityActive("AreaBreakWindow", false);
        }
        void TimerGlassEffects(string asTimer)
        {
            if(asTimer == "glass1"){
                SetEntityActive("windowb_1", false);
                SetEntityActive("windowb_2", true);
                SetEntityActive("mansionbase_large_window_breakable_1", true);

                PlaySoundAtEntity("bglass", "11_glass_crack", "AreaGlassEffects", 0.0f, false);

                CreateParticleSystemAtEntity("bglassp", "ps_hit_glass.ps", "AreaGlassEffects", false);
            }
            else if(asTimer == "glass3"){
                PlaySoundAtEntity("bglass2", "11_glass_crack", "AreaGlassEffects_1", 0.0f, false);
                CreateParticleSystemAtEntity("bglassp2", "ps_hit_glass.ps", "AreaGlassEffects_1", false);
                StopPlayerLookAt();
            }
            else if(asTimer == "glass4"){
                PlaySoundAtEntity("bglass3", "11_glass_crack", "AreaGlassEffects_2", 0.0f, false);
                CreateParticleSystemAtEntity("bglassp3", "ps_hit_glass.ps", "AreaGlassEffects_2", false);
            }
            else{
                StartScreenShake(0.02f, 0.25f, 1.0f, 1.75f);
                StopSound("brum", 4.0f);
                StopSound("guard", 5.0f);
            }
        }
        //END BREAK WINDOW


        void CollideFlashBack(string asParent, string asChild, int alState)
        {
            AddTimer("startmusic", 2.0f, nameof(TimerStartMusicDog));
        }
        void TimerStartMusicDog(string asTimer)
        {
            PlayMusic("11_event_dog.ogg", false, 0.7f, 0, 10, false);

            AddTimer("stopmusic", 12.0f, nameof(TimerStopMusicDog));
        }
        void TimerStopMusicDog(string asTimer)
        {
            StopMusic(6.0f, 10);
        }

        void CollideAreaCorridorQuest(string asParent, string asChild, int alState)
        {
            AddQuest("11Path", "11Path");

            CheckIfAllRoomSearched();
        }


        void CollideAreaFadeOut(string asParent, string asChild, int alState)
        {
            FadeLightTo("PointLight_34", 0, 0, 0, 0, 0, 2);
        }


        //Collide count room visits
        void CollideAreaRoom(string asParent, string asChild, int alState)
        {
            CheckIfAllRoomSearched();
        }

        void CheckIfAllRoomSearched()
        {
            AddLocalVarInt("SeenAllRooms", 1);

            if(GetLocalVarInt("SeenAllRooms") == 4){
                SetEntityActive("AreaBreakWindow", true);
                SetEntityActive("AreaBreakWindow_1", true);
                RemoveTimer("BreakWindowNoFS");
                SetEntityActive("AreaBreakWindowOnClose", false);
            }
        }

        //Collide window directly when close
        void CollideAreaBreakWindowOnClose(string asParent, string asChild, int alState)
        {
            CollideAreaBreakWindow("", "", 1);    //Break window

            //Remove areas that count for break window
            SetEntityActive("AreaRoom1", false);
            SetEntityActive("AreaRoom2", false);
            SetEntityActive("AreaRoom3", false);
            SetEntityActive("AreaCorridorQuest", false);

            RemoveTimer("BreakWindowNoFS");    //Remove the timer that breaks window
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("Study");

            //---- COLLIDE INIT ----//
            AddEntityCollideCallback("Player", "AreaFallDeath", nameof(CollideFallDeath), false, 1);    //Kill player instantly if he falls down when outside
            AddEntityCollideCallback("work_desk_door_1", "AreaDesk", nameof(CollideAreaDesk), true, 1);
            AddEntityCollideCallback("Player", "AreaForestThings", nameof(CollideForestThings), false, 1);    //Kill player instantly if he falls down when outside

            AddEntityCollideCallback("Player", "AreaCheckSouth", nameof(CollideCheckSouth), false, 1);
            AddEntityCollideCallback("Player", "AreaCheckNorth", nameof(CollideCheckNorth), false, 1);

            AddEntityCollideCallback("Player", "FlashbackArea_1", nameof(CollideFlashBack), false, 1);

            AddEntityCollideCallback("Player", "AreaCorridorQuest", nameof(CollideAreaCorridorQuest), true, 1);

            AddEntityCollideCallback("Player", "AreaFadeOut", nameof(CollideAreaFadeOut), true, 1);

            AddEntityCollideCallback("Player", "AreaBreakWindow", nameof(CollideAreaBreakWindow), true, 1);
            AddEntityCollideCallback("Player", "AreaBreakWindow_1", nameof(CollideAreaBreakWindow), true, 1);

            AddEntityCollideCallback("Player", "AreaRoom1", nameof(CollideAreaRoom), true, 1);
            AddEntityCollideCallback("Player", "AreaRoom2", nameof(CollideAreaRoom), true, 1);
            AddEntityCollideCallback("Player", "AreaRoom3", nameof(CollideAreaRoom), true, 1);

            AddEntityCollideCallback("Player", "AreaBreakWindowOnClose", nameof(CollideAreaBreakWindowOnClose), true, 1);

            AddTimer("BreakWindowNoFS", 300, nameof(TimerBreakWindowWithNoFullSearch));    //Break window if player been in level long but not visited all rooms.

            //---- DEBUG ----//
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
            //---- PRELOAD SOUNDS ----//
            PreloadSound("11_fall"); PreloadSound("scare_male_terrified5"); PreloadSound("player_bodyfall");
            PreloadSound("player_falldamage_max"); PreloadSound("react_breath"); PreloadSound("general_birds_flee");
            PreloadSound("11_forest_minor"); PreloadSound("11_forest_major"); PreloadSound("04_warn"); PreloadSound("11_glass_crack");
            PreloadSound("impact_glass_low");  PreloadSound("react_scare");  PreloadSound("11_event_tree");

            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {

        }
    }
}
