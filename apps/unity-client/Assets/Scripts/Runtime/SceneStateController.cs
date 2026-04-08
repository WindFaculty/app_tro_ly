using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class SceneStateController : MonoBehaviour
    {
        private RoomRuntime roomRuntime;
        private AvatarRuntime avatarRuntime;

        public string CurrentPage { get; private set; } = "dashboard";

        public void Bind(RoomRuntime room, AvatarRuntime avatar)
        {
            roomRuntime = room;
            avatarRuntime = avatar;
        }

        public void ApplyPageContext(string page)
        {
            CurrentPage = string.IsNullOrWhiteSpace(page) ? "dashboard" : page.Trim().ToLowerInvariant();

            switch (CurrentPage)
            {
                case "chat":
                    roomRuntime?.SetFocusPreset("avatar");
                    avatarRuntime?.SetIdleState();
                    break;
                case "planner":
                    roomRuntime?.SetFocusPreset("desk");
                    avatarRuntime?.SetIdleState();
                    break;
                case "wardrobe":
                    roomRuntime?.SetFocusPreset("wardrobe");
                    avatarRuntime?.SetIdleState();
                    break;
                default:
                    roomRuntime?.SetFocusPreset("overview");
                    avatarRuntime?.SetIdleState();
                    break;
            }
        }
    }
}
