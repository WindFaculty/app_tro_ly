using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class AssistantUiRefs
    {
        public Label HealthBanner;
        public Label AvatarStateText;
        public Label StageStatusText;
        public Label StagePlaceholderText;
        public Label SubtitleText;
        public VisualElement SubtitleCard;
        public Label TaskSummaryText;
        public Label TaskContentText;
        public VisualElement SettingsPanel;
        public Label SettingsSummaryText;
        public Label SettingsStatusText;
        public Toggle SpeakRepliesToggle;
        public Toggle TranscriptPreviewToggle;
        public Toggle MiniAssistantToggle;
        public Toggle ReminderSpeechToggle;
        public Button ReloadSettingsButton;
        public Button SaveSettingsButton;
        public Label ChatLogText;
        public VisualElement ReminderCard;
        public Label ReminderText;
        public TextField ChatInput;
        public TextField QuickAddInput;
        public Button SendButton;
        public Button MicButton;
        public Button QuickAddButton;
        public Button RefreshButton;

        public Button TodayTab;
        public Button WeekTab;
        public Button InboxTab;
        public Button CompletedTab;
        public Button SettingsTab;
        
        public VisualElement HomeViewContainer;
        public VisualElement ScheduleViewContainer;
        public VisualElement ChatPanelView;
        public VisualElement ScheduleSideView;
        
        public VisualElement CalendarArea;
        public Label TaskSheetHeaderTitle;
        public Label TaskSheetMonthLabel;
    }

    public static class UiFactory
    {
        public static AssistantUiRefs Build(Transform parent)
        {
            var refs = new AssistantUiRefs();

            var canvasRoot = new GameObject("AssistantUI_Toolkit", typeof(UIDocument));
            canvasRoot.transform.SetParent(parent, false);
            var uiDocument = canvasRoot.GetComponent<UIDocument>();
            
            var visualTree = Resources.Load<VisualTreeAsset>("UI/MainUI");
            if (visualTree == null)
            {
                Debug.LogError("Failed to load UI/MainUI.uxml from Resources");
                return refs;
            }
            uiDocument.visualTreeAsset = visualTree;

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.clearColor = false; // Background is already dark brown from camera
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.match = 0.5f;
            uiDocument.panelSettings = panelSettings;

            var root = uiDocument.rootVisualElement;

            refs.HealthBanner = root.Q<Label>("HealthBanner");
            refs.AvatarStateText = root.Q<Label>("AvatarStateText");
            refs.StageStatusText = root.Q<Label>("StageStatusText");
            refs.StagePlaceholderText = root.Q<Label>("StagePlaceholderText");
            
            refs.SubtitleCard = root.Q<VisualElement>("SubtitleCard");
            refs.SubtitleText = root.Q<Label>("SubtitleText");
            
            refs.TaskSummaryText = root.Q<Label>("TaskSummaryText");
            refs.TaskContentText = root.Q<Label>("TaskContentText");
            
            refs.SettingsPanel = root.Q<VisualElement>("SettingsPanel");
            refs.SettingsSummaryText = root.Q<Label>("SettingsSummaryText");
            refs.SettingsStatusText = root.Q<Label>("SettingsStatusText");
            
            refs.SpeakRepliesToggle = root.Q<Toggle>("SpeakRepliesToggle");
            refs.TranscriptPreviewToggle = root.Q<Toggle>("TranscriptPreviewToggle");
            refs.MiniAssistantToggle = root.Q<Toggle>("MiniAssistantToggle");
            refs.ReminderSpeechToggle = root.Q<Toggle>("ReminderSpeechToggle");
            
            refs.ReloadSettingsButton = root.Q<Button>("ReloadButton");
            refs.SaveSettingsButton = root.Q<Button>("SaveButton");
            
            refs.ChatLogText = root.Q<Label>("ChatLogText");
            
            refs.ReminderCard = root.Q<VisualElement>("ReminderCard");
            refs.ReminderText = root.Q<Label>("ReminderText");
            
            refs.ChatInput = root.Q<TextField>("ChatInput");
            refs.QuickAddInput = root.Q<TextField>("QuickAddInput");
            
            refs.SendButton = root.Q<Button>("SendButton");
            refs.MicButton = root.Q<Button>("MicButton");
            refs.QuickAddButton = root.Q<Button>("QuickAddButton");
            refs.RefreshButton = root.Q<Button>("RefreshButton");
            
            refs.TodayTab = root.Q<Button>("TodayTab");
            refs.WeekTab = root.Q<Button>("WeekTab");
            refs.InboxTab = root.Q<Button>("InboxButton");
            refs.CompletedTab = root.Q<Button>("DoneButton");
            refs.SettingsTab = root.Q<Button>("SettingsTab");
            
            refs.CalendarArea = root.Q<VisualElement>("CalendarArea");
            refs.TaskSheetHeaderTitle = root.Q<Label>("TaskSheetHeaderTitle");
            refs.TaskSheetMonthLabel = root.Q<Label>("TaskSheetMonthLabel");
            
            refs.HomeViewContainer = root.Q<VisualElement>("HomeViewContainer");
            refs.ScheduleViewContainer = root.Q<VisualElement>("ScheduleViewContainer");
            refs.ChatPanelView = root.Q<VisualElement>("ChatPanelView");
            refs.ScheduleSideView = root.Q<VisualElement>("ScheduleSideView");

            return refs;
        }
    }
}
