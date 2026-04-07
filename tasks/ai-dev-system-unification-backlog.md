# AI Dev System Unification Backlog

Updated: 2026-04-07
Status values: TODO | DOING | DONE | BLOCKED
Ownership: `Uxx` = AI-executable root-unification work | `Pxx` = manual or off-repo work tracked in `tasks/task-people.md`

## How To Use This File

- This file is the phased tracker for the `tao_lai_agent.md` root-unification plan.
- Track only the non-backend unification work that changes repo structure, ownership language, or validation around `ai-dev-system/`.
- Treat `local-backend/` as intentionally outside this backlog's migration scope.
- Keep `tasks/task-queue.md` as the active AI queue and `tasks/done.md` as the historical record.

## Lane Map

- `Control Plane`: automation runtime, MCP client, profile surface, and supported bootstrap surface
- `Unity Client`: absorbed client path, client-side scripts, path-sensitive validation, Unity-root references
- `Avatar + Customization`: shared avatar or customization or room contracts, validator ownership, runtime contract boundaries
- `Asset Pipeline`: workbench inventories, tool catalogs, import roots, naming guidance, pipeline validation
- `Governance + Validation`: docs, task governance, migration summaries, drift validators, path-audit follow-through

## Phase Summary

- `Phase 0`: freeze, audit, and root mapping
- `Phase 1`: `ai-dev-system/` target architecture definition
- `Phase 2`: context absorption
- `Phase 3`: Unity client absorption
- `Phase 4`: control-plane unification
- `Phase 5`: avatar or customization or room domain pass
- `Phase 6`: workbench and asset-pipeline organization
- `Phase 7`: scripts, tests, and packaging surface standardization
- `Phase 8`: docs and task-governance rewrite
- `Phase 9`: legacy shim cleanup and architecture lock

## Backlog

- `U00 | DONE | Phase 0 | Lane: Governance + Validation | Purpose: freeze the non-backend repo state before root-level unification. | Scope: baseline docs, root-directory inventory, and old-to-new mapping. | Evidence: docs/migration/ai-dev-system-unification-phase0.md`
- `U01 | DONE | Phase 1 | Lane: Governance + Validation | Purpose: define `ai-dev-system/` as the non-backend integration root before deeper path moves. | Scope: layer scaffolding, subsystem README, subsystem AGENTS rules. | Evidence: docs/migration/ai-dev-system-unification-phase1.md`
- `U02 | DONE | Phase 2 | Lane: Governance + Validation | Purpose: absorb repo AI context into the subsystem context root. | Scope: `ai-dev-system/context/` plus docs and guidance rewrites. | Evidence: docs/migration/ai-dev-system-unification-phase2.md`
- `U03 | DONE | Phase 3 | Lane: Unity Client | Purpose: absorb the Unity project into `ai-dev-system/clients/unity-client/`. | Scope: project move, path rewrites, and current-state docs or script updates. | Evidence: docs/migration/ai-dev-system-unification-phase3.md`
- `U04 | DONE | Phase 4 | Lane: Control Plane | Purpose: move automation runtime truth under `ai-dev-system/control-plane/`. | Scope: control-plane move, MCP client move, and compatibility shims. | Evidence: docs/migration/ai-dev-system-unification-phase4.md`
- `U05 | DONE | Phase 5 | Lane: Avatar + Customization | Purpose: establish shared avatar, customization, room, and shared contract ownership under `ai-dev-system/domain/`. | Scope: domain docs, slot taxonomy snapshot, validator ownership notes, room contract snapshots. | Evidence: docs/migration/ai-dev-system-unification-phase5.md`
- `U06 | DONE | Phase 6 | Lane: Asset Pipeline | Purpose: establish workbench and asset-pipeline ownership before large authoring-file moves. | Scope: source inventories, naming guidance, tool catalog, and structure validation. | Evidence: docs/migration/ai-dev-system-unification-phase6.md`
- `U07 | DONE | Phase 7 | Lane: Governance + Validation | Purpose: standardize scripts and tests around `ai-dev-system/`. | Scope: subsystem entry-point wrappers, test buckets, and structure validation. | Evidence: docs/migration/ai-dev-system-unification-phase7.md`
- `U08 | DONE | Phase 8 | Lane: Governance + Validation | Purpose: rewrite root docs and task governance around the landed `ai-dev-system/` architecture. | Scope: `docs/roadmap.md`, `docs/architecture/non-backend-integration.md`, `docs/migration/ai-dev-system-unification.md`, this backlog file, and tracker alignment in `tasks/task-queue.md`. | Evidence: docs/migration/ai-dev-system-unification-phase8.md`
- `U09 | DONE | Phase 9 | Lane: Governance + Validation | Purpose: remove half-old or half-new compatibility leftovers and lock the architecture. | Scope: legacy shim cleanup, stale path removal, doc-link audit, task-link audit, release-path drift validation, and supported bootstrap-surface definition. | Depends on: U03 U04 U06 U07 U08. | Evidence: docs/migration/ai-dev-system-unification-phase9.md`

## Current Notes

- Current implementation already uses `ai-dev-system/clients/unity-client/` as the live client root.
- Current implementation already uses `ai-dev-system/control-plane/` as the live automation root.
- Current implementation already uses `ai-dev-system/domain/` plus `ai-dev-system/workbench/` plus `ai-dev-system/asset-pipeline/` for contract and governance ownership.
- Phase 9 is now landed for the absorbed control-plane roots, active bootstrap surface, and stale-path drift checks.
