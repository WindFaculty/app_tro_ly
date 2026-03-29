using System.Text;

namespace LocalAssistant.Core
{
    public sealed class SettingsViewModelStore
    {
        public SettingsPayload Current { get; private set; } = new();
        private SettingsPayload baseline = new();

        public void Apply(SettingsPayload payload)
        {
            Current = Clone(payload ?? new SettingsPayload());
            baseline = Clone(Current);
        }

        public SettingsPayload Snapshot()
        {
            return Clone(Current);
        }

        public bool HasUnsavedChanges()
        {
            return UnityJson.Serialize(Current) != UnityJson.Serialize(baseline);
        }

        public void MarkSaved()
        {
            baseline = Clone(Current);
        }

        public void SetSpeakReplies(bool value) => Current.voice.speak_replies = value;
        public void SetTranscriptPreview(bool value) => Current.voice.show_transcript_preview = value;
        public void SetMiniAssistantEnabled(bool value) => Current.window_mode.mini_assistant_enabled = value;
        public void SetReminderSpeechEnabled(bool value) => Current.reminder.speech_enabled = value;

        public string BuildSummary()
        {
            var payload = Current;
            var builder = new StringBuilder();
            builder.AppendLine($"Model: {payload.model.provider} / {payload.model.name}");
            builder.AppendLine($"Routing: {payload.model.routing_mode} | Fast {payload.model.fast_provider} | Deep {payload.model.deep_provider}");
            builder.AppendLine($"Voice: {payload.voice.tts_voice}");
            builder.AppendLine($"Input: {payload.voice.input_mode}");
            builder.AppendLine($"Voice replies: {ToOnOff(payload.voice.speak_replies)}");
            builder.AppendLine($"Mini assistant: {ToOnOff(payload.window_mode.mini_assistant_enabled)}");
            builder.AppendLine($"Reminder lead: {payload.reminder.lead_minutes} minutes");
            builder.AppendLine($"Memory: {ToOnOff(payload.memory.auto_extract)} | {payload.memory.short_term_turn_limit} turns");
            return builder.ToString().Trim();
        }

        private static string ToOnOff(bool value) => value ? "On" : "Off";

        private static SettingsPayload Clone(SettingsPayload payload)
        {
            return UnityJson.Deserialize<SettingsPayload>(UnityJson.Serialize(payload));
        }
    }
}
