using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>The title screen owns a live decorative world; it is not the paused gameplay container.</summary>
    [DefaultExecutionOrder(-30)]
    public sealed class HplMainMenu : MonoBehaviour
    {
        public HplGameUi Ui { get; private set; }
        void Start()
        {
            var map = GetComponent<HplMap>();
            map.Begin();
            Ui = gameObject.AddComponent<HplGameUi>();
            Ui.Init(map, true);
            map.hud.Canvas.enabled = false;
            map.sound.EnsureMusic();
            map.sound.Music.GameVolumeMul = 1;
            string music = map.menuConfig != null ? map.menuConfig.GetString("Main", "Music", "game_menu.ogg") : "game_menu.ogg";
            map.sound.Music.Play(music, true, 1, 1f / 0.35f, 0, false);
        }
    }
}
