# Architecture - Current Assistant Runtime

Updated: 2026-04-09

This document describes the current implementation in the repo. It does not treat target-state design notes as already shipped.

## High-Level Topology

```text
Unity runtime
  -> Unity bridge and runtime services
     -> FastAPI backend
        -> AssistantOrchestrator
        -> RouterService / PlanningService / FastResponseService
        -> MemoryService / TaskService / PlannerService
        -> SpeechService / SchedulerService
        -> SQLiteRepository
```

## Unity Runtime

### Entry and composition

- `Assets/Scripts/App/AssistantBootstrap.cs` ensures the runtime boots through `StandaloneRoomApp`.
- `Assets/Scripts/App/StandaloneRoomCompositionRoot.cs` creates the room stage, camera anchors, avatar-facing runtime services, bridge wiring, and audio playback support.
- `Assets/Scripts/Runtime/RoomRuntime.cs` owns room focus presets and runtime scene state.
- `Assets/Scripts/Runtime/UnityBridgeClient.cs` plus `TauriBridgeRuntime.cs` own typed runtime bridge behavior.

### Network integration

- The backend remains the business-logic source of truth under `local-backend/`.
- Unity-side bridge models live under `Assets/Scripts/Runtime/RuntimeModels.cs`.
- The control-plane Unity integration layer under `ai-dev-system/control-plane/unity_integration/` is external automation infrastructure, not runtime business logic.

### Avatar and presentation

- `AvatarStateMachine` drives placeholder state visuals.
- `AudioPlaybackController` manages spoken reply playback.
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
- `agent-platform/` is optional when present and must not be treated as a hidden runtime dependency.

## Known Gaps

- The current client still contains placeholder or shell-owned helper content in the Home avatar stage and the schedule-side panel.
- The schedule surface is list-first, not a finished calendar-grid module.
- The default LLM route is not fully local.
- Production-avatar runtime behavior still needs scene-level integration and manual validation.
- Latest backend terminal verification is not fully green because `tests/test_tts_service.py::test_chattts_synthesize_writes_cached_wav` failed on 2026-04-04 during the Phase 0 rerun.
- Unity client verification still requires Editor or built-client runs outside terminal-only inspection.
