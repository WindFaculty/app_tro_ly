# Architecture - Current Assistant Runtime

Updated: 2026-04-06

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
- `Assets/Scripts/App/AppCompositionRoot.cs` creates the runtime camera, UI document, audio playback, subtitle presenter, reminder presenter, placeholder avatar-state runtime pieces, the room-world foundation bootstrap for the center stage, and the first character-to-room bridge for spawn plus attention wiring.
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
- `Assets/Resources/World/Rooms/Room_Base.prefab` now provides the placeholder-safe room-root template used by the center-stage room bootstrap.
- `Assets/Scripts/World/Room/` now holds the placeholder-safe room foundation bootstrap used by the Home center stage.
- `Assets/Scripts/World/Objects/` now holds the placeholder-safe room-object registry, definitions, placements, and primitive object factory used by the room foundation.
- `Assets/Scripts/World/Interaction/` now holds the basic room interaction layer for hover, selection, camera focus, and selected-object snapshots.

### Screen flow and controllers

- `IShellModule` and `AppShellState` manage four-zone shell state such as planner-sheet expansion, chat visibility, and settings drawer visibility.
- `HomeModule` now owns the center-stage Home boundary while `HomeScreenController` stays presentation-only for the room-backed Character Space foundation copy, selected-object overlay, current-activity strip, room action dock, task summary, quick-add input, focus lanes, and stage-status copy.
- `ScheduleScreenController` renders today, week, inbox, and completed planner views into the bottom-sheet schedule canvas with date navigation and direct task actions.
- `SettingsScreenController` binds backend-backed toggles and save or reload actions inside the drawer.
- `SettingsModule` now owns the Unity-side settings snapshot, dirty-state tracking, and settings drawer event boundary before `AssistantApp` calls backend settings APIs.
- `ChatPanelController` binds the text input, send button, mic button, and transcript rendering.
- `AppShellController` renders health and stage status in the shell rail and stage header areas.
- `AssistantApp` now routes planner screen or date or task-action requests through shared event contracts before they reach planner mutations and shell-region updates, and it now consumes feature application services for quick-add wording, chat-turn request planning, and planner task-mutation summaries instead of formatting those flows inline.

### Network integration

- `LocalApiClient` handles REST requests for health, tasks, settings, STT, TTS, and compatibility chat.
- `EventsClient` consumes `WS /v1/events`.
- `AssistantStreamClient` consumes `WS /v1/assistant/stream`.
- `AssistantApp` prefers the streaming path and falls back to compatibility chat when the stream is unavailable.
- `AssistantApp` now feeds compatibility replies and assistant-stream transcript or route or chunk or final events into `ChatModule` APIs so transcript and diagnostics state stay chat-owned before shell refresh or audio playback, while `ChatTurnApplicationService` builds the request plan for streaming versus compatibility transport.

### Client-side state

- `TaskViewModelStore` keeps today, week, inbox, and completed snapshots.
- `ChatViewModelStore` keeps transcript, assistant draft, transcript preview, routing diagnostics, system-status copy, and task-action summaries for both compatibility and streaming chat paths.
- `SettingsViewModelStore` keeps the backend-backed settings snapshot currently used by the client, and it is now owned through `SettingsModule` instead of being mutated directly by `AssistantApp`.
- `HomeQuickAddApplicationService` owns quick-add command wording and quick-add status wording for the Home surface.
- `PlannerTaskCommandApplicationService` owns planner task-mutation summaries for complete-task and inbox-schedule flows.

### Avatar and presentation

- `AvatarStateMachine` drives placeholder state visuals.
- `AudioPlaybackController` manages spoken reply playback while subtitle visibility and avatar-state transitions are now triggered from shared event contracts in `AssistantApp`.
- `LipSyncController` applies amplitude-based lip sync to a fallback face mesh or transform.
- `CharacterRoomBridge` now keeps the room-owned avatar presence aligned with the room spawn point: if no production avatar exists in the active scene, it builds a placeholder avatar proxy inside the room, binds that proxy to `AvatarStateMachine`, and turns the avatar toward the current room attention target.
- `RoomSceneBootstrap` now loads `Assets/Resources/World/Rooms/Room_Base.prefab` when available and falls back to a generated room root when it is not.
- `RoomWorldController` now binds to that room template hierarchy, rebuilds the placeholder shell geometry under `ShellGeometry`, and keeps the spawn point, camera anchor, and basic anchor clusters aligned with `RoomLayoutDefinition`.
- `RoomObjectRegistry` and `RoomObjectFactory` now drive a richer 12-object placeholder population from metadata plus placement config instead of hardcoded per-cluster object creation, including the desk, laptop, chair, sofa, side table, shelf, books, plant, lamp, wall art, cabinet, and storage box clusters used by the current room foundation.
- `RoomInteractionController`, `InteractableObject`, and `SelectedRoomObjectStore` now provide the first room interaction slice: selectable room objects can be hovered, clicked, highlighted, pushed into a selected-object snapshot with state plus suggested-action metadata, and focus-capable objects can redirect the stage camera while the Home overlay shows the current room selection.
- `AssistantApp` now handles the first room action-dock commands on top of that selection flow: go-to or inspect or use intents remain placeholder-safe activity updates, return-to-avatar clears selection plus attention, and hotspot visibility toggles room anchor markers without moving world logic into `HomeScreenController`.
- `RoomObjectRegistryValidator` now provides intake-time contract checks for the current room-object registry, and `Tools/TroLy/Validate Room Object Registry` plus `Tools/TroLy/Validate Room Object Prefab Intake` expose placeholder-safe versus strict-prefab validation paths in the Unity Editor.
- `AvatarOutfitApplicationService` now exposes a placeholder-safe application contract over `AvatarEquipmentManager` and `AvatarPresetManager`, but no shell-facing wardrobe UI is shipped yet.
- `AvatarAssetRegistryDefinition` now defines the registry shape for allowed outfit items, cross-slot rules, and presets so future customization flows do not need to scan folders or pass ad-hoc item lists.
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

- The room-backed Home stage is still a foundation slice only: a template-backed room root, fuller placeholder object population, selected-object UI, a lightweight room action dock, and a placeholder-safe character-room bridge now exist, but prefab-backed object intake, movement-grade character behavior behind the dock, real object-state mutations for use or go-to intents, and production-avatar scene binding are not implemented yet.
- Phase 7 intake hardening is now repo-side only: checklist and validator scaffolding exist, but the current room population still uses primitive placeholder geometry rather than shipped prefab assets.
- The current client still contains placeholder or shell-owned helper content in the schedule-side panel.
- The schedule surface is list-first, not a finished calendar-grid module.
- The Phase 2 layering slice is partial: `AssistantApp` still remains the top-level coordinator and the new application services are consumed from there instead of a separate application-composition root.
- No wardrobe or accessory UI exists in the shell yet; only the outfit application contract is present.
- The default LLM route is not fully local.
- Production-avatar runtime behavior still needs scene-level integration and manual validation.
- Latest backend terminal verification is not fully green because `tests/test_tts_service.py::test_chattts_synthesize_writes_cached_wav` failed on 2026-04-04 during the Phase 0 rerun.
- Unity client verification still requires Editor or built-client runs outside terminal-only inspection.
