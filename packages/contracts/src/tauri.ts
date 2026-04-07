import type {
  UnityBridgeCommandEnvelope,
  UnityBridgeEventEnvelope,
} from "./unity";

export const TAURI_COMMANDS = {
  isBackendReady: "is_backend_ready",
  getBackendUrl: "get_backend_url",
  checkBackendHealth: "check_backend_health",
  getUnityRuntimeStatus: "get_unity_runtime_status",
  launchUnityRuntime: "launch_unity_runtime",
  stopUnityRuntime: "stop_unity_runtime",
  getUnityBridgeStatus: "get_unity_bridge_status",
  sendUnityBridgeCommand: "send_unity_bridge_command",
} as const;

export const TAURI_EVENTS = {
  backendReady: "backend-ready",
  backendError: "backend-error",
  unityRuntimeStatus: "unity-runtime-status",
  unityBridgeStatus: "unity-bridge-status",
  unityBridgeEvent: "unity-bridge-event",
} as const;

export interface BackendLifecycleEvent {
  status?: string;
  message?: string;
}

export interface UnityRuntimeStatus {
  state: string;
  message: string;
  executable_path?: string | null;
  build_root?: string | null;
  pid?: number | null;
  source?: string | null;
}

export interface UnityBridgeStatus {
  state: string;
  transport: string;
  listen_url?: string | null;
  connected_client?: string | null;
  last_command_type?: string | null;
  last_event_type?: string | null;
  last_error?: string | null;
  note: string;
}

export interface UnityBridgeDispatchResult {
  accepted: boolean;
  delivered: boolean;
  message: string;
  status: UnityBridgeStatus;
  command: UnityBridgeCommandEnvelope;
}

export type UnityBridgeEvent = UnityBridgeEventEnvelope;
