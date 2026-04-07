# AI Dev System Unification

Updated: 2026-04-07
Status: Current implementation plus remaining planned work

## Purpose

This document is the overview page for the `ai-dev-system/` non-backend unification plan.

Use it to answer three questions quickly:

1. Which phases have already landed in current implementation?
2. Which source-of-truth roots now live under `ai-dev-system/`?
3. What still remains planned work after the landed unification phases?

## Current State Summary

### Current implementation

- `ai-dev-system/` is now the main non-backend integration root.
- `ai-dev-system/context/` owns subsystem-local AI context.
- `ai-dev-system/clients/unity-client/` owns the Unity client path.
- `ai-dev-system/control-plane/` owns the automation runtime path.
- `ai-dev-system/domain/` owns shared avatar, customization, room, and shared-contract docs.
- `ai-dev-system/workbench/` and `ai-dev-system/asset-pipeline/` own workbench and pipeline governance.
- `ai-dev-system/scripts/` and `ai-dev-system/tests/` own the standardized non-backend entry-point and validation surface.
- Phase 9 removed the temporary root-level shim packages for absorbed control-plane imports and locked the supported bootstrap surface.

### Planned work still not done

- Root `scripts/`, root `tools/`, root `bleder/`, root import folders, and `release/` still contain current material that has not been fully absorbed or cleaned up.
- Further cleanup is limited to later runtime or asset moves that are outside the already-landed absorbed roots.

## Phase Status

| Phase | Status | Main result |
| --- | --- | --- |
| Phase 0 | DONE | Freeze baseline and root-directory mapping |
| Phase 1 | DONE | `ai-dev-system/` target layer scaffolding and local AGENTS rules |
| Phase 2 | DONE | Repo AI context absorbed into `ai-dev-system/context/` |
| Phase 3 | DONE | Unity project absorbed into `ai-dev-system/clients/unity-client/` |
| Phase 4 | DONE | Automation runtime moved under `ai-dev-system/control-plane/` |
| Phase 5 | DONE | Shared avatar, customization, and room contracts moved under `ai-dev-system/domain/` |
| Phase 6 | DONE | Workbench and asset-pipeline inventories, naming, and validation established |
| Phase 7 | DONE | Standardized subsystem scripts and structure-validation surface established |
| Phase 8 | DONE | Root docs and task governance rewritten around the landed `ai-dev-system/` architecture |
| Phase 9 | DONE | Legacy shim cleanup, stale absorbed-root path removal, and architecture-lock validation |

## Phase Documents

- `ai-dev-system-unification-phase0.md`
- `ai-dev-system-unification-phase1.md`
- `ai-dev-system-unification-phase2.md`
- `ai-dev-system-unification-phase3.md`
- `ai-dev-system-unification-phase4.md`
- `ai-dev-system-unification-phase5.md`
- `ai-dev-system-unification-phase6.md`
- `ai-dev-system-unification-phase7.md`
- `ai-dev-system-unification-phase8.md`
- `ai-dev-system-unification-phase9.md`

## Current Source Of Truth

- Backend:
  - `local-backend/`
- Non-backend integration root:
  - `ai-dev-system/`
- Governance:
  - `docs/`
  - `tasks/`

## Phase 9 Scope

Phase 9 is now the landed architecture-lock pass for the already-absorbed roots.

Its landed scope includes:

- removing temporary import shims for absorbed control-plane packages
- removing the stale Unity root fallback from active scripts
- updating active docs to stop relying on the absorbed-root phrasing for current behavior
- adding architecture-lock validation for shim absence and stale-path drift
