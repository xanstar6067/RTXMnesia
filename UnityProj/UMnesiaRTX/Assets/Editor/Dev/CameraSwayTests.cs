using System;
using System.Reflection;
using RTXMnesia.Player;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>Checks the rendered camera without loading a level, running audio or building a player.</summary>
    public static class CameraSwayTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Tools/RTXMnesia/Tests/Camera sway")]
        public static void RunBatch()
        {
            var root = new GameObject("Camera sway checks") { hideFlags = HideFlags.HideAndDontSave };
            int checks = 0;
            bool failed = false;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks++;
            }
            try
            {
                var player = root.AddComponent<AmnesiaPlayer>();
                var body = root.AddComponent<HplCharacterBody>();
                typeof(AmnesiaPlayer).GetProperty(nameof(AmnesiaPlayer.Body)).SetValue(player, body);
                var cameraObject = new GameObject("Camera");
                cameraObject.transform.SetParent(root.transform);
                player.playerCamera = cameraObject.AddComponent<Camera>();
                var camera = cameraObject.transform;
                void Set(string name, float value) => typeof(AmnesiaPlayer).GetField(name, Private).SetValue(player, value);
                void Render(float alpha) => typeof(AmnesiaPlayer).GetMethod("UpdateCameraTransform", Private).Invoke(player, new object[] { alpha });
                float Roll() => Mathf.Atan2(camera.right.y, camera.right.x) * Mathf.Rad2Deg;

                player.SetRoll(0f);
                player.SnapCamera();
                // A 0.5 degree/s scripted roll at 60 Hz, rendered at 120 Hz. Each frame must move,
                // including angles which Quaternion.Angle rounds to zero in single precision.
                float last = Roll();
                for (int step = 0; step < 120; step++)
                {
                    Set("_previousCameraRoll", player.Roll);
                    player.FadeRollTo(12f * Mathf.Deg2Rad, 0.3f, 0.5f * Mathf.Deg2Rad);
                    typeof(AmnesiaPlayer).GetMethod("UpdateCameraEffects", Private).Invoke(player, new object[] { 1f / 60f });
                    foreach (float alpha in new[] { 0.5f, 1f })
                    {
                        Render(alpha);
                        float current = Roll();
                        Check(Mathf.Abs(current - last - 0.5f / 120f) < 0.00001f,
                              $"standing sway advances evenly: step {step}, alpha {alpha}, delta {current - last}");
                        last = current;
                    }
                }
                // Position changes must not decide whether the same rotation reaches the camera.
                Set("_previousCameraRoll", 0f);
                Set("_roll", 0.01f * Mathf.Deg2Rad);
                Render(0.5f);
                float standing = Roll();
                typeof(HplCharacterBody).GetField("_cameraPosition", Private).SetValue(body, Vector3.forward);
                Render(0.5f);
                Check(Mathf.Abs(Roll() - standing) < 0.000001f, "walking and standing render the same roll");

                player.SetRoll(0.4f);
                Render(0f);
                Check(Mathf.Abs(Roll() - 0.4f * Mathf.Rad2Deg) < 0.00001f, "SetRoll is immediate");
                Set("_roll", 0.5f);
                player.SnapCamera();
                Render(0f);
                Check(Mathf.Abs(Roll() - 0.5f * Mathf.Rad2Deg) < 0.00001f, "SnapCamera resets interpolation");

                player.SetRoll(0.3f);
                player.SnapCamera();
                camera.hasChanged = false;
                for (int i = 0; i < 20; i++) Render(1f);
                Check(!camera.hasChanged, "stationary camera remains untouched for path tracing");
                camera.rotation = Quaternion.identity;
                Render(1f);
                Check(Mathf.Abs(Roll() - 0.3f * Mathf.Rad2Deg) < 0.00001f, "external camera rotation is restored");
                Debug.Log($"[CameraSwayTests] PASS: {checks} checks");
            }
            catch (Exception e)
            {
                failed = true;
                Debug.LogException(e);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
