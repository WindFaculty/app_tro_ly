using AvatarSystem;
using LocalAssistant.Audio;
using LocalAssistant.Avatar;
using LocalAssistant.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace LocalAssistant.App
{
    public sealed class StandaloneRoomComposition
    {
        public Camera SceneCamera;
        public AvatarStateMachine AvatarStateMachine;
        public AvatarConversationBridge AvatarConversationBridge;
        public AvatarRootController AvatarRoot;
        public AudioPlaybackController AudioPlaybackController;
        public LipSyncController LipSyncController;
        public AvatarRuntime AvatarRuntime;
        public AnimationRuntime AnimationRuntime;
        public LipSyncRuntime LipSyncRuntime;
        public AvatarItemRegistry AvatarItemRegistry;
        public MeshAssetRegistry MeshAssetRegistry;
        public RoomRuntime RoomRuntime;
        public InteractionRuntime InteractionRuntime;
        public SceneStateController SceneStateController;
        public UnityBridgeClient UnityBridgeClient;
        public TauriBridgeRuntime TauriBridgeRuntime;
    }

    public static class StandaloneRoomCompositionRoot
    {
        public static StandaloneRoomComposition Compose(GameObject host, Transform parent)
        {
            var stage = EnsurePlaceholderStage(parent);
            var sceneCamera = EnsureSceneCamera(parent);
            ConfigureEnvironment(parent);
            var avatarRoot = EnsureAvatarRoot(stage);
            var avatarConversationBridge = avatarRoot != null
                ? avatarRoot.ConversationBridge
                : Object.FindAnyObjectByType<AvatarConversationBridge>();

            var runtimeRoot = new GameObject("StandaloneRoomRuntime");
            runtimeRoot.transform.SetParent(parent, false);

            var avatarStateMachine = runtimeRoot.AddComponent<AvatarStateMachine>();
            var audioPlaybackController = runtimeRoot.AddComponent<AudioPlaybackController>();
            var lipSyncController = runtimeRoot.AddComponent<LipSyncController>();
            lipSyncController.BindAudioSource(audioPlaybackController.Output);

            var avatarRuntime = runtimeRoot.AddComponent<AvatarRuntime>();
            avatarRuntime.Bind(avatarStateMachine, avatarConversationBridge, avatarRoot);

            var animationRuntime = runtimeRoot.AddComponent<AnimationRuntime>();
            animationRuntime.Bind(avatarConversationBridge, avatarRoot);

            var lipSyncRuntime = runtimeRoot.AddComponent<LipSyncRuntime>();
            lipSyncRuntime.Bind(lipSyncController, avatarRoot, audioPlaybackController.Output);

            var avatarItemRegistry = runtimeRoot.AddComponent<AvatarItemRegistry>();
            avatarItemRegistry.Initialize();

            var meshAssetRegistry = runtimeRoot.AddComponent<MeshAssetRegistry>();
            meshAssetRegistry.Initialize();

            var roomRuntime = runtimeRoot.AddComponent<RoomRuntime>();
            roomRuntime.Bind(sceneCamera);
            roomRuntime.ConfigureFocusTargets(
                stage.OverviewFocusPoint,
                stage.AvatarFocusPoint,
                stage.DeskFocusPoint,
                stage.WardrobeFocusPoint);

            var interactionRuntime = runtimeRoot.AddComponent<InteractionRuntime>();
            interactionRuntime.Bind(stage.StageRoot.transform);

            var sceneStateController = runtimeRoot.AddComponent<SceneStateController>();
            sceneStateController.Bind(roomRuntime, avatarRuntime);
            sceneStateController.ApplyPageContext("dashboard");

            var unityBridgeClient = runtimeRoot.AddComponent<UnityBridgeClient>();
            unityBridgeClient.Bind(
                sceneStateController,
                avatarRuntime,
                animationRuntime,
                lipSyncRuntime,
                roomRuntime,
                interactionRuntime,
                meshAssetRegistry,
                avatarItemRegistry);

            var tauriBridgeRuntime = runtimeRoot.AddComponent<TauriBridgeRuntime>();
            tauriBridgeRuntime.Bind(unityBridgeClient, avatarRuntime, interactionRuntime);

            return new StandaloneRoomComposition
            {
                SceneCamera = sceneCamera,
                AvatarStateMachine = avatarStateMachine,
                AvatarConversationBridge = avatarConversationBridge,
                AvatarRoot = avatarRoot,
                AudioPlaybackController = audioPlaybackController,
                LipSyncController = lipSyncController,
                AvatarRuntime = avatarRuntime,
                AnimationRuntime = animationRuntime,
                LipSyncRuntime = lipSyncRuntime,
                AvatarItemRegistry = avatarItemRegistry,
                MeshAssetRegistry = meshAssetRegistry,
                RoomRuntime = roomRuntime,
                InteractionRuntime = interactionRuntime,
                SceneStateController = sceneStateController,
                UnityBridgeClient = unityBridgeClient,
                TauriBridgeRuntime = tauriBridgeRuntime,
            };
        }

        private static Camera EnsureSceneCamera(Transform parent)
        {
            var sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                var cameraGo = new GameObject("AssistantCamera", typeof(Camera), typeof(AudioListener));
                cameraGo.transform.SetParent(parent, false);
                sceneCamera = cameraGo.GetComponent<Camera>();
                cameraGo.tag = "MainCamera";
            }

            sceneCamera.orthographic = true;
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.transform.position = new Vector3(0f, 1.2f, -10f);
            sceneCamera.transform.rotation = Quaternion.identity;
            sceneCamera.backgroundColor = new Color(0.88f, 0.78f, 0.68f, 1f);
            sceneCamera.orthographicSize = 5.2f;
            return sceneCamera;
        }

        private static void ConfigureEnvironment(Transform parent)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.84f, 0.76f, 0.68f, 1f);

            if (Object.FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("StandaloneRoomDirectionalLight", typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.86f, 1f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
        }

        private static StandaloneRoomStage EnsurePlaceholderStage(Transform parent)
        {
            var stageRoot = GameObject.Find("RoomPlaceholderStage");
            if (stageRoot == null)
            {
                stageRoot = new GameObject("RoomPlaceholderStage");
                stageRoot.transform.SetParent(parent, false);

                CreateCube(stageRoot.transform, "RoomFloor", new Vector3(0f, -0.05f, 0f), new Vector3(5f, 0.1f, 4f));
                CreateCube(stageRoot.transform, "BackWall", new Vector3(0f, 1.2f, 1.95f), new Vector3(5f, 2.4f, 0.1f));
                CreateCube(stageRoot.transform, "LeftWall", new Vector3(-2.45f, 1.2f, 0f), new Vector3(0.1f, 2.4f, 4f));
                CreateCube(stageRoot.transform, "RightWall", new Vector3(2.45f, 1.2f, 0f), new Vector3(0.1f, 2.4f, 4f));
                CreateCube(stageRoot.transform, "DeskZone", new Vector3(1.35f, 0.45f, 1.1f), new Vector3(1.2f, 0.9f, 0.55f));
                CreateCube(stageRoot.transform, "BedZone", new Vector3(-1.45f, 0.3f, 1.05f), new Vector3(1.6f, 0.6f, 2f));
                CreateCube(stageRoot.transform, "WardrobeZone", new Vector3(-1.55f, 1f, -1.2f), new Vector3(1.1f, 2f, 0.55f));
                CreateCube(stageRoot.transform, "BookshelfZone", new Vector3(1.6f, 1.05f, -1.15f), new Vector3(0.85f, 2.1f, 0.4f));
                CreateCube(stageRoot.transform, "WindowFrame", new Vector3(0f, 1.35f, 1.89f), new Vector3(1.5f, 1.1f, 0.05f));
            }

            var overviewFocusPoint = FindOrCreateMarker(stageRoot.transform, "OverviewFocusPoint", new Vector3(0f, 1.2f, 0f));
            var avatarFocusPoint = FindOrCreateMarker(stageRoot.transform, "AvatarFocusPoint", new Vector3(0f, 1.2f, 0f));
            var deskFocusPoint = FindOrCreateMarker(stageRoot.transform, "DeskFocusPoint", new Vector3(1.35f, 1.05f, 0f));
            var wardrobeFocusPoint = FindOrCreateMarker(stageRoot.transform, "WardrobeFocusPoint", new Vector3(-1.5f, 1.05f, 0f));

            return new StandaloneRoomStage
            {
                StageRoot = stageRoot,
                OverviewFocusPoint = overviewFocusPoint,
                AvatarFocusPoint = avatarFocusPoint,
                DeskFocusPoint = deskFocusPoint,
                WardrobeFocusPoint = wardrobeFocusPoint,
            };
        }

        private static AvatarRootController EnsureAvatarRoot(StandaloneRoomStage stage)
        {
            var avatarRoot = Object.FindAnyObjectByType<AvatarRootController>();
            if (avatarRoot != null)
            {
                avatarRoot.Initialize();
                return avatarRoot;
            }

            var avatarObject = new GameObject("AvatarPlaceholder");
            avatarObject.transform.SetParent(stage.StageRoot.transform, false);
            avatarObject.transform.localPosition = Vector3.zero;

            avatarObject.AddComponent<AvatarEquipmentManager>();
            avatarObject.AddComponent<AvatarConversationBridge>();
            avatarRoot = avatarObject.AddComponent<AvatarRootController>();

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "AvatarBody";
            body.transform.SetParent(avatarObject.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.8f, 0.9f, 0.75f);

            avatarRoot.Initialize();
            return avatarRoot;
        }

        private static Transform FindOrCreateMarker(Transform parent, string name, Vector3 localPosition)
        {
            var marker = parent.Find(name);
            if (marker == null)
            {
                marker = new GameObject(name).transform;
                marker.SetParent(parent, false);
            }

            marker.localPosition = localPosition;
            return marker;
        }

        private static void CreateCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
        {
            if (parent.Find(name) != null)
            {
                return;
            }

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
        }

        private sealed class StandaloneRoomStage
        {
            public GameObject StageRoot;
            public Transform OverviewFocusPoint;
            public Transform AvatarFocusPoint;
            public Transform DeskFocusPoint;
            public Transform WardrobeFocusPoint;
        }
    }
}
