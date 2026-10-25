# 00_rainy_hall

- **Оригинал:** `source/maps/main/ch01/00_rainy_hall.hps`.
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/RainyHall.cs` (класс `RainyHall`, `[HplMapScript("maps/main/ch01/00_rainy_hall.map")]`).
- **Статус:** логика перенесена полностью и проверена прогоном (см. «Проверка»). Двери, лампы и силы работают с шагами 4.2–4.3, звук и музыка — с портом звука; частиц пока нет.

## Функции

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `PlayEffectVoice` (:10) | `PlayEffectVoice` | готов | |
| глобальные `gfIntroSequence*` (:32) | поля `gfIntroSequenceGlobalVolume`, `gfIntroSequenceFadeOutTime` | готов | глобальные переменные скрипта не сохраняются (как в HPL2) |
| `TimerStartGame` (:37) | `TimerStartGame` | готов | музыка — шаг 6 |
| `ResumeIntroSequence` (:44) | `ResumeIntroSequence` | готов | |
| `TimerIntroSequence` (:51) | `TimerIntroSequence` | готов | состояния 0–7 один в один |
| `TimerActiveFix` (:180) | `TimerActiveFix` | готов | |
| `IntroSequenceVoiceOver` (:187) | `IntroSequenceVoiceOver` | готов | см. «Особенности оригинала» |
| `TimerWakeUpPlayer` (:194) | `TimerWakeUpPlayer` | готов | |
| `TimerSFXIntro`, `TimerVoiceIntro` (:242–254) | — | не переносятся | закомментированы в оригинале |
| `TimerBeginSoundsIntro` (:256) | `TimerBeginSoundsIntro` | готов | |
| `TimerBlackOut` (:267) | `TimerBlackOut` | готов | шаги 1–18 |
| `TimerRose` (:388) | `TimerRose` | готов | частицы — после конвертера `.ps` |
| `TimerThunder` (:403) | `TimerThunder` | готов | |
| `ThunderLights` (:440) | `ThunderLights` | готов | `or` → `\|\|` |
| `CollideAreaThunder` (:469) | `CollideAreaThunder` | готов | |
| `CollideNarrowHalls` (:495) | `CollideNarrowHalls` | готов | |
| глобальные `fStep`, `fLoop` (:533) | поля `fStep`, `fLoop` | готов | |
| `HallwaySteps` (:535) | `HallwaySteps` | готов | `fStep += 0.03f` в аргументе, как в оригинале |
| глобальная `bSwing` (:552) | поле `bSwing` | готов | |
| `HallwayEvents` (:553) | `HallwayEvents` | готов | `(int)RandFloat(1, 4)`; люстры раскачиваются силами `AddBodyForce` |
| `CollideAreaNarrowPassOut` (:603) | `CollideAreaNarrowPassOut` | готов | |
| `NarrowPassOutEvents` (:629) | `NarrowPassOutEvents` | готов | |
| `NarrowPassOutEvents02` (:716) | `NarrowPassOutEvents02` | готов | |
| `TimerPlayerInactive` (:803) | `TimerPlayerInactive` | готов | нигде не вызывается (как в оригинале) |
| `CollideAreaGalleryDoor` (:818) | `CollideAreaGalleryDoor` | готов | дверь медленно приоткрывается |
| `TimerSwingDoor` (:831) | `TimerSwingDoor` | готов | 10 толчков `AddPropForce` по 0.03 с |
| `TimerStopCreak` (:847) | `TimerStopCreak` | готов | `SetLampLit` гасит люстру и подсвечник за 1 с |
| `CollideAreaGallery` (:895) | `CollideAreaGallery` | готов | |
| `CollideAreaGustDoor` (:942) | `CollideAreaGustDoor` | готов | |
| `TimerGustDoor` (:957) | `TimerGustDoor` | готов | |
| `CollideAreaGust` (:985) | `CollideAreaGust` | готов | `(int)RandFloat(2, 3)` |
| `CollideAreaFaint` (:1010) | `CollideAreaFaint` | готов | |
| `TimerFaintEvents` (:1023) | `TimerFaintEvents` | готов | имя таймера = имя переменной = имя функции |
| глобальная `bRoll` (:1110) | поле `bRoll` | готов | |
| `TimerRandomLook` (:1111) | `TimerRandomLook` | готов | `(int)RandFloat(1, 5)` |
| `StopRandomLook` (:1145) | `StopRandomLook` | готов | |
| `InteractLargeGate`, `TimerLargeGate`, `InteractLargeGate02` (:1163–1199) | те же имена | готов | мёртвый код: сущности `LargeGate` в карте нет |
| `LookAtQuest` (:1210) | `LookAtQuest` | готов | нигде не регистрируется (как в оригинале) |
| `CollideAreaQuest` (:1214) | `CollideAreaQuest` | готов | |
| `OnStart` (:1222) | `RainyHall.OnStart` | готов | |
| `OnEnter` (:1304) | `RainyHall.OnEnter` | готов | предзагрузка — пустые функции |
| `OnLeave` (:1318) | `RainyHall.OnLeave` | готов | экран загрузки — шаг 6 |

## Отличия от оригинала

Синтаксис AngelScript переведён на C#:
- `int x = RandFloat(...)` → `(int)RandFloat(...)` — отбрасывание дробной части, как в AngelScript;
- `or` → `||`;
- литералы `0.7` → `0.7f`;
- имена колбэков записаны через `nameof(...)` (компилятор проверяет, что функция существует);
- динамическое имя в `TimerFaintEvents` осталось строкой;
- числа в отладочных сообщениях форматируются с точкой независимо от локали.

Части движка, которых пока нет, пропускаются с одним предупреждением в логе на функцию:
- частицы — нужен конвертер `.ps`;
- наборы безумия — шаг 6.

Двери, лампы, предметы и силы работают через пропы шагов 4.2–4.3 (`Assets/Scripts/Props`, `Dynamics`).

Звук, музыка и голоса идут через `Assets/Scripts/Sound` (порт `cSoundHandler`, `cSoundEntity`, музыки HPL2 и Lux).

## Особенности оригинала (сохранены)

- **Вторая цепочка вступления.** `IntroSequenceVoiceOver` ставит ещё один таймер `IntroSequence`, когда заканчиваются четыре голоса (~36 с). Возникает вторая цепочка `TimerIntroSequence` с состояниями 8, 9, …, которые ничего не делают. В отладочном выводе каждую секунду идёт `Event:N Time:1`.
- **Несуществующие источники света.** `ThunderLights` перебирает `spotthunder_0`, `pointthunder_0`, `ambthunder_0`, которых в карте нет. HPL2 пишет ошибку «Could not find light» в `hpl.log` и продолжает; порт пишет её один раз на имя предупреждением `[Script] ERROR: …` (не ошибкой Unity, чтобы Error Pause не останавливал игру).
- **Пропуск вспышек почти никогда не срабатывает.** Условие в `ThunderLights` всегда истинно, а `RandFloat(1, 3) == 1` выполняется с вероятностью ~1/32768.
- **Сквозняк влияет на коридор.** `TimerStopCreak("breath3")` заново запускает `TimerRandomLook`. Если войти в узкий коридор раньше чем через 8 с после сквозняка, голова снова начнёт «плыть».

## Известные проблемы

- Не воспроизводятся частицы — лепестки розы, пыль, сквозняки.
- Удар по рассудку не даёт эффекта приближения (шаг 6), значение рассудка при этом уменьшается.

## Проверка

Прогон без Play mode (`PlayerSimulation`, см. CLAUDE.md), снимки в `Logs/Player_00_rainy_hall_*.png`.

**Вступление** (`-seed 1`, сценарий `watch 5.5 1; shot …; watch 8 2; …`, ~85 с):
- затемнения и телепорты по `IntroStart_1..4`;
- субтитры четырёх реплик (`CH01L00_DanielsMind01..04_01`);
- пробуждение на `PlayerStartArea_1`: крен 75°, голова опущена, взгляд на `Arealook1`;
- шаги `TimerBlackOut` 1–18;
- в конце запускается `TimerRandomLook`.

**Области** (`-scriptDebug`):
- `AreaFaint` → `CollideAreaFaint` и `TimerFaintEvents`;
- `areagustdoor` → `CollideAreaGustDoor`, `TimerGustDoor`, 10 вызовов `TimerSwingDoor`;
- вход в `areanarrowhalls` → `HallwaySteps`, `HallwayEvents`, медленное растяжение aspect до 1.7.

Ещё не проверены прогоном: `narrowpassout`, галерея, гром на `areabeginthunder`/`areaendthunder`.

**Пропы (шаги 4.2–4.3)**, вызовы функций уровня командой `call`:
- `CollideAreaGalleryDoor` → `door_gallery` за 2 с приоткрывается до ~35° (10 толчков силой 70);
- `CollideAreaGustDoor` → `door_gust` распахивается до 90°;
- `TimerStopCreak("lightsout")` → `chandelier_nice_1` и `candlestick_floor_2` гаснут за 1 с (свет и эмиссия до 0).

**Исправления по Play-тесту (2026-10-15)**:
- белые прямоугольники на полу и стенах — декали `dirt_floor01` (~290 шт.), `moist_wall01`, лужа `pool_water` со смешиванием Mul; теперь шейдер `HplMultiply`, проверено рендерами `RenderLevelBatch -pick`;
- осколки `pot_plant_small02_1`: горшок, поднятый и отпущенный над столом, разбивается, все 7 осколков остаются на столе и полу (`body`/`overlaps`). Причины были в спекулятивном CCD (провал тонкого осколка сквозь пол, разгон удерживаемого предмета, вдавленного в пол) и в том, что в Play целый горшок жил до конца кадра;
- ковры: `carpets_carpet03_1` (1.7 см) и `carpets_carpet01_1` (2.5 см) проходятся вперёд и назад без прыжка, лестница `PlayerStartArea_x2` вниз и вверх — без изменений.

**Звук (2026-10-15)**, команда `sounds`:
- вступление: музыка `23_amb.ogg`, голоса `CH01L00_DanielsMind01..04` и звук воспоминания `fb_sfx_00_daniel`, глобальная скорость звука 0.7, громкость мира 0;
- после пробуждения: дождь (`general_rain_m`), ветер, `ambience_hollow_tinker`, гром, поток `12_epoxy_flow`, огонь костра (звук из `.ent`);
- шаги `step_walk_rock`, удары и `break_pottery` при разбивании горшка;
- `CollideAreaGustDoor`: вихрь у двери, испуг, дыхание и вздох игрока, скрип петель `joint_door_move` (громкость по скорости).

## Доведение до оригинала: частицы, ореолы, контур, шрифты (2026-10-15)

- Частицы `.ps` (`ParticleConverter`, `Scripts/Particles`): 92 системы на уровне (туман, пыль, свечи, струя и брызги воды, капли); `CreateParticleSystemAtEntity` / `DestroyParticleSystem` из скрипта (лепестки розы, пыль коридора, сквозняк), частицы разбивания, ударов, мерцания света, эффекты пропов.
- Ореолы ламп (172, `HplHalo.shader`): без теста глубины, гаснут по видимой доле источника; проверено: за стеной свечи не просвечивают.
- Контур предмета в фокусе и мерцание предметов рядом (`HplEffectRenderer`): проверено на `tinderbox_3`.
- Шрифты игры в HUD (`game_default`, `game_hints`, русские): субтитры вступления переносятся как в оригинале.
- Лестниц и фонаря на уровне нет. Дверь уровня ведёт в `01_old_archives`: переход между картами — вместе со следующей картой.

## Плавность качания камеры (2026-10-25)

- Медленный крен `TimerRandomLook` стоя обновлялся скачками: `Quaternion.Angle` округляет малые повороты до нуля. При ходьбе изменение позиции обходило эту проверку и заодно обновляло поворот.
- `AmnesiaPlayer` сравнивает компоненты запрошенного поворота точно и интерполирует крен между шагами логики. Положение сохраняет фильтр микроколебаний тела; неподвижный поворот кэшируется без повторных записей в Transform, чтобы не сбрасывать накопление path tracing. Телепорт, восстановление сохранения и `SnapCamera` сбрасывают интерполяцию крена.
- `RTXMnesia.EditorDev.CameraSwayTests.RunBatch`: 245 проверок в редакторе без звука и сборки плеера — равномерное качание 0.5°/с при 60 шагах логики / 120 кадрах, одинаковый крен при ходьбе и стоя, мгновенные установки и сброс интерполяции, отсутствие обновлений неподвижной камеры, восстановление после внешнего поворота.
