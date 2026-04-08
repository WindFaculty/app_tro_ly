using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Bridges conversation/locomotion state to Animator parameters.
    /// Sets standardized parameters on the Mecanim Animator Controller.
    /// </summary>
    public sealed class AvatarAnimatorBridge : MonoBehaviour
    {
        // Cached parameter hashes for performance
        private static readonly int HashIsListening = Animator.StringToHash("IsListening");
        private static readonly int HashIsThinking = Animator.StringToHash("IsThinking");
        private static readonly int HashIsSpeaking = Animator.StringToHash("IsSpeaking");
        private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int HashMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int HashTurnAngle = Animator.StringToHash("TurnAngle");
        private static readonly int HashGestureIndex = Animator.StringToHash("GestureIndex");
        private static readonly int HashEmotionIndex = Animator.StringToHash("EmotionIndex");

        private Animator animator;

        public void Initialize(Animator anim)
        {
            animator = anim;
        }

        // ──────────────────────────────────────────────
        // Conversation state
        // ──────────────────────────────────────────────

        public void SetConversationState(ConversationState state)
        {
            if (animator == null) return;

            animator.SetBool(HashIsListening, state == ConversationState.Listening);
            animator.SetBool(HashIsThinking, state == ConversationState.Thinking);
            animator.SetBool(HashIsSpeaking, state == ConversationState.Speaking);
        }

        // ──────────────────────────────────────────────
        // Locomotion
        // ──────────────────────────────────────────────

        public void SetMoving(bool isMoving, float speed = 0f)
        {
            if (animator == null) return;
            animator.SetBool(HashIsMoving, isMoving);
            animator.SetFloat(HashMoveSpeed, speed);
        }

        public void SetTurnAngle(float angle)
        {
            if (animator == null) return;
            animator.SetFloat(HashTurnAngle, angle);
        }

        // ──────────────────────────────────────────────
        // Gestures and emotions
        // ──────────────────────────────────────────────

        public void SetGesture(GestureType gesture)
        {
            if (animator == null) return;
            animator.SetInteger(HashGestureIndex, (int)gesture);
        }

        public void SetEmotionIndex(EmotionType emotion)
        {
            if (animator == null) return;
            animator.SetInteger(HashEmotionIndex, (int)emotion);
        }

        /// <summary>Trigger a one-shot animator trigger by name.</summary>
        public void FireTrigger(string triggerName)
        {
            if (animator == null) return;
            animator.SetTrigger(triggerName);
        }
    }
}
