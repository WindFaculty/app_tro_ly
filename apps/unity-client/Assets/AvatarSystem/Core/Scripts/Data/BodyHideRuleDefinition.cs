using UnityEngine;

namespace AvatarSystem.Data
{
    /// <summary>
    /// Standalone body-hide rule asset. Useful for defining hide rules
    /// that apply to categories of items rather than individual items.
    /// Per-item hide rules live on AvatarItemDefinition.hideBodyRegions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBodyHideRule", menuName = "AvatarSystem/Body Hide Rule")]
    public sealed class BodyHideRuleDefinition : ScriptableObject
    {
        [Tooltip("The slot type this rule applies to.")]
        public SlotType slot;

        [Tooltip("Tags on items that activate this rule.")]
        public string[] activatingTags;

        [Tooltip("Body regions to hide when rule is active.")]
        public BodyRegion[] regionsToHide;

        [TextArea]
        public string description;
    }
}
