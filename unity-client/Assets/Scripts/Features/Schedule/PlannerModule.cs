using System;
using LocalAssistant.App;
using LocalAssistant.Core;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Schedule
{
    public sealed class PlannerModule : IPlannerModule
    {
        private readonly ScheduleScreenRefs refs;
        private readonly ScheduleScreenController screenController;
        private bool isBound;

        public PlannerModule(ScheduleScreenRefs refs)
            : this(refs, new ScheduleScreenController(refs))
        {
        }

        public PlannerModule(ScheduleScreenRefs refs, ScheduleScreenController screenController)
        {
            this.refs = refs ?? throw new ArgumentNullException(nameof(refs));
            this.screenController = screenController ?? throw new ArgumentNullException(nameof(screenController));
        }

        public event Action TodayRequested;
        public event Action<int> DateOffsetRequested;
        public event Action<string> DaySelected;
        public event Action<AppScreen> ScreenRequested;
        public event Action<string> CompleteTaskRequested;
        public event Action<string, string> ScheduleTaskRequested;

        public AppScreen CurrentScreen { get; private set; } = AppScreen.Week;

        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            isBound = true;
            screenController.Bind();
            screenController.TodayViewRequested += HandleTodayViewRequested;
            screenController.WeekViewRequested += HandleWeekViewRequested;
            screenController.TodayRequested += HandleTodayRequested;
            screenController.DateOffsetRequested += HandleDateOffsetRequested;
            screenController.DaySelected += HandleDaySelected;
            screenController.InboxRequested += HandleInboxRequested;
            screenController.CompletedRequested += HandleCompletedRequested;
            screenController.CompleteTaskRequested += HandleCompleteTaskRequested;
            screenController.ScheduleTaskRequested += HandleScheduleTaskRequested;
        }

        public void ShowScreen(AppScreen screen)
        {
            CurrentScreen = screen;
            SetTabButtonVisual(refs.TodayViewButton, screen == AppScreen.Today);
            SetTabButtonVisual(refs.WeekViewButton, screen == AppScreen.Week);
            SetTabButtonVisual(refs.InboxTab, screen == AppScreen.Inbox);
            SetTabButtonVisual(refs.CompletedTab, screen == AppScreen.Completed);
        }

        public void Render(IPlannerTaskSnapshotSource taskStore, string selectedDate)
        {
            screenController.Render(taskStore, CurrentScreen, selectedDate);
        }

        public void SetTaskActionsEnabled(bool isEnabled) => screenController.SetTaskActionsEnabled(isEnabled);
        public void RequestTodayView() => screenController.RequestTodayView();
        public void RequestWeekView() => screenController.RequestWeekView();
        public void RequestToday() => screenController.RequestToday();
        public void RequestDateOffset(int offset) => screenController.RequestDateOffset(offset);
        public void RequestDaySelected(string date) => screenController.RequestDaySelected(date);
        public void RequestInbox() => screenController.RequestInbox();
        public void RequestCompleted() => screenController.RequestCompleted();
        public void RequestCompleteTask(string taskId) => screenController.RequestCompleteTask(taskId);
        public void RequestScheduleTask(string taskId, string selectedDate) => screenController.RequestScheduleTask(taskId, selectedDate);

        private void HandleTodayViewRequested() => ScreenRequested?.Invoke(AppScreen.Today);
        private void HandleWeekViewRequested() => ScreenRequested?.Invoke(AppScreen.Week);
        private void HandleTodayRequested() => TodayRequested?.Invoke();
        private void HandleDateOffsetRequested(int offset) => DateOffsetRequested?.Invoke(offset);
        private void HandleDaySelected(string selectedDate) => DaySelected?.Invoke(selectedDate);
        private void HandleInboxRequested() => ScreenRequested?.Invoke(AppScreen.Inbox);
        private void HandleCompletedRequested() => ScreenRequested?.Invoke(AppScreen.Completed);
        private void HandleCompleteTaskRequested(string taskId) => CompleteTaskRequested?.Invoke(taskId);
        private void HandleScheduleTaskRequested(string taskId, string selectedDate) => ScheduleTaskRequested?.Invoke(taskId, selectedDate);

        private static void SetTabButtonVisual(Button button, bool isActive)
        {
            if (button == null)
            {
                return;
            }

            if (isActive)
            {
                button.AddToClassList("active");
            }
            else
            {
                button.RemoveFromClassList("active");
            }
        }
    }
}
