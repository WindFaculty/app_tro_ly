using UnityEngine;

namespace LocalAssistant.Avatar
{
    public sealed class LipSyncController : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private SkinnedMeshRenderer faceMesh;
        [SerializeField] private int mouthOpenBlendShapeIndex = -1;
        [SerializeField] private Transform fallbackTransform;
        [SerializeField] private float amplitudeScale = 160f;
        [SerializeField] private float fallbackScale = 0.08f;

        private readonly float[] sampleBuffer = new float[64];
        private Vector3 fallbackBaseScale;

        private void Awake()
        {
            if (fallbackTransform != null)
            {
                fallbackBaseScale = fallbackTransform.localScale;
            }
        }

        private void Update()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.GetOutputData(sampleBuffer, 0);
            float peak = 0f;
            for (var index = 0; index < sampleBuffer.Length; index++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sampleBuffer[index]));
            }

            if (faceMesh != null && mouthOpenBlendShapeIndex >= 0)
            {
                faceMesh.SetBlendShapeWeight(mouthOpenBlendShapeIndex, Mathf.Clamp01(peak * amplitudeScale) * 100f);
                return;
            }

            if (fallbackTransform != null)
            {
                var modifier = Mathf.Clamp01(peak * amplitudeScale) * fallbackScale;
                fallbackTransform.localScale = fallbackBaseScale + new Vector3(0f, modifier, 0f);
            }
        }

        public void BindAudioSource(AudioSource value)
        {
            audioSource = value;
        }
    }
}
