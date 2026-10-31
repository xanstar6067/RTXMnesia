using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// A bug report (F1, own feature): the state of the frame of the key — map, player, camera, what the camera looks
    /// at, enemies, graphics, script variables, a save, the screenshot and the recent log — is captured at once; the
    /// comment is asked for afterwards and <see cref="Write"/> puts everything into BugReports next to the game.
    /// </summary>
    public sealed class HplBugReport
    {
        public const string FolderName = "BugReports";
        const int LogLines = 200, MaxVars = 2000;
        const float LookDistance = 100f;

        public Texture2D Screenshot { get; private set; }
        /// <summary>The end-of-frame capture has finished (with or without a screenshot).</summary>
        public bool Ready { get; private set; }

        readonly DateTime _time = DateTime.Now;
        readonly string _mapName;
        readonly string _report;
        readonly string _saveJson;
        readonly string[] _log;

        HplBugReport(HplMap map)
        {
            _mapName = map != null && !string.IsNullOrEmpty(map.mapFile) ? map.MapName : "no_map";
            _report = Describe(map);
            _saveJson = Save.HplSaveHandler.CreateJson(map);
            _log = HplLogBuffer.Snapshot(LogLines);
        }

        /// <summary>Captures the state now and the screenshot at the end of this frame (before any report window is drawn).</summary>
        public static HplBugReport Begin(MonoBehaviour owner, HplMap map)
        {
            var report = new HplBugReport(map);
            if (Application.isBatchMode && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) report.Ready = true;
            else owner.StartCoroutine(report.Capture());
            return report;
        }

        IEnumerator Capture()
        {
            // Batch runs never reach WaitForEndOfFrame: the frame goes to a file there (written at the end of this
            // frame too), at most a second of waiting; a report never stays pending without its screenshot.
            if (Application.isBatchMode)
            {
                string file = System.IO.Path.Combine(Application.temporaryCachePath, "bug_report_capture.png");
                try { if (File.Exists(file)) File.Delete(file); ScreenCapture.CaptureScreenshot(file); }
                catch (Exception e) { Debug.LogWarning("[RTXMnesia] Bug report: no screenshot: " + e.Message); }
                double deadline = Time.realtimeSinceStartupAsDouble + 1;
                while (!File.Exists(file) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                yield return null;
                try
                {
                    if (File.Exists(file))
                    {
                        var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                        if (texture.LoadImage(File.ReadAllBytes(file))) Screenshot = texture;
                        else UnityEngine.Object.Destroy(texture);
                        File.Delete(file);
                    }
                }
                catch (Exception e) { Debug.LogWarning("[RTXMnesia] Bug report: no screenshot: " + e.Message); }
                Ready = true;
                yield break;
            }
            yield return new WaitForEndOfFrame();
            try { Screenshot = ScreenCapture.CaptureScreenshotAsTexture(); }
            catch (Exception e) { Debug.LogWarning("[RTXMnesia] Bug report: no screenshot: " + e.Message); }
            Ready = true;
        }

        public void Dispose()
        {
            if (Screenshot != null) UnityEngine.Object.Destroy(Screenshot);
            Screenshot = null;
        }

        // ------------------------------------------------------------------ files

        /// <summary>BugReports next to the game (the folder of the executable; the project folder in the editor).</summary>
        public static string Root => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Application.dataPath)) ?? ".", FolderName);
        /// <summary>Tests: another folder instead of <see cref="Root"/>.</summary>
        public static string RootOverride;

        /// <summary>
        /// Writes report.txt, screen.jpg, save.sav and log.txt into a new folder of the report and returns it. A game
        /// folder without write access falls back to the user's application data (the path is returned either way).
        /// </summary>
        public string Write(string comment)
        {
            string name = _time.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + "_" + SafeName(_mapName);
            try { return WriteTo(RootOverride ?? Root, name, comment); }
            catch (Exception e) when (RootOverride == null && (e is UnauthorizedAccessException || e is IOException))
            {
                Debug.LogWarning($"[RTXMnesia] Bug report: cannot write next to the game ({e.Message}), using the user data folder.");
                return WriteTo(Path.Combine(Application.persistentDataPath, FolderName), name, comment);
            }
        }

        string WriteTo(string root, string name, string comment)
        {
            string folder = Path.Combine(root, name);
            for (int i = 2; Directory.Exists(folder); i++) folder = Path.Combine(root, name + "_" + i);
            Directory.CreateDirectory(folder);
            var utf8 = new UTF8Encoding(false);
            var text = new StringBuilder();
            text.Append("Комментарий: ").AppendLine(comment.Trim());
            text.AppendLine();
            text.Append(_report);
            text.AppendLine();
            text.AppendLine("[Файлы]");
            if (Screenshot != null)
            {
                File.WriteAllBytes(Path.Combine(folder, "screen.jpg"), Screenshot.EncodeToJPG(95));
                text.AppendLine("Снимок: screen.jpg" + (HplHdr.Active ? " (HDR-вывод: цвета снимка могут отличаться от экрана)" : ""));
            }
            else text.AppendLine("Снимок: нет");
            if (_saveJson != null)
            {
                Save.HplSaveHandler.WriteFile(Path.Combine(folder, "save" + Save.HplSaveHandler.Extension), _saveJson);
                text.AppendLine("Сохранение: save" + Save.HplSaveHandler.Extension);
            }
            else text.AppendLine("Сохранение: нет (здесь игра не сохраняется)");
            File.WriteAllLines(Path.Combine(folder, "log.txt"), _log, utf8);
            text.AppendLine("Лог: log.txt (последние " + _log.Length + " сообщений)");
            File.WriteAllText(Path.Combine(folder, "report.txt"), text.ToString(), utf8);
            Debug.Log("[RTXMnesia] Bug report saved: " + folder);
            return folder;
        }

        static string SafeName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name) sb.Append(Array.IndexOf(invalid, c) >= 0 || c == ' ' ? '_' : c);
            return sb.ToString();
        }

        // ------------------------------------------------------------------ the report text

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string V(Vector3 v) => F(v.x) + " " + F(v.y) + " " + F(v.z);
        static string YesNo(bool v) => v ? "да" : "нет";

        static string Describe(HplMap map)
        {
            var sb = new StringBuilder();
            void Line(string key, object value) => sb.Append(key).Append(": ").AppendLine(Convert.ToString(value, CultureInfo.InvariantCulture));

            Line("Время", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            Line("Сборка", HplBuildInfo.Describe());
            Line("Unity", Application.unityVersion + (Application.isEditor ? " (редактор)" : ""));

            sb.AppendLine().AppendLine("[Карта]");
            if (map == null) Line("Карта", "нет");
            else
            {
                Line("Кампания", map.Campaign.IsJustine ? "Justine" : "основная");
                Line("Карта", map.MapName);
                Line("Файл карты", map.mapFile);
                Line("Сцена", map.gameObject.scene.path);
                Line("Главное меню", YesNo(map.menuBackground || (map.GameUi != null && map.GameUi.IsTitleScreen)));
                Line("Время на карте, с", F(map.GameTime));
                Line("Сложный режим", YesNo(HplDifficulty.HardMode));
                Line("Профиль", Save.HplProfiles.Current ?? "нет");
                Line("Язык", map.language != null ? map.language.language : "нет");
                Line("Смена карты", YesNo(map.IsChangingMap));
                string screen = map.GameUi == null ? "нет" : map.GameUi.JournalOpen ? "журнал" : map.GameUi.InventoryOpen ? "инвентарь"
                    : map.GameUi.IsPaused && !map.GameUi.IsTitleScreen ? "меню паузы" : "игра";
                Line("Экран", screen);
            }

            var player = map != null ? map.player : null;
            if (player != null) Player(sb, Line, player);
            else
            {
                var camera = map != null ? map.PlayerCamera : Camera.main;
                if (camera != null)
                {
                    sb.AppendLine().AppendLine("[Камера]");
                    Line("Камера Unity", V(camera.transform.position));
                    Line("Камера HPL", V(HplSpace.Position(camera.transform.position)));
                    Line("Направление HPL", V(HplSpace.Direction(camera.transform.forward)));
                }
            }

            var view = map != null ? map.PlayerCamera : Camera.main;
            if (view != null) LookAt(sb, Line, map, player, view);
            if (map != null) Enemies(sb, map, player);
            Graphics(sb, Line, map);
            if (map != null) Variables(sb, "[Локальные переменные карты]", map.Vars);
            Variables(sb, "[Глобальные переменные]", HplGame.GlobalVars);
            return sb.ToString();
        }

        static void Player(StringBuilder sb, Action<string, object> Line, AmnesiaPlayer player)
        {
            sb.AppendLine().AppendLine("[Игрок]");
            Vector3 feet = player.Body.FeetPosition;
            Line("Позиция HPL (ноги)", V(HplSpace.Position(feet)));
            Line("Позиция Unity (ноги)", V(feet));
            Line("Камера HPL", V(player.HplCameraPosition));
            Line("Камера Unity", V(player.LogicCameraPosition));
            Vector3 angles = player.HplCameraAngles * Mathf.Rad2Deg;
            Line("Углы камеры HPL, град (тангаж рыскание крен)", V(angles));
            Line("Направление HPL", V(player.HplCameraForward));
            Line("Направление Unity", V(player.LogicCameraRotation * Vector3.forward));
            Line("Состояние", player.States != null && player.States.Current != null ? player.States.CurrentType.ToString() : "нет");
            Line("Присел", YesNo(player.MoveState != null && player.MoveState.Crouching));
            Line("В воде", YesNo(player.IsInWater));
            Line("Здоровье", F(player.health));
            Line("Рассудок", F(player.sanity));
            Line("Масло", F(player.lampOil));
            Line("Трутницы", player.tinderboxes);
            Line("Фонарь", YesNo(player.lanternActive) + (player.lanternDisabled ? " (запрещён)" : ""));
            Line("Управление игрока", YesNo(player.readDevices));
            Line("В фокусе", player.EntityInFocus != null ? player.EntityInFocus.Name + " (" + F(player.FocusDistance) + " м)" : "нет");
        }

        /// <summary>The closest solid surface along the camera ray (the player's character body excluded).</summary>
        static void LookAt(StringBuilder sb, Action<string, object> Line, HplMap map, AmnesiaPlayer player, Camera camera)
        {
            sb.AppendLine().AppendLine("[Взгляд]");
            Vector3 start = player != null ? player.LogicCameraPosition : camera.transform.position;
            Vector3 dir = player != null ? player.LogicCameraRotation * Vector3.forward : camera.transform.forward;
            int mask = ~(1 << HplPhysics.CharacterLayer);
            if (!Physics.Raycast(start, dir, out var hit, LookDistance, mask, QueryTriggerInteraction.Ignore))
            {
                Line("Луч", "ничего ближе " + F(LookDistance) + " м");
                return;
            }
            var body = map != null ? map.PhysicsWorld?.BodyOf(hit.collider) : null;
            Line("Объект", HierarchyPath(hit.collider.transform));
            Line("Сущность", body?.UserData is HplEntity entity ? entity.Name + " (" + entity.Type + ")" : "нет");
            Line("Расстояние, м", F(hit.distance));
            Line("Точка HPL", V(HplSpace.Position(hit.point)));
            Line("Точка Unity", V(hit.point));
            Line("Нормаль HPL", V(HplSpace.Direction(hit.normal)));
            var renderer = hit.collider.GetComponentInParent<Renderer>();
            if (renderer == null && body?.UserData is HplEntity owner) renderer = owner.GameObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Line("Модель", HierarchyPath(renderer.transform));
                if (renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null) Line("Меш", filter.sharedMesh.name);
                Line("Материалы", string.Join(", ", renderer.sharedMaterials.Where(m => m != null).Select(m => m.name)));
            }
        }

        static void Enemies(StringBuilder sb, HplMap map, AmnesiaPlayer player)
        {
            if (map.Enemies == null || map.Enemies.Count == 0) return;
            sb.AppendLine().AppendLine("[Враги]");
            foreach (var enemy in map.Enemies)
            {
                if (enemy == null) continue;
                Vector3 position = enemy.AttachTransform != null ? enemy.AttachTransform.position : enemy.GameObject.transform.position;
                string distance = player != null ? ", " + F(Vector3.Distance(position, player.Body.FeetPosition)) + " м" : "";
                sb.Append(enemy.Name).Append(" (").Append(enemy.FileName).Append("): ")
                  .Append(enemy.Active ? enemy.CurrentStateName : "неактивен").Append(", HPL ").Append(V(HplSpace.Position(position)))
                  .AppendLine(distance);
            }
        }

        static void Graphics(StringBuilder sb, Action<string, object> Line, HplMap map)
        {
            sb.AppendLine().AppendLine("[Графика]");
            var settings = map != null && map.GameUi != null ? map.GameUi.Settings : null;
            if (settings != null)
                foreach (var line in HplBenchmarkResults.CaptureSystem(settings, false, map.PlayerCamera)) sb.AppendLine(line);
            else Line("Видеокарта", SystemInfo.graphicsDeviceName + " | " + SystemInfo.graphicsDeviceVersion);
            Line("Экран", Screen.width + " x " + Screen.height + ", " + Screen.fullScreenMode);
            Line("HDR-вывод", YesNo(HplHdr.Active));
            Line("FPS (среднее за 1 с)", HplLogBuffer.Fps.ToString("0.0", CultureInfo.InvariantCulture));
            Line("Видеопамять, МБ", SystemInfo.graphicsMemorySize);
        }

        static void Variables(StringBuilder sb, string title, IReadOnlyDictionary<string, string> vars)
        {
            if (vars == null || vars.Count == 0) return;
            sb.AppendLine().AppendLine(title);
            int n = 0;
            foreach (var pair in vars.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (n++ >= MaxVars) { sb.AppendLine("... ещё " + (vars.Count - MaxVars)); break; }
                sb.Append(pair.Key).Append(" = ").AppendLine(pair.Value);
            }
        }

        static string HierarchyPath(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }

    /// <summary>
    /// The recent log messages and the frame rate for <see cref="HplBugReport"/>: listens from the start of the
    /// application (messages of any thread), keeps the last few hundred.
    /// </summary>
    public static class HplLogBuffer
    {
        const int Capacity = 400, StackLines = 6;
        static readonly Queue<string> s_lines = new();
        static readonly object s_lock = new();
        static double s_frameTime = 1.0 / 60;

        /// <summary>Frames per second, an exponential average over about a second of real time.</summary>
        public static double Fps => s_frameTime > 0 ? 1.0 / s_frameTime : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            lock (s_lock) s_lines.Clear();
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
        }

        /// <summary>Called once per frame (HplGameUi.Update).</summary>
        public static void TrackFrame(float unscaledDelta)
        {
            if (unscaledDelta <= 0) return;
            double k = Math.Min(1.0, unscaledDelta);
            s_frameTime += (unscaledDelta - s_frameTime) * k;
        }

        static void OnLog(string message, string stackTrace, LogType type)
        {
            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)).Append(' ');
            sb.Append(type switch { LogType.Error => "[E] ", LogType.Exception => "[X] ", LogType.Assert => "[A] ", LogType.Warning => "[W] ", _ => "" });
            sb.Append(message);
            if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) && !string.IsNullOrEmpty(stackTrace))
                foreach (var line in stackTrace.Split('\n').Where(l => l.Trim().Length > 0).Take(StackLines))
                    sb.Append("\n    ").Append(line.TrimEnd());
            lock (s_lock)
            {
                s_lines.Enqueue(sb.ToString());
                while (s_lines.Count > Capacity) s_lines.Dequeue();
            }
        }

        public static string[] Snapshot(int count)
        {
            lock (s_lock) return s_lines.Skip(Math.Max(0, s_lines.Count - count)).ToArray();
        }
    }

    /// <summary>
    /// The commit of this build for the reports: Resources/HplBuildInfo (written by the editor before a player build,
    /// not committed); in the editor the repository is read directly.
    /// </summary>
    public static class HplBuildInfo
    {
        public const string ResourceName = "HplBuildInfo";
        static string s_cached;

        public static string Describe()
        {
            if (s_cached != null) return s_cached;
            if (Application.isEditor) return s_cached = EditorCommit() ?? "неизвестно";
            var asset = Resources.Load<TextAsset>(ResourceName);
            return s_cached = asset != null ? asset.text.Trim() : "неизвестно";
        }

        /// <summary>The checked-out commit from .git (HEAD → its ref → packed-refs), without running git.</summary>
        static string EditorCommit()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
                string git = Path.Combine(dir, ".git");
                if (!Directory.Exists(git)) return null;
                string head = File.ReadAllText(Path.Combine(git, "HEAD")).Trim();
                if (!head.StartsWith("ref: ", StringComparison.Ordinal)) return Short(head) + " (редактор)";
                string reference = head.Substring(5);
                string file = Path.Combine(git, reference.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(file)) return Short(File.ReadAllText(file).Trim()) + " " + Path.GetFileName(reference) + " (редактор)";
                string packed = Path.Combine(git, "packed-refs");
                if (File.Exists(packed))
                    foreach (var line in File.ReadAllLines(packed))
                        if (line.EndsWith(" " + reference, StringComparison.Ordinal)) return Short(line.Split(' ')[0]) + " (редактор)";
            }
            catch (Exception) { }
            return null;
        }

        static string Short(string hash) => hash.Length > 10 ? hash.Substring(0, 10) : hash;
    }
}
