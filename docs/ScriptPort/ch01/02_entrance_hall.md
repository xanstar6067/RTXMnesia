# 02_entrance_hall

- **Оригинал:** `source/maps/main/ch01/02_entrance_hall.hps`.
- **Порт:** `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/EntranceHall.cs` (класс `EntranceHall`, `[HplMapScript("maps/main/ch01/02_entrance_hall.map")]`).
- **Статус:** логика перенесена полностью. Карта — хаб главы 1: на неё возвращаются из 03/04/05, состояние восстанавливает `HplSavedMaps` (порт `cLuxSavedGameMap`). События стража при возвращении зависят от глобальных переменных других карт (ещё не портированы); проверены прогоном с заданными переменными (см. «Проверка»).

## Функции

| Оригинал (строка) | Порт (C#) | Статус | Примечание |
|---|---|---|---|
| `CollideEnterLargeRoom` (:5) | `CollideEnterLargeRoom` | готов | птицы, первый вихрь через 7 с |
| `TimerDustWhirl` (:17) | `TimerDustWhirl` | готов | `(int)RandFloat(1, 4)`: зоны `Whirl01..03` |
| `FuncUnlockDoor` (:40) | `FuncUnlockDoor` | готов | нигде не вызывается; двери `castle_arched01_1` в карте нет |
| глобальная `aSlime` (:54) | поле `aSlime` | готов | |
| `TimerGuardSlime` (:57) | `TimerGuardSlime` | готов | `fade1` / `fade2` / `lantern` и `guard1..3`; см. «Особенности» про цикл |
| `TimerActivateSlimeEntity` (:168) | `TimerActivateSlimeEntity` | готов | |
| `GuardianEffects` (:176) | `GuardianEffects` | готов | `7.0f-(iSound/2)` — целочисленное деление сохранено |
| `TimerSlimeSounds` (:208) | `TimerSlimeSounds` | готов | |
| `PlayerReactions` (:219), `ReactionTimer` (:228) | те же имена | готов | |
| `PlayerInteractDoor` (:241) | `PlayerInteractDoor` | готов | коллбэк двери `level_wood_2` в карте |
| `UseKeyOnDoor` (:246) | `UseKeyOnDoor` | готов | |
| `CollideGiveQuestsWeb` (:263) | `CollideGiveQuestsWeb` | готов | |
| `UseAcidOnWeb` (:268) | `UseAcidOnWeb` | готов | |
| `TimerMusicDelay` (:283), `TimerFadeAcidLight` (:287) | те же имена | готов | |
| `UseEmptyContainerOnWeb` (:292), `UseChemicalOnWeb` (:297) | те же имена | готов | |
| `CollideAreaFlashBack` (:310) | `CollideAreaFlashBack` | готов | |
| `TimerCreateTrail` (:317) | `TimerCreateTrail` | готов | |
| глобальная `bSwing` (:344), `MakeTrailEffect` (:345) | поле `bSwing`, `MakeTrailEffect` | готов | |
| `InteractSlime` (:362), `TimerCellarSlime` (:369) | те же имена | готов | коллбэк `web_1` в карте |
| `InteractLockedDoor` (:373), `TimerLockedDoor4evah` (:379) | те же имена | готов | двери `castle_arched01_3`, `_5` |
| `TimerRunHint` (:384) | `TimerRunHint` | готов | |
| `InteractLargeGate` (:392), `TimerLargeGate` (:403) | те же имена | готов | зона `LargeGate` |
| `InteractLargeGate02` (:428), `TimerSlimeGateOn` (:434) | те же имена | готов | |
| `CollideDirectionHint` (:444), `TimerScream` (:467) | те же имена | готов | глобальная `BeenToLab` |
| `OnStart` (:485) | `EntranceHall.OnStart` | готов | `ClearSavedMaps` в конце |
| `OnEnter` (:545) | `EntranceHall.OnEnter` | готов | `AutoSave` пишет автосохранение (с 2026-10-17) |
| `OnLeave` (:595) | `EntranceHall.OnLeave` | готов | |

## Отличия от оригинала

- Синтаксис AngelScript переведён на C# по правилам из README (`(int)RandFloat`, литералы `f`, `nameof`, `string[]` → `new[] { … }`).
- Строка + float в отладочных сообщениях: AngelScript печатает 6 значащих цифр, порт — `G6` в инвариантной культуре.
- `AutoSave` с 2026-10-17 пишет автосохранение: сохранения перенесены (CLAUDE.md «Сохранения»); строки `skipped` ниже — из прогонов до этого.
- `SetPropActiveAndFade` включает слизь сразу, без затухания модели.
- `GiveSanityDamage` / `GiveSanityBoost*` меняют рассудок, но эффектов безумия ещё нет: шаг 6 (этап C).

## Особенности оригинала (сохранены)

- **Цикл слизи `iSlime <= aSlime.length()`.** Последний индекс выходит за массив. Исключение AngelScript обрывает функцию; HPL2 результат `Execute` не проверяет и ничего не пишет. После цикла кода нет, поэтому порт идёт до `< Length` с тем же результатом.
- **`SetEntityPlayerInteractCallback("castle_1", …)`.** Сущности `castle_1` в карте нет, HPL2 пишет ошибку в лог. Дверь в винный погреб — `level_wood_2`, её коллбэк `PlayerInteractDoor` задан в самой карте.
- **`BoxLight_2`.** Окружающий свет всей карты (цвет 0.15, 0.17, 0.2). События стража затемняют его `FadeLightTo`. Box-свет перенесён (`AmnesiaLight.box`, `HplBoxLightPass`).
- **Повторные таймеры с одним именем.** `TimerSlimeSounds` добавляет `iRand + 1` таймеров с именем сущности: в HPL2 они независимы.

## Проверка

- `PlayerSimulation` (`-amnesiaMap maps/main/ch01/02_entrance_hall.map -seed 1`): старт без ошибок, подсказка бега через 3 с; с `-globals "key_study_1=key_study_1"` — событие стража 1 через 0.87 с: тряска, след 0.8, замедление 0.6 → 0.8 → 1, рассудок −10, ~190 таймеров звуков слизи, затухание `PointLightx1_*` и `BoxLight_2`, след гаснет к 9 с.
- `MapChangePreview` (Play): дверь уровня `01_old_archives` → `02_entrance_hall` (стартовая точка `PlayerStartArea_4`, `ClearSavedMaps` в OnStart забывает 01, музыка `02_amb_safe`, box-свет), возвращение с `key_study_1`: только OnEnter, `ScaryMusic` = 1, таймер `guard1`, затем `BoxLight_2` гаснет, след, урон рассудку, слизь видна.
