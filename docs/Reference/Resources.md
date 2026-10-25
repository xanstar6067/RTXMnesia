# Ресурсы игры, исходный код HPL2, ключевые факты

> Вынесено из CLAUDE.md (2026-10-25) без изменений текста; перекрёстные ссылки «см. «Раздел»» ищи по заголовкам в `docs/Reference/`. Индекс — в CLAUDE.md.

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
