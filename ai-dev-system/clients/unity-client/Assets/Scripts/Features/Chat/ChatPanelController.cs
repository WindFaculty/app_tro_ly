using System;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Chat
{
    public sealed class ChatPanelController
    {
        private readonly ChatPanelRefs chat;
        private bool canSendText = true;
        private bool canUseMic = true;

        public ChatPanelController(ChatPanelRefs chat)
        {
            this.chat = chat;
        }

        public event Action<string> SendRequested;
        public event Action MicRequested;

        public void Bind()
        {
            if (chat.SendButton != null)
            {
                chat.SendButton.clicked += SubmitCurrentInput;
            }

            if (chat.MicButton != null)
            {
                chat.MicButton.clicked += RequestMicToggle;
            }

            if (chat.ChatInput != null)
            {
                chat.ChatInput.RegisterCallback<KeyDownEvent>(HandleChatInputKeyDown);
            }
        }

        public void Render(ChatPanelSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (chat.ChatStateBadge != null)
            {
                chat.ChatStateBadge.text = snapshot.StatusBadge;
            }

            if (chat.ChatStateTitle != null)
            {
                chat.ChatStateTitle.text = snapshot.StatusTitle;
            }

            if (chat.ChatStateDetail != null)
            {
                chat.ChatStateDetail.text = snapshot.StatusDetail;
            }

            if (chat.ChatRouteBadge != null)
            {
                chat.ChatRouteBadge.text = snapshot.RouteBadgeText;
            }

            if (chat.ChatTranscriptPreviewTitle != null)
            {
                chat.ChatTranscriptPreviewTitle.text = snapshot.TranscriptPreviewTitle;
            }

            if (chat.ChatTranscriptPreviewText != null)
            {
                chat.ChatTranscriptPreviewText.text = snapshot.TranscriptPreviewText;
            }

            if (chat.ChatActionSummaryTitle != null)
            {
                chat.ChatActionSummaryTitle.text = snapshot.ActionSummaryTitle;
            }

            if (chat.ChatActionSummaryText != null)
            {
                chat.ChatActionSummaryText.text = snapshot.ActionSummaryText;
            }

            if (chat.ChatLogText != null)
            {
                chat.ChatLogText.text = snapshot.Transcript;
            }
        }

        public void SubmitCurrentInput()
        {
            if (chat.ChatInput == null || !canSendText)
            {
                return;
            }

            var text = chat.ChatInput.value?.Trim();
            chat.ChatInput.value = string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                SendRequested?.Invoke(text);
            }
        }

        public void RequestMicToggle()
        {
            if (canUseMic)
            {
                MicRequested?.Invoke();
            }
        }

        public void SetInteractable(bool canSendText, bool canUseMic)
        {
            this.canSendText = canSendText;
            this.canUseMic = canUseMic;
            chat.ChatInput?.SetEnabled(canSendText);
            chat.SendButton?.SetEnabled(canSendText);
            chat.MicButton?.SetEnabled(canUseMic);
        }

        public void HandleChatInputKeyDown(KeyDownEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                SubmitCurrentInput();
                evt.StopPropagation();
            }
        }
    }
}
