using System.Collections.Generic;
using LocalAssistant.Core;
using LocalAssistant.Tasks;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class TaskViewModelStoreTests
    {
        [Test]
        public void GroupWeekByDayUsesBucketDate()
        {
            var store = new TaskViewModelStore();
            store.ApplyWeek(new WeekTasksResponse
            {
                days = new List<WeekDayBucket>
                {
                    new()
                    {
                        date = "2026-03-14",
                        task_count = 1,
                        items = new List<TaskRecord> { new() { id = "task_1", title = "Bao cao" } },
                    },
                },
            });

            var grouped = TaskViewModelStore.GroupWeekByDay(store.Week);
            Assert.That(grouped.ContainsKey("2026-03-14"));
            Assert.AreEqual("Bao cao", grouped["2026-03-14"][0].title);
        }

        [Test]
        public void BuildTodayTextIncludesOverdueSection()
        {
            var store = new TaskViewModelStore();
            store.ApplyToday(new TodayTasksResponse
            {
                items = new List<TaskRecord> { new() { title = "Task A", priority = "high" } },
                overdue = new List<TaskRecord> { new() { title = "Task B", priority = "critical" } },
            });

            var text = store.BuildTabText("Today");
            StringAssert.Contains("Overdue", text);
            StringAssert.Contains("Task B", text);
        }

        [Test]
        public void BuildOverviewTextIncludesKeyCounters()
        {
            var store = new TaskViewModelStore();
            store.ApplyToday(new TodayTasksResponse
            {
                items = new List<TaskRecord> { new() { title = "Task A" } },
                due_soon = new List<TaskRecord> { new() { title = "Task B" } },
            });
            store.ApplyInbox(new TaskListResponse
            {
                items = new List<TaskRecord> { new() { title = "Task C" }, new() { title = "Task D" } },
            });
            store.ApplyCompleted(new TaskListResponse
            {
                items = new List<TaskRecord> { new() { title = "Task E" } },
            });
            store.ApplyWeek(new WeekTasksResponse
            {
                conflicts = new List<ConflictRecord> { new() { date = "2026-03-14" } },
            });

            var summary = store.BuildOverviewText();
            StringAssert.Contains("Today 1", summary);
            StringAssert.Contains("Due soon 1", summary);
            StringAssert.Contains("Inbox 2", summary);
            StringAssert.Contains("Completed 1", summary);
            StringAssert.Contains("Conflicts 1", summary);
        }
    }
}
