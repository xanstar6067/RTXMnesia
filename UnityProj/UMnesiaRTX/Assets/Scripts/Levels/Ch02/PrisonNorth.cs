using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch02
{
    /// <summary>Port of maps/main/ch02/15_prison_north.hps; journal: docs/ScriptPort/ch02/15_prison_north.md.</summary>
    [HplMapScript("maps/main/ch02/15_prison_north.map")]
    public class PrisonNorth : HplScript
    {
        //////////////////////////
        //BEGIN ALEXANDER VOICES//
        /*When in area play alexander voices
         */
        void CollideAlexanderArea(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("InAlexArea") == 0)
        		AddTimer("alex", RandInt(25,35), nameof(TimerAlexVoice));

        	SetLocalVarInt("InAlexArea", alState);
        }
        void TimerAlexVoice(string asTimer)
        {
        	if(GetLocalVarInt("InAlexArea") == 1)
        		AddLocalVarInt("AlexVoice", 1);

        	switch(GetLocalVarInt("AlexVoice")){
        		case 1:
        			AlexanderMental(1);
        			AddLocalVarInt("AlexVoice", 1);
        		break;
        		case 3:
        			AlexanderMental(2);
        			AddLocalVarInt("AlexVoice", 1);
        		break;
        		case 5:
        			AlexanderMental(3);
        			return;
        		break;
        	}

        	AddTimer("alex", RandInt(10,30), nameof(TimerAlexVoice));
        }
        void AlexanderMental(int iVoice)
        {
        	AddEffectVoice("CH02L15_Alexander_0"+iVoice, "", "Voice", "CH02L15_Alexander_0"+iVoice, false, "", 0.0f, 0.0f);
        }

        /*If close to prisoner vision, turn off alexander voices and turn back on when vision over
         */
        void CollideNoAlex(string asParent, string asChild, int alState)
        {
        	SetEntityActive("AlexanderArea", false);

        	AddTimer("shakeshackles", 2.5f, nameof(TimerShakeShackles));
        	AddTimer("shakeshackles", 3.0f, nameof(TimerShakeShackles));
        	AddTimer("shakeshackles", 3.4f, nameof(TimerShakeShackles));
        	AddTimer("shakeshackles", 4.0f, nameof(TimerShakeShackles));
        }
        void VisionOverAlexOn()
        {
        	SetEntityActive("AlexanderArea", true);
        }
        //END ALEXANDER VOICES//
        ////////////////////////


        ////////////////////
        //BEGIN MISC STUFF//
        /*Shake some shackles during captured prisioner Vision
         */
        void TimerShakeShackles(string asTimer)
        {
        	AddBodyImpulse("shackles_3_Body_5", RandFloat(-0.7f, 0.7f), RandFloat(1.0f, 2.5f), RandFloat(-0.7f, 0.7f), "World");
        	AddBodyImpulse("shackles_3_Body_9", RandFloat(-0.7f, 0.7f), RandFloat(1.0f, 2.5f), RandFloat(-0.7f, 0.7f), "World");
        }

        /*Messages for interacting/using items on doors that can not be opened.
         */
        void InteractExtraDoor(string asEntity)
        {
        	AddTimer("messdoor", 0.3f, nameof(TimerMessDoor));
        }
        void TimerMessDoor(string asTimer)
        {
        	SetMessage("Ch02Level15", "LockedPerfect", 0);
        }

        void UseItemOnExtraDoor(string asItem, string asEntity)
        {
        	AddTimer(asItem, 0.3f, nameof(TimerItemMessDoor));
        }
        void TimerItemMessDoor(string asTimer)
        {
        	if(asTimer == "glass_container_filled")
        		SetMessage("Ch02Level15", "UseGlassFilledOnExtra", 0);
        	else if(asTimer == "glass_container_1")
        		SetMessage("Ch02Level15", "UseGlassOnExit", 0);
        	else
        		SetMessage("Ch02Level15", "UseItemMint", 0);
        }

        /*Always locked door
         */
        void InteractAlwaysLocked(string asEntity)
        {
        	SetMessage("LevelDoors", "LockedForever", 0);
        }

        /*Help me Voice
         */
        void CollideAreaHelpMe(string asParent, string asChild, int alState)
        {
        	PlaySoundAtEntity("help", "15_help", "AreaHelpMe_1", 0.0f, false);

        	AddTimer("i", 0.3f, nameof(TimerEee));
        }
        void TimerEee(string asTimer)
        {
        	PlaySoundAtEntity("ii", "react_scare", "Player", 0.1f, false);

        	GiveSanityDamage(10.0f, true);
        }
        //END MISC STUFF//
        //////////////////


        /////////////////////
        //BEGIN ACID PUZZLE//
        /*If player touches or jump up on the barrel he gets acid damage
         */
        void CollideAcidDamage(string asParent, string asChild, int alState)
        {
        	InteractTouchAcid(asChild);
        }
        void InteractTouchAcid(string asEntity)
        {
        	PlaySoundAtEntity("acid_burn", "puzzle_acid", "Player", 0.2f, false);

        	AddQuest("15Acid", "15Acid");

        	AddTimer("acidmess", 0.3f, nameof(TimerAcidDamageMess));
        	AddTimer("acidmessoff", 1.5f, nameof(TimerAcidDamageMess));
        }
        void TimerAcidDamageMess(string asTimer)
        {
        	if(asTimer == "acidmessoff"){
        		FadeRadialBlurTo(0, 0.1f);
        		return;
        	}

        	SetMessage("Ch02Level15", "AcidBurnPlayer", 0);

        	GivePlayerDamage(10.0f, "BloodSplat", false, false);

        	FadeRadialBlurTo(0.1f, 1);
        	SetRadialBlurStartDist(0.2f);
        }

        /*Using the empty bottle will fill it, all else will give error messages
         */
        void UseBottle(string asItem, string asEntity)
        {
        	if(asItem == "glass_container_1"){
        		PlaySoundAtEntity("fillbottle", "puzzle_acid_success", asEntity, 1.0f, false);

        		RemoveItem(asItem);
        		GiveItemFromFile("glass_container_filled", "glass_container_filled.ent");

        		SetMessage("Ch02Level15", "AcidInJar", 0);

        		GiveSanityBoostSmall();

        		CompleteQuest("15Acid", "15Acid");
        	}
        	else if(asItem == "glass_container_filled"){
        		SetMessage("Ch02Level15", "AcidAlreadyInJar", 0);
        	}
        	else SetMessage("Ch02Level15", "AcidWithItem", 0);
        }

        /*Just fun stuff - vaporize organic objects and hint on glass and metal objects
         */
        void CollideAcidWater(string asParent, string asChild, int alState)
        {
        	if(StringContains(asParent, "glass_")){
        		string sGlassVar = "used_"+asParent+"_on_acid";
        		AddLocalVarInt(sGlassVar, 1);
        		AddDebugMessage("Added 1 to var " + sGlassVar + ": " + GetLocalVarInt(sGlassVar), false);
        		AddTimer("glass "+sGlassVar, 0.5f, nameof(TimerVanishOA));
        		return;
        	}

         	FadeLightTo("AcidLight", 0.0f, 1.0f, 0.4f, 1.0f, -1, 2.0f);

        	PlaySoundAtEntity("burn"+asParent, "gameplay_acid_web", asChild, 1.0f, false);
        	PlaySoundAtEntity("burn2"+asParent, "puzzle_acid", asChild, 0.0f, false);

        	AddTimer(asParent, 1.0f, nameof(TimerVanishOA));
        	AddTimer("light", 1.5f, nameof(TimerVanishOA));
        }
        void TimerVanishOA(string asTimer)
        {
        	if(StringSub(asTimer, 0, 5) == "glass" &&
        		GetLocalVarInt(StringSub(asTimer, 6, -1))==1){
        		SetMessage("Ch02Level15", "AcidOnGlass", 0);
        		return;
        	}
         	else if(asTimer == "light"){
        		FadeLightTo("AcidLight", 0.0f, 0.0f, 0.0f, 0.0f, -1, 0.5f);
        		return;
        	}
        	else if(StringContains(asTimer, "plate_")){
        		StopSound("burn"+asTimer, 1.0f);

        		SetPropHealth(asTimer, 0.0f);
        		return;
        	}
        	else if(asTimer == "pot_1"){
        		StopSound("burn"+asTimer, 1.0f);

        		SetMessage("Ch02Level15", "AcidOnPot", 0);
        		return;
        	}

         	SetPropActiveAndFade(asTimer, false, 1.0f);

        	StopSound("burn"+asTimer, 1.0f);
        }
        //END ACID PUZZLE//
        ///////////////////


        ///////////////////
        //BEGIN EXIT GATE//
        /*Touch the exit gate or lock for quest and hint messages
         */
        void InteractExitGate(string asEntity)
        {
        	AddTimer(asEntity, 0.3f, nameof(TimerExitGate));
        }
        void TimerExitGate(string asTimer)
        {
        	if(asTimer== "padlock_rusty_1"){
        		SetMessage("Ch02Level15", "IntRustyLock", 0);

        	}
        	else if(asTimer== "padlock_acid_1"){
        		SetMessage("Ch02Level15", "IntAcidLock", 0);

        	} else {
        		SetMessage("Ch02Level15", "IntExitGate", 0);
        	}

        	AddQuest("15ExitGate", "15ExitGate");
        }

        /*Messages with hints for various use of items on exit gate/lock,
        **tear the lock on acid use, break it on hammer use after acid use
         */
        void UseItemOnExit(string asItem, string asEntity)
        {
        	if(asItem == "glass_container_1")
        		SetMessage("Ch02Level15", "UseGlassOnExit", 0);

        	if(asItem == "handdrill")
        		SetMessage("Ch02Level15", "UseDrillOnExit", 0);

        	if(asItem == "glass_container_filled" && asEntity != "padlock_acid_1")
        		TearLock();

        	if(asItem == "glass_container_filled" && asEntity == "padlock_acid_1")
        		SetMessage("Ch02Level15", "UseAcidOnAcidLock", 0);

        	if(asItem == "stone_hammer_chipper"){
        		if(GetLocalVarInt("AcidOnGate") == 0){
        			SetMessage("Ch02Level15", "UseOnGateFail", 0);

        		} else{

        			RemoveUseItemCallback("hce");
        			RemoveUseItemCallback("hcp");
        			RemoveUseItemCallback("hcpa");

        			SetEntityActive("stone_hammer_move_1", true);

        			AddTimer("hactive", 0.05f, nameof(TimerActiveHammer));
        			AddTimer("hdie", 0.25f, nameof(TimerActiveHammer));
        		}
        	}
        }

        /*Using the acid will wear the lock some more
         */
        void TearLock()
        {
        	if(GetLocalVarInt("AcidOnGate") == 1) return;

        	RemoveItem("glass_container_filled");
        	GiveItemFromFile("glass_container_1", "glass_container.ent");

        	SetLocalVarInt("AcidOnGate", 1);

        	FadeLightTo("PointLight_14", 0.0f, 1.0f, 0.3f, 1.0f, -1, 1);
        	PlaySoundAtEntity("burnlock", "puzzle_acid", "prison_section_2", 1.0f, false);

        	AddTimer("endLight", 1.0f, nameof(TimerEndlight));

        	SetPropActiveAndFade("padlock_rusty_1", false, 2.0f);
        	SetPropActiveAndFade("padlock_acid_1", true, 1.0f);
        }
        void TimerEndlight(string asTimer)
        {
        	FadeLightTo("PointLight_14", 0.0f, 0.0f, 0.0f, 0.0f, -1, 1);
        }

        /*Complete the quest and break lock if all done properly
         */
        void EntityCallBreakLock(string asEntity, string type)
        {
        	PlaySoundAtEntity("break", "impact_metal_high", "stone_hammer_move_1", 0.0f, false);

        	SetMoveObjectState("stone_hammer_move_1", 0.0f);

        	SetSwingDoorLocked("prison_section_2",false, false);

        	GiveSanityBoostSmall();

        	CompleteQuest("15ExitGate", "15ExitGate");

        	SetEntityPlayerInteractCallback("prison_section_2", "", false);

        	AddTimer("removeh", 0.3f, nameof(TimerRemoveHammer));

        	AddTimer("servant_grunt_6", 1.0f, nameof(TimerStartGrunt));

        	CheckPoint("checkpoint3","PlayerStartArea_12",nameof(CPCall03), "", "");
        }
        void TimerActiveHammer(string asTimer)
        {
        	if(asTimer == "hdie"){
        		SetPropHealth("padlock_acid_1", 0.0f);
        		return;
        	}

        	PlaySoundAtEntity("swing", "player_crouch", "Player", 0.0f, false);

        	SetMoveObjectState("stone_hammer_move_1", 0.2f);
        }
        void TimerRemoveHammer(string asTimer)
        {
        	SetEntityActive("stone_hammer_move_1", false);
        }
        void TimerStartGrunt(string asTimer)
        {
        	SetEntityActive(asTimer, true);
        }
        //END EXIT GATE//
        /////////////////


        //////////////////////////
        //BEGIN ENEMY ENCOUNTERS//
        /*When in kitchen a grunt is activated in the corridor outside
         */
        void EntityCallPickGlass(string asEntity, string type)
        {
        	SetEntityActive("AreaKitchenGrunt", true);	//Grunt in kitchen will activate.
        }
        void CollideKitchenGrunt(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("EnemyActive") != 0) return;

        	SetEntityActive(asChild, true);

        	if(HasItem("glass_container_1")){
        		SetEntityActive("servant_grunt_1", true);

        		CheckPoint("checkpoint1","PlayerStartArea_8",nameof(CPCall01), "", "");
        	}
        }
        /*Enemy disables, only play sound if grunt disables at door*/
        void CollideAreaGrunt1Exit(string asParent, string asChild, int alState)
        {
        	SetLocalVarInt(asParent, alState);
        }
        void EntityCallEnemyKitchen(string asEntity, string type)
        {
        	if(GetLocalVarInt(asEntity) != 1) return;

        	PlaySoundAtEntity("doorleave", "open_door.snt", "cellar_wood01_4", 0.0f, false);

        	AddTimer("close_door.snt", 1.5f, nameof(TimerEnemyKitchenDoor));
        }
        void TimerEnemyKitchenDoor(string asTimer)
        {
        	PlaySoundAtEntity("doorleave", asTimer, "cellar_wood01_4", 0.0f, false);
        }

        /*When walking down the long corridor to the storage, a brute is seen far away
         */
        void CollideCorridorBrute(string asParent, string asChild, int alState)
        {
        	if(asParent == "Player"){
        		SetEntityActive("servant_brute_1", true);

        		//To great risk of missing the trigger
        		//SetEntityPlayerLookAtCallback("AreaCorridorBruteLook", "LookAtBrute", true);

        		AddTimer("noticegrunt", 1.0f, nameof(TimerNoticeGrunt));

        		CheckPoint("checkpoint2","PlayerStartArea_9",nameof(CPCall02), "", "");
        	}
        	else {
        		SetEntityActive("servant_brute_1", false);

        		PlaySoundAtEntity("bruteleave", "attack_claw", "AreaCorridorBruteDisable", 0.0f, false);
        		PlaySoundAtEntity("bruteslam", "15_slam_door", "AreaCorridorBruteDisable", 0.5f, false);

        		StartScreenShake(0.01f, 0.5f, 0.5f,1.0f);
        	}
        }
        /*When seeing the brute, scare player and slow him down so brute event has time to finish
         */
        void TimerNoticeGrunt(string asTimer)
        {
        	GiveSanityDamage(30.0f, true);

        	FadeRadialBlurTo(0.1f, 1);
        	SetRadialBlurStartDist(0.2f);

        	SetPlayerMoveSpeedMul(0.8f);
        	SetPlayerRunSpeedMul(0.8f);

        	AddEntityCollideCallback("Player", "AreaCorridorServant_1", nameof(CollideCorridorServant), false, 1);

        	AddTimer("stepchange", 0.3f, nameof(TimerPlayerSpeed));
        }
        /*When seeing the brute, scare player and slow him down so brute event has time to finish
         */
        /* void LookAtBrute(string &in asEntity, int alState)
        {
        	GiveSanityDamage(30.0f, true);

        	FadeRadialBlurTo(0.1f, 1);
        	SetRadialBlurStartDist(0.2f);

        	SetPlayerMoveSpeedMul(0.8f);
        	SetPlayerRunSpeedMul(0.8f);

        	AddEntityCollideCallback("Player", "AreaCorridorServant_1", "CollideCorridorServant", true, 1);

        	AddTimer("stepchange", 0.3f, "TimerPlayerSpeed");
        } */
        void TimerPlayerSpeed(string asTimer)
        {
        	AddLocalVarInt("PlayerSpeed", 1);

        	if(GetLocalVarInt("PlayerSpeed") < 8) AddTimer("stepchange", 0.5f, nameof(TimerPlayerSpeed));

        	switch(GetLocalVarInt("PlayerSpeed")){
        		case 1:
        			PlaySoundAtEntity("eee", "scare_whine_loop1", "Player", 2.0f, false);
        			SetPlayerMoveSpeedMul(0.7f);
        			SetPlayerRunSpeedMul(0.7f);
        			PlaySoundAtEntity("ii", "react_scare", "Player", 0.1f, false);
        		break;
        		case 2:
        			SetPlayerMoveSpeedMul(0.6f);
        			SetPlayerRunSpeedMul(0.6f);
        		break;
        		case 3:
        			SetPlayerMoveSpeedMul(0.4f);
        			SetPlayerRunSpeedMul(0.4f);
        			PlaySoundAtEntity("b1", "react_breath", "Player", 0.1f, false);
        			RemoveTimer("stepchange");
        			AddTimer("stepchange", 1.5f, nameof(TimerPlayerSpeed));
        		break;
        		case 4:
        			SetPlayerMoveSpeedMul(0.5f);
        			SetPlayerRunSpeedMul(0.5f);
        			PlaySoundAtEntity("b1", "react_breath", "Player", 0.1f, false);
        			FadeRadialBlurTo(0.0f, 1);
        			RemoveTimer("stepchange");
        			AddTimer("stepchange", 1.0f, nameof(TimerPlayerSpeed));
        		break;
        		case 5:
        			SetPlayerMoveSpeedMul(0.6f);
        			SetPlayerRunSpeedMul(0.6f);
        			PlaySoundAtEntity("b2", "react_breath", "Player", 0.2f, false);
        		break;
        		case 6:
        			SetPlayerMoveSpeedMul(0.7f);
        			SetPlayerRunSpeedMul(0.7f);
        		break;
        		case 7:
        			SetPlayerMoveSpeedMul(0.8f);
        			SetPlayerRunSpeedMul(0.8f);
        			PlaySoundAtEntity("b3", "react_breath", "Player", 0.4f, false);
        		break;
        		case 8:
        			SetPlayerMoveSpeedMul(1.0f);
        			SetPlayerRunSpeedMul(1.0f);
        			SetLocalVarInt("PlayerSpeed", 0);
        		break;
        	}
        }

        /*After having seen the brute and been in the storage corridor, a grunt is activated on the way back
         */
        void CollideCorridorServant(string asParent, string asChild, int alState)
        {
        	if(asChild == "AreaCorridorServant_2"){
        		if(GetLocalVarInt("GruntDoor") == 1) return;

        		SetSwingDoorDisableAutoClose("prison_8", true);
        		SetSwingDoorClosed("prison_8", false, false);

        		AddPropImpulse("prison_8", 0, 0, -3, "World");

        		SetEntityActive("servant_grunt_2", true);
        		SetLocalVarInt("GruntDoor", 1);

        		SetEntityActive("AreaCorridorServant_2", false);

        		CheckPoint("cp_wayback", "PlayerStartArea_6", nameof(CPCallWayBack), "", "");
        	}
        	else if(asChild == "AreaCorridorServant_1"){
        		AddEntityCollideCallback("Player", "AreaCorridorServant_2", nameof(CollideCorridorServant), false, 1);

        		SetEntityActive("AreaCorridorServant_1", false);
        	}
        }
        void EntityCallCorridorServant(string asEntity, string type)
        {
        	//SetLocalVarInt("GruntDoor", 0);	//Doing this no good, as player can be in room and know it is empty

        	AddDebugMessage("Corridor Servant Disabled", false);
        }
        void CPCallWayBack(string asName, int alCount)
        {
        	SetSwingDoorClosed("prison_8", true, false);

        	SetLocalVarInt("GruntDoor", 0);	//The grunt in double prison can activate again, regardless if he has before or not.
        }


        /*After picking diary in west wing 3 rooms corridor, grunt activates on way back
         */
        void EntityCallPickDiary(string asEntity, string type)
        {
        	if(asEntity == "diary_paper01_1") SetEntityActive("AreaNoteGrunt", true);

        	AddLocalVarInt("DiaryMusic", 1);

        	PlayMusic("15_paper_daniel0"+GetLocalVarInt("DiaryMusic"), false, 0.85f, 0.0f, 10, false);
        }
        void CollideAreaNoteGrunt(string asParent, string asChild, int alState)
        {
        	SetEntityActive("servant_grunt_4", true);

        	CheckPoint("grunt_4", "PlayerStartArea_14", nameof(CPCallGrunt4), "", "");
        }
        void CPCallGrunt4(string asName, int alCount)
        {
        	//SetEntityActive("cp4_rat_*", true);
        	AddTimer("cp4", 4.0f, nameof(TimerCP4Rat));
        }
        void TimerCP4Rat(string asTimer)
        {
        	//SetPropActiveAndFade("cp4_rat_*", false, 3);
        }
        //END ENEMY ENCOUNTERS//
        ////////////////////////


        ///////////////////
        //BEGIN CP EVENTS//
        /*If dying after the grunt in kitches has been activated
         */
        void CPCall01(string asName, int alCount)
        {
        	SetPlayerCrouching(true);

        	SetPropActiveAndFade("slime_*", true, 4.0f);

        	SetEntityActive("SlimeDamageArea_3", true);
        	SetEntityActive("SlimeDamageArea_4", true);

        	for(int i=1;i<7;i++) CreateParticleSystemAtEntity("slimeps"+i, "ps_slime_fog", "AreaPS_"+i, true);
        	for(int i=1;i<6;i++) FadeLightTo("LightSiime_"+i, 0.2f, 0.04f, 0.08f, 1.0f, -1, 4.0f);

        	PlaySoundAtEntity("slimes1", "slime_loop", "AreaPS_1", 4.0f, true);
        	PlaySoundAtEntity("slimes3", "slime_loop", "AreaPS_3", 4.0f, true);
        	PlaySoundAtEntity("slimes5", "slime_loop", "AreaPS_5", 4.0f, true);
        	PlaySoundAtEntity("guard", "guardian_activated.snt", "Player", 1.5f, false);

        	AddTimer("splat", 0.4f, nameof(TimerSplat));
        	AddTimer("loop", 1.0f, nameof(TimerLoopSlime));

        	SetLocalVarInt("SlimeRemove", 1);
        }
        void TimerSplat(string asTimer)
        {
        	AddLocalVarInt("Splat", 1);

        	PlaySoundAtEntity("splat"+GetGlobalVarInt("Splat"), "slime_create", "Player", RandFloat(0.1f, 0.3f), false);

        	if(GetLocalVarInt("Splat") <= 6) AddTimer("splat", RandFloat(0.2f, 0.8f), nameof(TimerSplat));
        }
        void TimerLoopSlime(string asTimer)
        {
        	if(asTimer == "loops"){
        		StopSound("loops", 4.0f);
        		StopSound("loops2", 4.0f);
        		PlaySoundAtEntity("breath", "react_breath", "Player", 0.6f, false);
        		FadePlayerFOVMulTo(1.0f, 0.3f);
        		FadeRadialBlurTo(0, 0.1f);
        		return;
        	}

        	PlaySoundAtEntity("loops", "general_rock_rumble_no3d", "Player", 10.0f, false);
        	PlaySoundAtEntity("loops2", "15_cloud_swarm", "Player", 8.0f, false);
        	PlaySoundAtEntity("loops2", "15_cloud", "Player", 7.0f, false);

        	FadePlayerFOVMulTo(0.5f, 0.1f);

        	FadeRadialBlurTo(0.1f, 0.025f);
        	SetRadialBlurStartDist(0.2f);

        	StartScreenShake(0.01f, 1.0f, 2.0f,3.0f);

        	AddTimer("loop", 10.0f, nameof(TimerLoopSlime));
        	AddTimer("loops", 4.0f, nameof(TimerLoopSlime));
        }
        void CollideAreaRemoveSlime(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("SlimeRemove") != 1) return;

        	//SetPropActiveAndFade("slime_*", false, 1.0f);

        	//for(int i=1;i<7;i++) DestroyParticleSystem("slimeps"+i);
        	//for(int i=1;i<6;i++) FadeLightTo("LightSiime_"+i, 0, 0, 0, 0, -1, 1.0f);

        	//StopSound("slimes1", 1.0f); StopSound("slimes3", 1.0f); StopSound("slimes5", 1.0f);
        	StopSound("loops", 1.0f); StopSound("loops2", 1.0f);

        	RemoveTimer("loop"); RemoveTimer("loops");

        	PlaySoundAtEntity("breath", "react_scare", "Player", 0.6f, false);
        	PlaySoundAtEntity("breath", "react_breath", "Player", 0.6f, false);
        	//PlaySoundAtEntity("crak", "sanity_flick", "Player", 0.0f, false);

        	FadePlayerFOVMulTo(1.0f, 1.0f);
        	FadeRadialBlurTo(0, 1.0f);

        	//StartScreenShake(0.1f, 0.1f, 0.0f, 0.0f);

        	//AddTimer("shake", 0.3f, "TimerSlimeEnd");

        	AddLocalVarInt("SlimeRemove", 1);
        }
        void TimerSlimeEnd(string asTimer)
        {
        	StartScreenShake(0.1f, 0.1f, 0.1f, 0.0f);

        	PlaySoundAtEntity("crak", "sanity_flick", "Player", 0.2f, false);
        }

        /*If dying after the second grunt has been activated
         */
        void CPCall02(string asName, int alCount)
        {
        	if(GetGlobalVarInt("15CPEvent") == 0){
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
        	} else {
        		SetPlayerCrouching(false);

        		PlaySoundAtEntity("guard", "guardian_activated.snt", "Player", 1.0f, false);

        		TeleportPlayer("PlayerStartArea_10");

        		StartPlayerLookAt("AreaLookCP02", 4.0f, 4.0f, "");

        		SetSwingDoorLocked("prison_8", true, false);

        		AddTimer("1", 0.5f, nameof(TimerCP02Event));

        		PlaySoundAtEntity("slime1", "slime_loop_no3d", "Player", 1.0f, false);
        		PlaySoundAtEntity("slime2", "slime_loop", "slime02_1", 1.0f, false);
        		PlaySoundAtEntity("slime3", "slime_loop", "slime02_5", 1.0f, false);
        		PlaySoundAtEntity("swarm", "15_cloud", "Player", 10.0f, false);

        		FadeRadialBlurTo(0.1f, 0.025f);
        		SetRadialBlurStartDist(0.2f);

        		SetEntityActive("SlimeDamageArea_1", true);
        		SetEntityActive("SlimeDamageArea_2", true);

        		SetPropActiveAndFade("slime02_*", true, 4.0f);

        		SetGlobalVarInt("15CPEvent", 0);
        	}
        }

        /*For version1 of CP02
         */
        void TimerCPEvent(string asTimer)
        {
        	if(asTimer == "1"){
        		SetEntityActive("servant_grunt_3", true);
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
        		PlaySoundAtEntity("aa", "react_scare.snt", "Player", 0.5f, false);
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

        /*For version2 of CP02
         */
        void LookAtPrisonDoor(string asEntity, int alState)
        {
        	AddTimer("2", 0.1f, nameof(TimerCP02Event));
        	AddTimer("3", 2.2f, nameof(TimerCP02Event));
        	AddTimer("4", 2.5f, nameof(TimerCP02Event));
        	AddTimer("5", 2.7f, nameof(TimerCP02Event));
        }
        void TimerCP02Event(string asTimer)
        {
        	if(asTimer == "1"){
        		StopPlayerLookAt();
        		SetEntityPlayerLookAtCallback("prison_8", nameof(LookAtPrisonDoor), true);
        	}
        	else if(asTimer == "2"){
        		PlaySoundAtEntity("eeek", "scare_whine_loop3", "Player", 2.0f, false);
        		PlaySoundAtEntity("guardian", "guardian_distant3", "Player", 0.0f, false);
        	}
        	else if(asTimer == "3"){
        		SetSwingDoorLocked("prison_8", false, false);
        		StartScreenShake(0.1f, 0.1f, 0.1f, 0.1f);
        		PlaySoundAtEntity("flick1", "sanity_flick.snt", "Player", 0.0f, false);
        		FadeLightTo("CPLight02", 1, 1, 1, 1, -1, 0.1f);
        		AddTimer("lightoff", 0.2f, nameof(TimerLightOff02));
        	}
        	else if(asTimer == "4"){
        		FadeLightTo("CPLight02", 1, 1, 1, 1, -1, 0.1f);
        		AddTimer("lightoff", 0.2f, nameof(TimerLightOff02));
        		StartScreenShake(0.3f, 0.1f, 0.1f, 0.1f);
        		PlaySoundAtEntity("flick1", "sanity_flick.snt", "Player", 0.0f, false);
        		FadeRadialBlurTo(0.0f, 0.1f);
        		StopSound("slime1", 0.4f); StopSound("slime2", 0.4f); StopSound("slime3", 0.4f);
        		StopSound("eeek", 0.4f);
        		StopSound("guardian", 0.4f);
        		StopSound("swarm", 0.4f);
        		SetPropActiveAndFade("slime02_*", false, 0.8f);
        	}
        	else if(asTimer == "5"){
        		PlaySoundAtEntity("aa", "react_scare.snt", "Player", 0.5f, false);
        		GiveSanityDamage(10.0f, true);
        	}
        }
        void TimerLightOff02(string asTimer)
        {
        	FadeLightTo("CPLight02", 0, 0, 0, 1, -1, 0.1f);
        }

        /*For the Grunt after picking Diary
         */
        void CPCall03(string asName, int alCount)
        {
        	SetEntityActive("torch_cp_*", true);
        	SetEntityActive("torch_empty_*", false);

        	SetLampLit("torch_cp_*", true, false);

        	SetEntityActive("AreaCP03", true);

        	SetEntityActive("AreaDarkSteps", false);	//No dark sneaky steps as this area is now lit and comfy (except the 3 grunts of course, but they enjoy a nice cup of tea too. honest.)

        	AddEnemyPatrolNode("servant_grunt_7", "PathNodeArea_197", 5.0f, "");
        	AddEnemyPatrolNode("servant_grunt_7", "PathNodeArea_94", 0.0f, "");

        	AddEnemyPatrolNode("servant_grunt_8", "PathNodeArea_198", 15.0f, "");
        	AddEnemyPatrolNode("servant_grunt_8", "PathNodeArea_102", 0.0f, "");

        	SetEntityActive("servant_grunt_7", true);
        	SetEntityActive("servant_grunt_8", true);

        	SetLampLit("torch_static01_17", true, true);
        	SetLampLit("torch_static01_18", true, true);

        	ResetProp("prison_section_static_2");
        	ResetProp("padlock_2");
        	ResetProp("padlock_2_replace");
        }
        void CollideAreaCP03(string asParent, string asChild, int alState)
        {
        	AddEnemyPatrolNode("servant_grunt_9", "PathNodeArea_119", 3.0f, "");
        	AddEnemyPatrolNode("servant_grunt_9", "PathNodeArea_133", 0.0f, "");

        	SetEntityActive("servant_grunt_9", true);

        	PlaySoundAtEntity("door_open", "15_open_door", "cellar_wood01_2", 0.0f, false);
        }
        //END CP EVENTS//
        /////////////////


        /////////////////////
        //BEGIN TUNNEL DUST//
        /*Some shaking and dust falling in the tunnel at Start
         */
        void TimerTunnelDust(string asTimer)
        {
        	CreateParticleSystemAtEntity("dustyfally", "ps_debris_tunnel.ps", "AreaDustFall_"+RandInt(1,7), false);
        	PlaySoundAtEntity("dustynoise", "03_rock_move.snt", "Player", 2.0f, false);

        	AddTimer("stoprsound", 1, nameof(TimerStopTunnelDust));

        	AddTimer(asTimer, RandFloat(3.0f, 7.0f), asTimer);

        	StartScreenShake(0.003f, 0.2f, 1.0f, 1.0f);
        }
        void TimerStopTunnelDust(string asTimer)
        {
        	StopSound("dustynoise", 4.0f);

        	PlayMusic("04_amb.ogg", true, 0.9f, 4.0f, 0, true);
        }
        /*When exiting the tunnel, stop the effect*/
        void CollideAreaRemoveDust(string asParent, string asChild, int alState)
        {
        	RemoveTimer(nameof(TimerTunnelDust));
        }

        /*Stop the look at for the start of the game*/
        void LookAtIntro(string asTimer)
        {
        	StopPlayerLookAt();
        }
        //END TUNNEL DUST//
        ///////////////////


        //BEGIN KITCHEN FIRE//
        void CollideAreaFire(string asParent, string asChild, int alState)
        {
        	if(alState == 1) AddTimer("burn", 0.1f, nameof(TimerBurnFire));
        	else RemoveTimer("burn");
        }
        void InteractFire(string asEntity)
        {
        	TimerBurnFire("touch");
        }
        void TimerInteractFire(string asTimer)
        {
        	SetEntityPlayerInteractCallback("AreaFire", nameof(InteractFire), true);
        }
        void TimerBurnFire(string asTimer)
        {
        	if(asTimer == "touch"){
        		PlayGuiSound("15_player_burn.snt", 1.0f);
        		GivePlayerDamage(5.0f, "BloodSplat", false,false);
        		AddTimer(nameof(InteractFire), 1.0f, nameof(TimerInteractFire));
        		return;
        	}

        	PlayGuiSound("15_player_burn.snt", 1.0f);
        	GivePlayerDamage(5.0f, "BloodSplat", false,false);

        	AddTimer("burn", 1.0f, nameof(TimerBurnFire));
        }
        //END KITCHEN FIRE//


        //BEGIN BANG HEAD//
        void CollideAreaBodyFallActivate(string asParent, string asChild, int alState)
        {
        	RemoveTimer(nameof(TimerBangHead));
        	PlaySoundAtEntity("AreaBodyFallBang", "15_bang_fall.snt", "AreaBodyFall", 0.0f, false);
        }
        void TimerBangHead(string asTimer)
        {
        	PlaySoundAtEntity("AreaBodyFallBang", "15_bang.snt", "AreaBodyFall", 0.0f, false);
        	AddTimer(nameof(TimerBangHead), RandFloat(4.0f,6.0f), nameof(TimerBangHead));
        }
        //END BANG HEAD//


        //Prison gate - break padlock then gate breaks
        void EntityCallPrisonGate(string asEntity, string type)
        {
        	SetEntityActive("padlock_2", false);
        	SetEntityActive("padlock_2_replace", true);
        }


        /////////////////
        //Grunt in double cell
        void InteractGruntDoor(string asEntity)
        {
        	if(GetLocalVarInt("GruntDoor") == 1) return;	//If player went to storage, this event will be cancled

        	SetEntityActive("servant_grunt_5", true);
        	SetLocalVarInt("GruntDoor", 1);

        	SetEntityPlayerInteractCallback(asEntity, "", true);

        	CheckPoint("cp_gruntdoor", "PlayerStartArea_9", nameof(CPCallGruntDoor), "", "");

        	AddTimer("scare", 2.5f, nameof(TimerPlayerreact));
        	AddTimer("breath", 4.0f, nameof(TimerPlayerreact));
        	AddTimer("breathl", 6.0f, nameof(TimerPlayerreact));
        }
        void TimerPlayerreact(string asTimer)
        {
        	if(asTimer == "breathl")
        		PlayGuiSound("react_"+asTimer, 0.4f);
        	else
        		PlayGuiSound("react_"+asTimer, 0.7f);

        	if(asTimer == "scare")
        		GiveSanityDamage(10.0f, true);
        }
        void EntityCallGruntDoor(string asEntity, string type)
        {
        	SetLocalVarInt("GruntDoor", 0);

        	AddDebugMessage("Grunt Door Gone", false);
        }
        void CPCallGruntDoor(string asName, int alCount)
        {
        	SetLocalVarInt("GruntDoor", 0);
        	SetPlayerCrouching(true);
        	PlaySoundAtEntity("grunt1", "grunt/amb_alert.snt", "AreaLookCP_1", 0.0f, false);
        	AddTimer("sound2", 2.0f, nameof(TimerCPGruntDoor));
        }
        void TimerCPGruntDoor(string asTimer)
        {
        	PlaySoundAtEntity("grunt2", "grunt/amb_idle.snt", "AreaLookCP_1", 0.0f, false);
        }


        ////////////////////
        //Dark echo steps
        void CollideAreaDarkSteps(string asParent, string asChild, int alState)
        {
        	if(alState == 1){
        		AddTimer("dark_step", RandFloat(0.0f, 1.5f), nameof(TimerDarkSteps));
        	}
        	else{
        		RemoveTimer("dark_step");
        		PlayGuiSound("step_sneak_hard_water.snt", 0.9f);
        	}
        }
        //Play steps at various speed and volume depending on movement of player
        void TimerDarkSteps(string asTimer)
        {
        	if(GetPlayerSpeed() > 3){
        		AddTimer("dark_step", 0.5f, nameof(TimerDarkSteps));

        		PlayGuiSound("step_run_hard_water.snt", 0.3f);
        		SetLocalVarInt("StopStep", 0);

        		AddDebugMessage("Speed High", false);
        	}
        	if(GetPlayerSpeed() > 2 && GetPlayerSpeed() < 3){
        		AddTimer("dark_step", 0.7f, nameof(TimerDarkSteps));

        		PlayGuiSound("step_walk_hard_water.snt", 0.4f);
        		SetLocalVarInt("StopStep", 0);

        		AddDebugMessage("Speed Med", false);
        	}
        	if(GetPlayerSpeed() > 1 && GetPlayerSpeed() < 2){
        		AddTimer("dark_step", 0.9f, nameof(TimerDarkSteps));

        		PlayGuiSound("step_sneak_hard_water.snt", 0.8f);
        		SetLocalVarInt("StopStep", 0);

        		AddDebugMessage("Speed Low", false);
        	}
        	if(GetPlayerSpeed() < 1){
        		AddTimer("dark_step", 1.0f, nameof(TimerDarkSteps));

        		if(GetLocalVarInt("StopStep") != 0) return;

        		PlayGuiSound("step_sneak_hard_water.snt", 1.0f);
        		SetLocalVarInt("StopStep", 1);

        		AddDebugMessage("Speed Silent", false);
        	}
        }

        ///////////////////
        //Enemy at Start
        void CollideAreaACStart(string asParent, string asChild, int alState)
        {
        	SetEntityActive("AreaAroundCorner_1", true);
        	SetEntityActive("AreaAroundCorner_2", true);
        }
        void CollideAreaAroundCorner(string asParent, string asChild, int alState)
        {
        	if(GetLocalVarInt("GruntDoor") == 1) return;	//If any of the grunts from the corridor to storage is active, skip this.

        	SetEntityActive("servant_grunt_10", true);

        	AddTimer("scare", 1.0f, nameof(TimerPlayerreact));
        	AddTimer("breath", 3.0f, nameof(TimerPlayerreact));
        	AddTimer("breathl", 5.0f, nameof(TimerPlayerreact));

        	SetEntityActive("AreaAroundCorner_1", false);
        	SetEntityActive("AreaAroundCorner_2", false);
        	SetEntityActive("AreaACStart_1", false);
        	SetEntityActive("AreaACStart_2", false);

        	SetLocalVarInt("EnemyActive", 1);

        	CheckPoint("cp_start", "PlayerStartArea_13", nameof(CPCallStartGrunt), "", "");
        }
        void EntityCallEnemyRemoved(string asEntity, string type)
        {
        	SetLocalVarInt("EnemyActive", 0);

        	AddDebugMessage("Removed AroundCorner Grunt", false);
        }
        void CPCallStartGrunt(string asName, int alCount)
        {
        	SetEntityActive("AreaCPStart", true);
        	AddEntityCollideCallback("Player", "AreaCPStart", nameof(CollideAreaCPStart), true, 1);
        	SetLocalVarInt("EnemyActive", 0);
        }
        void CollideAreaCPStart(string asParent, string asChild, int alState)
        {
        	AddPropImpulse("cellar_wood01_4", 4.0f, 0.0f, 0.0f, "World");

        	CreateParticleSystemAtEntity("pscp", "ps_hit_wood.ps", "cellar_wood01_4", false);
        	PlaySoundAtEntity("scp", "impact_wood_heavy_high.snt", "cellar_wood01_4", 0.0f, false);
        	PlaySoundAtEntity("grunt", "grunt/amb_alert.snt", "cellar_wood01_4", 0.0f, false);

        	AddTimer("bang1", 0.8f, nameof(TimerCPStart));
        	AddTimer("bang2", 1.2f, nameof(TimerCPStart));

        	AddTimer("scare", 0.5f, nameof(TimerPlayerreact));
        	AddTimer("breath", 2.5f, nameof(TimerPlayerreact));
        	AddTimer("breathl", 4.5f, nameof(TimerPlayerreact));
        }
        void TimerCPStart(string asTimer)
        {
        	CreateParticleSystemAtEntity("pscp", "ps_hit_wood.ps", "cellar_wood01_4", false);
        	PlaySoundAtEntity("scp", "impact_wood_heavy_high.snt", "cellar_wood01_4", 0.0f, false);
        }

        /////////////////////
        //Enemy in the long dead end
        void CollideAreaLongCorridorGrunt(string asParent, string asChild, int alState)
        {
        	SetEntityActive("servant_grunt_11", true);

        	CheckPoint("cp_corridor", "PlayerStartArea_12", nameof(CPCallLongCorridor), "", "");
        }
        void CPCallLongCorridor(string asName, int alCount)
        {
        	SetEntityActive("AreaLongCP", true);
        	SetEntityActive("AreaLongCP_1", true);

        	AddEntityCollideCallback("Player", "AreaLongCP", nameof(CollideAreaLongCP), true, 1);
        	AddEntityCollideCallback("Player", "AreaLongCP_1", nameof(CollideAreaLongCP), true, 1);

        	SetEntityActive("cp_roach_*", true);
        }
        void CollideAreaLongCP(string asParent, string asChild, int alState)
        {
        	SetPropActiveAndFade("cp_roach_*", false, 5.0f);

        	SetEntityActive("AreaLongCP_1", false);
        	SetEntityActive("AreaLongCP", false);
        }

        //Interqact on door to activate Vision
        void InteractVisionDoor(string asEntity)
        {
        	SetEntityActive("FlashbackArea_1", true);
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
        	SetMapDisplayNameEntry("PrisonNorth");

        	SetPlayerCrouching(true);

        	//---- COLLIDE CALLBACKS ----//
        	AddEntityCollideCallback("Player", "AlexanderArea", nameof(CollideAlexanderArea), false, 0);
        	AddEntityCollideCallback("Player", "FlashbackArea_1", nameof(CollideNoAlex), true, 1);
        	AddEntityCollideCallback("Player", "AcidDamage", nameof(CollideAcidDamage), false, 1);
        	AddEntityCollideCallback("Player", "AreaKitchenGrunt", nameof(CollideKitchenGrunt), false, 1);
        	AddEntityCollideCallback("Player", "AreaCorridorBrute", nameof(CollideCorridorBrute), true, 1);
        	AddEntityCollideCallback("servant_brute_1", "AreaCorridorBruteDisable", nameof(CollideCorridorBrute), true, 1);

        	AddEntityCollideCallback("Player", "AreaHelpMe", nameof(CollideAreaHelpMe), true, 1);

        	for(int i=1;i<=44;i++)
        		AddEntityCollideCallback("object_"+i, "AcidArea", nameof(CollideAcidWater), true, 1);

        	for(int i=1;i<=5;i++)
        		AddEntityCollideCallback("plate_"+i, "AcidArea", nameof(CollideAcidWater), true, 1);

        	AddEntityCollideCallback("pot_1", "AcidArea", nameof(CollideAcidWater), false, 1);

        	for(int i=1;i<=5;i++)
        		AddEntityCollideCallback("glass_"+i, "AcidArea", nameof(CollideAcidWater), false, 1);

        	AddEntityCollideCallback("Player", "AreaRemoveSlime", nameof(CollideAreaRemoveSlime), false, 1);
        	AddEntityCollideCallback("Player", "AreaNoteGrunt", nameof(CollideAreaNoteGrunt), true, 1);

        	AddEntityCollideCallback("servant_grunt_1", "AreaGrunt1Exit", nameof(CollideAreaGrunt1Exit), false, 0);
        	AddEntityCollideCallback("servant_grunt_10", "AreaGrunt1Exit", nameof(CollideAreaGrunt1Exit), false, 0);

        	AddEntityCollideCallback("Player", "AreaRemoveDust", nameof(CollideAreaRemoveDust), true, 1);

        	AddEntityCollideCallback("Player", "AreaFire", nameof(CollideAreaFire), false, 0);

        	AddEntityCollideCallback("Player", "AreaBodyFallActivate", nameof(CollideAreaBodyFallActivate), true, 1);

        	AddEntityCollideCallback("Player", "AreaCP03", nameof(CollideAreaCP03), true, 1);

        	AddEntityCollideCallback("Player", "AreaDarkSteps", nameof(CollideAreaDarkSteps), false, 0);

        	AddEntityCollideCallback("Player", "AreaAroundCorner_1", nameof(CollideAreaAroundCorner), true, 1);
        	AddEntityCollideCallback("Player", "AreaAroundCorner_2", nameof(CollideAreaAroundCorner), true, 1);
        	AddEntityCollideCallback("Player", "AreaACStart_1", nameof(CollideAreaACStart), true, 1);
        	AddEntityCollideCallback("Player", "AreaACStart_2", nameof(CollideAreaACStart), true, 1);

        	AddEntityCollideCallback("Player", "AreaLongCorridorGrunt", nameof(CollideAreaLongCorridorGrunt), true, 1);

        	//---- INTERACT CALLBACKS ----//
        	AddUseItemCallback("bottleEcon", "glass_container_1", "acid_container_1", nameof(UseBottle), true);
        	AddUseItemCallback("bottleFcon", "glass_container_filled", "acid_container_1", nameof(UseBottle), false);
        	AddUseItemCallback("hammerchipperFcon", "stone_hammer_chipper", "acid_container_1", nameof(UseBottle), false);
        	AddUseItemCallback("drillFcon", "handdrill", "acid_container_1", nameof(UseBottle), false);

        	AddUseItemCallback("hce", "stone_hammer_chipper", "prison_section_2", nameof(UseItemOnExit), false);
        	AddUseItemCallback("hcp", "stone_hammer_chipper", "padlock_rusty_1", nameof(UseItemOnExit), false);
        	AddUseItemCallback("hde", "handdrill", "prison_section_2", nameof(UseItemOnExit), false);
        	AddUseItemCallback("hdp", "handdrill", "padlock_rusty_1", nameof(UseItemOnExit), false);
        	AddUseItemCallback("gce", "glass_container_1", "prison_section_2", nameof(UseItemOnExit), false);
        	AddUseItemCallback("gcp", "glass_container_1", "padlock_rusty_1", nameof(UseItemOnExit), false);
        	AddUseItemCallback("gcfe", "glass_container_filled", "prison_section_2", nameof(UseItemOnExit), false);
        	AddUseItemCallback("gcfpa", "glass_container_filled", "padlock_rusty_1", nameof(UseItemOnExit), false);
        	AddUseItemCallback("gcfpa", "glass_container_filled", "padlock_acid_1", nameof(UseItemOnExit), false);
        	AddUseItemCallback("gcpa", "glass_container_1", "padlock_acid_1", nameof(UseItemOnExit), false);
        	AddUseItemCallback("hdpa", "handdrill", "padlock_acid_1", nameof(UseItemOnExit), false);
        	AddUseItemCallback("hcpa", "stone_hammer_chipper", "padlock_acid_1", nameof(UseItemOnExit), false);

        	for(int i=1;i<=2;i++){
        		AddUseItemCallback("hce", "stone_hammer_chipper", "prison_section_static_"+i, nameof(UseItemOnExtraDoor), false);
        		AddUseItemCallback("hcp", "stone_hammer_chipper", "padlock_"+i, nameof(UseItemOnExtraDoor), false);
        		AddUseItemCallback("hde", "handdrill", "prison_section_static_"+i, nameof(UseItemOnExtraDoor), false);
        		AddUseItemCallback("hdp", "handdrill", "padlock_"+i, nameof(UseItemOnExtraDoor), false);
        		AddUseItemCallback("gce", "glass_container_1", "prison_section_static_"+i, nameof(UseItemOnExtraDoor), false);
        		AddUseItemCallback("gcp", "glass_container_1", "padlock_"+i, nameof(UseItemOnExtraDoor), false);
        		AddUseItemCallback("gcfe", "glass_container_filled", "prison_section_static_"+i, nameof(UseItemOnExtraDoor), false);
        		AddUseItemCallback("gcfp", "glass_container_filled", "padlock_"+i, nameof(UseItemOnExtraDoor), false);
        	}


        	//---- ENEMY PATROLS ----//
        	/*Kitchen enemy
        	 */
        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_3", 0.0f, "");
        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_29", 0.0f, "");
        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_32", 2.0f, "");
        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_33", 0.0f, "");
        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_29", 0.0f, "");
        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_3", 0.0f, "");
        	AddEnemyPatrolNode("servant_grunt_1", "PathNodeArea_10", 0.0f, "");

        	/*Corridor Brute
        	 */
        	AddEnemyPatrolNode("servant_brute_1", "PathNodeArea_167", 0.0f, "");

        	/*Corridor Servant
        	 */
        	AddEnemyPatrolNode("servant_grunt_2", "PathNodeArea_159", 1.0f, "");
        	AddEnemyPatrolNode("servant_grunt_2", "PathNodeArea_106", 5.0f, "");

        	/*Grunt door
        	 */
        	AddEnemyPatrolNode("servant_grunt_5", "PathNodeArea_181", 0.0f, "");
        	AddEnemyPatrolNode("servant_grunt_5", "PathNodeArea_158", 0.0f, "");
        	AddEnemyPatrolNode("servant_grunt_5", "PathNodeArea_114", 0.0f, "");


        	/*CP Servant
        	 */
        	AddEnemyPatrolNode("servant_grunt_3", "PathNodeArea_187", 0.0f, "");

        	/*Diary Servant
        	 */
        	AddEnemyPatrolNode("servant_grunt_4", "PathNodeArea_105", 5.0f, "IdleExtra2");
        	AddEnemyPatrolNode("servant_grunt_4", "PathNodeArea_112", 3.0f, "IdleExtra1");
        	AddEnemyPatrolNode("servant_grunt_4", "PathNodeArea_133", 0.0f, "0");

        	/*Exit grunts
        	 */
        	AddEnemyPatrolNode("servant_grunt_6", "PathNodeArea_115", 0.0f, "");

        	/*Entry grunt
        	 */
        	AddEnemyPatrolNode("servant_grunt_10", "PathNodeArea_3", 8.0f, "IdleExtra1");
        	AddEnemyPatrolNode("servant_grunt_10", "PathNodeArea_46", 0.0f, "");

        	/*Long hall
        	 */
        	AddEnemyPatrolNode("servant_grunt_11", "PathNodeArea_127", 4.0f, "IdleExtra3");
        	AddEnemyPatrolNode("servant_grunt_11", "PathNodeArea_137", 6.0f, "");

        	SetNumberOfQuestsInMap(1);


        	//----INSANITY----//
        	SetInsanitySetEnabled("Ch02", true);
        	SetInsanitySetEnabled("Ch01", false);

        	StartPlayerLookAt("AreaStartLook", 4.0f, 4.0f, "");
        	AddTimer("Stop", 0.5f, nameof(LookAtIntro));

        	AddTimer(nameof(TimerTunnelDust), RandInt(0,3), nameof(TimerTunnelDust));	//Dust falling random from ceiling in tunnel
        	AddTimer(nameof(TimerBangHead), RandFloat(3.0f,5.0f), nameof(TimerBangHead));	//Person banging head on wall


        	//----DEBUG----//
        	if(ScriptDebugOn())
        	{
        		//CPCall01("", 1); //Test player checkpoint 1, must be in PlayerStart_8 and crouching.

        		//SetGlobalVarInt("15CPEvent", 1);	//Test CP02 version1(same as in level south) if == 0 else test version2
        		//CPCall02("", 2);	//PlayerStart_9 for version 1, PlayerStart_10 for version 2

        		//CPCall03("",1);	//PlayerStartArea_12

        		GiveItemFromFile("lantern", "lantern.ent");
        		for(int i=0;i<10;i++) GiveItemFromFile("tinderbox_"+i, "tinderbox.ent");

        		GiveItem("stone_hammer_chipper", "Puzzle", "stone_hammer_chipper", "stone_hammer_chipper.tga", 0);
        		//GiveItemFromFile("glass_container_1", "glass_container.ent");
        		GiveItemFromFile("glass_container_filled", "glass_container_filled.ent");
        	}
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
        	PreloadSound("attack_claw"); PreloadSound("15_slam_door"); PreloadSound("scare_whine_loop1"); PreloadSound("react_scare");
        	PreloadSound("react_breath"); PreloadSound("slime_loop"); PreloadSound("guardian_activated"); PreloadSound("slime_create");
        	PreloadSound("general_rock_rumble_no3d"); PreloadSound("15_cloud_swarm"); PreloadSound("15_cloud"); PreloadSound("sanity_flick");
        	PreloadSound("step_walk_hard_water"); PreloadSound("guardian_distant3"); PreloadSound("scare_whine_loop3"); PreloadSound("03_rock_move");
        	PreloadSound("impact_metal_high"); PreloadSound("player_crouch"); PreloadSound("step_sneak_hard_water"); PreloadSound("step_run_hard_water");
        	PreloadSound("gameplay_acid_web"); PreloadSound("15_help"); PreloadSound("15_bang"); PreloadSound("15_bang_fall");

        	AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
        	//////////////////////
        	//Load Screen Setup
        	//SetupLoadScreen("LoadingText", "Ch02_Prison03", 1, "game_loading_well.jpg");
        }
    }
}
