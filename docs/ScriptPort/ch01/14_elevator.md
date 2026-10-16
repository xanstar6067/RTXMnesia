# 14_elevator

- **Оригинал:** `source/maps/main/ch01/14_elevator.hps` (Latin-1, 281 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Elevator.cs`, класс `RTXMnesia.Levels.Ch01.Elevator`, атрибут `[HplMapScript("maps/main/ch01/14_elevator.map")]`.
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/14_elevator.unity` (генерируется, вне git).
- **Статус:** Перенесены все 8 функций; проверены обычные первые 30 секунд, пять безопасных обработчиков и вся последовательность до финала preview-ветки. Переход в главу 2 не выполнялся. Перенос выполнен в составе пакета 08–14; обе обязательные регрессии прошли. Независимый аудит сцены имеет замечания, разобранные ниже; полное ручное прохождение не заявляется. Дата: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в исходном скрипте нет. Все функции перечислены в исходном порядке; local/global var API хранит значения карты/игры, а не поля класса.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `TimerShake` (`14_elevator.hps:3`) | `Elevator.TimerShake` | готов | 1:1, исходный порядок и параметры |
| `TimerElevatorPreHowlEvents` (`14_elevator.hps:12`) | `Elevator.TimerElevatorPreHowlEvents` | готов | все пять стадий света/звука |
| `TimerElevatorPostHowlEvents` (`14_elevator.hps:74`) | `Elevator.TimerElevatorPostHowlEvents` | готов | все семь стадий, включая исходные preview/ChangeMap |
| `TimerStuff` (`14_elevator.hps:228`) | `Elevator.TimerStuff` | готов | двойное восстановление FOV сохранено |
| `TimerPlayerReact` (`14_elevator.hps:237`) | `Elevator.TimerPlayerReact` | готов | 1:1, исходный порядок и параметры |
| `OnStart` (`14_elevator.hps:244`) | `Elevator.OnStart` | готов | движение стен, три таймера и исходные переменные |
| `OnEnter` (`14_elevator.hps:261`) | `Elevator.OnEnter` | готов | 1:1, исходный порядок и параметры |
| `OnLeave` (`14_elevator.hps:275`) | `Elevator.OnLeave` | готов | 1:1, исходный порядок и параметры |

## Отличия от оригинала

Сигнатуры string &in / string & переведены в string; жизненный цикл — public override. Десятичные литералы имеют суффикс f. Статические имена функций в строковых аргументах используют nameof, динамические строки сохранены. Комментарии и порядок функций не менялись.

Дополнительная лексическая сверка после нормализации только разрешённых синтаксических изменений: **1355 одинаковых токенов** действующего кода, сохранены **все 40 исходных комментариев**. Штатный Check-ScriptPort также проверил функции, строковые константы, количество вызовов API, числа и коллбэки карты. Движок, API, импортёр, инструменты и прежние порты/журналы не изменялись.

## Особенности оригинала (сохранены)

- `14_elevator.hps:92–:206`: этапы после воя заданы отдельными if, а не else-if. Структура сохранена, lState читается один раз до проверок.
- `TimerStuff` дважды задаёт FOV=1 (:230, :234). Оба вызова сохранены.
- На стадии 4 первой последовательности fTime остаётся −1 (:58–:68), поэтому ElevatorEventState1 остаётся 4 и новый таймер не добавляется. После второй последовательности счётчик остаётся 6; Shake продолжает работать.
- `OnEnter` (:270) дважды предзагружает 14_elevator_clank. Дубликат сохранён.
- Ветка PreviewVersion=1 (:209–:215) переносит на PlayerStartArea_2, проявляет экран, отключает управление и показывает PreviewOver. Обычный ChangeMap в ch02/15_prison_south.map (:218) сохранён без подмены, но не исполнялся в проверках.

## Не перенесено движком

Скрипт не вызывает NotPorted API: в итоговых прогонах нет сообщений `skipped, needs`.

Одна из частей elevator_wall01 не найдена импортёром (ниже). Переход в ещё не перенесённую главу 2 и непрерывное прохождение между всеми новыми сценами этим прогоном не подтверждались.

## Проверка

Unity 6000.6.2f1 запускался последовательно, при отсутствии Temp/UnityLockfile, с доступом к лицензии Windows. Симуляции и обе регрессии — с GPU. Все итоговые симуляции завершились кодом 0, без исключений, PortError и несовпадения числа аргументов. Предупреждения и исходные ошибки перечислены ниже. Новых предупреждений компилятора из семи портов нет; прежние предупреждения других файлов не исправлялись.

Все пути `Logs/...` ниже относятся к `Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/` и не входят в git.

### Импорт

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/14_elevator.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Import_14_elevator.log
```

```text
=== Level: maps/main/ch01/14_elevator.map — 2026-10-16 18:50:11 — 3 warnings, 0 errors ===
Resources: 5 models (1 imported now), 2 extra materials (0 converted now), 0 cookies
WARNING: entities/special/elevator_wall01/elevator_wall01.ent: sub mesh 'polySurface285' not found in the model
WARNING: Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
WARNING: No level script port for maps/main/ch01/14_elevator.map yet (the level runs without its events)
```

Импорт завершён кодом 0. Полный сохранённый отчёт: `Logs/14_elevator_import_report.log`. Предупреждение No level script port относится к импорту до добавления C#; последующие прогоны находят Elevator. Сцену автоматически добавил импортёр в EditorBuildSettings.asset; это ожидаемое изменение сохранено.

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 14_elevator -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Elevator.cs
```

```text
14_elevator : функций 8, строк 64, ошибок 0, предупреждений 0
```

### Симуляции и события

Общий запуск (полный путь Unity.exe: `C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe`):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/14_elevator.map -seed 1 -script "$script" -logFile <полный путь отдельного лога>
```

| Итоговый лог | Предупреждения | Ошибки отчёта | Код выхода |
|---|---:|---:|---:|
| `Sim_14_elevator.log` | 0 | 0 | 0 |
| `Sim_14_elevator_events.log` | 0 | 0 | 0 |
| `Sim_14_elevator_preview.log` | 0 | 0 | 0 |
| `Sim_14_elevator_walk.log` | 0 | 0 | 0 |

Стартовый сценарий: `watch 30 2; shot start`. Для основных прямых вызовов далее выдержано `watch 10 2`; подготовка, запросы состояния и проверки смерти явно указаны. `ChangeMap` не вызывался. Исходные ошибки HplLog отображаются как WARNING в отчёте симулятора, поэтому счётчик «0 errors» сам по себе их не исключает.

Обычный старт автоматически проходит все стадии до начала падения (30 с): ElevatorEventState2=4, музыка 14_elevator_falls.ogg, след 0.5, скорость движения 0.1, игрок на полу. Перед отдельным прогоном пяти безопасных обработчиков диагностически удалён Events2, чтобы не исполнить ChangeMap. Вызваны TimerShake, TimerElevatorPreHowlEvents, TimerStuff, TimerPlayerReact и OnLeave с watch 10 2 после каждого.

Дополнительный исходный режим -globals PreviewVersion=1, watch 45 2 проходит также удар, затемнение и стадию 6. Подтверждены ElevatorEventState1=4, ElevatorEventState2=6, перенос в (11.75,0.00,3.00), active False, сообщение PreviewOver, FOV/скорость=1, fade/trail/blur=0. Сам TimerElevatorPostHowlEvents вручную не вызывался.

Воспроизводимые итоговые сценарии:

**events**:

```text
call RemoveTimer Events2;
call TimerShake Shake;
watch 10 2;
call TimerElevatorPreHowlEvents Events1;
watch 10 2;
call TimerStuff 1;
watch 10 2;
call TimerPlayerReact scare;
watch 10 2;
call OnLeave;
watch 10 2;
shot events;
```

**preview** (`-globals "PreviewVersion=1"`):

```text
watch 45 2;
call GetLocalVarInt ElevatorEventState1;
call GetLocalVarInt ElevatorEventState2;
shot preview;
```

Все уникальные предупреждения итоговых симуляций (время удалено, повторы объединены):

```text
Нет предупреждений.
```

### Геометрия, пути моделей и движение

Дополнительная независимая проверка существующим инструментом, без изменений импортёра:

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.HplMeshAudit.Run -amnesiaMap maps/main/ch01/14_elevator.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Audit_14_elevator.log
```

```text
Models=5, submeshes=89, geometryFailures=0, prefabMatrixDifferences=0, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=0
Scene: instances=5, submeshMatrices=78, sharedMeshColliders=1, failures=1, maxMatrixError=2,38419E-07
Resource regression: checks=471, failures=0
```

**Общий аудит завершён кодом 1, не PASS.** Геометрия проверенных моделей совпала с MSH (geometryFailures=0, maxVertexError=0), кэши найдены, 471 проверка выбора ресурсов прошла. Замечания Scene и различия матриц исходных префабов не скрыты. Полный отчёт: `Logs/Audit_14_elevator.txt`.

Одно замечание Scene — elevator_wall01_1/polySurface285, также отмеченное импортёром. Его ожидаемое имя не найдено в модели; полный аудит сцены не прошёл. Остальные проверенные матрицы и геометрия моделей совпали. Исправлений импортёра или ресурсов не выполнялось.

Отдельный GPU-прогон движения: `watch 1 1; w 1; s 1; health; shot entry_walk`. Проверен короткий проход у начальной точки, не весь маршрут. Результат:

```text
t=  2,00 w 1              feet (-1.245, 0.025, -0.961) y 0,025..0,025 max 2,51 m/s now 1,25 ground True size 0 cam up 1,738 side 0,069 roll 0,0 | Normal crosshair Default text ""
t=  3,00 s 1              feet (0.378, 0.025, -0.036) y 0,025..0,025 max 2,00 m/s now 2,00 ground True size 0 cam up 1,706 side 0,024 roll 0,0 | Normal crosshair Default text ""
t=  3,00   health 100,00 sanity 100,00 dead False pos (0.38, 0.92, -0.04) roll 0,0 pitch 0,0 yaw -120,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Просмотрен контрольный `Logs/Player_14_elevator_00_start.png`: Внутренняя стенка кабины во время падения и русская подсказка рассудка. Снимки сценариев и прохода также сохранены в Logs. Специальный обход всех помещений и исправление замечаний геометрического аудитора в задачу переноса скриптов не включались.

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
