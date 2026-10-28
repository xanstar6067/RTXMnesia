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
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
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
                // Render at the first key under the curtain while textures and temporal lighting settle.
                double warmUntil = Time.realtimeSinceStartupAsDouble + 2;
                while (Time.realtimeSinceStartupAsDouble < warmUntil && !CancelRequested) yield return null;
                float fade = 0;
                while (fade < 0.5f && !CancelRequested) { fade += Time.unscaledDeltaTime; _black = 1 - fade / 0.5f; yield return null; }
                _black = 0; _sceneStartedAt = Time.realtimeSinceStartupAsDouble; _playing = true;
                while (SceneTime < route.Duration && !CancelRequested) yield return null;
                _playing = false;
                fade = 0;
                while (fade < 0.5f && !CancelRequested) { fade += Time.unscaledDeltaTime; _black = fade / 0.5f; yield return null; }
                _black = 1;
                Map = null;
                yield return SceneManager.UnloadSceneAsync(_worldScene);
                _worldScene = default;
                yield return SceneManager.UnloadSceneAsync(_scratchScene);
                _scratchScene = default;
                yield return Resources.UnloadUnusedAssets();
            }
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
            // Real elapsed time: a slow frame must not stretch the camera route, and render motion
            // must remain smooth on displays faster than the 60 Hz world simulation.
            if (_playing) SceneTime = Mathf.Min((float)(Time.realtimeSinceStartupAsDouble - _sceneStartedAt),
                HplBenchmarkRoute.All[SceneIndex].Duration);
            bool held = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
            UpdateCancellation(held, Application.isFocused, Time.unscaledDeltaTime);
            if (_curtain == null) return;
            _curtain.color = new Color(0, 0, 0, Mathf.Clamp01(_black));
            _holdBar.rectTransform.sizeDelta = new Vector2(360 * Mathf.Clamp01(_hold / CancelHoldSeconds), 3);
            _text.Clear();
            _text.DrawFont("Удерживайте Esc для отмены", new Vector2(400, 565), Vector2.one * 16, Color.white, HplFontAlign.Center);
            if (_black > 0.95f)
                _text.DrawFont(CancelRequested ? "Завершение теста..." : "Тест производительности: загрузка...",
                    new Vector2(400, 285), Vector2.one * 20, Color.white, HplFontAlign.Center);
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
            float u = Mathf.Clamp01((SceneTime - 8) / 17);
            _enemy.Body.SetFeetPosition(HplSpace.Position(new Vector3(4.5f, 0, Mathf.Lerp(11, 23, u))));
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

        void LateUpdate() => ApplyCamera();

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
            _holdBar = Image("Cancel hold", new Vector2(0, 3), new Vector2(-180, -281));
            _holdBar.rectTransform.pivot = new Vector2(0, 0.5f);
            var text = new GameObject("Benchmark instructions", typeof(RectTransform), typeof(HplGuiText));
            text.transform.SetParent(go.transform, false);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(800, 600);
            _text = text.GetComponent<HplGuiText>(); _text.raycastTarget = false; _text.font = _owner.Map.hud.defaultFont;
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
            if (_restored || s_instance != this) return;
            // Also cover a component destroyed externally (normal completion restores in Drive).
            Restore();
            if (_worldScene.IsValid() && _worldScene.isLoaded) SceneManager.UnloadSceneAsync(_worldScene);
            if (_scratchScene.IsValid() && _scratchScene.isLoaded) SceneManager.UnloadSceneAsync(_scratchScene);
        }
    }
}
