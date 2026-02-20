using UnityEngine;
using System.Collections;

namespace MatchingPair.Gameplay.Card
{
    public sealed class Card_Controller : MonoBehaviour
    {
        #region Fields
        [Header("Card")]
        [SerializeField] private SpriteRenderer icon_SR;
        [SerializeField] private SpriteRenderer back_SR;
        [SerializeField] private Collider collider_Card;
        [SerializeField, Min(0.01f)] private float flipDuration = 0.15f;

        [Space(5)]
        [SerializeField] private CardType cardType;
        private bool isInPool;
        private bool isMatched;
        private bool isFrontVisible;
        private Coroutine flipCoroutine;
        #endregion

        #region Properties
        public CardType Type => cardType;
        public bool IsInPool => isInPool;
        public Collider SelectionCollider => collider_Card;
        public float FlipDuration => flipDuration;
        public bool IsFlipping => flipCoroutine != null;
        #endregion

        #region UnityCallbacks
        private void Awake()
        {
            CacheReferences();
            EnsureRendererOrder();
        }

        private void OnValidate()
        {
            CacheReferences();
            EnsureRendererOrder();
        }
        #endregion

        #region PublicMethods
        public void ApplyData(CardItem_Data cardData)
        {
            if (cardData == null)
            {
                Debug.LogError("Cannot apply null CardItem_Data on card '" + name + "'.", this);
                return;
            }

            cardType = cardData.Type;
            SetIcon(cardData.GetIconOrNull());
        }

        public void SetIcon(Sprite iconSprite)
        {
            if (icon_SR == null)
            {
                Debug.LogError("Card '" + name + "' is missing SpriteRenderer reference.", this);
                return;
            }

            if (!icon_SR.gameObject.activeSelf)
            {
                icon_SR.gameObject.SetActive(true);
            }

            if (!icon_SR.enabled)
            {
                icon_SR.enabled = true;
            }

            icon_SR.sprite = iconSprite;
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            if (collider_Card != null)
            {
                collider_Card.enabled = isEnabled;
            }
        }

        public void ShowCard()
        {
            StartFlipAnimation(true);
        }

        public void HideCard()
        {
            if (isMatched)
            {
                return;
            }

            StartFlipAnimation(false);
        }

        public void CardMatched()
        {
            isMatched = true;
        }

        public void CardUnmatched()
        {
            isMatched = false;
        }
        #endregion

        #region InternalMethods
        internal void MarkTakenFromPool()
        {
            isInPool = false;
            isMatched = false;
            StopFlipAnimation();
            transform.localEulerAngles = Vector3.zero;
            SetFaceVisual(false);
            transform.localScale = Vector3.one;
        }

        internal void MarkReturnedToPool()
        {
            isInPool = true;
            isMatched = false;
            StopFlipAnimation();
            transform.localEulerAngles = Vector3.zero;
            SetFaceVisual(false);
            transform.localScale = Vector3.one;
        }
        #endregion

        #region PrivateHelpers
        private void StartFlipAnimation(bool showFront)
        {
            if (isFrontVisible == showFront)
            {
                StopFlipAnimation();
                SetRotationY(showFront ? 180f : 0f);
                SetFaceVisual(showFront);
                return;
            }

            StopFlipAnimation();
            flipCoroutine = StartCoroutine(FlipRoutine(showFront));
        }

        private void StopFlipAnimation()
        {
            if (flipCoroutine == null)
            {
                return;
            }

            StopCoroutine(flipCoroutine);
            flipCoroutine = null;
        }

        private IEnumerator FlipRoutine(bool showFront)
        {
            float startY = NormalizeAngle(transform.localEulerAngles.y);
            float targetY = showFront ? 180f : 0f;
            bool startFrontVisible = isFrontVisible;
            SetFaceVisual(startFrontVisible);

            float elapsedTime = 0f;
            bool swappedFace = false;
            while (elapsedTime < flipDuration)
            {
                float normalizedTime = elapsedTime / flipDuration;
                float currentY = Mathf.LerpAngle(startY, targetY, normalizedTime);
                SetRotationY(currentY);

                if (!swappedFace && normalizedTime >= 0.5f)
                {
                    SetFaceVisual(showFront);
                    swappedFace = true;
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            SetRotationY(targetY);
            SetFaceVisual(showFront);
            flipCoroutine = null;
        }

        private void SetFaceVisual(bool showFront)
        {
            if (icon_SR != null)
            {
                if (!icon_SR.gameObject.activeSelf)
                {
                    icon_SR.gameObject.SetActive(true);
                }

                if (!icon_SR.enabled)
                {
                    icon_SR.enabled = true;
                }

                Color iconColor = icon_SR.color;
                iconColor.a = showFront ? 1f : 0f;
                icon_SR.color = iconColor;
            }

            isFrontVisible = showFront;
        }

        private void CacheReferences()
        {
            if (collider_Card == null)
            {
                collider_Card = GetComponent<Collider>();
            }

            if (icon_SR == null)
            {
                icon_SR = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (back_SR == null)
            {
                SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                int rendererCount = spriteRenderers.Length;
                for (int index = 0; index < rendererCount; index++)
                {
                    SpriteRenderer spriteRenderer = spriteRenderers[index];
                    if (spriteRenderer == null || spriteRenderer == icon_SR)
                    {
                        continue;
                    }

                    back_SR = spriteRenderer;
                    break;
                }
            }
        }

        private void EnsureRendererOrder()
        {
            if (icon_SR == null || back_SR == null)
            {
                return;
            }

            if (icon_SR.sortingLayerID != back_SR.sortingLayerID)
            {
                icon_SR.sortingLayerID = back_SR.sortingLayerID;
            }

            if (icon_SR.sortingOrder <= back_SR.sortingOrder)
            {
                icon_SR.sortingOrder = back_SR.sortingOrder + 1;
            }
        }

        private void SetRotationY(float yRotation)
        {
            Vector3 currentRotation = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(currentRotation.x, yRotation, currentRotation.z);
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
            {
                angle -= 360f;
            }

            while (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }
        #endregion

    }// CLASS

}// NAMESPACE
