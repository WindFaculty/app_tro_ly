using System;
using System.Collections.Generic;
using UnityEngine;

namespace AvatarSystem.Data
{
    /// <summary>
    /// Central registry for avatar items, presets, and rule assets that are allowed to
    /// participate in placeholder-safe customization flows.
    /// </summary>
    [CreateAssetMenu(fileName = "AvatarAssetRegistry", menuName = "AvatarSystem/Asset Registry")]
    public sealed class AvatarAssetRegistryDefinition : ScriptableObject
    {
        [Header("Registry Identity")]
        public string registryId = "default";
        public string displayName = "Default Avatar Registry";
        public string requiredBaseVersion = "v001";
        public string bodyTypeId = "base";

        [Header("Registered Assets")]
        public AvatarItemDefinition[] items;
        public ConflictRuleDefinition[] conflictRules;
        public OutfitPresetDefinition[] outfitPresets;

        public AvatarItemDefinition[] Items => items ?? Array.Empty<AvatarItemDefinition>();
        public ConflictRuleDefinition[] ConflictRules => conflictRules ?? Array.Empty<ConflictRuleDefinition>();
        public OutfitPresetDefinition[] OutfitPresets => outfitPresets ?? Array.Empty<OutfitPresetDefinition>();

        public AvatarItemDefinition[] GetItemsForSlot(SlotType slot)
        {
            var matches = new List<AvatarItemDefinition>();
            foreach (var item in Items)
            {
                if (item != null && item.slotType == slot)
                {
                    matches.Add(item);
                }
            }

            return matches.ToArray();
        }

        public ConflictRuleDefinition[] GetConflictRulesForSlot(SlotType slot)
        {
            var matches = new List<ConflictRuleDefinition>();
            foreach (var rule in ConflictRules)
            {
                if (rule != null && rule.sourceSlot == slot)
                {
                    matches.Add(rule);
                }
            }

            return matches.ToArray();
        }

        public bool TryGetItemById(string itemId, out AvatarItemDefinition item)
        {
            item = null;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            foreach (var candidate in Items)
            {
                if (candidate != null &&
                    string.Equals(candidate.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    item = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPreset(string presetKey, out OutfitPresetDefinition preset)
        {
            preset = null;
            if (string.IsNullOrWhiteSpace(presetKey))
            {
                return false;
            }

            foreach (var candidate in OutfitPresets)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.presetId, presetKey, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidate.presetName, presetKey, StringComparison.OrdinalIgnoreCase))
                {
                    preset = candidate;
                    return true;
                }
            }

            return false;
        }

        public AvatarAssetRegistryValidationReport ValidateRegistry()
        {
            var report = new AvatarAssetRegistryValidationReport();

            if (string.IsNullOrWhiteSpace(registryId))
            {
                report.AddError("Registry is missing registryId.");
            }

            if (string.IsNullOrWhiteSpace(requiredBaseVersion))
            {
                report.AddWarning("Registry is missing requiredBaseVersion.");
            }

            var seenItemIds = new Dictionary<string, AvatarItemDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in Items)
            {
                ValidateItem(item, seenItemIds, report);
            }

            var seenPresetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var preset in OutfitPresets)
            {
                ValidatePreset(preset, seenPresetKeys, report);
            }

            foreach (var rule in ConflictRules)
            {
                ValidateRule(rule, report);
            }

            return report;
        }

        private static void ValidateItem(
            AvatarItemDefinition item,
            IDictionary<string, AvatarItemDefinition> seenItemIds,
            AvatarAssetRegistryValidationReport report)
        {
            if (item == null)
            {
                report.AddWarning("Registry contains a null item entry.");
                return;
            }

            if (string.IsNullOrWhiteSpace(item.itemId))
            {
                report.AddError($"Item '{item.name}' is missing itemId.");
            }
            else if (seenItemIds.ContainsKey(item.itemId))
            {
                report.AddError($"Duplicate itemId '{item.itemId}' found in registry.");
            }
            else
            {
                seenItemIds[item.itemId] = item;
            }

            if (string.IsNullOrWhiteSpace(item.displayName))
            {
                report.AddWarning($"Item '{item.name}' is missing displayName.");
            }

            if (string.IsNullOrWhiteSpace(item.requiredBaseVersion))
            {
                report.AddWarning($"Item '{item.itemId}' is missing requiredBaseVersion.");
            }

            if (ContainsSlot(item.occupiesSlots, item.slotType))
            {
                report.AddWarning($"Item '{item.itemId}' redundantly occupies its own slot '{item.slotType}'.");
            }

            if (ContainsSlot(item.blocksSlots, item.slotType))
            {
                report.AddWarning($"Item '{item.itemId}' blocks its own slot '{item.slotType}'.");
            }

            if (ContainsSlot(item.requiresSlots, item.slotType))
            {
                report.AddError($"Item '{item.itemId}' requires its own slot '{item.slotType}'.");
            }
        }

        private static void ValidatePreset(
            OutfitPresetDefinition preset,
            ISet<string> seenPresetKeys,
            AvatarAssetRegistryValidationReport report)
        {
            if (preset == null)
            {
                report.AddWarning("Registry contains a null outfit preset entry.");
                return;
            }

            var presetKey = !string.IsNullOrWhiteSpace(preset.presetId)
                ? preset.presetId
                : preset.presetName;

            if (string.IsNullOrWhiteSpace(presetKey))
            {
                report.AddWarning($"Preset '{preset.name}' is missing presetId and presetName.");
            }
            else if (!seenPresetKeys.Add(presetKey))
            {
                report.AddError($"Duplicate preset key '{presetKey}' found in registry.");
            }

            ValidatePresetAssignment(preset, preset.hair, SlotType.Hair, "hair", report);
            ValidatePresetAssignment(preset, preset.hairAccessory, SlotType.HairAccessory, "hairAccessory", report);
            ValidatePresetAssignment(preset, preset.top, SlotType.Top, "top", report);
            ValidatePresetAssignment(preset, preset.bottom, SlotType.Bottom, "bottom", report);
            ValidatePresetAssignment(preset, preset.dress, SlotType.Dress, "dress", report);
            ValidatePresetAssignment(preset, preset.socks, SlotType.Socks, "socks", report);
            ValidatePresetAssignment(preset, preset.shoes, SlotType.Shoes, "shoes", report);
            ValidatePresetAssignment(preset, preset.gloves, SlotType.Gloves, "gloves", report);
            ValidatePresetAssignment(preset, preset.braceletL, SlotType.BraceletL, "braceletL", report);
            ValidatePresetAssignment(preset, preset.braceletR, SlotType.BraceletR, "braceletR", report);

            if (preset.dress != null && (preset.top != null || preset.bottom != null))
            {
                report.AddWarning($"Preset '{presetKey}' includes Dress together with Top or Bottom.");
            }
        }

        private static void ValidatePresetAssignment(
            OutfitPresetDefinition preset,
            AvatarItemDefinition item,
            SlotType expectedSlot,
            string fieldName,
            AvatarAssetRegistryValidationReport report)
        {
            if (item == null)
            {
                return;
            }

            if (item.slotType != expectedSlot)
            {
                var presetKey = !string.IsNullOrWhiteSpace(preset.presetId)
                    ? preset.presetId
                    : preset.presetName;
                report.AddError($"Preset '{presetKey}' assigns item '{item.itemId}' to field '{fieldName}', but the item slot is '{item.slotType}' instead of '{expectedSlot}'.");
            }
        }

        private static void ValidateRule(ConflictRuleDefinition rule, AvatarAssetRegistryValidationReport report)
        {
            if (rule == null)
            {
                report.AddWarning("Registry contains a null conflict rule entry.");
                return;
            }

            if (ContainsSlot(rule.blockedSlots, rule.sourceSlot))
            {
                report.AddWarning($"Conflict rule '{rule.name}' blocks its own source slot '{rule.sourceSlot}'.");
            }

            if (ContainsSlot(rule.requiredSlots, rule.sourceSlot))
            {
                report.AddError($"Conflict rule '{rule.name}' requires its own source slot '{rule.sourceSlot}'.");
            }
        }

        private static bool ContainsSlot(SlotType[] slots, SlotType target)
        {
            if (slots == null)
            {
                return false;
            }

            foreach (var slot in slots)
            {
                if (slot == target)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class AvatarAssetRegistryValidationReport
    {
        private readonly List<string> errors = new();
        private readonly List<string> warnings = new();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool IsValid => errors.Count == 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
            }
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                warnings.Add(message);
            }
        }
    }
}
