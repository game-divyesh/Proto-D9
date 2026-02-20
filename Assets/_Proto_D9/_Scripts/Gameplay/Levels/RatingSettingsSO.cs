using UnityEngine;

namespace MatchingPair.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "RatingSettingsSO", menuName = "MatchingPair/Gameplay/Levels/Rating Settings")]
    public sealed class RatingSettingsSO : ScriptableObject
    {
        #region Fields
        [Range(0f, 100f)] public float TwoStarThresholdPercent = 25f;
        [Range(0f, 100f)] public float OneStarThresholdPercent = 50f;
        #endregion

        #region PublicAPI
        public int CalculateStars(int totalCards, int openCount)
        {
            int safeTotalCards = Mathf.Max(1, totalCards);
            int safeOpenCount = Mathf.Max(0, openCount);

            float baseValue = safeTotalCards;
            float twoStarCutoff = baseValue + (baseValue * TwoStarThresholdPercent * 0.01f);
            float oneStarCutoff = baseValue + (baseValue * OneStarThresholdPercent * 0.01f);

            if (safeOpenCount <= safeTotalCards)
            {
                return 3;
            }

            if (safeOpenCount <= twoStarCutoff)
            {
                return 2;
            }

            if (safeOpenCount <= oneStarCutoff)
            {
                return 1;
            }

            return 1;
        }
        #endregion
    }
}
