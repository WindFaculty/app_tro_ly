using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class ScheduleScreenRefs
    {
        public Button InboxTab;
        public Button CompletedTab;
        public VisualElement CalendarArea;
        public Label TaskSheetHeaderTitle;
        public Label TaskSheetMonthLabel;

        public ScheduleScreenRefs()
        {
        }

        public ScheduleScreenRefs(VisualElement root)
        {
            InboxTab = root.Q<Button>(UiElementNames.Schedule.InboxButton);
            CompletedTab = root.Q<Button>(UiElementNames.Schedule.DoneButton);
            CalendarArea = root.Q<VisualElement>(UiElementNames.Schedule.CalendarArea);
            TaskSheetHeaderTitle = root.Q<Label>(UiElementNames.Schedule.TaskSheetHeaderTitle);
            TaskSheetMonthLabel = root.Q<Label>(UiElementNames.Schedule.TaskSheetMonthLabel);
        }
    }
}
