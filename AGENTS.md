# AGENTS.md

Applies to the entire repository.

Machine-facing operating rules for Codex and similar agents.

## Repo Goal

- `local-backend/`: source of truth for API behavior, task logic, routing, memory, speech adapters, scheduler, and persistence
- `unity-client/`: source of truth for client UI, screen flow, overlays, audio playback, and avatar presentation wiring
- `scripts/`: source of truth for Windows setup, startup, packaging, and backend smoke automation
- `docs/`: maintained documentation; may include both current-state docs and target-state design docs
- `tasks/`: work tracking
- `agent-platform/`: optional adjacent subsystem, not part of the required assistant runtime

## Module Boundaries

- Treat `local-backend/` and `unity-client/` as the required runtime roots.
- Do not invent a second competing source of truth for runtime behavior, asset ownership, or workflow rules.
- Use the repo truth sources below before making claims about behavior.

## Truth Sources

- Trust code over docs.
- Backend truth lives in:
  - `local-backend/app/api/routes.py`
  - `local-backend/app/services/`
  - `local-backend/app/models/`
  - `local-backend/app/core/`
- Unity UI truth lives in:
  - `unity-client/Assets/Resources/UI/MainUI.uxml`
  - `unity-client/Assets/Resources/UI/Shell/AppShell.uxml`
  - `unity-client/Assets/Resources/UI/Styles/*.uss`
  - `unity-client/Assets/Scripts/App/`
  - `unity-client/Assets/Scripts/Core/`
  - `unity-client/Assets/Scripts/Features/`
- Avatar integration truth lives in:
  - `unity-client/Assets/Scripts/Avatar/`
  - `unity-client/Assets/AvatarSystem/`
- Operations truth lives in:
  - `scripts/setup_windows.ps1`
  - `scripts/run_all.ps1`
  - `scripts/package_release.ps1`
  - `scripts/smoke_backend.py`

## Hard Prohibitions

- Do not invent features, routes, screens, runtime behavior, or architecture.
- Do not treat target-state design docs as implemented reality.
- Do not treat manual-only validation as already verified from terminal work.
- Do not overwrite or revert unrelated user changes.
- Do not change runtime code during a docs task unless a source comment is materially misleading.
- Do not present design-target docs as shipped features.
- Do not create a new file if an existing file already owns that role, unless the task scope explicitly requires the new file.

## Before Coding

1. Analyze the request and identify the exact truth sources.
2. Read the active task entry in `tasks/task-queue.md` when one already exists for the requested area.
3. If the request falls outside active scoped work during the freeze, record it in `tasks/task-queue.md` or `tasks/task-people.md` before changing code or docs.
4. Plan the change before editing.
5. Use `tasks/task-template.md` when creating or splitting AI-executable work so scope, validation, expected outputs, docs updates, and allowed files stay explicit.

## During Coding

- Execute only inside the confirmed scope.
- Keep queue files current-state focused.
- Keep `tasks/done.md` historical; add clarifying notes instead of rewriting history into current status.
- If docs conflict with code, update docs to match code.
- If code is ambiguous, say what is uncertain and avoid stronger claims.

## After Coding

1. Verify with tests, logs, script output, or concrete runtime evidence.
2. Update the relevant tracker and docs files in the same task.
3. Do not claim done without evidence.
4. Report remaining manual gates, risks, and out-of-scope follow-up.

## No-Guessing Rules

- Use `docs/operations/agent-workflow.md` as the current completion protocol reference.

## Documentation Labels

Use these distinctions consistently:

- `Current implementation`: behavior proven by code in this repo now
- `Planned work`: intended work not implemented yet
- `Optional subsystem`: present but not required for the assistant runtime
- `Manual validation required`: needs Unity Editor, a built client, external runtime binaries, credentials, or target-machine checks
- `Design target`: aspirational UI or architecture direction
- `Placeholder`: temporary UI, avatar, text, or runtime behavior

## Docs Rules

- Follow `docs/operations/documentation-governance.md` when changes affect structure, feature flow, contracts, asset specs, or workflow rules.
- Run through `docs/operations/doc-audit-checklist.md` before closing scoped work that changed repo truth.
- Keep docs concise, specific, and grounded in files that exist.

## Task File Rules

- Update `tasks/task-queue.md` when AI-executable repo work changes status, scope, blockers, or definition of done.
- Update `tasks/task-people.md` when a task requires a person, a target machine, Unity Editor interaction, external assets, credentials, or approvals.
- Update `tasks/done.md` when work is actually completed and there is verification or concrete evidence to justify the history entry.
- Use `tasks/task-template.md` when creating or splitting AI-executable work so scope, validation, docs updates, and allowed files stay explicit.

## Completion Report

Every completed slice should be able to say:

- what changed
- what stayed out of scope
- what evidence verified the change
- what docs and trackers were updated
- what manual gates or risks remain

## Phase 0 Freeze Rules

- During the modularization freeze, do not start net-new product features unless the task tracker explicitly scopes them as approved work.
- Allowed work during the freeze: boundary extraction, docs sync, tracker governance, validation automation, regression fixes, and placeholder-safe contracts needed for later phases.
- Defer design-target expansion, new runtime surfaces, root-level repo moves, and production-avatar claims until the relevant phased task and gate say otherwise.
- If a requested change falls outside the active task scope, record it in `tasks/task-queue.md` or `tasks/task-people.md` before changing code or docs.

## Architecture Gate

- Treat the following as gated changes: adding a new top-level module boundary, moving required runtime roots, changing persistence strategy, changing shared event-bus or state-ownership contracts, changing avatar asset structure or slot rules, or adding a second competing source of truth.
- A gated change is not complete unless it updates the relevant task tracker, the affected current-state docs, and `docs/06-decisions.md`.
- If the change also alters the migration guardrails themselves, update `docs/migration/phase0.md` in the same task.
