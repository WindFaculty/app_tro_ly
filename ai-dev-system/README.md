# AI Dev System

Current implementation: `ai-dev-system/` is now the main integration root for the repo's non-backend system.

Important boundary:

- `local-backend/` remains outside this subsystem and is out of scope for the current unification plan.
- The shipped assistant runtime is now `local-backend/` plus `clients/unity-client/` inside `ai-dev-system/`.
- Phase 1 establishes the target architecture and ownership map inside `ai-dev-system/` before any large path move.

## Mission

`ai-dev-system/` is the main non-backend system for this repo.

Its long-term role is to own:

- the shared control plane for GUI automation and Unity automation
- absorbed client code such as `clients/unity-client/`
- non-backend domain contracts for avatar, customization, room, and shared events or models
- AI context and subsystem-local policies
- asset-pipeline code and workbench authoring areas
- non-backend scripts, validations, packaging helpers, and subsystem tests

## Phase 1 Architecture

The target layout after this phase is:

```text
ai-dev-system/
|- control-plane/   automation runtime, orchestration, profiles, MCP, verification, healing
|- clients/         absorbed non-backend clients such as `clients/unity-client/`
|- domain/          avatar, customization, room, and shared contracts or models
|- context/         AI context, prompts, summaries, and local subsystem policies
|- asset-pipeline/  import, conversion, validation, and export pipeline code
|- workbench/       authoring and lab area for Blender, Clothy3D, Meshy imports, reports
|- scripts/         non-backend run, validate, package, and migrate entry points
|- tests/           subsystem-level verification entry points
|- workflows/       bounded task specs, demo flows, and automation workflows
|- logs/            run artifacts
`- dist/            packaged outputs when they are intentionally kept in-repo
```

## Current Implementation vs Planned Work

### Current implementation

The current automation subsystem now lives under `control-plane/`, and Phase 9 removed the temporary root-level shim packages that had bridged the old import layout.
The shared avatar, customization, and room contract layer now lives under `domain/`, while runtime Unity code remains in `clients/unity-client/`.
For the architecture-lock cleanup details, read `../docs/migration/ai-dev-system-unification-phase9.md`.

Current source-of-truth roots:

- `control-plane/app/`
- `control-plane/agents/`
- `control-plane/executor/`
- `control-plane/planner/`
- `control-plane/memory/`
- `control-plane/tools/`
- `control-plane/mcp_client.py`
- `clients/unity-client/`
- `domain/`
- `workflows/`
- `tests/`
- `tasks/`
- `context/`

### Planned work

The new Phase 1 layer directories exist to define ownership before larger moves:

- `control-plane/`
- `clients/`
- `domain/`
- `context/`
- `asset-pipeline/`
- `workbench/`
- `scripts/`

Some directories are still architecture placeholders. Their presence does not mean runtime code has already been migrated into them.
Current implemented exceptions are:

- `clients/unity-client/`, which is the live Unity project path after the Phase 3 client move
- `context/`, which owns subsystem-local AI context after the Phase 2 absorption
- `control-plane/`, which owns the automation runtime after the Phase 4 move
- `domain/`, which now owns shared avatar, customization, and room contracts after the Phase 5 domain pass
- `workbench/` and `asset-pipeline/`, which now own inventory, naming, and validation entry points for authoring roots after the Phase 6 pass

## Ownership Map

### Control plane

Owns the shared non-backend automation runtime:

- GUI agent runtime from `control-plane/app/`
- orchestration helpers from `control-plane/agents/`, `control-plane/executor/`, and `control-plane/planner/`
- Unity MCP client and related workflow control from `control-plane/app/unity/` and `control-plane/mcp_client.py`
- runtime lesson storage or prompts that belong to automation execution rather than repo governance

### Clients

Owns absorbed client applications.

Current state:

- `clients/unity-client/` is now the current home of the Unity project.

Planned work:

- root-level script and doc references have been updated to use `clients/unity-client/`

### Domain

Owns non-backend contracts and metadata for:

- avatar
- customization
- room
- shared events, models, and contracts

Current state:

- `domain/` now owns the shared contract docs and taxonomy snapshots for avatar, customization, and room
- runtime implementation still lives in `clients/unity-client/Assets/Scripts/` and `clients/unity-client/Assets/AvatarSystem/`

### Context

Owns subsystem-local AI context:

- prompts
- summaries
- policies
- memory notes and patterns for the subsystem

Current state:

- `context/` now owns the absorbed repo AI context files and the subsystem prompt material
- the former repo AI context root is no longer the source of truth for this layer

### Asset pipeline

Owns conversion and validation code for avatar or clothing or import processing.

Current state:

- `asset-pipeline/` now owns the helper-script catalog and structure validator for this phase
- current executable helper scripts still live in root `tools/`

### Workbench

Owns authoring and lab material:

- Blender work files
- Clothy3D workspace
- Meshy and other raw imports
- reports and review artifacts

Current state:

- `workbench/` now owns the inventory, naming guidance, and ownership notes for these roots
- current workbench data still lives outside `ai-dev-system/` in root folders such as `bleder/` and `Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/`

### Scripts

Owns non-backend entry points for:

- run
- validate
- package
- migrate

Current state:

- `scripts/` now owns the standardized non-backend command surface for run or validate or package entry points inside `ai-dev-system/`
- root `scripts/` remains the current source of truth for the underlying Windows setup or startup or packaging internals

### Tests

Owns subsystem-level validation for the non-backend system.

Current state:

- `tests/` now owns the subsystem test catalog and the repo-side structure validation bucket
- existing subsystem tests still live in `tests/`, `control-plane/app/tests/`, and `clients/unity-client/Assets/Tests/`
- `tests/structure/` now provides repeatable repo-side validation for Phase 7, Phase 8, and Phase 9 drift checks

## Current Source-Of-Truth Inside `ai-dev-system/`

Use these paths first when the request is about the current automation subsystem:

- desktop and hybrid Unity automation runtime:
  - `control-plane/app/main.py`
  - `control-plane/app/automation/`
  - `control-plane/app/agent/`
  - `control-plane/app/profiles/`
  - `control-plane/app/unity/`
  - `control-plane/app/logging/`
  - `control-plane/app/vision/`
- autonomous workflow path still in active use:
  - `control-plane/agents/`
  - `control-plane/executor/`
  - `control-plane/planner/`
  - `control-plane/memory/`
  - `control-plane/tools/`
  - `control-plane/mcp_client.py`
  - `workflows/`
  - `tests/`
  - `tasks/`
- subsystem guidance:
  - `AGENTS.md`
- subsystem-local context:
  - `context/summaries/`
  - `context/policies/`
  - `context/prompts/`
  - `context/memory/`
- subsystem-local domain contracts:
  - `domain/avatar/`
  - `domain/customization/`
  - `domain/room/`
  - `domain/shared/`
- subsystem-local workbench and asset-pipeline governance:
  - `workbench/`
  - `asset-pipeline/`
- standardized subsystem entry points and structure validation:
  - `scripts/run/`
  - `scripts/validate/`
  - `scripts/validate/validate-structure.ps1`
  - `scripts/validate/validate-architecture-lock.ps1`
  - `scripts/validate/validate-docs-tasks.ps1`
  - `scripts/package/`
  - `tests/structure/`

## Phase 1 Deliverables

This phase intentionally does all of the following before any root move:

- establishes the target layer directories
- rewrites this README so `ai-dev-system/` is described as the main non-backend system
- creates a local `ai-dev-system/AGENTS.md`
- keeps the distinction between current implementation and planned structure explicit
- unifies the current automation runtime under `control-plane/`

This phase intentionally does not:

- move root `tools/`
- move root `scripts/`
- change runtime code paths just to match the future tree

## Verification For This Phase

Phase 1 is considered verified when:

- the Phase 1 layer directories exist under `ai-dev-system/`
- this README explains the subsystem as the non-backend integration root
- the local `ai-dev-system/AGENTS.md` exists and points to the current automation truth
- repo docs and task trackers reference the new Phase 1 architecture without claiming the runtime has already moved
