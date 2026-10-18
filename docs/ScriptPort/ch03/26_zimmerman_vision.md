# 26_zimmerman_vision

Оригинал: `source/maps/main/ch03/26_zimmerman_vision.hps` (272 строк, Latin-1).  
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/ZimmermanVision.cs`, `RTXMnesia.Levels.Ch03.ZimmermanVision`.  
Атрибут: `HplMapScript("maps/main/ch03/26_zimmerman_vision.map")`.  
Сцена: `Assets/AmnesiaImported/Levels/maps/main/ch03/26_zimmerman_vision.unity` (вне git).  
Дата: 2026-10-16. Статус: готов — C#-порт, компиляция и диагностические проверки завершены; ограничения движка и геометрии перечислены ниже.

## Функции

Все 10 исходных функций перенесены в исходном порядке, включая debug/неиспользуемые обработчики. Сигнатуры сохраняют число/типы параметров; только имеющиеся lifecycle-функции стали public override.

| Оригинал | Порт | Статус |
|---|---|---|
| `26_zimmerman_vision.hps:9` | `ZimmermanVision.PlayEffectVoice` | готов |
| `26_zimmerman_vision.hps:30` | `ZimmermanVision.ResumeEvents` | готов |
| `26_zimmerman_vision.hps:37` | `ZimmermanVision.TimerMainEvents` | готов |
| `26_zimmerman_vision.hps:145` | `ZimmermanVision.TimerDelaySFX` | готов |
| `26_zimmerman_vision.hps:160` | `ZimmermanVision.OnVoiceOver` | готов |
| `26_zimmerman_vision.hps:174` | `ZimmermanVision.CollideSpeedDecInOut` | готов |
| `26_zimmerman_vision.hps:184` | `ZimmermanVision.CollideSpeedDec` | готов |
| `26_zimmerman_vision.hps:195` | `ZimmermanVision.OnStart` | готов |
| `26_zimmerman_vision.hps:250` | `ZimmermanVision.OnEnter` | готов |
| `26_zimmerman_vision.hps:258` | `ZimmermanVision.OnLeave` | готов |

## Данные

Глобальных полей/enum нет; локальные переменные остаются в исходных функциях.

## Отличия, ошибки оригинала и зависимости

- Все 10 исходных функций, состояния TimerMainEvents 0–11, паузы на голосах и все четыре SFX сохранены, включая не вызываемый штатной цепочкой sfx2.
- Обычный старт ограничен 15 секундами: экран ещё чёрный по оригиналу, слышны вступительные реплики. Это не проверка геометрии после FadeIn.
- Отдельная диагностика прошла состояния 0–10 с точечным снятием TimerMainEvents после каждого шага и сбросом callback голосов между состояниями; OnLeave вызван отдельно. Она не считается межкартовым переходом. Состояние 11 переносит в NaveRedux, PlayerStartArea_AfterVision.
- Никаких AutoSave/CheckPoint в исходнике нет, они не добавлены.
- PreloadSound — пустой API. ChangeMap и HplSavedMaps реализованы; CreateDataCache/DestroyDataCache в соседней карте пока ничего не делают.
- Аудит выявил реальные расхождения матриц ground road/forest, а не missing SkinnedMeshRenderer. Причина: оригинал рисует кэш `.msh` (`ForceCacheLoadingAndSkipSaving` = true, `LuxConfigHandler.cpp:73`), а `.msh` у `zimmarman_ground.dae` записан другой версией загрузчика Collada (знак X узловых сдвигов Z_UP). С 2026-10-18 сборщик ставит подмеш по матрице `.msh`, если вершины совпадают (`HplModelBuilder.ApplyMeshCache`, CLAUDE.md «Кэш `.msh`»); аудит проходит (ниже).

Общие изменения синтаксиса: string &in / string & → string; десятичные литералы → float с f; существующие статические callback-имена → nameof, динамические/пустые/необъявленные остаются строками. Порядок вызовов, исходные литералы, ветви и комментарии сохранены. API/инвентарь/импортёр в этой задаче не исправлялись. Статическая проверка нашла все имена API и совместимые сигнатуры; наличие объявления не означает реализованную механику.

## Проверки

Результаты и точные fixtures приведены ниже. Все ресурсы/отчёты — в игнорируемых Assets/AmnesiaImported и Logs. Исходники игры не изменялись.


## Итог проверок главы 3

Дата: 2026-10-16. Начальное git status было чистым; чужих изменений не было. Новых механик/API не добавлено. Источники и установленная игра не изменялись, коммит/пуш не выполнялись.

Сверка `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch03 -Map 26_zimmerman_vision -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/ZimmermanVision.cs`: **0 ошибок, 0 предупреждений**. Компиляция подтверждена Unity-прогонами. Независимое сравнение токенов после перечисленных адаптаций C# не нашло различий; все исходные комментарии присутствуют (`Logs/Ch03_TokenReview.txt`, воспроизводимый диагностический Ch03_TokenReview.py).

Проверены 36 используемых API: все имена и сигнатуры существуют. Полная таблица актуальных сигнатур/файлов и ограничений — `Logs/Ch03_API_Inventory.md`. Существенные заглушки и пустые операции:

| API | Фактический статус |
|---|---|
| `PreloadSound` | пустая операция |

Все отчёты далее — внутри `UnityProj/UMnesiaRTX/Logs/`, вне git. PlayerSimulation: `-batchmode -quit`, GPU, `-seed 1`, шаг 1/60; импорт/аудит — `-batchmode -nographics -quit`. Ни один отчёт с WARNING [Script] ERROR не трактуется как чистый скрипт только из-за 0 errors.

| Отчёт | Итог |
|---|---|
| `Ch03_Import_26_zimmerman_vision.log/.txt` | ExitCode 0; 3 warnings, 0 errors |
| `Ch03_Sim_26_zimmerman_events.log/.txt` | ExitCode 0; 0 warnings, 0 errors |
| `Ch03_Sim_26_zimmerman_start.log/.txt` | ExitCode 0; 0 warnings, 0 errors |
| `Ch03_Audit_26_zimmerman_vision.log/.txt` | **ExitCode 1, общий аудит не PASS** (до исправления 2026-10-18, повторный — PASS, см. ниже): Models=21, submeshes=24, geometryFailures=0, prefabMatrixDifferences=2, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=3,11688; Scene: instances=191, submeshMatrices=193, sharedMeshColliders=18, failures=2, maxMatrixError=3,8961; Resource regression: checks=471, failures=0 |

Два placement failure road/forest у special_zimmarman_ground_1 — реальная разница матриц до 3.8961, связанная с матрицами модели (prefab max 3.11688). Исправлено в импортёре 2026-10-18 (см. выше). Повторный аудит 2026-10-18 (`HplMeshAudit.Run -amnesiaMap maps/main/ch03/26_zimmerman_vision.map`, ExitCode 0): Models=21, submeshes=24, geometryFailures=0, prefabMatrixDifferences=0, maxPrefabMatrixError=1.2e-7; Scene: instances=191, submeshMatrices=193, failures=0, maxMatrixError=4.3e-6; Resource regression: checks=471, failures=0. Серое «небо» в кадрах — подмеш `Scene_Root` модели земли (оболочка вокруг сцены), HPL2 его тоже затуманивает областями тумана карты.

### Точные диагностические fixtures

Базовая команда: `Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch03/26_zimmerman_vision.map -seed 1 -script "<команды>" -logFile <Logs/Ch03_имя.log>`. Флаг scriptDebug включается только там, где указан; глобальные переменные передаются до OnStart. Скриптовые выдачи предметов/снятие таймеров/callbacks — подготовка диагностики, не изменения порта и не доказательство физического решения.

`Ch03_Sim_26_zimmerman_events`: scriptDebug=false, globals=`(нет)`.

```text
call RemoveTimer TimerMainEvents; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 0; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 1; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 2; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 3; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 4; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 5; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 6; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 7; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 8; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 9; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call SetLocalVarInt MainEventNum 10; call TimerMainEvents TimerMainEvents; call RemoveTimer TimerMainEvents; watch 10 2; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveTimer TimerMainEvents; call CollideSpeedDecInOut Player AreaSpeedDecInOut_1 1; call CollideSpeedDecInOut Player AreaSpeedDecInOut_1 -1; call CollideSpeedDec Player AreaSpeedDec_1 1; call TimerDelaySFX sfx2; watch 10 2; call OnLeave; watch 1 1; call GetLocalVarInt MainEventNum; shot events
```

### Предупреждения прогонов



Это сохранённые исходные обращения, недостающие механики или явно указанная диагностическая подготовка. Для одноразовых/уже завершённых звуков и PS отсутствие записи при Stop/Destroy не трактуется как отсутствующий ресурс. Полный порядок сообщений и состояния находятся в соответствующих .txt. Исключений, PortError, takes N arguments и отсутствующих функций порта не обнаружено; исходные отсутствующие callbacks перечислены отдельно.

### Общие регрессии и границы готовности

До изменений: Ch03_Baseline_MapChange.log — **66 PASS**; Ch03_Baseline_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS**. После всех шести портов: Ch03_Regression_MapChange.log — **66 PASS, ExitCode 0**; Ch03_Regression_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS, ExitCode 0**. Обе Play-регрессии запускались без -quit/-nographics и завершили процесс самостоятельно. Тесты не менялись.

Межкартовая проверка: `Logs/Ch03_PlayReturn/Report.txt` — **18 PASS, ExitCode 0**. Оригинальный PickUpDiary запускает таймерный переход в Zimmerman; все состояния 0–11 проходят с настоящими длительностями голосов; затем исходный ChangeMap возвращает в PlayerStartArea_AfterVision. Восстановлены локальный marker=77 и FlashbackEventNum=6 из HplSavedMaps; сохранились инвентарь и global marker=321; FromZimmermanFarm потреблён, инвентарь/фонарь/рассудок/прыжок/присед восстановлены. Проверен короткий проход в видении. Снимки VisionVisible, VisionWalk, Returned сохранены; видимое окружение видения и возврат просмотрены. Временный диагностический Editor-скрипт размещался в игнорируемой папке и удалён после выхода Unity; его копия для воспроизведения осталась только в Logs/Ch03Verification.cs. Это проверяет связку 26↔Zimmerman, а не весь маршрут основной игры.

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
