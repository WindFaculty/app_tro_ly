using System;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.Features.Home
{
    public interface IHomeModule
    {
        event Action<string> QuickAddRequested;

        void Bind();
        void Render(IPlannerTaskSnapshotSource taskStore);
        void RenderAssistantOrbit(ChatPanelSnapshot snapshot);
        void SetTaskActionsEnabled(bool isEnabled);
        void SetQuickAddStatus(string message, Color color);
        void RenderStage(AvatarState avatarState);
    }
}
