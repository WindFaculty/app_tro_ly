using AvatarSystem;
using LocalAssistant.Avatar;
using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class LipSyncRuntime : MonoBehaviour
    {
        private LipSyncController localLipSyncController;
        private AvatarRootController avatarRoot;
        private AudioSource audioSource;

        public void Bind(
            LipSyncController lipSyncController,
            AvatarRootController rootController,
            AudioSource outputSource)
        {
            localLipSyncController = lipSyncController;
            avatarRoot = rootController;
            audioSource = outputSource;
            localLipSyncController?.BindAudioSource(outputSource);
            avatarRoot?.LipSync?.BindAudioSource(outputSource);
        }

        public void StartSpeech()
        {
            if (audioSource != null)
            {
                localLipSyncController?.BindAudioSource(audioSource);
                avatarRoot?.LipSync?.BindAudioSource(audioSource);
            }
        }

        public void StopSpeech()
        {
            avatarRoot?.LipSync?.StopSpeaking();
        }

        public void ApplyViseme(string visemeName)
        {
            if (avatarRoot?.LipSync == null || string.IsNullOrWhiteSpace(visemeName))
            {
                return;
            }

            if (System.Enum.TryParse(visemeName, true, out VisemeType viseme))
            {
                avatarRoot.LipSync.SetViseme(viseme);
            }
        }
    }
}
