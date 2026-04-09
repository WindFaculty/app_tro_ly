# ECC-Inspired Gap Analysis

Current implementation: this comparison uses `everything-claude-code` only as a reference for agent catalogs, skill packaging, rules layering, workflow surfaces, verification loops, learning patterns, and harness adapters. It is not the target architecture for this repo.

## KEEP

- Keep the live runtime in `ai-dev-system/control-plane/app/`; it is already the strongest deterministic execution path in the repo.
- Keep the bounded Unity workflow stack in `ai-dev-system/control-plane/planner/`, `executor/`, `agents/`, and `workflows/`; it already provides real value and should be normalized, not replaced.
- Keep lessons capture and workflow reporting from `ai-dev-system/control-plane/memory/lesson_store.py` and `ai-dev-system/control-plane/tools/workflow_report.py`.
- Keep repo governance centered on `AGENTS.md`, `ai-dev-system/AGENTS.md`, `docs/`, and `tasks/`.

## IMPROVE

- Improve agent specialization by defining planner, architect, reviewer, security-reviewer, build-error-resolver, and loop-operator in one catalog.
- Improve workflow surface clarity by making lifecycle phases explicit and visible in run history.
- Improve rules centralization by tying policy markdown to named rule packs instead of free-floating guidance.
- Improve skill reuse by defining a small repo-native skill set for search-first, verification, docs sync, context compaction, and lessons extraction.
- Improve Codex integration by generating `.codex/` and `.agents/skills/` from the canonical core.
- Improve Antigravity readiness by exporting `.agent/` from the same source rather than designing around Antigravity first.
- Improve docs synchronization by validating generated harness surfaces against the catalog.

## REWRITE

- Rewrite `ai-dev-system/workflows/autonomous_loop.py` so it runs through a shared orchestrator with lifecycle, review, verification, and replan support.
- Rewrite the final workflow-completion decision so `workflow_report.py` and named eval gates can block `done` when missing objects or console errors remain.
- Rewrite harness-facing agent and skill definitions as generated outputs from a canonical catalog instead of disconnected markdown-only artifacts.

## REMOVE

- Remove manual harness drift as an acceptable operating mode. `.codex/`, `.agents/skills/`, and `.agent/` should no longer be hand-maintained truth sources.
- Remove implicit completion based only on step success when review or verification findings still exist.
- Remove the expectation that new agent roles can be introduced by prompt files alone without catalog, policy, and validation updates.

## DEFER

- Defer large skill catalogs and language-specific reviewer sprawl until the repo demonstrates repeated need for them.
- Defer hook-heavy enforcement models; this repo should stay Codex-first and adapter-friendly rather than depending on harness-specific hook features.
- Defer a generalized installer or marketplace layer. A repo-local sync plus validation path is enough for the current scope.
- Defer deep unification between `control-plane/app/agent/planner.py` and `control-plane/planner/planner_agent.py` until more than one workflow shares the same planning contract.
