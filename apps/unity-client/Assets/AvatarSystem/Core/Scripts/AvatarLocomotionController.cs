using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Moves the avatar within a small bounded area using named anchor points.
    /// Uses simple lerp-based movement — no NavMesh required for the desktop assistant.
    /// </summary>
    public sealed class AvatarLocomotionController : MonoBehaviour
    {
        [Header("Anchor Points")]
        [SerializeField] private Transform idlePoint;
        [SerializeField] private Transform talkPoint;
        [SerializeField] private Transform listenPoint;
        [SerializeField] private Transform wanderPointA;
        [SerializeField] private Transform wanderPointB;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.2f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float arrivalThreshold = 0.05f;

        [Header("Wander Behavior")]
        [SerializeField] private float wanderInterval = 12f;
        [SerializeField] private float wanderRandomRange = 5f;

        private Transform avatarRoot;
        private AvatarAnimatorBridge animatorBridge;
        private Transform currentTarget;
        private bool isMoving;
        private float wanderTimer;

        public bool IsMoving => isMoving;

        public void Initialize(Transform root, AvatarAnimatorBridge bridge)
        {
            avatarRoot = root;
            animatorBridge = bridge;
            wanderTimer = wanderInterval + Random.Range(0f, wanderRandomRange);
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>Move to a named anchor.</summary>
        public void MoveToAnchor(string anchorName)
        {
            Transform target = anchorName switch
            {
                "idle" => idlePoint,
                "talk" => talkPoint,
                "listen" => listenPoint,
                "wanderA" => wanderPointA,
                "wanderB" => wanderPointB,
                _ => null,
            };

            if (target != null)
            {
                currentTarget = target;
            }
        }

        /// <summary>Move to an arbitrary world position.</summary>
        public void MoveTo(Vector3 worldPosition)
        {
            // Create a temporary target
            if (currentTarget == null)
            {
                var go = new GameObject("TempTarget");
                go.transform.SetParent(transform);
                currentTarget = go.transform;
            }
            currentTarget.position = worldPosition;
        }

        /// <summary>Immediately stop and clear target.</summary>
        public void Stop()
        {
            currentTarget = null;
            SetMovingState(false);
        }

        /// <summary>Respond to conversation state changes.</summary>
        public void OnConversationStateChanged(ConversationState state)
        {
            switch (state)
            {
                case ConversationState.Listening:
                    if (listenPoint != null) currentTarget = listenPoint;
                    break;
                case ConversationState.Speaking:
                    if (talkPoint != null) currentTarget = talkPoint;
                    break;
                case ConversationState.Idle:
                    if (idlePoint != null) currentTarget = idlePoint;
                    break;
            }
        }

        // ──────────────────────────────────────────────
        // Update
        // ──────────────────────────────────────────────

        private void Update()
        {
            if (avatarRoot == null) return;

            // Movement toward target
            if (currentTarget != null)
            {
                Vector3 toTarget = currentTarget.position - avatarRoot.position;
                toTarget.y = 0f; // Stay on ground plane
                float distance = toTarget.magnitude;

                if (distance > arrivalThreshold)
                {
                    SetMovingState(true);

                    // Turn toward target
                    if (toTarget.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(toTarget);
                        avatarRoot.rotation = Quaternion.Slerp(avatarRoot.rotation, targetRot, Time.deltaTime * turnSpeed);
                    }

                    // Move forward
                    float step = moveSpeed * Time.deltaTime;
                    avatarRoot.position = Vector3.MoveTowards(avatarRoot.position, currentTarget.position, step);
                    avatarRoot.position = new Vector3(avatarRoot.position.x, avatarRoot.transform.position.y, avatarRoot.position.z);
                }
                else
                {
                    SetMovingState(false);
                    currentTarget = null;
                }
            }
            else
            {
                // Idle wander behavior
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f)
                {
                    wanderTimer = wanderInterval + Random.Range(-wanderRandomRange, wanderRandomRange);
                    WanderToRandomPoint();
                }
            }
        }

        private void WanderToRandomPoint()
        {
            if (wanderPointA == null && wanderPointB == null) return;

            if (wanderPointA != null && wanderPointB != null)
            {
                currentTarget = Random.value > 0.5f ? wanderPointA : wanderPointB;
            }
            else
            {
                currentTarget = wanderPointA != null ? wanderPointA : wanderPointB;
            }
        }

        private void SetMovingState(bool moving)
        {
            if (isMoving == moving) return;
            isMoving = moving;
            animatorBridge?.SetMoving(moving, moving ? moveSpeed : 0f);
        }
    }
}
