using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class RoomRuntime : MonoBehaviour
    {
        private Camera sceneCamera;

        [SerializeField] private Transform overviewFocusPoint;
        [SerializeField] private Transform avatarFocusPoint;
        [SerializeField] private Transform deskFocusPoint;
        [SerializeField] private Transform wardrobeFocusPoint;

        public string CurrentFocus { get; private set; } = "overview";

        public void Bind(Camera cameraRef)
        {
            sceneCamera = cameraRef;
            SetFocusPreset(CurrentFocus);
        }

        public void SetFocusPreset(string focus)
        {
            CurrentFocus = string.IsNullOrWhiteSpace(focus) ? "overview" : focus.Trim().ToLowerInvariant();
            if (sceneCamera == null)
            {
                return;
            }

            var target = ResolveTarget(CurrentFocus);
            var size = ResolveOrthoSize(CurrentFocus);
            sceneCamera.orthographicSize = size;

            if (target != null)
            {
                var currentPosition = sceneCamera.transform.position;
                sceneCamera.transform.position = new Vector3(target.position.x, target.position.y, currentPosition.z);
            }
        }

        private Transform ResolveTarget(string focus)
        {
            return focus switch
            {
                "avatar" or "chat" => avatarFocusPoint,
                "desk" or "planner" => deskFocusPoint,
                "wardrobe" => wardrobeFocusPoint,
                _ => overviewFocusPoint,
            };
        }

        private static float ResolveOrthoSize(string focus)
        {
            return focus switch
            {
                "avatar" or "chat" => 3.6f,
                "desk" or "planner" => 4.2f,
                "wardrobe" => 3.2f,
                _ => 5.2f,
            };
        }
    }
}
