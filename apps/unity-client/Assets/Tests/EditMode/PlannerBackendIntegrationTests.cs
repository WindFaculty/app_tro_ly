using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LocalAssistant.Core;
using LocalAssistant.Network;
using LocalAssistant.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class PlannerBackendIntegrationTests
    {
        [Test]
        public async Task LoadSnapshotAsyncMapsPlannerResponsesIntoTypedSnapshot()
        {
            var api = new FakeApiClient
            {
                Today = new TodayTasksResponse
                {
                    date = "2026-04-05",
                    items = new List<TaskRecord> { new() { id = "today-1", title = "Morning review", priority = "high", start_at = "08:30" } },
                    overdue = new List<TaskRecord> { new() { id = "overdue-1", title = "Late follow-up", due_at = "2026-04-04T18:00:00" } },
                },
                Week = new WeekTasksResponse
                {
                    start_date = "2026-04-05",
                    end_date = "2026-04-11",
                    overdue_count = 2,
                    days = new List<WeekDayBucket>
                    {
                        new()
                        {
                            date = "2026-04-05",
                            task_count = 1,
                            high_priority_count = 1,
                            items = new List<TaskRecord> { new() { id = "week-1", title = "Plan sprint", priority = "high" } },
                        },
                    },
                    conflicts = new List<ConflictRecord>
                    {
                        new() { date = "2026-04-07", task_ids = new List<string> { "week-1" }, titles = new List<string> { "Plan sprint" } },
                    },
                },
                Inbox = new TaskListResponse
                {
                    items = new List<TaskRecord> { new() { id = "inbox-1", title = "Capture idea", category = "notes" } },
                    count = 1,
                },
                Completed = new TaskListResponse
                {
                    items = new List<TaskRecord> { new() { id = "done-1", title = "Ship patch", status = "done" } },
                    count = 1,
                },
            };

            var integration = new PlannerBackendIntegration(api);
            var snapshot = await integration.LoadSnapshotAsync("2026-04-05");

            Assert.AreEqual("2026-04-05", api.LastTodayDate);
            Assert.AreEqual("2026-04-05", api.LastWeekStartDate);
            Assert.AreEqual("Morning review", snapshot.Today.Items[0].Title);
            Assert.AreEqual("08:30", snapshot.Today.Items[0].StartAt);
            Assert.AreEqual("2026-04-05", snapshot.Week.Days[0].Date);
            Assert.AreEqual(1, snapshot.Week.Days[0].TaskCount);
            Assert.AreEqual("Capture idea", snapshot.Inbox.Items[0].Title);
            Assert.AreEqual("done", snapshot.Completed.Items[0].Status);
            Assert.AreEqual("Plan sprint", snapshot.Week.Conflicts[0].Titles[0]);
        }

        [Test]
        public async Task MutationCallsMapBackIntoPlannerResult()
        {
            var api = new FakeApiClient
            {
                CompleteResponse = new TaskRecord { id = "task-1", title = "Close loop", status = "done" },
                RescheduleResponse = new TaskRecord { id = "task-2", title = "Move to inbox date", status = "planned", scheduled_date = "2026-04-06" },
            };

            var integration = new PlannerBackendIntegration(api);
            var completed = await integration.CompleteTaskAsync("task-1");
            var rescheduled = await integration.ScheduleInboxTaskAsync("task-2", "2026-04-06");

            Assert.AreEqual("task-1", api.LastCompletedTaskId);
            Assert.AreEqual("Close loop", completed.Title);
            Assert.AreEqual("task-2", api.LastRescheduledTaskId);
            Assert.AreEqual("2026-04-06", api.LastReschedulePayload.scheduled_date);
            Assert.AreEqual("2026-04-06", rescheduled.ScheduledDate);
        }

        private sealed class FakeApiClient : IAssistantApiClient
        {
            public string EventsUrl => "ws://127.0.0.1:8096/v1/events";
            public string AssistantStreamUrl => "ws://127.0.0.1:8096/v1/assistant/stream";
            public HealthResponse Health { get; set; } = new();
            public TodayTasksResponse Today { get; set; } = new();
            public WeekTasksResponse Week { get; set; } = new();
            public TaskListResponse Inbox { get; set; } = new();
            public TaskListResponse Completed { get; set; } = new();
            public SettingsPayload Settings { get; set; } = new();
            public ChatResponsePayload Chat { get; set; } = new();
            public string LastTodayDate { get; private set; }
            public string LastWeekStartDate { get; private set; }
            public string LastCompletedTaskId { get; private set; }
            public string LastRescheduledTaskId { get; private set; }
            public RescheduleTaskRequestPayload LastReschedulePayload { get; private set; }
            public TaskRecord CompleteResponse { get; set; } = new() { id = "complete-1", title = "Completed", status = "done" };
            public TaskRecord RescheduleResponse { get; set; } = new() { id = "reschedule-1", title = "Rescheduled", status = "planned" };

            public Task<HealthResponse> GetHealthAsync() => Task.FromResult(Health);

            public Task<TodayTasksResponse> GetTodayAsync(string date = null)
            {
                LastTodayDate = date;
                return Task.FromResult(Today);
            }

            public Task<WeekTasksResponse> GetWeekAsync(string startDate = null)
            {
                LastWeekStartDate = startDate;
                return Task.FromResult(Week);
            }

            public Task<TaskListResponse> GetInboxAsync() => Task.FromResult(Inbox);
            public Task<TaskListResponse> GetCompletedAsync() => Task.FromResult(Completed);
            public Task<SettingsPayload> GetSettingsAsync() => Task.FromResult(Settings);
            public Task<ChatResponsePayload> SendChatAsync(ChatRequestPayload payload) => Task.FromResult(Chat);

            public Task<TaskRecord> CompleteTaskAsync(string taskId, CompleteTaskRequestPayload payload = null)
            {
                LastCompletedTaskId = taskId;
                return Task.FromResult(CompleteResponse);
            }

            public Task<TaskRecord> RescheduleTaskAsync(string taskId, RescheduleTaskRequestPayload payload)
            {
                LastRescheduledTaskId = taskId;
                LastReschedulePayload = payload;
                return Task.FromResult(RescheduleResponse);
            }

            public Task<SpeechSttResponse> SendSpeechToTextAsync(byte[] wavBytes, string language = "vi")
            {
                throw new NotSupportedException();
            }

            public Task<SettingsPayload> UpdateSettingsAsync(SettingsPayload payload)
            {
                throw new NotSupportedException();
            }

            public Task<AudioClip> DownloadAudioClipAsync(string url)
            {
                throw new NotSupportedException();
            }
        }
    }
}
