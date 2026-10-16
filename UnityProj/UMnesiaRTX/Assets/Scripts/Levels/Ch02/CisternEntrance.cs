using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/16_cistern_entrance.hps; journal: docs/ScriptPort/ch02/16_cistern_entrance.md.</summary>
    [HplMapScript("maps/main/ch02/16_cistern_entrance.map")]
    public class CisternEntrance : HplScript
    {
        ////////////////////////////
        // FLOODED CORRIDOR
        ////////////////////////////

        void CollideDeepWater(string asParent, string asChild, int alState)
        {
        	SetMessage("Ch02Level16", "WaterDeep", 0);
        	AddQuest("SewerFlooded", "16SewerFlooded");
        }


        ////////////////////////////
        // SEWER POSION
        ////////////////////////////

        //----------------------------

        void InteractSewerDoorHasAntidote(string asDoor)
        {
        	SetupLoadScreen("LoadingText", "Ch02_Sewer01", 1, "game_loading_well.jpg");
        }

        //----------------------------

        ////////////////////////////
        // SEWER POSION
        ////////////////////////////

        //----------------------------

        void InteractSewerDoor(string asDoor)
        {
        	if(QuestIsAdded("FindAntidote")==false)
        	{
        		AddTimer("SewerInteract",5.5f, nameof(TimerSewerDoorInteraction));
        	}
        	SetMessage("Ch02Level16", "SewerDoorHasPoison", 0);
        }


        void TimerSewerDoorInteraction(string asTimer)
        {
        	AddQuest("FindAntidote", "16FindAntidote");
        }

        void CollidePoison_Start(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("InPosion")==1) return;
        	SetLocalVarInt("InPosion", 1);

        	FadeImageTrailTo(0.7f, 1);
        	AddTimer("PoisonCough",0.5f, nameof(TimerPoisonCough));
        }

        void CollidePoison_Stop(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("InPosion")==0) return;
        	SetLocalVarInt("InPosion", 0);

        	StopPoisonEffects();
        }

        void StopPoisonEffects()
        {
        	FadePlayerFOVMulTo( 	1, 1);
        	FadePlayerAspectMulTo(	1, 1);
        	FadeImageTrailTo(0, 1);
        	FadeRadialBlurTo(0.0f, 0.5f);

        	RemoveTimer("PoisonCough");
        }

        //----------------------------

        void TimerPoisonCough(string asTimer)
        {
        	PlaySoundAtEntity("cough","player_cough", "Player", 0, false);
        	GivePlayerDamage(0.5f, "BloodSplash", false, false);

        	if(GetLocalVarInt("PosionOn") == 0){
        		FadeRadialBlurTo(0.3f, 2.0f);
        		SetRadialBlurStartDist(0.6f);
        		SetLocalVarInt("PosionOn", 1);
        	}else{
        		FadeRadialBlurTo(0.2f, 2.0f);
        		SetRadialBlurStartDist(0.8f);
        		SetLocalVarInt("PosionOn", 0);
        	}

        	AddTimer("PoisonCough",RandFloat(1.5f, 3.5f), nameof(TimerPoisonCough));
        }

        //----------------------------

        ////////////////////////////
        // MOVE LADDER
        ////////////////////////////

        void InteractRustyLever(string asEntity)
        {
        	SetMessage("Ch02Level16", nameof(InteractRustyLever), 0);

        	if(GetLocalVarInt("FilledContainer")==0)
        		SetEntityPlayerInteractCallback("AreaOil", nameof(InteractAreaOil), false);

        	PlaySoundAtEntity("stuck", "16_lever_stuck", asEntity, 0.0f, false);
        }

        void LeverStateChange(string asEntity, int alState)
        {
        	if(alState == 0) return;
        	if(GetLocalVarInt("LadderState") == alState) return;
        	if(GetLocalVarInt("LeverGreased") == 0) return;

        	SetLocalVarInt("LadderState", 0);

        	AddDebugMessage("Lever State: "+alState, true);

        	if(alState == -1 && GetLocalVarInt("LadderFix") == 0)
        		return;
        	else
        		SetLeverStuckState("lever_simple01_1", alState, true);

        	SetEntityActive("LadderArea_1", false); //Ladder is always disabled when moving ladder!

        	if(alState == 1)
        	{
        		SetMoveObjectState("ladder_static_1", 1);

        		SetLocalVarInt("LadderFix", 1);

        		PlaySoundAtEntity("moveladderdown","16_ladder_down","ladder_static_1", 0, false);
        	}
        	else
        	{
        		SetMoveObjectState("ladder_static_1", 0);

        		PlaySoundAtEntity("moveladderup","16_ladder_down","ladder_static_1", 0, false);
        	}
        }

        void CollideLadderPipe(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("LadderCollided")==1) return;
        	SetLocalVarInt("LadderCollided",1); //So there is no collision when making a pipe active.

        	////////////
        	// 01
        	if(asParent == "cistern_breakable_pipe01")
        	{
        		SetEntityActive("cistern_breakable_pipe02", true);

        		//SetEntityActive("AreaLadderPipe", true);
        		SetEntityActive("AreaInteractPipe", true);

        		SetLeverStuckState("lever_simple01_1", 0, true);
        		SetLocalVarInt("LadderState", 1);

        		StopSound("moveladderdown", 0.2f);

        		StopPropMovement(asChild);
        	}
        	////////////
        	// 02
        	else if(asParent == "cistern_breakable_pipe02")
        	{
        		SetEntityActive("cistern_breakable_pipe03", true);
        		SetLeverStuckState("lever_simple01_1", 0, true);
        		SetLocalVarInt("LadderState", 1);

        		StopSound("moveladder", 0.3f);

        		StopPropMovement(asChild);
        	}
        	////////////
        	// 03
        	else if(asParent == "cistern_breakable_pipe03")
        	{
        		SetEntityActive("cistern_breakable_pipe04", true);

        		//SetEntityActive("AreaLadderPipe", false);
        		SetEntityActive("AreaInteractPipe", false);
        	}

        	StartScreenShake(0.003f, 0.2f, 0.1f, 0.3f);

        	CreateParticleSystemAtEntity("collide","ps_break_wood_small","AreaLadderCollideEffect", false);

        	SetEntityActive(asParent, false);
        	AddLocalVarInt("PipeHitCount",1);

        	PlaySoundAtEntity("collide","16_ladder_imp"+GetLocalVarInt("PipeHitCount"),"AreaLadderCollideEffect", 0, false);
        }

        //----------------------------

        void CollideLadderDown(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("LeverGreased") == 0) return;

        	SetLeverStuckState("lever_simple01_1", 0, true);
        	SetLocalVarInt("LadderState", 1);

        	SetEntityActive("LadderArea_1", true);

        	if(GetLocalVarInt("LadderPuzzleCompleted")==0)
        	{
        		SetLocalVarInt("LadderPuzzleCompleted", 1);
        		GiveSanityBoostSmall();
        		PlayMusic("02_puzzle.ogg", false, 0.7f, 0, 10, false);
        	}
        }

        void CollideLadderUp(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("LeverGreased") == 0) return;

        	SetLeverStuckState("lever_simple01_1", 0, true);
        	SetLocalVarInt("LadderState", -1);
        	SetLocalVarInt("LadderCollided",0);
        }

        //----------------------------

        void InteractPipe(string asEntity)
        {
        	SetMessage("Ch02Level16", "LadderPipeHelp", -1);
        }


        //----------------------------

        ////////////////////////////
        // OIL
        ////////////////////////////

        //----------------------------

        void InteractAreaOil(string asEntity)
        {
        	SetMessage("Ch02Level16", "InteractOil", 0);

        	PlayGuiSound("impact_organic_med", 0.4f);
        }

        void InteractAreaOil02(string asEntity)
        {
        	SetMessage("Ch02Level16", "InteractOil02", 0);
        }

        //----------------------------

        void UseOilOnOil(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level16", nameof(UseOilOnOil), 0);
        }

        void UseGlassOnOil(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level16", nameof(UseOilOnOil), 0);
        }

        //----------------------------

        void UseContainerOnOil(string asItem, string asEntity)
        {
        	RemoveItem(asItem);
        	AddTimer("AddContainer", 1.1f, nameof(TimerAddContainer));
        	AddTimer("StartCrouch", 0.8f, nameof(TimerCrouch));
        	AddTimer("StopCrouch", 1.4f, nameof(TimerCrouch));
        	AddTimer("AddContainer", 1.6f, nameof(TimerStopLookAt));

        	SetLocalVarInt("FilledContainer", 1);

        	SetEntityPlayerInteractCallback("AreaOil", "", false);

        	StartPlayerLookAt("AreaLookAtContainer", 3, 3, "");
        }

        void TimerAddContainer(string asTimer)
        {
        	DestroyParticleSystem("ParticleSystem_42");
        	SetEntityActive("glass_container_oil_1", true);

        	CreateParticleSystemAtEntity("fill", "ps_oil_jar", "glass_container_oil_1", true);

        	PlaySoundAtEntity("slurp", "16_fill_oil", "glass_container_oil_1", 0.0f, false);
        }

        void TimerCrouch(string asTimer)
        {
        	if(asTimer == "StartCrouch"){
        		SetPlayerCrouching(true);
        		PlaySoundAtEntity("crouch", "player_crouch", "Player", 0.0f, false);
        		PlaySoundAtEntity("slurp2", "16_water_stream", "glass_container_oil_1", 0.3f, false);
        		return;
        	}

        	SetPlayerCrouching(false);
        	PlaySoundAtEntity("stand", "player_stand", "Player", 0.0f, false);
        }

        void TimerStopLookAt(string asTimer)
        {
        	StopPlayerLookAt();
        	StopSound("slurp2", 4.0f);
        }

        void EntityCallGlassContainer(string asEntity, string type)
        {
        	DestroyParticleSystem("fill");
        	CreateParticleSystemAtEntity("oilsplah", "ps_oil_splash.ps", "AreaLookAtContainer", true);

        	SetEntityPlayerInteractCallback("AreaOil", nameof(InteractAreaOil02), false);

        	AddUseItemCallback("OilOil", "glass_container_oil_1", "AreaOil", nameof(UseOilOnOil), false);
        	AddUseItemCallback("GlassOil", "glass_container_1", "AreaOil", nameof(UseGlassOnOil), false);
        }

        //----------------------------

        void UseOilOnLever(string asItem, string asEntity)
        {
        	RemoveItem(asItem);
        	GiveItemFromFile("glass_container_1", "glass_container.ent");

        	SetEntityPlayerInteractCallback("lever_simple01_1", "", false);
        	SetLeverStuckState("lever_simple01_1", 0, true);

        	SetLocalVarInt("LeverGreased", 1);

        	CreateParticleSystemAtEntity("oilgrease", "ps_oil_grease.ps", asEntity, false);
        	PlaySoundAtEntity("greas", "16_grease_lever", asEntity, 0.0f, false);
        }

        //----------------------------

        ////////////////////////////
        // BRDIGES
        ////////////////////////////

        //----------------------------

        void InteractBridgeLever(string asEntity, int alState)
        {
        	if(alState == 0) return;

        	string sBridge = asEntity=="bridge_lever_1" ? "bridge_metal_vert_1" : "bridge_metal_vert_2";

        	if(alState == -1)
        	{
        		if(GetLocalVarInt(asEntity) == -1) return; //Fix behaviour error if pulling lever double direction.

        		SetMoveObjectState(sBridge, 2.66f);

        		PlaySoundAtEntity("soundclank_"+sBridge, "14_elevator_clank", "Player", 0.5f, false);
        		StartScreenShake(0.005f, 0.5f, 0.2f, 0.6f);

        		StopSound("sound_"+sBridge, 0.0f);
        		PlaySoundAtEntity("sound_"+sBridge, "16_winch_bridge", "mount_"+sBridge, 1.0f, true);
        	}
        	else
        	{
        		if(GetLocalVarInt(asEntity) == 1) return; //Fix behaviour error if pulling lever double direction.

        		RemoveTimer(sBridge);
        		AddTimer(sBridge, 5.5f, nameof(TimerMoveBridgeUp));
        	}

        	SetLeverStuckState(asEntity, alState, true);
        	AddDebugMessage(asEntity + " State: "+alState, true);
        }

        void TimerMoveBridgeUp(string asTimer)
        {
        	StopSound("sound_"+asTimer, 0.0f);
        	PlaySoundAtEntity("sound_"+asTimer, "16_winch_bridge", "mount_"+asTimer, 1.0f, true);

        	PlaySoundAtEntity("soundclank_"+asTimer, "14_elevator_clank", "Player", 0.5f, false);
        	StartScreenShake(0.005f, 0.5f, 0.2f, 0.6f);

        	SetMoveObjectState(asTimer, 0);
        }

        void CollideBridgeSoundOff(string asParent, string asChild, int alState)
        {
        	AddLocalVarInt("LoadFix", 1);
        	if(GetLocalVarInt("LoadFix") <= 2) return;

        	StopSound("sound_"+asChild, 1.0f);

        	PlaySoundAtEntity("bridgestop", "elevator_stop", "Player", 1.0f, false);
        	AddTimer("bstopsound", 0.5f, nameof(TimerStopBSound));

        	string sLever = asChild=="bridge_metal_vert_1" ? "bridge_lever_1" : "bridge_lever_2";

        	//asEntity in InteractBridgeLever
        	if(asParent == "BridgeSoundOff1") SetLocalVarInt(sLever, -1);
        	else SetLocalVarInt(sLever, 1);

        	SetLeverStuckState(sLever, 0, true);
        }

        void TimerStopBSound(string asTimer)
        {
        	StopSound("bridgestop", 1.0f);
        	StartScreenShake(0.005f, 0.5f, 0.1f, 0.5f);
        }

        //----------------------------

        void CollideBridgeStuck(string asParent, string asChild, int alState)
        {
        	StopPropMovement(asChild);

        	SetLeverInteractionDisablesStuck("bridge_lever_1", false);
        	SetLeverStuckState("bridge_lever_1", -1, true);
        	SetEntityPlayerInteractCallback("bridge_lever_1", nameof(InteractBridgeLever01Stuck), true);

        	PlaySoundAtEntity("stucksound", "16_bridge_stuck", "bridge_metal_vert_1", 0.0f, false);
        	StartScreenShake(0.008f, 0.5f, 0.2f, 0.6f);

        	SetLocalVarInt("BridgeStuck",1);
        	SetEntityActive("AreaThrowRock", true);

        	StopSound("sound_"+asChild, 1.0f);
        }

        void InteractBridgeLever01Stuck(string asEntity)
        {
        	SetMessage("Ch02Level16", "InteractStuckBridgeLever", 0);

        	PlaySoundAtEntity("badunk", "14_elevator_clank", "Player", 0.0f, false);

        	AddTimer("leverstuck", 2.0f, nameof(TimerLeverStuckBackOn));
        }

        void TimerLeverStuckBackOn(string asTimer)
        {
        	SetEntityPlayerInteractCallback("bridge_lever_1", nameof(InteractBridgeLever01Stuck), true);
        }

        //----------------------------

        void CollidePlayerJumpBridge(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("BridgeStuck")==0) return;

        	MoveStuckBridge();
        }

        void CollideRockOnChain(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("BridgeStuck")==0) return;

        	PlaySoundAtEntity("HitChain","impact_metal_chain_high.snt", "AreaThrowRock", 0, false);

        	MoveStuckBridge();
        }

        void MoveStuckBridge()
        {
        	SetEntityActive("AreaThrowRock", false);
        	SetEntityActive("AreaPlayerJumpBridge", false);

        	PlaySoundAtEntity("unstucksound", "16_bridge_unstuck", "bridge_metal_vert_1", 0.0f, false);
        	PlaySoundAtEntity("fall", "16_winch_bridge", "bridge_metal_vert_1", 1.0f, true);

        	AddTimer("hitground", 1.0f, nameof(TimerBridgeHitGround));

        	StartScreenShake(0.05f, 0.5f, 0.1f, 0.3f);

        	SetEntityPlayerInteractCallback("bridge_lever_1", "", false);
        	SetMoveObjectStateExt("bridge_metal_vert_1", 2.66f, 8, 8, 0.5f, false);

        	GiveSanityBoostSmall();
        	PlayMusic("16_puzzle_bridge.ogg", false, 0.7f, 0, 10, false);
        }

        void TimerBridgeHitGround(string asTimer)
        {
        	StopSound("fall", 0.3f);
        	PlaySoundAtEntity("stucksound", "18_bridge_hit", "bridge_metal_vert_1", 0.0f, false);
        	StartScreenShake(0.01f, 1.0f, 0.1f, 0.5f);
        }

        //----------------------------

        void InteractBridgeLever01NotWorking(string asEntity)
        {
        	AddTimer("BridgeNotWorkInteract", 0.7f, nameof(TimerInteractBridgeLever01NotWorking));
        }

        void TimerInteractBridgeLever01NotWorking(string asTimer)
        {
        	SetEntityPlayerInteractCallback("bridge_lever_1", "", false);
        	AddQuest("CisternEntrBridgeNonWork", "16BridgeNonWork");
        }

        //----------------------------

        ////////////////////////////
        // MISC
        ////////////////////////////

        //----------------------------

        void InteractLargeDoor(string asEntity)
        {
        	if(GetLocalVarInt(nameof(InteractLargeDoor))==1) return;

        	SetMessage("Ch02Level16", nameof(InteractLargeDoor), 0);
        	PlaySoundAtEntity("jammed", "close_gate", "Player", 0.5f, false);

        	SetLocalVarInt(nameof(InteractLargeDoor), 1);
        	AddTimer(nameof(InteractLargeDoor), 1.5f, nameof(TimerInteractLargeDoor));
        }

        void TimerInteractLargeDoor(string asTimer)
        {
        	SetLocalVarInt(nameof(InteractLargeDoor), 0);
        }

        //----------------------------


        //BEGIN ROSE WHIRL
        void CollideFlashback(string asParent, string asChild, int alState)
        {
        	CreateParticleSystemAtEntity("RoseWhirl", "ps_rose_petals_whirl.ps", "AreaRose", false);
        	AddTimer("WhirlSound", 1.5f, nameof(TimerWhirlSound));
        	AddTimer("WhirlSound", 3.0f, nameof(TimerWhirlSound));
        	AddTimer("WhirlSound", 4.5f, nameof(TimerWhirlSound));

        	StopMusic(3.0f, 10);
        }
        void TimerWhirlSound(string asTimer)
        {
        	PlaySoundAtEntity("RoseWhirlS", "general_wind_whirl.snt", "AreaRose", 0.0f, false);
        }
        //END ROSE WHIRL


        //////////////////
        //Better water collision
        void CollideAreaDeepWater(string asParent, string asChild, int alState)
        {
        	//If going towards deep water, slow down player
        	if(alState == 1)
        		AddLocalVarFloat("WaterSlow", 0.16f);

        	//If going back from deep water, speed up player
        	else
        		AddLocalVarFloat("WaterSlow", -0.16f);

        	SetPlayerMoveSpeedMul(1.0f-GetLocalVarFloat("WaterSlow"));

        	AddDebugMessage("speed is slow: "+Str(GetLocalVarFloat("WaterSlow")), false);
        }

        //Just incase something could go wrong with above collision, reset speed to 1 when leaving water.
        void CollideAreaDeepWaterSafe(string asParent, string asChild, int alState)
        {
        	SetPlayerMoveSpeedMul(1.0f);

        	AddDebugMessage("Speed to normal safety", false);
        }


        /////////////////////////
        //Upstairs checkpoint when completing ladder
        void CollideAreaPlayerUp(string asParent, string asChild, int alState)
        {
        	CheckPoint("cp_up", "PlayerStartArea_2", "", "Hints", "DeathFall");
        }

        //Kill player if failing from far up so he spawns up on bridge instead
        void CollideAreaGround(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Speed is: "+Str(GetPlayerYSpeed()), false);

        	if(GetPlayerYSpeed() > -16) return;

        	GivePlayerDamage(100, "BloodSplat", true, true);
        	PlayGuiSound("player_falldamage_max.snt", 1.0f);
        	StartScreenShake(0.3f, 0.5f, 0.1f, 0.4f);

        	AddDebugMessage("Player Killed On Ground", false);
        }


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	//////////////////////
        	//Load Screen Setup
        	SetupLoadScreen("LoadingText", "Ch02_Diary06_", 6, "game_loading_alexander.jpg");

        	////////////////////
        	// Hub start sanity boost
        	GiveSanityBoost();


        	////////////////////////////////////////
        	//Area set up
        	AddEntityCollideCallback("Player", "AreaDeepWater", nameof(CollideDeepWater), false, 1);
        	AddEntityCollideCallback("Player", "AreaPoison_Start", nameof(CollidePoison_Start), false, 1);
        	AddEntityCollideCallback("Player", "AreaPoison_Stop", nameof(CollidePoison_Stop), false, 1);
        	AddEntityCollideCallback("Player", "AreaPlayerJumpBridge", nameof(CollidePlayerJumpBridge), false, 1);

        	AddEntityCollideCallback("Player", "FlashbackArea_1", nameof(CollideFlashback),true, 1);

        	for(int i=1;i<=5;i++)
        		AddEntityCollideCallback("Player", "AreaDeepWater_"+i, nameof(CollideAreaDeepWater), false, 0);
        	AddEntityCollideCallback("Player", "AreaDeepWaterSafe", nameof(CollideAreaDeepWaterSafe), false, 1);

        	////////////////////////////////////////
        	//Item usage
        	AddUseItemCallback("GetOil", "glass_container_1", "AreaOil", nameof(UseContainerOnOil), true);
        	AddUseItemCallback("GreaseLever", "glass_container_oil_1", "lever_simple01_1", nameof(UseOilOnLever), true);

        	////////////////////////////////////////
        	//Entity set up
        	for(int i=1; i<=3; ++i)
        		AddEntityCollideCallback("cistern_breakable_pipe0"+i, "ladder_static_1", nameof(CollideLadderPipe), false, 1);
        	AddEntityCollideCallback("LadderDown", "ladder_static_1", nameof(CollideLadderDown), false, 1);
        	AddEntityCollideCallback("LadderUp", "ladder_static_1", nameof(CollideLadderUp), false, 1);
        	AddEntityCollideCallback("AreaBridgeStuck", "bridge_metal_vert_1", nameof(CollideBridgeStuck), true, 1);
        	for(int i=1;i<3;i++){
        		AddEntityCollideCallback("BridgeSoundOff"+i, "bridge_metal_vert_1", nameof(CollideBridgeSoundOff), false, 1);
        		AddEntityCollideCallback("BridgeSoundOff"+i, "bridge_metal_vert_2", nameof(CollideBridgeSoundOff), false, 1);
        	}

        	for(int i=1;i<=8;i++)
        	{
        		AddEntityCollideCallback("AreaThrowRock", "stone_small01_"+i, nameof(CollideRockOnChain), false, 1);
        		AddEntityCollideCallback("AreaPlayerJumpBridge", "stone_small01_"+i, nameof(CollideRockOnChain), false, 1);
        	}
        	for(int i=1;i<=23;i++) AddEntityCollideCallback("AreaThrowRock", "xbrick02_"+i, nameof(CollideRockOnChain), false, 1);
        	for(int i=1;i<=23;i++) AddEntityCollideCallback("AreaPlayerJumpBridge", "xbrick02_"+i, nameof(CollideRockOnChain), false, 1);

        	AddEntityCollideCallback("Player", "AreaPlayerUp", nameof(CollideAreaPlayerUp), true, 1);
        	AddEntityCollideCallback("Player", "AreaGround", nameof(CollideAreaGround), false, 1);

        	SetLocalVarInt("bridge_lever_1", 1);
        	SetLocalVarInt("bridge_lever_2", 1);

        	////////////////////////////////////////
        	//Music
        	PlayMusic("08_amb_success", false, 1, 1, 10, false);

        	////////////////////////////////////////
        	//Debug
        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("glass_container_1", "glass_container.ent");
        		//GiveItemFromFile("glass_container_oil_1", "glass_container.ent");

        		//SetGlobalVarInt("WaterHasBeenDrained",1);
        		SetGlobalVarInt("ControlRoomEastSideWorking",1);
        		//SetGlobalVarInt("PlayerHasAntidote",1)

        		GiveItemFromFile("lantern", "lantern.ent");
        	}

        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("player_cough"); PreloadSound("14_elevator_clank"); PreloadSound("player_stand"); PreloadSound("16_water_stream");
        	PreloadSound("16_lever_stuck"); PreloadSound("16_ladder_down"); PreloadSound("16_ladder_imp1"); PreloadSound("16_ladder_imp2");
        	PreloadSound("16_ladder_imp3"); PreloadSound("16_fill_oil"); PreloadSound("16_grease_lever"); PreloadSound("16_winch_bridge");
        	PreloadSound("elevator_stop"); PreloadSound("16_bridge_stuck"); PreloadSound("16_bridge_unstuck"); PreloadSound("18_bridge_hit");
        	PreloadSound("general_wind_whirl");

        	SetMapDisplayNameEntry("CisternEntrance");

        	PlayMusic("16_amb.ogg", true, 1, 1, 0, true);

        	//////////////////////////
        	//Check if water has been pumped out
        	if(GetGlobalVarInt("WaterHasBeenDrained")==1)
        	{
        		SetEntityActive("LiquidArea_1", false);
        		SetEntityActive("water_plane", false);
        		SetEntityActive("water_flood_block", false);
        		SetEntityActive("AreaDeepWater", false);

        		for(int i=1;i<=5;i++) SetEntityActive("AreaDeepWater_"+i, false);
        		SetEntityActive("AreaDeepWaterSafe", false);
        	}

        	//////////////////////////
        	//Check if player has antidote
        	if(GetGlobalVarInt("PlayerHasAntidote")==1)
        	{
        		SetEntityActive("AreaPoison_*", false);
        		SetEntityPlayerInteractCallback("level_wood_5", nameof(InteractSewerDoorHasAntidote), false); //Reset the interaction callback
        		SetLevelDoorLocked("level_wood_5", false);
        	}

        	//////////////////////////
        	//Check if lever is working
        	if(GetGlobalVarInt("ControlRoomEastSideWorking")==1)
        	{
        		SetEntityConnectionStateChangeCallback("bridge_lever_1", nameof(InteractBridgeLever));
        	}
        	else
        	{
        		SetEntityPlayerInteractCallback("bridge_lever_1", nameof(InteractBridgeLever01NotWorking), false);
        	}

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {

        }

        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);
    }
}
