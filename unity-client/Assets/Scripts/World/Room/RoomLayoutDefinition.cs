using System;
using LocalAssistant.World.Objects;
using UnityEngine;

namespace LocalAssistant.World.Room
{
    [Serializable]
    public sealed class RoomLayoutDefinition
    {
        public string LayoutId = "room_base";
        public string DisplayName = "Character Space Foundation";
        public string TemplateResourcePath = "World/Rooms/Room_Base";
        public Vector3 RoomSize = new(8.4f, 3.4f, 7.6f);
        public float WallThickness = 0.14f;
        public bool IncludeCeiling;
        public Vector3 AvatarSpawnPosition = new(0f, 0f, 1.35f);
        public Vector3 AvatarFacingEuler = new(0f, 180f, 0f);
        public Vector3 CameraAnchorPosition = new(0f, 1.85f, -6.2f);
        public Vector3 CameraLookAt = new(0f, 1.25f, 0.35f);
        public Vector3 DeskAnchorPosition = new(1.85f, 0f, -1.7f);
        public Vector3 RestAnchorPosition = new(-1.95f, 0f, 0.7f);
        public Vector3 DecorAnchorPosition = new(2.7f, 0f, 1.9f);
        public Color BackgroundColor = new(0.08f, 0.11f, 0.15f, 1f);
        public Color FloorColor = new(0.44f, 0.34f, 0.25f, 1f);
        public Color WallColor = new(0.85f, 0.86f, 0.88f, 1f);
        public Color CeilingColor = new(0.92f, 0.93f, 0.95f, 1f);
        public Color AccentColor = new(0.32f, 0.51f, 0.58f, 1f);
        public Color FurnitureColor = new(0.57f, 0.43f, 0.31f, 1f);
        public Color SoftFurnitureColor = new(0.38f, 0.47f, 0.54f, 1f);
        public RoomObjectPlacement[] ObjectPlacements = Array.Empty<RoomObjectPlacement>();

        public static RoomLayoutDefinition CreateDefault()
        {
            return new RoomLayoutDefinition
            {
                ObjectPlacements = new[]
                {
                    new RoomObjectPlacement
                    {
                        ObjectId = "desk_main_01",
                        InstanceName = "DeskMain",
                        AnchorType = RoomAnchorTypes.Desk,
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "laptop_work_01",
                        InstanceName = "LaptopWork",
                        AnchorType = RoomAnchorTypes.Desk,
                        LocalPosition = new Vector3(0.18f, 0.88f, -0.05f),
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "chair_task_01",
                        InstanceName = "ChairTask",
                        AnchorType = RoomAnchorTypes.Desk,
                        LocalPosition = new Vector3(0f, 0f, 0.82f),
                        LocalEulerAngles = new Vector3(0f, 180f, 0f),
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "sofa_rest_01",
                        InstanceName = "SofaRest",
                        AnchorType = RoomAnchorTypes.Rest,
                        LocalEulerAngles = new Vector3(0f, 22f, 0f),
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "side_table_rest_01",
                        InstanceName = "RestSideTable",
                        AnchorType = RoomAnchorTypes.Rest,
                        LocalPosition = new Vector3(1.2f, 0f, 0.12f),
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "shelf_books_01",
                        InstanceName = "ShelfBooks",
                        AnchorType = RoomAnchorTypes.Decor,
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "books_display_01",
                        InstanceName = "BooksDisplay",
                        AnchorType = RoomAnchorTypes.Decor,
                        LocalPosition = new Vector3(0f, 1.58f, 0.02f),
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "plant_corner_01",
                        InstanceName = "PlantCorner",
                        AnchorType = RoomAnchorTypes.Decor,
                        LocalPosition = new Vector3(-1.2f, 0f, 0.2f),
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "lamp_table_01",
                        InstanceName = "LampTable",
                        AnchorType = RoomAnchorTypes.Decor,
                        LocalPosition = new Vector3(1.05f, 0f, -0.1f),
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "art_wall_01",
                        InstanceName = "WallArt",
                        AnchorType = RoomAnchorTypes.Decor,
                        LocalPosition = new Vector3(-0.1f, 0f, 1.3f),
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "cabinet_storage_01",
                        InstanceName = "StorageCabinet",
                        AnchorType = RoomAnchorTypes.Decor,
                        LocalPosition = new Vector3(-2.05f, 0f, -0.18f),
                    },
                    new RoomObjectPlacement
                    {
                        ObjectId = "storage_box_01",
                        InstanceName = "StorageBox",
                        AnchorType = RoomAnchorTypes.Decor,
                        LocalPosition = new Vector3(-2.05f, 1.32f, -0.12f),
                    },
                },
            };
        }
    }
}
