using UnityEngine;

namespace MatchingPair.Gameplay.Card
{
    public interface ICardPool
    {
        #region Methods
        Card_Controller Get(Transform parent, Vector3 position, Quaternion rotation);
        void Release(Card_Controller cardInstance);
        void Prewarm(int count);
        #endregion
    }
}
