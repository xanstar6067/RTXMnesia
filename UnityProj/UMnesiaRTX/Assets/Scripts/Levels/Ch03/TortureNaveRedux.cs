using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch03
{
    /// <summary>Port of maps/main/ch03/26_torture_nave_redux.hps; journal: docs/ScriptPort/ch03/26_torture_nave_redux.md.</summary>
    [HplMapScript("maps/main/ch03/26_torture_nave_redux.map")]
    public class TortureNaveRedux : HplScript
    {
        //------------------------------------------

        ///////////////////////////////////////////
        // GENERAL EFFECTS
        ////////////////////////////////////////////

        //------------------------------------------

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
        		GiveSanityDamage(20.0f, true);
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

        //------------------------------------------

        ///////////////////////////////////////////
        // BOIL BLOOD
        ////////////////////////////////////////////

        //------------------------------------------

        void UseBloodOnBurner(string asItem, string asEntity)
        {
        	SetEntityActive("glass_container_blood_static_1", true);

        	RemoveItem(asItem);

        	PlaySoundAtEntity("placeglass", "26_place_glass", asEntity, 0.0f, false);

        	//Delay the boil a bit
        	AddTimer("BloodInPlace", 1, nameof(TimerBloodInPlace));
        }

        void TimerBloodInPlace(string asTimer)
        {
        	SetLocalVarInt("BloodOnBurner", 1);

        	//Check if blood is ready to boil!
        	CheckBoilBlood();
        }

        //------------------------------------------

        void TurnSpecialBurner(string asEntityName, int alState)
        {
        	if(alState == 1)
        	{
        		PlaySoundAtEntity("bruner_on","26_burner_ignite.snt", "AreaBurner", 0, false);

        		CreateParticleSystemAtEntity("burnerfire", "ps_fire_lab_burner.ps", "AreaBurner", true);

        		PlaySoundAtEntity("burnerfire","26_burner_loop.snt", "AreaBurner", 1, true);

        		AddTimer("burnerfullspeed",2, nameof(TimerBurnerFullSpeed));
        	}
        	else if(alState == -1)
        	{
        		PlaySoundAtEntity("bruner_off","26_burner_ignite.snt", "AreaBurner", 0.15f, false);

        		DestroyParticleSystem("burnerfire");
        		StopSound("burnerfire", 1);

        		RemoveTimer("burnerfullspeed");

        		SetLocalVarInt("BurnerReady", 0);
        	}

        	AddDebugMessage("Burner state: "+ alState, false);
        }

        void TimerBurnerFullSpeed(string asTimer)
        {
        	SetLocalVarInt("BurnerReady", 1);

        	//Check if blood is ready to boil!
        	CheckBoilBlood();
        }

        //------------------------------------------

        void CheckBoilBlood()
        {
        	//If the burner is a full speed, blood is in place and blood is not boiled yet, then
        	if(GetLocalVarInt("BurnerReady")==1 && GetLocalVarInt("BloodOnBurner")==1 && GetLocalVarInt("BloodBoiled")==0)
        	{
        		SetLocalVarInt("BloodBoiled", 1);

        		CreateParticleSystemAtEntity("bloodboil", "ps_glass_container_blood_bubbles.ps", "AreaBloodBoil", false);
        		PlaySoundAtEntity("bloodboil","puzzle_boil.snt", "AreaBloodBoil", 1, false);

        		AddTimer("boileffectdone", 3, nameof(TimerBoilEffectDone));
        	}
        }

        void TimerBoilEffectDone(string asTimer)
        {
        	DestroyParticleSystem("bloodboil");
        	StopSound("bloodboil",1);

        	AddTimer("fadeoutblood", 0.3f, nameof(TimerBoilEffectFadeOutBlood));
        	SetPropActiveAndFade("glass_container_mix_notdone_1", true, 0.5f);

        	PlaySoundAtEntity("boildone","puzzle_acid", "AreaBloodBoil", 0, false);
        }

        void TimerBoilEffectFadeOutBlood(string asTimer)
        {
        	SetPropActiveAndFade("glass_container_blood_static_1", false, 0.5f);
        }

        //------------------------------------------

        void UseMixtureOnBurner(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseMixtureOnBurner), -1);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // SQUEEZE GLAND
        ////////////////////////////////////////////

        //------------------------------------------

        void UseBloodOnSqueezer(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseBloodOnSqueezer), -1);
        }

        //------------------------------------------

        void UseMixOnSqueezer(string asItem, string asEntity)
        {
        	//Mixture has alrady been placed once!
        	if(GetLocalVarInt("MixPlacedUnderSqueezer")==1)
        	{
        		SetMessage("Ch03Level26", "UseMixOnSqueezerSecondTime", -1);
        		return;
        	}

        	PlaySoundAtEntity("placeglass", "26_place_glass", asEntity, 0.0f, false);

        	RemoveItem(asItem);

        	SetEntityActive("glass_container_mix_notdone_static_1", true);

        	SetLocalVarInt("MixPlacedUnderSqueezer", 1);
        }

        //------------------------------------------

        void UseGlandOnSqueezer(string asItem, string asEntity)
        {
        	int lState = GetLocalVarInt("SqueezerState");

        	////////////////////
        	//Check if there is a container below
        	if(GetLocalVarInt("MixPlacedUnderSqueezer")==0)
        	{
        		//SetMessage("Ch03Level26", "NoContainerBelowSqueezer", -1);
        		//return;
        	}

        	////////////////////
        	//Squeezer is open
        	if(lState == -1)
        	{
        		PlaySoundAtEntity("placegland","26_place_gland.snt", "AreaSqueezer", 1, false);
        		PlaySoundAtEntity("placegland1","26_place_gland.snt", "AreaSqueezer", 1, false);

        		RemoveItem(asItem);
        		SetEntityActive("poison_gland_static_1", true);

        		SetLocalVarInt("GlandInSqueezer", 1);
        	}
        	////////////////////
        	//Squeezer is NOT open
        	else
        	{
        		SetMessage("Ch03Level26", "UseOnClosedSqueezer", -1);
        	}
        }

        //------------------------------------------


        void InteractSqueezer(string asEntity)
        {
        	if(GetLocalVarInt("MixPlacedUnderSqueezer")==0 && GetLocalVarInt("GlandInSqueezer")==1){
        		SetWheelStuckState(asEntity, -1, false);
        		SetMessage("Ch03Level26", "InteractSqueezeTooSoon", 0);
        		return;
        	}

        	SetWheelStuckState(asEntity, 0, false);
        }

        void TurnSqueezer(string asEntityName, int alState)
        {
        	SetLocalVarInt("SqueezerState", alState);
        	AddDebugMessage("Squeezer state: "+alState, false);

        	//Do nothing if no gland is in squeezer or has already been squeezed.
        	if(GetLocalVarInt("GlandInSqueezer")==0 || GetLocalVarInt("GlandIsSqueezed")==1)
        	{
        		return;
        	}

        	if(alState == 1)
        	{
        		PlaySoundAtEntity("splouuutch", "26_squeeze_gland.snt", "AreaSqueezer", 0.0f, false);
        		PlaySoundAtEntity("liquidsound","26_liquid.snt", "AreaSqueezer", 2, false);

        		CreateParticleSystemAtEntity("squeezeeffect", "ps_squeeze_gland_effect.ps", "AreaSqueezer", false);

        		SetLocalVarInt("GlandIsSqueezed", 1);

        		AddTimer("SqueezeEffectDone", 2, nameof(TimerSqueezeEffectDone));
        	}
        }

        void TimerSqueezeEffectDone(string asTimer)
        {
        	AddTimer("fadeoutmix", 0.3f, nameof(TimerSqueezeEffectFadeOutMix));
        	StopSound("liquidsound", 1.0f);

        	//Check if the completed or incomplete mixture should be made active.
        	if(GetLocalVarInt("BoneIsCooked")==1)
        		SetPropActiveAndFade("glass_container_mix_done_1", true, 0.5f);
        	else
        		SetPropActiveAndFade("glass_container_mix_notdone_2", true, 0.5f);
        }

        void TimerSqueezeEffectFadeOutMix(string asTimer)
        {
        	SetPropActiveAndFade("glass_container_mix_notdone_static_1", false, 0.5f);
        }

        //------------------------------------------


        ///////////////////////////////////////////
        // COOK BONE
        ////////////////////////////////////////////


        //------------------------------------------

        void UseBloodOnOven(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseBloodOnOven), -1);
        }

        //------------------------------------------

        void UseMixOnOven(string asItem, string asEntity)
        {
        	//Mixture has alrady been placed once!
        	if(GetLocalVarInt("MixPlacedUnderOven")==1)
        	{
        		SetMessage("Ch03Level26", "UseMixOnOvenSecondTime", -1);
        		return;
        	}

        	PlaySoundAtEntity("placeglass", "26_place_glass", "AreaOvenNossel", 0.0f, false);

        	SetEntityActive("AreaOvenTable", false);//No more use of this area!

        	RemoveItem(asItem);

        	SetEntityActive("glass_container_mix_notdone_static_2", true);

        	SetLocalVarInt("MixPlacedUnderOven", 1);
        }

        //------------------------------------------

        void TimerOvenDoorImpulse(string asTimer)
        {
        	SetSwingDoorDisableAutoClose("extaction_oven_1", true);
        	SetSwingDoorClosed("extaction_oven_1", false, true);

        	AddPropImpulse("extaction_oven_1", -1, 0,0,"World");
        	AddTimer("push1", 0.1f, nameof(TimerPushAgain));
        }
        void TimerPushAgain(string asTimer)
        {
        	AddPropImpulse("extaction_oven_1", -1, 0,0,"World");
        }

        //------------------------------------------

        void InteractOven(string asEntity)
        {
        	if(GetLocalVarInt("MixPlacedUnderOven")==0 && GetLocalVarInt("BoneInOven")==1){
        		SetLeverStuckState(asEntity, -1, false);
        		SetMessage("Ch03Level26", "InteractOvenTooSoon", 0);
        		return;
        	}

        	SetLeverStuckState(asEntity, 0, false);
        }

        void UseBoneOnOven(string asItem, string asEntity)
        {
        	//Check if oven is running
        	if(GetLocalVarInt("OvenRunning")==1)
        	{
        		SetMessage("Ch03Level26", "OvenIsRunning", -1);
        		return;
        	}

        	//Check so tehre is a container below
        	if(GetLocalVarInt("MixPlacedUnderOven")==0)
        	{
        		//SetMessage("Ch03Level26", "NoContainerBelowOven", -1);
        		//return;
        	}

        	//Open door and make sure the player looks at it.
        	StartPlayerLookAt("fresh_meat_remains_static_1", 2, 5,"");

        	//Swing open the door, if closed.
        	if(GetSwingDoorState("extaction_oven_1")<1)
        	{
        		SetSwingDoorClosed("extaction_oven_1", false, true);
        		SetSwingDoorDisableAutoClose("extaction_oven_1", true);

        		AddTimer("OvenDoorImpulse", 0.1f, nameof(TimerOvenDoorImpulse));
        	}

        	AddTimer("PlaceBoneOvenDoorOpen", 0.6f, nameof(TimerPlaceBoneOvenDoorOpen));

        	SetLocalVarInt("BoneInOven", 1);
        }

        void TimerPlaceBoneOvenDoorOpen(string asTimer)
        {
        	StopPlayerLookAt();

        	SetSwingDoorDisableAutoClose("extaction_oven_1", false);

        	RemoveItem("fresh_meat_remains_1");
        	SetEntityActive("fresh_meat_remains_static_1", true);
        }

        //------------------------------------------

        void PullOvenLever(string asEntityName, int alState)
        {
        	if(alState != 1) return;

        	OvenHint();

        	SetLocalVarInt("OvenRunning", 1);

        	PlaySoundAtEntity("oven_run", "26_oven_run.snt", "extaction_oven_1", 0.5f, false);

        	SetLeverStuckState("extaction_oven_lever_1", 1, true);
        	SetSwingDoorLocked("extaction_oven_1", true, true);

        	//Bone is in oven and uncooked
        	if(GetLocalVarInt("BoneInOven")==1 && GetLocalVarInt("BoneIsCooked")==0)
        	{
        		AddTimer("OvenRunningOver", 4, nameof(TimerOvenRunningOver));

        		//Pour liquid
        		CreateParticleSystemAtEntity("ovenliquid", "ps_extract_oven_liquid.ps", "AreaOvenNossel", true);

        		//Pour liquid
        		PlaySoundAtEntity("ovenliquidsound","26_liquid.snt", "AreaOvenNossel", 3, true);
        	}
        	//Oven is empty
        	else
        	{
        		AddTimer("OvenRunningOver", 2, nameof(TimerOvenRunningOver));
        	}
        }

        void TimerOvenRunningOver(string asTimer)
        {
        	SetLocalVarInt("OvenRunning", 0);
        	SetLeverStuckState("extaction_oven_lever_1", 0, true);

        	StopSound("oven_run", 0.5f);

        	//Turn off the pouring and create the new mixture!
        	if(GetLocalVarInt("BoneInOven")==1 && GetLocalVarInt("BoneIsCooked")==0)
        	{
        		SetEntityActive("fresh_meat_remains_static_1", false);

        		DestroyParticleSystem("ovenliquid");
        		StopSound("ovenliquidsound",1);

        		SetLocalVarInt("BoneIsCooked",1);

        		//The container created depends on if gland is squeezed or not.
        		if(GetLocalVarInt("GlandIsSqueezed")==1)
        			SetPropActiveAndFade("glass_container_mix_done_2", true, 0.5f);
        		else
        			SetPropActiveAndFade("glass_container_mix_notdone_3", true, 0.5f);

        		AddTimer(nameof(TimerFadeOutOvenMix), 0.3f, nameof(TimerFadeOutOvenMix));
        	}

        	//Smoke comes out.
        	CreateParticleSystemAtEntity("ovensmoke", "ps_extract_oven_smoke.ps", "AreaInsideOven", false);

        	//Swing open the door!
        	SetSwingDoorLocked("extaction_oven_1", false, true);

        	SetSwingDoorClosed("extaction_oven_1", false, true);
        	SetSwingDoorDisableAutoClose("extaction_oven_1", true);

        	AddTimer("OvenDoorImpulse", 0.1f, nameof(TimerOvenDoorImpulse));
        	AddTimer("OvenDoorImpulse", 0.2f, nameof(TimerOvenDoorImpulse));

        	AddTimer("RunningDoneOvenDoorOpen", 0.6f, nameof(TimerRunningDoneOvenDoorOpen));
        }

        void TimerFadeOutOvenMix(string asTimer)
        {
        	SetPropActiveAndFade("glass_container_mix_notdone_static_2", false, 0.5f);
        }

        void TimerRunningDoneOvenDoorOpen(string asTimer)
        {
        	SetSwingDoorDisableAutoClose("extaction_oven_1", false);
        }

        void OvenHint()
        {
        	if(GetLocalVarInt(nameof(OvenHint)) != 0) return;
        	if(GetLocalVarInt("BoneInOven") != 0) return;
        	if(GetLocalVarInt("BoneIsCooked") !=0) return;

        	AddTimer("hintoven", 2.0f, nameof(TimerHintOven));

        	SetLocalVarInt(nameof(OvenHint), 1);
        }
        void TimerHintOven(string asTimer)
        {
        	SetMessage("Ch03Level26", nameof(InteractOven), 0);
        }
        //------------------------------------------

        ///////////////////////////////////////////
        // PICK COMPLETED MIXTURE
        ////////////////////////////////////////////

        //------------------------------------------

        void PickDoneMixture(string asEntity, string asType)
        {
        	GiveSanityBoost();
        	PlayMusic("15_puzzle_hole.ogg", false, 0.8f, 0.5f, 9, false);

        	CompleteQuest("21FoundTonicNote", "21FoundTonicNote");


        	SetEntityActive("AreaGuardianHowl_2", true);

        	SetLocalVarInt("MixtureDone",1);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // EXTRA MIXTURE CREATION TEXT FEEDBACK
        ////////////////////////////////////////////

        //------------------------------------------

        void UseGlandOnBurner(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseGlandOnBurner), -1);
        }

        void UseBoneOnBurner(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseBoneOnBurner), -1);
        }

        void UseBoneOnSqueezer(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseBoneOnSqueezer), -1);
        }

        void UseGlandOnOven(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseGlandOnOven), -1);
        }

        void UseDoneMixtureOnSqueezer(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseDoneMixtureOnSqueezer), -1);
        }

        void UseDoneMixtureOnOven(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseDoneMixtureOnOven), -1);
        }

        //------------------------------------------

        void UseGlassOnMachine(string asItem, string asEntity)
        {
        	SetMessage("BasicPhrases", "UseItemOrCombineItem02", -1);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // PLAYER FALLS INTO WELL
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideWellKillPlayer(string asParent, string asChild, int alState)
        {
        	CheckPoint("checkFallWell", "PlayerStartArea_1", nameof(CheckPointFallWell), "Hints", "DeathFall_21_TortureNave");

        	PlaySoundAtEntity("fallaaaaaah", "11_fall", "Player", 0, false);
        	PlaySoundAtEntity("fallaaaaaah2", "scare_male_terrified5", "Player", 0, false);

        	FadeOut(0.5f);

        	AddTimer("death1", 0.5f, nameof(TimerFallDeath));
        	AddTimer("death2", 1, nameof(TimerFallDeath));
        }

        void TimerFallDeath(string asTimer)
        {
        	//If first timer, just play a bump
        	if(asTimer == "death1"){
        		PlaySoundAtEntity("bump1", "player_bodyfall", "Player", 0, false);
        		return;
        	}

        	//Kill player without making a sound
        	DisableDeathStartSound();
        	AddPlayerHealth(-200);

        	//Lurker attacks player
        	PlaySoundAtEntity("LurkerScream", "water_lurker_attack.snt", "AreaRopeDown", 0.1f, false);

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
        // GUARDIAN HOWL
        ////////////////////////////////////////////

        //------------------------------------------

        void DoGuardianHowl()
        {
        	PlaySoundAtEntity("howl","guardian_distant3", "Player", 0, false);

        	AddDebugMessage("Howl!", false);

        	AddLocalVarInt("HowlCount", 1);

        	AddTimer("StopRadialBlur", 4.5f, nameof(TimerStopGuardianHowl));
        	AddTimer("react_scare", 0.5f, nameof(TimerPlayerPantReaction));
        	AddTimer("react_breath", 2.5f, nameof(TimerPlayerPantReaction));

        	StartScreenShake(0.03f, 0.0f, 2.0f, 4.0f);

        	SetRadialBlurStartDist(0.2f);
        	FadeRadialBlurTo(0.09f, 0.015f);
        }

        void TimerPlayerPantReaction(string asTimer)
        {
        	PlayGuiSound(asTimer, 0.7f);

        	if(asTimer == "react_scare") GiveSanityDamage(10.0f, true);
        }

        void TimerStopGuardianHowl(string asTimer)
        {
        	AddDebugMessage("Stopping howl blur.", false);
        	FadeRadialBlurTo(0, 0.02f);
        }

        //------------------------------------------

        void CollideGuardianHowl_1(string asParent, string asChild, int alState)
        {
        	DoGuardianHowl();
        }

        //------------------------------------------

        void CollideGuardianHowl_2(string asParent, string asChild, int alState)
        {
        	DoGuardianHowl();
        }

        //------------------------------------------

        void CollideGuardianHowl_3(string asParent, string asChild, int alState)
        {
        	DoGuardianHowl();
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
        	if(GetLocalVarInt("AgrippaDead")==1) return;

        	//Start speaking.
        	AgrippaNormalSpeak(true);
        }

        //------------------------------------------

        void CollideAgrippaStopTalk(string asParent, string asChild, int alState)
        {
        	//Only run function is inside talk range
        	if(GetLocalVarInt("PlayerInAgrippaTalkRange")==0) return;
        	SetLocalVarInt("PlayerInAgrippaTalkRange",0);

        	AddDebugMessage("OUTSIDE STOP TALK", false);

        	//If not awake, do nothing
        	if(GetLocalVarInt("AgrippaDead")==1) return;

        	StopAgrippaNormalSpeak();
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

        void PlayAgrippaNormalVoice(string asTopic, int alNumOfParts, bool abGreeting, bool abSetAsSpoken, string asPrefix)
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
        		string sSoundName = asPrefix+"_Agrippa_Nrm_Greeting_0"+RandInt(1,4);

        		AddEffectVoice(sSoundName+ ".ogg", "", "Voice", sSoundName, true, "channeling_machine_1", fMinDist, fMaxDist);
        	}

        	//Start all voices in topic
        	for(int i=1; i<=alNumOfParts; ++i)
        	{
        		string sNum = i<10 ? "0"+i : ""+i;
        		string sSoundName = asPrefix+"_Agrippa_Nrm_"+asTopic+"_"+sNum;
        		Print(sSoundName);
        		AddDebugMessage(sSoundName, false);
        		AddEffectVoice(sSoundName+ ".ogg", "", "Voice", sSoundName, true, "channeling_machine_1", fMinDist, fMaxDist);
        	}

        	//Callback when voices are done playing
        	SetEffectVoiceOverCallback(nameof(AgrippaNormalTalkOver));
        }

        //------------------------------------------

        //Starts a topic for Agrippa. Not that all topics are listed in an order of priority.
        //So the top once are started before the lower.
        void AgrippaNormalSpeak(bool abGreeting)
        {
        	//If Agrippa is dead, do nothing
        	if(GetLocalVarInt("AgrippaDead")==1) return;

        	//If there is an voice active, then wait adding.
        	//same if there is a flashback playing!
        	if(GetEffectVoiceActive() || GetFlashbackIsActive())
        	{
        		AgrippaNormalTalkOver();
        		return;
        	}

        	string sTopicName = "";
        	string sPrefix = "CH03L26";

        	//////////////////////////////////////////
        	// Return
        	//  - First thing spoken! When Daniel returns.
        	sTopicName = "Return";
        	if(AgrippaTopicNotSpoken(sTopicName))
        	{
        		//If player does not have all ingredient, skip the final line.
        		int lEntryCount = 3;
        		if(GetGlobalVarInt("IngredientCount")<3) lEntryCount = 2;

        		PlayAgrippaNormalVoice(sTopicName, lEntryCount, false, true,sPrefix);
        		return;
        	}

        	//////////////////////////////////////////
        	// Potion Done
        	//  - Potion has been fixed!
        	sTopicName = "PotionDone";
        	if(AgrippaTopicNotSpoken(sTopicName) && GetLocalVarInt("MixtureDone")==1)
        	{
        		PlayAgrippaNormalVoice(sTopicName, 6, false, true,sPrefix);
        		return;
        	}

        	//////////////////////////////////////////
        	// Potion not Done
        	//  - Potion is not done yet, play first time then make it random (so it is not repeated over and over)
        	sTopicName = "PotionNotStart";
        	if((AgrippaTopicNotSpoken(sTopicName) || RandInt(0,1)==0) && GetLocalVarInt("MixtureDone")==0)
        	{
        		//If player does not have all ingredient, skip the final line.
        		int lEntryCount = 2;
        		if(GetGlobalVarInt("IngredientCount")<3) lEntryCount = 1;

        		PlayAgrippaNormalVoice(sTopicName, lEntryCount, false, true,sPrefix);
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
        		// AngelScript aborts this call when the inherited chatter index is outside the array.
        		if(lNum < 1 || lNum > vRandomPartNum.Length) return;
        		int lPartNum = vRandomPartNum[lNum-1];//Index starts at 1!

        		string sNum = lNum<10 ? "0"+lNum : ""+lNum;
        		sTopicName = "Random"+sNum;

        		sPrefix = "CH02L21"; //Using voices from other chapter

        		/////////////////////
        		// Play voice
        		PlayAgrippaNormalVoice(sTopicName, lPartNum, false, false,sPrefix);//Always skip greeting here (compared to level 21!)

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
        // AGRIPPA GIVE TONIC
        ////////////////////////////////////////////

        //------------------------------------------


        void UseDoneMixOnAgrippa(string asItem, string asEntity)
        {
        	StopAgrippaNormalSpeak();

        	//Remove some topics to make sure agrippa does not speak them after been given potion.
        	SetAgrippaTopicHasSpoken("PotionDone");
        	SetAgrippaTopicHasSpoken("Return");

        	SetPlayerActive(false);
        	FadeOut(0.5f);

        	AddTimer("Drink", 0.6f, nameof(TimerDrinkSounds));

        	RemoveItem(asItem);

        	SetLocalVarInt("AgrippaGotTonic",1);

        	CompleteQuest("21FindTonic", "21FindTonic");
        }

        void TimerDrinkSounds(string asTimer)
        {
        	PlayGuiSound("26_agrippa_drink.ogg", 1.0f);

        	AddTimer("DrinkOver", 3.0f, nameof(TimerDrinkOverSounds));
        }

        void TimerDrinkOverSounds(string asTimer)
        {
        	SetPlayerActive(true);
        	FadeIn(1);

        	PlayAgrippaNormalVoice("PotionGiven", 2, false, true,"CH03L26");

        	AddQuest("26SawHead", "26SawHead");
        }

        //------------------------------------------

        void UseBloodOnAgrippa(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseBloodOnAgrippa),-1);
        }

        void UseNotdoneMixOnAgrippa(string asItem, string asEntity)
        {
        	SetMessage("Ch03Level26", nameof(UseNotdoneMixOnAgrippa),-1);
        }

        //------------------------------------------


        ///////////////////////////////////////////
        // AGRIPPA SAW HEAD
        ////////////////////////////////////////////

        //------------------------------------------

        void UseSawOnAgrippa(string asItem, string asEntity)
        {
        	//////////////////////////////////
        	// Agrippa not been given tonic yet
        	if(GetLocalVarInt("AgrippaGotTonic")==0)
        	{
        		StopAgrippaNormalSpeak();
        		PlayAgrippaNormalVoice("SawNoPotion", 1, false, true,"CH03L26");
        	}
        	//////////////////////////////////
        	// Agrippa has been given tonic
        	else
        	{
        		StopAgrippaNormalSpeak();

        		SetLocalVarInt("AgrippaDead",1);

        		SetPlayerActive(false);
        		FadeOut(0.5f);
        		AddTimer("SawSounds", 1, nameof(TimerSawSounds));

        		RemoveItem(asItem);

        		CompleteQuest("26SawHead", "26SawHead");
        	}
        }

        void TimerSawSounds(string asTimer)
        {
        	/////////////////////////////
        	// Change the entity and show head
        	SetEntityActive("agrippa_2", false);

        	SetEntityActive("block_box_nonchar_1", false);
        	SetEntityActive("block_box_nonchar_2", false);
        	SetEntityActive("block_box_nonchar_3", true);
        	SetEntityActive("block_box_nonchar_4", true);

        	SetEntityActive("agrippa_headless_1", true);
        	SetEntityActive("agrippa_head_1", true);

        	SetEntityActive("AreaAgrippa", false); //Turn off area so head can be picked up.

        	/////////////////////////////
        	// Play sounds
        	AddTimer("Saw", 0.0f, nameof(TimerSawSounds02)); AddTimer("Gurg", 0.2f, nameof(TimerSawSounds02)); AddTimer("Meat1", 0.8f, nameof(TimerSawSounds02));
        	AddTimer("Gurg", 1.0f, nameof(TimerSawSounds02)); AddTimer("Meat2", 1.6f, nameof(TimerSawSounds02)); AddTimer("Meat3", 1.8f, nameof(TimerSawSounds02));
        	AddTimer("Gurg", 2.4f, nameof(TimerSawSounds02)); AddTimer("Meat1", 2.6f, nameof(TimerSawSounds02)); AddTimer("Meat2", 2.8f, nameof(TimerSawSounds02));
        	AddTimer("Gurg", 2.9f, nameof(TimerSawSounds02)); AddTimer("Meat3", 3.0f, nameof(TimerSawSounds02));
        	AddTimer("Meat2", 3.1f, nameof(TimerSawSounds02)); AddTimer("Gurg", 3.4f, nameof(TimerSawSounds02)); AddTimer("Meat3", 3.6f, nameof(TimerSawSounds02));
        	AddTimer("Meat1", 3.9f, nameof(TimerSawSounds02)); AddTimer("Meat3", 4.1f, nameof(TimerSawSounds02));
        	AddTimer("Head", 4.5f, nameof(TimerSawSounds02));

        	AddTimer("SawStop", 5.0f, nameof(TimerSawSounds02));
        }
        void TimerSawSounds02(string asTimer)
        {
        	if(asTimer == "Meat1")
        		PlayGuiSound("21_meat_long.snt", 1);
        	if(asTimer == "Meat2")
        		PlayGuiSound("21_meat.snt", 1);
        	if(asTimer == "Meat3")
        		PlayGuiSound("21_meat_snap.snt", 1);

        	if(asTimer == "Saw")
        		PlayGuiSound("26_saw.snt", 1);
        		//PlayGuiSound("26_saw0"+RandInt(1,3)+".ogg", RandFloat(0.5f, 0.7f));
        	if(asTimer == "Gurg")
        		PlayGuiSound("CH03L26_Agrippa_SawOfHead0"+RandInt(1,8)+".ogg", RandFloat(0.3f, 0.6f));
        	if(asTimer == "Head")
        		PlayGuiSound("26_head_on_ground.ogg", 1);

        	if(asTimer == "SawStop")
        		SawSoundsOver();
        }

        //------------------------------------------

        void SawSoundsOver()
        {
        	SetPlayerActive(true);
        	FadeIn(1);
        	GiveSanityDamage(20, false);
        }

        //------------------------------------------

        //PUZZLE ITEM ON AGRIPPA
        void UseMiscOnAgrippa(string asItem, string asEntity)
        {
        	if(asItem == "fresh_meat_remains_1")
        		SetMessage("Ch02Level21", "AgrippaBoneNoNote", 0);
        	if(asItem == "fresh_meat_2")
        		SetMessage("Ch02Level21", "AgrippaMeatNoNote", 0);
        	if(asItem == "poison_gland")
        		SetMessage("Ch03Level26", "UseGlandOnAgrippa", 0);
        }

        void EntityCallAgrippaHead(string asEntity, string type)
        {
        	PlayMusic("26_event_agrippa_head.ogg", false, 1.0f, 1.0f, 10, false);
        	GiveSanityBoostSmall();
        }

        ///////////////////////////////////////////
        // INIT AGRIPPA
        ////////////////////////////////////////////

        //------------------------------------------

        void InitAgrippa()
        {
        	// Check if player has awakend agrippa in 21, if not make Agrippa dead.
        	if(GetGlobalVarInt("AgrippaActivatedIn21") != 1)
        	{
        		SetEntityActive("agrippa_2", false);
        		SetEntityActive("agrippa_headless_1", true);
        		SetEntityActive("AreaAgrippa", false); //Turn off no item use on corpse is possible.

        		StopSound("Sound_60", 0.0f); //Channeling machine
        		DestroyParticleSystem("ParticleSystem_62"); //Channeling machine

        		SetLocalVarInt("AgrippaDead",1);

        		SetEntityActive("bone_saw_1", false);

        		InitWaterLurker();
        	}
        	else
        		InitMissingIngredients();
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // ADD INTERACTION FOR WATER LURKER AS HE IS ACTIVE REGARDLESS OF PLAYER AND AGRIPPA STATES
        ////////////////////////////////////////////

        //------------------------------------------

        void InitWaterLurker()
        {
        	InteractConnectPropWithRope("WellRope","crank_wheel_1", "RopeArea_1", false, 3,5,5, false, 0);

        	AddUseItemCallback("MeatOnRope", "fresh_meat_2", "AreaRopeInteraction", nameof(UseMeatOnRope), false);
        	AddUseItemCallback("MeatOnRope", "fresh_meat_2", "crank_wheel_1", nameof(UseMeatOnRope), false);

        	AddUseItemCallback("BoneOnRope", "fresh_meat_remains_1", "crank_wheel_1", nameof(UseItemOnRope), false);
        	AddUseItemCallback("BoneOnRope", "fresh_meat_remains_1", "AreaRopeInteraction", nameof(UseItemOnRope), false);

        	AddUseItemCallback("GlandOnRope", "poison_gland", "AreaRopeInteraction", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlandOnRope", "poison_gland", "crank_wheel_1", nameof(UseItemOnRope), false);

        	AddUseItemCallback("GlassOnRope", "glass_container_1", "crank_wheel_1", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_1", "AreaRopeInteraction", nameof(UseItemOnRope), false);

        	AddUseItemCallback("GlassOnRope", "glass_container_mix_done_1", "crank_wheel_1", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_mix_done_1", "AreaRopeInteraction", nameof(UseItemOnRope), false);

        	AddUseItemCallback("GlassOnRope", "glass_container_mix_notdone_1", "AreaRopeInteraction", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_mix_notdone_1", "crank_wheel_1", nameof(UseItemOnRope), false);

        	AddUseItemCallback("GlassOnRope", "glass_container_blood", "crank_wheel_1", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_blood", "AreaRopeInteraction", nameof(UseItemOnRope), false);

        	AddEntityCollideCallback("invisible_box_mass_3", "AreaRopeInteraction",nameof(CollideRopeInteraction), false, 0);
        	AddEntityCollideCallback("invisible_box_mass_3", "AreaRopeDown",nameof(CollideRopeDown), false, 0);

        	AddUseBucketToRope();
        }

        //------------------------------------------


        ///////////////////////////////////////////
        // MISSING INGREDIENTS - GENERAL
        ////////////////////////////////////////////

        //------------------------------------------

        void InitMissingIngredients()
        {
        	/////////////////////////////////
        	// Posion Gland
        	if(HasItem("poison_gland")==false)
        	{
        		SetEntityActive("poison_fungi_breakable_1", true);

        		SetEntityPlayerInteractCallback("AreaFungus", nameof(descFungus), true);

        		AddUseItemCallback("hammer_chipper_on_fungi", "stone_hammer_chipper", "poison_fungi_breakable_1", nameof(UseChipperOnFungi), false);
        		AddEntityCollideCallback("Player", "poison_fungi_breakable_1", nameof(CollidePlayerWithFungi), false, 1);
        	}

        	/////////////////////////////////
        	// Blood
        	if(HasItem("glass_container_blood")==false)
        	{
        		//Add string if it is missing
        		if(HasItem("string_pile_2")==false && HasItem("container_and_string")==false)
        		{
        			SetEntityActive("string_pile_2", true);
        			FadeLightTo("PointLight_23", 0.3f, 0.5f, 0.8f,0.5f, -1, 1);
        		}

        		SetEntityPlayerInteractCallback("hatch_drainage_2", nameof(InteractHatchDrainage), true);

        		//Area active
        		SetEntityActive("AreaBloodHole", true);

        		//Light up the hole
        		FadeLightTo("PointLight_20", 0.5f,0.3f,0.3f,0.5f, -1,1);
        		FadeLightTo("PointLight_24", 0.8f,0.1f,0.1f,0.5f, -1,1);

        		//Add collide areas
        		AddUseItemCallback("container_on_blood", "glass_container_1", "AreaBloodHole", nameof(UseContainerOnBlood), false);
        		AddUseItemCallback("container_on_blood", "glass_container_1", "hatch_drainage_2", nameof(UseContainerOnHatch), false);

        		AddUseItemCallback("container_n_string_on_blood","container_and_string", "AreaBloodHole", nameof(UseContainerAndStringOnBlood), false);
        		AddUseItemCallback("container_n_string_on_blood", "container_and_string", "hatch_drainage_2", nameof(UseContainerAndStringOnHatch), false);
        	}


        	/////////////////////////////////
        	// Bone with secrete

        	//Always set up this!
        	InteractConnectPropWithRope("WellRope","crank_wheel_1", "RopeArea_1", false, 3,5,5, false, 0);
        	//AddEntityCollideCallback("crank_wheel_1", "CrankSound_2", "CollideCrankSound", false, 1); <- probably not proper for crank type.
        	//AddEntityCollideCallback("crank_wheel_1", "CrankSound_1", "CollideCrankSound", false, 1); <- probably not proper for crank type.

        	AddUseItemCallback("GlassOnRope", "glass_container_1", "AreaRopeInteraction", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_mix_done_1", "AreaRopeInteraction", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_mix_notdone_1", "AreaRopeInteraction", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_blood", "AreaRopeInteraction", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlandOnRope", "poison_gland", "AreaRopeInteraction", nameof(UseItemOnRope), false);
        	AddUseItemCallback("BoneOnRope", "fresh_meat_remains_1", "AreaRopeInteraction", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_1", "crank_wheel_1", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_mix_done_1", "crank_wheel_1", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_mix_notdone_1", "crank_wheel_1", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlassOnRope", "glass_container_blood", "crank_wheel_1", nameof(UseItemOnRope), false);
        	AddUseItemCallback("GlandOnRope", "poison_gland", "crank_wheel_1", nameof(UseItemOnRope), false);
        	AddUseItemCallback("BoneOnRope", "fresh_meat_remains_1", "crank_wheel_1", nameof(UseItemOnRope), false);

        	//Special set up if player does not yet have items
        	if(HasItem("fresh_meat_remains_1")==false)
        	{
        		if(HasItem("fresh_meat_2")==false)
        		{
        			SetEntityActive("fresh_meat_2", true);
        		}

        		AddUseItemCallback("MeatOnRope", "fresh_meat_2", "AreaRopeInteraction", nameof(UseMeatOnRope), false);
        		AddUseItemCallback("MeatOnRope", "fresh_meat_2", "crank_wheel_1", nameof(UseMeatOnRope), false);

        		AddUseItemCallback("MeatOnAgrippa", "fresh_meat_2", "AreaAgrippa_1", nameof(UseStuffOnAgrippa), false);
        		AddUseItemCallback("BoneOnAgrippa", "fresh_meat_remains_1", "AreaAgrippa_1", nameof(UseStuffOnAgrippa), false);

        		AddEntityCollideCallback("invisible_box_mass_3", "AreaRopeInteraction",nameof(CollideRopeInteraction), false, 0);
        		AddEntityCollideCallback("invisible_box_mass_3", "AreaRopeDown",nameof(CollideRopeDown), false, 0);

        		//Add bucket use too if player has one
        		if(HasItem("wooden_bucket_filled_1") || HasItem("wooden_bucket_1"))
        			AddUseBucketToRope();
        	}

        	//Has bucket, but also fresh remains
        	if(HasItem("fresh_meat_remains_1") && (HasItem("wooden_bucket_filled_1") || HasItem("wooden_bucket_1")))
        	{
        		AddEntityCollideCallback("invisible_box_mass_3", "AreaRopeInteraction",nameof(CollideRopeInteraction), false, 0);
        		AddEntityCollideCallback("invisible_box_mass_3", "AreaRopeDown",nameof(CollideRopeDown), false, 0);

        		AddUseBucketToRope();
        	}


        	/////////////////////////////////
        	// Weyer Note
        	if(GetGlobalVarInt("WeyerNoteIsPicked")==0)
        	{
        		SetEntityActive("note_generic_1", true);
        	}
        }

        //------------------------------------------

        ///////////////////////////////////////////
        //  MISSING INGREDIENTS - BONE WITH SECRETE
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

        	if(GetLocalVarInt("BucketOnRope")==1) DoBucketCrush();

        	if(GetLocalVarInt("MeatOnRope")!=1) return;

        	SetLocalVarInt("MeatOnRope",0);

        	PlayMusic("21_event_pit.ogg", false, 1.0f, 0.0f, 10, false);

        	//Remove normal meat for the eaten one.
        	RemoveAttachedPropFromProp("invisible_box_mass_3", "fresh_meat_onrope_1");

        	//Make it possible to pick up bone + have correct icon
        	SetEntityPlayerInteractCallback("invisible_box_mass_3", nameof(InteractSlimeBone), true);
        	SetEntityCustomFocusCrossHair("invisible_box_mass_3","Pick");

        	//need to turn off this area, else it is not possible to interact with entity on rope!
        	SetEntityActive("AreaRopeInteraction", false);

        	//Lever get stuck for a while, hinting that the water lurker is tugging on it.
        	SetWheelStuckState("crank_wheel_1", 1, true);
        	SetEntityPlayerInteractCallback("crank_wheel_1",nameof(InteractStuckWellLever), false);

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
        	AddPropImpulse("invisible_box_mass_3", RandFloat(-2,2), RandFloat(1,3), RandFloat(-2,2), "World");
        }

        void TimerLurkerEatRopeImpulseOnly(string asTimer)
        {
        	AddPropImpulse("invisible_box_mass_3", RandFloat(-1,1), 0, RandFloat(-1,1), "World");
        }


        void TimerStopLookAtWheel(string asTimer)
        {
        	StopPlayerLookAt();
        }

        void TimerLurkerDoneEating(string asTimer)
        {
        	//Reset stuck callback
        	SetWheelStuckState("crank_wheel_1", 0, true);
        	SetEntityPlayerInteractCallback("crank_wheel_1","", false);

        	//Add the remains.
        	AddAttachedPropToProp("invisible_box_mass_3", "fresh_meat_remains_onrope_1", "fresh_meat_remains_onrope.ent", 0,0,0, 0,0,0);


        	//Okay to start ambience again
        	StartWaterLurkerAmbience();
        }

        //------------------------------------------

        void InteractSlimeBone(string asEntity)
        {
        	RemoveAttachedPropFromProp("invisible_box_mass_3", "fresh_meat_remains_onrope_1");
        	GiveItemFromFile("fresh_meat_remains_1", "fresh_meat_remains.ent");

        	//Increase count.
        	AddGlobalVarInt("IngredientCount",1);

        	//Need to do like this or else a message is not shown!
        	SetMessage("Ch02Level21", "PickUpFreshMeatRemains", 0);

        	//aplay pick sound
        	PlaySoundAtEntity("PickUpItem", "pick_meat.snt", "Player", 0.1f, false);

        	//Give a sanity boost.
        	GiveSanityBoostSmall();
        	PlayMusic("15_puzzle_hole.ogg", false, 0.8f, 0.5f, 9, false);

        	//Too allow bucket to be placed
        	SetLocalVarInt("RopeIsUp", 1);
        	SetEntityActive("AreaRopeInteraction", true);
        }

        //------------------------------------------

        void UseMeatOnRope(string asItem, string asEntity)
        {
        	//////////////////////////////
        	//Rope is in right position
        	if(GetLocalVarInt("RopeIsUp")==1)
        	{
        		AddAttachedPropToProp("invisible_box_mass_3", "fresh_meat_onrope_1", "fresh_meat_onrope.ent", 0,0,0, 0,0,0);
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

        //-------------------------------

        //Just some misc usage
        void UseStuffOnAgrippa(string asItem, string asEntity)
        {
        	if(asItem == "fresh_meat_2")		SetMessage("Ch02Level21", "AgrippaMeatNote", 0);
        	else if(asItem == "fresh_meat_remains_1")SetMessage("Ch02Level21", "AgrippaMeatNote", 0);
        }

        void UseItemOnRope(string asItem, string asEntity)
        {
        	if(asItem == "glass_container_1")
        		SetMessage("Ch03Level26", "UseGlassOnRope", 0);
        	else if(asItem == "fresh_meat_remains_1")
        		SetMessage("Ch03Level26", "UseBoneOnRope", 0);
        	else
        		SetMessage("Ch03Level26", "UseGlandOnRope", 0);
        }
        //-------------------------------

        ////////////////////////////
        // WATER LURKER AMBIENCE
        /////////////////////////

        //------------------------------------------

        void CollideStartLurkerSounds(string asParent, string asChild, int alState)
        {
        	StartWaterLurkerAmbience();
        }

        //------------------------------------------

        void CollideStopLurkerSounds(string asParent, string asChild, int alState)
        {
        	StopWaterLurkerAmbience();
        }

        //------------------------------------------

        void StartWaterLurkerAmbience()
        {
        	if(GetLocalVarInt("WaterLukrerAmbActive")==1) return;

        	SetLocalVarInt("WaterLukrerAmbActive",1);
        	AddTimer("WaterLurkerAmbience", RandFloat(3,7), nameof(TimerWaterLurkerAmbience));

        	AddDebugMessage("Start Water lurker Amb.", false);
        }

        void StopWaterLurkerAmbience()
        {
        	if(GetLocalVarInt("WaterLukrerAmbActive")==0) return;

        	SetLocalVarInt("WaterLukrerAmbActive",0);
        	RemoveTimer("WaterLurkerAmbience");

        	AddDebugMessage("Stop Water lurker Amb.", false);
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


        ////////////////////////////
        // MISSING INGREDIENTS - POISON FUNGI
        /////////////////////////

        //-------------------------------
        void descFungus(string asEntity)
        {
        	SetMessage("Ch02Level24", "DescFungus", 0);
        }

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

        	RemoveItem("stone_hammer_chipper");	//Remove hammer, chipper as no more use
        }

        //-------------------------------

        void PosionFungiBreak(string asEntity, string asType)
        {
        	SetEntityActive("poison_gland", true);

        	FadeLightTo("PointLight_12", 0, 0.6f, 0.8f, 0.8f, -1, 2);//Add extra glow to posion gland.

        	PlayGuiSound("26_break_fungu.ogg", 0.8f);

        	AddDebugMessage("Broke the fungi!", false);
        }

        //-------------------------------

        // Player picks up poison gland
        void PickPoisonGland(string asEntityName, string asType)
        {
        	AddDebugMessage("Picked up poison gland, incrementing ingredient count", false);

        	FadeLightTo("PointLight_12", 0,0,0,0, -1, 2);

        	AddLocalVarInt("IngredientCount", 1);

        	GiveSanityBoostSmall();
        	PlayMusic("15_puzzle_hole.ogg", false, 0.8f, 0.5f, 9, false);

        	RemoveItem("stone_hammer_chipper");	//Remove hammer, chipper as no more use - here incase player did not use it to break
        }

        //-------------------------------

        ///////////////////////////////
        // MISSING INGREDIENTS - BLOOD
        ///////////////////////////////

        //Hint for interacting on hatch
        void InteractHatchDrainage(string asEntity)
        {
        	SetMessage("Ch02Level23", "SignGrate", -1);
        }

        //-------------------------------

        void PickUpStringPile(string asEntityName, string asType)
        {
        	FadeLightTo("PointLight_23", 0, 0, 0,0, -1, 2);
        }

        //-------------------------------


        void InteractBloodHole(string asEntity)
        {
        	SetMessage("Ch02Level23", nameof(InteractBloodHole), -1);
        }

        //------------------------------------------

        //The player needs to have a string tied to the container.
        void UseContainerOnBlood(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level23", "UseContainerOnBloodHole", -1);
        	AddQuest("23NeedString", "23NeedString");
        }

        void UseContainerOnHatch(string asItem, string asEntity)
        {
        	//Hatch is open
        	if(GetLeverState(asEntity)==1)
        	{
        		UseContainerOnBlood(asItem, asEntity);
        	}
        	//Hatch is closed
        	else
        	{
        		SetMessage("Ch02Level23", "UseContainerOnClosedHatch", -1);
        	}
        }

        //------------------------------------------

        void UseContainerAndStringOnBlood(string asItem, string asEntity)
        {
        	SetPlayerCrouching(true);				//Make sure player sits down.
        	StartPlayerLookAt("AreaBloodHole", 2, 4, "");		//Look at the hole

        	RemoveItem("container_and_string");
        	AddTimer("bloodevent1", 0.3f, nameof(TimerGetBloodEvent));

        	SetEntityActive("AreaBloodHole", false);
        }

        void UseContainerAndStringOnHatch(string asItem, string asEntity)
        {
        	//Hatch is open
        	if(GetLeverState(asEntity)==1)
        	{
        		UseContainerAndStringOnBlood(asItem, asEntity);
        	}
        	//Hatch is closed
        	else
        	{
        		SetMessage("Ch02Level23", "UseContainerOnClosedHatch", -1);
        	}
        }


        void TimerGetBloodEvent(string asTimer)
        {
        	if(asTimer == "bloodevent1")
        	{
        		SetPropActiveAndFade("glass_container_string_1", true, 0.5f);

        		PlayGuiSound("26_put_blood.ogg", 0.8f);

        		AddTimer("bloodevent2", 0.5f, nameof(TimerGetBloodEvent));
        	}
        	if(asTimer == "bloodevent2")
        	{
        		PlayGuiSound("26_fill_blood.ogg", 0.8f);

        		AddTimer("bloodevent3", 1.5f, nameof(TimerGetBloodEvent));
        	}
        	if(asTimer == "bloodevent3")
        	{
        		PlayGuiSound("26_pick_blood.ogg", 0.8f);

        		SetPropActiveAndFade("glass_container_string_1", false, 0.5f);

        		GiveItem("glass_container_blood", "Puzzle", "glass_container_blood", "glass_container_blood.tga", 0);
        		StopPlayerLookAt();

        		//Increase ingredient counter!
        		AddGlobalVarInt("IngredientCount",1);

        		GiveSanityBoostSmall();
        		PlayMusic("15_puzzle_hole.ogg", false, 0.8f, 0.5f, 9, false);
        		CompleteQuest("23NeedString", "23NeedString");
        	}
        }

        /////////////////////////////////////
        // MISSING INGREDIENTS - WEYER NOTE
        ////////////////////////////////////

        //-------------------------------

        void PickupChannelWeyerNote(string asEntity, string asType)
        {
        	PlayMusic("01_puzzle_passage.ogg", false, 1.0f, 0, 10, false);

        	SetGlobalVarInt("WeyerNoteIsPicked", 1);

        	AddQuest("21FoundTonicNote", "21FoundTonicNote");
        	CompleteQuest("21FindWeyerNote", "21FindWeyerNote");
        }

        //-------------------------------

        ///////////////////////////////////////////
        // PICK UP DIARY
        ////////////////////////////////////////////

        //------------------------------------------

        void PickUpDiary(string asDiary, int alNum)
        {
        	ReturnOpenJournal(false);

        	PlayMusic("26_paper_daniel01.ogg", false, 0.7f, 0, 10, false);

        	AddTimer(nameof(TimerFlashBackEvents), 0.01f, nameof(TimerFlashBackEvents));
        }

        void TimerFlashBackEvents(string asTimer)
        {
        	int lEventNum = GetLocalVarInt("FlashbackEventNum");
        	AddLocalVarInt("FlashbackEventNum",1);
        	float fNextEventTime = 1.0f;


        	/////////////////////
        	// 0: Start up
        	if(lEventNum == 0)
        	{
        		SetLanternDisabled(true);
        		SetPlayerLookSpeedMul(0.8f);
        		SetInventoryDisabled(true);
        		StartEffectFlash(0.1f, 0.1f, 0.25f);
        		SetPlayerRunSpeedMul(0.6f);
        		SetPlayerMoveSpeedMul(0.75f);
        		SetPlayerJumpDisabled(true);
        		SetPlayerCrouchDisabled(true);

        		PlaySoundAtEntity("SoundFlash1", "scare_thump_flash.snt", "Player", 0.0f, false);
        		PlaySoundAtEntity("creak", "03_creak.snt", "Player", 4.0f, false);
        		PlaySoundAtEntity("Breath1", "react_breath.snt", "Player", 1.0f, false);

        		fNextEventTime = 0.3f;
        	}
        	/////////////////////
        	// 1
        	else if(lEventNum == 1)
        	{
        		SetPlayerLookSpeedMul(0.6f);
        		StartScreenShake(0.01f, 0.75f, 0.25f,1.5f);
        		FadePlayerRollTo(60, 5, 5);
        		FadeImageTrailTo(1.2f, 2);
        		MovePlayerHeadPos(0, -1.2f, 0, 0.3f, 0.25f);

        		fNextEventTime = 2.0f;
        	}
        	/////////////////////
        	// 2
        	else if(lEventNum == 2)
        	{
        		SetPlayerLookSpeedMul(0.4f);
        		FadePlayerRollTo(-60, 10, 10);

        		MovePlayerHeadPos(0, 0, 0, 0.2f, 0.25f);
        		SetPlayerRunSpeedMul(0.3f);
        		SetPlayerMoveSpeedMul(0.5f);
        		PlaySoundAtEntity("Breath1", "react_breath.snt", "Player", 1.1f, false);

        		fNextEventTime = 1.75f;
        	}
        	/////////////////////
        	// 3
        	else if(lEventNum == 3)
        	{
        		SetPlayerLookSpeedMul(0.2f);

        		FadeOut(4);

        		FadePlayerRollTo(0, 10, 10);
        		MovePlayerHeadPos(0, -0.7f, 0, 1.0f, 0.25f);
        		StartPlayerLookAt("AreaFlashLookUp", 1, 1, "");
        		PlayGuiSound("player_bodyfall.snt", 0.8f);
        		SetPlayerRunSpeedMul(0);
        		SetPlayerMoveSpeedMul(0.3f);
        		PlaySoundAtEntity("Breath1", "react_breath.snt", "Player", 1.0f, false);

        		fNextEventTime = 3.0f;
        	}

        	else if(lEventNum == 4)
        	{
        		PlaySoundAtEntity("Breath1", "react_sigh.snt", "Player", 0.9f, false);

        		fNextEventTime = 2.0f;
        	}

        	/////////////////////
        	// 5 Change map
        	else if(lEventNum == 5)
        	{
        		FadeImageTrailTo(0, 1);

        		SetPlayerLookSpeedMul(1.0f);
        		MovePlayerHeadPos(0, 0, 0, 1.0f, 0.25f);
        		StopPlayerLookAt();

        		SetupLoadScreen("LoadingText", "Ch03_Zimmerman01", 1, "game_loading_rose.jpg");

        		//Create cache to enable faster loading when going back to the map
        		CreateDataCache();

        		SetPlayerJumpDisabled(false);
        		SetPlayerCrouchDisabled(false);

        		ChangeMap("26_zimmerman_vision.map","PlayerStartArea_1","","");
        		return;
        	}


        	AddTimer(nameof(TimerFlashBackEvents), fNextEventTime, nameof(TimerFlashBackEvents));
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // BACK FROM ZIMMERMAN EVENTS
        ////////////////////////////////////////////

        //------------------------------------------

        void TimerAfterZimmermanEffects(string asTimer)
        {
        	AddTimer("wake1", 0.1f, nameof(TimerWakeEvents));
        	AddTimer("wake2", 0.8f, nameof(TimerWakeEvents));
        	AddTimer("wake3", 1.0f, nameof(TimerWakeEvents));
        	AddTimer("wake4", 1.6f, nameof(TimerWakeEvents));
        	AddTimer("wake5", 2.4f, nameof(TimerWakeEvents));
        	AddTimer("wake6", 3.4f, nameof(TimerWakeEvents));
        	AddTimer("wake7", 4.0f, nameof(TimerWakeEvents));
        	AddTimer("wake8", 5.0f, nameof(TimerWakeEvents));

        	AddTimer("AreaFlashLookUp_1", 0.1f, nameof(TimerShowPetals));
        	AddTimer("AreaFlashLookUp_2", 0.8f, nameof(TimerShowPetals));
        	AddTimer("AreaFlashLookUp_1", 1.5f, nameof(TimerShowPetals));
        	AddTimer("AreaFlashLookUp_1", 2.5f, nameof(TimerShowPetals));
        	AddTimer("AreaFlashLookUp_2", 3.5f, nameof(TimerShowPetals));
        }

        //------------------------------------------

        void TimerShowPetals(string asTimer)
        {
        	CreateParticleSystemAtEntity("petals1", "ps_rose_petals.ps", asTimer, false);
        }

        //------------------------------------------
        void TimerWakeEvents(string asTimer)
        {
        	if(asTimer == "wake1"){
        		PlayGuiSound("general_wind_whirl.snt", 0.5f);
        		PlayGuiSound("react_sigh", 0.5f);
        		StartPlayerLookAt("AreaFlashLookUp_2", 0.1f, 0.1f, "");
        		FadePlayerRollTo(-20, 0.1f, 0.1f);
        		ChangePlayerProp(0.2f);
        	}
        	if(asTimer == "wake2"){
        		ChangePlayerProp(0.3f);
        	}
        	if(asTimer == "wake3"){
        		StartPlayerLookAt("AreaFlashLookUp_1", 0.3f, 0.3f, "");
        		ChangePlayerProp(0.4f);
        	}
        	if(asTimer == "wake4"){
        		FadePlayerRollTo(20, 1.0f, 0.0f);
        		ChangePlayerProp(0.5f);
        	}
        	if(asTimer == "wake5"){
        		FadePlayerRollTo(-20, 1.0f, 0.0f);
        		StartPlayerLookAt("AreaFlashLookUp", 1.0f, 1.0f, "");
        		MovePlayerHeadPos(0.0f, 0.0f, 0.0f, 1.0f, 0.5f);
        		PlayGuiSound("player_climb7.ogg", 0.8f);
        		ChangePlayerProp(0.6f);
        	}
        	if(asTimer == "wake6"){
        		StartPlayerLookAt("AreaFlashLookUp_3", 4.0f, 4.0f, "");
        		FadePlayerRollTo(0, 1.0f, 0.0f);
        		PlayGuiSound("player_crouch.snt", 0.6f);
        		MovePlayerHeadPos(0.0f, -0.5f, 0.0f, 0.5f, 0.5f);
        		ChangePlayerProp(0.7f);
        	}
        	if(asTimer == "wake7"){
        		StopPlayerLookAt();
        		ChangePlayerProp(0.8f);
        	}
        	if(asTimer == "wake8"){
        		PlayGuiSound("player_stand.snt", 0.8f);
        		MovePlayerHeadPos(-0.0f, 0.0f, 0.0f, 0.5f, 0.1f);
        		ChangePlayerProp(1.0f);
        		SetPlayerCrouchDisabled(false);
        		SetPlayerJumpDisabled(false);
        		FadeRadialBlurTo(0.0f, 0.1f);

        		AddTimer("lookloop", 2, nameof(TimerRandomLook));
        	}
        }

        void ChangePlayerProp(float fSpeed)
        {
        	SetPlayerMoveSpeedMul(fSpeed);
        	SetPlayerLookSpeedMul(fSpeed);
        	SetPlayerRunSpeedMul(fSpeed);
        }

        //------------------------------------------

        /////////////////////////////////////
        //SPIN HEAD
        void CollideAreaStopHead(string asParent, string asChild, int alState)
        {
        	StopRandomLook();
        }
        void CollideAreaFaintHead(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("00", "00_creak.snt", "Player", 1.0f, false);
        	StartScreenShake(0.02f, 1.0f, 2.0f, 2.0f);
        	FadeOut(8.0f);
        	MovePlayerHeadPos(0, -1, 0, 0.1f, 0.1f);
        	AddTimer("stopevent", 4.0f, nameof(TimerStopFaint));
        }
        void TimerStopFaint(string asTimer)
        {
        	FadeIn(4.0f);
        	StopSound("00", 6.0f);
        	MovePlayerHeadPos(0, 0, 0, 1, 1);
        }

        bool bRoll = true;
        void TimerRandomLook(string asTimer)
        {
        	int iLook = (int)RandFloat(1,5);

        	if(iLook > 4){
        		SetPlayerRunSpeedMul(0.8f);
        		SetPlayerMoveSpeedMul(0.9f);
        		FadePlayerRollTo(0, 0.5f, 1);
        		FadePlayerFOVMulTo(1, 1);
        		FadeImageTrailTo(0,1.5f);

        		AddTimer("lookloop", RandFloat(6.0f,8.0f), nameof(TimerRandomLook));
        		return;
        	}

        	if(iLook == 1 || iLook == 3)
        		PlaySoundAtEntity("sigh", "react_sigh.snt", "Player", 1.0f / 0.75f, false);

        	FadePlayerFOVMulTo(RandFloat(0.7f,1.3f), RandFloat(0.05f,0.1f));

        	SetPlayerMoveSpeedMul(RandFloat(0.7f,0.8f));
        	SetPlayerRunSpeedMul(RandFloat(0.8f,0.9f));

        	FadeImageTrailTo(RandFloat(0.75f,1.0f),RandFloat(0.75f,1.25f));

        	bRoll = bRoll == false ? true : false;

        	if(bRoll)
        		FadePlayerRollTo(RandFloat(4,12), RandFloat(0.075f,0.35f), RandFloat(0.55f,1.15f));
        	else
        		FadePlayerRollTo(RandFloat(-4,-12), RandFloat(0.05f,0.25f), RandFloat(0.5f,1));

        	AddTimer("lookloop", RandFloat(3.0f,6.0f), nameof(TimerRandomLook));
        }
        void StopRandomLook()
        {
        	RemoveTimer("lookloop");

        	SetPlayerMoveSpeedMul(1);
        	FadePlayerFOVMulTo(1, 1);
        	SetPlayerRunSpeedMul(1);
        	FadeImageTrailTo(0,1.0f);
        	FadePlayerRollTo(0, 0.5f, 2);
        }
        ////////////////////////////////////////
        ///////////////////////////////////////

        //guaridan amb
        void TimerGuardianAmb(string asTimer)
        {
        	PlaySoundAtEntity(asTimer+RandInt(1,6), "26_guardian_idle.snt", asTimer+RandInt(1,6), 0.0f, false);
        	StartScreenShake(0.003f, 1.0f, 1.5f, 2.5f);

        	AddTimer("GuardianAmb_", RandFloat(15.0f, 30.0f), nameof(TimerGuardianAmb));
        }

        //Locked always door
        void InteractLockedDoor(string asEntity)
        {
        	SetMessage("Ch02Level16", "InteractLargeDoor", 0);

        	//PlayGuiSound("locked_door", 0.7f);

        	AddTimer(asEntity, 1.0f, nameof(TimerInteractLockedDoor));
        }
        void TimerInteractLockedDoor(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractLockedDoor), true);
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


        /////////////////////////////////////////////
        //BUCKET ON ROPE

        //Use bucket on rope setup
        void AddUseBucketToRope()
        {
        	AddUseItemCallback("BucketFilledOnRope1", "wooden_bucket_filled_1", "AreaRopeInteraction", nameof(UseBucketOnRope), false);
        	AddUseItemCallback("BucketFilledOnRope2", "wooden_bucket_filled_1", "crank_wheel_1", nameof(UseBucketOnRope), false);
        	AddUseItemCallback("BucketOnRope1", "wooden_bucket_1", "AreaRopeInteraction", nameof(UseBucketOnRope), false);
        	AddUseItemCallback("BucketOnRope2", "wooden_bucket_1", "crank_wheel_1", nameof(UseBucketOnRope), false);
        }


        //Bucket on rope
        void UseBucketOnRope(string asItem, string asEntity)
        {
        	//////////////////////////////
        	//Rope is in right position
        	if(GetLocalVarInt("RopeIsUp")==1)
        	{
        		if(asItem == "wooden_bucket_filled_1")
        			AddAttachedPropToProp("invisible_box_mass_3", "wooden_bucket_onrope_filled_1", "wooden_bucket_onrope_filled.ent", 0,-0.1f,0, 0,0,0);
        		else
        			AddAttachedPropToProp("invisible_box_mass_3", "wooden_bucket_onrope_1", "wooden_bucket_onrope.ent", 0,-0.1f,0, 0,0,0);

        		RemoveItem(asItem);

        		SetLocalVarInt("BucketOnRope", 1);

        		PlaySoundAtEntity("attach", "25_attach_bucket.snt", "AreaRopeInteraction", 0.0f, false);
        	}
        	//////////////////////////////
        	//Rope is too low
        	else
        	{
        		SetMessage("Ch02Level21", "RopeTooLow", 0);
        	}
        }

        //Crush the bucket when done
        void DoBucketCrush()
        {
        	PlaySoundAtEntity("LurkerScream", "water_lurker_eat_rev.snt", "AreaWellSounds", 0.1f, false);

        	RemoveAttachedPropFromProp("invisible_box_mass_3", "wooden_bucket_onrope_1");
        	RemoveAttachedPropFromProp("invisible_box_mass_3", "wooden_bucket_onrope_filled_1");

        	AddTimer("breakbucket", 0.5f, nameof(TimerBreakBucket));

        	SetLocalVarInt("BucketOnRope", 0);
        }
        void TimerBreakBucket(string asTimer)
        {
        	AddPropImpulse("invisible_box_mass_3", RandFloat(-2,2), RandFloat(1,3), RandFloat(-2,2), "World");
        	PlaySoundAtEntity("BreakBucket", "break_wood.snt", "AreaWellSounds", 0.1f, false);
        	PlayerReact(true,0.5f);
        }


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("Nave");

        	ClearSavedMaps();

        	AddTimer("GuardianAmb_", RandFloat(0.0f,8.0f), nameof(TimerGuardianAmb));


        	///////////////////////////
        	// Load screen setup
        	// (need to have here in case the zimmerman note is not picked up!)
        	SetupLoadScreen("LoadingText", "Ch03_Diary08_", 6, "game_loading_girl.jpg");

        	///////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		SetGlobalVarInt("AgrippaActivatedIn21", 1);	//Agrippa is awake in 26 too

        		GiveItemFromFile("lantern", "lantern.ent");
        		for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

        		//GiveItemFromFile("fresh_meat_remains_1", "fresh_meat_remains.ent");
        		//GiveItemFromFile("poison_gland", "poison_gland.ent");
        		//GiveItem("glass_container_blood", "Puzzle", "GlassContainerBlood", "glass_container_blood.tga", 0);
        		//GiveItemFromFile("glass_container_1", "glass_container.ent");
        		//GiveItemFromFile("fresh_meat_2", "fresh_meat.ent");

        		//GiveItemFromFile("wooden_bucket_filled_1", "wooden_bucket_filled.ent");
        		//GiveItemFromFile("wooden_bucket_1", "wooden_bucket.ent");

        		//SetGlobalVarInt("IngredientCount",3);
        		//SetGlobalVarInt("WeyerNoteIsPicked", 1);

        		//GiveItemFromFile("glass_container_mix_notdone_1", "glass_container_mix_notdone.ent");
        		//GiveItemFromFile("glass_container_mix_done_1", "glass_container_mix_done.ent");
        		//GiveItemFromFile("bone_saw_1", "bone_saw.ent");

        		GiveItem("stone_hammer_chipper", "Puzzle", "stone_hammer_chipper", "stone_hammer_chipper.tga", 0);
        	}


        	//////////////////////////
        	//Init Agrippa stuff
        	InitAgrippa();

        	//////////////////////////
        	//Init Ingredients that are missing.
        	//IN InitAgrippa Instead

        	///////////////////////////////
        	//Water lurker ambience
        	StartWaterLurkerAmbience();
        	AddEntityCollideCallback("Player", "AreaStartLurkerSounds", nameof(CollideStartLurkerSounds), false, 1);
        	AddEntityCollideCallback("Player", "AreaStopLurkerSounds", nameof(CollideStopLurkerSounds), false, 1);

        	//////////////////////////
        	//Blood Burner
        	AddUseItemCallback("BloodOnBurner", "glass_container_blood", "special_burner_1", nameof(UseBloodOnBurner), true);

        	AddUseItemCallback("GlandOnBurner", "poison_gland", "special_burner_1", nameof(UseGlandOnBurner), false);
        	AddUseItemCallback("BoneOnBurner", "fresh_meat_remains_1", "special_burner_1", nameof(UseBoneOnBurner), false);

        	for(int i=1; i<=2; ++i)
        		AddUseItemCallback("MixOnBurner2"+i, "glass_container_mix_done_"+i, "special_burner_1", nameof(UseMixtureOnBurner), false);
        	for(int i=1; i<=3; ++i)
        		AddUseItemCallback("MixOnBurner1"+i, "glass_container_mix_notdone_"+i, "special_burner_1", nameof(UseMixtureOnBurner), false);

        	AddUseItemCallback("GlassOnBurner", "glass_container_1", "special_burner_1", nameof(UseGlassOnMachine), false);
        	AddUseItemCallback("GlassStringOnBurner", "container_and_string", "special_burner_1", nameof(UseGlassOnMachine), false);

        	//////////////////////////
        	//Squeezer
        	AddUseItemCallback("BloodOnSqueezer", "glass_container_blood", "squeezer_1", nameof(UseBloodOnSqueezer), false);

        	AddUseItemCallback("BoneOnSqueezer", "fresh_meat_remains_1", "squeezer_1", nameof(UseBoneOnSqueezer), false);
        	for(int i=1; i<=2; ++i)
        		AddUseItemCallback("DoneMixtureOnSqueezer"+i, "glass_container_mix_done_"+i, "squeezer_1", nameof(UseDoneMixtureOnSqueezer), false);

        	AddUseItemCallback("GlandOnSqueezer", "poison_gland", "squeezer_1", nameof(UseGlandOnSqueezer), false);
        	for(int i=1; i<=3; ++i)
        		AddUseItemCallback("MixOnSqueezer"+i, "glass_container_mix_notdone_"+i, "squeezer_1", nameof(UseMixOnSqueezer), true);

        	AddUseItemCallback("GlassOnSqueezer", "glass_container_1", "squeezer_1", nameof(UseGlassOnMachine), false);
        	AddUseItemCallback("GlassStringOnSqueezer", "container_and_string", "squeezer_1", nameof(UseGlassOnMachine), false);

        	//////////////////////////
        	//Oven
        	AddUseItemCallback("BloodOnOven", "glass_container_blood", "extaction_oven_1", nameof(UseBloodOnOven), false);
        	AddUseItemCallback("BloodOnOven", "glass_container_blood", "AreaOvenTable", nameof(UseBloodOnOven), false);

        	AddUseItemCallback("GlandOnOven", "poison_gland", "extaction_oven_1", nameof(UseGlandOnOven), false);
        	for(int i=1; i<=2; ++i)
        		AddUseItemCallback("DoneMixtureOnOven"+i, "glass_container_mix_done_"+i, "extaction_oven_1", nameof(UseDoneMixtureOnOven), false);

        	AddUseItemCallback("BoneOnOven", "fresh_meat_remains_1", "extaction_oven_1", nameof(UseBoneOnOven), false);
        	for(int i=1; i<=3; ++i)
        	{
        		AddUseItemCallback("MixOnOven"+i, "glass_container_mix_notdone_"+i, "extaction_oven_1", nameof(UseMixOnOven), true);
        		AddUseItemCallback("MixOnOven"+i, "glass_container_mix_notdone_"+i, "AreaOvenTable", nameof(UseMixOnOven), true);
        	}
        	AddUseItemCallback("GlassOnOven", "glass_container_1", "extaction_oven_1", nameof(UseGlassOnMachine), false);
        	AddUseItemCallback("GlassStringOnOven", "container_and_string", "extaction_oven_1", nameof(UseGlassOnMachine), false);


        	//////////////////////////
        	//Collide callbacks
        	AddEntityCollideCallback("Player", "AreaWellKillPlayer", nameof(CollideWellKillPlayer), false, 1);


        	for(int i=1; i<=3; ++i)
        		AddEntityCollideCallback("Player", "AreaGuardianHowl_"+i, "CollideGuardianHowl_"+i, true, 1);

        	AddEntityCollideCallback("Player", "AreaAgrippaStartTalk", nameof(CollideAgrippaStartTalk), false, 1);
        	AddEntityCollideCallback("Player", "AreaAgrippaStopTalk", nameof(CollideAgrippaStopTalk), false, -1);

        	AddEntityCollideCallback("Player", "AreaAgrippaRandom", nameof(CollideAgrippaRandomArea), false, 0);

        	AddEntityCollideCallback("Player", "AreaSpikes", nameof(CollideAreaSpikes), false, 0);
        	AddEntityCollideCallback("Player", "AreaSpikes_1", nameof(CollideAreaSpikes), false, 0);
        	AddEntityCollideCallback("Player", "AreaSpikes_2", nameof(CollideAreaSpikes), false, 0);

        	//////////////////////////
        	//Item Callbacks
        	AddUseItemCallback("SawOnAgrippa","bone_saw_1", "AreaAgrippa", nameof(UseSawOnAgrippa), false);
        	AddUseItemCallback("BloodOnAgrippa","glass_container_blood", "AreaAgrippa", nameof(UseBloodOnAgrippa), false);
        	for(int i=1; i<=3; ++i)
        		AddUseItemCallback("NotdoneMixOnAgrippa","glass_container_mix_notdone_"+i, "AreaAgrippa", nameof(UseNotdoneMixOnAgrippa), false);
        	for(int i=1; i<=2; ++i)
        		AddUseItemCallback("DoneMixOnAgrippa","glass_container_mix_done_"+i, "AreaAgrippa", nameof(UseDoneMixOnAgrippa), false);


        	AddUseItemCallback("BoneOnAgrippa","fresh_meat_remains_1", "AreaAgrippa", nameof(UseMiscOnAgrippa), false);
        	AddUseItemCallback("GlandOnAgrippa","poison_gland", "AreaAgrippa", nameof(UseMiscOnAgrippa), false);
        	AddUseItemCallback("MeatOnAgrippa","fresh_meat_2", "AreaAgrippa", nameof(UseMiscOnAgrippa), false);



        }

        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PlayMusic("21_amb.ogg", true, 1, 4, 0, true);

        	PreloadSound("react_scare"); PreloadSound("21_meat_snap"); PreloadSound("scare_thump_flash"); PreloadSound("player_bodyfall");
        	PreloadSound("21_meat"); PreloadSound("general_wind_whirl"); PreloadSound("puzzle_boil"); PreloadSound("player_crouch");
        	PreloadSound("26_burner_loop"); PreloadSound("player_stand"); PreloadSound("26_liquid"); PreloadSound("26_oven_run");
        	PreloadSound("guardian_distant3"); PreloadSound("water_lurker_hunt_rev"); PreloadSound("water_lurker_eat_rev"); PreloadSound("06_rope_strain");
        	PreloadSound("21_meat_long"); PreloadSound("water_lurker_idle_rev"); PreloadSound("03_creak"); PreloadSound("react_breath");
        	PreloadSound("26_guardian_idle"); PreloadSound("react_sigh");

        	if(GetGlobalVarInt("FromZimmermanFarm")==1)
        	{
        		//The cache data is no longer needed!
        		DestroyDataCache();

        		SetGlobalVarInt("FromZimmermanFarm",0);

        		AddTimer("ZimmarmanEvents", 0.1f, nameof(TimerAfterZimmermanEffects));

        		AddEntityCollideCallback("Player", "AreaStopHead", nameof(CollideAreaStopHead), true, 1);
        		AddEntityCollideCallback("Player", "AreaFaintHead", nameof(CollideAreaFaintHead), true, 1);
        		SetEntityActive("AreaStopHead", true);
        		SetEntityActive("AreaFaintHead", true);

        		StartPlayerLookAt("AreaFlashLookUp_1", 10.0f, 10.0f, "");
        		SetPlayerCrouchDisabled(true);
        		SetPlayerJumpDisabled(true);
        		SetPlayerMoveSpeedMul(0.1f);
        		SetPlayerLookSpeedMul(0.1f);
        		SetPlayerRunSpeedMul(0.1f);
        		MovePlayerHeadPos(0.0f, -1.3f, 0.0f, 10.0f, 0.1f);
        		SetRadialBlurStartDist(0.4f);
        		FadeRadialBlurTo(0.4f, 0.4f);

        		SetupLoadScreen("LoadingText", "Ch03_Diary08_", 6, "game_loading_girl.jpg");
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
