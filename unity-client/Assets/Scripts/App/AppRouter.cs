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
        private readonly AssistantUiRefs ui;
        private readonly Action<AppScreen> onScreenChanged;

        public AppRouter(AssistantUiRefs ui, Action<AppScreen> onScreenChanged)
        {
            this.ui = ui;
            this.onScreenChanged = onScreenChanged;
        }

        public AppScreen CurrentScreen { get; private set; }

        public void BindTabs()
        {
            ui.TodayTab.clicked += () => Navigate(AppScreen.Today);
            ui.WeekTab.clicked += () => Navigate(AppScreen.Week);
            ui.InboxTab.clicked += () => Navigate(AppScreen.Inbox);
            ui.CompletedTab.clicked += () => Navigate(AppScreen.Completed);
            ui.SettingsTab.clicked += () => Navigate(AppScreen.Settings);
        }

        public void Navigate(AppScreen screen)
        {
            CurrentScreen = screen;

            var isHome = screen == AppScreen.Today;
            var isSchedule = screen == AppScreen.Week || screen == AppScreen.Inbox || screen == AppScreen.Completed;
            var isSettings = screen == AppScreen.Settings;

            SetDisplay(ui.HomeViewContainer, isHome);
            SetDisplay(ui.ScheduleViewContainer, isSchedule);
            SetDisplay(ui.SettingsPanel, isSettings);
            SetDisplay(ui.ChatPanelView, !isSchedule);
            SetDisplay(ui.ScheduleSideView, isSchedule);

            SetTabButtonVisual(ui.TodayTab, screen == AppScreen.Today);
            SetTabButtonVisual(ui.WeekTab, screen == AppScreen.Week || screen == AppScreen.Inbox || screen == AppScreen.Completed);
            SetTabButtonVisual(ui.InboxTab, screen == AppScreen.Inbox);
            SetTabButtonVisual(ui.CompletedTab, screen == AppScreen.Completed);
            SetTabButtonVisual(ui.SettingsTab, screen == AppScreen.Settings);

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
