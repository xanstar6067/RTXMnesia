#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Editor-only Play snapshot of one view (RTXMnesia.EditorDev.PlayViewPreview): the player is placed at a position,
    /// looks at an entity and the camera renders it as the first frame there, like a level seen in the game.
    /// </summary>
    public sealed class HplPlayViewProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.PlayViewPreview.ExitCode";
        public Vector3 position;
        public float yawDegrees;
        public string aim;
        public string file;

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
            Finish(0);
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
            player.PlaceAt(position, yawDegrees * Mathf.Deg2Rad);
            yield return new WaitForSecondsRealtime(0.5f);
            // "pitch=<degrees>" instead of an entity: look up (positive) or down (negative).
            if (!string.IsNullOrEmpty(aim) && aim.StartsWith("pitch=", StringComparison.Ordinal))
            {
                float pitchDegrees = float.Parse(aim.Substring(6), System.Globalization.CultureInfo.InvariantCulture);
                player.AddLook(0f, -pitchDegrees * Mathf.Deg2Rad - player.Pitch);
                aim = null;
            }
            var target = string.IsNullOrEmpty(aim) ? null : map.GetEntityByName(aim);
            if (target != null)
            {
                var body = target.Bodies.Count > 0 ? target.Bodies[0] : null;
                Vector3 goal = body?.Physics != null ? body.Physics.WorldCenterOfMass : target.GameObject.transform.position;
                Vector3 dir = goal - player.LogicCameraPosition;
                float yaw = Mathf.Atan2(dir.x, dir.z), pitch = -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude);
                player.AddLook(Mathf.DeltaAngle(player.Yaw * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg) * Mathf.Deg2Rad, pitch - player.Pitch);
            }
            yield return new WaitForSecondsRealtime(0.5f);

            var camera = map.PlayerCamera;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = rt;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                camera.Render();
                UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
            }
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, image.EncodeToPNG());
            Destroy(image);
            rt.Release();
            Debug.Log($"Play view preview: {watch.Elapsed.TotalMilliseconds / 10:0.0} ms per frame, {file}");
        }

        void Finish(int code)
        {
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
