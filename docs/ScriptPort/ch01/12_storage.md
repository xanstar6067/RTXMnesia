# 12_storage

- **Оригинал:** `source/maps/main/ch01/12_storage.hps` (Latin-1, 1184 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Storage.cs`, класс `RTXMnesia.Levels.Ch01.Storage`, атрибут `[HplMapScript("maps/main/ch01/12_storage.map")]`.
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/12_storage.unity` (генерируется, вне git).
- **Статус:** Перенесены все 70 функций; проверены обе ёмкости, полная смесь, завал, алтарное событие, стержни и основные события монстров. Перенос выполнен в составе пакета 08–14; обе обязательные регрессии прошли. Независимый аудит сцены имеет замечания, разобранные ниже; полное ручное прохождение не заявляется. Дата: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в исходном скрипте нет. Все функции перечислены в исходном порядке; local/global var API хранит значения карты/игры, а не поля класса.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `PickDrill` (`12_storage.hps:5`) | `Storage.PickDrill` | готов | 1:1, исходный порядок и параметры |
| `TimerDrillEnemy` (`12_storage.hps:18`) | `Storage.TimerDrillEnemy` | готов | 1:1, исходный порядок и параметры |
| `DisableEnemy` (`12_storage.hps:31`) | `Storage.DisableEnemy` | готов | 1:1, исходный порядок и параметры |
| `InteractEpoxy` (`12_storage.hps:48`) | `Storage.InteractEpoxy` | готов | 1:1, исходный порядок и параметры |
| `TimerInteractEpoxy` (`12_storage.hps:61`) | `Storage.TimerInteractEpoxy` | готов | 1:1, исходный порядок и параметры |
| `UseDrillOnEpoxy` (`12_storage.hps:68`) | `Storage.UseDrillOnEpoxy` | готов | сверло, вращение, частицы и все таймеры |
| `TimerDrill` (`12_storage.hps:106`) | `Storage.TimerDrill` | готов | 1:1, исходный порядок и параметры |
| `UseDrillPartOnEpoxy` (`12_storage.hps:129`) | `Storage.UseDrillPartOnEpoxy` | готов | 1:1, исходный порядок и параметры |
| `UseEmptyContainerOnEpoxy` (`12_storage.hps:136`) | `Storage.UseEmptyContainerOnEpoxy` | готов | укороченное имя, первая половина жидкости |
| `TimerHalfFill` (`12_storage.hps:178`) | `Storage.TimerHalfFill` | готов | 1:1, исходный порядок и параметры |
| `UseHalfContainerOnEpoxy` (`12_storage.hps:196`) | `Storage.UseHalfContainerOnEpoxy` | готов | исходные проверки имён и полный состав |
| `TimerFullFill` (`12_storage.hps:239`) | `Storage.TimerFullFill` | готов | 1:1, исходный порядок и параметры |
| `PickEpoxy` (`12_storage.hps:258`) | `Storage.PickEpoxy` | готов | 1:1, исходный порядок и параметры |
| `TimerMmMonster` (`12_storage.hps:266`) | `Storage.TimerMmMonster` | готов | 1:1, исходный порядок и параметры |
| `UseEpoxyOnCavein` (`12_storage.hps:282`) | `Storage.UseEpoxyOnCavein` | готов | 1:1, исходный порядок и параметры |
| `TimerStopLookCaveIn` (`12_storage.hps:294`) | `Storage.TimerStopLookCaveIn` | готов | 1:1, исходный порядок и параметры |
| `InteractEpoxyByCave` (`12_storage.hps:311`) | `Storage.InteractEpoxyByCave` | готов | 1:1, исходный порядок и параметры |
| `DaBigBoom` (`12_storage.hps:316`) | `Storage.DaBigBoom` | готов | 1:1, исходный порядок и параметры |
| `TimerBigBoomEvent` (`12_storage.hps:325`) | `Storage.TimerBigBoomEvent` | готов | все три ветки урона, свет и разрушение завала |
| `TimerCompleteQuest` (`12_storage.hps:365`) | `Storage.TimerCompleteQuest` | готов | 1:1, исходный порядок и параметры |
| `CollideBoomArea` (`12_storage.hps:381`) | `Storage.CollideBoomArea` | готов | 1:1, исходный порядок и параметры |
| `CollideLightsOut` (`12_storage.hps:397`) | `Storage.CollideLightsOut` | готов | 1:1, исходный порядок и параметры |
| `TimerLightsOut` (`12_storage.hps:424`) | `Storage.TimerLightsOut` | готов | все исходные этапы, включая необработанный lightsq |
| `InteractCloth` (`12_storage.hps:476`) | `Storage.InteractCloth` | готов | 1:1, исходный порядок и параметры |
| `TimerClothOn` (`12_storage.hps:489`) | `Storage.TimerClothOn` | готов | 1:1, исходный порядок и параметры |
| `CollideEnemy1` (`12_storage.hps:501`) | `Storage.CollideEnemy1` | готов | 1:1, исходный порядок и параметры |
| `TimerEnemy1` (`12_storage.hps:507`) | `Storage.TimerEnemy1` | готов | 1:1, исходный порядок и параметры |
| `PickRod` (`12_storage.hps:525`) | `Storage.PickRod` | готов | 1:1, исходный порядок и параметры |
| `TimerRodGrunt` (`12_storage.hps:566`) | `Storage.TimerRodGrunt` | готов | 1:1, исходный порядок и параметры |
| `AlwaysLocked` (`12_storage.hps:587`) | `Storage.AlwaysLocked` | готов | 1:1, исходный порядок и параметры |
| `CollideGiveQuest` (`12_storage.hps:591`) | `Storage.CollideGiveQuest` | готов | 1:1, исходный порядок и параметры |
| `VisionOverGiveQuest` (`12_storage.hps:595`) | `Storage.VisionOverGiveQuest` | готов | 1:1, исходный порядок и параметры |
| `TimerGiveQuest` (`12_storage.hps:600`) | `Storage.TimerGiveQuest` | готов | 1:1, исходный порядок и параметры |
| `TimerGirlCry` (`12_storage.hps:604`) | `Storage.TimerGirlCry` | готов | 1:1, исходный порядок и параметры |
| `CollideEnemy2Extra` (`12_storage.hps:617`) | `Storage.CollideEnemy2Extra` | готов | 1:1, исходный порядок и параметры |
| `CollideEnemy3Extra` (`12_storage.hps:635`) | `Storage.CollideEnemy3Extra` | готов | 1:1, исходный порядок и параметры |
| `CollideEnemy1Extra` (`12_storage.hps:652`) | `Storage.CollideEnemy1Extra` | готов | 1:1, исходный порядок и параметры |
| `CheckPointEnemy2` (`12_storage.hps:674`) | `Storage.CheckPointEnemy2` | готов | 1:1, исходный порядок и параметры |
| `CheckPointEnemy3` (`12_storage.hps:683`) | `Storage.CheckPointEnemy3` | готов | 1:1, исходный порядок и параметры |
| `CheckPointEnemy1` (`12_storage.hps:693`) | `Storage.CheckPointEnemy1` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaNoise` (`12_storage.hps:718`) | `Storage.CollideAreaNoise` | готов | 1:1, исходный порядок и параметры |
| `EntityCallAreaNoise_1` (`12_storage.hps:744`) | `Storage.EntityCallAreaNoise_1` | готов | 1:1, исходный порядок и параметры |
| `EntityCallAreaNoise_2` (`12_storage.hps:750`) | `Storage.EntityCallAreaNoise_2` | готов | 1:1, исходный порядок и параметры |
| `EntityCallAreaNoise_3` (`12_storage.hps:756`) | `Storage.EntityCallAreaNoise_3` | готов | 1:1, исходный порядок и параметры |
| `EntityCallAreaNoise_4` (`12_storage.hps:762`) | `Storage.EntityCallAreaNoise_4` | готов | 1:1, исходный порядок и параметры |
| `UpdateNoiseValue` (`12_storage.hps:769`) | `Storage.UpdateNoiseValue` | готов | усечение float в int и Str для отладочной строки |
| `TimerSanityDamage` (`12_storage.hps:793`) | `Storage.TimerSanityDamage` | готов | 1:1, исходный порядок и параметры |
| `CollideHoleDude` (`12_storage.hps:813`) | `Storage.CollideHoleDude` | готов | 1:1, исходный порядок и параметры |
| `TimerHoleMonster` (`12_storage.hps:830`) | `Storage.TimerHoleMonster` | готов | 1:1, исходный порядок и параметры |
| `TimerPlayerReactions` (`12_storage.hps:839`) | `Storage.TimerPlayerReactions` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaChain` (`12_storage.hps:861`) | `Storage.CollideAreaChain` | готов | 1:1, исходный порядок и параметры |
| `CollideChainEndWithAreaChain` (`12_storage.hps:866`) | `Storage.CollideChainEndWithAreaChain` | готов | 1:1, исходный порядок и параметры |
| `InteractGruntDoor` (`12_storage.hps:874`) | `Storage.InteractGruntDoor` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaCloseDoor` (`12_storage.hps:882`) | `Storage.CollideAreaCloseDoor` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaGruntRoom` (`12_storage.hps:905`) | `Storage.CollideAreaGruntRoom` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaGruntRoomActivate` (`12_storage.hps:921`) | `Storage.CollideAreaGruntRoomActivate` | готов | 1:1, исходный порядок и параметры |
| `StartBangDoorEvent` (`12_storage.hps:933`) | `Storage.StartBangDoorEvent` | готов | 1:1, исходный порядок и параметры |
| `StopBangDoorEvent` (`12_storage.hps:943`) | `Storage.StopBangDoorEvent` | готов | 1:1, исходный порядок и параметры |
| `TimerEBangOnDoor` (`12_storage.hps:964`) | `Storage.TimerEBangOnDoor` | готов | все четыре стадии и исходный повтор таймера |
| `BangOnDoorEffects` (`12_storage.hps:1015`) | `Storage.BangOnDoorEffects` | готов | 1:1, исходный порядок и параметры |
| `EntityCallGruntRoomDisabled` (`12_storage.hps:1024`) | `Storage.EntityCallGruntRoomDisabled` | готов | 1:1, исходный порядок и параметры |
| `CPCallGruntRoom` (`12_storage.hps:1032`) | `Storage.CPCallGruntRoom` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaGruntBoo` (`12_storage.hps:1041`) | `Storage.CollideAreaGruntBoo` | готов | 1:1, исходный порядок и параметры |
| `CollideAreaFire` (`12_storage.hps:1052`) | `Storage.CollideAreaFire` | готов | 1:1, исходный порядок и параметры |
| `InteractFire` (`12_storage.hps:1057`) | `Storage.InteractFire` | готов | 1:1, исходный порядок и параметры |
| `TimerInteractFire` (`12_storage.hps:1061`) | `Storage.TimerInteractFire` | готов | 1:1, исходный порядок и параметры |
| `TimerBurnFire` (`12_storage.hps:1065`) | `Storage.TimerBurnFire` | готов | 1:1, исходный порядок и параметры |
| `OnStart` (`12_storage.hps:1084`) | `Storage.OnStart` | готов | 1:1, исходный порядок и параметры |
| `OnEnter` (`12_storage.hps:1161`) | `Storage.OnEnter` | готов | 1:1, исходный порядок и параметры |
| `OnLeave` (`12_storage.hps:1181`) | `Storage.OnLeave` | готов | 1:1, исходный порядок и параметры |

## Отличия от оригинала

Сигнатуры string &in / string & переведены в string; жизненный цикл — public override. Десятичные литералы имеют суффикс f. Статические имена функций в строковых аргументах используют nameof, динамические строки сохранены. Комментарии и порядок функций не менялись. Два float → int в CollideAreaNoise и UpdateNoiseValue явно усекаются. Единственная отладочная конкатенация float использует Str с G6 и InvariantCulture; 20.f записано как 20.0f. Логическое or переведено в ||.

Дополнительная лексическая сверка после нормализации только разрешённых синтаксических изменений: **5955 одинаковых токенов** действующего кода, сохранены **все 129 исходных комментариев**. Штатный Check-ScriptPort также проверил функции, строковые константы, количество вызовов API, числа и коллбэки карты. Движок, API, импортёр, инструменты и прежние порты/журналы не изменялись.

## Особенности оригинала (сохранены)

- `12_storage.hps:1108`: восемь имён Invisible_box_mass_2_1…8 отсутствуют в исходной .map, включая поиск без учёта регистра. Поэтому старт даёт восемь исходных предупреждений регистрации коллизий. Имена не заменены.
- `UseHalfContainerOnEpoxy` (:204) проверяет локальную переменную по полному asEntity, а дальше использует укороченный sEntity. Это исходная несогласованность проверяемых имён; проверка не исправлена.
- WhatEpoxyEffect и BottleNr — общие локальные переменные карты (:91–:92, :170, :228), которые читают отложенные таймеры. Возможное пересечение двух одновременных операций не устранено; в прогоне ёмкости обрабатывались последовательно.
- `TimerEBangOnDoor` (:999–:1011) при третьем цикле вызывает StopBangDoorEvent, который сбрасывает счётчик таймера; затем исходная последняя строка может снова поставить таймер. Порядок и условия не изменены.
- `UpdateNoiseValue` (:777) безусловно останавливает динамический noise-звук. Если область ещё не создавала его, возможна ошибка отсутствующего звука; защита существования не добавлялась. Этот путь не входил в основной событийный прогон.
- `TimerLightsOut` имеет отдельный таймер lightsq (:420) с закомментированной обработкой (:473). Сохранены оба, а также повторная предзагрузка 12_noise_2 (:1168).

## Не перенесено движком

| API NotPorted | skipped, needs | Последствие |
|---|---|---|
| `AutoSave` | `saving (step 6)` | нет сохранения на диск |
| `ClearEnemyPatrolNodes` | `enemies (step 6)` | маршруты монстров не очищаются |
| `AddEnemyPatrolNode` | `enemies (step 6)` | патрули/погоня не работают |

Включение монстров, их скриптовые реакции и CheckPoint-вызовы сохранены. ИИ, Insanity-области, физика верёвок/цепей ещё ограничивают полноценное прохождение. Отдельные дополнительные ветки шума, огня, стука и всех чекпоинтов не выдаются за проверенные вручную.

## Проверка

Unity 6000.6.2f1 запускался последовательно, при отсутствии Temp/UnityLockfile, с доступом к лицензии Windows. Симуляции и обе регрессии — с GPU. Все итоговые симуляции завершились кодом 0, без исключений, PortError и несовпадения числа аргументов. Предупреждения и исходные ошибки перечислены ниже. Новых предупреждений компилятора из семи портов нет; прежние предупреждения других файлов не исправлялись.

Все пути `Logs/...` ниже относятся к `Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/` и не входят в git.

### Импорт

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/12_storage.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Import_12_storage.log
```

```text
=== Level: maps/main/ch01/12_storage.map — 2026-10-16 18:49:28 — 10 warnings, 0 errors ===
Resources: 143 models (33 imported now), 14 extra materials (0 converted now), 1 cookies
WARNING: entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1'
WARNING: entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2'
WARNING: entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3'
WARNING: entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4'
WARNING: entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 6 to attach to body 'Body_1'
WARNING: entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 4 to attach to body 'Body_2'
WARNING: entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 3 to attach to body 'Body_3'
WARNING: entities/equipment/rope_beam/rope_beam02.ent: could not find child with unique ID 2 to attach to body 'Body_4'
WARNING: Skin references a missing image (unused by this menu): gui_amn_mm_textbox_marker.tga
WARNING: No level script port for maps/main/ch01/12_storage.map yet (the level runs without its events)
```

Импорт завершён кодом 0. Полный сохранённый отчёт: `Logs/12_storage_import_report.log`. Предупреждение No level script port относится к импорту до добавления C#; последующие прогоны находят Storage. Сцену автоматически добавил импортёр в EditorBuildSettings.asset; это ожидаемое изменение сохранено.

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 12_storage -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Storage.cs
```

```text
12_storage : функций 70, строк 353, ошибок 0, предупреждений 0
```

### Симуляции и события

Общий запуск (полный путь Unity.exe: `C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe`):

```text
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/12_storage.map -seed 1 -script "$script" -logFile <полный путь отдельного лога>
```

| Итоговый лог | Предупреждения | Ошибки отчёта | Код выхода |
|---|---:|---:|---:|
| `Sim_12_storage.log` | 9 | 0 | 0 |
| `Sim_12_storage_events.log` | 11 | 0 | 0 |
| `Sim_12_storage_walk.log` | 9 | 0 | 0 |

Стартовый сценарий: `watch 30 2; shot start`. Для основных прямых вызовов далее выдержано `watch 10 2`; подготовка, запросы состояния и проверки смерти явно указаны. `ChangeMap` не вызывался. Исходные ошибки HplLog отображаются как WARNING в отчёте симулятора, поэтому счётчик «0 errors» сам по себе их не исключает.

Первый резервуар просверлен (локальное состояние 1), пустая ёмкость превращается в половинную. После сверления второго половинная превращается в полную: GotEpoxy=1, epoxy_container02_4 активна. Размещение у завала включает explosive_container; DaBigBoom приводит к разрушению cave_in_1, GetEntityExists=False, QuestIsCompleted 12Cavein=True. Сработали свет, вспышки, частицы и музыка.

Алтарное событие включает лампу, последовательно проигрывает реакции и возвращает управление взглядом/фонарём. Два PickRod дают GuidingRod=2, финальный монстр и соответствующий чекпоинт; CollideEnemy1 включает первого монстра и задаёт check_enemy1. HoleDude включает камни, частицы, звуки и повторный таймер.

Первый диагностический запрос GetPropHealth уже уничтоженного cave_in_1 сам дал предупреждение отсутствия сущности. Итоговый запрос заменён на GetEntityExists, код карты не менялся. Инвентарные применения здесь вызываются напрямую для проверки цепочек скрипта, а не через UI инвентаря.

Воспроизводимые итоговые сценарии:

**events**:

```text
call InteractEpoxy epoxy_container01;
watch 10 2;
call UseDrillOnEpoxy handdrill epoxy_container01;
watch 10 2;
call GetLocalVarInt epoxy_container01;
call UseEmptyContainerOnEpoxy empty_container epoxy_container01_area_2;
watch 10 2;
body epoxy_container01_2;
call UseDrillOnEpoxy handdrill epoxy_container02;
watch 10 2;
call UseHalfContainerOnEpoxy epoxy_container01_2 epoxy_container02_area_2;
watch 10 2;
call GetLocalVarInt GotEpoxy;
body epoxy_container02_4;
call UseEpoxyOnCavein epoxy_container02_4 cave_in_1;
watch 10 2;
body explosive_container;
call DaBigBoom explosive_container OnBreak;
watch 10 2;
call GetEntityExists cave_in_1;
call QuestIsCompleted 12Cavein;
call CollideLightsOut Player AreaLightsOut 1;
watch 10 2;
call PickRod guiding_rod01 OnPickup;
watch 10 2;
call PickRod guiding_rod02 OnPickup;
watch 10 2;
call GetGlobalVarInt GuidingRod;
call CollideEnemy1 Player AreaEnemy_1 1;
watch 10 2;
call CollideHoleDude Player HoleDudeStart 1;
watch 10 2;
shot events;
```

Все уникальные предупреждения итоговых симуляций (время удалено, повторы объединены):

```text
WARNING: [Script] Entity 'Invisible_box_mass_2_1' with type any and subtype any does not exist!
WARNING: [Script] Entity 'Invisible_box_mass_2_2' with type any and subtype any does not exist!
WARNING: [Script] Entity 'Invisible_box_mass_2_3' with type any and subtype any does not exist!
WARNING: [Script] Entity 'Invisible_box_mass_2_4' with type any and subtype any does not exist!
WARNING: [Script] Entity 'Invisible_box_mass_2_5' with type any and subtype any does not exist!
WARNING: [Script] Entity 'Invisible_box_mass_2_6' with type any and subtype any does not exist!
WARNING: [Script] Entity 'Invisible_box_mass_2_7' with type any and subtype any does not exist!
WARNING: [Script] Entity 'Invisible_box_mass_2_8' with type any and subtype any does not exist!
WARNING: [Script] AutoSave: skipped, needs saving (step 6)
WARNING: [Script] AddEnemyPatrolNode: skipped, needs enemies (step 6)
WARNING: [Script] ClearEnemyPatrolNodes: skipped, needs enemies (step 6)
```

### Геометрия, пути моделей и движение

Дополнительная независимая проверка существующим инструментом, без изменений импортёра:

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.HplMeshAudit.Run -amnesiaMap maps/main/ch01/12_storage.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Audit_12_storage.log
```

```text
Models=150, submeshes=312, geometryFailures=0, prefabMatrixDifferences=8, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2
Scene: instances=1652, submeshMatrices=2547, sharedMeshColliders=1588, failures=14, maxMatrixError=1,53143E-05
Resource regression: checks=471, failures=0
```

**Общий аудит завершён кодом 1, не PASS.** Геометрия проверенных моделей совпала с MSH (geometryFailures=0, maxVertexError=0), кэши найдены, 471 проверка выбора ресурсов прошла. Замечания Scene и различия матриц исходных префабов не скрыты. Полный отчёт: `Logs/Audit_12_storage.txt`.

14 замечаний Scene относятся к двум rope_beam02/pCylinder1 и двенадцати подмешам шести монстров. Проверена структура сериализованной сцены: pCylinder1 у rope_beam02_1 является SkinnedMeshRenderer, как и рендереры монстров; аудитор ищет только MeshFilter. Это ограничение проверки, а не доказательство отсутствия всех указанных моделей. Численные матрицы проверенных узлов совпали в пределах 0.001.

Отдельный GPU-прогон движения: `watch 1 1; w 1; s 1; health; shot entry_walk`. Проверен короткий проход у начальной точки, не весь маршрут. Результат:

```text
t=  2,00 w 1              feet (-10.645, 2.793, 7.250) y 2,793..3,697 max 2,50 m/s now 2,83 ground True size 0 cam up 1,802 side -0,025 roll 0,0 | Normal crosshair Default text ""
t=  3,00 s 1              feet (-12.501, 3.695, 7.250) y 2,785..3,695 max 2,00 m/s now 2,23 ground True size 0 cam up 1,670 side 0,010 roll 0,0 | Normal crosshair Default text ""
t=  3,00   health 100,00 sanity 100,00 dead False pos (-12.50, 4.59, 7.25) roll 0,0 pitch 0,0 yaw 90,0 | hurt 0,00 noise 0 move× 1,00 head 0,000 | splashes 0 flash - | death - hint / | check point _auto at PlayerStartArea_1 count 0
```

Просмотрен контрольный `Logs/Player_12_storage_00_start.png`: Начальная лестница склада с низким сводом, свечами и пылью. Снимки сценариев и прохода также сохранены в Logs. Специальный обход всех помещений и исправление замечаний геометрического аудитора в задачу переноса скриптов не включались.

### Регрессия пакета 08–14

Оба запуска — без -quit и -nographics, после добавления всех семи портов; последующие контрольные повторы не меняли код:

```text
Unity.exe -batchmode -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MapChangePreview.RunBatch -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Regression_08_14_MapChange.log
Unity.exe -batchmode -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MainMenuPreview.RunBatch -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Regression_08_14_MainMenu.log
```

```text
PASS: 66 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian, damage and death); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 14 main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).
PASS: 14 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

Оба процесса завершены кодом 0. Проверки не менялись; ожидаемые 66 учтены.

## Файлы и состояние репозитория

В пакете созданы семь портов (.cs + сгенерированные Unity .meta) и семь журналов 08–14. В CLAUDE.md/AGENTS.md изменена только строка этапа 5; файлы побайтно одинаковы. Импортёр добавил семь сцен в EditorBuildSettings.asset. Windows.asset не изменился. Автоматическое пересохранение TimeManager.asset возвращено командой `git checkout -- UnityProj/UMnesiaRTX/ProjectSettings/TimeManager.asset` после закрытия Unity. Остальные настройки, готовые порты и журналы не менялись. Ресурсы Amnesia остаются в игнорируемых каталогах. Коммитов и push нет.

Итоговый `git status --short` всего пакета (Unity закрыт, lock-файла нет):

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/BackHall.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/BackHall.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/CellarMaze.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/CellarMaze.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/DanielsRoom.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/DanielsRoom.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Elevator.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Elevator.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/MachineRoom.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/MachineRoom.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Storage.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Storage.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Study.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Study.cs.meta
?? docs/ScriptPort/ch01/08_cellar_maze.md
?? docs/ScriptPort/ch01/09_back_hall.md
?? docs/ScriptPort/ch01/10_daniels_room.md
?? docs/ScriptPort/ch01/11_study.md
?? docs/ScriptPort/ch01/12_storage.md
?? docs/ScriptPort/ch01/13_machine_room.md
?? docs/ScriptPort/ch01/14_elevator.md
```
