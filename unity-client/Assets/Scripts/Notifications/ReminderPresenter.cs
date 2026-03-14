using System.Collections.Generic;
using LocalAssistant.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LocalAssistant.Notifications
{
    public sealed class ReminderPresenter : MonoBehaviour
    {
        [SerializeField] private Text reminderText;
        private readonly Queue<string> pendingMessages = new();

        public void Bind(Text value)
        {
            reminderText = value;
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
            reminderText.gameObject.SetActive(hasMessage);
            SetCardVisibility(hasMessage);
        }

        private void SetCardVisibility(bool visible)
        {
            if (reminderText.transform.parent == null)
            {
                return;
            }

            var card = reminderText.transform.parent.gameObject;
            if (card.name.EndsWith("Card"))
            {
                card.SetActive(visible);
            }
        }
    }
}
