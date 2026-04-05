using System.Linq;
using LocalAssistant.World.Objects;
using LocalAssistant.World.Room;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class RoomObjectRegistryTests
    {
        [Test]
        public void FoundationRegistryContainsExpectedMvpDefinitions()
        {
            var registry = RoomObjectRegistry.CreateFoundationMvp(RoomLayoutDefinition.CreateDefault());

            Assert.IsTrue(registry.TryGetDefinition("desk_main_01", out var desk));
            Assert.AreEqual(RoomObjectCategory.Furniture, desk.Category);
            Assert.AreEqual(RoomObjectShapeKind.Desk, desk.ShapeKind);
            Assert.IsTrue(registry.TryGetDefinition("laptop_work_01", out var laptop));
            Assert.AreEqual(RoomInteractionType.InspectAndFocus, laptop.InteractionType);
            Assert.IsTrue(registry.TryGetDefinition("books_display_01", out var books));
            Assert.AreEqual(RoomObjectShapeKind.Books, books.ShapeKind);
            Assert.IsTrue(registry.TryGetDefinition("cabinet_storage_01", out var cabinet));
            Assert.AreEqual(RoomObjectCategory.Utility, cabinet.Category);
            Assert.IsTrue(registry.TryGetDefinition("storage_box_01", out var box));
            Assert.AreEqual(RoomObjectShapeKind.StorageBox, box.ShapeKind);
            Assert.GreaterOrEqual(registry.Definitions.Count, 12);
        }

        [Test]
        public void FactorySpawnsConfiguredInstanceUnderRequestedAnchor()
        {
            var parent = new GameObject("Anchor").transform;
            var factory = new RoomObjectFactory();
            var definition = new RoomObjectDefinition
            {
                Id = "lamp_table_01",
                ShapeKind = RoomObjectShapeKind.Lamp,
                DefaultScale = Vector3.one,
                BaseColor = Color.gray,
                AccentColor = Color.white,
            };
            var placement = new RoomObjectPlacement
            {
                InstanceName = "LampTable",
                LocalPosition = new Vector3(1f, 0f, 2f),
            };

            try
            {
                var instance = factory.Spawn(definition, placement, parent);

                Assert.NotNull(instance);
                Assert.AreEqual("LampTable", instance.name);
                Assert.AreEqual(parent, instance.transform.parent);
                Assert.AreEqual(new Vector3(1f, 0f, 2f), instance.transform.localPosition);
                Assert.IsTrue(instance.transform.Cast<Transform>().Any(child => child.name == "Shade"));
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        [Test]
        public void FactoryBuildsBooksShapeForDecorClusters()
        {
            var parent = new GameObject("DecorAnchor").transform;
            var factory = new RoomObjectFactory();
            var definition = new RoomObjectDefinition
            {
                Id = "books_display_01",
                ShapeKind = RoomObjectShapeKind.Books,
                DefaultScale = Vector3.one,
                BaseColor = Color.yellow,
                AccentColor = Color.blue,
            };

            try
            {
                var instance = factory.Spawn(definition, new RoomObjectPlacement(), parent);

                Assert.NotNull(instance);
                Assert.IsTrue(instance.transform.Cast<Transform>().Any(child => child.name == "BookA"));
                Assert.IsTrue(instance.transform.Cast<Transform>().Any(child => child.name == "BookB"));
                Assert.IsTrue(instance.transform.Cast<Transform>().Any(child => child.name == "BookC"));
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        [Test]
        public void FoundationRegistryPassesPlaceholderSafeValidation()
        {
            var registry = RoomObjectRegistry.CreateFoundationMvp(RoomLayoutDefinition.CreateDefault());

            var report = RoomObjectRegistryValidator.Validate(
                registry,
                RoomObjectValidationMode.PlaceholderSafe,
                _ => false);

            Assert.AreEqual(12, report.DefinitionCount);
            Assert.AreEqual(0, report.Errors.Count);
            Assert.GreaterOrEqual(report.Infos.Count, 12);
        }

        [Test]
        public void StrictPrefabValidationFlagsMissingPrefabAssets()
        {
            var registry = RoomObjectRegistry.CreateFoundationMvp(RoomLayoutDefinition.CreateDefault());

            var report = RoomObjectRegistryValidator.Validate(
                registry,
                RoomObjectValidationMode.StrictPrefabIntake,
                _ => false);

            Assert.GreaterOrEqual(report.Errors.Count, 12);
            StringAssert.Contains("prefabPath", report.Errors[0]);
        }

        [Test]
        public void ValidatorFlagsInvalidInteractionContract()
        {
            var report = RoomObjectRegistryValidator.ValidateDefinitions(
                new[]
                {
                    new RoomObjectDefinition
                    {
                        Id = "lamp_invalid_01",
                        DisplayName = "Lamp",
                        Category = RoomObjectCategory.Lighting,
                        PrefabKey = "OBJ_Lighting_Lamp_Table_A01",
                        PrefabPath = "Assets/World/Prefabs/Lighting/OBJ_Lighting_Lamp_Table_A01.prefab",
                        AnchorType = RoomAnchorTypes.Decor,
                        InteractionType = RoomInteractionType.Focus,
                        Selectable = false,
                        Hoverable = true,
                        DefaultScale = new Vector3(1f, 1f, 1f),
                    },
                });

            Assert.AreEqual(1, report.Errors.Count);
            StringAssert.Contains("requires selectable=true", report.Errors[0]);
        }
    }
}
