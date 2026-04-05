using System.Collections.Generic;
using AvatarSystem;
using AvatarSystem.Data;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class AvatarAssetRegistryDefinitionTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void RegistryReturnsItemsBySlotAndId()
        {
            var registry = CreateScriptableObject<AvatarAssetRegistryDefinition>();
            var hair = CreateItem("hair-short-a", SlotType.Hair);
            var top = CreateItem("top-casual-a", SlotType.Top);
            registry.items = new[] { hair, top };

            Assert.IsTrue(registry.TryGetItemById("hair-short-a", out var found));
            Assert.AreSame(hair, found);

            var topItems = registry.GetItemsForSlot(SlotType.Top);
            Assert.AreEqual(1, topItems.Length);
            Assert.AreSame(top, topItems[0]);
        }

        [Test]
        public void ValidateRegistryReportsDuplicateIdsPresetMismatchAndRuleSelfDependency()
        {
            var registry = CreateScriptableObject<AvatarAssetRegistryDefinition>();
            registry.registryId = "base";

            var duplicateTopA = CreateItem("top-01", SlotType.Top);
            var duplicateTopB = CreateItem("top-01", SlotType.Top);
            registry.items = new[] { duplicateTopA, duplicateTopB };

            var invalidPreset = CreateScriptableObject<OutfitPresetDefinition>();
            invalidPreset.presetId = "starter";
            invalidPreset.presetName = "Starter";
            invalidPreset.bottom = duplicateTopA;
            registry.outfitPresets = new[] { invalidPreset };

            var invalidRule = CreateScriptableObject<ConflictRuleDefinition>();
            invalidRule.sourceSlot = SlotType.Dress;
            invalidRule.requiredSlots = new[] { SlotType.Dress };
            registry.conflictRules = new[] { invalidRule };

            var report = registry.ValidateRegistry();

            Assert.IsFalse(report.IsValid);
            CollectionAssert.Contains(
                report.Errors,
                "Duplicate itemId 'top-01' found in registry.");
            Assert.That(
                report.Errors,
                Has.Some.Contains("Preset 'starter' assigns item 'top-01' to field 'bottom'"));
            Assert.That(
                report.Errors,
                Has.Some.Contains("requires its own source slot 'Dress'."));
        }

        private AvatarItemDefinition CreateItem(string itemId, SlotType slotType)
        {
            var item = CreateScriptableObject<AvatarItemDefinition>();
            item.itemId = itemId;
            item.displayName = itemId;
            item.slotType = slotType;
            item.requiredBaseVersion = "v001";
            item.bodyTypeId = "base";
            return item;
        }

        private T CreateScriptableObject<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(instance);
            return instance;
        }
    }
}
