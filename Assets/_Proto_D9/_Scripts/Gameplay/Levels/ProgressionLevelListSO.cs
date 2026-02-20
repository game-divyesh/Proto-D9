using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchingPair.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "ProgressionLevelListSO", menuName = "MatchingPair/Gameplay/Levels/Progression Level List")]
    public sealed class ProgressionLevelListSO : ScriptableObject
    {
        #region Fields
        public List<ProgressionLevelSO> Levels = new List<ProgressionLevelSO>();
        #endregion

        #region Properties
        public int Count => Levels == null ? 0 : Levels.Count;
        #endregion

        #region PublicAPI
        public ProgressionLevelSO GetLevel(int index)
        {
            int levelCount = Count;
            if (index < 0 || index >= levelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Requested progression level index is out of range.");
            }

            ProgressionLevelSO level = Levels[index];
            if (level == null)
            {
                throw new InvalidOperationException("Progression level at index " + index + " is null in '" + name + "'.");
            }

            return level;
        }
        #endregion
    }
}
