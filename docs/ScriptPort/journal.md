# Журнал

Спецификация: `src/AmnesiaTheDarkDescent/amnesia/src/game/LuxJournal.cpp`, `LuxItemType.cpp`, `LuxInputHandler.cpp`.
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Ui/HplJournal.cs`, `HplJournalView.cs`; общие данные — `HplGame`.
Статус: реализован; автоматические проверки и контрольные снимки Unity Play. Прохождение следующих карт и сохранения ещё не проверяются.

## Соответствие

| Оригинал | Порт | Поведение |
|---|---|---|
| `LuxJournal.cpp:680` AddNote | HplGame.AddJournalEntry | Note_<имя>_Name / Text, варианты бумаги _large / _icon |
| `LuxJournal.cpp:724` AddDiary | HplGame.AddJournalEntry | Серии дневников, номер с 1 внутри каждой серии |
| `LuxJournal.cpp:752`, `:773` | HplGame.Quests, HplJournal.BuildLists | Активные задания в обратном порядке; выполненные хранятся, но не рисуются |
| `LuxJournal.cpp:491`, `:558` | HplReadingScreens.OpenJournal, HplGameUi.EnterScreen / Resume | Мир заморожен, окружение и музыка продолжаются, World → 0.5, реплики мира приостановлены |
| `LuxJournal.cpp:421`, `:617` | HplJournal.Tick / ExitPressed | Появление 0.5 с, уход 1/3 с, возврат по разделам; narrated сначала переключает сегмент |
| `LuxJournal.cpp:843`, `:877`, `:924` | OpenEntry, OpenRecentText | Обычное / озвученное чтение; N открывает последний текст без повторной озвучки |
| `LuxJournal.cpp:1136`, `:1178`, `:1269` | BuildPages, SetPage | 18 строк, [new_page], голосовые сегменты [voice …], смена реплики и автопереход по окончании |
| `LuxJournal.cpp:1426`–`:1799` | HplJournalView | Главное оглавление, списки, задания, бумага, заголовок, текст, стрелки и возврат |
| `LuxItemType.cpp:150`, `:196` | HplGame.AddItem | Автооткрытие записки по amount, дневника с озвучкой; одноразовый выбор ReturnOpenJournal из callback |
| `LuxInputHandler.cpp:1001`, `:1354` | HplGameUi.RouteKeys, HplReadingScreens.Update | J / M / N, Esc по иерархии, клавиши экранов закрывают напрямую |
| `LuxScriptHandler.cpp:1712`–`:1730` | HplScript.Journal | AddNote / AddDiary добавляют данные, ReturnOpenJournal управляет автооткрытием дневника |

## Данные и ресурсы

- HplGame.JournalEntry хранит тип, исходное имя локализации, базовую картинку, номер серии и отметку прочтения. LastReadEntry сохраняется между картами вместе с общим состоянием; Reset очищает всё для новой игры.
- DiaryCallback получает имя пропа и номер записи **с 1 внутри серии**. Перед каждым подбором флаг показа сбрасывается в true; callback может вызвать ReturnOpenJournal(false). Такой дневник сохраняется как последний текст, без немедленного открытия.
- AddNote / AddDiary из скрипта не открывают UI. Подбор Note с amount > 0 открывает её; amount >= 2 включает озвучку. Отрицательный amount только добавляет запись.
- При открытии из инвентаря общая пауза не снимается; выход возвращает в инвентарь. Голос мира не возобновляется между экранами.
- Обычное чтение прячет voice-теги и делит текст по строкам / страницам. Озвученное чтение делит по voice-тегам; завершение SoundEntry переводит на следующий сегмент. Выход во время narrated также сначала переключает сегмент, как оригинал.
- JournalConverter.ConvertBatch генерирует игнорируемый Resources/HplJournal.asset из graphics/journal и русских game_journal.fnt / game_header.fnt. Общий путь FontConverter → HplFont → HplGuiText сохранён. Существующая карта не требует переимпорта; LevelConverter тоже обновляет ресурсы.

## Адаптация и оставшееся

- Оригинальные координаты и бумага на виртуальном экране 800×600, красный пульсирующий фокус. Фон сцены использует тот же резкий затемнённый снимок с сепией, что инвентарь.
- Раздел появляется за 0.5 с; прежний слой раздела не сохраняется для перекрёстного затухания. Оформление навигации остаётся оригинальным.
- Клавиатура и мышь: стрелки / Enter, колесо, Esc / ПКМ назад; при озвучке ЛКМ / Enter / вправо переключают сегмент. Переназначение клавиш и геймпад — следующие задачи настроек.
- Отметка прочтения хранится в данных, без добавления значков, которых нет в исходном интерфейсе.
- Не реализованы достижения, процент завершения и сохранение прохождения. CompleteQuest сохраняет прежний частичный статус по этой причине.

## Проверки

RTXMnesia.EditorDev.JournalTests.Run: 36 проверок серий, API и callback, автооткрытия, страниц, озвучки, active-заданий, последнего текста, запретов вступления, перехода из инвентаря, аудио, реальной геометрии текста и очистки паузы. Отчёт: Logs/JournalCheck/Tests.txt.

Общий MenuPreview.RunBatch дополнительно повторяет 50 InventoryTests и 27 PauseTests, сохраняет JournalMain / JournalNotes / JournalDiaries / JournalQuestLog / JournalNote / JournalNotePage2 / JournalNarrated в Logs/MenuCheck. Мир — GPU, UI — реальные CanvasRenderer и атласы через HudSnapshot.DrawCanvas. Сцена и тестовое содержимое не сохраняются.
