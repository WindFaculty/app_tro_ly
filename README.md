# Local Desktop Assistant

Windows-first local assistant with a FastAPI backend, a desktop rebuild under `apps/`, and a Unity runtime-only project under `apps/unity-runtime/`.

## Current State

- `local-backend/` is the current backend source of truth.
- `apps/unity-runtime/` is the only live Unity project in the repo.
- Unity now owns room, avatar, bridge, audio, and runtime-focused tests only. Legacy Unity business UI and UI Toolkit shell code have been retired from the live project.
- `apps/web-ui/` and `apps/desktop-shell/` remain the current desktop rebuild surfaces.
- `ai-dev-system/control-plane/unity_integration/` is the current Unity automation and environment-probing source of truth.
- `ai-dev-system/domain/` owns shared avatar, customization, and room contract documentation.

## Repo Layout

```text
.
|- apps/
|  |- desktop-shell/   Tauri desktop host
|  |- unity-runtime/   Unity room, avatar, bridge, audio, tests
|  `- web-ui/          React business UI rebuild
|- local-backend/      FastAPI backend, SQLite persistence, AI orchestration, speech adapters
|- ai-dev-system/      control plane, domain docs, workbench, asset pipeline, validation
|- docs/               governance and architecture docs
|- tasks/              active queue, manual gates, historical log
`- scripts/            Windows setup, startup, packaging, and smoke helpers
```

## Implemented Runtime

### Backend

- FastAPI routes under `local-backend/app/api/routes.py`
- SQLite-backed task, reminder, conversation, memory, route-log, and settings storage
- Shared assistant orchestration for `POST /v1/chat` and `WS /v1/assistant/stream`
- Deterministic task validation before mutation
- Groq or Gemini or hybrid routed assistant delivery
- STT and TTS adapters plus health diagnostics

### Unity runtime

- Bootstrap and room composition under `apps/unity-runtime/Assets/Scripts/App/`
- Room, bridge, registry, animation, and runtime services under `apps/unity-runtime/Assets/Scripts/Runtime/`
- Avatar state and lip-sync foundations under `apps/unity-runtime/Assets/Scripts/Avatar/` and `apps/unity-runtime/Assets/AvatarSystem/`
- Unity-side audio playback and WAV support under `apps/unity-runtime/Assets/Scripts/Audio/`
- Runtime-focused EditMode and PlayMode tests under `apps/unity-runtime/Assets/Tests/`

## Current Limitations

- Unity visuals still require Unity Editor or a built runtime for full validation.
- Production avatar hookup and broader room art remain partial and manual-validation-heavy.
- The desktop rebuild is not yet the single shipped end-user runtime.
- Speech quality still depends on machine-local runtime setup.

## Quick Start

From the repo root:

```powershell
.\scripts\setup_windows.ps1
.\scripts\run_all.ps1
python .\scripts\smoke_backend.py
```

Open `apps/unity-runtime/` in Unity `6000.4.1f1` when Unity runtime validation is needed.

## Project Structure Overview

Start with [docs/roadmap.md](docs/roadmap.md) for the current repo map, then use [docs/index.md](docs/index.md) for deeper architecture, runtime, validation, and migration context.

## Task Tracking

- `tasks/task-queue.md`: current AI-executable repo work
- `tasks/task-people.md`: manual or off-repo work
- `tasks/done.md`: historical completion log
