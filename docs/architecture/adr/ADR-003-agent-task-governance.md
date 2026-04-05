# ADR-003: Agent Task Workflow And Completion Protocol

- Status: Accepted
- Date: 2026-04-05

## Context

- The repo already had freeze rules, task trackers, and a task template, but the expected agent behavior was still spread across `AGENTS.md`, tracker notes, and completion habits.
- Phase 5 requires a stronger agent workflow with a task queue protocol, a completion protocol, and explicit guardrails around scope and evidence.
- The repo is in a modularization freeze, so the workflow must reinforce current truth sources instead of inventing a second process.

## Decision

- Keep `AGENTS.md` as the repo-wide machine-facing rule file.
- Add a dedicated operations doc for the current agent workflow and completion protocol.
- Expand `tasks/task-template.md` so scoped work includes expected outputs and the final completion report items.
- Require agents to read the relevant active task before editing and to update the tracker when scope, blockers, or done criteria changed.
- Keep verification evidence mandatory before work is described as done.

## Options Considered

### Leave workflow rules only in `AGENTS.md`

- Rejected because the repo now has enough governance detail that a short operation guide improves reuse and reduces drift.

### Move all workflow rules into task tracker files

- Rejected because tracker files should stay focused on current work, not become a policy dump.

## Consequences

- Agent expectations are now easier to trace and maintain.
- Completion reporting is more consistent across repo-only and manual-gate work.
- The workflow still depends on humans respecting `P02`, `P04`, and other manual gates before making production-level claims.
