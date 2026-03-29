using System;
using System.Collections.Generic;
using LocalAssistant.App;
using LocalAssistant.Core;
using LocalAssistant.Tasks;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Schedule
{
    public sealed class ScheduleScreenController
    {
        private readonly ScheduleScreenRefs schedule;
        private bool taskActionsEnabled = true;

        public ScheduleScreenController(ScheduleScreenRefs schedule)
        {
            this.schedule = schedule;
        }

        public event Action TodayRequested;
        public event Action<int> DateOffsetRequested;
        public event Action<string> DaySelected;
        public event Action InboxRequested;
        public event Action CompletedRequested;
        public event Action<string> CompleteTaskRequested;
        public event Action<string, string> ScheduleTaskRequested;

        public void Bind()
        {
            UiButtonActionBinder.Bind(schedule.ScheduleTodayButton, RequestToday);
            UiButtonActionBinder.Bind(schedule.SchedulePrevButton, () => RequestDateOffset(-1));
            UiButtonActionBinder.Bind(schedule.ScheduleNextButton, () => RequestDateOffset(1));
            UiButtonActionBinder.Bind(schedule.InboxTab, RequestInbox);
            UiButtonActionBinder.Bind(schedule.CompletedTab, RequestCompleted);
        }

        public void Render(TaskViewModelStore taskStore, AppScreen currentScreen, string selectedDate)
        {
            ApplyButtonState(schedule.ScheduleTodayButton, taskActionsEnabled);
            ApplyButtonState(schedule.SchedulePrevButton, taskActionsEnabled);
            ApplyButtonState(schedule.ScheduleNextButton, taskActionsEnabled);
            ApplyButtonState(schedule.InboxTab, taskActionsEnabled);
            ApplyButtonState(schedule.CompletedTab, taskActionsEnabled);

            if (schedule.TaskSheetHeaderTitle != null)
            {
                schedule.TaskSheetHeaderTitle.text = currentScreen switch
                {
                    AppScreen.Inbox => "Inbox triage",
                    AppScreen.Completed => "Completed review",
                    _ => "Week schedule",
                };
            }

            if (schedule.TaskSheetMonthLabel != null)
            {
                schedule.TaskSheetMonthLabel.text = BuildMonthLabel(taskStore, currentScreen, selectedDate);
            }

            if (schedule.ScheduleSummaryText != null)
            {
                schedule.ScheduleSummaryText.text = BuildSummary(taskStore, currentScreen);
            }

            if (schedule.ScheduleMetaText != null)
            {
                schedule.ScheduleMetaText.text = BuildMeta(taskStore, currentScreen, selectedDate);
            }

            var hasItems = RenderItems(taskStore, currentScreen, selectedDate);
            if (schedule.ScheduleEmptyStateText != null)
            {
                schedule.ScheduleEmptyStateText.text = BuildEmptyState(currentScreen);
                schedule.ScheduleEmptyStateText.style.display = hasItems ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public void SetTaskActionsEnabled(bool isEnabled)
        {
            taskActionsEnabled = isEnabled;
            ApplyButtonState(schedule.ScheduleTodayButton, isEnabled);
            ApplyButtonState(schedule.SchedulePrevButton, isEnabled);
            ApplyButtonState(schedule.ScheduleNextButton, isEnabled);
            ApplyButtonState(schedule.InboxTab, isEnabled);
            ApplyButtonState(schedule.CompletedTab, isEnabled);
        }

        public void RequestToday() => TodayRequested?.Invoke();
        public void RequestDateOffset(int offset) => DateOffsetRequested?.Invoke(offset);
        public void RequestDaySelected(string date) => DaySelected?.Invoke(date);
        public void RequestInbox()
        {
            if (taskActionsEnabled)
            {
                InboxRequested?.Invoke();
            }
        }

        public void RequestCompleted()
        {
            if (taskActionsEnabled)
            {
                CompletedRequested?.Invoke();
            }
        }

        public void RequestCompleteTask(string taskId) => CompleteTaskRequested?.Invoke(taskId);
        public void RequestScheduleTask(string taskId, string selectedDate) => ScheduleTaskRequested?.Invoke(taskId, selectedDate);

        private bool RenderItems(TaskViewModelStore taskStore, AppScreen currentScreen, string selectedDate)
        {
            if (schedule.ScheduleListContainer == null)
            {
                return false;
            }

            schedule.ScheduleListContainer.Clear();

            return currentScreen switch
            {
                AppScreen.Inbox => RenderTaskList(taskStore.Inbox.items, "Inbox item", currentScreen, selectedDate),
                AppScreen.Completed => RenderTaskList(taskStore.Completed.items, "Completed task", currentScreen, selectedDate),
                _ => RenderWeek(taskStore.Week, currentScreen, selectedDate),
            };
        }

        private bool RenderWeek(WeekTasksResponse week, AppScreen currentScreen, string selectedDate)
        {
            var hasItems = false;
            if (week?.days == null || week.days.Count == 0)
            {
                return false;
            }

            foreach (var bucket in week.days)
            {
                if (bucket == null)
                {
                    continue;
                }

                if (bucket.items != null && bucket.items.Count > 0)
                {
                    hasItems = true;
                }

                schedule.ScheduleListContainer.Add(CreateDayCard(bucket, currentScreen, selectedDate));
            }

            return hasItems;
        }

        private bool RenderTaskList(List<TaskRecord> items, string cardTitle, AppScreen currentScreen, string selectedDate)
        {
            if (items == null || items.Count == 0)
            {
                return false;
            }

            foreach (var task in items)
            {
                var card = new VisualElement();
                card.AddToClassList("shell-card");
                card.AddToClassList("schedule-day-card");
                card.Add(CreateLabel(cardTitle, "schedule-day-meta"));
                card.Add(CreateTaskRow(task, currentScreen, selectedDate));
                schedule.ScheduleListContainer.Add(card);
            }

            return true;
        }

        private VisualElement CreateDayCard(WeekDayBucket bucket, AppScreen currentScreen, string selectedDate)
        {
            var card = new VisualElement();
            card.AddToClassList("shell-card");
            card.AddToClassList("schedule-day-card");
            if (string.Equals(bucket.date, selectedDate, StringComparison.Ordinal))
            {
                card.AddToClassList("schedule-day-card-selected");
            }

            var header = new VisualElement();
            header.AddToClassList("schedule-day-header");

            var selectButton = new Button(() => RequestDaySelected(bucket.date))
            {
                text = FormatDayTitle(bucket.date),
            };
            selectButton.AddToClassList("schedule-day-select-btn");
            selectButton.AddToClassList("schedule-day-title");
            selectButton.SetEnabled(taskActionsEnabled);
            header.Add(selectButton);
            header.Add(CreateLabel($"{CountOf(bucket.items)} tasks", "schedule-day-count"));
            card.Add(header);

            if (bucket.high_priority_count > 0)
            {
                card.Add(CreateLabel($"High priority: {bucket.high_priority_count}", "schedule-day-meta"));
            }

            if (bucket.items == null || bucket.items.Count == 0)
            {
                card.Add(CreateLabel("No scheduled work for this day.", "schedule-task-empty"));
                return card;
            }

            var limit = Math.Min(bucket.items.Count, 4);
            for (var index = 0; index < limit; index++)
            {
                card.Add(CreateTaskRow(bucket.items[index], currentScreen, selectedDate));
            }

            if (bucket.items.Count > limit)
            {
                card.Add(CreateLabel($"+{bucket.items.Count - limit} more items", "schedule-task-more"));
            }

            return card;
        }

        private VisualElement CreateTaskRow(TaskRecord task, AppScreen currentScreen, string selectedDate)
        {
            var row = new VisualElement();
            row.AddToClassList("schedule-task-row");

            var layout = new VisualElement();
            layout.AddToClassList("schedule-task-layout");

            var content = new VisualElement();
            content.AddToClassList("schedule-task-content");
            content.Add(CreateLabel(task.title, "schedule-task-title"));
            content.Add(CreateLabel(BuildTaskMeta(task), "schedule-task-meta"));
            layout.Add(content);

            var actions = CreateTaskActions(task, currentScreen, selectedDate);
            if (actions != null)
            {
                layout.Add(actions);
            }

            row.Add(layout);
            return row;
        }

        private VisualElement CreateTaskActions(TaskRecord task, AppScreen currentScreen, string selectedDate)
        {
            if (currentScreen == AppScreen.Completed)
            {
                return null;
            }

            var actions = new VisualElement();
            actions.AddToClassList("schedule-task-actions");

            if (currentScreen == AppScreen.Inbox)
            {
                var scheduleButton = new Button(() => RequestScheduleTask(task.id, selectedDate))
                {
                    text = $"Schedule to {FormatActionDate(selectedDate)}",
                };
                scheduleButton.AddToClassList("btn-secondary");
                scheduleButton.AddToClassList("schedule-task-action-btn");
                scheduleButton.SetEnabled(taskActionsEnabled && !string.IsNullOrWhiteSpace(selectedDate));
                actions.Add(scheduleButton);
                return actions;
            }

            var completeButton = new Button(() => RequestCompleteTask(task.id))
            {
                text = "Complete",
            };
            completeButton.AddToClassList("btn-secondary");
            completeButton.AddToClassList("schedule-task-action-btn");
            completeButton.SetEnabled(taskActionsEnabled && !string.Equals(task.status, "done", StringComparison.OrdinalIgnoreCase));
            actions.Add(completeButton);
            return actions;
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static string BuildSummary(TaskViewModelStore taskStore, AppScreen currentScreen)
        {
            return currentScreen switch
            {
                AppScreen.Inbox => $"{CountOf(taskStore.Inbox.items)} inbox items are waiting for scheduling.",
                AppScreen.Completed => $"{CountOf(taskStore.Completed.items)} completed tasks are ready for review.",
                _ => $"{CountWeekTasks(taskStore.Week)} tasks across {CountDaysWithItems(taskStore.Week)} active days. Conflicts: {CountOf(taskStore.Week.conflicts)}.",
            };
        }

        private static string BuildMeta(TaskViewModelStore taskStore, AppScreen currentScreen, string selectedDate)
        {
            var displayDate = string.IsNullOrWhiteSpace(selectedDate) ? "auto" : selectedDate;
            return currentScreen switch
            {
                AppScreen.Inbox => $"Selected date {displayDate}. Inbox items can be scheduled directly to the selected day.",
                AppScreen.Completed => $"Selected date {displayDate}. Review completed tasks for follow-up or missed documentation.",
                _ => $"Selected date {displayDate}. Overdue this week: {taskStore.Week.overdue_count}. Select a day card to move the shell context.",
            };
        }

        private static string BuildEmptyState(AppScreen currentScreen)
        {
            return currentScreen switch
            {
                AppScreen.Inbox => "Inbox is clear. New quick captures will show here before they are scheduled.",
                AppScreen.Completed => "No completed tasks yet. Finished work will show here for review.",
                _ => "No tasks are scheduled for this week yet. Add something from Home or chat to populate the calendar.",
            };
        }

        private static string BuildMonthLabel(TaskViewModelStore taskStore, AppScreen currentScreen, string selectedDate)
        {
            if (DateTime.TryParse(selectedDate, out var parsed))
            {
                return parsed.ToString("MMMM yyyy");
            }

            if (currentScreen == AppScreen.Week && DateTime.TryParse(taskStore.Week.start_date, out parsed))
            {
                return parsed.ToString("MMMM yyyy");
            }

            return "Auto date";
        }

        private static string FormatDayTitle(string value)
        {
            return DateTime.TryParse(value, out var parsed)
                ? parsed.ToString("ddd dd MMM")
                : value;
        }

        private static string BuildTaskMeta(TaskRecord task)
        {
            var timeBlock = string.IsNullOrWhiteSpace(task.start_at) ? "No start time" : $"Starts {task.start_at}";
            var dueBlock = string.IsNullOrWhiteSpace(task.due_at) ? "No due time" : $"Due {task.due_at}";
            var category = string.IsNullOrWhiteSpace(task.category) ? "general" : task.category;
            return $"Priority {task.priority} | {timeBlock} | {dueBlock} | {category}";
        }

        private static string FormatActionDate(string selectedDate)
        {
            return DateTime.TryParse(selectedDate, out var parsed)
                ? parsed.ToString("dd MMM")
                : "selected day";
        }

        private static int CountWeekTasks(WeekTasksResponse response)
        {
            if (response?.days == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var bucket in response.days)
            {
                total += CountOf(bucket?.items);
            }

            return total;
        }

        private static int CountDaysWithItems(WeekTasksResponse response)
        {
            if (response?.days == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var bucket in response.days)
            {
                if (CountOf(bucket?.items) > 0)
                {
                    total++;
                }
            }

            return total;
        }

        private static int CountOf<T>(List<T> items)
        {
            return items?.Count ?? 0;
        }

        private static void ApplyButtonState(Button button, bool isEnabled)
        {
            button?.SetEnabled(isEnabled);
        }
    }
}
