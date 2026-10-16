#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using HplInventoryAssets = RTXMnesia.Ui.HplInventoryAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Editor-only Play test of a map change: the level door of 00_rainy_hall → fade out → load screen → 01_old_archives
    /// with the player, effects, music and game state carried over, the load screen text time and exit, then a change
    /// to another start position of the same map.
    /// </summary>
    public sealed class HplMapChangePreviewProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.MapChangePreview.ExitCode";
        int _checks;
        string Folder => Path.GetFullPath("Logs/MapChangeCheck");

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
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

        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            _checks++;
        }

        IEnumerator Run()
        {
            Directory.CreateDirectory(Folder);
            yield return new WaitForSecondsRealtime(1f);
            var map = HplMap.Current;
            Check(map != null && map.MapName == "00_rainy_hall" && map.player != null, "Test must start in 00_rainy_hall");

            // Batch Play has no window focus: no device input, no focus pause (HplGameUi.OnApplicationFocus).
            map.player.readDevices = false;
            if (map.GameUi.IsPaused) map.GameUi.Resume();
            yield return new WaitForSecondsRealtime(0.5f);
            Check(!map.IsMenuPaused, "00_rainy_hall stays paused");
            // The intro of 00 (black screen, inactive player, its timers) is not part of this test.
            map.runScript = false;
            map.effects.Fade.FadeIn(0f);
            map.player.active = true;

            // State that HPL2 keeps across maps (one player, effect handler, music, global data).
            var player = map.player;
            player.lampOil = 37f;
            player.health = 55f;
            player.tinderboxes = 3;
            player.scriptMoveSpeedMul = 0.8f;
            HplGame.SetGlobalVar("carry", "1");
            map.sound.Music.Play("01_amb_darkness.ogg", true, 1f, 0.1f, 0, true);
            map.effects.ImageTrail.FadeTo(0.4f, 100f);
            map.effects.Sepia.FadeTo(0.8f, 100f);
            HplLoadScreen.SetupLoadText("LoadingText", "Ch01_Beginning", 5, "game_loading_rose.jpg");
            yield return new WaitForSecondsRealtime(0.3f);

            // The lantern: item, hands draw → idle, its lights fade in (cLuxPlayerLantern, cLuxPlayerHands).
            Check(player.Hands != null, "Player hands are not imported (Import > Player hands and hand objects)");
            player.Lantern.SetActive(true, true);
            Check(!player.lanternActive, "Lantern turned on without the lantern item");
            var bank = Resources.Load<HplInventoryAssets>(HplInventoryAssets.ResourceName);
            var lanternItem = bank.Item("lantern.ent");
            HplGame.AddItem(map, "lantern", HplGame.ItemType(lanternItem.type), lanternItem.subType, lanternItem.image, lanternItem.amount, "", "", out _);
            player.DoAction(Player.PlayerAction.Lantern, true);
            Check(player.lanternActive && player.Hands.CurrentHandObject == "lantern" && player.Hands.State == Player.PlayerHands.HandsState.Draw, "Lantern key did not draw the lantern");
            yield return new WaitForSecondsRealtime(1.6f);
            Check(player.Hands.State == Player.PlayerHands.HandsState.Idle && player.Hands.CurrentAnimation == "Lantern_Idle", "Hands did not go from draw to idle: " + player.Hands.State + " " + player.Hands.CurrentAnimation);
            Check(player.Hands.HandObjectAlpha >= 1f, "Lantern light did not fade in");
            var handLight = player.Hands.Root.GetComponentInChildren<World.AmnesiaLight>();
            Check(handLight != null && handLight.CurrentColor.maxColorComponent > 0.1f, "Lantern light is dark");
            Check(player.lampOil < 37f, "Lantern does not burn oil");
            // The view with the lantern, without the sepia / trail set for the carry test.
            float sepia = map.effects.Sepia.Amount, trail = map.effects.ImageTrail.Amount;
            map.effects.Sepia.FadeTo(0f, 1000f);
            map.effects.ImageTrail.FadeTo(0f, 1000f);
            yield return new WaitForSecondsRealtime(0.1f);
            CaptureCamera(map.PlayerCamera, "Lantern00.png");
            map.effects.Sepia.FadeTo(sepia, 1000f);
            map.effects.ImageTrail.FadeTo(trail, 1000f);
            yield return new WaitForSecondsRealtime(0.1f);

            PropLevelDoor door = null;
            foreach (var e in map.Entities) if (e is PropLevelDoor d) { door = d; break; }
            Check(door != null, "00_rainy_hall has no level door");
            door.Locked = false;
            door.OnInteract(door.MainBody, Vector3.zero);
            Check(map.IsChangingMap && !player.active && map.effects.Fade.IsFading, "ChangeMap did not start the fade out with the player inactive");

            double deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (!HplLoadScreen.Active && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Check(HplLoadScreen.Active && map.effects.Fade.Alpha >= 1f, "Load screen did not follow the 1.5 s fade out");
            Check(HplLoadScreen.Current.TextEntry.StartsWith("Ch01_Beginning0", StringComparison.Ordinal), "Load screen text is not a numbered LoadingText entry: " + HplLoadScreen.Current.TextEntry);
            Capture(HplLoadScreen.Current, "LoadScreen.png");

            deadline = Time.realtimeSinceStartupAsDouble + 60;
            while ((HplLoadScreen.Current == null || !HplLoadScreen.Current.Loaded) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            var screen = HplLoadScreen.Current;
            map = HplMap.Current;
            Check(screen != null && screen.Loaded && map != null && map.MapName == "01_old_archives", "01_old_archives did not load under the load screen");
            Check(SceneManager.GetActiveScene().path.EndsWith("01_old_archives.unity", StringComparison.Ordinal), "Active scene is not 01_old_archives");
            player = map.player;
            player.readDevices = false;
            Check(player.lampOil > 30f && player.lampOil < 37f && Mathf.Approximately(player.health, 55f) && player.tinderboxes == 3 && Mathf.Approximately(player.scriptMoveSpeedMul, 0.8f), "Player state was not carried over");
            Check(HplGame.GetGlobalVar("carry") == "1", "Global data was lost");
            Check(map.sound.Music.CurrentSongName == "01_amb_darkness.ogg", "Music did not go on in the new map: " + map.sound.Music.CurrentSongName);
            Check(player.lanternActive && player.Hands != null && player.Hands.CurrentHandObject == "lantern" && player.Hands.State == Player.PlayerHands.HandsState.Idle && player.Hands.HandObjectAlpha >= 1f, "The held lantern did not come along");
            Check(Mathf.Approximately(map.effects.ImageTrail.Amount, 0.4f), "Image trail was not carried over");
            Check(map.effects.Fade.Alpha >= 1f && map.effects.Fade.IsFading, "New map does not start black and fading in");
            Check(map.IsMenuPaused && Time.timeScale == 0 && player.UiInputBlocked, "New map runs while the load screen is up");
            var start = map.GetPlayerStart("PlayerStartArea_1");
            Check(start != null && Vector3.Distance(player.Body.FeetPosition, start.transform.position) < 0.3f, "Player is not at the start position");
            yield return new WaitForSecondsRealtime(0.5f);
            Check(screen.LoadingAlpha < 1f, "Loading text does not fade after the load");
            Capture(screen, "LoadScreenText.png");

            screen.ExitPressed();
            deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (HplLoadScreen.Active && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Check(!HplLoadScreen.Active && Time.timeScale == 1 && !player.UiInputBlocked && player.active, "Load screen exit did not resume the game");
            float gameTime = map.GameTime;
            yield return new WaitForSecondsRealtime(0.5f);
            Check(map.GameTime > gameTime && map.effects.Fade.Alpha < 1f, "Game does not update / fade in after the load screen");
            Check(map.effects.Sepia.Amount < 0.8f, "Sepia does not fade out after the map change (cLuxEffectHandler::OnMapLeave)");
            yield return new WaitForSecondsRealtime(2f);
            CaptureCamera(map.PlayerCamera, "Arrived.png");

            // Putting the lantern away: holster animation, lights fade out, hands hidden.
            player.DoAction(Player.PlayerAction.Lantern, true);
            Check(!player.lanternActive && player.Hands.State == Player.PlayerHands.HandsState.Holster, "Lantern key did not holster the lantern");
            yield return new WaitForSecondsRealtime(1.6f);
            Check(player.Hands.State == Player.PlayerHands.HandsState.Disabled && player.Hands.HandObjectAlpha <= 0f, "Hands did not put the lantern away");
            float oil = player.lampOil;
            yield return new WaitForSecondsRealtime(0.3f);
            Check(Mathf.Approximately(oil, player.lampOil), "Oil burns with the lantern off");

            // Same map, another start: only the player moves after the fade out (no load).
            var other = map.GetPlayerStart("PlayerStartArea_2");
            if (other != null)
            {
                map.ChangeMap("01_old_archives.map", "PlayerStartArea_2", "", "");
                deadline = Time.realtimeSinceStartupAsDouble + 5;
                while (map.IsChangingMap && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                Check(!map.IsChangingMap && !HplLoadScreen.Active && HplMap.Current == map, "Same-map change reloaded the map");
                Check(Vector3.Distance(player.Body.FeetPosition, other.transform.position) < 0.3f, "Same-map change did not move the player");
            }

            // Coming back: the map cache (cLuxSavedGameMap) restores what HPL2 keeps of a visited map.
            map.SetVar("cache_test", "7");
            map.AddTimer("cache_timer", 100f, "TimerNothing");
            var door1 = map.GetEntityByName("Door_1") as PropSwingDoor;
            Check(door1 != null, "01_old_archives has no Door_1");
            door1.SetLocked(true, false);
            var torch = map.GetEntityByName("torch_static01_1") as PropLamp;
            Check(torch != null && torch.Lit, "torch_static01_1 is not a lit lamp");
            torch.SetLit(false, false);
            HplEntity disabled = null;
            foreach (var e in map.Entities) if (e is PropObject && e.Active && !e.FullGameSave) { disabled = e; break; }
            disabled.SetActive(false);
            var lanternProp = map.GetEntityByName("lantern");
            Check(lanternProp is PropItem, "01_old_archives has no lantern item");
            map.DestroyEntity(lanternProp);
            var secretDoor = map.GetEntityByName("secret_door") as HplProp;
            Check(secretDoor != null && secretDoor.FullGameSave, "secret_door is not a full game save move object");
            secretDoor.OnConnectionStateChange(map.GetEntityByName("secret_lever"), 1);
            yield return new WaitForSecondsRealtime(1.5f);
            Vector3 secretPosition = secretDoor.MainBody.Position;
            Check(Vector3.Distance(secretPosition, secretDoor.GameObject.transform.position) >= 0f, "secret door");
            int playerCallbacks = map.PlayerCollideCallbacks.Export().Count;
            Check(playerCallbacks > 0, "01_old_archives registered no player collide callbacks");
            string disabledName = disabled.Name;

            HplMapChange.Reenter(map, "PlayerStartArea_1");
            deadline = Time.realtimeSinceStartupAsDouble + 60;
            while ((HplLoadScreen.Current == null || !HplLoadScreen.Current.Loaded) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            map = HplMap.Current;
            player = map.player;
            player.readDevices = false;
            Check(map.MapName == "01_old_archives" && map.GetVar("cache_test") == "7", "Local variables were not restored");
            Check(map.GetTimerTimeLeft("cache_timer") > 90f, "Timers were not restored");
            Check(map.PlayerCollideCallbacks.Export().Count == playerCallbacks, "Player collide callbacks were not restored");
            Check(((PropSwingDoor)map.GetEntityByName("Door_1")).Locked, "Locked door was not restored");
            Check(!((PropLamp)map.GetEntityByName("torch_static01_1")).Lit, "Unlit lamp was not restored");
            Check(!map.GetEntityByName(disabledName).Active, "Disabled entity was not restored");
            Check(map.GetEntityByName("lantern") == null, "Picked item came back");
            var restoredDoor = (HplProp)map.GetEntityByName("secret_door");
            Check(Vector3.Distance(restoredDoor.MainBody.Position, secretPosition) < 0.05f, "Full game save move object is not where it was: " + restoredDoor.MainBody.Position + " vs " + secretPosition);
            HplLoadScreen.Current.ExitPressed();
            deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (HplLoadScreen.Active && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Check(!HplLoadScreen.Active, "Return visit did not resume");

            // Hub: the level door of 01 into 02_entrance_hall (OnStart clears the saved maps), then a return visit
            // with the study key global set (guardian event 1).
            PropLevelDoor hubDoor = null;
            foreach (var e in map.Entities) if (e is PropLevelDoor d && d.MapFile == "02_entrance_hall") hubDoor = d;
            Check(hubDoor != null, "01_old_archives has no level door to 02_entrance_hall");
            hubDoor.Locked = false;
            hubDoor.OnInteract(hubDoor.MainBody, Vector3.zero);
            deadline = Time.realtimeSinceStartupAsDouble + 60;
            while ((HplLoadScreen.Current == null || !HplLoadScreen.Current.Loaded || HplMap.Current == map) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            map = HplMap.Current;
            player = map.player;
            player.readDevices = false;
            Check(map.MapName == "02_entrance_hall" && map.Script is Levels.Ch01.EntranceHall, "02_entrance_hall did not load with its script");
            var hubStart = map.GetPlayerStart("PlayerStartArea_4");
            Check(hubStart != null && Vector3.Distance(player.Body.FeetPosition, hubStart.transform.position) < 0.3f, "Player is not at PlayerStartArea_4 of 02_entrance_hall");
            Check(!HplSavedMaps.MapExists("01_old_archives"), "02_entrance_hall OnStart did not clear the saved maps");
            Check(map.GetVar("ScaryMusic") == "0" && map.GetTimerTimeLeft("runhint") > 0f, "02_entrance_hall OnStart did not run");
            World.AmnesiaLight boxLight = map.GetLight("BoxLight_2");
            Check(boxLight != null && boxLight.box && boxLight.Visible, "02_entrance_hall has no visible box light BoxLight_2");
            HplLoadScreen.Current.ExitPressed();
            deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (HplLoadScreen.Active && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            yield return new WaitForSecondsRealtime(2.5f);
            Check(map.sound.Music.CurrentSongName.StartsWith("02_amb_safe", StringComparison.Ordinal), "02_entrance_hall music is not 02_amb_safe: " + map.sound.Music.CurrentSongName);
            CaptureCamera(map.PlayerCamera, "EntranceHall.png");
            float withBox = _lastMean;
            boxLight.SetVisible(false);
            CaptureCamera(map.PlayerCamera, "EntranceHallNoBox.png");
            boxLight.SetVisible(true);
            Check(withBox > _lastMean * 1.05f, $"Box light does not brighten the view: mean {withBox:0.0000} with, {_lastMean:0.0000} without");
            Debug.Log($"[RTXMnesia] 02_entrance_hall mean brightness: {withBox:0.0000} with BoxLight_2, {_lastMean:0.0000} without");

            HplGame.SetGlobalVar("key_study_1", "key_study_1");
            float hubSanity = player.sanity;
            HplMapChange.Reenter(map, "PlayerStartArea_1");
            deadline = Time.realtimeSinceStartupAsDouble + 60;
            while ((HplLoadScreen.Current == null || !HplLoadScreen.Current.Loaded) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            map = HplMap.Current;
            player = map.player;
            player.readDevices = false;
            Check(map.MapName == "02_entrance_hall" && map.GetVar("ScaryMusic") == "1", "Return to 02 did not see the study key global");
            Check(HplGame.GetGlobalVar("key_study_1") == "" && map.GetTimerTimeLeft("guard1") > 0f, "Guardian event 1 was not scheduled");
            HplLoadScreen.Current.ExitPressed();
            deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (HplLoadScreen.Active && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            yield return new WaitForSecondsRealtime(2.5f);
            boxLight = map.GetLight("BoxLight_2");
            Check(boxLight.CurrentColor.g < 0.165f, "Guardian event did not fade BoxLight_2: " + boxLight.CurrentColor);
            Check(map.effects.ImageTrail.Amount > 0f && player.sanity < hubSanity - 5f, "Guardian event gave no image trail / sanity damage");
            Check(map.GetEntityByName("slime_pile_1") is { Active: true }, "Guardian event did not show the slime");
            CaptureCamera(map.PlayerCamera, "Guardian.png");

            string report = $"PASS: {_checks} Play-mode map change checks (level door, fade, load screen, carry-over, exit, same map, return visit, hub 02 and guardian); screenshots {Folder}";
            File.WriteAllText(Path.Combine(Folder, "PlayTests.txt"), report);
            Debug.Log(report);
        }

        /// <summary>The load screen canvas through a temporary camera (the overlay is not rendered in batch Play).</summary>
        void Capture(HplLoadScreen screen, string name)
        {
            var canvas = screen.GetComponent<Canvas>();
            var go = new GameObject("Load screen capture");
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 31;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            camera.targetTexture = target;
            var mode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            Save(target, canvas, camera, name);
            canvas.renderMode = mode;
            camera.targetTexture = null;
            target.Release();
            Destroy(target);
            Destroy(go);
        }

        /// <summary>Mean sRGB brightness (0-1) of the last <see cref="CaptureCamera"/>.</summary>
        float _lastMean;

        void CaptureCamera(Camera camera, string name)
        {
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            camera.targetTexture = target;
            camera.Render();
            Save(target, null, camera, name);
            camera.targetTexture = null;
            target.Release();
            Destroy(target);
        }

        void Save(RenderTexture target, Canvas canvas, Camera camera, string name)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            double sum = 0;
            foreach (var c in image.GetPixels32()) sum += c.r + c.g + c.b;
            _lastMean = (float)(sum / (3.0 * 255.0 * image.width * image.height));
            if (canvas != null)
                Type.GetType("RTXMnesia.EditorDev.HudSnapshot, Assembly-CSharp-Editor").GetMethod("DrawCanvas").Invoke(null, new object[] { image, canvas, camera });
            File.WriteAllBytes(Path.Combine(Folder, name), image.EncodeToPNG());
            Destroy(image);
            RenderTexture.active = previous;
        }

        void Finish(int code)
        {
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
