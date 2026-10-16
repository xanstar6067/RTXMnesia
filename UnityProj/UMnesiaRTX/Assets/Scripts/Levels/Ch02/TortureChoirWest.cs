using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/24_torture_choir_west.hps; journal: docs/ScriptPort/ch02/24_torture_choir_west.md.</summary>
    [HplMapScript("maps/main/ch02/24_torture_choir_west.map")]
    public class TortureChoirWest : HplScript
    {
        // TODO: Maybe tweak the blur in the vision?
        // TODO: Add some background noise for the vision?

        //------------------------------------------

        ////////////////////////////
        //GLOBAL HELPERS
        /////////////////////////////

        //------------------------------------------

        // Vars
        float gfVoiceDelay = 0.05f;
        string gsLanternVar = "lantern_active";

        /////////////////////////////
        // Door opener
        void OpenSwingDoor(bool abProp, string asDoorName, float afTimeBetweenSteps, int alSteps, float afX, float afY, float afZ, string asCoordSystem)
        {
        	SetLocalVarFloat(asDoorName+"X", afX);
        	SetLocalVarFloat(asDoorName+"Y", afX);
        	SetLocalVarFloat(asDoorName+"Z", afX);
        	SetLocalVarString(asDoorName+"CoordSystem", asCoordSystem);

        	string sTimerName = "TimerSwingDoor" + (abProp ? "Prop" : "Body" );

        	for(int i=0;i<alSteps;++i)
        	{
        		AddTimer(asDoorName, i*afTimeBetweenSteps, sTimerName);
        	}
        }

        //------------------------------------------

        //////////////////////////////
        // Door opener helper timers
        void TimerSwingDoorProp(string asTimer)
        {
        	AddPropForce(asTimer, GetLocalVarFloat(asTimer+"X"),
        						  GetLocalVarFloat(asTimer+"X"),
        						  GetLocalVarFloat(asTimer+"X"),
        						  GetLocalVarString(asTimer+"CoordSystem"));
        }

        void TimerSwingDoorBody(string asTimer)
        {
        	AddBodyForce(asTimer, GetLocalVarFloat(asTimer+"X"),
        						  GetLocalVarFloat(asTimer+"X"),
        						  GetLocalVarFloat(asTimer+"X"),
        						  GetLocalVarString(asTimer+"CoordSystem"));
        }
        /////////////////////////////

        //------------------------------------------

        void SaveLanternState()
        {
        	int lLanternActive = GetLanternActive() ? 1 : 0;
        	SetLocalVarInt(gsLanternVar, lLanternActive);
        }

        void RestoreLanternState()
        {
        	SetLanternActive(GetLocalVarInt(gsLanternVar)==1, false);
        }

        //------------------------------------------


        void PlayEffectVoice(string asEntryBase, string asCat,int alStartIdx, int alEndIdx, string asCallback)
        {
        	for(int i=alStartIdx; i<=alEndIdx; ++i)
        	{
        		string sEntry = asEntryBase;
        		if(i<10) sEntry += "0";
        		sEntry += i;

        		AddEffectVoice(sEntry, "", asCat, sEntry, false, "", 0,0 );
        	}
        	SetEffectVoiceOverCallback(asCallback);
        }

        void RitualVoiceOver()
        {
        	ResumeEventTimer(nameof(TimerRitualVision), gfVoiceDelay);
        }

        //------------------------------------------

        //////////////////////////////////////////
        // Event Timer helpers

        //This starts an event timer, with the state set to 0 (just to be sure)
        void ResumeEventTimer(string asTimer)
        {
        	SetLocalVarInt(asTimer, 0);
        	ResumeEventTimer(asTimer, 0);
        }

        //Resumes an event timer after a step is over.
        void ResumeEventTimer(string asTimer, float afTime)
        {
        	AddTimer(asTimer, afTime, asTimer);
        }

        //------------------------------------------

        ////////////////////////////
        //RITUAL VISION
        ////////////////////////////////

        //------------------------------------------

        // Pick up diary (Start vision)
        void PickDiary(string asItem, int alEntryIdx)
        {
        	ReturnOpenJournal(false);

        	PlayMusic("24_paper_daniel01.ogg", false, 0.7f, 1.0f, 10, false);

        	SetLocalVarInt("VisionActive", 1);

        	StopSound("AreaAmb", 4.0f);
        	for(int i=1;i<=4;i++) StopSound("AreaDrop_"+i, 0.0f);

        	PlayGuiSound("flashback_flash.snt", 0.8f);
        	PlayGuiSound("react_scare.snt", 0.5f);

        	StartScreenShake(0.1f, 0.1f, 0.0f, 0.2f);
        	StartEffectFlash(0.05f, 0.0f, 0.05f);
        	FadeImageTrailTo(0.3f, 0.3f);
        	PlayGuiSound("sanity_flick.snt", 0.5f);
        	ResumeEventTimer(nameof(TimerRitualVision));
        }

        //------------------------------------------

        ////////////////////////////
        // Diary voice helper
        // Note when callback != "" then it means that next event is when voice is over!
        void PlayVisionVoice(string asEntry, int alStartIdx, int alEndIdx, string asCallback)
        {
        	PlayEffectVoice(asEntry, "Flashbacks",alStartIdx, alEndIdx, asCallback);
        }

        ////////////////////////////
        // Diary voice Callback
        void VisionVoiceOver()
        {
        	ResumeEventTimer(nameof(TimerRitualVision), gfVoiceDelay);
        }

        //------------------------------------------

        //Voice talking about using knife is over
        void VisionKnifeVoiceOver()
        {
        	AddLocalVarInt("KnifeVoiceCount",1);
        	if(GetLocalVarInt("KnifeVoiceCount")==2)
        		ResumeEventTimer(nameof(TimerRitualVision), 0);
        }

        // Pick up knife
        void InteractWithKnife(string asEntity)
        {
        	AddDebugMessage("Picking up knife", false);

        	SetEntityActive("ceremony_knife", false);

        	//Fade out the the light used to illuminate it.
        	FadeLightTo("PointLight_knife", 0,0,0,0, -1, 1);

        	AddLocalVarInt("KnifeVoiceCount",1);
        	if(GetLocalVarInt("KnifeVoiceCount")==2)
        		ResumeEventTimer(nameof(TimerRitualVision), 0);
        }

        //------------------------------------------

        // Use knife
        void InteractWithPrisoner(string asEntity)
        {
        	AddDebugMessage("Using knife on prisoner", false);

        	ResumeEventTimer(nameof(TimerRitualVision), 0);
        }

        //------------------------------------------

        void TimerLookAtOver(string asTimer)
        {
        	StopPlayerLookAt();
        }

        //------------------------------------------

        ////////////////////////////
        // Prisoner squirm effect helper
        void TimerPrisonerSquirm(string asTimer)
        {
        	if(GetLocalVarInt( "prisoner_anim_active" )==0)
        		return;

        	StartPrisonerSquirm();
        }

        void StartPrisonerSquirm()
        {
        	PlayPropAnimation("ritual_prisoner", "squirm", 0.2f, false, nameof(PrisonerSquirmOver));
        }
        void PrisonerSquirmOver(string asName)
        {
        	PlayPropAnimation("ritual_prisoner", "Idle", 0.2f, true, "");

        	AddTimer(nameof(TimerPrisonerSquirm), RandFloat(1.0f, 4.0f), nameof(TimerPrisonerSquirm));
        }

        //------------------------------------------

        void SetToolsVisible(bool abX)
        {
        	SetEntityActive("dissect_knife_*", abX);
        	SetEntityActive("hammer_*", abX);
        	SetEntityActive("saw_*", abX);
        	SetEntityActive("pincers_*", abX);
        	SetEntityActive("dissect_scissors_*", abX);
        	SetEntityActive("chisel_*", abX);
        	SetEntityActive("cell_bucket_*", abX);
        }

        void SetLightsOutsideRitualRoomVisible(bool abX)
        {
        	SetLightVisible("PointLight_11", abX);
        	SetLightVisible("PointLight_10", abX);
        	SetLightVisible("PointLight_9", abX);
        	SetLightVisible("PointLight_8", abX);
        	SetLightVisible("PointLight_4", abX);
        	SetLightVisible("PointLight_5", abX);
        	SetLightVisible("PointLight_6", abX);
        	SetLightVisible("PointLight_7", abX);
        	SetLightVisible("PointLight_12", abX);

        	//SetLampLit("candle_floor_no_light_53", abX, false);
        	SetLampLit("candle_floor_no_light_21", abX, false);
        	SetLampLit("candle_floor_no_light_22", abX, false);
        	SetLampLit("candle_floor_no_light_23", abX, false);
        	SetLampLit("candle_floor_no_light_24", abX, false);
        	SetLampLit("candle_floor_7", abX, false);
        	SetLampLit("candle_floor_no_light_25", abX, false);
        	SetLampLit("candle_floor_no_light_26", abX, false);
        	SetLampLit("candle_floor_no_light_27", abX, false);
        	SetLampLit("candle_floor_no_light_28", abX, false);
        	SetLampLit("candle_floor_no_light_32", abX, false);
        	SetLampLit("candle_floor_no_light_36", abX, false);
        	SetLampLit("candle_floor_no_light_29", abX, false);
        	SetLampLit("candle_floor_2", abX, false);
        	SetLampLit("candle_floor_no_light_31", abX, false);
        	SetLampLit("candle_floor_no_light_33", abX, false);
        	SetLampLit("candle_floor_no_light_34", abX, false);
        	SetLampLit("candle_floor_no_light_35", abX, false);
        	SetLampLit("candle_floor_no_light_51", abX, false);
        	SetLampLit("candle_floor_no_light_50", abX, false);
        	SetLampLit("candle_floor_no_light_49", abX, false);
        	SetLampLit("candle_floor_no_light_48", abX, false);
        	SetLampLit("candle_floor_no_light_47", abX, false);
        	SetLampLit("candle_floor_no_light_46", abX, false);
        	SetLampLit("candle_floor_8", abX, false);
        	SetLampLit("candle_floor_no_light_52", abX, false);
        	SetLampLit("candle_floor_no_light_45", abX, false);
        	SetLampLit("candle_floor_no_light_44", abX, false);
        	SetLampLit("candle_floor_no_light_43", abX, false);
        }


        //------------------------------------------


        ////////////////////////////
        // Timed ritual event
        void TimerRitualVision(string asTimer)
        {
        	/*Configurables
        	 */
        	int iMaxEventStep = 21;		//How many steps there are in the switch event
        	float fEventSpeed = 1.0f;	//The default time between steps in an event

        	/*Helpers - Do not edit
        	 */
        	string sEvent = asTimer;	//Using first timer name for variable, timer name & callback for the timer that loops
        	AddLocalVarInt(sEvent, 1);	//What step to play in the event
        	bool bPauseAtEvent = false;	//Pauses after current step is done (a ResumeEventTimer would unpause)

        	AddDebugMessage(asTimer + " - current step : " + GetLocalVarInt(sEvent), false);

        	/*Steps in the event
        	 */
        	switch(GetLocalVarInt(sEvent))
        	{

        		////////////////////////
        		// Fade Out
        		case 1:
        			SetInventoryDisabled(true);
        			SetSanityDrainDisabled(true);
        			SetPlayerJumpDisabled(true);
        			SetPlayerCrouchDisabled(true);

        			StartPlayerLookAt("AreaPrisonerMarks", 0.5f, 3, "");

        			fEventSpeed = 1.0f;
        			FadeOut(fEventSpeed);

        			SetPlayerLookSpeedMul(0.9f);
        			SetPlayerMoveSpeedMul(0.75f);
        			break;

        		////////////////////////
        		// Narration: Background to the ritual
        		// 	Next event when: voice ends.
        		case 2:
        			FadeImageTrailTo(0.0f, 0.3f);
        			PlayGuiSound("react_breath", 0.5f);
        			//Turn off lights in corridor outside
        			SetLightsOutsideRitualRoomVisible(false);
        			SetToolsVisible(false);

        			//Turn off lantern
        			SaveLanternState();
        			SetLanternActive(false, false);
        			SetLanternDisabled(true);

        			SetPlayerActive(false);

        			SetPlayerRunSpeedMul(0.0f);
        			SetPlayerMoveSpeedMul(0.4f);
        			SetPlayerLookSpeedMul(0.7f);

        			TeleportPlayer("PlayerStartArea_VisionEnd");

        			// Set up blurry visuals
        			SetRadialBlurStartDist(0.2f);
        			FadeRadialBlurTo(0.1f, 1);
        			FadeSepiaColorTo(0.5f, 0.025f);

        			SetSwingDoorLocked("cellar_wood01_cellar_wood01_1", true, false);
        			SetPropActiveAndFade("ritual_prisoner", true, 0);
        			SetPropEffectActive("ritual_prisoner", false, false);

        			SetEntityActive("blood_spatter*", false);

        			PlayVisionVoice("CH02L24_DanielDiary07_03_",1, 1, nameof(VisionVoiceOver));
        			bPauseAtEvent = true;
        			break;

        		////////////////////////
        		// Fade in
        		case 3:
        			PlayMusic("24_event_vision.ogg", true, 0.6f, 4.0f, 10, true);

        			StopPlayerLookAt();
        			SetPlayerActive(true);
        			SetLocalVarInt( "prisoner_anim_active", 1);
        			//StartPrisonerSquirm();

        			fEventSpeed = 2.0f;
        			FadeIn(fEventSpeed);

        			PlaySoundAtEntity("amb_noise", "24_amb_noise.snt", "AreaPrisonerMarks", 4.0f, false);
        			PlaySoundAtEntity("sound1", "24_mb_01.snt", "AreaPrisonerSounds", 0.0f, false);
        			break;

        		////////////////////////
        		// Narration: Alexander begins describing what to do.
        		case 4:
        			PlaySoundAtEntity("knife", "24_knife.snt", "AreaDrop_2", 2.0f, true);
        			PlayVisionVoice("CH02L24_DanielDiary07_03_",2,2, nameof(VisionVoiceOver));
        			bPauseAtEvent = true;
        			break;

        		////////////////////////
        		// More narration on knife and act. Next event when voice is over and knife is picked.
        		case 5:
        			PlayVisionVoice("CH02L24_DanielDiary07_03_",3,3, nameof(VisionVoiceOver));

        			bPauseAtEvent = true;
        			break;

        		////////////////////////
        		// Show knife a bit after instead, or else a long wait with nothign happening.
        		case 6:
        			PlayVisionVoice("CH02L24_DanielDiary07_03_",4,5, nameof(VisionKnifeVoiceOver));

        			//Look at knife
        			StartPlayerLookAt("ceremony_knife", 0.8f, 3, "");
        			AddTimer(nameof(TimerLookAtOver), 1.5f ,nameof(TimerLookAtOver));

        			FadeLightTo("PointLight_knife", 0.8f,0.1f,0.1f,0.7f, -1, 1);

        			//Show knife
        			SetPropActiveAndFade("ceremony_knife", true, 1);
        			PlaySoundAtEntity("ceremony_knifeplace", "24_put_knife.snt", "ceremony_knife", 0.0f, false);
        			StopSound("knife", 0.0f);
        			bPauseAtEvent = true;
        			break;

        		////////////////////////
        		// Player picks up knife and narration. Narration: Daniel about doubts and description of what is to be done
        		case 7:
        			PlayVisionVoice("CH02L24_DanielDiary07_03_",6,6, nameof(VisionVoiceOver));
        			bPauseAtEvent = true;
        			break;

        		////////////////////////
        		// Player picks up knife and narration. Narration: Daniel about doubts and description of what is to be done
        		case 8:
        			PlayVisionVoice("CH02L24_DanielDiary07_03_",7,7, nameof(VisionVoiceOver));

        			PlayMusic("24_event_vision02.ogg", true, 0.6f, 4.0f, 10, true);

        			PlayPropAnimation("ritual_prisoner", "squirm", 0.2f, true, nameof(PrisonerSquirmOver));

        			StopSound("sound1", 0.0f);
        			PlaySoundAtEntity("sound2", "24_mb_02.snt", "AreaPrisonerSounds", 0.0f, false);

        			bPauseAtEvent = true;
        			break;

        		////////////////////////
        		// Pattern appear on body,
        		case 9:

        			//Look at body
        			StartPlayerLookAt("AreaPrisonerMarks", 0.8f, 3, "");
        			AddTimer(nameof(TimerLookAtOver), 1.5f ,nameof(TimerLookAtOver));

        			//light up chest
        			FadeLightTo("PointLight_chest", 0.2f,0.8f,0.2f,0.7f, -1, 1);

        			PlaySoundAtEntity("paint", "24_paint.snt", "AreaPrisonerMarks", 0.0f, false);

        			//Fade in the markings on prisoner
        			SetPropEffectActive("ritual_prisoner", true, true);

        			//Make the interact area active.
        			SetEntityActive("AreaPrisonerMarks", true);

        			bPauseAtEvent = true;
        			break;

        		////////////////////////
        		// player interacts with prisoner, prisoner starts saying "please"-> Fade to black.
        		case 10:
        			FadeOut(2);
        			PlayMusic("24_event_vision03.ogg", true, 0.6f, 4.0f, 10, true);

        			FadeLightTo("PointLight_chest", 0,0,0,0, -1, 1);

        			//Freeze player and look at prisoner
        			SetPlayerActive(false);
        			StartPlayerLookAt("AreaPrisonerMarks", 0.5f, 3, "");

        			fEventSpeed = 2; //Next event after fade is done.

        			StopSound("amb_noise", 4.0f);
        			break;

        		////////////////////////
        		// Voice starts after fade out, Voice Line 8. (Lines area sepperate below to easily time additional effects.
        		case 11:
        			StopSound("sound2", 0.0f);
        			PlaySoundAtEntity("sound3", "24_mb_03.snt", "AreaPrisonerSounds", 0.0f, false);

        			PlaySoundAtEntity("cut", "24_cut.snt", "AreaPrisonerMarks", 0.0f, false);

        			PlayVisionVoice("CH02L24_DanielDiary07_03_",8,8, nameof(VisionVoiceOver));

        			// Set up scene: remove prisoner and add blood trail
        			SetLocalVarInt("prisoner_active", 0);

        			SetEntityActive("ritual_prisoner", false);
        			//SetEntityActive("ceremony_knife_dropped", true); //Do not drop knife...

        			bPauseAtEvent = true;
        			break;

        		///////////////////////
        		// Voice Line 9
        		case 12:
        			PlayVisionVoice("CH02L24_DanielDiary07_03_",9,9, nameof(VisionVoiceOver));
        			bPauseAtEvent = true;
        			break;

        		///////////////////////
        		// Voice Line 10
        		case 13:
        			StopSound("sound3", 0.0f);
        			PlaySoundAtEntity("sound4", "24_mb_04.snt", "AreaPrisonerSounds", 0.0f, false);
        			PlayVisionVoice("CH02L24_DanielDiary07_03_",10,10, nameof(VisionVoiceOver));
        			bPauseAtEvent = true;
        			StopMusic(4.0f, 10);
        			break;

        		///////////////////////
        		// Voice Line 11
        		case 14:
        			PlayMusic("24_event_vision04.ogg", true, 1.0f, 4.0f, 10, true);
        			PlayVisionVoice("CH02L24_DanielDiary07_03_",11,11, nameof(VisionVoiceOver));
        			StopSound("cut", 0.5f);
        			PlaySoundAtEntity("pick", "24_pick_body.snt", "AreaPrisonerMarks", 0.0f, false);
        			bPauseAtEvent = true;
        			break;

        		////////////////////////
        		// Fade in. Prisoner is gone and blood trail instead.
        		case 15:
        			fEventSpeed = 2;
        			FadeIn(fEventSpeed);

        			SetPlayerActive(true);
        			StopPlayerLookAt();

        			//Show blood + fadein lamp to make it more visible
        			SetEntityActive("blood_spatter*", true);

        			FadeLightTo("PointLight_Blood", 0.8f,0.1f,0.1f,0.7f, -1, 1);

        			PlaySoundAtEntity("amb_noise", "24_amb_noise.snt", "Player", 4.0f, false);
        			AddTimer("heart", 2.0f, nameof(TimerHeartSound));
        			StopMusic(6.0f, 10);
        			FadeGlobalSoundVolume(0.0f, 4.0f);
        			break;

        		////////////////////////
        		// Narration: Daniel reflects on act
        		case 16:
        			PlayVisionVoice("CH02L24_DanielDiary07_03_",12,12, nameof(VisionVoiceOver));
        			bPauseAtEvent = true;
        			break;

        		////////////////////////
        		// Door opens,
        		case 17:
        			// open door slowly
        			SetSwingDoorLocked("cellar_wood01_cellar_wood01_1", false, false);
        			SetSwingDoorClosed("cellar_wood01_cellar_wood01_1", false, false);
        			SetSwingDoorDisableAutoClose("cellar_wood01_cellar_wood01_1", true);
        			OpenSwingDoor(true, "cellar_wood01_cellar_wood01_1", 0.1f, 15, -100, 0, -100, "world");
        			PlaySoundAtEntity("creak", "24_joint.snt", "Player", 2.0f, false);

        			SetEntityActive("AreaLeaveVision", true);
        			bPauseAtEvent = true;
        			break;
        		////////////////////////
        		// Player leaves, fade to black
        		case 18:
        			fEventSpeed = 3;
        			SetEntityActive("AreaLeaveVision", false);

        			RemoveItem("ceremony_knife");

        			PlayGuiSound("general_wind_whirl", 0.5f);
        			StartEffectFlash(2, 8, 2);
        			FadePlayerFOVMulTo(3,0.1f);
        			SetPlayerLookSpeedMul(0.3f);
        			SetPlayerMoveSpeedMul(0.3f);
        			FadeOut(fEventSpeed);
        			StopMusic(8.0f, 10);

        			//Fade out blood light.
        			FadeLightTo("PointLight_Blood", 0,0,0,0, -1, 1);
        			RemoveTimer("heart");
        			StopSound("amb_noise", 6.0f);
        			break;
        		////////////////////////
        		// 12. Narration: Epilogue
        		case 19:
        			////////////////////////
        			// Restore previous state
        			SetEntityActive("ceremony_knife_dropped", false);

        			SetLightsOutsideRitualRoomVisible(true);
        			SetToolsVisible(true);

        			SetPlayerJumpDisabled(false);
        			SetPlayerCrouchDisabled(false);

        			SetInventoryDisabled(false);
        			SetLanternDisabled(false);
        			SetSanityDrainDisabled(false);

        			RestoreLanternState();

        			FadeRadialBlurTo(0,1);
        			FadeSepiaColorTo(0, 0.1f);

        			FadePlayerFOVMulTo(1,1);
        			SetPlayerLookSpeedMul(1);
        			SetPlayerMoveSpeedMul(1);
        			SetPlayerRunSpeedMul(1);

        			SetPlayerActive(false);

        			PlayVisionVoice("CH02L24_DanielDiary07_03_",13,13,nameof(VisionVoiceOver));
        			bPauseAtEvent = true;
        			break;
        		////////////////////////
        		// 13. Fade in and normal gameplay continue
        		case 20:
        			SetLocalVarInt("VisionActive", 0);
        			AddTimer("AreaRock_", RandFloat(1.0f, 5.0f), nameof(TimerRandRockAmb));

        			PlaySoundAtEntity("AreaAmb", "24_amb.snt", "AreaAmb", 4.0f, true);
        			PlaySoundAtEntity("AreaDrop_1", "general_cave_drip.snt", "AreaDrop_1", 0.0f, true);
        			PlaySoundAtEntity("AreaDrop_2", "general_cave_drip.snt", "AreaDrop_2", 0.0f, true);
        			PlaySoundAtEntity("AreaDrop_3", "general_cave_drip.snt", "AreaDrop_3", 0.0f, true);
        			PlaySoundAtEntity("AreaDrop_4", "general_cave_drip.snt", "AreaDrop_4", 0.0f, true);
        			PlayMusic("amb_extra01.ogg", true, 0.8f, 4.0f, 0, true);
        			PlayGuiSound("flashback_flash.snt", 1.0f);
        			FadeGlobalSoundVolume(1.0f, 2.0f);

        			AddTimer("AreaPedals_1", 0.1f, nameof(TimerShowPetals));
        			AddTimer("AreaPedals_1", 0.8f, nameof(TimerShowPetals));
        			AddTimer("AreaPedals_1", 1.5f, nameof(TimerShowPetals));
        			AddTimer("AreaPedals_1", 2.5f, nameof(TimerShowPetals));

        			GiveSanityDamage(35, false);

        			TeleportPlayer("PlayerStartArea_VisionEnd");

        			SetPlayerActive(true);

        			fEventSpeed = 2;
        			FadeIn(fEventSpeed);
        			break;
        	}

        	if(GetLocalVarInt(sEvent) <= iMaxEventStep && bPauseAtEvent==false)
        	{
        		ResumeEventTimer(sEvent, fEventSpeed);
        	}
        }


        //----
        void TimerHeartSound(string asTimer)
        {
        	PlayGuiSound("sanity_heartbeat.snt", 0.5f);

        	AddTimer("heart", 3.0f, nameof(TimerHeartSound));
        }
        //------------------------------------------

        void TimerShowPetals(string asTimer)
        {
        	CreateParticleSystemAtEntity("petals1", "ps_rose_petals.ps", asTimer, false);

        	if(GetLocalVarInt("VisionEnd") == 2)
        		PlayGuiSound("general_wind_whirl.snt", 0.5f);

        	AddLocalVarInt("VisionEnd", 1);
        }

        //------------------------------------------


        ////////////////////////////////////////////
        // Player exits room
        void PlayerExitVision(string asParent, string asChild, int alState)
        {
        	ResumeEventTimer(nameof(TimerRitualVision), 0);
        }

        //------------------------------------------

        /////////////////////////
        //ALEXANDER VOICE
        /////////////////////////

        //------------------------------------------

        void CollideWithVoiceArea(string asParent, string asChild, int alState)
        {
        	return; //Moved to 24 east beginning instead.

        	PlayEffectVoice("CH02L24_Alexander_", "Voice",1, 4, "");
        }

        //------------------------------------------


        void TimerRandRockAmb(string asTimer)
        {
        	if(GetLocalVarInt("VisionActive") == 1) return;

        	int iRand = RandInt(1, 4);
        	int iSound = RandInt(1, 2);

        	if(iSound == 1)
        		PlaySoundAtEntity("AreaRock_"+iRand, "24_rock_move.snt", "AreaRock_"+iRand, RandFloat(0.0f, 2.0f), false);
        	else
        		PlaySoundAtEntity("AreaRock_"+iRand, "24_chain_rattle.snt", "AreaRock_"+iRand, RandFloat(0.0f, 2.0f), false);

        	AddTimer(asTimer, RandFloat(8.0f, 16.0f), nameof(TimerRandRockAmb));
        }

        void InteractTableDesc(string asEntity)
        {
        	PlaySoundAtEntity(asEntity, "general_chain_rattle_single.snt", asEntity, 0.0f, false);
        	SetMessage("Ch02Level24", "DescTable", 0);
        	AddTimer(asEntity, 1.0f, nameof(TimerInteractBackOn));
        }
        void TimerInteractBackOn(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractTableDesc), true);
        	StopSound(asTimer, 0.5f);
        }
        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	AddTimer("AreaRock_", RandFloat(1.0f, 5.0f), nameof(TimerRandRockAmb));

        	SetMapDisplayNameEntry("ChoirWest");

        	if(ScriptDebugOn())
        	{
        		//SetPlayerSanity(20.0f);
        		GiveItemFromFile("lantern", "lantern.ent");
        	}

        	//SOUNDS
        	PlaySoundAtEntity("AreaAmb", "24_amb.snt", "AreaAmb", 0.1f, true);
        	PlaySoundAtEntity("AreaDrop_1", "general_cave_drip.snt", "AreaDrop_1", 0.0f, true);
        	PlaySoundAtEntity("AreaDrop_2", "general_cave_drip.snt", "AreaDrop_2", 0.0f, true);
        	PlaySoundAtEntity("AreaDrop_3", "general_cave_drip.snt", "AreaDrop_3", 0.0f, true);
        	PlaySoundAtEntity("AreaDrop_4", "general_cave_drip.snt", "AreaDrop_4", 0.0f, true);

        	// COLLIDE CALLBACKS
        	AddEntityCollideCallback("Player", "AreaAlexanderVoice", nameof(CollideWithVoiceArea), true, 1);
        	AddEntityCollideCallback("Player", "AreaLeaveVision", nameof(PlayerExitVision), true, 1);

        	SetEntityPlayerInteractCallback("AreaPrisonerMarks", nameof(InteractWithPrisoner), true);


        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("24_amb_noise"); PreloadSound("24_mb_01"); PreloadSound("24_paint"); PreloadSound("24_mb_02");
        	PreloadSound("24_mb_03"); PreloadSound("24_cut"); PreloadSound("24_pick_body"); PreloadSound("flashback_flash");
        	PreloadSound("24_rock_move"); PreloadSound("24_chain_rattle"); PreloadSound("general_chain_rattle_single"); PreloadSound("general_cave_drip");
        	PreloadSound("24_mb_04"); PreloadSound("react_breath"); PreloadSound("sanity_heartbeat"); PreloadSound("general_wind_whirl");
        	PreloadSound("react_scare");

        	PlayMusic("23_amb02.ogg", true, 0.7f, 4.0f, 0, true);
        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {

        }
    }
}
