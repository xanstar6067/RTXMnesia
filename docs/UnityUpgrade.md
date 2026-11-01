# Пробный переезд на Unity 6000.6.7f1

План на эксперимент (запланирован примерно на 2026-11-08). Текущая версия — 6000.6.2f1
(`ProjectSettings/ProjectVersion.txt`), HDRP 17.6.0.

## Зачем

- Заметки к 6.3–6.7 изучены 2026-11-01: исправлений про DLSS, видеопамять D3D12, утечку камер или
  память HDRP нет. Утечку при смене сцены уже обходит `HplRenderCamera` (docs/Reference/Graphics.md).
- Польза от переезда: 6.4 — случайный вылет на Windows под D3D12 (use-after-free при освобождении
  GPU-буферов, UUM-150682); 6.6 — искажения Render Graph с async compute (UUM-141896); 6.7 — точнее
  Memory Profiler по нативной памяти (пригодится для остатка +320 МБ на возврат из бенчмарка в титул).
- Известная ошибка, открытая и в 6.7: зависание плеера при многократной смене сцен через
  `LoadSceneAsync` (UUM-156125, отмечена с 6000.0.75f1, значит, вероятно, есть и у нас). Проверять отдельно.

## Насколько это сложно

Переход внутри одной ветки 6000.6 (patch-релизы f1): API не ломается, формат ассетов тот же.
Ожидаемо: полный реимпорт `Library` и, возможно, мелкая правка пакетов. Главные риски — наш код,
который лезет во внутренности HDRP через reflection (при несовпадении он молча ничего не делает,
поэтому сломается тихо, а не ошибкой компиляции):

| Где | Что берёт | Что будет, если пропадёт |
|---|---|---|
| `Hpl/HplDlss.cs` | `HDRenderPipelineAsset.m_RenderPipelineSettings` | не применится пресет DLSS |
| `World/HplEnhancedShadows.cs` | то же поле | не переключится качество фильтрации теней |
| `Hpl/HplGraphics.cs` `ReleaseCameraData` | `HDCamera.s_Cameras`, `HDCamera.Dispose()` | бенчмарк не освободит буферы камеры титула |
| `World/HplEnhancedLighting.cs` `WaxProfileHash` | `DiffusionProfileSettings.profile.hash` | воск свечей без просвечивания |
| `Editor/ConfigureHDRP.cs` | `PlayerSettings.SetLightmapEncodingQualityForPlatform`, `Ensure` глобальных настроек | настройка HDRP из меню не сработает |
| `Hpl/HplRenderCamera.cs` | `Camera.CopyFrom`, `HDAdditionalCameraData.CopyTo` (публичные) | вернётся утечка видеопамяти |

Нативные плагины: `com.unity.modules.nvidia` (DLSS), `com.unity.rendering.denoising` 1.0.5 — проверить,
что DLSS доступен (`HDDynamicResolutionPlatformCapabilities.DLSSDetected`) и пресеты работают.

## Подготовка (основной проект не трогаем)

1. Закоммитить всё нужное в `main`, рабочее дерево чистое. Записать хэш коммита.
2. Установить 6000.6.7f1 через Hub с модулем Windows Build Support (IL2CPP, если нужен).
3. Сделать **копию проекта** вне основного: `UnityProj/UMnesiaRTX` без `Library`, `Temp`, `Logs`
   (Assets с `AmnesiaImported` ~23,5 ГБ + Packages + ProjectSettings), например `E:\UnityDev\RTXMnesia-67\`.
   Место есть (на E: свободно ~3,6 ТБ); с `Library` копия займёт ~50 ГБ. Скрипты из `tools/` и `source/`
   не нужны: импортированный контент уже в `Assets/AmnesiaImported`.
4. Основной проект на 6.2 остаётся рабочим: если эксперимент не удастся, копия просто удаляется.

## Переезд

5. Открыть копию в 6.7 (Hub → Add → выбрать версию). Ждать полный реимпорт: несколько часов
   (сейчас `Library\DataStore` ~26 ГБ). Сохранить `Editor.log` первого открытия.
6. Принять обновление пакетов, если Unity предложит (HDRP 17.6.x той же ветки). Сравнить
   `Packages/manifest.json` и `packages-lock.json` с основным проектом, записать изменения.
7. Снять diff `ProjectSettings` с основным проектом: что Unity переписал сам.

## Проверки (по порядку, всё со звуком 0%)

8. `BuildCheck.RunBatch` — ошибки компиляции и устаревшие API (CS0618/CS0612/CS0619).
9. Reflection из таблицы выше: в Play-режиме на карте проверить пресет DLSS, фильтрацию теней,
   воск, `ReleaseCameraData` (в логе не должно быть «Camera data not released»).
10. Существующие batch-проверки (все в `Assets/Editor/Dev`, 50+ штук), в первую очередь:
    `MenuPreview`, `HdrTests`, `DlssPreview`, `RtPreview`, `EnhancedLightingPreview`, `LampTests`,
    `SaveTests`, `MapChangePreview`, `BenchmarkPreview`, `BugReportTests`, `PlayerSimulation` на нескольких картах.
11. Тестовый плеер (`VideoMemoryPlayer.BuildBatch`) на обеих версиях с одинаковыми настройками:
    - `-benchmarkReturn 2` — FPS по сценам, 1%, пик видеопамяти, память перед сценами и в титуле;
    - `-benchmarkChain` — смены карт через `HplMapChange` (утечка и зависание UUM-156125; для зависания
      увеличить число переходов до 30–50);
    - `-benchmarkShots` с `hdrMode=0` — сравнить картинку 6.2 и 6.7 по сценам.
    Сравнить с цифрами 6.2 из docs/Reference/DevTools.md («Видеопамять: утечка при смене сцены»).
12. Отдельно попробовать без `HplRenderCamera` (временный выключатель только в тестовом плеере):
    вдруг в 6.7 утечка камер исчезла сама.
13. Ручная проверка игроком (по желанию пользователя): главное меню, новая игра, пара переходов,
    HDR, DLSS, RT, сохранение и загрузка, бенчмарк на ноутбуке с RTX 3050.

## Решение

- **Переезжать**, если: компиляция чистая, проверки 8–11 не хуже 6.2, картинка та же, FPS не ниже.
  Тогда переезд повторяется уже на основном проекте (открыть в 6.7, закоммитить `ProjectVersion.txt`,
  `Packages/*`, изменённые `ProjectSettings`) и обновляются CLAUDE.md/AGENTS.md (версия в таблице
  структуры и в команде headless-запуска) и docs/Reference/State.md.
- **Остаться на 6.2**, если что-то сломалось и не чинится быстро: записать найденное сюда, копию удалить.
- После эксперимента удалить копию проекта и тестовые сборки (правило проекта).

## Итоги эксперимента

### 2026-11-01: обновление основного проекта пользователем, проверки агента

Unity 6000.6.7f1, HDRP остался 17.6.0. Navigation 2.0.16, Input System 1.20.1,
Rider 3.1.0. Перед batch-прогонами сохранена копия ProjectSettings:
`UnityProj/UMnesiaRTX/Logs/Upgrade67/ProjectSettingsBefore`.
Все запуски в batchmode, вывод звука заглушён штатным HplBatchAudio; плеер не собирался.

| Проверка | Результат |
|---|---|
| BuildCheck (повторно после исправлений) | PASS: 33 сборки, 0 ошибок, 0 obsolete API |
| HdrTests.RunRenderBatch | PASS: 37 проверок с GPU |
| SaveTests | PASS: 41 проверка |
| LampTests | PASS |
| DlssPreview, 01_old_archives | PASS: 21 проверка, DLAA / масштабы DLSS / пресет K |
| RtPreview, 01_old_archives | PASS: 64 проверки, RT-тени / AO / отражения / GI / path tracing / C |
| MenuPreview | PASS: настройки, инвентарь, журнал |
| MapChangePreview | PASS: 69 проверок переходов, переноса состояния, возврата, урона и смерти |
| BenchmarkPreview -benchmarkQuick, повторно | PASS: шесть сцен, результаты, отмена, возврат, сохранность настроек |

Найдены и исправлены:
- `HdrTests`: после прямого изменения hdrMode надо обновить кнопки меню; иначе сохранённый
  выключенный HDR оставляет кнопку калибровки заблокированной. Перед отменой настроек нужно
  закрыть отдельное окно калибровки. Исправлена подготовка/последовательность теста, не игра.
- В первом бенчмарке, несмотря на PASS самого теста, лог содержал MissingReferenceException
  в HplRenderCamera.OnBeforeRender при обращении к уничтоженной камере. Добавлены защита
  от вызова вне Play и повторное создание утраченной render camera с прежним источником.
  Повторный полный прогон BenchmarkPreview: exit 0, исключений и FAIL в логе нет.

Unity мигрировала сериализацию TimeManager.asset: 2352000 / 141120000 = 1/60 секунды,
частота логики сохранена. Добавление dev_room в Windows build profile тестом отменено.
AGENTS.md и CLAUDE.md синхронно обновлены на 6000.6.7f1.

Логи: `UnityProj/UMnesiaRTX/Logs/Upgrade67/` (финальные: BuildCheckFinal.log,
HdrTestsFinal.log, BenchmarkFinal.log); отчёты DLSS, RT, переходов и бенчмарка в штатных
папках Logs. Итоговый визуально просмотренный кадр RTGI:
`UnityProj/UMnesiaRTX/Logs/RtCheck/01_old_archives_gi2.png`. Промежуточные PNG прогона удалены.

Проверки редактора прошли. Это не полный сквозной проход игры и не доказательство равенства
FPS/видеопамяти версии 6.2: парное измерение плееров, 30–50 переходов LoadSceneAsync,
физический HDR-вывод дисплея и отдельная проверка на RTX 3050 не выполнялись.
