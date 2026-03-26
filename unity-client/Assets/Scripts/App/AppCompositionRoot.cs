using LocalAssistant.Audio;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using LocalAssistant.Notifications;
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

            var ui = UiFactory.Build(parent);
            var subtitlePresenter = host.AddComponent<SubtitlePresenter>();
            subtitlePresenter.Bind(ui.SubtitleText, ui.SubtitleCard);

            var reminderPresenter = host.AddComponent<ReminderPresenter>();
            reminderPresenter.Bind(ui.ReminderText, ui.ReminderCard);

            return new AppComposition
            {
                Ui = ui,
                AvatarStateMachine = avatarStateMachine,
                AvatarConversationBridge = Object.FindFirstObjectByType<AvatarConversationBridge>(),
                LipSyncController = lipSyncController,
                AudioPlaybackController = audioPlaybackController,
                SubtitlePresenter = subtitlePresenter,
                ReminderPresenter = reminderPresenter,
            };
        }
    }
}
