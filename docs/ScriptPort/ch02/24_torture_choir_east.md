# 24_torture_choir_east

Оригинал: `source/maps/main/ch02/24_torture_choir_east.hps` (927 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChoirEast.cs`.
Класс: `RTXMnesia.Levels.Ch02.TortureChoirEast`, атрибут: `HplMapScript("maps/main/ch02/24_torture_choir_east.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/24_torture_choir_east.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `24_torture_choir_east.hps:9` | `TortureChoirEast.CollideFallDeath(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:22` | `TortureChoirEast.TimerFallDeath(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:39` | `TortureChoirEast.CheckPointFallChasm(string asName, int alCount)` → void | перенесена |
| `24_torture_choir_east.hps:52` | `TortureChoirEast.CollideTerrorArea(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:90` | `TortureChoirEast.TimerTerrorDrain(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:110` | `TortureChoirEast.OpenIronMaiden(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:142` | `TortureChoirEast.TimerExtraMaidenImpulse(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:149` | `TortureChoirEast.TimerActiveMaidenEmotion(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:166` | `TortureChoirEast.IgniteBonfire(string asEntityName, string asType)` → void | перенесена |
| `24_torture_choir_east.hps:178` | `TortureChoirEast.TimedBrazenBullScream(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:234` | `TortureChoirEast.TimerBurnMore(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:257` | `TortureChoirEast.InteractPoisionFungi(string asEntity)` → void | перенесена |
| `24_torture_choir_east.hps:282` | `TortureChoirEast.TimerDecClickCount(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:289` | `TortureChoirEast.CollidePlayerWithFungi(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:302` | `TortureChoirEast.UseChipperOnFungi(string asItem, string asEntity)` → void | перенесена |
| `24_torture_choir_east.hps:309` | `TortureChoirEast.PosionFungiBreak(string asEntity, string asType)` → void | перенесена |
| `24_torture_choir_east.hps:326` | `TortureChoirEast.PickPoisonGland(string asEntityName, string asType)` → void | перенесена |
| `24_torture_choir_east.hps:357` | `TortureChoirEast.InitEnemies()` → void | перенесена |
| `24_torture_choir_east.hps:418` | `TortureChoirEast.ResetBruteEvent()` → void | перенесена |
| `24_torture_choir_east.hps:430` | `TortureChoirEast.EnemyDisabled(string asEnemy, string asType)` → void | перенесена |
| `24_torture_choir_east.hps:439` | `TortureChoirEast.EnemyCollideChasm(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:449` | `TortureChoirEast.CollideBruteEvent(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:490` | `TortureChoirEast.TimerBruteEventStart(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:501` | `TortureChoirEast.CheckPointBrute(string asName, int alCount)` → void | перенесена |
| `24_torture_choir_east.hps:517` | `TortureChoirEast.CollideTortureEntranceStart(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:536` | `TortureChoirEast.CollideTortureEntranceExit(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:555` | `TortureChoirEast.CollideTortureEntranceEnd(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:578` | `TortureChoirEast.PickOrbPiece(string asEntityName, string asType)` → void | перенесена |
| `24_torture_choir_east.hps:613` | `TortureChoirEast.InteractEmotionEffectBrazenBull(string asArea)` → void | перенесена |
| `24_torture_choir_east.hps:622` | `TortureChoirEast.InteractEmotionEffectIronMaiden(string asArea)` → void | перенесена |
| `24_torture_choir_east.hps:631` | `TortureChoirEast.WheelFlashbackOver()` → void | перенесена |
| `24_torture_choir_east.hps:637` | `TortureChoirEast.InteractEmotionEffectWheel(string asArea)` → void | перенесена |
| `24_torture_choir_east.hps:648` | `TortureChoirEast.TimerAlexanderVoice(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:658` | `TortureChoirEast.CollidePlant(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:663` | `TortureChoirEast.PlayerReact(float fTime)` → void | перенесена |
| `24_torture_choir_east.hps:669` | `TortureChoirEast.TimerPlayerReact(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:679` | `TortureChoirEast.descFungus(string asEntity)` → void | перенесена |
| `24_torture_choir_east.hps:686` | `TortureChoirEast.CollideAreaInMaiden(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:693` | `TortureChoirEast.LookAtAreaLookMaiden(string asEntity, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:712` | `TortureChoirEast.TimerAreaRock(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:725` | `TortureChoirEast.TimerEndRadial(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:731` | `TortureChoirEast.CollideAreaTriggerBones(string asParent, string asChild, int alState)` → void | перенесена |
| `24_torture_choir_east.hps:748` | `TortureChoirEast.TimerStopBones(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:762` | `TortureChoirEast.TimerPipeEvent(string asTimer)` → void | перенесена |
| `24_torture_choir_east.hps:822` | `TortureChoirEast.OnStart()` → void | перенесена |
| `24_torture_choir_east.hps:908` | `TortureChoirEast.OnEnter()` → void | перенесена |
| `24_torture_choir_east.hps:924` | `TortureChoirEast.OnLeave()` → void | перенесена |

Итого: 47 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: `int glNumOfBrutes`, начальное значение `6` (`24_torture_choir_east.hps:353`)

Массивов нет.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `24_torture_choir_east.hps:855`: Исходное 10.f записано 10.0f для C#. Значение сохранено; fSpeed в строке форматируется Str/G6.
- `24_torture_choir_east.hps:332`: PickPoisonGland повышает локальный IngredientCount, хотя хаб читает глобальный. Сохранено расхождение исходника.
- `24_torture_choir_east.hps:672`: TimerPlayerReact строит отсутствующий react_breathl; условие и строка сохранены.
- `24_torture_choir_east.hps:210`: TimerBurnMore после специальной ветки всё равно пытается проиграть звук stopradial_trigger_enemy. Не добавлен ранний return.
- `24_torture_choir_east.hps:279`: У cooldown PoisonInteract исходный callback пустой; предупреждение при истечении сохранено.
- `24_torture_choir_east.hps:473`: Динамические старты PlayerStartArea_2/3/4 и три ключа DeathBrute_24_TortureChoir_01/02/03 сохранены без переписывания маршрутизации.

## API движка

Проверены 59 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | NotPorted | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs:119` |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave остаётся заглушкой. Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/24_torture_choir_east.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_24_torture_choir_east.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 9. Полный отчёт: `Logs/Ch02_Import_24_torture_choir_east.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют TortureChoirEast.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× 'FogArea' is not supported yet (skipped)
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4'
- 1× Entity template 'ui_torture.ent' not found in source/
- 1× Map has a sky box: not converted yet (black background)
- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/24_torture_choir_east.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 24_torture_choir_east -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureChoirEast.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_24_torture_choir_east.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/24_torture_choir_east.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Проверены iron maiden / bull / wheel, шесть событий brute и настоящий CP/падение. Шесть быстрых InteractPoisionFungi реально разрушили гриб: poison_gland Item/Puzzle active=True. Пустой PoisonInteract callback дал исходное предупреждение. Подбор железы сохранил расхождение local IngredientCount=1 / global=0; ChoirOrbCount=3. В первом emotion-fixture передано несуществующее AreaEmotionEffectBrazenBull, отдельный fixture использует исходное AreaEmotionEffectBull. Белый снимок maiden занят исходным emotion-text overlay, для геометрии дополнительно просмотрен checkpoints.

### Sim_24_torture_choir_east_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_24_torture_choir_east_start.log` / `.txt`. Player simulation: 24_torture_choir_east — 2026-10-16 23:45:56 — 2 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (-1.66, 0.90, 2.45) roll 0,0 pitch -14,4 yaw -43,9 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Cannot find sound entity 'react_breathl'

### Sim_24_torture_choir_east_torture_fungi

ExitCode=0. Отчёты: `Logs/Ch02_Sim_24_torture_choir_east_torture_fungi.log` / `.txt`. Player simulation: 24_torture_choir_east — 2026-10-16 23:57:29 — 4 warnings, 0 errors.

```text
watch 30 2; call OpenIronMaiden Player AreaIronMaidenOpen 1; watch 10 2; call IgniteBonfire bonfire_1 OnIgnite; watch 10 2; call InteractEmotionEffectBrazenBull AreaEmotionEffectBrazenBull; watch 10 2; call InteractEmotionEffectIronMaiden AreaEmotionEffectIronMaiden; watch 10 2; call WheelFlashbackOver; watch 10 2; call InteractEmotionEffectWheel AreaEmotionEventWheel; watch 10 2; call UseChipperOnFungi stone_hammer_chipper poison_fungi_breakable; watch 10 2; call GiveItemFromFile poison_gland poison_gland.ent; active poison_gland false; call PickPoisonGland poison_gland OnPickup; watch 10 2; call PickOrbPiece OrbPiece_4 OnPickup; watch 10 2; call PickOrbPiece OrbPiece_5 OnPickup; watch 10 2; call PickOrbPiece OrbPiece_6 OnPickup; watch 10 2; call GetLocalVarInt IngredientCount; call GetGlobalVarInt IngredientCount; call GetGlobalVarInt ChoirOrbCount; shot fungi
```

Фактическое состояние:

```text
t=140,23   call call GetLocalVarInt IngredientCount → 1
t=140,24   call call GetGlobalVarInt IngredientCount → 0
t=140,26   call call GetGlobalVarInt ChoirOrbCount → 3
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Cannot find sound entity 'react_breathl'
- 1× [Script] ERROR: Cannot find sound entity 'stopradial_trigger_enemy'
- 1× [Script] Entity 'AreaEmotionEffectBrazenBull' with type any and subtype any does not exist!

### Sim_24_torture_choir_east_brutes_cp

ExitCode=0. Отчёты: `Logs/Ch02_Sim_24_torture_choir_east_brutes_cp.log` / `.txt`. Player simulation: 24_torture_choir_east — 2026-10-16 23:58:28 — 2 warnings, 0 errors.

```text
watch 30 2; call ResetBruteEvent; call CollideBruteEvent Player AreaBruteEvent_1 1; watch 10 2; call CollideTortureEntranceStart Player AreaTortureEntranceStart_1 1; watch 10 2; call CollideTortureEntranceExit Player AreaTortureEntranceExit_1 1; watch 10 2; call CollideTortureEntranceEnd Player AreaTortureEntranceEnd_1 1; watch 10 2; call ResetBruteEvent; call CollideBruteEvent Player AreaBruteEvent_2 1; watch 10 2; call CollideTortureEntranceStart Player AreaTortureEntranceStart_1 1; watch 10 2; call CollideTortureEntranceExit Player AreaTortureEntranceExit_1 1; watch 10 2; call CollideTortureEntranceEnd Player AreaTortureEntranceEnd_1 1; watch 10 2; call ResetBruteEvent; call CollideBruteEvent Player AreaBruteEvent_3 1; watch 10 2; call CollideTortureEntranceStart Player AreaTortureEntranceStart_1 1; watch 10 2; call CollideTortureEntranceExit Player AreaTortureEntranceExit_1 1; watch 10 2; call CollideTortureEntranceEnd Player AreaTortureEntranceEnd_1 1; watch 10 2; call ResetBruteEvent; call CollideBruteEvent Player AreaBruteEvent_4 1; watch 10 2; call CollideTortureEntranceStart Player AreaTortureEntranceStart_1 1; watch 10 2; call CollideTortureEntranceExit Player AreaTortureEntranceExit_1 1; watch 10 2; call CollideTortureEntranceEnd Player AreaTortureEntranceEnd_1 1; watch 10 2; call ResetBruteEvent; call CollideBruteEvent Player AreaBruteEvent_5 1; watch 10 2; call CollideTortureEntranceStart Player AreaTortureEntranceStart_1 1; watch 10 2; call CollideTortureEntranceExit Player AreaTortureEntranceExit_1 1; watch 10 2; call CollideTortureEntranceEnd Player AreaTortureEntranceEnd_1 1; watch 10 2; call ResetBruteEvent; call CollideBruteEvent Player AreaBruteEvent_6 1; watch 10 2; call CollideTortureEntranceStart Player AreaTortureEntranceStart_1 1; watch 10 2; call CollideTortureEntranceExit Player AreaTortureEntranceExit_1 1; watch 10 2; call CollideTortureEntranceEnd Player AreaTortureEntranceEnd_1 1; watch 10 2; call SetGlobalVarInt ChoirOrbCount 3; call CollideBruteEvent Player AreaBruteEvent_1 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call CollideFallDeath Player AreaFallDeath_1 1; watch 20 2; health; shot checkpoints
```

Фактическое состояние:

```text
t=292,81 [Script debug] Loading check point checkBrute1 Count: 0
t=300,52   health 52,00 sanity 92,50 dead False pos (-40.30, -0.42, 130.24) roll 0,0 pitch 0,0 yaw -27,1 | hurt 0,00 noise 0 move× 0,80 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_24_TortureChoir_01 | check point checkBrute1 at PlayerStartArea_3 count 1
t=313,82 [Script debug] Loading check point checkFallFirstWell Count: 0
t=320,53   health 52,00 sanity 92,50 dead False pos (0.00, 0.90, 0.73) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathFall_21_TortureNave | check point checkFallFirstWell at PlayerStartArea_1 count 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Cannot find sound entity 'react_breathl'

### Sim_24_torture_choir_east_fungi_prepared

ExitCode=0. Отчёты: `Logs/Ch02_Sim_24_torture_choir_east_fungi_prepared.log` / `.txt`. Player simulation: 24_torture_choir_east — 2026-10-17 00:02:49 — 4 warnings, 0 errors.

```text
watch 30 2; call IgniteBonfire bonfire_2 OnIgnite; watch 10 2; call InteractEmotionEffectBrazenBull AreaEmotionEffectBull; watch 10 2; call InteractPoisionFungi poison_fungi_breakable; call InteractPoisionFungi poison_fungi_breakable; call InteractPoisionFungi poison_fungi_breakable; call InteractPoisionFungi poison_fungi_breakable; call InteractPoisionFungi poison_fungi_breakable; call InteractPoisionFungi poison_fungi_breakable; watch 10 2; body poison_gland; start PlayerStartArea_maiden; shot maiden; health
```

Фактическое состояние:

```text
t= 60,15   health 100,00 sanity 80,00 dead False pos (-60.50, 1.44, 26.25) roll 0,0 pitch 0,0 yaw -90,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Cannot find sound entity 'react_breathl'
- 1× [Script] ERROR: Cannot find sound entity 'stopradial_trigger_enemy'
- 1× [Script] ERROR: Script call without a function name

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_24_torture_choir_east.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=127, submeshes=232, geometryFailures=0, prefabMatrixDifferences=5, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=1
Scene: instances=2732, submeshMatrices=4079, sharedMeshColliders=2538, failures=8, maxMatrixError=1,52588E-05
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 8, другое имя — у 0, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
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
