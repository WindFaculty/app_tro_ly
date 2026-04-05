# Architecture Decision Records

Use this folder for decisions that need more context than the summary log in `docs/06-decisions.md`.

## Workflow

- Add or update an ADR when a task changes module boundaries, source-of-truth ownership, migration governance, avatar slot rules, persistence strategy, or shared event/state contracts.
- Keep `docs/06-decisions.md` as the short chronological log.
- Use ADRs for the reasoning, rejected options, and impact details.

## Current ADRs

- [ADR-001-documentation-governance.md](ADR-001-documentation-governance.md) - adopts the documentation hierarchy, source-of-truth map, and docs-changed-with-code workflow
- [ADR-002-avatar-asset-registry.md](ADR-002-avatar-asset-registry.md) - adopts the registry-backed avatar asset catalog and metadata boundary
- [ADR-003-agent-task-governance.md](ADR-003-agent-task-governance.md) - adopts the current agent workflow, task protocol, and completion reporting shape
