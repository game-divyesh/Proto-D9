using UnityEngine;

namespace MatchingPair.Gameplay.UI
{
    public class BaseScreen : MonoBehaviour
    {
        #region Fields
        [SerializeField] private UIScreenType screenType = UIScreenType.None;
        [SerializeField] private Canvas screenCanvas;
        [SerializeField] private CanvasGroup screenCanvasGroup;

        private bool isVisible;
        #endregion

        #region Properties
        public UIScreenType ScreenType => screenType;
        public Canvas ScreenCanvas => screenCanvas;
        public CanvasGroup ScreenCanvasGroup => screenCanvasGroup;
        public bool IsVisible => isVisible;
        #endregion

        #region UnityCallbacks
        protected virtual void Awake()
        {
            CacheReferences();
        }

        protected virtual void OnValidate()
        {
            CacheReferences();
        }
        #endregion

        #region PublicAPI
        public virtual void Show(bool setAsTop)
        {
            CacheReferences();

            gameObject.SetActive(true);
            if (setAsTop)
            {
                transform.SetAsLastSibling();
            }

            if (screenCanvas != null)
            {
                screenCanvas.enabled = true;
            }

            if (screenCanvasGroup != null)
            {
                screenCanvasGroup.alpha = 1f;
                screenCanvasGroup.interactable = true;
                screenCanvasGroup.blocksRaycasts = true;
            }

            isVisible = true;
            OnScreenShown();
        }

        public virtual void Hide()
        {
            CacheReferences();

            if (screenCanvasGroup != null)
            {
                screenCanvasGroup.alpha = 0f;
                screenCanvasGroup.interactable = false;
                screenCanvasGroup.blocksRaycasts = false;
            }

            if (screenCanvas != null)
            {
                screenCanvas.enabled = false;
            }

            isVisible = false;
            OnScreenHidden();
            gameObject.SetActive(false);
        }
        #endregion

        #region ProtectedHooks
        protected virtual void OnScreenShown()
        {
        }

        protected virtual void OnScreenHidden()
        {
        }
        #endregion

        #region PrivateHelpers
        private void CacheReferences()
        {
            if (screenCanvas == null)
            {
                screenCanvas = GetComponent<Canvas>();
            }

            if (screenCanvasGroup == null)
            {
                screenCanvasGroup = GetComponent<CanvasGroup>();
            }
        }
        #endregion
    }
}
