# API Spec - Local Desktop Assistant

Updated: 2026-03-13
Base URL: `/v1`

## Conventions
- The Python backend is the source of truth for task, reminder, settings, and conversation state.
- Times are stored and returned as ISO 8601 strings in local time unless otherwise noted.
- Natural-language task mutations must be validated by backend rules before they change the database.
- REST covers request-response flows.
- WebSocket covers reminder and assistant state events.
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
  "status": "ready",
  "service": "local-desktop-assistant-backend",
  "version": "0.1.0",
  "database": {
    "available": true,
    "path": "D:/Antigaravity_Code/tro_ly/local-backend/data/app.db"
  },
  "runtimes": {
    "llm": {
      "available": true,
      "provider": "groq",
      "base_url": "https://api.groq.com/openai/v1",
      "model": "llama-3.1-8b-instant"
    },
    "stt": {
      "available": true,
      "provider": "whisper.cpp"
    },
    "tts": {
      "available": true,
      "provider": "piper"
    }
  },
  "degraded_features": []
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

Suggested query params:
- `date=YYYY-MM-DD` for today-style lookups when the client wants a specific date
- `start_date=YYYY-MM-DD` for week aggregation
- `limit=<int>` for completed history or inbox trimming

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

## Create Task
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

## Update Task
- `PUT /v1/tasks/{task_id}`

Behavior:
- Partial updates are allowed.
- Backend normalizes time fields and rejects contradictory combinations.

## Complete Task
- `POST /v1/tasks/{task_id}/complete`

Request:

```json
{
  "completed_at": "2026-03-13T17:45:00"
}
```

## Reschedule Task
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
  "mode": "text",
  "selected_date": "2026-03-13",
  "include_voice": true
}
```

Response:

```json
{
  "conversation_id": "conv_01hq7sd2v0t9",
  "reply_text": "You have 4 tasks today, including 2 high-priority items.",
  "emotion": "serious",
  "animation_hint": "explain",
  "speak": true,
  "audio_url": "/v1/speech/cache/reply_01hq7sd2.wav",
  "task_actions": [
    {
      "type": "summary",
      "status": "applied"
    }
  ],
  "cards": [
    {
      "type": "today_summary"
    }
  ]
}
```

Notes:
- The backend should parse intent before or alongside model generation.
- Database writes must only happen through validated backend actions.
- `task_actions` reports what the backend actually applied, not raw model guesses.

## Speech to Text
- `POST /v1/speech/stt`

Multipart request:
- `audio`: wav or mp3 upload from the Unity client
- `language`: optional, defaults to auto

Response:

```json
{
  "text": "Move task A to Friday",
  "language": "en",
  "confidence": 0.92
}
```

## Text to Speech
- `POST /v1/speech/tts`

Request:

```json
{
  "text": "Your report is due tomorrow at 5 PM.",
  "voice": "default_female",
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

## Settings
- `GET /v1/settings`
- `PUT /v1/settings`

Suggested setting groups:
- voice
- model
- window_mode
- avatar
- reminder
- startup

Example:

```json
{
  "voice": {
    "input_mode": "push_to_talk",
    "tts_voice": "default_female",
    "speak_replies": true
  },
  "model": {
    "provider": "groq",
    "name": "llama-3.1-8b-instant"
  },
  "window_mode": {
    "mini_assistant_enabled": true
  }
}
```

## WebSocket Events

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

### `speech_started`

```json
{
  "type": "speech_started",
  "utterance_id": "utt_01hq7sjq6m"
}
```

### `speech_finished`

```json
{
  "type": "speech_finished",
  "utterance_id": "utt_01hq7sjq6m"
}
```
