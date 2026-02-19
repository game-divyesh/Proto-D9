using System;
using UnityEngine;

namespace MatchingPair.Gameplay.Card
{
    [Serializable]
    public sealed class CardItem_Data
    {
        #region Fields
        public string Name;

        [Space]
        public CardType Type;
        public Sprite iconSprite;
        #endregion

        #region Methods
        public void Validate()
        {
            Name = Type.ToString();

#if UNITY_EDITOR
            if (iconSprite == null)
            {
                Debug.LogError("Card icon is missing for type '" + Name + "'.");
            }
#endif
        }

        public Sprite GetIconOrNull()
        {
            return iconSprite;
        }
        #endregion

    }// CLASS

}// NAMESPACE
