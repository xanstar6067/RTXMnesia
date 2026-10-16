using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch03
{
    /// <summary>Port of maps/main/ch03/28_inner_sanctum.hps; journal: docs/ScriptPort/ch03/28_inner_sanctum.md.</summary>
    [HplMapScript("maps/main/ch03/28_inner_sanctum.map")]
    public class InnerSanctum : HplScript
    {
        //svn tag test comment

        //////////////////////////////////////////////////////////
        // ROAR HELPER
        //////////////////////////////////////////////////////////

        //--------------------------------------------------------

        void TimerDistantHowl(string asTimer)
        {
        	PlaySoundAtEntity("howl","guardian_distant1", "Player", 0, false);

        	AddTimer("StopRadialBlur", 4.5f, nameof(TimerStopRadialBlur));
        	if(asTimer=="AddScareReactions")
        	{
        		AddTimer("react_scare", 0.5f, nameof(TimerScareReaction));
        		AddTimer("react_breath", 2.5f, nameof(TimerScareReaction));
        	}

        	StartScreenShake(0.01f, 0.0f, 2.0f, 4.0f);

        	SetRadialBlurStartDist(0.2f);
        	FadeRadialBlurTo(0.09f, 0.015f);
        }

        void TimerStopRadialBlur(string asTimer)
        {
        	AddDebugMessage("Stopping howl blur.", false);
        	FadeRadialBlurTo(0, 0.02f);
        }

        void TimerScareReaction(string asTimer)
        {
        	PlayGuiSound(asTimer, 0.7f);

        	if(asTimer == "react_scare")
        		GiveSanityDamage(15.0f, true);
        }

        //--------------------------------------------------------

        //////////////////////////////////////////////////////////
        // SAFETY DOOR
        //////////////////////////////////////////////////////////

        //--------------------------------------------------------

        // Player interacts with safety door, add quest
        void PlayerInteractDoor(string asEntity)
        {
        	//Show message
        	SetMessage("Ch03Level28", "CantOpenDoor", 0);

        	//Locked sound
        	PlayGuiSound("17_attach_crank", 0.5f);

        	SetMoveObjectState(asEntity, 0.05f);

        	AddTimer(asEntity, 0.4f, nameof(TimerCloseGate));
        	AddTimer("sound", 1.0f, nameof(TimerCloseGate));

        	if(asEntity !=  "safety_large_vert_1") return;

        	//Wait 4 seconds and then give a quest.
        	if(QuestIsAdded("20SafetyDoorClosed")==false)
        		AddTimer("SafetyDoorQuest", 1.0f, nameof(TimerSafetyDoorQuest));
        }

        void TimerCloseGate(string asTimer)
        {
        	if(asTimer == "sound"){
        		PlayGuiSound("close_gate.ogg", 0.5f);
        	} else{
        		SetMoveObjectState(asTimer, 0.00f);

        		SetEntityPlayerInteractCallback(asTimer, nameof(PlayerInteractDoor), true);
        	}
        }

        void TimerSafetyDoorQuest(string asTimer)
        {
        	AddQuest("28DoorShut", "28DoorShut");
        }

        // Remove quest if the door is collapsed and gotten in the checkpoint area - DISABLED
        void CollideCheckCollapsedDoor(string asEntity, string asChild, int alState)
        {
        	if(GetLocalVarInt("DoorCollapsed")==0)
        		return;
        	CompleteQuest("28DoorShut", "28DoorShut");
        }

        //--------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PICK NOTE EVENT
        //////////////////////////////////////////////////////////

        //--------------------------------------------------------

        // Player picks diary page, start guardian event
        void PickDiary(string asItem, int alEntryIdx)
        {
        	AddDebugMessage("Picked diary page", false);
        	PlayMusic("28_paper_daniel01.ogg", false, 0.4f, 0.5f, 10, false);

        	//DISABLED HERE, AFTER FIRST ALTAR INSTEAD AND NOT THE MAIN LARGE DOOR, THE SMALL ENTRANCE INSTEAD
        	//AddTimer("TimerGuardianRoar", 2.0f, "TimerGuardianRoar");
        }

        //--------------------------------------------------------

        // Activate slime packs in passage to east shrine
        void TimerPreSlimeShow(string asTimer)
        {
        	AddDebugMessage("Making "+asTimer+" appear!", false);
        	SetPropActiveAndFade(asTimer + "*", true, 1);

        	PlaySoundAtEntity("slimeappear", "slime_create.snt", "Player", 0.5f, false);
        }

        // Activate slime at central room, start roaring sound and screen shake
        void TimerGuardianRoar(string asTimer)
        {
        	// Make slime appear
        	for(int i=0;i<=3;++i)
        	{
        		AddTimer("pre"+(3-i), 0.25f*i, nameof(TimerPreSlimeShow));
        	}

        	SetPropActiveAndFade("slime*", true, 1);

        	// Set up pinkish light
        	FadeLightTo("PointLight_11_slime", -1, -1, -1, -1, 9, 1);
        	FadeLightTo("PointLight_7", -1, -1, -1, -1, 0, 1);

        	// Set up door break
        	//AddTimer("TimerGuardianDoorBreak", 3, "TimerGuardianDoorBreak");

        	StartScreenShake(0.1f, 0.5f, 0.5f, 2.0f);
        	PlayGuiSound("guardian_idle6.ogg", 1.0f);

        	//Some more effects for the slime appearing
        	PlaySoundAtEntity("bang1", "25_guardian_slime_appear.snt", "Player", 0.0f, false);
        	PlaySoundAtEntity("slime1", "slime_loop.snt", "AreaSlimeSound_1", 1.0f, true);
        	PlaySoundAtEntity("slime2", "slime_loop.snt", "AreaSlimeSound_2", 1.0f, true);
        	PlaySoundAtEntity("slime3", "slime_loop.snt", "AreaSlimeSound_3", 1.0f, true);
        	PlaySoundAtEntity("slime4", "slime_loop.snt", "AreaSlimeSound_4", 1.0f, true);
        	PlaySoundAtEntity("slime5", "slime_loop.snt", "AreaSlimeSound_5", 1.0f, true);
        	CreateParticleSystemAtEntity("slimeps1", "ps_slime_fog.ps", "AreaSlimeSound_1", true);
        	CreateParticleSystemAtEntity("slimeps2", "ps_slime_fog.ps", "AreaSlimeSound_2", true);
        	CreateParticleSystemAtEntity("slimeps3", "ps_slime_fog.ps", "AreaSlimeSound_3", true);
        	CreateParticleSystemAtEntity("slimeps4", "ps_slime_fog.ps", "AreaSlimeSound_4", true);
        	CreateParticleSystemAtEntity("slimeps5", "ps_slime_fog.ps", "AreaSlimeSound_5", true);

        	for(int i=1;i<=9;i++)
        		SetEntityActive("SlimeDamageArea_"+i, true);

        	//Function was triggered because player did not lock door for intro event
        	if(GetLocalVarInt("GuardianInRoom") == 1)
        		AddTimer("playerdeath", 4.0f, nameof(TimerPlayerDeath));

        	//Function was triggered because player locked door for intro event and later completed first altar.
        	if(GetLocalVarInt("GuardianInRoom") == 0){
        		SetEntityActive("safety_normal_vert_4", true);
        		SetEntityActive("safety_normal_vert_1", false);

        		PlaySoundAtEntity("bang2", "28_break_wood.snt", "AreaSeeLever", 0.0f, false);
        		PlaySoundAtEntity("bang3", "28_break_rock.snt", "AreaSeeLever", 0.0f, false);

        		StopMusic(1.0f, 10);
        		PlayMusic("02_amb_strange.ogg", true, 1, 1, 0, true);

        		// Sound and screen shake
        		AddTimer("AddScareReactions", 0.5f, nameof(TimerDistantHowl));
        	}
        }

        // Create particles, play break sound and set door to collapsed
        // THIS TIMER IS DISABLED
        void TimerGuardianDoorBreak(string asTimer)
        {
        	// TODO: Maybe add some other sound? explosion_rock_large is like starting to get
        	PlaySoundAtEntity("DoorBreak", "explosion_rock_large", "AreaBreakDoor2", 0, false);
        	StartScreenShake(0.2f, 0, 1, 0.2f);

        	// Particles here
        	CreateParticleSystemAtEntity("cloud1", "ps_break_cavein.ps", "AreaBreakDoor1", false);
        	CreateParticleSystemAtEntity("cloud2", "ps_break_cavein.ps", "AreaBreakDoor2", false);
        	CreateParticleSystemAtEntity("cloud3", "ps_break_cavein.ps", "AreaBreakDoor3", false);

        	// Break door here
        	SetLocalVarInt("DoorCollapsed", 1);
        	SetPropActiveAndFade("safety_large_vert_1", false, 0.1f);
        }

        //--------------------------------------------------------

        ////////////////////////////////
        // NEW PUZZLE FOR DOOR
        ////////////////////////////////

        //--------------------------------------------------------

        //Touch  one of the two altars activates two areas in the same room and a bunch of effects and sounds.
        void InteractTouchAltar(string asEntity)
        {
        	// If already activated, only damage sanity
        	if(GetLocalVarInt(asEntity+"Activated")==1)
        	{
        		GiveSanityDamage(5, true);
        		return;
        	}

        	SetLocalVarInt(asEntity +"Activated", 1);

        	SetEntityActive(asEntity+"_Ground", true);

        	//SetEntityPlayerLookAtCallback(asEntity+"_Wall", "LookAtAltarWall", false);

        	SetLampLit(asEntity+"_Light_*", false, true);

        	PlayGuiSound("sanity_heartbeat.snt", 1.0f);
        	PlayGuiSound("19_inject.ogg", 0.4f);
        	PlayGuiSound("general_wind_whirl.snt", 0.5f);

        	PlaySoundAtEntity("blood", "28_touch", "Player", 0.05f, false);
        	AddTimer("blood", 1.0f, nameof(TimerBloodSound));

        	SetRadialBlurStartDist(0.2f);
        	FadeRadialBlurTo(0.2f, 0.1f);

        	AddTimer("react_scare", 0.5f, nameof(TimerPlayerReact));
        	AddTimer("react_breath", 2.0f, nameof(TimerPlayerReact));
        	AddTimer("react_breath", 4.0f, nameof(TimerPlayerReact));
        	AddTimer("stopeffect", 8.0f, nameof(TimerPlayerReact));

        	GivePlayerDamage(20.0f, "BloodSplat", false, false);

        	SetEntityActive(asEntity+"_knife", false);
        	SetEntityActive(asEntity+"_knife_blood", true);

        	//Did not look so good
        	//SetPropActiveAndFade(asEntity+"_knife", false, 0.5f);
        	//SetPropActiveAndFade(asEntity+"_knife_blood", true, 0.5f);

        	FadeLightTo(asEntity+"_AltarLight", 0.6f, 0.0f, 0.0f, 1.0f, -1, 1);
        	FadeLightTo(asEntity+"_GroundLight", 0.8f, 0.0f, 0.0f, 1.0f, -1, 1);

        	FadeLightTo(asEntity+"_LightPoint_1", 0.15f, 0.05f, 0.0f, -1, -1, 1);
        	FadeLightTo(asEntity+"_LightPoint_2", 0.15f, 0.05f, 0.0f, -1, -1, 1);

        	AddEntityCollideCallback("Player", asEntity+"_Ground", nameof(CollideAltarGround), false, 0);

        	AddDebugMessage("Activate Areas", false);
        }
        void TimerBloodSound(string asTimer)
        {
        	StopSound("blood", 1.0f);
        }
        void TimerPlayerReact(string asTimer)
        {
        	PlaySoundAtEntity(asTimer, asTimer, "Player", RandFloat(0.0f, 1.0f), false);

        	if(asTimer == "stopeffect")
        		FadeRadialBlurTo(0.0f, 0.1f);
        }

        //Stand at the mark on ground to activate look at for wall ornament over altar
        void CollideAltarGround(string asParent, string asChild, int alState)
        {
        	string sChild = StringSub(asChild, 0, 7);

        	//When in area it is possible to look at altar and a red light shows this, also first time a bunch of effects to hint of what to do
        	if(alState == 1){
        		SetLampLit(sChild+"_Light2_*", false, true);

        		FadeLightTo(sChild+"_LightPoint_3", 0.15f, 0.05f, 0.0f, -1, -1, 1);
        		FadeLightTo(sChild+"_AltarLight", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        		FadeLightTo(sChild+"_GroundLight", 0.6f, 0.0f, 0.0f, 1.0f, -1, 1);
        		FadeLightTo(sChild+"_HintLight", 0.3f, 0.0f, 0.0f, 1.0f, -1, 0.5f);

        		PlaySoundAtEntity(sChild+"_groundloop", "28_ground.snt", "Player", 0.5f, false);

        		SetEntityActive(sChild+"_Wall", true);

        		if(GetLocalVarInt(sChild) == 0)
        			PlayGuiSound("general_wind_whirl.snt", 0.4f);
        		SetLocalVarInt(sChild, 1);

        		AddDebugMessage("Activate "+sChild+"_Wall", false);
        	}

        	//When leaving area, not possible to look at altar and sound and red light clues fade out
        	else if(alState == -1){
        		SetEntityActive(sChild+"_Wall", false);
        		//RemoveTimer(sChild+"_Wall");

        		//FadeLightTo(sChild+"_HintLight", 0.0f, 0.0f, 0.0f, 0.0f, -1, 0.5f);

        		StopSound(sChild+"_groundloop", 0.5f);
        		//StopSound(sChild+"_wallloop", 0.5f);

        		AddDebugMessage("Disabled "+sChild+"_Wall", false);
        	}

        	LookAtAltarWall(sChild+"_Wall", alState);	//Instead of looking at wall, just trigger it on collide with ground.
        }

        //Looking at wall ornament begins a timer
        void LookAtAltarWall(string asEntity, int alState)
        {
        	string sChild = StringSub(asEntity, 0, 7);

        	//Looking at altar will make it shin brighter red and a sound is heard
        	if(alState == 1){
        		RemoveTimer(asEntity);
        		AddTimer(asEntity, 3.0f, nameof(TimerCompletePuzzle));

        		FadeLightTo(sChild+"_HintLight", 0.9f, 0.0f, 0.0f, -1, -1, 2.0f);

        		PlaySoundAtEntity(sChild+"_wallloop", "28_wall.snt", "Player", 0.1f, false);
        	}

        	//The light and sound fade out if looking away
        	else{
        		RemoveTimer(asEntity);
        		StopSound(sChild+"_wallloop", 0.1f);
        		FadeLightTo(sChild+"_HintLight", 0.3f, 0.0f, 0.0f, -1, -1, 0.5f);
        	}

        	AddDebugMessage("Look at state: "+alState, false);
        }

        //If looking the timer amount activte wall ornament, it goes square red and sound/effects are played
        void TimerCompletePuzzle(string asTimer)
        {
        	AddDebugMessage("Part Complete!", false);

        	AddLocalVarInt("PuzzleComplete", 1);

        	string sChild = StringSub(asTimer, 0, 7);

        	FadeLightTo(sChild+"_WallLight", 0.5f, 0.0f, 0.0f, 1.0f, -1, 2);
        	FadeLightTo(sChild+"_GroundLight", 0.0f, 0.0f, 0.0f, 0.0f, -1, 2);
        	FadeLightTo(sChild+"_HintLight", 0.0f, 0.0f, 0.0f, 0.0f, -1, 2);

        	SetEntityActive(asTimer, false);
        	SetEntityActive(sChild+"_Ground", false);

        	StopSound(sChild+"_groundloop", 2.0f);
        	StopSound(sChild+"_wallloop", 2.0f);

        	PlaySoundAtEntity("dones2", "28_done02.snt", "Player", 0.0f, false);
        	PlaySoundAtEntity("dones3", "28_done03.snt", sChild, 0.0f, false);
        	PlaySoundAtEntity("dones", "28_done.snt", sChild, 0.0f, false);

        	GiveSanityDamage(35.0f, true);

        	//Both walls have been activated, this triggers the old slime event and also opens he large door
        	if(GetLocalVarInt("PuzzleComplete") == 2){
        		SetEntityActive("slime_entry_5_5", false);	//Remove the area that kills player from the intro event
        		SetEntityActive("slime_entry_4_5", false);

        		AddTimer(nameof(TimerGuardianRoar), 4.0f, nameof(TimerGuardianRoar));

        		//AddTimer("SmallAttack", 6.0f, "TimerGuardianHit");

        		AddTimer("WallLightOut", 4.0f, nameof(TimerWallLightOut));
        		AddTimer("Endthings", 6.0f, nameof(TimerEndThings));

        		SetMoveObjectState("safety_large_vert_1", 1.0f);
        		SetEntityPlayerInteractCallback("safety_large_vert_1", "", false);	//Disabled interact on saftey door

        		StartScreenShake(0.01f, 0.5f, 0.5f, 2.0f);

        		CompleteQuest("28DoorShut", "28DoorShut");

        		PlayMusic("16_puzzle_bridge.ogg", false, 0.7f, 1.0f, 10, false);

        		StopSound("Sound_2", 12.0f);
        		StopMusic(12.0f, 0);

        		PlaySoundAtEntity("rock", "28_rotating_rock.snt", "AreaEndDoor", 12.0f, true);
        		PlaySoundAtEntity("iii", "28_terror_meter.snt", "Player", 12.0f, true);
        		PlaySoundAtEntity("dooropen", "door_safety_open.snt", "safety_large_vert_1", 0.0f, false);

        		FadeGlobalSoundSpeed(0.8f, 12.0f);

        		for(int i =1;i<=19;i++)
        			StopSound("torch_static01_"+i+"_Sound_1", 12.0f);

        		StopSound("slime1", 12.0f); StopSound("slime2", 12.0f);
        		StopSound("slime3", 12.0f); StopSound("slime4", 12.0f);
        		StopSound("slime5", 12.0f);
        	}
        }
        //Fadeout square lights for altars when puzzle done.
        void TimerWallLightOut(string asTimer)
        {
        	FadeLightTo("Altar_1_WallLight", 0.0f, 0.0f, 0.0f, 1.0f, -1, 2);
        	FadeLightTo("Altar_2_WallLight", 0.0f, 0.0f, 0.0f, 1.0f, -1, 2);
        }
        //Stop music and only play heartbeat + breath sound for rest of level + some other ambient sounds
        void TimerEndThings(string asTimer)
        {
        	StopMusic(8.0f, 10);
        	AddTimer("react_breath_slow", 4.0f, nameof(TimerEndReact));
        	AddTimer("sanity_heartbeat", 2.0f, nameof(TimerEndReact));
        }
        void TimerEndReact(string asTimer)
        {
        	if(asTimer == "sanity_heartbeat"){
        		PlayGuiSound(asTimer+".snt", 0.9f);
        		AddTimer("sanity_heartbeat", 2.0f, nameof(TimerEndReact));
        	}
        	else{
        		PlayGuiSound(asTimer+".snt", 0.4f);
        		AddTimer("react_breath_slow", 4.0f, nameof(TimerEndReact));
        	}
        }
        //When large door open, the player gets a constant small hit from the guardian to stress for the final battle.
        //Disabled and replaced with the areas instead
        void TimerGuardianHit(string asTimer)
        {
        	StartScreenShake(0.05f, 0.5f, 0.5f, 0.5f);

        	PlayGuiSound("water_lurker_eat_rev2.ogg", 0.2f);
        	PlayGuiSound("attack_claw_hit03.ogg", 0.2f);

        	GivePlayerDamage(2, "BloodSplat", true, false);

        	AddTimer("SmallAttack", RandFloat(3.0f,6.0f), nameof(TimerGuardianHit));
        }

        //----------------------------------------------------------

        //////////////////////////////////
        //END HALL - close the large door behind the player
        //////////////////////////////////

        //----------------------------------------------------------

        void CollideAreaCloseDoor(string asParent, string asChild, int alState)
        {
        	PlayGuiSound("guardian_activated.snt", 0.8f);
        	StartScreenShake(0.01f, 1.0f, 1.0f, 1.0f);
        	SetMoveObjectState("safety_large_vert_1", 0.0f);

        	RemoveTimer("SmallAttack");
        }

        //----------------------------------------------------------

        //////////////////////////////////
        //SEE NOTE HINT
        //////////////////////////////////

        //----------------------------------------------------------

        //Point vision to the table to hint about the note (as it is no longer requiered to pickup for advancement)

        void CollideAreaSeeNote(string asParent, string asChild, int alState)
        {
        	StartPlayerLookAt("note_generic_1", 4.0f, 4.0f, "");
        	PlayMusic("04_puzzle_acid.ogg", false, 1.0f, 1.0f, 10, false);
        	AddTimer("EndLookAt", 1.0f, nameof(TimerEndLookAt));
        	AddTimer("music", 4.0f, nameof(TimerEndLookAt));
        }
        void TimerEndLookAt(string asTimer)
        {
        	StopPlayerLookAt();

        	if(asTimer == "music")
        		StopMusic(4.0f, 10);
        }

        //----------------------------------------------------------

        ////////////////////////////////////////////
        //GUARDIAN INTRO
        ////////////////////////////////////////////

        //----------------------------------------------------------

        //A faint guardian sound at start of level
        void CollideAreaGuardianAmb(string asParent, string asChild, int alState)
        {
        	PlayGuiSound("guardian_distant2.ogg", 1.0f);
        	StartScreenShake(0.02f, 1.0f, 1.0f, 3.0f);
        }
        //Half-way down the stairs the guardian slime poofs start to appear
        void CollideAreaStartIntro(string asParent, string asChild, int alState)
        {
        	CheckPoint("CP01", "PlayerStart_Check", nameof(CPCall01), "Hints", "DeathGuardian_25_CellTunnels");

        	AddTimer(nameof(TimerEIntro), 0.1f, nameof(TimerEIntro));
        }
        //Events for each of the 5 poofs, in 6 poof player dies if he did not close the door.
        void TimerEIntro(string asTimer)
        {
        	string sEvent = asTimer;	//Do not edit, sets timer loop function name.
        	AddLocalVarInt(sEvent, 1);	//Do not edit, steps through timer loop events.
        	bool bPauseAtStep = false;	//Do not edit, to pause or end the timer loop at specified step.

        	float fEventSpeed = 5.0f;	//Set to preferred default time for the timer loop.


        	switch(GetLocalVarInt(sEvent)){
        		//////////
        		//Guardian begins chase in stairs step 1
        		case 1:
        			IntroEventSteps("slime_entry_1_");
        			PlayMusic("att_guardian.ogg", true, 0.8f, 1.0f, 5, true);
        		break;

        		//////////
        		//Guardian step 2
        		case 2:
        			IntroEventSteps("slime_entry_2_");
        		break;

        		//////////
        		//Guardian step 3
        		case 3:
        			IntroEventSteps("slime_entry_3_");
        		break;

        		//////////
        		//Guardian step 4
        		case 4:
        			IntroEventSteps("slime_entry_4_");
        		break;

        		//////////
        		//Guardian step 5
        		case 5:
        			IntroEventSteps("slime_entry_5_");

        			//Player has closed the door, so some impacts on it for the last step to hint that guardian is stopped.
        			if(GetLocalVarInt("LeverDown") == 1){
        				PlaySoundAtEntity("impact", "28_break_wood", "safety_normal_vert_1", 0.0f, false);
        				PlaySoundAtEntity("impact2", "28_impact_wood", "safety_normal_vert_1", 0.0f, false);
        				StartScreenShake(0.1f, 0.5f, 0.25f, 1.25f);
        				CreateParticleSystemAtEntity("impact_door", "ps_dust_impact_vert.ps", "safety_normal_vert_1", false);
        			}
        		break;

        		//////////
        		//Player did not close door, Guardian in main room and player dies from it.
        		case 6:
        			if(GetLocalVarInt("LeverDown") == 1) return;

        			SetLocalVarInt("GuardianInRoom", 1);
        			AddTimer(nameof(TimerGuardianRoar), 0.0f, nameof(TimerGuardianRoar));
        		break;

        		//////////
        		//End timer when no more steps in the event found.
        		default:
        			bPauseAtStep = true;
        		break;
        	}

        	if(!bPauseAtStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }
        //Shared settings for intro event steps
        void IntroEventSteps(string sArea)
        {
        	SetPropActiveAndFade(sArea+"*", true, 1.0f);
        	FadeLightTo(sArea+"4", 0.7f, 0.0f, 0.2f, 1.0f, 4, 1);
        	SetEntityActive(sArea+"5", true);
        	PlaySoundAtEntity(sArea+"appear", "25_guardian_slime_appear.snt", sArea+"5", 0.0f, false);
        	CreateParticleSystemAtEntity(sArea+"GuardianPoof","ps_guardian_appear_explosion.ps", sArea+"6", false);
        	StartScreenShake(0.1f, 0.5f, 0.5f, 2.0f);
        }

        //If player is not fast enough it is instant death on the slime poof
        void CollideGuardianArea(string asParent, string asChild, int alState)
        {
        	if(GetPlayerHealth()<=0) return;

        	StartScreenShake(0.2f, 0.5f, 0.5f, 0.5f);
        	PlayGuiSound("water_lurker_eat_rev2.ogg", 0.8f);
        	PlayGuiSound("attack_claw_hit03.ogg", 0.8f);

        	GivePlayerDamage(120, "BloodSplat", true, true);
        }

        //When coming to main hall, direct vision to the lever for closing door.
        void CollideAreaSeeLever(string asParent, string asChild, int alState)
        {
        	StartPlayerLookAt("lever_nice01_1", 4.0f, 4.0f, "");

        	AddTimer("EndLookAtLever", 1.0f, nameof(TimerEndLookAtLever));
        }
        void TimerEndLookAtLever(string asTimer)
        {
        	StopPlayerLookAt();
        }

        //Pulling the lever closes the entrance door and also opens the two doors to altar rooms.
        void StateChangeLever(string asEntity, int alState)
        {
        	if(alState != -1) return;

        	SetLeverStuckState(asEntity, alState, true);

        	StartScreenShake(0.01f, 0.5f, 0.2f, 1.8f);

        	SetLocalVarInt("LeverDown", 1);
        	StopMusic(4.0f, 5);
        	StopMusic(4.0f, 0);

        	SetEntityPlayerInteractCallback("safety_normal_vert_2", "", true);
        	SetEntityPlayerInteractCallback("safety_normal_vert_3", "", true);

        	AddTimer("music", 4.0f, nameof(TimerMusic));
        }
        void TimerMusic(string asTimer)
        {
        	PlayMusic("21_agrippa_lever.ogg", false, 1.0f, 0.0f, 1, false);
        	PlayMusic("amb_extra01.ogg", true, 0.9f, 5, 0, true);
        }

        //Kill player if he did not lock the door. - This is called from the old slime appear event
        void TimerPlayerDeath(string asTimer)
        {
        	CollideGuardianArea("", "", 1);
        }

        //////////////////////////////
        //Interact with altars
        void InteractPlayerAltar(string asEntity)
        {
        	GiveSanityDamage(5, true);
        }

        //----------------------------------------------------------

        //////////////////////////////
        //CHECKPOINT RESET AND EVENT
        //////////////////////////////

        //----------------------------------------------------------

        //Player wakes up at bottom stairs just as the chase restarts
        void CPCall01(string asName, int alCount)
        {
        	CheckPoint("CP02", "PlayerStart_Check02", nameof(CPCall02), "", "");

        	CPReset();

        	RemoveTimer(nameof(TimerEIntro));

        	SetLocalVarInt(nameof(TimerEIntro), 0);
        	SetLocalVarInt("LeverDown", 0);

        	AddDebugMessage("Resetting props", false);
        	SetLeverStuckState("lever_nice01_1", 0, false);
        	SetMoveObjectState("safety_normal_vert_1", 1);
        	SetMoveObjectState("safety_normal_vert_2", 0); SetMoveObjectState("safety_normal_vert_3", 0);

        	//////////////////////////////////////////////////////////////////////
        	// Removed these as they could break the level in some situations
        	//ResetProp("lever_nice01_1");
        	//ResetProp("safety_normal_vert_1"); ResetProp("safety_normal_vert_2");
        	//ResetProp("safety_normal_vert_3");

        	SetEntityPlayerInteractCallback("safety_normal_vert_2", nameof(PlayerInteractDoor), true);
        	SetEntityPlayerInteractCallback("safety_normal_vert_3", nameof(PlayerInteractDoor), true);

        	CPEventInit("AreaStartIntro");

        	SetEntityActive("AreaSeeLever", true);	//Area to show player lever as an extra hint second attempt

        	AddTimer(nameof(TimerEIntro), 4.0f, nameof(TimerEIntro));
        }

        //----------------------------------------------------------

        //Player wakes up inside main hall, with the door closed behind him.
        void CPCall02(string asName, int alCount)
        {
        	CPReset();

        	RemoveEntityCollideCallback("Player", "AreaSeeLever");

        	SetLeverStuckState("lever_nice01_1", -1, false);

        	//safety_normal_vert_1

        	CPEventInit("lever_nice01_1");
        }

        //----------------------------------------------------------

        //Shared CP reset of props, vars, sounds, light
        void CPReset()
        {
        	AddDebugMessage("Resetting", false);
        	SetPlayerHealth(100);

        	SetLocalVarInt("GuardianInRoom", 0);

        	RemoveTimer(nameof(TimerGuardianRoar));
        	RemoveTimer("playerdeath");
        	RemoveTimer("pre3"); RemoveTimer("pre2");
        	RemoveTimer("pre1"); RemoveTimer("pre0");

        	SetEntityActive("slime*", false);
        	SetEntityActive("pre0_*", false); SetEntityActive("pre1_*", false);
        	SetEntityActive("pre2_*", false); SetEntityActive("pre3_*", false);

        	StopSound("slime1", 0.1f); StopSound("slime2", 0.1f); StopSound("slime3", 0.1f);
        	StopSound("slime4", 0.1f); StopSound("slime5", 0.1f);

        	DestroyParticleSystem("slimeps1"); DestroyParticleSystem("slimeps2"); DestroyParticleSystem("slimeps3");
        	DestroyParticleSystem("slimeps4"); DestroyParticleSystem("slimeps5");

        	FadeLightTo("slime_entry_1_4", 0.6f, 0.1f, 0, -1, -1, 0);
        	FadeLightTo("slime_entry_2_4", 0.6f, 0.1f, 0, -1, -1, 0);
        	FadeLightTo("slime_entry_3_4", 0.6f, 0.1f, 0, -1, -1, 0);
        	FadeLightTo("slime_entry_4_4", 0.6f, 0.1f, 0, -1, -1, 0);
        	FadeLightTo("slime_entry_5_4", 0.6f, 0.1f, 0, -1, -1, 0);

        	FadeLightTo("PointLight_11_slime", -1, -1, -1, -1, 0, 0);
        	FadeLightTo("PointLight_7", -1, -1, -1, -1, 9, 0);
        }

        //Shared CP setup for waking up event
        void CPEventInit(string sLook)
        {
        	SetRadialBlurStartDist(0.3f);
        	FadeRadialBlurTo(0.3f, 0.3f);
        	SetPlayerCrouchDisabled(true);
        	SetPlayerJumpDisabled(true);
        	PlayerMovement(0.1f);
        	FadePlayerRollTo(40, 10, 100);
        	MovePlayerHeadPos(0, -1.3f, 0, 10, 0.5f);
        	StartPlayerLookAt(sLook, 5.0f, 5.0f, "");

        	AddTimer("1", 2.0f, nameof(TimerCPEvent));
        	AddTimer("2", 3.0f, nameof(TimerCPEvent));
        	AddTimer("3", 3.5f, nameof(TimerCPEvent));
        	AddTimer("4", 4.5f, nameof(TimerCPEvent));
        	AddTimer("5", 5.0f, nameof(TimerCPEvent));
        	AddTimer("6", 6.0f, nameof(TimerCPEvent));
        }

        //----------------------------------------------------------

        //Shared waking up event
        void TimerCPEvent(string asTimer)
        {
        	if(asTimer == "1"){
        		FadePlayerRollTo(0, 4, 20);
        		PlayGuiSound("react_sigh.snt", 0.7f);
        		PlayerMovement(0.2f);
        	}
        	else if(asTimer == "2"){
        		MovePlayerHeadPos(0, 0, 0, 1, 0.5f);
        		PlayGuiSound("player_stand.snt", 1);
        		PlayerMovement(0.3f);
        		StopPlayerLookAt();
        	}
        	else if(asTimer == "3"){
        		FadePlayerRollTo(-20, 8, 30);
        		PlayerMovement(0.4f);
        	}
        	else if(asTimer == "4"){
        		FadeRadialBlurTo(0.0f, 0.1f);
        		MovePlayerHeadPos(0, -0.8f, 0, 2, 0.5f);
        		PlayGuiSound("player_crouch.snt", 1);
        		PlayerMovement(0.6f);
        	}
        	else if(asTimer == "5"){
        		FadePlayerRollTo(20, 10, 40);
        		PlayGuiSound("react_sigh.snt", 0.7f);
        		PlayerMovement(0.8f);
        	}
        	else if(asTimer == "6"){
        		MovePlayerHeadPos(0, 0, 0, 1, 0.5f);
        		FadePlayerRollTo(0, 10, 60);
        		PlayGuiSound("player_stand.snt", 1);
        		PlayerMovement(1.0f);
        		SetPlayerCrouchDisabled(false);
        		SetPlayerJumpDisabled(false);
        	}
        }

        //----------------------------------------------------------

        //Shared player movement
        void PlayerMovement(float fSpeed)
        {
        	SetPlayerMoveSpeedMul(fSpeed);
        	SetPlayerRunSpeedMul(fSpeed);
        	SetPlayerLookSpeedMul(fSpeed);
        }

        //----------------------------------------------------------

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("InnerSanctum");

        	ClearSavedMaps();

        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("lantern", "lantern.ent");
        		for(int i=0;i<10;++i)
        			GiveItemFromFile("tinderbox", "tinderbox.ent");
        	}

        	///////////////////////////
        	// Collide Callbacks
        	AddEntityCollideCallback("Player", "AreaCheckCollapsedDoor", nameof(CollideCheckCollapsedDoor), false, 1);

        	AddEntityCollideCallback("Player", "AreaCloseDoor", nameof(CollideAreaCloseDoor), true, 1);

        	AddEntityCollideCallback("Player", "AreaSeeNote", nameof(CollideAreaSeeNote), true, 1);
        	AddEntityCollideCallback("Player", "AreaSeeLever", nameof(CollideAreaSeeLever), true, 1);
        	AddEntityCollideCallback("Player", "AreaStartIntro", nameof(CollideAreaStartIntro), true, 1);
        	AddEntityCollideCallback("Player", "AreaGuardianAmb", nameof(CollideAreaGuardianAmb), true, 1);

        	for(int i=1;i<=6;i++)
        		AddEntityCollideCallback("Player", "slime_entry_"+i+"_5", nameof(CollideGuardianArea), false, 1);

        	///////////////////////////
        	// Connect entities
        	ConnectEntities("door1", "lever_nice01_1", "safety_normal_vert_1",false, 0,"");
        	ConnectEntities("door1", "lever_nice01_1", "safety_normal_vert_2",true, 0,"");
        	ConnectEntities("door1", "lever_nice01_1", "safety_normal_vert_3",true, 0,"");

        	///////////////////////////
        	// Move objects
        	SetMoveObjectState("safety_normal_vert_1", 1);
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("28_impact_wood.snt"); PreloadSound("door_safety_open.snt"); PreloadSound("28_terror_meter.snt"); PreloadSound("guardian_distant1.snt");
        	PreloadSound("slime_create.snt"); PreloadSound("25_guardian_slime_appear.snt"); PreloadSound("slime_loop.snt"); PreloadSound("28_break_wood.snt");
        	PreloadSound("28_rotating_rock.snt"); PreloadSound("28_break_rock.snt"); PreloadSound("28_ground.snt");
        	PreloadSound("28_wall.snt"); PreloadSound("8_done02.snt"); PreloadSound("28_done03.snt "); PreloadSound("28_done.snt ");
        	PreloadSound("player_crouch.snt"); PreloadSound("player_stand.snt"); PreloadSound("react_sigh.snt"); PreloadSound("guardian_distant2.ogg");
        	PreloadSound("guardian_idle6.ogg"); PreloadSound("sanity_heartbeat.snt"); PreloadSound("general_wind_whirl.snt"); PreloadSound("guardian_activated.snt ");
        	PreloadSound("water_lurker_eat_rev2.ogg "); PreloadSound("attack_claw_hit03.ogg ");

        	PreloadParticleSystem("ps_slime_fog.ps "); PreloadParticleSystem("ps_dust_impact_vert.ps "); PreloadParticleSystem("ps_guardian_appear_explosion.ps");

        	PlayMusic("amb_extra01.ogg", true, 0.9f, 5, 0, true);

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	FadeGlobalSoundSpeed(1.0f, 0.0f);
        	GiveSanityBoostSmall();
        	SetPlayerHealth(100);
        	TimerStopRadialBlur("");
        }
    }
}
