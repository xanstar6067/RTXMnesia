using System.Collections;
using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// The scene side of cLuxMapHandler::CheckMapChange. HPL2 keeps one player, effect handler and music for the
    /// whole game and swaps only the map; here every map is a scene with its own player, effects and sound handler.
    /// The old map hands over what the engine keeps (<see cref="Arrival"/>: the player settings, the screen effects,
    /// the playing music and global sound settings, the start position and end sound), the next scene loads under
    /// the load screen and its <see cref="HplMap"/> takes the arrival in Begin, before OnStart / OnEnter.
    /// </summary>
    public static class HplMapChange
    {
        public sealed class Arrival
        {
            public string MapName, StartPos, EndSound;
            /// <summary>The map left (cLuxMapHandler::CheckMapChange pLastMap).</summary>
            public string FromMap;
            public float LoadStartTime;
            public Player.AmnesiaPlayer.Carry Player;
            public Effects.HplEffects.Carry Effects;
            public Sound.HplMusicHandler Music;
            public readonly Sound.HplSoundHandler.MultipleSettings GlobalVolume = new(), GlobalSpeed = new();
        }

        static Arrival s_pending;

        /// <summary>A map scene is loading or loaded and waiting to take the arrival.</summary>
        public static bool Pending => s_pending != null;

        /// <summary>cLuxMapHandler::FileToMapName: the file name without extension, lower case.</summary>
        public static string FileToMapName(string file) =>
            System.IO.Path.GetFileNameWithoutExtension((file ?? "").Replace('\\', '/')).ToLowerInvariant();

        /// <summary>
        /// The build scene of a map file. HPL2 finds "01_old_archives.map" or "ch02/15_prison_south.map" by file name
        /// (resources are searched by name); the imported scenes are named after their maps.
        /// </summary>
        public static string FindScene(string mapFile)
        {
            string name = FileToMapName(mapFile);
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (FileToMapName(path) == name && path.Contains("/maps/")) return path;
            }
            return null;
        }

        /// <summary>
        /// The new map part of CheckMapChange after OnLeave and saving the old map: the world sounds fade, the load
        /// screen is drawn, the engine state is taken from the old map and the scene loads. The old map stays frozen.
        /// </summary>
        public static void Begin(HplMap from, string scenePath, string mapName, string startPos, string endSound)
        {
            var arrival = new Arrival
            {
                MapName = mapName,
                FromMap = from.MapName,
                StartPos = startPos ?? "",
                EndSound = endSound ?? "",
                LoadStartTime = Time.realtimeSinceStartup,
                Player = from.player != null ? from.player.CaptureCarry() : null,
                Effects = from.effects != null ? from.effects.CaptureCarry() : null,
            };
            var screen = HplLoadScreen.Show(from);
            if (from.sound != null)
            {
                // cLuxMapHandler::SetCurrentMap → cLuxMusicHandler::OnMapLeave, before the world goes.
                arrival.GlobalVolume.CopyFrom(from.sound.GlobalVolume);
                arrival.GlobalSpeed.CopyFrom(from.sound.GlobalSpeed);
                from.sound.Music?.OnMapLeave();
                arrival.Music = from.sound.DetachMusic(screen.transform);
                screen.Music = arrival.Music;
            }
            if (from.player != null) from.player.SetUiInputBlocked(true);
            s_pending = arrival;
            screen.StartCoroutine(Load(from, scenePath));
        }

        static IEnumerator Load(HplMap from, string scenePath)
        {
            // cLuxLoadScreenHandler::DrawGameScreen draws before the load starts.
            yield return null;
            var operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            while (operation != null && !operation.isDone)
            {
                // The old world's sounds fade out (FadeOutAll with stop disabled) until the scene is replaced.
                if (from != null && from.sound != null) from.sound.StepWorldFade(Time.unscaledDeltaTime);
                yield return null;
            }
        }

        /// <summary>
        /// Tools: leave the current map and load it again as a return visit (the saved state is loaded, OnStart does
        /// not run), like coming back through a level door.
        /// </summary>
        public static void Reenter(HplMap map, string startPos)
        {
            string scene = FindScene(map.mapFile);
            if (scene == null) throw new System.InvalidOperationException("Map scene not in the build: " + map.mapFile);
            map.LeaveForReentry(scene, startPos);
        }

        /// <summary>The arrival for the map that is being entered, if this map was loaded by a map change.</summary>
        public static Arrival Take(string mapFile)
        {
            if (s_pending == null) return null;
            if (s_pending.MapName != FileToMapName(mapFile))
            {
                HplLog.PortError($"Map change to '{s_pending.MapName}' arrived in '{mapFile}'");
                s_pending = null;
                return null;
            }
            var arrival = s_pending;
            s_pending = null;
            return arrival;
        }

        /// <summary>New game / back to the title: no map change in progress.</summary>
        public static void ClearPending() => s_pending = null;
    }
}
