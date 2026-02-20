using System;
using System.Collections;
using MatchingPair.Gameplay.Levels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using CardController = MatchingPair.Gameplay.Card.Card_Controller;

namespace MatchingPair.Gameplay
{
    public sealed class GameplayManager : MonoBehaviour
    {
        public enum GameState
        {
            None = 0,
            Playing = 1,
            Won = 2,
            Lost = 3
        }

        #region Fields
        [Header("Dependencies")]
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private RatingSettingsSO ratingSettings;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private ScoreComboSettingsSO scoreComboSettings;
        [SerializeField] private FloatingTextFeedbackSettingsSO floatingTextFeedbackSettings;
        [SerializeField] private FloatingTextManager floatingTextManager;
        [SerializeField] private bool autoBuildLevelOnStart = false;

        [Header("Input")]
        [SerializeField, Min(0f)] private float selectionCastRadius = 0.05f;
        [SerializeField, Min(0.1f)] private float selectionMaxDistance = 100f;
        [SerializeField] private LayerMask selectionLayerMask = ~0;

        [Header("Animation")]
        [SerializeField] private GameplayAnimationSettingsSO animationSettings;
        [SerializeField, Min(0f)] private float levelStartRevealSeconds = 3f;
        [SerializeField, Min(0f)] private float gameCompleteDelaySeconds = 1f;

        private LevelContext currentLevelContext;
        private CardController firstSelectedCard;
        private CardController secondSelectedCard;
        private bool isResolvingSelection;
        private bool hasInitializedFreeTimer;
        private float remainingTimeSeconds;
        private float levelTimerMaxSeconds;
        private int openAttemptCount;
        private GameState currentGameState;
        private Coroutine levelStartPreviewCoroutine;
        private Coroutine pendingGameWonCoroutine;
        private readonly RaycastHit[] selectionHitBuffer = new RaycastHit[24];
        private readonly ScoreComboTracker scoreComboTracker = new ScoreComboTracker();
        #endregion

        #region Events
        public event Action OnGameWon;
        public event Action OnGameLost;
        public event Action<int> OnStarsCalculated;
        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnScoreChanged;
        public event Action<int, float> OnComboChanged;
        public event Action<float, float> OnLevelTimerChanged;
        #endregion

        #region Properties
        public GameState CurrentState => currentGameState;
        public float RemainingTimeSeconds => remainingTimeSeconds;
        public float LevelTimerMaxSeconds => levelTimerMaxSeconds;
        public bool HasLevelTimer => levelTimerMaxSeconds > 0f;
        public int OpenAttemptCount => openAttemptCount;
        public int CurrentScore => scoreComboTracker.CurrentScore;
        public int CurrentComboCount => scoreComboTracker.CurrentComboCount;
        #endregion

        #region UnityCallbacks
        private void Awake()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            SubscribeToLevelManagerEvents();
        }

        private void Start()
        {
            if (levelManager == null)
            {
                return;
            }

            if (autoBuildLevelOnStart && levelManager.CurrentLevelContext == null)
            {
                levelManager.BuildCurrentModeLevel();
                return;
            }

            if (currentLevelContext == null && levelManager.CurrentLevelContext != null)
            {
                HandleLevelBuilt(levelManager.CurrentLevelContext);
            }
        }

        private void OnDisable()
        {
            StopLevelStartPreview();
            StopPendingGameWonRoutine();
            UnsubscribeFromLevelManagerEvents();
        }

        private void Update()
        {
            if (currentGameState != GameState.Playing)
            {
                return;
            }

            ProcessSelectionInput();
            UpdateTimerForCurrentMode();
            UpdateScoreComboTimer();
        }
        #endregion

        #region PublicAPI
        public void StartGame(GameMode mode)
        {
            StopPendingGameWonRoutine();
            hasInitializedFreeTimer = false;
            firstSelectedCard = null;
            secondSelectedCard = null;
            isResolvingSelection = false;
            openAttemptCount = 0;
            remainingTimeSeconds = 0f;
            levelTimerMaxSeconds = 0f;
            scoreComboTracker.Reset(scoreComboSettings);
            NotifyScoreComboChanged();
            NotifyLevelTimerChanged();

            if (levelManager != null)
            {
                levelManager.BuildLevel(mode);
            }
        }

        public void ContinueAfterWin()
        {
            if (levelManager == null || currentLevelContext == null)
            {
                return;
            }

            if (currentGameState != GameState.Won)
            {
                return;
            }

            if (currentLevelContext.Mode == GameMode.Progression)
            {
                levelManager.BuildNextProgressionLevel();
                return;
            }

            levelManager.BuildLevel(GameMode.FreeToPlay);
        }
        #endregion

        #region Input
        private void ProcessSelectionInput()
        {
            if (isResolvingSelection)
            {
                return;
            }

            if (gameplayCamera == null)
            {
                return;
            }

            if (!TryGetPointerDownScreenPosition(out Vector2 screenPosition))
            {
                return;
            }

            Ray selectionRay = gameplayCamera.ScreenPointToRay(screenPosition);
            if (!TryGetCardFromRay(selectionRay, out CardController selectedCard))
            {
                return;
            }

            if (!CanSelectCard(selectedCard))
            {
                return;
            }

            SelectCard(selectedCard);
        }

        private bool TryGetPointerDownScreenPosition(out Vector2 screenPosition)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touches = touchscreen.touches;
                int touchCount = touches.Count;
                for (int index = 0; index < touchCount; index++)
                {
                    TouchControl touch = touches[index];
                    if (touch == null || !touch.press.wasPressedThisFrame)
                    {
                        continue;
                    }

                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }

            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                screenPosition = pointer.position.ReadValue();
                return true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }
#endif

            screenPosition = default;
            return false;
        }

        private void UpdateTimerForCurrentMode()
        {
            if (currentLevelContext == null)
            {
                return;
            }

            bool hasTimedProgression = currentLevelContext.Mode == GameMode.Progression && levelTimerMaxSeconds > 0f;
            if (currentLevelContext.Mode != GameMode.FreeToPlay && !hasTimedProgression)
            {
                return;
            }

            remainingTimeSeconds -= Time.deltaTime;
            if (remainingTimeSeconds > 0f)
            {
                NotifyLevelTimerChanged();
                return;
            }

            if (remainingTimeSeconds <= 0f)
            {
                remainingTimeSeconds = 0f;
                NotifyLevelTimerChanged();
                HandleGameLost();
            }
        }
        #endregion

        #region LevelEvents
        private void SubscribeToLevelManagerEvents()
        {
            if (levelManager == null)
            {
                return;
            }

            levelManager.OnLevelBuilt += HandleLevelBuilt;
            levelManager.OnAllPairsSolved += HandleAllPairsSolved;
        }

        private void UnsubscribeFromLevelManagerEvents()
        {
            if (levelManager == null)
            {
                return;
            }

            levelManager.OnLevelBuilt -= HandleLevelBuilt;
            levelManager.OnAllPairsSolved -= HandleAllPairsSolved;
        }

        private void HandleLevelBuilt(LevelContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("GameplayManager received null LevelContext. Level build was skipped.");
                return;
            }

            bool hasPreviousContext = currentLevelContext != null;
            bool modeChanged = hasPreviousContext && currentLevelContext.Mode != context.Mode;
            if (modeChanged)
            {
                hasInitializedFreeTimer = false;
            }

            currentLevelContext = context;
            firstSelectedCard = null;
            secondSelectedCard = null;
            isResolvingSelection = false;
            openAttemptCount = 0;
            scoreComboTracker.Reset(scoreComboSettings);
            NotifyScoreComboChanged();

            if (context.Mode == GameMode.FreeToPlay)
            {
                levelTimerMaxSeconds = Mathf.Max(0f, context.StartTimeSeconds);
                if (!hasInitializedFreeTimer)
                {
                    remainingTimeSeconds = levelTimerMaxSeconds;
                    hasInitializedFreeTimer = true;
                }
            }
            else
            {
                hasInitializedFreeTimer = false;
                levelTimerMaxSeconds = Mathf.Max(0f, context.StartTimeSeconds);
                remainingTimeSeconds = levelTimerMaxSeconds;
            }
            NotifyLevelTimerChanged();

            StopLevelStartPreview();
            levelStartPreviewCoroutine = StartCoroutine(PlayLevelStartPreview());
        }

        private void HandleAllPairsSolved()
        {
            if (currentGameState != GameState.Playing)
            {
                return;
            }

            SetGameState(GameState.None);
            StopPendingGameWonRoutine();
            pendingGameWonCoroutine = StartCoroutine(HandleGameWonAfterDelay());
        }
        #endregion

        #region SelectionFlow
        private bool TryGetCardFromRay(Ray ray, out CardController cardController)
        {
            int hitCount;
            if (selectionCastRadius > 0f)
            {
                hitCount = Physics.SphereCastNonAlloc(
                    ray,
                    selectionCastRadius,
                    selectionHitBuffer,
                    selectionMaxDistance,
                    selectionLayerMask,
                    QueryTriggerInteraction.Ignore);
            }
            else
            {
                hitCount = Physics.RaycastNonAlloc(
                    ray,
                    selectionHitBuffer,
                    selectionMaxDistance,
                    selectionLayerMask,
                    QueryTriggerInteraction.Ignore);
            }

            if (hitCount <= 0)
            {
                cardController = null;
                return false;
            }

            float closestDistance = float.MaxValue;
            CardController closestCard = null;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hitInfo = selectionHitBuffer[index];
                Collider hitCollider = hitInfo.collider;
                if (hitCollider == null)
                {
                    continue;
                }

                CardController directCard = hitCollider.GetComponent<CardController>();
                CardController candidateCard = directCard != null
                    ? directCard
                    : hitCollider.GetComponentInParent<CardController>();

                if (candidateCard == null)
                {
                    continue;
                }

                float hitDistance = hitInfo.distance;
                if (hitDistance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hitDistance;
                closestCard = candidateCard;
            }

            if (closestCard == null)
            {
                cardController = null;
                return false;
            }

            cardController = closestCard;
            return true;
        }

        private bool CanSelectCard(CardController cardController)
        {
            if (cardController == null || !cardController.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (cardController == firstSelectedCard || cardController == secondSelectedCard)
            {
                return false;
            }

            Collider selectionCollider = cardController.SelectionCollider;
            if (selectionCollider == null)
            {
                selectionCollider = cardController.GetComponent<Collider>();
            }

            if (selectionCollider == null || !selectionCollider.enabled)
            {
                return false;
            }

            return true;
        }

        private void SelectCard(CardController selectedCard)
        {
            openAttemptCount++;
            selectedCard.ShowCard();

            if (firstSelectedCard == null)
            {
                firstSelectedCard = selectedCard;
                return;
            }

            secondSelectedCard = selectedCard;
            isResolvingSelection = true;
            StartCoroutine(ResolveSelectedCards());
        }

        private IEnumerator ResolveSelectedCards()
        {
            CardController firstCard = firstSelectedCard;
            CardController secondCard = secondSelectedCard;

            if (firstCard == null || secondCard == null)
            {
                ResetSelectionFlow();
                yield break;
            }

            yield return WaitForSelectedCardsFlipComplete(firstCard, secondCard);

            if (firstCard == null || secondCard == null)
            {
                ResetSelectionFlow();
                yield break;
            }

            bool isMatch = firstCard.Type == secondCard.Type;
            if (isMatch)
            {
                firstCard.CardMatched();
                secondCard.CardMatched();

                SetCardSelectionEnabled(firstCard, false);
                SetCardSelectionEnabled(secondCard, false);
                RegisterScoreForMatch(firstCard, secondCard);

                yield return WaitForDelay(GetMatchAnimationDelay());
                yield return AnimateMatchSolve(
                    firstCard.transform,
                    secondCard.transform,
                    GetMatchScaleUpDuration(),
                    GetMatchScaleOutDuration(),
                    GetMatchScaleUpMultiplier());

                SetCardActive(firstCard, false);
                SetCardActive(secondCard, false);

                if (levelManager != null)
                {
                    levelManager.NotifyPairSolved();
                }
            }
            else
            {
                firstCard.CardUnmatched();
                secondCard.CardUnmatched();

                yield return AnimateMismatchNudge(
                    firstCard.transform,
                    secondCard.transform,
                    GetMismatchNudgeDuration(),
                    GetMismatchNudgeScaleStrength(),
                    GetMismatchNudgeRotationDegrees(),
                    GetMismatchNudgeFrequency());
                yield return WaitForDelay(GetMismatchFlipDelay());

                if (firstCard != null)
                {
                    firstCard.HideCard();
                }

                if (secondCard != null)
                {
                    secondCard.HideCard();
                }

                float hideFlipDuration = GetMaxCardFlipDuration(firstCard, secondCard);
                if (hideFlipDuration > 0f)
                {
                    yield return WaitForDelay(hideFlipDuration);
                }
            }

            ResetSelectionFlow();
        }

        private IEnumerator WaitForSelectedCardsFlipComplete(CardController firstCard, CardController secondCard)
        {
            float timeout = Mathf.Max(0.2f, GetMaxCardFlipDuration(firstCard, secondCard) + 0.25f);
            float elapsedTime = 0f;

            while (elapsedTime < timeout)
            {
                bool firstStillFlipping = firstCard != null && firstCard.IsFlipping;
                bool secondStillFlipping = secondCard != null && secondCard.IsFlipping;
                if (!firstStillFlipping && !secondStillFlipping)
                {
                    yield break;
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        private void ResetSelectionFlow()
        {
            firstSelectedCard = null;
            secondSelectedCard = null;
            isResolvingSelection = false;
        }

        private void RegisterScoreForMatch(CardController firstCard, CardController secondCard)
        {
            if (currentLevelContext == null)
            {
                return;
            }

            if (scoreComboSettings == null)
            {
                return;
            }

            ScoreComboResult result = scoreComboTracker.RegisterMatch(Time.time, scoreComboSettings);
            if (result.ScoreGained <= 0)
            {
                NotifyScoreComboChanged();
                return;
            }

            NotifyScoreComboChanged();
            ShowMatchFloatingText(result, firstCard, secondCard);
        }

        private void UpdateScoreComboTimer()
        {
            if (currentLevelContext == null)
            {
                return;
            }

            bool comboBroken = scoreComboTracker.Tick(Time.time, scoreComboSettings);
            if (!comboBroken)
            {
                return;
            }

            NotifyScoreComboChanged();
        }

        private void NotifyScoreComboChanged()
        {
            OnScoreChanged?.Invoke(scoreComboTracker.CurrentScore);
            OnComboChanged?.Invoke(
                scoreComboTracker.CurrentComboCount,
                scoreComboTracker.GetRemainingComboTime(Time.time));
        }

        private void NotifyLevelTimerChanged()
        {
            OnLevelTimerChanged?.Invoke(remainingTimeSeconds, levelTimerMaxSeconds);
        }

        private void ShowMatchFloatingText(ScoreComboResult result, CardController firstCard, CardController secondCard)
        {
            if (floatingTextManager == null)
            {
                return;
            }

            Vector3 pairCenter = GetPairCenter(firstCard, secondCard);
            Vector3 scoreOffset = floatingTextFeedbackSettings != null
                ? floatingTextFeedbackSettings.ScoreTextOffset
                : new Vector3(0f, 0.35f, 0f);
            floatingTextManager.ShowText(
                "+" + result.ScoreGained,
                pairCenter + scoreOffset,
                GetScoreTextColor(),
                GetScoreTextLifetime());

            if (result.ComboCount <= 0)
            {
                return;
            }

            Vector3 comboOffset = floatingTextFeedbackSettings != null
                ? floatingTextFeedbackSettings.ComboTextOffset
                : new Vector3(0f, 0.75f, 0f);
            floatingTextManager.ShowText(
                "Combo x" + result.ComboCount,
                pairCenter + comboOffset,
                GetComboTextColor(),
                GetComboTextLifetime());
        }

        private static Vector3 GetPairCenter(CardController firstCard, CardController secondCard)
        {
            if (firstCard == null && secondCard == null)
            {
                return Vector3.zero;
            }

            if (firstCard == null)
            {
                return secondCard.transform.position;
            }

            if (secondCard == null)
            {
                return firstCard.transform.position;
            }

            return (firstCard.transform.position + secondCard.transform.position) * 0.5f;
        }

        private Color GetScoreTextColor()
        {
            if (floatingTextFeedbackSettings == null)
            {
                return Color.white;
            }

            return floatingTextFeedbackSettings.ScoreTextColor;
        }

        private Color GetComboTextColor()
        {
            if (floatingTextFeedbackSettings == null)
            {
                return new Color(1f, 0.9f, 0.2f, 1f);
            }

            return floatingTextFeedbackSettings.ComboTextColor;
        }

        private float GetScoreTextLifetime()
        {
            if (floatingTextFeedbackSettings == null)
            {
                return 0.9f;
            }

            return floatingTextFeedbackSettings.ScoreTextLifetime;
        }

        private float GetComboTextLifetime()
        {
            if (floatingTextFeedbackSettings == null)
            {
                return 1f;
            }

            return floatingTextFeedbackSettings.ComboTextLifetime;
        }

        private void SetCardSelectionEnabled(CardController cardController, bool isEnabled)
        {
            if (cardController == null)
            {
                return;
            }

            Collider selectionCollider = cardController.SelectionCollider;
            if (selectionCollider == null)
            {
                selectionCollider = cardController.GetComponent<Collider>();
            }

            if (selectionCollider != null)
            {
                selectionCollider.enabled = isEnabled;
            }
        }
        #endregion

        #region GameState
        private void HandleGameWon()
        {
            StopPendingGameWonRoutine();
            StopLevelStartPreview();

            int stars = 1;
            if (ratingSettings != null && currentLevelContext != null)
            {
                stars = ratingSettings.CalculateStars(currentLevelContext.TotalCards, openAttemptCount);
            }

            if (currentLevelContext != null && currentLevelContext.Mode == GameMode.FreeToPlay)
            {
                remainingTimeSeconds += currentLevelContext.ClearRewardSeconds;
                NotifyLevelTimerChanged();
            }

            SetGameState(GameState.Won);
            OnGameWon?.Invoke();
            OnStarsCalculated?.Invoke(stars);

            if (levelManager == null || currentLevelContext == null)
            {
                return;
            }

            if (currentLevelContext.Mode == GameMode.Progression)
            {
                levelManager.UnlockNextProgressionLevel();
            }
        }

        private void HandleGameLost()
        {
            StopPendingGameWonRoutine();
            StopLevelStartPreview();

            if (currentGameState == GameState.Lost)
            {
                return;
            }

            SetGameState(GameState.Lost);
            NotifyLevelTimerChanged();
            OnGameLost?.Invoke();
        }

        private void SetGameState(GameState newState)
        {
            if (currentGameState == newState)
            {
                return;
            }

            currentGameState = newState;
            OnGameStateChanged?.Invoke(currentGameState);
        }

        private IEnumerator HandleGameWonAfterDelay()
        {
            if (gameCompleteDelaySeconds > 0f)
            {
                yield return WaitForDelay(gameCompleteDelaySeconds);
            }

            pendingGameWonCoroutine = null;
            HandleGameWon();
        }

        private void StopPendingGameWonRoutine()
        {
            if (pendingGameWonCoroutine == null)
            {
                return;
            }

            StopCoroutine(pendingGameWonCoroutine);
            pendingGameWonCoroutine = null;
        }
        #endregion

        #region LevelStartPreview
        private IEnumerator PlayLevelStartPreview()
        {
            if (levelStartRevealSeconds <= 0f)
            {
                SetAllCardsHiddenInstant();
                SetAllCardsSelectionEnabled(true);
                SetGameState(GameState.Playing);
                levelStartPreviewCoroutine = null;
                yield break;
            }

            SetGameState(GameState.None);
            SetAllCardsSelectionEnabled(false);
            SetAllCardsHiddenInstant();
            yield return null;
            ShowAllCards();

            float revealFlipDuration = GetMaxCardFlipDuration();
            if (revealFlipDuration > 0f)
            {
                yield return WaitForDelay(revealFlipDuration);
            }

            yield return WaitForDelay(levelStartRevealSeconds);

            HideAllCards();
            float hideAnimationDuration = GetMaxCardFlipDuration();
            if (hideAnimationDuration > 0f)
            {
                yield return WaitForDelay(hideAnimationDuration);
            }

            SetAllCardsSelectionEnabled(true);
            SetGameState(GameState.Playing);
            levelStartPreviewCoroutine = null;
        }

        private void StopLevelStartPreview()
        {
            if (levelStartPreviewCoroutine == null)
            {
                return;
            }

            StopCoroutine(levelStartPreviewCoroutine);
            levelStartPreviewCoroutine = null;
        }

        private void ShowAllCards()
        {
            if (levelManager == null)
            {
                return;
            }

            System.Collections.Generic.IReadOnlyList<CardController> cards = levelManager.SpawnedCards;
            int cardCount = cards.Count;
            for (int index = 0; index < cardCount; index++)
            {
                CardController card = cards[index];
                if (card == null)
                {
                    continue;
                }

                card.ShowCard();
            }
        }

        private void HideAllCards()
        {
            if (levelManager == null)
            {
                return;
            }

            System.Collections.Generic.IReadOnlyList<CardController> cards = levelManager.SpawnedCards;
            int cardCount = cards.Count;
            for (int index = 0; index < cardCount; index++)
            {
                CardController card = cards[index];
                if (card == null)
                {
                    continue;
                }

                card.HideCard();
            }
        }

        private void SetAllCardsHiddenInstant()
        {
            if (levelManager == null)
            {
                return;
            }

            System.Collections.Generic.IReadOnlyList<CardController> cards = levelManager.SpawnedCards;
            int cardCount = cards.Count;
            for (int index = 0; index < cardCount; index++)
            {
                CardController card = cards[index];
                if (card == null)
                {
                    continue;
                }

                card.SetCardFaceInstant(false);
            }
        }

        private void SetAllCardsSelectionEnabled(bool isEnabled)
        {
            if (levelManager == null)
            {
                return;
            }

            System.Collections.Generic.IReadOnlyList<CardController> cards = levelManager.SpawnedCards;
            int cardCount = cards.Count;
            for (int index = 0; index < cardCount; index++)
            {
                SetCardSelectionEnabled(cards[index], isEnabled);
            }
        }

        private float GetMaxCardFlipDuration()
        {
            if (levelManager == null)
            {
                return 0f;
            }

            float maxDuration = 0f;
            System.Collections.Generic.IReadOnlyList<CardController> cards = levelManager.SpawnedCards;
            int cardCount = cards.Count;
            for (int index = 0; index < cardCount; index++)
            {
                CardController card = cards[index];
                if (card == null)
                {
                    continue;
                }

                float cardFlipDuration = card.FlipDuration;
                if (cardFlipDuration > maxDuration)
                {
                    maxDuration = cardFlipDuration;
                }
            }

            return maxDuration;
        }
        #endregion

        #region Animations
        private float GetMatchAnimationDelay()
        {
            if (animationSettings == null)
            {
                return 0.05f;
            }

            return animationSettings.MatchAnimationDelay;
        }

        private float GetMatchScaleUpDuration()
        {
            if (animationSettings == null)
            {
                return 0.12f;
            }

            return animationSettings.MatchScaleUpDuration;
        }

        private float GetMatchScaleOutDuration()
        {
            if (animationSettings == null)
            {
                return 0.18f;
            }

            return animationSettings.MatchScaleOutDuration;
        }

        private float GetMatchScaleUpMultiplier()
        {
            if (animationSettings == null)
            {
                return 1.15f;
            }

            return animationSettings.MatchScaleUpMultiplier;
        }

        private float GetMismatchNudgeDuration()
        {
            if (animationSettings == null)
            {
                return 1f;
            }

            return animationSettings.MismatchNudgeDuration;
        }

        private float GetMismatchNudgeScaleStrength()
        {
            if (animationSettings == null)
            {
                return 0.08f;
            }

            return animationSettings.MismatchNudgeScaleStrength;
        }

        private float GetMismatchNudgeRotationDegrees()
        {
            if (animationSettings == null)
            {
                return 12f;
            }

            return animationSettings.MismatchNudgeRotationDegrees;
        }

        private float GetMismatchNudgeFrequency()
        {
            if (animationSettings == null)
            {
                return 6f;
            }

            return animationSettings.MismatchNudgeFrequency;
        }

        private float GetMismatchFlipDelay()
        {
            if (animationSettings == null)
            {
                return 0f;
            }

            return animationSettings.MismatchFlipDelay;
        }

        private IEnumerator AnimateMatchSolve(
            Transform firstTransform,
            Transform secondTransform,
            float scaleUpDuration,
            float scaleOutDuration,
            float scaleUpMultiplier)
        {
            Vector3 firstBaseScale = firstTransform != null ? firstTransform.localScale : Vector3.one;
            Vector3 secondBaseScale = secondTransform != null ? secondTransform.localScale : Vector3.one;
            Vector3 firstScaleUpTarget = firstBaseScale * scaleUpMultiplier;
            Vector3 secondScaleUpTarget = secondBaseScale * scaleUpMultiplier;

            if (scaleUpDuration > 0f)
            {
                float elapsedScaleUp = 0f;
                while (elapsedScaleUp < scaleUpDuration)
                {
                    float normalizedScaleUp = elapsedScaleUp / scaleUpDuration;
                    if (firstTransform != null)
                    {
                        firstTransform.localScale = Vector3.LerpUnclamped(firstBaseScale, firstScaleUpTarget, normalizedScaleUp);
                    }

                    if (secondTransform != null)
                    {
                        secondTransform.localScale = Vector3.LerpUnclamped(secondBaseScale, secondScaleUpTarget, normalizedScaleUp);
                    }

                    elapsedScaleUp += Time.deltaTime;
                    yield return null;
                }
            }

            if (firstTransform != null)
            {
                firstTransform.localScale = firstScaleUpTarget;
            }

            if (secondTransform != null)
            {
                secondTransform.localScale = secondScaleUpTarget;
            }

            if (scaleOutDuration <= 0f)
            {
                if (firstTransform != null)
                {
                    firstTransform.localScale = Vector3.zero;
                }

                if (secondTransform != null)
                {
                    secondTransform.localScale = Vector3.zero;
                }

                yield break;
            }

            float elapsedScaleOut = 0f;
            while (elapsedScaleOut < scaleOutDuration)
            {
                float normalizedScaleOut = elapsedScaleOut / scaleOutDuration;
                if (firstTransform != null)
                {
                    firstTransform.localScale = Vector3.LerpUnclamped(firstScaleUpTarget, Vector3.zero, normalizedScaleOut);
                }

                if (secondTransform != null)
                {
                    secondTransform.localScale = Vector3.LerpUnclamped(secondScaleUpTarget, Vector3.zero, normalizedScaleOut);
                }

                elapsedScaleOut += Time.deltaTime;
                yield return null;
            }

            if (firstTransform != null)
            {
                firstTransform.localScale = Vector3.zero;
            }

            if (secondTransform != null)
            {
                secondTransform.localScale = Vector3.zero;
            }
        }

        private IEnumerator AnimateMismatchNudge(
            Transform firstTransform,
            Transform secondTransform,
            float duration,
            float scaleStrength,
            float rotationDegrees,
            float frequency)
        {
            if (duration <= 0f)
            {
                yield break;
            }

            Vector3 firstBaseScale = firstTransform != null ? firstTransform.localScale : Vector3.one;
            Vector3 secondBaseScale = secondTransform != null ? secondTransform.localScale : Vector3.one;
            Vector3 firstBaseEuler = firstTransform != null ? firstTransform.localEulerAngles : Vector3.zero;
            Vector3 secondBaseEuler = secondTransform != null ? secondTransform.localEulerAngles : Vector3.zero;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float normalizedTime = elapsedTime / duration;
                float decay = 1f - normalizedTime;
                float wave = Mathf.Sin(normalizedTime * Mathf.PI * 2f * frequency);
                float rotationOffset = wave * rotationDegrees * decay;
                float scaleMultiplier = 1f + (Mathf.Abs(wave) * scaleStrength * decay);

                if (firstTransform != null)
                {
                    firstTransform.localScale = firstBaseScale * scaleMultiplier;
                    firstTransform.localEulerAngles = new Vector3(
                        firstBaseEuler.x,
                        firstBaseEuler.y,
                        firstBaseEuler.z + rotationOffset);
                }

                if (secondTransform != null)
                {
                    secondTransform.localScale = secondBaseScale * scaleMultiplier;
                    secondTransform.localEulerAngles = new Vector3(
                        secondBaseEuler.x,
                        secondBaseEuler.y,
                        secondBaseEuler.z - rotationOffset);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (firstTransform != null)
            {
                firstTransform.localScale = firstBaseScale;
                firstTransform.localEulerAngles = firstBaseEuler;
            }

            if (secondTransform != null)
            {
                secondTransform.localScale = secondBaseScale;
                secondTransform.localEulerAngles = secondBaseEuler;
            }
        }

        private static float GetMaxCardFlipDuration(CardController firstCard, CardController secondCard)
        {
            float firstDuration = firstCard != null ? firstCard.FlipDuration : 0f;
            float secondDuration = secondCard != null ? secondCard.FlipDuration : 0f;
            return Mathf.Max(firstDuration, secondDuration);
        }

        private static void SetCardActive(CardController cardController, bool isActive)
        {
            if (cardController == null)
            {
                return;
            }

            GameObject cardGameObject = cardController.gameObject;
            if (cardGameObject != null && cardGameObject.activeSelf != isActive)
            {
                cardGameObject.SetActive(isActive);
            }
        }

        private IEnumerator WaitForDelay(float delay)
        {
            if (delay <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        #endregion
    }
}
