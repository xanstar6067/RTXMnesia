# 27_torture_chancel_redux

Оригинал: `source/maps/main/ch03/27_torture_chancel_redux.hps` (1081 строк, Latin-1).  
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureChancelRedux.cs`, `RTXMnesia.Levels.Ch03.TortureChancelRedux`.  
Атрибут: `HplMapScript("maps/main/ch03/27_torture_chancel_redux.map")`.  
Сцена: `Assets/AmnesiaImported/Levels/maps/main/ch03/27_torture_chancel_redux.unity` (вне git).  
Дата: 2026-10-16. Статус: готов — C#-порт, компиляция и диагностические проверки завершены; ограничения движка и геометрии перечислены ниже.

## Функции

Все 54 исходных функций перенесены в исходном порядке, включая debug/неиспользуемые обработчики. Сигнатуры сохраняют число/типы параметров; только имеющиеся lifecycle-функции стали public override.

| Оригинал | Порт | Статус |
|---|---|---|
| `27_torture_chancel_redux.hps:11` | `TortureChancelRedux.RotateMachineryToSpeed` | готов |
| `27_torture_chancel_redux.hps:31` | `TortureChancelRedux.CollideSmallObjectWithCogs` | готов |
| `27_torture_chancel_redux.hps:52` | `TortureChancelRedux.CollideWeakObjectWithCogs` | готов |
| `27_torture_chancel_redux.hps:85` | `TortureChancelRedux.CollideCorrectObjectWithCogs` | готов |
| `27_torture_chancel_redux.hps:148` | `TortureChancelRedux.TimerMachineJamMusic` | готов |
| `27_torture_chancel_redux.hps:155` | `TortureChancelRedux.TimerRestoreMachineRotation` | готов |
| `27_torture_chancel_redux.hps:161` | `TortureChancelRedux.TimerJamMachine` | готов |
| `27_torture_chancel_redux.hps:171` | `TortureChancelRedux.CollidePlayerCog` | готов |
| `27_torture_chancel_redux.hps:179` | `TortureChancelRedux.InteractCogWheel` | готов |
| `27_torture_chancel_redux.hps:198` | `TortureChancelRedux.UseStringOnPedestal` | готов |
| `27_torture_chancel_redux.hps:205` | `TortureChancelRedux.UseOrbPieceOnPedestal` | готов |
| `27_torture_chancel_redux.hps:290` | `TortureChancelRedux.TimerOrbReadyMachineRunning` | готов |
| `27_torture_chancel_redux.hps:299` | `TortureChancelRedux.TimerFadeProp` | готов |
| `27_torture_chancel_redux.hps:307` | `TortureChancelRedux.UseTarOnPedestal` | готов |
| `27_torture_chancel_redux.hps:322` | `TortureChancelRedux.InteractOrbPedestal` | готов |
| `27_torture_chancel_redux.hps:332` | `TortureChancelRedux.CreateBarrierEnergyStream` | готов |
| `27_torture_chancel_redux.hps:361` | `TortureChancelRedux.TimerCreateStreamPS` | готов |
| `27_torture_chancel_redux.hps:367` | `TortureChancelRedux.TimerCreateOrbEffectPS` | готов |
| `27_torture_chancel_redux.hps:386` | `TortureChancelRedux.CollideFallDeath` | готов |
| `27_torture_chancel_redux.hps:407` | `TortureChancelRedux.TimerFallDeath` | готов |
| `27_torture_chancel_redux.hps:423` | `TortureChancelRedux.CheckPointFallDeath` | готов |
| `27_torture_chancel_redux.hps:436` | `TortureChancelRedux.CollideWindBreeze` | готов |
| `27_torture_chancel_redux.hps:453` | `TortureChancelRedux.TimerBreezeOver` | готов |
| `27_torture_chancel_redux.hps:467` | `TortureChancelRedux.InitBarrierMachine` | готов |
| `27_torture_chancel_redux.hps:486` | `TortureChancelRedux.InitBarrierTunnel` | готов |
| `27_torture_chancel_redux.hps:499` | `TortureChancelRedux.CollideTunnelDamage` | готов |
| `27_torture_chancel_redux.hps:515` | `TortureChancelRedux.TimerTunnelDamage` | готов |
| `27_torture_chancel_redux.hps:526` | `TortureChancelRedux.InteractAreaTunnelDamage` | готов |
| `27_torture_chancel_redux.hps:533` | `TortureChancelRedux.TimerInteractBackOn` | готов |
| `27_torture_chancel_redux.hps:542` | `TortureChancelRedux.UpdateBarrierState` | готов |
| `27_torture_chancel_redux.hps:632` | `TortureChancelRedux.TimerOrbExplode` | готов |
| `27_torture_chancel_redux.hps:665` | `TortureChancelRedux.TimerFadeOutExplodeLight` | готов |
| `27_torture_chancel_redux.hps:674` | `TortureChancelRedux.TimerBarrierGone` | готов |
| `27_torture_chancel_redux.hps:682` | `TortureChancelRedux.TimerStopSparks` | готов |
| `27_torture_chancel_redux.hps:694` | `TortureChancelRedux.PickDiary` | готов |
| `27_torture_chancel_redux.hps:707` | `TortureChancelRedux.PickBucketOfTar` | готов |
| `27_torture_chancel_redux.hps:720` | `TortureChancelRedux.InteractLargeGate` | готов |
| `27_torture_chancel_redux.hps:726` | `TortureChancelRedux.TimerLargeGate` | готов |
| `27_torture_chancel_redux.hps:733` | `TortureChancelRedux.InteractMachineRoomDoor` | готов |
| `27_torture_chancel_redux.hps:742` | `TortureChancelRedux.PlayerReact` | готов |
| `27_torture_chancel_redux.hps:753` | `TortureChancelRedux.TimerPlayerReactions` | готов |
| `27_torture_chancel_redux.hps:772` | `TortureChancelRedux.CollideAreaBruteInside` | готов |
| `27_torture_chancel_redux.hps:781` | `TortureChancelRedux.EventGrunt02` | готов |
| `27_torture_chancel_redux.hps:799` | `TortureChancelRedux.CollideAreaCP` | готов |
| `27_torture_chancel_redux.hps:811` | `TortureChancelRedux.CPCallLocation` | готов |
| `27_torture_chancel_redux.hps:853` | `TortureChancelRedux.TimerRemoveRoaches` | готов |
| `27_torture_chancel_redux.hps:858` | `TortureChancelRedux.TimerStopLookAt` | готов |
| `27_torture_chancel_redux.hps:868` | `TortureChancelRedux.TimerGuradianCP` | готов |
| `27_torture_chancel_redux.hps:879` | `TortureChancelRedux.TimerStopRadial` | готов |
| `27_torture_chancel_redux.hps:887` | `TortureChancelRedux.CollideAreaExitGuardian` | готов |
| `27_torture_chancel_redux.hps:910` | `TortureChancelRedux.TimerSounds` | готов |
| `27_torture_chancel_redux.hps:920` | `TortureChancelRedux.OnStart` | готов |
| `27_torture_chancel_redux.hps:1062` | `TortureChancelRedux.OnEnter` | готов |
| `27_torture_chancel_redux.hps:1078` | `TortureChancelRedux.OnLeave` | готов |

## Данные

Глобальных полей/enum нет; локальные переменные остаются в исходных функциях.

## Отличия, ошибки оригинала и зависимости

- Класс самостоятельный; код карты 22 и общий InventoryScript не изменялись.
- CreateBarrierEnergyStream: присваивание int строке заменено на invariant ToString. В UpdateBarrierState внутренние счётчики i названы j из-за запрета C# на затенение; обе вложенные петли остаются внутри внешней, включая исходные повторные эффекты. Английские комментарии оригинала сохранены.
- `CollideFallDeath` регистрирует отсутствующий CheckPointFallWell, хотя объявлен CheckPointFallDeath. Исходная ошибка сохранена, пустой подставной callback не добавлялся.
- Таймеры SmallObjectCogWait/WeakObjectCogWait/TimerInteractCogWheel имеют пустые callbacks: исходный движок пишет Script call without a function name.
- В исходной карте отсутствуют barrier_spotlight_2 и barrier_spotlight_weak_2. PSOrbStream_1 — имя созданной системы частиц; PlaySoundAtEntity ищет сущность и не находит. Литералы сохранены.
- Проверяются оба порядка завершения машины/сферы, смола и отказ без неё, все шесть частей, barrier orb absorption и снятие блокера; разные CP и падение. Диагностические выдачи/прямые callbacks не заменяют физическое решение коробкой.
- AutoSave с 2026-10-17 работает (сохранения перенесены (CLAUDE.md «Сохранения»)). ChangePlayerStateToNormal с 2026-10-17 работает (предмет в руках отпускается); PreloadSound — пустая (предзагрузка не нужна). Grunt/brute используют уже существующий EnemyGrunt, новых механик врага карта не добавляет.

Общие изменения синтаксиса: string &in / string & → string; десятичные литералы → float с f; существующие статические callback-имена → nameof, динамические/пустые/необъявленные остаются строками. Порядок вызовов, исходные литералы, ветви и комментарии сохранены. API/инвентарь/импортёр в этой задаче не исправлялись. Статическая проверка нашла все имена API и совместимые сигнатуры; наличие объявления не означает реализованную механику.

## Проверки

Результаты и точные fixtures приведены ниже. Все ресурсы/отчёты — в игнорируемых Assets/AmnesiaImported и Logs. Исходники игры не изменялись.


## Итог проверок главы 3

Дата: 2026-10-16. Начальное git status было чистым; чужих изменений не было. Новых механик/API не добавлено. Источники и установленная игра не изменялись, коммит/пуш не выполнялись.

Сверка `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch03 -Map 27_torture_chancel_redux -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureChancelRedux.cs`: **0 ошибок, 0 предупреждений**. Компиляция подтверждена Unity-прогонами. Независимое сравнение токенов после перечисленных адаптаций C# не нашло различий; все исходные комментарии присутствуют (`Logs/Ch03_TokenReview.txt`, воспроизводимый диагностический Ch03_TokenReview.py).

Проверены 60 используемых API: все имена и сигнатуры существуют. Полная таблица актуальных сигнатур/файлов и ограничений — `Logs/Ch03_API_Inventory.md`. Существенные заглушки и пустые операции:

| API | Фактический статус |
|---|---|
| `AutoSave` | ✓ (2026-10-17) |
| `ChangePlayerStateToNormal` | ✓ с 2026-10-17: состояние Normal, предмет в руках отпускается |
| `PreloadSound` | пустая операция |

Все отчёты далее — внутри `UnityProj/UMnesiaRTX/Logs/`, вне git. PlayerSimulation: `-batchmode -quit`, GPU, `-seed 1`, шаг 1/60; импорт/аудит — `-batchmode -nographics -quit`. Ни один отчёт с WARNING [Script] ERROR не трактуется как чистый скрипт только из-за 0 errors.

| Отчёт | Итог |
|---|---|
| `Ch03_Import_27_torture_chancel_redux.log/.txt` | ExitCode 0; 3 warnings, 0 errors |
| `Ch03_Sim_27_checkpoints.log/.txt` | ExitCode 0; 1 warnings, 0 errors |
| `Ch03_Sim_27_fall_feedback.log/.txt` | ExitCode 0; 2 warnings, 0 errors |
| `Ch03_Sim_27_machine_first.log/.txt` | ExitCode 0; 4 warnings, 0 errors |
| `Ch03_Sim_27_orb_machine.log/.txt` | ExitCode 0; 6 warnings, 0 errors |
| `Ch03_Sim_27_torture_chancel_redux_start.log/.txt` | ExitCode 0; 1 warnings, 0 errors |
| `Ch03_Audit_27_torture_chancel_redux.log/.txt` | **ExitCode 1, общий аудит не PASS**: Models=133, submeshes=240, geometryFailures=0, prefabMatrixDifferences=7, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2; Scene: instances=1614, submeshMatrices=2046, sharedMeshColliders=1388, failures=62, maxMatrixError=15; Resource regression: checks=471, failures=0 |

62 scene failures разобраны: 12 missing submesh связаны со SkinnedMeshRenderer (10 точных имён и два servant_grunt 1); 50 placement-сообщений возникают из-за поиска первого объекта с одинаковым именем. Независимо сопоставлены все 32 экземпляра труб семи повторяющихся имён: позиции, масштабы, вращения и GUID prefab совпадают с исходными ID/FileIndex. Максимальная ошибка компонента quaternion < 1.8e-7. Данные: Ch03_DuplicateReview.json. Это не исправляет ограниченный аудитор и не превращает его ExitCode в PASS.

### Точные диагностические fixtures

Базовая команда: `Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch03/27_torture_chancel_redux.map -seed 1 -script "<команды>" -logFile <Logs/Ch03_имя.log>`. Флаг scriptDebug включается только там, где указан; глобальные переменные передаются до OnStart. Скриптовые выдачи предметов/снятие таймеров/callbacks — подготовка диагностики, не изменения порта и не доказательство физического решения.

`Ch03_Sim_27_orb_machine`: scriptDebug=false, globals=`(нет)`.

```text
watch 1 1; call CollideSmallObjectWithCogs stone_small01_brown_1 AreaStoneCogs_1 1; watch 10 2; call CollideWeakObjectWithCogs wood_box01_2 AreaStoneCogs_1 1; watch 10 2; call CollideWeakObjectWithCogs flask01_2 AreaStoneCogs_1 1; watch 10 2; call UseOrbPieceOnPedestal OrbPiece_1 orb_pedestal_1; watch 10 2; call UseOrbPieceOnPedestal OrbPiece_2 orb_pedestal_1; watch 10 2; call GetGlobalVarInt OrbsAddedCount; call UseTarOnPedestal bucket_of_tar_1 orb_pedestal_1; watch 10 2; call UseOrbPieceOnPedestal OrbPiece_2 orb_pedestal_1; watch 10 2; call UseOrbPieceOnPedestal OrbPiece_3 orb_pedestal_1; watch 10 2; call UseOrbPieceOnPedestal OrbPiece_4 orb_pedestal_1; watch 10 2; call UseOrbPieceOnPedestal OrbPiece_5 orb_pedestal_1; watch 10 2; call UseOrbPieceOnPedestal OrbPiece_6 orb_pedestal_1; watch 10 2; call GetGlobalVarInt OrbsAddedCount; call CollideCorrectObjectWithCogs storage_box_wood01_2 AreaStoneCogs_1 1; watch 20 2; call GetLocalVarInt MachineJammed; call GetLocalVarInt OrbAbsorbing; body block_box_block_box_1; enemy servant_brute_2; start PS_AreaMachineCP; w 1; s 1; shot orb_and_machine
```

`Ch03_Sim_27_machine_first`: scriptDebug=false, globals=`OrbsAddedCount=5;TarOnPedestal=1`.

```text
call CollideCorrectObjectWithCogs rock_debris_brown01_1 AreaStoneCogs_1 1; watch 10 2; call UseOrbPieceOnPedestal OrbPiece_6 orb_pedestal_1; watch 30 2; call GetLocalVarInt MachineJammed; call GetLocalVarInt OrbAbsorbing; call GetGlobalVarInt OrbsAddedCount; body block_box_block_box_1; shot machine_first
```

`Ch03_Sim_27_checkpoints`: scriptDebug=false, globals=`(нет)`.

```text
call SetEnemyDisableTriggers AreaTunnelCP_Grunt true; call SetEnemyDisableTriggers AreaMachineCP_Grunt true; call CollideAreaCP Player AreaStartCP 1; call GivePlayerDamage 200 claws true true; watch 20 2; health; call GetLocalVarInt AreaStartCP; call CollideAreaCP Player AreaStartCP 1; call GivePlayerDamage 200 claws true true; watch 20 2; health; call GetLocalVarInt AreaStartCP; call CollideAreaCP Player AreaTunnelCP 1; call GivePlayerDamage 200 claws true true; watch 20 2; health; call GetLocalVarInt AreaTunnelCP; call CollideAreaCP Player AreaMachineCP 1; call GivePlayerDamage 200 claws true true; watch 20 2; health; call GetLocalVarInt AreaMachineCP; call CollideAreaCP Player AreaMiddleCP 1; call GivePlayerDamage 200 claws true true; watch 20 2; health; call GetLocalVarInt AreaMiddleCP; call CollideAreaExitGuardian Player AreaExitGuardian 1; watch 10 2; call CollideTunnelDamage Player AreaTunnelDamage 1; watch 1 1; call CollideTunnelDamage Player AreaTunnelDamage -1; watch 10 2; shot checkpoints
```

`Ch03_Sim_27_fall_feedback`: scriptDebug=false, globals=`(нет)`.

```text
call InteractLargeGate level_hub_2; watch 10 2; call InteractMachineRoomDoor sewer_arched_2; watch 10 2; call UseStringOnPedestal string_pile_2 orb_pedestal_1; watch 10 2; call PickDiary note_generic_1 OnPickup; watch 10 2; call PickBucketOfTar bucket_of_tar_1 OnPickup; watch 10 2; call CollideFallDeath Player AreaFallDeath 1; watch 20 2; health; shot fall
```

### Предупреждения прогонов

- [Script] AutoSave: skipped, needs saving (step 6)
- [Script] ERROR: Script function 'CheckPointFallWell' does not exist in TortureChancelRedux
- [Script] ERROR: Could not find light 'barrier_spotlight_2'
- [Script] Entity 'PSOrbStream_1' does not exist!
- [Script] ERROR: Could not find light 'barrier_spotlight_weak_2'
- [Script] ERROR: Script call without a function name
- [Script] CreateEntity 'wood_box01_2_broken': scale (0.80, 0.80, 0.80) is not applied to created entities yet

Это сохранённые исходные обращения, недостающие механики или явно указанная диагностическая подготовка. Для одноразовых/уже завершённых звуков и PS отсутствие записи при Stop/Destroy не трактуется как отсутствующий ресурс. Полный порядок сообщений и состояния находятся в соответствующих .txt. Исключений, PortError, takes N arguments и отсутствующих функций порта не обнаружено; исходные отсутствующие callbacks перечислены отдельно.

### Общие регрессии и границы готовности

До изменений: Ch03_Baseline_MapChange.log — **66 PASS**; Ch03_Baseline_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS**. После всех шести портов: Ch03_Regression_MapChange.log — **66 PASS, ExitCode 0**; Ch03_Regression_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS, ExitCode 0**. Обе Play-регрессии запускались без -quit/-nographics и завершили процесс самостоятельно. Тесты не менялись.

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
