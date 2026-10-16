using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch03
{
    /// <summary>Port of maps/main/ch03/27_torture_chancel_redux.hps; journal: docs/ScriptPort/ch03/27_torture_chancel_redux.md.</summary>
    [HplMapScript("maps/main/ch03/27_torture_chancel_redux.map")]
    public class TortureChancelRedux : HplScript
    {
        //------------------------------------------

        ///////////////////////////////////////////
        // JAM MACHINE
        ////////////////////////////////////////////

        //------------------------------------------

        /////////////////////////////////////////////
        // Cog + Barrier rotation helper
        void RotateMachineryToSpeed(float afAccel, float afCogSpeed, float afRingSpeed)
        {
        	// Set up machine cogs rotation
        	float fSmallSpeed = afCogSpeed;
        	float fLargeSpeed = afCogSpeed * 1.3333f; //4/3 ratio on wheel size

        	RotatePropToSpeed("barrier_machine_cog_1", afAccel, fSmallSpeed, 1, 0, 0, true, "");
        	RotatePropToSpeed("barrier_machine_cog_3", afAccel, fLargeSpeed, 0, 0, -1, true, "");
        	RotatePropToSpeed("barrier_machine_cog_4", afAccel, fLargeSpeed, 0, 0, 1, true, "");

        	// Set up ring rotation
        	for(int i=2; i<=8; ++i)
        	{
        		float fDir = i%2==0 ? -1 : 1;
        		RotatePropToSpeed("barrier_ring02_"+i, afAccel, afRingSpeed,0,0,fDir, true, "");
        	}
        }

        //------------------------------------------

        void CollideSmallObjectWithCogs(string asParent, string asChild, int alState)
        {
        	if(GetTimerTimeLeft("SmallObjectCogWait") >0) return;

        	SetMessage("Ch03Level27", "TryJamSmallObject", -1);

        	CreateParticleSystemAtEntity("MachineSpark", "ps_cog_stone.ps", asChild, false);

        	//To make the player drop the item.
        	ChangePlayerStateToNormal();

        	//Little force to object so it flies off.
        	AddPropImpulse(asParent, 0, -2, 2, "World");

        	PlaySoundAtEntity("metalbang", "impact_metal_high.snt", asChild, 0, false);
        	AddTimer("SmallObjectCogWait", 1, ""); //Wait 1 second until displaying this message + effect
        }


        //------------------------------------------

        void CollideWeakObjectWithCogs(string asParent, string asChild, int alState)
        {
        	if(GetTimerTimeLeft("WeakObjectCogWait") >0) return;

        	SetMessage("Ch03Level27", "TryJamWeakObject", -1);

        	//If a wood box, then destroy the object
        	if(StringContains(asParent, "wood_box"))
        	{
        		//SetEntityActive(asParent, false);
        		SetPropHealth(asParent, 0);
        		CreateParticleSystemAtEntity("WoodBreak", "ps_break_wood_small.ps", asChild, false);
        		//PlaySoundAtEntity("woodrash", "break_wood_metal.snt", asChild, 0, false);
        	}
        	//If a flask, remove object and display some glass shattering.
        	else if(StringContains(asParent, "flask"))
        	{
        		SetEntityActive(asParent, false);
        		CreateParticleSystemAtEntity("Glass", "ps_glass_shards.ps", asChild, false);
        		CreateParticleSystemAtEntity("Dust", "ps_break_wood_small.ps", asChild, false);
        		PlaySoundAtEntity("glasscrash", "break_glass_bottle.snt", asChild, 0, false);
        	}
        	//Default, just show dust.
        	else
        	{
        		CreateParticleSystemAtEntity("Dust", "ps_break_wood_small.ps", asChild, false);
        	}

        	AddTimer("WeakObjectCogWait", 1, ""); //Wait 1 second until displaying this message + effect
        }

        //------------------------------------------

        void CollideCorrectObjectWithCogs(string asParent, string asChild, int alState)
        {
        	/////////////////////
        	//Complete quests
        	CompleteQuest("27OrbReadyNoJam", "27OrbReadyNoJam");

        	/////////////////////
        	//Effects when destroying.
        	StartScreenShake(0.18f, 0.1f, 1.3f, 2.1f);

        	CreateParticleSystemAtEntity("MachineSpark", "ps_cog_stone.ps", asChild, false);
        	CreateParticleSystemAtEntity("Rubble", "ps_cog_rubble.ps", asChild, false);

        	CreateParticleSystemAtEntity("MachineDestroid", "ps_break_mansionbase_wall.ps", "AreaDestroyEffect", false);
        	CreateParticleSystemAtEntity("MachineDestroid", "ps_break_mansionbase_wall.ps", asChild, false);

        	//To make the player drop the item.
        	ChangePlayerStateToNormal();

        	GivePlayerDamage(10, "BloodSplat", true, false);

        	/////////////////////
        	//make machine cogs slow down and stop
        	RotateMachineryToSpeed(1, 0.2f, 0.2f);
        	AddTimer(nameof(TimerJamMachine), 1, nameof(TimerJamMachine));

        	/////////////////////
        	//Sound effects
        	PlaySoundAtEntity("MachineFail", "27_jam.snt","MachinerySound_1", 0, false);

        	/////////////////////
        	//Turn off areas.
        	SetEntityActive("AreaStoneCogs_*", false);
        	SetEntityActive("AreaCogwheelDamage_*", false);

        	/////////////////////
        	//Turn off particle systems
        	DestroyParticleSystem("ParticleSystem_11");
        	DestroyParticleSystem("ParticleSystem_10");
        	DestroyParticleSystem("ParticleSystem_32");

        	/////////////////////
        	//Turn off steam sounds
        	StopSound("SteamSound_2", 1);
        	StopSound("SteamSound_3", 1);

        	StopSound("MachineSound_4", 0.2f);

        	StopSound("Sound_3", 0.0f);
        	StopSound("Sound_16", 0.0f);

        	StopSound("HumSound_1", 4); StopSound("HumSound_3", 4);
        	StopSound("HumSound_2", 4); StopSound("HumSound_4", 4);

        	/////////////////////
        	//Music playing
        	AddTimer(nameof(TimerMachineJamMusic),2,nameof(TimerMachineJamMusic));

        	////////////////////////////
        	//Grunt event on way back
        	EventGrunt02();
        }

        void TimerMachineJamMusic(string asTimer)
        {
        	PlayMusic("27_event_bang.ogg", false, 1, 0.5f, 9, false);
        }

        //------------------------------------------

        void TimerRestoreMachineRotation(string asTimer)
        {
        	if(GetLocalVarInt("MachineJammed")==0)
        		RotateMachineryToSpeed(0.9f, 2.1f, 1);
        }

        void TimerJamMachine(string asTimer)
        {
        	PlaySoundAtEntity("MachineDestroyed", "27_break.snt", "MachinerySound_1", 0, false);

        	SetLocalVarInt("MachineJammed", 1);
        	UpdateBarrierState();

        	RotateMachineryToSpeed(1, 0, 0);
        }

        void CollidePlayerCog(string asParent, string asChild, int alState)
        {
        	PlayGuiSound("21_meat_snap02.ogg", 1.0f);
        	PlayGuiSound("21_meat_long04.ogg", 1.0f);
        	PlayGuiSound("attack_claw_hit02.ogg", 0.3f);

        	GivePlayerDamage(10.0f, "Slash", true, false);
        }
        void InteractCogWheel(string asEntity)
        {
        	AddDebugMessage(asEntity + " being interacted!", false);
        	if(GetTimerTimeLeft("TimerInteractCogWheel")>0)
        		return;

        	CollidePlayerCog("","",1);

        	AddTimer("TimerInteractCogWheel", 1.0f, "");
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // BUILD ORB
        ////////////////////////////////////////////

        //------------------------------------------

        void UseStringOnPedestal(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level21", nameof(UseStringOnPedestal), 0);
        }

        //------------------------------------------

        void UseOrbPieceOnPedestal(string asItem, string asEntity)
        {
        	////////////////////
        	//Check if tar has been added
        	if(GetGlobalVarInt("TarOnPedestal")==0 && GetGlobalVarInt("OrbsAddedCount")==1)
        	{
        		SetMessage("Ch02Level22", "UseOrbPieceAndNoTar", 0);
        		AddQuest("22NeedTar", "22NeedTar");
        		PlaySoundAtEntity("prbd", "22_orb_loose.snt", asEntity, 1.0f, false);
        		return;
        	}

        	////////////////////
        	//Increase count and remove item
        	AddGlobalVarInt("OrbsAddedCount",1);
        	int lCurrentCount = GetGlobalVarInt("OrbsAddedCount");
        	RemoveItem(asItem);

        	////////////////////
        	//Make the orb piece visible

        	//All orbs after first forge.
        	if(lCurrentCount > 1)
        	{
        		SetPropActiveAndFade("orb_assembly_"+lCurrentCount, true, 1.0f);
        		AddTimer("orb_assembly_"+(lCurrentCount-1), 0.8f, nameof(TimerFadeProp));
        	}
        	//First should just pop up.
        	else
        	{
        		SetEntityActive("orb_assembly_"+lCurrentCount, true);
        	}

        	////////////////////
        	//Play sound
        	PlaySoundAtEntity("orbd", "22_orb_loose.snt", asEntity, 0.5f, false);

        	////////////////////
        	//Forge Effect
        	if(lCurrentCount>1)
        	{
        		PlaySoundAtEntity("forges", "22_orb_forge.snt", asEntity, 0.5f, false);
        		CreateParticleSystemAtEntity("orb_repair", "ps_orb_repair_effect.ps", "AreaOrbEffect", false);
        	}

        	////////////////////
        	//Add Piece Number spefics
        	if(lCurrentCount ==1)
        	{
        		//Remove interact callback on pedestal
        		SetEntityPlayerInteractCallback("AreaOrbEffect","", false);

        	}
        	else if(lCurrentCount <=5)
        	{

        	}
        	else if(lCurrentCount ==6)
        	{
        		SetLampLit("orb_pedestal_1", true, true);
        		CompleteQuest("22FoundPedestal", "22FoundPedestal");
        		CompleteQuest("21FindOrb", "21FindOrb");

        		PlaySoundAtEntity("orbdonesound", "27_orb_done.snt", "AreaOrbEffect", 0.0f, false);

        		//If machine is not jammed yet, add a message and memento.
        		if(GetLocalVarInt("MachineJammed")==0)
        		{
        			AddTimer("OrbReadyMachineRunning", 12, nameof(TimerOrbReadyMachineRunning));
        		}

        		for(int i=1;i<=3; ++i)
        		{
        			SetEntityActive("barrier_ring03_weakened_"+i, true);
        			SetEntityActive("barrier_ring03_"+i, false);
        		}

        		CreateBarrierEnergyStream();

        		UpdateBarrierState();
        	}
        }

        //------------------------------------------

        void TimerOrbReadyMachineRunning(string asTimer)
        {
        	AddQuest("27OrbReadyNoJam", "27OrbReadyNoJam");
        }


        //------------------------------------------

        //Used to fade prop with a small delay!
        void TimerFadeProp(string asTimer)
        {
        	SetPropActiveAndFade(asTimer, false, 1.0f);
        }

        //------------------------------------------

        //Grease up the pedestal
        void UseTarOnPedestal(string asItem, string asEntity)
        {
        	RemoveItem(asItem);

        	//Tar has been added!
        	SetGlobalVarInt("TarOnPedestal",1);
        	CompleteQuest("22NeedTar", "22NeedTar");

        	PlaySoundAtEntity("tars", "22_use_tar.snt", asEntity, 1.0f, false);

        	SetEntityActive("orb_pedistal_tar_1", true);
        }

        //------------------------------------------

        void InteractOrbPedestal(string asEntity)
        {
        	SetMessage("Ch02Level22", nameof(InteractOrbPedestal), 0);
        	AddQuest("22FoundPedestal", "22FoundPedestal");
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // This is used to display the energy stream
        void CreateBarrierEnergyStream()
        {
        	////////////////////////////////////////////////////////////////////////////////////////////
        	// The stream starts showing 6 secs after the event is triggered to sync with the gather PS.
        	// Next ones will spawn every next second.
        	float fTimeStep = 1;
        	float fTime = 6;
        	CreateParticleSystemAtEntity("PSOrbGather", "ps_orb_stream_gather.ps", "Orb_Stream_Gather", true);
        	PlaySoundAtEntity("orbstart1", "29_hole_open.snt", "Orb_Stream_Gather", 0.0f, false);
        	PlaySoundAtEntity("orbstart2", "29_hole_blue.snt", "Orb_Stream_Gather", 0.0f, false);
        	PlaySoundAtEntity("Eloop", "27_energy_loop.snt", "Orb_Stream_Gather", 1.0f, false);

        	for(int i=1;i<=6;++i)
        	{
        		string sIndex = i.ToString(CultureInfo.InvariantCulture);
        		AddTimer(sIndex, fTime, nameof(TimerCreateStreamPS));
        		fTime += fTimeStep;
        	}


        	/////////////////////////////////////////////////////
        	// Sounds
        	//Did in timer instead

        	/////////////////////////////////////////////////////
        	// Create orb absorbing effect
        	AddTimer(nameof(TimerCreateOrbEffectPS), fTime, nameof(TimerCreateOrbEffectPS));
        }

        void TimerCreateStreamPS(string asTimer)
        {
        	CreateParticleSystemAtEntity("PSOrbStream_" + asTimer, "ps_orb_absorb_stream.ps", "Orb_Stream_" + asTimer, true);
        	PlaySoundAtEntity("SOrbStream_" + asTimer, "27_energy_loop.snt", "Orb_Stream_" + asTimer, 0.0f, false);
        }

        void TimerCreateOrbEffectPS(string asTimer)
        {
        	CreateParticleSystemAtEntity("PSOrbEffect", "ps_orb_absorb.ps", "AreaOrbEffect", true);
        	StopSound("SOrbStream_1", 1.0f); StopSound("SOrbStream_2", 1.0f); StopSound("SOrbStream_3", 1.0f);
        	StopSound("SOrbStream_4", 1.0f); StopSound("SOrbStream_5", 1.0f); StopSound("SOrbStream_6", 1.0f);
        	StopSound("Eloop", 1.0f);

        	SetLocalVarInt("OrbAbsorbing", 1);
        	UpdateBarrierState();
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // FALL DEATH
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideFallDeath(string asParent, string asChild, int alState)
        {
        	/////////////////////////////////
        	//A respawn at random position.
        	// If ambush is in progress, we need to use it's checkpoint!
        	if(GetLocalVarInt("AmbushStarted")==0)
        	{
        		int lRand = RandInt(0,1);
        		if(lRand==0)	CheckPoint("checkFallDeath1", "PlayerStartArea_1", "CheckPointFallWell", "Hints", "DeathFall_21_TortureNave");
        		if(lRand==1)	CheckPoint("checkFallDeath2", "PlayerStartArea_5", "CheckPointFallWell", "Hints", "DeathFall_21_TortureNave");
        	}

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
        }



        void CheckPointFallDeath(string asName, int alCount)
        {
        	//Do nothing??
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // WIND BREEZE
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideWindBreeze(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("BreezeActive")==1) return; //Another breeze is already playing.
        	if(RandInt(0, 3)!=0) return;

        	PlaySoundAtEntity("breeze", "27_wind.snt", asChild, 0, false);

        	//The particles
        	CreateParticleSystemAtEntityExt("breeze","ps_green_wind_breeze.ps", asChild, true, 1,1,1,0.5f,true, 1, 2, 100, 110);

        	//Set a var that a breeze is playing, so several do not play at the same time!
        	SetLocalVarInt("BreezeActive", 1);
        	AddTimer("BreezeOver",3,nameof(TimerBreezeOver));

        	AddDebugMessage("AreaCollide", false);
        }

        void TimerBreezeOver(string asTimer)
        {
        	SetLocalVarInt("BreezeActive", 0);
        }


        //------------------------------------------

        ///////////////////////////////////////////
        // BARRIER MACHINE
        ////////////////////////////////////////////

        //------------------------------------------

        void InitBarrierMachine()
        {
        	float fSmallSpeed = 2.1f;
        	float fLargeSpeed = fSmallSpeed * 1.3333f; //4/3 ratio on wheel size

        	RotatePropToSpeed("barrier_machine_cog_1", 10, fSmallSpeed, 1, 0, 0, true, "");
        	RotatePropToSpeed("barrier_machine_cog_3", 10, fLargeSpeed, 0, 0, -1, true, "");
        	RotatePropToSpeed("barrier_machine_cog_4", 10, fLargeSpeed, 0, 0, 1, true, "");
        }


        //------------------------------------------

        ///////////////////////////////////////////
        // BARRIER TUNNEL
        ////////////////////////////////////////////

        //------------------------------------------

        void InitBarrierTunnel()
        {
        	float fRingSpeed = 1.0f;

        	for(int i=2; i<=8; ++i)
        	{
        		float fDir = i%2==0 ? -1 : 1;
        		RotatePropToSpeed("barrier_ring02_"+i, 10, fRingSpeed,0,0,fDir, true, "");
        	}
        }

        //------------------------------------------

        void CollideTunnelDamage(string asParent, string asChild, int alState)
        {
        	//Enter area
        	if(alState == 1)
        	{
        		AddQuest("22BarrierTunnel", "22BarrierTunnel");

        		TimerTunnelDamage("");
        	}
        	//Leave area
        	else
        	{
        		RemoveTimer("TunnelDamage");
        	}
        }

        void TimerTunnelDamage(string asTimer)
        {
        	AddPlayerBodyForce(0,0,9000*2.5f, false);
        	GivePlayerDamage(10, "BloodSplat", true, false);
        	PlayerReact(false, 0.5f);

        	PlayGuiSound("ui_elec_damage.snt", 1.0f);

        	AddTimer("TunnelDamage",2, nameof(TimerTunnelDamage));
        }

        void InteractAreaTunnelDamage(string asEntity)
        {
        	AddTimer(asEntity, 1.0f, nameof(TimerInteractBackOn));
        	TimerTunnelDamage("");

        	AddQuest("22BarrierTunnel", "22BarrierTunnel");
        }
        void TimerInteractBackOn(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, "Interact"+asTimer, true);

        	RemoveTimer("TunnelDamage");
        }

        //------------------------------------------

        void UpdateBarrierState()
        {
        	int lKeyPuzzlesCompleted = 0;

        	// Check if machine is jammed
        	if(GetLocalVarInt("MachineJammed")==1)
        		lKeyPuzzlesCompleted++;

        	// Check if orb is built
        	if(GetGlobalVarInt("OrbsAddedCount")==6)
        	{
        		lKeyPuzzlesCompleted++;
        	}

        	AddDebugMessage("MachineJammed==" + GetLocalVarInt("MachineJammed") + " - OrbsAddedCount==" + GetGlobalVarInt("OrbsAddedCount") + " - KeyPuzzlesCompleted==" + lKeyPuzzlesCompleted, false);

        	////////////////////////////
        	//Either orb is completed or machine is jammed
        	if(lKeyPuzzlesCompleted==1)
        	{
        		for(int i=1;i<=3;++i)
        		{
        			// Replace electro rings by weakened versions
        			SetEntityActive("barrier_ring03_"+i, false);
        			SetPropActiveAndFade("barrier_ring03_weakened_" + i, true, 0.25f);

        			// C# inner counters are named j; AngelScript shadows the outer i here.
        			// Make lights weaker
        			for(int j=1; j<=3; ++j)
        			{
        				FadeLightTo("barrier_pointlight_"+j, 0, 0, 0, 0, 0, 0.5f);
        				SetLightFlickerActive("barrier_pointlight_"+j,false);

                        //FadeLightTo("barrier_pointlight_weak"+i, 0, 0, 0, 0, 0, 0.5f);
                        //SetLightFlickerActive("barrier_pointlight_"+i,false);
        			}
        			for(int j=1; j<=2; ++j)
        			{
        				FadeLightTo("barrier_spotlight_"+j, 0.4f, 0.4f, 0.6f, -1, 8, 0.5f);
        				SetLightFlickerActive("barrier_spotlight_"+j,false);
        			}
        		}
        	}
        	////////////////////////////
        	//Both orb is completed and machine is jammed
        	else if(lKeyPuzzlesCompleted==2)
        	{
        		PlaySoundAtEntity("thump_end", "27_thump", "PSOrbStream_1", 0.0f, false);

        		//The barrier can be walked through
        		CompleteQuest("22BarrierTunnel", "22BarrierTunnel");

        		if(GetLocalVarInt("OrbAbsorbing")==1)
        		{
        			// Turn electric effects off
        			for(int i=1; i<=3; ++i)
        			{
        				SetPropEffectActive("barrier_ring03_weakened_" + i, false, true);
        			}
        			// Turn lights off
        			for(int i=1; i<=3; ++i)
        			{
        				FadeLightTo("barrier_pointlight_weak_"+i, 0, 0, 0, 0, 0, 0.5f);
        				SetLightFlickerActive("barrier_pointlight_weak_"+i,false);
        			}
        			for(int i=1; i<=2; ++i)
        			{
        				FadeLightTo("barrier_spotlight_weak_"+i, 0, 0, 0, 0, 0, 0.5f);
        				SetLightFlickerActive("barrier_spotlight_weak_"+i,false);
        			}

        			//Stop the ambient track!
        			StopMusic(2.5f, 0);

        			//TODO: Remove looping energy sound here!
        			//Did in timer instead

        			// Remove Energy Stream
        			DestroyParticleSystem("PSOrbGather");
        			for(int i=1;i<=6; ++i)
        			{
        				DestroyParticleSystem("PSOrbStream_"+i);
        			}
        			DestroyParticleSystem("PSOrbEffect");
        			AddTimer(nameof(TimerOrbExplode), 1.0f, nameof(TimerOrbExplode));
        		}
        	}
        }

        //----------------------------------------------------

        void TimerOrbExplode(string asTimer)
        {
        	StartScreenShake(0.2f, 6, 0.5f, 3);
        	CreateParticleSystemAtEntity("PSOrbShockwave", "ps_orb_shockwave.ps", "AreaOrbEffect", false);

        	PlaySoundAtEntity("implodoelexplodo", "27_orb_implode.snt", "AreaOrbEffect", 0.0f, false);
        	StopSound("Sound_18", 0.0f);
        	StopSound("Sound_17", 4.0f);

        	//Blow out candles
        	SetLampLit("candlestick_floor*", false, false);
        	SetLampLit("candle_floor_*", false, false);

        	//Fade in blur light
        	FadeLightTo("OrbLight", 0.3f,0.6f,1.0f,1, 12, 0.2f);
        	AddTimer(nameof(TimerFadeOutExplodeLight), 5, nameof(TimerFadeOutExplodeLight));

        	// Disable tunnel damage, so player can walk through
        	SetEntityActive("AreaTunnelDamage", false);
        	// Remove blocker
        	SetEntityActive("block_box_block_box_1", false);
        	//RemoveEntityCollideCallback("Player", "AreaTunnelDamage");
        	//SetEntityPlayerInteractCallback("AreaTunnelDamage", "", false);

        	// Make Orb disappear
        	SetPropActiveAndFade("orb_assembly_6", false, 1);
        	SetLampLit("orb_pedestal_1", false, false);

        	// Play puzzle completed music and give sanity boost
        	AddTimer(nameof(TimerBarrierGone),6.5f, nameof(TimerBarrierGone));
        	AddTimer("stopsparks", 29.0f, nameof(TimerStopSparks));
        }

        void TimerFadeOutExplodeLight(string asTimer)
        {
        	FadeLightTo("OrbLight", 0,0,0,0, -1, 2);

        	PlaySoundAtEntity("orbend", "29_hole_blue.snt", "AreaOrbEffect", 4.0f, false);
        }

        //------------------------------------------

        void TimerBarrierGone(string asTimer)
        {
        	GiveSanityBoost();

        	PlayMusic("27_puzzle_passage.ogg", false, 0.8f, 0.5f, 9, false);
        	PlaySoundAtEntity("sparks", "27_spark", "AreaOrbEffect", 0.0f, true);
        }

        void TimerStopSparks(string asTimer)
        {
        	StopSound("sparks", 0.0f);
        }
        //------------------------------------------

        ///////////////////////////////////////////
        // DIARY PICKUP
        ////////////////////////////////////////////

        //------------------------------------------

        void PickDiary(string asEntity, string asType)
        {
        	PlayMusic("27_paper_daniel01.ogg", false, 0.3f, 0.5f, 10, false);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // PICK BUCKET OF TAR
        ////////////////////////////////////////////

        //------------------------------------------

        void PickBucketOfTar(string asEntity, string asType)
        {
        	SetGlobalVarInt("PickedBucketOfTar",1);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // MISC INTERACTIONS
        ////////////////////////////////////////////

        //------------------------------------------

        void InteractLargeGate(string asEntity)
        {
        	SetMessage("Ch02Level22", nameof(InteractLargeGate), 3.0f);
        	PlayGuiSound("close_gate.ogg", 0.7f);
        	AddTimer(asEntity, 1.0f, nameof(TimerLargeGate));
        }
        void TimerLargeGate(string asTimer)
        {
        	SetEntityPlayerInteractCallback(asTimer, nameof(InteractLargeGate), true);
        }

        //------------------------------------------

        void InteractMachineRoomDoor(string asEntity)
        {
        	SetMessage("Ch02Level22", nameof(InteractMachineRoomDoor), 0);
        }

        //------------------------------------------


        //Player Reactions
        void PlayerReact(bool bDamage, float fTime)
        {
        	if(bDamage)
        		AddTimer("sanity", fTime, nameof(TimerPlayerReactions));
        	else
        		AddTimer("scare", fTime, nameof(TimerPlayerReactions));

        	AddTimer("breath", fTime+2, nameof(TimerPlayerReactions));
        	AddTimer("breathl", fTime+4, nameof(TimerPlayerReactions));

        }
        void TimerPlayerReactions(string asTimer)
        {
        	if(asTimer == "sanity"){
        		GiveSanityDamage(10.0f, true);
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


        //BRUTE AT Start
        void CollideAreaBruteInside(string asParent, string asChild, int alState)
        {
        	//changed my mind i think
        	//SetSwingDoorClosed("sewer_arched_2", true, true);
        	//Play some wrecking the place sounds?
        }


        //BRUTE after breaking machine, if orb already broken
        void EventGrunt02()
        {
        	if(GetGlobalVarInt("OrbsAddedCount") != 6) return;

        	SetEntityActive("servant_brute_2", true);

        	SetSwingDoorDisableAutoClose("sewer_arched_2", true);
        	SetSwingDoorClosed("sewer_arched_2", false, false);
        	AddPropImpulse("sewer_arched_2", -3.0f, 0.0f, 0.0f, "World");

        	SetSwingDoorDisableAutoClose("sewer_arched_2", true);
        	SetSwingDoorClosed("sewer_arched_2", false, false);
        	AddPropImpulse("sewer_arched_4", 0, 0.0f, -3.0f, "World");
        }


        ///////
        //CHECKPOINTS
        void CollideAreaCP(string asParent, string asChild, int alState)
        {
        	if(alState == 1)
        		SetLocalVarString("WhatCPArea", asChild);

        	else if(alState == -1)
        		SetLocalVarString("WhatCPArea", "AreaMiddleCP");

        	CheckPoint(GetLocalVarString("WhatCPArea"), "PS_"+GetLocalVarString("WhatCPArea"), nameof(CPCallLocation), "", "");
        }

        //Different CP depending on location of death.
        void CPCallLocation(string asName, int alCount)
        {
        	//A grunt is coming towards the player if in tunnel area, but not if all puzzles done
        	if(asName == "AreaTunnelCP" && GetLocalVarInt(asName) != 1 &&
        		GetGlobalVarInt("OrbsAddedCount") != 6 && GetLocalVarInt("MachineJammed") != 1){
        		SetEntityActive(asName+"_Grunt", true);

        		SetLocalVarInt(asName, 1);
        		return;
        	}

        	//A grunt is coming towards the player if in machine area
        	if(asName == "AreaMachineCP" && GetLocalVarInt(asName) != 1){
        		SetEntityActive(asName+"_Grunt", true);

        		SetLocalVarInt(asName, 1);
        		return;
        	}

        	//Look down into the void in the middle of level
        	if(asName == "AreaMiddleCP" && GetLocalVarInt(asName) != 1){
        		StartPlayerLookAt("AreaLookMIddle", 10.0f, 10.0f, "");
        		AddTimer("AreaLookMIddle", 1.5f, nameof(TimerStopLookAt));
        		SetPlayerActive(false);

        		SetLocalVarInt(asName, 1);
        		return;
        	}

        	//Roaches crawls around the player if at the start
        	if(asName == "AreaStartCP" && GetLocalVarInt(asName) != 1){
        		SetEntityActive("cockroach_*", true);
        		AddTimer("cockroach_", 20.0f, nameof(TimerRemoveRoaches));

        		SetLocalVarInt(asName, 1);
        		return;
        	}

        	//A minor guaridan event should the player die more than one time at same area
        	AddTimer("guaridancp", 2.0f, nameof(TimerGuradianCP));
        }

        void TimerRemoveRoaches(string asTimer)
        {
        	SetPropActiveAndFade(asTimer+"*", false, 5.0f);
        }

        void TimerStopLookAt(string asTimer)
        {
        	CreateParticleSystemAtEntity("ps", "ps_cave_monster_warn.ps", asTimer, false);
        	PlaySoundAtEntity("sound", "04_warn.snt", asTimer, 0.0f, false);
        	StartScreenShake(0.03f, 0.5f, 1.0f, 1.5f);

        	StopPlayerLookAt();
        	SetPlayerActive(true);
        }

        void TimerGuradianCP(string asTimer)
        {
        	StartScreenShake(0.02f, 1.0f, 1.5f, 2.5f);
        	PlayGuiSound("guardian_distant.snt", 0.8f);
        	SetRadialBlurStartDist(0.3f);
        	FadeRadialBlurTo(0.3f, 0.3f);

        	PlayerReact(false, 1.0f);

        	AddTimer("stopradial", 5.0f, nameof(TimerStopRadial));
        }
        void TimerStopRadial(string asTimer)
        {
        	FadeRadialBlurTo(0.0f, 0.1f);
        }


        ////////////////////
        //A little guaridan event for when going through tunnel
        void CollideAreaExitGuardian(string asParent, string asChild, int alState)
        {
        	PlayGuiSound("guardian_ontop.snt", 0.8f);
        	PlayMusic("10_event_coming.ogg", false, 0.7f, 0.0f, 10, false);
        	StartScreenShake(0.1f, 1.0f, 1.5f, 2.5f);
        	PlayerReact(false, 1.0f);
        	SetRadialBlurStartDist(0.3f);
        	FadeRadialBlurTo(0.3f, 0.3f);

        	SetPropActiveAndFade("end_slime_*", true, 3.0f);
        	PlaySoundAtEntity("slime1", "slime_loop.snt", "end_slime_1", 1.0f, false);
        	PlaySoundAtEntity("slime2", "slime_loop.snt", "end_slime_2", 1.0f, false);
        	PlayGuiSound("guardian_idle6.ogg", 0.6f);

        	SetEntityActive("SlimeDamageArea_9", true);

        	AddTimer("3", 0.1f, nameof(TimerSounds));
        	AddTimer("5", 0.3f, nameof(TimerSounds));
        	AddTimer("6", 0.6f, nameof(TimerSounds));
        	AddTimer("7", 0.8f, nameof(TimerSounds));

        	AddTimer("stop", 4.0f, nameof(TimerSounds));
        }
        void TimerSounds(string asTimer)
        {
        	if(asTimer == "stop")
        		FadeRadialBlurTo(0.0f, 0.1f);
        	else
        		PlaySoundAtEntity("slafs", "slime_create.snt", "end_slime_"+asTimer, 0.0f, false);
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("Chancel");


        	///////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("lantern", "lantern.ent");
        		//GiveItemFromFile("bucket_of_tar_1", "bucket_of_tar.ent");

        		int lOrbPieces = 5;
        		for(int i=lOrbPieces+1; i<=6; ++i)
        			GiveItemFromFile("OrbPiece_"+i, "orbpiece_crimson.ent");

        		SetGlobalVarInt("OrbsAddedCount", lOrbPieces);
        		//SetGlobalVarInt("TarOnPedestal",1);
        		//SetLocalVarInt("MachineJammed",1);
        	}

        	////////////////////
        	//Init stuff
        	InitBarrierMachine();
        	InitBarrierTunnel();

        	//Enemy
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_155", 40.0f, "Flinch");
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_128", 0.0f, "");

        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_14", 20.0f, "IdleExtra1");
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_152", 10.0f, "IdleExtra2");
        	AddEnemyPatrolNode("servant_brute_2", "PathNodeArea_128", 0.0f, "");

        	AddEnemyPatrolNode("AreaTunnelCP_Grunt", "PathNodeArea_151", 6.0f, "IdleExtra1");
        	AddEnemyPatrolNode("AreaTunnelCP_Grunt", "PathNodeArea_128", 0.0f, "");

        	AddEnemyPatrolNode("AreaMachineCP_Grunt", "PathNodeArea_18", 6.0f, "IdleExtra2");
        	AddEnemyPatrolNode("AreaMachineCP_Grunt", "PathNodeArea_128", 0.0f, "");


        	////////////////////
        	// Collide Callbacks
        	AddEntityCollideCallback("Player", "AreaFallDeath", nameof(CollideFallDeath), false, 1);
        	AddEntityCollideCallback("Player", "AreaTunnelDamage", nameof(CollideTunnelDamage), false, 0);

        	for(int i=1; i<=14; ++i)
        	{
        		if(i==7 || i==8) continue; //Not available any more.

        		AddEntityCollideCallback("Player", "AreaWindBreeze_"+i, nameof(CollideWindBreeze), false, 1);
        	}

        	AddEntityCollideCallback("servant_brute_1", "AreaBruteInside", nameof(CollideAreaBruteInside), true, 1);

        	////////////////////
        	// Cogwheel damage setup
        	for(int i=1;i<=3;++i)
        	{
        		AddEntityCollideCallback("Player", "AreaCogwheelDamage_"+i, nameof(CollidePlayerCog), false, 1);
        		SetEntityPlayerInteractCallback("AreaCogwheelDamage_"+i, nameof(InteractCogWheel), false);
        	}

        	AddEntityCollideCallback("Player", "AreaTunnelCP", nameof(CollideAreaCP), false, 0);
        	AddEntityCollideCallback("Player", "AreaMachineCP", nameof(CollideAreaCP), false, 0);
        	AddEntityCollideCallback("Player", "AreaStartCP", nameof(CollideAreaCP), false, 0);

        	AddEntityCollideCallback("Player", "AreaExitGuardian", nameof(CollideAreaExitGuardian), true, 1);

        	//Interact
        	SetEntityPlayerInteractCallback("AreaTunnelDamage", nameof(InteractAreaTunnelDamage), true);

        	////////////////////
        	// Jamming Collide callbacks
        	for(int i=1; i<=2; ++i)
        	{
        		///////////////
        		//Small objects
        		AddEntityCollideCallback("stone_small01*", "AreaStoneCogs_"+i, nameof(CollideSmallObjectWithCogs), false, 1);
        		AddEntityCollideCallback("hammer*", "AreaStoneCogs_"+i, nameof(CollideSmallObjectWithCogs), false, 1);
        		AddEntityCollideCallback("sheet_metal*", "AreaStoneCogs_"+i, nameof(CollideSmallObjectWithCogs), false, 1);
        		AddEntityCollideCallback("saw_2", "AreaStoneCogs_"+i, nameof(CollideSmallObjectWithCogs), false, 1);

        		///////////////
        		//Weak objects
        		AddEntityCollideCallback("wood_box*", "AreaStoneCogs_"+i, nameof(CollideWeakObjectWithCogs), false, 1);
        		AddEntityCollideCallback("flask*", "AreaStoneCogs_"+i, nameof(CollideWeakObjectWithCogs), false, 1);

        		///////////////
        		//Correct objects
        		AddEntityCollideCallback("storage_box_wood*", "AreaStoneCogs_"+i, nameof(CollideCorrectObjectWithCogs), false, 1);
        		AddEntityCollideCallback("rock_debris*", "AreaStoneCogs_"+i, nameof(CollideCorrectObjectWithCogs), false, 1);
        	}

        	////////////////////
        	// Use Item Callbacks
        	AddUseItemCallback("StringOnPedestal", "string_pile_2", "AreaOrbEffect", nameof(UseStringOnPedestal), false);
        	AddUseItemCallback("StringOnPedestal", "string_pile_2", "orb_pedestal_1", nameof(UseStringOnPedestal), false);

        	// Remove tar on pedestal interaction if already done
        	if(GetGlobalVarInt("TarOnPedestal")==0){
        		AddUseItemCallback("TarOnPedestal","bucket_of_tar_1", "AreaOrbEffect", nameof(UseTarOnPedestal), true);
        		AddUseItemCallback("TarOnPedestal","bucket_of_tar_1", "orb_pedestal_1", nameof(UseTarOnPedestal), true);
        	}

        	for(int i=1; i<=6; ++i)
        	{
        		AddUseItemCallback("AddOrb"+i,"OrbPiece_"+i, "AreaOrbEffect", nameof(UseOrbPieceOnPedestal),false);
        		AddUseItemCallback("AddOrb"+i,"OrbPiece_"+i, "orb_pedestal_1", nameof(UseOrbPieceOnPedestal),false);

        		//Be able to use on orb pieces aswell
        		for(int j=1; j<=5; ++j)
        			AddUseItemCallback("AddOrb"+i,"OrbPiece_"+i, "orb_assembly_"+j, nameof(UseOrbPieceOnPedestal),false);
        	}

        	////////////////////
        	// Init map from last time

        	//Is bucked of tar picked up?
        	if(GetGlobalVarInt("PickedBucketOfTar")==0)
        		SetEntityActive("bucket_of_tar_1", true);

        	//How many orb pieces have been placed so far?
        	int lNumOfOrbPieces = GetGlobalVarInt("OrbsAddedCount");
        	if(lNumOfOrbPieces > 0)
        	{
        		SetEntityActive("orb_assembly_"+lNumOfOrbPieces, true);

        		//Remove interact callback on pedestal
        		SetEntityPlayerInteractCallback("AreaOrbEffect","", false);
        	}

        	//Tar on pedestal?
        	if(GetGlobalVarInt("TarOnPedestal")==1)
        	{
        		SetEntityActive("orb_pedistal_tar_1", true);
        	}
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("27_wind"); PreloadSound("impact_metal_high"); PreloadSound("break_wood_metal"); PreloadSound("break_glass_bottle");
        	PreloadSound("27_jam"); PreloadSound("27_break"); PreloadSound("22_orb_loose"); PreloadSound("29_hole_open");
        	PreloadSound("29_hole_blue"); PreloadSound("27_energy_loop"); PreloadSound("11_fall"); PreloadSound("player_bodyfall");
        	PreloadSound("player_falldamage_max"); PreloadSound("27_thump"); PreloadSound("27_orb_implode"); PreloadSound("27_spark");
        	PreloadSound("slime_loop"); PreloadSound("react_breath"); PreloadSound("react_scare"); PreloadSound("guardian_distant");
        	PreloadSound("guardian_ontop"); PreloadSound("guardian_idle6");

        	PlayMusic("18_amb.ogg", true, 0.8f, 4, 0, true);

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	FadeRadialBlurTo(0.0f, 0.0f);
        }
    }
}
