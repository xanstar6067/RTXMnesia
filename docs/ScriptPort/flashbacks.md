# Флешбэки

Реализовано и проверено в Unity 6000.6.2f1 16 октября 2026 года.

## Поведение и источники

| Оригинал | Порт |
|---|---|
| `amnesia/src/game/LuxPlayerHelpers.cpp:858`, `Reset` | `PlayerFlashback.Reset`: активность, задержка вспышки, очистка очереди |
| `amnesia/src/game/LuxPlayerHelpers.cpp:867`, `Start` | Флаг отключения для отладки, очередь, звук вспышки, задержки и громкость мира |
| `amnesia/src/game/LuxPlayerHelpers.cpp:900`, `Update` | Вспышки, множители скорости, сепия, размытие, голоса, завершение и коллбэк |
| `amnesia/src/game/LuxPlayerHelpers.cpp:996`, `LoadAndPlayFlashbackFile` | `FlashbackConverter`, `HplFlashbackAssets`, постановка голосов и перевод субтитров |
| `amnesia/src/game/LuxArea_Flashback.cpp:55`, `LoadVariables`; `:95`, `OnUpdate` | `HplArea`: переменные FlashbackFile / Callback и проверка текущей формы персонажа |
| `amnesia/src/game/LuxEffectHandler.cpp:1194`, `VoiceDonePlaying` | Одноимённый метод `PlayVoiceEffect` |
| `amnesia/src/game/LuxScriptHandler.cpp:1242`, `GetFlashbackIsActive` | Состояние помощника игрока |
| `amnesia/src/game/LuxPlayerHelpers.cpp:2947`, проверка потери рассудка в темноте | Дополнительное условие в `PlayerInDarkness.Update` |

Все ссылки на оригинал относятся к `src/AmnesiaTheDarkDescent/`. Код и ресурсы оригинала в этот документ не включены.

Область проверяется раз в 0.1 с, пропускает неполные шаги и выключается перед запуском флешбэка. Пересечение определяется через существующий `HplCollision.BodiesCollide` и активную форму персонажа, без `Collider.bounds` и ожидания синхронизации PhysX.

Звук вспышки начинается сразу. Через 0.5 с запускается вспышка с временами 0.5 / 0.5 / 2.5 и задаются множители движения. Через 1.5 с начинаются голоса; сепия и радиальное размытие нарастают за 3.5 с. Когда голоса завершены, запускается вспышка 1 / 0.1 / 2.5, эффекты и громкость восстанавливаются за 4 с. Множители скорости сбрасываются, Active становится false, устанавливается задержка очереди 6 с, затем вызывается коллбэк.

Сохранены особенности оригинала:

- `VoiceDonePlaying` проверяет пустую очередь и нулевую ссылку на голос, а не валидность записи и не общую активность эффекта; сопровождающий звук сам по себе не удерживает флешбэк.
- Задержка следующего флешбэка уменьшается только при непустой очереди; новый прямой вызов Start в промежутке запускается сразу.
- У задержки первой вспышки сохранено строгое сравнение: попадание ровно в ноль не запускает вспышку. Обычный шаг 1/60 проверен отдельно.
- Active и движение восстанавливаются до коллбэка, когда затухание экранных эффектов ещё продолжается.

Параметры берутся из `Player_General` текущего `game.cfg`: blur 0.09, start distance 0.3, world volume 0.3, move/run 0.6. Значения по умолчанию сохранены по исходнику. Указанные в брифе 0.4 для громкости относятся к значению по умолчанию; проверка фактического конфига даёт 0.3.

Сознательно не подключены `BroadcastEnemyMessage` (врагов ещё нет), `AddCompletionAmount` (нет системы процента завершения), `ProgLog` (нет обработчика журнала прогресса). В коде оставлены комментарии `not ported` с причинами. Флаг оригинального `GetDisableFlashBacks` доступен через `PlayerFlashback.Disabled`; интерфейс отладчика ещё не перенесён. Перенос состояния между картами не добавлялся по брифу.

## Файлы

Новые файлы (пути относительно `UnityProj/UMnesiaRTX/Assets/`):

- `Scripts/Player/HplFlashbackAssets.cs` и `.cs.meta` — данные флешбэков, поиск без учёта регистра, пути и расширения.
- `Scripts/Player/PlayerFlashback.cs` и `.cs.meta` — помощник игрока.
- `Editor/Importer/FlashbackConverter.cs` и `.cs.meta` — конвертер, меню и batch-метод, обновление существующего ассета на месте.
- `Editor/Dev/FlashbackTests.cs` и `.cs.meta` — проверки жизненного цикла, очереди, коллбэков и особенностей оригинала.

Точечно изменены:

- `Scripts/Player/AmnesiaPlayer.cs` — event-множители, помощник, обновление сразу после LookAt.
- `Scripts/Player/PlayerDarkness.cs` — одно дополнительное условие потери рассудка.
- `Scripts/ScriptApi/HplEntity.cs` — только логика областей Flashback.
- `Scripts/ScriptApi/HplScript.Effects.cs` — реализация GetFlashbackIsActive.
- `Scripts/Effects/HplEffects.cs` — только новый метод VoiceDonePlaying.
- `Editor/Importer/LevelConverter.cs` — вызов конвертера рядом с PostEffectsConverter.
- `Editor/Dev/PlayerSimulation.cs` — поле move× показывает итоговый MoveSpeedMul.

Документация: `CLAUDE.md`, его побайтно одинаковая копия `AGENTS.md`, статус API в `docs/ScriptPort/api.md` и этот отчёт.

`Assets/AmnesiaImported/Resources/HplFlashbacks.asset` сгенерирован вне git. Игровые карты не конвертировались заново. Регрессия главного меню штатно пересоздала свою импортированную сцену меню. Установленная игра, `src/`, запрещённые в брифе компоненты, шейдеры и настройки HDRP не изменялись. Автоматическое изменение `ProjectSettings/TimeManager.asset` возвращено командой из брифа. Изначальные изменения `Assets/Settings/Build Profiles/Windows.asset` сохранены побайтно; `docs/tasks/` существовала до задачи. Коммиты и push не выполнялись.

## Проверки

Импорт, прогон карты, итоговый тест флешбэков и обе обязательные регрессии вернули код 0. Перед запуском проверялся `Temp/UnityLockfile`. Для GPU-прогонов не использовался `-nographics`, для Play-регрессий также не использовался `-quit`. Unity запускалась вне песочницы из-за недоступности IPC службы лицензирования внутри неё.

Импорт, `RTXMnesia.Importer.FlashbackConverter.ConvertBatch`:

```text
=== Flashbacks — 2026-10-16 13:47:35 — 0 warnings, 0 errors ===
Flashbacks: 37 flashbacks, 224 voices -> Assets/AmnesiaImported/Resources/HplFlashbacks.asset
```

Логи: `UnityProj/UMnesiaRTX/Logs/Flashbacks.log`, `Logs/AmnesiaImport.log`. Меню: Tools > RTXMnesia > Import > Flashbacks (.flash). Вызов конвертера автоматически добавлен в импорт уровня; переимпорт существующих игровых карт не требуется.

Прогон `RTXMnesia.EditorDev.PlayerSimulation.RunBatch`, карта `maps/main/ch01/02_entrance_hall.map`, `-seed 1`, команды:

```text
idle 1; shot flashback_before; goto -33.25 0.05 9.75 0;
watch 0.65 0.1; shot flashback_start; watch 1.5 0.5; sounds;
watch 6 1; shot flashback_voice; call GetFlashbackIsActive;
watch 40 1; watch 60 5; sounds; shot flashback_end; call GetFlashbackIsActive
```

Проверенная точка пола после стабилизации: feet (-33.211, 0.005, 9.651), ground True. Ниже фрагменты строк лога; поля субтитров и остальные несвязанные поля опущены. Полные строки и субтитры доступны в игнорируемом логе и снимках.

```text
t=  1,62 ... move×0,60 ... sepia 0,00 blur 0,00 flash 0,17 | voice -
t=  2,67 ... move×0,60 ... sepia 0,03 blur 0,00 flash 0,95 | voice flashbacks/CH01L02_JourneyToSanctum01bs_01.ogg
t=  3,17 ... global world vol 0,30 speed 1,00
t=  4,17 ... move×0,60 ... sepia 0,46 blur 0,04 flash 0,35 | voice flashbacks/CH01L02_JourneyToSanctum01_01.ogg
t=  9,17   call call GetFlashbackIsActive → True
t= 35,20 ... move×1,00 ... sepia 0,94 blur 0,08 flash 0,23 | voice -
t= 39,20 ... move×1,00 ... sepia 0,00 blur 0,00 flash 0,00 | voice -
t=109,19 ... global world vol 1,00 speed 1,00
t=109,19   call call GetFlashbackIsActive → False
```

Прогон: 2 warnings, 0 errors. Предупреждения относятся к существующим обращениям скрипта карты к отсутствующей `castle_1` и к ещё не перенесённому AutoSave. Ошибок загрузки флешбэка и голосов нет. Просмотрены снимки `Logs/Player_02_entrance_hall_01_flashback_start.png`, `02_flashback_voice.png`, `03_flashback_end.png`: вспышка, размытие / сепия с русскими субтитрами, обычный вид после завершения.

Дополнительный `RTXMnesia.EditorDev.FlashbackTests.RunBatch`, `Logs/FlashbackTests.txt`:

```text
PASS: 21 flashback lifecycle checks (area, real voices, queue, callbacks, volume and original quirks)
```

Обязательная регрессия `RTXMnesia.EditorDev.MapChangePreview.RunBatch`, `Logs/FlashbackMapChangeRegression.log`:

```text
PASS: 60 Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian)
```

Обязательная регрессия `RTXMnesia.EditorDev.MainMenuPreview.RunBatch`, `Logs/FlashbackMainMenuRegression.log`:

```text
PASS: 27 pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).
PASS: 50 inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).
PASS: 36 journal checks (series, callbacks, pages, narration, quests, recent text, inventory transitions, sound, UI cleanup).
PASS: 14 main menu model checks (localization, controls, confirmation, preferences, live world, build scene order).
PASS: 14 Play-mode main menu checks (startup, live background / music, options, new game, cleanup, return, cancellation, second new game)
```

`git diff --check` проходит, ресурсы Amnesia не отслеживаются, изменения ProjectSettings отсутствуют, SHA256 `CLAUDE.md` и `AGENTS.md` совпадают. Вывод `git status --short` после завершения задачи:

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/Assets/Editor/Dev/PlayerSimulation.cs
 M UnityProj/UMnesiaRTX/Assets/Editor/Importer/LevelConverter.cs
 M UnityProj/UMnesiaRTX/Assets/Scripts/Effects/HplEffects.cs
 M UnityProj/UMnesiaRTX/Assets/Scripts/Player/AmnesiaPlayer.cs
 M UnityProj/UMnesiaRTX/Assets/Scripts/Player/PlayerDarkness.cs
 M UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplEntity.cs
 M UnityProj/UMnesiaRTX/Assets/Scripts/ScriptApi/HplScript.Effects.cs
 M "UnityProj/UMnesiaRTX/Assets/Settings/Build Profiles/Windows.asset"
 M docs/ScriptPort/api.md
?? UnityProj/UMnesiaRTX/Assets/Editor/Dev/FlashbackTests.cs
?? UnityProj/UMnesiaRTX/Assets/Editor/Dev/FlashbackTests.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Editor/Importer/FlashbackConverter.cs
?? UnityProj/UMnesiaRTX/Assets/Editor/Importer/FlashbackConverter.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Player/HplFlashbackAssets.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Player/HplFlashbackAssets.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Player/PlayerFlashback.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Player/PlayerFlashback.cs.meta
?? docs/ScriptPort/flashbacks.md
?? docs/tasks/
```
