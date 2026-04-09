# Scripts

Current implementation: this directory now owns the standardized non-backend entry-point surface for the unified subsystem.

## Current Entry Points

- `run/run-gui-agent.ps1`
  - forwards to the current control-plane CLI in `ai-dev-system/`
- `run/inspect-unity-profile.ps1`
  - standard inspect entry point for the `unity-editor` profile
- `run/run-unity-automation.ps1`
  - now selects between the legacy workflow demo, GUI-agent lane, and shared Unity integration lane
- `validate/validate-structure.ps1`
  - runs the Phase 6, Phase 7, and Phase 9 structure validators
- `validate/validate-avatar-pipeline.ps1`
  - validates the current avatar-domain and asset-pipeline contract roots without claiming editor automation already moved
- `validate/validate-docs-tasks.ps1`
  - validates the unification overview docs and task-governance tracker links for drift
- `validate/validate-architecture-lock.ps1`
  - validates the Phase 9 architecture lock for shim absence, bootstrap surface, and stale active-path drift
- `validate/validate-mesh-ai-pipeline.ps1`
  - validates the Mesh AI lifecycle contracts, workflow specs, wrapper mapping, and task/doc references
- `validate/validate-unity-integration.ps1`
  - validates the shared Unity integration layer, capability catalog, and required docs
- `package/package-unity-runtime.ps1`
  - forwards to the current release-packaging script at repo root

## Ownership Boundary

- `ai-dev-system/scripts/` now owns the standardized command surface for non-backend run, validate, and package actions.
- Root `scripts/` still owns the current Windows operational internals for setup, startup, packaging, and backend smoke.
- `migrate/` exists as the reserved location for later migration helpers; no active move script is required in current implementation.

## Planned Work Still Not Done

- Root `scripts/setup_windows.ps1`, `scripts/run_all.ps1`, `scripts/package_release.ps1`, and `scripts/smoke_backend.py` have not been moved under `ai-dev-system/scripts/`.
- No single wrapper here replaces backend setup or full repo startup yet because those flows still cross the repo boundary outside `ai-dev-system/`.
