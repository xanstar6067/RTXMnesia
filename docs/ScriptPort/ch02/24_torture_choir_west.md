# 24_torture_choir_west

Оригинал: `source/maps/main/ch02/24_torture_choir_west.hps` (767 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChoirWest.cs`.
Класс: `RTXMnesia.Levels.Ch02.TortureChoirWest`, атрибут: `HplMapScript("maps/main/ch02/24_torture_choir_west.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/24_torture_choir_west.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `24_torture_choir_west.hps:18` | `TortureChoirWest.OpenSwingDoor(bool abProp, string asDoorName, float afTimeBetweenSteps, int alSteps, float afX, float afY, float afZ, string asCoordSystem)` → void | перенесена |
| `24_torture_choir_west.hps:37` | `TortureChoirWest.TimerSwingDoorProp(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:45` | `TortureChoirWest.TimerSwingDoorBody(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:56` | `TortureChoirWest.SaveLanternState()` → void | перенесена |
| `24_torture_choir_west.hps:62` | `TortureChoirWest.RestoreLanternState()` → void | перенесена |
| `24_torture_choir_west.hps:70` | `TortureChoirWest.PlayEffectVoice(string asEntryBase, string asCat,int alStartIdx, int alEndIdx, string asCallback)` → void | перенесена |
| `24_torture_choir_west.hps:83` | `TortureChoirWest.RitualVoiceOver()` → void | перенесена |
| `24_torture_choir_west.hps:94` | `TortureChoirWest.ResumeEventTimer(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:101` | `TortureChoirWest.ResumeEventTimer(string asTimer, float afTime)` → void | перенесена |
| `24_torture_choir_west.hps:115` | `TortureChoirWest.PickDiary(string asItem, int alEntryIdx)` → void | перенесена |
| `24_torture_choir_west.hps:141` | `TortureChoirWest.PlayVisionVoice(string asEntry, int alStartIdx, int alEndIdx, string asCallback)` → void | перенесена |
| `24_torture_choir_west.hps:148` | `TortureChoirWest.VisionVoiceOver()` → void | перенесена |
| `24_torture_choir_west.hps:156` | `TortureChoirWest.VisionKnifeVoiceOver()` → void | перенесена |
| `24_torture_choir_west.hps:164` | `TortureChoirWest.InteractWithKnife(string asEntity)` → void | перенесена |
| `24_torture_choir_west.hps:181` | `TortureChoirWest.InteractWithPrisoner(string asEntity)` → void | перенесена |
| `24_torture_choir_west.hps:190` | `TortureChoirWest.TimerLookAtOver(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:199` | `TortureChoirWest.TimerPrisonerSquirm(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:207` | `TortureChoirWest.StartPrisonerSquirm()` → void | перенесена |
| `24_torture_choir_west.hps:211` | `TortureChoirWest.PrisonerSquirmOver(string asName)` → void | перенесена |
| `24_torture_choir_west.hps:220` | `TortureChoirWest.SetToolsVisible(bool abX)` → void | перенесена |
| `24_torture_choir_west.hps:231` | `TortureChoirWest.SetLightsOutsideRitualRoomVisible(bool abX)` → void | перенесена |
| `24_torture_choir_west.hps:280` | `TortureChoirWest.TimerRitualVision(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:645` | `TortureChoirWest.TimerHeartSound(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:653` | `TortureChoirWest.TimerShowPetals(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:668` | `TortureChoirWest.PlayerExitVision(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_west.hps:681` | `TortureChoirWest.CollideWithVoiceArea(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_west.hps:691` | `TortureChoirWest.TimerRandRockAmb(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:706` | `TortureChoirWest.InteractTableDesc(string asEntity)` → void | перенесена |
| `24_torture_choir_west.hps:712` | `TortureChoirWest.TimerInteractBackOn(string asTimer)` → void | перенесена |
| `24_torture_choir_west.hps:719` | `TortureChoirWest.OnStart()` → void | перенесена |
| `24_torture_choir_west.hps:750` | `TortureChoirWest.OnEnter()` → void | перенесена |
| `24_torture_choir_west.hps:764` | `TortureChoirWest.OnLeave()` → void | перенесена |

Итого: 32 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: `float gfVoiceDelay`, начальное значение `0.05f` (`24_torture_choir_west.hps:13`); `string gsLanternVar`, начальное значение `"lantern_active"` (`24_torture_choir_west.hps:14`)

Массивов нет.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `24_torture_choir_west.hps:94`: Сохранены обе перегрузки ResumeEventTimer (один string; string+float), на исходных строках 94 и 101. Статический checker считает 31 уникальное имя, фактически перенесены 32 функции.
- `24_torture_choir_west.hps:21`: OpenSwingDoor записывает afX в X/Y/Z, а timers читают X трижды. Эти исходные ошибки сохранены.
- `24_torture_choir_west.hps:683`: Недостижимые строки после return в CollideWithVoiceArea сохранены. CS0162 соответствует исходнику.
- `24_torture_choir_west.hps:209`: Анимация пленника остаётся NotPorted. Её ручной callback в диагностике не считается работающей анимацией.
- `24_torture_choir_west.hps:56`: Проверяются стадии ритуала, голосовые ожидания, нож/пленник/выход, сохранение фонаря и восстановление запретов игрока.

## API движка

Проверены 67 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | ✓ (2026-10-17, сохранения перенесены (CLAUDE.md «Сохранения»)) | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs` |
| `PlayPropAnimation` | ✓ с 2026-10-17 | CLAUDE.md «Титры, NPC, анимации и прикреплённые пропы» |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave с 2026-10-17 пишет автосохранение (CLAUDE.md «Сохранения»). Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/24_torture_choir_west.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_24_torture_choir_west.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 2. Полный отчёт: `Logs/Ch02_Import_24_torture_choir_west.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют TortureChoirWest.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/24_torture_choir_west.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 24_torture_choir_west -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChoirWest.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_24_torture_choir_west.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/24_torture_choir_west.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Ритуал прошёл голосовые остановки, нож, взаимодействие с пленником и выход. Итог TimerRitualVision=22, VisionActive=0, GetLanternActive=True, игрок жив и активен. Обе перегрузки ResumeEventTimer использованы через исходные уникальные обработчики. Картинка restored просмотрена. Анимации пленника всё ещё NotPorted.

### Sim_24_torture_choir_west_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_24_torture_choir_west_start.log` / `.txt`. Player simulation: 24_torture_choir_west — 2026-10-16 23:45:35 — 2 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (0.04, 0.90, 4.10) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] TortureChoirWest: several functions named 'ResumeEventTimer', callbacks use the first

### Sim_24_torture_choir_west_vision

ExitCode=0. Отчёты: `Logs/Ch02_Sim_24_torture_choir_west_vision.log` / `.txt`. Player simulation: 24_torture_choir_west — 2026-10-16 23:56:54 — 3 warnings, 0 errors.

```text
watch 30 2; call GiveItemFromFile lantern lantern.ent; call SetLanternActive true false; call PickDiary Diary_Ch02L24_DanielDiary07_03 1; watch 150 10; shot knife; call InteractWithKnife ceremony_knife; watch 100 10; shot prisoner; call InteractWithPrisoner AreaPrisonerMarks; watch 170 10; shot door; call RemoveEntityCollideCallback Player AreaLeaveVision; call PlayerExitVision Player AreaLeaveVision 1; watch 100 10; call GetLocalVarInt TimerRitualVision; call GetLocalVarInt VisionActive; call GetLanternActive; health; shot restored
```

Фактическое состояние:

```text
t=550,03   call call GetLocalVarInt TimerRitualVision → 22
t=550,04   call call GetLocalVarInt VisionActive → 0
t=550,06   call call GetLanternActive → True
t=550,08   health 100,00 sanity 65,00 dead False pos (-5.06, 0.90, 14.42) roll 0,0 pitch 0,0 yaw -88,2 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] TortureChoirWest: several functions named 'ResumeEventTimer', callbacks use the first
- 1× [Script] PlayPropAnimation: skipped, needs prop animations

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_24_torture_choir_west.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=49, submeshes=74, geometryFailures=0, prefabMatrixDifferences=4, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=0,677284
Scene: instances=375, submeshMatrices=404, sharedMeshColliders=201, failures=2, maxMatrixError=1,90735E-06
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 2, другое имя — у 0, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
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
