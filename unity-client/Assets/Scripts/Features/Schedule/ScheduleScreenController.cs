using System;
using LocalAssistant.App;
using LocalAssistant.Core;
using LocalAssistant.Tasks;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Schedule
{
    public sealed class ScheduleScreenController
    {
        private readonly ScheduleScreenRefs schedule;

        public ScheduleScreenController(ScheduleScreenRefs schedule)
        {
            this.schedule = schedule;
        }

        public void Render(TaskViewModelStore taskStore, AppScreen currentScreen, string selectedDate)
        {
            if (schedule.TaskSheetHeaderTitle != null)
            {
                schedule.TaskSheetHeaderTitle.text = currentScreen switch
                {
                    AppScreen.Inbox => "Inbox AI",
                    AppScreen.Completed => "Hoan tat AI",
                    _ => "Lich trinh AI",
                };
            }

            if (schedule.TaskSheetMonthLabel != null)
            {
                if (DateTime.TryParse(selectedDate, out var parsed))
                {
                    schedule.TaskSheetMonthLabel.text = parsed.ToString("MMMM yyyy");
                }
            }

            var placeholder = schedule.CalendarArea?.Q<Label>();
            if (placeholder != null)
            {
                placeholder.text = taskStore.BuildTabText(ToTaskTabName(currentScreen));
            }
        }

        private static string ToTaskTabName(AppScreen screen) => screen switch
        {
            AppScreen.Inbox => "Inbox",
            AppScreen.Completed => "Completed",
            _ => "Week",
        };
    }
}
