# 02_library

Оригинал: `source/maps/ptest/02_library.hps` (1004 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Justine/JustineLibrary.cs`.
Класс `RTXMnesia.Levels.Justine.JustineLibrary`, атрибут `HplMapScript("maps/ptest/02_library.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/ptest/02_library.unity` (вне git).
Дата: 2026-10-17.
Статус: готов — полный скрипт перенесён, проверки выполнены; готовность порта не означает полного прохождения механик движка.

## Функции

| Оригинал (строка) | C# / сигнатура | Статус |
|---|---|---|
| `02_library.hps:10` | `JustineLibrary.GetLightBoxSpotLight() → string` | перенесена |
| `02_library.hps:21` | `JustineLibrary.OnLightBoxLit(string asEntity, string asType) → void` | перенесена |
| `02_library.hps:40` | `JustineLibrary.TimerBoxLit(string asTimer) → void` | перенесена |
| `02_library.hps:47` | `JustineLibrary.FadeOutAllBoxSpotLights(float afFadeTime) → void` | перенесена |
| `02_library.hps:62` | `JustineLibrary.UseStencilOnBox(string asItem, string asEntity) → void` | перенесена |
| `02_library.hps:116` | `JustineLibrary.PlayStencilSound(bool bSwitchedStencils, string asEntity) → void` | перенесена |
| `02_library.hps:132` | `JustineLibrary.PlayerInteractLightBox(string asEntity) → void` | перенесена |
| `02_library.hps:172` | `JustineLibrary.UseStencilOnSlot(string asItem, string asEntity) → void` | перенесена |
| `02_library.hps:205` | `JustineLibrary.PlayerInteractSlot(string asEntity) → void` | перенесена |
| `02_library.hps:233` | `JustineLibrary.SlotLeverMove(string asEntity, int alState) → void` | перенесена |
| `02_library.hps:284` | `JustineLibrary.SavePrisoner() → void` | перенесена |
| `02_library.hps:310` | `JustineLibrary.KillPrisoner() → void` | перенесена |
| `02_library.hps:339` | `JustineLibrary.TimerPrisonerDeath(string asTimer) → void` | перенесена |
| `02_library.hps:352` | `JustineLibrary.TorturePrisoner() → void` | перенесена |
| `02_library.hps:375` | `JustineLibrary.TimerUnstuckSlotLever(string asTimer) → void` | перенесена |
| `02_library.hps:383` | `JustineLibrary.PlayPrisonerSound(string sNoise, bool bStopIdle) → void` | перенесена |
| `02_library.hps:403` | `JustineLibrary.TimerPrisonerIdle(string asTimer) → void` | перенесена |
| `02_library.hps:419` | `JustineLibrary.OpenSecretDoor() → void` | перенесена |
| `02_library.hps:427` | `JustineLibrary.TimerStartShelfSound(string asTimer) → void` | перенесена |
| `02_library.hps:437` | `JustineLibrary.TimerStopShelfSound(string asTimer) → void` | перенесена |
| `02_library.hps:451` | `JustineLibrary.CollideLightGoOut(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:464` | `JustineLibrary.TimerDelayLampsOut(string asTimer) → void` | перенесена |
| `02_library.hps:475` | `JustineLibrary.CollideStartEnemy(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:498` | `JustineLibrary.CollideEnterNextArea(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:514` | `JustineLibrary.CollideAreaEnterExit(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:524` | `JustineLibrary.CollideEnemyAreaEnterExit(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:538` | `JustineLibrary.PickStencil(string asEntity, string asType) → void` | перенесена |
| `02_library.hps:550` | `JustineLibrary.CollidePoundDoor(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:557` | `JustineLibrary.CollideAreaStopPoundDoor(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:564` | `JustineLibrary.TimerDoorPoundLoop(string asTimer) → void` | перенесена |
| `02_library.hps:587` | `JustineLibrary.CollideAreaPitch(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:653` | `JustineLibrary.StateChangePhono01(string asEntity, int alState) → void` | перенесена |
| `02_library.hps:659` | `JustineLibrary.StateChangePhono02(string asEntity, int alState) → void` | перенесена |
| `02_library.hps:665` | `JustineLibrary.PlayPhono(int iLevel, int iSet, int iPart, string sEntity) → void` | перенесена |
| `02_library.hps:683` | `JustineLibrary.VoiceOverPhono() → void` | перенесена |
| `02_library.hps:690` | `JustineLibrary.CollideAreaStopPhono(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:704` | `JustineLibrary.CollideCharacterStartTalk(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:726` | `JustineLibrary.CollideCharacterStopTalk(string asParent, string asChild, int alState) → void` | перенесена |
| `02_library.hps:742` | `JustineLibrary.GetCharacterTopicNotSpoken(string asTopic) → bool` | перенесена |
| `02_library.hps:748` | `JustineLibrary.SetCharacterTopicSpoken(string asTopic) → void` | перенесена |
| `02_library.hps:754` | `JustineLibrary.SetCharacterTopicNotSpoken(string asTopic) → void` | перенесена |
| `02_library.hps:760` | `JustineLibrary.PlayCharacterVoice(string asTopic, int alNumOfParts, bool abRandom, bool abSetAsSpoken, bool abForceNewTopic) → void` | перенесена |
| `02_library.hps:837` | `JustineLibrary.CharacterTalkOver(float fStopVoiceFade) → void` | перенесена |
| `02_library.hps:848` | `JustineLibrary.TimerDelayIdleVoice(string asTimer) → void` | перенесена |
| `02_library.hps:856` | `JustineLibrary.StartCharacterSpeak(string sTopicName, int iNr, bool abRandom,  bool abSetAsSpoken, bool abForceNewTopic) → void` | перенесена |
| `02_library.hps:880` | `JustineLibrary.StopCharacterSpeak() → void` | перенесена |
| `02_library.hps:893` | `JustineLibrary.EntityCallPickNote(string asEntity, string type) → void` | перенесена |
| `02_library.hps:902` | `JustineLibrary.EntityCallPickNoteCount(string asEntity, string type) → void` | перенесена |
| `02_library.hps:915` | `JustineLibrary.OnStart() → void` | перенесена |
| `02_library.hps:996` | `JustineLibrary.OnLeave() → void` | перенесена |

Всего 50 функций, включая неиспользуемые, отладочные и недостижимые участки.

## Поля, массивы, константы и enum

| Исходная строка | Данные (исходные значения и порядок) |
|---|---|
| `02_library.hps:3` | `gfBoxSpotR` — float; начальное значение `0.5` |
| `02_library.hps:4` | `gfBoxSpotG` — float; начальное значение `0.6` |
| `02_library.hps:5` | `gfBoxSpotB` — float; начальное значение `0.9` |
| `02_library.hps:6` | `gfBoxSpotA` — float; начальное значение `0.5` |

## Синтаксис и исходные особенности

Ручной перенос сохраняет имена/порядок функций и операций, поля, условия, циклы, строки и английские комментарии. Только существующие lifecycle-функции стали public override; string &in/& → string, статические callbacks → nameof, десятичные литералы → float, and/or/not → C#-операторы. Локальная string без инициализации получает пустую строку, как в AngelScript. AddKeyPart с верхним битом — unchecked((int)0x…), все 32 бита/порядок сохранены.

Независимая сверка 26 891 токена и 980 исходных комментариев во всех четырёх портах: `Logs/Justine_TokenReview.txt`; фактические .cs дополнительно сравниваются с reviewed drafts. Компиляция: `Logs/Justine_Compile.txt`, затем Unity перед каждым запуском. Исходные неиспользуемые/недостижимые участки вызывают CS0162/CS0219/CS0169 и не удалялись.

- `02_library.hps:799`: Исходная несовместимость voice callback описана выше для cells: float-параметр при вызове без аргументов; сохранена во всех ветках.
- `02_library.hps:62`: Четыре трафарета, замены/снятие, оба слота, повторное использование и выбор spotlight проверяются отдельно от решений рычагом.
- `02_library.hps:246`: Предметы выдаются fixture до помещения в слоты. SetLeverStuckState с true вызывает настоящий callback состояния; это не ручной вызов SavePrisoner.
- `02_library.hps:252`: Спасение, убийство и четыре пытки выполняются в свежих процессах, без накопления предыдущего теста.
- `02_library.hps:395`: В изолированных save/kill/torture-fixtures не запускался разговор при входе в зону, поэтому именованный idle ещё не создан. Это предупреждение подготовки теста, не доказательство отсутствия аудиоресурса.

## API и пределы реализации

Подготовка профиля/режима смерти выполнена отдельно в J0: [README.md](README.md). Русские тексты DLC и английская озвучка lang/eng/ptest; основные рецепты отсутствуют.

| API | Вызовов в .hps | Фактическое объявление / статус |
|---|---:|---|
| `AddDebugMessage` | 22 | `AddDebugMessage(string asString, bool abCheckForDuplicates)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:14`) |
| `AddEffectVoice` | 4 | `AddEffectVoice(string asVoiceFile, string asEffectFile, string asTextCat, string asTextEntry, bool abUsePostion,                                       string asPosEnitity, float afMinDistance, float afMaxDistance)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:31`) |
| `AddEnemyPatrolNode` | 13 | `AddEnemyPatrolNode(string asEnemyName, string asNodeName, float afWaitTime, string asAnimation)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:569`) |
| `AddEntityCollideCallback` | 11 | `AddEntityCollideCallback(string asParentName, string asChildName, string asFunction, bool abDeleteOnCollide, int alStates)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:205`) |
| `AddKeyPart` | 6 | `AddKeyPart(int alKeyPart)` — ✓ с 2026-10-17 (CLAUDE.md «Титры, NPC, анимации и прикреплённые пропы») |
| `AddLocalVarFloat` | 3 | `AddLocalVarFloat(string asName, float afVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:61`) |
| `AddLocalVarInt` | 5 | `AddLocalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:58`) |
| `AddPropImpulse` | 1 | `AddPropImpulse(string asName, float afX, float afY, float afZ, string asCoordSystem)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:424`) |
| `AddTimer` | 13 | `AddTimer(string asName, float afTime, string asFunction)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:44`) |
| `AddUseItemCallback` | 3 | `AddUseItemCallback(string asName, string asItem, string asEntity, string asFunction, bool abAutoDestroy)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:96`) |
| `ClearEnemyPatrolNodes` | 2 | `ClearEnemyPatrolNodes(string asEnemyName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:589`) |
| `CreateParticleSystemAtEntity` | 3 | `CreateParticleSystemAtEntity(string asPSName, string asPSFile, string asEntity, bool abSavePS)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:165`) |
| `FadeGlobalSoundSpeed` | 1 | `FadeGlobalSoundSpeed(float afDestSpeed, float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:150`) |
| `FadeLightTo` | 8 | `FadeLightTo(string asLightName, float afR, float afG, float afB, float afA, float afRadius, float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:30`) |
| `FadeRadialBlurTo` | 1 | `FadeRadialBlurTo(float afSize, float afSpeed)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:16`) |
| `GetEffectVoiceActive` | 3 | `GetEffectVoiceActive()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:46`) |
| `GetFlashbackIsActive` | 1 | `GetFlashbackIsActive()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:53`) |
| `GetLocalVarFloat` | 2 | `GetLocalVarFloat(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:72`) |
| `GetLocalVarInt` | 24 | `GetLocalVarInt(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:66`) |
| `GetLocalVarString` | 15 | `GetLocalVarString(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:78`) |
| `GiveItemFromFile` | 6 | `GiveItemFromFile(string asName, string asFileName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:75`) |
| `GiveSanityBoost` | 1 | `GiveSanityBoost()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:57`) |
| `PlayMusic` | 5 | `PlayMusic(string asMusicFile, bool abLoop, float afVolume, float afFadeTime, int alPrio, bool abResume)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:137`) |
| `PlaySoundAtEntity` | 21 | `PlaySoundAtEntity(string asSoundName, string asSoundFile, string asEntity, float afFadeSpeed, bool abSaveSound)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:87`) |
| `PreloadSound` | 19 | `PreloadSound(string asSoundFile)` — empty (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:80`) |
| `RandFloat` | 17 | `RandFloat(float afMin, float afMax)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:26`) |
| `RandInt` | 2 | `RandInt(int alMin, int alMax)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:28`) |
| `RemoveItem` | 2 | `RemoveItem(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:85`) |
| `RemoveTimer` | 5 | `RemoveTimer(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:46`) |
| `ScriptDebugOn` | 1 | `ScriptDebugOn()` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:24`) |
| `SetEffectVoiceOverCallback` | 6 | `SetEffectVoiceOverCallback(string asFunc)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:48`) |
| `SetEntityActive` | 12 | `SetEntityActive(string asName, bool abActive)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:58`) |
| `SetEntityCustomFocusCrossHair` | 4 | `SetEntityCustomFocusCrossHair(string asName, string asCrossHair)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:109`) |
| `SetEntityInteractionDisabled` | 1 | `SetEntityInteractionDisabled(string asName, bool abDisabled)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:191`) |
| `SetEntityPlayerInteractCallback` | 4 | `SetEntityPlayerInteractCallback(string asName, string asCallback, bool abRemoveOnInteraction)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:176`) |
| `SetGlobalVarInt` | 3 | `SetGlobalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:82`) |
| `SetLampLit` | 3 | `SetLampLit(string asName, bool abLit, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:280`) |
| `SetLeverStuckState` | 2 | `SetLeverStuckState(string asName, int alState, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:341`) |
| `SetLocalVarFloat` | 3 | `SetLocalVarFloat(string asName, float afVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:54`) |
| `SetLocalVarInt` | 16 | `SetLocalVarInt(string asName, int alVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:52`) |
| `SetLocalVarString` | 3 | `SetLocalVarString(string asName, string asVal)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:56`) |
| `SetMapDisplayNameEntry` | 1 | `SetMapDisplayNameEntry(string asNameEntry)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:138`) |
| `SetMessage` | 2 | `SetMessage(string asTextCategory, string asTextEntry, float afTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Journal.cs:9`) |
| `SetMoveObjectState` | 1 | `SetMoveObjectState(string asName, float afState)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:397`) |
| `SetPlayerActive` | 2 | `SetPlayerActive(bool abActive)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:10`) |
| `SetPlayerMoveSpeedMul` | 1 | `SetPlayerMoveSpeedMul(float afMul)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:126`) |
| `SetPlayerPermaDeathSound` | 1 | `SetPlayerPermaDeathSound(string asSound)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Player.cs:50`) |
| `SetPropHealth` | 1 | `SetPropHealth(string asName, float afHealth)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:377`) |
| `SetRadialBlurStartDist` | 1 | `SetRadialBlurStartDist(float afStartDist)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:18`) |
| `SetSwingDoorLocked` | 1 | `SetSwingDoorLocked(string asName, bool abLocked, bool abEffects)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:285`) |
| `SetupLoadScreen` | 1 | `SetupLoadScreen(string asTextCat, string asTextEntry, int alRandomNum, string asImageFile)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.General.cs:156`) |
| `ShowEnemyPlayerPosition` | 1 | `ShowEnemyPlayerPosition(string asName)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Entities.cs:540`) |
| `StartScreenShake` | 5 | `StartScreenShake(float afAmount, float afTime, float afFadeInTime, float afFadeOutTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:50`) |
| `StopAllEffectVoices` | 3 | `StopAllEffectVoices(float afFadeOutTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:44`) |
| `StopSound` | 8 | `StopSound(string asSoundName, float afFadeTime)` — implemented (`UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs:123`) |

Статус implemented означает наличие реализации, а не полную совместимость механики. Suitor-модели/параметры импортированы, но EnemyConverter не включает их отдельные анимации из entities/ptest; патрули/активация не доказывают полную погоню и атаки. Interact connections, rope, Sticky/prop animation и титры/ARG остаются явными ограничениями. Пустые API в таблице отмечены отдельно и не выдаются за реализованные.

## Импорт и статическая сверка

Импорт: `Logs/Justine_Import_02_library.txt` и одноимённый .log, Unity ExitCode=0, 0 ошибок. Числа/виды предупреждений зафиксированы в отчёте импорта. Исторические D:/SVN и E:/amnesia переходы не переписаны.

Штатная команда: `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -MapRoot maps/ptest -Map 02_library -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Justine/JustineLibrary.cs`. Отчёт `Logs/Justine_Check_Final_02_library.txt`: требование 0 ошибок; отсутствующие исходные callbacks не подменяются фиктивными функциями.


Фактически импорт: 27 предупреждений, 0 ошибок. Предупреждение No level script port относится к импорту до установки нового класса; последующие запуски уже используют порт. FogArea пока пропускается; missing child отражает привязки скелетных узлов, а не численный PASS геометрии. gui_amn_mm_textbox_marker.tga — неиспользуемая данным меню ссылка skin.

| Предупреждение импорта | Количество |
|---|---:|
| entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1' | 6 |
| entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2' | 6 |
| entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3' | 6 |
| entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4' | 6 |
| 'FogArea' is not supported yet (skipped) | 1 |
| Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga | 1 |
| No level script port for maps/ptest/02_library.map yet (the level runs without its events) | 1 |

## Сценарии

Все ниже: seed=1, каждый сценарий в свежем Unity-процессе, PlayerSimulation.RunBatch, GPU (без -nographics), -batchmode -quit, исходная карта maps/ptest. Логи в UnityProj/UMnesiaRTX/Logs, имя Justine_<fixture> .log/.txt/.scenario.txt/.exit.txt. W/E — фактические строки отчёта, дополнительно проверены вложенные [Script] ERROR и исключения. ExitCode=0 сам по себе не означает чистый PASS.

### 02_library_start

ExitCode=0; предупреждений=1, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 30 5; shot start; w 1; watch 2 1; shot walked; health
```

Наблюдавшиеся voice-файлы: .

### 02_library_stencils

ExitCode=0; предупреждений=1, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=True; DX12.

```text
watch 1 1; call OnLightBoxLit light_box_1 OnIgnite; watch 2 1; call UseStencilOnBox stencil01 light_box_1; watch 2 1; call UseStencilOnBox stencil02 light_box_1; watch 2 1; call UseStencilOnBox stencil03 light_box_1; watch 2 1; call UseStencilOnBox stencil04 light_box_1; watch 2 1; call GetLightBoxSpotLight; shot light_box; call PlayerInteractLightBox light_box_1; call UseStencilOnSlot stencil01 AreaCardSlot_01; call UseStencilOnSlot stencil03 AreaCardSlot_01; call PlayerInteractSlot AreaCardSlot_01; call UseStencilOnSlot stencil04 AreaCardSlot_01; call UseStencilOnSlot stencil03 AreaCardSlot_02; call GetLocalVarInt SlotVal1; call GetLocalVarInt SlotVal2
```

Возвращённые значения: `GetLightBoxSpotLight` → `SpotLight_stencil04`; `GetLocalVarInt SlotVal1` → `4`; `GetLocalVarInt SlotVal2` → `3`.

Наблюдавшиеся voice-файлы: .

### 02_library_save

ExitCode=0; предупреждений=3, ошибок=1, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 1 1; call GiveItemFromFile stencil04 stencil04.ent; call GiveItemFromFile stencil03 stencil03.ent; call UseStencilOnSlot stencil04 AreaCardSlot_01; call UseStencilOnSlot stencil03 AreaCardSlot_02; call SetLeverStuckState slot_machine_1 1 true; watch 35 3; call GetGlobalVarInt HectorIsDead; call GetLocalVarInt PrisonerAlive; body shelf_secret_door; shot secret_door
```

Возвращённые значения: `GetGlobalVarInt HectorIsDead` → `0`; `GetLocalVarInt PrisonerAlive` → `0`.

Наблюдавшиеся voice-файлы: `hector_saved_01.ogg`.

Результат содержит исходную ошибку callback; не чистый PASS.

### 02_library_kill

ExitCode=0; предупреждений=3, ошибок=1, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 1 1; call GiveItemFromFile stencil02 stencil02.ent; call UseStencilOnSlot stencil02 AreaCardSlot_01; call SetLeverStuckState slot_machine_1 1 true; watch 35 3; call GetGlobalVarInt HectorIsDead; call GetLocalVarInt PrisonerAlive; body shelf_secret_door
```

Возвращённые значения: `GetGlobalVarInt HectorIsDead` → `1`; `GetLocalVarInt PrisonerAlive` → `0`.

Наблюдавшиеся voice-файлы: `hector_dying_01.ogg`, `hector_death_01.ogg`.

Результат содержит исходную ошибку callback; не чистый PASS.

### 02_library_torture

ExitCode=0; предупреждений=3, ошибок=1, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 1 1; call SetLeverStuckState slot_machine_1 1 true; watch 15 3; call SetLeverStuckState slot_machine_1 1 true; watch 15 3; call SetLeverStuckState slot_machine_1 1 true; watch 15 3; call SetLeverStuckState slot_machine_1 1 true; watch 35 3; call GetLocalVarInt TortureCount; call GetGlobalVarInt HectorIsDead
```

Возвращённые значения: `GetLocalVarInt TortureCount` → `4`; `GetGlobalVarInt HectorIsDead` → `1`.

Наблюдавшиеся voice-файлы: `hector_terrorized_01.ogg`, `hector_sobbing_01.ogg`, `hector_sobbing_02.ogg`, `hector_dying_01.ogg`, `hector_death_01.ogg`.

Результат содержит исходную ошибку callback; не чистый PASS.

### 02_library_phono_enemy

ExitCode=0; предупреждений=2, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 1 1; call SetWheelStuckState phonograph_1 1 true; watch 60 5; call GetLocalVarInt PlayPhono; call CollideLightGoOut Player AreaLightGoOut 1; watch 5 1; call CollideStartEnemy Player AreaStartEnemy 1; call CollideEnterNextArea Player AreaEnterNextArea 1; enemy; call PickStencil stencil01 OnPickup; call PickStencil stencil02 OnPickup; call CollidePoundDoor Player AreaPoundDoor 1; watch 5 1; call CollideAreaStopPoundDoor Player AreaStopPoundDoor 1; call CollideAreaPitch Player AreaPitch_1 1; call CollideAreaPitch Player AreaPitch_6 1; call CollideAreaPitch Player AreaPitch_1 -1; call EntityCallPickNote note_1 OnPickup; call GetGlobalVarInt HasAptNote; health; start PlayerStartArea_4; shot library
```

Возвращённые значения: `GetLocalVarInt PlayPhono` → `0`; `GetGlobalVarInt HasAptNote` → `1`.

Наблюдавшиеся voice-файлы: `justine_rec_lvl2_01_01.ogg`, `justine_rec_lvl2_01_02.ogg`.

### 02_library_perma_death

ExitCode=0; предупреждений=1, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 1 1; health; call GivePlayerDamage 500 claws false true; watch 35 2; health; sounds
```

Наблюдавшиеся voice-файлы: .

### 02_library_passage

ExitCode=0; предупреждений=1, ошибок=0, исключений C# в логе=0.
Начальные globals: `пусто`; scriptDebug=False; DX12.

```text
watch 1 1; start PlayerStartArea_3; w 1; shot reading_room; start PlayerStartArea_5; w 1; shot exit_passage; health
```

Наблюдавшиеся voice-файлы: `hector_discover_01.ogg`.

### Сообщения и диагностика

Все уникальные сообщения выполненных сценариев (время удалено, полные количества по каждому fixture выше):

- `AddKeyPart: skipped, needs credits (step 6, menus)`
- `Animation 'Idle' does not exist!`
- `ERROR: Could not find sound entity 'idle'`
- `Enemy 'suitor_basile_1': no animations (Tools > RTXMnesia > Import > Enemies)`
- `Script function 'CharacterTalkOver' takes 1 arguments, the engine passes 0`

Ручные call, выдача предметов, снятие callback/таймеров и задание globals — диагностическая подготовка, не доказательство естественного решения загадок. body LadderArea_1 в первом cells fixture ошибочен (area не prop); padlock_2 к концу phono-fixture уже изменён/разрушен. Полные сообщения сохранены.

## Геометрия и маршрут

Моделей 190, подмешей 360, prefabMatrixDifferences 19, scene failures 7.

`Logs/Justine_Audit_02_library.txt/.log/.exit.txt`: Unity ExitCode=1, общий аудит НЕ PASS. Ошибок геометрии=0, отсутствующих кэшей=0, maxVertexError=0; выбор ресурсов 471 проверка / 0 провалов. Расхождения матриц префабов и scene failures приведены в отчёте; PLACEMENT ошибок нет.

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

Собственные файлы карты: `JustineLibrary.cs`, его `.meta`, этот журнал; общая новая папка Justine/.meta. J0 перечислен отдельно. При возобновлении git status был чистым: ранее выполненная работа уже вошла в HEAD c4064fe вместе с пользовательским этапом сохранений. Предыдущие порты/их журналы/планы и новые сохранения здесь не правились.

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
