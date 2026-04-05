# Project Roadmap

This file is the fastest repo entry point for current implementation truth.
Use code as the final source of truth, and treat design-target or manual-only areas as non-shipped unless stated otherwise.

## 1. Repo At A Glance

- `local-backend/`: required FastAPI backend, assistant orchestration, task or planner logic, speech adapters, scheduler, SQLite persistence
- `unity-client/`: required Unity client, UI Toolkit shell, screen flow, chat panel, overlays, audio playback, placeholder avatar-state wiring
- `scripts/`: Windows setup, startup, packaging, and backend smoke helpers
- `docs/`: current implementation docs, runbook, test plan, migration baseline, avatar specs
- `tasks/`: active AI queue, manual gates, phased backlog, and historical done log
- `agent-platform/`: optional adjacent subsystem; present in repo but not required for the assistant runtime
- `ai-dev-system/`: adjacent automation tooling for GUI and Unity control-plane work; not part of the required assistant runtime
- `ai/`: prompt/context support files for AI-assisted repo work
- `tools/`: avatar or asset pipeline helper scripts, validation reports, and renders
- `bleder/`: Blender work files for avatar or clothing authoring
- `Clothy3D_Studio/`: external asset-workbench/tool folder; not part of the required runtime
- `Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/`: imported asset source folder; not part of the required runtime
- `release/`: packaged output snapshot for release validation; not the source of truth for runtime code

## 2. Main Runtime Flow

`User -> Unity client -> /v1 API -> backend services -> SQLite -> response/events/audio`

Current implementation flow:

1. Unity boots through `unity-client/Assets/Resources/UI/MainUI.uxml` and `unity-client/Assets/Scripts/App/AssistantBootstrap.cs`.
2. `unity-client/Assets/Scripts/Core/AssistantApp.cs` coordinates shell state, API calls, event streams, voice flow, overlays, and placeholder avatar-state presentation.
3. Unity calls `local-backend/app/api/routes.py` over REST and WebSocket.
4. Backend routes fan into services wired by `local-backend/app/container.py`, including task, planner, routing, memory, speech, and scheduler services.
5. SQLite persistence in `local-backend/data/app.db` stores tasks, reminders, settings, conversations, summaries, memory, and route logs.
6. Backend returns task snapshots, chat replies, stream events, reminder events, and cached audio URLs back to the client.

## 3. Product Modules

### Shell

- Purpose: own left-rail shell state, health-state rendering, boot state, center-stage status, planner-sheet visibility, chat visibility, and settings drawer state
- Main files:
  - `unity-client/Assets/Scripts/App/ShellModule.cs`
  - `unity-client/Assets/Scripts/App/ShellModuleContracts.cs`
  - `unity-client/Assets/Scripts/App/AppShellController.cs`
  - `unity-client/Assets/Resources/UI/Shell/AppShell.uxml`
- Status: `implemented`

### Planner

- Purpose: render Today or Week or Inbox or Completed task views and support planner-facing task actions against backend data
- Main files:
  - `unity-client/Assets/Scripts/Features/Schedule/PlannerModule.cs`
  - `unity-client/Assets/Scripts/Features/Schedule/ScheduleScreenController.cs`
  - `local-backend/app/services/tasks.py`
  - `local-backend/app/services/planner.py`
- Status: `implemented`
  Current notes: modularization work is still in progress, and the UI is list-first rather than a finished calendar grid

### Chat

- Purpose: own transcript state, text turns, mic flow, compatibility fallback, route diagnostics, and assistant stream handling
- Main files:
  - `unity-client/Assets/Scripts/Features/Chat/ChatModule.cs`
  - `unity-client/Assets/Scripts/Features/Chat/ChatPanelController.cs`
  - `unity-client/Assets/Scripts/Chat/ChatViewModelStore.cs`
  - `unity-client/Assets/Scripts/Core/AppModuleEvents.cs`
  - `local-backend/app/services/assistant_orchestrator.py`
  - `local-backend/app/api/routes.py`
- Status: `implemented`
  Current notes: `ChatModule` plus `ChatViewModelStore` own turn-state reduction for compatibility replies, assistant-stream transcript or chunk or final events, and planner-action summaries, while `AssistantApp` coordinates transport and now forwards planner handoff plus subtitle plus avatar-state cues through `AssistantEventBus` contracts before they reach shell or presentation components.

### Avatar

- Purpose: provide shell-visible avatar state, lip-sync fallback, and an optional bridge into the production avatar system when a scene contains an `AvatarConversationBridge`
- Main files:
  - `unity-client/Assets/Scripts/Avatar/AvatarStateMachine.cs`
  - `unity-client/Assets/Scripts/Avatar/LipSyncController.cs`
  - `unity-client/Assets/Scripts/Avatar/AvatarOutfitApplicationService.cs`
  - `unity-client/Assets/AvatarSystem/Core/Scripts/AvatarConversationBridge.cs`
  - `unity-client/Assets/AvatarSystem/Core/Scripts/Data/AvatarAssetRegistryDefinition.cs`
  - `unity-client/Assets/AvatarSystem/AvatarProduction/`
- Status: `partial`
  Current notes: shell presentation is still placeholder-driven, the production avatar path still depends on scene setup plus manual validation, and placeholder-safe outfit metadata now has a registry catalog type even though no shell-facing wardrobe UI is shipped yet

### Settings

- Purpose: load, edit, and persist user-facing runtime settings through backend-owned storage
- Main files:
  - `unity-client/Assets/Scripts/Features/Settings/SettingsModule.cs`
  - `unity-client/Assets/Scripts/Features/Settings/SettingsScreenController.cs`
  - `unity-client/Assets/Scripts/Core/SettingsViewModelStore.cs`
  - `local-backend/app/services/settings.py`
  - `local-backend/app/api/routes.py`
- Status: `implemented`
  Current notes: backend stores more settings groups than the current client exposes for editing, and the Unity client now owns settings UI mutations through `ISettingsModule` before calling backend save or reload flows

## 4. Directory Reading Guide

Recommended read order for understanding the required runtime:

1. `README.md`
2. `docs/roadmap.md`
3. `local-backend/app/api/routes.py`
4. `local-backend/app/container.py`
5. `local-backend/app/services/`
6. `unity-client/Assets/Scripts/Core/AssistantApp.cs`
7. `unity-client/Assets/Scripts/App/`
8. `unity-client/Assets/Scripts/Features/`
9. `unity-client/Assets/Resources/UI/`
10. `docs/02-architecture.md`, `docs/03-api.md`, `docs/04-ui.md`, `docs/09-runbook.md`
11. `tasks/task-queue.md` and `tasks/task-people.md` for current work and gates

## 5. Important Entry Points

- Backend entry:
  - `local-backend/run_local.py`
  - `local-backend/app/main.py`
- Client bootstrap:
  - `unity-client/Assets/Scripts/App/AssistantBootstrap.cs`
  - `unity-client/Assets/Scripts/Core/AssistantApp.cs`
- UI entry:
  - `unity-client/Assets/Resources/UI/MainUI.uxml`
  - `unity-client/Assets/Resources/UI/Shell/AppShell.uxml`
- API routes:
  - `local-backend/app/api/routes.py`

## 6. Stable vs Partial vs Placeholder

### Stable enough to treat as current implementation

- FastAPI `/v1` backend with health, task, chat, speech, settings, events, and assistant stream routes
- SQLite-backed task, reminder, settings, conversation, memory, and route-log persistence
- Unity UI Toolkit shell with Home, Schedule, Settings, Chat, subtitle overlay, and reminder overlay
- REST plus WebSocket integration between client and backend

### Partial

- Avatar production path under `unity-client/Assets/AvatarSystem/`
- Shell-to-avatar scene integration through optional `AvatarConversationBridge`
- Settings editing coverage in the Unity client compared with backend-supported groups
- Planner modularization tracked in `tasks/module-migration-backlog.md`

### Placeholder

- Home avatar stage presentation in the shell
- Right-side schedule helper panel in `AppShell.uxml`

### Not implemented

- Finished calendar-grid schedule UI
- Compact mini-assistant mode

### Current verification gap

- Manual validation required for Unity Editor visuals, packaged-client behavior, production avatar behavior, and target-machine speech quality
- Current backend automated evidence is not fully green: `python -m pytest -q` in `local-backend/` on 2026-04-04 reported `69 passed, 1 failed`, with `tests/test_tts_service.py::test_chattts_synthesize_writes_cached_wav` failing

## 7. Where To Change What

- UI layout or navigation: `unity-client/Assets/Resources/UI/`, `unity-client/Assets/Scripts/App/`, `unity-client/Assets/Scripts/Core/`
- Planner or task views: `unity-client/Assets/Scripts/Features/Schedule/` and `local-backend/app/services/tasks.py` plus `local-backend/app/services/planner.py`
- Chat UX or stream wiring: `unity-client/Assets/Scripts/Features/Chat/`, `unity-client/Assets/Scripts/Core/AssistantApp.cs`, and `local-backend/app/services/assistant_orchestrator.py`
- Settings behavior: `unity-client/Assets/Scripts/Features/Settings/` and `local-backend/app/services/settings.py`
- Backend contracts or service behavior: `local-backend/app/api/routes.py`, `local-backend/app/services/`, `local-backend/app/models/`, `local-backend/app/core/`
- Avatar runtime or production asset path: `unity-client/Assets/Scripts/Avatar/` and `unity-client/Assets/AvatarSystem/`
- Windows setup or validation flow: `scripts/setup_windows.ps1`, `scripts/run_all.ps1`, `scripts/package_release.ps1`, `scripts/smoke_backend.py`

## 8. Scripts And Validation

- Setup:
  - `scripts/setup_windows.ps1`
- Startup:
  - `scripts/run_all.ps1`
- Packaging:
  - `scripts/package_release.ps1`
- Backend smoke:
  - `scripts/smoke_backend.py`

Current automated evidence:

- Backend tests exist under `local-backend/tests/`
- Unity EditMode and PlayMode tests exist under `unity-client/Assets/Tests/`

Manual validation required:

- Unity visual behavior
- Packaged-client smoke and readability
- Production avatar scene behavior
- Target-machine STT or TTS quality and runtime setup

## 9. Docs And Task System

- Code truth:
  - backend behavior: `local-backend/app/api/routes.py`, `local-backend/app/services/`, `local-backend/app/models/`, `local-backend/app/core/`
  - Unity behavior: `unity-client/Assets/Resources/UI/MainUI.uxml`, `unity-client/Assets/Resources/UI/Shell/AppShell.uxml`, `unity-client/Assets/Resources/UI/Styles/`, `unity-client/Assets/Scripts/`
- Documentation hierarchy:
  - repo entry: `README.md`
  - architecture docs: `docs/architecture/`
  - feature docs: `docs/features/`
  - operations and governance docs: `docs/operations/`
- Design target:
  - `unity-client/Assets/Resources/UI/ui_feature_map.md`
- Current implementation docs:
  - `docs/02-architecture.md`
  - `docs/architecture/domain-map.md`
  - `docs/architecture/dependency-rules.md`
  - `docs/architecture/phase1-audit.md`
  - `docs/architecture/phase2-layering.md`
  - `docs/03-api.md`
  - `docs/04-ui.md`
  - `docs/features/avatar-asset-spec.md`
  - `docs/features/avatar-asset-intake-checklist.md`
  - `docs/05-test-plan.md`
  - `docs/09-runbook.md`
- Docs governance:
  - `docs/operations/agent-workflow.md`
  - `docs/operations/documentation-governance.md`
  - `docs/operations/doc-audit-checklist.md`
  - `docs/architecture/adr/`
- Migration baseline:
  - `docs/migration/phase0.md`
- Phase 0 task template and completion checklist:
  - `tasks/task-template.md`
- Active AI queue:
  - `tasks/task-queue.md`
- Manual or off-repo gates:
  - `tasks/task-people.md`
- Phased plan, not shipped structure:
  - `tasks/module-migration-backlog.md`
- Historical log:
  - `tasks/done.md`

## 10. Recommended Next Reading By Goal

- Backend:
  - `local-backend/app/api/routes.py`
  - `local-backend/app/container.py`
  - `local-backend/app/services/`
- UI or shell:
  - `unity-client/Assets/Scripts/Core/AssistantApp.cs`
  - `unity-client/Assets/Scripts/App/`
  - `unity-client/Assets/Resources/UI/`
- Chat:
  - `unity-client/Assets/Scripts/Features/Chat/`
  - `local-backend/app/services/assistant_orchestrator.py`
  - `docs/07-ai-runtime.md`
- Planner:
  - `unity-client/Assets/Scripts/Features/Schedule/`
  - `local-backend/app/services/tasks.py`
  - `local-backend/app/services/planner.py`
- Avatar:
  - `unity-client/Assets/Scripts/Avatar/`
  - `unity-client/Assets/AvatarSystem/Core/Scripts/`
  - `unity-client/Assets/AvatarSystem/Core/Scripts/Data/`
  - `docs/features/avatar-asset-spec.md`
  - `docs/avatar-spec.md`
  - `docs/avatar-rig-cleanup.md`

## 11. Current Architectural Direction

- Keep the required assistant runtime in `local-backend/` plus `unity-client/`.
- Continue modularization inside the current Unity tree before considering any root-level layout move.
- Keep backend ownership of task logic, planner summaries, reminder logic, settings persistence, memory, routing, and speech adapters.
- Keep Unity ownership of presentation, shell flow, overlays, audio playback, and interaction wiring.
- Preserve design-target docs as design targets, not implementation truth.
- Treat avatar work as placeholder-safe and scene-dependent until manual production handoff and validation are complete.

## Core Navigation

- [Documentation Index](index.md)
- [Architecture](02-architecture.md)
- [API](03-api.md)
- [UI](04-ui.md)
- [Runbook](09-runbook.md)
