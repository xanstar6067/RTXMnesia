# 06_distillery

- **Оригинал:** `source/maps/main/ch01/06_distillery.hps` (Latin-1, 551 строка).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Distillery.cs` (класс `RTXMnesia.Levels.Ch01.Distillery`, `[HplMapScript("maps/main/ch01/06_distillery.map")]`).
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/06_distillery.unity` (генерируется, вне git).
- **Статус:** все 34 функции перенесены; проверены старт, основные события, разрушение блокиратора шкива, четыре поворота, обе ветки коллбэка следов и пустые функции оригинала. Полное прохождение с рукояткой ограничено отсутствием связи с верёвками; ИИ стража ещё не перенесён. Обе обязательные регрессии прошли. Дата проверки: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в оригинальном скрипте нет; состояние хранится в переменных API. Все методы ниже — в классе `Distillery`, в исходном порядке.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `FuncMoanHorrors` (:5) | `FuncMoanHorrors` | готов | животные звуки, след, тряска и таймеры остановки |
| `TimerStopScareSound` (:29) | `TimerStopScareSound` | готов | отдельный урон рассудку и остановка двух звуков |
| `FuncMoanTimer` (:41) | `FuncMoanTimer` | готов | все три ветки: moan, child, animal; повторные RandFloat сохранены |
| `FuncCreakTimer` (:78) | `FuncCreakTimer` | готов | скрип, пыль, повторный таймер; float в отладочной строке через `Str` |
| `InteractBarrelBlock` (:100) | `InteractBarrelBlock` | готов | отпирает дверь и снимает взаимодействие с обеих бочек |
| `CollideBlockingBarellsArea` (:108) | `CollideBlockingBarellsArea` | готов | исходный немедленный возврат и недостижимый код сохранены |
| `InteractCrank` (:120) | `InteractCrank` | готов | сообщение, квест шкива, пыль и таймер повторного взаимодействия |
| `TimerInteractCrank` (:129) | `TimerInteractCrank` | готов | повторно устанавливает коллбэк на сущности с именем таймера |
| `BreakPulleyBlocker` (:134) | `BreakPulleyBlocker` | готов | только тип `Break`: разблокировка рукоятки, квест, свет и снятие коллбэков |
| `InteractHatch` (:156) | `InteractHatch` | готов | тяжёлый люк: сообщение, звук, импульс и два таймера |
| `TimerHatchOn` (:165) | `TimerHatchOn` | готов | обе ветки: остановка звука/повторный импульс и повторный коллбэк |
| `CollideInPulley` (:177) | `CollideInPulley` | готов | вход; выход сбрасывает оба счётчика и показывает подсказку |
| `CollidePulleyBlock` (:196) | `CollidePulleyBlock` | готов | разрушение при третьем пересечении и блокировка управления на 0.3 с |
| `TimerMakePlayerActive` (:212) | `TimerMakePlayerActive` | готов | возвращает управление |
| `InteractPulley` (:219) | `InteractPulley` | готов | флаг удержания блокиратора |
| `EnterEmptyStorageDoorOpen` (:229) | `EnterEmptyStorageDoorOpen` | готов | проверка закрытой двери, снятие коллбэка, 30 таймеров силы и реакции |
| `TimerOpenDoor` (:255) | `TimerOpenDoor` | готов | сила на тело из имени таймера |
| `EnterBottleRoomWallScratch` (:269) | `EnterBottleRoomWallScratch` | готов | закрытие двери, удары, звук врага и событие соседней двери |
| `TimerWallScratchNextDoor` (:290) | `TimerWallScratchNextDoor` | готов | соседняя дверь, звук поломки и реакции |
| `TimerWallScratchSound` (:299) | `TimerWallScratchSound` | готов | звук из имени таймера, включая исходную ошибку `enemy` |
| `EnterHowlArea` (:314) | `EnterHowlArea` | готов | три волны воя, box-свет, тряска/размытие; особая ветка второй области |
| `TimerStopRadialBlur` (:342) | `TimerStopRadialBlur` | готов | затухание размытия |
| `EnterEnemyGlimpse` (:355) | `EnterEnemyGlimpse` | готов | включение стража, взгляд, музыка, реакции и чекпоинт |
| `TimerStopLookAt` (:367) | `TimerStopLookAt` | готов | исходная проверка строки `react_scare`, ветка GUI-звука и остановка взгляда |
| `CollideEnemyHint` (:381) | `CollideEnemyHint` | готов | подсказка об опасности врагов |
| `EnemyDisabled` (:386) | `EnemyDisabled` | готов | условие пересечения, включение следов `grunt_gone_*`, звук двери |
| `CheckPoint01` (:397) | `CheckPoint01` | готов | пустой коллбэк оригинала |
| `CollideAreaRoses` (:405) | `CollideAreaRoses` | готов | лепестки роз и звук ветра |
| `EntityCallDiary` (:413) | `EntityCallDiary` | готов | пустое тело с исходным закомментированным вызовом |
| `CollideAreaRope` (:418) | `CollideAreaRope` | готов | два случайных звука; на четвёртом пересечении снимает коллбэк люка и два таймера |
| `OnStart` (:439) | `Distillery.OnStart` | готов | коллбэки, патруль, таймеры, четыре связи верёвок, пустой сосуд, отладка и туман |
| `InteractLastDoor` (:518) | `InteractLastDoor` | готов | сообщение и квест заблокированной двери |
| `OnEnter` (:526) | `Distillery.OnEnter` | готов | все preload-вызовы, музыка и `AutoSave` |
| `OnLeave` (:546) | `Distillery.OnLeave` | готов | исходный экран загрузки следующего уровня |

## Отличия от оригинала

- Синтаксис C#: `string &in` → `string`, десятичные литералы имеют `f`, четыре присваивания RandFloat целым явно усекаются; методы жизненного цикла — `public override`.
- Статические имена коллбэков используют `nameof`, динамические имена сущностей/таймеров остались строками. Для единственной конкатенации float на `06_distillery.hps:91` добавлен `Str(float)` с `G6` и `InvariantCulture`, как у AngelScript. В логе русской Windows дробная часть этой строки имеет точку (`15.2904`, `18.3535`).
- Сохранены все 74 комментария, пустые функции, отключённый патрульный узел и код после исходного возврата. После нормализации только перечисленного синтаксиса совпадают все 2403 токена действующего кода и порядок комментариев.
- Движок, API, импортёр, готовые порты и их журналы, `docs/ScriptPort/api.md` не менялись. Обходы недостающих механик не добавлялись.

## Особенности оригинала (сохранены)

- **Имя звука перепутано с именем таймера.** `06_distillery.hps:280` планирует `TimerWallScratchSound` с именем `enemy`. На :301 обработчик передаёт это имя как файл звуковой сущности, а затем на :303–:304 останавливает звук с именем `enemy`. Файла `enemy.snt` во всём `source/` нет; узла с `Name="enemy"` в `.map` также нет. Уже играющие звуки с таким именем не являются определением файла для нового `PlaySoundAtEntity`. Прогон события сохраняет единственный `[Script] ERROR: Cannot find sound entity 'enemy'`; имя и порядок вызовов не исправлялись.
- **Отключённая разблокировка через область бочек.** `06_distillery.hps:110` немедленно возвращается, хотя ниже остаётся разблокировка двери. Действующий путь — взаимодействие с одной из двух бочек (:100–:105), зарегистрированное в `.map:8278`, :8297. Порт сохраняет и возврат, и тело после него; C# выдаёт ожидаемый `CS0162` на `Distillery.cs:124`, предупреждение не подавлено. Прямой вызов отключённого метода оставляет дверь запертой; основной путь отпирает её.
- **Разные строки реакции испуга.** В событии стража на `06_distillery.hps:362` таймер называется `react_scare.snt`; дополнительный урон внутри `TimerStopLookAt` на :372 требует точного совпадения с `react_scare`. Поэтому эта реакция проигрывает GUI-звук, но не вызывает этот дополнительный урон. Расширение и сравнение сохранены.
- **Точный третий и четвёртый шаг.** Разрушение требует `PulleyBreak == 3` (:200), снятие коллбэка люка — `CountTurn == 4` (:431), а не превышения порогов. Сброс `PulleyBreak`/`HoldPulley` на выходе оставлен. При первом физическом шаге уже возникает одно пересечение блокиратора с `AreaPullPulley`; в отдельной проверке оно сбрасывается действующим `CollideInPulley` перед последовательностью 1 → 2 → 3.
- **Комментарий и длительность блокировки.** На :204 комментарий говорит о половине секунды, но таймер на :206 равен 0.3 с. Сохранены оба; в прогоне управление сначала выключено, затем возвращается.
- **Повторные таймеры одного имени.** Все 30 вызовов открытия двери с одним именем (:243–:245), повторные удары и реакции оставлены отдельными вызовами. Они не заменены единственным таймером или циклом в обработчике.

## Не перенесено движком

| API `NotPorted` | Сообщение `skipped, needs` | Последствие |
|---|---|---|
| `InteractConnectPropWithRope` | `interact connections` | четыре вызова соединения рукоятки с `Rope01`/`RopeArea_1..3` сохраняются, но вращение не натягивает верёвки и не поднимает люк |
| `AutoSave` | — | с 2026-10-17 пишет автосохранение: сохранения перенесены (CLAUDE.md «Сохранения») |

Дополнительные ограничения существующего движка:

- Страж — `EnemyGrunt` (враги перенесены (6.2–6.3, CLAUDE.md «Враги»)): патруль, автоотключение в конце маршрута и `OnAutoDisabled` → `EnemyDisabled`. `GetEntitiesCollide(grunt_normal_1, AreaGruntGone)` проверяет форму тела и у уже выключенного врага, как HPL2: прогон с `TeleportEnemyToEntity grunt_normal_1 AreaGruntGone` даёт «Disabling enemy» и «Leaving track behind».
- `SetPropActiveAndFade` включает пропы сразу; плавное проявление за 0.5 с ожидает dissolve-материалы. `CheckPoint` ставит точку возрождения (смерть и возрождение перенесены, см. CLAUDE.md «Урон и смерть»); исходный коллбэк `CheckPoint01` пуст.
- В карте есть четыре Rope и девять SlimeDamage-областей; их механики отсутствуют. Две Insanity-области работают (события безумия перенесены (CLAUDE.md «События безумия»)). `FogArea` пропускается импортёром.
- При импорте трёх `rope_beam02` получено 12 предупреждений о привязке ID 6/4/3/2 к телам 1/2/3/4. Эти ID существуют в `source/entities/equipment/rope_beam/rope_beam02.ent:17–:24` как кости, ссылки тел — :35, :41, :47, :53. Это ограничение привязки скелета импортёром, а не отсутствующие узлы оригинала; импортёр не менялся.

## Проверка

Unity 6000.6.2f1 запускался последовательно, с проверкой отсутствия `Temp/UnityLockfile` и доступом к действующей лицензии Windows. Все четыре итоговые симуляции завершились кодом 0; исключений, `PortError`, `does not exist in`, несовпадений числа аргументов и ошибок компиляции нет. Единственный `[Script] ERROR` разобран выше.

### Импорт

Команда (пути от корня репозитория; лог фактически задан абсолютным путём внутри Unity-проекта):

```text
Unity.exe -batchmode -nographics -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/06_distillery.map -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Import_06_distillery.log
```

```text
=== Level: maps/main/ch01/06_distillery.map — 2026-10-16 14:59:30 — 15 warnings, 0 errors ===
Resources: 122 models (27 imported now), 17 extra materials (0 converted now), 0 cookies
```

Предупреждения: 12 о привязке костей `rope_beam02`, одно о `FogArea`, одно об отсутствующем неиспользуемом изображении скина `gui_amn_mm_textbox_marker.tga`, одно об ещё отсутствовавшем порте скрипта. После создания класса симуляции находят `Distillery`, 385 сущностей скрипта.

Импорт создал сцену и добавил её в `ProjectSettings/EditorBuildSettings.asset`. Это ожидаемое изменение сохранено. Отдельный снимок отчёта импорта до симуляций — `UnityProj/UMnesiaRTX/Logs/06_distillery_import_report.log`; полный лог Unity — `Logs/Import_06_distillery.log`.

### Сверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 06_distillery -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Distillery.cs
```

```text
06_distillery : функций 34, строк 180, ошибок 0, предупреждений 0
```

Дополнительная лексическая сверка подтвердила совпадение 2403 токенов после нормализации синтаксиса C# и всех 74 исходных комментариев. Исходные `.hps`/`.map` не изменялись. Единственное новое предупреждение компилятора — описанный `CS0162` в отключённой функции бочек.

### Симуляции

Общий запуск (для каждого набора ниже; `$script` — последовательность команд):

```powershell
# Unity.exe: C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe
Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/06_distillery.map -seed 1 -script "$script" -logFile <полный путь к логу>
```

| Лог в `UnityProj/UMnesiaRTX/Logs/` | Доп. флаг | Предупреждения | Ошибки | Результат |
|---|---|---|---|---|
| `Sim_06_distillery.log` | — | 3 | 0 | 30 с старта, музыка, голоса, скрип/пыль и пустой сосуд |
| `Sim_06_distillery_events.log` | — | 4 | 0 | десять разных основных обработчиков, все три HowlArea; 3 заглушки + исходный звук `enemy` |
| `Sim_06_distillery_pulley.log` | `-scriptDebug` | 3 | 0 | сброс удержания, блокиратор 1 → 2 → 3, квест и CountTurn = 4 |
| `Sim_06_distillery_traces.log` | `-scriptDebug` | 3 | 0 | появление стража, обе ветки следов, пустые коллбэки, OnLeave без смены карты |

Все три предупреждения базовых прогонов — `NotPorted` из таблицы выше. В первом черновом прогоне событий две диагностические команды запрашивали отсутствующие геттеры; они заменены существующим `QuestIsCompleted` и командой `body`. Итоговый `Sim_06_distillery_events.log` — повтор с корректными командами, без `No function`; код порта при этом не менялся. Старый раздел остаётся в дописываемом `AmnesiaImport.log`.

**Старт:** `watch 30 2; shot start`. Игрок остаётся на полу, управление включено, музыка `06_amb.ogg`, три повторных таймера, голоса/скрип/пыль работают; выдан `empty_container`. Отладочная ветка дополнительно выдаёт фонарь и десять огнив. Удаление старых именных сосудов сохранено 1:1; перенос инвентаря с предыдущей карты отдельно не симулировался.

**Основные события:** после каждого вызова обработчика — `watch 10 2`. Вызваны:

```text
call FuncMoanHorrors Player AreaBeginAnimal 1
call InteractBarrelBlock barrel01_12
call InteractLastDoor cellar_wood01_1
call EnterEmptyStorageDoorOpen Player EmptyStorageDoorOpen 1
call EnterBottleRoomWallScratch Player BottleRoomWallScratch 1
call EnterHowlArea Player HowlArea_1 1
call EnterHowlArea Player HowlArea_2 1
call EnterHowlArea Player HowlArea_3 1
call EnterEnemyGlimpse Player EnemyGlimpse 1
call CollideEnemyHint Player AreaEnemyHint 1
call CollideAreaRoses Player AreaRoses_1 1
call InteractHatch hatch_large_1
```

Перед пустой кладовой её дверь закрыта диагностическим `SetSwingDoorClosed`. Проверены отпирание `cellar_wood01_1`, открытие `cellar_wood01_5`, три значения воя (`HowlCount = 3`), `QuestIsCompleted 06BlockedDoor = True`, отключение `block_box_1`, `grunt_normal_1 active True`, музыка и чекпоинт `check01`. Звуки/эффекты, лепестки и оба таймера тяжёлого люка завершились без исключений. В этой искусственно плотной последовательности рассудок на время достигает обморока; к концу он восстанавливается, крен и размытие — 0, управление включено. Смена карты не вызывалась.

**Шкив:** после первоначальных 2 с вызваны `CollideBlockingBarellsArea`, `InteractPulley`, выход `CollideInPulley`, `InteractCrank`, вход `CollideInPulley` — с `watch 10 2` после каждого. Получены `HoldPulley: 1 → 0`, `PulleyBreak = 0`, подсказка о выскальзывании и `QuestIsAdded 06BlockedPulley = True`. Затем три прямых вызова `CollidePulleyBlock wood_blocking_pulley_1 AreaPullPulley` со состояниями 1, -1, 1 и ожиданиями дают счётчики 1, 2, 3. Третий вызов действительно разбивает проп: зарегистрированный картой `BreakPulleyBlocker` завершается естественно, `GetEntityExists wood_blocking_pulley_1 = False`, `QuestIsCompleted 06BlockedPulley = True`. Сразу после него управление выключено (короткий `watch 0.1 0.05`), через таймер возвращается; ошибок света/имён нет.

Четыре вызова `CollideAreaRope` для чередующихся `AreaRope_1`/`AreaRope_2` с ожиданиями доводят `CountTurn: 0 → 4`. Перед первым и четвёртым вызовами запущен `InteractHatch`; на четвёртом шаге отменяются его таймеры. Остаются только три таймера окружения. Это проверка обработчиков и состояний, а не физического поднятия люка верёвкой.

**Следы:** у реального `grunt_normal_1` прямой `EnemyDisabled` оставляет следы выключенными. Для покрытия истинной ветки неподвижный `barrel01_12` диагностическими `SetPropStaticPhysics`/`SetEntityPos` помещён в HPL-позицию области (64.75, -7, 6.25). Проверка `GetEntitiesCollide barrel01_12 AreaGruntGone = True`, затем `EnemyDisabled barrel01_12 OnAutoDisabled` включает следы: нога и кровь имеют `active True`, проигрывается закрытие двери. Подмена аргумента только в тестовой команде; код коллбэка не изменён. Пустые `CheckPoint01 check01 1`, `EntityCallDiary ... OnPickup` и `OnLeave` вызваны отдельно с ожиданиями, без исключений и без `ChangeMap`.

Контрольные снимки: `Logs/Player_06_distillery_00_start.png`, `..._00_events.png`, `..._00_pulley.png`, `..._00_traces.png` (вне git). Просмотрены старт и кадр после проверки шкива; сцена тёмная, туман и источники света присутствуют.

### Регрессия

Оба запуска выполнены с GPU, без `-quit` и без `-nographics`; каждый завершился кодом 0:

```text
Unity.exe -batchmode -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MapChangePreview.RunBatch -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Regression_06_distillery_MapChange.log
Unity.exe -batchmode -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MainMenuPreview.RunBatch -logFile Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/Regression_06_distillery_MainMenu.log
```

Строки результатов из логов:

```text
PASS: 60 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 14 main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).
PASS: 14 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

Сверка текущей карты повторена после симуляций: 34 функции, 180 строковых констант, 0 ошибок, 0 предупреждений. Карты 07/08 остаются за пределами текущего задания пользователя; их импорт/порт не запускался.

## Файлы и состояние репозитория

Созданы только `Distillery.cs`, его Unity `.meta` и этот журнал. В строке этапа 5 `CLAUDE.md`/`AGENTS.md` отмечена только текущая карта; файлы инструкций побайтно одинаковы. Сцену в список сборки добавил импортёр. После выхода последней Unity-сессии автоматически пересохранённый `TimeManager.asset` возвращён командой `git checkout -- UnityProj/UMnesiaRTX/ProjectSettings/TimeManager.asset`; остальные настройки проекта не изменились. После тестов Unity закрыт, lock-файла нет. Коммитов и push нет; ресурсы остаются в игнорируемой папке.

Итоговый `git status --short`:

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Distillery.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Distillery.cs.meta
?? docs/ScriptPort/ch01/06_distillery.md
```
