using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/23_torture_transept.hps; journal: docs/ScriptPort/ch02/23_torture_transept.md.</summary>
    [HplMapScript("maps/main/ch02/23_torture_transept.map")]
    public class TortureTransept : HplScript
    {

        //------------------------------------------

        ///////////////////////////////////////////
        // GET BLOOD
        ////////////////////////////////////////////

        //------------------------------------------

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

        //Player already has blood in container but wants more or something
        void UseBloodContainerOnBlood(string asItem, string asEntity)
        {
        	SetMessage("Ch02Level23", "FilledContainerOnPool", -1);
        }

        //Hint for interacting on hatch
        void InteractHatchDrainage(string asEntity)
        {
        	SetMessage("Ch02Level23", "SignGrate", -1);
        }

        //------------------------------------------

        void UseContainerAndStringOnBlood(string asItem, string asEntity)
        {
        	SetPlayerCrouching(true);				//Make sure player sits down.
        	StartPlayerLookAt("AreaBloodHole", 2, 4, "");		//Look at the hole

        	RemoveItem("container_and_string");
        	AddTimer("bloodevent1", 0.3f, nameof(TimerGetBloodEvent));

        	SetEntityPlayerInteractCallback("AreaBloodHole", "", true);
        	//SetEntityActive("AreaBloodHole", false);//No need for this any more
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
        		PlayMusic("23_puzzle.ogg", false, 0.7f, 0.5f, 9, false);
        		CompleteQuest("23NeedString", "23NeedString");

        		//Disable the terror of this area.
        		StopSound("AreaTerror_1terror", 2);
        		RemoveTimer("AreaTerror_1terror");
        	}
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // PICK UP ORB PIECE
        ////////////////////////////////////////////

        //------------------------------------------

        void PickUpOrbPiece(string asEntity, string asType)
        {
        	AddGlobalVarInt("TranseptOrbCount",1);

        	AddDebugMessage("Transept OrbPiece count: "+GetGlobalVarInt("TranseptOrbCount"), false);

        	if(asEntity == "OrbPiece_2")
        		SetEntityActive("AreaCradleSoundEvent", true);

        	//When all orbs are collecting, give player some extra sanity and nice music.
        	if(GetGlobalVarInt("TranseptOrbCount")==3)
        	{
        		PlayMusic("02_puzzle.ogg", false, 0.7f, 0, 10, false);
        		GiveSanityBoost();

        		CompleteQuest("21OrbsLeftInTransept", "21OrbsLeftInTransept");
        	}
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
        		FadeImageTrailTo(1.0f, 0.5f);
        		AddTimer(asChild+"terror", 2, nameof(TimerTerrorDrain));
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

        void TimerTerrorDrain(string asTimer)
        {
        	if(GetPlayerSanity() < 35)
        		GiveSanityDamage(0.05f*0.15f, false);
        	else
        		GiveSanityDamage(0.05f*0.5f, false);

        	AddTimer(asTimer, 0.05f, nameof(TimerTerrorDrain));
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // STRAPPADO EVENTS
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideStrappadoShackles(string asParent, string asChild, int alState)
        {
        	string sSound = "";

        	//Sound of stretch and pain
        	if(asParent == "AreaStrappadoEvent_1")
        	{
        		sSound = "23_strap1";
        		PlayerReact(1.0f,0.4f,false);
        		SetRadialBlurStartDist(0.3f);
        		FadeRadialBlurTo(0.3f, 0.1f);
        	}
        	//More stretch, panting, the vicitim is now in air and kicks chair
        	else if(asParent == "AreaStrappadoEvent_2")
        	{
        		sSound = "23_strap2";
        		SetPropActiveAndFade("stool_wood_2", false, 0.3f);
        		SetPropActiveAndFade("stool_wood_4", true, 0.3f);
        		PlayerReact(1.0f,0.6f,false);
        		SetRadialBlurStartDist(0.3f);
        		FadeRadialBlurTo(0.3f, 0.1f);
        	}
        	//Higher up, even more pain.
        	else if(asParent == "AreaStrappadoEvent_3")
        	{
        		sSound = "23_strap3";
        		PlayerReact(1.0f,0.8f,true);
        		SetRadialBlurStartDist(0.3f);
        		FadeRadialBlurTo(0.3f, 0.1f);
        	}

        	AddTimer("stopradial", 1.0f, nameof(TimerStopRadial));
        	AddDebugMessage("Strappado area: "+asParent+" sound: "+sSound, false);
        	PlaySoundAtEntity("strappado",sSound, asParent, 0, false);
        }

        void TimerStopRadial(string asTimer)
        {
        	FadeRadialBlurTo(0.0f, 0.05f);
        }
        //------------------------------------------


        ///////////////////////////////////////////
        // SAW SOUND EVENTS
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideSawSounds(string asParent, string asChild, int alState)
        {
        	string sLoopingSound = "";
        	string sEventSound = "";
        	float fFadeOutTime = 2.0f;

        	//Low muffled cries + silent looping sawing starts
        	if(asChild == "AreaSawSounds_1")
        	{
        		sEventSound = "23_saw_voice1";
        		sLoopingSound = "23_saw1";
        	}
        	//A little bit  muffled cries + a bit louder looping sawing starts
        	if(asChild == "AreaSawSounds_2")
        	{
        		sEventSound = "23_saw_voice2";
        		sLoopingSound = "23_saw2";
        		PlayerReact(1.0f,0.5f,false);
        	}
        	//A louder (but still muffled) screams and intense sawing.
        	if(asChild == "AreaSawSounds_3")
        	{
        		sEventSound = "23_saw_voice3";
        		sLoopingSound = "23_saw3";
        		PlayerReact(1.0f,0.8f,true);
        	}
        	//Just fade out sawing.
        	if(asChild == "AreaSawSounds_4")
        	{
        		fFadeOutTime = 2.0f;
        	}

        	//////////////////////////////////////
        	//Stop current sound and play a new one
        	StopSound("saw_script_loop",fFadeOutTime);
        	StopSound("saw_script_event",fFadeOutTime);
        	if(sLoopingSound != "")	PlaySoundAtEntity("saw_script_loop",sLoopingSound, "AreaSawSoundSource", 1, true);
        	if(sEventSound != "")	PlaySoundAtEntity("saw_script_event",sEventSound, "AreaSawSoundSource", 0, false);

        	AddDebugMessage("saw area: "+asChild+" loopsound: "+sLoopingSound + " event sound: "+sEventSound, false);

        }

        ///////////////////////////////////////////
        // CRADLE SOUND EVENTS
        ////////////////////////////////////////////

        //------------------------------------------

        void CollideCradleSoundEvent(string asParent, string asChild, int alState)
        {
        	//The captive (heard in visions) is still alive, pants silently and is dragged away.
        	//PlaySoundAtEntity("sawvictim", "23_saw_victim_pant_and_drag", "AreaCradleSoundSource", 0, false);

        	AddDebugMessage("Captive pants and is dragged off.", false);
        }


        ///////////////////////////////////////////
        // PICK UP TORTURE NOTE
        ////////////////////////////////////////////

        //------------------------------------------

        void PickUpTortureNote(string asEntity, string asType)
        {
        	PlayMusic("23_paper_alexander.ogg", false, 0.7f, 0.1f, 9, false);
        }

        //------------------------------------------

        ///////////////////////////////////////////
        // INTERACT TORTURE MACHINE
        ////////////////////////////////////////////

        //------------------------------------------

        void InteractEmotionEffectSaw(string asArea)
        {
        	StartEffectEmotionFlash("Ch02Level23", "TortureText_Saw", "ui_insanity_touch.snt");
        }

        //------------------------------------------

        void CradleFlashbackOver()
        {
        	SetEntityActive("AreaEmotionEffectCradle", true);
        	PlaySoundAtEntity("AreaEmotionEffectCradle", "23_pyramid", "AreaEmotionEffectCradle", 0.0f, false);
        }

        void InteractEmotionEffectCradle(string asArea)
        {
        	StartEffectEmotionFlash("Ch02Level23", "TortureText_Cradle", "ui_insanity_touch.snt");
        }

        //------------------------------------------

        void InteractEmotionEffectStrappado(string asArea)
        {
        	StartEffectEmotionFlash("Ch02Level23", "TortureText_Strappado", "ui_insanity_touch.snt");
        }

        //------------------------------------------

        //PLAYER REACT
        void PlayerReact(float fTime, float fVolume, bool bSanity)
        {
        	SetLocalVarFloat("PlayerReactVolume", fVolume);

        	if(bSanity) AddTimer("sanity", fTime, nameof(TimerPlayerReact));

        	AddTimer("scare", fTime, nameof(TimerPlayerReact));
        	AddTimer("breath", fTime+2.0f, nameof(TimerPlayerReact));
        	AddTimer("breathl", fTime+4.0f, nameof(TimerPlayerReact));
        }
        void TimerPlayerReact(string asTimer)
        {
        	if(asTimer == "sanity")
        		GiveSanityDamage(20.0f, true);

        	if(asTimer == "scare")
        		PlayGuiSound("react_"+asTimer, GetLocalVarFloat("PlayerReactVolume"));

        	if(asTimer == "breath")
        		PlayGuiSound("react_"+asTimer, GetLocalVarFloat("PlayerReactVolume")-0.1f);

        	if(asTimer == "breathl")
        		PlayGuiSound("react_"+asTimer, GetLocalVarFloat("PlayerReactVolume")-0.3f);
        }


        /////////////////////
        //EVENT ON EXIT
        void CollideAreaStartBreath(string asParent, string asChild, int alState)
        {
        	if(GetPlayerSanity() > 90) return;	//No event on sanity more than 70
        	if(GetGlobalVarInt("TranseptOrbCount")!=3) return;
        	if(GetLocalVarInt("BreathEvent") == 1) return;

        	SetLocalVarInt("BreathEvent", 1);

        	for(int i=1;i<=16;i++){
        		AddTimer("AreaBreath_"+RandInt(0, 16), RandFloat(0.0f, i), nameof(TimerBreath));
        	}

        	StartScreenShake(0.03f, 4.0f, 2.0f, 4.0f);
        	SetRadialBlurStartDist(0.3f);

        	PlayerReact(1.0f, 0.4f, false);

        	PlaySoundAtEntity("creak", "00_creak.snt", "Player", 4.0f, false);
        	PlaySoundAtEntity("creak", "23_warn.snt", "Player", 1.0f, false);

        	AddTimer("startr", 1.0f, nameof(TimerStopBreath));
        	AddTimer("stopc", 10.0f, nameof(TimerStopBreath));
        	AddTimer("stopr", 10.0f, nameof(TimerStopBreath));
        }
        void TimerBreath(string asTimer)
        {
        	CreateParticleSystemAtEntity("ps"+asTimer, "ps_cave_monster_breath_up.ps", asTimer, false);
        	PlaySoundAtEntity("s"+asTimer, "23_breath", asTimer, RandFloat(0.0f, 1.0f), false);
        }
        void TimerStopBreath(string asTimer)
        {
        	if(asTimer == "startr")
        		FadeRadialBlurTo(0.1f, 0.01f);

        	if(asTimer == "stopc")
        		StopSound("creak", 8.0f);

        	if(asTimer == "stopr")
        		FadeRadialBlurTo(0.0f, 0.04f);
        }


        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("Transept");

        	///////////////////////////
        	// Item callbacks
        	AddUseItemCallback("container_on_blood", "glass_container_1", "AreaBloodHole", nameof(UseContainerOnBlood), false);
        	AddUseItemCallback("container_on_blood", "glass_container_1", "hatch_drainage_2", nameof(UseContainerOnHatch), false);

        	AddUseItemCallback("container_n_string_on_blood","container_and_string", "AreaBloodHole", nameof(UseContainerAndStringOnBlood), false);
        	AddUseItemCallback("container_n_string_on_blood", "container_and_string", "hatch_drainage_2", nameof(UseContainerAndStringOnHatch), false);

        	AddUseItemCallback("contblood_on_blood1", "glass_container_blood", "AreaBloodHole", nameof(UseBloodContainerOnBlood), false);
        	AddUseItemCallback("contblood_on_blood2", "glass_container_blood", "hatch_drainage_2", nameof(UseBloodContainerOnBlood), false);

        	///////////////////////////
        	// Collide Areas
        	for(int i=1; i<=3; ++i)
        		AddEntityCollideCallback("Player", "AreaTerror_"+i, nameof(CollideTerrorArea), false, 0);

        	for(int i=1; i<=4; ++i)
        		AddEntityCollideCallback("Player", "AreaSawSounds_"+i, nameof(CollideSawSounds),true, 1);

        	AddEntityCollideCallback("AreaStrappadoEvent_*", "strappado_shackles_2", nameof(CollideStrappadoShackles),true, 1);
        	AddEntityCollideCallback("Player","AreaCradleSoundEvent", nameof(CollideCradleSoundEvent), true, 1);

        	AddEntityCollideCallback("Player", "AreaStartBreath", nameof(CollideAreaStartBreath), false, 1);

        	//Interacttion
        	SetEntityPlayerInteractCallback("hatch_drainage_2", nameof(InteractHatchDrainage), true);

        	///////////////////////////
        	// Connections
        	InteractConnectPropWithRope("wheelrope", "crank_iron_2","RopeArea_1", true, 6,3,3, false, 0);

        	///////////////////////////
        	// Debug
        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("lantern", "lantern.ent");
        		GiveItemFromFile("crank_wood", "crank_wood_no_mount_item.ent");
        		for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

        		GiveItemFromFile("glass_container_1", "glass_container.ent");

        		GiveItemFromFile("string_pile_2", "string_pile.ent");
        	}

        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("ui_torture"); PreloadSound("23_strap1"); PreloadSound("23_strap2"); PreloadSound("23_strap3");
        	PreloadSound("23_saw_voice1"); PreloadSound("23_saw1"); PreloadSound("23_saw_voice2"); PreloadSound("23_saw2");
        	PreloadSound("23_saw_voice3"); PreloadSound("23_saw3"); PreloadSound("23_pyramid"); PreloadSound("react_scare");
        	PreloadSound("react_breath");

        	PlayMusic("23_amb.ogg", true, 1.0f, 4.0f, 0, true);
        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {

        }
    }
}
