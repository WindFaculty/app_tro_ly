# Architecture - Local Desktop Assistant

Updated: 2026-03-14

## High-level

```text
Unity desktop app
  -> REST + WebSocket client
     -> Python local backend
        -> AssistantOrchestrator
           -> ActionValidator -> TaskService -> SQLite
           -> PlannerService
           -> RouterService
              -> FastResponseService -> Groq or Gemini
              -> PlanningService -> Gemini or Groq
           -> MemoryService
           -> SpeechService -> faster-whisper or whisper.cpp / Piper or ChatTTS
        -> SchedulerService -> EventBus -> reminder and state events
```

## Runtime Components

### Unity client

- `AvatarRenderer`
  - placeholder model load today
  - camera, lighting, and presentation shell
- `AvatarAnimator`
  - idle
  - listening
  - thinking
  - talking
  - greeting
  - confirming
  - warning
- `LipSyncController`
  - amplitude-based mouth movement for MVP
- `TaskUI`
  - today
  - week
  - inbox
  - completed
  - quick add and edit surfaces
- `ChatUI`
  - message thread
  - transcript preview
  - subtitle
  - thinking and talking states
  - mic button
- `LocalApiClient`
  - REST for health, settings, speech, and task mutations
  - `WS /v1/events` for reminder and state events
  - `WS /v1/assistant/stream` for live assistant turns
- `NotificationPresenter`
  - reminder popups
  - overdue alerts
  - degraded-runtime recovery guidance

### Python local backend

- `AssistantOrchestrator`
  - single turn pipeline for `/chat` and `/assistant/stream`
  - session creation, state transitions, route logging, and final response assembly
- `ActionValidator`
  - keyword and regex-based intent parsing
  - safe task actions only
  - deterministic summaries for day, week, overdue, urgency, and free-time requests
- `RouterService`
  - chooses `groq_fast`, `gemini_deep`, or `hybrid_plan_then_groq`
  - considers complexity, planning keywords, notes length, voice mode, and provider health
- `FastResponseService`
  - short, voice-friendly final phrasing
- `PlanningService`
  - deep planning prompt
  - structured JSON plan payload
  - fallback plan when provider calls fail
- `MemoryService`
  - recent-message window
  - rolling conversation summary
  - long-term memory extraction and retrieval
- `TaskService`
  - task CRUD
  - status changes
  - repeat expansion
  - reminder generation
  - today, week, overdue, inbox, and completed views
- `PlannerService`
  - daily summary
  - weekly summary
  - overdue summary
  - urgency summary
  - free-slot detection
- `SchedulerService`
  - reminder polling
  - event emission
  - due-soon calculations
- `SpeechService`
  - STT upload and byte-stream transcription
  - TTS synthesis and sentence-level streaming support
- `PersistenceLayer`
  - SQLite repositories
  - settings and session storage
  - logs and route logs

### LLM and speech runtimes

- `Groq API`
  - fast-response path by default
- `Gemini API`
  - deep-planning path by default
- `Ollama`
  - present in config as a future local path
  - currently reported as disabled for this phase
- `faster-whisper`
  - default STT path
- `whisper.cpp`
  - optional STT fallback when configured
- `Piper`
- `ChatTTS`
  - local TTS runtime with content-hash caching

## Data Model

- `tasks`
  - source of truth for user-visible work items
- `task_occurrences`
  - expanded rows for repeating items shown in day and week views
- `reminders`
  - reminder schedule plus delivery state
- `conversations`
  - chat sessions
- `messages`
  - user and assistant messages
- `assistant_sessions`
  - active assistant stream or chat session metadata
- `conversation_summaries`
  - rolling short-term summary for a conversation
- `memory_items`
  - extracted long-term memory records
- `route_logs`
  - route, provider, latency, fallback, and token-usage trace
- `app_settings`
  - voice, model, window mode, avatar, reminder, startup, and memory preferences
- `session_state`
  - focused date and other lightweight backend-owned UI state

## AI Contract Boundaries

- Unity renders and presents state; it does not own task planning rules.
- The backend owns validation, persistence, routing, and session memory.
- LLMs help phrase or plan, but they never write directly to SQLite.
- Local speech runtimes stay behind adapters so failures can be surfaced cleanly.
- `agent-platform` is optional and must not become a hidden dependency of the MVP runtime.

## Startup and Runtime Flow

1. Start the configured speech runtimes if needed.
2. Start the Python backend.
3. Launch the Unity client.
4. Unity calls `GET /v1/health`.
5. Backend returns DB, LLM, STT, and TTS diagnostics plus logs and recovery actions.
6. Unity shows `ready`, `partial`, or `error` status and adjusts available features.
7. Text chat can use `POST /v1/chat`.
8. Live assistant mode can use `WS /v1/assistant/stream`.
9. Background reminder and state events flow through `WS /v1/events`.
