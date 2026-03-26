using System;
using LocalAssistant.Core;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Chat
{
    public sealed class ChatPanelController
    {
        private readonly ChatPanelRefs chat;

        public ChatPanelController(ChatPanelRefs chat)
        {
            this.chat = chat;
        }

        public event Action<string> SendRequested;
        public event Action MicRequested;

        public void Bind()
        {
            chat.SendButton.clicked += SubmitCurrentInput;
            chat.MicButton.clicked += RequestMicToggle;
        }

        public void Render(string transcript)
        {
            if (chat.ChatLogText != null)
            {
                chat.ChatLogText.text = transcript;
            }
        }

        public void SubmitCurrentInput()
        {
            if (chat.ChatInput == null)
            {
                return;
            }

            var text = chat.ChatInput.value;
            chat.ChatInput.value = string.Empty;
            SendRequested?.Invoke(text);
        }

        public void RequestMicToggle()
        {
            MicRequested?.Invoke();
        }

        public void SetInteractable(bool canSendText, bool canUseMic)
        {
            chat.ChatInput?.SetEnabled(canSendText);
            chat.SendButton?.SetEnabled(canSendText);
            chat.MicButton?.SetEnabled(canUseMic);
        }
    }
}
