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
- `mcp_client.py`
  - shared MCP stdio client for the older autonomous workflow path

## Ownership Map

- GUI automation: `app/automation/`
- Unity automation runtime: `app/unity/` and `mcp_client.py`
- canonical platform definitions: `catalog/`
- workflow lifecycle and run history: `orchestrator/`
- harness export rendering: `adapters/`
- profiles: `app/profiles/`
- planner logic:
  - `app/agent/planner.py`
  - `planner/planner_agent.py`
- verifier logic:
  - `app/agent/verifier.py`
  - `agents/debugger_agent.py`
- healing and recovery:
  - `app/agent/healing.py`
  - `app/agent/recovery.py`
- capability routing:
  - `app/unity/capabilities.py`
  - `app/agent/strategies/`

## Compatibility Note

- Phase 9 removed the temporary root-level compatibility packages that had mirrored `app`, `agents`, `planner`, `executor`, `memory`, `tools`, and `mcp_client.py`.
- Supported entry points now bootstrap `control-plane/` directly through `bootstrap_control_plane.py`, `sitecustomize.py`, or the standardized wrappers under `../scripts/run/`.
