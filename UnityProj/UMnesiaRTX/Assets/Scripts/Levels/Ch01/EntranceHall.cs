using System.Globalization;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Levels.Ch01
{
    /// <summary>
    /// Port of maps/main/ch01/02_entrance_hall.hps: the hub of chapter 1. Birds and dust whirls in the large room,
    /// the guardian slime events on return visits (after the study key, the four chemical jars, the acid container),
    /// the locked wine cellar door, the slime web and the acid, the flashback trail, the large gate and the screams
    /// pointing to the next area.
    /// Journal: docs/ScriptPort/ch01/02_entrance_hall.md.
    /// </summary>
    [HplMapScript("maps/main/ch01/02_entrance_hall.map")]
    public class EntranceHall : HplScript
    {
        /// <summary>AngelScript string + float (scriptstring addon: ostream, 6 significant digits).</summary>
        static string Str(float f) => f.ToString("G6", CultureInfo.InvariantCulture);

        ////////////////////
        //BEGIN LARGE ROOM//
        /*Bird wings when entering room first time
         */
        void CollideEnterLargeRoom(string asParent, string asChild, int alState)
        {
            PlaySoundAtEntity("BirdSound", "general_birds_flee", "BirdSound", 0, false);

            AddTimer("whirl", 7, nameof(TimerDustWhirl));

            /*DEBUG
             */
            AddDebugMessage("Entered big room, birds flee!", true);
        }
        /*Random whirls of dust
         */
        void TimerDustWhirl(string asTimer)
        {
            int iWhirl = (int)RandFloat(1, 4);
            float fWhirl = RandFloat(0.5f, 15.5f);

            CreateParticleSystemAtEntity("whirl" + iWhirl, "ps_dust_whirl.ps", "Whirl0" + iWhirl, false);

            PlaySoundAtEntity("whirl" + iWhirl, "general_wind_whirl.snt", "Whirl0" + iWhirl, 0, false);

            AddTimer("whirl", 14.5f + fWhirl, nameof(TimerDustWhirl));

            /*DEBUG
             */
            AddDebugMessage("Where Whirl Now: " + iWhirl + " Next Whirl In: " + Str(14.5f + fWhirl), true);
        }
        //END LARGE ROOM//
        //////////////////


        //////////////////////
        //BEGIN BLOCKED DOOR//
        /*Break wood planks to unlock door
         */
        void FuncUnlockDoor(string asEntity, string asType)
        {
            SetSwingDoorLocked("castle_arched01_1", false, true);

            /*DEBUG
             */
            AddDebugMessage("UnlockDoor!", true);
        }
        //END BLOCKED DOOR//
        ////////////////////


        /////////////////////////
        //BEGIN GUARDIAN EVENTS//
        string[] aSlime; //Holds the entities to SetPropActiveAndFade during the guardian events
        /*Guardian setup and effects specific for each event
         */
        void TimerGuardSlime(string asTimer)
        {
            if (asTimer == "fade1")
            {
                PlayerReactions(1.0f);
                SetPlayerMoveSpeedMul(0.8f);
                SetPlayerRunSpeedMul(0.5f);
                return;
            }
            else if (asTimer == "fade2")
            {
                FadeImageTrailTo(0, 2);
                SetPlayerMoveSpeedMul(1.0f);
                SetPlayerRunSpeedMul(1.0f);
                AddTimer("lantern", 1.0f, nameof(TimerGuardSlime));
                return;
            }
            else if (asTimer == "lantern")
            {
                return;
            }

            if (asTimer == "guard1")
            {
                GuardianEffects(0.02f, 4.5f, 1.0f, 0.8f, 1);

                FadeLightTo("BoxLight_2", 0.15f, 0.07f, 0.1f, 0.0f, -1, 5.0f);

                PlaySoundAtEntity("slimeloop1", "slime_loop", "slime_pile_3", 3, true);
                PlaySoundAtEntity("slimeloop2", "slime_loop", "slime_anim_ceiling_1", 4, true);
                PlaySoundAtEntity("slimeloop3", "slime_loop", "slime_pile_large_2", 6, true);
                PlaySoundAtEntity("amb_guard", "amb_guardian", "chandelier_simple_4", 5, true);

                SetEntityActive("SlimeDamageArea_1", true);
                AddTimer("SlimeDamageArea_2", 4, nameof(TimerActivateSlimeEntity));

                aSlime = new[] { "slime_pile_1","slime_pile_2","slime_pile_3","slime_pile_4","slime_pile_5",
                                 "slime_egg_1","slime_egg_2","slime_egg_3","slime_egg_4","slime_egg_5",
                                 "slime_pile_large_1","slime_pile_large_2","slime_pile_large_3","slime_pile_large_4","slime_pile_large_5",
                                 "slime_6way_1","slime_6way_2","slime_6way_3","slime_6way_4",
                                 "slime_3way_2","slime_3way_3","slime_3way_4","slime_3way_5",
                                 "slime_anim_wall_1","slime_anim_ceiling_1" };
            }
            else if (asTimer == "guard2")
            {
                GuardianEffects(0.035f, 3.0f, 1.0f, 1.1f, 2);

                FadeLightTo("BoxLight_2", 0.15f, 0.03f, 0.06f, 0.0f, -1, 5.0f);

                PlaySoundAtEntity("slimeloop4", "slime_loop", "slime_egg2_3", 3, true);
                PlaySoundAtEntity("slimeloop5", "slime_loop", "slime_pile_large2_4", 4, true);
                PlaySoundAtEntity("slimeloop6", "slime_loop", "slime_pile_large2_1", 6, true);

                SetEntityActive("SlimeDamageArea_3", true);
                AddTimer("SlimeDamageArea_5", 4, nameof(TimerActivateSlimeEntity));

                aSlime = new[] { "slime_pile_large2_1","slime_pile_large2_2","slime_pile_large2_3","slime_pile_large2_4","slime_pile_large2_5",
                                 "slime_pile2_1","slime_pile2_2","slime_pile2_3","slime_pile2_4","slime_pile2_5",
                                 "slime_egg2_1","slime_egg2_2","slime_egg2_3","slime_egg2_4","slime_egg2_5",
                                 "slime_3way2_1","slime_3way2_2","slime_3way2_3","slime_3way2_4","slime_3way2_5",
                                 "slime_anim_ceiling2_1","slime_anim_ceiling2_2","slime_anim_ceiling2_3","slime_anim_wall2_1","slime_anim_wall2_2",
                                 "slime_6way2_1","slime_6way2_2","slime_6way2_3","slime_6way2_4","slime_6way2_5" };
            }
            else if (asTimer == "guard3")
            {
                GuardianEffects(0.05f, 3.0f, 1.0f, 1.4f, 3);

                FadeLightTo("BoxLight_2", 0.15f, 0.03f, 0.06f, 0.0f, -1, 5.0f);

                PlaySoundAtEntity("slimeloop7", "slime_loop", "slime_pile3_2", 3, true);
                PlaySoundAtEntity("slimeloop8", "slime_loop", "slime_pile_large3_5", 4, true);
                PlaySoundAtEntity("slimeloop9", "slime_loop", "slime_pile_large3_4", 6, true);

                SetEntityActive("SlimeDamageArea_4", true);
                AddTimer("SlimeDamageArea_6", 4, nameof(TimerActivateSlimeEntity));

                aSlime = new[] { "slime_pile_large3_1","slime_pile_large3_2","slime_pile_large3_3","slime_pile_large3_4","slime_pile_large3_5",
                                 "slime_pile3_1","slime_pile3_2","slime_pile3_3","slime_pile3_4","slime_pile3_5",
                                 "slime_pile3_6","slime_pile3_7","slime_pile3_8","slime_pile3_9","slime_pile3_10",
                                 "slime_anim_wall3_1","slime_anim_wall3_2","slime_anim_ceiling3_1",
                                 "slime_6way3_1","slime_6way3_2","slime_6way3_3","slime_6way3_4","slime_6way3_5",
                                 "slime_egg3_1","slime_egg3_2","slime_egg3_3","slime_egg3_4","slime_egg3_5",
                                 "slime_3way3_1","slime_3way3_2","slime_3way3_3","slime_3way3_4" };
            }

            SetPlayerMoveSpeedMul(0.6f);
            SetPlayerRunSpeedMul(0.25f);

            SetLocalVarFloat("SlimeSound", 0.0f);

            // The original loops to iSlime <= length: the last index is out of bounds, the AngelScript exception
            // ends the function there (cScriptFuncAS ignores the Execute result). Nothing follows the loop.
            for (int iSlime = 0; iSlime < aSlime.Length; iSlime++)
            {
                int iRand = (int)RandFloat(5, 8);

                SetPropActiveAndFade(aSlime[iSlime], true, iRand);

                for (int i = 0; i <= iRand; i++)
                    AddTimer(aSlime[iSlime], RandFloat(1, iRand - 1), nameof(TimerSlimeSounds));
            }
        }

        void TimerActivateSlimeEntity(string asTimer)
        {
            SetEntityActive(asTimer, true);
        }


        /*General re-used guardian effects
         */
        void GuardianEffects(float fSS1, float fSS2, float fSS4, float fIT1, int iSound)
        {
            StartScreenShake(fSS1, fSS2, 1.0f, fSS4);
            FadeImageTrailTo(fIT1, 2.0f);

            for (int i = 1; i <= 3; i++)
            {
                AddDebugMessage("Fading light PointLightx" + iSound + "_" + i, false);
                FadeLightTo("PointLightx" + iSound + "_" + i, -1, -1, -1, -1, 3, 5);
            }

            for (int i = 1; i <= 3; i++)
            {
                CreateParticleSystemAtEntity("slime" + iSound + "ps" + i, "ps_slime_fog.ps", "AreaSlime" + iSound + "_" + i, true);
            }

            CreateParticleSystemAtEntity("fogfall" + iSound + "ps", "ps_slime_fog_falling.ps", "AreaFogFall" + iSound, true);

            PlaySoundAtEntity("guard" + iSound, "guardian_distant" + iSound, "Player", 0, false);

            AddTimer("fade1", 0.3f, nameof(TimerGuardSlime));
            AddTimer("fade2", 7.0f - (iSound / 2), nameof(TimerGuardSlime)); // integer division, as in AngelScript

            GiveSanityDamage(10.0f, false);

            /*DEBUG
             */
            AddDebugMessage("Guardian Effect with shake amount: " + Str(fSS1) + " With Sound: " + iSound, true);
        }
        /*Timer loop to play slime sounds during their SetPropActiveAndFade
         */
        void TimerSlimeSounds(string asTimer)
        {
            float fPlaySound = RandFloat(0.9f, 1.5f);

            if (fPlaySound < 1)
                PlaySoundAtEntity(asTimer, "slime_create.snt", asTimer, 0.0f + GetLocalVarFloat("SlimeSound"), false);

            AddLocalVarFloat("SlimeSound", 0.002f);
        }
        /*Player react to the guaridan effect
         */
        void PlayerReactions(float fTimer)
        {
            PlaySoundAtEntity("aah", "react_scare.snt", "Player", 0.3f, false);

            AddTimer("react1", fTimer * 0.5f, nameof(ReactionTimer));
            AddTimer("react2", fTimer * 2.5f, nameof(ReactionTimer));
            AddTimer("react3", fTimer * 4.5f, nameof(ReactionTimer));
            AddTimer("react4", fTimer * 7.0f, nameof(ReactionTimer));
        }
        void ReactionTimer(string asTimer)
        {
            if (asTimer == "react1") PlaySoundAtEntity("s" + asTimer, "react_breath.snt", "Player", 0.0f, false);
            else if (asTimer == "react2") PlaySoundAtEntity("s" + asTimer, "react_breath.snt", "Player", 0.1f, false);
            else if (asTimer == "react3") PlaySoundAtEntity("s" + asTimer, "react_breath.snt", "Player", 0.3f, false);
            else PlaySoundAtEntity("s" + asTimer, "react_breath.snt", "Player", 0.6f, false);
        }
        //END GUARDIAN EVENTS//
        ///////////////////////


        /////////////////////////////////
        //BEGIN LOCKED WINE CELLAR DOOR//
        void PlayerInteractDoor(string asEntity)
        {
            if (HasItem("key_study_1")) SetMessage("Ch01Level02", "InteractDoorHaveKey", 0);
            else AddQuest("02LockedDoor", "02LockedDoor");
        }
        void UseKeyOnDoor(string asItem, string asEntity)
        {
            PlaySoundAtEntity("unlocked", "unlock_door", asEntity, 0.0f, false);

            GiveSanityBoostSmall();

            SetLevelDoorLocked(asEntity, false);
            RemoveItem(asItem);

            CompleteQuest("02LockedDoor", "02LockedDoor");
        }
        //END LOCKED WINE CELLAR DOOR//
        ///////////////////////////////


        ////////////////////////
        //BEGIN BLOCKING SLIME//
        void CollideGiveQuestsWeb(string asParent, string asChild, int alState)
        {
            AddQuest("02Web", "02Web");
            SetEntityActive(asChild, false);
        }
        void UseAcidOnWeb(string asItem, string asEntity)
        {
            SetPropHealth(asEntity, 0);
            RemoveItem(asItem);
            GiveItemFromFile("empty_container", "chemical_container.ent");

            CompleteQuest("02Web", "02Web");

            GiveSanityBoost();

            AddTimer("music", 1, nameof(TimerMusicDelay));

            FadeLightTo("PointLightAcid", -1, -1, -1, -1, 3, 1.5f);
            AddTimer("PointLightAcid", 4, nameof(TimerFadeAcidLight));
        }
        void TimerMusicDelay(string asTimer)
        {
            PlayMusic("02_puzzle", false, 1, 0.1f, 10, false);
        }
        void TimerFadeAcidLight(string asTimer)
        {
            FadeLightTo(asTimer, 0, 0, 0, 0, -1, 3);
        }

        void UseEmptyContainerOnWeb(string asItem, string asEntity)
        {
            SetMessage("Ch01Level02", "UseContainerOnSlime", 0);
        }

        void UseChemicalOnWeb(string asItem, string asEntity)
        {
            SetMessage("Ch01Level02", "UseChemicalOnSlime", 0);
        }

        //END BLOCKING SLIME//
        //////////////////////


        ///////////////////////////
        //BEGIN FLASHBACK & TRAIL//
        /*Begin timer to show trail
         */
        void CollideAreaFlashBack(string asParent, string asChild, int alState)
        {
            AddTimer("trail", 2.0f, nameof(TimerCreateTrail));
            StartPlayerLookAt("AreaTrail_1", 3, 3, "");
            CreateParticleSystemAtEntity("rosesonmybed", "ps_rose_petals_wind.ps", "AreaTrail_1", false);
            PlayGuiSound("general_wind_whirl6.ogg", 0.3f);
        }
        void TimerCreateTrail(string asTimer)
        {
            AddLocalVarInt("Trail", 1);

            int iTrail = GetLocalVarInt("Trail");

            switch (GetLocalVarInt("Trail"))
            {
                case 1:
                    StopPlayerLookAt();
                    MakeTrailEffect();
                    break;
                case 2:
                    StartPlayerLookAt("AreaTrailDoor", 1, 1, "");
                    MakeTrailEffect();
                    break;
                case 3:
                    StopPlayerLookAt();
                    MakeTrailEffect();
                    break;
                default:
                    MakeTrailEffect();
                    break;
            }

            if (iTrail < 8) AddTimer("trail", 1.5f, nameof(TimerCreateTrail));
        }
        bool bSwing = true;
        void MakeTrailEffect()
        {
            if (GetLocalVarInt("Trail") == 8)
            {
                FadeLightTo("PointLight_2", 0.0f, 0.0f, 0.0f, 0.0f, -1, 2.0f);
                return;
            }

            bSwing = bSwing == false ? true : false;
            if (bSwing) FadeLightTo("PointLight_2", 0.6f, 0.3f, 0.6f, 0.5f, -1, 1.4f);
            else FadeLightTo("PointLight_2", 0.3f, 0.1f, 0.3f, 0.25f, -1, 1.4f);

            AddDebugMessage("hello" + GetLocalVarInt("Trail"), false);
        }
        //END FLASHBACK & TRAIL//
        /////////////////////////

        void InteractSlime(string asEntity)
        {
            PlayGuiSound("slime_create3.ogg", 0.4f);
            SetMessage("Ch01Level02", "InteractSlime", 0);

            AddTimer("backoncellar", 1.0f, nameof(TimerCellarSlime));
        }
        void TimerCellarSlime(string asTimer)
        {
            SetEntityPlayerInteractCallback("web_1", nameof(InteractSlime), true);
        }
        void InteractLockedDoor(string asEntity)
        {
            AddTimer("lockeddoor", 0.5f, nameof(TimerLockedDoor4evah));

            PlaySoundAtEntity("locked_" + asEntity, "locked_door", asEntity, 0.0f, false);
        }
        void TimerLockedDoor4evah(string asTimer)
        {
            SetMessage("LevelDoors", "LockedForever", 0);
        }

        void TimerRunHint(string asTimer)
        {
            GiveHint("run", "Hints", "RunHint", 0);
        }


        /////////////////////////////
        //BEGIN INTERACT LARGE GATE//
        void InteractLargeGate(string asEntity)
        {
            PlayGuiSound("close_gate.ogg", 0.5f);
            PlaySoundAtEntity("guardboo", "guardian_distant1", "LargeGate", 2.0f, false);
            FadeLightTo("PointLight_15", 0.2f, 0.0f, 0.05f, 1.0f, -1, 4.0f);

            AddTimer("1", 0.5f, nameof(TimerLargeGate));
            AddTimer("2", 1.2f, nameof(TimerLargeGate));
            AddTimer("3", 1.7f, nameof(TimerLargeGate));
            AddTimer("4", 2.0f, nameof(TimerLargeGate));
        }
        void TimerLargeGate(string asTimer)
        {
            if (asTimer == "1")
            {
                PlayMusic("01_event_critters", false, 0.7f, 1.0f, 10, false);
                StartScreenShake(0.005f, 2.0f, 1.0f, 1.0f);
                SetPropActiveAndFade("slime_static_slime_6way_1", true, 2.5f);
                SetPropActiveAndFade("slime_static_slime_6way_2", true, 1.5f);
                PlaySoundAtEntity("slimecreate", "slime_loop", "LargeGate", 4.0f, true);
                PlaySoundAtEntity("slimecreate1", "slime_create", "LargeGate", 0.0f, true);
                GiveSanityDamage(10.0f, true);
            }
            else if (asTimer == "2")
            {
                PlaySoundAtEntity("slimecreate2", "slime_create", "LargeGate", 0.1f, true);
            }
            else if (asTimer == "3")
            {
                PlaySoundAtEntity("slimecreate3", "slime_create", "LargeGate", 0.15f, true);
            }
            else if (asTimer == "4")
            {
                StopMusic(4.0f, 10);
                StopSound("guardboo", 2.0f);
                PlaySoundAtEntity("slimecreate4", "slime_create", "LargeGate", 0.2f, true);
                SetEntityPlayerInteractCallback("LargeGate", nameof(InteractLargeGate02), true);
                PlayMusic("02_amb_strange", true, 1.0f, 2.0f, 0, true);
            }
        }
        void InteractLargeGate02(string asEntity)
        {
            PlayGuiSound("slime_create3.ogg", 0.5f);

            AddTimer("intslimebackon", 1.0f, nameof(TimerSlimeGateOn));
        }
        void TimerSlimeGateOn(string asTimer)
        {
            SetEntityPlayerInteractCallback("LargeGate", nameof(InteractLargeGate02), true);
        }
        //END INTERACT LARGE GATE//
        ///////////////////////////


        /*Scream from 03 if player has been in Lab first, Scream from 04 if player goes towards 03 first
         */
        void CollideDirectionHint(string asParent, string asChild, int alState)
        {
            if (asChild == "LabHint")
            {
                PlaySoundAtEntity("scream", "03_no.snt", "ArchiveHint", 0.0f, false);

                AddTimer("ArchiveHint", 0.2f, nameof(TimerScream));
                AddTimer("scream2", 0.8f, nameof(TimerScream));

                RemoveEntityCollideCallback("Player", "ArchiveHint");
                RemoveEntityCollideCallback("Player", "LabHint");
            }
            else
            {
                if (GetGlobalVarInt("BeenToLab") == 0) return;

                PlaySoundAtEntity("scream", "03_no.snt", "LabHint", 0.0f, false);

                AddTimer("LabHint", 0.2f, nameof(TimerScream));
                AddTimer("scream2", 0.8f, nameof(TimerScream));

                RemoveEntityCollideCallback("Player", "LabHint");
                RemoveEntityCollideCallback("Player", "ArchiveHint");
            }
        }
        void TimerScream(string asTimer)
        {
            if (asTimer == "scream2")
            {
                StopPlayerLookAt();
            }
            else if (asTimer == "stopmusic")
            {
                StopMusic(3.0f, 10);
            }
            else
            {
                PlayGuiSound("react_scare", 0.7f);
                StartPlayerLookAt(asTimer, 4.0f, 4.0f, "");
                PlayMusic("11_event_tree", false, 1.0f, 0.5f, 10, false);
                AddTimer("stopmusic", 3.0f, nameof(TimerScream));
            }
        }

        ////////////////////////////
        // Run first time starting map
        public override void OnStart()
        {
            ////////////////////
            // Hub start sanity boost
            GiveSanityBoost();


            //----COLLIDE CALLBACKS----//
            AddEntityCollideCallback("Player", "ScareBirds", nameof(CollideEnterLargeRoom), true, 1);
            AddEntityCollideCallback("Player", "AreaGiveQuestWeb", nameof(CollideGiveQuestsWeb), false, 1);
            AddEntityCollideCallback("Player", "AreaCollideFlash", nameof(CollideAreaFlashBack), true, 1);
            AddEntityCollideCallback("Player", "LabHint", nameof(CollideDirectionHint), false, 1);
            AddEntityCollideCallback("Player", "ArchiveHint", nameof(CollideDirectionHint), false, 1);

            //---- INTERACT INIT ----//
            AddUseItemCallback("UseKeyOnDoor", "key_study_1", "level_wood_2", nameof(UseKeyOnDoor), true);
            AddUseItemCallback("UseAcidOnWeb", "chemical_container_2", "web_1", nameof(UseAcidOnWeb), true);
            AddUseItemCallback("UseEmptyContainerOnWeb", "chemical_container_1", "web_1", nameof(UseEmptyContainerOnWeb), false);
            AddUseItemCallback("UseChemicalOnWeb", "Chemical_1", "web_1", nameof(UseChemicalOnWeb), false);
            AddUseItemCallback("UseChemicalOnWeb", "Chemical_2", "web_1", nameof(UseChemicalOnWeb), false);
            AddUseItemCallback("UseChemicalOnWeb", "Chemical_3", "web_1", nameof(UseChemicalOnWeb), false);
            AddUseItemCallback("UseChemicalOnWeb", "Chemical_4", "web_1", nameof(UseChemicalOnWeb), false);

            SetEntityPlayerInteractCallback("castle_1", nameof(PlayerInteractDoor), true);
            SetEntityCustomFocusCrossHair("LargeGate", "LevelDoor");

            //---- ENITTY INIT----//
            SetNumberOfQuestsInMap(2);

            SetLocalVarInt("ScaryMusic", 0);

            UnBlockHint("QuestAdded");
            UnBlockHint("EntityGrab02"); UnBlockHint("EntityPush");

            AddTimer("runhint", 3, nameof(TimerRunHint));

            //----DEBUG----//
            if (ScriptDebugOn())
            {
                GiveItemFromFile("lantern", "lantern.ent");
                GiveItemFromFile("chemical_container_1", "chemical_container.ent");
                GiveItemFromFile("chemical_container_2", "chemical_container.ent");
                GiveItemFromFile("Chemical_1", "flask01_aqua_regia.ent");
                GiveItemFromFile("Chemical_2", "flask01_aqua_regia.ent");
                GiveItemFromFile("Chemical_3", "flask01_aqua_regia.ent");
                GiveItemFromFile("Chemical_4", "flask01_aqua_regia.ent");

                GiveItemFromFile("key_study_1", "key_study.ent");
            }

            ClearSavedMaps();
        }


        ////////////////////////////
        // Run when entering map
        public override void OnEnter()
        {
            SetMapDisplayNameEntry("EntranceHall");

            //----PRELOADING----//
            PreloadSound("react_scare"); PreloadSound("general_birds_flee"); PreloadSound("general_wind_whirl"); PreloadSound("slime_loop");
            PreloadSound("amb_guardian"); PreloadSound("guardian_distant1"); PreloadSound("guardian_distant2"); PreloadSound("guardian_distant3");
            PreloadSound("slime_create");

            PreloadParticleSystem("ps_dust_whirl"); PreloadParticleSystem("ps_slime_fog");
            PreloadParticleSystem("ps_slime_fog_falling"); PreloadParticleSystem("ps_trail_flow"); PreloadParticleSystem("ps_trail_large");


            //----AUDIO----//
            SetFogProperties(10, 50, 1, false);
            SetFogColor(0.12f, 0.14f, 0.18f, 1);
            SetFogActive(true);

            //----GUARDIAN INIT----//
            float fGuardianDelay = 0.5f;
            if (GetGlobalVarString("key_study_1") == "key_study_1")
            {
                AddTimer("guard1", RandFloat(fGuardianDelay + 0.25f, fGuardianDelay + 0.5f), nameof(TimerGuardSlime));
                SetLocalVarInt("ScaryMusic", 1);
                SetGlobalVarString("key_study_1", "");
            }
            else if (GetGlobalVarInt("ChemJars") == 4)
            {
                AddTimer("guard2", RandFloat(fGuardianDelay + 0.5f, fGuardianDelay + 0.75f), nameof(TimerGuardSlime));
                SetLocalVarInt("ScaryMusic", 1);
                SetGlobalVarInt("ChemJars", 0);
            }
            else if (GetGlobalVarString("chemical_container_2") == "chemical_container_2")
            {
                AddTimer("guard3", RandFloat(fGuardianDelay + 0.75f, fGuardianDelay + 1.0f), nameof(TimerGuardSlime));
                SetLocalVarInt("ScaryMusic", 1);
                SetGlobalVarString("chemical_container_2", "");
            }

            if (GetLocalVarInt("ScaryMusic") == 0) PlayMusic("02_amb_safe", true, 0.5f, 5, 0, true);
            else PlayMusic("02_amb_strange", true, 0.5f, 5, 0, true);

            //----PLAYER INIT----//
            SetPlayerRunSpeedMul(1); //Able to run now!

            AutoSave();
        }

        ////////////////////////////
        // Run when leaving map
        public override void OnLeave()
        {
            StopMusic(5, 0);
        }
    }
}
