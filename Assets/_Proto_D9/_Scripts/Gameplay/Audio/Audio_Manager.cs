using UnityEngine;

namespace MatchingPair.Gameplay
{
    public sealed class Audio_Manager : MonoBehaviour
    {
        #region Fields
        [SerializeField] private GameplayAudioCatalogSO audioCatalog;
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;
        #endregion

        #region UnityCallbacks
        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }
        #endregion

        #region PublicAPI
        public void PlayCardShowAudio()
        {
            PlayAudio(GameplayAudioType.CardShow);
        }

        public void PlayCardHideAudio()
        {
            PlayAudio(GameplayAudioType.CardHide);
        }

        public void PlayLevelCompleteAudio()
        {
            PlayAudio(GameplayAudioType.LevelComplete);
        }

        public void PlayLevelFailedAudio()
        {
            PlayAudio(GameplayAudioType.LevelFailed);
        }
        #endregion

        #region PrivateHelpers
        private void PlayAudio(GameplayAudioType audioType)
        {
            if (audioSource == null || audioCatalog == null)
            {
                return;
            }

            if (!audioCatalog.TryGetClip(audioType, out AudioClip clip) || clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, defaultVolume);
        }
        #endregion
    }
}
