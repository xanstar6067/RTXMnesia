# 13_machine_room

- **Оригинал:** `source/maps/main/ch01/13_machine_room.hps` (Latin-1, 655 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/MachineRoom.cs`, класс `RTXMnesia.Levels.Ch01.MachineRoom`, атрибут `[HplMapScript("maps/main/ch01/13_machine_room.map")]`.
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/13_machine_room.unity` (генерируется, вне git).
- **Статус:** Перенесены все 28 функций; проверены ошибочный запуск, уголь, три колеса, три стержня, рычаги 8/8, запуск машины и оба события слизи. Перенос выполнен в составе пакета 08–14; обе обязательные регрессии прошли. Независимый аудит сцены имеет замечания, разобранные ниже; полное ручное прохождение не заявляется. Дата: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в исходном скрипте нет. Все функции перечислены в исходном порядке; local/global var API хранит значения карты/игры, а не поля класса.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `AttachWheel` (`13_machine_room.hps:5`) | `MachineRoom.AttachWheel` | готов | общий WheelSound и отложенная замена модели |
| `TimerWheel` (`13_machine_room.hps:21`) | `MachineRoom.TimerWheel` | готов | 1:1, исходный порядок и параметры |
| `CollideWrongWheel` (`13_machine_room.hps:30`) | `MachineRoom.CollideWrongWheel` | готов | 1:1, исходный порядок и параметры |
| `CollideCoalInBurner` (`13_machine_room.hps:41`) | `MachineRoom.CollideCoalInBurner` | готов | 1:1, исходный порядок и параметры |
| `TimerBurnerFull` (`13_machine_room.hps:64`) | `MachineRoom.TimerBurnerFull` | готов | 1:1, исходный порядок и параметры |
| `PullCoalLever` (`13_machine_room.hps:70`) | `MachineRoom.PullCoalLever` | готов | 1:1, исходный порядок и параметры |
| `TimerBurnerFailed` (`13_machine_room.hps:105`) | `MachineRoom.TimerBurnerFailed` | готов | 1:1, исходный порядок и параметры |
| `TimerBurnerStarted` (`13_machine_room.hps:117`) | `MachineRoom.TimerBurnerStarted` | готов | 1:1, исходный порядок и параметры |
| `UseRod` (`13_machine_room.hps:127`) | `MachineRoom.UseRod` | готов | все слоты, правильные и ошибочные размещения |
| `PickRod` (`13_machine_room.hps:196`) | `MachineRoom.PickRod` | готов | 1:1, исходный порядок и параметры |
| `SixLevers` (`13_machine_room.hps:225`) | `MachineRoom.SixLevers` | готов | исходные суммы и фиксация состояний |
| `CheckValue` (`13_machine_room.hps:280`) | `MachineRoom.CheckValue` | готов | 1:1, исходный порядок и параметры |
| `PullStartLever` (`13_machine_room.hps:295`) | `MachineRoom.PullStartLever` | готов | 1:1, исходный порядок и параметры |
| `StartMachine` (`13_machine_room.hps:302`) | `MachineRoom.StartMachine` | готов | неудачный и успешный запуск |
| `TimerMachineError` (`13_machine_room.hps:345`) | `MachineRoom.TimerMachineError` | готов | 1:1, исходный порядок и параметры |
| `RunWheels` (`13_machine_room.hps:351`) | `MachineRoom.RunWheels` | готов | 1:1, исходный порядок и параметры |
| `CollideExitHowls` (`13_machine_room.hps:370`) | `MachineRoom.CollideExitHowls` | готов | два исходных выхода за границу массива с тихим завершением |
| `TimerSlimeAppear` (`13_machine_room.hps:434`) | `MachineRoom.TimerSlimeAppear` | готов | 1:1, исходный порядок и параметры |
| `TimerSlimeSound` (`13_machine_room.hps:443`) | `MachineRoom.TimerSlimeSound` | готов | уменьшение fFade и invariant-строка |
| `TimerEHowl` (`13_machine_room.hps:453`) | `MachineRoom.TimerEHowl` | готов | 1:1, исходный порядок и параметры |
| `TimerDrop` (`13_machine_room.hps:514`) | `MachineRoom.TimerDrop` | готов | 1:1, исходный порядок и параметры |
| `InteractDynCogWheel` (`13_machine_room.hps:531`) | `MachineRoom.InteractDynCogWheel` | готов | 1:1, исходный порядок и параметры |
| `InteractAreaSpokes` (`13_machine_room.hps:545`) | `MachineRoom.InteractAreaSpokes` | готов | 1:1, исходный порядок и параметры |
| `EntityCallPickDiary` (`13_machine_room.hps:554`) | `MachineRoom.EntityCallPickDiary` | готов | 1:1, исходный порядок и параметры |
| `TimerGuardianAmb` (`13_machine_room.hps:567`) | `MachineRoom.TimerGuardianAmb` | готов | 1:1, исходный порядок и параметры |
| `OnStart` (`13_machine_room.hps:578`) | `MachineRoom.OnStart` | готов | 1:1, исходный порядок и параметры |
| `OnEnter` (`13_machine_room.hps:636`) | `MachineRoom.OnEnter` | готов | 1:1, исходный порядок и параметры |
| `OnLeave` (`13_machine_room.hps:652`) | `MachineRoom.OnLeave` | готов | 1:1, исходный порядок и параметры |

## Отличия от оригинала

Сигнатуры string &in / string & переведены в string; жизненный цикл — public override. Десятичные литералы имеют суффикс f. Статические имена функций в строковых аргументах используют nameof, динамические строки сохранены. Комментарии и порядок функций не менялись. Логическое or переведено в ||, length() — в Length. Присваивание int строке sTemp переведено через ToString(InvariantCulture); отладочная конкатенация fFade — через Str с G6. Два исходных исключения AngelScript воспроизведены ранним выходом в точке ошибки, как описано ниже.

Дополнительная лексическая сверка после нормализации только разрешённых синтаксических изменений: **3832 одинаковых токенов** действующего кода, сохранены **все 66 исходных комментариев**. Штатный Check-ScriptPort также проверил функции, строковые константы, количество вызовов API, числа и коллбэки карты. Движок, API, импортёр, инструменты и прежние порты/журналы не изменялись.

## Особенности оригинала (сохранены)

- `13_machine_room.hps:409`, :429: оба цикла используют <= длины массива. После всех допустимых AddTimer исходный AngelScript молча обрывает вызов на следующем индексе. В порте сохранены <= и ранний return при достижении длины с английским пояснением; исключение C# не выбрасывается, дополнительный RandFloat не выполняется.
- Во втором массиве (:424) есть slime_pile_large_4, которой нет в исходной .map. Имя сохранено; попытка появления и затем звука дают два исходных предупреждения отсутствующей сущности.
- `SixLevers` в средней позиции (:253–:258) очищает две переменные рычага, но не пересчитывает up_value/down_value. В крайних положениях не обнуляет противоположное значение. Логика не улучшалась.
- WheelSound (:13) общий для отложенных TimerWheel; возможное наложение одновременных установок не устранялось. В тесте колёса устанавливались последовательно.
- `TimerBurnerFailed` (:107) останавливает BurnerSoundFail при обоих таймерах (:83–:84). При удалённом вызове звук вне радиуса уже может быть уничтожен к повторному StopSound. Сам звук создаётся динамически, в карте готового Sound-узла нет; исходные вызовы сохранены.
- `TimerEHowl` (:504) использует <= iMaxEventStep и ставит ещё один вызов после восьмого состояния. Повторное событие не сбрасывает его счётчик. Ошибки написания guradianamb и MISC SECTUP оставлены.

## Не перенесено движком

| API NotPorted | skipped, needs | Последствие |
|---|---|---|
| `AutoSave` | — | с 2026-10-17 пишет автосохранение: сохранения перенесены (CLAUDE.md «Сохранения») |

Sticky-области трёх колёс ещё не выполняют физическое прикрепление и автоматический AttachWheel; обработчики вызваны явно для проверки последующей цепочки. Вращение пропов, состояния рычагов и создание стержней уже реализованы и проверены. Insanity-области пока не исполняют события.

## Проверка

Unity 6000.6.2f1 запускался последовательно, при отсутствии Temp/UnityLockfile, с доступом к лицензии Windows. Симуляции и обе регрессии — с GPU. Все итоговые симуляции завершились кодом 0, без исключений, PortError и несовпадения числа аргументов. Предупреждения и исходные ошибки перечислены ниже. Новых предупреждений компилятора из семи портов нет; прежние предупреждения других файлов не исправлялись.

Все пути `Logs/...` ниже относятся к `Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/` и не входят в git.

### Импорт

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/13_machine_room.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Import_13_machine_room.log
```

```text
=== Level: maps/main/ch01/13_machine_room.map — 2026-10-16 18:49:51 — 2 warnings, 0 errors ===
Resources: 99 models (38 imported now), 10 extra materials (0 converted now), 1 cookies
WARNING: Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
WARNING: No level script port for maps/main/ch01/13_machine_room.map yet (the level runs without its events)
```

Импорт завершён кодом 0. Полный сохранённый отчёт: `Logs/13_machine_room_import_report.log`. Предупреждение No level script port относится к импорту до добавления C#; последующие прогоны находят MachineRoom. Сцену автоматически добавил импортёр в EditorBuildSettings.asset; это ожидаемое изменение сохранено.

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 13_machine_room -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/MachineRoom.cs
```

```text
13_machine_room : функций 28, строк 254, ошибок 0, предупреждений 0
```

### Симуляции и события

Общий запуск (полный путь Unity.exe: `C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe`):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/13_machine_room.map -seed 1 -script "$script" -logFile <полный путь отдельного лога>
```

| Итоговый лог | Предупреждения | Ошибки отчёта | Код выхода |
|---|---:|---:|---:|
| `Sim_13_machine_room.log` | 1 | 0 | 0 |
| `Sim_13_machine_room_events.log` | 3 | 0 | 0 |
| `Sim_13_machine_room_walk.log` | 1 | 0 | 0 |

Стартовый сценарий: `watch 30 2; shot start`. Для основных прямых вызовов далее выдержано `watch 10 2`; подготовка, запросы состояния и проверки смерти явно указаны. `ChangeMap` не вызывался. Исходные ошибки HplLog отображаются как WARNING в отчёте симулятора, поэтому счётчик «0 errors» сам по себе их не исключает.

Проверены сообщения отсутствующих колёс и неподходящего колеса, ошибка запуска машины. Три AttachWheel дают WheelOK=3 и включают mounted-модели. Три коллизии угля дают CoalOK=3, рычаг включает огонь. Правильные UseRod дают GuideOK=1, стержни создаются и становятся неинтерактивными.

Рычаги 1/2/6 установлены в +1, 3/4/5 в −1 через SetLeverStuckState с настоящими connection-callbacks. Значения up_value=8, down_value=8, SticksOK=1. Рычаг start_lever включён тем же способом: ElevatorMachineRunning=1, квест завершается, включаются пар, звук и вращение. Затем вызваны обе CollideExitHowls. Массивы обрабатываются до исходной точки выхода, исключений нет; отсутствующая slime_pile_large_4 сохранена.

В первом прямом вызове StartMachine успешная установка состояния рычага вызвала его connection-callback ещё раз, поэтому были два предупреждения о повторной регистрации AreaHowl. Итоговый сценарий запускает рычаг через состояние. Удалённый пробный вызов топки дал BurnerSoundFail; ещё одна пробная координата goto оказалась ниже пола и вызвала смерть с пустым _auto. В итоговом прогоне использована исходная PlayerStartArea_2 у топки; эти диагностические ошибки не исправлялись в порте.

Воспроизводимые итоговые сценарии:

**events**:

```text
call InteractAreaSpokes AreaSpokes;
watch 10 2;
call StartMachine start_lever;
watch 10 2;
call CollideWrongWheel cogwheel01_dyn cogwheel02 1;
watch 10 2;
call AttachWheel cogwheel01 cogwheel01_dyn_Body_1;
watch 10 2;
call AttachWheel cogwheel02 cogwheel02_dyn_Body_1;
watch 10 2;
call AttachWheel cogwheel03 cogwheel03_dyn_Body_1;
watch 10 2;
call GetLocalVarInt WheelOK;
body cogwheel03_mounted;
start PlayerStartArea_2;
watch 1 1;
health;
call PullCoalLever coal_lever 1;
watch 10 2;
call CollideCoalInBurner coal_1 AreaBurner 1;
watch 10 2;
call CollideCoalInBurner coal_2 AreaBurner 1;
watch 10 2;
call CollideCoalInBurner coal_3 AreaBurner 1;
watch 10 2;
call SetLeverStuckState coal_lever 1 true;
watch 10 2;
call GetLocalVarInt CoalOK;
call UseRod guiding_rod01 ra01;
watch 10 2;
call UseRod guiding_rod02 ra02;
watch 10 2;
call UseRod guiding_rod03 ra03;
watch 10 2;
call GetLocalVarInt GuideOK;
call SetLeverStuckState machine_lever_1 1 true;
call SetLeverStuckState machine_lever_2 1 true;
call SetLeverStuckState machine_lever_3 -1 true;
call SetLeverStuckState machine_lever_4 -1 true;
call SetLeverStuckState machine_lever_5 -1 true;
call SetLeverStuckState machine_lever_6 1 true;
call SixLevers machine_lever_6 1;
watch 10 2;
call GetLocalVarInt up_value;
call GetLocalVarInt down_value;
call GetLocalVarInt SticksOK;
call SetLeverStuckState start_lever 1 true;
watch 10 2;
call GetGlobalVarInt ElevatorMachineRunning;
call CollideExitHowls Player AreaHowl_1 1;
watch 10 2;
call CollideExitHowls Player AreaHowl_2 1;
watch 10 2;
shot events;
```

Все уникальные предупреждения итоговых симуляций (время удалено, повторы объединены):

```text
WARNING: [Script] AutoSave: skipped, needs saving (step 6)
WARNING: [Script] Entity 'slime_pile_large_4' with type Prop and subtype any does not exist!
WARNING: [Script] Entity 'slime_pile_large_4' does not exist!
```

### Геометрия, пути моделей и движение

Дополнительная независимая проверка существующим инструментом, без изменений импортёра:

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.HplMeshAudit.Run -amnesiaMap maps/main/ch01/13_machine_room.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Audit_13_machine_room.log
```

```text
Models=106, submeshes=155, geometryFailures=0, prefabMatrixDifferences=6, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=1
Scene: instances=856, submeshMatrices=975, sharedMeshColliders=624, failures=5, maxMatrixError=3,8147E-06
Resource regression: checks=471, failures=0
```

**Общий аудит завершён кодом 1, не PASS.** Геометрия проверенных моделей совпала с MSH (geometryFailures=0, maxVertexError=0), кэши найдены, 471 проверка выбора ресурсов прошла. Замечания Scene и различия матриц исходных префабов не скрыты. Полный отчёт: `Logs/Audit_13_machine_room.txt`.

Пять замечаний Scene — подмеши анимированной слизи. Подтверждено наличие SkinnedMeshRenderer на slime_wall, который не проверяется текущим аудитором MeshFilter. Численные матрицы проверенных узлов совпали в пределах 0.001. Шесть отличий матриц исходных префабов отмечены отдельно и не скрыты.

Отдельный GPU-прогон движения: `watch 1 1; w 1; s 1; health; shot entry_walk`. Проверен короткий проход у начальной точки, не весь маршрут. Результат:

```text
t=  2,00 w 1              feet (0.370, 0.005, 20.250) y 0,005..0,005 max 2,50 m/s now 2,50 ground True size 0 cam up 1,708 side -0,025 roll 0,0 | Normal crosshair Default text ""
t=  3,00 s 1              feet (2.222, 0.005, 20.250) y 0,005..0,005 max 2,00 m/s now 2,00 ground True size 0 cam up 1,726 side 0,019 roll 0,0 | Normal crosshair Default text ""
t=  3,00   health 100,00 sanity 99,91 dead False pos (2.22, 0.90, 20.25) roll 0,0 pitch 0,0 yaw -90,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Просмотрен контрольный `Logs/Player_13_machine_room_00_start.png`: Тёмный коридор с трубами; дополнительный событийный снимок — у топки. Снимки сценариев и прохода также сохранены в Logs. Специальный обход всех помещений и исправление замечаний геометрического аудитора в задачу переноса скриптов не включались.

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
