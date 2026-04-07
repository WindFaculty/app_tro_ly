using UnityEngine.UIElements;

namespace LocalAssistant.Features.Schedule
{
    public sealed class ScheduleScreenRefs
    {
        public Button ScheduleTodayButton;
        public Button SchedulePrevButton;
        public Button ScheduleNextButton;
        public Button TodayViewButton;
        public Button WeekViewButton;
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
            ScheduleTodayButton = root.Q<Button>(Core.UiElementNames.Schedule.ScheduleTodayButton);
            SchedulePrevButton = root.Q<Button>(Core.UiElementNames.Schedule.SchedulePrevButton);
            ScheduleNextButton = root.Q<Button>(Core.UiElementNames.Schedule.ScheduleNextButton);
            TodayViewButton = root.Q<Button>(Core.UiElementNames.Schedule.TodayViewButton);
            WeekViewButton = root.Q<Button>(Core.UiElementNames.Schedule.WeekViewButton);
            InboxTab = root.Q<Button>(Core.UiElementNames.Schedule.InboxButton);
            CompletedTab = root.Q<Button>(Core.UiElementNames.Schedule.DoneButton);
            CalendarArea = root.Q<VisualElement>(Core.UiElementNames.Schedule.CalendarArea);
            TaskSheetHeaderTitle = root.Q<Label>(Core.UiElementNames.Schedule.TaskSheetHeaderTitle);
            TaskSheetMonthLabel = root.Q<Label>(Core.UiElementNames.Schedule.TaskSheetMonthLabel);
            ScheduleSummaryText = root.Q<Label>(Core.UiElementNames.Schedule.ScheduleSummaryText);
            ScheduleMetaText = root.Q<Label>(Core.UiElementNames.Schedule.ScheduleMetaText);
            ScheduleEmptyStateText = root.Q<Label>(Core.UiElementNames.Schedule.ScheduleEmptyStateText);
            ScheduleListContainer = root.Q<VisualElement>(Core.UiElementNames.Schedule.ScheduleListContainer);
        }
    }
}
