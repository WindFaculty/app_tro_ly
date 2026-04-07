using System;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.App
{
    public sealed class ShellModule : IShellModule
    {
        private readonly AppShellController shellController;
        private bool isBound;

        public ShellModule(AppShellRefs shell)
            : this(new AppShellController(shell))
        {
        }

        public ShellModule(AppShellController shellController)
        {
            this.shellController = shellController ?? throw new ArgumentNullException(nameof(shellController));
        }

        public event Action RefreshRequested;

        public AppShellState CurrentState => shellController.CurrentState;

        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            isBound = true;
            shellController.Bind();
            shellController.RefreshRequested += HandleRefreshRequested;
        }

        public void FocusStage() => shellController.FocusStage();
        public void ToggleCalendar() => shellController.ToggleCalendar();
        public void ToggleChat() => shellController.ToggleChat();
        public void ToggleSettings() => shellController.ToggleSettings();
        public void CloseSettings() => shellController.CloseSettings();
        public void SetCalendarExpanded(bool isExpanded) => shellController.SetCalendarExpanded(isExpanded);
        public void SetChatVisible(bool isVisible) => shellController.SetChatVisible(isVisible);
        public void SetSettingsOpen(bool isOpen) => shellController.SetSettingsOpen(isOpen);
        public void RenderBootState(string bannerText, string detailText, Color accentColor) => shellController.RenderBootState(bannerText, detailText, accentColor);
        public void RenderHealth(HealthResponse health) => shellController.RenderHealth(health);
        public void RenderStage(ShellStageSnapshot snapshot) => shellController.RenderStage(snapshot);
        public void SetRefreshEnabled(bool isEnabled) => shellController.SetRefreshEnabled(isEnabled);

        private void HandleRefreshRequested() => RefreshRequested?.Invoke();
    }
}
