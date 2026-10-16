# 28_inner_sanctum

Оригинал: `source/maps/main/ch03/28_inner_sanctum.hps` (872 строк, Latin-1).  
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/InnerSanctum.cs`, `RTXMnesia.Levels.Ch03.InnerSanctum`.  
Атрибут: `HplMapScript("maps/main/ch03/28_inner_sanctum.map")`.  
Сцена: `Assets/AmnesiaImported/Levels/maps/main/ch03/28_inner_sanctum.unity` (вне git).  
Дата: 2026-10-16. Статус: готов — C#-порт, компиляция и диагностические проверки завершены; ограничения движка и геометрии перечислены ниже.

## Функции

Все 44 исходных функций перенесены в исходном порядке, включая debug/неиспользуемые обработчики. Сигнатуры сохраняют число/типы параметров; только имеющиеся lifecycle-функции стали public override.

| Оригинал | Порт | Статус |
|---|---|---|
| `28_inner_sanctum.hps:9` | `InnerSanctum.TimerDistantHowl` | готов |
| `28_inner_sanctum.hps:26` | `InnerSanctum.TimerStopRadialBlur` | готов |
| `28_inner_sanctum.hps:32` | `InnerSanctum.TimerScareReaction` | готов |
| `28_inner_sanctum.hps:49` | `InnerSanctum.PlayerInteractDoor` | готов |
| `28_inner_sanctum.hps:69` | `InnerSanctum.TimerCloseGate` | готов |
| `28_inner_sanctum.hps:80` | `InnerSanctum.TimerSafetyDoorQuest` | готов |
| `28_inner_sanctum.hps:86` | `InnerSanctum.CollideCheckCollapsedDoor` | готов |
| `28_inner_sanctum.hps:102` | `InnerSanctum.PickDiary` | готов |
| `28_inner_sanctum.hps:114` | `InnerSanctum.TimerPreSlimeShow` | готов |
| `28_inner_sanctum.hps:123` | `InnerSanctum.TimerGuardianRoar` | готов |
| `28_inner_sanctum.hps:181` | `InnerSanctum.TimerGuardianDoorBreak` | готов |
| `28_inner_sanctum.hps:206` | `InnerSanctum.InteractTouchAltar` | готов |
| `28_inner_sanctum.hps:257` | `InnerSanctum.TimerBloodSound` | готов |
| `28_inner_sanctum.hps:261` | `InnerSanctum.TimerPlayerReact` | готов |
| `28_inner_sanctum.hps:270` | `InnerSanctum.CollideAltarGround` | готов |
| `28_inner_sanctum.hps:311` | `InnerSanctum.LookAtAltarWall` | готов |
| `28_inner_sanctum.hps:336` | `InnerSanctum.TimerCompletePuzzle` | готов |
| `28_inner_sanctum.hps:399` | `InnerSanctum.TimerWallLightOut` | готов |
| `28_inner_sanctum.hps:405` | `InnerSanctum.TimerEndThings` | готов |
| `28_inner_sanctum.hps:411` | `InnerSanctum.TimerEndReact` | готов |
| `28_inner_sanctum.hps:424` | `InnerSanctum.TimerGuardianHit` | готов |
| `28_inner_sanctum.hps:444` | `InnerSanctum.CollideAreaCloseDoor` | готов |
| `28_inner_sanctum.hps:463` | `InnerSanctum.CollideAreaSeeNote` | готов |
| `28_inner_sanctum.hps:470` | `InnerSanctum.TimerEndLookAt` | готов |
| `28_inner_sanctum.hps:487` | `InnerSanctum.CollideAreaGuardianAmb` | готов |
| `28_inner_sanctum.hps:493` | `InnerSanctum.CollideAreaStartIntro` | готов |
| `28_inner_sanctum.hps:500` | `InnerSanctum.TimerEIntro` | готов |
| `28_inner_sanctum.hps:568` | `InnerSanctum.IntroEventSteps` | готов |
| `28_inner_sanctum.hps:579` | `InnerSanctum.CollideGuardianArea` | готов |
| `28_inner_sanctum.hps:591` | `InnerSanctum.CollideAreaSeeLever` | готов |
| `28_inner_sanctum.hps:597` | `InnerSanctum.TimerEndLookAtLever` | готов |
| `28_inner_sanctum.hps:603` | `InnerSanctum.StateChangeLever` | готов |
| `28_inner_sanctum.hps:620` | `InnerSanctum.TimerMusic` | готов |
| `28_inner_sanctum.hps:627` | `InnerSanctum.TimerPlayerDeath` | готов |
| `28_inner_sanctum.hps:634` | `InnerSanctum.InteractPlayerAltar` | готов |
| `28_inner_sanctum.hps:648` | `InnerSanctum.CPCall01` | готов |
| `28_inner_sanctum.hps:683` | `InnerSanctum.CPCall02` | готов |
| `28_inner_sanctum.hps:699` | `InnerSanctum.CPReset` | готов |
| `28_inner_sanctum.hps:732` | `InnerSanctum.CPEventInit` | готов |
| `28_inner_sanctum.hps:754` | `InnerSanctum.TimerCPEvent` | готов |
| `28_inner_sanctum.hps:795` | `InnerSanctum.PlayerMovement` | готов |
| `28_inner_sanctum.hps:806` | `InnerSanctum.OnStart` | готов |
| `28_inner_sanctum.hps:847` | `InnerSanctum.OnEnter` | готов |
| `28_inner_sanctum.hps:866` | `InnerSanctum.OnLeave` | готов |

## Данные

Глобальных полей/enum нет; локальные переменные остаются в исходных функциях.

## Отличия, ошибки оригинала и зависимости

- Все 44 исходные функции, отключённые события/комментарии и этапы пробуждения после смерти сохранены.
- PlayerInteractDoor проверяет quest 20SafetyDoorClosed, но добавляет 28DoorShut: исходное несоответствие сохранено.
- TimerPlayerReact всегда пытается проиграть переданное имя звука, включая stopeffect; если такого звука нет, это предупреждение источника, а не повод убрать вызов.
- OnStart регистрирует slime_entry_6_5, хотя в сцене основная последовательность имеет пять шагов; имена и границы не исправлялись.
- Preload-строки с пробелами и 8_done02 сохранены. PreloadSound/PreloadParticleSystem сейчас пустые, поэтому не доказывают наличие каждого звука.
- Проверяются закрытие входа рычагом, обе жертвы/плиты, PuzzleComplete=2, открытие большого затвора и последующее закрытие, повторное взаимодействие с алтарём, CP01→CP02 через реальные смерти и сброс/восстановление игрока.
- AutoSave — NotPorted. Ресурсы/движок и импортер не менялись.

Общие изменения синтаксиса: string &in / string & → string; десятичные литералы → float с f; существующие статические callback-имена → nameof, динамические/пустые/необъявленные остаются строками. Порядок вызовов, исходные литералы, ветви и комментарии сохранены. API/инвентарь/импортёр в этой задаче не исправлялись. Статическая проверка нашла все имена API и совместимые сигнатуры; наличие объявления не означает реализованную механику.

## Проверки

Результаты и точные fixtures приведены ниже. Все ресурсы/отчёты — в игнорируемых Assets/AmnesiaImported и Logs. Исходники игры не изменялись.


## Итог проверок главы 3

Дата: 2026-10-16. Начальное git status было чистым; чужих изменений не было. Новых механик/API не добавлено. Источники и установленная игра не изменялись, коммит/пуш не выполнялись.

Сверка `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch03 -Map 28_inner_sanctum -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/InnerSanctum.cs`: **0 ошибок, 0 предупреждений**. Компиляция подтверждена Unity-прогонами. Независимое сравнение токенов после перечисленных адаптаций C# не нашло различий; все исходные комментарии присутствуют (`Logs/Ch03_TokenReview.txt`, воспроизводимый диагностический Ch03_TokenReview.py).

Проверены 55 используемых API: все имена и сигнатуры существуют. Полная таблица актуальных сигнатур/файлов и ограничений — `Logs/Ch03_API_Inventory.md`. Существенные заглушки и пустые операции:

| API | Фактический статус |
|---|---|
| `AutoSave` | NotPorted / условная реализация |
| `PreloadParticleSystem` | пустая операция |
| `PreloadSound` | пустая операция |

Все отчёты далее — внутри `UnityProj/UMnesiaRTX/Logs/`, вне git. PlayerSimulation: `-batchmode -quit`, GPU, `-seed 1`, шаг 1/60; импорт/аудит — `-batchmode -nographics -quit`. Ни один отчёт с WARNING [Script] ERROR не трактуется как чистый скрипт только из-за 0 errors.

| Отчёт | Итог |
|---|---|
| `Ch03_Import_28_inner_sanctum.log/.txt` | ExitCode 0; 7 warnings, 0 errors |
| `Ch03_Sim_28_altars.log/.txt` | ExitCode 0; 8 warnings, 0 errors |
| `Ch03_Sim_28_checkpoints.log/.txt` | ExitCode 0; 12 warnings, 0 errors |
| `Ch03_Sim_28_inner_sanctum_start.log/.txt` | ExitCode 0; 2 warnings, 0 errors |
| `Ch03_Audit_28_inner_sanctum.log/.txt` | **ExitCode 1, общий аудит не PASS**: Models=80, submeshes=125, geometryFailures=0, prefabMatrixDifferences=2, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2; Scene: instances=827, submeshMatrices=1465, sharedMeshColliders=1015, failures=4, maxMatrixError=3,8147E-06; Resource regression: checks=471, failures=0 |

Разбор missing submesh: точное имя с существующим рендерером — 4; рендерер с другим именем внутри сущности — 0; полностью отсутствующих рендереров — 0. Это ограничение поиска MeshFilter/имён, а не доказательство независимой проверки всей скелетной геометрии. Подробности: Ch03_RendererReview.json. Разницы матриц prefab и итоговой сцены не смешиваются.

### Точные диагностические fixtures

Базовая команда: `Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch03/28_inner_sanctum.map -seed 1 -script "<команды>" -logFile <Logs/Ch03_имя.log>`. Флаг scriptDebug включается только там, где указан; глобальные переменные передаются до OnStart. Скриптовые выдачи предметов/снятие таймеров/callbacks — подготовка диагностики, не изменения порта и не доказательство физического решения.

`Ch03_Sim_28_altars`: scriptDebug=false, globals=`(нет)`.

```text
call PlayerInteractDoor safety_large_vert_1; watch 10 2; call StateChangeLever lever_nice01_1 -1; watch 10 2; call InteractTouchAltar Altar_1; watch 10 2; call CollideAltarGround Player Altar_1_Ground 1; watch 10 2; call GetLocalVarInt PuzzleComplete; call InteractTouchAltar Altar_2; watch 10 2; call CollideAltarGround Player Altar_2_Ground 1; watch 20 2; call GetLocalVarInt PuzzleComplete; body safety_large_vert_1; call InteractTouchAltar Altar_1; watch 10 2; call CollideAreaCloseDoor Player AreaCloseDoor 1; watch 10 2; start center_room; w 1; s 1; shot altars
```

`Ch03_Sim_28_checkpoints`: scriptDebug=false, globals=`(нет)`.

```text
call CollideAreaStartIntro Player AreaStartIntro 1; watch 10 2; call GivePlayerDamage 200 claws true true; watch 20 2; health; call StateChangeLever lever_nice01_1 -1; call GivePlayerDamage 200 claws true true; watch 20 2; health; call CollideAreaSeeNote Player AreaSeeNote 1; watch 10 2; call CollideAreaGuardianAmb Player AreaGuardianAmb 1; watch 10 2; call InteractPlayerAltar Altar_1; watch 10 2; call CPReset; health; shot cp
```

### Предупреждения прогонов

- [Script] Entity 'slime_entry_6_5' does not exist!
- [Script] AutoSave: skipped, needs saving (step 6)
- [Script] ERROR: Cannot find sound entity 'stopeffect'
- [Script] ERROR: Could not find sound entity 'slime1'
- [Script] ERROR: Could not find sound entity 'slime2'
- [Script] ERROR: Could not find sound entity 'slime3'
- [Script] ERROR: Could not find sound entity 'slime4'
- [Script] ERROR: Could not find sound entity 'slime5'
- [Script] ERROR: Could not find particle system 'slimeps1'
- [Script] ERROR: Could not find particle system 'slimeps2'
- [Script] ERROR: Could not find particle system 'slimeps3'
- [Script] ERROR: Could not find particle system 'slimeps4'
- [Script] ERROR: Could not find particle system 'slimeps5'

Это сохранённые исходные обращения, недостающие механики или явно указанная диагностическая подготовка. Для одноразовых/уже завершённых звуков и PS отсутствие записи при Stop/Destroy не трактуется как отсутствующий ресурс. Полный порядок сообщений и состояния находятся в соответствующих .txt. Исключений, PortError, takes N arguments и отсутствующих функций порта не обнаружено; исходные отсутствующие callbacks перечислены отдельно.

### Общие регрессии и границы готовности

До изменений: Ch03_Baseline_MapChange.log — **66 PASS**; Ch03_Baseline_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS**. После всех шести портов: Ch03_Regression_MapChange.log — **66 PASS, ExitCode 0**; Ch03_Regression_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS, ExitCode 0**. Обе Play-регрессии запускались без -quit/-nographics и завершили процесс самостоятельно. Тесты не менялись.

В собственный итог входят шесть новых классов Ch03 с .meta, meta папки, шесть журналов ch03, строка этапа 5 в синхронных AGENTS.md/CLAUDE.md и автоматически добавленные шесть сцен в EditorBuildSettings.asset. Стартовое меню осталось первым. TimeManager/DynamicsManager были чистыми до работы; автоматическое пересохранение Unity убрано после выхода. Планы других глав, старые порты/журналы, api.md, движок, импортёр и тесты не менялись. Игнорируемые ресурсы в git не включены.

Итоговый `git status --short --untracked-files=all` (все перечисленные изменения относятся к этой задаче):

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/CellTunnels.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/CellTunnels.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/InnerSanctum.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/InnerSanctum.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/OrbChamber.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/OrbChamber.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureChancelRedux.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureChancelRedux.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureNaveRedux.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureNaveRedux.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/ZimmermanVision.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/ZimmermanVision.cs.meta
?? docs/ScriptPort/ch03/25_cell_tunnels.md
?? docs/ScriptPort/ch03/26_torture_nave_redux.md
?? docs/ScriptPort/ch03/26_zimmerman_vision.md
?? docs/ScriptPort/ch03/27_torture_chancel_redux.md
?? docs/ScriptPort/ch03/28_inner_sanctum.md
?? docs/ScriptPort/ch03/29_orb_chamber.md
```
