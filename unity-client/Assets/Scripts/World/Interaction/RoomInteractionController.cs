using System;
using LocalAssistant.World.Objects;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LocalAssistant.World.Interaction
{
    public sealed class RoomInteractionController : MonoBehaviour
    {
        private Camera stageCamera;
        private Func<Rect> viewportRectProvider;
        private InteractableObject hoveredObject;
        private InteractableObject selectedObject;
        private Vector3 defaultCameraPosition;
        private Quaternion defaultCameraRotation;

        public SelectedRoomObjectStore SelectedObjectStore { get; } = new();
        public RoomObjectSelectionSnapshot CurrentSelection => SelectedObjectStore.Current;
        public bool HasFocusTarget => selectedObject != null;
        public Vector3 CurrentFocusPoint => selectedObject != null ? selectedObject.FocusPoint : Vector3.zero;

        public event Action<RoomObjectSelectionSnapshot> SelectionChanged;

        private void Awake()
        {
            SelectedObjectStore.Changed += HandleSelectionChanged;
        }

        private void OnDestroy()
        {
            SelectedObjectStore.Changed -= HandleSelectionChanged;
        }

        public void Bind(Camera camera, Func<Rect> viewportProvider = null)
        {
            stageCamera = camera;
            viewportRectProvider = viewportProvider;
            SelectedObjectStore.Changed -= HandleSelectionChanged;
            SelectedObjectStore.Changed += HandleSelectionChanged;
            if (stageCamera != null)
            {
                defaultCameraPosition = stageCamera.transform.position;
                defaultCameraRotation = stageCamera.transform.rotation;
            }

            SelectedObjectStore.Set(RoomObjectSelectionSnapshot.None);
        }

        public void Update()
        {
            if (!Application.isPlaying || stageCamera == null)
            {
                return;
            }

            var pointerState = ReadPointerState();
            if (!pointerState.HasValue)
            {
                return;
            }

            ProcessPointer(pointerState.Value.Position, pointerState.Value.PressedThisFrame);
        }

        public void ProcessPointer(Vector2 screenPoint, bool pressed)
        {
            var ray = stageCamera != null
                ? stageCamera.ScreenPointToRay(screenPoint)
                : default;
            ProcessRay(ray, pressed, IsInsideViewport(screenPoint));
        }

        public void ProcessRay(Ray ray, bool pressed, bool isInsideViewport = true)
        {
            if (stageCamera == null)
            {
                return;
            }

            if (!isInsideViewport)
            {
                SetHoveredObject(null);
                return;
            }

            var hitObject = TryResolveInteractable(ray);
            SetHoveredObject(hitObject);

            if (!pressed)
            {
                return;
            }

            if (hitObject != null && hitObject.SupportsSelection)
            {
                SetSelectedObject(hitObject);
                return;
            }

            ClearSelection();
        }

        public void ClearSelection()
        {
            SetSelectedObject(null);
        }

        private void SetHoveredObject(InteractableObject next)
        {
            if (hoveredObject == next)
            {
                return;
            }

            if (hoveredObject != null)
            {
                hoveredObject.SetHovered(false);
            }

            hoveredObject = next;
            hoveredObject?.SetHovered(true);
        }

        private void SetSelectedObject(InteractableObject next)
        {
            if (selectedObject == next)
            {
                SelectedObjectStore.Set(selectedObject?.CreateSnapshot() ?? RoomObjectSelectionSnapshot.None);
                RefreshCameraFocus();
                return;
            }

            if (selectedObject != null)
            {
                selectedObject.SetSelected(false);
            }

            selectedObject = next;
            selectedObject?.SetSelected(true);
            SelectedObjectStore.Set(selectedObject?.CreateSnapshot() ?? RoomObjectSelectionSnapshot.None);
            RefreshCameraFocus();
        }

        private void RefreshCameraFocus()
        {
            if (stageCamera == null)
            {
                return;
            }

            stageCamera.transform.position = defaultCameraPosition;
            if (selectedObject == null || !ShouldFocusCamera(selectedObject.InteractionType))
            {
                stageCamera.transform.rotation = defaultCameraRotation;
                return;
            }

            var direction = selectedObject.FocusPoint - defaultCameraPosition;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                stageCamera.transform.rotation = defaultCameraRotation;
                return;
            }

            stageCamera.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private InteractableObject TryResolveInteractable(Ray ray)
        {
            if (!Physics.Raycast(ray, out var hitInfo, 50f))
            {
                return null;
            }

            var interactable = hitInfo.collider.GetComponentInParent<InteractableObject>();
            if (interactable == null)
            {
                return null;
            }

            return interactable.SupportsHover || interactable.SupportsSelection ? interactable : null;
        }

        private bool IsInsideViewport(Vector2 screenPoint)
        {
            if (viewportRectProvider == null)
            {
                return true;
            }

            var rect = viewportRectProvider.Invoke();
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return true;
            }

            return rect.Contains(screenPoint);
        }

        private void HandleSelectionChanged(RoomObjectSelectionSnapshot snapshot)
        {
            SelectionChanged?.Invoke(snapshot ?? RoomObjectSelectionSnapshot.None);
        }

        private static bool ShouldFocusCamera(RoomInteractionType interactionType)
        {
            return interactionType == RoomInteractionType.Focus || interactionType == RoomInteractionType.InspectAndFocus;
        }

        private static PointerState? ReadPointerState()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return new PointerState(Mouse.current.position.ReadValue(), Mouse.current.leftButton.wasPressedThisFrame);
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return new PointerState(Input.mousePosition, Input.GetMouseButtonDown(0));
#else
            return null;
#endif
        }

        private readonly struct PointerState
        {
            public PointerState(Vector2 position, bool pressedThisFrame)
            {
                Position = position;
                PressedThisFrame = pressedThisFrame;
            }

            public Vector2 Position { get; }
            public bool PressedThisFrame { get; }
        }
    }
}
