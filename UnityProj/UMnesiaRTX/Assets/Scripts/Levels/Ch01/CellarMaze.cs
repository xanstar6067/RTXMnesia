using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/08_cellar_maze.hps: water lurker chase,
    /// locked doors, fake impacts and checkpoint resets.
    /// Journal: docs/ScriptPort/ch01/08_cellar_maze.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/08_cellar_maze.map")]
    public class CellarMaze : HplScript
    {
        ////////////////////////////
        // Stop the lurker
        void CollideStopLurker01(string asParent, string asChild, int alState)
        {
            if(GetSwingDoorClosed("cellar_wood01_2")==false) return;

            AddDebugMessage("stopped 1", false);
            SetEnemyDisabled("waterlurker_1", true);

            SetSwingDoorLocked("cellar_wood01_2", true, false);

            AddTimer("FakeBreakSounds01", 0.5f, nameof(TimerBreakSounds01));
        }

        void CollideStopLurker02(string asParent, string asChild, int alState)
        {
            if(GetSwingDoorClosed("cellar_wood01_4")==false) return;

            AddDebugMessage("stopped 2", false);
            SetEnemyDisabled("waterlurker_2", true);

            SetSwingDoorLocked("cellar_wood01_4", true, false);

            RemoveTimer("FakeBreakSounds02");
            AddTimer("FakeBreakSounds03", 0.5f, nameof(TimerBreakSounds03));
        }

        ////////////////////////////
        // Player just closed another door.
        void CollideHitDoorEffects01(string asParent, string asChild, int alState)
        {
            //PlayMusic("08_amb.ogg", true, 1, 1.0f, 0, true);

            RemoveTimer("FakeBreakSounds01");

            if(GetSwingDoorClosed("cellar_wood01_3"))
            {
                SetSwingDoorLocked("cellar_wood01_3", true, false);
                AddTimer("FakeBreakSounds02", 0.5f, nameof(TimerBreakSounds02));
            }
        }

        ////////////////////////////
        // Guardian Howls
        void TimerGuardianCalls(string asTimer)
        {
            int lNum = RandInt(1,2);

            if(lNum == 1)
                PlaySoundAtEntity("GuardianHowl","guardian_ontop", "Player",0, false);
            else
                PlaySoundAtEntity("GuardianHowl","guardian_distant", "Player",0, false);

            AddTimer("Howling", RandFloat(2.5f,10.5f), nameof(TimerGuardianCalls));
        }

        ////////////////////////////
        // Fake Enemy Sounds
        void HitDoorEffects(string asDoor)
        {
            PlaySoundAtEntity("DoorPound","lurker_hit_wood", asDoor,0, false);
            PlaySoundAtEntity("DoorWaterPound","impact_water_high", asDoor,0, false);
            CreateParticleSystemAtEntity("DoorParticle","ps_hit_wood" ,asDoor, false);
            StartScreenShake(0.01f, 0.25f, 0.25f, 0.5f);
        }

        void TimerBreakSounds01(string asTimer)
        {
            HitDoorEffects("cellar_wood01_2");

            AddTimer("FakeSounds01", RandFloat(0.5f,2.5f), nameof(TimerBreakSounds01));
        }

        void TimerBreakSounds02(string asTimer)
        {
            HitDoorEffects("cellar_wood01_3");

            AddTimer("FakeSounds02", RandFloat(0.5f,2.5f), nameof(TimerBreakSounds02));
        }

        void TimerBreakSounds03(string asTimer)
        {
            HitDoorEffects("cellar_wood01_4");

            AddTimer("FakeSounds03", RandFloat(1,2), "TimerFakeEnemySounds03");
        }

        ////////////////////////////
        // Start the lurkers
        void CollideStartLurker01(string asParent, string asChild, int alState)
        {
            AddDebugMessage("Starting 1", false);

            //PlayMusic("08_amb.ogg", true, 1, 1.0f, 0, true);

            //////////////////////
            //Start first enemy instance
            AddTimer("TimerStartLurker01", 4, nameof(TimerStartLurker01));

            AddTimer("TimerStartSplash1", 2.2f, nameof(TimerStartSplash));
            AddTimer("TimerStartSplash2", 3.2f, nameof(TimerStartSplash));

            AddTimer("scare", 1.5f, nameof(TimerPlayerReact));
            AddTimer("breath", 4.0f, nameof(TimerPlayerReact));
            AddTimer("breathl", 6.0f, nameof(TimerPlayerReact));

            ////////////////////////
            //Start howling
            PlaySoundAtEntity("GuardianHowl","guardian_activated", "Player",0, false);
            AddTimer("Howling", 10, nameof(TimerGuardianCalls));
        }
        void TimerStartSplash(string asTimer)
        {
            PlaySoundAtEntity("splash","water_lurker_hunt", "AreaLurkerSplash", 0, false);
        }

        void TimerStartLurker01(string asTimer)
        {
            SetEntityActive("waterlurker_1", true);

            AddTimer("damage", 0.2f, nameof(TimerDamageSanity));
        }

        void TimerDamageSanity(string asTimer)
        {
            GiveSanityDamage(5.0f, false);
        }

        //////////

        void CollideStartLurker02(string asParent, string asChild, int alState)
        {
            AddDebugMessage("Starting 2", false);
            SetEntityActive("waterlurker_2", true);

            AddTimer("damage", 0.2f, nameof(TimerDamageSanity));

            AddTimer("scare", 1.5f, nameof(TimerPlayerReact));
            AddTimer("breath", 4.0f, nameof(TimerPlayerReact));
            AddTimer("breathl", 6.0f, nameof(TimerPlayerReact));
        }

        //////////

        void CollideStartLurker03(string asParent, string asChild, int alState)
        {
            //PlayMusic("08_amb.ogg", true, 1, 1.0f, 0, true);

            AddDebugMessage("Starting 3", false);
            SetEntityActive("waterlurker_3", true);

            AddTimer("damage", 0.2f, nameof(TimerDamageSanity));

            AddTimer("scare", 1.5f, nameof(TimerPlayerReact));
            AddTimer("breath", 4.0f, nameof(TimerPlayerReact));
            AddTimer("breathl", 6.0f, nameof(TimerPlayerReact));
        }

        //////////

        void CollideStartLurker04(string asParent, string asChild, int alState)
        {
            AddDebugMessage("Starting 4", false);
            SetEntityActive("waterlurker_4", true);

            AddTimer("damage", 0.2f, nameof(TimerDamageSanity));

            AddTimer("scare", 1.5f, nameof(TimerPlayerReact));
            AddTimer("breath", 4.0f, nameof(TimerPlayerReact));
            AddTimer("breathl", 6.0f, nameof(TimerPlayerReact));
        }

        //////////

        ////////////////////////////
        // CheckPoints
        void CheckPoint01(string asName, int alCount)
        {
            SetPlayerHealth(RandFloat(80, 95));

            //Reset all doors
            ResetProp("cellar_wood01*");
            SetSwingDoorLocked("cellar_wood01*", false, false);

            ResetProp("barrel01*");
            ResetProp("bag01*");
            ResetProp("wood_box01*");
            ResetProp("wood_box02*");
            ResetProp("chair_wood*");
            ResetProp("wood_box_small01*");
            ResetProp("chair_wood02*");

            RemoveTimer("Howling");

            for(int i=1; i<=3; ++i)
                RemoveTimer("FakeSounds0"+i);

            for(int i=1; i<=3; ++i)
                RemoveTimer("FakeBreakSounds0"+i);

            SetupCollideAreas(alCount);
        }

        void SetupCollideAreas(int alCount)
        {
            for(int i=1; i<=4; ++i)
            {
                if(alCount>=1 && i<=2) continue;

                AddEntityCollideCallback("Player","StartLurker_"+i, "CollideStartLurker0"+i, true, 0);
            }

            AddEntityCollideCallback("Player","HitDoorEffects01", nameof(CollideHitDoorEffects01), true, 0);

            AddEntityCollideCallback("Player","StopLurker_1_1", nameof(CollideStopLurker01), true, 0);
            AddEntityCollideCallback("Player","StopLurker_1_2", nameof(CollideStopLurker01), true, 0);

            AddEntityCollideCallback("Player","StopLurker_2_1", nameof(CollideStopLurker02), true, 0);
            AddEntityCollideCallback("Player","StopLurker_2_2", nameof(CollideStopLurker02), true, 0);
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

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("CellarMaze");

            ///////////////////////////////
            // Set up areas
            SetupCollideAreas(-1);

            ///////////////////////////////
            // Debug
            if(ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                //StartUpDarkSide();
            }
        }

        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            StopMusic(2, 0);

            PreloadSound("guardian_distant");
            PreloadSound("hit_wood"); PreloadSound("guardian_ontop"); PreloadSound("guardian_activated");

            PreloadParticleSystem("ps_hit_wood");

            CheckPoint("check01","PlayerStartArea_1", nameof(CheckPoint01), "Hints", "DeathWaterLurker03");

            ClearSavedMaps();

            ///////////////////////////////
            // Autosave
            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            SetDeathHint("", "");

            //////////////////////
            //Load Screen Setup
            SetupLoadScreen("LoadingText", "Ch01_WaterLurker03", 1, "game_loading_water.jpg");
        }
    }
}
