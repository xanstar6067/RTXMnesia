# 03_archives

- **Оригинал:** `source/maps/main/ch01/03_archives.hps` (Latin-1, 1446 строк).
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Archives.cs` (класс `RTXMnesia.Levels.Ch01.Archives`, `[HplMapScript("maps/main/ch01/03_archives.map")]`).
- **Сцена:** `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/Levels/maps/main/ch01/03_archives.unity` (генерируется, вне git).
- **Статус:** все 57 функций и 6 глобальных переменных перенесены; проверены старт, десять отдельных событий, полное воспоминание со сферой, возврат фонаря и головоломка с книгами. Ограничения движка и ошибки оригинала перечислены ниже. Дата проверки: 2026-10-16.

## Функции и глобальные переменные

Номера строк относятся к оригинальному `.hps`; методы и поля находятся в классе `Archives`. Порядок совпадает с оригиналом.

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| глобальная `iLength` (:5) | поле `iLength` | готов | длина массива; начальное значение 0 |
| глобальная `iEnd` (:6) | поле `iEnd` | готов | оставшиеся уникальные числа; начальное значение 0 |
| глобальная `iRand` (:7) | поле `iRand` | готов | временный индекс; начальное значение 0 |
| глобальная `aRand` (:8) | поле `aRand` | готов | выделяется при инициализации генератора |
| глобальная `iUniqueRand` (:9) | поле `iUniqueRand` | готов | результат; начальное значение 0 |
| `UniqueRandom` (:10) | `UniqueRandom` | готов | усечение float в int; исходная включённая верхняя граница сохранена |
| `MapFlashbackOver` (:50) | `MapFlashbackOver` | готов | коллбэк Flashback из карты; восстановление эффектов при активном воспоминании |
| `PickDiary` (:65) | `PickDiary` | готов | все три дневника; начало воспоминания после третьего |
| `InteractOrb` (:91) | `InteractOrb` | готов | переход к завершению воспоминания |
| `TimerFlashBack` (:110) | `TimerFlashBack` | готов | все состояния 1–18, ожидание голосов и возврат к дневнику |
| `TimerExtraVoiceEffects` (:360) | `TimerExtraVoiceEffects` | готов | все имена дополнительных таймеров и звуков |
| `DiaryPartsOver` (:396) | `DiaryPartsOver` | готов | возобновление по `VoiceResume` |
| `DiaryPartsLast` (:406) | `DiaryPartsLast` | готов | заключительные реплики и звуки |
| `CollideOrb` (:414) | `CollideOrb` | готов | обе области пути к сфере |
| `LookAtOrb` (:423) | `LookAtOrb` | готов | включает взаимодействие со сферой |
| `CollideSlowArea` (:437) | `CollideSlowArea` | готов | последовательное подключение четырёх замедляющих областей |
| `TimerRandRock` (:486) | `TimerRandRock` | готов | тихое прерывание при ошибке индекса в `UniqueRandom` |
| глобальная `bRoll` (:507) | поле `bRoll` | готов | исходное значение true |
| `TimerRandomLook` (:508) | `TimerRandomLook` | готов | случайное покачивание и завершение по `Dizzy` |
| `StopRandomLook` (:551) | `StopRandomLook` | готов | сохранена и неиспользуемая функция |
| `CollideBeginPiano` (:625) | `CollideBeginPiano` | готов | обе ветки: первое и второе пианино |
| `CollideCloseLid` (:655) | `CollideCloseLid` | готов | крышка, пыль, урон рассудку |
| `FallBook` (:669) | `FallBook` | готов | основной вызов и три специальных имени таймера |
| `InteractLibDoor` (:696) | `InteractLibDoor` | готов | коллбэк двери из карты |
| `TimerLockedDoorMess` (:700) | `TimerLockedDoorMess` | готов | сообщение и задание закрытой библиотеки |
| `EntityCallBreakWall` (:706) | `EntityCallBreakWall` | готов | коллбэк разрушения стены из карты |
| `CollideSecretBook` (:716) | `CollideSecretBook` | готов | вход, выход, таймер возврата и все три книги |
| `PushBackBook` (:779) | `PushBackBook` | готов | первая фаза возврата книги |
| `PushBackBook02` (:789) | `PushBackBook02` | готов | все три книги |
| `VisionOverBooks` (:798) | `VisionOverBooks` | готов | коллбэк Flashback из карты, задание книг и эффекты |
| `TimerPlayEnemy` (:816) | `TimerPlayEnemy` | готов | звук врага |
| `InteractMoveShelf` (:821) | `InteractMoveShelf` | готов | сообщение полки |
| `PickKey` (:828) | `PickKey` | готов | глобальная переменная ключа и событие у двери |
| `TimerEGrunt` (:839) | `TimerEGrunt` | готов | все состояния 1–8 |
| `CloudEffects` (:919) | `CloudEffects` | готов | сохранён отключённый в оригинале эффект облака |
| `CollideCloudThing` (:938) | `CollideCloudThing` | готов | все четыре ветки; регистрация в оригинале закомментирована |
| `TimerThing` (:953) | `TimerThing` | готов | сохранены ошибочные имена источников звука |
| `CollideJarLeaveArea` (:997) | `CollideJarLeaveArea` | готов | разбивание вазы и освобождение облака |
| `CollideLastPiano` (:1008) | `CollideLastPiano` | готов | последнее пианино после ключа |
| `TimerBloodDrop` (:1021) | `TimerBloodDrop` | готов | чередование звука и частиц крови |
| `TimerCrawl` (:1035) | `TimerCrawl` | готов | случайные звуки и пыль; float в отладочной строке через `Str` |
| `PickVillageNote` (:1053) | `PickVillageNote` | готов | музыка записки |
| `CollideHint` (:1079) | `CollideHint` | готов | подсказка броска предмета |
| `PlayerInteractSecretPassage` (:1089) | `PlayerInteractSecretPassage` | готов | счётчик быстрых ударов, частицы и разрушение |
| `TimerDecreaseHitCount` (:1117) | `TimerDecreaseHitCount` | готов | уменьшение счётчика |
| `CollideCaveIn` (:1130) | `CollideCaveIn` | готов | обвал, области событий, запирание двери, свет |
| `Interactmansion_2` (:1164) | `Interactmansion_2` | готов | сообщение запертой двери |
| `TimerGruntDoorBackon` (:1171) | `TimerGruntDoorBackon` | готов | повторное подключение коллбэка двери |
| `TimerCaveIn` (:1178) | `TimerCaveIn` | готов | все четыре имени таймера |
| `CollideCaveInQuest` (:1201) | `CollideCaveInQuest` | готов | задание обхода обвала |
| `CollideQuestDone` (:1208) | `CollideQuestDone` | готов | завершение задания и активация областей стражей |
| `CollideActivateLiveGrunt` (:1227) | `CollideActivateLiveGrunt` | готов | обе ветки появления стража |
| `TimerGruntScare` (:1246) | `TimerGruntScare` | готов | испуг и восстановление скорости |
| `CollideDisableLiveGrunt` (:1263) | `CollideDisableLiveGrunt` | готов | выключение стража |
| `CollideJumpGrunt` (:1270) | `CollideJumpGrunt` | готов | включение и выключение области взгляда |
| `LookAtGrunt` (:1278) | `LookAtGrunt` | готов | коллбэк взгляда из карты |
| `CollideGruntSound01` (:1290) | `CollideGruntSound01` | готов | первый звук стража |
| `CollideGruntSound02` (:1295) | `CollideGruntSound02` | готов | второй звук стража |
| `CollideGruntSound03` (:1300) | `CollideGruntSound03` | готов | третий звук стража |
| `TimerIntroEnemy` (:1305) | `TimerIntroEnemy` | готов | начальный звук стража |
| `OnStart` (:1316) | `Archives.OnStart` | готов | все коллбэки, инициализация и отладочная ветка |
| `OnEnter` (:1415) | `Archives.OnEnter` | готов | все preload-вызовы, музыка, `AutoSave` |
| `OnLeave` (:1441) | `Archives.OnLeave` | готов | исходные параметры экрана загрузки |

Закомментированные функции также сохранены внутри исходных комментариев: `CollideActualOrb` (:430), `CollideBeginManyBook` (:567), `TimerManyBooks` (:578), `InteractBrokenWall` (:1063), `TimerBrokenWall` (:1072). Они не стали активными методами C#.

## Отличия от оригинала

- Синтаксис C#: ссылочные параметры строк AngelScript переведены в `string`, десятичные литералы имеют суффикс `f`, float → int явно усекается, `or` заменён на `||`, увеличение массива — выделением `int[]`, функции жизненного цикла — `public override`.
- Имена существующих коллбэков — `nameof`; динамический `sEvent` остаётся строкой. Имя отсутствующей функции `CollideBeginManyBook` оставлено строкой, поскольку её тело отключено и в оригинале.
- Единственное соединение строки с float, в `TimerCrawl`, использует `Str` с `G6` и `InvariantCulture`; проверено в русской локали лога.
- Удалены три недостижимых `break` после `return` в `TimerFlashBack`. Переходов между `case` без `break` в действующем коде этой карты нет.
- Ошибка индекса в `UniqueRandom` перехватывается непосредственно его единственным вызывающим методом `TimerRandRock`, который молча завершается до следующего случайного числа, звука и таймера. Это повторяет прерывание всего вызова AngelScript; диапазон генератора и содержимое массива не исправляются.
- Движок, импортёр, API, готовые порты 00–02 и `docs/ScriptPort/api.md` не изменялись.

## Особенности оригинала (сохранены)

- **Включённая верхняя граница индекса.** `03_archives.hps:32` и обращение к массиву на :34: случайное значение может равняться длине массива. HPL2 включает обе границы (`src/AmnesiaTheDarkDescent/HPL2/core/sources/math/Math.cpp:50`), поэтому редкий выход за массив возможен в оригинале. При нём текущий вызов `TimerRandRock` тихо прерывается; C# не пишет исключение в Unity. После исчерпания набора следующий вызов также выдаёт обычное случайное число, а новый набор создаётся лишь при следующем обращении (:23–28); алгоритм сохранён.
- **Отключённый коллбэк книг.** `03_archives.hps:1322` регистрирует `CollideBeginManyBook`, тело которого закомментировано на :567. `03_archives.map:8361` содержит `AreaTriggerManyBook` с `Active=false`, поэтому в проверенных прогонах вызова отсутствующего метода нет. Коллбэк не удалён и фиктивный метод не добавлен.
- **Отсутствующий рычаг.** `03_archives.hps:1379`–:1384 связывает картину с `lever_small01`; сущности с таким именем в `.map` нет (проверены все XML-узлы с `Name`). Старт пишет `Entity 'lever_small01' does not exist!`; вызов сохранён.
- **Неверный тип звукового ресурса.** `03_archives.hps:1185`, :1256 вызывают `PlayGuiSound` с `react_scare6` без расширения. Есть `source/sounds/react/react_scare6.ogg`, но нет `react_scare6.snt`, и в карте такого имени тоже нет. Оригинальный API считает имя без расширения звуковой сущностью (`LuxScriptHandler.cpp:1325`, `LuxHelpFuncs.cpp:158`); предупреждение `ERROR: Cannot find sound entity 'react_scare6'` ожидаемо. Расширение в порте не добавлено.
- **Имена таймеров вместо областей облака.** `03_archives.hps:955` передаёт `asTimer` как источник звука. `react1`, `thing1`, `thing2`, `thing3`, `thing4` в `.map` отсутствуют; при прямом вызове отключённого события появляются пять предупреждений об отсутствующих сущностях. Подключение этого события в `OnStart` закомментировано на :1360, как и в оригинале.
- **Повторная остановка звуков книг.** `03_archives.hps:765` останавливает `SoundSecretBook_1..3` при решении; :742 снова останавливает звук при выходе книги из области. В дополнительном прогоне после решения возникли предупреждения для `SoundSecretBook_1` и `_2`: это создаваемые скриптом звуки, отсутствующие как статические сущности `.map`; к повторному вызову они уже удалены. Оригинальный `StopSound` тоже сообщает об отсутствии звуковой сущности (`LuxScriptHandler.cpp:2100`). Вызовы и порядок сохранены, предупреждения не являются опечатками порта.
- **Лишний шаг таймеров.** `03_archives.hps:358` после состояния 18 ещё планирует таймер, поэтому итоговый счётчик `TimerFlashBack` равен 19. Аналогично `TimerEGrunt` (:911) после состояния 8 доходит до 9. Условия сохранены.
- **Неточный отладочный интервал.** В `TimerCrawl` фактическая задержка на :1044 отличается от числа, выводимого на :1048. Оба исходных значения сохранены.

## Не перенесено движком

`AddEnemyPatrolNode` (три вызова в `OnStart`) больше не заглушка: враги перенесены (6.2–6.3, CLAUDE.md «Враги»). Проверено прогоном `CollideActivateLiveGrunt`: `servant_grunt_1` идёт к своему узлу и в конце маршрута выключается (игрок дальше 10 м и не смотрит). `servant_grunt_1/2` в карте с `DisableTriggers=true` — игрока не замечают, как в оригинале.

Карта вызывает следующие `NotPorted` без обходных реализаций:

| API | Сообщение `skipped, needs` | Последствие |
|---|---|---|
| `AutoSave` | `saving (step 6)` | вход на карту не создаёт сохранение на диск |
| `SetSkyBoxActive` | `sky boxes` | шаг 5 воспоминания не переключает скайбокс |

В импорте также пропускаются `FogArea` и две декали с отсутствующей/повреждённой геометрией. Это ограничения текущего импорта, код импортёра не менялся.

## Проверка

Все запуски выполнялись по очереди, при закрытом Unity, с проверкой отсутствия `Temp/UnityLockfile`. Для запуска использовался Unity 6000.6.2f1 с доступом к обычному профилю Windows: первый запуск внутри sandbox не смог подключиться к Unity Licensing Client и был перезапущен до выполнения импортёра. Ниже — результаты завершившихся запусков.

### Импорт

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch -amnesiaMap maps/main/ch01/03_archives.map -logFile Logs/Import_03_archives.log
```

Результат: код выхода 0; раздел `Level: maps/main/ch01/03_archives.map` в `Logs/AmnesiaImport.log` — **5 warnings, 0 errors**. Все вспомогательные разделы моделей и материалов — 0 ошибок. Копия лога импорта до прогонов: `Logs/03_archives_import_report.log`.

| Предупреждение импорта | Количество |
|---|---:|
| Декали без корректной геометрии: `dirt_stain_large_18`, `dirt_floor01_13` | 2 |
| `FogArea` не поддержана | 1 |
| Отсутствующая картинка skin меню `gui_amn_mm_textbox_marker.tga` (меню её не использует) | 1 |
| Отсутствие порта скрипта на момент первоначального импорта | 1 |

Скрипт затем найден в симуляции как `Archives` (390 скриптовых сущностей), переимпорт для подключения не требуется. Сцена добавлена импортёром в `ProjectSettings/EditorBuildSettings.asset`; профиль `Windows.asset` не изменился. Ресурсы остались в игнорируемой папке.

### Сверка

После каждой правки C# запускалось:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 03_archives -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Archives.cs
```

Итог:

```text
03_archives : функций 57, строк 400, ошибок 0, предупреждений 0
```

Дополнительная сверка последовательности операторов после явных синтаксических преобразований C# подтвердила совпадение 6932 токенов; все 162 комментария оригинала сохранены (без учёта отступов и пробелов). Ошибок компиляции нет; новый `Archives.cs` не добавил предупреждений компилятора.

### Старт и основные события

Базовая команда симуляции (без `-nographics`, с GPU):

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe' -batchmode -quit -projectPath UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/03_archives.map -seed 1 -script 'watch 30 2; shot start' -logFile Logs/Sim_03_archives.log
```

Старт: **3 warnings, 0 errors**, код выхода 0. Предупреждения: `AddEnemyPatrolNode`, отсутствующий `lever_small01`, `AutoSave`. Работают фоновые звуки, таймеры пыли/крови и HUD; снимок `Logs/Player_03_archives_00_start.png` просмотрен.

Второй прогон использовал тот же запуск с `-logFile Logs/Sim_03_archives_events.log` и следующей строкой `-script`. После каждого из десяти вызовов — `watch 10 2`:

```text
call CollideBeginPiano Player AreaTriggerPiano 1; watch 10 2;
call CollideCloseLid Player AreaClosePiano 1; watch 10 2;
call VisionOverBooks; watch 10 2;
call CollideSecretBook SecretBook_1 AreaSecretBook_1 1; watch 10 2;
call PickKey key_study_1 OnPickup; watch 10 2;
call CollideCaveIn Player AreaCloudActivate_5 1; watch 10 2;
call CollideCaveInQuest Player AreaCaveInQuest 1; watch 10 2;
call CollideQuestDone Player AreaQuestDone 1; watch 10 2;
call CollideActivateLiveGrunt Player AreaActivateLiveGrunt_1 1; watch 10 2;
call CollideCloudThing Player AreaCloudActivate_2 1; watch 10 2; shot events
```

| Вызов | Результат |
|---|---|
| `CollideBeginPiano` | музыка/звук пианино, отложенные книга, пыль и урон рассудку |
| `CollideCloseLid` | импульс, пыль, звук крышки и звук врага |
| `VisionOverBooks` | задание книг, сообщение журнала и таймер звука врага |
| `CollideSecretBook` | фиксация книги, тиканье; после 20 с `PushBackBook` и `PushBackBook02` |
| `PickKey` | глобальная переменная ключа, все состояния `TimerEGrunt`, восстановление скорости и следа |
| `CollideCaveIn` | обвал, пыль, музыка, затемнение света, активация областей; исходная ошибка `react_scare6` |
| `CollideCaveInQuest` | добавлено задание обхода обвала |
| `CollideQuestDone` | задание завершено, области стражей активированы |
| `CollideActivateLiveGrunt` | скриптовая активация стража, реакция игрока и восстановление скорости; ИИ отсутствует |
| `CollideCloudThing` | облако, замедление и эффекты с восстановлением; пять исходных ошибочных источников звука |

Итог второго прогона: **9 warnings, 0 errors**, код выхода 0. Все предупреждения разобраны в «Особенности оригинала» и «Не перенесено движком». Вызовов `ChangeMap` не было.

### Полное воспоминание и решение книг

Дополнительный прогон: тот же `PlayerSimulation.RunBatch`, `-seed 1`, лог `Logs/Sim_03_archives_flashback.log`, строка `-script`:

```text
call GiveItemFromFile lantern lantern.ent; call SetLanternActive true true;
call PickDiary diary_paper01_2 0; watch 10 2;
call PickDiary diary_paper01_3 0; watch 10 2;
call PickDiary diary_paper01_4 0; watch 70 5; shot orb;
call CollideOrb Player AreaHalfToOrb 1; watch 10 2;
call CollideOrb Player AreaCloseToOrb 1; watch 10 2;
call LookAtOrb orb_1 1; watch 10 2;
call InteractOrb orb_1; watch 60 5; shot flashback_end;
call GetLocalVarInt DiaryFlashbackActive; call GetLocalVarInt TimerFlashBack;
call GetLanternActive; call GetPlayerLampOil;
call VisionOverBooks;
call CollideSecretBook SecretBook_1 AreaSecretBook_1 1; watch 2 1;
call CollideSecretBook SecretBook_2 AreaSecretBook_2 1; watch 2 1;
call CollideSecretBook SecretBook_3 AreaSecretBook_3 1; watch 10 2;
body shelf_secret_door_1; shot shelf
```

Итог: **6 warnings, 0 errors**. Воспоминание прошло через голоса, телепорт к сфере, взаимодействие и возврат к `Start_diary_paper01_4`: `DiaryFlashbackActive` = 0, `TimerFlashBack` = 19, фонарь снова активен, масло ≈87.22, игрок активен, множители движения/взгляда/FOV равны 1, след/сепия/затемнение погасли. Генератор камней прошёл несколько циклов. Все три книги успели до истечения таймеров: в логе `Completed Quest 03Books`, `All books in position, move shelf!`, играет `03_puzzle_secret.ogg`, полка сдвинута. Снимки `Player_03_archives_00_orb.png`, `Player_03_archives_01_flashback_end.png`, `Player_03_archives_02_shelf.png` сохранены в `Logs/`; снимок сферы просмотрен.

Дополнительные предупреждения: `SetSkyBoxActive` и повторная остановка уже удалённых `SoundSecretBook_1`, `_2` (описаны выше). Во всех трёх прогонах нет исключений, `PortError`, `does not exist in` и сообщений о несовпадении числа аргументов.

### Регрессия

Запуски с GPU, без `-quit` и без `-nographics`:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe' -batchmode -projectPath UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MapChangePreview.RunBatch -logFile Logs/Regression_03_archives_MapChange.log
& 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe' -batchmode -projectPath UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.MainMenuPreview.RunBatch -logFile Logs/Regression_03_archives_MainMenu.log
```

`MapChangePreview` завершился с кодом 0:

```text
PASS: 60 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MapChangeCheck
```

`MainMenuPreview` завершился с кодом 0:

```text
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 14 main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).
PASS: 14 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game); screenshots Z:\Unity\RTXMnesia\UnityProj\UMnesiaRTX\Logs\MainMenuCheck
```

Все обязательные проверки прошли. `MainMenuPreview` автоматически пересохранил `ProjectSettings/TimeManager.asset`; файл возвращён командой из задания. Другие посторонние настройки проекта не менялись.

## Изменённые файлы и итоговый git status

Новые файлы этой задачи:

- `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Archives.cs`;
- `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Archives.cs.meta` (создан Unity);
- `docs/ScriptPort/ch01/03_archives.md`.

Изменённые файлы:

- `CLAUDE.md`, `AGENTS.md`: только добавление `03_archives` в строку этапа 5; файлы побайтно одинаковы, предшествующие изменения пользователя сохранены;
- `UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset`: импортёр добавил сцену `03_archives`.

`docs/tasks/codex_ch01_maps.md` и `tools/Check-ScriptPort.ps1` уже были untracked до начала работы; их содержимое не менялось. Коммитов и push не выполнялось. Карты 04–14 не переносились.

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Archives.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Archives.cs.meta
?? docs/ScriptPort/ch01/03_archives.md
?? docs/tasks/codex_ch01_maps.md
?? tools/Check-ScriptPort.ps1
```
