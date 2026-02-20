using MatchingPair.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MatchingPair.Gameplay.UI
{
    public sealed class UI_PauseScreen : BaseScreen
    {
        #region Fields
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
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

        #region ProtectedHooks
        protected override void OnScreenShown()
        {
            base.OnScreenShown();
            Time.timeScale = 0f;
        }

        protected override void OnScreenHidden()
        {
            Time.timeScale = 1f;
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
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void UnregisterButtons()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        private void OnResumeClicked()
        {
            if (!UIManager.HasInstance)
            {
                return;
            }

            UIManager.Instance.HideScreen(UIScreenType.Pause);
        }

        private void OnRestartClicked()
        {
            CacheDependencies();

            GameMode modeToRestart = levelManager != null ? levelManager.CurrentMode : GamePrefs.GetLastSelectedMode();
            if (modeToRestart == GameMode.Progression && levelManager != null)
            {
                int unlockedLevelIndex = GamePrefs.GetUnlockedLevelIndex();
                levelManager.SetProgressionLevelIndex(unlockedLevelIndex);
            }

            if (gameplayManager != null)
            {
                gameplayManager.StartGame(modeToRestart);
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
