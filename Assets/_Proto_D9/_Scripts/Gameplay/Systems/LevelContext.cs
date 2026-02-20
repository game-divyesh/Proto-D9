using System;
using System.Collections.Generic;
using CardType = MatchingPair.Gameplay.Card.CardType;
using MatchingPair.Gameplay.Levels;

namespace MatchingPair.Gameplay
{
    public sealed class LevelContext
    {
        #region Properties
        public GameMode Mode { get; }
        public int Width { get; }
        public int Height { get; }
        public int TotalCards { get; }
        public float StartTimeSeconds { get; }
        public float ClearRewardSeconds { get; }
        public CardDistributionDifficulty DistributionDifficulty { get; }
        public IReadOnlyList<CardType> TypesUsed => typesUsed;
        #endregion

        #region Fields
        private readonly CardType[] typesUsed;
        #endregion

        #region Constructors
        public LevelContext(
            GameMode mode,
            int width,
            int height,
            int totalCards,
            float startTimeSeconds,
            float clearRewardSeconds,
            CardDistributionDifficulty distributionDifficulty,
            CardType[] typesUsed)
        {
            if (typesUsed == null)
            {
                throw new ArgumentNullException(nameof(typesUsed));
            }

            Mode = mode;
            Width = width;
            Height = height;
            TotalCards = totalCards;
            StartTimeSeconds = startTimeSeconds;
            ClearRewardSeconds = clearRewardSeconds;
            DistributionDifficulty = distributionDifficulty;

            this.typesUsed = new CardType[typesUsed.Length];
            int typeCount = typesUsed.Length;
            for (int index = 0; index < typeCount; index++)
            {
                this.typesUsed[index] = typesUsed[index];
            }
        }
        #endregion
    }
}
