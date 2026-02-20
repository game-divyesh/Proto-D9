using MatchingPair.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MatchingPair.Gameplay.UI
{
    public sealed class UI_GameCompleteScreen : BaseScreen
    {
        #region Fields
        [Header("Buttons")]
        [SerializeField] private Button continueButton;
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
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void UnregisterButtons()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        private void OnContinueClicked()
        {
            CacheDependencies();

            if (gameplayManager != null)
            {
                gameplayManager.ContinueAfterWin();
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
