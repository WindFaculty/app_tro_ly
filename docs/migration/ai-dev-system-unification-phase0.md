# AI Dev System Unification Phase 0 Baseline

Updated: 2026-04-07
Status: Current implementation freeze for the proposed root-level non-backend unification under `ai-dev-system/`

## Purpose

This document is the Phase 0 audit-and-freeze record for the root-level restructure proposed in `tao_lai_agent.md` and `tai_cau_truc.md`.

It captures:

- the current source-of-truth boundaries before any root move
- the root-directory inventory outside `local-backend/`
- the migration action chosen for each non-governance root
- the explicit roots that must stay outside `ai-dev-system/`
- the old-root to new-root mapping that later phases must follow

## Scope And Guardrails

### Current implementation

- `local-backend/` is out of scope for this migration phase and remains a required runtime root.
- The active assistant runtime on 2026-04-07 is still `local-backend/` plus `unity-client/`.
- `ai-dev-system/` is currently an adjacent automation subsystem for GUI-agent and Unity control-plane work. It is not yet the source of truth for the non-backend runtime.

### Inventory exclusions

- `.git/` is excluded from the migration inventory because it is VCS metadata, not repo architecture.
- The four migration action labels in this baseline apply to legacy or adjacent non-backend work roots that must be resolved by the plan.
- Governance or machine-local roots that intentionally stay outside `ai-dev-system/` are recorded separately so their status is still explicit.

## Root Inventory Snapshot

### Target root anchor

- `ai-dev-system/`
  - Current implementation: adjacent automation tooling with `app/`, Unity MCP workflow folders, tests, tasks, tools, and a local README describing it as separate from the shipped assistant runtime.
  - Current source-of-truth status: source of truth only for the automation subsystem already living inside `ai-dev-system/`.
  - Migration role: retain as the destination root and reorganize internally in later phases; this folder is not itself labeled as `absorb`, `wrap`, `archive/workbench`, or `delete after migrate`.

### Roots that must be resolved by the migration

| Root | Current implementation on 2026-04-07 | Current source-of-truth status | Phase 0 action label | Planned destination | Phase 0 decision note |
| --- | --- | --- | --- | --- | --- |
| `unity-client/` | Unity project with `Assets/`, `Packages/`, and `ProjectSettings/` | Current source of truth for UI, shell flow, overlays, audio playback, and avatar presentation wiring | `absorb into ai-dev-system` | `ai-dev-system/clients/unity-client/` | This is the most path-sensitive move and needs temporary adapters or shim scripts when the actual relocation happens. |
| `ai/` | Repo-local AI context files such as `CONTEXT_SUMMARY.md`, `SYSTEM_PROMPT.md`, and `RULES_SHORT.md` | Current source of truth for repo AI context files only | `absorb into ai-dev-system` | `ai-dev-system/context/` | This root should not coexist long term with a second context root under `ai-dev-system/`. |
| `scripts/` | Windows setup, startup, packaging, and backend smoke helpers | Current source of truth for operational scripts listed in `AGENTS.md` | `wrap by temporary adapter` | `ai-dev-system/scripts/` with root wrapper entry points during migration | Current commands and docs still point at root `scripts/`, so later phases need compatibility shims instead of a hard cutover. |
| `tools/` | Blender or asset helper scripts plus generated `reports/` and `renders/` | Current source of truth for asset-helper scripts; generated outputs are supporting artifacts rather than runtime truth | `absorb into ai-dev-system` | `ai-dev-system/asset-pipeline/tools/` plus later split of generated outputs into workbench/report locations | The folder contains active helper code, so it should move into the owned non-backend system rather than stay as a sibling root. |
| `bleder/` | Blender work files for avatar and clothing authoring | Lab or workbench area, not required runtime code | `archive/workbench` | `ai-dev-system/workbench/blender/` | Treat as authoring input, not as runtime or governance. |
| `Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/` | Raw imported FBX and texture source files | Lab or workbench area, not required runtime code | `archive/workbench` | `ai-dev-system/workbench/imports/meshy/Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/` | Treat as raw import source and keep it away from runtime folders. |
| `release/` | Checked-in packaged snapshot with mirrored `backend/` and `scripts/` subtrees | Not a source-of-truth code root; this is a release artifact or staging snapshot | `delete after migrate` | Prefer `ai-dev-system/dist/` only if checked-in release artifacts remain necessary; otherwise stop treating this as a persistent root | Packaging truth already lives in root scripts, so the checked-in release tree should not survive as a parallel source. |

### Roots that stay outside `ai-dev-system/`

| Root | Current implementation on 2026-04-07 | Why it stays outside |
| --- | --- | --- |
| `docs/` | Repo documentation, including current-state docs, migration docs, runbook docs, and design-target docs | Governance root by design; it documents the whole repo and should stay outside runtime or subsystem code. |
| `tasks/` | Active queue, manual blockers, phased backlog, and done log | Governance root by design; it tracks work across the whole repo and should stay outside runtime or subsystem code. |
| `.vscode/` | Machine-local editor configuration | Editor-local convenience root, not part of runtime or migration ownership. |

## Root Mapping Baseline

| Current root | Planned root or disposition |
| --- | --- |
| `ai-dev-system/` | Remains the destination root; later phases redesign its internal layout |
| `unity-client/` | `ai-dev-system/clients/unity-client/` |
| `ai/` | `ai-dev-system/context/` |
| `scripts/` | `ai-dev-system/scripts/` with temporary root wrappers during transition |
| `tools/` | `ai-dev-system/asset-pipeline/tools/` |
| `bleder/` | `ai-dev-system/workbench/blender/` |
| `Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/` | `ai-dev-system/workbench/imports/meshy/Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/` |
| `release/` | retire the root after migration; use `ai-dev-system/dist/` only if a checked-in packaged snapshot is still justified |
| `docs/` | stays at repo root |
| `tasks/` | stays at repo root |
| `.vscode/` | stays at repo root |

## Current Source-Of-Truth Summary

### Current implementation

- Required runtime truth still lives in `local-backend/` plus `unity-client/`.
- Root operational truth still lives in `scripts/setup_windows.ps1`, `scripts/run_all.ps1`, `scripts/package_release.ps1`, and `scripts/smoke_backend.py`.
- `ai-dev-system/` already owns its own automation code, tests, tasks, and logs, but it does not yet own the Unity client, repo AI context, or root operational scripts.

### Optional subsystem

- `ai-dev-system/` is currently optional relative to the shipped assistant runtime, even though it is real and actively maintained automation code.

### Lab or workbench areas

- `bleder/`
- `Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/`
- the generated `reports/` and `renders/` outputs under `tools/`
- the checked-in `release/` snapshot

## Uncertainty Removed By This Baseline

- There is no ambiguous "maybe still runtime" status for `tools/`, `bleder/`, `Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/`, or `release/`; they are now explicitly classified as owned migration candidates or workbench or artifact areas.
- There is no ambiguity about `docs/` and `tasks/`; they stay outside `ai-dev-system/` as governance roots.
- There is no ambiguity about the current runtime; it remains `local-backend/` plus `unity-client/` until later phases actually move paths and re-verify them.
- There is no ambiguity about missing planned roots from the design text: `Clothy3D_Studio/` and `agent-platform/` are not present in the repository snapshot audited on 2026-04-07, so they are not part of this baseline inventory.

## Verification Evidence

- Root inventory captured from the repo root on 2026-04-07 via directory listing.
- `ai-dev-system/README.md` was reviewed to confirm that `ai-dev-system/` is currently documented as separate from the shipped assistant runtime.
- `README.md`, `docs/roadmap.md`, `docs/migration/phase0.md`, and task trackers were reviewed to confirm that current implementation still treats `unity-client/` plus `local-backend/` as the runtime source of truth.
- Root-path reference audit confirmed that root `scripts/` and `unity-client/` are still directly referenced by current docs and operational helpers, which is why this baseline marks them as path-sensitive migration targets rather than already-moved structure.
