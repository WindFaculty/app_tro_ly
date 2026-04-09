using System;
using System.Collections.Generic;
using System.IO;
using AvatarSystem;
using AvatarSystem.Data;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class MeshAssetRegistryTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new();
        private string temporaryDirectory;

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

            if (!string.IsNullOrWhiteSpace(temporaryDirectory) && Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }

            temporaryDirectory = null;
        }

        [Test]
        public void ReloadRegistersWardrobeAndRoomHandoffManifests()
        {
            temporaryDirectory = CreateTemporaryManifestDirectory();
            WriteManifest(
                "azure-sakura-handoff.json",
                BuildManifestJson(
                    assetId: "azure-sakura-kimono-0326010047",
                    assetName: "Azure Sakura Kimono",
                    assetType: "avatar_clothing",
                    category: "clothing_foundation",
                    targetUnityPath: "Assets/AvatarSystem/AvatarProduction/Wardrobe/Foundations/AzureSakuraKimono",
                    slot: "dress",
                    roomFocusPreset: "wardrobe"));
            WriteManifest(
                "sakura-hairpin-handoff.json",
                BuildManifestJson(
                    assetId: "sakura-hairpin-01",
                    assetName: "Sakura Hairpin",
                    assetType: "avatar_accessory",
                    category: "accessory_foundation",
                    targetUnityPath: "Assets/AvatarSystem/AvatarProduction/Wardrobe/Accessories/SakuraHairpin",
                    slot: "hair_accessory",
                    roomFocusPreset: "wardrobe"));
            WriteManifest(
                "desk-lantern-handoff.json",
                BuildManifestJson(
                    assetId: "desk-lantern-01",
                    assetName: "Desk Lantern",
                    assetType: "room_item",
                    category: "room_static_asset",
                    targetUnityPath: "Assets/Environment/RoomItems/DeskLantern",
                    roomFocusPreset: "desk"));
            WriteManifest(
                "ignored-validation.json",
                BuildManifestJson(
                    assetId: "ignored-01",
                    assetName: "Ignored Entry",
                    assetType: "avatar_clothing",
                    category: "clothing_foundation",
                    targetUnityPath: "Assets/AvatarSystem/AvatarProduction/Wardrobe/Foundations/Ignored",
                    slot: "dress",
                    roomFocusPreset: "wardrobe",
                    status: "validated"));

            var registry = CreateGameObject("MeshAssetRegistry").AddComponent<LocalAssistant.Runtime.MeshAssetRegistry>();
            registry.ConfigureSearchRoots(temporaryDirectory);
            registry.Reload();

            Assert.AreEqual(3, registry.TotalEntryCount);
            Assert.AreEqual(2, registry.WardrobeFoundationCount);
            Assert.AreEqual(1, registry.RoomAssetCount);
            Assert.AreEqual(1, registry.IgnoredManifestCount);
            Assert.AreEqual(0, registry.FailedManifestCount);
            Assert.That(registry.LoadedManifestPaths, Has.Count.EqualTo(3));

            Assert.IsTrue(registry.TryGetWardrobeFoundation("AzureSakuraKimono", out var clothing));
            Assert.AreEqual("azure-sakura-kimono-0326010047", clothing.AssetId);
            Assert.AreEqual("dress", clothing.Slot);

            Assert.IsTrue(registry.TryGetWardrobeFoundation("Sakura Hairpin", out var accessory));
            Assert.AreEqual("hair_accessory", accessory.Slot);

            Assert.IsTrue(registry.TryGetRoomAsset("DeskLantern", out var roomItem));
            Assert.AreEqual("desk", roomItem.RoomFocusPreset);
        }

        [Test]
        public void TryApplyRoomFocusPresetUsesRegistryAliasWhenSceneObjectIsMissing()
        {
            temporaryDirectory = CreateTemporaryManifestDirectory();
            WriteManifest(
                "desk-lantern-handoff.json",
                BuildManifestJson(
                    assetId: "desk-lantern-01",
                    assetName: "Desk Lantern",
                    assetType: "room_item",
                    category: "room_static_asset",
                    targetUnityPath: "Assets/Environment/RoomItems/DeskLantern",
                    roomFocusPreset: "desk"));

            var registry = CreateGameObject("MeshAssetRegistry").AddComponent<LocalAssistant.Runtime.MeshAssetRegistry>();
            registry.ConfigureSearchRoots(temporaryDirectory);
            registry.Reload();

            var camera = CreateGameObject("SceneCamera").AddComponent<Camera>();
            var roomRuntime = CreateGameObject("RoomRuntime").AddComponent<LocalAssistant.Runtime.RoomRuntime>();
            roomRuntime.Bind(camera);

            Assert.IsTrue(registry.TryApplyRoomFocusPreset("DeskLantern", roomRuntime, out var roomEntry));
            Assert.AreEqual("desk", roomRuntime.CurrentFocus);
            Assert.AreEqual("desk-lantern-01", roomEntry.AssetId);
        }

        [Test]
        public void UnityBridgeClientRejectsWardrobeEquipWhenManifestHasNoRuntimeItemMapping()
        {
            temporaryDirectory = CreateTemporaryManifestDirectory();
            WriteManifest(
                "azure-sakura-handoff.json",
                BuildManifestJson(
                    assetId: "azure-sakura-kimono-0326010047",
                    assetName: "Azure Sakura Kimono",
                    assetType: "avatar_clothing",
                    category: "clothing_foundation",
                    targetUnityPath: "Assets/AvatarSystem/AvatarProduction/Wardrobe/Foundations/AzureSakuraKimono",
                    slot: "dress",
                    roomFocusPreset: "wardrobe"));
            WriteManifest(
                "desk-lantern-handoff.json",
                BuildManifestJson(
                    assetId: "desk-lantern-01",
                    assetName: "Desk Lantern",
                    assetType: "room_item",
                    category: "room_static_asset",
                    targetUnityPath: "Assets/Environment/RoomItems/DeskLantern",
                    roomFocusPreset: "desk"));

            var registry = CreateGameObject("MeshAssetRegistry").AddComponent<LocalAssistant.Runtime.MeshAssetRegistry>();
            registry.ConfigureSearchRoots(temporaryDirectory);
            registry.Reload();

            var camera = CreateGameObject("SceneCamera").AddComponent<Camera>();
            var roomRuntime = CreateGameObject("RoomRuntime").AddComponent<LocalAssistant.Runtime.RoomRuntime>();
            roomRuntime.Bind(camera);

            var interactionRuntime = CreateGameObject("InteractionRuntime").AddComponent<LocalAssistant.Runtime.InteractionRuntime>();
            interactionRuntime.Bind(null);

            var itemRegistry = CreateGameObject("AvatarItemRegistry").AddComponent<LocalAssistant.Runtime.AvatarItemRegistry>();
            itemRegistry.ConfigureResourcesPath(string.Empty);
            itemRegistry.ConfigureAdditionalItems(Array.Empty<AvatarItemDefinition>());

            var bridgeClient = CreateGameObject("UnityBridgeClient").AddComponent<LocalAssistant.Runtime.UnityBridgeClient>();
            bridgeClient.Bind(null, null, null, null, roomRuntime, interactionRuntime, registry, itemRegistry);

            string lastRejectedReason = null;
            bridgeClient.CommandRejected += reason => lastRejectedReason = reason;

            var focusHandled = bridgeClient.ApplyCommand(
                "room.focusObject",
                new LocalAssistant.Runtime.UnityBridgeCommandPayload
                {
                    object_name = "DeskLantern",
                });

            Assert.IsTrue(focusHandled);
            Assert.AreEqual("desk", roomRuntime.CurrentFocus);

            var equipHandled = bridgeClient.ApplyCommand(
                "wardrobe.equipItem",
                new LocalAssistant.Runtime.UnityBridgeCommandPayload
                {
                    item_id = "AzureSakuraKimono",
                });

            Assert.IsFalse(equipHandled);
            Assert.That(lastRejectedReason, Does.Contain("AvatarItemDefinition"));
            Assert.That(lastRejectedReason, Does.Contain("azure-sakura-kimono-0326010047"));
        }

        [Test]
        public void UnityBridgeClientEquipsWardrobeItemFromManifestAliasWhenRuntimeItemExists()
        {
            temporaryDirectory = CreateTemporaryManifestDirectory();
            WriteManifest(
                "azure-sakura-handoff.json",
                BuildManifestJson(
                    assetId: "azure-sakura-kimono-0326010047",
                    assetName: "Azure Sakura Kimono",
                    assetType: "avatar_clothing",
                    category: "clothing_foundation",
                    targetUnityPath: "Assets/AvatarSystem/AvatarProduction/Wardrobe/Foundations/AzureSakuraKimono",
                    slot: "dress",
                    roomFocusPreset: "wardrobe"));

            var registry = CreateGameObject("MeshAssetRegistry").AddComponent<LocalAssistant.Runtime.MeshAssetRegistry>();
            registry.ConfigureSearchRoots(temporaryDirectory);
            registry.Reload();

            var avatarRootObject = CreateGameObject("AvatarRoot");
            var equipment = avatarRootObject.AddComponent<AvatarEquipmentManager>();
            var avatarRoot = avatarRootObject.AddComponent<AvatarRootController>();
            avatarRoot.Initialize();

            var avatarRuntime = CreateGameObject("AvatarRuntime").AddComponent<LocalAssistant.Runtime.AvatarRuntime>();
            avatarRuntime.Bind(null, null, avatarRoot);

            var itemRegistry = CreateGameObject("AvatarItemRegistry").AddComponent<LocalAssistant.Runtime.AvatarItemRegistry>();
            itemRegistry.ConfigureResourcesPath(string.Empty);
            itemRegistry.ConfigureAdditionalItems(
                CreateItemDefinition(
                    itemId: "azure-sakura-kimono-0326010047",
                    displayName: "Azure Sakura Kimono",
                    assetName: "AzureSakuraKimono",
                    slot: SlotType.Dress));
            itemRegistry.Reload();

            var interactionRuntime = CreateGameObject("InteractionRuntime").AddComponent<LocalAssistant.Runtime.InteractionRuntime>();
            interactionRuntime.Bind(null);

            var bridgeClient = CreateGameObject("UnityBridgeClient").AddComponent<LocalAssistant.Runtime.UnityBridgeClient>();
            bridgeClient.Bind(null, avatarRuntime, null, null, null, interactionRuntime, registry, itemRegistry);

            var equipHandled = bridgeClient.ApplyCommand(
                "wardrobe.equipItem",
                new LocalAssistant.Runtime.UnityBridgeCommandPayload
                {
                    item_id = "Assets/AvatarSystem/AvatarProduction/Wardrobe/Foundations/AzureSakuraKimono",
                });

            Assert.IsTrue(equipHandled);
            Assert.AreEqual("azure-sakura-kimono-0326010047", equipment.GetEquippedItem(SlotType.Dress).itemId);
        }

        private string CreateTemporaryManifestDirectory()
        {
            var manifestDirectory = Path.Combine(Path.GetTempPath(), "mesh-asset-registry-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(manifestDirectory);
            return manifestDirectory;
        }

        private void WriteManifest(string fileName, string json)
        {
            File.WriteAllText(Path.Combine(temporaryDirectory, fileName), json);
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private AvatarItemDefinition CreateItemDefinition(string itemId, string displayName, string assetName, SlotType slot)
        {
            var item = ScriptableObject.CreateInstance<AvatarItemDefinition>();
            item.itemId = itemId;
            item.displayName = displayName;
            item.slotType = slot;
            item.name = assetName;
            createdObjects.Add(item);
            return item;
        }

        private static string BuildManifestJson(
            string assetId,
            string assetName,
            string assetType,
            string category,
            string targetUnityPath,
            string slot = "",
            string roomFocusPreset = "",
            string status = "export-ready")
        {
            return $@"{{
  ""asset_id"": ""{assetId}"",
  ""asset_name"": ""{assetName}"",
  ""asset_type"": ""{assetType}"",
  ""category"": ""{category}"",
  ""status"": ""{status}"",
  ""target_runtime"": ""unity"",
  ""target_unity_path"": ""{targetUnityPath}"",
  ""slot"": ""{slot}"",
  ""room_focus_preset"": ""{roomFocusPreset}"",
  ""validation_report_path"": ""tools/reports/{assetId}.json"",
  ""preview_render_path"": ""tools/renders/{assetId}.png"",
  ""handoff_source_path"": ""bleder/{assetId}.blend"",
  ""notes"": [""test fixture""],
  ""manual_gates"": []
}}";
        }
    }
}
