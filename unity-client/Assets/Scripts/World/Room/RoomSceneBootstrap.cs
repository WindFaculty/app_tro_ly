using UnityEngine;

namespace LocalAssistant.World.Room
{
    public sealed class RoomSceneBootstrap : MonoBehaviour
    {
        private const string DefaultRoomTemplateResourcePath = "World/Rooms/Room_Base";

        [SerializeField] private RoomWorldController roomWorldController;
        [SerializeField] private GameObject roomTemplatePrefab;
        [SerializeField] private string roomTemplateResourcePath = DefaultRoomTemplateResourcePath;

        private GameObject roomTemplateInstance;

        public RoomWorldController Bootstrap(Camera stageCamera, RoomLayoutDefinition layoutDefinition = null)
        {
            if (roomWorldController == null)
            {
                var roomGo = new GameObject("RoomWorld");
                roomGo.transform.SetParent(transform, false);
                roomWorldController = roomGo.AddComponent<RoomWorldController>();
            }

            var layout = layoutDefinition ?? RoomLayoutDefinition.CreateDefault();
            EnsureRoomTemplate(layout);
            roomWorldController.Initialize(layout);
            roomWorldController.ConfigureStageCamera(stageCamera);
            return roomWorldController;
        }

        private void EnsureRoomTemplate(RoomLayoutDefinition layout)
        {
            if (roomTemplateInstance != null)
            {
                roomWorldController.BindRoomTemplate(roomTemplateInstance.transform);
                return;
            }

            var templatePrefab = ResolveTemplatePrefab(layout);
            if (templatePrefab == null)
            {
                roomWorldController.BindRoomTemplate(null);
                return;
            }

            roomTemplateInstance = Instantiate(templatePrefab, roomWorldController.transform);
            roomTemplateInstance.name = templatePrefab.name;
            roomWorldController.BindRoomTemplate(roomTemplateInstance.transform);
        }

        private GameObject ResolveTemplatePrefab(RoomLayoutDefinition layout)
        {
            if (roomTemplatePrefab != null)
            {
                return roomTemplatePrefab;
            }

            var resourcePath = !string.IsNullOrWhiteSpace(layout?.TemplateResourcePath)
                ? layout.TemplateResourcePath
                : roomTemplateResourcePath;
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                resourcePath = DefaultRoomTemplateResourcePath;
            }

            return Resources.Load<GameObject>(resourcePath);
        }
    }
}
