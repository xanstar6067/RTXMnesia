# Проверка трёх концовок — 2026-10-17

Методика: [EndingsCheck.md](EndingsCheck.md). Unity 6000.6.2f1, карта `maps/main/ch03/29_orb_chamber.map`, русский язык, seed 1, шаг 1/60. Начальное рабочее дерево было чистым. Игровой код, оригинальные ресурсы и установленная игра не изменялись; коммита нет.

## Итог

| Концовка | Цепочка от игрового действия | Титры в Play, песня и код | Меню, сброс и новая игра | Итог |
|---|---|---|---|---|
| Александр | PASS: без вмешательства, AlexEndState=13 | PASS: ending_alexander.ogg, tyr299, видна надпись «Конец» | PASS | PASS в проверенном объёме |
| Даниэль | PASS: первая колонна физически, две через разрешённые методикой коллбэки, DanielEndState=18 | PASS: ending_daniel.ogg, lke271, видна надпись «Конец» | PASS | PASS в проверенном объёме |
| Агриппа | PASS: естественное открытие портала, применение головы, AgrippaEndState=12 | PASS (2026-10-18): ending_agrippa.ogg, odn314, видна надпись «Конец»; в прогоне 2026-10-17 надписи на снимке не было | PASS | PASS в проверенном объёме |

Проверка состоит из двух частей, как предусмотрено методикой: `PlayerSimulation` проходит игровую цепочку до настоящего запуска титров; `CreditsPreview` отдельно запускает те же титры в Play через API скрипта. Это не три непрерывных ручных прохождения в Play. Звук проверялся по состоянию музыкального обработчика, без прослушивания.

## Команды воспроизведения

Все пути далее относительно `UnityProj/UMnesiaRTX/`, если не указано иное. Логи и снимки игнорируются git.

Базовая команда симуляции (добавить `-script "<команды ниже>"` и отдельный `-logFile`):

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch03/29_orb_chamber.map -seed 1
```

Титры: без `-quit` и `-nographics`, каждый запуск отдельно при закрытом Unity:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe' -batchmode -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.CreditsPreview.RunBatch -endNum 0 -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/EndingsCheck/Credits0.log
```

Повторить с `-endNum 1` и `-endNum 2`, меняя имя лога. В `Logs/EndingsCheck/RunCredits.ps1` сохранён последовательный запуск всех трёх.

### Александр

```text
watch 300 5; call GetLocalVarInt AlexEndState
```

Логи: `Logs/EndingsCheck/AlexanderRetry.log` и сокращённый `Alexander.txt`; ExitCode 0, 5 предупреждений, 0 ошибок симулятора.

Ключевые события: `Portal Done Voice:1` в t=137.16; `Portal Done Voice:2` в t=153.76; `Start Alexander Ending` и `Achievement 'Sacrifice'` в t=167.32. `Event:0..11` проходят последовательно; в t=225.47 выполняется последний переход к титрам, в t=230 уже `music "ending_alexander.ogg"`; итог `AlexEndState → 13`. Конечный вызов титров: порт `Assets/Scripts/Levels/Ch03/OrbChamber.cs:981`, оригинал `source/maps/main/ch03/29_orb_chamber.hps:967`.

Play: `Credits0.log`, ExitCode 0, 17 проверок PASS. Снимки `Logs/CreditsCheck/0/Scroll.png` и `TheEnd.png` просмотрены; «Конец» и `tyr299` видны.

### Даниэль

`goto` принимает координаты Unity: для первой колонны X=-7, хотя в исходной карте X=7.

```text
goto -7 0 9.1 180; aim orb_room_rod_1; interact; w 4; body orb_room_rod_1; release; watch 8 2; call GetLocalVarInt InterruptAlexanderCount; call RemoveEntityCollideCallback orb_room_rod_2 AreaRodTippingPoint_2; call CollideRodTippingPoint orb_room_rod_2 AreaRodTippingPoint_2 1; watch 15 5; call RemoveEntityCollideCallback orb_room_rod_3 AreaRodTippingPoint_3; call CollideRodTippingPoint orb_room_rod_3 AreaRodTippingPoint_3 1; watch 150 5; call GetLocalVarInt DanielEndState; call GetLocalVarInt InterruptAlexanderCount
```

Логи: `DanielRetry.log/.txt`; ExitCode 0, 3 предупреждения, 0 ошибок. В t=0.05 вход в `InteractPush`; t=1.05 — `Start interruption: 1`; t=2.43 — `Rod hit ground: orb_room_rod_1`. В t=4.05 игрок уже `Normal`, шарнир первой колонны 90°. Частиц стало 114 вместо 117. Исходная логика дополнительного толчка: порт `OrbChamber.cs:471`, оригинал `.hps:457`; свет и частицы выключаются тем же коллбэком, отдельный визуальный замер яркости не выполнялся.

Следующие прерывания в t=12.10 и 27.13; множители 0.3 и 0.25; `Start Daniel Ending` и `Achievement 'Survivor'` в t=35.62. `Event:0..16` проходят по порядку, к t=122.16 звучит `ending_daniel.ogg`; итог `DanielEndState → 18`, `InterruptAlexanderCount → 3`. Вызов титров: порт `OrbChamber.cs:1544`, оригинал `.hps:1530`.

Play: `Credits2.log`, ExitCode 0, 17 проверок PASS; `Logs/CreditsCheck/2/TheEnd.png` просмотрен: «Конец», `lke271`.

Первая попытка (`Daniel.log/.txt`) сделана с неправильным знаком X: фокус оказался на алтаре, первая колонна не упала, в результате сработал Александр. Это ошибка тестового маршрута, не дефект игры; попытка не засчитана.

### Агриппа

Дополнительный флаг `-scriptDebug`. Состояния портала не подменялись. Переходы областей финального коридора выполнены перемещением игрока, коллбэки реплик вручную не вызывались.

```text
call UseHeadOnPortal agrippa_head_1 orb_altar_1; call HasItem agrippa_head_1; until PortalState 1 240; call UseHeadOnPortal agrippa_head_1 orb_altar_1; call HasItem agrippa_head_1; until PortalState 2 30; expectvar AlexanderEnding 0; goto 0 0.1 11.75; idle 0.1; call UseHeadOnPortal agrippa_head_1 orb_altar_1; call HasItem agrippa_head_1; until AgrippaEndState 9 120; goto 27.25 0.2 1.5; watch 12 3; goto 26.5 0.2 8.5; watch 12 3; goto 26.25 0.2 15.75; until AgrippaEndState 12 120; watch 2 1; expectvar AgrippaEnding 1
```

Логи: `AgrippaRetry.log/.txt`; ExitCode 0, 5 предупреждений, 0 ошибок. Ранние сообщения «Портал еще не открылся» и «Портал скоро откроется», `HasItem → True` оба раза. `PortalState=2` в t=150.14; `Start Agrippa Ending`, `Benefactor` в t=150.26; `HasItem → False` в t=150.27. Область головы — `AreaHead_1`. `Event:0..10` проходят по порядку; все три голоса финального коридора присутствуют, последний завершает цепочку. В t=214.34 — `AgrippaEndState=12`, `music "ending_agrippa.ogg"`, затем `PASS: AgrippaEnding=1`. Вызов титров: порт `OrbChamber.cs:1214`, оригинал `.hps:1200`.

Play: `Credits1.log` — 17 автоматических проверок PASS; повтор `Credits1Retry.log` — 18 PASS, оба ExitCode 0. На `Logs/CreditsCheck/1/TheEnd.png` и `TheEndRepeat.png` виден `odn314`, **надписи «Конец» нет**. Повтор подтверждает наличие команды заголовка, 5 глифов `fonts/rus/menu.fnt`, UI-меша из 20 вершин, `culled=False`. Таким образом, успешные программные проверки не закрывают визуальную проверку. Причина пока не локализована: нельзя уверенно отделить дефект UI от ограничения CPU-захвата `HudSnapshot.DrawCanvas`. Игровой код по этому наблюдению не менялся. Место отрисовки заголовка: `Assets/Scripts/Ui/HplCredits.cs:265`.

Первая попытка `Agrippa.log` остановилась на неверной проверке `AlexanderEnding`: отсутствующая целочисленная переменная хранится пустой строкой, но API читает её как 0. Исправлена проверка в симуляторе, затем выполнен полный повтор. Это исключение тестового инструмента, не игровой цепочки.

## Повторная проверка 2026-10-18

`CreditsPreview.RunBatch -endNum 0/1/2` заново, логи `Logs/EndingsCheck/Credits{0,1,2}_2026-10-18.log`: все три — ExitCode 0, **18 проверок PASS**. На `Logs/CreditsCheck/1/TheEnd.png` и `TheEndRepeat.png` (Агриппа) видны и «Конец», и `odn314`; у Александра и Даниэля — так же. Отсутствие надписи в прогоне 2026-10-17 не воспроизводится (игровой код титров не менялся; снимки того прогона перезаписаны, причину установить уже нельзя — вероятнее всего, CPU-захват). **Визуальная проверка финала Агриппы закрыта.**

Попутно исправлен пробник (`HplCreditsPreviewProbe`): после добавления сложного режима новая игра идёт через окно выбора режима (`StartGameStart`), старый шаг «новая игра → подтверждение» падал на проверке «new game accepted after ending». Игровой код не менялся.

## Общие проверки титров

Все три варианта подтвердили `HplCredits.Active`, 126 строк текста, остановку карты и `timeScale=0`, блокировку ввода игрока, скрытие HUD, нужную песню, прокрутку при неизменном времени карты, затухание музыки у конца текста, переход к конечному экрану, код, выход в главное меню с `timeScale=1`. Проверены удаление титров, очистка тестовой глобальной переменной и буфера `AddKeyPart`. После каждой концовки новая игра через меню действительно загрузила `00_rainy_hall` без старой тестовой переменной.

Пропуск раньше 3 секунд проигнорирован; конечный экран можно пропустить сразу — это оставлено как в оригинале. Использовался `ExitPressed()`: физические Esc/пробел/Tab/ЛКМ/ПКМ по отдельности и пропуск середины прокрутки после 3 секунд не проверены. Прокрутка после первых секунд ускорена через `Step`, целиком в реальном времени песня не прослушивалась. Ручное применение головы через интерфейс инвентаря не проверялось.

## Автосохранение и повреждения

Автосохранение `OnEnter` первого прогона скопировано в игнорируемый `Logs/EndingsCheck/orb-autosave.sav`. Дополнительный параметр к базовой команде: `-loadSave Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/EndingsCheck/orb-autosave.sav`.

```text
health; call SetPlayerHealth 5; call CollideFireDamage Player AreaFireDamage_1 1; health; watch 30 5; health; call SetPlayerHealth 5; call InteractActivePort AreaPortalInteractArea; health; watch 30 5; health; until AlexEndState 13 300; watch 2 1; expectvar AlexanderEnding 1
```

`LoadedDamage.log/.txt`: ExitCode 0, 8 предупреждений, 0 ошибок. В логе `LOADING SAVE GAME!`, восстановлено здоровье 100 и чекпойнт `_auto` / `PlayerStartArea_1`. После обоих воздействий здоровье около 10.01, `dead False`; к t=225.49 — `AlexEndState=13`, песня Александра, `PASS: AlexanderEnding=1`. **Загрузка и завершение после неё — PASS. Проверка смерти не состоялась**, поскольку оба исходных воздействия нелетальные.

Флаг летальности: порт `OrbChamber.cs:689`, `OrbChamber.cs:1716`; оригинал `.hps:675`, `.hps:1702`; контракт `abLethal` — `src/AmnesiaTheDarkDescent/amnesia/src/game/LuxScriptHandler.cpp:499`. Минимум 10 HP сохраняет `Assets/Scripts/Player/AmnesiaPlayer.cs:919`. Поведение не изменено. Урон вызван коллбэками, без физического входа во все три области огня.

В загруженном прогоне также появились предупреждения о `29_monolith_0`, `29_monolith02_0` и пустом имени коллбэка таймера портала. Они не остановили концовку. Звуки `_0` запрашивает `TimerPlayMSound` при начальном значении поля `g_iSound`: порт `OrbChamber.cs:1687`, оригинал `.hps:1670`. Это отдельное наблюдение о восстановлении фонового звука, не доказательство отсутствия всех звуков и не PortError.

## Уточнения методики и границы результата

1. Нельзя применить голову при полностью открытом портале **до начала** второй фразы: открытие и запуск фразы находятся в одном коллбэке `TimerDelayOpenPortal` — порт `OrbChamber.cs:571`, оригинал `.hps:557`. Рабочее окно — во время второй фразы, до её завершения и `AlexanderEnding=1`. Именно это окно проверено.
2. Огонь и портал нелетальны; требование смерти от них расходится с исходными вызовами. Принудительная смерть другим API не подменяла эту проверку.
3. Ожидаемые предупреждения `mono_sound1/2`, `monolith01_2`, `orb_room_rod_2sound`, `PortalOpens` сохранены. В успешных игровых прогонах C#-исключений и PortError не обнаружено. Системные сообщения лицензирования и GetVirtualKey отделены от ошибок порта.
4. Поворот головы Александра к смещённому игроку виден на просмотренных `Logs/Player_29_orb_chamber_00_ending_npc_head_front.png` и `01_ending_npc_head_side.png`; точность угла не измерялась. Команды GPU-прогона (без `-nographics`): `goto 0 3 4 180; idle 0.5; pitch -22; shot ending_npc_head_front; goto -2 3 4 145; idle 0.5; pitch -22; shot ending_npc_head_side`. Лог `Logs/EndingsCheck/NpcHeadVisual.log`, ExitCode 0. Предыдущие кадры `ending_npc_front/side` и `ending_npc_close_front/side` не засчитаны: алтарь заслонял NPC либо камера смотрела на основание модели. Сквозного ручного наблюдения анимаций Release/Death в Play нет; подтверждено прохождение соответствующих ветвей без исключений.

Изменены только инструменты проверки и документы: параметр `-endNum`, проверки кода/сброса/новой игры и диагностика заголовка; в симуляторе — `-loadSave`, `until`, целочисленный `expectvar`. Настройки проекта после batch восстановлены из копии, сделанной до первого запуска. Все игровые ресурсы, сохранения и снимки остались вне git.
