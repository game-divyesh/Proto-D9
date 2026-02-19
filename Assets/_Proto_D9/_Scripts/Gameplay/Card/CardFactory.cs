using System;
using UnityEngine;

namespace MatchingPair.Gameplay.Card
{
    public sealed class CardFactory : MonoBehaviour, ICardFactory
    {
        #region Fields
        [SerializeField] private CardItem_Catalog cardCatalog;
        [SerializeField] private Transform poolRoot;
        [SerializeField, Min(0)] private int prewarmCount = 0;

        private ICardPool cardPool;
        private bool isInitialized;
        #endregion

        #region Unity
        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnValidate()
        {
            prewarmCount = Mathf.Max(0, prewarmCount);
        }
        #endregion

        #region PublicMethods
        public Card_Controller CreateCard(CardType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            InitializeIfNeeded();

            CardItem_Data cardData = cardCatalog.GetDataOrThrow(type);
            Card_Controller cardInstance = cardPool.Get(parent, position, rotation);
            cardInstance.ApplyData(cardData);
            return cardInstance;
        }

        public void ReleaseCard(Card_Controller cardInstance)
        {
            if (cardInstance == null)
            {
                return;
            }

            InitializeIfNeeded();
            cardPool.Release(cardInstance);
        }
        #endregion

        #region PrivateMethods
        private void InitializeIfNeeded()
        {
            if (isInitialized)
            {
                return;
            }

            if (cardCatalog == null)
            {
                throw new InvalidOperationException("CardFactory is missing CardItem_Catalog reference on '" + name + "'.");
            }

            Card_Controller prefab = cardCatalog.GetDefaultPrefabOrThrow();
            Transform runtimePoolRoot = poolRoot != null ? poolRoot : transform;

            cardPool = new CardPool(prefab, runtimePoolRoot, prewarmCount);
            if (prewarmCount > 0)
            {
                cardPool.Prewarm(prewarmCount);
            }

            isInitialized = true;
        }
        #endregion
    }
}
