# Structure Tests

Current implementation:

- `test_phase7_structure.py` is the stdlib-only unittest that verifies the Phase 7 script surface, migration-doc presence, and tracker references stay aligned.
- `test_unification_docs_tasks.py` is the stdlib-only unittest that verifies the Phase 8 overview docs, unification backlog, and queue references stay aligned.
- `test_phase9_architecture_lock.py` is the stdlib-only unittest that verifies the Phase 9 shim removal, supported bootstrap surface, and stale active-path checks stay aligned.
- `test_mesh_ai_pipeline_structure.py` is the stdlib-only unittest that verifies Mesh AI lifecycle contracts, workflow specs, wrapper mapping, and doc/task references stay aligned.
- `../../scripts/validate/validate_phase7_structure.py` is the command-line validator that the unittest reuses.
- `../../scripts/validate/validate_unification_docs_tasks.py` is the command-line validator for the docs-and-tasks rewrite.
- `../../scripts/validate/validate_phase9_architecture_lock.py` is the command-line validator for the Phase 9 architecture lock.
- `../../scripts/validate/validate_mesh_ai_pipeline.py` is the command-line validator for the Mesh AI asset-refine foundation.

Use this bucket for repo-side drift checks that can run without Unity Editor or external Python packages.
