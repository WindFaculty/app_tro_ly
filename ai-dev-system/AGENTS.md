# AGENTS.md

Applies to `ai-dev-system/`.

Machine-facing rules for work inside the non-backend subsystem.

## Role

- `ai-dev-system/` is the planned integration root for the repo's non-backend system.
- In the current implementation, it already contains the live automation subsystem for GUI automation and Unity automation.
- `context/`, `control-plane/`, `domain/`, `workbench/`, and `asset-pipeline/` now have implemented ownership inside this subsystem; the live Unity runtime has moved to `../apps/unity-runtime/`, so keep the distinction between current code and historical client-absorption context explicit.

## Current Truth Sources

- Hybrid GUI and Unity automation runtime:
  - `control-plane/app/main.py`
  - `control-plane/app/automation/`
  - `control-plane/app/agent/`
  - `control-plane/app/profiles/`
  - `control-plane/app/unity/`
  - `control-plane/unity_integration/`
  - `control-plane/app/logging/`
  - `control-plane/app/vision/`
- Existing workflow scaffold and runtime helpers now unified under the control plane:
  - `control-plane/catalog/`
  - `control-plane/orchestrator/`
  - `control-plane/adapters/`
  - `control-plane/agents/`
  - `control-plane/executor/`
  - `control-plane/planner/`
  - `control-plane/memory/`
  - `control-plane/tools/`
  - `control-plane/mcp_client.py`
  - `verify_unity_integration.py`
  - `workflows/`
  - `tests/`
  - `tasks/`
- Subsystem-local context:
  - `context/summaries/`
  - `context/policies/`
  - `context/prompts/`
  - `context/memory/`
- Subsystem-local domain contracts:
  - `domain/avatar/`
  - `domain/customization/`
  - `domain/room/`
  - `domain/shared/`
- Subsystem-local workbench and pipeline governance:
  - `workbench/`
  - `asset-pipeline/`
  - `asset-pipeline/profiles/`
  - `asset-pipeline/schemas/`
  - `asset-pipeline/manifests/`
  - `asset-pipeline/toolchain-map.json`
  - `control-plane/tools/mesh_ai_pipeline.py`
  - `control-plane/tools/mesh_ai_blender_wrappers.py`
- Standardized subsystem entry points and drift validation:
  - `scripts/run/`
  - `scripts/sync-agent-surfaces.py`
  - `scripts/validate/`
  - `scripts/validate/validate-architecture-lock.ps1`
  - `scripts/validate/validate-docs-tasks.ps1`
  - `scripts/validate/validate_agent_platform_surfaces.py`
  - `scripts/package/`
  - `tests/structure/`

## Target Ownership

- `control-plane/`: current home for shared automation runtime and orchestration ownership
- `clients/`: historical absorbed-client context inside `ai-dev-system/`; the live Unity runtime is now `../apps/unity-runtime/`
- `domain/`: current home for shared avatar, customization, room, and cross-domain contract ownership
- `context/`: current home for absorbed AI context and subsystem-local policies
- `asset-pipeline/`: current home for tool catalogs, structure validation, and migration-owned pipeline guidance
- `asset-pipeline/`: current home for Mesh AI lifecycle contracts, toolchain mapping, and normalized handoff manifests
- `workbench/`: current home for authoring-root inventories, naming guidance, and migration-owned workbench notes
- `workbench/`: current home for Mesh AI intake guidance and logical lifecycle ownership notes
- `scripts/`: current standardized home for non-backend run, validate, package, and migrate entry points

## Working Rules

1. Trust current code over target architecture docs when they differ.
2. Keep every edit explicit about whether it changes current implementation or only planned structure.
3. Do not move code into a target directory unless the active migration phase calls for that move.
4. When a directory becomes the new source of truth, update this file and `ai-dev-system/README.md` in the same change.
5. Treat `logs/` and `dist/` as artifacts, not source-of-truth code roots.
6. Treat `bootstrap_control_plane.py`, `sitecustomize.py`, and the standardized script wrappers as the supported path-bootstrap surface for control-plane imports.
