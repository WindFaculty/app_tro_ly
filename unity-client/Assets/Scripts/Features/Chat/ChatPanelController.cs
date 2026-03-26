using System;
using LocalAssistant.Core;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Chat
{
    public sealed class ChatPanelController
    {
        private readonly AssistantUiRefs ui;

        public ChatPanelController(AssistantUiRefs ui)
        {
            this.ui = ui;
        }

        public event Action<string> SendRequested;
        public event Action MicRequested;

        public void Bind()
        {
            ui.SendButton.clicked += SubmitCurrentInput;
            ui.MicButton.clicked += RequestMicToggle;
        }

        public void Render(string transcript)
        {
            if (ui.ChatLogText != null)
            {
                ui.ChatLogText.text = transcript;
            }
        }

        public void SubmitCurrentInput()
        {
            if (ui.ChatInput == null)
            {
                return;
            }

            var text = ui.ChatInput.value;
            ui.ChatInput.value = string.Empty;
            SendRequested?.Invoke(text);
        }

        public void RequestMicToggle()
        {
            MicRequested?.Invoke();
        }

        public void SetInteractable(bool canSendText, bool canUseMic)
        {
            ui.ChatInput?.SetEnabled(canSendText);
            ui.SendButton?.SetEnabled(canSendText);
            ui.MicButton?.SetEnabled(canUseMic);
        }
    }
}
