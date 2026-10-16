# 11_study

- **Оригинал:** `source/maps/main/ch01/11_study.hps` (Latin-1, 442 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Study.cs`, класс `RTXMnesia.Levels.Ch01.Study`, атрибут `[HplMapScript("maps/main/ch01/11_study.map")]`.
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/11_study.unity` (генерируется, вне git).
- **Статус:** Перенесены все 34 функции; проверены окно, кости, оба события дерева, предметы и шесть настоящих возрождений на двух сторонах. Перенос выполнен в составе пакета 08–14; обе обязательные регрессии прошли. Независимый аудит сцены имеет замечания, разобранные ниже; полное ручное прохождение не заявляется. Дата: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в исходном скрипте нет. Все функции перечислены в исходном порядке; local/global var API хранит значения карты/игры, а не поля класса.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `BreakWindow` (`11_study.hps:3`) | `Study.BreakWindow` | готов | 1:1, исходный порядок и параметры |
| `TimerBreakWindow` (`11_study.hps:16`) | `Study.TimerBreakWindow` | готов | 1:1, исходный порядок и параметры |
| `InteractWindow` (`11_study.hps:21`) | `Study.InteractWindow` | готов | пустой таймер и счётчик трёх быстрых ударов |
| `TimerDecreaseVar` (`11_study.hps:44`) | `Study.TimerDecreaseVar` | готов | 1:1, исходный порядок и параметры |
| `CollideFallDeath` (`11_study.hps:60`) | `Study.CollideFallDeath` | готов | 1:1, исходный порядок и параметры |
| `TimerFallDeath` (`11_study.hps:70`) | `Study.TimerFallDeath` | готов | обе стадии, отключение стартового звука и летальный урон |
| `CollideAreaDesk` (`11_study.hps:90`) | `Study.CollideAreaDesk` | готов | 1:1, исходный порядок и параметры |
| `TimerPlayerReact` (`11_study.hps:107`) | `Study.TimerPlayerReact` | готов | 1:1, исходный порядок и параметры |
| `CollideForestThings` (`11_study.hps:117`) | `Study.CollideForestThings` | готов | обе ветки, таймеры дерева и исходное увеличение счётчика |
| `TimerPlayerSanity` (`11_study.hps:153`) | `Study.TimerPlayerSanity` | готов | 1:1, исходный порядок и параметры |
| `TimerPlayerReact02` (`11_study.hps:158`) | `Study.TimerPlayerReact02` | готов | 1:1, исходный порядок и параметры |
| `TimerFreezeTree` (`11_study.hps:169`) | `Study.TimerFreezeTree` | готов | 1:1, исходный порядок и параметры |
| `CheckHaveAllItems` (`11_study.hps:177`) | `Study.CheckHaveAllItems` | готов | 1:1, исходный порядок и параметры |
| `GotManual` (`11_study.hps:196`) | `Study.GotManual` | готов | 1:1, исходный порядок и параметры |
| `GotRod` (`11_study.hps:203`) | `Study.GotRod` | готов | 1:1, исходный порядок и параметры |
| `PickAgrippaNote` (`11_study.hps:215`) | `Study.PickAgrippaNote` | готов | 1:1, исходный порядок и параметры |
| `CollideCheckSouth` (`11_study.hps:227`) | `Study.CollideCheckSouth` | готов | 1:1, исходный порядок и параметры |
| `CollideCheckNorth` (`11_study.hps:232`) | `Study.CollideCheckNorth` | готов | 1:1, исходный порядок и параметры |
| `CheckPointSouth` (`11_study.hps:238`) | `Study.CheckPointSouth` | готов | облегчённая точка при alCount >= 2 |
| `CheckPointNorth` (`11_study.hps:248`) | `Study.CheckPointNorth` | готов | облегчённая точка при alCount >= 2 |
| `TimerBreakWindowWithNoFullSearch` (`11_study.hps:263`) | `Study.TimerBreakWindowWithNoFullSearch` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaBreakWindow` (`11_study.hps:273`) | `Study.CollideAreaBreakWindow` | готов | 1:1, исходный порядок и параметры |
| `TimerGlassEffects` (`11_study.hps:294`) | `Study.TimerGlassEffects` | готов | 1:1, исходный порядок и параметры |
| `CollideFlashBack` (`11_study.hps:323`) | `Study.CollideFlashBack` | готов | 1:1, исходный порядок и параметры |
| `TimerStartMusicDog` (`11_study.hps:327`) | `Study.TimerStartMusicDog` | готов | 1:1, исходный порядок и параметры |
| `TimerStopMusicDog` (`11_study.hps:333`) | `Study.TimerStopMusicDog` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaCorridorQuest` (`11_study.hps:338`) | `Study.CollideAreaCorridorQuest` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaFadeOut` (`11_study.hps:346`) | `Study.CollideAreaFadeOut` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaRoom` (`11_study.hps:353`) | `Study.CollideAreaRoom` | готов | 1:1, исходный порядок и параметры |
| `CheckIfAllRoomSearched` (`11_study.hps:358`) | `Study.CheckIfAllRoomSearched` | готов | сравнение строго с 4 |
| `CollideAreaBreakWindowOnClose` (`11_study.hps:371`) | `Study.CollideAreaBreakWindowOnClose` | готов | 1:1, исходный порядок и параметры |
| `OnStart` (`11_study.hps:386`) | `Study.OnStart` | готов | 1:1, исходный порядок и параметры |
| `OnEnter` (`11_study.hps:426`) | `Study.OnEnter` | готов | 1:1, исходный порядок и параметры |
| `OnLeave` (`11_study.hps:439`) | `Study.OnLeave` | готов | 1:1, исходный порядок и параметры |

## Отличия от оригинала

Сигнатуры string &in / string & переведены в string; жизненный цикл — public override. Десятичные литералы имеют суффикс f. Статические имена функций в строковых аргументах используют nameof, динамические строки сохранены. Комментарии и порядок функций не менялись.

Дополнительная лексическая сверка после нормализации только разрешённых синтаксических изменений: **2077 одинаковых токенов** действующего кода, сохранены **все 52 исходных комментариев**. Штатный Check-ScriptPort также проверил функции, строковые константы, количество вызовов API, числа и коллбэки карты. Движок, API, импортёр, инструменты и прежние порты/журналы не изменялись.

## Особенности оригинала (сохранены)

- `11_study.map:7097`, :7256: candlestick02_10/13 задают CallbackFunc=LightAnimalLamp. Такой функции нет в исходном .hps. Это единственное предупреждение штатной сверки; функция не выдумана.
- `11_study.hps:30`: таймер WindowMessagePaused содержит пустое имя коллбэка. По истечении 1 секунды возникает исходный `Script call without a function name`; пустая строка сохранена.
- `InteractWindow` (:33–:38) требует ровно трёх быстрых ударов до срабатывания отдельных таймеров уменьшения счётчика. Одно нажатие и прямой BreakWindow проверены; три быстрых нажатия вручную отдельно не проверялись.
- `CollideAreaDesk` (:92–:98) задаёт fMin=−300, fMax=−500 и передаёт их в RandFloat в этом порядке. Границы не переставлены.
- Вторая ветка CollideForestThings увеличивает ForestThing до 2 (:147), а TimerPlayerReact02 (:163) проверяет ровно 1. Условие включения 11_event_tree не «исправлено».
- `CheckIfAllRoomSearched` (:362) сравнивает счётчик ровно с 4, таймер на :267 сначала задаёт 3. Исходные равенства сохранены.
- Южная Easy-точка находится у северной стороны, северная — у южной: координаты `.map` не исправлялись. После облегчённого телепорта игрок естественно активирует чекпоинт противоположной стороны.

## Не перенесено движком

| API NotPorted | skipped, needs | Последствие |
|---|---|---|
| `AutoSave` | `saving (step 6)` | нет сохранения на диск |

Смерть, отключение звука её начала, урон и возрождения уже реализованы и здесь проверены. Insanity-области пока не поддерживают события. Предупреждения об окне/картинах при импорте отмечены ниже; полная геометрическая исправность этих ресурсов не следует из успешного переноса скрипта.

## Проверка

Unity 6000.6.2f1 запускался последовательно, при отсутствии Temp/UnityLockfile, с доступом к лицензии Windows. Симуляции и обе регрессии — с GPU. Все итоговые симуляции завершились кодом 0, без исключений, PortError и несовпадения числа аргументов. Предупреждения и исходные ошибки перечислены ниже. Новых предупреждений компилятора из семи портов нет; прежние предупреждения других файлов не исправлялись.

Все пути `Logs/...` ниже относятся к `Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/` и не входят в git.

### Импорт

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/11_study.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Import_11_study.log
```

```text
=== Level: maps/main/ch01/11_study.map — 2026-10-16 18:48:57 — 9 warnings, 0 errors ===
Resources: 191 models (44 imported now), 13 extra materials (0 converted now), 2 cookies
WARNING: 'FogArea' is not supported yet (skipped)
WARNING: entities/obstacles/mansionbase_window/mb_window.ent: sub mesh 'polySurface640' not found in the model
WARNING: entities/obstacles/mansionbase_window/mb_window.ent: sub mesh 'polySurface680' not found in the model
WARNING: entities/obstacles/mansionbase_window/mb_window.ent: could not find child with unique ID 20 to attach to body 'Body_1'
WARNING: entities/obstacles/mansionbase_window/mb_window.ent: could not find child with unique ID 22 to attach to body 'Body_1'
WARNING: entities/ornament/paintings/painting_portrait/painting_portrait.ent: sub mesh 'painting_image02' not found in the model
WARNING: entities/ornament/paintings/painting_portrait/painting_portrait.ent: sub mesh 'painting_image02' not found in the model
WARNING: Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
WARNING: No level script port for maps/main/ch01/11_study.map yet (the level runs without its events)
```

Импорт завершён кодом 0. Полный сохранённый отчёт: `Logs/11_study_import_report.log`. Предупреждение No level script port относится к импорту до добавления C#; последующие прогоны находят Study. Сцену автоматически добавил импортёр в EditorBuildSettings.asset; это ожидаемое изменение сохранено.

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 11_study -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Study.cs
```

```text
предупреждение: коллбэк карты LightAnimalLamp нет и в оригинале .hps (ошибка оригинала, в порт не добавлять)

11_study : функций 34, строк 148, ошибок 0, предупреждений 1
```

### Симуляции и события

Общий запуск (полный путь Unity.exe: `C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe`):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/11_study.map -seed 1 -script "$script" -logFile <полный путь отдельного лога>
```

| Итоговый лог | Предупреждения | Ошибки отчёта | Код выхода |
|---|---:|---:|---:|
| `Sim_11_study.log` | 1 | 0 | 0 |
| `Sim_11_study_events.log` | 2 | 0 | 0 |
| `Sim_11_study_checkpoints.log` | 1 | 0 | 0 |
| `Sim_11_study_walk.log` | 1 | 0 | 0 |

Стартовый сценарий: `watch 30 2; shot start`. Для основных прямых вызовов далее выдержано `watch 10 2`; подготовка, запросы состояния и проверки смерти явно указаны. `ChangeMap` не вызывался. Исходные ошибки HplLog отображаются как WARNING в отчёте симулятора, поэтому счётчик «0 errors» сам по себе их не исключает.

Четыре регистрации посещений дают SeenAllRooms=4 и открывают области события окна. Звуки, частицы, свет и завершение 11Path отрабатывают; сохранён пустой коллбэк таймера. Кости получают силы, первое событие дерева замораживает его; GotManual и GotRod устанавливают инструкции/стержень и завершение 10/11=1. Повторный лесной триггер даёт ForestThing=2 и выключает дерево через 10 секунд.

Дополнительный прогон: по три смерти для South и North. Первая смерть South вызвана исходным CollideFallDeath (две стадии звуков и AddPlayerHealth −200), остальные — GivePlayerDamage 500. После каждой — watch 20 2. Счётчики 0/1 возвращают на обычную точку; при 2 телепортируют на Easy. Позиции тела: South около (12.50,0.90,−7.75), его Easy (15.00,0.67,−27.50); North около (12.50,0.90,−27.50), его Easy (15.00,0.67,−7.75). Все шесть раз dead False, здоровье около 52; дополнительные ошибки отсутствуют.

Воспроизводимые итоговые сценарии:

**events**:

```text
call CollideAreaCorridorQuest Player AreaCorridorQuest 1;
watch 10 2;
call CollideAreaRoom Player AreaRoom1 1;
watch 10 2;
call CollideAreaRoom Player AreaRoom2 1;
watch 10 2;
call CollideAreaRoom Player AreaRoom3 1;
watch 10 2;
call GetLocalVarInt SeenAllRooms;
call CollideAreaBreakWindow Player AreaBreakWindow 1;
watch 10 2;
call InteractWindow mansionbase_large_window_breakable_1;
watch 10 2;
call BreakWindow mansionbase_large_window_breakable_1 OnBreak;
watch 10 2;
call CollideAreaDesk work_desk_door_1 AreaDesk 1;
watch 10 2;
call CollideForestThings Player AreaForestThings 1;
watch 10 2;
call GotManual note_generic_3 OnPickup;
watch 10 2;
call GotRod guiding_rod03 OnPickup;
watch 10 2;
call GetGlobalVarInt CompletedLevel_10_11;
call CollideForestThings Player AreaForestThings 1;
watch 10 2;
call GetLocalVarInt ForestThing;
body falling_tree_1;
shot events;
```

**checkpoints**:

```text
call CollideCheckSouth Player AreaCheckSouth 1;
watch 10 2;
call CollideFallDeath Player AreaFallDeath 1;
watch 20 2;
health;
call GivePlayerDamage 500 BloodSplat true true;
watch 20 2;
health;
call GivePlayerDamage 500 BloodSplat true true;
watch 20 2;
health;
call CollideCheckNorth Player AreaCheckNorth 1;
watch 10 2;
call GivePlayerDamage 500 BloodSplat true true;
watch 20 2;
health;
call GivePlayerDamage 500 BloodSplat true true;
watch 20 2;
health;
call GivePlayerDamage 500 BloodSplat true true;
watch 20 2;
health;
shot checkpoints;
```

Все уникальные предупреждения итоговых симуляций (время удалено, повторы объединены):

```text
WARNING: [Script] AutoSave: skipped, needs saving (step 6)
WARNING: [Script] ERROR: Script call without a function name
```

### Геометрия, пути моделей и движение

Дополнительная независимая проверка существующим инструментом, без изменений импортёра:

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.HplMeshAudit.Run -amnesiaMap maps/main/ch01/11_study.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Audit_11_study.log
```

```text
Models=203, submeshes=312, geometryFailures=0, prefabMatrixDifferences=8, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2
Scene: instances=1187, submeshMatrices=1330, sharedMeshColliders=522, failures=4, maxMatrixError=3,8147E-06
Resource regression: checks=471, failures=0
```

**Общий аудит завершён кодом 1, не PASS.** Геометрия проверенных моделей совпала с MSH (geometryFailures=0, maxVertexError=0), кэши найдены, 471 проверка выбора ресурсов прошла. Замечания Scene и различия матриц исходных префабов не скрыты. Полный отчёт: `Logs/Audit_11_study.txt`.

Четыре замечания Scene совпадают с импортом: два ожидаемых подмеша windowb_1 (polySurface640/680) и два упоминания painting_portrait_1/painting_image02 не найдены. Аудит не подтверждает полную корректность этих объектов, несмотря на совпавшую геометрию моделей с MSH. Корневая причина различия имён в .ent и модели не исправлялась. Численные матрицы остальных проверенных узлов совпали в пределах 0.001.

Отдельный GPU-прогон движения: `watch 1 1; w 1; s 1; health; shot entry_walk`. Проверен короткий проход у начальной точки, не весь маршрут. Результат:

```text
t=  2,00 w 1              feet (-4.500, 0.022, 8.620) y 0,022..0,022 max 2,50 m/s now 2,50 ground True size 0 cam up 1,708 side -0,025 roll 0,0 | Normal crosshair Default text ""
t=  3,00 s 1              feet (-4.500, 0.022, 10.472) y 0,022..0,022 max 2,00 m/s now 2,00 ground True size 0 cam up 1,726 side 0,019 roll 0,0 | Normal crosshair Default text ""
t=  3,00   health 100,00 sanity 100,00 dead False pos (-4.50, 0.92, 10.47) roll 0,0 pitch 0,0 yaw -180,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Просмотрен контрольный `Logs/Player_11_study_00_start.png`: Тёмный коридор кабинета со свечами и картинами. Снимки сценариев и прохода также сохранены в Logs. Специальный обход всех помещений и исправление замечаний геометрического аудитора в задачу переноса скриптов не включались.

### Регрессия пакета 08–14

Оба запуска — без -quit и -nographics, после добавления всех семи портов; последующие контрольные повторы не меняли код:

```text
Unity.exe -batchmode -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MapChangePreview.RunBatch -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Regression_08_14_MapChange.log
Unity.exe -batchmode -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MainMenuPreview.RunBatch -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Regression_08_14_MainMenu.log
```

```text
PASS: 66 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian, damage and death); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 14 main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).
PASS: 14 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

Оба процесса завершены кодом 0. Проверки не менялись; ожидаемые 66 учтены.

## Файлы и состояние репозитория

В пакете созданы семь портов (.cs + сгенерированные Unity .meta) и семь журналов 08–14. В CLAUDE.md/AGENTS.md изменена только строка этапа 5; файлы побайтно одинаковы. Импортёр добавил семь сцен в EditorBuildSettings.asset. Windows.asset не изменился. Автоматическое пересохранение TimeManager.asset возвращено командой `git checkout -- UnityProj/UMnesiaRTX/ProjectSettings/TimeManager.asset` после закрытия Unity. Остальные настройки, готовые порты и журналы не менялись. Ресурсы Amnesia остаются в игнорируемых каталогах. Коммитов и push нет.

Итоговый `git status --short` всего пакета (Unity закрыт, lock-файла нет):

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/BackHall.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/BackHall.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/CellarMaze.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/CellarMaze.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/DanielsRoom.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/DanielsRoom.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Elevator.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Elevator.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/MachineRoom.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/MachineRoom.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Storage.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Storage.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Study.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Study.cs.meta
?? docs/ScriptPort/ch01/08_cellar_maze.md
?? docs/ScriptPort/ch01/09_back_hall.md
?? docs/ScriptPort/ch01/10_daniels_room.md
?? docs/ScriptPort/ch01/11_study.md
?? docs/ScriptPort/ch01/12_storage.md
?? docs/ScriptPort/ch01/13_machine_room.md
?? docs/ScriptPort/ch01/14_elevator.md
```
