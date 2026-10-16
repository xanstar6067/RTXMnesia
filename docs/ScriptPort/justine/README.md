# Justine — подготовка профиля (J0)

**Пауза по просьбе пользователя, 2026-10-17.** Все 4 порта созданы (327 функций), но перенос ещё не принят целиком: выполнены 8 из 35 сценариев, переходы/финальные регрессии и журналы карт остаются. Unity полностью завершён. Точка продолжения и подробное состояние: `UnityProj/UMnesiaRTX/Logs/Justine_Resume.md`. Не запускать работу до явного продолжения от пользователя.

Дата: 2026-10-17. Статус J0: принят для переноса карт; сценарий завершения приложения в Play проверяется отдельно от симуляции.

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

Финальные титры/ARG AddKeyPart не реализуются в J0: существующие вызовы сохраняются с точным статусом NotPorted. Полный финал с титрами не заявляется. Входная последовательность pre-menu сохраняется как отдельный исходный конфиг; отображение приветственных слайдов отсутствует и у основной кампании.

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
