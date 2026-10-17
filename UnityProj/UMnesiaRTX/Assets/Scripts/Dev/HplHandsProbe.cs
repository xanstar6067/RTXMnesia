#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Editor-only Play check of the hands animations (RTXMnesia.EditorDev.HandsPreview): the lantern is drawn,
    /// held, holstered, drawn again and switched during the holster; every frame the hand bone pose relative to the
    /// camera, the hands state and the animation weights are logged (Logs/HandsCheck/hands.csv). A frame whose bone
    /// jump is far above its neighbours' is a pop of the animation.
    /// </summary>
    public sealed class HplHandsProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.HandsPreview.ExitCode";
        public string folder;

        readonly StringBuilder _csv = new();
        readonly List<(float time, string phase, Vector3 pos, float angle)> _samples = new();
        Quaternion _previousRotation;

        IEnumerator Start()
        {
            var run = Run();
            while (true)
            {
                bool more = false, failed = false; object current = null;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception exception) { Debug.LogException(exception); failed = true; }
                if (failed) { Finish(1); yield break; }
                if (!more) break;
                yield return current;
            }
        }

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1f);
            var map = HplMap.Current;
            if (map == null || map.player == null) throw new InvalidOperationException("no map");
            var player = map.player;
            player.readDevices = false;
            if (map.GameUi != null && map.GameUi.IsPaused) map.GameUi.Resume();
            map.effects.Fade.FadeIn(0f);
            player.active = true;
            HplGame.AddItem(map, "lantern", "Lantern", "Lantern", "", 1f, "", "", out _);
            player.SetLampOil(100f);
            yield return new WaitForSeconds(0.5f);

            var hands = player.Hands;
            var camera = map.PlayerCamera.transform;
            _csv.AppendLine("time;phase;state;anim;bone_x;bone_y;bone_z;bone_rot_step_deg;active_states");
            string phase = "";
            IEnumerator Record(float seconds)
            {
                float end = Time.time + seconds;
                while (Time.time < end)
                {
                    yield return null;
                    var model = hands.HandsModel;
                    var bone = model != null ? FindDeep(model, "attachpoint") ?? model : null;
                    if (bone == null || !model.gameObject.activeInHierarchy) continue;
                    Vector3 local = camera.InverseTransformPoint(bone.position);
                    Quaternion rotation = Quaternion.Inverse(camera.rotation) * bone.rotation;
                    float angle = _samples.Count > 0 ? Quaternion.Angle(_previousRotation, rotation) : 0f;
                    _previousRotation = rotation;
                    _samples.Add((Time.time, phase, local, angle));
                    var states = new List<string>();
                    if (hands.HandsAnimation != null)
                        foreach (AnimationState s in hands.HandsAnimation)
                            if (s.enabled || s.weight > 0f) states.Add($"{s.name}:w{s.weight:0.00}:t{s.time:0.00}/{s.length:0.00}");
                    _csv.AppendLine(string.Join(";", Time.time.ToString("0.000", CultureInfo.InvariantCulture), phase, hands.State, hands.CurrentAnimation,
                        local.x.ToString("0.0000", CultureInfo.InvariantCulture), local.y.ToString("0.0000", CultureInfo.InvariantCulture),
                        local.z.ToString("0.0000", CultureInfo.InvariantCulture), angle.ToString("0.00", CultureInfo.InvariantCulture), string.Join(" ", states)));
                }
            }

            phase = "draw1"; player.Lantern.SetActive(true, true);
            yield return Record(2.5f);
            phase = "holster1"; player.Lantern.SetActive(false, true);
            yield return Record(2f);
            phase = "draw2"; player.Lantern.SetActive(true, true);
            yield return Record(2.5f);
            phase = "holster2"; player.Lantern.SetActive(false, true);
            yield return Record(0.3f);
            phase = "redraw"; player.Lantern.SetActive(true, true);
            yield return Record(2.5f);

            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "hands.csv"), _csv.ToString());

            // A pop: a frame's position step more than 4× the median of the 6 frames around it and above 1.5 cm.
            int pops = 0;
            var report = new StringBuilder();
            for (int i = 1; i < _samples.Count; i++)
            {
                if (_samples[i].phase != _samples[i - 1].phase && i > 1) { }
                float step = (_samples[i].pos - _samples[i - 1].pos).magnitude;
                var around = new List<float>();
                for (int j = Mathf.Max(1, i - 3); j <= Mathf.Min(_samples.Count - 1, i + 3); j++)
                    if (j != i) around.Add((_samples[j].pos - _samples[j - 1].pos).magnitude);
                around.Sort();
                float median = around.Count > 0 ? around[around.Count / 2] : 0f;
                if (step > 0.015f && step > median * 4f)
                {
                    pops++;
                    report.AppendLine($"POP t={_samples[i].time:0.000} {_samples[i].phase} step {step * 100f:0.0} cm (median {median * 100f:0.00} cm)");
                }
                if (_samples[i].angle > 8f && _samples[i].angle > 4f * Mathf.Max(0.5f, _samples[Mathf.Max(1, i - 1)].angle))
                {
                    pops++;
                    report.AppendLine($"POP t={_samples[i].time:0.000} {_samples[i].phase} rotation step {_samples[i].angle:0.0}°");
                }
            }
            report.AppendLine($"{_samples.Count} frames, {pops} pops");
            File.WriteAllText(Path.Combine(folder, "report.txt"), report.ToString());
            Debug.Log("[HandsCheck]\n" + report);
            Finish(pops == 0 ? 0 : 2);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase)) return t;
            return null;
        }

        static void Finish(int code)
        {
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
