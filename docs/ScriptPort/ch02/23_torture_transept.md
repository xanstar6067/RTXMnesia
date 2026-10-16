# 23_torture_transept

Оригинал: `source/maps/main/ch02/23_torture_transept.hps` (488 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureTransept.cs`.
Класс: `RTXMnesia.Levels.Ch02.TortureTransept`, атрибут: `HplMapScript("maps/main/ch02/23_torture_transept.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/23_torture_transept.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `23_torture_transept.hps:10` | `TortureTransept.InteractBloodHole(string asEntity)` → void | перенесена |
| `23_torture_transept.hps:18` | `TortureTransept.UseContainerOnBlood(string asItem, string asEntity)` → void | перенесена |
| `23_torture_transept.hps:24` | `TortureTransept.UseContainerOnHatch(string asItem, string asEntity)` → void | перенесена |
| `23_torture_transept.hps:39` | `TortureTransept.UseBloodContainerOnBlood(string asItem, string asEntity)` → void | перенесена |
| `23_torture_transept.hps:45` | `TortureTransept.InteractHatchDrainage(string asEntity)` → void | перенесена |
| `23_torture_transept.hps:52` | `TortureTransept.UseContainerAndStringOnBlood(string asItem, string asEntity)` → void | перенесена |
| `23_torture_transept.hps:64` | `TortureTransept.UseContainerAndStringOnHatch(string asItem, string asEntity)` → void | перенесена |
| `23_torture_transept.hps:79` | `TortureTransept.TimerGetBloodEvent(string asTimer)` → void | перенесена |
| `23_torture_transept.hps:125` | `TortureTransept.PickUpOrbPiece(string asEntity, string asType)` → void | перенесена |
| `23_torture_transept.hps:152` | `TortureTransept.CollideTerrorArea(string asParent, string asChild, int alState)` → void | перенесена |
| `23_torture_transept.hps:175` | `TortureTransept.TimerTerrorDrain(string asTimer)` → void | перенесена |
| `23_torture_transept.hps:193` | `TortureTransept.CollideStrappadoShackles(string asParent, string asChild, int alState)` → void | перенесена |
| `23_torture_transept.hps:229` | `TortureTransept.TimerStopRadial(string asTimer)` → void | перенесена |
| `23_torture_transept.hps:242` | `TortureTransept.CollideSawSounds(string asParent, string asChild, int alState)` → void | перенесена |
| `23_torture_transept.hps:291` | `TortureTransept.CollideCradleSoundEvent(string asParent, string asChild, int alState)` → void | перенесена |
| `23_torture_transept.hps:306` | `TortureTransept.PickUpTortureNote(string asEntity, string asType)` → void | перенесена |
| `23_torture_transept.hps:319` | `TortureTransept.InteractEmotionEffectSaw(string asArea)` → void | перенесена |
| `23_torture_transept.hps:326` | `TortureTransept.CradleFlashbackOver()` → void | перенесена |
| `23_torture_transept.hps:332` | `TortureTransept.InteractEmotionEffectCradle(string asArea)` → void | перенесена |
| `23_torture_transept.hps:339` | `TortureTransept.InteractEmotionEffectStrappado(string asArea)` → void | перенесена |
| `23_torture_transept.hps:347` | `TortureTransept.PlayerReact(float fTime, float fVolume, bool bSanity)` → void | перенесена |
| `23_torture_transept.hps:357` | `TortureTransept.TimerPlayerReact(string asTimer)` → void | перенесена |
| `23_torture_transept.hps:375` | `TortureTransept.CollideAreaStartBreath(string asParent, string asChild, int alState)` → void | перенесена |
| `23_torture_transept.hps:399` | `TortureTransept.TimerBreath(string asTimer)` → void | перенесена |
| `23_torture_transept.hps:404` | `TortureTransept.TimerStopBreath(string asTimer)` → void | перенесена |
| `23_torture_transept.hps:419` | `TortureTransept.OnStart()` → void | перенесена |
| `23_torture_transept.hps:472` | `TortureTransept.OnEnter()` → void | перенесена |
| `23_torture_transept.hps:485` | `TortureTransept.OnLeave()` → void | перенесена |

Итого: 28 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: глобальных полей нет.

Массивов нет.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `23_torture_transept.hps:160`: Литерал ui_torture.ent оставлен в звуковом вызове; предупреждение импортёра о шаблоне описано отдельно.
- `23_torture_transept.hps:384`: Выходное событие может выбрать AreaBreath_0. Диапазон RandInt сохранён.
- `23_torture_transept.hps:105`: Кровь повышает глобальный IngredientCount. Ручной вызов pickup-handler без выдачи предмета не объявляется настоящим подбором; fixtures описаны командами.

## API движка

Проверены 45 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | NotPorted | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs:119` |
| `InteractConnectPropWithRope` | NotPorted | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Entities.cs:488` |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave остаётся заглушкой. Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/23_torture_transept.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_23_torture_transept.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 5. Полный отчёт: `Logs/Ch02_Import_23_torture_transept.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют TortureTransept.

Типы/тексты предупреждений (повторы сгруппированы):

- 2× entities/ornament/paintings/painting_portrait/painting_portrait.ent: sub mesh 'painting_image02' not found in the model
- 1× Entity template 'ui_torture.ent' not found in source/
- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/23_torture_transept.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 23_torture_transept -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureTransept.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_23_torture_transept.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/23_torture_transept.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Кровь дала IngredientCount=1, три pickup-handler — TranseptOrbCount=3. Выдача/расход container_and_string явно перечислены в fixture. Ручные pickup-calls не доказывают поднятие предмета лучом. react_breathl — исходная конкатенация TimerPlayerReact. StopSound для ещё не созданных AreaTerror_1terror / saw_script_* в изолированных вызовах не доказывает отсутствие .snt.

### Sim_23_torture_transept_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_23_torture_transept_start.log` / `.txt`. Player simulation: 23_torture_transept — 2026-10-16 23:45:16 — 2 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (3.34, 0.90, 0.08) roll 0,0 pitch 0,0 yaw 90,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_23_torture_transept_blood_orbs

ExitCode=0. Отчёты: `Logs/Ch02_Sim_23_torture_transept_blood_orbs.log` / `.txt`. Player simulation: 23_torture_transept — 2026-10-16 23:55:59 — 6 warnings, 0 errors.

```text
watch 30 2; call InteractBloodHole AreaBloodHole; watch 10 2; call UseContainerOnBlood glass_container_1 AreaBloodHole; watch 10 2; call GiveItem container_and_string Puzzle container_and_string container_and_string.tga 0; call UseContainerAndStringOnBlood container_and_string AreaBloodHole; watch 10 2; call GetGlobalVarInt IngredientCount; call PickUpOrbPiece OrbPiece_1 OnPickup; watch 10 2; call PickUpOrbPiece OrbPiece_2 OnPickup; watch 10 2; call PickUpOrbPiece OrbPiece_3 OnPickup; watch 10 2; call CollideStrappadoShackles AreaStrappadoEvent_1 strappado_shackles_2 1; watch 10 2; call CollideSawSounds Player AreaSawSounds_2 1; watch 10 2; call CollideSawSounds Player AreaSawSounds_4 1; watch 10 2; call CradleFlashbackOver; watch 10 2; call GetGlobalVarInt TranseptOrbCount; shot blood
```

Фактическое состояние:

```text
t= 60,07   call call GetGlobalVarInt IngredientCount → 1
t=130,21   call call GetGlobalVarInt TranseptOrbCount → 3
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Could not find sound entity 'AreaTerror_1terror'
- 1× [Script] ERROR: Cannot find sound entity 'react_breathl'
- 1× [Script] ERROR: Could not find sound entity 'saw_script_loop'
- 1× [Script] ERROR: Could not find sound entity 'saw_script_event'

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_23_torture_transept.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=143, submeshes=240, geometryFailures=0, prefabMatrixDifferences=15, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=3,13934
Scene: instances=842, submeshMatrices=1061, sharedMeshColliders=657, failures=4, maxMatrixError=7,6294E-06
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 2, другое имя — у 2, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
Точные имена в сообщениях missing соответствуют SkinnedMeshRenderer; аудитор проверяет только MeshFilter. Численное сравнение вершин этих скелетных мешей этим аудитом не выполнено.
Два missing painting_image02 относятся к устаревшим повторным записям source/entities/ornament/paintings/painting_portrait/painting_portrait.ent:14 и :15. В .dae и сцене имеются painting_portrait и painting_image_portrait; актуальные записи .ent:16–17 также присутствуют. HPL2 EntityLoader_Object.cpp:386–392 ищет подмеш по имени и пропускает отсутствующий с предупреждением. Это расхождение исходных записей, сохранённое импортёром; два missing rat относятся к SkinnedMeshRenderer.

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
