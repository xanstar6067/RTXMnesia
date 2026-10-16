# 18_cistern

Оригинал: `source/maps/main/ch02/18_cistern.hps` (740 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Cistern.cs`.
Класс: `RTXMnesia.Levels.Ch02.Cistern`, атрибут: `HplMapScript("maps/main/ch02/18_cistern.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/18_cistern.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `18_cistern.hps:5` | `Cistern.StartMusicHelper(int alValveCount, bool abPlayEventMusic)` → void | перенесена |
| `18_cistern.hps:54` | `Cistern.RotateWaterValve01(string asEntity, int alState)` → void | перенесена |
| `18_cistern.hps:79` | `Cistern.RotateWaterValve02(string asEntity, int alState)` → void | перенесена |
| `18_cistern.hps:109` | `Cistern.RotateWaterValve03(string asEntity, int alState)` → void | перенесена |
| `18_cistern.hps:135` | `Cistern.TimerPumpDelay(string asTimer)` → void | перенесена |
| `18_cistern.hps:152` | `Cistern.PumpWater()` → void | перенесена |
| `18_cistern.hps:239` | `Cistern.TimerPumpRumbleStop(string asTimer)` → void | перенесена |
| `18_cistern.hps:244` | `Cistern.TimerLastWallMess(string asTimer)` → void | перенесена |
| `18_cistern.hps:257` | `Cistern.TimerWaterHoleVoice(string asTimer)` → void | перенесена |
| `18_cistern.hps:305` | `Cistern.TimerSteam(string asTimer)` → void | перенесена |
| `18_cistern.hps:330` | `Cistern.TimerTurnOffSteam(string asTimer)` → void | перенесена |
| `18_cistern.hps:338` | `Cistern.CollidePlayerSteam(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:351` | `Cistern.InteractSteamValve(string asEntity, int alState)` → void | перенесена |
| `18_cistern.hps:378` | `Cistern.CollideBreakBridge(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:397` | `Cistern.TimerChainForce(string asTimer)` → void | перенесена |
| `18_cistern.hps:403` | `Cistern.InteractBridge(string asEntity)` → void | перенесена |
| `18_cistern.hps:414` | `Cistern.TimerInteractBridge(string asTimer)` → void | перенесена |
| `18_cistern.hps:419` | `Cistern.CollideImpactBridge(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:427` | `Cistern.CollideLurkerEventCenter(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:447` | `Cistern.CollideLurkerCorridorEvent1(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:452` | `Cistern.CollideLurkerCorridorEvent2(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:459` | `Cistern.CollideLurkerCorridorEvent3(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:466` | `Cistern.TimerLurkerSplash(string asTimer)` → void | перенесена |
| `18_cistern.hps:475` | `Cistern.EntityCallPickDiary(string asEntity, string type)` → void | перенесена |
| `18_cistern.hps:486` | `Cistern.CollideAreaRoseOn(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:499` | `Cistern.TimerAreaRose(string asTimer)` → void | перенесена |
| `18_cistern.hps:506` | `Cistern.CollideAreaRoseOff(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:518` | `Cistern.InteractPrisonWall(string asEntity)` → void | перенесена |
| `18_cistern.hps:548` | `Cistern.TimerInteractPrisonWall(string asTimer)` → void | перенесена |
| `18_cistern.hps:557` | `Cistern.CollideAreaGuardian(string asParent, string asChild, int alState)` → void | перенесена |
| `18_cistern.hps:590` | `Cistern.TimerStartLurky(string asTimer)` → void | перенесена |
| `18_cistern.hps:600` | `Cistern.TimerGuardNoises(string asTimer)` → void | перенесена |
| `18_cistern.hps:613` | `Cistern.CPCallCheckPoint(string asName, int alCount)` → void | перенесена |
| `18_cistern.hps:630` | `Cistern.OnStart()` → void | перенесена |
| `18_cistern.hps:682` | `Cistern.OnEnter()` → void | перенесена |
| `18_cistern.hps:725` | `Cistern.OnLeave()` → void | перенесена |

Итого: 36 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: `float[] gvAmbientColorLevels`, начальное значение см. массив ниже (`18_cistern.hps:146`); `bool bFirst`, начальное значение `true` (`18_cistern.hps:517`)

Массивы (порядок, длина, повторы и значения сохранены):

- `gvAmbientColorLevels` (float, `18_cistern.hps:146`), значения: 0.025f * 0.8f, 0.15f * 0.8f, 0.18f * 0.8f, 0.025f * 0.6f, 0.15f * 0.6f, 0.18f * 0.6f, 0.025f * 0.3f, 0.15f * 0.3f, 0.18f * 0.3f.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `18_cistern.hps:9`: Четыре неинициализированные локальные float в StartMusicHelper явно получают 0, как примитивные локальные переменные AngelScript. Условия музыки и switch сохранены.
- `18_cistern.hps:161`: На четвёртом PumpWater исходный индекс выходит за массив. Выход добавлен после AddQuest и активации AreaLurkerEventCenter, перед чтением цветов. Счётчик остаётся 3.
- `18_cistern.hps:508`: CollideAreaRoseOff удаляет rose_1/2/3, хотя другие обработчики создают системы с именами AreaRose_N. Несовпадение исходника сохранено.

## API движка

Проверены 44 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | ✓ (2026-10-17, сохранения перенесены (CLAUDE.md «Сохранения»)) | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs` |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave с 2026-10-17 пишет автосохранение (CLAUDE.md «Сохранения»). Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/18_cistern.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_18_cistern.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 2. Полный отчёт: `Logs/Ch02_Import_18_cistern.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют Cistern.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/18_cistern.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 18_cistern -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Cistern.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_18_cistern.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/18_cistern.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Три исходных клапана дали PumpWaterCount=3 и WaterHasBeenDrained=1. Четвёртый PumpWater прерван на исходной ошибке массива; счётчик остался 3. Проверены пар/урон, сломанный мост, постановочные всплески и настоящий CP после смерти. Активация WaterLurker в этом сценарии не заменяет полный тест его ИИ.

### Sim_18_cistern_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_18_cistern_start.log` / `.txt`. Player simulation: 18_cistern — 2026-10-16 23:43:29 — 1 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (-2.00, 3.62, 1.64) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_18_cistern_valves_guardian

ExitCode=0. Отчёты: `Logs/Ch02_Sim_18_cistern_valves_guardian.log` / `.txt`. Player simulation: 18_cistern — 2026-10-16 23:50:07 — 1 warnings, 0 errors.

```text
watch 30 2; call RotateWaterValve01 valve_iron_rusty_1 1; watch 10 2; call CollideLurkerEventCenter Player AreaLurkerEventCenter 1; watch 10 2; call RotateWaterValve02 valve_iron_rusty_2 1; watch 10 2; call CollideLurkerEventCenter Player AreaLurkerEventCenter 1; watch 10 2; call RotateWaterValve03 valve_iron_rusty_3 1; watch 10 2; call CollideLurkerEventCenter Player AreaLurkerEventCenter 1; watch 10 2; call GetLocalVarInt PumpWaterCount; call GetGlobalVarInt WaterHasBeenDrained; call PumpWater; watch 10 2; call GetLocalVarInt PumpWaterCount; sanity 60; call CollideAreaGuardian Player AreaGuardian 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; enemy; shot pumped
```

Фактическое состояние:

```text
t= 90,10   call call GetLocalVarInt PumpWaterCount → 3
t= 90,12   call call GetGlobalVarInt WaterHasBeenDrained → 1
t=100,16   call call GetLocalVarInt PumpWaterCount → 3
t=122,50 [Script debug] Loading check point cp1 Count: 0
t=130,21   health 52,00 sanity 40,58 dead False pos (17.43, 3.52, 30.83) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/18_WaterLurk | check point cp1 at PlayerStartArea_4 count 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_18_cistern_bridge_steam

ExitCode=0. Отчёты: `Logs/Ch02_Sim_18_cistern_bridge_steam.log` / `.txt`. Player simulation: 18_cistern — 2026-10-16 23:50:31 — 1 warnings, 0 errors.

```text
watch 30 2; call InteractBridge bridge_chained_1; watch 10 2; call CollideBreakBridge stone_small01_1 AreaBreakBridge_1 1; watch 10 2; call CollidePlayerSteam Player AreaPushPlayer_1 1; watch 10 2; call CollidePlayerSteam Player AreaPushPlayer_1 -1; watch 10 2; call InteractSteamValve valve_iron_rusty_4 1; watch 10 2; call InteractSteamValve valve_iron_rusty_4 -1; watch 10 2; call CollideAreaRoseOn Player FlashbackArea_1 1; watch 10 2; body bridge_chained_1; shot bridge
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_18_cistern.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=89, submeshes=138, geometryFailures=0, prefabMatrixDifferences=0, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2,38419E-07
Scene: instances=883, submeshMatrices=1187, sharedMeshColliders=695, failures=4, maxMatrixError=7,6294E-06
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 4, другое имя — у 0, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
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
