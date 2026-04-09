# Architecture As-Is

Updated: 2026-04-09

This document is the implementation snapshot for the repo as it exists now.

## Repo Topology

- `local-backend/` is the active assistant backend.
- `apps/unity-runtime/` is the active Unity runtime project.
- `agent-platform/` is not present in the current repo snapshot; if used as an adjacent subsystem elsewhere, it remains optional and not required for the assistant runtime.
- `docs/` and `tasks/` now track the assistant rather than the older project direction.

## What Exists Today

### Backend

- FastAPI app with `/v1` routes
- SQLite persistence and repositories
- health endpoint with runtime diagnostics and recovery actions
- task CRUD, day or week or overdue or inbox or completed queries
- planner summaries derived from real task data
- shared assistant orchestration for REST and stream flows
- route logging, conversation history, rolling summaries, and long-term memory items
- reminder scheduler and event bus
- speech adapters for STT and TTS
- Windows setup, startup, packaging, and smoke helpers

### Unity runtime

- standalone room bootstrap under `Assets/Scripts/App/`
- room runtime, bridge, and registry services under `Assets/Scripts/Runtime/`
- avatar state, lip-sync, and conversation bridge support
- Unity-side audio playback and WAV helpers
- runtime-focused EditMode and PlayMode coverage

### Avatar groundwork

- `Assets/AvatarSystem/` contains runtime controllers, production-asset folders, validators, import tools, and prototype assets
- animation assets referenced by current avatar tasks are checked in
- the shell does not yet provide a fully finished production-avatar experience by itself

## What Is Still Partial

- Production-avatar integration still depends on Unity scene setup and manual validation.
- Room art, broader prop population, and production-avatar hookup remain partial.

## Verification Snapshot

- Historical backend verification was executed on 2026-03-26 in `local-backend/` with `pytest -q`: `62 passed`.
- Latest backend terminal rerun for the Phase 0 baseline was executed on 2026-04-04 in `local-backend/` with `python -m pytest -q`: `69 passed, 1 failed`.
- The current failing test is `tests/test_tts_service.py::test_chattts_synthesize_writes_cached_wav`.
- Latest Unity verification notes remain tracker-backed evidence from 2026-03-28 and 2026-03-29; Unity Editor or built-client execution was not re-run in this terminal session.

## Adjacent Or Optional Code

- `agent-platform/` may still be useful later when present, but it is not part of the required assistant runtime.
- Ollama-related settings and preflight checks exist, but the current backend does not route live assistant requests to Ollama.

## Practical Reading Order

If you need current implementation truth, read in this order:

1. `local-backend/app/api/routes.py`
2. `local-backend/app/container.py`
3. `local-backend/app/services/`
4. `apps/unity-runtime/Assets/Scripts/App/`
5. `apps/unity-runtime/Assets/Scripts/Runtime/`
6. `apps/unity-runtime/Assets/Scripts/Avatar/`
7. `apps/unity-runtime/Assets/AvatarSystem/`
