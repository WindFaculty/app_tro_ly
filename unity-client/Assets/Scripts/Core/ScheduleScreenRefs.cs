using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class ScheduleScreenRefs
    {
        public Button ScheduleTodayButton;
        public Button SchedulePrevButton;
        public Button ScheduleNextButton;
        public Button InboxTab;
        public Button CompletedTab;
        public VisualElement CalendarArea;
        public Label TaskSheetHeaderTitle;
        public Label TaskSheetMonthLabel;
        public Label ScheduleSummaryText;
        public Label ScheduleMetaText;
        public Label ScheduleEmptyStateText;
        public VisualElement ScheduleListContainer;

        public ScheduleScreenRefs()
        {
        }

        public ScheduleScreenRefs(VisualElement root)
        {
            ScheduleTodayButton = root.Q<Button>(UiElementNames.Schedule.ScheduleTodayButton);
            SchedulePrevButton = root.Q<Button>(UiElementNames.Schedule.SchedulePrevButton);
            ScheduleNextButton = root.Q<Button>(UiElementNames.Schedule.ScheduleNextButton);
            InboxTab = root.Q<Button>(UiElementNames.Schedule.InboxButton);
            CompletedTab = root.Q<Button>(UiElementNames.Schedule.DoneButton);
            CalendarArea = root.Q<VisualElement>(UiElementNames.Schedule.CalendarArea);
            TaskSheetHeaderTitle = root.Q<Label>(UiElementNames.Schedule.TaskSheetHeaderTitle);
            TaskSheetMonthLabel = root.Q<Label>(UiElementNames.Schedule.TaskSheetMonthLabel);
            ScheduleSummaryText = root.Q<Label>(UiElementNames.Schedule.ScheduleSummaryText);
            ScheduleMetaText = root.Q<Label>(UiElementNames.Schedule.ScheduleMetaText);
            ScheduleEmptyStateText = root.Q<Label>(UiElementNames.Schedule.ScheduleEmptyStateText);
            ScheduleListContainer = root.Q<VisualElement>(UiElementNames.Schedule.ScheduleListContainer);
        }
    }
}
