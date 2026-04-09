using AvatarSystem;
using AvatarSystem.Data;
using LocalAssistant.Avatar;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class AvatarRuntime : MonoBehaviour
    {
        private AvatarStateMachine avatarStateMachine;
        private AvatarConversationBridge conversationBridge;
        private AvatarRootController avatarRoot;

        public LocalAssistant.Core.AvatarState CurrentState =>
            avatarStateMachine != null ? avatarStateMachine.CurrentState : LocalAssistant.Core.AvatarState.Idle;
        public event System.Action<string> StateChanged;

        public void Bind(
            AvatarStateMachine stateMachine,
            AvatarConversationBridge bridge,
            AvatarRootController rootController)
        {
            avatarStateMachine = stateMachine;
            conversationBridge = bridge;
            avatarRoot = rootController;
        }

        public void SetIdleState()
        {
            conversationBridge?.OnIdle();
            avatarStateMachine?.SetState(LocalAssistant.Core.AvatarState.Idle);
            RaiseStateChanged();
        }

        public void SetListeningState()
        {
            conversationBridge?.OnListeningStart();
            avatarStateMachine?.SetState(LocalAssistant.Core.AvatarState.Listening);
            RaiseStateChanged();
        }

        public void SetThinkingState()
        {
            conversationBridge?.OnThinkingStart();
            avatarStateMachine?.SetState(LocalAssistant.Core.AvatarState.Thinking);
            RaiseStateChanged();
        }

        public void SetTalkingState()
        {
            conversationBridge?.OnSpeakingStart();
            avatarStateMachine?.SetState(LocalAssistant.Core.AvatarState.Talking);
            RaiseStateChanged();
        }

        public void SetMood(string backendState, string animationHint = "")
        {
            if (avatarStateMachine == null)
            {
                return;
            }

            avatarStateMachine.ApplyBackendState(backendState, animationHint);
            RaiseStateChanged();
        }

        public void PlayEmote(string triggerName)
        {
            if (string.IsNullOrWhiteSpace(triggerName))
            {
                return;
            }

            avatarRoot?.AnimatorBridge?.FireTrigger(triggerName);
        }

        public bool EquipItem(AvatarItemDefinition item)
        {
            return item != null && avatarRoot?.Equipment != null && avatarRoot.Equipment.Equip(item);
        }

        public void SetLookAtTarget(Transform target)
        {
            avatarRoot?.LookAt?.SetLookAtTarget(target);
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(CurrentState.ToString().ToLowerInvariant());
        }
    }
}
