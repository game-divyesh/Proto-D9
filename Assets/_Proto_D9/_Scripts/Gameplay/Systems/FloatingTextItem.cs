using TMPro;
using UnityEngine;

namespace MatchingPair.Gameplay
{
    public sealed class FloatingTextItem : MonoBehaviour
    {
        #region Fields
        [SerializeField] private TMP_Text textComponent;

        private float lifetimeSeconds;
        private float elapsedSeconds;
        private Vector3 movePerSecond;
        private Color baseColor;
        #endregion

        #region PublicAPI
        public void Show(string message, Vector3 worldPosition, Color color, float lifetime, Vector3 movement)
        {
            EnsureTextComponent();
            if (textComponent == null)
            {
                return;
            }

            transform.position = worldPosition;
            elapsedSeconds = 0f;
            lifetimeSeconds = Mathf.Max(0.01f, lifetime);
            movePerSecond = movement;
            baseColor = color;

            textComponent.text = message;
            textComponent.color = baseColor;
            gameObject.SetActive(true);
        }

        public bool Tick(float deltaTime)
        {
            elapsedSeconds += deltaTime;
            transform.position += movePerSecond * deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsedSeconds / lifetimeSeconds);
            Color currentColor = baseColor;
            currentColor.a = 1f - normalizedTime;
            textComponent.color = currentColor;

            return normalizedTime >= 1f;
        }
        #endregion

        #region PrivateHelpers
        private void EnsureTextComponent()
        {
            if (textComponent == null)
            {
                textComponent = GetComponent<TMP_Text>();
            }
        }
        #endregion
    }
}
