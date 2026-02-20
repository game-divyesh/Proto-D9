using UnityEngine;

namespace MatchingPair.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "ScoreComboSettingsSO", menuName = "MatchingPair/Gameplay/Levels/Score Combo Settings")]
    public sealed class ScoreComboSettingsSO : ScriptableObject
    {
        #region Fields
        [Header("Scoring")]
        [Min(0)] public int BaseMatchScore = 100;

        [Header("Combo Timing")]
        [Min(0.1f)] public float ComboStartWindowSeconds = 5f;
        [Min(0f)] public float ComboWindowReductionPerStep = 0.1f;
        [Min(0.1f)] public float ComboMinimumWindowSeconds = 1f;
        #endregion

        #region UnityCallbacks
        private void OnValidate()
        {
            BaseMatchScore = Mathf.Max(0, BaseMatchScore);
            ComboStartWindowSeconds = Mathf.Max(0.1f, ComboStartWindowSeconds);
            ComboWindowReductionPerStep = Mathf.Max(0f, ComboWindowReductionPerStep);
            ComboMinimumWindowSeconds = Mathf.Max(0.1f, ComboMinimumWindowSeconds);

            if (ComboMinimumWindowSeconds > ComboStartWindowSeconds)
            {
                ComboMinimumWindowSeconds = ComboStartWindowSeconds;
            }
        }
        #endregion
    }
}
