# 29_orb_chamber

Оригинал: `source/maps/main/ch03/29_orb_chamber.hps` (1831 строк, Latin-1).  
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/OrbChamber.cs`, `RTXMnesia.Levels.Ch03.OrbChamber`.  
Атрибут: `HplMapScript("maps/main/ch03/29_orb_chamber.map")`.  
Сцена: `Assets/AmnesiaImported/Levels/maps/main/ch03/29_orb_chamber.unity` (вне git).  
Дата: 2026-10-16. Статус: готов — C#-порт, компиляция и диагностические проверки завершены; ограничения движка и геометрии перечислены ниже.

## Функции

Все 65 исходных функций перенесены в исходном порядке, включая debug/неиспользуемые обработчики. Сигнатуры сохраняют число/типы параметров; только имеющиеся lifecycle-функции стали public override.

| Оригинал | Порт | Статус |
|---|---|---|
| `29_orb_chamber.hps:13` | `OrbChamber.PlayEffectVoice` | готов |
| `29_orb_chamber.hps:26` | `OrbChamber.PlayFlashbackVoice` | готов |
| `29_orb_chamber.hps:47` | `OrbChamber.TimerAlexanderStartSpeak` | готов |
| `29_orb_chamber.hps:59` | `OrbChamber.ArriveVoiceOver` | готов |
| `29_orb_chamber.hps:76` | `OrbChamber.PlayAlmostDoneVoice` | готов |
| `29_orb_chamber.hps:96` | `OrbChamber.AlmostDoneVoiceOver` | готов |
| `29_orb_chamber.hps:125` | `OrbChamber.TimerStartPortalDone` | готов |
| `29_orb_chamber.hps:148` | `OrbChamber.ResetPortalCountDown` | готов |
| `29_orb_chamber.hps:161` | `OrbChamber.StartPortalCountDown` | готов |
| `29_orb_chamber.hps:177` | `OrbChamber.TimerPortalCountDown` | готов |
| `29_orb_chamber.hps:236` | `OrbChamber.TimerPortalCountDownFlash` | готов |
| `29_orb_chamber.hps:278` | `OrbChamber.InterruptAlexander` | готов |
| `29_orb_chamber.hps:345` | `OrbChamber.InterruptAlexanderVoiceOver` | готов |
| `29_orb_chamber.hps:364` | `OrbChamber.TimerInterruptionOver` | готов |
| `29_orb_chamber.hps:375` | `OrbChamber.TimerPlayerLookAtRod` | готов |
| `29_orb_chamber.hps:383` | `OrbChamber.TimerPlayerLookAtRodSparks` | готов |
| `29_orb_chamber.hps:392` | `OrbChamber.TimerStopPlayerLookAtRod` | готов |
| `29_orb_chamber.hps:406` | `OrbChamber.FirstRodInteraction` | готов |
| `29_orb_chamber.hps:421` | `OrbChamber.TimerPillarOn` | готов |
| `29_orb_chamber.hps:434` | `OrbChamber.CollideRodTippingPoint` | готов |
| `29_orb_chamber.hps:472` | `OrbChamber.TimerExtraRodPush` | готов |
| `29_orb_chamber.hps:481` | `OrbChamber.CollideRodHitGround` | готов |
| `29_orb_chamber.hps:504` | `OrbChamber.TimerRodFallEffectOver` | готов |
| `29_orb_chamber.hps:519` | `OrbChamber.PlayPortalDoneVoice` | готов |
| `29_orb_chamber.hps:533` | `OrbChamber.PortalDoneVoiceOver` | готов |
| `29_orb_chamber.hps:557` | `OrbChamber.TimerDelayOpenPortal` | готов |
| `29_orb_chamber.hps:582` | `OrbChamber.SetPortalPhase` | готов |
| `29_orb_chamber.hps:668` | `OrbChamber.InteractActivePort` | готов |
| `29_orb_chamber.hps:682` | `OrbChamber.TimerStopLookAtPortal` | готов |
| `29_orb_chamber.hps:690` | `OrbChamber.TimerPortalShakeLoop` | готов |
| `29_orb_chamber.hps:705` | `OrbChamber.UseHeadOnPortal` | готов |
| `29_orb_chamber.hps:739` | `OrbChamber.EndingSetup` | готов |
| `29_orb_chamber.hps:780` | `OrbChamber.StartAlexanderEnding` | готов |
| `29_orb_chamber.hps:795` | `OrbChamber.ResumeAlexEnding` | готов |
| `29_orb_chamber.hps:802` | `OrbChamber.TimerAlexEnding` | готов |
| `29_orb_chamber.hps:978` | `OrbChamber.TimerEndSanityDamage` | готов |
| `29_orb_chamber.hps:992` | `OrbChamber.RemorseVoiceOver` | готов |
| `29_orb_chamber.hps:997` | `OrbChamber.AlexEndWordsVoiceOver` | готов |
| `29_orb_chamber.hps:1011` | `OrbChamber.StartAgrippaEnding` | готов |
| `29_orb_chamber.hps:1028` | `OrbChamber.ResumeAgrippaEnding` | готов |
| `29_orb_chamber.hps:1034` | `OrbChamber.TimerAgrippaEnding` | готов |
| `29_orb_chamber.hps:1210` | `OrbChamber.TimerHeadOnPortalEvent` | готов |
| `29_orb_chamber.hps:1226` | `OrbChamber.CollideAgrippaEnding1` | готов |
| `29_orb_chamber.hps:1231` | `OrbChamber.CollideAgrippaEnding2` | готов |
| `29_orb_chamber.hps:1236` | `OrbChamber.CollideAgrippaEnding3` | готов |
| `29_orb_chamber.hps:1243` | `OrbChamber.AlexAngryVoiceOver` | готов |
| `29_orb_chamber.hps:1248` | `OrbChamber.AgrippaVoiceOver` | готов |
| `29_orb_chamber.hps:1266` | `OrbChamber.StartDanielEnding` | готов |
| `29_orb_chamber.hps:1284` | `OrbChamber.ResumeDanielEnding` | готов |
| `29_orb_chamber.hps:1290` | `OrbChamber.TimerDanielEnding` | готов |
| `29_orb_chamber.hps:1541` | `OrbChamber.TimerDoor` | готов |
| `29_orb_chamber.hps:1548` | `OrbChamber.DanielEndingVoiceOver` | готов |
| `29_orb_chamber.hps:1562` | `OrbChamber.ShowPlayerSlime` | готов |
| `29_orb_chamber.hps:1605` | `OrbChamber.ShowAlexanderSlime` | готов |
| `29_orb_chamber.hps:1631` | `OrbChamber.InitFlyingMonoliths` | готов |
| `29_orb_chamber.hps:1655` | `OrbChamber.SetFlyingMonolithsSpeed` | готов |
| `29_orb_chamber.hps:1671` | `OrbChamber.PlayMonolithSound` | готов |
| `29_orb_chamber.hps:1683` | `OrbChamber.TimerPlayMSound` | готов |
| `29_orb_chamber.hps:1699` | `OrbChamber.CollideFireDamage` | готов |
| `29_orb_chamber.hps:1709` | `OrbChamber.InteractBlueFire` | готов |
| `29_orb_chamber.hps:1715` | `OrbChamber.TimerBlueFire` | готов |
| `29_orb_chamber.hps:1723` | `OrbChamber.CollideAreaHead` | готов |
| `29_orb_chamber.hps:1733` | `OrbChamber.OnStart` | готов |
| `29_orb_chamber.hps:1807` | `OrbChamber.OnEnter` | готов |
| `29_orb_chamber.hps:1828` | `OrbChamber.OnLeave` | готов |

## Данные

- `gbShowCredits` — `29_orb_chamber.hps:3`; начальное значение и размер/порядок массива сохранены.
- `gfCountDownMul` — `29_orb_chamber.hps:146`; начальное значение и размер/порядок массива сохранены.
- `gvInterruptEntryNum` — `29_orb_chamber.hps:276`; начальное значение и размер/порядок массива сохранены.
- `gvRodGlowParticles` — `29_orb_chamber.hps:428`; начальное значение и размер/порядок массива сохранены.
- `gvRodBeamParticles` — `29_orb_chamber.hps:429`; начальное значение и размер/порядок массива сохранены.
- `gvRodBeamLights` — `29_orb_chamber.hps:430`; начальное значение и размер/порядок массива сохранены.
- `glNumOfOrbParticles` — `29_orb_chamber.hps:574`; начальное значение и размер/порядок массива сохранены.
- `gvOrbParticles` — `29_orb_chamber.hps:575`; начальное значение и размер/порядок массива сохранены.
- `glNumOfOrbLights` — `29_orb_chamber.hps:577`; начальное значение и размер/порядок массива сохранены.
- `gvOrbLights` — `29_orb_chamber.hps:578`; начальное значение и размер/порядок массива сохранены.
- `gfDanielEndGlobalVolume` — `29_orb_chamber.hps:1261`; начальное значение и размер/порядок массива сохранены.
- `gfDanielEndSceneFadeOutTime` — `29_orb_chamber.hps:1262`; начальное значение и размер/порядок массива сохранены.
- `gvMonolithTypeNum` — `29_orb_chamber.hps:1628`; начальное значение и размер/порядок массива сохранены.
- `gvMonolithSpeeds` — `29_orb_chamber.hps:1629`; начальное значение и размер/порядок массива сохранены.
- `g_iSound` — `29_orb_chamber.hps:1670`; начальное значение и размер/порядок массива сохранены.

## Отличия, ошибки оригинала и зависимости

- Три концовки оставлены отдельными цепочками Alexander/Agrippa/Daniel, с исходными голосами, таймерами и StartCredits endNum 0/1/2; не заменены ReturnToMenu.
- Сохранены gbShowCredits, множитель countdown, массивы частиц/света/колонн/монолитов, исходный порядок/числа и неинициализированный g_iSound (0 по умолчанию в обоих языках).
- InterruptAlexander и CollideRodTippingPoint: проверки индекса стоят непосредственно перед первым опасным чтением, после исходных предшествующих эффектов. При выходе за массив прерывается вызов как в AngelScript, без C#-исключения.
- Конкатенации float в отладочных строках — Str(float), G6/invariant.
- ResetPortalCountDown снимает TimerPortalCountDownFlash, а фактический таймер называется PortalCountDownFlash. EndingSetup также содержит исходные несовпадающие имена снимаемых таймеров. Эти ошибки не исправлены.
- Отсутствующий monolith01_2 и остановка ещё не созданных mono_sound1/2 остаются предупреждениями оригинальных обращений. OnEnter создаёт preloadhead как в источнике.
- PlayPropAnimation, SetNPCFollowPlayer и StartCredits — NotPorted (AutoSave с 2026-10-17 работает: сохранения перенесены (CLAUDE.md «Сохранения»)); ChangePlayerStateToNormal и PreloadSound — пустые. Поэтому срабатывание всей цепочки/запроса титров не означает работающие анимации Александра или титры.
- Проверки головных областей задают WhatHeadArea перед использованием головы, чтобы динамический agrippa_head_obj появился в исходной точке _Place. При ручной имитации наклона колонны одноразовый collide callback снимается отдельно: прямой call сам его не расходует.

Общие изменения синтаксиса: string &in / string & → string; десятичные литералы → float с f; существующие статические callback-имена → nameof, динамические/пустые/необъявленные остаются строками. Порядок вызовов, исходные литералы, ветви и комментарии сохранены. API/инвентарь/импортёр в этой задаче не исправлялись. Статическая проверка нашла все имена API и совместимые сигнатуры; наличие объявления не означает реализованную механику.

## Проверки

Результаты и точные fixtures приведены ниже. Все ресурсы/отчёты — в игнорируемых Assets/AmnesiaImported и Logs. Исходники игры не изменялись.


## Итог проверок главы 3

Дата: 2026-10-16. Начальное git status было чистым; чужих изменений не было. Новых механик/API не добавлено. Источники и установленная игра не изменялись, коммит/пуш не выполнялись.

Сверка `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch03 -Map 29_orb_chamber -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/OrbChamber.cs`: **0 ошибок, 0 предупреждений**. Компиляция подтверждена Unity-прогонами. Независимое сравнение токенов после перечисленных адаптаций C# не нашло различий; все исходные комментарии присутствуют (`Logs/Ch03_TokenReview.txt`, воспроизводимый диагностический Ch03_TokenReview.py).

Проверены 78 используемых API: все имена и сигнатуры существуют. Полная таблица актуальных сигнатур/файлов и ограничений — `Logs/Ch03_API_Inventory.md`. Существенные заглушки и пустые операции:

| API | Фактический статус |
|---|---|
| `AutoSave` | ✓ (2026-10-17) |
| `ChangePlayerStateToNormal` | пустая операция |
| `PlayPropAnimation` | NotPorted / условная реализация |
| `PreloadSound` | пустая операция |
| `SetNPCFollowPlayer` | NotPorted / условная реализация |
| `StartCredits` | NotPorted / условная реализация |

Все отчёты далее — внутри `UnityProj/UMnesiaRTX/Logs/`, вне git. PlayerSimulation: `-batchmode -quit`, GPU, `-seed 1`, шаг 1/60; импорт/аудит — `-batchmode -nographics -quit`. Ни один отчёт с WARNING [Script] ERROR не трактуется как чистый скрипт только из-за 0 errors.

| Отчёт | Итог |
|---|---|
| `Ch03_Import_29_orb_chamber.log/.txt` | ExitCode 0; 7 warnings, 0 errors |
| `Ch03_Sim_29_agrippa.log/.txt` | ExitCode 0; 11 warnings, 0 errors |
| `Ch03_Sim_29_agrippa_portal.log/.txt` | ExitCode 0; 9 warnings, 0 errors |
| `Ch03_Sim_29_alexander.log/.txt` | ExitCode 0; 11 warnings, 0 errors |
| `Ch03_Sim_29_alexander_natural.log/.txt` | ExitCode 0; 9 warnings, 0 errors |
| `Ch03_Sim_29_daniel.log/.txt` | ExitCode 0; 7 warnings, 0 errors |
| `Ch03_Sim_29_orb_chamber_start.log/.txt` | ExitCode 0; 4 warnings, 0 errors |
| `Ch03_Sim_29_portal.log/.txt` | ExitCode 0; 6 warnings, 0 errors |
| `Ch03_Audit_29_orb_chamber.log/.txt` | **ExitCode 1, общий аудит не PASS**: Models=131, submeshes=310, geometryFailures=0, prefabMatrixDifferences=6, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=2; Scene: instances=681, submeshMatrices=1153, sharedMeshColliders=491, failures=14, maxMatrixError=50,0519; Resource regression: checks=471, failures=0 |

14 scene failures: 6 missing submesh имеют точные SkinnedMeshRenderer; 8 placement-сообщений относятся к повторяющимся cellar_wood01_4 (ID 783/971) и chair_nice01_1 (ID 993/1386). У всех четырёх сопоставлены hplId, FileIndex/.ent, позиции и вращения; объекты стоят согласно своим исходным ID. Аудитор выбирает первое совпадение имени. Данные: Ch03_DuplicateReview.json. Общий аудит остаётся ExitCode 1.

### Точные диагностические fixtures

Базовая команда: `Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch03/29_orb_chamber.map -seed 1 -script "<команды>" -logFile <Logs/Ch03_имя.log>`. Флаг scriptDebug включается только там, где указан; глобальные переменные передаются до OnStart. Скриптовые выдачи предметов/снятие таймеров/callbacks — подготовка диагностики, не изменения порта и не доказательство физического решения.

`Ch03_Sim_29_portal`: scriptDebug=true, globals=`(нет)`.

```text
call RemoveTimer TimerAlexanderStartSpeak; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call UseHeadOnPortal agrippa_head_1 AreaPortalInteractArea; watch 10 2; call StartPortalCountDown; call RemoveTimer PortalCountDowne; watch 10 2; call TimerPortalCountDown PortalCountDowne; call RemoveTimer PortalCountDowne; call RemoveTimer StartPortalDoneVoice; watch 10 2; call TimerPortalCountDown PortalCountDowne; call RemoveTimer PortalCountDowne; call RemoveTimer StartPortalDoneVoice; watch 10 2; call TimerPortalCountDown PortalCountDowne; call RemoveTimer PortalCountDowne; call RemoveTimer StartPortalDoneVoice; watch 10 2; call TimerPortalCountDown PortalCountDowne; call RemoveTimer PortalCountDowne; call RemoveTimer StartPortalDoneVoice; watch 10 2; call SetPortalPhase 0; watch 10 2; call UseHeadOnPortal agrippa_head_1 AreaPortalInteractArea; watch 10 2; call InteractActivePort AreaPortalInteractArea; watch 10 2; call ResetPortalCountDown; call GetLocalVarInt PortalCountDownState; call SetLocalVarInt InterruptAlexanderCount 3; call InterruptAlexander; call GetLocalVarInt InterruptAlexanderCount; watch 10 2; start PlayerStartArea_1; w 1; s 1; shot portal
```

`Ch03_Sim_29_alexander`: scriptDebug=false, globals=`(нет)`.

```text
call RemoveTimer TimerAlexanderStartSpeak; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call StartAlexanderEnding; watch 240 10; call GetLocalVarInt AlexEndState; call GetLocalVarInt AlexanderEnding; shot alexander
```

`Ch03_Sim_29_agrippa`: scriptDebug=true, globals=`(нет)`.

```text
call RemoveTimer TimerAlexanderStartSpeak; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call CollideAreaHead Player AreaHead_1 1; call SetLocalVarInt PortalState 2; call UseHeadOnPortal agrippa_head_1 AreaPortalInteractArea; watch 100 10; call GetLocalVarInt AgrippaEndState; call CollideAgrippaEnding1 Player AgrippaEnding_1 1; watch 15 5; call CollideAgrippaEnding2 Player AgrippaEnding_2 1; watch 15 5; call CollideAgrippaEnding3 Player AgrippaEnding_3 1; watch 120 10; call GetLocalVarInt AgrippaEndState; call GetLocalVarInt AgrippaEnding; shot agrippa
```

`Ch03_Sim_29_daniel`: scriptDebug=false, globals=`(нет)`.

```text
call RemoveTimer TimerAlexanderStartSpeak; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call RemoveEntityCollideCallback orb_room_rod_1 AreaRodTippingPoint_1; call CollideRodTippingPoint orb_room_rod_1 AreaRodTippingPoint_1 1; watch 10 2; call RemoveEntityCollideCallback orb_room_rod_2 AreaRodTippingPoint_2; call CollideRodTippingPoint orb_room_rod_2 AreaRodTippingPoint_2 1; watch 10 2; call RemoveEntityCollideCallback orb_room_rod_3 AreaRodTippingPoint_3; call CollideRodTippingPoint orb_room_rod_3 AreaRodTippingPoint_3 1; watch 10 2; watch 240 10; call GetLocalVarInt InterruptAlexanderCount; call GetLocalVarInt DanielEndState; call GetLocalVarInt DanielEnding; shot daniel
```

`Ch03_Sim_29_alexander_natural`: scriptDebug=false, globals=`(нет)`.

```text
watch 360 20; call GetLocalVarInt PortalCountDownState; call GetLocalVarInt AlexEndState; call GetLocalVarInt AlexanderEnding; shot natural_alexander
```

`Ch03_Sim_29_agrippa_portal`: scriptDebug=true, globals=`(нет)`.

```text
call RemoveTimer TimerAlexanderStartSpeak; call SetEffectVoiceOverCallback; call StopAllEffectVoices 0; call CollideAreaHead Player AreaHead_1 1; call SetPortalPhase 0; watch 10 2; call SetPortalPhase 1; watch 10 2; call UseHeadOnPortal agrippa_head_1 AreaPortalInteractArea; watch 100 10; call GetLocalVarInt AgrippaEndState; call CollideAgrippaEnding1 Player AgrippaEnding_1 1; watch 15 5; call CollideAgrippaEnding2 Player AgrippaEnding_2 1; watch 15 5; call CollideAgrippaEnding3 Player AgrippaEnding_3 1; watch 120 10; call GetLocalVarInt AgrippaEndState; call GetLocalVarInt AgrippaEnding; shot agrippa
```

### Предупреждения прогонов

- [Script] ERROR: Could not find sound entity 'mono_sound1'
- [Script] ERROR: Could not find sound entity 'mono_sound2'
- [Script] Entity 'monolith01_2' with type Prop and subtype any does not exist!
- [Script] AutoSave: skipped, needs saving (step 6)
- [Script] SetNPCFollowPlayer: skipped, needs NPCs and the man pig (step 6)
- [Script] PlayPropAnimation: skipped, needs prop animations
- [Script] ERROR: Could not find sound entity 'PortalOpenened'
- [Script] ERROR: Could not find sound entity 'PortalOpens'
- [Script] ERROR: Could not find sound entity 'PortalOpensd'
- [Script] ERROR: Could not find particle system 'PortalOpen'
- [Script] StartCredits: skipped, needs credits (step 6, menus)
- [Script] ERROR: Could not find sound entity 'orb_room_rod_2sound'
- [Script] ERROR: Script call without a function name

Это сохранённые исходные обращения, недостающие механики или явно указанная диагностическая подготовка. Для одноразовых/уже завершённых звуков и PS отсутствие записи при Stop/Destroy не трактуется как отсутствующий ресурс. Полный порядок сообщений и состояния находятся в соответствующих .txt. Исключений, PortError, takes N arguments и отсутствующих функций порта не обнаружено; исходные отсутствующие callbacks перечислены отдельно.

### Общие регрессии и границы готовности

До изменений: Ch03_Baseline_MapChange.log — **66 PASS**; Ch03_Baseline_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS**. После всех шести портов: Ch03_Regression_MapChange.log — **66 PASS, ExitCode 0**; Ch03_Regression_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS, ExitCode 0**. Обе Play-регрессии запускались без -quit/-nographics и завершили процесс самостоятельно. Тесты не менялись.

Финалы: естественный Alexander прошёл весь countdown/портал и достиг AlexEndState=13 (StartCredits в t≈225.47); Agrippa с открытым порталом достиг AgrippaEndState=12; три наклонённые колонны привели к InterruptAlexanderCount=3, DanielEndState=18. Отдельные чистые fixtures, endNum 0/1/2 сохранены по исходным вызовам. **Реальные титры и анимации NPC не реализованы**, поэтому полного завершённого игрового финала не заявляется.

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
