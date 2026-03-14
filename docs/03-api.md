# API Spec - Local Desktop Assistant

Updated: 2026-03-14
Base URL: `/v1`

## Conventions

- The Python backend is the source of truth for task, reminder, settings, session, and conversation state.
- Times are stored and returned as ISO 8601 strings in local time unless otherwise noted.
- Natural-language task mutations must be validated by backend rules before they change the database.
- `POST /chat` and `WS /assistant/stream` share the same assistant orchestration pipeline.
- REST covers request-response flows.
- WebSockets cover reminder events, assistant state, and live streamed assistant turns.
- No auth layer is planned for MVP because the app targets trusted local use.

## Enumerations

### Task status

- `inbox`
- `planned`
- `in_progress`
- `done`
- `cancelled`

### Task priority

- `low`
- `medium`
- `high`
- `critical`

### Repeat rule

- `none`
- `daily`
- `weekdays`
- `weekly`
- `monthly`

### Assistant emotion

- `neutral`
- `happy`
- `serious`
- `warning`
- `thinking`

### Animation hint

- `idle`
- `greet`
- `nod`
- `listen`
- `think`
- `explain`
- `confirm`
- `alert`

## Canonical Task Shape

```json
{
  "id": "task_01hq7r8m6k8x",
  "title": "Submit weekly report",
  "description": "Final pass before sending to the team lead",
  "status": "planned",
  "priority": "high",
  "category": "work",
  "scheduled_date": "2026-03-13",
  "start_at": "2026-03-13T14:00:00",
  "end_at": "2026-03-13T15:00:00",
  "due_at": "2026-03-14T17:00:00",
  "is_all_day": false,
  "repeat_rule": "none",
  "estimated_minutes": 60,
  "actual_minutes": null,
  "tags": ["report", "deadline"],
  "created_at": "2026-03-13T08:00:00",
  "updated_at": "2026-03-13T08:00:00",
  "completed_at": null
}
```

Notes:

- `scheduled_date` is the primary calendar bucket for day and week views.
- `due_at` is optional and is used for deadlines and reminders.
- `start_at` and `end_at` are optional for time-blocked work.

## Health

- `GET /v1/health`

Response shape:

```json
{
  "status": "partial",
  "service": "local-desktop-assistant-backend",
  "version": "0.1.0",
  "database": {
    "available": true,
    "path": "D:/Antigaravity_Code/tro_ly/local-backend/data/app.db"
  },
  "runtimes": {
    "llm": {
      "available": true,
      "provider": "hybrid",
      "model": "llama-3.1-8b-instant | gemini-2.5-flash",
      "routing_mode": "auto",
      "fast": {
        "available": true,
        "provider": "groq"
      },
      "deep": {
        "available": true,
        "provider": "gemini"
      },
      "base_url": "hybrid"
    },
    "stt": {
      "available": true,
      "provider": "faster-whisper",
      "model_path": "small"
    },
    "tts": {
      "available": false,
      "provider": "piper",
      "reason": "command not configured or not found"
    }
  },
  "degraded_features": ["tts"],
  "logs": {
    "directory": "D:/Antigaravity_Code/tro_ly/local-backend/data/logs",
    "app_log": "D:/Antigaravity_Code/tro_ly/local-backend/data/logs/local-desktop-assistant-backend.log"
  },
  "recovery_actions": [
    "Configure assistant_piper_command and assistant_piper_model_path for speech output."
  ]
}
```

Status values:

- `ready`
- `partial`
- `error`

## Task Queries

- `GET /v1/tasks/today`
- `GET /v1/tasks/week`
- `GET /v1/tasks/overdue`
- `GET /v1/tasks/inbox`
- `GET /v1/tasks/completed`

Query params:

- `date=YYYY-MM-DD` for day lookups
- `start_date=YYYY-MM-DD` for week aggregation
- `limit=<int>` for inbox or completed history

Example `GET /v1/tasks/week` response:

```json
{
  "start_date": "2026-03-13",
  "end_date": "2026-03-19",
  "days": [
    {
      "date": "2026-03-13",
      "task_count": 4,
      "high_priority_count": 2,
      "items": []
    }
  ],
  "overdue_count": 1,
  "conflicts": []
}
```

## Task Mutations

### Create task

- `POST /v1/tasks`

Request:

```json
{
  "title": "Team meeting",
  "description": "Sprint review",
  "status": "planned",
  "priority": "medium",
  "scheduled_date": "2026-03-14",
  "start_at": "2026-03-14T14:00:00",
  "end_at": "2026-03-14T15:00:00",
  "due_at": null,
  "is_all_day": false,
  "repeat_rule": "none",
  "estimated_minutes": 60,
  "tags": ["meeting"]
}
```

### Update task

- `PUT /v1/tasks/{task_id}`

Behavior:

- Partial updates are allowed.
- Backend normalizes time fields and rejects contradictory combinations.
- Returns `404` when the task does not exist.

### Complete task

- `POST /v1/tasks/{task_id}/complete`

Request:

```json
{
  "completed_at": "2026-03-13T17:45:00"
}
```

### Reschedule task

- `POST /v1/tasks/{task_id}/reschedule`

Request:

```json
{
  "scheduled_date": "2026-03-15",
  "start_at": "2026-03-15T10:00:00",
  "end_at": "2026-03-15T11:00:00",
  "due_at": "2026-03-15T17:00:00"
}
```

## Chat

- `POST /v1/chat`

Request:

```json
{
  "message": "What do I have today?",
  "conversation_id": null,
  "session_id": null,
  "mode": "text",
  "selected_date": "2026-03-13",
  "include_voice": true,
  "voice_mode": false,
  "notes_context": null
}
```

Response:

```json
{
  "conversation_id": "conv_01hq7sd2v0t9",
  "reply_text": "Ban co 4 viec hom nay, trong do co 2 viec uu tien cao.",
  "emotion": "serious",
  "animation_hint": "explain",
  "speak": true,
  "audio_url": "/v1/speech/cache/a13b4b.wav",
  "task_actions": [],
  "cards": [
    {
      "type": "today_summary",
      "payload": {}
    }
  ],
  "route": "groq_fast",
  "provider": "groq",
  "latency_ms": 412,
  "token_usage": {
    "input_tokens": 120,
    "output_tokens": 40
  },
  "fallback_used": false,
  "plan_id": null
}
```

Notes:

- The backend parses intent before or alongside model generation.
- Database writes happen only through validated backend actions.
- `task_actions` reports what the backend actually applied, not raw model guesses.
- `cards` may include planner output, task action details, or summary payloads.

## Assistant Streaming

- `WS /v1/assistant/stream`

This is the preferred path for the Unity live assistant.

Inbound message types:

- `session_start`
- `context_update`
- `text_turn`
- `voice_chunk`
- `voice_end`
- `cancel_response`
- `session_stop`

Example text turn:

```json
{
  "type": "text_turn",
  "session_id": "sess_01",
  "conversation_id": "conv_01",
  "message": "Lap ke hoach cho hom nay",
  "selected_date": "2026-03-14",
  "voice_mode": false,
  "notes_context": "Deadline bao cao vao 17:00."
}
```

Example voice end message:

```json
{
  "type": "voice_end",
  "session_id": "sess_01",
  "voice_mode": true,
  "language": "vi",
  "audio_base64": "<base64 wav chunk>"
}
```

Common outbound event types:

- `assistant_state_changed`
- `route_selected`
- `transcript_partial`
- `transcript_final`
- `assistant_chunk`
- `speech_started`
- `tts_sentence_ready`
- `speech_finished`
- `task_action_applied`
- `assistant_final`
- `error`

Example `assistant_final` event:

```json
{
  "type": "assistant_final",
  "conversation_id": "conv_01",
  "session_id": "sess_01",
  "reply_text": "Minh da tong hop xong va uu tien viec quan trong nhat truoc.",
  "route": "hybrid_plan_then_groq",
  "provider": "groq",
  "latency_ms": 923,
  "token_usage": {
    "input_tokens": 420,
    "output_tokens": 98
  },
  "fallback_used": false,
  "plan_id": "plan_01",
  "cards": [],
  "task_actions": [],
  "memory_items": []
}
```

## Reminder and State Events

- `WS /v1/events`

This socket accepts a client and immediately sends an idle `assistant_state_changed` event. It then forwards published events such as reminders and task updates.

### `reminder_due`

```json
{
  "type": "reminder_due",
  "task_id": "task_01hq7r8m6k8x",
  "title": "Team meeting",
  "scheduled_for": "2026-03-14T14:00:00",
  "minutes_until": 15
}
```

### `task_updated`

```json
{
  "type": "task_updated",
  "task_id": "task_01hq7r8m6k8x",
  "change": "completed"
}
```

### `assistant_state_changed`

```json
{
  "type": "assistant_state_changed",
  "state": "thinking",
  "emotion": "thinking",
  "animation_hint": "think"
}
```

## Speech

### Speech to text

- `POST /v1/speech/stt`

Multipart request:

- `audio`: wav or mp3 upload from the Unity client
- `language`: optional override

Response:

```json
{
  "text": "Move task A to Friday",
  "language": "en",
  "confidence": 0.92
}
```

Notes:

- Default STT path is `faster-whisper`.
- If that path is unavailable, the backend can fall back to `whisper.cpp` when configured.

### Text to speech

- `POST /v1/speech/tts`

Request:

```json
{
  "text": "Your report is due tomorrow at 5 PM.",
  "voice": "vi-VN-default",
  "cache": true
}
```

Response:

```json
{
  "audio_url": "/v1/speech/cache/tts_01hq7sga.wav",
  "duration_ms": 2140,
  "cached": false
}
```

Notes:

- TTS can be backed by `piper` or `chattts`, depending on `assistant_tts_provider`.

### Speech cache

- `GET /v1/speech/cache/{filename}`

Returns a generated or cached audio file.

## Settings

- `GET /v1/settings`
- `PUT /v1/settings`

Setting groups:

- `voice`
- `model`
- `window_mode`
- `avatar`
- `reminder`
- `startup`
- `memory`

Example:

```json
{
  "voice": {
    "input_mode": "continuous",
    "tts_voice": "vi-VN-default",
    "speak_replies": true,
    "show_transcript_preview": true
  },
  "model": {
    "provider": "hybrid",
    "name": "llama-3.1-8b-instant | gemini-2.5-flash",
    "routing_mode": "auto",
    "fast_provider": "groq",
    "deep_provider": "gemini"
  },
  "window_mode": {
    "main_app_enabled": true,
    "mini_assistant_enabled": false
  },
  "memory": {
    "auto_extract": true,
    "short_term_turn_limit": 12
  }
}
