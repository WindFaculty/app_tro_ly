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
        public Button TodayTab;
        public Button WeekTab;
        public Button SettingsTab;
        public VisualElement HomeViewContainer;
        public VisualElement ScheduleViewContainer;
        public VisualElement ScheduleSideView;

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
            TodayTab = root.Q<Button>(UiElementNames.Shell.TodayTab);
            WeekTab = root.Q<Button>(UiElementNames.Shell.WeekTab);
            SettingsTab = root.Q<Button>(UiElementNames.Shell.SettingsTab);
            HomeViewContainer = root.Q<VisualElement>(UiElementNames.Shell.HomeViewContainer);
            ScheduleViewContainer = root.Q<VisualElement>(UiElementNames.Shell.ScheduleViewContainer);
            ScheduleSideView = root.Q<VisualElement>(UiElementNames.Shell.ScheduleSideView);
        }
    }
}
