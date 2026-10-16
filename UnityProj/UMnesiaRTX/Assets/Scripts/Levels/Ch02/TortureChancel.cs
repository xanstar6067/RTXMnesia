using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/22_torture_chancel.hps; journal: docs/ScriptPort/ch02/22_torture_chancel.md.</summary>
    [HplMapScript("maps/main/ch02/22_torture_chancel.map")]
    public class TortureChancel : HplScript
    {
        //------------------------------------------

        ///////////////////////////////////////////
        // BUILD ORB
        ////////////////////////////////////////////

        //------------------------------------------

        void UseStringOnPedestal(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level21", nameof(UseStringOnPedestal), 0);
        }

        //------------------------------------------

        void UseOrbPieceOnPedestal(string asItem, string asEntity)
        {
        	////////////////////
        	//Check if tar has been added
        	if(GetGlobalVarInt("TarOnPedestal")==0 && GetGlobalVarInt("OrbsAddedCount")==1)
        	{
        		SetMessage("Ch02Level22", "UseOrbPieceAndNoTar", 0);
        		AddQuest("22NeedTar", "22NeedTar");
        		PlaySoundAtEntity("prbd", "22_orb_loose.snt", asEntity, 1.0f, false);
        		return;
        	}

        	////////////////////
        	//Increase count and remove item
        	AddGlobalVarInt("OrbsAddedCount",1);
        	int lCurrentCount = GetGlobalVarInt("OrbsAddedCount");
        	RemoveItem(asItem);

        	////////////////////
        	//Make the orb piece visible

        	//All orbs after first forge.
        	if(lCurrentCount > 1)
        	{
        		SetPropActiveAndFade("orb_assembly_"+lCurrentCount, true, 1.0f);
        		AddTimer("orb_assembly_"+(lCurrentCount-1), 0.8f, nameof(TimerFadeProp));
        	}
        	//First should just pop up.
        	else
        	{
        		SetEntityActive("orb_assembly_"+lCurrentCount, true);
        	}

        	////////////////////
        	//Play sound
        	PlaySoundAtEntity("orbd", "22_orb_loose.snt", asEntity, 0.5f, false);

        	////////////////////
        	//Forge Effect
        	if(lCurrentCount>1)
        	{
        		PlaySoundAtEntity("forges", "22_orb_forge.snt", asEntity, 0.5f, false);
        		CreateParticleSystemAtEntity("orb_repair", "ps_orb_repair_effect.ps", "AreaOrbEffect", false);
        	}

        	////////////////////
        	//Add Piece Number spefics
        	if(lCurrentCount ==1)
        	{
        		//Remove interact callback on pedestal
        		SetEntityPlayerInteractCallback("AreaOrbEffect","", false);

        	}
        	else if(lCurrentCount <=5)
        	{

        	}
        	//NOTE: This cannot happen in this map! Just heer for testing completnes
        	else if(lCurrentCount ==6)
        	{
        		SetLampLit("orb_pedestal_1", true, true);
        	}
        }


        //------------------------------------------

        //Used to fade prop with a small delay!
        void TimerFadeProp(string asTimer)
        {
        	SetPropActiveAndFade(asTimer, false, 1.0f);
        }

        //------------------------------------------

        //Grease up the pedestal
        void UseTarOnPedestal(string asItem, string asEntity)
        {
        	RemoveItem(asItem);

        	//Tar has been added!
        	SetGlobalVarInt("TarOnPedestal",1);
        	CompleteQuest("22NeedTar", "22NeedTar");

        	PlaySoundAtEntity("tars", "22_use_tar.snt", asEntity, 1.0f, false);

        	SetEntityActive("orb_pedistal_tar_1", true);
        }

        //------------------------------------------

        void InteractOrbPedestal(string asEntity)
        {
        	SetMessage("Ch02Level22", nameof(InteractOrbPedestal), 0);
        	AddQuest("22FoundPedestal", "22FoundPedestal");
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // FALL DEATH
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideFallDeath(string asParent, string asChild, int alState)
        {
        	/////////////////////////////////
        	//A respawn at random position.
        	// If ambush is in progress, we need to use it's checkpoint!
        	if(GetLocalVarInt("AmbushStarted")==0)
        	{
        		if(GetLocalVarInt("CPAltar") != 1)
        			CheckPoint("checkpoint_default", "PlayerStartArea_6", nameof(CPCallAltar), "", "");
        		else{
        			int lRand = RandInt(0,2);
        			if(lRand==0) CheckPoint("checkFallDeath1", "PlayerStartArea_1", "CheckPointFallWell", "Hints", "DeathFall_21_TortureNave");
        			if(lRand==1) CheckPoint("checkFallDeath2", "PlayerStartArea_5", "CheckPointFallWell", "Hints", "DeathFall_21_TortureNave");
        			if(lRand==2) CheckPoint("checkFallDeath3", "PlayerStartArea_6", "CheckPointFallWell", "Hints", "DeathFall_21_TortureNave");
        		}
        	}

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



        void CheckPointFallDeath(string asName, int alCount)
        {
        	//Do nothing??
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // AMBUSH
        ////////////////////////////////////////////

        //------------------------------------------

        //Rumble and activate areas for ambush!
        //Ifplayer has all orbs that is!
        void CollideAmbushRumble(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Starting ambush!", false);

        	//If player does not have all orbs, skip
        	if(GetGlobalVarInt("ChoirOrbCount")!=3 || GetGlobalVarInt("TranseptOrbCount")!=3)
        	{
        		return;
        	}

        	///////////////////////
        	//Lower player speed a bit
        	SetPlayerMoveSpeedMul(0.6f);

        	///////////////////////
        	//Activate areas
        	SetEntityActive("AreaGruntsStartHunt", true);
        	SetEntityActive("AreaAmbushStart", true);
        	SetEntityActive("AreaAmbushInstantKill", true);

        	///////////////////////
        	//Turn off flashback
        	SetEntityActive("FlashbackArea_1", false);

        	///////////////////////
        	//Lock level door
        	SetLevelDoorLocked("level_dungeon_1", true);

        	///////////////////////
        	//Shake screen
        	StartScreenShake(0.02f, 6, 2, 2);

        	PlaySoundAtEntity("AmbushRumbleLoop", "21_alex_low_freq_rumble.snt", asChild, 2, false);
        	PlayerReact(true, 1.0f);
        	AddTimer("AmbushRumbleFadeOut", 6.0f, nameof(TimerAmbushRumbleFadeOut));

        	///////////////////////
        	//Setup the checkpoint
        	CheckPoint("checkGruntDeath1", "PlayerStartDarkPlace", nameof(CheckPointGrunt), "Hints", "DeathGrunt_22_Chancel");

        	///////////////////////
        	//Remove the callback
        	RemoveEntityCollideCallback(asParent, asChild);

        	////////////////////
        	//Set a local var, that is used by fall damage just in case player manages to throw himself outside.
        	SetLocalVarInt("AmbushStarted",1);

        }

        void TimerAmbushRumbleFadeOut(string asTimer)
        {
        	SetPlayerMoveSpeedMul(1.0f);

        	StopSound("AmbushRumbleLoop", 2);
        }

        //------------------------------------------

        //Swing door slams shut and the enemies appear
        void CollideAmbushStart(string asParent, string asChild, int alState)
        {
        	PlayMusic("22_event_trapped.ogg", true, 1.0f, 1.0f, 10, false);

        	PlayerReact(true, 0.5f);

        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_74", 1.0f, "");
        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_31", 1.0f, "");

        	AddEnemyPatrolNode("servant_grunt_2", "PathNodeArea_39", 1.0f, "");
        	AddEnemyPatrolNode("servant_grunt_2", "PathNodeArea_27", 3.0f, "");

        	AddEnemyPatrolNode("servant_grunt_3", "PathNodeArea_23", 2.0f, "");
        	AddEnemyPatrolNode("servant_grunt_3", "PathNodeArea_59", 3.0f, "");

        	//If player tries to run back he dies.
        	SetEntityActive("AreaAmbushInstantKill2", true);

        	//Make the grunts active, and then show them the player pos after a little delay
        	for(int i=1; i<=3; ++i)
        	{
        		SetEntityActive("servant_grunt_"+i, true);
        		AddTimer("servant_grunt_"+i, 0.533f*i, nameof(TimerStartGrunt));
        	}



        	//Slam and Lock door (skip this
        	//PlaySoundAtEntity("slamdoor", "impact_wood_heavy_high.snt",  "sewer_arched_6", 0, false);
        	//SetSwingDoorLocked("sewer_arched_6", true, true);
        }

        void TimerStartGrunt(string asTimer)
        {
        	SetEnemyDisableTriggers(asTimer, false);
        	ShowEnemyPlayerPosition(asTimer);
        }

        //------------------------------------------

        //The grunts or player are close enough
        void CollideGruntsStartHunt(string asParent, string asChild, int alState)
        {
        	//Enemies will now see player! Do not start same to skipped the synched look.
        	AddTimer("servant_grunt_1", 0.5f, nameof(TimerEnableGruntTriggers));
        	AddTimer("servant_grunt_2", 1.2f, nameof(TimerEnableGruntTriggers));
        	AddTimer("servant_grunt_3", 0.1f, nameof(TimerEnableGruntTriggers));

        	SetEntityActive(asChild, false); //Disable area, since this event is connected to many entities.
        }


        void TimerEnableGruntTriggers(string asTimer)
        {
        	//SetEnemyDisableTriggers(asTimer, false);
        }

        //------------------------------------------

        //Player is close enough to the
        void CollideAmbushInstantKill(string asParent, string asChild, int alState)
        {
        	GivePlayerDamage(110, "Claws", true, true);
        	StartScreenShake(0.01f, 0.5f, 0.5f, 1.0f);
        	PlayGuiSound("attack_claw.snt", 0.7f);
        }

        //------------------------------------------

        //Change the map to level 25
        void CheckPointGrunt(string asName, int alCount)
        {
        	//Change map
        	AddTimer("ChangeMap", 5.0f, nameof(TimerChangeMap));
        	FadeOut(0.0f);
        	SetPlayerActive(false);
        	PlayGuiSound("22_start.snt", 1.0f);
        	PlayGuiSound("22_start_daniel.snt", 1.0f);
        }

        void TimerChangeMap(string asChangeMap)
        {
        	if(GetLocalVarInt("SkipAmbushTeleport")==1) return; //Debug skipping

        	//////////////////////
        	//Load Screen Setup
        	SetupLoadScreen("LoadingText", "Ch03_Captured01", 1, "game_loading_well.jpg");
        	ChangeMap("25_cell_tunnels.map","PlayerStartArea_1", "22_end.snt", "");
        }


        //------------------------------------------

        ///////////////////////////////////////////
        // WIND BREEZE
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideWindBreeze(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("BreezeActive")==1) return; //Another breeze is already playing.
        	if(RandInt(0, 2)!=0) return;

        	PlaySoundAtEntity("breeze", "27_wind.snt", asChild, 0, false);

        	//The particles
        	CreateParticleSystemAtEntityExt("breeze","ps_green_wind_breeze.ps", asChild, true, 1,1,1,0.5f,true, 1, 2, 100, 110);

        	//Set a var that a breeze is playing, so several do not play at the same time!
        	SetLocalVarInt("BreezeActive", 1);
        	AddTimer("BreezeOver",3,nameof(TimerBreezeOver));
        }

        void TimerBreezeOver(string asTimer)
        {
        	SetLocalVarInt("BreezeActive", 0);
        }


        //------------------------------------------

        ///////////////////////////////////////////
        // BARRIER MACHINE
        ////////////////////////////////////////////

        //------------------------------------------

        void InitBarrierMachine()
        {
        	float fSmallSpeed = 2.1f;
        	float fLargeSpeed = fSmallSpeed * 1.3333f; //4/3 ratio on wheel size

        	RotatePropToSpeed("barrier_machine_cog_1", 10, fSmallSpeed, 1, 0, 0, true, "");
        	RotatePropToSpeed("barrier_machine_cog_3", 10, fLargeSpeed, 0, 0, -1, true, "");
        	RotatePropToSpeed("barrier_machine_cog_4", 10, fLargeSpeed, 0, 0, 1, true, "");
        }


        //------------------------------------------

        ///////////////////////////////////////////
        // BARRIER TUNNEL
        ////////////////////////////////////////////

        //------------------------------------------

        void InitBarrierTunnel()
        {
        	float fRingSpeed = 1.0f;

        	for(int i=2; i<=8; ++i)
        	{
        		float fDir = i%2==0 ? -1 : 1;
        		RotatePropToSpeed("barrier_ring02_"+i, 10, fRingSpeed,0,0,fDir, true, "");
        	}
        }

        //------------------------------------------

        void CollideTunnelDamage(string asParent, string asChild, int alState)
        {
        	//Enter area
        	if(alState == 1)
        	{
        		AddQuest("22BarrierTunnel", "22BarrierTunnel");

        		TimerTunnelDamage("");
        	}
        	//Leave area
        	else
        	{
        		RemoveTimer("TunnelDamage");
        	}
        }

        void TimerTunnelDamage(string asTimer)
        {
        	AddPlayerBodyForce(0,0,9000*2.5f, false);
        	GivePlayerDamage(10, "BloodSplat", true, false);
        	PlayerReact(false, 0.5f);

        	PlayGuiSound("ui_elec_damage.snt", 1.0f);

        	AddTimer("TunnelDamage",2, nameof(TimerTunnelDamage));
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // FLASHBACK
        ////////////////////////////////////////////

        //------------------------------------------

        //When entering flashback turn on a brightlight at the end of the map.
        void CollideFlashbackArea(string asParent, string asChild, int alState)
        {
        	CreateParticleSystemAtEntity("flashorb", "ps_orb_light_fade.ps", "AreaFlashbackEffect", false);
        	AddTimer("petals", 2.0f, nameof(TimerPetals));
        }

        void TimerPetals(string asTimer)
        {
        	PlayGuiSound("general_wind_whirl.snt", 0.8f);
        	CreateParticleSystemAtEntity("roses", "ps_rose_petals_wind.ps", "ScriptArea_1",false);
        }


        void FlashbackOver()
        {
        	DestroyParticleSystem("flashorb");
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // ALEXANDER VOICE
        ////////////////////////////////////////////

        //------------------------------------------

        //Play alexander voice + rumble a bit.
        void CollideAlexanderVoice(string asParent, string asChild, int alState)
        {
        	AddEffectVoice("CH02L22_Alexander_01", "", "Voice", "CH02L22_Alexander_01", false, "", 0.0f, 0.0f);
        	AddEffectVoice("CH02L22_Alexander_02", "", "Voice", "CH02L22_Alexander_02", false, "", 0.0f, 0.0f);

        	AddTimer("AlexVoiceRumble", 1.0f, nameof(TimerAlexVoiceRumble));
        }

        void TimerAlexVoiceRumble(string asTimer)
        {
        	StartScreenShake(0.02f, 6, 2, 2);

        	PlaySoundAtEntity("AlexanderRumbleLoop", "21_alex_low_freq_rumble.snt", "AreaAlexanderVoice", 2, false);
        	PlayerReact(true, 2.0f);
        	AddTimer("AlexVoiceRumbleFadeOut", 6.0f, nameof(TimerAlexVoiceRumbleFadeOut));
        }

        void TimerAlexVoiceRumbleFadeOut(string asTimer)
        {
        	StopSound("AlexanderRumbleLoop", 2);
        }


        //------------------------------------------

        void CollideRoomCount(string asParent, string asChild, int alState)
        {
        	AddLocalVarInt("RoomCount",1);

        	AddDebugMessage("RoomCount:"+GetLocalVarInt("RoomCount"), false);

        	if(GetLocalVarInt("RoomCount") == 2 && GetLocalVarInt("NoGrunt") == 1)
        		SetEntityActive("servant_grunt_5", true);

        	if(GetLocalVarInt("RoomCount")==3)
        	{
        		SetEntityActive("AreaAlexanderVoice", true);
        	}
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // NOTE AND DIARY PICKUP
        ////////////////////////////////////////////

        //------------------------------------------

        void PickNote(string asEntity, string asType)
        {
        	PlayMusic("24_event_vision04.ogg", false, 0.5f, 0, 10, false);
        }

        void PickDiary(string asEntity, string asType)
        {
        	PlayMusic("22_paper_daniel01.ogg", false, 0.5f, 0, 10, false);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // PICK BUCKET OF TAR
        ////////////////////////////////////////////

        //------------------------------------------

        void PickBucketOfTar(string asEntity, string asType)
        {
        	SetGlobalVarInt("PickedBucketOfTar",1);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // MISC INTERACTIONS
        ////////////////////////////////////////////

        //------------------------------------------

        void InteractLargeGate(string asEntity)
        {
        	SetMessage("Ch02Level22", nameof(InteractLargeGate), 3.0f);
        	PlayGuiSound("close_gate.ogg", 0.7f);
        	AddTimer(asEntity, 1.0f, nameof(TimerLargeGate));
        }
        void TimerLargeGate(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractLargeGate), true);
        }
        //------------------------------------------

        void InteractMachineRoomDoor(string asEntity)
        {
        	//SetMessage("Ch02Level22", "InteractMachineRoomDoor", 0);
        	SetMessage("LevelDoors", "LockedForever", 0);
        }

        //------------------------------------------

        void UseHammerChipperOnMachineDoor(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level15", "DoorItemNoUse", 0);
        }

        //------------------------------------------

        //Player Reactions
        void PlayerReact(bool bDamage, float fTime)
        {
        	if(bDamage)
        		AddTimer("sanity", fTime, nameof(TimerPlayerReactions));
        	else
        		AddTimer("scare", fTime, nameof(TimerPlayerReactions));

        	AddTimer("breath", fTime+2, nameof(TimerPlayerReactions));
        	AddTimer("breathl", fTime+4, nameof(TimerPlayerReactions));

        }
        void TimerPlayerReactions(string asTimer)
        {
        	if(asTimer == "sanity"){
        		GiveSanityDamage(10.0f, true);
        		PlayGuiSound("react_scare", 0.9f);
        	}
        	if(asTimer == "scare"){
        		GiveSanityDamage(10.0f, false);
        		PlayGuiSound("react_scare", 0.9f);
        	}
        	if(asTimer == "breath")
        		PlayGuiSound("react_breath", 0.7f);

        	if(asTimer == "breathl")
        		PlayGuiSound("react_breath", 0.5f);
        }

        //-------------------------------------------------------------

        void InteractAreaRitualFurniture(string asEntity)
        {
        	//SetMessage("Ch02Level22", "InteractFurniture", 0);
        	StartEffectFlash(2,1, 2);
        	SetRadialBlurStartDist(0.2f);
        	FadeRadialBlurTo(0.09f, 0.015f);
        	FadePlayerFOVMulTo(0.4f, 0.7f);

        	PlaySoundAtEntity("RitualSound","insanity_imageflash01.snt", "Player", 0, false);

        	//AddTimer(asEntity, 1.0f, "TimerInteractBackOn");
        	AddTimer(asEntity, 2, nameof(TimerInteractFlashOver));
        }

        void InteractAreaRitualTable(string asEntity)
        {
        	//SetMessage("Ch02Level22", "InteractTable", 0);
        	StartEffectFlash(2,1, 2);
        	SetRadialBlurStartDist(0.2f);
        	FadeRadialBlurTo(0.09f, 0.015f);
        	FadePlayerFOVMulTo(0.4f, 0.7f);

        	PlaySoundAtEntity("RitualSound","insanity_imageflash01.snt", "Player", 0, false);

        	//AddTimer(asEntity, 1.0f, "TimerInteractBackOn");
        	AddTimer(asEntity, 2, nameof(TimerInteractFlashOver));
        }

        void TimerInteractFlashOver(string asTimer)
        {
        	FadeRadialBlurTo(0, 0.02f);
        	FadePlayerFOVMulTo(1, 3);

        	if(asTimer == "AreaRitualTable")
        		PlaySoundAtEntity("RitualSound","scare_baby_cry.snt", "Player", 0, false);

        	if(asTimer == "AreaRitualFurniture")
        		PlaySoundAtEntity("RitualSound","scare_male_terrified.snt", "Player", 0, false);


        	//SetEntityPlayerInteractCallback(asTimer, "Interact"+asTimer, true);
        }

        //-------------------------------------------------------------

        void TimerInteractBackOn(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, "Interact"+asTimer, true);

        	RemoveTimer("TunnelDamage");
        }

        void InteractAreaTunnelDamage(string asEntity)
        {
        	AddTimer(asEntity, 1.0f, nameof(TimerInteractBackOn));
        	TimerTunnelDamage("");

        	AddQuest("22BarrierTunnel", "22BarrierTunnel");
        }


        //A little CheckPoint
        void CPCallAltar(string asName, int alCount)
        {
        	StartPlayerLookAt("AreaRitualTable", 10.0f, 10.0f, "");

        	AddTimer("cp", 0.1f, nameof(TimerCPCall));
        	AddTimer("stop", 0.5f, nameof(TimerCPCall));

        	SetLocalVarInt("CPAltar", 1);
        }
        void TimerCPCall(string asTimer)
        {
        	if(asTimer == "cp"){
        		PlayGuiSound("00_cuts.ogg", 1.0f);
        		PlayerReact(false, 2);
        		PlayerReact(false, 6);
        		return;
        	}

        	GiveSanityDamage(10.0f, true);
        	StopPlayerLookAt();
        }


        //Only be able to activate grunt 2 if grunt 1 has been deactivated.
        void EntityCallGruntDisabled(string asEntity, string type)
        {
        	SetLocalVarInt("NoGrunt", 1);

        	AddDebugMessage("Enemy Disabled", false);
        }


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("Chancel");

        	////////////////////
        	//Init stuff
        	InitBarrierMachine();
        	InitBarrierTunnel();

        	////////////////////
        	// Set varaibles showing this level has been visited
        	SetGlobalVarInt("PlayerHasVisitedLevel22",1);

        	////////////////////
        	// Collide Callbacks
        	AddEntityCollideCallback("Player", "AreaFallDeath", nameof(CollideFallDeath), false, 1);
        	AddEntityCollideCallback("Player", "AreaTunnelDamage", nameof(CollideTunnelDamage), false, 0);

        	AddEntityCollideCallback("Player", "AreaGruntsStartHunt", nameof(CollideGruntsStartHunt), false, 1);
        	AddEntityCollideCallback("Player", "AreaAmbushStart", nameof(CollideAmbushStart), true, 1);
        	AddEntityCollideCallback("Player", "AreaAmbushInstantKill", nameof(CollideAmbushInstantKill), true, 1);
        	AddEntityCollideCallback("Player", "AreaAmbushInstantKill2", nameof(CollideAmbushInstantKill), true, 1);

        	AddEntityCollideCallback("Player", "AreaAmbushRumble", nameof(CollideAmbushRumble), false, 1);

        	AddEntityCollideCallback("Player", "FlashbackArea_1", nameof(CollideFlashbackArea), true, 1);

        	AddEntityCollideCallback("Player", "AreaAlexanderVoice", nameof(CollideAlexanderVoice), true, 1);
        	for(int i=1; i<=3; ++i)
        		AddEntityCollideCallback("Player", "AreaRoomCount_"+i, nameof(CollideRoomCount), true,1);

        	for(int i=1; i<=14; ++i)
        		AddEntityCollideCallback("Player", "AreaWindBreeze_"+i, nameof(CollideWindBreeze), false, 1);

        	AddEntityCollideCallback("servant_grunt*", "AreaGruntsStartHunt", nameof(CollideGruntsStartHunt), false, 1);

        	//Path nodes
        	AddEnemyPatrolNode("servant_grunt_4", "PathNodeArea_79", 10.0f, "IdleExtra1");
        	AddEnemyPatrolNode("servant_grunt_4", "PathNodeArea_6", 0, "");
        	AddEnemyPatrolNode("servant_grunt_4", "PathNodeArea_59", 0, "");
        	AddEnemyPatrolNode("servant_grunt_4", "PathNodeArea_195", 0, "");

        	AddEnemyPatrolNode("servant_grunt_5", "PathNodeArea_23", 0, "");
        	AddEnemyPatrolNode("servant_grunt_5", "PathNodeArea_6", 8, "IdleExtra2");
        	AddEnemyPatrolNode("servant_grunt_5", "PathNodeArea_23", 0, "");
        	AddEnemyPatrolNode("servant_grunt_5", "PathNodeArea_195", 0, "");

        	//INTERACT CALLS
        	//SetEntityPlayerInteractCallback("AreaRitualFurniture", "InteractAreaRitualFurniture", true);
        	//SetEntityPlayerInteractCallback("AreaRitualTable", "InteractAreaRitualTable", true);
        	SetEntityPlayerInteractCallback("AreaTunnelDamage", nameof(InteractAreaTunnelDamage), true);

        	////////////////////
        	// Use Item Callbacks
        	AddUseItemCallback("HammerChipperOnMachineDoor", "stone_hammer_chipper", "prison_section_static_1", nameof(UseHammerChipperOnMachineDoor), false);
        	AddUseItemCallback("StringOnPedestal", "string_pile_2", "AreaOrbEffect", nameof(UseStringOnPedestal), false);
        	AddUseItemCallback("StringOnPedestal", "string_pile_2", "orb_pedestal_1", nameof(UseStringOnPedestal), false);

        	AddUseItemCallback("TarOnPedestal","bucket_of_tar_1", "AreaOrbEffect", nameof(UseTarOnPedestal), true);
        	AddUseItemCallback("TarOnPedestal","bucket_of_tar_1", "orb_pedestal_1", nameof(UseTarOnPedestal), true);

        	for(int i=1; i<=6; ++i)
        	{
        		AddUseItemCallback("AddOrb"+i,"OrbPiece_"+i, "AreaOrbEffect", nameof(UseOrbPieceOnPedestal),false);
        		AddUseItemCallback("AddOrb"+i,"OrbPiece_"+i, "orb_pedestal_1", nameof(UseOrbPieceOnPedestal),false);

        		//Be able to use on orb pieces aswell
        		for(int j=1; j<=5; ++j)
        			AddUseItemCallback("AddOrb"+i,"OrbPiece_"+i, "orb_assembly_"+j, nameof(UseOrbPieceOnPedestal),false);
        	}

        	CheckPoint("checkpoint_default", "PlayerStartArea_6", nameof(CPCallAltar), "", "");

        	///////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("lantern", "lantern.ent");
        		GiveItemFromFile("bucket_of_tar_1", "bucket_of_tar.ent");

        		for(int i=1; i<=6; ++i)
        			GiveItemFromFile("OrbPiece_"+i, "orbpiece_crimson.ent");

        		GiveItem("stone_hammer_chipper", "Puzzle", "stone_hammer_chipper", "stone_hammer_chipper.tga", 0);
        		GiveItemFromFile("string_pile_2", "string_pile.ent");

        		//Test Ambush. Give player all orbs.
        		//SetGlobalVarInt("ChoirOrbCount",3);
        		//SetGlobalVarInt("TranseptOrbCount",3);
        		//SetLocalVarInt("SkipAmbushTeleport",1);
        	}
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("attack_claw.snt"); PreloadSound("22_start_daniel.snt"); PreloadSound("general_wind_whirl.snt");
        	PreloadSound("react_scare"); PreloadSound("react_breath"); PreloadSound("22_orb_forge.snt"); PreloadSound("11_fall");
        	PreloadSound("player_bodyfall"); PreloadSound("player_falldamage_max"); PreloadSound("21_alex_low_freq_rumble.snt "); PreloadSound("impact_wood_heavy_high.snt");
        	PreloadSound("27_wind.snt");

        	PlayMusic("22_amb.ogg", true, 1.0f, 4.0f, 0, true);

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	FadeIn(1.0f);
        	SetPlayerActive(true);
        }
    }
}
