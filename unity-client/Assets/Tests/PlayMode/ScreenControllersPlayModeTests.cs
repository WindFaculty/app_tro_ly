using LocalAssistant.App;
using LocalAssistant.Chat;
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
        public void HomeScreenControllerRendersOverviewInAlwaysVisibleStage()
        {
            var refs = CreateHomeRefs();
            var store = new TaskViewModelStore();
            store.ApplyToday(new TodayTasksResponse
            {
                items = new System.Collections.Generic.List<TaskRecord> { new() { title = "Hop team", priority = "med", start_at = "09:00" } },
                due_soon = new System.Collections.Generic.List<TaskRecord> { new() { title = "Gui bao cao", priority = "high", due_at = "17:00" } },
            });
            store.ApplyInbox(new TaskListResponse
            {
                items = new System.Collections.Generic.List<TaskRecord> { new() { title = "Phan loai ghi chu", priority = "low" } },
            });

            var controller = new HomeScreenController(refs);
            controller.Render(store);
            controller.RenderAssistantOrbit(new ChatPanelSnapshot
            {
                StatusBadge = "READY",
                StatusTitle = "Ready for the next turn",
                StatusDetail = "Route planner | Provider local | Fallbacks 0",
            });

            StringAssert.Contains("1 scheduled", refs.TaskSummaryText.text);
            Assert.AreEqual("1", refs.TodayCountText.text);
            Assert.AreEqual(DisplayStyle.Flex, refs.TaskContentText.style.display.value);
            StringAssert.Contains("Hop team", refs.FocusText.text);
            StringAssert.Contains("Room template active", refs.StagePlaceholderText.text);
            Assert.AreEqual("READY", refs.HomeChatStatusBadge.text);
            StringAssert.Contains("planner", refs.HomeChatStatusDetail.text);
        }

        [Test]
        public void HomeScreenControllerBindRaisesQuickAddRequestAndClearsInput()
        {
            var refs = CreateHomeRefs();
            refs.QuickAddInput.value = "nap task";
            var controller = new HomeScreenController(refs);
            string submitted = null;
            controller.QuickAddRequested += value => submitted = value;
            controller.RequestQuickAdd();

            Assert.AreEqual("nap task", submitted);
            Assert.AreEqual(string.Empty, refs.QuickAddInput.value);
        }

        [Test]
        public void HomeScreenControllerEnterKeySubmitsQuickAdd()
        {
            var refs = CreateHomeRefs();
            refs.QuickAddInput.value = "xac nhan";
            var controller = new HomeScreenController(refs);
            string submitted = null;
            controller.QuickAddRequested += value => submitted = value;

            using var evt = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None);
            controller.HandleQuickAddKeyDown(evt);

            Assert.AreEqual("xac nhan", submitted);
        }

        [Test]
        public void ScheduleScreenControllerRendersListCardsAndSummary()
        {
            var refs = CreateScheduleRefs();
            var store = new TaskViewModelStore();
            store.ApplyInbox(new TaskListResponse
            {
                items = new System.Collections.Generic.List<TaskRecord> { new() { title = "Gui mail", priority = "low" } },
            });

            var controller = new ScheduleScreenController(refs);
            controller.Render(store, AppScreen.Inbox, "2026-03-26");

            StringAssert.Contains("1 inbox", refs.ScheduleSummaryText.text);
            StringAssert.Contains("Inbox triage", refs.TaskSheetHeaderTitle.text);
            Assert.AreEqual(1, refs.ScheduleListContainer.childCount);
            Assert.AreEqual(DisplayStyle.None, refs.ScheduleEmptyStateText.style.display.value);
        }

        [Test]
        public void ScheduleScreenControllerRaisesDateAndTaskActionEvents()
        {
            var refs = CreateScheduleRefs();
            var controller = new ScheduleScreenController(refs);
            var todayRequested = false;
            int observedOffset = 0;
            string selectedDay = null;
            var todayViewRequested = false;
            var weekViewRequested = false;
            var inboxRequested = false;
            var completedRequested = false;
            string completedTaskId = null;
            string scheduledTaskId = null;
            string scheduledDate = null;
            controller.TodayViewRequested += () => todayViewRequested = true;
            controller.WeekViewRequested += () => weekViewRequested = true;
            controller.TodayRequested += () => todayRequested = true;
            controller.DateOffsetRequested += value => observedOffset = value;
            controller.DaySelected += value => selectedDay = value;
            controller.InboxRequested += () => inboxRequested = true;
            controller.CompletedRequested += () => completedRequested = true;
            controller.CompleteTaskRequested += value => completedTaskId = value;
            controller.ScheduleTaskRequested += (taskId, date) =>
            {
                scheduledTaskId = taskId;
                scheduledDate = date;
            };

            controller.RequestTodayView();
            controller.RequestWeekView();
            controller.RequestToday();
            controller.RequestDateOffset(-1);
            controller.RequestDaySelected("2026-03-26");
            controller.RequestInbox();
            controller.RequestCompleted();
            controller.RequestCompleteTask("task-1");
            controller.RequestScheduleTask("task-2", "2026-03-27");

            Assert.IsTrue(todayViewRequested);
            Assert.IsTrue(weekViewRequested);
            Assert.IsTrue(todayRequested);
            Assert.AreEqual(-1, observedOffset);
            Assert.AreEqual("2026-03-26", selectedDay);
            Assert.IsTrue(inboxRequested);
            Assert.IsTrue(completedRequested);
            Assert.AreEqual("task-1", completedTaskId);
            Assert.AreEqual("task-2", scheduledTaskId);
            Assert.AreEqual("2026-03-27", scheduledDate);
        }

        [Test]
        public void ScheduleScreenControllerDoesNotRaiseInboxOrCompletedWhenDisabled()
        {
            var refs = CreateScheduleRefs();
            var controller = new ScheduleScreenController(refs);
            var inboxRequested = false;
            var completedRequested = false;
            var todayViewRequested = false;
            controller.InboxRequested += () => inboxRequested = true;
            controller.CompletedRequested += () => completedRequested = true;
            controller.TodayViewRequested += () => todayViewRequested = true;

            controller.SetTaskActionsEnabled(false);
            controller.RequestTodayView();
            controller.RequestInbox();
            controller.RequestCompleted();

            Assert.IsFalse(todayViewRequested);
            Assert.IsFalse(inboxRequested);
            Assert.IsFalse(completedRequested);
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
            StringAssert.Contains("Voice replies are On", refs.SettingsSummaryText.text);
            StringAssert.Contains("Provider:", refs.SettingsModelSummaryText.text);
            Assert.AreEqual("Saved", refs.SettingsStatusText.text);
            Assert.IsFalse(refs.SaveSettingsButton.enabledSelf);
        }

        [Test]
        public void SettingsScreenControllerBindRaisesSettingEvents()
        {
            var refs = CreateSettingsRefs();
            var controller = new SettingsScreenController(refs);
            bool reloadRequested = false;
            bool saveRequested = false;
            bool? speakReplies = null;
            controller.ReloadRequested += () => reloadRequested = true;
            controller.SaveRequested += () => saveRequested = true;
            controller.SpeakRepliesChanged += value => speakReplies = value;
            controller.RequestReload();
            controller.RequestSave();
            controller.NotifySpeakRepliesChanged(true);

            Assert.IsTrue(reloadRequested);
            Assert.IsTrue(saveRequested);
            Assert.AreEqual(true, speakReplies);
        }

        [Test]
        public void SettingsScreenControllerDoesNotRaiseReloadOrSaveWhenDisabled()
        {
            var refs = CreateSettingsRefs();
            var controller = new SettingsScreenController(refs);
            var reloadRequested = false;
            var saveRequested = false;
            controller.ReloadRequested += () => reloadRequested = true;
            controller.SaveRequested += () => saveRequested = true;

            controller.SetEditable(false);
            controller.RequestReload();
            controller.RequestSave();

            Assert.IsFalse(reloadRequested);
            Assert.IsFalse(saveRequested);
        }

        private static HomeScreenRefs CreateHomeRefs()
        {
            return new HomeScreenRefs
            {
                HomeStageViewport = new VisualElement(),
                HomeAvatarStateBadge = new Label(),
                StagePlaceholderText = new Label(),
                RoomActivityTitleText = new Label(),
                RoomActivityDetailText = new Label(),
                RoomModeText = new Label(),
                RoomGoToButton = new Button(),
                RoomInspectButton = new Button(),
                RoomUseButton = new Button(),
                RoomReturnButton = new Button(),
                RoomHotspotToggleButton = new Button(),
                SelectedRoomObjectTitleText = new Label(),
                SelectedRoomObjectMetaText = new Label(),
                SelectedRoomObjectActionText = new Label(),
                TaskSummaryText = new Label(),
                TaskContentText = new Label(),
                TaskEmptyStateText = new Label(),
                QuickAddStatusText = new Label(),
                QuickAddInput = new TextField(),
                QuickAddButton = new Button(),
                QuickAddHintText = new Label(),
                TodayCountText = new Label(),
                DueSoonCountText = new Label(),
                OverdueCountText = new Label(),
                InboxCountText = new Label(),
                CompletedCountText = new Label(),
                FocusText = new Label(),
                DueSoonText = new Label(),
                OverdueText = new Label(),
                HomeChatStatusBadge = new Label(),
                HomeChatStatusTitle = new Label(),
                HomeChatStatusDetail = new Label(),
            };
        }

        private static ScheduleScreenRefs CreateScheduleRefs()
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
                SettingsActionHintText = new Label(),
                SettingsVoiceSummaryText = new Label(),
                SettingsAutomationSummaryText = new Label(),
                SettingsModelSummaryText = new Label(),
                SettingsMemorySummaryText = new Label(),
                SaveSettingsButton = new Button(),
                ReloadSettingsButton = new Button(),
            };
        }
    }
}
