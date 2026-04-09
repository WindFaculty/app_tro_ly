import { invoke } from "@tauri-apps/api/core";
import { listen, type UnlistenFn } from "@tauri-apps/api/event";
import { TAURI_COMMANDS, TAURI_EVENTS } from "@contracts";
import type {
  BackendLifecycleEvent,
  DesktopPlannerView,
  DesktopRestoreState,
  DesktopSessionState,
  ShellRuntimeState,
  UnityRuntimeStatus,
} from "@contracts";

export type {
  DesktopRestoreState,
  DesktopSessionState,
  ShellRuntimeState,
  UnityRuntimeStatus,
} from "@contracts";

export type RuntimeMode = "desktop" | "browser";

const DEFAULT_BACKEND_URL =
  import.meta.env.VITE_BACKEND_URL?.trim() || "http://127.0.0.1:8096";

const BROWSER_BACKEND_EVENT: BackendLifecycleEvent = {
  status: "browser_preview",
  message: "Browser preview mode khong so huu desktop host lifecycle.",
  backend_url: DEFAULT_BACKEND_URL,
  health_status: "browser_preview",
};

const BROWSER_SHELL_RUNTIME_STATE: ShellRuntimeState = {
  runtime_mode: "browser_preview",
  app_name: "App Tro Ly",
  app_version: "0.1.0-preview",
  backend_url: DEFAULT_BACKEND_URL,
  backend_ready: true,
  backend_status: "browser_preview",
  backend_message:
    "Browser preview mode khong co Tauri host, window chrome, hay backend auto-start.",
  backend_pid: null,
  backend_path: null,
  python_path: null,
  app_data_dir: null,
  app_cache_dir: null,
  app_log_dir: null,
  app_config_dir: null,
  export_dir: null,
  window_visible: true,
  window_maximized: false,
};

const BROWSER_DESKTOP_SESSION_STATE: DesktopSessionState = {
  session: {
    schema_version: 1,
    active_route: "/",
    recent_routes: ["/"],
    updated_at_ms: null,
  },
  preferences: {
    schema_version: 1,
    restore_last_route: true,
    show_host_diagnostics: true,
    updated_at_ms: null,
  },
  theme: {
    schema_version: 1,
    active_theme: "system",
    updated_at_ms: null,
  },
  filters: {
    schema_version: 1,
    planner_view: "today",
    updated_at_ms: null,
  },
  runtime_snapshot: {
    schema_version: 1,
    runtime_mode: "browser_preview",
    backend_status: "browser_preview",
    backend_message: "Browser preview mode luu restore state bang localStorage.",
    backend_ready: true,
    unity_runtime_state: null,
    unity_bridge_state: null,
    window_maximized: false,
    updated_at_ms: null,
  },
};

const BROWSER_DESKTOP_RESTORE_STATE: DesktopRestoreState = {
  ...BROWSER_DESKTOP_SESSION_STATE,
  window_state: null,
  restore_status: "browser_preview",
  restore_message: "Browser preview mode doc va ghi desktop restore state bang localStorage.",
  paths: {
    session_state: null,
    window_state: null,
    runtime_snapshot: null,
    theme_state: null,
    filters_state: null,
    app_preferences: null,
  },
};

const BROWSER_SESSION_STORAGE_KEY = "desktop-shell-session-state-v1";

function tauriGlobals(): { __TAURI__?: unknown; __TAURI_INTERNALS__?: unknown } {
  return globalThis as typeof globalThis & {
    __TAURI__?: unknown;
    __TAURI_INTERNALS__?: unknown;
  };
}

export function isTauriRuntime(): boolean {
  const globals = tauriGlobals();
  return Boolean(globals.__TAURI__ || globals.__TAURI_INTERNALS__);
}

export function getRuntimeMode(): RuntimeMode {
  return isTauriRuntime() ? "desktop" : "browser";
}

export function getDefaultBackendUrl(): string {
  return DEFAULT_BACKEND_URL;
}

function browserStorage(): Storage | null {
  try {
    return globalThis.localStorage;
  } catch {
    return null;
  }
}

function normalizePlannerView(view: string | null | undefined): DesktopPlannerView {
  switch ((view ?? "").trim()) {
    case "week":
    case "calendar":
    case "inbox":
    case "overdue":
    case "completed":
    case "reminders":
    case "tags":
      return view!.trim() as DesktopPlannerView;
    default:
      return "today";
  }
}

function normalizeBrowserSessionState(state: DesktopSessionState): DesktopSessionState {
  const activeRoute = state.session.active_route?.trim() || "/";
  const normalizedRoute = activeRoute.startsWith("/") ? activeRoute : `/${activeRoute}`;
  const recentRoutes = [normalizedRoute, ...(state.session.recent_routes ?? [])]
    .map((route) => route.trim())
    .filter((route) => route.length > 0)
    .map((route) => (route.startsWith("/") ? route : `/${route}`))
    .filter((route, index, values) => values.indexOf(route) === index)
    .slice(0, 5);
  const updatedAt = Date.now();

  return {
    session: {
      schema_version: 1,
      active_route: normalizedRoute,
      recent_routes: recentRoutes,
      updated_at_ms: updatedAt,
    },
    preferences: {
      schema_version: 1,
      restore_last_route: state.preferences.restore_last_route,
      show_host_diagnostics: state.preferences.show_host_diagnostics,
      updated_at_ms: updatedAt,
    },
    theme: {
      schema_version: 1,
      active_theme:
        state.theme.active_theme === "light" || state.theme.active_theme === "dark"
          ? state.theme.active_theme
          : "system",
      updated_at_ms: updatedAt,
    },
    filters: {
      schema_version: 1,
      planner_view: normalizePlannerView(state.filters.planner_view),
      updated_at_ms: updatedAt,
    },
    runtime_snapshot: {
      schema_version: 1,
      runtime_mode: state.runtime_snapshot.runtime_mode?.trim() || "browser_preview",
      backend_status: state.runtime_snapshot.backend_status?.trim() || "browser_preview",
      backend_message: state.runtime_snapshot.backend_message?.trim() || "",
      backend_ready: state.runtime_snapshot.backend_ready,
      unity_runtime_state: state.runtime_snapshot.unity_runtime_state?.trim() || null,
      unity_bridge_state: state.runtime_snapshot.unity_bridge_state?.trim() || null,
      window_maximized: state.runtime_snapshot.window_maximized,
      updated_at_ms: updatedAt,
    },
  };
}

function loadBrowserRestoreState(): DesktopRestoreState {
  const storage = browserStorage();
  if (!storage) {
    return BROWSER_DESKTOP_RESTORE_STATE;
  }

  try {
    const raw = storage.getItem(BROWSER_SESSION_STORAGE_KEY);
    if (!raw) {
      return BROWSER_DESKTOP_RESTORE_STATE;
    }
    const parsed = JSON.parse(raw) as DesktopSessionState;
    const normalized = normalizeBrowserSessionState(parsed);
    return {
      ...normalized,
      window_state: null,
      restore_status: "restored",
      restore_message: "Browser preview mode restored shell session from localStorage.",
      paths: BROWSER_DESKTOP_RESTORE_STATE.paths,
    };
  } catch {
    storage.removeItem(BROWSER_SESSION_STORAGE_KEY);
    return {
      ...BROWSER_DESKTOP_RESTORE_STATE,
      restore_status: "recovered",
      restore_message:
        "Browser preview restore state bi hong va da duoc reset ve mac dinh trong localStorage.",
    };
  }
}

export async function subscribeBackendReady(
  handler: (event: BackendLifecycleEvent) => void,
): Promise<UnlistenFn> {
  if (!isTauriRuntime()) {
    return async () => undefined;
  }
  return listen<BackendLifecycleEvent>(TAURI_EVENTS.backendReady, (event) =>
    handler(event.payload ?? {}),
  );
}

export async function subscribeBackendError(
  handler: (event: BackendLifecycleEvent) => void,
): Promise<UnlistenFn> {
  if (!isTauriRuntime()) {
    return async () => undefined;
  }
  return listen<BackendLifecycleEvent>(TAURI_EVENTS.backendError, (event) =>
    handler(event.payload ?? {}),
  );
}

export async function subscribeBackendStatus(
  handler: (event: BackendLifecycleEvent) => void,
): Promise<UnlistenFn> {
  if (!isTauriRuntime()) {
    return async () => undefined;
  }
  return listen<BackendLifecycleEvent>(TAURI_EVENTS.backendStatus, (event) =>
    handler(event.payload ?? {}),
  );
}

export async function checkDesktopBackendReady(): Promise<boolean> {
  if (!isTauriRuntime()) {
    return true;
  }

  try {
    return await invoke<boolean>(TAURI_COMMANDS.isBackendReady);
  } catch {
    return false;
  }
}

export async function resolveRuntimeBackendUrl(): Promise<string> {
  if (!isTauriRuntime()) {
    return DEFAULT_BACKEND_URL;
  }

  try {
    return await invoke<string>(TAURI_COMMANDS.getBackendUrl);
  } catch {
    return DEFAULT_BACKEND_URL;
  }
}

export async function getShellRuntimeState(): Promise<ShellRuntimeState> {
  if (!isTauriRuntime()) {
    return BROWSER_SHELL_RUNTIME_STATE;
  }

  try {
    return await invoke<ShellRuntimeState>(TAURI_COMMANDS.getShellRuntimeState);
  } catch {
    return BROWSER_SHELL_RUNTIME_STATE;
  }
}

export async function getDesktopRestoreState(): Promise<DesktopRestoreState> {
  if (!isTauriRuntime()) {
    return loadBrowserRestoreState();
  }

  return invoke<DesktopRestoreState>(TAURI_COMMANDS.getDesktopRestoreState);
}

export async function persistDesktopSessionState(
  sessionState: DesktopSessionState,
): Promise<DesktopRestoreState> {
  if (!isTauriRuntime()) {
    const normalized = normalizeBrowserSessionState(sessionState);
    const storage = browserStorage();
    storage?.setItem(BROWSER_SESSION_STORAGE_KEY, JSON.stringify(normalized));
    return {
      ...normalized,
      window_state: null,
      restore_status: "persisted",
      restore_message: "Browser preview mode saved shell session to localStorage.",
      paths: BROWSER_DESKTOP_RESTORE_STATE.paths,
    };
  }

  return invoke<DesktopRestoreState>(TAURI_COMMANDS.persistDesktopSessionState, {
    sessionState,
  });
}

export async function resetDesktopRestoreState(): Promise<DesktopRestoreState> {
  if (!isTauriRuntime()) {
    const storage = browserStorage();
    storage?.removeItem(BROWSER_SESSION_STORAGE_KEY);
    return {
      ...BROWSER_DESKTOP_RESTORE_STATE,
      restore_status: "reset",
      restore_message: "Browser preview mode reset shell restore state in localStorage.",
    };
  }

  return invoke<DesktopRestoreState>(TAURI_COMMANDS.resetDesktopRestoreState);
}

export async function restartDesktopBackend(): Promise<BackendLifecycleEvent> {
  if (!isTauriRuntime()) {
    return BROWSER_BACKEND_EVENT;
  }

  return invoke<BackendLifecycleEvent>(TAURI_COMMANDS.restartBackend);
}

export async function minimizeDesktopWindow(): Promise<void> {
  if (!isTauriRuntime()) {
    return;
  }

  await invoke<void>(TAURI_COMMANDS.minimizeShellWindow);
}

export async function toggleDesktopWindowMaximize(): Promise<ShellRuntimeState> {
  if (!isTauriRuntime()) {
    return BROWSER_SHELL_RUNTIME_STATE;
  }

  return invoke<ShellRuntimeState>(TAURI_COMMANDS.toggleShellWindowMaximize);
}

export async function closeDesktopWindow(): Promise<void> {
  if (!isTauriRuntime()) {
    return;
  }

  await invoke<void>(TAURI_COMMANDS.closeShellWindow);
}

export async function getUnityRuntimeStatus(): Promise<UnityRuntimeStatus> {
  if (!isTauriRuntime()) {
    return {
      state: "browser_preview",
      message:
        "Browser preview mode khong spawn duoc Unity sidecar. Can chay qua Tauri de kiem tra embed lifecycle.",
      executable_path: null,
      build_root: null,
      pid: null,
      source: "browser",
    };
  }

  return invoke<UnityRuntimeStatus>(TAURI_COMMANDS.getUnityRuntimeStatus);
}

export async function launchUnityRuntime(): Promise<UnityRuntimeStatus> {
  if (!isTauriRuntime()) {
    return getUnityRuntimeStatus();
  }

  return invoke<UnityRuntimeStatus>(TAURI_COMMANDS.launchUnityRuntime);
}

export async function stopUnityRuntime(): Promise<UnityRuntimeStatus> {
  if (!isTauriRuntime()) {
    return getUnityRuntimeStatus();
  }

  return invoke<UnityRuntimeStatus>(TAURI_COMMANDS.stopUnityRuntime);
}

export async function subscribeUnityRuntimeStatus(
  handler: (status: UnityRuntimeStatus) => void,
): Promise<UnlistenFn> {
  if (!isTauriRuntime()) {
    return async () => undefined;
  }

  return listen<UnityRuntimeStatus>(TAURI_EVENTS.unityRuntimeStatus, (event) =>
    handler(event.payload),
  );
}
