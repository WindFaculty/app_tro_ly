using System.Text;
using LocalAssistant.App;

namespace LocalAssistant.Core
{
    public sealed class ShellStageSnapshot
    {
        public string AvatarStateText = string.Empty;
        public string StageStatusText = string.Empty;
        public string InsightTitle = string.Empty;
        public string InsightSummary = string.Empty;
        public string InsightMeta = string.Empty;
    }

    public static class ShellStageSnapshotBuilder
    {
        public static ShellStageSnapshot Build(
            AvatarState avatarState,
            HealthResponse health,
            AppScreen currentScreen,
            string selectedDate,
            ISettingsStateSource settingsState,
            IChatStatusSource chatStatus)
        {
            health ??= new HealthResponse();
            var settings = settingsState?.Current ?? new SettingsPayload();

            return new ShellStageSnapshot
            {
                AvatarStateText = avatarState.ToString(),
                StageStatusText = BuildStageStatusText(health, currentScreen, selectedDate, settings, chatStatus),
                InsightTitle = BuildScheduleInsightTitle(currentScreen),
                InsightSummary = BuildScheduleInsightSummary(health, currentScreen),
                InsightMeta = BuildScheduleInsightMeta(selectedDate, settings, chatStatus),
            };
        }

        private static string BuildStageStatusText(
            HealthResponse health,
            AppScreen currentScreen,
            string selectedDate,
            SettingsPayload settings,
            IChatStatusSource chatStatus)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"DB {ToOnOff(health.database.available)} | Focus {ToTaskTabName(currentScreen)} | Date {ToDisplayDate(selectedDate)}");
            builder.AppendLine($"{BuildLlmStatus(health.runtimes.llm)}  |  STT {ToOnOff(health.runtimes.stt.available)}  |  TTS {ToOnOff(health.runtimes.tts.available)}");
            builder.AppendLine($"Voice replies {ToOnOff(settings.voice.speak_replies)}  |  Transcript {ToOnOff(settings.voice.show_transcript_preview)}");
            builder.Append($"Route {ToTextOrUnknown(chatStatus?.CurrentRoute)}  |  Provider {ToTextOrUnknown(chatStatus?.CurrentProvider)}  |  Fallbacks {chatStatus?.FallbackCount ?? 0}");
            return builder.ToString().Trim();
        }

        private static string BuildScheduleInsightTitle(AppScreen screen) => screen switch
        {
            AppScreen.Inbox => "Inbox triage",
            AppScreen.Completed => "Completed review",
            AppScreen.Today => "Today focus",
            _ => "Week overview",
        };

        private static string BuildScheduleInsightSummary(HealthResponse health, AppScreen currentScreen)
        {
            if (health.status == "error")
            {
                return "Backend is unavailable. Keep this shell in recovery mode until the local services come back.";
            }

            if (health.status == "partial")
            {
                return currentScreen switch
                {
                    AppScreen.Inbox => "Some runtime features are degraded, so this panel stays text-first while you sort new work.",
                    AppScreen.Completed => "Some runtime features are degraded, so use completed review to spot follow-up items manually.",
                    _ => "Some runtime features are degraded, so keep the schedule readable while chat and voice recover.",
                };
            }

            return currentScreen switch
            {
                AppScreen.Inbox => "Sort quick captures and unscheduled work here before they land on the calendar.",
                AppScreen.Completed => "Review what finished recently and watch for items that still need a follow-up.",
                AppScreen.Today => "Use the stage summary for quick-add and keep the selected-day planner sheet close at hand.",
                _ => "Keep the week visible while the assistant collects routing and reminder updates in the background.",
            };
        }

        private static string BuildScheduleInsightMeta(string selectedDate, SettingsPayload settings, IChatStatusSource chatStatus)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Date {ToDisplayDate(selectedDate)}");
            builder.AppendLine($"Route {ToTextOrUnknown(chatStatus?.CurrentRoute)} | Provider {ToTextOrUnknown(chatStatus?.CurrentProvider)}");
            builder.Append($"Voice {ToOnOff(settings.voice.speak_replies)} | Transcript {ToOnOff(settings.voice.show_transcript_preview)} | Fallbacks {chatStatus?.FallbackCount ?? 0}");
            return builder.ToString();
        }

        private static string BuildLlmStatus(RuntimeHealth runtime)
        {
            var label = "LLM";
            if (runtime != null && !string.IsNullOrWhiteSpace(runtime.provider))
            {
                label += " " + runtime.provider;
            }

            return $"{label} {ToOnOff(runtime != null && runtime.available)}";
        }

        private static string ToOnOff(bool value) => value ? "On" : "Off";
        private static string ToDisplayDate(string value) => string.IsNullOrWhiteSpace(value) ? "auto" : value;
        private static string ToTextOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;

        private static string ToTaskTabName(AppScreen screen) => screen switch
        {
            AppScreen.Today => "Today",
            AppScreen.Week => "Week",
            AppScreen.Inbox => "Inbox",
            AppScreen.Completed => "Completed",
            _ => "Week",
        };
    }
}
