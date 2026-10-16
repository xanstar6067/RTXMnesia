using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/18_cistern.hps; journal: docs/ScriptPort/ch02/18_cistern.md.</summary>
    [HplMapScript("maps/main/ch02/18_cistern.map")]
    public class Cistern : HplScript
    {
        ////////////////////////////
        // Rotate valves

        // Global helper to start an appropiate music track according to map state
        void StartMusicHelper(int alValveCount, bool abPlayEventMusic)
        {
        	string sMusicFile = "";
        	string sEventMusicFile = "";
        	float fVolume = 0, fFadeTime = 0; // AngelScript initializes primitive locals to zero.
        	float fEventVolume = 0;
        	float fEventFadeTime = 0;

        	StopMusic(8, 0);
        	AddDebugMessage("StartMusicHelper called with valve count : " + alValveCount, false);

        	switch(alValveCount)
        	{
        		case 1:
        			if(GetLocalVarInt("IAmStart") !=1)
        				sMusicFile = "18_amb.ogg";
        			sEventMusicFile = "18_puzzle_valve01.ogg";
        			fVolume = 0.5f;
        			fFadeTime = 4;
        			fEventVolume = 1.0f;
        			fEventFadeTime = 0.5f;
        			break;
        		case 2:
        			sMusicFile = "18_amb.ogg";
        			sEventMusicFile = "18_puzzle_valve01.ogg";
        			fVolume = 1.0f;
        			fFadeTime = 4;
        			fEventVolume = 1.0f;
        			fEventFadeTime = 0;
        			break;
        		case 3:
        			sMusicFile = "23_amb02.ogg";
        			sEventMusicFile = "18_puzzle_valve02.ogg";
        			fVolume = 1.0f;
        			fFadeTime = 4;
        			fEventVolume = 1.0f;
        			fEventFadeTime = 0;
        			break;
        	}

        	if(abPlayEventMusic && sEventMusicFile!="")
        		PlayMusic(sEventMusicFile, false, fEventVolume, fEventFadeTime, 10, false);
        	if(sMusicFile!="")
        	{
        		AddDebugMessage("Playing music : " + sMusicFile, false);
        		PlayMusic(sMusicFile, true, fVolume, fFadeTime, 0, true);
        	}
        }

        void RotateWaterValve01(string asEntity, int alState)
        {
        	if(alState != 1) return;

        	SetLampLit("torch_static01_10", false, true);
        	SetLampLit("torch_static01_4", false, true);
        	SetLampLit("torch_static01_3", false, true);
        	SetLampLit("torch_static01_13", false, true);

        	FadeLightTo("PointLight_14", 0,0,0,0, -1, 2);
        	FadeLightTo("PointLight_7", 0,0,0,0, -1, 2);
        	FadeLightTo("PointLight_17", 0,0,0,0, -1, 2);

        	FadeLightTo("SpotLight_8", 0.05f,0.15f,0.2f,-1, -1, 2);

        	CreateParticleSystemAtEntity("AreaWindValve_1", "ps_dust_whirl.ps", "AreaWindValve_1", false);

        	PlaySoundAtEntity("blow", "18_blow_wind", "Player", 0.0f, false);
        	PlaySoundAtEntity("PumpClank", "18_valve_off", "Player", 0.0f, false);
        	PlaySoundAtEntity("Clank", "14_elevator_clank", "Player", 0.0f, false);

        	AddTimer("PumpDelay", 1.5f, nameof(TimerPumpDelay));
        }


        void RotateWaterValve02(string asEntity, int alState)
        {
        	if(alState != 1) return;

        	SetLampLit("torch_static01_11", false, true);
        	SetLampLit("torch_static01_2", false, true);
        	SetLampLit("torch_static01_9", false, true);
        	SetLampLit("torch_static01_7", false, true);
        	SetLampLit("torch_static01_12", false, true);

        	FadeLightTo("PointLight_6", 0,0,0,0, -1, 2);
        	FadeLightTo("PointLight_13", 0,0,0,0, -1, 2);
        	FadeLightTo("PointLight_11", 0,0,0,0, -1, 2);
        	FadeLightTo("PointLight_16", 0,0,0,0, -1, 2);

        	FadeLightTo("SpotLight_4", 0.05f,0.15f,0.2f,-1, -1, 2);
        	FadeLightTo("SpotLight_2", 0.05f,0.15f,0.2f,-1, -1, 2);
        	FadeLightTo("SpotLight_3", 0.05f,0.15f,0.2f,-1, -1, 2);

        	CreateParticleSystemAtEntity("AreaWindValve_2", "ps_dust_whirl.ps", "AreaWindValve_2", false);

        	PlaySoundAtEntity("blow", "18_blow_wind", "Player", 0.0f, false);
        	PlaySoundAtEntity("PumpClank", "18_valve_off", "Player", 0.0f, false);
        	PlaySoundAtEntity("Clank", "14_elevator_clank", "Player", 0.0f, false);

        	CompleteQuest("18_Bridge", "18_Bridge");	//Incase player managed to skip it

        	AddTimer("PumpDelay", 1.5f, nameof(TimerPumpDelay));
        }

        void RotateWaterValve03(string asEntity, int alState)
        {
        	if(alState != 1) return;

        	SetLampLit("torch_static01_6", false, true);
        	SetLampLit("torch_static01_5", false, true);
        	SetLampLit("torch_static01_8", false, true);

        	FadeLightTo("SpotLight_11", 0,0,0,0, -1, 2);
        	FadeLightTo("PointLight_10", 0,0,0,0, -1, 2);
        	FadeLightTo("PointLight_15", 0,0,0,0, -1, 2);
        	FadeLightTo("PointLight_12", 0,0,0,0, -1, 2);

        	FadeLightTo("SpotLight_7", 0.05f,0.15f,0.2f,-1, -1, 2);
        	FadeLightTo("SpotLight_5", 0.05f,0.15f,0.2f,-1, -1, 2);
        	FadeLightTo("SpotLight_6", 0.05f,0.15f,0.2f,-1, -1, 2);

        	CreateParticleSystemAtEntity("AreaWindValve_3", "ps_dust_whirl.ps", "AreaWindValve_3", false);

        	PlaySoundAtEntity("blow", "18_blow_wind", "Player", 0.0f, false);
        	PlaySoundAtEntity("PumpClank", "18_valve_off", "Player", 0.0f, false);
        	PlaySoundAtEntity("Clank", "14_elevator_clank", "Player", 0.0f, false);

        	AddTimer("PumpDelay", 1.5f, nameof(TimerPumpDelay));
        }

        void TimerPumpDelay(string asTimer)
        {
        	PumpWater();

        	//Enable new interact messages on wall
        	SetEntityPlayerInteractCallback("AreaTouchWall1", nameof(InteractPrisonWall), true);
        	SetEntityPlayerInteractCallback("AreaTouchWall2", nameof(InteractPrisonWall), true);
        }

        ////////////////////////////
        // Pump water (and do effects)
        float[] gvAmbientColorLevels = {
        	0.025f * 0.8f, 0.15f * 0.8f, 0.18f * 0.8f,
        	0.025f * 0.6f, 0.15f * 0.6f, 0.18f * 0.6f,
        	0.025f * 0.3f, 0.15f * 0.3f, 0.18f * 0.3f
        };

        void PumpWater()
        {
        	AddQuest("18Valves", "18Valves");

        	int lCount = GetLocalVarInt("PumpWaterCount");

        	//Active event area
        	SetEntityActive("AreaLurkerEventCenter", true);

        	//Fade ambient light.
        	// AngelScript aborts here on a fourth pump, after activating the event area.
        	if(lCount < 0 || lCount >= gvAmbientColorLevels.Length / 3) return;
        	FadeLightTo(	"BoxLight_1",
        			gvAmbientColorLevels[lCount*3 + 0], gvAmbientColorLevels[lCount*3 + 1], gvAmbientColorLevels[lCount*3 + 2],
        			1, -1, 0.5f);

        	//play rumble sound
        	PlaySoundAtEntity("PumpRumble", "18_pump_rumble", "Player", 4, false);
        	PlaySoundAtEntity("PumpFlow", "18_fill_water", "Player", 0.5f, false);

        	//Stop the flow particle system
        	if(lCount!=0)
        	{
        		//StopMusic(8, 0);
        		DestroyParticleSystem("PipeFlow");
        		StopSound("FlowSound", 2.0f);
        	}

        	//Start particles, music and sound at pipe
        	// lCount+1 == valves turned at this point
        	StartMusicHelper(lCount+1, true);
        	if(lCount==0)
        	{
        		StartScreenShake(0.02f, 2, 1, 1);

        		PlayMusic("18_puzzle_valve01.ogg", false, 0.8f, 0.5f, 10, false);
        		//PlayMusic("18_amb.ogg", true, 0.3, 8, 0, true);
        		CreateParticleSystemAtEntity("PipeFlow", "ps_pipe_flow_small", "AreaPipeFlow", true);
        		PlaySoundAtEntity("FlowSound", "18_water_pour1", "AreaPipeFlow", 1, true);

        		SetLocalVarInt("TouchWall", 2); bFirst = true;
        	}
        	else if(lCount==1)
        	{
        		StartScreenShake(0.035f, 2.5f, 1, 1);

        		//PlayMusic("18_puzzle_valve01.ogg", false, 1.0, 0, 10, false);
        		//PlayMusic("18_amb.ogg", true, 0.7, 8, 0, true);
        		CreateParticleSystemAtEntity("PipeFlow", "ps_pipe_flow_medium", "AreaPipeFlow", true);
        		PlaySoundAtEntity("FlowSound", "18_water_pour2", "AreaPipeFlow", 1, true);

        		SetLocalVarInt("TouchWall", 3); bFirst = true;
        	}
        	else if(lCount==2)
        	{
        		StartScreenShake(0.05f, 3, 1, 1);

        		//PlayMusic("18_puzzle_valve02.ogg", false, 1.0, 0, 10, false);
        		//PlayMusic("18_amb.ogg", true, 1.0, 8, 0, true);
        		CreateParticleSystemAtEntity("PipeFlow", "ps_pipe_flow_large", "AreaPipeFlow", true);
        		PlaySoundAtEntity("FlowSound", "18_water_pour3", "AreaPipeFlow", 1, true);

        		SetLocalVarInt("TouchWall", 4); bFirst = true;

        		AddTimer("LastWallMess", 25.0f, nameof(TimerLastWallMess));

        		CompleteQuest("18Valves", "18Valves");

        		SetEntityActive("AreaGuardian", true);
        	}

        	//Quest
        	if(lCount==2)
        	{
        		CompleteQuest("SewerFlooded", "16SewerFlooded");
        		GiveSanityBoostSmall();
        		SetGlobalVarInt("WaterHasBeenDrained", 1);
        		PlaySoundAtEntity("ambscare", "18_amb_end", "AreaPipeFlow", 8.0f, true);
        	}

        	//Start timer
        	AddTimer("PumpRumble", 4, nameof(TimerPumpRumbleStop));


        	AddLocalVarInt("PumpWaterCount", 1);

        	AddTimer("WaterHoleVoice", 1, nameof(TimerWaterHoleVoice));
        }

        void TimerPumpRumbleStop(string asTimer)
        {
        	StopSound("PumpRumble", 1);
        }

        void TimerLastWallMess(string asTimer)
        {
        	AddDebugMessage("Last message active!", false);

        	SetLocalVarInt("TouchWall", 5); bFirst = true;

        	//Enable new interact messages on wall
        	SetEntityPlayerInteractCallback("AreaTouchWall1", nameof(InteractPrisonWall), true);
        	SetEntityPlayerInteractCallback("AreaTouchWall2", nameof(InteractPrisonWall), true);
        }

        ////////////////////////////
        // Voices from water release hole
        void TimerWaterHoleVoice(string asTimer)
        {
        	if(GetLocalVarInt("PrisonerDrowned")==1)
        		return;

        	string sFile = "";
        	//float fNextVoiceTime = 0;
        	int lCount = GetLocalVarInt("PumpWaterCount");

        	// No water coming in, just pants
        	if(lCount==0)
        	{
        		sFile = "18_prisoner1";
        		//fNextVoiceTime = RandFloat(10, 20);
        	}
        	// Little water coming in, worried
        	else if(lCount==1)
        	{
        		StopSound("WaterHoleVoice", 0.0f);
        		sFile = "18_prisoner2";
        		//fNextVoiceTime = RandFloat(7, 15);
        	}
        	// Much water coming in, screaming, about to drown!
        	else if(lCount==2)
        	{
        		StopSound("WaterHoleVoice", 0.0f);
        		sFile = "18_prisoner3";
        		//fNextVoiceTime = RandFloat(5, 10);
        	}

        	else if(lCount==3)
        	{
        		StopSound("WaterHoleVoice", 0.0f);
        		sFile = "18_prisoner4";
        		SetLocalVarInt("PrisonerDrowned", 1);
        		//fNextVoiceTime = RandFloat(5, 10);
        	}

        	if(sFile != "")
        		PlaySoundAtEntity("WaterHoleVoice", sFile, "AreaWaterHoleVoice",0, false);

        	//if(fNextVoiceTime>0)
        		//AddTimer("WaterHoleVoice", fNextVoiceTime, "TimerWaterHoleVoice");
        }


        ////////////////////////////
        // Pipe Steam
        void TimerSteam(string asTimer)
        {
        	string sCount = StringSub(asTimer, 5,1);

        	string sVar = "PlayerAtAreaPushPlayer_"+sCount;
        	if(GetLocalVarInt(sVar)==1)
        	{
        		GivePlayerDamage(20, "BloodSplat", true, false);
        		PlaySoundAtEntity("sd1", "18_burn_steam", "Player", 0.0f, false);
        	}

        	CreateParticleSystemAtEntity("deadlysteam"+sCount, "ps_steam_deadly", "AreaDeadlySteam_"+sCount, false);
        	PlaySoundAtEntity("sound_steam_deadly"+sCount,"18_steam_release", "AreaDeadlySteam_"+sCount, 0.2f, false);

        	float fNextSteam = 1;
        	if(sCount == "1")	fNextSteam = 4.25f;
        	if(sCount == "2")	fNextSteam = 4.1f;
        	if(sCount == "3")	fNextSteam = 4.5f;

        	AddTimer("steam"+sCount, fNextSteam, nameof(TimerSteam));

        	AddTimer("Off"+asTimer, 2.5f, nameof(TimerTurnOffSteam));
        	SetLocalVarInt("SteamAtAreaPushPlayer_"+sCount, 1);
        }

        void TimerTurnOffSteam(string asTimer)
        {
        	string sCount = StringSub(asTimer, 8, 1);

        	StopSound("sound_steam_deadly"+sCount, 0.3f);
        	SetLocalVarInt("SteamAtAreaPushPlayer_"+sCount, 0);
        }

        void CollidePlayerSteam(string asParent, string asChild, int alState)
        {
        	if(alState==0) return;

        	SetLocalVarInt("PlayerAt"+asChild, alState==1 ? 1 : 0);

        	if(alState==1 && GetLocalVarInt("SteamAt"+asChild)==1)
        	{
        		GivePlayerDamage(20, "BloodSplat", true, false);
        		PlaySoundAtEntity("sd1", "18_burn_steam", "Player", 0.0f, false);
        	}
        }

        void InteractSteamValve(string asEntity, int alState)
        {
        	if(alState == 0) return;

        	//Turn off steam
        	if(alState == 1 && GetLocalVarInt("PipeSteamOff")==0)
        	{
        		SetLocalVarInt("PipeSteamOff", 1);

        		PlaySoundAtEntity("steamoff", "13_rod_out", asEntity, 0.0f, false);

        		for(int i=1; i<=3; ++i)
        			RemoveTimer("steam"+i);
        	}

        	//Turn on steam
        	if(alState == -1 && GetLocalVarInt("PipeSteamOff")==1)
        	{
        		SetLocalVarInt("PipeSteamOff", 0);

        		for(int i=1; i<=3; ++i)
        			AddTimer("steam"+i, 0, nameof(TimerSteam));
        	}
        }

        ////////////////////////////
        // Break bridge
        void CollideBreakBridge(string asParent, string asChild, int alState)
        {
        	for(int i=1; i<=2; ++i)
        		RemoveEntityCollideCallback("stone_small01*", "AreaBreakBridge_"+i);

        	CreateParticleSystemAtEntity("BreakBridge", "ps_break_wood_small", "AreaBreakBridge_1", false);
        	PlaySoundAtEntity("BreakBridge","break_wood_metal", "AreaBreakBridge_1", 0, false);

        	BreakJoint("bridge_chained_1_BallJoint_1");

        	SetEntityPlayerInteractCallback("bridge_chained_1", "", false);

        	AddTimer("ChainForce",0.3f,nameof(TimerChainForce));

        	CompleteQuest("18_Bridge", "18_Bridge");

        	SetEntityActive("bridgeblocker_1", false);
        }

        void TimerChainForce(string asTimer)
        {
        	AddBodyImpulse("bridge_chained_1_chain", 1,0.5f,-2, "World");
        	PlaySoundAtEntity("creak", "18_bridge_fall", "bridge_chained_1", 0.0f, false);
        }

        void InteractBridge(string asEntity)
        {
        	SetMessage("Ch02Level18", nameof(InteractBridge), 0);

        	PlayGuiSound("18_touch_bridge.snt", 1.0f);
        	AddPropImpulse(asEntity, 0.0f, 4.0f, 2.0f, "World");

        	AddQuest("18_Bridge", "18_Bridge");

        	AddTimer(asEntity, 1.0f, nameof(TimerInteractBridge));
        }
        void TimerInteractBridge(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractBridge), true);
        }

        void CollideImpactBridge(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("impact", "18_bridge_hit", asChild, 0.0f, false);
        	CreateParticleSystemAtEntity("impps", "ps_dust_impact", asChild, false);
        }

        ////////////////////////////
        // Water Lurker Scares
        void CollideLurkerEventCenter(string asParent, string asChild, int alState)
        {
        	int lCount = GetLocalVarInt("PumpWaterCount");

        	if(lCount==1)
        	{
        		PlaySoundAtEntity("LurkerSound","water_lurker_idle.snt", "AreaCenterSound_1", 0, false);
        	}
        	else if(lCount==2)
        	{
        		PlaySoundAtEntity("LurkerSound","water_lurker_eat.snt", "AreaCenterSound_2", 0, false);
        	}
        	else if(lCount==3)
        	{
        		PlaySoundAtEntity("LurkerSound","water_lurker_hunt.snt", "AreaCenterSound_3", 0, false);
        	}

        	SetEntityActive(asChild, false);
        }

        void CollideLurkerCorridorEvent1(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("LurkerSound","water_lurker_idle.snt", "AreaLurkerSound_1", 0, false);
        }

        void CollideLurkerCorridorEvent2(string asParent, string asChild, int alState)
        {
        	AddTimer("AreaLurkerSound_2", 0.3f, nameof(TimerLurkerSplash));
        	//AddTimer("AreaLurkerSound_21", 1.3, "TimerLurkerSplash");
        	//AddTimer("AreaLurkerSound_22", 2.5, "TimerLurkerSplash");
        }

        void CollideLurkerCorridorEvent3(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("LurkerSound","water_lurker_idle.snt", "AreaLurkerSound_3", 0, false);
        	AddTimer("AreaLurkerSound_3", 0.5f, nameof(TimerLurkerSplash));
        	//AddTimer("AreaLurkerSound_31", 1.5, "TimerLurkerSplash");
        }

        void TimerLurkerSplash(string asTimer)
        {
        	PlaySoundAtEntity("LurkerSound","impact_water_high.snt", asTimer, 0, false);
        	CreateParticleSystemAtEntity("LurkerPS", "waterlurker_walk_splash", asTimer, false);
        }


        //////////////
        //PICK DIARIES
        void EntityCallPickDiary(string asEntity, string type)
        {
        	AddLocalVarInt("DiaryNr", 1);

        	PlayMusic("18_paper_daniel0"+GetLocalVarInt("DiaryNr"), false, 0.5f, 0.0f, 10, false);
        }
        //////////////


        ////////////
        //Roses for vision
        void CollideAreaRoseOn(string asParent, string asChild, int alState)
        {
        	PlayGuiSound("general_wind_whirl", 0.7f);

        	AddTimer("AreaRose_1", 1.5f, nameof(TimerAreaRose));
        	AddTimer("AreaRose_2", 2.0f, nameof(TimerAreaRose));
        	AddTimer("AreaRose_3", 2.5f, nameof(TimerAreaRose));
        	AddTimer("AreaRose_4", 3.0f, nameof(TimerAreaRose));
        	AddTimer("AreaRose_5", 3.5f, nameof(TimerAreaRose));
        	AddTimer("AreaRose_6", 4.0f, nameof(TimerAreaRose));
        	AddTimer("AreaRose_7", 5.5f, nameof(TimerAreaRose));
        	AddTimer("AreaRose_8", 6.0f, nameof(TimerAreaRose));
        }
        void TimerAreaRose(string asTimer)
        {
        	CreateParticleSystemAtEntity(asTimer, "ps_rose_petals.ps", asTimer, false);

        	if(asTimer == "AreaRose_2")
        		PlayGuiSound("general_wind_whirl", 0.6f);
        }
        void CollideAreaRoseOff(string asParent, string asChild, int alState)
        {
        	DestroyParticleSystem("rose_1");
        	DestroyParticleSystem("rose_2");
        	DestroyParticleSystem("rose_3");
        }
        ///////////////////


        ///////////
        //INTERACT PRISON WALLS
        bool bFirst = true;
        void InteractPrisonWall(string asEntity)
        {
        	if(GetLocalVarInt("TouchWall") == 1 && bFirst){
        		SetMessage("Ch02Level18", "TouchPrison01", 0);
        		bFirst = false;
        	}
        	else if(GetLocalVarInt("TouchWall") == 2 && bFirst){
        		SetMessage("Ch02Level18", "TouchPrison02", 0);
        		bFirst = false;
        	}
        	else if(GetLocalVarInt("TouchWall") == 3 && bFirst){
        		SetMessage("Ch02Level18", "TouchPrison03", 0);
        		bFirst = false;
        	}
        	else if(GetLocalVarInt("TouchWall") == 4 && bFirst){
        		SetMessage("Ch02Level18", "TouchPrison04", 0);
        		bFirst = false;
        	}
        	else if(GetLocalVarInt("TouchWall") == 5 && bFirst){
        		SetMessage("Ch02Level18", "TouchPrison05", 0);
        		bFirst = false;
        	}

        	PlayGuiSound("18_touch_rock.snt", 1.0f);

        	SetEntityPlayerInteractCallback("AreaTouchWall1", "", true);
        	SetEntityPlayerInteractCallback("AreaTouchWall2", "", true);

        	//AddTimer(asEntity, 1.0f, "TimerInteractPrisonWall");
        }
        void TimerInteractPrisonWall(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractPrisonWall), true);
        }
        //////////


        ///////////////////////////
        //EXIT LEVEL GUARDIAN EVENT
        void CollideAreaGuardian(string asParent, string asChild, int alState)
        {
        	if(GetPlayerSanity() > 75) return;

        	PlayGuiSound("guardian_ontop", 1.0f);

        	StartScreenShake(0.01f, 1.0f, 1.0f, 2.0f);
        	SetRadialBlurStartDist(0.4f);
        	FadeRadialBlurTo(0.1f, 1.0f);

        	SetPropActiveAndFade("Slime_*", true, 2.0f);

        	FadeLightTo("BoxLight_1", 0.2f, 0.1f, 0.02f, -1, -1, 4.0f);

        	PlaySoundAtEntity("slime1", "slime_loop", "Slime_1", 2.0f, true);
        	PlaySoundAtEntity("slime2", "slime_loop", "Slime_5", 2.0f, true);
        	PlaySoundAtEntity("slime3", "slime_loop", "Slime_10", 2.0f, true);
        	PlaySoundAtEntity("slime4", "slime_loop", "Slime_17", 2.0f, true);
        	PlaySoundAtEntity("slime5", "slime_loop", "Slime_12", 2.0f, true);

        	AddTimer("lurky", 1.0f, nameof(TimerStartLurky));
        	AddTimer("breath", 3.0f, nameof(TimerStartLurky));
        	AddTimer("guardon", 5.0f, nameof(TimerGuardNoises));

        	for(int i=1;i<=5;i++)
        		SetEntityActive("SlimeDamageArea_"+i, true);

        	SetLocalVarInt("GuardianEvent", 1);

        	PlayMusic("07_amb.ogg", true, 0.8f, 1, 5, true);

        	CheckPoint("cp1", "PlayerStartArea_4", nameof(CPCallCheckPoint), "Hints", "18_WaterLurk");
        }
        void TimerStartLurky(string asTimer)
        {
        	SetEntityActive("waterlurker_music02_1", true);
        	FadeRadialBlurTo(0.0f, 1.0f);
        	PlayGuiSound("react_scare", 0.8f);
        	GiveSanityDamage(15.0f, true);

        	if(asTimer == "breath")
        		PlayGuiSound("react_breath", 0.7f);
        }
        void TimerGuardNoises(string asTimer)
        {
        	if(asTimer == "guardon"){
        		PlayGuiSound("guardian_ontop", 1.0f);
        		StartScreenShake(0.01f, 1.0f, 1.0f, 2.0f);
        		FadeRadialBlurTo(0.05f, 1.0f);
        		AddTimer("guardoff", 4.0f, nameof(TimerGuardNoises));
        	}
        	else if(asTimer == "guardoff"){
        		FadeRadialBlurTo(0.0f, 1.0f);
        		AddTimer("guardon", RandFloat(8.0f,16.0f), nameof(TimerGuardNoises));
        	}
        }
        void CPCallCheckPoint(string asName, int alCount)
        {
        	FadeRadialBlurTo(0.0f, 0.0f);
        	RemoveTimer("guardon");
        	RemoveTimer("guardoff");
        	SetEntityActive("Slime_*", false);
        	SetEntityActive("waterlurker_music02_1", false);
        	FadeLightTo("BoxLight_1", 0.025f, 0.15f, 0.18f, -1, -1, 0);

        	SetEntityActive("waterlurker_1", true);
        	for(int i=1;i<=5;i++) StopSound("slime"+i, 2.0f);
        }
        ////////////


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("Cistern");

        	////////////////////////////////////////
        	// Water Lurker Scares
        	AddEntityCollideCallback("Player", "AreaLurkerEventCenter",nameof(CollideLurkerEventCenter), false, 1);

        	for(int i=1; i<=3; ++i)
        		AddEntityCollideCallback("Player", "AreaLurkerCorridorEvent_"+i, "CollideLurkerCorridorEvent"+i, true, 1);

        	////////////////////////////////////////
        	// Break bridge
        	for(int i=1; i<=2; ++i)
        		AddEntityCollideCallback("stone_small01*", "AreaBreakBridge_"+i,  nameof(CollideBreakBridge), false, 1);
        	AddEntityCollideCallback("bridge_chained_1", "AreaImpactBridge",  nameof(CollideImpactBridge), true, 1);

        	////////////////////////////////////////
        	// Steam stuff
        	for(int i=1; i<=3; ++i)
        	{
        		AddEntityCollideCallback("Player", "AreaPushPlayer_"+i, nameof(CollidePlayerSteam), false, 0);
        	}


        	SetNumberOfQuestsInMap(1);
        	//Guardian stuff
        	AddEntityCollideCallback("Player", "AreaGuardian", nameof(CollideAreaGuardian), true, 1);
        	SetEntityActive("Slime_*", false);

        	//ROSE STUFF
        	AddEntityCollideCallback("Player", "AreaRoseOff", nameof(CollideAreaRoseOff), true, 1);
        	AddEntityCollideCallback("Player", "FlashbackArea_1", nameof(CollideAreaRoseOn), true, 1);

        	//Touch stuff
        	SetLocalVarInt("TouchWall", 1);

        	////////////////////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		//SetEntityActive("AreaGuardian", true);

        		GiveItemFromFile("lantern", "lantern.ent");

        		for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");
        	}
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	//////////////////////////
        	// Hole voices
        	if(GetLocalVarInt("PipeSteamOff")==0)
        	{
        		for(int i=1; i<=3; ++i)
        			RemoveTimer("steam"+i);

        		for(int i=1; i<=3; ++i)
        			AddTimer("steam"+i, 1+i, nameof(TimerSteam));
        	}

        	PreloadSound("break_wood_metal"); PreloadSound("18_water_pour3"); PreloadSound("18_water_pour2"); PreloadSound("18_water_pour1");
        	PreloadSound("18_blow_wind"); PreloadSound("18_valve_off"); PreloadSound("14_elevator_clank"); PreloadSound("18_pump_rumble");
        	PreloadSound("18_fill_water"); PreloadSound("18_burn_steam"); PreloadSound("18_steam_release"); PreloadSound("13_rod_out");
        	PreloadSound("18_bridge_fall"); PreloadSound("18_bridge_hit"); PreloadSound("water_lurker_idle"); PreloadSound("water_lurker_eat");
        	PreloadSound("water_lurker_hunt"); PreloadSound("impact_water_high");

        	//////////////////////////
        	// Hole voices
        	AddTimer("WaterHoleVoice", 5, nameof(TimerWaterHoleVoice));

        	////////////////////////////////////////
        	// Stop water flow if sewer drained
        	if(QuestIsCompleted("SewerFlooded") && GetLocalVarInt("FlowDisabled")==0)
        	{
        		DestroyParticleSystem("PipeFlow");
        		StopSound("FlowSound", 0);
        		SetLocalVarInt("FlowDisabled", 1);
        	}

        	////////////////////////////////////////
        	// Select music depending on the map state
        	SetLocalVarInt("IAmStart", 1);
        	StartMusicHelper(GetLocalVarInt("PumpWaterCount"), false);
        	SetLocalVarInt("IAmStart", 0);

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	//StopMusic(0, 3);
        	if(GetLocalVarInt("GuardianEvent") == 1){
        		StopMusic(2.0f, 5);
        		FadeRadialBlurTo(0.0f, 0.0f);
        		RemoveTimer("guardon");
        		SetEntityActive("Slime_*", false);
        		SetEntityActive("waterlurker_music02_1", false);
        		SetEntityActive("waterlurker_1", false);
        		FadeLightTo("BoxLight_1", 0.025f, 0.15f, 0.18f, -1, -1, 0);
        		for(int i=1;i<=5;i++) StopSound("slime"+i, 0.0f);
        		for(int i=1;i<=5;i++)
        			SetEntityActive("SlimeDamageArea_"+i, false);
        	}
        }
    }
}
