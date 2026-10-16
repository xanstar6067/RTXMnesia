# 07_archives_cellar

- **Оригинал:** `source/maps/main/ch01/07_archives_cellar.hps` (Latin-1, 427 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/ArchivesCellar.cs` (класс `RTXMnesia.Levels.Ch01.ArchivesCellar`, `[HplMapScript("maps/main/ch01/07_archives_cellar.map")]`).
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/07_archives_cellar.unity` (генерируется, вне git).
- **Статус:** все 30 функций перенесены; проверены старт, десять основных событий, связь рычага с первыми воротами, таймер и по три настоящих смерти/возрождения на каждом чекпоинте. Полное прохождение ограничено отсутствием связи колеса со вторыми воротами; водяные монстры перенесены (6.4). Обе обязательные регрессии прошли, включая обновлённые 66 проверок переходов/урона/смерти. Дата проверки: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в оригинальном скрипте нет. Все методы ниже — в классе `ArchivesCellar`, в исходном порядке.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `CollideActiveWaterLurker` (:5) | `CollideActiveWaterLurker` | готов | первый монстр и три таймера реакций |
| `CollideActiveWaterLurker2` (:14) | `CollideActiveWaterLurker2` | готов | альтернативный первый монстр и реакции |
| `CollideTeleportToDarkSide` (:26) | `CollideTeleportToDarkSide` | готов | затемнение, звук, громкость, вспышка, FOV и таймеры переноса |
| `TimerPlayerReact` (:55) | `TimerPlayerReact` | готов | все три варианта реакции |
| `TimerTeleportHowl` (:68) | `TimerTeleportHowl` | готов | вой, отключение управления и включение тумана |
| `TimerTelportDone` (:75) | `TimerTelportDone` | готов | исходное имя с опечаткой, телепорт и восстановление эффектов/управления |
| `StartUpDarkSide` (:92) | `StartUpDarkSide` | готов | check01 и музыка в исходном порядке |
| `GateLeverPulled` (:102) | `GateLeverPulled` | готов | фиксирует рычаг, звуки, таймер 42.5 + TimeAdd и увеличение времени |
| `TimerStopLeverSound` (:123) | `TimerStopLeverSound` | готов | остановка звука из имени таймера |
| `TimerFirstGateGoesDown` (:127) | `TimerFirstGateGoesDown` | готов | закрытие ворот, освобождение рычага, звуки и восстановление коллбэка |
| `CollidePastFirstGate` (:142) | `CollidePastFirstGate` | готов | быстрое закрытие, квест, смена активных монстров и check02 |
| `CollideGateWithWater` (:167) | `CollideGateWithWater` | готов | всплеск и звук у области ворот |
| `CollideActiveWaterLurker3` (:176) | `CollideActiveWaterLurker3` | готов | три таймера урона двери и последний монстр |
| `TimerDestroyDoor` (:184) | `TimerDestroyDoor` | готов | каждый вызов снимает 35 здоровья двери и проигрывает звуки |
| `TimerActivateLastLurker` (:192) | `TimerActivateLastLurker` | готов | включение четвёртого монстра |
| `UseHollowNeedleOnDoor` (:199) | `UseHollowNeedleOnDoor` | готов | отпирание выхода, звук, музыка и рассудок |
| `TimerEntranceHowl` (:208) | `TimerEntranceHowl` | готов | начальный вой у первого монстра |
| `TimerRandomGuardian` (:213) | `TimerRandomGuardian` | готов | исходный RandFloat с усечением, звук и повторный таймер |
| `CreateOrResetEntity` (:225) | `CreateOrResetEntity` | готов | создание отсутствующей сущности или ResetProp существующей |
| `CreateAndResetBodyParts` (:237) | `CreateAndResetBodyParts` | готов | четыре руки, три ноги и два торса, все исходные пути |
| `CheckPoint01` (:256) | `CheckPoint01` | готов | здоровье, сброс ворот, увеличение TimeAdd; ветки счётчика <1 / ==1 / >1 |
| `CheckPoint02` (:275) | `CheckPoint02` | готов | колесо, ворота, дверь, части тел; все три ветки счётчика |
| `InteractGate` (:298) | `InteractGate` | готов | сообщение, короткое смещение ворот и два таймера |
| `TimerCloseGate` (:308) | `TimerCloseGate` | готов | звук, закрытие, условная регистрация коллбэка и квест первых ворот |
| `InteractWheel` (:322) | `InteractWheel` | готов | снятие взаимодействия со вторыми воротами |
| `InteractSlimeExit` (:330) | `InteractSlimeExit` | готов | слизь, сообщение, квест и таймер повторного коллбэка |
| `TimerSlimeExit` (:340) | `TimerSlimeExit` | готов | повторное взаимодействие с ScriptArea_1 |
| `OnStart` (:346) | `ArchivesCellar.OnStart` | готов | части тел, переменные, коллбэки, связи, предмет, туман, кэш и отладка |
| `OnEnter` (:400) | `ArchivesCellar.OnEnter` | готов | исходные preload-вызовы, музыка и AutoSave |
| `OnLeave` (:418) | `ArchivesCellar.OnLeave` | готов | выход из приседа, завершение квеста и экран загрузки |

## Отличия от оригинала

- Синтаксис C#: `string &in` → `string`, десятичные литералы имеют `f`, единственное присваивание RandFloat целому явно усекается; жизненный цикл — `public override`.
- Статические имена функций-коллбэков используют `nameof`; динамические имена таймеров/сущностей остаются строками. Конкатенация `fTime` на `07_archives_cellar.hps:120` переведена через `Str(float)` с форматом `G6` и `InvariantCulture`; в отладочной строке время записано `42.5` даже в русской Windows.
- Сохранены все 53 комментария и исходный порядок операций. Дополнительная лексическая сверка после нормализации только синтаксиса C# даёт 1834 одинаковых токена действующего кода. Функции, строки, вызовы, числа и коллбэки карты также проверены штатной сверкой.
- Движок, API, импортёр, инструменты, готовые порты, предыдущие журналы и `docs/ScriptPort/api.md` не изменялись. Правка пользователя о перенесённой смерти в журнале 06 сохранена.

## Особенности оригинала (сохранены)

- **Остановка отсутствующего тиканья.** `07_archives_cellar.hps:132–:133`, :146–:147 безусловно останавливают `ticking1`/`ticking2`. Эти динамические звуки создаёт только рычаг (:112–:113); в `.map` одноимённых Sound-узлов нет. Если первые ворота уже закрылись по таймеру, последующий `CheckPoint01` (:261) снова вызывает остановку; если пройти ворота без рычага, звуки ещё не создавались. Оба случая дают `[Script] ERROR: Could not find sound entity 'ticking1'` и такой же для `ticking2`. Это поведение оригинала, а не опечатка порта: `src/AmnesiaTheDarkDescent/amnesia/src/game/LuxScriptHandler.cpp:2101–:2120` также сообщает ошибку, если StopSound ничего не нашёл. В обоих прогонах чекпоинтов предупреждения сохранены, без проверок существования и обходов.
- **Опечатка предзагрузки.** `07_archives_cellar.hps:405` содержит `ater_lurker_hunt`, а действующий звук на :188 — `water_lurker_hunt`. Первого файла в ресурсах нет, второй существует. Строка не исправлена; текущий `PreloadSound` — пустая операция, предупреждения из-за этой строки не возникает.
- **Повторные изменения FOV.** В одном обработчике цель сначала 4.0 (:42), затем 0.5 (:52); по завершении дважды устанавливается 1.0 (:80, :84). Все вызовы оставлены в прежнем порядке.
- **Опечатка имени и одинаковые имена таймеров.** Имя `TimerTelportDone` сохранено. Три `destroydoor1` (:178–:180), повторы `breath`/`breathl` не объединены; три удара дают здоровье двери 100 → 65 → 30 → −5.
- **Чекпоинт до новой музыки.** `StartUpDarkSide` сначала задаёт check01 (:94), затем включает `07_amb.ogg` (:96). При обычном переходе предыдущая музыка уже остановлена (:49), поэтому журнал рантайма фиксирует у check01 пустую сохранённую музыку. У check02 сохранена `07_amb.ogg`. Порядок не переставлен ради изменения музыки после смерти.
- **Добавки времени.** Каждый штатный запуск рычага добавляет TimeAdd на 2.5, каждое возрождение на check01 — на 3. Ветки количества возрождений строго `< 1`, `== 1`; для следующих попыток регистрация нового монстра не выполняется.
- **Неиспользуемая область ноги.** В `.map` есть `AreaLeg_4`, но исходный цикл создаёт только три ноги (:243–:245); четвёртая не добавлялась.

## Не перенесено движком

| API `NotPorted` | Сообщение `skipped, needs` | Последствие |
|---|---|---|
| `InteractConnectPropWithMoveObject` | `interact connections` | колесо не передаёт движение вторым воротам; вызов и параметры сохранены |
| `AutoSave` | `saving (step 6)` | вход не создаёт сохранение на диск |

Дополнительные ограничения и наблюдения:

- Водяные монстры — `EnemyWaterLurker` (водяной монстр перенесён (6.4, CLAUDE.md «Враги» и «Вода»)). Прогон 2026-10-16: `CollideActiveWaterLurker` включает `waterlurker_1`, он охотится на игрока в воде у `PlayerStartArea_3`, кусает (здоровье 61), рассудок падает, пока он охотится, музыка `att_guardian.ogg`; части трупов из `OnStart` (`IsFood`) в воде приманивают и съедаются (`WaterLurkerTests`). Чекпоинт выключает и сбрасывает врагов.
- `LiquidArea_1` работает: игрок в воде медленнее (×0.8) и шагает `step_*_water`, ящики и бочки плавают, при входе всплески. `ConnectEntities` первых ворот уже работает: проверены физическое поднятие и закрытие. Существующий `SetMoveObjectState` вторых ворот проверен отдельно от отсутствующей связи с колесом.
- **Дверь и восстановление её вида требуют проверки движка.** В обоих событийных прогонах три удара действительно доводят `cellar_wood01_2` до −5, но `GetEntityExists cellar_wood01_2_broken` остаётся False. После check02 здоровье возвращается к 100, что подтверждено, но полное визуальное разрушение/восстановление не считается проверенным. Исходный `.ent` задаёт `Breakable=true` и BrokenEntity; порт вызывает AddPropHealth и ResetProp точно как `.hps`. В текущем `PropSwingDoor.cs` также нет переопределения `OnResetProperties`, тогда как оригинал `LuxProp_SwingDoor.cpp:300` сбрасывает флаг разрушения, удаляет обломки и включает тела/меши. Конкретная причина отсутствия broken-сущности в прогоне не установлена. Правок и искусственного создания/удаления двери нет.
- Смерть и возрождение уже реализованы (`PlayerHurt.cs`, `HplMap.LoadCheckPoint`); здесь проверены через реальные летальные повреждения, а не заменены прямыми вызовами обработчиков.

## Проверка

Unity 6000.6.2f1 запускался последовательно, с проверкой отсутствия `Temp/UnityLockfile`, с GPU для симуляций и доступом к действующей лицензии Windows. Во всех четырёх итоговых симуляциях код выхода 0; нет исключений, `PortError`, `does not exist in`, несовпадения числа аргументов, `No function` и ошибок компиляции. Все `[Script] ERROR` разобраны выше.

### Импорт

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/07_archives_cellar.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Import_07_archives_cellar.log
```

```text
=== Level: maps/main/ch01/07_archives_cellar.map — 2026-10-16 15:55:49 — 2 warnings, 0 errors ===
Resources: 77 models (6 imported now), 8 extra materials (0 converted now), 1 cookies
```

Два предупреждения: неиспользуемое изображение скина `gui_amn_mm_textbox_marker.tga` и ещё отсутствовавший порт. После добавления класса симуляция находит `ArchivesCellar`, 138 сущностей скрипта. Сцена создана и добавлена импортёром в `ProjectSettings/EditorBuildSettings.asset`; это ожидаемое изменение сохраняется. Отчёт до симуляций — `Logs/07_archives_cellar_import_report.log`, полный лог — `Logs/Import_07_archives_cellar.log` (все Logs находятся внутри `UnityProj/UMnesiaRTX`).

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 07_archives_cellar -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/ArchivesCellar.cs
```

```text
07_archives_cellar : функций 30, строк 132, ошибок 0, предупреждений 0
```

Дополнительно совпали 1834 токена после нормализации синтаксиса и все 53 исходных комментария. Новых предупреждений компилятора из `ArchivesCellar.cs` нет. `.hps`/`.map` не менялись.

### Симуляции

Общий запуск; `$script` — набор команд ниже, для каждого запуска задан отдельный полный путь лога:

```powershell
# Unity.exe: C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/07_archives_cellar.map -seed 1 -script "$script" -logFile <полный путь к логу>
```

Все четыре запуска — без `-scriptDebug`, с обычным входом и исходными чекпоинтами:

| Лог в `UnityProj/UMnesiaRTX/Logs/` | Предупреждения | Ошибки | Результат |
|---|---|---|---|
| `Sim_07_archives_cellar.log` | 2 | 0 | 30 с старта, атмосфера и создание девяти частей тел |
| `Sim_07_archives_cellar_events.log` | 2 | 0 | десять основных обработчиков и их таймеры |
| `Sim_07_archives_cellar_checkpoint01.log` | 4 | 0 | связь первых ворот, таймер, три возрождения check01; +2 исходных StopSound |
| `Sim_07_archives_cellar_checkpoint02.log` | 4 | 0 | три возрождения check02, части тел, вторые ворота, финальное событие; +2 исходных StopSound |

**Старт:** `watch 30 2; shot start`. Музыка `01_amb_darkness.ogg`, начальный вой, повторный таймер голосов; игрок остаётся на полу, управление включено, FOV/aspect = 1, эффекты = 0. Части тел создаются из шаблонов без ошибок путей.

**Основные события:** после каждого из десяти вызовов — `watch 10 2`:

```text
call CollideTeleportToDarkSide Player TeleportToDarkSideArea 1
call CollideActiveWaterLurker Player ActiveWaterLurker 1
call InteractSlimeExit ScriptArea_1
call InteractGate castle_portcullis_1
call GateLeverPulled FirstGate lever_simple01_1 castle_portcullis_1 -1
call CollideActiveWaterLurker2 Player ActiveWaterLurker2 1
call CollidePastFirstGate Player PastFirstGate 1
call InteractWheel valve_iron_1
call CollideActiveWaterLurker3 Player ActivateLurker3 1
call UseHollowNeedleOnDoor hollow_needle_1 level_wood_1
```

Телепорт переносит к `PlayerStartArea_3`: управление восстановлено, FOV = 1, затемнение/blur = 0, музыка `07_amb.ogg`. Первые монстры активируются; после прохода ворот 1/3 выключены, 2 включён, квест `07Gate` выполнен, создан check02. Финальный обработчик даёт три удара двери и включает waterlurker_4. Отмычка вызывает исходное отпирание, звук, музыку успеха и восстановление рассудка; переход через дверь не запускался, инвентарное применение предмета отдельно не проверялось.

Прямой вызов `GateLeverPulled` в этом прогоне сначала сам меняет состояние рычага и тем самым ещё раз получает коллбэк от зарегистрированной связи: TimeAdd становится 5. Это побочный эффект диагностического вызова извне, код не менялся. Отдельный прогон ниже запускает рычаг через его состояние и подтверждает один штатный коллбэк с TimeAdd = 2.5.

**Первые ворота и check01:** после обычного телепорта использован `SetLeverStuckState lever_simple01_1 -1 true`. Связь сама поднимает ворота; лог содержит `Lever pulled to: 1 time: 42.5`, TimeAdd = 2.5. Положение тела ворот меняется с Y ≈ −2.522 в закрытом состоянии на ≈ −0.092 при открытии. После ожидания таймера 42.5 с ворота опускаются, локальная переменная ворот = 0, рычаг освобождён.

Перед первой смертью сняты два коллбэка водяных монстров диагностическими `RemoveEntityCollideCallback`, чтобы исходная стартовая регистрация не мешала проверке повторного добавления. Три раза использовано `GivePlayerDamage 500 BloodSplat true true`, после каждого — `watch 20 2` для полного затухания, текста смерти, возрождения и проявления. Движок вызывает check01 со счётчиками 0, 1, 2. Здоровье после них — примерно 84.84 / 83.68 / 82.04; TimeAdd — 5.5 / 8.5 / 11.5. После первой/второй смерти переход в соответствующую область командой `goto` реально запускает восстановленные коллбэки: waterlurker_1 и waterlurker_3 имеют active True. Третья ветка не регистрирует нового монстра. Ошибки тиканья возникают после уже завершённого таймера ворот и сохранены как в оригинале.

Первый пробный запуск ждал только 10 с после каждого удара; этого недостаточно до завершения текста смерти. Итоговый лог check01 — повтор с ожиданием 20 с, без изменений порта. Старый раздел остаётся в дописываемом `AmnesiaImport.log`.

**Check02:** обычный телепорт, событие слизи и проход первых ворот создают check02; проход без рычага сохраняет предупреждения об отсутствии ticking1/2. Рука `FoodArm1` диагностически перенесена на X = 160; колесо и вторые ворота установлены в отличное от исходного состояние. После трёх летальных повреждений с `watch 20 2` рантайм вызывает check02 со счётчиками 0, 1, 2; здоровье игрока примерно 88.20 / 81.17 / 81.39. Рука возвращается к X ≈ 144.2068 и здоровью 100; вторые ворота закрываются, waterlurker_2 включён.

После второго возрождения переход в `ActivateLurker3` запускает уже зарегистрированный коллбэк через столкновение: дверь получает −105 здоровья, waterlurker_4 включается. После третьего возрождения здоровье двери = 100; ограничения её визуального состояния описаны выше. Под конец `SetPlayerCrouching true` и `OnLeave` возвращают размер игрока из приседа в стоячий, завершают `07PushOnward` и задают экран загрузки; `QuestIsCompleted = True`. `ChangeMap` не вызывался.

Контрольные снимки: `Logs/Player_07_archives_cellar_00_start.png`, `..._00_dark_side.png`, `..._01_events.png`, `..._00_checkpoint01.png`, `..._00_checkpoint02.png` (вне git). Просмотрены старт и тёмная сторона: стартовый коридор со свечами, после переноса — тёмное помещение; значения затухания/эффектов проверены отдельно по состоянию рантайма.

### Регрессия

Выполнена с GPU, без `-quit` и `-nographics`. Ожидаемое число проверок переходов обновлено пользователем до 66; проверки не менялись.

```text
Unity.exe -batchmode -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MapChangePreview.RunBatch -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Regression_07_archives_cellar_MapChange.log
Unity.exe -batchmode -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MainMenuPreview.RunBatch -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Regression_07_archives_cellar_MainMenu.log
```

```text
PASS: 66 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian, damage and death); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 14 main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).
PASS: 14 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

Оба запуска завершены кодом 0. Финальная сверка текущей карты повторена: 30 функций, 132 строковые константы, 0 ошибок, 0 предупреждений.

## Файлы и состояние репозитория

Созданы только `ArchivesCellar.cs`, его Unity `.meta` и этот журнал. В строке плана `CLAUDE.md`/`AGENTS.md` отмечена только текущая карта; файлы инструкций побайтно одинаковы. Сцену в список сборки добавил импортёр. Автоматическое пересохранение `TimeManager.asset` возвращено командой `git checkout -- UnityProj/UMnesiaRTX/ProjectSettings/TimeManager.asset`. Остальные настройки проекта не изменились. Unity закрыт, lock-файла нет. Ресурсы остаются вне git. Коммитов и push нет; карта 08 не переносилась.

Итоговый `git status --short`:

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/ArchivesCellar.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/ArchivesCellar.cs.meta
?? docs/ScriptPort/ch01/07_archives_cellar.md
```
