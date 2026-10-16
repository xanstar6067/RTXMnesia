# 10_daniels_room

- **Оригинал:** `source/maps/main/ch01/10_daniels_room.hps` (Latin-1, 404 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/DanielsRoom.cs`, класс `RTXMnesia.Levels.Ch01.DanielsRoom`, атрибут `[HplMapScript("maps/main/ch01/10_daniels_room.map")]`.
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/10_daniels_room.unity` (генерируется, вне git).
- **Статус:** Перенесены все 34 функции; проверены дверь и лом, картина, реальное разрушение ёмкости с ключом, события монстра и два возрождения. Перенос выполнен в составе пакета 08–14; обе обязательные регрессии прошли. Независимый аудит сцены имеет замечания, разобранные ниже; полное ручное прохождение не заявляется. Дата: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в исходном скрипте нет. Все функции перечислены в исходном порядке; local/global var API хранит значения карты/игры, а не поля класса.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `BreakBottle` (`10_daniels_room.hps:1`) | `DanielsRoom.BreakBottle` | готов | квест, взгляд на созданный ключ, порядок завершения уровней |
| `TimerStopLook` (`10_daniels_room.hps:22`) | `DanielsRoom.TimerStopLook` | готов | 1:1, исходный порядок и параметры |
| `InteractClosedDoor` (`10_daniels_room.hps:30`) | `DanielsRoom.InteractClosedDoor` | готов | 1:1, исходный порядок и параметры |
| `TimerDoorMessage` (`10_daniels_room.hps:34`) | `DanielsRoom.TimerDoorMessage` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaGruntInCloset` (`10_daniels_room.hps:42`) | `DanielsRoom.CollideAreaGruntInCloset` | готов | 1:1, исходный порядок и параметры |
| `GiveEnemyExitPath` (`10_daniels_room.hps:50`) | `DanielsRoom.GiveEnemyExitPath` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaGrunt02` (`10_daniels_room.hps:59`) | `DanielsRoom.CollideAreaGrunt02` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaGrunt` (`10_daniels_room.hps:72`) | `DanielsRoom.CollideAreaGrunt` | готов | 1:1, исходный порядок и параметры |
| `TimerActivateGrunt` (`10_daniels_room.hps:85`) | `DanielsRoom.TimerActivateGrunt` | готов | 1:1, исходный порядок и параметры |
| `TimerExitGrunt` (`10_daniels_room.hps:112`) | `DanielsRoom.TimerExitGrunt` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaCheckPlayer` (`10_daniels_room.hps:117`) | `DanielsRoom.CollideAreaCheckPlayer` | готов | обе одинаковые исходные ветки сохранены |
| `CollideAreaGruntDeactivate` (`10_daniels_room.hps:124`) | `DanielsRoom.CollideAreaGruntDeactivate` | готов | 1:1, исходный порядок и параметры |
| `UseCrowbarOnDoor` (`10_daniels_room.hps:148`) | `DanielsRoom.UseCrowbarOnDoor` | готов | исходный таймер TimerSwitchShovel и удаление предмета |
| `TimerSwitchShovel` (`10_daniels_room.hps:159`) | `DanielsRoom.TimerSwitchShovel` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaBreakDoor` (`10_daniels_room.hps:165`) | `DanielsRoom.CollideAreaBreakDoor` | готов | 1:1, исходный порядок и параметры |
| `TimerPushDoor` (`10_daniels_room.hps:189`) | `DanielsRoom.TimerPushDoor` | готов | 1:1, исходный порядок и параметры |
| `TimerDoorCanClose` (`10_daniels_room.hps:195`) | `DanielsRoom.TimerDoorCanClose` | готов | 1:1, исходный порядок и параметры |
| `TimerDanielVoices` (`10_daniels_room.hps:203`) | `DanielsRoom.TimerDanielVoices` | готов | 1:1, исходный порядок и параметры |
| `VoiceOver1` (`10_daniels_room.hps:209`) | `DanielsRoom.VoiceOver1` | готов | 1:1, исходный порядок и параметры |
| `VoiceOver2` (`10_daniels_room.hps:213`) | `DanielsRoom.VoiceOver2` | готов | 1:1, исходный порядок и параметры |
| `DiaryCall` (`10_daniels_room.hps:220`) | `DanielsRoom.DiaryCall` | готов | 1:1, исходный порядок и параметры |
| `CheckPoint01` (`10_daniels_room.hps:228`) | `DanielsRoom.CheckPoint01` | готов | сброс пропов и установка второго чекпоинта |
| `TimerAreaOnAgain` (`10_daniels_room.hps:255`) | `DanielsRoom.TimerAreaOnAgain` | готов | 1:1, исходный порядок и параметры |
| `CPCall02` (`10_daniels_room.hps:261`) | `DanielsRoom.CPCall02` | готов | взгляд, туман и труп |
| `TimerStopLookCP` (`10_daniels_room.hps:274`) | `DanielsRoom.TimerStopLookCP` | готов | 1:1, исходный порядок и параметры |
| `LookAtCorpse` (`10_daniels_room.hps:278`) | `DanielsRoom.LookAtCorpse` | готов | исчезновение трупа и удаление двух частиц |
| `LookAtPainting` (`10_daniels_room.hps:291`) | `DanielsRoom.LookAtPainting` | готов | 1:1, исходный порядок и параметры |
| `TimerShakePainting` (`10_daniels_room.hps:311`) | `DanielsRoom.TimerShakePainting` | готов | 1:1, исходный порядок и параметры |
| `CollideMovePainting` (`10_daniels_room.hps:318`) | `DanielsRoom.CollideMovePainting` | готов | 1:1, исходный порядок и параметры |
| `InteractAreaBed` (`10_daniels_room.hps:329`) | `DanielsRoom.InteractAreaBed` | готов | 1:1, исходный порядок и параметры |
| `EntityCallGrunt` (`10_daniels_room.hps:335`) | `DanielsRoom.EntityCallGrunt` | готов | 1:1, исходный порядок и параметры |
| `OnStart` (`10_daniels_room.hps:342`) | `DanielsRoom.OnStart` | готов | 1:1, исходный порядок и параметры |
| `OnEnter` (`10_daniels_room.hps:383`) | `DanielsRoom.OnEnter` | готов | 1:1, исходный порядок и параметры |
| `OnLeave` (`10_daniels_room.hps:397`) | `DanielsRoom.OnLeave` | готов | 1:1, исходный порядок и параметры |

## Отличия от оригинала

Сигнатуры string &in / string & переведены в string; жизненный цикл — public override. Десятичные литералы имеют суффикс f. Статические имена функций в строковых аргументах используют nameof, динамические строки сохранены. Комментарии и порядок функций не менялись.

Дополнительная лексическая сверка после нормализации только разрешённых синтаксических изменений: **2016 одинаковых токенов** действующего кода, сохранены **все 34 исходных комментариев**. Штатный Check-ScriptPort также проверил функции, строковые константы, количество вызовов API, числа и коллбэки карты. Движок, API, импортёр, инструменты и прежние порты/журналы не изменялись.

## Особенности оригинала (сохранены)

- `10_daniels_room.hps:121–:122`: обе ветки проверки alState записывают одно и то же значение PlayerInArea. Ветки не объединены.
- Исходные названия TimerSwitchShovel и voice-событий и все неиспользуемые/закомментированные вызовы сохранены.
- `BreakBottle` (:17–:18) записывает порядок завершения пары карт 10/11 только при нулевой глобальной переменной; повторный вызов не увеличивает его.
- В `.map` нет готовой сущности key_jar_1_item: она создаётся разрушением key_jar_1 с ContainedItem=key_study. Прямой вызов BreakBottle на целой ёмкости не создаёт ключ и даёт диагностическое предупреждение look-at; итоговый прогон сначала действительно разбивает ёмкость.
- `CheckPoint01` (:248) устанавливает check02 только в первой ветке. Повторные таймеры с именем impulse (:300–:303), пустая VoiceOver1 и действия VoiceOver2 и исходные интервалы оставлены.

## Не перенесено движком

| API NotPorted | skipped, needs | Последствие |
|---|---|---|
| `AutoSave` | `saving (step 6)` | нет сохранения на диск |
| `ClearEnemyPatrolNodes` | `enemies (step 6)` | маршрут монстра не очищается |
| `AddEnemyPatrolNode` | `enemies (step 6)` | монстр не следует заданному маршруту |

Переключение активности монстра и связанные флаги работают; полноценного ИИ нет. Insanity-области пока не исполняют события. Прямой вызов события взлома подтверждает скрипт, но не заменяет отдельную ручную проверку физического движения лома.

## Проверка

Unity 6000.6.2f1 запускался последовательно, при отсутствии Temp/UnityLockfile, с доступом к лицензии Windows. Симуляции и обе регрессии — с GPU. Все итоговые симуляции завершились кодом 0, без исключений, PortError и несовпадения числа аргументов. Предупреждения и исходные ошибки перечислены ниже. Новых предупреждений компилятора из семи портов нет; прежние предупреждения других файлов не исправлялись.

Все пути `Logs/...` ниже относятся к `Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/` и не входят в git.

### Импорт

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/10_daniels_room.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Import_10_daniels_room.log
```

```text
=== Level: maps/main/ch01/10_daniels_room.map — 2026-10-16 18:48:29 — 2 warnings, 0 errors ===
Resources: 109 models (16 imported now), 9 extra materials (0 converted now), 2 cookies
WARNING: Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
WARNING: No level script port for maps/main/ch01/10_daniels_room.map yet (the level runs without its events)
```

Импорт завершён кодом 0. Полный сохранённый отчёт: `Logs/10_daniels_room_import_report.log`. Предупреждение No level script port относится к импорту до добавления C#; последующие прогоны находят DanielsRoom. Сцену автоматически добавил импортёр в EditorBuildSettings.asset; это ожидаемое изменение сохранено.

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 10_daniels_room -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/DanielsRoom.cs
```

```text
10_daniels_room : функций 34, строк 168, ошибок 0, предупреждений 0
```

### Симуляции и события

Общий запуск (полный путь Unity.exe: `C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe`):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/10_daniels_room.map -seed 1 -script "$script" -logFile <полный путь отдельного лога>
```

| Итоговый лог | Предупреждения | Ошибки отчёта | Код выхода |
|---|---:|---:|---:|
| `Sim_10_daniels_room.log` | 1 | 0 | 0 |
| `Sim_10_daniels_room_events.log` | 3 | 0 | 0 |
| `Sim_10_daniels_room_walk.log` | 1 | 0 | 0 |

Стартовый сценарий: `watch 30 2; shot start`. Для основных прямых вызовов далее выдержано `watch 10 2`; подготовка, запросы состояния и проверки смерти явно указаны. `ChangeMap` не вызывался. Исходные ошибки HplLog отображаются как WARNING в отчёте симулятора, поэтому счётчик «0 errors» сам по себе их не исключает.

Дверь mansion_1 отпирается и толкается, шарнирный лом заменяется динамическим, квест двери завершается. VoiceOver2 даёт VisionOver=1; LookAtPainting запускает импульсы и переводит его в 2. Перемещение картины включает следующую flashback-область.

SetPropHealth key_jar_1 0 действительно разрушает ёмкость: GetEntityExists key_jar_1_item = True, естественный BreakBottle срабатывает, глобальное завершение 10/11 = 1. Дополнительный прямой вызов обработчика сохраняет 1. После события монстра PlayerInArea=1 позволяет отключить его и установить GruntLeftGuestRoom=1.

Перед двумя реальными смертями сняты уже зарегистрированные коллбэки для изоляции проверки. Первый респавн задаёт check02; второй переносит к PlayerStartArea_3 и выполняет CPCall02 (туман, труп, look-at). После LookAtCorpse труп имеет active False. Здоровье после ожидания — около 52, оба раза dead False. В первом пробном сценарии прямой вызов AreaGrunt оставлял исходный триггер нерасходованным; в итоговом он снят перед вызовом, порт не менялся.

Воспроизводимые итоговые сценарии:

**events**:

```text
call InteractClosedDoor mansion_1;
watch 10 2;
call UseCrowbarOnDoor crowbar_1 mansion_1;
watch 10 2;
body crowbar_joint_1;
call CollideAreaBreakDoor crowbar_joint_1 BreakDoor 1;
watch 10 2;
body mansion_1;
call VoiceOver2;
watch 10 2;
call LookAtPainting painting03_dynamic_1 1;
watch 10 2;
call GetLocalVarInt VisionOver;
call CollideMovePainting ScriptArea_4 painting03_dynamic_1 -1;
watch 10 2;
call SetPropHealth key_jar_1 0;
watch 1 1;
call GetEntityExists key_jar_1_item;
call BreakBottle key_jar_1 OnBreak;
watch 10 2;
call GetGlobalVarInt CompletedLevel_10_11;
call RemoveEntityCollideCallback Player AreaGrunt;
call CollideAreaGrunt Player AreaGrunt 1;
watch 10 2;
body grunt_normal_1;
call CollideAreaCheckPlayer Player AreaCheckPlayer 1;
watch 10 2;
call CollideAreaGruntDeactivate grunt_normal_1 AreaGruntDeactivate 1;
watch 10 2;
call GetGlobalVarInt GruntLeftGuestRoom;
call RemoveEntityCollideCallback grunt_normal_1 AreaGruntInCloset;
call RemoveEntityCollideCallback Player AreaCheckPlayer;
call RemoveEntityCollideCallback Player AreaCheckPlayer2;
call RemoveEntityCollideCallback Player AreaGrunt_2;
call RemoveEntityCollideCallback grunt_normal_1 AreaGruntDeactivate;
call GivePlayerDamage 500 BloodSplat true true;
watch 20 2;
health;
call RemoveEntityCollideCallback grunt_normal_1 AreaGruntInCloset;
call RemoveEntityCollideCallback Player AreaCheckPlayer;
call RemoveEntityCollideCallback Player AreaCheckPlayer2;
call RemoveEntityCollideCallback Player AreaGrunt_2;
call RemoveEntityCollideCallback grunt_normal_1 AreaGruntDeactivate;
call GivePlayerDamage 500 BloodSplat true true;
watch 20 2;
health;
call LookAtCorpse CPParticle 1;
watch 10 2;
body corpse_pile2_1;
shot events;
```

Все уникальные предупреждения итоговых симуляций (время удалено, повторы объединены):

```text
WARNING: [Script] AutoSave: skipped, needs saving (step 6)
WARNING: [Script] ClearEnemyPatrolNodes: skipped, needs enemies (step 6)
WARNING: [Script] AddEnemyPatrolNode: skipped, needs enemies (step 6)
```

### Геометрия, пути моделей и движение

Дополнительная независимая проверка существующим инструментом, без изменений импортёра:

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.HplMeshAudit.Run -amnesiaMap maps/main/ch01/10_daniels_room.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Audit_10_daniels_room.log
```

```text
Models=115, submeshes=202, geometryFailures=0, prefabMatrixDifferences=15, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2
Scene: instances=389, submeshMatrices=525, sharedMeshColliders=97, failures=2, maxMatrixError=4,2914E-06
Resource regression: checks=471, failures=0
```

**Общий аудит завершён кодом 1, не PASS.** Геометрия проверенных моделей совпала с MSH (geometryFailures=0, maxVertexError=0), кэши найдены, 471 проверка выбора ресурсов прошла. Замечания Scene и различия матриц исходных префабов не скрыты. Полный отчёт: `Logs/Audit_10_daniels_room.txt`.

Два замечания относятся к servant_grunt/servant_grunt_hair. У монстра в сцене есть SkinnedMeshRenderer (включая servant_grunt 1); проверка аудитора ищет только MeshFilter и не учитывает эту структуру. Матрицы проверенных узлов отличаются менее чем на 0.001. 15 отличий матриц исходных префабов отдельно отмечены аудитором; их нельзя автоматически считать ошибкой окончательных трансформов .ent.

Отдельный GPU-прогон движения: `watch 1 1; w 1; s 1; health; shot entry_walk`. Проверен короткий проход у начальной точки, не весь маршрут. Результат:

```text
t=  2,00 w 1              feet (-11.750, 0.005, 6.668) y 0,005..0,005 max 2,50 m/s now 1,50 ground True size 0 cam up 1,748 side -0,006 roll 0,0 | Normal crosshair Default text ""
t=  3,00 s 1              feet (-11.750, 0.005, 5.501) y 0,005..0,005 max 1,20 m/s now 1,20 ground True size 0 cam up 1,741 side 0,011 roll 0,0 | Normal crosshair Default text ""
t=  3,00   health 100,00 sanity 100,00 dead False pos (-11.75, 0.90, 5.50) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 0,60 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Просмотрен контрольный `Logs/Player_10_daniels_room_00_start.png`: Освещённая комната со свечами, мебелью и пылью. Снимки сценариев и прохода также сохранены в Logs. Специальный обход всех помещений и исправление замечаний геометрического аудитора в задачу переноса скриптов не включались.

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
