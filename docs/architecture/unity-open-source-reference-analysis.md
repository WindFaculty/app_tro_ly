# Unity Open-Source Reference Analysis

Updated: 2026-04-08
Status: Current implementation plus reference study

## Scope

This repo reviewed two upstream references as inputs to the repo-native Unity automation design:

- `hatayama/unity-cli-loop`
- `Besty0728/Unity-Skills`

Current implementation note:

- These repos are references, not source-of-truth code roots for `app_tro_ly`.
- This repo does not vendor either upstream codebase into `ai-dev-system/control-plane/`.

## Unity CLI Loop

### Design goal

- Give AI agents a tight Unity development loop through CLI and optional MCP surfaces.
- Keep the core tool set small around compile, tests, logs, hierarchy, play mode, screenshots, and dynamic code execution.

### What is strong

- Clear build or test or log loop for agentic iteration.
- Direct CLI usage is explicit and production-friendly for local automation.
- Artifact-first patterns reduce context pressure during long runs.
- The upstream docs are clear that CLI is the preferred lane and MCP is secondary.

### What this repo absorbs

- Execution-layer mindset for `launch`, `compile`, `get-logs`, `run-tests`, `get-hierarchy`, and Play Mode control.
- Optional dependency posture instead of repo vendoring.
- Artifact-first command normalization in the new `unity_integration/backends/cli_loop.py`.

### What this repo does not absorb directly

- Upstream package source code.
- Upstream skill templates as repo-owned truth.
- Upstream command surface as business-logic API.

## Unity-Skills

### Design goal

- Expose a large capability-based automation surface for Unity Editor tasks at scene, object, material, workflow, and perception levels.
- Support both code-first and direct-manipulation modes.

### What is strong

- Capability taxonomy is broad and organized.
- Batch operations and rollback-aware workflow concepts are useful.
- Registry and discovery mindset scales better than one-off tool wiring.
- The project distinguishes light assistance from full-auto control.

### What this repo absorbs

- Capability taxonomy and curated-surface thinking.
- Batch-operation framing for scene or object work.
- Workflow snapshot or rollback concepts as future extension points.
- Discovery-minded cataloging in the new `unity_integration/capability_catalog.py`.

### What this repo does not absorb directly

- The upstream HTTP server and reflection router.
- The 500-plus skill breadth.
- The bundled advisory module set as runtime dependency.
- A direct mirror of the upstream package layout.

## Repo-Native Conclusion

Current implementation:

- `ai-dev-system/control-plane/unity_integration/` now owns the shared Unity integration core.
- Unity CLI Loop is treated as an optional external execution lane.
- Unity-Skills is treated as a capability-model reference, not a dependency.

Design result:

- Execution concerns stay separated from orchestration and Unity business runtime code.
- Capability names are curated for this repo instead of copied wholesale from either upstream.
- Existing Unity MCP and GUI-agent lanes remain compatible while moving onto the shared core.
