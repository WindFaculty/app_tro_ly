import type {
  UnityBridgeCommandEnvelope,
  UnityBridgeEventEnvelope,
} from "./unity";

export const TAURI_COMMANDS = {
  isBackendReady: "is_backend_ready",
  getBackendUrl: "get_backend_url",
  checkBackendHealth: "check_backend_health",
  getShellRuntimeState: "get_shell_runtime_state",
  getDesktopRestoreState: "get_desktop_restore_state",
  persistDesktopSessionState: "persist_desktop_session_state",
  resetDesktopRestoreState: "reset_desktop_restore_state",
  restartBackend: "restart_backend",
  minimizeShellWindow: "minimize_shell_window",
  toggleShellWindowMaximize: "toggle_shell_window_maximize",
  closeShellWindow: "close_shell_window",
  getUnityRuntimeStatus: "get_unity_runtime_status",
  launchUnityRuntime: "launch_unity_runtime",
  stopUnityRuntime: "stop_unity_runtime",
  getUnityBridgeStatus: "get_unity_bridge_status",
  sendUnityBridgeCommand: "send_unity_bridge_command",
} as const;

export const TAURI_EVENTS = {
  backendReady: "backend-ready",
  backendError: "backend-error",
  backendStatus: "backend-status",
  unityRuntimeStatus: "unity-runtime-status",
  unityBridgeStatus: "unity-bridge-status",
  unityBridgeEvent: "unity-bridge-event",
} as const;

export interface BackendLifecycleEvent {
  status?: string;
  message?: string;
  attempt?: number | null;
  max_attempts?: number | null;
  backend_url?: string;
  backend_path?: string | null;
  python_path?: string | null;
  pid?: number | null;
  health_status?: string | null;
}

export interface ShellRuntimeState {
  runtime_mode: string;
  app_name: string;
  app_version: string;
  backend_url: string;
  backend_ready: boolean;
  backend_status: string;
  backend_message: string;
  backend_pid?: number | null;
  backend_path?: string | null;
  python_path?: string | null;
  app_data_dir?: string | null;
  app_cache_dir?: string | null;
  app_log_dir?: string | null;
  app_config_dir?: string | null;
  export_dir?: string | null;
  window_visible: boolean;
  window_maximized: boolean;
}

export interface DesktopSessionRouteState {
  schema_version: number;
  active_route: string;
  recent_routes: string[];
  updated_at_ms?: number | null;
}

export interface DesktopAppPreferencesState {
  schema_version: number;
  restore_last_route: boolean;
  show_host_diagnostics: boolean;
  updated_at_ms?: number | null;
}

export interface DesktopThemeState {
  schema_version: number;
  active_theme: "system" | "light" | "dark";
  updated_at_ms?: number | null;
}

export type DesktopPlannerView =
  | "today"
  | "week"
  | "calendar"
  | "inbox"
  | "overdue"
  | "completed"
  | "reminders"
  | "tags";

export interface DesktopUiFiltersState {
  schema_version: number;
  planner_view: DesktopPlannerView;
  updated_at_ms?: number | null;
}

export interface DesktopRuntimeSnapshot {
  schema_version: number;
  runtime_mode: string;
  backend_status: string;
  backend_message: string;
  backend_ready: boolean;
  unity_runtime_state?: string | null;
  unity_bridge_state?: string | null;
  window_maximized: boolean;
  updated_at_ms?: number | null;
}

export interface DesktopSessionState {
  session: DesktopSessionRouteState;
  preferences: DesktopAppPreferencesState;
  theme: DesktopThemeState;
  filters: DesktopUiFiltersState;
  runtime_snapshot: DesktopRuntimeSnapshot;
}

export interface DesktopWindowState {
  schema_version: number;
  x?: number | null;
  y?: number | null;
  width?: number | null;
  height?: number | null;
  maximized: boolean;
  updated_at_ms?: number | null;
}

export interface DesktopPersistencePaths {
  session_state?: string | null;
  window_state?: string | null;
  runtime_snapshot?: string | null;
  theme_state?: string | null;
  filters_state?: string | null;
  app_preferences?: string | null;
}

export interface DesktopRestoreState extends DesktopSessionState {
  window_state?: DesktopWindowState | null;
  restore_status: string;
  restore_message: string;
  paths: DesktopPersistencePaths;
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
