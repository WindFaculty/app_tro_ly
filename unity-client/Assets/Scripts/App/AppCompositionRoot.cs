using LocalAssistant.Audio;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using LocalAssistant.Notifications;
using LocalAssistant.World.Interaction;
using LocalAssistant.World.Room;
using UnityEngine;
using AvatarSystem;

namespace LocalAssistant.App
{
    public sealed class AppComposition
    {
        public AssistantUiRefs Ui;
        public AvatarStateMachine AvatarStateMachine;
        public AvatarConversationBridge AvatarConversationBridge;
        public AvatarRootController AvatarRootController;
        public CharacterRoomBridge CharacterRoomBridge;
        public LipSyncController LipSyncController;
        public AudioPlaybackController AudioPlaybackController;
        public SubtitlePresenter SubtitlePresenter;
        public ReminderPresenter ReminderPresenter;
        public RoomWorldController RoomWorldController;
        public RoomInteractionController RoomInteractionController;
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

            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.14f, 0.08f, 0.05f, 1f);

            var runtimeRoot = new GameObject("AssistantRuntime");
            runtimeRoot.transform.SetParent(parent, false);

            var avatarStateMachine = runtimeRoot.AddComponent<AvatarStateMachine>();
            var audioPlaybackController = runtimeRoot.AddComponent<AudioPlaybackController>();
            var lipSyncController = runtimeRoot.AddComponent<LipSyncController>();
            lipSyncController.BindAudioSource(audioPlaybackController.Output);
            var roomBootstrap = runtimeRoot.AddComponent<RoomSceneBootstrap>();
            var roomWorldController = roomBootstrap.Bootstrap(sceneCamera, RoomLayoutDefinition.CreateDefault());
            var avatarConversationBridge = Object.FindFirstObjectByType<AvatarConversationBridge>();
            var avatarRootController = Object.FindFirstObjectByType<AvatarRootController>();

            var ui = UiDocumentLoader.Load(parent);
            var roomInteractionController = runtimeRoot.AddComponent<RoomInteractionController>();
            roomInteractionController.Bind(sceneCamera, () => ResolveStageViewportRect(ui.Home));
            var characterRoomBridge = runtimeRoot.AddComponent<CharacterRoomBridge>();
            characterRoomBridge.Bind(
                avatarStateMachine,
                roomWorldController,
                roomInteractionController,
                avatarConversationBridge,
                avatarRootController);
            var subtitlePresenter = host.AddComponent<SubtitlePresenter>();
            subtitlePresenter.Bind(ui.Subtitle.SubtitleText, ui.Subtitle.SubtitleCard);

            var reminderPresenter = host.AddComponent<ReminderPresenter>();
            reminderPresenter.Bind(ui.Reminder.ReminderText, ui.Reminder.ReminderCard);

            return new AppComposition
            {
                Ui = ui,
                AvatarStateMachine = avatarStateMachine,
                AvatarConversationBridge = avatarConversationBridge,
                AvatarRootController = avatarRootController,
                CharacterRoomBridge = characterRoomBridge,
                LipSyncController = lipSyncController,
                AudioPlaybackController = audioPlaybackController,
                SubtitlePresenter = subtitlePresenter,
                ReminderPresenter = reminderPresenter,
                RoomWorldController = roomWorldController,
                RoomInteractionController = roomInteractionController,
            };
        }

        private static Rect ResolveStageViewportRect(HomeScreenRefs home)
        {
            if (home?.HomeStageViewport == null)
            {
                return new Rect(0f, 0f, Screen.width, Screen.height);
            }

            var worldBound = home.HomeStageViewport.worldBound;
            if (worldBound.width <= 0f || worldBound.height <= 0f)
            {
                return new Rect(0f, 0f, Screen.width, Screen.height);
            }

            return new Rect(
                worldBound.xMin,
                Screen.height - worldBound.yMax,
                worldBound.width,
                worldBound.height);
        }
    }
}
