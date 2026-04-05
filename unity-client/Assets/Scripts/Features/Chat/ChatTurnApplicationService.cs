using LocalAssistant.Core;

namespace LocalAssistant.Features.Chat
{
    public enum ChatTurnTransport
    {
        Compatibility,
        Streaming,
    }

    public sealed class ChatTurnRequest
    {
        private ChatTurnRequest()
        {
        }

        public string Message { get; private set; } = string.Empty;
        public string ConversationId { get; private set; } = string.Empty;
        public string SessionId { get; private set; } = string.Empty;
        public string SelectedDate { get; private set; } = string.Empty;
        public bool FromVoice { get; private set; }
        public bool IncludeVoiceReplies { get; private set; }

        public static bool TryCreate(
            string message,
            string conversationId,
            string sessionId,
            string selectedDate,
            bool fromVoice,
            bool includeVoiceReplies,
            out ChatTurnRequest request)
        {
            var trimmed = message?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                request = null;
                return false;
            }

            request = new ChatTurnRequest
            {
                Message = trimmed,
                ConversationId = conversationId ?? string.Empty,
                SessionId = sessionId ?? string.Empty,
                SelectedDate = selectedDate ?? string.Empty,
                FromVoice = fromVoice,
                IncludeVoiceReplies = includeVoiceReplies,
            };
            return true;
        }
    }

    public sealed class ChatTurnExecutionPlan
    {
        public ChatTurnTransport Transport { get; set; }
        public ChatRequestPayload CompatibilityRequest { get; set; }
        public AssistantStreamRequestPayload StreamRequest { get; set; }
    }

    public sealed class ChatTurnApplicationService
    {
        public ChatTurnExecutionPlan BuildPlan(ChatTurnRequest request, bool streamingAvailable)
        {
            if (streamingAvailable)
            {
                return new ChatTurnExecutionPlan
                {
                    Transport = ChatTurnTransport.Streaming,
                    StreamRequest = new AssistantStreamRequestPayload
                    {
                        type = "text_turn",
                        session_id = request?.SessionId ?? string.Empty,
                        conversation_id = request?.ConversationId ?? string.Empty,
                        message = request?.Message ?? string.Empty,
                        selected_date = request?.SelectedDate ?? string.Empty,
                        voice_mode = request != null && request.FromVoice,
                    },
                };
            }

            return new ChatTurnExecutionPlan
            {
                Transport = ChatTurnTransport.Compatibility,
                CompatibilityRequest = ChatRequestFactory.CreateTextRequest(
                    request?.Message ?? string.Empty,
                    request?.ConversationId ?? string.Empty,
                    request?.SelectedDate ?? string.Empty,
                    request != null && request.IncludeVoiceReplies),
            };
        }
    }
}
