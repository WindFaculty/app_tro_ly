# Control Plane

Current implementation: this directory is now the source of truth for the automation control plane inside `ai-dev-system/`.

## Current Source Of Truth

- `app/`
  - hybrid GUI and Unity automation runtime
  - profiles
  - controller
  - verification
  - healing
  - strategy routing
- `catalog/`
  - canonical agent, skill, workflow, rule-pack, eval, and tool-policy definitions
- `orchestrator/`
  - explicit workflow lifecycle, review, verification, and run-history handling
- `adapters/`
  - generated Codex and Antigravity surface rendering
- `agents/`
  - shared workflow contracts
  - console debugger agent
- `planner/`
  - bounded workflow planner agent
- `executor/`
  - MCP-backed workflow executor
- `memory/`
  - lesson store for workflow runs
- `tools/`
  - workflow reporting and JSON logging helpers
  - Mesh AI pipeline planning and Blender wrapper rendering
- `mcp_client.py`
  - shared MCP stdio client for the older autonomous workflow path
- `unity_integration/`
  - shared Unity environment probe, capability catalog, CLI Loop adapter, and consolidated MCP runtime

## Ownership Map

- GUI automation: `app/automation/`
- Blender MCP automation runtime: `app/blender/`
- Unity automation runtime: `app/unity/` and `mcp_client.py`
- Shared Unity integration core: `unity_integration/`
- canonical platform definitions: `catalog/`
- workflow lifecycle and run history: `orchestrator/`
- harness export rendering: `adapters/`
- profiles: `app/profiles/`
- Blender profile and policy-gated interactive lane:
  - `app/blender/`
  - `app/profiles/blender_editor_profile.py`
- planner logic:
  - `app/agent/planner.py`
  - `planner/planner_agent.py`
  - `planner/mesh_pipeline_planner.py`
- verifier logic:
  - `app/agent/verifier.py`
  - `agents/debugger_agent.py`
- healing and recovery:
  - `app/agent/healing.py`
  - `app/agent/recovery.py`
- capability routing:
  - `app/unity/capabilities.py`
  - `app/agent/strategies/`
- Mesh AI asset-refine workflow foundation:
  - `tools/mesh_ai_pipeline.py`
  - `tools/mesh_ai_blender_wrappers.py`
  - `executor/mesh_pipeline_executor.py`
  - `workflows/mesh_ai_refine.py`

## Compatibility Note

- Phase 9 removed the temporary root-level compatibility packages that had mirrored `app`, `agents`, `planner`, `executor`, `memory`, `tools`, and `mcp_client.py`.
- Supported entry points now bootstrap `control-plane/` directly through `bootstrap_control_plane.py`, `sitecustomize.py`, or the standardized wrappers under `../scripts/run/`.
