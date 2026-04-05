using LocalAssistant.Avatar;
using LocalAssistant.Core;
using LocalAssistant.World.Interaction;
using LocalAssistant.World.Objects;
using LocalAssistant.World.Room;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class CharacterRoomBridgeTests
    {
        [Test]
        public void CharacterRoomBridgeCreatesProxyAtRoomSpawn()
        {
            var roomHost = new GameObject("RoomHost");
            var roomWorld = roomHost.AddComponent<RoomWorldController>();
            var avatarStateMachine = roomHost.AddComponent<AvatarStateMachine>();
            var interactionCameraGo = new GameObject("InteractionCamera");
            var interactionCamera = interactionCameraGo.AddComponent<Camera>();
            var interactionController = roomHost.AddComponent<RoomInteractionController>();
            var bridge = roomHost.AddComponent<CharacterRoomBridge>();

            try
            {
                var layout = RoomLayoutDefinition.CreateDefault();
                layout.ObjectPlacements = System.Array.Empty<RoomObjectPlacement>();
                roomWorld.Initialize(layout);
                interactionController.Bind(interactionCamera);
                bridge.Bind(avatarStateMachine, roomWorld, interactionController, null, null);

                Assert.IsTrue(bridge.UsingProxyAvatar);
                Assert.NotNull(bridge.ActiveAvatarRoot);
                Assert.AreEqual(roomWorld.AvatarSpawnPoint.position, bridge.ActiveAvatarRoot.position);
            }
            finally
            {
                Object.DestroyImmediate(interactionCameraGo);
                Object.DestroyImmediate(roomHost);
            }
        }

        [Test]
        public void CharacterRoomBridgeTurnsProxyTowardSelectedRoomObject()
        {
            var roomHost = new GameObject("RoomHost");
            var roomWorld = roomHost.AddComponent<RoomWorldController>();
            var avatarStateMachine = roomHost.AddComponent<AvatarStateMachine>();
            var interactionCameraGo = new GameObject("InteractionCamera");
            var interactionCamera = interactionCameraGo.AddComponent<Camera>();
            var interactionController = roomHost.AddComponent<RoomInteractionController>();
            var bridge = roomHost.AddComponent<CharacterRoomBridge>();

            try
            {
                var layout = RoomLayoutDefinition.CreateDefault();
                layout.ObjectPlacements = System.Array.Empty<RoomObjectPlacement>();
                roomWorld.Initialize(layout);
                interactionCamera.transform.position = new Vector3(0f, 1.85f, -6.2f);
                interactionCamera.transform.LookAt(new Vector3(0f, 1.25f, 0.35f));
                interactionController.Bind(interactionCamera, () => new Rect(0f, 0f, 1920f, 1080f));
                bridge.Bind(avatarStateMachine, roomWorld, interactionController, null, null);
                var startForward = bridge.ActiveAvatarRoot.forward;

                var focusAnchor = new GameObject("FocusAnchor").transform;
                focusAnchor.position = roomWorld.AvatarSpawnPoint.position + new Vector3(2.1f, 0.8f, 0.4f);
                new RoomObjectFactory().Spawn(
                    new RoomObjectDefinition
                    {
                        Id = "desk_focus_01",
                        DisplayName = "Desk Focus",
                        Category = RoomObjectCategory.Furniture,
                        ShapeKind = RoomObjectShapeKind.Desk,
                        InteractionType = RoomInteractionType.Inspect,
                        Selectable = true,
                        Hoverable = true,
                        DefaultScale = Vector3.one,
                        BaseColor = Color.gray,
                        AccentColor = Color.black,
                    },
                    new RoomObjectPlacement
                    {
                        InstanceName = "DeskFocus",
                    },
                    focusAnchor);

                bridge.SetAttentionTarget(focusAnchor.position);
                Assert.AreNotEqual(startForward, bridge.ActiveAvatarRoot.forward);

                Object.DestroyImmediate(focusAnchor.gameObject);
            }
            finally
            {
                Object.DestroyImmediate(interactionCameraGo);
                Object.DestroyImmediate(roomHost);
            }
        }
    }
}
