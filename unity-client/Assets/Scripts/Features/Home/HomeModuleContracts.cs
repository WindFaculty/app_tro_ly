using System;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using LocalAssistant.World.Interaction;
using UnityEngine;

namespace LocalAssistant.Features.Home
{
    public enum HomeRoomAction
    {
        GoTo,
        Inspect,
        Use,
        ReturnToAvatar,
        ToggleHotspots,
    }

    public sealed class HomeRoomOverlayState
    {
        public static HomeRoomOverlayState Default => new()
        {
            ActivityTitle = "Avatar hold",
            ActivityDetail = "Select a highlighted room object to reveal contextual room actions.",
            ModeLabel = "ROOM READY",
            HotspotButtonText = "Hide hotspots",
            ReturnEnabled = true,
            ToggleHotspotsEnabled = true,
        };

        public string ActivityTitle = string.Empty;
        public string ActivityDetail = string.Empty;
        public string ModeLabel = string.Empty;
        public string HotspotButtonText = string.Empty;
        public bool GoToEnabled;
        public bool InspectEnabled;
        public bool UseEnabled;
        public bool ReturnEnabled;
        public bool ToggleHotspotsEnabled;
    }

    public interface IHomeModule
    {
        event Action<string> QuickAddRequested;
        event Action<HomeRoomAction> RoomActionRequested;

        void Bind();
        void Render(IPlannerTaskSnapshotSource taskStore);
        void RenderAssistantOrbit(ChatPanelSnapshot snapshot);
        void SetTaskActionsEnabled(bool isEnabled);
        void SetQuickAddStatus(string message, Color color);
        void RenderStage(AvatarState avatarState);
        void RenderSelectedRoomObject(RoomObjectSelectionSnapshot snapshot);
        void RenderRoomOverlayState(HomeRoomOverlayState state);
    }
}
