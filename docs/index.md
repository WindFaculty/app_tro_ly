# Documentation Index

`README.md` is the repo entry point. This page is the docs entry point.

## Architecture

- [architecture/README.md](architecture/README.md) - folder guide for current architecture, dependency, layering, and ADR docs
- [roadmap.md](roadmap.md) - fastest current-state repo map and reading order
- [02-architecture.md](02-architecture.md) - current assistant runtime architecture
- [architecture/domain-map.md](architecture/domain-map.md) - current runtime domains and ownership boundaries
- [architecture/dependency-rules.md](architecture/dependency-rules.md) - dependency rules for the current repo layout
- [architecture/phase1-audit.md](architecture/phase1-audit.md) - current violation list and cleanup status
- [architecture/phase2-layering.md](architecture/phase2-layering.md) - current Phase 2 layering slice notes
- [architecture/adr/README.md](architecture/adr/README.md) - ADR index for deeper decision rationale

## Features

- [features/README.md](features/README.md) - feature-level navigation for UI and avatar docs
- [04-ui.md](04-ui.md) - current Unity UI behavior and placeholder boundaries
- [03-api.md](03-api.md) - current backend contract
- [features/avatar-asset-spec.md](features/avatar-asset-spec.md) - current avatar asset contract, registry boundary, and slot rules
- [features/avatar-asset-intake-checklist.md](features/avatar-asset-intake-checklist.md) - intake checklist for avatar content changes
- [avatar-spec.md](avatar-spec.md) - avatar-specific spec notes currently tracked in the repo

## Operations

- [operations/README.md](operations/README.md) - workflow and governance navigation
- [operations/agent-workflow.md](operations/agent-workflow.md) - repo workflow and completion protocol for agents
- [operations/documentation-governance.md](operations/documentation-governance.md) - docs hierarchy, source-of-truth map, and docs-changed-with-code rule
- [operations/doc-audit-checklist.md](operations/doc-audit-checklist.md) - completion-time doc audit checklist
- [09-runbook.md](09-runbook.md) - setup, startup, smoke, packaging, and troubleshooting
- [migration/phase0.md](migration/phase0.md) - modularization baseline, freeze rules, and change-governance guardrails
- [../tasks/task-template.md](../tasks/task-template.md) - required task shape and completion checklist for scoped AI work

## Notes

- Code remains the final source of truth when docs and implementation diverge.
- `unity-client/Assets/Resources/UI/ui_feature_map.md` is a design target, not implementation truth.
