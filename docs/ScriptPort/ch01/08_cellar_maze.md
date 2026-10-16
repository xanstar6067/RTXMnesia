# 08_cellar_maze

- **Оригинал:** `source/maps/main/ch01/08_cellar_maze.hps` (Latin-1, 302 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/CellarMaze.cs`, класс `RTXMnesia.Levels.Ch01.CellarMaze`, атрибут `[HplMapScript("maps/main/ch01/08_cellar_maze.map")]`.
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/08_cellar_maze.unity` (генерируется, вне git).
- **Статус:** Перенесены все 21 функции; проверены старт, восемь основных обработчиков, двери и два реальных возрождения. Полная погоня ограничена отсутствием ИИ. Перенос выполнен в составе пакета 08–14; обе обязательные регрессии прошли. Независимый аудит сцены имеет замечания, разобранные ниже; полное ручное прохождение не заявляется. Дата: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в исходном скрипте нет. Все функции перечислены в исходном порядке; local/global var API хранит значения карты/игры, а не поля класса.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `CollideStopLurker01` (`08_cellar_maze.hps:3`) | `CellarMaze.CollideStopLurker01` | готов | 1:1, исходный порядок и параметры |
| `CollideStopLurker02` (`08_cellar_maze.hps:15`) | `CellarMaze.CollideStopLurker02` | готов | 1:1, исходный порядок и параметры |
| `CollideHitDoorEffects01` (`08_cellar_maze.hps:31`) | `CellarMaze.CollideHitDoorEffects01` | готов | 1:1, исходный порядок и параметры |
| `TimerGuardianCalls` (`08_cellar_maze.hps:46`) | `CellarMaze.TimerGuardianCalls` | готов | 1:1, исходный порядок и параметры |
| `HitDoorEffects` (`08_cellar_maze.hps:60`) | `CellarMaze.HitDoorEffects` | готов | 1:1, исходный порядок и параметры |
| `TimerBreakSounds01` (`08_cellar_maze.hps:68`) | `CellarMaze.TimerBreakSounds01` | готов | 1:1, исходный порядок и параметры |
| `TimerBreakSounds02` (`08_cellar_maze.hps:75`) | `CellarMaze.TimerBreakSounds02` | готов | 1:1, исходный порядок и параметры |
| `TimerBreakSounds03` (`08_cellar_maze.hps:83`) | `CellarMaze.TimerBreakSounds03` | готов | исходное отсутствующее имя коллбэка сохранено |
| `CollideStartLurker01` (`08_cellar_maze.hps:93`) | `CellarMaze.CollideStartLurker01` | готов | 1:1, исходный порядок и параметры |
| `TimerStartSplash` (`08_cellar_maze.hps:115`) | `CellarMaze.TimerStartSplash` | готов | 1:1, исходный порядок и параметры |
| `TimerStartLurker01` (`08_cellar_maze.hps:120`) | `CellarMaze.TimerStartLurker01` | готов | 1:1, исходный порядок и параметры |
| `TimerDamageSanity` (`08_cellar_maze.hps:127`) | `CellarMaze.TimerDamageSanity` | готов | 1:1, исходный порядок и параметры |
| `CollideStartLurker02` (`08_cellar_maze.hps:136`) | `CellarMaze.CollideStartLurker02` | готов | 1:1, исходный порядок и параметры |
| `CollideStartLurker03` (`08_cellar_maze.hps:150`) | `CellarMaze.CollideStartLurker03` | готов | 1:1, исходный порядок и параметры |
| `CollideStartLurker04` (`08_cellar_maze.hps:166`) | `CellarMaze.CollideStartLurker04` | готов | 1:1, исходный порядок и параметры |
| `CheckPoint01` (`08_cellar_maze.hps:184`) | `CellarMaze.CheckPoint01` | готов | сброс дверей, пропов, обеих групп таймеров и повторная регистрация |
| `SetupCollideAreas` (`08_cellar_maze.hps:212`) | `CellarMaze.SetupCollideAreas` | готов | после alCount >= 1 пропускаются первые две погони |
| `TimerPlayerReact` (`08_cellar_maze.hps:233`) | `CellarMaze.TimerPlayerReact` | готов | 1:1, исходный порядок и параметры |
| `OnStart` (`08_cellar_maze.hps:252`) | `CellarMaze.OnStart` | готов | 1:1, исходный порядок и параметры |
| `OnEnter` (`08_cellar_maze.hps:275`) | `CellarMaze.OnEnter` | готов | 1:1, исходный порядок и параметры |
| `OnLeave` (`08_cellar_maze.hps:295`) | `CellarMaze.OnLeave` | готов | очистка подсказки смерти и экран загрузки |

## Отличия от оригинала

Сигнатуры string &in / string & переведены в string; жизненный цикл — public override. Десятичные литералы имеют суффикс f. Статические имена функций в строковых аргументах используют nameof, динамические строки сохранены. Комментарии и порядок функций не менялись.

Дополнительная лексическая сверка после нормализации только разрешённых синтаксических изменений: **1171 одинаковых токенов** действующего кода, сохранены **все 41 исходных комментариев**. Штатный Check-ScriptPort также проверил функции, строковые константы, количество вызовов API, числа и коллбэки карты. Движок, API, импортёр, инструменты и прежние порты/журналы не изменялись.

## Особенности оригинала (сохранены)

- `08_cellar_maze.hps:87`: таймер указывает на `TimerFakeEnemySounds03`, которой нет в исходном скрипте. Строка сохранена; событийный прогон получает исходную ошибку `does not exist in CellarMaze`. Фиктивный обработчик не добавлен.
- `08_cellar_maze.hps:35`, :24 удаляют `FakeBreakSounds01/02`, но повторные таймеры на :72, :79 называются `FakeSounds01/02`. Поэтому уже начавшиеся удары могут продолжаться; имена не исправлены. Чекпоинт удаляет обе группы (:202–:206).
- `SetupCollideAreas` (:216) начиная со второго возрождения пропускает первые две погони. Регистрации оставлены с исходным состоянием 0, а не только входом.
- `CheckPoint01` (:192–:198) содержит пересекающиеся маски кресел и сбрасывает только `bag01*`, а не все мешки; маски не объединены.

## Не перенесено движком

| API NotPorted | skipped, needs | Последствие |
|---|---|---|
| `AutoSave` | — | с 2026-10-17 пишет автосохранение: сохранения перенесены (CLAUDE.md «Сохранения») |

`SetEnemyDisabled` больше не заглушка: водяной монстр перенесён (6.4, CLAUDE.md «Враги» и «Вода»). Прогон 2026-10-16: `CollideStartLurker02` включает `waterlurker_2`, он охотится (вся вода карты на одной высоте с игроком, `HearVolume` = 10 у `waterlurker_music02.ent` — слуха нет), `CollideStopLurker02` останавливает его (`SetEnemyDisabled`: обновление врага пропускается, состояние остаётся Hunt, как в HPL2). Область `Liquid` карты работает: замедление и шаги по воде, плавучесть, всплески. Чекпоинт выключает и сбрасывает врагов.

## Проверка

Unity 6000.6.2f1 запускался последовательно, при отсутствии Temp/UnityLockfile, с доступом к лицензии Windows. Симуляции и обе регрессии — с GPU. Все итоговые симуляции завершились кодом 0, без исключений, PortError и несовпадения числа аргументов. Предупреждения и исходные ошибки перечислены ниже. Новых предупреждений компилятора из семи портов нет; прежние предупреждения других файлов не исправлялись.

Все пути `Logs/...` ниже относятся к `Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/` и не входят в git.

### Импорт

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/08_cellar_maze.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Import_08_cellar_maze.log
```

```text
=== Level: maps/main/ch01/08_cellar_maze.map — 2026-10-16 18:47:19 — 2 warnings, 0 errors ===
Resources: 51 models (2 imported now), 7 extra materials (0 converted now), 2 cookies
WARNING: Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
WARNING: No level script port for maps/main/ch01/08_cellar_maze.map yet (the level runs without its events)
```

Импорт завершён кодом 0. Полный сохранённый отчёт: `Logs/08_cellar_maze_import_report.log`. Предупреждение No level script port относится к импорту до добавления C#; последующие прогоны находят CellarMaze. Сцену автоматически добавил импортёр в EditorBuildSettings.asset; это ожидаемое изменение сохранено.

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 08_cellar_maze -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/CellarMaze.cs
```

```text
08_cellar_maze : функций 21, строк 84, ошибок 0, предупреждений 0
```

### Симуляции и события

Общий запуск (полный путь Unity.exe: `C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe`):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/08_cellar_maze.map -seed 1 -script "$script" -logFile <полный путь отдельного лога>
```

| Итоговый лог | Предупреждения | Ошибки отчёта | Код выхода |
|---|---:|---:|---:|
| `Sim_08_cellar_maze.log` | 1 | 0 | 0 |
| `Sim_08_cellar_maze_events.log` | 3 | 0 | 0 |
| `Sim_08_cellar_maze_walk.log` | 1 | 0 | 0 |

Стартовый сценарий: `watch 30 2; shot start`. Для основных прямых вызовов далее выдержано `watch 10 2`; подготовка, запросы состояния и проверки смерти явно указаны. `ChangeMap` не вызывался. Исходные ошибки HplLog отображаются как WARNING в отчёте симулятора, поэтому счётчик «0 errors» сам по себе их не исключает.

Активированы все четыре waterlurker-сущности; двери 2/3/4 запираются, работают всплески, удары и реакции. Перед летальными повреждениями изолированно сняты девять старых коллбэков: прямой `call` сам их не расходует. После первого возрождения (здоровье около 91.51) переход в StartLurker_1 действительно включает waterlurker_1. После второго (около 81.27) та же область оставляет его неактивным, а StartLurker_3 включает waterlurker_3. Проверены счётчики 0/1 и восстановление дверей. Ожидание после смерти — 20 секунд, чтобы завершились текст и проявление.

В первом пробном прогоне оставленные регистрации дали девять предупреждений о повторных коллбэках; итоговый сценарий изолирует их перед смертью, код порта не менялся.

Воспроизводимые итоговые сценарии:

**events**:

```text
call CollideStartLurker01 Player StartLurker_1 1;
watch 10 2;
body waterlurker_1;
call SetSwingDoorClosed cellar_wood01_2 true true;
call CollideStopLurker01 Player StopLurker_1_1 1;
watch 10 2;
body cellar_wood01_2;
call SetSwingDoorClosed cellar_wood01_3 true true;
call CollideHitDoorEffects01 Player HitDoorEffects01 1;
watch 10 2;
call CollideStartLurker02 Player StartLurker_2 1;
watch 10 2;
body waterlurker_2;
call SetSwingDoorClosed cellar_wood01_4 true true;
call CollideStopLurker02 Player StopLurker_2_1 1;
watch 10 2;
body cellar_wood01_4;
call CollideStartLurker03 Player StartLurker_3 1;
watch 10 2;
body waterlurker_3;
call CollideStartLurker04 Player StartLurker_4 1;
watch 10 2;
body waterlurker_4;
call RemoveEntityCollideCallback Player StartLurker_1;
call RemoveEntityCollideCallback Player StartLurker_2;
call RemoveEntityCollideCallback Player StartLurker_3;
call RemoveEntityCollideCallback Player StartLurker_4;
call RemoveEntityCollideCallback Player HitDoorEffects01;
call RemoveEntityCollideCallback Player StopLurker_1_1;
call RemoveEntityCollideCallback Player StopLurker_1_2;
call RemoveEntityCollideCallback Player StopLurker_2_1;
call RemoveEntityCollideCallback Player StopLurker_2_2;
call GivePlayerDamage 500 BloodSplat true true;
watch 20 2;
health;
body cellar_wood01_2;
call SetEntityActive waterlurker_1 false;
start StartLurker_1;
watch 10 2;
body waterlurker_1;
call RemoveEntityCollideCallback Player StartLurker_1;
call RemoveEntityCollideCallback Player StartLurker_2;
call RemoveEntityCollideCallback Player StartLurker_3;
call RemoveEntityCollideCallback Player StartLurker_4;
call RemoveEntityCollideCallback Player HitDoorEffects01;
call RemoveEntityCollideCallback Player StopLurker_1_1;
call RemoveEntityCollideCallback Player StopLurker_1_2;
call RemoveEntityCollideCallback Player StopLurker_2_1;
call RemoveEntityCollideCallback Player StopLurker_2_2;
call GivePlayerDamage 500 BloodSplat true true;
watch 20 2;
health;
call SetEntityActive waterlurker_1 false;
start StartLurker_1;
watch 10 2;
body waterlurker_1;
call SetEntityActive waterlurker_3 false;
start StartLurker_3;
watch 10 2;
body waterlurker_3;
call OnLeave;
watch 10 2;
shot events;
```

Все уникальные предупреждения итоговых симуляций (время удалено, повторы объединены):

```text
WARNING: [Script] AutoSave: skipped, needs saving (step 6)
WARNING: [Script] SetEnemyDisabled: skipped, needs enemies (step 6)
WARNING: [Script] ERROR: Script function 'TimerFakeEnemySounds03' does not exist in CellarMaze
```

### Геометрия, пути моделей и движение

Дополнительная независимая проверка существующим инструментом, без изменений импортёра:

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.HplMeshAudit.Run -amnesiaMap maps/main/ch01/08_cellar_maze.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Audit_08_cellar_maze.log
```

```text
Models=54, submeshes=97, geometryFailures=0, prefabMatrixDifferences=5, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2
Scene: instances=1093, submeshMatrices=1464, sharedMeshColliders=609, failures=34, maxMatrixError=3,8147E-06
Resource regression: checks=471, failures=0
```

**Общий аудит завершён кодом 1, не PASS.** Геометрия проверенных моделей совпала с MSH (geometryFailures=0, maxVertexError=0), кэши найдены, 471 проверка выбора ресурсов прошла. Замечания Scene и различия матриц исходных префабов не скрыты. Полный отчёт: `Logs/Audit_08_cellar_maze.txt`.

Все 34 замечания Scene — MISSING ENTITY SUBMESH для анимированной слизи. В сериализованной сцене такие узлы содержат SkinnedMeshRenderer (тип Unity 137), а аудитор HplMeshAudit.Run проверяет только MeshFilter. Поэтому этот результат не доказывает отсутствие слизи; ограничения аудитора не исправлялись. Численные матрицы проверенных узлов совпадают в пределах 0.001.

Отдельный GPU-прогон движения: `watch 1 1; w 1; s 1; health; shot entry_walk`. Проверен короткий проход у начальной точки, не весь маршрут. Результат:

```text
t=  2,00 w 1              feet (-10.120, 0.005, 16.500) y 0,005..0,005 max 2,50 m/s now 2,50 ground True size 0 cam up 1,708 side -0,025 roll 0,0 | Normal crosshair Default text ""
t=  3,00 s 1              feet (-11.972, 0.005, 16.500) y 0,005..0,005 max 2,00 m/s now 2,00 ground True size 0 cam up 1,726 side 0,019 roll 0,0 | Normal crosshair Default text ""
t=  3,00   health 100,00 sanity 100,00 dead False pos (-11.97, 0.90, 16.50) roll 0,0 pitch 0,0 yaw 90,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathWaterLurker03 | check point check01 at PlayerStartArea_1 count 0
```

Просмотрен контрольный `Logs/Player_08_cellar_maze_00_start.png`: Затопленный коридор со слизью и свечами. Снимки сценариев и прохода также сохранены в Logs. Специальный обход всех помещений и исправление замечаний геометрического аудитора в задачу переноса скриптов не включались.

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
