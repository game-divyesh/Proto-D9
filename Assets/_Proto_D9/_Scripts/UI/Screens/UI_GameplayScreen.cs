using System.Collections;
using MatchingPair.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchingPair.Gameplay.UI
{
    public sealed class UI_GameplayScreen : BaseScreen
    {
        #region Fields
        [Header("Buttons")]
        [SerializeField] private Button pauseButton;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Image levelTimerFillImage;

        [Header("Score Pop Animation")]
        [SerializeField, Min(1f)] private float scorePopScale = 1.15f;
        [SerializeField, Min(0.01f)] private float scorePopDuration = 0.12f;

        [Header("Dependencies")]
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private LevelManager levelManager;

        private Coroutine scorePopCoroutine;
        private Vector3 scoreTextBaseScale = Vector3.one;
        #endregion

        #region UnityCallbacks
        protected override void Awake()
        {
            base.Awake();
            CacheDependencies();
            RegisterButtons();

            if (scoreText != null)
            {
                scoreTextBaseScale = scoreText.rectTransform.localScale;
            }
        }

        private void OnDestroy()
        {
            UnregisterButtons();
            UnsubscribeEvents();
        }
        #endregion

        #region ProtectedHooks
        protected override void OnScreenShown()
        {
            base.OnScreenShown();
            SubscribeEvents();
            RefreshLevelText();
            RefreshScoreText();
            RefreshTimerFill();
        }

        protected override void OnScreenHidden()
        {
            UnsubscribeEvents();
            StopScorePopAnimation();
            base.OnScreenHidden();
        }
        #endregion

        #region PrivateHelpers
        private void CacheDependencies()
        {
            if (gameplayManager == null)
            {
                gameplayManager = FindFirstObjectByType<GameplayManager>();
            }

            if (levelManager == null)
            {
                levelManager = FindFirstObjectByType<LevelManager>();
            }
        }

        private void RegisterButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }
        }

        private void UnregisterButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseClicked);
            }
        }

        private void SubscribeEvents()
        {
            CacheDependencies();

            if (gameplayManager != null)
            {
                gameplayManager.OnScoreChanged -= HandleScoreChanged;
                gameplayManager.OnScoreChanged += HandleScoreChanged;
                gameplayManager.OnLevelTimerChanged -= HandleLevelTimerChanged;
                gameplayManager.OnLevelTimerChanged += HandleLevelTimerChanged;
            }

            if (levelManager != null)
            {
                levelManager.OnLevelBuilt -= HandleLevelBuilt;
                levelManager.OnLevelBuilt += HandleLevelBuilt;
            }
        }

        private void UnsubscribeEvents()
        {
            if (gameplayManager != null)
            {
                gameplayManager.OnScoreChanged -= HandleScoreChanged;
                gameplayManager.OnLevelTimerChanged -= HandleLevelTimerChanged;
            }

            if (levelManager != null)
            {
                levelManager.OnLevelBuilt -= HandleLevelBuilt;
            }
        }

        private void HandleLevelBuilt(LevelContext context)
        {
            RefreshLevelText();
            HandleScoreChanged(0);
            SetTimerFillAmount(1f);
        }

        private void HandleScoreChanged(int scoreValue)
        {
            if (scoreText != null)
            {
                scoreText.text = "Score: " + scoreValue;
                PlayScorePopAnimation();
            }
        }

        private void HandleLevelTimerChanged(float remainingTimeSeconds, float maxTimeSeconds)
        {
            UpdateTimerFill(remainingTimeSeconds, maxTimeSeconds);
        }

        private void RefreshLevelText()
        {
            if (levelNumberText == null || levelManager == null)
            {
                return;
            }

            if (levelManager.CurrentMode == GameMode.Progression)
            {
                levelNumberText.text = "Level " + (levelManager.CurrentProgressionLevelIndex + 1);
                return;
            }

            levelNumberText.text = "Free Mode";
        }

        private void RefreshScoreText()
        {
            int scoreValue = gameplayManager != null ? gameplayManager.CurrentScore : 0;
            HandleScoreChanged(scoreValue);
        }

        private void RefreshTimerFill()
        {
            if (gameplayManager == null)
            {
                SetTimerFillAmount(1f);
                return;
            }

            UpdateTimerFill(gameplayManager.RemainingTimeSeconds, gameplayManager.LevelTimerMaxSeconds);
        }

        private void UpdateTimerFill(float remainingTimeSeconds, float maxTimeSeconds)
        {
            if (maxTimeSeconds <= 0f)
            {
                SetTimerFillAmount(1f);
                return;
            }

            float normalizedFill = Mathf.Clamp01(remainingTimeSeconds / maxTimeSeconds);
            SetTimerFillAmount(normalizedFill);
        }

        private void SetTimerFillAmount(float fillAmount)
        {
            if (levelTimerFillImage == null)
            {
                return;
            }

            levelTimerFillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }

        private void OnPauseClicked()
        {
            if (!UIManager.HasInstance)
            {
                return;
            }

            UIManager.Instance.ShowScreenOnTop(UIScreenType.Pause);
        }

        private void PlayScorePopAnimation()
        {
            if (scoreText == null)
            {
                return;
            }

            StopScorePopAnimation();
            scorePopCoroutine = StartCoroutine(PlayScorePopCoroutine());
        }

        private void StopScorePopAnimation()
        {
            if (scorePopCoroutine != null)
            {
                StopCoroutine(scorePopCoroutine);
                scorePopCoroutine = null;
            }

            if (scoreText != null)
            {
                scoreText.rectTransform.localScale = scoreTextBaseScale;
            }
        }

        private IEnumerator PlayScorePopCoroutine()
        {
            RectTransform scoreRectTransform = scoreText.rectTransform;
            Vector3 popTargetScale = scoreTextBaseScale * scorePopScale;
            float halfDuration = scorePopDuration * 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < halfDuration)
            {
                float normalizedTime = elapsedTime / halfDuration;
                scoreRectTransform.localScale = Vector3.LerpUnclamped(scoreTextBaseScale, popTargetScale, normalizedTime);
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            scoreRectTransform.localScale = popTargetScale;
            elapsedTime = 0f;

            while (elapsedTime < halfDuration)
            {
                float normalizedTime = elapsedTime / halfDuration;
                scoreRectTransform.localScale = Vector3.LerpUnclamped(popTargetScale, scoreTextBaseScale, normalizedTime);
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            scoreRectTransform.localScale = scoreTextBaseScale;
            scorePopCoroutine = null;
        }
        #endregion
    }
}
