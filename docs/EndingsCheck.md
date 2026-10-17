# Проверка концовок (29_orb_chamber) — задание для сквозного прохода

Результаты прогона 2026-10-17: [EndingsCheckResults.md](EndingsCheckResults.md), включая незакрытую визуальную проверку титров Агриппы и уточнения исходного поведения.

Цель: убедиться, что все три концовки основной игры доходят от игрового действия до титров и возврата в главное меню без ошибок порта. Исправлять найденное можно только с отчётом; особенности оригинала, перечисленные в журнале карты, **не исправлять**.

Правила проекта (AGENTS.md): ресурсы Amnesia не коммитить; `src/` и папку игры не трогать; Unity для batch должен быть закрыт (`UnityProj/UMnesiaRTX/Temp/UnityLockfile`); перед batch-прогоном посмотреть `git diff UnityProj/UMnesiaRTX/ProjectSettings/` — если пусто, после прогона вернуть `git checkout UnityProj/UMnesiaRTX/ProjectSettings/`, иначе сделать копию папки и восстановить из неё; коммитит пользователь.

## Где что

- Оригинал: `source/maps/main/ch03/29_orb_chamber.hps` (только справка, код не копировать в документы).
- Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/OrbChamber.cs`; журнал: `docs/ScriptPort/ch03/29_orb_chamber.md`.
- Титры: `Assets/Scripts/Ui/HplCredits.cs` (`StartCredits(music, loop, cat, entry, endNum)`), код концовки: endNum 0 → `tyr299`, 1 → `odn314`, 2 → `lke271`.
- Сцена: `Assets/AmnesiaImported/Levels/maps/main/ch03/29_orb_chamber.unity` (генерируется импортом, вне git).
- Лог batch-прогонов: `UnityProj/UMnesiaRTX/Logs/AmnesiaImport.log` (строки `[Script…]` копируются туда).

## Инструменты

1. **Логика без Play** — `PlayerSimulation` (`-batchmode -quit -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch03/29_orb_chamber.map -seed 1 [-scriptDebug] -script "..."`, без `-nographics`, если нужны `shot`). Команды: `watch t интервал` (таймеры, реплики, сообщения), `call Функция арг…` (функция скрипта уровня или API `HplScript`), `goto x y z yaw`, `aim Сущность`, `interact`, `release`, `throw`, `mouse x y t`, `body Сущность`, `sounds`, `health`, `shot имя`. `-scriptDebug` выдаёт фонарь и голову Агриппы (`OnStart`, как в оригинале).
2. **Титры в Play** — `RTXMnesia.EditorDev.CreditsPreview.RunBatch` (без `-quit`/`-nographics`), параметр `-endNum 0|1|2` (по умолчанию 2, Даниэль). Снимки: `Logs/CreditsCheck/<endNum>/`. Проверяются также код, очистка `AddKeyPart` и новая игра после возврата в меню.
3. **Ручная проверка** — открыть сцену 29 в редакторе и нажать Play (карта стартует сразу). Чтобы получить голову Агриппы, на `HplMap` включить `scriptDebug`.

## Общий ход карты

`OnStart`: через 4 с Александр начинает говорить; обратный отсчёт портала — таймер `PortalCountDowne` на 60 × 0.4 × `PortalCountDownMul` с (1 → 24 с), состояния `PortalCountDownState`. Первая реплика «портал открывается» (`PlayPortalDoneVoice`, `AlexanderVoiceState` = 2) → через 5 с `SetPortalPhase(1)` → `PortalState` = 2 (портал полностью открыт). Вторая реплика → концовка Александра. `OnEnter` делает `AutoSave`.

Ожидаемые «ошибки движка» (`[Script] ERROR`, не провал): отсутствующий `monolith01_2`, остановка ещё не созданных `mono_sound1/2`, несовпадающие имена таймеров в `ResetPortalCountDown` / `EndingSetup` — всё описано в журнале. **Провал**: исключения C#, `HplLog.PortError` (настоящие ошибки Unity), зависание цепочки (таймер не срабатывает, реплика не заканчивается), отсутствие `StartCredits`.

## Концовка 1 — Александр (endNum 0, `ending_alexander.ogg`, код `tyr299`)

Действие игрока: ничего не делать, не трогать колонны и портал.
Проверить по порядку:
- реплики Александра, обратный отсчёт, две реплики `CH03L29_Alexander_PortalOpens_1/2`;
- `StartAlexanderEnding`: `AlexanderEnding` = 1, `UnlockAchievement("Sacrifice")` в логе;
- `TimerAlexEnding` проходит все состояния: `alexander_1` — анимация `Release` (`PlayPropAnimation`), музыка `29_event_end.ogg`, вспышка, взгляд на Александра, Александр выключен, `SetPortalPhase(2)`, урон рассудку (`TimerEndSanityDamage`), голос `Remorse` / `AlexEndWords`;
- в конце `StartCredits("ending_alexander.ogg", false, "Ending", "MainCredits", 0)`.
Прогон: `-script "watch 240 5"` (при необходимости дольше).

## Концовка 2 — Даниэль (endNum 2, `ending_daniel.ogg`, код `lke271`)

Действие игрока: повалить три колонны `orb_room_rod_1..3`, пока Александр не дошёл до реплики «портал открывается» (`AlexanderVoiceState` ≤ 1).
- Физически хотя бы один раз: подойти, `aim orb_room_rod_N`, `interact` + движение мыши / толчок, пока колонна не войдёт в `AreaRodTippingPoint_N` и не упадёт в `AreaRodHitGround_N`. Проверить, что колонна реально падает (дополнительный толчок `TimerExtraRodPush` через 0.5 с), предмет в руках отпускается (`ChangePlayerStateToNormal`), гаснут частицы и свет луча.
- Логика для остальных (если физику не довести): `call CollideRodTippingPoint orb_room_rod_N AreaRodTippingPoint_N 1`.
Каждое падение → `InterruptAlexander`: музыка `26_event_brute.ogg`, `InterruptAlexanderCount` + 1, отсчёт сбрасывается, `PortalCountDownMul` 0.3, затем 0.25, реплика прерывания. После третьей реплики → `StartDanielEnding` → `TimerDanielEnding` (слизь у игрока и Александра, дверь, шаги Даниэля, затемнение со сценой глобальной громкости) → `StartCredits("ending_daniel.ogg", …, 2)`.

## Концовка 3 — Агриппа (endNum 1, `ending_agrippa.ogg`, код `odn314`)

Нужна голова: в обычной игре `agrippa_head_1` берётся на `26_torture_nave_redux` (событие Агриппы), для проверки — `-scriptDebug`.
- Применить голову раньше времени: использование на `orb_altar_1` или `AreaPortalInteractArea` при `PortalState` 0 → сообщение `Ch03Level29/HeadOnNotOpenPortal`, при 1 → `HeadOnStartPortal`; голова остаётся.
- Дождаться `PortalState` = 2 (≈ 5 с после первой реплики «портал открывается»), но до второй реплики (`AlexanderEnding` = 0), и применить голову: в Play — из инвентаря на алтарь; в симуляции — `call UseHeadOnPortal agrippa_head_1 orb_altar_1`.
- Ожидается: предмет убран, `StartAgrippaEnding` → `TimerAgrippaEnding` (голова `elheado` появляется у области `AreaHead_N`, в которой игрок стоял последним (`WhatHeadArea`), и растворяется в портале; голоса Агриппы `CH03L29_Ending_Agrippa_1..3` по областям `CollideAgrippaEnding1..3`, злой Александр, `SetPortalPhase(2)`) → `StartCredits("ending_agrippa.ogg", …, 1)`. `SetPropActiveAndFade` здесь включает/выключает сразу — затухание модели не перенесено (известно).

## Титры (все три, в Play)

- `HplCredits.Active`, карта стоит (`timeScale` 0, игрок и меню паузы заблокированы, HUD скрыт), играет своя песня концовки;
- текст `Ending/MainCredits` прокручивается, заголовки (строки с `*`) шрифтом меню;
- у конца песня гаснет, затемнение, «Конец» (`General/TheEnd`) и правильный код концовки (в Justine кода нет);
- Esc / пробел / Tab / ЛКМ / ПКМ пропускают прокрутку не раньше 3 с; «Конец» можно пропустить сразу — особенность оригинала, не ошибка;
- после выхода — главное меню, игра сброшена (новая игра начинается с `00_rainy_hall`, части ключа `AddKeyPart` очищены).

## Дополнительно

- Загрузка: сохраниться/загрузить автосохранение 29 и пройти любую концовку после загрузки.
- Смерть у огня (`AreaFireDamage_1..3`) и от портала (`InteractActivePort`, урон 10) не ломают цепочки.
- Не проверено ранее: направление головы Александра к игроку (`SetNPCFollowPlayer`) только расчётом — посмотреть глазами.

## Отчёт

По каждой концовке: PASS / FAIL, команды прогона, ключевые строки лога (`[Script]` с `StartCredits`, состояния таймеров), снимки, если делались. Найденные расхождения с оригиналом — с ссылкой `файл:строка` на `.hps` и порт; журнал карты обновить (без кода оригинала).
