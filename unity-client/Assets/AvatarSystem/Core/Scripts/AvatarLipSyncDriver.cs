using AvatarSystem.Data;
using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Drives lip-sync blendshapes from audio amplitude or viseme data.
    /// Supports 3 modes:
    ///   1. Amplitude (prototype) — reads AudioSource output data
    ///   2. Viseme-timed (usable) — receives viseme frames from TTS
    ///   3. Timeline (production) — full viseme timeline with smoothing
    /// </summary>
    public sealed class AvatarLipSyncDriver : MonoBehaviour
    {
        public enum LipSyncMode { Amplitude, Viseme, Timeline }

        [Header("Configuration")]
        [SerializeField] private LipSyncMode mode = LipSyncMode.Amplitude;
        [SerializeField] private LipSyncMapDefinition lipSyncMap;
        [SerializeField] private float amplitudeScale = 160f;
        [SerializeField] private float visemeSmoothSpeed = 12f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private AvatarFacialController facialController;
        private readonly float[] sampleBuffer = new float[64];
        private VisemeType currentViseme = VisemeType.Rest;
        private float currentAmplitude;

        public LipSyncMode Mode => mode;
        public LipSyncMapDefinition LipSyncMap => lipSyncMap;

        public void Initialize(AvatarFacialController facial)
        {
            facialController = facial;
        }

        public void BindAudioSource(AudioSource source)
        {
            audioSource = source;
        }

        public void SetLipSyncMap(LipSyncMapDefinition definition)
        {
            lipSyncMap = definition;
        }

        public void SetMode(LipSyncMode newMode)
        {
            mode = newMode;
        }

        // ──────────────────────────────────────────────
        // Viseme API (called by ConversationBridge from TTS events)
        // ──────────────────────────────────────────────

        /// <summary>Set the current viseme from a TTS phoneme frame.</summary>
        public void SetViseme(VisemeType viseme)
        {
            currentViseme = viseme;
        }

        /// <summary>Clear lip-sync state when speech ends.</summary>
        public void StopSpeaking()
        {
            currentViseme = VisemeType.Rest;
            currentAmplitude = 0f;
            if (facialController != null) facialController.ClearLipSync();
        }

        // ──────────────────────────────────────────────
        // Update
        // ──────────────────────────────────────────────

        private void Update()
        {
            if (facialController == null) return;

            switch (mode)
            {
                case LipSyncMode.Amplitude:
                    UpdateAmplitude();
                    break;
                case LipSyncMode.Viseme:
                case LipSyncMode.Timeline:
                    UpdateViseme();
                    break;
            }
        }

        private void UpdateAmplitude()
        {
            if (audioSource == null || !audioSource.isPlaying)
            {
                if (currentAmplitude > 0.01f)
                {
                    currentAmplitude = Mathf.Lerp(currentAmplitude, 0f, Time.deltaTime * visemeSmoothSpeed);
                    ApplyAmplitudeMouth(currentAmplitude);
                }
                else if (currentAmplitude > 0f)
                {
                    currentAmplitude = 0f;
                    facialController.ClearLipSync();
                }
                else
                {
                    facialController.ClearLipSync();
                }
                return;
            }

            audioSource.GetOutputData(sampleBuffer, 0);
            float peak = 0f;
            for (int i = 0; i < sampleBuffer.Length; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sampleBuffer[i]));
            }

            currentAmplitude = Mathf.Clamp01(peak * amplitudeScale);
            ApplyAmplitudeMouth(currentAmplitude);
        }

        private void ApplyAmplitudeMouth(float amplitude)
        {
            // In amplitude mode, we drive a simple mouth open blend
            if (lipSyncMap == null)
            {
                facialController.SetLipSyncWeight("MouthOpen", amplitude * 100f);
                return;
            }

            // Use the AA viseme mapping as the open-mouth shape
            var mapping = lipSyncMap.GetMapping(VisemeType.AA);
            if (mapping.HasValue && mapping.Value.targets != null)
            {
                foreach (var target in mapping.Value.targets)
                {
                    facialController.SetLipSyncWeight(target.blendShapeName, target.weight * amplitude);
                }
            }
        }

        private void UpdateViseme()
        {
            if (lipSyncMap == null) return;

            var mapping = lipSyncMap.GetMapping(currentViseme);
            if (!mapping.HasValue) return;

            // Clear previous lip-sync and apply new targets with smoothing
            facialController.ClearLipSync();

            if (mapping.Value.targets != null)
            {
                foreach (var target in mapping.Value.targets)
                {
                    facialController.SetLipSyncWeight(target.blendShapeName, target.weight);
                }
            }
        }
    }
}
