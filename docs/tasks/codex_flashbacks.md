# Задача для Codex: флешбэки (cLuxPlayerFlashback + области Flashback)

Прочитай целиком перед началом. Сначала прочитай `CLAUDE.md` в корне репозитория: там правила проекта, структура и соглашения. Всё, что ниже, их не отменяет.

## Что сделать

Перенести из исходников Amnesia механику флешбэков:
- **область Flashback** на карте: игрок входит, область выключается, начинается флешбэк;
- **сам флешбэк** у игрока: звук вспышки, белая вспышка, сепия и радиальное размытие, приглушение мировых звуков, замедление игрока, проигрывание голосов из файла `.flash` с субтитрами, по окончании — вспышка, эффекты гаснут, вызывается коллбэк скрипта;
- **очередь**: если флешбэк уже идёт, следующий ждёт и стартует через 6 с после конца текущего;
- **функцию скрипта** `GetFlashbackIsActive()`.

Где это нужно в игре: `02_entrance_hall` (область `AreaFlashBack`, файл `CH01L02_JourneyToSanctum01`), дальше 03, 04, 05, 06, 09, 10, 11, 12.

## Исходники (только читать, никогда не менять)

- `src/AmnesiaTheDarkDescent/amnesia/src/game/LuxPlayerHelpers.cpp`, класс `cLuxPlayerFlashback` (строки ~841–1035): `Reset`, `Start`, `Update`, `LoadAndPlayFlashbackFile`.
- `src/AmnesiaTheDarkDescent/amnesia/src/game/LuxArea_Flashback.cpp`: загрузка переменных области (`FlashbackFile`, `Callback`) и `OnUpdate`.
- `src/AmnesiaTheDarkDescent/amnesia/src/game/LuxScriptHandler.cpp`: `GetFlashbackIsActive` (~строка 1242).
- `src/AmnesiaTheDarkDescent/amnesia/src/game/LuxEffectHandler.cpp`: `cLuxEffect_PlayVoice::VoiceDonePlaying` — что значит «голоса доиграли».
- Числа: `source/config/game.cfg`, категория `Player_General`, ключи `FlashbackRadialBlurSize`, `FlashbackRadialBlurStartDist`, `FlashbackWorldSoundVolume`, `FlashbackMoveSpeedMul`, `FlashbackRunSpeedMul`. Значения по умолчанию — как в `cLuxPlayerFlashback` (0.12, 0.4, 0.4, 1, 1).
- Данные: `source/flashbacks/*.flash` — XML, элемент `Voices`, дочерние элементы с атрибутами `VoiceSound`, `EffectSound`, `TextCat`, `TextEntry`.

Переносить **поведение 1:1**, включая порядок действий и числа. Ничего не «упрощать» и не выбрасывать. Если в оригинале есть странность — сохранить и написать комментарий, что это особенность оригинала.

## Как устроен наш проект (где что лежит)

Unity-проект: `UnityProj/UMnesiaRTX`. Код: `Assets/Scripts`.

- **Игрок:** `Assets/Scripts/Player/AmnesiaPlayer.cs`. Помощники игрока — отдельные классы, примеры: `PlayerLantern.cs`, `PlayerSanity.cs`, `PlayerDarkness.cs`. Они создаются в `AmnesiaPlayer.Awake` (рядом с `Lantern = new PlayerLantern(this, cfg)`) и обновляются в `AmnesiaPlayer.StepLogic` в порядке оригинала (там есть комментарий с порядком: …Sanity, LookAt, **Flashback**, Hurt…). Конфиг — `gameConfig` (`HplConfig`: `GetFloat(категория, ключ, умолчание)`).
- **Эффекты экрана:** `Assets/Scripts/Effects/HplEffects.cs`: `Flash.Start(fadeIn, white, fadeOut)`, `Sepia.FadeTo(amount, speed)`, `RadialBlur.SetBlurStartDist(...)`, `RadialBlur.FadeTo(size, speed)`, `PlayVoice.AddVoice(voiceFile, effectFile, text, usePosition, position, min, max)`. Доступ из помощника игрока: `HplMap.Current.effects`. В HPL2 `GetSepiaColor()` — это наш `Sepia`, `GetFlash()` — `Flash`.
- **Звук:** у игрока поле `sound` (`HplSoundHandler`). `PlayGuiSoundData(имя, SoundEntryType.Gui)` — как `cLuxHelpFuncs::PlayGuiSoundData`. Громкость мира: `FadeGlobalVolume(dest, speed, SoundEntryType.World, id, destroyAtDest)`. Для флешбэка `id` = **1** (`eLuxGlobalVolumeType_Flashback`; Script = 0, InsanityCollapse = 4 — не путать). Пример использования — `PlayerInsanityCollapse` в `PlayerSanity.cs`.
- **Тексты субтитров:** `HplMap.Current.hud.Translate(категория, запись)` — так же, как делает `AddEffectVoice` в `Assets/Scripts/ScriptApi/HplScript.Effects.cs`. Посмотри его и сделай так же.
- **Области карты:** класс `HplArea` в `Assets/Scripts/ScriptApi/HplEntity.cs`. Тип — `AreaType` (строка из карты, будет `"Flashback"`), переменные — `Component.GetVariable("FlashbackFile")`. Логика каждый шаг — переопределение `OnUpdate(float timeStep)` (вызывается картой только для активных сущностей). Выключение — `SetActive(false)`. Проверка «игрок внутри» — посмотри, как сделаны коллбэки столкновений с игроком (`HplCollision`, `CollideCallbacks` в том же файле), и используй ту же проверку пересечения с телом игрока. Не пиши свою проверку через `Collider.bounds`: в `CLAUDE.md` объяснено, почему так нельзя.
- **Скриптовое API:** `Assets/Scripts/ScriptApi/HplScript*.cs`. Сейчас там заглушка `GetFlashbackIsActive() => false` в `HplScript.Effects.cs`.
- **Ресурсы игры в рантайме:** файлы из `source/` в рантайме читать **нельзя**. Их переносят конвертеры (Editor) в игнорируемую папку `Assets/AmnesiaImported/Resources/*.asset`. Делай по образцу:
  - конвертер `Assets/Editor/Importer/PostEffectsConverter.cs`;
  - ассет `Assets/Scripts/Effects/HplPostEffectAssets.cs`;
  - вызов из импорта уровня — `LevelConverter.cs`, рядом с `PostEffectsConverter.Convert(_log);`;
  - пункт меню `Tools/RTXMnesia/Import/...`.

  Числа из XML парсить только через `CultureInfo.InvariantCulture` (у пользователя русская локаль). Читать XML так же, как это делают другие конвертеры.

## Как делать по шагам

1. **Ассет данных** `Assets/Scripts/Player/HplFlashbackAssets.cs` (`ScriptableObject`, `ResourceName = "HplFlashbacks"`). Список флешбэков: имя файла без расширения, список голосов (`voiceSound`, `effectSound`, `textCat`, `textEntry`). Поиск по имени без учёта регистра; имя приходит без `.flash`, как `cString::SetFileExt`.
2. **Конвертер** `Assets/Editor/Importer/FlashbackConverter.cs`: все `source/flashbacks/*.flash` → `Assets/AmnesiaImported/Resources/HplFlashbacks.asset`. Существующий ассет обновлять на месте (как `PostEffectsConverter`). Добавить пункт меню, batch-метод `ConvertBatch` и вызов из `LevelConverter`. Ошибки — в `ImportLog`.
3. **Помощник** `Assets/Scripts/Player/PlayerFlashback.cs`, класс `PlayerFlashback`: порт `cLuxPlayerFlashback` (поля, `Start`, `Update`, очередь, загрузка голосов). Свойство `Active`. Нет файла — ошибка как в оригинале, через `HplLog.Error`. Строки `BroadcastEnemyMessage` (враги), `AddCompletionAmount` (процент прохождения) и `ProgLog` **не выбрасывать молча**: оставить комментарий `// not ported: ...` с причиной (врагов и процента прохождения ещё нет).
4. **«Голоса доиграли»:** добавить в `PlayVoiceEffect` (в `HplEffects.cs`) метод `VoiceDonePlaying()` строго по оригиналу (`cLuxEffect_PlayVoice::VoiceDonePlaying`). Остальной код `PlayVoiceEffect` не менять.
5. **Замедление игрока:** в оригинале `SetEventMoveSpeedMul` / `SetEventRunSpeedMul`. В `AmnesiaPlayer` таких полей нет. Добавить `eventMoveSpeedMul` и `eventRunSpeedMul` (по умолчанию 1) и **умножить** их в существующие свойства `MoveSpeedMul` и `RunSpeedMul` (строки ~100–101). Больше ничего в формулах не трогать.
6. **Подключение в `AmnesiaPlayer`:** свойство `Flashback`, создание в `Awake` рядом с остальными помощниками, вызов `Flashback.Update(dt)` в `StepLogic` **сразу после** `LookAt.Update(dt)` (порядок оригинала).
7. **Область:** в `HplArea` для `AreaType == "Flashback"` — чтение `FlashbackFile` и `Callback` в конструкторе и `OnUpdate` по `cLuxArea_Flashback::OnUpdate`: проверка раз в 0.1 с; при пересечении с игроком — `SetActive(false)` и `player.Flashback.Start(file, callback)`. Остальные типы областей не трогать.
8. **Скрипт:** `GetFlashbackIsActive()` → `Player.Flashback.Active`. Коллбэк по окончании: `HplMap.Current.RunScript(callback)`, если не пустой.
9. **Темнота:** в `Assets/Scripts/Player/PlayerDarkness.cs`, метод `PlayerInDarkness.Update`, в условие потери рассудка добавить «флешбэк не идёт» — в оригинале там проверка `GetHelperFlashback()->IsActive()==false` (`LuxPlayerHelpers.cpp`, `cLuxPlayerInDarkness::Update`). Только это одно условие.
10. **Перенос между картами:** не нужен (в HPL2 флешбэк на смене карты не прерывается специально). Ничего в `HplMapChange` не делать.

## Проверка (обязательно, с результатами)

Unity должен быть закрыт: проверь, что нет `UnityProj/UMnesiaRTX/Temp/UnityLockfile`. Процесс `unity.exe serve` — это Hub, он не мешает. Unity: `C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe`.

1. **Импорт:** `-batchmode -nographics -quit -projectPath UnityProj/UMnesiaRTX -executeMethod RTXMnesia.Importer.FlashbackConverter.ConvertBatch -logFile Logs/Flashbacks.log`. В `Logs/AmnesiaImport.log` должно быть число флешбэков (в `source/flashbacks` их 37), 0 ошибок.
2. **Прогон** (без `-nographics`, нужен GPU для снимков):
   ```
   -batchmode -quit -projectPath UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch01/02_entrance_hall.map -seed 1 -script "<команды>" -logFile Logs/SimFlashback.log
   ```
   Игрок стартует далеко от области. Координаты области `AreaFlashBack` в HPL: `33.25 3.25 9.75`, размер `25 7 3`. В Unity X меняет знак: центр ≈ `(-33.25, 3.25, 9.75)`. Команда `goto x y z yaw` ставит ноги игрока. Найди точку пола внутри области (попробуй y около 0–1, смотри по логу, что игрок стоит на земле), затем `watch 40 1` и снимки `shot`. Список команд — `CLAUDE.md`, раздел «Прогон».
   В строке состояния скрипта (`script: ...`) должны быть видны: `flash` > 0 через ~0.5 с, `sepia` растёт после ~1.5 с, `blur` > 0, `voice` — имя голоса флешбэка и субтитр, `move×` = значение из конфига; в конце `sepia` и `blur` к 0. Команда `sounds` покажет глобальную громкость мира 0.4 во время флешбэка и 1 после.
3. **Регрессия** — обязательно запустить и приложить строки PASS:
   - `RTXMnesia.EditorDev.MapChangePreview.RunBatch` (без `-quit` и без `-nographics`): ожидается `PASS: 60 ...`;
   - `RTXMnesia.EditorDev.MainMenuPreview.RunBatch`: ожидается 27 / 50 / 36 / 14 / 14 PASS.

   Если что-то упало — чинить, а не отключать проверку.
4. Проверка компиляции без Unity допустима для быстрых итераций (см. `CLAUDE.md`, «Быстрая проверка компиляции без Unity»), но финал — только через прогоны выше.

## Что НЕ трогать и чего НЕ делать

- **Не менять** установленную игру `E:\SteamLibrary\...` и папку `src/`.
- **Не коммитить** и не пушить (коммитит пользователь). Ресурсы Amnesia (всё из `source/`, `Assets/AmnesiaImported/`) в git не попадают никогда. Проверь `git status`: новых файлов из `AmnesiaImported` быть не должно.
- **Не удалять, не переименовывать и не «чистить»** чужой код, ассеты, предметы, сцены, тесты. Никаких массовых замен по проекту. Если кажется, что что-то лишнее, — не трогать, а написать в отчёте.
- Не переимпортировать карты целиком без нужды: для этой задачи хватит `FlashbackConverter.ConvertBatch`. Карту 02 переимпортировать **не нужно**: области уже есть в сцене (`AmnesiaArea` с типом Flashback).
- **Не менять:** `HplMapChange.cs`, `HplSavedMaps.cs`, `PlayerSanity.cs`, `PlayerStateLadder.cs`, `HplBoxLightPass.cs`, шейдеры, `HplScreenEffectsPass.cs`, настройки HDRP и `ProjectSettings`. Если Unity сам пересохранит `ProjectSettings/TimeManager.asset` — вернуть через `git checkout -- UnityProj/UMnesiaRTX/ProjectSettings/TimeManager.asset`.
- В `AmnesiaPlayer.cs`, `HplEntity.cs`, `HplEffects.cs`, `LevelConverter.cs`, `PlayerDarkness.cs` — только точечные правки из шагов выше, без переформатирования и рефакторинга.
- Чисел в коде не придумывать: только конфиг или те же константы, что в исходнике.
- Код оригинала в документацию не копировать — только ссылки `файл:строка`.

## Документация (часть задачи)

- `CLAUDE.md` и `AGENTS.md`: короткий абзац «Флешбэки» в разделе «Скрипты уровней» (что перенесено, где файлы, как проверить) и команда импорта. Файлы должны остаться **побайтно одинаковыми**: правишь `CLAUDE.md`, затем копируешь его в `AGENTS.md`.
- `docs/ScriptPort/api.md`: статус `GetFlashbackIsActive` → ✓.
- Стиль комментариев в коде — английский, коротко, как в соседних файлах (`<summary>` с именем оригинального класса или метода).

## Отчёт в конце

1. Список изменённых и новых файлов.
2. Что перенесено и что сознательно не перенесено (с причиной).
3. Выдержки из логов: импорт (число флешбэков, 0 ошибок), прогон флешбэка (строки со вспышкой, сепией, голосом, концом), строки PASS обоих регрессионных тестов.
4. Вывод `git status --short`.
