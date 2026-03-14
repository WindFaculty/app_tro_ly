using System;
using System.Collections.Generic;

namespace LocalAssistant.Core
{
    public enum AvatarState
    {
        Idle,
        Listening,
        Thinking,
        Talking,
        Confirming,
        Warning,
        Greeting,
        Waiting,
        Error
    }

    [Serializable]
    public sealed class HealthResponse
    {
        public string status = "error";
        public string service = string.Empty;
        public string version = string.Empty;
        public DatabaseHealth database = new();
        public RuntimeHealthCollection runtimes = new();
        public List<string> degraded_features = new();
        public LogInfo logs = new();
        public List<string> recovery_actions = new();
    }

    [Serializable]
    public sealed class DatabaseHealth
    {
        public bool available;
        public string path = string.Empty;
    }

    [Serializable]
    public sealed class RuntimeHealthCollection
    {
        public RuntimeHealth llm = new();
        public RuntimeHealth stt = new();
        public RuntimeHealth tts = new();
    }

    [Serializable]
    public sealed class RuntimeHealth
    {
        public bool available;
        public string provider = string.Empty;
        public string base_url = string.Empty;
        public string model = string.Empty;
        public string command = string.Empty;
        public string reason = string.Empty;
        public string model_path = string.Empty;
        public string routing_mode = string.Empty;
    }

    [Serializable]
    public sealed class LogInfo
    {
        public string directory = string.Empty;
        public string app_log = string.Empty;
    }

    [Serializable]
    public sealed class TaskRecord
    {
        public string id = string.Empty;
        public string title = string.Empty;
        public string description = string.Empty;
        public string status = "planned";
        public string priority = "medium";
        public string category = string.Empty;
        public string scheduled_date = string.Empty;
        public string start_at = string.Empty;
        public string end_at = string.Empty;
        public string due_at = string.Empty;
        public bool is_all_day;
        public string repeat_rule = "none";
        public int? estimated_minutes;
        public int? actual_minutes;
        public List<string> tags = new();
        public string created_at = string.Empty;
        public string updated_at = string.Empty;
        public string completed_at = string.Empty;
    }

    [Serializable]
    public sealed class TodayTasksResponse
    {
        public string date = string.Empty;
        public List<TaskRecord> items = new();
        public List<TaskRecord> overdue = new();
        public List<TaskRecord> due_soon = new();
        public List<TaskRecord> in_progress = new();
    }

    [Serializable]
    public sealed class WeekDayBucket
    {
        public string date = string.Empty;
        public int task_count;
        public int high_priority_count;
        public List<TaskRecord> items = new();
    }

    [Serializable]
    public sealed class ConflictRecord
    {
        public string date = string.Empty;
        public List<string> task_ids = new();
        public List<string> titles = new();
    }

    [Serializable]
    public sealed class WeekTasksResponse
    {
        public string start_date = string.Empty;
        public string end_date = string.Empty;
        public List<WeekDayBucket> days = new();
        public int overdue_count;
        public List<ConflictRecord> conflicts = new();
    }

    [Serializable]
    public sealed class TaskListResponse
    {
        public List<TaskRecord> items = new();
        public int count;
    }

    [Serializable]
    public sealed class ChatRequestPayload
    {
        public string message = string.Empty;
        public string conversation_id;
        public string session_id;
        public string mode = "text";
        public string selected_date;
        public bool include_voice = true;
        public bool voice_mode;
        public string notes_context;
    }

    [Serializable]
    public sealed class TaskActionReport
    {
        public string type = string.Empty;
        public string status = string.Empty;
        public string task_id = string.Empty;
        public string title = string.Empty;
        public string detail = string.Empty;
    }

    [Serializable]
    public sealed class ChatCard
    {
        public string type = string.Empty;
    }

    [Serializable]
    public sealed class ChatResponsePayload
    {
        public string conversation_id = string.Empty;
        public string reply_text = string.Empty;
        public string emotion = "neutral";
        public string animation_hint = "idle";
        public bool speak;
        public string audio_url = string.Empty;
        public List<TaskActionReport> task_actions = new();
        public List<ChatCard> cards = new();
        public string route = string.Empty;
        public string provider = string.Empty;
        public int latency_ms;
        public TokenUsage token_usage = new();
        public bool fallback_used;
        public string plan_id = string.Empty;
    }

    [Serializable]
    public sealed class TokenUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    [Serializable]
    public sealed class SpeechSttResponse
    {
        public string text = string.Empty;
        public string language = "vi";
        public float confidence;
    }

    [Serializable]
    public sealed class SpeechTtsResponse
    {
        public string audio_url = string.Empty;
        public int duration_ms;
        public bool cached;
    }

    [Serializable]
    public sealed class SettingsPayload
    {
        public VoiceSettings voice = new();
        public ModelSettings model = new();
        public WindowModeSettings window_mode = new();
        public AvatarSettings avatar = new();
        public ReminderSettings reminder = new();
        public StartupSettings startup = new();
        public MemorySettings memory = new();
    }

    [Serializable]
    public sealed class VoiceSettings
    {
        public string input_mode = "continuous";
        public string tts_voice = "vi-VN-default";
        public bool speak_replies = true;
        public bool show_transcript_preview = true;
    }

    [Serializable]
    public sealed class ModelSettings
    {
        public string provider = "hybrid";
        public string name = "llama-3.1-8b-instant | gemini-2.5-flash";
        public string routing_mode = "auto";
        public string fast_provider = "groq";
        public string deep_provider = "gemini";
    }

    [Serializable]
    public sealed class WindowModeSettings
    {
        public bool main_app_enabled = true;
        public bool mini_assistant_enabled;
    }

    [Serializable]
    public sealed class AvatarSettings
    {
        public string character = "default";
        public string lip_sync_mode = "amplitude";
    }

    [Serializable]
    public sealed class ReminderSettings
    {
        public bool speech_enabled = true;
        public int lead_minutes = 15;
    }

    [Serializable]
    public sealed class StartupSettings
    {
        public bool launch_backend = true;
        public bool launch_main_app = true;
    }

    [Serializable]
    public sealed class MemorySettings
    {
        public bool auto_extract = true;
        public int short_term_turn_limit = 12;
    }

    [Serializable]
    public sealed class ReminderDueEvent
    {
        public string type = "reminder_due";
        public string task_id = string.Empty;
        public string title = string.Empty;
        public string scheduled_for = string.Empty;
        public int minutes_until;
    }

    [Serializable]
    public sealed class TaskUpdatedEvent
    {
        public string type = "task_updated";
        public string task_id = string.Empty;
        public string change = string.Empty;
    }

    [Serializable]
    public sealed class AssistantStateChangedEvent
    {
        public string type = "assistant_state_changed";
        public string state = "idle";
        public string emotion = "neutral";
        public string animation_hint = "idle";
    }

    [Serializable]
    public sealed class EventEnvelope
    {
        public string type = string.Empty;
    }

    [Serializable]
    public sealed class SpeechLifecycleEvent
    {
        public string type = string.Empty;
        public string utterance_id = string.Empty;
    }

    [Serializable]
    public sealed class AssistantStreamRequestPayload
    {
        public string type = string.Empty;
        public string session_id = string.Empty;
        public string conversation_id = string.Empty;
        public string message = string.Empty;
        public string selected_date = string.Empty;
        public bool voice_mode;
        public string notes_context = string.Empty;
        public string audio_base64 = string.Empty;
        public string language = "vi";
    }

    [Serializable]
    public sealed class TranscriptEvent
    {
        public string type = string.Empty;
        public string session_id = string.Empty;
        public string text = string.Empty;
    }

    [Serializable]
    public sealed class RouteSelectedEvent
    {
        public string type = string.Empty;
        public string route = string.Empty;
        public string reason = string.Empty;
        public string provider = string.Empty;
    }

    [Serializable]
    public sealed class AssistantChunkEvent
    {
        public string type = string.Empty;
        public string text = string.Empty;
    }

    [Serializable]
    public sealed class AssistantFinalEvent
    {
        public string type = string.Empty;
        public string conversation_id = string.Empty;
        public string session_id = string.Empty;
        public string reply_text = string.Empty;
        public string route = string.Empty;
        public string provider = string.Empty;
        public int latency_ms;
        public TokenUsage token_usage = new();
        public bool fallback_used;
        public string plan_id = string.Empty;
        public List<TaskActionReport> task_actions = new();
        public List<ChatCard> cards = new();
    }

    [Serializable]
    public sealed class TtsSentenceReadyEvent
    {
        public string type = string.Empty;
        public string text = string.Empty;
        public string audio_url = string.Empty;
        public int duration_ms;
    }
}
