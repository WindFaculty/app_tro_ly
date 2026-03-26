using LocalAssistant.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Settings
{
    public sealed class SettingsScreenController
    {
        private readonly SettingsScreenRefs settings;

        public SettingsScreenController(SettingsScreenRefs settings)
        {
            this.settings = settings;
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

            if (settings.SettingsSummaryText != null)
            {
                settings.SettingsSummaryText.text = settingsStore.BuildSummary();
            }
        }

        public void SetStatus(string message, Color color)
        {
            if (settings.SettingsStatusText == null)
            {
                return;
            }

            settings.SettingsStatusText.text = message;
            settings.SettingsStatusText.style.color = new StyleColor(color);
        }

        public void SetEditable(bool isEditable)
        {
            settings.SaveSettingsButton?.SetEnabled(isEditable);
            settings.ReloadSettingsButton?.SetEnabled(isEditable);
            settings.SpeakRepliesToggle?.SetEnabled(isEditable);
            settings.TranscriptPreviewToggle?.SetEnabled(isEditable);
            settings.MiniAssistantToggle?.SetEnabled(isEditable);
            settings.ReminderSpeechToggle?.SetEnabled(isEditable);
        }
    }
}
