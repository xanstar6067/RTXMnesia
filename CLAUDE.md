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

HDRP-база (`Assets/Editor/ConfigureHDRP.cs`, меню Tools > RTXMnesia > Configure HDRP, `Assets/Settings/*`, `SampleScene`), импортёры ресурсов и уровней (`Assets/Editor/Importer`), компоненты уровня и игрок (`Assets/Scripts`). Первый уровень: `Assets/AmnesiaImported/Levels/maps/main/ch01/00_rainy_hall.unity` (генерируется, не в git), в нём игрок — Play и ходить.
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
- План этапов: 1) подготовка ✓ → 2) импортёры (текстуры, материалы, модели ✓; префабы .ent → вместе с шагом 4) → 3) импорт одного уровня ✓ (`00_rainy_hall`; частицы .ps — заглушки) → 4) игрок: 4.1 передвижение ✓, 4.2 физика сущностей (Rigidbody, джоинты, физ. материалы), 4.3 взаимодействие (фокус/прицел, Grab, двери, ящики, рычаги, колёса), 4.4 лестницы, фонарь → 5) скрипты уровней → 6) враги, безумие, звук, меню, сохранения → 7) RTX → 8) ИИ-апскейл текстур.

## Импортёры (Assets/Editor/Importer, namespace RTXMnesia.Importer)

Меню: **Tools > RTXMnesia > Import**. Лог: `UnityProj/UMnesiaRTX/Logs/AmnesiaImport.log`.
Batch-методы `RTXMnesia.Importer.ImportMenu.*` (Unity с этим проектом должен быть закрыт; проверять lock `Temp/UnityLockfile`):
`ConvertMaterialsBatch`, `ConvertModelsBatch` с `[-amnesiaFolder <папка в source>] [-forceTextures] [-describe a.dae,b.dae]`;
`RenderMaterialsBatch`/`RenderModelsBatch` — сцена-витрина + рендер в `Logs/<Kind>Preview.png` (без `-nographics`, нужен GPU).
Быстрая проверка компиляции без Unity: Roslyn `Editor/Data/DotNetSdk/.../csc.dll` + ссылки на `Managed/UnityEngine/*Module.dll` и HDRP dll из `Library/ScriptAssemblies` (фасады UnityEngine.dll/UnityEditor.dll не подключать).
Полный импорт `static_objects` + `entities`: ~3 мин, ~1 GB в `Assets/AmnesiaImported`.

- `ResourceIndex` — поиск ресурса по имени файла, как в HPL2 (пути внутри .mat/.dae вида `G:/amnesia/redist/...` игнорируются; приоритет: та же папка, затем порядок `resources.cfg`). Дубликаты имён есть (27 .dds, 11 .mat, 52 .dae).
- `DdsDecoder` — DXT1/3/5, ATI2(BC5), несжатые 24/32 бит, кубкарты. Форматы в игре: DXT1 1131, DXT5 714, BGRA32 36, ATI2 12, кубкарт 4.
- `TextureConverter` — DDS → PNG в `Assets/AmnesiaImported/Textures/<путь>__<роль>.png`; роли: albedo, normal, mask, specular, height, emissive. Настройки импорта — `TextureImportRules` по роли из имени.
- `MaterialConverter` — `.mat` → HDRP материал в `Assets/AmnesiaImported/Materials/<путь>.mat`; существующие обновляются на месте (GUID сохраняется). Константы тюнинга вверху файла.
- `ImageLoader` — .dds/.tga/.jpg/.png → пиксели «верхняя строка первой» (HPL2 грузит всё через DevIL с IL_ORIGIN_UPPER_LEFT). Текстура без расширения → перебор форматов, как `TextureManager::FindTexture2D`.
- `TextureConverter.Version` — при изменении математики конвертации увеличить: PNG старше момента смены версии перегенерируются.
- `ModelConverter` + `ModelImportRules` — `.dae` копируется в `Assets/AmnesiaImported/Models/<путь>.dae` и импортируется **встроенным Collada-импортом Unity**; поверх: ремап материалов (`AddRemap` по id/name/symbol материала Collada), узлы `_collider_<box|sphere|capsule|cylinder|mesh>_*` → невидимые коллайдеры (cylinder → convex MeshCollider), `_charcollider_*` — то же, но только для персонажей (пока отличаются лишь именем узла), подмеши с материалом без текстуры удаляются (HPL2 их не рисует). `.msh` не нужны. При изменении правил — увеличить `GetVersion()`.
- Статик без `_collider` узлов: в HPL2 коллизия строится из визуального меша, если у StaticObject `Collides="true"` — делать при расстановке карты (шаг 3).

Проверенные по исходникам HPL2 конвенции:
- UV COLLADA: движок делает `v = 1 - v`, DDS грузится верхней строкой в v=0 → в Unity UV не трогаем, PNG сохраняем как DDS (верх = верх).
- Нормали: бинормаль движка смотрит «вниз по картинке» → зелёный канал инвертируем.
- Specular-карта: R = интенсивность, G = степень `exp2(G*10)+1`. В HDRP: Specular Color workflow, F0 = R × 0.08, smoothness = 1 − sqrt(sqrt(2/(n+2))).
- Height — в альфа-канале. Юнит `Alpha` = alpha test по альфе диффуза. `Illumination` = emissive (у ламп регулируется в игре).
- Имя материала у меша = имя текстуры из `<library_images>` `.dae` с расширением `.mat`.
- Height-карта Amnesia — это **глубина** (белое = глубже, relief mapping) → инвертируется для HDRP. Проверено численно: градиент инвертированной карты совпадает с normal map по знаку в обоих каналах (заодно подтверждает инверсию G).
- **Оси**: Unity-импорт Collada отражает X: HPL `(x,y,z)` → Unity `(-x,y,z)` (проверено на `corner_concave_round.dae`). Позиции из `.map`/`.ent` переводятся так же, повороты (Euler XYZ, радианы, M = Rz·Ry·Rx) — отражением: угол X сохраняется, Y и Z меняют знак; порядок вершин треугольников не меняется. Всё это — `Assets/Scripts/Hpl/HplSpace.cs`; проверено на уровне (стены стыкуются, двери/люстры на местах). Свет и камеры HPL смотрят в локальный −Z (Unity +Z) → `HplSpace.LookRotation`. UV из .map (плоскости, декали, билборды): v = 1 − v.
- **Z_UP Collada** (157 файлов, в основном сущности): Unity оставляет вершины узла в осях файла (X отражён) и добавляет Rx(−90°) сверху (на корень или на узлы верхнего уровня); HPL2 переписывает вершины как (−x, z, y), трансляции/масштаб узлов как (x, z, y), а rotate-элементы оставляет как есть. Поправка для узла: Unity-вершина (−x,y,z) → (x,z,y) (`LevelConverter.ZUpNodeCorrection`). Для сущностей узел целиком задаётся трансформом подмеша из `.ent`, для статики — пересобирается из трансляции узла.
- TODO: блендинг Mul/MulX2 (нет в HDRP, 14 материалов: бутылки, стёкла, лужи), вода, DissolveAlpha, кубкарты отражений (заменяются HDRP/RT). ~10 моделей с материалом без .mat (кости, паук) — вероятно, материал задаётся в `.ent` (`SubMesh Material=`).
- `.ent` (сущности): `ModelData` = Mesh + SubMesh (материал-override), Shapes, Bodies (масса, физ. материал, какие подмеши привязаны), Joints (двери — JointHinge), Bones, Animations; `UserDefinedVariables EntityType="SwingDoor|..."` + звуки/поведение. Префабы из .ent делать на шаге 4 вместе с механиками.
  Важно: SubMesh в `.ent` хранит трансформ подмеша относительно сущности, он **заменяет** трансформ узла Collada (масштаб узла остаётся). Пример: дверь `castle` развёрнута там на 180°. Внутри `.ent` есть свои свет/билборды/частицы/звуки (`ModelData/Entities`), их позиции × масштаб экземпляра.

## Импорт уровня (LevelConverter, шаг 3)

Меню: **Tools > RTXMnesia > Import > Level: 00_rainy_hall (ch01)** / **Level: choose .map...**. Batch: `ConvertLevelBatch`, `RenderLevelBatch [-amnesiaMap maps/main/ch01/X.map] [-convert] [-views "x,y,z,yaw,pitch;..."]` (рендер с камеры, всех PlayerStart и доп. точек → `Logs/Level_<map>_NN_*.png`, без `-nographics`), `DescribeModelsBatch -describe a.dae,b.dae`. Перед уровнем недостающие модели/материалы/cookie импортируются сами. 00_rainy_hall: ~20 с.
Результат: `Assets/AmnesiaImported/Levels/<путь карты>.unity` + `<карта>/<карта>_generated.asset` (меши плоскостей/декалей/квад билбордов/цилиндр коллизии, варианты материалов по цвету) + `<карта>_volume.asset`.

- StaticObject → экземпляр префаба модели; коллизия по правилу HPL2: если `Collides` и в модели нет узлов `_collider*` — MeshCollider на визуальные меши. Plane → порт `MeshCreator::CreatePlane`. Decal → готовый мировой меш из .map + HDRP/Decal материал (цвет → вариант материала, без вертекс-цвета).
- Entity → GameObject с `AmnesiaEntity` (файл, тип, переменные .ent + экземпляра); модель распакована, подмеши по `.ent`; `Body_*` с `AmnesiaBody` и коллайдерами из Shapes (пока всё статично, Rigidbody/джоинты — шаг 4). Цилиндр → выпуклый MeshCollider. `CommentaryIcon` выключены.
- Area → `AmnesiaArea` (+ триггер BoxCollider размера Scale, кроме PlayerStart). ParticleSystem/Sound → заглушки `AmnesiaParticleSystem`/`AmnesiaSound` (TODO конвертер .ps и звук). BoxLight, FogArea, точечные gobo (кубкарты), скайбокс — пока пропускаются с предупреждением.
- Свет → HDRP через `AmnesiaLight` (хранит параметры HPL, включая flicker — пока не симулируется). Пересчёт яркости — `Assets/Scripts/Hpl/HplLighting.cs`: фиксированная экспозиция EV100 = 3; HPL-свет почти не затухает в радиусе, HDRP — 1/d², совпадение на 0.5 радиуса (`ReferenceDistance`); `Candela = π·d²·1.2·2^EV`. Spot с gobo → Pyramid + cookie (роль текстуры `Cookie`, перевёрнута по вертикали, как проецирует HPL).
- Эмиссия Lit и цвет тумана в HDRP экспонируются → × `UnitRadiance` (1.2·2^EV); **цвет HDRP/Unlit не экспонируется** (ShaderPassForwardUnlit: `_DeExposureMultiplier` = 1) → цвет HPL как есть. При смене EV — перегнать материалы и уровень.
- Билборды: FixedAxis — обычный MeshRenderer; Point/Axis рисует скрытый несохраняемый дочерний объект `AmnesiaBillboard` ([ExecuteAlways], поворот к камере в `beginCameraRendering`, работает и в Scene view). TODO: ореолы (IsHalo) в HPL рисуются без depth test с затуханием по occlusion query — пока с обычным depth test; связь билборда со светом (ConnectLight).
- Окружение: Volume — без неба (HPL без скайбокса не даёт ambient), фикс. экспозиция, без тонмаппинга, туман HDRP подобран так, что на FogEnd непрозрачность = alpha цвета тумана. Игрок (`AmnesiaPlayer` + камера, фон чёрный) на PlayerStartArea_1; `config/game.cfg` → `Assets/AmnesiaImported/Config/game.asset` (`HplConfig`) при каждом импорте уровня.
- PlayerStart: игрок смотрит вдоль **+Z** зоны (`cLuxAreaNodeLoader_PlayerStart`: forward = R·(0,0,1)), без разворота на 180°.

## Игрок (шаг 4, Assets/Scripts/Player, namespace RTXMnesia.Player)

Порт по исходникам, числа — только из `game.cfg` через `HplConfig` (`GetFloat("Player_Body","Mass")` как `cConfigFile`), в коде их нет.
- `HplCharacterBody` — порт `iCharacterBody` (CharacterBody.cpp): не Rigidbody, а вертикальный цилиндр, который двигается скоростями по направлениям (ускорение/торможение/разворот), гравитацией и силами, и выталкивается из мира после каждого шага; ступени — лучами (AccurateClimbing: 3 луча), камера на высоте стоячего тела + `CameraPosAdd`, сглаживание по `CameraSmoothPosNum` шагам. Формы — выпуклые MeshCollider-цилиндры на слое 2 (Ignore Raycast: их не видят запросы по умолчанию); неактивный размер (присед/стоя) — trigger. Не портировано: прилипание к движущимся телам (GravityAttachment), соединение с телом (Push).
- `HplPhysics.CheckShapeWorldCollision` — аналог `iPhysicsWorld::CheckShapeWorldCollision`: OverlapBox + `Physics.ComputePenetration`, вектор выталкивания = покомпонентный максимум по контактам; с одним коллайдером до 4 итераций (PhysX даёт одно разделение на пару, Newton — список контактов). Тела с `CollideCharacter=false` и триггеры игнорируются.
- `PlayerMoveNormal` — `cLuxMoveState_Normal`: ходьба/бег/присед/прыжок (сила прыжка + ослабление гравитации 0.22 с), покачивание головы и шаги на минимуме, отскок головы при приземлении, урон от падения. `PlayerLean` — `cLuxPlayerLean` (до 0.5 м и 15°, голова-цилиндр останавливается о стены).
- `AmnesiaPlayer` — `cLuxPlayer` + `cLuxInputHandler`: логика в FixedUpdate (60 Гц), камера интерполируется между шагами, мышь — каждый кадр: пиксели × чувствительность / (1.7 × высота экрана) × 2 рад, сглаживание как `GetSmoothMousePos` (веса 0.7^возраст в шагах 60 Гц — не зависит от FPS), питч ±70°. Смещения головы `HeadPosAdd` (Main/Bob/Lean/…) как `MoveHeadPosAdd`. Клавиши: WASD, Shift, Space, Ctrl (переключает присед), Q/E наклон, F2 свободная камера (`FlyCamera`), Esc отпускает мышь. Звуки (шаги, прыжок) — пока события/заглушки до шага 6.
- Проверка без Play: `-executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch [-amnesiaMap ...] [-script "start PlayerStartArea_x2; turn 90; w 6; run 4; crouch; jump; leanr 1; shot name"]` → состояние в `Logs/AmnesiaImport.log`, снимки `Logs/Player_*.png` (без `-nographics`). Проверено на 00_rainy_hall: шаг 2.5 м/с, бег 4.25, лестница PlayerStartArea_x2 вниз/вверх, камера 1.75 / 1.15 (присед), прыжок ~1 м. PlayerStartArea_x1 — отладочный старт снаружи уровня (оттуда можно уйти в пустоту, как и в оригинале).

## Соглашения

- Код пишем на C# в `UnityProj/UMnesiaRTX/Assets/Scripts/` (наш код: `Hpl/` — конвертации HPL→Unity, конфиг, физ. запросы, `World/` — компоненты уровня, `Player/` — игрок, `Dev/` — отладка; редакторные проверки — `Assets/Editor/Dev`) и `Assets/Editor/` (конвертеры). Импортёры — Editor-скрипты, запускаемые также через `-executeMethod`.
- Числа из XML HPL парсить только с `CultureInfo.InvariantCulture` (`HplXml`): в русской локали разделитель — запятая.
- Не изменять ничего в папке установленной игры и в `src/`.
- Перед первым импортом ресурсов: убедиться, что `.gitignore` исключает импортированные папки (`git status` не должен показывать контент Amnesia).
