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
        public List<CardType> CardTypesToUse = new List<CardType>();
        public CardDistributionDifficulty DistributionDifficulty = CardDistributionDifficulty.Medium;
        #endregion

        #region Unity
        private void OnValidate()
        {
            Width = Mathf.Max(1, Width);
            Height = Mathf.Max(1, Height);

#if UNITY_EDITOR
            if ((Width * Height) % 2 != 0)
            {
                Debug.LogError(
                    "ProgressionLevelSO '" + name + "' has odd total cells. Width * Height must be even.",
                    this);
            }

            if ((Width % 2) != 0)
            {
                Debug.LogError(
                    "ProgressionLevelSO '" + name + "' has odd Width. Width must be even.",
                    this);
            }

            if ((Height % 2) != 0)
            {
                Debug.LogError(
                    "ProgressionLevelSO '" + name + "' has odd Height. Height must be even.",
                    this);
            }

            if (CardTypesToUse == null || CardTypesToUse.Count == 0)
            {
                Debug.LogError(
                    "ProgressionLevelSO '" + name + "' has no CardTypesToUse.",
                    this);
            }
#endif
        }
        #endregion
    }
}
