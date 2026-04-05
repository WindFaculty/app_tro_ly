using System.Threading.Tasks;
using LocalAssistant.Features.Schedule;
using LocalAssistant.Tasks;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class PlannerTaskCommandApplicationServiceTests
    {
        [Test]
        public async Task CompleteTaskAsyncBuildsPlannerActionSummary()
        {
            var backend = new FakePlannerBackend();
            var service = new PlannerTaskCommandApplicationService(backend);

            var result = await service.CompleteTaskAsync("task-1");

            Assert.AreEqual("task-1", backend.LastCompletedTaskId);
            Assert.AreEqual("complete_task", result.ActionType);
            Assert.AreEqual("Close loop", result.Title);
            Assert.AreEqual("Updated directly from the Schedule screen.", result.Detail);
        }

        [Test]
        public async Task ScheduleInboxTaskAsyncBuildsPlannerActionSummary()
        {
            var backend = new FakePlannerBackend();
            var service = new PlannerTaskCommandApplicationService(backend);

            var result = await service.ScheduleInboxTaskAsync("task-2", "2026-04-06");

            Assert.AreEqual("task-2", backend.LastScheduledTaskId);
            Assert.AreEqual("2026-04-06", backend.LastTargetDate);
            Assert.AreEqual("reschedule_task", result.ActionType);
            Assert.AreEqual("Scheduled to 2026-04-06 from the Schedule inbox view.", result.Detail);
        }

        private sealed class FakePlannerBackend : IPlannerBackendIntegration
        {
            public string LastCompletedTaskId { get; private set; } = string.Empty;
            public string LastScheduledTaskId { get; private set; } = string.Empty;
            public string LastTargetDate { get; private set; } = string.Empty;

            public Task<PlannerTaskSnapshot> LoadSnapshotAsync(string selectedDate)
            {
                return Task.FromResult(new PlannerTaskSnapshot());
            }

            public Task<PlannerTaskMutationResult> CompleteTaskAsync(string taskId)
            {
                LastCompletedTaskId = taskId;
                return Task.FromResult(new PlannerTaskMutationResult
                {
                    TaskId = taskId,
                    Title = "Close loop",
                    Status = "done",
                });
            }

            public Task<PlannerTaskMutationResult> ScheduleInboxTaskAsync(string taskId, string targetDate)
            {
                LastScheduledTaskId = taskId;
                LastTargetDate = targetDate;
                return Task.FromResult(new PlannerTaskMutationResult
                {
                    TaskId = taskId,
                    Title = "Moved item",
                    Status = "planned",
                    ScheduledDate = targetDate,
                });
            }
        }
    }
}
