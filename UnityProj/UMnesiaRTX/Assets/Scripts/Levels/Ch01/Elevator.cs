using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/14_elevator.hps: elevator lights, guardian howls, fall and chapter transition.
    /// Journal: docs/ScriptPort/ch01/14_elevator.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/14_elevator.map")]
    public class Elevator : HplScript
    {
        ////////////////////////////
        // Shake timer
        void TimerShake(string asTimer)
        {
            StartScreenShake(0.01f, 2, 0,0);
            AddTimer("Shake", 2.0f, nameof(TimerShake));
        }


        ////////////////////////////
        // Elevator events before howl
        void TimerElevatorPreHowlEvents(string asTimer)
        {
            float fTime = -1.0f;
            int lState = GetLocalVarInt("ElevatorEventState1");
            AddDebugMessage("Elevator State1: "+lState, false);

            ///////////////////
            // 0 - Wait
            if(lState==0)
            {
                CreateParticleSystemAtEntity("ScriptArea_1", "ps_elevator_slow.ps", "ScriptArea_1", false);
                CreateParticleSystemAtEntity("ScriptArea_2", "ps_elevator_slow.ps", "ScriptArea_2", false);
                PlaySoundAtEntity("ScriptArea_1", "14_scrape_low.snt", "ScriptArea_1", 5, false);
                PlaySoundAtEntity("ScriptArea_2", "14_scrape_low.snt", "ScriptArea_2", 5, false);
                fTime = 6;
            }
            ///////////////////
            // 1 - Flash light off
            else if(lState==1)
            {
                fTime = 1.5f;
                SetLampLit("elevator_lamp_1", false, true);
                FadeLightTo("PointLight_1", 0.6f, 0.3f, 0.0f, -1, -1, 1);
                PlaySoundAtEntity("wind", "general_wind_whirl.snt", "EffectArea", 1.0f, false);
            }
            ///////////////////
            // 2 - Flash light on
            else if(lState==2)
            {
                fTime = 1;
                SetLampLit("elevator_lamp_1", true, true);
                FadeLightTo("PointLight_1", 0.2f, 0.0f, 0.0f, -1, -1, 2);
                PlaySoundAtEntity("fire", "general_fire_burning_low", "EffectArea", 1.0f, false);
            }
            ///////////////////
            // 3 - Flash light off
            else if(lState==3)
            {
                fTime = 1.6f;
                SetLampLit("elevator_lamp_1", false, true);
                FadeLightTo("PointLight_1", 0.6f, 0.3f, 0.0f, -1, -1, 1);
                PlaySoundAtEntity("wind", "general_wind_whirl.snt", "EffectArea", 2.0f, false);
                StopSound("fire", 1.0f);
            }
            ///////////////////
            // 4- Flash light on
            else if(lState==4)
            {
                SetLampLit("elevator_lamp_1", true, true);
                FadeLightTo("PointLight_1", 0.2f, 0.0f, 0.0f, -1, -1, 3);
                PlaySoundAtEntity("fire", "general_fire_burning_low", "EffectArea", 1.0f, false);
            }


            if(fTime<0) return;
            AddLocalVarInt("ElevatorEventState1",1);
            AddTimer("Events1", fTime, nameof(TimerElevatorPreHowlEvents));
        }


        ////////////////////////////
        // Elevator events after howl
        void TimerElevatorPostHowlEvents(string asTimer)
        {
            float fTime = -1.0f;
            int lState = GetLocalVarInt("ElevatorEventState2");
            AddDebugMessage("Elevator State2: "+lState, false);

            ///////////////////
            // 0 - Black out
            if(lState==0)
            {
                fTime = 3;
                SetLampLit("elevator_lamp_1", false, true);
                FadeLightTo("PointLight_1", 0.6f, 0.3f, 0.0f, -1, -1, 3);
                PlaySoundAtEntity("wind", "general_wind_whirl.snt", "EffectArea", 2.0f, false);
                StopSound("fire", 1.0f);
            }
            ///////////////////
            // 1 - Fade in when slime is visble
            if(lState==1)
            {
                PlayMusic("00_event_gallery.ogg", false, 1, 0.1f, 0, false);
                fTime = 0.5f;
                SetLampLit("elevator_lamp_1", true, true);
                FadeLightTo("PointLight_1", 0.2f, 0.0f, 0.0f, -1, -1, 2);
                PlaySoundAtEntity("fire", "general_fire_burning_low", "EffectArea", 1.0f, false);
            }
            ///////////////////
            // 2 - Howl Distant
            if(lState==2)
            {
                PlaySoundAtEntity("stomp","scare_wall_stomp","Player", 0, false);
                StartScreenShake(0.1f, 0.45f, 0.05f, 1);
                SetRadialBlurStartDist(0.1f);
                FadeRadialBlurTo(1.0f, 5.0f);
                StartEffectFlash(0.2f, 0.1f,0.3f);
                FadePlayerFOVMulTo(0.5f, 3);
                AddTimer("scare", 1.0f, nameof(TimerPlayerReact));
                AddTimer("breath", 2.5f, nameof(TimerPlayerReact));
                AddTimer("breath", 4.5f, nameof(TimerPlayerReact));

                StopSound("fire", 1.0f);

                FadeOut(0.3f);

                AddTimer("1", 1, nameof(TimerStuff));

                fTime = 5;
                PlaySoundAtEntity("howl","guardian_distant1","Player", 0, false);
            }
            ///////////////////
            // 3 - Howl Close + stop elevator and shake screen.
            if(lState==3)
            {
                GiveSanityDamage(20, true);

                fTime = 5;
                PlaySoundAtEntity("howl","guardian_ontop.snt","Player", 0, false);
                PlaySoundAtEntity("explosion","elevator_stop","Player", 0, false);

                AddTimer("scare", 1.0f, nameof(TimerPlayerReact));
                AddTimer("breath", 2.5f, nameof(TimerPlayerReact));
                AddTimer("breath", 4.5f, nameof(TimerPlayerReact));

                StartScreenShake(0.1f, 1, 0,0);

                StopSound("MoveSound", 0.2f);

                DestroyParticleSystem("ScriptArea_1");
                DestroyParticleSystem("ScriptArea_2");
                StopSound("ScriptArea_1", 0.1f);
                StopSound("ScriptArea_2", 0.1f);

                SetLightVisible("PointLight_3", false);
                SetLightVisible("PointLight_4", false);

                SetMoveObjectStateExt("elevator_wall01_1", 1, 2, 0, 0.2f, true);
            }
            ///////////////////
            // 4 - Elevator falls!
            if(lState==4)
            {
                fTime = 6;
                StartScreenShake(0.05f, 3, 0,0);

                PlayMusic("14_elevator_falls.ogg", true, 1, 0.1f, 1, false);

                FadeImageTrailTo(0.5f, 3.1f);

                SetPlayerMoveSpeedMul(0.1f);

                PlaySoundAtEntity("FallSound","elevator_move_fall", "Player", 0.2f, false);

                PlayGuiSound("react_scare.snt", 1.0f);

                CreateParticleSystemAtEntity("ScriptArea_1", "ps_elevator_fast.ps", "ScriptArea_1", false);
                CreateParticleSystemAtEntity("ScriptArea_2", "ps_elevator_fast.ps", "ScriptArea_2", false);
                PlaySoundAtEntity("ScriptArea_1", "14_scrape_high.snt", "ScriptArea_1", 0.5f, false);
                PlaySoundAtEntity("ScriptArea_2", "14_scrape_high.snt", "ScriptArea_2", 0.5f, false);
                SetLightVisible("PointLight_3", true);
                SetLightVisible("PointLight_4", true);

                SetMoveObjectStateExt("elevator_wall01_1", 1, 2, 7, 0.2f, true);
            }
            ///////////////////
            // 5 - Fade out quickly and change map!
            if(lState==5)
            {
                fTime = 4;

                FadeOut(0.5f);
                StopMusic(0.1f, 1);
                FadeImageTrailTo(0, 1.1f);

                SetPlayerMoveSpeedMul(1.0f);

                StartScreenShake(0.1f, 1, 0,0);
                CreateParticleSystemAtEntity("dust","ps_break_wood.ps","EffectArea", false);

                StopSound("FallSound", 0.2f);
                PlaySoundAtEntity("explosion","explosion_rock_large","Player", 0, false);
                PlaySoundAtEntity("clankadank","14_elevator_clank","Player", 0, false);

                StopSound("ScriptArea_1", 0.1f);
                StopSound("ScriptArea_2", 0.1f);
                SetLightVisible("PointLight_3", false);
                SetLightVisible("PointLight_4", false);
                DestroyParticleSystem("ScriptArea_1");
                DestroyParticleSystem("ScriptArea_2");
            }

            ///////////////////
            // 6 - Change the map
            if(lState==6)
            {
                //If Preview just display message!
                if(GetGlobalVarInt("PreviewVersion")==1)
                {
                    TeleportPlayer("PlayerStartArea_2");
                    FadeIn(1);
                    SetPlayerActive(false);
                    SetMessage("Temp", "PreviewOver", 200);
                }
                else
                {
                    ChangeMap("ch02/15_prison_south.map", "PlayerStartArea_1", "","");
                }
            }


            if(fTime<0) return;
            AddLocalVarInt("ElevatorEventState2",1);
            AddTimer("Events2", fTime, nameof(TimerElevatorPostHowlEvents));
        }

        void TimerStuff(string asTimer)
        {
            FadePlayerFOVMulTo(1.0f, 0.5f);
            FadeRadialBlurTo(0.0f, 1.0f);
            FadeIn(1.3f);

            FadePlayerFOVMulTo(1, 1);
        }

        void TimerPlayerReact(string asTimer)
        {
            PlayGuiSound("react_"+asTimer, 0.6f);
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            SetMapDisplayNameEntry("Elevator");

            SetMoveObjectStateExt("elevator_wall01_1", 1, 2, 1.0f, 0.2f, true);
            AddTimer("Shake", 0.01f, nameof(TimerShake));
            AddTimer("Events1", 0.01f, nameof(TimerElevatorPreHowlEvents));
            AddTimer("Events2", 13.5f, nameof(TimerElevatorPostHowlEvents));


            SetLocalVarInt("ElevatorEventState1", 0);
            SetLocalVarInt("ElevatorEventState2", 0);
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            StopMusic(2,0);

            PlaySoundAtEntity("MoveSound", "elevator_move_normal", "EffectArea", 1.0f, true);

            PreloadParticleSystem("ps_break_wood");

            PreloadSound("guardian_distant1"); PreloadSound("elevator_stop"); PreloadSound("elevator_move_fall"); PreloadSound("explosion_rock_large");
            PreloadSound("14_elevator_clank"); PreloadSound("14_elevator_clank");
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            //////////////////////
            //Load Screen Setup
            //SetupLoadScreen("LoadingText", "Ch02_Prison01", 1, "game_loading_well.jpg");
            SetupLoadScreen("LoadingText", "Ch02_Diary05_", 6, "game_loading_brennenburg.jpg");
        }
    }
}
