# Задача 01: средство отчётов F1 — снимок при HDR, видеопамять, «Взгляд»

Отдельный чат. Прочитать `CLAUDE.md`, `docs/Reference/DevTools.md` (раздел отчёта F1 и «Видеопамять»),
`docs/Reference/RayTracing.md` (проход `HplScreenEffectsPass`). Код: `Assets/Scripts/Dev/HplBugReport.cs`,
`Assets/Scripts/Dev/HplBugReportProbe.cs`, `Assets/Scripts/Ui/HplBugReportView.cs`, `Assets/Scripts/Ui/HplGameUi.cs`
(`BeginBugReport`), `Assets/Scripts/Hpl/HplVideoMemory.cs`, `Assets/Scripts/Hpl/HplHdr.cs`.

## 1. Снимок не создаётся при HDR-выводе (все 15 отчётов)

**Сделано 2026-11-02**: кадр из `HplScreenEffectsPass` (проход «SDR capture»), причина отсутствия снимка в отчёте и в окне, проба с `ForcePassCapture`. Ориентация и вид кадра проверены по снимку batch-прогона. Осталось: проверить в своей сборке на HDR-дисплее.

Факт: во всех `report.txt` — `Снимок: нет` и `HDR-вывод: да`; в `log.txt` при каждом F1:
`[E] CaptureScreenshotAsTexture() failed to generate texture! Was method called before the 'end of frame' state was reached?`.
Ожидание `WaitForEndOfFrame` в `HplBugReport.Capture` есть; причина — HDR-вывод: `ScreenCapture` при активном HDR
не пишет (уже известно для бенчмарка, `DevTools.md`: «при HDR-выводе `ScreenCapture` не пишет: `hdrMode=0`»).
Unity сообщает об этом ошибкой в лог, не исключением — `catch` в `Capture()` не срабатывает, отчёт молча без снимка.
Проба `HplBugReportProbe` проверяет только batch-путь (там «Снимок: нет» ожидаем), поэтому ошибку не поймала.

Решение (рекомендация, уточнить при реализации):
- При `HplHdr.Active` снимать кадр своим путём: копия буфера цвета камеры в проходе после пост-обработки
  (рядом с `HplScreenEffectsPass`, выходное разрешение), перевод в SDR — деление на paper white (`HplHdr.PaperWhite`),
  клэмп, sRGB — и `ReadPixels`/`AsyncGPUReadback` в `Texture2D`. Проверить, попадает ли HUD в этот буфер; если нет —
  отметить в отчёте («без HUD»). Без HDR оставить `ScreenCapture.CaptureScreenshotAsTexture`.
- Проверять результат на `null`, а не только исключение; при неудаче писать в отчёт причину
  (`Снимок: нет (HDR-вывод, ...)`) и показывать в окне отчёта, что снимка нет, — до ввода комментария.
- Расширить проверку: в обычном (не batch) запуске с HDR и без — снимок есть, размеры = экран, не чёрный,
  средняя яркость сопоставима с кадром. Если HDR-дисплей недоступен в автоматической проверке — проверять путь
  прохода принудительно (флаг теста), а живой HDR — вручную пользователем.

## 2. «Видеопамять, МБ» — общий объём, а не занятость

**Сделано 2026-11-02** (`HplBugReport.Graphics`).

`Line("Видеопамять, МБ", SystemInfo.graphicsMemorySize)` — 15977 во всех отчётах: это объём карты. Писать из
`HplVideoMemory.TryQuery`: занято процессом, бюджет DXGI, объём карты (`HplVideoMemory.Dedicated`); при отсутствии DXGI —
явно «нет данных». Нужно для задачи 08.

## 3. Раздел «Взгляд» теряет видимый объект

**Сделано 2026-11-03** (`Dev/HplVisibleHit`, `World/AmnesiaRenderSources`, `Editor/RenderSourceProcessor`, проверка `BugReportTests.RunLookBatch`, описание в `DevTools.md`). На 21-10-01 центр экрана — стена `wall_corner_concave_round_extension_1` (штора `curtains_curtain01_closed_1` рядом, но не на луче: треугольники шторы проверены через чтение из GPU); жалоба там на ткань в кадре, не в центре.

В 4 из 15 отчётов нет `Модель/Меш/Материалы`: луч попал в невидимые коллизии персонажа `_charcollider_box_*`
(`HplModelBuilder.IsCharColliderName`), напр. `wall_corner_concave_round_extension_1/_charcollider_box_022` стоит
перед шторой `curtains_curtain01_closed_1` (отчёт 21-10-01). Ещё: `paper01_39/Body_1/Shape_2_box` без сущности
и рендерера (21-28-20).
- Пропускать charcollider-ы (или повторять луч без них) и искать видимый рендерер: сначала по попаданию в
  отображаемую геометрию, иначе ближайший рендерер того же объекта уровня/сущности.
- Для статически батченных (`Combined Mesh (root: scene)`) писать исходный объект HPL: имя StaticObject/Entity и
  файл `.dae`/`.ent` (если в сцене этого нет — сохранить при импорте уровня в лёгком компоненте или таблице карты).
- Материал — подмеша в точке попадания (`hit.triangleIndex` у MeshCollider → submesh), остальные — отдельной строкой.
- Писать и классический, и улучшенный материал (пара из `HplTextureSet`), и включён ли рельеф меша.

## 4. Мелочи

**Сделано 2026-11-02**: углы в (−180, 180], строки «Трассировка пути», «Качество текстур», «Бюджет видеопамяти текстур, ГБ».

- Углы камеры нормализовать (сейчас рыскание 406°, −588°, −600°).
- В разделе «Графика» добавить режим трассировки (PT вкл/выкл) и бюджет текстур — сейчас его нет.

## Готово, когда

Снимок есть при HDR и без него (проверено пользователем в своей сборке), видеопамять показывает занятость,
«Взгляд» называет видимую модель/материал на точках отчётов 21-10-01, 21-12-23, 21-14-53, 21-28-20 (через их
`save.sav`), проба обновлена, `docs/Reference/DevTools.md` обновлён.
