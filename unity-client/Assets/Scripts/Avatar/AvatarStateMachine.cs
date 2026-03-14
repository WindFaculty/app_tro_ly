using System;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.Avatar
{
    public sealed class AvatarStateMachine : MonoBehaviour
    {
        [SerializeField] private Renderer placeholderRenderer;

        public AvatarState CurrentState { get; private set; } = AvatarState.Idle;
        public event Action<AvatarState> StateChanged;

        public void SetState(AvatarState state)
        {
            if (CurrentState == state)
            {
                return;
            }

            CurrentState = state;
            ApplyVisuals(state);
            StateChanged?.Invoke(state);
        }

        public void ApplyBackendState(string backendState, string animationHint)
        {
            var target = animationHint switch
            {
                "listen" => AvatarState.Listening,
                "think" => AvatarState.Thinking,
                "confirm" => AvatarState.Confirming,
                "alert" => AvatarState.Warning,
                "greet" => AvatarState.Greeting,
                "idle" when backendState == "waiting" => AvatarState.Waiting,
                "explain" when backendState == "talking" => AvatarState.Talking,
                "react" => AvatarState.Reacting,
                _ => backendState switch
                {
                    "listening" => AvatarState.Listening,
                    "thinking" => AvatarState.Thinking,
                    "talking" => AvatarState.Talking,
                    "waiting" => AvatarState.Waiting,
                    "error" => AvatarState.Error,
                    "dormant" => AvatarState.Dormant,
                    "reacting" => AvatarState.Reacting,
                    _ => AvatarState.Idle,
                },
            };

            SetState(target);
        }

        public void BindPlaceholder(Renderer value)
        {
            placeholderRenderer = value;
            ApplyVisuals(CurrentState);
        }

        private void ApplyVisuals(AvatarState state)
        {
            if (placeholderRenderer == null)
            {
                return;
            }

            placeholderRenderer.material.color = state switch
            {
                AvatarState.Listening => new Color(0.15f, 0.55f, 0.85f),
                AvatarState.Thinking => new Color(0.95f, 0.65f, 0.18f),
                AvatarState.Talking => new Color(0.25f, 0.70f, 0.45f),
                AvatarState.Confirming => new Color(0.18f, 0.76f, 0.52f),
                AvatarState.Warning => new Color(0.90f, 0.22f, 0.18f),
                AvatarState.Greeting => new Color(0.56f, 0.45f, 0.82f),
                AvatarState.Waiting => new Color(0.54f, 0.62f, 0.78f),
                AvatarState.Error => new Color(0.72f, 0.14f, 0.16f),
                AvatarState.Reacting => new Color(0.86f, 0.56f, 0.78f),
                AvatarState.Dormant => new Color(0.42f, 0.42f, 0.52f),
                _ => new Color(0.72f, 0.72f, 0.78f),
            };
        }
    }
}
