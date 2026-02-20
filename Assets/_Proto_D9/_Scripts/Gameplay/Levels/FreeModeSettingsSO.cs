using System.Collections.Generic;
using UnityEngine;
using CardType = MatchingPair.Gameplay.Card.CardType;

namespace MatchingPair.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "FreeModeSettingsSO", menuName = "MatchingPair/Gameplay/Levels/Free Mode Settings")]
    public sealed class FreeModeSettingsSO : ScriptableObject
    {
        #region Fields
        [Header("Grid Range")]
        [Min(1)] public int MinWidth = 2;
        [Min(1)] public int MaxWidth = 6;
        [Min(1)] public int MinHeight = 2;
        [Min(1)] public int MaxHeight = 6;

        [Header("Timer")]
        [Min(1f)] public float DefaultStartTimeSeconds = 60f;
        [Min(0f)] public float BaseRewardSeconds = 5f;
        [Min(0f)] public float RewardPerCellSeconds = 0.5f;

        [Header("Grid Auto Fit")]
        public Vector2 TargetWorldSize = new Vector2(8f, 12f);
        [Min(0f)] public float Spacing = 0.1f;

        [Header("Card Pool")]
        public List<CardType> AllowedCardTypes = new List<CardType>();

        [Header("Distribution")]
        public bool RandomizeDistributionDifficulty = true;
        public CardDistributionDifficulty DistributionDifficulty = CardDistributionDifficulty.Medium;
        #endregion

        #region Unity
        private void OnValidate()
        {
            MinWidth = Mathf.Max(1, MinWidth);
            MaxWidth = Mathf.Max(1, MaxWidth);
            MinHeight = Mathf.Max(1, MinHeight);
            MaxHeight = Mathf.Max(1, MaxHeight);

            if (MinWidth > MaxWidth)
            {
                MaxWidth = MinWidth;
            }

            if (MinHeight > MaxHeight)
            {
                MaxHeight = MinHeight;
            }

            DefaultStartTimeSeconds = Mathf.Max(1f, DefaultStartTimeSeconds);
            BaseRewardSeconds = Mathf.Max(0f, BaseRewardSeconds);
            RewardPerCellSeconds = Mathf.Max(0f, RewardPerCellSeconds);
            Spacing = Mathf.Max(0f, Spacing);
            TargetWorldSize.x = Mathf.Max(0f, TargetWorldSize.x);
            TargetWorldSize.y = Mathf.Max(0f, TargetWorldSize.y);

            bool hadInvalidEasyDifficulty = !RandomizeDistributionDifficulty &&
                                            DistributionDifficulty == CardDistributionDifficulty.Easy;
            if (hadInvalidEasyDifficulty)
            {
                DistributionDifficulty = CardDistributionDifficulty.Medium;
            }

#if UNITY_EDITOR
            if (AllowedCardTypes == null || AllowedCardTypes.Count == 0)
            {
                Debug.LogError("AllowedCardTypes is empty in FreeModeSettingsSO '" + name + "'.", this);
            }

            if (hadInvalidEasyDifficulty)
            {
                Debug.LogWarning(
                    "FreeModeSettingsSO '" + name + "' only supports Medium/Hard distribution. Easy was changed to Medium.",
                    this);
            }
#endif
        }
        #endregion
    }
}
