# Project Roadmap

This file is the fastest repo entry point for current implementation truth.
Use code as the final source of truth, and treat planned work or manual-only validation as non-shipped unless stated otherwise.

## 1. Current Architecture

### Current implementation

- `local-backend/` remains the backend source of truth for business logic, storage, API routes, scheduler, speech adapters, and assistant orchestration.
- `ai-dev-system/` is now the main non-backend integration root for the repo.
- `ai-dev-system/clients/unity-client/` is the current Unity client source of truth.
- `ai-dev-system/control-plane/` is the current automation runtime source of truth.
- `ai-dev-system/context/` is the current subsystem context source of truth.
- `ai-dev-system/domain/` is the current shared avatar, customization, room, and shared-contract source of truth.
- `ai-dev-system/workbench/` and `ai-dev-system/asset-pipeline/` now own workbench inventory, naming guidance, and pipeline validation governance.
- `ai-dev-system/scripts/` and `ai-dev-system/tests/` now provide the standardized non-backend entry-point surface and the subsystem test-catalog root.
- Phase 9 removed the temporary root-level import shims that had mirrored control-plane packages at the `ai-dev-system/` root and locked the active import/bootstrap surface.

### Governance roots

- `docs/` stays outside `ai-dev-system/` as repo governance and architecture navigation.
- `tasks/` stays outside `ai-dev-system/` as queue, backlog, manual-gate, and done-history governance.

### Planned work still not done

- `local-backend/` remains intentionally outside the `ai-dev-system/` unification scope.
- Root `scripts/`, root `tools/`, root `bleder/`, root import folders, and `release/` still contain current operational internals or authoring material that have not been fully absorbed yet.
- Unity Editor and packaged-client smoke remain manual validation gates.

## 2. Non-Backend Integration Map

| Area | Current source of truth | Role |
| --- | --- | --- |
| Control plane | `ai-dev-system/control-plane/` | GUI automation, Unity automation, MCP client, profiles, verification, healing |
| Unity client | `ai-dev-system/clients/unity-client/` | UI Toolkit shell, 3D runtime, overlays, transport wiring, avatar presentation |
| Domain | `ai-dev-system/domain/` | Shared avatar, customization, room, and cross-domain contracts |
| Context | `ai-dev-system/context/` | Subsystem-local prompts, summaries, policies, and memory notes |
| Asset pipeline | `ai-dev-system/asset-pipeline/` | Tool catalog, validation entry points, migration-owned pipeline governance |
| Workbench | `ai-dev-system/workbench/` | Authoring-root inventory, imports, naming guidance, reports |
| Entry points | `ai-dev-system/scripts/` | Standardized run, validate, and package surface for non-backend work |
| Test catalog | `ai-dev-system/tests/` | Subsystem test ownership map and repo-side drift validation |

Detailed architecture view:

- `docs/architecture/non-backend-integration.md`

## 3. Runtime Flow

Current implementation runtime flow:

1. `ai-dev-system/clients/unity-client/` boots the current client shell and runtime presentation.
2. `ai-dev-system/clients/unity-client/Assets/Scripts/Core/AssistantApp.cs` coordinates UI state, transport, overlays, and shell-visible avatar cues.
3. The client calls `local-backend/app/api/routes.py` over REST and WebSocket.
4. Backend services under `local-backend/app/services/` execute planner, settings, reminder, memory, routing, and speech logic.
5. SQLite persistence under `local-backend/data/app.db` stores tasks, reminders, settings, conversations, summaries, and memory.
6. `ai-dev-system/control-plane/` remains the non-backend automation subsystem for GUI and Unity automation workflows; it is not required for the baseline end-user runtime loop.

## 4. Source-Of-Truth Guide

Use these roots first:

- Backend behavior:
  - `local-backend/app/api/routes.py`
  - `local-backend/app/services/`
  - `local-backend/app/models/`
  - `local-backend/app/core/`
- Unity client behavior:
  - `ai-dev-system/clients/unity-client/Assets/Resources/UI/`
  - `ai-dev-system/clients/unity-client/Assets/Scripts/`
  - `ai-dev-system/clients/unity-client/Assets/AvatarSystem/`
- Automation runtime:
  - `ai-dev-system/control-plane/app/`
  - `ai-dev-system/control-plane/agents/`
  - `ai-dev-system/control-plane/executor/`
  - `ai-dev-system/control-plane/planner/`
  - `ai-dev-system/control-plane/memory/`
  - `ai-dev-system/control-plane/tools/`
  - `ai-dev-system/control-plane/mcp_client.py`
- Shared domain contracts:
  - `ai-dev-system/domain/avatar/`
  - `ai-dev-system/domain/customization/`
  - `ai-dev-system/domain/room/`
  - `ai-dev-system/domain/shared/`
- Workbench and asset pipeline governance:
  - `ai-dev-system/workbench/`
  - `ai-dev-system/asset-pipeline/`
- Standardized scripts and structure validation:
  - `ai-dev-system/scripts/run/`
  - `ai-dev-system/scripts/validate/`
  - `ai-dev-system/scripts/package/`
  - `ai-dev-system/tests/structure/`

## 5. Where To Change What

- UI layout or shell behavior:
  - `ai-dev-system/clients/unity-client/Assets/Resources/UI/`
  - `ai-dev-system/clients/unity-client/Assets/Scripts/App/`
  - `ai-dev-system/clients/unity-client/Assets/Scripts/Core/`
- Planner or chat or settings client behavior:
  - `ai-dev-system/clients/unity-client/Assets/Scripts/Features/`
  - `ai-dev-system/clients/unity-client/Assets/Scripts/Tasks/`
- Avatar or customization contracts:
  - `ai-dev-system/domain/avatar/`
  - `ai-dev-system/domain/customization/`
- Avatar runtime or production asset wiring:
  - `ai-dev-system/clients/unity-client/Assets/Scripts/Avatar/`
  - `ai-dev-system/clients/unity-client/Assets/AvatarSystem/`
- Automation runtime:
  - `ai-dev-system/control-plane/`
- Asset-pipeline governance:
  - `ai-dev-system/asset-pipeline/`
  - `ai-dev-system/workbench/`
- Backend behavior:
  - `local-backend/app/api/routes.py`
  - `local-backend/app/services/`

## 6. Validation And Entry Points

### Current implementation

- Setup or startup or packaging internals still live in root `scripts/`.
- Standardized non-backend entry points now live in `ai-dev-system/scripts/`.
- Repo-side unification drift validation now lives in `ai-dev-system/scripts/validate/` and `ai-dev-system/tests/structure/`.

### Key commands

- `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/validate/validate-structure.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/validate/validate-avatar-pipeline.ps1`
- `python ai-dev-system/scripts/validate/validate_phase7_structure.py`
- `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/validate/validate-architecture-lock.ps1`

### Manual validation required

- Unity Editor visuals and runtime interaction
- Packaged-client smoke
- Production avatar scene integration
- Target-machine speech quality and runtime installation

## 7. Migration Docs

Use these docs in order for the non-backend unification story:

- `docs/migration/ai-dev-system-unification.md`
- `docs/migration/ai-dev-system-unification-phase0.md`
- `docs/migration/ai-dev-system-unification-phase1.md`
- `docs/migration/ai-dev-system-unification-phase2.md`
- `docs/migration/ai-dev-system-unification-phase3.md`
- `docs/migration/ai-dev-system-unification-phase4.md`
- `docs/migration/ai-dev-system-unification-phase5.md`
- `docs/migration/ai-dev-system-unification-phase6.md`
- `docs/migration/ai-dev-system-unification-phase7.md`
- `docs/migration/ai-dev-system-unification-phase8.md`
- `docs/migration/ai-dev-system-unification-phase9.md`

## 8. Task Tracking

- Active AI queue:
  - `tasks/task-queue.md`
- Manual gates:
  - `tasks/task-people.md`
- Current root-unification phase tracker:
  - `tasks/ai-dev-system-unification-backlog.md`
- Older Unity modularization backlog:
  - `tasks/module-migration-backlog.md`
- Historical completion log:
  - `tasks/done.md`

## 9. Read Order

Recommended read order for the current repo state:

1. `README.md`
2. `docs/index.md`
3. `docs/roadmap.md`
4. `docs/architecture/non-backend-integration.md`
5. `docs/migration/ai-dev-system-unification.md`
6. `local-backend/app/api/routes.py`
7. `ai-dev-system/README.md`
8. `ai-dev-system/control-plane/README.md`
9. `ai-dev-system/domain/README.md`
10. `tasks/task-queue.md`
