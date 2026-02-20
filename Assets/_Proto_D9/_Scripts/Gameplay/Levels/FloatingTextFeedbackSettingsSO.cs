using UnityEngine;

namespace MatchingPair.Gameplay.Levels
{
    [CreateAssetMenu(
        fileName = "FloatingTextFeedbackSettingsSO",
        menuName = "MatchingPair/Gameplay/Levels/Floating Text Feedback Settings")]
    public sealed class FloatingTextFeedbackSettingsSO : ScriptableObject
    {
        #region Fields
        [Header("Offsets")]
        public Vector3 ScoreTextOffset = new Vector3(0f, 0.35f, 0f);
        public Vector3 ComboTextOffset = new Vector3(0f, 0.75f, 0f);

        [Header("Style")]
        public Color ScoreTextColor = Color.white;
        public Color ComboTextColor = new Color(1f, 0.9f, 0.2f, 1f);
        [Min(0.01f)] public float ScoreTextLifetime = 0.9f;
        [Min(0.01f)] public float ComboTextLifetime = 1f;
        #endregion

        #region UnityCallbacks
        private void OnValidate()
        {
            ScoreTextLifetime = Mathf.Max(0.01f, ScoreTextLifetime);
            ComboTextLifetime = Mathf.Max(0.01f, ComboTextLifetime);
        }
        #endregion
    }
}
