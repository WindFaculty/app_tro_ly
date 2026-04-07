# AI Dev System Unification Phase 1

Updated: 2026-04-07
Status: Current implementation plus planned-architecture baseline for `ai-dev-system/`

## Purpose

This document records the Phase 1 architecture pass for the `ai-dev-system` unification plan.

Phase 1 does not move the main runtime roots yet.
It defines the target subsystem layers, rewrites subsystem guidance, and makes the non-backend integration root explicit before later migrations.

## What Changed In Phase 1

### Current implementation

- `ai-dev-system/README.md` now describes `ai-dev-system/` as the planned main non-backend system rather than an adjacent optional tooling folder only.
- `ai-dev-system/AGENTS.md` now provides subsystem-local truth sources and migration rules.
- The following Phase 1 layer directories now exist:
  - `ai-dev-system/control-plane/`
  - `ai-dev-system/clients/`
  - `ai-dev-system/domain/`
  - `ai-dev-system/context/`
  - `ai-dev-system/asset-pipeline/`
  - `ai-dev-system/workbench/`
  - `ai-dev-system/scripts/`
- Root `AGENTS.md` was shortened to keep repo-level rules and truth-source pointers only.

### Planned work

- No large path move has happened yet.
- `unity-client/` still lives at the repo root.
- Root `ai/` still lives at the repo root.
- Root `scripts/`, `tools/`, `bleder/`, `release/`, and raw import roots have not been migrated yet.

## Phase 1 Ownership Snapshot

| Layer | Phase 1 status | Current source of truth today |
| --- | --- | --- |
| `control-plane/` | scaffolded target layer | `ai-dev-system/app/`, `agents/`, `executor/`, `planner/`, `unity-interface/` |
| `clients/` | scaffolded target layer | root `unity-client/` |
| `domain/` | scaffolded target layer | still distributed outside `ai-dev-system/` |
| `context/` | scaffolded target layer | root `ai/` plus `ai-dev-system/prompts/` |
| `asset-pipeline/` | scaffolded target layer | root `tools/` |
| `workbench/` | scaffolded target layer | root `bleder/` and raw import roots |
| `scripts/` | scaffolded target layer | root `scripts/` |
| `tests/` | already active and now explicitly labeled | `ai-dev-system/tests/` plus `ai-dev-system/app/tests/` |

## Acceptance Check

Phase 1 is satisfied in repo state when:

- `ai-dev-system/README.md` is enough to explain that this subsystem is the non-backend integration center
- the target layer directories exist under `ai-dev-system/`
- `ai-dev-system/AGENTS.md` exists
- repo-level docs keep distinguishing current runtime truth from planned structure

## Verification Evidence

- Directory inventory under `ai-dev-system/` was rechecked after the Phase 1 edit set.
- `ai-dev-system/README.md` and `ai-dev-system/AGENTS.md` were reviewed to confirm the new ownership framing.
- Root `AGENTS.md`, root `README.md`, `docs/index.md`, `docs/roadmap.md`, and task trackers were updated so the architecture shift is documented without claiming runtime path moves that have not happened.
