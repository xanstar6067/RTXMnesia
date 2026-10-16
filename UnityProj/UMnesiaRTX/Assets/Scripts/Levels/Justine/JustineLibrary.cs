using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Justine
{
    [HplMapScript("maps/ptest/02_library.map")]
    public class JustineLibrary : HplScript
    {
        //////////////////////////////////////////////////////////
        // LIGHT BOX
        float gfBoxSpotR=0.5f;
        float gfBoxSpotG=0.6f;
        float gfBoxSpotB=0.9f;
        float gfBoxSpotA=0.5f; 

        //------------------------------------------------------------------------------------------

        string GetLightBoxSpotLight()
        {
        	string sCurrentStencil = GetLocalVarString("CurrentBoxStencil");
        	if(sCurrentStencil == "") return "SpotLight_no_stencil";
        	
        	int lStencilNum = GetLocalVarInt("StencilNum_"+sCurrentStencil);
        	return "SpotLight_stencil0"+lStencilNum;
        }

        //------------------------------------------------------------------------------------------

        void OnLightBoxLit(string asEntity, string asType)
        {
        	float fFadeTime = 1;
        	
        	//Want a slightly delayed for this setting else the callback will happen any way.
        	AddTimer("BoxLit", 0.3f, nameof(TimerBoxLit));
        	
        	for(int i=1; i<=5; ++i)
        	{
        		CreateParticleSystemAtEntity("dust_parts"+i,"ps_light_dust_large.ps", "AreaDustParticles_"+i, true);
        	}

        	FadeLightTo("PointLight_2", 0.3f,0.3f, 1, 0.4f, -1, fFadeTime); //Light in box
        	FadeLightTo("PointLight_9",0.3f,0.3f, 1, 0.4f, -1, fFadeTime); //Light outside and in front of box.
        	FadeLightTo("SpotLight_2",0.3f,0.3f, 1, 0.4f, -1, fFadeTime);
        	
        	FadeLightTo(GetLightBoxSpotLight(),gfBoxSpotR,gfBoxSpotG,gfBoxSpotB,gfBoxSpotA, -1, fFadeTime);
        }

        void TimerBoxLit(string asTimer)
        {
        	SetLocalVarInt("BoxLightLit", 1);
        }

        //------------------------------------------------------------------------------------------

        void FadeOutAllBoxSpotLights(float afFadeTime)
        {
        	FadeLightTo("SpotLight_no_stencil",0,0,0,0, -1, afFadeTime);
        	for(int i=1; i<=4; ++i)
        		FadeLightTo("SpotLight_stencil0"+i,0,0,0,0, -1, afFadeTime);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PlACE STENCILS IN BOX
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void UseStencilOnBox(string asItem, string asEntity)
        {
        	////////////////////////////////
        	// Remove the item form inventory
        	RemoveItem(asItem);
        	
        	////////////////////////////////
        	// Check if there is an existing item
        	bool bSwitchedStencils = false;
        	if(GetLocalVarString("CurrentBoxStencil") != "")
        	{
        		string sCurrentStencil = GetLocalVarString("CurrentBoxStencil");
        		int lStencilNum = GetLocalVarInt("StencilNum_"+sCurrentStencil);
        		
        		//Give Item
        		GiveItemFromFile(sCurrentStencil, "stencil0"+lStencilNum+".ent");
        		
        		bSwitchedStencils = true;
        	}
        	
        	////////////////////////////////
        	// Reset all static objects
        	for(int i=1; i<=4; ++i)
        		SetEntityActive("stencil_box_static_"+i, false);

        	////////////////////////////////
        	// Make the correct stencil visible.
        	int lNum = GetLocalVarInt("StencilNum_"+asItem);
        	SetEntityActive("stencil_box_static_"+lNum, true);
        	
        	SetLocalVarString("CurrentBoxStencil", asItem);
        	
        	AddDebugMessage("Added box stencil '"+asItem+"'", false);
        	
        	//////////////////////////////////
        	// Set the callback to interact with box
        	SetEntityPlayerInteractCallback("light_box_1", nameof(PlayerInteractLightBox), false);
        	SetEntityCustomFocusCrossHair("light_box_1", "Pick");
        	
        	//////////////////////////////////
        	//Set up correct spot light
        	if(GetLocalVarInt("BoxLightLit")==1)
        	{
        		float fFadeTime = 1;
        	
        		FadeOutAllBoxSpotLights(fFadeTime);	
        		FadeLightTo(GetLightBoxSpotLight(),gfBoxSpotR,gfBoxSpotG,gfBoxSpotB,gfBoxSpotA, -1, fFadeTime);
        	}
        	
        	////////////////////////////////
        	// Play sound
        	PlayStencilSound(false, asEntity);
        }

        void PlayStencilSound(bool bSwitchedStencils, string asEntity)
        {
        	if(bSwitchedStencils)
        	{
        		PlaySoundAtEntity("sentcil1", "pick_generic", asEntity, 0.0f, false);
        	}
        	
        	else 
        	{
        		PlaySoundAtEntity("sentcil1", "19_attach_needle", asEntity, 0.1f, false);
        		PlaySoundAtEntity("sentcil1", "pick_paper", asEntity, 0.25f, false);
        	}
        }

        //------------------------------------------------------------------------------------------

        void PlayerInteractLightBox(string asEntity)
        {
        	//If not lit yet, do nothing
        	if(GetLocalVarInt("BoxLightLit")==0) return;

        	//Get current stencil
        	string sCurrentStencil = GetLocalVarString("CurrentBoxStencil");
        	int lStencilNum = GetLocalVarInt("StencilNum_"+sCurrentStencil);
        	
        	//Fade out all lights
        	FadeOutAllBoxSpotLights(0.5f);
        	
        	//Give Item
        	GiveItemFromFile(sCurrentStencil, "stencil0"+lStencilNum+".ent");
        	
        	SetMessage("L02Library", "PickupStencil"+lStencilNum, 0);
        	
        	AddDebugMessage("Took box stencil '"+sCurrentStencil+"'", false);
        	
        	SetLocalVarString("CurrentBoxStencil", "");
        	
        	//Remove callback
        	SetEntityPlayerInteractCallback("light_box_1", "", false);
        	SetEntityCustomFocusCrossHair("light_box_1", "Default");
        	
        	//Remove object from box.
        	for(int i=1; i<=4; ++i)
        		SetEntityActive("stencil_box_static_"+i, false);

        	PlayStencilSound(true, asEntity);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PlACE STENCILS IN SLOT MACHINE
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void UseStencilOnSlot(string asItem, string asEntity)
        {
        	int lStencilNum = GetLocalVarInt("StencilNum_"+asItem);
        	int lSlotNum = GetLocalVarInt("SlotNum_"+asEntity);
        	
        	//////////////////////////////////
        	//Remove any existing stencil first
        	int lPrevStencil = GetLocalVarInt("SlotVal"+lSlotNum);
        	if(lPrevStencil>0)
        	{
        		GiveItemFromFile( "stencil0"+lPrevStencil, "stencil0"+lPrevStencil+".ent");	
        	}

        	RemoveItem(asItem);	
        	SetLocalVarInt("SlotVal"+lSlotNum, lStencilNum);
        	
        	//////////////////////////////////
        	//Set the graphics active
        	SetEntityActive("stencil_slot"+lSlotNum+"_static_*", false);
        	SetEntityActive("stencil_slot"+lSlotNum+"_static_"+lStencilNum, true);
        	
        	AddDebugMessage("Added slot"+lSlotNum+" stencil '"+asItem+"'", false);
        	
        	//////////////////////////////////
        	//Set the callback
        	SetEntityPlayerInteractCallback(asEntity, nameof(PlayerInteractSlot), false);
        	SetEntityCustomFocusCrossHair(asEntity, "Pick");
        	
        	PlayStencilSound(false, asEntity);
        }

        //------------------------------------------------------------------------------------------

        void PlayerInteractSlot(string asEntity)
        {
        	int lSlotNum = GetLocalVarInt("SlotNum_"+asEntity);
        	
        	int lPrevStencil = GetLocalVarInt("SlotVal"+lSlotNum);
        	GiveItemFromFile( "stencil0"+lPrevStencil, "stencil0"+lPrevStencil+".ent");	

        	SetLocalVarInt("SlotVal"+lSlotNum, 0);
        	SetEntityActive("stencil_slot"+lSlotNum+"_static_*", false);
        	
        	SetEntityPlayerInteractCallback(asEntity, "", false);
        	SetEntityCustomFocusCrossHair(asEntity, "Default");
        	
        	SetMessage("L02Library", "PickupStencil"+lPrevStencil, 0);
        	
        	AddDebugMessage("removed slot"+lSlotNum+" stencil 'stencil0"+lPrevStencil+"'", false);
        	
        	PlayStencilSound(true, asEntity);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // USE SLOT MACHINE
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void SlotLeverMove(string asEntity, int alState)
        {
        	if(alState != 1) return;
        	
        	int lUpStencil = GetLocalVarInt("SlotVal1");
        	int lDownStencil = GetLocalVarInt("SlotVal2");
        	
        	//IMPORTANT NOTE: The following solutions are just temp!
        	
        	//////////////////////////////
        	// Save Prisoner solution
        	if(lUpStencil == 4 && lDownStencil==3)
        	{
        		SavePrisoner();
        	}
        	//////////////////////////////
        	// Kill Prisoner solution 
        	else if(lUpStencil == 2 || lDownStencil==2)
        	{
        		KillPrisoner();
        	}
        	//////////////////////////////
        	// Torture  (or death)
        	else
        	{
        		AddLocalVarInt("TortureCount",1);
        		
        		if(GetLocalVarInt("TortureCount")==4)		KillPrisoner();
        		else									TorturePrisoner();	
        	}
        	
        	//////////////////////////////
        	// Set lever as stuck
        	SetPlayerActive(false);
        	SetPlayerActive(true);
        	SetLeverStuckState(asEntity, alState, true);
        	
        	/////////////
        	// General effects
        	PlaySoundAtEntity("sticks", "L02_lever.snt", asEntity, 0.5f, false);
        	PlaySoundAtEntity("sticks2", "gameplay_open_chest.snt", "AreaLeverEffect", 0.5f, false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PUZZLE COMPLETE EVENTS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void SavePrisoner()
        {
        	AddDebugMessage("Save prisoner!", false);
        	
        	PlayPrisonerSound("free", true);
        	RemoveTimer("prisoneridle");	//No more random idle sounds
        	SetLocalVarInt("PrisonerAlive", 0);
        	
        	PlayMusic("15_puzzle_hole", false, 0.7f, 2.0f, 10, true);
        	
        	StartCharacterSpeak(GetLocalVarString("character")+"_saved", 1, false, true, true);
        	
        	StopSound("Sound_4", 0.0f);
        	StopSound("Sound_5", 0.0f);
        	
        	SetEntityActive("AreaCharStopTalk", false);
        	SetEntityActive("AreaCharStartTalk", false);
        	
        	AddKeyPart(unchecked((int)0x8912AA01));
        	AddKeyPart(unchecked((int)0xd1e55326));
        	
        	OpenSecretDoor();
        }

        //------------------------------------------------------------------------------------------

        void KillPrisoner()
        {
        	AddDebugMessage("Kill prisoner!", false);
        	
        	PlayPrisonerSound("strain", true);
        	RemoveTimer("prisoneridle");	//No more random idle sounds
        	SetLocalVarInt("PrisonerAlive", 0);
        	
        	PlayMusic("15_event_prisoner", false, 0.7f, 2.0f, 10, true);
        	
        	SetGlobalVarInt("HectorIsDead", 1);
        	
        	AddKeyPart(0x3F1cAA38);
        	AddKeyPart(unchecked((int)0xe4daa6f8));
        	
        	StartCharacterSpeak(GetLocalVarString("character")+"_dying", 1, false, true, true);
        	
        	AddTimer("prisoner_death", 6.0f, nameof(TimerPrisonerDeath));
        	AddTimer("prisoner_death2", 3.0f, nameof(TimerPrisonerDeath));
        	
        	StopSound("Sound_4", 0.0f);
        	StopSound("Sound_5", 0.0f);
        	
        	SetEntityActive("AreaCharStopTalk", false);
        	SetEntityActive("AreaCharStartTalk", false);
        	
        	OpenSecretDoor();
        }

        void TimerPrisonerDeath(string asTimer)
        {
        	if(asTimer == "prisoner_death2")
        	{
        		PlayPrisonerSound("kill", true);
        		return;
        	}
        	
        	StartCharacterSpeak(GetLocalVarString("character")+"_death", 1, false, true, true);
        }

        //------------------------------------------------------------------------------------------

        void TorturePrisoner()
        {
        	AddDebugMessage("Torture prisoner!", false);

        	PlayPrisonerSound("strain", true);
        	
        	if(GetCharacterTopicNotSpoken(GetLocalVarString("character")+"_terrorized"))
        	{
        		StartCharacterSpeak(GetLocalVarString("character")+"_terrorized", 1, false, true, true);
        		
        		AddTimer("UnstuckSlotLever", 3, nameof(TimerUnstuckSlotLever));
        		
        		return;
        	}
        		
        	if(RandInt(0,2) == 0)
        		StartCharacterSpeak(GetLocalVarString("character")+"_scared", 3, true, false, true);
        	else
        		StartCharacterSpeak(GetLocalVarString("character")+"_sobbing", 3, true, false, true);
        	
        	AddTimer("UnstuckSlotLever", 3, nameof(TimerUnstuckSlotLever));
        }

        void TimerUnstuckSlotLever(string asTimer)
        {
        	SetLeverStuckState("slot_machine_1", 0, true);
        }

        //------------------------------------------------------------------------------------------

        //-----------
        void PlayPrisonerSound(string sNoise, bool bStopIdle)
        {
        	AddDebugMessage("Playing chain noise: "+sNoise, false);
        	
        	//Remove an potential random idle, and set a new one to begin after this current sound has played
        	RemoveTimer("prisoneridle");
        	if(GetLocalVarInt("PrisonerAlive") == 1)
        		AddTimer("prisoneridle", RandFloat(5.0f,15.0f), nameof(TimerPrisonerIdle));
        	
        	//Stop an idle sound so it does nto conflict with torture, kill or free
        	if(bStopIdle)
        	{
        		StopSound("idle", 1.0f);
        	}
        	
        	PlaySoundAtEntity(sNoise, "L02_prisoner_"+sNoise, "AreaCharTalk", 0.0f, false);
        }
        //------------

        //--------------------
        void TimerPrisonerIdle(string asTimer)
        {
        	PlayPrisonerSound("idle", false);
        	
        	if(!GetEffectVoiceActive())
        		StartCharacterSpeak(GetLocalVarString("character")+"_idle", 5, true, false, false);
        }
        //-----------------------


        //////////////////////////////////////////////////////////
        // OPEN DOOR TO CELLAR
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void OpenSecretDoor()
        {
        	SetMoveObjectState("shelf_secret_door", 0.43f); 
        	
        	AddTimer("start", 2.0f, nameof(TimerStartShelfSound));
        	AddTimer("stopshelfsound", 6.0f, nameof(TimerStopShelfSound));
        }

        void TimerStartShelfSound(string asTimer)
        {
        	PlaySoundAtEntity("clank", "L02_start", "AreaShelfRotatePoint", 0.0f, false);
        	PlaySoundAtEntity("moveshelf", "L02_scrape", "AreaShelfRotatePoint", 2.0f, false);
        	
        	FadeLightTo("PointLight_44", 0, 0, 0, 0, 0, 4);
        	
        	StartScreenShake(0.005f, 4.0f, 2.0f, 2.0f);	
        }

        void TimerStopShelfSound(string asTimer)
        {
        	StopSound("moveshelf", 0.5f);
        	PlaySoundAtEntity("clank2", "L02_stop", "AreaShelfRotatePoint", 0.0f, false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // ENEMY EVENTS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void CollideLightGoOut(string asParent, string asChild, int alState)
        {
        	CreateParticleSystemAtEntity("wind","ps_dust_whirl","AreaWindEffect", false);
        	
        	PlaySoundAtEntity("winds", "general_wind_whirl", "AreaWindEffect", 5.0f, false);
        	PlaySoundAtEntity("loon", "scare_loon_single", "AreaWindEffect", 1.0f, false);
        	
        	PlayMusic("20_event_darkness", false, 0.8f, 2.0f, 10, true);
        	PlayMusic("12_amb", true, 0.6f, 2.0f, 0, true);
        	
        	AddTimer("delaylamp", 1.0f, nameof(TimerDelayLampsOut));
        }

        void TimerDelayLampsOut(string asTimer)
        {
        	PlaySoundAtEntity("wind2s", "general_wind_whirl", "AreaWindEffect", 5.0f, false);
        	
        	SetLampLit("torch_static01_6", false, true);
        	SetLampLit("torch_static01_4", false, true);
        	SetLampLit("torch_static01_8", false, true);
        }

        //------------------------------------------------------------------------------------------

        void CollideStartEnemy(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Starting enemy", false);
        	ClearEnemyPatrolNodes("suitor_basile_1");
        	
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_8", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_4", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_53", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_65", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_70", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_52", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_60", RandFloat(2,4), "");

        	SetEntityActive("suitor_basile_1", true);
        	
        	SetSwingDoorLocked("cellar_wood01_1", true, false);
        	
        	PlaySoundAtEntity("bang", "scare_slam_door", "cellar_wood01_1", 0.0f, false);
        	StartScreenShake(0.02f, 1.0f, 0.5f, 1.0f);
        }

        //------------------------------------------------------------------------------------------

        void CollideEnterNextArea(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Entering next area", false);
        	ClearEnemyPatrolNodes("suitor_basile_1");
        	
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_16", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_32", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_26", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_20", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_11", RandFloat(2,4), "");
        	AddEnemyPatrolNode("suitor_basile_1", "PathNodeArea_43", RandFloat(2,4), "");
        }

        //------------------------------------------------------------------------------------------

        //Show enemy player as player clears the rubble for exit
        void CollideAreaEnterExit(string asParent, string asChild, int alState)
        {
        	ShowEnemyPlayerPosition("suitor_basile_1");
        	
        	AddDebugMessage("Cutie suitor, get him!", false);
        }

        //------------------------------------------------------------------------------------------

        //Break all boxes at the exit as the enemy is close.
        void CollideEnemyAreaEnterExit(string asParent, string asChild, int alState)
        {
        	StartScreenShake(0.1f, 0.5f, 0.1f, 0.4f);
        	SetPropHealth("boxboom_*", 0);
        	
        	AddDebugMessage("Suitor big, suitor don't like boxes, suitor eat player as a potato!", false);
        }

        //////////////////////////////////////////////////////////
        // DOOR POUNDING EVENT
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void PickStencil(string asEntity, string asType)
        {
        	AddLocalVarInt("StencilCount", 1);
        	
        	AddDebugMessage("Stencil count: "+GetLocalVarInt("StencilCount"), false);
        	
        	if(GetLocalVarInt("StencilCount")==2)
        		AddEntityCollideCallback("Player", "AreaPoundDoor", nameof(CollidePoundDoor), true, 1);
        }

        //------------------------------------------------------------------------------------------

        void CollidePoundDoor(string asParent, string asChild, int alState)
        {
        	AddTimer("DoorPoundLoop", 1, nameof(TimerDoorPoundLoop));
        }


        //No more door pounding.
        void CollideAreaStopPoundDoor(string asParent, string asChild, int alState)
        {
        	RemoveTimer("DoorPoundLoop");
        	
        	AddDebugMessage("No More Quarter Pounder. Veggie of course.", false);
        }

        void TimerDoorPoundLoop(string asTimer)
        {
        	AddLocalVarInt("PoundDoorCount", 1);
        	if(GetLocalVarInt("PoundDoorCount") == 5) return;
        	
        	AddPropImpulse("mansion_1", -2,0,0,"World");
        	PlaySoundAtEntity("pound", "hit_wood", "AreaPoundDoorEffect", 0, false);
        	PlaySoundAtEntity("enemy", "L02_attack", "AreaPoundDoorEffect", RandFloat(0.0f, 0.5f), false);
        	CreateParticleSystemAtEntity("pound_dust","ps_hit_wood","AreaPoundDoorEffect", false);
        	
        	AddTimer("DoorPoundLoop", RandFloat(14, 50), nameof(TimerDoorPoundLoop));
        	
        	AddDebugMessage("Pound pound", false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR PITCH DOWN SOUNDS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void CollideAreaPitch(string asParent, string asChild, int alState)
        {
        	//Slow down player and pitch when walking towards door
        	if(alState == 1)
        	{
        		AddLocalVarFloat("PlayerPitch", -0.045f);
        		AddLocalVarFloat("PlayerRadial", 0.001f);
        		
        		if(asChild == "AreaPitch_1")
        		{
        			SetRadialBlurStartDist(0.4f);
        		}
        		
        		if(asChild == "AreaPitch_2")
        			PlaySoundAtEntity("", "player_female_breath", "Player", 0.1f, false);
        		if(asChild == "AreaPitch_6")
        			PlaySoundAtEntity("", "player_female_breath", "Player", 0.2f, false);
        		if(asChild == "AreaPitch_10")
        			PlaySoundAtEntity("", "player_female_breath", "Player", 0.3f, false);
        		if(asChild == "AreaPitch_14")
        			PlaySoundAtEntity("", "player_female_breath", "Player", 0.4f, false);
        		if(asChild == "AreaPitch_18")
        			PlaySoundAtEntity("", "player_female_breath", "Player", 0.5f, false);
        	}

        	//Speed up player and pitch when walking away from door
        	else
        	{
        		AddLocalVarFloat("PlayerPitch", 0.045f);
        		
        		//Always reset on leaving last area to make sure no problems occure.
        		if(asChild == "AreaPitch_1")
        		{
        			SetLocalVarFloat("PlayerPitch", 0.96f);
        			SetLocalVarFloat("PlayerRadial", 0);
        			
        			//Disable event
        			for(int i=1;i<=20;i++) 
        				SetEntityActive("AreaPitch_"+i, false);
        				
        			StopSound("Sound_7", 4.0f);
        		}
        	}
        	
        	float fPlayerPitch = GetLocalVarFloat("PlayerPitch");
        	float fPlayerRadial = GetLocalVarFloat("PlayerRadial");
        	
        	FadeGlobalSoundSpeed(fPlayerPitch+0.04f, 5);
        	SetPlayerMoveSpeedMul(fPlayerPitch);
        	FadeRadialBlurTo(fPlayerRadial, 10);

        	if(alState == 1)
        		StartScreenShake(fPlayerRadial*0.3f, fPlayerRadial*16, fPlayerRadial*2, fPlayerRadial*4);
        	else
        		StartScreenShake(fPlayerRadial*0.5f, fPlayerRadial*12, fPlayerRadial*2, fPlayerRadial*6);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PHONOGRAPH MESSAGES 
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        // The phonograph by the entrance
        void StateChangePhono01(string asEntity, int alState)
        {
        	PlayPhono(2, 1, 2, asEntity);
        }

        // The phonograph in the stairs down
        void StateChangePhono02(string asEntity, int alState)
        {
        	PlayPhono(2, 2, 3, asEntity);
        }

        // Play the phonograph messages on interact with phonograph
        void PlayPhono(int iLevel, int iSet, int iPart, string sEntity)
        {
        	SetEntityInteractionDisabled(sEntity, true);	//No more interact with phono as it does not do anything.
        	
        	SetLocalVarInt(nameof(PlayPhono), 1);
        	
        	PlaySoundAtEntity("phono_scrape", "ptest_phono_loop", sEntity, 1.0f, false);
        	
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

        void CollideAreaStopPhono(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt(nameof(PlayPhono)) != 1) return;
        	
        	StopAllEffectVoices(2.0f);
        	VoiceOverPhono();
        }

        /////////////////////////////////////////////////
        // CHARACTER INTERACTION

        // -------------- START/STOP COLLIDE AREAS --------------------------- //

        //Character start talking
        void CollideCharacterStartTalk(string asParent, string asChild, int alState)
        {
        	if(GetEffectVoiceActive()) return;
        	
        	//Only run function if outside talk range
        	if(GetLocalVarInt("PlayerInCharTalkRange")==1) return; 
        	SetLocalVarInt("PlayerInCharTalkRange",1);
        	
        	AddTimer("prisoneridle", RandFloat(4,8), nameof(TimerPrisonerIdle));	//Random idle
        	
        	if(GetCharacterTopicNotSpoken(GetLocalVarString("character")+"_discover"))
        	{
        		SetLocalVarInt("PrisonerAlive", 1);
        		StartCharacterSpeak(GetLocalVarString("character")+"_discover", 1, false, true, true);
        	}
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
        	
        	RemoveTimer("prisoneridle");	//No more random idle sounds
        	
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
        		
        		//Make sure that each variation has been used before playing a varation again
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

        void EntityCallPickNote(string asEntity, string type)
        {
        	SetGlobalVarInt("HasAptNote", 1);
        	
        	AddLocalVarInt("NumOfNotesPicked", 1);
        	
        	AddDebugMessage("PickedAptNote", false);
        }

        void EntityCallPickNoteCount(string asEntity, string type)
        {
        	AddLocalVarInt("NumOfNotesPicked", 1);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // MAIN CALLBACKS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        public override void OnStart()
        {
        	SetupLoadScreen("LoadingText","Load03_",3,"ptest_gameloading_02.jpg");
        	
        	////////////////////////////
        	// SET LEVEL NAME
        	SetMapDisplayNameEntry("L02Library");
        	
        	GiveSanityBoost();
        	
        	PlayMusic("21_amb", true, 0.6f, 4.0f, 0, true);
        	
        	SetPlayerPermaDeathSound("basile_death.snt");
        	
        	////////////////////////////
        	// Variables
        	for(int i=1; i<=4; i++) SetLocalVarInt("StencilNum_stencil0"+i, i);
        	for(int i=1; i<=2; i++) SetLocalVarInt("SlotNum_AreaCardSlot_0"+i, i);
        	
        	////////////////////////////
        	// Collide Callbacks
        	AddEntityCollideCallback("Player", "AreaStopPoundDoor", nameof(CollideAreaStopPoundDoor), true, 1);
        	
        	AddEntityCollideCallback("Player", "AreaStopPhono_1", nameof(CollideAreaStopPhono), false, -1);
        	AddEntityCollideCallback("Player", "AreaStopPhono_2", nameof(CollideAreaStopPhono), false, -1);
        	
        	AddEntityCollideCallback("Player", "AreaEnterExit", nameof(CollideAreaEnterExit), true, 1);
        	//AddEntityCollideCallback("suitor_basile_1", "AreaEnterExit", "CollideEnemyAreaEnterExit", true, 1);
        	AddEntityCollideCallback("Player", "AreaLightGoOut", nameof(CollideLightGoOut), true, 1);
        	AddEntityCollideCallback("Player", "AreaStartEnemy", nameof(CollideStartEnemy), true, 1);
        	AddEntityCollideCallback("Player", "AreaEnterNextArea", nameof(CollideEnterNextArea), true, 1);
        	
        	for(int i=1;i<=20;i++)
        		AddEntityCollideCallback("Player", "AreaPitch_"+i, nameof(CollideAreaPitch), false, 0);
        		
        	AddEntityCollideCallback("Player", "AreaCharStartTalk", nameof(CollideCharacterStartTalk), false, 1);	
        	AddEntityCollideCallback("Player", "AreaCharStopTalk", nameof(CollideCharacterStopTalk), false, -1);		
        		
        	////////////////////////////
        	// Item Callbacks
        	for(int i=0; i<=4; ++i)
        	{
        		AddUseItemCallback("stencil0"+i+"onbox","stencil0"+i, "light_box_1",nameof(UseStencilOnBox), false); 
        		
        		//To be able to use stencils on stencils, as there is a risk of doing that 
        		//when you use stuff on the box after using the first stencil.
        		for(int j=0; j<=4; ++j)
        			AddUseItemCallback("stencil0"+i+"onstensil0"+j,"stencil0"+i, "stencil_box_static_"+j,nameof(UseStencilOnBox), false); 
        	}
        	
        	for(int i=0; i<=4; ++i)
        	for(int j=1; j<=2; ++j)
        		AddUseItemCallback("stencil0"+i+"onslot","stencil0"+i, "AreaCardSlot_0"+j, nameof(UseStencilOnSlot), false); 
        	
        	//////////////////////
        	// VARIABLES
        	SetGlobalVarInt("HectorIsDead", 0);	//Is set to 1 in KillPrisoner();
        	SetLocalVarString("character", "hector");	//Used to play voices for prisoner character in level
        	SetLocalVarFloat("PlayerPitch", 1);
        	
        	///////////////////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		for(int i=1; i<=4; i++)
        			GiveItemFromFile("stencil0"+i, "stencil0"+i+".ent");
        	
        		for(int i=0;i<5;i++){ 
        			GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");
        		}
        	}

        	PreloadSound("pick_generic"); PreloadSound("pick_paper"); PreloadSound("L02_stop"); PreloadSound("gameplay_open_chest");
        	PreloadSound("19_attach_needle"); PreloadSound("L02_lever"); PreloadSound("L02_scrape"); PreloadSound("L02_start");
        	PreloadSound("general_wind_whirl"); PreloadSound("scare_loon_single"); PreloadSound("scare_slam_door"); PreloadSound("L02_attack");
        	PreloadSound("L02_prisoner_idle"); PreloadSound("hit_wood"); PreloadSound("player_female_breath"); PreloadSound("ptest_phono_loop");
        	PreloadSound("L02_prisoner_free"); PreloadSound("L02_prisoner_kill"); PreloadSound("L02_prisoner_strain");
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	if(GetLocalVarInt("NumOfNotesPicked")==4)
        	{
        		AddKeyPart(unchecked((int)0x8FF2AA11));
        		AddKeyPart(0x51da1618);
        		AddDebugMessage("Picked up all notes!", false);
        	}
        }
    }
}
