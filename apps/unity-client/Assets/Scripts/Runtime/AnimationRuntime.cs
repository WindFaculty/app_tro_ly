using AvatarSystem;
using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class AnimationRuntime : MonoBehaviour
    {
        private AvatarConversationBridge conversationBridge;
        private AvatarRootController avatarRoot;

        public void Bind(AvatarConversationBridge bridge, AvatarRootController rootController)
        {
            conversationBridge = bridge;
            avatarRoot = rootController;
        }

        public void PlayEmoteTrigger(string triggerName)
        {
            if (string.IsNullOrWhiteSpace(triggerName))
            {
                return;
            }

            avatarRoot?.AnimatorBridge?.FireTrigger(triggerName);
        }

        public void SetConversationState(string state)
        {
            switch (state)
            {
                case "listening":
                    conversationBridge?.OnListeningStart();
                    break;
                case "thinking":
                    conversationBridge?.OnThinkingStart();
                    break;
                case "talking":
                    conversationBridge?.OnSpeakingStart();
                    break;
                case "reacting":
                    conversationBridge?.OnReacting();
                    break;
                case "dormant":
                    conversationBridge?.OnDormant();
                    break;
                default:
                    conversationBridge?.OnIdle();
                    break;
            }
        }
    }
}
