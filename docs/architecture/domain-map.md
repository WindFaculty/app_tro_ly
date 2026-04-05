# Domain Map - Phase 1

Updated: 2026-04-05
Status: Current implementation map for boundary work inside the existing repo layout

This map adapts the Phase 1 modularization plan to the runtime that actually exists today in `unity-client/` plus `local-backend/`.

## Unity Domains

### Shell/App

- Owns: boot state, shell focus, planner-sheet visibility, chat visibility, settings-drawer visibility, stage-status rendering
- Main files:
  - `unity-client/Assets/Scripts/App/ShellModule.cs`
  - `unity-client/Assets/Scripts/App/AppShellController.cs`
  - `unity-client/Assets/Scripts/Core/AssistantApp.cs`
- Provides:
  - shell navigation and refresh requests
  - health and stage rendering
  - shell composition over Home, Schedule, Chat, and Settings surfaces
- Must not:
  - own planner task snapshots
  - own chat transcript reduction
  - mutate backend-backed settings fields directly when a settings module boundary exists
  - format feature-specific quick-add or planner-mutation wording inline when a feature application service already owns that use case
- Allowed dependencies:
  - `Core/` shared contracts
  - module interfaces from `Features/`
  - UI refs under `Core/`

### Home/Stage

- Owns: center-stage home rendering, quick-add input capture, orbit-status copy, quick-add status presentation
- Main files:
  - `unity-client/Assets/Scripts/Features/Home/HomeModule.cs`
  - `unity-client/Assets/Scripts/Features/Home/HomeModuleContracts.cs`
  - `unity-client/Assets/Scripts/Features/Home/HomeScreenController.cs`
  - `unity-client/Assets/Scripts/Features/Home/HomeQuickAddApplicationService.cs`
- Provides:
  - a module boundary for the center-stage Home surface
  - raw quick-add intent capture from presentation code
  - application-owned quick-add command and status wording
- Must not:
  - call backend APIs directly
  - format planner or chat transport payloads inside the UI controller
  - mutate planner task snapshots directly
- Allowed dependencies:
  - `Core/` UI refs and shared contracts
  - planner snapshot read models
  - chat snapshot read models for orbit-status copy

### Planner

- Owns: selected planner view, schedule interaction events, planner-facing task snapshot rendering
- Main files:
  - `unity-client/Assets/Scripts/Features/Schedule/PlannerModule.cs`
  - `unity-client/Assets/Scripts/Features/Schedule/ScheduleScreenController.cs`
  - `unity-client/Assets/Scripts/Features/Schedule/PlannerTaskCommandApplicationService.cs`
  - `unity-client/Assets/Scripts/Tasks/PlannerBackendIntegration.cs`
  - `unity-client/Assets/Scripts/Tasks/TaskViewModelStore.cs`
- Provides:
  - today or week or inbox or completed screen switching
  - planner task-action requests
  - typed planner snapshot mapping from backend DTOs
- Must not:
  - call chat or settings controllers directly
  - own shell drawer state
  - write persistence directly outside backend APIs
- Allowed dependencies:
  - `Tasks/`
  - `Core/ModuleContracts.cs`
  - shell-facing enums or event contracts

### Chat/Runtime Feedback

- Owns: transcript state, assistant drafts, diagnostics, transcript preview, mic/send requests, planner-action summaries
- Main files:
  - `unity-client/Assets/Scripts/Features/Chat/ChatModule.cs`
  - `unity-client/Assets/Scripts/Features/Chat/ChatPanelController.cs`
  - `unity-client/Assets/Scripts/Features/Chat/ChatTurnApplicationService.cs`
  - `unity-client/Assets/Scripts/Chat/ChatViewModelStore.cs`
- Provides:
  - chat-owned rendering snapshot
  - compatibility and streaming turn-state reduction
  - transcript preview and system-status surfaces
- Must not:
  - call planner or settings controllers directly
  - own shell-region state
  - own avatar scene objects directly
- Allowed dependencies:
  - `Chat/`
  - `Core/ModuleContracts.cs`
  - shared event contracts

### Settings

- Owns: backend-backed settings snapshot used by the current Unity client, settings-toggle mutations, save or reload requests, settings drawer rendering
- Main files:
  - `unity-client/Assets/Scripts/Features/Settings/SettingsModule.cs`
  - `unity-client/Assets/Scripts/Features/Settings/SettingsModuleContracts.cs`
  - `unity-client/Assets/Scripts/Features/Settings/SettingsScreenController.cs`
  - `unity-client/Assets/Scripts/Core/SettingsViewModelStore.cs`
- Provides:
  - a single module boundary for settings UI and dirty-state tracking
  - settings snapshot access for shell-stage summaries and outbound save calls
  - settings-changed notification back to the runtime coordinator
- Must not:
  - call backend routes directly
  - own shell boot state
  - mutate chat transcript state except through the runtime coordinator
- Allowed dependencies:
  - `Core/` shared settings payload types and UI refs
  - Unity UI Toolkit controls

### Avatar/Presentation

- Owns: placeholder avatar state, lip-sync fallback, optional bridge into the production avatar system
- Main files:
  - `unity-client/Assets/Scripts/Avatar/AvatarStateMachine.cs`
  - `unity-client/Assets/Scripts/Avatar/AvatarOutfitApplicationService.cs`
  - `unity-client/Assets/Scripts/Avatar/LipSyncController.cs`
  - `unity-client/Assets/AvatarSystem/Core/Scripts/AvatarConversationBridge.cs`
- Provides:
  - visible conversation-state presentation
  - optional scene bridge to production avatar objects
  - a placeholder-safe application-facing outfit contract over `AvatarEquipmentManager` and `AvatarPresetManager`
- Must not:
  - own planner or chat stores
  - read settings persistence directly
  - become a second source of truth for backend state
- Allowed dependencies:
  - shared event payloads
  - shell-visible runtime state passed in by the coordinator

### Shared/System

- Owns: shared contracts, event bus, UI refs, health normalization, app-wide DTOs used by multiple modules
- Main files:
  - `unity-client/Assets/Scripts/Core/ModuleContracts.cs`
  - `unity-client/Assets/Scripts/Core/AssistantEventBus.cs`
  - `unity-client/Assets/Scripts/Core/AppModuleEvents.cs`
  - `unity-client/Assets/Scripts/Core/*Refs.cs`
- Provides:
  - shared eventing and read-only contracts
  - UI reference binding helpers
  - normalized cross-module runtime types
- Must not:
  - grow into feature-owned business logic
  - become the place where planner or chat or settings rules accumulate by default

## Backend Domains

### API/Composition

- Owns: HTTP and WebSocket routing, service wiring, process lifecycle
- Main files:
  - `local-backend/app/api/routes.py`
  - `local-backend/app/container.py`
- Must not:
  - duplicate task or planner business rules already owned by services

### Application Services

- Owns: task logic, planner summaries, assistant orchestration, memory, speech adapters, scheduler behavior, settings persistence
- Main files:
  - `local-backend/app/services/`
- Must not:
  - leak UI-specific concerns into service APIs
  - let persistence details spread into route handlers

### Persistence/Core

- Owns: SQLite persistence and backend-wide infrastructure types
- Main files:
  - `local-backend/app/db/repository.py`
  - `local-backend/app/core/`
  - `local-backend/app/models/`
- Must not:
  - become a direct dependency of Unity client code

## Current Boundary Notes

- Current implementation still relies on `unity-client/Assets/Scripts/Core/AssistantApp.cs` as the top-level runtime coordinator.
- Phase 1 boundary work should keep extracting feature ownership out of that coordinator without pretending the coordinator is already gone.
- The first Phase 2 slice now keeps quick-add command wording, chat transport planning, planner task-mutation summaries, and outfit command contracts out of presentation controllers, but the runtime coordinator still invokes those application services.
