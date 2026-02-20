using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchingPair.Gameplay
{
    [CreateAssetMenu(fileName = "GameplayAudioCatalogSO", menuName = "MatchingPair/Gameplay/Audio/Gameplay Audio Catalog")]
    public sealed class GameplayAudioCatalogSO : ScriptableObject
    {
#pragma warning disable CS0649
        [Serializable]
        private sealed class AudioClipEntry
        {
            public GameplayAudioType Type;
            public AudioClip Clip;
        }
#pragma warning restore CS0649

        #region Fields
        [SerializeField] private AudioClipEntry[] entries = Array.Empty<AudioClipEntry>();
        private readonly Dictionary<GameplayAudioType, AudioClip> clipLookup = new Dictionary<GameplayAudioType, AudioClip>(8);
        private bool isLookupDirty = true;
        #endregion

        #region UnityCallbacks
        private void OnEnable()
        {
            isLookupDirty = true;
        }

        private void OnValidate()
        {
            isLookupDirty = true;
        }
        #endregion

        #region PublicAPI
        public bool TryGetClip(GameplayAudioType audioType, out AudioClip clip)
        {
            EnsureLookupBuilt();
            return clipLookup.TryGetValue(audioType, out clip);
        }
        #endregion

        #region PrivateHelpers
        private void EnsureLookupBuilt()
        {
            if (!isLookupDirty)
            {
                return;
            }

            clipLookup.Clear();
            int entryCount = entries != null ? entries.Length : 0;
            for (int index = 0; index < entryCount; index++)
            {
                AudioClipEntry entry = entries[index];
                clipLookup[entry.Type] = entry.Clip;
            }

            isLookupDirty = false;
        }
        #endregion
    }
}
