using System;
using UnityEngine;

namespace LocalAssistant.World.Objects
{
    [Serializable]
    public sealed class RoomObjectDefinition
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public RoomObjectCategory Category = RoomObjectCategory.Decor;
        public string PrefabKey = string.Empty;
        public string PrefabPath = string.Empty;
        public string AnchorType = string.Empty;
        public RoomInteractionType InteractionType = RoomInteractionType.None;
        public RoomObjectShapeKind ShapeKind = RoomObjectShapeKind.Block;
        public Vector3 DefaultScale = Vector3.one;
        public bool Selectable;
        public bool Hoverable;
        public bool Inspectable;
        public string[] Tags = Array.Empty<string>();
        public string[] OptionalStates = Array.Empty<string>();
        public Color BaseColor = Color.white;
        public Color AccentColor = new(0.18f, 0.22f, 0.26f, 1f);
    }
}
