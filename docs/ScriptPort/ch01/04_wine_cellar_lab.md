# 04_wine_cellar_lab

- **Оригинал:** `source/maps/main/ch01/04_wine_cellar_lab.hps` (Latin-1, 918 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellarLab.cs` (класс `RTXMnesia.Levels.Ch01.WineCellarLab`, `[HplMapScript("maps/main/ch01/04_wine_cellar_lab.map")]`).
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/04_wine_cellar_lab.unity` (генерируется, вне git).
- **Статус:** все 43 функции перенесены; проверены старт, десять основных событий, приготовление кислоты с ёмкостью и без неё, коллбэки замены досок. Ограничения движка и ошибки оригинала описаны ниже. Дата проверки: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в оригинальном скрипте нет. Состояние хранится в локальных/глобальных переменных API. Все методы ниже находятся в классе `WineCellarLab`; номера строк и порядок соответствуют `.hps`.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `FuncMoanHorrors` (:5) | `FuncMoanHorrors` | готов | два начальных таймера фоновых голосов |
| `FuncMoanTimer` (:19) | `FuncMoanTimer` | готов | обе ветки, повторный RandFloat в ветке ребёнка сохранён |
| `FuncCreakHorrors` (:49) | `FuncCreakHorrors` | готов | включение/выключение скрипов и взаимная регистрация областей |
| `FuncCreakTimer` (:70) | `FuncCreakTimer` | готов | пыль, звук, случайная задержка; float в строке через `Str` |
| `CollideBreakStairs` (:91) | `CollideBreakStairs` | готов | условие наличия глобальной переменной ключа |
| `BreakMyStairs` (:97) | `BreakMyStairs` | готов | защита от повторного запуска, обвал, свет, платформы и таймеры |
| `TimerSanity` (:139) | `TimerSanity` | готов | все пять имён таймеров реакции на обвал |
| `GiveStackHint` (:179) | `GiveStackHint` | готов | подсказка липкой области |
| `CollideStickLiftExtra` (:186) | `CollideStickLiftExtra` | готов | дополнительная область второй доски, исходное извлечение подстроки |
| `CollideStickLift` (:201) | `CollideStickLift` | готов | сигнатура AttachFunction: две строки |
| `TimerChangeEntity` (:213) | `TimerChangeEntity` | готов | обе доски и лестница; ошибочное составное имя сохранено |
| `CollideQuestStairs` (:223) | `CollideQuestStairs` | готов | задание восстановления пути |
| `CollideQuestStairs02` (:229) | `CollideQuestStairs02` | готов | завершение задания |
| `ItemJarOnMachine` (:241) | `ItemJarOnMachine` | готов | установка ёмкости и удаление предмета инвентаря |
| `AddChemical` (:253) | `AddChemical` | готов | все четыре химиката; обвал на третьем, завершение задания на четвёртом |
| `InteractAcidMachine` (:282) | `InteractAcidMachine` | готов | недостающие химикаты или разблокировка зажигания |
| `InteractAcidMachine02` (:294) | `InteractAcidMachine02` | готов | сообщение о необходимости зажигания, смена коллбэков |
| `InteractTurnOnAcidMachine` (:306) | `InteractTurnOnAcidMachine` | готов | включение и выключение всех горелок, вентилей, света и звука |
| `InteractTurnValve` (:345) | `InteractTurnValve` | готов | обе ветки; активная логика счётчика и отключённая старая последовательность сохранены |
| `DoEffectLarge` (:463) | `DoEffectLarge` | готов | пар, кипение и увеличение `RoadToSuccess` |
| `DoEffectSmall` (:474) | `DoEffectSmall` | готов | сохранён неиспользуемый метод |
| `TimerReleaseValves` (:482) | `TimerReleaseValves` | готов | сохранён неиспользуемый метод |
| `TimerAcidDone` (:490) | `TimerAcidDone` | готов | ветки с ёмкостью и без неё; исходное имя `Part4PSteam` |
| `PickFinalAcid` (:522) | `PickFinalAcid` | готов | глобальная переменная кислоты и гром |
| `EntityCallPickEmptyChem` (:530) | `EntityCallPickEmptyChem` | готов | коллбэк из карты, прибавка рассудка |
| `EntityCallPickNote` (:535) | `EntityCallPickNote` | готов | коллбэк из карты, задание поиска химикатов |
| `EntityCallPickNote02` (:539) | `EntityCallPickNote02` | готов | коллбэк из карты, задание о перенесённых химикатах |
| `CollideActiveHole` (:549) | `CollideActiveHole` | готов | активация области взгляда в провал |
| `LookAtHole` (:553) | `LookAtHole` | готов | проверка состояния взгляда и начало события |
| `TimerLookHole` (:561) | `TimerLookHole` | готов | все состояния 1–12, звуки, брызги и реакции |
| `CollideActivateSpider` (:639) | `CollideActivateSpider` | готов | область взгляда на пещеру и начало дыхания |
| `TimerRockBreath` (:648) | `TimerRockBreath` | готов | усиление дыхания/тряски, повторный таймер, float через `Str` |
| `LookAtCave` (:664) | `LookAtCave` | готов | коллбэк карты, предупреждение существа и таймеры реакции |
| `TimerRockMonster` (:677) | `TimerRockMonster` | готов | все четыре ветки; исходное `react_creath` |
| `CollideScreamCave` (:697) | `CollideScreamCave` | готов | крик, частицы и таймеры |
| `TimerCaveScream` (:710) | `TimerCaveScream` | готов | все ветки, включая пустую `scream2`; исходное `react_creath` |
| `TimerBigFeet` (:737) | `TimerBigFeet` | готов | все пять шагов |
| `DoSteps` (:755) | `DoSteps` | готов | исходная сигнатура из четырёх float и строки |
| `TimerEnterClank` (:767) | `TimerEnterClank` | готов | вступление и отдельная ветка грома после кислоты |
| `EntityCallLamp` (:794) | `EntityCallLamp` | готов | коллбэки ламп из карты, увеличение фонового света |
| `OnStart` (:804) | `WineCellarLab.OnStart` | готов | все коллбэки и подключения, отладочные предметы, туман |
| `OnEnter` (:883) | `WineCellarLab.OnEnter` | готов | все preload-вызовы, музыка, однократное вступление, `AutoSave` |
| `OnLeave` (:915) | `WineCellarLab.OnLeave` | готов | исходный пустой метод |

## Отличия от оригинала

- Синтаксис AngelScript переведён в C#: строковые параметры — `string`, десятичные литералы — с `f`, три присваивания RandFloat целым — с явным усечением, методы жизненного цикла — `public override`.
- Имена существующих коллбэков передаются через `nameof`. Динамический `sEvent` и пустые строки сохранены.
- Два соединения строки с float (`FuncCreakTimer`, `TimerRockBreath`) используют `Str` с `G6` и `InvariantCulture`. В русской локали лога значения внутри этих отладочных строк остаются с точкой.
- Все 92 исходных комментария, включая отключённые правила старой головоломки, сохранены. Все ветки `switch` имеют исходный `break`; провалов между `case` и специальных обработчиков исключений эта карта не требует.
- Движок, импортёр, API, готовые порты и журналы 00–03, `docs/ScriptPort/api.md` не менялись.

## Особенности оригинала (сохранены)

- **Удаление ещё не созданных эффектов при старте.** `04_wine_cellar_lab.hps:823`–:827 сначала подключает коллбэки, затем устанавливает вентили в состояние −1. Это вызывает ветки выключения на :426–:429 и :330–:340 до создания соответствующих эффектов. В карте нет `Part_1..4PSteam`, `Part_1..4SBoil`, `Part_1..4SSteam`, `IgniteFire1..4`, `IgniteSound` (проверены все XML-узлы с `Name`): это имена создаваемых скриптом эффектов, которых на старте ещё нет. Получаются 17 предупреждений `ERROR`, по одному на каждое имя. Исходный `cLuxProp_Wheel::SetStuckState` вызывает `ChangeState` и при выключенных звуковых эффектах (`src/AmnesiaTheDarkDescent/amnesia/src/game/LuxProp_Wheel.cpp:277`, :584). Оригинальные `DestroyParticleSystem` и `StopSound` тоже сообщают об отсутствии объекта (`LuxScriptHandler.cpp:2002`, :2100). Порядок и вызовы сохранены.
- **Отрицательный счётчик вентилей.** Тот же старт уменьшает `ValvesNrOn` до −4. Открытие четырёх вентилей увеличивает его до 0, что запускает завершение (:358); значение −2 запускает тяжёлые шаги (:375). Это не заменено обычным счётчиком 0–4. Старые проверки порядка через `RoadToSuccess` остаются в исходных комментариях.
- **Опечатка звука дыхания.** `04_wine_cellar_lab.hps:690`, :694, :723, :728 запрашивает `react_creath`. Такого имени нет ни в `.map`, ни среди файлов `source/sounds`; `react_breath.snt` существует. Порт сохраняет опечатку и предупреждение `Cannot find sound entity 'react_creath'`.
- **Имя пара без подчёркивания.** `04_wine_cellar_lab.hps:492` удаляет `Part4PSteam`, хотя соединения называются `Part_1..4` (:823), а пар создаётся по имени соединения (:465). `Part4PSteam` отсутствует в карте и нигде не создаётся скриптом; соответствующее предупреждение сохранено.
- **Двойной префикс платформы.** `04_wine_cellar_lab.hps:215`–:220 отключает динамическую доску, затем для любого таймера добавляет префикс `Platform`. Для имён тел получаются `PlatformPlatformDynStickLift_1_Body_1` и `_2_Body_1`, которых в карте нет. При этом второй таймер правильно включает `PlatformStickLift_1` / `_2`, а для второй области ещё и лестницу. Обе ошибочные попытки и полезные действия сохранены.
- **Повторяющиеся имена use-item коллбэков.** `04_wine_cellar_lab.hps:844`–:845 использует одно имя `Chemicalv_<i>` для четырёх разных вентилей. Имена не переименовывались, вложенный цикл и все регистрации сохранены.
- **Дополнительный шаг события провала.** Условие повторения на `04_wine_cellar_lab.hps:630` планирует ещё один вызов после состояния 12. Исходная верхняя граница сохранена.
- **Дублирование области при прямом тестовом вызове.** Во втором прогоне прямой `FuncCreakHorrors` добавил область выключения, пока исходный коллбэк области включения ещё оставался зарегистрированным. При проверке пересечений он привёл к предупреждению `A callback with entity 'EnterCreakOnArea' already exists!`. Область существует в карте; это следствие прямого вызова функции вне обычной цепочки одноразовых столкновений, а не опечатка порта. Код регистрации не менялся.

## Не перенесено движком

Единственный вызываемый этой картой API `NotPorted` — `AutoSave`: `skipped, needs saving (step 6)`. Вход не создаёт сохранение на диск; оба исходных вызова в ветках `OnEnter` оставлены.

Липкие области `StickLift_1` / `_2` импортируются, но механизм прикрепления и автоматический вызов их `AttachFunction` пока отсутствуют. `CollideStickLift` полностью перенесён и проверен прямым вызовом; естественный запуск через Sticky ожидает реализации движка. Дополнительная обычная область `StickLift_2_Extra` использует уже работающие коллбэки столкновений. В карте также есть две области Insanity и одна Liquid, для которых игровое поведение пока не реализовано. Это ограничения областей, отдельных вызовов `NotPorted` от скрипта они не дают.

`FogArea` пропущена импортёром. Обходов и исправлений движка в этой задаче нет.

## Проверка

Запуски выполнялись последовательно с проверкой отсутствия `Temp/UnityLockfile`; Unity 6000.6.2f1 запускался с доступом к действующей лицензии Windows. Все четыре симуляции завершились с кодом 0, без исключений, `PortError`, `does not exist in` и сообщений о несовпадении числа аргументов. Предупреждения `ERROR` разобраны выше.

### Импорт

```powershell
$unityExe = 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe'
$unityProject = 'Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX'
& $unityExe -batchmode -nographics -quit -projectPath $unityProject -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/04_wine_cellar_lab.map -logFile "$unityProject/Logs/Import_04_wine_cellar_lab.log"
```

Повторный импорт: **3 warnings, 0 errors**, код выхода 0; 92 модели, 14 дополнительных материалов, 2 cookie. Сцена уже была в проекте и в списке сборки, поэтому build-настройки не изменились. Копия лога импорта: `Logs/04_wine_cellar_lab_import_report.log`.

| Предупреждение | Количество |
|---|---:|
| `FogArea` не поддержана | 1 |
| Отсутствующая картинка skin меню `gui_amn_mm_textbox_marker.tga`, не используемая этим меню | 1 |
| Отсутствие порта скрипта на момент импорта | 1 |

После добавления C# симуляция нашла `WineCellarLab`, 164 скриптовые сущности; переимпорт для подключения порта не требуется. Ресурсы остались в игнорируемой папке.

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 04_wine_cellar_lab -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellarLab.cs
```

```text
04_wine_cellar_lab : функций 43, строк 295, ошибок 0, предупреждений 0
```

Дополнительно подтверждено совпадение последовательности 4593 токенов после явных синтаксических преобразований C# и сохранение всех 92 комментариев (без учёта пробелов/отступов). Новый порт компилируется без собственных предупреждений; существующие предупреждения компилятора в `PlayerDarkness`, `PlayerHands`, `OldArchives` не относятся к этой карте.

### Старт

Базовый запуск симуляции с GPU, без `-nographics`:

```powershell
& $unityExe -batchmode -quit -projectPath $unityProject -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/04_wine_cellar_lab.map -seed 1 -script 'watch 30 2; shot start' -logFile "$unityProject/Logs/Sim_04_wine_cellar_lab.log"
```

Результат: **18 warnings, 0 errors** — 17 исходных попыток удаления ещё не созданных эффектов и `AutoSave`. Вступление, фоновые голоса, музыка `18_amb`, туман и HUD работают. `Logs/Player_04_wine_cellar_lab_00_start.png` просмотрен.

### Десять основных событий

Тот же запуск с `-globals 'key_study_1=key_study_1'`, логом `Logs/Sim_04_wine_cellar_lab_events.log` и строкой `-script` ниже. После каждого из десяти вызовов — `watch 10 2`:

```text
call FuncCreakHorrors Player EnterCreakOnArea 1; watch 10 2;
call CollideBreakStairs Player AreaBreakStairs 1; watch 10 2;
call CollideQuestStairs Player AreaQuestStairs 1; watch 10 2;
call CollideQuestStairs02 Player AreaQuestStairs_2 1; watch 10 2;
call LookAtHole AreaLookHole 1; watch 10 2;
call CollideActivateSpider Player AreaBeginSpider 1; watch 10 2;
call LookAtCave AreaBeginSpider_1 1; watch 10 2;
call CollideScreamCave Player AreaScreamCave 1; watch 10 2;
call InteractAcidMachine ValveIgnite; watch 10 2;
call EntityCallLamp torch_static01_16 OnIgnite; watch 10 2; shot events
```

| Событие | Результат |
|---|---|
| Скрипы | случайная пыль и звуки, повторные таймеры; разобранное предупреждение дублирования коллбэка |
| Лестница | обрушение, замена объектов, погашенные факелы, все пять таймеров реакции и восстановление движения |
| Задание лестницы | добавление, включение следующей области и завершение |
| Взгляд в провал | музыка, все 12 шагов звуков/брызг/реакций |
| Активация пещеры | включение области взгляда, дыхание с усилением тряски |
| Взгляд на пещеру | звук предупреждения, краткий взгляд игрока на существо, восстановление размытия |
| Крик | частицы, тряска, потеря рассудка, восстановление эффектов; исходная опечатка дыхания |
| Машина без химикатов | задание и сообщение о недостающих веществах |
| Лампа | увеличение фонового света через `AmbLight` |

Результат: **20 warnings, 0 errors** — те же 18, плюс дублирование коллбэка и `react_creath`. Вызовов `ChangeMap` не было.

### Полное приготовление кислоты

Тот же базовый запуск с `-seed 1`, логом `Logs/Sim_04_wine_cellar_lab_acid.log` и следующей строкой `-script` (без `-scriptDebug`):

```text
start PlayerStartArea_5;
call GiveItemFromFile lantern lantern.ent; call SetLanternActive true true;
call InteractAcidMachine ValveIgnite; call GetLocalVarInt ValvesNrOn;
call GiveItemFromFile chemical_container_1 chemical_container.ent;
call GiveItemFromFile Chemical_1 flask01_aqua_regia.ent;
call GiveItemFromFile Chemical_2 flask01_calamine.ent;
call GiveItemFromFile Chemical_3 flask01_cuprite.ent;
call GiveItemFromFile Chemical_4 flask01_orpiment.ent;
call ItemJarOnMachine chemical_container_1 AreaPlaceJar; watch 1 1;
call AddChemical Chemical_1 acid_machine_1; watch 1 1;
call AddChemical Chemical_2 acid_machine_1; watch 1 1;
call AddChemical Chemical_3 acid_machine_1; watch 1 1;
call AddChemical Chemical_4 acid_machine_1; watch 10 2;
call InteractAcidMachine02 Valve_1; watch 1 1;
call InteractAcidMachine ValveIgnite; watch 1 1;
call SetWheelStuckState ValveIgnite 1 true; watch 2 1;
call SetWheelStuckState Valve_1 1 true; watch 2 1;
call SetWheelStuckState Valve_2 1 true; watch 10 2;
call SetWheelStuckState Valve_3 1 true; watch 2 1;
call SetWheelStuckState Valve_4 1 true; watch 10 2;
body chemical_container_2;
call GetLocalVarInt ChemicalsInMachine;
call GetLocalVarInt StairsWentDownTheDrain; call GetLocalVarInt DoBigFeet;
aim chemical_container_2; shot acid;
call PickFinalAcid chemical_container_2 OnPickup; watch 10 2;
call GetGlobalVarString chemical_container_2
```

Вентили переводились через существующий API `SetWheelStuckState`, который отправляет реальные события соединений; `InteractTurnValve` и `InteractTurnOnAcidMachine` здесь вызывались движком. Подтверждены:

- стартовый `ValvesNrOn` = −4 и последовательность −3, −2, −1, 0;
- удаление четырёх химикатов, смена пустых/полных колб и света, `ChemicalsInMachine` = 4;
- обвал на третьем химикате, `StairsWentDownTheDrain` = 1;
- завершение задания поиска, сообщение о зажигании и разблокировка главного вентиля;
- четыре горелки, пар, кипение, отключение машины после четвёртого вентиля;
- тяжёлые шаги над лабораторией после второго вентиля, `DoBigFeet` = 1;
- `chemical_container_2` активна, музыка `04_puzzle_acid.ogg`, получение глобальной переменной `chemical_container_2` после коллбэка подбора, отложенный гром.

Результат: **19 warnings, 0 errors** — стартовые 18 и исходное `Part4PSteam`. Снимок `Logs/Player_04_wine_cellar_lab_00_acid.png` просмотрен: на столе полная ёмкость, колбы с химикатами и остановленная машина.

### Слив без ёмкости и доски

Ещё один запуск использовал `-scriptDebug`: предметы химикатов и огнива выдал сам исходный отладочный блок `OnStart`. Лог: `Logs/Sim_04_wine_cellar_lab_branches.log`; строка `-script`:

```text
start PlayerStartArea_5; call SetLanternActive true true;
call InteractAcidMachine ValveIgnite;
call AddChemical Chemical_1 acid_machine_1; watch 1 1;
call AddChemical Chemical_2 acid_machine_1; watch 1 1;
call AddChemical Chemical_3 acid_machine_1; watch 1 1;
call AddChemical Chemical_4 acid_machine_1; watch 10 2;
call InteractAcidMachine ValveIgnite;
call SetWheelStuckState ValveIgnite 1 true; watch 1 1;
call SetWheelStuckState Valve_1 1 true; watch 1 1;
call SetWheelStuckState Valve_2 1 true; watch 8 2;
call SetWheelStuckState Valve_3 1 true; watch 1 1;
call SetWheelStuckState Valve_4 1 true; watch 5 1;
call GetLocalVarInt chemical_container_static_1; body chemical_container_2;
call CollideStickLift StickLift_1 PlatformDynStickLift_1_Body_1; watch 10 2;
body PlatformStickLift_1;
call CollideStickLiftExtra PlatformDynStickLift_2 StickLift_2_Extra 1; watch 10 2;
body PlatformStickLift_2; call GetLocalVarString StickOneTime; shot no_jar_platforms
```

Без установки ёмкости её локальная переменная остаётся 0, `chemical_container_2` не активируется, появляется русское сообщение о недостающей посуде, главный вентиль разблокирован для повторной попытки. Прямые коллбэки досок отключили динамические сущности и активировали обе статические платформы; `StickOneTime` в конце равен `StickLift_2`. Это проверка скрипта коллбэка, не проверка отсутствующей механики Sticky.

Результат: **21 warnings, 0 errors** — стартовые 18, `Part4PSteam`, два исходных имени с двойным префиксом `Platform`. Все имена проверены по XML карты.

### Регрессия

Запуски с GPU, без `-quit` и без `-nographics`, оба завершились с кодом 0:

```powershell
& $unityExe -batchmode -projectPath $unityProject -executeMethod RTXMnesia.EditorDev.MapChangePreview.RunBatch -logFile "$unityProject/Logs/Regression_04_wine_cellar_lab_MapChange.log"
& $unityExe -batchmode -projectPath $unityProject -executeMethod RTXMnesia.EditorDev.MainMenuPreview.RunBatch -logFile "$unityProject/Logs/Regression_04_wine_cellar_lab_MainMenu.log"
```

```text
PASS: 60 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 14 main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).
PASS: 14 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

Unity автоматически пересохранил `ProjectSettings/TimeManager.asset`. Во время работы внешний коммит `745febd` включил это пересохранение вместе с новым портом и первой частью журнала. После выполнения команды возврата из задания файл дополнительно возвращён к содержимому на момент начала задачи (`e105a8c`); обратное изменение формата сериализации осталось в рабочей копии. Фиксированный шаг — по-прежнему 1/60. История коммита не менялась, другие настройки проекта не изменились.

## Изменённые файлы и итоговый git status

Файлы, созданные в этой задаче (порт, метаданные и первая часть журнала уже включены внешним коммитом `745febd`):

- `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellarLab.cs`;
- `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellarLab.cs.meta` (создан Unity);
- `docs/ScriptPort/ch01/04_wine_cellar_lab.md`.

В `CLAUDE.md` и `AGENTS.md` только добавлена карта `04_wine_cellar_lab` в строку этапа 5; файлы побайтно одинаковы. В рабочей копии также дополнен этот журнал и восстановлен исходный формат `TimeManager.asset`. Список сборки и профиль Windows уже содержали сцену и не изменились. Агент не выполнял коммиты и push; внешний коммит сохранён. Работа ограничена картой 04.

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/TimeManager.asset
 M docs/ScriptPort/ch01/04_wine_cellar_lab.md
```
