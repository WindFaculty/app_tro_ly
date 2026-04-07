# Non-Backend Integration

Updated: 2026-04-07
Status: Current implementation

## Purpose

This document describes the current non-backend architecture after the landed `ai-dev-system/` unification phases.

`local-backend/` remains the backend source of truth and stays outside this integration scope.
Everything else in the non-backend system should now be understood through `ai-dev-system/` first, with `docs/` and `tasks/` remaining outside as governance roots.

## Ownership Matrix

| Layer | Owns | Current source of truth |
| --- | --- | --- |
| Backend | Business logic, storage, API routes, scheduling, speech adapters | `local-backend/` |
| Control Plane | GUI automation, Unity automation, MCP runtime, profiles, verification, healing | `ai-dev-system/control-plane/` |
| Unity Client | Runtime shell, 3D presentation, overlays, audio playback, input wiring | `ai-dev-system/clients/unity-client/` |
| Domain | Shared avatar, customization, room, and shared contracts | `ai-dev-system/domain/` |
| Context | Subsystem-local prompts, summaries, policies, memory notes | `ai-dev-system/context/` |
| Asset Pipeline | Tool catalogs, structure validation, migration-owned pipeline rules | `ai-dev-system/asset-pipeline/` |
| Workbench | Authoring-root inventory, imports, naming guidance, reports | `ai-dev-system/workbench/` |
| Standardized Entry Points | Run, validate, and package wrappers for the non-backend subsystem | `ai-dev-system/scripts/` |
| Test Catalog | Subsystem test ownership map and repo-side drift validation | `ai-dev-system/tests/` |
| Governance | Repo docs, queues, backlog, manual gates, done history | `docs/`, `tasks/` |

## Current Source Of Truth

### Control plane

- `ai-dev-system/control-plane/app/`
- `ai-dev-system/control-plane/agents/`
- `ai-dev-system/control-plane/executor/`
- `ai-dev-system/control-plane/planner/`
- `ai-dev-system/control-plane/memory/`
- `ai-dev-system/control-plane/tools/`
- `ai-dev-system/control-plane/mcp_client.py`

### Unity client

- `ai-dev-system/clients/unity-client/Assets/Resources/UI/`
- `ai-dev-system/clients/unity-client/Assets/Scripts/`
- `ai-dev-system/clients/unity-client/Assets/AvatarSystem/`
- `ai-dev-system/clients/unity-client/Assets/Tests/`

### Domain

- `ai-dev-system/domain/avatar/`
- `ai-dev-system/domain/customization/`
- `ai-dev-system/domain/room/`
- `ai-dev-system/domain/shared/`

### Workbench and asset pipeline

- `ai-dev-system/workbench/`
- `ai-dev-system/asset-pipeline/`

### Standardized subsystem entry points

- `ai-dev-system/scripts/run/`
- `ai-dev-system/scripts/validate/`
- `ai-dev-system/scripts/package/`
- `ai-dev-system/tests/structure/`

## Current Implementation Versus Planned Work

### Current implementation

- `ai-dev-system/` is already the main non-backend integration root.
- The Unity project no longer lives at a separate repo root.
- The automation runtime no longer treats `ai-dev-system/app/` as the real home; the current runtime lives under `ai-dev-system/control-plane/`.
- Shared avatar, customization, and room contracts now have a subsystem-local home under `ai-dev-system/domain/`.
- Scripts and test ownership now have a subsystem-local surface under `ai-dev-system/scripts/` and `ai-dev-system/tests/`.

### Planned work

- Root operational internals and authoring files have not been fully migrated under `ai-dev-system/` yet.
- Runtime code and production assets inside the Unity project have not been decomposed into deeper domain-owned runtime packages.
- Manual smoke remains required for Unity Editor, packaged client, and production avatar behavior.

## Working Rules

- Treat `ai-dev-system/` as the first stop for non-backend ownership questions.
- Treat `docs/` and `tasks/` as governance roots that should remain outside runtime code.
- Treat `local-backend/` as intentionally outside this unification scope.
- When a doc mentions old root paths such as `unity-client/`, it should be explicit that the reference is historical or planned-only.
