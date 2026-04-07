using System;
using LocalAssistant.Core;

namespace LocalAssistant.Features.Chat
{
    public static class ChatRequestFactory
    {
        public static ChatRequestPayload CreateTextRequest(string text, string conversationId, string selectedDate, bool includeVoice)
        {
            return new ChatRequestPayload
            {
                message = text,
                conversation_id = string.IsNullOrEmpty(conversationId) ? null : conversationId,
                session_id = null,
                selected_date = string.IsNullOrEmpty(selectedDate) ? null : selectedDate,
                mode = "text",
                include_voice = includeVoice,
                voice_mode = false,
            };
        }
    }
}
