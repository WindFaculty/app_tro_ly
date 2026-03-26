using LocalAssistant.App;
using LocalAssistant.Core;
using LocalAssistant.Features.Home;
using LocalAssistant.Features.Schedule;
using LocalAssistant.Features.Settings;
using LocalAssistant.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.PlayMode
{
    public class ScreenControllersPlayModeTests
    {
        [Test]
        public void HomeScreenControllerRendersOverviewAndHomeVisibility()
        {
            var refs = CreateHomeRefs();
            var store = new TaskViewModelStore();
            store.ApplyToday(new TodayTasksResponse
            {
                items = new System.Collections.Generic.List<TaskRecord> { new() { title = "Hop team", priority = "med" } },
            });

            var controller = new HomeScreenController(refs);
            controller.Render(store, "Stage placeholder", AppScreen.Today);

            StringAssert.Contains("Today", refs.TaskSummaryText.text);
            Assert.AreEqual(DisplayStyle.Flex, refs.TaskContentText.style.display.value);
            Assert.AreEqual("Stage placeholder", refs.StagePlaceholderText.text);
        }

        [Test]
        public void ScheduleScreenControllerRendersTabTextIntoCalendarArea()
        {
            var refs = CreateScheduleRefs();
            var calendarLabel = new Label();
            refs.CalendarArea.Add(calendarLabel);
            var store = new TaskViewModelStore();
            store.ApplyInbox(new TaskListResponse
            {
                items = new System.Collections.Generic.List<TaskRecord> { new() { title = "Gui mail", priority = "low" } },
            });

            var controller = new ScheduleScreenController(refs);
            controller.Render(store, AppScreen.Inbox, "2026-03-26");

            StringAssert.Contains("Inbox", calendarLabel.text);
            StringAssert.Contains("Inbox AI", refs.TaskSheetHeaderTitle.text);
        }

        [Test]
        public void SettingsScreenControllerRendersValuesAndStatus()
        {
            var refs = CreateSettingsRefs();
            var store = new SettingsViewModelStore();
            store.SetSpeakReplies(true);
            store.SetTranscriptPreview(true);
            store.SetMiniAssistantEnabled(true);
            store.SetReminderSpeechEnabled(false);

            var controller = new SettingsScreenController(refs);
            controller.Render(store);
            controller.SetStatus("Saved", Color.green);
            controller.SetEditable(false);

            Assert.IsTrue(refs.SpeakRepliesToggle.value);
            Assert.IsTrue(refs.TranscriptPreviewToggle.value);
            Assert.AreEqual("Saved", refs.SettingsStatusText.text);
            Assert.IsFalse(refs.SaveSettingsButton.enabledSelf);
        }

        private static HomeScreenRefs CreateHomeRefs()
        {
            return new HomeScreenRefs
            {
                TaskSummaryText = new Label(),
                TaskContentText = new Label(),
                StagePlaceholderText = new Label(),
                QuickAddInput = new TextField(),
                QuickAddButton = new Button(),
            };
        }

        private static ScheduleScreenRefs CreateScheduleRefs()
        {
            return new ScheduleScreenRefs
            {
                TaskSheetHeaderTitle = new Label(),
                TaskSheetMonthLabel = new Label(),
                CalendarArea = new VisualElement(),
            };
        }

        private static SettingsScreenRefs CreateSettingsRefs()
        {
            return new SettingsScreenRefs
            {
                SpeakRepliesToggle = new Toggle(),
                TranscriptPreviewToggle = new Toggle(),
                MiniAssistantToggle = new Toggle(),
                ReminderSpeechToggle = new Toggle(),
                SettingsSummaryText = new Label(),
                SettingsStatusText = new Label(),
                SaveSettingsButton = new Button(),
                ReloadSettingsButton = new Button(),
            };
        }
    }
}
