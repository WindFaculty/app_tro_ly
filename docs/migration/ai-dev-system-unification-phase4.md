# AI Dev System Unification Phase 4

Updated: 2026-04-07
Status: Current implementation updated so automation runtime now lives under `ai-dev-system/control-plane/`

## Purpose

This document records the Phase 4 control-plane unification pass for the `ai-dev-system` migration plan.

The goal of this phase is to make `control-plane/` the current source of truth for the automation subsystem before any `unity-client/` root move happens.

## What Changed In Phase 4

### Current implementation

- The following automation roots were moved under `ai-dev-system/control-plane/`:
  - `app/`
  - `agents/`
  - `executor/`
  - `planner/`
  - `memory/`
  - `tools/`
- The old `unity-interface/mcp_client.py` module was moved to `ai-dev-system/control-plane/mcp_client.py`.
- Root-level compatibility shims were added for:
  - `app`
  - `agents`
  - `executor`
  - `planner`
  - `memory`
  - `tools`
  - `mcp_client.py`
- Workflow entry scripts and tests were updated so they resolve control-plane code through the new structure.

### Planned work still not done

- `unity-client/` still remains at the repo root.
- The control plane still has mixed internal shapes such as `app/agent/` plus sibling workflow agents; later cleanup can unify these further.
- Root `scripts/`, root `tools/`, root `bleder/`, `release/`, and asset-import roots have not been migrated yet.

## Source-Of-Truth Shift

| Before Phase 4 | After Phase 4 |
| --- | --- |
| `ai-dev-system/app/` | `ai-dev-system/control-plane/app/` |
| `ai-dev-system/agents/` | `ai-dev-system/control-plane/agents/` |
| `ai-dev-system/executor/` | `ai-dev-system/control-plane/executor/` |
| `ai-dev-system/planner/` | `ai-dev-system/control-plane/planner/` |
| `ai-dev-system/memory/` | `ai-dev-system/control-plane/memory/` |
| `ai-dev-system/tools/` | `ai-dev-system/control-plane/tools/` |
| `ai-dev-system/unity-interface/mcp_client.py` | `ai-dev-system/control-plane/mcp_client.py` |

## Acceptance Check

Phase 4 is satisfied in repo state when:

- `control-plane/` is the current runtime home for automation code
- the older automation roots no longer carry the real source files
- entry scripts and tests can still import the automation code through compatibility shims or updated imports
- docs and task trackers describe `control-plane/` as current implementation rather than planned-only structure

## Verification Evidence

- Post-move inventory confirmed the runtime roots now exist under `ai-dev-system/control-plane/`.
- Workflow scripts and tests were updated to import the moved control-plane modules.
- Repo docs and task trackers were refreshed so `control-plane/` is now described as current implementation.
- Attempted Python verification from `ai-dev-system/` with `python -m pytest tests control-plane/app/tests -q` was blocked in this shell because no usable Python interpreter is available on PATH and no repo-local `.venv/` exists.
