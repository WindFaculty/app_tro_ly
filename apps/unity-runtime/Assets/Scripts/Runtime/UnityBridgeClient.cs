using System;
using AvatarSystem;
using AvatarSystem.Data;
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
        private MeshAssetRegistry meshAssetRegistry;
        private AvatarItemRegistry avatarItemRegistry;
        private bool lastCommandExplicitlyRejected;

        public event Action<string> CommandHandled;
        public event Action<string> CommandRejected;

        public void Bind(
            SceneStateController sceneState,
            AvatarRuntime avatar,
            AnimationRuntime animation,
            LipSyncRuntime lipSync,
            RoomRuntime room,
            InteractionRuntime interaction,
            MeshAssetRegistry assetRegistry,
            AvatarItemRegistry itemRegistry)
        {
            sceneStateController = sceneState;
            avatarRuntime = avatar;
            animationRuntime = animation;
            lipSyncRuntime = lipSync;
            roomRuntime = room;
            interactionRuntime = interaction;
            meshAssetRegistry = assetRegistry;
            avatarItemRegistry = itemRegistry;
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

            lastCommandExplicitlyRejected = false;
            var result = commandType switch
            {
                "app.pageChanged" => CreateCommandResult(HandlePageChanged(command)),
                "avatar.setMood" => CreateCommandResult(HandleAvatarMood(command)),
                "avatar.playEmote" => CreateCommandResult(HandlePlayEmote(command)),
                "avatar.speakStart" => CreateCommandResult(HandleSpeakStart()),
                "avatar.speakStop" => CreateCommandResult(HandleSpeakStop()),
                "avatar.setIdleState" => CreateCommandResult(HandleSetIdle()),
                "avatar.setListeningState" => CreateCommandResult(HandleSetListening()),
                "room.setCameraFocus" => CreateCommandResult(HandleSetCameraFocus(command)),
                "room.focusObject" => CreateCommandResult(HandleFocusObject(command)),
                "wardrobe.equipItem" => CreateCommandResult(HandleEquipItem(command)),
                "avatar.equipItem" => CreateCommandResult(HandleEquipItem(command)),
                _ => (recognized: false, handled: false),
            };

            if (!result.recognized)
            {
                Reject($"Unsupported Unity bridge command '{commandType}'.");
                return false;
            }

            if (!result.handled)
            {
                if (!lastCommandExplicitlyRejected)
                {
                    Reject($"Unity bridge command '{commandType}' payload was invalid.");
                }

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
            if (interactionRuntime != null && interactionRuntime.FocusObject(command.object_name))
            {
                return true;
            }

            if (meshAssetRegistry != null &&
                meshAssetRegistry.TryApplyRoomFocusPreset(command.object_name, roomRuntime, out var roomEntry))
            {
                return roomEntry != null;
            }

            Reject($"Room focus target '{command.object_name}' did not match a scene object or a registry-backed room asset.");
            return false;
        }

        private bool HandleEquipItem(UnityBridgeCommandPayload command)
        {
            if (string.IsNullOrWhiteSpace(command.item_id))
            {
                Reject("Wardrobe equip command chua co item_id nen chua the apply vao AvatarEquipmentManager.");
                return false;
            }

            if (!TryResolveAvatarItem(command.item_id, out var item, out var foundation))
            {
                return false;
            }

            if (!IsSlotCompatible(command.slot, item.slotType))
            {
                Reject(
                    $"Wardrobe key '{command.item_id}' resolved to runtime item '{item.itemId}', " +
                    $"nhung command slot '{command.slot}' khong hop voi {item.slotType}.");
                return false;
            }

            if (foundation != null &&
                !string.IsNullOrWhiteSpace(foundation.Slot) &&
                !IsSlotCompatible(foundation.Slot, item.slotType))
            {
                Reject(
                    $"Wardrobe key '{command.item_id}' matched handoff manifest '{foundation.AssetId}', " +
                    $"nhung manifest slot '{foundation.Slot}' khong hop voi runtime item '{item.itemId}' ({item.slotType}).");
                return false;
            }

            if (avatarRuntime == null)
            {
                Reject(
                    $"Wardrobe key '{command.item_id}' da resolve sang runtime item '{item.itemId}', " +
                    "nhung AvatarRuntime chua duoc bind.");
                return false;
            }

            if (!avatarRuntime.EquipItem(item))
            {
                Reject(
                    $"Wardrobe key '{command.item_id}' da resolve sang runtime item '{item.itemId}', " +
                    "nhung AvatarRuntime tu choi equip.");
                return false;
            }

            return true;
        }

        private bool TryResolveAvatarItem(
            string lookupKey,
            out AvatarItemDefinition item,
            out WardrobeFoundationRegistryEntry foundation)
        {
            foundation = null;

            if (avatarItemRegistry != null &&
                avatarItemRegistry.TryGetItem(lookupKey, out item))
            {
                return true;
            }

            if (meshAssetRegistry != null &&
                meshAssetRegistry.TryGetWardrobeFoundation(lookupKey, out foundation))
            {
                if (avatarItemRegistry != null &&
                    avatarItemRegistry.TryResolveFoundation(foundation, out item))
                {
                    return true;
                }

                Reject(
                    $"Wardrobe key '{lookupKey}' matched handoff manifest '{foundation.AssetId}', " +
                    "nhung AvatarItemRegistry chua co AvatarItemDefinition tuong ung.");
                item = null;
                return false;
            }

            Reject(
                $"Wardrobe key '{lookupKey}' chua match runtime item hoac registry-backed handoff manifest nao, " +
                "nen runtime equip mapping van chua the apply.");
            item = null;
            return false;
        }

        private static bool IsSlotCompatible(string slotValue, SlotType slotType)
        {
            if (string.IsNullOrWhiteSpace(slotValue))
            {
                return true;
            }

            return TryMapSlot(slotValue, out var resolvedSlot) && resolvedSlot == slotType;
        }

        private static bool TryMapSlot(string slotValue, out SlotType slotType)
        {
            slotType = default;
            if (string.IsNullOrWhiteSpace(slotValue))
            {
                return false;
            }

            var normalized = slotValue.Trim().Replace("-", string.Empty).Replace("_", string.Empty);
            return Enum.TryParse(normalized, true, out slotType);
        }

        private void Reject(string reason)
        {
            lastCommandExplicitlyRejected = true;
            Debug.LogWarning($"[UnityBridgeClient] {reason}");
            CommandRejected?.Invoke(reason);
        }

        private static (bool recognized, bool handled) CreateCommandResult(bool handled)
        {
            return (true, handled);
        }
    }
}
