using System;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using LocalAssistant.World.Interaction;
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
        public event Action<HomeRoomAction> RoomActionRequested;

        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            isBound = true;
            screenController.Bind();
            screenController.QuickAddRequested += HandleQuickAddRequested;
            screenController.RoomActionRequested += HandleRoomActionRequested;
        }

        public void Render(IPlannerTaskSnapshotSource taskStore) => screenController.Render(taskStore);
        public void RenderAssistantOrbit(ChatPanelSnapshot snapshot) => screenController.RenderAssistantOrbit(snapshot);
        public void SetTaskActionsEnabled(bool isEnabled) => screenController.SetTaskActionsEnabled(isEnabled);
        public void SetQuickAddStatus(string message, Color color) => screenController.SetQuickAddStatus(message, color);
        public void RenderStage(AvatarState avatarState) => screenController.RenderStage(avatarState);
        public void RenderSelectedRoomObject(RoomObjectSelectionSnapshot snapshot) => screenController.RenderSelectedRoomObject(snapshot);
        public void RenderRoomOverlayState(HomeRoomOverlayState state) => screenController.RenderRoomOverlayState(state);

        private void HandleQuickAddRequested(string text) => QuickAddRequested?.Invoke(text);
        private void HandleRoomActionRequested(HomeRoomAction action) => RoomActionRequested?.Invoke(action);
    }
}
