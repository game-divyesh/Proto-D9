using UnityEngine;

namespace MatchingPair.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "GameplayAnimationSettingsSO", menuName = "MatchingPair/Gameplay/Levels/Gameplay Animation Settings")]
    public sealed class GameplayAnimationSettingsSO : ScriptableObject
    {
        #region Fields
        [Header("Match")]
        [Min(0f)] public float MatchAnimationDelay = 0.05f;
        [Min(0f)] public float MatchScaleUpDuration = 0.12f;
        [Min(0f)] public float MatchScaleOutDuration = 0.18f;
        [Min(1f)] public float MatchScaleUpMultiplier = 1.15f;

        [Header("Mismatch")]
        [Min(0f)] public float MismatchNudgeDuration = 1f;
        [Min(0f)] public float MismatchNudgeScaleStrength = 0.08f;
        [Min(0f)] public float MismatchNudgeRotationDegrees = 12f;
        [Min(0f)] public float MismatchNudgeFrequency = 6f;
        [Min(0f)] public float MismatchFlipDelay = 0f;
        #endregion

        #region Unity
        private void OnValidate()
        {
            MatchAnimationDelay = Mathf.Max(0f, MatchAnimationDelay);
            MatchScaleUpDuration = Mathf.Max(0f, MatchScaleUpDuration);
            MatchScaleOutDuration = Mathf.Max(0f, MatchScaleOutDuration);
            MatchScaleUpMultiplier = Mathf.Max(1f, MatchScaleUpMultiplier);
            MismatchNudgeDuration = Mathf.Max(0f, MismatchNudgeDuration);
            MismatchNudgeScaleStrength = Mathf.Max(0f, MismatchNudgeScaleStrength);
            MismatchNudgeFrequency = Mathf.Max(0f, MismatchNudgeFrequency);
            MismatchFlipDelay = Mathf.Max(0f, MismatchFlipDelay);
        }
        #endregion
    }
}
