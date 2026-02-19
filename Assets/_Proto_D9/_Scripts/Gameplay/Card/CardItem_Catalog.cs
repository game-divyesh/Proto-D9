using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchingPair.Gameplay.Card
{
    [CreateAssetMenu(fileName = "CardItem_Catalog_SO", menuName = "MatchingPair/Gameplay/Card Item Catalog")]
    public sealed class CardItem_Catalog : ScriptableObject
    {
        #region Fields
        [SerializeField] private Card_Controller defaultPrefab;
        [Space(10)]
        [SerializeField] private CardItem_Data[] cardData_Array = Array.Empty<CardItem_Data>();

        private Dictionary<CardType, CardItem_Data> dataLookup;

        private bool isLookupReady;
        #endregion


        #region Properties
        public IReadOnlyList<CardItem_Data> Entries => cardData_Array;
        public Card_Controller DefaultPrefab => defaultPrefab;
        #endregion



        #region Unity
        private void OnEnable()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            ValidateEntries();
            BuildLookup();
        }
        #endregion



        #region PublicMethods
        public bool TryGetData(CardType type, out CardItem_Data cardData)
        {
            EnsureLookup();
            return dataLookup.TryGetValue(type, out cardData);
        }

        public bool TryGetPrefab(CardType type, out Card_Controller prefab)
        {
            if (TryGetData(type, out CardItem_Data _))
            {
                prefab = defaultPrefab;
                return prefab != null;
            }

            prefab = null;
            return false;
        }

        public CardItem_Data GetDataOrThrow(CardType type)
        {
            if (TryGetData(type, out CardItem_Data cardData))
            {
                return cardData;
            }

            throw new KeyNotFoundException("No card data found for CardType '" + type + "' in catalog '" + name + "'.");
        }

        public Card_Controller GetDefaultPrefabOrThrow()
        {
            if (defaultPrefab != null)
            {
                return defaultPrefab;
            }

            throw new InvalidOperationException("Default card prefab is missing in catalog '" + name + "'.");
        }

        public Card_Controller GetPrefabOrThrow(CardType type)
        {
            if (!TryGetData(type, out CardItem_Data _))
            {
                throw new KeyNotFoundException("No card data found for CardType '" + type + "' in catalog '" + name + "'.");
            }

            return GetDefaultPrefabOrThrow();
        }
        #endregion

        #region PrivateMethods
        private void EnsureLookup()
        {
            if (isLookupReady)
            {
                return;
            }

            BuildLookup();
        }

        private void BuildLookup()
        {
            if (dataLookup == null)
            {
                int initialCapacity = cardData_Array == null ? 0 : cardData_Array.Length;
                dataLookup = new Dictionary<CardType, CardItem_Data>(initialCapacity);
            }
            else
            {
                dataLookup.Clear();
            }

            if (cardData_Array != null)
            {
                int entryCount = cardData_Array.Length;
                for (int index = 0; index < entryCount; index++)
                {
                    CardItem_Data entry = cardData_Array[index];
                    if (entry == null)
                    {
                        continue;
                    }

                    if (dataLookup.ContainsKey(entry.Type))
                    {
                        continue;
                    }

                    dataLookup.Add(entry.Type, entry);
                }
            }

            isLookupReady = true;
        }

        private void ValidateEntries()
        {
            if (cardData_Array == null)
            {
                cardData_Array = Array.Empty<CardItem_Data>();
                isLookupReady = false;
                return;
            }

#if UNITY_EDITOR
            if (defaultPrefab == null)
            {
                Debug.LogError("Default card prefab is missing in catalog '" + GetAssetIdentifier() + "'.", this);
            }
#endif

            HashSet<CardType> seenTypes = new HashSet<CardType>();
            string assetIdentifier = GetAssetIdentifier();
            int entryCount = cardData_Array.Length;

            for (int index = 0; index < entryCount; index++)
            {
                CardItem_Data entry = cardData_Array[index];
                if (entry == null)
                {
#if UNITY_EDITOR
                    Debug.LogError("Null card entry found in catalog '" + assetIdentifier + "' at index " + index + ".", this);
#endif
                    continue;
                }

                entry.Validate();

#if UNITY_EDITOR
                if (entry.GetIconOrNull() == null)
                {
                    Debug.LogError(
                        "Missing icon for CardType '" + entry.Type + "' in catalog '" + assetIdentifier + "'.",
                        this);
                }
#endif

                if (!seenTypes.Add(entry.Type))
                {
#if UNITY_EDITOR
                    Debug.LogError(
                        "Duplicate CardType '" + entry.Type + "' found in catalog '" + assetIdentifier + "'.",
                        this);
#endif
                }
            }

            isLookupReady = false;
        }

        private string GetAssetIdentifier()
        {
#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return assetPath;
            }
#endif
            return name;
        }
        #endregion

    }// CLASS

}// NAMESPACE
