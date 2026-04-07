import type {
  ChatResponse,
  DayTasksResponse,
  HealthResponse,
  SettingsResponse,
  TaskListResponse,
  WeekTasksResponse,
} from "@/contracts/backend";
import { resolveRuntimeBackendUrl } from "@/services/runtimeHost";

let backendUrlPromise: Promise<string> | null = null;

async function getBackendBaseUrl(): Promise<string> {
  backendUrlPromise ??= resolveRuntimeBackendUrl();
  const url = await backendUrlPromise;
  return url.replace(/\/+$/, "");
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const baseUrl = await getBackendBaseUrl();
  const response = await fetch(`${baseUrl}${path.startsWith("/") ? path : `/${path}`}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    let detail = `${response.status} ${response.statusText}`;
    try {
      const payload = (await response.json()) as { detail?: string };
      if (payload.detail) {
        detail = payload.detail;
      }
    } catch {
      // Ignore non-JSON error bodies.
    }
    throw new Error(detail);
  }

  return (await response.json()) as T;
}

export async function getBackendBaseUrlForUi(): Promise<string> {
  return getBackendBaseUrl();
}

export async function checkHealth(): Promise<HealthResponse> {
  return requestJson<HealthResponse>("/v1/health");
}

export async function getTodayTasks(): Promise<DayTasksResponse> {
  return requestJson<DayTasksResponse>("/v1/tasks/today");
}

export async function getWeekTasks(): Promise<WeekTasksResponse> {
  return requestJson<WeekTasksResponse>("/v1/tasks/week");
}

export async function getInboxTasks(): Promise<TaskListResponse> {
  return requestJson<TaskListResponse>("/v1/tasks/inbox");
}

export async function getCompletedTasks(): Promise<TaskListResponse> {
  return requestJson<TaskListResponse>("/v1/tasks/completed");
}

export async function getSettings(): Promise<SettingsResponse> {
  return requestJson<SettingsResponse>("/v1/settings");
}

export async function sendChatMessage(message: string): Promise<ChatResponse> {
  return requestJson<ChatResponse>("/v1/chat", {
    method: "POST",
    body: JSON.stringify({
      message,
      mode: "text",
      include_voice: false,
      voice_mode: false,
    }),
  });
}
