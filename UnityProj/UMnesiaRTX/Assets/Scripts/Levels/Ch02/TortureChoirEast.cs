using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/24_torture_choir_east.hps; journal: docs/ScriptPort/ch02/24_torture_choir_east.md.</summary>
    [HplMapScript("maps/main/ch02/24_torture_choir_east.map")]
    public class TortureChoirEast : HplScript
    {
        //-------------------------------

        ////////////////////////////
        //FALL IN CHASM
        ////////////////////////////

        //-------------------------------

        void CollideFallDeath(string asParent, string asChild, int alState)
        {
        	CheckPoint("checkFallFirstWell", "PlayerStartArea_1", nameof(CheckPointFallChasm), "Hints", "DeathFall_21_TortureNave");

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

        	//Need to reset the brute events!
        	ResetBruteEvent();
        }

        void CheckPointFallChasm(string asName, int alCount)
        {
        	//Do nothing?
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // TERROR AREA
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideTerrorArea(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Terror area: "+asChild+" State: "+alState, false);

        	////////////////////////
        	// Enter
        	if(alState == 1)
        	{
        		PlaySoundAtEntity(asChild+"terror", "ui_torture.ent", asChild, 10, true);
        		AddTimer(asChild+"terror", 2, nameof(TimerTerrorDrain));

        		FadeImageTrailTo(1.0f, 0.5f);

        		//////////////////
        		// A bit of extra ambient track if been in contact with all terror rooms machines
        		if(GetLocalVarInt(asChild) != 2) SetLocalVarInt(asChild, 1);

        		if(GetLocalVarInt("AreaInsanityEffect_1") == 1 && GetLocalVarInt("AreaInsanityEffect_2") == 1 &&
        			GetLocalVarInt("AreaInsanityEffect_3") == 1){
        			PlaySoundAtEntity("amb_out", "24_amb_out.snt", "AreaPlant_2", 4.0f, true);
        			SetLocalVarInt(asChild, 2);

        			AddDebugMessage(asChild, false);
        		}
        	}
        	////////////////////////
        	// Leave
        	else
        	{
        		FadeImageTrailTo(0.0f, 0.5f);
        		StopSound(asChild+"terror", 2);
        		RemoveTimer(asChild+"terror");
        	}

        }

        //------------------------------------------

        void TimerTerrorDrain(string asTimer)
        {
        	if(GetPlayerSanity() < 35)
        		GiveSanityDamage(0.05f*0.15f, false);
        	else
        		GiveSanityDamage(0.05f*0.5f, false);

        	AddTimer(asTimer, 0.05f, nameof(TimerTerrorDrain));
        }

        //------------------------------------------

        ////////////////////////////
        // IRON MAIDEN EVENT
        ////////////////////////////

        //-------------------------------

        ///////////////////////////////////////////////////
        // Iron Maiden doors opening scare
        void OpenIronMaiden(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Scary iron maiden opening", false);

        	SetSwingDoorDisableAutoClose("iron_maiden_2", true);
        	SetSwingDoorClosed("iron_maiden_2", false, false);

        	/////////////////////////////////////////////
        	// Open iron maiden doors
        	AddBodyImpulse("iron_maiden_2_leftDoor", -0.7f, 0 , -0.7f, "world");
        	AddBodyImpulse("iron_maiden_2_rightDoor", -0.7f, 0 , 0.7f, "world");

        	AddTimer(nameof(TimerExtraMaidenImpulse), 0.1f, nameof(TimerExtraMaidenImpulse));

        	PlaySoundAtEntity("AreaIronMaidenEffect", "24_iron_maiden", "AreaIronMaidenEffect", 0.0f, false);

        	PlayerReact(0.5f);

        	CreateParticleSystemAtEntity("maidensmoke","ps_iron_maiden_event_smoke.ps", "AreaIronMaidenEffect", false);
        	StartScreenShake(0.1f, 0.1f, 0.0f, 0.1f);

        	// Disable trigger area and activate room sanity drain
        	SetEntityActive("AreaIronMaidenOpen", false);
        	SetEntityActive("AreaInsanityEffect_1", true);

        	GiveSanityDamage(10.0f, true);
        	SetRadialBlurStartDist(0.4f);
        	FadeRadialBlurTo(0.2f, 0.4f);

        	AddTimer(nameof(TimerActiveMaidenEmotion),1.5f,nameof(TimerActiveMaidenEmotion));
        }

        void TimerExtraMaidenImpulse(string asTimer)
        {
        	AddBodyImpulse("iron_maiden_2_leftDoor", -0.7f, 0 , -0.7f, "world");
        	AddBodyImpulse("iron_maiden_2_rightDoor", -0.7f, 0 , 0.7f, "world");
        }

        //Activate an emotion event area.
        void TimerActiveMaidenEmotion(string asTimer)
        {
        	SetEntityActive("AreaEmotionEffectIronMaiden", true);
        	FadeRadialBlurTo(0.0f, 0.4f);
        }


        //-------------------------------

        ////////////////////////////
        // BRAZEN BULL EVENT
        ////////////////////////////

        //-------------------------------

        ///////////////////////////////////////////////////
        // Event when lighting bonfire under brazen bull
        void IgniteBonfire(string asEntityName, string asType)
        {
        	if(asType!="OnIgnite")
        		return;

        	AddDebugMessage("Activating brazen bull scream event", false);

        	AddTimer("BrazenBullScream01", 3.0f, nameof(TimedBrazenBullScream));
        }

        ///////////////////////////////////////////////////
        // The screams from the brazen bull.
        void TimedBrazenBullScream(string asTimer)
        {
        	float fNextScreamTime = 0.5f;
        	int lIdx = GetLocalVarInt("BrazenBullScreamCount");

        	string sScreamSound = "";
        	bool bAddNewTimer = true;

        	if(lIdx==0)
        	{
        		sScreamSound = "24_bang.snt";

        		SetLeverStuckState("brazen_bull_2", -1, true);//Slam the hatch shut
        		AddBodyImpulse("brazen_bull_2_hatch",0,-1,-2, "World");

        		PlaySoundAtEntity("brazen_bull_2", "24_hatch_imp.snt", "brazen_bull_2", 0.0f, false);

        		PlayerReact(0.5f);

        		SetEntityActive("AreaInsanityEffect_3", true);

        		SetRadialBlurStartDist(0.3f);
        		FadeRadialBlurTo(0.1f, 0.5f);

        		GiveSanityDamage(20.0f, true);
        	}
        	else if(lIdx==1)
        	{
        		sScreamSound = "24_burn.snt";

        		AddTimer("24_burn2", 1.0f, nameof(TimerBurnMore));
        		AddTimer("24_burn3", 2.0f, nameof(TimerBurnMore));
        		AddTimer("stopradial_trigger_enemy", 5.0f, nameof(TimerBurnMore));
        		fNextScreamTime = 2.5f;
        	}
        	else if(lIdx==2)
        	{
        		sScreamSound = "24_bull.snt";

        		bAddNewTimer = false;
        	}

        	//Add index var
        	AddLocalVarInt("BrazenBullScreamCount",1);

        	//Play sound
        	if(sScreamSound != "")
        		PlaySoundAtEntity("BrazenScream", sScreamSound, "AreaBrazenBullScream", 0, false);

        	AddDebugMessage("Brazen bull scream "+lIdx+" screamsound: '"+sScreamSound+"'", false);

        	//Run event again
        	if(bAddNewTimer)
        		AddTimer("BrazenBullScream01", fNextScreamTime, nameof(TimedBrazenBullScream));
        }

        void TimerBurnMore(string asTimer)
        {
        	if(asTimer == "stopradial_trigger_enemy"){
        		FadeRadialBlurTo(0.0f, 0.1f);

        		//TODO: TRIGGER ENEMY SO HE DOES NOT RUIN THE OTHER ENEMIES AND SO THAT HE DOES NOT
        		//		GO INTO THE BULL ROOM (ROOM STRUCTURE MAKES HIS BEHAVIOR IMPOSSIBLE).
        		//AddEnemyPatrolNode("servant_brute_7", "PathNodeArea_1", 0.0f, "");
        		//AddEnemyPatrolNode("servant_brute_7", "PathNodeArea_385", 10.0f, "");
        		//AddEnemyPatrolNode("servant_brute_7", "PathNodeArea_52", 0, "");
        		//SetEntityActive("servant_brute_7", true);
        	}

        	PlaySoundAtEntity(asTimer, asTimer, "AreaBrazenBullScream", 0, false);
        }
        //-------------------------------

        ////////////////////////////
        // POISON FUNGI
        /////////////////////////

        //-------------------------------

        void InteractPoisionFungi(string asEntity)
        {
        	////////////////////////////////////
        	//If player clicks fast enough, the fungi will break.
        	AddLocalVarInt("PosionFungiClickCount",1);
        	AddTimer("DecClickCount", 0.9f, nameof(TimerDecClickCount));

        	if(GetLocalVarInt("PosionFungiClickCount")==6)
        	{
        		SetPropHealth(asEntity, 0);
        		return;
        	}


        	////////////////////////////////////
        	//Check if interact message + squish sound should be shown and played
        	if(GetTimerTimeLeft("PoisonInteract")>0) return;

        	SetMessage("Ch02Level24", "InteractPoisonFungi", -1);

        	PlayGuiSound("26_touch_fungu.snt", 1.0f);

        	AddTimer("PoisonInteract", 1, "");
        }

        void TimerDecClickCount(string asTimer)
        {
        	AddLocalVarInt("PosionFungiClickCount",-1);
        }

        //-------------------------------

        void CollidePlayerWithFungi(string asParent, string asChild, int alState)
        {
        	//Check so speed is high enough for breaking with a stomp
        	float fSpeed = GetPlayerYSpeed();
        	AddDebugMessage("Land on fungi speed:"+Str(fSpeed), false);
        	if(fSpeed<-5)
        	{
        		SetPropHealth(asChild, 0);
        	}
        }

        //-------------------------------

        void UseChipperOnFungi(string asItem, string asEntity)
        {
        	SetPropHealth(asEntity, 0);
        }

        //-------------------------------

        void PosionFungiBreak(string asEntity, string asType)
        {
        	SetEntityActive("poison_gland", true);

        	FadeLightTo("PointLight_5", 0, 0.6f, 0.8f, 0.8f, -1, 2);//Add extra glow to posion gland.

        	PlayGuiSound("26_break_fungu.ogg", 0.8f);

        	SetEntityActive("AreaPlant_14", false);
        	SetEntityActive("AreaPlant_24", false);

        	AddDebugMessage("Broke the fungi!", false);
        }

        //-------------------------------

        // Player picks up poison gland
        void PickPoisonGland(string asEntityName, string asType)
        {
        	AddDebugMessage("Picked up poison gland, incrementing ingredient count", false);

        	FadeLightTo("PointLight_5", 0,0,0,0, -1, 2);

        	AddLocalVarInt("IngredientCount", 1);

        	GiveSanityBoostSmall();
        	PlayMusic("15_puzzle_hole.ogg", false, 0.8f, 0.5f, 9, false);
        }

        //-------------------------------

        ////////////////////////////
        // BRUTE
        /////////////////////////

        //INFO:
        // The way the following works is that there are areas wich enable a Brute connected to that area.
        // When an area is triggered it is disabled, and no more area can be triggered until brute is disabled.
        // Brute will be disabled if the player enters a torture room corrdidor (and each its end) or by normal
        // means (player cannot see brute and its path is over). After this new areas can be triggered.


        //-------------------------------

        int glNumOfBrutes=6;

        //-------------------------------

        void InitEnemies()
        {
        	////////////////////////////
        	//General setup
        	for(int i=1; i<=glNumOfBrutes; ++i)
        	{
        		SetEntityActive("servant_brute_"+i, false);
        		SetEnemyDisabled("servant_brute_"+i, false);
        		ClearEnemyPatrolNodes("servant_brute_"+i);
        	}

        	////////////////////////////
        	//1
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_152", RandFloat(1,7), "");

        	if(RandInt(0,1)==1)	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_217", RandFloat(1,7), "");
        	else			AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_213", RandFloat(1,7), "");

        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_225", RandFloat(1,4), "");
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_218", RandFloat(1,3), "");

        	////////////////////////////
        	//2
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_74", RandFloat(1,5), "");
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_88", RandFloat(1,5), "");
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_158", RandFloat(1,7), "");

        	////////////////////////////
        	//3
        	AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_373", RandFloat(1,5), "");
        	AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_60", RandFloat(1,5), "");
        	AddEnemyPatrolNode("servant_brute_3", "PathNodeArea_74", RandFloat(1,5), "");

        	////////////////////////////
        	//4
        	AddEnemyPatrolNode("servant_brute_4", "PathNodeArea_95", RandFloat(1,4), "");
        	AddEnemyPatrolNode("servant_brute_4", "PathNodeArea_251", RandFloat(1,4), "");
        	AddEnemyPatrolNode("servant_brute_4", "PathNodeArea_318", RandFloat(1,4), "");

        	////////////////////////////
        	//5
        	AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_283", RandFloat(1,5), "");
        	AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_276", RandFloat(1,5), "");
        	if(RandInt(0,1)==1)	AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_294", RandFloat(1,5), "");
        	else			AddEnemyPatrolNode("servant_brute_5", "PathNodeArea_251", RandFloat(1,5), "");

        	////////////////////////////
        	//6
        	if(RandInt(0,1)==1)	AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_37", RandFloat(1,5), "");
        	else			AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_54", RandFloat(1,5), "");

        	AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_52", RandFloat(1,5), "");

        	if(RandInt(0,1)==1)	AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_359", RandFloat(1,5), "");
        	else			AddEnemyPatrolNode("servant_brute_6", "PathNodeArea_33", RandFloat(1,5), "");

        }

        //-------------------------------

        //Helper to reset all brute event related stuff
        void ResetBruteEvent()
        {
        	AddDebugMessage("Resetting Brute event!", false);

        	SetLocalVarInt("BruteEventActive",0); //Clear that any brute event is in progress!
        	SetLocalVarInt("PlayerAtTortureEntranceStart",0); //Reset this var.

        	RemoveTimer("BruteEventStart"); //In case the timer has ended yet.
        }

        //-------------------------------

        void EnemyDisabled(string asEnemy, string asType)
        {
        	ResetBruteEvent();

        	AddDebugMessage("Disabled Brute: "+ asEnemy, false);
        }

        //-------------------------------

        void EnemyCollideChasm(string asParent, string asChild, int alState)
        {
        	SetEntityActive(asParent, false);
        	ResetBruteEvent();

        	AddDebugMessage("Brute: "+ asParent+" fell in chasm!", false);
        }

        //-------------------------------

        void CollideBruteEvent(string asParent, string asChild, int alState)
        {
        	/////////////////////////
        	//Check if OK to start event

        	//There can only be one brute event triggered at any time!
        	if(GetLocalVarInt("BruteEventActive")==1) return;
        	SetLocalVarInt("BruteEventActive",1);

        	//Get index of area (and brute)
        	int lIdx = GetLocalVarInt(asChild+"_Idx");
        	SetLocalVarInt("BruteEvent_Idx",lIdx);

        	/////////////////////////
        	//Start up event

        	//Area only triggered once!
        	SetEntityActive(asChild, false);

        	//Use timer so it enconter feels more random
        	AddTimer("BruteEventStart", RandFloat(0.5f, 3.5f), nameof(TimerBruteEventStart));

        	/////////////////////////
        	//Setup checkpoint
        	string sCheckPointStartPos = "PlayerStartArea_1";

        	//If player has all orbs, then we want to pick a start pos in one of the torture chambers
        	if(GetGlobalVarInt("ChoirOrbCount")==3)
        	{
        		 sCheckPointStartPos = "PlayerStartArea_"+RandInt(2,4);
        	}

        	//This idx is only used to randomize hint messages.
        	int lHintIdx = RandInt(1,3);
        	CheckPoint("checkBrute"+lHintIdx, sCheckPointStartPos, nameof(CheckPointBrute), "Hints", "DeathBrute_24_TortureChoir_0"+lHintIdx);


        	AddDebugMessage("Enter brute event area: "+ asChild, false);
        }

        //Used a delay start of enemy to make it feel more random.
        void TimerBruteEventStart(string asTimer)
        {
        	int lIdx = GetLocalVarInt("BruteEvent_Idx");

        	SetEntityActive("servant_brute_"+lIdx, true);

        	AddDebugMessage("Started brute event: "+ "servant_brute_"+lIdx, false);
        }

        //-----------------------------------------------------

        void CheckPointBrute(string asName, int alCount)
        {
        	ResetBruteEvent();

        	InitEnemies(); //Reset all enemy paths

        	//Enable a random area
        	int lIdx = RandInt(1,glNumOfBrutes);
        	SetEntityActive("AreaBruteEvent_"+lIdx, true);

        	AddDebugMessage("Checkpoint activated Random Area: "+lIdx, false);
        }

        //-------------------------------

        //Player is at start of corridor, make the brute walk towards center of map.
        void CollideTortureEntranceStart(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("BruteEventActive")!=1) return; //No event so do nothing
        	if(GetLocalVarInt("PlayerAtTortureEntranceStart") == 1) return; //Player as already been in stat

        	SetLocalVarInt("PlayerAtTortureEntranceStart",1); //Reset the var.

        	int lIdx = GetLocalVarInt("BruteEvent_Idx");

        	SetEnemyDisableTriggers("servant_brute_"+lIdx, true); //Disable enemy
        	ClearEnemyPatrolNodes("servant_brute_"+lIdx);
        	AddEnemyPatrolNode("servant_brute_"+lIdx, "PathNodeArea_157", 5, ""); //Move towards center of map.

        	AddDebugMessage("In start area: "+ asChild, false);
        }

        //-----------------------------------------------------

        //Player has been at start but turns back. Enable enemy again!
        void CollideTortureEntranceExit(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("BruteEventActive")!=1) return; //No event so do nothing
        	if(GetLocalVarInt("PlayerAtTortureEntranceStart") != 1) return; //Unless player has been at player start, do nothing here.

        	SetLocalVarInt("PlayerAtTortureEntranceStart",0);

        	int lIdx = GetLocalVarInt("BruteEvent_Idx");

        	//Reset all enemies and set current to true!
        	InitEnemies();
        	SetEntityActive("servant_brute_"+lIdx, true);

        	AddDebugMessage("In exit area: "+ asChild, false);
        }

        //-----------------------------------------------------

        //Enemy is disabled and player is at end of tunnel. Disable the enemy
        void CollideTortureEntranceEnd(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("BruteEventActive")!=1) return; //No event so do nothing

        	//Reset all variables concerning the brute event
        	ResetBruteEvent();

        	int lIdx = GetLocalVarInt("BruteEvent_Idx");

        	SetEntityActive("servant_brute_"+lIdx, false);
        	SetEnemyDisableTriggers("servant_brute_"+lIdx, false);

        	AddDebugMessage("In end area: "+ asChild, false);
        }

        //-----------------------------------------------------

        ////////////////////////////
        // PICK UP ORBS
        ////////////////////////////

        //------------------------------------------

        void PickOrbPiece(string asEntityName, string asType)
        {
        	AddDebugMessage("Picked up orb piece, incrementing count", false);

        	AddGlobalVarInt("ChoirOrbCount", 1);

        	if(asEntityName=="OrbPiece_4") FadeLightTo("PointLight_7", 0,0,0,0, -1, 2);
        	if(asEntityName=="OrbPiece_5") FadeLightTo("PointLight_8", 0,0,0,0, -1, 2);
        	if(asEntityName=="OrbPiece_6") FadeLightTo("PointLight_9", 0,0,0,0, -1, 2);

        	//Enable random enemy area
        	int lIdx = RandInt(1,glNumOfBrutes);
        	SetEntityActive("AreaBruteEvent_"+lIdx, true);
        	AddDebugMessage("PickOrb activated Random Area: "+lIdx, false);

        	//When all orbs are collecting, give player some extra sanity and nice music.
        	if(GetGlobalVarInt("ChoirOrbCount")==3)
        	{
        		PlayMusic("02_puzzle.ogg", false, 0.7f, 0, 10, false);
        		GiveSanityBoost();

        		CompleteQuest("21OrbsLeftInChoir", "21OrbsLeftInChoir");

        		SetEntityActive("AreaTriggerBones", true);
        	}
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // INTERACT TORTURE MACHINE
        ////////////////////////////////////////////

        //------------------------------------------

        void InteractEmotionEffectBrazenBull(string asArea)
        {
        	StartEffectEmotionFlash("Ch02Level24", "TortureText_Bull", "ui_insanity_touch.snt");

        	SetEntityActive(asArea, false);
        }

        //------------------------------------------

        void InteractEmotionEffectIronMaiden(string asArea)
        {
        	StartEffectEmotionFlash("Ch02Level24", "TortureText_Maiden", "ui_insanity_touch.snt");

        	SetEntityActive(asArea, false);
        }

        //------------------------------------------

        void WheelFlashbackOver()
        {
        	SetEntityActive("AreaEmotionEventWheel", true);
        }


        void InteractEmotionEffectWheel(string asArea)
        {
        	StartEffectEmotionFlash("Ch02Level24", "TortureText_Wheel", "ui_insanity_touch.snt");

        	SetEntityActive(asArea, false);
        }

        //------------------------------------------


        //ALEXANDER VOICE ON ENTRY
        void TimerAlexanderVoice(string asTimer)
        {
        	AddEffectVoice("CH02L24_Alexander_01", "", "Voice", "CH02L24_Alexander_01", false, "", 0,0 );
        	AddEffectVoice("CH02L24_Alexander_02", "", "Voice", "CH02L24_Alexander_02", false, "", 0,0 );
        	AddEffectVoice("CH02L24_Alexander_03", "", "Voice", "CH02L24_Alexander_03", false, "", 0,0 );
        	AddEffectVoice("CH02L24_Alexander_04", "", "Voice", "CH02L24_Alexander_04", false, "", 0,0 );
        }


        //Plant branch sounds
        void CollidePlant(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity(asChild, "24_bush.snt", asChild, RandFloat(0.5f,3.0f), false);
        }

        void PlayerReact(float fTime)
        {
        	AddTimer("scare", fTime, nameof(TimerPlayerReact));
        	AddTimer("breath", 2.0f+fTime, nameof(TimerPlayerReact));
        	AddTimer("breathl", 4.0f+fTime, nameof(TimerPlayerReact));
        }
        void TimerPlayerReact(string asTimer)
        {
        	if(asTimer == "breathl")
        		PlayGuiSound("react_"+asTimer, 0.4f);
        	else
        		PlayGuiSound("react_"+asTimer, 0.7f);
        }


        //Fungu description
        void descFungus(string asEntity)
        {
        	SetMessage("Ch02Level24", "DescFungus", 0);
        }


        //Maiden horror
        void CollideAreaInMaiden(string asParent, string asChild, int alState)
        {
        	if(alState == 1)
        		SetEntityPlayerLookAtCallback("AreaLookMaiden", nameof(LookAtAreaLookMaiden), true);
        	else
        		SetEntityPlayerLookAtCallback("AreaLookMaiden", "", true);
        }
        void LookAtAreaLookMaiden(string asEntity, int alState)
        {
        	if(GetLocalVarInt("MaidenEvent") == 1) return;

        	AddBodyImpulse("iron_maiden_2_rightDoor", 10, 0 , -10, "world");

        	GivePlayerDamage(10.0f, "Slash", false, false);
        	GiveSanityDamage(10.0f, true);

        	PlayGuiSound("attack_claw_hit02.ogg", 0.8f);

        	SetSwingDoorClosed("iron_maiden_2", true, true);

        	PlayerReact(0.5f);

        	SetLocalVarInt("MaidenEvent", 1);
        }

        //Random rock ambience
        void TimerAreaRock(string asTimer)
        {
        	int iRand = RandInt(1, 6);

        	PlaySoundAtEntity(asTimer+iRand, "24_rock.snt", asTimer+iRand, 1, false);

        	StartScreenShake(0.007f, 0.0f, 4.0f, 2.0f);
        	SetRadialBlurStartDist(0.8f);
        	FadeRadialBlurTo(0.1f, 0.03f);

        	AddTimer("EndRadial", 4.0f, nameof(TimerEndRadial));
        	AddTimer(asTimer, RandFloat(15.0f, 30.0f), nameof(TimerAreaRock));
        }
        void TimerEndRadial(string asTimer)
        {
        	FadeRadialBlurTo(0.0f, 0.1f);
        }

        //It's raining bones! Hallelujah! It's raining bones! Amen!
        void CollideAreaTriggerBones(string asParent, string asChild, int alState)
        {
        	if(GetPlayerSanity() > 70) return;	//No event on sanity more than 70

        	StartScreenShake(0.1f, 0.0f, 2.0f, 4.0f);

        	PlaySoundAtEntity("shake", "general_rock_rumble_no3d.snt", "Player", 2.0f, false);

        	PlayerReact(2.0f);

        	SetRadialBlurStartDist(0.3f);
        	FadeRadialBlurTo(0.3f, 0.1f);

        	AddTimer("roar", 0.1f, nameof(TimerStopBones));
        	AddTimer("drop", 0.2f, nameof(TimerStopBones));
        	AddTimer("stopbones", 2.0f, nameof(TimerStopBones));
        }
        void TimerStopBones(string asTimer)
        {
        	if(asTimer == "roar")
        		PlayGuiSound("insanity_monster_roar02.ogg", 0.7f);
        	if(asTimer == "drop")
        		SetEntityActive("Bone_*", true);
        	else{
        		StopSound("shake", 5.0f);
        		FadeRadialBlurTo(0.0f, 0.01f);
        	}
        }


        //PIPE EVENT TO HINT ABOUT FOLLOWING
        void TimerPipeEvent(string asTimer)
        {
        	string sEvent = asTimer;	//Do not edit, sets timer loop function name.
        	AddLocalVarInt(sEvent, 1);	//Do not edit, steps through timer loop events.
        	bool bPauseAtStep = false;	//Do not edit, to pause or end the timer loop at specified step.

        	float fEventSpeed = 0.5f;	//Set to preferred default time for the timer loop.

        	switch(GetLocalVarInt(sEvent)){
        		//////////
        		//Pipe sound
        		case 1:
        			PlaySoundAtEntity("sound1", "24_pipe.snt", "AreaPipe_1", 0.0f, false);
        			SetPlayerRunSpeedMul(0.2f);
        		break;

        		//////////
        		//Pipe look at area 1
        		case 2:
        			StartPlayerLookAt("AreaPipe_1", 4.0f, 4.0f, "");
        			SetPlayerRunSpeedMul(0.4f);
        			SetPlayerMoveSpeedMul(0.4f);
        			PlayerReact(0.1f);
        			fEventSpeed = 1.5f;
        		break;

        		//////////
        		//Pipe sound 2
        		case 3:
        			StartPlayerLookAt("AreaPipe_2", 0.8f, 0.8f, "");
        			SetPlayerRunSpeedMul(0.6f);
        			SetPlayerMoveSpeedMul(0.6f);
        			fEventSpeed = 1.5f;
        		break;

        		//////////
        		//Pipe look at area 2
        		case 4:
        			FadeRadialBlurTo(0.0f, 0.1f);
        			PlaySoundAtEntity("sound2", "24_pipe.snt", "AreaPipe_2", 1.0f, false);
        			SetPlayerRunSpeedMul(0.8f);
        			SetPlayerMoveSpeedMul(0.8f);
        			SetPlayerLookSpeedMul(0.6f);
        		break;

        		//////////
        		//End timer when no more steps in the event found.
        		default:
        			StopPlayerLookAt();
        			SetPlayerMoveSpeedMul(1.0f);
        			SetPlayerRunSpeedMul(1.0f);
        			SetPlayerLookSpeedMul(1.0f);
        			bPauseAtStep = true;
        		break;
        	}

        	if(!bPauseAtStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }
        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("ChoirEast");

        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("lantern", "lantern.ent");
        		for(int i=0;i<10;++i)
        			GiveItemFromFile("tinderbox", "tinderbox.ent");

        		GiveItem("stone_hammer_chipper", "Puzzle", "stone_hammer_chipper", "stone_hammer_chipper.tga", 0);

        		AddTimer("alexander", 10.0f, nameof(TimerAlexanderVoice));
        		AddTimer(nameof(TimerPipeEvent), 1.0f, nameof(TimerPipeEvent));
        		SetPlayerMoveSpeedMul(0.2f);
        		SetPlayerRunSpeedMul(0.0f);
        		SetPlayerLookSpeedMul(0.2f);
        		SetRadialBlurStartDist(0.3f);
        		FadeRadialBlurTo(0.1f, 0.1f);
        		PlayGuiSound("react_sigh.snt", 0.6f);

        	//PLAY ALEXANDER VOICE
        	} else{
        		AddTimer("alexander", 10.0f, nameof(TimerAlexanderVoice));
        		AddTimer(nameof(TimerPipeEvent), 1.0f, nameof(TimerPipeEvent));
        		SetPlayerMoveSpeedMul(0.2f);
        		SetPlayerRunSpeedMul(0.0f);
        		SetPlayerLookSpeedMul(0.2f);
        		SetRadialBlurStartDist(0.3f);
        		FadeRadialBlurTo(0.1f, 0.1f);
        		PlayGuiSound("react_sigh.snt", 0.7f);
        	}

        	AddTimer("AreaRock_", RandFloat(3.0f,10.0f), nameof(TimerAreaRock));

        	///////////////////////////////////////////////
        	// USE ITEM CALLBACKS
        	AddUseItemCallback("hammer_chipper_on_fungi", "stone_hammer_chipper", "poison_fungi_breakable", nameof(UseChipperOnFungi), false);

        	///////////////////////////////////////////////
        	// COLLIDE CALLBACKS
        	// Torture rooms
        	for(int i=1;i<=3;++i)
        	{
        		AddEntityCollideCallback("Player", "AreaInsanityEffect_"+i, nameof(CollideTerrorArea), false, 0);
        	}
        	AddEntityCollideCallback("Player", "AreaIronMaidenOpen", nameof(OpenIronMaiden), true, 1);

        	AddEntityCollideCallback("Player", "AreaInMaiden", nameof(CollideAreaInMaiden), false, 0);

        	// Poison fungi
        	AddEntityCollideCallback("Player", "poison_fungi_breakable", nameof(CollidePlayerWithFungi), false, 1);

        	// Bute event
        	for(int i=1; i<=glNumOfBrutes; ++i)
        	{
        		AddEntityCollideCallback("Player", "AreaBruteEvent_"+i, nameof(CollideBruteEvent), false, 1);
        		SetLocalVarInt("AreaBruteEvent_"+i+"_Idx", i);

        	}
        	for(int i=1; i<=3; ++i)
        	{
        		AddEntityCollideCallback("Player", "AreaTortureEntranceExit_"+i, nameof(CollideTortureEntranceExit), false, 1);
        		AddEntityCollideCallback("Player", "AreaTortureEntranceStart_"+i, nameof(CollideTortureEntranceStart), false, 1);
        		AddEntityCollideCallback("Player", "AreaTortureEntranceEnd_"+i, nameof(CollideTortureEntranceEnd), false, 1);
        	}

        	// Fall areas
        	for(int i=1; i<=3; ++i)
        	{
        		AddEntityCollideCallback("Player", "AreaFallDeath_"+i, nameof(CollideFallDeath), false, 1);
        		AddEntityCollideCallback("servant_brute*", "AreaFallDeath_"+i, nameof(EnemyCollideChasm), false, 1);
        	}


        	//Plant branch sound
        	for(int i=1;i<=23;i++)AddEntityCollideCallback("Player", "AreaPlant_"+i, nameof(CollidePlant), false, 0);

        	//Bones, bones, bones - Dr Brennan would be so proud.
        	AddEntityCollideCallback("Player", "AreaTriggerBones", nameof(CollideAreaTriggerBones), true, 1);

        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PlayMusic("game_menu.ogg", true, 0.7f, 4.0f, 0, true);

        	PreloadSound("24_bull"); PreloadSound("24_burn"); PreloadSound("24_bang"); PreloadSound("11_fall");
        	PreloadSound("scare_male_terrified5"); PreloadSound("player_bodyfall"); PreloadSound("player_falldamage_max"); PreloadSound("ui_torture");
        	PreloadSound("24_iron_maiden"); PreloadSound("24_bush"); PreloadSound("general_rock_rumble_no3d"); PreloadSound("24_rock");
        	PreloadSound("react_breath"); PreloadSound("react_scare"); PreloadSound("insanity_monster_roar02");

        	InitEnemies(); //Want to init all enemies on each init, so it is sure that they are all reset properly!

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	ResetBruteEvent();
        }

        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);
    }
}
