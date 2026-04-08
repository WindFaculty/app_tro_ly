# Tests

Current implementation: this directory now owns the subsystem test catalog and the repo-side structure validation entry points for the unified non-backend tree.

## Current Ownership Buckets

- `control-plane/`
  - catalog entry for the current Python automation tests in `../control-plane/app/tests/` and the workflow-oriented tests that still live directly under this root
- `unity-integration/`
  - catalog entry for the current Unity EditMode and PlayMode test roots in `../clients/unity-client/Assets/Tests/`
- `asset-pipeline/`
  - catalog entry for the current structure and asset-contract validation entry points
  - Mesh AI pipeline structure validation entry points
- `structure/`
  - current repo-side validation tests that catch Phase 7, Phase 8, and Phase 9 wrapper, doc, tracker, and architecture-lock drift

## Current Truth Sources

- `tests/test_*.py`
  - existing workflow and control-plane-adjacent Python tests
  - `test_mesh_ai_pipeline.py` now covers the Mesh AI profile-selection, plan-building, and bundle-materialization path
- `../control-plane/app/tests/`
  - current control-plane runtime and unit coverage
- `../clients/unity-client/Assets/Tests/`
  - current Unity EditMode and PlayMode coverage
- `structure/test_phase7_structure.py`
  - current repo-side unittest coverage for Phase 7 structure validation
- `structure/test_unification_docs_tasks.py`
  - current repo-side unittest coverage for Phase 8 docs-and-task-governance drift validation
- `structure/test_phase9_architecture_lock.py`
  - current repo-side unittest coverage for Phase 9 shim-removal and stale-path architecture-lock validation

## Planned Work Still Not Done

- The historical top-level Python tests under this root have not been moved into per-bucket directories yet.
- Unity test execution still requires Unity Editor runtime evidence and remains separate from terminal-only structure validation.
- Python coverage that depends on external packages still requires installing `requirements.txt`; Phase 7 only standardizes the layout and adds stdlib-only structure validation.
