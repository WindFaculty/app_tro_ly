using System.Collections.Generic;
using LocalAssistant.World.Objects;
using UnityEngine;

namespace LocalAssistant.World.Room
{
    public sealed class RoomWorldController : MonoBehaviour
    {
        private const string RoomRootName = "CharacterSpaceRoomRoot";
        private const string ShellGeometryName = "ShellGeometry";
        private const string AvatarSpawnPointName = "AvatarSpawnPoint";
        private const string CameraAnchorName = "RoomCameraAnchor";
        private const string DeskAnchorName = "DeskAnchor";
        private const string RestAnchorName = "RestAnchor";
        private const string DecorAnchorName = "DecorAnchor";

        private RoomLayoutDefinition currentLayout;
        private Transform roomRoot;
        private Transform shellGeometryRoot;
        private readonly RoomObjectFactory objectFactory = new();
        private readonly List<GameObject> hotspotMarkers = new();

        public string CurrentLayoutId => currentLayout?.LayoutId ?? string.Empty;
        public Transform AvatarSpawnPoint { get; private set; }
        public Transform CameraAnchor { get; private set; }
        public Transform DeskAnchor { get; private set; }
        public Transform RestAnchor { get; private set; }
        public Transform DecorAnchor { get; private set; }
        public bool HotspotsVisible { get; private set; } = true;

        public void BindRoomTemplate(Transform templateRoot)
        {
            roomRoot = templateRoot;
            shellGeometryRoot = null;
            AvatarSpawnPoint = null;
            CameraAnchor = null;
            DeskAnchor = null;
            RestAnchor = null;
            DecorAnchor = null;

            if (roomRoot == null)
            {
                return;
            }

            roomRoot.name = RoomRootName;
            ResolveHierarchyRoots();
        }

        public void Initialize(RoomLayoutDefinition layout)
        {
            currentLayout = layout ?? RoomLayoutDefinition.CreateDefault();
            EnsureRoomHierarchy();
            hotspotMarkers.Clear();
            ClearChildren(shellGeometryRoot);
            ClearChildren(AvatarSpawnPoint);
            ClearChildren(CameraAnchor);
            ClearChildren(DeskAnchor);
            ClearChildren(RestAnchor);
            ClearChildren(DecorAnchor);
            BuildFoundation(currentLayout);
        }

        public void ConfigureStageCamera(Camera stageCamera)
        {
            if (stageCamera == null)
            {
                return;
            }

            var layout = currentLayout ?? RoomLayoutDefinition.CreateDefault();
            stageCamera.orthographic = false;
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = layout.BackgroundColor;
            stageCamera.nearClipPlane = 0.05f;
            stageCamera.farClipPlane = 50f;
            stageCamera.fieldOfView = 42f;
            stageCamera.transform.position = layout.CameraAnchorPosition;
            stageCamera.transform.LookAt(layout.CameraLookAt);
        }

        private void EnsureRoomHierarchy()
        {
            if (roomRoot == null)
            {
                roomRoot = new GameObject(RoomRootName).transform;
                roomRoot.SetParent(transform, false);
            }
            else if (roomRoot.parent != transform)
            {
                roomRoot.SetParent(transform, false);
            }

            ResolveHierarchyRoots();
        }

        private void ResolveHierarchyRoots()
        {
            if (roomRoot == null)
            {
                return;
            }

            shellGeometryRoot = GetOrCreateChild(roomRoot, ShellGeometryName);
            AvatarSpawnPoint = GetOrCreateChild(roomRoot, AvatarSpawnPointName);
            CameraAnchor = GetOrCreateChild(roomRoot, CameraAnchorName);
            DeskAnchor = GetOrCreateChild(roomRoot, DeskAnchorName);
            RestAnchor = GetOrCreateChild(roomRoot, RestAnchorName);
            DecorAnchor = GetOrCreateChild(roomRoot, DecorAnchorName);
        }

        private void BuildFoundation(RoomLayoutDefinition layout)
        {
            var roomSize = layout.RoomSize;
            var wallThickness = layout.WallThickness;
            var halfWidth = roomSize.x * 0.5f;
            var halfDepth = roomSize.z * 0.5f;
            var wallHeight = roomSize.y;

            CreateBlock("Floor", shellGeometryRoot, new Vector3(0f, -0.05f, 0f), new Vector3(roomSize.x, 0.1f, roomSize.z), layout.FloorColor);
            CreateBlock("BackWall", shellGeometryRoot, new Vector3(0f, wallHeight * 0.5f, halfDepth), new Vector3(roomSize.x, wallHeight, wallThickness), layout.WallColor);
            CreateBlock("LeftWall", shellGeometryRoot, new Vector3(-halfWidth, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, roomSize.z), layout.WallColor);
            CreateBlock("RightWall", shellGeometryRoot, new Vector3(halfWidth, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, roomSize.z), layout.WallColor);
            if (layout.IncludeCeiling)
            {
                CreateBlock("Ceiling", shellGeometryRoot, new Vector3(0f, wallHeight, 0f), new Vector3(roomSize.x, 0.06f, roomSize.z), layout.CeilingColor);
            }

            CreateBlock("WindowPanel", shellGeometryRoot, new Vector3(0f, 2.05f, halfDepth - 0.02f), new Vector3(2.1f, 1.15f, 0.04f), new Color(0.73f, 0.84f, 0.92f, 1f));
            CreateBlock("Rug", shellGeometryRoot, new Vector3(0f, 0.01f, -0.1f), new Vector3(3.65f, 0.02f, 2.75f), layout.AccentColor);

            ConfigureAnchor(AvatarSpawnPoint, layout.AvatarSpawnPosition, layout.AvatarFacingEuler);
            ConfigureAnchor(CameraAnchor, layout.CameraAnchorPosition, Vector3.zero);
            ConfigureAnchor(DeskAnchor, layout.DeskAnchorPosition, Vector3.zero);
            ConfigureAnchor(RestAnchor, layout.RestAnchorPosition, Vector3.zero);
            ConfigureAnchor(DecorAnchor, layout.DecorAnchorPosition, Vector3.zero);

            SpawnRegistryObjects(layout);
            hotspotMarkers.Add(BuildAnchorMarker("AvatarSpawnMarker", AvatarSpawnPoint, layout.AccentColor, 0.18f, 0.02f));
            hotspotMarkers.Add(BuildAnchorMarker("DeskAnchorMarker", DeskAnchor, layout.AccentColor, 0.2f, 0.02f));
            hotspotMarkers.Add(BuildAnchorMarker("RestAnchorMarker", RestAnchor, layout.AccentColor, 0.2f, 0.02f));
            hotspotMarkers.Add(BuildAnchorMarker("DecorAnchorMarker", DecorAnchor, layout.AccentColor, 0.2f, 0.02f));
            SetHotspotsVisible(HotspotsVisible);
        }

        public void SetHotspotsVisible(bool isVisible)
        {
            HotspotsVisible = isVisible;
            for (var index = 0; index < hotspotMarkers.Count; index++)
            {
                if (hotspotMarkers[index] != null)
                {
                    hotspotMarkers[index].SetActive(isVisible);
                }
            }
        }

        public bool ToggleHotspotsVisible()
        {
            SetHotspotsVisible(!HotspotsVisible);
            return HotspotsVisible;
        }

        private void SpawnRegistryObjects(RoomLayoutDefinition layout)
        {
            var registry = RoomObjectRegistry.CreateFoundationMvp(layout);
            if (layout.ObjectPlacements == null)
            {
                return;
            }

            foreach (var placement in layout.ObjectPlacements)
            {
                if (placement == null || !registry.TryGetDefinition(placement.ObjectId, out var definition))
                {
                    continue;
                }

                var anchor = ResolveAnchor(placement.AnchorType, definition.AnchorType);
                objectFactory.Spawn(definition, placement, anchor);
            }
        }

        private static void ConfigureAnchor(Transform anchor, Vector3 localPosition, Vector3 localEulerAngles)
        {
            if (anchor == null)
            {
                return;
            }

            anchor.localPosition = localPosition;
            anchor.localEulerAngles = localEulerAngles;
        }

        private Transform ResolveAnchor(string placementAnchorType, string defaultAnchorType)
        {
            var anchorType = string.IsNullOrWhiteSpace(placementAnchorType) ? defaultAnchorType : placementAnchorType;
            return anchorType switch
            {
                RoomAnchorTypes.Desk => DeskAnchor,
                RoomAnchorTypes.Rest => RestAnchor,
                RoomAnchorTypes.Decor => DecorAnchor,
                RoomAnchorTypes.AvatarSpawn => AvatarSpawnPoint,
                _ => roomRoot,
            };
        }

        private static GameObject BuildAnchorMarker(string name, Transform parent, Color color, float radius, float height)
        {
            if (parent == null)
            {
                return null;
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            marker.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            ApplyColor(marker, color);
            if (marker.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            return marker;
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            ApplyColor(block, color);
            return block;
        }

        private static void ApplyColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            renderer.sharedMaterial = new Material(shader)
            {
                color = color,
            };
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                    continue;
                }

                DestroyImmediate(child);
            }
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            var child = parent.Find(childName);
            if (child != null)
            {
                child.name = childName;
                return child;
            }

            child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            return child;
        }
    }
}
