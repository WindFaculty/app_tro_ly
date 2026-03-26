using System.Collections.Generic;
using LocalAssistant.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Notifications
{
    public sealed class ReminderPresenter : MonoBehaviour
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
            SetCardVisibility(hasMessage);
        }

        private void SetCardVisibility(bool visible)
        {
            if (cardElement != null)
            {
                if (visible)
                    cardElement.RemoveFromClassList("hidden");
                else
                    cardElement.AddToClassList("hidden");
            }
        }
    }
}
