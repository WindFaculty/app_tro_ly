# ADR-001: Documentation Governance Hierarchy

- Status: Accepted
- Date: 2026-04-05

## Context

- The repo already has current-state docs, migration docs, and task trackers, but the navigation and ownership rules were still spread across README text, tracker notes, and individual docs.
- Phase 3 from `tai_cau_truc_tach_logic.md` requires a docs hierarchy, an explicit source-of-truth map, ADR usage, and a mandatory docs update workflow.
- The active runtime is still `local-backend/` plus `unity-client/`, so the docs change should improve governance without inventing new runtime surfaces.

## Decision

- Keep `README.md` as the repo entry point.
- Use `docs/architecture/` for current module, dependency, layering, and ADR records.
- Use `docs/features/` for feature-specific navigation and specs.
- Use `docs/operations/` for documentation governance, runbook-style process, and task or agent workflow guidance.
- Keep `docs/06-decisions.md` as the short decision ledger, and store expanded rationale in `docs/architecture/adr/`.
- Require docs updates in the same task when a change affects module boundaries, feature flow, public contracts, asset spec, or agent workflow.

## Options Considered

### Keep the current flat docs layout only

- Rejected because readers still have to guess which file owns which kind of truth.

### Move every existing doc into new folders immediately

- Rejected for now because it adds churn and broken-link risk during the modularization freeze.
- Navigation layers can land first without claiming the deeper move is already necessary.

## Consequences

- Repo navigation becomes clearer without changing runtime code paths.
- Task completion now has a concrete checklist for doc updates.
- Future architecture or governance changes must touch both `docs/06-decisions.md` and the relevant ADR when the summary log is too small.
