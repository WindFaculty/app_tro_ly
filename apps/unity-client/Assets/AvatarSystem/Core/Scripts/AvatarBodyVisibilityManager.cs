using System;
using System.Collections.Generic;
using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Toggles body region mesh renderers based on currently equipped items.
    /// Listens to AvatarEquipmentManager.SlotChanged and recalculates visibility.
    /// </summary>
    public sealed class AvatarBodyVisibilityManager : MonoBehaviour
    {
        [Serializable]
        public struct BodyRegionMapping
        {
            public BodyRegion region;
            public Renderer[] renderers;
        }

        [SerializeField] private BodyRegionMapping[] regionMappings;

        private AvatarRootController root;
        private readonly Dictionary<BodyRegion, Renderer[]> regionLookup = new();

        public void Initialize(AvatarRootController rootController)
        {
            root = rootController;

            // Build lookup
            if (regionMappings != null)
            {
                foreach (var mapping in regionMappings)
                {
                    regionLookup[mapping.region] = mapping.renderers;
                }
            }

            // Subscribe to equipment changes
            if (root.Equipment != null)
            {
                root.Equipment.SlotChanged += OnSlotChanged;
            }

            RefreshVisibility();
        }

        private void OnDestroy()
        {
            if (root != null && root.Equipment != null)
            {
                root.Equipment.SlotChanged -= OnSlotChanged;
            }
        }

        private void OnSlotChanged(SlotType slot, Data.AvatarItemDefinition item)
        {
            RefreshVisibility();
        }

        /// <summary>
        /// Recalculates which body regions should be visible or hidden.
        /// </summary>
        public void RefreshVisibility()
        {
            if (root == null || root.Equipment == null) return;

            var hiddenRegions = root.Equipment.GetAllHiddenRegions();

            foreach (var kvp in regionLookup)
            {
                bool shouldHide = hiddenRegions.Contains(kvp.Key);
                if (kvp.Value == null) continue;
                foreach (var r in kvp.Value)
                {
                    if (r != null) r.enabled = !shouldHide;
                }
            }
        }
    }
}
