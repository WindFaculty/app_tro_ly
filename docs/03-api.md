# API - Current Backend Contract

Updated: 2026-04-07
Base path: `/v1`

This document reflects the routes and payload shapes implemented in `local-backend/app/api/routes.py` and `local-backend/app/models/schemas.py`.

## General Rules

- The backend is the source of truth for task, note, reminder, conversation, settings, and session state.
- Task mutations happen only through backend validation and service logic.
- `POST /chat` and `WS /assistant/stream` use the same assistant orchestration pipeline.
- There is no auth layer in the current repo.

## Health

### `GET /v1/health`

Returns:

- `status`: `ready`, `partial`, or `error`
- `service`
- `version`
- `database`
- `runtimes.llm`
- `runtimes.stt`
- `runtimes.tts`
- `degraded_features`
- `logs`
- `recovery_actions`

Behavior:

- `error` is used when the database is unavailable.
- `partial` is used when the database is available but one or more runtimes are unavailable.
- `ready` is used when database and all three runtime groups are available.

## Task Queries

### `GET /v1/tasks/today`

Query:

- `date=YYYY-MM-DD` optional

Returns the current day snapshot from `TaskService.list_day(...)`.

### `GET /v1/tasks/week`

Query:

- `start_date=YYYY-MM-DD` optional

Returns the current week snapshot from `TaskService.list_week(...)`.

### `GET /v1/tasks/overdue`

Returns overdue tasks.

### `GET /v1/tasks/inbox`

Query:

- `limit` default `50`, min `1`, max `200`

### `GET /v1/tasks/active`

Query:

- `limit` default `100`, min `1`, max `200`

Returns the current active-task list used by note linking and cross-module search surfaces.

### `GET /v1/tasks/completed`

Query:

- `limit` default `50`, min `1`, max `200`

## Task Mutations

### `POST /v1/tasks`

Request model: `TaskCreateRequest`

Important fields:

- `title`
- `description`
- `status`
- `priority`
- `category`
- `scheduled_date`
- `start_at`
- `end_at`
- `due_at`
- `is_all_day`
- `repeat_rule`
- `repeat_config_json`
- `estimated_minutes`
- `actual_minutes`
- `tags`

Behavior:

- Returns `400` on validation errors.
- Publishes `task_updated` with `change: "created"` on success.

### `PUT /v1/tasks/{task_id}`

Request model: `TaskUpdateRequest`

Behavior:

- Partial updates are allowed.
- Returns `404` if the task does not exist.
- Returns `400` for rejected values.
- Publishes `task_updated` with `change: "updated"` on success.

### `POST /v1/tasks/{task_id}/complete`

Request model: `CompleteTaskRequest`

Behavior:

- Returns `404` if the task does not exist.
- Publishes `task_updated` with `change: "completed"` on success.

### `POST /v1/tasks/{task_id}/reschedule`

Request model: `RescheduleTaskRequest`

Behavior:

- Returns `404` if the task does not exist.
- Returns `400` for rejected values.
- Publishes `task_updated` with `change: "rescheduled"` on success.

## Notes

### `GET /v1/notes`

Query:

- `limit` default `100`, min `1`, max `200`
- `tag` optional exact tag filter
- `linked_task_id` optional
- `linked_conversation_id` optional

Response model: `NoteListResponse`

Each item includes:

- `id`
- `title`
- `body`
- `tags`
- `linked_task_id`
- `linked_conversation_id`
- `pinned`
- `created_at`
- `updated_at`

Behavior notes:

- Notes are ordered by `pinned DESC`, then most-recent `updated_at`.
- Link filters are validated only on create or update, not on list.

### `POST /v1/notes`

Request model: `NoteCreateRequest`

Important fields:

- `title`
- `body`
- `tags`
- `linked_task_id`
- `linked_conversation_id`
- `pinned`

Behavior:

- Returns `400` when title is empty or a linked task or conversation does not exist.

### `PUT /v1/notes/{note_id}`

Request model: `NoteUpdateRequest`

Behavior:

- Partial updates are allowed.
- Returns `404` if the note does not exist.
- Returns `400` for rejected linked ids or invalid payload values.

## Memory

### `GET /v1/memory/items`

Query:

- `limit` default `50`, min `1`, max `200`

Response model: `MemoryListResponse`

Each item includes:

- `id`
- `category`
- `content`
- `confidence`
- `status`
- `metadata`
- `source_conversation_id`
- `created_at`
- `updated_at`

Behavior notes:

- Returns the current active memory items ordered by strongest confidence and newest update first.
- This route is read-only in the current implementation and exists to support the desktop knowledge module.

## Chat

### `POST /v1/chat`

Request model: `ChatRequest`

Fields:

- `message`
- `conversation_id`
- `session_id`
- `mode`
- `selected_date`
- `include_voice`
- `voice_mode`
- `notes_context`

Response model: `ChatResponse`

Fields:

- `conversation_id`
- `reply_text`
- `emotion`
- `animation_hint`
- `speak`
- `audio_url`
- `task_actions`
- `cards`
- `route`
- `provider`
- `latency_ms`
- `token_usage`
- `fallback_used`
- `plan_id`

Notes:

- The backend may answer deterministically from planner output without requiring a deep plan.
- `task_actions` reports validated backend-applied actions, not raw model guesses.
- If TTS synthesis fails in compatibility mode, the response falls back to `speak: false`.
- Assistant message history now persists route, provider, latency, fallback, `cards`, and `task_actions` metadata on the stored assistant turn.

### `GET /v1/chat/conversations`

Query:

- `limit` default `20`, min `1`, max `100`

Response model: `ChatConversationListResponse`

Each item includes:

- `conversation_id`
- `mode`
- `created_at`
- `updated_at`
- `message_count`
- `last_message_preview`
- `last_message_role`
- `last_message_at`
- `summary_text`

Behavior notes:

- Returns recent conversations ordered by most-recent `updated_at`.
- `last_message_preview` is derived from the latest stored message for that conversation.
- `summary_text` comes from the current rolling conversation summary when one exists.

### `GET /v1/chat/conversations/{conversation_id}`

Response model: `ChatConversationDetailResponse`

Fields:

- `conversation_id`
- `mode`
- `created_at`
- `updated_at`
- `summary_text`
- `message_count`
- `messages`

Each `messages[]` item includes:

- `id`
- `conversation_id`
- `role`
- `content`
- `emotion`
- `animation_hint`
- `metadata`
- `created_at`

Behavior notes:

- Returns `404` when the conversation does not exist.
- `metadata` mirrors the stored message metadata currently kept in SQLite for that turn.

## Assistant Streaming

### `WS /v1/assistant/stream`

Inbound message model: `AssistantStreamMessage`

Current inbound `type` values used by the backend:

- `session_start`
- `context_update`
- `text_turn`
- `voice_chunk`
- `voice_end`
- `cancel_response`
- `session_stop`

Current outbound event types:

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

Behavior notes:

- `session_start` and `context_update` only update stream state and metadata.
- `text_turn` runs the full assistant pipeline immediately.
- `voice_chunk` can emit incremental transcript previews.
- `voice_end` emits `transcript_final` and then, if there is transcript text, runs the assistant pipeline.
- `assistant_final` includes `cards`, `task_actions`, route metadata, and stored memory items.

## Event Stream

### `WS /v1/events`

Behavior:

- Accepts immediately.
- Sends an initial `assistant_state_changed` event with idle state.
- Forwards events published by the backend event bus.

Current event types observed from code:

- `assistant_state_changed`
- `task_updated`
- `reminder_due`

## Speech

### `POST /v1/speech/stt`

Multipart fields:

- `audio` required
- `language` optional query parameter

Response model: `SpeechSttResponse`

- `text`
- `language`
- `confidence`

Failure behavior:

- Returns `503` when STT is unavailable or runtime transcription fails.

### `POST /v1/speech/tts`

Request model: `SpeechTtsRequest`

- `text`
- `voice`
- `cache`

Response model: `SpeechTtsResponse`

- `audio_url`
- `duration_ms`
- `cached`

Failure behavior:

- Returns `503` when TTS is unavailable or synthesis fails.

### `GET /v1/speech/cache/{filename}`

Returns the generated audio file if it exists.

Returns `404` if the file does not exist.

## Settings

### `GET /v1/settings`

Returns the merged settings view built from:

- hardcoded defaults in `SettingsService`
- persisted SQLite settings
- runtime-backed model defaults from current backend configuration

Current top-level groups:

- `voice`
- `model`
- `window_mode`
- `avatar`
- `reminder`
- `startup`
- `memory`

### `PUT /v1/settings`

Request model: `SettingsPayload`

Behavior:

- Merges the provided nested payload into current settings.
- Persists each top-level group in SQLite.
- Returns the merged settings snapshot.

### `POST /v1/settings/reset`

Behavior:

- Clears persisted `app_settings` rows from SQLite.
- Rebuilds the returned snapshot from `SettingsService` defaults plus current backend runtime configuration.
- This resets user-managed settings groups without changing provider secrets or other backend environment values.

## Current Enumerations

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
