using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch03
{
    /// <summary>Port of maps/main/ch03/29_orb_chamber.hps; journal: docs/ScriptPort/ch03/29_orb_chamber.md.</summary>
    [HplMapScript("maps/main/ch03/29_orb_chamber.map")]
    public class OrbChamber : HplScript
    {
        //-----------------------------------

        bool gbShowCredits = true;

        //-----------------------------------

        ///////////////////////////////
        // VOICE HELPERS
        ///////////////////////////////

        //-----------------------------------

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

        //-----------------------------------

        ///////////////////////////////
        // ARRIVE VOICES
        ///////////////////////////////

        //-----------------------------------

        void TimerAlexanderStartSpeak(string asTimer)
        {
        	AddDebugMessage("Arrive Voice 1 - 4", false);

        	if(HasItem("agrippa_head_1"))
        		PlayEffectVoice("CH03L29_Alexander_ArriveHead_", 1, 4,nameof(ArriveVoiceOver));
        	else
        		PlayEffectVoice("CH03L29_Alexander_ArriveNoHead_", 1, 4,nameof(ArriveVoiceOver));
        }

        //-----------------------------------

        void ArriveVoiceOver()
        {
        	//Small delay before playing over voice.

        	AddTimer("PlayerLookAtRod", 3,nameof(TimerPlayerLookAtRod)); //Direct attention to pylon!

        	StartPortalCountDown();
        }

        //-----------------------------------

        ///////////////////////////////
        // ALMOST DONE VOICES
        ///////////////////////////////

        //-----------------------------------

        void PlayAlmostDoneVoice()
        {
        	//Alexander is in a higher state, just return
        	if(GetLocalVarInt("AlexanderVoiceState") > 1) return;

        	//Set that it is a new state
        	SetLocalVarInt("AlexanderVoiceState",1);

        	//Increase entry conunter
        	AddLocalVarInt("CurrentAlmostDoneEntry", 1);
        	int lEntry = GetLocalVarInt("CurrentAlmostDoneEntry");

        	AddDebugMessage("Almost Done Voice:"+lEntry, false);

        	//Playe one entry
        	PlayEffectVoice("CH03L29_Alexander_AlmostDone_", lEntry, lEntry,nameof(AlmostDoneVoiceOver));
        }

        //-----------------------------------

        void AlmostDoneVoiceOver()
        {
        	int lInterruptCount = GetLocalVarInt("InterruptAlexanderCount");
        	int lAlmostDoneEntry = GetLocalVarInt("CurrentAlmostDoneEntry");

        	/////////////////////////////////
        	//First set of entries over
        	if(lInterruptCount==1 && lAlmostDoneEntry==2)
        	{
        		AddDebugMessage("Almost Done Over 1", false);

        		StartPortalCountDown();
        	}
        	//////////////////////
        	// Almost done entries over
        	else if(lInterruptCount==2 && lAlmostDoneEntry==5)
        	{
        		AddDebugMessage("Almost Done Over 2", false);

        		StartPortalCountDown();
        	}
        	//////////////////////
        	//Play next entry
        	else
        	{
        		PlayAlmostDoneVoice();
        	}
        }

        void TimerStartPortalDone(string asTimer)
        {
        	//If Alexander has been interrupted, then skip this
        	if(GetLocalVarInt("InterruptAlexanderCount")>=3) return;


        	//Start portal
        	SetPortalPhase(0);

        	//Play final Alexander voices.
        	PlayPortalDoneVoice();
        }

        //-----------------------------------

        ///////////////////////////////
        // PORTAL START COUNTDOWN
        ///////////////////////////////

        //-----------------------------------

        float gfCountDownMul = 0.4f;

        void ResetPortalCountDown()
        {
        	RemoveTimer("PortalCountDowne");
        	RemoveTimer(nameof(TimerPortalCountDownFlash));

        	PlayMonolithSound(1);

        	SetFlyingMonolithsSpeed(1);
        	SetLocalVarInt("PortalCountDownState", 0);

        	SetLocalVarFloat("PortalCountDownFlashSpeed",1);
        }

        void StartPortalCountDown()
        {
        	float fCountDownTime = 60.0f * gfCountDownMul * GetLocalVarFloat("PortalCountDownMul");
        	AddDebugMessage("PortalCountDownTime: "+Str(fCountDownTime), false);

        	AddTimer("PortalCountDowne", fCountDownTime, nameof(TimerPortalCountDown));

        	SetLocalVarFloat("PortalCountDownFlashSpeed",1);
        	AddTimer("PortalCountDownFlash", 1,nameof(TimerPortalCountDownFlash));


        	AddDebugMessage("Starting Portal Count Down!", false);
        }

        //-----------------------------------'

        void TimerPortalCountDown(string asTimer)
        {
        	AddLocalVarInt("PortalCountDownState", 1);
        	int lState = GetLocalVarInt("PortalCountDownState");

        	AddDebugMessage("Increasing Count Down: "+lState, false);


        	float fCountDownTime = 60.0f * gfCountDownMul * GetLocalVarFloat("PortalCountDownMul");

        	if(lState < 4)
        		AddDebugMessage("PortalCountDownTime: "+Str(fCountDownTime), false);

        	/////////////////////////
        	//State 1
        	if(lState == 1)
        	{
        		PlayMonolithSound(2);

        		PlayMusic("29_amb02.ogg", true, 0.7f, 2, 0, false);

        		AddTimer("PortalCountDowne", fCountDownTime, nameof(TimerPortalCountDown));
        		SetFlyingMonolithsSpeed(2);

        		SetLocalVarFloat("PortalCountDownFlashSpeed", 1.5f);
        	}
        	/////////////////////////
        	//State 2
        	else if(lState == 2)
        	{
        		PlayMonolithSound(3);

        		AddTimer("PortalCountDowne", fCountDownTime, nameof(TimerPortalCountDown));
        		SetFlyingMonolithsSpeed(4);

        		SetLocalVarFloat("PortalCountDownFlashSpeed", 2.0f);
        	}
        	/////////////////////////
        	//State 3
        	else if(lState == 3)
        	{
        		PlayMonolithSound(4);

        		AddTimer("PortalCountDowne", fCountDownTime, nameof(TimerPortalCountDown));
        		SetFlyingMonolithsSpeed(8);

        		SetLocalVarFloat("PortalCountDownFlashSpeed", 4.0f);
        	}
        	/////////////////////////
        	//Final State, Start portal
        	else if(lState == 4)
        	{
        		AddTimer("StartPortalDoneVoice", 0.1f, nameof(TimerStartPortalDone));
        	}

        }

        //-----------------------------------'

        void TimerPortalCountDownFlash(string asTimer)
        {
        	int lState = GetLocalVarInt("PortalCountDownFlashState");
        	lState = lState==0 ? 1 : 0;
        	SetLocalVarInt("PortalCountDownFlashState", lState);

        	float fSpeed = GetLocalVarFloat("PortalCountDownFlashSpeed");

        	if(lState == 0)
        	{
        		FadeLightTo("PointLight_47", 0,0,0,0, -1, 1.0f / fSpeed);
        	}
        	else
        	{
        		FadeLightTo("PointLight_47", 0.2f,0.2f,1.0f,1, -1, 1.0f / fSpeed);
        	}


        	float fTimerTime = 0;
        	if(lState == 0)
        		fTimerTime = 3.0f / fSpeed;
        	else
        		fTimerTime = 1.0f / fSpeed;

        	//AddDebugMessage("CountDownFlash: "+lState + " Time: "+fTimerTime, false);


        	AddTimer("PortalCountDownFlash",fTimerTime , nameof(TimerPortalCountDownFlash));
        }



        //-----------------------------------'

        ///////////////////////////////
        // INTERRUPT VOICES
        ///////////////////////////////

        //-----------------------------------'

        int[] gvInterruptEntryNum = {1,2,2};

        void InterruptAlexander()
        {
        	AddLocalVarInt("InterruptAlexanderCount", 1);
        	int lEntry = GetLocalVarInt("InterruptAlexanderCount");

        	//Change music that is played as mood shall change, when Daniel tries to stop Alexander.
        	//TODO: New music track Or not good to change??
        	PlayMusic("26_event_brute.ogg", false, 0.8f, 2, 0, false);

        	AddDebugMessage("Start interruption: "+lEntry, false);

        	///////////////////////////////
        	// Stop the portal opening
        	ResetPortalCountDown();

        	//Also increase portal opening speed:
        	if(lEntry==1)
        		SetLocalVarFloat("PortalCountDownMul",0.3f);
        	if(lEntry==2)
        		SetLocalVarFloat("PortalCountDownMul",0.25f);

        	AddDebugMessage("PortalCountDownMul Now: "+ Str(GetLocalVarFloat("PortalCountDownMul")), false);

        	///////////////////////////////
        	// Remove timers
        	{
        		//Remove the interruption over time, since it otherwise starts a almost done entry.
        		RemoveTimer("InterruptionOver");

        		//In case arrive is not yet over
        		RemoveTimer("ArriveVoiceOverDelay");

        		//Player has already tipped rod, no need to look at it!
        		RemoveTimer("PlayerLookAtRod");

        		//In case first dialog has not started.
        		RemoveTimer(nameof(TimerAlexanderStartSpeak));

        		//So that final portal done voice does not start!
        		RemoveTimer("StartPortalDoneVoice");
        	}

        	//Only stop previous voice, if no interruption is active. Else just add the comment in the qeueue
        	if(GetLocalVarInt("AlexanderInterruptionVoiceActive")==0)
        	{
        		StopAllEffectVoices(0.5f);
        	}

        	//Set that interruption voice is active and start voice
        	SetLocalVarInt("AlexanderInterruptionVoiceActive",1);
        	// AngelScript aborts on this array access, after the preceding interruption effects.
        	if(lEntry < 1 || lEntry > gvInterruptEntryNum.Length) return;
        	PlayEffectVoice("CH03L29_Alexander_Interrupt0"+lEntry+"_", 1, gvInterruptEntryNum[lEntry-1], nameof(InterruptAlexanderVoiceOver));

        	///////////////////////////
        	//Daniel ending effects
        	if(lEntry == 3)
        	{
        		//Remove this timer, to stop portal from opening!
        		RemoveTimer("StartPortalDoneVoice");

        		PlayMusic("29_amb_end_intense.ogg", true, 0.8f, 5, 0, false);

        		ShowPlayerSlime(2, false);
        		DestroyParticleSystem("ParticleSystem_1");
        	}
        }


        void InterruptAlexanderVoiceOver()
        {
        	SetLocalVarInt("AlexanderInterruptionVoiceActive",0);

        	int lEntry = GetLocalVarInt("InterruptAlexanderCount");

        	//////////////////////////////
        	//Reach last interruption, start Daniel ending!
        	if(lEntry == 3)
        	{
        		StartDanielEnding();
        	}
        	else
        	{
        		//Wait a few seconds before starting almost done dialogue again
        		AddTimer("InterruptionOver", 3, nameof(TimerInterruptionOver));
        	}
        }

        void TimerInterruptionOver(string asTimer)
        {
        	AlmostDoneVoiceOver();
        }

        ///////////////////////////////
        // PLAYER LOOKS AT ROD
        ///////////////////////////////

        //-----------------------------------

        void TimerPlayerLookAtRod(string asTimer)
        {
        	StartPlayerLookAt("AreaRod2Effect", 3, 3, "");

        	AddTimer("PlayerLookAtRodSparks",0.6f,nameof(TimerPlayerLookAtRodSparks));
        	AddTimer("StopPlayerLookAtRod",2,nameof(TimerStopPlayerLookAtRod));
        }

        void TimerPlayerLookAtRodSparks(string asTimer)
        {
        	PlaySoundAtEntity("RandGuard", "29_elec_burst.snt", "AreaRod2Effect", 0, false);
        	PlaySoundAtEntity("RandGuard", "29_elec_burst02.snt", "AreaRod2Effect", 0, false);
        	PlaySoundAtEntity("RandGuard", "29_amb_guard.snt", "AreaRandGuard_"+RandInt(1, 4), RandFloat(0.0f, 2.0f), false);
        	CreateParticleSystemAtEntity("Sparks", "ps_orb_room_rod_sparks.ps", "AreaRod2Effect", false);
        	StartScreenShake(0.08f, 0.5f,0,2);
        }

        void TimerStopPlayerLookAtRod(string asTimer)
        {
        	StopPlayerLookAt();
        }

        //-----------------------------------


        ///////////////////////////////
        // TIP ROD
        ///////////////////////////////

        //-----------------------------------

        void FirstRodInteraction(string asEntity)
        {
        	if(GetLocalVarInt("PillarFirstTouch") == 0)
        		SetMessage("Ch03Level29", "InteractRodFirstTime", -1);

        	SetLocalVarInt("PillarFirstTouch", 1);

        	PlayGuiSound("29_touch_pillar.snt", 1.0f);

        	AddTimer(asEntity, 1.0f, nameof(TimerPillarOn));

        	//for(int i=1; i<=3;++i)
        		//SetEntityPlayerInteractCallback("orb_room_rod_"+i, "", false);
        }

        void TimerPillarOn(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(FirstRodInteraction), true);
        }

        //-----------------------------------

        string[] gvRodGlowParticles = { "ParticleSystem_13", "ParticleSystem_16", "ParticleSystem_17" };
        string[] gvRodBeamParticles = { "ParticleSystem_19", "ParticleSystem_20", "ParticleSystem_18" };
        string[] gvRodBeamLights = { 	"PointLight_29", "PointLight_40",
        				"PointLight_38", "PointLight_28",
        				"PointLight_33", "PointLight_30"  };

        void CollideRodTippingPoint(string asParent, string asChild, int alState)
        {
        	//If entering last stage of experiment, there is no stopping!
        	if(GetLocalVarInt("AlexanderVoiceState")>1) return;

        	int lIdx = GetLocalVarInt(asParent+"_idx");

        	//Player lets go of rod
        	ChangePlayerStateToNormal();

        	//Disable interaction and make fall
        	SetEntityInteractionDisabled(asParent, true);

        	// AngelScript aborts at the first array access after disabling interaction.
        	if(lIdx < 0 || lIdx >= gvRodGlowParticles.Length) return;

        	//Detroy the particle systems
        	DestroyParticleSystem(gvRodGlowParticles[lIdx]);
        	DestroyParticleSystem(gvRodBeamParticles[lIdx]);

        	//Fade out purple lights
        	for(int i=0; i<2; ++i)
        		FadeLightTo(gvRodBeamLights[lIdx*2+i], 0,0,0,0, -1, 1);

        	//Give extra push to make sure it falls.
        	AddPropImpulse(asParent, 0,0,-2, "Local");
        	AddTimer(asParent, 0.5f, nameof(TimerExtraRodPush)); //To make sure it falls!

        	PlaySoundAtEntity("RandGuard", "29_amb_guard.snt", "AreaRandGuard_"+RandInt(1, 4), RandFloat(0.0f, 2.0f), false);
        	PlaySoundAtEntity(asChild+"crash", "29_pillar_fall.snt", asParent, 0.0f, false);
        	StopSound(asParent+"_sound", 1.0f);

        	AddLocalVarInt("PillarTipped", 1);
        	if(GetLocalVarInt("PillarTipped") == 3)
        		StopSound("Sound_11", 1.0f);

        	//////////////////////////////
        	//Change Alexander voice
        	InterruptAlexander();
        }

        void TimerExtraRodPush(string asTimer)
        {
        	AddPropImpulse(asTimer, 0,-1,-1, "Local");

        	AddTimer(asTimer, 0.1f, nameof(TimerExtraRodPush));
        }

        //-----------------------------------

        void CollideRodHitGround(string asParent, string asChild, int alState)
        {
        	AddDebugMessage("Rod hit ground: "+asParent, false);

        	//Remove the impulse add
        	RemoveTimer(asParent);

        	//////////////////////////////
        	//Shake and blur effect
        	StartScreenShake(0.1f, 0.0f, 2.0f, 1);
        	SetRadialBlurStartDist(0.2f);
        	FadeRadialBlurTo(0.09f, 0.03f);
        	SetPlayerMoveSpeedMul(0.3f);
        	AddTimer("RodFallEffectOver", 2, nameof(TimerRodFallEffectOver));

        	AddLocalVarFloat("PlayerDefuaultSpeed", -0.25f);

        	//Any need for particle system?
        	//CreateParticleSystemAtEntity("rodfall", "ps_impact_dust_high.ps", asChild, false);

        	PlaySoundAtEntity("pillar_ground", "29_pillar_hit.snt", asChild, 0.0f, false);
        }

        void TimerRodFallEffectOver(string asTimer)
        {
        	FadeRadialBlurTo(0.0f, 0.01f);
        	SetPlayerMoveSpeedMul(GetLocalVarFloat("PlayerDefuaultSpeed"));
        }

        //-----------------------------------

        ///////////////////////////////
        // PORTAL OPENS VOICES
        ///////////////////////////////


        //-----------------------------------

        void PlayPortalDoneVoice()
        {
        	//Set that we are in new state!
        	SetLocalVarInt("AlexanderVoiceState",2);

        	int lEntry = GetLocalVarInt("PortalOpensCount") + 1;

        	AddDebugMessage("Portal Done Voice:"+lEntry, false);

        	PlayEffectVoice("CH03L29_Alexander_PortalOpens_", lEntry, lEntry,nameof(PortalDoneVoiceOver));
        }

        //-----------------------------------

        void PortalDoneVoiceOver()
        {
        	if(GetLocalVarInt("AlexanderVoiceState")>2) return;

        	AddLocalVarInt("PortalOpensCount", 1);
        	int lEntry = GetLocalVarInt("PortalOpensCount");

        	///////////////////////////
        	//Change the portal 2nd portal phase and play next voice
        	if(lEntry == 1)
        	{
        		//Delay the final talk a little bit
        		AddTimer("DelayOpenPortal", 5, nameof(TimerDelayOpenPortal));

        	}
        	/////////////////////////
        	// Portal is done
        	else if(lEntry == 2)
        	{
        		StartAlexanderEnding();
        	}

        }

        void TimerDelayOpenPortal(string asTimer)
        {
        	//Open portal (it starts sucking in debris!)
        	SetPortalPhase(1);

        	//The final talk.
        	PlayPortalDoneVoice();
        }

        //-----------------------------------

        ///////////////////////////////
        // PORTAL EFFECTS
        ///////////////////////////////

        //-----------------------------------

        int glNumOfOrbParticles = 7;
        string[] gvOrbParticles = {"ParticleSystem_16", "ParticleSystem_20", "ParticleSystem_1", "ParticleSystem_19", "ParticleSystem_13", "ParticleSystem_18", "ParticleSystem_17"};

        int glNumOfOrbLights = 6;
        string[] gvOrbLights = { "PointLight_28", "PointLight_29", "PointLight_30", "PointLight_33", "PointLight_38", "PointLight_40"};

        //-----------------------------------

        void SetPortalPhase(int alPhase)
        {
        	SetLocalVarInt("PortalState", alPhase+1);

        	//////////////////////////
        	// Start Phase
        	if(alPhase == 0)
        	{
        		//If player has grippa head, look at portal as it opens
        		if(HasItem("agrippa_head_1"))
        		{
        			StartPlayerLookAt("AreaPortalCenter",3 ,5,"");
        			AddTimer("StopLookAtPortal", 1.5f, nameof(TimerStopLookAtPortal));
        		}

        		PlaySoundAtEntity("RandGuard", "29_amb_guard.snt", "AreaPortalCenter", 0, false);

        		PlaySoundAtEntity("PortalOpens", "29_hole_open", "AreaPortalCenter", 0.0f, false);
        		PlaySoundAtEntity("PortalOpens2", "29_hole_blue", "AreaPortalCenter", 2.0f, false);
        		PlaySoundAtEntity("PortalOpensd", "29_hole_opened02", "AreaPortalCenter", 8.0f, false);

        		StopSound("orb_room_rod_1_sound", 1.0f);
        		StopSound("orb_room_rod_2sound", 1.0f);
        		StopSound("orb_room_rod_3_sound", 1.0f);

        		//Shake when portal opens.
        		AddTimer("PortalShakeLoop", 0.1f, nameof(TimerPortalShakeLoop));

        		//Destroy all particle systems to do with orb
        		for(int i=0; i<glNumOfOrbParticles; ++i)
        			DestroyParticleSystem(gvOrbParticles[i]);

        		//Fade out all orb lights
        		for(int i=0; i<glNumOfOrbLights; ++i)
        			FadeLightTo(gvOrbLights[i], 0,0,0,0, -1, 1);


        		//Fade in large portal light
        		FadeLightTo("PointLight_32", 0.3f,0.8f,1,1, -1, 3);

        		//Crealse port
        		CreateParticleSystemAtEntity("PortalStart", "ps_orb_room_portal_start.ps", "AreaPortalCenter", false);

        		//Player gets hurt when interacting with active portal
        		SetEntityPlayerInteractCallback("AreaPortalInteractArea", nameof(InteractActivePort), false);
        	}
        	//////////////////////////
        	// Open Phase
        	else if(alPhase == 1)
        	{
        		PlaySoundAtEntity("PortalOpenened", "29_wind", "AreaPortalCenter", 4.0f, false);
        		PlaySoundAtEntity("RandGuard", "29_amb_guard.snt", "AreaPortalCenter", 0, false);
        		PlaySoundAtEntity("PortalBlue", "29_hole_blue.snt", "AreaPortalCenter", 0, false);

        		DestroyParticleSystem("PortalStart");
        		CreateParticleSystemAtEntity("PortalOpen", "ps_orb_room_portal_open.ps", "AreaPortalCenter", false);
        	}
        	//////////////////////////
        	// Closed Phase
        	else if(alPhase == 2)
        	{
        		StopSound("PortalOpenened", 3.0f);
        		StopSound("PortalOpens", 3.0f);
        		StopSound("PortalOpensd", 3.0f);
        		RemoveTimer("mono_sound1");RemoveTimer("mono_sound2");
        		StopSound("mono_sound1", 3.0f);StopSound("mono_sound2", 3.0f);
        		RemoveTimer("mono_sound1");RemoveTimer("mono_sound2");
        		StopSound("mono_sound1", 3.0f);StopSound("mono_sound2", 3.0f);
        		RemoveTimer("mono_sound1");RemoveTimer("mono_sound2");
        		StopSound("mono_sound1", 3.0f);StopSound("mono_sound2", 3.0f);

        		//Count down is now over!
        		ResetPortalCountDown();

        		//No more interaction allowed on portal!
        		SetEntityPlayerInteractCallback("AreaPortalInteractArea", "", false);

        		RemoveTimer("PortalShakeLoop");

        		FadeLightTo("PointLight_32", 0,0,0,0, -1, 2);
        		DestroyParticleSystem("PortalOpen");
        	}
        }

        //-----------------------------------

        void InteractActivePort(string asEntity)
        {
        	if(GetTimerTimeLeft("InteractActivePortTimer")>0) return;

        	PlayGuiSound("ui_fire_damage.ogg", 0.5f);
        	PlayGuiSound("attack_claw_hit01.ogg", 0.1f);

        	GivePlayerDamage(10, "BloodSplat", true, false);

        	AddTimer("InteractActivePortTimer", 0.5f, ""); //So player cannot click alot
        }

        //-----------------------------------

        void TimerStopLookAtPortal(string asTimer)
        {
        	StopPlayerLookAt();
        }

        //-----------------------------------


        void TimerPortalShakeLoop(string asTimer)
        {
        	StartScreenShake(0.05f, 1, 1, 1);

        	AddTimer("PortalShakeLoop", 1, nameof(TimerPortalShakeLoop));
        }

        //-----------------------------------

        ///////////////////////////////
        // THROW HEAD INTO PORTAL
        ///////////////////////////////

        //-----------------------------------

        void UseHeadOnPortal(string asItem, string asEntity)
        {
        	int lPortalState = GetLocalVarInt("PortalState");

        	///////////////////////////
        	//Portal not yet open
        	if(lPortalState==0)
        	{
        		SetMessage("Ch03Level29", "HeadOnNotOpenPortal",-1);
        	}
        	///////////////////////////
        	//Portal starting up
        	else if(lPortalState==1)
        	{
        		SetMessage("Ch03Level29", "HeadOnStartPortal",-1);
        	}
        	///////////////////////////
        	//Portal open and Alexander Ending not started
        	else if(lPortalState==2 && GetLocalVarInt("AlexanderEnding")==0)
        	{
        		RemoveItem(asItem);

        		StartAgrippaEnding();
        	}
        }

        //-----------------------------------

        ///////////////////////////////
        // ENDING BASE
        ///////////////////////////////

        //-----------------------------------

        void EndingSetup()
        {
        	//////////////////////////
        	//New voice state for Alexander.
        	SetLocalVarInt("AlexanderVoiceState",3);

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

        	//////////////////////////
        	//Remove timers
        	RemoveTimer(nameof(TimerRodFallEffectOver));
        	RemoveTimer(nameof(TimerInterruptionOver));
        	RemoveTimer("TimerArriveVoiceOverDelay"); //Good to remove for debug reasosns
        	RemoveTimer(nameof(TimerStartPortalDone));	  //Good to remove for debug reasosns
        	RemoveTimer(nameof(TimerAlexanderStartSpeak));  //Good to remove for debug reasosns

        	////////////////////////
        	//No sanity
        	SetSanityDrainDisabled(true);
        }


        //-----------------------------------

        ///////////////////////////////
        // ALEXANDER END
        ///////////////////////////////

        //-----------------------------------

        void StartAlexanderEnding()
        {
        	AddDebugMessage("Start Alexander Ending", false);

        	EndingSetup();

        	SetLocalVarInt("AlexanderEnding",1);

        	ResumeAlexEnding();

        	UnlockAchievement("Sacrifice");
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
        	// 0: Set up Alexander and fade music
        	if(lState == 0)
        	{
        		SetNPCFollowPlayer("alexander_1", false);
        		StopMusic(4, 0);

        		PlayPropAnimation("alexander_1", "Release", 0.3f, false, "");

        		PlaySoundAtEntity("close_portal", "29_hole_close.snt", "AreaPortalCenter", 0.0f, false);

        		PlayMusic("29_event_end.ogg", false, 1.0f, 0.5f, 10, false);

        		fNextEventTime	= 2;
        	}
        	////////////////////////
        	// 1: Play Alexander animation and fade to white
        	else if(lState == 1)
        	{
        		StartEffectFlash(1, 2.2f, 2);

        		StartPlayerLookAt("AreaLookAtAlex", 1, 1,"");

        		fNextEventTime	= 4;
        	}
        	////////////////////////
        	// 2: Remove portal and Alexander
        	else if(lState == 2)
        	{
        		SetEntityActive("alexander_1", false);
        		//PlaySoundAtEntity("cough", "29_cough.snt", "Player", 10.0f, false);

        		//Remove portal
        		SetPortalPhase(2);

        		fNextEventTime	= 1;
        	}
        	////////////////////////
        	// 3: Stop look at and fade in music (+ flash fade in)
        	else if(lState == 3)
        	{
        		StopPlayerLookAt();
        		PlayMusic("29_amb_end_soft.ogg", true, 0.9f, 5, 0, false);
        		PlaySoundAtEntity("slime", "25_slime_loop.snt", "Player", 3.0f, false);

        		fNextEventTime= 3;
        	}
        	////////////////////////
        	// 4: Recalled voice "Girl"
        	else if(lState == 4)
        	{
        		PlayFlashbackVoice("CH03L29_DanielRemorse_", 1,1, nameof(RemorseVoiceOver));

        		bPause	= true;
        	}
        	////////////////////////
        	// 5: Guardian roar #1
        	else if(lState == 5)
        	{
        		SetPlayerSanity(100); //To make sure it does not fall to 0 at the end.
        		AddTimer("50", 1.5f, nameof(TimerEndSanityDamage));

        		PlaySoundAtEntity("howl","guardian_distant1", "Player", 0, false);
        		//PlaySoundAtEntity("cough", "29_cough.snt", "Player", 0.0f, false);

        		ShowPlayerSlime(0, true);

        		fNextEventTime= 7;
        	}
        	////////////////////////
        	// 6: Recalled voice "Captive"
        	else if(lState == 6)
        	{
        		PlayFlashbackVoice("CH03L29_DanielRemorse_", 2,2, nameof(RemorseVoiceOver));

        		bPause	= true;
        	}
        	////////////////////////
        	// 7: Guardian roar #2
        	else if(lState == 7)
        	{
        		AddTimer("20", 1.5f, nameof(TimerEndSanityDamage));
        		PlaySoundAtEntity("howl","guardian_distant2", "Player", 0, false);
        		//StopSound("cough", 0.5f);
        		//PlayGuiSound("CH03L29_Ending_Daniel_Cough.ogg", 0.6f);

        		ShowPlayerSlime(1, true);

        		fNextEventTime= 5;
        	}
        	////////////////////////
        	// 8: Recalled voice "Mother"
        	else if(lState == 8)
        	{
        		PlayFlashbackVoice("CH03L29_DanielRemorse_", 3,3, nameof(RemorseVoiceOver));
        		//PlaySoundAtEntity("cough", "29_cough.snt", "Player", 0.0f, false);

        		bPause	= true;
        	}
        	////////////////////////
        	// 9: Guardian roar #3
        	else if(lState == 9)
        	{
        		//GiveSanityDamage(20, true);
        		PlaySoundAtEntity("howl","guardian_distant3", "Player", 0, false);

        		//StopSound("cough", 0.5f);

        		GivePlayerDamage(20, "BloodSplat", true, false);
        		PlayGuiSound("attack_claw_hit.snt", 0.5f);
        		PlayGuiSound("25_guardian_slime_appear.snt", 0.8f);
        		PlayGuiSound("water_lurker_eat_rev2.ogg", 0.5f);

        		PlayGuiSound("CH03L29_Ending_Daniel_Scream.ogg", 0.8f);

        		ShowPlayerSlime(2, false);

        		fNextEventTime= 6.5f;
        	}
        	////////////////////////
        	// 10: Guardian final + kill player
        	else if(lState == 10)
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

        		StopMusic(4,0);

        		//TODO: 3nd batch of slime!
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
        		if(gbShowCredits)
        			StartCredits("ending_alexander.ogg",false,  "Ending", "MainCredits", 0);
        		return;
        	}


        	AddDebugMessage("Event:"+lState,false);//+" Time:"+fNextEventTime+" Pause"+bPause?1:0, false);

        	if(bPause==false)
        		AddTimer("AlexEnding", fNextEventTime, nameof(TimerAlexEnding));
        }

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

        ///////////////////////////////
        // AGRIPPA END
        ///////////////////////////////

        //-----------------------------------

        void StartAgrippaEnding()
        {
        	AddDebugMessage("Start Agrippa Ending", false);

        	EndingSetup();

        	SetLocalVarInt("AgrippaEnding",1);

        	for(int i=1; i<=3; ++i)
        		AddEntityCollideCallback("Player","AgrippaEnding_"+i, "CollideAgrippaEnding"+i, true, 1);

        	ResumeAgrippaEnding();
        	UnlockAchievement("Benefactor");
        }

        //-----------------------------------

        void ResumeAgrippaEnding()
        {
        	AddTimer("AgrippaEnding", 0.1f, nameof(TimerAgrippaEnding));
        }
        //-----------------------------------

        void TimerAgrippaEnding(string asTimer)
        {
        	int lState = GetLocalVarInt("AgrippaEndState");
        	AddLocalVarInt("AgrippaEndState",1);
        	float fNextEventTime = 1.0f;
        	bool bPause = false;

        	////////////////////////
        	// 0: Set up Alexander, make alex speak and change music
        	if(lState == 0)
        	{
        		SetNPCFollowPlayer("alexander_1", false);
        		PlayMusic("29_amb_end_intense.ogg", true, 0.8f, 5, 0, false);

        		StopAllEffectVoices(0.5f);
        		PlayEffectVoice("CH03L29_Alexander_AgrippaEnd_", 1, 1, nameof(AlexAngryVoiceOver));
        		//PlayGuiSound("CH03L29_Ending_Daniel_Cough.ogg", 0.6f);

        		PlayGuiSound("player_jump7.ogg", 1.0f);
        		AddTimer("headevent1", 1.0f, nameof(TimerHeadOnPortalEvent));
        		AddTimer("headevent2", 3.0f, nameof(TimerHeadOnPortalEvent));

        		PlayMusic("29_event_end.ogg", false, 1.0f, 0.5f, 10, false);

        		bPause = true;
        	}
        	////////////////////////
        	// 1: Change Alexander animation and show slime #1
        	else if(lState == 1)
        	{
        		PlayPropAnimation("alexander_1", "Death", 0.3f, false, "");

        		PlayEffectVoice("CH03L29_Alexander_DeathScream_", 1, 1, nameof(AlexAngryVoiceOver));
        		PlaySoundAtEntity("slimel", "25_slime_loop.snt", "Player", 4.0f, false);

        		GiveSanityDamage(20, true);

        		//Remove portal
        		SetPortalPhase(2);

        		ShowPlayerSlime(0, true);
        		ShowAlexanderSlime(0);

        		StartPlayerLookAt("AreaLookAtAlex", 1, 1,"");

        		fNextEventTime = 0.5f;
        	}
        	////////////////////////
        	// 2: Slime #2
        	else if(lState == 2)
        	{
        		ShowPlayerSlime(1, true);
        		fNextEventTime = 0.5f;
        	}
        	////////////////////////
        	// 3: Slime #3
        	else if(lState == 3)
        	{
        		ShowPlayerSlime(2, false);
        		fNextEventTime = 2.5f;
        	}
        	////////////////////////
        	// 4: Fade out alexander
        	else if(lState == 4)
        	{
        		PlaySoundAtEntity("screamd", "29_scream.snt", "Player", 0.0f, false);

        		StopPlayerLookAt();

        		SetPropActiveAndFade("alexander_1", false, 2);
        		CreateParticleSystemAtEntity("AlexExplode","ps_orb_room_alex_die.ps", "AreaAlexEffect", false);
        		PlaySoundAtEntity("killalex", "25_guardian_slime_appear.snt", "AreaLookAtAlex_1", 0.0f, false);

        		StartScreenShake(0.22f, 3, 1, 3);

        		GiveSanityDamage(10, true);

        		bPause = true;
        	}
        	////////////////////////
        	// 5: Kill Player
        	else if(lState == 5)
        	{
        		FadeOut(5);

        		StopMusic(5,0);
        		StopSound("screamd", 0.5f);
        		GivePlayerDamage(20, "BloodSplat", true, false);
        		PlayGuiSound("attack_claw_hit.snt", 0.5f);
        		PlayGuiSound("25_guardian_slime_appear.snt", 0.8f);
        		PlayGuiSound("water_lurker_eat_rev2.ogg", 0.5f);

        		SetPlayerActive(false);

        		FadeGlobalSoundVolume(0.3f, 9);
        		FadeGlobalSoundSpeed(0.7f, 9);
        		StopSound("slimel", 8.0f);

        		FadePlayerRollTo(90, 10, 10);
        		MovePlayerHeadPos(0, -0.6f, 0.3f, 3, 5);

        		SetPlayerSanity(100);//So there is no panting when in dark or so.

        		fNextEventTime	= 5;
        	}
        	////////////////////////
        	// 6: Fade in when player is in darkness + start music
        	else if(lState == 6)
        	{
        		TeleportPlayer("PlayerStartArea_Agrippa");
        		StartPlayerLookAt("AreaAgrippaEndEffect", 1, 1,"");

        		FadePlayerRollTo(0, 90, 90);
        		MovePlayerHeadPos(0, 0, 0, 5, 5);

        		PlayMusic("29_amb_end_soft.ogg", true, 0.6f, 3, 0, false);
        		FadeIn(3);

        		fNextEventTime = 3;
        	}
        	////////////////////////
        	// 7: Add some sparkles
        	else if(lState == 7)
        	{
        		CreateParticleSystemAtEntity("AgrippaEndEffect","ps_orb_room_agrippa_end_effect.ps", "AreaAgrippaEndEffect", false);

        		fNextEventTime = 0.1f;
        	}
        	////////////////////////
        	// 8: Agrippa speaks
        	else if(lState == 8)
        	{
        		//Player is now free to move about and has to walk to the end of the "tunnel"

        		SetPlayerActive(true);
        		SetPlayerMoveSpeedMul(0.3f);
        		SetPlayerRunSpeedMul(0.0f);
        		SetPlayerJumpDisabled(true);
        		SetPlayerCrouchDisabled(true);
        		bPause = true;
        	}
        	////////////////////////
        	// 9: Fade to white!
        	else if(lState == 9)
        	{
        		SetPlayerActive(false);

        		PlayGuiSound("29_hole_open.ogg", 1.0f);

        		FadePlayerFOVMulTo(0.5f, 0.25f);

        		StartEffectFlash(3,5,3);
        		fNextEventTime = 5;
        	}
        	////////////////////////
        	// 10: Fade out
        	else if(lState == 10)
        	{
        		FadeOut(3);
        		fNextEventTime = 8;
        	}
        	////////////////////////
        	// 11: Fade out
        	else if(lState == 11)
        	{
        		if(gbShowCredits)
        			StartCredits("ending_agrippa.ogg",false,  "Ending", "MainCredits", 1);
        		return;
        	}

        	AddDebugMessage("Event:"+lState+" Time:"+Str(fNextEventTime),false);//+" Time:"+fNextEventTime+" Pause"+bPause?1:0, false);

        	if(bPause==false)
        		AddTimer("AgrippaEnding", fNextEventTime, nameof(TimerAgrippaEnding));
        }

        void TimerHeadOnPortalEvent(string asTimer)
        {
        	if(asTimer == "headevent1"){
        		CreateEntityAtArea("elheado", "agrippa_head_obj.ent", GetLocalVarString("WhatHeadArea")+"_Place", false);
        		PlaySoundAtEntity("meat1", "21_meat.snt", "AreaPortalCenter", 0.0f, false);
        	}
        	else{
        		SetPropActiveAndFade("elheado", false, 3.0f);
        		PlaySoundAtEntity("headsound", "29_hole_blue.snt", "AreaPortalCenter", 0.0f, false);
        		PlaySoundAtEntity("meat2", "21_meat_long.snt", "AreaPortalCenter", 0.0f, false);
        		PlayGuiSound("03_in_a_bottle.ogg", 0.6f);
        	}
        }

        //-----------------------------------

        void CollideAgrippaEnding1(string asParent, string asChild, int alState)
        {
        	PlayEffectVoice("CH03L29_Ending_Agrippa_", 1, 1, "");
        }

        void CollideAgrippaEnding2(string asParent, string asChild, int alState)
        {
        	PlayEffectVoice("CH03L29_Ending_Agrippa_", 2, 2, "");
        }

        void CollideAgrippaEnding3(string asParent, string asChild, int alState)
        {
        	PlayEffectVoice("CH03L29_Ending_Agrippa_", 3, 3, nameof(AgrippaVoiceOver));
        }

        //-----------------------------------

        void AlexAngryVoiceOver()
        {
        	ResumeAgrippaEnding();
        }

        void AgrippaVoiceOver()
        {
        	ResumeAgrippaEnding();
        }

        //-----------------------------------

        ///////////////////////////////
        // DANIEL END
        ///////////////////////////////

        //-----------------------------------

        float gfDanielEndGlobalVolume = 0.3f;
        float gfDanielEndSceneFadeOutTime = 5;

        //-----------------------------------

        void StartDanielEnding()
        {
        	AddDebugMessage("Start Daniel Ending", false);

        	//PlaySoundAtEntity("cough", "29_cough.snt", "Player", 0.0f, false);
        	PlaySoundAtEntity("slimes", "25_slime_loop.snt", "Player", 2.0f, false);

        	EndingSetup();

        	SetLocalVarInt("DanielEnding",1);

        	ResumeDanielEnding();

        	UnlockAchievement("Survivor");
        }

        //-----------------------------------

        void ResumeDanielEnding()
        {
        	AddTimer("DanielEnding", 0.1f, nameof(TimerDanielEnding));
        }
        //-----------------------------------

        void TimerDanielEnding(string asTimer)
        {
        	int lState = GetLocalVarInt("DanielEndState");
        	AddLocalVarInt("DanielEndState",1);
        	float fNextEventTime = 1.0f;
        	bool bPause = false;

        	////////////////////////
        	// 0: Slime at Alexander,player looks at him and play music!
        	if(lState == 0)
        	{
        		SetNPCFollowPlayer("alexander_1", false);

        		PlayEffectVoice("CH03L29_Alexander_DeathScream_", 1, 1, nameof(DanielEndingVoiceOver));

        		StartPlayerLookAt("AreaLookAtAlex", 1, 1,"");

        		PlayMusic("29_event_end.ogg", false, 1.0f, 0.5f, 10, false);

        		fNextEventTime = 2;
        	}
        	////////////////////////
        	// 1: Change Alexander animation
        	else if(lState ==1)
        	{
        		ShowAlexanderSlime(0);

        		PlayPropAnimation("alexander_1", "Death", 0.3f, false, "");

        		fNextEventTime = 3;
        	}
        	////////////////////////
        	// 2: Kill Alexander.
        	else if(lState ==2)
        	{
        		StopPlayerLookAt();

        		SetPropActiveAndFade("alexander_1", false, 2);
        		CreateParticleSystemAtEntity("AlexExplode","ps_orb_room_alex_die.ps", "AreaAlexEffect", false);
        		PlaySoundAtEntity("killalex", "25_guardian_slime_appear.snt", "AreaLookAtAlex_1", 0.0f, false);
        		PlaySoundAtEntity("killalex", "slime_create.snt", "AreaLookAtAlex_1", 0.0f, false);

        		StartScreenShake(0.22f, 3, 1, 3);

        		GiveSanityDamage(10, true);

        		bPause = true;
        	}
        	////////////////////////
        	// 3: Fade out #1
        	else if(lState ==3)
        	{
        		StopMusic(gfDanielEndSceneFadeOutTime, 0);

        		//PlayGuiSound("CH03L29_Ending_Daniel_Cough.ogg", 0.5f);

        		FadeOut(gfDanielEndSceneFadeOutTime);
        		FadeGlobalSoundVolume(0, gfDanielEndSceneFadeOutTime);

        		FadeGlobalSoundSpeed(0.7f, gfDanielEndSceneFadeOutTime); //Change freq on sounds!

        		StopSound("slimes", 8.0f);

        		fNextEventTime = gfDanielEndSceneFadeOutTime;
        	}
        	////////////////////////
        	// 4: Foot Steps #1
        	else if(lState ==4)
        	{
        		PlayMusic("09_amb_safe.ogg", true, 0.7f, 5, 0, false);

        		SetPlayerActive(false);

        		SetPlayerJumpDisabled(true);

        		//PlaySoundAtEntity("Footsteps", "29_daniel_footsteps.snt", "Player", 0, false);
        		TeleportPlayer("PlayerStartArea_Daniel1");

        		//Setup player for coming events:
        		SetPlayerMoveSpeedMul(0.4f);
        		SetPlayerLookSpeedMul(0.7f);

        		fNextEventTime = 3;

        	}
        	////////////////////////
        	// 5: Fade In #1 (Torture Nave)
        	else if(lState ==5)
        	{
        		FadeIn(3);

        		FadeGlobalSoundVolume(gfDanielEndGlobalVolume, 3);

        		PlayEffectVoice("CH03L29_Ending_Daniel_", 1, 2, nameof(DanielEndingVoiceOver));

        		SetPlayerActive(true);
        		StartPlayerLookAt("AreaDanielEndLookAt_1", 0.3f, 1,"");

        		bPause = true;
        	}
        	////////////////////////
        	// 6: Fade out #2
        	else if(lState ==6)
        	{
        		FadeOut(gfDanielEndSceneFadeOutTime);
        		FadeGlobalSoundVolume(0, gfDanielEndSceneFadeOutTime);

        		fNextEventTime = gfDanielEndSceneFadeOutTime;
        	}
        	////////////////////////
        	// 7: Foot Steps #2
        	else if(lState ==7)
        	{
        		StopPlayerLookAt();
        		SetPlayerActive(false);

        		//PlaySoundAtEntity("Footsteps", "29_daniel_footsteps.snt", "Player", 0, false);
        		TeleportPlayer("PlayerStartArea_Daniel2");

        		fNextEventTime = 3;
        	}
        	////////////////////////
        	// 8: Fade In #2 (Sewer)
        	else if(lState ==8)
        	{
        		FadeIn(3);
        		FadeGlobalSoundVolume(gfDanielEndGlobalVolume, 3);

        		PlayEffectVoice("CH03L29_Ending_Daniel_", 3, 3, nameof(DanielEndingVoiceOver));

        		SetPlayerActive(true);
        		StartPlayerLookAt("AreaDanielEndLookAt_2", 0.3f, 1,"");

        		bPause = true;
        	}
        	////////////////////////
        	// 9: Fade out #3
        	else if(lState ==9)
        	{
        		FadeOut(gfDanielEndSceneFadeOutTime);
        		FadeGlobalSoundVolume(0, gfDanielEndSceneFadeOutTime);

        		fNextEventTime = gfDanielEndSceneFadeOutTime;
        	}
        	////////////////////////
        	// 10: Foot Steps #3
        	else if(lState ==10)
        	{
        		StopPlayerLookAt();
        		SetPlayerActive(false);

        		//PlaySoundAtEntity("Footsteps", "29_daniel_footsteps.snt", "Player", 0, false);
        		TeleportPlayer("PlayerStartArea_Daniel3");

        		fNextEventTime = 3;
        	}
        	////////////////////////
        	// 11: Fade In #3 (Archive Tunnels)
        	else if(lState ==11)
        	{
        		FadeIn(3);
        		FadeGlobalSoundVolume(gfDanielEndGlobalVolume, 3);

        		PlayEffectVoice("CH03L29_Ending_Daniel_", 4, 4, nameof(DanielEndingVoiceOver));

        		SetPlayerActive(true);
        		StartPlayerLookAt("AreaDanielEndLookAt_3", 0.3f, 1,"");

        		bPause = true;
        	}
        	////////////////////////
        	// 12: Fade out #4
        	else if(lState ==12)
        	{
        		FadeOut(gfDanielEndSceneFadeOutTime);
        		FadeGlobalSoundVolume(0, gfDanielEndSceneFadeOutTime);

        		fNextEventTime = gfDanielEndSceneFadeOutTime;
        	}
        	////////////////////////
        	// 13: Foot Steps #4
        	else if(lState ==13)
        	{
        		StopPlayerLookAt();
        		SetPlayerActive(false);

        		//PlaySoundAtEntity("Footsteps", "29_daniel_footsteps.snt", "Player", 0, false);
        		TeleportPlayer("PlayerStartArea_Daniel4");

        		fNextEventTime = 3.0f;
        	}
        	////////////////////////
        	// 14: Fade In #4 (Castle Entrance)
        	else if(lState ==14)
        	{
        		FadeIn(3);
        		FadeGlobalSoundVolume(gfDanielEndGlobalVolume, 3);

        		SetPlayerMoveSpeedMul(0.3f);//Want a really slow player
        		SetPlayerRunSpeedMul(0.3f);


        		PlayEffectVoice("CH03L29_Ending_Daniel_", 5, 6, nameof(DanielEndingVoiceOver));

        		SetPlayerActive(true);
        		StartPlayerLookAt("AreaDanielEndLookAt_4", 0.3f, 1,"");

        		bPause = true;
        	}
        	////////////////////////
        	// 15: Fade to white!
        	else if(lState == 15)
        	{
        		PlaySoundAtEntity("crumble", "29_crumble", "Player", 7.0f, false);

        		AddTimer("doorTimer", 2.0f, nameof(TimerDoor));
        		StopMusic(3,0);

        		FadePlayerFOVMulTo(0.6f, 0.25f);

        		StartEffectFlash(3,10,3);
        		fNextEventTime = 5;

        		FadeGlobalSoundVolume(0, 3);
        	}
        	////////////////////////
        	// 16: Fade out
        	else if(lState == 16)
        	{
        		SetPlayerActive(true);
        		FadeOut(3);
        		fNextEventTime = 8;
        	}
        	////////////////////////
        	// 17: Fade out
        	else if(lState == 17)
        	{
        		StopSound("crumble", 4.0f);

        		if(gbShowCredits)
        			StartCredits("ending_daniel.ogg",false,  "Ending", "MainCredits", 2);
        		return;
        	}


        	AddDebugMessage("Event:"+lState+" Time:"+Str(fNextEventTime),false);//+" Time:"+fNextEventTime+" Pause"+bPause?1:0, false);

        	if(bPause==false)
        		AddTimer("DanielEnding", fNextEventTime, nameof(TimerDanielEnding));
        }

        void TimerDoor(string asTimer)
        {
        	PlaySoundAtEntity("door", "29_exit_door", "Player", 1.5f, false);
        }

        //-----------------------------------

        void DanielEndingVoiceOver()
        {
        	ResumeDanielEnding();
        }


        //-----------------------------------

        ///////////////////////////////
        // SLIME
        ///////////////////////////////

        //-----------------------------------

        void ShowPlayerSlime(int alNum, bool abActivateDamage)
        {
        	if(abActivateDamage)
        		SetEntityActive("SlimeDamageArea_1", true);
        	else
        		SetEntityActive("SlimeDamageArea_1", false);

        	int lIdx = alNum+1;
        	SetPropActiveAndFade("slime_player"+lIdx+"*", true, 2);
        	PlaySoundAtEntity("slime"+lIdx+"1", "slime_loop.snt", "slime_player"+lIdx+"_1", 1.0f, false);
        	PlaySoundAtEntity("slime"+lIdx+"3", "slime_loop.snt", "slime_player"+lIdx+"_3", 1.0f, false);
        	PlayGuiSound("25_guardian_slime_appear.snt", 0.8f);
        	PlayGuiSound("guardian_idle6.ogg", 1.0f);

        	if(alNum==0)
        	{
        		SetLampLit("torch_floor_*", false, true);

        		//Fade out torch lights
        		FadeLightTo("PointLight_12", 0,0,0,0,  -1, 1);
        		FadeLightTo("PointLight_19", 0,0,0,0,  -1, 1);
        		FadeLightTo("PointLight_14", 0,0,0,0,  -1, 1);
        		FadeLightTo("PointLight_15", 0,0,0,0,  -1, 1);
        		FadeLightTo("PointLight_11", 0,0,0,0,  -1, 1);
        		FadeLightTo("PointLight_10", 0,0,0,0,  -1, 1);
        		FadeLightTo("PointLight_8", 0,0,0,0,  -1, 1);
        		FadeLightTo("PointLight_9", 0,0,0,0,  -1, 1);

        		FadeLightTo("PointLight_18", 1,0,0.2f,1,  6, 3);
        	}
        	else if(alNum==1)
        	{
        		FadeLightTo("PointLight_32", 1,0,0.2f,1,  -1, 3);
        	}
        	else if(alNum==2)
        	{
        		FadeLightTo("PointLight_28", 1,0,0.2f,0.7f,  -1, 3);
        		FadeLightTo("PointLight_29", 1,0,0.2f,0.7f,  -1, 3);
        	}
        }

        //-----------------------------------

        void ShowAlexanderSlime(int alNum)
        {
        	int lIdx = alNum+1;
        	SetPropActiveAndFade("slime_alex"+lIdx+"*", true, 2);

        	if(alNum==0)
        	{
        		FadeLightTo("PointLight_2", 1,0,0.2f,1,  6, 3);
        	}

        }

        //-----------------------------------




        ///////////////////////////////
        // FLYING MONOLITHS
        ///////////////////////////////

        //-----------------------------------

        int[] gvMonolithTypeNum = {18, 12};
        float[] gvMonolithSpeeds = {1, 1.4f, 5.6f, 0.8f, 3};

        void InitFlyingMonoliths()
        {
        	int lAxis =1;
        	int lCount =0;
        	for(int type=1; type<=2; ++type)
        	{
        		for(int idx=1; idx<=gvMonolithTypeNum[type-1]; ++idx)
        		{
        			string sName = "monolith0"+type+"_"+idx;

        			SetLocalVarFloat(sName+"Speed", gvMonolithSpeeds[lCount%5]*RandFloat(0.04f,0.06f));
        			SetLocalVarString(sName+"Axis","AreaMonolithRotAxis_"+lAxis);

        			lAxis++;
        			lCount++;
        			if(lAxis > 3) lAxis = 1;
        		}
        	}

        	PlayMonolithSound(1);

        	SetFlyingMonolithsSpeed(1.0f);
        }

        void SetFlyingMonolithsSpeed(float afSpeed)
        {
        	for(int type=1; type<=2; ++type)
        	{
        		for(int idx=1; idx<=gvMonolithTypeNum[type-1]; ++idx)
        		{
        			string sName = "monolith0"+type+"_"+idx;

        			float fLocalSpeed = GetLocalVarFloat(sName+"Speed");
        			string sAxis = GetLocalVarString(sName+"Axis");

        			RotatePropToSpeed(sName, 0.7f, fLocalSpeed*afSpeed , 0, 1, 0, false, sAxis);
        		}
        	}
        }
        int g_iSound;
        void PlayMonolithSound(int iSound)
        {
        	StopSound("mono_sound1", 0.5f);
        	StopSound("mono_sound2", 1.0f);

        	g_iSound = iSound;

        	AddTimer("mono_sound1", 0.51f, nameof(TimerPlayMSound));
        	AddTimer("mono_sound2", 1.01f, nameof(TimerPlayMSound));

        	AddDebugMessage("Playing monolith sound: "+iSound,false);
        }
        void TimerPlayMSound(string asTimer)
        {
        	if(asTimer == "mono_sound1")
        		PlaySoundAtEntity(asTimer, "29_monolith_"+g_iSound, "monolith01_14", 0.0f, true);
        	else
        		PlaySoundAtEntity(asTimer, "29_monolith02_"+g_iSound, "monolith01_14", 0.0f, true);
        }
        //-----------------------------------


        ///////////////////////////////
        // COLLIDE WITH BLUE FIRE
        ///////////////////////////////

        //-----------------------------------

        void CollideFireDamage(string asParent, string asChild, int alState)
        {
        	AddPlayerBodyForce(0,0,9000*2.5f, false);
        	GivePlayerDamage(5, "BloodSplat", true, false);
        	PlayGuiSound("ui_fire_damage.ogg", 0.7f);
        }

        //-----------------------------------

        //TOUCH BLUE FIRE
        void InteractBlueFire(string asEntity)
        {
        	CollideFireDamage("", "", 1);

        	AddTimer(asEntity, 1.0f, nameof(TimerBlueFire));
        }
        void TimerBlueFire(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractBlueFire), true);
        }


        //////////////////////////////
        //USE HEAD EVENT
        void CollideAreaHead(string asParent, string asChild, int alState)
        {
        	SetLocalVarString("WhatHeadArea", asChild);

        	AddDebugMessage("In area: " +asChild, false);
        }


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("OrbChamber");

        	ClearSavedMaps();

        	///////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("lantern", "lantern.ent");

        		GiveItemFromFile("agrippa_head_1", "agrippa_head.ent");
        	}

        	////////////////
        	// Init effects
        	InitFlyingMonoliths();

        	////////////////
        	// Vars
        	SetLocalVarFloat("PlayerDefuaultSpeed", 1); //Used when lowering speed when tipping rods.
        	SetLocalVarFloat("PortalCountDownMul", 1);

        	////////////////
        	// Start alexander talking
        	AddTimer(nameof(TimerAlexanderStartSpeak), 4, nameof(TimerAlexanderStartSpeak));

        	////////////////
        	// Init Look At
        	//NOTE: Skip this and do it after arrive instead!
        	//AddTimer("PlayerLookAtRod", 3,"TimerPlayerLookAtRod");

        	////////////////
        	// Use Item callbacks
        	AddUseItemCallback("HeadOnPortal1", "agrippa_head_1", "orb_altar_1", nameof(UseHeadOnPortal), false);
        	AddUseItemCallback("HeadOnPortal1", "agrippa_head_1", "AreaPortalInteractArea", nameof(UseHeadOnPortal), false);

        	////////////////
        	// Collide callbacks
        	for(int i=1; i<=3; ++i)
        		AddEntityCollideCallback("Player","AreaFireDamage_"+i, nameof(CollideFireDamage), false, 1);

        	////////////////
        	// Set up rods
        	for(int i=0; i<3; ++i)
        	{
        		int lNum = i+1;

        		SetLocalVarInt("orb_room_rod_"+lNum+"_idx", i);
        		AddEntityCollideCallback("orb_room_rod_"+lNum, "AreaRodTippingPoint_"+lNum, nameof(CollideRodTippingPoint), true, 1);
        		AddEntityCollideCallback("orb_room_rod_"+lNum, "AreaRodHitGround_"+lNum, nameof(CollideRodHitGround), true, 1);
        	}


        	//USING HEAD
        	for(int i=1;i<=4;i++)
        		AddEntityCollideCallback("Player", "AreaHead_"+i, nameof(CollideAreaHead), false, 1);

        	///////////////////////////
        	// Ending Debug
        	if(ScriptDebugOn())
        	{
        		//StartAlexanderEnding();
        		//StartAgrippaEnding();
        		//StartDanielEnding();

        		gbShowCredits = true;
        	}
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PlayMusic("29_amb.ogg", true, 0.7f, 5, 0, false);

        	PreloadSound("attack_claw_hit");	PreloadSound("water_lurker_eat_rev2");	PreloadSound("29_hole_open");
        	PreloadSound("29_monolith_3.snt"); PreloadSound("29_monolith_4.snt"); PreloadSound("29_monolith02_1.snt"); PreloadSound("29_monolith02_2.snt");
        	PreloadSound("25_slime_loop.snt"); PreloadSound("guardian_distant2.snt"); PreloadSound("guardian_distant3.snt"); PreloadSound("guardian_ontop.snt");
        	PreloadSound("29_scream.snt"); PreloadSound("25_guardian_slime_appear.snt"); PreloadSound("slime_create.snt"); PreloadSound("29_daniel_footsteps.snt");
        	PreloadSound("29_crumble.snt"); PreloadSound("29_exit_door.snt"); PreloadSound("29_monolith_1.snt"); PreloadSound("29_monolith_2.snt");
        	PreloadSound("guardian_distant1.snt"); PreloadSound("29_amb_guard.snt"); PreloadSound("29_elec_burst02.snt"); PreloadSound("29_elec_burst.snt");
        	PreloadSound("29_pillar_fall.snt"); PreloadSound("29_pillar_hit.snt"); PreloadSound("29_hole_open.snt"); PreloadSound("29_hole_opened02.snt");
        	PreloadSound("29_wind.snt"); PreloadSound("29_hole_blue.snt"); PreloadSound("29_hole_close.snt"); //PreloadSound("29_cough.snt");
        	PreloadSound("29_monolith02_3.snt");  PreloadSound("29_monolith02_4.snt"); PreloadSound("21_meat_long.snt"); PreloadSound("21_meat.snt");

        	CreateEntityAtArea("preloadhead", "agrippa_head_obj.ent", "AreaRandGuard_1", false);

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
