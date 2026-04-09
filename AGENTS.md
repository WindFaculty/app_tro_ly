# AGENTS.md

Applies to the entire repository.

Machine-facing operating rules for Codex and similar agents.

## Repo Summary

- `local-backend/`: source of truth for backend behavior and remains out of scope for the current non-backend unification
- `apps/unity-runtime/`: current source of truth for the Unity room, avatar, bridge, audio, and runtime-focused tests
- `ai-dev-system/`: current source of truth for the automation subsystem and planned integration root for the repo's non-backend system
- `scripts/`: current source of truth for Windows setup, startup, packaging, and backend smoke automation
- `docs/`: governance and maintained documentation
- `tasks/`: work tracking and evidence history

## Truth Sources

- Trust code over docs.
- Backend truth lives in:
  - `local-backend/app/api/routes.py`
  - `local-backend/app/services/`
  - `local-backend/app/models/`
  - `local-backend/app/core/`
- Unity runtime truth lives in:
  - `apps/unity-runtime/Assets/Scripts/App/`
  - `apps/unity-runtime/Assets/Scripts/Runtime/`
  - `apps/unity-runtime/Assets/Scripts/Audio/`
  - `apps/unity-runtime/Assets/Scripts/Avatar/`
- Avatar integration truth lives in:
  - `apps/unity-runtime/Assets/Scripts/Avatar/`
  - `apps/unity-runtime/Assets/AvatarSystem/`
- Shared avatar, customization, and room contract truth lives in:
  - `ai-dev-system/domain/avatar/`
  - `ai-dev-system/domain/customization/`
  - `ai-dev-system/domain/room/`
  - `ai-dev-system/domain/shared/`
- Asset workbench and pipeline governance truth lives in:
  - `ai-dev-system/workbench/`
  - `ai-dev-system/asset-pipeline/`
- Current `ai-dev-system/` automation truth lives in:
  - `ai-dev-system/control-plane/app/`
  - `ai-dev-system/control-plane/unity_integration/`
  - `ai-dev-system/control-plane/catalog/`
  - `ai-dev-system/control-plane/orchestrator/`
  - `ai-dev-system/control-plane/adapters/`
  - `ai-dev-system/control-plane/agents/`
  - `ai-dev-system/control-plane/executor/`
  - `ai-dev-system/control-plane/planner/`
  - `ai-dev-system/control-plane/memory/`
  - `ai-dev-system/control-plane/tools/`
  - `ai-dev-system/control-plane/mcp_client.py`
  - `ai-dev-system/workflows/`
  - `ai-dev-system/tests/`
  - `ai-dev-system/context/`
  - `ai-dev-system/app/`, `ai-dev-system/agents/`, `ai-dev-system/executor/`, `ai-dev-system/planner/`, `ai-dev-system/memory/`, `ai-dev-system/tools/`, and `ai-dev-system/mcp_client.py` only as compatibility shims
- Operations truth lives in:
  - `scripts/setup_windows.ps1`
  - `scripts/run_all.ps1`
  - `scripts/package_release.ps1`
  - `scripts/smoke_backend.py`
- Migration guidance lives in:
  - `docs/migration/ai-dev-system-unification-phase0.md`
  - `docs/migration/ai-dev-system-unification-phase1.md`
  - `docs/migration/ai-dev-system-unification-phase2.md`
  - `docs/migration/ai-dev-system-unification-phase3.md`
  - `docs/migration/ai-dev-system-unification-phase4.md`
  - `docs/migration/ai-dev-system-unification-phase5.md`
  - `docs/migration/ai-dev-system-unification-phase6.md`

## Required Workflow

1. Analyze the request and identify the exact truth sources.
2. Plan the change before editing.
3. Execute only inside the confirmed scope.
4. Verify with tests, logs, script output, or concrete runtime evidence.
5. Do not claim done without evidence.

## No-Guessing Rules

- Do not invent features, routes, screens, runtime behavior, or architecture.
- If docs conflict with code, update docs to match code.
- If code is ambiguous, say what is uncertain and avoid stronger claims.
- Do not treat target-state design docs as implemented reality.
- Do not treat planned migration directories as current runtime truth until code actually moves.
- Do not treat manual-only validation as already verified from terminal work.

## Documentation Labels

Use these distinctions consistently:

- `Current implementation`: behavior proven by code in this repo now
- `Planned work`: intended work not implemented yet
- `Optional subsystem`: present but not required for the assistant runtime
- `Manual validation required`: needs Unity Editor, a built client, external runtime binaries, credentials, or target-machine checks
- `Design target`: aspirational UI or architecture direction
- `Placeholder`: temporary UI, avatar, text, or runtime behavior

## Task File Rules

- Update `tasks/task-queue.md` when AI-executable repo work changes status, scope, blockers, or definition of done.
- Update `tasks/task-people.md` when a task requires a person, a target machine, Unity Editor interaction, external assets, credentials, or approvals.
- Update `tasks/done.md` when work is actually completed and there is verification or concrete evidence to justify the history entry.
- Keep queue files current-state focused.
- Keep `tasks/done.md` historical; add clarifying notes instead of rewriting history into current status.

## Safety Rules

- Do not overwrite or revert unrelated user changes unless the approved migration phase explicitly requires replacing that file.
- Do not change runtime code during a docs task unless a source comment is materially misleading.
- Do not present design-target docs as shipped features.
- Keep docs concise, specific, and grounded in files that exist.
