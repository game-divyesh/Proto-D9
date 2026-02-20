using System;

namespace MatchingPair.Gameplay.UI
{
    public enum UIScreenType
    {
        None = 0,
        MainMenu = 1,
        Gameplay = 2,
        Pause = 3,
        GameComplete = 4,
        GameOver = 5,
        Settings = 6,

        [Obsolete("Use GameComplete")]
        Win = GameComplete,

        [Obsolete("Use GameOver")]
        Lose = GameOver
    }
}
