# 16_cistern_entrance

Оригинал: `source/maps/main/ch02/16_cistern_entrance.hps` (725 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/CisternEntrance.cs`.
Класс: `RTXMnesia.Levels.Ch02.CisternEntrance`, атрибут: `HplMapScript("maps/main/ch02/16_cistern_entrance.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/16_cistern_entrance.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `16_cistern_entrance.hps:5` | `CisternEntrance.CollideDeepWater(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:18` | `CisternEntrance.InteractSewerDoorHasAntidote(string asDoor)` → void | перенесена |
| `16_cistern_entrance.hps:31` | `CisternEntrance.InteractSewerDoor(string asDoor)` → void | перенесена |
| `16_cistern_entrance.hps:41` | `CisternEntrance.TimerSewerDoorInteraction(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:46` | `CisternEntrance.CollidePoison_Start(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:55` | `CisternEntrance.CollidePoison_Stop(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:63` | `CisternEntrance.StopPoisonEffects()` → void | перенесена |
| `16_cistern_entrance.hps:75` | `CisternEntrance.TimerPoisonCough(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:99` | `CisternEntrance.InteractRustyLever(string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:109` | `CisternEntrance.LeverStateChange(string asEntity, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:142` | `CisternEntrance.CollideLadderPipe(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:197` | `CisternEntrance.CollideLadderDown(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:214` | `CisternEntrance.CollideLadderUp(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:225` | `CisternEntrance.InteractPipe(string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:239` | `CisternEntrance.InteractAreaOil(string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:246` | `CisternEntrance.InteractAreaOil02(string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:253` | `CisternEntrance.UseOilOnOil(string asItem, string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:258` | `CisternEntrance.UseGlassOnOil(string asItem, string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:265` | `CisternEntrance.UseContainerOnOil(string asItem, string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:280` | `CisternEntrance.TimerAddContainer(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:290` | `CisternEntrance.TimerCrouch(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:303` | `CisternEntrance.TimerStopLookAt(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:309` | `CisternEntrance.EntityCallGlassContainer(string asEntity, string type)` → void | перенесена |
| `16_cistern_entrance.hps:322` | `CisternEntrance.UseOilOnLever(string asItem, string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:344` | `CisternEntrance.InteractBridgeLever(string asEntity, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:374` | `CisternEntrance.TimerMoveBridgeUp(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:385` | `CisternEntrance.CollideBridgeSoundOff(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:404` | `CisternEntrance.TimerStopBSound(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:412` | `CisternEntrance.CollideBridgeStuck(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:429` | `CisternEntrance.InteractBridgeLever01Stuck(string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:438` | `CisternEntrance.TimerLeverStuckBackOn(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:445` | `CisternEntrance.CollidePlayerJumpBridge(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:452` | `CisternEntrance.CollideRockOnChain(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:461` | `CisternEntrance.MoveStuckBridge()` → void | перенесена |
| `16_cistern_entrance.hps:480` | `CisternEntrance.TimerBridgeHitGround(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:489` | `CisternEntrance.InteractBridgeLever01NotWorking(string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:494` | `CisternEntrance.TimerInteractBridgeLever01NotWorking(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:508` | `CisternEntrance.InteractLargeDoor(string asEntity)` → void | перенесена |
| `16_cistern_entrance.hps:519` | `CisternEntrance.TimerInteractLargeDoor(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:528` | `CisternEntrance.CollideFlashback(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:537` | `CisternEntrance.TimerWhirlSound(string asTimer)` → void | перенесена |
| `16_cistern_entrance.hps:546` | `CisternEntrance.CollideAreaDeepWater(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:562` | `CisternEntrance.CollideAreaDeepWaterSafe(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:572` | `CisternEntrance.CollideAreaPlayerUp(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:578` | `CisternEntrance.CollideAreaGround(string asParent, string asChild, int alState)` → void | перенесена |
| `16_cistern_entrance.hps:594` | `CisternEntrance.OnStart()` → void | перенесена |
| `16_cistern_entrance.hps:672` | `CisternEntrance.OnEnter()` → void | перенесена |
| `16_cistern_entrance.hps:722` | `CisternEntrance.OnLeave()` → void | перенесена |

Итого: 48 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: глобальных полей нет.

Массивов нет.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `16_cistern_entrance.hps:574`: У cp_up пустое имя callback. После настоящей смерти рантайм сообщает Script call without a function name; фиктивный callback не добавлен.
- `16_cistern_entrance.hps:558`: Конкатенации WaterSlow и вертикальной скорости используют Str/G6/InvariantCulture.
- `16_cistern_entrance.hps:660`: OnEnter читает глобальные WaterHasBeenDrained, PlayerHasAntidote и ControlRoomEastSideWorking. Возвраты проверяются отдельно от первой загрузки.

## API движка

Проверены 54 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | NotPorted | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs:119` |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave остаётся заглушкой. Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/16_cistern_entrance.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_16_cistern_entrance.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 2. Полный отчёт: `Logs/Ch02_Import_16_cistern_entrance.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют CisternEntrance.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/16_cistern_entrance.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 16_cistern_entrance -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/CisternEntrance.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_16_cistern_entrance.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/16_cistern_entrance.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: LeverGreased=1 и LadderPuzzleCompleted=1; проверены мостовые обработчики, вода, яд и пустой callback cp_up после настоящей смерти. Флаги возврата из 17/18/19 проверяются в Play. Спуск по LadderArea_1 НЕ подтверждён: оба диагностических подхода оставались в Normal; подготовленный прогон закончился после падения с health=60. Отключение трёх труб в fixture не считается решением физической загадки. Нужна отдельная проверка лестницы после полного решения в игре.

### Sim_16_cistern_entrance_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_16_cistern_entrance_start.log` / `.txt`. Player simulation: 16_cistern_entrance — 2026-10-16 23:42:52 — 1 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (0.00, 0.90, -10.29) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_16_cistern_entrance_ladder_bridge

ExitCode=0. Отчёты: `Logs/Ch02_Sim_16_cistern_entrance_ladder_bridge.log` / `.txt`. Player simulation: 16_cistern_entrance — 2026-10-16 23:48:43 — 2 warnings, 0 errors.

```text
watch 30 2; call InteractRustyLever lever_simple01_1; watch 10 2; call GiveItemFromFile glass_container_1 glass_container.ent; call UseContainerOnOil glass_container_1 AreaOil; watch 10 2; call GiveItemFromFile glass_container_oil_1 glass_container_oil.ent; active glass_container_oil_1 false; call EntityCallGlassContainer glass_container_oil_1 OnPickup; watch 10 2; call UseOilOnLever glass_container_oil_1 lever_simple01_1; watch 10 2; call LeverStateChange lever_simple01_1 1; watch 10 2; call CollideLadderDown LadderDown ladder_static_1 1; watch 10 2; call InteractBridgeLever bridge_lever_1 -1; watch 10 2; call CollideBridgeStuck AreaBridgeStuck bridge_metal_vert_1 1; watch 10 2; call CollideRockOnChain stone_small01_1 AreaThrowRock 1; watch 10 2; call InteractBridgeLever bridge_lever_2 1; watch 10 2; call GetLocalVarInt LeverGreased; call GetLocalVarInt LadderPuzzleCompleted; body ladder_static_1; shot ladder_bridge
```

Фактическое состояние:

```text
t=130,23   call call GetLocalVarInt LeverGreased → 1
t=130,25   call call GetLocalVarInt LadderPuzzleCompleted → 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Could not find sound entity 'sound_bridge_metal_vert_1'

### Sim_16_cistern_entrance_poison_return

ExitCode=0. Отчёты: `Logs/Ch02_Sim_16_cistern_entrance_poison_return.log` / `.txt`. Player simulation: 16_cistern_entrance — 2026-10-16 23:49:11 — 3 warnings, 0 errors.

```text
watch 30 2; call InteractSewerDoor level_wood_5; watch 10 2; call CollidePoison_Start Player AreaPoison_Start 1; watch 10 2; call CollidePoison_Stop Player AreaPoison_Stop 1; watch 10 2; call CollideAreaDeepWater Player AreaDeepWater_1 1; watch 10 2; call CollideAreaDeepWater Player AreaDeepWater_1 -1; watch 10 2; call CollideAreaDeepWaterSafe Player AreaDeepWaterSafe 1; watch 10 2; call CollideAreaPlayerUp Player AreaPlayerUp 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call SetGlobalVarInt WaterHasBeenDrained 1; call SetGlobalVarInt PlayerHasAntidote 1; call SetGlobalVarInt ControlRoomEastSideWorking 1; call OnEnter; watch 10 2; body level_wood_5; body LiquidArea_1; shot return
```

Фактическое состояние:

```text
t=112,43 [Script debug] Loading check point cp_up Count: 0
t=120,14   health 52,00 sanity 100,00 dead False pos (0.00, 6.89, -0.25) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathFall | check point cp_up at PlayerStartArea_2 count 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Script call without a function name
- 1× WARNING: No prop 'body LiquidArea_1'

### Sim_16_cistern_entrance_ladder_walk

ExitCode=0. Отчёты: `Logs/Ch02_Sim_16_cistern_entrance_ladder_walk.log` / `.txt`. Player simulation: 16_cistern_entrance — 2026-10-16 23:59:52 — 1 warnings, 0 errors.

```text
watch 30 2; call SetLocalVarInt LeverGreased 1; call LeverStateChange lever_simple01_1 1; watch 8 2; call CollideLadderDown LadderDown ladder_static_1 1; start PlayerStartArea_2; w 0.85; aim LadderArea_1; interact; watch 2 1; s 8; shot ladder; health
```

Фактическое состояние:

```text
t= 48,95   health 100,00 sanity 100,00 dead False pos (0.36, 7.02, -13.20) roll 0,0 pitch 70,0 yaw -1,6 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathFall | check point cp_up at PlayerStartArea_2 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_16_cistern_entrance_ladder_prepared

ExitCode=0. Отчёты: `Logs/Ch02_Sim_16_cistern_entrance_ladder_prepared.log` / `.txt`. Player simulation: 16_cistern_entrance — 2026-10-17 00:06:40 — 1 warnings, 0 errors.

```text
watch 30 2; call SetLocalVarInt LeverGreased 1; active cistern_breakable_pipe01 false; active cistern_breakable_pipe02 false; active cistern_breakable_pipe03 false; call SetLocalVarInt LadderCollided 1; call LeverStateChange lever_simple01_1 1; watch 10 2; call CollideLadderDown LadderDown ladder_static_1 1; start PlayerStartArea_2; w 1.4; watch 1 1; turn 180; aim LadderArea_1; watch 1 1; interact; watch 2 1; s 8; shot ladder_down; health
```

Фактическое состояние:

```text
t= 53,58   health 60,00 sanity 100,00 dead False pos (0.26, 3.02, 15.46) roll 0,0 pitch -43,2 yaw 181,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathFall | check point cp_up at PlayerStartArea_2 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_16_cistern_entrance.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=0. Сцена и регрессия ресурсов без ошибок; различие матрицы префаба указано отдельно.

```text
Models=41, submeshes=103, geometryFailures=0, prefabMatrixDifferences=1, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2
Scene: instances=211, submeshMatrices=332, sharedMeshColliders=211, failures=0, maxMatrixError=3,89219E-06
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 0, другое имя — у 0, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.

## Регрессии и итог партии

Исходные регрессии до изменений: `Logs/Ch02_Baseline_MapChange.log` — 66 PASS; `Logs/Ch02_Baseline_MainMenu.log` — 27 / 50 / 36 / 14 / 14 PASS, ExitCode=0 у обеих.

`Logs/Ch02_Regression_MapChange.log` — ExitCode=0:

```text
PASS: 66 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian, damage and death); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
```

`Logs/Ch02_Regression_MainMenu.log` — ExitCode=0:

```text
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 14 main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).
PASS: 14 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

Интеграционный Play-маршрут: `Logs/Ch02_PlayRoute/Report.txt`. PASS: 82 Play route checks: both prisons, hub 16 branches, sewer, hub 21 branches, capture into chapter 3.
Успешный запуск: `Logs/Ch02_PlayRoute_Prepared.log`, ExitCode=0. Метод `RTXMnesia.EditorDev.Ch02Temporary.Ch02Verification.RunBatch`, -batchmode, без -quit/-nographics, самостоятельно завершает процесс.
Подготовка маршрута: `Logs/Ch02Verification.cs`; выдача предметов и вызовы обработчиков — диагностические fixtures. Проверяются настоящие загрузки сцен и возврат сохранённого состояния, а не непрерывное ручное прохождение.
Первый диагностический запуск (`Logs/Ch02_PlayRoute.log`, ExitCode=1; `Logs/Ch02_PlayRoute/Report_Fixture_NoLitLamp.txt`) выбирал уже зажжённую лампу, которой в хабе 16 нет. Исправлен только fixture: состояние кэша проверяется замком level_dungeon_1. Код карты и движка не менялся. Временный Editor-helper и его .meta удалены после проверки; воспроизводимый исходник оставлен в игнорируемом Logs.
Просмотрены ReturnedHub8.png, ReturnedHub18.png и Captured25.png из `Logs/Ch02_PlayRoute`. Это тёмные виды по ходу переходов; они подтверждают появление мира в кадре, но не заменяют аудит всей геометрии.

Собственные изменения: 12 новых Ch02 .cs и их .meta, Ch02.meta, 12 журналов, одна строка этапа 5 в AGENTS.md/CLAUDE.md; импортёр добавляет игровые сцены в EditorBuildSettings.asset. Исходное рабочее дерево чистое. Коммитов и push нет.
После полного выхода Unity восстановлены только собственные пересохранения TimeManager.asset и DynamicsManager.asset, изначально чистых. AGENTS.md и CLAUDE.md побайтно одинаковы. Движок, импортёры, тесты, api.md и ранее готовые порты не изменялись.

Итоговый git status --short (вся партия):

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Cistern.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Cistern.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/CisternEntrance.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/CisternEntrance.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/ControlRoom.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/ControlRoom.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Morgue.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Morgue.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/PrisonNorth.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/PrisonNorth.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/PrisonSouth.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/PrisonSouth.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Sewer.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Sewer.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChancel.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChancel.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChoirEast.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChoirEast.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChoirWest.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChoirWest.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureNave.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureNave.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureTransept.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureTransept.cs.meta
?? docs/ScriptPort/ch02/15_prison_north.md
?? docs/ScriptPort/ch02/15_prison_south.md
?? docs/ScriptPort/ch02/16_cistern_entrance.md
?? docs/ScriptPort/ch02/17_control_room.md
?? docs/ScriptPort/ch02/18_cistern.md
?? docs/ScriptPort/ch02/19_morgue.md
?? docs/ScriptPort/ch02/20_sewer.md
?? docs/ScriptPort/ch02/21_torture_nave.md
?? docs/ScriptPort/ch02/22_torture_chancel.md
?? docs/ScriptPort/ch02/23_torture_transept.md
?? docs/ScriptPort/ch02/24_torture_choir_east.md
?? docs/ScriptPort/ch02/24_torture_choir_west.md
```
