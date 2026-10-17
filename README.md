# RTXMnesia

[Русский](#русский) · [English](#english)

## Русский

### О проекте

RTXMnesia — неофициальная переработка **Amnesia: The Dark Descent** на Unity с целью применить современные технологии **NVIDIA RTX и DLSS**, а также улучшить оригинальные текстуры при помощи нейронной обработки NVIDIA.

Сейчас проект использует Unity HDRP. Аппаратная трассировка лучей, DLSS и нейронная обработка текстур — цели следующих этапов, а не заявленные возможности текущей версии. Название проекта не означает участия или одобрения со стороны NVIDIA, Unity или Frictional Games.

**Состояние: незавершённая версия, около 80% оригинальной игры готово** по оценке автора на 17 октября 2026 года. Наличие порта скрипта карты не означает, что все её механики и полное прохождение проверены. Реализованы импорт ресурсов и уровней, основные механики игрока и взаимодействия, фонарь, физика, звук, враги, рассудок, переходы между картами, меню и сохранения. В проекте также есть порты сценариев Justine; завершённость дополнения отдельно не заявляется.

Проект создаётся для личного использования. Готовые сборки с ресурсами Amnesia не распространяются.

### Разработка и происхождение кода

По заявлению автора, разработка порта велась **исключительно при помощи Claude AI и Codex AI**:

- **Claude AI** — механики игры и игровые системы.
- **Codex AI** — импорт ресурсов, импорт и адаптация уровней игры.

Оригинальная игра и HPL2 созданы **Frictional Games**. Указание ИИ-инструментов описывает процесс разработки порта и не заменяет авторство оригинала или лицензионные обязательства.

Открытый [исходный код Amnesia и HPL2](https://github.com/FrictionalGames/AmnesiaTheDarkDescent) использовался как справочник поведения и форматов. Оригинальный C++ не компилируется и не включается в проект как нативная библиотека. Логика перенесена и адаптирована на C# под Unity:

- Системы `Lux*` и HPL2 служат основой для воспроизведения поведения игрока, объектов, врагов, звука и событий. Вместо исходного цикла движка используются компоненты Unity и C#-системы с фиксированным шагом логики 1/60 секунды.
- Newton заменён физикой Unity и собственными системами тел, шарниров и верёвок; OpenAL — звуковыми системами Unity; OpenGL deferred renderer — HDRP и собственными шейдерами/проходами для отдельных эффектов HPL2. Перенос включает адаптацию поведения, а не только замену синтаксиса.
- Сценарии уровней `.hps` перенесены вручную с AngelScript на C#. Встроенного интерпретатора AngelScript нет: таймеры, триггеры, коллбэки, переменные и команды выполняются через совместимый слой `HplScript` / `HplMap`. Сохраняются последовательности событий и ссылки на имена объектов/ресурсов.
- Редакторные конвертеры читают XML карт, сущностей и материалов, геометрию COLLADA, DDS/TGA и другие форматы. Они создают Unity-сцены, меши, материалы HDRP, коллайдеры, звуковые и прочие банки. Учитываются различия осей координат, трансформов, нормалей и параметров материалов; общие модели и материалы переиспользуются.
- Ход переноса сценариев, ссылки на исходные функции и известные отличия описаны в [журнале переноса](docs/ScriptPort/README.md) и [таблице API](docs/ScriptPort/api.md).

Переписывание на C# или использование ИИ само по себе не устраняет права на исходные произведения. В частности, права на `.hps` из установленной игры нельзя автоматически приравнивать к лицензии опубликованного C++: вопрос распространения их C#-портов остаётся отдельным.

### Что нужно для локальной сборки

- Windows, Git и PowerShell.
- **Unity 6000.6.2f1**, модуль Windows Build Support для выбранного backend сборки и совместимая с HDRP видеокарта. Проект настроен на DirectX 12 с DirectX 11 fallback.
- **HDRP 17.6.0** и остальные зависимости из `UnityProj/UMnesiaRTX/Packages/manifest.json`; Unity восстанавливает их при открытии проекта.
- Собственная законно полученная установленная копия **Amnesia: The Dark Descent**. Для Justine нужны также ресурсы дополнения из этой установки.

Исходники HPL2 в `src/AmnesiaTheDarkDescent/` для обычного импорта и сборки не требуются. Настройки рендера описаны в [HDRP-SETUP.md](UnityProj/UMnesiaRTX/HDRP-SETUP.md).

### Куда положить ресурсы игры

Импортёры ищут ресурсы **строго в папке `source/` в корне этого репозитория**, рядом с `README.md` и `UnityProj/`. Путь вычисляется относительно Unity-проекта, а не относительно диска установки игры. Вложенная папка `source/Amnesia The Dark Descent/` не подходит: файл должен находиться по пути `source/resources.cfg`.

Рекомендуемый способ — из корня репозитория выполнить:

```powershell
.\tools\Copy-AmnesiaResources.ps1 -GamePath 'E:\SteamLibrary\steamapps\common\Amnesia The Dark Descent'
```

Замените `-GamePath` своим путём к установленной игре. Скрипт только читает установку и копирует ресурсы в `source/`; повторный запуск обновляет новые и изменённые файлы. Параметр `-Destination` у скрипта есть, но импортёры продолжают искать стандартную `source/`: для обычной подготовки его менять не нужно.

При ручном копировании сохраните структуру каталогов:

```text
RTXMnesia/
├── LICENSE
├── README.md
├── source/                         # локальные ресурсы, вне Git
│   ├── resources.cfg
│   ├── materials.cfg
│   ├── ptest_materials.cfg          # материалы Justine
│   ├── maps/
│   │   ├── main/ch01/
│   │   ├── main/ch02/
│   │   ├── main/ch03/
│   │   └── ptest/                  # карты Justine
│   ├── static_objects/
│   ├── entities/
│   ├── textures/, graphics/, gui/, fonts/
│   ├── sounds/, music/, commentary/, lang/
│   ├── particles/, lights/, billboards/
│   ├── flashbacks/, main_menu/, models/, misc/
│   └── config/, core/, shaders/
├── tools/
└── UnityProj/UMnesiaRTX/            # открывать в Unity Hub
    ├── Assets/
    │   └── AmnesiaImported/         # результат импорта, вне Git
    ├── Packages/
    └── ProjectSettings/
```

Записи через запятую в схеме обозначают отдельные каталоги. Это те же папки, которые копирует скрипт. `resources.cfg` нужен для поиска ресурсов; `materials.cfg` и `ptest_materials.cfg` содержат описания физических поверхностей. Для Justine также сохраните `config/ptest_*`, `config/lang_ptest/` и `main_menu/ptest/`.

Не нужны `custom_stories/`, исполняемые файлы оригинала, редакторы, launcher и `_CommonRedist`. Кэш `.map_cache` пропускается: импортируются XML `.map`. Сохраняйте исходные `.hps` рядом с картами: импортёр использует их для поиска дополнительных сущностей, хотя исполняется C#-порт. Скрипт копирования переносит `lang/` целиком; конвертер локализации использует русский язык с английским запасным вариантом.

### Импорт и сборка в Unity

1. Откройте в Unity Hub папку **`UnityProj/UMnesiaRTX`**, дождитесь восстановления пакетов и компиляции редакторных скриптов.
2. При необходимости примените **Tools > RTXMnesia > Configure HDRP**.
3. Выполните **Tools > RTXMnesia > Import > Main menu and build scenes**. Этот пункт импортирует главное меню и первую карту `00_rainy_hall`, если её ещё нет, и обновляет список сцен сборки. Он не импортирует всю кампанию.
4. Для остальных игровых карт используйте **Tools > RTXMnesia > Import > Level: choose .map...**, выбирая файлы из `source/maps/main/ch01`, `ch02` и `ch03`. Импортируйте все требуемые карты: переход не сможет загрузить карту, для которой сцена ещё не создана. Конвертер сам добавляет импортированные игровые сцены в список сборки.
5. Выполните **Tools > RTXMnesia > Import > Player hands and hand objects**, чтобы создать ресурсы рук и фонаря. Импорт уровня создаёт необходимые конфиги, локализацию, интерфейс, банки звуков, частиц и другие связанные ресурсы; недостающие модели и материалы подготавливаются автоматически. Для отдельного обновления есть пункты **Sounds (sounds, music, voices)**, **Particles (.ps)** и другие меню импорта.
6. Для Justine импортируйте четыре карты из `source/maps/ptest/` и выполните **Tools > RTXMnesia > Import > Justine menu and build scenes**. Этот пункт готовит меню дополнения и список сцен, а не все четыре карты.
7. Проверьте ошибки в Console и `UnityProj/UMnesiaRTX/Logs/AmnesiaImport.log`. В **File > Build Profiles** выберите Windows и проверьте Scene List используемого профиля: первой сценой должна быть `Assets/AmnesiaImported/Levels/main_menu/main/menu_bg.unity`. Профиль с собственным списком сцен также должен содержать все нужные импортированные карты. `SampleScene` не является стартовой сценой игры.
8. Сохраните локальную сборку, например, в `UnityProj/UMnesiaRTX/Builds/Windows/`. Эта папка исключена из Git. Сборка без предшествующего импорта не восстановит ресурсы самостоятельно.

Для импорта одной карты из PowerShell при **закрытом редакторе этого проекта**:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe'
$projectPath = (Resolve-Path '.\UnityProj\UMnesiaRTX').Path
& $unityEditor -batchmode -nographics -quit -projectPath $projectPath `
    -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch `
    -amnesiaMap 'maps/main/ch01/00_rainy_hall.map'
```

Для остальных карт повторите команду с их путями относительно `source/`. Для меню доступны `RTXMnesia.Importer.MainMenuConverter.ConvertBatch` и `ConvertJustineBatch`, для рук — `RTXMnesia.Importer.HandsConverter.ConvertBatch`. Не запускайте второй Unity поверх проекта с `Temp/UnityLockfile`.

**Как ресурсы попадают в Player:** `source/` → редакторные конвертеры → `Assets/AmnesiaImported/` → ссылки сцен и Unity `Resources` → файлы Player. Готовая игра использует уже упакованные Unity-ресурсы. Она не читает установленную Amnesia или папку `source/` рядом с `.exe`; копирование оригинальных файлов к готовому `.exe` не заменяет импорт и пересборку.

### Лицензия, ресурсы и результат проверки

Полный неизменённый текст **GNU GPL version 3** находится в [LICENSE](LICENSE); он скопирован из официального репозитория исходников Amnesia. Код, на который автор порта вправе предоставлять лицензию, предоставляется по GPLv3. Исходные уведомления Frictional Games и права третьих лиц сохраняют силу; см. [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Проект предоставляется без гарантий в пределах условий лицензии.

GPL кода **не распространяет разрешение на ресурсы коммерческой игры**: модели, текстуры, звук, музыку, озвучку, тексты, шрифты, карты и сценарии из установленной игры. Это прямо разграничено в [объявлении Frictional Games](https://frictionalgames.com/2020-09-amnesia-is-now-open-source/). Конвертация и нейронное улучшение текстур сами по себе не дают разрешения на распространение этих материалов. Покупка игры также сама по себе не разрешает распространять её ресурсы.

**Проверка 17.10.2026:** в текущих отслеживаемых файлах и по путям/типам файлов в доступной истории Git не обнаружены исходные или импортированные медиа-ресурсы Amnesia. Однако **локальная готовая сборка содержит ресурсы Amnesia** — это подтверждают отчёт Unity и данные Player. `source/`, `Assets/AmnesiaImported/`, кэши и каталоги сборок исключены из Git, но `.gitignore` не управляет содержимым Player. C#-порты `.hps` присутствуют в репозитории и требуют отдельного выяснения прав. Полная методика, доказательства и ограничения — в [отчёте проверки](docs/Legal/CONTENT_AUDIT_2026-10-17.md).

Добавление GPL не подтверждает возможность распространения Unity-сборки: остаются вопросы совместимости GPL-производного кода с проприетарным Unity Runtime, лицензий ресурсов и перенесённых сценариев. [FAQ GNU о несовместимых библиотеках](https://www.gnu.org/licenses/gpl-faq.en.html#GPLIncompatibleLibs) объясняет, почему исключение для связывания требует разрешения соответствующих правообладателей; проект такого исключения от их имени не выдаёт. Это техническая фиксация состояния, а не юридическое заключение о готовности к публикации.

## English

### About the project

RTXMnesia is an unofficial reworking of **Amnesia: The Dark Descent** in Unity, intended to apply modern **NVIDIA RTX and DLSS** technologies and improve the original textures through NVIDIA neural processing.

The project currently uses Unity HDRP. Hardware ray tracing, DLSS and neural texture processing are goals for later development, not advertised features of this version. The project name does not imply involvement or endorsement by NVIDIA, Unity or Frictional Games.

**Status: unfinished; approximately 80% of the original game is ready**, according to the author's estimate as of October 17, 2026. A ported map script does not mean that every mechanic or a complete playthrough has been verified. Resource and level import, core player mechanics and interactions, the lantern, physics, audio, enemies, sanity, map transitions, menus and saves have been implemented. The project also includes Justine script ports; no separate completion claim is made for the expansion.

This is a personal project. Ready-to-play builds containing Amnesia resources are not distributed.

### Development and code provenance

According to the author, the port was developed **exclusively with the assistance of Claude AI and Codex AI**:

- **Claude AI** — game mechanics and gameplay systems.
- **Codex AI** — resource import, level import and adaptation.

The original game and HPL2 were created by **Frictional Games**. AI tool credits describe the port's development process; they do not replace the original authorship or licensing obligations.

The published [Amnesia and HPL2 source code](https://github.com/FrictionalGames/AmnesiaTheDarkDescent) was used as a reference for behavior and file formats. The original C++ is not compiled or included as a native library. Its logic has been ported and adapted to C# for Unity:

- `Lux*` and HPL2 systems inform the implementation of player, object, enemy, audio and event behavior. Unity components and C# systems replace the original engine loop, with a fixed logic timestep of 1/60 second.
- Unity physics and custom body, joint and rope systems replace Newton; Unity audio systems replace OpenAL; HDRP and custom shaders/render passes replace the OpenGL deferred renderer and selected HPL2 effects. This involves adapting behavior, not merely changing syntax.
- Level `.hps` scripts were manually ported from AngelScript to C#. There is no embedded AngelScript interpreter: timers, triggers, callbacks, variables and commands run through the `HplScript` / `HplMap` compatibility layer. Event sequences and object/resource identifiers are retained.
- Editor converters read map, entity and material XML, COLLADA geometry, DDS/TGA and other formats. They generate Unity scenes, meshes, HDRP materials, colliders, audio banks and other data. Coordinate axes, transforms, normal maps and material parameters are adapted; shared models and materials are reused.
- The [script port journal](docs/ScriptPort/README.md) and [API table](docs/ScriptPort/api.md) record porting work, original function references and known differences.

Rewriting code in C# or using AI does not by itself remove rights in the original works. In particular, `.hps` files from the installed game cannot automatically be assumed to share the published C++ source license: distribution rights for their C# ports remain a separate question.

### Local build requirements

- Windows, Git and PowerShell.
- **Unity 6000.6.2f1**, Windows Build Support for the chosen build backend, and an HDRP-compatible GPU. The project is configured for DirectX 12 with a DirectX 11 fallback.
- **HDRP 17.6.0** and the other dependencies in `UnityProj/UMnesiaRTX/Packages/manifest.json`, restored by Unity when the project opens.
- Your own lawfully obtained installed copy of **Amnesia: The Dark Descent**. Justine also requires the expansion's resources from that installation.

The HPL2 reference checkout at `src/AmnesiaTheDarkDescent/` is not required for normal import or building. See [HDRP-SETUP.md](UnityProj/UMnesiaRTX/HDRP-SETUP.md) for render configuration.

### Where to put the game resources

Importers look **specifically in `source/` at the repository root**, alongside `README.md` and `UnityProj/`. This path is calculated relative to the Unity project, not the game's installation drive. Do not add another `Amnesia The Dark Descent/` directory inside `source/`: the root configuration must be at `source/resources.cfg`.

From the repository root, the recommended preparation command is:

```powershell
.\tools\Copy-AmnesiaResources.ps1 -GamePath 'E:\SteamLibrary\steamapps\common\Amnesia The Dark Descent'
```

Replace `-GamePath` with your installation path. The script only reads the installation and copies resources into `source/`; rerunning it updates new or changed files. The script has a `-Destination` parameter, but importers still expect the standard `source/` location, so leave it unchanged for normal setup.

For manual copying, preserve this layout:

```text
RTXMnesia/
├── LICENSE
├── README.md
├── source/                         # local resources, excluded from Git
│   ├── resources.cfg
│   ├── materials.cfg
│   ├── ptest_materials.cfg          # Justine surfaces
│   ├── maps/
│   │   ├── main/ch01/
│   │   ├── main/ch02/
│   │   ├── main/ch03/
│   │   └── ptest/                  # Justine maps
│   ├── static_objects/
│   ├── entities/
│   ├── textures/, graphics/, gui/, fonts/
│   ├── sounds/, music/, commentary/, lang/
│   ├── particles/, lights/, billboards/
│   ├── flashbacks/, main_menu/, models/, misc/
│   └── config/, core/, shaders/
├── tools/
└── UnityProj/UMnesiaRTX/            # open this folder in Unity Hub
    ├── Assets/
    │   └── AmnesiaImported/         # generated assets, excluded from Git
    ├── Packages/
    └── ProjectSettings/
```

Comma-separated names in the diagram represent separate directories. These are the same directories copied by the script. `resources.cfg` controls resource lookup; `materials.cfg` and `ptest_materials.cfg` describe physical surfaces. For Justine, retain `config/ptest_*`, `config/lang_ptest/` and `main_menu/ptest/` as well.

You do not need `custom_stories/`, the original executables, editors, launcher or `_CommonRedist`. The script skips `.map_cache`: import uses XML `.map` files. Keep the original `.hps` files alongside the maps: the importer scans them for additional entity references, although gameplay executes the C# ports. The copy script copies all of `lang/`; localization conversion uses Russian with an English fallback.

### Importing and building in Unity

1. Open **`UnityProj/UMnesiaRTX`** in Unity Hub and wait for package restoration and editor script compilation.
2. Apply **Tools > RTXMnesia > Configure HDRP** if needed.
3. Run **Tools > RTXMnesia > Import > Main menu and build scenes**. It imports the main menu and the first map, `00_rainy_hall`, if that map is missing, and updates the build scene list. It does not import the whole campaign.
4. Import other gameplay maps with **Tools > RTXMnesia > Import > Level: choose .map...**, selecting files from `source/maps/main/ch01`, `ch02` and `ch03`. Import every required map: transitions cannot load a map whose scene has not been generated. The converter adds imported gameplay scenes to the build scene list automatically.
5. Run **Tools > RTXMnesia > Import > Player hands and hand objects** to generate hand and lantern resources. Level import generates the required configuration, localization, UI, audio/particle banks and other associated resources; missing models and materials are imported automatically. **Sounds (sounds, music, voices)**, **Particles (.ps)** and other import menu entries support separate updates.
6. For Justine, import the four maps in `source/maps/ptest/`, then run **Tools > RTXMnesia > Import > Justine menu and build scenes**. That command prepares the expansion's menu and scene list, not all four maps.
7. Check the Console and `UnityProj/UMnesiaRTX/Logs/AmnesiaImport.log` for errors. In **File > Build Profiles**, select Windows and check the active profile's Scene List: `Assets/AmnesiaImported/Levels/main_menu/main/menu_bg.unity` must be first. A profile with its own scene list must also include all required imported maps. `SampleScene` is not the game's startup scene.
8. Save the local build, for example, under `UnityProj/UMnesiaRTX/Builds/Windows/`, which is excluded from Git. Building before resource import will not restore resources automatically.

To import a single map through PowerShell, with **this project's Unity editor closed**:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe'
$projectPath = (Resolve-Path '.\UnityProj\UMnesiaRTX').Path
& $unityEditor -batchmode -nographics -quit -projectPath $projectPath `
    -executeMethod RTXMnesia.Importer.ImportMenu.ConvertLevelBatch `
    -amnesiaMap 'maps/main/ch01/00_rainy_hall.map'
```

Repeat with other map paths relative to `source/`. Menu entry points are `RTXMnesia.Importer.MainMenuConverter.ConvertBatch` and `ConvertJustineBatch`; hands use `RTXMnesia.Importer.HandsConverter.ConvertBatch`. Do not launch another Unity instance against a project with `Temp/UnityLockfile`.

**How content reaches the Player:** `source/` → editor converters → `Assets/AmnesiaImported/` → scene references and Unity `Resources` → Player data files. The executable uses assets already packed by Unity. It does not load an installed Amnesia copy or a `source/` directory next to the `.exe`; copying original files beside an existing executable does not replace importing and rebuilding.

### License, game resources and audit result

The complete, unmodified **GNU GPL version 3** text is included in [LICENSE](LICENSE), copied from the official Amnesia source repository. Code for which the port's author can grant a license is provided under GPLv3. Original Frictional Games notices and third-party rights remain in effect; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). The project is provided without warranty under the license terms.

The code's GPL **does not grant permission to redistribute commercial game resources**: models, textures, audio, music, voice recordings, text, fonts, maps or scripts from the installed game. [Frictional Games' announcement](https://frictionalgames.com/2020-09-amnesia-is-now-open-source/) explicitly distinguishes the source release from ownership of game content. Conversion or neural texture enhancement does not by itself grant redistribution rights. Purchasing the game does not by itself grant permission to redistribute its resources either.

**Audit on October 17, 2026:** no original or imported Amnesia media resources were found among current tracked files or through file-path/type inspection of available Git history. However, **the existing local Player build contains Amnesia resources**, confirmed by the Unity build report and Player data. `source/`, `Assets/AmnesiaImported/`, caches and build directories are excluded from Git, but `.gitignore` does not control Player contents. The repository contains C# ports of `.hps` scripts whose rights require separate clarification. See the [audit report](docs/Legal/CONTENT_AUDIT_2026-10-17.md) for methods, evidence and limitations.

Adding GPL does not establish that the Unity build can be distributed: compatibility between GPL-derived code and the proprietary Unity Runtime, game resource licenses, and ported script rights remain unresolved. The [GNU FAQ on incompatible libraries](https://www.gnu.org/licenses/gpl-faq.en.html#GPLIncompatibleLibs) explains why a linking exception requires permission from the relevant copyright holders; this project grants no such exception on their behalf. This is a technical record, not a legal opinion establishing publication readiness.
