using AvatarSystem.Data;
using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Manages outfit presets — apply a full preset or save/load the current outfit.
    /// </summary>
    public sealed class AvatarPresetManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AvatarEquipmentManager equipmentManager;

        [Header("Available Presets")]
        [SerializeField] private OutfitPresetDefinition[] presets;

        private static readonly SlotType[] PresetSlots =
        {
            SlotType.Hair,
            SlotType.HairAccessory,
            SlotType.Top,
            SlotType.Bottom,
            SlotType.Dress,
            SlotType.Socks,
            SlotType.Shoes,
            SlotType.Gloves,
            SlotType.BraceletL,
            SlotType.BraceletR,
        };

        public OutfitPresetDefinition[] Presets => presets;

        /// <summary>Apply a full preset, unequipping everything first.</summary>
        public void ApplyPreset(OutfitPresetDefinition preset)
        {
            if (preset == null || equipmentManager == null) return;

            equipmentManager.UnequipAll();

            foreach (var slot in PresetSlots)
            {
                var item = preset.GetItemForSlot(slot);
                if (item != null)
                {
                    equipmentManager.Equip(item);
                }
            }
        }

        /// <summary>Apply a preset by its index in the presets array.</summary>
        public void ApplyPreset(int index)
        {
            if (presets == null || index < 0 || index >= presets.Length) return;
            ApplyPreset(presets[index]);
        }

        /// <summary>Save the currently equipped item IDs to PlayerPrefs.</summary>
        public void SaveCurrentOutfit(string saveKey = "AvatarOutfit")
        {
            if (equipmentManager == null) return;

            foreach (var slot in PresetSlots)
            {
                var item = equipmentManager.GetEquippedItem(slot);
                string key = $"{saveKey}_{slot}";
                PlayerPrefs.SetString(key, item != null ? item.itemId : string.Empty);
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load a saved outfit from PlayerPrefs. Requires an item lookup database.
        /// Returns the number of items successfully equipped.
        /// </summary>
        public int LoadSavedOutfit(AvatarItemDefinition[] allItems, string saveKey = "AvatarOutfit")
        {
            if (equipmentManager == null || allItems == null) return 0;

            equipmentManager.UnequipAll();
            int equipped = 0;

            foreach (var slot in PresetSlots)
            {
                string key = $"{saveKey}_{slot}";
                string savedId = PlayerPrefs.GetString(key, string.Empty);
                if (string.IsNullOrEmpty(savedId)) continue;

                foreach (var item in allItems)
                {
                    if (item != null && item.itemId == savedId)
                    {
                        if (equipmentManager.Equip(item)) equipped++;
                        break;
                    }
                }
            }

            return equipped;
        }
    }
}
