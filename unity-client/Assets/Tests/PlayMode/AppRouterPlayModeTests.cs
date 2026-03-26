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
            var refs = CreateRefs();
            var router = new AppRouter(refs, null);

            router.Navigate(AppScreen.Today);

            Assert.AreEqual(DisplayStyle.Flex, refs.HomeViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.None, refs.ScheduleViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.None, refs.SettingsPanel.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, refs.ChatPanelView.style.display.value);
            Assert.AreEqual(DisplayStyle.None, refs.ScheduleSideView.style.display.value);
            Assert.IsTrue(refs.TodayTab.ClassListContains("active"));
        }

        [Test]
        public void NavigateScheduleScreensShowScheduleAndSidebar()
        {
            var refs = CreateRefs();
            var router = new AppRouter(refs, null);

            router.Navigate(AppScreen.Inbox);

            Assert.AreEqual(DisplayStyle.None, refs.HomeViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, refs.ScheduleViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.None, refs.SettingsPanel.style.display.value);
            Assert.AreEqual(DisplayStyle.None, refs.ChatPanelView.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, refs.ScheduleSideView.style.display.value);
            Assert.IsTrue(refs.WeekTab.ClassListContains("active"));
            Assert.IsTrue(refs.InboxTab.ClassListContains("active"));
        }

        [Test]
        public void NavigateSettingsInvokesCallbackAndShowsSettings()
        {
            var refs = CreateRefs();
            AppScreen? observedScreen = null;
            var router = new AppRouter(refs, screen => observedScreen = screen);

            router.Navigate(AppScreen.Settings);

            Assert.AreEqual(AppScreen.Settings, observedScreen);
            Assert.AreEqual(DisplayStyle.None, refs.HomeViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.None, refs.ScheduleViewContainer.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, refs.SettingsPanel.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, refs.ChatPanelView.style.display.value);
            Assert.AreEqual(DisplayStyle.None, refs.ScheduleSideView.style.display.value);
            Assert.IsTrue(refs.SettingsTab.ClassListContains("active"));
        }

        private static AssistantUiRefs CreateRefs()
        {
            return new AssistantUiRefs
            {
                TodayTab = new Button(),
                WeekTab = new Button(),
                InboxTab = new Button(),
                CompletedTab = new Button(),
                SettingsTab = new Button(),
                HomeViewContainer = new VisualElement(),
                ScheduleViewContainer = new VisualElement(),
                SettingsPanel = new VisualElement(),
                ChatPanelView = new VisualElement(),
                ScheduleSideView = new VisualElement(),
            };
        }
    }
}
