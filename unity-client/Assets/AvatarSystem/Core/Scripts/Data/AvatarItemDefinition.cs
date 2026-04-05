using UnityEngine;

namespace AvatarSystem.Data
{
    /// <summary>
    /// Defines a single equippable item (hair, clothing, accessory, etc.).
    /// Create instances via Assets > Create > AvatarSystem > Item Definition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "AvatarSystem/Item Definition")]
    public sealed class AvatarItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        public SlotType slotType;
        [Tooltip("Registry-level base avatar version this item expects, for example v001.")]
        public string requiredBaseVersion = "v001";
        [Tooltip("Logical body type identifier for compatibility checks, for example base or petite.")]
        public string bodyTypeId = "base";

        [Header("Visuals")]
        public GameObject prefab;
        public Material[] materialSet;
        public Sprite thumbnail;

        [Header("Slot Rules")]
        [Tooltip("Additional slots this item occupies beyond its own slotType.")]
        public SlotType[] occupiesSlots;

        [Tooltip("Slots that must be unequipped when this item is worn.")]
        public SlotType[] blocksSlots;

        [Tooltip("Slots that must already be equipped for this item to be valid.")]
        public SlotType[] requiresSlots;

        [Header("Tag Compatibility")]
        [Tooltip("Tags this item carries (e.g., long-sleeve, high-collar).")]
        public string[] compatibleTags;

        [Tooltip("Tags on other items that conflict with this one.")]
        public string[] incompatibleTags;

        [Header("Body Visibility")]
        [Tooltip("Body regions hidden when this item is equipped.")]
        public BodyRegion[] hideBodyRegions;

        [Header("Attachment")]
        public AnchorType anchorType;
        [Tooltip("Bone name for BoneAttach anchor type.")]
        public string anchorBoneName;
    }
}
