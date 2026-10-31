using UnityEngine;

namespace MathRunner.Game
{
    /// <summary>
    /// Starts the desktop game full screen. The Player Settings fullscreen mode only applies on a computer's
    /// very first run: Unity then remembers the last window size and mode, so a PC that once ran a windowed
    /// build stays windowed. This applies full screen once per computer, and after that the player's own choice
    /// (Alt+Enter) is remembered as usual.
    /// </summary>
    public static class ScreenDefaults
    {
        private const string AppliedKey = "MathRunner.FullscreenDefault";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            if (PlayerPrefs.GetInt(AppliedKey, 0) == 1) return;
            var screen = Screen.currentResolution;
            Screen.SetResolution(screen.width, screen.height, FullScreenMode.FullScreenWindow);
            PlayerPrefs.SetInt(AppliedKey, 1);
            PlayerPrefs.Save();
#endif
        }
    }
}
