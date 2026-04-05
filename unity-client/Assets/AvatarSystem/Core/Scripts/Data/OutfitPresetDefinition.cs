using UnityEngine;

namespace AvatarSystem.Data
{
    /// <summary>
    /// A complete outfit preset — one item reference per slot.
    /// Apply with AvatarPresetManager to equip an entire look at once.
    /// </summary>
    [CreateAssetMenu(fileName = "NewOutfitPreset", menuName = "AvatarSystem/Outfit Preset")]
    public sealed class OutfitPresetDefinition : ScriptableObject
    {
        public string presetId;
        public string presetName;
        public string requiredBaseVersion = "v001";
        public Sprite thumbnail;

        [Header("Slot Assignments")]
        public AvatarItemDefinition hair;
        public AvatarItemDefinition hairAccessory;
        public AvatarItemDefinition top;
        public AvatarItemDefinition bottom;
        public AvatarItemDefinition dress;
        public AvatarItemDefinition socks;
        public AvatarItemDefinition shoes;
        public AvatarItemDefinition gloves;
        public AvatarItemDefinition braceletL;
        public AvatarItemDefinition braceletR;

        /// <summary>
        /// Returns the item assigned to a given slot, or null if empty.
        /// </summary>
        public AvatarItemDefinition GetItemForSlot(SlotType slot)
        {
            return slot switch
            {
                SlotType.Hair => hair,
                SlotType.HairAccessory => hairAccessory,
                SlotType.Top => top,
                SlotType.Bottom => bottom,
                SlotType.Dress => dress,
                SlotType.Socks => socks,
                SlotType.Shoes => shoes,
                SlotType.Gloves => gloves,
                SlotType.BraceletL => braceletL,
                SlotType.BraceletR => braceletR,
                _ => null,
            };
        }
    }
}
