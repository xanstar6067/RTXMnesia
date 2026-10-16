using RTXMnesia.ScriptApi;
using static RTXMnesia.Levels.Justine.JustineDungeon.eEnemyState;
using static RTXMnesia.Levels.Justine.JustineDungeon.ePoundArea;
using static RTXMnesia.Levels.Justine.JustineDungeon.ePoundIntensity;
using static RTXMnesia.Levels.Justine.JustineDungeon.eDoorState;

namespace RTXMnesia.Levels.Justine
{
    [HplMapScript("maps/ptest/03_dungeon.map")]
    public class JustineDungeon : HplScript
    {
        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // CONTROL VARIABLES
        //////////////////////////////////////////////////////////
        //------------------------------------------------------------------------------------------

        float gfTimeToActivateEnemyAfterEnterSlideDoor1Area = 2.0f;
        float gfTimeToActivateEnemyAfterFixingLever = 2.0f;
        float gfTimeToStartPoundingAfterSlideDoor1Closed = 15.0f;
        float gfTimeToStartPoundingAfterSlideDoor2Closed = 1.0f;
        float gfTimeToActivateEnemyAfterFixingMachine = 7.0f;
        float gfTimeToActivateEnemyAtSlideDoor2WhenInteractingValve = 10.0f;
        float gfTimeToActivateEnemyAtSlideDoor2WhenDoorHalfUp = 5.0f;
        float gfTimeToActivateEnemyAtSlideDoor3 = 2.0f;

        //------------------------------------------------------------------------------------------


        //////////////////////////////////////////////////////////
        // ENEMY EFFECTS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void DoEnemyEffects(int alArea, string sEnemy)
        {
        	SetEnemyDisableTriggers(sEnemy, true);	//Else the enemy will hear door breaks.
        	
        	StartScreenShake(0.05f, 0.5f, 0.25f, 0.5f);
        	SetRadialBlurStartDist(0.3f);
        	FadeRadialBlurTo(0.1f, 0.5f);
        	
        	PlaySoundAtEntity("break_doors", "break_door_suitor.snt", "wooden_slide_door_"+alArea, 0, false);
        	PlaySoundAtEntity("suitor_noise", "suitor/enabled.snt", "wooden_slide_door_"+alArea, 0, false);
        	
        	AddTimer(sEnemy, 2.0f, nameof(TimerStopEnemySpawnEffect));
        }

        //------------------------------------------------------------------------------------------

        void TimerStopEnemySpawnEffect(string asTimer)
        {
        	SetEnemyDisableTriggers(asTimer, false);
        	
        	FadeRadialBlurTo(0.0f, 0.1f);
        }


        //////////////////////////////////////////////////////////
        // ENEMY HELPERS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        public enum eEnemyState
        {
        	eEnemyState_Hindered,
        	eEnemyState_Hunting,
        };

        //------------------------------------------------------------------------------------------

        //////////////////////////////////
        //This is called each time a new enemy is added to the rooms.
        void DoEnemyStep()
        {
        	int lCurrentStep = GetLocalVarInt("EnemyStep");
        	
        	// If enemy is not behind door or is eating prisoner, do not continue!
        	if(GetLocalVarInt("EnemyState")!=(int)eEnemyState_Hindered || GetLocalVarInt("FelixIsEaten")==1)
        		return;
        		
        	StopPounding();
        	
        	AddLocalVarInt("EnemyStep", 1);
        	int lStep = GetLocalVarInt("EnemyStep");
        	
        	string sEnemy = "enemy_suitor_"+lStep;
        	ActivateEnemy(sEnemy);
        	
        	// Skip east corridor area, as no door is to be broken there
        	if(lStep!=0)
        	{
        		string sDoorName = "wooden_slide_door_" + lStep;
        		string sBrokenDoorName = "wooden_slide_door_broken_" + lStep;
        		
        		//AddDebugMessage("Breaking door " + sDoorName, false);
        		
        		SetEntityActive(sDoorName, false);
        		SetEntityActive(sBrokenDoorName, true);
        		
        		AddTimer(sBrokenDoorName, 1.0f, nameof(Timer_BreakDoor));
        		
        		//Do all the break effects
        		DoEnemyEffects(lStep,sEnemy);
        		
        		//Play the most frenzy music!
        		PlayEnemyMusic(2);
        	}
        	
        	
        	if(GetLocalVarInt("ChaseStarted")==1)
        		ChasePlayer();
        }

        void Timer_BreakDoor(string asTimer)
        {
        	SetPropHealth(asTimer, -100.0f);
        	SetLocalVarInt(asTimer+"_state", (int)eDoorState_Destroyed);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////////
        // Determines if the enemy got behind a door we are closing.
        void SetUpEnemyHindering(string asDoorName)
        {
        	bool bEnemyBehindDoor = false;
        	int lDoorIndex = -1;
        	for(int i=0; i<4; ++i)
        	{
        		string sAreaName = "Area_Room_"+i;
        		if(sAreaName==GetLocalVarString("CurrentEnemyLocation"))
        			bEnemyBehindDoor = true;
        			
        		string sDoorName = "wooden_slide_door_"+i;
        		if(sDoorName==asDoorName)
        		{
        			lDoorIndex = i;
        			break;
        		}
        	}
        	
        	/////////////////////////////////////////
        	// The player us now behind a door, set up accordingly.
        	if(bEnemyBehindDoor)
        	{
        		SetLocalVarInt("EnemyStep", lDoorIndex-1);
        		SetLocalVarInt("EnemyState", (int)eEnemyState_Hindered);
        		SetEntityActive(GetLocalVarString("ActiveEnemy"), false);
        		//SetEnemyDisabled(GetLocalVarString("ActiveEnemy"), true);
        		
        		AddDebugMessage("Enemy ("+GetLocalVarString("ActiveEnemy")+") hindered by door " + lDoorIndex, false);
        	}
        	else
        	{
        		AddDebugMessage("Enemy ("+GetLocalVarString("ActiveEnemy")+") NOT hindered by door " + lDoorIndex, false);
        	}
        }

        //------------------------------------------------------------------------------------------

        ///////////////////////////////////////
        // Activates an instance of the enemy
        void ActivateEnemy(string asEnemy)
        {
        	// If asEnemy is current enemy, do nothing
        	string sActiveEnemy = GetLocalVarString("ActiveEnemy");
        	if(sActiveEnemy==asEnemy)
        		return;
        	
        	string sOldActiveEnemy = sActiveEnemy;
        	
        	// Set up new active enemy
        	SetLocalVarString("ActiveEnemy", asEnemy);
        	SetLocalVarInt("EnemyState", (int)eEnemyState_Hunting);
        	
        	SetEntityActive(asEnemy, true);
        	UpdatePatrolNodes(true);
        	
        	// Deactivate old enemy if any.
        	if(sOldActiveEnemy!="")
        	{
        		//SetEnemyDisabled(sOldActiveEnemy, true);
        		SetEntityActive(sOldActiveEnemy, false);
        	}
        }

        // Deactivates current active enemy
        void DisableEnemy()
        {
        	ActivateEnemy("");
        }

        //------------------------------------------------------------------------------------------

        bool IsEnemyActive()
        {
        	string sActiveEnemy = GetLocalVarString("ActiveEnemy");

        	return sActiveEnemy!="" && GetEntityExists(sActiveEnemy);
        }

        //------------------------------------------------------------------------------------------

        string[][] gvPatrolNodes = new string[][]
        {
        	new string[] { "PathNodeArea_36", "PathNodeArea_42", "PathNodeArea_1", ""},
        	new string[] { "PathNodeArea_101", "PathNodeArea_100", "PathNodeArea_90", ""},
        	new string[] { "PathNodeArea_181", "PathNodeArea_185", "" },
        	new string[] { "" }
        };

        ///////////////////////////////////////////////////////
        // Sets up patrol route for active enemy depending on where the player is
        void UpdatePatrolNodes(bool abForce)
        {
        	if(IsEnemyActive()==false)
        		return;
        		
        	string sEnemy = GetLocalVarString("ActiveEnemy");

        	string sCurrentPlayerLocation = GetLocalVarString("CurrentPlayerLocation");
        	string sCurrentEnemyLocation = GetLocalVarString("CurrentEnemyLocation");
        	string sCurrentEnemyPatrolLocation = GetLocalVarString("CurrentEnemyPatrolLocation");
        	
        	//AddDebugMessage("Enemy Location " + sCurrentEnemyLocation + " - Player Location " + sCurrentPlayerLocation, false);
        	if(sCurrentEnemyPatrolLocation==sCurrentPlayerLocation && abForce==false)
        		return;
        		
        	ClearEnemyPatrolNodes(sEnemy);
        	SetLocalVarString("CurrentEnemyPatrolLocation", sCurrentPlayerLocation);
        	
        	int lRoomIndex = 0;
        	for(int i=0; i<4; ++i)
        	{
        		if(sCurrentPlayerLocation=="Area_Room_" + (i+1))
        		{
        			lRoomIndex = i;
        			break;
        		}
        	}
        	
        		
        	string sNode;
        	int j=0;
        	do
        	{
        		sNode = gvPatrolNodes[lRoomIndex][j];
        		
        		if(sNode!="")
        		{
        			//AddDebugMessage("Adding node " + sNode, false);
        			AddEnemyPatrolNode(sEnemy, sNode, 2.0f, "");
        		}
        		
        		++j;		
        	}
        	while(sNode!="");	
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////
        // Starts showing player position to enemy in a timer loop
        void ChasePlayer()
        {
        	RemoveTimer("chaseplayer");
        	
        	AddTimer("chaseplayer", 1.0f, nameof(Timer_KeepEnemyUpdated));
        }

        void Timer_KeepEnemyUpdated(string asTimer)
        {
        	ShowEnemyPlayerPosition(GetLocalVarString("ActiveEnemy"));
        	AddTimer(asTimer, 1.0f, nameof(Timer_KeepEnemyUpdated));
        }

        //------------------------------------------------------------------------------------------

        ///////////////////////////////////////////////////////////
        // Updates info so we can know where the player and enemy are
        void TrackEntityLocation(string asParent, string asChild, int alState)
        {
        	string sEntity = asParent;
        	if(asParent=="Player")
        	{
        		SetLocalVarString("CurrentPlayerLocation", asChild);
        		UpdatePatrolNodes(false);
        	}
        	else
        	{
        		sEntity = "Enemy (" + sEntity + ")";
        		SetLocalVarString("CurrentEnemyLocation", asChild);
        	}
        	
        	//AddDebugMessage(sEntity + " is in area " + asChild, false);
        }

        //------------------------------------------------------------------------------------------


        //////////////////////////////////////////////////////////
        // HELPER - POUNDING
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        public enum ePoundArea
        {
        	ePoundArea_EastCorridor,
        	ePoundArea_SlideDoor1,
        	ePoundArea_SlideDoor2,
        	ePoundArea_SlideDoor3
        };

        string[] gvPoundDoorNames =
        {
        	"wooden_slide_door_0",
        	"wooden_slide_door_1",
        	"wooden_slide_door_2",
        	"wooden_slide_door_3",
        };

        public enum ePoundIntensity
        {
        	ePoundIntensity_Low,
        	ePoundIntensity_High
        };
        string[] gvPoundSuffix = { "low", "high" };

        //------------------------------------------------------------------------------------------

        float gfMinTimeBetweenPounding = 1.0f;
        float gfMaxTimeBetweenPounding = 3.0f;

        const string ksDelayedPoundingTimerName = "DelayedStartPounding";
        const string ksPoundingTimerName = "Pounding";

        //------------------------------------------------------------------------------------------

        void AddDelayedStartPounding(float afTime)
        {
        	//AddDebugMessage("Starting pounding in " + afTime + " secs.", false);
        	AddTimer(ksDelayedPoundingTimerName, afTime, nameof(Timer_StartPounding));
        }

        //------------------------------------------------------------------------------------------

        void StartPounding(int aArea, float afMinTime, float afMaxTime)
        {
        	if(GetLocalVarInt("PoundingActive")==1 || GetLocalVarInt("EnemyState")!=(int)eEnemyState_Hindered)
        		return;
        		
        	RemoveTimer(ksDelayedPoundingTimerName); 
        	RemoveTimer(ksPoundingTimerName);
        	
        	if(GetLocalVarInt(nameof(StopPounding))==1) SetLocalVarInt(nameof(StopPounding),0);
        	
        	SetPoundingTime(afMinTime, afMaxTime);
        	
        	int lArea = GetLocalVarInt("EnemyStep")+1;
        	
        	// AngelScript aborts here on an out-of-range array access.
        	if(lArea < 0 || lArea >= gvPoundDoorNames.Length) return;
        	if(GetLocalVarInt(gvPoundDoorNames[lArea]+"_state")!=(int)eDoorState_Closed) 
        		return;
        	
        	SetLocalVarInt("PoundingActive", 1);
        	AddTimer(ksPoundingTimerName, RandFloat(gfMinTimeBetweenPounding, gfMaxTimeBetweenPounding), nameof(Timer_Pounding));
        }

        //------------------------------------------------------------------------------------------

        void SetPoundingTime(float afMinTime, float afMaxTime)
        {
        	if(afMinTime>=0.0f)
        		gfMinTimeBetweenPounding = afMinTime;
        		
        	if(afMaxTime>=gfMinTimeBetweenPounding)
        		gfMaxTimeBetweenPounding = afMaxTime;
        }

        //------------------------------------------------------------------------------------------

        void SetPoundingIntensity(ePoundIntensity aX)
        {
        	SetLocalVarInt("PoundingIntensity", (int)aX);
        }

        //------------------------------------------------------------------------------------------

        void StopPounding()
        {
        	SetLocalVarInt(nameof(StopPounding),1);
        }

        //------------------------------------------------------------------------------------------

        /////////////////////////////////////////////////////////
        // Timer_Pounding : controls the timed pounding effect
        void Timer_Pounding(string asTimer)
        {
        	int lPound = GetLocalVarInt("EnemyStep")+1;
        	// Check if we want to stop
        	if(GetLocalVarInt(nameof(StopPounding))==1 || GetLocalVarInt("FelixIsEaten")==1)
        	{
        		//AddDebugMessage("Stopping pounding at area " + lPound , false);
        		
        		SetLocalVarInt("PoundingActive", 0);
        		SetLocalVarInt(nameof(StopPounding), 0);
        		
        		return;
        	}
        	
        	// Check if the pounding should be low or high
        	// AngelScript aborts here on an out-of-range array access.
        	int lPoundingIntensity = GetLocalVarInt("PoundingIntensity");
        	if(lPoundingIntensity < 0 || lPoundingIntensity >= gvPoundSuffix.Length) return;
        	string sSuffix = gvPoundSuffix[lPoundingIntensity];
        	//AddDebugMessage("Pound! "+ sSuffix + " at area " + lPound , false);
        	
        	PlaySoundAtEntity("", "pounding_"+sSuffix+".snt", "Area_Pounding_"+lPound, 0, false);
        	
        	if(RandInt(0, 2) == 1)
        		PlaySoundAtEntity("", "pounding_suitor_"+sSuffix+".snt", "Area_Pounding_"+lPound, 0, false);
        	
        	CreateParticleSystemAtEntity("", "ps_hit_wood.ps", "Area_PoundEffects_"+lPound, false);
        	
        	// Loop timer
        	AddTimer(ksPoundingTimerName, RandFloat(gfMinTimeBetweenPounding, gfMaxTimeBetweenPounding), nameof(Timer_Pounding));
        }

        //------------------------------------------------------------------------------------------

        void Timer_StartPounding(string asTimer)
        {
        	StartPounding(GetLocalVarInt("CurrentPoundArea"), -1, -1);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // HELPER - DOOR EFFECTS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        ////////////////////////////
        // Water splash for secuiry doors
        void CollideSecurityDoorInWater(string asParent, string asChild, int alState)
        {
        	CreateParticleSystemAtEntity("", "waterlurker_walk_splash", asChild, false);
        	CreateParticleSystemAtEntity("", "waterlurker_walk_splash", asChild+"_2", false);
        	PlaySoundAtEntity("", "impact_water_med", asChild, 0.0f, false);
        }

        ///////////////////////////////
        // Water splash for regular doors
        void CollideWaterEffectsSwingDoor(string asParent, string asChild, int alState)
        {
        	CreateParticleSystemAtEntity("", "ps_impact_water_low.ps", asChild, false);
        	PlaySoundAtEntity("", "L03_water.snt", asChild, RandFloat(0.0f, 1.0f), false);
        	
        	AddPropForce(asParent, RandInt(-300, 300), 0, RandInt(-300, 300), "World");
        	
        	//AddDebugMessage("Water splash", false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR INTERACT
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void PlayerInteractSlideDoor(string asEntity)
        {
        	if(GetTimerTimeLeft("InteractSlideDoor")>0) return;
        	AddTimer("InteractSlideDoor",2,"");

        	int lArea = GetLocalVarInt("EnemyStep")+1;
        	// AngelScript aborts here on an out-of-range array access.
        	if(lArea < 0 || lArea >= gvPoundDoorNames.Length) return;
        	if(asEntity==gvPoundDoorNames[lArea] && GetLocalVarInt("PoundingActive")==1)
        	{
        		DoEnemyStep();
        		SetEntityPlayerInteractCallback(asEntity, "", false);
        	}
        	else
        	{
        		SetMessage("L03Dungeon", "InteractSlideDoor",1);
        		PlaySoundAtEntity("InteractSlideDoor", "door_prison_locked",asEntity, 0, false);
        	}
        }

        //------------------------------------------------------------------------------------------

        void InteractLockedDoor(string asEntity)
        {
        	SetMessage("L03Dungeon", nameof(InteractLockedDoor),1);	
        }

        //------------------------------------------------------------------------------------------


        //////////////////////////////////////////////////////////
        // HELPER - DOOR OPENING
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        const int kLever_Open = -1;
        const int kLever_Close = 1;

        public enum eDoorState
        {
        	eDoorState_Closed,
        	eDoorState_Open,
        	eDoorState_Destroyed,
        };

        //------------------------------------------------------------------------------------------


        void SetDoorState(string asDoorName, eDoorState aState)
        {
        	SetLocalVarInt(asDoorName+"_state", (int)aState);

        }

        //------------------------------------------------------------------------------------------

        bool LeverHelper_Open(string asLeverName, int alSlideDoorNum, int alState)
        {
        	if(alState!=kLever_Open) return false;
        	
        	string sDoorName = "wooden_slide_door_"+alSlideDoorNum;
        	
        	SetLeverStuckState(asLeverName, alState, true);
        	SetMoveObjectState(sDoorName, 1.0f);
        	SetEntityActive("Area_PassedDoor_"+alSlideDoorNum, true);
        	
        	SetDoorState(sDoorName, eDoorState_Open);
        	
        	return true;
        }

        //------------------------------------------------------------------------------------------

        bool LeverHelper_Close(string asLeverName, int alSlideDoorNum, int alState)
        {
        	// AngelScript aborts here on an out-of-range array access.
        	if(alSlideDoorNum < 0 || alSlideDoorNum >= gvPoundDoorNames.Length) return false;
        	string sDoorName = gvPoundDoorNames[alSlideDoorNum];
        	int lState = GetLocalVarInt(sDoorName+"_state");
        	
        	//AddDebugMessage("Door " + sDoorName + " state: " + lState, false);
        	
        	if(alState!=kLever_Close) return false;
        	if(lState!=(int)eDoorState_Open) return false;
        	
        	RemoveTimer("PissOffMonster");
        	RemoveTimer("chaseplayer");
        	//string sDoorName = "wooden_slide_door_"+alSlideDoorNum;
        	
        	SetLeverStuckState(asLeverName, alState, true);
        	SetMoveObjectStateExt(sDoorName, 0.0f, 8, 14, 0.1f, true);
        	
        	SetDoorState(sDoorName, eDoorState_Closed);
        	
        	SetUpEnemyHindering(sDoorName);
        	
        	return true;
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PASS DOOR CHECK
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        float gfTimeToLockDoorSafely = 3.0f;
        void CollidePassedDoor(string asParent, string asChild, int alState)
        {
        	//AddDebugMessage("Player has " + gfTimeToLockDoorSafely + " seconds to close door!", false);
        	AddTimer("PissOffMonster", gfTimeToLockDoorSafely, nameof(Timer_PissOffMonster));
        }

        //------------------------------------------------------------------------------------------

        void Timer_PissOffMonster(string asTimer)
        {
        	//AddDebugMessage("Took too long to close door, pissing off monster!", false);
        	ChasePlayer();
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR01 - PICK LEVER
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void InteractMissingLever(string asEntity)
        {
        	SetMessage("L03Dungeon", "MissingStick", 0);
        	
        	AddTimer(asEntity, 1.0f, nameof(TimerInteractMissingLever));
        }

        void TimerInteractMissingLever(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractMissingLever), true);
        }

        //------------------------------------------------------------------------------------------

        void EntityCallPickLever(string asEntity, string type)
        {
        	SetEntityActive("Area_StartPoundingWithLever", true);
        }

        void PuzzleSlideDoor01_PickupLeverHandle(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Lever handle picked - Start pounding", false);
        	
        	//StartPounding(ePoundArea_EastCorridor, 1.0f, 3.0f);
        }

        void CollideStartPoundingWithLever(string asParent, string asChild, int alState)
        {
        	//StartPounding(ePoundArea_EastCorridor, 0.0f, 1.0f);
        	//SetPoundingTime(1.0f, 3.0f);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////////////////////////////////////////////////
        // This func sets a flag indicating if the lever handle or the playeris in the area near the door
        void PuzzleOpenDoor01_EnterNearDoorArea(string asParent, string asChild, int alState)
        {
        	if(alState<0) alState = 0;
        	
        	//AddDebugMessage(asParent + " state " + alState, false);
        	
        	SetLocalVarInt(asParent + "_InsideArea", alState);
        	
        	if(HasItem("slide_door01_lever_handle")) SetLocalVarInt("slide_door01_lever_handle_InsideArea", alState);
        	
        	CheckPlayerAndLeverInsideArea();
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR01 - FIX MISSING LEVER
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void CheckPlayerAndLeverInsideArea()
        {
        	return;
        }

        //------------------------------------------------------------------------------------------

        void UseLeverOnMount(string asItem, string asEntity)
        {
        	RemoveItem(asItem);
        	
        	PlaySoundAtEntity("", "13_rod_in", asEntity, 0.0f, false);
        	CreateParticleSystemAtEntity("", "ps_hit_metal", asEntity, false);
        	
        	PuzzleOpenDoor01_StartFixLeverTimer(asEntity, asItem);
        	
        	SetEntityActive(asEntity, false);
        }

        void PuzzleOpenDoor01_StartFixLeverTimer(string asStickyArea, string asBodyName)
        {
        	//AddDebugMessage("Sticky! " + asStickyArea + " glueing " + asBodyName , false);
        	
        	AddTimer("", 0.01f, nameof(PuzzleOpenDoor01_FixLever));
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // Fixes the lever (activates the lever entity)
        void PuzzleOpenDoor01_FixLever(string asTimer)
        {
        	//AddDebugMessage("Lever Fixed!", false);
        	
        	SetLocalVarInt("PuzzleSlideDoor01_LeverFixed", 1);
        	
        	SetEntityActive("slide_door01_lever_mount", false);
        	SetEntityActive("slide_door01_lever_handle", false);
        	SetEntityActive("slide_door01_fixedlever", true);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR01 - OPEN/CLOSE
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------


        void PuzzleOpenDoor01_LeverPulled(string asEntityName, int alState)
        {
        	AddDebugMessage("Lever " + asEntityName + " Pulled: " + alState, false);
        	
        	if(asEntityName=="slide_door01_fixedlever")
        	{
        		if(LeverHelper_Open(asEntityName, 1, alState)==false) return;
        	
        		ActivateEnemy00();
        	}
        	else if(asEntityName=="slide_door01_lever")
        	{
        		if(LeverHelper_Close(asEntityName, 1, alState)==false) return;
        		
        		SetLocalVarInt("Door01LeverPulledDown", 1);
        		
        		//Turn off music if the enemy is now under the other side.
        		if(GetLocalVarInt("EnemyState")==(int)eEnemyState_Hindered)
        		{
        			PlayEnemyMusic(0); //Go to danger!
        		}
        				
        		//AddDelayedStartPounding(gfTimeToStartPoundingAfterSlideDoor1Closed);
        		//SetPoundingIntensity(ePoundIntensity_Low);
        	}
        }

        //------------------------------------------------------------------------------------------



        //////////////////////////////////////////////////////////
        // DOOR01 - ENEMY
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////
        // Activate the enemy
        void ActivateEnemy00()
        {
        	AddTimer("doorsound", 4.5f, nameof(Timer_ActivateEnemy00_Door));
        	AddTimer("doorsound", 6, nameof(Timer_ActivateEnemy00_Sound));
        	
        	DoEnemyStep();
        	
        	//When player or enemy reach a certain point we want to start playing search music
        	AddEntityCollideCallback("Player", "Area_StartSearchMusc01", nameof(CollideStartSearchMusic), true, 1);
        	AddEntityCollideCallback("enemy_suitor_0", "Area_StartSearchMusc01", nameof(CollideStartSearchMusic), true, 1);
        }

        //------------------------------------------------------------------------------------------
        void Timer_ActivateEnemy00_Door(string asTimer)
        {
        	PlayEnemyMusic(0);
        	PlaySoundAtEntity("SoundDoorCreak", "01_door.snt", "wooden_slide_door_0", 0.0f, false);
        }

        void Timer_ActivateEnemy00_Sound(string asTimer)
        {
        	PlaySoundAtEntity("EnemyHowl", "suitor/enabled.snt", "wooden_slide_door_0", 0.0f, false);
        }
        //------------------------------------------------------------------------------------------

        void CollideStartSearchMusic(string asParent, string asChild, int alState)
        {	
        	SetEntityActive(asChild, false);
        	PlayEnemyMusic(1);
        }


        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR02 - OPEN/CLOSE
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void PuzzleSlideDoor02_LeverPulled(string asEntityName, int alState)
        {
        	//AddDebugMessage("Lever " + asEntityName + " Pulled: " + alState, false);
        	
        	//AddDebugMessage("Works now!", false);
        	
        	if(asEntityName=="slide_door02_lever_open")
        	{
        		if(alState!=-1) return;
        	
        		if(GetLocalVarInt("PuzzleSlideDoor02_MachineFixed")==0)
        		{
        		
        			SetMessage("L03Dungeon", "BrokenMachine", 0);
        			PlaySoundAtEntity("", "L03_machine_fail", "AreaMachineSound", 0.0f, false);
        		
        			//AddDebugMessage("This won't work unless the machine is fixed! - Add memento or whatever", false);
        			return;
        		}	

        		LeverHelper_Open(asEntityName, 2, alState);
        	}
        	else if(asEntityName=="slide_door02_lever_close")
        	{
        		if(LeverHelper_Close(asEntityName, 2, alState)==false) return;
        		
        		AddDelayedStartPounding(gfTimeToStartPoundingAfterSlideDoor2Closed);
        	}
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR02 - PICK UP COGWHEEL
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void PuzzleSlideDoor02_PickupCogWheel(string asParent, string asChild, int alState)
        {
        	//AddDebugMessage("Cogwheel picked", false);
        	
        	SetEntityActive("AreaMachineHelp", false);  //Disable help interaction area
        	
        	//Only change music if the player is not out and running
        	if(GetLocalVarInt("EnemyState")==(int)eEnemyState_Hindered)
        	{
        		AddTimer("pickupcog", 2, nameof(TimerPickupCogEvent));
        	}
        	else
        	{
        		ChasePlayer();
        	}
        }

        void TimerPickupCogEvent(string asTimer)
        {
        	StartPounding((int)ePoundArea_SlideDoor1, 1.0f, 3.0f);
        	PlayEnemyMusic(0);
        }

        //------------------------------------------------------------------------------------------

        void PuzzleSlideDoor02_CogwheelInMachineryHall(string asParent, string asChild, int alState)
        {
        	//Only change music if the player is not out and running
        	if(GetLocalVarInt("EnemyState")==(int)eEnemyState_Hindered)
        	{
        		SetPoundingIntensity(ePoundIntensity_High);
        		PlayEnemyMusic(1);
        	}
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR02 - PUT COGHWEEL ON MACHINE
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void PuzzleOpenDoor02_StartFixMachineTimer(string asEntityName, string asBodyName)
        {
        	 //Just incase setting a stick on/off after something is stuck is not a good idea.
        	SetEntityActive("AreaCloseToMachine", false); 
        	
        	//AddDebugMessage("Make some time until cog is in place...", false);
        	
        	AddTimer("", 1.0f, nameof(PuzzleOpenDoor02_FixMachine));
        }

        //------------------------------------------------------------------------------------------

        //When close to machine activate sticky area. Else player can throw cogwheel in place.
        void CollideAreaCloseToMachine(string asParent, string asChild, int alState)
        {
        	if(alState == 1)
        		SetEntityActive("PuzzleSlideDoor02_CogSticky", true);
        		
        	if(alState == -1)
        		SetEntityActive("PuzzleSlideDoor02_CogSticky", false);
        }

        //------------------------------------------------------------------------------------------

        void PuzzleOpenDoor02_FixMachine(string asTimer)
        {
        	PlaySoundAtEntity("run1", "L03_machine_run", "AreaMachineSound", 2.0f, false);
        	PlaySoundAtEntity("run2", "L03_machine_extra", "AreaMachineSound", 2.0f, false);
        	PlaySoundAtEntity("start", "13_ignite", "AreaMachineSound", 0.2f, false);
        	
        	RotatePropToSpeed("elevator_cogwheel01_1", 1, 1, -1, 0, 0, false, "");
        	RotatePropToSpeed("elevator_cogwheel02_mounted_1", 1, 1, 1, 0, 0, false, "");
        	RotatePropToSpeed("elevator_cogwheel03_mounted_1", 1, 1, -1, 0, 0, false, "");
        	
        	RotatePropToSpeed("elevator_machine_cog_small_5", 1, 1, 1, 0, 0, false, "");
        	RotatePropToSpeed("elevator_machine_cog_small_4", 1, 2, -2, 0, 0, false, "");
        	RotatePropToSpeed("elevator_machine_cog_small_6", 1, 1, 1, 0, 0, false, "");
        	RotatePropToSpeed("elevator_machine_cog_small_7", 1, 2, -2, 0, 0, false, "");
        	
        	RotatePropToSpeed("elevator_machine_cog_small_2", 1, 1, 0, 0, 1, false, "");
        	RotatePropToSpeed("elevator_machine_cog_small_3", 1, 2, 0, 0, -2, false, "");
        	RotatePropToSpeed("elevator_machine_cog_small_1", 1, 1, 0, 0, 1, false, "");
        	
        	RotatePropToSpeed("elevator_machine_cog_small_8", 1, 1, 1, 0, 0, false, "");
        	RotatePropToSpeed("elevator_machine_cog_small_9", 1, 2, -2, 0, 0, false, "");
        	RotatePropToSpeed("elevator_machine_cog_small_10", 1, 1, 1, 0, 0, false, "");
        	RotatePropToSpeed("elevator_machine_cog_small_11", 1, 2, -2, 0, 0, false, "");
        	
        	RotatePropToSpeed("cogwheel_large_3", 0.6f, 0.6f, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_medium_6", 1, 1, 0, 0, -1, false, "");
        	RotatePropToSpeed("cogwheel_medium_7", 1, 1, 0, 0, 1, false, "");
        	
        	RotatePropToSpeed("cogwheel_medium_3", 1, 1, 1, 0, 0, false, "");
        	RotatePropToSpeed("cogwheel_medium_4", 1, 1, -0.8f, -0.2f, 0, false, "");
        	RotatePropToSpeed("cogwheel_medium_5", 1, 1, 0.3f, 0.6f, 0, false, "");
        	
        	RotatePropToSpeed("cogwheel_large_1", 0.6f, 0.6f, 0, -1, 0, false, "");
        	RotatePropToSpeed("cogwheel_large_2", 0.6f, 0.6f, 0, 1, 0, false, "");
        	RotatePropToSpeed("cogwheel_medium_1", 1, 1, 0, -1, 0, false, "");
        	RotatePropToSpeed("cogwheel_medium_2", 1, 1, 0, 1, 0, false, "");
        	
        	//AddDebugMessage("Cogwheel in place, starting machine!", false);
        	
        	SetLocalVarInt("PuzzleSlideDoor02_MachineFixed", 1);
        	
        	AddTimer("", gfTimeToActivateEnemyAfterFixingMachine, nameof(Timer_EnableEnemyAtDoor1));
        }

        //------------------------------------------------------------------------------------------

        void Timer_EnableEnemyAtDoor1(string asTimer)
        {
        	DoEnemyStep();
        }

        //------------------------------------------------------------------------------------------

        ////////////////////////////////////////
        // Interact help on the spoke
        void InteractAreaMachineHelp(string asEntity)
        {
        	SetMessage("L03Dungeon", "MissingCogwheel", 0);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR03 - OPEN / CLOSE EXIT DOOR
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void PuzzleSlideDoor03_DoorHalfUp(string asParent, string asChild, int alState)
        {
        	//AddDebugMessage("Southern door halfway up!", false);
        	SetDoorState("wooden_slide_door_3", eDoorState_Open);
        	
        	if(GetLocalVarInt("PuzzleSlideDoor03_PrisonerDoorClosed") != 1)
        		StartCharacterSpeak(GetLocalVarString("character")+"_terrorized", 1, false, true, true);
        	
        	if(GetLocalVarInt("PuzzleSlideDoor03_PrisonerDoorValveInteracted")==0)
        		AddTimer("EnableEnemyDoor2", gfTimeToActivateEnemyAtSlideDoor2WhenDoorHalfUp, nameof(Timer_EnableEnemyAtDoor2));		
        }

        void PuzzleSlideDoor03_DoorSetCollidable(string asParent, string asChild, int alState)
        {
        	//AddDebugMessage("Southern door no longer collidable!", false);
        	
        	SetEntityActive("Area_BreakDoor", false);
        }

        //------------------------------------------------------------------------------------------

        void PuzzleSlideDoor03_LeverPulled(string asEntityName, int alState)
        {
        	if(alState != 1) return;
        	
        	LeverHelper_Close(asEntityName, 3, alState);
        	SetEntityActive("Area_SouthernDoorClosed", true);
        }

        //------------------------------------------------------------------------------------------

        void PuzzleSlideDoor03_DoorClosed(string asParent, string asChild, int alState)
        {
        	int lPrisonerDoorClosed = GetLocalVarInt("PuzzleSlideDoor03_PrisonerDoorClosed");
        	//////////////////////////////////////
        	// Prisoner survives
        	if(lPrisonerDoorClosed==1)
        	{
        		if(GetLocalVarInt("EnemyState")==(int)eEnemyState_Hindered)
        		{
        			SetPoundingTime(1.0f, 2.5f);
        			
        			AddDelayedStartPounding(1.0f);
        			
        			AddTimer("", gfTimeToActivateEnemyAtSlideDoor3, nameof(Timer_EnableEnemyAtDoor3));
        		}
        		else
        		{
        			
        			ChasePlayer();
        		}
        	}
        	//////////////////////////////////////
        	// Prisoner is eaten behind the door.
        	else
        	{
        		PlayEnemyMusic(0);
        	
        		SetLocalVarInt("FelixIsEaten", 1);
        	
        		BeginDeathNoises();
        		
        		AddTimer("prisonerdeath1", 3.0f, nameof(TimerPrisonerDeathVoice));
        		AddTimer("prisonerdeath2", 8.0f, nameof(TimerPrisonerDeathVoice));
        		
        		AddDebugMessage("Enemy eats prisoner! Om nom nom nom", false);
        	}
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR03 - OPEN / CLOSE PRISONER DOOR
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void PuzzleSliderDoor03_PrisonerDoorValveInteract(string asEntity)
        {
        	AddDebugMessage("Prisoner door Valve interact!", false);

        	SetLocalVarInt("PuzzleSlideDoor03_PrisonerDoorValveInteracted", 1);
        	
        	if(GetTimerTimeLeft("EnableEnemyDoor2")==0)
        		AddTimer("EnableEnemyDoor2", gfTimeToActivateEnemyAtSlideDoor2WhenInteractingValve, nameof(Timer_EnableEnemyAtDoor2));
        }

        //------------------------------------------------------------------------------------------

        void PuzzleSlideDoor03_ClosePrisonerDoor(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Prisoner door closed", false);
        		
        	SetEntityActive("AreaCharStopTalk", false);
        	SetEntityActive("AreaCharStartTalk", false);
        	
        	SetEntityPlayerInteractCallback("valve_iron_rusty02_3", "", false);
        	SetEntityPlayerInteractCallback("valve_iron_rusty02_4", "", false);
        	
        	SetLocalVarInt("PuzzleSlideDoor03_PrisonerDoorClosed", 1);
        	
        	PlaySoundAtEntity("", "14_elevator_clank.snt", asChild, 0.0f, false);
        	PlaySoundAtEntity("", "07_pick_lock.snt", asChild, 0.0f, false);
        	
        	//////////////////////////////////
        	// Door is down so Felix survived!
        	SetGlobalVarInt("FelixIsDead", 0);
        	
        	AddKeyPart(0x3F4E2617);
        	AddKeyPart(0x1aaf2127); 
        }

        void PuzzleSlideDoor03_PrisonerDoorValveState(string asEntityName, int alState)
        {
        	/*
        	AddDebugMessage("Prisoner door valve state changed!", false);
        	if(alState==-1)
        	{
        		AddDebugMessage("Prisoner door closed", false);
        		
        		SetEntityActive("AreaCharStopTalk", false);
        		SetEntityActive("AreaCharStartTalk", false);
        		
        		SetEntityPlayerInteractCallback("valve_iron_rusty02_3", "", false);
        		SetEntityPlayerInteractCallback("valve_iron_rusty02_4", "", false);
        	}
        	*/
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DOOR03 - ENEMY
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void Timer_EnableEnemyAtDoor2(string asTimer)
        {
        	DoEnemyStep();
        }

        //------------------------------------------------------------------------------------------

        void CollideBreakDoor(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("wooden_slide_door_3_state")==(int)eDoorState_Closed) return;
        	
        	//AddDebugMessage("Monster wants to break door!",false);
        	SetDoorState("wooden_slide_door_3", eDoorState_Destroyed);
        	
        	SetEntityActive("wooden_slide_door_3", false);
        	SetEntityActive("wooden_slide_door_destroyed_1", true);
        	
        	DoEnemyEffects(3,asParent);
        }


        //////////////////////////////////////////////////////////
        // TUNNEL CHASE
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        ///////////////////////////////////
        // Start the chase
        void Timer_EnableEnemyAtDoor3(string asTimer)
        {
        	DoEnemyStep();
        }

        //------------------------------------------------------------------------------------------

        ///////////////////////////////////////////
        // Break all boxes when enemy enters area
        void CollideEnemyEndCrash(string asParent, string asChild, int alState)
        {
        	if(asChild == "Area_EnemyBreaksThroughDebris_1")
        		BreakTunnelBoxEffect(asChild, 1);
        	
        	else if(asChild == "Area_EnemyBreaksThroughDebris_2")
        		BreakTunnelBoxEffect(asChild, 2);	
        		
        	else if(asChild == "Area_EnemyBreaksThroughDebris_3")
        		BreakTunnelBoxEffect(asChild, 3);
        }

        //------------------------------------------------------------------------------------------

        ///////////////////////////////////////////
        // Start chasing player at tunnel (if not eating prisoner)
        void CollideStartChase(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Collided with tunnel area", false);
        	
        	if(GetLocalVarInt("FelixIsEaten")==0)
        	{
        		AddDebugMessage("Starting tunnel chase: ", false);
        		SetLocalVarInt("ChaseStarted", 1);
        		ChasePlayer();
        	}
        }

        //------------------------------------------------------------------------------------------

        void BreakTunnelBoxEffect(string sEntity, int iArea)
        {
        	PlaySoundAtEntity("bangs", "break_wood_metal", sEntity, 0.0f, false);
        	PlaySoundAtEntity("bangs", "attack_launch", sEntity, 0.0f, false);
        	
        	CreateParticleSystemAtEntity("bangp", "ps_dust_break_25", sEntity, false);
        	
        	SetPropHealth("a"+iArea+"b_*", 0);
        	
        	StartScreenShake(0.05f, 0.4f, 0.2f, 0.6f);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PHONOGRAPH MESSAGES 
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        // The phonograph in the starting corridor
        void StateChangePhono01(string asEntity, int alState)
        {
        	PlayPhono(3, 1, 3, asEntity);
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

        void CollideAreaStopPhono(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt(nameof(PlayPhono)) != 1) return;
        	
        	StopAllEffectVoices(2.0f);
        	VoiceOverPhono();
        }
        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PRISONER TALK
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

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
        		
        	//AddDebugMessage("INSIDE START TALK", false);
        }

        //Character stop talking
        void CollideCharacterStopTalk(string asParent, string asChild, int alState)
        {
        	//Only run function is inside talk range
        	if(GetLocalVarInt("PlayerInCharTalkRange")==0) return; 
        	SetLocalVarInt("PlayerInCharTalkRange",0);
        	
        	StopCharacterSpeak();
        	
        	//AddDebugMessage("OUTSIDE STOP TALK", false);
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
        	float fMinDist = 8;
        	float fMaxDist = 40;
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
        	
        	//AddDebugMessage("Starting topic "+sSoundName, false);
        }

        void CharacterTalkOver(float fStopVoiceFade)
        {
        	//No more callback
        	SetEffectVoiceOverCallback("");
        	
        	//Stop the current voice playing
        	StopAllEffectVoices(fStopVoiceFade);
        	
        	//AddDebugMessage("Message is over.", false);
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
        	
        	//AddDebugMessage("Message was stopped.", false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PRISONER TALKING EXTRA
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------
        void InteractValveVoice(string asEntity)
        {			
        	AddTimer(asEntity, RandFloat(0.3f, 3.0f), nameof(TimerInteractValveVoice));
        }
         
        void TimerInteractValveVoice(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractValveVoice), true);
        	
        	if(GetEffectVoiceActive()) return;
        	
        	if(RandInt(0,2) == 0)
        		StartCharacterSpeak(GetLocalVarString("character")+"_scared", 3, true, false, false);
        	else if(RandInt(0,2) == 0)
        		StartCharacterSpeak(GetLocalVarString("character")+"_sobbing", 3, true, false, false);
        	else
        		StartCharacterSpeak(GetLocalVarString("character")+"_idle",5 , true, false, false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PRISONER DEATH
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void TimerPrisonerDeathVoice(string asTimer)
        {
        	if(asTimer == "prisonerdeath1")
        	{
        		SetEntityActive("enemy_suitor_2", false);
        		StartCharacterSpeak(GetLocalVarString("character")+"_dying", 1, false, true, true);
        	}	
        	
        	if(asTimer == "prisonerdeath2")
        		StartCharacterSpeak(GetLocalVarString("character")+"_death", 1, false, true, true);
        }

        //------------------------------------------------------------------------------------------

        void BeginDeathNoises()
        {
        	PlaySoundAtEntity("", "suitor/enabled.snt", "AreaCharTalk_3", 0, false);
        	
        	AddTimer("AreaCharTalk_3", 0.5f, nameof(TimerDeathNoises));
        	AddTimer("AreaCharTalk_2", 1.5f, nameof(TimerDeathNoises));
        	AddTimer("AreaCharTalk_1", 3.0f, nameof(TimerDeathNoises));
        	
        	AddTimer("L03_alert", 3.5f, nameof(TimerDeathNoises));
        	AddTimer("L03_attack", 5.0f, nameof(TimerDeathNoises));
        	AddTimer("L03_alert", 7.0f, nameof(TimerDeathNoises));
        	AddTimer("L03_attack", 9.0f, nameof(TimerDeathNoises));
        	AddTimer("L03_launch", 13.0f, nameof(TimerDeathNoises));
        	
        	AddTimer("L03_chains_fast", 3.0f, nameof(TimerDeathNoises));
        	AddTimer("L03_chains_slow", 4.0f, nameof(TimerDeathNoises));
        	AddTimer("L03_chains_fast", 6.0f, nameof(TimerDeathNoises));
        	AddTimer("L03_chains_slow", 8.0f, nameof(TimerDeathNoises));
        	AddTimer("L03_chains_fast", 9.0f, nameof(TimerDeathNoises));
        	AddTimer("L03_chains_slow", 11.0f, nameof(TimerDeathNoises));
        	
        	AddTimer("L03_meat_long", 4.5f, nameof(TimerDeathNoises));
        	AddTimer("L03_meat_snap", 5.1f, nameof(TimerDeathNoises));
        	AddTimer("L03_meat_squeaz", 7.0f, nameof(TimerDeathNoises));
        	AddTimer("L03_meat_long", 9.2f, nameof(TimerDeathNoises));
        	AddTimer("L03_meat_snap", 10.7f, nameof(TimerDeathNoises));
        	AddTimer("L03_meat_squeaz", 11.2f, nameof(TimerDeathNoises));
        	AddTimer("L03_meat_long", 13.7f, nameof(TimerDeathNoises));
        	
        	AddTimer("L03_prisoner_break", 3.7f, nameof(TimerDeathNoises));
        	AddTimer("L03_prisoner_squeeze", 4.3f, nameof(TimerDeathNoises));
        	AddTimer("L03_prisoner_break", 6.5f, nameof(TimerDeathNoises));
        	AddTimer("L03_prisoner_squeeze", 7.6f, nameof(TimerDeathNoises));
        	AddTimer("L03_prisoner_break", 9.9f, nameof(TimerDeathNoises));
        	AddTimer("L03_prisoner_squeeze", 12.7f, nameof(TimerDeathNoises));
        	AddTimer("L03_prisoner_break", 13.2f, nameof(TimerDeathNoises));
        	AddTimer("L03_prisoner_squeeze", 14.6f, nameof(TimerDeathNoises));
        	
        	AddTimer("StopMusic", 13, nameof(TimerDeathNoises));
        }

        //------------------------------------------------------------------------------------------

        void TimerDeathNoises(string asTimer)
        {
        	if(asTimer == "AreaCharTalk_3")
        	{
        		PlayMusic("29_amb_end_intense", false, 0.7f, 2.0f, 10, false);
        		
        		AddTimer("L03_chains_slow", 0.1f, nameof(TimerDeathNoises));
        		AddTimer("L03_sneak_water", 0.2f, nameof(TimerDeathNoises));
        	}
        	
        	if(asTimer == "AreaCharTalk_2")
        	{
        		AddTimer("L03_chains_fast", 0.3f, nameof(TimerDeathNoises));
        		AddTimer("L03_sneak_water", 0.6f, nameof(TimerDeathNoises));
        		AddTimer("L03_alert", 0.8f, nameof(TimerDeathNoises));
        	}
        	
        	if(asTimer == "AreaCharTalk_1")
        	{
        		AddTimer("L03_chains_slow", 0.2f, nameof(TimerDeathNoises));
        		AddTimer("L03_sneak_water", 0.3f, nameof(TimerDeathNoises));
        	}
        	
        	if(asTimer == "StopMusic")
        	{
        		StopMusic(5.0f, 10);
        	}
        	
        	PlaySoundAtEntity("", asTimer, "AreaCharTalk", RandFloat(0,0.4f), false);

        	//AddDebugMessage("Playing sound: "+ asTimer, false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // SOUND & MUSIC
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        //Levels:
        // 0=danger, 1=near, 2=hunting.
        void PlayEnemyMusic(int alLevel)
        {
        	AddDebugMessage("Enemy music: "+alLevel, false);
        	
        	if(alLevel<0){
        		StopMusic(6, 8);
        		return;
        	}

        	string sMusic = "search_brute.ogg";
        	if(alLevel==1) sMusic = "att_brute.ogg";
        	if(alLevel==2) sMusic = "att_suitor.ogg";
        	
        	PlayMusic(sMusic, true, 1.0f, 3, 8, true);
        }


        //------------------------------------------------------------------------------------------

        //Fade in the water ambience when you see the water at the start.
        void CollideAreaAmbSoundChange(string asParent, string asChild, int alState)
        {
        	if(alState == -1)
        	{
        		StopSound("cave", 3.0f);
        		PlaySoundAtEntity("water", "ambience_water_no3d.snt", asChild, 2.0f, true);
        	}
        	
        	else if(alState == 1)
        	{
        		StopSound("water", 3.0f);
        		PlaySoundAtEntity("cave", "ambience_haunting.snt", asChild, 2.0f, true);
        	}
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // ITEM PICK UP
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void PickUpNote(string asEntity, string asType)
        {
        	AddKeyPart(unchecked((int)0x911111AF));
        	AddKeyPart(unchecked((int)0x9eef3721));
        	AddDebugMessage("PickNote!\n", false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // MAIN CALLBACKS
        //////////////////////////////////////////////////////////


        //------------------------------------------------------------------------------------------

        public override void OnStart()
        {
        	SetupLoadScreen("LoadingText","Load04_",3,"ptest_gameloading_03.jpg");
        	
        	////////////////////////////
        	// SET LEVEL NAME
        	SetMapDisplayNameEntry("L03Dungeon");
        	
        	GiveSanityBoost();
        	
        	SetPlayerPermaDeathSound("malo_death.snt");
        	
        	////////////////////////////
        	// Variables
        	// NOTE: In case a reset is done, this var needs to be -1 initially!!
        	SetLocalVarInt("EnemyStep",-1);
        	
        	// Character vars
        	SetGlobalVarInt("FelixIsDead", 1);	//Start out as dead and then onlive survive if the player 
        	SetLocalVarString("character", "felix");	//Used to play voices for prisoner character in level
        	
        	
        	////////////////////////////
        	// Collide Callbacks
        	
        	for(int i=1;i<=5;i++)
        	{
        		AddEntityCollideCallback("cellar_wood01_slow_6", "cellar_wood01_slow_6_A_"+i, nameof(CollideWaterEffectsSwingDoor), false, 1);
        		AddEntityCollideCallback("cellar_wood01_slow_1", "cellar_wood01_slow_1_A_"+i, nameof(CollideWaterEffectsSwingDoor), false, 1);
        		AddEntityCollideCallback("cellar_wood01_slow_2", "cellar_wood01_slow_2_A_"+i, nameof(CollideWaterEffectsSwingDoor), false, 1);
        		AddEntityCollideCallback("cellar_wood01_slow_5", "cellar_wood01_slow_5_A_"+i, nameof(CollideWaterEffectsSwingDoor), false, 1);
        		AddEntityCollideCallback("cellar_wood01_slow_8", "cellar_wood01_slow_8_A_"+i, nameof(CollideWaterEffectsSwingDoor), false, 1);
        		AddEntityCollideCallback("cellar_wood01_slow_4", "cellar_wood01_slow_4_A_"+i, nameof(CollideWaterEffectsSwingDoor), false, 1);
        	}	
        	
        	AddEntityCollideCallback("Player", "AreaStopPhono", nameof(CollideAreaStopPhono), false, -1);
        	
        	AddEntityCollideCallback("Area_PickLeverHandle", "slide_door01_lever_handle", nameof(PuzzleSlideDoor01_PickupLeverHandle), true, -1);
        	AddEntityCollideCallback("Player", "Area_StartPoundingWithLever", nameof(CollideStartPoundingWithLever), true, 1);
        	AddEntityCollideCallback("slide_door01_lever_handle", "Area_NearSlideDoor01", nameof(PuzzleOpenDoor01_EnterNearDoorArea), false, 0);
        	AddEntityCollideCallback("Player", "Area_NearSlideDoor01", nameof(PuzzleOpenDoor01_EnterNearDoorArea), false, 0);
        	AddEntityCollideCallback("Area_PickCogWheel", "elevator_cogwheel01_1", nameof(PuzzleSlideDoor02_PickupCogWheel), true, -1);
        	AddEntityCollideCallback("Area_SouthernDoorHalfUp", "wooden_slide_door_3", nameof(PuzzleSlideDoor03_DoorHalfUp), true, -1);
        	AddEntityCollideCallback("Area_SouthernDoorCollidable", "wooden_slide_door_3", nameof(PuzzleSlideDoor03_DoorSetCollidable), true, -1);
        	AddEntityCollideCallback("Area_SouthernDoorClosed", "wooden_slide_door_3", nameof(PuzzleSlideDoor03_DoorClosed), true, 1);
        	
        	AddEntityCollideCallback("elevator_cogwheel01_1", "Area_Machinery", nameof(PuzzleSlideDoor02_CogwheelInMachineryHall), true, 1);
        	AddEntityCollideCallback("Player", "AreaCloseToMachine", nameof(CollideAreaCloseToMachine), false, 0);	//Only allow sticky when close to machine.
        	
        	for(int i=-1; i<=3; ++i)
        	{
        		string sEntity = "Player";
        		if(i>=0) sEntity = "enemy_suitor_" + i;
        		
        		for(int j=1; j<=4; ++j)
        			AddEntityCollideCallback(sEntity, "Area_Room_" + j, nameof(TrackEntityLocation), false, 1);
        	}
        	
        	//This only affect the two last doors and not the first!!
        	for(int i=2; i<=3; ++i)
        		AddEntityCollideCallback("Player", "Area_PassedDoor_"+i, nameof(CollidePassedDoor), true, 1);	
        	
        	AddEntityCollideCallback("Player", "AreaCharStartTalk", nameof(CollideCharacterStartTalk), false, 1);	
        	AddEntityCollideCallback("Player", "AreaCharStopTalk", nameof(CollideCharacterStopTalk), false, -1);
        	
        	for(int i=0; i<4; ++i)
        		AddEntityCollideCallback("enemy_suitor_"+i, "Area_BreakDoor", nameof(CollideBreakDoor), true, 1);
        		
        	AddEntityCollideCallback("Player", "Area_StartChase", nameof(CollideStartChase), true, 1);
        	
        	for(int i=0; i<4; ++i)
        	{
        		for(int j=1; j<=3; ++j)
        			AddEntityCollideCallback("enemy_suitor_"+i, "Area_EnemyBreaksThroughDebris_"+j, nameof(CollideEnemyEndCrash), true, 1);
        	}
        		
        	AddEntityCollideCallback("Player", "AreaAmbSoundChange", nameof(CollideAreaAmbSoundChange), false, 0);
        	
        	//for(int i=1; i<=5; ++i)
        		//AddEntityCollideCallback("Player", "Area_Punishment_"+i, "CollidePushArea", false, 0);
        	
        	// Remove lurker events
        	//AddEntityCollideCallback("Player", "AreaLurkyBoo", "CollideAreaLurkyBoo", false, 0);
        	//AddEntityCollideCallback("Player", "AreaLurkyBooFool", "CollideAreaLurkyBooFool", true, 0);
        	
        	for(int i=1;i<=4;i++)
        		AddEntityCollideCallback("wooden_slide_door_"+i, "Area_DoorSplash_"+i, nameof(CollideSecurityDoorInWater), false, 1);
        		
        	//AddEntityCollideCallback("Player", "Area_CheckDoorClosed", "CollideCheckDoorClosed", true, 1);
        	
        	/////////////////////////////
        	// Item callbacks
        	//HAX 1: Lever made into an item instead, all changes for this are commented HAX 1.
        	AddUseItemCallback("leveronmount", "slide_door01_lever_handle", "PuzzleOpenDoor01_StickyArea", nameof(UseLeverOnMount), true);
        	
        	////////////////////////////
        	// Entity connections
        	InteractConnectPropWithMoveObject("ValveSouthDoor","valve_iron_rusty02_1", "wooden_slide_door_3",true, false, 0);

        	InteractConnectPropWithMoveObject("ValvePrisonerDoor","valve_iron_rusty02_2", "wooden_slide_door_4",true, false, 0);
        	// Prisoner door starts out open
        	SetMoveObjectState("wooden_slide_door_4", 1.0f);
        	AddTimer("", 2.0f, nameof(Timer_SetUpPrisonerDoor));
        		
        	////////////////////////////
        	// Item Callbacks
        	
        	///////////////////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("slide_door01_lever_handle", "slide_door_lever_handle");
        		//PuzzleSlideDoor01_PickupLeverHandle("", "", 1);
        	}       
        	
        	PreloadSound("L03_water"); PreloadSound("impact_water_med"); PreloadSound("ambience_water_no3d"); PreloadSound("ambience_haunting");
        	PreloadSound("attack_launch"); PreloadSound("13_ignite"); PreloadSound("break_door_suitor"); PreloadSound("suitor/enabled.snt");
        	PreloadSound("pounding_hard"); PreloadSound("pounding_low"); PreloadSound("L03_machine_fail"); PreloadSound("L03_machine_extra");
        	PreloadSound("L03_machine_run"); PreloadSound("L03_chains_slow"); PreloadSound("L03_sneak_water"); PreloadSound("L03_chains_fast");
        	PreloadSound("break_wood_metal"); PreloadSound("L03_alert");
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	//Sop enemy music playing
        	StopMusic(3,8);
        }

        //------------------------------------------------------------------------------------------

        void Timer_SetUpPrisonerDoor(string asTimer)
        {
        	AddEntityCollideCallback("wooden_slide_door_4", "Area_PrisonerDoorClosed", nameof(PuzzleSlideDoor03_ClosePrisonerDoor), true, 1);
        }

        float fPushX, fPushY, fPushZ;
        void CollidePushArea(string asParent, string asChild, int alState)
        {
        	if(asChild=="Area_Punishment_1")
        	{
        		//AddDebugMessage("Pushing other stuff!", false);
        		AddPropForce("wood_box02_1",9000.0f, 2000.0f, 0.0f, "World");
        		AddPropForce("wood_box_small01_2",9000.0f, 2000.0f, 0.0f, "World");
        		AddPropForce("wood_box_small01_3",9000.0f, 2000.0f, 0.0f, "World");
        	}
        	
        	if(alState==-1)
        		RemoveTimer("pushplayer");
        	else
        		AddTimer("pushplayer", 0.5f, nameof(Timer_PushPlayer));
        }

        void Timer_PushPlayer(string asTimer)
        {
        	//AddDebugMessage("Push!", false);
        	AddPlayerBodyForce(19000.0f, 15000.0f, 0.0f, false);
        }

        //------------------------------------------------------------------------------------------

    }
}
