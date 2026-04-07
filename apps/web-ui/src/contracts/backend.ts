export interface HealthRuntimeState {
  available?: boolean;
  provider_available?: boolean;
  provider?: string;
  detail?: string;
}

export interface HealthResponse {
  status: string;
  service: string;
  version: string;
  database: Record<string, unknown>;
  runtimes: Record<string, HealthRuntimeState>;
  degraded_features: string[];
  logs: Record<string, unknown>;
  recovery_actions: string[];
}

export interface TaskRecord {
  id: string;
  title: string;
  description?: string | null;
  status: string;
  priority: string;
  category?: string | null;
  scheduled_date?: string | null;
  start_at?: string | null;
  end_at?: string | null;
  due_at?: string | null;
  is_all_day: boolean;
  repeat_rule: string;
  estimated_minutes?: number | null;
  actual_minutes?: number | null;
  tags: string[];
  created_at: string;
  updated_at: string;
  completed_at?: string | null;
}

export interface TaskCreatePayload {
  title: string;
  description?: string | null;
  status?: string;
  priority?: string;
  category?: string | null;
  scheduled_date?: string | null;
  start_at?: string | null;
  end_at?: string | null;
  due_at?: string | null;
  is_all_day?: boolean;
  repeat_rule?: string;
  repeat_config_json?: Record<string, unknown> | null;
  estimated_minutes?: number | null;
  actual_minutes?: number | null;
  tags?: string[];
}

export interface TaskUpdatePayload {
  title?: string;
  description?: string | null;
  status?: string;
  priority?: string;
  category?: string | null;
  scheduled_date?: string | null;
  start_at?: string | null;
  end_at?: string | null;
  due_at?: string | null;
  is_all_day?: boolean;
  repeat_rule?: string;
  repeat_config_json?: Record<string, unknown> | null;
  estimated_minutes?: number | null;
  actual_minutes?: number | null;
  tags?: string[];
}

export interface CompleteTaskPayload {
  completed_at?: string | null;
}

export interface RescheduleTaskPayload {
  scheduled_date?: string | null;
  start_at?: string | null;
  end_at?: string | null;
  due_at?: string | null;
}

export interface DayTasksResponse {
  date: string;
  items: TaskRecord[];
  overdue: TaskRecord[];
  due_soon: TaskRecord[];
  in_progress: TaskRecord[];
}

export interface WeekDaySummary {
  date: string;
  task_count: number;
  high_priority_count: number;
  items: TaskRecord[];
}

export interface WeekConflict {
  date: string;
  task_ids: string[];
  titles: string[];
}

export interface WeekTasksResponse {
  start_date: string;
  end_date: string;
  days: WeekDaySummary[];
  overdue_count: number;
  conflicts: WeekConflict[];
}

export interface TaskListResponse {
  items: TaskRecord[];
  count: number;
}

export interface OverdueTasksResponse {
  generated_at: string;
  items: TaskRecord[];
}

export interface NoteRecord {
  id: string;
  title: string;
  body: string;
  tags: string[];
  linked_task_id?: string | null;
  linked_conversation_id?: string | null;
  pinned: boolean;
  created_at: string;
  updated_at: string;
}

export interface NoteCreatePayload {
  title: string;
  body?: string;
  tags?: string[];
  linked_task_id?: string | null;
  linked_conversation_id?: string | null;
  pinned?: boolean;
}

export interface NoteUpdatePayload {
  title?: string;
  body?: string;
  tags?: string[];
  linked_task_id?: string | null;
  linked_conversation_id?: string | null;
  pinned?: boolean;
}

export interface NoteListResponse {
  items: NoteRecord[];
  count: number;
}

export interface MemoryItemRecord {
  id: string;
  category: string;
  content: string;
  confidence: number;
  status: string;
  metadata: Record<string, unknown>;
  source_conversation_id?: string | null;
  created_at: string;
  updated_at: string;
}

export interface MemoryListResponse {
  items: MemoryItemRecord[];
  count: number;
}

export interface VoiceSettings {
  input_mode: string;
  tts_voice: string;
  speak_replies: boolean;
  show_transcript_preview: boolean;
  [key: string]: unknown;
}

export interface ModelSettings {
  provider: string;
  name: string;
  routing_mode: string;
  fast_provider: string;
  deep_provider: string;
  [key: string]: unknown;
}

export interface WindowModeSettings {
  main_app_enabled: boolean;
  mini_assistant_enabled: boolean;
  [key: string]: unknown;
}

export interface AvatarSettings {
  character: string;
  lip_sync_mode: string;
  [key: string]: unknown;
}

export interface ReminderSettings {
  speech_enabled: boolean;
  lead_minutes: number;
  [key: string]: unknown;
}

export interface StartupSettings {
  launch_backend: boolean;
  launch_main_app: boolean;
  [key: string]: unknown;
}

export interface MemorySettings {
  auto_extract: boolean;
  short_term_turn_limit: number;
  [key: string]: unknown;
}

export interface SettingsResponse {
  voice: VoiceSettings;
  model: ModelSettings;
  window_mode: WindowModeSettings;
  avatar: AvatarSettings;
  reminder: ReminderSettings;
  startup: StartupSettings;
  memory: MemorySettings;
}

export interface SettingsPayload {
  voice?: Partial<VoiceSettings>;
  model?: Partial<ModelSettings>;
  window_mode?: Partial<WindowModeSettings>;
  avatar?: Partial<AvatarSettings>;
  reminder?: Partial<ReminderSettings>;
  startup?: Partial<StartupSettings>;
  memory?: Partial<MemorySettings>;
}

export interface ChatActionReport {
  type: string;
  status: string;
  task_id?: string | null;
  title?: string | null;
  detail?: string | null;
}

export interface ChatRequestPayload {
  message: string;
  conversation_id?: string | null;
  session_id?: string | null;
  mode?: string;
  selected_date?: string | null;
  include_voice?: boolean;
  voice_mode?: boolean;
  notes_context?: string | null;
}

export interface ChatResponse {
  conversation_id: string;
  reply_text: string;
  emotion: string;
  animation_hint: string;
  speak: boolean;
  audio_url?: string | null;
  task_actions: ChatActionReport[];
  cards: Array<Record<string, unknown>>;
  route?: string | null;
  provider?: string | null;
  latency_ms?: number | null;
  token_usage: Record<string, unknown>;
  fallback_used: boolean;
  plan_id?: string | null;
}

export interface ChatConversationSummary {
  conversation_id: string;
  mode: string;
  created_at: string;
  updated_at: string;
  message_count: number;
  last_message_preview: string;
  last_message_role?: string | null;
  last_message_at?: string | null;
  summary_text?: string | null;
}

export interface ChatConversationListResponse {
  items: ChatConversationSummary[];
  count: number;
}

export interface ChatMessageRecord {
  id: string;
  conversation_id: string;
  role: string;
  content: string;
  emotion?: string | null;
  animation_hint?: string | null;
  metadata: Record<string, unknown>;
  created_at: string;
}

export interface ChatConversationDetailResponse {
  conversation_id: string;
  mode: string;
  created_at: string;
  updated_at: string;
  summary_text?: string | null;
  message_count: number;
  messages: ChatMessageRecord[];
}

export interface AssistantStreamRequest {
  type:
    | "session_start"
    | "context_update"
    | "text_turn"
    | "voice_chunk"
    | "voice_end"
    | "cancel_response"
    | "session_stop";
  session_id?: string | null;
  conversation_id?: string | null;
  message?: string | null;
  selected_date?: string | null;
  voice_mode?: boolean;
  notes_context?: string | null;
  audio_base64?: string | null;
  language?: string | null;
}

export interface AssistantStateChangedEvent {
  type: "assistant_state_changed";
  state: string;
  emotion?: string;
  animation_hint?: string;
  session_id?: string;
}

export interface RouteSelectedEvent {
  type: "route_selected";
  route: string;
  reason?: string;
  provider?: string;
}

export interface TranscriptEvent {
  type: "transcript_partial" | "transcript_final";
  session_id?: string;
  text: string;
}

export interface AssistantChunkEvent {
  type: "assistant_chunk";
  text: string;
}

export interface TaskActionAppliedEvent {
  type: "task_action_applied";
  action: ChatActionReport;
}

export interface SpeechLifecycleEvent {
  type: "speech_started" | "speech_finished";
  utterance_id: string;
}

export interface TtsSentenceReadyEvent {
  type: "tts_sentence_ready";
  text: string;
  audio_url: string;
  duration_ms: number;
}

export interface AssistantFinalEvent {
  type: "assistant_final";
  conversation_id: string;
  session_id: string;
  reply_text: string;
  route?: string | null;
  provider?: string | null;
  latency_ms?: number | null;
  token_usage: Record<string, unknown>;
  fallback_used: boolean;
  plan_id?: string | null;
  cards: Array<Record<string, unknown>>;
  task_actions: ChatActionReport[];
  memory_items?: Array<Record<string, unknown>>;
}

export interface AssistantErrorEvent {
  type: "error";
  session_id?: string;
  detail: string;
}

export type AssistantStreamEvent =
  | AssistantStateChangedEvent
  | RouteSelectedEvent
  | TranscriptEvent
  | AssistantChunkEvent
  | TaskActionAppliedEvent
  | SpeechLifecycleEvent
  | TtsSentenceReadyEvent
  | AssistantFinalEvent
  | AssistantErrorEvent;
