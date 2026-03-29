using LocalAssistant.Core;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Settings
{
    public sealed class SettingsScreenController
    {
        private readonly SettingsScreenRefs settings;
        private bool isEditable = true;

        public SettingsScreenController(SettingsScreenRefs settings)
        {
            this.settings = settings;
        }

        public event Action ReloadRequested;
        public event Action SaveRequested;
        public event Action<bool> SpeakRepliesChanged;
        public event Action<bool> TranscriptPreviewChanged;
        public event Action<bool> MiniAssistantChanged;
        public event Action<bool> ReminderSpeechChanged;

        public void Bind()
        {
            if (settings.ReloadSettingsButton != null)
            {
                settings.ReloadSettingsButton.clicked += RequestReload;
            }

            if (settings.SaveSettingsButton != null)
            {
                settings.SaveSettingsButton.clicked += RequestSave;
            }

            if (settings.SpeakRepliesToggle != null)
            {
                settings.SpeakRepliesToggle.RegisterValueChangedCallback(evt => NotifySpeakRepliesChanged(evt.newValue));
            }

            if (settings.TranscriptPreviewToggle != null)
            {
                settings.TranscriptPreviewToggle.RegisterValueChangedCallback(evt => NotifyTranscriptPreviewChanged(evt.newValue));
            }

            if (settings.MiniAssistantToggle != null)
            {
                settings.MiniAssistantToggle.RegisterValueChangedCallback(evt => NotifyMiniAssistantChanged(evt.newValue));
            }

            if (settings.ReminderSpeechToggle != null)
            {
                settings.ReminderSpeechToggle.RegisterValueChangedCallback(evt => NotifyReminderSpeechChanged(evt.newValue));
            }
        }

        public void Render(SettingsViewModelStore settingsStore)
        {
            if (settings.SpeakRepliesToggle != null)
            {
                settings.SpeakRepliesToggle.SetValueWithoutNotify(settingsStore.Current.voice.speak_replies);
            }

            if (settings.TranscriptPreviewToggle != null)
            {
                settings.TranscriptPreviewToggle.SetValueWithoutNotify(settingsStore.Current.voice.show_transcript_preview);
            }

            if (settings.MiniAssistantToggle != null)
            {
                settings.MiniAssistantToggle.SetValueWithoutNotify(settingsStore.Current.window_mode.mini_assistant_enabled);
            }

            if (settings.ReminderSpeechToggle != null)
            {
                settings.ReminderSpeechToggle.SetValueWithoutNotify(settingsStore.Current.reminder.speech_enabled);
            }

            SetLabel(settings.SettingsSummaryText, BuildTopSummary(settingsStore));
            SetLabel(settings.SettingsActionHintText, BuildActionHint(settingsStore));
            SetLabel(settings.SettingsVoiceSummaryText, BuildVoiceSummary(settingsStore));
            SetLabel(settings.SettingsAutomationSummaryText, BuildAutomationSummary(settingsStore));
            SetLabel(settings.SettingsModelSummaryText, BuildModelSummary(settingsStore));
            SetLabel(settings.SettingsMemorySummaryText, BuildMemorySummary(settingsStore));
        }

        public void SetStatus(string message, Color color)
        {
            if (settings.SettingsStatusText == null)
            {
                return;
            }

            settings.SettingsStatusText.text = message;
            settings.SettingsStatusText.style.color = new StyleColor(color);
            settings.SettingsStatusText.style.backgroundColor = new StyleColor(new Color(color.r, color.g, color.b, 0.18f));
        }

        public void SetEditable(bool isEditable)
        {
            this.isEditable = isEditable;
            settings.SaveSettingsButton?.SetEnabled(isEditable);
            settings.ReloadSettingsButton?.SetEnabled(isEditable);
            settings.SpeakRepliesToggle?.SetEnabled(isEditable);
            settings.TranscriptPreviewToggle?.SetEnabled(isEditable);
            settings.MiniAssistantToggle?.SetEnabled(isEditable);
            settings.ReminderSpeechToggle?.SetEnabled(isEditable);
        }

        public void RequestReload()
        {
            if (isEditable)
            {
                ReloadRequested?.Invoke();
            }
        }

        public void RequestSave()
        {
            if (isEditable)
            {
                SaveRequested?.Invoke();
            }
        }

        public void NotifySpeakRepliesChanged(bool value) => SpeakRepliesChanged?.Invoke(value);
        public void NotifyTranscriptPreviewChanged(bool value) => TranscriptPreviewChanged?.Invoke(value);
        public void NotifyMiniAssistantChanged(bool value) => MiniAssistantChanged?.Invoke(value);
        public void NotifyReminderSpeechChanged(bool value) => ReminderSpeechChanged?.Invoke(value);

        private static string BuildTopSummary(SettingsViewModelStore settingsStore)
        {
            var payload = settingsStore.Current;
            return
                $"{payload.model.provider} routing is {payload.model.routing_mode}. " +
                $"Voice replies are {ToOnOff(payload.voice.speak_replies)}, transcript preview is {ToOnOff(payload.voice.show_transcript_preview)}, and mini assistant is {ToOnOff(payload.window_mode.mini_assistant_enabled)}.";
        }

        private static string BuildActionHint(SettingsViewModelStore settingsStore)
        {
            return
                $"Reminder speech is {ToOnOff(settingsStore.Current.reminder.speech_enabled)}. " +
                "Reload first if the backend may have changed outside this client.";
        }

        private static string BuildVoiceSummary(SettingsViewModelStore settingsStore)
        {
            var voice = settingsStore.Current.voice;
            return
                $"Input {voice.input_mode} | Voice {voice.tts_voice} | Replies {ToOnOff(voice.speak_replies)} | Transcript {ToOnOff(voice.show_transcript_preview)}";
        }

        private static string BuildAutomationSummary(SettingsViewModelStore settingsStore)
        {
            var reminder = settingsStore.Current.reminder;
            var windowMode = settingsStore.Current.window_mode;
            return
                $"Mini assistant {ToOnOff(windowMode.mini_assistant_enabled)} | Main app {ToOnOff(windowMode.main_app_enabled)} | Reminder speech {ToOnOff(reminder.speech_enabled)}";
        }

        private static string BuildModelSummary(SettingsViewModelStore settingsStore)
        {
            var model = settingsStore.Current.model;
            var builder = new StringBuilder();
            builder.AppendLine($"Provider: {model.provider}");
            builder.AppendLine($"Model: {model.name}");
            builder.AppendLine($"Routing: {model.routing_mode}");
            builder.Append($"Fast {model.fast_provider} | Deep {model.deep_provider}");
            return builder.ToString();
        }

        private static string BuildMemorySummary(SettingsViewModelStore settingsStore)
        {
            var memory = settingsStore.Current.memory;
            var startup = settingsStore.Current.startup;
            var avatar = settingsStore.Current.avatar;
            var reminder = settingsStore.Current.reminder;
            var builder = new StringBuilder();
            builder.AppendLine($"Reminder lead: {reminder.lead_minutes} minutes");
            builder.AppendLine($"Memory extract: {ToOnOff(memory.auto_extract)} | Turns: {memory.short_term_turn_limit}");
            builder.AppendLine($"Startup backend: {ToOnOff(startup.launch_backend)} | Main app: {ToOnOff(startup.launch_main_app)}");
            builder.Append($"Avatar: {avatar.character} | Lip sync: {avatar.lip_sync_mode}");
            return builder.ToString();
        }

        private static string ToOnOff(bool value) => value ? "On" : "Off";

        private static void SetLabel(Label label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
