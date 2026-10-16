using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/20_sewer.hps; journal: docs/ScriptPort/ch02/20_sewer.md.</summary>
    [HplMapScript("maps/main/ch02/20_sewer.map")]
    public class Sewer : HplScript
    {
        ////////////////////////////
        // HELPER AND DEBUG
        ////////////////////////////

        //------------------------------

        /////////////////////////
        // Debug variables

        bool gbForceWaterWheelSlowSpeed = false;

        //------------------------------

        //////////////////////////////
        // Cog combo Helpers
        int GetCogComboIndex()
        {
        	if(gbForceWaterWheelSlowSpeed)
        		return 0;
        	else
        		return GetLocalVarInt("water_wheel_cog_panel_1") + (2-GetLocalVarInt("water_wheel_cog_panel_2"));
        }

        string GetCogComboString()
        {
        	string[] vComboStrings = { "lowest", "low", "med", "high", "highest" };

        	return vComboStrings[GetCogComboIndex()];
        }

        //This is the number of steps to wait before playing next impact sound (depends on what speed the wheel is turning)
        int GetCogPlaySoundWaitSteps()
        {
        	int[] vWaitSteps = { 1, 1, 1, 1, 0 };

        	return vWaitSteps[GetCogComboIndex()];
        }

        float GetCogRotateSpeed()
        {
        	if(GetLocalVarInt("WaterWheelBlocked")==1) return 0.0f;

        	int lCogComboIdx = GetCogComboIndex();
        	//AddDebugMessage("CogIdx: "+lCogComboIdx, false);

        	return GetLocalVarFloat("WaterWheelRotateSpeed"+lCogComboIdx);
        }

        //------------------------------

        //////////////////////////////
        // Bone dropper helper
        int glBoneCount = 0;
        string[] gvBoneEnts = { "human_skull.ent", "human_bone.ent" };

        string GetBoneModel()
        {
        	int lIndex = RandInt(0, 1);

        	return gvBoneEnts[lIndex];
        }

        void DropBone(string asArea, float fTime)
        {
        	AddTimer(asArea, fTime, nameof(TimerCreateBone));
        }

        void TimerCreateBone(string asTimer)
        {
        	string sBoneName = "script_created_bone_"+glBoneCount;
        	CreateEntityAtArea(sBoneName, GetBoneModel(), asTimer, false);
        	SetEntityInteractionDisabled(sBoneName, false);
        	AddTimer(sBoneName, 5, nameof(TimerRemoveBone));

        	glBoneCount++;
        }

        void TimerRemoveBone(string asTimer)
        {
        	SetPropActiveAndFade(asTimer, false, 0.5f);
        }


        //------------------------------

        ////////////////////////////
        // INTERACT WITH PIPE
        ////////////////////////////

        //------------------------------

        /////////////////////////
        // Interact pipe piece
        void InteractWithPipePiece(string asEntity)
        {
        	SetMessage("Ch02Level20", "PipeLoose", 0);
        	AddQuest("20Pipe", "20Pipe");
        }

        //------------------------------

        /////////////////////////
        // Break pipe piece
        void CollidePipePieceWithBreakArea(string asParent, string asChild, int alState)
        {
        	///////////////////////////////////////////
        	// Check how many times the pipe has collided with area, and break if enough
        	int lBreakAmount = GetLocalVarInt("PipeBreakAmount");
        	if(lBreakAmount==2)
        	{
        		PlaySoundAtEntity("PipeBreak", "17_break_wheel.snt", asParent, 0.3f, false);
        		PlaySoundAtEntity("PipeBreak2", "25_bar_drop.snt", asParent, 0, false);
        		PlaySoundAtEntity("PipeBreak3", "impact_water_med.snt", asParent, 0, false);

        		SetEntityActive(asParent, false);
        		SetEntityActive("pipe_piece", true);
        		RemoveEntityCollideCallback(asParent, asChild);

        		CompleteQuest("20Pipe", "20Pipe");

        		SetEntityActive("AreaScaryWaterLurkerEvent", true); //Activate a scary water lurker event.
        	}
        	else
        	{
        		lBreakAmount++;
        		SetLocalVarInt("PipeBreakAmount", lBreakAmount);
        	}
        }


        //------------------------------

        ////////////////////////////
        // COG CONTROL PANEL
        ////////////////////////////

        //------------------------------

        void CogPanelChange(string asEntity, int alState)
        {
        	AddDebugMessage(asEntity + " state: " + alState, false);
        	SetLocalVarInt(asEntity, alState);

        	int lCogsInRightPlace = 0;
        	if(GetLocalVarInt("water_wheel_cog_panel_1")==0 && GetLocalVarInt("water_wheel_cog_panel_2")==2)
        		lCogsInRightPlace = 1;

        	SetLocalVarInt("CogsInRightPlace", lCogsInRightPlace);

        	PlayGuiSound("20_lever_position.snt", 0.9f);

        	UpdateWaterWheelRotateSpeed();
        }

        /*
        /////////////////////////
        // Cog panel controls
        void CogLeverInPlace(string &in asAreaName, string &in asBodyName)
        {

        	///////////////////////////////////////////
        	// Set cog state based on lever position
        	for(int i=0;i<3;++i)
        	{
        		string sAreaName = "water_wheel_cogpanel_"+lID+"_leverpos_"+i;
        		//AddDebugMessage("Checking agains area: " + sAreaName, false);
        		if(asAreaName==sAreaName)
        		{
        			SetLocalVarInt("CogPanel"+lID+"State", i);
        			AddDebugMessage("CogPanel"+lID+"State is now "+i+" Speed: "+GetCogRotateSpeed(),false);

        			PlaySoundAtEntity("LeverInPlace", "20_stick_"+GetCogComboString()+".snt", asAreaName,0.3, false);

        			// Fix so that player releases lever
        			SetPlayerActive(false);
        			SetPlayerActive(true);

        			break;
        		}
        	}

        	int lCogsInRightPlace=0;
        	if(GetLocalVarInt("CogPanel1State")==0 && GetLocalVarInt("CogPanel2State")==0)
        		lCogsInRightPlace=1;

        	SetLocalVarInt("CogsInRightPlace", lCogsInRightPlace);

        	UpdateWaterWheelRotateSpeed();
        }
        */

        void InteractWheelMachine(string asEntity)
        {
        	AddQuest("20Machine", "20Machine");
        }
        //------------------------------------------------------

        //////////////////////////
        // WATER WHEEL SPEED HELPER
        //////////////////////////

        //------------------------------------------------------

        /////////////////////////
        // Wheel rotate speed
        void UpdateWaterWheelRotateSpeed()
        {
        	//AddDebugMessage("Calling UpdateWaterWheelRotateSpeed()", false);

        	float fRotateSpeed = GetCogRotateSpeed();

        	//AddDebugMessage("Updating rotation speed: "+fRotateSpeed, false);

        	//////////////////////////
        	// Modify wheel movement
        	if(fRotateSpeed==0.0f)
        	{
        		StopPropMovement("water_wheel_1");
        		SetPropStaticPhysics("water_wheel_1", true);
        	}
        	else
        	{
        		RotatePropToSpeed("water_wheel_1", 1.0f, fRotateSpeed, 1.0f, 0.0f,0.0f, true, "water_wheel_center_area");
        	}

        	///////////////////////////
        	// Update stream particles
        	DestroyParticleSystem("PS_WaterWheelStream");
        	if(fRotateSpeed>0.0f)
        	{
        		if(GetCogComboIndex()==0)
        			CreateParticleSystemAtEntity("PS_WaterWheelStream", "ps_waterwheel_stream.ps", "water_wheel_splasharea_in", false);
        		else
        			CreateParticleSystemAtEntity("PS_WaterWheelStream", "ps_waterwheel_stream.ps", "water_wheel_splasharea_out", false);
        	}

        	StopSound("CogMachinery",2);
        	AddTimer("UpdateCogSounds", 0.5f, nameof(TimerUpdateCogMachinerySounds));
        }


        //------------------------------------------------------

        //////////////////////////
        // COG MACHINERY SOUNDS (played in control room)
        //////////////////////////

        //------------------------------------------------------

        void TimerUpdateCogMachinerySounds(string asTimer)
        {
        	UpdateCogMachinerySounds(GetCogRotateSpeed());
        }

        void UpdateCogMachinerySounds(float afRotateSpeed)
        {
        	////////////////////////////////////
        	// All sound files in here are placeholders
        	string sSndFile;
        	if(afRotateSpeed==0.0f)
        	{
        		sSndFile = "20_machinery_stop.snt";
        	}
        	else
        	{
        		sSndFile = "20_machinery_running_"+GetCogComboString()+".snt";
        	}

        	//AddDebugMessage("Setting machinery sound for speed "+ afRotateSpeed +": " + sSndFile, false);

        	StopSound("CogMachinery",2);
        	for(int i=1;i<2;++i)
        		PlaySoundAtEntity("CogMachinery", sSndFile, "WaterWheelMachinerySoundArea"+i, 0.5f, true);
        }

        //------------------------------------------------------

        //////////////////////////
        // WATER WHEEL SPLASH EFFECT
        //////////////////////////

        //------------------------------------------------------


        /////////////////////////
        // Wheel splash IN
        void CollideWaterWheelBladeWithWaterIn(string asParent, string asChild, int alState)
        {
        	//AddDebugMessage("Collision between " + asParent	+ " and " + asChild + " state: " +alState, false);
        	string sSplashType = GetCogComboString();

        	// Play splashes every 3rd time
        	int lSoundCounter = GetLocalVarInt("WaterWheelPlayingSplashSound");

        	if(lSoundCounter>=GetCogPlaySoundWaitSteps())	//So count is high enough for sound to play
        	{
        		//AddDebugMessage("WaterSound: "+lSoundCounter+" ID:"+GetCogComboIndex() , false);
        		lSoundCounter=0;

        		//CheckSo player is in area close enough to play sound.
        		if(GetLocalVarInt("WaterWheelSplashSoundHeard")==1)
        		{
        			PlaySoundAtEntity(asChild + "SplashSound", "20_waterwheel_impact_"+sSplashType+".snt", asChild, 0, false);
        		}
        	}
        	else
        	{
        		lSoundCounter++;
        	}

        	SetLocalVarInt("WaterWheelPlayingSplashSound", lSoundCounter);
        	CreateParticleSystemAtEntity("PS_Splash_"+asChild, "ps_impact_water_wheel_splash_"+sSplashType+".ps", asChild, false);

        	//PlaySoundAtEntity(asChild + "SplashSound", "20_waterwheel_impact_"+sSplashType+".snt", asChild, 0, false);
        	//CreateParticleSystemAtEntity("PS_Splash_"+asChild, "ps_impact_water_wheel_splash_"+sSplashType+".ps", asChild, false);
        }

        /////////////////////////
        // Wheel splash OUT
        void CollideWaterWheelBladeWithWaterOut(string asParent, string asChild, int alState)
        {
        	//AddDebugMessage("Collision between " + asParent	+ " and " + asChild + " state: " +alState, false);
        	string sSplashType = GetCogComboString();

        	CreateParticleSystemAtEntity("PS_Splash_"+asChild, "ps_impact_water_wheel_splash_out.ps", asChild, false);
        }


        //------------------------------------------------------

        //////////////////////////
        // WATER WHEEL VS PLAYER DAMAGE
        //////////////////////////

        //------------------------------------------------------

        //////////////////////////
        // Collide with player
        void CollidePlayerWithWaterWheel(string asParent, string asChild, int alState)
        {
        	//AddDebugMessage("Collision between " + asParent	+ " and " + asChild + " state: " +alState, false);
        	AddQuest("20WaterWheelBlocking", "20WaterWheelBlocking");

        	AddPlayerBodyForce(0,0,9000, false);

        	GivePlayerDamage(10, "BloodSplat", true, false);

        	PlayGuiSound("20_hit_wheel.snt", 0.8f);
        }

        //------------------------------------------------------

        //////////////////////////
        // JAM WATER WHEEL
        //////////////////////////

        //------------------------------------------------------

        /////////////////////////
        // Use pipe
        void UsePipeOnWaterWheelCallback(string asItem, string asEntity)
        {
        	//AddDebugMessage("Using pipe on wheel", false);
        	int lCogComboIndex = GetCogComboIndex();

        	////////////////////////////////////////////////////////////////////////////////
        	// Player is not allowed to put pipe on wheel unless the wheel rotate speed is low
        	if(lCogComboIndex!=0)
        	{
        		AddQuest("20WaterWheelFast", "20WaterWheelFast");

        		SetMessage("Ch02Level20", "WheelTooFast", 0);
        		PlaySoundAtEntity("PipeHitSound", "20_pipe_reject.snt", "PipeHitArea", 0, false);
        		CreateParticleSystemAtEntity("PipeHitSpark", "ps_hit_metal.ps", "PipeHitArea", false);
        		CreateParticleSystemAtEntity("PipeHitSpark", "ps_hit_metal.ps", "PipeHitArea", false);
        		CreateParticleSystemAtEntity("PipeHitSmoke", "ps_dust_impact.ps", "PipeHitArea", false);
        	}
        	else
        	{
        		// Start LookAt to pipe pos
        		StartPlayerLookAt("PipeBlockPosArea", 2.0f,4.0f,"");
        		RemoveUseItemCallback("UsePipeOnWaterWheel");

        		SetEntityPlayerInteractCallback("WheelInteractArea", "", false);

        		CompleteQuest("20WaterWheelFast", "20WaterWheelFast");
        		CompleteQuest("20WaterWheelBlocking", "20WaterWheelBlocking");
        		CompleteQuest("20Machine", "20Machine");

        		PlayMusic("20_puzzle.ogg", false, 1.0f, 0.0f, 10, false);
        		//Wait a second until starting area that allows pipe to be placed.
        		AddTimer("PipePosTarget",1.0f,nameof(TimerPipePosTargetedCallback));
        	}
        }

        // Called when pipe position is targeted
        void TimerPipePosTargetedCallback(string asTimer)
        {
        	// Check pipe pos is not blocked
        	//AddDebugMessage("Looking at target!!", false);
        	AddEntityCollideCallback("water_wheel_1", "PipeBlockPosArea", nameof(PipePosClear), true, -1);
        }

        ////////////////////////
        // Pipe position is clear
        void PipePosClear(string asParent, string asChild, int alState)
        {
        	// Put pipe in place
        	StopPlayerLookAt();
        	SetEntityActive("PipeBlock", true);
        	SetPropStaticPhysics("PipeBlock", true);
        	SetEntityInteractionDisabled("PipeBlock", true);
        	AddEntityCollideCallback("water_wheel_1", "PipeHitArea", nameof(CollideWheelWithPipe), true, 1);

        	StopSound("WheelLoop", 2.0f);

        	RemoveItem("pipe_piece");
        }

        ////////////////////////
        // Water wheel collide with static pipe piece
        void CollideWheelWithPipe(string asParent, string asChild, int alState)
        {
        	///////////////////////////
        	// Set block boolean and do some effects, also block wheel (done by the update func)
        	SetLocalVarInt("WaterWheelBlocked", 1);
        	StartScreenShake(0.025f, 2, 0, 1.5f);

        	//Remove blocker so player can pass.
        	SetEntityActive("WaterWheelBlocker", false);

        	//No more damage from water wheel!
        	RemoveEntityCollideCallback("Player", "water_wheel_1");

        	//Puzzle complete extras!
        	PlayMusic("12_puzzle_epoxy.ogg", false, 1.0f, 0.1f, 10, false);
        	GiveSanityBoostSmall();

        	// Create particles
        	CreateParticleSystemAtEntity("PipeHitSpark", "ps_hit_metal.ps", "PipeHitArea", false);
        	CreateParticleSystemAtEntity("PipeHitSpark", "ps_hit_metal.ps", "PipeHitArea", false);
        	CreateParticleSystemAtEntity("PipeHitSmoke", "ps_dust_impact.ps", "PipeHitArea", false);

        	PlaySoundAtEntity("WheelPipeHit", "20_pipe_stuck.snt", asChild, 0, false);

        	UpdateWaterWheelRotateSpeed();
        }

        //------------------------------------------------------

        /////////////////////////
        // Player is near enough to hear
        void CollidePlayerWithSplashHeardArea(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Wheel Splash is heard? " + alState, false);
        	SetLocalVarInt("WaterWheelSplashSoundHeard", alState);
        }

        //------------------------------------------------------

        //////////////////////////
        // BRUTE ENCOUNTER - CENTER ROOM
        //////////////////////////

        //------------------------------------------------------

        void CollidePlayerWithBruteArea1(string asParent, string asChild, int alState)
        {
        	SetEntityActive("servant_brute_2", true);

        	ClearEnemyPatrolNodes("servant_brute_2");
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_143", 3.0f, "");
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_116", 3.0f, "");
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_124", 1.0f, "");

        	AddTimer(nameof(TimerEBrute01), 1.5f, nameof(TimerEBrute01));
        }

        void TimerEBrute01(string asTimer)
        {
        	string sEvent = asTimer;	//Do not edit, sets timer loop function name.
        	AddLocalVarInt(sEvent, 1);	//Do not edit, steps through timer loop events.
        	bool bPauseAtStep = false;	//Do not edit, to pause or end the timer loop at specified step.

        	float fEventSpeed = 1.0f;	//Set to preferred default time for the timer loop.

        	switch(GetLocalVarInt(sEvent)){
        		//////////
        		//First step in the event - replace with step comment.
        		case 1:
        			StartScreenShake(0.003f, 2.0f, 2.0f, 2.0f);
        			GiveSanityDamage(10.0f, true);
        			SetPlayerMoveSpeedMul(0.8f);
        			SetPlayerRunSpeedMul(0.8f);
        			AddTimer("scare", 0.5f, nameof(TimerPlayerReact));
        			AddTimer("breath", 2.5f, nameof(TimerPlayerReact));
        			AddTimer("breathl", 4.5f, nameof(TimerPlayerReact));
        		break;
        		case 2:
        			SetPlayerMoveSpeedMul(0.7f);
        			SetPlayerRunSpeedMul(0.7f);
        		break;
        		case 3:
        			SetPlayerMoveSpeedMul(0.6f);
        			SetPlayerRunSpeedMul(0.6f);
        		break;
        		case 4:
        			SetPlayerMoveSpeedMul(0.5f);
        			SetPlayerRunSpeedMul(0.5f);
        		break;
        		case 5:
        			SetPlayerMoveSpeedMul(0.6f);
        			SetPlayerRunSpeedMul(0.6f);
        		break;
        		case 6:
        			SetPlayerMoveSpeedMul(0.7f);
        			SetPlayerRunSpeedMul(0.7f);
        		break;
        		case 7:
        			SetPlayerMoveSpeedMul(0.8f);
        			SetPlayerRunSpeedMul(0.8f);
        		break;
        		case 8:
        			SetPlayerMoveSpeedMul(1.0f);
        			SetPlayerRunSpeedMul(1.0f);
        		break;
        		//////////
        		//End timer when no more steps in the event found.
        		default:
        			bPauseAtStep = true;
        		break;
        	}

        	if(!bPauseAtStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }

        void TimerPlayerReact(string asTimer)
        {
        	if(asTimer == "scare")
        		PlayGuiSound("react_scare", 1.0f);
        	else if(asTimer == "breath")
        		PlayGuiSound("react_breath", 0.8f);
        	else if(asTimer == "breathl")
        		PlayGuiSound("react_breath", 0.5f);
        	else if(asTimer == "sanity")
        		GiveSanityDamage(5, true);
        }
        //------------------------------------------------------

        //////////////////////////
        // BRUTE ENCOUNTER - HALL 3
        ///////////////////////////

        //------------------------------------------------------

        void CollidePlayerWithBruteAreaHall3(string asParent, string asChild, int alState)
        {
        	/////////////////////////////////////////////
        	// Check if wheel is slowed down
        	if(GetLocalVarInt("CogsInRightPlace")==1)
        	{
        		//////////////////////////////
        		// Destroy callback
        		RemoveEntityCollideCallback(asParent, asChild);
        		SetEntityActive("servant_brute_3", true);

        		ClearEnemyPatrolNodes("servant_brute_3");
        		AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_145", 3.0f, "");
        		AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_94", 3.0f, "");
        		AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_64", 6.0f, "");

        		AddTimer("scare", 1.2f, nameof(TimerPlayerReact));
        		AddTimer("sanity", 1.2f, nameof(TimerPlayerReact));
        		AddTimer("breath", 3.2f, nameof(TimerPlayerReact));
        		AddTimer("breathl", 5.5f, nameof(TimerPlayerReact));
        		PlayMusic("20_event_darkness.ogg",false , 1.0f, 10, 20, false);
        		CheckPoint("check01","PlayerStartArea_10", nameof(CheckPoint01), "Hints", "DeathBrute_20_Sewer_01");
        	}
        }

        //------------------------------------------------------

        void CheckPoint01(string asName, int alCount)
        {
        	ResetProp("sewer_arched_3");

        	if(alCount == 0)
        	{
        		SetEntityPlayerInteractCallback("sewer_arched_3",nameof(InteractHall3CheckPoint01Door), true);
        	}
        	else if(alCount == 1)
        	{
        		SetEntityActive("servant_brute_3", true);

        		ClearEnemyPatrolNodes("servant_brute_3");
        		AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_74", 5.0f, "");
        		AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_94", 3.0f, "");
        		AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_64", 6.0f, "");
        		AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_74", 5.0f, "");
        		AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_53", 6.0f, "");
        		AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_41", 3.0f, "");
        	}
        	else
        	{
        		//Free path for player!
        	}
        }

        void InteractHall3CheckPoint01Door(string asEntity)
        {
        	SetEntityActive("servant_brute_4", true);

        	ClearEnemyPatrolNodes("servant_brute_4");
        	AddEnemyPatrolNode("servant_brute_4", "PathNodeArea_93", 5.0f, "");
        	AddEnemyPatrolNode("servant_brute_4", "PathNodeArea_94", 3.0f, "");
        	AddEnemyPatrolNode("servant_brute_4", "PathNodeArea_64", 3.0f, "");
        }

        //------------------------------------------------------

        //////////////////////////
        // GRUNT CORPSE + ALEXANDER VOICE
        //////////////////////////

        //------------------------------------------------------

        void CollideGruntCorpse(string asParent, string asChild, int alState)
        {
        	GiveSanityDamage(10, true);

        	PlayMusic("20_event_darkness.ogg", false, 1.0f, 0, 20, false);

        	AddTimer("Music", 4.0f, nameof(TimerAlexanderVoice01));
        	AddTimer("AlexVoice01", 10.0f, nameof(TimerAlexanderVoice01));

        	SetEntityActive("AreaBruteHall4Encount", true); //Enable area that start brute Hall 4 encounter.
        }

        //------------------------------------------------------

        void TimerAlexanderVoice01(string asTimer)
        {
        	if(asTimer == "Music"){
        		StopMusic(4.0f, 20);
        		return;
        	}
        	AddEffectVoice("CH02L20_Alexander_01", "", "Voice", "CH02L20_Alexander_01", false, "", 0.0f, 0.0f);
        }

        //------------------------------------------------------

        //////////////////////////
        // BRUTE ENCOUNTER - HALL 4
        //////////////////////////

        //------------------------------------------------------

        void CollideBruteHall4Encount(string asParent, string asChild, int alState)
        {
        	SetPropHealth("sewer_grate_worn_1",0); //Break the grate! <- this shall play lound sound, if not enough play other sound here!
        	StartScreenShake(0.05f, 0.5f, 0.1f, 0.1f);

        	SetEntityPlayerInteractCallback("sewer_grate_worn_1", "", true);

        	AddTimer("BarsBrokenQuest",2, nameof(TimerBarsBrokenQuest));

        	PlayMusic("20_event_darkness.ogg", false, 1.0f, 10, 20, false);

        	AddTimer("scare", 0.5f, nameof(TimerPlayerReact));
        	AddTimer("sanity", 0.5f, nameof(TimerPlayerReact));
        	AddTimer("breath", 3.0f, nameof(TimerPlayerReact));
        	AddTimer("breathl", 6.0f, nameof(TimerPlayerReact));

        	SetEntityActive("servant_brute_5", true);

        	PlaySoundAtEntity("breakbars", "15_ripp_door.snt", "servant_brute_5", 0.0f, false);

        	CheckPoint("check02","PlayerStartArea_12", nameof(CheckPoint02), "Hints", "DeathBrute_20_Sewer_02");

        	ClearEnemyPatrolNodes("servant_brute_5");
        	AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_169", 5.0f, "");
        	AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_217", 3.0f, "");
        	AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_313", 3.0f, "");

        	//Disalbe the area that activates the guard brute.
        	SetEntityActive("AreaBruteEncountHall5", false);
        }

        void TimerBarsBrokenQuest(string asTimer)
        {
        	CompleteQuest("20Bars", "20Bars");
        	AddQuest("20Bars02", "20Bars02");
        }

        //------------------------------------------------------

        void CheckPoint02(string asName, int alCount)
        {
        	ResetProp("sewer_arched_4");

        	if(alCount ==0)
        	{
        		SetEntityActive("servant_brute_5", true);
        		ClearEnemyPatrolNodes("servant_brute_5");

        		AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_312", 5.0f, "");
        		AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_162", 3.0f, "");
        		AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_179", 2.0f, "");
        		AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_167", 1.0f, "");
        	}
        	else
        	{
        		//Free path for player!

        		//Turn off the grunt appearance up ahead!
        		SetEntityActive("AreaBruteEncountHall5", false);
        	}
        }

        //------------------------------------------------------

        void OnBrute05AutoDisabled(string asEntity, string asType)
        {
        	SetEntityActive("AreaBruteEncountHall5", true);
        }

        //------------------------------------------------------

        //////////////////////////
        // BRUTE ENCOUNTER - HALL 5
        //////////////////////////

        //------------------------------------------------------

        void CollideBruteEncountHall5(string asParent, string asChild, int alState)
        {
        	SetEntityActive("servant_brute_6", true);

        	PlaySoundAtEntity("metalamb", "15_amb_metal.snt", "AreaBruteEncountHall5", 4.0f, true);

        	StartScreenShake(0.003f, 1.0f, 2.0f, 2.0f);
        	AddTimer("breath", 2.0f, nameof(TimerPlayerReact));
        	AddTimer("breathl", 4.0f, nameof(TimerPlayerReact));

        	CheckPoint("check03","PlayerStartArea_4", nameof(CheckPoint03), "Hints", "DeathBrute_20_Sewer_03");
        }

        void CheckPoint03(string asName, int alCount)
        {
        	ResetProp("sewer_arched_*");
        	ResetProp("stone_small01*");

        	if(alCount ==0)
        	{
        		SetEntityActive("servant_brute_6", true);
        	}
        	else if(alCount ==1)
        	{
        		SetEntityActive("servant_brute_6", true);
        		ClearEnemyPatrolNodes("servant_brute_6");

        		AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_258", 5.0f, "");
        		AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_269", 5.0f, "");
        		AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_242", 5.0f, "");
        		AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_269", 5.0f, "");
        		AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_258", 5.0f, "");
        	}
        	else
        	{
        		//Set that the brute is in the patrol area so we get a run event.
        		SetLocalVarInt("Brute06IsInPatrolArea", 1);
        	}

        }

        //Check and see if brute 6 is in the patrol area.
        void CollideBruteInsidePatrolArea(string asParent, string asChild, int alState)
        {
        	int lVar = alState==1 ? 1 : 0;
        	SetLocalVarInt("Brute06IsInPatrolArea", lVar);

        	AddDebugMessage("Grunt 06 Patrol area: "+lVar, false);
        }

        //////////////////////////
        // BRUTE ENCOUNTER - HALL 6
        //////////////////////////

        //------------------------------------------------------

        void CollideBruteEncountHall6(string asParent, string asChild, int alState)
        {
        	//If brute 6 is not in patrol area, then we do not spawn this one!
        	if(GetLocalVarInt("Brute06IsInPatrolArea")==0) return;

        	//Do not want another event.
        	SetEntityActive("AreaBruteEncountHall6", false);

        	AddTimer("scare", 0.5f, nameof(TimerPlayerReact));
        	AddTimer("sanity", 0.5f, nameof(TimerPlayerReact));
        	AddTimer("breath", 2.5f, nameof(TimerPlayerReact));
        	AddTimer("breathl", 4.5f, nameof(TimerPlayerReact));

        	SetEntityActive("servant_brute_7", true);
        	SetEntityActive("servant_brute_6", false); //Turn off the guard!

        	SetLocalVarInt("Brute07Activated",1);

        	PlaySoundAtEntity("brute_scream", "brute/amb_alert.snt", "Player", 0, false);

        	ClearEnemyPatrolNodes("servant_brute_7");
        	AddEnemyPatrolNode("servant_brute_7", "PathNodeArea_246", 1.0f, "");

        	ShowEnemyPlayerPosition("servant_brute_7");

        	//New checkpoint
        	//if player reaches this point, can let him through without any problems.
        	CheckPoint("check04","PlayerStartArea_4", nameof(CheckPoint04), "Hints", "DeathGuardian_25_CellTunnels");

        	//add a timer to make sure the player is not lost!
        	AddTimer("ShowBrute7Player", 3, nameof(TimerShowBrute7Player));
        }

        void TimerShowBrute7Player(string asTimer)
        {
        	ShowEnemyPlayerPosition("servant_brute_7");
        	AddTimer("ShowBrute7Player", 3, nameof(TimerShowBrute7Player));
        }

        //------------------------------------------------------

        void CheckPoint04(string asName, int alCount)
        {
        	ResetProp("sewer_arched_*");
        	ResetProp("stone_small01*");
        }

        //------------------------------------------------------

        //////////////////////////
        // WATER LURKER SCARE IN HALL 2
        //////////////////////////

        //------------------------------------------------------

        void CollideScaryWaterLurkerEvent(string asParent, string asChild, int alState)
        {
        	if(GetPlayerSanity()>=30)
        		return;

        	PlayMusic("04_event_stairs.ogg", false, 0.7f, 0.1f, 10, false);

        	AddTimer("scare", 0.7f, nameof(TimerPlayerReact));
        	AddTimer("breath", 2.7f, nameof(TimerPlayerReact));
        	AddTimer("breathl", 4.7f, nameof(TimerPlayerReact));

        	AddTimer("AreaLurkerSound", 0.5f, nameof(TimerLurkerSplash));
        	AddTimer("AreaLurkerSplash_1", 1.0f, nameof(TimerLurkerSplash));
        	AddTimer("AreaLurkerSplash_2", 2.2f, nameof(TimerLurkerSplash));
        	AddTimer("AreaLurkerSplash_3", 3.0f, nameof(TimerLurkerSplash));
        	AddTimer("AreaLurkerSplash_4", 3.5f, nameof(TimerLurkerSplash));
        	AddTimer("AreaLurkerSplash_5", 4.0f, nameof(TimerLurkerSplash));

        	RemoveEntityCollideCallback(asParent, asChild);
        }

        //------------------------------------------------------

        void TimerLurkerSplash(string asTimer)
        {
        	if(asTimer == "AreaLurkerSound"){
        		PlaySoundAtEntity("LurkerSound","water_lurker_hunt.snt", "AreaLurkerSplash_1", 0, false);
        		GiveSanityDamage(10.0f, true);
        		StartScreenShake(0.003f, 0.0f, 0.5f, 3.0f);

        	}else{

        		PlaySoundAtEntity("LurkerSound","impact_water_high.snt", asTimer, 0, false);
        		CreateParticleSystemAtEntity("LurkerPS", "waterlurker_walk_splash", asTimer, false);
        	}

        	if(asTimer == "AreaLurkerSplash_5")
        		PlaySoundAtEntity("LurkerSound2","water_lurker_eat.snt", asTimer, 0, false);
        }

        //------------------------------------------------------


        //////////////////////////
        // EXIT MAP
        //////////////////////////

        //------------------------------------------------------

        void CollideExitMap(string asParent, string asChild, int alState)
        {
        	ChangeMap("21_torture_nave.map", "PlayerStartArea_1", "20_exit", "");
        }

        //------------------------------------------------------


        //////////////////////////
        // INTERACT BARS
        //////////////////////////

        //------------------------------------------------------
        void InteractMetalBars(string asEntity)
        {
        	PlaySoundAtEntity("squeak", "20_bend_pipe", asEntity, 0.5f, false);

        	SetMessage("Ch02Level20", "BruteMetalBars", 0);
        	AddQuest("20Bars", "20Bars");

        	AddTimer(asEntity, 1.0f, nameof(TimerMetalBars));
        }
        void TimerMetalBars(string asTimer)
        {
        	StopSound("squeak", 0.5f);
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractMetalBars), true);
        }

        void CollideAreaBarQuest(string asParent, string asChild, int alState)
        {
        	CompleteQuest("20Bars02","20Bars02");
        }


        //------------------------------------------------------

        //////////////////////////
        // INTERACT WATER WHEEL
        //////////////////////////

        //------------------------------------------------------

        void InteractWaterWheel(string asEntity)
        {
        	CollidePlayerWithWaterWheel("","",1);
        	AddTimer(asEntity, 1.0f, nameof(TimerWaterWheel));
        }
        void TimerWaterWheel(string asTimer)
        {
        	StopSound("squeak", 0.5f);
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractWaterWheel), true);
        }


        //------------------------------------------------------

        //////////////////////////
        // FINAL TUNNEL EVENTS
        //////////////////////////

        //------------------------------------------------------

        void CollideAreaExitEvent(string asParent, string asChild, int alState)
        {
        	if(asChild == "AreaExitEvent_1")
        	{
        		PlayGuiSound("insanity_monster_roar03.ogg", 1.0f);
        		StartScreenShake(0.01f, 0.5f, 2.0f, 4.0f);
        		AddTimer("scare", 0.5f, nameof(TimerPlayerReact));
        		AddTimer("breath", 2.5f, nameof(TimerPlayerReact));
        		AddTimer("breathl", 4.5f, nameof(TimerPlayerReact));

        		//This is just to make sure that no enemy is hunting the player at this point!
        		SetEntityActive("servant_brute_6", false);
        		SetEntityActive("servant_brute_5", false);
        		SetEntityActive("servant_brute_4", false);
        	}
        	else
        	{
        		// Grunt stops getting player position
        		if(asChild=="AreaExitEvent_6")
        		{
        			RemoveTimer("ShowBrute7Player");
        		}

        		AddTimer("breathl", 1.5f, nameof(TimerPlayerReact));
        		PlayGuiSound("15_sand_debris.snt", 1.0f);
        	}
        }

        //------------------------------------------------------

        void CollideExitAreaDisableBrute(string asParent, string asChild, int alState)
        {
        	//Do not do anything if brute is not activated
        	if(GetLocalVarInt("Brute07Activated")==0)return;

        	if(GetLocalVarInt("Brute7Disabled")==1) return;
        	SetLocalVarInt("Brute7Disabled", 1);

        	//PlaySoundAtEntity("brute_scream", "brute/amb_alert.snt", "servant_brute_7", 0, false);
        	SetEnemyDisableTriggers("servant_brute_7", true);

        	//Reset enemy
        	SetEntityActive("servant_brute_7", false);
        	SetEntityActive("servant_brute_7", true);

        	ClearEnemyPatrolNodes("servant_brute_7");
        	AddEnemyPatrolNode("servant_brute_7", "PathNodeArea_297", 5, "");
        }

        void CollideExitAreaEnableBrute(string asParent, string asChild, int alState)
        {
        	//Do not do anything if brute is not activated
        	if(GetLocalVarInt("Brute07Activated")==0)return;

        	if(GetLocalVarInt("Brute7Disabled")==0) return;
        	SetLocalVarInt("Brute7Disabled", 0);

        	SetEnemyDisableTriggers("servant_brute_7", false);
        	ShowEnemyPlayerPosition("servant_brute_7");
        }

        //------------------------------------------------------

        //////////////////////////
        // MONSTER BREATHS
        //////////////////////////

        //------------------------------------------------------

        void CollideAreaBreath(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("warn", "04_warn.snt", "AreaBreath_1", 0.0f, false);
        	PlaySoundAtEntity("rocks", "03_rock_move.snt", "AreaBreath_1", 0.0f, false);
        	CreateParticleSystemAtEntity("warnps", "ps_cave_monster_warn.ps", "AreaBreath_1", false);
        	StartScreenShake(0.01f, 0.5f, 2.0f, 3.5f);
        	AddTimer("scare", 0.5f, nameof(TimerPlayerReact));
        	AddTimer("sanity", 0.5f, nameof(TimerPlayerReact));
        	AddTimer("breath", 3.0f, nameof(TimerPlayerReact));
        	AddTimer("breathl", 5.0f, nameof(TimerPlayerReact));
        	AddTimer("AreaBreath_1", 5.0f, nameof(TimerCaveBreath));
        }
        void TimerCaveBreath(string asTimer)
        {
        	PlaySoundAtEntity("breaths", "04_breath.snt", "AreaBreath_1", 0.0f, false);
        	PlaySoundAtEntity("rocksm", "03_rock_move.snt", "AreaBreath_1", RandFloat(2.0f,6.0f), false);
        	CreateParticleSystemAtEntity("breathps", "ps_cave_monster_breath.ps", "AreaBreath_1", false);

        	AddTimer(asTimer, RandFloat(6.0f,18.0f), nameof(TimerCaveBreath));
        }


        //CAVE WITH A BIT OF A SPLAH
        void CollideAreaCaveEvent(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("splash", "water_lurker_eat_rev.snt", "AreaCaveEvent_1", 0.0f, false);

        	AddTimer("scare", 0.5f, nameof(TimerPlayerReact));
        	AddTimer("breath", 2.5f, nameof(TimerPlayerReact));
        	AddTimer("breathl", 4.5f, nameof(TimerPlayerReact));

        	AddTimer("splash", 7.0f, nameof(TimerCaveSplah));
        	AddTimer("splash", 16.0f, nameof(TimerCaveSplah));
        	AddTimer("splash", 25.0f, nameof(TimerCaveSplah));
        	AddTimer("splash", 50.0f, nameof(TimerCaveSplah));
        	AddTimer("splash", 100.0f, nameof(TimerCaveSplah));
        }
        void TimerCaveSplah(string asTimer)
        {
        	PlaySoundAtEntity("splash", "water_lurker_idle.snt", "AreaCaveEvent_1", RandFloat(0.0f, 5.0f), false);
        }

        //------------------------------------------------------

        ////////////////////////////
        // BONE DROPPERS
        //////////////////////////

        //------------------------------------------------------

        void LookAtBoneDropper(string asEntity, int alState)
        {
        	if(alState==-1)
        		return;

        	if(GetPlayerSanity()>=30)
        		return;

        	int lNumBones = RandInt(3,5);
        	AddDebugMessage("Dropping bones!", false);
        	for(int i=0;i<lNumBones;++i)
        		DropBone(asEntity + "_source", (i+1)*0.3f);
        }

        //------------------------------------------------------

        //////////////////////////
        // DEAD END GROWLS
        //////////////////////////

        //------------------------------------------------------

        void CollideAreaGateMonster(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("GateMonsterDisabled")==1)
        		return;

        	AddTimer(asChild, RandFloat(1.0f,3.0f), nameof(TimerStartGateMonster));
        }

        void TimerStartGateMonster(string asTimer)
        {
        	// TODO : add more fluff here
        	PlaySoundAtEntity("growl", "04_warn.snt", asTimer+"_sound", 0, false);
        	StartScreenShake(0.1f, 0.5f, 1, 0.25f);
        	SetLocalVarInt("GateMonsterDisabled", 1);
        	AddTimer(nameof(TimerRestoreGateMonster), 60, nameof(TimerRestoreGateMonster));
        }

        void TimerRestoreGateMonster(string asTimer)
        {
        	SetLocalVarInt("GateMonsterDisabled", 0);
        }

        //------------------------------------------------------


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("Sewer");

        	/////////////////////////////////
        	// Clear all previously saved maps.
        	ClearSavedMaps();

        	/////////////////////////////////
        	//Sanity Boost
        	GiveSanityBoostSmall();

        	/////////////////////////////////
        	// Set up variables

        	// Pipe puzzle
        	///AddLocalVarInt("PipeBreakAmount", 0);

        	// Wheel puzzle
        	float[] vRotateSpeeds =
        	{
        		1.5708f,
        		3.1416f,
        		4.7124f,
        		5.1832f,
        		5.354f,
        	};

        	for(int i=0;i<5;++i)
        		SetLocalVarFloat("WaterWheelRotateSpeed"+i, vRotateSpeeds[i]);

        	/*
        	AddLocalVarInt("CogPanel1State", 0);
        	AddLocalVarInt("CogPanel2State", 0);

        	AddLocalVarInt("CogsInRightPlace", 0);

        	AddLocalVarInt("WaterWheelBlocked", 0);

        	AddLocalVarInt("WaterWheelPlayingSplashSound", 0);
        	AddLocalVarInt("WaterWheelSplashSoundHeard", 0);
        	*/

        	//Init this var so one is sure it is 1 at start! (Otherwise grunt 7 will not appear is testing.!)
        	SetLocalVarInt("Brute06IsInPatrolArea", 1);

        	///////////////////////////////
        	// Set up collide callbacks
        	AddEntityCollideCallback("Player", "water_wheel_1", nameof(CollidePlayerWithWaterWheel), false, 0);
        	AddEntityCollideCallback("water_wheel_1", "water_wheel_splasharea_in", nameof(CollideWaterWheelBladeWithWaterIn), false, 1);
        	AddEntityCollideCallback("water_wheel_1", "water_wheel_splasharea_out", nameof(CollideWaterWheelBladeWithWaterOut), false, -1);

        	AddEntityCollideCallback("pipe_piece_static", "PipeBreakArea", nameof(CollidePipePieceWithBreakArea), false, 1);

        	AddEntityCollideCallback("Player", "AreaScaryWaterLurkerEvent", nameof(CollideScaryWaterLurkerEvent), false, 1);

        	AddEntityCollideCallback("Player", "WheelSplashHeardArea", nameof(CollidePlayerWithSplashHeardArea), false, 0);

        	AddEntityCollideCallback("Player", "BruteScriptEvent1", nameof(CollidePlayerWithBruteArea1), true, 0);
        	AddEntityCollideCallback("Player", "BruteHall3EventArea", nameof(CollidePlayerWithBruteAreaHall3), false, 0);

        	AddEntityCollideCallback("sewer_arched_4", "AreaGruntCorpse", nameof(CollideGruntCorpse), true, 0);
        	AddEntityCollideCallback("Player", "AreaBruteHall4Encount", nameof(CollideBruteHall4Encount), true, 0);

        	AddEntityCollideCallback("Player", "AreaBruteEncountHall5", nameof(CollideBruteEncountHall5), true, 0);
        	AddEntityCollideCallback("Player", "AreaBruteEncountHall6", nameof(CollideBruteEncountHall6), false, 0);

        	AddEntityCollideCallback("servant_brute_6", "AreaBruteInsidePatrolArea", nameof(CollideBruteInsidePatrolArea),false,0);

        	AddEntityCollideCallback("Player", "AreaExitMap", nameof(CollideExitMap), true, 0);

        	AddEntityCollideCallback("Player", "AreaBarQuest", nameof(CollideAreaBarQuest), true, 1);

        	AddEntityCollideCallback("Player", "AreaBreath", nameof(CollideAreaBreath), true, 1);

        	AddEntityCollideCallback("Player", "AreaCaveEvent", nameof(CollideAreaCaveEvent), true, 1);

        	for(int i=1;i<7;i++) AddEntityCollideCallback("Player", "AreaExitEvent_"+i, nameof(CollideAreaExitEvent), true,1);

        	for(int i=1;i<=6;++i)
        		AddEntityCollideCallback("Player", "AreaGateMonster_" + i, nameof(CollideAreaGateMonster), false, 1);

        	AddEntityCollideCallback("Player", "AreaExit_DisableBrute", nameof(CollideExitAreaDisableBrute), false, 1);
        	AddEntityCollideCallback("Player", "AreaExit_EnableBrute", nameof(CollideExitAreaEnableBrute), false, 1);

        	///////////////////////////////
        	// Set up use item callbacks
        	AddUseItemCallback("UsePipeOnWaterWheel", "pipe_piece", "WheelInteractArea", nameof(UsePipeOnWaterWheelCallback), false);

        	///////////////////////////////
        	// Set up interaction callbacks
        	SetEntityPlayerInteractCallback("pipe_piece_static", nameof(InteractWithPipePiece), true);

        	///////////////////////////////
        	// Debug!
        	if(ScriptDebugOn())
        	{
        		//SetPlayerSanity(25.0);
        		GiveItemFromFile("lantern", "lantern.ent");
        		GiveItem("pipe_piece", "Puzzle", "PipePiece", "pipe_piece.tga", 1);
        		//gbForceWaterWheelSlowSpeed = true;
        	}


        	StopMusic(4.0f, 0);
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("20_waterwheel_impact_low"); PreloadSound("20_waterwheel_impact_med"); PreloadSound("20_waterwheel_impact_high"); PreloadSound("25_bar_drop");
        	PreloadSound("20_machinery_stop"); PreloadSound("20_machinery_running_high"); PreloadSound("20_machinery_running_low"); PreloadSound("20_machinery_running_med");
        	PreloadSound("water_lurker_hunt"); PreloadSound("04_warn"); PreloadSound("03_rock_move"); PreloadSound("04_breath");
        	PreloadSound("water_lurker_idle"); PreloadSound("react_breath"); PreloadSound("react_scare"); PreloadSound("insanity_monster_roar03");
        	PreloadSound("15_sand_debris"); PreloadSound("water_lurker_eat_rev");

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	//////////////////////
        	//Load Screen Setup
        	SetupLoadScreen("LoadingText", "Ch02_Sewer02", 1, "game_loading_well.jpg");
        }
    }
}
