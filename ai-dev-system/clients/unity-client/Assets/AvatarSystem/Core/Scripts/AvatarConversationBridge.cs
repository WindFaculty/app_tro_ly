using System;
using AvatarSystem.Data;
using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Receives conversation events from the STT/LLM/TTS pipeline and drives
    /// the avatar sub-systems. The external system (e.g., AssistantApp) calls
    /// the event methods on this bridge — it never touches mesh or animator directly.
    /// </summary>
    public sealed class AvatarConversationBridge : MonoBehaviour
    {
        private AvatarAnimatorBridge animatorBridge;
        private AvatarFacialController facialController;
        private AvatarLipSyncDriver lipSyncDriver;

        [Header("Expression Library")]
        [SerializeField] private ExpressionDefinition[] expressionLibrary;

        public ConversationState CurrentState { get; private set; } = ConversationState.Idle;
        public event Action<ConversationState> StateChanged;

        public void Initialize(
            AvatarAnimatorBridge animator,
            AvatarFacialController facial,
            AvatarLipSyncDriver lipSync)
        {
            animatorBridge = animator;
            facialController = facial;
            lipSyncDriver = lipSync;
        }

        // ──────────────────────────────────────────────
        // Event handlers — called by external systems
        // ──────────────────────────────────────────────

        public void OnListeningStart()
        {
            TransitionTo(ConversationState.Listening);
            facialController?.SetEmotion(EmotionType.Focused, FindExpression(EmotionType.Focused));
        }

        public void OnListeningEnd()
        {
            TransitionTo(ConversationState.Thinking);
        }

        public void OnThinkingStart()
        {
            TransitionTo(ConversationState.Thinking);
            facialController?.SetEmotion(EmotionType.Curious, FindExpression(EmotionType.Curious));
            animatorBridge?.SetGesture(GestureType.ThinkingPose);
        }

        public void OnSpeakingStart()
        {
            TransitionTo(ConversationState.Speaking);
            facialController?.SetEmotion(EmotionType.SoftSmile, FindExpression(EmotionType.SoftSmile));
            animatorBridge?.SetGesture(GestureType.None);
        }

        public void OnSpeakingEnd()
        {
            lipSyncDriver?.StopSpeaking();
            TransitionTo(ConversationState.Idle);
            facialController?.ClearEmotion();
            animatorBridge?.SetGesture(GestureType.None);
        }

        /// <summary>Receive a viseme frame from TTS output.</summary>
        public void OnSpeakingVisemeFrame(VisemeType viseme)
        {
            lipSyncDriver?.SetViseme(viseme);
        }

        /// <summary>Receive an emotion hint from the LLM response.</summary>
        public void OnEmotionHint(EmotionType emotion)
        {
            var def = FindExpression(emotion);
            facialController?.SetEmotion(emotion, def);
            animatorBridge?.SetEmotionIndex(emotion);
        }

        /// <summary>Trigger a body gesture.</summary>
        public void OnGestureHint(GestureType gesture)
        {
            animatorBridge?.SetGesture(gesture);
        }

        public void OnReacting()
        {
            TransitionTo(ConversationState.Reacting);
        }

        public void OnIdle()
        {
            TransitionTo(ConversationState.Idle);
            facialController?.ClearEmotion();
            lipSyncDriver?.StopSpeaking();
            animatorBridge?.SetGesture(GestureType.None);
        }

        public void OnDormant()
        {
            TransitionTo(ConversationState.Dormant);
            facialController?.ClearEmotion();
            lipSyncDriver?.StopSpeaking();
        }

        // ──────────────────────────────────────────────
        // Internal
        // ──────────────────────────────────────────────

        private void TransitionTo(ConversationState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            animatorBridge?.SetConversationState(newState);
            StateChanged?.Invoke(newState);
        }

        private ExpressionDefinition FindExpression(EmotionType emotion)
        {
            if (expressionLibrary == null) return null;
            foreach (var def in expressionLibrary)
            {
                if (def != null && def.emotionType == emotion) return def;
            }
            return null;
        }
    }
}
