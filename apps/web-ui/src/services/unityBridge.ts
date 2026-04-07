import { invoke } from "@tauri-apps/api/core";
import { listen, type UnlistenFn } from "@tauri-apps/api/event";
import {
  APP_PAGES,
  TAURI_COMMANDS,
  TAURI_EVENTS,
  UNITY_BRIDGE_COMMAND_TYPES,
  createUnityBridgeCommand,
  type AppPage,
  type UnityBridgeCommandEnvelope,
  type UnityBridgeDispatchResult,
  type UnityBridgeEvent,
  type UnityBridgeStatus,
} from "@contracts";
import { isTauriRuntime } from "@/services/runtimeHost";

const BROWSER_BRIDGE_STATUS: UnityBridgeStatus = {
  state: "browser_preview",
  transport: "none",
  listen_url: null,
  connected_client: null,
  last_command_type: null,
  last_event_type: null,
  last_error: null,
  note: "Browser preview mode khong co Tauri host nen Unity bridge moi o muc placeholder.",
};

export function pathToAppPage(pathname: string): AppPage {
  const normalized = pathname.replace(/^\/+/, "").split("/")[0]?.trim().toLowerCase() ?? "";
  if (APP_PAGES.includes(normalized as AppPage)) {
    return normalized as AppPage;
  }

  return "dashboard";
}

export async function getUnityBridgeStatus(): Promise<UnityBridgeStatus> {
  if (!isTauriRuntime()) {
    return BROWSER_BRIDGE_STATUS;
  }

  return invoke<UnityBridgeStatus>(TAURI_COMMANDS.getUnityBridgeStatus);
}

export async function sendUnityBridgeCommand(
  command: UnityBridgeCommandEnvelope,
): Promise<UnityBridgeDispatchResult> {
  if (!isTauriRuntime()) {
    return {
      accepted: false,
      delivered: false,
      message: "Browser preview khong the gui command sang Unity bridge.",
      status: BROWSER_BRIDGE_STATUS,
      command,
    };
  }

  return invoke<UnityBridgeDispatchResult>(TAURI_COMMANDS.sendUnityBridgeCommand, { command });
}

export async function sendPageChangedCommand(page: AppPage): Promise<UnityBridgeDispatchResult> {
  return sendUnityBridgeCommand(
    createUnityBridgeCommand(UNITY_BRIDGE_COMMAND_TYPES.pageChanged, { page }),
  );
}

export async function subscribeUnityBridgeStatus(
  handler: (status: UnityBridgeStatus) => void,
): Promise<UnlistenFn> {
  if (!isTauriRuntime()) {
    return async () => undefined;
  }

  return listen<UnityBridgeStatus>(TAURI_EVENTS.unityBridgeStatus, (event) =>
    handler(event.payload),
  );
}

export async function subscribeUnityBridgeEvents(
  handler: (event: UnityBridgeEvent) => void,
): Promise<UnlistenFn> {
  if (!isTauriRuntime()) {
    return async () => undefined;
  }

  return listen<UnityBridgeEvent>(TAURI_EVENTS.unityBridgeEvent, (event) =>
    handler(event.payload),
  );
}
