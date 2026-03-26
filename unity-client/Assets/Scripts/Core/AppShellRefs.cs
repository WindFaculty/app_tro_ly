using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class AppShellRefs
    {
        public Label HealthBanner;
        public Label AvatarStateText;
        public Label StageStatusText;
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
            HealthBanner = root.Q<Label>(UiElementNames.Shell.HealthBanner);
            AvatarStateText = root.Q<Label>(UiElementNames.Shell.AvatarStateText);
            StageStatusText = root.Q<Label>(UiElementNames.Shell.StageStatusText);
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
