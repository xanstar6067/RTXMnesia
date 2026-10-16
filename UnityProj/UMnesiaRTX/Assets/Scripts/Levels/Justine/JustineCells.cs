using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Justine
{
    [HplMapScript("maps/ptest/01_cells.map")]
    public class JustineCells : HplScript
    {
        ////////////////////////////////////////////
        // INTRO EVENT WITH MONSTER AND ROOM EXAMINE

        // Intro setup
        void IntroSetup(bool bIntroActive)
        {
        	if(bIntroActive)
        	{
        		FadeOut(0.0f);
        		SetPlayerRunSpeedMul(0.0f);
        		SetPlayerMoveSpeedMul(0.0f);
        		SetPlayerLookSpeedMul(0.03f);
        		SetPlayerCrouching(true);
        		SetPlayerCrouchDisabled(true);
        		SetPlayerJumpDisabled(true);
        		SetInventoryDisabled(true);
        		MovePlayerHeadPos(0.5f, -0.3f, 0.0f, 1.0f, 0.1f);
        		StartPlayerLookAt("AreaIntroLook_1", 10.0f, 10.0f, "");
        		
        		AddTimer(nameof(TimerEIntro), 2.0f, nameof(TimerEIntro));
        	}
        	
        	else
        	{
        		SetEntityActive("enemy_suitor_1", false);
        	}
        }

        // Patrol route for enemy when waking up
        void Enemy1Patrol()
        {
        	AddEnemyPatrolNode("enemy_suitor_1", "PathNodeArea_39", 0.0f, "");
        	AddEnemyPatrolNode("enemy_suitor_1", "PathNodeArea_38", 0.0f, "");
        	AddEnemyPatrolNode("enemy_suitor_1", "PathNodeArea_18", 0.0f, "");
        }

        // Intro sequence
        void TimerEIntro(string asTimer)
        {
        	string sEvent = asTimer;	//Do not edit, sets timer loop function name.
        	AddLocalVarInt(sEvent, 1);	//Do not edit, steps through timer loop events.
        	bool bPauseAtStep = false;	//Do not edit, to pause or end the timer loop at specified step.

        	float fEventSpeed = 1.0f;	//Set to preferred default time for the timer loop.
        	
        	// Player wakes up on bed, hears enemy, looks briefly at enemy, passes out and falls out of bed
        	// wakes up and gets up and game begins
        	switch(GetLocalVarInt(sEvent))
        	{
        		case 1:
        			PlayGuiSound("justine_wake2.ogg", 1.0f);
        			FadeIn(4.0f);
        			StartPlayerLookAt("AreaIntroLook_2", 0.05f, 0.05f, "");
        			FadePlayerRollTo(-45, 0.8f, 8);
        			fEventSpeed = 2.0f;
        		break;
        		
        		case 2:
        			StartPlayerLookAt("AreaIntroLook_2", 0.2f, 0.2f, "");
        			Enemy1Patrol();
        			fEventSpeed = 2.0f;
        		break;
        		
        		case 3:
        			PlayGuiSound("justine_faint.ogg", 0.7f);
        			FadeOut(3.0f);
        		break;
        		
        		case 4:
        			FadeIn(6.0f);
        			SetPlayerLookSpeedMul(0.06f);
        			StartPlayerLookAt("enemy_suitor_1", 0.4f, 0.4f, "");
        		break;
        		
        		case 5:
        			FadeOut(8.0f);
        			PlayGuiSound("justine_rapid_breaths.ogg", 0.8f);
        			fEventSpeed = 4.0f;
        		break;
        		
        		case 6:
        			StartPlayerLookAt("AreaIntroLook_4", 0.2f, 0.2f, "");
        			FadePlayerRollTo(0, 0.8f, 4);
        			fEventSpeed = 3.0f;
        		break;
        		
        		case 7:
        			FadeIn(4.0f);
        			SetPlayerLookSpeedMul(0.1f);
        			StartPlayerLookAt("AreaIntroLook_3", 0.6f, 0.6f, "");
        			FadePlayerRollTo(30, 15, 30);
        			PlayGuiSound("player_female_climb.snt", 1.0f);
        			PlayGuiSound("justine_breath1.ogg", 0.7f);
        			fEventSpeed = 1.5f;
        		break;	
        		
        		case 8:
        			FadeIn(3.0f);
        			StartPlayerLookAt("AreaIntroLook_5", 0.5f, 0.5f, "");
        			SetRadialBlurStartDist(0.3f);
        			FadeRadialBlurTo(0.1f, 0.1f);
        			FadePlayerRollTo(0, 5, 10);
        			SetPlayerCrouching(false);
        			MovePlayerHeadPos(-1.0f, -0.3f, 0.0f, 1.0f, 0.1f);
        			PlayGuiSound("justine_breath2.ogg", 0.6f);
        			fEventSpeed = 1.5f;
        		break;	
        		
        		case 9:
        			StartPlayerLookAt("AreaIntroLook_2", 2, 2, "");
        			SetPlayerCrouching(true);
        			PlayGuiSound("player_female_climb.snt", 1.0f);
        			FadeOut(3.0f);
        		break;
        		
        		case 10:
        			StartPlayerLookAt("AreaIntroLook_1", 0.5f, 0.5f, "");
        			MovePlayerHeadPos(0, -0.3f, 0.0f, 1.0f, 0.1f);
        			PlayGuiSound("justine_faint.ogg", 1.0f);
        			PlayGuiSound("player_stand.snt", 1);
        			PlayGuiSound("step_sneak_female_dirt4.ogg", 0.8f);
        			AddTimer("extra", 1.0f, nameof(TimerExtraSound));
        			fEventSpeed = 4.0f;
        		break;
        		
        		case 11:
        			TeleportPlayer("PlayerStartArea_2");
        			StartPlayerLookAt("AreaIntroLook_3", 10, 10, "");
        		break;
        		
        		case 12:
        			SetRadialBlurStartDist(0.1f);
        			FadeIn(4.0f);
        			PlayGuiSound("justine_wake1.ogg", 0.8f);
        			fEventSpeed = 4.0f;
        		break;
        		
        		case 13:
        			FadeRadialBlurTo(0, 0.01f);
        			FadePlayerRollTo(-45, 0.8f, 8);
        			StartPlayerLookAt("AreaIntroLook_2", 0.1f, 0.1f, "");
        			fEventSpeed = 3.0f;
        		break;
        		
        		case 14:
        			SetPlayerMoveSpeedMul(0.1f);
        			SetPlayerLookSpeedMul(0.1f);
        			FadePlayerRollTo(0, 0.4f, 4);
        			MovePlayerHeadPos(0.5f, -0.3f, 0.0f, 1.0f, 0.1f);
        			PlayGuiSound("step_sneak_female_dirt.snt", 1);
        			PlayGuiSound("player_female_climb.snt", 1.0f);
        		break;
        		
        		case 15:
        			SetPlayerMoveSpeedMul(0.2f);
        			SetPlayerLookSpeedMul(0.3f);
        			MovePlayerHeadPos(-0.5f, -0.3f, 0.0f, 1.0f, 0.1f);
        			PlayGuiSound("justine_breath3.ogg", 0.6f);
        			PlayGuiSound("step_sneak_female_dirt.snt", 1);
        			SetPlayerCrouching(false);
        		break;
        		
        		case 16:
        			SetPlayerMoveSpeedMul(0.4f);
        			SetPlayerLookSpeedMul(0.5f);
        			StartPlayerLookAt("AreaIntroLook_4", 1.2f, 1.2f, "");
        			PlayGuiSound("step_sneak_female_dirt.snt", 1);
        			PlayGuiSound("player_female_climb.snt", 1.0f);
        			MovePlayerHeadPos(0, 0, 0.0f, 0.5f, 0.1f);
        		break;
        		
        		case 17:
        			PlayGuiSound("justine_breath1.ogg", 0.8f);
        			SetPlayerMoveSpeedMul(0.6f);
        			SetPlayerLookSpeedMul(0.8f);
        			FadePlayerRollTo(0, 2, 8);
        		break;
        		
        		case 18:
        			SetPlayerMoveSpeedMul(0.8f);
        			SetPlayerLookSpeedMul(1.0f);
        			SetPlayerCrouchDisabled(false);
        			SetPlayerJumpDisabled(false);
        			SetInventoryDisabled(false);
        			AddTimer("more", 0.1f, nameof(TimerDoBlurPounding));
        			StopPlayerLookAt();
        		break;
        		
        		//////////
        		//End timer when no more steps in the event found.
        		default:
        			bPauseAtStep = true;
        		break;
        	}

        	if(!bPauseAtStep)
        		AddTimer(sEvent, fEventSpeed, sEvent);
        }

        void TimerExtraSound(string asTimer)
        {
        	PlayGuiSound("player_bodyfall.snt", 0.5f);
        }

        void TimerDoBlurPounding(string asTimer)
        {
        	if(asTimer == "more")
        	{
        		SetRadialBlurStartDist(0.7f);
        		FadeRadialBlurTo(0.2f, 0.1f);
        		AddTimer("less", 2, nameof(TimerDoBlurPounding));
        	}
        	else
        	{
        		FadeRadialBlurTo(0.01f, 0.04f);
        		AddTimer("more", 5, nameof(TimerDoBlurPounding));
        	}
        }
        //////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////
        //FIRST CELL INTERACTION WITH PHONO AND LANTERN TO GET OUT

        // Player turned on the recording and the lantern lowers
        void StateChangePhono(string asEntity, int alState)
        {
        	SetWheelInteractionDisablesStuck(asEntity, false);
        	SetWheelStuckState(asEntity, alState, false);
        	
        	SetEntityInteractionDisabled("lantern_1", false);
        	
        	PlayPhono(1, 1, 4, asEntity);
        	
        	AddDebugMessage("State changed to: "+alState, false);
        }

        // Player picks up the lantern, triggers cogwheel and pulling of lock.
        void EntityCallPickLantern(string asEntity, string type)
        {
        	//Wheel begins to rotate
        	RotatePropToSpeed("ceiling_machinery_cog_1", 1, 2, 0, 0.3f, 0, false, "");
        	
        	PlaySoundAtEntity("clank", "L01_cogwheel_start", "AreaIntroLook_1", 2.0f, false);
        	PlaySoundAtEntity("clank2", "21_ignite", "AreaIntroLook_1", 0.0f, false);
        	PlaySoundAtEntity("rotate", "L01_cogwheel", "AreaIntroLook_1", 1.0f, false);
        	
        	//Rope begins to pull
        	AddTimer("pull1", 1.5f, nameof(TimerPullOutLock));
        	
        	//Lock is pull out of the door
        	AddTimer("pull2", 3.0f, nameof(TimerPullOutLock));
        	
        	//Stop rotating cogwheel
        	AddTimer("pull3", 4.5f, nameof(TimerPullOutLock));
        }

        // Timers for opening the door.
        void TimerPullOutLock(string asTimer)
        {
        	if(asTimer == "pull1")
        	{
        		SetWheelStuckState("valve_iron_1", 0, false);								
        		SetMoveObjectState("safety_normal_vert_1", 0);
        		InteractConnectPropWithMoveObject("test", "valve_iron_1", "safety_normal_vert_1", true, false, 0);
        		
        		PlaySoundAtEntity("strain", "joint_crank_well_move", "AreaRopeSound",1.0f, false);
        	}
        	
        	if(asTimer == "pull2")
        	{
        		SetPropStaticPhysics("stone_chipper_1", false);
        		SetSwingDoorLocked("prison_section_6", false, true);
        		
        		SetPropHealth("padlock_2", 0);
        		
        		SetEntityPlayerInteractCallback("prison_section_6", "", false);	//Remove locked message.
        		
        		StopSound("strain", 1.0f);
        		
        		PlaySoundAtEntity("clankbank", "impact_metal_high", "AreaIntroLook_2", 0.0f, false);
        		PlaySoundAtEntity("clankbank", "07_pick_lock", "AreaIntroLook_2", 0.0f, false);
        	}
        	
        	if(asTimer == "pull3")
        	{
        		RotatePropToSpeed("ceiling_machinery_cog_1", 0.5f, 0, 0, 1, 0, false, "");
        		
        		PlaySoundAtEntity("stoprotate", "05_attach_ladder", "AreaIntroLook_1", 0.0f, false);
        		StopSound("rotate", 2.0f);
        	}
        }

        //////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////
        // STRANGE SOUNDS IN START CORRIDOR

        // Starting 4 strange sounds when entering the corridor, stopping them when leaving, if they did not end before then.
        void CollideAreaStrangeSound(string asParent, string asChild, int alState)
        {
        	if(asChild == "AreaStrangeSoundActivate")
        	{
        		RemoveTimer("more"); RemoveTimer("less");
        		AddTimer("strangesound", RandFloat(1.0f,5.0f), nameof(TimerStrangeSounds));
        	}
        	else
        	{
        		SetPlayerRunSpeedMul(1.0f);
        		SetPlayerMoveSpeedMul(1.0f);
        		RemoveTimer("strangesound");
        		FadeRadialBlurTo(0, 0.05f);
        		PlayMusic("23_amb02.ogg", true, 0.6f, 4.0f, 0, true);	//Change background music
        	}
        	
        	AddDebugMessage("Player entered: "+asChild, false);
        }

        void TimerStrangeSounds(string asTimer)
        {
        	AddLocalVarInt("iStrangeSound", 1);
        	int iSound = GetLocalVarInt("iStrangeSound");
        	
        	FadeGlobalSoundSpeed(0.5f, 0);
        	
        	SetRadialBlurStartDist(0.5f);
        	FadeRadialBlurTo(0.1f, 0.3f);
        	
        	PlaySoundAtEntity("srock", "L01_rotating", "AreaStrangeSound_1", 0, false);
        	PlaySoundAtEntity("srock2", "L01_rotating", "Player", 3.0f, false);
        	PlaySoundAtEntity("stranges"+iSound, "L01_screams", "AreaStrangeSound_"+iSound, RandFloat(1.5f, 3.0f), false);
        	
        	AddTimer("stopsseffect", 4.0f, nameof(TimerStopEffect));
        	
        	//Do another effect loop 5 times
        	if(iSound < 5)
        		AddTimer("strangesound", RandFloat(8.0f, 14.0f), nameof(TimerStrangeSounds));
        		
        	AddDebugMessage("Play sound in: "+"AreaStrangeSound_"+iSound, false);
        }

        void TimerStopEffect(string asTimer)
        {
        	FadeGlobalSoundSpeed(1, 8);
        	
        	FadeRadialBlurTo(0.04f, 0.1f);
        }

        //////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////


        /////////////////////////////////////////////////////////
        // BLOWING WIND IN START CORRIDOR

        // Player enters the end of the corridor and wind is seen/heard
        void CollideAreaWindActivate(string asParent, string asChild, int alState)
        {
        	FadeGlobalSoundSpeed(0.5f, 1);
        	
        	PlaySoundAtEntity("winds", "general_wind_whirl.snt", "AreaWind", 4.0f, false);
        	CreateParticleSystemAtEntity("windp", "ps_dust_push_15", "AreaWind", false);
        	
        	StartScreenShake(0.005f, 2.0f, 6.0f, 3.0f);
        	
        	AddTimer("general_wind_whirl", 1.0f, nameof(TimerAreaWind));
        	
        	AddDebugMessage("Activate the Wind!", false);
        }

        void TimerAreaWind(string asTimer)
        {
        	FadeGlobalSoundSpeed(1, 8);
        	
        	PlaySoundAtEntity("wind2s", asTimer, "AreaWind_2", 2.0f, false);
        	CreateParticleSystemAtEntity("wind2p", "ps_dust_push_15", "AreaWind_2", false);
        }

        //////////////////////////////////////////////////////
        //////////////////////////////////////////////////////


        ////////////////////////////////////////////////////
        // ENEMY IN BASEMENT WALKING BY

        // Patrol route for enemy in basement
        void Enemy3Patrol()
        {
        	AddEnemyPatrolNode("enemy_suitor_3", "PathNodeArea_109", 0.0f, "");
        }

        // An enemy walks by the basement window.
        void CollideAreaBasementEnemyActivate(string asParent, string asChild, int alState)
        {
        	SetEntityActive("enemy_suitor_3", true);
        	
        	AddTimer("enemy3", 10.0f, nameof(TimerRemoveEnemy3));
        }

        void TimerRemoveEnemy3(string asTimer)
        {
        	SetEntityActive("enemy_suitor_3", false);
        	
        	AddTimer("working_man", RandFloat(1,3), nameof(TimerNoRush));
        }

        // A looping time playing sound as if enemy is working on something
        void TimerNoRush(string asTimer)
        {
        	int iEvent = RandInt(1, 5);
        	float fTime = 0.0f;
        	
        	switch(iEvent)
        	{
        		case 1:
        			PlaySoundAtEntity("bang", "L01_metal", "AreaStrangeSound_3", RandFloat(0.04f, 0.1f), false);
        			PlaySoundAtEntity("monster", "L01_suitor_working", "AreaStrangeSound_3", RandFloat(1, 4), false);
        			fTime = RandFloat(3.0f, 6.0f);
        		break;
        		
        		case 2:
        			PlaySoundAtEntity("bang", "L01_metal", "AreaStrangeSound_3", RandFloat(0.08f, 0.2f), false);
        			fTime = RandFloat(0.25f, 1.0f);
        		break;
        	
        		case 3:
        			PlaySoundAtEntity("bang", "L01_metal", "AreaStrangeSound_3", RandFloat(0.12f, 0.3f), false);
        			fTime = RandFloat(0.75f, 2.0f);
        		break;
        		
        		case 4:
        			PlaySoundAtEntity("monster", "L01_suitor_working", "AreaStrangeSound_3", RandFloat(1, 4), false);
        			PlaySoundAtEntity("bang", "L01_metal", "AreaStrangeSound_3", RandFloat(0.16f, 0.4f), false);
        			fTime = RandFloat(3.0f, 6.0f);
        		break;
        		
        		case 5:
        			PlaySoundAtEntity("bang", "L01_metal", "AreaStrangeSound_3", RandFloat(0.2f, 0.5f), false);
        			fTime = RandFloat(1.25f, 3.0f);
        		break;
        	}
        	
        	AddTimer("working_man", fTime, nameof(TimerNoRush));
        }

        /////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////


        ////////////////////////////////////////////////////
        // ENEMY ENCOUNTER MAIN ROOM

        // Patrol for enemy_suitor_2
        void Enemy2Patrol()
        {
        	AddEnemyPatrolNode("enemy_suitor_2", "PathNodeArea_71", 0.0f, "");
        	AddEnemyPatrolNode("enemy_suitor_2", "PathNodeArea_59", 0.0f, "");
        	AddEnemyPatrolNode("enemy_suitor_2", "PathNodeArea_43", 0.0f, "");
        	AddEnemyPatrolNode("enemy_suitor_2", "PathNodeArea_37", 0.0f, "");
        }

        // Player enters cell 4 and an enemy activates in cell 3
        void CollideAreaEnemyActivate(string asParent, string asChild, int alState)
        {
        	SetEntityActive("enemy_suitor_2", true);
        	SetEntityActive("AreaExtraEnemyWarning", true);
        	
        	SetSwingDoorDisableAutoClose("prison_section_2", true);
        	SetSwingDoorClosed("prison_section_2", false, false);
        	AddPropImpulse("prison_section_2", 0, 0, -100, "World");
        }

        // Player runs quickly out and in the direction of enemy, replay a sound to hint about coming horrors.
        void CollideAreaExtraEnemyWarning(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("boo", "suitor/enabled.snt", "AreaExtraEnemyWarningSound", 0.0f, false);
        	AddDebugMessage("Playing extra suitor sound!", false);
        }

        // Enemy has moved so far it is no use.
        void CollideAreaExtraEnemyWarningOff(string asParent, string asChild, int alState)
        {
        	SetEntityActive("AreaExtraEnemyWarning", false);
        	AddDebugMessage("Don't play a extra sound any longer!", false);
        }

        // Swing second door open if it is closed
        void CollideAreaEnemy2Door(string asParent, string asChild, int alState)
        {
        	if(GetSwingDoorState("prison_1") != 1)
        	{
        		SetSwingDoorDisableAutoClose("prison_1", true);
        		SetSwingDoorClosed("prison_1", false, false);
        		AddPropImpulse("prison_1", -25, 0, 0, "World");
        	}
        }

        //Enables new enemy event when this one has deactivated
        void EntityCallEnemyDisabled(string asEntity, string type)
        {
        	SetLocalVarInt("MainEnemyEvent", 1); 
        }

        //////////////////////////////////////////////////
        //////////////////////////////////////////////////


        /////////////////////////////////////////////////
        //MAIN ROOM CEILING HATCH

        // -------------------- USE THE LEVER -------------------------- //
        void StateChangeLever(string asEntity, int alState)
        {
        	SetLeverInteractionDisablesStuck(asEntity, false);
        	
        	if(alState == 1)
        	{
        		if(GetLocalVarInt("HatchOpen") == 0)
        		{
        			SetMoveObjectState("roof_pole_1", -0.06f);
        			
        			PlaySoundAtEntity("leverdown", "21_ignite", asEntity, 0.5f, false);
        			CreateParticleSystemAtEntity("hatchp", "ptest_dust_falling.ps", "hatch_ceiling_1", false);	
        			
        			AddTimer("leverrelease", 0.5f, nameof(TimerLadderImpactHatch));
        			AddTimer("hatchclosed1", 0.6f, nameof(TimerLadderImpactHatch));
        			AddTimer("hatchclosed2", 0.9f, nameof(TimerLadderImpactHatch));
        			
        			SetLeverStuckState(asEntity, 1, true);	//To avoid double activation, released in TimerLadderImpactHatch
        			
        			AddDebugMessage("Lever not locked as hatch closed!", false);
        			
        			AddLocalVarInt("LeverPullCount", 1);
        			
        			if(GetLocalVarInt("LeverPullCount") == 3)
        			{
        				AddTimer("break", 1, nameof(TimerBreakHatch));
        				
        				RemoveTimer("leverrelease"); //Lever stays stuck
        				
        				SetLocalVarInt("HatchOpen", 1);
        				
        				StateChangeLever(asEntity, alState);
        				
        				AddDebugMessage("Lever locked and hatch banged through!", false);
        			}
        			
        			return;
        		}
        		
        		SetMoveObjectState("roof_pole_1", -0.06f);
        		
        		SetLeverStuckState(asEntity, 1, true);

        		SetLightVisible("PointLight_58", false);
        		
        		PlayMusic("29_event_end", false, 0.8f, 1.0f, 10, true);
        		
        		RollDownLadder();
        		PierceBody();
        		
        		SetLocalVarInt("FirstPrisonInteract", 1);	//No more voice on cell door interact
        		
        		SetEntityActive("AreaCharStartTalk", false);
        		SetEntityActive("AreaCharStopTalk", false);
        		
        		DisableOtherSolutions("lever");
        		
        		AddDebugMessage("Lever down!", false);
        	}
        }

        void TimerLadderImpactHatch(string asTimer)
        {
        	//Release lever
        	if(asTimer == "leverrelease")
        	{
        		SetLeverStuckState("lever_simple01_1", 0, true);
        	}
        	
        	
        	// Hatch effects & prisoner reaction
        	if(asTimer == "hatchclosed1")
        	{		
        		PlaySoundAtEntity("bang1", "L01_hit_hatch", "hatch_ceiling_1", 0.0f, false);
        		PlaySoundAtEntity("bang2", "L01_hit_hatch02", "hatch_ceiling_1", 0.0f, false);
        		
        		if(GetCharacterTopicNotSpoken(GetLocalVarString("character")+"_terrorized"))
        		{
        			StartCharacterSpeak(GetLocalVarString("character")+"_terrorized", 1, false, true, true);
        			return;
        		}
        		
        		if(GetLocalVarInt("HatchOpen") == 1) return;  //Dono't play any interfering voices if about to pierce the poor guy.
        		
        		if(RandInt(0,2) == 0)
        			StartCharacterSpeak(GetLocalVarString("character")+"_scared", 3, true, false, true);
        		else if(RandInt(0,2) == 0)
        			StartCharacterSpeak(GetLocalVarString("character")+"_sobbing", 3, true, false, true);
        		else
        			StartCharacterSpeak(GetLocalVarString("character")+"_idle",5 , true, false, true);
        	}
        	
        	// Spike effetcs over prisoner
        	if(asTimer == "hatchclosed2")
        	{
        		CreateParticleSystemAtEntity("spikep", "ptest_dust_falling.ps", "roof_mount_1", false);	
        		PlaySoundAtEntity("crank", "L01_spike_stuck02", "AreaCharTalk", 0.0f, false);
        	}
        }

        // ---------------------- THE MOVEABLE LADDER -------------------------- //

        // Ladder rolls down, wheels spin and it all stops when finished and ladder area activates
        void RollDownLadder()
        {	
        	if(GetLocalVarInt("ManualLadderPlaced") == 1) return;	// Manual ladder attached, so do not roll down this ladder.

        	RotatePropToSpeed("cogwheel_tiny03_1", 1, 1, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_tiny03_2", 1, 1, 0, 0, -1, false, "");
        	
        	for(int i=1;i<5;i++)
        		SetMoveObjectState("ladder4m_tileable_"+i, -1.12f);
        		
        	AddTimer("stoproll", 9.0f, nameof(TimerStopRoll));
        	
        	PlaySoundAtEntity("laddermove", "L01_ladder_move", "AreaLadderSounds", 1.0f, false);
        	PlaySoundAtEntity("laddermove2", "L01_ladder_move02", "AreaLadderSounds", 1.0f, false);
        	PlaySoundAtEntity("beingmove", "L01_ladder", "AreaLadderSounds", 0.0f, false);
        }

        void TimerStopRoll(string asTimer)
        {
        	RotatePropToSpeed("cogwheel_tiny03_1", 0.5f, 0.5f, 0, 0, 0, false, "");
        	RotatePropToSpeed("cogwheel_tiny03_2", 0.5f, 0.5f, 0, 0, 0, false, "");
        	
        	SetEntityActive("LadderArea_1", true);
        	
        	StopSound("laddermove", 0.5f);
        	StopSound("laddermove2", 0.5f);
        	
        	PlaySoundAtEntity("stopmove", "L01_ladder", "AreaLadderSounds", 0.0f, false);
        }


        // ------------------ BODY ON TABLE DIES ---------------------------- //
        void PierceBody()
        {
        	RotatePropToSpeed("roof_wheel_1", 1, 1, 0, 0, -1, false, "");
        	RotatePropToSpeed("roof_wheel_2", 1, 1, 0, 0, 1, false, "");
        	
        	SetMoveObjectState("roof_pole_1", -0.17f);
        	 
        	AddTimer("pierce1", 2.8f, nameof(TimerStopPierce));
        	
        	StartCharacterSpeak(GetLocalVarString("character")+"_dying", 1, false, true, true);

        	PlaySoundAtEntity("swomp", "L01_pierce01", "AreaCharTalk", 0.0f, false);
        	PlaySoundAtEntity("swomp2", "L01_pierce02", "AreaCharTalk", 0.0f, false);
        	PlaySoundAtEntity("down", "27_tunnel", "AreaCharTalk", 0.0f, false);
        }

        void TimerStopPierce(string asTimer)
        {
        	PlaySoundAtEntity("splatt", "L01_spike_body", "AreaCharTalk", 0.0f, false);
        	PlaySoundAtEntity("splatt2", "24_cut", "AreaCharTalk", 0.0f, false);
        	
        	CreateParticleSystemAtEntity("blodp", "blood.ps", "AreaCharDeath", false);
        	CreateParticleSystemAtEntity("blodp2", "blood.ps", "AreaCharDeath", false);
        	
        	SetPropActiveAndFade("barrel02_1", true, 2);
        	SetPropActiveAndFade("barrel02_2", true, 2);
        	SetPropActiveAndFade("barrel02_3", true, 2);
        	SetMoveObjectState("barrel02_1", 0.06f);
        	
        	PlayPropAnimation("ritual_prisoner_noillum_1", "death", 0.2f, false, "");
        	
        	StopSound("down", 0.5f);
        	
        	AddTimer("stopsplattp", 0.2f, nameof(TimerStopPierceSound));
        	AddTimer("stopsplatt", 2.0f, nameof(TimerStopPierceSound));
        	AddTimer("stopsplatt2", 3.7f, nameof(TimerStopPierceSound));
        	
        	StartCharacterSpeak(GetLocalVarString("character")+"_death", 1, false, true, true);
        	
        	SetGlobalVarInt("VictorIsDead", 1);
        	
        	AddKeyPart(unchecked((int)0xF1fa3164));
        	AddKeyPart(0x65e5431a);
        	
        	RotatePropToSpeed("roof_wheel_1", 0.7f, 0.7f, 0, 0, 0, false, "");
        	RotatePropToSpeed("roof_wheel_2", 0.7f, 0.7f, 0, 0, 0, false, "");
        }

        void TimerStopPierceSound(string asTimer)
        {
        	if(asTimer == "stopsplatt2")
        	{
        		SetMoveObjectState("barrel02_1", 0.0f);
        		return;
        	}
        	
        	if(asTimer == "stopsplattp")
        	{
        		SetMoveObjectState("barrel02_1", 0.1f);
        		CreateParticleSystemAtEntity("blodp3", "blood.ps", "AreaCharDeath", false);
        		return;
        	}
        	SetMoveObjectState("barrel02_1", 0.06f);
        	StopSound("splatt2", 0.5f);
        }

        // ---------------- STACKING AND MANUAL LADDER ------------------------- //
        void InteractCeilingHatch(string asEntity)
        {
        	SetEntityActive("AreaBangOnHatch", false);	//Removing area to check if something is thrown at hatch
        	
        	AddTimer("break", 0.5f, nameof(TimerBreakHatch));
        }

        // Interacting with the hole, gives a hint
        void InteractLadderArea(string asEntity)
        {
        	SetMessage("L01Cells", nameof(InteractLadderArea), 0);
        }

        // Using ladder item on the hole, activates a ladder entity and ladder area
        void UseLadderOnArea(string asItem, string asEntity)
        {
        	RemoveItem(asItem);
        	
        	SetPropActiveAndFade("ladder_static_1", true, 0.5f);
        	SetEntityActive("LadderArea_2", true);
        	
        	SetEntityActive("block_box_6", false);	//No need for this extra "step" when having this ladder.
        	SetEntityActive("AreaAttachLadder", false);
        	
        	PlaySoundAtEntity("attach", "05_attach_ladder", "ladder_static_1", 0.0f, false);
        	
        	DisableOtherSolutions("ladder");
        	
        	//Remove use callback, not needed but incase I forget and change something.
        	if(asEntity == "AreaAttachLadder")
        		RemoveUseItemCallback("LadderOnHatch");
        	else
        		RemoveUseItemCallback("LadderOnArea");
        }

        // -------------------------- THROW SOMETHING TO OPEN THE HATCH ------------------- //

        // Hatch left outside a small area, so something was thrown at it.
        void CollideAreaBangOnHatch(string asParent, string asChild, int alState)
        {
        	AddTimer("break", 0.1f, nameof(TimerBreakHatch));
        	
        	AddDebugMessage("Hatch Left Area! Break!", false);
        }

        // ------------------------- THE HATCH EFFECTS AND STUFF -------------------- //

        // Open ceiling hatch
        void TimerBreakHatch(string asTimer)
        {
        	SetLeverStuckState("hatch_ceiling_1", 0, false);
        	AddPropImpulse("hatch_ceiling_1", 0, -1, 0, "World");
        	
        	SetLocalVarInt("HatchOpen", 1);
        	
        	AddEntityCollideCallback("hatch_ceiling_1", "AreaHatchEffects", nameof(CollideAreaHatchEffects), true, 1);
        	
        	AddDebugMessage("Break!", false);
        }

        // Some dust and sound when hatch opens
        void CollideAreaHatchEffects(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("hatchs", "door_sewer_open", "hatch_ceiling_1", 0.0f, false);
        	CreateParticleSystemAtEntity("hatchp", "ps_dust_impact", "hatch_ceiling_1", false);
        }

        // Hatch is fully open, so it can not be interacted with any longer to avoid problems
        void CollideAreaHatchDisabled(string asParent, string asChild, int alState)
        {
        	SetEntityInteractionDisabled("hatch_ceiling_1", true);
        	
        	SetEntityActive("AreaAttachLadder", true);	//Possible to attach ladder and also a hint message on interact
        		
        	//Make it possible to attach area using hatch, because it is easy to click on it by mistake.
        	AddUseItemCallback("LadderOnHatch", "ladder_item_1", "hatch_ceiling_1", nameof(UseLadderOnArea), true);
        	
        	AddDebugMessage("No more hatch interact!", false);
        }

        // ----------------- DISABLE SOLUTION CONFLICTS -------------------------- //
        void DisableOtherSolutions(string sSolution)
        {
        	//Lever was used, so disable all conflicts from other solutions
        	if(sSolution == "lever")
        	{
        		//These does not really conflict, but felt it was a bit messy with two ladders etc.
        		SetEntityActive("AreaAttachLadder", false);
        		SetEntityActive("AreaHatchDisabled", false);
        		RemoveUseItemCallback("LadderOnHatch");
        	}
        	
        	//Ladder used, don't move down the moveable ladder, but still kill the poor guy if lever pulled.
        	if(sSolution == "ladder")
        	{
        		SetLocalVarInt("ManualLadderPlaced", 1);
        		SetEntityActive("AreaLadderHint", false);
        	}
        	
        	AddDebugMessage("Disabled solutions after using: "+sSolution, false);
        }
        /////////////////////////////////////////////////
        /////////////////////////////////////////////////


        /////////////////////////////////////////////////
        // CHARACTER INTERACTION

        // -------------- START/STOP COLLIDE AREAS --------------------------- //

        //Character start talking
        void CollideCharacterStartTalk(string asParent, string asChild, int alState)
        {
        	//Only run function if outside talk range
        	if(GetLocalVarInt("PlayerInCharTalkRange")==1) return; 
        	SetLocalVarInt("PlayerInCharTalkRange",1);
        	
        	if(GetCharacterTopicNotSpoken(GetLocalVarString("character")+"_discover"))
        		StartCharacterSpeak(GetLocalVarString("character")+"_discover", 1, false, true, true);
        	else
        		StartCharacterSpeak(GetLocalVarString("character")+"_idle", 5, true, false, false);
        		
        	AddDebugMessage("INSIDE START TALK", false);
        }

        //Character stop talking
        void CollideCharacterStopTalk(string asParent, string asChild, int alState)
        {
        	//Only run function is inside talk range
        	if(GetLocalVarInt("PlayerInCharTalkRange")==0) return; 
        	SetLocalVarInt("PlayerInCharTalkRange",0);
        	
        	StopCharacterSpeak();
        	
        	AddDebugMessage("OUTSIDE STOP TALK", false);
        }

        // -------------- HELPER FUNCTIONS --------------------------- //

        //Check if character has spoken or not
        bool GetCharacterTopicNotSpoken(string asTopic)
        {
        	return GetLocalVarInt("SpokenVar_"+asTopic)==1 ? false : true;
        }

        //Set a topic to spoken
        void SetCharacterTopicSpoken(string asTopic)
        {
        	SetLocalVarInt("SpokenVar_"+asTopic, 1);	
        }

        //Set a topic to not spoken
        void SetCharacterTopicNotSpoken(string asTopic)
        {
        	SetLocalVarInt("SpokenVar_"+asTopic, 0);	
        }

        //Play the voice file as specified
        void PlayCharacterVoice(string asTopic, int alNumOfParts, bool abRandom, bool abSetAsSpoken, bool abForceNewTopic)
        {	
        	//Set up some settings
        	float fMinDist = 6;
        	float fMaxDist = 16;
        	string sSoundName = "";
        	
        	//Set the topic as spoken
        	if(abSetAsSpoken) SetCharacterTopicSpoken(asTopic);
        	
        	//Play random idle/scare/sob voice
        	if(abRandom)
        	{
        		if(GetLocalVarInt("DelayIdleVoice") == 1 && !abForceNewTopic) return;
        		
        		SetLocalVarInt("DelayIdleVoice", 1);
        		AddTimer("idle_voice", 8, nameof(TimerDelayIdleVoice));
        		
        		//Make sure to play each variation has been used before playing a varation again
        		int iCount = 0;
        		for(int i=1;i<=alNumOfParts;i++)
        		{
        			sSoundName = asTopic+"_0"+i;
        			
        			//Count variations already spoken
        			if(!GetCharacterTopicNotSpoken(sSoundName))
        				iCount++;
        			
        			//If all variations spoken, set them to not spoken and then speak a random voice
        			if(iCount == alNumOfParts)
        			{
        				for(int j=1;j<=alNumOfParts;j++) 
        					SetCharacterTopicNotSpoken(asTopic+"_0"+j);
        				
        				sSoundName = asTopic+"_0"+RandInt(1,alNumOfParts);
        				
        				AddEffectVoice(sSoundName+ ".ogg", "", "Voice", sSoundName, true, "AreaCharTalk", fMinDist, fMaxDist);
        				SetCharacterTopicSpoken(sSoundName);
        				
        				SetEffectVoiceOverCallback(nameof(CharacterTalkOver));
        				return;
        			}
        			
        			//If variation not spoken speak it.
        			else
        			{
        				if(GetCharacterTopicNotSpoken(sSoundName))
        				{
        					AddEffectVoice(sSoundName+ ".ogg", "", "Voice", sSoundName, true, "AreaCharTalk", fMinDist, fMaxDist);
        					SetCharacterTopicSpoken(sSoundName);
        					
        					SetEffectVoiceOverCallback(nameof(CharacterTalkOver));
        					return;
        				}
        			}
        		}
        	}
        	
        	//Play specific voice topic
        	else
        	{
        		//Start all voices in topic
        		for(int i=1; i<=alNumOfParts; ++i)
        		{
        			string sNum = i<10 ? "0"+i : ""+i;
        			sSoundName = asTopic+"_"+sNum;
        		
        			AddEffectVoice(sSoundName+ ".ogg", "", "Voice", sSoundName, true, "AreaCharTalk", fMinDist, fMaxDist);		
        		}
        	}
        	
        	//Callback when voices are done playing
        	SetEffectVoiceOverCallback(nameof(CharacterTalkOver));
        	
        	AddDebugMessage("Starting topic "+sSoundName, false);
        }

        void CharacterTalkOver(float fStopVoiceFade)
        {
        	//No more callback
        	SetEffectVoiceOverCallback("");
        	
        	//Stop the current voice playing
        	StopAllEffectVoices(fStopVoiceFade);
        	
        	AddDebugMessage("Message is over.", false);
        }

        void TimerDelayIdleVoice(string asTimer)
        {
        	SetLocalVarInt("DelayIdleVoice", 0);
        }

        // ---------------- START / STOP PHRASES --------------- //

        // Start character phrases
        void StartCharacterSpeak(string sTopicName, int iNr, bool abRandom,  bool abSetAsSpoken, bool abForceNewTopic)
        {
        	//If there is a voice active, then wait adding.
        	//If there is a flashback playing!
        	if(GetEffectVoiceActive() || GetFlashbackIsActive())
        	{
        		if(abForceNewTopic)
        		{
        			CharacterTalkOver(0);
        			PlayCharacterVoice(sTopicName, iNr, abRandom, abSetAsSpoken, abForceNewTopic);
        		}
        		
        		else
        			CharacterTalkOver(0.5f);

        		return;
        	}
        	
        	/////////////////////////////
        	// Play specific topic if no voice active
        	PlayCharacterVoice(sTopicName, iNr, abRandom, abSetAsSpoken, abForceNewTopic);
        }

        // Stop character phrases
        void StopCharacterSpeak()
        {
        	//No more callback
        	SetEffectVoiceOverCallback("");
        	
        	//Stop the current voice playing
        	StopAllEffectVoices(0.5f);
        	
        	AddDebugMessage("Message was stopped.", false);
        }
        /////////////////////////////////////////////////
        //////////////////////////////////////////////////


        ////////////////////////////////////////////////
        // ENEMY ENCOUNTER IF GOING BACK TO STAR CORRIDOR

        // Activate a new enemy if player goes back into corridor after the main hall enemy has gone in there and deactivated.
        void CollideAreaActivateCorridorEnemy(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("MainEnemyEvent") == 0 || GetLocalVarInt("MainEnemyEvent") == 2) 
        	{
        		AddDebugMessage("No corridor enemy!", false);
        		return;
        	}
        	
        	SetEntityActive("enemy_suitor_4", true);
        	
        	SetLocalVarInt("MainEnemyEvent", 2);
        	
        	AddDebugMessage("Corridor enemy activated! Run!", false);
        }

        void Enemy4Patrol()
        {
        	AddEnemyPatrolNode("enemy_suitor_4", "PathNodeArea_30", 10.0f, "IdleExtra1");
        	AddEnemyPatrolNode("enemy_suitor_4", "PathNodeArea_22", 0.0f, "");
        }

        //////////////////////////////////////////////////
        //////////////////////////////////////////////////


        //////////////////////////////////////////////////////
        // LOFT SCARE EVENT

        // Check if player goes back instead of running forward
        void CollideAreaEndLoftScares(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt(nameof(LoftScareStep)) < 1) return;
        		
        	LoftScareHaltEnd();
        	
        	SetEntityActive(asChild, false);	//To not trigger this end anymore
        }

        // Events that occur at each step in the scare as player runs.
        void CollideAreaLoftScares(string asParent, string asChild, int alState)
        {
        	AddLocalVarInt(nameof(LoftScareStep), 1);
        	
        	switch(GetLocalVarInt(nameof(LoftScareStep)))
        	{
        		case 1:
        			PlayMusic("29_amb02.ogg", true, 0.7f, 2.0f, 0, true);
        			LoftScareStep("L01_suitor_enabled.snt","AreaChangeAmbMusic");
        		break;
        		
        		case 2:
        			LoftScareStep("L01_suitor_notice_long.snt","AreaEndLoftScare");
        		break;
        		
        		case 3:
        			LoftScareStep("L01_suitor_notice.snt","ScriptArea_1");
        		break;
        		
        		case 4:
        			LoftScareStep("L01_suitor_alert.snt","ScriptArea_2");
        		break;
        		
        		case 5:
        			LoftScareStep("L01_suitor_hunt.snt","ScriptArea_3");
        			//SetEntityActive("AreaEndLoftScare", false);	//Don't do the end of loft scare any longer.
        		break;
        		
        		case 6:
        			//Used to be used for re-opening the hatch, but removed the closing of it.
        		break;
        	}
        }

        // General event base for all steps
        void LoftScareStep(string sSound, string sArea)
        {
        	AddLocalVarFloat("LoftStep", 0.05f);
        	
        	float fStep = GetLocalVarFloat("LoftStep");
        	
        	PlaySoundAtEntity("monster", sSound, sArea, 0.0f, false);
        	PlaySoundAtEntity("rocks", "L01_suitor_rock", sArea, 1-(fStep*2), false);
        	
        	CreateParticleSystemAtEntity("rockp", "ptest_dust_tunnel", sArea, false);
        	
        	StartScreenShake(fStep/10, fStep*4, fStep*1.5f, fStep*3);
        	
        	AddDebugMessage("Doing loft scare part: "+GetLocalVarInt(nameof(LoftScareStep)), false);
        }

        // Player turned around, not running from the scare, so ending it.
        void LoftScareHaltEnd()
        {
        	PlaySoundAtEntity("monster", "L01_suitor_hunt", "AreaEndHunt", 0.0f, false);
        	PlaySoundAtEntity("slam", "L01_hit_hatch", "AreaEndHunt", 0.0f, false);
        	PlaySoundAtEntity("rocks", "L01_suitor_rock", "AreaEndHunt", 0.0f, false);
        	CreateParticleSystemAtEntity("rockp", "ptest_dust_tunnel", "AreaEndHunt", false);
        	
        	PlayMusic("11_event_tree.ogg", false, 0.8f, 2.0f, 10, true);
        	
        	//Disable all areas except last
        	for(int i = 1;i<5;i++) 
        		SetEntityActive("ScriptArea_"+i, false);
        		
        	SetLocalVarInt(nameof(LoftScareStep), 5);	//This will unlock the hatch on last area, incase player wants to go back down for whatever reason.
        	
        	ActivateAlternateLoftEvent();
        	
        	SetEntityActive("AreaChangeAmbMusic_3", true);
        	
        	AddDebugMessage("Ending loft scare prematurely", false);
        }

        // Little bit of rumbles and view distortion
        void ActivateAlternateLoftEvent()
        {
        	SetLocalVarInt("iStrangeSound", 0);
        	AddTimer("strangesound", 14, nameof(TimerStrangeSounds));
        	
        	FadePlayerAspectMulTo(0.5f, 0.01f);
        	FadePlayerFOVMulTo(2, 0.01f);
        	
        	PlayMusic("29_amb.ogg", true, 0.6f, 2.0f, 0, true);
        	
        	AddDebugMessage("Alternate loft ambience activated.", false);
        }

        /////////////////////////////////////////////////////
        /////////////////////////////////////////////////////


        //////////////////////////////////////////////////
        // MISC CALLBACKS 

        // --------------------- LOCKED DOORS ------------------------- //

        // Interact messages for all locked doors - with a half-second delay
        void InteractLockedDoor(string asEntity)
        {
        	AddTimer(asEntity, 0.5f, nameof(TimerLockedDoor));
        	
        	if(asEntity == "prison_section_static_1" || asEntity == "hatch_drainage_1")
        		PlaySoundAtEntity("lockeds", "door_gate_bar_locked", asEntity, RandFloat(0.0f,1.0f), false);
        		
        	if(asEntity == "prison_section_static_1" && GetLocalVarInt("FirstPrisonInteract") != 1)
        	{
        		StartCharacterSpeak(GetLocalVarString("character")+"_idle", 5, true, false, true);
        		SetLocalVarInt("FirstPrisonInteract", 1);
        	}
        }
        void TimerLockedDoor(string asTimer)
        {
        	// Starting cell door - can be opened
        	if(asTimer == "prison_section_6")
        		SetMessage("L01Cells", "LockedStartDoor", 0);
        	
        	// Prisoner cell, floor hatch and end hall door - can never be opened
        	else
        		SetMessage("L01Cells", "LockedDoorAlways", 0);
        }

        // Hint when top of ladder
        void CollideAreaLadderHint(string asParent, string asChild, int alState)
        {
        	GiveHint("ladder_hint", "Hints", "L01Ladder", 0);
        }

        //Teleport player when top of ladder
        void CollideAreaLadderUp(string asParent, string asChild, int alState)
        {
        	PlayGuiSound("player_climb.snt", 0.5f);
        	FadeOut(0.5f);
        	AddTimer("teleport", 0.5f, nameof(TimerTeleport));
        }
        void TimerTeleport(string asTimer)
        {
        	SetPlayerCrouching(true);
        	TeleportPlayer("tunnel");
        	FadeIn(0.5f);
        }

        // ----------------------------- FIRE ------------------------------------ //

        // Enter fire and receive damage
        void CollideAreaFire(string asParent, string asChild, int alState)
        {
        	// Touching or entering fire is state 1
        	if(alState == 1)
        	{
        		GivePlayerDamage(5, "BloodSplat", false, false);
        		PlayGuiSound("15_player_burn", 0.8f);
        	
        		SetEntityActive(asChild, false);
        		
        		AddTimer(asChild, 1.0f, nameof(TimerFireBackOn));
        	}
        	
        	//If walking into fire, set var to check if player stays in fire
        	if(asParent == "Player")
        		SetLocalVarInt("InFire", alState);
        }

        // Touch fire and receive damage
        void InteractAreaFire(string asEntity)
        {
        	CollideAreaFire("", "AreaFire", 1);
        }
        //Touch iron by fire
        void InteractIron(string asEntity)
        {
        	CollideAreaFire("", "AreaFire", 1);
        }

        // Enable touch/enter fire again
        void TimerFireBackOn(string asTimer)
        {
        	//If staying in fire, loop a damage
        	if(GetLocalVarInt("InFire") == 1)
        	{
        		CollideAreaFire("", "AreaFire", 1);
        	}
        	
        	SetEntityActive(asTimer, true);
        }


        // ---------------------------- CHANGE AMB ------------------------- //

        //Change amb music during various parts of the level
        void CollideAreaChangeAmbMusic(string asParent, string asChild, int alState)
        {
        	if(asChild == "AreaChangeAmbMusic")
        		PlayMusic("29_amb.ogg", true, 0.6f, 2.0f, 0, true);
        	
        	//Turn it off if player leaves tunnel
        	else if(asChild == "AreaChangeAmbMusic_2" || asChild == "AreaChangeAmbMusic_3")
        	{
        		PlayMusic("23_amb02.ogg", true, 0.6f, 4.0f, 0, true);
        		
        		// Alternative tunnel event ending.
        		RemoveTimer("strangesound");	//Remove scary sound in tunnel if the event is active.
        		FadePlayerAspectMulTo(1, 0.1f);	//Restore view if it was distorted
        		FadePlayerFOVMulTo(1, 0.1f);	//Restore view if it was distorted
        		FadeRadialBlurTo(0, 0.05f);
        	}
        }


        // ----------------------------- PHONOGRAPH MESSAGES ------------------------- //

        // The phonograph in the last room
        void StateChangePhono02(string asEntity, int alState)
        {
        	PlayPhono(1, 2, 2, asEntity);
        }

        // Play the phonograph messages on interact with phonograph
        void PlayPhono(int iLevel, int iSet, int iPart, string sEntity)
        {
        	SetEntityInteractionDisabled(sEntity, true);	//No more interact with phono as it does not do anything.
        	
        	PlaySoundAtEntity("phono_scrape", "ptest_phono_loop", sEntity, 1.0f, false);
        	
        	SetLocalVarInt(nameof(PlayPhono), 1);
        	
        	for(int i = 1;i<=iPart;i++)
        		AddEffectVoice("justine_rec_lvl"+iLevel+"_0"+iSet+"_0"+i+".ogg", 
        			"", 
        			"Recording", "justine_rec_lvl"+iLevel+"_0"+iSet+"_0"+i, 
        			true, sEntity, 4, 16
        		);
        	
        	SetEffectVoiceOverCallback(nameof(VoiceOverPhono));
        }

        void VoiceOverPhono()
        {
        	SetLocalVarInt(nameof(PlayPhono), 0);
        	StopSound("phono_scrape", 2.0f);
        }

        //If running too far off from phono, stop it from playing.
        void CollideAreaStopPhono(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt(nameof(PlayPhono)) != 1) return;
        	
        	StopAllEffectVoices(2.0f);
        	VoiceOverPhono();
        }

        // ------------------------------- BIRDS FLY OFF ------------------------------ //
        void CollideAreaBirdsFly(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("wings", "general_birds_flee", asChild, 1.0f, false);
        }

        // ------------------------------- CELL MEMORY SOUNDS ------------------------------ //
        //A sound plays in each cell and vision blurs a little.
        void CollideAreaCell(string asParent, string asChild, int alState)
        {
        	//Don't play a vision sound when entering the cell that activates grunt, do it on the next enter instead.
        	if(asChild == "AreaCell_1")
        	{
        		AddLocalVarInt("AreaCell_1", 1);

        		if(GetLocalVarInt("AreaCell_1") == 1)
        		{
        			AddTimer("addcollide", 40, nameof(TimerStopCellSound));
        			return;
        		}
        	}
        	
        	for(int i=1;i<=6;i++) 
        		StopSound("AreaCell_"+i, 4.0f);
        	
        	StopMusic(4.0f, 0);
        	
        	//The specific "vision" sound effect for each cell.
        	if(asChild == "AreaCell_1")
        		PlaySoundAtEntity(asChild, "cell_felix", asChild, 4.0f, false);
        	if(asChild == "AreaCell_2")
        		PlaySoundAtEntity(asChild, "cell_victor", asChild, 4.0f, false);
        	if(asChild == "AreaCell_3")
        		PlaySoundAtEntity(asChild, "cell_hector", asChild, 4.0f, false);
        	if(asChild == "AreaCell_4")
        		PlaySoundAtEntity(asChild, "cell_malo", asChild, 4.0f, false);
        	if(asChild == "AreaCell_5")
        		PlaySoundAtEntity(asChild, "cell_basile", asChild, 4.0f, false);
        	if(asChild == "AreaCell_6")
        		PlaySoundAtEntity(asChild, "cell_alois", asChild, 4.0f, false);
        	
        	SetRadialBlurStartDist(0.5f);
        	FadeRadialBlurTo(0.1f, 0.01f);
        	
        	AddTimer(asChild, 12, nameof(TimerStopCellSound));
        }

        void TimerStopCellSound(string asTimer)
        {	
        	if(GetLocalVarInt("AreaCell_1") == 1 && asTimer == "addcollide")
        	{
        		AddLocalVarInt("AreaCell_1", 1);
        		AddEntityCollideCallback("Player", "AreaCell_1", nameof(CollideAreaCell), true, 1);
        		return;
        	}
        	
        	PlayMusic("23_amb02.ogg", true, 0.6f, 2.0f, 0, true);
        	FadeRadialBlurTo(0, 0.1f);
        	StopSound(asTimer, 4.0f);
        }

        //---------------------------- EXTRA WALL ROLL -------------------------------//
        void UseWaxOnPhono(string asItem, string asEntity)
        {
        	if(asEntity == "phonograph_1")
        	{
        		SetMessage("L01Cells", "Phono0102", 0);
        	}
        	
        	if(asEntity == "phonograph_2")
        	{
        		SetMessage("L01Cells", "Phono0102", 0);
        	}
        	
        	if(asEntity == "phonograph_3")
        	{
        		SetWheelStuckState("phonograph_3", 0, false);
        		PlaySoundAtEntity("", "12_make_drill.snt", asEntity, 0.0f,false);
        		SetEntityPlayerInteractCallback("phonograph_3", "", false);
        		RemoveItem(asItem);
        	}
        	
        }

        void InteractPhono03(string asEntity)
        {
        	SetMessage("L01Cells", "Phono03", 0);
        }

        void StateChangePhono03(string asEntity, int alState)
        {
        	if(alState != 1) return;
        	
        	AddKeyPart(unchecked((int)0xc3af2127));
        	AddKeyPart(unchecked((int)0xd4a52361));
        	AddDebugMessage("Play secret phono", false);
        	
        	SetEntityInteractionDisabled(asEntity, true);	//No more interact with phono as it does not do anything.
        	
        	PlaySoundAtEntity("phono_scrape", "ptest_phono_loop", asEntity, 1.0f, false);
        	
        	SetLocalVarInt(nameof(PlayPhono), 1);
        	
        	AddEffectVoice("brokenrec_justine_01.ogg", 
        			"", 
        			"Recording", "brokenrec_justine_01", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_basile_02.ogg", 
        			"", 
        			"Recording", "brokenrec_basile_02", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_justine_03.ogg", 
        			"", 
        			"Recording", "brokenrec_justine_03", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_basile_04.ogg", 
        			"", 
        			"Recording", "brokenrec_basile_04", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_justine_05.ogg", 
        			"", 
        			"Recording", "brokenrec_justine_05", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_basile_06.ogg", 
        			"", 
        			"Recording", "brokenrec_basile_06", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_justine_07.ogg", 
        			"", 
        			"Recording", "brokenrec_justine_07", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_basile_08.ogg", 
        			"", 
        			"Recording", "brokenrec_basile_08", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_basile_09.ogg", 
        			"", 
        			"Recording", "brokenrec_basile_09", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_basile_10.ogg", 
        			"", 
        			"Recording", "brokenrec_basile_10", 
        			true, asEntity, 4, 16
        		);
        	AddEffectVoice("brokenrec_justine_11.ogg", 
        			"", 
        			"Recording", "brokenrec_justine_11", 
        			true, asEntity, 4, 16
        		);	
        	AddEffectVoice("brokenrec_basile_12.ogg", 
        			"", 
        			"Recording", "brokenrec_basile_12", 
        			true, asEntity, 4, 16
        		);
        	
        	SetEffectVoiceOverCallback(nameof(VoiceOverPhono));
        }


        //---------------------------- Pick up notes -------------------------------//

        void PickupNote01(string asEntity, string asType)
        {
        	AddDebugMessage("Pickup note 01", false);
        	AddLocalVarInt("NoteCountNum", 100);
        }

        void PickupNote02(string asEntity, string asType)
        {
        	AddDebugMessage("Pickup note 02", false);
        	AddLocalVarInt("NoteCountNum", 300);
        }

        void PickupNote03(string asEntity, string asType)
        {
        	AddDebugMessage("Pickup note 03", false);
        	AddLocalVarInt("NoteCountNum", 37);
        }


        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	//////////////////////////////
        	// SET LEVEL NAME
        	SetMapDisplayNameEntry("L01Cells");
        	
        	
        	////////////////////////////
        	// ENTITY CONFIG
        	SetWheelStuckState("phonograph_3", -1, false);
        	AddUseItemCallback("onphono1", "waxroll_record_1", "phonograph_1", nameof(UseWaxOnPhono), false);
        	AddUseItemCallback("onphono2", "waxroll_record_1", "phonograph_2", nameof(UseWaxOnPhono), false);
        	AddUseItemCallback("onphono3", "waxroll_record_1", "phonograph_3", nameof(UseWaxOnPhono), false);
        	
        	SetPlayerPermaDeathSound("alois_death.snt");
        	
        	SetEntityInteractionDisabled("lantern_1", true);
        	SetMoveObjectState("safety_normal_vert_1", 1);
        	
        	InteractConnectPropWithRope("phonolantern", "phonograph_1", "RopeArea_2", true,
        		1, 1, 1,
        		false, 0
        	);
        	InteractConnectPropWithRope("lockrope", "valve_iron_1", "RopeArea_3", false,
        		3, 2, 3,
        		false, 0
        	);	
        	
        	
        	/////////////////////////////
        	// ENEMY PATHS
        	
        	// enemy_suitor_1
        	// Enemy1Patrol() found in intro event, active from start due to simple sound effects gain.
        	
        	// enemy_suitor_2
        	Enemy2Patrol();	//Main area enemy
        	
        	// enemy_suitor_3
        	Enemy3Patrol();	//Basement event
        	
        	// enemy_suitor_4
        	Enemy4Patrol();	//Start corridor if going back
        	
        	
        	/////////////////////////////
        	// COLLIDE CALLBACKS
        	AddEntityCollideCallback("Player", "AreaExtraEnemyWarning", nameof(CollideAreaExtraEnemyWarning), true, 1);
        	AddEntityCollideCallback("enemy_suitor_2", "AreaExtraEnemyWarningOff", nameof(CollideAreaExtraEnemyWarningOff), true, 1);
        	
        	AddEntityCollideCallback("Player", "AreaStopPhono_1", nameof(CollideAreaStopPhono), false, -1);
        	AddEntityCollideCallback("Player", "AreaStopPhono_2", nameof(CollideAreaStopPhono), false, -1);
        	AddEntityCollideCallback("Player", "AreaStopPhono_3", nameof(CollideAreaStopPhono), false, -1);
        	
        	for(int i=1;i<=6;i++)
        		AddEntityCollideCallback("Player", "AreaCell_"+i, nameof(CollideAreaCell), true, 1);
        		
        	AddEntityCollideCallback("Player", "AreaWindActivate", nameof(CollideAreaWindActivate), true, 1);
        	
        	AddEntityCollideCallback("Player", "AreaEnemyActivate", nameof(CollideAreaEnemyActivate), true, 1);
        	AddEntityCollideCallback("enemy_suitor_2", "AreaEnemy2Door", nameof(CollideAreaEnemy2Door), true, 1);
        	AddEntityCollideCallback("Player", "AreaStrangeSoundActivate", nameof(CollideAreaStrangeSound), true, 1);
        	AddEntityCollideCallback("Player", "AreaStrangeSoundDeactivate", nameof(CollideAreaStrangeSound), true, 1);
        	
        	AddEntityCollideCallback("Player", "AreaBasementEnemyActivate", nameof(CollideAreaBasementEnemyActivate), true, 1);
        	
        	AddEntityCollideCallback("hatch_ceiling_1", "AreaBangOnHatch", nameof(CollideAreaBangOnHatch), true, -1);
        	AddEntityCollideCallback("hatch_ceiling_1", "AreaHatchDisabled", nameof(CollideAreaHatchDisabled), true, 1);
        	
        	AddEntityCollideCallback("Player", "AreaCharStartTalk", nameof(CollideCharacterStartTalk), false, 1);	
        	AddEntityCollideCallback("Player", "AreaCharStopTalk", nameof(CollideCharacterStopTalk), false, -1);	
        	
        	AddEntityCollideCallback("Player", "AreaLadderHint", nameof(CollideAreaLadderHint), true, 1);
        	AddEntityCollideCallback("Player", "AreaLadderUp", nameof(CollideAreaLadderUp), false, 1);
        	
        	AddEntityCollideCallback("Player", "AreaActivateCorridorEnemy", nameof(CollideAreaActivateCorridorEnemy), false, 1);
        	
        	for(int i = 1;i<6;i++)
        		AddEntityCollideCallback("Player", "ScriptArea_"+i, nameof(CollideAreaLoftScares), true, 1);
        	
        	AddEntityCollideCallback("Player", "AreaEndLoftScare", nameof(CollideAreaEndLoftScares), false, 1);
        	
        	AddEntityCollideCallback("Player", "AreaFire", nameof(CollideAreaFire), false, 0);
        	
        	AddEntityCollideCallback("Player", "AreaChangeAmbMusic", nameof(CollideAreaChangeAmbMusic), true, 1);
        	AddEntityCollideCallback("Player", "AreaChangeAmbMusic_2", nameof(CollideAreaChangeAmbMusic), true, 1);
        	AddEntityCollideCallback("Player", "AreaChangeAmbMusic_3", nameof(CollideAreaChangeAmbMusic), true, 1);
        	
        	AddEntityCollideCallback("Player", "AreaBirdsFly", nameof(CollideAreaBirdsFly), true, 1);
        	
        	
        	////////////////////////////
        	// ENTITY CALLBACKS
        	AddUseItemCallback("LadderOnHole", "ladder_item_1", "AreaAttachLadder", nameof(UseLadderOnArea), true);
        	
        	
        	///////////////////////////
        	// VARIABLES
        	bool bIntroActive = true;	// Sets the intro active, disable in ScriptDebugOn if wanted.
        	SetGlobalVarInt("VictorIsDead", 0);	//Is set to 1 in PierceBody();
        	SetLocalVarString("character", "victor");	//Used to play voices for prisoner character in level
        	
        	
        	///////////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		//GiveItemFromFile("lantern", "lantern.ent");
        		//for(int i=0;i<20;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");
        		
        		///////////////
        		// TEST
        		//SetMessage("Temp", "TestCustomLang", 10);	//Testing custom lang file
        		//AddTimer("break", 3.0f, "TimerBreakHatch"); //Timer to test opening of hatch without solving puzzle.
        		//AddEntityCollideCallback("Player", "AreaEnemy2Door", "CollideAreaEnemy2Door", true, 1); //Testing impulse on a door to fake enemy opening it
        		bIntroActive = false;	//No intro on level start
        		
        		GiveItemFromFile("waxroll_record_1", "waxroll_record.ent");
        	}
        	
        	
        	//////////////////////////////////
        	//INTRO EVENT
        	IntroSetup(bIntroActive);
        	
        	
        	////////////////////////////////////
        	//DISABLE HINTS AND LANTER
        	BlockHint("LanternNoItem"); BlockHint("LanternNoOil"); 
        	SetPlayerLampOil(0);
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {	
        	SetupLoadScreen("LoadingText","Load02_",3,"ptest_gameloading_01.jpg");

        	PlayMusic("25_amb.ogg", true, 0.6f, 2.0f, 0, true);
        	
        	PreloadSound("L01_suitor_enabled.snt"); PreloadSound("L01_suitor_notice_long.snt"); PreloadSound("L01_suitor_notice.snt"); PreloadSound("L01_suitor_alert.snt");
        	PreloadSound("27_tunnel"); PreloadSound("L01_pierce02"); PreloadSound("L01_spike_body"); PreloadSound("24_cut");
        	PreloadSound("L01_pierce01"); PreloadSound("L01_ladder"); PreloadSound("L01_ladder_move02"); PreloadSound("L01_ladder_move");
        	PreloadSound("L01_suitor_working"); PreloadSound("L01_hit_hatch"); PreloadSound("L01_hit_hatch02"); PreloadSound("L01_spike_stuck02");
        	PreloadSound("L01_rotating"); PreloadSound("L01_screams"); PreloadSound("general_wind_whirl"); PreloadSound("L01_metal");
        	PreloadSound("impact_metal_high"); PreloadSound("L01_suitor_hunt.snt"); PreloadSound("L01_suitor_rock.snt"); PreloadSound("joint_crank_well_move");
        	PreloadSound("general_birds_flee.snt"); PreloadSound("L01_cogwheel_start"); PreloadSound("21_ignite"); PreloadSound("L01_cogwheel");
        	PreloadSound("ptest_phono_loop.snt"); PreloadSound("player_female_climb.snt"); PreloadSound("player_stand.snt"); PreloadSound("step_sneak_female_dirt.snt");
        	PreloadSound("player_bodyfall.snt");
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	/////////////////////////
        	// Check if got all notes
        	if(GetLocalVarInt("NoteCountNum")==437)
        	{
        		AddKeyPart(0x11ad2144);
        		AddKeyPart(unchecked((int)0xa1d56311));
        		AddDebugMessage("All notes added!\n", false);
        	}
        }
    }
}
