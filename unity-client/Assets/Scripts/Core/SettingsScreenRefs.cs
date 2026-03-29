using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class SettingsScreenRefs
    {
        public VisualElement SettingsPanel;
        public Label SettingsSummaryText;
        public Label SettingsStatusText;
        public Label SettingsActionHintText;
        public Label SettingsVoiceSummaryText;
        public Label SettingsAutomationSummaryText;
        public Label SettingsModelSummaryText;
        public Label SettingsMemorySummaryText;
        public Toggle SpeakRepliesToggle;
        public Toggle TranscriptPreviewToggle;
        public Toggle MiniAssistantToggle;
        public Toggle ReminderSpeechToggle;
        public Button ReloadSettingsButton;
        public Button SaveSettingsButton;

        public SettingsScreenRefs()
        {
        }

        public SettingsScreenRefs(VisualElement root)
        {
            SettingsPanel = root.Q<VisualElement>(UiElementNames.Settings.SettingsPanel);
            SettingsSummaryText = root.Q<Label>(UiElementNames.Settings.SettingsSummaryText);
            SettingsStatusText = root.Q<Label>(UiElementNames.Settings.SettingsStatusText);
            SettingsActionHintText = root.Q<Label>(UiElementNames.Settings.SettingsActionHintText);
            SettingsVoiceSummaryText = root.Q<Label>(UiElementNames.Settings.SettingsVoiceSummaryText);
            SettingsAutomationSummaryText = root.Q<Label>(UiElementNames.Settings.SettingsAutomationSummaryText);
            SettingsModelSummaryText = root.Q<Label>(UiElementNames.Settings.SettingsModelSummaryText);
            SettingsMemorySummaryText = root.Q<Label>(UiElementNames.Settings.SettingsMemorySummaryText);
            SpeakRepliesToggle = root.Q<Toggle>(UiElementNames.Settings.SpeakRepliesToggle);
            TranscriptPreviewToggle = root.Q<Toggle>(UiElementNames.Settings.TranscriptPreviewToggle);
            MiniAssistantToggle = root.Q<Toggle>(UiElementNames.Settings.MiniAssistantToggle);
            ReminderSpeechToggle = root.Q<Toggle>(UiElementNames.Settings.ReminderSpeechToggle);
            ReloadSettingsButton = root.Q<Button>(UiElementNames.Settings.ReloadButton);
            SaveSettingsButton = root.Q<Button>(UiElementNames.Settings.SaveButton);
        }
    }
}
