using LocalAssistant.App;
using LocalAssistant.Core;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.PlayMode
{
    public class AppRouterPlayModeTests
    {
        [Test]
        public void NavigateTodayShowsHomeAndChat()
        {
            CreateRefs(out var shell, out var schedule, out var settings, out var chat);
            var router = new AppRouter(shell, schedule, settings, chat, null);

            router.Navigate(AppScreen.Today);

            Assert.AreEqual(DisplayStyle.Flex, shell.HomeViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.None, shell.ScheduleViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.None, settings.SettingsPanel.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, chat.ChatPanelView.style.display.value);
            Assert.AreEqual(DisplayStyle.None, shell.ScheduleSideView.style.display.value);
            Assert.IsTrue(shell.TodayTab.ClassListContains("active"));
        }

        [Test]
        public void NavigateScheduleScreensShowScheduleAndSidebar()
        {
            CreateRefs(out var shell, out var schedule, out var settings, out var chat);
            var router = new AppRouter(shell, schedule, settings, chat, null);

            router.Navigate(AppScreen.Inbox);

            Assert.AreEqual(DisplayStyle.None, shell.HomeViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, shell.ScheduleViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.None, settings.SettingsPanel.style.display.value);
            Assert.AreEqual(DisplayStyle.None, chat.ChatPanelView.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, shell.ScheduleSideView.style.display.value);
            Assert.IsTrue(shell.WeekTab.ClassListContains("active"));
            Assert.IsTrue(schedule.InboxTab.ClassListContains("active"));
        }

        [Test]
        public void NavigateSettingsInvokesCallbackAndShowsSettings()
        {
            CreateRefs(out var shell, out var schedule, out var settings, out var chat);
            AppScreen? observedScreen = null;
            var router = new AppRouter(shell, schedule, settings, chat, screen => observedScreen = screen);

            router.Navigate(AppScreen.Settings);

            Assert.AreEqual(AppScreen.Settings, observedScreen);
            Assert.AreEqual(DisplayStyle.None, shell.HomeViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.None, shell.ScheduleViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, settings.SettingsPanel.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, chat.ChatPanelView.style.display.value);
            Assert.AreEqual(DisplayStyle.None, shell.ScheduleSideView.style.display.value);
            Assert.IsTrue(shell.SettingsTab.ClassListContains("active"));
        }

        [Test]
        public void NavigateSettingsClearsScheduleToolbarActiveState()
        {
            CreateRefs(out var shell, out var schedule, out var settings, out var chat);
            var router = new AppRouter(shell, schedule, settings, chat, null);

            router.Navigate(AppScreen.Inbox);
            router.Navigate(AppScreen.Settings);

            Assert.IsFalse(schedule.InboxTab.ClassListContains("active"));
            Assert.IsFalse(schedule.CompletedTab.ClassListContains("active"));
            Assert.IsFalse(shell.WeekTab.ClassListContains("active"));
            Assert.IsTrue(shell.SettingsTab.ClassListContains("active"));
        }

        private static void CreateRefs(out AppShellRefs shell, out ScheduleScreenRefs schedule, out SettingsScreenRefs settings, out ChatPanelRefs chat)
        {
            shell = new AppShellRefs
            {
                TodayTab = new Button(),
                WeekTab = new Button(),
                SettingsTab = new Button(),
                HomeViewContainer = new VisualElement(),
                ScheduleViewContainer = new VisualElement(),
                ScheduleSideView = new VisualElement(),
            };

            schedule = new ScheduleScreenRefs
            {
                InboxTab = new Button(),
                CompletedTab = new Button(),
            };

            settings = new SettingsScreenRefs
            {
                SettingsPanel = new VisualElement(),
            };

            chat = new ChatPanelRefs
            {
                ChatPanelView = new VisualElement(),
            };
        }
    }
}
