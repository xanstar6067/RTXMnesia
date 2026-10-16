# 22_torture_chancel

Оригинал: `source/maps/main/ch02/22_torture_chancel.hps` (808 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChancel.cs`.
Класс: `RTXMnesia.Levels.Ch02.TortureChancel`, атрибут: `HplMapScript("maps/main/ch02/22_torture_chancel.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/22_torture_chancel.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `22_torture_chancel.hps:9` | `TortureChancel.UseStringOnPedestal(string asItem, string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:16` | `TortureChancel.UseOrbPieceOnPedestal(string asItem, string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:84` | `TortureChancel.TimerFadeProp(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:92` | `TortureChancel.UseTarOnPedestal(string asItem, string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:107` | `TortureChancel.InteractOrbPedestal(string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:121` | `TortureChancel.CollideFallDeath(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:147` | `TortureChancel.TimerFallDeath(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:163` | `TortureChancel.CheckPointFallDeath(string asName, int alCount)` → void | перенесена |
| `22_torture_chancel.hps:178` | `TortureChancel.CollideAmbushRumble(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:228` | `TortureChancel.TimerAmbushRumbleFadeOut(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:238` | `TortureChancel.CollideAmbushStart(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:270` | `TortureChancel.TimerStartGrunt(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:279` | `TortureChancel.CollideGruntsStartHunt(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:290` | `TortureChancel.TimerEnableGruntTriggers(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:298` | `TortureChancel.CollideAmbushInstantKill(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:308` | `TortureChancel.CheckPointGrunt(string asName, int alCount)` → void | перенесена |
| `22_torture_chancel.hps:318` | `TortureChancel.TimerChangeMap(string asChangeMap)` → void | перенесена |
| `22_torture_chancel.hps:337` | `TortureChancel.CollideWindBreeze(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:352` | `TortureChancel.TimerBreezeOver(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:366` | `TortureChancel.InitBarrierMachine()` → void | перенесена |
| `22_torture_chancel.hps:385` | `TortureChancel.InitBarrierTunnel()` → void | перенесена |
| `22_torture_chancel.hps:398` | `TortureChancel.CollideTunnelDamage(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:414` | `TortureChancel.TimerTunnelDamage(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:434` | `TortureChancel.CollideFlashbackArea(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:440` | `TortureChancel.TimerPetals(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:447` | `TortureChancel.FlashbackOver()` → void | перенесена |
| `22_torture_chancel.hps:461` | `TortureChancel.CollideAlexanderVoice(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:469` | `TortureChancel.TimerAlexVoiceRumble(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:478` | `TortureChancel.TimerAlexVoiceRumbleFadeOut(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:486` | `TortureChancel.CollideRoomCount(string asParent, string asChild, int alState)` → void | перенесена |
| `22_torture_chancel.hps:509` | `TortureChancel.PickNote(string asEntity, string asType)` → void | перенесена |
| `22_torture_chancel.hps:514` | `TortureChancel.PickDiary(string asEntity, string asType)` → void | перенесена |
| `22_torture_chancel.hps:527` | `TortureChancel.PickBucketOfTar(string asEntity, string asType)` → void | перенесена |
| `22_torture_chancel.hps:540` | `TortureChancel.InteractLargeGate(string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:546` | `TortureChancel.TimerLargeGate(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:552` | `TortureChancel.InteractMachineRoomDoor(string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:560` | `TortureChancel.UseHammerChipperOnMachineDoor(string asItem, string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:568` | `TortureChancel.PlayerReact(bool bDamage, float fTime)` → void | перенесена |
| `22_torture_chancel.hps:579` | `TortureChancel.TimerPlayerReactions(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:598` | `TortureChancel.InteractAreaRitualFurniture(string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:612` | `TortureChancel.InteractAreaRitualTable(string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:626` | `TortureChancel.TimerInteractFlashOver(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:643` | `TortureChancel.TimerInteractBackOn(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:650` | `TortureChancel.InteractAreaTunnelDamage(string asEntity)` → void | перенесена |
| `22_torture_chancel.hps:660` | `TortureChancel.CPCallAltar(string asName, int alCount)` → void | перенесена |
| `22_torture_chancel.hps:669` | `TortureChancel.TimerCPCall(string asTimer)` → void | перенесена |
| `22_torture_chancel.hps:684` | `TortureChancel.EntityCallGruntDisabled(string asEntity, string type)` → void | перенесена |
| `22_torture_chancel.hps:694` | `TortureChancel.OnStart()` → void | перенесена |
| `22_torture_chancel.hps:790` | `TortureChancel.OnEnter()` → void | перенесена |
| `22_torture_chancel.hps:804` | `TortureChancel.OnLeave()` → void | перенесена |

Итого: 50 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: глобальных полей нет.

Массивов нет.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `22_torture_chancel.hps:132`: При CPAltar=1 CollideFallDeath регистрирует отсутствующий CheckPointFallWell; в исходнике объявлен CheckPointFallDeath. Имя не исправлено и новая функция не добавлена.
- `22_torture_chancel.hps:311`: Засада после смерти вызывает CheckPointGrunt и отложенный TimerChangeMap в ch03/25_cell_tunnels. Эта цепочка вынесена в интеграционный Play-прогон.
- `22_torture_chancel.hps:694`: ParticleSystem_1 (ID 2835) в .map имеет пустой File; предупреждение создания ожидаемо по исходным данным.
- Ссылка данных: `source/maps/main/ch02/22_torture_chancel.map:7406` — пустой File у ParticleSystem_1.

## API движка

Проверены 59 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | ✓ (2026-10-17, сохранения перенесены (CLAUDE.md «Сохранения»)) | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs` |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave с 2026-10-17 пишет автосохранение (CLAUDE.md «Сохранения»). Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/22_torture_chancel.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_22_torture_chancel.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 3. Полный отчёт: `Logs/Ch02_Import_22_torture_chancel.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют TortureChancel.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× 'FogArea' is not supported yet (skipped)
- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/22_torture_chancel.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 22_torture_chancel -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChancel.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_22_torture_chancel.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/22_torture_chancel.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Проверены отказ второго фрагмента без смолы, добавление смолы и повторное добавление: OrbsAddedCount=2, TarOnPedestal=1. Ошибочный asEntity=safety_large_vert_1 в первом диагностическом gate-call заменён в отдельном fixture на исходную castle_gate_arched01_1; исходник порта не менялся. Две смерти при падении показали CPCallAltar и затем отсутствующий исходный CheckPointFallWell. Засада/захват проверяются только настоящим Play-переходом в главу 3.

### Sim_22_torture_chancel_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_22_torture_chancel_start.log` / `.txt`. Player simulation: 22_torture_chancel — 2026-10-16 23:44:59 — 2 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (-23.75, -5.99, 42.40) roll 0,0 pitch 0,0 yaw -180,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point checkpoint_default at PlayerStartArea_6 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] ERROR: Couldn't create particle system 'ParticleSystem_1' of type ''
- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_22_torture_chancel_orb_feedback

ExitCode=0. Отчёты: `Logs/Ch02_Sim_22_torture_chancel_orb_feedback.log` / `.txt`. Player simulation: 22_torture_chancel — 2026-10-16 23:55:37 — 3 warnings, 0 errors.

```text
watch 30 2; call InteractOrbPedestal orb_pedestal_1; watch 10 2; call GiveItemFromFile OrbPiece_1 orbpiece_crimson.ent; call UseOrbPieceOnPedestal OrbPiece_1 orb_pedestal_1; watch 10 2; call GiveItemFromFile OrbPiece_2 orbpiece_crimson.ent; call UseOrbPieceOnPedestal OrbPiece_2 orb_pedestal_1; watch 10 2; call GiveItemFromFile bucket_of_tar_1 bucket_of_tar.ent; call UseTarOnPedestal bucket_of_tar_1 orb_pedestal_1; watch 10 2; call UseOrbPieceOnPedestal OrbPiece_2 orb_pedestal_1; watch 10 2; call InteractMachineRoomDoor prison_section_static_1; watch 10 2; call InteractLargeGate safety_large_vert_1; watch 10 2; call CollideTunnelDamage Player AreaTunnelDamage 1; watch 10 2; call CollideTunnelDamage Player AreaTunnelDamage -1; watch 10 2; call GetGlobalVarInt OrbsAddedCount; call GetGlobalVarInt TarOnPedestal; call CollideFallDeath Player AreaFallDeath 1; watch 20 2; health; shot orb
```

Фактическое состояние:

```text
t=120,21   call call GetGlobalVarInt OrbsAddedCount → 2
t=120,23   call call GetGlobalVarInt TarOnPedestal → 1
t=133,54 [Script debug] Loading check point checkpoint_default Count: 0
t=140,26   health 52,00 sanity 21,29 dead False pos (-45.25, -0.84, -18.25) roll 0,0 pitch 25,0 yaw -181,7 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point checkpoint_default at PlayerStartArea_6 count 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] ERROR: Couldn't create particle system 'ParticleSystem_1' of type ''
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] Entity 'safety_large_vert_1' with type any and subtype any does not exist!

### Sim_22_torture_chancel_gate_prepared

ExitCode=0. Отчёты: `Logs/Ch02_Sim_22_torture_chancel_gate_prepared.log` / `.txt`. Player simulation: 22_torture_chancel — 2026-10-17 00:02:26 — 3 warnings, 0 errors.

```text
watch 30 2; call InteractLargeGate castle_gate_arched01_1; watch 10 2; call CollideFlashbackArea Player FlashbackArea_1 1; watch 10 2; call FlashbackOver; watch 10 2; call CollideAlexanderVoice Player AreaAlexanderVoice 1; watch 10 2; call CollideFallDeath Player AreaFallDeath 1; watch 20 2; health; call CollideFallDeath Player AreaFallDeath 1; watch 20 2; health; start PlayerStartArea_6; shot altar
```

Фактическое состояние:

```text
t= 83,37 [Script debug] Loading check point checkpoint_default Count: 0
t= 90,09   health 52,00 sanity 60,00 dead False pos (-45.25, -0.84, -18.25) roll 0,0 pitch 25,0 yaw -181,7 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point checkpoint_default at PlayerStartArea_6 count 1
t=103,39 [Script debug] Loading check point checkFallDeath3 Count: 0
t=110,11   health 52,00 sanity 60,00 dead False pos (-45.25, -0.84, -18.25) roll 0,0 pitch 0,0 yaw -180,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathFall_21_TortureNave | check point checkFallDeath3 at PlayerStartArea_6 count 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] ERROR: Couldn't create particle system 'ParticleSystem_1' of type ''
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Script function 'CheckPointFallWell' does not exist in TortureChancel

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_22_torture_chancel.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=155, submeshes=279, geometryFailures=0, prefabMatrixDifferences=7, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2
Scene: instances=1733, submeshMatrices=2064, sharedMeshColliders=1320, failures=178, maxMatrixError=25,5
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 5, другое имя — у 5, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
Точные имена в сообщениях missing соответствуют SkinnedMeshRenderer; аудитор проверяет только MeshFilter. Численное сравнение вершин этих скелетных мешей этим аудитом не выполнено.
Для servant_grunt присутствует SkinnedMeshRenderer servant_grunt 1 с mesh GUID efd1d42486a630a40bee10a3c8b05d7e и fileID -4556356290143606882 (тот же исходный меш), плюс отдельные волосы. Это конкретная проверка имени/ресурса, а не предположение об отсутствии геометрии.

Повторяющиеся имена: pillar_hub_pillar_83, pillar_hub_pillar_84, pillar_hub_pillar_85, pillar_hub_pillar_86, pipesthick_bend_short_2, pipesthick_bend_short_8, pipesthick_straight_medium_15, pipesthick_straight_medium_16, pipesthick_straight_medium_2, pipesthick_straight_medium_6, pipesthick_straight_medium_7, wallcorner_concave_extension_diagonal_2, wallcorner_concave_extension_diagonal_3, wallcorridor_extension_2m_2, wallcorridor_extension_2m_3, walldefault_extension_diagonal_2, walldefault_extension_diagonal_3, wallwelder_wood_14, wallwelder_wood_15, wallwelder_wood_16. Независимо сопоставлены 104 экземпляров по исходным ID/FileIndex, положениям, поворотам, масштабам и GUID префабов статики (сущности — по hplId и пути .ent). Неразрешённых сопоставлений: 0. `Logs/Ch02_DuplicateReview.json`. Сообщения PLACEMENT у этих имён вызваны выбором первого одноимённого объекта аудитором; импортёр и аудитор не изменены.

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
