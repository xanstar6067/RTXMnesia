# RTXMnesia — личный порт Amnesia: The Dark Descent на Unity (HDRP, позже RTX)

Проект строго личный, не распространяется. Общаться с пользователем по-русски.

## Структура репозитория

| Путь | Что это | В git? |
|---|---|---|
| `UnityProj/UMnesiaRTX/` | Unity-проект, **Unity 6000.6.2f1**, HDRP 17.6.0, Input System, DX12 → DX11 fallback | да |
| `src/AmnesiaTheDarkDescent/` | Официальный исходный код (GPLv3) — только справочник | **нет** (`/src/`) |
| `source/` | Копия нужных ресурсов игры (~1.5 GB, 11.5k файлов). Создаётся/обновляется `tools/Copy-AmnesiaResources.ps1` | **нет** (`/source/`) |
| `tools/` | Вспомогательные скрипты (PowerShell) | да |
| `docs/ScriptPort/` | Журнал переноса `.hps` → C#: `README.md` (формат), `api.md` (API движка, частота использования, статус), `chXX/<карта>.md` | да |
| `E:\SteamLibrary\steamapps\common\Amnesia The Dark Descent` | Установленная лицензионная игра (оригинал, не трогать) | — |

Правило авторского права: ресурсы Amnesia (текстуры, модели, звуки, карты, скрипты) **никогда не коммитятся**.
Любой импортированный в Unity контент должен лежать в игнорируемой папке (напр. `UnityProj/UMnesiaRTX/Assets/AmnesiaImported/` — добавить в `.gitignore` ДО первого импорта) и генерироваться
скриптами-конвертерами из `source/`. В git живёт только наш код: конвертеры, игровая логика, шейдеры, настройки HDRP.
Результаты ИИ-апскейла текстур — тоже ресурсы Amnesia, значит тоже вне git.

## Состояние Unity-проекта

Только HDRP-база: `Assets/Editor/ConfigureHDRP.cs` (меню Tools > RTXMnesia > Configure HDRP), `Assets/Settings/*`, `SampleScene`.
Hardware RT не включён. Подробности: `UnityProj/UMnesiaRTX/HDRP-SETUP.md`.
Headless-запуск Unity: `C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe -batchmode -nographics -quit -projectPath ... -executeMethod ...`.

## Ресурсы игры (E:\...\Amnesia The Dark Descent)

Всё лежит **распакованным** (архивов нет), форматы открытые. Корень = виртуальная ФС движка; список папок — `resources.cfg`.

| Папка | Размер | Содержимое |
|---|---|---|
| `maps/main/ch01..ch03`, `maps/ptest` | 690 MB | 39 `.map` (XML уровни), `.hps` (AngelScript-скрипты уровня, 41), `.map_cache` (бинарный кэш, **игнорировать**) |
| `static_objects/` | 329 MB | статические модели (`castlebase`, `cellarbase`, `dungeonbase`, …) `.dae`+`.msh`+`.mat`+`.ent` |
| `entities/` | 574 MB | интерактивные объекты, двери, враги, предметы (`.ent` XML + `.dae`/`.msh` + `.mat` + текстуры) |
| `textures/`, `graphics/`, `gui/`, `fonts/` | | `.dds` (диффуз/`_nrm`/`_spec`), `.tga`, `.jpg`, шрифты `.fnt` |
| `sounds/`, `music/`, `commentary/` | ~350 MB | `.ogg` (+6 `.wav`); `.snt` — XML-описания звуковых сущностей |
| `particles/`, `lights/`, `billboards/` | | `.ps` (частицы), определения источников света |
| `lang/` | 112 MB | `.lang` XML-локализации (есть русский) |
| `config/` | | `game.cfg`, `*_settings.cfg`, `menu.cfg`, `lang_main` и т.д. |
| `core/`, `shaders/` | | GLSL шейдеры движка, примитивы (`core_box.dae`…) — полезны только как справка |
| `custom_stories/custom_stories.zip` | **5.6 GB** | пользовательские истории; для порта **не нужны** |

Расширения: `.dds` 1901, `.dae` 1467 (COLLADA 1.4.1, Maya), `.msh` 1462 (бинарный кэш меша HPL, дублирует .dae — берём `.dae`), `.snt` 979, `.ent` 909, `.mat` 860, `.tga` 467, `.ogg` 2889.

Форматы:
- **`.mat`** — XML: `<Main Type="SolidDiffuse" PhysicsMaterial="Rock">` + `<TextureUnits>` (`Diffuse`, `NMap`, `Specular`, `Height` → имена `.dds`). Spec-карта у Amnesia → нужно конвертить в Metallic/Smoothness/Mask для HDRP.
- **`.ent`** — XML: `<ModelData>` с `Mesh Filename=...dae`, SubMesh→материал, `<Shapes>` (коллизии: Box/Cylinder/…), далее `UserDefinedVariables` (тип сущности, масса, звуки).
- **`.map`** — XML: `<StaticObject>` (769 в одной карте), `<Entity>`, `<PointLight>`/`<SpotLight>`, `<Area>`, `<Billboard>`, `<ParticleSystem>`, `<Decal>`, `<Plane>`, `<Compound>`, `<Var>`; позиции/повороты/масштаб в атрибутах. Приложен встроенный геометрический кэш для декалей/комбинированных объектов.
- **`.hps`** — AngelScript, логика уровня (триггеры, таймеры, `SetPlayerActive`, `PlayMusic`, `AddTimer`, коллбэки областей). Нужен интерпретатор-замена на C# (или ручной порт).
- **Единицы/оси**: метры; осей Y-up (проверить при импорте — COLLADA up_axis и знак Z).

## Исходный код (src/AmnesiaTheDarkDescent)

- `amnesia/src/game/` — игровая логика, префикс `Lux*`: `LuxPlayer*`, `LuxEnemy_*` (Grunt, ManPig, WaterLurker), `LuxArea_*` (Ladder, Rope, Liquid, Script, Insanity…), `LuxHandObject_*` (лампа, масло, мили), `LuxInventory`, `LuxMapHandler`, `LuxScriptHandler`, `LuxInsanityHandler`, `LuxSaveHandler`, `LuxMusicHandler` и т.д. Это **спецификация поведения** — портируем на C#, не компилируем.
- `HPL2/core/` — движок: `include/` (рендер `graphics/`, физика `physics/` (Newton), `scene/`, `resources/` загрузчики форматов, `sound/`), `sources/`. Полезно читать загрузчики `.mat/.ent/.map/.msh` для точного формата и `graphics/` для понимания G-buffer/освещения.
- `HPL2/tools/` — редакторы (`mshconverter`, `texconverter`, `editors`) — справка по форматам.
- Сборка оригинала невозможна (нужен FBX SDK 2012, которого нет) — и не нужна. Код = GPLv3.

## Ключевые факты для плана

- Оригинальный рендер: OpenGL deferred, динамическое освещение, Newton physics, AngelScript, OpenAL.
- Основное освещение игры — динамические point/spot-света + SSAO + ambient; запечённого GI нет. Это хорошо ложится на HDRP (+ RTGI/RT shadows/RT reflections позже).
- Карты состоят из огромного числа статик-объектов, ссылающихся на `.dae`/`.mat` → импорт должен кэшировать и переиспользовать меши/материалы.

## Решения пользователя

- Языки: **русский** (основной, есть русская озвучка `lang/rus`) + **английский** как запасной. Остальные языки не импортируем.
- Объём: в итоге вся игра, но идём по шагам; первая цель — **глава 1** (`maps/main/ch01`, 15 карт, начиная с `00_rainy_hall`).
- Скрипты уровней портируются **вручную на C#** (без встраивания AngelScript). Каждая карта документируется в `docs/ScriptPort/` по шаблону из README; код оригинала туда не копируется, только ссылки `файл:строка`.
- После переноса функции API обновлять её статус в `docs/ScriptPort/api.md`.
- План этапов: 1) подготовка ✓ → 2) импортёры (текстуры, материалы, модели, .ent) → 3) импорт одного уровня → 4) игрок → 5) скрипты уровней → 6) враги, безумие, звук, меню, сохранения → 7) RTX → 8) ИИ-апскейл текстур.

## Соглашения

- Код пишем на C# в `UnityProj/UMnesiaRTX/Assets/Scripts/` (наш код) и `Assets/Editor/` (конвертеры). Импортёры — Editor-скрипты, запускаемые также через `-executeMethod`.
- Не изменять ничего в папке установленной игры и в `src/`.
- Перед первым импортом ресурсов: убедиться, что `.gitignore` исключает импортированные папки (`git status` не должен показывать контент Amnesia).
