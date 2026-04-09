using System.Collections;
using System;
using UnityEngine;

namespace LocalAssistant.Audio
{
    public sealed class AudioPlaybackController : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        public AudioSource Output => audioSource;
        public bool IsPlaying => audioSource != null && audioSource.isPlaying;
        public event Action PlaybackCompleted;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        public Coroutine Play(AudioClip clip)
        {
            return StartCoroutine(PlayRoutine(clip));
        }

        private IEnumerator PlayRoutine(AudioClip clip)
        {
            if (clip == null)
            {
                yield break;
            }

            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
            PlaybackCompleted?.Invoke();
        }
    }
}
