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

export interface SettingsResponse {
  voice: Record<string, unknown>;
  model: Record<string, unknown>;
  window_mode: Record<string, unknown>;
  avatar: Record<string, unknown>;
  reminder: Record<string, unknown>;
  startup: Record<string, unknown>;
  memory: Record<string, unknown>;
}

export interface ChatActionReport {
  type: string;
  status: string;
  task_id?: string | null;
  title?: string | null;
  detail?: string | null;
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
