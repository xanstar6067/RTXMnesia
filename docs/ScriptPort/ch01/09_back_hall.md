# 09_back_hall

- **Оригинал:** `source/maps/main/ch01/09_back_hall.hps` (Latin-1, 509 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/BackHall.cs`, класс `RTXMnesia.Levels.Ch01.BackHall`, атрибут `[HplMapScript("maps/main/ch01/09_back_hall.map")]`.
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/09_back_hall.unity` (генерируется, вне git).
- **Статус:** Перенесены все 27 функций; проверены старт, двери, цепочка следа, видения и обе ветки изменения хаба. Смена карты из лифта не вызывалась. Перенос выполнен в составе пакета 08–14; обе обязательные регрессии прошли. Независимый аудит сцены имеет замечания, разобранные ниже; полное ручное прохождение не заявляется. Дата: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в исходном скрипте нет. Все функции перечислены в исходном порядке; local/global var API хранит значения карты/игры, а не поля класса.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `TimerDistantHowl` (`09_back_hall.hps:3`) | `BackHall.TimerDistantHowl` | готов | 1:1, исходный порядок и параметры |
| `TimerGuardianNearEffect` (`09_back_hall.hps:16`) | `BackHall.TimerGuardianNearEffect` | готов | 1:1, исходный порядок и параметры |
| `TimerGuardianNearHowl` (`09_back_hall.hps:40`) | `BackHall.TimerGuardianNearHowl` | готов | 1:1, исходный порядок и параметры |
| `TimerGuardianNearDone` (`09_back_hall.hps:55`) | `BackHall.TimerGuardianNearDone` | готов | 1:1, исходный порядок и параметры |
| `InteractElevatorLever` (`09_back_hall.hps:78`) | `BackHall.InteractElevatorLever` | готов | 1:1, исходный порядок и параметры |
| `PullElevatorLever` (`09_back_hall.hps:87`) | `BackHall.PullElevatorLever` | готов | 1:1, исходный порядок и параметры |
| `TimerStartElevator` (`09_back_hall.hps:99`) | `BackHall.TimerStartElevator` | готов | ChangeMap сохранён, диагностически не вызывается |
| `CollideFlashBack` (`09_back_hall.hps:108`) | `BackHall.CollideFlashBack` | готов | 1:1, исходный порядок и параметры |
| `TimerCreateTrail` (`09_back_hall.hps:114`) | `BackHall.TimerCreateTrail` | готов | все шесть состояний и пять таймеров толчка двери |
| `MakeTrailEffect` (`09_back_hall.hps:163`) | `BackHall.MakeTrailEffect` | готов | 1:1, исходный порядок и параметры |
| `TimerKillTrail` (`09_back_hall.hps:169`) | `BackHall.TimerKillTrail` | готов | 1:1, исходный порядок и параметры |
| `TimerPushElevatorDoors` (`09_back_hall.hps:183`) | `BackHall.TimerPushElevatorDoors` | готов | 1:1, исходный порядок и параметры |
| `InteractMachineRoomDoor` (`09_back_hall.hps:190`) | `BackHall.InteractMachineRoomDoor` | готов | 1:1, исходный порядок и параметры |
| `TimerDelayUnLockHint` (`09_back_hall.hps:197`) | `BackHall.TimerDelayUnLockHint` | готов | 1:1, исходный порядок и параметры |
| `UnlockMachineRoomDoor` (`09_back_hall.hps:202`) | `BackHall.UnlockMachineRoomDoor` | готов | 1:1, исходный порядок и параметры |
| `TimerPlayerReact` (`09_back_hall.hps:217`) | `BackHall.TimerPlayerReact` | готов | 1:1, исходный порядок и параметры |
| `InteractFountainWater` (`09_back_hall.hps:235`) | `BackHall.InteractFountainWater` | готов | 1:1, исходный порядок и параметры |
| `TimerFountainWater` (`09_back_hall.hps:243`) | `BackHall.TimerFountainWater` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaShowDoor` (`09_back_hall.hps:249`) | `BackHall.CollideAreaShowDoor` | готов | 1:1, исходный порядок и параметры |
| `TimerSecondBreakfast` (`09_back_hall.hps:263`) | `BackHall.TimerSecondBreakfast` | готов | 1:1, исходный порядок и параметры |
| `TimerStopShow` (`09_back_hall.hps:267`) | `BackHall.TimerStopShow` | готов | 1:1, исходный порядок и параметры |
| `CollideWithHerbertVision_1` (`09_back_hall.hps:277`) | `BackHall.CollideWithHerbertVision_1` | готов | 1:1, исходный порядок и параметры |
| `CollideWithHerbertVision_2` (`09_back_hall.hps:285`) | `BackHall.CollideWithHerbertVision_2` | готов | 1:1, исходный порядок и параметры |
| `SetUpHerbertVisionAreas` (`09_back_hall.hps:292`) | `BackHall.SetUpHerbertVisionAreas` | готов | четыре области и исходные защитные условия |
| `OnStart` (`09_back_hall.hps:346`) | `BackHall.OnStart` | готов | 1:1, исходный порядок и параметры |
| `OnEnter` (`09_back_hall.hps:406`) | `BackHall.OnEnter` | готов | независимые события стража/фонтана, след, видения и приоритет музыки |
| `OnLeave` (`09_back_hall.hps:506`) | `BackHall.OnLeave` | готов | 1:1, исходный порядок и параметры |

## Отличия от оригинала

Сигнатуры string &in / string & переведены в string; жизненный цикл — public override. Десятичные литералы имеют суффикс f. Статические имена функций в строковых аргументах используют nameof, динамические строки сохранены. Комментарии и порядок функций не менялись.

Дополнительная лексическая сверка после нормализации только разрешённых синтаксических изменений: **2362 одинаковых токенов** действующего кода, сохранены **все 80 исходных комментариев**. Штатный Check-ScriptPort также проверил функции, строковые константы, количество вызовов API, числа и коллбэки карты. Движок, API, импортёр, инструменты и прежние порты/журналы не изменялись.

## Особенности оригинала (сохранены)

- `09_back_hall.hps:47` использует имя `SlineFog_` с исходным написанием; эти области действительно есть в карте, переименование не требуется.
- Шесть состояний `TimerCreateTrail` (:120–:159), закомментированные вызовы MakeTrailEffect, пять толчков двери и отложенный TimerKillTrail оставлены отдельно и в том же порядке.
- `SetUpHerbertVisionAreas` сначала отключает все четыре области (:298–:302); условия завершения уровней строго равны 1 или 2, а защита от совпадения с событием стража стоит внутри двух отдельных веток (:317, :335).
- `OnEnter` (:411–:495) независимо проверяет близость стража и кровавый фонтан; это два отдельных if. Ветки музыкального выбора имеют исходный приоритет, включая пустую ветку уже близкого стража.
- `PullElevatorLever` (:89) принимает только состояние −1; TimerStartElevator (:104) содержит исходный ChangeMap. Этот путь сохранён, но в диагностических вызовах не запускался.
- В .map повторяются имена двух пар сущностей с разными ID. Имена сохранены; последствия для независимого аудитора разобраны в разделе геометрии.

## Не перенесено движком

| API NotPorted | skipped, needs | Последствие |
|---|---|---|
| `AutoSave` | `saving (step 6)` | вход не сохраняется на диск |

Области видений здесь имеют тип Flashback, который уже поддерживается. Их обработчики и глобальные переменные перенесены; полный маршрут через все видения отдельно не проходился.

## Проверка

Unity 6000.6.2f1 запускался последовательно, при отсутствии Temp/UnityLockfile, с доступом к лицензии Windows. Симуляции и обе регрессии — с GPU. Все итоговые симуляции завершились кодом 0, без исключений, PortError и несовпадения числа аргументов. Предупреждения и исходные ошибки перечислены ниже. Новых предупреждений компилятора из семи портов нет; прежние предупреждения других файлов не исправлялись.

Все пути `Logs/...` ниже относятся к `Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/` и не входят в git.

### Импорт

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/09_back_hall.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Import_09_back_hall.log
```

```text
=== Level: maps/main/ch01/09_back_hall.map — 2026-10-16 18:48:06 — 2 warnings, 0 errors ===
Resources: 90 models (17 imported now), 7 extra materials (0 converted now), 0 cookies
WARNING: Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
WARNING: No level script port for maps/main/ch01/09_back_hall.map yet (the level runs without its events)
```

Импорт завершён кодом 0. Полный сохранённый отчёт: `Logs/09_back_hall_import_report.log`. Предупреждение No level script port относится к импорту до добавления C#; последующие прогоны находят BackHall. Сцену автоматически добавил импортёр в EditorBuildSettings.asset; это ожидаемое изменение сохранено.

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 09_back_hall -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/BackHall.cs
```

```text
09_back_hall : функций 27, строк 176, ошибок 0, предупреждений 0
```

### Симуляции и события

Общий запуск (полный путь Unity.exe: `C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe`):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/09_back_hall.map -seed 1 -script "$script" -logFile <полный путь отдельного лога>
```

| Итоговый лог | Предупреждения | Ошибки отчёта | Код выхода |
|---|---:|---:|---:|
| `Sim_09_back_hall.log` | 1 | 0 | 0 |
| `Sim_09_back_hall_events.log` | 1 | 0 | 0 |
| `Sim_09_back_hall_walk.log` | 1 | 0 | 0 |

Стартовый сценарий: `watch 30 2; shot start`. Для основных прямых вызовов далее выдержано `watch 10 2`; подготовка, запросы состояния и проверки смерти явно указаны. `ChangeMap` не вызывался. Исходные ошибки HplLog отображаются как WARNING в отчёте симулятора, поэтому счётчик «0 errors» сам по себе их не исключает.

Квест 09StartElevator добавлен, дверь castle_1 толкается, след проходит состояния и меняет освещение/частицы. Проверены обе ветки запертой двери машинного отделения: без ключа и с выданным диагностически ключом. UnlockMachineRoomDoor отпирает её и удаляет ключ. Взаимодействие с фонтаном восстанавливается таймером. Оба обработчика видений меняют LastHerbertVisionTriggered.

Повторные OnEnter с GuidingRod=3 и PickedMachineInstructions=1 дают EventBloodFountain=1, заменяют воду кровью, частицы и слизь; затем ElevatorMachineRunning=1 даёт EventGuardianNear=1, эффект близкого стража, запирание дверей и освобождение рычага. Функции, запускающие ChangeMap, не вызывались. Ошибок скрипта в итоговых прогонах нет.

Воспроизводимые итоговые сценарии:

**events**:

```text
call InteractElevatorLever elevator_lever_1;
watch 10 2;
call QuestIsAdded 09StartElevator;
call CollideAreaShowDoor Player AreaShowDoor 1;
watch 10 2;
body castle_1;
call CollideFlashBack Player AreaFlashTrigger 1;
watch 10 2;
call GetLocalVarInt Trail;
call InteractMachineRoomDoor level_wood_4;
watch 10 2;
call GiveItemFromFile key_jar_1_item key_study.ent;
call InteractMachineRoomDoor level_wood_4;
watch 10 2;
call UnlockMachineRoomDoor key_jar_1_item level_wood_4;
watch 10 2;
body level_wood_4;
call InteractFountainWater ScriptArea_1;
watch 10 2;
call SetGlobalVarInt CompletedLevel_10_11 1;
call CollideWithHerbertVision_1;
watch 10 2;
call SetGlobalVarInt CompletedLevel_12 2;
call CollideWithHerbertVision_2;
watch 10 2;
call SetGlobalVarInt GuidingRod 3;
call SetGlobalVarInt PickedMachineInstructions 1;
call OnEnter;
watch 10 2;
call GetLocalVarInt EventBloodFountain;
body fountain_childsnake_blood_1;
call SetGlobalVarInt ElevatorMachineRunning 1;
call OnEnter;
watch 10 2;
call GetLocalVarInt EventGuardianNear;
shot events;
```

Все уникальные предупреждения итоговых симуляций (время удалено, повторы объединены):

```text
WARNING: [Script] AutoSave: skipped, needs saving (step 6)
```

### Геометрия, пути моделей и движение

Дополнительная независимая проверка существующим инструментом, без изменений импортёра:

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.HplMeshAudit.Run -amnesiaMap maps/main/ch01/09_back_hall.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Audit_09_back_hall.log
```

```text
Models=92, submeshes=222, geometryFailures=0, prefabMatrixDifferences=2, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2
Scene: instances=620, submeshMatrices=1025, sharedMeshColliders=532, failures=16, maxMatrixError=16,7206
Resource regression: checks=471, failures=0
```

**Общий аудит завершён кодом 1, не PASS.** Геометрия проверенных моделей совпала с MSH (geometryFailures=0, maxVertexError=0), кэши найдены, 471 проверка выбора ресурсов прошла. Замечания Scene и различия матриц исходных префабов не скрыты. Полный отчёт: `Logs/Audit_09_back_hall.txt`.

Из 16 замечаний девять относятся к подмешам анимированной слизи, которые аудитор ищет только среди MeshFilter. Ещё семь связаны с повторяющимися именами в исходной карте: Even01Slime04 (ID 1030/1040) и Even01Slime02_3 (ID 1047/1055). Они находятся на строках .map:1766, :1937, :2032, :2108. Аудитор вызывает transform.Find по имени и при проверке второго экземпляра получает первый, откуда PLACEMENT и WRONG ENTITY RESOURCE. Независимо проверены сериализованные AmnesiaEntity по hplId: все четыре файла соответствуют своему FileIndex исходника, включая slime_anim_wall для 1047 и slime_pile_large для 1055. Подмены ресурса по этому сообщению не установлено; исходные имена и аудитор не менялись.

Отдельный GPU-прогон движения: `watch 1 1; w 1; s 1; health; shot entry_walk`. Проверен короткий проход у начальной точки, не весь маршрут. Результат:

```text
t=  2,00 w 1              feet (-5.750, -11.995, -9.870) y -11,995..-11,995 max 2,50 m/s now 2,50 ground True size 0 cam up 1,708 side -0,025 roll 0,0 | Normal crosshair Default text ""
t=  3,00 s 1              feet (-5.750, -11.995, -11.722) y -11,995..-11,995 max 2,00 m/s now 2,00 ground True size 0 cam up 1,726 side 0,019 roll 0,0 | Normal crosshair Default text ""
t=  3,00   health 100,00 sanity 100,00 dead False pos (-5.75, -11.09, -11.72) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Просмотрен контрольный `Logs/Player_09_back_hall_00_start.png`: Лестница хаба с факелами и холодным окружающим светом. Снимки сценариев и прохода также сохранены в Logs. Специальный обход всех помещений и исправление замечаний геометрического аудитора в задачу переноса скриптов не включались.

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
