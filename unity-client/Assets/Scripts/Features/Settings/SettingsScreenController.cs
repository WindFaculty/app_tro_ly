using LocalAssistant.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Settings
{
    public sealed class SettingsScreenController
    {
        private readonly AssistantUiRefs ui;

        public SettingsScreenController(AssistantUiRefs ui)
        {
            this.ui = ui;
        }

        public void Render(SettingsViewModelStore settingsStore)
        {
            if (ui.SpeakRepliesToggle != null)
            {
                ui.SpeakRepliesToggle.SetValueWithoutNotify(settingsStore.Current.voice.speak_replies);
            }

            if (ui.TranscriptPreviewToggle != null)
            {
                ui.TranscriptPreviewToggle.SetValueWithoutNotify(settingsStore.Current.voice.show_transcript_preview);
            }

            if (ui.MiniAssistantToggle != null)
            {
                ui.MiniAssistantToggle.SetValueWithoutNotify(settingsStore.Current.window_mode.mini_assistant_enabled);
            }

            if (ui.ReminderSpeechToggle != null)
            {
                ui.ReminderSpeechToggle.SetValueWithoutNotify(settingsStore.Current.reminder.speech_enabled);
            }

            if (ui.SettingsSummaryText != null)
            {
                ui.SettingsSummaryText.text = settingsStore.BuildSummary();
            }
        }

        public void SetStatus(string message, Color color)
        {
            if (ui.SettingsStatusText == null)
            {
                return;
            }

            ui.SettingsStatusText.text = message;
            ui.SettingsStatusText.style.color = new StyleColor(color);
        }

        public void SetEditable(bool isEditable)
        {
            ui.SaveSettingsButton?.SetEnabled(isEditable);
            ui.ReloadSettingsButton?.SetEnabled(isEditable);
            ui.SpeakRepliesToggle?.SetEnabled(isEditable);
            ui.TranscriptPreviewToggle?.SetEnabled(isEditable);
            ui.MiniAssistantToggle?.SetEnabled(isEditable);
            ui.ReminderSpeechToggle?.SetEnabled(isEditable);
        }
    }
}
