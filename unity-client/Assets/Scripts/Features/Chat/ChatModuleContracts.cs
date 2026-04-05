using System;
using LocalAssistant.Chat;
using LocalAssistant.Core;

namespace LocalAssistant.Features.Chat
{
    public interface IChatModule : IChatStatusSource
    {
        event Action<string> SendRequested;
        event Action MicRequested;

        string ConversationId { get; set; }
        string SessionId { get; set; }

        void Bind();
        ChatPanelSnapshot Render(bool transcriptPreviewEnabled);
        void SubmitCurrentInput();
        void RequestMicToggle();
        void SetInteractable(bool canSendText, bool canUseMic);
        void BeginTurn(string message, bool fromVoice, bool transcriptPreviewEnabled);
        void ApplyCompatibilityResponse(ChatResponsePayload response);
        void ApplyTranscriptPartial(string value, bool transcriptPreviewEnabled);
        void ApplyTranscriptFinal(string value, bool transcriptPreviewEnabled);
        void ApplyRouteSelection(string route, string provider);
        void ApplyAssistantChunk(string value);
        void ApplyStreamingFinal(AssistantFinalEvent response);
        void ApplyRequestFailure(string assistantMessage, string detail);
        void ApplyPlannerActionResult(string actionType, string taskId, string title, string detail);
        void AddUser(string text);
        void AddAssistant(string text);
        void SetThinking(bool value);
        void SetListening(bool value);
        void SetTalking(bool value);
        void SetTranscriptPreview(string value);
        void SetSystemStatus(string badge, string title, string detail);
        void ClearSystemStatus();
        void ResetAssistantDraft();
        void AppendAssistantDraft(string value);
        void FinalizeAssistantDraft(string fallbackText = null);
        void SetDiagnostics(string route, string provider, int latencyMs, bool fallbackUsed);
        void SetTaskActions(System.Collections.Generic.IReadOnlyList<TaskActionReport> actions);
    }
}
