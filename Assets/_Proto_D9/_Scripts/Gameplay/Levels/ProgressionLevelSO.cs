using System.Collections.Generic;
using UnityEngine;
using CardType = MatchingPair.Gameplay.Card.CardType;

namespace MatchingPair.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "ProgressionLevelSO", menuName = "MatchingPair/Gameplay/Levels/Progression Level")]
    public sealed class ProgressionLevelSO : ScriptableObject
    {
        #region Fields
        [Min(1)] public int Width = 2;
        [Min(1)] public int Height = 2;
        [Min(0f)] public float LevelTimerSeconds = 60f;
        public List<CardType> CardTypesToUse = new List<CardType>();
        public CardDistributionDifficulty DistributionDifficulty = CardDistributionDifficulty.Medium;
        #endregion

        #region Unity
        private void OnValidate()
        {
            Width = Mathf.Max(1, Width);
            Height = Mathf.Max(1, Height);
            LevelTimerSeconds = Mathf.Max(0f, LevelTimerSeconds);

#if UNITY_EDITOR
            if (UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
            {
                return;
            }

            if ((Width * Height) % 2 != 0)
            {
                Debug.LogError(
                    "ProgressionLevelSO '" + name + "' has odd total cells. Width=" + Width +
                    ", Height=" + Height + ", Total=" + (Width * Height) + ". Width * Height must be even.",
                    this);
            }

            if (CardTypesToUse == null || CardTypesToUse.Count == 0)
            {
                Debug.LogError(
                    "ProgressionLevelSO '" + name + "' has no CardTypesToUse.",
                    this);
                return;
            }

            int pairCount = (Width * Height) / 2;
            if (CardTypesToUse.Count > pairCount)
            {
                Debug.LogWarning(
                    "ProgressionLevelSO '" + name + "' has " + CardTypesToUse.Count +
                    " card types but only " + pairCount + " pairs. Not all types can appear in this level.",
                    this);
            }
#endif
        }
        #endregion
    }
}
