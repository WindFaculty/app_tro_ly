using LocalAssistant.World.Room;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class RoomWorldFoundationTests
    {
        [Test]
        public void DefaultLayoutProvidesStableFoundationData()
        {
            var layout = RoomLayoutDefinition.CreateDefault();

            Assert.AreEqual("room_base", layout.LayoutId);
            Assert.AreEqual("World/Rooms/Room_Base", layout.TemplateResourcePath);
            Assert.Greater(layout.RoomSize.x, 0f);
            Assert.Greater(layout.RoomSize.y, 0f);
            Assert.Greater(layout.RoomSize.z, 0f);
            Assert.GreaterOrEqual(layout.ObjectPlacements.Length, 12);
            Assert.AreNotEqual(layout.AvatarSpawnPosition, layout.CameraAnchorPosition);
        }

        [Test]
        public void BootstrapCreatesRoomRootAnchorsAndConfiguresPerspectiveCamera()
        {
            var host = new GameObject("RoomBootstrapHost");
            var cameraGo = new GameObject("RoomBootstrapCamera");
            var sceneCamera = cameraGo.AddComponent<Camera>();
            var bootstrap = host.AddComponent<RoomSceneBootstrap>();

            try
            {
                var layout = RoomLayoutDefinition.CreateDefault();
                var controller = bootstrap.Bootstrap(sceneCamera, layout);

                Assert.NotNull(controller);
                Assert.AreEqual("room_base", controller.CurrentLayoutId);
                Assert.NotNull(Resources.Load<GameObject>(layout.TemplateResourcePath));
                Assert.NotNull(controller.AvatarSpawnPoint);
                Assert.NotNull(controller.CameraAnchor);
                Assert.NotNull(controller.DeskAnchor);
                Assert.NotNull(controller.RestAnchor);
                Assert.NotNull(controller.DecorAnchor);
                Assert.IsFalse(sceneCamera.orthographic);
                Assert.Greater(sceneCamera.fieldOfView, 0f);
                Assert.NotNull(host.transform.Find("RoomWorld"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/ShellGeometry"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/ShellGeometry/Floor"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/AvatarSpawnPoint"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/DeskAnchor/DeskMain"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/RestAnchor/RestSideTable"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/DecorAnchor/LampTable"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/DecorAnchor/BooksDisplay"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/DecorAnchor/StorageCabinet"));
                Assert.NotNull(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/DecorAnchor/StorageBox"));
                Assert.IsTrue(controller.HotspotsVisible);

                controller.SetHotspotsVisible(false);

                Assert.IsFalse(controller.HotspotsVisible);
                Assert.IsFalse(host.transform.Find("RoomWorld/CharacterSpaceRoomRoot/DeskAnchor/DeskAnchorMarker").gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(host);
            }
        }
    }
}
