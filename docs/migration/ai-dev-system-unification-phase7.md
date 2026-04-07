# AI Dev System Unification Phase 7

Updated: 2026-04-07
Status: Current implementation updated so standardized non-backend entry points and repo-side structure validation now live under `ai-dev-system/scripts/` and `ai-dev-system/tests/`

## Purpose

This document records the Phase 7 scripts and tests standardization pass for the `ai-dev-system` migration plan.

The goal of this phase is to make `ai-dev-system/` the first place a contributor checks for non-backend run, validate, package, and structure-validation entry points without claiming the older root operational scripts or Unity Editor validation already moved.

## What Changed In Phase 7

### Current implementation

- `ai-dev-system/scripts/` now owns a standardized command surface for:
  - `run/run-gui-agent.ps1`
  - `run/inspect-unity-profile.ps1`
  - `run/run-unity-automation.ps1`
  - `validate/validate-structure.ps1`
  - `validate/validate-avatar-pipeline.ps1`
  - `package/package-unity-client.ps1`
- `ai-dev-system/tests/` now owns:
  - a test-catalog README that maps current tests by ownership bucket
  - explicit `control-plane/`, `unity-integration/`, `asset-pipeline/`, and `structure/` buckets
  - a stdlib-only `tests/structure/test_phase7_structure.py` check for wrapper, doc, and tracker drift
- `ai-dev-system/README.md` and `ai-dev-system/AGENTS.md` now describe `scripts/` and `tests/structure/` as current implementation instead of planned-only placeholders.

### Planned work still not done

- Root `scripts/setup_windows.ps1`, `scripts/run_all.ps1`, `scripts/package_release.ps1`, and `scripts/smoke_backend.py` still own the underlying repo-wide operational flow.
- The older top-level Python tests under `ai-dev-system/tests/` were not moved into per-bucket folders in this phase.
- Live control-plane commands still require installing Python dependencies from `ai-dev-system/requirements.txt`.
- Unity EditMode or PlayMode execution still requires Unity Editor and remains manual or editor-driven validation.

## Source-Of-Truth Shift

| Before Phase 7 | After Phase 7 |
| --- | --- |
| `ai-dev-system/scripts/` was a README-only placeholder. | `ai-dev-system/scripts/` now provides the standardized run, validate, and package entry-point surface for the non-backend subsystem. |
| `ai-dev-system/tests/` only held root-level tests plus a placeholder README. | `ai-dev-system/tests/` now includes explicit ownership buckets and repo-side structure validation under `tests/structure/`. |
| Contributors still had to infer where validation for migration drift should live. | Phase 7 adds `scripts/validate/validate_phase7_structure.py` and `tests/structure/test_phase7_structure.py` as repeatable drift checks. |

## Acceptance Check

Phase 7 is satisfied in repo state when:

- `ai-dev-system/scripts/` exposes standardized run or validate or package entry points
- `ai-dev-system/tests/` exposes explicit ownership buckets and a repo-side structure-validation bucket
- subsystem docs and task trackers describe the new command surface without claiming root operational scripts or Unity Editor validation already moved
- a repeatable validation command exists for Phase 7 structure drift

## Verification Evidence

- `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/validate/validate-structure.ps1`
  - completed successfully and re-ran the Phase 6 validator plus the new Phase 7 validator
- `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/validate/validate-avatar-pipeline.ps1`
  - completed successfully and confirmed current slot taxonomy, sample item catalog, tool catalog, Unity validator path, and root Blender validator path
- `python ai-dev-system/scripts/validate/validate_phase7_structure.py`
  - completed successfully with stdlib-only validation
- `python -m unittest discover -s ai-dev-system/tests/structure -p "test_*.py"`
  - completed successfully for repo-side Phase 7 structure coverage
- Live control-plane command execution beyond structural checks remains environment-dependent because this shell does not have the `ai-dev-system` Python dependencies installed yet.
