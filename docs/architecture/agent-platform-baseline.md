# Agent Platform Baseline

Current implementation: the repo already contains a capable automation control plane, but agent-platform behavior was split across Python runtime code, prompt files, workflow helpers, and harness-facing guidance without one canonical source of truth.

## Audit Scope

- `ai-dev-system/control-plane/app/agent/`
- `ai-dev-system/control-plane/agents/`
- `ai-dev-system/control-plane/planner/`
- `ai-dev-system/control-plane/executor/`
- `ai-dev-system/control-plane/memory/`
- `ai-dev-system/control-plane/tools/`
- `ai-dev-system/workflows/`
- `ai-dev-system/context/`
- `ai-dev-system/tests/`
- repo harness surfaces such as `AGENTS.md`, `.codex/`, `.agent/`, and `.agents/skills/` when present

## What Already Exists And Is Good

- `ai-dev-system/control-plane/app/agent/controller.py` already implements a deterministic observe -> act -> verify -> recover loop for Windows and Unity automation with structured artifacts, retries, and task-level verification.
- `ai-dev-system/control-plane/app/agent/planner.py`, `state.py`, `verifier.py`, `recovery.py`, and `jobs.py` already provide bounded planning, explicit action objects, verification checks, and recovery decisions for the live GUI agent.
- `ai-dev-system/control-plane/planner/planner_agent.py`, `executor/executor_agent.py`, `agents/debugger_agent.py`, and `workflows/autonomous_loop.py` already provide a bounded Unity MCP workflow with plan generation, execution, console analysis, and lessons capture.
- `ai-dev-system/control-plane/tools/workflow_report.py` already converts raw workflow output into failure categories and verification summaries instead of leaving review purely in log text.
- `ai-dev-system/control-plane/memory/lesson_store.py` plus `ai-dev-system/context/memory/` already show that reusable learning and pattern capture matter in this repo.
- `ai-dev-system/tests/` and `ai-dev-system/control-plane/app/tests/` already cover planner, debugger, workflow reporting, recovery, and task specs. The repo is not starting from a prompt bundle; it already has executable platform code.

## What Is Weak

- There was no canonical catalog for agents, skills, workflows, rules, evals, or tool policy. Definitions were scattered between code, docs, and prompts.
- The Unity autonomous loop only exposed `completed` or `failed` workflow states. Lifecycle phases such as `queued`, `planning`, `ready`, `reviewing`, and `verifying` were implicit in logs rather than modeled directly.
- Review and verification existed, but `workflow_report.py` was advisory rather than a first-class completion gate.
- Codex- and Antigravity-facing surfaces were absent or ad hoc, so role descriptions and skill guidance would have drifted as the platform evolved.
- Prompts under `ai-dev-system/context/prompts/automation/` existed as standalone markdown, not as generated or catalog-backed harness surfaces.

## What Is Missing

- A canonical agent catalog that explains when planner, architect, reviewer, security, build-fix, and loop-supervisor roles should exist in this repo.
- A small repo-native skill catalog for search-first behavior, verification loops, docs sync, context compaction, and reusable lessons.
- Explicit tool policy and eval specs that tie workflow completion to named gates instead of custom code paths only.
- Adapter generation and drift validation for `.codex/`, `.agents/skills/`, and `.agent/`.
- Run-history logging that records workflow lifecycle state as a durable trace rather than only step logs.

## What Is Duplicated Or Unclear

- `ai-dev-system/control-plane/app/agent/planner.py` and `ai-dev-system/control-plane/planner/planner_agent.py` both represent planning, but for different execution paths without a shared canonical mapping.
- `ai-dev-system/control-plane/app/agent/verifier.py` and `ai-dev-system/control-plane/agents/debugger_agent.py` both contribute to verification, but one is action-postcondition oriented while the other is workflow-console oriented.
- `ai-dev-system/context/prompts/automation/*.md` describe roles, while the actual executable behaviors live in Python classes. Before this refactor there was no single structure connecting them.
- `ai-dev-system/control-plane/memory/lesson_store.py`, `ai-dev-system/context/memory/lessons.md`, and generated lessons logs all touched learning, but they did not roll up into a reusable platform contract.

## What Is Dangerous To Scale

- Adding more workflows would have required repeating role definitions, harness instructions, and review expectations in multiple places.
- The lack of a canonical catalog meant any future Codex or Antigravity support would likely clone or flatten repo knowledge by hand.
- Completion could look healthy in logs while still shipping verification issues such as missing expected objects, because the final workflow state was not derived from named eval gates.
- Task decomposition, lessons capture, and harness adaptation were strong enough to matter, but not centralized enough to remain maintainable as the repo grows.

## What ECC-Style Reference Patterns Can Upgrade Here

- Small, explicit agent and skill catalogs instead of loose prompt collections.
- Layered rules and policies that distinguish governance from language or workflow details.
- Workflow surfaces that expose planner, execution, review, verification, and learning as named stages.
- Generated harness adapters so Codex and Antigravity consume exported surfaces from the canonical core.
- Drift validation so generated surfaces, docs, and internal specs stay synchronized.
