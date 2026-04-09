# API - Current Backend Contract

Updated: 2026-04-07
Base path: `/v1`

This document reflects the routes and payload shapes implemented in `local-backend/app/api/routes.py` and `local-backend/app/models/schemas.py`.

## General Rules

- The backend is the source of truth for task, note, wardrobe, reminder, conversation, settings, and session state.
- Task mutations happen only through backend validation and service logic.
- `POST /chat` and `WS /assistant/stream` use the same assistant orchestration pipeline.
- There is no app-level user auth layer in the current repo.
- Google email uses a local OAuth callback flow for provider access only; that is separate from any future app auth.

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

## Wardrobe

### `GET /v1/wardrobe`

Response model: `WardrobeSnapshotResponse`

Fields:

- `version`
- `updated_at`
- `registry_path`
- `slot_taxonomy_version`
- `slots`
- `items`
- `outfits`
- `summary`

Behavior notes:

- Creates `local-backend/data/wardrobe/registry.json` on first access if the file does not exist yet.
- Seeds the initial registry from `ai-dev-system/domain/customization/contracts/slot-taxonomy.json` plus `ai-dev-system/domain/customization/sample-data/current-item-catalog.json`.
- Returns derived validation issues and sync status for both item records and outfit presets.

### `GET /v1/wardrobe/export`

Response model: `WardrobeSnapshotResponse`

Behavior notes:

- Returns the current wardrobe registry snapshot in the same sync-ready JSON shape used by the desktop export surface.
- Export does not claim live Unity runtime registry wiring; it only reflects the desktop-side wardrobe data system.

### `POST /v1/wardrobe/import`

Request model: `WardrobeImportRequest`

Fields:

- `mode`: `merge` or `replace`
- `items`
- `outfits`

Behavior notes:

- `merge` upserts imported items and presets by id.
- `replace` discards the current desktop registry and rebuilds it from the imported payload.
- Returns `400` when imported slots, body regions, anchor types, or preset item references do not match the current contract.

### `POST /v1/wardrobe/items`

Request model: `WardrobeItemCreateRequest`

Important fields:

- `item_id`
- `display_name`
- `slot`
- `source`
- `prefab_asset_path`
- `occupies_slots`
- `blocks_slots`
- `requires_slots`
- `compatible_tags`
- `incompatible_tags`
- `hide_body_regions`
- `anchor_type`
- `anchor_bone_name`

Behavior notes:

- Persists the item into the JSON wardrobe registry, not SQLite.
- Returns derived `validation_issues` plus `sync_status` on the created record.
- Returns `400` if the item id already exists or if slot-related fields reference unknown contract keys.

### `PUT /v1/wardrobe/items/{item_id}`

Request model: `WardrobeItemUpdateRequest`

Behavior notes:

- Partial updates are allowed.
- Returns `404` if the item does not exist.
- Returns `400` when the updated slot or compatibility fields violate the current contract, or when the item slot would change while an outfit still references that item.

### `DELETE /v1/wardrobe/items/{item_id}`

Response model: `WardrobeSnapshotResponse`

Behavior notes:

- Removes the item from the JSON registry.
- Any preset assignment that referenced the deleted item is removed from that preset before the new snapshot is returned.

### `POST /v1/wardrobe/outfits`

Request model: `WardrobeOutfitCreateRequest`

Important fields:

- `outfit_id`
- `display_name`
- `source`
- `thumbnail_asset_path`
- `slot_assignments`

Behavior notes:

- Each `slot_assignments` key must match the assigned item's canonical slot.
- The returned outfit record includes derived validation issues such as `dress_with_separates`, blocked-slot conflicts, missing required slots, occupied-slot conflicts, and incompatible tags.

### `PUT /v1/wardrobe/outfits/{outfit_id}`

Request model: `WardrobeOutfitUpdateRequest`

Behavior notes:

- Partial updates are allowed.
- Returns `404` if the preset does not exist.
- Returns `400` when an assignment references an unknown item or mismatched slot.

### `DELETE /v1/wardrobe/outfits/{outfit_id}`

Response model: `WardrobeSnapshotResponse`

Behavior notes:

- Removes the preset from the JSON wardrobe registry and returns the latest snapshot.

## Google Calendar

### `GET /v1/calendar/status`

Response model: `GoogleCalendarStatusResponse`

Fields include:

- `provider`
- `status`
- `configured`
- `connected`
- `sync_enabled`
- `calendar_id`
- `auth_url_available`
- `redirect_uri`
- `default_calendar_id`
- `agenda_days`
- `event_limit`
- `scopes`
- `last_sync_at`
- `last_error`
- `detail`

Behavior notes:

- `status` may be `not_configured`, `disconnected`, `disabled`, `ready`, or `error`.
- This route is safe to call even when Google OAuth is not configured; it returns capability state instead of raising.

### `GET /v1/calendar/google/connect`

Response model: `GoogleCalendarConnectResponse`

Fields:

- `authorization_url`
- `redirect_uri`
- `state`

Behavior notes:

- Returns `400` when backend Google OAuth credentials are not configured.
- The desktop shell opens `authorization_url` in the browser and the OAuth redirect returns to the local backend callback.

### `GET /v1/calendar/google/callback`

Query:

- `code`
- `state`
- `error`

Behavior notes:

- Completes the local Google OAuth exchange, stores tokens in SQLite, and returns a simple HTML success or failure page for the browser window.
- Returns `400` on missing or mismatched callback state.
- Returns `503` if the token exchange or primary-calendar lookup fails.

### `POST /v1/calendar/google/disconnect`

Response model: `GoogleCalendarStatusResponse`

Behavior:

- Clears stored Google OAuth token state from SQLite and returns the new disconnected status snapshot.

### `GET /v1/calendar/events`

Query:

- `start_date` optional
- `days` default `7`, min `1`, max `31`
- `calendar_id` optional
- `query` default `""`
- `limit` default `20`, min `1`, max `100`

Response model: `GoogleCalendarEventListResponse`

Fields:

- `account`
- `calendar_id`
- `start_date`
- `end_date`
- `query`
- `time_zone`
- `items`
- `count`

Each `items[]` entry is a `GoogleCalendarEventRecord` with:

- `id`
- `calendar_id`
- `status`
- `summary`
- `description`
- `location`
- `html_link`
- `conference_link`
- `organizer_email`
- `creator_email`
- `attendees`
- `start_at`
- `end_at`
- `start_date`
- `end_date`
- `is_all_day`
- `created_at`
- `updated_at`

Behavior notes:

- When Google Calendar is not configured, disconnected, or sync-disabled, this route returns an empty list plus the current account state instead of failing.
- When Google Calendar is connected, the backend performs the provider query directly and returns the current agenda window without creating a local calendar-event cache.

### `POST /v1/calendar/events`

Request model: `GoogleCalendarEventCreateRequest`

Behavior notes:

- Creates a Google Calendar event through the provider API.
- Supports timed events with `start_at` plus `end_at`, or all-day events with `start_date` plus `end_date`.
- Returns `400` for invalid event ranges and `503` when the provider request fails.

### `PUT /v1/calendar/events/{event_id}`

Request model: `GoogleCalendarEventUpdateRequest`

Behavior notes:

- Partial updates are allowed.
- Returns `400` when no writable fields are provided or the updated event range is invalid.
- Returns `503` when Google Calendar is unavailable, disconnected, or the provider request fails.

### `DELETE /v1/calendar/events/{event_id}`

Query:

- `calendar_id` optional

Response model: `GoogleCalendarDeleteResponse`

Behavior notes:

- Deletes the provider-backed event and returns the deleted event id plus resolved calendar id.
- Returns `503` when Google Calendar is unavailable, disconnected, or the provider request fails.

## Email

### `GET /v1/email/status`

Response model: `GoogleEmailStatusResponse`

Fields include:

- `provider`
- `status`
- `configured`
- `connected`
- `sync_enabled`
- `email_address`
- `auth_url_available`
- `redirect_uri`
- `default_label`
- `query_limit`
- `scopes`
- `last_sync_at`
- `last_error`
- `detail`

Behavior notes:

- `status` may be `not_configured`, `disconnected`, `disabled`, `ready`, or `error`.
- This route is safe to call even when Google OAuth is not configured; it returns capability state instead of raising.

### `GET /v1/email/google/connect`

Response model: `GoogleEmailConnectResponse`

Fields:

- `authorization_url`
- `redirect_uri`
- `state`

Behavior notes:

- Returns `400` when backend Google OAuth credentials are not configured.
- The desktop shell opens `authorization_url` in the browser and the OAuth redirect returns to the local backend callback.

### `GET /v1/email/google/callback`

Query:

- `code`
- `state`
- `error`

Behavior notes:

- Completes the local Google OAuth exchange, stores tokens in SQLite, and returns a simple HTML success or failure page for the browser window.
- Returns `400` on missing or mismatched callback state.
- Returns `503` if the token exchange or profile lookup fails.

### `POST /v1/email/google/disconnect`

Response model: `GoogleEmailStatusResponse`

Behavior:

- Clears stored Google OAuth token state from SQLite and returns the new disconnected status snapshot.

### `GET /v1/email/messages`

Query:

- `query` default `""`
- `label` optional
- `limit` default `20`, min `1`, max `50`

Response model: `EmailMessageListResponse`

Fields:

- `account`
- `query`
- `label`
- `items`
- `count`
- `draft_count`

Each `items[]` entry is an `EmailMessageSummary` with:

- `id`
- `thread_id`
- `subject`
- `from_display`
- `from_address`
- `to`
- `snippet`
- `labels`
- `is_read`
- `starred`
- `has_attachments`
- `received_at`
- `linked_task_ids`

Behavior notes:

- When Gmail is not configured, disconnected, or sync-disabled, this route returns an empty list plus the current account state instead of failing.
- When Gmail is connected, the backend performs the provider query and merges task-link metadata from SQLite into the returned message list.

### `GET /v1/email/messages/{message_id}`

Response model: `EmailMessageDetail`

Additional fields beyond `EmailMessageSummary`:

- `cc`
- `body_text`
- `body_html`

Behavior notes:

- Returns `503` when Gmail is unavailable, disconnected, or the provider request fails.
- Includes any linked planner task ids already created from that email.

### `POST /v1/email/messages/{message_id}/task`

Request model: `EmailToTaskRequest`

Important fields:

- `title`
- `priority`
- `scheduled_date`
- `due_at`
- `tags`

Behavior notes:

- Builds a normal backend task with `category: "email"` and appends `email` plus `gmail` tags.
- Persists an `email_task_links` record in SQLite so later inbox reads can show linked task ids.
- Publishes `task_updated` with `change: "created_from_email"` on success.

### `GET /v1/email/drafts`

Query:

- `limit` default `50`, min `1`, max `100`

Response model: `EmailDraftListResponse`

Each `items[]` entry is an `EmailDraftRecord` with:

- `id`
- `provider`
- `thread_id`
- `linked_message_id`
- `to`
- `cc`
- `bcc`
- `subject`
- `body_text`
- `status`
- `gmail_message_id`
- `created_at`
- `updated_at`
- `sent_at`

Behavior notes:

- Drafts are persisted locally in SQLite and ordered with active drafts first, then most-recent updates.

### `POST /v1/email/drafts`

Request model: `EmailDraftCreateRequest`

Behavior:

- Creates a local Gmail draft record in SQLite.

### `PUT /v1/email/drafts/{draft_id}`

Request model: `EmailDraftUpdateRequest`

Behavior:

- Partial updates are allowed.
- Returns `404` if the draft does not exist.

### `POST /v1/email/drafts/{draft_id}/send`

Response model: `EmailDraftRecord`

Behavior:

- Requires at least one recipient.
- Returns `404` if the draft does not exist.
- Returns `503` when Gmail is unavailable, disconnected, or the provider send fails.
- On success, marks the local draft as `sent`, stores the provider message id, and preserves the draft history locally.

## Browser Automation

### `GET /v1/browser-automation/templates`

Response model: `BrowserAutomationTemplateListResponse`

Fields:

- `items`
- `count`

Each `items[]` entry is a `BrowserAutomationTemplateRecord` with:

- `template_id`
- `title`
- `description`
- `step_count`
- `fields`

Behavior notes:

- Current implementation exposes bounded templates only; the backend does not accept free-form opaque browser action graphs.
- Current templates are `open_page_review` and `search_query_review`.

### `GET /v1/browser-automation/runs`

Query:

- `limit` default `20`, min `1`, max `100`

Response model: `BrowserAutomationRunListResponse`

Each `items[]` entry is a `BrowserAutomationRunSummary` with:

- `id`
- `template_id`
- `title`
- `goal`
- `status`
- `current_step_index`
- `step_count`
- `pending_step_title`
- `last_log_message`
- `created_at`
- `updated_at`
- `completed_at`
- `cancelled_at`

Behavior notes:

- Returns the most recently updated runs first.
- Summary rows are backed by SQLite automation history, not in-memory session state.

### `POST /v1/browser-automation/runs`

Request model: `BrowserAutomationRunCreateRequest`

Fields:

- `template_id`
- `title`
- `goal`
- `inputs`

Behavior notes:

- Returns `400` when the template is unknown or required inputs such as `start_url` or `query` are missing.
- Current implementation stores each run plus its step list and audit log in SQLite before any step executes.
- New runs start in `awaiting_approval` with only the first step marked `pending_approval`; later steps stay queued until the current step completes.

### `GET /v1/browser-automation/runs/{run_id}`

Response model: `BrowserAutomationRunDetail`

Additional fields beyond the summary:

- `start_url`
- `inputs`
- `steps`
- `logs`

Each `steps[]` entry is a `BrowserAutomationStepRecord` with:

- `id`
- `position`
- `action_type`
- `title`
- `description`
- `status`
- `requires_approval`
- `url`
- `approval_note`
- `recovery_notes`
- `result`
- `updated_at`
- `completed_at`

Each `logs[]` entry is a `BrowserAutomationLogRecord` with:

- `id`
- `run_id`
- `step_id`
- `level`
- `code`
- `message`
- `payload`
- `created_at`

### `POST /v1/browser-automation/runs/{run_id}/approve`

Request model: `BrowserAutomationApprovalRequest`

Fields:

- `approval_note`

Behavior notes:

- Approves only the current `pending_approval` step.
- Current step handlers are bounded to `open_url`, `fetch_page`, and `manual_checkpoint`.
- On success, the backend stores both the approval log and the step result payload, then advances the next queued step into `pending_approval`.
- When the last step completes, the run moves to `completed`.
- Returns `400` if there is no pending step or the run is already terminal.

### `POST /v1/browser-automation/runs/{run_id}/reject`

Request model: `BrowserAutomationRejectRequest`

Fields:

- `reason`

Behavior notes:

- Rejects only the current `pending_approval` step.
- Moves the run to `blocked` and stores the operator reason alongside the step record plus audit log.

### `POST /v1/browser-automation/runs/{run_id}/cancel`

Request model: `BrowserAutomationCancelRequest`

Fields:

- `reason`

Behavior notes:

- Cancels the run and marks any queued or pending step as `cancelled`.
- Stores a `run_cancelled` audit log entry in SQLite.

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
- Current desktop voice capture sends WAV-backed chunk payloads so the same backend stream path works with both `faster-whisper` and `whisper.cpp`.
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
- `google_email`
- `google_calendar`

Current `voice` fields returned by code now include:

- `input_mode` currently normalized to `push_to_talk`
- `tts_voice`
- `speak_replies`
- `show_transcript_preview`

### `PUT /v1/settings`

Request model: `SettingsPayload`

Behavior:

- Merges the provided nested payload into current settings.
- Persists each top-level group in SQLite.
- Returns the merged settings snapshot.
- Current implementation normalizes `voice.input_mode` back to `push_to_talk` even if an older or unsupported value is submitted.

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
