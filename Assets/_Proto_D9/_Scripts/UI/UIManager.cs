using System;
using System.Collections.Generic;
using UnityEngine;
using MatchingPair.Gameplay;

namespace MatchingPair.Gameplay.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [Serializable]
        private struct ScreenBinding
        {
            #region Fields
            [SerializeField] public UIScreenType ScreenType;
            [SerializeField] public BaseScreen Screen;
            #endregion
        }

        #region Fields
        [Header("Setup")]
        [SerializeField] private UIScreenType defaultStartingScreen = UIScreenType.MainMenu;
        [SerializeField] private ScreenBinding[] screenBindings = Array.Empty<ScreenBinding>();
        [SerializeField] private GameplayManager gameplayManager;

        private readonly Dictionary<UIScreenType, BaseScreen> screenLookup = new Dictionary<UIScreenType, BaseScreen>(16);
        private readonly List<BaseScreen> showingScreens = new List<BaseScreen>(8);
        private bool isInitialized;

        public static UIManager Instance { get; private set; }
        public static bool HasInstance => Instance != null;
        #endregion

        #region Properties
        public UIScreenType DefaultStartingScreen => defaultStartingScreen;
        #endregion

        #region UnityCallbacks
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Multiple UIManager instances found. Keeping the first one.", this);
                return;
            }

            Instance = this;
            Initialize();
        }

        private void OnEnable()
        {
            if (Instance != this)
            {
                return;
            }

            SubscribeToGameplayEvents();
        }

        private void Start()
        {
            if (Instance != this)
            {
                return;
            }

            if (defaultStartingScreen != UIScreenType.None)
            {
                ChangeScreen(defaultStartingScreen);
            }
        }

        private void OnDisable()
        {
            if (Instance != this)
            {
                return;
            }

            UnsubscribeFromGameplayEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region PublicAPI
        public void ChangeScreen(UIScreenType screenType)
        {
            Initialize();
            HideAllShowingScreens();
            ShowScreenInternal(screenType, true);
        }

        public void ShowScreenOnTop(UIScreenType screenType)
        {
            Initialize();
            ShowScreenInternal(screenType, true);
        }

        public void HideScreen(UIScreenType screenType)
        {
            Initialize();
            if (!screenLookup.TryGetValue(screenType, out BaseScreen screen) || screen == null)
            {
                return;
            }

            if (!screen.IsVisible)
            {
                return;
            }

            screen.Hide();
            RemoveFromShowing(screen);
        }

        public void HideAllScreens()
        {
            Initialize();
            HideAllShowingScreens();
        }

        public BaseScreen GetScreen(UIScreenType screenType)
        {
            Initialize();
            screenLookup.TryGetValue(screenType, out BaseScreen screen);
            return screen;
        }
        #endregion

        #region PrivateHelpers
        private void SubscribeToGameplayEvents()
        {
            if (gameplayManager == null)
            {
                gameplayManager = FindFirstObjectByType<GameplayManager>();
            }

            if (gameplayManager == null)
            {
                return;
            }

            gameplayManager.OnGameWon += HandleGameWon;
            gameplayManager.OnGameLost += HandleGameLost;
        }

        private void UnsubscribeFromGameplayEvents()
        {
            if (gameplayManager == null)
            {
                return;
            }

            gameplayManager.OnGameWon -= HandleGameWon;
            gameplayManager.OnGameLost -= HandleGameLost;
        }

        private void HandleGameWon()
        {
            ShowScreenOnTop(UIScreenType.GameComplete);
        }

        private void HandleGameLost()
        {
            ShowScreenOnTop(UIScreenType.GameOver);
        }

        private void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            screenLookup.Clear();
            showingScreens.Clear();

            int bindingCount = screenBindings.Length;
            for (int index = 0; index < bindingCount; index++)
            {
                ScreenBinding binding = screenBindings[index];
                if (binding.Screen == null)
                {
                    continue;
                }

                if (screenLookup.ContainsKey(binding.ScreenType))
                {
                    Debug.LogError("Duplicate screen type in UIManager: " + binding.ScreenType, this);
                    continue;
                }

                if (binding.Screen.ScreenType != binding.ScreenType)
                {
                    Debug.LogWarning(
                        "UIManager binding mismatch: binding type '" + binding.ScreenType +
                        "' differs from BaseScreen type '" + binding.Screen.ScreenType + "'.",
                        binding.Screen);
                }

                screenLookup.Add(binding.ScreenType, binding.Screen);
                binding.Screen.Hide();
            }

            isInitialized = true;
        }

        private void ShowScreenInternal(UIScreenType screenType, bool setAsTop)
        {
            if (screenType == UIScreenType.None)
            {
                return;
            }

            if (!screenLookup.TryGetValue(screenType, out BaseScreen screen) || screen == null)
            {
                Debug.LogError("UIManager could not find screen for type: " + screenType, this);
                return;
            }

            screen.Show(setAsTop);
            AddToShowingIfMissing(screen);
        }

        private void HideAllShowingScreens()
        {
            for (int index = showingScreens.Count - 1; index >= 0; index--)
            {
                BaseScreen screen = showingScreens[index];
                if (screen == null)
                {
                    continue;
                }

                screen.Hide();
            }

            showingScreens.Clear();
        }

        private void AddToShowingIfMissing(BaseScreen screen)
        {
            int showingCount = showingScreens.Count;
            for (int index = 0; index < showingCount; index++)
            {
                if (showingScreens[index] == screen)
                {
                    return;
                }
            }

            showingScreens.Add(screen);
        }

        private void RemoveFromShowing(BaseScreen screen)
        {
            int showingCount = showingScreens.Count;
            for (int index = 0; index < showingCount; index++)
            {
                if (showingScreens[index] != screen)
                {
                    continue;
                }

                showingScreens.RemoveAt(index);
                return;
            }
        }
        #endregion
    }
}
