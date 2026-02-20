using UnityEngine;

namespace MatchingPair.Gameplay
{
    public static class GamePrefs
    {
        #region Constants
        private const string UnlockedLevelIndexKey = "MatchingPair.UnlockedLevelIndex";
        private const string LastSelectedModeKey = "MatchingPair.LastSelectedMode";
        #endregion

        #region PublicAPI
        public static int GetUnlockedLevelIndex()
        {
            int storedIndex = PlayerPrefs.GetInt(UnlockedLevelIndexKey, 0);
            return Mathf.Max(0, storedIndex);
        }

        public static void SetUnlockedLevelIndex(int index)
        {
            int safeIndex = Mathf.Max(0, index);
            int currentIndex = GetUnlockedLevelIndex();
            if (safeIndex < currentIndex)
            {
                return;
            }

            PlayerPrefs.SetInt(UnlockedLevelIndexKey, safeIndex);
            PlayerPrefs.Save();
        }

        public static GameMode GetLastSelectedMode()
        {
            int storedValue = PlayerPrefs.GetInt(LastSelectedModeKey, (int)GameMode.FreeToPlay);
            if (storedValue != (int)GameMode.FreeToPlay && storedValue != (int)GameMode.Progression)
            {
                return GameMode.FreeToPlay;
            }

            return (GameMode)storedValue;
        }

        public static void SetLastSelectedMode(GameMode mode)
        {
            PlayerPrefs.SetInt(LastSelectedModeKey, (int)mode);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(UnlockedLevelIndexKey);
            PlayerPrefs.DeleteKey(LastSelectedModeKey);
            PlayerPrefs.Save();
        }
        #endregion
    }
}
