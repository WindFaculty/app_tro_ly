using LocalAssistant.App;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.EditMode
{
    public class StandaloneRoomRuntimeTests
    {
        [TearDown]
        public void TearDown()
        {
            DestroyIfExists("StandaloneRoomRuntimeTestHost");
            DestroyIfExists("StandaloneRoomRuntime");
            DestroyIfExists("RoomPlaceholderStage");
            DestroyIfExists("AssistantCamera");
        }

        [Test]
        public void ComposeBuildsStandaloneRuntimeWithoutLegacyUiShell()
        {
            var host = new GameObject("StandaloneRoomRuntimeTestHost");

            var composition = StandaloneRoomCompositionRoot.Compose(host, host.transform);

            Assert.NotNull(composition);
            Assert.NotNull(composition.RoomRuntime);
            Assert.NotNull(composition.SceneStateController);
            Assert.NotNull(composition.UnityBridgeClient);
            Assert.NotNull(composition.TauriBridgeRuntime);
            Assert.NotNull(composition.SceneCamera);
            Assert.AreEqual("overview", composition.RoomRuntime.CurrentFocus);
            Assert.IsNull(Object.FindFirstObjectByType<UIDocument>());
            Assert.IsNull(GameObject.Find("AssistantUI_Toolkit"));
            Assert.IsNull(Object.FindFirstObjectByType<LocalAssistant.Core.AssistantApp>());
        }

        [Test]
        public void ComposeCreatesPlaceholderRoomZonesAndAvatarRoot()
        {
            var host = new GameObject("StandaloneRoomRuntimeTestHost");

            var composition = StandaloneRoomCompositionRoot.Compose(host, host.transform);

            Assert.NotNull(GameObject.Find("RoomPlaceholderStage"));
            Assert.NotNull(GameObject.Find("DeskZone"));
            Assert.NotNull(GameObject.Find("WardrobeZone"));
            Assert.NotNull(GameObject.Find("BedZone"));
            Assert.NotNull(composition.AvatarRoot);
            Assert.NotNull(composition.AvatarRoot.Equipment);
            Assert.NotNull(composition.AvatarConversationBridge);
        }

        [Test]
        public void SceneStateControllerMovesCameraBetweenStandaloneFocusAnchors()
        {
            var host = new GameObject("StandaloneRoomRuntimeTestHost");

            var composition = StandaloneRoomCompositionRoot.Compose(host, host.transform);
            var camera = composition.SceneCamera;

            composition.SceneStateController.ApplyPageContext("planner");
            var plannerPosition = camera.transform.position;

            composition.SceneStateController.ApplyPageContext("wardrobe");
            var wardrobePosition = camera.transform.position;

            Assert.AreEqual("wardrobe", composition.RoomRuntime.CurrentFocus);
            Assert.That(plannerPosition.x, Is.GreaterThan(0.5f));
            Assert.That(wardrobePosition.x, Is.LessThan(-0.5f));
            Assert.AreNotEqual(plannerPosition, wardrobePosition);
        }

        private static void DestroyIfExists(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
