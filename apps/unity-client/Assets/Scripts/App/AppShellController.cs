using System;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.App
{
    public sealed class AppShellController
    {
        private readonly AppShellRefs shell;
        private readonly AppShellState currentState = new();

        public AppShellController(AppShellRefs shell)
        {
            this.shell = shell;
        }

        public event Action RefreshRequested;
        public event Action<AppShellState> StateChanged;

        public AppShellState CurrentState => currentState;

        public void Bind()
        {
            if (shell.RefreshButton != null)
            {
                shell.RefreshButton.clicked += RequestRefresh;
            }

            UiButtonActionBinder.Bind(shell.FocusStageButton, FocusStage);
            UiButtonActionBinder.Bind(shell.ToggleCalendarButton, ToggleCalendar);
            UiButtonActionBinder.Bind(shell.ToggleChatButton, ToggleChat);
            UiButtonActionBinder.Bind(shell.ToggleSettingsButton, ToggleSettings);
            UiButtonActionBinder.Bind(shell.CloseSettingsButton, CloseSettings);

            if (shell.SettingsScrim != null)
            {
                shell.SettingsScrim.RegisterCallback<ClickEvent>(_ => CloseSettings());
            }

            ApplyShellState();
        }

        public void RenderBootState(string bannerText, string detailText, Color accentColor)
        {
            SetRuntimeStatus(bannerText, accentColor);
            if (shell.HealthBanner != null)
            {
                shell.HealthBanner.text = bannerText;
                shell.HealthBanner.style.color = new StyleColor(accentColor);
            }

            if (shell.StageStatusText != null)
            {
                shell.StageStatusText.text = detailText;
            }
        }

        public void RenderHealth(HealthResponse health)
        {
            if (shell.HealthBanner == null)
            {
                return;
            }

            shell.HealthBanner.text = $"{HealthStatusMapper.ToLabel(health.status)} runtime";
            shell.HealthBanner.style.color = new StyleColor(HealthStatusMapper.ToColor(health.status));
            SetRuntimeStatus(shell.HealthBanner.text, HealthStatusMapper.ToColor(health.status));
        }

        public void RenderStage(ShellStageSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (shell.AvatarStateText != null)
            {
                shell.AvatarStateText.text = snapshot.AvatarStateText;
            }

            if (shell.StageStatusText != null)
            {
                shell.StageStatusText.text = snapshot.StageStatusText;
            }

            if (shell.ScheduleInsightTitle != null)
            {
                shell.ScheduleInsightTitle.text = snapshot.InsightTitle;
            }

            if (shell.ScheduleInsightSummary != null)
            {
                shell.ScheduleInsightSummary.text = snapshot.InsightSummary;
            }

            if (shell.ScheduleInsightMeta != null)
            {
                shell.ScheduleInsightMeta.text = snapshot.InsightMeta;
            }
        }

        public void SetRefreshEnabled(bool isEnabled)
        {
            shell.RefreshButton?.SetEnabled(isEnabled);
        }

        public void FocusStage()
        {
            currentState.Focus = ShellRegionFocus.Stage;
            currentState.CalendarExpanded = false;
            currentState.SettingsOpen = false;
            ApplyShellState();
        }

        public void ToggleCalendar()
        {
            SetCalendarExpanded(!currentState.CalendarExpanded);
        }

        public void ToggleChat()
        {
            SetChatVisible(!currentState.ChatVisible);
        }

        public void ToggleSettings()
        {
            SetSettingsOpen(!currentState.SettingsOpen);
        }

        public void CloseSettings()
        {
            SetSettingsOpen(false);
        }

        public void SetCalendarExpanded(bool isExpanded)
        {
            currentState.CalendarExpanded = isExpanded;
            currentState.Focus = isExpanded ? ShellRegionFocus.Calendar : ShellRegionFocus.Stage;
            ApplyShellState();
        }

        public void SetChatVisible(bool isVisible)
        {
            currentState.ChatVisible = isVisible;
            currentState.Focus = isVisible ? ShellRegionFocus.Chat : ShellRegionFocus.Stage;
            ApplyShellState();
        }

        public void SetSettingsOpen(bool isOpen)
        {
            currentState.SettingsOpen = isOpen;
            currentState.Focus = isOpen ? ShellRegionFocus.Settings : ShellRegionFocus.Stage;
            ApplyShellState();
        }

        public void RequestRefresh()
        {
            RefreshRequested?.Invoke();
        }

        private void ApplyShellState()
        {
            SetDisplay(shell.CalendarSheetHost, DisplayStyle.Flex);
            SetDisplay(shell.ChatPanelHost, currentState.ChatVisible ? DisplayStyle.Flex : DisplayStyle.None);
            SetDisplay(shell.SettingsDrawer, currentState.SettingsOpen ? DisplayStyle.Flex : DisplayStyle.None);
            SetDisplay(shell.SettingsScrim, currentState.SettingsOpen ? DisplayStyle.Flex : DisplayStyle.None);

            SetToggleVisual(shell.FocusStageButton, currentState.Focus == ShellRegionFocus.Stage);
            SetToggleVisual(shell.ToggleCalendarButton, currentState.CalendarExpanded);
            SetToggleVisual(shell.ToggleChatButton, currentState.ChatVisible);
            SetToggleVisual(shell.ToggleSettingsButton, currentState.SettingsOpen);

            SetClass(shell.CalendarSheetHost, "calendar-sheet-expanded", currentState.CalendarExpanded);
            SetClass(shell.CalendarSheetHost, "calendar-sheet-collapsed", !currentState.CalendarExpanded);
            SetClass(shell.SettingsDrawer, "settings-drawer-open", currentState.SettingsOpen);

            StateChanged?.Invoke(currentState);
        }

        private void SetRuntimeStatus(string text, Color color)
        {
            if (shell.TopStatusChipLabel == null)
            {
                return;
            }

            shell.TopStatusChipLabel.text = text;
            shell.TopStatusChipLabel.style.color = new StyleColor(color);
        }

        private static void SetDisplay(VisualElement element, DisplayStyle display)
        {
            if (element != null)
            {
                element.style.display = display;
            }
        }

        private static void SetToggleVisual(Button button, bool isActive)
        {
            if (button == null)
            {
                return;
            }

            SetClass(button, "active", isActive);
        }

        private static void SetClass(VisualElement element, string className, bool isActive)
        {
            if (element == null || string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            if (isActive)
            {
                element.AddToClassList(className);
            }
            else
            {
                element.RemoveFromClassList(className);
            }
        }
    }
}
