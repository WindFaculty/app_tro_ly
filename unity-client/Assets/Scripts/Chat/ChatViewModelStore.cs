using System;
using System.Collections.Generic;
using System.Text;
using LocalAssistant.Core;

namespace LocalAssistant.Chat
{
    public sealed class ChatPanelSnapshot
    {
        public string Transcript = string.Empty;
        public string StatusBadge = "READY";
        public string StatusTitle = "Ready for the next turn";
        public string StatusDetail = string.Empty;
        public string RouteBadgeText = "Route pending";
        public string TranscriptPreviewTitle = "Transcript preview";
        public string TranscriptPreviewText = string.Empty;
        public string ActionSummaryTitle = "Action confirmation";
        public string ActionSummaryText = string.Empty;
    }

    public sealed class ChatViewModelStore : IChatStatusSource
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
        public bool IsTalking { get; private set; }
        public string SystemStatusBadge { get; private set; } = string.Empty;
        public string SystemStatusTitle { get; private set; } = string.Empty;
        public string SystemStatusDetail { get; private set; } = string.Empty;
        public string LastActionTitle { get; private set; } = "Action confirmation";
        public string LastActionSummary { get; private set; } = "Task changes triggered from chat will appear here.";

        public void BeginTurn(string message, bool fromVoice, bool transcriptPreviewEnabled)
        {
            ClearSystemStatus();
            AddUser(message);
            SetListening(false);
            SetThinking(true);
            SetTalking(false);
            SetTaskActions(Array.Empty<TaskActionReport>());
            ResetAssistantDraft();
            if (fromVoice && transcriptPreviewEnabled)
            {
                SetTranscriptPreview(message);
            }
        }

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
        public void SetTalking(bool value) => IsTalking = value;
        public void SetTranscriptPreview(string value) => TranscriptPreview = value ?? string.Empty;
        public void SetSystemStatus(string badge, string title, string detail)
        {
            SystemStatusBadge = badge ?? string.Empty;
            SystemStatusTitle = title ?? string.Empty;
            SystemStatusDetail = detail ?? string.Empty;
        }

        public void ClearSystemStatus()
        {
            SystemStatusBadge = string.Empty;
            SystemStatusTitle = string.Empty;
            SystemStatusDetail = string.Empty;
        }

        public void ResetAssistantDraft() => AssistantDraft = string.Empty;
        public void AppendAssistantDraft(string value) => AssistantDraft += value ?? string.Empty;
        public void ApplyAssistantChunk(string value) => AppendAssistantDraft(string.IsNullOrWhiteSpace(value) ? string.Empty : value + " ");

        public void FinalizeAssistantDraft(string fallbackText = null)
        {
            var finalText = string.IsNullOrWhiteSpace(fallbackText) ? AssistantDraft : fallbackText;
            if (!string.IsNullOrWhiteSpace(finalText))
            {
                AddAssistant(finalText.Trim());
            }

            AssistantDraft = string.Empty;
        }

        public void ApplyCompatibilityResponse(ChatResponsePayload response)
        {
            ApplyFinalReply(
                response?.conversation_id,
                null,
                response?.reply_text,
                response?.route,
                response?.provider,
                response?.latency_ms ?? 0,
                response?.fallback_used ?? false,
                response?.task_actions);
        }

        public void ApplyTranscriptPartial(string value, bool transcriptPreviewEnabled)
        {
            if (!transcriptPreviewEnabled)
            {
                return;
            }

            SetTranscriptPreview(value);
        }

        public void ApplyTranscriptFinal(string value, bool transcriptPreviewEnabled)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            SetTranscriptPreview(transcriptPreviewEnabled ? value : string.Empty);
            AddUser(value);
            SetThinking(true);
            SetTalking(false);
            ResetAssistantDraft();
        }

        public void ApplyRouteSelection(string route, string provider)
        {
            SetDiagnostics(route, provider, 0, false);
        }

        public void ApplyStreamingFinal(AssistantFinalEvent response)
        {
            ApplyFinalReply(
                response?.conversation_id,
                response?.session_id,
                response?.reply_text,
                response?.route,
                response?.provider,
                response?.latency_ms ?? 0,
                response?.fallback_used ?? false,
                response?.task_actions);
        }

        public void ApplyRequestFailure(string assistantMessage, string detail)
        {
            SetThinking(false);
            SetTalking(false);
            AddAssistant(assistantMessage);
            SetSystemStatus("ERROR", "Chat request failed", detail);
        }

        public void ApplyPlannerActionResult(string actionType, string taskId, string title, string detail)
        {
            SetTaskActions(new[]
            {
                new TaskActionReport
                {
                    type = actionType ?? string.Empty,
                    status = "applied",
                    task_id = taskId ?? string.Empty,
                    title = title ?? string.Empty,
                    detail = detail ?? string.Empty,
                },
            });
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

        public void SetTaskActions(IReadOnlyList<TaskActionReport> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                LastActionTitle = "Action confirmation";
                LastActionSummary = "Task changes triggered from chat will appear here.";
                return;
            }

            LastActionTitle = actions.Count == 1 ? "Last task action" : $"Recent task actions ({actions.Count})";

            var builder = new StringBuilder();
            var limit = actions.Count > 3 ? 3 : actions.Count;
            for (var index = 0; index < limit; index++)
            {
                if (index > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(FormatAction(actions[index]));
            }

            if (actions.Count > limit)
            {
                builder.AppendLine();
                builder.Append($"+{actions.Count - limit} more update(s)");
            }

            LastActionSummary = builder.ToString().Trim();
        }

        public string BuildTranscript()
        {
            var builder = new StringBuilder();

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

            var transcript = builder.ToString().Trim();
            return string.IsNullOrWhiteSpace(transcript)
                ? "Conversation will appear here once a message or voice turn arrives."
                : transcript;
        }

        public ChatPanelSnapshot BuildPanelSnapshot(bool transcriptPreviewEnabled)
        {
            return new ChatPanelSnapshot
            {
                Transcript = BuildTranscript(),
                StatusBadge = BuildStatusBadge(),
                StatusTitle = BuildStatusTitle(),
                StatusDetail = BuildStatusDetail(transcriptPreviewEnabled),
                RouteBadgeText = BuildRouteBadgeText(),
                TranscriptPreviewTitle = transcriptPreviewEnabled ? "Transcript preview" : "Transcript preview off",
                TranscriptPreviewText = BuildTranscriptPreviewText(transcriptPreviewEnabled),
                ActionSummaryTitle = LastActionTitle,
                ActionSummaryText = LastActionSummary,
            };
        }

        private string BuildStatusBadge()
        {
            if (IsTalking)
            {
                return "TALKING";
            }

            if (IsListening)
            {
                return "LISTENING";
            }

            if (IsThinking)
            {
                return "THINKING";
            }

            if (HasSystemStatus())
            {
                return SystemStatusBadge;
            }

            return HasRecentAction() ? "CONFIRM" : "READY";
        }

        private string BuildStatusTitle()
        {
            if (IsTalking)
            {
                return "Speaking the current reply";
            }

            if (IsListening)
            {
                return "Listening for your next voice turn";
            }

            if (IsThinking)
            {
                return "Planning the next response";
            }

            if (HasSystemStatus())
            {
                return SystemStatusTitle;
            }

            return HasRecentAction()
                ? "Applied task updates from the latest turn"
                : "Ready for the next turn";
        }

        private string BuildStatusDetail(bool transcriptPreviewEnabled)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(CurrentRoute) || !string.IsNullOrWhiteSpace(CurrentProvider))
            {
                builder.AppendLine($"Route {ToKnownText(CurrentRoute)} | Provider {ToKnownText(CurrentProvider)} | Latency {CurrentLatencyMs} ms");
            }
            else if (HasSystemStatus())
            {
                builder.AppendLine(SystemStatusDetail);
            }
            else
            {
                builder.AppendLine("Route diagnostics will appear after the assistant selects a provider.");
            }

            builder.Append($"Fallbacks {FallbackCount} | Transcript {(transcriptPreviewEnabled ? "On" : "Off")}");
            return builder.ToString().Trim();
        }

        private string BuildTranscriptPreviewText(bool transcriptPreviewEnabled)
        {
            if (!transcriptPreviewEnabled)
            {
                return "Enable transcript preview in Settings to inspect captured speech before and during voice turns.";
            }

            if (!string.IsNullOrWhiteSpace(TranscriptPreview))
            {
                return TranscriptPreview;
            }

            if (IsListening)
            {
                return "Live speech text will appear here while the microphone is active.";
            }

            if (IsThinking)
            {
                return "Waiting for the latest captured speech to settle into a final transcript.";
            }

            return "Start the mic to inspect captured speech before or during a voice turn.";
        }

        private string BuildRouteBadgeText()
        {
            if (!string.IsNullOrWhiteSpace(CurrentRoute) && !string.IsNullOrWhiteSpace(CurrentProvider))
            {
                return $"{CurrentRoute} / {CurrentProvider}";
            }

            if (!string.IsNullOrWhiteSpace(CurrentRoute))
            {
                return CurrentRoute;
            }

            if (!string.IsNullOrWhiteSpace(CurrentProvider))
            {
                return CurrentProvider;
            }

            return "Route pending";
        }

        private bool HasRecentAction()
        {
            return !string.IsNullOrWhiteSpace(LastActionSummary)
                && LastActionSummary != "Task changes triggered from chat will appear here.";
        }

        private bool HasSystemStatus()
        {
            return !string.IsNullOrWhiteSpace(SystemStatusBadge)
                && !string.IsNullOrWhiteSpace(SystemStatusTitle);
        }

        private void ApplyFinalReply(
            string conversationId,
            string sessionId,
            string replyText,
            string route,
            string provider,
            int latencyMs,
            bool fallbackUsed,
            IReadOnlyList<TaskActionReport> actions)
        {
            ConversationId = conversationId ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                SessionId = sessionId;
            }

            SetListening(false);
            SetThinking(false);
            SetTalking(false);
            FinalizeAssistantDraft(replyText);
            SetDiagnostics(route, provider, latencyMs, fallbackUsed);
            SetTaskActions(actions);
        }

        private static string FormatAction(TaskActionReport action)
        {
            var title = string.IsNullOrWhiteSpace(action?.title) ? "Untitled task" : action.title;
            var detail = string.IsNullOrWhiteSpace(action?.detail) ? string.Empty : $" {action.detail.Trim()}";
            return action?.type switch
            {
                "create_task" => $"Created '{title}'.{detail}".Trim(),
                "complete_task" => $"Marked '{title}' complete.{detail}".Trim(),
                "reschedule_task" => $"Rescheduled '{title}'.{detail}".Trim(),
                "priority_task" => $"Raised priority for '{title}'.{detail}".Trim(),
                _ => $"Applied {ToKnownText(action?.type)} for '{title}'.{detail}".Trim(),
            };
        }

        private static string ToKnownText(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;
    }
}
