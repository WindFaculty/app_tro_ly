using LocalAssistant.World.Interaction;
using UnityEngine;

namespace LocalAssistant.World.Objects
{
    public sealed class RoomObjectFactory
    {
        public GameObject Spawn(RoomObjectDefinition definition, RoomObjectPlacement placement, Transform anchor)
        {
            if (definition == null || anchor == null)
            {
                return null;
            }

            var instance = new GameObject(string.IsNullOrWhiteSpace(placement?.InstanceName) ? definition.Id : placement.InstanceName);
            instance.transform.SetParent(anchor, false);
            instance.transform.localPosition = placement?.LocalPosition ?? Vector3.zero;
            instance.transform.localEulerAngles = placement?.LocalEulerAngles ?? Vector3.zero;

            BuildShape(definition, instance.transform);
            var scale = placement != null && placement.UseScaleOverride
                ? placement.ScaleOverride
                : definition.DefaultScale;
            instance.transform.localScale = scale == Vector3.zero ? Vector3.one : scale;
            TryAttachInteractable(definition, placement, instance);
            return instance;
        }

        private void BuildShape(RoomObjectDefinition definition, Transform parent)
        {
            switch (definition.ShapeKind)
            {
                case RoomObjectShapeKind.Desk:
                    BuildDesk(definition, parent);
                    break;
                case RoomObjectShapeKind.Chair:
                    BuildChair(definition, parent);
                    break;
                case RoomObjectShapeKind.Laptop:
                    BuildLaptop(definition, parent);
                    break;
                case RoomObjectShapeKind.Sofa:
                    BuildSofa(definition, parent);
                    break;
                case RoomObjectShapeKind.SideTable:
                    BuildSideTable(definition, parent);
                    break;
                case RoomObjectShapeKind.Shelf:
                    BuildShelf(definition, parent);
                    break;
                case RoomObjectShapeKind.Books:
                    BuildBooks(definition, parent);
                    break;
                case RoomObjectShapeKind.Plant:
                    BuildPlant(definition, parent);
                    break;
                case RoomObjectShapeKind.Lamp:
                    BuildLamp(definition, parent);
                    break;
                case RoomObjectShapeKind.WallArt:
                    BuildWallArt(definition, parent);
                    break;
                case RoomObjectShapeKind.Cabinet:
                    BuildCabinet(definition, parent);
                    break;
                case RoomObjectShapeKind.StorageBox:
                    BuildStorageBox(definition, parent);
                    break;
                case RoomObjectShapeKind.Rug:
                    CreateBlock("Rug", parent, new Vector3(0f, 0.01f, 0f), new Vector3(1.2f, 0.02f, 0.8f), definition.BaseColor);
                    break;
                default:
                    CreateBlock("Block", parent, new Vector3(0f, 0.5f, 0f), Vector3.one, definition.BaseColor);
                    break;
            }
        }

        private void BuildDesk(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("DeskTop", parent, new Vector3(0f, 0.78f, 0f), new Vector3(1.45f, 0.09f, 0.65f), definition.BaseColor);
            CreateBlock("DeskLegA", parent, new Vector3(-0.58f, 0.37f, -0.22f), new Vector3(0.08f, 0.72f, 0.08f), definition.AccentColor);
            CreateBlock("DeskLegB", parent, new Vector3(0.58f, 0.37f, -0.22f), new Vector3(0.08f, 0.72f, 0.08f), definition.AccentColor);
            CreateBlock("DeskLegC", parent, new Vector3(-0.58f, 0.37f, 0.22f), new Vector3(0.08f, 0.72f, 0.08f), definition.AccentColor);
            CreateBlock("DeskLegD", parent, new Vector3(0.58f, 0.37f, 0.22f), new Vector3(0.08f, 0.72f, 0.08f), definition.AccentColor);
        }

        private void BuildChair(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("Seat", parent, new Vector3(0f, 0.46f, 0f), new Vector3(0.52f, 0.08f, 0.52f), definition.BaseColor);
            CreateBlock("Back", parent, new Vector3(0f, 0.82f, -0.24f), new Vector3(0.52f, 0.58f, 0.08f), definition.BaseColor);
            CreateBlock("LegA", parent, new Vector3(-0.18f, 0.22f, -0.18f), new Vector3(0.06f, 0.44f, 0.06f), definition.AccentColor);
            CreateBlock("LegB", parent, new Vector3(0.18f, 0.22f, -0.18f), new Vector3(0.06f, 0.44f, 0.06f), definition.AccentColor);
            CreateBlock("LegC", parent, new Vector3(-0.18f, 0.22f, 0.18f), new Vector3(0.06f, 0.44f, 0.06f), definition.AccentColor);
            CreateBlock("LegD", parent, new Vector3(0.18f, 0.22f, 0.18f), new Vector3(0.06f, 0.44f, 0.06f), definition.AccentColor);
        }

        private void BuildLaptop(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("LaptopBase", parent, new Vector3(0f, 0.03f, 0f), new Vector3(0.42f, 0.03f, 0.28f), definition.BaseColor);
            CreateBlock("LaptopScreen", parent, new Vector3(0f, 0.17f, -0.11f), new Vector3(0.40f, 0.25f, 0.03f), definition.AccentColor);
        }

        private void BuildSofa(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("Base", parent, new Vector3(0f, 0.42f, 0f), new Vector3(1.9f, 0.56f, 0.82f), definition.BaseColor);
            CreateBlock("Back", parent, new Vector3(0f, 0.9f, -0.3f), new Vector3(1.9f, 0.62f, 0.18f), definition.BaseColor);
            CreateBlock("ArmLeft", parent, new Vector3(-0.88f, 0.67f, 0f), new Vector3(0.18f, 0.5f, 0.82f), definition.AccentColor);
            CreateBlock("ArmRight", parent, new Vector3(0.88f, 0.67f, 0f), new Vector3(0.18f, 0.5f, 0.82f), definition.AccentColor);
        }

        private void BuildSideTable(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("Top", parent, new Vector3(0f, 0.58f, 0f), new Vector3(0.56f, 0.08f, 0.56f), definition.BaseColor);
            CreateBlock("LowerShelf", parent, new Vector3(0f, 0.24f, 0f), new Vector3(0.48f, 0.05f, 0.48f), definition.AccentColor);
            CreateBlock("LegA", parent, new Vector3(-0.2f, 0.29f, -0.2f), new Vector3(0.06f, 0.58f, 0.06f), definition.AccentColor);
            CreateBlock("LegB", parent, new Vector3(0.2f, 0.29f, -0.2f), new Vector3(0.06f, 0.58f, 0.06f), definition.AccentColor);
            CreateBlock("LegC", parent, new Vector3(-0.2f, 0.29f, 0.2f), new Vector3(0.06f, 0.58f, 0.06f), definition.AccentColor);
            CreateBlock("LegD", parent, new Vector3(0.2f, 0.29f, 0.2f), new Vector3(0.06f, 0.58f, 0.06f), definition.AccentColor);
        }

        private void BuildShelf(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("Frame", parent, new Vector3(0f, 1.05f, 0f), new Vector3(0.8f, 2.1f, 0.26f), definition.BaseColor);
            CreateBlock("ShelfA", parent, new Vector3(0f, 0.46f, 0.02f), new Vector3(0.72f, 0.05f, 0.22f), definition.AccentColor);
            CreateBlock("ShelfB", parent, new Vector3(0f, 0.98f, 0.02f), new Vector3(0.72f, 0.05f, 0.22f), definition.AccentColor);
            CreateBlock("ShelfC", parent, new Vector3(0f, 1.5f, 0.02f), new Vector3(0.72f, 0.05f, 0.22f), definition.AccentColor);
        }

        private void BuildBooks(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("BookA", parent, new Vector3(-0.16f, 0.12f, 0f), new Vector3(0.14f, 0.24f, 0.2f), definition.BaseColor);
            CreateBlock("BookB", parent, new Vector3(0f, 0.1f, 0f), new Vector3(0.12f, 0.2f, 0.22f), definition.AccentColor);
            CreateBlock("BookC", parent, new Vector3(0.16f, 0.14f, 0f), new Vector3(0.14f, 0.28f, 0.18f), new Color(
                Mathf.Clamp01(definition.BaseColor.r + 0.14f),
                Mathf.Clamp01(definition.BaseColor.g + 0.08f),
                Mathf.Clamp01(definition.BaseColor.b - 0.06f),
                1f));
        }

        private void BuildPlant(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("Pot", parent, new Vector3(0f, 0.22f, 0f), new Vector3(0.36f, 0.42f, 0.36f), definition.AccentColor);
            CreateBlock("LeafCluster", parent, new Vector3(0f, 0.78f, 0f), new Vector3(0.78f, 0.94f, 0.78f), definition.BaseColor);
        }

        private void BuildLamp(RoomObjectDefinition definition, Transform parent)
        {
            CreateCylinder("Base", parent, new Vector3(0f, 0.58f, 0f), new Vector3(0.16f, 0.58f, 0.16f), definition.BaseColor);
            CreateBlock("Shade", parent, new Vector3(0f, 1.35f, 0f), new Vector3(0.56f, 0.3f, 0.56f), definition.AccentColor);
        }

        private void BuildWallArt(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("Frame", parent, new Vector3(0f, 1.65f, 0f), new Vector3(0.92f, 0.64f, 0.06f), definition.AccentColor);
            CreateBlock("Canvas", parent, new Vector3(0f, 1.65f, -0.01f), new Vector3(0.76f, 0.48f, 0.03f), definition.BaseColor);
        }

        private void BuildCabinet(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("Body", parent, new Vector3(0f, 0.65f, 0f), new Vector3(1.1f, 1.3f, 0.46f), definition.BaseColor);
            CreateBlock("DoorLeft", parent, new Vector3(-0.26f, 0.67f, 0.24f), new Vector3(0.46f, 1.08f, 0.04f), definition.AccentColor);
            CreateBlock("DoorRight", parent, new Vector3(0.26f, 0.67f, 0.24f), new Vector3(0.46f, 1.08f, 0.04f), definition.AccentColor);
            CreateBlock("HandleLeft", parent, new Vector3(-0.06f, 0.67f, 0.28f), new Vector3(0.04f, 0.22f, 0.03f), definition.BaseColor);
            CreateBlock("HandleRight", parent, new Vector3(0.06f, 0.67f, 0.28f), new Vector3(0.04f, 0.22f, 0.03f), definition.BaseColor);
            CreateBlock("FootLeft", parent, new Vector3(-0.34f, 0.08f, 0f), new Vector3(0.1f, 0.16f, 0.1f), definition.AccentColor);
            CreateBlock("FootRight", parent, new Vector3(0.34f, 0.08f, 0f), new Vector3(0.1f, 0.16f, 0.1f), definition.AccentColor);
        }

        private void BuildStorageBox(RoomObjectDefinition definition, Transform parent)
        {
            CreateBlock("BoxBase", parent, new Vector3(0f, 0.13f, 0f), new Vector3(0.42f, 0.26f, 0.32f), definition.BaseColor);
            CreateBlock("Lid", parent, new Vector3(0f, 0.29f, 0f), new Vector3(0.46f, 0.06f, 0.36f), definition.AccentColor);
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

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localScale = localScale;
            ApplyColor(cylinder, color);
            return cylinder;
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

        private static void TryAttachInteractable(RoomObjectDefinition definition, RoomObjectPlacement placement, GameObject instance)
        {
            if (definition == null || instance == null)
            {
                return;
            }

            var shouldAttach =
                definition.Selectable ||
                definition.Hoverable ||
                definition.Inspectable ||
                definition.InteractionType != RoomInteractionType.None;
            if (!shouldAttach)
            {
                return;
            }

            var interactable = instance.GetComponent<InteractableObject>() ?? instance.AddComponent<InteractableObject>();
            interactable.Initialize(definition, placement);
        }
    }
}
