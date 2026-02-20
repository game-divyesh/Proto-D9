using MatchingPair.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MatchingPair.Gameplay.UI
{
    public sealed class UI_MainMenuScreen : BaseScreen
    {
        #region Fields
        [Header("Buttons")]
        [SerializeField] private Button playButton;

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
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        private void UnregisterButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }
        }

        private void OnPlayClicked()
        {
            CacheDependencies();

            if (levelManager != null)
            {
                int unlockedLevelIndex = GamePrefs.GetUnlockedLevelIndex();
                levelManager.SetProgressionLevelIndex(unlockedLevelIndex);
            }

            if (gameplayManager != null)
            {
                gameplayManager.StartGame(GameMode.Progression);
            }

            if (UIManager.HasInstance)
            {
                UIManager.Instance.ChangeScreen(UIScreenType.Gameplay);
            }
        }
        #endregion
    }
}
