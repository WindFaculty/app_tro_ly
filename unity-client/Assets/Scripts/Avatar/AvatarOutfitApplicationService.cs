using System;
using AvatarSystem;
using AvatarSystem.Data;

namespace LocalAssistant.Avatar
{
    public interface IAvatarOutfitRepository
    {
        bool Equip(AvatarItemDefinition item);
        void Unequip(SlotType slot);
        void ApplyPreset(OutfitPresetDefinition preset);
        void SaveCurrentOutfit(string saveKey);
        int LoadSavedOutfit(AvatarItemDefinition[] allItems, string saveKey);
    }

    public sealed class AvatarOutfitRuntimeRepository : IAvatarOutfitRepository
    {
        private readonly AvatarEquipmentManager equipmentManager;
        private readonly AvatarPresetManager presetManager;

        public AvatarOutfitRuntimeRepository(AvatarEquipmentManager equipmentManager, AvatarPresetManager presetManager)
        {
            this.equipmentManager = equipmentManager ?? throw new ArgumentNullException(nameof(equipmentManager));
            this.presetManager = presetManager ?? throw new ArgumentNullException(nameof(presetManager));
        }

        public bool Equip(AvatarItemDefinition item) => equipmentManager.Equip(item);
        public void Unequip(SlotType slot) => equipmentManager.Unequip(slot);
        public void ApplyPreset(OutfitPresetDefinition preset) => presetManager.ApplyPreset(preset);
        public void SaveCurrentOutfit(string saveKey) => presetManager.SaveCurrentOutfit(saveKey);
        public int LoadSavedOutfit(AvatarItemDefinition[] allItems, string saveKey) => presetManager.LoadSavedOutfit(allItems, saveKey);
    }

    public sealed class AvatarOutfitCommandResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public int EquippedCount { get; set; }
    }

    public sealed class AvatarOutfitApplicationService
    {
        private readonly IAvatarOutfitRepository repository;

        public AvatarOutfitApplicationService(IAvatarOutfitRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public AvatarOutfitCommandResult Equip(AvatarItemDefinition item)
        {
            if (item == null)
            {
                return new AvatarOutfitCommandResult
                {
                    Succeeded = false,
                    Message = "No outfit item was provided.",
                };
            }

            var applied = repository.Equip(item);
            return new AvatarOutfitCommandResult
            {
                Succeeded = applied,
                ItemId = item.itemId ?? string.Empty,
                Message = applied
                    ? $"Equipped '{item.displayName}'."
                    : $"Could not equip '{item.displayName}'.",
            };
        }

        public void Unequip(SlotType slot)
        {
            repository.Unequip(slot);
        }

        public AvatarOutfitCommandResult ApplyPreset(OutfitPresetDefinition preset)
        {
            if (preset == null)
            {
                return new AvatarOutfitCommandResult
                {
                    Succeeded = false,
                    Message = "No outfit preset was provided.",
                };
            }

            repository.ApplyPreset(preset);
            return new AvatarOutfitCommandResult
            {
                Succeeded = true,
                Message = $"Applied preset '{preset.presetName}'.",
            };
        }

        public AvatarOutfitCommandResult SaveCurrentOutfit(string saveKey = "AvatarOutfit")
        {
            repository.SaveCurrentOutfit(saveKey);
            return new AvatarOutfitCommandResult
            {
                Succeeded = true,
                Message = $"Saved outfit to '{saveKey}'.",
            };
        }

        public AvatarOutfitCommandResult LoadSavedOutfit(AvatarItemDefinition[] allItems, string saveKey = "AvatarOutfit")
        {
            var equippedCount = repository.LoadSavedOutfit(allItems, saveKey);
            return new AvatarOutfitCommandResult
            {
                Succeeded = equippedCount > 0,
                EquippedCount = equippedCount,
                Message = equippedCount > 0
                    ? $"Loaded {equippedCount} outfit items from '{saveKey}'."
                    : $"No saved outfit items could be loaded from '{saveKey}'.",
            };
        }

        public AvatarOutfitCommandResult LoadSavedOutfit(AvatarAssetRegistryDefinition registry, string saveKey = "AvatarOutfit")
        {
            if (registry == null)
            {
                return new AvatarOutfitCommandResult
                {
                    Succeeded = false,
                    Message = "No avatar asset registry was provided.",
                };
            }

            return LoadSavedOutfit(registry.Items, saveKey);
        }
    }
}
