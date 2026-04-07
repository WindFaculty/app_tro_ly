import type {
  ChatConversationDetailResponse,
  ChatConversationListResponse,
  ChatRequestPayload,
  ChatResponse,
  CompleteTaskPayload,
  DayTasksResponse,
  HealthResponse,
  MemoryListResponse,
  NoteCreatePayload,
  NoteListResponse,
  NoteRecord,
  NoteUpdatePayload,
  OverdueTasksResponse,
  SettingsPayload,
  RescheduleTaskPayload,
  SettingsResponse,
  TaskCreatePayload,
  TaskListResponse,
  WeekTasksResponse,
  TaskRecord,
  TaskUpdatePayload,
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

export async function getOverdueTasks(): Promise<OverdueTasksResponse> {
  return requestJson<OverdueTasksResponse>("/v1/tasks/overdue");
}

export async function getInboxTasks(): Promise<TaskListResponse> {
  return requestJson<TaskListResponse>("/v1/tasks/inbox");
}

export async function getActiveTasks(limit = 100): Promise<TaskListResponse> {
  return requestJson<TaskListResponse>(`/v1/tasks/active?limit=${limit}`);
}

export async function getCompletedTasks(): Promise<TaskListResponse> {
  return requestJson<TaskListResponse>("/v1/tasks/completed");
}

export async function getSettings(): Promise<SettingsResponse> {
  return requestJson<SettingsResponse>("/v1/settings");
}

export async function updateSettings(payload: SettingsPayload): Promise<SettingsResponse> {
  return requestJson<SettingsResponse>("/v1/settings", {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function resetSettings(): Promise<SettingsResponse> {
  return requestJson<SettingsResponse>("/v1/settings/reset", {
    method: "POST",
  });
}

export async function listNotes(limit = 100): Promise<NoteListResponse> {
  return requestJson<NoteListResponse>(`/v1/notes?limit=${limit}`);
}

export async function createNote(payload: NoteCreatePayload): Promise<NoteRecord> {
  return requestJson<NoteRecord>("/v1/notes", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateNote(noteId: string, payload: NoteUpdatePayload): Promise<NoteRecord> {
  return requestJson<NoteRecord>(`/v1/notes/${noteId}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function getMemoryItems(limit = 50): Promise<MemoryListResponse> {
  return requestJson<MemoryListResponse>(`/v1/memory/items?limit=${limit}`);
}

export async function createTask(payload: TaskCreatePayload): Promise<TaskRecord> {
  return requestJson<TaskRecord>("/v1/tasks", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateTask(taskId: string, payload: TaskUpdatePayload): Promise<TaskRecord> {
  return requestJson<TaskRecord>(`/v1/tasks/${taskId}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function completeTask(
  taskId: string,
  payload: CompleteTaskPayload = {},
): Promise<TaskRecord> {
  return requestJson<TaskRecord>(`/v1/tasks/${taskId}/complete`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function rescheduleTask(
  taskId: string,
  payload: RescheduleTaskPayload,
): Promise<TaskRecord> {
  return requestJson<TaskRecord>(`/v1/tasks/${taskId}/reschedule`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function listChatConversations(limit = 20): Promise<ChatConversationListResponse> {
  return requestJson<ChatConversationListResponse>(`/v1/chat/conversations?limit=${limit}`);
}

export async function getChatConversation(
  conversationId: string,
): Promise<ChatConversationDetailResponse> {
  return requestJson<ChatConversationDetailResponse>(`/v1/chat/conversations/${conversationId}`);
}

export async function sendChatMessage(payload: ChatRequestPayload): Promise<ChatResponse> {
  return requestJson<ChatResponse>("/v1/chat", {
    method: "POST",
    body: JSON.stringify({
      mode: "text",
      include_voice: false,
      voice_mode: false,
      ...payload,
    }),
  });
}
