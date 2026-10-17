# Justine — профиль, карты и проверки

**Перенос четырёх карт завершён, 2026-10-17.** 327 функций, сверка/компиляция и сценарии разобраны, Play-маршрут 57 PASS, обе основные регрессии прошли. Полная проходимость ограничена перечисленными ниже механиками. При возобновлении рабочее дерево было чистым, HEAD `c4064fe`: предыдущая работа уже сохранена вместе с отдельным этапом сохранений. Историческая точка паузы: `UnityProj/UMnesiaRTX/Logs/Justine_Resume.md`.

Дата: 2026-10-17. Статус J0: принят для переноса карт; выход из Play после смерти принят отдельным интеграционным прогоном.

Журналы: [01_cells](01_cells.md), [02_library](02_library.md), [03_dungeon](03_dungeon.md), [04_final](04_final.md). Для запуска открыть `Assets/AmnesiaImported/Levels/main_menu/ptest/ptest_menu.unity` и Play; новая игра выберет Justine. Начальная сцена сборки остаётся главным меню основной игры.

Исходный план: `docs/tasks/codex_justine_maps.md`. Заказан весь перенос Justine, поэтому подготовка J0 выполняется перед четырьмя картами. Исходное рабочее дерево чистое, UnityLockfile отсутствует. Исходные ресурсы игры читаются; разрешённое исключение — воспроизводимое добавление недостающего `ptest_materials.cfg` в игнорируемую копию `source`.

## Область подготовки до изменения кода

- `tools/Copy-AmnesiaResources.ps1`: недостающий корневой ресурс.
- `tools/Check-ScriptPort.ps1`: явный `-MapRoot maps/ptest`, прежние `-Chapter` сохраняются.
- Новый `Assets/Scripts/Hpl/HplCampaign.cs`: неизменяемый контекст по пути карты, отдельные старт/меню/ресурсные имена.
- `Assets/Scripts/Hpl/HplLanguage.cs`: идентичность кампании языковых данных.
- `Assets/Editor/Importer/{ConfigConverter,LanguageConverter,SurfaceConverter,MenuConverter,InventoryConverter,JournalConverter,LoadScreenConverter,SoundConverter,FontConverter,LevelConverter,MainMenuConverter}.cs`: раздельные конфиги, физические материалы, языки, звуковые банки, шрифты и UI-данные; выбор профиля по исходному пути карты. Общие модели/текстуры остаются кэшируемыми.
- `Assets/Scripts/ScriptApi/{HplMap,HplGame,HplScript.Player}.cs`: профиль карты, отсутствие основных рецептов/авточекпоинта в Justine, звук perma death.
- `Assets/Scripts/Ui/{HplInventory,HplJournal,HplJournalView,HplPauseMenu,HplLoadScreen,HplMenuScreen}.cs`: ресурсные имена и переходы меню своей кампании.
- `Assets/Scripts/Player/PlayerHurt.cs`: отдельный исходный режим perma death без возрождения; английская реплика и завершение приложения (в Editor — выход из Play). Основная смерть сохраняется.
- `Assets/Scripts/Player/AmnesiaPlayer.cs`: перенос выбранного perma-death звука между сценами, как у общего игрока HPL2. `Assets/Scripts/Hpl/HplConfig.cs`: сохранение исходного XML вложенных pre-menu секций для отдельного контекста.
- Новый `Assets/Editor/Dev/JustineProfileTests.cs` и meta новых файлов: приёмка контекста, русского текста/английского голоса, отсутствия основных рецептов, сохранности GUID/содержимого основных ассетов, checker и режима смерти.
- `ProjectSettings/EditorBuildSettings.asset`: ожидаемые добавления сцен, главное меню основной игры остаётся первым.
- `docs/ScriptPort/api.md`: только действительно реализованный API подготовки. `AGENTS.md`/`CLAUDE.md`: одна синхронная строка этапа 5 после завершения карт.

Пути в списке Assets относятся к `UnityProj/UMnesiaRTX`. Импортированные данные, временные fixtures и логи — вне git. Старые порты, их журналы и планы не меняются.

## Критерии приёмки

Обе исходные регрессии сняты до изменений. После J0: основная кампания проходит прежние проверки; Justine использует config/ptest_*, ptest_materials.cfg, config/lang_ptest/russian.lang с английским fallback и lang/eng/ptest, отдельные GUID и ресурсы UI; основные рецепты отсутствуют; основной набор ассетов не перезаписан. Штатный checker поддерживает maps/ptest и прежние ch01/ch02/ch03.

Финальные титры и ARG AddKeyPart работают с 2026-10-17 (CLAUDE.md «Титры, NPC, анимации, прикреплённые пропы и критеры»): код концовки в Justine не показывается, части ключа хранятся (их проверка в HPL2 закомментирована). Входная последовательность pre-menu сохраняется как отдельный исходный конфиг; отображение приветственных слайдов отсутствует и у основной кампании.

`source/maps/ptest/global.hps` и `inventory.hps` содержат только OnGameStart с комментарием; исполняемых операций нет. Отдельные фиктивные скрипты не создаются, основной InventoryScript не подменяется.

## Результаты J0

- Исходные регрессии: `Logs/Justine_Baseline_MapChange.log` — 66 PASS; `Logs/Justine_Baseline_MainMenu.log` — 27/50/36/14/14 PASS, оба ExitCode=0.
- `Logs/Justine_J0_Profile_Retry.txt`: 41 PASS, ExitCode=0. Проверены 11 основных ассетов побайтно и по GUID до/после импорта DLC. Первый запуск выявил поиск логотипа без вложенной папки ptest; поиск исправлен в рамках J0. Исходный неуспешный отчёт сохранён в `Logs/Justine_J0_Profile.log` (ExitCode=1).
- `Logs/Justine_J0_Map_Lethal.txt`: 10 PASS, ExitCode=0. Настоящий смертельный урон, английская alois_death_01, ожидание конца записи, terminal state=4, health=-400, без чекпоинта/возрождения. Два предыдущих запуска ошибочно использовали несмертельный флаг урона; исправлен только fixture (Logs/Justine_J0_Map*.log). Выполнена симуляция в Edit mode, не заявляется проверка Application.Quit в сборке.
- После подготовки: `Logs/Justine_J0_MapChange.log` — 66 PASS; `Logs/Justine_J0_MainMenu.log` — 27/50/36/14/14 PASS, ExitCode=0.
- Старые команды checker на RainyHall/ch01, Cistern/ch02, CellTunnels/ch03 — 0 ошибок/предупреждений (`Logs/Justine_Check_Legacy_*.txt`). PowerShell-скрипты сохранены в UTF-8 с BOM для Windows PowerShell 5.1. Новая команда: `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -MapRoot maps/ptest -Map 01_cells -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Justine/JustineCells.cs`; полная сверка DLC выполняется после появления порта J1.
- ptest_materials.cfg скопирован только в игнорируемый source. SHA256 обеих копий: `24515880D8E522FAA579760D276A0F1BC910EE6CC1CC82440E2DC137C9D124CB`.

Контекст выбирается исходным путём карты `maps/ptest/...` или `main_menu/ptest/...`; скрытого глобального переключателя нет. Меню: Tools > RTXMnesia > Import > Justine menu and build scenes, batch `RTXMnesia.Importer.MainMenuConverter.ConvertJustineBatch`. Его сцена — `Assets/AmnesiaImported/Levels/main_menu/ptest/ptest_menu.unity`; Play этой сцены выбирает новую кампанию Justine. Начальная сцена сборки основной игры сохранена.

Ограничение врагов: существующий EnemyConverter обходит entities/enemy, а сущности Suitor находятся в entities/ptest/enemy_suitor. Модель и Enemy_Grunt-параметры читаются импортом уровня, но отдельные анимационные определения Suitor в общий банк не попадают. В J0 ИИ/импортёр врагов не дописывался; активация и скриптовые патрули не выдаются за полную проверку погони и атак.

## Сверка четырёх карт

Перенесены 81 / 50 / 92 / 104 функции, всего 327. Фактические .cs побайтно совпадают с reviewed drafts. Независимая сверка: 26 891 токен после разрешённых синтаксических замен, 980 исходных комментариев, все hex-литералы с точными 32 битами и порядком (`Logs/Justine_TokenReview.txt`). Штатный checker всех четырёх карт: 0 ошибок; предупреждения 1 / 0 / 2 / 1 — отсутствующие исходные callbacks InteractMe, LookAtLurky, PuzzleSliderDoor03_SouthernDoorValveState, InteractSecondDoor (`Logs/Justine_Check_Final_*.txt`).

Во всех оригиналах CharacterTalkOver(float) назначается окончанием голоса без аргументов. Оригинальный `src/AmnesiaTheDarkDescent/amnesia/src/game/LuxEffectHandler.cpp:1086` и `:1141` вызывает callback с (). Сигнатуры и регистрации сохранены: сценарии с этой ERROR не считаются чистым PASS. Игнорируемый runner отдельно классифицирует только это точное сообщение; любые другие ERROR останавливают очередь. Набор тестов проекта не изменён.

## Восемь комбинаций пленников

Каждая строка — свежий Unity-процесс, seed=1, globals заданы до OnStart, затем BeginPrisonerVoices и watch 70 1. H/V/F — HectorIsDead/VictorIsDead/FelixIsDead. Наблюдался именно порядок voice-файлов, а не только отсутствие исключений. `Logs/Justine_MatrixReview.txt`: 8 совпадений с вручную разобранным исходником. Стартовые предупреждения final о данных карты при этом остаются.

| H/V/F | Наблюдавшаяся очередь |
|---|---|
| 000 | victor_saved_01 → hector_saved_02 → felix_saved_01 |
| 001 | victor_saved_02 → hector_saved_02 |
| 010 | hector_saved_02 → felix_saved_01 |
| 011 | hector_saved_02 |
| 100 | victor_saved_02 → felix_saved_01 |
| 101 | victor_saved_03 |
| 110 | victor_saved_02 → felix_saved_01 |
| 111 | alois_attack_01 → malo_idle_01 (seed=1, выбор из исходных диапазонов 0…2 и 3…5) |

В 110 исходное условие medium Victor содержит &&/|| без общих скобок: реплика звучит при VictorIsDead=1. Это поведение сохранено. Матрица не упрощалась до количества выживших.

## Геометрия и контрольные проходы

Все четыре аудита имеют **ExitCode=1, общего PASS нет**. Во всех: geometryFailures=0, missingCaches=0, maxVertexError=0; выбор ресурсов 471/0. PLACEMENT ошибок нет, максимальное отклонение матриц сцены около 7.63e-6.

| Карта | Модели / подмеши | prefabMatrixDifferences | scene failures |
|---|---:|---:|---:|
| 01_cells | 179 / 307 | 18 | 10 |
| 02_library | 190 / 360 | 19 | 7 |
| 03_dungeon | 107 / 197 | 4 | 16 |
| 04_final | 168 / 250 | 19 | 1 |

`Logs/Justine_RendererReview.json`: 10/7/14/1 отсутствующих для MeshFilter имён присутствуют как SkinnedMeshRenderer. Ещё два имени dungeon — повторные polySurfaceShape1 исходной .ent (SubMeshID=0/2): соответствующие разные .dae geometry имеют одно отображаемое имя, Unity переименовала их в polySurfaceShape1 1/2. GUID и mesh fileID сохранены. Это подтверждение наличия/пути, а не численная сверка скелетных вершин. Расхождения матриц префабов остаются явными.

Просмотрены стартовые снимки всех карт и дополнительные light_box/reading_room, machinery/chase_passage, loft, awakened/final_passage. graveyard закрыт исходным белым emotion-text overlay и не использован для подтверждения геометрии. Проверены короткие движения из исходных PlayerStart, полного пешего маршрута нет.

Лестницы cells: первая попытка с PlayerStartArea_3 потеряла высоту до нажатия interact. Два уточнённых сценария (ручная лестница и RollDownLadder) завершились без C#-исключений, но состояние осталось Normal; подъём/спуск **не подтверждены**. Во время DX12-повтора Unity упала на ReadPixels с GPU device error 887a0006, ExitCode=1073741845. Аварийный лог `Logs/Justine_01_cells_ladder_attach_retry.log` сохранён; оба самостоятельных DX11-повтора завершились ExitCode=0. Код движка/геометрии ради тестов не изменялся.

## Границы готовности

Полный перенос скриптов не равен полностью проходимой Justine. Титры, AddKeyPart, PlayPropAnimation, interact connections и верёвки работают с 2026-10-17; анимации Suitor и указанные лестничные маршруты остаются ограничениями. Исходные ссылки final разобраны: три graveyard_corpse в .map — Object/Static вместо ожидаемого Enemy, 50 из 96 AreaSpark_N отсутствуют, 16 torch_static01_N из цикла остановки звука отсутствуют. Порты эти данные не исправляют.

После отдельного этапа сохранений (уже в HEAD при возобновлении) пауза также предлагает «Сохранить и выйти» для Justine. Оригинальный `LuxMainMenu.cpp:1208` исключает ptest из этой команды. Этот разрыв UI не исправлялся в задаче карт; ручное сохранение/загрузка DLC здесь не принимались. Исходные карты не получают дополнительных AutoSave/CheckPoint, проверяемый режим смерти не возрождает игрока.

## Сценарии и воспроизводимость

`Logs/Justine_Scenarios.json`: 35 основных сценариев; `Logs/Justine_ExtraScenarios.json`: дополнительные проходы/фонографы/лестницы и аварийный повтор. Всего 42 попытки: 41 штатный ExitCode=0, один native GPU crash DX12. Семь штатно завершённых сценариев содержат по одной ERROR CharacterTalkOver (cells lever_kill/prisoner_topics, library save/kill/torture, dungeon felix_eaten_chase/phono_talk); это не 41 чистый PASS. C#-исключений в отчётах симуляции нет. Вложенные WARNING [Script] ERROR также разобраны в журналах карт, а не потеряны за счётчиком errors=0.

Каждый сценарий — свежий Unity-процесс, seed=1, точные -script/-globals/scriptDebug сохранены рядом в `.scenario.txt`; полные команды приведены в журналах. Два последних лестничных повтора использовали -force-d3d11 после подтверждённого DX12 device error. Итоговый разбор — `Logs/Justine_Results.json`; все неуспешные логи сохранены.

Подтверждены разные исходы испытаний: VictorIsDead=1 после трёх рычагов; ManualLadderPlaced=1 и VictorIsDead=0 в отдельном ручном решении; HectorIsDead=0/1 для stencil 4+3 / неверной комбинации, TortureCount=4; FelixIsDead=0 при закрытой двери пленника и =1/FelixIsEaten=1 в ветке гибели. Погоня при FelixIsEaten=1 по оригиналу не запускается, а отдельный fresh chase_saved даёт ChaseStarted=1. Ремонтные скрипты дали LeverFixed=1 и MachineFixed=1, но ручной Sticky callback не доказывает физическое прикрепление шестерни.

Через SetWheelStuckState(..., true) проверены настоящие callbacks фонографов: cells lvl1/01 (4 части), library lvl2/01 (2), dungeon lvl3/01 (3), final lvl4/01 (6) и lvl3/02 (3). Очередь, русский текст в watch, длительности и завершение записаны в отчётах; PlayPhono возвращается к 0 на первых трёх картах. Голоса куклы в final дополнительно проходят через исходный crush-сценарий. HasAptNote=0/1 проверены отдельно: proper / patrol очереди различаются, обе доходят до TimerEEnding=12. Player→AreaUpstairs и последующие move/TimerEEnding сняты только в fixture: StartCredits изолирован.

Инициализация флагов соответствует исходнику: cells OnStart ставит Victor=0; library OnStart Hector=0; dungeon OnStart Felix=1 (меняется на 0 при спасении). Поэтому проверка переноса учитывает уже пройденные испытания и собственную инициализацию следующей карты.

## Интеграционный Play-маршрут

`Logs/Justine_PlayRoute_SourceInit.log/.exit.txt` и `Logs/Justine_PlayRoute/Report.txt`: **PASS: 57, Unity ExitCode=0**, DX11, без -quit/-nographics. Маршрут: DLC-меню → NewGame/подтверждение → 01_cells (полное исходное вступление) → level_wood_1 → 02_library → level_celler_1 → 03_dungeon → level_dungeon_2 → 04_final → ReturnTitle/подтверждение → своё DLC-меню → новая попытка → настоящая смерть → выход из Play после alois_death_01.

Двери вызваны через настоящий PropLevelDoor.OnInteract с исходными историческими путями .map; между дверями это проверка загрузок, не пешее прохождение. Проверены свои game/menu/language на всех посещениях, отсутствие основных рецептов, перенос выданного stencil-предмета, флаги завершённых испытаний с учётом OnStart, наследование точного malo_death.snt в final, отсутствие авточекпоинта. Флаги исходов задавались диагностикой после соответствующей карты; сами save/kill-обработчики проверены отдельными свежими сценариями.

Возврат/новая игра сбросили инвентарь, global/local marker, старый таймер и голос; вступление запустилось заново. Летальный GivePlayerDamage вызвал штатный perma death: английская запись проиграна и дождалась завершения, terminal state=4, без восстановления checkpoint. Подтверждён Editor exit из Play; Application.Quit в отдельной сборке не тестировался.

Первый helper не учёл, что UI меню находится в HplMainMenu.Ui, и был остановлен (Logs/Justine_PlayRoute.log, ExitCode=-1). Следующий выявил Scene unavailable; штатный ConfigureBuildScenes обновил доступный Unity список, дополнительно в Windows.asset добавлены только 5 сцен Justine, прежние 7 записей и старт основной игры сохранены. Ещё два fixture-ожидания не учитывали исходное обнуление Hector / исходное Felix=1; исправлены только ожидания, не порт. Все отчёты сохранены как Report_MenuUnavailable/FlagFixture/FelixDefault.txt и одноимённые PlayRoute_Retry/Configured/Flags.log. Эти попытки не объявляются PASS.

Временный helper находился в игнорируемом Assets/AmnesiaImported/Editor, после полного выхода Unity удалён вместе с .meta. Его итоговый исходник сохранён в `Logs/JustineVerification.cs`. Код движка и ранее готовые карты в возобновлённой части не менялись.

## Финальные регрессии и состояние файлов

`Logs/Justine_Final_MapChange.log` и `Logs/Justine_Final_MainMenu.log`: оба Unity ExitCode=0, без -quit/-nographics, -force-d3d11. Фактические строки:

```text
PASS: 66 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian, damage and death); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 19 main menu model checks (localization, controls, confirmation, preferences, live world, continue / load game, build scene order).
PASS: 23 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game, exit and save, continue, load game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

Числа main menu 19/23 выросли после отдельного этапа сохранений относительно исторических 14/14. Тесты и ожидания не понижались и не менялись в этой работе. После всех прогонов снова выполнены штатный checker четырёх карт (0 ошибок) и независимая сверка фактических .cs (26891 токен / 980 комментариев, точное совпадение).

В возобновлённой части изменены только 4 новых журнала, этот README, пять добавлений сцен в Windows build profile и одна синхронная строка этапа 5 AGENTS.md/CLAUDE.md. Код J0/карт уже находился в HEAD и остался побайтно прежним; это подтверждено снимком Logs/Justine_Resume_SourceHashes.json. Собственные автоматические пересохранения TimeManager/DynamicsManager возвращены после полного выхода Unity. Ресурсы/сцены/логи/helper вне git; helper и .meta удалены. Коммитов/push в этой работе нет.
