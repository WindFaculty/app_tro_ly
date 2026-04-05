# Architecture - Current Assistant Runtime

Updated: 2026-04-05

This document describes the current implementation in the repo. It does not treat target-state design notes as already shipped.

## High-Level Topology

```text
Unity client
  -> REST and WebSocket clients
     -> FastAPI backend
        -> AssistantOrchestrator
           -> ActionValidator
           -> RouterService
           -> PlanningService
           -> FastResponseService
           -> MemoryService
           -> TaskService / PlannerService
           -> SpeechService
        -> SchedulerService
        -> SQLiteRepository
```

## Unity Client

### Entry and composition

- `Assets/Scripts/Core/AssistantApp.cs` is the runtime coordinator.
- `Assets/Scripts/App/AppCompositionRoot.cs` creates the runtime camera, UI document, audio playback, subtitle presenter, reminder presenter, and placeholder avatar-state runtime pieces.
- `Assets/Scripts/Core/UiDocumentLoader.cs` loads `Assets/Resources/UI/MainUI.uxml`.
- `Assets/Scripts/Core/AssistantEventBus.cs` plus `Assets/Scripts/Core/AppModuleEvents.cs` now carry planner handoff, subtitle visibility, and avatar-state signals across the Unity client runtime.

### UI structure

- `Assets/Resources/UI/MainUI.uxml` only wraps `Shell/AppShell.uxml`.
- `Assets/Resources/UI/Shell/AppShell.uxml` composes the visible shell from:
  - left control rail
  - center-stage presentation area
  - bottom planner sheet
  - right-side chat panel
  - settings drawer
  - subtitle overlay
  - reminder overlay
- Runtime styles live in `Assets/Resources/UI/Styles/*.uss`.
- `Assets/Resources/UI/MainStyle.uss` is deprecated and not the active style source.

### Screen flow and controllers

- `IShellModule` and `AppShellState` manage four-zone shell state such as planner-sheet expansion, chat visibility, and settings drawer visibility.
- `HomeScreenController` renders the orbit-style center stage, task summary, quick-add input, focus lanes, and stage-status copy.
- `ScheduleScreenController` renders today, week, inbox, and completed planner views into the bottom-sheet schedule canvas with date navigation and direct task actions.
- `SettingsScreenController` binds backend-backed toggles and save or reload actions inside the drawer.
- `ChatPanelController` binds the text input, send button, mic button, and transcript rendering.
- `AppShellController` renders health and stage status in the shell rail and stage header areas.
- `AssistantApp` now routes planner screen or date or task-action requests through shared event contracts before they reach planner mutations and shell-region updates.

### Network integration

- `LocalApiClient` handles REST requests for health, tasks, settings, STT, TTS, and compatibility chat.
- `EventsClient` consumes `WS /v1/events`.
- `AssistantStreamClient` consumes `WS /v1/assistant/stream`.
- `AssistantApp` prefers the streaming path and falls back to compatibility chat when the stream is unavailable.
- `AssistantApp` now feeds compatibility replies and assistant-stream transcript or route or chunk or final events into `ChatModule` APIs so transcript and diagnostics state stay chat-owned before shell refresh or audio playback.

### Client-side state

- `TaskViewModelStore` keeps today, week, inbox, and completed snapshots.
- `ChatViewModelStore` keeps transcript, assistant draft, transcript preview, routing diagnostics, system-status copy, and task-action summaries for both compatibility and streaming chat paths.
- `SettingsViewModelStore` keeps the backend-backed settings snapshot currently used by the client.

### Avatar and presentation

- `AvatarStateMachine` drives placeholder state visuals.
- `AudioPlaybackController` manages spoken reply playback while subtitle visibility and avatar-state transitions are now triggered from shared event contracts in `AssistantApp`.
- `LipSyncController` applies amplitude-based lip sync to a fallback face mesh or transform.
- `Assets/AvatarSystem/` contains the production-avatar groundwork:
  - `AvatarConversationBridge`
  - `AvatarAnimatorBridge`
  - `AvatarRootController`
  - `AvatarLipSyncDriver`
  - production-asset and validator scaffolding
- The assistant shell does not instantiate a production avatar by itself. It integrates with `AvatarConversationBridge` only if one exists in the active Unity scene.

## Backend

### Application composition

- `local-backend/app/main.py` creates the FastAPI app and starts scheduler lifecycle management.
- `local-backend/app/container.py` wires:
  - `SQLiteRepository`
  - `EventBus`
  - `SettingsService`
  - `LlmService`
  - `SpeechService`
  - `TaskService`
  - `PlannerService`
  - `ActionValidator`
  - `RouterService`
  - `MemoryService`
  - `PlanningService`
  - `FastResponseService`
  - `AssistantOrchestrator`
  - `ConversationService`
  - `SchedulerService`

### API surface

- Routes live in `local-backend/app/api/routes.py`.
- REST endpoints cover health, tasks, chat, speech, settings, and cached speech files.
- WebSockets cover event streaming and assistant turn streaming.

### Assistant pipeline

- `AssistantOrchestrator` is the shared pipeline behind `POST /v1/chat` and `WS /v1/assistant/stream`.
- `ActionValidator` performs deterministic intent analysis and approved task actions before any write.
- `RouterService` chooses `groq_fast`, `gemini_deep`, or `hybrid_plan_then_groq`.
- `PlanningService` generates deeper structured planning output.
- `FastResponseService` turns validated context into short final phrasing.
- `MemoryService` manages recent-message context, rolling summaries, and conservative long-term memory extraction.

### Task and planning logic

- `TaskService` owns task CRUD, occurrence generation, conflict detection, overdue queries, inbox queries, completed queries, and reminder sync.
- `PlannerService` derives daily, weekly, overdue, urgency, and free-slot summaries from task data.
- `SchedulerService` polls reminders and publishes due events.

### Speech and runtime health

- `SpeechService` delegates to `SttService` and `TtsService`.
- STT providers:
  - `faster_whisper`
  - `whisper_cpp`
- TTS providers:
  - `piper`
  - `chattts`
- Health payloads report `ready`, `partial`, or `error` based on database and runtime availability.
- Recovery actions are generated from actual configured runtime state.

## Persistence

SQLite stores:

- tasks
- reminders
- conversations
- messages
- assistant sessions
- conversation summaries
- memory items
- route logs
- app settings
- session state

## Ownership Boundaries

- The backend is the source of truth for task mutations, summaries, routing, memory, settings persistence, and reminder logic.
- The Unity client is the source of truth for presentation, screen flow, overlays, audio playback, and user interaction wiring.
- `agent-platform/` is optional and must not be treated as a hidden runtime dependency.

## Known Gaps

- The current client still contains placeholder or shell-owned helper content in the Home avatar stage and the schedule-side panel.
- The schedule surface is list-first, not a finished calendar-grid module.
- The default LLM route is not fully local.
- Production-avatar runtime behavior still needs scene-level integration and manual validation.
- Latest backend terminal verification is not fully green because `tests/test_tts_service.py::test_chattts_synthesize_writes_cached_wav` failed on 2026-04-04 during the Phase 0 rerun.
- Unity client verification still requires Editor or built-client runs outside terminal-only inspection.
