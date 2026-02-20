using MatchingPair.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MatchingPair.Gameplay.UI
{
    public sealed class UI_GameOverScreen : BaseScreen
    {
        #region Fields
        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Dependencies")]
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private LevelManager levelManager;
        #endregion

        #region UnityCallbacks
        protected override void Awake()
        {
            base.Awake();
            CacheDependencies();
            RegisterButtons();
        }

        private void OnDestroy()
        {
            UnregisterButtons();
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
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void UnregisterButtons()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        private void OnRetryClicked()
        {
            CacheDependencies();

            GameMode modeToRetry = levelManager != null ? levelManager.CurrentMode : GamePrefs.GetLastSelectedMode();
            if (modeToRetry == GameMode.Progression && levelManager != null)
            {
                int unlockedLevelIndex = GamePrefs.GetUnlockedLevelIndex();
                levelManager.SetProgressionLevelIndex(unlockedLevelIndex);
            }

            if (gameplayManager != null)
            {
                gameplayManager.StartGame(modeToRetry);
            }

            if (UIManager.HasInstance)
            {
                UIManager.Instance.ChangeScreen(UIScreenType.Gameplay);
            }
        }

        private void OnMainMenuClicked()
        {
            CacheDependencies();

            if (levelManager != null)
            {
                levelManager.UnloadCurrentLevel();
            }

            if (!UIManager.HasInstance)
            {
                return;
            }

            UIManager.Instance.ChangeScreen(UIScreenType.MainMenu);
        }
        #endregion
    }
}
