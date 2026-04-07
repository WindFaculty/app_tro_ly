using LocalAssistant.App;
using LocalAssistant.Core;
using LocalAssistant.Features.Schedule;
using LocalAssistant.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.PlayMode
{
    public class PlannerModulePlayModeTests
    {
        [Test]
        public void ShowScreenOwnsPlannerTabActiveState()
        {
            var refs = CreateRefs();
            var module = new PlannerModule(refs);

            module.ShowScreen(AppScreen.Today);
            Assert.IsTrue(refs.TodayViewButton.ClassListContains("active"));
            Assert.IsFalse(refs.WeekViewButton.ClassListContains("active"));

            module.ShowScreen(AppScreen.Inbox);
            Assert.IsTrue(refs.InboxTab.ClassListContains("active"));
            Assert.IsFalse(refs.CompletedTab.ClassListContains("active"));

            module.ShowScreen(AppScreen.Week);
            Assert.IsFalse(refs.InboxTab.ClassListContains("active"));
            Assert.IsFalse(refs.CompletedTab.ClassListContains("active"));
            Assert.IsTrue(refs.WeekViewButton.ClassListContains("active"));
        }

        [Test]
        public void BindForwardsPlannerEventsAndScreenRequests()
        {
            var refs = CreateRefs();
            var module = new PlannerModule(refs);
            var todayRequested = false;
            var observedOffset = 0;
            string selectedDay = null;
            AppScreen? observedScreen = null;
            string completedTaskId = null;
            string scheduledTaskId = null;
            string scheduledDate = null;

            module.TodayRequested += () => todayRequested = true;
            module.DateOffsetRequested += offset => observedOffset = offset;
            module.DaySelected += date => selectedDay = date;
            module.ScreenRequested += screen => observedScreen = screen;
            module.CompleteTaskRequested += taskId => completedTaskId = taskId;
            module.ScheduleTaskRequested += (taskId, date) =>
            {
                scheduledTaskId = taskId;
                scheduledDate = date;
            };

            module.Bind();
            module.RequestTodayView();
            module.RequestToday();
            module.RequestDateOffset(1);
            module.RequestDaySelected("2026-04-04");
            module.RequestInbox();
            module.RequestCompleteTask("task-1");
            module.RequestScheduleTask("task-2", "2026-04-05");

            Assert.IsTrue(todayRequested);
            Assert.AreEqual(1, observedOffset);
            Assert.AreEqual("2026-04-04", selectedDay);
            Assert.AreEqual(AppScreen.Inbox, observedScreen);
            Assert.AreEqual("task-1", completedTaskId);
            Assert.AreEqual("task-2", scheduledTaskId);
            Assert.AreEqual("2026-04-05", scheduledDate);
        }

        [Test]
        public void RenderUsesPlannerOwnedCurrentScreen()
        {
            var refs = CreateRefs();
            var module = new PlannerModule(refs);
            var store = new TaskViewModelStore();
            store.ApplyCompleted(new TaskListResponse
            {
                items = new System.Collections.Generic.List<TaskRecord>
                {
                    new() { id = "task-1", title = "Wrap milestone", priority = "high", status = "done" },
                },
            });

            module.ShowScreen(AppScreen.Completed);
            module.Render(store, "2026-04-04");

            StringAssert.Contains("Completed review", refs.TaskSheetHeaderTitle.text);
            StringAssert.Contains("1 completed", refs.ScheduleSummaryText.text);
            Assert.AreEqual(1, refs.ScheduleListContainer.childCount);
            Assert.IsTrue(refs.CompletedTab.ClassListContains("active"));
        }

        private static ScheduleScreenRefs CreateRefs()
        {
            return new ScheduleScreenRefs
            {
                ScheduleTodayButton = new Button(),
                SchedulePrevButton = new Button(),
                ScheduleNextButton = new Button(),
                TodayViewButton = new Button(),
                WeekViewButton = new Button(),
                InboxTab = new Button(),
                CompletedTab = new Button(),
                TaskSheetHeaderTitle = new Label(),
                TaskSheetMonthLabel = new Label(),
                CalendarArea = new VisualElement(),
                ScheduleSummaryText = new Label(),
                ScheduleMetaText = new Label(),
                ScheduleEmptyStateText = new Label(),
                ScheduleListContainer = new VisualElement(),
            };
        }
    }
}
