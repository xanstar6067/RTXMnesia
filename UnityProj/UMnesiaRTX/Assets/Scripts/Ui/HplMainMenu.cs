using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// The title screen owns a live decorative world; it is not the paused gameplay container. The first title
    /// screen of a run comes after the pre menu (cLuxPreMenu: logos, first start welcome and gamma), as the
    /// PreMenu container precedes MainMenu in HPL2.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class HplMainMenu : MonoBehaviour
    {
        public HplGameUi Ui { get; private set; }
        /// <summary>The pre menu shown before this title screen (null when skipped or done).</summary>
        public HplPreMenu PreMenu { get; private set; }

        HplMap _map;

        void Start()
        {
            _map = GetComponent<HplMap>();
            _map.Begin();
            _map.hud.Canvas.enabled = false;
            _map.sound.EnsureMusic();
            _map.sound.Music.GameVolumeMul = 1;
            if (HplPreMenu.ShouldShow)
            {
                PreMenu = gameObject.AddComponent<HplPreMenu>();
                PreMenu.Begin(_map, StartTitle);
            }
            else StartTitle();
        }

        void StartTitle()
        {
            PreMenu = null;
            Ui = gameObject.AddComponent<HplGameUi>();
            Ui.Init(_map, true);
            string music = _map.menuConfig != null ? _map.menuConfig.GetString("Main", "Music", "game_menu.ogg") : "game_menu.ogg";
            _map.sound.Music.Play(music, true, 1, 1f / 0.35f, 0, false);
        }
    }
}
