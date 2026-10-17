# 04_final

Оригинал: `source/maps/ptest/04_final.hps` (2098 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Justine/JustineFinal.cs`.
Класс `RTXMnesia.Levels.Justine.JustineFinal`, атрибут `HplMapScript("maps/ptest/04_final.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/ptest/04_final.unity` (вне git).
Дата: 2026-10-17.
Статус: готов — полный скрипт перенесён, проверки выполнены; готовность порта не означает полного прохождения механик движка.

## Функции

| Оригинал (строка) | C# / сигнатура | Статус |
|---|---|---|
| `04_final.hps:14` | `JustineFinal.FadePlayerSpeed(float afNewSpeed, float afTime, int alSteps) → void` | перенесена |
| `04_final.hps:32` | `JustineFinal.Timer_FadePlayerSpeed(string asTimer) → void` | перенесена |
| `04_final.hps:50` | `JustineFinal.Timer_BlurVision(string asTimer) → void` | перенесена |
| `04_final.hps:71` | `JustineFinal.Timer_RestoreBlurVision(string asTimer) → void` | перенесена |
| `04_final.hps:89` | `JustineFinal.TimerRandomLook(string asTimer) → void` | перенесена |
| `04_final.hps:147` | `JustineFinal.CollideHeavyPanting(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:171` | `JustineFinal.TimerBreathSounds(string asTimer) → void` | перенесена |
| `04_final.hps:182` | `JustineFinal.CollideAreaAroundGrave(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:194` | `JustineFinal.LookAtGrave(int alGrave, int alState, string asEntity) → void` | перенесена |
| `04_final.hps:206` | `JustineFinal.CheckCorpseVanish() → void` | перенесена |
| `04_final.hps:238` | `JustineFinal.VanishEffect(string sCorpseName) → void` | перенесена |
| `04_final.hps:247` | `JustineFinal.LookAtGrave1(string entity, int alState) → void` | перенесена |
| `04_final.hps:252` | `JustineFinal.LookAtGrave2(string entity, int alState) → void` | перенесена |
| `04_final.hps:257` | `JustineFinal.LookAtGrave3(string entity, int alState) → void` | перенесена |
| `04_final.hps:270` | `JustineFinal.CollideDizzyMaze(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:299` | `JustineFinal.TimerRandomSteps(string asTimer) → void` | перенесена |
| `04_final.hps:316` | `JustineFinal.CollideCrushRoom(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:327` | `JustineFinal.Timer_CloseCrushRoom(string asTimer) → void` | перенесена |
| `04_final.hps:342` | `JustineFinal.Timer_CloseBarricade(string asTimer) → void` | перенесена |
| `04_final.hps:348` | `JustineFinal.Timer_LockBarricade(string asTimer) → void` | перенесена |
| `04_final.hps:358` | `JustineFinal.Timer_StartFrontCogwheelsRotation(string asTimer) → void` | перенесена |
| `04_final.hps:376` | `JustineFinal.Timer_ActivateDebrisCogwheelsCollision(string asTimer) → void` | перенесена |
| `04_final.hps:381` | `JustineFinal.Timer_StartBackCogwheelsRotation(string asTimer) → void` | перенесена |
| `04_final.hps:398` | `JustineFinal.Timer_StartWallsMovement(string asTimer) → void` | перенесена |
| `04_final.hps:407` | `JustineFinal.CollideCrushWallSafeArea(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:437` | `JustineFinal.Timer_StartCogwheelsReverse(string asTimer) → void` | перенесена |
| `04_final.hps:462` | `JustineFinal.Timer_RestoreCrushWalls(string asTimer) → void` | перенесена |
| `04_final.hps:468` | `JustineFinal.CollideCrushWallOpenArea(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:475` | `JustineFinal.CheckCrushWallsOpenState() → void` | перенесена |
| `04_final.hps:509` | `JustineFinal.CollideDebrisWithCogWheels(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:524` | `JustineFinal.CogwheelDamagePlayer(string asEntity) → void` | перенесена |
| `04_final.hps:533` | `JustineFinal.CollidePlayerCogWheels(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:539` | `JustineFinal.CollideWallSparks(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:554` | `JustineFinal.CollideStairs(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:578` | `JustineFinal.Timer_StairSteps(string asTimer) → void` | перенесена |
| `04_final.hps:605` | `JustineFinal.SetLastStairStep(int alStep, float afStepPeriod) → void` | перенесена |
| `04_final.hps:622` | `JustineFinal.LookAtStairStep(string asEntity, int alState) → void` | перенесена |
| `04_final.hps:631` | `JustineFinal.CollideStairsSteps2(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:636` | `JustineFinal.CollideStairsSteps3(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:641` | `JustineFinal.CollideStairsSteps4(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:646` | `JustineFinal.CollideStairsSteps5(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:659` | `JustineFinal.CollideMansionHall(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:670` | `JustineFinal.Timer_EndingCloseDoor(string asTimer) → void` | перенесена |
| `04_final.hps:687` | `JustineFinal.Timer_EndingStartDialogue(string asTimer) → void` | перенесена |
| `04_final.hps:702` | `JustineFinal.LookAroundMansion() → void` | перенесена |
| `04_final.hps:712` | `JustineFinal.DialogueEndCallback() → void` | перенесена |
| `04_final.hps:730` | `JustineFinal.StateChangePhono01(string asEntity, int alState) → void` | перенесена |
| `04_final.hps:737` | `JustineFinal.StateChangePhono02(string asEntity, int alState) → void` | перенесена |
| `04_final.hps:744` | `JustineFinal.StateChangePhono03(string asEntity, int alState) → void` | перенесена |
| `04_final.hps:751` | `JustineFinal.CollideAreaStopPhono(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:758` | `JustineFinal.PlayPhono(int iLevel, int iSet, int iPart, string sEntity) → void` | перенесена |
| `04_final.hps:774` | `JustineFinal.VoiceOverPhono() → void` | перенесена |
| `04_final.hps:790` | `JustineFinal.CollideCharacterStartTalk(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:805` | `JustineFinal.CollideCharacterStopTalk(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:819` | `JustineFinal.GetCharacterTopicNotSpoken(string asTopic) → bool` | перенесена |
| `04_final.hps:825` | `JustineFinal.SetCharacterTopicSpoken(string asTopic) → void` | перенесена |
| `04_final.hps:831` | `JustineFinal.SetCharacterTopicNotSpoken(string asTopic) → void` | перенесена |
| `04_final.hps:837` | `JustineFinal.PlayCharacterVoice(string asTopic, int alNumOfParts, bool abRandom, bool abSetAsSpoken, bool abForceNewTopic) → void` | перенесена |
| `04_final.hps:914` | `JustineFinal.CharacterTalkOver(float fStopVoiceFade) → void` | перенесена |
| `04_final.hps:925` | `JustineFinal.TimerDelayIdleVoice(string asTimer) → void` | перенесена |
| `04_final.hps:933` | `JustineFinal.StartCharacterSpeak(string sTopicName, int iNr, bool abRandom,  bool abSetAsSpoken, bool abForceNewTopic) → void` | перенесена |
| `04_final.hps:957` | `JustineFinal.StopCharacterSpeak() → void` | перенесена |
| `04_final.hps:978` | `JustineFinal.JustinePassOut() → void` | перенесена |
| `04_final.hps:983` | `JustineFinal.TimerEPassOut(string asTimer) → void` | перенесена |
| `04_final.hps:1199` | `JustineFinal.ChangePlayerSpeed(float fSpeed) → void` | перенесена |
| `04_final.hps:1208` | `JustineFinal.CollideAreaPlayerGotUp(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1232` | `JustineFinal.TimerBangOnDoor(string asTimer) → void` | перенесена |
| `04_final.hps:1238` | `JustineFinal.BangOnDoor() → void` | перенесена |
| `04_final.hps:1257` | `JustineFinal.BeginPrisonerVoices() → void` | перенесена |
| `04_final.hps:1329` | `JustineFinal.CollideAreaPushDownBar(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1353` | `JustineFinal.CollideAreaLockBar(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1365` | `JustineFinal.CollideAreaCheckFirstDoor(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1383` | `JustineFinal.JustineAutoCloseBar() → void` | перенесена |
| `04_final.hps:1398` | `JustineFinal.TimerJustineLockBar(string asTimer) → void` | перенесена |
| `04_final.hps:1419` | `JustineFinal.TimerJustineLockBarEnd(string asTimer) → void` | перенесена |
| `04_final.hps:1428` | `JustineFinal.TimerMoveForward(string asTimer) → void` | перенесена |
| `04_final.hps:1436` | `JustineFinal.Interactbarreddoor(string asEntity) → void` | перенесена |
| `04_final.hps:1463` | `JustineFinal.TimerLookAtBar(string asTimer) → void` | перенесена |
| `04_final.hps:1481` | `JustineFinal.CollideAreaDoor2ndVisit(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1493` | `JustineFinal.TimerAddCallback(string asTimer) → void` | перенесена |
| `04_final.hps:1499` | `JustineFinal.CollideAreaDoor2ndVisit02(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1516` | `JustineFinal.LookAtDoorArea01(string asEntity, int alState) → void` | перенесена |
| `04_final.hps:1523` | `JustineFinal.LookAtMachine(string asEntity, int alState) → void` | перенесена |
| `04_final.hps:1533` | `JustineFinal.LookAtDoll(string asEntity, int alState) → void` | перенесена |
| `04_final.hps:1543` | `JustineFinal.LookAtCeiling(string asEntity, int alState) → void` | перенесена |
| `04_final.hps:1553` | `JustineFinal.TimerSpeakObject(string asTimer) → void` | перенесена |
| `04_final.hps:1573` | `JustineFinal.CollideCrushRoomCheckDoor(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1581` | `JustineFinal.CollideAreaDoorLock(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1607` | `JustineFinal.CollideAreaCheckSecondDoorLocked(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1616` | `JustineFinal.JustinAutoCloseDoor() → void` | перенесена |
| `04_final.hps:1631` | `JustineFinal.TimerJustineLockDoor(string asTimer) → void` | перенесена |
| `04_final.hps:1661` | `JustineFinal.TimerJustineLockDoorEnd(string asTimer) → void` | перенесена |
| `04_final.hps:1673` | `JustineFinal.CollideAreaClarice_1(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1753` | `JustineFinal.TimerEEnding(string asTimer) → void` | перенесена |
| `04_final.hps:1840` | `JustineFinal.TimerMoveEnding(string asTimer) → void` | перенесена |
| `04_final.hps:1847` | `JustineFinal.CollideAreaUpstairs(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1858` | `JustineFinal.TimerUpEnding(string asTimer) → void` | перенесена |
| `04_final.hps:1893` | `JustineFinal.CollideAreaShake(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1906` | `JustineFinal.TimerShake(string asTimer) → void` | перенесена |
| `04_final.hps:1918` | `JustineFinal.InteractTouchGrave(string asEntity) → void` | перенесена |
| `04_final.hps:1922` | `JustineFinal.InteractTouchAngel(string asEntity) → void` | перенесена |
| `04_final.hps:1929` | `JustineFinal.PickUpNote(string asEntity, string asType) → void` | перенесена |
| `04_final.hps:1938` | `JustineFinal.CollideDollChainSounds(string asParent, string asChild, int alState) → void` | перенесена |
| `04_final.hps:1952` | `JustineFinal.OnStart() → void` | перенесена |

Всего 104 функций, включая неиспользуемые, отладочные и недостижимые участки.

## Поля, массивы, константы и enum

| Исходная строка | Данные (исходные значения и порядок) |
|---|---|
| `04_final.hps:9` | `gfCurrentSpeed` — float; начальное значение `1.0f` |
| `04_final.hps:10` | `gfDestSpeed` — float; начальное значение 0 |
| `04_final.hps:11` | `gfSpeedStep` — float; начальное значение 0 |
| `04_final.hps:12` | `gfTimeStep` — float; начальное значение 0 |
| `04_final.hps:49` | `gbStopBlur` — bool; начальное значение `false` |
| `04_final.hps:83` | `gbStopHeadSway` — bool; начальное значение `false` |
| `04_final.hps:84` | `bRoll` — bool; начальное значение `true` |
| `04_final.hps:85` | `gfMinHeadSway` — float; начальное значение `8.0f` |
| `04_final.hps:86` | `gfMaxHeadSway` — float; начальное значение `20.0f` |
| `04_final.hps:574` | `gbStepsActive` — bool; начальное значение `false` |
| `04_final.hps:575` | `glCurrentStep` — int; начальное значение `1` |
| `04_final.hps:576` | `glLastStairStep` — int; начальное значение `0` |
| `04_final.hps:577` | `gfStepPeriod` — float; начальное значение `1.0f` |

Локальный массив `aEndSuitor` (`04_final.hps:1304`, BeginPrisonerVoices): 6 звуков в порядке alois_idle_03, alois_attack_01, basile_giveup_02, basile_idle_01, malo_attack_02, malo_idle_01. Первый выбор RandInt 0…2, второй 3…5.

## Синтаксис и исходные особенности

Ручной перенос сохраняет имена/порядок функций и операций, поля, условия, циклы, строки и английские комментарии. Только существующие lifecycle-функции стали public override; string &in/& → string, статические callbacks → nameof, десятичные литералы → float, and/or/not → C#-операторы. Локальная string без инициализации получает пустую строку, как в AngelScript. AddKeyPart с верхним битом — unchecked((int)0x…), все 32 бита/порядок сохранены.

Независимая сверка 26 891 токена и 980 исходных комментариев во всех четырёх портах: `Logs/Justine_TokenReview.txt`; фактические .cs дополнительно сравниваются с reviewed drafts. Компиляция: `Logs/Justine_Compile.txt`, затем Unity перед каждым запуском. Исходные неиспользуемые/недостижимые участки вызывают CS0162/CS0219/CS0169 и не удалялись.

- `04_final.hps:876`: Сохранён исходный voice callback с несовместимой сигнатурой float/0 аргументов.
- `04_final.hps:1249`: Проверяются все 8 свежих комбинаций Hector/Victor/Felix. Исходный приоритет &&/|| в средней реплике Victor сохранён: 110 проигрывает victor_saved_02, хотя VictorIsDead=1.
- `04_final.hps:1988`: В .map три могильных тела используют graveyard_corpse01/02/03.ent типа Object/Static, а скрипт обращается к Enemy. Это подтверждённая несовместимость исходных данных; generic graveyard_corpse.ent имеет Enemy_Grunt, но карта на него не ссылается.
- `04_final.hps:2001`: Циклы регистрируют 96 AreaSpark_N, но карта содержит только 46 этих областей: 50 предупреждений о пропущенных номерах являются исходными ссылками, не потерей импорта.
- `04_final.hps:2041`: 16 имён torch_static01_N из исходного цикла отсутствуют среди сущностей .map. Общий torch_static01.ent действительно содержит Sound_1; отсутствие конкретных экземпляров подтверждено отдельно.
- `04_final.hps:717`: Вызов сохранён, остаётся NotPorted. Два ending-fixture снимают Player→AreaUpstairs и завершаются до титров; настоящее завершение с титрами не заявляется.
- `04_final.hps:68`: Явное усечение float→int сохранено; отладочные строки gf* форматируются Str/G6 invariant. Недостижимый код после исходных return оставлен.
- `04_final.hps:978`: crush_passout принудительно вызывает обработчики, сохранив исходные collide registrations: повторные Area_CrushRoom/AreaCheckFirstDoor — следствие диагностической подготовки; это не установленный дефект естественного игрового пути.

## API и пределы реализации

Подготовка профиля/режима смерти выполнена отдельно в J0: [README.md](README.md). Русские тексты DLC и английская озвучка lang/eng/ptest; основные рецепты отсутствуют.

| API | Вызовов в .hps | Фактическое объявление / статус |
|---|---:|---|
| `AddDebugMessage` | 35 | `AddDebugMessage(string asString, bool abCheckForDuplicates)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:14`) |
| `AddEffectVoice` | 35 | `AddEffectVoice(string asVoiceFile, string asEffectFile, string asTextCat, string asTextEntry, bool abUsePostion,                                       string asPosEnitity, float afMinDistance, float afMaxDistance)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:31`) |
| `AddEntityCollideCallback` | 39 | `AddEntityCollideCallback(string asParentName, string asChildName, string asFunction, bool abDeleteOnCollide, int alStates)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:205`) |
| `AddKeyPart` | 4 | `AddKeyPart(int alKeyPart)` — ✓ с 2026-10-17 (CLAUDE.md «Титры, NPC, анимации и прикреплённые пропы») |
| `AddLocalVarInt` | 4 | `AddLocalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:58`) |
| `AddPropImpulse` | 9 | `AddPropImpulse(string asName, float afX, float afY, float afZ, string asCoordSystem)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:424`) |
| `AddTimer` | 61 | `AddTimer(string asName, float afTime, string asFunction)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:44`) |
| `ChangePlayerStateToNormal` | 1 | `ChangePlayerStateToNormal()` — empty (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:12`) |
| `CreateParticleSystemAtEntity` | 5 | `CreateParticleSystemAtEntity(string asPSName, string asPSFile, string asEntity, bool abSavePS)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:165`) |
| `FadeGlobalSoundSpeed` | 6 | `FadeGlobalSoundSpeed(float afDestSpeed, float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:150`) |
| `FadeImageTrailTo` | 3 | `FadeImageTrailTo(float afAmount, float afSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:12`) |
| `FadeIn` | 6 | `FadeIn(float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:8`) |
| `FadeOut` | 8 | `FadeOut(float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:10`) |
| `FadePlayerFOVMulTo` | 3 | `FadePlayerFOVMulTo(float afX, float afSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:91`) |
| `FadePlayerRollTo` | 8 | `FadePlayerRollTo(float afX, float afSpeedMul, float afMaxSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:96`) |
| `FadeRadialBlurTo` | 4 | `FadeRadialBlurTo(float afSize, float afSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:16`) |
| `GetEffectVoiceActive` | 1 | `GetEffectVoiceActive()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:46`) |
| `GetEntitiesCollide` | 4 | `GetEntitiesCollide(string asEntityA, string asEntityB)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:196`) |
| `GetFlashbackIsActive` | 1 | `GetFlashbackIsActive()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:53`) |
| `GetGlobalVarInt` | 27 | `GetGlobalVarInt(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:96`) |
| `GetLocalVarInt` | 24 | `GetLocalVarInt(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:66`) |
| `GetLocalVarString` | 3 | `GetLocalVarString(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:78`) |
| `GetSwingDoorClosed` | 1 | `GetSwingDoorClosed(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:303`) |
| `GetSwingDoorLocked` | 1 | `GetSwingDoorLocked(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:300`) |
| `GivePlayerDamage` | 1 | `GivePlayerDamage(float afAmount, string asType, bool abSpinHead, bool abLethal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:82`) |
| `GiveSanityBoost` | 1 | `GiveSanityBoost()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:57`) |
| `MovePlayerForward` | 2 | `MovePlayerForward(float afAmount)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:48`) |
| `MovePlayerHeadPos` | 2 | `MovePlayerHeadPos(float afX, float afY, float afZ, float afSpeed, float afSlowDownDist)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:103`) |
| `PlayGuiSound` | 19 | `PlayGuiSound(string asSoundFile, float afVolume)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:73`) |
| `PlayMusic` | 5 | `PlayMusic(string asMusicFile, bool abLoop, float afVolume, float afFadeTime, int alPrio, bool abResume)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:137`) |
| `PlaySoundAtEntity` | 18 | `PlaySoundAtEntity(string asSoundName, string asSoundFile, string asEntity, float afFadeSpeed, bool abSaveSound)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:87`) |
| `PreloadSound` | 17 | `PreloadSound(string asSoundFile)` — empty (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:80`) |
| `RandFloat` | 28 | `RandFloat(float afMin, float afMax)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:26`) |
| `RandInt` | 8 | `RandInt(int alMin, int alMax)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:28`) |
| `RemoveEntityCollideCallback` | 3 | `RemoveEntityCollideCallback(string asParentName, string asChildName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:217`) |
| `RemoveTimer` | 9 | `RemoveTimer(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:46`) |
| `RotatePropToSpeed` | 40 | `RotatePropToSpeed(string asName, float afAcc, float afGoalSpeed, float afAxisX, float afAxisY, float afAxisZ,                                          bool abResetSpeed, string asOffsetArea)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:249`) |
| `ScriptDebugOn` | 1 | `ScriptDebugOn()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:24`) |
| `SetEffectVoiceOverCallback` | 7 | `SetEffectVoiceOverCallback(string asFunc)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:48`) |
| `SetEnemyDisabled` | 4 | `SetEnemyDisabled(string asName, bool abDisabled)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:524`) |
| `SetEnemyIsHallucination` | 1 | `SetEnemyIsHallucination(string asName, bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:529`) |
| `SetEntityActive` | 26 | `SetEntityActive(string asName, bool abActive)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:58`) |
| `SetEntityInteractionDisabled` | 7 | `SetEntityInteractionDisabled(string asName, bool abDisabled)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:191`) |
| `SetEntityPlayerInteractCallback` | 2 | `SetEntityPlayerInteractCallback(string asName, string asCallback, bool abRemoveOnInteraction)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:176`) |
| `SetEntityPlayerLookAtCallback` | 4 | `SetEntityPlayerLookAtCallback(string asName, string asCallback, bool abRemoveWhenLookedAt)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:170`) |
| `SetGlobalVarInt` | 4 | `SetGlobalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:82`) |
| `SetInventoryDisabled` | 7 | `SetInventoryDisabled(bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:63`) |
| `SetLanternActive` | 1 | `SetLanternActive(bool abX, bool abUseEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:145`) |
| `SetLocalVarInt` | 24 | `SetLocalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:52`) |
| `SetMapDisplayNameEntry` | 1 | `SetMapDisplayNameEntry(string asNameEntry)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:138`) |
| `SetMoveObjectState` | 1 | `SetMoveObjectState(string asName, float afState)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:397`) |
| `SetMoveObjectStateExt` | 4 | `SetMoveObjectStateExt(string asName, float afState, float afAcc, float afMaxSpeed, float afSlowdownDist, bool abResetSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:402`) |
| `SetPlayerActive` | 6 | `SetPlayerActive(bool abActive)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:10`) |
| `SetPlayerCrouchDisabled` | 8 | `SetPlayerCrouchDisabled(bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:131`) |
| `SetPlayerCrouching` | 4 | `SetPlayerCrouching(bool abCrouch)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:14`) |
| `SetPlayerJumpDisabled` | 8 | `SetPlayerJumpDisabled(bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:130`) |
| `SetPlayerLookSpeedMul` | 13 | `SetPlayerLookSpeedMul(float afMul)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:128`) |
| `SetPlayerMoveSpeedMul` | 8 | `SetPlayerMoveSpeedMul(float afMul)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:126`) |
| `SetPlayerRunSpeedMul` | 14 | `SetPlayerRunSpeedMul(float afMul)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:127`) |
| `SetPropActiveAndFade` | 1 | `SetPropActiveAndFade(string asName, bool abActive, float afFadeTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:236`) |
| `SetRadialBlurStartDist` | 2 | `SetRadialBlurStartDist(float afStartDist)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:18`) |
| `SetSwingDoorClosed` | 5 | `SetSwingDoorClosed(string asName, bool abClosed, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:290`) |
| `SetSwingDoorLocked` | 5 | `SetSwingDoorLocked(string asName, bool abLocked, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:285`) |
| `StartCredits` | 2 | `StartCredits(string asMusic, bool abLoopMusic, string asTextCat, string asTextEntry, int alEndNum)` — ✓ с 2026-10-17 (CLAUDE.md «Титры, NPC, анимации и прикреплённые пропы») |
| `StartEffectEmotionFlash` | 2 | `StartEffectEmotionFlash(string asTextCat, string asTextEntry, string asSound)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:23`) |
| `StartPlayerLookAt` | 32 | `StartPlayerLookAt(string asEntityName, float afSpeedMul, float afMaxSpeed, string asAtTargetCallback)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:107`) |
| `StartScreenShake` | 5 | `StartScreenShake(float afAmount, float afTime, float afFadeInTime, float afFadeOutTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:50`) |
| `StopAllEffectVoices` | 11 | `StopAllEffectVoices(float afFadeOutTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:44`) |
| `StopMusic` | 2 | `StopMusic(float afFadeTime, int alPrio)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:140`) |
| `StopPlayerLookAt` | 6 | `StopPlayerLookAt()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:124`) |
| `StopSound` | 12 | `StopSound(string asSoundName, float afFadeTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:123`) |
| `TeleportPlayer` | 1 | `TeleportPlayer(string asStartPosName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:134`) |
| `UnlockAchievement` | 3 | `UnlockAchievement(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:146`) |

Статус implemented означает наличие реализации, а не полную совместимость механики. Suitor-модели/параметры импортированы, но EnemyConverter не включает их отдельные анимации из entities/ptest; патрули/активация не доказывают полную погоню и атаки. Interact connections, rope, Sticky/prop animation и титры/ARG остаются явными ограничениями. Пустые API в таблице отмечены отдельно и не выдаются за реализованные.

## Импорт и статическая сверка

Импорт: `Logs/Justine_Import_04_final.txt` и одноимённый .log, Unity ExitCode=0, 0 ошибок. Числа/виды предупреждений зафиксированы в отчёте импорта. Исторические D:/SVN и E:/amnesia переходы не переписаны.

Штатная команда: `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -MapRoot maps/ptest -Map 04_final -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Justine/JustineFinal.cs`. Отчёт `Logs/Justine_Check_Final_04_final.txt`: требование 0 ошибок; отсутствующие исходные callbacks не подменяются фиктивными функциями.


Фактически импорт: 8 предупреждений, 0 ошибок. Предупреждение No level script port относится к импорту до установки нового класса; последующие запуски уже используют порт. FogArea пока пропускается; missing child отражает привязки скелетных узлов, а не численный PASS геометрии. gui_amn_mm_textbox_marker.tga — неиспользуемая данным меню ссылка skin.

| Предупреждение импорта | Количество |
|---|---:|
| 'FogArea' is not supported yet (skipped) | 1 |
| entities/ptest/ceiling_doll/ceiling_doll.ent: could not find child with unique ID 4 to attach to body 'Body_torso' | 1 |
| entities/ptest/ceiling_doll/ceiling_doll.ent: could not find child with unique ID 3 to attach to body 'Body_tighs' | 1 |
| entities/ptest/ceiling_doll/ceiling_doll.ent: could not find child with unique ID 5 to attach to body 'Body_head' | 1 |
| entities/ptest/ceiling_doll/ceiling_doll.ent: could not find child with unique ID 1 to attach to body 'Body_base' | 1 |
| entities/ptest/ceiling_doll/ceiling_doll.ent: could not find child with unique ID 2 to attach to body 'Body_legs_and_chains' | 1 |
| Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga | 1 |
| No level script port for maps/ptest/04_final.map yet (the level runs without its events) | 1 |

## Сценарии

Все ниже: seed=1, каждый сценарий в свежем Unity-процессе, PlayerSimulation.RunBatch, GPU (без -nographics), -batchmode -quit, исходная карта maps/ptest. Логи в UnityProj/UMnesiaRTX/Logs, имя Justine_<fixture> .log/.txt/.scenario.txt/.exit.txt. W/E — фактические строки отчёта, дополнительно проверены вложенные [Script] ERROR и исключения. ExitCode=0 сам по себе не означает чистый PASS.

### 04_final_start

ExitCode=0; предупреждений=69, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 30 5; shot start; w 1; watch 2 1; shot walked; health
```

Наблюдавшиеся voice-файлы: .

### 04_final_graves_maze

ExitCode=0; предупреждений=70, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=1;VictorIsDead=1;FelixIsDead=1`; scriptDebug=False; DX12.

```text
watch 1 1; call CollideHeavyPanting Player Area_FathersGrave 1; watch 8 1; call CollideHeavyPanting Player Area_FathersGrave -1; call CollideAreaAroundGrave Player Area_AroundGrave_1 1; call LookAtGrave1 Area_Grave_1 1; call CollideAreaAroundGrave Player Area_AroundGrave_2 1; call LookAtGrave2 Area_Grave_2 1; call CollideAreaAroundGrave Player Area_AroundGrave_3 1; call LookAtGrave3 Area_Grave_3 1; call CollideDizzyMaze Player Area_DizzyMaze 1; watch 20 2; call CollideDizzyMaze Player Area_DizzyMaze -1; watch 15 3; call InteractTouchGrave grave_1; watch 15 3; start PlayerStartArea_2; shot graveyard
```

Наблюдавшиеся voice-файлы: .

### 04_final_crush_passout

ExitCode=0; предупреждений=71, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=0;VictorIsDead=0;FelixIsDead=0`; scriptDebug=False; DX12.

```text
watch 1 1; call CollideCrushRoom Player Area_CrushRoom 1; watch 70 5; health; call GetLocalVarInt TimerEPassOut; body crush_wall_1; body crush_wall_2; shot crushing; call JustinePassOut; watch 65 5; call CollideAreaPlayerGotUp Player AreaPlayerGotUp 1; watch 30 3; call GetLocalVarInt JustineWokeUp; health; shot awakened
```

Возвращённые значения: `GetLocalVarInt TimerEPassOut` → `1`; `GetLocalVarInt JustineWokeUp` → `1`.

Наблюдавшиеся voice-файлы: `justine_rec_lvl4_02_01.ogg`, `justine_end_03`, `justine_end_04`, `hector_saved_02`, `felix_saved_01`.

### 04_final_doors

ExitCode=0; предупреждений=69, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 1 1; call CollideAreaPlayerGotUp Player AreaPlayerGotUp 1; watch 10 2; call CollideAreaCheckFirstDoor Player AreaCheckFirstDoor -1; watch 10 2; call Interactbarreddoor castle_5; watch 6 1; call LookAtMachine AreaVoiceMachinery 1; watch 10 2; call LookAtDoll AreaVoiceDoll 1; watch 10 2; call LookAtCeiling AreaVoiceCeiling_1 1; watch 10 2; call CollideAreaDoorLock castle_6 AreaDoorLock -1; call CollideAreaCheckSecondDoorLocked Player AreaCheckSecondDoorLocked 1; watch 8 1; body castle_6; health
```

Наблюдавшиеся voice-файлы: `victor_saved_01`, `hector_saved_02`, `felix_saved_01`, `justine_end_door_01`, `justine_end_door_02`, `justine_end_look_02`, `justine_end_look_01`, `justine_end_look_04`, `justine_end_door_04`.

### 04_final_voices_000

ExitCode=0; предупреждений=69, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=0;VictorIsDead=0;FelixIsDead=0`; scriptDebug=False; DX12.

```text
watch 1 1; call BeginPrisonerVoices; watch 70 1; sounds; health
```

Наблюдавшиеся voice-файлы: `victor_saved_01`, `hector_saved_02`, `felix_saved_01`.

### 04_final_voices_001

ExitCode=0; предупреждений=70, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=0;VictorIsDead=0;FelixIsDead=1`; scriptDebug=False; DX12.

```text
watch 1 1; call BeginPrisonerVoices; watch 70 1; sounds; health
```

Наблюдавшиеся voice-файлы: `victor_saved_02`, `hector_saved_02`.

### 04_final_voices_010

ExitCode=0; предупреждений=69, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=0;VictorIsDead=1;FelixIsDead=0`; scriptDebug=False; DX12.

```text
watch 1 1; call BeginPrisonerVoices; watch 70 1; sounds; health
```

Наблюдавшиеся voice-файлы: `hector_saved_02`, `felix_saved_01`.

### 04_final_voices_011

ExitCode=0; предупреждений=70, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=0;VictorIsDead=1;FelixIsDead=1`; scriptDebug=False; DX12.

```text
watch 1 1; call BeginPrisonerVoices; watch 70 1; sounds; health
```

Наблюдавшиеся voice-файлы: `hector_saved_02`.

### 04_final_voices_100

ExitCode=0; предупреждений=69, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=1;VictorIsDead=0;FelixIsDead=0`; scriptDebug=False; DX12.

```text
watch 1 1; call BeginPrisonerVoices; watch 70 1; sounds; health
```

Наблюдавшиеся voice-файлы: `victor_saved_02`, `felix_saved_01`.

### 04_final_voices_101

ExitCode=0; предупреждений=70, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=1;VictorIsDead=0;FelixIsDead=1`; scriptDebug=False; DX12.

```text
watch 1 1; call BeginPrisonerVoices; watch 70 1; sounds; health
```

Наблюдавшиеся voice-файлы: `victor_saved_03`.

### 04_final_voices_110

ExitCode=0; предупреждений=69, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=1;VictorIsDead=1;FelixIsDead=0`; scriptDebug=False; DX12.

```text
watch 1 1; call BeginPrisonerVoices; watch 70 1; sounds; health
```

Наблюдавшиеся voice-файлы: `victor_saved_02`, `felix_saved_01`.

### 04_final_voices_111

ExitCode=0; предупреждений=70, ошибок=0, исключений C# в логе=0.
Начальные globals: `HectorIsDead=1;VictorIsDead=1;FelixIsDead=1`; scriptDebug=False; DX12.

```text
watch 1 1; call BeginPrisonerVoices; watch 70 1; sounds; health
```

Наблюдавшиеся voice-файлы: `alois_attack_01`, `malo_idle_01`.

### 04_final_ending_0

ExitCode=0; предупреждений=69, ошибок=0, исключений C# в логе=0.
Начальные globals: `HasAptNote=0;HectorIsDead=0;VictorIsDead=0;FelixIsDead=0`; scriptDebug=False; DX12.

```text
watch 1 1; call RemoveEntityCollideCallback Player AreaUpstairs; call CollideAreaClarice_1 Player AreaClarice_1 1; watch 80 2; call RemoveTimer move; call RemoveTimer TimerEEnding; call GetLocalVarInt TimerEEnding; sounds; health; shot ending
```

Возвращённые значения: `GetLocalVarInt TimerEEnding` → `12`.

Наблюдавшиеся voice-файлы: `clarice_end_01`, `justine_end_05`, `justine_end_06`, `clarice_end_02`, `clarice_end_proper_01`, `justine_end_proper_01`, `clarice_end_proper_02`, `justine_end_proper_02`.

### 04_final_ending_1

ExitCode=0; предупреждений=69, ошибок=0, исключений C# в логе=0.
Начальные globals: `HasAptNote=1;HectorIsDead=0;VictorIsDead=0;FelixIsDead=0`; scriptDebug=False; DX12.

```text
watch 1 1; call RemoveEntityCollideCallback Player AreaUpstairs; call CollideAreaClarice_1 Player AreaClarice_1 1; watch 80 2; call RemoveTimer move; call RemoveTimer TimerEEnding; call GetLocalVarInt TimerEEnding; sounds; health; shot ending
```

Возвращённые значения: `GetLocalVarInt TimerEEnding` → `12`.

Наблюдавшиеся voice-файлы: `clarice_end_01`, `justine_end_05`, `justine_end_06`, `clarice_end_02`, `justine_end_patrol_01`, `clarice_end_patrol_01`, `justine_end_patrol_02`.

### 04_final_phonographs

ExitCode=0; предупреждений=69, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 1 1; call SetWheelStuckState phonograph_1 1 true; watch 130 2; sounds; call SetWheelStuckState phonograph_2 1 true; watch 140 2; sounds; start PlayerStartArea_2; w 1; shot final_passage; health
```

Наблюдавшиеся voice-файлы: `justine_rec_lvl4_01_01.ogg`, `justine_rec_lvl4_01_02.ogg`, `justine_rec_lvl4_01_03.ogg`, `justine_rec_lvl4_01_04.ogg`, `justine_rec_lvl4_01_05.ogg`, `justine_rec_lvl4_01_06.ogg`, `justine_rec_lvl3_02_01.ogg`, `justine_rec_lvl3_02_02.ogg`, `justine_rec_lvl3_02_03.ogg`.

### Сообщения и диагностика

Все уникальные сообщения выполненных сценариев (время удалено, полные количества по каждому fixture выше):

- `A callback with entity 'AreaCheckFirstDoor' already exists!`
- `A callback with entity 'Area_CrushRoom' already exists!`
- `AddKeyPart: skipped, needs credits (step 6, menus)`
- `ERROR: Could not find sound entity 'torch_static01_12_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_16_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_18_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_19_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_20_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_29_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_30_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_31_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_32_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_3_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_4_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_5_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_6_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_7_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_8_Sound_1'`
- `ERROR: Could not find sound entity 'torch_static01_9_Sound_1'`
- `Entity 'AreaSpark_11' does not exist!`
- `Entity 'AreaSpark_12' does not exist!`
- `Entity 'AreaSpark_13' does not exist!`
- `Entity 'AreaSpark_14' does not exist!`
- `Entity 'AreaSpark_19' does not exist!`
- `Entity 'AreaSpark_2' does not exist!`
- `Entity 'AreaSpark_20' does not exist!`
- `Entity 'AreaSpark_21' does not exist!`
- `Entity 'AreaSpark_22' does not exist!`
- `Entity 'AreaSpark_27' does not exist!`
- `Entity 'AreaSpark_28' does not exist!`
- `Entity 'AreaSpark_29' does not exist!`
- `Entity 'AreaSpark_3' does not exist!`
- `Entity 'AreaSpark_30' does not exist!`
- `Entity 'AreaSpark_34' does not exist!`
- `Entity 'AreaSpark_35' does not exist!`
- `Entity 'AreaSpark_37' does not exist!`
- `Entity 'AreaSpark_40' does not exist!`
- `Entity 'AreaSpark_42' does not exist!`
- `Entity 'AreaSpark_43' does not exist!`
- `Entity 'AreaSpark_45' does not exist!`
- `Entity 'AreaSpark_47' does not exist!`
- `Entity 'AreaSpark_49' does not exist!`
- `Entity 'AreaSpark_5' does not exist!`
- `Entity 'AreaSpark_50' does not exist!`
- `Entity 'AreaSpark_54' does not exist!`
- `Entity 'AreaSpark_56' does not exist!`
- `Entity 'AreaSpark_57' does not exist!`
- `Entity 'AreaSpark_59' does not exist!`
- `Entity 'AreaSpark_6' does not exist!`
- `Entity 'AreaSpark_62' does not exist!`
- `Entity 'AreaSpark_64' does not exist!`
- `Entity 'AreaSpark_65' does not exist!`
- `Entity 'AreaSpark_67' does not exist!`
- `Entity 'AreaSpark_71' does not exist!`
- `Entity 'AreaSpark_72' does not exist!`
- `Entity 'AreaSpark_73' does not exist!`
- `Entity 'AreaSpark_74' does not exist!`
- `Entity 'AreaSpark_79' does not exist!`
- `Entity 'AreaSpark_80' does not exist!`
- `Entity 'AreaSpark_81' does not exist!`
- `Entity 'AreaSpark_82' does not exist!`
- `Entity 'AreaSpark_87' does not exist!`
- `Entity 'AreaSpark_88' does not exist!`
- `Entity 'AreaSpark_89' does not exist!`
- `Entity 'AreaSpark_9' does not exist!`
- `Entity 'AreaSpark_91' does not exist!`
- `Entity 'AreaSpark_92' does not exist!`
- `Entity 'AreaSpark_94' does not exist!`
- `Entity 'AreaSpark_96' does not exist!`
- `Entity 'graveyard_corpse_1' with type Enemy and subtype any does not exist!`
- `Entity 'graveyard_corpse_2' with type Enemy and subtype any does not exist!`
- `Entity 'graveyard_corpse_3' with type Enemy and subtype any does not exist!`

Ручные call, выдача предметов, снятие callback/таймеров и задание globals — диагностическая подготовка, не доказательство естественного решения загадок. body LadderArea_1 в первом cells fixture ошибочен (area не prop); padlock_2 к концу phono-fixture уже изменён/разрушен. Полные сообщения сохранены.

## Геометрия и маршрут

Моделей 168, подмешей 250, prefabMatrixDifferences 19, scene failures 1.

`Logs/Justine_Audit_04_final.txt/.log/.exit.txt`: Unity ExitCode=1, общий аудит НЕ PASS. Ошибок геометрии=0, отсутствующих кэшей=0, maxVertexError=0; выбор ресурсов 471 проверка / 0 провалов. Расхождения матриц префабов и scene failures приведены в отчёте; PLACEMENT ошибок нет.

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

Собственные файлы карты: `JustineFinal.cs`, его `.meta`, этот журнал; общая новая папка Justine/.meta. J0 перечислен отдельно. При возобновлении git status был чистым: ранее выполненная работа уже вошла в HEAD c4064fe вместе с пользовательским этапом сохранений. Предыдущие порты/их журналы/планы и новые сохранения здесь не правились.

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
