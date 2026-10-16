using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/21_torture_nave.hps; journal: docs/ScriptPort/ch02/21_torture_nave.md.</summary>
    [HplMapScript("maps/main/ch02/21_torture_nave.map")]
    public class TortureNave : HplScript
    {
        //------------------------------------------

        ///////////////////////////////////////////
        // AGRIPPA WAKE UP
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideWakeUpAgrippa(string asParent, string asChild, int alState)
        {
        	//If there is a flashback playing wait a little longer.
        	if(GetFlashbackIsActive() || alState != 1)
        	{
        		return;
        	}

        	RemoveEntityCollideCallback(asParent, asChild);

        	SetNPCAwake("agrippa_2", true, true);

        	SetLocalVarInt("AgrippaAwake",1);

        	StartPlayerLookAt("AreaAgrippa", 3, 5, "");
        	PlayerReact(true,0.5f);

        	AddTimer("AgrippaScareStopLookAt", 1.5f, nameof(TimerAgrippaScareStopLookAt));
        	AddTimer("AgrippaScareOver", 	3, nameof(TimerAgrippaScareOver));

        	PlayGuiSound("general_chain_rattle_single.snt", 0.5f);

        	PlayMusic("21_agrippa_lever.ogg", false, 1.0f, 0.8f, 9, false);

        	PlaySoundAtEntity("wake", "agrippa_wake.snt", "AreaAgrippa", 1.0f, false);
        }

        void TimerAgrippaScareStopLookAt(string asTimer)
        {
        	StopPlayerLookAt();
        }

        void TimerAgrippaScareOver(string asTimer)
        {
        	//Agrippa starts talking, make sure 1 and 2 are played first. After that it will be random.
        	AgrippaStrainedSpeak(1);
        	AgrippaStrainedSpeak(2);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // AGRIPPA STRAINED TALKING
        ////////////////////////////////////////////

        //------------------------------------------

        void AgrippaStrainedSpeak(int alNum)
        {
        	////////////////////////////////////////
        	//If below zero, pick at random.
        	if(alNum < 0)
        	{
        		int lLast = GetLocalVarInt("LastStrainedVoiceIndex");

        		alNum = RandInt(1, 4);
        		if(alNum == lLast)
        		{
        			alNum++;
        			if(alNum > 4) alNum = 1;
        		}
        	}

        	////////////////////////////////////////
        	//Save what voice is played
        	SetLocalVarInt("LastStrainedVoiceIndex", alNum);

        	////////////////////////////////////////
        	//Play the voice at Agrippa
        	AddEffectVoice("CH02L21_Agrippa_Strained_0"+alNum+".ogg", "", "Voice", "CH02L21_Agrippa_Strained_0"+alNum, true, "AreaAgrippa", 4, 12);

        	////////////////////////////////////////
        	//When over, play something else.
        	SetEffectVoiceOverCallback(nameof(AgrippaStrainedSpeakStartRandom));
        }

        //------------------------------------------

        void AgrippaStrainedSpeakStartRandom()
        {
        	AddTimer("AgrippaRandomStrainedSpeak", RandFloat(2, 4), nameof(TimerAgrippaRandomStrainedSpeak));
        }

        void TimerAgrippaRandomStrainedSpeak(string asTimer)
        {
        	//If a flashback or voice is active, wait a little longer.
        	if(GetFlashbackIsActive() || GetEffectVoiceActive())
        	{
        		AgrippaStrainedSpeakStartRandom();
        		return;
        	}

        	AgrippaStrainedSpeak(-1);
        }


        //------------------------------------------

        void StopAgrippaStrainedSpeak()
        {
        	//Make sure that no voice is played again!
        	SetEffectVoiceOverCallback("");
        	RemoveTimer("AgrippaRandomStrainedSpeak");

        	//Stop the current voice playing
        	StopAllEffectVoices(0.5f);

        	//In case player is very fast and runs outside of area before first event is over.
        	RemoveTimer("AgrippaScareOver");
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // AGRIPPA CHANNELING MACHINE START
        ////////////////////////////////////////////

        //------------------------------------------

        void ChangeChannelingLever(string asEntityName, int alState)
        {
        	if(alState != 1) return;

        	AddDebugMessage("Turned channeling on chaneling machine!", false);

        	//Stop any strained talking going on
        	StopAgrippaStrainedSpeak();

        	//Lever is now stuck and will not move.
        	SetLeverStuckState("channeling_machine_lever_1", alState, true);

        	SetLocalVarInt("AgrippaVoiceMachineOn",1);

        	SetGlobalVarInt("AgrippaActivatedIn21", 1);	//Agrippa is awake in 26 too

        	AddTimer("AgrippaNormalTalkStart", 2, nameof(TimerAgrippaNormalTalkStart));

        	//Add effects on machine
        	CreateParticleSystemAtEntity("ChannelingMachinePS", "ps_channeling_machine_smoke", "AreaChannelingMachine", true);

        	PlaySoundAtEntity("ChannelingMachineStart","21_cm_reverse_low.snt", "AreaChannelingMachine", 0.05f, false);
        	PlaySoundAtEntity("ChannelingMachineRunning","21_channeling_machine.snt", "AreaChannelingMachine", 2.0f, true);
        }

        //------------------------------------------

        void TimerAgrippaNormalTalkStart(string asTimer)
        {
        	AgrippaNormalSpeak(true);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // AGRIPPA NORMAL TALK
        ////////////////////////////////////////////

        //------------------------------------------

        //Helper function to see if agrippa has spoken or not
        bool AgrippaTopicNotSpoken(string asTopic)
        {
        	return GetLocalVarInt("SpokenVar_"+asTopic)==1 ? false : true;
        }

        //Used to set that a certain
        void SetAgrippaTopicHasSpoken(string asTopic)
        {
        	SetLocalVarInt("SpokenVar_"+asTopic, 1);
        }

        //------------------------------------------

        void PlayAgrippaNormalVoice(string asTopic, int alNumOfParts, bool abGreeting, bool abSetAsSpoken)
        {
        	//Set up some settings
        	float fMinDist = 9;
        	float fMaxDist = 16;

        	//Set the topic as spoken
        	if(abSetAsSpoken) SetAgrippaTopicHasSpoken(asTopic);

        	AddDebugMessage("Starting topic "+asTopic+" parts: "+alNumOfParts+".", false);

        	//TODO: Start with random greeting!
        	if(abGreeting)
        	{
        		string sSoundName = "CH02L21_Agrippa_Nrm_Greeting_0"+RandInt(1,4);

        		AddEffectVoice(sSoundName+ ".ogg", "", "Voice", sSoundName, true, "AreaChannelingMachine", fMinDist, fMaxDist);
        	}

        	//Start all voices in topic
        	for(int i=1; i<=alNumOfParts; ++i)
        	{
        		string sNum = i<10 ? "0"+i : ""+i;
        		string sSoundName = "CH02L21_Agrippa_Nrm_"+asTopic+"_"+sNum;

        		AddEffectVoice(sSoundName+ ".ogg", "", "Voice", sSoundName, true, "AreaChannelingMachine", fMinDist, fMaxDist);
        	}

        	//Callback when voices are done playing
        	SetEffectVoiceOverCallback(nameof(AgrippaNormalTalkOver));
        }

        //------------------------------------------

        //Starts a topic for Agrippa. Not that all topics are listed in an order of priority.
        //So the top once are started before the lower.
        void AgrippaNormalSpeak(bool abGreeting)
        {
        	//If there is an voice active, then wait adding.
        	//same if there is a flashback playing!
        	if(GetEffectVoiceActive() || GetFlashbackIsActive())
        	{
        		AgrippaNormalTalkOver();
        		return;
        	}

        	string sTopicName = "";

        	//////////////////////////////////////////
        	// Introduction and Orb Quest
        	//  - First thing spoken!
        	sTopicName = "Introduction";
        	if(AgrippaTopicNotSpoken(sTopicName))
        	{
        		SetLocalVarInt("MentionedOrb", 1);

        		PlayAgrippaNormalVoice(sTopicName, 12, false, true);
        		return;
        	}

        	//////////////////////////////////////////
        	// Tonic Quest
        	//  - Player has not yet picked up weyer note.
        	sTopicName = "TonicQuest";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetLocalVarInt("PickedWeyerNote")==0)
        	{
        		SetLocalVarInt("AgrippaWantsWeyerNote", 1);//<- so player is given a quest!

        		PlayAgrippaNormalVoice(sTopicName, 4, false, true);
        		return;
        	}

        	//////////////////////////////////////////
        	// Weyer Note
        	//  - Player has picked Weyer note
        	sTopicName = "WeyerNote";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetLocalVarInt("PickedWeyerNote")==1)
        	{
        		PlayAgrippaNormalVoice(sTopicName, 5, abGreeting, true);
        		return;
        	}

        	//////////////////////////////////////////
        	// Orbs for Choir done
        	//  - All orbs in choir picked up, but not all in transept
        	sTopicName = "ChoirDone";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetGlobalVarInt("ChoirOrbCount")==3 && GetGlobalVarInt("TranseptOrbCount")<3)
        	{
        		SetLocalVarInt("TranseptOrbNotDone", 1);

        		PlayAgrippaNormalVoice(sTopicName, 1, abGreeting, true);
        		return;
        	}

        	//////////////////////////////////////////
        	// Orbs for Transept done
        	//  - All orbs in transept picked up, but not all in choir
        	sTopicName = "TranseptDone";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetGlobalVarInt("TranseptOrbCount")==3 && GetGlobalVarInt("ChoirOrbCount")<3)
        	{
        		SetLocalVarInt("ChoirOrbNotDone", 1);

        		PlayAgrippaNormalVoice(sTopicName, 1, abGreeting, true);
        		return;
        	}

        	//////////////////////////////////////////
        	// All Orbs Done BUT not all ingredients
        	// - All orbs in choir and transpept picked up. Ingredients are missing.
        	// - This will be repeated until player has found all ingredients!
        	sTopicName = "AllOrbsNotIngred";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetGlobalVarInt("ChoirOrbCount")==3 && GetGlobalVarInt("TranseptOrbCount")==3 && GetGlobalVarInt("IngredientCount")<3)
        	{
        		PlayAgrippaNormalVoice(sTopicName, 3, false, false);
        		return;
        	}

        	//////////////////////////////////////////
        	// All Orbs Done AND all ingredients!
        	// - All orbs in choir picked up, but not all in transept
        	sTopicName = "AllOrbs";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetGlobalVarInt("ChoirOrbCount")==3 && GetGlobalVarInt("TranseptOrbCount")==3 && GetGlobalVarInt("IngredientCount")==3)
        	{
        		PlayAgrippaNormalVoice(sTopicName, 2, false, false);
        		return;
        	}

        	//////////////////////////////////////////
        	// Opened Safety Doors
        	//  - Player has opened safety doors
        	sTopicName = "SafetyDoors";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetLocalVarInt("SafetyDoorsOpen")==1)
        	{
        		PlayAgrippaNormalVoice(sTopicName, 1, abGreeting, true);
        		return;
        	}

        	//////////////////////////////////////////
        	// Pulled Lever
        	//  - Player has pulled lever in Lever Room and Safy doors are NOT open.
        	sTopicName = "PulledLever";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetLocalVarInt("InteractedDoorLever")==1 && GetLocalVarInt("SafetyDoorsOpen")==0)
        	{
        		PlayAgrippaNormalVoice(sTopicName, 2, abGreeting, true);
        		return;
        	}

        	//////////////////////////////////////////
        	// Laboratory Locked
        	//  - Player has interacted with locked lab door
        	sTopicName = "LabLocked";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetLocalVarInt("InteractedLabDoor")==1)
        	{
        		PlayAgrippaNormalVoice(sTopicName, 2, abGreeting, true);
        		return;
        	}


        	//////////////////////////////////////////
        	// Random Chatter
        	// - Agrippa has nothing else to say
        	// - The current chatter is saved in global var AgrippaRandomChatterNum, and is increased after every time he has spoken.
        	{
        		/////////////////////
        		// Number of parts in each random topic
        		int[] vRandomPartNum = {
        			2, 1, 3, 2,
        			3, 2, 2, 1,
        			2, 1, 1, 1
        		};

        		/////////////////////
        		// If end of random chatter is reached, loop the last two.

        		/////////////////////
        		//Get the name of the topic
        		int lNum = GetGlobalVarInt("AgrippaRandomChatterNum");
        		// AngelScript aborts this call for the original default chatter index zero.
        		if(lNum < 1 || lNum > vRandomPartNum.Length) return;
        		int lPartNum = vRandomPartNum[lNum-1];//Index starts at 1!

        		string sNum = lNum<10 ? "0"+lNum : ""+lNum;
        		sTopicName = "Random"+sNum;

        		/////////////////////
        		// Play voice
        		PlayAgrippaNormalVoice(sTopicName, lPartNum, abGreeting, false);

        		/////////////////////
        		// Increase counter, so new chatter is played next time
        		lNum++;
        		if(lNum > 12) lNum = 11; //if end of list, repeat last two!
        		SetGlobalVarInt("AgrippaRandomChatterNum", lNum);
        	}
        }

        //------------------------------------------

        void AgrippaNormalTalkOver()
        {
        	AddTimer("NewAgrippaTopic", RandFloat(3, 12), nameof(TimerNewAgrippaTopic));

        	CheckAgrippaNormalTalkOverEvent();
        }

        void CheckAgrippaNormalTalkOverEvent()
        {
        	////////////////////////////////
        	//Give quest about weyere not
        	if(GetLocalVarInt("AgrippaWantsWeyerNote")==1)
        	{
        		SetLocalVarInt("AgrippaWantsWeyerNote", 0);

        		AddQuest("21FindWeyerNote", "21FindWeyerNote");
        	}

        	////////////////////////////////
        	//Give quest about tonic
        	if(GetLocalVarInt("PickedWeyerNote")==1)
        	{
        		CompleteQuest("21FindWeyerNote", "21FindWeyerNote");
        		AddQuest("21FindTonic", "21FindTonic");
        	}

        	////////////////////////////////
        	//Give quest about orb
        	if(GetLocalVarInt("MentionedOrb")==1)
        	{
        		AddQuest("21FindOrb", "21FindOrb");
        	}

        	////////////////////////////////
        	//Give quest about orb
        	if(GetLocalVarInt("TranseptOrbNotDone")==1)
        	{
        		AddQuest("21OrbsLeftInTransept", "21OrbsLeftInTransept");
        	}

        	////////////////////////////////
        	//Give quest about orb
        	if(GetLocalVarInt("ChoirOrbNotDone")==1)
        	{
        		AddQuest("21OrbsLeftInChoir", "21OrbsLeftInChoir");
        	}
        }

        void TimerNewAgrippaTopic(string asTimer)
        {
        	//Check if player is close enough to Agrippa.
        	if(GetLocalVarInt("PlayerInRandomArea")==1)
        	{
        		//Start a new topic, without any greeting.
        		AgrippaNormalSpeak(false);
        	}
        	else
        	{
        		//Wait some more
        		AddTimer("NewAgrippaTopic", RandFloat(3, 12), nameof(TimerNewAgrippaTopic));

        	}
        }

        //------------------------------------------

        void StopAgrippaNormalSpeak()
        {
        	//No more callback
        	SetEffectVoiceOverCallback("");

        	//Check if there is any event to do when ending.
        	CheckAgrippaNormalTalkOverEvent();

        	//Stop the current voice playing
        	StopAllEffectVoices(0.5f);

        	//No more extra talk
        	RemoveTimer("NewAgrippaTopic");

        	//In case player is very fast and runs before for first greeting is started!
        	RemoveTimer("AgrippaNormalTalkStart");

        }

        //------------------------------------------

        ///////////////////////////////////////////
        // AGRIPPA RANDOM AREA
        // - Simply checks if payer is in the random area, only when in this will random messages be spoken (ie messages after the one spoken when player enters area)
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideAgrippaRandomArea(string asParent, string asChild, int alState)
        {
        	int lVarValue = alState == 1 ? 1 : 0;

        	SetLocalVarInt("PlayerInRandomArea", lVarValue);

        	AddDebugMessage("PlayerInRandomArea: "+lVarValue, false);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // AGRIPPA BASE TALKING
        // - All basic stuff for talking
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideAgrippaStartTalk(string asParent, string asChild, int alState)
        {
        	//Only run function is outside talk range
        	if(GetLocalVarInt("PlayerInAgrippaTalkRange")==1) return;
        	SetLocalVarInt("PlayerInAgrippaTalkRange",1);

        	AddDebugMessage("INSIDE START TALK", false);

        	//If not awake, do nothing
        	if(GetLocalVarInt("AgrippaAwake")==0) return;

        	////////////////////////////
        	//Machine is on, Normal Voice
        	if(GetLocalVarInt("AgrippaVoiceMachineOn")==1)
        	{
        		//Start speaking.
        		AgrippaNormalSpeak(true);
        	}
        	////////////////////////////
        	//Machine is off, Strained Voice
        	else
        	{
        		//Start a timer that will trigger a voice.
        		AgrippaStrainedSpeakStartRandom();
        	}
        }

        //------------------------------------------

        void CollideAgrippaStopTalk(string asParent, string asChild, int alState)
        {
        	//Only run function is inside talk range
        	if(GetLocalVarInt("PlayerInAgrippaTalkRange")==0) return;
        	SetLocalVarInt("PlayerInAgrippaTalkRange",0);

        	AddDebugMessage("OUTSIDE STOP TALK", false);

        	//If not awake, do nothing
        	if(GetLocalVarInt("AgrippaAwake")==0) return;

        	////////////////////////////
        	//Machine is on, Normal Voice
        	if(GetLocalVarInt("AgrippaVoiceMachineOn")==1)
        	{
        		StopAgrippaNormalSpeak();
        	}
        	////////////////////////////
        	//Machine is off, Strained Voice
        	else
        	{
        		//Stop any strained speak
        		StopAgrippaStrainedSpeak();
        	}
        }


        ///////////////////////////////////////////
        // INTERACT LAB DOOR
        ////////////////////////////////////////////

        //------------------------------------------

        void PlayerInteractLabDoor(string asEntity)
        {
        	//SetMessage("Ch02Level21", "LabDoorLocked", 0);
        	SetMessage("LevelDoors", "LockedForever", 0);

        	SetLocalVarInt("InteractedLabDoor", 1);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // REPAIR COG WHEELS IN CEILING
        ////////////////////////////////////////////

        //------------------------------------------

        float gfCogWheelSpeed = 2.0f;
        //------------------------------------------

        void InitCeilingCogWheels()
        {
        	RotatePropToSpeed("ceiling_machinery_cog_4", 10, gfCogWheelSpeed, 0, 1, 0, true, "");
        	RotatePropToSpeed("ceiling_machinery_cog_1", 10, gfCogWheelSpeed, 0, -1, 0, true, "");
        }

        //When player interacts with cog just attach it to the sticky area. (so no movements are needed!)
        void CeilingCogWheelInteracted(string asEntity)
        {
        	AddDebugMessage("Interacted with cogwheel!", false);

        	AttachBodyToStickyArea("StickyArea_1", "ceiling_machinery_cog_interact_1_Body_1");

        	PlaySoundAtEntity("wheel11", "21_cog_slide.snt", "ceiling_machinery_cog_interact_1", 0.0f, true);

        	AddTimer("CogWheelAttached", 0.5f, nameof(TimerCogWheelAttached));
        }

        void TimerCogWheelAttached(string asTimer)
        {
        	//Remove the interactable cog wheel
        	SetEntityActive("ceiling_machinery_cog_interact_1", false);

        	//Make the non-interable wheel visible
        	SetEntityActive("ceiling_machinery_cog_interact_static_2", true);

        	PlaySoundAtEntity("wheel13", "21_cog_attach.snt", "ceiling_machinery_cog_interact_static_2", 0.0f, true);
        	PlaySoundAtEntity("wheel2", "21_cog_wheel_big.snt", "ceiling_machinery_cog_interact_static_2", 1.0f, true);

        	//Make all remaining wheel rotate
        	RotatePropToSpeed("ceiling_machinery_cog_interact_static_2", 10, gfCogWheelSpeed, 0, 1, 0, true, "");
        	RotatePropToSpeed("ceiling_machinery_cog_2", 10, gfCogWheelSpeed, 0, -1, 0, true, "");
        	RotatePropToSpeed("ceiling_machinery_cog_5", 10, gfCogWheelSpeed, 0, 1, 0, true, "");

        	//Set a var to notify that the wheel
        	SetLocalVarInt("CogWheelInPlace", 1);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // PULL SECURITY DOOR LEVER
        ////////////////////////////////////////////

        //------------------------------------------

        //Player interacts with a safety door.
        void InteractSafetyDoor(string asEntity)
        {
        	//Show message
        	SetMessage("Ch02Level21", nameof(InteractSafetyDoor), 0);

        	//Locked sound
        	PlayGuiSound("17_attach_crank", 0.5f);

        	SetMoveObjectState(asEntity, 0.05f);

        	AddTimer(asEntity, 0.4f, nameof(TimerCloseGate));
        	AddTimer("sound", 1.0f, nameof(TimerCloseGate));

        	//Wait 4 seconds and then give a quest.
        	if(QuestIsAdded("20SafetyDoorClosed")==false)
        		AddTimer("SafetyDoorQuest", 3.1f, nameof(TimerSafetyDoorQuest));
        }

        void TimerCloseGate(string asTimer)
        {
        	if(asTimer == "sound"){
        		PlayGuiSound("close_gate.ogg", 0.5f);
        	} else{
        		SetMoveObjectState(asTimer, 0.00f);

        		SetEntityPlayerInteractCallback(asTimer, nameof(InteractSafetyDoor), true);
        	}
        }

        void TimerSafetyDoorQuest(string asTimer)
        {
        	AddQuest("21SafetyDoorClosed", "21SafetyDoorClosed");
        }

        //------------------------------------------

        void PullSafetyDoorLever(string asEntity, int alState)
        {
        	if(alState != -1) return;

        	/////////////////////////////////
        	//Cog wheels are in place
        	if(GetLocalVarInt("CogWheelInPlace")==1)
        	{
        		string sDoorName = asEntity=="lever_simple01_1" ? "safety_large_vert_1" : "safety_large_vert_2";

        		//Open the door
        		SetMoveObjectState(sDoorName, 1);

        		//No more interact callbacks on safety doors.
        		SetEntityPlayerInteractCallback("safety_large_vert_1","",false);
        		SetEntityPlayerInteractCallback("safety_large_vert_2","",false);

        		//Lever becomes stuck
        		SetLeverStuckState(asEntity, alState, true);

        		PlaySoundAtEntity("workins", "21_ignite.snt", "LeverSounds", 0.2f, false);
        		PlaySoundAtEntity("working", "21_lever_success", "LeverSounds", 2.0f, false);
        		PlaySoundAtEntity("working2", "21_loop", "LeverSounds", 2.0f, false);
        		AddTimer("working", 3.0f, nameof(TimerStopSounds));

        		StartScreenShake(0.005f, 0.0f, 3.0f, 3.0f);

        		//Inc lever in position counter
        		AddLocalVarInt("SafetyDoorCount",1);
        		if(GetLocalVarInt("SafetyDoorCount")==2)
        		{
        			//Complete quest with levers.
        			CompleteQuest("21LeverNotWorking","20LeverNotWorking");

        			//Tell agrippa that safety doors are open
        			SetLocalVarInt("SafetyDoorsOpen", 1);

        			//activate an area that triggers a voice from Alexander
        			SetEntityActive("AreaStartAlexanderVoice", true);

        			//Complete quest!
        			CompleteQuest("21SafetyDoorClosed", "20SafetyDoorClosed");

        			//Sanity boost + nice music!
        			GiveSanityBoostSmall();
        			PlayMusic("21_puzzle_door.ogg", false, 0.8f, 0.5f, 9, false);
        		}

        	}
        	///////////////////////////////
        	//Cog wheels are NOT in place
        	else
        	{
        		//Add quest
        		AddQuest("21LeverNotWorking","21LeverNotWorking");

        		//Tell agrippa that we have tried a lever.
        		SetLocalVarInt("InteractedDoorLever", 1);

        		SetMessage("Ch02Level21", "LeverIsNotWorking", 0);

        		PlaySoundAtEntity("notworking", "21_lever_fail", "LeverSounds", 0.1f, false);
        	}
        }

        void TimerStopSounds(string asTimer)
        {
        	StopSound(asTimer, 3.0f);
        	StopSound(asTimer+2, 3.0f);
        	PlaySoundAtEntity("workins", "21_ignite.snt", "LeverSounds", 1.0f, false);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // FEED WATER LURKER AND GET SLIME SUBSTANCE
        ////////////////////////////////////////////

        //------------------------------------------

        //If rope is up and ready for interaction!
        void CollideRopeInteraction(string asParent, string asChild, int alState)
        {
        	//Set the variable that decides if the rope is up or not.
        	SetLocalVarInt("RopeIsUp", alState==1 ? 1 : 0);

        	AddDebugMessage("Rope up: "+alState, false);
        }

        //------------------------------------------

        //If rope is down at bottom, at the lurker
        void CollideRopeDown(string asParent, string asChild, int alState)
        {
        	if(alState!=1) return;

        	AddDebugMessage("Rope is down!", false);

        	if(GetLocalVarInt("MeatOnRope")!=1) return;

        	SetLocalVarInt("MeatOnRope",0);

        	PlayMusic("21_event_pit.ogg", false, 1.0f, 0.0f, 10, false);

        	//Remove normal meat for the eaten one.
        	RemoveAttachedPropFromProp("invisible_box_mass_2_1", "fresh_meat_onrope_1");

        	//Make it possible to pick up bone + have correct icon
        	SetEntityPlayerInteractCallback("invisible_box_mass_2_1", nameof(InteractSlimeBone), true);
        	SetEntityCustomFocusCrossHair("invisible_box_mass_2_1","Pick");

        	//need to turn off this area, else it is not possible to interact with entity on rope!
        	SetEntityActive("AreaRopeInteraction", false);

        	//Lever get stuck for a while, hinting that the water lurker is tugging on it.
        	SetWheelStuckState("crank_iron_2", 1, true);
        	SetEntityPlayerInteractCallback("crank_iron_2",nameof(InteractStuckWellLever), false);

        	//Stop any ambience.
        	StopWaterLurkerAmbience();

        	//Lurker screams
        	PlaySoundAtEntity("LurkerScream", "water_lurker_hunt_rev.snt", "AreaWellSounds", 0.1f, false);

        	//Look at well
        	StartPlayerLookAt("AreaWellLookAt", 10, 5, "");
        	PlayerReact(true,0.5f);

        	//Some timers for events
        	AddTimer("LurkerEat", 0.8f, nameof(TimerLurkerEat));
        	AddTimer("LurkerEat", 3.3f, nameof(TimerLurkerEat));
        	AddTimer("LurkerEat", 6.3f, nameof(TimerLurkerEat));
        	AddTimer("LurkerEat", 8.5f, nameof(TimerLurkerEat));

        	for(int i=1;i<=8; ++i) AddTimer("LurkerEat", 8.5f, nameof(TimerLurkerEatRopeImpulseOnly));

        	AddTimer("StopLookAtWheel", 1.5f,nameof(TimerStopLookAtWheel));
        	AddTimer("LurkerDoneEating", 10.0f,nameof(TimerLurkerDoneEating));
        }

        //------------------------------------------

        void InteractStuckWellLever(string asEntity)
        {
        	SetMessage("Ch02Level21", "WellLeverStuck", 0);
        }

        void TimerLurkerEat(string asTimer)
        {
        	//Grunt makes eating sound.
        	PlaySoundAtEntity("LurkerScream", "water_lurker_eat_rev.snt", "AreaWellSounds", 0.1f, false);

        	//Make the rope bounce a little
        	AddPropImpulse("invisible_box_mass_2_1", RandFloat(-2,2), RandFloat(1,3), RandFloat(-2,2), "World");
        }

        void TimerLurkerEatRopeImpulseOnly(string asTimer)
        {
        	AddPropImpulse("invisible_box_mass_2_1", RandFloat(-1,1), 0, RandFloat(-1,1), "World");
        }


        void TimerStopLookAtWheel(string asTimer)
        {
        	StopPlayerLookAt();
        }

        void TimerLurkerDoneEating(string asTimer)
        {
        	//Reset stuck callback
        	SetWheelStuckState("crank_iron_2", 0, true);
        	SetEntityPlayerInteractCallback("crank_iron_2","", false);

        	//Add the remains.
        	AddAttachedPropToProp("invisible_box_mass_2_1", "fresh_meat_remains_onrope_1", "fresh_meat_remains_onrope.ent", 0,0,0, 0,0,0);


        	//Okay to start ambience again
        	StartWaterLurkerAmbience();
        }

        //------------------------------------------

        void InteractSlimeBone(string asEntity)
        {
        	RemoveAttachedPropFromProp("invisible_box_mass_2_1", "fresh_meat_remains_onrope_1");
        	GiveItemFromFile("fresh_meat_remains_1", "fresh_meat_remains.ent");

        	//Increase count.
        	AddGlobalVarInt("IngredientCount",1);

        	//Need to do like this or else a message is not shown!
        	SetMessage("Ch02Level21", "PickUpFreshMeatRemains", 0);

        	//also need to play pucik sound
        	PlaySoundAtEntity("PickUpItem", "pick_meat.snt", "Player", 0.1f, false);

        	//Give a sanity boost.
        	GiveSanityBoostSmall();
        	PlayMusic("15_puzzle_hole.ogg", false, 0.8f, 0.5f, 9, false);
        }

        //------------------------------------------

        void UseMeatOnRope(string asItem, string asEntity)
        {
        	//////////////////////////////
        	//Rope is in right position
        	if(GetLocalVarInt("RopeIsUp")==1)
        	{
        		AddAttachedPropToProp("invisible_box_mass_2_1", "fresh_meat_onrope_1", "fresh_meat_onrope.ent", 0,0,0, 0,0,0);
        		RemoveItem(asItem);

        		SetLocalVarInt("MeatOnRope", 1);

        		PlaySoundAtEntity("attach", "21_meat_long.snt", "AreaRopeInteraction", 0.0f, false);
        	}
        	//////////////////////////////
        	//Rope is too low
        	else
        	{
        		SetMessage("Ch02Level21", "RopeTooLow", 0);
        	}
        }

        //------------------------------------------

        void CollideCrankSound(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("ropes", "06_rope_strain", "CrankSound_3", RandFloat(0.0f,1.0f), false);
        }

        ///////////////////////////////////////////
        // WATER LURKER EXTRA SOUNDS
        //  Ambient sounds and effects when things are thrown into well.
        ////////////////////////////////////////////

        //------------------------------------------

        void InitWaterLurkerSounds()
        {
        	StartWaterLurkerAmbience();

        	//add callbacks for any junk thrown down.
        	AddEntityCollideCallback("rock_debris*","AreaRopeDown", nameof(CollideJunkInWell), true, 1);
        	AddEntityCollideCallback("stone_small*","AreaRopeDown", nameof(CollideJunkInWell), true, 1);
        	AddEntityCollideCallback("bread_rotten*","AreaRopeDown", nameof(CollideJunkInWell), true, 1);
        	AddEntityCollideCallback("bag01*","AreaRopeDown", nameof(CollideJunkInWell), true, 1);
        	AddEntityCollideCallback("rotten_apple*","AreaRopeDown", nameof(CollideJunkInWell), true, 1);
        	AddEntityCollideCallback("wood_box*","AreaRopeDown", nameof(CollideJunkInWell), true, 1);
        }

        //------------------------------------------

        void CollideJunkInWell(string asParent, string asChild, int alState)
        {
        	//Wait 3 seconds to remove so a splash can be heard.
        	AddTimer(asParent, 3, nameof(TimerRemoveJunkInWell));
        }

        void TimerRemoveJunkInWell(string asTimer)
        {
        	AddDebugMessage("Removed: "+asTimer, false);

        	//Remove the entity and play a sound with a 1/3 chance
        	SetEntityActive(asTimer, false);
        	if(RandInt(0,2)==0)
        		PlaySoundAtEntity("LurkerScream", "water_lurker_attack_rev.snt", "AreaWellSoundsLow", 0.1f, false);
        }

        //------------------------------------------

        void StartWaterLurkerAmbience()
        {
        	AddTimer("WaterLurkerAmbience", RandFloat(1,5), nameof(TimerWaterLurkerAmbience));
        }

        void StopWaterLurkerAmbience()
        {
        	RemoveTimer("WaterLurkerAmbience");
        }

        //------------------------------------------

        void TimerWaterLurkerAmbience(string asTimer)
        {
        	AddDebugMessage("Water lurker amb sound.", false);

        	//Play sound in the lower part of the well.
        	PlaySoundAtEntity("LurkerScream", "water_lurker_idle_rev.snt", "AreaWellSoundsLow", 0.1f, false);

        	AddTimer(asTimer, RandFloat(5,16), nameof(TimerWaterLurkerAmbience));
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // PLAYER FALLS INTO WELL
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideWellKillPlayer(string asParent, string asChild, int alState)
        {
        	CheckPoint("checkFallWell", "PlayerStartArea_5", nameof(CheckPointFallWell), "Hints", "DeathFall_21_TortureNave");

        	PlaySoundAtEntity("fallaaaaaah", "11_fall", "Player", 0, false);
        	PlaySoundAtEntity("fallaaaaaah2", "scare_male_terrified5", "Player", 0, false);

        	FadeOut(0.5f);

        	AddTimer("death1", 0.5f, nameof(TimerFallDeath));
        	AddTimer("death2", 1, nameof(TimerFallDeath));

        	AddTimer("meat", 1.6f, nameof(TimerFallDeath));
        	AddTimer("meatb", 1.8f, nameof(TimerFallDeath));
        	AddTimer("meatl", 1.9f, nameof(TimerFallDeath));
        	AddTimer("meat", 1.0f, nameof(TimerFallDeath));
        	AddTimer("meatb", 1.2f, nameof(TimerFallDeath));
        	AddTimer("meatl", 1.4f, nameof(TimerFallDeath));
        	AddTimer("meat", 2.1f, nameof(TimerFallDeath));
        	AddTimer("meatb", 2.3f, nameof(TimerFallDeath));
        	AddTimer("meatl", 2.4f, nameof(TimerFallDeath));
        }

        void TimerFallDeath(string asTimer)
        {
        	if(asTimer == "meat"){
        		PlayGuiSound("21_meat.snt",RandFloat(0.4f,0.9f));
        		return;
        	}
        	if(asTimer == "meatb"){
        		PlayGuiSound("21_meat_snap.snt",RandFloat(0.4f,0.9f));
        		return;
        	}
        	if(asTimer == "meatl"){
        		PlayGuiSound("21_meat_long.snt",RandFloat(0.4f,0.9f));;
        		return;
        	}

        	//If first timer, just play a bump
        	if(asTimer == "death1"){
        		PlaySoundAtEntity("bump1", "player_bodyfall", "Player", 0, false);
        		return;
        	}

        	//Kill player without making a sound
        	DisableDeathStartSound();
        	AddPlayerHealth(-200);

        	//Lurker attacks player
        	PlaySoundAtEntity("LurkerScream", "water_lurker_attack_rev.snt", "AreaRopeDown", 0.1f, false);

        	//Players gets some pain
        	PlaySoundAtEntity("pain", "player_falldamage_max", "Player", 0, false);
        	PlaySoundAtEntity("bump2", "player_bodyfall", "Player", 0, false);
        }

        //------------------------------------------

        void CheckPointFallWell(string asName, int alCount)
        {
        	//Do nothing??
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // ALEXANDER VOICE
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideStartAlexanderVoice(string asParent, string asChild, int alState)
        {
        	AddEffectVoice("CH02L21_Alexander_01", "", "Voice", "CH02L21_Alexander_01", false, "", 0.0f, 0.0f);

        	AddTimer("AlexVoiceRumble", 3.0f, nameof(TimerAlexVoiceRumble));
        }

        void TimerAlexVoiceRumble(string asTimer)
        {
        	PlayerReact(true,0.8f);

        	StartScreenShake(0.02f, 6, 2, 2);

        	PlayGuiSound("21_alex_low_freq_rumble", 0.7f);
        	AddTimer("AlexVoiceRumbleFadeOut", 6.0f, nameof(TimerTimerAlexVoiceRumbleFadeOut));
        }

        void TimerTimerAlexVoiceRumbleFadeOut(string asTimer)
        {
        	StopSound("AlexanderRumbleLoop", 2);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // PICK UP AGRIPPA CHANNELS WEYER NOTE
        ////////////////////////////////////////////

        //------------------------------------------

        void PickupChannelWeyerNote(string asEntity, string asType)
        {
        	PlayMusic("01_puzzle_passage.ogg", false, 1.0f, 0, 10, false);

        	SetLocalVarInt("PickedWeyerNote", 1);
        	SetGlobalVarInt("WeyerNoteIsPicked", 1);

        	AddQuest("21FoundTonicNote", "21FoundTonicNote");
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // ALL ORBS HOWL EVENT
        //  When player enters map with all orbs, do some howling!
        ////////////////////////////////////////////

        //------------------------------------------

        void TimerAllOrbsHowlEvent(string asTimer)
        {
        	//Shake screen and make it blurry
        	StartScreenShake(0.03f, 0.2f, 5.0f, 0.2f);

        	SetRadialBlurStartDist(0.2f);
        	FadeRadialBlurTo(0.09f, 0.015f);

        	PlaySoundAtEntity("guard1", "guardian_distant2", "Player", 0, false);

        	GiveSanityDamage(15.0f, false);

        	AddTimer("AllOrbsHowlEventStop",5,nameof(TimerAllOrbsHowlEventStop));
        }

        void TimerAllOrbsHowlEventStop(string asTimer)
        {
        	FadeRadialBlurTo(0.0f, 0.02f);
        }


        //------------------------------------------

        ///////////////////////////////////////////
        // PLAYER FALLS INTO FIRST WELL
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideFirstWellKillPlayer(string asParent, string asChild, int alState)
        {
        	CheckPoint("checkFallFirstWell", "PlayerStartArea_6", nameof(CheckPointFallFirstWell), "Hints", "DeathFall_21_TortureNave");

        	PlaySoundAtEntity("fallaaaaaah", "11_fall", "Player", 0, false);
        	PlaySoundAtEntity("fallaaaaaah2", "scare_male_terrified5", "Player", 0, false);

        	FadeOut(0.5f);

        	AddTimer("death1", 0.5f, nameof(TimerFirstWellFallDeath));
        	AddTimer("death2", 1, nameof(TimerFirstWellFallDeath));
        }

        //------------------------------------------

        void TimerFirstWellFallDeath(string asTimer)
        {
        	//If first timer, just play a bump
        	if(asTimer == "death1"){
        		PlaySoundAtEntity("bump1", "player_bodyfall", "Player", 0, false);
        		return;
        	}

        	//Kill player without making a sound
        	DisableDeathStartSound();
        	AddPlayerHealth(-200);

        	//Players gets some pain
        	PlaySoundAtEntity("pain", "player_falldamage_max", "Player", 0, false);
        	PlaySoundAtEntity("bump2", "player_bodyfall", "Player", 0, false);

        }


        //------------------------------------------

        //When player falls down first well, add some extra events to make a creepy atmosphere.
        void CheckPointFallFirstWell(string asName, int alCount)
        {
        	if(alCount != 0) return; //Only do it the first time!

        	SetSwingDoorLocked("cellar_wood01_2", true, false);
        	SetEntityPlayerInteractCallback("cellar_wood01_2", nameof(InteractCellarWood0102), true);
        }

        void TimerFallFirstWellDeath(string asTimer)
        {
        	///////////////
        	//Guardian scream
        	if(asTimer == "falldeath1")
        	{
        		StartScreenShake(0.03f, 0.2f, 5.0f, 0.2f);

        		PlayerReact(true, 0.8f);

        		SetRadialBlurStartDist(0.2f);
        		FadeRadialBlurTo(0.09f, 0.015f);

        		PlaySoundAtEntity("guard1", "guardian_distant2", "Player", 0, false);
        	}
        	///////////////
        	//Stop Effects
        	else if(asTimer == "falldeath2")
        	{
        		FadeRadialBlurTo(0.0f, 0.015f);
        	}
        	///////////////
        	//Unlock door
        	else if(asTimer == "falldeath3")
        	{
        		PlayerReact(false, 0.8f);
        		SetSwingDoorLocked("cellar_wood01_2", false, false);
        		SetPropActiveAndFade("Slime_Door", false, 4.0f);
        		PlaySoundAtEntity("dissolve", "gameplay_acid_web.snt", "cellar_wood01_2", 3.0f, false);
        		PlayGuiSound("guardian_idle.snt", 0.8f);
        		StartScreenShake(0.005f, 0.0f, 1.0f, 2.0f);
        	}
        }

        void InteractCellarWood0102(string asEntity)
        {
        	SetPropActiveAndFade("slime_*", true, 2.0f);

        	AddTimer("falldeath1", 0.0f, nameof(TimerFallFirstWellDeath));
        	AddTimer("falldeath2", 5.0f, nameof(TimerFallFirstWellDeath));
        	AddTimer("falldeath3", 8.0f, nameof(TimerFallFirstWellDeath));

        	PlaySoundAtEntity("slime1", "slime_loop.snt", "SlimeArea_1", 2.0f, true);
        	PlaySoundAtEntity("slime2", "slime_loop.snt", "SlimeArea_2", 3.0f, true);
        	PlaySoundAtEntity("slime3", "slime_loop.snt", "SlimeArea_3", 4.0f, true);

        	SetEntityActive("SlimeDamageArea_1", true);
        	SetEntityActive("SlimeDamageArea_2", true);

        	CreateParticleSystemAtEntity("slimeps1", "ps_slime_fog.ps", "SlimeArea_1", true);
        	CreateParticleSystemAtEntity("slimeps1", "ps_slime_fog.ps", "SlimeArea_2", true);
        	CreateParticleSystemAtEntity("slimeps1", "ps_slime_fog.ps", "SlimeArea_3", true);
        }
        //------------------------------------------


        ///////////////////////////////////////////
        // JAMMED DOOR
        ////////////////////////////////////////////

        //------------------------------------------

        void InteractJammedDoor(string asEntity)
        {
        	SetMessage("Ch02Level21", "DoorJammed", 0);

        	AddTimer(asEntity, 1.0f, nameof(TimerJammedDoor));
        }
        void TimerJammedDoor(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractJammedDoor), true);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // FIRST WELL INTERACTION
        ////////////////////////////////////////////

        //------------------------------------------

        void InteractFirstWellHole(string asEntity)
        {
        	SetMessage("Ch02Level21", "CanNotClimbFirstWell", 0);
        	PlayGuiSound("18_touch_bridge.snt", RandFloat(0.4f, 1.0f));
        	AddTimer(asEntity, 1.0f, nameof(TimerFirstWellHole));
        }
        void TimerFirstWellHole(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractFirstWellHole), true);
        }
        //------------------------------------------



        ////////////MISC STUF//////////////

        //Some chain sounds if walking through them
        void CollideAreaChains(string asParent, string asChild, int alState)
        {
        	PlayGuiSound("general_chain_rattle_single.snt", RandFloat(0.0f, 0.7f));
        }


        //A girl cries in a small cell, stops when door opens.
        void InteractGirlCellDoor(string asEntity)
        {
        	StopSound("GirlCry", 0);
        	PlayerReact(true,1.0f);
        	PlaySoundAtEntity("ghost", "03_in_a_bottle.snt", "AreaGirl", 0.0f, false);
        	CreateParticleSystemAtEntity("ghostp", "ps_ghost_release.ps", "AreaGirl", false);
        }


        //Pick Daniels Diary
        void EntityCallDiary(string asEntity, string type)
        {
        	PlayMusic("21_paper_daniel01.ogg", false, 0.5f, 0.0f, 10, false);
        }

        //Misc messages for using potion ingredient on agrippa
        void UseStuffOnAgrippa(string asItem, string asEntity)
        {
        	if(asItem == "fresh_meat_2")
        	{
        		if(GetLocalVarInt("PickedWeyerNote") == 0)
        			SetMessage("Ch02Level21", "AgrippaMeatNoNote", 0);
        		else
        			SetMessage("Ch02Level21", "AgrippaMeatNote", 0);
        	}

        	if(asItem == "fresh_meat_remains_1")
        	{
        		if(GetLocalVarInt("PickedWeyerNote") == 0)
        			SetMessage("Ch02Level21", "AgrippaBoneNoNote", 0);
        		else
        			SetMessage("Ch02Level21", "AgrippaMeatNote", 0);
        	}

        	if(asItem == "poison_gland")
        		SetMessage("Ch03Level26", "UseGlandOnAgrippa", 0);

        	if(asItem == "glass_container_blood")
        		SetMessage("Ch03Level26", "UseBloodOnAgrippa", 0);
        }

        //Player Reactions
        void PlayerReact(bool bDamage, float fTime)
        {
        	if(bDamage)
        		AddTimer("sanity", fTime, nameof(TimerPlayerReactions));
        	else
        		AddTimer("scare", fTime, nameof(TimerPlayerReactions));

        	AddTimer("breath", fTime+2, nameof(TimerPlayerReactions));
        	AddTimer("breathl", fTime+2, nameof(TimerPlayerReactions));

        }
        void TimerPlayerReactions(string asTimer)
        {
        	if(asTimer == "sanity"){
        		GiveSanityDamage(15.0f, true);
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


        //Collide with spikes by agrippa
        void CollideAreaSpikes(string asParent, string asChild, int alState)
        {
        	if(alState == 1){
        		if(GetLocalVarInt("GotSpiked") == 1) return;
        		GivePlayerDamage(5.0f, "BloodSplat", false, false);
        		PlayGuiSound("21_meat.snt", 1);
        		SetLocalVarInt("GotSpiked", 1);
        	}

        	if(alState == -1){
        		SetLocalVarInt("GotSpiked", 0);
        	}
        }


        //Interact on door for prisoner Vision
        void InteractVisionDoor(string asEntity)
        {
        	SetEntityActive("FlashbackArea_3", true);
        	SetEntityActive("FlashbackArea_3_C", true);
        }

        void TimerRandSound(string asTimer)
        {
        	PlaySoundAtEntity("ambrand"+RandInt(1,4), "03_rock_move.snt", "ScriptArea_"+RandInt(1,4), RandFloat(0.0f, 2.0f), false);

        	AddTimer(asTimer, RandFloat(15.0f, 30.0f), nameof(TimerRandSound));
        }
        void TimerRandSound02(string asTimer)
        {
        	PlaySoundAtEntity("ambrand"+RandInt(1,4), "15_man01_whimp.snt", "ScriptArea_"+RandInt(1,4), RandFloat(0.0f, 2.0f), false);

        	AddTimer(asTimer, RandFloat(15.0f, 30.0f), nameof(TimerRandSound02));
        }


        //////////////////////////
        //Cell event on low sanity
        //----------------------------------
        //Check for when to do the cell event

        //Entering waiting room from agrippa room or level 22
        void CollideAreaCellEvent(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("CellEvent") == 1) return;

        	if(GetPlayerSanity() > GetLocalVarInt("SanityForCellEvent")){
        		AddDebugMessage("No Cell event as sanity in good shape", false);
        		return;
        	}

        	SetLocalVarInt("CellEvent", 1);

        	AddTimer(nameof(TimerCellEvent), 0.1f, nameof(TimerCellEvent));
        	AddDebugMessage("Do Cell Event, sanity below "+GetLocalVarInt("SanityForCellEvent"), false);
        }

        //Vision(s) in waitroom begins - no cell event
        void CollideFlashbackArea_23(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Flashback begin, block cell event!", false);
        	SetEntityActive("AreaCellEvent", false);
        }

        //Vision(s) in waitroom over - allow cell event
        void WaitRoomOver()
        {
        	AddDebugMessage("Flashback Over!", false);

        	AddTimer("AllowCellEvent", 10.0f, nameof(TimerAllowCellEvent));
        }

        //When a vision is over, after 10 second allow for the cell event again
        void TimerAllowCellEvent(string asTimer)
        {
        	SetEntityActive("AreaCellEvent", true);
        }

        //-----------------------------------------
        //Cell event functions

        void TimerCellEvent(string asTimer)
        {
        	string sEvent = asTimer;	//Do not edit, sets timer loop function name.
        	AddLocalVarInt(sEvent, 1);	//Do not edit, steps through timer loop events.
        	bool bPauseAtStep = false;	//Do not edit, to pause or end the timer loop at specified step.

        	float fEventSpeed = RandFloat(0.7f, 1.4f);	//Set to preferred default time for the timer loop.

        	switch(GetLocalVarInt(sEvent)){
        		//////////
        		//Begin event with a bang!
        		case 1:
        			AddDebugMessage("Step 1", false);

        			PlayMusic("00_event_gallery.ogg", false, 1.0f, 0.1f, 10, false);

        			StartScreenShake(0.1f, 0, 0.2f, 0.8f);

        			PlayGuiSound("scare_wall_stomp.snt", 0.8f);
        			PlaySoundAtEntity("scream", "21_intro_scream", "Player", 1, false);

        			FadeImageTrailTo(2,1);

        			SetLampLit("torch_static01_40", false, true);
        			SetLampLit("torch_static01_44", false, true);
        			SetLampLit("torch_static01_22", false, true);
        			SetLampLit("torch_static01_43", false, true);
        			SetLampLit("torch_static01_47", false, true);
        			SetLampLit("chandelier_simple_2", false, true);
        			SetLampLit("hanging_lantern_ceiling_8", false, true);
        			SetLampLit("hanging_lantern_ceiling_5", false, true);

        			FadeLightTo("PointLight_83", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("PointLight_77", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("SpotLight_6", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			//FadeLightTo("PointLight_8", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("SpotLight_9", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("PointLight_78", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("PointLight_82", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("PointLight_12", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("PointLight_94", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("PointLight_87", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("PointLight_90", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("PointLight_91", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        			FadeLightTo("PointLight_92", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);

        			fEventSpeed = 1.0f;
        		break;

        		//////////
        		//Stop intro scream
        		case 3:
        			BangOnCellDoor();
        			StopSound("scream", 1.0f);
        		break;

        		//////////
        		//Whisper fades in
        		case 5:
        			BangOnCellDoor();
        			PlaySoundAtEntity("whisper1", "insanity_whisper.snt", "Player", 0.0f, false);
        		break;

        		//////////
        		//A second whisper fades in
        		case 7:
        			BangOnCellDoor();
        			PlaySoundAtEntity("whisper2", "insanity_whisper.snt", "Player", 0.0f, false);
        		break;

        		//////////
        		//Event ends abruptly, did it happen or not?
        		case 12:
        			StartScreenShake(0.3f, 0.0f, 0.1f, 0.1f);
        			FadeImageTrailTo(0.0f, 1);

        			PlayGuiSound("sanity_flick", 0.4f);

        			StopMusic(0.5f, 10);

        			StopSound("whisper1", 0.5f);
        			StopSound("whisper2", 0.5f);
        			StopSound("chain", 5.0f); StopSound("chain", 5.0f); StopSound("chain", 5.0f); StopSound("chain", 5.0f);
        			StopSound("chain", 5.0f); StopSound("chain", 5.0f); StopSound("chain", 5.0f); StopSound("chain", 5.0f);
        			StopSound("scream", 1.0f); StopSound("scream", 1.0f); StopSound("scream", 1.0f); StopSound("scream", 1.0f);
        			StopSound("scream", 1.0f); StopSound("scream", 1.0f); StopSound("scream", 1.0f); StopSound("scream", 1.0f);

        			PlayerReact(false,0.5f);

        			bPauseAtStep = true;
        		break;

        		//////////
        		//Bang door on empty event steps
        		default:
        			BangOnCellDoor();
        		break;
        	}

        	if(!bPauseAtStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }

        //A general random effect for banging on doors.
        void BangOnCellDoor()
        {
        	int iDoor = RandInt(1, 8);

        	if(iDoor == 1 || iDoor == 4 || iDoor == 5 || iDoor == 8)
        		PlaySoundAtEntity("chain", "general_chain_rattle.snt", "prison_"+iDoor, RandFloat(0.0f, 1.0f), false);

        	PlaySoundAtEntity("scream", "21_screams.snt", "prison_"+iDoor, RandFloat(0.0f, 1.0f), false);

        	PlaySoundAtEntity("bang"+iDoor, "21_bang_door.snt", "prison_"+iDoor, RandFloat(0.0f, 1.0f), false);

        	//CreateParticleSystemAtEntity("psbang"+iDoor, "ps_dust_impact_vert.ps", "prison_"+iDoor, false);

        	FadeLightTo("light_prison_"+iDoor, 0.9f, 0.15f, 0.05f, 1, -1, 0.2f);

        	AddPropImpulse("prison_"+iDoor, RandFloat(-8.0f, 8.0f), 0, 0, "World");

        	StartScreenShake(0.02f, 0, 0.2f, 0.3f);

        	AddTimer("light_prison_"+iDoor, 0.6f, nameof(TimerEventCellLight));

        	AddDebugMessage("Bang on Door "+iDoor, false);
        }

        //Fade out light in cell
        void TimerEventCellLight(string asTimer)
        {
        	FadeLightTo(asTimer, 0, 0, 0, 0, -1, 0.4f);
        }


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("Nave");

        	// Clear previously saved maps!
        	ClearSavedMaps();

        	////////////////////
        	// Hub start sanity boost
        	GiveSanityBoost();

        	//////////////////////
        	//Load Screen Setup
        	SetupLoadScreen("LoadingText", "Ch02_Diary07_", 6, "game_loading_strappado.jpg");

        	///////////////////////////
        	// Init entities
        	InitCeilingCogWheels();
        	InitWaterLurkerSounds();


        	///////////////////////////
        	// Variables
        	SetLocalVarInt("AgrippaAwake",0);		//If agrippa is awake yet.
        	SetLocalVarInt("AgrippaVoiceMachineOn",0); 	//If the machine that lets Agrippa talk is on.
        	SetLocalVarInt("PlayerInAgrippaTalkRange",0);	//If the player is in Agrippa talk range

        	///////////////////////////
        	// Connections
        	InteractConnectPropWithRope("WellRope","crank_iron_2", "RopeArea_1", false, 3,5,5, false, 0);

        	///////////////////////////
        	// UseItem
        	AddUseItemCallback("MeatOnRope", "fresh_meat_2", "AreaRopeInteraction", nameof(UseMeatOnRope), false);


        	AddUseItemCallback("MeatOnAgrippa", "fresh_meat_2", "AreaAgrippa_1", nameof(UseStuffOnAgrippa), false);
        	AddUseItemCallback("BoneOnAgrippa", "fresh_meat_remains_1", "AreaAgrippa_1", nameof(UseStuffOnAgrippa), false);

        	AddUseItemCallback("GlandOnAgrippa","poison_gland", "AreaAgrippa_1", nameof(UseStuffOnAgrippa), false);
        	AddUseItemCallback("BloodOnAgrippa","glass_container_blood", "AreaAgrippa_1", nameof(UseStuffOnAgrippa), false);

        	///////////////////////////
        	// Collide Callbacks
        	AddEntityCollideCallback("Player", "AreaWakeUpAgrippa", nameof(CollideWakeUpAgrippa), false, 1);

        	AddEntityCollideCallback("Player", "AreaAgrippaStartTalk", nameof(CollideAgrippaStartTalk), false, 1);
        	AddEntityCollideCallback("Player", "AreaAgrippaStopTalk", nameof(CollideAgrippaStopTalk), false, -1);

        	AddEntityCollideCallback("Player", "AreaWellKillPlayer", nameof(CollideWellKillPlayer), false, 1);

        	AddEntityCollideCallback("Player", "AreaStartAlexanderVoice", nameof(CollideStartAlexanderVoice), true, 1);

        	AddEntityCollideCallback("invisible_box_mass_2_1", "AreaRopeInteraction",nameof(CollideRopeInteraction), false, 0);
        	AddEntityCollideCallback("invisible_box_mass_2_1", "AreaRopeDown",nameof(CollideRopeDown), false, 0);

        	AddEntityCollideCallback("Player", "AreaFirstWellKillPlayer", nameof(CollideFirstWellKillPlayer), false, 1);

        	AddEntityCollideCallback("crank_iron_2", "CrankSound_2", nameof(CollideCrankSound), false, 1);
        	AddEntityCollideCallback("crank_iron_2", "CrankSound_1", nameof(CollideCrankSound), false, 1);

        	AddEntityCollideCallback("Player", "AreaChains", nameof(CollideAreaChains), false, 1);

        	AddEntityCollideCallback("Player", "AreaAgrippaRandom", nameof(CollideAgrippaRandomArea), false, 0);

        	AddEntityCollideCallback("Player", "AreaSpikes", nameof(CollideAreaSpikes), false, 0);
        	AddEntityCollideCallback("Player", "AreaSpikes_1", nameof(CollideAreaSpikes), false, 0);
        	AddEntityCollideCallback("Player", "AreaSpikes_2", nameof(CollideAreaSpikes), false, 0);

        	AddEntityCollideCallback("Player", "AreaCellEvent", nameof(CollideAreaCellEvent), false, 1);
        	AddEntityCollideCallback("Player", "FlashbackArea_2_C", nameof(CollideFlashbackArea_23), true, 1);
        	AddEntityCollideCallback("Player", "FlashbackArea_3_C", nameof(CollideFlashbackArea_23), true, 1);

        	AddTimer(nameof(TimerRandSound), RandFloat(1.0f, 4.0f), nameof(TimerRandSound));
        	AddTimer(nameof(TimerRandSound02), RandFloat(5.0f, 8.0f), nameof(TimerRandSound02));

        	SetLocalVarInt("SanityForCellEvent", 70);	//Sanity below this value triggers an event in waiting room

        	///////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		//SetLocalVarInt("SanityForCellEvent", 100);

        		GiveItemFromFile("lantern", "lantern.ent");
        		for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

        		GiveItemFromFile("poison_gland", "poison_gland.ent");
        		GiveItem("glass_container_blood", "Puzzle", "glass_container_blood", "glass_container_blood.tga", 0);


        		GiveItemFromFile("fresh_meat_2", "fresh_meat.ent");
        		//SetGlobalVarInt("PlayerHasVisitedLevel22",1);
        		//SetGlobalVarInt("ChoirOrbCount",3);
        		//SetGlobalVarInt("TranseptOrbCount",3);
        		//SetEntityActive("AreaStartAlexanderVoice", true);
        	}
        }

        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	/////////////////////////////////
        	// Play music!
        	PlayMusic("21_amb.ogg", true, 0.8f, 5, 0, true);

        	PreloadSound("21_loop"); PreloadSound("agrippa_wake"); PreloadSound("21_cm_reverse_low"); PreloadSound("21_channeling_machine");
        	PreloadSound("21_cog_slide"); PreloadSound("21_cog_wheel_big"); PreloadSound("21_lever_success"); PreloadSound("21_ignite");
        	PreloadSound("water_lurker_eat_rev"); PreloadSound("water_lurker_attack_rev"); PreloadSound("water_lurker_idle_rev"); PreloadSound("guardian_distant2");
        	PreloadSound("player_bodyfall"); PreloadSound("water_lurker_hunt_rev"); PreloadSound("21_meat_long"); PreloadSound("06_rope_strain");
        	PreloadSound("21_bang_door"); PreloadSound("general_chain_rattle"); PreloadSound("21_screams"); PreloadSound("insanity_whisper");
        	PreloadSound("slime_loop"); PreloadSound("03_rock_move"); PreloadSound("15_man01_whimp"); PreloadSound("21_intro_scream");
        	PreloadSound("general_chain_rattle_single"); PreloadSound("21_meat"); PreloadSound("21_meat_long"); PreloadSound("21_meat_snap");
        	PreloadSound("21_alex_low_freq_rumble"); PreloadSound("guardian_idle"); PreloadSound("react_breath"); PreloadSound("react_scare");
        	PreloadSound("scare_wall_stomp"); PreloadSound("sanity_flick");

        	/////////////////////////////////
        	// Check if WaitRoom flashback should be enabled!
        	if(GetGlobalVarInt("PlayerHasVisitedLevel22")==1 && GetLocalVarInt("WaitRoomFlashBackActivated")==0)
        	{
        		SetEntityActive("FlashbackArea_2", true);
        		SetEntityActive("FlashbackArea_2_C", true);
        		SetLocalVarInt("WaitRoomFlashBackActivated",1);
        	}

        	/////////////////////////////////
        	// Check if Player has all Orbs, then to howl event
        	if(GetGlobalVarInt("ChoirOrbCount")==3 && GetGlobalVarInt("TranseptOrbCount")==3 &&
        	   GetLocalVarInt("AllOrbsHowlEventActivated")==0)
        	{
        		SetLocalVarInt("AllOrbsHowlEventActivated",1);

        		AddTimer("AllOrbsHowlEvent", 3.0f, nameof(TimerAllOrbsHowlEvent));
        	}


        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {

        }
    }
}
