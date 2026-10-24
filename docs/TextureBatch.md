# Массовая обработка текстур PBRify

`tools/Remix-Textures.py` использует локальный ComfyUI на `127.0.0.1:8188` и уже
установленные NVIDIA RTX Remix Nodes. Нужен Python из окружения ComfyUI (Pillow).
Установка новых пакетов, загрузка исходников на внешние сервисы и запуск Unity не требуются.

## Запуск и управление

Из корня проекта, PowerShell 7:

```powershell
./tools/Start-RemixTextures.ps1
./tools/Start-RemixTextures.ps1 -Status
./tools/Start-RemixTextures.ps1 -Stop
./tools/Start-RemixTextures.ps1 -Resume
./tools/Start-RemixTextures.ps1 -Resume -RetryFailed
```

Лаунчер запускает скрытый Python-процесс. ПК и ComfyUI должны оставаться включёнными;
закрытие чата не управляет этим процессом. Stop ставит маркер: текущая текстура
дорабатывается, следующая не запускается. Уже завершённые результаты проверяются по SHA256
и пропускаются при продолжении. Второй worker блокируется файловой блокировкой Windows.
Разрыв связи останавливает worker, сохраняя prompt_id активной задачи для продолжения.
После трёх последовательных ошибок генерации очередь останавливается.

Все служебные данные игнорируются git:

- `sourceRTXRemix/_batch/pbrify-v1/manifest.json` — список текстур, ссылки на материалы,
  состояния, SHA256 источников и результаты;
- `launcher.json`, `worker-*.log`, `worker-*.errors.log` рядом — PID и журналы;
- `manifest.stop` — просьба остановиться после текущей текстуры.

Планирование можно выполнить отдельно, без сети:

```powershell
& '<ComfyUI Python>' ./tools/Remix-Textures.py plan
& '<ComfyUI Python>' ./tools/Remix-Textures.py run --limit 2
& '<ComfyUI Python>' ./tools/test_remix_textures.py
```

Существующий manifest не перезаписывается командой plan. Для другого набора нужны
отдельный `--manifest` внутри sourceRTXRemix и при необходимости `--folder static_objects/castlebase`.

## Первый проход

План 2026-10-19: 497 уникальных текстур. Перед фоновым запуском проверены два полных
задания; при продолжении они пропущены, успешно выполнено третье. Три offline-проверки
покрывают выбор/исключение материалов, дедупликацию ссылок, проверку повреждённых
результатов при продолжении и удаление ветки alpha из RGB-графа.

План выбирает импортированные классические **HDRP/Lit opaque** материалы с исходным
`Main Type="SolidDiffuse"`, без alpha test. Albedo определяется по точному GUID из
Unity-материала, а не по короткому имени. Несколько материалов с одной текстурой
обрабатываются одним заданием. В приоритете castlebase, затем остальная статика и сущности.

Коэффициент ×4, максимум 4096 по большей стороне: 256→1024, 512→2048, 1024→4096;
пропорции сохраняются. Исходники 2K требуют отдельного прохода ×2, уже готовые 4K
не уменьшаются. Технические текстуры до 16×16 исключены.
Полупрозрачные материалы, вода, декали, alpha test и остальные специальные материалы перечисляются
в `excluded` с причиной: их нельзя автоматически проводить через этот RGB workflow.
GUI, шрифты, normal/specular-карты не передаются как albedo.

Нейросети те же, что в принятом эксперименте: PBRify UpscalerDAT2 V1, Normal V3,
Roughness V2, Height. Граф берётся из request.json первого эксперимента. Убраны
preview и ветка апскейла alpha, создававшая ложную прозрачность; RGB-ветка прежняя.
Никакие новые детали по текстовому описанию не генерируются.

Результат каждой текстуры:

`sourceRTXRemix/<путь текстуры без __albedo.png>/pbrify-x4-batch-v1/`

Четыре PNG (`__albedo`, `__normal_ogl`, `__roughness`, `__height`) и `result.json`
с SHA256 файлов, источника, графа и списком материалов. PNG сжимаются **без потерь**;
повторное чтение проверяет побайтное совпадение пикселей. ComfyUI также хранит свои
несжатые рабочие копии в стандартном output на диске C; скрипт их не удаляет.

Генерация не меняет оригиналы и Unity-ассеты. Автоимпорта в Unity нет.
Завершённые `result.json` подключаются через меню **Tools → RTXMnesia → Import →
Enhanced textures: all completed PBRify sets** или batch-метод
`RTXMnesia.Importer.EnhancedMaterialConverter.ConvertReadyBatch` при закрытом редакторе.
Импортёр проверяет SHA256, ссылки на исходную albedo и тип материала, сохраняет
emission/UV/двусторонность, добавляет отдельные PBR-пары в существующий каталог.
Перед импортом полного пакета запустить `tools/Prepare-EnhancedTextures.py` (Python с Pillow,
Unity закрыт): проверка источников и GPU-сжатие в DDS BC7/BC5/BC4 через NVTT из RTX Remix.
Исходные PNG/workflow не удаляются. Металлы сохраняют исходный specular F0;
cutout поддерживается с сохранением масок, порогов и mip coverage.
PBRify не удаляет нарисованные тени исходного albedo. Подробности первого материала:
`docs/EnhancedTextures.md`.

## Отдельный проход alpha test

```powershell
./tools/Start-RemixTextures.ps1 -Kind alpha-test
./tools/Start-RemixTextures.ps1 -Kind alpha-test -Status
./tools/Start-RemixTextures.ps1 -Kind alpha-test -Stop
./tools/Start-RemixTextures.ps1 -Kind alpha-test -Resume
```

План 2026-10-19: **119 уникальных текстур для 121 материала**. Отбираются SolidDiffuse
HDRP/Lit opaque с включённым alpha test и непустой прозрачностью в импортированном albedo.
31 материал с включённым alpha test, но полностью непрозрачным импортированным albedo,
отмечен `no_transparency_in_albedo`; два крупных исходника требуют отдельного прохода ×2.
Мягкая полупрозрачность, вода и смешиваемые декали не попадают в эту очередь.
Декоративные плоскости, импортированные именно как alpha test, обрабатываются.

Маска берётся из **готового albedo импортёра**, уже содержащего объединение Diffuse и
отдельного Alpha-источника, если он указан в исходном материале. Точное сопоставление
идёт по GUID, поэтому варианты `__albedo~<имя маски>.png` не теряются и не подменяются.

Входной RGB обрабатывается теми же PBRify-моделями; alpha через нейросеть не проходит.
После получения результата исходный 8-битный A увеличивается nearest ×4 и возвращается
в RGBA albedo. Каждый исходный образец занимает ровно 4×4 пикселя: сохраняются отверстия,
тонкие детали и площадь покрытия при **любом** пороге alpha test. Контур намеренно
не перерисовывается и не сглаживается. Отдельно сохраняется `__alpha.png` (L).

Вывод: `sourceRTXRemix/<путь текстуры>/pbrify-alpha-x4-batch-v1/`, четыре PBR-карты плюс
маска alpha и `result.json`. Для каждого материала записаны его исходные alpha cutoff и
double-sided; варианты с разными порогами не объединяются в один Unity-материал.
Статус и журналы: `sourceRTXRemix/_batch/pbrify-alpha-v1/`.

Обе очереди могут работать с одним ComfyUI: сервер выполняет задания последовательно.
Скрипты не создают второй GPU-сервер. Остановкой каждой очереди управляет её собственный
маркер. Автоимпорта по завершении генерации нет: подготовка DDS и импорт выполняются отдельно.
На 2026-10-19 обе очереди завершены: 497 RGB + 119 cutout наборов для 630 материалов.
Полный импорт сохраняет маски, отсечение, mip coverage, двусторонность и настройки теней;
улучшенные материалы загружаются по использованию на карте, текстуры поддерживают streaming.

## Проход ×2 для исходников 2K (2026-10-24)

Первые очереди пропустили 31 материал с исходником больше 1024 (`needs_separate_2x_workflow`):
×4 дал бы 8K. Среди них стены `mansionbase` (глава 1), `cisternbase`, `sanctumbase`, атлас
`dungeonbase` (почти все стены глав 2–3 и Justine), `torture_chancel`, `gigantic_stones`, враги
grunt и brute, Агриппа. Без набора они и с «Улучшенными» текстурами оставались классическими.

```powershell
& '<ComfyUI Python>' ./tools/Remix-Textures.py plan --scale 2
& '<ComfyUI Python>' ./tools/Remix-Textures.py run --scale 2
& '<ComfyUI Python>' ./tools/Remix-Textures.py plan --scale 2 --kind alpha-test
& '<ComfyUI Python>' ./tools/Remix-Textures.py run --scale 2 --kind alpha-test
```

Те же модели и граф; после DAT2 (×4) добавлен узел `ImageScale` (Lanczos) до ×2, из него строятся
albedo, нормаль, шероховатость и высота — 2048 → 4096, предел 4K соблюдён. Маска alpha — nearest ×2.
План 2026-10-24: 27 непрозрачных текстур (29 материалов) и 2 cutout (Агриппа, `strange_sketch`).
Вывод — `pbrify-x2-batch-v1` / `pbrify-alpha-x2-batch-v1`, статус — `_batch/pbrify-x2-v1`,
`_batch/pbrify-alpha-x2-v1`; в `result.json` поле `scale: 2`. `Prepare-EnhancedTextures.py` и
`EnhancedMaterialConverter.ConvertReadyBatch` берут и эти папки.
Прогон 2026-10-24: 27 + 2 набора за ~31 мин (RTX 5080), все карты 4096; `Prepare-EnhancedTextures.py` —
94 новых DDS, итого 645 наборов / 16.6 ГБ; `ConvertReadyBatch`: READY PASS, 645 наборов, 661 пара
материалов, ProjectSettings не изменились.
