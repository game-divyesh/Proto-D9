using System.Collections.Generic;
using UnityEngine;

namespace MatchingPair.Gameplay.Card
{
    public sealed class CardPool : ICardPool
    {
        #region Fields
        private readonly Card_Controller prefab;
        private readonly Transform poolRoot;
        private readonly Stack<Card_Controller> inactiveCards;
        #endregion

        #region Constructors
        public CardPool(Card_Controller prefab, Transform poolRoot, int initialCapacity)
        {
            if (prefab == null)
            {
                throw new System.ArgumentNullException(nameof(prefab));
            }

            if (poolRoot == null)
            {
                throw new System.ArgumentNullException(nameof(poolRoot));
            }

            this.prefab = prefab;
            this.poolRoot = poolRoot;
            inactiveCards = new Stack<Card_Controller>(Mathf.Max(0, initialCapacity));
        }
        #endregion

        #region PublicMethods
        public Card_Controller Get(Transform parent, Vector3 position, Quaternion rotation)
        {
            Card_Controller cardInstance;
            if (inactiveCards.Count > 0)
            {
                cardInstance = inactiveCards.Pop();
            }
            else
            {
                cardInstance = Object.Instantiate(prefab, poolRoot);
            }

            Transform cardTransform = cardInstance.transform;
            cardTransform.SetParent(parent, false);
            cardTransform.SetPositionAndRotation(position, rotation);

            cardInstance.gameObject.SetActive(true);
            cardInstance.SetInteractionEnabled(true);
            cardInstance.MarkTakenFromPool();
            return cardInstance;
        }

        public void Release(Card_Controller cardInstance)
        {
            if (cardInstance == null || cardInstance.IsInPool)
            {
                return;
            }

            cardInstance.SetInteractionEnabled(false);
            cardInstance.gameObject.SetActive(false);
            cardInstance.transform.SetParent(poolRoot, false);
            cardInstance.MarkReturnedToPool();
            inactiveCards.Push(cardInstance);
        }

        public void Prewarm(int count)
        {
            int cardCount = Mathf.Max(0, count);
            for (int index = 0; index < cardCount; index++)
            {
                Card_Controller cardInstance = Object.Instantiate(prefab, poolRoot);
                cardInstance.SetInteractionEnabled(false);
                cardInstance.gameObject.SetActive(false);
                cardInstance.MarkReturnedToPool();
                inactiveCards.Push(cardInstance);
            }
        }
        #endregion
    }
}
