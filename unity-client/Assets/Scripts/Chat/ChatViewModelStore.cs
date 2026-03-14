using System.Collections.Generic;
using System.Text;
using LocalAssistant.Core;

namespace LocalAssistant.Chat
{
    public sealed class ChatViewModelStore
    {
        public sealed class ChatLine
        {
            public string Role = "assistant";
            public string Text = string.Empty;
        }

        public List<ChatLine> Lines { get; } = new();
        public string ConversationId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string TranscriptPreview { get; private set; } = string.Empty;
        public string AssistantDraft { get; private set; } = string.Empty;
        public string CurrentRoute { get; private set; } = string.Empty;
        public string CurrentProvider { get; private set; } = string.Empty;
        public int CurrentLatencyMs { get; private set; }
        public int FallbackCount { get; private set; }
        public bool IsThinking { get; private set; }
        public bool IsListening { get; private set; }

        public void AddUser(string text)
        {
            Lines.Add(new ChatLine { Role = "user", Text = text });
        }

        public void AddAssistant(string text)
        {
            Lines.Add(new ChatLine { Role = "assistant", Text = text });
        }

        public void SetThinking(bool value) => IsThinking = value;
        public void SetListening(bool value) => IsListening = value;
        public void SetTranscriptPreview(string value) => TranscriptPreview = value ?? string.Empty;
        public void ResetAssistantDraft() => AssistantDraft = string.Empty;
        public void AppendAssistantDraft(string value) => AssistantDraft += value ?? string.Empty;
        public void FinalizeAssistantDraft(string fallbackText = null)
        {
            var finalText = string.IsNullOrWhiteSpace(fallbackText) ? AssistantDraft : fallbackText;
            if (!string.IsNullOrWhiteSpace(finalText))
            {
                AddAssistant(finalText.Trim());
            }

            AssistantDraft = string.Empty;
        }

        public void SetDiagnostics(string route, string provider, int latencyMs, bool fallbackUsed)
        {
            CurrentRoute = route ?? string.Empty;
            CurrentProvider = provider ?? string.Empty;
            CurrentLatencyMs = latencyMs;
            if (fallbackUsed)
            {
                FallbackCount += 1;
            }
        }

        public string BuildTranscript()
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(CurrentRoute) || !string.IsNullOrEmpty(CurrentProvider))
            {
                builder.AppendLine($"<color=#FFD0A5><b>Route</b></color> {CurrentRoute} | {CurrentProvider} | {CurrentLatencyMs} ms");
                builder.AppendLine($"<color=#FFD0A5><b>Fallbacks</b></color> {FallbackCount}");
                builder.AppendLine();
            }

            foreach (var line in Lines)
            {
                var badge = line.Role == "user" ? "ME" : "AI";
                var badgeColor = line.Role == "user" ? "#FFF2E8" : "#FFC28F";
                builder.AppendLine($"<color={badgeColor}><b>{badge}</b></color>");
                builder.AppendLine(line.Text);
                builder.AppendLine();
            }

            if (!string.IsNullOrEmpty(AssistantDraft))
            {
                builder.AppendLine("<color=#FFC28F><b>AI</b></color>");
                builder.AppendLine(AssistantDraft.Trim());
                builder.AppendLine();
            }

            if (IsListening)
            {
                builder.AppendLine("<color=#FFB168><b>Listening...</b></color>");
            }
            else if (IsThinking)
            {
                builder.AppendLine("<color=#FFB168><b>Thinking...</b></color>");
            }

            if (!string.IsNullOrEmpty(TranscriptPreview))
            {
                builder.AppendLine();
                builder.AppendLine("<color=#FFD0A5><b>Transcript</b></color>");
                builder.AppendLine(TranscriptPreview);
            }

            return builder.ToString().Trim();
        }

        public static ChatRequestPayload CreateRequest(string text, string conversationId, string selectedDate, bool includeVoice)
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
