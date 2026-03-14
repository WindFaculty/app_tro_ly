using System.Collections;
using System;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
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

        public Coroutine Play(AudioClip clip, string subtitle, SubtitlePresenter subtitlePresenter, AvatarStateMachine avatar)
        {
            return StartCoroutine(PlayRoutine(clip, subtitle, subtitlePresenter, avatar));
        }

        private IEnumerator PlayRoutine(AudioClip clip, string subtitle, SubtitlePresenter subtitlePresenter, AvatarStateMachine avatar)
        {
            if (clip == null)
            {
                yield break;
            }

            subtitlePresenter.Show(subtitle);
            avatar.SetState(AvatarState.Talking);
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
            subtitlePresenter.Hide();
            avatar.SetState(AvatarState.Idle);
            PlaybackCompleted?.Invoke();
        }
    }
}
