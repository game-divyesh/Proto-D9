using MatchingPair.Gameplay.Levels;
using UnityEngine;

namespace MatchingPair.Gameplay
{
    public struct ScoreComboResult
    {
        #region Fields
        public int ScoreGained;
        public int ComboCount;
        public bool ComboStarted;
        public bool ComboContinued;
        #endregion
    }

    public sealed class ScoreComboTracker
    {
        #region Fields
        private int currentScore;
        private int currentComboCount;
        private int streakCount;
        private float currentWindowSeconds;
        private float chainExpireTimestamp;
        #endregion

        #region Properties
        public int CurrentScore => currentScore;
        public int CurrentComboCount => currentComboCount;
        public float CurrentWindowSeconds => currentWindowSeconds;
        public bool IsComboActive => currentComboCount > 0;
        #endregion

        #region PublicAPI
        public void Reset(ScoreComboSettingsSO settings)
        {
            currentScore = 0;
            currentComboCount = 0;
            streakCount = 0;
            currentWindowSeconds = settings != null ? settings.ComboStartWindowSeconds : 5f;
            chainExpireTimestamp = -1f;
        }

        public bool Tick(float currentTimestamp, ScoreComboSettingsSO settings)
        {
            if (settings == null)
            {
                return false;
            }

            if (streakCount <= 0 || currentTimestamp <= chainExpireTimestamp)
            {
                return false;
            }

            bool wasComboActive = currentComboCount > 0;
            streakCount = 0;
            currentComboCount = 0;
            currentWindowSeconds = settings.ComboStartWindowSeconds;
            chainExpireTimestamp = -1f;
            return wasComboActive;
        }

        public ScoreComboResult RegisterMatch(float currentTimestamp, ScoreComboSettingsSO settings)
        {
            ScoreComboResult result = default;
            if (settings == null)
            {
                return result;
            }

            Tick(currentTimestamp, settings);

            result.ScoreGained = settings.BaseMatchScore;

            if (streakCount <= 0)
            {
                streakCount = 1;
                currentComboCount = 0;
                currentWindowSeconds = settings.ComboStartWindowSeconds;
                chainExpireTimestamp = currentTimestamp + currentWindowSeconds;
                currentScore += result.ScoreGained;
                result.ComboCount = 0;
                return result;
            }

            streakCount++;
            bool isComboHit = streakCount >= 2;
            if (isComboHit)
            {
                if (currentComboCount <= 0)
                {
                    currentComboCount = 1;
                    result.ComboStarted = true;
                }
                else
                {
                    currentComboCount++;
                    result.ComboContinued = true;
                }

                result.ComboCount = currentComboCount;
                result.ScoreGained += currentComboCount * settings.BaseMatchScore;
                currentWindowSeconds = Mathf.Max(
                    settings.ComboMinimumWindowSeconds,
                    currentWindowSeconds - settings.ComboWindowReductionPerStep);
            }
            else
            {
                result.ComboCount = 0;
            }

            chainExpireTimestamp = currentTimestamp + currentWindowSeconds;
            currentScore += result.ScoreGained;
            return result;
        }

        public float GetRemainingComboTime(float currentTimestamp)
        {
            if (streakCount <= 0 || chainExpireTimestamp < 0f)
            {
                return 0f;
            }

            return Mathf.Max(0f, chainExpireTimestamp - currentTimestamp);
        }
        #endregion
    }
}
