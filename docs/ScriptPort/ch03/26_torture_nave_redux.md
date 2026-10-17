# 26_torture_nave_redux

Оригинал: `source/maps/main/ch03/26_torture_nave_redux.hps` (2291 строк, Latin-1).  
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureNaveRedux.cs`, `RTXMnesia.Levels.Ch03.TortureNaveRedux`.  
Атрибут: `HplMapScript("maps/main/ch03/26_torture_nave_redux.map")`.  
Сцена: `Assets/AmnesiaImported/Levels/maps/main/ch03/26_torture_nave_redux.unity` (вне git).  
Дата: 2026-10-16. Статус: готов — C#-порт, компиляция и диагностические проверки завершены; ограничения движка и геометрии перечислены ниже.

## Функции

Все 127 исходных функций перенесены в исходном порядке, включая debug/неиспользуемые обработчики. Сигнатуры сохраняют число/типы параметров; только имеющиеся lifecycle-функции стали public override.

| Оригинал | Порт | Статус |
|---|---|---|
| `26_torture_nave_redux.hps:10` | `TortureNaveRedux.PlayerReact` | готов |
| `26_torture_nave_redux.hps:21` | `TortureNaveRedux.TimerPlayerReactions` | готов |
| `26_torture_nave_redux.hps:46` | `TortureNaveRedux.UseBloodOnBurner` | готов |
| `26_torture_nave_redux.hps:58` | `TortureNaveRedux.TimerBloodInPlace` | готов |
| `26_torture_nave_redux.hps:68` | `TortureNaveRedux.TurnSpecialBurner` | готов |
| `26_torture_nave_redux.hps:95` | `TortureNaveRedux.TimerBurnerFullSpeed` | готов |
| `26_torture_nave_redux.hps:105` | `TortureNaveRedux.CheckBoilBlood` | готов |
| `26_torture_nave_redux.hps:119` | `TortureNaveRedux.TimerBoilEffectDone` | готов |
| `26_torture_nave_redux.hps:130` | `TortureNaveRedux.TimerBoilEffectFadeOutBlood` | готов |
| `26_torture_nave_redux.hps:137` | `TortureNaveRedux.UseMixtureOnBurner` | готов |
| `26_torture_nave_redux.hps:150` | `TortureNaveRedux.UseBloodOnSqueezer` | готов |
| `26_torture_nave_redux.hps:157` | `TortureNaveRedux.UseMixOnSqueezer` | готов |
| `26_torture_nave_redux.hps:177` | `TortureNaveRedux.UseGlandOnSqueezer` | готов |
| `26_torture_nave_redux.hps:212` | `TortureNaveRedux.InteractSqueezer` | готов |
| `26_torture_nave_redux.hps:223` | `TortureNaveRedux.TurnSqueezer` | готов |
| `26_torture_nave_redux.hps:247` | `TortureNaveRedux.TimerSqueezeEffectDone` | готов |
| `26_torture_nave_redux.hps:259` | `TortureNaveRedux.TimerSqueezeEffectFadeOutMix` | готов |
| `26_torture_nave_redux.hps:274` | `TortureNaveRedux.UseBloodOnOven` | готов |
| `26_torture_nave_redux.hps:281` | `TortureNaveRedux.UseMixOnOven` | готов |
| `26_torture_nave_redux.hps:303` | `TortureNaveRedux.TimerOvenDoorImpulse` | готов |
| `26_torture_nave_redux.hps:311` | `TortureNaveRedux.TimerPushAgain` | готов |
| `26_torture_nave_redux.hps:318` | `TortureNaveRedux.InteractOven` | готов |
| `26_torture_nave_redux.hps:329` | `TortureNaveRedux.UseBoneOnOven` | готов |
| `26_torture_nave_redux.hps:362` | `TortureNaveRedux.TimerPlaceBoneOvenDoorOpen` | готов |
| `26_torture_nave_redux.hps:374` | `TortureNaveRedux.PullOvenLever` | готов |
| `26_torture_nave_redux.hps:405` | `TortureNaveRedux.TimerOvenRunningOver` | готов |
| `26_torture_nave_redux.hps:446` | `TortureNaveRedux.TimerFadeOutOvenMix` | готов |
| `26_torture_nave_redux.hps:451` | `TortureNaveRedux.TimerRunningDoneOvenDoorOpen` | готов |
| `26_torture_nave_redux.hps:456` | `TortureNaveRedux.OvenHint` | готов |
| `26_torture_nave_redux.hps:466` | `TortureNaveRedux.TimerHintOven` | готов |
| `26_torture_nave_redux.hps:478` | `TortureNaveRedux.PickDoneMixture` | готов |
| `26_torture_nave_redux.hps:499` | `TortureNaveRedux.UseGlandOnBurner` | готов |
| `26_torture_nave_redux.hps:504` | `TortureNaveRedux.UseBoneOnBurner` | готов |
| `26_torture_nave_redux.hps:509` | `TortureNaveRedux.UseBoneOnSqueezer` | готов |
| `26_torture_nave_redux.hps:514` | `TortureNaveRedux.UseGlandOnOven` | готов |
| `26_torture_nave_redux.hps:519` | `TortureNaveRedux.UseDoneMixtureOnSqueezer` | готов |
| `26_torture_nave_redux.hps:524` | `TortureNaveRedux.UseDoneMixtureOnOven` | готов |
| `26_torture_nave_redux.hps:531` | `TortureNaveRedux.UseGlassOnMachine` | готов |
| `26_torture_nave_redux.hps:544` | `TortureNaveRedux.CollideWellKillPlayer` | готов |
| `26_torture_nave_redux.hps:557` | `TortureNaveRedux.TimerFallDeath` | готов |
| `26_torture_nave_redux.hps:579` | `TortureNaveRedux.CheckPointFallWell` | готов |
| `26_torture_nave_redux.hps:592` | `TortureNaveRedux.DoGuardianHowl` | готов |
| `26_torture_nave_redux.hps:610` | `TortureNaveRedux.TimerPlayerPantReaction` | готов |
| `26_torture_nave_redux.hps:617` | `TortureNaveRedux.TimerStopGuardianHowl` | готов |
| `26_torture_nave_redux.hps:625` | `TortureNaveRedux.CollideGuardianHowl_1` | готов |
| `26_torture_nave_redux.hps:632` | `TortureNaveRedux.CollideGuardianHowl_2` | готов |
| `26_torture_nave_redux.hps:639` | `TortureNaveRedux.CollideGuardianHowl_3` | готов |
| `26_torture_nave_redux.hps:653` | `TortureNaveRedux.CollideAgrippaStartTalk` | готов |
| `26_torture_nave_redux.hps:670` | `TortureNaveRedux.CollideAgrippaStopTalk` | готов |
| `26_torture_nave_redux.hps:694` | `TortureNaveRedux.AgrippaTopicNotSpoken` | готов |
| `26_torture_nave_redux.hps:700` | `TortureNaveRedux.SetAgrippaTopicHasSpoken` | готов |
| `26_torture_nave_redux.hps:707` | `TortureNaveRedux.PlayAgrippaNormalVoice` | готов |
| `26_torture_nave_redux.hps:744` | `TortureNaveRedux.AgrippaNormalSpeak` | готов |
| `26_torture_nave_redux.hps:839` | `TortureNaveRedux.AgrippaNormalTalkOver` | готов |
| `26_torture_nave_redux.hps:846` | `TortureNaveRedux.CheckAgrippaNormalTalkOverEvent` | готов |
| `26_torture_nave_redux.hps:851` | `TortureNaveRedux.TimerNewAgrippaTopic` | готов |
| `26_torture_nave_redux.hps:869` | `TortureNaveRedux.StopAgrippaNormalSpeak` | готов |
| `26_torture_nave_redux.hps:897` | `TortureNaveRedux.CollideAgrippaRandomArea` | готов |
| `26_torture_nave_redux.hps:915` | `TortureNaveRedux.UseDoneMixOnAgrippa` | готов |
| `26_torture_nave_redux.hps:935` | `TortureNaveRedux.TimerDrinkSounds` | готов |
| `26_torture_nave_redux.hps:942` | `TortureNaveRedux.TimerDrinkOverSounds` | готов |
| `26_torture_nave_redux.hps:954` | `TortureNaveRedux.UseBloodOnAgrippa` | готов |
| `26_torture_nave_redux.hps:959` | `TortureNaveRedux.UseNotdoneMixOnAgrippa` | готов |
| `26_torture_nave_redux.hps:973` | `TortureNaveRedux.UseSawOnAgrippa` | готов |
| `26_torture_nave_redux.hps:1000` | `TortureNaveRedux.TimerSawSounds` | готов |
| `26_torture_nave_redux.hps:1028` | `TortureNaveRedux.TimerSawSounds02` | готов |
| `26_torture_nave_redux.hps:1051` | `TortureNaveRedux.SawSoundsOver` | готов |
| `26_torture_nave_redux.hps:1061` | `TortureNaveRedux.UseMiscOnAgrippa` | готов |
| `26_torture_nave_redux.hps:1071` | `TortureNaveRedux.EntityCallAgrippaHead` | готов |
| `26_torture_nave_redux.hps:1083` | `TortureNaveRedux.InitAgrippa` | готов |
| `26_torture_nave_redux.hps:1113` | `TortureNaveRedux.InitWaterLurker` | готов |
| `26_torture_nave_redux.hps:1153` | `TortureNaveRedux.InitMissingIngredients` | готов |
| `26_torture_nave_redux.hps:1266` | `TortureNaveRedux.CollideRopeInteraction` | готов |
| `26_torture_nave_redux.hps:1277` | `TortureNaveRedux.CollideRopeDown` | готов |
| `26_torture_nave_redux.hps:1329` | `TortureNaveRedux.InteractStuckWellLever` | готов |
| `26_torture_nave_redux.hps:1334` | `TortureNaveRedux.TimerLurkerEat` | готов |
| `26_torture_nave_redux.hps:1343` | `TortureNaveRedux.TimerLurkerEatRopeImpulseOnly` | готов |
| `26_torture_nave_redux.hps:1349` | `TortureNaveRedux.TimerStopLookAtWheel` | готов |
| `26_torture_nave_redux.hps:1354` | `TortureNaveRedux.TimerLurkerDoneEating` | готов |
| `26_torture_nave_redux.hps:1370` | `TortureNaveRedux.InteractSlimeBone` | готов |
| `26_torture_nave_redux.hps:1395` | `TortureNaveRedux.UseMeatOnRope` | готов |
| `26_torture_nave_redux.hps:1418` | `TortureNaveRedux.CollideCrankSound` | готов |
| `26_torture_nave_redux.hps:1426` | `TortureNaveRedux.UseStuffOnAgrippa` | готов |
| `26_torture_nave_redux.hps:1432` | `TortureNaveRedux.UseItemOnRope` | готов |
| `26_torture_nave_redux.hps:1449` | `TortureNaveRedux.CollideStartLurkerSounds` | готов |
| `26_torture_nave_redux.hps:1456` | `TortureNaveRedux.CollideStopLurkerSounds` | готов |
| `26_torture_nave_redux.hps:1463` | `TortureNaveRedux.StartWaterLurkerAmbience` | готов |
| `26_torture_nave_redux.hps:1473` | `TortureNaveRedux.StopWaterLurkerAmbience` | готов |
| `26_torture_nave_redux.hps:1485` | `TortureNaveRedux.TimerWaterLurkerAmbience` | готов |
| `26_torture_nave_redux.hps:1503` | `TortureNaveRedux.descFungus` | готов |
| `26_torture_nave_redux.hps:1508` | `TortureNaveRedux.InteractPoisionFungi` | готов |
| `26_torture_nave_redux.hps:1533` | `TortureNaveRedux.TimerDecClickCount` | готов |
| `26_torture_nave_redux.hps:1540` | `TortureNaveRedux.CollidePlayerWithFungi` | готов |
| `26_torture_nave_redux.hps:1553` | `TortureNaveRedux.UseChipperOnFungi` | готов |
| `26_torture_nave_redux.hps:1562` | `TortureNaveRedux.PosionFungiBreak` | готов |
| `26_torture_nave_redux.hps:1576` | `TortureNaveRedux.PickPoisonGland` | готов |
| `26_torture_nave_redux.hps:1597` | `TortureNaveRedux.InteractHatchDrainage` | готов |
| `26_torture_nave_redux.hps:1604` | `TortureNaveRedux.PickUpStringPile` | готов |
| `26_torture_nave_redux.hps:1612` | `TortureNaveRedux.InteractBloodHole` | готов |
| `26_torture_nave_redux.hps:1620` | `TortureNaveRedux.UseContainerOnBlood` | готов |
| `26_torture_nave_redux.hps:1626` | `TortureNaveRedux.UseContainerOnHatch` | готов |
| `26_torture_nave_redux.hps:1642` | `TortureNaveRedux.UseContainerAndStringOnBlood` | готов |
| `26_torture_nave_redux.hps:1653` | `TortureNaveRedux.UseContainerAndStringOnHatch` | готов |
| `26_torture_nave_redux.hps:1668` | `TortureNaveRedux.TimerGetBloodEvent` | готов |
| `26_torture_nave_redux.hps:1708` | `TortureNaveRedux.PickupChannelWeyerNote` | готов |
| `26_torture_nave_redux.hps:1726` | `TortureNaveRedux.PickUpDiary` | готов |
| `26_torture_nave_redux.hps:1735` | `TortureNaveRedux.TimerFlashBackEvents` | готов |
| `26_torture_nave_redux.hps:1847` | `TortureNaveRedux.TimerAfterZimmermanEffects` | готов |
| `26_torture_nave_redux.hps:1867` | `TortureNaveRedux.TimerShowPetals` | готов |
| `26_torture_nave_redux.hps:1873` | `TortureNaveRedux.TimerWakeEvents` | готов |
| `26_torture_nave_redux.hps:1923` | `TortureNaveRedux.ChangePlayerProp` | готов |
| `26_torture_nave_redux.hps:1934` | `TortureNaveRedux.CollideAreaStopHead` | готов |
| `26_torture_nave_redux.hps:1938` | `TortureNaveRedux.CollideAreaFaintHead` | готов |
| `26_torture_nave_redux.hps:1946` | `TortureNaveRedux.TimerStopFaint` | готов |
| `26_torture_nave_redux.hps:1954` | `TortureNaveRedux.TimerRandomLook` | готов |
| `26_torture_nave_redux.hps:1988` | `TortureNaveRedux.StopRandomLook` | готов |
| `26_torture_nave_redux.hps:2002` | `TortureNaveRedux.TimerGuardianAmb` | готов |
| `26_torture_nave_redux.hps:2011` | `TortureNaveRedux.InteractLockedDoor` | готов |
| `26_torture_nave_redux.hps:2019` | `TortureNaveRedux.TimerInteractLockedDoor` | готов |
| `26_torture_nave_redux.hps:2026` | `TortureNaveRedux.CollideAreaSpikes` | готов |
| `26_torture_nave_redux.hps:2045` | `TortureNaveRedux.AddUseBucketToRope` | готов |
| `26_torture_nave_redux.hps:2055` | `TortureNaveRedux.UseBucketOnRope` | готов |
| `26_torture_nave_redux.hps:2081` | `TortureNaveRedux.DoBucketCrush` | готов |
| `26_torture_nave_redux.hps:2092` | `TortureNaveRedux.TimerBreakBucket` | готов |
| `26_torture_nave_redux.hps:2102` | `TortureNaveRedux.OnStart` | готов |
| `26_torture_nave_redux.hps:2245` | `TortureNaveRedux.OnEnter` | готов |
| `26_torture_nave_redux.hps:2288` | `TortureNaveRedux.OnLeave` | готов |

## Данные

- `bRoll` — `26_torture_nave_redux.hps:1953`; начальное значение и размер/порядок массива сохранены.
Локальный массив vRandomPartNum внутри AgrippaNormalSpeak (:806) сохранён целиком.

## Отличия, ошибки оригинала и зависимости

- Redux перенесён как самостоятельный класс, без наследования/рефакторинга карты 21 и без изменений InventoryScript.
- `TimerRandomLook`: явное усечение `(int)RandFloat(1,5)`. Обратный диапазон `RandFloat(-4,-12)` сохранён; оператор `or` заменён на ||.
- `AgrippaNormalSpeak`: исходный глобальный AgrippaRandomChatterNum может дать индекс -1. В той же точке чтения массива добавлен return при недопустимом индексе, как тихое прерывание AngelScript. Нормальная fixture наследует значение 1 из главы 2; отдельно проверено значение 0.
- Строка отладочной скорости fungi использует Str(float), G6/invariant, как scriptstring addon.
- `PickPoisonGland` увеличивает локальный IngredientCount; кровь и кость — глобальный. Это исходная разница, сохранена. В диагностике получены global=2/local=1.
- `AreaWellLookAt` отсутствует в исходной карте; исходное имя не заменено. `TimerNewAgrippaTopic` использует PlayerInRandomArea, отличный от PlayerInAgrippaTalkRange.
- Начальный callback колеса может остановить ещё не созданный burnerfire: предупреждения StopSound/DestroyParticleSystem в отдельных стартах не являются исключениями порта.
- Заглушки: attached props (InteractConnectPropWithRope работает с 2026-10-17: CLAUDE.md «Верёвки, связи, липкие зоны, слизь»; AutoSave с 2026-10-17 работает: сохранения перенесены (CLAUDE.md «Сохранения»)). CreateDataCache/DestroyDataCache и PreloadSound — пустые операции. Действительный кэш посещённой карты — HplSavedMaps, это отдельная работающая система. Физическое получение кости на канате этим портом не реализуется.
- Проверены обе последовательности лаборатории и ветки тоника/пилы, ингредиенты/неверные применения, реальная смерть в колодце и FromZimmermanFarm. Полный Play-переход в видение/назад проверяется отдельно после подготовки сцен.

Общие изменения синтаксиса: string &in / string & → string; десятичные литералы → float с f; существующие статические callback-имена → nameof, динамические/пустые/необъявленные остаются строками. Порядок вызовов, исходные литералы, ветви и комментарии сохранены. API/инвентарь/импортёр в этой задаче не исправлялись. Статическая проверка нашла все имена API и совместимые сигнатуры; наличие объявления не означает реализованную механику.

## Проверки

Результаты и точные fixtures приведены ниже. Все ресурсы/отчёты — в игнорируемых Assets/AmnesiaImported и Logs. Исходники игры не изменялись.


## Итог проверок главы 3

Дата: 2026-10-16. Начальное git status было чистым; чужих изменений не было. Новых механик/API не добавлено. Источники и установленная игра не изменялись, коммит/пуш не выполнялись.

Сверка `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch03 -Map 26_torture_nave_redux -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureNaveRedux.cs`: **0 ошибок, 0 предупреждений**. Компиляция подтверждена Unity-прогонами. Независимое сравнение токенов после перечисленных адаптаций C# не нашло различий; все исходные комментарии присутствуют (`Logs/Ch03_TokenReview.txt`, воспроизводимый диагностический Ch03_TokenReview.py).

Проверены 89 используемых API: все имена и сигнатуры существуют. Полная таблица актуальных сигнатур/файлов и ограничений — `Logs/Ch03_API_Inventory.md`. Существенные заглушки и пустые операции:

| API | Фактический статус |
|---|---|
| `AddAttachedPropToProp` | ✓ с 2026-10-17 | CLAUDE.md «Титры, NPC, анимации, прикреплённые пропы и критеры» |
| `AutoSave` | ✓ (2026-10-17) |
| `CreateDataCache` | пустая операция |
| `DestroyDataCache` | пустая операция |
| `InteractConnectPropWithRope` | ✓ с 2026-10-17: CLAUDE.md «Верёвки, связи, липкие зоны, слизь» |
| `PreloadSound` | пустая операция |
| `RemoveAttachedPropFromProp` | ✓ с 2026-10-17 | CLAUDE.md «Титры, NPC, анимации, прикреплённые пропы и критеры» |

Все отчёты далее — внутри `UnityProj/UMnesiaRTX/Logs/`, вне git. PlayerSimulation: `-batchmode -quit`, GPU, `-seed 1`, шаг 1/60; импорт/аудит — `-batchmode -nographics -quit`. Ни один отчёт с WARNING [Script] ERROR не трактуется как чистый скрипт только из-за 0 errors.

| Отчёт | Итог |
|---|---|
| `Ch03_Import_26_torture_nave_redux.log/.txt` | ExitCode 0; 25 warnings, 0 errors |
| `Ch03_Sim_26_feedback.log/.txt` | ExitCode 0; 4 warnings, 0 errors |
| `Ch03_Sim_26_ingredients.log/.txt` | ExitCode 0; 7 warnings, 0 errors |
| `Ch03_Sim_26_lab_bone_first.log/.txt` | ExitCode 0; 2 warnings, 0 errors |
| `Ch03_Sim_26_lab_gland_first.log/.txt` | ExitCode 0; 4 warnings, 0 errors |
| `Ch03_Sim_26_nave_start.log/.txt` | ExitCode 0; 4 warnings, 0 errors |
| `Ch03_Sim_26_return_death.log/.txt` | ExitCode 0; 4 warnings, 0 errors |
| `Ch03_Audit_26_torture_nave_redux.log/.txt` | **ExitCode 1, общий аудит не PASS**: Models=228, submeshes=497, geometryFailures=0, prefabMatrixDifferences=22, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=1,73205; Scene: instances=2312, submeshMatrices=3049, sharedMeshColliders=2314, failures=33, maxMatrixError=7,6294E-06; Resource regression: checks=471, failures=0 |

Разбор missing submesh: точное имя с существующим рендерером — 33; рендерер с другим именем внутри сущности — 0; полностью отсутствующих рендереров — 0. Это ограничение поиска MeshFilter/имён, а не доказательство независимой проверки всей скелетной геометрии. Подробности: Ch03_RendererReview.json. Разницы матриц prefab и итоговой сцены не смешиваются.

### Точные диагностические fixtures

Базовая команда: `Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch03/26_torture_nave_redux.map -seed 1 -script "<команды>" -logFile <Logs/Ch03_имя.log>`. Флаг scriptDebug включается только там, где указан; глобальные переменные передаются до OnStart. Скриптовые выдачи предметов/снятие таймеров/callbacks — подготовка диагностики, не изменения порта и не доказательство физического решения.

`Ch03_Sim_26_lab_gland_first`: scriptDebug=false, globals=`AgrippaActivatedIn21=1;AgrippaRandomChatterNum=1;IngredientCount=3;WeyerNoteIsPicked=1`.

```text
call GiveItem glass_container_blood Puzzle glass_container_blood glass_container_blood.tga 0; call UseBloodOnBurner glass_container_blood special_burner_1; call TurnSpecialBurner special_burner_1 1; watch 10 2; call GetLocalVarInt BloodBoiled; call GiveItemFromFile glass_container_mix_notdone_1 glass_container_mix_notdone.ent; call UseMixOnSqueezer glass_container_mix_notdone_1 squeezer_1; call TurnSqueezer squeezer_1 -1; call GiveItemFromFile poison_gland poison_gland.ent; call UseGlandOnSqueezer poison_gland squeezer_1; call TurnSqueezer squeezer_1 1; watch 10 2; call GetLocalVarInt GlandIsSqueezed; call GiveItemFromFile glass_container_mix_notdone_2 glass_container_mix_notdone.ent; call UseMixOnOven glass_container_mix_notdone_2 AreaOvenTable; call GiveItemFromFile fresh_meat_remains_1 fresh_meat_remains.ent; call UseBoneOnOven fresh_meat_remains_1 extaction_oven_1; watch 10 2; call PullOvenLever extaction_oven_lever_1 1; watch 10 2; call GetLocalVarInt BoneIsCooked; body glass_container_mix_done_2; call PickDoneMixture glass_container_mix_done_2 OnPickup; call GetLocalVarInt MixtureDone; shot lab_gland_first
```

`Ch03_Sim_26_lab_bone_first`: scriptDebug=false, globals=`AgrippaActivatedIn21=1;AgrippaRandomChatterNum=1;IngredientCount=3;WeyerNoteIsPicked=1`.

```text
call TurnSpecialBurner special_burner_1 1; call UseBloodOnBurner glass_container_blood special_burner_1; watch 10 2; call UseMixOnOven glass_container_mix_notdone_1 AreaOvenTable; call UseBoneOnOven fresh_meat_remains_1 extaction_oven_1; watch 10 2; call PullOvenLever extaction_oven_lever_1 1; watch 10 2; body glass_container_mix_notdone_3; call UseMixOnSqueezer glass_container_mix_notdone_3 squeezer_1; call TurnSqueezer squeezer_1 -1; call UseGlandOnSqueezer poison_gland squeezer_1; call TurnSqueezer squeezer_1 1; watch 10 2; body glass_container_mix_done_1; call GetLocalVarInt BoneIsCooked; call GetLocalVarInt GlandIsSqueezed; call UseSawOnAgrippa bone_saw_1 AreaAgrippa; watch 10 2; call UseDoneMixOnAgrippa glass_container_mix_done_1 AreaAgrippa; watch 10 2; call GetLocalVarInt AgrippaGotTonic; call UseSawOnAgrippa bone_saw_1 AreaAgrippa; watch 10 2; call GetLocalVarInt AgrippaDead; body agrippa_head_1; shot agrippa_head
```

`Ch03_Sim_26_ingredients`: scriptDebug=false, globals=`AgrippaActivatedIn21=1;AgrippaRandomChatterNum=1`.

```text
call UseContainerOnBlood glass_container_1 AreaBloodHole; watch 10 2; call UseContainerAndStringOnBlood container_and_string AreaBloodHole; watch 10 2; call GetGlobalVarInt IngredientCount; call UseChipperOnFungi stone_hammer_chipper poison_fungi_breakable_1; watch 10 2; body poison_gland; call PickPoisonGland poison_gland OnPickup; watch 10 2; call CollideRopeInteraction invisible_box_mass_3 AreaRopeInteraction 1; call UseMeatOnRope fresh_meat_2 AreaRopeInteraction; call CollideRopeDown invisible_box_mass_3 AreaRopeDown 1; watch 12 2; call InteractSlimeBone invisible_box_mass_3; watch 10 2; call UseBucketOnRope wooden_bucket_1 AreaRopeInteraction; call CollideRopeDown invisible_box_mass_3 AreaRopeDown 1; watch 10 2; call PickupChannelWeyerNote note_generic_1 OnPickup; watch 10 2; call GetGlobalVarInt WeyerNoteIsPicked; call GetGlobalVarInt IngredientCount; call GetLocalVarInt IngredientCount; start start_agrippa; w 1; s 1; shot ingredients
```

`Ch03_Sim_26_feedback`: scriptDebug=false, globals=`AgrippaActivatedIn21=1;AgrippaRandomChatterNum=1;IngredientCount=3;WeyerNoteIsPicked=1`.

```text
call UseBloodOnSqueezer glass_container_1 AreaAgrippa; watch 10 2; call UseBloodOnOven glass_container_1 AreaAgrippa; watch 10 2; call UseMixtureOnBurner glass_container_1 AreaAgrippa; watch 10 2; call UseGlandOnBurner glass_container_1 AreaAgrippa; watch 10 2; call UseBoneOnBurner glass_container_1 AreaAgrippa; watch 10 2; call UseBoneOnSqueezer glass_container_1 AreaAgrippa; watch 10 2; call UseGlandOnOven glass_container_1 AreaAgrippa; watch 10 2; call UseDoneMixtureOnSqueezer glass_container_1 AreaAgrippa; watch 10 2; call UseDoneMixtureOnOven glass_container_1 AreaAgrippa; watch 10 2; call UseGlassOnMachine glass_container_1 AreaAgrippa; watch 10 2; call UseBloodOnAgrippa glass_container_1 AreaAgrippa; watch 10 2; call UseNotdoneMixOnAgrippa glass_container_1 AreaAgrippa; watch 10 2; call SetGlobalVarInt AgrippaRandomChatterNum 0; call SetLocalVarInt SpokenVar_Return 1; call SetLocalVarInt MixtureDone 1; call SetLocalVarInt SpokenVar_PotionDone 1; call AgrippaNormalSpeak false; watch 10 2; shot feedback
```

`Ch03_Sim_26_return_death`: scriptDebug=false, globals=`FromZimmermanFarm=1;AgrippaActivatedIn21=1;AgrippaRandomChatterNum=1;IngredientCount=3;WeyerNoteIsPicked=1`.

```text
watch 10 2; call GetGlobalVarInt FromZimmermanFarm; call CollideAreaStopHead Player AreaStopHead 1; watch 10 2; call CollideAreaFaintHead Player AreaFaintHead 1; watch 10 2; call CollideWellKillPlayer Player AreaWellKillPlayer 1; watch 20 2; health; shot return_death
```

### Предупреждения прогонов

- [Script] InteractConnectPropWithRope: skipped, needs interact connections
- [Script] AutoSave: skipped, needs saving (step 6)
- [Script] ERROR: Could not find particle system 'burnerfire'
- [Script] ERROR: Could not find sound entity 'burnerfire'
- [Script] AddAttachedPropToProp: skipped, needs attached props
- [Script] RemoveAttachedPropFromProp: skipped, needs attached props
- [Script] Entity 'AreaWellLookAt' does not exist!

Это сохранённые исходные обращения, недостающие механики или явно указанная диагностическая подготовка. Для одноразовых/уже завершённых звуков и PS отсутствие записи при Stop/Destroy не трактуется как отсутствующий ресурс. Полный порядок сообщений и состояния находятся в соответствующих .txt. Исключений, PortError, takes N arguments и отсутствующих функций порта не обнаружено; исходные отсутствующие callbacks перечислены отдельно.

### Общие регрессии и границы готовности

До изменений: Ch03_Baseline_MapChange.log — **66 PASS**; Ch03_Baseline_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS**. После всех шести портов: Ch03_Regression_MapChange.log — **66 PASS, ExitCode 0**; Ch03_Regression_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS, ExitCode 0**. Обе Play-регрессии запускались без -quit/-nographics и завершили процесс самостоятельно. Тесты не менялись.

Межкартовая проверка: `Logs/Ch03_PlayReturn/Report.txt` — **18 PASS, ExitCode 0**. Оригинальный PickUpDiary запускает таймерный переход в Zimmerman; все состояния 0–11 проходят с настоящими длительностями голосов; затем исходный ChangeMap возвращает в PlayerStartArea_AfterVision. Восстановлены локальный marker=77 и FlashbackEventNum=6 из HplSavedMaps; сохранились инвентарь и global marker=321; FromZimmermanFarm потреблён, инвентарь/фонарь/рассудок/прыжок/присед восстановлены. Проверен короткий проход в видении. Снимки VisionVisible, VisionWalk, Returned сохранены; видимое окружение видения и возврат просмотрены. Временный диагностический Editor-скрипт размещался в игнорируемой папке и удалён после выхода Unity; его копия для воспроизведения осталась только в Logs/Ch03Verification.cs. Это проверяет связку 26↔Zimmerman, а не весь маршрут основной игры.

В собственный итог входят шесть новых классов Ch03 с .meta, meta папки, шесть журналов ch03, строка этапа 5 в синхронных AGENTS.md/CLAUDE.md и автоматически добавленные шесть сцен в EditorBuildSettings.asset. Стартовое меню осталось первым. TimeManager/DynamicsManager были чистыми до работы; автоматическое пересохранение Unity убрано после выхода. Планы других глав, старые порты/журналы, api.md, движок, импортёр и тесты не менялись. Игнорируемые ресурсы в git не включены.

Итоговый `git status --short --untracked-files=all` (все перечисленные изменения относятся к этой задаче):

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/CellTunnels.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/CellTunnels.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/InnerSanctum.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/InnerSanctum.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/OrbChamber.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/OrbChamber.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureChancelRedux.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureChancelRedux.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureNaveRedux.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureNaveRedux.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/ZimmermanVision.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/ZimmermanVision.cs.meta
?? docs/ScriptPort/ch03/25_cell_tunnels.md
?? docs/ScriptPort/ch03/26_torture_nave_redux.md
?? docs/ScriptPort/ch03/26_zimmerman_vision.md
?? docs/ScriptPort/ch03/27_torture_chancel_redux.md
?? docs/ScriptPort/ch03/28_inner_sanctum.md
?? docs/ScriptPort/ch03/29_orb_chamber.md
```
