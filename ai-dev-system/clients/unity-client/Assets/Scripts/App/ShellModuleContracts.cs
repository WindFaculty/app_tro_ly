using System;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.App
{
    public enum ShellRegionFocus
    {
        Stage,
        Calendar,
        Chat,
        Settings,
    }

    public sealed class AppShellState
    {
        public bool CalendarExpanded;
        public bool ChatVisible = true;
        public bool SettingsOpen;
        public ShellRegionFocus Focus = ShellRegionFocus.Stage;
    }

    public interface IShellModule
    {
        event Action RefreshRequested;

        AppShellState CurrentState { get; }

        void Bind();
        void FocusStage();
        void ToggleCalendar();
        void ToggleChat();
        void ToggleSettings();
        void CloseSettings();
        void SetCalendarExpanded(bool isExpanded);
        void SetChatVisible(bool isVisible);
        void SetSettingsOpen(bool isOpen);
        void RenderBootState(string bannerText, string detailText, Color accentColor);
        void RenderHealth(HealthResponse health);
        void RenderStage(ShellStageSnapshot snapshot);
        void SetRefreshEnabled(bool isEnabled);
    }
}
