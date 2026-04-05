# Documentation Governance

Updated: 2026-04-05
Status: Current implementation governance

This page defines the documentation hierarchy, source-of-truth map, and the mandatory `docs changed with code` rule for this repo.

## Documentation Hierarchy

### Layer 1: Repo entry

- `README.md`
- Purpose: project summary, quick start, high-level runtime description, and links into deeper docs

### Layer 2: Architecture

- `docs/architecture/`
- Purpose: module boundaries, dependency rules, layering notes, and ADRs

### Layer 3: Features

- `docs/features/`
- Purpose: feature-facing specs and navigation for UI, avatar, and other domain behavior

### Layer 4: Operations

- `docs/operations/`
- Purpose: repo workflow, doc governance, runbook references, task-system rules, and agent-facing operating guidance

## Source-Of-Truth Map

| Information type | Source of truth | Notes |
| --- | --- | --- |
| Repo summary and startup path | `README.md` | Entry point only; do not duplicate deep implementation detail here |
| Required runtime roots | `docs/roadmap.md` and `docs/migration/phase0.md` | Must stay aligned with `AGENTS.md` and actual code paths |
| Backend behavior | `local-backend/app/api/routes.py`, `local-backend/app/services/`, `local-backend/app/models/`, `local-backend/app/core/` | Code wins over docs |
| Unity UI behavior | `unity-client/Assets/Resources/UI/MainUI.uxml`, `unity-client/Assets/Resources/UI/Shell/AppShell.uxml`, `unity-client/Assets/Resources/UI/Styles/`, `unity-client/Assets/Scripts/` | Code wins over docs |
| Current architecture narrative | `docs/02-architecture.md` and `docs/architecture/` | Use for module ownership and layering, not design targets |
| Current UI narrative | `docs/04-ui.md` | Keep placeholders and manual-only areas explicit |
| Feature-specific avatar notes | `docs/features/README.md`, `docs/features/avatar-asset-spec.md`, `docs/features/avatar-asset-intake-checklist.md`, `docs/avatar-spec.md`, `docs/avatar-rig-cleanup.md` | Navigation lives under `docs/features/`; older spec files remain valid until intentionally moved or retired |
| Operations and workflow | `docs/operations/` plus `docs/09-runbook.md` | Keep setup, governance, and process docs here |
| AI task governance | `AGENTS.md`, `docs/operations/agent-workflow.md`, `tasks/task-template.md`, `tasks/task-queue.md`, `tasks/task-people.md` | `AGENTS.md` defines the repo-wide operating rules and `docs/operations/agent-workflow.md` defines the current task protocol |
| Architecture decision summary | `docs/06-decisions.md` | Short chronological log |
| Expanded architecture rationale | `docs/architecture/adr/` | Use when a one-line decision log is not enough |
| Design target UI | `unity-client/Assets/Resources/UI/ui_feature_map.md` | Design target only; never treat as current implementation proof |

## Docs Changed With Code

Update docs in the same task when the change affects any of the following:

- directory or module structure
- feature flow or user-visible behavior
- interface or contract between modules
- asset or avatar spec
- agent workflow, tracker workflow, or operating rules

Minimum expectation per task:

- update the current-state doc that describes the changed behavior
- update navigation docs when a new page or folder becomes part of the reading path
- update `docs/06-decisions.md` and an ADR when the change crosses the architecture gate in `docs/migration/phase0.md`
- update task trackers when scope, blockers, or done criteria changed

## Duplication Rule

- Do not let the same ownership claim live in several docs with different wording.
- Prefer one owner doc plus links from navigation pages.
- If an older page still exists for history or design-target reasons, label it clearly instead of silently duplicating current-state claims.
