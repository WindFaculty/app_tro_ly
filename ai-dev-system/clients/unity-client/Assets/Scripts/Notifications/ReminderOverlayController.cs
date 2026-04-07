using System.Collections.Generic;
using LocalAssistant.Core;
using UnityEngine.UIElements;

namespace LocalAssistant.Notifications
{
    public sealed class ReminderOverlayController
    {
        private Label reminderText;
        private VisualElement cardElement;
        private readonly Queue<string> pendingMessages = new();

        public void Bind(Label textElement, VisualElement card)
        {
            reminderText = textElement;
            cardElement = card;
            Clear();
        }

        public void Push(ReminderDueEvent reminder)
        {
            pendingMessages.Enqueue($"Nhac viec: {reminder.title} trong {reminder.minutes_until} phut.");
            Render();
        }

        public void Dismiss()
        {
            if (pendingMessages.Count > 0)
            {
                pendingMessages.Dequeue();
            }

            Render();
        }

        public void Clear()
        {
            pendingMessages.Clear();
            Render();
        }

        private void Render()
        {
            if (reminderText == null)
            {
                return;
            }

            var hasMessage = pendingMessages.Count > 0;
            reminderText.text = hasMessage ? pendingMessages.Peek() : string.Empty;
            if (cardElement != null)
            {
                if (hasMessage)
                    cardElement.RemoveFromClassList("hidden");
                else
                    cardElement.AddToClassList("hidden");
            }
        }
    }
}
