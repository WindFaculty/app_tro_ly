using System;
using System.Collections.Generic;
using System.Reflection;
using AvatarSystem;
using AvatarSystem.Data;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class AvatarOutfitSystemTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new();
        private string playerPrefsKey;

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(playerPrefsKey))
            {
                foreach (SlotType slot in Enum.GetValues(typeof(SlotType)))
                {
                    PlayerPrefs.DeleteKey($"{playerPrefsKey}_{slot}");
                }
            }

            PlayerPrefs.Save();

            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
            playerPrefsKey = null;
        }

        [Test]
        public void EquipAndUnequipItemUpdatesSlotOccupancy()
        {
            var equipment = CreateGameObject("EquipmentRoot").AddComponent<AvatarEquipmentManager>();
            equipment.Initialize(null);

            var top = CreateItem("Top_01", SlotType.Top);

            Assert.IsTrue(equipment.Equip(top));
            Assert.IsTrue(equipment.IsSlotOccupied(SlotType.Top));
            Assert.AreSame(top, equipment.GetEquippedItem(SlotType.Top));

            equipment.Unequip(SlotType.Top);

            Assert.IsFalse(equipment.IsSlotOccupied(SlotType.Top));
            Assert.IsNull(equipment.GetEquippedItem(SlotType.Top));
        }

        [Test]
        public void EquipDressUnequipsTopAndBottom()
        {
            var equipment = CreateGameObject("EquipmentRoot").AddComponent<AvatarEquipmentManager>();
            equipment.Initialize(null);

            var top = CreateItem("Top_01", SlotType.Top);
            var bottom = CreateItem("Bottom_01", SlotType.Bottom);
            var dress = CreateItem(
                "Dress_01",
                SlotType.Dress,
                blocksSlots: new[] { SlotType.Top, SlotType.Bottom });

            Assert.IsTrue(equipment.Equip(top));
            Assert.IsTrue(equipment.Equip(bottom));

            Assert.IsTrue(equipment.Equip(dress));

            Assert.IsFalse(equipment.IsSlotOccupied(SlotType.Top));
            Assert.IsFalse(equipment.IsSlotOccupied(SlotType.Bottom));
            Assert.AreSame(dress, equipment.GetEquippedItem(SlotType.Dress));
        }

        [Test]
        public void BodyVisibilityManagerHidesAndRestoresMappedRegionsForLongClothes()
        {
            var avatarRootObject = CreateGameObject("AvatarRoot");
            var equipment = avatarRootObject.AddComponent<AvatarEquipmentManager>();
            var visibility = avatarRootObject.AddComponent<AvatarBodyVisibilityManager>();
            var root = avatarRootObject.AddComponent<AvatarRootController>();

            var torsoRenderer = CreateRendererObject("TorsoRenderer", avatarRootObject.transform);
            var armRenderer = CreateRendererObject("ArmRenderer", avatarRootObject.transform);
            var thighRenderer = CreateRendererObject("ThighRenderer", avatarRootObject.transform);
            SetPrivateField(
                visibility,
                "regionMappings",
                new[]
                {
                    new AvatarBodyVisibilityManager.BodyRegionMapping
                    {
                        region = BodyRegion.TorsoUpper,
                        renderers = new Renderer[] { torsoRenderer },
                    },
                    new AvatarBodyVisibilityManager.BodyRegionMapping
                    {
                        region = BodyRegion.ArmUpperL,
                        renderers = new Renderer[] { armRenderer },
                    },
                    new AvatarBodyVisibilityManager.BodyRegionMapping
                    {
                        region = BodyRegion.ThighL,
                        renderers = new Renderer[] { thighRenderer },
                    },
                });

            root.Initialize();

            var top = CreateItem(
                "Top_01",
                SlotType.Top,
                hideBodyRegions: new[] { BodyRegion.TorsoUpper, BodyRegion.ArmUpperL });
            var bottom = CreateItem(
                "Bottom_01",
                SlotType.Bottom,
                hideBodyRegions: new[] { BodyRegion.ThighL });

            Assert.IsTrue(torsoRenderer.enabled);
            Assert.IsTrue(armRenderer.enabled);
            Assert.IsTrue(thighRenderer.enabled);

            Assert.IsTrue(equipment.Equip(top));
            Assert.IsFalse(torsoRenderer.enabled);
            Assert.IsFalse(armRenderer.enabled);
            Assert.IsTrue(thighRenderer.enabled);

            Assert.IsTrue(equipment.Equip(bottom));
            Assert.IsFalse(thighRenderer.enabled);

            equipment.Unequip(SlotType.Top);
            Assert.IsTrue(torsoRenderer.enabled);
            Assert.IsTrue(armRenderer.enabled);
            Assert.IsFalse(thighRenderer.enabled);

            equipment.Unequip(SlotType.Bottom);
            Assert.IsTrue(thighRenderer.enabled);
        }

        [Test]
        public void PresetManagerCanApplySaveAndLoadOutfit()
        {
            playerPrefsKey = "AvatarOutfitTest_" + Guid.NewGuid().ToString("N");

            var rootObject = CreateGameObject("PresetRoot");
            var equipment = rootObject.AddComponent<AvatarEquipmentManager>();
            var presetManager = rootObject.AddComponent<AvatarPresetManager>();
            equipment.Initialize(null);
            SetPrivateField(presetManager, "equipmentManager", equipment);

            var hair = CreateItem("Hair_01", SlotType.Hair);
            var top = CreateItem("Top_01", SlotType.Top);
            var shoes = CreateItem("Shoes_01", SlotType.Shoes);
            var previousBottom = CreateItem("Bottom_01", SlotType.Bottom);

            var preset = CreateScriptableObject<OutfitPresetDefinition>();
            preset.presetName = "Starter Outfit";
            preset.hair = hair;
            preset.top = top;
            preset.shoes = shoes;

            Assert.IsTrue(equipment.Equip(previousBottom));

            presetManager.ApplyPreset(preset);

            Assert.AreSame(hair, equipment.GetEquippedItem(SlotType.Hair));
            Assert.AreSame(top, equipment.GetEquippedItem(SlotType.Top));
            Assert.AreSame(shoes, equipment.GetEquippedItem(SlotType.Shoes));
            Assert.IsFalse(equipment.IsSlotOccupied(SlotType.Bottom));

            presetManager.SaveCurrentOutfit(playerPrefsKey);
            equipment.UnequipAll();

            var equippedCount = presetManager.LoadSavedOutfit(
                new[] { hair, top, shoes, previousBottom },
                playerPrefsKey);

            Assert.AreEqual(3, equippedCount);
            Assert.AreSame(hair, equipment.GetEquippedItem(SlotType.Hair));
            Assert.AreSame(top, equipment.GetEquippedItem(SlotType.Top));
            Assert.AreSame(shoes, equipment.GetEquippedItem(SlotType.Shoes));
        }

        private AvatarItemDefinition CreateItem(
            string itemId,
            SlotType slot,
            SlotType[] blocksSlots = null,
            SlotType[] requiresSlots = null,
            BodyRegion[] hideBodyRegions = null)
        {
            var item = CreateScriptableObject<AvatarItemDefinition>();
            item.itemId = itemId;
            item.displayName = itemId;
            item.slotType = slot;
            item.blocksSlots = blocksSlots;
            item.requiresSlots = requiresSlots;
            item.hideBodyRegions = hideBodyRegions;
            return item;
        }

        private T CreateScriptableObject<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(instance);
            return instance;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private MeshRenderer CreateRendererObject(string name, Transform parent)
        {
            var gameObject = CreateGameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<MeshFilter>();
            return gameObject.AddComponent<MeshRenderer>();
        }

        private static void SetPrivateField<TTarget>(TTarget target, string fieldName, object value)
        {
            var field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected field '{fieldName}' on {typeof(TTarget).Name}.");
            field.SetValue(target, value);
        }
    }
}
