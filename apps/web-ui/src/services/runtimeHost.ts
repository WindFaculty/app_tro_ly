import { invoke } from "@tauri-apps/api/core";
import { listen, type UnlistenFn } from "@tauri-apps/api/event";
import {
  TAURI_COMMANDS,
  TAURI_EVENTS,
  type BackendLifecycleEvent,
  type UnityRuntimeStatus,
} from "@contracts";

export type RuntimeMode = "desktop" | "browser";

const DEFAULT_BACKEND_URL =
  import.meta.env.VITE_BACKEND_URL?.trim() || "http://127.0.0.1:7821";

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

export async function getUnityRuntimeStatus(): Promise<UnityRuntimeStatus> {
  if (!isTauriRuntime()) {
    return {
      state: "browser_preview",
      message: "Browser preview mode không spawn được Unity sidecar. Cần chạy qua Tauri để kiểm tra embed lifecycle.",
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
