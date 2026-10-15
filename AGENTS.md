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

HDRP-база (`Assets/Editor/ConfigureHDRP.cs`, меню Tools > RTXMnesia > Configure HDRP, `Assets/Settings/*`, `SampleScene`), импортёры ресурсов и уровней (`Assets/Editor/Importer`), компоненты уровня, игрок и рантайм скриптов уровней (`Assets/Scripts`). Первый уровень: `Assets/AmnesiaImported/Levels/maps/main/ch01/00_rainy_hall.unity` (генерируется, не в git). В нём Play запускает скрипт уровня — вступление, пробуждение, события по зонам; пропы — физические тела с шарнирами, их можно брать, толкать, открывать (двери, ящики, рычаги, колёса), зажигать лампы, подбирать предметы; звук и музыка как в оригинале; без частиц.
Fixed Timestep = 1/60 (как шаг логики HPL2, `EngineInitVars mlUpdateRate`).
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
- План этапов: 1) подготовка ✓ → 2) импортёры (текстуры, материалы, модели ✓; префабы .ent → вместе с шагом 4) → 3) импорт одного уровня ✓ (`00_rainy_hall`; частицы .ps — заглушки) → 4) игрок: 4.1 передвижение ✓, 4.2 физика сущностей ✓ (Rigidbody, джоинты, физ. материалы), 4.3 взаимодействие ✓ (фокус/прицел, Grab, Push, Slide, двери, рычаги, колёса, лампы, предметы), 4.4 лестницы, фонарь → 5) скрипты уровней (идёт: рантайм, эффекты, HUD, `00_rainy_hall` ✓) → 6) звук ✓, враги, безумие, меню, сохранения → 7) RTX → 8) ИИ-апскейл текстур.

## Импортёры (Assets/Editor/Importer, namespace RTXMnesia.Importer)

Меню: **Tools > RTXMnesia > Import**. Лог: `UnityProj/UMnesiaRTX/Logs/AmnesiaImport.log`.
Batch-методы `RTXMnesia.Importer.ImportMenu.*` (Unity с этим проектом должен быть закрыт; проверять lock `Temp/UnityLockfile`):
`ConvertMaterialsBatch`, `ConvertModelsBatch` с `[-amnesiaFolder <папка в source>] [-forceTextures] [-describe a.dae,b.dae]`;
`RenderMaterialsBatch`/`RenderModelsBatch` — сцена-витрина + рендер в `Logs/<Kind>Preview.png` (без `-nographics`, нужен GPU).
Быстрая проверка компиляции без Unity: Roslyn `Editor/Data/DotNetSdk/.../csc.dll` + ссылки на `Managed/UnityEngine/*Module.dll` и HDRP dll из `Library/ScriptAssemblies` (фасады UnityEngine.dll/UnityEditor.dll не подключать).
Полный импорт `static_objects` + `entities`: ~3 мин, ~1 GB в `Assets/AmnesiaImported`.

- `ResourceIndex` — поиск как `FileSearcher::GetFilePath` HPL2: индекс по имени, при дубликатах сравнение компонентов папок с конца в обоих направлениях. Путь из `.map/.ent/.dae` нельзя отбрасывать: `castlebase/wall/default.dae` и `castlebase/stairs/default.dae` — разные модели. Старые префиксы дисков экспортёра не мешают сравнению папок; для голого имени используется папка ссылающегося файла, затем порядок `resources.cfg`.
- `DdsDecoder` — DXT1/3/5, ATI2(BC5), несжатые 24/32 бит, кубкарты. Форматы в игре: DXT1 1131, DXT5 714, BGRA32 36, ATI2 12, кубкарт 4.
- `TextureConverter` — DDS → PNG в `Assets/AmnesiaImported/Textures/<путь>__<роль>.png`; роли: albedo, normal, mask, specular, height, emissive. Настройки импорта — `TextureImportRules` по роли из имени.
- `MaterialConverter` — `.mat` → HDRP материал в `Assets/AmnesiaImported/Materials/<путь>.mat`; существующие обновляются на месте (GUID сохраняется). Константы тюнинга вверху файла.
- `ImageLoader` — .dds/.tga/.jpg/.png → пиксели «верхняя строка первой» (HPL2 грузит всё через DevIL с IL_ORIGIN_UPPER_LEFT). Текстура без расширения → перебор форматов, как `TextureManager::FindTexture2D`.
- `TextureConverter.Version` — при изменении математики конвертации увеличить: PNG старше момента смены версии перегенерируются.
- `ModelConverter` + `HplCollada` + `HplModelBuilder` — модели без скелета читаются собственным парсером Collada (1.4 и старый 1.3), сохраняются в `Models/<путь>.prefab` и `<путь>_meshes.asset`. Собственный масштаб узла запекается в вершины и снимается с узла; единицы файла применяются только для FBX COLLADA exporter. Встроенный импорт Unity (`ModelImportRules`) остаётся для скелетных моделей. При изменении сборки или выбора ресурсов повышать `HplModelBuilder.Version`, чтобы пересобрать старые префабы.
- Коллизия статики: примитивный коллайдер отменяет mesh-коллизию только своего подмеша; наличие обычного `_collider`-меша отменяет визуальные mesh-коллайдеры всей модели. `Collides=false` отключает коллизии экземпляра.

Проверенные по исходникам HPL2 конвенции:
- UV COLLADA: движок делает `v = 1 - v`, DDS грузится верхней строкой в v=0 → в Unity UV не трогаем, PNG сохраняем как DDS (верх = верх).
- Нормали: бинормаль движка смотрит «вниз по картинке» → зелёный канал инвертируем.
- Specular-карта: R = интенсивность, G = степень `exp2(G*10)+1`. В HDRP: Specular Color workflow, F0 = R × 0.08, smoothness = 1 − sqrt(sqrt(2/(n+2))).
- Height — в альфа-канале. Юнит `Alpha` = alpha test по альфе диффуза. `Illumination` = emissive (у ламп регулируется в игре).
- Имя материала у меша = имя текстуры из `<library_images>` `.dae` с расширением `.mat`.
- Height-карта Amnesia — это **глубина** (белое = глубже, relief mapping) → инвертируется для HDRP. Проверено численно: градиент инвертированной карты совпадает с normal map по знаку в обоих каналах (заодно подтверждает инверсию G).
- **Оси**: Unity-импорт Collada отражает X: HPL `(x,y,z)` → Unity `(-x,y,z)` (проверено на `corner_concave_round.dae`). Позиции из `.map`/`.ent` переводятся так же, повороты (Euler XYZ, радианы, M = Rz·Ry·Rx) — отражением: угол X сохраняется, Y и Z меняют знак; порядок вершин треугольников не меняется. Всё это — `Assets/Scripts/Hpl/HplSpace.cs`; проверено на уровне (стены стыкуются, двери/люстры на местах). Свет и камеры HPL смотрят в локальный −Z (Unity +Z) → `HplSpace.LookRotation`. UV из .map (плоскости, декали, билборды): v = 1 − v.
- **Z_UP Collada**: собственный парсер повторяет HPL2: вершины/нормали `(x,y,z) → (-x,z,y)`, translate/scale `(x,z,y)`, rotate и matrix без перестановки. Затем применяется общее отражение X HPL→Unity. Старые поправки к встроенному импорту для нескелетных моделей больше не используются.
- Блендинг Mul/MulX2 (в HDRP нет; декали грязи и сырости `static_objects/decals`, лужи, бутылки, стёкла — ~30 материалов, на `00_rainy_hall` ~290 экземпляров `dirt_floor01`) — свой шейдер `Assets/Shaders/HplMultiply.shader`: множитель на освещённый цвет (HPL2 умножает альбедо в G-buffer — разница только в бликах), считается в гамме как в HPL2 (t·цвет экземпляра, ×2 для MulX2, клэмп для Mul), декали — до остальной прозрачности (очередь 2900) и с alpha test 0.01; туман HPL2 накладывается после декалей → множитель ослабляется непрозрачностью тумана (глобал `_HplFog` из `HplLighting.SetMultiplyFog`). Цвет декали карты для него не клэмпится (1.5 у `dirt_floor01_272`). Декаль Add → HDRP/Unlit additive, Alpha → HDRP/Decal. Без своего шейдера эти материалы выглядели белыми прямоугольниками.
- TODO: вода, DissolveAlpha, кубкарты отражений (заменяются HDRP/RT). ~10 моделей с материалом без .mat (кости, паук) — вероятно, материал задаётся в `.ent` (`SubMesh Material=`).
- `.ent` (сущности): `ModelData` = Mesh + SubMesh (материал-override), Shapes, Bodies (масса, физ. материал, какие подмеши привязаны), Joints (двери — JointHinge), Bones, Animations; `UserDefinedVariables EntityType="SwingDoor|..."` + звуки/поведение. Префабы из .ent делать на шаге 4 вместе с механиками.
  Важно: SubMesh в `.ent` хранит трансформ подмеша относительно сущности, он **заменяет** трансформ узла Collada (масштаб узла остаётся). Пример: дверь `castle` развёрнута там на 180°. Внутри `.ent` есть свои свет/билборды/частицы/звуки (`ModelData/Entities`), их позиции × масштаб экземпляра.

## Импорт уровня (LevelConverter, шаг 3)

Меню: **Tools > RTXMnesia > Import > Level: 00_rainy_hall (ch01)** / **Level: choose .map...**. Batch: `ConvertLevelBatch`, `RenderLevelBatch [-amnesiaMap maps/main/ch01/X.map] [-convert] [-views "x,y,z,yaw,pitch;..."] [-pick "u,v;..."]` (рендер с камеры, всех PlayerStart и доп. точек → `Logs/Level_<map>_NN_*.png`, без `-nographics`; `-pick` — какие рендереры и материалы под точкой кадра доп. видов, доли, v сверху, в Editor.log), `DescribeModelsBatch -describe a.dae,b.dae`. Перед уровнем недостающие модели/материалы/cookie импортируются сами. 00_rainy_hall: ~20 с.
Результат: `Assets/AmnesiaImported/Levels/<путь карты>.unity` + `<карта>/<карта>_generated.asset` (меши плоскостей/декалей/квад билбордов/цилиндр коллизии, варианты материалов по цвету) + `<карта>_volume.asset`.

- StaticObject → экземпляр собственного префаба с трансформом `T·R·S` из карты и готовой коллизией HPL2. Plane → порт `MeshCreator::CreatePlane`. Decal → готовый мировой меш из .map + HDRP/Decal материал (цвет → вариант материала, без вертекс-цвета).
- Entity → GameObject с `AmnesiaEntity` (файл, тип, переменные .ent + экземпляра); модель распакована, подмеши по `.ent`; `Body_*` с `AmnesiaBody` (масса, гравитация, damping, макс. скорости, CollideCharacter, физ. материал) и коллайдерами из Shapes. Тела без форм пропускаются. Цилиндр → выпуклый MeshCollider. `CommentaryIcon` выключены.
  - Подмеши и дочерние объекты `.ent` (свет, билборды, частицы) прикрепляются к телу по `<Children>` его ID; остальные и скелетные модели — к первому телу, мировая поза сохраняется.
  - Joints → `AmnesiaJoint` (данные; градусы → радианы; pivot/pin в локальном пространстве сущности). Ошибки оригинала сохранены: тип `joinscrew` с опечаткой загружается как ball.
  - Физ. материалы: `SurfaceConverter` — `materials.cfg` → `Assets/AmnesiaImported/Physics/surfaces.asset` (`HplSurfaces`, PhysicsMaterial — под-ассеты, обновляются на месте). Статика берёт материал по `PhysicsMaterial` своего `.mat`, тела — из `.ent`.
  - Шаблоны сущностей: неактивные, в масштабе 1, под корнем `EntityTemplates` — break-сущности, сломанные двери, ContainedItem и литералы `.ent` из `.hps` (без расширения — добавляется `.ent`), а также их собственные Break/Broken. `HplMap.CreateEntity` клонирует их.
- Area → `AmnesiaArea` (+ триггер BoxCollider размера Scale, кроме PlayerStart). ParticleSystem → заглушка `AmnesiaParticleSystem` (TODO конвертер .ps). Sound → `AmnesiaSound` (файл .snt, UseDefault, расстояния, громкость), в рантайме — звуковая сущность (см. «Звук»). BoxLight, FogArea, точечные gobo (кубкарты), скайбокс — пока пропускаются с предупреждением.
- Свет → HDRP через `AmnesiaLight` (хранит параметры HPL, включая flicker — пока не симулируется). Пересчёт яркости — `Assets/Scripts/Hpl/HplLighting.cs`: фиксированная экспозиция EV100 = 3; HPL-свет почти не затухает в радиусе, HDRP — 1/d², совпадение на 0.5 радиуса (`ReferenceDistance`); `Candela = π·d²·1.2·2^EV`. Spot с gobo → Pyramid + cookie (роль текстуры `Cookie`, перевёрнута по вертикали, как проецирует HPL).
- Эмиссия Lit и цвет тумана в HDRP экспонируются → × `UnitRadiance` (1.2·2^EV); **цвет HDRP/Unlit не экспонируется** (ShaderPassForwardUnlit: `_DeExposureMultiplier` = 1) → цвет HPL как есть. При смене EV — перегнать материалы и уровень.
- Билборды: FixedAxis — обычный MeshRenderer; Point/Axis рисует скрытый несохраняемый дочерний объект `AmnesiaBillboard` ([ExecuteAlways], поворот к камере в `beginCameraRendering`, работает и в Scene view). TODO: ореолы (IsHalo) в HPL рисуются без depth test с затуханием по occlusion query — пока с обычным depth test. Связь билборда со светом (`iLight::AttachBillboard`) — в рантайме `HplMap`: билборд сущности → свет той же сущности, билборд карты → свет карты; цвет = базовый × rgb света.
- Окружение: Volume — без неба (HPL без скайбокса не даёт ambient), фикс. экспозиция, без тонмаппинга, туман HDRP подобран так, что на FogEnd непрозрачность = alpha цвета тумана (`HplLighting.ApplyFog`; параметры тумана карты — в `AmnesiaLevel`, скрипты меняют их в игре). Игрок (`AmnesiaPlayer` + камера, фон чёрный) на PlayerStartArea_1; `config/game.cfg`, `config/menu.cfg` и `config/default_user_keys.cfg` → `Assets/AmnesiaImported/Config/*.asset` (`HplConfig`) при каждом импорте уровня.
- На корне сцены — рантайм скриптов: `HplMap` (+ `HplEffects` с шейдером и таблицей сепии, `HplHud` с иконкой записи). Тексты — `LanguageConverter` → `Assets/AmnesiaImported/Lang/russian.asset` (`HplLanguage`). Порядок файлов как в HPL2, первая запись имени побеждает: `config/lang_main/russian.lang`, `config/base_russian.lang`, затем английские для недостающих. Ещё в нём список `.ogg` папки языка (`lang/rus`) с длительностями из заголовков Ogg (`OggInfo`). Нужные рантайму текстуры игры конвертируются ролями `Lookup` (линейная, без мипов) и `Gui`.
- PlayerStart: игрок смотрит вдоль **+Z** зоны (`cLuxAreaNodeLoader_PlayerStart`: forward = R·(0,0,1)), без разворота на 180°.

## Игрок (шаг 4, Assets/Scripts/Player, namespace RTXMnesia.Player)

Порт по исходникам, числа — только из `game.cfg` через `HplConfig` (`GetFloat("Player_Body","Mass")` как `cConfigFile`), в коде их нет.
- `HplCharacterBody` — порт `iCharacterBody` (CharacterBody.cpp): не Rigidbody, а вертикальный цилиндр, который двигается скоростями по направлениям (ускорение/торможение/разворот), гравитацией и силами, и выталкивается из мира после каждого шага; ступени — лучами (AccurateClimbing: 3 луча), камера на высоте стоячего тела + `CameraPosAdd`, сглаживание по `CameraSmoothPosNum` шагам. Формы — выпуклые MeshCollider-цилиндры на слое 2 (Ignore Raycast: их не видят запросы по умолчанию); неактивный размер (присед/стоя) — trigger. Не портировано: прилипание к движущимся телам (GravityAttachment), соединение с телом (Push).
- `HplPhysics.CheckShapeWorldCollision` — аналог `iPhysicsWorld::CheckShapeWorldCollision`: OverlapBox + `Physics.ComputePenetration`, вектор выталкивания = покомпонентный максимум по контактам; с одним коллайдером до 4 итераций (PhysX даёт одно разделение на пару, Newton — список контактов). Тела с `CollideCharacter=false` и триггеры игнорируются. Поправка: если коллайдер выталкивает вбок, а на 3 см выше форма от него свободна, — вместо толчка подъём на нужную высоту (бинарный поиск, `LiftOverLowEdge`). Иначе ковры 1–2 см и пороги останавливают игрока: PhysX-меш толкает и по нормали низкой боковой грани, а подъём на ступени HPL2 видит только высоту > 2.5 см; Newton поднимал на такие края.
- `PlayerMoveNormal` — `cLuxMoveState_Normal`: ходьба/бег/присед/прыжок (сила прыжка + ослабление гравитации 0.22 с), покачивание головы и шаги на минимуме, отскок головы при приземлении, урон от падения. `PlayerLean` — `cLuxPlayerLean` (до 0.5 м и 15°, голова-цилиндр останавливается о стены).
- `AmnesiaPlayer` — `cLuxPlayer` + `cLuxInputHandler`:
  - логика 60 Гц разделена на `StepLogic` и `StepPhysics`. На уровне со скриптом их вызывает `HplMap` в порядке движка (`steppedByMap`), иначе FixedUpdate;
  - камера интерполируется между шагами;
  - мышь — каждый кадр: пиксели × чувствительность / (1.7 × высота экрана) × 2 рад, сглаживание как `GetSmoothMousePos` (веса 0.7^возраст в шагах 60 Гц — не зависит от FPS), питч ±70°;
  - смещения головы `HeadPosAdd` (Main/Bob/Lean/Script/ScreenShake/…) как `MoveHeadPosAdd`: x — вдоль правой оси тела, z — вдоль forward, без зеркала;
  - для скриптов: `active` (неактивный игрок не получает ввода), затухания FOV/aspect/крена как `cLuxPlayer::UpdateCamera`, `PlayerLookAt` (порт `cLuxPlayerLookAt`; углы считаются в терминах HPL2: yaw_HPL = π − yaw, pitch_HPL = −pitch, крен совпадает), здоровье/рассудок/масло с событиями для эффектов;
  - клавиши: WASD, Shift, Space, Ctrl (переключает присед), Q/E наклон, ЛКМ взаимодействие, ПКМ бросок, R — вращать взятый предмет мышью, колесо — дальность; F2 свободная камера (`FlyCamera`), Esc отпускает мышь;
  - `AddYaw`/`AddPitch` принимают величины HPL (мышь: `AddYaw(-x)`, `AddPitch(y)`), ходьба/бег/прыжок/присед и действия идут через состояние игрока — оно может их перехватить; ограничения взгляда — стек `PushViewLimits`/`PopViewLimits`;
  - звуки: прыжок, присед, вставание, урон от падения — `PlaySound` (PlayGuiSoundData как мировой звук); шаги — `step_<walk|run|sneak>_<StepType>` материала под ногами (`GroundCollider`, иначе "rock"); шаги в воде и слух врагов — шаг 6.
- Состояния игрока (`PlayerStates.cs`, порт `iLuxPlayerState`): `PlayerStateNormal` — фокус лучом 20 м с триггерами (пропускает слой персонажа, неактивные сущности и тела/области, с которыми нельзя взаимодействовать; статика луч блокирует), прицел по типу сущности, текст фокуса. `PlayerStateInteract.cs` — Grab (PID сила 400/0/40, момент 40/0/0.4, масса × GrabMassMul, без гравитации, бросок, вращение), Push, Slide, RotateBase → SwingDoor/Lever/Wheel. Вся математика — в пространстве HPL (`HplCamera*`, `HplMatrix`). Ошибки оригинала сохранены (prevPos колеса). `StepPostUpdate` — силы удержания после симуляции.
- Прицелы — `HplHud`: `hud_crosshair_*.tga` (роль `Gui`), видимость как `CanDrawCrossHair`; затемнение экрана затемняет и прицел с текстом фокуса (в HPL fade рисуется поверх HUD).
- Проверка без Play: `-executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch [-amnesiaMap ...] [-script "start PlayerStartArea_x2; turn 90; w 6; run 4; crouch; jump; leanr 1; shot name"]` → состояние в `Logs/AmnesiaImport.log`, снимки `Logs/Player_*.png` (без `-nographics`). Проверено на 00_rainy_hall: шаг 2.5 м/с, бег 4.25, лестница PlayerStartArea_x2 вниз/вверх, камера 1.75 / 1.15 (присед), прыжок ~1 м. PlayerStartArea_x1 — отладочный старт снаружи уровня (оттуда можно уйти в пустоту, как и в оригинале).

## Физика сущностей (шаг 4.2, Assets/Scripts/Dynamics, namespace RTXMnesia.Dynamics)

Порт Newton-обёртки HPL2 (`iPhysicsBody`, `iPhysicsJoint*`, `iPhysicsWorld`) поверх PhysX.
- Пространство: позиции/направления HPL → Unity `(−x, y, z)`; осевые векторы (ω, момент) → `(x, −y, −z)`; кватернион `(x, −y, −z, w)`; матрица `S·M·S`, S = diag(−1,1,1,1) — `HplSpace`. `HplMatrix` — `cMatrixf`/`cMath` (MatrixRotate XYZ = Rz·Ry·Rx, GetRight/Up/Forward — строки, `GetAngleDistance`, `HplPid` = `cPidController`). Логику с углами писать в пространстве HPL через `Hpl*`-аксессоры тела, а не переводить формулы.
- `HplPhysicsBody` (Rigidbody): масса 0 = кинематическое, interpolation только в Play, итерации решателя 12/4. `AddImpulse` = `ForceMode.VelocityChange` (`NewtonBodyAddImpulse` принимает Δv), в точке — масса × Δv. Damping Unity = clamp(d, 0.001, 1) × 0.02 × 60. Ограничение скорости повторяет ошибку HPL2 (длина сравнивается с max²). Статическое движение (`StaticLinearMove`/`StaticAngularMove`) — кинематический MovePosition/MoveRotation, поворот `RotateXYZ(ω·dt)·R` в пространстве HPL.
- Слои: `CollideNonCharacter=false` → слой 30 (`NoCollideLayer`, не сталкивается ни с чем); `CollideCharacter=false` → `Physics.IgnoreCollision` с формами персонажа (`SetCharacterShapes`).
- Комбинирование трения/упругости: порядок приоритетов Average < Min < Multiply < Max совпадает с Unity.
- `HplJoint`: Hinge → `HingeJoint`, остальные → `ConfigurableJoint`. Угол шарнира HPL = −(правый поворот ребёнка вокруг пина HPL) = AngleAxis в Unity вокруг отражённого пина = `HingeJoint.angle` (проверено `PhysicsTests`), лимиты PhysX = [min°, max°]. Диапазоны за ±π или min > max — мягкий лимит коррекцией скорости. Slider — connected anchor в центре диапазона, симметричный `linearLimit`. Ball — twist ±MaxTwist, swing по конусу, 0 = свободно. Перед шагом: обновление лимитов (допуск 0.002 рад / 0.0005 м), LimitAutoSleep, Sticky, события MinLimitReached/MaxLimitReached; после — разрыв по `currentForce` ≥ BreakForce.
- `HplPhysicsWorld`: `AddEntity` создаёт тела, затем джоинты; поиск тела по Rigidbody/коллайдеру/данным; `Step` для edit/batch.
- `HplBodyContacts` ([ExecuteAlways]) — `OnCollisionEnter` для пропов, реагирующих на удар; скорость до удара — `PreviousVelocityAt`.
- ContinuousCollision из `.ent`: динамическим телам — `ContinuousDynamic` (протяжка), кинематическим — `ContinuousSpeculative`. Спекулятивный режим пропускал тонкий пол под быстро вращающимися осколками и накачивал скорость удерживаемому предмету, вдавленному в пол.
- Прогон шага: в Play FixedUpdate делает часть до симуляции, корутина `WaitForFixedUpdate` — после. В edit/batch `Physics.simulationMode = Script` и `HplMap.Step` вызывает `Physics.Simulate`.
- Проверка: `-executeMethod RTXMnesia.EditorDev.PhysicsTests.RunBatch` — синтетические шарнир (знак угла, лимиты, закрытие [0°, 2°], мягкий лимит 120°) и слайдер [0, 0.5].

## Пропы и взаимодействие (шаг 4.3, Assets/Scripts/Props, namespace RTXMnesia.Props)

- `HplProp` (ScriptApi, порт `iLuxProp` + `iLuxPropLoader::AfterLoad`): тела, джоинты, здоровье/прочность, эффекты (свет `FadeTo`, билборды и эмиссия `_EmissiveColor` через MaterialPropertyBlock), статическое движение, `SetDisableCollisionUntilOutsidePlayer`, связанные пропы. Переменные `.ent` — `Var*`, экземпляра карты — `InstanceVar*`, числа конфига — `ConfigFloat`.
- `PropFactory.Create` по `EntityType`: `PropObject` (Grab/Push/Slide/Static, разбивание по энергии удара или здоровью → break-шаблон, ContainedItem), `PropSwingDoor` (Closed — лимит 2°, Locked, автозакрытие < 10°, поломка), `PropLamp` (зажигание огнивом + рассудок из `Player_Sanity`), `PropItem` (`HplGame.AddItem` — порт `cLuxInventory::AddItem`), `PropLevelDoor` (Locked, смена карты пока только в лог), `PropLever`, `PropWheel`, `PropMoveObject`, `PropGeneric`. StaticProp и простые модели — не пропы, но их тела всё равно физические.
- Ошибки оригинала сохраняются (например, `SetStuckState` шарнира ставит оба лимита в min; счётчик огнива).
- Звуки пропов как в оригинале: двери (закрыть/открыть, запереть/отпереть, «заперто», повреждение/поломка), лампа (огниво), подбор предмета (`ui/pick_<PickSound>`), дверь уровня (заперто), рычаг/колесо (пределы, «застрял»), разбивание, эффекты (`EffectsOn/OffSound`, свои звуковые сущности `.ent` гаснут/загораются с эффектами и активностью), статическое движение (`StaticMoveStart/Stop/LoopSound`). `HplEntity.PlaySound` = `iLuxEntity::PlaySound`, `PlaySoundAt` = звук в точке.
- `HplMap.DestroyEntity` — отложенное удаление в конце шага (объект сразу выключается: `Destroy` в Play ждёт конца кадра, а осколки не должны выталкиваться из целого горшка); `CreateEntity` — клон шаблона с переименованием префиксов детей.

## Звук (часть шага 6, Assets/Scripts/Sound, namespace RTXMnesia.Sound)

- Импорт: `SoundConverter` (меню **Tools > RTXMnesia > Import > Sounds**, batch `ImportMenu.ConvertSoundsBatch`, сам вызывается импортом уровня) копирует все `.ogg`/`.wav` из `sounds/`, `music/` и голоса папки языка (`lang/rus`) как есть в `Assets/AmnesiaImported/Sounds/<путь>` (~2150 файлов, ~320 МБ; повторно копируются только изменённые) и собирает `Sounds/sounds.asset` (`HplSoundBank`): клипы по имени и все 979 `.snt` (`cSoundEntityData`, булевы как `cString::ToBool`: не "true" = false; битые байты читаются как Latin-1, как TinyXML). Поиск по имени с выбором по папкам (`grunt/amb_alert01`), как `FileSearcher`. Импорт клипов — `SoundImportRules`: музыка и голоса Streaming, короткие DecompressOnLoad.
- `HplSoundHandler` (на корне сцены, шаг из `HplMap`) — порт `cSoundHandler`/`cSoundEntry`: громкость 3D считает сам (линейно от MinDistance до MaxDistance, блокировка лучом только телами с `BlocksSound` — двери; затухание 0.55 с / 0.2 с; quirk: `BlockVolumeMul` из `.snt` действует и без `Blockable`), Unity лишь панорамирует (своя плоская кривая rolloff, без доплера). Скорость (pitch) и громкость с затуханиями, глобальные громкость/скорость (`MultipleSettings` = `cMultipleSettingsHandler`, минимум запросов; скрипт — запись 0). Время проигрывания считается по длине клипа, а не по AudioSource — логика одинакова и в batch без звука. Лимита каналов нет (Unity сам).
- `HplSoundEntity` — порт `cSoundEntity`: старт при первом обновлении, Loop/Interval/Random, Start/Stop-звуки, выход за MaxDistance останавливает петлю и возвращает её внутри, `RemoveWhenOver`, следует за `Parent`. Звуковые сущности карты и `.ent` (`AmnesiaSound`) создаются при сборке карты/`CreateEntity`; `PlayGuiSoundData` = `cLuxHelpFuncs::PlayGuiSoundData`; quirk `PlaySoundEntityGui`: громкость .snt в квадрате.
- `HplMusicHandler` — `cMusicHandler` (главная песня, затухающие, позиции продолжения) + `cLuxMusicHandler` (приоритеты 0–10, после остановки/конца играет следующая зацикленная; громкость × `Sound/InGameMusicVolume`). Музыка врагов — шаг 6.
- Звуки физики (`Dynamics/HplSurfaceSounds`): удары (`Impact` материала по скорости сближения до шага, не более 6 за 0.4 с), скольжение (`Scrape`, высота по скорости, MinScrapeContacts, остановка через 8 шагов без скольжения), перекатывание (`Roll` по осям RollAxis), правило приоритетов материалов как в `ContactsProcessCallback`; контакты из `HplBodyContacts` (Enter/Stay) всех тел, пара тел — один раз; контакты PhysX с зазором > 2 мм (contact offset) не считаются касанием. Джоинты (`HplJoint.UpdateSound`): MoveSound (`CalcSoundFreq`), звуки пределов (громкость по скорости), звук разрыва. Свет: звуки мерцания.
- Проверено (`sounds` в `PlayerSimulation`): вступление `00_rainy_hall` — музыка `23_amb.ogg`, голоса и `fb_sfx_00_daniel`, глобальная скорость 0.7 и громкость 0; после пробуждения дождь, ветер, гром, поток, огонь; шаги `step_walk_rock`; удары и `break_pottery` горшка; сквозняк, испуг, скрип двери `joint_door_move` (громкость по скорости).

## Скрипты уровней (шаг 5, Assets/Scripts/ScriptApi, Effects, Ui, Levels)

**Порт уровня.** Класс `: HplScript` с атрибутом `[HplMapScript("maps/main/chXX/<карта>.map")]` в `Assets/Scripts/Levels/ChXX/`. `HplMap` находит его по пути карты, переимпорт не нужен. Журнал — `docs/ScriptPort/chXX/<карта>.md`, статусы функций — `docs/ScriptPort/api.md`.
- Функции движка — в `HplScript*.cs` с оригинальными именами и параметрами (порт `LuxScriptHandler.cpp`). Порт читается как `.hps`.
- Колбэки вызываются по имени, как в AngelScript. В порте имена писать через `nameof(...)`; динамические имена остаются строками.
- Преобразования AngelScript → C#:
  - `int x = RandFloat(...)` → `(int)RandFloat(...)` (отбрасывание);
  - `or`/`and` → `||`/`&&`;
  - `0.7` → `0.7f`.
- Глобальные переменные скрипта — поля класса. Локальные и глобальные переменные хранятся строками, как в HPL2 (`HplMath`: atoi/atof, `%.20f`).
- Части движка, которых ещё нет (частицы `.ps`, прикреплённые пропы, липкие зоны, мульти-слайдеры, кнопки, interact connections, анимации пропов, лестницы/фонарь 4.4, смена карты, враги/безумие/журнал/сохранения 6), — `NotPorted`: одно предупреждение `[Script] X: skipped, needs …` на функцию. Состояние (двери, лампы, предметы, квесты) при этом хранится, `Get*` возвращают заданное.
- Ошибки, которые пишет сам движок (`Error()` HPL2 — в `hpl.log`, игра продолжается; например, скрипт просит свет, которого нет в карте), — `HplLog.Error` → предупреждение `[Script] ERROR: …`. Ошибка Unity при включённом в консоли Error Pause ставит Play на паузу. Настоящие ошибки Unity (`HplLog.PortError`, исключения) — только для ошибок порта.

**`HplMap` (порт `cLuxMap` и порядка обновления).**
- Реестр сущностей скрипта, имена без учёта регистра:
  - области, кроме PlayerStart/PathNode/PosNode;
  - пропы и враги из `.ent` с Lux-типами; StaticProp не сущность.
- Тела `<сущность>_<тело>`, свет по имени, PlayerStart.
- Таймеры: `cLuxMap::UpdateTimers` — сначала все счётчики, потом вызовы; добавленные в колбэке не срабатывают в тот же шаг.
- Шаг 60 Гц:
  1. таймеры;
  2. логика активных сущностей: коллбэки столкновений и взгляда, затем `OnUpdate` пропов;
  3. игрок (`StepLogic`): движение, состояние, голова, наклон, взгляд, в конце коллбэки «Player», текст фокуса;
  4. HUD;
  5. эффекты;
  6. тело игрока (`StepPhysics`);
  7. физика: `HplPhysicsWorld.BeforeSimulate` → симуляция (контакты → звуки поверхностей) → `AfterSimulate` (разрыв джоинтов, их звуки, остановка скольжения, перекатывание);
  8. свет (`AmnesiaLight.UpdateLight`: `FadeTo`, мерцание и его звуки);
  9. звук (`HplSoundHandler.Step`: звуковые сущности мира, записи, музыка);
  10. `StepPostUpdate` игрока (силы удержания);
  11. удаление сущностей, помеченных на уничтожение (их звуковые сущности уничтожаются, прикреплённые звуки остаются на месте и доигрывают).
- Сборка: свет регистрируется и инициализируется до пропов; затем связи билбордов со светом, пропы (`PropFactory` + `Load`), тела остальных сущностей; шаблоны (`EntityTemplates`) пропускаются.
- `OnStart`/`OnEnter` — после постановки игрока.
- Флаги: `scriptDebug` (`ScriptDebugOn`), `logDebugMessages` (`AddDebugMessage` в консоль; в HPL2 видно только в отладке), `runScript`.

**Коллбэки столкновений** (`HplCollideCallbacks`, порт `iLuxCollideCallbackContainer`).
- Тест — `Physics.ComputePenetration` по текущим transform, без `Collider.bounds`: bounds обновляются только синхронизацией PhysX и отстают у тела, которое двигает код.
- Сохранена особенность HPL2: тело i владельца сравнивается с телом i другой сущности; области и персонажи отдают своё единственное тело для любого i.

**Эффекты** (`HplEffects`, порт `cLuxEffectHandler`) рисует `HplScreenEffectsPass`: HDRP custom pass AfterPostProcess, создаётся в рантайме, глобальные настройки HDRP не нужны. Шейдер `Assets/Shaders/HplScreenEffects.shader`.
- Порядок HPL2: ImageTrail → RadialBlur → Sepia (1D LUT `colorconv_sepia.tga`, по каналам) → затемнение (× (1 − alpha)) и вспышки (+).
- Расчёт в sRGB-значениях, как в гамма-рендере HPL2.
- Вес нового кадра следа: `exp(-amount / frameTime × 0.015)`, frameTime — реальное время между рендерами.
- Тряска экрана — `HeadPosAdd.ScreenShake`.
- Голоса (`PlayVoiceEffect`) — потоки GUI (или 3D в позиции) через обработчик звука; реплика идёт, пока её запись жива (время клипа считает обработчик), затем следующая и колбэк окончания.

**HUD** (`HplHud`, порты `cLuxMessageHandler` и `cLuxHintHandler`).
- Субтитры, `SetMessage`, подсказки (данные/заблокированные — в `HplGame`), иконка «запись добавлена», текст emotion flash.
- `ParseString` (порт `cLuxHelpFuncs`) — `$ButJournal` → клавиша из `default_user_keys.cfg`, название из категории `ButtonNames`.
- Виртуальный экран 800×600 по центру. Unity UI overlay со встроенным шрифтом, пока не сконвертированы шрифты игры.
- В снимки `RenderToFile` HUD не попадает.

**Общее состояние игры** — `HplGame`: глобальные переменные, квесты, подсказки, предметы по имени.

**Прогон** — `PlayerSimulation`: на уровне с `HplMap` шаги идут через карту, сообщения `[Script…]` копируются в лог.
- Флаги:
  - `-scriptDebug` — области вступления активны сразу, вступление пропускается;
  - `-noScript`;
  - `-seed N` — повторяемые `RandFloat`.
- Команды:
  - `watch t интервал` — шаги со строкой состояния скрипта: игрок, FOV/aspect/крен, затемнение/след/сепия, реплика, сообщения, таймеры;
  - `fx sepia|trail|fade|blur|flash|off значение`;
  - `goto x y z yaw`;
  - `start Зона`;
  - `interact`, `release`, `throw`, `scroll n`, `rotate on|off`, `mouse x y t [log]` (единицы мыши HPL за шаг, y вниз);
  - `aim Сущность [Тело]` — повернуть камеру на тело; `body Сущность` — состояние пропа/двери/лампы, тела, джоинты;
  - `call Функция аргументы` — вызвать функцию скрипта уровня.
  - `sounds` — играющие записи (имя, тип, расстояние, время), звуковые сущности, музыка, глобальные громкость и скорость.
  - `overlaps Сущность` — пересечения коллайдеров её тел между собой и с миром (глубина, направление, слой).
- Строка состояния включает состояние игрока, прицел и текст фокуса.
- Пример: вступление `00_rainy_hall` — `-seed 1 -script "watch 80 2; shot awake"` (~70 с до первого квеста).

## Соглашения

- Код на C#:
  - `UnityProj/UMnesiaRTX/Assets/Scripts/`:
    - `Hpl/` — конвертации HPL→Unity, конфиг, язык, физ. запросы, `HplMath`;
    - `World/` — компоненты уровня;
    - `Dynamics/` — физика тел и джоинтов;
    - `Props/` — пропы;
    - `Sound/` — звук и музыка;
    - `Player/` — игрок;
    - `ScriptApi/` — карта и API скриптов;
    - `Effects/` — эффекты экрана;
    - `Ui/` — HUD;
    - `Levels/ChXX/` — порты скриптов уровней;
    - `Dev/` — отладка;
  - `Assets/Shaders/` — наши шейдеры;
  - `Assets/Editor/` — конвертеры; редакторные проверки — `Assets/Editor/Dev`.
- Импортёры — Editor-скрипты, запускаемые также через `-executeMethod`.
- Числа из XML HPL парсить только с `CultureInfo.InvariantCulture` (`HplXml`): в русской локали разделитель — запятая.
- Не изменять ничего в папке установленной игры и в `src/`.
- Перед первым импортом ресурсов: убедиться, что `.gitignore` исключает импортированные папки (`git status` не должен показывать контент Amnesia).

## Синхронизация инструкций

- Поддерживать CLAUDE.md и AGENTS.md в корне репозитория побайтно одинаковыми. Любое изменение правил или описания проекта в одном файле сразу переносить в другой.

## Проверка импорта геометрии

- `HplMeshAudit.Run` (меню Tools > RTXMnesia > Import > Audit level meshes against MSH) сравнивает меши уровня с бинарными `.msh` v7 из `source/`, проверяет исходные пути моделей, матрицы всех статических объектов и подмешей сущностей, положения тел и соответствие mesh-коллизии визуальному мешу. Отчёт: `Logs/HplMeshAudit.txt`. Это независимая проверка; `.msh` не являются источником импорта.
- Порядок треугольников: сборщик отражает X и меняет COLLADA-тройку `(a,b,c)` на `(a,c,b)`. HPL2 `CreateVertexBuffer` сохраняет `(c,b,a)`: это тот же обход с циклически сдвинутой первой вершиной. При сверке `.msh` сравнивать ориентированные тройки с учётом циклического сдвига, а не индексы побайтно.
- Проверять не только успешный batch-импорт, но и контрольные рендеры, соответствие пути модели записи карты и проход игрока. Геометрически правильный меш может оказаться неверно выбранным одноимённым ресурсом.
- Проверка `00_rainy_hall` 2026-10-14: исправлены 123 подмены статических моделей. 124 модели / 274 подмеша совпадают с MSH по вершинам и ориентированным треугольникам; 925 экземпляров / 1413 матриц подмешей проверены; 471 проверка выбора одноимённых ресурсов прошла. Две разницы матриц исходных префабов относительно MSH (`candlestick_wall`, `armour_rusty_complete/podium`) остаются явно отмеченными в отчёте: в этой сцене их заменяют трансформы `.ent`, и итоговая расстановка совпадает. Прогон игрока по лестнице PlayerStartArea_x2 вниз и вверх успешен.
