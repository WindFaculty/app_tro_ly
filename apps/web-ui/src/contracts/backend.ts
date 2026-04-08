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

export interface WardrobeValidationIssue {
  severity: string;
  code: string;
  message: string;
  slots: string[];
  item_ids: string[];
}

export interface WardrobeItemRecord {
  item_id: string;
  display_name: string;
  slot: string;
  source: string;
  source_asset_path?: string | null;
  prefab_asset_path?: string | null;
  material_asset_paths: string[];
  thumbnail_asset_path?: string | null;
  occupies_slots: string[];
  blocks_slots: string[];
  requires_slots: string[];
  compatible_tags: string[];
  incompatible_tags: string[];
  hide_body_regions: string[];
  anchor_type: string;
  anchor_bone_name?: string | null;
  created_at: string;
  updated_at: string;
  validation_issues: WardrobeValidationIssue[];
  sync_status: string;
}

export interface WardrobeItemCreatePayload {
  item_id: string;
  display_name: string;
  slot: string;
  source?: string;
  source_asset_path?: string | null;
  prefab_asset_path?: string | null;
  material_asset_paths?: string[];
  thumbnail_asset_path?: string | null;
  occupies_slots?: string[];
  blocks_slots?: string[];
  requires_slots?: string[];
  compatible_tags?: string[];
  incompatible_tags?: string[];
  hide_body_regions?: string[];
  anchor_type?: string;
  anchor_bone_name?: string | null;
}

export interface WardrobeItemUpdatePayload {
  display_name?: string;
  slot?: string;
  source?: string;
  source_asset_path?: string | null;
  prefab_asset_path?: string | null;
  material_asset_paths?: string[];
  thumbnail_asset_path?: string | null;
  occupies_slots?: string[];
  blocks_slots?: string[];
  requires_slots?: string[];
  compatible_tags?: string[];
  incompatible_tags?: string[];
  hide_body_regions?: string[];
  anchor_type?: string;
  anchor_bone_name?: string | null;
}

export interface WardrobeOutfitRecord {
  outfit_id: string;
  display_name: string;
  source: string;
  thumbnail_asset_path?: string | null;
  slot_assignments: Record<string, string>;
  created_at: string;
  updated_at: string;
  validation_issues: WardrobeValidationIssue[];
  sync_status: string;
}

export interface WardrobeOutfitCreatePayload {
  outfit_id: string;
  display_name: string;
  source?: string;
  thumbnail_asset_path?: string | null;
  slot_assignments?: Record<string, string>;
}

export interface WardrobeOutfitUpdatePayload {
  display_name?: string;
  source?: string;
  thumbnail_asset_path?: string | null;
  slot_assignments?: Record<string, string>;
}

export interface WardrobeSlotRecord {
  key: string;
  unity_enum: string;
  display_name: string;
  reserved: boolean;
  item_count: number;
  outfit_count: number;
  notes: string[];
}

export interface WardrobeSummary {
  slot_count: number;
  reserved_slot_count: number;
  item_count: number;
  outfit_count: number;
  warning_count: number;
  error_count: number;
  ready_item_count: number;
  ready_outfit_count: number;
}

export interface WardrobeSnapshotResponse {
  version: number;
  updated_at: string;
  registry_path: string;
  slot_taxonomy_version: number;
  slots: WardrobeSlotRecord[];
  items: WardrobeItemRecord[];
  outfits: WardrobeOutfitRecord[];
  summary: WardrobeSummary;
}

export interface WardrobeImportPayload {
  mode?: "merge" | "replace";
  items?: WardrobeItemCreatePayload[];
  outfits?: WardrobeOutfitCreatePayload[];
}

export interface GoogleEmailStatusResponse {
  provider: string;
  status: string;
  configured: boolean;
  connected: boolean;
  sync_enabled: boolean;
  email_address?: string | null;
  auth_url_available: boolean;
  redirect_uri?: string | null;
  default_label: string;
  query_limit: number;
  scopes: string[];
  last_sync_at?: string | null;
  last_error?: string | null;
  detail: string;
}

export interface EmailMessageSummary {
  id: string;
  thread_id: string;
  subject: string;
  from_display: string;
  from_address: string;
  to: string[];
  snippet: string;
  labels: string[];
  is_read: boolean;
  starred: boolean;
  has_attachments: boolean;
  received_at?: string | null;
  linked_task_ids: string[];
}

export interface EmailMessageDetail extends EmailMessageSummary {
  cc: string[];
  body_text: string;
  body_html?: string | null;
}

export interface EmailMessageListResponse {
  account: GoogleEmailStatusResponse;
  query: string;
  label: string;
  items: EmailMessageSummary[];
  count: number;
  draft_count: number;
}

export interface EmailDraftCreatePayload {
  to?: string[];
  cc?: string[];
  bcc?: string[];
  subject?: string;
  body_text?: string;
  linked_message_id?: string | null;
  thread_id?: string | null;
}

export interface EmailDraftUpdatePayload extends EmailDraftCreatePayload {}

export interface EmailDraftRecord {
  id: string;
  provider: string;
  thread_id?: string | null;
  linked_message_id?: string | null;
  to: string[];
  cc: string[];
  bcc: string[];
  subject: string;
  body_text: string;
  status: string;
  gmail_message_id?: string | null;
  created_at: string;
  updated_at: string;
  sent_at?: string | null;
}

export interface EmailDraftListResponse {
  items: EmailDraftRecord[];
  count: number;
}

export interface GoogleEmailConnectResponse {
  authorization_url: string;
  redirect_uri: string;
  state: string;
}

export interface GoogleCalendarStatusResponse {
  provider: string;
  status: string;
  configured: boolean;
  connected: boolean;
  sync_enabled: boolean;
  calendar_id?: string | null;
  auth_url_available: boolean;
  redirect_uri?: string | null;
  default_calendar_id: string;
  agenda_days: number;
  event_limit: number;
  scopes: string[];
  last_sync_at?: string | null;
  last_error?: string | null;
  detail: string;
}

export interface GoogleCalendarEventRecord {
  id: string;
  calendar_id: string;
  status: string;
  summary: string;
  description?: string | null;
  location?: string | null;
  html_link?: string | null;
  conference_link?: string | null;
  organizer_email?: string | null;
  creator_email?: string | null;
  attendees: string[];
  start_at?: string | null;
  end_at?: string | null;
  start_date?: string | null;
  end_date?: string | null;
  is_all_day: boolean;
  created_at?: string | null;
  updated_at?: string | null;
}

export interface GoogleCalendarEventCreatePayload {
  summary: string;
  description?: string | null;
  location?: string | null;
  attendees?: string[];
  calendar_id?: string | null;
  is_all_day?: boolean;
  start_at?: string | null;
  end_at?: string | null;
  start_date?: string | null;
  end_date?: string | null;
}

export interface GoogleCalendarEventUpdatePayload {
  summary?: string;
  description?: string | null;
  location?: string | null;
  attendees?: string[];
  calendar_id?: string | null;
  is_all_day?: boolean;
  start_at?: string | null;
  end_at?: string | null;
  start_date?: string | null;
  end_date?: string | null;
}

export interface GoogleCalendarEventListResponse {
  account: GoogleCalendarStatusResponse;
  calendar_id: string;
  start_date: string;
  end_date: string;
  query: string;
  time_zone?: string | null;
  items: GoogleCalendarEventRecord[];
  count: number;
}

export interface GoogleCalendarConnectResponse {
  authorization_url: string;
  redirect_uri: string;
  state: string;
}

export interface GoogleCalendarDeleteResponse {
  status: string;
  event_id: string;
  calendar_id: string;
}

export interface EmailToTaskPayload {
  title?: string | null;
  priority?: string;
  scheduled_date?: string | null;
  due_at?: string | null;
  tags?: string[];
}

export interface BrowserAutomationTemplateField {
  key: string;
  label: string;
  required: boolean;
  placeholder?: string | null;
  help_text?: string | null;
}

export interface BrowserAutomationTemplateRecord {
  template_id: string;
  title: string;
  description: string;
  step_count: number;
  fields: BrowserAutomationTemplateField[];
}

export interface BrowserAutomationTemplateListResponse {
  items: BrowserAutomationTemplateRecord[];
  count: number;
}

export interface BrowserAutomationRunCreatePayload {
  template_id: string;
  title: string;
  goal: string;
  inputs?: Record<string, unknown>;
}

export interface BrowserAutomationApprovalPayload {
  approval_note?: string | null;
}

export interface BrowserAutomationRejectPayload {
  reason: string;
}

export interface BrowserAutomationCancelPayload {
  reason?: string | null;
}

export interface BrowserAutomationStepRecord {
  id: string;
  position: number;
  action_type: string;
  title: string;
  description: string;
  status: string;
  requires_approval: boolean;
  url?: string | null;
  approval_note?: string | null;
  recovery_notes: string;
  result: Record<string, unknown>;
  updated_at: string;
  completed_at?: string | null;
}

export interface BrowserAutomationLogRecord {
  id: string;
  run_id: string;
  step_id?: string | null;
  level: string;
  code: string;
  message: string;
  payload: Record<string, unknown>;
  created_at: string;
}

export interface BrowserAutomationRunSummary {
  id: string;
  template_id: string;
  title: string;
  goal: string;
  status: string;
  current_step_index: number;
  step_count: number;
  pending_step_title?: string | null;
  last_log_message?: string | null;
  created_at: string;
  updated_at: string;
  completed_at?: string | null;
  cancelled_at?: string | null;
}

export interface BrowserAutomationRunDetail extends BrowserAutomationRunSummary {
  start_url?: string | null;
  inputs: Record<string, unknown>;
  steps: BrowserAutomationStepRecord[];
  logs: BrowserAutomationLogRecord[];
}

export interface BrowserAutomationRunListResponse {
  items: BrowserAutomationRunSummary[];
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

export interface SpeechSttResponse {
  text: string;
  language: string;
  confidence: number;
}

export interface SpeechTtsResponse {
  audio_url: string;
  duration_ms: number;
  cached: boolean;
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

export interface GoogleEmailSettings {
  sync_enabled: boolean;
  default_label: string;
  query_limit: number;
  [key: string]: unknown;
}

export interface GoogleCalendarSettings {
  sync_enabled: boolean;
  default_calendar_id: string;
  agenda_days: number;
  event_limit: number;
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
  google_email: GoogleEmailSettings;
  google_calendar: GoogleCalendarSettings;
}

export interface SettingsPayload {
  voice?: Partial<VoiceSettings>;
  model?: Partial<ModelSettings>;
  window_mode?: Partial<WindowModeSettings>;
  avatar?: Partial<AvatarSettings>;
  reminder?: Partial<ReminderSettings>;
  startup?: Partial<StartupSettings>;
  memory?: Partial<MemorySettings>;
  google_email?: Partial<GoogleEmailSettings>;
  google_calendar?: Partial<GoogleCalendarSettings>;
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
