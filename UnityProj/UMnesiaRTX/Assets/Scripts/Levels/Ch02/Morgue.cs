using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/19_morgue.hps; journal: docs/ScriptPort/ch02/19_morgue.md.</summary>
    [HplMapScript("maps/main/ch02/19_morgue.map")]
    public class Morgue : HplScript
    {
        ////////////////////////////////////////
        // Drill Corpse
        void UseDrillOnCorpseHeadArea(string asItem, string asEntity)
        {
        	if(GetLocalVarInt("CorpseDrilled")==0)
        	{
        		AddDebugMessage("Drilled in corpse head!", false);
        		AddTimer("DrillHead", 0.1f, nameof(TimerDrillHead));
        		RemoveItem("handdrill");

        		StartPlayerLookAt("AreaCorpseHead", 1.5f, 3, "");
        		SetPlayerMoveSpeedMul(0.3f);
        		SetRadialBlurStartDist(0.1f);
        		FadeRadialBlurTo(0.05f, 0.025f);
        	}
        	else
        	{
        		SetMessage("Ch02Level19", "DrillCorpse_AlreadyDone", 0);
        	}
        }

        void TimerDrillHead(string asTimer)
        {
        	SetEntityActive("antidote_corpse_drilled",true);
        	SetPropActiveAndFade("antidote_corpse",false, 0.3f);

        	CreateParticleSystemAtEntity("bloodSplash", "ps_blood_tiny_splash.ps", "AreaDrillCenter",false);

        	SetPropActiveAndFade("hand_drill_static_1",true, 0.3f);
        	RotatePropToSpeed("hand_drill_static_1", 8.0f, 8.0f,  1.0f, 0, 0 ,  true, "AreaDrillCenter");
        	PlaySoundAtEntity("drilling", "19_drill.snt", "AreaDrillCenter", 0.5f, false);

        	AddTimer("StopDrillHead", 1.5f, nameof(TimerStopDrillHead));
        }

        void TimerStopDrillHead(string asTimer)
        {
        	SetPlayerMoveSpeedMul(1.0f);
        	StopPlayerLookAt();
        	FadeRadialBlurTo(0, 0.015f);

        	SetEntityPlayerInteractCallback("AreaCorpseHead", nameof(InteractCorpseHeadArea), false);

        	CreateParticleSystemAtEntity("bloodStream", "ps_blood_tiny_stream.ps", "AreaDrillCenter",true);
        	CreateParticleSystemAtEntity("bloodStreamImpact", "ps_blood_tiny_stream_impact02.ps","AreaBloodImpact",true);
        	PlaySoundAtEntity("bloodStream","19_pour_blood.snt", "AreaDrillCenter", 0, true);

        	StopPropMovement("hand_drill_static_1");
        	SetPropActiveAndFade("hand_drill_static_1",false, 0.3f);

        	AddTimer("nomoredrill", 1.5f, nameof(TimerNoMoreDrill));

        	SetLocalVarInt("CorpseDrilled", 1);
        }

        void TimerNoMoreDrill(string asTimer)
        {
        	PlayGuiSound("19_attach_needle", 0.7f);

        	SetPropActiveAndFade("hand_drill_part0*_static_2", true, 1.0f);

        	SetMessage("Ch02Level19", "DrillBroke", 0);
        }

        void UseDrillOnCorpseBodyArea(string asItem, string asEntity)
        {
        	if(GetLocalVarInt("CorpseDrilled")==0)
        	{
        		SetMessage("Ch02Level19", "DrillCorpseBody", 0);
        	}
        	else
        	{
        		SetMessage("Ch02Level19", "DrillCorpse_AlreadyDone", 0);
        	}
        }


        ////////////////////////////////////////
        // Use Tube-Need in Head
        void UseTubeNeedleOnHead(string asItem, string asEntity)
        {
        	if(GetLocalVarInt("CorpseDrilled")==0)
        	{
        		SetMessage("Ch02Level19", "UseTubeNeedleOnHead_NoBlood", 0);
        		PlayGuiSound("impact_generic_soft_med", 0.7f);
        	}
        	else
        	{
        		RemoveItem(asItem);

        		CompleteQuest("19Pipe", "19Pipe");

        		if(GetLocalVarInt("TubeInPlace")==0)
        		{
        			PlaySoundAtEntity("attach","19_attach_pipe.snt", "AreaDrillCenter", 0, false);
        			PlaySoundAtEntity("bloodStream2","19_pour_blood_low.snt", "AreaDrillCenter", 0, true);

        			DestroyParticleSystem("bloodStream");
        			DestroyParticleSystem("bloodStreamImpact");
        			StopSound("bloodStream", 0.5f);
        		}
        		else
        		{
        			SetEntityActive("copper_tube_skull", false);
        			DestroyParticleSystem("bloodStreamTube");
        		}

        		SetEntityActive("copper_tube_and_needle_1", true);
        		CreateParticleSystemAtEntity("bloodStreamNeedle", "ps_blood_tiny_stream_needle.ps", "AreaNeedleTip",true);

        		SetLocalVarInt("TubeNeedleInPlace", 1);
        	}
        }

        void UseTubeOnHead(string asItem, string asEntity)
        {
        	//ADD SO THAT TUBE ONLY CAN BE INSERTED INTO HEAD HOLE, THEN NEEDLED ADDED TO IT
        	//IF SO ALSO CompleteQuest("19Pipe", "19Pipe");
        	//Added a UseHandOnTube for hint it trying to interact with the Tube only before adding needle.

        	if(GetLocalVarInt("CorpseDrilled")==0){
        		SetMessage("Ch02Level19", "UseTubeOnHead_NoBlood", 0);
        		PlayGuiSound("impact_generic_soft_med", 0.7f);
        	}
        	else{
        		RemoveItem(asItem);

        		DestroyParticleSystem("bloodStream");
        		DestroyParticleSystem("bloodStreamImpact");

        		PlaySoundAtEntity("attach","19_attach_pipe.snt", "AreaDrillCenter", 0, false);

        		StopSound("bloodStream", 0.5f);
        		PlaySoundAtEntity("bloodStream2","19_pour_blood_low.snt", "AreaDrillCenter", 0, true);

        		SetEntityActive("copper_tube_skull", true);
        		CreateParticleSystemAtEntity("bloodStreamTube", "ps_blood_tiny_stream_tube.ps", "AreaTubeTip",true);

        		SetLocalVarInt("TubeInPlace", 1);

        		//SetMessage("Ch02Level19", "UseTubeOnHead_Blood", 0);
        		//PlayGuiSound("impact_generic_soft_med", 0.7f);
        	}
        }

        void UseNeedleOnHead(string asItem, string asEntity)
        {
        	if(GetLocalVarInt("CorpseDrilled")==0){
        		SetMessage("Ch02Level19", "UseNeedleOnHead_NoBlood", 0);
        		PlayGuiSound("impact_generic_soft_med", 0.7f);
        	}
        	else{
        		if(GetLocalVarInt("TubeInPlace")==0)
        		{
        			SetMessage("Ch02Level19", "UseNeedleOnHead_Blood", 0);
        			PlayGuiSound("impact_generic_soft_med", 0.7f);
        		}
        		else
        		{
        			UseTubeNeedleOnHead(asItem, asEntity);
        			PlayGuiSound("switch_min_max01.ogg", 1);
        		}
        	}
        }

        ///////////////////////////////////
        //CORPSE HINT
        void UseTubeNeedleOnBody(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level19", nameof(UseTubeNeedleOnBody), 0);

        	PlayGuiSound("impact_generic_soft_med", 0.7f);
        }

        void InteractCorpseHint(string asEntity)
        {
        	PlayGuiSound("touch_corpse", 0.3f);
        	SetMessage("Ch02Level19", "TouchCorpse", 5.0f);

        	AddQuest("19Corpse", "19Corpse");

        	AddTimer("1", 0.1f, nameof(TimerInteractSounds));
        	AddTimer("2", 0.4f, nameof(TimerInteractSounds));

        	if(GetLocalVarInt("CorpseDrilled") == 1) return;
        	AddTimer(asEntity, 1.0f, nameof(TimerInteractSounds));

        	//string sDisable = asEntity == "AreaCorpseHead" ? "AreaCorpseBody" : "AreaCorpseHead";
        	//SetEntityPlayerInteractCallback(sDisable, "InteractCorpseHint", true);
        }

        void TimerInteractSounds(string asTimer)
        {
        	if(asTimer=="1")
        		PlayGuiSound("touch_corpse", 1.0f);
        	if(asTimer=="2")
        		PlayGuiSound("touch_corpse", 0.7f);

        	else SetEntityPlayerInteractCallback(asTimer, nameof(InteractCorpseHint), true);
        }

        ////////////////////////////////////////
        // Get Antidote

        void InteractCorpseHeadArea(string asEntity)
        {
        	if(GetLocalVarInt("TubeNeedleInPlace")==0)
        	{
        		if(GetLocalVarInt("TubeInPlace")==0)
        			SetMessage("Ch02Level19", "InteractBloodStream", 0);
        		else
        			SetMessage("Ch02Level19", "UseHandOnTube", 0);

        		PlayGuiSound("impact_organic_low", 0.3f);
        	}
        	else
        	{
        		//SetEntityActive("AreaCorpseHead", false);

        		CompleteQuest("19Corpse", "19Corpse");

        		SetGlobalVarInt("PlayerHasAntidote", 1); //Setup global var sp player no survives poison in 16.

        		StartPlayerLookAt("AreaCorpseHead", 1, 3, "");
        		SetPlayerMoveSpeedMul(0.3f);
        		SetRadialBlurStartDist(0.1f);
        		FadeRadialBlurTo(0.1f, 2);
        		FadePlayerFOVMulTo(0.3f, 0.6f);

        		PlayGuiSound("19_inject.snt", 1.0f);

        		StopSound("bloodStream2", 1.0f);

        		AddTimer("InjectSounds", 1.15f, nameof(TimerInjectSounds));
        		FadeOut(1.06f);
        	}
        }

        void TimerInjectSounds(string asTimer)
        {
        	PlayGuiSound("19_creak.snt", 1.0f);
        	PlayGuiSound("sanity_heartbeat", 0.5f);

        	PlayMusic("19_puzzle_inject.ogg", false,1, 1, 10, false);

        	AddTimer("1", 1.2f, nameof(TimerInjectSounds02));
        	AddTimer("2", 2.4f, nameof(TimerInjectSounds02));
        	AddTimer("3", 3.6f, nameof(TimerInjectSounds02));
        	AddTimer("4", 4.8f, nameof(TimerInjectSounds02));

        	SetPlayerMoveSpeedMul(1.0f);
        	StopPlayerLookAt();
        	FadeRadialBlurTo(0, 0.015f);
        	FadePlayerFOVMulTo(1, 2);

        	ResetProp("cellar_wood01_2");

        	SetPlayerActive(false);

        	DestroyParticleSystem("bloodStreamNeedle");

        	AddTimer("InjectSoundsOver", 5.0f, nameof(TimerInjectSoundsOver));
        }

        void TimerInjectSounds02(string asTimer)
        {
        	if(asTimer == "1")
        		PlayGuiSound("react_breath_slow", 0.3f);
        	else if(asTimer == "2")
        		PlayGuiSound("sanity_heartbeat", 0.7f);
        	else if(asTimer == "3")
        		PlayGuiSound("react_breath_slow", 0.5f);
        	else
        		PlayGuiSound("sanity_heartbeat", 1.0f);
        }
        void TimerInjectSoundsOver(string asTimer)
        {
        	AddTimer("CompleteQuest", 3.0f, nameof(TimerCompleteAntidoteQuest));

        	if(GetPlayerSanity() > 40)
        	{
        		SetPlayerSanity(40);
        		FadeImageTrailTo(0.9f, 1);
        		AddTimer("ImageTrailOver", 7.0f, nameof(TimerImageTrailOver));
        	}

        	PlayGuiSound("react_pant", 0.5f);
        	PlayGuiSound("03_in_a_bottle.snt", 0.5f);

        	SetPlayerActive(true);
        	FadeIn(4.0f);

        	SetEntityPlayerInteractCallback("AreaCorpseBody", nameof(InteractCorpseAllDone), true);
        	SetEntityPlayerInteractCallback("AreaCorpseHead", nameof(InteractCorpseAllDone), true);
        }

        void InteractCorpseAllDone(string asEntity)
        {
        	SetMessage("Ch02Level19", "TouchCorpseAllDone", 0);

        	AddTimer("1", 0.1f, nameof(TimerInteractSounds));
        	AddTimer("2", 0.4f, nameof(TimerInteractSounds));

        	AddTimer("intback", 1.0f, nameof(TimerIntBackOn));
        }

        void TimerIntBackOn(string asTimer)
        {
        	SetEntityPlayerInteractCallback("AreaCorpseBody", nameof(InteractCorpseAllDone), true);
        	SetEntityPlayerInteractCallback("AreaCorpseHead", nameof(InteractCorpseAllDone), true);
        }

        void TimerImageTrailOver(string asTimer)
        {
        	FadeImageTrailTo(0, 0.3f);
        }

        void TimerCompleteAntidoteQuest(string asTimer)
        {
        	CompleteQuest("FindAntidote", "16FindAntidote");
        	SetEntityActive("AreaShowBrute", true);
        }


        ////////////////////////////////////////
        // Brute Appears
        void CollideShowBrute(string asParent, string asChild, int alState)
        {
        	AddTimer("PoundDoor", 0.5f, nameof(TimerBrutePoundDoor));

        	CheckPoint("check01","PlayerStartArea_3", nameof(CheckPoint01), "Hints", "DeathBrute_18_Morgue_01");

        	SetEntityPlayerInteractCallback("cellar_wood01_2", nameof(InteractCellarDoor), true);

        	PlaySoundAtEntity("activate", "enabled", "cellar_wood01_2", 0.0f, false);
        }

        void InteractCellarDoor(string asEntity)
        {
        	ActivatePoundingBrute();
        }

        void ActivatePoundingBrute()
        {
        	RemoveTimer("PoundDoor");

        	SetEntityPlayerInteractCallback("cellar_wood01_2", "", true);

        	SetPropHealth("cellar_wood01_2", 0);

        	ClearEnemyPatrolNodes("servant_brute_1");
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_42", 3, "");
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_40", 5, "");
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_43", 5, "");
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_36", 3, "");
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_11", 2, "");
        	SetEntityActive("servant_brute_1", true);
        }

        void TimerBrutePoundDoor(string asTimer)
        {
        	if(GetLocalVarInt("PoundDoorCount")==4)
        	{
        		ActivatePoundingBrute();

        		return;
        	}

        	if(GetLocalVarInt("PoundDoorCount")==0)
        	{
        		PlayMusic("19_event_brute.ogg", false,1, 0.1f, 10, false);

        		GiveSanityDamage(10, true);
        		StartPlayerLookAt("cellar_wood01_2", 3, 5, "");
        		AddTimer("StopHeadMove", 0.75f, nameof(TimerStopHeadMove));
        	}

        	if(GetLocalVarInt("PoundDoorCount")==1)
        		PlaySoundAtEntity("mumble", "amb_idle", "cellar_wood01_2", 0.0f, false);

        	AddBodyImpulse("cellar_wood01_2_Body_1", 0, 0,5, "World");
        	PlaySoundAtEntity("Bang","break_wood.snt", "AreaDoorBang", 0, false);
        	CreateParticleSystemAtEntity("Bang", "ps_break_wood_small.ps", "AreaDoorBang", false);


        	AddLocalVarInt("PoundDoorCount", 1);
        	AddTimer("PoundDoor", 3.5f, nameof(TimerBrutePoundDoor));
        }

        void TimerStopHeadMove(string asTimer)
        {
        	StopPlayerLookAt();
        }

        void CollideShowBrute02(string asParent, string asChild, int alState)
        {
        	ClearEnemyPatrolNodes("servant_brute_1");
        	SetEntityActive("servant_brute_2", true);
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_16", 3, "");
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_12", 3, "");
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_17", 3, "");

        	CheckPoint("check02","PlayerStartArea_4", nameof(CheckPoint02), "Hints", "DeathBrute_18_Morgue_02");
        }

        void CheckPoint01(string asName, int alCount)
        {
        	SetEntityActive("AreaShowBrute02", true);
        	ResetProp("cellar_wood01_2");
        }

        void CheckPoint02(string asName, int alCount)
        {
        	SetEntityActive("slime*", true);
        	SetEntityActive("antidote_corpse_4", true);

        	SetEntityActive("SlimeDamageArea_1", true);
        	SetEntityActive("SlimeDamageArea_2", true);

        	StopSound("Sound_19", 4.0f);
        	PlaySoundAtEntity("slimea", "amb_guardian", "slime_anim_wall_1", 2.0f, true);
        	PlaySoundAtEntity("slime2", "slime_loop", "slime_pile_4", 2.0f, true);
        	PlaySoundAtEntity("slime3", "slime_loop", "slime_6way_1", 2.0f, true);
        	PlaySoundAtEntity("slime1", "slime_loop", "slime_egg_1", 2.0f, true);

        	PlayMusic("04_event_hole.ogg", false,1, 0.1f, 10, false);
        	FadeLightTo("PointLight_24", 0.6f, 0,0, 0.55f, -1, 1);
        	ResetProp("cellar_wood01_2");

        	AddTimer("GuardianScream", 0.5f, nameof(TimerGuardianScream));
        	AddTimer("1", 1.0f, nameof(TimerGuardianScream));
        	AddTimer("2", 2.0f, nameof(TimerGuardianScream));
        	AddTimer("3", 5.0f, nameof(TimerGuardianScream));
        }

        void TimerGuardianScream(string asTimer)
        {
        	if(asTimer == "1"){
        		PlaySoundAtEntity("react_breath1", "react_breath", "Player", 0.5f, false);
        		FadePlayerFOVMulTo(0.5f, 0.1f);
        		return;
        	}
        	else if(asTimer == "2"){
        		PlaySoundAtEntity("react_scare1", "react_scare", "Player", 0.3f, false);
        		return;
        	}
        	else if(asTimer == "3"){
        		FadePlayerFOVMulTo(1.0f, 0.01f);
        		return;
        	}
        	PlaySoundAtEntity("howl","guardian_distant1", "Player", 0, false);
        }


        ///////////////////
        //ALEXANDER VOICE
        void VisionOver01()
        {
        	SetEntityActive("AreaAlexVoice", true);
        }
        void CollideAlexVoice(string asParent, string asChild, int alState)
        {
        	AddTimer("voicedelay", 4.0f, nameof(TimerAlexVoice));
        }
        void TimerAlexVoice(string asTimer)
        {
        	AddEffectVoice("CH02L19_Alexander_01", "", "Voice", "CH02L19_Alexander_01", false, "", 0.0f, 0.0f);
        }


        ///////////////////////////
        //LIGHT OPTIMIZATION
        void CollideLights(string asParent, string asChild, int alState)
        {
        	if(alState == 1){
        		FadeLightTo("SpotLight_1", 0.8f, 0.7f, 0.6f, 0.5f, -1, 0);
        		FadeLightTo("SpotLight_8", 0.6f, 0.6f, 0.5f, 0.6f, -1, 0);
        		FadeLightTo("SpotLight_7", 0.4f, 0.4f, 0.4f, -1, -1, 0);
        		FadeLightTo("SpotLight_3", 0.3f, 0.3f, 0.3f, -1, -1, 0);
        		FadeLightTo("SpotLight_4", 0.3f, 0.3f, 0.3f, -1, -1, 0);
        		FadeLightTo("SpotLight_5", 0.3f, 0.3f, 0.3f, -1, -1, 0);
        		FadeLightTo("SpotLight_6", 0.3f, 0.3f, 0.3f, -1, -1, 0);

        		FadeLightTo("SpotLight_2", 0, 0, 0, 0, -1, 0);
        		FadeLightTo("SpotLight_10", 0, 0, 0, -1, -1, 0);

        	} else{
        		FadeLightTo("SpotLight_1", 0, 0, 0, 0, -1, 0);
        		FadeLightTo("SpotLight_8", 0, 0, 0, 0, -1, 0);
        		FadeLightTo("SpotLight_7", 0, 0, 0, -1, -1, 0);
        		FadeLightTo("SpotLight_3", 0, 0, 0, -1, -1, 0);
        		FadeLightTo("SpotLight_4", 0, 0, 0, -1, -1, 0);
        		FadeLightTo("SpotLight_5", 0, 0, 0, -1, -1, 0);
        		FadeLightTo("SpotLight_6", 0, 0, 0, -1, -1, 0);

        		FadeLightTo("SpotLight_2", 0.3f, 0.3f, 0.2f, 1, -1, 0);
        		FadeLightTo("SpotLight_10", 0.4f, 0.4f, 0.3f, -1, -1, 0);
        	}

        }
        ////////////////////////


        //////////////////////////////
        //Sanity drain in corpse rooms
        void CollideAreaRooms(string asParent, string asChild, int alState)
        {
        	if(alState == 1){
        		AddTimer(asChild, RandFloat(0.2f,2.0f), nameof(TimerEnterAreaRooms));
        		PlaySoundAtEntity(asChild+"terror", "ui_torture.ent", asChild, 10, true);
        	}
        	else if(alState == -1){
        		RemoveTimer(asChild);
        		FadeRadialBlurTo(0.0f, 0.1f);
        		StopSound(asChild+"terror", 2);
        		AddDebugMessage("Leave", false);
        	}
        }
        void TimerEnterAreaRooms(string asTimer)
        {
        	if(GetLocalVarInt("Sanity") == 0){
        		FadeRadialBlurTo(0.1f, 0.2f);
        		SetRadialBlurStartDist(0.7f);
        		SetLocalVarInt("Sanity", 1);
        		PlayGuiSound("react_sigh.snt", 0.5f);
        	}
        	else{
        		FadeRadialBlurTo(0.0f, 0.1f);
        		SetLocalVarInt("Sanity", 0);
        		PlayGuiSound("react_breath.snt", 0.5f);
        	}

        	GiveSanityDamage(2.5f, false);

        	PlaySoundAtEntity("E_"+asTimer+"s", "scare_baby_cry.snt", "E_"+asTimer+"_"+RandInt(1,5), RandFloat(0.1f, 4.0f), false);

        	AddTimer(asTimer, RandFloat(6.0f,12.0f), nameof(TimerEnterAreaRooms));
        }
        /////////////////


        //GOT THE COPPER TUBE
        void EntityCallPickCopper(string asEntity, string type)
        {
        	SetEntityActive("AreaCopperEvent", true);

        	AddQuest("19Pipe", "19Pipe");
        }
        void CollideAreaCopperEvent(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("creak", "00_creak.snt", "Player", 6.0f, true);
        	PlaySoundAtEntity("metal", "15_amb_metal", "Player", 6.0f, true);

        	StartScreenShake(0.002f, 2.0f, 5.0f, 5.0f);
        	FadePlayerAspectMulTo(1.8f, 0.02f);
        	GiveSanityDamage(5.0f, false);

        	AddTimer(nameof(TimerECopper), 2.0f, nameof(TimerECopper));
        }
        void TimerECopper(string asTimer)
        {
        	string sEvent = asTimer;	//Do not edit, sets timer loop function name.
        	AddLocalVarInt(sEvent, 1);	//Do not edit, steps through timer loop events.
        	bool bPauseAtStep = false;	//Do not edit, to pause or end the timer loop at specified step.

        	float fEventSpeed = 2.0f;	//Set to preferred default time for the timer loop.

        	switch(GetLocalVarInt(sEvent)){
        		//////////
        		//First step in the event - replace with step comment.
        		case 1:
        			FadeOut(8.0f);
        			PlayGuiSound("react_breath", 0.3f);
        			FadeImageTrailTo(0.1f, 0.1f);
        			SetPlayerRunSpeedMul(0.8f);
        			SetPlayerMoveSpeedMul(0.8f);
        		break;
                case 2:
        			PlayGuiSound("react_breath", 0.5f);
        			FadeImageTrailTo(0.3f, 0.3f);
        			SetPlayerRunSpeedMul(0.7f);
        			SetPlayerMoveSpeedMul(0.7f);
        		break;
        		case 3:
        			PlayGuiSound("react_breath", 0.7f);
        			StopSound("creak", 8.0f);
        			StopSound("metal", 10.0f);
        			FadeImageTrailTo(0.5f, 0.5f);
        			FadePlayerAspectMulTo(1.0f, 0.05f);
        			SetPlayerRunSpeedMul(0.6f);
        			SetPlayerMoveSpeedMul(0.6f);
        		break;
        		case 4:
        			FadeIn(8.0f);
        			PlayGuiSound("react_breath", 0.5f);
        			FadeImageTrailTo(0.3f, 0.3f);
        			SetPlayerRunSpeedMul(0.7f);
        			SetPlayerMoveSpeedMul(0.7f);
        		break;
        		case 5:
        			PlayGuiSound("react_breath", 0.3f);
        			FadeImageTrailTo(0.1f, 0.1f);
        			SetPlayerRunSpeedMul(0.85f);
        			SetPlayerMoveSpeedMul(0.85f);
        		break;
        		//////////
        		//End timer when no more steps in the event found.
        		default:
        			FadeImageTrailTo(0.0f, 0.0f);
        			SetPlayerRunSpeedMul(1.0f);
        			SetPlayerMoveSpeedMul(1.0f);
        			bPauseAtStep = true;
        		break;
        	}

        	if(!bPauseAtStep) AddTimer(sEvent, fEventSpeed, sEvent);
        }
        /////////////////////



        //Flashback 01 from survivor over
        void SurvivorEnd()
        {
        	AddTimer("FlashbackArea_3", 10.0f, nameof(TimerSecondSurvivorFlash));
        }
        void TimerSecondSurvivorFlash(string asTimer)
        {
        	SetEntityActive(asTimer, true);
        }


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("Morgue");

        	////////////////////////////////////////
        	// Use item callbacks
        	AddUseItemCallback("usedrillhead", "handdrill", "AreaCorpseHead", nameof(UseDrillOnCorpseHeadArea), false);
        	AddUseItemCallback("usedrillhead", "handdrill", "AreaCorpseBody", nameof(UseDrillOnCorpseHeadArea), false);
        	//AddUseItemCallback("usedrillbody", "handdrill", "AreaCorpseBody", "UseDrillOnCorpseBodyArea", false);

        	AddUseItemCallback("use_tube_and_needle_on_head", "copper_tube_and_needle", "AreaCorpseHead", nameof(UseTubeNeedleOnHead), false);
        	AddUseItemCallback("use_tube_on_head", "copper_tube_1", "AreaCorpseHead", nameof(UseTubeOnHead), false);
        	AddUseItemCallback("use_needle_on_head", "hollow_needle_1", "AreaCorpseHead", nameof(UseNeedleOnHead), false);

        	//AddUseItemCallback("use_tube_and_needle_on_body", "copper_tube_and_needle", "AreaCorpseBody", "UseTubeNeedleOnBody", false);
        	//AddUseItemCallback("use_tube_on_body", "copper_tube_1", "AreaCorpseBody", "UseTubeNeedleOnBody", false);
        	AddUseItemCallback("use_tube_and_needle_on_body", "copper_tube_and_needle", "AreaCorpseBody", nameof(UseTubeNeedleOnHead), false);
        	AddUseItemCallback("use_tube_on_body", "copper_tube_1", "AreaCorpseBody", nameof(UseTubeOnHead), false);

        	AddUseItemCallback("use_needle_on_body", "hollow_needle_1", "AreaCorpseBody", nameof(UseNeedleOnHead), false);


        	/////////////////////////////////////
        	//PLAYER INTERACT
        	SetEntityPlayerInteractCallback("AreaCorpseHead", nameof(InteractCorpseHint), true);
        	SetEntityPlayerInteractCallback("AreaCorpseBody", nameof(InteractCorpseHint), true);


        	////////////////////////////////////////
        	// Collisions
        	AddEntityCollideCallback("Player", "AreaShowBrute", nameof(CollideShowBrute), true, 1);
        	AddEntityCollideCallback("Player", "AreaShowBrute02", nameof(CollideShowBrute02), true, 1);
        	AddEntityCollideCallback("Player", "AreaAlexVoice", nameof(CollideAlexVoice), true, 1);
        	AddEntityCollideCallback("Player", "AreaLights", nameof(CollideLights), false, 0);
        	for(int i=1;i<=3;i++) AddEntityCollideCallback("Player", "AreaRoom_"+i, nameof(CollideAreaRooms), false, 0);
        	AddEntityCollideCallback("Player", "AreaCopperEvent", nameof(CollideAreaCopperEvent), true, 1);

        	////////////////////////////////////////
        	//Debug
        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("copper_tube_1", "copper_tube.ent");
        		GiveItemFromFile("lantern", "lantern.ent");
        		GiveItemFromFile("hollow_needle_1", "hollow_needle.ent");

        		for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

        		GiveItem("handdrill", "Puzzle", "drill", "hand_drill.tga", 0);
        		GiveItem("copper_tube_and_needle", "Puzzle", "copper_tube_and_needle", "copper_tube_and_needle.tga", 0);
        	}
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("19_drill"); PreloadSound("19_pour_blood"); PreloadSound("19_attach_pipe"); PreloadSound("19_pour_blood_low");
        	PreloadSound("enabled"); PreloadSound("amb_idle"); PreloadSound("break_wood"); PreloadSound("amb_guardian");
        	PreloadSound("slime_loop"); PreloadSound("react_breath"); PreloadSound("react_scare"); PreloadSound("guardian_distant1");
        	PreloadSound("scare_baby_cry"); PreloadSound("00_creak"); PreloadSound("15_amb_metal"); PreloadSound("19_attach_needle");
        	PreloadSound("touch_corpse"); PreloadSound("19_inject"); PreloadSound("19_creak"); PreloadSound("sanity_heartbeat");
        	PreloadSound("sanity_heartbeat"); PreloadSound("react_breath_slow"); PreloadSound("03_in_a_bottle"); PreloadSound("react_pant");
        	PreloadSound("react_sigh");

        	PlayMusic("04_amb.ogg", true, 0.8f, 8, 0, true);

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	StopMusic(4, 0);
        }
    }
}
