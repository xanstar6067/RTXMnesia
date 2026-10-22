# Журнал улучшенного освещения по картам

Как проходить карту — [README.md](README.md). Статусы: **готово** (все комнаты проверены рендерами и
пользователем), **первичная** (оверлей есть, проверен рендерами, пользователь не смотрел), **частично**
(часть комнат), пусто — не начата. Общие правила (лампы, заполняющий свет без теней) действуют на всех картах
и без оверлея.

| Карта | Статус | Оверлей |
|---|---|---|
| `maps/main/ch01/00_rainy_hall` | | |
| `maps/main/ch01/01_old_archives` | частично (остался `LightOff_6`) | `01_old_archives.json` |
| `maps/main/ch01/02_entrance_hall` | | |
| `maps/main/ch01/03_archives` | | |
| `maps/main/ch01/04_wine_cellar_lab` | | |
| `maps/main/ch01/05_wine_cellar` | | |
| `maps/main/ch01/06_distillery` | | |
| `maps/main/ch01/07_archives_cellar` | | |
| `maps/main/ch01/08_cellar_maze` | | |
| `maps/main/ch01/09_back_hall` | | |
| `maps/main/ch01/10_daniels_room` | | |
| `maps/main/ch01/11_study` | | |
| `maps/main/ch01/12_storage` | | |
| `maps/main/ch01/13_machine_room` | | |
| `maps/main/ch01/14_elevator` | | |
| `maps/main/ch02/15_prison_north` | | |
| `maps/main/ch02/15_prison_south` | | |
| `maps/main/ch02/16_cistern_entrance` | | |
| `maps/main/ch02/17_control_room` | | |
| `maps/main/ch02/18_cistern` | | |
| `maps/main/ch02/19_morgue` | | |
| `maps/main/ch02/20_sewer` | | |
| `maps/main/ch02/21_torture_nave` | | |
| `maps/main/ch02/22_torture_chancel` | | |
| `maps/main/ch02/23_torture_transept` | | |
| `maps/main/ch02/24_torture_choir_east` | | |
| `maps/main/ch02/24_torture_choir_west` | | |
| `maps/main/ch03/25_cell_tunnels` | | |
| `maps/main/ch03/26_torture_nave_redux` | | |
| `maps/main/ch03/26_zimmerman_vision` | | |
| `maps/main/ch03/27_torture_chancel_redux` | | |
| `maps/main/ch03/28_inner_sanctum` | | |
| `maps/main/ch03/29_orb_chamber` | | |
| `maps/ptest/01_cells` | | |
| `maps/ptest/02_library` | | |
| `maps/ptest/03_dungeon` | | |
| `maps/ptest/04_final` | | |

## maps/main/ch01/01_old_archives (2026-10-21 — 2026-10-22)

| Комната | Что сделано | Почему |
|---|---|---|
| Комната с печью (x 24.5–36.5, z 11–27) | `PointLight_35/36` (заполняющие в центре): при RTGI 0, без GI 0.7, без теней. `SpotLight_19/20` (лунные лучи) перенесены к стеклу, 2.5, дальность 10, `sourceRadius` 0.35. Добавлены `Window_sky_17/21` (rect 1.1 × 1.47, 90 нит). Ниши окон `PointLight_33/34` — 0.3. `SpotLight_2` (огонь печи) — дальность 7, `sourceRadius` 0.15 | Заполняющий свет в 2 м над центром давал тени ящиков «из центра в стороны»; луч окна стоял в 0.7 м от стекла, и тени не шли вглубь комнаты |
| Зал с факелами (x −10…0, z 7–26) | `LightOff_1…4` (тёплый «свет факела на весь зал»): при RTGI 0, без GI 0.6. Скрипт гасит их вместе с факелами — имена сохранены | Физические факелы (10 свечей, 24.5 м) и RTGI освещают зал сами; колонны теперь отбрасывают тени на стены |
| Коридор у окон (x 8.5, z 9–31) | `SpotLight_14/15` к стеклу (2.5, 10, 0.35). Добавлены `Window_sky_corridor_south` (устье эркера, 2.9 × 1.43, 150 нит) и `_north` (1.1 × 1.47, 150 нит). `PointLight_23/32/27`: RTGI 0, без GI 0.6. Ниши `PointLight_19/24` — 0.3 | Как в комнате с печью; без заполняющего света коридор был почти чёрным, пока не было источников окон |
| Северная комната (x 12–20, z 24–31) | Добавлен `Window_sky_north_room` за досками заколоченного окна (150 нит). `PointLight_28`: RTGI 0, без GI 0.6. Ниши `PointLight_26/37` — 0.3 | Доски дают полосы на полу |
| Обрушенная комната (x −12…−6, z 17–24) | Добавлен `Hole_light_collapsed_room` над досками пролома (rect 2.2 × 3.2, смотрит вниз, 80 нит). `PointLight_30/31`: RTGI 0, без GI 0.6 | Свет лунного зала сверху через пролом; доски и камни дают тени. Дальняя стена с лестницей темнее классики — физично, можно поднять яркость |
| Комната `LightOff_6` (к северу от комнаты с печью) | не тронута | Без заполняющего света полностью чёрная: сначала нужен настоящий источник |
| Верхний ярус | не тронут | Освещается box-светом (`BoxLight_1/2`), оверлей его не меняет |

Проверено рендерами `EnhancedLightingPreview` (классика RT, улучшенное RT и растр); пользователь одобрил
комнату с печью и зал с факелами. Не проверено: `LightOff_6`, верхний ярус.
