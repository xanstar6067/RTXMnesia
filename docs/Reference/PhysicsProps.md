# Физика сущностей, пропы и взаимодействие

> Вынесено из CLAUDE.md (2026-10-25) без изменений текста; перекрёстные ссылки «см. «Раздел»» ищи по заголовкам в `docs/Reference/`. Индекс — в CLAUDE.md.

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

### Цепочки на шаровых шарнирах (отступление, 2026-11-22)

Висящие верёвки `rope_beam02`, цепи `chain_beam` (линейное затухание), мост `barrier_bridge`, люстра `chandelier_large` в `.ent` имеют затухание 0, то есть минимум HPL2 0.001 (≈ 0.0012 1/с в Unity). В HPL2 их успокаивал сам Newton: игра ставит среднюю точность (`WorldLoaderHplMap.cpp:273`, `SetAccuracyLevel(Medium)` → `NewtonSetSolverModel(2)`, итеративный решатель в два прохода), и он теряет энергию цепочки. PhysX её почти не теряет: тройной маятник `rope_beam02_1` в `21_torture_nave` через 18 с после загрузки качался со скоростью 0.58 м/с, цепи `chain_beam_6/7` — тоже, и каждый кадр перерисовывали кэш теней всех свечей в радиусе (`docs/RtPerformance.md`).

Правило (`HplPhysicsBody.BallChainSettleDamping` = 0.5 1/с, `SettleOnBallJoint` из `HplJoint.CreateJoint`): у тела на шаровом шарнире затухание, которое в `.ent` на минимуме (≤ 0.001), не ниже 0.5 1/с: амплитуда падает примерно в e раз за 2 с. Затухание, заданное в `.ent` (туши 0.3–0.5, кандалы 1, люстры 0.03, висящий заключённый 0.1), не меняется; петли и ползунки (дверцы, ящики) не затронуты. Толкнуть верёвку по-прежнему можно, она успокаивается за несколько секунд. Проверка: проба освещения, флаг `sleep` (`docs/EnhancedLighting/README.md`): в `21_torture_nave` через 18 с все верёвки и цепи спят (до правки качались), `PhysicsTests` и `RopeStickySlimeTests` без изменений.

### Тела на джоинтах, стоящие в соседе (отступление, 2026-11-27)

Дверцы столов `work_desk_door.ent` (SwingDoor, отдельная от стола `work_desk.ent` сущность) не открывались обычной тягой: в `04_wine_cellar_lab` — `work_desk_door_1` (там сюжетный предмет), в `03_archives` — `_1`, `_3`, `_4`. Причина — не липкий предел (`HplJoint.UpdateLimits` совпадает с `PhysicsJointHingeNewton.cpp:236-246`, и с выключенным sticky дверца тоже стояла). Причина — авторская геометрия. Нижний край панели `Shape_13` стоит в нижней полке стола `Shape_12` на ~1 мм по высоте и ~7 мм по глубине. Newton такое малое проникновение не выталкивал. PhysX выталкивает дверцу вверх, шарнир её держит, нормальная сила растёт, и трение запирает поворот. Игрок просил −13.5 рад/с, а угол стоял на 0.3–0.7° (`PlayerSimulation` по `save.sav` из отчётов F1, `mouse 0 ±0.01`). `work_desk_door_2` в `03_archives` стоит на 9 мм дальше от стола и открывался. Ящики того же стола входят в его боковину на 4 мм, слайдер их держит.

Правило (`HplPhysicsWorld.IgnoreRestOverlaps`, вызов в конце `HplMap.Build`, то есть в позе карты до применения сохранения):
- у динамического ребёнка петли или ползунка коллайдеры не сталкиваются с нединамическими коллайдерами (статика карты, тела массы 0, в том числе своей сущности), в которые они уже входят не глубже `MaxRestOverlap` = 5 мм (`Physics.ComputePenetration`);
- персонаж, триггеры и слой `NoCollideLayer` не учитываются, шаровые шарниры не затронуты;
- пределы джоинта держат тело как раньше;
- `HplPhysicsBody.Active` при включении восстанавливает исключённые пары: выключенный коллайдер забывает `IgnoreCollision`.

После правки все четыре дверцы при той же тяге открываются до 84–110° за 0.7 с.

Что ещё попадает под правило (обзор `restoverlaps`, 2026-11-27):
- нет пар: `01_old_archives`, `02_entrance_hall`, `09_back_hall`, а также двери `mansion` в `03_archives`; их закрытие и защёлкивание не меняются;
- есть пары: дверцы и ящики столов (`03_archives` 27, `04_wine_cellar_lab` 7, `10_daniels_room` 12, `11_study` 7), `scewer_place` в `15_prison_north`, висячий замок в `25_cell_tunnels`;
- `ptest/01_cells`: двери камер `prison_section` с рамой проёма, матрас с кроватью, фонарь с блоком, замок. Двери камер открываются, как и без правила.

`PhysicsTests` дают те же результаты. Исключённые пары видны командой симуляции `restoverlaps`, а `nocollide A B [коллайдер]` и `sticky Сущность on|off` помогают проверять похожие случаи.

## Пропы и взаимодействие (шаг 4.3, Assets/Scripts/Props, namespace RTXMnesia.Props)

- `HplProp` (ScriptApi, порт `iLuxProp` + `iLuxPropLoader::AfterLoad`): тела, джоинты, здоровье/прочность, эффекты (свет `FadeTo`, билборды и эмиссия `_EmissiveColor` через собственные runtime-материалы `HplEmission`), статическое движение, `SetDisableCollisionUntilOutsidePlayer`, связанные пропы. Переменные `.ent` — `Var*`, экземпляра карты — `InstanceVar*`, числа конфига — `ConfigFloat`.
- `PropFactory.Create` по `EntityType`: `PropObject` (Grab/Push/Slide/Static, разбивание по энергии удара или здоровью → break-шаблон, ContainedItem), `PropSwingDoor` (Closed — лимит 2°, Locked, автозакрытие < 10°, поломка), `PropLamp` (зажигание огнивом + рассудок из `Player_Sanity`), `PropItem` (`HplGame.AddItem` — порт `cLuxInventory::AddItem`), `PropLevelDoor` (Locked; иначе `ChangeMap` со звуками двери `EnterSound` / `ExitSound`), `PropLever`, `PropWheel`, `PropMoveObject`, `PropGeneric`. StaticProp и простые модели — не пропы, но их тела всё равно физические.
- Ошибки оригинала сохраняются (например, `SetStuckState` шарнира ставит оба лимита в min; счётчик огнива).
- Факелы и свечи (2026-10-19): `AmnesiaLight.SetColor/SetRadius` не меняют настроенные значения включённой фазы мерцания (как HPL2); `Restore` задаёт их из сохранения. Иначе зажжённый факел, исходно потушенный в карте, мерцал до чёрного. `HplEmission` выключает эмиссию самого материала лампы, без сохранения светящегося материала под MaterialPropertyBlock, сохраняет состояние при переключении текстур и освобождает копии; общие ассеты не меняются. Без плавного перехода внешний вид обновляется сразу. Диагностика и команды проверок — `docs/LampRendering.md`.
- Звуки пропов как в оригинале: двери (закрыть/открыть, запереть/отпереть, «заперто», повреждение/поломка), лампа (огниво), подбор предмета (`ui/pick_<PickSound>`), дверь уровня (заперто), рычаг/колесо (пределы, «застрял»), разбивание, эффекты (`EffectsOn/OffSound`, свои звуковые сущности `.ent` гаснут/загораются с эффектами и активностью), статическое движение (`StaticMoveStart/Stop/LoopSound`). `HplEntity.PlaySound` = `iLuxEntity::PlaySound`, `PlaySoundAt` = звук в точке.
- `HplMap.DestroyEntity` — отложенное удаление в конце шага (объект сразу выключается: `Destroy` в Play ждёт конца кадра, а осколки не должны выталкиваться из целого горшка); `CreateEntity` — клон шаблона с переименованием префиксов детей.
