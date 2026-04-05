using System;
using System.Threading.Tasks;
using LocalAssistant.Tasks;

namespace LocalAssistant.Features.Schedule
{
    public sealed class PlannerTaskCommandResult
    {
        public string ActionType { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string ScheduledDate { get; set; } = string.Empty;
    }

    public sealed class PlannerTaskCommandApplicationService
    {
        private readonly IPlannerBackendIntegration plannerBackend;

        public PlannerTaskCommandApplicationService(IPlannerBackendIntegration plannerBackend)
        {
            this.plannerBackend = plannerBackend ?? throw new ArgumentNullException(nameof(plannerBackend));
        }

        public async Task<PlannerTaskCommandResult> CompleteTaskAsync(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw new ArgumentException("Task id is required.", nameof(taskId));
            }

            var updatedTask = await plannerBackend.CompleteTaskAsync(taskId);
            return new PlannerTaskCommandResult
            {
                ActionType = "complete_task",
                TaskId = updatedTask.TaskId,
                Title = updatedTask.Title,
                Detail = "Updated directly from the Schedule screen.",
                ScheduledDate = updatedTask.ScheduledDate,
            };
        }

        public async Task<PlannerTaskCommandResult> ScheduleInboxTaskAsync(string taskId, string targetDate)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw new ArgumentException("Task id is required.", nameof(taskId));
            }

            if (string.IsNullOrWhiteSpace(targetDate))
            {
                throw new ArgumentException("Target date is required.", nameof(targetDate));
            }

            var updatedTask = await plannerBackend.ScheduleInboxTaskAsync(taskId, targetDate);
            return new PlannerTaskCommandResult
            {
                ActionType = "reschedule_task",
                TaskId = updatedTask.TaskId,
                Title = updatedTask.Title,
                Detail = $"Scheduled to {targetDate} from the Schedule inbox view.",
                ScheduledDate = updatedTask.ScheduledDate,
            };
        }
    }
}
