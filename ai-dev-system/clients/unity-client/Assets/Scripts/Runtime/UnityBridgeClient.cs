using System;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class UnityBridgeClient : MonoBehaviour
    {
        private SceneStateController sceneStateController;
        private AvatarRuntime avatarRuntime;
        private AnimationRuntime animationRuntime;
        private LipSyncRuntime lipSyncRuntime;
        private RoomRuntime roomRuntime;
        private InteractionRuntime interactionRuntime;

        public event Action<string> CommandHandled;
        public event Action<string> CommandRejected;

        public void Bind(
            SceneStateController sceneState,
            AvatarRuntime avatar,
            AnimationRuntime animation,
            LipSyncRuntime lipSync,
            RoomRuntime room,
            InteractionRuntime interaction)
        {
            sceneStateController = sceneState;
            avatarRuntime = avatar;
            animationRuntime = animation;
            lipSyncRuntime = lipSync;
            roomRuntime = room;
            interactionRuntime = interaction;
        }

        public bool ApplyJsonCommand(string json)
        {
            try
            {
                return ApplyEnvelope(UnityJson.Deserialize<UnityBridgeCommandEnvelope>(json));
            }
            catch (Exception exception)
            {
                Reject($"Unity bridge command parse failed: {exception.Message}");
                return false;
            }
        }

        public bool ApplyEnvelope(UnityBridgeCommandEnvelope envelope)
        {
            if (envelope == null)
            {
                Reject("Unity bridge envelope is empty.");
                return false;
            }

            var command = envelope.payload ?? new UnityBridgeCommandPayload();
            return ApplyCommand(envelope.type, command);
        }

        public bool ApplyCommand(string commandType, UnityBridgeCommandPayload command)
        {
            if (command == null || string.IsNullOrWhiteSpace(commandType))
            {
                Reject("Unity bridge command is empty.");
                return false;
            }

            var handled = commandType switch
            {
                "app.pageChanged" => HandlePageChanged(command),
                "avatar.setMood" => HandleAvatarMood(command),
                "avatar.playEmote" => HandlePlayEmote(command),
                "avatar.speakStart" => HandleSpeakStart(),
                "avatar.speakStop" => HandleSpeakStop(),
                "avatar.setIdleState" => HandleSetIdle(),
                "avatar.setListeningState" => HandleSetListening(),
                "room.setCameraFocus" => HandleSetCameraFocus(command),
                "room.focusObject" => HandleFocusObject(command),
                "wardrobe.equipItem" => HandleEquipItem(command),
                "avatar.equipItem" => HandleEquipItem(command),
                _ => false,
            };

            if (!handled)
            {
                Reject($"Unsupported Unity bridge command '{commandType}'.");
                return false;
            }

            CommandHandled?.Invoke(commandType);
            return true;
        }

        private bool HandlePageChanged(UnityBridgeCommandPayload command)
        {
            if (string.IsNullOrWhiteSpace(command.page))
            {
                return false;
            }

            sceneStateController?.ApplyPageContext(command.page);
            return true;
        }

        private bool HandleAvatarMood(UnityBridgeCommandPayload command)
        {
            var backendState = string.IsNullOrWhiteSpace(command.backend_state) ? command.mood : command.backend_state;
            avatarRuntime?.SetMood(backendState, command.animation_hint);
            return !string.IsNullOrWhiteSpace(backendState);
        }

        private bool HandlePlayEmote(UnityBridgeCommandPayload command)
        {
            if (string.IsNullOrWhiteSpace(command.emote))
            {
                return false;
            }

            animationRuntime?.PlayEmoteTrigger(command.emote);
            return true;
        }

        private bool HandleSpeakStart()
        {
            lipSyncRuntime?.StartSpeech();
            avatarRuntime?.SetTalkingState();
            return true;
        }

        private bool HandleSpeakStop()
        {
            lipSyncRuntime?.StopSpeech();
            avatarRuntime?.SetIdleState();
            return true;
        }

        private bool HandleSetIdle()
        {
            avatarRuntime?.SetIdleState();
            return true;
        }

        private bool HandleSetListening()
        {
            avatarRuntime?.SetListeningState();
            return true;
        }

        private bool HandleSetCameraFocus(UnityBridgeCommandPayload command)
        {
            if (string.IsNullOrWhiteSpace(command.focus))
            {
                return false;
            }

            roomRuntime?.SetFocusPreset(command.focus);
            return true;
        }

        private bool HandleFocusObject(UnityBridgeCommandPayload command)
        {
            return interactionRuntime != null && interactionRuntime.FocusObject(command.object_name);
        }

        private bool HandleEquipItem(UnityBridgeCommandPayload command)
        {
            if (string.IsNullOrWhiteSpace(command.item_id))
            {
                Reject("Wardrobe equip command chua co item_id nen chua the apply vao AvatarEquipmentManager.");
                return false;
            }

            Reject("Wardrobe equip command da co schema typed, nhung item registry/runtime mapping van la planned work.");
            return false;
        }

        private void Reject(string reason)
        {
            Debug.LogWarning($"[UnityBridgeClient] {reason}");
            CommandRejected?.Invoke(reason);
        }
    }
}
