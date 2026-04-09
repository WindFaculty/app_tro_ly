using UnityEngine;

namespace AvatarSystem.Data
{
    /// <summary>
    /// Global conflict rule evaluated when equipping items.
    /// Use for cross-slot rules that go beyond per-item BlocksSlots.
    /// </summary>
    [CreateAssetMenu(fileName = "NewConflictRule", menuName = "AvatarSystem/Conflict Rule")]
    public sealed class ConflictRuleDefinition : ScriptableObject
    {
        [Tooltip("The slot that triggers this rule when equipped.")]
        public SlotType sourceSlot;

        [Tooltip("Slots that must be unequipped when sourceSlot is filled.")]
        public SlotType[] blockedSlots;

        [Tooltip("Slots that must already be filled for sourceSlot to be equippable.")]
        public SlotType[] requiredSlots;

        [Tooltip("Tag strings on other items that are incompatible with the source.")]
        public string[] incompatibleTags;

        [TextArea]
        public string description;
    }
}
