using System.Collections.Generic;
using System.Linq;
using System.Text;
using LocalAssistant.Core;

namespace LocalAssistant.Tasks
{
    public sealed class TaskViewModelStore : IPlannerTaskSnapshotSource
    {
        public PlannerTodaySnapshot Today { get; private set; } = new();
        public PlannerWeekSnapshot Week { get; private set; } = new();
        public PlannerTaskListSnapshot Inbox { get; private set; } = new();
        public PlannerTaskListSnapshot Completed { get; private set; } = new();

        public void ApplySnapshot(PlannerTaskSnapshot snapshot)
        {
            Today = snapshot?.Today ?? new PlannerTodaySnapshot();
            Week = snapshot?.Week ?? new PlannerWeekSnapshot();
            Inbox = snapshot?.Inbox ?? new PlannerTaskListSnapshot();
            Completed = snapshot?.Completed ?? new PlannerTaskListSnapshot();
        }

        public void ApplyToday(TodayTasksResponse payload) => Today = PlannerTaskMapper.MapToday(payload);
        public void ApplyWeek(WeekTasksResponse payload) => Week = PlannerTaskMapper.MapWeek(payload);
        public void ApplyInbox(TaskListResponse payload) => Inbox = PlannerTaskMapper.MapTaskList(payload);
        public void ApplyCompleted(TaskListResponse payload) => Completed = PlannerTaskMapper.MapTaskList(payload);

        public string BuildTabText(string tabName)
        {
            return tabName switch
            {
                "Today" => BuildTodayText(),
                "Week" => BuildWeekText(),
                "Inbox" => BuildListText("Inbox", Inbox.Items),
                "Completed" => BuildListText("Completed", Completed.Items),
                "Settings" => "Settings are loaded from the local backend.\nToggle speech and mini mode from backend-backed settings.",
                _ => string.Empty,
            };
        }

        public string BuildOverviewText()
        {
            return
                $"Today {CountOf(Today.Items)}  |  Due soon {CountOf(Today.DueSoon)}  |  Overdue {CountOf(Today.Overdue)}  |  Inbox {CountOf(Inbox.Items)}  |  Completed {CountOf(Completed.Items)}  |  Conflicts {CountOf(Week.Conflicts)}";
        }

        public static Dictionary<string, List<PlannerTaskItem>> GroupWeekByDay(PlannerWeekSnapshot response)
        {
            return response.Days.ToDictionary(bucket => bucket.Date, bucket => bucket.Items ?? new List<PlannerTaskItem>());
        }

        private string BuildTodayText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Today");
            builder.AppendLine();
            AppendList(builder, "Scheduled", Today.Items);
            AppendList(builder, "Due Soon", Today.DueSoon);
            AppendList(builder, "Overdue", Today.Overdue);
            return builder.ToString().Trim();
        }

        private string BuildWeekText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Week");
            builder.AppendLine();
            foreach (var bucket in Week.Days)
            {
                builder.AppendLine($"{bucket.Date} ({bucket.TaskCount})");
                if (bucket.Items == null || bucket.Items.Count == 0)
                {
                    builder.AppendLine("  - No tasks");
                    continue;
                }

                foreach (var task in bucket.Items)
                {
                    builder.AppendLine($"  - [{task.Priority}] {task.Title}");
                }
            }

            if (Week.Conflicts != null && Week.Conflicts.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"Conflicts: {Week.Conflicts.Count}");
            }

            return builder.ToString().Trim();
        }

        private string BuildListText(string title, List<PlannerTaskItem> items)
        {
            var builder = new StringBuilder();
            builder.AppendLine(title);
            builder.AppendLine();
            AppendList(builder, title, items);
            return builder.ToString().Trim();
        }

        private static void AppendList(StringBuilder builder, string title, List<PlannerTaskItem> items)
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
                var timeBlock = string.IsNullOrEmpty(task.StartAt) ? string.Empty : $" @ {task.StartAt}";
                builder.AppendLine($"- [{task.Priority}] {task.Title}{timeBlock}");
            }

            builder.AppendLine();
        }

        private static int CountOf<T>(List<T> items)
        {
            return items?.Count ?? 0;
        }
    }
}
