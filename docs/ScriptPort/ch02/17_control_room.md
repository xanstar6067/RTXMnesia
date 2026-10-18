# 17_control_room

Оригинал: `source/maps/main/ch02/17_control_room.hps` (632 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/ControlRoom.cs`.
Класс: `RTXMnesia.Levels.Ch02.ControlRoom`, атрибут: `HplMapScript("maps/main/ch02/17_control_room.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/17_control_room.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `17_control_room.hps:5` | `ControlRoom.CollideWithHerbertVision_1()` → void | перенесена |
| `17_control_room.hps:14` | `ControlRoom.CollideWithHerbertVision_2()` → void | перенесена |
| `17_control_room.hps:21` | `ControlRoom.SetUpHerbertVisionAreas()` → void | перенесена |
| `17_control_room.hps:81` | `ControlRoom.CollideTonArea(string asParent, string asChild, int alState)` → void | перенесена |
| `17_control_room.hps:135` | `ControlRoom.InteractLockedWheel(string asEntity)` → void | перенесена |
| `17_control_room.hps:144` | `ControlRoom.TimerLockedMess(string asTimer)` → void | перенесена |
| `17_control_room.hps:156` | `ControlRoom.StateChangePistonLever(string asEntity, int alState)` → void | перенесена |
| `17_control_room.hps:214` | `ControlRoom.TimerDelayProg(string asTimer)` → void | перенесена |
| `17_control_room.hps:226` | `ControlRoom.CollideLever(string asParent, string asChild, int alState)` → void | перенесена |
| `17_control_room.hps:236` | `ControlRoom.TimerReleaseLever(string asTimer)` → void | перенесена |
| `17_control_room.hps:247` | `ControlRoom.RemovePipeLight(string asEntity)` → void | перенесена |
| `17_control_room.hps:252` | `ControlRoom.StickyAttachPipe(string asStickyArea, string asBodyName)` → void | перенесена |
| `17_control_room.hps:299` | `ControlRoom.StickyDetachPipe(string asStickyArea, string asBodyName)` → void | перенесена |
| `17_control_room.hps:328` | `ControlRoom.FixSticky(string asStickyArea, bool bActive)` → void | перенесена |
| `17_control_room.hps:352` | `ControlRoom.DoProgress(int iProg)` → void | перенесена |
| `17_control_room.hps:398` | `ControlRoom.TimerAlexanderVoice(string asTimer)` → void | перенесена |
| `17_control_room.hps:403` | `ControlRoom.TimerShakeLoop(string asTimer)` → void | перенесена |
| `17_control_room.hps:417` | `ControlRoom.CollideBreakJoint(string asParent, string asChild, int alState)` → void | перенесена |
| `17_control_room.hps:425` | `ControlRoom.InteractBreakMe(string asEntity)` → void | перенесена |
| `17_control_room.hps:432` | `ControlRoom.TimerBreakJoint(string asTimer)` → void | перенесена |
| `17_control_room.hps:456` | `ControlRoom.CollideBrokenWheel(string asParent, string asChild, int alState)` → void | перенесена |
| `17_control_room.hps:461` | `ControlRoom.InteractBrokenWheelMount(string asEntity)` → void | перенесена |
| `17_control_room.hps:467` | `ControlRoom.TimerInteractWheelOnAgain(string asTimer)` → void | перенесена |
| `17_control_room.hps:474` | `ControlRoom.UseCrank(string asItem, string asEntity)` → void | перенесена |
| `17_control_room.hps:488` | `ControlRoom.EntityCallDiary(string asEntity, string type)` → void | перенесена |
| `17_control_room.hps:499` | `ControlRoom.InteractGate(string asEntity)` → void | перенесена |
| `17_control_room.hps:509` | `ControlRoom.TimerCloseGate(string asTimer)` → void | перенесена |
| `17_control_room.hps:520` | `ControlRoom.InteractWheel(string asEntity)` → void | перенесена |
| `17_control_room.hps:527` | `ControlRoom.TimerFixSaveMapLoad(string asTimer)` → void | перенесена |
| `17_control_room.hps:535` | `ControlRoom.OnStart()` → void | перенесена |
| `17_control_room.hps:603` | `ControlRoom.OnEnter()` → void | перенесена |
| `17_control_room.hps:621` | `ControlRoom.OnLeave()` → void | перенесена |

Итого: 32 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: глобальных полей нет.

Массивы (порядок, длина, повторы и значения сохранены):

- `vPuzzleNames` (string, `17_control_room.hps:59`), значения: "Weights", "Piston", "Pipes".
- `aL1` (float, `17_control_room.hps:161`), значения: -0.075f,0.0f,0.05f.
- `aL2` (float, `17_control_room.hps:162`), значения: 0.025f,0.0f,-0.03f.
- `aL3` (float, `17_control_room.hps:163`), значения: 0.025f,0.0f,-0.025f.
- `aL4` (float, `17_control_room.hps:164`), значения: -0.075f,0.0f,0.05f.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `17_control_room.hps:59`: Сохранён локальный vPuzzleNames из трёх элементов. Перед первым доступом добавлен выход при недопустимом FirstPuzzleSolved, повторяющий прерывание AngelScript после предшествующих действий.
- `17_control_room.hps:167`: Значения четырёх массивов поршня, сложение и все исходные состояния рычагов сохранены. fMove в именах/сообщениях форматируется через Str/G6.
- `17_control_room.hps:252`: Ручной StickyAttachPipe в диагностике проверял логику счётчиков; с 2026-10-17 прикрепление труб липкими зонами, цепи (`Rope0N_Left/Right`) и связи колёс с ними работают (CLAUDE.md «Верёвки, связи, липкие зоны, слизь»). Сквозной проход всех трёх загадок действиями игрока — ниже, «Сквозная проверка 2026-10-18».
- `17_control_room.hps:368`: Окончание трёх загадок устанавливает глобальный флаг работы моста в хабе 16.

## API движка

Проверены 44 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | ✓ (2026-10-17, сохранения перенесены (CLAUDE.md «Сохранения»)) | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs` |
| `InteractConnectPropWithMoveObject` | ✓ с 2026-10-17: CLAUDE.md «Верёвки, связи, липкие зоны, слизь» |
| `InteractConnectPropWithRope` | ✓ с 2026-10-17: CLAUDE.md «Верёвки, связи, липкие зоны, слизь» |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave с 2026-10-17 пишет автосохранение (CLAUDE.md «Сохранения»). Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/17_control_room.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_17_control_room.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 5. Полный отчёт: `Logs/Ch02_Import_17_control_room.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют ControlRoom.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× Decal 'filth_rust_7' has no or broken geometry, skipped
- 1× Decal 'dirt_stain_large_7' has no or broken geometry, skipped
- 1× entities/door/safety_normal_vert/safety_normal_vert_valve.ent: could not find child with unique ID 1 to attach to body 'Body_1'
- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/17_control_room.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 17_control_room -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/ControlRoom.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_17_control_room.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/17_control_room.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Первый fixture заранее ставил local-переменные поршня и затем вызывал обработчик: это вызвало повторные callbacks от SetLeverStuckState, Progress=7. Уточнённый fixture меняет состояние самих рычагов через API: Progress=3, ControlRoomEastSideWorking=1. Исходные обработчики не изменялись. Физическая работа верёвок и липких зон подтверждена позже сквозным прогоном (ниже).

### Сквозная проверка 2026-10-18

Все три загадки проходятся действиями игрока в одном прогоне `PlayerSimulation` (`-seed 1`, отчёт `Logs/ControlRoomCheck/Full2.txt`), без вызова обработчиков скрипта и без установки переменных:

- **Грузы.** Через 3 с после входа таймер `FixSaveMapLoad` регистрирует коллбэки зон `TonArea0N` (раньше груз проходит зону без события, как в оригинале). Игрок у каждого колеса (`goto -10.1 0 6.645/5.895/5.145 -90`, `aim Wheel_Chain0N_End_Left`, `interact`) крутит его мышью (`mouseuntil 0 6 TonArea0N 1` — пока не сработает зона) и отпускает. Колесо поднимает груз через `InteractConnectPropWithRope` и мотор цепи, отпущенный груз остаётся на месте. После третьего груза — `Progress=1`, грузы статичны, колёса застревают.
- **Поршень.** Рычаги (`goto -17.3 -4 14.0…15.5 -90`, `aim p_lever_N`): мышь вниз (+y) — состояние −1 (переменная 0), вверх — +1 (переменная 2). Нужное сочетание 0 / 2 / 2 / 0; коллбэки `L_Up_Area_N` / `L_Down_Area_N` срабатывают на пределах, через 1.5 с — `Progress=2`.
- **Трубы.** Только здесь есть упрощение: игрок не несёт трубы через уровень, их переставляет к стене `SetEntityPos` (пол у стены, 1 м перед игроком). Затем игрок берёт трубу (`interact`) и подносит её взглядом к гнезду (`aim StickySmallArea_1` / `StickySmallArea_4` / `StickyLargeArea_1`); липкая зона сама прикрепляет её за 0.5 с и вызывает `StickyAttachPipe`. Высокая труба стоит торцом вверх: луч через её центр масс проходит сквозь полость, поэтому перед захватом взгляд чуть опускается (`mouse 0 2 0.05`).
- **Итог.** `Progress=3`, `ControlRoomEastSideWorking=1`, `playpipesmall` / `playpipehigh_` / `playpipelarge` = 1, музыка `17_puzzle.ogg`, звуки `17_all_correct`, `17_metal_cog`, пар; 0 предупреждений, 0 ошибок.

### Sim_17_control_room_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_17_control_room_start.log` / `.txt`. Player simulation: 17_control_room — 2026-10-16 23:43:11 — 3 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (0.25, 0.90, -14.73) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 0,60 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] InteractConnectPropWithMoveObject: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_17_control_room_puzzles

ExitCode=0. Отчёты: `Logs/Ch02_Sim_17_control_room_puzzles.log` / `.txt`. Player simulation: 17_control_room — 2026-10-16 23:49:42 — 3 warnings, 0 errors.

```text
watch 30 2; call CollideWithHerbertVision_1; watch 10 2; call CollideTonArea Ton_Chain01_End_Left TonArea01 1; watch 10 2; call CollideTonArea Ton_Chain02_End_Left TonArea02 1; watch 10 2; call CollideTonArea Ton_Chain03_End_Left TonArea03 1; watch 10 2; call SetLocalVarInt p_lever_1 0; call SetLocalVarInt p_lever_2 2; call SetLocalVarInt p_lever_3 2; call StateChangePistonLever p_lever_4 -1; watch 10 2; call StickyAttachPipe StickySmallArea_1 playpipesmall_part01_Body_1; watch 10 2; call StickyAttachPipe StickySmallArea_4 playpipehigh_part02_Body_1; watch 10 2; call StickyAttachPipe StickyLargeArea_1 playpipelarge_part03_Body_1; watch 10 2; call CollideWithHerbertVision_2; watch 10 2; call GetLocalVarInt Progress; call GetGlobalVarInt ControlRoomEastSideWorking; call InteractBreakMe valve_iron_rusty_breakable_2; watch 10 2; call GiveItemFromFile crank_wood crank_wood_no_mount_item.ent; call UseCrank crank_wood AreaBrokenWheel; watch 10 2; call InteractGate safety_normal_vert_valve_1; watch 10 2; shot machinery
```

Фактическое состояние:

```text
t=120,21   call call GetLocalVarInt Progress → 7
t=120,23   call call GetGlobalVarInt ControlRoomEastSideWorking → 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] InteractConnectPropWithMoveObject: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_17_control_room_puzzles_prepared

ExitCode=0. Отчёты: `Logs/Ch02_Sim_17_control_room_puzzles_prepared.log` / `.txt`. Player simulation: 17_control_room — 2026-10-17 00:00:42 — 3 warnings, 0 errors.

```text
watch 30 2; call CollideWithHerbertVision_1; watch 10 2; call CollideTonArea Ton_Chain01_End_Left TonArea01 1; watch 10 2; call CollideTonArea Ton_Chain02_End_Left TonArea02 1; watch 10 2; call CollideTonArea Ton_Chain03_End_Left TonArea03 1; watch 10 2; call SetLeverStuckState p_lever_1 -1 true; watch 10 2; call SetLeverStuckState p_lever_2 1 true; watch 10 2; call SetLeverStuckState p_lever_3 1 true; watch 10 2; call SetLeverStuckState p_lever_4 -1 true; watch 10 2; call StickyAttachPipe StickySmallArea_1 playpipesmall_part01_Body_1; watch 10 2; call StickyAttachPipe StickySmallArea_4 playpipehigh_part02_Body_1; watch 10 2; call StickyAttachPipe StickyLargeArea_1 playpipelarge_part03_Body_1; watch 10 2; call CollideWithHerbertVision_2; watch 10 2; call GetLocalVarInt Progress; call GetGlobalVarInt ControlRoomEastSideWorking; call InteractBreakMe valve_iron_rusty_breakable_2; watch 10 2; call GiveItemFromFile crank_wood crank_wood_no_mount_item.ent; call UseCrank crank_wood AreaBrokenWheel; watch 10 2; call InteractGate safety_normal_vert_valve_1; watch 10 2; shot machinery; start PlayerStartArea_2; w 1; shot inner_room; health
```

Фактическое состояние:

```text
t=150,21   call call GetLocalVarInt Progress → 3
t=150,22   call call GetGlobalVarInt ControlRoomEastSideWorking → 1
t=181,32   health 100,00 sanity 100,00 dead False pos (-8.40, 0.90, 6.09) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] InteractConnectPropWithMoveObject: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_17_control_room.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=106, submeshes=167, geometryFailures=0, prefabMatrixDifferences=5, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=1
Scene: instances=1138, submeshMatrices=1328, sharedMeshColliders=873, failures=3, maxMatrixError=1,5
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 0, другое имя — у 0, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.

Повторяющиеся имена: mo1. Независимо сопоставлены 2 экземпляров по исходным ID/FileIndex, положениям, поворотам, масштабам и GUID префабов статики (сущности — по hplId и пути .ent). Неразрешённых сопоставлений: 0. `Logs/Ch02_DuplicateReview.json`. Сообщения PLACEMENT у этих имён вызваны выбором первого одноимённого объекта аудитором; импортёр и аудитор не изменены.

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
