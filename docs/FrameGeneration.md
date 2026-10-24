# DLSS Frame Generation через Streamline: исследование (2026-10-18)

Вопрос: можно ли подключить NVIDIA DLSS Frame Generation (DLSS-G, в том числе MFG на RTX 50) к сборке
Unity 6000.6.2f1 + HDRP 17.6 (DX12) проекта `UnityProj/UMnesiaRTX` через Streamline SDK.
Сейчас генерация кадров — NVIDIA Smooth Motion драйвера (см. CLAUDE.md, «DLSS и вкладка RTX»).
Ничего не внедрено: этот файл — отчёт и план.

## Вывод

**Реально, но только в собранном плеере (не в редакторе) и ценой своего нативного плагина.**
Главное препятствие (Unity сам создаёт устройство и swap chain) снимается штатными средствами:
плагин с флагом «Load on startup» загружается до создания графического устройства, ставит хук на
`IDXGIFactory::CreateSwapChainForHwnd` и отдаёт Unity прокси-swap chain Streamline. Это уже сделано
в открытом проекте для Unity 6.3 (UnityDLSSNR, см. ниже) — правда, без Streamline, напрямую через NGX.
Всё остальное (ресурсы, константы, Reflex) в Unity 6000.6 доступно: `IUnityGraphicsD3D12v8` даёт
командный список рендер-потока и управление состояниями ресурсов, HDRP — глубину, векторы движения и
матрицы без джиттера.

Оценка объёма: **2–4 недели** сосредоточенной работы. Из них 2–3 дня — проверочный прототип
(шаг 1 плана), по результату которого станет ясно, нет ли блокирующей несовместимости с present-путём Unity.
Код: нативный плагин C++ ~1.5–2.5 тыс. строк, C# ~0.5–1 тыс. строк, плюс проверки.

Выигрыш против Smooth Motion: векторы движения и глубина игры (меньше артефактов на краях и в движении),
HUD-less буфер (HUD не плывёт), MFG 3x/4x, Reflex (меньше задержка), управление из меню игры.
Smooth Motion работает без всего этого и уже есть, поэтому DLSS-G — улучшение, а не необходимость.

## 1. Как Streamline перехватывает swap chain

- Обычный способ: игра линкуется с `sl.interposer.dll` вместо `dxgi.dll` / `d3d12.dll`, вызывает `slInit`
  до любых вызовов DXGI/D3D, и все фабрики, устройства и swap chain — прокси Streamline. В Unity
  невозможно: движок сам грузит `dxgi.dll` / `d3d12.dll`.
- **Ручные хуки** (`ProgrammingGuideManualHooking.md`, SDK 2.14.16): `slInit` с
  `PreferenceFlags::eUseManualHooking`, затем `slUpgradeInterface(&obj)` превращает нативный объект в прокси.
  Обязательные перехваты: `IDXGIFactory::CreateSwapChain*`, `IDXGISwapChain::Present/Present1/GetBuffer/
  ResizeBuffers/ResizeBuffers1/GetCurrentBackBufferIndex/SetFullscreenState`, `ID3D12Device::CreateCommandQueue`.
  Важно: «D3D-устройство может быть создано до или после `slInit`» — значит, устройство Unity подходит,
  достаточно `slSetD3DDevice(native)`. Каждый кадр должен вызываться `presentCommon()` — его вызывает прокси
  swap chain, если хост делает Present через прокси.
- С DLSS-G Streamline сам владеет настоящим swap chain (больше буферов, флаг waitable object), а хосту отдаёт прокси;
  при включённой генерации Present идёт в потоке Streamline.

## 2. Как это сделать в Unity

1. **Ранняя загрузка.** Нативный плагин (`Assets/Plugins/x86_64/RtxStreamline.dll`, импортёр: Load on startup /
   `isPreloaded`). Его `UnityPluginLoad` в плеере вызывается до «GfxDevice: creating device» (так работает
   UnityDLSSNR на Unity 6.3; для 6000.6 проверить в шаге 1). В нём: проверка подписи и `LoadLibrary`
   `sl.interposer.dll`, `slInit` (DLSS-G, Reflex, PCL; `eUseManualHooking`), временная фабрика DXGI и подмена
   слота `CreateSwapChainForHwnd` в её vtable (vtable общая для всех фабрик `dxgi.dll`).
2. **Хук создания swap chain**: `queue->GetDevice()` → `slSetD3DDevice`; `slUpgradeInterface(&factory)`;
   прокси-фабрика создаёт swap chain → Unity получает прокси. Нужна защита от повторного входа: прокси
   Streamline сам вызывает нативную фабрику (и создаёт свой настоящий swap chain) — через тот же хук.
3. **`CreateCommandQueue`**: очереди Unity создаёт до swap chain. Если Streamline без перехвата очередей не
   работает (выяснить в шаге 1) — дополнительно перехватить `D3D12CreateDevice` (IAT `UnityPlayer.dll` или
   detour) и создавать устройство через `slUpgradeInterface`, отдавая Unity нативное устройство, а
   `CreateCommandQueue` направлять в прокси.
4. **Редактор**: генерации не будет (в редакторе много swap chain, `IUnityGraphicsD3D12v8::GetSwapChain` = null).
   Плагин в редакторе только логирует и отключается; проверка — только в собранном плеере (как и Smooth Motion).

## 3. Что тегировать каждый кадр и где взять в HDRP

| Что | Откуда | Примечание |
|---|---|---|
| Backbuffer | сам Streamline (прокси) | тег не нужен |
| Глубина (`kBufferTypeDepth`) | `CustomPassContext.cameraDepthBuffer` | разрешение рендера (DLSS SR), обратный Z → `depthInverted = true` |
| Векторы движения (`kBufferTypeMotionVectors`) | `ctx.cameraMotionVectorsBuffer` | разрешение рендера; HDRP хранит смещение в UV, масштаб как у HDRP `DLSSPass` (`mvScale = −размер рендера` в пикселях → для SL, вероятно, `{−1, −1}` в нормированных — знак проверить в SL ImGui); включают движение камеры |
| HUD-less цвет (`kBufferTypeHUDLessColor`) | копия в конце `HplScreenEffectsPass` (AfterPostProcess, выходное разрешение, после гаммы) | должен совпадать по цветовому пространству с backbuffer: писать в `R8G8B8A8_SRGB`; финальный проход HDRP в SDR — копия |
| UI (`kBufferTypeUIAlpha` / `UIColorAndAlpha`) | нет (см. п. 4) | желательно, но не обязательно |
| Константы (`slSetConstants`) | `HDCamera.GetOrCreate(cam).mainViewConstants`: `viewMatrix`, `nonJitteredProjMatrix`, `prevViewProjMatrix`; позиция и оси камеры, near/far, FOV | матрицы без джиттера, построчные (transpose из Unity); `reset` при смене карты, загрузке, телепорте, катсцене |

- Разрешение рендера плавает (динамическое разрешение HDRP для DLSS) → у тегов глубины и векторов —
  `sl::Extent` (вьюпорт внутри RTHandle), флаг `DLSSGFlags::eDynamicResolutionEnabled` только если масштаб
  действительно меняется; у фиксированного режима DLSS — не ставить.
- Надёжный путь: в custom pass копировать глубину (в R32_Float), векторы и HUD-less в свои постоянные
  RenderTexture (указатели `GetNativeTexturePtr()` берутся один раз при создании), затем
  `cmd.IssuePluginEventAndData` → в рендер-потоке `IUnityGraphicsD3D12v8::CommandRecordingState` (командный
  список Unity), `RequestResourceState` → нужное состояние, `slSetTagForFrame(token, …, cmdList)`.
  Событие настраивается `ConfigureEvent(kUnityD3D12GraphicsQueueAccess_DontCare, …)`.
- Кадровый токен: главный поток кадра N берёт `slGetNewFrameToken` и константы, рендер-поток Unity
  рисует кадр N−1 — индекс кадра передаётся в данных plugin event; Present-маркеры должны получить индекс
  того кадра, что реально показывается.

## 4. HUD и меню

HDRP 17.6 сам рисует Screen Space Overlay UI внутри своего render graph: в SDR — проход «Screen Space Overlay UI»
сразу в backbuffer после финального прохода (`HDRenderPipeline.RenderGraph.cs`, `RenderScreenSpaceOverlayUI`),
в HDR — в отдельный буфер «UI Color Buffer» (`RenderHDROffscreenUI`) с наложением в финальном проходе.
Точки вставки custom pass после финального прохода нет.

Варианты (по возрастанию качества и работы):
1. **Только HUD-less** (копия в `HplScreenEffectsPass`, она же без HUD). DLSS-G работает и без UI-буфера;
   HUD частично искажается при интерполяции, но HUD-less позволяет это ослабить. Первый этап.
2. **UI alpha**: в `beginContextRendering` выключить показ холстов HUD для HDRP, рисовать их самим
   (`ScriptableRenderContext.CreateUIOverlayRendererList` в custom pass AfterPostProcess) в свой RT и
   накладывать в `HplScreenEffectsPass` — тогда HUD-less = кадр до наложения, UI alpha = альфа RT.
   Меняется порядок с финальным проходом HDRP (в SDR он копия — допустимо). Затрагивает `HplHud`,
   `HplGameUi` и CPU-снимки `HudSnapshot`.
3. **Меню**: на паузе, в инвентаре, журнале, на экране загрузки, в титрах и главном меню генерацию выключать
   (`DLSSGOptions::mode = eOff`; автоопределение меню `eEnableFullscreenMenuDetection` требует UI-буфер).
   Выключать также при смене разрешения / режима экрана (требование Streamline, иначе возможны дедлоки Present).

HDR: финальный проход HDRP кодирует PQ — HUD-less из `HplScreenEffectsPass` не совпадёт с backbuffer.
Для HDR первым этапом — без HUD-less (или генерацию выключать), позже — свой HUD-less после кодирования.

## 5. Reflex

Обязателен: DLSS-G работает только с `sl.reflex` + `sl.pcl` (своя интеграция Reflex не подходит).
- `slReflexSleep(token)` и `eSimulationStart` — в начале кадра (`PlayerLoop`, своя система перед `Initialization`);
  `eSimulationEnd` — после `PreLateUpdate`; `eRenderSubmitStart/End` — вокруг рендера (события рендер-потока);
  `ePresentStart/End` — вокруг `Present` прокси (подменить слот `Present` в vtable прокси или обернуть прокси
  своим COM-объектом). Индексы кадров маркеров Present должны совпадать с константами, иначе в логе SL
  «common constants cannot be found for frame N».
- Ограничение FPS при генерации — `ReflexOptions::frameLimitUs`, а не `Application.targetFrameRate`;
  VSync — только если `bIsVsyncSupportAvailable`.
- Waitable object: если Unity создаёт swap chain с `DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT`
  и ждёт на нём, он остаётся у Unity (SL его не трогает); если нет — Unity не должна вызывать
  `GetFrameLatencyWaitableObject` / `SetMaximumFrameLatency`. Проверить в шаге 1.

## 6. Лицензия

- Streamline SDK — MIT (`license.txt`, github.com/NVIDIA-RTX/Streamline, 2.14.16 от 2026-10-16), кроме
  `sl_nvperf` (Nsight Perf SDK). Готовые подписанные DLL (`sl.interposer.dll`, `sl.common.dll`, `sl.dlss_g.dll`,
  `sl.reflex.dll`, `sl.pcl.dll`, `nvngx_dlssg.dll`) — из релизов SDK, распространяются по условиям NVIDIA RTX SDK.
  Проект личный и не распространяется — ограничений нет. Самосборные DLL Streamline не подписаны и годятся
  только для отладки.
- DLL NVIDIA — не наш код: в git не класть (как ресурсы Amnesia), копировать скриптом из SDK в
  игнорируемую папку / рядом с собранным exe.
- Заголовки Unity Plugin API — Unity Companion License (для проектов на Unity допустимо).
- UnityDLSSNR — **без лицензии** (все права у автора): код не копировать, только изучать подход.

## 7. Существующие примеры

- **Kuan-Mi/UnityDLSSNR** (github.com/Kuan-Mi/UnityDLSSNR), Unity 6.3, URP 17 + RenderGraph, DX12:
  DLSS SR, DLSS-G и DLSS 5 Neural Rendering. Генерация — **не через Streamline**, а напрямую NGX
  (`NGX_D3D12_CREATE_DLSSG` / `EVALUATE_DLSSG`) со своим прокси swap chain (Unity видит свои буферы 1:1,
  настоящих вдвое больше, плагин делает второй Present сам) и своим пейсингом. Плагин «Load on startup»
  подменяет слоты `IDXGIFactory::CreateSwapChain/ForHwnd` до создания swap chain; запасной путь — принять
  уже созданный swap chain из `IUnityGraphicsD3D12v7/v8` без вставки кадров. Только плеер, HDRP нет.
  Главное доказательство, что перехват в Unity 6 работает.
- Форум NVIDIA «DLSS Unity integration» — попытка своего плагина для URP: ошибок нет, но и сгенерированных
  кадров не видно (вероятные причины — не тот swap chain / нет Reflex-маркеров).
- Официальной интеграции DLSS-G для Unity у NVIDIA нет (только Unreal); встроенный модуль Unity
  `com.unity.modules.nvidia` — только DLSS SR / DLAA.

## 8. План (после согласования)

1. **Проверочный прототип (2–3 дня).** Плагин «Load on startup»: лог всех вызовов Unity к фабрике и swap chain
   (создание, флаги, число буферов, waitable object, `ResizeBuffers`, `SetFullscreenState`, QI на
   `IDXGISwapChain3/4`); затем `slInit` + прокси swap chain с DLSS-G `eOff` (игра должна работать как раньше),
   затем `eOn` с заглушками тегов — счётчик `numFramesActuallyPresented` из `slDLSSGGetState`.
   Критерий: в собранном плеере `00_rainy_hall` показывается ×2 кадров без падений при смене окна / разрешения.
2. Reflex/PCL маркеры и `slReflexSleep` в `PlayerLoop` и рендер-потоке; Reflex-оверлей SL для проверки.
3. Теги: custom pass копий (глубина, векторы, HUD-less), константы из `HDCamera`, `reset` при смене карты /
   загрузке / `PlayerLookAt`-скачках; отладка через SL ImGui (`sl.imgui`, отладочные DLL).
4. Настройки на вкладке RTX: «Генерация кадров» (выкл / 2x / 3x / 4x, недоступные — серым по
   `numFramesToGenerateMax`), предупреждения (HAGS выключен, нет RTX 40+), выключение в меню и при смене экрана.
5. UI alpha (вариант 2 из п. 4), затем HDR.
6. Проверка: `FrameGenPreview` для собранного плеера (как `DecalCheck.exe`): запуск exe, отчёт SL о
   кадрах, снимки, время кадра; запись в CLAUDE.md.

## 9. Риски

- **Present-путь Unity** (главный): Unity может использовать waitable object, `Present1`, свои предположения о
  числе буферов, вызывать `ResizeBuffers` из другого потока, держать нативный указатель где-то помимо
  возвращённого — тогда прокси не сработает или зависнет. Проверяется шагом 1; если не выходит — запасной путь
  UnityDLSSNR (свой прокси + NGX DLSS-G без Streamline), но тогда Reflex — отдельно через NVAPI, а без
  Streamline-Reflex NVIDIA DLSS-G официально не поддерживает.
- **Два NGX в процессе**: DLSS SR — встроенный модуль Unity (свой NGX), DLSS-G — NGX внутри Streamline.
  Совместимость не документирована; при конфликте — перенести DLSS SR в Streamline (`sl.dlss`) и отказаться от
  встроенного DLSS HDRP (это большая переделка: HDRP-путь динамического разрешения).
- **Обновления Unity**: хуки vtable и порядок загрузки плагинов — внутреннее поведение, может сломаться в
  следующей версии редактора.
- **Качество**: полупрозрачные (частицы, ореолы, Mul-декали, вода) без векторов движения, след при движении и
  радиальное размытие HPL2 рисуются после интерполируемых данных — возможны артефакты, как у Smooth Motion,
  но слабее. Path tracing копит кадры — генерация при нём бессмысленна, выключать.
- **Отладка**: только в собранном плеере (каждая итерация — сборка), отладочные DLL SL без подписи требуют
  отключения проверки подписи в отладке.
- Требования: Windows 10 20H1+, включённое аппаратное планирование GPU (HAGS), RTX 40+ (MFG — RTX 50),
  цена на RTX 5080: 1.45 мс (2x) / 2.43 мс (4x) на 1080p, ~270 МБ VRAM.

## Источники

- Streamline SDK 2.14.16: `docs/ProgrammingGuide.md`, `ProgrammingGuideManualHooking.md`,
  `ProgrammingGuideDLSS_G.md`, `ProgrammingGuideReflex.md`, `ProgrammingGuidePCL.md` — https://github.com/NVIDIA-RTX/Streamline
- https://developer.nvidia.com/rtx/streamline/get-started
- https://github.com/Kuan-Mi/UnityDLSSNR (`RenderingPlugin/Source/FrameGenerationHooks.*`, `FrameGenerationSwapChain.*`, `PluginMain.cpp`)
- https://forums.developer.nvidia.com/t/dlss-unity-integration/360467
- Unity Plugin API 6000.6: `Editor/Data/PluginAPI/IUnityGraphicsD3D12.h` (`IUnityGraphicsD3D12v8`)
- HDRP 17.6: `HDRenderPipeline.RenderGraph.cs` (UI overlay), `RenderPass/DLSSPass.cs` (масштаб векторов), `Camera/HDCamera.cs` (`ViewConstants`)
