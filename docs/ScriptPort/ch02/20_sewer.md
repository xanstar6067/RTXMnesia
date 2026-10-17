# 20_sewer

Оригинал: `source/maps/main/ch02/20_sewer.hps` (1253 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Sewer.cs`.
Класс: `RTXMnesia.Levels.Ch02.Sewer`, атрибут: `HplMapScript("maps/main/ch02/20_sewer.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/20_sewer.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `20_sewer.hps:16` | `Sewer.GetCogComboIndex()` → int | перенесена |
| `20_sewer.hps:24` | `Sewer.GetCogComboString()` → string | перенесена |
| `20_sewer.hps:32` | `Sewer.GetCogPlaySoundWaitSteps()` → int | перенесена |
| `20_sewer.hps:39` | `Sewer.GetCogRotateSpeed()` → float | перенесена |
| `20_sewer.hps:56` | `Sewer.GetBoneModel()` → string | перенесена |
| `20_sewer.hps:63` | `Sewer.DropBone(string asArea, float fTime)` → void | перенесена |
| `20_sewer.hps:68` | `Sewer.TimerCreateBone(string asTimer)` → void | перенесена |
| `20_sewer.hps:78` | `Sewer.TimerRemoveBone(string asTimer)` → void | перенесена |
| `20_sewer.hps:94` | `Sewer.InteractWithPipePiece(string asEntity)` → void | перенесена |
| `20_sewer.hps:104` | `Sewer.CollidePipePieceWithBreakArea(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:139` | `Sewer.CogPanelChange(string asEntity, int alState)` → void | перенесена |
| `20_sewer.hps:192` | `Sewer.InteractWheelMachine(string asEntity)` → void | перенесена |
| `20_sewer.hps:206` | `Sewer.UpdateWaterWheelRotateSpeed()` → void | перенесена |
| `20_sewer.hps:250` | `Sewer.TimerUpdateCogMachinerySounds(string asTimer)` → void | перенесена |
| `20_sewer.hps:255` | `Sewer.UpdateCogMachinerySounds(float afRotateSpeed)` → void | перенесена |
| `20_sewer.hps:287` | `Sewer.CollideWaterWheelBladeWithWaterIn(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:320` | `Sewer.CollideWaterWheelBladeWithWaterOut(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:339` | `Sewer.CollidePlayerWithWaterWheel(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:361` | `Sewer.UsePipeOnWaterWheelCallback(string asItem, string asEntity)` → void | перенесена |
| `20_sewer.hps:397` | `Sewer.TimerPipePosTargetedCallback(string asTimer)` → void | перенесена |
| `20_sewer.hps:406` | `Sewer.PipePosClear(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:422` | `Sewer.CollideWheelWithPipe(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:453` | `Sewer.CollidePlayerWithSplashHeardArea(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:467` | `Sewer.CollidePlayerWithBruteArea1(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:479` | `Sewer.TimerEBrute01(string asTimer)` → void | перенесена |
| `20_sewer.hps:537` | `Sewer.TimerPlayerReact(string asTimer)` → void | перенесена |
| `20_sewer.hps:556` | `Sewer.CollidePlayerWithBruteAreaHall3(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:583` | `Sewer.CheckPoint01(string asName, int alCount)` → void | перенесена |
| `20_sewer.hps:609` | `Sewer.InteractHall3CheckPoint01Door(string asEntity)` → void | перенесена |
| `20_sewer.hps:627` | `Sewer.CollideGruntCorpse(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:641` | `Sewer.TimerAlexanderVoice01(string asTimer)` → void | перенесена |
| `20_sewer.hps:658` | `Sewer.CollideBruteHall4Encount(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:689` | `Sewer.TimerBarsBrokenQuest(string asTimer)` → void | перенесена |
| `20_sewer.hps:697` | `Sewer.CheckPoint02(string asName, int alCount)` → void | перенесена |
| `20_sewer.hps:722` | `Sewer.OnBrute05AutoDisabled(string asEntity, string asType)` → void | перенесена |
| `20_sewer.hps:735` | `Sewer.CollideBruteEncountHall5(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:748` | `Sewer.CheckPoint03(string asName, int alCount)` → void | перенесена |
| `20_sewer.hps:777` | `Sewer.CollideBruteInsidePatrolArea(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:791` | `Sewer.CollideBruteEncountHall6(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:824` | `Sewer.TimerShowBrute7Player(string asTimer)` → void | перенесена |
| `20_sewer.hps:832` | `Sewer.CheckPoint04(string asName, int alCount)` → void | перенесена |
| `20_sewer.hps:846` | `Sewer.CollideScaryWaterLurkerEvent(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:869` | `Sewer.TimerLurkerSplash(string asTimer)` → void | перенесена |
| `20_sewer.hps:895` | `Sewer.CollideExitMap(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:908` | `Sewer.InteractMetalBars(string asEntity)` → void | перенесена |
| `20_sewer.hps:917` | `Sewer.TimerMetalBars(string asTimer)` → void | перенесена |
| `20_sewer.hps:923` | `Sewer.CollideAreaBarQuest(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:937` | `Sewer.InteractWaterWheel(string asEntity)` → void | перенесена |
| `20_sewer.hps:942` | `Sewer.TimerWaterWheel(string asTimer)` → void | перенесена |
| `20_sewer.hps:957` | `Sewer.CollideAreaExitEvent(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:987` | `Sewer.CollideExitAreaDisableBrute(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:1006` | `Sewer.CollideExitAreaEnableBrute(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:1026` | `Sewer.CollideAreaBreath(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:1038` | `Sewer.TimerCaveBreath(string asTimer)` → void | перенесена |
| `20_sewer.hps:1049` | `Sewer.CollideAreaCaveEvent(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:1063` | `Sewer.TimerCaveSplah(string asTimer)` → void | перенесена |
| `20_sewer.hps:1076` | `Sewer.LookAtBoneDropper(string asEntity, int alState)` → void | перенесена |
| `20_sewer.hps:1098` | `Sewer.CollideAreaGateMonster(string asParent, string asChild, int alState)` → void | перенесена |
| `20_sewer.hps:1106` | `Sewer.TimerStartGateMonster(string asTimer)` → void | перенесена |
| `20_sewer.hps:1115` | `Sewer.TimerRestoreGateMonster(string asTimer)` → void | перенесена |
| `20_sewer.hps:1125` | `Sewer.OnStart()` → void | перенесена |
| `20_sewer.hps:1235` | `Sewer.OnEnter()` → void | перенесена |
| `20_sewer.hps:1248` | `Sewer.OnLeave()` → void | перенесена |

Итого: 63 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: `bool gbForceWaterWheelSlowSpeed`, начальное значение `false` (`20_sewer.hps:10`); `int glBoneCount`, начальное значение `0` (`20_sewer.hps:53`); `string[] gvBoneEnts`, начальное значение `{ "human_skull.ent", "human_bone.ent" }` (`20_sewer.hps:54`)

Массивы (порядок, длина, повторы и значения сохранены):

- `vComboStrings` (string, `20_sewer.hps:26`), значения: "lowest", "low", "med", "high", "highest".
- `vWaitSteps` (int, `20_sewer.hps:34`), значения: 1, 1, 1, 1, 0.
- `gvBoneEnts` (string, `20_sewer.hps:54`), значения: "human_skull.ent", "human_bone.ent".
- `vRotateSpeeds` (float, `20_sewer.hps:1144`), значения: 1.5708f, 3.1416f, 4.7124f, 5.1832f, 5.354f,.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `20_sewer.hps:139`: Панели water_wheel_cog_panel_1/2 — EntityType=MultiSlider; с 2026-10-17 это `PropMultiSlider` (порт `cLuxProp_MultiSlider`): три состояния по диапазону слайдера ±0.425, прилипание в 0.15 м, автодвижение к текущему состоянию, `ChangeStateCallback` = `CogPanelChange(панель, состояние)`. Проверено `FeatureTests` (CLAUDE.md «Мульти-слайдеры»).
- `20_sewer.hps:16`: Сохранены пять комбинаций строк/задержек и девять допустимых сочетаний двух панелей (состояния 0..2). Отладочный gbForceWaterWheelSlowSpeed сохранён полем.
- `20_sewer.hps:56`: Случайный выбор gvBoneEnts остаётся RandInt(0,1); счётчик созданных костей glBoneCount остаётся полем.
- `20_sewer.hps:577`: Проверяются настоящие смерти на четырёх CP, включая ветви исходных alCount=0/1/2. Отображаемый счётчик health увеличивается уже после вызова callback.
- `20_sewer.hps:895`: CollideExitMap вызывает переход в Nave и проверяется в настоящем Play; из одиночных сценариев исключён.

## API движка

Проверены 60 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | ✓ (2026-10-17, сохранения перенесены (CLAUDE.md «Сохранения»)) | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs` |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave с 2026-10-17 пишет автосохранение (CLAUDE.md «Сохранения»). Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/20_sewer.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_20_sewer.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 18. Полный отчёт: `Logs/Ch02_Import_20_sewer.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют Sewer.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× Decal 'blood_pool01_1' has no or broken geometry, skipped
- 1× 'FogArea' is not supported yet (skipped)
- 1× entities/organic/grunt_body_part/grunt_body_part2.ent: could not find child with unique ID 6 to attach to body 'Body_1'
- 1× entities/organic/grunt_body_part/grunt_body_part2.ent: could not find child with unique ID 7 to attach to body 'Body_1'
- 1× entities/organic/grunt_body_part/grunt_body_part2.ent: could not find child with unique ID 8 to attach to body 'Body_1'
- 1× entities/organic/grunt_body_part/grunt_body_part2.ent: could not find child with unique ID 9 to attach to body 'Body_2'
- 2× entities/organic/grunt_body_part/grunt_body_part1.ent: could not find child with unique ID 5 to attach to body 'Body_1'
- 2× entities/organic/grunt_body_part/grunt_body_part1.ent: could not find child with unique ID 6 to attach to body 'Body_2'
- 2× entities/organic/grunt_body_part/grunt_body_part1.ent: could not find child with unique ID 7 to attach to body 'Body_3'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3'
- 1× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4'
- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/20_sewer.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 20_sewer -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/Sewer.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_20_sewer.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/20_sewer.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: WaterWheelBlocked=1. Проверены все девять допустимых сочетаний панелей, пять индексов/скоростей/звуковых вариантов и четыре CP с настоящими смертями (ветви callback alCount=0/1/2). После выдержки у лестниц зарегистрированы LadderArea_1 state=0→1, подъём ног до Y=4.446 и спуск до Y=0.002; LadderArea_2 state=0→1 и подъём с Y=2.054 до 16.454, health=100. Полный выход наверху второй лестницы не проверен. Первый fixture нажимал interact до достижения дистанции 2.3 м и в Ladder не входил; он сохранён как неуспешная диагностика. Панели MultiSlider физически ещё не работают.

### Sim_20_sewer_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_20_sewer_start.log` / `.txt`. Player simulation: 20_sewer — 2026-10-16 23:44:13 — 1 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (0.00, 8.98, 23.75) roll 0,0 pitch 0,0 yaw 180,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_20_sewer_wheel

ExitCode=0. Отчёты: `Logs/Ch02_Sim_20_sewer_wheel.log` / `.txt`. Player simulation: 20_sewer — 2026-10-16 23:52:09 — 3 warnings, 0 errors.

```text
watch 30 2; call InteractWithPipePiece pipe_piece_static; watch 10 2; call CollidePipePieceWithBreakArea pipe_piece_static PipeBreakArea 1; watch 10 2; call CollidePipePieceWithBreakArea pipe_piece_static PipeBreakArea 1; watch 10 2; call CollidePipePieceWithBreakArea pipe_piece_static PipeBreakArea 1; watch 10 2; call GiveItem pipe_piece Puzzle PipePiece pipe_piece.tga 1; active pipe_piece false; call CogPanelChange water_wheel_cog_panel_1 2; watch 10 2; call CogPanelChange water_wheel_cog_panel_2 0; watch 10 2; call UsePipeOnWaterWheelCallback pipe_piece WheelInteractArea; watch 10 2; call CogPanelChange water_wheel_cog_panel_1 0; watch 10 2; call CogPanelChange water_wheel_cog_panel_2 2; watch 10 2; call UsePipeOnWaterWheelCallback pipe_piece WheelInteractArea; watch 10 2; call RemoveEntityCollideCallback water_wheel_1 PipeBlockPosArea; call PipePosClear water_wheel_1 PipeBlockPosArea -1; watch 10 2; call RemoveEntityCollideCallback water_wheel_1 PipeHitArea; call CollideWheelWithPipe water_wheel_1 PipeHitArea 1; watch 10 2; call GetLocalVarInt WaterWheelBlocked; body WaterWheelBlocker; shot wheel
```

Фактическое состояние:

```text
t=150,27   call call GetLocalVarInt WaterWheelBlocked → 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Could not find particle system 'PS_WaterWheelStream'
- 1× [Script] ERROR: Could not find sound entity 'CogMachinery'

### Sim_20_sewer_brutes_cp

ExitCode=0. Отчёты: `Logs/Ch02_Sim_20_sewer_brutes_cp.log` / `.txt`. Player simulation: 20_sewer — 2026-10-16 23:53:24 — 1 warnings, 0 errors.

```text
watch 30 2; call SetLocalVarInt CogsInRightPlace 1; call CollidePlayerWithBruteAreaHall3 Player BruteHall3EventArea 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call CollideBruteHall4Encount Player AreaBruteHall4Encount 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call CollideBruteEncountHall5 Player AreaBruteEncountHall5 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call CollideBruteEncountHall6 Player AreaBruteEncountHall6 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call RemoveTimer ShowBrute7Player; enemy; health; shot checkpoints
```

Фактическое состояние:

```text
t= 52,33 [Script debug] Loading check point check01 Count: 0
t= 60,05   health 52,00 sanity 95,00 dead False pos (39.87, 2.90, -35.52) roll 0,0 pitch 0,0 yaw -162,3 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_20_Sewer_01 | check point check01 at PlayerStartArea_10 count 1
t= 72,35 [Script debug] Loading check point check01 Count: 1
t= 80,07   health 52,00 sanity 95,00 dead False pos (39.87, 2.90, -35.52) roll 0,0 pitch 0,0 yaw -162,3 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_20_Sewer_01 | check point check01 at PlayerStartArea_10 count 2
t= 92,37 [Script debug] Loading check point check01 Count: 2
t=100,09   health 52,00 sanity 95,00 dead False pos (39.87, 2.90, -35.52) roll 0,0 pitch 0,0 yaw -162,3 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_20_Sewer_01 | check point check01 at PlayerStartArea_10 count 3
t=122,41 [Script debug] Loading check point check02 Count: 0
t=130,13   health 52,00 sanity 90,00 dead False pos (-33.12, 4.90, -36.54) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_20_Sewer_02 | check point check02 at PlayerStartArea_12 count 1
t=142,43 [Script debug] Loading check point check02 Count: 1
t=150,14   health 52,00 sanity 90,00 dead False pos (-33.12, 4.90, -36.54) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_20_Sewer_02 | check point check02 at PlayerStartArea_12 count 2
t=172,45 [Script debug] Loading check point check03 Count: 0
t=180,17   health 52,00 sanity 90,00 dead False pos (-0.10, 0.90, -46.50) roll 0,0 pitch 0,0 yaw -178,1 | hurt 0,00 noise 0 move× 0,80 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_20_Sewer_03 | check point check03 at PlayerStartArea_4 count 1
t=192,46 [Script debug] Loading check point check03 Count: 1
t=200,18   health 52,00 sanity 90,00 dead False pos (-0.10, 0.90, -46.50) roll 0,0 pitch 0,0 yaw -178,1 | hurt 0,00 noise 0 move× 0,80 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_20_Sewer_03 | check point check03 at PlayerStartArea_4 count 2
t=212,48 [Script debug] Loading check point check03 Count: 2
t=220,19   health 52,00 sanity 90,00 dead False pos (-0.10, 0.90, -46.50) roll 0,0 pitch 0,0 yaw -178,1 | hurt 0,00 noise 0 move× 0,80 head 0,000 | splashes 0 flash - | death - hint Hints/DeathBrute_20_Sewer_03 | check point check03 at PlayerStartArea_4 count 3
t=242,00 [Script debug] Loading check point check04 Count: 0
t=250,22   health 52,00 sanity 83,20 dead False pos (-0.10, 0.90, -46.50) roll 0,0 pitch 0,0 yaw -178,1 | hurt 0,00 noise 0 move× 0,80 head 0,000 | splashes 0 flash - | death - hint Hints/DeathGuardian_25_CellTunnels | check point check04 at PlayerStartArea_4 count 1
t=250,23   health 52,00 sanity 83,20 dead False pos (-0.10, 0.90, -46.50) roll 0,0 pitch 0,0 yaw -178,1 | hurt 0,00 noise 0 move× 0,80 head 0,000 | splashes 0 flash - | death - hint Hints/DeathGuardian_25_CellTunnels | check point check04 at PlayerStartArea_4 count 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_20_sewer_combos_ladders

ExitCode=0. Отчёты: `Logs/Ch02_Sim_20_sewer_combos_ladders.log` / `.txt`. Player simulation: 20_sewer — 2026-10-16 23:59:32 — 3 warnings, 0 errors.

```text
watch 30 2; call CogPanelChange water_wheel_cog_panel_1 0; call CogPanelChange water_wheel_cog_panel_2 0; watch 1 1; call GetCogComboIndex; call GetCogComboString; call GetCogPlaySoundWaitSteps; call GetCogRotateSpeed; call CogPanelChange water_wheel_cog_panel_1 0; call CogPanelChange water_wheel_cog_panel_2 1; watch 1 1; call GetCogComboIndex; call GetCogComboString; call GetCogPlaySoundWaitSteps; call GetCogRotateSpeed; call CogPanelChange water_wheel_cog_panel_1 0; call CogPanelChange water_wheel_cog_panel_2 2; watch 1 1; call GetCogComboIndex; call GetCogComboString; call GetCogPlaySoundWaitSteps; call GetCogRotateSpeed; call CogPanelChange water_wheel_cog_panel_1 1; call CogPanelChange water_wheel_cog_panel_2 0; watch 1 1; call GetCogComboIndex; call GetCogComboString; call GetCogPlaySoundWaitSteps; call GetCogRotateSpeed; call CogPanelChange water_wheel_cog_panel_1 1; call CogPanelChange water_wheel_cog_panel_2 1; watch 1 1; call GetCogComboIndex; call GetCogComboString; call GetCogPlaySoundWaitSteps; call GetCogRotateSpeed; call CogPanelChange water_wheel_cog_panel_1 1; call CogPanelChange water_wheel_cog_panel_2 2; watch 1 1; call GetCogComboIndex; call GetCogComboString; call GetCogPlaySoundWaitSteps; call GetCogRotateSpeed; call CogPanelChange water_wheel_cog_panel_1 2; call CogPanelChange water_wheel_cog_panel_2 0; watch 1 1; call GetCogComboIndex; call GetCogComboString; call GetCogPlaySoundWaitSteps; call GetCogRotateSpeed; call CogPanelChange water_wheel_cog_panel_1 2; call CogPanelChange water_wheel_cog_panel_2 1; watch 1 1; call GetCogComboIndex; call GetCogComboString; call GetCogPlaySoundWaitSteps; call GetCogRotateSpeed; call CogPanelChange water_wheel_cog_panel_1 2; call CogPanelChange water_wheel_cog_panel_2 2; watch 1 1; call GetCogComboIndex; call GetCogComboString; call GetCogPlaySoundWaitSteps; call GetCogRotateSpeed; call RemoveEntityCollideCallback Player AreaExitMap; start PlayerStartArea_2; w 0.7; interact; watch 2 1; w 8; shot ladder1up; s 8; shot ladder1down; release; start PlayerStartArea_6; turn 180; w 0.4; interact; watch 2 1; w 20; shot ladder2; health
```

Фактическое состояние:

```text
t= 31,03   call call GetCogComboIndex → 2
t= 31,05   call call GetCogComboString → med
t= 31,07   call call GetCogPlaySoundWaitSteps → 1
t= 31,08   call call GetCogRotateSpeed → 4,7124
t= 32,13   call call GetCogComboIndex → 1
t= 32,15   call call GetCogComboString → low
t= 32,17   call call GetCogPlaySoundWaitSteps → 1
t= 32,18   call call GetCogRotateSpeed → 3,1416
t= 33,23   call call GetCogComboIndex → 0
t= 33,25   call call GetCogComboString → lowest
t= 33,27   call call GetCogPlaySoundWaitSteps → 1
t= 33,28   call call GetCogRotateSpeed → 1,5708
t= 34,33   call call GetCogComboIndex → 3
t= 34,35   call call GetCogComboString → high
t= 34,37   call call GetCogPlaySoundWaitSteps → 1
t= 34,38   call call GetCogRotateSpeed → 5,1832
t= 35,43   call call GetCogComboIndex → 2
t= 35,45   call call GetCogComboString → med
t= 35,47   call call GetCogPlaySoundWaitSteps → 1
t= 35,48   call call GetCogRotateSpeed → 4,7124
t= 36,53   call call GetCogComboIndex → 1
t= 36,55   call call GetCogComboString → low
t= 36,57   call call GetCogPlaySoundWaitSteps → 1
t= 36,58   call call GetCogRotateSpeed → 3,1416
t= 37,63   call call GetCogComboIndex → 4
t= 37,65   call call GetCogComboString → highest
t= 37,67   call call GetCogPlaySoundWaitSteps → 0
t= 37,68   call call GetCogRotateSpeed → 5,354
t= 38,73   call call GetCogComboIndex → 3
t= 38,75   call call GetCogComboString → high
t= 38,77   call call GetCogPlaySoundWaitSteps → 1
t= 38,78   call call GetCogRotateSpeed → 5,1832
t= 39,83   call call GetCogComboIndex → 2
t= 39,85   call call GetCogComboString → med
t= 39,87   call call GetCogPlaySoundWaitSteps → 1
t= 39,88   call call GetCogRotateSpeed → 4,7124
t= 81,12   health 100,00 sanity 90,00 dead False pos (-6.19, 2.90, -102.10) roll 0,0 pitch 0,0 yaw 180,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Could not find particle system 'PS_WaterWheelStream'
- 1× [Script] ERROR: Could not find sound entity 'CogMachinery'

### Sim_20_sewer_ladders_prepared

ExitCode=0. Отчёты: `Logs/Ch02_Sim_20_sewer_ladders_prepared.log` / `.txt`. Player simulation: 20_sewer — 2026-10-17 00:06:20 — 1 warnings, 0 errors.

```text
watch 30 2; call RemoveEntityCollideCallback Player AreaExitMap; start PlayerStartArea_2; w 0.9; watch 1 1; interact; watch 2 1; w 4; shot ladder1climb; s 6; shot ladder1down; release; start PlayerStartArea_6; turn 180; w 0.65; watch 1 1; interact; watch 2 1; w 18; shot ladder2; health
```

Фактическое состояние:

```text
t= 65,67   health 100,00 sanity 100,00 dead False pos (-6.25, 17.35, -101.52) roll 0,0 pitch 0,0 yaw 180,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] AutoSave: skipped, needs saving (step 6)

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_20_sewer.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=124, submeshes=257, geometryFailures=0, prefabMatrixDifferences=5, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=0,332018
Scene: instances=1877, submeshMatrices=4204, sharedMeshColliders=3056, failures=19, maxMatrixError=77,5
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 17, другое имя — у 0, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
Точные имена в сообщениях missing соответствуют SkinnedMeshRenderer; аудитор проверяет только MeshFilter. Численное сравнение вершин этих скелетных мешей этим аудитом не выполнено.

Повторяющиеся имена: ceiling_default_46. Независимо сопоставлены 2 экземпляров по исходным ID/FileIndex, положениям, поворотам, масштабам и GUID префабов статики (сущности — по hplId и пути .ent). Неразрешённых сопоставлений: 0. `Logs/Ch02_DuplicateReview.json`. Сообщения PLACEMENT у этих имён вызваны выбором первого одноимённого объекта аудитором; импортёр и аудитор не изменены.

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
