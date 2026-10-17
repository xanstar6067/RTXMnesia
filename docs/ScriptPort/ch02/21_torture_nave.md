# 21_torture_nave

Оригинал: `source/maps/main/ch02/21_torture_nave.hps` (1689 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureNave.cs`.
Класс: `RTXMnesia.Levels.Ch02.TortureNave`, атрибут: `HplMapScript("maps/main/ch02/21_torture_nave.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/21_torture_nave.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `21_torture_nave.hps:9` | `TortureNave.CollideWakeUpAgrippa(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:36` | `TortureNave.TimerAgrippaScareStopLookAt(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:41` | `TortureNave.TimerAgrippaScareOver(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:56` | `TortureNave.AgrippaStrainedSpeak(int alNum)` → void | перенесена |
| `21_torture_nave.hps:87` | `TortureNave.AgrippaStrainedSpeakStartRandom()` → void | перенесена |
| `21_torture_nave.hps:92` | `TortureNave.TimerAgrippaRandomStrainedSpeak(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:107` | `TortureNave.StopAgrippaStrainedSpeak()` → void | перенесена |
| `21_torture_nave.hps:128` | `TortureNave.ChangeChannelingLever(string asEntityName, int alState)` → void | перенесена |
| `21_torture_nave.hps:155` | `TortureNave.TimerAgrippaNormalTalkStart(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:169` | `TortureNave.AgrippaTopicNotSpoken(string asTopic)` → bool | перенесена |
| `21_torture_nave.hps:175` | `TortureNave.SetAgrippaTopicHasSpoken(string asTopic)` → void | перенесена |
| `21_torture_nave.hps:182` | `TortureNave.PlayAgrippaNormalVoice(string asTopic, int alNumOfParts, bool abGreeting, bool abSetAsSpoken)` → void | перенесена |
| `21_torture_nave.hps:218` | `TortureNave.AgrippaNormalSpeak(bool abGreeting)` → void | перенесена |
| `21_torture_nave.hps:378` | `TortureNave.AgrippaNormalTalkOver()` → void | перенесена |
| `21_torture_nave.hps:385` | `TortureNave.CheckAgrippaNormalTalkOverEvent()` → void | перенесена |
| `21_torture_nave.hps:426` | `TortureNave.TimerNewAgrippaTopic(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:444` | `TortureNave.StopAgrippaNormalSpeak()` → void | перенесена |
| `21_torture_nave.hps:472` | `TortureNave.CollideAgrippaRandomArea(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:490` | `TortureNave.CollideAgrippaStartTalk(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:519` | `TortureNave.CollideAgrippaStopTalk(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:552` | `TortureNave.PlayerInteractLabDoor(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:571` | `TortureNave.InitCeilingCogWheels()` → void | перенесена |
| `21_torture_nave.hps:578` | `TortureNave.CeilingCogWheelInteracted(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:589` | `TortureNave.TimerCogWheelAttached(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:618` | `TortureNave.InteractSafetyDoor(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:636` | `TortureNave.TimerCloseGate(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:647` | `TortureNave.TimerSafetyDoorQuest(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:654` | `TortureNave.PullSafetyDoorLever(string asEntity, int alState)` → void | перенесена |
| `21_torture_nave.hps:719` | `TortureNave.TimerStopSounds(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:735` | `TortureNave.CollideRopeInteraction(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:746` | `TortureNave.CollideRopeDown(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:796` | `TortureNave.InteractStuckWellLever(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:801` | `TortureNave.TimerLurkerEat(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:810` | `TortureNave.TimerLurkerEatRopeImpulseOnly(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:816` | `TortureNave.TimerStopLookAtWheel(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:821` | `TortureNave.TimerLurkerDoneEating(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:837` | `TortureNave.InteractSlimeBone(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:858` | `TortureNave.UseMeatOnRope(string asItem, string asEntity)` → void | перенесена |
| `21_torture_nave.hps:881` | `TortureNave.CollideCrankSound(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:893` | `TortureNave.InitWaterLurkerSounds()` → void | перенесена |
| `21_torture_nave.hps:908` | `TortureNave.CollideJunkInWell(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:914` | `TortureNave.TimerRemoveJunkInWell(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:926` | `TortureNave.StartWaterLurkerAmbience()` → void | перенесена |
| `21_torture_nave.hps:931` | `TortureNave.StopWaterLurkerAmbience()` → void | перенесена |
| `21_torture_nave.hps:938` | `TortureNave.TimerWaterLurkerAmbience(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:956` | `TortureNave.CollideWellKillPlayer(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:979` | `TortureNave.TimerFallDeath(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1014` | `TortureNave.CheckPointFallWell(string asName, int alCount)` → void | перенесена |
| `21_torture_nave.hps:1027` | `TortureNave.CollideStartAlexanderVoice(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:1034` | `TortureNave.TimerAlexVoiceRumble(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1044` | `TortureNave.TimerTimerAlexVoiceRumbleFadeOut(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1057` | `TortureNave.PickupChannelWeyerNote(string asEntity, string asType)` → void | перенесена |
| `21_torture_nave.hps:1076` | `TortureNave.TimerAllOrbsHowlEvent(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1091` | `TortureNave.TimerAllOrbsHowlEventStop(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1105` | `TortureNave.CollideFirstWellKillPlayer(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:1120` | `TortureNave.TimerFirstWellFallDeath(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1142` | `TortureNave.CheckPointFallFirstWell(string asName, int alCount)` → void | перенесена |
| `21_torture_nave.hps:1150` | `TortureNave.TimerFallFirstWellDeath(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1184` | `TortureNave.InteractCellarWood0102(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:1212` | `TortureNave.InteractJammedDoor(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:1218` | `TortureNave.TimerJammedDoor(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1231` | `TortureNave.InteractFirstWellHole(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:1237` | `TortureNave.TimerFirstWellHole(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1248` | `TortureNave.CollideAreaChains(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:1255` | `TortureNave.InteractGirlCellDoor(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:1265` | `TortureNave.EntityCallDiary(string asEntity, string type)` → void | перенесена |
| `21_torture_nave.hps:1271` | `TortureNave.UseStuffOnAgrippa(string asItem, string asEntity)` → void | перенесена |
| `21_torture_nave.hps:1297` | `TortureNave.PlayerReact(bool bDamage, float fTime)` → void | перенесена |
| `21_torture_nave.hps:1308` | `TortureNave.TimerPlayerReactions(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1327` | `TortureNave.CollideAreaSpikes(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:1343` | `TortureNave.InteractVisionDoor(string asEntity)` → void | перенесена |
| `21_torture_nave.hps:1349` | `TortureNave.TimerRandSound(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1355` | `TortureNave.TimerRandSound02(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1369` | `TortureNave.CollideAreaCellEvent(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:1385` | `TortureNave.CollideFlashbackArea_23(string asParent, string asChild, int alState)` → void | перенесена |
| `21_torture_nave.hps:1392` | `TortureNave.WaitRoomOver()` → void | перенесена |
| `21_torture_nave.hps:1400` | `TortureNave.TimerAllowCellEvent(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1408` | `TortureNave.TimerCellEvent(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1511` | `TortureNave.BangOnCellDoor()` → void | перенесена |
| `21_torture_nave.hps:1536` | `TortureNave.TimerEventCellLight(string asTimer)` → void | перенесена |
| `21_torture_nave.hps:1544` | `TortureNave.OnStart()` → void | перенесена |
| `21_torture_nave.hps:1645` | `TortureNave.OnEnter()` → void | перенесена |
| `21_torture_nave.hps:1686` | `TortureNave.OnLeave()` → void | перенесена |

Итого: 83 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: `float gfCogWheelSpeed`, начальное значение `2.0` (`21_torture_nave.hps:568`)

Массивы (порядок, длина, повторы и значения сохранены):

- `vRandomPartNum` (int, `21_torture_nave.hps:347`), значения: 2, 1, 3, 2, 3, 2, 2, 1, 2, 1, 1, 1.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `21_torture_nave.hps:347`: При исходном глобальном AgrippaRandomChatterNum=0 AngelScript прерывает чтение индекса -1. Добавлен выход в этой точке без увеличения счётчика и без искусственного выбора первой темы.
- `21_torture_nave.hps`: Исходные local/global переменные не объединялись. Кость повышает глобальный IngredientCount; у хора яд повышает локальный счётчик.
- `21_torture_nave.hps:632`: Сохранены разные имена квестов 20SafetyDoorClosed/21SafetyDoorClosed и исходные ключи переводов 20LeverNotWorking.
- `21_torture_nave.hps:779`: AreaWellLookAt отсутствует в исходной .map; соответствующая попытка взгляда оставлена.
- `21_torture_nave.hps:19`: SetNPCAwake (пробуждение Агриппы с анимацией Awake, затем Idle, голова следит за игроком в AreaAgrippaStartTalk) и прикреплённые пропы (мясо на грузе верёвки `invisible_box_mass_2_1`) работают с 2026-10-17 (CLAUDE.md «Титры, NPC, анимации, прикреплённые пропы и критеры»). Sticky и связь колеса с верёвкой работают с 2026-10-17 (CLAUDE.md «Верёвки, связи, липкие зоны, слизь»); проход с косточкой на верёвке здесь не прогонялся.
- `source/maps/main/ch02/21_torture_nave.map:4572`: candle_floor_4 ссылается на отсутствующий PointLight_40. Исходная ссылка сохранена.

## API движка

Проверены 70 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AddAttachedPropToProp` | ✓ с 2026-10-17 | CLAUDE.md «Титры, NPC, анимации, прикреплённые пропы и критеры» |
| `AttachBodyToStickyArea` | ✓ с 2026-10-17: CLAUDE.md «Верёвки, связи, липкие зоны, слизь» |
| `AutoSave` | ✓ (2026-10-17, сохранения перенесены (CLAUDE.md «Сохранения»)) | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs` |
| `InteractConnectPropWithRope` | ✓ с 2026-10-17: CLAUDE.md «Верёвки, связи, липкие зоны, слизь» |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |
| `RemoveAttachedPropFromProp` | ✓ с 2026-10-17 | CLAUDE.md «Титры, NPC, анимации, прикреплённые пропы и критеры» |
| `SetNPCAwake` | ✓ с 2026-10-17 | CLAUDE.md «Титры, NPC, анимации, прикреплённые пропы и критеры» |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave с 2026-10-17 пишет автосохранение (CLAUDE.md «Сохранения»). Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/21_torture_nave.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_21_torture_nave.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 17. Полный отчёт: `Logs/Ch02_Import_21_torture_nave.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют TortureNave.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× Decal 'hay_small_28' has no or broken geometry, skipped
- 1× Decal 'hay_small_33' has no or broken geometry, skipped
- 2× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1'
- 2× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2'
- 2× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3'
- 2× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4'
- 1× 'FogArea' is not supported yet (skipped)
- 1× entities/item/fresh_meat/fresh_meat_onrope.ent: sub mesh 'polySurface3' not found in the model
- 1× entities/item/fresh_meat/fresh_meat_onrope.ent: sub mesh 'polySurface4' not found in the model
- 1× entities/item/fresh_meat/fresh_meat_onrope.ent: could not find child with unique ID 7 to attach to body 'Body_1'
- 1× entities/item/fresh_meat/fresh_meat_onrope.ent: could not find child with unique ID 9 to attach to body 'Body_1'
- 1× Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
- 1× No level script port for maps/main/ch02/21_torture_nave.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 21_torture_nave -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/TortureNave.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_21_torture_nave.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/21_torture_nave.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Проверены Introduction, TonicQuest, WeyerNote, ChoirDone, TranseptDone, AllOrbsNotIngred, AllOrbs, SafetyDoors, PulledLever, LabLocked, Random01/Random12; fixture подавляет прежние голоса и таймер между темами. Random12 оставляет глобальный индекс 11, индекс 0 прерывается. У подготовленных рычагов SafetyDoorCount=2, SafetyDoorsOpen=1, AgrippaActivatedIn21=1. Кость дала глобальный IngredientCount=1; обе смерти в колодцах завершились возрождением. AlexanderRumbleLoop не создаётся этой исходной цепочкой, попытка StopSound сохранена.

### Sim_21_torture_nave_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_21_torture_nave_start.log` / `.txt`. Player simulation: 21_torture_nave — 2026-10-16 23:44:35 — 3 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 100,00 dead False pos (-22.95, 20.90, 85.71) roll 0,0 pitch 0,0 yaw -143,9 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Light 'PointLight_40' that is connected to lamp 'candle_floor_4' does not exist!

### Sim_21_torture_nave_agrippa_machine

ExitCode=0. Отчёты: `Logs/Ch02_Sim_21_torture_nave_agrippa_machine.log` / `.txt`. Player simulation: 21_torture_nave — 2026-10-16 23:53:54 — 6 warnings, 0 errors.

```text
watch 30 2; call CollideWakeUpAgrippa Player AreaWakeUpAgrippa 1; watch 10 2; call StopAgrippaStrainedSpeak; call ChangeChannelingLever channeling_machine_lever_1 1; watch 10 2; call StopAgrippaNormalSpeak; call PickupChannelWeyerNote note_1 OnPickup; watch 10 2; call PullSafetyDoorLever lever_simple01_1 -1; watch 10 2; call CeilingCogWheelInteracted ceiling_machinery_cog_interact_1; watch 10 2; call PullSafetyDoorLever lever_simple01_1 -1; watch 10 2; call PullSafetyDoorLever lever_simple01_2 -1; watch 10 2; call CollideStartAlexanderVoice Player AreaStartAlexanderVoice 1; watch 10 2; call GetLocalVarInt SafetyDoorsOpen; call GetGlobalVarInt AgrippaActivatedIn21; shot agrippa
```

Фактическое состояние:

```text
t=110,18   call call GetLocalVarInt SafetyDoorsOpen → 1
t=110,19   call call GetGlobalVarInt AgrippaActivatedIn21 → 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Light 'PointLight_40' that is connected to lamp 'candle_floor_4' does not exist!
- 1× [Script] SetNPCAwake: skipped, needs NPCs and the man pig (step 6)
- 1× [Script] AttachBodyToStickyArea: skipped, needs sticky areas
- 1× [Script] ERROR: Could not find sound entity 'AlexanderRumbleLoop'

### Sim_21_torture_nave_well_ingredients

ExitCode=0. Отчёты: `Logs/Ch02_Sim_21_torture_nave_well_ingredients.log` / `.txt`. Player simulation: 21_torture_nave — 2026-10-16 23:54:26 — 5 warnings, 0 errors.

```text
watch 30 2; call GiveItemFromFile fresh_meat_2 fresh_meat.ent; call UseMeatOnRope fresh_meat_2 AreaRopeInteraction; watch 10 2; call CollideRopeInteraction invisible_box_mass_2_1 AreaRopeInteraction 1; watch 10 2; call UseMeatOnRope fresh_meat_2 AreaRopeInteraction; watch 10 2; call CollideRopeDown invisible_box_mass_2_1 AreaRopeDown 1; watch 15 2; call InteractSlimeBone invisible_box_mass_2_1; watch 10 2; call CollideWellKillPlayer Player AreaWellKillPlayer 1; watch 20 2; health; call CollideFirstWellKillPlayer Player AreaFirstWellKillPlayer 1; watch 20 2; health; call GetGlobalVarInt IngredientCount; shot wells
```

Фактическое состояние:

```text
t= 98,41 [Script debug] Loading check point checkFallWell Count: 0
t=105,12   health 52,00 sanity 89,59 dead False pos (-15.78, 4.90, 53.88) roll 0,0 pitch 0,0 yaw -86,6 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathFall_21_TortureNave | check point checkFallWell at PlayerStartArea_5 count 1
t=118,43 [Script debug] Loading check point checkFallFirstWell Count: 0
t=125,15   health 52,00 sanity 88,11 dead False pos (-36.81, 20.91, 83.38) roll 0,0 pitch 0,0 yaw -91,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint Hints/DeathFall_21_TortureNave | check point checkFallFirstWell at PlayerStartArea_6 count 1
t=125,15   call call GetGlobalVarInt IngredientCount → 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Light 'PointLight_40' that is connected to lamp 'candle_floor_4' does not exist!
- 1× [Script] AddAttachedPropToProp: skipped, needs attached props
- 1× [Script] RemoveAttachedPropFromProp: skipped, needs attached props

### Sim_21_torture_nave_topics_return

ExitCode=0. Отчёты: `Logs/Ch02_Sim_21_torture_nave_topics_return.log` / `.txt`. Player simulation: 21_torture_nave — 2026-10-16 23:54:53 — 3 warnings, 0 errors.

```text
watch 30 2; call SetLocalVarInt SpokenVar_Introduction 1; call SetLocalVarInt SpokenVar_TonicQuest 1; call SetLocalVarInt PickedWeyerNote 1; call SetLocalVarInt SpokenVar_WeyerNote 1; call SetLocalVarInt SpokenVar_ChoirDone 1; call SetLocalVarInt SpokenVar_TranseptDone 1; call SetLocalVarInt SpokenVar_SafetyDoors 1; call SetLocalVarInt SpokenVar_PulledLever 1; call SetLocalVarInt SpokenVar_LabLocked 1; call AgrippaNormalSpeak false; watch 10 2; call SetGlobalVarInt AgrippaRandomChatterNum 1; call AgrippaNormalSpeak false; watch 10 2; call StopAgrippaNormalSpeak; call SetGlobalVarInt ChoirOrbCount 3; call SetGlobalVarInt TranseptOrbCount 3; call SetGlobalVarInt PlayerHasVisitedLevel22 1; call OnEnter; watch 10 2; sanity 60; call CollideAreaCellEvent Player AreaCellEvent 1; watch 25 2; call GetLocalVarInt AllOrbsHowlEventActivated; shot return
```

Фактическое состояние:

```text
t= 85,30   call call GetLocalVarInt AllOrbsHowlEventActivated → 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Light 'PointLight_40' that is connected to lamp 'candle_floor_4' does not exist!

### Sim_21_torture_nave_machine_prepared

ExitCode=0. Отчёты: `Logs/Ch02_Sim_21_torture_nave_machine_prepared.log` / `.txt`. Player simulation: 21_torture_nave — 2026-10-17 00:01:12 — 6 warnings, 0 errors.

```text
watch 30 2; call CollideWakeUpAgrippa Player AreaWakeUpAgrippa 1; watch 10 2; call StopAgrippaStrainedSpeak; call SetLeverStuckState channeling_machine_lever_1 1 true; watch 10 2; call StopAgrippaNormalSpeak; call PickupChannelWeyerNote note_1 OnPickup; watch 10 2; call PullSafetyDoorLever lever_simple01_1 -1; watch 10 2; call CeilingCogWheelInteracted ceiling_machinery_cog_interact_1; watch 10 2; call SetLeverStuckState lever_simple01_1 -1 true; watch 10 2; call SetLeverStuckState lever_simple01_2 -1 true; watch 10 2; call CollideStartAlexanderVoice Player AreaStartAlexanderVoice 1; watch 10 2; call GetLocalVarInt SafetyDoorsOpen; call GetGlobalVarInt AgrippaActivatedIn21; shot agrippa; call GetLocalVarInt SafetyDoorCount
```

Фактическое состояние:

```text
t=110,18   call call GetLocalVarInt SafetyDoorsOpen → 1
t=110,19   call call GetGlobalVarInt AgrippaActivatedIn21 → 1
t=110,21   call call GetLocalVarInt SafetyDoorCount → 2
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Light 'PointLight_40' that is connected to lamp 'candle_floor_4' does not exist!
- 1× [Script] SetNPCAwake: skipped, needs NPCs and the man pig (step 6)
- 1× [Script] AttachBodyToStickyArea: skipped, needs sticky areas
- 1× [Script] ERROR: Could not find sound entity 'AlexanderRumbleLoop'

### Sim_21_torture_nave_all_topics

ExitCode=0. Отчёты: `Logs/Ch02_Sim_21_torture_nave_all_topics.log` / `.txt`. Player simulation: 21_torture_nave — 2026-10-17 00:01:48 — 3 warnings, 0 errors.

```text
watch 30 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call PickupChannelWeyerNote note_1 OnPickup; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetGlobalVarInt ChoirOrbCount 3; call SetGlobalVarInt TranseptOrbCount 0; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetGlobalVarInt ChoirOrbCount 0; call SetGlobalVarInt TranseptOrbCount 3; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetGlobalVarInt ChoirOrbCount 3; call SetGlobalVarInt IngredientCount 0; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetLocalVarInt SpokenVar_AllOrbsNotIngred 1; call SetGlobalVarInt IngredientCount 3; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetLocalVarInt SpokenVar_AllOrbs 1; call SetGlobalVarInt ChoirOrbCount 0; call SetGlobalVarInt TranseptOrbCount 0; call SetLocalVarInt SafetyDoorsOpen 1; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetLocalVarInt SafetyDoorsOpen 0; call SetLocalVarInt InteractedDoorLever 1; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetLocalVarInt InteractedLabDoor 1; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetGlobalVarInt AgrippaRandomChatterNum 0; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetGlobalVarInt AgrippaRandomChatterNum 1; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call SetGlobalVarInt AgrippaRandomChatterNum 12; call AgrippaNormalSpeak false; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer NewAgrippaTopic; call GetGlobalVarInt AgrippaRandomChatterNum; shot topics
```

Фактическое состояние:

```text
t=161,24   call call GetGlobalVarInt AgrippaRandomChatterNum → 11
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] InteractConnectPropWithRope: skipped, needs interact connections
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Light 'PointLight_40' that is connected to lamp 'candle_floor_4' does not exist!

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_21_torture_nave.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=196, submeshes=395, geometryFailures=0, prefabMatrixDifferences=9, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=1
Scene: instances=1839, submeshMatrices=2408, sharedMeshColliders=1768, failures=25, maxMatrixError=44,5216
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 23, другое имя — у 0, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
Точные имена в сообщениях missing соответствуют SkinnedMeshRenderer; аудитор проверяет только MeshFilter. Численное сравнение вершин этих скелетных мешей этим аудитом не выполнено.

Повторяющиеся имена: rock_stone_med01_brown_2. Независимо сопоставлены 2 экземпляров по исходным ID/FileIndex, положениям, поворотам, масштабам и GUID префабов статики (сущности — по hplId и пути .ent). Неразрешённых сопоставлений: 0. `Logs/Ch02_DuplicateReview.json`. Сообщения PLACEMENT у этих имён вызваны выбором первого одноимённого объекта аудитором; импортёр и аудитор не изменены.

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
