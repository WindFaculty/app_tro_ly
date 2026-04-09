using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Controls head and eye look-at behavior via bone transforms.
    /// Smoothly slerps the head bone toward a target with configurable weight.
    /// </summary>
    public sealed class AvatarLookAtController : MonoBehaviour
    {
        [Header("Bone References")]
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform eyeLeftBone;
        [SerializeField] private Transform eyeRightBone;
        [SerializeField] private Transform neckBone;

        [Header("Target")]
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private Transform defaultTarget;

        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float headWeight = 0.6f;
        [SerializeField] [Range(0f, 1f)] private float eyeWeight = 0.3f;
        [SerializeField] private float smoothSpeed = 4f;
        [SerializeField] private float maxHeadAngle = 45f;

        private Quaternion headBaseRotation;
        private Quaternion eyeLBaseRotation;
        private Quaternion eyeRBaseRotation;

        private void Start()
        {
            CaptureBaseRotations();
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>Set the primary look-at target (e.g., camera or user).</summary>
        public void SetLookAtTarget(Transform target)
        {
            lookAtTarget = target;
        }

        /// <summary>Clear the look-at target and return to default gaze.</summary>
        public void ClearLookAt()
        {
            lookAtTarget = null;
        }

        public void SetHeadWeight(float weight)
        {
            headWeight = Mathf.Clamp01(weight);
        }

        public void SetEyeWeight(float weight)
        {
            eyeWeight = Mathf.Clamp01(weight);
        }

        // ──────────────────────────────────────────────
        // LateUpdate — apply after animation
        // ──────────────────────────────────────────────

        private void LateUpdate()
        {
            Transform effectiveTarget = lookAtTarget != null ? lookAtTarget : defaultTarget;
            if (effectiveTarget == null) return;

            ApplyHeadLookAt(effectiveTarget);
            ApplyEyeLookAt(effectiveTarget);
        }

        private void ApplyHeadLookAt(Transform target)
        {
            if (headBone == null) return;

            Vector3 direction = target.position - headBone.position;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion desiredRotation = Quaternion.Slerp(headBone.rotation, targetRotation, headWeight);

            // Clamp angle to prevent unnatural neck-breaking turns
            float angle = Quaternion.Angle(headBaseRotation, desiredRotation);
            if (angle > maxHeadAngle)
            {
                desiredRotation = Quaternion.Slerp(headBaseRotation, desiredRotation, maxHeadAngle / angle);
            }

            headBone.rotation = Quaternion.Slerp(headBone.rotation, desiredRotation, Time.deltaTime * smoothSpeed);
        }

        private void ApplyEyeLookAt(Transform target)
        {
            ApplyEyeBoneLookAt(eyeLeftBone, target);
            ApplyEyeBoneLookAt(eyeRightBone, target);
        }

        private void ApplyEyeBoneLookAt(Transform eyeBone, Transform target)
        {
            if (eyeBone == null) return;

            Vector3 direction = target.position - eyeBone.position;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion desiredRotation = Quaternion.Slerp(eyeBone.rotation, targetRotation, eyeWeight);
            eyeBone.rotation = Quaternion.Slerp(eyeBone.rotation, desiredRotation, Time.deltaTime * smoothSpeed);
        }

        private void CaptureBaseRotations()
        {
            if (headBone != null) headBaseRotation = headBone.rotation;
            if (eyeLeftBone != null) eyeLBaseRotation = eyeLeftBone.rotation;
            if (eyeRightBone != null) eyeRBaseRotation = eyeRightBone.rotation;
        }
    }
}
