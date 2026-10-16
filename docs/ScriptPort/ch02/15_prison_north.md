# 15_prison_north

Оригинал: `source/maps/main/ch02/15_prison_north.hps` (1309 строк, Latin-1).
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/PrisonNorth.cs`.
Класс: `RTXMnesia.Levels.Ch02.PrisonNorth`, атрибут: `HplMapScript("maps/main/ch02/15_prison_north.map")`.
Сцена: `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch02/15_prison_north.unity` (вне git).
Дата партии: 2026-10-16–17.
Статус: готов — весь скрипт перенесён, компиляция/сверка и прогоны выполнены; ограничения движка и геометрии ниже.

## Функции

| Оригинал (строка) | Порт / сигнатура | Статус |
|---|---|---|
| `15_prison_north.hps:5` | `PrisonNorth.CollideAlexanderArea(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:12` | `PrisonNorth.TimerAlexVoice(string asTimer)` → void | перенесена |
| `15_prison_north.hps:34` | `PrisonNorth.AlexanderMental(int iVoice)` → void | перенесена |
| `15_prison_north.hps:41` | `PrisonNorth.CollideNoAlex(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:50` | `PrisonNorth.VisionOverAlexOn()` → void | перенесена |
| `15_prison_north.hps:62` | `PrisonNorth.TimerShakeShackles(string asTimer)` → void | перенесена |
| `15_prison_north.hps:70` | `PrisonNorth.InteractExtraDoor(string asEntity)` → void | перенесена |
| `15_prison_north.hps:74` | `PrisonNorth.TimerMessDoor(string asTimer)` → void | перенесена |
| `15_prison_north.hps:79` | `PrisonNorth.UseItemOnExtraDoor(string asItem, string asEntity)` → void | перенесена |
| `15_prison_north.hps:83` | `PrisonNorth.TimerItemMessDoor(string asTimer)` → void | перенесена |
| `15_prison_north.hps:95` | `PrisonNorth.InteractAlwaysLocked(string asEntity)` → void | перенесена |
| `15_prison_north.hps:102` | `PrisonNorth.CollideAreaHelpMe(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:108` | `PrisonNorth.TimerEee(string asTimer)` → void | перенесена |
| `15_prison_north.hps:122` | `PrisonNorth.CollideAcidDamage(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:126` | `PrisonNorth.InteractTouchAcid(string asEntity)` → void | перенесена |
| `15_prison_north.hps:135` | `PrisonNorth.TimerAcidDamageMess(string asTimer)` → void | перенесена |
| `15_prison_north.hps:152` | `PrisonNorth.UseBottle(string asItem, string asEntity)` → void | перенесена |
| `15_prison_north.hps:174` | `PrisonNorth.CollideAcidWater(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:192` | `PrisonNorth.TimerVanishOA(string asTimer)` → void | перенесена |
| `15_prison_north.hps:228` | `PrisonNorth.InteractExitGate(string asEntity)` → void | перенесена |
| `15_prison_north.hps:232` | `PrisonNorth.TimerExitGate(string asTimer)` → void | перенесена |
| `15_prison_north.hps:251` | `PrisonNorth.UseItemOnExit(string asItem, string asEntity)` → void | перенесена |
| `15_prison_north.hps:285` | `PrisonNorth.TearLock()` → void | перенесена |
| `15_prison_north.hps:302` | `PrisonNorth.TimerEndlight(string asTimer)` → void | перенесена |
| `15_prison_north.hps:309` | `PrisonNorth.EntityCallBreakLock(string asEntity, string type)` → void | перенесена |
| `15_prison_north.hps:329` | `PrisonNorth.TimerActiveHammer(string asTimer)` → void | перенесена |
| `15_prison_north.hps:340` | `PrisonNorth.TimerRemoveHammer(string asTimer)` → void | перенесена |
| `15_prison_north.hps:344` | `PrisonNorth.TimerStartGrunt(string asTimer)` → void | перенесена |
| `15_prison_north.hps:356` | `PrisonNorth.EntityCallPickGlass(string asEntity, string type)` → void | перенесена |
| `15_prison_north.hps:360` | `PrisonNorth.CollideKitchenGrunt(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:373` | `PrisonNorth.CollideAreaGrunt1Exit(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:377` | `PrisonNorth.EntityCallEnemyKitchen(string asEntity, string type)` → void | перенесена |
| `15_prison_north.hps:385` | `PrisonNorth.TimerEnemyKitchenDoor(string asTimer)` → void | перенесена |
| `15_prison_north.hps:392` | `PrisonNorth.CollideCorridorBrute(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:415` | `PrisonNorth.TimerNoticeGrunt(string asTimer)` → void | перенесена |
| `15_prison_north.hps:445` | `PrisonNorth.TimerPlayerSpeed(string asTimer)` → void | перенесена |
| `15_prison_north.hps:501` | `PrisonNorth.CollideCorridorServant(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:524` | `PrisonNorth.EntityCallCorridorServant(string asEntity, string type)` → void | перенесена |
| `15_prison_north.hps:530` | `PrisonNorth.CPCallWayBack(string asName, int alCount)` → void | перенесена |
| `15_prison_north.hps:540` | `PrisonNorth.EntityCallPickDiary(string asEntity, string type)` → void | перенесена |
| `15_prison_north.hps:548` | `PrisonNorth.CollideAreaNoteGrunt(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:554` | `PrisonNorth.CPCallGrunt4(string asName, int alCount)` → void | перенесена |
| `15_prison_north.hps:559` | `PrisonNorth.TimerCP4Rat(string asTimer)` → void | перенесена |
| `15_prison_north.hps:571` | `PrisonNorth.CPCall01(string asName, int alCount)` → void | перенесена |
| `15_prison_north.hps:593` | `PrisonNorth.TimerSplat(string asTimer)` → void | перенесена |
| `15_prison_north.hps:601` | `PrisonNorth.TimerLoopSlime(string asTimer)` → void | перенесена |
| `15_prison_north.hps:626` | `PrisonNorth.CollideAreaRemoveSlime(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:653` | `PrisonNorth.TimerSlimeEnd(string asTimer)` → void | перенесена |
| `15_prison_north.hps:662` | `PrisonNorth.CPCall02(string asName, int alCount)` → void | перенесена |
| `15_prison_north.hps:710` | `PrisonNorth.TimerCPEvent(string asTimer)` → void | перенесена |
| `15_prison_north.hps:748` | `PrisonNorth.TimerLightOff(string asTimer)` → void | перенесена |
| `15_prison_north.hps:755` | `PrisonNorth.LookAtPrisonDoor(string asEntity, int alState)` → void | перенесена |
| `15_prison_north.hps:762` | `PrisonNorth.TimerCP02Event(string asTimer)` → void | перенесена |
| `15_prison_north.hps:796` | `PrisonNorth.TimerLightOff02(string asTimer)` → void | перенесена |
| `15_prison_north.hps:803` | `PrisonNorth.CPCall03(string asName, int alCount)` → void | перенесена |
| `15_prison_north.hps:830` | `PrisonNorth.CollideAreaCP03(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:847` | `PrisonNorth.TimerTunnelDust(string asTimer)` → void | перенесена |
| `15_prison_north.hps:858` | `PrisonNorth.TimerStopTunnelDust(string asTimer)` → void | перенесена |
| `15_prison_north.hps:865` | `PrisonNorth.CollideAreaRemoveDust(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:871` | `PrisonNorth.LookAtIntro(string asTimer)` → void | перенесена |
| `15_prison_north.hps:880` | `PrisonNorth.CollideAreaFire(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:885` | `PrisonNorth.InteractFire(string asEntity)` → void | перенесена |
| `15_prison_north.hps:889` | `PrisonNorth.TimerInteractFire(string asTimer)` → void | перенесена |
| `15_prison_north.hps:893` | `PrisonNorth.TimerBurnFire(string asTimer)` → void | перенесена |
| `15_prison_north.hps:911` | `PrisonNorth.CollideAreaBodyFallActivate(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:916` | `PrisonNorth.TimerBangHead(string asTimer)` → void | перенесена |
| `15_prison_north.hps:925` | `PrisonNorth.EntityCallPrisonGate(string asEntity, string type)` → void | перенесена |
| `15_prison_north.hps:934` | `PrisonNorth.InteractGruntDoor(string asEntity)` → void | перенесена |
| `15_prison_north.hps:949` | `PrisonNorth.TimerPlayerreact(string asTimer)` → void | перенесена |
| `15_prison_north.hps:959` | `PrisonNorth.EntityCallGruntDoor(string asEntity, string type)` → void | перенесена |
| `15_prison_north.hps:965` | `PrisonNorth.CPCallGruntDoor(string asName, int alCount)` → void | перенесена |
| `15_prison_north.hps:972` | `PrisonNorth.TimerCPGruntDoor(string asTimer)` → void | перенесена |
| `15_prison_north.hps:980` | `PrisonNorth.CollideAreaDarkSteps(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:991` | `PrisonNorth.TimerDarkSteps(string asTimer)` → void | перенесена |
| `15_prison_north.hps:1031` | `PrisonNorth.CollideAreaACStart(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:1036` | `PrisonNorth.CollideAreaAroundCorner(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:1055` | `PrisonNorth.EntityCallEnemyRemoved(string asEntity, string type)` → void | перенесена |
| `15_prison_north.hps:1061` | `PrisonNorth.CPCallStartGrunt(string asName, int alCount)` → void | перенесена |
| `15_prison_north.hps:1067` | `PrisonNorth.CollideAreaCPStart(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:1082` | `PrisonNorth.TimerCPStart(string asTimer)` → void | перенесена |
| `15_prison_north.hps:1090` | `PrisonNorth.CollideAreaLongCorridorGrunt(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:1096` | `PrisonNorth.CPCallLongCorridor(string asName, int alCount)` → void | перенесена |
| `15_prison_north.hps:1106` | `PrisonNorth.CollideAreaLongCP(string asParent, string asChild, int alState)` → void | перенесена |
| `15_prison_north.hps:1115` | `PrisonNorth.InteractVisionDoor(string asEntity)` → void | перенесена |
| `15_prison_north.hps:1122` | `PrisonNorth.OnStart()` → void | перенесена |
| `15_prison_north.hps:1290` | `PrisonNorth.OnEnter()` → void | перенесена |
| `15_prison_north.hps:1304` | `PrisonNorth.OnLeave()` → void | перенесена |

Итого: 87 исходных функций, включая перегрузки.
## Данные и соответствие исходнику

Поля: глобальных полей нет.

Массивов нет.

Объявлений const и enum в исходнике нет. Поля не заменялись local/global API-переменными. Все switch/if, границы циклов, динамические callbacks, порядок операций и английские комментарии сохранены.

Синтаксис: string &in/string & → string; существующие lifecycle-функции → public override; статические имена существующих функций → nameof; десятичные литералы → float; or/and/not → C#-операторы. Пустые и отсутствующие callbacks сохранены.

Независимая сверка: `UnityProj/UMnesiaRTX/Logs/Ch02_TokenReview.txt` (нормализует только перечисленные синтаксические адаптации). Компиляция Roslyn: `Logs/Ch02_Compile.txt`; Unity также компилирует порты перед прогонами. Предупреждения CS0162 у PrisonNorth и TortureChoirWest вызваны исходным недостижимым кодом.

## Отличия и сохранённые особенности

- `15_prison_north.hps:1139`: В OnStart регистрируется object_19, которого нет в .map; динамического создания в скрипте и ContainedItem с таким именем нет.
- `15_prison_north.hps:110`: TimerPlayerreact строит react_breathl вместо существующего react_breath; строка и обе ветки сохранены.
- `15_prison_north.hps:597`: TimerSplat увеличивает локальную Splat, а имя звука строит с глобальной Splat; не исправлено.
- `15_prison_north.hps:251`: Уничтожение padlock_acid_1 штатно вызывает EntityCallBreakLock и удаляет проп. Предупреждение диагностической body-команды после разрушения не означает пропажу исходного ассета.
- `15_prison_north.hps:1185`: Повторы имён use-item callbacks и исходные имена hce/hcp сохранены; callback-реестр не переписан.

## API движка

Проверены 66 имён API и актуальные сигнатуры: `UnityProj/UMnesiaRTX/Logs/Ch02_API_Inventory.md`. Отсутствующих объявлений нет; компиляция проверяет типы и перегрузки. `docs/ScriptPort/api.md` не изменялся — движок в этой партии не реализовывался.

| Ограниченный API | Фактический статус | Ссылка |
|---|---|---|
| `AutoSave` | ✓ (2026-10-17, сохранения перенесены (CLAUDE.md «Сохранения»)) | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.General.cs` |
| `PreloadSound` | empty | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs:75` |
| `SetInsanitySetEnabled` | ✓ (события безумия перенесены (CLAUDE.md «События безумия»); 2026-10-17) | `UnityProj\UMnesiaRTX\Assets\Scripts\ScriptApi\HplScript.Effects.cs` |

Grunt/brute и WaterLurker уже имеют ИИ в актуальном движке; скриптовая активация сама по себе не является проверкой всех состояний ИИ. AutoSave с 2026-10-17 пишет автосохранение (CLAUDE.md «Сохранения»). `SetInsanitySetEnabled` с 2026-10-17 работает: включает набор `Ch02` (бормотание Даниэля главы 2) и выключает `Ch01`; строки `skipped` ниже — из прогонов до этого. Пустой PreloadSound не проверяет ресурсы и не загружает их заранее.

## Импорт

Unity 6000.6.2f1; запуск последовательно, только без Temp/UnityLockfile. Импортированные папки и Logs проверены через git check-ignore. Исходники и установленная игра не изменялись.

```text
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch02/15_prison_north.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_Import_15_prison_north.log
```

ExitCode=0; ошибок 0. Всего предупреждений по сохранённым секциям: 44. Полный отчёт: `Logs/Ch02_Import_15_prison_north.txt`. Предупреждение No level script port yet относится к импорту до добавления C#-класса; последующие прогоны используют PrisonNorth.

Типы/тексты предупреждений (повторы сгруппированы):

- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 2 to attach to body 'body_hip'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 1 to attach to body 'body_base02'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 12 to attach to body 'body_leg_front_left01'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 9 to attach to body 'body_leg_front_right01'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 14 to attach to body 'body_leg_front_left02'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 13 to attach to body 'body_leg_front_left02'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 10 to attach to body 'body_leg_front_right02'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 11 to attach to body 'body_leg_front_right02'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 6 to attach to body 'body_leg_back_left01'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 4 to attach to body 'body_leg_back_right02'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 5 to attach to body 'body_leg_back_right02'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 3 to attach to body 'body_leg_back_right01'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 15 to attach to body 'Body_neck'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 16 to attach to body 'Body_neck'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 7 to attach to body 'body_leg_back_left02'
- 1× entities/organic/pig_corpse/pig_corpse.ent: could not find child with unique ID 8 to attach to body 'body_leg_back_left02'
- 1× 'FogArea' is not supported yet (skipped)
- 2× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1'
- 2× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2'
- 2× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3'
- 2× entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4'
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
- 1× No level script port for maps/main/ch02/15_prison_north.map yet (the level runs without its events)

## Сверка и сценарии

`powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch02 -Map 15_prison_north -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch02/PrisonNorth.cs` — 0 ошибок, 0 предупреждений; отчёт `Logs/Ch02_Check_15_prison_north.txt`.

Все команды ниже — диагностические fixtures, seed=1. Выдача предмета, вызов pickup-handler, установка переменной и ручной Sticky callback перечислены явно и не выдаются за прохождение через взаимодействие игрока. Изменённые/снятые callback-регистрации относятся только к сцене теста и не сохраняются.

Общая команда для каждого сценария (без -nographics):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch02/15_prison_north.map -seed 1 -script "<точная строка ниже>" -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Ch02_<имя сценария>.log
```

Фактический результат и предел проверки: Кислота из пустой бутылки, замена замка и его разрушение проверены: AcidOnGate=1, prison_section_2 locked=False. Проверены кухонный, коридорный и другие CP, обе ветви глобального 15CPEvent; health после возрождения положительное, dead=False. No prop padlock_acid_1 относится к диагностике уже разрушенного замка. Остановки loops/loops2 после завершения записи — не отсутствие звукового ресурса.

### Sim_15_prison_north_start

ExitCode=0. Отчёты: `Logs/Ch02_Sim_15_prison_north_start.log` / `.txt`. Player simulation: 15_prison_north — 2026-10-16 23:42:33 — 3 warnings, 0 errors.

```text
watch 30 2; shot start; w 1; watch 2 1; shot walk; health
```

Фактическое состояние:

```text
t= 33,00   health 100,00 sanity 99,96 dead False pos (-61.31, 0.12, 49.45) roll 0,0 pitch 46,8 yaw 3,7 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] Entity 'object_19' with type any and subtype any does not exist!
- 1× [Script] SetInsanitySetEnabled: skipped, needs insanity events (step 6)
- 1× [Script] AutoSave: skipped, needs saving (step 6)

### Sim_15_prison_north_acid_gate

ExitCode=0. Отчёты: `Logs/Ch02_Sim_15_prison_north_acid_gate.log` / `.txt`. Player simulation: 15_prison_north — 2026-10-16 23:47:19 — 4 warnings, 0 errors.

```text
watch 30 2; call GiveItemFromFile glass_container_1 glass_container.ent; call UseBottle handdrill acid_container_1; watch 10 2; call InteractTouchAcid AcidDamage; watch 10 2; call UseBottle glass_container_1 acid_container_1; watch 10 2; call InteractExitGate padlock_rusty_1; watch 10 2; call UseItemOnExit glass_container_filled padlock_rusty_1; watch 10 2; call GiveItem stone_hammer_chipper Puzzle stone_hammer_chipper stone_hammer_chipper.tga 0; call UseItemOnExit stone_hammer_chipper padlock_acid_1; watch 10 2; body prison_section_2; body padlock_acid_1; call GetLocalVarInt AcidOnGate; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call CollideAreaCP03 Player AreaCP03 1; watch 10 2; shot gate
```

Фактическое состояние:

```text
t= 90,14   call call GetLocalVarInt AcidOnGate → 1
t=102,46 [Script debug] Loading check point checkpoint3 Count: 0
t=110,18   health 52,00 sanity 99,46 dead False pos (-3.75, 4.60, 118.50) roll 0,0 pitch 0,0 yaw 150,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point checkpoint3 at PlayerStartArea_12 count 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] Entity 'object_19' with type any and subtype any does not exist!
- 1× [Script] SetInsanitySetEnabled: skipped, needs insanity events (step 6)
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× WARNING: No prop 'body padlock_acid_1'

### Sim_15_prison_north_voices_cp

ExitCode=0. Отчёты: `Logs/Ch02_Sim_15_prison_north_voices_cp.log` / `.txt`. Player simulation: 15_prison_north — 2026-10-16 23:48:14 — 6 warnings, 0 errors.

```text
watch 30 2; call CollideAlexanderArea Player AlexanderArea 1; watch 10 2; call TimerAlexVoice alex; watch 10 2; call CollideNoAlex Player FlashbackArea_1 1; watch 10 2; call VisionOverAlexOn; watch 10 2; call GiveItemFromFile glass_container_1 glass_container.ent; call EntityCallPickGlass glass_container_1 OnPickup; call SetEnemyDisableTriggers servant_grunt_1 true; call CollideKitchenGrunt Player AreaKitchenGrunt 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call CollideAreaRemoveSlime Player AreaRemoveSlime 1; watch 10 2; call SetEnemyDisableTriggers servant_brute_1 true; call SetEnemyDisableTriggers servant_grunt_3 true; call CollideCorridorBrute Player AreaCorridorBrute 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call SetGlobalVarInt 15CPEvent 1; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call LookAtPrisonDoor prison_8 1; watch 10 2; call SetEnemyDisableTriggers servant_grunt_2 true; call CollideCorridorServant Player AreaCorridorServant_2 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call SetEnemyDisableTriggers servant_grunt_4 true; call CollideAreaNoteGrunt Player AreaNoteGrunt 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call SetEnemyDisableTriggers servant_grunt_5 true; call SetLocalVarInt GruntDoor 0; call InteractGruntDoor prison_8; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call SetEnemyDisableTriggers servant_grunt_10 true; call SetLocalVarInt GruntDoor 0; call CollideAreaAroundCorner Player AreaAroundCorner_1 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; call SetEnemyDisableTriggers servant_grunt_11 true; call CollideAreaLongCorridorGrunt Player AreaLongCorridorGrunt 1; watch 10 2; call GivePlayerDamage 200 Claws true true; watch 20 2; health; health; shot checkpoints
```

Фактическое состояние:

```text
t= 92,44 [Script debug] Loading check point checkpoint1 Count: 0
t=100,16   health 52,00 sanity 99,87 dead False pos (-66.74, 0.28, 62.73) roll 0,0 pitch 0,0 yaw 94,2 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point checkpoint1 at PlayerStartArea_8 count 1
t=132,53 [Script debug] Loading check point checkpoint2 Count: 0
t=140,24   health 52,00 sanity 67,77 dead False pos (-23.55, 4.79, 119.37) roll 0,0 pitch -5,3 yaw -162,9 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point checkpoint2 at PlayerStartArea_9 count 1
t=152,56 [Script debug] Loading check point checkpoint2 Count: 1
t=160,27   health 52,00 sanity 67,77 dead False pos (-22.75, 5.23, 112.00) roll 0,0 pitch -58,9 yaw 155,2 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point checkpoint2 at PlayerStartArea_9 count 2
t=192,61 [Script debug] Loading check point cp_wayback Count: 0
t=200,33   health 52,00 sanity 57,19 dead False pos (-1.69, 4.90, 114.86) roll 0,0 pitch 0,0 yaw -169,7 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point cp_wayback at PlayerStartArea_6 count 1
t=222,66 [Script debug] Loading check point grunt_4 Count: 0
t=230,37   health 52,00 sanity 55,24 dead False pos (-75.56, 4.90, 104.40) roll 0,0 pitch 0,0 yaw -168,8 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point grunt_4 at PlayerStartArea_14 count 1
t=252,72 [Script debug] Loading check point cp_gruntdoor Count: 0
t=260,43   health 52,00 sanity 46,23 dead False pos (-23.55, 4.79, 119.37) roll 0,0 pitch 0,0 yaw -150,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point cp_gruntdoor at PlayerStartArea_9 count 1
t=282,78 [Script debug] Loading check point cp_start Count: 0
t=290,49   health 52,00 sanity 40,03 dead False pos (-47.31, 0.60, 54.61) roll 0,0 pitch 0,0 yaw 0,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point cp_start at PlayerStartArea_13 count 1
t=312,82 [Script debug] Loading check point cp_corridor Count: 0
t=320,53   health 52,00 sanity 39,04 dead False pos (-3.75, 4.60, 118.50) roll 0,0 pitch 0,0 yaw 150,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point cp_corridor at PlayerStartArea_12 count 1
t=320,53   health 52,00 sanity 39,04 dead False pos (-3.75, 4.60, 118.50) roll 0,0 pitch 0,0 yaw 150,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point cp_corridor at PlayerStartArea_12 count 1
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] Entity 'object_19' with type any and subtype any does not exist!
- 1× [Script] SetInsanitySetEnabled: skipped, needs insanity events (step 6)
- 1× [Script] AutoSave: skipped, needs saving (step 6)
- 1× [Script] ERROR: Could not find sound entity 'loops'
- 1× [Script] ERROR: Could not find sound entity 'loops2'
- 1× [Script] ERROR: Cannot find sound entity 'react_breathl'

### Sim_15_prison_north_view

ExitCode=0. Отчёты: `Logs/Ch02_Sim_15_prison_north_view.log` / `.txt`. Player simulation: 15_prison_north — 2026-10-17 00:00:10 — 3 warnings, 0 errors.

```text
watch 30 2; start storage; w 1; shot storage; health
```

Фактическое состояние:

```text
t= 31,02   health 100,00 sanity 100,00 dead False pos (3.49, 2.85, 92.41) roll 0,0 pitch 0,0 yaw 90,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Предупреждения/ошибки скриптов в отчёте (0 errors не исключает HplLog.Error, записанные как WARNING):

- 1× [Script] Entity 'object_19' with type any and subtype any does not exist!
- 1× [Script] SetInsanitySetEnabled: skipped, needs insanity events (step 6)
- 1× [Script] AutoSave: skipped, needs saving (step 6)

## Геометрия и контрольный проход

Просмотрены стартовый снимок и характерный вид; короткий проход у старта выполняется командой w 1 после watch 30. Его пределы, остановки о двери и дополнительные лестницы уточняются фактическими отчётами. Полное прохождение карты не заявляется.

Аудит: `Logs/Ch02_Audit_15_prison_north.log` / `.txt` (HplMeshAudit.Run, -batchmode -nographics -quit), ExitCode=1. Общий PASS не заявляется.

```text
Models=135, submeshes=294, geometryFailures=0, prefabMatrixDifferences=17, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2
Scene: instances=1334, submeshMatrices=1606, sharedMeshColliders=930, failures=42, maxMatrixError=1,52588E-05
Resource regression: checks=471, failures=0
```

Проверка сериализованной сцены: среди сообщений missing submesh точное имя рендерера найдено у 31, другое имя — у 11, рендерер не найден — у 0. Это отдельная проверка ограничения MeshFilter/SkinnedMeshRenderer, не общий PASS аудитора. Подробности: `Logs/Ch02_RendererReview.json`.
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
