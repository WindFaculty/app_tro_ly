using AvatarSystem;
using AvatarSystem.Data;
using LocalAssistant.Avatar;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class AvatarOutfitApplicationServiceTests
    {
        [Test]
        public void EquipReturnsSuccessSummaryWhenRepositoryAcceptsItem()
        {
            var repository = new FakeAvatarOutfitRepository { EquipResult = true };
            var service = new AvatarOutfitApplicationService(repository);
            var item = ScriptableObject.CreateInstance<AvatarItemDefinition>();
            item.itemId = "top-01";
            item.displayName = "Top 01";

            var result = service.Equip(item);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("top-01", result.ItemId);
            Assert.AreEqual("Equipped 'Top 01'.", result.Message);
            UnityEngine.Object.DestroyImmediate(item);
        }

        [Test]
        public void LoadSavedOutfitReturnsCountFromRepository()
        {
            var repository = new FakeAvatarOutfitRepository { LoadCount = 3 };
            var service = new AvatarOutfitApplicationService(repository);

            var result = service.LoadSavedOutfit(new AvatarItemDefinition[0], "TestOutfit");

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(3, result.EquippedCount);
            Assert.AreEqual("Loaded 3 outfit items from 'TestOutfit'.", result.Message);
        }

        [Test]
        public void LoadSavedOutfitUsesRegistryItemsWhenRegistryIsProvided()
        {
            var repository = new FakeAvatarOutfitRepository { LoadCount = 1 };
            var service = new AvatarOutfitApplicationService(repository);
            var registry = ScriptableObject.CreateInstance<AvatarAssetRegistryDefinition>();
            var item = ScriptableObject.CreateInstance<AvatarItemDefinition>();
            item.itemId = "hair-01";
            item.displayName = "Hair 01";
            item.slotType = SlotType.Hair;
            registry.items = new[] { item };

            var result = service.LoadSavedOutfit(registry, "RegistryOutfit");

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, repository.LastLoadedItems.Length);
            Assert.AreSame(item, repository.LastLoadedItems[0]);
            Assert.AreEqual("Loaded 1 outfit items from 'RegistryOutfit'.", result.Message);

            UnityEngine.Object.DestroyImmediate(item);
            UnityEngine.Object.DestroyImmediate(registry);
        }

        private sealed class FakeAvatarOutfitRepository : IAvatarOutfitRepository
        {
            public bool EquipResult { get; set; }
            public int LoadCount { get; set; }
            public AvatarItemDefinition[] LastLoadedItems { get; private set; }

            public bool Equip(AvatarItemDefinition item) => EquipResult;
            public void Unequip(SlotType slot) { }
            public void ApplyPreset(OutfitPresetDefinition preset) { }
            public void SaveCurrentOutfit(string saveKey) { }
            public int LoadSavedOutfit(AvatarItemDefinition[] allItems, string saveKey)
            {
                LastLoadedItems = allItems;
                return LoadCount;
            }
        }
    }
}
