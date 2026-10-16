# 05_wine_cellar

- **Оригинал:** `source/maps/main/ch01/05_wine_cellar.hps` (Latin-1, 906 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellar.cs` (класс `RTXMnesia.Levels.Ch01.WineCellar`, `[HplMapScript("maps/main/ch01/05_wine_cellar.map")]`).
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/05_wine_cellar.unity` (генерируется, вне git).
- **Статус:** все 45 функций перенесены; проверены старт, десять вызовов событий, полный обвал с бочкой, четыре химиката, событие стража и отключённые коллбэки чердака. Ограничения и ошибки оригинала описаны ниже. Дата проверки: 2026-10-16.

## Функции и глобальные переменные

Глобальных переменных в оригинальном скрипте нет; состояние хранится в переменных API. Все методы ниже — в классе `WineCellar`, в исходном порядке.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `FuncUnlockDoor` (:5) | `FuncUnlockDoor` | готов | разблокировка двери после перемещения бочки |
| `FuncMoanHorrors` (:21) | `FuncMoanHorrors` | готов | начальные звуки, таймеры голосов/шагов и урон рассудку |
| `FuncMoanTimer` (:37) | `FuncMoanTimer` | готов | обе ветки, включая повторный RandFloat для шагов |
| `CreakTimer` (:61) | `CreakTimer` | готов | пыль, скрип и повторный таймер |
| `TimerSanity` (:76) | `TimerSanity` | готов | урон рассудку |
| `PickOil` (:88) | `PickOil` | готов | исходный пустой метод с отключённым кодом |
| `CollideActivateGrunt` (:107) | `CollideActivateGrunt` | готов | сохранён неиспользуемый метод открытия двери |
| `CollideBlockDoor` (:122) | `CollideBlockDoor` | готов | выбор одной из трёх бочек |
| `PickCoin` (:128) | `PickCoin` | готов | коллбэк `Chemical_2`, начало обвала, увеличение `ChemJars` |
| `TimerActiveRocks` (:154) | `TimerActiveRocks` | готов | все пять камней |
| `TimerRockEvents` (:158) | `TimerRockEvents` | готов | все состояния 1–16, удар, обморок, пробуждение и восстановление |
| `CollideStickLadder` (:320) | `CollideStickLadder` | готов | исходный немедленный возврат; тело после него сохранено |
| `CollideLoft` (:328) | `CollideLoft` | готов | исходный немедленный возврат; тело после него сохранено |
| `TimerLoft` (:342) | `TimerLoft` | готов | все пять веток отключённого события чердака |
| `GiveHintPush` (:381) | `GiveHintPush` | готов | коллбэки обломков из карты |
| `CollideGiveSanity` (:385) | `CollideGiveSanity` | готов | восстановление рассудка после выхода |
| `PickChemical` (:393) | `PickChemical` | готов | химикаты 1/3/4, свет, дверь, счётчик и музыка успеха |
| `TimerDoorForce` (:436) | `TimerDoorForce` | готов | пять толчков двери и остановка звука на пятом |
| `PickNoteTrans` (:444) | `PickNoteTrans` | готов | музыка записки, коллбэк карты |
| `TimerBloodDrop` (:454) | `TimerBloodDrop` | готов | чередование частиц и звука, исходный порядок выбора номера |
| `TimerChain` (:471) | `TimerChain` | готов | силы на шесть невидимых тел, динамическое имя повторного таймера |
| `CollideActiveCeiling` (:480) | `CollideActiveCeiling` | готов | включение/выключение области взгляда |
| `LookAtCeiling` (:487) | `LookAtCeiling` | готов | обе ветки, сепия, звук и отключение областей |
| `CollideAreaRemoveDoorEvent` (:507) | `CollideAreaRemoveDoorEvent` | готов | снятие коллбэка двери |
| `InteractDoorWithGruntBehind` (:513) | `InteractDoorWithGruntBehind` | готов | шум бутылок, `HeardRumble` и альтернативный выход |
| `InteractDoorToHole` (:529) | `InteractDoorToHole` | готов | шаги и таймеры ящиков при повторном шуме |
| `TimerPlaySteps` (:541) | `TimerPlaySteps` | готов | все три ящика |
| `CollideHoleEvents` (:549) | `CollideHoleEvents` | готов | условие `HeardRumble`, камни, пыль и звук |
| `TimerGiveSanityDamage` (:563) | `TimerGiveSanityDamage` | готов | урон рассудку |
| `CollideAreaOtherEscape` (:568) | `CollideAreaOtherEscape` | готов | обе двери, бочка и таймеры открытия |
| `TimerPushTheDumbDoor02` (:586) | `TimerPushTheDumbDoor02` | готов | импульс двери альтернативного выхода |
| `CollideBangDoor` (:597) | `CollideBangDoor` | готов | четыре таймера стука и эффекты |
| `TimerBangDoor` (:610) | `TimerBangDoor` | готов | все четыре ветки, пыль/толчки/реакции и разблокировка |
| `PickEnemy` (:643) | `PickEnemy` | готов | коллбэк `potion_health_2`, обе защиты от запуска |
| `LookAtEnemy` (:670) | `LookAtEnemy` | готов | ветка взгляда, таймеры и патруль |
| `CollideActivateEnemy` (:681) | `CollideActivateEnemy` | готов | альтернативная ветка запуска таймеров и патруля |
| `TimerEnemy` (:691) | `TimerEnemy` | готов | `speed`, `enemy1..3` и заключительная ветка |
| `CollideDeactivateEnemy` (:727) | `CollideDeactivateEnemy` | готов | выключение стража, двери/бочка, снятие таймера, восстановление скорости |
| `TimerPushTheDumbDoor` (:758) | `TimerPushTheDumbDoor` | готов | исходный отдельный метод, не объединён с версией 02 |
| `CollideEnemyHint` (:763) | `CollideEnemyHint` | готов | подсказка врагов |
| `InteractTouchIt` (:773) | `InteractTouchIt` | готов | звук прикосновения и таймер повторного коллбэка |
| `TimerTouchIt` (:781) | `TimerTouchIt` | готов | повторная регистрация взаимодействия |
| `OnStart` (:789) | `WineCellar.OnStart` | готов | все коллбэки, звук, таймеры, переменные, туман и отладка |
| `OnEnter` (:874) | `WineCellar.OnEnter` | готов | все preload-вызовы, музыка и `AutoSave` |
| `OnLeave` (:899) | `WineCellar.OnLeave` | готов | выключение стража и исходные параметры экрана загрузки |

## Отличия от оригинала

- Синтаксис C#: строковые ссылочные параметры AngelScript переведены в `string`, десятичные литералы имеют `f`, четыре присваивания RandFloat целым явно усекаются, `or` заменён на `||`, методы жизненного цикла — `public override`.
- Статические имена функций-коллбэков используют `nameof`; динамические `asTimer` / `sEvent` остаются строками. Конкатенаций строки с float в действующем коде нет, помощник `Str` не нужен.
- Сохранены все 131 комментарий оригинала, отключённое создание цепей, пустой `PickOil`, неиспользуемые функции и недостижимый код после возвратов в событиях чердака. Все ветки `switch` имеют исходный `break`; провалов между `case` нет.
- Движок, API, импортёр, готовые порты 00–04 и их журналы, `docs/ScriptPort/api.md` не менялись.

## Особенности оригинала (сохранены)

- **Некорректное имя коллбэка в карте.** `05_wine_cellar.map:4391` задаёт у активной `barrel01_29` (`:4386`) `CallbackFunc` со значением `0,0,0.5`. В оригинальном `.hps` такой функции нет, это также не допустимое имя метода C#. Данные карты не менялись, фиктивный обработчик не добавлялся. Сверка справедливо оставляет одно предупреждение об ошибке оригинала. В четырёх прогонах этот коллбэк бочки не срабатывал; при его вызове движок сможет сообщить об отсутствующей функции.
- **Кровь до инициализации номера.** Первый `TimerBloodDrop`, запущенный из `05_wine_cellar.hps:837`, на :462 строит имя области из ещё нулевой переменной `Blood`. Случайный номер назначается только на :465–:467. Области `AreaEndBlood_0` в карте нет, есть только 1–3 (`.map:10654`, :10664, :10684). Через 2.5 с появляется предупреждение об отсутствующей сущности; оно сохранено вместе с исходным порядком действий.
- **Отсутствующий факел.** `05_wine_cellar.hps:188` гасит `torch_static01_1`, которого в `.map` нет (проверены все узлы XML с `Name`). В прогоне обвала сохранено предупреждение о недостающей лампе; имя не исправлялось.
- **Отключённые лестница и чердак.** `05_wine_cellar.hps:322`, :330 немедленно возвращаются: оригинал отключил эти события, поскольку на лестнице нельзя присесть. В `.map:9378` `LadderArea_1` неактивна. Порт оставляет возвраты, исходные комментарии и весь последующий код; `TimerLoft` тоже перенесён полностью. C# выдаёт два ожидаемых `CS0162` о недостижимом коде, предупреждения не подавлены. Прямые вызовы обеих функций не запускают событие и не активируют лестницу.
- **Дополнительный шаг обвала.** Условие на `05_wine_cellar.hps:316` планирует ещё один таймер после состояния 16. Итоговый счётчик `TimerRockEvents` = 17; граница не изменена.
- **Повторные таймеры с одинаковыми именами.** Пары толчков двери на `05_wine_cellar.hps:579`, :750 и другие повторения оставлены отдельными вызовами. `TimerChain` повторяется по имени переданного таймера, как в оригинале.
- **Фонарь после обвала.** На :237–:238 фонарь выключается и блокируется, на :289 снова разрешается, но автоматически не зажигается. Проверено именно это поведение: после события он выключен и снова включается по запросу.

## Не перенесено движком

| API `NotPorted` | Сообщение `skipped, needs` | Последствие |
|---|---|---|
| `AutoSave` | `saving (step 6)` | вход не создаёт сохранение на диск |

`AddEnemyPatrolNode` больше не заглушка: враги перенесены (6.2–6.3, CLAUDE.md «Враги»). Прогон: `grunt_normal_1` идёт к `PathNodeArea_3`, на `AreaEnemyEvent_3` скрипт его выключает, играет музыка опасности; у него в карте `DisableTriggers=true`, игрока он не замечает (постановочный враг, как в оригинале). Скриптовые включение/выключение `grunt_normal_1`, звук, двери, подсказки и реакции игрока выполняются. В карте также есть Sticky и Rope-области без соответствующих игровых механик; Insanity-области работают (события безумия перенесены (CLAUDE.md «События безумия»)). Создание верёвок через `CreateRope` закомментировано уже в оригинале (:828–:833), поэтому этот API скрипт не вызывает. `TimerChain` по-прежнему толкает шесть тел. Отключение чердака описано отдельно выше: оно сохранено из оригинала.

Импортёр пропускает `FogArea`. Обходов и исправлений движка в задаче нет.

## Проверка

Unity 6000.6.2f1 запускался последовательно, с проверкой отсутствия `Temp/UnityLockfile`, с доступом к действующей лицензии Windows. Во всех четырёх симуляциях код выхода 0; нет исключений, `PortError`, `does not exist in` и несовпадения числа аргументов. Все предупреждения о сущностях сверены с XML карты.

### Импорт

```powershell
$unityExe = 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe'
$unityProject = 'Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX'
& $unityExe -batchmode -nographics -quit -projectPath $unityProject -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/05_wine_cellar.map -logFile "$unityProject/Logs/Import_05_wine_cellar.log"
```

Повторный импорт: **3 warnings, 0 errors**, код выхода 0; 129 моделей, 16 дополнительных материалов, 2 cookie. Сцена уже была в проекте и списке сборки. Копия лога импорта: `Logs/05_wine_cellar_import_report.log`.

| Предупреждение | Количество |
|---|---:|
| `FogArea` не поддержана | 1 |
| Отсутствующая картинка skin меню `gui_amn_mm_textbox_marker.tga`, не используемая этим меню | 1 |
| Отсутствие порта на момент импорта | 1 |

После добавления C# карта находит `WineCellar`; отдельный переимпорт для подключения не требуется. Ресурсы остаются вне git.

### Сверка и компиляция

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 05_wine_cellar -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellar.cs
```

```text
предупреждение: коллбэк карты 0,0,0.5 нет и в оригинале .hps (ошибка оригинала, в порт не добавлять)
05_wine_cellar : функций 45, строк 265, ошибок 0, предупреждений 1
```

Единственное предупреждение разобрано выше; сверка не менялась и не обходилась. Дополнительная сверка подтвердила совпадение последовательности 4673 токенов после явных преобразований синтаксиса C# и сохранение всех 131 комментария (без учёта отступов/пробелов).

Ошибок компиляции нет. Два собственных предупреждения `CS0162` относятся к сохранённым возвратам оригинала перед кодом чердака (`WineCellar.cs:336`, :344). Остальные предупреждения компилятора — существующие `shapeRadius` в `PlayerDarkness`/`PlayerHands` и неиспользуемое поле `OldArchives`.

### Старт

```powershell
& $unityExe -batchmode -quit -projectPath $unityProject -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/05_wine_cellar.map -seed 1 -script 'watch 30 2; shot start' -logFile "$unityProject/Logs/Sim_05_wine_cellar.log"
```

Результат: **2 warnings, 0 errors** — `AutoSave`, отсутствующая `AreaEndBlood_0`. Музыка `04_amb`, окружающие звуки, скрипы/пыль, кровь и таймер сил цепей работают. `Logs/Player_05_wine_cellar_00_start.png` просмотрен.

### Десять вызовов основных событий

Тот же запуск с логом `Logs/Sim_05_wine_cellar_events.log`, строка `-script` ниже. После каждого вызова — `watch 10 2`:

```text
call FuncUnlockDoor AreaUnlockDoor BarrelBlock -1; watch 10 2;
call FuncMoanHorrors Player AreaBeginMoan 1; watch 10 2;
call InteractDoorWithGruntBehind cellar_wood01_3; watch 10 2;
call InteractDoorToHole cellar_wood01_4; watch 10 2;
call CollideHoleEvents Player AreaHoleEvents 1; watch 10 2;
call CollideBangDoor Player AreaBangDoor 1; watch 10 2;
call LookAtCeiling AreaCeiling 1; watch 10 2;
call LookAtCeiling AreaCeiling -1; watch 10 2;
call CollideAreaOtherEscape Player AreaOtherEscape 1; watch 10 2;
call InteractTouchIt TouchIt; watch 10 2; shot events
```

| Проверка | Результат |
|---|---|
| Заблокированная дверь | разблокируется |
| Голоса и тяжёлые шаги | первоначальный испуг и повторные таймеры |
| Двери, бутылки, провал | `HeardRumble` проходит необходимые условия, появляются ящики и камни, пыль и звуки |
| Стук в дверь | все четыре таймера, толчки, пыль, урон рассудку, снятие блокировки и затухание эффектов |
| Тела под потолком | обе ветки взгляда, сепия/звук включаются и гасятся, области выключаются |
| Альтернативный выход | двери 3/6 открываются, бочка получает импульс, оба таймера толчка выполняются |
| Прикосновение | звук и повторная регистрация взаимодействия |

Результат: **2 warnings, 0 errors** — только исходная кровь и `AutoSave`. Вызовов `ChangeMap` не было.

### Полный обвал и четыре химиката

Тот же запуск, `-seed 1`, лог `Logs/Sim_05_wine_cellar_debris.log`, строка `-script`:

```text
call GiveItemFromFile lantern lantern.ent; call SetLanternActive true true;
call AddQuest 04ChemicalsMoved 04ChemicalsMoved;
call PickChemical Chemical_1 OnPickup; watch 10 2;
goto -13.78 -4 30.57 180;
call CollideBlockDoor Player AreaBlockDoor_2 1;
call PickCoin Chemical_2 OnPickup; watch 35 2; shot awake; watch 45 5;
call GetLocalVarInt TimerRockEvents; call GetPlayerHealth; call GetLanternActive;
call SetLanternActive true true; call GetLanternActive;
call SetSwingDoorClosed cellar_wood01_1 false false;
call PickChemical Chemical_3 OnPickup; watch 10 2;
call PickChemical Chemical_4 OnPickup; watch 10 2;
call GetGlobalVarInt ChemJars; call GetLocalVarString WhatBarrel; shot four_chemicals
```

Предусловие задания соответствует прочитанной записке карты 04; химикаты здесь проверялись через их исходные коллбэки подбора. Игрок помещён в комнату обвала, выбранная бочка — `AreaBlockDoor_2`. Подтверждены:

- все 16 состояний, включение камней, импульсы балок/бочек, пыль, тряска, звук, удар и обморок;
- `TimerRockEvents` = 17, здоровье = 80, управление восстановлено: движение/бег/взгляд/FOV = 1, крен = 0;
- после ожидания полностью погасли затемнение, сепия и размытие;
- фонарь после события выключен, но снова разрешён: следующий запрос его включает;
- ветка третьего химиката выполнила все пять `TimerDoorForce`;
- `ChemJars` = 4, задание `04ChemicalsMoved` завершено, играет `02_puzzle.ogg`.

Результат: **3 warnings, 0 errors** — `AutoSave`, `AreaEndBlood_0`, отсутствующий `torch_static01_1`. Сохранены снимки `Logs/Player_05_wine_cellar_00_awake.png` и `Player_05_wine_cellar_01_four_chemicals.png`; снимок пробуждения просмотрен.

### Страж, защитные флаги и отключённый чердак

Дополнительный прогон с `-scriptDebug` (фонарь и огнива выдаёт сам отладочный блок оригинала), `-seed 1`, лог `Logs/Sim_05_wine_cellar_enemy.log`, строка `-script`:

```text
start PlayerStartArea_6; call SetLanternActive true true;
call SetGlobalVarInt IGF 1; call PickEnemy potion_health_2 OnPickup; watch 10 2;
call GetLocalVarInt EnemyEventDone;
call SetGlobalVarInt IGF 0; call PickEnemy potion_health_2 OnPickup; watch 10 2;
body grunt_normal_1; call GetLocalVarInt EnemyEventDone;
call PickEnemy potion_health_2 OnPickup;
call LookAtEnemy AreaHoleEvents 1; watch 12 2; aim grunt_normal_1; shot enemy;
call CollideDeactivateEnemy grunt_normal_1 AreaEnemyEvent_3 1; watch 10 2;
body grunt_normal_1;
call CollideEnemyHint Player AreaEnemyHint 1; watch 10 2;
call CollideStickLadder StickyArea_1 ladder_1_Body_1; watch 10 2;
call CollideLoft Player AreaLoft 1; watch 10 2;
call GetLocalVarInt EnemyEventDone
```

При `IGF=1` событие не начинается, `EnemyEventDone` остаётся 0. После снятия флага запускается один раз, страж активен, флаг = 1; повторный `PickEnemy` ничего не дублирует. Ветка взгляда запускает таймеры `enemy2..4`, скорости восстанавливаются. Прямой коллбэк выхода выключает стража, закрывает дверь 4, открывает проход через двери 3/6, толкает бочку и восстанавливает скорость через `speed`. Подсказка врагов отображается. Коллбэки лестницы/чердака немедленно возвращаются и не запускают отключённое событие.

Результат: **3 warnings, 0 errors** — `AutoSave`, `AreaEndBlood_0`, `AddEnemyPatrolNode`. Сущность стража проверена через `body` до и после выхода: active True → False. Проверка патруля/атаки не заявляется: ИИ отсутствует в движке.

### Регрессия

Запуски с GPU, без `-quit` и без `-nographics`; оба завершились с кодом 0:

```powershell
& $unityExe -batchmode -projectPath $unityProject -executeMethod RTXMnesia.EditorDev.MapChangePreview.RunBatch -logFile "$unityProject/Logs/Regression_05_wine_cellar_MapChange.log"
& $unityExe -batchmode -projectPath $unityProject -executeMethod RTXMnesia.EditorDev.MainMenuPreview.RunBatch -logFile "$unityProject/Logs/Regression_05_wine_cellar_MainMenu.log"
```

```text
PASS: 60 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 14 main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).
PASS: 14 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

В завершение первой партии повторена сверка всех трёх портов, без изменения предыдущих карт:

```text
03_archives : функций 57, строк 400, ошибок 0, предупреждений 0
04_wine_cellar_lab : функций 43, строк 295, ошибок 0, предупреждений 0
05_wine_cellar : функций 45, строк 265, ошибок 0, предупреждений 1
```

Единственное предупреждение — исходный некорректный коллбэк бочки, описанный выше. Автоматическое пересохранение `ProjectSettings/TimeManager.asset` возвращено командой из задания; исходный шаг 1/60 сохранён. Другие настройки проекта не изменились.

## Изменённые файлы и итоговый git status

Новые файлы:

- `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellar.cs`;
- `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellar.cs.meta` (создан Unity);
- `docs/ScriptPort/ch01/05_wine_cellar.md`.

В `CLAUDE.md` и `AGENTS.md` добавлена только карта `05_wine_cellar` в строку этапа 5; файлы побайтно одинаковы. Сцена уже была в списке сборки и профиле Windows, их изменений нет. Работа ограничена картой 05; проверки предыдущих портов были только чтением. Коммиты и push не выполнялись.

```text
 M AGENTS.md
 M CLAUDE.md
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellar.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/WineCellar.cs.meta
?? docs/ScriptPort/ch01/05_wine_cellar.md
```
