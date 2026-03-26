using LocalAssistant.Core;
using System;
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

        public void RequestReload() => ReloadRequested?.Invoke();
        public void RequestSave() => SaveRequested?.Invoke();
        public void NotifySpeakRepliesChanged(bool value) => SpeakRepliesChanged?.Invoke(value);
        public void NotifyTranscriptPreviewChanged(bool value) => TranscriptPreviewChanged?.Invoke(value);
        public void NotifyMiniAssistantChanged(bool value) => MiniAssistantChanged?.Invoke(value);
        public void NotifyReminderSpeechChanged(bool value) => ReminderSpeechChanged?.Invoke(value);
    }
}
