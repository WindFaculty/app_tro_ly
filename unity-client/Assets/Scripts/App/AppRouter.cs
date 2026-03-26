using System;
using LocalAssistant.Core;
using UnityEngine.UIElements;

namespace LocalAssistant.App
{
    public enum AppScreen
    {
        Today,
        Week,
        Inbox,
        Completed,
        Settings,
    }

    public sealed class AppRouter
    {
        private readonly AppShellRefs shell;
        private readonly ScheduleScreenRefs schedule;
        private readonly SettingsScreenRefs settings;
        private readonly ChatPanelRefs chat;
        private readonly Action<AppScreen> onScreenChanged;

        public AppRouter(AppShellRefs shell, ScheduleScreenRefs schedule, SettingsScreenRefs settings, ChatPanelRefs chat, Action<AppScreen> onScreenChanged)
        {
            this.shell = shell;
            this.schedule = schedule;
            this.settings = settings;
            this.chat = chat;
            this.onScreenChanged = onScreenChanged;
        }

        public AppScreen CurrentScreen { get; private set; }

        public void BindTabs()
        {
            shell.TodayTab.clicked += () => Navigate(AppScreen.Today);
            shell.WeekTab.clicked += () => Navigate(AppScreen.Week);
            schedule.InboxTab.clicked += () => Navigate(AppScreen.Inbox);
            schedule.CompletedTab.clicked += () => Navigate(AppScreen.Completed);
            shell.SettingsTab.clicked += () => Navigate(AppScreen.Settings);
        }

        public void Navigate(AppScreen screen)
        {
            CurrentScreen = screen;

            var isHome = screen == AppScreen.Today;
            var isSchedule = screen == AppScreen.Week || screen == AppScreen.Inbox || screen == AppScreen.Completed;
            var isSettings = screen == AppScreen.Settings;

            SetDisplay(shell.HomeViewContainer, isHome);
            SetDisplay(shell.ScheduleViewContainer, isSchedule);
            SetDisplay(settings.SettingsPanel, isSettings);
            SetDisplay(chat.ChatPanelView, !isSchedule);
            SetDisplay(shell.ScheduleSideView, isSchedule);

            SetTabButtonVisual(shell.TodayTab, screen == AppScreen.Today);
            SetTabButtonVisual(shell.WeekTab, screen == AppScreen.Week || screen == AppScreen.Inbox || screen == AppScreen.Completed);
            SetTabButtonVisual(schedule.InboxTab, screen == AppScreen.Inbox);
            SetTabButtonVisual(schedule.CompletedTab, screen == AppScreen.Completed);
            SetTabButtonVisual(shell.SettingsTab, screen == AppScreen.Settings);

            onScreenChanged?.Invoke(screen);
        }

        private static void SetDisplay(VisualElement element, bool isVisible)
        {
            if (element != null)
            {
                element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static void SetTabButtonVisual(Button button, bool isActive)
        {
            if (button == null)
            {
                return;
            }

            if (isActive)
                button.AddToClassList("active");
            else
                button.RemoveFromClassList("active");
        }
    }
}
