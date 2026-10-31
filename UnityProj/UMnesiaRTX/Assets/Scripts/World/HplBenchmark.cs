using System;
using System.Collections;
using System.Collections.Generic;
using RTXMnesia.Enemies;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RTXMnesia.World
{
    /// <summary>Temporary additive worlds. The originating scene and options draft stay in memory, untouched.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class HplBenchmark : MonoBehaviour
    {
        public const float CancelHoldSeconds = 1.5f;
        static HplBenchmark s_instance;
        static string s_loadingPath;
        public static bool Active => s_instance != null;
        internal static bool IsLoading(string path) => Active && path == s_loadingPath;
        public int SceneIndex { get; private set; } = -1;
        public float SceneTime { get; private set; }
        public bool CancelRequested { get; private set; }
        public bool IsPlaying => _playing;
        public HplMap Map { get; private set; }
        public string Error { get; private set; }
        public HplBenchmarkStats Stats { get; } = new();
        public bool ShowingResults => _results != null;
        public bool CounterVisible => _counterVisible;

        HplGameUi _owner;
        HplUserSettings _settings;
        Scene _originalScene, _worldScene, _scratchScene;
        readonly List<GameObject> _suspended = new();
        readonly List<(AudioSource source, int sample, bool playing)> _audio = new();
        UnityEngine.Random.State _random;
        System.Random _hplRandom;
        CursorLockMode _cursorLock;
        bool _cursorVisible, _soundPaused;
        float _timeScale, _hold, _black = 1;
        bool _playing, _restored;
        Canvas _canvas;
        RawImage _curtain, _holdBar;
        HplGuiText _text;
        HplEnemy _enemy;
        bool _enemyVoice;
        float _enemyWalkScale;
        double _sceneStartedAt;
        bool _skipFirstFrame, _counterVisible = true, _english;
        double _lastSampleAt, _nextCounterAt, _nextMemoryAt;
        const float OverlayMargin = 20;
        static readonly Color Gold = new(232f / 255, 201f / 255, 28f / 255), Dim = new(0.82f, 0.79f, 0.72f);
        HplGuiText _counter;
        RawImage[] _segmentBack, _segmentFill;
        string _fpsText = "--", _detailText = "";
        bool _counterDirty = true;
        float _counterDrawnAlpha = -1;
        readonly FrameTiming[] _timings = new FrameTiming[1];
        readonly int[] _timingFrames = new int[16], _timingSamples = new int[16];
        ulong _lastTimingTimestamp;
        HplBenchmarkResults _results;
        HplBenchmarkResults.Choice _resultChoice;
        string[] _system;

        string T(string ru, string en) => _english ? en : ru;

        sealed class Setup : HplScript
        {
            public void Machine()
            {
                RotatePropToSpeed("elevator_machine_cog_small_2", 1, -3, 0, 0, 1, false, "");
                RotatePropToSpeed("elevator_machine_cog_small_1", 1, 3, 0, 0, 1, false, "");
                RotatePropToSpeed("elevator_machine_cog_small_3", 1, -3, 1, 0, 0, false, "");
                RotatePropToSpeed("elevator_machine_cog_main_1", 1, 1, 1, 0, 0, false, "");
                PlaySoundAtEntity("benchmark_machine", "13_machine_run", "elevator_machine_cog_main_1", 0, true);
            }
        }

        public static bool TryStart(HplGameUi owner, HplUserSettings settings, out string error)
        {
            error = null;
            if (Active || owner == null || settings == null || owner.IsTransitioning || owner.Map.IsChangingMap)
                return false;
            foreach (var route in HplBenchmarkRoute.All)
                if (!Application.CanStreamedLevelBeLoaded(route.ScenePath))
                {
                    error = "Тест недоступен: карта не включена в сборку: " + route.map;
                    return false;
                }
            var go = new GameObject("HPL performance test");
            DontDestroyOnLoad(go);
            var test = go.AddComponent<HplBenchmark>();
            s_instance = test;
            test._owner = owner;
            test._settings = settings.Copy();
            test.StartCoroutine(test.Drive());
            return true;
        }

        // Catch failures from nested scene work, then ALWAYS unload temporary worlds and restore the owner.
        IEnumerator Drive()
        {
            var run = Run();
            while (true)
            {
                object next = null;
                bool moved;
                try { moved = run.MoveNext(); if (moved) next = run.Current; }
                catch (Exception e) { Error = e.Message; Debug.LogException(e); break; }
                if (!moved) break;
                yield return next;
            }
            _playing = false; _black = 1;
            CloseResults();
            s_loadingPath = null;
            if (_worldScene.IsValid() && _worldScene.isLoaded) yield return SceneManager.UnloadSceneAsync(_worldScene);
            if (_scratchScene.IsValid() && _scratchScene.isLoaded) yield return SceneManager.UnloadSceneAsync(_scratchScene);
            Restore();
            Destroy(gameObject);
        }

        IEnumerator Run()
        {
            _originalScene = SceneManager.GetActiveScene();
            _timeScale = Time.timeScale;
            _cursorLock = Cursor.lockState; _cursorVisible = Cursor.visible;
            _random = UnityEngine.Random.state;
            _hplRandom = HplMath.ExchangeRandom(new System.Random(317));
            _english = _owner.Map.language?.language?.StartsWith("eng", StringComparison.OrdinalIgnoreCase) == true;
            CreateOverlay();
            _soundPaused = _owner.Map.sound.MenuPaused;
            // Include all initially loaded scenes, so a multi-scene editor setup is suspended too.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    if (root.activeSelf)
                    {
                        _suspended.Add(root);
                        // HPL sound objects have DontSave: FindObjectsByType would miss them.
                        foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                            if (source.clip != null && source.gameObject.activeInHierarchy)
                                _audio.Add((source, source.timeSamples, source.isPlaying));
                    }
            _owner.Map.sound.SetMenuPaused(true);
            foreach (var root in _suspended) root.SetActive(false);
            // Disabled cameras keep their HDRP buffers forever; the tour would hold both scenes' sets (video memory).
            foreach (var root in _suspended)
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) HplGraphics.ReleaseCameraData(camera);
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            bool repeat;
            do
            {
                repeat = false;
                Stats.Clear();
                for (int i = 0; i < _timingFrames.Length; i++) _timingFrames[i] = -1;
                for (SceneIndex = 0; SceneIndex < HplBenchmarkRoute.All.Length && !CancelRequested; SceneIndex++)
                {
                    var route = HplBenchmarkRoute.All[SceneIndex];
                    SceneTime = 0; _enemy = null; _enemyVoice = false;
                    UnityEngine.Random.InitState(317 + SceneIndex);
                    HplMath.Seed(317 + SceneIndex);
                    _scratchScene = SceneManager.CreateScene("Benchmark runtime " + SceneIndex);
                    SceneManager.SetActiveScene(_scratchScene);
                    s_loadingPath = route.ScenePath;
                    void Loaded(Scene scene, LoadSceneMode mode)
                    {
                        if (scene.path != route.ScenePath) return;
                        _worldScene = scene;
                        SceneManager.SetActiveScene(scene);
                    }
                    SceneManager.sceneLoaded += Loaded;
                    AsyncOperation load;
                    try
                    {
                        load = SceneManager.LoadSceneAsync(route.ScenePath, LoadSceneMode.Additive);
                        while (!load.isDone) yield return null;
                    }
                    finally { SceneManager.sceneLoaded -= Loaded; }
                    if (!_worldScene.IsValid()) throw new InvalidOperationException("Benchmark scene did not load: " + route.map);
                    Map = null;
                    foreach (var root in _worldScene.GetRootGameObjects())
                        if (root.TryGetComponent<HplMap>(out var found)) { Map = found; break; }
                    if (Map == null || !Map.IsBenchmark) throw new InvalidOperationException("Benchmark map was not isolated: " + route.map);
                    yield return null; // player Start/Init before configuring its observer helpers
                    if (CancelRequested) break;
                    PrepareWorld();
                    ResetCounter();
                    // Render at the first key under the curtain while textures and temporal lighting settle.
                    double warmUntil = Time.realtimeSinceStartupAsDouble + 2;
                    while (Time.realtimeSinceStartupAsDouble < warmUntil && !CancelRequested) yield return null;
                    float fade = 0;
                    while (fade < 0.5f && !CancelRequested) { fade += Time.unscaledDeltaTime; _black = 1 - fade / 0.5f; yield return null; }
                    _black = 0; _sceneStartedAt = Time.realtimeSinceStartupAsDouble; _playing = true;
                    _skipFirstFrame = true; _nextMemoryAt = 0;
                    while (SceneTime < route.Duration && !CancelRequested) yield return null;
                    _playing = false;
                    fade = 0;
                    while (fade < 0.5f && !CancelRequested) { fade += Time.unscaledDeltaTime; _black = fade / 0.5f; yield return null; }
                    _black = 1;
                    if (SceneIndex == HplBenchmarkRoute.All.Length - 1)
                        _system = HplBenchmarkResults.CaptureSystem(_settings, _english, Map.PlayerCamera);
                    Map = null;
                    yield return SceneManager.UnloadSceneAsync(_worldScene);
                    _worldScene = default;
                    yield return SceneManager.UnloadSceneAsync(_scratchScene);
                    _scratchScene = default;
                    yield return Resources.UnloadUnusedAssets();
                }
                if (!CancelRequested && SceneIndex == HplBenchmarkRoute.All.Length)
                {
                    var assets = Resources.Load<HplMenuAssets>(_owner.Map.Campaign.ResourceName(HplMenuAssets.ResourceName));
                    _resultChoice = HplBenchmarkResults.Choice.None;
                    _text.Clear(); _holdBar.gameObject.SetActive(false); SetCounterActive(false);
                    _results = new HplBenchmarkResults(_canvas, assets != null ? assets.guiFont : _owner.Map.hud.defaultFont,
                        Stats, _settings, _english, _system);
                    Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                    // Do not let a key that ended the previous frame also dismiss this screen.
                    yield return null;
                    while (_resultChoice == HplBenchmarkResults.Choice.None) yield return null;
                    repeat = _resultChoice == HplBenchmarkResults.Choice.Repeat;
                    CloseResults(); _holdBar.gameObject.SetActive(true);
                    _hold = 0;
                }
            } while (repeat);
        }

        void PrepareWorld()
        {
            var p = Map.player;
            if (p == null || p.playerCamera == null) throw new InvalidOperationException("Benchmark camera is missing");
            p.readDevices = false; p.active = false; p.enabled = false;
            p.Body.SetBodyActive(false); p.Body.gravityActive = false;
            p.sanity = 100; p.sanityDrainDisabled = true;
            p.Flashback.Disabled = true;
            // No gameplay hands, free camera, HUD or light attached to the observer.
            foreach (var renderer in p.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var light in p.GetComponentsInChildren<Light>(true)) light.enabled = false;
            var fly = p.playerCamera.GetComponent<Dev.FlyCamera>();
            if (fly != null) fly.enabled = false;
            Map.hud.Canvas.enabled = false;
            _settings.Apply(Map); // copy of the options draft; never saved
            foreach (var enemy in Map.Enemies) enemy.SetActive(false);
            // SwingDoor is also used by stoves, cupboards and the machine hatch. Only passage
            // doors may be removed: hiding every SwingDoor leaves the stove's separate fire floating.
            var setup = new Setup { Map = Map };
            foreach (var entity in Map.Entities)
                if (entity is Props.PropSwingDoor door &&
                    (door.Component.file ?? "").Replace('\\', '/').StartsWith("entities/door/", StringComparison.OrdinalIgnoreCase))
                    door.SetActive(false);
            if (SceneIndex == 0)
            {
                Map.GetEntityByName("fountain_childsnake_blood_1")?.SetActive(false);
                Map.GetEntityByName("corpse_male_plague_fountain_1")?.SetActive(false);
            }
            if (SceneIndex == 1)
                (Map.GetEntityByName("candlestick01_1") as Props.PropLamp)?.SetLit(true, false);
            // Stage existing practical lights, keeping their configured intensity, flicker and audio.
            if (SceneIndex == 2 || SceneIndex == 3)
                foreach (var entity in Map.Entities)
                    if (entity is Props.PropLamp lamp &&
                        (lamp.Component.file ?? "").Replace('\\', '/').StartsWith("entities/lamp/", StringComparison.OrdinalIgnoreCase))
                        lamp.SetLit(true, false);
            if (SceneIndex == 3)
            {
                (Map.GetEntityByName("13_burner") as Props.PropLamp)?.SetLit(true, false);
                setup.Machine();
            }
            if (SceneIndex == 5)
            {
                _enemy = Map.GetEntityByName("servant_brute_1") as HplEnemy;
                if (_enemy == null) throw new InvalidOperationException("Benchmark brute is missing");
                _enemy.SetActive(true); _enemy.SetDisabled(true);
                _enemy.SetSanityDecreaseActive(true);
                _enemy.Body.gravityActive = false;
                _enemy.Body.SetBodyActive(false);
                if (!float.TryParse(_enemy.Component.GetVariable("Walk_ForwardSpeed"),
                        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float originalSpeed) || originalSpeed <= 0)
                    throw new InvalidOperationException("Benchmark brute walk speed is missing");
                _enemyWalkScale = (12f / 17f) / originalSpeed;
                _enemy.SetActive(false);
            }
            ApplyCamera();
        }

        void Update()
        {
            if (ShowingResults)
            {
                if (_resultChoice == HplBenchmarkResults.Choice.None) _resultChoice = _results.UpdateInput();
                return;
            }
            SampleFrame();
            // Real elapsed time: a slow frame must not stretch the camera route, and render motion
            // must remain smooth on displays faster than the 60 Hz world simulation.
            if (_playing) SceneTime = Mathf.Min((float)(Time.realtimeSinceStartupAsDouble - _sceneStartedAt),
                HplBenchmarkRoute.All[SceneIndex].Duration);
            bool held = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
            UpdateCancellation(held, Application.isFocused, Time.unscaledDeltaTime);
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) ToggleCounter();
            if (_playing && Time.realtimeSinceStartupAsDouble >= _nextCounterAt) UpdateCounter();
            DrawOverlay();
        }

        void DrawOverlay()
        {
            if (_curtain == null) return;
            bool inScene = Map != null && SceneIndex >= 0 && SceneIndex < HplBenchmarkRoute.All.Length;
            float black = Mathf.Clamp01(_black), overlay = inScene && _counterVisible ? 1 - black : 0;
            SetCounterActive(overlay > 0);
            if (overlay > 0)
            {
                // Text is rebuilt only for new values (4 Hz) or while the curtain fades.
                if (_counterDirty || Mathf.Abs(overlay - _counterDrawnAlpha) > 0.01f) { DrawCounter(overlay); _counterDirty = false; }
                LayoutProgress(overlay);
            }
            _curtain.color = new Color(0, 0, 0, black);
            _holdBar.rectTransform.anchoredPosition = new Vector2(-OverlayMargin, 24);
            _holdBar.rectTransform.sizeDelta = new Vector2(200 * Mathf.Clamp01(_hold / CancelHoldSeconds), 2);
            _text.Clear();
            float width = Width, fade = inScene ? CaptionFade(SceneTime) : 0;
            if (overlay * fade > 0)
            {
                Shadowed(_text, T("Сцена ", "Scene ") + (SceneIndex + 1) + T(" из ", " of ") + HplBenchmarkRoute.All.Length,
                    new Vector2(OverlayMargin, 600 - 66), 12, Alpha(Gold, overlay * fade));
                Shadowed(_text, HplBenchmarkResults.SceneTitle(SceneIndex, _english), new Vector2(OverlayMargin, 600 - 52), 20, Alpha(Color.white, overlay * fade));
            }
            float hint = Mathf.Max(black, fade, _hold > 0 ? 1 : 0);
            if (hint > 0)
                Shadowed(_text, T("Удерживайте Esc для отмены   Tab: показатели", "Hold Esc to cancel   Tab: stats"),
                    new Vector2(width - OverlayMargin, 600 - 40), 13, Alpha(Dim, hint), HplFontAlign.Right);
            if (_black > 0.95f)
                _text.DrawFont(CancelRequested ? T("Завершение теста...", "Finishing test...") : T("Тест производительности: загрузка...", "Performance test: loading..."),
                    new Vector2(width / 2, 285), Vector2.one * 20, Color.white, HplFontAlign.Center);
        }

        void SampleFrame()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            int sample = -1;
            if (_playing)
            {
                if (!_skipFirstFrame) sample = Stats.AddFrame(SceneIndex, now - _sceneStartedAt, now - _lastSampleAt);
                _skipFirstFrame = false;
                // A cheap OS query, still kept to 4 Hz: the peaks only need the plateaus.
                if (sample >= 0 && now >= _nextMemoryAt && HplVideoMemory.TryQuery(out var memory))
                {
                    _nextMemoryAt = now + 0.25;
                    Stats.SetMemory(sample, memory.localUsage / 1048576.0, memory.sharedUsage / 1048576.0, memory.localBudget / 1048576.0);
                }
            }
            _lastSampleAt = now;
            // The elapsed interval belongs to the previous rendered frame. Unity returns CPU/GPU
            // timings four frames late. Keep the association through fades, excluding all loading samples.
            int renderedFrame = Time.frameCount - 1, slot = renderedFrame % _timingFrames.Length;
            _timingFrames[slot] = renderedFrame; _timingSamples[slot] = sample;
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _timings) == 0) return;
            var timing = _timings[0];
            if (timing.frameStartTimestamp == 0 || timing.frameStartTimestamp == _lastTimingTimestamp) return;
            _lastTimingTimestamp = timing.frameStartTimestamp;
            int frame = Time.frameCount - 4;
            if (frame < 0) return;
            slot = frame % _timingFrames.Length;
            if (_timingFrames[slot] == frame)
                Stats.SetTiming(_timingSamples[slot], timing.cpuMainThreadFrameTime, timing.cpuRenderThreadFrameTime, timing.gpuFrameTime);
        }

        internal void ToggleCounter() { _counterVisible = !_counterVisible; SetCounterActive(_playing && _counterVisible); }
        internal void ChooseResult(bool repeat) => _resultChoice = repeat ? HplBenchmarkResults.Choice.Repeat : HplBenchmarkResults.Choice.Back;
        void CloseResults() { _results?.Dispose(); _results = null; }

        void SetCounterActive(bool visible)
        {
            if (_counter == null || _counter.gameObject.activeSelf == visible) return;
            _counter.gameObject.SetActive(visible);
            foreach (var image in _segmentBack) image.gameObject.SetActive(visible);
            foreach (var image in _segmentFill) image.gameObject.SetActive(visible);
            _counterDirty = true;
        }

        void ResetCounter() { _fpsText = "--"; _detailText = ""; _counterDirty = true; _nextCounterAt = 0; }

        void UpdateCounter()
        {
            _nextCounterAt = Time.realtimeSinceStartupAsDouble + 0.25;
            if (!_counterVisible) return;
            int n = 0, mainN = 0, renderN = 0, gpuN = 0;
            double seconds = 0, main = 0, render = 0, gpu = 0;
            for (int i = Stats.Count - 1; i >= 0; i--)
            {
                var f = Stats[i];
                if (f.scene != SceneIndex || seconds >= 0.5) break;
                n++; seconds += f.seconds;
                if (f.cpuMainMs > 0) { main += f.cpuMainMs; mainN++; }
                if (f.cpuRenderMs > 0) { render += f.cpuRenderMs; renderN++; }
                if (f.gpuMs > 0) { gpu += f.gpuMs; gpuN++; }
            }
            if (n == 0) return;
            _fpsText = Mathf.RoundToInt((float)(n / seconds)).ToString();
            // CPU is the slower of the main and render threads, as in the results bottleneck.
            _detailText = (seconds * 1000 / n).ToString("0.0") + T(" мс", " ms");
            if (mainN > 0 || renderN > 0)
                _detailText += "   CPU " + Math.Max(mainN > 0 ? main / mainN : 0, renderN > 0 ? render / renderN : 0).ToString("0.0");
            if (gpuN > 0) _detailText += "   GPU " + (gpu / gpuN).ToString("0.0");
            _counterDirty = true;
        }

        internal void UpdateCancellation(bool held, bool focused, float deltaTime)
        {
            _hold = held && focused ? _hold + deltaTime : 0;
            if (_hold >= CancelHoldSeconds) CancelRequested = true;
        }

        void FixedUpdate()
        {
            if (!_playing || Map == null) return;
            if (_enemy == null) return;
            bool visible = SceneTime >= 8 && SceneTime < 28;
            if (_enemy.Active != visible)
            {
                _enemy.SetActive(visible);
                _enemy.Body.SetBodyActive(false);
                if (visible) _enemy.PlayAnim("Walk", true, 0, false, _enemyWalkScale);
            }
            // Disabled AI keeps the same walk and animation events; the camera cannot trigger a hunt.
            _enemy.Body.SetFeetPosition(HplSpace.Position(EnemyFeet(SceneTime)));
            _enemy.SetHplYaw(Mathf.PI);
            _enemy.StepBody(0); // updates the model pose without a character physics step
            bool fear = SceneTime >= 10 && SceneTime < 24;
            if (fear) Map.player.AddTerrorEnemy(_enemy); else Map.player.RemoveTerrorEnemy(_enemy);
            if (!_enemyVoice && SceneTime >= 8)
            {
                _enemyVoice = true;
                _enemy.PlaySound(_enemy.Component.GetVariable("AmbientSound_Alert"));
            }
        }

        void LateUpdate()
        {
            if (_restored) return;
            if (ShowingResults) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return; }
            // Player.OnDisable and scene activation can release the cursor after the initial lock.
            // Own it throughout the tour and loading transitions, until Restore returns it to the menu.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ApplyCamera();
        }

        /// <summary>Straight central aisle, clear of both the broken pillar and the intact pillar row.</summary>
        public static Vector3 EnemyFeet(float time) => new(0, 0, Mathf.Lerp(11, 23, Mathf.Clamp01((time - 8) / 17)));

        void ApplyCamera()
        {
            if (Map == null || Map.player == null || SceneIndex < 0 || SceneIndex >= HplBenchmarkRoute.All.Length) return;
            HplBenchmarkRoute.All[SceneIndex].Evaluate(SceneTime, out var eye, out var target);
            var camera = Map.player.playerCamera;
            if (camera == null) return;
            Vector3 position = HplSpace.Position(eye), direction = HplSpace.Direction(target - eye);
            // The camera is a child of the body. Move its parent FIRST so a teleport/turn cannot
            // displace the camera a second time after its world pose has been assigned.
            Map.player.Body.SetFeetPosition(position - Vector3.up * 1.6f);
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(75, camera.aspect);
            Map.sound.listener = camera.transform;
        }

        void CreateOverlay()
        {
            var go = new GameObject("Benchmark overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 32000;
            var scale = go.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(800, 600); scale.matchWidthOrHeight = 1;
            RawImage Image(string name, Vector2 size, Vector2 pos)
            {
                var child = new GameObject(name, typeof(RectTransform), typeof(RawImage));
                child.transform.SetParent(go.transform, false);
                var image = child.GetComponent<RawImage>(); image.texture = Texture2D.whiteTexture; image.raycastTarget = false;
                image.rectTransform.sizeDelta = size; image.rectTransform.anchoredPosition = pos;
                return image;
            }
            _curtain = Image("Loading curtain", Vector2.zero, Vector2.zero);
            _curtain.rectTransform.anchorMin = Vector2.zero; _curtain.rectTransform.anchorMax = Vector2.one;
            _curtain.rectTransform.offsetMin = _curtain.rectTransform.offsetMax = Vector2.zero;
            _curtain.color = Color.black;
            // Info sits on the real screen corners (full canvas width), not inside the centred 4:3 area.
            int scenes = HplBenchmarkRoute.All.Length;
            _segmentBack = new RawImage[scenes]; _segmentFill = new RawImage[scenes];
            for (int i = 0; i < scenes; i++)
            {
                _segmentBack[i] = Corner(Image("Tour segment " + i, Vector2.zero, Vector2.zero), Vector2.zero);
                _segmentFill[i] = Corner(Image("Tour segment fill " + i, Vector2.zero, Vector2.zero), Vector2.zero);
            }
            _counter = MakeText("Benchmark counter");
            SetCounterActive(false);
            _holdBar = Corner(Image("Cancel hold", Vector2.zero, Vector2.zero), new Vector2(1, 0));
            _holdBar.color = Gold;
            _text = MakeText("Benchmark instructions");

            static RawImage Corner(RawImage image, Vector2 corner)
            {
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = corner;
                return image;
            }
            HplGuiText MakeText(string name)
            {
                var child = new GameObject(name, typeof(RectTransform), typeof(HplGuiText));
                child.transform.SetParent(go.transform, false);
                var rect = child.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                var text = child.GetComponent<HplGuiText>(); text.raycastTarget = false; text.font = _owner.Map.hud.defaultFont;
                return text;
            }
        }

        float Width => ((RectTransform)_canvas.transform).rect.width;

        /// <summary>Scene caption and key hint: fade in after the curtain opens, fade out after five seconds.</summary>
        static float CaptionFade(float time) => Mathf.Clamp01(time / 0.6f) * Mathf.Clamp01((5 - time) / 0.8f);

        static void Shadowed(HplGuiText text, string value, Vector2 position, float size, Color color, HplFontAlign align = HplFontAlign.Left)
        {
            text.DrawFont(value, position + Vector2.one, Vector2.one * size, new Color(0, 0, 0, color.a * 0.75f), align);
            text.DrawFont(value, position, Vector2.one * size, color, align);
        }

        static Color Alpha(Color color, float alpha) { color.a *= alpha; return color; }

        void DrawCounter(float alpha)
        {
            _counter.Clear();
            Shadowed(_counter, _fpsText, new Vector2(OverlayMargin, 12), 28, Alpha(Color.white, alpha));
            float x = OverlayMargin + _counter.font.GetLength(Vector2.one * 28, _fpsText) + 5;
            Shadowed(_counter, "FPS", new Vector2(x, 25), 12, Alpha(Gold, alpha));
            if (_detailText.Length > 0) Shadowed(_counter, _detailText, new Vector2(OverlayMargin, 45), 12, Alpha(Dim, alpha));
            _counterDrawnAlpha = alpha;
        }

        void LayoutProgress(float alpha)
        {
            const float gap = 4, height = 2, bottom = 10;
            var routes = HplBenchmarkRoute.All;
            float total = 0;
            foreach (var route in routes) total += route.Duration;
            float x = OverlayMargin, span = Width - 2 * OverlayMargin - gap * (routes.Length - 1);
            for (int i = 0; i < routes.Length; i++)
            {
                float w = span * routes[i].Duration / total;
                _segmentBack[i].rectTransform.anchoredPosition = _segmentFill[i].rectTransform.anchoredPosition = new Vector2(x, bottom);
                _segmentBack[i].rectTransform.sizeDelta = new Vector2(w, height);
                _segmentBack[i].color = i < SceneIndex ? Alpha(Gold, 0.55f * alpha) : new Color(1, 1, 1, 0.18f * alpha);
                float fill = i == SceneIndex ? w * Mathf.Clamp01(SceneTime / routes[i].Duration) : 0;
                _segmentFill[i].rectTransform.sizeDelta = new Vector2(fill, height);
                _segmentFill[i].color = Alpha(Gold, 0.9f * alpha);
                x += w + gap;
            }
        }

        void Restore()
        {
            if (_restored) return;
            _restored = true;
            if (_originalScene.IsValid() && _originalScene.isLoaded) SceneManager.SetActiveScene(_originalScene);
            foreach (var root in _suspended) if (root != null) root.SetActive(true);
            // Deactivating a scene stops Unity AudioSources. Recreate their native pause/play positions;
            // HPL entries and clocks themselves were kept in memory and never stepped.
            foreach (var item in _audio)
                if (item.source != null && item.source.clip != null)
                {
                    item.source.Play(); item.source.timeSamples = item.sample;
                    if (!item.playing || _soundPaused) item.source.Pause();
                }
            if (_owner != null)
            {
                _owner.Map.sound.SetMenuPaused(_soundPaused);
                _owner.Settings.Apply(_owner.Map);
                // Enhanced lighting owns a single current-map overlay. Loading a temporary map
                // detached the suspended one; reattach it before returning to its camera.
                if (!_owner.Map.menuBackground) HplEnhancedLighting.Attach(_owner.Map);
                _owner.Map.SetFog();
                _owner.Map.SetSkyBox();
                HplKeyBindings.SetDefaultsConfig(_owner.Map.keysConfig);
            }
            if (_hplRandom != null) HplMath.ExchangeRandom(_hplRandom);
            UnityEngine.Random.state = _random;
            Time.timeScale = _timeScale;
            Cursor.lockState = _cursorLock; Cursor.visible = _cursorVisible;
            s_loadingPath = null; s_instance = null;
            if (_owner != null && Error != null) _owner.ReportBenchmarkError(Error);
        }

        void OnDestroy()
        {
            CloseResults();
            if (_restored || s_instance != this) return;
            // Also cover a component destroyed externally (normal completion restores in Drive).
            Restore();
            if (_worldScene.IsValid() && _worldScene.isLoaded) SceneManager.UnloadSceneAsync(_worldScene);
            if (_scratchScene.IsValid() && _scratchScene.isLoaded) SceneManager.UnloadSceneAsync(_scratchScene);
        }
    }
}
