# Улучшенные текстуры

Первый материал — `static_objects/castlebase/wall/castlebase_wall`, PBRify 4K, 2026-10-19.
Переключение: **Настройки → Графика → Текстуры → Классические / Улучшенные → OK**.
Черновик и «Отмена» работают через существующую систему настроек; выбор сохраняется как
`HplUserSettings.textures` (0 / 1). Освещение выбирается независимо от текстур.
Пока замена есть только для этого камня. Остальные материалы сохраняют классический вид.

## Исходные данные и импорт

Исходники эксперимента, включая workflow и контрольные суммы:
`sourceRTXRemix/static_objects/castlebase/wall/castlebase_wall/pbrify-x4-20261019-181639/`.
Оригиналы в `source/`, установленная игра и классические Unity-материалы не изменяются.
Обе папки ресурсов и весь `Assets/AmnesiaImported/` исключены из git.

Меню: **Tools → RTXMnesia → Import → Enhanced textures: castlebase wall (PBRify)**.
Batch-метод: `RTXMnesia.Importer.EnhancedMaterialConverter.ConvertBatch`.
Перед batch-запуском редактор проекта должен быть закрыт.

Результаты генерируются отдельно:

- `Assets/AmnesiaImported/Enhanced/static_objects/castlebase/wall/castlebase_wall__*.png`;
- `Assets/AmnesiaImported/Enhanced/static_objects/castlebase/wall/castlebase_wall.mat`;
- `Assets/AmnesiaImported/Resources/HplEnhancedTextures.asset` — каталог пар материалов.

Повторный импорт обновляет ассеты на месте, сохраняя GUID и ссылки. Каталог дополняется,
а не обнуляется: при добавлении следующих материалов нужно добавлять пары.
Пересобирать игровые карты для этой замены не требуется. Для готового плеера нужна новая сборка.

## PBR и конвенции

Используется стандартный **HDRP/Lit, opaque, metallic workflow**. Камень — диэлектрик:
metallic = 0, обычный F0 HDRP. Старые HPL specular-карты к новому материалу не примешиваются.

| Карта эксперимента | Назначение в HDRP | Цветовое пространство |
|---|---|---|
| albedo | Base Color RGB, альфа игнорируется | sRGB |
| normal_ogl | Normal, сила 1, BC5 | linear, Normal Map |
| roughness R | Mask A = 1 − roughness | linear |
| height R | Height, белое = выше | linear |

Mask R = 0 (metallic), G = 1 (AO отсутствует), B = 1 (маска detail).
Выбор красного канала scalar-карт явный: выходы модели RGB немного различаются по каналам.
Albedo/mask/height хранятся в BC7; все карты 4096×4096, Repeat, mipmaps, Trilinear.
Качество и анизотропия продолжают управляться общими графическими настройками.

**Имя `normal_ogl` в этом эксперименте не соответствует фактическому знаку Y.**
На исходных PNG корреляция `∂height/∂imageX` с normal R = −0.721455,
`∂height/∂imageY` с normal G = −0.727695 (Y изображения направлен вниз).
Для согласования с Y-up tangent normal Unity импортёр разворачивает G ровно один раз
через `TextureImporter.flipGreenChannel`. Height не инвертируется. PNG эксперимента не меняется.
Это решение относится к проверенному выводу NormalV3; для следующего workflow конвенцию
нужно проверять заново, а не выбирать по имени файла.

В workflow SaveTexture выбран `linear`, но сам LoadImage читает байты входного PNG без
декодирования sRGB, а сохранение `linear` записывает их без дополнительной функции переноса.
Поэтому albedo импортируется как sRGB, scalar-карты и нормали — как linear.

Высота используется как **pixel displacement (POM)** с амплитудой 2 см и 8–32 шагами.
Это сдержанный исходный масштаб для предсказанной моделью карты. Геометрия, силуэт,
коллизии и геометрические RT-тени от неё не меняются. POM не является настоящим
displacement для path tracing. RTGI/отражения используют PBR-свойства HDRP/Lit.
Albedo по-прежнему содержит нарисованные тени оригинала; этот эксперимент не выполняет delighting.

## Переключение

`HplEnhancedTextures` загружает каталог из Resources (так обе стороны и их карты попадают
в сборку), меняет только `Renderer.sharedMaterials` при применении настроек и загрузке сцены.
Неактивные объекты также обрабатываются. Исходные материалы не редактируются и `.material`
не вызывается. Неподдерживаемые материалы остаются прежними.

Цветовые копии/клоны непрозрачного HDRP/Lit распознаются по точной ссылке на исходную
albedo, не по имени. Для них создаётся кэшированный улучшенный вариант с исходными tint
и UV scale/offset. Обратная таблица возвращает **точные исходные ссылки**, сохраняя
чужие слоты, null и MaterialPropertyBlock. Выход из Play освобождает временные варианты.
При смене набора сбрасывается накопление path tracing, чтобы неподвижная камера
не продолжала накапливать изображение прежних материалов.

Для объектов, создаваемых после `sceneLoaded`, есть `HplEnhancedTextures.Apply(renderer,
HplGraphics.Textures == 1)`. Текущий камень используется статической геометрией карт.

## Проверки

- `RTXMnesia.EditorDev.EnhancedTextureTests.RunBatch` (`-batchmode -nographics -quit`):
  повторный импорт и GUID; SHA256 классических материалов, карт и meta; настройки PBR;
  меню/JSON; пять циклов переключения; tint/UV/null/чужие слоты/MaterialPropertyBlock;
  все рендереры обеих первых карт и точное восстановление ссылок.
- `RunRenderBatch` (`-batchmode -quit`, GPU): освещение атласа, classic/enhanced/restored,
  сравнение пикселей и снимки первой карты. Не сохраняет сцены.
- `RunPlayBatch` (`-batchmode`, **без `-quit`**): применение через настоящее событие настроек,
  загрузка второй карты с выбранными улучшенными текстурами, возврат к классике.
  Временно подставляет выбор в PlayerPrefs для загрузки карты и восстанавливает точное исходное
  значение в `finally`; файлы профиля не меняет. Процесс сам завершает работу.
- `RTXMnesia.EditorDev.BuildCheck.RunBatch`: компиляция скриптов Windows-плеера и obsolete API.

Отчёты и PNG: `UnityProj/UMnesiaRTX/Logs/EnhancedTextureCheck/`, логи batch-запусков в `Logs/`.

Результат 2026-10-19: 43 проверки импорта/обратимости прошли, GPU PASS, Play PASS.
Проверены 246 рендереров камня в `00_rainy_hall` и 196 в `01_old_archives`.
После всех GPU/Play-проверок SHA256 всех 12 файлов классического материала, карт и meta
остались прежними. BuildCheck: 33 сборки, 0 ошибок, 0 obsolete API warnings.
