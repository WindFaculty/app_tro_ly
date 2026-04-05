using System;
using System.Collections.Generic;
using LocalAssistant.World.Room;
using UnityEngine;

namespace LocalAssistant.World.Objects
{
    public sealed class RoomObjectRegistry
    {
        private readonly Dictionary<string, RoomObjectDefinition> definitionsById = new(StringComparer.Ordinal);

        public IReadOnlyCollection<RoomObjectDefinition> Definitions => definitionsById.Values;

        public RoomObjectRegistry(IEnumerable<RoomObjectDefinition> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                {
                    continue;
                }

                definitionsById[definition.Id] = definition;
            }
        }

        public bool TryGetDefinition(string id, out RoomObjectDefinition definition)
        {
            return definitionsById.TryGetValue(id ?? string.Empty, out definition);
        }

        public static RoomObjectRegistry CreateFoundationMvp(RoomLayoutDefinition layout)
        {
            layout ??= RoomLayoutDefinition.CreateDefault();
            return new RoomObjectRegistry(new[]
            {
                new RoomObjectDefinition
                {
                    Id = "desk_main_01",
                    DisplayName = "Main Desk",
                    Category = RoomObjectCategory.Furniture,
                    PrefabKey = "OBJ_Furniture_Desk_Wood_A01",
                    PrefabPath = "Assets/World/Prefabs/Furniture/OBJ_Furniture_Desk_Wood_A01.prefab",
                    AnchorType = RoomAnchorTypes.Desk,
                    InteractionType = RoomInteractionType.Inspect,
                    ShapeKind = RoomObjectShapeKind.Desk,
                    DefaultScale = Vector3.one,
                    Selectable = true,
                    Hoverable = true,
                    Inspectable = true,
                    Tags = new[] { "work", "desk", "main" },
                    BaseColor = layout.FurnitureColor,
                    AccentColor = new Color(0.20f, 0.18f, 0.16f, 1f),
                },
                new RoomObjectDefinition
                {
                    Id = "laptop_work_01",
                    DisplayName = "Work Laptop",
                    Category = RoomObjectCategory.Interactive,
                    PrefabKey = "OBJ_Interactive_Laptop_A01",
                    PrefabPath = "Assets/World/Prefabs/Interactive/OBJ_Interactive_Laptop_A01.prefab",
                    AnchorType = RoomAnchorTypes.Desk,
                    InteractionType = RoomInteractionType.InspectAndFocus,
                    ShapeKind = RoomObjectShapeKind.Laptop,
                    DefaultScale = Vector3.one,
                    Selectable = true,
                    Hoverable = true,
                    Inspectable = true,
                    Tags = new[] { "work", "laptop", "interactive" },
                    BaseColor = new Color(0.17f, 0.18f, 0.20f, 1f),
                    AccentColor = new Color(0.48f, 0.78f, 0.90f, 1f),
                },
                new RoomObjectDefinition
                {
                    Id = "chair_task_01",
                    DisplayName = "Desk Chair",
                    Category = RoomObjectCategory.Furniture,
                    PrefabKey = "OBJ_Furniture_Chair_Task_A01",
                    PrefabPath = "Assets/World/Prefabs/Furniture/OBJ_Furniture_Chair_Task_A01.prefab",
                    AnchorType = RoomAnchorTypes.Desk,
                    InteractionType = RoomInteractionType.Focus,
                    ShapeKind = RoomObjectShapeKind.Chair,
                    DefaultScale = Vector3.one,
                    Selectable = true,
                    Hoverable = true,
                    Tags = new[] { "chair", "desk", "seat" },
                    BaseColor = layout.SoftFurnitureColor,
                    AccentColor = layout.FurnitureColor,
                },
                new RoomObjectDefinition
                {
                    Id = "sofa_rest_01",
                    DisplayName = "Rest Sofa",
                    Category = RoomObjectCategory.Furniture,
                    PrefabKey = "OBJ_Furniture_Sofa_Rest_A01",
                    PrefabPath = "Assets/World/Prefabs/Furniture/OBJ_Furniture_Sofa_Rest_A01.prefab",
                    AnchorType = RoomAnchorTypes.Rest,
                    InteractionType = RoomInteractionType.Focus,
                    ShapeKind = RoomObjectShapeKind.Sofa,
                    DefaultScale = Vector3.one,
                    Selectable = true,
                    Hoverable = true,
                    Tags = new[] { "rest", "sofa", "seat" },
                    BaseColor = layout.SoftFurnitureColor,
                    AccentColor = new Color(0.26f, 0.31f, 0.36f, 1f),
                },
                new RoomObjectDefinition
                {
                    Id = "side_table_rest_01",
                    DisplayName = "Side Table",
                    Category = RoomObjectCategory.Furniture,
                    PrefabKey = "OBJ_Furniture_SideTable_Rest_A01",
                    PrefabPath = "Assets/World/Prefabs/Furniture/OBJ_Furniture_SideTable_Rest_A01.prefab",
                    AnchorType = RoomAnchorTypes.Rest,
                    InteractionType = RoomInteractionType.None,
                    ShapeKind = RoomObjectShapeKind.SideTable,
                    DefaultScale = Vector3.one,
                    Tags = new[] { "table", "side", "rest" },
                    BaseColor = layout.FurnitureColor,
                    AccentColor = new Color(0.24f, 0.21f, 0.18f, 1f),
                },
                new RoomObjectDefinition
                {
                    Id = "shelf_books_01",
                    DisplayName = "Book Shelf",
                    Category = RoomObjectCategory.Furniture,
                    PrefabKey = "OBJ_Furniture_Shelf_Books_A01",
                    PrefabPath = "Assets/World/Prefabs/Furniture/OBJ_Furniture_Shelf_Books_A01.prefab",
                    AnchorType = RoomAnchorTypes.Decor,
                    InteractionType = RoomInteractionType.Inspect,
                    ShapeKind = RoomObjectShapeKind.Shelf,
                    DefaultScale = Vector3.one,
                    Selectable = true,
                    Hoverable = true,
                    Inspectable = true,
                    Tags = new[] { "shelf", "books", "storage" },
                    BaseColor = layout.FurnitureColor,
                    AccentColor = new Color(0.77f, 0.71f, 0.58f, 1f),
                },
                new RoomObjectDefinition
                {
                    Id = "books_display_01",
                    DisplayName = "Book Stack",
                    Category = RoomObjectCategory.Decor,
                    PrefabKey = "OBJ_Decor_Books_Display_A01",
                    PrefabPath = "Assets/World/Prefabs/Decor/OBJ_Decor_Books_Display_A01.prefab",
                    AnchorType = RoomAnchorTypes.Decor,
                    InteractionType = RoomInteractionType.None,
                    ShapeKind = RoomObjectShapeKind.Books,
                    DefaultScale = Vector3.one,
                    Tags = new[] { "books", "decor", "display" },
                    BaseColor = new Color(0.73f, 0.66f, 0.49f, 1f),
                    AccentColor = new Color(0.31f, 0.38f, 0.48f, 1f),
                },
                new RoomObjectDefinition
                {
                    Id = "plant_corner_01",
                    DisplayName = "Corner Plant",
                    Category = RoomObjectCategory.Decor,
                    PrefabKey = "OBJ_Decor_Plant_Small_A01",
                    PrefabPath = "Assets/World/Prefabs/Decor/OBJ_Decor_Plant_Small_A01.prefab",
                    AnchorType = RoomAnchorTypes.Decor,
                    InteractionType = RoomInteractionType.Inspect,
                    ShapeKind = RoomObjectShapeKind.Plant,
                    DefaultScale = Vector3.one,
                    Selectable = true,
                    Hoverable = true,
                    Inspectable = true,
                    Tags = new[] { "plant", "decor", "green" },
                    BaseColor = new Color(0.36f, 0.59f, 0.41f, 1f),
                    AccentColor = layout.FurnitureColor,
                },
                new RoomObjectDefinition
                {
                    Id = "lamp_table_01",
                    DisplayName = "Warm Lamp",
                    Category = RoomObjectCategory.Lighting,
                    PrefabKey = "OBJ_Lighting_Lamp_Table_A01",
                    PrefabPath = "Assets/World/Prefabs/Lighting/OBJ_Lighting_Lamp_Table_A01.prefab",
                    AnchorType = RoomAnchorTypes.Decor,
                    InteractionType = RoomInteractionType.Focus,
                    ShapeKind = RoomObjectShapeKind.Lamp,
                    DefaultScale = Vector3.one,
                    Selectable = true,
                    Hoverable = true,
                    Tags = new[] { "lamp", "light", "decor" },
                    BaseColor = layout.FurnitureColor,
                    AccentColor = new Color(0.93f, 0.86f, 0.68f, 1f),
                },
                new RoomObjectDefinition
                {
                    Id = "art_wall_01",
                    DisplayName = "Wall Art",
                    Category = RoomObjectCategory.Decor,
                    PrefabKey = "OBJ_Decor_WallArt_A01",
                    PrefabPath = "Assets/World/Prefabs/Decor/OBJ_Decor_WallArt_A01.prefab",
                    AnchorType = RoomAnchorTypes.Decor,
                    InteractionType = RoomInteractionType.Inspect,
                    ShapeKind = RoomObjectShapeKind.WallArt,
                    DefaultScale = Vector3.one,
                    Selectable = true,
                    Hoverable = true,
                    Inspectable = true,
                    Tags = new[] { "art", "wall", "decor" },
                    BaseColor = new Color(0.72f, 0.52f, 0.42f, 1f),
                    AccentColor = new Color(0.22f, 0.33f, 0.40f, 1f),
                },
                new RoomObjectDefinition
                {
                    Id = "cabinet_storage_01",
                    DisplayName = "Storage Cabinet",
                    Category = RoomObjectCategory.Utility,
                    PrefabKey = "OBJ_Utility_Cabinet_Storage_A01",
                    PrefabPath = "Assets/World/Prefabs/Utility/OBJ_Utility_Cabinet_Storage_A01.prefab",
                    AnchorType = RoomAnchorTypes.Decor,
                    InteractionType = RoomInteractionType.None,
                    ShapeKind = RoomObjectShapeKind.Cabinet,
                    DefaultScale = Vector3.one,
                    Tags = new[] { "cabinet", "storage", "utility" },
                    BaseColor = new Color(0.50f, 0.39f, 0.29f, 1f),
                    AccentColor = new Color(0.80f, 0.76f, 0.67f, 1f),
                },
                new RoomObjectDefinition
                {
                    Id = "storage_box_01",
                    DisplayName = "Storage Box",
                    Category = RoomObjectCategory.Decor,
                    PrefabKey = "OBJ_Decor_Box_Storage_A01",
                    PrefabPath = "Assets/World/Prefabs/Decor/OBJ_Decor_Box_Storage_A01.prefab",
                    AnchorType = RoomAnchorTypes.Decor,
                    InteractionType = RoomInteractionType.None,
                    ShapeKind = RoomObjectShapeKind.StorageBox,
                    DefaultScale = Vector3.one,
                    Tags = new[] { "box", "storage", "decor" },
                    BaseColor = new Color(0.74f, 0.65f, 0.52f, 1f),
                    AccentColor = new Color(0.44f, 0.33f, 0.25f, 1f),
                },
            });
        }
    }
}
