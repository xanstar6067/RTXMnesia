using RTXMnesia.ScriptApi;
using System.Globalization;

namespace RTXMnesia.Levels.Justine
{
    [HplMapScript("maps/ptest/04_final.map")]
    public class JustineFinal : HplScript
    {
        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // GLOBAL HELPERS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        float gfCurrentSpeed = 1.0f;
        float gfDestSpeed;
        float gfSpeedStep;
        float gfTimeStep;

        void FadePlayerSpeed(float afNewSpeed, float afTime, int alSteps)
        {
        	if(gfCurrentSpeed==afNewSpeed || 
        		alSteps<=0 || 
        		afTime<=0.0f) 
        		return;
        	
        	gfDestSpeed = afNewSpeed;
        	float fSpeedDiff = afNewSpeed-gfCurrentSpeed;
        		
        	gfSpeedStep = fSpeedDiff/alSteps;
        	gfTimeStep = afTime/alSteps;
        	
        	AddDebugMessage("Started FadePlayerSpeed - SpeedStep:" + Str(gfSpeedStep) + " TimeStep:" + Str(gfTimeStep), false);
        	
        	AddTimer("", gfTimeStep, nameof(Timer_FadePlayerSpeed));
        }

        void Timer_FadePlayerSpeed(string asTimer)
        {
        	gfCurrentSpeed += gfSpeedStep;
        	if(gfSpeedStep>0.0f && gfCurrentSpeed>gfDestSpeed || 
        		gfSpeedStep<0.0f && gfCurrentSpeed<gfDestSpeed)
        			gfCurrentSpeed = gfDestSpeed;
        			
        	AddDebugMessage("Fading player move speed mul to " + Str(gfCurrentSpeed), false);
        			
        	SetPlayerMoveSpeedMul(gfCurrentSpeed);

        	if(gfCurrentSpeed!=gfDestSpeed)
        		AddTimer("", gfTimeStep, nameof(Timer_FadePlayerSpeed));
        }

        ////////////////////////////////////////////////////////
        // Blurry vision stuff
        bool gbStopBlur = false;
        void Timer_BlurVision(string asTimer)
        {
        	if(gbStopBlur)
        	{
        		gbStopBlur = false;
        		return;
        	}
        	
        	if(RandInt(0, 2) == 1)
        		PlayGuiSound("justine_crazy"+RandInt(1,4)+".ogg", 0.3f);
        	else if(RandInt(0, 1) == 1)
        		PlayGuiSound("justine_breath"+RandInt(1,3)+".ogg", 0.3f);
        	
        	FadeGlobalSoundSpeed(0.5f, 6);
        	
        	SetRadialBlurStartDist(0.4f);
        	FadeRadialBlurTo(0.1f, 0.1f);

        	AddTimer("", RandFloat(0.5f, 4), nameof(Timer_RestoreBlurVision));
        }

        void Timer_RestoreBlurVision(string asTimer)
        {
        	FadeGlobalSoundSpeed(1, 3);
        	
        	FadeRadialBlurTo(0,0.1f);
        	AddTimer("", RandFloat(3.0f, 6.0f), nameof(Timer_BlurVision));
        }

        ////////////////////////////////////////////////////////
        // Head sway stuff
        /*Player has a bit of a random head during the level
         */
        bool gbStopHeadSway = false;
        bool bRoll = true;
        float gfMinHeadSway = 8.0f;
        float gfMaxHeadSway = 20.0f;
        //float gfMinHeadSway = 4.0f;
        //float gfMaxHeadSway = 12.0f;
        void TimerRandomLook(string asTimer)
        {
        	if(gbStopHeadSway)
        	{
        		gbStopHeadSway = false;
        		
        		//RemoveTimer("lookloop");
        	
        		SetPlayerMoveSpeedMul(1);
        		SetPlayerRunSpeedMul(1);
        		FadePlayerFOVMulTo(1, 1);
        		FadeImageTrailTo(0,1.0f);
        		FadePlayerRollTo(0, 0.5f, 2); 
        		
        		return;
        	}
        	int iLook = (int)RandFloat(1,5);
        	
        	if(iLook > 4){
        		SetPlayerRunSpeedMul(0.5f);
        		SetPlayerMoveSpeedMul(1);
        		FadePlayerRollTo(0, 0.5f, 1); 
        		FadePlayerFOVMulTo(1, 1);
        		FadeImageTrailTo(0,1.5f);
        		
        		AddTimer("lookloop", RandFloat(6.0f,8.0f), nameof(TimerRandomLook));
        		return;
        	}
        	
        	FadePlayerFOVMulTo(RandFloat(0.7f,1.3f), RandFloat(0.05f,0.1f));
        	
        	SetPlayerMoveSpeedMul(RandFloat(0.6f,0.9f));
        	SetPlayerRunSpeedMul(RandFloat(0.2f,0.4f));
        	
        	FadeImageTrailTo(RandFloat(0.75f,1.0f),RandFloat(0.75f,1.25f));
        	
        	bRoll = !bRoll;
        	float fSign = 1.0f;
        	
        	if(bRoll==false)
        		fSign = -1.0f;

        	//else
        		//FadePlayerRollTo(RandFloat(-4,-12), RandFloat(0.05f,0.25f), RandFloat(0.5f,1)); 
        		
        	FadePlayerRollTo(RandFloat(gfMinHeadSway,gfMaxHeadSway)*fSign, RandFloat(0.075f,0.35f), RandFloat(0.55f,1.15f)); 
        		
        	AddTimer("lookloop", RandFloat(3.0f,6.0f), nameof(TimerRandomLook));
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // GRAVEYARD PART
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void CollideHeavyPanting(string asParent, string asChild, int alState)
        {
        	string sMessage = "Heavy panting ";
        	if(alState==1)
        	{
        		AddTimer("breath", 1.0f, nameof(TimerBreathSounds));
        		FadePlayerSpeed(0.4f, 2.0f, 10);
        		FadeGlobalSoundSpeed(0.8f, 20);
        		SetRadialBlurStartDist(0.4f);
        		FadeRadialBlurTo(0.2f, 0.01f);
        		sMessage += "enabled!";
        	}
        	else if(alState==-1)
        	{
        		RemoveTimer("breath");
        		FadePlayerSpeed(1.0f, 0.5f, 10);
        		FadeGlobalSoundSpeed(1, 5);
        		FadeRadialBlurTo(0.0f, 0.04f);
        		sMessage += "disabled!";
        	}
        	
        	AddDebugMessage(sMessage, false);
        }

        void TimerBreathSounds(string asTimer)
        {
        	PlaySoundAtEntity("breath", "player_female_breath.snt", "ScriptArea_1", 0.0f, false);
        	AddTimer("breath", 2.0f, nameof(TimerBreathSounds));
        }
        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////////////////
        // Corpse disappearing stuff

        // This sets a flag that indicates that the player is near the grave
        void CollideAreaAroundGrave(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("CollideAreaAroundGrave!: PlayerInArea? " + alState, false);
        	
        	if(alState<0) alState = 0;
        	
        	SetLocalVarInt(asChild, alState);
        	
        	CheckCorpseVanish();
        }

        // This sets a flag that indicates that the player is looking at the grave
        void LookAtGrave(int alGrave, int alState, string asEntity)
        {
        	AddDebugMessage("Looking at grave " + alGrave + "?", false);
        	
        	if(alState<0) alState = 0;
        	
        	SetLocalVarInt("Area_Grave_"+alGrave, alState);
        	
        	CheckCorpseVanish();
        }

        // This checks if any of the corpses should be gone
        void CheckCorpseVanish()
        {
        	for(int i=1; i<=3; ++i)
        	{
        		string sAreaName = "Area_AroundGrave_" + i;
        		string sLookAreaName = "Area_Grave_" + i;
        		
        		bool bPlayerInArea = GetLocalVarInt(sAreaName)==1;
        		bool bPlayerLooking = GetLocalVarInt(sLookAreaName)==1;
        		
        		if(bPlayerInArea==false || bPlayerLooking==false)
        			continue;
        			
        		string sCorpseName = "graveyard_corpse_" + i;
        		
        		SetEnemyDisabled(sCorpseName, false);
        		SetEnemyIsHallucination(sCorpseName, true);
        		
        		VanishEffect(sCorpseName);
        		
        		// Remove callbacks
        		SetEntityPlayerLookAtCallback(sLookAreaName, "", false);
        		RemoveEntityCollideCallback("Player", sAreaName);
        		
        		// And disable further activation
        		SetLocalVarInt(sAreaName, 0);
        		SetLocalVarInt(sLookAreaName, 0);

        		break;
        	}
        }

        void VanishEffect(string sCorpseName)
        {
        	SetPropActiveAndFade(sCorpseName, false, 0.5f);
        	StartScreenShake(0.1f, 0, 0.1f, 0.1f);
        	PlayGuiSound("sanity_flick.snt", 0.5f);
        	PlayGuiSound("scare_wall_scratch_single.snt", 1);
        	PlayGuiSound("player_female_breath.snt", 1);
        }

        void LookAtGrave1(string entity, int alState) 
        {
        	LookAtGrave(1, alState, "");
        }

        void LookAtGrave2(string entity, int alState) 
        {
        	LookAtGrave(2, alState, "");
        }

        void LookAtGrave3(string entity, int alState) 
        {
        	LookAtGrave(3, alState, "");
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // DIZZY MAZE
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void CollideDizzyMaze(string asParent, string asChild, int alState)
        {
        	string sMessage = " maze!";
        	if(alState==1)
        	{
        		PlayMusic("search_grunt.ogg", true, 0.8f, 8.0f, 0, true);
        		
        		sMessage = "Entering" + sMessage;
        		AddTimer("", RandFloat(1.0f, 4.0f), nameof(Timer_BlurVision));
        		AddTimer("lookloop", RandFloat(0.0f, 3.0f), nameof(TimerRandomLook));
        		
        		AddTimer("steps", RandFloat(5.0f, 10.0f), nameof(TimerRandomSteps));
        		PlaySoundAtEntity("creak", "L04_creak.snt", "Area_DizzyMaze", 30, false);
        	}
        	else if(alState==-1)
        	{
        		sMessage = "Exiting" + sMessage;
        		gbStopBlur = true;
        		gbStopHeadSway = true;
        		
        		RemoveTimer("steps");
        		StopSound("creak", 5.0f);
        	}
        	
        	AddDebugMessage(sMessage, false);
        }

        //------------------------------------------------------------------------------------------

        void TimerRandomSteps(string asTimer)
        {
        	PlaySoundAtEntity("", "L04_steps.snt", "Area_DizzyMaze", RandFloat(0.0f, 6.0f), false);
        	StartScreenShake(0.007f, 1, 4, 2);
        	
        	AddTimer("steps", RandFloat(15.0f, 30.0f), nameof(TimerRandomSteps));
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // CRUSH ROOM 
        // ! SEE CLOSE TO LAST PART OF SCRIPT FILE FOR ALL JUSTINE INTERACTION/DIALOGS ! //
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void CollideCrushRoom(string asParent, string asChild, int alState)
        {	
        	AddDebugMessage("Starting crush room", false);
        	
        	StateChangePhono02("doll_entity", 1);	//Start last phonograph recording automatically (coming from the doll).
        	
        	AddTimer("", 0.25f, nameof(Timer_CloseCrushRoom));
        	AddTimer("", 2.5f, nameof(Timer_StartFrontCogwheelsRotation));
        	AddTimer("", 4.0f, nameof(Timer_StartBackCogwheelsRotation));
        }

        void Timer_CloseCrushRoom(string asTimer)
        {
        	SetSwingDoorClosed("castle_5", true, true);
        	SetSwingDoorLocked("castle_5", true, false);
        	
        	PlaySoundAtEntity("", "door_prison_locked.snt", "castle_5", 0.0f, false);
        	
        	/* FIX FOR SECOND DOOR LOCK EVENT
        	 */
        	SetLocalVarInt("JustineInCrushRoom", 1);
        	AddEntityCollideCallback("Player", "Area_CrushRoom", nameof(CollideCrushRoomCheckDoor), false, 0);

        	//AddTimer("", 0.5f, "Timer_CloseBarricade"); //This will happen later as different voices are played depending on what player does.
        }

        void Timer_CloseBarricade(string asTimer)
        {
        	SetSwingDoorClosed("door_barricade_2", true, true); 
        	AddTimer("", 0.5f, nameof(Timer_LockBarricade));
        }

        void Timer_LockBarricade(string asTimer)
        {
        	SetEntityActive("door_barricade_2", false);
        	SetEntityActive("door_barricade_locked_1", true);

        	PlaySoundAtEntity("", "lock_door.snt", "Area_BarricadeLockDust", 0.0f, false);
        	CreateParticleSystemAtEntity("", "ps_hit_dirt.ps", "Area_BarricadeLockDust", false);
        	CreateParticleSystemAtEntity("", "ps_hit_metal.ps", "Area_BarricadeLockDust", false);
        }

        void Timer_StartFrontCogwheelsRotation(string asTimer)
        {
        	AddDebugMessage("Starting front cogwheels", false);
        	
        	AddTimer("", 1.0f, nameof(Timer_ActivateDebrisCogwheelsCollision));
        	
        	PlayMusic("29_amb02.ogg", true, 1, 50.0f, 0, true);
        	
        	PlayGuiSound("27_break", 0.2f);
        	PlaySoundAtEntity("wheelf", "L04_wheel", "spinning_cogwheel_script_3", 16.0f, false);
        	
        	RotatePropToSpeed("spinning_cogwheel_script_1", 0.1f, 3.1416f, 0, 0, 1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_2", 0.1f, 3.1416f, 0, 0, -1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_3", 0.1f, 3.1416f, 0, 0, 1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_4", 0.1f, 3.1416f, 0, 0, -1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_5", 0.1f, 3.1416f, 0, 0, 1, false, "");
        }

        void Timer_ActivateDebrisCogwheelsCollision(string asTimer)
        {
        	SetLocalVarInt("CogwheelDebrisCollide", 1);
        }

        void Timer_StartBackCogwheelsRotation(string asTimer)
        {
        	AddDebugMessage("Starting back cogwheels", false);
        	
        	PlaySoundAtEntity("wheelb", "L04_wheel02", "cogwheel_large_1", 20.0f, false);
        	
        	StartScreenShake(0.01f, 60, 10, 10);
        	
        	RotatePropToSpeed("cogwheel_large_2", 0.06f, 0.9f, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_medium_1", 0.1f, 1.5f, 0, 0, -1, false, "");
        	RotatePropToSpeed("cogwheel_large_1", 0.06f, 0.9f, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_medium_2", 0.1f, 1.5f, 0, 0, -1, false, "");
        	RotatePropToSpeed("cogwheel_medium_3", 0.1f, 1.5f, 0, 0, 1, false, "");
        		
        	AddTimer("", 1.0f, nameof(Timer_StartWallsMovement));
        }

        void Timer_StartWallsMovement(string asTimer)
        {
        	PlaySoundAtEntity("wall1", "L04_movewall", "Area_CrushSafe", 16.0f, false);
        	PlaySoundAtEntity("wall2", "L04_movewall02", "Area_CrushSafe", 16.0f, false);
        	
        	SetMoveObjectStateExt("crush_wall_1", -1.07f, 0.1f, 0.1f, 0.05f, false);
        	SetMoveObjectStateExt("crush_wall_2", 0.98f, 0.1f, 0.055f, 0.05f, false);
        }

        void CollideCrushWallSafeArea(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Wall " + asParent + "colliding!", false);
        	
        	if(GetEntitiesCollide("crush_wall_1", "Area_CrushSafe")==false ||
        		GetEntitiesCollide("crush_wall_2", "Area_CrushSafe")==false)
        		return;
        		
        	SetEntityActive("Area_CrushWallOpen_1", true);
        	SetEntityActive("Area_CrushWallOpen_2", true);
        	
        	RotatePropToSpeed("spinning_cogwheel_script_1", 1.0f, 0, 0, 0, 1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_2", 1.0f, 0, 0, 0, -1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_3", 1.0f, 0, 0, 0, 1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_4", 1.0f, 0, 0, 0, -1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_5", 1.0f, 0, 0, 0, 1, false, "");
        	
        	RotatePropToSpeed("cogwheel_large_2", 1.0f, 0.0f, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_medium_1",1.0f, 0.0f, 0, 0, -1, false, "");
        	RotatePropToSpeed("cogwheel_large_1", 1.0f, 0.0f, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_medium_2", 1.0f, 0.0f, 0, 0, -1, false, "");
        	RotatePropToSpeed("cogwheel_medium_3", 1.0f, 0.0f, 0, 0, 1, false, "");

        	AddTimer("", 20.0f, nameof(Timer_StartCogwheelsReverse));
        	
        	JustinePassOut();
        	
        	PlayMusic("29_amb_end_intense.ogg", true, 1, 1.0f, 0, true);
        }

        void Timer_StartCogwheelsReverse(string asTimer)
        {
        	PlaySoundAtEntity("wall1", "L04_movewall", "Area_CrushSafe", 60.0f, false);
        	PlaySoundAtEntity("wall2", "L04_movewall02", "Area_CrushSafe", 60.0f, false);
        	PlaySoundAtEntity("wheelb", "L04_wheel02", "cogwheel_large_1", 60.0f, false);
        	PlaySoundAtEntity("wheelf", "L04_wheel", "spinning_cogwheel_script_3", 60.0f, false);
        	PlayGuiSound("27_break", 0.2f);
        	
        	StartScreenShake(0.01f, 20, 5, 5);
        	
        	RotatePropToSpeed("spinning_cogwheel_script_1", 0.5f, 3.1416f, 0, 0, -1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_2", 0.5f, 3.1416f, 0, 0, 1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_3", 0.5f, 3.1416f, 0, 0, -1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_4", 0.5f, 3.1416f, 0, 0, 1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_5", 0.5f, 3.1416f, 0, 0, -1, false, "");
        	
        	RotatePropToSpeed("cogwheel_large_2", 0.27f, 0.9f, 0, 0, -1, false, "");
        	RotatePropToSpeed("cogwheel_medium_1", 0.5f, 1.5f, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_large_1", 0.27f, 0.9f, 0, 0, -1, false, "");
        	RotatePropToSpeed("cogwheel_medium_2", 0.5f, 1.5f, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_medium_3", 0.5f, 1.5f, 0, 0, -1, false, "");
        	
        	AddTimer("", 1.0f, nameof(Timer_RestoreCrushWalls));
        }

        void Timer_RestoreCrushWalls(string asTimer)
        {
        	SetMoveObjectStateExt("crush_wall_1", 0.0f, 0.5f, 0.3f, 0.1f, false);
        	SetMoveObjectStateExt("crush_wall_2", 0.0f, 0.5f, 0.3f, 0.1f, false);
        }

        void CollideCrushWallOpenArea(string asParent, string asChild, int alState)
        {
        	SetLocalVarInt(asParent, 1);
        	
        	CheckCrushWallsOpenState();
        }

        void CheckCrushWallsOpenState()
        {
        	if(GetEntitiesCollide("crush_wall_1", "Area_CrushWallOpen_1")==false ||
        		GetEntitiesCollide("crush_wall_2", "Area_CrushWallOpen_2")==false)
        		return;
        	
        	AddDebugMessage("Crush walls open, stopping wheels!", false);
        	
        	StopSound("wall1", 2);
        	StopSound("wall2", 2);
        	StopSound("wheelb", 2);
        	StopSound("wheelf", 2);
        	
        	PlayGuiSound("27_break", 0.3f);
        	
        	RotatePropToSpeed("spinning_cogwheel_script_1", 1.0f, 0, 0, 0, -1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_2", 1.0f, 0, 0, 0, 1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_3", 1.0f, 0, 0, 0, -1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_4", 1.0f, 0, 0, 0, 1, false, "");
        	RotatePropToSpeed("spinning_cogwheel_script_5", 1.0f, 0, 0, 0, -1, false, "");
        	
        	RotatePropToSpeed("cogwheel_large_2", 1.0f, 0.0f, 0, 0, -1, false, "");
        	RotatePropToSpeed("cogwheel_medium_1",1.0f, 0.0f, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_large_1", 1.0f, 0.0f, 0, 0, -1, false, "");
        	RotatePropToSpeed("cogwheel_medium_2", 1.0f, 0.0f, 0, 0, 1, false, "");
        	RotatePropToSpeed("cogwheel_medium_3", 1.0f, 0.0f, 0, 0, -1, false, "");
        	
        	SetEntityActive("Area_CogwheelDamage_1", false);
        	SetEntityActive("Area_CogwheelDamage_2", false);
        	SetEntityActive("Area_CogwheelDamage_3", false);
        	
        	SetLocalVarInt("CogwheelDebrisCollide", 0);
        }

        void CollideDebrisWithCogWheels(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("CogwheelDebrisCollide")!=1) return;
        	
        	AddDebugMessage("Debris in cogwheels!", false);
        	
        	ChangePlayerStateToNormal();
        	PlaySoundAtEntity("", "20_pipe_reject.snt", asParent, 0.0f, false);
        	CreateParticleSystemAtEntity("", "ps_hit_metal.ps", asParent, false);
        		
        	AddPropImpulse(asParent, 0.0f, RandFloat(0.0f, 10.0f), RandFloat(10.0f, 30.0f), "World");
        	SetPlayerActive(true);
        }

        //If player touches cogwheels he will feel their wrath.
        void CogwheelDamagePlayer(string asEntity)
        {
        	if(GetLocalVarInt("CogwheelDebrisCollide")!=1) return;

        	GivePlayerDamage(5.0f, "BloodSplat", true, false);
        	PlayGuiSound("20_hit_wheel", 1.0f);
        }

        //If player tries to kiss cogwheels he will feel their wrath as well. Silly player.
        void CollidePlayerCogWheels(string asParent, string asChild, int alState)
        {
        	CogwheelDamagePlayer("");
        }

        //Sparks as the walls move
        void CollideWallSparks(string asParent, string asChild, int alState)
        {
        	CreateParticleSystemAtEntity("", "ps_wall_move.ps", asChild, false);
        	
        	AddDebugMessage("spark!", false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // STAIRS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void CollideStairs(string asParent, string asChild, int alState)
        {
        	return;	//disabled
        	string sMessage = " stairs!";
        	if(alState==1)
        	{
        		sMessage = "Entering" + sMessage;
        		AddTimer("", RandFloat(3.0f, 6.0f), nameof(Timer_BlurVision));
        		AddTimer("lookloop", RandFloat(2.0f, 5.0f), nameof(TimerRandomLook));
        	}
        	else if(alState==-1)
        	{
        		sMessage = "Exiting" + sMessage;
        		gbStopBlur = true;
        		gbStopHeadSway = true;
        	}
        	
        	AddDebugMessage(sMessage, false);
        }

        bool gbStepsActive = false;
        int glCurrentStep = 1;
        int glLastStairStep = 0;
        float gfStepPeriod = 1.0f;
        void Timer_StairSteps(string asTimer)
        {
        	if(gbStepsActive==false) return;
        	
        	string sCurrentStep = "Area_StairSteps_"+glCurrentStep;
        	
        	if(GetLocalVarInt(sCurrentStep)==0)
        	{
        		PlaySoundAtEntity("", "scare_walk_hallway.snt", "Area_StairSteps_"+glCurrentStep, 0, false);
        		AddDebugMessage("Step at area " + "Area_StairSteps_"+glCurrentStep +"!", false);
        		
        		glCurrentStep++;
        	}
        	else
        	{
        		AddDebugMessage("Looking back, dont play sound", false);
        	}
        	
        	if(glCurrentStep>glLastStairStep)
        	{
        		gbStepsActive = false;
        		return;
        	}
        	
        	AddTimer("StairSteps", gfStepPeriod, nameof(Timer_StairSteps));
        }

        void SetLastStairStep(int alStep, float afStepPeriod)
        {
        	return;
        	AddDebugMessage("Setting last step..." + alStep, false);
        	if(glLastStairStep==alStep) return;
        	
        	gfStepPeriod = afStepPeriod;
        	
        	glLastStairStep = alStep;
        	if(gbStepsActive==false)
        	{
        		AddDebugMessage("Starting steps!", false);
        		gbStepsActive = true;
        		AddTimer("StairSteps", afStepPeriod, nameof(Timer_StairSteps));		
        	}
        }

        void LookAtStairStep(string asEntity, int alState)
        {
        	if(alState<0) alState = 0;
        	
        	SetLocalVarInt(asEntity, alState);
        }

        ////////////////////////////////////////////////////////////////
        // This below is all temp right now!
        void CollideStairsSteps2(string asParent, string asChild, int alState)
        {
        	SetLastStairStep(5, 1.0f);
        }

        void CollideStairsSteps3(string asParent, string asChild, int alState)
        {
        	SetLastStairStep(9, 1.0f);
        }

        void CollideStairsSteps4(string asParent, string asChild, int alState)
        {
        	SetLastStairStep(13, 0.75f);
        }

        void CollideStairsSteps5(string asParent, string asChild, int alState)
        {
        	SetLastStairStep(17, 0.5f);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // MANSION HALL
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        void CollideMansionHall(string asParent, string asChild, int alState)
        {
        	return;
        	AddDebugMessage("Ending triggered!", false);
        	
        	gbStepsActive = false;
        	FadeOut(3.0f);
        	
        	AddTimer("", 3.5f, nameof(Timer_EndingCloseDoor));
        }

        void Timer_EndingCloseDoor(string asTimer)
        {
        	SetPlayerActive(false);
        	SetLanternActive(false, true);
        	
        	float fDelay = 0.0f;
        	if(GetSwingDoorClosed("mansion_1")==false)
        	{
        		AddDebugMessage("Playing close and locking sound", false);
        		SetSwingDoorClosed("mansion_1", true, true);
        		
        		fDelay = 1.0f;
        	}
        	
        	AddTimer("", fDelay, nameof(Timer_EndingStartDialogue));
        }

        void Timer_EndingStartDialogue(string asTimer)
        {
        	SetSwingDoorLocked("mansion_1", true, true);
        	FadeIn(3.0f);
        	
        	LookAroundMansion();
        		
        	// Start Justine-Pierre dialogue - FIXME!!!
        	SetEffectVoiceOverCallback(nameof(DialogueEndCallback));
        	AddEffectVoice("", "", "", "", false, "", 0, 0);
        	AddEffectVoice("", "", "", "", false, "", 0, 0);
        	AddEffectVoice("", "", "", "", false, "", 0, 0);
        	AddEffectVoice("", "", "", "", false, "", 0, 0); 
        }

        void LookAroundMansion()
        {
        	StopPlayerLookAt();
        	int lMansionLook = GetLocalVarInt("MansionLook");
        	if(lMansionLook>4) return;
        	
        	StartPlayerLookAt("Area_MansionLook_" + lMansionLook,  0.5f, 1, nameof(LookAroundMansion));
        	AddLocalVarInt("MansionLook", 1);
        }

        void DialogueEndCallback()
        {
        	FadeOut(3.0f);
        	
        	// Credits - FIXME!!
        	StartCredits("", false, "", "", 0);
        }


        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // PHONOGRAPH MESSAGES 
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        // The phonograph after the graveyard
        void StateChangePhono01(string asEntity, int alState)
        {
        	PlayPhono(4, 1, 6, asEntity);
        	AddEntityCollideCallback("Player", "AreaStopPhono_2", nameof(CollideAreaStopPhono), true, -1);
        }

        // The phonograph attached to the ceiling doll
        void StateChangePhono02(string asEntity, int alState)
        {
        	PlayPhono(4, 2, 1, asEntity);
        }

        // The phonograph when entering level, might be remove if it does not fit here 
        //(should have been in level 3, but no place to have it due to the chase).
        void StateChangePhono03(string asEntity, int alState)
        {
        	PlayPhono(3, 2, 3, asEntity);
        	AddEntityCollideCallback("Player", "AreaStopPhono_1", nameof(CollideAreaStopPhono), true, -1);
        }

        //Stop phongraphs
        void CollideAreaStopPhono(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Stop phono message!", false);
        	StopAllEffectVoices(2.0f);
        }

        // Play the phonograph messages on interact with phonograph
        void PlayPhono(int iLevel, int iSet, int iPart, string sEntity)
        {
        	SetEntityInteractionDisabled(sEntity, true);	//No more interact with phono as it does not do anything.
        	
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
        	StopSound("phono_scrape", 2.0f);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // CHARACTER INTERACTION
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

        //------------------------------------------

        //////////////////////////////////////////////////////////
        // JUSTINE PASSOUT, DOOR AND END EVENTS
        //////////////////////////////////////////////////////////

        //-----------------------------------------

        ////////////////////
        // JUSTINE PASSOUT IN CRUCH ROOM EVENTS
        void JustinePassOut()
        {
        	AddTimer(nameof(TimerEPassOut), 0.1f, nameof(TimerEPassOut));
        }

        void TimerEPassOut(string asTimer)
        {
        	string sEvent = asTimer;	//Do not edit, sets timer loop function name.
        	AddLocalVarInt(sEvent, 1);	//Do not edit, steps through timer loop events.
        	bool bPauseAtStep = false;	//Do not edit, to pause or end the timer loop at specified step.

        	float fEventSpeed = 0.5f;	//Set to preferred default time for the timer loop.

        	switch(GetLocalVarInt(sEvent))
        	{	
        		case 1:
        			FadeGlobalSoundSpeed(0.7f, 20);
        			SetPlayerJumpDisabled(true);
        			SetPlayerCrouchDisabled(true);
        			SetInventoryDisabled(true);
        			FadeOut(4.0f);
        			StartPlayerLookAt("AreaLook_1", 0.5f, 0.5f, "");
        			ChangePlayerSpeed(0.8f);
        			fEventSpeed = 1;
        		break;
        		
        		case 2:
        			FadePlayerRollTo(30, 5, 5);
        			ChangePlayerSpeed(0.7f);
        			PlayGuiSound("justine_wake1.ogg", 0.6f);
        			FadeIn(4.0f);
        		break;
        		
        		case 3:
        			FadeOut(3.0f);
        			StartPlayerLookAt("AreaLook_3", 0.6f, 0.6f, "");
        			ChangePlayerSpeed(0.6f);
        			PlayGuiSound("justine_rapid_breaths.ogg", 0.8f);
        			fEventSpeed = 1;
        		break;
        		
        		case 4:
        			FadeIn(4.0f);
        			StartPlayerLookAt("AreaLook_1", 0.4f, 0.4f, "");
        			ChangePlayerSpeed(0.5f);
        		break;
        		
        		case 5:
        			FadeOut(2.0f);
        			StartPlayerLookAt("AreaLook_4", 0.8f, 0.8f, "");
        			ChangePlayerSpeed(0.4f);
        			fEventSpeed = 1;
        		break;
        		
        		case 6:
        			FadeIn(4.0f);
        			StartPlayerLookAt("AreaLook_2", 1, 1, "");
        			ChangePlayerSpeed(0.3f);
        			fEventSpeed = 1;
        		break;
        		
        		case 7:
        			StopMusic(10.0f, 0);
        			StopSound("wall1", 12);
        			StopSound("wall2", 12);
        			StopSound("wheelf", 12);
        			StopSound("wheelb", 12);
        			FadeOut(1.5f);
        			FadePlayerRollTo(0, 10, 10);
        			SetPlayerCrouching(true);
        			ChangePlayerSpeed(0.2f);
        			PlayGuiSound("player_bodyfall", 1.0f);
        			fEventSpeed = 1;
        		break;
        		
        		case 8:
        			FadeIn(4.0f);
        			ChangePlayerSpeed(0.1f);
        			StartPlayerLookAt("AreaLook_5", 0.8f, 0.8f, "");
        			FadePlayerRollTo(45, 10, 10);
        			MovePlayerHeadPos(0, -0.7f, 0, 2, 0.1f);
        			fEventSpeed = 3;
        		break;
        		
        		case 9:
        			ChangePlayerSpeed(0.05f);
        			FadeOut(2.0f);
        			PlayGuiSound("justine_faint.ogg", 1);
        			fEventSpeed = 2;
        		break;
        		
        		case 10:
        			FadeGlobalSoundSpeed(1,10);
        			SetPlayerActive(false);
        			MovePlayerHeadPos(0, 0, 0, 10, 0.1f);
        			TeleportPlayer("PlayerStartArea_4");
        			PlayGuiSound("justine_breath1.ogg", 0.8f);
        			fEventSpeed = 2;
        		break;
        		
        		case 11:
        			PlayGuiSound("justine_breath2.ogg", 0.6f);
        			fEventSpeed = 2;
        		break;
        		
        		case 12:
        			PlayGuiSound("justine_breath3.ogg", 0.4f);
        			fEventSpeed = 2;
        		break;
        		
        		case 13:
        			SetPlayerCrouching(true);	//repat as MovePlayerHeadPos interfers
        			PlayGuiSound("justine_breath1.ogg", 0.2f);
        			StartPlayerLookAt("AreaLook_2", 1, 1, "");
        			fEventSpeed = 4;
        		break;
        		
        		case 14:
        			AddEntityCollideCallback("Player", "AreaPlayerGotUp", nameof(CollideAreaPlayerGotUp), true, 1); //Check when player gets back up.
        			FadeIn(2.0f);
        			SetPlayerActive(true);
        			ChangePlayerSpeed(0.05f);
        			PlayGuiSound("justine_wake2.ogg", 1);
        			fEventSpeed = 1;

        			if(GetGlobalVarInt("HectorIsDead") == 1 && GetGlobalVarInt("VictorIsDead") == 1 && GetGlobalVarInt("FelixIsDead") == 1)
        			{
        				// All dead...
        				UnlockAchievement("Egotist");
        			}
        			else if(GetGlobalVarInt("HectorIsDead") == 0 && GetGlobalVarInt("VictorIsDead") == 0 && GetGlobalVarInt("FelixIsDead") == 0)
        			{
        				// All alive!
        				UnlockAchievement("Altruist");
        			}
        			else
        			{
        				// Some dead, some alive.
        				UnlockAchievement("Vacillator");
        			}
        		break;
        		
        		case 15:
        			ChangePlayerSpeed(0.1f);
        			SetPlayerLookSpeedMul(0.1f);
        			StartPlayerLookAt("AreaLook_3", 0.1f, 0.1f, "");
        			FadePlayerRollTo(0, 4, 4);
        			fEventSpeed = 1;
        		break;
        		
        		case 16:
        			SetPlayerLookSpeedMul(0.15f);
        			StartPlayerLookAt("AreaLook_5", 0.2f, 0.2f, "");
        			fEventSpeed = 1;
        		break;
        		
        		case 17:
        			SetPlayerLookSpeedMul(0.2f);
        			StartPlayerLookAt("AreaLook_4", 0.3f, 0.3f, "");
        			AddEffectVoice(
        				"justine_end_02", "",
        				"Voice", "justine_end_02", false,
        				"AreaPrisonerVoice", 100, 100
        			);
        			AddEffectVoice(
        				"justine_end_03", "",
        				"Voice", "justine_end_03", false,
        				"AreaPrisonerVoice", 100, 100
        			);
        			fEventSpeed = 2;
        		break;
        		
        		case 18:
        			SetPlayerLookSpeedMul(0.3f);
        			StartPlayerLookAt("AreaLook_6", 0.1f, 0.1f, "");
        			fEventSpeed = 7;
        		break;
        		
        		case 19:
        			StopPlayerLookAt();
        			ChangePlayerSpeed(0.2f);
        			SetPlayerJumpDisabled(false);
        			SetPlayerCrouchDisabled(false);
        		break;
        		
        		case 20:
        			ChangePlayerSpeed(0.3f);
        		break;
        		
        		case 21:
        			ChangePlayerSpeed(0.4f);
        		break;
        		
        		case 22:
        			SetPlayerLookSpeedMul(0.4f);
        		break;
        		
        		case 23:
        			SetPlayerLookSpeedMul(0.5f);
        		break;
        		
        		case 24:
        			AddEffectVoice(
        				"justine_end_04", "",
        				"Voice", "justine_end_04", false,
        				"AreaPrisonerVoice", 100, 100
        			);
        			SetPlayerCrouching(false);
        		break;
        		
        		//////////
        		//End timer when no more steps in the event found.
        		default:
        			bPauseAtStep = true;
        		break;
        	}

        	if(!bPauseAtStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }

        //Change move, run and look movement
        void ChangePlayerSpeed(float fSpeed)
        {
        	SetPlayerMoveSpeedMul(fSpeed);
        	SetPlayerRunSpeedMul(fSpeed);
        	SetPlayerLookSpeedMul(fSpeed);
        }

        ////////////////////
        // JUSTINE WAKES UP, THE BARRED DOOR AND PRISONER VOICES.
        void CollideAreaPlayerGotUp(string asParent, string asChild, int alState)
        {
        	PlayMusic("10_amb.ogg", true, 0.8f, 12.0f, 0, true);
        	
        	//Just incase it bugs out, reset all player properties
        	FadePlayerRollTo(0, 10, 10);
        	SetPlayerJumpDisabled(false);
        	SetPlayerCrouchDisabled(false);
        	SetPlayerCrouching(false);
        	SetInventoryDisabled(false);
        	StopPlayerLookAt();
        	ChangePlayerSpeed(1.0f);	//Make sure player speed is fully restored
        		
        	SetLocalVarInt(nameof(TimerEPassOut), 24);	//Don't play justines "get up message" in TimerEPassOut();
        	SetLocalVarInt("JustineWokeUp", 1); 	//When interacting with bar/door to know if done before or after passing out.
        	
        	//Setup end dialog things
        	AddEntityCollideCallback("Player", "AreaCheckFirstDoor", nameof(CollideAreaCheckFirstDoor), true, -1);
        	SetEntityPlayerInteractCallback("castle_5", nameof(Interactbarreddoor), true);
        	
        	AddTimer(nameof(BangOnDoor), 2, nameof(TimerBangOnDoor));
        }

        //Timer to loop bangs on door
        void TimerBangOnDoor(string asTimer)
        {
        	BangOnDoor();
        }

        //Gentle bang on door from prisoner.
        void BangOnDoor()
        {
        	//Bang effects
        	PlaySoundAtEntity("", "L04_bang_on_door", "AreaPrisonerVoice", RandFloat(0.0f, 0.2f), false);
        	CreateParticleSystemAtEntity("", "ps_break_wood_small", "AreaPrisonerVoice", false);
        	AddPropImpulse("castle_5", 0, 0, -2, "World");
        	
        	AddLocalVarInt("BangDoorCount", 1);
        	
        	//Begin voices on second bang
        	if(GetLocalVarInt("BangDoorCount") == 2)
        		BeginPrisonerVoices();
        	
        	//No more banging on the door
        	if(GetLocalVarInt("BangDoorCount") < 6)
        		AddTimer(nameof(BangOnDoor), RandFloat(0.5f, 1.0f), nameof(TimerBangOnDoor));
        }

        //Play voices from prisoners depending on who of them lives - play nothing if death is total. shame on you player.
        void BeginPrisonerVoices()
        {
        	//Play short victor if both the other prisoners are alive
        	if(GetGlobalVarInt("VictorIsDead") == 0 && GetGlobalVarInt("FelixIsDead") == 0 && GetGlobalVarInt("HectorIsDead") == 0)
        		AddEffectVoice(
        			"victor_saved_01", "",
        			"Voice", "victor_saved_01", true,
        			"AreaPrisonerVoice", 1, 16
        		);
        	
        	
        	//Play medium victor if one of the other prisoners are alive
        	if(GetGlobalVarInt("VictorIsDead") == 0 && (GetGlobalVarInt("FelixIsDead") == 1 && GetGlobalVarInt("HectorIsDead") == 0) || 
        											   (GetGlobalVarInt("FelixIsDead") == 0 && GetGlobalVarInt("HectorIsDead") == 1))
        		AddEffectVoice(
        			"victor_saved_02", "",
        			"Voice", "victor_saved_02", true,
        			"AreaPrisonerVoice", 1, 16
        		);
        	
        	//Play long victor if only he is alive
        	if(GetGlobalVarInt("VictorIsDead") == 0 && GetGlobalVarInt("FelixIsDead") == 1 && GetGlobalVarInt("HectorIsDead") == 1)
        		AddEffectVoice(
        			"victor_saved_03", "",
        			"Voice", "victor_saved_03", true,
        			"AreaPrisonerVoice", 1, 16
        		);
        		
        	//Play Hector if he is alive
        	if(GetGlobalVarInt("HectorIsDead") == 0)
        		AddEffectVoice(
        			"hector_saved_02", "",
        			"Voice", "hector_saved_02", true,
        			"AreaPrisonerVoice", 1, 16
        		);
        	
        	//Play Felix if he is alive
        	if(GetGlobalVarInt("FelixIsDead") == 0)
        		AddEffectVoice(
        			"felix_saved_01", "",
        			"Voice", "felix_saved_01", true,
        			"AreaPrisonerVoice", 1, 16
        		);
        	
        	//All dead, play a random suitor voice instead
        	if(GetGlobalVarInt("VictorIsDead") == 1 && GetGlobalVarInt("FelixIsDead") == 1 && GetGlobalVarInt("HectorIsDead") == 1)	
        	{
        		string[] aEndSuitor = {"alois_idle_03","alois_attack_01","basile_giveup_02","basile_idle_01","malo_attack_02","malo_idle_01"};
        		
        		int iVoice = RandInt(0, 2);
        		
        		AddEffectVoice(
        			aEndSuitor[iVoice], "",
        			"", "", true,
        			"AreaPrisonerVoice", 0, 12
        		);
        		
        		iVoice = RandInt(3, 5);
        		
        		AddEffectVoice(
        			aEndSuitor[iVoice], "",
        			"", "", true,
        			"AreaPrisonerVoice", 0, 12
        		);
        	}
        }

        ////////////////////
        // JUSTINE RESPONSES TO THE BARRED DOOR

        //LOCKING BAR FUNCTIONS
        //Stop player from interacting if he started to pull down the barricade
        void CollideAreaPushDownBar(string asParent, string asChild, int alState)
        {
        	//Player bars the door after waking up - play a voice.
        	if(GetLocalVarInt("JustineWokeUp") == 1)
        	{
        		StopAllEffectVoices(0);
        		AddEffectVoice(
        			"justine_end_door_01", "",
        			"Voice", "justine_end_door_01", false,
        			"AreaPrisonerVoice", 100, 100
        		);
        		
        		SetEntityPlayerInteractCallback("castle_5", "", false);	//Disable message on interact of door, too much with both.
        	}
        	
        	SetLocalVarInt("PlayerLockedBar", 1);
        	
        	SetEntityInteractionDisabled(asParent, true);
        	AddPropImpulse(asParent, -10, -10, 0, "World");
        	
        	AddDebugMessage("Closing bar!", false);
        }

        //Lock the barricade as it slams down
        void CollideAreaLockBar(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("JustineWokeUp") == 0)
        		SetLocalVarInt("ClosedBeforeFaint", 1);  //Do NOT allow final message to be played if going back, from and back to door until after faint sequence.

        	AddEntityCollideCallback("Player", "AreaDoor2ndVisit", nameof(CollideAreaDoor2ndVisit), false, -1);
        	
        	AddTimer("", 0.1f, nameof(Timer_CloseBarricade));
        }

        //CHECK JUSTINE ACTIONS AND REACTIONS AFETR WAKING UP AND DOOR BANGS
        //Justine goes away from the door, if door not barred trigger a "barring" event
        void CollideAreaCheckFirstDoor(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("PlayerLockedBar") == 0)
        		JustineAutoCloseBar();

        	SetLocalVarInt("ClosedBeforeFaint", 0);  //Allow final message to be played if going back, from and back to door.
        	
        	//Make comments for looking at stuff active.
        	SetEntityActive("AreaVoiceCeiling_1", true); 
        	SetEntityActive("AreaVoiceCeiling_2", true); 
        	SetEntityActive("AreaVoiceCeiling_3", true);
        	SetEntityActive("AreaVoiceDoll", true);
        	SetEntityActive("AreaVoiceMachinery", true);
        	
        	AddDebugMessage("Justine goes away from door!", false);
        }

        //Justine turns around, looks at door, walks towards it and then the bar auto closes.
        void JustineAutoCloseBar()
        {
        	ChangePlayerSpeed(0.5f);
        	SetPlayerRunSpeedMul(0);
        	SetPlayerLookSpeedMul(0.2f);
        	SetPlayerJumpDisabled(true);
        	SetPlayerCrouchDisabled(true);
        	SetInventoryDisabled(true);
        	StartPlayerLookAt("AreaLookDoor", 4, 4, "");
        	AddTimer("1", 0.5f, nameof(TimerJustineLockBar));
        	AddTimer("2", 3.0f, nameof(TimerJustineLockBar));
        	AddTimer("3", 3.5f, nameof(TimerJustineLockBar));
        	AddTimer("", 4.0f, nameof(TimerJustineLockBarEnd));
        }

        void TimerJustineLockBar(string asTimer)
        {
        	if(asTimer == "1")
        	{
        		StartPlayerLookAt("AreaPushDownBar", 2, 2, "");
        		SetEntityInteractionDisabled("door_barricade_2", true);
        		AddTimer("automove", 0.01f, nameof(TimerMoveForward));
        	}
        	
        	else if(asTimer == "2")
        	{		
        		AddPropImpulse("door_barricade_2", -10, -10, 0, "World");
        		StartPlayerLookAt("Area_BarricadeLockDust", 2, 2, "");
        		StopAllEffectVoices(0);
        	}
        	
        	else if(asTimer == "3")
        		RemoveTimer("automove");
        	
        }

        void TimerJustineLockBarEnd(string asTimer)
        {
        	StopPlayerLookAt();
        	ChangePlayerSpeed(1);
        	SetPlayerJumpDisabled(false);
        	SetPlayerCrouchDisabled(false);
        	SetInventoryDisabled(false);
        }

        void TimerMoveForward(string asTimer)
        {
        	MovePlayerForward(2);
        	AddTimer("automove", 0, nameof(TimerMoveForward));
        	AddDebugMessage("Move forward!", false);
        }

        //If justine did not go away, then play voices if she interacts with the door
        void Interactbarreddoor(string asEntity)
        {
        	//Bar is down so she only comments that is is safe and sound.
        	if(GetLocalVarInt("PlayerLockedBar") == 1)
        	{
        		StopAllEffectVoices(0);
        		AddEffectVoice(
        			"justine_end_door_02", "",
        			"Voice", "justine_end_door_02", false,
        			"AreaPrisonerVoice", 100, 100
        		);
        	}
        	
        	//Bar the door if player interacts with door - voice says better safe than sorry in CollideAreaPushDownBar();
        	if(GetLocalVarInt("PlayerLockedBar") == 0)
        	{
        		SetEntityInteractionDisabled("door_barricade_2", true);
        		
        		ChangePlayerSpeed(0.2f);
        		StartPlayerLookAt("AreaPushDownBar", 1,1, "");
        		
        		AddTimer("1", 1.0f, nameof(TimerLookAtBar));
        		AddTimer("2", 2.0f, nameof(TimerLookAtBar));
        	}
        	
        	SetLocalVarInt("ClosedBeforeFaint", 0);  //Allow final message to be played if going back, from and back to door.
        }
        void TimerLookAtBar(string asTimer)
        {
        	if(asTimer == "1")
        	{
        		AddPropImpulse("door_barricade_2", -10, -10, 0, "World");
        		
        		ChangePlayerSpeed(0.6f);
        		StartPlayerLookAt("AreaLookDoor", 1,1, "");
        	}
        	
        	if(asTimer == "2")
        	{
        		ChangePlayerSpeed(1);
        		StopPlayerLookAt();
        	}
        }

        //A collision for if the player goes back to the door after it is locked and barred to trigger a voice.
        void CollideAreaDoor2ndVisit(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("ClosedBeforeFaint") == 1) return;
        	
        	RemoveEntityCollideCallback(asParent, asChild);
        	
        	AddTimer("fix", 1.0f, nameof(TimerAddCallback));
        	
        	AddDebugMessage("ready final door message", false);
        }

        //Because adding it in the collide callback removes it.
        void TimerAddCallback(string asTimer)
        {
        	AddEntityCollideCallback("Player", "AreaDoor2ndVisit", nameof(CollideAreaDoor2ndVisit02), false, 1);
        }

        //When colliding with the area and looking at the door, play voice.
        void CollideAreaDoor2ndVisit02(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("LookAtFirstDoor") != 1) return;
        	
        	RemoveEntityCollideCallback(asParent, asChild);
        	
        	StopAllEffectVoices(0);
        	AddEffectVoice(
        		"justine_end_look_03", "",
        		"Voice", "justine_end_look_03", false,
        		"AreaPrisonerVoice", 100, 100
        	);	
        	
        	AddDebugMessage("Playing final door message", false);
        }

        //Only play message if player looks at door.
        void LookAtDoorArea01(string asEntity, int alState)
        {
        	SetLocalVarInt("LookAtFirstDoor", alState);
        }

        //LOOKING AT OBJECT AND SPEAKING ABOUT THEM
        //Cogwheels
        void LookAtMachine(string asEntity, int alState)
        {
        	if(alState == 1)
        		AddTimer("justine_end_look_02", 1.5f, nameof(TimerSpeakObject));
        		
        	else if(alState == -1)
        		RemoveTimer("justine_end_look_02");
        }

        //Hanging doll
        void LookAtDoll(string asEntity, int alState)
        {
        	if(alState == 1)
        		AddTimer("justine_end_look_01", 1.5f, nameof(TimerSpeakObject));
        		
        	else if(alState == -1)
        		RemoveTimer("justine_end_look_01");	
        }

        //Looking at the light through ceiling windows
        void LookAtCeiling(string asEntity, int alState)
        {
        	if(alState == 1)
        		AddTimer("justine_end_look_04", 1.5f, nameof(TimerSpeakObject));
        		
        	else if(alState == -1)
        		RemoveTimer("justine_end_look_04");	
        }

        //Speak the different voices.
        void TimerSpeakObject(string asTimer)
        {
        	StopAllEffectVoices(0);
        	AddEffectVoice(
        		asTimer, "",
        		"Voice", asTimer, false,
        		"AreaPrisonerVoice", 100, 100
        	);	
        	
        	if(asTimer == "justine_end_look_04")
        	{
        		SetEntityActive("AreaVoiceCeiling_1", false); 
        		SetEntityActive("AreaVoiceCeiling_2", false); 
        		SetEntityActive("AreaVoiceCeiling_3", false);
        	}
        }

        ////////////////////
        // JUSTINE CLOSING SECOND DOOR
        //Quick fix for not locking oneself into the crush room, still possible but best fix without editing the map.
        void CollideCrushRoomCheckDoor(string asParent, string asChild, int alState)
        {
        	SetLocalVarInt("JustineInCrushRoom", alState);
        	
        	AddDebugMessage("Var: "+GetLocalVarInt("JustineInCrushRoom"), false);
        }

        //If door has been opened then lock it if player closes it and play voice.
        void CollideAreaDoorLock(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("SecondDoorOpened") == -1 && GetLocalVarInt("JustineInCrushRoom") == -1)
        	{
        		SetSwingDoorClosed("castle_6", true, true);
        		SetSwingDoorLocked("castle_6", true, true);
        		
        		StopAllEffectVoices(0);
        		AddEffectVoice(
        			"justine_end_door_03", "",
        			"Voice", "justine_end_door_03", false,
        			"AreaPrisonerVoice", 100, 100
        		);
        		
        		SetEntityActive(asChild, false);
        		
        		return;
        	}
        	
        	//Set door state
        	SetLocalVarInt("SecondDoorOpened", alState);
        		
        	AddDebugMessage("Second door state: "+alState, false);
        }

        //Check if player did not close the second door, if not do a forced sequence and speak another voice.
        void CollideAreaCheckSecondDoorLocked(string asParent, string asChild, int alState)
        {
        	if(GetSwingDoorLocked("castle_6")) return;
        	
        	SetEntityActive("AreaDoorLock", false);
        	
        	JustinAutoCloseDoor();
        }

        void JustinAutoCloseDoor()
        {
        	SetPlayerJumpDisabled(true);
        	SetPlayerCrouchDisabled(true);
        	SetInventoryDisabled(true);
        	ChangePlayerSpeed(0.4f);
        	SetPlayerRunSpeedMul(0);
        	SetPlayerLookSpeedMul(0.2f);
        	StartPlayerLookAt("AreaLookSecondDoor", 3, 3, "");
        	AddTimer("1", 1.0f, nameof(TimerJustineLockDoor));
        	AddTimer("2", 2.0f, nameof(TimerJustineLockDoor));
        	AddTimer("3", 3.0f, nameof(TimerJustineLockDoor));
        	AddTimer("", 3.2f, nameof(TimerJustineLockDoorEnd));
        }

        void TimerJustineLockDoor(string asTimer)
        {
        	if(asTimer == "1")
        	{
        		StartPlayerLookAt("castle_6", 3, 3, "");
        		SetEntityInteractionDisabled("castle_6", true);
        		AddTimer("automove", 0.1f, nameof(TimerMoveForward));
        	}
        	
        	else if(asTimer == "2")
        	{		
        		StartPlayerLookAt("AreaLookSecondDoor", 3, 3, "");
        		AddPropImpulse("castle_6", -3, 0, 3, "World");
        		StopAllEffectVoices(0);
        		AddEffectVoice(
        			"justine_end_door_04", "",
        			"Voice", "justine_end_door_04", false,
        			"AreaPrisonerVoice", 100, 100
        		);
        	}
        	
        	else if(asTimer == "3")
        	{
        		SetSwingDoorLocked("castle_6", true, true);
        		SetEntityInteractionDisabled("castle_6", false);
        		RemoveTimer("automove");
        	}
        	
        }

        void TimerJustineLockDoorEnd(string asTimer)
        {
        	SetPlayerJumpDisabled(false);
        	SetPlayerCrouchDisabled(false);
        	SetInventoryDisabled(false);
        	StopPlayerLookAt();
        	ChangePlayerSpeed(1);
        }

        ////////////////////
        // JUSTINE WALKING UP THE STAIRS AND FINAL END
        //As entering stairs, clarice starts talking from above and voice + events are begun.
        void CollideAreaClarice_1(string asParent, string asChild, int alState)
        {
        	SetInventoryDisabled(true);
        	SetPlayerCrouchDisabled(true);
        	SetPlayerJumpDisabled(true);
        	StartPlayerLookAt("AreaLookLastDoor", 1, 1, "");
        	ChangePlayerSpeed(0.9f);
        	SetPlayerRunSpeedMul(0);
        	AddTimer(nameof(TimerEEnding), 1, nameof(TimerEEnding));
        	StopAllEffectVoices(0);
        	
        	//Begin ending conversation
        	AddEffectVoice(
        		"clarice_end_01", "",
        		"Voice", "clarice_end_01", true,
        		"AreaLookLastDoor", 10, 60
        	);
        	AddEffectVoice(
        		"justine_end_05", "",
        		"Voice", "justine_end_05", false,
        		"AreaLookLastDoor", 100, 100
        	);
        	AddEffectVoice(
        		"justine_end_06", "",
        		"Voice", "justine_end_06", false,
        		"AreaLookLastDoor", 100, 100
        	);
        	AddEffectVoice(
        		"clarice_end_02", "",
        		"Voice", "clarice_end_02", true,
        		"AreaLookLastDoor", 10, 60
        	);
        	
        	//Regular ending
        	if(GetGlobalVarInt("HasAptNote") == 1)
        	{
        		AddEffectVoice(
        			"justine_end_patrol_01", "",
        			"Voice", "justine_end_patrol_01", false,
        			"AreaLookLastDoor", 100, 100
        		);
        		AddEffectVoice(
        			"clarice_end_patrol_01", "",
        			"Voice", "clarice_end_patrol_01", true,
        			"AreaLookLastDoor", 10, 40
        		);
        		AddEffectVoice(
        			"justine_end_patrol_02", "",
        			"Voice", "justine_end_patrol_02", false,
        			"AreaLookLastDoor", 100, 100
        		);
        	}
        	
        	//That something special ending.
        	else
        	{
        		AddEffectVoice(
        			"clarice_end_proper_01", "",
        			"Voice", "clarice_end_proper_01", true,
        			"AreaLookLastDoor", 10, 50
        		);
        		AddEffectVoice(
        			"justine_end_proper_01", "",
        			"Voice", "justine_end_proper_01", false,
        			"AreaLookLastDoor", 100, 100
        		);
        		AddEffectVoice(
        			"clarice_end_proper_02", "",
        			"Voice", "clarice_end_proper_02", true,
        			"AreaLookLastDoor", 10, 40
        		);
        		AddEffectVoice(
        			"justine_end_proper_02", "",
        			"Voice", "justine_end_proper_02", false,
        			"AreaLookLastDoor", 100, 100
        		);
        	}
        }

        //Player looses more and more control over justine during the dialog with clarice.
        void TimerEEnding(string asTimer)
        {
        	string sEvent = asTimer;	//Do not edit, sets timer loop function name.
        	AddLocalVarInt(sEvent, 1);	//Do not edit, steps through timer loop events.
        	bool bPauseAtStep = false;	//Do not edit, to pause or end the timer loop at specified step.

        	float fEventSpeed = 1;	//Set to preferred default time for the timer loop.

        	switch(GetLocalVarInt(sEvent))
        	{	//////////
        		//First step in the event - replace with step comment.
        		case 1:
        			StartPlayerLookAt("AreaLookLastDoor", 1.5f, 1.5f, "");
        			ChangePlayerSpeed(0.8f);
        			SetPlayerRunSpeedMul(0);
        			AddPropImpulse("mansion_1", 0.2f, 0, 0, "World");
        		break;
        		
        		case 2:
        			StartPlayerLookAt("AreaLookLastDoor", 2, 2, "");
        			ChangePlayerSpeed(0.75f);
        			SetPlayerRunSpeedMul(0);
        			AddPropImpulse("mansion_1", 0.2f, 0, 0, "World");
        		break;
        		
        		case 3:
        			StartPlayerLookAt("AreaLookLastDoor", 2.5f, 2.5f, "");
        			ChangePlayerSpeed(0.7f);
        			SetPlayerRunSpeedMul(0);
        			AddTimer("move", 0.1f, nameof(TimerMoveEnding));
        		break;
        		
        		case 4:
        			StartPlayerLookAt("AreaLookLastDoor", 2.5f, 2.5f, "");
        			ChangePlayerSpeed(0.65f);
        			SetPlayerRunSpeedMul(0);
        		break;
        		
        		case 5:
        			StartPlayerLookAt("AreaLookLastDoor", 3, 3, "");
        			ChangePlayerSpeed(0.6f);
        			SetPlayerRunSpeedMul(0);
        		break;
        		
        		case 6:
        			StartPlayerLookAt("AreaLookLastDoor", 4, 4, "");
        			ChangePlayerSpeed(0.55f);
        			SetPlayerRunSpeedMul(0);
        		break;
        		
        		case 7:
        			ChangePlayerSpeed(0.5f);
        			SetPlayerRunSpeedMul(0);
        			SetPlayerLookSpeedMul(0.2f);
        			StartPlayerLookAt("AreaLookLastDoor", 5, 5, "");
        		break;
        		
        		case 8:
        			StartPlayerLookAt("AreaLookLastDoor", 6, 6, "");
        			SetPlayerLookSpeedMul(0.1f);
        		break;
        		
        		case 9:
        			SetPlayerLookSpeedMul(0.05f);
        		break;
        		
        		case 10:
        			SetPlayerLookSpeedMul(0);
        			SetPlayerActive(false);
        			fEventSpeed = 4;
        		break;
        		
        		case 11:
        			AddPropImpulse("mansion_1", 1, 0, 0, "World");
        		break;
        	
        		//////////
        		//End timer when no more steps in the event found.
        		default:
        			bPauseAtStep = true;
        		break;
        	}

        	if(!bPauseAtStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }

        //Loop for automated player movement.
        void TimerMoveEnding(string asTimer)
        {
        	MovePlayerForward(3);
        	AddTimer("move", 0, nameof(TimerMoveEnding));
        }

        //As player reaches the final door, do a last sequence where she goes into room and the door closes (probably by clarice the rock statue). 
        void CollideAreaUpstairs(string asParent, string asChild, int alState)
        {
        	StartPlayerLookAt("AreaUpstairsLook_1", 1, 1, "");

        	AddTimer("1", 1, nameof(TimerUpEnding));
        	AddTimer("2", 2, nameof(TimerUpEnding));
        	AddTimer("3", 3, nameof(TimerUpEnding));
        	AddTimer("4", 4, nameof(TimerUpEnding));
        	AddTimer("5", 7, nameof(TimerUpEnding));
        	AddTimer("6", 12, nameof(TimerUpEnding));
        }
        void TimerUpEnding(string asTimer)
        {
        	if(asTimer == "1")
        	{
        		SetPlayerMoveSpeedMul(0.4f);
        	}
        	if(asTimer == "2")
        	{
        		StartPlayerLookAt("AreaUpstairsLook_2", 1, 1, "");
        		SetPlayerMoveSpeedMul(0.3f);
        	}
        	if(asTimer == "3")
        	{
        		StartPlayerLookAt("AreaUpstairsLook_3", 0.5f, 0.5f, "");
        		SetSwingDoorClosed("mansion_1", true, true);
        		SetSwingDoorLocked("mansion_1", true, true);
        		FadeOut(4);
        	}
        	if(asTimer == "4")
        	{
        		SetPlayerMoveSpeedMul(0.1f);
        	}
        	if(asTimer == "5")
        	{
        		SetPlayerActive(false);
        		RemoveTimer("move");
        		StopMusic(5.0f, 0);
        		StopSound("Sound_18", 5.0f);
        	}
        	if(asTimer == "6")
        		StartCredits("amb_extra01.ogg",true,"Ending","MainCredits", -1);
        }

        //------------------------------------------------------------------------------------------
        //Random shake in the graveyard
        void CollideAreaShake(string asParent, string asChild, int alState)
        {
        	if(alState == 1)
        	{
        		AddTimer("shake", RandFloat(1.0f, 5.0f), nameof(TimerShake));
        	}
        	
        	else if(alState == -1)
        	{
        		RemoveTimer("shake");
        	}
        }

        void TimerShake(string asTimer)
        {
        	PlayGuiSound("24_rock.snt", 1);
        	StartScreenShake(0.005f, 1, 4, 4);
        	
        	AddTimer("shake", RandFloat(15.0f, 30.0f), nameof(TimerShake));
        	
        	AddDebugMessage("Shaking", false);
        }

        //------------------------------------------------------------------------------------------
        //Touch grave and angel
        void InteractTouchGrave(string asEntity)
        {
        	StartEffectEmotionFlash("L04Final", "FatherGrave", "flashback_flash.snt");
        }
        void InteractTouchAngel(string asEntity)
        {
        	StartEffectEmotionFlash("L04Final", "AngelStatue", "flashback_flash.snt");
        }

        //------------------------------------------------------------------------------------------

        void PickUpNote(string asEntity, string asType)
        {
        	AddKeyPart(0x7342145F);
        	AddKeyPart(0x13af5131);
        	AddDebugMessage("PickNote!\n", false);
        }

        //------------------------------------------------------------------------------------------

        void CollideDollChainSounds(string asParent, string asChild, int alState)
        {
        	if(RandInt(0, 1) == 1)
        		PlaySoundAtEntity("", "general_chain_rattle_single.snt", asChild, RandFloat(1.0f, 4.0f), false);
        }

        //------------------------------------------------------------------------------------------

        //////////////////////////////////////////////////////////
        // MAIN CALLBACKS
        //////////////////////////////////////////////////////////

        //------------------------------------------------------------------------------------------

        public override void OnStart()
        {
        	////////////////////////////
        	// SET LEVEL NAME
        	SetMapDisplayNameEntry("L04Final");

        	PlayMusic("18_amb.ogg", true, 0.7f, 2.0f, 0, true);
        	
        	GiveSanityBoost();
        	
        	////////////////////////////
        	// Was Felix saved?
        	if(GetGlobalVarInt("FelixIsDead")==1)
        	{
        		AddKeyPart(unchecked((int)0xac1f1eAF));
        		AddKeyPart(0x1eff3129);
        	}
        	
        	////////////////////////////
        	// Variables
        	
        	////////////////////////////
        	// Callbacks
        	
        	// Graveyard
        	AddEntityCollideCallback("Player", "AreaShake", nameof(CollideAreaShake), false, 0);
        	
        	AddEntityCollideCallback("Player", "Area_FathersGrave", nameof(CollideHeavyPanting), false, 0);
        	AddEntityCollideCallback("Player", "Area_AngelStatue", nameof(CollideHeavyPanting), false, 0);
        	AddEntityCollideCallback("Player", "Area_AroundGrave_1", nameof(CollideAreaAroundGrave), false, 0);
        	AddEntityCollideCallback("Player", "Area_AroundGrave_2", nameof(CollideAreaAroundGrave), false, 0);
        	AddEntityCollideCallback("Player", "Area_AroundGrave_3", nameof(CollideAreaAroundGrave), false, 0);
        	
        	SetEntityPlayerLookAtCallback("Area_Grave_1", nameof(LookAtGrave1), false);
        	SetEntityPlayerLookAtCallback("Area_Grave_2", nameof(LookAtGrave2), false);
        	SetEntityPlayerLookAtCallback("Area_Grave_3", nameof(LookAtGrave3), false);
        	SetEnemyDisabled("graveyard_corpse_1", true);
        	SetEnemyDisabled("graveyard_corpse_2", true);
        	SetEnemyDisabled("graveyard_corpse_3", true);
        	// ! SEE END OF ONSTART FOR PRISONER FIX TO MATCH RESULT OF EACH TRIAL. ! //
        	
        	
        	// Maze
        	AddEntityCollideCallback("Player", "Area_DizzyMaze", nameof(CollideDizzyMaze), false, 0);
        	
        	// Crush room
        	AddEntityCollideCallback("doll_entity", "AreaChain_1", nameof(CollideDollChainSounds), false, -1);
        	AddEntityCollideCallback("doll_entity", "AreaChain_2", nameof(CollideDollChainSounds), false, -1);
        	
        	for(int i=1;i<=48;i++) 
        		AddEntityCollideCallback("crush_wall_1", "AreaSpark_"+i, nameof(CollideWallSparks), false, 0);
        	for(int i=49;i<=96;i++) 
        		AddEntityCollideCallback("crush_wall_2", "AreaSpark_"+i, nameof(CollideWallSparks), false, 0);
        		
        	AddEntityCollideCallback("Player", "Area_CrushRoom", nameof(CollideCrushRoom), true, 1);
        	AddEntityCollideCallback("crush_wall_1", "Area_CrushSafe", nameof(CollideCrushWallSafeArea), true, 1);
        	AddEntityCollideCallback("crush_wall_2", "Area_CrushSafe", nameof(CollideCrushWallSafeArea), true, 1);
        	AddEntityCollideCallback("crush_wall_1", "Area_CrushWallOpen_1", nameof(CollideCrushWallOpenArea), true, 1);
        	AddEntityCollideCallback("crush_wall_2", "Area_CrushWallOpen_2", nameof(CollideCrushWallOpenArea), true, 1);
        	
        	AddEntityCollideCallback("crushdebris_*", "Area_CrushRoomCogWheels", nameof(CollideDebrisWithCogWheels), false, 1);
        	SetMoveObjectState("crush_wall_2", 0.4f);
        	
        	AddEntityCollideCallback("Player", "Area_CogwheelDamage_1", nameof(CollidePlayerCogWheels), false, 1);
        	AddEntityCollideCallback("Player", "Area_CogwheelDamage_2", nameof(CollidePlayerCogWheels), false, 1);
        	AddEntityCollideCallback("Player", "Area_CogwheelDamage_3", nameof(CollidePlayerCogWheels), false, 1);
        	
        	AddEntityCollideCallback("door_barricade_2", "AreaPushDownBar", nameof(CollideAreaPushDownBar), true, 1);
        	AddEntityCollideCallback("door_barricade_2", "AreaLockBar", nameof(CollideAreaLockBar), true, 1);
        	
        	// Stairs
        	AddEntityCollideCallback("Player", "AreaCheckSecondDoorLocked", nameof(CollideAreaCheckSecondDoorLocked), true, 1);
        	AddEntityCollideCallback("castle_6", "AreaDoorLock", nameof(CollideAreaDoorLock), false, 0);

        	AddEntityCollideCallback("Player", "AreaClarice_1", nameof(CollideAreaClarice_1), true, 1);
        	AddEntityCollideCallback("Player", "AreaUpstairs", nameof(CollideAreaUpstairs), true, 1);
        	
        	
        	AddEntityCollideCallback("Player", "Area_Stairs", nameof(CollideStairs), false, 0);
        	AddEntityCollideCallback("Player", "Area_TriggerBehindNoise_2", nameof(CollideStairsSteps2), true, 0);
        	AddEntityCollideCallback("Player", "Area_TriggerBehindNoise_3", nameof(CollideStairsSteps3), true, 0);
        	AddEntityCollideCallback("Player", "Area_TriggerBehindNoise_4", nameof(CollideStairsSteps4), true, 0);
        	AddEntityCollideCallback("Player", "Area_TriggerBehindNoise_5", nameof(CollideStairsSteps5), true, 0);
        	
        	// Mansion hall
        	AddEntityCollideCallback("Player", "Area_MansionHall", nameof(CollideMansionHall), true, 1);
        	
        	//Stop sounds
        	for(int i=1;i<=34;i++) 
        		StopSound("torch_static01_"+i+"_Sound_1", 0.0f);
        		
        	// Entities
        	SetEntityInteractionDisabled("doll_entity", true);
        	
        	////////////////////////////
        	// Entity connections
        		
        	////////////////////////////
        	// Item Callbacks
        	
        	///////////////////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		//SetEntityActive("Area_CrushRoom", false); //To not be disturbed by the phono message.
        		//JustinePassOut();
        		
        		SetGlobalVarInt("HasAptNote", 1);
        		
        		SetGlobalVarInt("HectorIsDead", 0);
        		SetGlobalVarInt("VictorIsDead", 0);
        		SetGlobalVarInt("FelixIsDead", 1);
        	}
        	
        	//Fix for corpses, they should only be there if the prisoners died.
        	if(GetGlobalVarInt("HectorIsDead") == 0)
        	{
        		SetEntityActive("Area_AroundGrave_1", false);
        		SetEntityActive("Area_Grave_1", false);
        		SetEntityActive("graveyard_corpse_1", false);
        	}
        	
        	
        	if(GetGlobalVarInt("VictorIsDead") == 0)
        	{
        		SetEntityActive("Area_AroundGrave_2", false);
        		SetEntityActive("Area_Grave_2", false);
        		SetEntityActive("graveyard_corpse_2", false);
        	}
        	
        	if(GetGlobalVarInt("FelixIsDead") == 0)
        	{
        		SetEntityActive("Area_AroundGrave_3", false);
        		SetEntityActive("Area_Grave_3", false);
        		SetEntityActive("graveyard_corpse_3", false);
        	}
        	
        	PreloadSound("L04_bang_on_door"); PreloadSound("player_female_breath"); PreloadSound("door_prison_locked"); PreloadSound("lock_door");
        	PreloadSound("L04_wheel"); PreloadSound("L04_wheel02"); PreloadSound("L04_movewall"); PreloadSound("L04_movewall02");
        	PreloadSound("20_pipe_reject"); PreloadSound("20_hit_wheel"); PreloadSound("ptest_phono_loop"); PreloadSound("24_rock");
        	PreloadSound("justine_wake1"); PreloadSound("justine_rapid_breaths"); PreloadSound("player_bodyfall"); PreloadSound("justine_faint");
        	PreloadSound("27_break");
        }

        //------------------------------------------------------------------------------------------

        	
        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);
    }
}
