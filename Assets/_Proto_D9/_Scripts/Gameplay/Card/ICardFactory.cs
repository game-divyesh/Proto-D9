using UnityEngine;

namespace MatchingPair.Gameplay.Card
{
    public interface ICardFactory
    {
        #region Methods
        Card_Controller CreateCard(CardType type, Vector3 position, Quaternion rotation, Transform parent = null);
        void ReleaseCard(Card_Controller cardInstance);
        #endregion
    }
}
