using LocalAssistant.World.Interaction;
using LocalAssistant.World.Objects;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class RoomInteractionControllerTests
    {
        [Test]
        public void InteractionControllerSelectsFocusObjectAndUpdatesSnapshot()
        {
            var cameraGo = new GameObject("StageCamera");
            var controllerGo = new GameObject("RoomInteraction");
            var anchorGo = new GameObject("RoomAnchor");
            var camera = cameraGo.AddComponent<Camera>();
            var controller = controllerGo.AddComponent<RoomInteractionController>();
            camera.transform.position = new Vector3(0f, 1.3f, -5.6f);
            camera.transform.LookAt(new Vector3(0f, 0.8f, 0f));

            try
            {
                controller.Bind(camera, () => new Rect(0f, 0f, 1920f, 1080f));
                var instance = new RoomObjectFactory().Spawn(
                    new RoomObjectDefinition
                    {
                        Id = "lamp_focus_01",
                        DisplayName = "Focus Lamp",
                        Category = RoomObjectCategory.Lighting,
                        ShapeKind = RoomObjectShapeKind.Lamp,
                        InteractionType = RoomInteractionType.Focus,
                        Selectable = true,
                        Hoverable = true,
                        DefaultScale = Vector3.one,
                        BaseColor = Color.gray,
                        AccentColor = Color.white,
                    },
                    new RoomObjectPlacement
                    {
                        InstanceName = "FocusLamp",
                        LocalPosition = Vector3.zero,
                    },
                    anchorGo.transform);

                Assert.NotNull(instance.GetComponent<InteractableObject>());
                var previousRotation = camera.transform.rotation;

                controller.ProcessRay(new Ray(camera.transform.position, camera.transform.forward), true);

                Assert.IsTrue(controller.CurrentSelection.HasSelection);
                Assert.AreEqual("Focus Lamp", controller.CurrentSelection.DisplayName);
                StringAssert.Contains("Focus stage camera", controller.CurrentSelection.ActionText);
                Assert.AreNotEqual(previousRotation, camera.transform.rotation);
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(controllerGo);
                Object.DestroyImmediate(anchorGo);
            }
        }

        [Test]
        public void InteractionControllerIgnoresPointerOutsideViewport()
        {
            var cameraGo = new GameObject("StageCamera");
            var controllerGo = new GameObject("RoomInteraction");
            var camera = cameraGo.AddComponent<Camera>();
            var controller = controllerGo.AddComponent<RoomInteractionController>();
            camera.transform.position = new Vector3(0f, 1.3f, -5.6f);
            camera.transform.LookAt(new Vector3(0f, 0.8f, 0f));

            try
            {
                controller.Bind(camera, () => new Rect(100f, 100f, 200f, 200f));
                controller.ProcessPointer(new Vector2(20f, 20f), true);

                Assert.IsFalse(controller.CurrentSelection.HasSelection);
                Assert.AreEqual("No object selected", controller.CurrentSelection.DisplayName);
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(controllerGo);
            }
        }
    }
}
