using System;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.Features.Home
{
    public sealed class HomeModule : IHomeModule
    {
        private readonly HomeScreenController screenController;
        private bool isBound;

        public HomeModule(HomeScreenRefs refs)
            : this(new HomeScreenController(refs))
        {
        }

        public HomeModule(HomeScreenController screenController)
        {
            this.screenController = screenController ?? throw new ArgumentNullException(nameof(screenController));
        }

        public event Action<string> QuickAddRequested;

        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            isBound = true;
            screenController.Bind();
            screenController.QuickAddRequested += HandleQuickAddRequested;
        }

        public void Render(IPlannerTaskSnapshotSource taskStore) => screenController.Render(taskStore);
        public void RenderAssistantOrbit(ChatPanelSnapshot snapshot) => screenController.RenderAssistantOrbit(snapshot);
        public void SetTaskActionsEnabled(bool isEnabled) => screenController.SetTaskActionsEnabled(isEnabled);
        public void SetQuickAddStatus(string message, Color color) => screenController.SetQuickAddStatus(message, color);
        public void RenderStage(AvatarState avatarState) => screenController.RenderStage(avatarState);

        private void HandleQuickAddRequested(string text) => QuickAddRequested?.Invoke(text);
    }
}
