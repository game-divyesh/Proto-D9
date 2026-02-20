using System;
using System.Collections.Generic;
using MatchingPair.Gameplay.GridSystem;
using MatchingPair.Gameplay.Levels;
using UnityEngine;
using CardItemCatalog = MatchingPair.Gameplay.Card.CardItem_Catalog;
using CardItemData = MatchingPair.Gameplay.Card.CardItem_Data;
using CardController = MatchingPair.Gameplay.Card.Card_Controller;
using CardFactoryType = MatchingPair.Gameplay.Card.CardFactory;
using CardType = MatchingPair.Gameplay.Card.CardType;

namespace MatchingPair.Gameplay
{
    public sealed class LevelManager : MonoBehaviour
    {
        #region Fields
        [Header("Mode")]
        [SerializeField] private GameMode startingMode = GameMode.FreeToPlay;
        [SerializeField] private bool loadLastModeFromPrefs = true;
        [SerializeField] private int progressionStartIndex = -1;
        [SerializeField] private bool buildOnStart = true;

        [Header("Dependencies")]
        [SerializeField] private CardGridSystem cardGridSystem;
        [SerializeField] private CardFactoryType cardFactory;
        [SerializeField] private CardItemCatalog cardCatalog;
        [SerializeField] private Transform cardParent;

        [Header("Configs")]
        [SerializeField] private FreeModeSettingsSO freeModeSettings;
        [SerializeField] private ProgressionLevelListSO progressionLevelList;

        [Header("Grid Defaults")]
        [SerializeField, Min(0.01f)] private float defaultCellSpacing = 0.1f;

        private CardController cardPrefab;
        private CardController[] spawnedCards = Array.Empty<CardController>();
        private LevelContext currentLevelContext;
        private GameMode currentMode;
        private int currentProgressionLevelIndex = -1;
        private int remainingPairs;
        #endregion

        #region Events
        public event Action<LevelContext> OnLevelBuilt;
        public event Action OnAllPairsSolved;
        #endregion

        #region Properties
        public GameMode CurrentMode => currentMode;
        public LevelContext CurrentLevelContext => currentLevelContext;
        public IReadOnlyList<CardController> SpawnedCards => spawnedCards;
        public int CurrentProgressionLevelIndex => currentProgressionLevelIndex;
        public int RemainingPairs => remainingPairs;
        #endregion

        #region UnityCallbacks
        private void Awake()
        {
            if (cardParent == null)
            {
                cardParent = transform;
            }
        }

        private void OnValidate()
        {
            defaultCellSpacing = Mathf.Max(0.01f, defaultCellSpacing);
        }

        private void Start()
        {
            if (buildOnStart)
            {
                BuildCurrentModeLevel();
            }
        }
        #endregion

        #region PublicAPI
        public void BuildCurrentModeLevel()
        {
            GameMode modeToBuild = startingMode;
            if (loadLastModeFromPrefs)
            {
                modeToBuild = GamePrefs.GetLastSelectedMode();
            }

            BuildLevel(modeToBuild);
        }

        public void BuildLevel(GameMode mode)
        {
            ValidateDependencies();

            currentMode = mode;
            GamePrefs.SetLastSelectedMode(mode);

            ClearSpawnedCards();

            if (mode == GameMode.FreeToPlay)
            {
                BuildFreeModeLevel();
                return;
            }

            BuildProgressionLevel();
        }

        public void BuildNextProgressionLevel()
        {
            if (progressionLevelList == null || progressionLevelList.Count == 0)
            {
                return;
            }

            if (currentProgressionLevelIndex < 0)
            {
                currentProgressionLevelIndex = ResolveInitialProgressionLevelIndex();
            }

            int nextIndex = currentProgressionLevelIndex + 1;
            int lastIndex = progressionLevelList.Count - 1;
            currentProgressionLevelIndex = Mathf.Clamp(nextIndex, 0, lastIndex);
            BuildLevel(GameMode.Progression);
        }

        public void UnlockNextProgressionLevel()
        {
            if (progressionLevelList == null || progressionLevelList.Count == 0)
            {
                return;
            }

            int maxIndex = progressionLevelList.Count - 1;
            int nextUnlockedIndex = Mathf.Clamp(currentProgressionLevelIndex + 1, 0, maxIndex);
            GamePrefs.SetUnlockedLevelIndex(nextUnlockedIndex);
        }

        public void NotifyPairSolved()
        {
            if (remainingPairs <= 0)
            {
                return;
            }

            remainingPairs--;
            if (remainingPairs == 0)
            {
                OnAllPairsSolved?.Invoke();
            }
        }

        public void SetProgressionLevelIndex(int index)
        {
            currentProgressionLevelIndex = index;
        }
        #endregion

        #region BuildFlow
        private void BuildFreeModeLevel()
        {
            if (freeModeSettings == null)
            {
                throw new InvalidOperationException("FreeModeSettingsSO is missing on LevelManager.");
            }

            if (freeModeSettings.AllowedCardTypes == null || freeModeSettings.AllowedCardTypes.Count == 0)
            {
                throw new InvalidOperationException("FreeModeSettingsSO has no AllowedCardTypes.");
            }

            int width = UnityEngine.Random.Range(freeModeSettings.MinWidth, freeModeSettings.MaxWidth + 1);
            int height = UnityEngine.Random.Range(freeModeSettings.MinHeight, freeModeSettings.MaxHeight + 1);
            EnsureEvenCellCount(
                ref width,
                ref height,
                freeModeSettings.MinWidth,
                freeModeSettings.MaxWidth,
                freeModeSettings.MinHeight,
                freeModeSettings.MaxHeight);

            int totalCards = width * height;
            CardType[] spawnedTypes = new CardType[totalCards];
            CardDistributionDifficulty distributionDifficulty = ResolveFreeModeDistributionDifficulty();
            FillAndShufflePairs(
                spawnedTypes,
                freeModeSettings.AllowedCardTypes,
                width,
                height,
                distributionDifficulty);

            float spacing = ResolveCellSpacing(freeModeSettings.Spacing);
            ApplyGridSettings(width, height, spacing);
            SpawnCards(spawnedTypes);

            float rewardSeconds = freeModeSettings.BaseRewardSeconds + (totalCards * freeModeSettings.RewardPerCellSeconds);
            LevelContext context = new LevelContext(
                GameMode.FreeToPlay,
                width,
                height,
                totalCards,
                Mathf.Max(1f, freeModeSettings.DefaultStartTimeSeconds),
                rewardSeconds,
                distributionDifficulty,
                spawnedTypes);

            CompleteBuild(context);
        }

        private void BuildProgressionLevel()
        {
            if (progressionLevelList == null || progressionLevelList.Count == 0)
            {
                throw new InvalidOperationException("ProgressionLevelListSO is missing or empty.");
            }

            if (currentProgressionLevelIndex < 0)
            {
                currentProgressionLevelIndex = ResolveInitialProgressionLevelIndex();
            }

            int lastIndex = progressionLevelList.Count - 1;
            currentProgressionLevelIndex = Mathf.Clamp(currentProgressionLevelIndex, 0, lastIndex);

            ProgressionLevelSO level = progressionLevelList.GetLevel(currentProgressionLevelIndex);
            ValidateProgressionLevel(level);

            int totalCards = level.Width * level.Height;
            CardType[] spawnedTypes = new CardType[totalCards];
            FillAndShufflePairs(
                spawnedTypes,
                level.CardTypesToUse,
                level.Width,
                level.Height,
                level.DistributionDifficulty);

            float spacing = ResolveCellSpacing(freeModeSettings != null ? freeModeSettings.Spacing : 0f);
            ApplyGridSettings(level.Width, level.Height, spacing);
            SpawnCards(spawnedTypes);

            LevelContext context = new LevelContext(
                GameMode.Progression,
                level.Width,
                level.Height,
                totalCards,
                0f,
                0f,
                level.DistributionDifficulty,
                spawnedTypes);

            CompleteBuild(context);
        }
        #endregion

        #region PrivateHelpers
        private void CompleteBuild(LevelContext context)
        {
            currentLevelContext = context;
            remainingPairs = context.TotalCards / 2;
            OnLevelBuilt?.Invoke(currentLevelContext);
        }

        private void ValidateDependencies()
        {
            if (cardGridSystem == null)
            {
                throw new InvalidOperationException("CardGridSystem is missing on LevelManager.");
            }

            if (cardFactory != null)
            {
                return;
            }

            if (cardCatalog == null)
            {
                throw new InvalidOperationException("CardItem_Catalog is missing on LevelManager.");
            }

            if (cardPrefab == null)
            {
                cardPrefab = cardCatalog.GetDefaultPrefabOrThrow();
            }
        }

        private void ValidateProgressionLevel(ProgressionLevelSO level)
        {
            if (level == null)
            {
                throw new InvalidOperationException("Progression level is null.");
            }

            if ((level.Width * level.Height) % 2 != 0)
            {
                throw new InvalidOperationException("Progression level '" + level.name + "' must have even total cell count.");
            }

            if ((level.Width % 2) != 0)
            {
                throw new InvalidOperationException("Progression level '" + level.name + "' must have even Width.");
            }

            if ((level.Height % 2) != 0)
            {
                throw new InvalidOperationException("Progression level '" + level.name + "' must have even Height.");
            }

            if (level.CardTypesToUse == null || level.CardTypesToUse.Count == 0)
            {
                throw new InvalidOperationException("Progression level '" + level.name + "' has no CardTypesToUse.");
            }
        }

        private int ResolveInitialProgressionLevelIndex()
        {
            if (progressionStartIndex >= 0)
            {
                return progressionStartIndex;
            }

            return GamePrefs.GetUnlockedLevelIndex();
        }

        private void ApplyGridSettings(int width, int height, float spacing)
        {
            GridSettings settings = cardGridSystem.Settings;
            settings.Width = Mathf.Max(1, width);
            settings.Height = Mathf.Max(1, height);
            settings.Spacing = Mathf.Max(0.01f, spacing);
            settings.AutoFitToArea = false;
            settings.Validate();
        }

        private float ResolveCellSpacing(float requestedSpacing)
        {
            if (requestedSpacing > 0f)
            {
                return requestedSpacing;
            }

            return defaultCellSpacing;
        }

        private CardDistributionDifficulty ResolveFreeModeDistributionDifficulty()
        {
            if (freeModeSettings == null)
            {
                return CardDistributionDifficulty.Medium;
            }

            if (freeModeSettings.RandomizeDistributionDifficulty)
            {
                return UnityEngine.Random.Range(0, 2) == 0
                    ? CardDistributionDifficulty.Medium
                    : CardDistributionDifficulty.Hard;
            }

            if (freeModeSettings.DistributionDifficulty == CardDistributionDifficulty.Easy)
            {
                return CardDistributionDifficulty.Medium;
            }

            return freeModeSettings.DistributionDifficulty;
        }

        private void FillAndShufflePairs(
            CardType[] targetBuffer,
            IList<CardType> allowedTypes,
            int width,
            int height,
            CardDistributionDifficulty difficulty)
        {
            int totalCards = targetBuffer.Length;
            int pairCount = totalCards / 2;
            int allowedCount = allowedTypes.Count;
            if (allowedCount == 0)
            {
                throw new InvalidOperationException("Cannot build pairs because allowed type list is empty.");
            }

            CardType[] pairTypes = new CardType[pairCount];
            for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
            {
                CardType type = allowedTypes[UnityEngine.Random.Range(0, allowedCount)];
                pairTypes[pairIndex] = type;
            }

            if (difficulty == CardDistributionDifficulty.Easy)
            {
                PlacePairsByDistance(targetBuffer, pairTypes, width, true);
                return;
            }

            if (difficulty == CardDistributionDifficulty.Hard)
            {
                PlacePairsByDistance(targetBuffer, pairTypes, width, false);
                ReduceAdjacentDuplicates(targetBuffer, width, height, 3);
                return;
            }

            PlacePairsRandom(targetBuffer, pairTypes);
            ReduceAdjacentDuplicates(targetBuffer, width, height, 1);
        }

        private static void PlacePairsRandom(CardType[] targetBuffer, CardType[] pairTypes)
        {
            int totalCards = targetBuffer.Length;
            int[] availableIndices = new int[totalCards];
            int availableCount = InitializeAvailableIndices(availableIndices);

            int pairCount = pairTypes.Length;
            for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
            {
                int firstCell = TakeRandomAvailableCell(availableIndices, ref availableCount);
                int secondCell = TakeRandomAvailableCell(availableIndices, ref availableCount);

                CardType pairType = pairTypes[pairIndex];
                targetBuffer[firstCell] = pairType;
                targetBuffer[secondCell] = pairType;
            }
        }

        private static void PlacePairsByDistance(
            CardType[] targetBuffer,
            CardType[] pairTypes,
            int width,
            bool placeNearby)
        {
            int totalCards = targetBuffer.Length;
            int[] availableIndices = new int[totalCards];
            int availableCount = InitializeAvailableIndices(availableIndices);

            int pairCount = pairTypes.Length;
            for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
            {
                int firstCell = TakeRandomAvailableCell(availableIndices, ref availableCount);
                int secondCell = TakeDistanceBasedCell(
                    firstCell,
                    width,
                    availableIndices,
                    ref availableCount,
                    placeNearby);

                CardType pairType = pairTypes[pairIndex];
                targetBuffer[firstCell] = pairType;
                targetBuffer[secondCell] = pairType;
            }
        }

        private static int InitializeAvailableIndices(int[] availableIndices)
        {
            int totalCards = availableIndices.Length;
            for (int index = 0; index < totalCards; index++)
            {
                availableIndices[index] = index;
            }

            return totalCards;
        }

        private static int TakeRandomAvailableCell(int[] availableIndices, ref int availableCount)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableCount);
            int selectedCell = availableIndices[randomIndex];
            RemoveAvailableAt(availableIndices, ref availableCount, randomIndex);
            return selectedCell;
        }

        private static int TakeDistanceBasedCell(
            int referenceCell,
            int width,
            int[] availableIndices,
            ref int availableCount,
            bool chooseNearest)
        {
            int referenceX = referenceCell % width;
            int referenceY = referenceCell / width;
            int selectedListIndex = -1;
            int selectedDistance = chooseNearest ? int.MaxValue : int.MinValue;
            int tieCount = 0;

            for (int listIndex = 0; listIndex < availableCount; listIndex++)
            {
                int cellIndex = availableIndices[listIndex];
                int cellX = cellIndex % width;
                int cellY = cellIndex / width;
                int distance = Mathf.Abs(referenceX - cellX) + Mathf.Abs(referenceY - cellY);

                bool isBetter = chooseNearest
                    ? distance < selectedDistance
                    : distance > selectedDistance;

                if (isBetter)
                {
                    selectedDistance = distance;
                    selectedListIndex = listIndex;
                    tieCount = 1;
                    continue;
                }

                if (distance == selectedDistance)
                {
                    tieCount++;
                    if (UnityEngine.Random.Range(0, tieCount) == 0)
                    {
                        selectedListIndex = listIndex;
                    }
                }
            }

            if (selectedListIndex < 0)
            {
                return TakeRandomAvailableCell(availableIndices, ref availableCount);
            }

            int selectedCell = availableIndices[selectedListIndex];
            RemoveAvailableAt(availableIndices, ref availableCount, selectedListIndex);
            return selectedCell;
        }

        private static void RemoveAvailableAt(int[] availableIndices, ref int availableCount, int listIndex)
        {
            int lastIndex = availableCount - 1;
            availableIndices[listIndex] = availableIndices[lastIndex];
            availableCount = lastIndex;
        }

        private static void ReduceAdjacentDuplicates(CardType[] cards, int width, int height, int passCount)
        {
            int totalCards = cards.Length;
            if (totalCards <= 1 || width <= 0 || height <= 0)
            {
                return;
            }

            for (int passIndex = 0; passIndex < passCount; passIndex++)
            {
                bool hasChanges = false;
                for (int cellIndex = 0; cellIndex < totalCards; cellIndex++)
                {
                    if (CountAdjacentMatches(cards, cellIndex, width, height) == 0)
                    {
                        continue;
                    }

                    int currentScore = GetLocalConflictScore(cards, cellIndex, width, height);
                    int bestSwapIndex = -1;
                    int bestScore = currentScore;

                    for (int candidateIndex = cellIndex + 1; candidateIndex < totalCards; candidateIndex++)
                    {
                        if (cards[candidateIndex] == cards[cellIndex])
                        {
                            continue;
                        }

                        int beforeScore = currentScore + GetLocalConflictScore(cards, candidateIndex, width, height);

                        CardType temporaryType = cards[cellIndex];
                        cards[cellIndex] = cards[candidateIndex];
                        cards[candidateIndex] = temporaryType;

                        int afterScore = GetLocalConflictScore(cards, cellIndex, width, height) +
                                         GetLocalConflictScore(cards, candidateIndex, width, height);

                        temporaryType = cards[cellIndex];
                        cards[cellIndex] = cards[candidateIndex];
                        cards[candidateIndex] = temporaryType;

                        if (afterScore >= beforeScore || afterScore >= bestScore)
                        {
                            continue;
                        }

                        bestScore = afterScore;
                        bestSwapIndex = candidateIndex;
                        if (bestScore == 0)
                        {
                            break;
                        }
                    }

                    if (bestSwapIndex < 0)
                    {
                        continue;
                    }

                    CardType swapType = cards[cellIndex];
                    cards[cellIndex] = cards[bestSwapIndex];
                    cards[bestSwapIndex] = swapType;
                    hasChanges = true;
                }

                if (!hasChanges)
                {
                    return;
                }
            }
        }

        private static int GetLocalConflictScore(CardType[] cards, int cellIndex, int width, int height)
        {
            return CountAdjacentMatches(cards, cellIndex, width, height);
        }

        private static int CountAdjacentMatches(CardType[] cards, int cellIndex, int width, int height)
        {
            int x = cellIndex % width;
            int y = cellIndex / width;
            CardType type = cards[cellIndex];
            int matches = 0;

            if (x > 0 && cards[cellIndex - 1] == type)
            {
                matches++;
            }

            if (x < width - 1 && cards[cellIndex + 1] == type)
            {
                matches++;
            }

            if (y > 0 && cards[cellIndex - width] == type)
            {
                matches++;
            }

            if (y < height - 1 && cards[cellIndex + width] == type)
            {
                matches++;
            }

            return matches;
        }

        private void SpawnCards(CardType[] typesForCells)
        {
            int cardCount = typesForCells.Length;
            spawnedCards = new CardController[cardCount];
            Transform parentTransform = cardParent != null ? cardParent : transform;

            for (int index = 0; index < cardCount; index++)
            {
                CardType cardType = typesForCells[index];
                CardController cardInstance;
                if (cardFactory != null)
                {
                    cardInstance = cardFactory.CreateCard(cardType, Vector3.zero, Quaternion.identity, parentTransform);
                }
                else
                {
                    CardItemData cardData = cardCatalog.GetDataOrThrow(cardType);
                    cardInstance = Instantiate(cardPrefab, parentTransform);
                    cardInstance.ApplyData(cardData);
                }

                cardInstance.HideCard();
                cardInstance.transform.localScale = cardGridSystem.Settings.CellSize;

                Collider selectionCollider = cardInstance.SelectionCollider;
                if (selectionCollider == null)
                {
                    selectionCollider = cardInstance.GetComponent<Collider>();
                }

                if (selectionCollider != null)
                {
                    selectionCollider.enabled = true;
                }

                cardGridSystem.PlaceAtIndex(cardInstance.transform, index);
                spawnedCards[index] = cardInstance;
            }
        }

        private void ClearSpawnedCards()
        {
            int spawnedCount = spawnedCards.Length;
            for (int index = 0; index < spawnedCount; index++)
            {
                CardController spawnedCard = spawnedCards[index];
                if (spawnedCard == null)
                {
                    continue;
                }

                if (cardFactory != null)
                {
                    cardFactory.ReleaseCard(spawnedCard);
                }
                else
                {
                    Destroy(spawnedCard.gameObject);
                }
            }

            spawnedCards = Array.Empty<CardController>();
            remainingPairs = 0;
        }

        private void EnsureEvenCellCount(
            ref int width,
            ref int height,
            int minWidth,
            int maxWidth,
            int minHeight,
            int maxHeight)
        {
            width = Mathf.Clamp(width, minWidth, maxWidth);
            height = Mathf.Clamp(height, minHeight, maxHeight);

            if (((width * height) % 2) == 0)
            {
                return;
            }

            if (width < maxWidth)
            {
                width += 1;
                return;
            }

            if (width > minWidth)
            {
                width -= 1;
                return;
            }

            if (height < maxHeight)
            {
                height += 1;
                return;
            }

            if (height > minHeight)
            {
                height -= 1;
                return;
            }

            throw new InvalidOperationException(
                "Cannot produce an even cell count from FreeModeSettingsSO range: " +
                "Width[" + minWidth + ".." + maxWidth + "], Height[" + minHeight + ".." + maxHeight + "].");
        }
        #endregion
    }
}
