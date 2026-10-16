using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch03
{
    /// <summary>Port of maps/main/ch03/25_cell_tunnels.hps; journal: docs/ScriptPort/ch03/25_cell_tunnels.md.</summary>
    [HplMapScript("maps/main/ch03/25_cell_tunnels.map")]
    public class CellTunnels : HplScript
    {

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // HELPERS
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void PlayEffectVoice(string asEntryBase,int alStartIdx, int alEndIdx, string asCallback)
        {
        	for(int i=alStartIdx; i<=alEndIdx; ++i)
        	{
        		string sEntry = asEntryBase;
        		if(i<10) sEntry += "0";
        		sEntry += i;

        		AddEffectVoice(sEntry, "", "Voice", sEntry, false, "", 0,0 );
        	}
        	SetEffectVoiceOverCallback(asCallback);
        }

        void PlayFlashbackVoice(string asEntryBase,int alStartIdx, int alEndIdx, string asCallback)
        {
        	for(int i=alStartIdx; i<=alEndIdx; ++i)
        	{
        		string sEntry = asEntryBase;
        		if(i<10) sEntry += "0";
        		sEntry += i;

        		AddEffectVoice(sEntry, "", "Flashbacks", sEntry, false, "", 0,0 );
        	}
        	SetEffectVoiceOverCallback(asCallback);
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // PLAYER WAKES UP
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        // This has been shamelessly copypasted from 00_rainy_hall.hps
        void TimerBlackOut(string asTimer)
        {
        	AddLocalVarInt("BlackoutStep", 1);	//What step to play in the event
        	float fEventSpeed = 0.5f;				//The default time between steps in an event

        	switch(GetLocalVarInt("BlackoutStep")) {
        		case 1:
        			StartPlayerLookAt("Arealook2", 0.1f, 0.1f, "");
        			SetEntityActive("servant_grunt_1", true);
        			AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_9", 4.0f, "IdleExtra3");
        			AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_4", 0.0f, "");
        			AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_8", 0.0f, "");
        			FadeIn(4);
        			FadeImageTrailTo(2,1);
        			SetPlayerMoveSpeedMul(0.05f);
        			SetPlayerLookSpeedMul(0.05f);
        			PlaySoundAtEntity("pant", "react_pant.snt", "Player", 2, false);
        			fEventSpeed = 3.0f;
        		break;
        		case 2:
        			FadePlayerRollTo(85, 1, 1);
        		break;
        		case 3:
        			SetPlayerActive(true);
        			StartPlayerLookAt("Arealook3", 0.1f, 0.1f, "");
        		break;
        		case 4:
        			FadeImageTrailTo(0,1);
        			FadePlayerRollTo(65, 1, 1);
        		break;
        		case 5:
        			PlaySoundAtEntity("sigh", "react_sigh.snt", "Player", 1.0f / 2, false);
        			FadeOut(2);
        			fEventSpeed = 1.5f;
        		break;
        		case 6:
        			FadePlayerRollTo(85, 1, 4);
        			StartPlayerLookAt("Arealook1", 0.1f, 0.1f, "");
        		break;
        		case 7:
        			FadeImageTrailTo(1.8f,1.5f);
        			FadePlayerFOVMulTo(1.25f, 0.01f);
        		break;
        		case 8:
        			FadePlayerRollTo(45, 1, 2);
        			FadeIn(2);
        			fEventSpeed = 1.5f;
        		break;
        		case 9:
        			StartPlayerLookAt("Arealook2", 0.1f, 0.1f, "");
        		break;
        		case 10:
        			FadePlayerRollTo(15, 1, 2);
        			FadePlayerFOVMulTo(0.75f, 0.01f);
        		break;
        		case 11:
        			PlaySoundAtEntity("sigh", "react_sigh.snt", "Player", 1.0f / 1.5f, false);
        			FadeOut(1);
        			StartPlayerLookAt("Arealook3", 1, 1, "");
        			FadePlayerRollTo(50, 1, 2);
        			fEventSpeed = 2.0f;
        		break;
        		case 12:
        			SetPlayerMoveSpeedMul(0.1f);
        			SetPlayerLookSpeedMul(0.1f);
        			StartPlayerLookAt("Arealook1", 1, 1, "");
        			FadePlayerFOVMulTo(1.1f, 0.01f);
        			FadeImageTrailTo(0,1.5f);
        			fEventSpeed = 1.5f;
        		break;
        		case 13:
        			SetPlayerMoveSpeedMul(0.2f);
        			SetPlayerLookSpeedMul(0.2f);
        			FadePlayerRollTo(-15, 1, 2);
        			FadeIn(1);
        			StartPlayerLookAt("Arealook4", 2, 2, "");
        			fEventSpeed = 2.0f;
        		break;
        		case 14:
        			SetPlayerMoveSpeedMul(0.3f);
        			SetPlayerLookSpeedMul(0.3f);
        			FadePlayerRollTo(-30, 10, 60);
        			MovePlayerHeadPos(0, 0, 0, 1, 0.5f);
        			StartPlayerLookAt("Arealook3", 1, 1, "");
        			FadePlayerFOVMulTo(0.9f, 0.01f);
        			FadeImageTrailTo(1.5f,2);
        			PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
        		break;
        		case 15:
        			SetPlayerMoveSpeedMul(0.4f);
        			SetPlayerLookSpeedMul(0.4f);
        			FadePlayerRollTo(10, 10, 20);
        			MovePlayerHeadPos(0, -0.5f, 0, 1, 0.5f);
        		break;
        		case 16:
        			SetPlayerMoveSpeedMul(0.5f);
        			SetPlayerLookSpeedMul(0.5f);
        			FadePlayerRollTo(0, 10, 60);
        			MovePlayerHeadPos(0, 0, 0, 1, 0.5f);
        			StartPlayerLookAt("Arealook4", 2, 2, "");
        			FadePlayerFOVMulTo(1, 0.01f);
        			PlaySoundAtEntity("movement", "player_climb.snt", "Player", 0, false);
        			SetPlayerCrouchDisabled(false);
        			fEventSpeed = 1.0f;
        		break;
        		case 17:
        			SetPlayerMoveSpeedMul(0.6f);
        			SetPlayerLookSpeedMul(0.6f);
        			FadeImageTrailTo(0,0.2f);
        			StopPlayerLookAt();
        			PlaySoundAtEntity("sigh", "react_sigh.snt", "Player", 1.0f / 1, false);
        			fEventSpeed = 1.0f;
        		break;
        		case 18:
        			SetPlayerJumpDisabled(false);
        			SetPlayerMoveSpeedMul(1.0f);
        			SetPlayerRunSpeedMul(1.0f);
        			SetPlayerLookSpeedMul(1.0f);
        			for(int i=1;i<=7;++i)
        				AddEffectVoice("CH03L25_Alexander_0"+i, "", "Voice", "CH03L25_Alexander_0"+i, false, "", 0.0f, 0.0f);

        		break;
        	}

        	if(GetLocalVarInt("BlackoutStep") < 19)  AddTimer("blackout", fEventSpeed, nameof(TimerBlackOut));
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // CELL DOOR
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void InteractPrisonDoor(string asEntity)
        {
        	SetMessage("Ch03Level25", "InteractLockedCellDoor", -1);

        	AddQuest("25LockedCellDoor", "25LockedCellDoor");
        }

        void UseKeyOnCellDoor(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level25", nameof(UseKeyOnCellDoor), -1);
        }


        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // WRIGGLE BAR BREAK
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void CollideWriggleBarBreak(string asParent, string asChild, int alState)
        {
        	AddLocalVarInt("wriggle_bar_break", 1);

        	//CreateParticleSystemAtEntity("dust", "ps_hit_soft.ps", "wriggle_prison_bar_1", false);

        	//Only break one of the bars
        	if(GetLocalVarInt("wriggle_bar_broken") == 1 ){
        		SetMessage("BasicPhrases", "UseItemOrCombineItem03", 0);
        		for(int i=1;i<=4;++i)
        			RemoveEntityCollideCallback("wriggle_prison_bar_1", "AreaWriggleBarBreak_" + i);
        		return;
        	}

        	//The bar finally breaks!
        	if(GetLocalVarInt("wriggle_bar_break")==6)
        	{
        		AddLocalVarInt("wriggle_bar_broken", 1);

        		CreateParticleSystemAtEntity("sparks", "ps_hit_metal.ps", "wriggle_prison_bar_1", false);

        		PlaySoundAtEntity("breakb", "17_break_wheel.snt", asChild, 0.2f, false);
        		PlaySoundAtEntity("breakb2", "25_bar_drop.snt", asChild, 0.2f, false);

        		SetEntityActive("wriggle_prison_bar_1", false);
        		SetEntityActive("wriggle_prison_bar_item_1", true);

        		for(int i=1;i<=4;++i)
        			RemoveEntityCollideCallback("wriggle_prison_bar_1", "AreaWriggleBarBreak_" + i);
        	}
        }

        void CollideWriggleBarBreak2(string asParent, string asChild, int alState)
        {
        	AddLocalVarInt("wriggle_bar_break2", 1);

        	//CreateParticleSystemAtEntity("dust", "ps_hit_soft.ps", "wriggle_prison_bar_1", false);

        	//Only break one of the bars
        	if(GetLocalVarInt("wriggle_bar_broken") == 1 ){
        		SetMessage("BasicPhrases", "UseItemOrCombineItem03", 0);
        		for(int i=5;i<=8;++i)
        			RemoveEntityCollideCallback("wriggle_prison_bar_2", "AreaWriggleBarBreak_" + i);
        		return;
        	}

        	//The bar finally breaks!
        	if(GetLocalVarInt("wriggle_bar_break2")==6)
        	{
        		AddLocalVarInt("wriggle_bar_broken", 1);

        		CreateParticleSystemAtEntity("sparks", "ps_hit_metal.ps", "wriggle_prison_bar_2", false);

        		PlaySoundAtEntity("breakb", "17_break_wheel.snt", asChild, 0.2f, false);
        		PlaySoundAtEntity("breakb2", "25_bar_drop.snt", asChild, 0.2f, false);

        		SetEntityActive("wriggle_prison_bar_2", false);
        		SetEntityActive("wriggle_prison_bar_item_2", true);

        		for(int i=5;i<=8;++i)
        			RemoveEntityCollideCallback("wriggle_prison_bar_2", "AreaWriggleBarBreak_" + i);
        	}
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // LOOSE BRICKS
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void InteractLooseBricks(string asEntity)
        {
        	SetMessage("Ch03Level25", nameof(InteractLooseBricks), -1);

        	PlayGuiSound("18_touch_rock.snt", 0.8f);

        	AddTimer(asEntity, 1.0f, nameof(TimerLooseBricks));
        }

        void TimerLooseBricks(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractLooseBricks), true);
        }
        //-------------------------------------------------------


        void UseBarOnBricks(string asItem, string asEntity)
        {
        	SetEntityActive("wriggle_prison_bar_bend_stone_1", true);

        	FadeLightTo("PointLight_21", 0.9f, 0.8f, 0.3f, 0.8f,  -1, 1);

        	CreateParticleSystemAtEntity("dust", "ps_hit_rock.ps", "AreaLooseBricks", false);

        	//If adding bar, but not breaking wall, then escaping through other means, this makes sure there is
        	//only a dyn bar in the level if this is true
        	SetLocalVarInt("BarInPlace", 1);

        	PlaySoundAtEntity("attachb", "25_attach_bar.snt", asEntity, 0.0f, false);

        	RemoveItem(asItem);
        	SetEntityActive(asEntity, false); //Turns off interaction message on area.
        }

        void UseHCOnBricks(string asItem, string asEntity)
        {
        	SetEntityActive("stone_chipper_move_x_1",true);
        	SetEntityActive("stone_hammer_move_1",true);

        	PlaySoundAtEntity("cumble1", "15_rock_break", "AreaHammer", 0, false);

        	SetMoveObjectState("stone_hammer_move_1", 0.25f);
        	SetMoveObjectState("stone_chipper_move_x_1", 1.0f);

        	AddTimer("1", 0.2f, nameof(TimerHammer));
        	AddTimer("2", 0.6f, nameof(TimerHammer));
        	AddTimer("3", 0.8f, nameof(TimerHammer));
        	AddTimer("4", 1.2f, nameof(TimerHammer));
        	AddTimer("5", 1.4f, nameof(TimerHammer));

        	SetEntityActive(asEntity, false); //Turns off interaction message on area.

        	//Remove hammer chisel as not needed any longer
        	if(HasItem("poison_gland")) RemoveItem(asItem);
        }

        void TimerHammer(string asTimer)
        {
        	if(asTimer == "1"){
        		PlaySoundAtEntity("bang", "impact_rock_high", "Player", 0, false);
        		PlaySoundAtEntity("hammer", "15_hammer", "Player", 0, false);
        		SetMoveObjectState("stone_hammer_move_1", 0.0f);
        		SetMoveObjectState("stone_chipper_move_x_1", 0.0f);
        	}
        	if(asTimer == "2"){
        		SetMoveObjectState("stone_hammer_move_1", 0.25f);
        		SetMoveObjectState("stone_chipper_move_x_1", 1.0f);
        	}
        	if(asTimer == "3"){
        		SetMoveObjectState("stone_hammer_move_1", 0.0f);
        		SetMoveObjectState("stone_chipper_move_x_1", 0.0f);
        		PlaySoundAtEntity("bang", "impact_rock_high", "Player", 0, false);
        		PlaySoundAtEntity("hammer", "15_hammer", "Player", 0, false);
        	}
        	if(asTimer == "4"){
        		SetMoveObjectState("stone_hammer_move_1", 0.25f);
        		SetMoveObjectState("stone_chipper_move_x_1", 1.0f);
        	}
        	if(asTimer == "5"){
        		SetEntityActive("stone_chipper_move_x_1",false);
        		SetEntityActive("stone_hammer_move_1",false);
        		PlaySoundAtEntity("bang", "impact_rock_high", "Player", 0, false);
        		PlaySoundAtEntity("hammer", "15_hammer", "Player", 0, false);

        		FadeLightTo("PointLight_21", 0.9f, 0.8f, 0.3f, 0.8f,  -1, 1);
        		CreateParticleSystemAtEntity("dust", "ps_hit_rock.ps", "AreaLooseBricks", false);
        		SetLocalVarInt("UsedHC", 1);
        		CollideBarBendDone("", "", 1);
        	}
        }

        void UseBarOnWall(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level25", nameof(UseBarOnWall), -1);
        }

        //-------------------------------------------------------

        //Bending bar
        void InteractBendBar(string asEntity)
        {
        	PlaySoundAtEntity("rockbars", "15_rock_break", asEntity, 0.5f, false);
        }

        //Bar is has been bent enough
        void CollideBarBendDone(string asParent, string asChild, int alState)
        {
        	FadeLightTo("PointLight_21", 0,0,0,0,  -1, 1);

        	SetEntityActive("wriggle_prison_bar_bend_stone_1", false);

        	if(GetLocalVarInt("BarInPlace") == 1)
        		SetEntityActive("wriggle_prison_bar_obj_dyn_1", true);

        	for(int i=1; i<=3; ++i)
        	{
        		SetPropStaticPhysics("cell_breakable_wall_rock"+i, false);

        		if(GetLocalVarInt("UsedHC") == 1)
        			AddPropImpulse("cell_breakable_wall_rock"+i, 0,0,-1.5f, "World");
        		else
        			AddPropImpulse("cell_breakable_wall_rock"+i, 0,0,1.5f, "World");
        	}
        	AddTimer("ExtraBrickPush", 0.4f, nameof(TimerExtraBrickPush));

        	CreateParticleSystemAtEntity("dust", "ps_break_pottery.ps", "AreaLooseBricks", false);

        	PlayGuiSound("25_break_rock.snt",1.0f);

        	//So player can interact with support beams!
        	SetPropStaticPhysics("cell_breakable_wall_support_1", false);
        }

        void TimerExtraBrickPush(string asTimer)
        {
        	if(GetLocalVarInt("UsedHC") == 1)
        		for(int i=1; i<=3; ++i)
        			AddPropImpulse("cell_breakable_wall_rock"+i, 0,0,-2, "World");
        	else
        		for(int i=1; i<=3; ++i)
        			AddPropImpulse("cell_breakable_wall_rock"+i, 0,0,2, "World");
        }

        void InteractWoodPillar(string asEntity)
        {
        	PlaySoundAtEntity("bendwood", "25_strain_wood", asEntity, 0.1f, false);
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // COLLAPSE WALL
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void CollideCollapseWall(string asParent, string asChild, int alState)
        {
        	////////////////////////////
        	//Turn off areas.
        	for(int i=1; i<=2; ++i)
        		SetEntityActive("AreaCollapseWall_"+i,false);

        	//guardian will no longer eat player.
        	StopGuardianCountDown();

        	//Push back player
        	AddPlayerBodyForce(0,0,-5000, false);

        	//Fade out the hole light.
        	FadeLightTo("PointLight_21", 0,0,0,0,  -1, 1);

        	SetPropHealth("cell_breakable_wall_1", 0);
        	SetPropHealth("cell_breakable_wall_support_1", 0);

        	StartScreenShake(0.2f, 0.3f, 1, 1);

        	PlayMusic("25_puzzle_escape", false, 1.0f, 1.0f, 10, false);

        	//Remove quest and cell door interaction
        	CompleteQuest("25LockedCellDoor", "25LockedCellDoor");
        	SetEntityPlayerInteractCallback("prison_section_plain_1","", false);

        	//Add a delayed timer to have distant howl.
        	AddTimer("DistantHowl", 8, nameof(TimerDistantHowl));

        	SetEntityActive("AreaEscaped", false);
        }

        //-------------------------------------------------------

        void TimerDistantHowl(string asTimer)
        {
        	PlaySoundAtEntity("howl","25_guardian_distant1", "Player", 0, false);

        	AddTimer("StopRadialBlur", 4.5f, nameof(TimerStopRadialBlur));
        	AddTimer("react_scare", 0.5f, nameof(TimerScareReaction));
        	AddTimer("react_breath", 2.5f, nameof(TimerScareReaction));

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

        //-------------------------------------------------------

        ///////////////////////////////
        // ALTERNATE ESCAPE ROUTE
        ////////////////////////////////

        //-------------------------------------------------------

        void CollideEscapeCell(string asParent, string asChild, int alState)
        {
        	AddTimer("DistantHowl", 2, nameof(TimerCollapseWall));

        	StartScreenShake(0.03f, 3.0f, 1.0f, 2.0f);

        	PlaySoundAtEntity("rumble", "general_rock_rumble_no3d.snt", "Player", 2.0f, false);

        	//guardian will no longer eat player.
        	StopGuardianCountDown();
        }

        void TimerCollapseWall(string asTimer)
        {
        	StopSound("rumble", 4.0f);

        	FadeLightTo("PointLight_21", 0,0,0,0,  -1, 1);

        	SetEntityActive("wriggle_prison_bar_bend_stone_1", false);

        	if(GetLocalVarInt("BarInPlace") == 1)
        		SetEntityActive("wriggle_prison_bar_obj_dyn_1", true);

        	for(int i=1; i<=3; ++i)
        	{
        		SetPropStaticPhysics("cell_breakable_wall_rock"+i, false);
        		AddPropImpulse("cell_breakable_wall_rock"+i, 0,0,0.5f, "World");
        	}

        	SetPropStaticPhysics("cell_breakable_wall_support_1", false);

        	SetEntityActive("AreaLooseBricks", false);

        	CollideCollapseWall("", "", 1);
        }

        //-------------------------------------------------------


        ///////////////////////////////////////////////
        // THROW OBJECT DOWN WELL
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void CollideObjectInWellWater(string asParent, string asChild, int alState)
        {
        	SetPropActiveAndFade(asParent, false, 0.7f);
        	PlaySoundAtEntity("WaterSplash","impact_water_med.snt", "AreaBuckSplash", 0, false);

        	AddDebugMessage("Entity that hit water: "+asParent, false);
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // FILL BUCKET WITH WATER
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        //If rope is up and ready for interaction!
        void CollideRopeInteraction(string asParent, string asChild, int alState)
        {
        	//Set the variable that decides if the rope is up or not.
        	SetLocalVarInt("RopeIsUp", alState==1 ? 1 : 0);

        	AddDebugMessage("Rope up: "+alState, false);
        }

        //------------------------------------------

        //If rope is down at bottom
        void CollideRopeDown(string asParent, string asChild, int alState)
        {
        	if(alState!=1) return;

        	AddDebugMessage("Rope is down!", false);

        	// If lowering rope alone or bucked already filled, do nothing
        	if(GetLocalVarInt("BucketOnRope")!=1 || GetLocalVarInt("BucketFilled")==1)
        	{
        		return;
        	}

        	//Make it possible to pick up bone + have correct icon
        	SetEntityPlayerInteractCallback("invisible_box_mass_2_1", nameof(InteractFilledBucket), true);
        	SetEntityCustomFocusCrossHair("invisible_box_mass_2_1","Pick");

        	//need to turn off this area, else it is not possible to interact with entity on rope!
        	SetEntityActive("AreaRopeInteraction", false);

        	PlaySoundAtEntity("WaterSplash","impact_water_med.snt", "AreaBuckSplash", 0, false);

        	//Replace Bucket with filled version
        	SetLocalVarInt("BucketFilled", 1);
        	RemoveAttachedPropFromProp("invisible_box_mass_2_1", "wooden_bucket_onrope_1");
        	AddAttachedPropToProp("invisible_box_mass_2_1", "wooden_bucket_onrope_filled_1", "wooden_bucket_onrope_filled.ent", 0,0,0, 0,0,0);
        }

        //-------------------------------------------------------

        // Tie bucket to rope
        void UseBucketOnRope(string asItem, string asEntity)
        {
        	//////////////////////////////
        	//Rope is in right position
        	if(GetLocalVarInt("RopeIsUp")==1)
        	{
        		AddAttachedPropToProp("invisible_box_mass_2_1", "wooden_bucket_onrope_1", "wooden_bucket_onrope.ent", 0,0,0, 0,0,0);
        		RemoveItem(asItem);

        		SetLocalVarInt("BucketOnRope", 1);

        		PlaySoundAtEntity("attachb", "25_attach_bucket.snt", asEntity, 0.0f, false);
        	}
        	//////////////////////////////
        	//Rope is too low
        	else
        	{
        		SetMessage("Ch03Level25", "RopeTooLow", 0);
        	}
        }

        void UseFilledBucketOnRope(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level25", "BucketAlreadyFilled", 0);
        }

        //-------------------------------------------------------

        // Pick up filled bucket
        void InteractFilledBucket(string asEntity)
        {
        	RemoveAttachedPropFromProp("invisible_box_mass_2_1", "wooden_bucket_onrope_filled_1");
        	GiveItemFromFile("wooden_bucket_filled_1", "wooden_bucket_filled.ent");

        	PlaySoundAtEntity("pickb", "25_pick_bucket_water.snt", "Player", 0.0f, false);

        	//Need to do like this or else a message is not shown!
        	SetMessage("Ch03Level25", "PickedUpFilledBucket", 0);

        	//also need to play pick sound
        	PlaySoundAtEntity("PickUpItem", "pick_generic.snt", "Player", 0.1f, false);
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // POUR WATER ON PIPE
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void InteractPipeHole(string asEntity)
        {
        	SetMessage("Ch03Level25", nameof(InteractPipeHole), -1);
        }

        //-------------------------------------------------------

        void UseBucketOnPipe(string asItem, string asEntity)
        {
        	// Bucket is empty...
        	if(asItem=="wooden_bucket_1")
        	{
        		SetMessage("Ch03Level25", "BucketEmpty", -1);
        	}
        	else
        	{
        		// Start bucket pouring sequence -> this entity will trigger a sticky area callback that will start the water pouring
        		SetPropActiveAndFade("wooden_bucket_filled_seq", true, 0.5f);
        		RemoveItem("wooden_bucket_filled_1");

        		SetEntityPlayerInteractCallback("AreaBucketPipe", "", false);
        		SetEntityPlayerInteractCallback("AreaBucketPipe_2", "", false);

        		StartPlayerLookAt("AreaBucketPipe", 1, 3, "");

        		if(GetLocalVarInt("PipeEmpty") == 0)
        			SetLocalVarInt("PipeEmpty", 1);
        	}
        }

        // This is called when the bucket attaches to the "pour" sticky area
        void BucketPourWater(string asStickyArea, string asBodyName)
        {
        	CreateParticleSystemAtEntity("PourWater","ps_bucket_pour_water.ps" ,"AreaWaterPour", false);

        	PlaySoundAtEntity("water_s", "25_water_stream", "Player", 1.0f, false);
        	PlaySoundAtEntity("water_s2", "16_water_stream_heavy", "Player", 2.0f, false);
        	PlaySoundAtEntity("water_s3", "12_epoxy_flow", "Player", 1.0f, false);

        	AddTimer("FlushWater1", 0.9f, nameof(TimerFlushWater));
        	AddTimer("FlushWater2", 1.7f, nameof(TimerFlushWater));
        }

        // Do timed events for water in pipe
        void TimerFlushWater(string asTimer)
        {
        	if(asTimer=="FlushWater1")
        	{
        		StopPlayerLookAt();

        		// Add water flowing inside pipe
        		CreateParticleSystemAtEntity("FlushWater1", "ps_bucket_flush_water.ps", "AreaFlushWater1", false);
        		SetPropActiveAndFade("key_tomb_rusty_1_show", false, 0.25f);
        	}
        	else if(asTimer=="FlushWater2")
        	{
        		// Make bucket fall after pouring water
        		SetEntityActive("AreaBucketPour", false);
        		DetachFromStickyArea("AreaBucketPour");
        		SetEntityActive("wooden_bucket_filled_seq", false);
        		SetEntityActive("wooden_bucket_object_1", true);
        		//SetPropStaticPhysics("wooden_bucket_filled_seq", false);

        		AddPropImpulse("wooden_bucket_object_1", -0.5f, 0,0, "World"); //Make sure the bucket does not get stuck and falls down on floor.

        		//Fade light to make clearer.
        		if(GetLocalVarInt("PipeEmpty") == 1)
        			FadeLightTo("PointLight_58", 0.3f, 0.4f, 1.0f, 0.8f,  -1, 1);

        		// Add water coming out of pipe and flushing key out
        		CreateParticleSystemAtEntity("FlushWater2", "ps_bucket_flush_water.ps", "AreaFlushWater2", false);
        		CreateParticleSystemAtEntity("FlushWaterImpact", "ps_bucket_impact_water.ps", "AreaLookAtKey", false);

        		StopSound("water_s", 3.0f);
        		StopSound("water_s2", 2.0f);
        		StopSound("water_s3", 3.0f);

        		SetPropActiveAndFade("key_tomb_rusty_1", true, 0.25f);
        		AddPropImpulse("key_tomb_rusty_1", -0.1f, 0, 0, "World");

        		if(GetLocalVarInt("PipeEmpty") == 1){
        			PlaySoundAtEntity("key_plink", "pick_key", "key_tomb_rusty_1", 0.25f, false);
        			PlaySoundAtEntity("key_plink2", "25_drop_key", "key_tomb_rusty_1", 0.25f, false);
        		}
        	}

        }

        //-------------------------------------------------------

        void PickUpKey(string asEntity, string asType)
        {
        	FadeLightTo("PointLight_58", 0,0,0,0,  -1, 1); //Fade out helper light when picked up.

        	GiveSanityBoostSmall();
        	PlayMusic("10_puzzle01.ogg", false, 0.8f, 0.5f, 9, false);
        }

        //-------------------------------------------------------

        /////////////////////////////////////
        //USE BAR TO GET KEY INSTEAD
        ////////////////////////////////////

        //-------------------------------------------------------

        void UseBarOnPipe(string asItem, string asEntity)
        {
        	RemoveItem(asItem);
        	SetEntityActive("wriggle_prison_bar_key_pusher_1", true);
        	PlaySoundAtEntity("attachb", "25_attach_bar_metal.snt", asEntity, 0.0f, false);
        	CreateParticleSystemAtEntity("kapling", "ps_hit_metal.ps", "wriggle_prison_bar_key_pusher_1", false);
        }

        void CollidePushKey(string asParent, string asChild, int alState)
        {
        	SetLeverStuckState(asParent, 1, true);
        	SetEntityPlayerInteractCallback(asParent, nameof(InteractBarInPipe), true);

        	if(GetLocalVarInt("PipeEmpty") == 1) return;

        	SetLocalVarInt("PipeEmpty", 2);

        	SetPropActiveAndFade("key_tomb_rusty_1_show", false, 0.25f);
        	PlaySoundAtEntity("push", "scrape_metal", "AreaSeeKey", 1.0f, false);
        	AddTimer("pushedkey", 1.0f, nameof(TimerPushedKey));
        }

        void TimerPushedKey(string asTimer)
        {
        	StopSound("push", 0.5f);

        	SetPropActiveAndFade("key_tomb_rusty_1", true, 0.25f);
        	AddPropImpulse("key_tomb_rusty_1", -0.1f, 0, 0, "World");

        	PlaySoundAtEntity("key_plink", "pick_key", "key_tomb_rusty_1", 0.25f, false);
        	PlaySoundAtEntity("key_plink2", "25_drop_key", "key_tomb_rusty_1", 0.25f, false);

        	FadeLightTo("PointLight_58", 0.3f, 0.4f, 1.0f, 0.8f,  -1, 1);
        }

        void InteractBarInPipe(string asEntity)
        {
        	SetMessage("Ch02Level17", "LockedWheel", 0);
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // LOCKED ROOM DOOR
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void InteractRoomDoor(string asEntity)
        {
        	SetMessage("Ch03Level25", "InteractLockedRoomDoor", 0);
        	AddQuest("25LockedRoomDoor", "25LockedRoomDoor");
        }

        void UseKeyOnDoor(string asItem, string asEntity)
        {
        	PlaySoundAtEntity("unlocked", "unlock_door", asEntity, 0.0f, false);

        	SetEntityPlayerInteractCallback("prison_1", "", false); //remove any interaction callback from door.

        	CompleteQuest("25LockedRoomDoor", "25LockedRoomDoor");

        	SetSwingDoorLocked(asEntity, false, false);
        	RemoveItem(asItem);
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // GUARDIAN TEASING (Stuff that happens after first death)
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void CollideGuardianTease1(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("GuardHowl","25_guardian_distant1.snt", "Player", 0, false);
        }

        void CollideGuardianTease2(string asParent, string asChild, int alState)
        {
        	StartScreenShake(0.07f, 1, 1, 1);
        	PlaySoundAtEntity("rumpustherumble","general_rock_rumble_no3d.snt", "Player", 2,false);
        	AddTimer("rumpustherumble", 1.0f, nameof(TimerStopRumbleSound));
        	PlaySoundAtEntity("GuardHowl","25_guardian_idle.snt", "Player", 0, false);
        }

        void CollideGuardianTease3(string asParent, string asChild, int alState)
        {
        	StartScreenShake(0.12f, 1, 0.5f, 1);
        	PlaySoundAtEntity("rumpustherumble","general_rock_rumble_no3d.snt", "Player", 2,false);
        	AddTimer("rumpustherumble", 1.0f, nameof(TimerStopRumbleSound));
        	PlaySoundAtEntity("GuardHowl","25_guardian_distant3.snt", "Player", 0, false);
        }

        //-------------------------------------------------------

        void CollideWaterLurkerTease(string asParent, string asChild, int alState)
        {
        	PlayMusic("04_event_stairs.ogg", false, 0.7f, 0.1f, 10, false);

        	AddTimer("WaterLurkerRoar", 1.2f, nameof(TimerWaterLurkerRoar));
        	AddTimer("AreaGuardianEffectsFloor_lump_49", 0.5f, nameof(TimerWaterLurkerSplash));
        	AddTimer("AreaGuardianEffectsFloor_lump_48", 2, nameof(TimerWaterLurkerSplash));
        	AddTimer("AreaGuardianEffectsFloor_lump_47", 3.3f, nameof(TimerWaterLurkerSplash));
        	AddTimer("AreaGuardianEffectsFloor_lump_46", 4.1f, nameof(TimerWaterLurkerSplash));
        }

        void TimerWaterLurkerRoar(string asTimer)
        {
        	PlaySoundAtEntity("LurkerSound","water_lurker_hunt.snt", "AreaGuardianEffectsFloor_lump_48", 0, false);
        	GiveSanityDamage(15.0f, true);
        	StartScreenShake(0.003f, 0.0f, 0.5f, 3.0f);
        }

        void TimerWaterLurkerSplash(string asTimer)
        {
        	PlaySoundAtEntity("LurkerSound","impact_water_high.snt", asTimer, 0, false);
        	CreateParticleSystemAtEntity("LurkerPS", "waterlurker_walk_splash", asTimer, false);
        }

        //-------------------------------------------------------

        void CollideCreepMusicTease(string asParent, string asChild, int alState)
        {
        	PlayMusic("12_amb.ogg", true, 1, 3, 0, false);
        }

        //-------------------------------------------------------

        void CollideLowRumbleTease(string asParent, string asChild, int alState)
        {
        	//Slam door
        	SetSwingDoorLocked("GuardianDoor1", true, true);
        	PlaySoundAtEntity("SlamDoor","scare_slam_door.snt", "GuardianDoor1", 0, false);

        	//Shake with low freq rumble.
        	StartScreenShake(0.03f, 1, 2, 3);
        	PlaySoundAtEntity("LowFreqLoop","00_loop.snt", "Player", 1,false);
        	PlaySoundAtEntity("rumpustherumble","general_rock_rumble_no3d.snt", "Player", 3,false);
        	AddTimer("FadeOutRumbleTeaseLoop",  3, nameof(TimerFadeOutRumbleTeaseLoop));
        	AddTimer("StopSpeed",  30, nameof(TimerFadeOutRumbleTeaseLoop));
        	FadeGlobalSoundSpeed(0.3f, 60.0f);
        }

        void TimerFadeOutRumbleTeaseLoop(string asTimer)
        {
        	StopSound("LowFreqLoop", 3);
        	StopSound("rumpustherumble", 3);
        	if(asTimer == "StopSpeed")
        		FadeGlobalSoundSpeed(1.0f, 20.0f);
        }

        //-------------------------------------------------------

        void CollideRoarTease(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("GuardHowl","25_guardian_distant1.snt", "Player", 0, false);
        }

        //-------------------------------------------------------

        void CollideGuardRumbleTease(string asParent, string asChild, int alState)
        {
        	StartScreenShake(0.07f, 1, 1, 1);
        	PlaySoundAtEntity("rumpustherumble","general_rock_rumble_no3d.snt", "Player", 2,false);
        	PlaySoundAtEntity("GuardHowl","25_guardian_idle.snt", "Player", 0, false);
        	AddTimer("rumpustherumble", 1.0f, nameof(TimerStopRumbleSound));
        }


        //-------------------------------------------------------

        void CollideScreamTease(string asParent, string asChild, int alState)
        {
        	StartScreenShake(0.15f, 1, 2, 3);
        	PlaySoundAtEntity("rumpustherumble","general_rock_rumble_no3d.snt", "Player", 2,false);
        	AddTimer("rumpustherumble", 1.0f, nameof(TimerStopRumbleSound));
        	PlaySoundAtEntity("GuardHowl","25_guardian_ontop.snt", "Player", 0, false);
        }

        void TimerStopRumbleSound(string asTimer)
        {
        	StopSound(asTimer, 3.0f);
        }
        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // GUARDIAN CHASE
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        // Guardian moves in "steps", every step has a "lump" and a "kill area" associated to it
        // One can do events in a step by creating a EventStepX(string &in) function,
        // with X being the index of the step.

        int glGuardianSteps = 59;
        float gfFadeTime = 0.5f;

        //Defines what steps are part of the dual paths (at the cavein)
        int glDualPathStartIdx = 28;
        int glDualPathEndIdx = 35;

        //Defines what steps are in water.
        int glWaterPathStartIdx = 43;
        int glWaterPathEndIdx = 51;

        //-------------------------------------------------------

        //Standard effect when the slime appears
        void GuardianGroundEffect(string asArea, bool abPlaySound)
        {
        	CreateParticleSystemAtEntity("GuardianSlimeAppearEffect","ps_guardian_appear_explosion.ps", asArea, false);
        	if(abPlaySound)
        		PlaySoundAtEntity("GuardianSplashSound", "25_guardian_slime_appear.snt", asArea, 0, false);

        }

        //-------------------------------------------------------

        ////////////////////////////////////////////////////////////////////////////////
        // Helper func that activates a "lump" of slime and its associated light and kill area
        // Also calls an event function "EventStep"+idx
        void SetLumpActive(int alLumpIdx, bool abX, float afFadeMul, float afDelayToEvent)
        {
        	AddDebugMessage("Activating lump with index " + alLumpIdx + " Event: EventLump" + alLumpIdx, false);

        	////////////////////////////
        	// Show / hide slime
        	SetPropActiveAndFade("*lump_"+ alLumpIdx + "_", abX, gfFadeTime*afFadeMul);

        	// Get cave in path
        	int lCaveInPathUnblocked = GetLocalVarInt("CaveInPathUnblocked");

        	//////////////////////////
        	// Activate damage area
        	string sDmgAreaName = "AreaGuardianKill_lump_" + alLumpIdx;
        	if(lCaveInPathUnblocked!=0 && GetEntityExists(sDmgAreaName)==false)
        	{
        		for(int i=1; i<=2; ++i)
        			SetEntityActive(sDmgAreaName + "_" + i, abX);
        	}
        	else
        	{
        		SetEntityActive(sDmgAreaName, abX);
        	}

        	// If we are deactivating, skip calling any events
        	if(abX==false)
        	{
        		return;
        	}

        	// Call event, if any
        	AddTimer("EventStep"+alLumpIdx, afDelayToEvent, "EventStep"+alLumpIdx);

        	/////////////////////////
        	// Add effects when slime appears
        	StartScreenShake(0.1f, RandFloat(0.15f,0.6f), 0, 0.1f);

        	//Dual path
        	if(glDualPathStartIdx <= alLumpIdx && alLumpIdx <= glDualPathEndIdx)
        	{
        		for(int i=1; i<=2; ++i)
        			GuardianGroundEffect("AreaGuardianEffectsFloor_lump_" + alLumpIdx+"_"+i, i==2); //Only want sound from one side!
        	}
        	//Normal path
        	else
        	{
        		GuardianGroundEffect("AreaGuardianEffectsFloor_lump_" + alLumpIdx, true);

        		//Effect is in water!
        		if(glWaterPathStartIdx <= alLumpIdx && alLumpIdx <= glWaterPathEndIdx)
        		{
        			PlaySoundAtEntity("GuardianSplashSound", "impact_water_high.snt", "AreaGuardianEffectsFloor_lump_" + alLumpIdx, 0, false);
        			CreateParticleSystemAtEntity("GuardianWaterSplash","waterlurker_run_splash.ps", "AreaGuardianEffectsFloor_lump_" + alLumpIdx, false);
        		}
        	}

        	//Random scream (33% chance), and only after a appearances.
        	if(alLumpIdx > 5 && RandInt(0,2)==0)
        	{
        		PlaySoundAtEntity("GuardianScream", "25_guardian_ontop.snt", "Player", 0, false);
        	}
        }

        //-------------------------------------------------------

        void TimerGuardianExtraSlime(string asName)
        {
        	SetPropActiveAndFade(asName, true, gfFadeTime);
        }

        //-------------------------------------------------------

        // Start guardian
        void CollideStartGuardian(string asParent, string asChild, int alState)
        {
        	///////////////////////////
        	//Check if player has already been killed and if this is the first start area
        	if(GetLocalVarInt("KilledByGuardian")>0 && asChild=="AreaStartGuardian_1")
        	{
        		//Turn on some teasing effect areas
        		SetEntityActive("AreaGuardianTease_*", true);

        		return;
        	}

        	// Set checkpoint
        	CheckPoint("check01", "PlayerStartArea_Guardian", nameof(CheckPoint01), "Hints", "DeathGuardian_25_CellTunnels");

        	//Start blur effect
        	SetRadialBlurStartDist(0.2f);
        	FadeRadialBlurTo(0.07f, 0.015f);

        	//Start music
        	PlayMusic("08_amb.ogg", true, 1, 1, 0, false);

        	//Play activate sound.
        	PlaySoundAtEntity("GuardHowl","25_guardian_activated.snt", "Player", 0, false);
        	PlaySoundAtEntity("SlimeLoopOnPlayer","25_slime_loop.snt", "Player", 2, false);

        	if(GetLocalVarInt("FirstSoundRun") != 1)
        		PlaySoundAtEntity("end_amb","amb_guardian.snt", "torch_static01_15", 2, true);
        	SetLocalVarInt("FirstSoundRun", 1);

        	//Reset variables and then start timer that starts the steps
        	SetLocalVarInt("GuardianCurrentStep", asChild=="AreaStartGuardian_1" ? 1 :  23); //If 2nd start area, we want the guardian to start closer to the player.
        	SetLocalVarInt("GuardianStop", 0);
        	SetLocalVarFloat("guardian_time", 2.0f);
        	AddTimer(nameof(TimerGuardian), 0, nameof(TimerGuardian));

        	AddDebugMessage("Starting guardian!", false);

        	// Disable further reactivation for now
        	for(int i=1;i<=2;++i)
        		SetEntityActive("AreaStartGuardian_"+i, false);
        }


        //-------------------------------------------------------

        void CheckPoint01(string asName, int alCount)
        {
        	StopSound("GuardianScream",1);
        	ResetGuardianSequence();

        	// Lock door into chase tunnel
        	SetSwingDoorLocked("prison_1", true, false);

        	///////////////////////////////
        	//After second death, no more guardian chase.
        	if(alCount>=1)
        	{
        		///////////////////////////////
        		// Special events after second death
        		if(alCount==1)
        		{
        			// Turn off light in level to make it darker.
        			FadeLightTo("PointLight_7", 0,0,0.1f,0, -1, 0.1f);
        			FadeLightTo("PointLight_8", 0,0,0.1f,0, -1, 0.1f);
        			FadeLightTo("PointLight_10", 0,0,0.1f,0, -1, 0.1f);
        			FadeLightTo("PointLight_11", 0,0,0.1f,0, -1, 0.1f);
        			FadeLightTo("PointLight_12", 0,0,0.1f,0, -1, 0.1f);
        			FadeLightTo("PointLight_16", 0,0,0.1f,0, -1, 0.1f);
        			FadeLightTo("PointLight_17", 0,0,0.1f,0, -1, 0.1f);
        			FadeLightTo("PointLight_22", 0,0,0.1f,0, -1, 0.1f);
        			FadeLightTo("PointLight_23", 0,0,0.1f,0, -1, 0.1f);

        			FadeLightTo("SpotLight_14", 0,0,0,0, -1, 0.1f);
        			FadeLightTo("SpotLight_9", 0,0,0,0, -1, 0.1f);
        			FadeLightTo("SpotLight_8", 0,0,0,0, -1, 0.1f);
        			FadeLightTo("SpotLight_7", 0,0,0,0, -1, 0.1f);
        			FadeLightTo("SpotLight_3", 0,0,0,0, -1, 0.1f);
        			FadeLightTo("SpotLight_2", 0,0,0,0, -1, 0.1f);
        			FadeLightTo("SpotLight_4", 0,0,0,0, -1, 0.1f);
        			FadeLightTo("SpotLight_5", 0,0,0,0, -1, 0.1f);
        			FadeLightTo("SpotLight_6", 0,0,0,0, -1, 0.1f);

        			//Remove rubble
        			SetEntityActive("wood_beam_3", false);
        			SetEntityActive("barrel01_1", false);
        			SetEntityActive("barrel01_2", false);

        			//Enable extra scare events
        			SetEntityActive("AreaWaterLurkerTease", true);
        			SetEntityActive("AreaCreepMusicTease", true);
        			SetEntityActive("AreaLowRumbleTease", true);
        			SetEntityActive("AreaRoarTease", true);
        			SetEntityActive("AreaGuardRumbleTease", true);
        			SetEntityActive("AreaScreamTease", true);

        		}

        		///////////////////
        		// Turn off guardian chase
        		for(int i=1;i<=2;++i)
        		{
        			SetEntityActive("AreaStartGuardian_"+i, false);
        		}
        	}
        }

        //-------------------------------------------------------

        // Player gets killed by guardian
        void CollideAreaGuardianKill(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Killing player", false);

        	AddLocalVarInt("KilledByGuardian", 1);
        	GivePlayerDamage(101, "BloodSplash", true, true);

        	//This makes the guardian stand still
        	SetLocalVarFloat("guardian_time", 3000.0f);

        	StartScreenShake(0.2f, 0.5f, 0.5f, 0.5f);
        	PlayGuiSound("water_lurker_eat_rev2.ogg", 0.8f);
        	PlayGuiSound("attack_claw_hit03.ogg", 0.8f);

        	StopSound("SlimeLoopOnPlayer", 2.0f);
        	UnlockAchievement("Quitter");
        }

        //-------------------------------------------------------

        void ResetGuardianSequence()
        {
        	for(int i=1;i<=glGuardianSteps;++i)
        		SetLumpActive(i, false, 0, 0);

        	// Stops any further advance
        	SetLocalVarInt("GuardianStop",1);

        	////////////////////////////////////////
        	// Reset cave in
        	SetLocalVarInt("CaveInUnblockedPath", 0);
        	for(int i=1;i<=2;++i)
        	{
        		for(int j=1;j<=4;++j)
        		{
        			SetEntityActive("cave_in_"+i+"_"+j, false);
        		}
        	}

        	//Reset rumble area
        	SetLocalVarInt("RumbleSound", 0);

        	//Reset effects
        	FadeRadialBlurTo(0, 0.02f);

        	// Reset props
        	ResetProp("GuardianDoor1");
        	ResetProp("GuardianDoor2");
        	ResetProp("wood_beam_3");
        	ResetProp("barrel01_2");
        	ResetProp("barrel01_1");
        	ResetProp("stone_small01_brown_1");
        	ResetProp("stone_med01_brown_1");
        	ResetProp("torch_static01_13");
        	SetLampLit("torch_static01_14", true, false);
        	SetLampLit("torch_static01_15", true, false);
        	SetLampLit("torch_static01_16", true, false);

        	//Reset particles
        	DestroyParticleSystem("GuardianEndFearFog");

        	// Reset areas
        	SetEntityActive("AreaAfterCaveIn", true);

        	//Reset lights
        	FadeOutGuardianLight("PointLight_60");
        	FadeOutGuardianLight("PointLight_59");
        	FadeOutGuardianLight("PointLight_61");
        	FadeOutGuardianLight("PointLight_62");
        	FadeOutGuardianLight("PointLight_63");
        	FadeOutGuardianLight("PointLight_64");
        	FadeOutGuardianLight("PointLight_65");
        	FadeOutGuardianLight("PointLight_66");
        	FadeOutGuardianLight("PointLight_67");
        	FadeOutGuardianLight("PointLight_68");
        	FadeOutGuardianLight("PointLight_69");
        	FadeOutGuardianLight("PointLight_71");
        	FadeOutGuardianLight("PointLight_72");
        	FadeOutGuardianLight("PointLight_74");
        	FadeOutGuardianLight("PointLight_75");
        	FadeOutGuardianLight("PointLight_76");
        	FadeOutGuardianLight("PointLight_77");
        	FadeOutGuardianLight("PointLight_78");
        	FadeOutGuardianLight("PointLight_54");
        	FadeOutGuardianLight("PointLight_79");
        	FadeOutGuardianLight("PointLight_40");
        	FadeOutGuardianLight("PointLight_53");
        	FadeOutGuardianLight("PointLight_39");
        	FadeOutGuardianLight("PointLight_80");
        	FadeOutGuardianLight("PointLight_38");
        	FadeOutGuardianLight("PointLight_81");


        	FadeLightToBlue("PointLight_7");
        	FadeLightToBlue("PointLight_14");
        	FadeLightToBlue("PointLight_8");
        	FadeLightToBlue("PointLight_18");
        	FadeLightToBlue("PointLight_9");
        	FadeLightToBlue("PointLight_10");
        	FadeLightToBlue("PointLight_11");
        	FadeLightToBlue("PointLight_12");
        	FadeLightToBlue("PointLight_13");
        	FadeLightToBlue("PointLight_15");
        	FadeLightToBlue("PointLight_17");
        	FadeLightToBlue("PointLight_23");
        	FadeLightToBlue("PointLight_24");
        	FadeLightToBlue("PointLight_22");
        	FadeLightToBlue("PointLight_16");
        	FadeLightToBlue("PointLight_19");

        	SetLampLit("torch_static01_13", false, true);


        	// Reset trigger areas
        	for(int i=1;i<=2;++i)
        		SetEntityActive("AreaStartGuardian_"+i, true);

        	SetEntityActive("slime_seq*", false);
        }

        void FadeOutGuardianLight(string asName)
        {
        	FadeLightTo(asName, 0,0,0,0, -1, 0.2f);
        }

        void FadeLightToBlue(string asName)
        {
        	FadeLightTo(asName, 0.1f,0.15f,0.2f,0, -1, 0.2f);
        }

        //-------------------------------------------------------

        //////////////////////////////////////////////////
        // TimerGuardian : this function controls the
        //	advance of the Guardian
        void TimerGuardian(string asTimer)
        {
        	if(GetLocalVarInt("GuardianStop")==1)
        		return;

        	int lCurrentStep = GetLocalVarInt("GuardianCurrentStep");
        	if(lCurrentStep>glGuardianSteps)return; //All steps are completed!

        	float fGuardianTime = GetLocalVarFloat("guardian_time");

        	AddDebugMessage("Guardian doing stuff! Current step = " + lCurrentStep, false);

        	SetLumpActive(lCurrentStep, true, fGuardianTime, 0);
        	SetLocalVarInt("GuardianCurrentStep", ++lCurrentStep);

        	AddTimer( asTimer, fGuardianTime, asTimer );
        }

        //-----------------------------------------------------

        //////////////////////////////////////////////////////////////
        // GUARDIAN CHASE EVENTS
        ///////////////////////////////////////////////////////////

        //-----------------------------------------------------

        // Start Guardian
        void EventStep1(string asX)
        {
        	AddDebugMessage("Guardian just got activated", false);
        	PlaySoundAtEntity("GuardianRoar", "25_guardian_activated", "Player", 0, false);

        	StartScreenShake(0.3f, 2, 0.75f, 0.75f);
        	PlaySoundAtEntity("rumpustherumble","general_rock_rumble_no3d.snt", "Player", 2,false);
        	AddTimer("rumpustherumble", 1.0f, nameof(TimerStopRumbleSound));
        }


        //-----------------------------------------------------

        void FadeInGuardianLight(string asName)
        {
        	FadeLightTo(asName, 1,0,0.2f,0.6f, -1, 1);
        }


        //////////////////////////////////
        // Fade lights

        void EventStep2(string asX){
        	FadeInGuardianLight("PointLight_60");
        }
        void EventStep4(string asX){
        	FadeInGuardianLight("PointLight_7");
        }

        void EventStep6(string asX) {
        	FadeInGuardianLight("PointLight_59");
        }
        void EventStep7(string asX) {
        	FadeInGuardianLight("PointLight_8");
        }
        void EventStep8(string asX) {
        	FadeInGuardianLight("PointLight_18");
        }

        void EventStep11(string asX) {
        	FadeInGuardianLight("PointLight_61");
        }

        void EventStep12(string asX) {
        	FadeInGuardianLight("PointLight_62");
        }

        void EventStep14(string asX) {
        	FadeInGuardianLight("PointLight_10");
        }

        void EventStep18(string asX) {
        	FadeInGuardianLight("PointLight_11");
        }

        void EventStep21(string asX) {
        	FadeInGuardianLight("PointLight_63");
        }

        void EventStep22(string asX) {
        	FadeInGuardianLight("PointLight_64");
        }

        void EventStep24(string asX) {
        	FadeInGuardianLight("PointLight_12");
        }

        void EventStep26(string asX) {
        	FadeInGuardianLight("PointLight_65");
        }

        void EventStep27(string asX) {
        	FadeInGuardianLight("PointLight_13");
        }

        void EventStep28(string asX) {
        	FadeInGuardianLight("PointLight_66");
        	FadeInGuardianLight("PointLight_67");
        }

        void EventStep29(string asX) {
        	FadeInGuardianLight("PointLight_68");
        	FadeInGuardianLight("PointLight_69");
        }

        void EventStep32(string asX) {
        	FadeInGuardianLight("PointLight_19");
        	FadeInGuardianLight("PointLight_15");

        	SetLampLit("torch_static01_13", false, true);
        }


        void EventStep34(string asX) {
        	FadeInGuardianLight("PointLight_71");
        	FadeInGuardianLight("PointLight_72");
        }

        void EventStep36(string asX) {
        	FadeInGuardianLight("PointLight_74");
        	FadeInGuardianLight("PointLight_75");
        }

        void EventStep37(string asX) {
        	FadeInGuardianLight("PointLight_16");
        }

        void EventStep38(string asX) {
        	FadeInGuardianLight("PointLight_76");
        }

        void EventStep40(string asX) {
        	FadeInGuardianLight("PointLight_17");
        }

        void EventStep41(string asX) {
        	FadeInGuardianLight("PointLight_77");
        }

        void EventStep42(string asX) {
        	FadeInGuardianLight("PointLight_78");
        }

        void EventStep43(string asX) {
        	FadeInGuardianLight("PointLight_54");

        	//Reached water, slow down a bit!
        	SetLocalVarFloat("guardian_time", 1.6f);
        }

        void EventStep44(string asX) {
        	FadeInGuardianLight("PointLight_79");
        }

        void EventStep45(string asX) {
        	FadeInGuardianLight("PointLight_40");
        }

        void EventStep47(string asX) {
        	FadeInGuardianLight("PointLight_53");
        }

        void EventStep49(string asX) {
        	FadeInGuardianLight("PointLight_39");
        }

        void EventStep50(string asX) {
        	FadeInGuardianLight("PointLight_80");
        }

        void EventStep51(string asX) {
        	FadeInGuardianLight("PointLight_38");

        	//Speed up again for final run
        	SetLocalVarFloat("guardian_time", 1.1f);
        }

        void EventStep53(string asX) {
        	FadeInGuardianLight("PointLight_22");
        }

        void EventStep55(string asX) {
        	FadeInGuardianLight("PointLight_81");
        }

        void EventStep56(string asX) {
        	FadeInGuardianLight("PointLight_23");
        }

        void EventStep58(string asX) {
        	FadeInGuardianLight("PointLight_24");
        }

        //-----------------------------------------------------

        // Break first door
        void EventStep9(string asX)
        {
        	AddDebugMessage("Guardian no like door, guardian break!", false);
        	PlaySoundAtEntity("GuardianRoar", "25_guardian_ontop", "Player", 0, false);

        	StartScreenShake(0.2f, 1, 0, 0.75f);

        	SetPropHealth("GuardianDoor1", 0);
        	AddTimer(nameof(TimerDelayedDoorImpulse01), 0.2f, nameof(TimerDelayedDoorImpulse01));

        	// Guardian enters now in blocked door corridor, slow down a bit
        	SetLocalVarFloat("guardian_time", 1.5f);
        }

        void TimerDelayedDoorImpulse01(string asTimer)
        {
        	AddPropImpulse("GuardianDoor1_broken", 0, 0, 4, "world");
        }


        //-----------------------------------------------------

        // Spawn more slime
        void EventStep10(string asX)
        {
        	//Fade light too!
        	FadeInGuardianLight("PointLight_9");

        	//More slime
        	float fTime =0;
        	for(int i=1;i<=4;++i)
        	{
        		AddTimer("slime_seq_" + i, fTime, nameof(TimerGuardianExtraSlime));
        		fTime += 0.1f;
        	}
        }

        //-----------------------------------------------------

        // Break second door
        void EventStep16(string asX)
        {
        	//Speed up slime movements a bit
        	SetLocalVarFloat("guardian_time", 1.5f);

        	FadeInGuardianLight("PointLight_14");

        	AddDebugMessage("Guardian no like door, guardian break!", false);
        	PlaySoundAtEntity("GuardianRoar", "25_guardian_ontop", "Player", 0, false);

        	// Temp so no crash happens
        	SetSwingDoorClosed("GuardianDoor2", true, false);

        	StartScreenShake(0.2f, 1, 0, 0.75f);

        	SetPropHealth("GuardianDoor2", 0);
        	AddTimer(nameof(TimerDelayedDoorImpulse02), 0.2f, nameof(TimerDelayedDoorImpulse02));
        }

        void TimerDelayedDoorImpulse02(string asTimer)
        {
        	AddPropImpulse("GuardianDoor2_broken", 4, 0, 0, "world");
        }

        //-------------------------------------------------------

        ///////////////////////////////////////
        //This is the final step, Add some extra effects + turn off the torches in the stair case.
        void EventStep59(string asX)
        {
        	//Looping particle to make sure the "resting pos" looks more fearful
        	CreateParticleSystemAtEntity("GuardianEndFearFog", "ps_guardian_danger_fog_loop.ps", "AreaGuardianKill_lump_59", false);

        	AddTimer("PutOutTorches", 1, nameof(TimerPutOutTorches)); //Start putting out the torches on the stair case
        }

        string[] gvPutOutTorches_Lamps = {"torch_static01_14", "torch_static01_15", "torch_static01_16"};
        string[] gvPutOutTorches_Lights = {"PointLight_25", "PointLight_26", "PointLight_27"};

        void TimerPutOutTorches(string asTimer)
        {
        	int lCount = GetLocalVarInt("PutOutTorchesCount");
        	AddLocalVarInt("PutOutTorchesCount",1);

        	// AngelScript aborts this call on an invalid array index, after incrementing the count.
        	if(lCount < 0 || lCount >= gvPutOutTorches_Lamps.Length) return;

        	SetLampLit(gvPutOutTorches_Lamps[lCount], false, true);
        	FadeLightTo(gvPutOutTorches_Lights[lCount], 0,0,0,0, -1, 1);

        	StartScreenShake(0.1f, RandFloat(0.15f,0.6f), 0, 0.1f);
        	PlaySoundAtEntity("GuardianSplashSound", "25_guardian_slime_appear.snt", "Player", 0, false);
        	PlaySoundAtEntity("rumpustherumble","general_rock_rumble_no3d.snt", "Player", 1,false);
        	AddTimer("rumpustherumble", 0.3f, nameof(TimerStopRumbleSound));

        	if(lCount < 2)
        		AddTimer("PutOutTorches", 2, nameof(TimerPutOutTorches));
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        // CAVE IN
        ///////////////////////////////////////////////

        //-------------------------------------------------------
        //rumble sound
        void CollideAreaBeginRumble(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("RumbleSound") == 1) return;

        	SetLocalVarInt("RumbleSound", 1);

        	PlaySoundAtEntity("rumblex", "general_rock_rumble_no3d", "Player", 0.5f, false);
        	StartScreenShake(0.05f, 4, 4,4);
        }

        // Block current path
        void CollideAreaCaveIn(string asParent, string asChild, int alState)
        {
        	int lPathToBlock;
        	if(asChild=="AreaCaveIn1")
        	{
        		lPathToBlock = 1;
        	}
        	else
        	{
        		SetLampLit("torch_static01_13", false, true);
        		lPathToBlock = 2;
        	}

        	CreateParticleSystemAtEntity("pscave", "ps_dust_break_25.ps", asChild+"Effect", false);

        	SetLocalVarInt("CaveInPathUnblocked", 3-lPathToBlock);

        	//////////////////////////
        	//Slow down the guardian
        	SetLocalVarFloat("guardian_time", 1.5f);//Slow down a lot!

        	//////////////////////////
        	//Effects
        	StartScreenShake(0.25f, 0, 2,1);

                PlaySoundAtEntity("CaveInRumble", "general_rock_rumble_no3d.snt", "Player", 0.5f, false);
                PlaySoundAtEntity("CaveInExpl", "explosion_rock_large.snt", "AreaCaveInEffect"+lPathToBlock, 0, false);
                AddTimer("StopRockRumble", 1, nameof(TimerStopRockRumble));

        	CreateParticleSystemAtEntity("fogarea", "ps_area_fog_large", "AreaCaveInEffect"+lPathToBlock, false); //Saving only complicates reset, so skip!
        	CreateParticleSystemAtEntity("fogarea", "ps_area_fog_large", "AreaCaveInEffect"+lPathToBlock, false); //Saving only complicates reset, so skip!

        	//CreateParticleSystemAtEntity("fogarea", "ps_break_cavein.ps", "AreaCaveInSmokeEffect"+lPathToBlock, false); <- Does not look that good...

        	//////////////////////////
        	//Show cavein models
        	for(int i=1;i<=4;++i)
        		SetPropActiveAndFade("cave_in_" + lPathToBlock + "_" + i, true, 0.25f);

        	// Disable cavein area for the other path
        	SetEntityActive("AreaCaveIn"+(3-lPathToBlock), false);
        }

        void TimerStopRockRumble(string asTimer)
        {
        	StopSound("CaveInRumble", 0.0f);
        	StopSound("rumblex", 2.0f);
        }

        //-------------------------------------------------------

        //////////////////////////////////////////////
        // AFTER CAVE IN
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void CollideAfterCaveIn(string asParent, string asChild, int alState)
        {
        	//Make the guardian pop up behind the player and make it alot faster
        	SetLocalVarInt("GuardianCurrentStep", 35);
        	SetLocalVarFloat("guardian_time", 1.0f);

        	SetEntityActive(asChild, false);
        }

        //-------------------------------------------------------


        //////////////////////////////////////////////
        // MISC ITEM USAGES
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        //MISC USE HAMMER ON LOCKED
        void UseHCOnLockedObject(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level15", "DoorItemNoUse", 0);
        }
        void UseHCOnLockedObject02(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level15", "AcidWithItem", 0);
        }
        void UseBarOnLockedObject(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level15", "UseGlassFilledOnExtra", 0);
        }

        //-------------------------------------------------------

        //USE GLASS CONTAINER ON ROPE/CRANK
        void UseGlassContainerOnRope(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", "UseGlassOnRope", 0);
        }

        //-------------------------------------------------------

        //LOCK DOOR AFTER GRUNT LEAVES DURING INTRO
        void CollideGruntOff(string asParent, string asChild, int alState)
        {
        	SetSwingDoorLocked("prison_1",true,true);

        	PlaySoundAtEntity("lock", "25_door_close.snt", "prison_1", 0.0f, false);
        }

        //-------------------------------------------------------

        ///////////////////////////////////////////////
        //GUARDIAN DEATH IF STAYING IN CELL
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        float gfGuardianClosingInWaitTime = 60.0f * 3.0f;

        //-------------------------------------------------------

        void CollideInCell(string asParent, string asChild, int alState)
        {
        	StopGuardianCountDown();
        	AddDebugMessage("PlayerLeftCell", false);
        }

        //-------------------------------------------------------

        void StopGuardianCountDown()
        {
        	RemoveTimer("playerlikesalexander");
        	StopSound("slimeps", 4.0f);
        	AddDebugMessage("Guardian countdown is stopped!", false);
        }

        //-------------------------------------------------------

        void TimerGuardianInCell(string asTimer)
        {
        	AddLocalVarInt("GuardianInCell", 1);
        	int lState = GetLocalVarInt("GuardianInCell");

        	AddDebugMessage("Guardian closing in: "+lState, false);

        	switch(lState)
        	{
        		case 1:
        			SetPropActiveAndFade("slime_1_*", true, 4.0f);
        			PlaySoundAtEntity("slime1s", "slime_loop.snt", "slime_1_1", 4.0f, true);
        			PlaySoundAtEntity("slimeps", "slime_loop_no3d.snt", "Player", 4.0f, true);
        			SetRadialBlurStartDist(0.1f);
        			FadeRadialBlurTo(0.1f, 0.1f);
        			PlayGuiSound("25_guardian_activated.snt", 0.6f);
        			AddTimer(nameof(TimerRadialOffCell), 4.0f, nameof(TimerRadialOffCell));
        			StartScreenShake(0.01f, 1.0f, 3.0f, 2.0f);
        			AddTimer("playerlikesalexander", gfGuardianClosingInWaitTime, nameof(TimerGuardianInCell));
        			GiveSanityDamage(25.0f, false);

        			SetEntityActive("SlimeDamageArea_1", true);

        			CreateParticleSystemAtEntityExt("SlimeFog_1", "ps_area_fog.ps", "AreaSlimeFog_1", true, 0.9f, 0.1f, 0.1f, 0.8f, true, 1,2, 10, 20);
        			CreateParticleSystemAtEntityExt("SlimeFog_1_2", "ps_area_fog.ps", "Arealook1", true, 0.9f, 0.1f, 0.1f, 0.8f, true, 1,2, 10, 20);
        		break;
        		case 2:
        			SetPropActiveAndFade("slime_2_*", true, 4.0f);
        			PlaySoundAtEntity("slime2s", "slime_loop.snt", "slime_2_1", 4.0f, true);
        			PlayGuiSound("25_guardian_activated.snt", 0.9f);
        			SetRadialBlurStartDist(0.1f);
        			FadeRadialBlurTo(0.2f, 0.1f);
        			AddTimer(nameof(TimerRadialOffCell), 4.0f, nameof(TimerRadialOffCell));
        			StartScreenShake(0.006f, 1.0f, 3.0f, 2.0f);
        			AddTimer("playerlikesalexander", gfGuardianClosingInWaitTime, nameof(TimerGuardianInCell));
        			GiveSanityDamage(25.0f, false);

        			SetEntityActive("SlimeDamageArea_2", true);

        			CreateParticleSystemAtEntityExt("SlimeFog_2", "ps_area_fog.ps", "AreaSlimeFog_2", true, 0.9f, 0.1f, 0.1f, 0.8f, true, 1,2, 10, 20);
        		break;
        		case 3:
        			SetPropActiveAndFade("slime_3_*", true, 4.0f);
        			PlaySoundAtEntity("slime3s", "slime_loop.snt", "slime_3_1", 4.0f, true);
        			PlayGuiSound("25_guardian_distant3.snt", 1.0f);
        			SetRadialBlurStartDist(0.1f);
        			FadeRadialBlurTo(0.3f, 0.1f);
        			AddTimer(nameof(TimerRadialOffCell), 6.0f, nameof(TimerRadialOffCell));
        			StartScreenShake(0.008f, 1.0f, 3.0f, 2.0f);
        			AddTimer("playerlikesalexander", gfGuardianClosingInWaitTime, nameof(TimerGuardianInCell));
        			GiveSanityDamage(25.0f, false);

        			SetEntityActive("SlimeDamageArea_3", true);

        			CreateParticleSystemAtEntityExt("SlimeFog_3", "ps_area_fog.ps", "AreaSlimeFog_3", true, 0.9f, 0.1f, 0.1f, 0.8f, true, 1,2, 10, 20);
        		break;
        		case 4:
        			SetPropActiveAndFade("slime_4_*", true, 4.0f);
        			PlaySoundAtEntity("slime4s", "slime_loop.snt", "slime_4_1", 4.0f, true);
        			PlayGuiSound("25_guardian_ontop", 1.0f);
        			StartScreenShake(0.06f, 1.0f, 3.0f, 2.0f);
        			FadeLightTo("PointLight_1", 1.0f, 0.0f, 0.0f, 1.0f, -1, 4.0f);
        			SetRadialBlurStartDist(0.1f);
        			FadeRadialBlurTo(0.1f, 0.1f);
        			AddTimer(nameof(TimerRadialOffCell), 4.0f, nameof(TimerRadialOffCell));
        			AddTimer("killplayer", 4.0f, nameof(TimerGuradianKillCell));
        			GiveSanityDamage(25.0f, false);
        		break;
        	}
        }

        //-------------------------------------------------------

        void TimerGuradianKillCell(string asTimer)
        {
        	StartAlexanderEnding();
        }

        //-------------------------------------------------------

        void TimerRadialOffCell(string asTimer)
        {
        	FadeRadialBlurTo(0.0f, 0.1f);
        }

        //-------------------------------------------------------

        ///////////////////////////////
        // ALEXANDER END
        ///////////////////////////////

        //-----------------------------------

        bool gbShowCredits = true;

        //-----------------------------------

        void StartAlexanderEnding()
        {
        	AddDebugMessage("Start Alexander Ending", false);

        	//////////////////////////
        	//Disable player action
        	SetInventoryDisabled(true);
        	SetLanternDisabled(true);
        	ChangePlayerStateToNormal(); //So a held item is dropped!
        	ShowPlayerCrossHairIcons(false);

        	//////////////////////////
        	//Add edge blur and make player slower.
        	SetRadialBlurStartDist(0.15f);
        	FadeRadialBlurTo(0.09f, 0.03f);
        	SetPlayerMoveSpeedMul(0.6f);

        	//turn off all damage areas.
        	SetEntityActive("SlimeDamageArea_*", false);

        	//Player should not be able to escape cell now:
        	SetEntityActive("BlackPlayerOnEnd", true);

        	SetLocalVarInt("AlexanderEnding",1);

        	ResumeAlexEnding();
        }

        //-----------------------------------

        void ResumeAlexEnding()
        {
        	AddTimer("AlexEnding", 0.1f, nameof(TimerAlexEnding));
        }

        //-----------------------------------

        void TimerAlexEnding(string asTimer)
        {
        	int lState = GetLocalVarInt("AlexEndState");
        	AddLocalVarInt("AlexEndState",1);
        	float fNextEventTime = 1.0f;
        	bool bPause = false;

        	////////////////////////
        	// 0: Stop look at and fade in music (+ flash fade in)
        	if(lState == 0)
        	{
        		StopPlayerLookAt();
        		PlayMusic("29_amb_end_soft.ogg", true, 0.9f, 5, 0, false);
        		PlaySoundAtEntity("slime", "25_slime_loop.snt", "Player", 3.0f, false);

        		fNextEventTime= 3;
        	}
        	////////////////////////
        	// 1: Recalled voice "Girl"
        	else if(lState == 1)
        	{
        		PlayFlashbackVoice("CH03L29_DanielRemorse_", 1,1, nameof(RemorseVoiceOver));

        		bPause	= true;
        	}
        	////////////////////////
        	// 2: Guardian roar #1
        	else if(lState == 2)
        	{
        		SetPlayerSanity(100); //To make sure it does not fall to 0 at the end.
        		AddTimer("50", 1.5f, nameof(TimerEndSanityDamage));

        		PlaySoundAtEntity("howl","guardian_distant1", "Player", 0, false);
        		//PlaySoundAtEntity("cough", "29_cough.snt", "Player", 0.0f, false);

        		//ShowPlayerSlime(0);

        		fNextEventTime= 7;
        	}
        	////////////////////////
        	// 3: Recalled voice "Captive"
        	else if(lState == 3)
        	{
        		PlayFlashbackVoice("CH03L29_DanielRemorse_", 2,2, nameof(RemorseVoiceOver));

        		bPause	= true;
        	}
        	////////////////////////
        	// 4: Guardian roar #2
        	else if(lState == 4)
        	{
        		AddTimer("20", 1.5f, nameof(TimerEndSanityDamage));
        		PlaySoundAtEntity("howl","guardian_distant2", "Player", 0, false);
        		//StopSound("cough", 0.5f);
        		//PlayGuiSound("CH03L29_Ending_Daniel_Cough.ogg", 0.6f);

        		//ShowPlayerSlime(1);

        		fNextEventTime= 5;
        	}
        	////////////////////////
        	// 5: Recalled voice "Mother"
        	else if(lState == 5)
        	{
        		PlayFlashbackVoice("CH03L29_DanielRemorse_", 3,3, nameof(RemorseVoiceOver));
        		//PlaySoundAtEntity("cough", "29_cough.snt", "Player", 0.0f, false);

        		bPause	= true;
        	}
        	////////////////////////
        	// 6: Guardian roar #3
        	else if(lState == 6)
        	{
        		//GiveSanityDamage(20, true);
        		PlaySoundAtEntity("howl","guardian_distant3", "Player", 0, false);

        		//StopSound("cough", 0.5f);

        		GivePlayerDamage(20, "BloodSplat", true, false);
        		PlayGuiSound("attack_claw_hit.snt", 0.5f);
        		PlayGuiSound("25_guardian_slime_appear.snt", 0.8f);
        		PlayGuiSound("water_lurker_eat_rev2.ogg", 0.5f);

        		PlayGuiSound("CH03L29_Ending_Daniel_Scream.ogg", 0.8f);

        		//ShowPlayerSlime(2);

        		fNextEventTime= 6.5f;
        	}
        	////////////////////////
        	// 7: Guardian final + kill player
        	else if(lState == 7)
        	{
        		PlaySoundAtEntity("howl","guardian_ontop.snt", "Player", 0, false);
        		GivePlayerDamage(20, "BloodSplat", false, false);
        		PlayGuiSound("attack_claw_hit.snt", 0.5f);
        		PlayGuiSound("25_guardian_slime_appear.snt", 0.8f);
        		PlayGuiSound("water_lurker_eat_rev2.ogg", 0.5f);

        		SetPlayerSanity(100);//So there is no panting when dead.
        		SetPlayerActive(false);
        		FadeOut(8);

        		FadePlayerRollTo(15, 10, 10);
        		MovePlayerHeadPos(0, -0.6f, 0.3f, 3, 5);
        		FadePlayerFOVMulTo(2.3f, 1.0f/8.0f);
        		FadePlayerAspectMulTo(3, 0.5f/8.0f);

        		StopSound("slime1s", 5.0f);
        		StopSound("slime2s", 5.0f);
        		StopSound("slime3s", 5.0f);
        		StopSound("slime4s", 5.0f);

        		RemoveTimer(nameof(TimerRandAmbSound));

        		StopMusic(4,0);

        		FadeGlobalSoundVolume(0.0f, 10.0f);
        		StopSound("slime", 10.0f);

        		fNextEventTime= 10;
        	}
        	////////////////////////
        	// 11: Final words by Alexander
        	else if(lState == 11)
        	{
        		PlayEffectVoice("CH03L29_Ending_Alexander_", 1,2, nameof(AlexEndWordsVoiceOver));
        		bPause	= true;
        	}
        	////////////////////////
        	// 12: Credits!
        	else if(lState == 12)
        	{

        		UnlockAchievement("Quitter");
        		if(gbShowCredits)
        			StartCredits("ending_alexander.ogg",false,  "Ending", "MainCredits", 0);
        		return;
        	}


        	AddDebugMessage("Event:"+lState,false);//+" Time:"+fNextEventTime+" Pause"+bPause?1:0, false);

        	if(bPause==false)
        		AddTimer("AlexEnding", fNextEventTime, nameof(TimerAlexEnding));
        }

        //-----------------------------------

        void TimerEndSanityDamage(string asTimer)
        {
        	int iDamage;

        	if(asTimer == "50")
        		iDamage = 50;
        	else
        		iDamage = 20;

        	GiveSanityDamage(iDamage, true);
        }

        //-----------------------------------

        void RemorseVoiceOver()
        {
        	ResumeAlexEnding();
        }

        void AlexEndWordsVoiceOver()
        {
        	ResumeAlexEnding();
        }


        //-----------------------------------


        ///////////////////////////////////////////////
        // RANDOM AMBIENT SOUNDS AT START
        ///////////////////////////////////////////////

        //-------------------------------------------------------

        void TimerRandAmbSound(string asTimer)
        {
        	int iRand = RandInt(1, 5);
        	string[] aSound = {"25_guardian_idle.snt", "15_man01_whimp.snt", "scare_male_terrified.snt"};

        	PlaySoundAtEntity(asTimer+iRand, aSound[RandInt(0,2)], asTimer+iRand, RandFloat(0.0f, 1.0f), false);

        	AddTimer(asTimer, RandFloat(15.0f, 30.0f), nameof(TimerRandAmbSound));

        	AddDebugMessage("RandSoundPlayer", false);
        }

        //-------------------------------------------------------


        //A LITTLE CELL EVENT
        void CollideAreaCellEvent(string asParent, string asChild, int alState)
        {
        	//if(GetPlayerSanity() > 70) return;

        	PlayMusic("11_event_tree.ogg", false, 1.0f, 0.1f, 10,false);

        	PlayGuiSound("scare_wall_stomp.snt", 1.0f);
        	PlayGuiSound("sanity_flick.snt", 0.4f);
        	PlaySoundAtEntity("brum", "00_creak.snt", "Player", 0.5f, false);

        	AddTimer("react_scare", 0.5f, nameof(TimerScareReaction02));
        	AddTimer("react_breath", 2.0f, nameof(TimerScareReaction02));

        	StartEffectFlash(0.4f, 0.2f, 0.2f);

        	AddTimer("cell1", 0.4f, nameof(TimerCellEvent));
        	AddTimer("cell2", 1.0f, nameof(TimerCellEvent));
        	AddTimer("cell3", 1.5f, nameof(TimerCellEvent));
        	AddTimer("cell4", 3.5f, nameof(TimerCellEvent));
        	AddTimer("cell4.1", 4.0f, nameof(TimerCellEvent));
        	AddTimer("cell5", 6.0f, nameof(TimerCellEvent));
        }
        void TimerCellEvent(string asTimer)
        {
        	if(asTimer == "cell1"){
        		FadeOut(0.4f);
        		PlayGuiSound("sanity_flick.snt", 0.7f);
        		StartScreenShake(0.3f, 0.0f, 0.5f, 0.0f);
        	}

        	if(asTimer == "cell2"){
        		SetEntityActive("corpse_male_1", true);
        		SetRadialBlurStartDist(0.3f);
        		FadeRadialBlurTo(0.1f, 1);
        		PlayGuiSound("15_the_big_scream3.ogg", 0.7f);
        		PlayGuiSound("amb_hunt.snt", 1.0f);
        	}
        	if(asTimer == "cell3"){
        		StartPlayerLookAt("corpse_male_1", 10.0f, 10.0f, "");
        		PlayGuiSound("15_ripp_door.snt", 1.0f);
        		PlayGuiSound("attack_launch03.ogg", 1.0f);
        	}
        	if(asTimer == "cell4"){
        		PlayGuiSound("15_the_big_scream2.ogg", 0.7f);
        		PlayGuiSound("notice_long.snt", 1.0f);
        		AddTimer("react_scare", 0.5f, nameof(TimerScareReaction02));
        		AddTimer("react_breath", 2.0f, nameof(TimerScareReaction02));
        	}
        	if(asTimer == "cell4.1"){
        		PlayGuiSound("attack_claw_hit.snt", 0.5f);
        		PlayGuiSound("15_bang_fall.snt", 1);
        	}
        	if(asTimer == "cell5"){
        		StopPlayerLookAt();
        		FadeIn(5.0f);
        		StopSound("brum", 5.0f);
        		FadeRadialBlurTo(0.0f, 0.01f);
        	}
        }
        void TimerScareReaction02(string asTimer)
        {
        	PlayGuiSound(asTimer, 0.7f);
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("Cells");

        	ClearSavedMaps();

        	////////////////////////////////////
        	// INSANITY SETUP
        	SetInsanitySetEnabled("Ch03", true);
        	SetInsanitySetEnabled("Ch02", false);

        	////////////////////
        	// Hub start sanity boost
        	GiveSanityBoost();

        	////////////////////////////////////
        	// COLLIDE CALLBACKS
        	// Wriggle bar
        	for(int i=1;i<=4;++i)
        		AddEntityCollideCallback("wriggle_prison_bar_1", "AreaWriggleBarBreak_" + i, nameof(CollideWriggleBarBreak), false, 1);

        	for(int i=5;i<=8;++i)
        		AddEntityCollideCallback("wriggle_prison_bar_2", "AreaWriggleBarBreak_" + i, nameof(CollideWriggleBarBreak2), false, 1);


        	AddEntityCollideCallback("wriggle_prison_bar_bend_stone_1", "AreaBarBendDone", nameof(CollideBarBendDone), true, 1);

        	for(int i=1; i<=2; ++i)
        		AddEntityCollideCallback("cell_breakable_wall_support_1", "AreaCollapseWall_"+i, nameof(CollideCollapseWall), true, 1);

        	// Rope
        	AddEntityCollideCallback("invisible_box_mass_2_1", "AreaRopeInteraction",nameof(CollideRopeInteraction), false, 0);
        	AddEntityCollideCallback("invisible_box_mass_2_1", "AreaRopeDown",nameof(CollideRopeDown), false, 0);

        	//Stuff that fall into well
        	AddEntityCollideCallback("cell_breakable_wall_rock*", "AreaRopeDown", nameof(CollideObjectInWellWater), true,1);
        	AddEntityCollideCallback("cell_bucket_*", "AreaRopeDown", nameof(CollideObjectInWellWater), true,1);
        	AddEntityCollideCallback("chair_wood02_*", "AreaRopeDown", nameof(CollideObjectInWellWater), true,1);
        	AddEntityCollideCallback("wooden_bucket_filled_seq", "AreaRopeDown", nameof(CollideObjectInWellWater), true,1);
        	AddEntityCollideCallback("wine02_1", "AreaRopeDown", nameof(CollideObjectInWellWater), true,1);
        	AddEntityCollideCallback("wine01_1", "AreaRopeDown", nameof(CollideObjectInWellWater), true,1);

        	//Additions for escaping the cell
        	AddEntityCollideCallback("Player","AreaEscaped", nameof(CollideEscapeCell), true, 1);
        	AddEntityCollideCallback("Player","AreaInCell", nameof(CollideInCell), true, -1);


        	//Using bar to get key
        	AddEntityCollideCallback("wriggle_prison_bar_key_pusher_1","AreaPushKey", nameof(CollidePushKey), true, 1);


        	//Grunt leaves room
        	AddEntityCollideCallback("servant_grunt_1","AreaGruntOff", nameof(CollideGruntOff), true, 1);


        	// Start guardian
        	for(int i=1;i<=2;++i)
        		AddEntityCollideCallback("Player","AreaStartGuardian_"+i, nameof(CollideStartGuardian), false, 1);

        	// Guardian damage (there are 53 steps for the Guardian so far)
        	for(int i=1;i<=glGuardianSteps; ++i)
        	{
        		string sDmgAreaName = "AreaGuardianKill_lump_" + i;
        		if(GetEntityExists(sDmgAreaName)==false)
        		{
        			// These are for the possible cavein area, so they are split.
        			for(int j=1;j<=2;++j)
        				AddEntityCollideCallback("Player", sDmgAreaName + "_"+j, nameof(CollideAreaGuardianKill), false, 1);
        		}
        		else
        			AddEntityCollideCallback("Player", sDmgAreaName, nameof(CollideAreaGuardianKill), false, 1);
        	}

        	// Guardian CheckPoint
        	AddEntityCollideCallback("Player", "AreaGuardianDoor2Checkpoint", "CollideAreaGuardianDoor2Checkpoint", true, 1);

        	// Cave In
        	AddEntityCollideCallback("Player", "AreaCaveIn1", nameof(CollideAreaCaveIn), true, 1);
        	AddEntityCollideCallback("Player", "AreaCaveIn2", nameof(CollideAreaCaveIn), true, 1);

        	AddEntityCollideCallback("Player", "AreaAfterCaveIn", nameof(CollideAfterCaveIn), false, 1);

        	AddEntityCollideCallback("Player", "AreaBeginRumble", nameof(CollideAreaBeginRumble), false, 1);

        	// Guardian Tease (after death)
        	for(int i=1; i<=3; ++i)
        		AddEntityCollideCallback("Player", "AreaGuardianTease_"+i, "CollideGuardianTease"+i, true, 1);
        	AddEntityCollideCallback("Player", "AreaWaterLurkerTease", nameof(CollideWaterLurkerTease), true, 1);
        	AddEntityCollideCallback("Player", "AreaCreepMusicTease", nameof(CollideCreepMusicTease), true, 1);
        	AddEntityCollideCallback("Player", "AreaLowRumbleTease", nameof(CollideLowRumbleTease), true, 1);
        	AddEntityCollideCallback("Player", "AreaRoarTease", nameof(CollideRoarTease), true, 1);
        	AddEntityCollideCallback("Player", "AreaGuardRumbleTease", nameof(CollideGuardRumbleTease), true, 1);
        	AddEntityCollideCallback("Player", "AreaScreamTease", nameof(CollideScreamTease), true, 1);

        	//Cell event
        	AddEntityCollideCallback("Player", "AreaCellEvent", nameof(CollideAreaCellEvent), true, 1);

        	///////////////////////////
        	// UseItem
        	AddUseItemCallback("BucketOnRope", "wooden_bucket_1", "AreaRopeInteraction", nameof(UseBucketOnRope), false);
        	AddUseItemCallback("BucketOnRope", "wooden_bucket_1", "crank_wheel_1", nameof(UseBucketOnRope), false);


        	AddUseItemCallback("BucketOnPipe", "wooden_bucket_1", "AreaBucketPipe", nameof(UseBucketOnPipe), true);
        	AddUseItemCallback("BucketOnPipe", "wooden_bucket_1", "cell_room_broken_pipe_1", nameof(UseBucketOnPipe), true);
        	AddUseItemCallback("BucketOnPipe", "wooden_bucket_1", "AreaBucketPipe_2", nameof(UseBucketOnPipe), true);

        	AddUseItemCallback("BarOnPipe", "wriggle_prison_bar_item_1", "AreaBucketPipe_2", nameof(UseBarOnPipe), true);
        	AddUseItemCallback("BarOnPipe", "wriggle_prison_bar_item_2", "AreaBucketPipe_2", nameof(UseBarOnPipe), true);
        	AddUseItemCallback("BarOnPipe", "wriggle_prison_bar_obj_dyn_1", "AreaBucketPipe_2", nameof(UseBarOnPipe), true);
        	AddUseItemCallback("BarOnPipe", "wriggle_prison_bar_item_1", "cell_room_broken_pipe_1", nameof(UseBarOnPipe), true);
        	AddUseItemCallback("BarOnPipe", "wriggle_prison_bar_item_2", "cell_room_broken_pipe_1", nameof(UseBarOnPipe), true);
        	AddUseItemCallback("BarOnPipe", "wriggle_prison_bar_obj_dyn_1", "cell_room_broken_pipe_1", nameof(UseBarOnPipe), true);

        	AddUseItemCallback("BarOnDoor", "wriggle_prison_bar_item_1", "prison_1", nameof(UseBarOnLockedObject), true);
        	AddUseItemCallback("BarOnDoor", "wriggle_prison_bar_item_2", "prison_1", nameof(UseBarOnLockedObject), true);
        	AddUseItemCallback("BarOnDoor", "wriggle_prison_bar_obj_dyn_1", "prison_1", nameof(UseBarOnLockedObject), true);

        	AddUseItemCallback("BucketOnPipe", "wooden_bucket_filled_1", "AreaBucketPipe", nameof(UseBucketOnPipe), true);
        	AddUseItemCallback("BucketOnPipe", "wooden_bucket_filled_1", "cell_room_broken_pipe_1", nameof(UseBucketOnPipe), true);
        	AddUseItemCallback("BucketOnPipe", "wooden_bucket_filled_1", "AreaBucketPipe_2", nameof(UseBucketOnPipe), true);

        	AddUseItemCallback("BucketOnRope", "wooden_bucket_filled_1", "AreaRopeInteraction", nameof(UseFilledBucketOnRope), false);
        	AddUseItemCallback("BucketOnRope", "wooden_bucket_filled_1", "crank_wheel_1", nameof(UseFilledBucketOnRope), false);


        	AddUseItemCallback("KeyOnDoor", "key_tomb_rusty_1", "prison_1", nameof(UseKeyOnDoor), false);
        	AddUseItemCallback("KeyOnCellDoor", "key_tomb_rusty_1", "prison_section_plain_1", nameof(UseKeyOnCellDoor), false);

        	AddUseItemCallback("BarOnBricks","wriggle_prison_bar_item_1", "AreaLooseBricks", nameof(UseBarOnBricks), true);
        	AddUseItemCallback("BarOnWall","wriggle_prison_bar_item_1", "cell_breakable_wall_1", nameof(UseBarOnWall), true);
        	AddUseItemCallback("Bar2OnBricks","wriggle_prison_bar_item_2", "AreaLooseBricks", nameof(UseBarOnBricks), true);
        	AddUseItemCallback("Bar2OnWall","wriggle_prison_bar_item_2", "cell_breakable_wall_1", nameof(UseBarOnWall), true);
        	AddUseItemCallback("HCOnBricks", "stone_hammer_chipper", "AreaLooseBricks", nameof(UseHCOnBricks), true);
        	AddUseItemCallback("HCOnBricks", "stone_hammer_chipper", "cell_breakable_wall_1", nameof(UseBarOnWall), true);

        	AddUseItemCallback("HCLock", "stone_hammer_chipper", "prison_section_plain_1", nameof(UseHCOnLockedObject), true);
        	AddUseItemCallback("HCLock", "stone_hammer_chipper", "padlock_rusty_1", nameof(UseHCOnLockedObject), true);
        	AddUseItemCallback("HCLock", "stone_hammer_chipper", "prison_1", nameof(UseHCOnLockedObject), true);
        	AddUseItemCallback("HCLock", "stone_hammer_chipper", "wriggle_prison_bar_1", nameof(UseHCOnLockedObject), true);
        	AddUseItemCallback("HCLock", "stone_hammer_chipper", "wriggle_prison_bar_2", nameof(UseHCOnLockedObject), true);
        	AddUseItemCallback("HCLock", "stone_hammer_chipper", "AreaBucketPipe_2", nameof(UseHCOnLockedObject02), true);
        	AddUseItemCallback("HCLock", "stone_hammer_chipper", "cell_room_broken_pipe_1", nameof(UseHCOnLockedObject02), true);

        	AddUseItemCallback("GlassContainerOnRope", "glass_container_1", "AreaRopeInteraction", nameof(UseGlassContainerOnRope), false);
        	AddUseItemCallback("GlassContainerOnRope", "glass_container_1", "crank_wheel_1", nameof(UseGlassContainerOnRope), false);

        	///////////////////////////
        	// Connections
        	InteractConnectPropWithRope("WellRope","crank_wheel_1", "RopeArea_1", false, 5,8,8, false, 0);

        	////////////////////////////////////
        	// Guardian wait time debug
        	if(ScriptDebugOn())
        	{
        		//gfGuardianClosingInWaitTime = 60.0f * 0.5f;
        	}


        	///////////////////////////
        	// Time Limit
        	AddTimer("playerlikesalexander", gfGuardianClosingInWaitTime, nameof(TimerGuardianInCell));

        	// Start!
        	if(ScriptDebugOn()==false)
        	{
        		FadeOut(0.0f);

        		SetPlayerActive(false);
        		SetPlayerCrouchDisabled(true);
        		SetPlayerJumpDisabled(true);
        		MovePlayerHeadPos(0, -0.9f, 0, 10.0f, 0.1f);
        		SetPlayerMoveSpeedMul(0.0f);
        		SetPlayerRunSpeedMul(0.0f);
        		SetPlayerLookSpeedMul(0.0f);

        		AddTimer(nameof(TimerBlackOut), 0, nameof(TimerBlackOut));

        		AddTimer("AreaRandSound_", RandFloat(5.0f, 15.0f), nameof(TimerRandAmbSound));
        		SetPlayerHealth(30.0f);
        	}

        	////////////////////////////////////
        	// DEBUG
        	if(ScriptDebugOn())
        	{
        		/* AddTimer("TimerBlackOut", 0, "TimerBlackOut");
        		FadeOut(0.0f);
        		SetPlayerActive(false);
        		SetPlayerCrouchDisabled(true);
        		SetPlayerJumpDisabled(true);
        		MovePlayerHeadPos(0, -0.9f, 0, 10.0f, 0.1f);
        		SetPlayerMoveSpeedMul(0.0f);
        		SetPlayerRunSpeedMul(0.0f);
        		SetPlayerLookSpeedMul(0.0f);
        		AddTimer("playerlikesalexander", 600, "TimerGuardianInCell");*/

        		AddTimer("AreaRandSound_", RandFloat(5.0f, 15.0f), nameof(TimerRandAmbSound));

        		SetPlayerHealth(30.0f);

        		SetSwingDoorLocked("prison_1", true, true);

        		GiveItemFromFile("lantern", "lantern.ent");

        		GiveItemFromFile("wooden_bucket_filled_1", "wooden_bucket_filled.ent");
        		GiveItemFromFile("wooden_bucket_1", "wooden_bucket.ent");
        		//GiveItemFromFile("wriggle_prison_bar_item_1", "wriggle_prison_bar_item.ent");

        		GiveItemFromFile("glass_container_1", "glass_container.ent");
        		GiveItem("stone_hammer_chipper", "Puzzle", "stone_hammer_chipper", "stone_hammer_chipper.tga", 0);
        	}

        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PlayMusic("25_amb.ogg", true, 0.8f, 10.0f, 0, true);

        	PreloadParticleSystem("ps_bucket_pour_water"); PreloadParticleSystem("ps_bucket_flush_water"); PreloadParticleSystem("ps_bucket_impact_water");
        	PreloadParticleSystem("waterlurker_walk_splash"); PreloadParticleSystem("ps_guardian_appear_explosion"); PreloadParticleSystem("waterlurker_run_splash");
        	PreloadParticleSystem("ps_guardian_danger_fog_loop"); PreloadParticleSystem("ps_area_fog_large");

        	PreloadSound("15_man01_whimp"); PreloadSound("scare_male_terrified");
        	PreloadSound("react_pant"); PreloadSound("react_sigh"); PreloadSound("player_climb"); PreloadSound("15_rock_break");
        	PreloadSound("impact_rock_high"); PreloadSound("15_hammer"); PreloadSound("25_strain_wood"); PreloadSound("25_guardian_distant1");
        	PreloadSound("general_rock_rumble_no3d"); PreloadSound("impact_water_med"); PreloadSound("impact_water_high"); PreloadSound("waterlurker_run_splash");
        	PreloadSound("25_water_stream"); PreloadSound("16_water_stream_heavy"); PreloadSound("12_epoxy_flow"); PreloadSound("25_drop_key");
        	PreloadSound("25_attach_bar_metal"); PreloadSound("scrape_metal"); PreloadSound("25_guardian_idle"); PreloadSound("25_guardian_distant3");
        	PreloadSound("water_lurker_hunt"); PreloadSound("scare_slam_door"); PreloadSound("25_guardian_slime_appear"); PreloadSound("25_guardian_ontop");
        	PreloadSound("25_guardian_activated"); PreloadSound("slime_loop_no3d"); PreloadSound("water_lurker_eat_rev2"); PreloadSound("25_break_rock");

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	FadeRadialBlurTo(0, 0.02f);
        	StopMusic(1, 0);

        	StopSound("SlimeLoopOnPlayer", 2.0f);
        	StopSound("rumpustherumble", 2.0f);
        	StopSound("rumble", 2.0f);
        	StopSound("CaveInRumble", 2.0f);	//just to be sure
        	StopSound("rumblex", 2.0f);	//just to be sure
        	StopSound("end_amb", 2.0f);

        	SetupLoadScreen("LoadingText", "Ch03_Captured02", 1, "game_loading_well.jpg");
        }
    }
}
