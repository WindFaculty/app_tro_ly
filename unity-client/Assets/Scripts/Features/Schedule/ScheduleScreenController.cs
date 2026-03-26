using System;
using LocalAssistant.App;
using LocalAssistant.Core;
using LocalAssistant.Tasks;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Schedule
{
    public sealed class ScheduleScreenController
    {
        private readonly AssistantUiRefs ui;

        public ScheduleScreenController(AssistantUiRefs ui)
        {
            this.ui = ui;
        }

        public void Render(TaskViewModelStore taskStore, AppScreen currentScreen, string selectedDate)
        {
            if (ui.TaskSheetHeaderTitle != null)
            {
                ui.TaskSheetHeaderTitle.text = currentScreen switch
                {
                    AppScreen.Inbox => "Inbox AI",
                    AppScreen.Completed => "Hoan tat AI",
                    _ => "Lich trinh AI",
                };
            }

            if (ui.TaskSheetMonthLabel != null)
            {
                if (DateTime.TryParse(selectedDate, out var parsed))
                {
                    ui.TaskSheetMonthLabel.text = parsed.ToString("MMMM yyyy");
                }
            }

            var placeholder = ui.CalendarArea?.Q<Label>();
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
