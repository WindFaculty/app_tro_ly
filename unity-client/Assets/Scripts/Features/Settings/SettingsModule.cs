using System;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.Features.Settings
{
    public sealed class SettingsModule : ISettingsModule
    {
        private static readonly Color WarningColor = new(0.74f, 0.49f, 0.14f, 1f);
        private static readonly Color SuccessColor = new(0.16f, 0.55f, 0.33f, 1f);

        private readonly SettingsViewModelStore store;
        private readonly SettingsScreenController screenController;
        private bool isBound;

        public SettingsModule(SettingsScreenRefs refs)
            : this(new SettingsScreenController(refs), new SettingsViewModelStore())
        {
        }

        public SettingsModule(SettingsScreenController screenController, SettingsViewModelStore store)
        {
            this.screenController = screenController ?? throw new ArgumentNullException(nameof(screenController));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public event Action ReloadRequested;
        public event Action SaveRequested;
        public event Action SettingsChanged;

        public SettingsPayload Current => store.Current;
        public bool HasUnsavedChanges => store.HasUnsavedChanges();

        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            isBound = true;
            screenController.Bind();
            screenController.ReloadRequested += HandleReloadRequested;
            screenController.SaveRequested += HandleSaveRequested;
            screenController.SpeakRepliesChanged += HandleSpeakRepliesChanged;
            screenController.TranscriptPreviewChanged += HandleTranscriptPreviewChanged;
            screenController.MiniAssistantChanged += HandleMiniAssistantChanged;
            screenController.ReminderSpeechChanged += HandleReminderSpeechChanged;
        }

        public SettingsPayload Snapshot() => store.Snapshot();

        public void Apply(SettingsPayload payload)
        {
            store.Apply(payload);
            Render();
        }

        public void SetStatus(string message, Color color) => screenController.SetStatus(message, color);

        public void SetEditable(bool isEditable) => screenController.SetEditable(isEditable);

        private void HandleReloadRequested() => ReloadRequested?.Invoke();
        private void HandleSaveRequested() => SaveRequested?.Invoke();
        private void HandleSpeakRepliesChanged(bool value) => ApplyLocalChange(() => store.SetSpeakReplies(value));
        private void HandleTranscriptPreviewChanged(bool value) => ApplyLocalChange(() => store.SetTranscriptPreview(value));
        private void HandleMiniAssistantChanged(bool value) => ApplyLocalChange(() => store.SetMiniAssistantEnabled(value));
        private void HandleReminderSpeechChanged(bool value) => ApplyLocalChange(() => store.SetReminderSpeechEnabled(value));

        private void ApplyLocalChange(Action mutation)
        {
            mutation?.Invoke();
            Render();
            SetStatus(
                HasUnsavedChanges ? "Unsaved changes." : "Settings saved.",
                HasUnsavedChanges ? WarningColor : SuccessColor);
            SettingsChanged?.Invoke();
        }

        private void Render() => screenController.Render(store);
    }
}
