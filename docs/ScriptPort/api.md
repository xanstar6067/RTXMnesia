# API скриптов уровней

Функции, которые движок Amnesia предоставляет скриптам `.hps` (регистрируются в `src/.../amnesia/src/game/LuxScriptHandler.cpp`, всего 261).
Сгенерировано по копии игры в `source/`; «гл.1» и «всего» — число вызовов в скриптах первой главы и всей игры.
Наш порт: `UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/` (`HplScript*.cs`). Статус: ✓ готово; ~ частично (состояние хранится, но части поведения ещё нет: завершение прохождения, затухание моделей, враги); — пропускается с одним предупреждением в логе (часть движка не портирована).

## Нужны для главы 1 (по частоте)

Подготовка Justine (2026-10-17): `SetPlayerPermaDeathSound` — ✓, хранит выбранную .snt у игрока и переносит между картами. В режиме Justine смерть проигрывает её и завершает приложение без загрузки checkpoint; основной режим смерти сохранён. `StartCredits` и `AddKeyPart` остаются NotPorted. Проверки и границы: `justine/README.md`.

| Функция | гл.1 | всего | Статус | Сигнатура |
|---|---:|---:|:---:|---|
| `AddTimer` | 535 | 1240 | ✓ | `void AddTimer(string &in asName, float afTime, string &in asFunction)` |
| `PlaySoundAtEntity` | 501 | 1069 | ✓ | `void PlaySoundAtEntity(string &in asSoundName, string &in asSoundFile, string &in asEntity, float afFadeSpeed, bool abSaveSound)` |
| `PreloadSound` | 247 | 628 | ✓ | `void PreloadSound(string& asSoundFile)` |
| `GetLocalVarInt` | 197 | 525 | ✓ | `int GetLocalVarInt(string &in asName)` |
| `SetEntityActive` | 180 | 548 | ✓ | `void SetEntityActive(string &in asName, bool abActive)` |
| `AddEntityCollideCallback` | 165 | 412 | ✓ | `void AddEntityCollideCallback(string &in asParentName, string &in asChildName, string &in asFunction, bool abDeleteOnCollide, int alStates)` |
| `RandFloat` | 144 | 275 | ✓ | `float RandFloat(float afMin, float afMax)` |
| `AddDebugMessage` | 138 | 310 | ✓ | `void AddDebugMessage(string &in asString, bool abCheckForDuplicates)` |
| `CreateParticleSystemAtEntity` | 119 | 243 | ✓ | `void CreateParticleSystemAtEntity(string &in asPSName, string &in asPSFile, string &in asEntity, bool abSavePS)` |
| `SetPlayerMoveSpeedMul` | 118 | 232 | ✓ | `void SetPlayerMoveSpeedMul(float afMul)` |
| `SetLocalVarInt` | 115 | 352 | ✓ | `void SetLocalVarInt(string &in asName, int alVal)` |
| `StopSound` | 90 | 281 | ✓ | `void StopSound(string &in asSoundName, float afFadeTime)` |
| `FadeLightTo` | 89 | 267 | ✓ | `void FadeLightTo(string &in asLightName, float afR, float afG, float afB, float afA, float afRadius, float afTime)` |
| `StartScreenShake` | 78 | 185 | ✓ | `void StartScreenShake(float afAmount, float afTime, float afFadeInTime,float afFadeOutTime)` |
| `PlayMusic` | 75 | 178 | ✓ | `void PlayMusic(string &in asMusicFile, bool abLoop, float afVolume, float afFadeTime, int alPrio, bool abResume)` |
| `PlayGuiSound` | 66 | 295 | ✓ | `void PlayGuiSound(string &in asSoundFile, float afVolume)` |
| `AddLocalVarInt` | 63 | 133 | ✓ | `void AddLocalVarInt(string &in asName, int alVal)` |
| `SetPlayerRunSpeedMul` | 62 | 149 | ✓ | `void SetPlayerRunSpeedMul(float afMul)` |
| `FadeImageTrailTo` | 58 | 91 | ✓ | `void FadeImageTrailTo(float afAmount, float afSpeed)` |
| `FadePlayerFOVMulTo` | 55 | 80 | ✓ | `void FadePlayerFOVMulTo(float afX, float afSpeed)` |
| `PreloadParticleSystem` | 54 | 65 | ✓ | `void PreloadParticleSystem(string& asPSFile)` |
| `StartPlayerLookAt` | 53 | 118 | ✓ | `void StartPlayerLookAt(string &in asEntityName, float afSpeedMul, float afMaxSpeed,string &in asAtTargetCallback)` |
| `SetPlayerLookSpeedMul` | 52 | 105 | ✓ | `void SetPlayerLookSpeedMul(float afMul)` |
| `FadePlayerRollTo` | 51 | 90 | ✓ | `void FadePlayerRollTo(float afX, float afSpeedMul, float afMaxSpeed)` |
| `SetMessage` | 50 | 216 | ✓ | `void SetMessage(string &in asTextCategory, string &in asTextEntry, float afTime)` |
| `SetEntityPlayerInteractCallback` | 48 | 148 | ✓ | `void SetEntityPlayerInteractCallback(string &in asName, string &in asCallback, bool abRemoveOnInteraction)` |
| `MovePlayerHeadPos` | 45 | 75 | ✓ | `void MovePlayerHeadPos(float afX, float afY, float afZ, float afSpeed, float afSlowDownDist)` |
| `AddEnemyPatrolNode` | 44 | 175 | ✓ | `void AddEnemyPatrolNode(string &in asEnemyName, string &in asNodeName, float afWaitTime, string &in asAnimation)` |
| `GiveSanityDamage` | 43 | 100 | ✓ | `void GiveSanityDamage(float afAmount, bool abUseEffect)` |
| `GiveItemFromFile` | 43 | 109 | ✓ | `void GiveItemFromFile(string& asName, string& asFileName)` |
| `RemoveTimer` | 41 | 112 | ✓ | `void RemoveTimer(string &in asName)` |
| `GetGlobalVarInt` | 40 | 87 | ✓ | `int GetGlobalVarInt(string &in asName)` |
| `GetLocalVarString` | 38 | 44 | ✓ | `string& GetLocalVarString(string &in asName)` |
| `AddUseItemCallback` | 35 | 230 | ✓ | `void AddUseItemCallback(string &in asName, string &in asItem, string &in asEntity, string &in asFunction, bool abAutoDestroy)` |
| `FadeRadialBlurTo` | 29 | 116 | ✓ | `void FadeRadialBlurTo(float afSize, float afSpeed)` |
| `AddPropImpulse` | 27 | 56 | ✓ | `void AddPropImpulse(string &in asName, float afX, float afY, float afZ, string &in asCoordSystem)` |
| `AddPropForce` | 27 | 28 | ✓ | `void AddPropForce(string &in asName, float afX, float afY, float afZ, string &in asCoordSystem)` |
| `StopPlayerLookAt` | 26 | 70 | ✓ | `void StopPlayerLookAt()` |
| `FadeOut` | 25 | 61 | ✓ | `void FadeOut(float afTime)` |
| `FadeIn` | 25 | 47 | ✓ | `void FadeIn(float afTime)` |
| `SetSwingDoorLocked` | 24 | 40 | ✓ | `void SetSwingDoorLocked(string &in asName, bool abLocked, bool abEffects)` |
| `SetLampLit` | 24 | 95 | ✓ | `void SetLampLit(string &in asName, bool abLit, bool abEffects)` |
| `AddQuest` | 24 | 67 | ~ | `void AddQuest(string &in asName, string &in asNameAndTextEntry)` |
| `SetSwingDoorClosed` | 23 | 36 | ✓ | `void SetSwingDoorClosed(string &in asName, bool abClosed, bool abEffects)` |
| `SetGlobalVarInt` | 22 | 56 | ✓ | `void SetGlobalVarInt(string &in asName, int alVal)` |
| `CompleteQuest` | 22 | 65 | ~ | `void CompleteQuest(string &in asName, string &in asNameAndTextEntry)` |
| `SetPropActiveAndFade` | 21 | 87 | ~ | `void SetPropActiveAndFade(string &in asName, bool abActive, float afFadeTime)` |
| `FadeSepiaColorTo` | 21 | 27 | ✓ | `void FadeSepiaColorTo(float afAmount, float afSpeed)` |
| `DestroyParticleSystem` | 21 | 59 | ✓ | `void DestroyParticleSystem(string &in asName)` |
| `StopMusic` | 20 | 49 | ✓ | `void StopMusic(float afFadeTime, int alPrio)` |
| `GiveSanityBoostSmall` | 20 | 39 | ✓ | `void GiveSanityBoostSmall()` |
| `SetSwingDoorDisableAutoClose` | 18 | 28 | ✓ | `void SetSwingDoorDisableAutoClose(string &in asName, bool abDisableAutoClose)` |
| `SetPlayerActive` | 17 | 57 | ✓ | `void SetPlayerActive(bool abActive)` |
| `ResetProp` | 17 | 41 | ✓ | `void ResetProp(string &in asName)` |
| `FadeGlobalSoundVolume` | 16 | 34 | ✓ | `void FadeGlobalSoundVolume(float afDestVolume, float afTime)` |
| `UnBlockHint` | 15 | 15 | ✓ | `void UnBlockHint(string &in asName)` |
| `SetWheelStuckState` | 15 | 22 | ✓ | `void SetWheelStuckState(string &in asName, int alState, bool abEffects)` |
| `SetMapDisplayNameEntry` | 15 | 33 | ✓ | `void SetMapDisplayNameEntry(string &in asNameEntry)` |
| `SetLanternDisabled` | 15 | 22 | ✓ | `void SetLanternDisabled(bool abX)` |
| `ScriptDebugOn` | 15 | 36 | ✓ | `bool ScriptDebugOn()` |
| `SetRadialBlurStartDist` | 14 | 59 | ✓ | `void SetRadialBlurStartDist(float afStartDist)` |
| `SetLocalVarString` | 14 | 19 | ✓ | `void SetLocalVarString(string &in asName, string &in asVal)` |
| `AutoSave` | 14 | 31 | ✓ | `void AutoSave()` |
| `SetEntityInteractionDisabled` | 13 | 19 | ✓ | `void SetEntityInteractionDisabled(string& asName, bool abDisabled)` |
| `BlockHint` | 13 | 13 | ✓ | `void BlockHint(string &in asName)` |
| `AddEffectVoice` | 13 | 36 | ✓ | `void AddEffectVoice(string &in asVoiceFile, string &in asEffectFile, string &in asTextCat, string &in asTextEntry, bool abUsePostion,  string &in asPosEnitity, float afMinDistance, float afMaxDistance)` |
| `AddBodyImpulse` | 13 | 23 | ✓ | `void AddBodyImpulse(string &in asName, float afX, float afY, float afZ, string &in asCoordSystem)` |
| `SetLeverStuckState` | 12 | 39 | ✓ | `void SetLeverStuckState(string &in asName, int alState, bool abEffects)` |
| `RemoveItem` | 12 | 55 | ✓ | `void RemoveItem(string &in asName)` |
| `GiveHint` | 12 | 12 | ✓ | `void GiveHint(string &in asName, string &in asMessageCat, string &in asMessageEntry, float afTimeShown)` |
| `GetLocalVarFloat` | 12 | 33 | ✓ | `float GetLocalVarFloat(string &in asName)` |
| `CheckPoint` | 12 | 48 | ✓ | `void CheckPoint(string &in asName,string &in asStartPos ,string &in asCallback, string &in asDeathHintCat, string &in asDeathHintEntry)` |
| `TeleportPlayer` | 11 | 19 | ✓ | `void TeleportPlayer(string &in asStartPosName)` |
| `SetPlayerCrouchDisabled` | 11 | 25 | ✓ | `void SetPlayerCrouchDisabled(bool abX)` |
| `RotatePropToSpeed` | 11 | 34 | ✓ | `void RotatePropToSpeed(string &in asName, float afAcc, float afGoalSpeed, float afAxisX, float afAxisY, float afAxisZ, bool abResetSpeed, string &in asOffsetArea)` |
| `GetSwingDoorClosed` | 11 | 11 | ✓ | `bool GetSwingDoorClosed(string &in asName)` |
| `ClearEnemyPatrolNodes` | 11 | 24 | ✓ | `void ClearEnemyPatrolNodes(string &in asEnemyName)` |
| `SetupLoadScreen` | 10 | 22 | ✓ | `void SetupLoadScreen(string &in asTextCat, string &in asTextEntry, int alRandomNum, string &in asImageFile)` |
| `FadePlayerAspectMulTo` | 10 | 17 | ✓ | `void FadePlayerAspectMulTo(float afX, float afSpeed)` |
| `SetPlayerJumpDisabled` | 9 | 26 | ✓ | `void SetPlayerJumpDisabled(bool abX)` |
| `SetNumberOfQuestsInMap` | 9 | 13 | ✓ | `void SetNumberOfQuestsInMap(int alNumberOfQuests)` |
| `StartEffectFlash` | 8 | 18 | ✓ | `void StartEffectFlash(float afFadeIn, float afWhite, float afFadeOut)` |
| `SetPropHealth` | 8 | 23 | ✓ | `void SetPropHealth(string &in asName, float afHealth)` |
| `SetLanternActive` | 8 | 10 | ✓ | `void SetLanternActive(bool abX, bool abUseEffects)` |
| `RemoveEntityCollideCallback` | 8 | 25 | ✓ | `void RemoveEntityCollideCallback(string &in asParentName, string &in asChildName)` |
| `AddLocalVarFloat` | 8 | 12 | ✓ | `void AddLocalVarFloat(string &in asName, float afVal)` |
| `StringContains` | 7 | 13 | ✓ | `bool StringContains(string &in asString, string &in asSubString)` |
| `SetFogActive` | 7 | 8 | ✓ | `void SetFogActive(bool abActive)` |
| `SetEntityPlayerLookAtCallback` | 7 | 12 | ✓ | `void SetEntityPlayerLookAtCallback(string &in asName, string &in asCallback, bool abRemoveWhenLookedAt)` |
| `RandInt` | 7 | 57 | ✓ | `int RandInt(int alMin, int alMax)` |
| `AddBodyForce` | 7 | 8 | ✓ | `void AddBodyForce(string &in asName, float afX, float afY, float afZ, string &in asCoordSystem)` |
| `SetPropObjectStuckState` | 6 | 6 | ✓ | `void SetPropObjectStuckState(string &in asName, int alState)` |
| `SetLightVisible` | 6 | 15 | ✓ | `void SetLightVisible(string &in asLightName, bool abVisible)` |
| `SetGlobalVarString` | 6 | 6 | ✓ | `void SetGlobalVarString(string &in asName, string &in asVal)` |
| `SetEffectVoiceOverCallback` | 6 | 18 | ✓ | `void SetEffectVoiceOverCallback(string &in asFunc)` |
| `GiveSanityBoost` | 6 | 15 | ✓ | `void GiveSanityBoost()` |
| `ConnectEntities` | 6 | 9 | ~ | `void ConnectEntities(string &in asName, string &in asMainEntity, string &in asConnectEntity, bool abInvertStateSent, int alStatesUsed, string &in asCallbackFunc)` |
| `SetMoveObjectState` | 5 | 42 | ✓ | `void SetMoveObjectState(string &in asName, float afState)` |
| `SetLocalVarFloat` | 5 | 29 | ✓ | `void SetLocalVarFloat(string &in asName, float afVal)` |
| `SetFogProperties` | 5 | 6 | ~ | `void SetFogProperties(float afStart, float afEnd, float afFalloffExp, bool abCulling)` |
| `SetFogColor` | 5 | 6 | ✓ | `void SetFogColor(float afR, float afG, float afB, float afA)` |
| `HasItem` | 5 | 24 | ✓ | `bool HasItem(string &in asName)` |
| `GivePlayerDamage` | 5 | 32 | ✓ | `void GivePlayerDamage(float afAmount, string &in asType, bool abSpinHead, bool abLethal)` |
| `AddGlobalVarInt` | 5 | 13 | ✓ | `void AddGlobalVarInt(string &in asName, int alVal)` |
| `SetPlayerLampOil` | 4 | 4 | ✓ | `void SetPlayerLampOil(float afOil)` |
| `SetMoveObjectStateExt` | 4 | 5 | ✓ | `void SetMoveObjectStateExt(string &in asName, float afState, float afAcc, float afMaxSpeed, float afSlowdownDist, bool abResetSpeed)` |
| `SetLevelDoorLocked` | 4 | 6 | ✓ | `void SetLevelDoorLocked(string &in asName, bool abLocked)` |
| `SetInventoryDisabled` | 4 | 11 | ✓ | `void SetInventoryDisabled(bool abX)` |
| `GetTimerTimeLeft` | 4 | 10 | ✓ | `float GetTimerTimeLeft(string &in asName)` |
| `FadeGlobalSoundSpeed` | 4 | 10 | ✓ | `void FadeGlobalSoundSpeed(float afDestSpeed, float afTime)` |
| `ClearSavedMaps` | 4 | 10 | ✓ | `void ClearSavedMaps()` |
| `StringSub` | 3 | 12 | ✓ | `string& StringSub(string &in asString, int alStart, int alCount)` |
| `SetSkyBoxActive` | 3 | 3 | — | `void SetSkyBoxActive(bool abActive)` |
| `SetPropEffectActive` | 3 | 6 | ✓ | `void SetPropEffectActive(string &in asName, bool abActive, bool abFadeAndPlaySounds)` |
| `SetPlayerHealth` | 3 | 7 | ✓ | `void SetPlayerHealth(float afHealth)` |
| `GetLanternActive` | 3 | 4 | ✓ | `bool GetLanternActive()` |
| `GetGlobalVarString` | 3 | 3 | ✓ | `string& GetGlobalVarString(string &in asName)` |
| `ShowPlayerCrossHairIcons` | 2 | 4 | ~ | `void ShowPlayerCrossHairIcons(bool abX)` |
| `SetSanityDrainDisabled` | 2 | 7 | ✓ | `void SetSanityDrainDisabled(bool abX)` |
| `SetPropStaticPhysics` | 2 | 12 | ✓ | `void SetPropStaticPhysics(string &in asName, bool abX)` |
| `SetPlayerCrouching` | 2 | 14 | ✓ | `void SetPlayerCrouching(bool abCrouch)` |
| `SetInsanitySetEnabled` | 2 | 6 | ✓ | `void SetInsanitySetEnabled(string &in asSet, bool abX)` |
| `SetEntityCustomFocusCrossHair` | 2 | 5 | ✓ | `void SetEntityCustomFocusCrossHair(string &in asName, string &in asCrossHair)` |
| `SetEntityCallbackFunc` | 2 | 2 | ~ | `void SetEntityCallbackFunc(string &in asName, string &in asCallback)` |
| `SetEnemyDisabled` | 2 | 3 | ✓ | `void SetEnemyDisabled(string &in asName, bool abDisabled)` |
| `SetDeathHint` | 2 | 2 | ✓ | `void SetDeathHint(string &in asTextCategory, string &in asTextEntry)` |
| `InteractConnectPropWithRope` | 2 | 9 | — | `void InteractConnectPropWithRope(string &in asName, string& asLeverName, string& asPropName, bool abInteractOnly, float afSpeedMul,float afMinSpeed, float afMaxSpeed, bool abInvert, int alStatesUsed)` |
| `GetEntitiesCollide` | 2 | 2 | ✓ | `bool GetEntitiesCollide(string &in asEntityA, string &in asEntityB)` |
| `CreateEntityAtArea` | 2 | 5 | ✓ | `void CreateEntityAtArea(string &in asEntityName, string &in asEntityFile, string &in asAreaName, bool abFullGameSave)` |
| `ChangeMap` | 2 | 7 | ✓ | `void ChangeMap(string &in asMapName, string &in asStartPos, string &in asStartSound, string &in asEndSound)` |
| `AddPlayerHealth` | 2 | 8 | ✓ | `void AddPlayerHealth(float afHealth)` |
| `StartEffectEmotionFlash` | 1 | 8 | ~ | `void StartEffectEmotionFlash(string &in asTextCat, string &in asTextEntry, string &in asSound)` |
| `SetWheelAngle` | 1 | 1 | ✓ | `void SetWheelAngle(string &in asName, float afAngle, bool abAutoMove)` |
| `SetSkyBoxColor` | 1 | 1 | — | `void SetSkyBoxColor(float afR, float afG, float afB, float afA)` |
| `SetPlayerSanity` | 1 | 9 | ✓ | `void SetPlayerSanity(float afSanity)` |
| `ReturnOpenJournal` | 1 | 3 | ✓ | `void ReturnOpenJournal(bool abOpenJournal)` |
| `RemoveHint` | 1 | 1 | ✓ | `void RemoveHint(string &in asName)` |
| `QuestIsCompleted` | 1 | 2 | ✓ | `bool QuestIsCompleted(string &in asName)` |
| `InteractConnectPropWithMoveObject` | 1 | 4 | — | `void InteractConnectPropWithMoveObject(string &in asName, string &in asPropName, string &in asMoveObjectName, bool abInteractOnly,bool abInvert, int alStatesUsed)` |
| `GiveItem` | 1 | 17 | ✓ | `void GiveItem(string &in asName, string &in asType, string &in asSubTypeName, string &in asImageName, float afAmount)` |
| `GetPlayerLampOil` | 1 | 1 | ✓ | `float GetPlayerLampOil()` |
| `GetLeverState` | 1 | 5 | ✓ | `int GetLeverState(string &in asName)` |
| `GetEntityExists` | 1 | 3 | ✓ | `bool GetEntityExists(string &in asName)` |
| `DisableDeathStartSound` | 1 | 7 | ✓ | `void DisableDeathStartSound()` |
| `CreateRope` | 1 | 1 | — | `void CreateRope(string &in asName,string &in asStartArea, string &in asEndArea, string &in asStartBody, string &in asEndBody,float afMinTotalLength, float afMaxTotalLength,float afSegmentLength, float afDamping,float afStrength, float afStiffness, string &in asMaterial, float afRadius, float afLengthTileAmount, float afLengthTileSize, string &in asSound,float afSoundStartSpeed, float afSoundStopSpeed,bool abAutoMove, float afAutoMoveAcc, float afAutoMoveMaxSpeed)` |
| `AddPropHealth` | 1 | 1 | ✓ | `void AddPropHealth(string &in asName, float afHealth)` |
| `AddPlayerLampOil` | 1 | 1 | ✓ | `void AddPlayerLampOil(float afOil)` |
| `AddNote` | 1 | 1 | ✓ | `void AddNote(string &in asNameAndTextEntry, string &in asImage)` |

## Используются только в других главах

| Функция | всего | Статус | Сигнатура |
|---|---:|:---:|---|
| `CreateParticleSystemAtEntityExt` | 13 | ✓ | `void CreateParticleSystemAtEntityExt(	string &in asPSName, string &in asPSFile, string &in asEntity, bool abSavePS, float afR, float afG, float afB, float afA, bool abFadeAtDistance, float afFadeMinEnd, float afFadeMinStart, float afFadeMaxStart, float afFadeMaxEnd)` |
| `GetPlayerSanity` | 10 | ✓ | `float GetPlayerSanity()` |
| `RemoveAttachedPropFromProp` | 8 | — | `void RemoveAttachedPropFromProp(string& asPropName, string& asAttachName)` |
| `AddAttachedPropToProp` | 8 | — | `void AddAttachedPropToProp(string& asPropName, string& asAttachName, string& asAttachFile, float fPosX, float fPosY, float fPosZ, float fRotX, float fRotY, float fRot)` |
| `SetEnemyDisableTriggers` | 6 | ✓ | `void SetEnemyDisableTriggers(string &in asName, bool abX)` |
| `PlayPropAnimation` | 6 | — | `void PlayPropAnimation(string &in asProp, string &in asAnimation, float afFadeTime, bool abLoop, string &in asCallback)` |
| `GetPlayerSpeed` | 6 | ✓ | `float GetPlayerSpeed()` |
| `UnlockAchievement` | 5 | — | `void UnlockAchievement(string &in asName)` |
| `StopPropMovement` | 5 | ✓ | `void StopPropMovement(string &in asName)` |
| `StopAllEffectVoices` | 5 | ✓ | `void StopAllEffectVoices(float afFadeOutTime)` |
| `SetLightFlickerActive` | 5 | ✓ | `void SetLightFlickerActive(string& asLightName, bool abActive)` |
| `ChangePlayerStateToNormal` | 5 | — | `void ChangePlayerStateToNormal()` |
| `AddPlayerBodyForce` | 5 | ✓ | `void AddPlayerBodyForce(float afX, float afY, float afZ, bool abUseLocalCoords)` |
| `StartCredits` | 4 | — | `void StartCredits(string &in asMusic, bool abLoopMusic, string &in asTextCat, string &in asTextEntry, int alEndNum)` |
| `ShowEnemyPlayerPosition` | 4 | ✓ | `void ShowEnemyPlayerPosition(string &in asName)` |
| `RemoveUseItemCallback` | 4 | ✓ | `void RemoveUseItemCallback(string &in asName)` |
| `QuestIsAdded` | 4 | ✓ | `bool QuestIsAdded(string &in asName)` |
| `GetPlayerYSpeed` | 4 | ✓ | `float GetPlayerYSpeed()` |
| `GetFlashbackIsActive` | 4 | ✓ | `bool GetFlashbackIsActive()` |
| `SetNPCFollowPlayer` | 3 | — | `void SetNPCFollowPlayer(string &in asName, bool abX)` |
| `GetEffectVoiceActive` | 3 | ✓ | `bool GetEffectVoiceActive()` |
| `BreakJoint` | 2 | ✓ | `void BreakJoint(string &in asName)` |
| `SetWheelInteractionDisablesStuck` | 1 | ✓ | `void SetWheelInteractionDisablesStuck(string &in asName, bool abX)` |
| `SetNPCAwake` | 1 | — | `void SetNPCAwake(string &in asName, bool abAwake, bool abEffects)` |
| `SetLeverInteractionDisablesStuck` | 1 | ✓ | `void SetLeverInteractionDisablesStuck(string &in asName, bool abX)` |
| `SetEntityConnectionStateChangeCallback` | 1 | ~ | `void SetEntityConnectionStateChangeCallback(string& asName, string& asCallback)` |
| `Print` | 1 | ✓ | `void Print(string &in asString)` |
| `GetSwingDoorState` | 1 | ✓ | `int GetSwingDoorState(string &in asName)` |
| `GetPlayerHealth` | 1 | ✓ | `float GetPlayerHealth()` |
| `DetachFromStickyArea` | 1 | — | `void DetachFromStickyArea(string &in asAreaName)` |
| `DestroyDataCache` | 1 | — | `void DestroyDataCache()` |
| `CreateDataCache` | 1 | — | `void CreateDataCache()` |
| `AttachBodyToStickyArea` | 1 | — | `void AttachBodyToStickyArea(string& asAreaName, string& asBody)` |

## Общий скрипт инвентаря

Исходная статистика выше считала скрипты карт. Отдельный `maps/main/inventory.hps` тоже перенесён вручную — [журнал переноса](inventory.md).

| Функция | Статус | Поведение |
|---|:---:|---|
| `AddCombineCallback` | ✓ | Общий реестр игры, вызов InventoryScript с каноническим порядком пары, одноразовое удаление по объекту коллбэка |
| `RemoveCombineCallback` | ✓ | Удаление первого совпадающего имени |
| `ExitInventory` | ✓ | Закрытие сообщения либо выход с плавным переходом, как ExitPressed |
| `SetInventoryMessage` | ✓ | Локализация, ParseString, сообщение ждёт ручного закрытия, как исходный Update |

GiveItemFromFile читает определения `.ent` из генерируемого HplInventoryAssets. Подбор записок / дневников подключён к журналу и озвучке. RemoveItem по имени удаляет всю запись; использование расходника уменьшает стопку на одну единицу.

## Смена карты и фонарь

`ChangeMap` — ✓: переход как в `cLuxMapHandler::ChangeMap` / `CheckMapChange` (затемнение 1.5 с, OnLeave, сохранение карты, экран загрузки, затухание мировых звуков, загрузка сцены карты, восстановление сохранённой карты, OnStart при первом посещении и OnEnter, FadeIn 2 с); игрок, эффекты, музыка и глобальные громкость / скорость звука переходят в новую карту. Состояние посещённой карты — `HplSavedMaps` (порт `cLuxSavedGameMap`): локальные переменные, таймеры, use-item коллбэки, коллбэки столкновений игрока и сущностей, свет, звуки и системы частиц из скрипта, связи сущностей (коллбэки взгляда / взаимодействия / состояния, соединения), отключённые сущности, запертые двери и двери уровня, подобранные предметы, разбитые контейнеры, погашенные лампы, сущности FullGameSave (MoveObject, созданные скриптом) целиком. `ClearSavedMaps` — ✓. `FadeLightTo` / `SetLightVisible` работают и с box-светом (`cLightBox`, `HplBoxLightPass`). `SetupLoadScreen` — ✓: экран загрузки `cLuxLoadScreenHandler` с текстом `LoadingText` и картинкой. `SetLanternActive`, `SetLanternDisabled`, `GetLanternActive`, `SetLanternLitCallback` — ✓: порт `cLuxPlayerLantern` с моделью фонаря в руке (`cLuxPlayerHands`, `cLuxHandObject_LightSource`).

## Журнал

`AddDiary` — ✓: добавляет следующую запись внутри серии; API добавления не открывают экран. `ReturnOpenJournal` вызывается из DiaryCallback, который получает номер с 1 внутри серии; false оставляет запись доступной через N без автооткрытия. Подбор записок с включённым открытием и дневников запускает чтение / озвучку. `CompleteQuest` скрывает выполненное задание из списка, но процент завершения ещё не реализован. Подробности и проверки — [journal.md](journal.md).

## Остальные функции без вызовов в скриптах карт (можно не портировать)

`AddGlobalVarFloat`, `AddGlobalVarString`, `AddKeyPart`, `AddLocalVarString`, `AddPlayerSanity`, `AlertEnemyOfPlayerPresence`, `AttachPropToProp`, `AttachPropToStickyArea`, `ChangeManPigPose`, `FadeEnemyToSmoke`, `FadeInSound`, `ForceTeslaPigSighting`, `GetBodyMass`, `GetEnemyStateName`, `GetEntityPosX`, `GetEntityPosY`, `GetEntityPosZ`, `GetGlobalVarFloat`, `GetPlayerPosX`, `GetPlayerPosY`, `GetPlayerPosZ`, `GetPropHealth`, `GetPropIsInteractedWith`, `GetSwingDoorLocked`, `InsanityEventIsActive`, `MathAbs`, `MathAcos`, `MathAsin`, `MathAtan`, `MathAtan2`, `MathClamp`, `MathCos`, `MathMax`, `MathMin`, `MathPow`, `MathSin`, `MathSqrt`, `MathTan`, `MovePlayerForward`, `PlaceEntityAtEntity`, `ProgLog`, `ReplaceEntity`, `SetAllowStickyAreaAttachment`, `SetBodyMass`, `SetButtonSwitchedOn`, `SetEnemyIsHallucination`, `SetEnemySanityDecreaseActive`, `SetEntityPos`, `SetEntityVisible`, `SetGlobalVarFloat`, `SetInDarknessEffectsActive`, `SetLanternLitCallback`, `SetLevelDoorLockedSound`, `SetLevelDoorLockedText`, `SetMultiSliderCallback`, `SetMultiSliderStuckState`, `SetPlayerFallDamageDisabled`, `SetPlayerJumpForceMul`, `SetPlayerPos`, `SetSkyBoxTexture`, `SetTeslaPigEasyEscapeDisabled`, `SetTeslaPigFadeDisabled`, `SetTeslaPigSoundDisabled`, `StartDemoEnd`, `StartInsanityEvent`, `StartPlayerSpawnPS`, `StartRandomInsanityEvent`, `StopCurrentInsanityEvent`, `StopPlayerSpawnPS`, `StringToBool`, `StringToFloat`, `StringToInt`, `TeleportEnemyToEntity`, `TeleportEnemyToNode`

## Рассудок и темнота

`GiveSanityDamage` — ✓: подсказка SanityHit, удар `cLuxPlayerSanity::StartHit` (звук, наезд камеры), потеря рассудка с эффектом (сердцебиение, утечка, эффект безумия), обморок ниже 0. `SetSanityDrainDisabled` — ✓: отключает потерю рассудка в темноте (`cLuxPlayerInDarkness`). `SetInDarknessEffectsActive` — ✓ (без вызовов в картах): `cLuxPlayerInDarkness::SetActive`. `SetInsanitySetEnabled` — ✓, а также без вызовов в картах `StartInsanityEvent`, `StartRandomInsanityEvent`, `StopCurrentInsanityEvent`, `InsanityEventIsActive` — ✓: `cLuxInsanityHandler` (`Effects/HplInsanity`, CLAUDE.md «События безумия»); неизвестное имя события ничего не делает, как в HPL2.

## Урон и смерть

`GivePlayerDamage` — ✓: тип по строке без учёта регистра (`claws`, `slash`, иначе BloodSplat), сила 1; брызги, красная вспышка HUD, поворот головы, не смертельный удар оставляет 10, ниже 0 — смерть (`cLuxPlayer::GiveDamage`). `CheckPoint` — ✓: `cLuxMap::SetCheckPoint` (только новое имя, запоминает зацикленную музыку высшего приоритета) + подсказка смерти; после смерти `LoadCheckPoint` ставит игрока на стартовую точку и вызывает коллбэк `(имя, счётчик)`. `SetDeathHint`, `DisableDeathStartSound` — ✓ (`cLuxPlayerDeath`). `SetPlayerHealth` / `AddPlayerHealth` при здоровье ≤ 0 начинают смерть.

## Враги

`AddEnemyPatrolNode`, `ClearEnemyPatrolNodes`, `SetEnemyDisabled`, `SetEnemyDisableTriggers`, `ShowEnemyPlayerPosition` — ✓ для Grunt (`EnemyGrunt`) и водяного монстра (`EnemyWaterLurker`, 6.4), см. CLAUDE.md «Враги»: узел ищется по имени в контейнере узлов врага (нет контейнера / узла — ошибка движка, как в HPL2); `ShowEnemyPlayerPosition` всегда переводит в охоту (условие оригинала с `||` всегда истинно). Без вызовов в картах, но тоже перенесены: `AlertEnemyOfPlayerPresence`, `SetEnemyIsHallucination`, `FadeEnemyToSmoke` (без растворения модели), `SetEnemySanityDecreaseActive`, `TeleportEnemyToNode`, `TeleportEnemyToEntity`, `GetEnemyStateName`, `SetEntityPos` для врага (ступни тела). Man pig (`Enemy_ManPig`) — враг A Machine for Pigs, в данных The Dark Descent его сущности нет: функции на нём — `NotPorted` (`needs the man pig (not in The Dark Descent)`), функции man pig (`ChangeManPigPose`, `SetTeslaPig*`, `ForceTeslaPigSighting`) не переносятся.
