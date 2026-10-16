using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/17_control_room.hps; journal: docs/ScriptPort/ch02/17_control_room.md.</summary>
    [HplMapScript("maps/main/ch02/17_control_room.map")]
    public class ControlRoom : HplScript
    {
        /////////////////////////////////////////
        // HERBERT VISION AREAS

        // Vision 1 (Random_Mirage01)
        void CollideWithHerbertVision_1()
        {
        	SetGlobalVarInt("LastHerbertVisionTriggered", 1);
        	SetLocalVarInt("TriggeredVisionAtStart", 1);

        	SetUpHerbertVisionAreas();
        }

        // Vision 2 (Random_Mirage02)
        void CollideWithHerbertVision_2()
        {
        	SetGlobalVarInt("LastHerbertVisionTriggered", 2);

        	SetUpHerbertVisionAreas();
        }

        void SetUpHerbertVisionAreas()
        {
        	int lLastVisionTriggered = GetGlobalVarInt("LastHerbertVisionTriggered");
        	int lTriggeredVisionAtStart = GetLocalVarInt("TriggeredVisionAtStart");
        	int lFirstPuzzleSolved = GetLocalVarInt("FirstPuzzleSolved");

        	AddDebugMessage("Last vision triggered:" + lLastVisionTriggered, false);
        	AddDebugMessage("TriggeredVisionAtStart:" + lTriggeredVisionAtStart, false);
        	AddDebugMessage("FirstPuzzleSolved:" + lFirstPuzzleSolved, false);

        	// Initial setup : all disabled
        	SetEntityActive("HerbertVisionArea_0_1", false);
        	SetEntityActive("HerbertVisionArea_0_2", false);
        	SetEntityActive("HerbertVisionArea_1_2", false);
        	SetEntityActive("HerbertVisionArea_2_2", false);
        	SetEntityActive("HerbertVisionArea_3_2", false);

        	// No visions have been triggered
        	if(lLastVisionTriggered==0)
        	{
        		AddDebugMessage("Setting up vision 1", false);
        		AddDebugMessage("Activating vision at start", false);
        		SetEntityActive("HerbertVisionArea_0_1", true);
        	}
        	// First vision was triggered
        	else if(lLastVisionTriggered==1)
        	{
        		AddDebugMessage("Setting up vision 2", false);

        		if(lTriggeredVisionAtStart==0)
        		{
        			AddDebugMessage("Activating vision at start", false);
        			SetEntityActive("HerbertVisionArea_0_2", true);
        		}
        		else
        		{
        			if(lFirstPuzzleSolved!=0)
        			{
        				string[] vPuzzleNames = { "Weights", "Piston", "Pipes" };
        				// AngelScript aborts this call at the first out-of-range array access.
        				if(lFirstPuzzleSolved < 1 || lFirstPuzzleSolved > vPuzzleNames.Length) return;
        				AddDebugMessage("Activating vision at " + vPuzzleNames[lFirstPuzzleSolved-1] + " puzzle zone", false);
        				SetEntityActive("HerbertVisionArea_" + lFirstPuzzleSolved + "_2", true);
        			}
        			else
        			{
        				AddDebugMessage("No vision activated", false);
        			}
        		}
        	}
        	else
        	{
        		AddDebugMessage("Done with visions!", false);
        	}
        }
        // END HERBERT VISION AREAS
        /////////////////////////////////////////

        /////////////////////////
        //BEGIN 3 WHEELS & TONS//
        /*Check if a wheight is in the correct position
         */
        void CollideTonArea(string asParent, string asChild, int alState)
        {
        	//Sound for into and off right position
        	if(alState == 1)
        		PlaySoundAtEntity("on", "17_position_on", "control_room_chain_mount_12", 0.0f, false);
        	else
        		PlaySoundAtEntity("off", "17_position_off", "control_room_chain_mount_12", 0.0f, false);

        	SetLocalVarInt(asChild, alState);

        	/*If all weights correct, complete puzzle*/
        	if(GetLocalVarInt("TonArea01") == 1 && GetLocalVarInt("TonArea02") == 1 && GetLocalVarInt("TonArea03") == 1)
        	{
        		if(GetLocalVarInt("FirstPuzzleSolved")==0)
        		{
        			SetLocalVarInt("FirstPuzzleSolved", 1);
        			SetUpHerbertVisionAreas();
        		}

        		DoProgress(1);

        		for(int i=1;i<3;i++){
        			CreateParticleSystemAtEntityExt("steamp"+i, "ps_steam", "SteamAreaPuzzle_"+i, true, 0.8f, 0.4f, 0.3f, 1, true, 0.3f, 0.6f, 12.0f, 13.0f);
        			PlaySoundAtEntity("steamps"+i, "17_steam_quiet", "SteamAreaPuzzle_"+i, 1.0f, true);
        		}

        		PlaySoundAtEntity("TonDone", "17_wheights_correct", "TonDone", 3.0f, true);

        		for(int i=1;i<4;i++){
        			InteractConnectPropWithRope("Wheel_Rope0"+i+"_Left", "Wheel_Chain0"+i+"_End_Left", "Rope0"+i+"_Left",
        				false, 0,0,0, false, 0
        			);

        			SetPropStaticPhysics("Ton_Chain01_End_Left", true);
        			SetPropStaticPhysics("Ton_Chain02_End_Left", true);
        			SetPropStaticPhysics("Ton_Chain03_End_Left", true);

        			SetWheelInteractionDisablesStuck("Wheel_Chain0"+i+"_End_Left", false);
        			SetWheelStuckState("Wheel_Chain0"+i+"_End_Left", 1, false);

        			SetEntityPlayerInteractCallback("Wheel_Chain0"+i+"_End_Left", nameof(InteractLockedWheel), false);
        		}

        		//To fix the very slim posibility of a wheight gupping up and down, completing puzzle twice, making progress count twice...
        		SetEntityActive("TonArea01", false);
        		SetEntityActive("TonArea02", false);
        		SetEntityActive("TonArea03", false);
        	}

        	AddDebugMessage("Area: "+asChild+"  Entity: "+asParent+"  State: "+alState, false);
        }

        /*Stuck message for wheels and levers (from 4 piston puzzle)
         */
        void InteractLockedWheel(string asEntity)
        {
        	if(StringContains(asEntity, "Wheel"))
        		PlayGuiSound("12_valve_stuck.snt", 1.0f);
        	else
        		PlayGuiSound("lever_mech_min_max.snt", 0.8f);

        	AddTimer("lockedmess", 0.5f, nameof(TimerLockedMess));
        }
        void TimerLockedMess(string asTimer)
        {
        	SetMessage("Ch02Level17", "LockedWheel", 0);
        }
        //END 3 WHEELS & TONS//
        ///////////////////////


        ///////////////////////////
        //BEGIN 4 LEVERS & PISTON//
        /*Move the position to various positions depending on lever combinations
         */
        void StateChangePistonLever(string asEntity, int alState)
        {
        	SetLocalVarInt(asEntity, alState+1);

        	/*Piston position values for the 4 levers on min/max*/
        	float[] aL1 = {-0.075f,0.0f,0.05f};
        	float[] aL2 = {0.025f,0.0f,-0.03f};
        	float[] aL3 = {0.025f,0.0f,-0.025f};
        	float[] aL4 = {-0.075f,0.0f,0.05f};

        	/*Value for piston movement base on the position of the 4 levers*/
        	float fMove = aL1[GetLocalVarInt("p_lever_1")] + aL2[GetLocalVarInt("p_lever_2")]
        				+ aL3[GetLocalVarInt("p_lever_3")] + aL4[GetLocalVarInt("p_lever_4")];

        	SetMoveObjectState("piston", fMove);

        	if(alState != 0){
        		PlaySoundAtEntity("piston"+Str(fMove), "17_piston_move", "piston", 1.0f, false);
        		CreateParticleSystemAtEntityExt("pistonp"+Str(fMove), "ps_steam_piston_move", "Area_piston", false, 0.8f, 0.4f, 0.3f, 1, true, 0.3f, 0.6f, 12.0f, 13.0f);
        	}

        	/*Complete the puzzle if all 4 levers in the correct positions*/
        	if(GetLocalVarInt("p_lever_1") == 0 && GetLocalVarInt("p_lever_2") == 2 &&
        		GetLocalVarInt("p_lever_3") == 2 && GetLocalVarInt("p_lever_4") == 0){

        		if(GetLocalVarInt("FirstPuzzleSolved")==0)
        		{
        			SetLocalVarInt("FirstPuzzleSolved", 2);
        			SetUpHerbertVisionAreas();
        		}

        		AddTimer("dprog1", 1.5f, nameof(TimerDelayProg));

        		for(int i=3;i<6;i++){
        			CreateParticleSystemAtEntityExt("steamp"+i, "ps_steam", "SteamAreaPuzzle_"+i, true, 0.8f, 0.4f, 0.3f, 1, true, 0.3f, 0.6f, 12.0f, 13.0f);
        			PlaySoundAtEntity("steamps"+i, "17_steam_quiet", "SteamAreaPuzzle_"+i, 1.0f, true);
        		}

        		PlaySoundAtEntity("PistonComp", "17_piston_correct", "PistonDone", 3.0f, true);

        		for(int i=1;i<5;i++){
        			RemoveEntityCollideCallback("p_lever_"+i, "L_Up_Area_"+i);
        			RemoveEntityCollideCallback("p_lever_"+i, "L_Down_Area_"+i);
        		}

        		PlaySoundAtEntity("pistondone", "17_piston_done", "piston", 0.0f, false);

        		SetLeverStuckState("p_lever_1", -1, false);
        		SetLeverStuckState("p_lever_2", 1, false);
        		SetLeverStuckState("p_lever_3", 1, false);
        		SetLeverStuckState("p_lever_4", -1, false);

        		for(int i=1;i<5;i++)
        			SetEntityPlayerInteractCallback("p_lever_"+i, nameof(InteractLockedWheel), false);
        	}

        	AddDebugMessage("Entity: "+asEntity+"  State: "+alState+"  Value: "+Str(fMove), false);
        }
        void TimerDelayProg(string asTimer)
        {
        	DoProgress(1);

        	if(asTimer != "dprog1") return;

        	PlaySoundAtEntity("piston", "17_piston_move", "piston", 1.0f, false);
        	CreateParticleSystemAtEntityExt("pistonp", "ps_steam_piston_move", "Area_piston", false, 0.8f, 0.4f, 0.3f, 1, true, 0.3f, 0.6f, 12.0f, 13.0f);
        }

        /*Set the lever stuck at max/min briefly so it does not bounce around
        */
        void CollideLever(string asParent, string asChild, int alState)
        {
        	if(StringContains(asChild,"Down")) //Because Down didn't work reliable.
        		SetLeverStuckState(asParent, -1, true);
        	else SetLeverStuckState(asParent, 1, true);

        	AddTimer(asParent, 0.2f, nameof(TimerReleaseLever));

        	AddDebugMessage("Lever: "+asParent+"  Area: "+asChild, false);
        }
        void TimerReleaseLever(string asTimer)
        {
        	SetLeverStuckState(asTimer, 0, false);
        }
        //BEGIN 4 LEVERS & PISTON//
        ///////////////////////////


        ////////////////////////
        //BEGIN 3 PIPES & WALL//
        // Fade out pipe lights
        void RemovePipeLight(string asEntity)
        {
        	FadeLightTo("PointLight_" + asEntity, 0, 0, 0, 0, -1, 0.5f);
        }

        void StickyAttachPipe(string asStickyArea, string asBodyName)
        {
        	string sTemp = StringSub(asBodyName, 0, 13);

        	PlaySoundAtEntity("Attach"+asBodyName, "17_pipe_attach", asStickyArea, 0.0f, false);

        	/*I was lazy and didn't rename to avoid double stick...*/
        	FixSticky(asStickyArea, false);

        	/*Set Var to 1 as pipe correct*/
        	if(asStickyArea == "StickySmallArea_1" && sTemp == "playpipesmall")
        		SetLocalVarInt(sTemp, 1);
        	else if(asStickyArea == "StickySmallArea_4" && sTemp == "playpipehigh_")
        		SetLocalVarInt(sTemp, 1);

        	/*DO NOT Allow attach to same area and set Var to 1 as pipe correct*/
        	if(sTemp == "playpipelarge"){
        		SetEntityActive("StickySmallArea_3", false);
        		SetLocalVarInt(sTemp, 1);
        	}

        	/*DO NOT Allow attach large pipe at area where smallpipe can attach to*/
        	if(sTemp == "playpipesmall" && asStickyArea == "StickySmallArea_3")
        		SetEntityActive("StickyLargeArea_1", false);

        	/*Complete puzzle as all three pipes in correct position*/
        	if(GetLocalVarInt("playpipesmall") == 1 && GetLocalVarInt("playpipehigh_") == 1 && GetLocalVarInt("playpipelarge") == 1){
        		SetEntityInteractionDisabled("playpipesmall_part01", true);
        		SetEntityInteractionDisabled("playpipehigh_part02", true);
        		SetEntityInteractionDisabled("playpipelarge_part03", true);

        		PlaySoundAtEntity("SteamComp", "17_steam", "PipeDone", 3.0f, true);

        		CreateParticleSystemAtEntityExt("steamp6", "ps_steam", "SteamAreaPuzzle_6", true, 0.8f, 0.4f, 0.3f, 1, true, 0.3f, 0.6f, 12.0f, 13.0f);

        		if(GetLocalVarInt("FirstPuzzleSolved")==0)
        		{
        			SetLocalVarInt("FirstPuzzleSolved", 3);
        			SetUpHerbertVisionAreas();
        		}

        		AddTimer("dprog", 0.5f, nameof(TimerDelayProg));
        	}

        	AddDebugMessage("Area:  "+asStickyArea+"  Body: "+asBodyName, false);
        }

        void StickyDetachPipe(string asStickyArea, string asBodyName)
        {
        	string sTemp = StringSub(asBodyName, 0, 13);

        	PlaySoundAtEntity("Detach"+asBodyName, "17_pipe_detach", asStickyArea, 0.0f, false);

        	/*I was lazy and didn't rename to avoid double stick...*/
        	FixSticky(asStickyArea, true);

        	if(asStickyArea == "StickySmallArea_1" && sTemp == "playpipesmall")
        		SetLocalVarInt(sTemp, 0);
        	else if(asStickyArea == "StickySmallArea_4" && sTemp == "playpipehigh_")
        		SetLocalVarInt(sTemp, 0);

        	/*Allow attach small pipe at area where largepipe can attach to*/
        	if(sTemp == "playpipelarge"){
        		SetEntityActive("StickySmallArea_3", true);
        		SetLocalVarInt(sTemp, 0);
        	}

        	/*Allow attach large pipe at area where smallpipe can attach to*/
        	if(sTemp == "playpipesmall" && asStickyArea == "StickySmallArea_3")
        		SetEntityActive("StickyLargeArea_1", true);

        	AddDebugMessage("Area:  "+asStickyArea+"  Body: "+asBodyName, false);
        }

        /*So that two small pipes can't stick in same places
         */
        void FixSticky(string asStickyArea, bool bActive)
        {
        	if(asStickyArea == "StickySmallArea_6")
        		SetEntityActive("StickySmallArea_1", bActive);
        	if(asStickyArea == "StickySmallArea_5")
        		SetEntityActive("StickySmallArea_2", bActive);
        	if(asStickyArea == "StickySmallArea_4")
        		SetEntityActive("StickySmallArea_3", bActive);

        	if(asStickyArea == "StickySmallArea_1")
        		SetEntityActive("StickySmallArea_6", bActive);
        	if(asStickyArea == "StickySmallArea_2")
        		SetEntityActive("StickySmallArea_5", bActive);
        	if(asStickyArea == "StickySmallArea_3")
        		SetEntityActive("StickySmallArea_4", bActive);
        }
        //END 3 PIPES & WALL//
        //////////////////////


        ////////////////////////
        //BEGIN LEVEL PROGRESS//
        /*Called each time the player completes a puzzle in level
         */
        void DoProgress(int iProg)
        {
        	AddLocalVarInt("Progress", iProg);

        	StartScreenShake(0.01f, 0.5f, 0.5f, 1.0f);

        	PlayGuiSound("14_elevator_clank", 1.0f);

        	/*Alexander speaks when 2 puzzles completed*/
        	if(GetLocalVarInt("Progress") == 2)
        		AddTimer("voice", 8.0f, nameof(TimerAlexanderVoice));

        	/*Level puzzle done when all 3 puzzles completed*/
        	if(GetLocalVarInt("Progress") == 3){
        		CollideBreakJoint("", "", 1);	//Make the wheel break if used to exit inner part of level

        		SetGlobalVarInt("ControlRoomEastSideWorking", 1);

        		CompleteQuest("CisternEntrBridgeNonWork", "16BridgeNonWork");

        		RotatePropToSpeed("spinning_cogwheel_script_1", 1.0f, 1.0f, 0.0f, 1.0f, 0.0f, true, "");
        		RotatePropToSpeed("spinning_cogwheel_script_2", 1.0f, 1.0f, -1.0f, 0.0f, 0.0f, true, "");
        		RotatePropToSpeed("spinning_cogwheel_script_3", 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, true, "");

        		PlaySoundAtEntity("spinningcog", "17_metal_cog", "spinning_cogwheel_script_1", 2.0f, true);
        		PlaySoundAtEntity("alldone", "17_all_correct", "spinning_cogwheel_script_1", 4.0f, true);
        		StopSound("Sound_1", 4.0f);	//Just to not have a zillion sounds playing
        		StopSound("Sound_14", 4.0f);	//Just to not have a zillion sounds playing

        		PlayMusic("17_puzzle.ogg", false, 1.0f, 0.0f, 10, false);

        		PlayGuiSound("17_start_machine.snt", 1.0f);

        		AddTimer("shake", 3.0f, nameof(TimerShakeLoop));

        		for(int i=1;i<11;i++){
        			CreateParticleSystemAtEntityExt("steam"+i, "ps_steam", "SteamArea_"+i, true, 0.8f, 0.4f, 0.3f, 1, true, 0.3f, 0.6f, 12.0f, 13.0f);
        			PlaySoundAtEntity("steams"+i, "17_steam_quiet", "SteamArea_"+i, 2.0f, true);
        		}

        		GiveSanityBoost();
        	}
        }

        /*Alexander voice when having completed 2 puzzles
         */
        void TimerAlexanderVoice(string asTimer)
        {
        	AddEffectVoice("CH02L17_Alexander_01", "", "Voice", "CH02L17_Alexander_01", false, "", 0.0f, 0.0f);
        }

        void TimerShakeLoop(string asTimer)
        {
        	StartScreenShake(0.008f, 0.5f, 0.5f, 1.5f);

        	AddTimer("shake", 4.0f, nameof(TimerShakeLoop));
        }
        //END LEVEL PROGRESS//
        //////////////////////


        ///////////////////////
        //BEGIN SECURITY DOOR//
        /*When player has been in the pipe room, make the wheel for leaving breakable
         */
        void CollideBreakJoint(string asParent, string asChild, int alState)
        {
        	SetEntityActive("valve_iron_rusty_2", false);
        	SetEntityActive("valve_iron_rusty_breakable_2", true);
        }

        /*When player tries to open security door, the wheel breaks
         */
        void InteractBreakMe(string asEntity)
        {
        	AddTimer(asEntity, 0.3f, nameof(TimerBreakJoint));
        	AddTimer("mess", 1.5f, nameof(TimerBreakJoint));

        	PlaySoundAtEntity(asEntity, "17_strain_wheel", asEntity, 0.0f, false);
        }
        void TimerBreakJoint(string asTimer)
        {
        	SetEntityActive("AreaBrokenWheel", true);

        	if(asTimer == "mess"){
        		AddEntityCollideCallback("valve_iron_rusty_breakable_2", "AreaBrokenWheel", nameof(CollideBrokenWheel), false, 1);
        		return;
        	}

        	BreakJoint(asTimer +"_HingeJoint_1");

        	SetEntityPlayerInteractCallback("AreaBrokenWheel", nameof(InteractBrokenWheelMount), true);

        	AddUseItemCallback("citemonvalve", "crank_wood", "valve_iron_rusty_breakable_2", nameof(UseCrank), true);
        	AddUseItemCallback("citemonvalve", "crank_wood", "AreaBrokenWheel", nameof(UseCrank), true);
        	AddUseItemCallback("citemonvalve", "crank_wood", "valve_iron_rusty_1", nameof(UseCrank), true);

        	PlaySoundAtEntity("BrokenWheelsound", "17_break_wheel", "AreaBrokenWheel", 0.0f, false);

        	CreateParticleSystemAtEntity("BrokenWheelpart", "ps_hit_metal", "AreaBrokenWheel", false);
        }

        /*Tryin to put back the wheel will give error message
         */
        void CollideBrokenWheel(string asParent, string asChild, int alState)
        {
        	SetMessage("Ch02Level17", "BrokenWheelNoFix", 0);
        }

        void InteractBrokenWheelMount(string asEntity)
        {
        	SetMessage("Ch02Level17", "BrokenWheelNoFix", 0);

        	AddTimer(asEntity, 1.0f, nameof(TimerInteractWheelOnAgain));
        }
        void TimerInteractWheelOnAgain(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractBrokenWheelMount), true);
        }

        /*Using the spare crank found, it will attach and works as a wheel replacement
         */
        void UseCrank(string asItem, string asEntity)
        {
        	RemoveItem(asItem);

        	PlaySoundAtEntity("AttachWheelsound", "17_attach_crank", "AreaBrokenWheel", 0.0f, false);

        	SetEntityActive("crank_wood_no_mount_2", true);
        	SetEntityActive("AreaBrokenWheel", false);
        }
        //END SECURITY DOOR//
        /////////////////////


        //BEGIN DIARY PICKUP
        void EntityCallDiary(string asEntity, string type)
        {
        	AddLocalVarInt("HerbertMusic", 1);

        	PlayMusic("17_paper_herbert0"+GetLocalVarInt("HerbertMusic"), false, 0.7f, 0.0f, 10, false);
        }
        //END DIARY PICKUP


        ///////////////////////////
        //INTERACT SECURITY GATES
        void InteractGate(string asEntity)
        {
        	PlayGuiSound("17_attach_crank", 0.5f);
        	SetMessage("Ch01Level07", "SecurityGate", 0);

        	SetMoveObjectState(asEntity, 0.05f);

        	AddTimer(asEntity, 0.4f, nameof(TimerCloseGate));
        	AddTimer("sound", 0.8f, nameof(TimerCloseGate));
        }
        void TimerCloseGate(string asTimer)
        {
        	if(asTimer == "sound"){
        		PlayGuiSound("close_gate.ogg", 0.5f);
        	} else{
        		SetMoveObjectState(asTimer, 0.00f);

        		if(GetLocalVarInt(asTimer) != 1)
        			SetEntityPlayerInteractCallback(asTimer, nameof(InteractGate), true);
        	}
        }
        void InteractWheel(string asEntity)
        {
        	SetEntityPlayerInteractCallback("safety_normal_vert_valve_1", "", true);
        }


        //Else if you pull up wheights to top, they will fall down on load and if unlucky (or lucky) complete the puzzle.
        void TimerFixSaveMapLoad(string asTimer)
        {
        	for(int i=1;i<4;i++)
        		AddEntityCollideCallback("Ton_Chain0"+i+"_End_Left", "TonArea0"+i, nameof(CollideTonArea), false, 0);
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("ControlRoom");

        	///////////////////////////////////////////////////////////////////////////////////////
        	//!!!!!!MIXED UP LEFT AND RIGHT, SO LEFT IS ACTUALLY RIGHT SIDE IN GAME DOCUMENT!!!!!//
        	///////////////////////////////////////////////////////////////////////////////////////

        	for(int i=1;i<4;i++)
        		InteractConnectPropWithRope("Wheel_Rope0"+i+"_Left", "Wheel_Chain0"+i+"_End_Left", "Rope0"+i+"_Left", true, 8,2,2, false, 0);

        	//The collide callbacks for the weights are in onenter with a timer.

        	///////////////////////////////////////////////////////////////////////////////////////
        	//!!!!!!MIXED UP LEFT AND RIGHT, SO LEFT IS ACTUALLY RIGHT SIDE IN GAME DOCUMENT!!!!!//
        	///////////////////////////////////////////////////////////////////////////////////////

        	/////////////////
        	//PISTON CONFIG//
        	/////////////////
        	for(int i=1;i<5;i++){
        		AddEntityCollideCallback("p_lever_"+i, "L_Up_Area_"+i, nameof(CollideLever), false, 1);
        		AddEntityCollideCallback("p_lever_"+i, "L_Down_Area_"+i, nameof(CollideLever), false, 1);

        		SetLocalVarInt("p_lever_"+i, 1);
        	}

        	for(int i=1;i<5;i++)
        		SetEntityPlayerInteractCallback("control_room_piston_lever_"+i, nameof(InteractLockedWheel), false);
        	/////////////////
        	//PISTON CONFIG//
        	/////////////////

        	////////////////////////
        	//SECURITY DOOR CONFIG//
        	////////////////////////
        	InteractConnectPropWithMoveObject("v_s_1", "valve_iron_rusty_1", "safety_normal_vert_valve_1", true, false, 1);
        	InteractConnectPropWithMoveObject("v_s_2", "valve_iron_rusty_2", "safety_normal_vert_valve_1", true, false, 1);
        	InteractConnectPropWithMoveObject("c_s_1", "crank_wood_no_mount_2", "safety_normal_vert_valve_1", true, false, 1);

        	//AddEntityCollideCallback("Player", "AreaBreakJoint", "CollideBreakJoint", true, 1); Doing this when all puzzles completed instead
        	////////////////////////
        	//SECURITY DOOR CONFIG//
        	////////////////////////


        	for(int i=1;i<6;i++){
        		CreateParticleSystemAtEntityExt("steame"+i, "ps_steam", "SteamAreaEnter_"+i, true, 0.8f, 0.4f, 0.3f, 1, true, 0.3f, 0.6f, 12.0f, 13.0f);
        		PlaySoundAtEntity("steames"+i, "17_steam_quiet", "SteamAreaEnter_"+i, 2.0f, true);
        	}

        	SetNumberOfQuestsInMap(1);

        	if(ScriptDebugOn())
        	{
        		//CollideBreakJoint("","", 1);
        		//TimerBreakJoint("");

        		GiveItemFromFile("lantern", "lantern.ent");
        		SetGlobalVarInt("LastHerbertVisionTriggered", 0);

        		for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");
        	}
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	AddTimer("FixSaveMapLoad", 3.0f, nameof(TimerFixSaveMapLoad));

        	PreloadSound("17_pipe_detach"); PreloadSound("17_steam_quiet"); PreloadSound("17_wheights_correct"); PreloadSound("17_piston_move");
        	PreloadSound("17_piston_correct"); PreloadSound("17_piston_done"); PreloadSound("17_piston_move"); PreloadSound("17_pipe_attach");
        	PreloadSound("17_steam"); PreloadSound("17_all_correct"); PreloadSound("17_metal_cog"); PreloadSound("17_strain_wheel");
        	PreloadSound("17_break_wheel"); PreloadSound("17_attach_crank");

        	StopMusic(2.0f, 0);

        	SetUpHerbertVisionAreas();

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	//Fix for accidental compleition of wheights on exit
        	for(int i=1;i<4;i++){
        		RemoveEntityCollideCallback("Ton_Chain0"+i+"_End_Left", "TonArea0"+i);
        	}
        }

        //	SetFogActive(true);
        //	SetFogColor(0.18, 0.12, 0.09, 0.25);
        //	SetFogProperties(1, 7, 1, false);
        //BBR21268820123-20411-25291

        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);
    }
}
