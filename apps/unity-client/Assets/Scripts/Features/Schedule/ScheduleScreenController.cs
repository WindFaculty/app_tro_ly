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
        public event Action TodayViewRequested;
        public event Action WeekViewRequested;
        public event Action InboxRequested;
        public event Action CompletedRequested;
        public event Action<string> CompleteTaskRequested;
        public event Action<string, string> ScheduleTaskRequested;

        public void Bind()
        {
            UiButtonActionBinder.Bind(schedule.TodayViewButton, RequestTodayView);
            UiButtonActionBinder.Bind(schedule.WeekViewButton, RequestWeekView);
            UiButtonActionBinder.Bind(schedule.ScheduleTodayButton, RequestToday);
            UiButtonActionBinder.Bind(schedule.SchedulePrevButton, () => RequestDateOffset(-1));
            UiButtonActionBinder.Bind(schedule.ScheduleNextButton, () => RequestDateOffset(1));
            UiButtonActionBinder.Bind(schedule.InboxTab, RequestInbox);
            UiButtonActionBinder.Bind(schedule.CompletedTab, RequestCompleted);
        }

        public void Render(IPlannerTaskSnapshotSource taskStore, AppScreen currentScreen, string selectedDate)
        {
            ApplyButtonState(schedule.ScheduleTodayButton, taskActionsEnabled);
            ApplyButtonState(schedule.SchedulePrevButton, taskActionsEnabled);
            ApplyButtonState(schedule.ScheduleNextButton, taskActionsEnabled);
            ApplyButtonState(schedule.TodayViewButton, taskActionsEnabled);
            ApplyButtonState(schedule.WeekViewButton, taskActionsEnabled);
            ApplyButtonState(schedule.InboxTab, taskActionsEnabled);
            ApplyButtonState(schedule.CompletedTab, taskActionsEnabled);

            if (schedule.TaskSheetHeaderTitle != null)
            {
                schedule.TaskSheetHeaderTitle.text = currentScreen switch
                {
                    AppScreen.Today => "Today focus",
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
            ApplyButtonState(schedule.TodayViewButton, isEnabled);
            ApplyButtonState(schedule.WeekViewButton, isEnabled);
            ApplyButtonState(schedule.InboxTab, isEnabled);
            ApplyButtonState(schedule.CompletedTab, isEnabled);
        }

        public void RequestTodayView()
        {
            if (taskActionsEnabled)
            {
                TodayViewRequested?.Invoke();
            }
        }

        public void RequestWeekView()
        {
            if (taskActionsEnabled)
            {
                WeekViewRequested?.Invoke();
            }
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

        private bool RenderItems(IPlannerTaskSnapshotSource taskStore, AppScreen currentScreen, string selectedDate)
        {
            if (schedule.ScheduleListContainer == null)
            {
                return false;
            }

            schedule.ScheduleListContainer.Clear();

            return currentScreen switch
            {
                AppScreen.Today => RenderTaskList(taskStore.Today.Items, "Today task", currentScreen, selectedDate),
                AppScreen.Inbox => RenderTaskList(taskStore.Inbox.Items, "Inbox item", currentScreen, selectedDate),
                AppScreen.Completed => RenderTaskList(taskStore.Completed.Items, "Completed task", currentScreen, selectedDate),
                _ => RenderWeek(taskStore.Week, currentScreen, selectedDate),
            };
        }

        private bool RenderWeek(PlannerWeekSnapshot week, AppScreen currentScreen, string selectedDate)
        {
            var hasItems = false;
            if (week?.Days == null || week.Days.Count == 0)
            {
                return false;
            }

            foreach (var bucket in week.Days)
            {
                if (bucket == null)
                {
                    continue;
                }

                if (bucket.Items != null && bucket.Items.Count > 0)
                {
                    hasItems = true;
                }

                schedule.ScheduleListContainer.Add(CreateDayCard(bucket, currentScreen, selectedDate));
            }

            return hasItems;
        }

        private bool RenderTaskList(List<PlannerTaskItem> items, string cardTitle, AppScreen currentScreen, string selectedDate)
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

        private VisualElement CreateDayCard(PlannerDayBucketSnapshot bucket, AppScreen currentScreen, string selectedDate)
        {
            var card = new VisualElement();
            card.AddToClassList("shell-card");
            card.AddToClassList("schedule-day-card");
            if (string.Equals(bucket.Date, selectedDate, StringComparison.Ordinal))
            {
                card.AddToClassList("schedule-day-card-selected");
            }

            var header = new VisualElement();
            header.AddToClassList("schedule-day-header");

            var selectButton = new Button(() => RequestDaySelected(bucket.Date))
            {
                text = FormatDayTitle(bucket.Date),
            };
            selectButton.AddToClassList("schedule-day-select-btn");
            selectButton.AddToClassList("schedule-day-title");
            selectButton.SetEnabled(taskActionsEnabled);
            header.Add(selectButton);
            header.Add(CreateLabel($"{CountOf(bucket.Items)} tasks", "schedule-day-count"));
            card.Add(header);

            if (bucket.HighPriorityCount > 0)
            {
                card.Add(CreateLabel($"High priority: {bucket.HighPriorityCount}", "schedule-day-meta"));
            }

            if (bucket.Items == null || bucket.Items.Count == 0)
            {
                card.Add(CreateLabel("No scheduled work for this day.", "schedule-task-empty"));
                return card;
            }

            var limit = Math.Min(bucket.Items.Count, 4);
            for (var index = 0; index < limit; index++)
            {
                card.Add(CreateTaskRow(bucket.Items[index], currentScreen, selectedDate));
            }

            if (bucket.Items.Count > limit)
            {
                card.Add(CreateLabel($"+{bucket.Items.Count - limit} more items", "schedule-task-more"));
            }

            return card;
        }

        private VisualElement CreateTaskRow(PlannerTaskItem task, AppScreen currentScreen, string selectedDate)
        {
            var row = new VisualElement();
            row.AddToClassList("schedule-task-row");

            var layout = new VisualElement();
            layout.AddToClassList("schedule-task-layout");

            var content = new VisualElement();
            content.AddToClassList("schedule-task-content");
            content.Add(CreateLabel(task.Title, "schedule-task-title"));
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

        private VisualElement CreateTaskActions(PlannerTaskItem task, AppScreen currentScreen, string selectedDate)
        {
            if (currentScreen == AppScreen.Completed)
            {
                return null;
            }

            var actions = new VisualElement();
            actions.AddToClassList("schedule-task-actions");

            if (currentScreen == AppScreen.Inbox)
            {
                var scheduleButton = new Button(() => RequestScheduleTask(task.Id, selectedDate))
                {
                    text = $"Schedule to {FormatActionDate(selectedDate)}",
                };
                scheduleButton.AddToClassList("btn-secondary");
                scheduleButton.AddToClassList("schedule-task-action-btn");
                scheduleButton.SetEnabled(taskActionsEnabled && !string.IsNullOrWhiteSpace(selectedDate));
                actions.Add(scheduleButton);
                return actions;
            }

            var completeButton = new Button(() => RequestCompleteTask(task.Id))
            {
                text = "Complete",
            };
            completeButton.AddToClassList("btn-secondary");
            completeButton.AddToClassList("schedule-task-action-btn");
            completeButton.SetEnabled(taskActionsEnabled && !string.Equals(task.Status, "done", StringComparison.OrdinalIgnoreCase));
            actions.Add(completeButton);
            return actions;
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static string BuildSummary(IPlannerTaskSnapshotSource taskStore, AppScreen currentScreen)
        {
            return currentScreen switch
            {
                AppScreen.Today => $"{CountOf(taskStore.Today.Items)} today tasks, {CountOf(taskStore.Today.DueSoon)} due soon, and {CountOf(taskStore.Today.Overdue)} overdue.",
                AppScreen.Inbox => $"{CountOf(taskStore.Inbox.Items)} inbox items are waiting for scheduling.",
                AppScreen.Completed => $"{CountOf(taskStore.Completed.Items)} completed tasks are ready for review.",
                _ => $"{CountWeekTasks(taskStore.Week)} tasks across {CountDaysWithItems(taskStore.Week)} active days. Conflicts: {CountOf(taskStore.Week.Conflicts)}.",
            };
        }

        private static string BuildMeta(IPlannerTaskSnapshotSource taskStore, AppScreen currentScreen, string selectedDate)
        {
            var displayDate = string.IsNullOrWhiteSpace(selectedDate) ? "auto" : selectedDate;
            return currentScreen switch
            {
                AppScreen.Today => $"Selected date {displayDate}. Use Today focus to work the selected day without leaving the avatar stage.",
                AppScreen.Inbox => $"Selected date {displayDate}. Inbox items can be scheduled directly to the selected day.",
                AppScreen.Completed => $"Selected date {displayDate}. Review completed tasks for follow-up or missed documentation.",
                _ => $"Selected date {displayDate}. Overdue this week: {taskStore.Week.OverdueCount}. Select a day card to move the shell context.",
            };
        }

        private static string BuildEmptyState(AppScreen currentScreen)
        {
            return currentScreen switch
            {
                AppScreen.Today => "No tasks are scheduled for the selected day yet. Add work from the stage quick-add or ask chat to capture something.",
                AppScreen.Inbox => "Inbox is clear. New quick captures will show here before they are scheduled.",
                AppScreen.Completed => "No completed tasks yet. Finished work will show here for review.",
                _ => "No tasks are scheduled for this week yet. Add something from Home or chat to populate the calendar.",
            };
        }

        private static string BuildMonthLabel(IPlannerTaskSnapshotSource taskStore, AppScreen currentScreen, string selectedDate)
        {
            if (DateTime.TryParse(selectedDate, out var parsed))
            {
                return parsed.ToString("MMMM yyyy");
            }

            if (currentScreen == AppScreen.Week && DateTime.TryParse(taskStore.Week.StartDate, out parsed))
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

        private static string BuildTaskMeta(PlannerTaskItem task)
        {
            var timeBlock = string.IsNullOrWhiteSpace(task.StartAt) ? "No start time" : $"Starts {task.StartAt}";
            var dueBlock = string.IsNullOrWhiteSpace(task.DueAt) ? "No due time" : $"Due {task.DueAt}";
            var category = string.IsNullOrWhiteSpace(task.Category) ? "general" : task.Category;
            return $"Priority {task.Priority} | {timeBlock} | {dueBlock} | {category}";
        }

        private static string FormatActionDate(string selectedDate)
        {
            return DateTime.TryParse(selectedDate, out var parsed)
                ? parsed.ToString("dd MMM")
                : "selected day";
        }

        private static int CountWeekTasks(PlannerWeekSnapshot response)
        {
            if (response?.Days == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var bucket in response.Days)
            {
                total += CountOf(bucket?.Items);
            }

            return total;
        }

        private static int CountDaysWithItems(PlannerWeekSnapshot response)
        {
            if (response?.Days == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var bucket in response.Days)
            {
                if (CountOf(bucket?.Items) > 0)
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
