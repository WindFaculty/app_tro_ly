# Target Agent Platform Architecture

Current implementation: the canonical agent-platform core now lives in `ai-dev-system/control-plane/catalog/platform.json`, `ai-dev-system/control-plane/orchestrator/`, and `ai-dev-system/control-plane/adapters/`, with generated Codex and Antigravity surfaces exported from that core.

## Canonical Internal Architecture

1. Agent catalog
   `ai-dev-system/control-plane/catalog/platform.json` -> `agents[]`
2. Skill catalog
   `ai-dev-system/control-plane/catalog/platform.json` -> `skills[]`
3. Workflow definitions
   `ai-dev-system/control-plane/catalog/platform.json` -> `workflows[]`
4. Rules and policies
   `ai-dev-system/control-plane/catalog/platform.json` -> `rule_packs[]`
   Policy sources: `ai-dev-system/context/policies/`
5. Orchestrator
   `ai-dev-system/control-plane/orchestrator/engine.py`
6. Planner
   Current implementation: `ai-dev-system/control-plane/planner/planner_agent.py`
   Live GUI planning remains in `ai-dev-system/control-plane/app/agent/planner.py`
7. Task model
   Current workflow task payloads remain in `ai-dev-system/control-plane/agents/contracts.py`
   Lifecycle records live in `ai-dev-system/control-plane/orchestrator/models.py`
8. Execution lifecycle
   `queued -> planning -> ready -> executing -> reviewing -> verifying -> done`
   Side states: `blocked`, `failed`, `replanning`
9. Review and verification layer
   `ai-dev-system/control-plane/tools/workflow_report.py`
   `ai-dev-system/control-plane/orchestrator/engine.py`
   `ai-dev-system/control-plane/catalog/platform.json` -> `evals[]`
10. Memory and learning layer
    `ai-dev-system/control-plane/memory/lesson_store.py`
    `ai-dev-system/control-plane/orchestrator/history.py`
11. Harness adapters
    `ai-dev-system/control-plane/adapters/surfaces.py`
    `ai-dev-system/scripts/sync-agent-surfaces.py`
12. Documentation sync
    `docs/architecture/*.md`
    `AGENTS.md`
    `ai-dev-system/AGENTS.md`
    `tasks/task-queue.md`
    `tasks/done.md`
13. Tool registry and MCP strategy
    `ai-dev-system/control-plane/catalog/platform.json` -> `tool_policies[]`
    `ai-dev-system/control-plane/mcp_client.py`
    `ai-dev-system/control-plane/executor/executor_agent.py`
14. Observability, logs, and run history
    `ai-dev-system/control-plane/tools/json_logger.py`
    `ai-dev-system/logs/run-log.jsonl`
    `ai-dev-system/logs/lessons.jsonl`
    `ai-dev-system/logs/agent-platform-run-history.jsonl`

## Recommended Repository Structure

```text
ai-dev-system/
|- control-plane/
|  |- app/                 live Windows + Unity automation runtime
|  |- catalog/             canonical agent, skill, workflow, rule, eval, tool-policy specs
|  |- orchestrator/        shared lifecycle, review, verification, and run history
|  |- adapters/            harness export rendering for Codex and Antigravity
|  |- planner/             bounded Unity workflow planner implementation
|  |- executor/            bounded Unity workflow executor implementation
|  |- agents/              workflow-specific reviewer/debugger helpers
|  |- memory/              lesson persistence
|  |- tools/               reporting and logging helpers
|  `- mcp_client.py        Unity MCP transport
|- context/
|  |- prompts/             role prompt sources
|  `- policies/            governance and verification policy sources
|- scripts/
|  |- sync-agent-surfaces.py
|  `- validate/
|- workflows/              runtime entrypoints using the orchestrator
`- tests/                  catalog, lifecycle, and surface-drift validation
```

## Current Implementation

- The canonical core is implemented for the bounded Unity workflow path today.
- Codex is now the primary generated harness surface through `.codex/` and `.agents/skills/`.
- Antigravity is a secondary generated adapter through `.agent/`.
- The live GUI automation runtime in `control-plane/app/` remains the runtime truth and is intentionally not replaced by the new catalog layer.

## Planned Work

- Unify more than one workflow under the canonical orchestrator once the repo adds additional bounded agent loops.
- Pull more review and verification patterns from `control-plane/app/agent/verifier.py` into shared eval specs when reuse becomes practical.
- Expand the skill and agent catalog only when repeated repo work proves those roles are operationally valuable.

## Migration Path

1. Audit and baseline the existing control plane.
2. Define the canonical catalog plus policy and prompt sources.
3. Route `workflows/autonomous_loop.py` through the shared orchestrator.
4. Generate `.codex/`, `.agents/skills/`, and `.agent/` from the catalog.
5. Add drift validation and targeted tests.
6. Later, normalize additional workflows onto the same catalog and lifecycle model instead of creating new ad hoc stacks.

## Validation

- Current implementation: unit coverage now exists for catalog loading, orchestrator lifecycle behavior, retryable replan behavior, workflow reporting, and generated surface drift.
- Manual validation required: live Unity Editor and real MCP execution still require the runtime dependencies and target-machine session that exist outside terminal-only verification.
