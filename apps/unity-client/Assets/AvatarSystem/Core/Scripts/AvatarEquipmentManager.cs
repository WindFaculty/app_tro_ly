using System;
using System.Collections.Generic;
using AvatarSystem.Data;
using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Manages equipping and unequipping items across all outfit slots.
    /// Handles conflict resolution, requirement checking, and prefab lifecycle.
    /// </summary>
    public sealed class AvatarEquipmentManager : MonoBehaviour
    {
        private AvatarRootController root;
        private readonly Dictionary<SlotType, EquippedSlot> slots = new();

        /// <summary>Fires after any slot changes (slot, new item or null).</summary>
        public event Action<SlotType, AvatarItemDefinition> SlotChanged;

        public void Initialize(AvatarRootController rootController)
        {
            root = rootController;
            // Pre-populate all slot entries
            foreach (SlotType slot in Enum.GetValues(typeof(SlotType)))
            {
                if (!slots.ContainsKey(slot))
                {
                    slots[slot] = new EquippedSlot();
                }
            }
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>Equip an item, resolving conflicts automatically.</summary>
        public bool Equip(AvatarItemDefinition item)
        {
            if (item == null) return false;

            // Check requirements
            if (item.requiresSlots != null)
            {
                foreach (var req in item.requiresSlots)
                {
                    if (!IsSlotOccupied(req))
                    {
                        Debug.LogWarning($"[AvatarEquipment] Cannot equip {item.displayName}: requires slot {req}.");
                        return false;
                    }
                }
            }

            // Check tag incompatibilities
            if (item.incompatibleTags != null && item.incompatibleTags.Length > 0)
            {
                foreach (var kvp in slots)
                {
                    if (kvp.Value.item == null) continue;
                    if (HasAnyTag(kvp.Value.item, item.incompatibleTags))
                    {
                        Debug.LogWarning($"[AvatarEquipment] Cannot equip {item.displayName}: incompatible tag on {kvp.Value.item.displayName}.");
                        return false;
                    }
                }
            }

            // Unequip blocked slots
            if (item.blocksSlots != null)
            {
                foreach (var blocked in item.blocksSlots)
                {
                    Unequip(blocked);
                }
            }

            // Unequip current item in the target slot
            Unequip(item.slotType);

            // Instantiate the prefab
            GameObject instance = null;
            if (item.prefab != null && root != null)
            {
                instance = Instantiate(item.prefab, root.transform);
                instance.name = $"Equipped_{item.slotType}_{item.itemId}";
            }

            slots[item.slotType] = new EquippedSlot { item = item, instance = instance };
            SlotChanged?.Invoke(item.slotType, item);

            return true;
        }

        /// <summary>Remove the item from a slot and destroy its instance.</summary>
        public void Unequip(SlotType slot)
        {
            if (!slots.TryGetValue(slot, out var equipped) || equipped.item == null) return;

            if (equipped.instance != null)
            {
                Destroy(equipped.instance);
            }

            slots[slot] = new EquippedSlot();
            SlotChanged?.Invoke(slot, null);
        }

        /// <summary>Remove all equipped items.</summary>
        public void UnequipAll()
        {
            foreach (SlotType slot in Enum.GetValues(typeof(SlotType)))
            {
                Unequip(slot);
            }
        }

        public bool IsSlotOccupied(SlotType slot)
        {
            return slots.TryGetValue(slot, out var s) && s.item != null;
        }

        public AvatarItemDefinition GetEquippedItem(SlotType slot)
        {
            return slots.TryGetValue(slot, out var s) ? s.item : null;
        }

        /// <summary>Collect all HideBodyRegions from equipped items.</summary>
        public HashSet<BodyRegion> GetAllHiddenRegions()
        {
            var regions = new HashSet<BodyRegion>();
            foreach (var kvp in slots)
            {
                if (kvp.Value.item == null) continue;
                if (kvp.Value.item.hideBodyRegions == null) continue;
                foreach (var r in kvp.Value.item.hideBodyRegions)
                {
                    regions.Add(r);
                }
            }
            return regions;
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private static bool HasAnyTag(AvatarItemDefinition item, string[] tags)
        {
            if (item.compatibleTags == null) return false;
            foreach (var itemTag in item.compatibleTags)
            {
                foreach (var checkTag in tags)
                {
                    if (string.Equals(itemTag, checkTag, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private struct EquippedSlot
        {
            public AvatarItemDefinition item;
            public GameObject instance;
        }
    }
}
