using System.Collections.Generic;
using LocalAssistant.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Notifications
{
    public sealed class ReminderPresenter : MonoBehaviour
    {
        private readonly ReminderOverlayController controller = new();

        public void Bind(Label textElement, VisualElement card)
        {
            controller.Bind(textElement, card);
        }

        public void Push(ReminderDueEvent reminder)
        {
            controller.Push(reminder);
        }

        public void Dismiss()
        {
            controller.Dismiss();
        }

        public void Clear()
        {
            controller.Clear();
        }
    }
}
