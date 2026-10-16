# 01_old_archives

- **Оригинал:** `source/maps/main/ch01/01_old_archives.hps`.
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/OldArchives.cs` (класс `OldArchives`, `[HplMapScript("maps/main/ch01/01_old_archives.map")]`).
- **Статус:** логика перенесена полностью, события проверены прогоном (см. «Проверка»). Карта загружается дверью уровня `00_rainy_hall` (переход между картами, экран загрузки). Фонарь подбирается и работает с моделью в руке.

## Функции

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `CollideOpeningDustDoor` (:5) | `CollideOpeningDustDoor` | готов | `1.0 / 2.5f` → `1.0f / 2.5f` |
| `TimerOpenDoor01` (:32) | `TimerOpenDoor01` | готов | `opendoor_1` / `lightsout` / `stopeffect` по имени таймера |
| `TimerSwingDoor` (:65) | `TimerSwingDoor` | готов | 10 толчков, общий счётчик для `Door_1` и `Door_3` |
| `CollideBeginSwirl` (:89) | `CollideBeginSwirl` | готов | закомментированные строки оригинала не переносились |
| `TimerSwirlHorror` (:108) | `TimerSwirlHorror` | готов | шаги 1–10; тараканы `cockroach_*` — `SetPropActiveAndFade` (см. «Отличия») |
| `TimerBreath` (:211) | `TimerBreath` | готов | |
| `CollideBeginGhost` (:224) | `CollideBeginGhost` | готов | |
| глобальная `iLoopGhost` (:234) | поле `iLoopGhost` | готов | нигде не используется (как в оригинале) |
| `TimerGhost` (:235) | `TimerGhost` | готов | шаги 1–10, бросок книг и фонаря `AddPropForce`, взгляд на фонарь |
| глобальная `bRoll` (:333) | поле `bRoll` | готов | |
| `TimerRandomLook` (:334), `TimerRandomLook02` (:368) | те же имена | готов | `(int)RandFloat(1, 5)`, `or` → `\|\|` |
| `StopRandomLook` (:402) | `StopRandomLook` | готов | |
| `PickLanterAndOil` (:416) | `PickLanterAndOil` | готов | `CallbackFunc` фонаря в карте; масло 25 |
| `DoSimpleDoorOpening` (:440), `DoDoorOpening` (:452) | те же имена | готов | |
| `TimerDelayEffect` (:466) | `TimerDelayEffect` | готов | |
| `CollideCompleteTrail` (:483), `TimerStopLook` (:499) | те же имена | готов | `cockroache_1..3` — опечатка имён (см. «Особенности») |
| `TimerWallScrape` (:509), `TimerBeginWind` (:517) | те же имена | готов | `(int)RandFloat(1, 6)` |
| `CollideLastWind` (:527) | `CollideLastWind` | готов | |
| `PickNoteToSelf` (:541) | `PickNoteToSelf` | готов | `CallbackFunc` записки в карте |
| `TimerHint` (:547) | `TimerHint` | готов | |
| `CreateDust` (:552), `TimerMusic` (:569) | те же имена | готов | коллбэк соединения `secret_lever` → `secret_door` |
| `CollideHintLean` (:579) | `CollideHintLean` | готов | |
| `CollideTremble` (:584) | `CollideTremble` | готов | дождь выключается, люстры раскачиваются |
| `TimerStopRumble` (:610) | `TimerStopRumble` | готов | см. «Особенности оригинала» |
| `CollideAreaCollideSeenDoor` (:637) | `CollideAreaCollideSeenDoor` | готов | |
| `InteractLockedDoor` (:643), `TimerDoorMessageOnAgain` (:652) | те же имена | готов | `PlayerInteractCallback` двери `Door_3` в карте |
| `OnStart` (:659) | `OldArchives.OnStart` | готов | |
| `OnEnter` (:723) | `OldArchives.OnEnter` | готов | `ClearSavedMaps` очищает посещённые карты; `AutoSave` пишет автосохранение (с 2026-10-17) |
| `OnLeave` (:746) | `OldArchives.OnLeave` | готов | пустая |

## Отличия от оригинала

- Синтаксис AngelScript переведён на C# по правилам из README (`(int)RandFloat`, `||`, литералы `f`, `nameof`).
- `AutoSave` с 2026-10-17 пишет автосохранение: сохранения перенесены (CLAUDE.md «Сохранения»); строки `skipped` ниже — из прогонов до этого.
- `SetPropActiveAndFade` включает и выключает тараканов (`CritterBug`) сразу, без затухания модели и без поведения жуков: жуки (`cLuxCritter_Bug`) — шаг 6.
- `01_idle.snt` у `AreaGrunt` — только звук; врагов на этой карте нет.

## Особенности оригинала (сохранены)

- **`TimerStopRumble`.** Оригинал объявляет `int i = i == 150 ? -150 : 150;`: переменная читает собственное неинициализированное значение (0), поэтому `i` всегда 150. Люстры каждый раз толкаются в одну сторону, звенит цепь `chandelier_simple_short_4`, в отладке `I is: 150`. C# не компилирует чтение неинициализированной переменной: порт явно ставит 0.
- **Несуществующая сущность.** `OnStart` запирает и отключает `shelf_secret_door_joint_1`, которой в карте нет. HPL2 пишет ошибку в лог, порт — два предупреждения `[Script] Entity ... does not exist!`.
- **`StopSound("SoundScratch_" + i)`.** Звуков `SoundScratch_1` и `SoundScratch_4` скрипт не создаёт: ошибка «Could not find sound entity» в логе.
- **`cockroache_1..3`.** `CollideCompleteTrail` включает сущности с опечаткой в имени; если их нет в карте, ничего не происходит (как в оригинале).

## Проверка

- `PlayerSimulation` (`-amnesiaMap maps/main/ch01/01_old_archives.map -seed 1`, зоны `AreaTriggerDoor_1`, `AreaBeginSwirl`, `AreaTriggerGhost`, `AreaCollideTremble`, `AreaTriggerTrail`, вызов `PickLanterAndOil`):
  - пыльная дверь распахивается 10 толчками, факелы гаснут, рассудок −10;
  - вихрь: шаги, тараканы, музыка `01_event_critters.ogg`, затемнение и замедление звука, возврат `01_amb_darkness.ogg`;
  - призрак: книги и фонарь летят, взгляд на фонарь, скрежет в стенах;
  - тряска: люстры, пыль, `I is: 150`;
  - задание `00Trail` выполнено, музыка `15_puzzle_hole.ogg`, взгляд на записку.
- `MapChangePreview` (Play): дверь уровня `00_rainy_hall` → экран загрузки → `01_old_archives` с переносом игрока, фонаря, эффектов и музыки.
- Box-свет `BoxLight_2` (окружающий свет верхнего яруса, 0.225 0.275 0.25) импортируется с этапа B; до этого ярус был темнее оригинала.
- Найдено попутно: рычаг `secret_lever` стоял внутри `_charcollider` стены и непрерывно бился о пределы. Исправлено в импортёре: в HPL2 `_charcollider` сталкивается только с персонажем (`SetCollide(false)`).
