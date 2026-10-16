# 19_morgue

Оригинал: `source/maps/main/ch02/19_morgue.hps` (710 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Morgue.cs`.
Класс: `RTXMnesia.Levels.Ch02.Morgue`, атрибут: `HplMapScript("maps/main/ch02/19_morgue.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/19_morgue.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `19_morgue.hps:3` | `Morgue.UseDrillOnCorpseHeadArea(string asItem, string asEntity)` → void | перенесена |
| `19_morgue.hps:22` | `Morgue.TimerDrillHead(string asTimer)` → void | перенесена |
| `19_morgue.hps:36` | `Morgue.TimerStopDrillHead(string asTimer)` → void | перенесена |
| `19_morgue.hps:56` | `Morgue.TimerNoMoreDrill(string asTimer)` → void | перенесена |
| `19_morgue.hps:65` | `Morgue.UseDrillOnCorpseBodyArea(string asItem, string asEntity)` → void | перенесена |
| `19_morgue.hps:80` | `Morgue.UseTubeNeedleOnHead(string asItem, string asEntity)` → void | перенесена |
| `19_morgue.hps:115` | `Morgue.UseTubeOnHead(string asItem, string asEntity)` → void | перенесена |
| `19_morgue.hps:146` | `Morgue.UseNeedleOnHead(string asItem, string asEntity)` → void | перенесена |
| `19_morgue.hps:168` | `Morgue.UseTubeNeedleOnBody(string asItem, string asEntity)` → void | перенесена |
| `19_morgue.hps:175` | `Morgue.InteractCorpseHint(string asEntity)` → void | перенесена |
| `19_morgue.hps:192` | `Morgue.TimerInteractSounds(string asTimer)` → void | перенесена |
| `19_morgue.hps:205` | `Morgue.InteractCorpseHeadArea(string asEntity)` → void | перенесена |
| `19_morgue.hps:239` | `Morgue.TimerInjectSounds(string asTimer)` → void | перенесена |
| `19_morgue.hps:265` | `Morgue.TimerInjectSounds02(string asTimer)` → void | перенесена |
| `19_morgue.hps:276` | `Morgue.TimerInjectSoundsOver(string asTimer)` → void | перенесена |
| `19_morgue.hps:297` | `Morgue.InteractCorpseAllDone(string asEntity)` → void | перенесена |
| `19_morgue.hps:307` | `Morgue.TimerIntBackOn(string asTimer)` → void | перенесена |
| `19_morgue.hps:313` | `Morgue.TimerImageTrailOver(string asTimer)` → void | перенесена |
| `19_morgue.hps:318` | `Morgue.TimerCompleteAntidoteQuest(string asTimer)` → void | перенесена |
| `19_morgue.hps:327` | `Morgue.CollideShowBrute(string asParent, string asChild, int alState)` → void | перенесена |
| `19_morgue.hps:338` | `Morgue.InteractCellarDoor(string asEntity)` → void | перенесена |
| `19_morgue.hps:343` | `Morgue.ActivatePoundingBrute()` → void | перенесена |
| `19_morgue.hps:360` | `Morgue.TimerBrutePoundDoor(string asTimer)` → void | перенесена |
| `19_morgue.hps:390` | `Morgue.TimerStopHeadMove(string asTimer)` → void | перенесена |
| `19_morgue.hps:395` | `Morgue.CollideShowBrute02(string asParent, string asChild, int alState)` → void | перенесена |
| `19_morgue.hps:406` | `Morgue.CheckPoint01(string asName, int alCount)` → void | перенесена |
| `19_morgue.hps:412` | `Morgue.CheckPoint02(string asName, int alCount)` → void | перенесена |
| `19_morgue.hps:436` | `Morgue.TimerGuardianScream(string asTimer)` → void | перенесена |
| `19_morgue.hps:457` | `Morgue.VisionOver01()` → void | перенесена |
| `19_morgue.hps:461` | `Morgue.CollideAlexVoice(string asParent, string asChild, int alState)` → void | перенесена |
| `19_morgue.hps:465` | `Morgue.TimerAlexVoice(string asTimer)` → void | перенесена |
| `19_morgue.hps:473` | `Morgue.CollideLights(string asParent, string asChild, int alState)` → void | перенесена |
| `19_morgue.hps:506` | `Morgue.CollideAreaRooms(string asParent, string asChild, int alState)` → void | перенесена |
| `19_morgue.hps:519` | `Morgue.TimerEnterAreaRooms(string asTimer)` → void | перенесена |
| `19_morgue.hps:543` | `Morgue.EntityCallPickCopper(string asEntity, string type)` → void | перенесена |
| `19_morgue.hps:549` | `Morgue.CollideAreaCopperEvent(string asParent, string asChild, int alState)` → void | перенесена |
| `19_morgue.hps:560` | `Morgue.TimerECopper(string asTimer)` → void | перенесена |
| `19_morgue.hps:623` | `Morgue.SurvivorEnd()` → void | перенесена |
| `19_morgue.hps:627` | `Morgue.TimerSecondSurvivorFlash(string asTimer)` → void | перенесена |
| `19_morgue.hps:635` | `Morgue.OnStart()` → void | перенесена |
| `19_morgue.hps:690` | `Morgue.OnEnter()` → void | перенесена |
| `19_morgue.hps:707` | `Morgue.OnLeave()` → void | перенесена |

Итого: 42 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: глобальных полей нет.

Массивов нет.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `19_morgue.hps:182`: Два if и else в TimerInteractSounds сохранены: для имени 1 после первого звука выполняется else второго if и попытка назначить callback сущности 1.
- `19_morgue.hps:3`: Регистрации применения к телу намеренно вызывают те же обработчики головы, как в исходном OnStart. Не заменены на неиспользуемые helpers.
- `19_morgue.hps:510`: В звуковом вызове оставлено исходное имя ui_torture.ent. Импортёр пытается также трактовать литерал .ent как шаблон и предупреждает о его отсутствии.
- `19_morgue.hps:635`: У candlestick_wall_13 в .map ConnectedLight=PointLight_41, но такого света в карте нет. Это исходная ссылка данных, не ошибка C#-порта.
- Ссылка данных: `source/maps/main/ch02/19_morgue.map:4153` — ConnectedLight у candlestick_wall_13.

## API движка

Проверены 57 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | ✓ (2026-10-17, сохранения перенесены (CLAUDE.md «Сохранения»)) | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs` |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave с 2026-10-17 пишет автосохранение (CLAUDE.md «Сохранения»). Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/19_morgue.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_19_morgue.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 29. Полный отчёт: `Logs/Ch02_Import_19_morgue.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют Morgue.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× entities/organic/bone_pile_human/bone_pile_human1.dae: sub mesh 'polySurface3' material 'lambert3' not converted
- 1× entities/organic/bone_pile_human/bone_pile_human1.dae: sub mesh 'polySurface1' material 'lambert2' not converted
- 1× entities/organic/bone_pile_human/bone_pile_human1.dae: sub mesh 'polySurface52' material 'lambert3' not converted
- 1× entities/organic/bone_pile_human/bone_pile_human1.dae: sub mesh 'polySurface54' material 'lambert2' not converted
- 1× entities/organic/bone_pile_human/bone_pile_human1.dae: sub mesh 'polySurface56' material 'lambert2' not converted
- 1× entities/organic/bone_pile_human/bone_pile_human2.dae: sub mesh 'polySurface3' material 'lambert3' not converted
- 1× entities/organic/bone_pile_human/bone_pile_human2.dae: sub mesh 'polySurface1' material 'lambert2' not converted
- 1× entities/organic/bone_pile_human/bone_pile_human2.dae: sub mesh 'polySurface52' material 'lambert3' not converted
- 1× entities/organic/bone_pile_human/bone_pile_human2.dae: sub mesh 'polySurface54' material 'lambert2' not converted
- 1× entities/organic/bone_pile_human/bone_pile_human2.dae: sub mesh 'polySurface56' material 'lambert2' not converted
- 4× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1'
- 4× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2'
- 4× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3'
- 4× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4'
- 1× Entity template 'ui_torture.ent' not found in source/
- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/19_morgue.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 19_morgue -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Morgue.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_19_morgue.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/19_morgue.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Drill → кровь → трубка → игла → инъекция: PlayerHasAntidote=1 и управление восстановлено. Альтернативное применение готовой трубки с иглой выполняется в Play-fixture. Две настоящие смерти прошли через check01/check02. Entity 1 — исходное else второго if в TimerInteractSounds, не ошибка аргумента теста.

### Sim_19_morgue_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_19_morgue_start.log` / `.txt`. Player simulation: 19_morgue — 2026-10-16 23:43:47 — 2 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 94,81 dead False pos (0.00, 0.90, 7.00) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 0,60 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Light 'PointLight_41' that is connected to lamp 'candlestick_wall_13' does not exist!

### Sim_19_morgue_antidote

ExitCode=0. Отчёты: `Logs/Ch02_Sim_19_morgue_antidote.log` / `.txt`. Player simulation: 19_morgue — 2026-10-16 23:50:53 — 3 warnings, 0 errors.

```text
watch 30 2; call UseNeedleOnHead hollow_needle_1 AreaCorpseHead; watch 10 2; call InteractCorpseHint AreaCorpseHead; watch 10 2; call GiveItem handdrill Puzzle drill hand_drill.tga 0; call UseDrillOnCorpseHeadArea handdrill AreaCorpseHead; watch 10 2; call GiveItemFromFile copper_tube_1 copper_tube.ent; call UseTubeOnHead copper_tube_1 AreaCorpseHead; watch 10 2; call InteractCorpseHeadArea AreaCorpseHead; watch 10 2; call GiveItemFromFile hollow_needle_1 hollow_needle.ent; call UseNeedleOnHead hollow_needle_1 AreaCorpseHead; watch 10 2; call InteractCorpseHeadArea AreaCorpseHead; watch 25 2; call GetGlobalVarInt PlayerHasAntidote; body copper_tube_and_needle_1; shot antidote
```

Фактическое состояние:

```text
t=115,18   call call GetGlobalVarInt PlayerHasAntidote → 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Light 'PointLight_41' that is connected to lamp 'candlestick_wall_13' does not exist!
- 1× [Script] Entity '1' with type any and subtype any does not exist!

### Sim_19_morgue_brutes_cp

ExitCode=0. Отчёты: `Logs/Ch02_Sim_19_morgue_brutes_cp.log` / `.txt`. Player simulation: 19_morgue — 2026-10-16 23:51:18 — 2 warnings, 0 errors.

```text
watch 30 2; call SetEnemyDisableTriggers servant_brute_1 true; call SetEnemyDisableTriggers servant_brute_2 true; call CollideShowBrute Player AreaShowBrute 1; watch 20 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call CollideShowBrute02 Player AreaShowBrute02 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call VisionOver01; watch 10 2; call CollideAlexVoice Player AreaAlexVoice 1; watch 10 2; call CollideAreaRooms Player AreaRoom_1 1; watch 10 2; call CollideAreaRooms Player AreaRoom_1 -1; watch 10 2; call CollideAreaCopperEvent Player AreaCopperEvent 1; watch 20 2; health; shot brutes
```

Фактическое состояние:

```text
t= 62,35 [Script debug] Loading check point check01 Count: 0
t= 70,07   health 52,00 sanity 78,46 dead False pos (10.51, 0.90, 35.86) roll 0,0 pitch 0,0 yaw 117,6 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_18_Morgue_01 | check point check01 at PlayerStartArea_3 count 1
t= 92,39 [Script debug] Loading check point check02 Count: 0
t=100,11   health 52,00 sanity 70,96 dead False pos (16.39, -0.10, 41.27) roll 0,0 pitch 0,0 yaw 14,5 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_18_Morgue_02 | check point check02 at PlayerStartArea_4 count 1
t=160,19   health 52,00 sanity 44,96 dead False pos (16.39, -0.10, 41.27) roll 0,0 pitch 0,0 yaw 14,5 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_18_Morgue_02 | check point check02 at PlayerStartArea_4 count 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Light 'PointLight_41' that is connected to lamp 'candlestick_wall_13' does not exist!

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_19_morgue.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=128, submeshes=205, geometryFailures=0, prefabMatrixDifferences=9, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2
Scene: instances=571, submeshMatrices=722, sharedMeshColliders=334, failures=9, maxMatrixError=3,8147E-06
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 9, другое имя — у 0, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
Точные имена в сообщениях missing соответствуют SkinnedMeshRenderer; аудитор проверяет только MeshFilter. Численное сравнение вершин этих скелетных мешей этим аудитом не выполнено.

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
