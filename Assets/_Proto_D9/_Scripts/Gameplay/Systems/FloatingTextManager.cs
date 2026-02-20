using System.Collections.Generic;
using UnityEngine;

namespace MatchingPair.Gameplay
{
    public sealed class FloatingTextManager : MonoBehaviour
    {
        #region Fields
        [SerializeField] private FloatingTextItem floatingTextPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField, Min(0)] private int prewarmCount = 8;

        [Header("Default Style")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField, Min(0.01f)] private float defaultLifetime = 0.8f;
        [SerializeField] private Vector3 defaultMovePerSecond = new Vector3(0f, 1f, 0f);
        [SerializeField] private Vector3 defaultWorldOffset = new Vector3(0f, 0.4f, 0f);

        private readonly Stack<FloatingTextItem> pooledItems = new Stack<FloatingTextItem>(16);
        private readonly List<FloatingTextItem> activeItems = new List<FloatingTextItem>(16);
        private bool isInitialized;
        #endregion

        #region UnityCallbacks
        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void Update()
        {
            int activeCount = activeItems.Count;
            for (int index = activeCount - 1; index >= 0; index--)
            {
                FloatingTextItem activeItem = activeItems[index];
                if (activeItem == null)
                {
                    activeItems.RemoveAt(index);
                    continue;
                }

                bool finished = activeItem.Tick(Time.deltaTime);
                if (!finished)
                {
                    continue;
                }

                Release(activeItem, index);
            }
        }
        #endregion

        #region PublicAPI
        public void ShowText(string message)
        {
            ShowText(message, transform.position + defaultWorldOffset);
        }

        public void ShowText(string message, Vector3 worldPosition)
        {
            ShowText(message, worldPosition, defaultColor, defaultLifetime);
        }

        public void ShowText(string message, Vector3 worldPosition, Color color, float lifetime)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            InitializeIfNeeded();
            if (floatingTextPrefab == null)
            {
                return;
            }

            FloatingTextItem item = GetFromPool();
            item.Show(message, worldPosition, color, lifetime, defaultMovePerSecond);
            activeItems.Add(item);
        }
        #endregion

        #region PrivateHelpers
        private void InitializeIfNeeded()
        {
            if (isInitialized)
            {
                return;
            }

            if (poolRoot == null)
            {
                poolRoot = transform;
            }

            if (floatingTextPrefab == null)
            {
                isInitialized = true;
                return;
            }

            for (int index = 0; index < prewarmCount; index++)
            {
                FloatingTextItem item = Instantiate(floatingTextPrefab, poolRoot);
                item.gameObject.SetActive(false);
                pooledItems.Push(item);
            }

            isInitialized = true;
        }

        private FloatingTextItem GetFromPool()
        {
            if (pooledItems.Count > 0)
            {
                return pooledItems.Pop();
            }

            FloatingTextItem item = Instantiate(floatingTextPrefab, poolRoot);
            item.gameObject.SetActive(false);
            return item;
        }

        private void Release(FloatingTextItem item, int activeIndex)
        {
            activeItems.RemoveAt(activeIndex);
            item.gameObject.SetActive(false);
            item.transform.SetParent(poolRoot, true);
            pooledItems.Push(item);
        }
        #endregion
    }
}
