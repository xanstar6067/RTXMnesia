using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/15_prison_south.hps; journal: docs/ScriptPort/ch02/15_prison_south.md.</summary>
    [HplMapScript("maps/main/ch02/15_prison_south.map")]
    public class PrisonSouth : HplScript
    {
        /////////////////////
        //BEGIN LOCKED DOOR//
        /*Sound, quest and message on door or pad interact
         */
        void InteractBarDoor(string asEntity)
        {
        	AddTimer(asEntity, 0.3f, nameof(TimerRattle));

        	if(GetLocalVarInt("IwalkedThePath") == 1) return;

        	SetEntityActive("im_just_cute_and_happy", true);
        	SetLocalVarInt("IwalkedThePath", 1);

        	AddTimer("breath", 1.5f, nameof(TimerPlayerReact));
        	AddTimer("breathl", 3.5f, nameof(TimerPlayerReact));
        	AddTimer("dude", 0.8f, nameof(TimerRattle));
        }
        void TimerRattle(string asTimer)
        {
        	if(asTimer == "dude"){
        		PlaySoundAtEntity("AreaRunDudeS", "15_man_hunted.snt", "AreaRunDude", 0.0f, false);
        		GiveSanityDamage(10.0f, true);
        	}

        	if(GetLocalVarInt("RattleQuest") == 0){
        		AddQuest("15Rattle", "15Rattle");
        		SetLocalVarInt("RattleQuest", 1);
        	} else{
        		if(asTimer == "prison_section_static_1") SetMessage("Ch02Level15", "RattleBars", 0);
        		else SetMessage("Ch02Level15", "MetalPad", 0);
        	}
        }

        /*Try to use hammer, chipper or combo on door/pad in vain
         */
        void UseHCDoor(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level15", "DoorItemNoUse", 0);
        }
        //END LOCKED DOOR//
        ///////////////////


        ///////////////////////////
        //BEGIN THE HIDDEN TUNNEL//
        /*When moved bed activate help hints for Exit
         */
         void CollideAreaHammer(string asParent, string asChild, int alState)
         {
         	if(asChild == "AreaHammer_1") SetLocalVarInt("HammerArea", 1);
        	else if(asChild == "AreaHammer_2") SetLocalVarInt("HammerArea", 2);
        	else if(asChild == "AreaHammer_3") SetLocalVarInt("HammerArea", 3);
        	else SetLocalVarInt("HammerArea", 4);
         }
        void CollideMoveBed(string asParent, string asChild, int alState)
        {
        	SetEntityPlayerInteractCallback("prison_tunnel_entry_1", nameof(InteractHole), false);

        	SetEntityActive("prison_tunnel_entry_static", false);
        	SetEntityActive("prison_tunnel_entry_1", true);

        	AddUseItemCallback("shc", "stone_hammer_chipper", "prison_tunnel_entry_1", nameof(UseHCH), true);
        	AddUseItemCallback("sh", "stone_hammer_1", "prison_tunnel_entry_1", nameof(UseHCH), false);
        	AddUseItemCallback("sc", "stone_chipper_1", "prison_tunnel_entry_1", nameof(UseHCH), false);
        	AddUseItemCallback("shc2", "stone_hammer_chipper", "BedOverHole", nameof(UseHCH), true);
        	AddUseItemCallback("sh2", "stone_hammer_1", "BedOverHole", nameof(UseHCH), false);
        	AddUseItemCallback("sc2", "stone_chipper_1", "BedOverHole", nameof(UseHCH), false);
        }

        /*Hint message on interact with small hole
         */
        void InteractHole(string asEntity)
        {
        	AddTimer(asEntity, 0.3f, nameof(TimerHoleSmall));

        	AddQuest("15Hole", "15Hole");
        }
        void TimerHoleSmall(string asTimer)
        {
        	PlaySoundAtEntity("rock2", "impact_rock_low", asTimer, 0.1f, false);
        }

        /*Use the Hammer, Chipper or Combo on the hole and break if correct items in possesion.
         */
        void UseHCH(string asItem, string asEntity)
        {
        	if(asItem == "stone_hammer_1" && HasItem("stone_chipper_1")){
        		BeingHoleBreak();

        		RemoveItem("stone_chipper_1");
        		RemoveItem("stone_hammer_1");

        		GiveItem("stone_hammer_chipper", "Puzzle", "stone_hammer_chipper", "stone_hammer_chipper.tga", 0);
        	}
        	else if(asItem == "stone_chipper_1" && HasItem("stone_hammer_1")){
        		BeingHoleBreak();

        		RemoveItem("stone_chipper_1");
        		RemoveItem("stone_hammer_1");

        		GiveItem("stone_hammer_chipper", "Puzzle", "stone_hammer_chipper", "stone_hammer_chipper.tga", 0);
        	}
        	else if(asItem == "stone_hammer_chipper") BeingHoleBreak();
        	else{
        		SetMessage("Ch02Level15", "HoleNotEnoughItem", 0);
        		AddQuest("15NeedMoreTool", "15NeedMoreTool");
        	}
        }
        /*Do a simple animation and event at using the tools on hole
         */
        void BeingHoleBreak()
        {
        	CompleteQuest("15Hole", "15Hole");

        	SetEntityActive("stone_hammer_move_"+GetLocalVarInt("HammerArea"), true);
        	SetEntityActive("stone_chipper_move_"+GetLocalVarInt("HammerArea"), true);

        	SetPlayerCrouching(true);
        	StartPlayerLookAt("stone_hammer_move_"+GetLocalVarInt("HammerArea"), 2.0f, 2.0f, "");

        	if(GetLocalVarInt("HammerTime") == 0){
        		SetLocalVarInt("HammerTime", 1);

        		AddTimer("movehammer", 0.1f, nameof(TimerMoveHammer));
        		AddTimer("movechipper", 0.27f, nameof(TimerMoveHammer));
        		AddTimer("resethammer", 0.4f, nameof(TimerMoveHammer));
        		AddTimer("resetchipper", 0.6f, nameof(TimerMoveHammer));
        	}

        	PlaySoundAtEntity("cumble1", "15_rock_break", "prison_tunnel_entry_1", 0, false);
        	PlaySoundAtEntity("begin", "15_make_hammer", "Player", 0.25f, false);
        	PlaySoundAtEntity("crumble", "03_rock_move", "prison_tunnel_entry_1", 2.0f, false);
        }
        void TimerMoveHammer(string asTimer)
        {
        	if(asTimer == "movehammer"){
        		SetMoveObjectState("stone_hammer_move_"+GetLocalVarInt("HammerArea"), 0.40f);
        	}
        	else if(asTimer == "movechipper"){
        		StopPlayerLookAt();
        		SetMoveObjectState("stone_chipper_move_"+GetLocalVarInt("HammerArea"), 1);
        		PlaySoundAtEntity("bang", "impact_rock_high", "Player", 0, false);
        		PlaySoundAtEntity("hammer", "15_hammer", "Player", 0, false);
        	}
        	else if(asTimer == "resethammer"){
        		SetMoveObjectState("stone_hammer_move_"+GetLocalVarInt("HammerArea"), 0);
        	}
        	else if(asTimer == "resetchipper"){
        		SetMoveObjectState("stone_chipper_move_"+GetLocalVarInt("HammerArea"), 0);
        	}

        	if(GetLocalVarInt("HammerTime") < 3 && asTimer == "resetchipper"){
        		AddTimer("movehammer", 0.01f, nameof(TimerMoveHammer));
        		AddTimer("movechipper", 0.17f, nameof(TimerMoveHammer));
        		AddTimer("resethammer", 0.3f, nameof(TimerMoveHammer));
        		AddTimer("resetchipper", 0.5f, nameof(TimerMoveHammer));
        		AddLocalVarInt("HammerTime", 1);
        	}
        	else if(GetLocalVarInt("HammerTime") == 3 && asTimer == "resetchipper"){
        		AddTimer("movehammer", 0.2f, nameof(TimerMoveHammer));
        		AddTimer("movechipper", 0.37f, nameof(TimerMoveHammer));
        		AddTimer("break", 0.42f, nameof(TimerMoveHammer));
        		AddTimer("resethammer", 0.5f, nameof(TimerMoveHammer));
        		AddTimer("resetchipper", 0.8f, nameof(TimerMoveHammer));
        		AddLocalVarInt("HammerTime", 1);
        	}
        	else if(GetLocalVarInt("HammerTime") == 4 && asTimer == "break"){
        		BreakHole();
        	}
        	else if(GetLocalVarInt("HammerTime") == 4 && asTimer == "resetchipper"){
        		SetEntityActive("stone_hammer_move_"+GetLocalVarInt("HammerArea"), false);
        		SetEntityActive("stone_chipper_move_"+GetLocalVarInt("HammerArea"), false);
        	}
        }
        /*Break the hole and add callbacks for exiting the level
         */
        void BreakHole()
        {
        	CompleteQuest("15Rattle", "15Rattle");

        	PlayMusic("15_puzzle_hole.ogg", false, 1.0f, 0.5f, 10, false);

        	SetEntityPlayerInteractCallback("ExitArea", nameof(InteractHoleExit), false);

        	AddEntityCollideCallback("Player", "ExitArea", nameof(CollideExitArea), true, 1);

        	GiveSanityBoost();

        	SetEntityActive("block_box_1", false); //An invisible block to keep the player from falling through the hole before breaking it.

        	SetEntityActive("prison_tunnel_entry_1", false);
        	SetEntityActive("prison_tunnel_entry_broken_1", true);
        	CreateParticleSystemAtEntity("breakps", "ps_break_prison_tunnel.ps", "BedOverHole", false);
        	PlaySoundAtEntity("breaks", "03_break_wall", "BedOverHole", 0.0f, false);

        	AddUseItemCallback("shc3", "stone_hammer_chipper", "prison_tunnel_entry_1_broken", nameof(UseHCHB), true);
        	AddUseItemCallback("sh3", "stone_hammer_1", "prison_tunnel_entry_1_broken", nameof(UseHCHB), false);
        	AddUseItemCallback("sc3", "stone_chipper_1", "prison_tunnel_entry_1_broken", nameof(UseHCHB), false);
        }

        /*A message just incase player tries to use tools again
         */
        void UseHCHB(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level15", "HoleAlreadyDone", 0);
        }

        /*Load next level on collide or interact with exit area
         */
        void CollideExitArea(string asParent, string asChild, int alState)
        {
        	ChangeMap("15_prison_north.map", "PlayerStartArea_1", "15_exit_level", "");

        	InteractHoleExit("ExitArea");
        }
        void InteractHoleExit(string asEntity)
        {
        	PlaySoundAtEntity("crawl", "scare_wall_scratch_single", "Player", 0.3f, false);
        }
        //END THE HIDDEN TUNNEL//
        /////////////////////////


        //////////////////////
        //BEGIN DRAGGED AWAY//
        /*Plead, break door, drag a prisoner
         */
        void CollideAreaDrag(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("brute", "15_brute", "AreaPrison", 0.0f, false);

        	AddTimer("p0", 0.5f, nameof(TimerDoPrisoner01));
        	AddTimer("p01", 2.5f, nameof(TimerDoPrisoner01));
        	AddTimer("p02", 4.5f, nameof(TimerDoPrisoner01));
        	AddTimer("p03", 5.5f, nameof(TimerDoPrisoner01));

        	AddTimer(nameof(TimerEPlayerReact), 0.7f, nameof(TimerEPlayerReact));
        }
        void TimerDoPrisoner01(string asTimer)
        {
        	if(asTimer == "p0") PlaySoundAtEntity("beg", "15_prisoner_beg", "AreaPrison", 0.0f, false);
        	if(asTimer == "p01"){
        		PlaySoundAtEntity("ripp", "15_ripp_door", "AreaPrison", 0.0f, false);
        		StartScreenShake(0.01f, 1.0f, 0.2f, 1.0f);
        		PlayMusic("15_event_prisoner.ogg", false, 0.6f, 0.5f, 10, false);
        	}
        	if(asTimer == "p02"){
        		PlaySoundAtEntity("scream", "15_prisoner_scream", "AreaPrison", 0.0f, false);
        		PlaySoundAtEntity("brute2", "15_brute", "AreaPrison", 0.5f, false);
        	}
        	if(asTimer == "p03"){
        		PlaySoundAtEntity("dragv", "15_prisoner_dragged", "AreaPrison_2", 0.0f, false);
        		PlaySoundAtEntity("drags", "15_drag_body", "AreaPrison_2", 0.0f, false);
        	}
        }
        void TimerEPlayerReact(string asTimer)
        {
        	int iMaxEventStep = 10;
        	float fEventSpeed = 1.5f;
        	string sEvent = asTimer;

        	AddLocalVarInt(sEvent, 1);

        	switch(GetLocalVarInt(sEvent)){
        		case 1:
        			GiveSanityDamage(20.0f, false);
        			PlaySoundAtEntity("scare", "react_scare", "Player", 0.3f, false);
        			PlaySoundAtEntity("creak", "00_creak", "Player", 6.0f, true);
        			FadeSepiaColorTo(1.0f, 0.1f);
        			FadePlayerAspectMulTo(2.0f, 0.02f);
        			FadeImageTrailTo(2.0f, 3.0f);
        			SetPlayerMoveSpeedMul(0.8f);
        			SetPlayerRunSpeedMul(0.8f);
        		break;
        		case 2:
        			PlaySoundAtEntity("b1", "react_breath", "Player", 0.5f, false);
        			SetPlayerMoveSpeedMul(0.7f);
        			SetPlayerRunSpeedMul(0.7f);
        		break;
        		case 3:
        			SetPlayerMoveSpeedMul(0.6f);
        			SetPlayerRunSpeedMul(0.6f);
        		break;
        		case 4:
        			PlaySoundAtEntity("b2", "react_breath", "Player", 0.5f, false);
        			SetPlayerMoveSpeedMul(0.5f);
        			SetPlayerRunSpeedMul(0.5f);
        		break;
        		case 5:
        			SetPlayerMoveSpeedMul(0.4f);
        			SetPlayerRunSpeedMul(0.4f);
        		break;
        		case 6:
        			PlaySoundAtEntity("brute3", "15_brute", "AreaPrison", 0.75f, false);
        			PlaySoundAtEntity("b3", "react_breath", "Player", 0.5f, false);
        			FadeSepiaColorTo(0.0f, 0.1f);
        			FadePlayerAspectMulTo(1.0f, 0.1f);
        			SetPlayerMoveSpeedMul(0.5f);
        			SetPlayerRunSpeedMul(0.5f);
        		break;
        		case 7:
        			SetPlayerMoveSpeedMul(0.6f);
        			SetPlayerRunSpeedMul(0.6f);
        		break;
        		case 8:
        			PlaySoundAtEntity("b4", "react_breath", "Player", 0.5f, false);
        			StopSound("creak", 6.0f);
        			FadeImageTrailTo(0.0f, 0.1f);
        			SetPlayerMoveSpeedMul(0.7f);
        			SetPlayerRunSpeedMul(0.7f);
        		break;
        		case 9:
        			SetPlayerMoveSpeedMul(0.8f);
        			SetPlayerRunSpeedMul(0.8f);
        		break;
        		case 10:
        			PlaySoundAtEntity("b5", "react_breath", "Player", 0.6f, false);
        			SetPlayerMoveSpeedMul(1.0f);
        			SetPlayerRunSpeedMul(1.0f);
        		break;
        	}

        	if(GetLocalVarInt(sEvent) <= iMaxEventStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }
        /*Bang on door and see brute walk away
         */
        void CollideAreaDrag02(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("death", "15_prisoner_death", "prison_6", 0.0f, false);
        	StopSound("Sound_1", 0.0f);

        	AddTimer(nameof(TimerEBangLast), 0.25f, nameof(TimerEBangLast));
        }
        void TimerEBangLast(string asTimer)
        {
        	int iMaxEventStep = 9;
        	float fEventSpeed = 0.5f;
        	string sEvent = asTimer;

        	AddLocalVarInt(sEvent, 1);

        	switch(GetLocalVarInt(sEvent)){
        		case 1:
        			PlaySoundAtEntity("rarw", "15_attack_claw", "prison_6", 0.0f, false);
        			PlaySoundAtEntity("scare", "react_scare", "Player", 0.0f, false);
        			fEventSpeed = 0.5f;
        		break;
        		case 2:
        			FadeImageTrailTo(3.0f, 4.0f);
        			GiveSanityDamage(30.0f, false);
        			PlaySoundAtEntity("puff", "15_body_impact", "prison_6", 0.0f, false);
        			fEventSpeed = 0.3f;
        		break;
        		case 3:
        			SetEntityActive("im_in_a_bad_mood", true);
        			SetEntityActive("corpse_male_1", true);
        			AddPropImpulse("corpse_male_1", -5.0f, 3.0f, 5.0f, "World");
        			fEventSpeed = 0.12f;
        		break;
        		case 4:
        			StartScreenShake(0.04f, 0.3f, 0.2f, 0.7f);
        			PlaySoundAtEntity("scare2", "react_scare", "Player", 0.0f, false);
        			AddPropImpulse("prison_6", 0.0f, 0.0f, 10.0f, "World");
        			PlaySoundAtEntity("doorbang", "player_falldamage_max.snt", "prison_6", 0.0f, false);
        			CreateParticleSystemAtEntity("splatt", "blood.ps", "AreaBloodSplatt", false);
        			fEventSpeed = 1.0f;
        		break;
        		case 5:
        			PlaySoundAtEntity("dinbe", "scare_tingeling", "Player", 0.0f, false);
        			PlaySoundAtEntity("scare3", "react_scare", "Player", 0.5f, false);
        			AddPropImpulse("corpse_male_1", 1.0f, 0.0f, -0.5f, "World");
        			fEventSpeed = 0.01f;
        		break;
        		case 6:
        			FadeImageTrailTo(0.0f, 0.1f);
        			StartScreenShake(0.2f, 0.1f, 0.0f, 0.0f);
        			//StartEffectFlash(0.2f, 0.1f, 0.2f);
        			PlaySoundAtEntity("shake1", "scare_wall_stomp", "Player", 0.0f, false);
        			PlaySoundAtEntity("shake2", "scare_whine_loop4", "Player", 0.0f, false);
        			fEventSpeed = 0.2f;
        		break;
        		case 7:
        			StartScreenShake(0.2f, 0.1f, 0.0f, 0.0f);
        			fEventSpeed = 0.1f;
        		break;
        		case 8:
        			StartScreenShake(0.2f, 0.1f, 0.0f, 0.0f);
        			PlaySoundAtEntity("breath", "react_breath", "Player", 0.5f, false);
        			fEventSpeed = 2.0f;
        		break;
        		case 9:
        			PlaySoundAtEntity("breath2", "react_breath", "Player", 0.7f, false);
        		break;
        	}

        	if(GetLocalVarInt(sEvent) <= iMaxEventStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }
        void EntityCallMonsterNoise(string asEntity, string type)
        {
        	AddTimer("noise", 4.0f, nameof(TimerMakeMonsterNoise));
        }
        void TimerMakeMonsterNoise(string asTimer)
        {
        	int iRand = RandInt(8, 16);

        	PlaySoundAtEntity("snoise"+iRand, "amb_idle", "prison_7", 0.0f, false);

        	AddTimer("noise"+iRand, iRand, nameof(TimerMakeMonsterNoise));
        }
        //END DRAGGED AWAY//
        ////////////////////


        ///////////////
        //BEGIN INTRO//
        void TimerEIntro(string asTimer)
        {
        	int iMaxEventStep = 16;
        	float fEventSpeed = 1.0f;
        	string sEvent = asTimer;

        	AddLocalVarInt(sEvent, 1);

        	switch(GetLocalVarInt(sEvent)){
        		case 1:
        			FadeIn(2.0f);
        			CreateParticleSystemAtEntity("dust", "ps_dust_elevator_crash.ps", "AreaDust", false);
        			PlaySoundAtEntity("breath1", "react_breath_slow", "Player", 0.5f, false);
        			StartPlayerLookAt("AreaLook_2", 1.2f, 1.2f, "");
        			MovePlayerHeadPos(0.0f, -0.8f, 0.5f, 0.8f, 0.8f);
        			FadePlayerRollTo(-30.0f, 2.0f, 2.0f);
        			PlayMusic("15_event_elevator.ogg", false, 1.0f, 0.5f, 10, false);
        			fEventSpeed = 1.0f;
        		break;
        		case 2:
        			SetPlayerActive(true);
        			PlaySoundAtEntity("cloth1", "player_crouch", "Player", 0.2f, false);
        			SetPlayerMoveSpeedMul(0.05f);
        			SetPlayerRunSpeedMul(0.05f);
        			SetPlayerLookSpeedMul(0.05f);
        			MovePlayerHeadPos(0.0f, -0.8f, -0.5f, 1.2f, 1.2f);
        			fEventSpeed = 1.0f;
        		break;
        		case 3:
        			FadePlayerRollTo(30.0f, 6.0f, 6.0f);
        			SetPlayerMoveSpeedMul(0.1f);
        			SetPlayerRunSpeedMul(0.1f);
        			SetPlayerLookSpeedMul(0.1f);
        			StartPlayerLookAt("AreaLook_3", 0.5f, 0.5f, "");
        			SetPlayerLookSpeedMul(0.2f);
        			fEventSpeed = 2.0f;
        		break;
        		case 4:
        			SetPlayerMoveSpeedMul(0.15f);
        			SetPlayerRunSpeedMul(0.15f);
        			SetPlayerLookSpeedMul(0.15f);
        			FadeOut(5.0f);
        			PlaySoundAtEntity("sight1", "react_sigh", "Player", 0.5f, false);
        			StartPlayerLookAt("AreaLook_1", 0.5f, 0.5f, "");
        			FadePlayerRollTo(-30.0f, 10.0f, 10.0f);
        			fEventSpeed = 0.5f;
        		break;
        		case 5:
        			PlaySoundAtEntity("cloth2", "player_climb", "Player", 0.2f, false);
        			SetPlayerMoveSpeedMul(0.2f);
        			SetPlayerRunSpeedMul(0.2f);
        			SetPlayerLookSpeedMul(0.2f);
        			MovePlayerHeadPos(0.0f, -0.5f, -0.25f, 0.5f, 0.5f);
        			fEventSpeed = 0.5f;
        		break;
        		case 6:
        			SetPlayerMoveSpeedMul(0.25f);
        			SetPlayerRunSpeedMul(0.25f);
        			SetPlayerLookSpeedMul(0.25f);
        			FadePlayerRollTo(0.0f, 15.0f, 15.0f);
        			fEventSpeed = 2.0f;
        		break;
        		case 7:
        			PlaySoundAtEntity("sight2", "react_sigh", "Player", 0.4f, false);
        			SetPlayerMoveSpeedMul(0.3f);
        			SetPlayerRunSpeedMul(0.3f);
        			SetPlayerLookSpeedMul(0.3f);
        			FadeIn(2.0f);
        			FadePlayerRollTo(30.0f, 8.0f, 8.0f);
        			MovePlayerHeadPos(0.0f, -0.8f, -0.5f, 0.7f, 0.5f);
        			PlaySoundAtEntity("cloth3", "player_crouch", "Player", 0.2f, false);
        		break;
        		case 8:
        			SetPlayerMoveSpeedMul(0.35f);
        			SetPlayerRunSpeedMul(0.35f);
        			SetPlayerLookSpeedMul(0.35f);
        			StartPlayerLookAt("AreaLook_3", 1.0f, 0.5f, "");
        			fEventSpeed = 0.5f;
        		break;
        		case 9:
        			SetPlayerMoveSpeedMul(0.4f);
        			SetPlayerRunSpeedMul(0.4f);
        			SetPlayerLookSpeedMul(0.4f);
        			FadePlayerRollTo(-30, 30.0f, 30.0f);
        			MovePlayerHeadPos(0.0f, 0.0f, 0.0f, 0.8f, 0.8f);
        			PlaySoundAtEntity("cloth4", "player_stand", "Player", 0.2f, false);
        			fEventSpeed = 0.5f;
        		break;
        		case 10:
        			FadePlayerRollTo(30, 8.0f, 8.0f);
        			PlaySoundAtEntity("cloth5", "player_climb", "Player", 0.5f, false);
        			SetPlayerMoveSpeedMul(0.45f);
        			SetPlayerRunSpeedMul(0.45f);
        			SetPlayerLookSpeedMul(0.45f);
        			FadeOut(4.0f);
        			StartPlayerLookAt("AreaLook_2", 1.5f, 1.5f, "");
        			fEventSpeed = 1.0f;
        		break;
        		case 11:
        			FadePlayerRollTo(-30, 10.0f, 10.0f);
        			fEventSpeed = 0.5f;
        		break;
        		case 12:
        			FadePlayerRollTo(0, 8.0f, 8.0f);
        			SetPlayerMoveSpeedMul(0.5f);
        			SetPlayerRunSpeedMul(0.5f);
        			SetPlayerLookSpeedMul(0.5f);
        			FadeIn(1.0f);
        			StopPlayerLookAt();
        		break;
        		case 13:
        			SetPlayerMoveSpeedMul(0.6f);
        			SetPlayerRunSpeedMul(0.6f);
        			SetPlayerLookSpeedMul(0.7f);
        		break;
        		case 14:
        			SetPlayerMoveSpeedMul(0.75f);
        			SetPlayerRunSpeedMul(0.75f);
        			SetPlayerLookSpeedMul(0.9f);
        		break;
        		case 15:
        			SetPlayerMoveSpeedMul(0.85f);
        			SetPlayerRunSpeedMul(0.85f);
        			SetPlayerLookSpeedMul(1.0f);
        		break;
        		case 16:
        			SetPlayerCrouchDisabled(false);
        			SetPlayerJumpDisabled(false);
        			SetPlayerMoveSpeedMul(1.0f);
        			SetPlayerRunSpeedMul(1.0f);
        			PlayMusic("12_amb.ogg", true, 1.0f, 0.5f, 0, true);
        		break;
        	}

        	if(GetLocalVarInt(sEvent) <= iMaxEventStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }
        //END INTRO//
        /////////////


        /////////////////////
        //BEGIN MISC EVENTS//
        /*Active second grunt if player goes back down
         */
        void CollideAreaGrunt(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("servant_grunt_1") == 1) return; //Don't do this while the first grunt is active.

        	if(asChild == "AreaCorridorGrunt_1"){
        		SetLocalVarInt("BeenInB", 1);
        	} else {
        		if(GetLocalVarInt("BeenInB") != 1) return;

        		SetEntityActive("grunt", true);

        		CheckPoint("checkpoint1","PlayerStartArea_6",nameof(CPCallDeath), "", "");

        		SetEntityActive("AreaCorridorGrunt_1", false);
        		SetEntityActive("AreaCorridorGrunt_2", false);
        	}
        }
        /*Some special events for the activation
         */
        void CollideAreaFlickGrunt(string asParent, string asChild, int alState)
        {
        	if(asChild == "AreaFlick_2" || asChild == "AreaFlick_4"){
        		GiveSanityDamage(10.0f, true);
        		PlaySoundAtEntity("scare", "react_scare", "Player", 0.3f, false);
        	}

        	StartScreenShake(0.2f, 0.1f, 0.1f, 0.1f);
        	PlaySoundAtEntity("s"+asChild, "sanity_flick.snt", asChild, 0.0f, false);
        	FadeLightTo("Light"+asChild, 1, 1, 1, 1, -1, 0.1f);
        	AddTimer("Light"+asChild, 0.2f, nameof(TimerLightOff02));
        	AddTimer("backon", 0.4f, nameof(TimerLightOff02));
        }
        void TimerLightOff02(string asTimer)
        {
        	if(asTimer == "backon"){
        		SetEntityActive("grunt", true);
        		StartScreenShake(0.1f, 0.1f, 0.0f, 0.1f);
        		return;
        	}
        	FadeLightTo(asTimer, 0, 0, 0, 1, -1, 0.1f);
        	SetEntityActive("grunt", false);
        }

        /*Event if player dies when second grunt activated
         */
        void CPCallDeath(string asName, int alCount)
        {
        	SetGlobalVarInt("15CPEvent", 1);

        	SetPlayerCrouching(true);

        	StartPlayerLookAt("AreaLookCP", 4.0f, 4.0f, "");

        	SetPlayerMoveSpeedMul(0.2f);
        	SetPlayerRunSpeedMul(0.2f);
        	SetPlayerLookSpeedMul(0.2f);

        	AddTimer("1", 0.5f, nameof(TimerCPEvent));
        	AddTimer("2", 3.0f, nameof(TimerCPEvent));
        	AddTimer("3", 3.8f, nameof(TimerCPEvent));
        	AddTimer("4", 4.0f, nameof(TimerCPEvent));
        	AddTimer("5", 4.5f, nameof(TimerCPEvent));
        }
        void TimerCPEvent(string asTimer)
        {
        	if(asTimer == "1"){
        		SetEntityActive("grunt_2", true);
        		StopPlayerLookAt();
        	}
        	else if(asTimer == "2"){
        		SetPlayerMoveSpeedMul(0.4f);
        		SetPlayerRunSpeedMul(0.4f);
        		SetPlayerLookSpeedMul(0.4f);
        	}
        	else if(asTimer == "3"){
        		StartScreenShake(0.1f, 0.1f, 0.1f, 0.1f);
        		PlaySoundAtEntity("flick1", "sanity_flick.snt", "Player", 0.0f, false);
        		FadeLightTo("CPLight", 1, 1, 1, 1, -1, 0.1f);
        		AddTimer("lightoff", 0.2f, nameof(TimerLightOff));
        		SetPlayerMoveSpeedMul(0.6f);
        		SetPlayerRunSpeedMul(0.6f);
        		SetPlayerLookSpeedMul(0.6f);
        	}
        	else if(asTimer == "4"){
        		FadeLightTo("CPLight", 1, 1, 1, 1, -1, 0.1f);
        		AddTimer("lightoff", 0.2f, nameof(TimerLightOff));
        		StartScreenShake(0.3f, 0.1f, 0.1f, 0.1f);
        		PlaySoundAtEntity("flick1", "sanity_flick.snt", "Player", 0.0f, false);
        		SetPlayerMoveSpeedMul(0.7f);
        		SetPlayerRunSpeedMul(0.7f);
        		SetPlayerLookSpeedMul(0.7f);
        		FadeGlobalSoundVolume(0.0f, 0.5f);
        	}
        	else if(asTimer == "5"){
        		PlaySoundAtEntity("aa", "sreact_scare.snt", "Player", 0.5f, false);
        		SetPlayerMoveSpeedMul(1.0f);
        		SetPlayerRunSpeedMul(1.0f);
        		SetPlayerLookSpeedMul(1.0f);
        		FadeGlobalSoundVolume(1.0f, 10.0f);
        	}
        }
        void TimerLightOff(string asTimer)
        {
        	FadeLightTo("CPLight", 0, 0, 0, 1, -1, 0.1f);
        }

        /*A bit of a rumble at a cavein
         */
        void CollideAreaRumble(string asParent, string asChild, int alState)
        {
        	StartScreenShake(0.008f, 1.0f, 2.0f,6.0f);

        	PlaySoundAtEntity("rumbs", "general_rock_rumble_no3d", "AreaRumbleEffect", 4.0f, false);

        	CreateParticleSystemAtEntity("rumbp", "ps_dust_falling_rumble.ps", "AreaRumbleEffect", false);

        	AddTimer("roar", 1.5f, nameof(TimerEndRumble));
        	AddTimer("rumbs", 3.0f, nameof(TimerEndRumble));
        }
        void TimerEndRumble(string asTimer)
        {
        	if(asTimer == "roar"){
        		PlaySoundAtEntity("AreRoars", "04_warn.snt", "AreRoar", 1.0f, false);
        		CreateParticleSystemAtEntity("AreRoarp", "ps_cave_monster_warn.ps", "AreRoar", false);

        		AddTimer("scare", 1.0f, nameof(TimerPlayerReact));
        		AddTimer("breath", 3.0f, nameof(TimerPlayerReact));
        		AddTimer("breathl", 5.0f, nameof(TimerPlayerReact));

        		GiveSanityDamage(5.0f, false);
        		return;
        	}

        	StopSound(asTimer, 8.0f);
        }
        void TimerPlayerReact(string asTimer)
        {
        	if(asTimer == "breathl") PlayGuiSound("react_breath", 0.5f);
        	else PlayGuiSound("react_"+asTimer, 0.7f);
        }

        /*Extra small puzzle to get sanity potion
         */
        void InteractLockedDoor(string asEntity)
        {
        	if(GetLocalVarInt("DoorOpenExtra") == 1) return;

        	AddQuest("15ExtraDoor", "15ExtraDoor");

        	AddTimer("mess", 0.3f, nameof(TimerDoorMess));
        }
        void TimerDoorMess(string asTimer)
        {
        	SetMessage("Ch02Level15", "InteractExtraDoor", 0);
        }
        void UseHCExtra(string asItem, string asEntity)
        {
        	if(GetLocalVarInt("DoorOpenExtra") == 1){
        		SetMessage("Ch02Level15", "UseExtraDoorDone", 0);
        		return;
        	}
        	if(asItem == "stone_hammer_1" && HasItem("stone_chipper_1")){
        		UnlockDoor();

        		RemoveItem("stone_chipper_1");
        		RemoveItem("stone_hammer_1");

        		GiveItem("stone_hammer_chipper", "Puzzle", "stone_hammer_chipper", "stone_hammer_chipper.tga", 0);
        	}
        	else if(asItem == "stone_chipper_1" && HasItem("stone_hammer_1")){
        		UnlockDoor();

        		RemoveItem("stone_chipper_1");
        		RemoveItem("stone_hammer_1");

        		GiveItem("stone_hammer_chipper", "Puzzle", "stone_hammer_chipper", "stone_hammer_chipper.tga", 0);
        	}
        	else if(asItem == "stone_hammer_chipper") UnlockDoor();
        	else SetMessage("Ch02Level15", "UseExtraDoor", 0);
        }
        void UnlockDoor()
        {
        	GiveSanityBoostSmall();

        	PlaySoundAtEntity("ready", "player_crouch", "Player", 0.0f, false);
        	StartPlayerLookAt("SwindArea_1", 2.0f, 2.0f, "");

        	CompleteQuest("15ExtraDoor", "15ExtraDoor");

        	AddTimer("e1", 0.1f, nameof(TimerDoorUnlock));
        	AddTimer("e2", 0.3f, nameof(TimerDoorUnlock));
        	AddTimer("e3", 0.4f, nameof(TimerDoorUnlock));

        	SetEntityActive("stone_hammer_move_5", true);
        	SetEntityActive("stone_chipper_move_5", true);
        }
        void TimerDoorUnlock(string asTimer)
        {
        	if(asTimer == "e1"){
        		SetMoveObjectState("stone_hammer_move_5", 0.25f);
        	}
        	else if(asTimer == "e2"){
        		PlaySoundAtEntity("hammer", "15_hammer", "Player", 0, false);
        		SetMoveObjectState("stone_chipper_move_5", 1.0f);
        		SetMoveObjectState("stone_hammer_move_5", 0.0f);
        		CreateParticleSystemAtEntity("bling", "ps_hit_metal", "stone_chipper_move_5", false);
        	}
        	else if(asTimer == "e3"){
        		StopPlayerLookAt();
        		SetSwingDoorLocked("prison_3", false, true);

        		SetEntityActive("stone_hammer_move_5", false);
        		SetEntityActive("stone_chipper_move_5", false);

        		PlaySoundAtEntity("break", "impact_metal_high", "Player", 0, false);
        		PlaySoundAtEntity("unlock", "unlock_door", "Player", 0, false);

        		SetLocalVarInt("DoorOpenExtra", 1);

        		PlayMusic("10_puzzle01", false, 1.0f, 0.5f, 10, false);
        	}
        }

        /*Extra lights for the torch casting shadows
         */
        void EntityCallFack(string asEntity, string type)
        {
        	SetLocalVarInt("DoBreezeScare", 1);

        	FadeLightTo("PointLight_6", 0.35f, 0.25f, 0.1f, -1, -1, 1.0f);
        	FadeLightTo("PointLight_4", 0.35f, 0.25f, 0.1f, -1, -1, 1.0f);
        }

        /*Play Music for girl and mother flashback
         */
        void CollideFlashBackGirlMother(string asParent, string asChild, int alState)
        {
        	AddTimer("gmmusic", 3.0f, nameof(TimerFlashBackGirlMother));
        	AddTimer("girlcry", 30.0f, nameof(TimerBeginGirlCry));
        }
        void TimerFlashBackGirlMother(string asTimer)
        {
        	PlayMusic("15_event_girl_mother.ogg", false, 0.7f, 0.5f, 10, false);
        }
        void TimerBeginGirlCry(string asTimer)
        {
        	PlaySoundAtEntity("girlcry", "15_girl_cry", "AreaGirlCry_"+RandInt(1,4), 0.0f, false);

        	AddTimer(nameof(TimerBeginGirlCry), RandInt(15, 40), nameof(TimerBeginGirlCry));
        }

        /*Interact with the blue cloth
         */
        void InteractAreaCloth(string asEntity)
        {
        	PlayGuiSound("12_cloth.snt", 0.9f);
        	//AddTimer(asEntity, 1.0f, "TimerClothOn");

        	//if(GetLocalVarInt("TouchedCloth") == 1) return;

        	StartEffectEmotionFlash("Ch02Level15", "InteractGirlCloth", "ui_insanity_touch.snt");

        	//SetLocalVarInt("TouchedCloth", 1);

        	//SetMessage("Ch02Level15", "InteractGirlCloth", 0);
        }
        void TimerClothOn(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractAreaCloth), true);
        }

        /*Rose petals for mother vision
         */
        void CollideFlashBackMother(string asParent, string asChild, int alState)
        {
        	AddTimer(nameof(TimerRosePetals), 2.0f, nameof(TimerRosePetals));
        }
        void TimerRosePetals(string asTimer)
        {
        	AddLocalVarInt("RosePetals", 1);

        	int iRose = GetLocalVarInt("RosePetals");

        	CreateParticleSystemAtEntity("rose"+iRose, "ps_rose_petals.ps", "AreaRose_"+RandInt(1,5), false);

        	if(GetLocalVarInt("RosePetals") == 1)
        		PlayGuiSound("general_wind_blow.ogg", 0.5f);

        	if(GetLocalVarInt("RosePetals") <= 10)
        		AddTimer(nameof(TimerRosePetals), RandFloat(0.5f, 2.5f), nameof(TimerRosePetals));
        }

        /*Rose petals for journey to sanctum
         */
        void CollideFlashBackJourney(string asParent, string asChild, int alState)
        {
        	AddTimer(nameof(TimerJourneyRosePetals), 2.0f, nameof(TimerJourneyRosePetals));
        }
        void TimerJourneyRosePetals(string asTimer)
        {
        	PlayGuiSound("general_wind_blow.ogg", 0.5f);

        	CreateParticleSystemAtEntity("AreaRoseJourney", "ps_rose_petals_wind.ps", "AreaRoseJourney", false);
        }

        /*A breeze flows through the level and turns off all corridor lights
         */
         void LitScareLantern(string asEntity, string type)
         {
         	SetLocalVarInt("DoBreezeScare", 1);
         }
        void EntityCallPickItem(string asEntity, string type)
        {
        	AddLocalVarInt("PickedItems", 1);

        	if(QuestIsAdded("15NeedMoreTool")) CompleteQuest("15NeedMoreTool", "15NeedMoreTool");

        	AddDebugMessage("Var is: " + GetLocalVarInt("PickedItems"), false);

        	if(GetLocalVarInt("PickedItems") == 2)
        	{
        		AddDebugMessage("Activating breeze area", true);
        		SetEntityActive("AreaBreeze", true);
        	}
        }
        void CollideAreaBreeze(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Collided with breeze area", false);
        	if(GetLocalVarInt("DoBreezeScare") != 1) return;

        	AddDebugMessage("Starting breeze!", false);

        	AddTimer(nameof(TimerEBreeze), 0, nameof(TimerEBreeze));

        	RemoveEntityCollideCallback(asParent, asChild);
        }
        void TimerEBreeze(string asTimer)
        {
        	string sEvent = asTimer;	//Do not edit, sets timer loop function name.
        	AddLocalVarInt(sEvent, 1);	//Do not edit, steps through timer loop events.
        	bool bPauseAtStep = false;	//Do not edit, to pause or end the timer loop at specified step.

        	float fEventSpeed = 0.5f;	//Set to preferred default time for the timer loop.

        	switch(GetLocalVarInt(sEvent)){
        		//////////
        		//Breeze from Prison North
        		case 1:
        			CreateParticleSystemAtEntity("ScriptArea_1p", "ps_dust_push_15.ps", "ScriptArea_1", false);
        			PlaySoundAtEntity("ScriptArea_1s", "general_wind_whirl.snt", "ScriptArea_1", 0, false);
        			PlaySoundAtEntity("amb_metal", "15_amb_metal", "ScriptArea_1", 8, true);
        			StopSound("Sound_8", 8.0f);
        		break;

        		//////////
        		//Breeze into corridor
        		case 2:
        			CreateParticleSystemAtEntity("ScriptArea_3p", "ps_dust_push_15.ps", "ScriptArea_3", false);
        			PlaySoundAtEntity("ScriptArea_3s", "general_wind_whirl.snt", "ScriptArea_3", 0, false);
        			CreateParticleSystemAtEntity("ScriptArea_4p", "ps_dust_push_15.ps", "ScriptArea_4", false);
        			PlaySoundAtEntity("ScriptArea_4s", "general_wind_whirl.snt", "ScriptArea_4", 0, false);
        		break;

        		//////////
        		//Breeze flows around corner
        		case 3:
        			CreateParticleSystemAtEntity("ScriptArea_2p", "ps_dust_push_15.ps", "ScriptArea_2", false);
        			PlaySoundAtEntity("ScriptArea_2s", "general_wind_whirl.snt", "ScriptArea_2", 0, false);
        			CreateParticleSystemAtEntity("ScriptArea_5p", "ps_dust_push_15.ps", "ScriptArea_5", false);
        			PlaySoundAtEntity("ScriptArea_5s", "general_wind_whirl.snt", "ScriptArea_5", 0, false);
        			SetLampLit("torch_static0b1_1*", false, true);
        			GiveSanityDamage(5.0f, true);
        			AddTimer("scare", 0.2f, nameof(TimerPlayerReact));
        			AddTimer("breath", 2.0f, nameof(TimerPlayerReact));
        			AddTimer("breathl", 4.0f, nameof(TimerPlayerReact));
        		break;

        		//////////
        		//Breeze has swept all around
        		case 4:
        			SetLampLit("torch_static0b2_1*", false, true);
        		break;

        		//////////
        		//Breeze has swept all around
        		case 5:
        			SetLampLit("torch_static0b3_1*", false, true);
        		break;

        		//////////
        		//Breeze has swept all around
        		case 6:
        			SetLampLit("torch_static0b4_1*", false, true);
        		break;

        		//////////
        		//End timer when no more steps in the event found.
        		default:
        			bPauseAtStep = true;
        		break;
        	}

        	if(!bPauseAtStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }

        /*Manly cries
         */
         void TimerManCry(string asTimer)
         {
        	PlaySoundAtEntity("AreaManCryS", "15_man0"+RandInt(1,2)+"_whimp", "AreaManCry_"+RandInt(1,6), 0.0f, false);

        	AddTimer(nameof(TimerManCry), RandInt(10, 60), nameof(TimerManCry));
         }
        //END MISC EVENTS//
        ///////////////////


        //GRUNT WHEN WALKING UP STAIRS
        void CollideAreaStairGrunt(string asParent, string asChild, int alState)
        {
        	SetEntityActive("servant_grunt_1", true);

        	AddTimer("scare", 1.0f, nameof(TimerPlayerReact));
        	AddTimer("breath", 3.0f, nameof(TimerPlayerReact));
        	AddTimer("breathl", 5.0f, nameof(TimerPlayerReact));

        	SetLocalVarInt("servant_grunt_1", 1);

        	CheckPoint("grunt_stairs", "ScriptArea_6", nameof(CPCallGruntStairs), "", "");
        }
        void EntityCallStairGrunt(string asEntity, string type)
        {
        	SetLocalVarInt("servant_grunt_1", 0);	//Grunt when going back down again can be activated.

        	SetEntityActive("AreaDoLastGrunt", true);	//A grunt can be activated outsidfe one of the cells upstairs when inside that cell.

        	AddDebugMessage("Stair Grunt Gone", false);
        }
        void CPCallGruntStairs(string asName, int alCount)
        {
        	SetLocalVarInt("servant_grunt_1", 0);

        	SetEntityActive("rat_hell_*", true);

        	PlaySoundAtEntity("rats", "insanity_bug3.snt", "Player", 5.0f, false);

        	SetPlayerCrouching(true);
        	StartPlayerLookAt("AreaRatLook", 10.0f, 10.0f, "");

        	AddTimer("stoplook", 1.0f, nameof(TimerRemoveRats));
        	AddTimer("rat_hell", 7.0f, nameof(TimerRemoveRats));

        	AddDebugMessage("I died!", false);
        }
        void TimerRemoveRats(string asTimer)
        {
        	if(asTimer == "stoplook"){
        		StopPlayerLookAt();
        		return;
        	}

        	SetPropActiveAndFade("rat_hell_*", false, 5.0f);
        	StopSound("rats", 5.0f);
        }

        //LAST GRUNT
        void CollideAreaDoLastGrunt(string asParent, string asChild, int alState)
        {
        	SetEntityActive("servant_grunt_2", true);
        	AddDebugMessage("In cell!", false);

        	CheckPoint("grunt_cell", "PlayerStartArea_10", nameof(CPCallGruntCell), "", "");
        }
        void CPCallGruntCell(string asName, int alCount)
        {
        	StartPlayerLookAt("LookCellGrunt", 10, 10, "");

        	AddEnemyPatrolNode("super_brute", "PathNodeArea_117", 0.0f, "");

        	FadeRadialBlurTo(0.5f, 0.5f);
        	SetRadialBlurStartDist(0.3f);

        	SetPlayerMoveSpeedMul(0.0f);
        	SetPlayerRunSpeedMul(0.0f);

        	FadeLightTo("PointLight_38", 0.8f, 0.4f, 0.2f, 1.0f, -1, 1);

        	AddTimer("1", 1.0f, nameof(TimerGruntCell));
        	AddTimer("2", 2.0f, nameof(TimerGruntCell));
        	AddTimer("3", 3.0f, nameof(TimerGruntCell));
        	AddTimer("4", 5.0f, nameof(TimerGruntCell));
        }
        void TimerGruntCell(string asTimer)
        {
        	if(asTimer == "1"){
        		StopPlayerLookAt();
        		SetSwingDoorClosed("prison_4", true, true);
        		SetPlayerMoveSpeedMul(0.1f);
        	}
        	if(asTimer == "2"){
        		AddTimer("scare", 0.5f, nameof(TimerPlayerReact));
        		AddTimer("breath", 2.5f, nameof(TimerPlayerReact));
        		AddTimer("breathl", 4.5f, nameof(TimerPlayerReact));
        		SetEntityActive("super_brute", true);
        		SetPlayerMoveSpeedMul(0.3f);
        	}
        	if(asTimer == "3"){
        		FadeLightTo("PointLight_38", 0, 0, 0, 0, -1, 1);
        		SetPlayerMoveSpeedMul(0.5f);
        	}
        	if(asTimer == "4"){
        		SetPlayerMoveSpeedMul(1.0f);
        		SetPlayerRunSpeedMul(1.0f);
        		FadeRadialBlurTo(0.0f, 0.1f);
        		SetEntityActive("super_brute", false);
        	}
        }


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("PrisonSouth");

        	////////////////////
        	// Hub start sanity boost
        	GiveSanityBoostSmall();


        	//---- COLLIDE INTI ----//
        	AddEntityCollideCallback("bed_simple_movable_1", "BedOverHole", nameof(CollideMoveBed), true, -1);
        	AddEntityCollideCallback("Player", "AreaDrag", nameof(CollideAreaDrag), true, 1);
        	AddEntityCollideCallback("Player", "AreaDrag_2", nameof(CollideAreaDrag02), true, 1);
        	AddEntityCollideCallback("Player", "AreaCorridorGrunt_1", nameof(CollideAreaGrunt), false, 1);
        	AddEntityCollideCallback("Player", "AreaCorridorGrunt_2", nameof(CollideAreaGrunt), false, 1);
        	AddEntityCollideCallback("Player", "AreaRumble", nameof(CollideAreaRumble), true, 1);
        	for(int i=0;i<5;i++) AddEntityCollideCallback("grunt", "AreaFlick_"+i, nameof(CollideAreaFlickGrunt), true, 1);
        	for(int i=0;i<5;i++) AddEntityCollideCallback("Player", "AreaHammer_"+i, nameof(CollideAreaHammer), false, 1);

        	AddEntityCollideCallback("Player", "FlashbackArea_3", nameof(CollideFlashBackGirlMother), true, 1);
        	AddEntityCollideCallback("Player", "FlashbackArea_2", nameof(CollideFlashBackMother), true, 1);
        	AddEntityCollideCallback("Player", "FlashbackArea_1", nameof(CollideFlashBackJourney), true, 1);

        	AddEntityCollideCallback("Player", "AreaBreeze", nameof(CollideAreaBreeze), false, 1);

        	AddEntityCollideCallback("Player", "AreaStairGrunt", nameof(CollideAreaStairGrunt), true, 1);

        	AddEntityCollideCallback("Player", "AreaDoLastGrunt", nameof(CollideAreaDoLastGrunt), true, 1);

        	//---- INTERACT INIT ----//
        	AddUseItemCallback("shcd", "stone_hammer_chipper", "prison_section_static_1", nameof(UseHCDoor), true);
        	AddUseItemCallback("shd", "stone_hammer_1", "prison_section_static_1", nameof(UseHCDoor), false);
        	AddUseItemCallback("scd", "stone_chipper_1", "prison_section_static_1", nameof(UseHCDoor), false);
        	AddUseItemCallback("shcp", "stone_hammer_chipper", "padlock_1", nameof(UseHCDoor), true);
        	AddUseItemCallback("shp", "stone_hammer_1", "padlock_1", nameof(UseHCDoor), false);
        	AddUseItemCallback("scp", "stone_chipper_1", "padlock_1", nameof(UseHCDoor), false);
        	AddUseItemCallback("shcx", "stone_hammer_chipper", "prison_3", nameof(UseHCExtra), false);
        	AddUseItemCallback("shx", "stone_hammer_1", "prison_3", nameof(UseHCExtra), false);
        	AddUseItemCallback("scx", "stone_chipper_1", "prison_3", nameof(UseHCExtra), false);

        	//---- ENEMY PATROL ----//
        	AddEnemyPatrolNode("im_in_a_bad_mood", "PathNodeArea_9", 0.1f, "");
        	AddEnemyPatrolNode("im_in_a_bad_mood", "PathNodeArea_1", 0.2f, "");
        	AddEnemyPatrolNode("im_in_a_bad_mood", "PathNodeArea_4", 0.0f, "");
        	AddEnemyPatrolNode("im_in_a_bad_mood", "PathNodeArea_6", 0.0f, "");

        	AddEnemyPatrolNode("im_just_cute_and_happy", "PathNodeArea_169", 0.0f, "");

        	AddEnemyPatrolNode("grunt", "PathNodeArea_53", 2.0f, "");

        	AddEnemyPatrolNode("grunt_2", "PathNodeArea_63", 2.0f, "");

        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_45", 2, "");
        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_144", 0, "");

        	AddEnemyPatrolNode("servant_grunt_2", "PathNodeArea_113", 8, "IdleExtra3");
        	AddEnemyPatrolNode("servant_grunt_2", "PathNodeArea_102", 0, "");
        	AddEnemyPatrolNode("servant_grunt_2", "PathNodeArea_52", 0, "");

        	SetNumberOfQuestsInMap(1);

        	AddTimer(nameof(TimerManCry), RandInt(30, 40), nameof(TimerManCry));

        	//----DEBUG----//
        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("lantern", "lantern.ent");
        		for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

        		GiveItemFromFile("stone_hammer_1", "stone_hammer.ent");
        		GiveItemFromFile("stone_chipper_1", "stone_chipper.ent");

        		//SetEntityActive("AreaDoLastGrunt", true);

        		/*Test pickup event on 1 item*/
        		//AddLocalVarInt("PickedItems", 1);

        		/*Test Intro*/
        /* 		SetPlayerActive(false);
        		MovePlayerHeadPos(0, -1.0f, -0.5f, 10.0f, 0.0f);
        		FadeOut(0.0f);
        		StartPlayerLookAt("AreaLook_1", 8.0f, 8.0f, "");
        		AddTimer("TimerEIntro", 0.25f, "TimerEIntro");
        		SetPlayerMoveSpeedMul(0.0f);
        		SetPlayerRunSpeedMul(0.0f);
        		SetPlayerLookSpeedMul(0.0f);
        		StartScreenShake(0.01f, 5.0f, 0.0f,5.0f);
        		PlaySoundAtEntity("intro", "15_intro", "Player", 0.1f, false); */

        	}
        	else
        	{
        		SetPlayerActive(false);
        		SetPlayerCrouchDisabled(true);
        		SetPlayerJumpDisabled(true);
        		MovePlayerHeadPos(0, -1.0f, -0.5f, 10.0f, 0.0f);
        		FadeOut(0.0f);
        		StartPlayerLookAt("AreaLook_1", 8.0f, 8.0f, "");
        		AddTimer(nameof(TimerEIntro), 0.25f, nameof(TimerEIntro));
        		SetPlayerMoveSpeedMul(0.0f);
        		SetPlayerRunSpeedMul(0.0f);
        		SetPlayerLookSpeedMul(0.0f);
        		PlaySoundAtEntity("intro", "15_intro", "Player", 0.1f, false);
        	}
        }



        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("15_intro"); PreloadSound("15_prisoner_death"); PreloadSound("react_breath"); PreloadSound("00_creak");
        	PreloadSound("react_scare"); PreloadSound("15_man_hunted"); PreloadSound("impact_rock_low"); PreloadSound("15_rock_break");
        	PreloadSound("03_rock_move"); PreloadSound("impact_rock_high"); PreloadSound("03_break_wall"); PreloadSound("scare_wall_scratch_single");
        	PreloadSound("15_brute"); PreloadSound("15_prisoner_beg"); PreloadSound("15_prisoner_scream"); PreloadSound("15_ripp_door");
        	PreloadSound("15_prisoner_dragged"); PreloadSound("15_drag_body"); PreloadSound("react_scare"); PreloadSound("15_body_impact");
        	PreloadSound("scare_tingeling"); PreloadSound("scare_wall_stomp"); PreloadSound("scare_whine_loop4"); PreloadSound("amb_idle");
        	PreloadSound("react_breath_slow"); PreloadSound("player_crouch"); PreloadSound("react_sigh"); PreloadSound("player_stand");
        	PreloadSound("player_climb"); PreloadSound("sanity_flick"); PreloadSound("general_rock_rumble_no3d"); PreloadSound("04_warn");
        	PreloadSound("impact_metal_high"); PreloadSound("15_girl_cry"); PreloadSound("general_wind_whirl");

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	CompleteQuest("15ExtraDoor", "15ExtraDoor");

        	//////////////////////
        	//Load Screen Setup
        	//SetupLoadScreen("LoadingText", "Ch02_Prison02", 1, "game_loading_well.jpg");
        }
    }
}
