using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LocalAssistant.Core;
using LocalAssistant.Network;

namespace LocalAssistant.Tasks
{
    public sealed class PlannerTaskItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "planned";
        public string Priority { get; set; } = "medium";
        public string Category { get; set; } = string.Empty;
        public string ScheduledDate { get; set; } = string.Empty;
        public string StartAt { get; set; } = string.Empty;
        public string EndAt { get; set; } = string.Empty;
        public string DueAt { get; set; } = string.Empty;
        public bool IsAllDay { get; set; }
        public string RepeatRule { get; set; } = "none";
        public int? EstimatedMinutes { get; set; }
        public int? ActualMinutes { get; set; }
        public List<string> Tags { get; set; } = new();
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
        public string CompletedAt { get; set; } = string.Empty;
    }

    public sealed class PlannerTodaySnapshot
    {
        public string Date { get; set; } = string.Empty;
        public List<PlannerTaskItem> Items { get; set; } = new();
        public List<PlannerTaskItem> Overdue { get; set; } = new();
        public List<PlannerTaskItem> DueSoon { get; set; } = new();
        public List<PlannerTaskItem> InProgress { get; set; } = new();
    }

    public sealed class PlannerDayBucketSnapshot
    {
        public string Date { get; set; } = string.Empty;
        public int TaskCount { get; set; }
        public int HighPriorityCount { get; set; }
        public List<PlannerTaskItem> Items { get; set; } = new();
    }

    public sealed class PlannerConflictSnapshot
    {
        public string Date { get; set; } = string.Empty;
        public List<string> TaskIds { get; set; } = new();
        public List<string> Titles { get; set; } = new();
    }

    public sealed class PlannerWeekSnapshot
    {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public List<PlannerDayBucketSnapshot> Days { get; set; } = new();
        public int OverdueCount { get; set; }
        public List<PlannerConflictSnapshot> Conflicts { get; set; } = new();
    }

    public sealed class PlannerTaskListSnapshot
    {
        public List<PlannerTaskItem> Items { get; set; } = new();
        public int Count { get; set; }
    }

    public sealed class PlannerTaskSnapshot
    {
        public PlannerTodaySnapshot Today { get; set; } = new();
        public PlannerWeekSnapshot Week { get; set; } = new();
        public PlannerTaskListSnapshot Inbox { get; set; } = new();
        public PlannerTaskListSnapshot Completed { get; set; } = new();
    }

    public sealed class PlannerTaskMutationResult
    {
        public string TaskId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ScheduledDate { get; set; } = string.Empty;
    }

    public interface IPlannerBackendIntegration
    {
        Task<PlannerTaskSnapshot> LoadSnapshotAsync(string selectedDate);
        Task<PlannerTaskMutationResult> CompleteTaskAsync(string taskId);
        Task<PlannerTaskMutationResult> ScheduleInboxTaskAsync(string taskId, string targetDate);
    }

    public sealed class PlannerBackendIntegration : IPlannerBackendIntegration
    {
        private readonly IAssistantApiClient apiClient;

        public PlannerBackendIntegration(IAssistantApiClient apiClient)
        {
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<PlannerTaskSnapshot> LoadSnapshotAsync(string selectedDate)
        {
            var todayTask = apiClient.GetTodayAsync(selectedDate);
            var weekTask = apiClient.GetWeekAsync(selectedDate);
            var inboxTask = apiClient.GetInboxAsync();
            var completedTask = apiClient.GetCompletedAsync();

            await Task.WhenAll(todayTask, weekTask, inboxTask, completedTask);

            return PlannerTaskMapper.MapSnapshot(
                await todayTask,
                await weekTask,
                await inboxTask,
                await completedTask);
        }

        public async Task<PlannerTaskMutationResult> CompleteTaskAsync(string taskId)
        {
            return PlannerTaskMapper.MapMutation(await apiClient.CompleteTaskAsync(taskId, new CompleteTaskRequestPayload()));
        }

        public async Task<PlannerTaskMutationResult> ScheduleInboxTaskAsync(string taskId, string targetDate)
        {
            return PlannerTaskMapper.MapMutation(await apiClient.RescheduleTaskAsync(taskId, new RescheduleTaskRequestPayload
            {
                scheduled_date = targetDate ?? string.Empty,
            }));
        }
    }

    internal static class PlannerTaskMapper
    {
        public static PlannerTaskSnapshot MapSnapshot(
            TodayTasksResponse today,
            WeekTasksResponse week,
            TaskListResponse inbox,
            TaskListResponse completed)
        {
            return new PlannerTaskSnapshot
            {
                Today = MapToday(today),
                Week = MapWeek(week),
                Inbox = MapTaskList(inbox),
                Completed = MapTaskList(completed),
            };
        }

        public static PlannerTodaySnapshot MapToday(TodayTasksResponse payload)
        {
            return new PlannerTodaySnapshot
            {
                Date = payload?.date ?? string.Empty,
                Items = MapItems(payload?.items),
                Overdue = MapItems(payload?.overdue),
                DueSoon = MapItems(payload?.due_soon),
                InProgress = MapItems(payload?.in_progress),
            };
        }

        public static PlannerWeekSnapshot MapWeek(WeekTasksResponse payload)
        {
            var days = new List<PlannerDayBucketSnapshot>();
            if (payload?.days != null)
            {
                foreach (var bucket in payload.days)
                {
                    if (bucket == null)
                    {
                        continue;
                    }

                    var items = MapItems(bucket.items);
                    days.Add(new PlannerDayBucketSnapshot
                    {
                        Date = bucket.date ?? string.Empty,
                        TaskCount = Math.Max(bucket.task_count, items.Count),
                        HighPriorityCount = bucket.high_priority_count,
                        Items = items,
                    });
                }
            }

            var conflicts = new List<PlannerConflictSnapshot>();
            if (payload?.conflicts != null)
            {
                foreach (var conflict in payload.conflicts)
                {
                    if (conflict == null)
                    {
                        continue;
                    }

                    conflicts.Add(new PlannerConflictSnapshot
                    {
                        Date = conflict.date ?? string.Empty,
                        TaskIds = CopyList(conflict.task_ids),
                        Titles = CopyList(conflict.titles),
                    });
                }
            }

            return new PlannerWeekSnapshot
            {
                StartDate = payload?.start_date ?? string.Empty,
                EndDate = payload?.end_date ?? string.Empty,
                Days = days,
                OverdueCount = payload?.overdue_count ?? 0,
                Conflicts = conflicts,
            };
        }

        public static PlannerTaskListSnapshot MapTaskList(TaskListResponse payload)
        {
            var items = MapItems(payload?.items);
            return new PlannerTaskListSnapshot
            {
                Items = items,
                Count = payload == null ? 0 : Math.Max(payload.count, items.Count),
            };
        }

        public static PlannerTaskMutationResult MapMutation(TaskRecord payload)
        {
            return new PlannerTaskMutationResult
            {
                TaskId = payload?.id ?? string.Empty,
                Title = payload?.title ?? string.Empty,
                Status = payload?.status ?? string.Empty,
                ScheduledDate = payload?.scheduled_date ?? string.Empty,
            };
        }

        public static List<PlannerTaskItem> MapItems(List<TaskRecord> items)
        {
            var mapped = new List<PlannerTaskItem>();
            if (items == null)
            {
                return mapped;
            }

            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

                mapped.Add(new PlannerTaskItem
                {
                    Id = item.id ?? string.Empty,
                    Title = item.title ?? string.Empty,
                    Description = item.description ?? string.Empty,
                    Status = item.status ?? "planned",
                    Priority = item.priority ?? "medium",
                    Category = item.category ?? string.Empty,
                    ScheduledDate = item.scheduled_date ?? string.Empty,
                    StartAt = item.start_at ?? string.Empty,
                    EndAt = item.end_at ?? string.Empty,
                    DueAt = item.due_at ?? string.Empty,
                    IsAllDay = item.is_all_day,
                    RepeatRule = item.repeat_rule ?? "none",
                    EstimatedMinutes = item.estimated_minutes,
                    ActualMinutes = item.actual_minutes,
                    Tags = CopyList(item.tags),
                    CreatedAt = item.created_at ?? string.Empty,
                    UpdatedAt = item.updated_at ?? string.Empty,
                    CompletedAt = item.completed_at ?? string.Empty,
                });
            }

            return mapped;
        }

        private static List<string> CopyList(List<string> values)
        {
            return values == null ? new List<string>() : new List<string>(values);
        }
    }
}
