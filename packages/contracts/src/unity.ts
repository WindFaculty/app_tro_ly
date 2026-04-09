export const UNITY_BRIDGE_PROTOCOL_VERSION = 1 as const;

export const APP_PAGES = [
  "dashboard",
  "chat",
  "planner",
  "notes",
  "email",
  "automation",
  "wardrobe",
  "settings",
  "status",
] as const;

export type AppPage = (typeof APP_PAGES)[number];

export const UNITY_CAMERA_FOCUS_PRESETS = ["overview", "avatar", "desk", "wardrobe"] as const;

export type UnityCameraFocusPreset = (typeof UNITY_CAMERA_FOCUS_PRESETS)[number];

export const UNITY_BRIDGE_COMMAND_TYPES = {
  pageChanged: "app.pageChanged",
  avatarSetMood: "avatar.setMood",
  avatarPlayEmote: "avatar.playEmote",
  avatarSpeakStart: "avatar.speakStart",
  avatarSpeakStop: "avatar.speakStop",
  avatarSetIdleState: "avatar.setIdleState",
  avatarSetListeningState: "avatar.setListeningState",
  roomSetCameraFocus: "room.setCameraFocus",
  roomFocusObject: "room.focusObject",
  wardrobeEquipItem: "wardrobe.equipItem",
} as const;

export type UnityBridgeCommandType =
  (typeof UNITY_BRIDGE_COMMAND_TYPES)[keyof typeof UNITY_BRIDGE_COMMAND_TYPES];

export const UNITY_BRIDGE_EVENT_TYPES = {
  bridgeReady: "bridge.ready",
  bridgeError: "bridge.error",
  avatarStateChanged: "avatar.stateChanged",
  avatarAnimationFinished: "avatar.animationFinished",
  roomObjectClicked: "room.objectClicked",
  roomInteractionTriggered: "room.interactionTriggered",
} as const;

export type UnityBridgeEventType =
  (typeof UNITY_BRIDGE_EVENT_TYPES)[keyof typeof UNITY_BRIDGE_EVENT_TYPES];

export type UnityBridgeSource = "react" | "tauri" | "unity";

export interface UnityBridgeEnvelopeBase<TType extends string, TPayload> {
  protocol_version: typeof UNITY_BRIDGE_PROTOCOL_VERSION;
  id: string;
  type: TType;
  source: UnityBridgeSource;
  timestamp: string;
  payload: TPayload;
}

export interface AppPageChangedPayload {
  page: AppPage;
}

export interface AvatarSetMoodPayload {
  mood: string;
  backend_state?: string;
  animation_hint?: string;
}

export interface AvatarPlayEmotePayload {
  emote: string;
}

export interface AvatarSpeechPayload {
  utterance_id?: string;
  reason?: string;
}

export interface AvatarStateCommandPayload {
  reason?: string;
}

export interface RoomSetCameraFocusPayload {
  focus: UnityCameraFocusPreset;
}

export interface RoomFocusObjectPayload {
  object_name: string;
}

export interface WardrobeEquipItemPayload {
  item_id: string;
  slot?: string;
}

export interface UnityBridgeCommandPayloadMap {
  "app.pageChanged": AppPageChangedPayload;
  "avatar.setMood": AvatarSetMoodPayload;
  "avatar.playEmote": AvatarPlayEmotePayload;
  "avatar.speakStart": AvatarSpeechPayload;
  "avatar.speakStop": AvatarSpeechPayload;
  "avatar.setIdleState": AvatarStateCommandPayload;
  "avatar.setListeningState": AvatarStateCommandPayload;
  "room.setCameraFocus": RoomSetCameraFocusPayload;
  "room.focusObject": RoomFocusObjectPayload;
  "wardrobe.equipItem": WardrobeEquipItemPayload;
}

export type UnityBridgeCommandEnvelope<
  TType extends UnityBridgeCommandType = UnityBridgeCommandType,
> = UnityBridgeEnvelopeBase<TType, UnityBridgeCommandPayloadMap[TType]>;

export interface BridgeReadyPayload {
  transport: "local_websocket";
  url?: string;
}

export interface BridgeErrorPayload {
  message: string;
  detail?: string;
}

export interface AvatarStateChangedPayload {
  state: string;
  mood?: string;
  animation_hint?: string;
}

export interface AvatarAnimationFinishedPayload {
  animation: string;
}

export interface RoomObjectClickedPayload {
  object_name: string;
}

export interface RoomInteractionTriggeredPayload {
  interaction: string;
  object_name?: string;
}

export interface UnityBridgeEventPayloadMap {
  "bridge.ready": BridgeReadyPayload;
  "bridge.error": BridgeErrorPayload;
  "avatar.stateChanged": AvatarStateChangedPayload;
  "avatar.animationFinished": AvatarAnimationFinishedPayload;
  "room.objectClicked": RoomObjectClickedPayload;
  "room.interactionTriggered": RoomInteractionTriggeredPayload;
}

export type UnityBridgeEventEnvelope<
  TType extends UnityBridgeEventType = UnityBridgeEventType,
> = UnityBridgeEnvelopeBase<TType, UnityBridgeEventPayloadMap[TType]>;

function createEnvelopeId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `bridge-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}

export function createUnityBridgeCommand<TType extends UnityBridgeCommandType>(
  type: TType,
  payload: UnityBridgeCommandPayloadMap[TType],
  source: UnityBridgeSource = "react",
): UnityBridgeCommandEnvelope<TType> {
  return {
    protocol_version: UNITY_BRIDGE_PROTOCOL_VERSION,
    id: createEnvelopeId(),
    type,
    source,
    timestamp: new Date().toISOString(),
    payload,
  };
}

export function createUnityBridgeEvent<TType extends UnityBridgeEventType>(
  type: TType,
  payload: UnityBridgeEventPayloadMap[TType],
  source: UnityBridgeSource = "unity",
): UnityBridgeEventEnvelope<TType> {
  return {
    protocol_version: UNITY_BRIDGE_PROTOCOL_VERSION,
    id: createEnvelopeId(),
    type,
    source,
    timestamp: new Date().toISOString(),
    payload,
  };
}
