# AI Dev System Unification Phase 9

Updated: 2026-04-07
Status: Current implementation updated so temporary control-plane import shims are removed, stale absorbed-root path fallbacks are retired, and architecture-lock validation now guards the landed layout

## Purpose

This document records the Phase 9 architecture-lock pass for the `ai-dev-system` migration plan.

The goal of this phase is to remove the last "half-old or half-new" compatibility surface for already-absorbed roots without claiming that unrelated runtime or asset migrations are complete.

## What Changed In Phase 9

### Current implementation

- The temporary root-level shim packages under `ai-dev-system/` were removed:
  - `app/`
  - `agents/`
  - `planner/`
  - `executor/`
  - `memory/`
  - `tools/`
  - `mcp_client.py`
- `ai-dev-system/bootstrap_control_plane.py` now provides the supported explicit bootstrap helper for control-plane imports.
- `ai-dev-system/sitecustomize.py` now provides a repo-local bootstrap hook for environments that honor Python `sitecustomize`.
- `ai-dev-system/run_demo.py`, `ai-dev-system/verify_connection.py`, `ai-dev-system/workflows/autonomous_loop.py`, and the surviving top-level Python tests now bootstrap `control-plane/` directly instead of depending on root shim packages.
- `ai-dev-system/scripts/run/run-gui-agent.ps1` and `ai-dev-system/scripts/run/inspect-unity-profile.ps1` now inject `PYTHONPATH` for `control-plane/` plus the subsystem root before invoking `python -m app.main`.
- `scripts/assistant_common.ps1` no longer falls back to the absorbed repo-root `unity-client/` path; the live Unity project path is only `ai-dev-system/clients/unity-client/`.
- Active docs and task trackers now describe the current automation runtime as `ai-dev-system/control-plane/` without presenting the removed shim layer as current behavior.
- `ai-dev-system/scripts/validate/validate_phase9_architecture_lock.py`, `ai-dev-system/scripts/validate/validate-architecture-lock.ps1`, and `ai-dev-system/tests/structure/test_phase9_architecture_lock.py` now provide repeatable architecture-lock drift checks.

### Planned work still not done

- Root `scripts/`, root `tools/`, root `bleder/`, root import folders, and `release/` still contain current material that has not been fully absorbed into `ai-dev-system/`.
- Unity Editor smoke, packaged-client smoke, and production-avatar validation remain manual gates.
- External Python dependencies from `ai-dev-system/requirements.txt` are still required for live control-plane execution beyond stdlib-only drift validation.

## Source-Of-Truth Shift

| Before Phase 9 | After Phase 9 |
| --- | --- |
| Active scripts and tests could still rely on root `app`, `agents`, `planner`, `tools`, or `mcp_client` shims under `ai-dev-system/`. | Active scripts and tests now bootstrap `ai-dev-system/control-plane/` directly. |
| Root `unity-client/` could still be checked as a fallback in shared scripts. | Active shared scripts now resolve only `ai-dev-system/clients/unity-client/` as the Unity project root. |
| Tracker and overview docs still described temporary shim packages as current compatibility behavior. | Active docs now treat shim removal as landed current implementation and validate drift against stale references. |

## Acceptance Check

Phase 9 is satisfied in repo state when:

- the temporary shim packages for the absorbed control-plane roots are gone
- active scripts and tests use the supported bootstrap surface instead of those removed shims
- active docs and trackers no longer describe the removed shims or absorbed root paths as current behavior
- repeatable validators fail if the removed shim paths or stale active-path references return

## Verification Evidence

- `python ai-dev-system/scripts/validate/validate_phase9_architecture_lock.py`
  - completed successfully with stdlib-only validation for shim absence, bootstrap files, active wrapper expectations, and stale-path checks
- `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/validate/validate-architecture-lock.ps1`
  - completed successfully as the standardized wrapper for the Phase 9 architecture-lock validator
- `python -m unittest discover -s ai-dev-system/tests/structure -p "test_*.py"`
  - completed successfully with repo-side structure coverage including the new Phase 9 drift test
- `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/validate/validate-structure.ps1`
  - completed successfully after Phase 9 and now re-runs the Phase 6, Phase 7, and Phase 9 structure checks together
- `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/run/run-gui-agent.ps1 --help`
  - advanced far enough to resolve `app.main` from `control-plane/` and then stopped on the environment dependency `ModuleNotFoundError: No module named 'mcp'`, which confirms shim removal did not break module resolution
- `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/run/inspect-unity-profile.ps1 --help`
  - likewise resolved the control-plane import path and then stopped on the same missing external `mcp` dependency rather than a removed shim path
