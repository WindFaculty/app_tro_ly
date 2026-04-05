using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class AppShellRefs
    {
        public Label TopStatusChipLabel;
        public Label HealthBanner;
        public Label AvatarStateText;
        public Label StageStatusText;
        public Label ScheduleInsightTitle;
        public Label ScheduleInsightSummary;
        public Label ScheduleInsightMeta;
        public Button RefreshButton;
        public Button FocusStageButton;
        public Button ToggleCalendarButton;
        public Button ToggleChatButton;
        public Button ToggleSettingsButton;
        public Button CloseSettingsButton;
        public VisualElement CalendarSheetHost;
        public VisualElement ChatPanelHost;
        public VisualElement SettingsDrawer;
        public VisualElement SettingsScrim;

        public AppShellRefs()
        {
        }

        public AppShellRefs(VisualElement root)
        {
            TopStatusChipLabel = root.Q<Label>(UiElementNames.Shell.TopStatusChipLabel);
            HealthBanner = root.Q<Label>(UiElementNames.Shell.HealthBanner);
            AvatarStateText = root.Q<Label>(UiElementNames.Shell.AvatarStateText);
            StageStatusText = root.Q<Label>(UiElementNames.Shell.StageStatusText);
            ScheduleInsightTitle = root.Q<Label>(UiElementNames.Shell.ScheduleInsightTitle);
            ScheduleInsightSummary = root.Q<Label>(UiElementNames.Shell.ScheduleInsightSummary);
            ScheduleInsightMeta = root.Q<Label>(UiElementNames.Shell.ScheduleInsightMeta);
            RefreshButton = root.Q<Button>(UiElementNames.Shell.RefreshButton);
            FocusStageButton = root.Q<Button>(UiElementNames.Shell.FocusStageButton);
            ToggleCalendarButton = root.Q<Button>(UiElementNames.Shell.ToggleCalendarButton);
            ToggleChatButton = root.Q<Button>(UiElementNames.Shell.ToggleChatButton);
            ToggleSettingsButton = root.Q<Button>(UiElementNames.Shell.ToggleSettingsButton);
            CloseSettingsButton = root.Q<Button>(UiElementNames.Shell.CloseSettingsButton);
            CalendarSheetHost = root.Q<VisualElement>(UiElementNames.Shell.CalendarSheetHost);
            ChatPanelHost = root.Q<VisualElement>(UiElementNames.Shell.ChatPanelHost);
            SettingsDrawer = root.Q<VisualElement>(UiElementNames.Shell.SettingsDrawer);
            SettingsScrim = root.Q<VisualElement>(UiElementNames.Shell.SettingsScrim);
        }
    }
}
