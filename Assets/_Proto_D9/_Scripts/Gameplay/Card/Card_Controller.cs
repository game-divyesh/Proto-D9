using UnityEngine;

namespace MatchingPair.Gameplay.Card
{
    public sealed class Card_Controller : MonoBehaviour
    {
        #region Fields
        [Header("Card")]
        [SerializeField] private SpriteRenderer icon_SR;
        [SerializeField] private Collider collider_Card;

        [Space(5)]
        [SerializeField] private CardType cardType;
        private bool isInPool;
        #endregion

        #region Properties
        public CardType Type => cardType;
        public bool IsInPool => isInPool;
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

            icon_SR.sprite = iconSprite;
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            if (collider_Card != null)
            {
                collider_Card.enabled = isEnabled;
            }
        }
        #endregion

        #region InternalMethods
        internal void MarkTakenFromPool()
        {
            isInPool = false;
        }

        internal void MarkReturnedToPool()
        {
            isInPool = true;
        }
        #endregion

    }// CLASS

}// NAMESPACE
