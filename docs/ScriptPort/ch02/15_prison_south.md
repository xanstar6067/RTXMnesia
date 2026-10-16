# 15_prison_south

Оригинал: `source/maps/main/ch02/15_prison_south.hps` (1215 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/PrisonSouth.cs`.
Класс: `RTXMnesia.Levels.Ch02.PrisonSouth`, атрибут: `HplMapScript("maps/main/ch02/15_prison_south.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/15_prison_south.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `15_prison_south.hps:5` | `PrisonSouth.InteractBarDoor(string asEntity)` → void | перенесена |
| `15_prison_south.hps:18` | `PrisonSouth.TimerRattle(string asTimer)` → void | перенесена |
| `15_prison_south.hps:36` | `PrisonSouth.UseHCDoor(string asItem, string asEntity)` → void | перенесена |
| `15_prison_south.hps:48` | `PrisonSouth.CollideAreaHammer(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:55` | `PrisonSouth.CollideMoveBed(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:72` | `PrisonSouth.InteractHole(string asEntity)` → void | перенесена |
| `15_prison_south.hps:78` | `PrisonSouth.TimerHoleSmall(string asTimer)` → void | перенесена |
| `15_prison_south.hps:85` | `PrisonSouth.UseHCH(string asItem, string asEntity)` → void | перенесена |
| `15_prison_south.hps:111` | `PrisonSouth.BeingHoleBreak()` → void | перенесена |
| `15_prison_south.hps:134` | `PrisonSouth.TimerMoveHammer(string asTimer)` → void | перенесена |
| `15_prison_south.hps:177` | `PrisonSouth.BreakHole()` → void | перенесена |
| `15_prison_south.hps:203` | `PrisonSouth.UseHCHB(string asItem, string asEntity)` → void | перенесена |
| `15_prison_south.hps:210` | `PrisonSouth.CollideExitArea(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:216` | `PrisonSouth.InteractHoleExit(string asEntity)` → void | перенесена |
| `15_prison_south.hps:228` | `PrisonSouth.CollideAreaDrag(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:239` | `PrisonSouth.TimerDoPrisoner01(string asTimer)` → void | перенесена |
| `15_prison_south.hps:256` | `PrisonSouth.TimerEPlayerReact(string asTimer)` → void | перенесена |
| `15_prison_south.hps:327` | `PrisonSouth.CollideAreaDrag02(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:334` | `PrisonSouth.TimerEBangLast(string asTimer)` → void | перенесена |
| `15_prison_south.hps:398` | `PrisonSouth.EntityCallMonsterNoise(string asEntity, string type)` → void | перенесена |
| `15_prison_south.hps:402` | `PrisonSouth.TimerMakeMonsterNoise(string asTimer)` → void | перенесена |
| `15_prison_south.hps:416` | `PrisonSouth.TimerEIntro(string asTimer)` → void | перенесена |
| `15_prison_south.hps:560` | `PrisonSouth.CollideAreaGrunt(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:579` | `PrisonSouth.CollideAreaFlickGrunt(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:592` | `PrisonSouth.TimerLightOff02(string asTimer)` → void | перенесена |
| `15_prison_south.hps:605` | `PrisonSouth.CPCallDeath(string asName, int alCount)` → void | перенесена |
| `15_prison_south.hps:623` | `PrisonSouth.TimerCPEvent(string asTimer)` → void | перенесена |
| `15_prison_south.hps:661` | `PrisonSouth.TimerLightOff(string asTimer)` → void | перенесена |
| `15_prison_south.hps:668` | `PrisonSouth.CollideAreaRumble(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:679` | `PrisonSouth.TimerEndRumble(string asTimer)` → void | перенесена |
| `15_prison_south.hps:695` | `PrisonSouth.TimerPlayerReact(string asTimer)` → void | перенесена |
| `15_prison_south.hps:703` | `PrisonSouth.InteractLockedDoor(string asEntity)` → void | перенесена |
| `15_prison_south.hps:711` | `PrisonSouth.TimerDoorMess(string asTimer)` → void | перенесена |
| `15_prison_south.hps:715` | `PrisonSouth.UseHCExtra(string asItem, string asEntity)` → void | перенесена |
| `15_prison_south.hps:740` | `PrisonSouth.UnlockDoor()` → void | перенесена |
| `15_prison_south.hps:756` | `PrisonSouth.TimerDoorUnlock(string asTimer)` → void | перенесена |
| `15_prison_south.hps:785` | `PrisonSouth.EntityCallFack(string asEntity, string type)` → void | перенесена |
| `15_prison_south.hps:795` | `PrisonSouth.CollideFlashBackGirlMother(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:800` | `PrisonSouth.TimerFlashBackGirlMother(string asTimer)` → void | перенесена |
| `15_prison_south.hps:804` | `PrisonSouth.TimerBeginGirlCry(string asTimer)` → void | перенесена |
| `15_prison_south.hps:813` | `PrisonSouth.InteractAreaCloth(string asEntity)` → void | перенесена |
| `15_prison_south.hps:826` | `PrisonSouth.TimerClothOn(string asTimer)` → void | перенесена |
| `15_prison_south.hps:833` | `PrisonSouth.CollideFlashBackMother(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:837` | `PrisonSouth.TimerRosePetals(string asTimer)` → void | перенесена |
| `15_prison_south.hps:854` | `PrisonSouth.CollideFlashBackJourney(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:858` | `PrisonSouth.TimerJourneyRosePetals(string asTimer)` → void | перенесена |
| `15_prison_south.hps:867` | `PrisonSouth.LitScareLantern(string asEntity, string type)` → void | перенесена |
| `15_prison_south.hps:871` | `PrisonSouth.EntityCallPickItem(string asEntity, string type)` → void | перенесена |
| `15_prison_south.hps:885` | `PrisonSouth.CollideAreaBreeze(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:896` | `PrisonSouth.TimerEBreeze(string asTimer)` → void | перенесена |
| `15_prison_south.hps:967` | `PrisonSouth.TimerManCry(string asTimer)` → void | перенесена |
| `15_prison_south.hps:978` | `PrisonSouth.CollideAreaStairGrunt(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:990` | `PrisonSouth.EntityCallStairGrunt(string asEntity, string type)` → void | перенесена |
| `15_prison_south.hps:998` | `PrisonSouth.CPCallGruntStairs(string asName, int alCount)` → void | перенесена |
| `15_prison_south.hps:1014` | `PrisonSouth.TimerRemoveRats(string asTimer)` → void | перенесена |
| `15_prison_south.hps:1026` | `PrisonSouth.CollideAreaDoLastGrunt(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_south.hps:1033` | `PrisonSouth.CPCallGruntCell(string asName, int alCount)` → void | перенесена |
| `15_prison_south.hps:1052` | `PrisonSouth.TimerGruntCell(string asTimer)` → void | перенесена |
| `15_prison_south.hps:1081` | `PrisonSouth.OnStart()` → void | перенесена |
| `15_prison_south.hps:1191` | `PrisonSouth.OnEnter()` → void | перенесена |
| `15_prison_south.hps:1208` | `PrisonSouth.OnLeave()` → void | перенесена |

Итого: 61 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: глобальных полей нет.

Массивов нет.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `15_prison_south.hps:581`: Циклы OnStart начинаются с 0: AreaFlick_0 и AreaHammer_0 отсутствуют в .map; сохранено исходное начало циклов.
- `15_prison_south.hps:654`: TimerCPEvent запрашивает sreact_scare.snt с лишней s. Звук не найден, остальные действия таймера сохранены.
- `15_prison_south.hps:185`: CollideExitArea выполняет ChangeMap в north. Проверяется в настоящем Play отдельно от изолированных call-сценариев.

## API движка

Проверены 65 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | NotPorted | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs:119` |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave остаётся заглушкой. Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/15_prison_south.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_15_prison_south.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 23. Полный отчёт: `Logs/Ch02_Import_15_prison_south.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют PrisonSouth.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 3 to attach to body 'Body_1'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 5 to attach to body 'Body_2'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 12 to attach to body 'Body_3'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 13 to attach to body 'Body_4'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 14 to attach to body 'Body_5'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 15 to attach to body 'Body_6'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 7 to attach to body 'Body_7'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 8 to attach to body 'Body_8'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 9 to attach to body 'Body_9'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 10 to attach to body 'Body_10'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 2 to attach to body 'Body_11'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 22 to attach to body 'Body_12'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 17 to attach to body 'Body_13'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 18 to attach to body 'Body_14'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 23 to attach to body 'Body_15'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 24 to attach to body 'Body_16'
- 1× entities/organic/corpse_male/corpse_male.ent: could not find child with unique ID 19 to attach to body 'Body_17'
- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/15_prison_south.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 15_prison_south -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/PrisonSouth.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_15_prison_south.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/15_prison_south.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Вступление прошло до восстановления управления. HammerTime=4, DoorOpenExtra=1, prison_3 разблокирована; три настоящие смерти завершились возрождением на grunt_stairs / checkpoint1 / grunt_cell. Переход в north выполняется отдельным Play-маршрутом.

### Sim_15_prison_south_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_15_prison_south_start.log` / `.txt`. Player simulation: 15_prison_south — 2026-10-16 23:42:13 — 3 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (1.25, 0.90, 6.65) roll 0,0 pitch 1,3 yaw -86,4 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] Entity 'AreaFlick_0' does not exist!
- 1× [Script] Entity 'AreaHammer_0' does not exist!
- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_15_prison_south_puzzle

ExitCode=0. Отчёты: `Logs/Ch02_Sim_15_prison_south_puzzle.log` / `.txt`. Player simulation: 15_prison_south — 2026-10-16 23:46:18 — 3 warnings, 0 errors.

```text
watch 30 2; call RemoveEntityCollideCallback bed_simple_movable_1 BedOverHole; call CollideMoveBed bed_simple_movable_1 BedOverHole -1; watch 10 2; call CollideAreaHammer Player AreaHammer_1 1; watch 10 2; call InteractHole prison_tunnel_entry_1; watch 10 2; call GiveItemFromFile stone_hammer_1 stone_hammer.ent; call GiveItemFromFile stone_chipper_1 stone_chipper.ent; call UseHCH stone_hammer_1 prison_tunnel_entry_1; watch 10 2; call InteractLockedDoor prison_3; watch 10 2; call UseHCExtra stone_hammer_chipper prison_3; watch 10 2; body prison_3; call GetLocalVarInt HammerTime; call GetLocalVarInt DoorOpenExtra; shot hole; health
```

Фактическое состояние:

```text
t= 90,15   call call GetLocalVarInt HammerTime → 4
t= 90,17   call call GetLocalVarInt DoorOpenExtra → 1
t= 90,19   health 100,00 sanity 100,00 dead False pos (3.64, 0.80, 6.50) roll 0,0 pitch -1,6 yaw -78,4 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] Entity 'AreaFlick_0' does not exist!
- 1× [Script] Entity 'AreaHammer_0' does not exist!
- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_15_prison_south_events_cp

ExitCode=0. Отчёты: `Logs/Ch02_Sim_15_prison_south_events_cp.log` / `.txt`. Player simulation: 15_prison_south — 2026-10-16 23:46:49 — 4 warnings, 0 errors.

```text
watch 30 2; call InteractBarDoor prison_section_static_1; watch 10 2; call CollideAreaDrag Player AreaDrag 1; watch 20 2; call CollideAreaDrag02 Player AreaDrag_2 1; watch 10 2; call CollideAreaRumble Player AreaRumble 1; watch 10 2; call CollideFlashBackMother Player FlashbackArea_2 1; watch 10 2; call SetEnemyDisableTriggers servant_grunt_1 true; call CollideAreaStairGrunt Player AreaStairGrunt 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call SetLocalVarInt servant_grunt_1 0; call CollideAreaGrunt Player AreaCorridorGrunt_1 1; call CollideAreaGrunt Player AreaCorridorGrunt_2 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call SetEnemyDisableTriggers servant_grunt_2 true; call SetEnemyDisableTriggers super_brute true; call CollideAreaDoLastGrunt Player AreaDoLastGrunt 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call GetGlobalVarInt 15CPEvent; shot checkpoint
```

Фактическое состояние:

```text
t=112,43 [Script debug] Loading check point grunt_stairs Count: 0
t=120,14   health 52,00 sanity 40,02 dead False pos (-17.75, 0.60, 5.50) roll 0,0 pitch 41,5 yaw 38,8 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point grunt_stairs at ScriptArea_6 count 1
t=142,49 [Script debug] Loading check point checkpoint1 Count: 0
t=150,21   health 52,00 sanity 33,34 dead False pos (-24.40, 0.60, 36.83) roll 0,0 pitch -17,3 yaw -124,8 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point checkpoint1 at PlayerStartArea_6 count 1
t=172,55 [Script debug] Loading check point grunt_cell Count: 0
t=180,27   health 52,00 sanity 39,04 dead False pos (-69.50, 4.60, 33.00) roll 0,0 pitch -12,9 yaw 61,7 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point grunt_cell at PlayerStartArea_10 count 1
t=180,27   call call GetGlobalVarInt 15CPEvent → 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] Entity 'AreaFlick_0' does not exist!
- 1× [Script] Entity 'AreaHammer_0' does not exist!
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Cannot find sound entity 'sreact_scare.snt'

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_15_prison_south.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=104, submeshes=195, geometryFailures=0, prefabMatrixDifferences=8, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=1,99478
Scene: instances=899, submeshMatrices=1088, sharedMeshColliders=710, failures=29, maxMatrixError=7,6294E-06
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 25, другое имя — у 4, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
Точные имена в сообщениях missing соответствуют SkinnedMeshRenderer; аудитор проверяет только MeshFilter. Численное сравнение вершин этих скелетных мешей этим аудитом не выполнено.
Для servant_grunt присутствует SkinnedMeshRenderer servant_grunt 1 с mesh GUID efd1d42486a630a40bee10a3c8b05d7e и fileID -4556356290143606882 (тот же исходный меш), плюс отдельные волосы. Это конкретная проверка имени/ресурса, а не предположение об отсутствии геометрии.

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
