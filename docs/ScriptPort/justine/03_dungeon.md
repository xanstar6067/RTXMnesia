# 03_dungeon

Оригинал: `source/maps/ptest/03_dungeon.hps` (1757 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Justine/JustineDungeon.cs`.
Класс `RTXMnesia.Levels.Justine.JustineDungeon`, атрибут `HplMapScript("maps/ptest/03_dungeon.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/ptest/03_dungeon.unity` (вне git).
Дата: 2026-10-17.
Статус: готов — полный скрипт перенесён, проверки выполнены; готовность порта не означает полного прохождения механик движка.

## Функции

| Оригинал (строка) | C# / сигнатура | Статус |
|---|---|---|
| `03_dungeon.hps:26` | `JustineDungeon.DoEnemyEffects(int alArea, string sEnemy) → void` | перенесена |
| `03_dungeon.hps:42` | `JustineDungeon.TimerStopEnemySpawnEffect(string asTimer) → void` | перенесена |
| `03_dungeon.hps:66` | `JustineDungeon.DoEnemyStep() → void` | перенесена |
| `03_dungeon.hps:107` | `JustineDungeon.Timer_BreakDoor(string asTimer) → void` | перенесена |
| `03_dungeon.hps:117` | `JustineDungeon.SetUpEnemyHindering(string asDoorName) → void` | перенесена |
| `03_dungeon.hps:156` | `JustineDungeon.ActivateEnemy(string asEnemy) → void` | перенесена |
| `03_dungeon.hps:181` | `JustineDungeon.DisableEnemy() → void` | перенесена |
| `03_dungeon.hps:188` | `JustineDungeon.IsEnemyActive() → bool` | перенесена |
| `03_dungeon.hps:207` | `JustineDungeon.UpdatePatrolNodes(bool abForce) → void` | перенесена |
| `03_dungeon.hps:257` | `JustineDungeon.ChasePlayer() → void` | перенесена |
| `03_dungeon.hps:264` | `JustineDungeon.Timer_KeepEnemyUpdated(string asTimer) → void` | перенесена |
| `03_dungeon.hps:274` | `JustineDungeon.TrackEntityLocation(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:333` | `JustineDungeon.AddDelayedStartPounding(float afTime) → void` | перенесена |
| `03_dungeon.hps:341` | `JustineDungeon.StartPounding(int aArea, float afMinTime, float afMaxTime) → void` | перенесена |
| `03_dungeon.hps:364` | `JustineDungeon.SetPoundingTime(float afMinTime, float afMaxTime) → void` | перенесена |
| `03_dungeon.hps:375` | `JustineDungeon.SetPoundingIntensity(ePoundIntensity aX) → void` | перенесена |
| `03_dungeon.hps:382` | `JustineDungeon.StopPounding() → void` | перенесена |
| `03_dungeon.hps:391` | `JustineDungeon.Timer_Pounding(string asTimer) → void` | перенесена |
| `03_dungeon.hps:422` | `JustineDungeon.Timer_StartPounding(string asTimer) → void` | перенесена |
| `03_dungeon.hps:437` | `JustineDungeon.CollideSecurityDoorInWater(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:446` | `JustineDungeon.CollideWaterEffectsSwingDoor(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:464` | `JustineDungeon.PlayerInteractSlideDoor(string asEntity) → void` | перенесена |
| `03_dungeon.hps:484` | `JustineDungeon.InteractLockedDoor(string asEntity) → void` | перенесена |
| `03_dungeon.hps:511` | `JustineDungeon.SetDoorState(string asDoorName, eDoorState aState) → void` | перенесена |
| `03_dungeon.hps:519` | `JustineDungeon.LeverHelper_Open(string asLeverName, int alSlideDoorNum, int alState) → bool` | перенесена |
| `03_dungeon.hps:536` | `JustineDungeon.LeverHelper_Close(string asLeverName, int alSlideDoorNum, int alState) → bool` | перенесена |
| `03_dungeon.hps:569` | `JustineDungeon.CollidePassedDoor(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:577` | `JustineDungeon.Timer_PissOffMonster(string asTimer) → void` | перенесена |
| `03_dungeon.hps:591` | `JustineDungeon.InteractMissingLever(string asEntity) → void` | перенесена |
| `03_dungeon.hps:598` | `JustineDungeon.TimerInteractMissingLever(string asTimer) → void` | перенесена |
| `03_dungeon.hps:605` | `JustineDungeon.EntityCallPickLever(string asEntity, string type) → void` | перенесена |
| `03_dungeon.hps:610` | `JustineDungeon.PuzzleSlideDoor01_PickupLeverHandle(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:617` | `JustineDungeon.CollideStartPoundingWithLever(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:627` | `JustineDungeon.PuzzleOpenDoor01_EnterNearDoorArea(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:648` | `JustineDungeon.CheckPlayerAndLeverInsideArea() → void` | перенесена |
| `03_dungeon.hps:655` | `JustineDungeon.UseLeverOnMount(string asItem, string asEntity) → void` | перенесена |
| `03_dungeon.hps:667` | `JustineDungeon.PuzzleOpenDoor01_StartFixLeverTimer(string asStickyArea, string asBodyName) → void` | перенесена |
| `03_dungeon.hps:678` | `JustineDungeon.PuzzleOpenDoor01_FixLever(string asTimer) → void` | перенесена |
| `03_dungeon.hps:698` | `JustineDungeon.PuzzleOpenDoor01_LeverPulled(string asEntityName, int alState) → void` | перенесена |
| `03_dungeon.hps:737` | `JustineDungeon.ActivateEnemy00() → void` | перенесена |
| `03_dungeon.hps:750` | `JustineDungeon.Timer_ActivateEnemy00_Door(string asTimer) → void` | перенесена |
| `03_dungeon.hps:756` | `JustineDungeon.Timer_ActivateEnemy00_Sound(string asTimer) → void` | перенесена |
| `03_dungeon.hps:762` | `JustineDungeon.CollideStartSearchMusic(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:777` | `JustineDungeon.PuzzleSlideDoor02_LeverPulled(string asEntityName, int alState) → void` | перенесена |
| `03_dungeon.hps:815` | `JustineDungeon.PuzzleSlideDoor02_PickupCogWheel(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:832` | `JustineDungeon.TimerPickupCogEvent(string asTimer) → void` | перенесена |
| `03_dungeon.hps:840` | `JustineDungeon.PuzzleSlideDoor02_CogwheelInMachineryHall(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:858` | `JustineDungeon.PuzzleOpenDoor02_StartFixMachineTimer(string asEntityName, string asBodyName) → void` | перенесена |
| `03_dungeon.hps:871` | `JustineDungeon.CollideAreaCloseToMachine(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:882` | `JustineDungeon.PuzzleOpenDoor02_FixMachine(string asTimer) → void` | перенесена |
| `03_dungeon.hps:928` | `JustineDungeon.Timer_EnableEnemyAtDoor1(string asTimer) → void` | перенесена |
| `03_dungeon.hps:937` | `JustineDungeon.InteractAreaMachineHelp(string asEntity) → void` | перенесена |
| `03_dungeon.hps:950` | `JustineDungeon.PuzzleSlideDoor03_DoorHalfUp(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:962` | `JustineDungeon.PuzzleSlideDoor03_DoorSetCollidable(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:971` | `JustineDungeon.PuzzleSlideDoor03_LeverPulled(string asEntityName, int alState) → void` | перенесена |
| `03_dungeon.hps:981` | `JustineDungeon.PuzzleSlideDoor03_DoorClosed(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1027` | `JustineDungeon.PuzzleSliderDoor03_PrisonerDoorValveInteract(string asEntity) → void` | перенесена |
| `03_dungeon.hps:1039` | `JustineDungeon.PuzzleSlideDoor03_ClosePrisonerDoor(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1062` | `JustineDungeon.PuzzleSlideDoor03_PrisonerDoorValveState(string asEntityName, int alState) → void` | перенесена |
| `03_dungeon.hps:1087` | `JustineDungeon.Timer_EnableEnemyAtDoor2(string asTimer) → void` | перенесена |
| `03_dungeon.hps:1094` | `JustineDungeon.CollideBreakDoor(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1116` | `JustineDungeon.Timer_EnableEnemyAtDoor3(string asTimer) → void` | перенесена |
| `03_dungeon.hps:1125` | `JustineDungeon.CollideEnemyEndCrash(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1141` | `JustineDungeon.CollideStartChase(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1155` | `JustineDungeon.BreakTunnelBoxEffect(string sEntity, int iArea) → void` | перенесена |
| `03_dungeon.hps:1176` | `JustineDungeon.StateChangePhono01(string asEntity, int alState) → void` | перенесена |
| `03_dungeon.hps:1182` | `JustineDungeon.PlayPhono(int iLevel, int iSet, int iPart, string sEntity) → void` | перенесена |
| `03_dungeon.hps:1200` | `JustineDungeon.VoiceOverPhono() → void` | перенесена |
| `03_dungeon.hps:1207` | `JustineDungeon.CollideAreaStopPhono(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1225` | `JustineDungeon.CollideCharacterStartTalk(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1240` | `JustineDungeon.CollideCharacterStopTalk(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1254` | `JustineDungeon.GetCharacterTopicNotSpoken(string asTopic) → bool` | перенесена |
| `03_dungeon.hps:1260` | `JustineDungeon.SetCharacterTopicSpoken(string asTopic) → void` | перенесена |
| `03_dungeon.hps:1266` | `JustineDungeon.SetCharacterTopicNotSpoken(string asTopic) → void` | перенесена |
| `03_dungeon.hps:1272` | `JustineDungeon.PlayCharacterVoice(string asTopic, int alNumOfParts, bool abRandom, bool abSetAsSpoken, bool abForceNewTopic) → void` | перенесена |
| `03_dungeon.hps:1349` | `JustineDungeon.CharacterTalkOver(float fStopVoiceFade) → void` | перенесена |
| `03_dungeon.hps:1360` | `JustineDungeon.TimerDelayIdleVoice(string asTimer) → void` | перенесена |
| `03_dungeon.hps:1368` | `JustineDungeon.StartCharacterSpeak(string sTopicName, int iNr, bool abRandom,  bool abSetAsSpoken, bool abForceNewTopic) → void` | перенесена |
| `03_dungeon.hps:1392` | `JustineDungeon.StopCharacterSpeak() → void` | перенесена |
| `03_dungeon.hps:1410` | `JustineDungeon.InteractValveVoice(string asEntity) → void` | перенесена |
| `03_dungeon.hps:1415` | `JustineDungeon.TimerInteractValveVoice(string asTimer) → void` | перенесена |
| `03_dungeon.hps:1437` | `JustineDungeon.TimerPrisonerDeathVoice(string asTimer) → void` | перенесена |
| `03_dungeon.hps:1451` | `JustineDungeon.BeginDeathNoises() → void` | перенесена |
| `03_dungeon.hps:1494` | `JustineDungeon.TimerDeathNoises(string asTimer) → void` | перенесена |
| `03_dungeon.hps:1537` | `JustineDungeon.PlayEnemyMusic(int alLevel) → void` | перенесена |
| `03_dungeon.hps:1557` | `JustineDungeon.CollideAreaAmbSoundChange(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1580` | `JustineDungeon.PickUpNote(string asEntity, string asType) → void` | перенесена |
| `03_dungeon.hps:1596` | `JustineDungeon.OnStart() → void` | перенесена |
| `03_dungeon.hps:1720` | `JustineDungeon.OnLeave() → void` | перенесена |
| `03_dungeon.hps:1728` | `JustineDungeon.Timer_SetUpPrisonerDoor(string asTimer) → void` | перенесена |
| `03_dungeon.hps:1734` | `JustineDungeon.CollidePushArea(string asParent, string asChild, int alState) → void` | перенесена |
| `03_dungeon.hps:1750` | `JustineDungeon.Timer_PushPlayer(string asTimer) → void` | перенесена |

Всего 92 функций, включая неиспользуемые, отладочные и недостижимые участки.

## Поля, массивы, константы и enum

| Исходная строка | Данные (исходные значения и порядок) |
|---|---|
| `03_dungeon.hps:8` | `gfTimeToActivateEnemyAfterEnterSlideDoor1Area` — float; начальное значение `2.0f` |
| `03_dungeon.hps:9` | `gfTimeToActivateEnemyAfterFixingLever` — float; начальное значение `2.0f` |
| `03_dungeon.hps:10` | `gfTimeToStartPoundingAfterSlideDoor1Closed` — float; начальное значение `15.0f` |
| `03_dungeon.hps:11` | `gfTimeToStartPoundingAfterSlideDoor2Closed` — float; начальное значение `1.0f` |
| `03_dungeon.hps:12` | `gfTimeToActivateEnemyAfterFixingMachine` — float; начальное значение `7.0f` |
| `03_dungeon.hps:13` | `gfTimeToActivateEnemyAtSlideDoor2WhenInteractingValve` — float; начальное значение `10.0f` |
| `03_dungeon.hps:14` | `gfTimeToActivateEnemyAtSlideDoor2WhenDoorHalfUp` — float; начальное значение `5.0f` |
| `03_dungeon.hps:15` | `gfTimeToActivateEnemyAtSlideDoor3` — float; начальное значение `2.0f` |
| `03_dungeon.hps:197` | `gvPatrolNodes` — string[][]; массив; исходные порядок, длина, значения и пустые sentinel сохранены, подтверждены токеновой сверкой |
| `03_dungeon.hps:308` | `gvPoundDoorNames` — string[]; массив; исходные порядок, длина, значения и пустые sentinel сохранены, подтверждены токеновой сверкой |
| `03_dungeon.hps:321` | `gvPoundSuffix` — string[]; массив; исходные порядок, длина, значения и пустые sentinel сохранены, подтверждены токеновой сверкой |
| `03_dungeon.hps:325` | `gfMinTimeBetweenPounding` — float; начальное значение `1.0f` |
| `03_dungeon.hps:326` | `gfMaxTimeBetweenPounding` — float; начальное значение `3.0f` |
| `03_dungeon.hps:328` | `ksDelayedPoundingTimerName` — string, константа; начальное значение `"DelayedStartPounding"` |
| `03_dungeon.hps:329` | `ksPoundingTimerName` — string, константа; начальное значение `"Pounding"` |
| `03_dungeon.hps:498` | `kLever_Open` — int, константа; начальное значение `-1` |
| `03_dungeon.hps:499` | `kLever_Close` — int, константа; начальное значение `1` |
| `03_dungeon.hps:568` | `gfTimeToLockDoorSafely` — float; начальное значение `3.0f` |
| `03_dungeon.hps:1733` | `fPushX, fPushY, fPushZ` — float; начальное значение 0 |
| `03_dungeon.hps:56` | enum `eEnemyState`; порядок: `eEnemyState_Hindered`, `eEnemyState_Hunting`; нумерация с 0 |
| `03_dungeon.hps:300` | enum `ePoundArea`; порядок: `ePoundArea_EastCorridor`, `ePoundArea_SlideDoor1`, `ePoundArea_SlideDoor2`, `ePoundArea_SlideDoor3`; нумерация с 0 |
| `03_dungeon.hps:316` | enum `ePoundIntensity`; порядок: `ePoundIntensity_Low`, `ePoundIntensity_High`; нумерация с 0 |
| `03_dungeon.hps:501` | enum `eDoorState`; порядок: `eDoorState_Closed`, `eDoorState_Open`, `eDoorState_Destroyed`; нумерация с 0 |

Размеры массивов: gvPatrolNodes — 4 строки длиной 4/4/3/1, каждая заканчивается пустой строкой; gvPoundDoorNames — 4 имени wooden_slide_door_0…3; gvPoundSuffix — 2 значения low/high.

## Синтаксис и исходные особенности

Ручной перенос сохраняет имена/порядок функций и операций, поля, условия, циклы, строки и английские комментарии. Только существующие lifecycle-функции стали public override; string &in/& → string, статические callbacks → nameof, десятичные литералы → float, and/or/not → C#-операторы. Локальная string без инициализации получает пустую строку, как в AngelScript. AddKeyPart с верхним битом — unchecked((int)0x…), все 32 бита/порядок сохранены.

Независимая сверка 26 891 токена и 980 исходных комментариев во всех четырёх портах: `Logs/Justine_TokenReview.txt`; фактические .cs дополнительно сравниваются с reviewed drafts. Компиляция: `Logs/Justine_Compile.txt`, затем Unity перед каждым запуском. Исходные неиспользуемые/недостижимые участки вызывают CS0162/CS0219/CS0169 и не удалялись.

- `03_dungeon.hps:1311`: Сохранён исходный voice callback с несовместимой сигнатурой float/0 аргументов.
- `03_dungeon.hps:197`: Двумерный массив перенесён в string[][] с явными вложенными конструкторами. Порядок, пустые sentinel, индексы и enum/int преобразования сохранены.
- `03_dungeon.hps:10`: На первом исходном чтении gvPoundDoorNames при неверном индексе добавлен return: AngelScript прерывает вызов при array abort. Предыдущие эффекты сохранены. Такие же выходы в PlayerInteractSlideDoor и LeverHelper_Close; в Timer_Pounding — перед gvPoundSuffix. PoundingIntensity читается один раз.
- `03_dungeon.hps (см. таблицу функций)`: OnStart регистрирует имя отсутствующей исходной функции. Точная строка оставлена; фиктивная функция не добавлялась.
- `03_dungeon.hps:1567`: Источник не создаёт именованную sound entity water; PreloadSound("L03_water") не создаёт её. Предупреждение остановки сохранено.
- `03_dungeon.hps:858`: с 2026-10-17 Sticky работает (CLAUDE.md «Верёвки, связи, липкие зоны, слизь»): шестерня прикрепляется сама и вызывает callback; ручное продолжение callback в этой диагностике — только проверка скрипта.
- `03_dungeon.hps:467`: PlayerInteractSlideDoor создаёт таймер с пустым callback; после двух секунд исходный пустой вызов записан как WARNING [Script] ERROR.
- `03_dungeon.hps:684`: Имя присутствует в .map (ID=2851), импортировано с тем же регистром и путём. .ent имеет EntityType=StaticProp: PropFactory не создаёт script entity, HplMap.Build добавляет только физические тела, lookup не находит имя. Оригинальный LuxStaticProp.cpp:35–37 также создаёт static entity. Это тип/lookup, не потерянный меш.
- `03_dungeon.hps:1494`: После специальных веток TimerDeathNoises всегда передаёт имя таймера как звуковой ресурс: AreaCharTalk_1/2/3 и StopMusic не .snt, а L03_attack отсутствует в звуковом банке. Исходный fall-through сохранён; отсутствующие сообщения не скрывались.
- `03_dungeon.hps:1141`: При FelixIsEaten=1 исходник не начинает погоню (ChaseStarted=0). Отдельный свежий chase_saved-fixture проверяет противоположную ветку.

## API и пределы реализации

Подготовка профиля/режима смерти выполнена отдельно в J0: [README.md](README.md). Русские тексты DLC и английская озвучка lang/eng/ptest; основные рецепты отсутствуют.

| API | Вызовов в .hps | Фактическое объявление / статус |
|---|---:|---|
| `AddDebugMessage` | 11 | `AddDebugMessage(string asString, bool abCheckForDuplicates)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:14`) |
| `AddEffectVoice` | 4 | `AddEffectVoice(string asVoiceFile, string asEffectFile, string asTextCat, string asTextEntry, bool abUsePostion,                                       string asPosEnitity, float afMinDistance, float afMaxDistance)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:31`) |
| `AddEnemyPatrolNode` | 1 | `AddEnemyPatrolNode(string asEnemyName, string asNodeName, float afWaitTime, string asAnimation)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:569`) |
| `AddEntityCollideCallback` | 29 | `AddEntityCollideCallback(string asParentName, string asChildName, string asFunction, bool abDeleteOnCollide, int alStates)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:205`) |
| `AddKeyPart` | 4 | `AddKeyPart(int alKeyPart)` — ✓ с 2026-10-17 (CLAUDE.md «Титры, NPC, анимации и прикреплённые пропы») |
| `AddLocalVarInt` | 1 | `AddLocalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:58`) |
| `AddPlayerBodyForce` | 1 | `AddPlayerBodyForce(float afX, float afY, float afZ, bool abUseLocalCoords)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:17`) |
| `AddPropForce` | 4 | `AddPropForce(string asName, float afX, float afY, float afZ, string asCoordSystem)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:417`) |
| `AddTimer` | 62 | `AddTimer(string asName, float afTime, string asFunction)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:44`) |
| `AddUseItemCallback` | 1 | `AddUseItemCallback(string asName, string asItem, string asEntity, string asFunction, bool abAutoDestroy)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:96`) |
| `ClearEnemyPatrolNodes` | 1 | `ClearEnemyPatrolNodes(string asEnemyName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:589`) |
| `CreateParticleSystemAtEntity` | 6 | `CreateParticleSystemAtEntity(string asPSName, string asPSFile, string asEntity, bool abSavePS)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:165`) |
| `FadeRadialBlurTo` | 2 | `FadeRadialBlurTo(float afSize, float afSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:16`) |
| `GetEffectVoiceActive` | 2 | `GetEffectVoiceActive()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:46`) |
| `GetEntityExists` | 1 | `GetEntityExists(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:72`) |
| `GetFlashbackIsActive` | 1 | `GetFlashbackIsActive()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:53`) |
| `GetLocalVarInt` | 33 | `GetLocalVarInt(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:66`) |
| `GetLocalVarString` | 20 | `GetLocalVarString(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:78`) |
| `GetTimerTimeLeft` | 2 | `GetTimerTimeLeft(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:48`) |
| `GiveItemFromFile` | 1 | `GiveItemFromFile(string asName, string asFileName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:75`) |
| `GiveSanityBoost` | 1 | `GiveSanityBoost()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:57`) |
| `HasItem` | 1 | `HasItem(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:87`) |
| `InteractConnectPropWithMoveObject` | 2 | `InteractConnectPropWithMoveObject(string asName, string asPropName, string asMoveObjectName, bool abInteractOnly,                                                          bool abInvert, int alStatesUsed)` — ✓ с 2026-10-17 (CLAUDE.md «Верёвки, связи, липкие зоны, слизь») |
| `PlayMusic` | 2 | `PlayMusic(string asMusicFile, bool abLoop, float afVolume, float afFadeTime, int alPrio, bool abResume)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:137`) |
| `PlaySoundAtEntity` | 23 | `PlaySoundAtEntity(string asSoundName, string asSoundFile, string asEntity, float afFadeSpeed, bool abSaveSound)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:87`) |
| `PreloadSound` | 18 | `PreloadSound(string asSoundFile)` — empty (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:80`) |
| `RandFloat` | 5 | `RandFloat(float afMin, float afMax)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:26`) |
| `RandInt` | 6 | `RandInt(int alMin, int alMax)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:28`) |
| `RemoveItem` | 1 | `RemoveItem(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:85`) |
| `RemoveTimer` | 6 | `RemoveTimer(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:46`) |
| `RotatePropToSpeed` | 24 | `RotatePropToSpeed(string asName, float afAcc, float afGoalSpeed, float afAxisX, float afAxisY, float afAxisZ,                                          bool abResetSpeed, string asOffsetArea)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:249`) |
| `ScriptDebugOn` | 1 | `ScriptDebugOn()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:24`) |
| `SetEffectVoiceOverCallback` | 6 | `SetEffectVoiceOverCallback(string asFunc)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:48`) |
| `SetEnemyDisableTriggers` | 2 | `SetEnemyDisableTriggers(string asName, bool abX)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:564`) |
| `SetEntityActive` | 23 | `SetEntityActive(string asName, bool abActive)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:58`) |
| `SetEntityInteractionDisabled` | 1 | `SetEntityInteractionDisabled(string asName, bool abDisabled)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:191`) |
| `SetEntityPlayerInteractCallback` | 5 | `SetEntityPlayerInteractCallback(string asName, string asCallback, bool abRemoveOnInteraction)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:176`) |
| `SetGlobalVarInt` | 2 | `SetGlobalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:82`) |
| `SetLeverStuckState` | 2 | `SetLeverStuckState(string asName, int alState, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:341`) |
| `SetLocalVarInt` | 29 | `SetLocalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:52`) |
| `SetLocalVarString` | 5 | `SetLocalVarString(string asName, string asVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:56`) |
| `SetMapDisplayNameEntry` | 1 | `SetMapDisplayNameEntry(string asNameEntry)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:138`) |
| `SetMessage` | 5 | `SetMessage(string asTextCategory, string asTextEntry, float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:9`) |
| `SetMoveObjectState` | 2 | `SetMoveObjectState(string asName, float afState)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:397`) |
| `SetMoveObjectStateExt` | 1 | `SetMoveObjectStateExt(string asName, float afState, float afAcc, float afMaxSpeed, float afSlowdownDist, bool abResetSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:402`) |
| `SetPlayerPermaDeathSound` | 1 | `SetPlayerPermaDeathSound(string asSound)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:50`) |
| `SetPropHealth` | 2 | `SetPropHealth(string asName, float afHealth)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:377`) |
| `SetRadialBlurStartDist` | 1 | `SetRadialBlurStartDist(float afStartDist)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:18`) |
| `SetupLoadScreen` | 1 | `SetupLoadScreen(string asTextCat, string asTextEntry, int alRandomNum, string asImageFile)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:156`) |
| `ShowEnemyPlayerPosition` | 1 | `ShowEnemyPlayerPosition(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:540`) |
| `StartScreenShake` | 2 | `StartScreenShake(float afAmount, float afTime, float afFadeInTime, float afFadeOutTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:50`) |
| `StopAllEffectVoices` | 3 | `StopAllEffectVoices(float afFadeOutTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:44`) |
| `StopMusic` | 3 | `StopMusic(float afFadeTime, int alPrio)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:140`) |
| `StopSound` | 3 | `StopSound(string asSoundName, float afFadeTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:123`) |

Статус implemented означает наличие реализации, а не полную совместимость механики. Suitor-модели/параметры импортированы, но EnemyConverter не включает их отдельные анимации из entities/ptest; патрули/активация не доказывают полную погоню и атаки. Interact connections, rope, Sticky/prop animation и титры/ARG остаются явными ограничениями. Пустые API в таблице отмечены отдельно и не выдаются за реализованные.

## Импорт и статическая сверка

Импорт: `Logs/Justine_Import_03_dungeon.txt` и одноимённый .log, Unity ExitCode=0, 0 ошибок. Числа/виды предупреждений зафиксированы в отчёте импорта. Исторические D:/SVN и E:/amnesia переходы не переписаны.

Штатная команда: `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -MapRoot maps/ptest -Map 03_dungeon -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Justine/JustineDungeon.cs`. Отчёт `Logs/Justine_Check_Final_03_dungeon.txt`: требование 0 ошибок; отсутствующие исходные callbacks не подменяются фиктивными функциями.


Фактически импорт: 3 предупреждений, 0 ошибок. Предупреждение No level script port относится к импорту до установки нового класса; последующие запуски уже используют порт. FogArea пока пропускается; missing child отражает привязки скелетных узлов, а не численный PASS геометрии. gui_amn_mm_textbox_marker.tga — неиспользуемая данным меню ссылка skin.

| Предупреждение импорта | Количество |
|---|---:|
| 'FogArea' is not supported yet (skipped) | 1 |
| Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga | 1 |
| No level script port for maps/ptest/03_dungeon.map yet (the level runs without its events) | 1 |

## Сценарии

Все ниже: seed=1, каждый сценарий в свежем Unity-процессе, PlayerSimulation.RunBatch, GPU (без -nographics), -batchmode -quit, исходная карта maps/ptest. Логи в UnityProj/UMnesiaRTX/Logs, имя Justine_<fixture> .log/.txt/.scenario.txt/.exit.txt. W/E — фактические строки отчёта, дополнительно проверены вложенные [Script] ERROR и исключения. ExitCode=0 сам по себе не означает чистый PASS.

### 03_dungeon_start

ExitCode=0; предупреждений=7, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 30 5; shot start; w 1; watch 2 1; shot walked; health
```

Наблюдавшиеся voice-файлы: .

### 03_dungeon_doors_machine

ExitCode=0; предупреждений=10, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 3 1; call GiveItemFromFile slide_door01_lever_handle slide_door_lever_handle.ent; call UseLeverOnMount slide_door01_lever_handle PuzzleOpenDoor01_StickyArea; watch 2 1; call GetLocalVarInt PuzzleSlideDoor01_LeverFixed; call SetLeverStuckState slide_door01_fixedlever -1 true; watch 10 2; call GetLocalVarInt EnemyStep; call TrackEntityLocation enemy_suitor_0 Area_Room_0 1; call SetLeverStuckState slide_door01_lever 1 true; watch 2 1; call GetLocalVarInt EnemyState; call PuzzleSlideDoor02_PickupCogWheel elevator_cogwheel01_1 Area_PickCogWheel -1; watch 4 1; call PuzzleOpenDoor02_StartFixMachineTimer PuzzleSlideDoor02_CogSticky elevator_cogwheel01_1_Body_1; watch 12 2; call GetLocalVarInt PuzzleSlideDoor02_MachineFixed; call SetLeverStuckState slide_door02_lever_open -1 true; watch 2 1; call TrackEntityLocation enemy_suitor_1 Area_Room_1 1; call SetLeverStuckState slide_door02_lever_close 1 true; watch 5 1; call GetLocalVarInt EnemyState; body wooden_slide_door_2; start SecondDoor; shot machinery
```

Возвращённые значения: `GetLocalVarInt PuzzleSlideDoor01_LeverFixed` → `1`; `GetLocalVarInt EnemyStep` → `0`; `GetLocalVarInt EnemyState` → `0`; `GetLocalVarInt PuzzleSlideDoor02_MachineFixed` → `1`; `GetLocalVarInt EnemyState` → `0`.

Наблюдавшиеся voice-файлы: .

### 03_dungeon_pounding_guards

ExitCode=0; предупреждений=8, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 3 1; call SetLocalVarInt EnemyStep 0; call SetLocalVarInt EnemyState 0; call SetLocalVarInt wooden_slide_door_1_state 0; call StartPounding 1 1 2; watch 6 1; call SetLocalVarInt PoundingIntensity 1; watch 6 1; call StopPounding; watch 4 1; call GetLocalVarInt PoundingActive; call SetLocalVarInt EnemyStep 4; call StartPounding 3 1 2; call PlayerInteractSlideDoor wooden_slide_door_3; watch 3 1; call SetLocalVarInt PoundingIntensity 8; call Timer_Pounding Pounding; call LeverHelper_Close slide_door03_lever 9 1; health
```

Возвращённые значения: `GetLocalVarInt PoundingActive` → `0`; `LeverHelper_Close slide_door03_lever 9 1` → `False`.

Наблюдавшиеся voice-файлы: .

### 03_dungeon_felix_saved

ExitCode=0; предупреждений=11, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 3 1; call PuzzleSliderDoor03_PrisonerDoorValveInteract valve_iron_rusty02_2; call PuzzleSlideDoor03_ClosePrisonerDoor wooden_slide_door_4 Area_PrisonerDoorClosed 1; call PuzzleSlideDoor03_DoorHalfUp Area_SouthernDoorHalfUp wooden_slide_door_3 -1; call SetLocalVarInt EnemyState 0; call SetLocalVarInt EnemyStep 1; call PuzzleSlideDoor03_DoorClosed Area_SouthernDoorClosed wooden_slide_door_3 1; watch 12 2; call GetGlobalVarInt FelixIsDead; call GetLocalVarInt PuzzleSlideDoor03_PrisonerDoorClosed; call GetLocalVarInt EnemyStep; enemy; start ThridDoor; shot prisoner_door
```

Возвращённые значения: `GetGlobalVarInt FelixIsDead` → `0`; `GetLocalVarInt PuzzleSlideDoor03_PrisonerDoorClosed` → `1`; `GetLocalVarInt EnemyStep` → `2`.

Наблюдавшиеся voice-файлы: .

### 03_dungeon_felix_eaten_chase

ExitCode=0; предупреждений=13, ошибок=1, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 3 1; call PuzzleSlideDoor03_DoorClosed Area_SouthernDoorClosed wooden_slide_door_3 1; watch 30 2; call GetGlobalVarInt FelixIsDead; call GetLocalVarInt FelixIsEaten; call CollideStartChase Player Area_StartChase 1; call CollideEnemyEndCrash enemy_suitor_3 Area_EnemyBreaksThroughDebris_1 1; call CollideEnemyEndCrash enemy_suitor_3 Area_EnemyBreaksThroughDebris_2 1; call CollideEnemyEndCrash enemy_suitor_3 Area_EnemyBreaksThroughDebris_3 1; call ActivateEnemy enemy_suitor_3; call TrackEntityLocation Player Area_Room_4 1; call CollideBreakDoor enemy_suitor_3 Area_BreakDoor 1; watch 5 1; enemy; call GetLocalVarInt ChaseStarted
```

Возвращённые значения: `GetGlobalVarInt FelixIsDead` → `1`; `GetLocalVarInt FelixIsEaten` → `1`; `GetLocalVarInt ChaseStarted` → `0`.

Наблюдавшиеся voice-файлы: `felix_dying_01.ogg`, `felix_death_01.ogg`.

Результат содержит исходную ошибку callback; не чистый PASS.

### 03_dungeon_phono_talk

ExitCode=0; предупреждений=7, ошибок=1, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 3 1; call SetWheelStuckState phonograph_1 1 true; watch 90 5; call GetLocalVarInt PlayPhono; call CollideCharacterStartTalk Player AreaCharStartTalk 1; watch 25 2; call CollideCharacterStopTalk Player AreaCharStopTalk -1; call CollideAreaAmbSoundChange Player AreaAmbSoundChange -1; watch 3 1; sounds
```

Возвращённые значения: `GetLocalVarInt PlayPhono` → `0`.

Наблюдавшиеся voice-файлы: `justine_rec_lvl3_01_01.ogg`, `justine_rec_lvl3_01_02.ogg`, `justine_rec_lvl3_01_03.ogg`, `felix_discover_01.ogg`.

Результат содержит исходную ошибку callback; не чистый PASS.

### 03_dungeon_perma_death

ExitCode=0; предупреждений=7, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 1 1; health; call GivePlayerDamage 500 claws false true; watch 35 2; health; sounds
```

Наблюдавшиеся voice-файлы: .

### 03_dungeon_chase_saved

ExitCode=0; предупреждений=9, ошибок=0, исключений C# в логе=0.
Начальные globals: `FelixIsDead=0`; scriptDebug=False; DX12.

```text
watch 3 1; call PuzzleSlideDoor03_ClosePrisonerDoor wooden_slide_door_4 Area_PrisonerDoorClosed 1; call CollideStartChase Player Area_StartChase 1; watch 10 2; call GetLocalVarInt ChaseStarted; call GetLocalVarInt FelixIsEaten; call GetGlobalVarInt FelixIsDead; enemy; start PlayerStartArea_5; w 1; shot chase_passage; health
```

Возвращённые значения: `GetLocalVarInt ChaseStarted` → `1`; `GetLocalVarInt FelixIsEaten` → `0`; `GetGlobalVarInt FelixIsDead` → `0`.

Наблюдавшиеся voice-файлы: .

### Сообщения и диагностика

Все уникальные сообщения выполненных сценариев (время удалено, полные количества по каждому fixture выше):

- `AddKeyPart: skipped, needs credits (step 6, menus)`
- `Animation 'Idle' does not exist!`
- `Animation 'Notice2' does not exist!`
- `Animation 'Walk' does not exist!`
- `ERROR: Cannot find sound entity 'AreaCharTalk_1'`
- `ERROR: Cannot find sound entity 'AreaCharTalk_2'`
- `ERROR: Cannot find sound entity 'AreaCharTalk_3'`
- `ERROR: Cannot find sound entity 'L03_attack'`
- `ERROR: Cannot find sound entity 'StopMusic'`
- `ERROR: Could not find sound entity 'water'`
- `ERROR: Script call without a function name`
- `ERROR: Script function 'PuzzleSliderDoor03_SouthernDoorValveState' does not exist in JustineDungeon`
- `Enemy 'enemy_suitor_0': no animations (Tools > RTXMnesia > Import > Enemies)`
- `Enemy 'enemy_suitor_1': no animations (Tools > RTXMnesia > Import > Enemies)`
- `Enemy 'enemy_suitor_2': no animations (Tools > RTXMnesia > Import > Enemies)`
- `Enemy 'enemy_suitor_3': no animations (Tools > RTXMnesia > Import > Enemies)`
- `Entity '' with type Enemy and subtype any does not exist!`
- `Entity 'slide_door01_lever_mount' with type any and subtype any does not exist!`
- `InteractConnectPropWithMoveObject: skipped, needs interact connections`
- `Script function 'CharacterTalkOver' takes 1 arguments, the engine passes 0`

Ручные call, выдача предметов, снятие callback/таймеров и задание globals — диагностическая подготовка, не доказательство естественного решения загадок. body LadderArea_1 в первом cells fixture ошибочен (area не prop); padlock_2 к концу phono-fixture уже изменён/разрушен. Полные сообщения сохранены.

## Геометрия и маршрут

Моделей 107, подмешей 197, prefabMatrixDifferences 4, scene failures 16.

`Logs/Justine_Audit_03_dungeon.txt/.log/.exit.txt`: Unity ExitCode=1, общий аудит НЕ PASS. Ошибок геометрии=0, отсутствующих кэшей=0, maxVertexError=0; выбор ресурсов 471 проверка / 0 провалов. Расхождения матриц префабов и scene failures приведены в отчёте; PLACEMENT ошибок нет.

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

Собственные файлы карты: `JustineDungeon.cs`, его `.meta`, этот журнал; общая новая папка Justine/.meta. J0 перечислен отдельно. При возобновлении git status был чистым: ранее выполненная работа уже вошла в HEAD c4064fe вместе с пользовательским этапом сохранений. Предыдущие порты/их журналы/планы и новые сохранения здесь не правились.

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
