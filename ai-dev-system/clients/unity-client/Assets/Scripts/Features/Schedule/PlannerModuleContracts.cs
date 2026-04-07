using System;
using LocalAssistant.App;
using LocalAssistant.Core;

namespace LocalAssistant.Features.Schedule
{
    public interface IPlannerModule
    {
        event Action TodayRequested;
        event Action<int> DateOffsetRequested;
        event Action<string> DaySelected;
        event Action<AppScreen> ScreenRequested;
        event Action<string> CompleteTaskRequested;
        event Action<string, string> ScheduleTaskRequested;

        AppScreen CurrentScreen { get; }

        void Bind();
        void ShowScreen(AppScreen screen);
        void Render(IPlannerTaskSnapshotSource taskStore, string selectedDate);
        void SetTaskActionsEnabled(bool isEnabled);
        void RequestTodayView();
        void RequestWeekView();
        void RequestToday();
        void RequestDateOffset(int offset);
        void RequestDaySelected(string date);
        void RequestInbox();
        void RequestCompleted();
        void RequestCompleteTask(string taskId);
        void RequestScheduleTask(string taskId, string selectedDate);
    }
}
