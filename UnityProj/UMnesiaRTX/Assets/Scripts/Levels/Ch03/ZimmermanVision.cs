using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch03
{
    /// <summary>Port of maps/main/ch03/26_zimmerman_vision.hps; journal: docs/ScriptPort/ch03/26_zimmerman_vision.md.</summary>
    [HplMapScript("maps/main/ch03/26_zimmerman_vision.map")]
    public class ZimmermanVision : HplScript
    {
        //-----------------------------------------------------------

        ///////////////////////////////////////////
        // HELPERS
        ///////////////////////////////////////////

        //-----------------------------------------------------------

        void PlayEffectVoice(string asEntryBase, string asCat,int alStartIdx, int alEndIdx, string asCallback)
        {
        	for(int i=alStartIdx; i<=alEndIdx; ++i)
        	{
        		string sEntry = asEntryBase;
        		if(i<10) sEntry += "0";
        		sEntry += i;

        		AddEffectVoice(sEntry, "", asCat, sEntry, false, "", 0,0 );
        	}
        	SetEffectVoiceOverCallback(asCallback);
        }

        //-----------------------------------------------------------

        ///////////////////////////////////////////
        // MAIN EVENTS
        ///////////////////////////////////////////

        //-----------------------------------------------------------

        void ResumeEvents()
        {
        	AddTimer(nameof(TimerMainEvents), 0.1f, nameof(TimerMainEvents));
        }

        //-----------------------------------------------------------

        void TimerMainEvents(string asTimer)
        {
        	int lEventNum = GetLocalVarInt("MainEventNum");
        	AddLocalVarInt("MainEventNum",1);
        	float fNextEventTime = 1.0f;
        	bool bPause = false;

        	////////////////////////
        	// 0: Wait 3 secs before doing anything
        	if(lEventNum == 0)
        	{
        		StartPlayerLookAt("AreaLookAt", 0.5f, 0.5f, "");

        		PlaySoundAtEntity("Rain", "26_rain_no3d.snt", "AreaRainSound", 3, false);
        		fNextEventTime	= 3;
        	}
        	////////////////////////
        	// 1: Player intro voices
        	else if(lEventNum == 1)
        	{
        		StopPlayerLookAt();
        		PlayEffectVoice("CH03L26_DanielDiary08_01_", "Flashbacks", 1,2, nameof(OnVoiceOver));

        		bPause = true;
        	}
        	////////////////////////
        	// 2: Fade in, no voices
        	else if(lEventNum == 2)
        	{
        		PlayMusic("amb_extra02.ogg", true, 0.7f, 4, 0, false);
        		SetPlayerActive(true);
        		FadeIn(3);
        		fNextEventTime	= 2.0f;
        	}
        	////////////////////////
        	// 3: Play voice on alstadt.
        	else if(lEventNum == 3)
        	{
        		AddTimer("sfx1", 1.0f, nameof(TimerDelaySFX));
        		PlayEffectVoice("CH03L26_DanielDiary08_01_", "Flashbacks", 3,3, nameof(OnVoiceOver));
        		bPause = true;
        	}
        	////////////////////////
        	// 4: Carriage sounds + music starts
        	else if(lEventNum == 4)
        	{
        		fNextEventTime	= 6.0f;
        	}
        	////////////////////////
        	// 5: Play voice on alstadt.
        	else if(lEventNum == 5)
        	{
        		AddTimer("sfx3", 5.0f, nameof(TimerDelaySFX));
        		PlayEffectVoice("CH03L26_DanielDiary08_01_", "Flashbacks", 4,4, nameof(OnVoiceOver));
        		bPause = true;
        	}
        	////////////////////////
        	// 6: Play voice on alstadt.
        	else if(lEventNum == 6)
        	{

        		fNextEventTime	= 3.0f;
        	}
        	////////////////////////
        	// 7: Play voice on alstadt.
        	else if(lEventNum == 7)
        	{
        		AddTimer("sfx4", 4.0f, nameof(TimerDelaySFX));
        		PlayEffectVoice("CH03L26_DanielDiary08_01_", "Flashbacks", 5,5, nameof(OnVoiceOver));
        		bPause = true;
        	}
        	////////////////////////
        	// 8: Play voice on alstadt.
        	else if(lEventNum == 8)
        	{
        		fNextEventTime	= 5.0f;
        	}
        	////////////////////////
        	// 9: Play voice on alstadt.
        	else if(lEventNum == 9)
        	{
        		PlayMusic("26_event_brute.ogg", false, 0.6f, 1, 0, false);
        		PlayEffectVoice("CH03L26_DanielDiary08_01_", "Flashbacks", 6,6, nameof(OnVoiceOver));
        		bPause = true;
        	}
        	////////////////////////
        	// 10: End of vision
        	else if(lEventNum == 10)
        	{
        		StopMusic(6.0f, 0);
        		StopSound("Rain", 6);
        		FadeOut(6.0f);
        		fNextEventTime	= 6.0f;
        	}
        	////////////////////////
        	// 11: New map
        	else if(lEventNum == 11)
        	{
        		ChangeMap("26_torture_nave_redux.map","PlayerStartArea_AfterVision","","");
        		return;
        	}

        	AddDebugMessage("Event:"+lEventNum,false);//+" Time:"+fNextEventTime+" Pause"+bPause?1:0, false);

        	if(bPause==false)
        		AddTimer(nameof(TimerMainEvents), fNextEventTime, nameof(TimerMainEvents));
        }

        void TimerDelaySFX(string asTimer)
        {
        	if(asTimer == "sfx1")
        		PlayGuiSound("26_zimmerman_part1.ogg", 0.7f);
        	if(asTimer == "sfx2")
        		PlayGuiSound("26_zimmerman_part2.ogg", 0.5f);
        	if(asTimer == "sfx3")
        		PlayGuiSound("26_zimmerman_part3.ogg", 0.3f);
        	if(asTimer == "sfx4")
        		PlayGuiSound("26_zimmerman_part4.ogg", 0.7f);

        }

        //-----------------------------------------------------------

        void OnVoiceOver()
        {
        	ResumeEvents();
        }

        //-----------------------------------------------------------

        ///////////////////////////////////////////
        // MAIN EVENTS
        ///////////////////////////////////////////

        //-----------------------------------------------------------

        //Reduce speed if player enters, increase if he exits
        void CollideSpeedDecInOut(string asParent, string asChild, int alState)
        {
        	if(alState == 1)
        		SetPlayerMoveSpeedMul(0.2f);
        	else
        		SetPlayerMoveSpeedMul(GetLocalVarFloat("PlayerDefualtMoveSpeed"));
        }

        //-----------------------------------------------------------

        void CollideSpeedDec(string asParent, string asChild, int alState)
        {
        	AddLocalVarFloat("PlayerDefualtMoveSpeed",-0.1f);

        	SetPlayerMoveSpeedMul(GetLocalVarFloat("PlayerDefualtMoveSpeed"));
        }

        //-----------------------------------------------------------

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("ZimmermanFarm");


        	//////////////////////////////
        	// Init Player
        	SetPlayerMoveSpeedMul(0.45f);
        	SetPlayerRunSpeedMul(0);

        	SetPlayerActive(false);

        	SetPlayerJumpDisabled(true);

        	SetInventoryDisabled(true);
        	SetLanternDisabled(true);
        	SetSanityDrainDisabled(true);

        	SetRadialBlurStartDist(0.2f);
        	FadeRadialBlurTo(0.1f, 1);
        	FadeSepiaColorTo(0.5f, 0.025f);

        	StopMusic(1.0f, 0);

        	FadeOut(0);

        	SetGlobalVarInt("FromZimmermanFarm",1); //To play special effects when player gets back.

        	/////////////////////////////
        	// Var init
        	SetLocalVarFloat("PlayerDefualtMoveSpeed", 0.45f);

        	/////////////////////////////
        	// Collision Areas
        	for(int i=1; i<=2; ++i)
        	{
        		AddEntityCollideCallback("Player", "AreaSpeedDecInOut_"+i, nameof(CollideSpeedDecInOut), false,0);
        		AddEntityCollideCallback("Player", "AreaSpeedDec_"+i, nameof(CollideSpeedDec), true,1);
        	}

        	//////////////////////////////
        	// Event timer
        	AddTimer(nameof(TimerMainEvents), 0.1f, nameof(TimerMainEvents));

        	///////////////////////////////
        	//Debug
        	if(ScriptDebugOn())
        	{
        		GiveItemFromFile("lantern", "lantern.ent");
        	}
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("26_zimmerman_part1.ogg"); PreloadSound("26_zimmerman_part2.ogg"); PreloadSound("26_zimmerman_part3.ogg"); PreloadSound("26_zimmerman_part4.ogg");
        	PreloadSound("26_rain_no3d.snt");
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	SetupLoadScreen("LoadingText", "Ch03_Zimmerman02", 1, "game_loading_rose.jpg");

        	SetInventoryDisabled(false);
        	SetLanternDisabled(false);
        	SetSanityDrainDisabled(false);

        	SetPlayerJumpDisabled(false);

        	SetPlayerMoveSpeedMul(1);
        	SetPlayerRunSpeedMul(1);
        	FadeRadialBlurTo(0,1);
        	FadeSepiaColorTo(0, 0.1f);
        }
    }
}
