# Architecture - Local Desktop Assistant

Updated: 2026-03-13

## High-level

```text
Unity desktop app
  -> REST + WebSocket client
     -> Python local backend
        -> Task service
        -> Conversation service
        -> Planner service
        -> Scheduler service
        -> Speech service
        -> SQLite
        -> Groq or Ollama
        -> whisper.cpp
        -> Piper
```

## Runtime Components

### Unity client
- `AvatarRenderer`
  - model load
  - camera and lighting
  - background and presentation shell
- `AvatarAnimator`
  - idle
  - listening
  - thinking
  - talking
  - greeting
  - confirming
  - warning
- `LipSyncController`
  - MVP: map audio amplitude to `mouth_open`
- `TaskUI`
  - today list
  - week list or grouped timeline
  - inbox
  - completed
  - quick add
- `ChatUI`
  - message thread
  - subtitle
  - typing and thinking states
  - mic button
- `LocalApiClient`
  - REST for reads and mutations
  - WebSocket for reminder and state events
- `NotificationPresenter`
  - reminder popups
  - overdue alerts
  - degraded-runtime warnings

### Python local backend
- `TaskService`
  - task CRUD
  - status changes
  - reschedule logic
  - repeat expansion
  - today/week/overdue aggregation
- `ConversationService`
  - history management
  - intent parsing
  - prompt assembly
  - action routing
- `PlannerService`
  - daily summary
  - weekly summary
  - urgency and conflict suggestions
- `SchedulerService`
  - reminder polling
  - event emission
  - due-soon calculations
- `SpeechService`
  - STT
  - TTS
  - temp and cached audio files
- `PersistenceLayer`
  - SQLite repositories
  - settings JSON if needed
  - local logs

### LLM and speech runtimes
- `Groq API`
  - cloud reply refinement option
  - same backend action pipeline and task validation rules
- `Ollama`
  - reasoning and phrasing only
  - backend keeps history and task context
- `whisper.cpp`
  - local speech-to-text
  - MVP flow uses push-to-talk only
- `Piper`
  - local text-to-speech
  - cache common short phrases where useful

## Data Model
- `tasks`
  - source of truth for user-visible work items
- `task_occurrences`
  - optional expanded rows for repeating items shown in daily and weekly views
- `conversations`
  - chat sessions
- `messages`
  - user and assistant messages
- `reminders`
  - reminder schedule plus delivery state
- `app_settings`
  - voice, model, window mode, avatar config, startup preferences
- `session_state`
  - currently focused date, selected task, current mode, voice state

## Integration Boundaries
- Unity renders and presents state; it does not own task planning rules.
- The backend owns session state, prompt context, validation, and persistence.
- Local runtimes stay behind adapters so failures can be reported cleanly.
- `agent-platform` is optional and must not become a hidden dependency of the MVP runtime.

## Startup Flow
1. Start the configured LLM and local speech runtimes if needed.
2. Start the Python backend.
3. Launch the Unity client.
4. Unity calls `GET /v1/health`.
5. Backend returns overall readiness plus DB, LLM, STT, and TTS diagnostics.
6. Unity shows `ready`, `partial`, or `error` status and adjusts available features.

## Target Repository Layout

```text
virtual-assistant/
  unity-client/
    Assets/
    Packages/
    ProjectSettings/
  local-backend/
    app/
    data/
    tests/
    run_local.py
  runtime/
    ollama/
    whisper/
    piper/
  docs/
  scripts/
```
