# 01_cells

Оригинал: `source/maps/ptest/01_cells.hps` (1661 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Justine/JustineCells.cs`.
Класс `RTXMnesia.Levels.Justine.JustineCells`, атрибут `HplMapScript("maps/ptest/01_cells.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/ptest/01_cells.unity` (вне git).
Дата: 2026-10-17.
Статус: готов — полный скрипт перенесён, проверки выполнены; готовность порта не означает полного прохождения механик движка.

## Функции

| Оригинал (строка) | C# / сигнатура | Статус |
|---|---|---|
| `01_cells.hps:5` | `JustineCells.IntroSetup(bool bIntroActive) → void` | перенесена |
| `01_cells.hps:30` | `JustineCells.Enemy1Patrol() → void` | перенесена |
| `01_cells.hps:38` | `JustineCells.TimerEIntro(string asTimer) → void` | перенесена |
| `01_cells.hps:200` | `JustineCells.TimerExtraSound(string asTimer) → void` | перенесена |
| `01_cells.hps:205` | `JustineCells.TimerDoBlurPounding(string asTimer) → void` | перенесена |
| `01_cells.hps:227` | `JustineCells.StateChangePhono(string asEntity, int alState) → void` | перенесена |
| `01_cells.hps:240` | `JustineCells.EntityCallPickLantern(string asEntity, string type) → void` | перенесена |
| `01_cells.hps:260` | `JustineCells.TimerPullOutLock(string asTimer) → void` | перенесена |
| `01_cells.hps:303` | `JustineCells.CollideAreaStrangeSound(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:322` | `JustineCells.TimerStrangeSounds(string asTimer) → void` | перенесена |
| `01_cells.hps:345` | `JustineCells.TimerStopEffect(string asTimer) → void` | перенесена |
| `01_cells.hps:360` | `JustineCells.CollideAreaWindActivate(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:374` | `JustineCells.TimerAreaWind(string asTimer) → void` | перенесена |
| `01_cells.hps:390` | `JustineCells.Enemy3Patrol() → void` | перенесена |
| `01_cells.hps:396` | `JustineCells.CollideAreaBasementEnemyActivate(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:403` | `JustineCells.TimerRemoveEnemy3(string asTimer) → void` | перенесена |
| `01_cells.hps:411` | `JustineCells.TimerNoRush(string asTimer) → void` | перенесена |
| `01_cells.hps:457` | `JustineCells.Enemy2Patrol() → void` | перенесена |
| `01_cells.hps:466` | `JustineCells.CollideAreaEnemyActivate(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:477` | `JustineCells.CollideAreaExtraEnemyWarning(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:484` | `JustineCells.CollideAreaExtraEnemyWarningOff(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:491` | `JustineCells.CollideAreaEnemy2Door(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:502` | `JustineCells.EntityCallEnemyDisabled(string asEntity, string type) → void` | перенесена |
| `01_cells.hps:515` | `JustineCells.StateChangeLever(string asEntity, int alState) → void` | перенесена |
| `01_cells.hps:576` | `JustineCells.TimerLadderImpactHatch(string asTimer) → void` | перенесена |
| `01_cells.hps:618` | `JustineCells.RollDownLadder() → void` | перенесена |
| `01_cells.hps:635` | `JustineCells.TimerStopRoll(string asTimer) → void` | перенесена |
| `01_cells.hps:650` | `JustineCells.PierceBody() → void` | перенесена |
| `01_cells.hps:666` | `JustineCells.TimerStopPierce(string asTimer) → void` | перенесена |
| `01_cells.hps:698` | `JustineCells.TimerStopPierceSound(string asTimer) → void` | перенесена |
| `01_cells.hps:717` | `JustineCells.InteractCeilingHatch(string asEntity) → void` | перенесена |
| `01_cells.hps:725` | `JustineCells.InteractLadderArea(string asEntity) → void` | перенесена |
| `01_cells.hps:731` | `JustineCells.UseLadderOnArea(string asItem, string asEntity) → void` | перенесена |
| `01_cells.hps:755` | `JustineCells.CollideAreaBangOnHatch(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:765` | `JustineCells.TimerBreakHatch(string asTimer) → void` | перенесена |
| `01_cells.hps:778` | `JustineCells.CollideAreaHatchEffects(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:785` | `JustineCells.CollideAreaHatchDisabled(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:798` | `JustineCells.DisableOtherSolutions(string sSolution) → void` | перенесена |
| `01_cells.hps:828` | `JustineCells.CollideCharacterStartTalk(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:843` | `JustineCells.CollideCharacterStopTalk(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:857` | `JustineCells.GetCharacterTopicNotSpoken(string asTopic) → bool` | перенесена |
| `01_cells.hps:863` | `JustineCells.SetCharacterTopicSpoken(string asTopic) → void` | перенесена |
| `01_cells.hps:869` | `JustineCells.SetCharacterTopicNotSpoken(string asTopic) → void` | перенесена |
| `01_cells.hps:875` | `JustineCells.PlayCharacterVoice(string asTopic, int alNumOfParts, bool abRandom, bool abSetAsSpoken, bool abForceNewTopic) → void` | перенесена |
| `01_cells.hps:952` | `JustineCells.CharacterTalkOver(float fStopVoiceFade) → void` | перенесена |
| `01_cells.hps:963` | `JustineCells.TimerDelayIdleVoice(string asTimer) → void` | перенесена |
| `01_cells.hps:971` | `JustineCells.StartCharacterSpeak(string sTopicName, int iNr, bool abRandom,  bool abSetAsSpoken, bool abForceNewTopic) → void` | перенесена |
| `01_cells.hps:995` | `JustineCells.StopCharacterSpeak() → void` | перенесена |
| `01_cells.hps:1013` | `JustineCells.CollideAreaActivateCorridorEnemy(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1028` | `JustineCells.Enemy4Patrol() → void` | перенесена |
| `01_cells.hps:1042` | `JustineCells.CollideAreaEndLoftScares(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1052` | `JustineCells.CollideAreaLoftScares(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1087` | `JustineCells.LoftScareStep(string sSound, string sArea) → void` | перенесена |
| `01_cells.hps:1104` | `JustineCells.LoftScareHaltEnd() → void` | перенесена |
| `01_cells.hps:1127` | `JustineCells.ActivateAlternateLoftEvent() → void` | перенесена |
| `01_cells.hps:1150` | `JustineCells.InteractLockedDoor(string asEntity) → void` | перенесена |
| `01_cells.hps:1163` | `JustineCells.TimerLockedDoor(string asTimer) → void` | перенесена |
| `01_cells.hps:1175` | `JustineCells.CollideAreaLadderHint(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1181` | `JustineCells.CollideAreaLadderUp(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1187` | `JustineCells.TimerTeleport(string asTimer) → void` | перенесена |
| `01_cells.hps:1197` | `JustineCells.CollideAreaFire(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1216` | `JustineCells.InteractAreaFire(string asEntity) → void` | перенесена |
| `01_cells.hps:1221` | `JustineCells.InteractIron(string asEntity) → void` | перенесена |
| `01_cells.hps:1227` | `JustineCells.TimerFireBackOn(string asTimer) → void` | перенесена |
| `01_cells.hps:1242` | `JustineCells.CollideAreaChangeAmbMusic(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1264` | `JustineCells.StateChangePhono02(string asEntity, int alState) → void` | перенесена |
| `01_cells.hps:1270` | `JustineCells.PlayPhono(int iLevel, int iSet, int iPart, string sEntity) → void` | перенесена |
| `01_cells.hps:1288` | `JustineCells.VoiceOverPhono() → void` | перенесена |
| `01_cells.hps:1295` | `JustineCells.CollideAreaStopPhono(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1304` | `JustineCells.CollideAreaBirdsFly(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1311` | `JustineCells.CollideAreaCell(string asParent, string asChild, int alState) → void` | перенесена |
| `01_cells.hps:1350` | `JustineCells.TimerStopCellSound(string asTimer) → void` | перенесена |
| `01_cells.hps:1365` | `JustineCells.UseWaxOnPhono(string asItem, string asEntity) → void` | перенесена |
| `01_cells.hps:1387` | `JustineCells.InteractPhono03(string asEntity) → void` | перенесена |
| `01_cells.hps:1392` | `JustineCells.StateChangePhono03(string asEntity, int alState) → void` | перенесена |
| `01_cells.hps:1473` | `JustineCells.PickupNote01(string asEntity, string asType) → void` | перенесена |
| `01_cells.hps:1479` | `JustineCells.PickupNote02(string asEntity, string asType) → void` | перенесена |
| `01_cells.hps:1485` | `JustineCells.PickupNote03(string asEntity, string asType) → void` | перенесена |
| `01_cells.hps:1497` | `JustineCells.OnStart() → void` | перенесена |
| `01_cells.hps:1632` | `JustineCells.OnEnter() → void` | перенесена |
| `01_cells.hps:1651` | `JustineCells.OnLeave() → void` | перенесена |

Всего 81 функций, включая неиспользуемые, отладочные и недостижимые участки.

## Поля, массивы, константы и enum

| Исходная строка | Данные (исходные значения и порядок) |
|---|---|
| — | Глобальных полей, массивов, const и enum нет. |

## Синтаксис и исходные особенности

Ручной перенос сохраняет имена/порядок функций и операций, поля, условия, циклы, строки и английские комментарии. Только существующие lifecycle-функции стали public override; string &in/& → string, статические callbacks → nameof, десятичные литералы → float, and/or/not → C#-операторы. Локальная string без инициализации получает пустую строку, как в AngelScript. AddKeyPart с верхним битом — unchecked((int)0x…), все 32 бита/порядок сохранены.

Независимая сверка 26 891 токена и 980 исходных комментариев во всех четырёх портах: `Logs/Justine_TokenReview.txt`; фактические .cs дополнительно сравниваются с reviewed drafts. Компиляция: `Logs/Justine_Compile.txt`, затем Unity перед каждым запуском. Исходные неиспользуемые/недостижимые участки вызывают CS0162/CS0219/CS0169 и не удалялись.

- `01_cells.hps:914`: Во всех четырёх оригиналах CharacterTalkOver(float) назначается нулевым voice callback. LuxEffectHandler.cpp:1086 и :1141 исполняет callback с (). Сохранена несовместимая исходная сигнатура; ERROR не скрыт и не считается чистым PASS.
- `01_cells.hps:570`: Снятие LadderOnHatch при решении рычагом может предшествовать его регистрации hatch-событием. Предупреждение о ненайденном callback не исправлялось.
- `01_cells.hps:731`: Диагностика выдаёт ladder_item_1 и вызывает исходный обработчик; физический подбор/наведение предмета и решение rope-загадки этим не доказаны.
- `01_cells.hps:1485`: NoteCountNum — исходный накопительный код, не число записок; результат 437 после трёх обработчиков сохраняется.

## API и пределы реализации

Подготовка профиля/режима смерти выполнена отдельно в J0: [README.md](README.md). Русские тексты DLC и английская озвучка lang/eng/ptest; основные рецепты отсутствуют.

| API | Вызовов в .hps | Фактическое объявление / статус |
|---|---:|---|
| `AddDebugMessage` | 28 | `AddDebugMessage(string asString, bool abCheckForDuplicates)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:14`) |
| `AddEffectVoice` | 16 | `AddEffectVoice(string asVoiceFile, string asEffectFile, string asTextCat, string asTextEntry, bool abUsePostion,                                       string asPosEnitity, float afMinDistance, float afMaxDistance)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:31`) |
| `AddEnemyPatrolNode` | 10 | `AddEnemyPatrolNode(string asEnemyName, string asNodeName, float afWaitTime, string asAnimation)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:569`) |
| `AddEntityCollideCallback` | 28 | `AddEntityCollideCallback(string asParentName, string asChildName, string asFunction, bool abDeleteOnCollide, int alStates)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:205`) |
| `AddKeyPart` | 6 | `AddKeyPart(int alKeyPart)` — ✓ с 2026-10-17 (CLAUDE.md «Титры, NPC, анимации, прикреплённые пропы и критеры») |
| `AddLocalVarFloat` | 1 | `AddLocalVarFloat(string asName, float afVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:61`) |
| `AddLocalVarInt` | 9 | `AddLocalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:58`) |
| `AddPropImpulse` | 3 | `AddPropImpulse(string asName, float afX, float afY, float afZ, string asCoordSystem)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:424`) |
| `AddTimer` | 34 | `AddTimer(string asName, float afTime, string asFunction)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:44`) |
| `AddUseItemCallback` | 5 | `AddUseItemCallback(string asName, string asItem, string asEntity, string asFunction, bool abAutoDestroy)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:96`) |
| `BlockHint` | 2 | `BlockHint(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:53`) |
| `CreateParticleSystemAtEntity` | 10 | `CreateParticleSystemAtEntity(string asPSName, string asPSFile, string asEntity, bool abSavePS)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:165`) |
| `FadeGlobalSoundSpeed` | 4 | `FadeGlobalSoundSpeed(float afDestSpeed, float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:150`) |
| `FadeIn` | 6 | `FadeIn(float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:8`) |
| `FadeOut` | 5 | `FadeOut(float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:10`) |
| `FadePlayerAspectMulTo` | 2 | `FadePlayerAspectMulTo(float afX, float afSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:93`) |
| `FadePlayerFOVMulTo` | 2 | `FadePlayerFOVMulTo(float afX, float afSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:91`) |
| `FadePlayerRollTo` | 7 | `FadePlayerRollTo(float afX, float afSpeedMul, float afMaxSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:96`) |
| `FadeRadialBlurTo` | 10 | `FadeRadialBlurTo(float afSize, float afSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:16`) |
| `GetEffectVoiceActive` | 1 | `GetEffectVoiceActive()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:46`) |
| `GetFlashbackIsActive` | 1 | `GetFlashbackIsActive()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:53`) |
| `GetLocalVarFloat` | 1 | `GetLocalVarFloat(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:72`) |
| `GetLocalVarInt` | 21 | `GetLocalVarInt(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:66`) |
| `GetLocalVarString` | 11 | `GetLocalVarString(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:78`) |
| `GetSwingDoorState` | 1 | `GetSwingDoorState(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:307`) |
| `GiveHint` | 1 | `GiveHint(string asName, string asMessageCat, string asMessageEntry, float afTimeShown)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:48`) |
| `GiveItemFromFile` | 1 | `GiveItemFromFile(string asName, string asFileName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:75`) |
| `GivePlayerDamage` | 1 | `GivePlayerDamage(float afAmount, string asType, bool abSpinHead, bool abLethal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:82`) |
| `InteractConnectPropWithMoveObject` | 1 | `InteractConnectPropWithMoveObject(string asName, string asPropName, string asMoveObjectName, bool abInteractOnly,                                                          bool abInvert, int alStatesUsed)` — ✓ с 2026-10-17 (CLAUDE.md «Верёвки, связи, липкие зоны, слизь») |
| `InteractConnectPropWithRope` | 2 | `InteractConnectPropWithRope(string asName, string asPropName, string asRopeName, bool abInteractOnly, float afSpeedMul,                                                    float afMinSpeed, float afMaxSpeed, bool abInvert, int alStatesUsed)` — ✓ с 2026-10-17 (CLAUDE.md «Верёвки, связи, липкие зоны, слизь») |
| `MovePlayerHeadPos` | 6 | `MovePlayerHeadPos(float afX, float afY, float afZ, float afSpeed, float afSlowDownDist)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:103`) |
| `PlayGuiSound` | 21 | `PlayGuiSound(string asSoundFile, float afVolume)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:73`) |
| `PlayMusic` | 9 | `PlayMusic(string asMusicFile, bool abLoop, float afVolume, float afFadeTime, int alPrio, bool abResume)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:137`) |
| `PlayPropAnimation` | 1 | `PlayPropAnimation(string asProp, string asAnimation, float afFadeTime, bool abLoop, string asCallback)` — ✓ с 2026-10-17 (CLAUDE.md «Титры, NPC, анимации, прикреплённые пропы и критеры») |
| `PlaySoundAtEntity` | 51 | `PlaySoundAtEntity(string asSoundName, string asSoundFile, string asEntity, float afFadeSpeed, bool abSaveSound)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:87`) |
| `PreloadSound` | 33 | `PreloadSound(string asSoundFile)` — empty (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:80`) |
| `RandFloat` | 17 | `RandFloat(float afMin, float afMax)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:26`) |
| `RandInt` | 4 | `RandInt(int alMin, int alMax)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:28`) |
| `RemoveItem` | 2 | `RemoveItem(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:85`) |
| `RemoveTimer` | 5 | `RemoveTimer(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:46`) |
| `RemoveUseItemCallback` | 3 | `RemoveUseItemCallback(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:122`) |
| `RotatePropToSpeed` | 10 | `RotatePropToSpeed(string asName, float afAcc, float afGoalSpeed, float afAxisX, float afAxisY, float afAxisZ,                                          bool abResetSpeed, string asOffsetArea)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:249`) |
| `ScriptDebugOn` | 1 | `ScriptDebugOn()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:24`) |
| `SetEffectVoiceOverCallback` | 7 | `SetEffectVoiceOverCallback(string asFunc)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:48`) |
| `SetEntityActive` | 23 | `SetEntityActive(string asName, bool abActive)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:58`) |
| `SetEntityInteractionDisabled` | 5 | `SetEntityInteractionDisabled(string asName, bool abDisabled)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:191`) |
| `SetEntityPlayerInteractCallback` | 2 | `SetEntityPlayerInteractCallback(string asName, string asCallback, bool abRemoveOnInteraction)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:176`) |
| `SetGlobalVarInt` | 2 | `SetGlobalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:82`) |
| `SetInventoryDisabled` | 2 | `SetInventoryDisabled(bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:63`) |
| `SetLeverInteractionDisablesStuck` | 1 | `SetLeverInteractionDisablesStuck(string asName, bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:351`) |
| `SetLeverStuckState` | 4 | `SetLeverStuckState(string asName, int alState, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:341`) |
| `SetLightVisible` | 1 | `SetLightVisible(string asLightName, bool abVisible)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:18`) |
| `SetLocalVarInt` | 19 | `SetLocalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:52`) |
| `SetLocalVarString` | 1 | `SetLocalVarString(string asName, string asVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:56`) |
| `SetMapDisplayNameEntry` | 1 | `SetMapDisplayNameEntry(string asNameEntry)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:138`) |
| `SetMessage` | 6 | `SetMessage(string asTextCategory, string asTextEntry, float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:9`) |
| `SetMoveObjectState` | 10 | `SetMoveObjectState(string asName, float afState)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:397`) |
| `SetPlayerCrouchDisabled` | 2 | `SetPlayerCrouchDisabled(bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:131`) |
| `SetPlayerCrouching` | 5 | `SetPlayerCrouching(bool abCrouch)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:14`) |
| `SetPlayerJumpDisabled` | 2 | `SetPlayerJumpDisabled(bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:130`) |
| `SetPlayerLampOil` | 1 | `SetPlayerLampOil(float afOil)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:41`) |
| `SetPlayerLookSpeedMul` | 8 | `SetPlayerLookSpeedMul(float afMul)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:128`) |
| `SetPlayerMoveSpeedMul` | 7 | `SetPlayerMoveSpeedMul(float afMul)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:126`) |
| `SetPlayerPermaDeathSound` | 1 | `SetPlayerPermaDeathSound(string asSound)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:50`) |
| `SetPlayerRunSpeedMul` | 2 | `SetPlayerRunSpeedMul(float afMul)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:127`) |
| `SetPropActiveAndFade` | 4 | `SetPropActiveAndFade(string asName, bool abActive, float afFadeTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:236`) |
| `SetPropHealth` | 1 | `SetPropHealth(string asName, float afHealth)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:377`) |
| `SetPropStaticPhysics` | 1 | `SetPropStaticPhysics(string asName, bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:241`) |
| `SetRadialBlurStartDist` | 5 | `SetRadialBlurStartDist(float afStartDist)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:18`) |
| `SetSwingDoorClosed` | 2 | `SetSwingDoorClosed(string asName, bool abClosed, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:290`) |
| `SetSwingDoorDisableAutoClose` | 2 | `SetSwingDoorDisableAutoClose(string asName, bool abDisableAutoClose)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:295`) |
| `SetSwingDoorLocked` | 1 | `SetSwingDoorLocked(string asName, bool abLocked, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:285`) |
| `SetWheelInteractionDisablesStuck` | 1 | `SetWheelInteractionDisablesStuck(string asName, bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:346`) |
| `SetWheelStuckState` | 4 | `SetWheelStuckState(string asName, int alState, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:336`) |
| `SetupLoadScreen` | 1 | `SetupLoadScreen(string asTextCat, string asTextEntry, int alRandomNum, string asImageFile)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:156`) |
| `StartPlayerLookAt` | 12 | `StartPlayerLookAt(string asEntityName, float afSpeedMul, float afMaxSpeed, string asAtTargetCallback)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:107`) |
| `StartScreenShake` | 2 | `StartScreenShake(float afAmount, float afTime, float afFadeInTime, float afFadeOutTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:50`) |
| `StopAllEffectVoices` | 3 | `StopAllEffectVoices(float afFadeOutTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:44`) |
| `StopMusic` | 1 | `StopMusic(float afFadeTime, int alPrio)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:140`) |
| `StopPlayerLookAt` | 1 | `StopPlayerLookAt()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:124`) |
| `StopSound` | 9 | `StopSound(string asSoundName, float afFadeTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:123`) |
| `TeleportPlayer` | 2 | `TeleportPlayer(string asStartPosName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:134`) |

Статус implemented означает наличие реализации, а не полную совместимость механики. Suitor-модели/параметры импортированы, но EnemyConverter не включает их отдельные анимации из entities/ptest; патрули/активация не доказывают полную погоню и атаки. Interact connections, rope, Sticky/prop animation и титры/ARG остаются явными ограничениями. Пустые API в таблице отмечены отдельно и не выдаются за реализованные.

## Импорт и статическая сверка

Импорт: `Logs/Justine_J0_Map.txt` и одноимённый .log, Unity ExitCode=0, 0 ошибок. Числа/виды предупреждений зафиксированы в отчёте импорта. Исторические D:/SVN и E:/amnesia переходы не переписаны.

Штатная команда: `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -MapRoot maps/ptest -Map 01_cells -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Justine/JustineCells.cs`. Отчёт `Logs/Justine_Check_Final_01_cells.txt`: требование 0 ошибок; отсутствующие исходные callbacks не подменяются фиктивными функциями.


Фактически импорт: 26 предупреждений, 0 ошибок. Предупреждение No level script port относится к импорту до установки нового класса; последующие запуски уже используют порт. FogArea пока пропускается; missing child отражает привязки скелетных узлов, а не численный PASS геометрии. gui_amn_mm_textbox_marker.tga — неиспользуемая данным меню ссылка skin.

| Предупреждение импорта | Количество |
|---|---:|
| entities/prison/worn_hay_mattress/worn_hay_mattress.ent: could not find child with unique ID 2 to attach to body 'Body_1' | 1 |
| entities/prison/worn_hay_mattress/worn_hay_mattress.ent: could not find child with unique ID 1 to attach to body 'Body_2' | 1 |
| entities/prison/worn_hay_mattress/worn_hay_mattress.ent: could not find child with unique ID 3 to attach to body 'Body_3' | 1 |
| entities/prison/worn_hay_mattress/worn_hay_mattress.ent: could not find child with unique ID 6 to attach to body 'Body_4' | 1 |
| entities/prison/worn_hay_mattress/worn_hay_mattress.ent: could not find child with unique ID 4 to attach to body 'Body_5' | 1 |
| entities/prison/worn_hay_mattress/worn_hay_mattress.ent: could not find child with unique ID 7 to attach to body 'Body_6' | 1 |
| entities/prison/worn_hay_mattress/worn_hay_mattress.ent: could not find child with unique ID 5 to attach to body 'Body_7' | 1 |
| entities/prison/worn_hay_mattress/worn_hay_mattress.ent: could not find child with unique ID 8 to attach to body 'Body_8' | 1 |
| entities/ptest/worn_hay_mattress_slow/worn_hay_mattress_slow.ent: could not find child with unique ID 2 to attach to body 'Body_1' | 1 |
| entities/ptest/worn_hay_mattress_slow/worn_hay_mattress_slow.ent: could not find child with unique ID 1 to attach to body 'Body_2' | 1 |
| entities/ptest/worn_hay_mattress_slow/worn_hay_mattress_slow.ent: could not find child with unique ID 3 to attach to body 'Body_3' | 1 |
| entities/ptest/worn_hay_mattress_slow/worn_hay_mattress_slow.ent: could not find child with unique ID 6 to attach to body 'Body_4' | 1 |
| entities/ptest/worn_hay_mattress_slow/worn_hay_mattress_slow.ent: could not find child with unique ID 4 to attach to body 'Body_5' | 1 |
| entities/ptest/worn_hay_mattress_slow/worn_hay_mattress_slow.ent: could not find child with unique ID 7 to attach to body 'Body_6' | 1 |
| entities/ptest/worn_hay_mattress_slow/worn_hay_mattress_slow.ent: could not find child with unique ID 5 to attach to body 'Body_7' | 1 |
| entities/ptest/worn_hay_mattress_slow/worn_hay_mattress_slow.ent: could not find child with unique ID 8 to attach to body 'Body_8' | 1 |
| 'FogArea' is not supported yet (skipped) | 1 |
| entities/ptest/ritual_prisoner_noillum/ritual_prisoner_noillum.ent: could not find child with unique ID 0 to attach to body 'Body_1' | 1 |
| entities/ptest/ritual_prisoner_noillum/ritual_prisoner_noillum.ent: could not find child with unique ID 1 to attach to body 'Body_1' | 1 |
| entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1' | 1 |
| entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2' | 1 |
| entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3' | 1 |
| entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4' | 1 |
| entities/door/safety_normal_vert/safety_normal_vert_noauto.ent: could not find child with unique ID 1 to attach to body 'Body_1' | 1 |
| Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga | 1 |
| No level script port for maps/ptest/01_cells.map yet (the level runs without its events) | 1 |

## Сценарии

Все ниже: seed=1, каждый сценарий в свежем Unity-процессе, PlayerSimulation.RunBatch, GPU (без -nographics), -batchmode -quit, исходная карта maps/ptest. Логи в UnityProj/UMnesiaRTX/Logs, имя Justine_<fixture> .log/.txt/.scenario.txt/.exit.txt. W/E — фактические строки отчёта, дополнительно проверены вложенные [Script] ERROR и исключения. ExitCode=0 сам по себе не означает чистый PASS.

### 01_cells_start

ExitCode=0; предупреждений=9, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 55 5; shot start; w 1; watch 2 1; shot walked; health
```

Наблюдавшиеся voice-файлы: .

### 01_cells_phono_lantern

ExitCode=0; предупреждений=7, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX12.

```text
watch 1 1; call SetWheelStuckState phonograph_1 1 true; watch 65 5; sounds; call GetLocalVarInt PlayPhono; call EntityCallPickLantern lantern_1 OnPickup; watch 12 2; body prison_section_6; body padlock_2; shot cell_open
```

Возвращённые значения: `GetLocalVarInt PlayPhono` → `0`.

Наблюдавшиеся voice-файлы: `justine_rec_lvl1_01_01.ogg`, `justine_rec_lvl1_01_02.ogg`, `justine_rec_lvl1_01_03.ogg`, `justine_rec_lvl1_01_04.ogg`.

### 01_cells_lever_kill

ExitCode=0; предупреждений=9, ошибок=1, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX12.

```text
watch 1 1; call SetLeverStuckState lever_simple01_1 1 true; watch 2 1; call SetLeverStuckState lever_simple01_1 1 true; watch 2 1; call SetLeverStuckState lever_simple01_1 1 true; watch 20 2; call GetLocalVarInt LeverPullCount; call GetLocalVarInt HatchOpen; call GetGlobalVarInt VictorIsDead; body ritual_prisoner_noillum_1; body LadderArea_1; shot prisoner
```

Возвращённые значения: `GetLocalVarInt LeverPullCount` → `3`; `GetLocalVarInt HatchOpen` → `1`; `GetGlobalVarInt VictorIsDead` → `1`.

Наблюдавшиеся voice-файлы: `victor_terrorized_01.ogg`, `victor_scared_01.ogg`, `victor_dying_01.ogg`, `victor_death_01.ogg`.

Результат содержит исходную ошибку callback; не чистый PASS.

### 01_cells_ladder_wax

ExitCode=0; предупреждений=6, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX12.

```text
watch 1 1; call InteractCeilingHatch hatch_ceiling_1; watch 3 1; call GiveItemFromFile ladder_item_1 ladder_item.ent; call UseLadderOnArea ladder_item_1 AreaAttachLadder; watch 2 1; call GetLocalVarInt ManualLadderPlaced; body ladder_static_1; call UseWaxOnPhono waxroll_record_1 phonograph_1; call UseWaxOnPhono waxroll_record_1 phonograph_3; call SetWheelStuckState phonograph_3 1 true; watch 100 10; call GetLocalVarInt PlayPhono; call GetGlobalVarInt VictorIsDead; sounds; start tortureroom; shot saved
```

Возвращённые значения: `GetLocalVarInt ManualLadderPlaced` → `1`; `GetLocalVarInt PlayPhono` → `0`; `GetGlobalVarInt VictorIsDead` → `0`.

Наблюдавшиеся voice-файлы: `brokenrec_justine_01.ogg`, `brokenrec_justine_03.ogg`, `brokenrec_basile_04.ogg`, `brokenrec_justine_05.ogg`, `brokenrec_basile_08.ogg`, `brokenrec_basile_10.ogg`.

### 01_cells_scares

ExitCode=0; предупреждений=7, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX12.

```text
watch 1 1; call Enemy1Patrol; call CollideAreaWindActivate Player AreaWindActivate 1; watch 10 2; call CollideAreaBasementEnemyActivate Player AreaBasementEnemyActivate 1; watch 15 3; call CollideAreaEnemyActivate Player AreaEnemyActivate 1; call EntityCallEnemyDisabled enemy_suitor_2 OnAutoDisabled; call CollideAreaActivateCorridorEnemy Player AreaActivateCorridorEnemy 1; call CollideAreaLoftScares Player ScriptArea_1 1; call CollideAreaLoftScares Player ScriptArea_2 1; call CollideAreaLoftScares Player ScriptArea_3 1; call CollideAreaLoftScares Player ScriptArea_4 1; call CollideAreaLoftScares Player ScriptArea_5 1; call CollideAreaEndLoftScares Player AreaEndLoftScare 1; watch 10 2; call CollideAreaChangeAmbMusic Player AreaChangeAmbMusic_2 1; call InteractAreaFire AreaFire; watch 2 1; health; start tunnel; shot loft
```

Наблюдавшиеся voice-файлы: .

### 01_cells_prisoner_topics

ExitCode=0; предупреждений=5, ошибок=1, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX12.

```text
watch 1 1; call CollideCharacterStartTalk Player AreaCharStartTalk 1; watch 35 2; sounds; call CollideCharacterStopTalk Player AreaCharStopTalk -1; call StartCharacterSpeak victor_idle 5 true false true; watch 15 2; call StopCharacterSpeak; call PickupNote01 note_1 OnPickup; call PickupNote02 note_2 OnPickup; call PickupNote03 note_3 OnPickup; call GetLocalVarInt NoteCountNum
```

Возвращённые значения: `GetLocalVarInt NoteCountNum` → `437`.

Наблюдавшиеся voice-файлы: `victor_discover_01.ogg`, `victor_idle_01.ogg`.

Результат содержит исходную ошибку callback; не чистый PASS.

### 01_cells_perma_death

ExitCode=0; предупреждений=5, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX12.

```text
watch 1 1; health; call GivePlayerDamage 500 claws false true; watch 35 2; health; sounds
```

Наблюдавшиеся voice-файлы: .

### 01_cells_ladder_passage

ExitCode=0; предупреждений=6, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX12.

```text
watch 1 1; call GiveItemFromFile ladder_item_1 ladder_item.ent; call UseLadderOnArea ladder_item_1 AreaAttachLadder; start PlayerStartArea_3; aim LadderArea_2; watch 1 0.5; interact; watch 1 0.5; w 3; watch 1 0.5; s 2; release; shot ladder_passage; health; start tunnel; w 1; shot tunnel_passage; health
```

Наблюдавшиеся voice-файлы: .

### 01_cells_ladder_attach_retry

ExitCode=1073741845; предупреждений=None, ошибок=None, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX12.

```text
watch 1 1; call GiveItemFromFile ladder_item_1 ladder_item.ent; call UseLadderOnArea ladder_item_1 AreaAttachLadder; start PlayerStartArea_3; aim LadderArea_2; interact; watch 1 0.25; w 3; watch 1 0.25; s 2; shot attached_ladder; health
```

ОТЧЁТ НЕ СФОРМИРОВАН: native Unity GPU crash при DX12 ReadPixels, ошибка устройства 887a0006; ExitCode=1073741845. W/E=None означает отсутствие итогового отчёта, не нулевые ошибки. Этот запуск НЕ PASS; аварийный лог сохранён, отдельный повтор DX11 указан ниже.

### 01_cells_moving_ladder_dx11

ExitCode=0; предупреждений=5, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX11 (-force-d3d11).

```text
watch 1 1; call RollDownLadder; watch 10 2; start PlayerStartArea_3; aim LadderArea_1; interact; watch 1 0.25; w 3; watch 1 0.25; s 2; shot moving_ladder; health
```

Наблюдавшиеся voice-файлы: .

### 01_cells_ladder_attach_retry_dx11

ExitCode=0; предупреждений=6, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX11 (-force-d3d11).

```text
watch 1 1; call GiveItemFromFile ladder_item_1 ladder_item.ent; call UseLadderOnArea ladder_item_1 AreaAttachLadder; start PlayerStartArea_3; aim LadderArea_2; interact; watch 1 0.25; w 3; watch 1 0.25; s 2; shot attached_ladder; health
```

Наблюдавшиеся voice-файлы: .

### Сообщения и диагностика

Все уникальные сообщения выполненных сценариев (время удалено, полные количества по каждому fixture выше):

- `AddKeyPart: skipped, needs credits (step 6, menus)`
- `Animation 'Idle' does not exist!`
- `Animation 'IdleExtra1' does not exist!`
- `Animation 'IdleExtra2' does not exist!`
- `Animation 'Walk' does not exist!`
- `Could not find use item callback 'LadderOnHatch'!`
- `Enemy 'enemy_suitor_1': no animations (Tools > RTXMnesia > Import > Enemies)`
- `Enemy 'enemy_suitor_2': no animations (Tools > RTXMnesia > Import > Enemies)`
- `Enemy 'enemy_suitor_3': no animations (Tools > RTXMnesia > Import > Enemies)`
- `Enemy 'enemy_suitor_4': no animations (Tools > RTXMnesia > Import > Enemies)`
- `InteractConnectPropWithMoveObject: skipped, needs interact connections`
- `InteractConnectPropWithRope: skipped, needs interact connections`
- `PlayPropAnimation: skipped, needs prop animations`
- `Script function 'CharacterTalkOver' takes 1 arguments, the engine passes 0`
- `WARNING: No prop 'body LadderArea_1'`
- `WARNING: No prop 'body padlock_2'`

Ручные call, выдача предметов, снятие callback/таймеров и задание globals — диагностическая подготовка, не доказательство естественного решения загадок. body LadderArea_1 в первом cells fixture ошибочен (area не prop); padlock_2 к концу phono-fixture уже изменён/разрушен. Полные сообщения сохранены.

## Геометрия и маршрут

Моделей 179, подмешей 307, prefabMatrixDifferences 18, scene failures 10.

`Logs/Justine_Audit_01_cells.txt/.log/.exit.txt`: Unity ExitCode=1, общий аудит НЕ PASS. Ошибок геометрии=0, отсутствующих кэшей=0, maxVertexError=0; выбор ресурсов 471 проверка / 0 провалов. Расхождения матриц префабов и scene failures приведены в отчёте; PLACEMENT ошибок нет.

Разбор: `Logs/Justine_RendererReview.json`. Точные имена отсутствующих для MeshFilter рендереров найдены как SkinnedMeshRenderer (cells 10, library 7, dungeon 14, final 1). В dungeon ещё два polySurfaceShape1: исходная .ent SubMeshID=0/2 и .dae разные geometry ID имеют одно отображаемое имя; Unity называет их polySurfaceShape1 1/2. Наличие/путь подтверждены сериализованными DAE GUID/fileID. Численного сравнения скелетных вершин этот аудит не выполняет; их геометрический PASS не заявляется.

Стартовые изображения всех четырёх карт просмотрены. Короткие движения из исходных PlayerStart и характерные виды указаны fixtures; это не полный маршрут. Реальные двери и перенос состояний проверяются отдельным Play-прогоном (см. общий README), заглушка StartCredits не считается проверенным финалом.

## Регрессии и файлы

Финальные регрессии: оба ExitCode=0, GPU/DX11, без -quit/-nographics; фактические строки:

```text
PASS: 66 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian, damage and death); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 19 main menu model checks (localization, controls, confirmation, preferences, live world, continue / load game, build scene order).
PASS: 23 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game, exit and save, continue, load game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

Отдельный реальный Play-маршрут Justine: `Logs/Justine_PlayRoute/Report.txt`, PASS: 57 / ExitCode=0; меню → 01 → исходные level doors → 02/03/04 → своё меню → новая попытка → летальный урон → выход из Play после английской записи. Проверены перенос предмета и результатов предыдущих испытаний, свои ресурсы/отсутствие основных рецептов, сброс таймеров/голоса/прогресса. Это маршрут загрузок с диагностическими флагами/предметом, а не полное пешее решение загадок. Ошибки подготовки первых Play fixtures, ограничение сохранений в DLC и пределы Application.Quit описаны в README.

Текущие собственные изменённые файлы дополнительно включают только пять сцен Justine в Windows build profile и одну строку этапа 5 AGENTS.md/CLAUDE.md. Настройки TimeManager/DynamicsManager восстановлены, временный helper удалён, предыдущие порты/тесты не менялись.

Исходные регрессии перед J0: MapChange 66 PASS, MainMenu 27 / 50 / 36 / 14 / 14 PASS (Logs/Justine_Baseline_*). Финальные результаты/актуальные числа — в README и Logs/Justine_Final_*.log; тесты проекта не менялись ради результата.

Собственные файлы карты: `JustineCells.cs`, его `.meta`, этот журнал; общая новая папка Justine/.meta. J0 перечислен отдельно. При возобновлении git status был чистым: ранее выполненная работа уже вошла в HEAD c4064fe вместе с пользовательским этапом сохранений. Предыдущие порты/их журналы/планы и новые сохранения здесь не правились.

Снимок рабочего дерева на момент генерации:

```text
M AGENTS.md
 M CLAUDE.md
 M "UnityProj/UMnesiaRTX/Assets/Settings/Build Profiles/Windows.asset"
 M docs/ScriptPort/justine/README.md
?? docs/ScriptPort/justine/01_cells.md
?? docs/ScriptPort/justine/02_library.md
?? docs/ScriptPort/justine/03_dungeon.md
?? docs/ScriptPort/justine/04_final.md
```

Дополнительная диагностика лестниц: attach_retry_dx11 и moving_ladder_dx11 завершились ExitCode=0, но игрок остался в Normal — подъём/спуск не подтверждены. Первый delayed interact потерял высоту с PlayerStartArea_3, native DX12 crash произошёл на ReadPixels и сохранён отдельно. Игровой движок и геометрия не менялись ради этой диагностики.
