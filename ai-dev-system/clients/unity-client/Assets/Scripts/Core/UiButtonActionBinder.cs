using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    internal static class UiButtonActionBinder
    {
        public static void Bind(Button button, Action action)
        {
            if (button == null || action == null)
            {
                return;
            }

            var guard = new ButtonActionGuard(button, action);
            button.clicked += guard.InvokeFromClick;
            button.RegisterCallback<PointerUpEvent>(guard.HandlePointerUp);
        }

        private sealed class ButtonActionGuard
        {
            private readonly Button button;
            private readonly Action action;
            private int lastHandledFrame = -1;

            public ButtonActionGuard(Button button, Action action)
            {
                this.button = button;
                this.action = action;
            }

            public void InvokeFromClick() => TryInvoke();

            public void HandlePointerUp(PointerUpEvent evt)
            {
                if (evt == null || evt.button != 0)
                {
                    return;
                }

                TryInvoke();
            }

            private void TryInvoke()
            {
                if (button == null || !button.enabledInHierarchy)
                {
                    return;
                }

                if (Time.frameCount == lastHandledFrame)
                {
                    return;
                }

                lastHandledFrame = Time.frameCount;
                action();
            }
        }
    }
}
