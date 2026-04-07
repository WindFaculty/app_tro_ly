using LocalAssistant.Audio;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using LocalAssistant.Notifications;
using LocalAssistant.Runtime;
using UnityEngine;
using AvatarSystem;

namespace LocalAssistant.App
{
    public sealed class AppComposition
    {
        public AssistantUiRefs Ui;
        public AvatarStateMachine AvatarStateMachine;
        public AvatarConversationBridge AvatarConversationBridge;
        public LipSyncController LipSyncController;
        public AudioPlaybackController AudioPlaybackController;
        public SubtitlePresenter SubtitlePresenter;
        public ReminderPresenter ReminderPresenter;
        public AvatarRuntime AvatarRuntime;
        public AnimationRuntime AnimationRuntime;
        public LipSyncRuntime LipSyncRuntime;
        public RoomRuntime RoomRuntime;
        public InteractionRuntime InteractionRuntime;
        public SceneStateController SceneStateController;
        public UnityBridgeClient UnityBridgeClient;
        public TauriBridgeRuntime TauriBridgeRuntime;
    }

    public static class AppCompositionRoot
    {
        public static AppComposition Compose(GameObject host, Transform parent)
        {
            var sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                var cameraGo = new GameObject("AssistantCamera", typeof(Camera), typeof(AudioListener));
                sceneCamera = cameraGo.GetComponent<Camera>();
            }

            sceneCamera.orthographic = true;
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.transform.position = new Vector3(0f, 0f, -10f);
            sceneCamera.transform.rotation = Quaternion.identity;
            sceneCamera.backgroundColor = new Color(0.14f, 0.08f, 0.05f, 1f);

            var runtimeRoot = new GameObject("AssistantRuntime");
            runtimeRoot.transform.SetParent(parent, false);

            var avatarStateMachine = runtimeRoot.AddComponent<AvatarStateMachine>();
            var audioPlaybackController = runtimeRoot.AddComponent<AudioPlaybackController>();
            var lipSyncController = runtimeRoot.AddComponent<LipSyncController>();
            lipSyncController.BindAudioSource(audioPlaybackController.Output);
            var avatarRoot = Object.FindFirstObjectByType<AvatarRootController>();
            var avatarConversationBridge = Object.FindFirstObjectByType<AvatarConversationBridge>();
            var avatarRuntime = runtimeRoot.AddComponent<AvatarRuntime>();
            avatarRuntime.Bind(avatarStateMachine, avatarConversationBridge, avatarRoot);
            var animationRuntime = runtimeRoot.AddComponent<AnimationRuntime>();
            animationRuntime.Bind(avatarConversationBridge, avatarRoot);
            var lipSyncRuntime = runtimeRoot.AddComponent<LipSyncRuntime>();
            lipSyncRuntime.Bind(lipSyncController, avatarRoot, audioPlaybackController.Output);
            var roomRuntime = runtimeRoot.AddComponent<RoomRuntime>();
            roomRuntime.Bind(sceneCamera);
            var interactionRuntime = runtimeRoot.AddComponent<InteractionRuntime>();
            interactionRuntime.Bind(runtimeRoot.transform);
            var sceneStateController = runtimeRoot.AddComponent<SceneStateController>();
            sceneStateController.Bind(roomRuntime, avatarRuntime);
            sceneStateController.ApplyPageContext("dashboard");
            var unityBridgeClient = runtimeRoot.AddComponent<UnityBridgeClient>();
            unityBridgeClient.Bind(sceneStateController, avatarRuntime, animationRuntime, lipSyncRuntime, roomRuntime, interactionRuntime);
            var tauriBridgeRuntime = runtimeRoot.AddComponent<TauriBridgeRuntime>();
            tauriBridgeRuntime.Bind(unityBridgeClient, avatarRuntime, interactionRuntime);

            var ui = UiDocumentLoader.Load(parent);
            var subtitlePresenter = host.AddComponent<SubtitlePresenter>();
            subtitlePresenter.Bind(ui.Subtitle.SubtitleText, ui.Subtitle.SubtitleCard);

            var reminderPresenter = host.AddComponent<ReminderPresenter>();
            reminderPresenter.Bind(ui.Reminder.ReminderText, ui.Reminder.ReminderCard);

            return new AppComposition
            {
                Ui = ui,
                AvatarStateMachine = avatarStateMachine,
                AvatarConversationBridge = avatarConversationBridge,
                LipSyncController = lipSyncController,
                AudioPlaybackController = audioPlaybackController,
                SubtitlePresenter = subtitlePresenter,
                ReminderPresenter = reminderPresenter,
                AvatarRuntime = avatarRuntime,
                AnimationRuntime = animationRuntime,
                LipSyncRuntime = lipSyncRuntime,
                RoomRuntime = roomRuntime,
                InteractionRuntime = interactionRuntime,
                SceneStateController = sceneStateController,
                UnityBridgeClient = unityBridgeClient,
                TauriBridgeRuntime = tauriBridgeRuntime,
            };
        }
    }
}
