import type {
  BrowserAutomationApprovalPayload,
  BrowserAutomationCancelPayload,
  BrowserAutomationRejectPayload,
  BrowserAutomationRunCreatePayload,
  BrowserAutomationRunDetail,
  BrowserAutomationRunListResponse,
  BrowserAutomationTemplateListResponse,
  ChatConversationDetailResponse,
  ChatConversationListResponse,
  ChatRequestPayload,
  ChatResponse,
  CompleteTaskPayload,
  DayTasksResponse,
  EmailDraftCreatePayload,
  EmailDraftListResponse,
  EmailDraftRecord,
  EmailDraftUpdatePayload,
  EmailMessageDetail,
  EmailMessageListResponse,
  EmailToTaskPayload,
  GoogleCalendarConnectResponse,
  GoogleCalendarDeleteResponse,
  GoogleCalendarEventCreatePayload,
  GoogleCalendarEventListResponse,
  GoogleCalendarEventRecord,
  GoogleCalendarEventUpdatePayload,
  GoogleCalendarStatusResponse,
  GoogleEmailConnectResponse,
  GoogleEmailStatusResponse,
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
  SpeechSttResponse,
  TaskCreatePayload,
  TaskListResponse,
  WeekTasksResponse,
  TaskRecord,
  TaskUpdatePayload,
  WardrobeImportPayload,
  WardrobeItemCreatePayload,
  WardrobeItemRecord,
  WardrobeItemUpdatePayload,
  WardrobeOutfitCreatePayload,
  WardrobeOutfitRecord,
  WardrobeOutfitUpdatePayload,
  WardrobeSnapshotResponse,
} from "@/contracts/backend";
import { resolveRuntimeBackendUrl } from "@/services/runtimeHost";

let backendUrlPromise: Promise<string> | null = null;

async function getBackendBaseUrl(): Promise<string> {
  backendUrlPromise ??= resolveRuntimeBackendUrl();
  const url = await backendUrlPromise;
  return url.replace(/\/+$/, "");
}

async function readErrorDetail(response: Response): Promise<string> {
  let detail = `${response.status} ${response.statusText}`;
  try {
    const payload = (await response.json()) as { detail?: string };
    if (payload.detail) {
      detail = payload.detail;
    }
  } catch {
    // Ignore non-JSON error bodies.
  }
  return detail;
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
    throw new Error(await readErrorDetail(response));
  }

  return (await response.json()) as T;
}

export async function getBackendBaseUrlForUi(): Promise<string> {
  return getBackendBaseUrl();
}

export async function resolveBackendAssetUrl(assetPath: string): Promise<string> {
  return new URL(assetPath, `${await getBackendBaseUrl()}/`).toString();
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

export async function getWardrobeSnapshot(): Promise<WardrobeSnapshotResponse> {
  return requestJson<WardrobeSnapshotResponse>("/v1/wardrobe");
}

export async function exportWardrobeSnapshot(): Promise<WardrobeSnapshotResponse> {
  return requestJson<WardrobeSnapshotResponse>("/v1/wardrobe/export");
}

export async function importWardrobeSnapshot(
  payload: WardrobeImportPayload,
): Promise<WardrobeSnapshotResponse> {
  return requestJson<WardrobeSnapshotResponse>("/v1/wardrobe/import", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function createWardrobeItem(
  payload: WardrobeItemCreatePayload,
): Promise<WardrobeItemRecord> {
  return requestJson<WardrobeItemRecord>("/v1/wardrobe/items", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateWardrobeItem(
  itemId: string,
  payload: WardrobeItemUpdatePayload,
): Promise<WardrobeItemRecord> {
  return requestJson<WardrobeItemRecord>(`/v1/wardrobe/items/${itemId}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function deleteWardrobeItem(itemId: string): Promise<WardrobeSnapshotResponse> {
  return requestJson<WardrobeSnapshotResponse>(`/v1/wardrobe/items/${itemId}`, {
    method: "DELETE",
  });
}

export async function createWardrobeOutfit(
  payload: WardrobeOutfitCreatePayload,
): Promise<WardrobeOutfitRecord> {
  return requestJson<WardrobeOutfitRecord>("/v1/wardrobe/outfits", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateWardrobeOutfit(
  outfitId: string,
  payload: WardrobeOutfitUpdatePayload,
): Promise<WardrobeOutfitRecord> {
  return requestJson<WardrobeOutfitRecord>(`/v1/wardrobe/outfits/${outfitId}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function deleteWardrobeOutfit(outfitId: string): Promise<WardrobeSnapshotResponse> {
  return requestJson<WardrobeSnapshotResponse>(`/v1/wardrobe/outfits/${outfitId}`, {
    method: "DELETE",
  });
}

export async function getGoogleEmailStatus(): Promise<GoogleEmailStatusResponse> {
  return requestJson<GoogleEmailStatusResponse>("/v1/email/status");
}

export async function getGoogleCalendarStatus(): Promise<GoogleCalendarStatusResponse> {
  return requestJson<GoogleCalendarStatusResponse>("/v1/calendar/status");
}

export async function startGoogleCalendarConnect(): Promise<GoogleCalendarConnectResponse> {
  return requestJson<GoogleCalendarConnectResponse>("/v1/calendar/google/connect");
}

export async function disconnectGoogleCalendar(): Promise<GoogleCalendarStatusResponse> {
  return requestJson<GoogleCalendarStatusResponse>("/v1/calendar/google/disconnect", {
    method: "POST",
  });
}

export async function listGoogleCalendarEvents(
  params: { start_date?: string; days?: number; calendar_id?: string; query?: string; limit?: number } = {},
): Promise<GoogleCalendarEventListResponse> {
  const searchParams = new URLSearchParams();
  if (params.start_date) {
    searchParams.set("start_date", params.start_date);
  }
  if (params.days) {
    searchParams.set("days", String(params.days));
  }
  if (params.calendar_id) {
    searchParams.set("calendar_id", params.calendar_id);
  }
  if (params.query) {
    searchParams.set("query", params.query);
  }
  if (params.limit) {
    searchParams.set("limit", String(params.limit));
  }
  const query = searchParams.toString();
  return requestJson<GoogleCalendarEventListResponse>(`/v1/calendar/events${query ? `?${query}` : ""}`);
}

export async function createGoogleCalendarEvent(
  payload: GoogleCalendarEventCreatePayload,
): Promise<GoogleCalendarEventRecord> {
  return requestJson<GoogleCalendarEventRecord>("/v1/calendar/events", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateGoogleCalendarEvent(
  eventId: string,
  payload: GoogleCalendarEventUpdatePayload,
): Promise<GoogleCalendarEventRecord> {
  return requestJson<GoogleCalendarEventRecord>(`/v1/calendar/events/${eventId}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function deleteGoogleCalendarEvent(
  eventId: string,
  calendarId?: string,
): Promise<GoogleCalendarDeleteResponse> {
  const query = calendarId ? `?calendar_id=${encodeURIComponent(calendarId)}` : "";
  return requestJson<GoogleCalendarDeleteResponse>(`/v1/calendar/events/${eventId}${query}`, {
    method: "DELETE",
  });
}

export async function startGoogleEmailConnect(): Promise<GoogleEmailConnectResponse> {
  return requestJson<GoogleEmailConnectResponse>("/v1/email/google/connect");
}

export async function disconnectGoogleEmail(): Promise<GoogleEmailStatusResponse> {
  return requestJson<GoogleEmailStatusResponse>("/v1/email/google/disconnect", {
    method: "POST",
  });
}

export async function listEmailMessages(
  params: { query?: string; label?: string; limit?: number } = {},
): Promise<EmailMessageListResponse> {
  const searchParams = new URLSearchParams();
  if (params.query) {
    searchParams.set("query", params.query);
  }
  if (params.label) {
    searchParams.set("label", params.label);
  }
  if (params.limit) {
    searchParams.set("limit", String(params.limit));
  }
  const query = searchParams.toString();
  return requestJson<EmailMessageListResponse>(`/v1/email/messages${query ? `?${query}` : ""}`);
}

export async function getEmailMessage(messageId: string): Promise<EmailMessageDetail> {
  return requestJson<EmailMessageDetail>(`/v1/email/messages/${messageId}`);
}

export async function convertEmailMessageToTask(
  messageId: string,
  payload: EmailToTaskPayload,
): Promise<TaskRecord> {
  return requestJson<TaskRecord>(`/v1/email/messages/${messageId}/task`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function listEmailDrafts(limit = 50): Promise<EmailDraftListResponse> {
  return requestJson<EmailDraftListResponse>(`/v1/email/drafts?limit=${limit}`);
}

export async function createEmailDraft(payload: EmailDraftCreatePayload): Promise<EmailDraftRecord> {
  return requestJson<EmailDraftRecord>("/v1/email/drafts", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateEmailDraft(
  draftId: string,
  payload: EmailDraftUpdatePayload,
): Promise<EmailDraftRecord> {
  return requestJson<EmailDraftRecord>(`/v1/email/drafts/${draftId}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function sendEmailDraft(draftId: string): Promise<EmailDraftRecord> {
  return requestJson<EmailDraftRecord>(`/v1/email/drafts/${draftId}/send`, {
    method: "POST",
  });
}

export async function listBrowserAutomationTemplates(): Promise<BrowserAutomationTemplateListResponse> {
  return requestJson<BrowserAutomationTemplateListResponse>("/v1/browser-automation/templates");
}

export async function listBrowserAutomationRuns(limit = 20): Promise<BrowserAutomationRunListResponse> {
  return requestJson<BrowserAutomationRunListResponse>(`/v1/browser-automation/runs?limit=${limit}`);
}

export async function getBrowserAutomationRun(runId: string): Promise<BrowserAutomationRunDetail> {
  return requestJson<BrowserAutomationRunDetail>(`/v1/browser-automation/runs/${runId}`);
}

export async function createBrowserAutomationRun(
  payload: BrowserAutomationRunCreatePayload,
): Promise<BrowserAutomationRunDetail> {
  return requestJson<BrowserAutomationRunDetail>("/v1/browser-automation/runs", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function approveBrowserAutomationRun(
  runId: string,
  payload: BrowserAutomationApprovalPayload = {},
): Promise<BrowserAutomationRunDetail> {
  return requestJson<BrowserAutomationRunDetail>(`/v1/browser-automation/runs/${runId}/approve`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function rejectBrowserAutomationRun(
  runId: string,
  payload: BrowserAutomationRejectPayload,
): Promise<BrowserAutomationRunDetail> {
  return requestJson<BrowserAutomationRunDetail>(`/v1/browser-automation/runs/${runId}/reject`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function cancelBrowserAutomationRun(
  runId: string,
  payload: BrowserAutomationCancelPayload = {},
): Promise<BrowserAutomationRunDetail> {
  return requestJson<BrowserAutomationRunDetail>(`/v1/browser-automation/runs/${runId}/cancel`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
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

export async function transcribeSpeechAudio(
  audio: Blob,
  language?: string,
): Promise<SpeechSttResponse> {
  const baseUrl = await getBackendBaseUrl();
  const formData = new FormData();
  formData.append("audio", audio, "speech.wav");

  const query = language ? `?language=${encodeURIComponent(language)}` : "";
  const response = await fetch(`${baseUrl}/v1/speech/stt${query}`, {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    throw new Error(await readErrorDetail(response));
  }

  return (await response.json()) as SpeechSttResponse;
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
