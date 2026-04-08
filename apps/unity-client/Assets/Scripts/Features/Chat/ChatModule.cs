using System;
using LocalAssistant.Chat;
using LocalAssistant.Core;

namespace LocalAssistant.Features.Chat
{
    public sealed class ChatModule : IChatModule
    {
        private readonly ChatViewModelStore store;
        private readonly ChatPanelController panelController;
        private bool isBound;

        public ChatModule(ChatPanelRefs refs)
            : this(new ChatPanelController(refs), new ChatViewModelStore())
        {
        }

        public ChatModule(ChatPanelController panelController, ChatViewModelStore store)
        {
            this.panelController = panelController ?? throw new ArgumentNullException(nameof(panelController));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public event Action<string> SendRequested;
        public event Action MicRequested;

        public string ConversationId
        {
            get => store.ConversationId;
            set => store.ConversationId = value ?? string.Empty;
        }

        public string SessionId
        {
            get => store.SessionId;
            set => store.SessionId = value ?? string.Empty;
        }

        public string CurrentRoute => store.CurrentRoute;
        public string CurrentProvider => store.CurrentProvider;
        public int FallbackCount => store.FallbackCount;

        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            isBound = true;
            panelController.Bind();
            panelController.SendRequested += HandleSendRequested;
            panelController.MicRequested += HandleMicRequested;
        }

        public ChatPanelSnapshot Render(bool transcriptPreviewEnabled)
        {
            var snapshot = store.BuildPanelSnapshot(transcriptPreviewEnabled);
            panelController.Render(snapshot);
            return snapshot;
        }

        public void SubmitCurrentInput() => panelController.SubmitCurrentInput();
        public void RequestMicToggle() => panelController.RequestMicToggle();
        public void SetInteractable(bool canSendText, bool canUseMic) => panelController.SetInteractable(canSendText, canUseMic);
        public void BeginTurn(string message, bool fromVoice, bool transcriptPreviewEnabled) => store.BeginTurn(message, fromVoice, transcriptPreviewEnabled);
        public void ApplyCompatibilityResponse(ChatResponsePayload response) => store.ApplyCompatibilityResponse(response);
        public void ApplyTranscriptPartial(string value, bool transcriptPreviewEnabled) => store.ApplyTranscriptPartial(value, transcriptPreviewEnabled);
        public void ApplyTranscriptFinal(string value, bool transcriptPreviewEnabled) => store.ApplyTranscriptFinal(value, transcriptPreviewEnabled);
        public void ApplyRouteSelection(string route, string provider) => store.ApplyRouteSelection(route, provider);
        public void ApplyAssistantChunk(string value) => store.ApplyAssistantChunk(value);
        public void ApplyStreamingFinal(AssistantFinalEvent response) => store.ApplyStreamingFinal(response);
        public void ApplyRequestFailure(string assistantMessage, string detail) => store.ApplyRequestFailure(assistantMessage, detail);
        public void ApplyPlannerActionResult(string actionType, string taskId, string title, string detail) => store.ApplyPlannerActionResult(actionType, taskId, title, detail);
        public void AddUser(string text) => store.AddUser(text);
        public void AddAssistant(string text) => store.AddAssistant(text);
        public void SetThinking(bool value) => store.SetThinking(value);
        public void SetListening(bool value) => store.SetListening(value);
        public void SetTalking(bool value) => store.SetTalking(value);
        public void SetTranscriptPreview(string value) => store.SetTranscriptPreview(value);
        public void SetSystemStatus(string badge, string title, string detail) => store.SetSystemStatus(badge, title, detail);
        public void ClearSystemStatus() => store.ClearSystemStatus();
        public void ResetAssistantDraft() => store.ResetAssistantDraft();
        public void AppendAssistantDraft(string value) => store.AppendAssistantDraft(value);
        public void FinalizeAssistantDraft(string fallbackText = null) => store.FinalizeAssistantDraft(fallbackText);
        public void SetDiagnostics(string route, string provider, int latencyMs, bool fallbackUsed) => store.SetDiagnostics(route, provider, latencyMs, fallbackUsed);
        public void SetTaskActions(System.Collections.Generic.IReadOnlyList<TaskActionReport> actions) => store.SetTaskActions(actions);

        private void HandleSendRequested(string message) => SendRequested?.Invoke(message);
        private void HandleMicRequested() => MicRequested?.Invoke();
    }
}
