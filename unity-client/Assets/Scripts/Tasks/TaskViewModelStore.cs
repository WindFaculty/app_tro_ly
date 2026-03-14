using System.Collections.Generic;
using System.Linq;
using System.Text;
using LocalAssistant.Core;

namespace LocalAssistant.Tasks
{
    public sealed class TaskViewModelStore
    {
        public TodayTasksResponse Today { get; private set; } = new();
        public WeekTasksResponse Week { get; private set; } = new();
        public TaskListResponse Inbox { get; private set; } = new();
        public TaskListResponse Completed { get; private set; } = new();

        public void ApplyToday(TodayTasksResponse payload) => Today = payload ?? new TodayTasksResponse();
        public void ApplyWeek(WeekTasksResponse payload) => Week = payload ?? new WeekTasksResponse();
        public void ApplyInbox(TaskListResponse payload) => Inbox = payload ?? new TaskListResponse();
        public void ApplyCompleted(TaskListResponse payload) => Completed = payload ?? new TaskListResponse();

        public string BuildTabText(string tabName)
        {
            return tabName switch
            {
                "Today" => BuildTodayText(),
                "Week" => BuildWeekText(),
                "Inbox" => BuildListText("Inbox", Inbox.items),
                "Completed" => BuildListText("Completed", Completed.items),
                "Settings" => "Settings are loaded from the local backend.\nToggle speech and mini mode from backend-backed settings.",
                _ => string.Empty,
            };
        }

        public string BuildOverviewText()
        {
            return
                $"Today {CountOf(Today.items)}  |  Due soon {CountOf(Today.due_soon)}  |  Overdue {CountOf(Today.overdue)}  |  Inbox {CountOf(Inbox.items)}  |  Completed {CountOf(Completed.items)}  |  Conflicts {CountOf(Week.conflicts)}";
        }

        public static Dictionary<string, List<TaskRecord>> GroupWeekByDay(WeekTasksResponse response)
        {
            return response.days.ToDictionary(bucket => bucket.date, bucket => bucket.items ?? new List<TaskRecord>());
        }

        private string BuildTodayText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Today");
            builder.AppendLine();
            AppendList(builder, "Scheduled", Today.items);
            AppendList(builder, "Due Soon", Today.due_soon);
            AppendList(builder, "Overdue", Today.overdue);
            return builder.ToString().Trim();
        }

        private string BuildWeekText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Week");
            builder.AppendLine();
            foreach (var bucket in Week.days)
            {
                builder.AppendLine($"{bucket.date} ({bucket.task_count})");
                if (bucket.items == null || bucket.items.Count == 0)
                {
                    builder.AppendLine("  - No tasks");
                    continue;
                }

                foreach (var task in bucket.items)
                {
                    builder.AppendLine($"  - [{task.priority}] {task.title}");
                }
            }

            if (Week.conflicts != null && Week.conflicts.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"Conflicts: {Week.conflicts.Count}");
            }

            return builder.ToString().Trim();
        }

        private string BuildListText(string title, List<TaskRecord> items)
        {
            var builder = new StringBuilder();
            builder.AppendLine(title);
            builder.AppendLine();
            AppendList(builder, title, items);
            return builder.ToString().Trim();
        }

        private static void AppendList(StringBuilder builder, string title, List<TaskRecord> items)
        {
            builder.AppendLine(title + ":");
            if (items == null || items.Count == 0)
            {
                builder.AppendLine("- No tasks");
                builder.AppendLine();
                return;
            }

            foreach (var task in items)
            {
                var timeBlock = string.IsNullOrEmpty(task.start_at) ? string.Empty : $" @ {task.start_at}";
                builder.AppendLine($"- [{task.priority}] {task.title}{timeBlock}");
            }

            builder.AppendLine();
        }

        private static int CountOf<T>(List<T> items)
        {
            return items?.Count ?? 0;
        }
    }
}
