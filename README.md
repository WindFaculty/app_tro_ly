# Local Desktop Assistant

Windows-first local assistant built from a Unity client, a FastAPI backend, and SQLite task data.

## Current State

- The active product in this repo is the local desktop assistant.
- The backend in `local-backend/` is implemented and tested.
- The Unity client in `ai-dev-system/clients/unity-client/` is implemented as a UI Toolkit shell with an orbit-style Home screen, a list-first Schedule screen, chat, settings, subtitle, reminder, and voice wiring.
- `agent-platform/` is not present in the current repo snapshot; if used as an adjacent subsystem elsewhere, it remains optional and not required for the assistant runtime.
- The non-backend unification under `ai-dev-system/` started from the freeze baseline in `docs/migration/ai-dev-system-unification-phase0.md` and now has landed context, client, control-plane, and domain slices.
- Phase 1 architecture scaffolding for that unification now exists under `ai-dev-system/`, and later phases have already moved current truth into `context/`, `clients/unity-client/`, `control-plane/`, and `domain/`.
- Phase 2 has now absorbed the former repo AI context root into `ai-dev-system/context/`.
- Phase 3 has now absorbed the former separate Unity client root into `ai-dev-system/clients/unity-client/`.
- Phase 4 moved the automation runtime under `ai-dev-system/control-plane/`, and Phase 9 removed the temporary root-level shim packages that had bridged the old import layout.
- Phase 5 has now established `ai-dev-system/domain/` as the shared contract root for avatar, customization, and room ownership while Unity runtime code remains under `ai-dev-system/clients/unity-client/`.
- Phase 6 now gives `ai-dev-system/workbench/` and `ai-dev-system/asset-pipeline/` real ownership over authoring inventories, naming guidance, and structure validation, while large authoring files and helper scripts still remain in their current root paths.
- The Mesh AI -> Blender -> validation -> Unity handoff foundation now lives under `ai-dev-system/asset-pipeline/`, `ai-dev-system/workbench/`, and `ai-dev-system/control-plane/` as lifecycle contracts, wrapper planning, and workflow specs that still point at the current root Blender helpers.
- A production-ready avatar experience is not fully wired end-to-end yet. The repo contains avatar groundwork and prototype assets under `ai-dev-system/clients/unity-client/Assets/AvatarSystem/`, but live behavior still depends on Unity scene setup and manual validation.

## Repo Layout

```text
.
|- local-backend/   FastAPI backend, SQLite persistence, AI orchestration, speech adapters
|- ai-dev-system/   Non-backend integration root in progress; automation subsystem, absorbed AI context, Unity client, and domain contracts
|- docs/            Product, architecture, API, runtime, test, and runbook docs
|- tasks/           AI-executable queue, manual blockers, and historical done log
|- scripts/         Windows setup, startup, packaging, and smoke helpers
`- agent-platform/  Optional adjacent tooling when present; not part of the assistant runtime
```

## Implemented Runtime

### Local backend

- FastAPI routes under `local-backend/app/api/routes.py`
- SQLite-backed task, reminder, conversation, memory, route-log, and settings storage
- One guarded assistant pipeline shared by `POST /v1/chat` and `WS /v1/assistant/stream`
- Deterministic task validation before any database mutation
- Routing between Groq fast responses, Gemini deep planning, and hybrid plan-then-fast delivery
- Speech adapters for faster-whisper or whisper.cpp and Piper or ChatTTS
- Health diagnostics with `ready`, `partial`, or `error` plus recovery actions

### Unity client

- UI Toolkit entrypoint at `ai-dev-system/clients/unity-client/Assets/Resources/UI/MainUI.uxml`
- Shell layout in `ai-dev-system/clients/unity-client/Assets/Resources/UI/Shell/AppShell.uxml`
- Screen controllers for Home, Schedule, Settings, and Chat
- REST plus WebSocket clients for health, tasks, settings, reminders, chat, and streaming assistant turns
- Subtitle overlay, reminder overlay, audio playback, transcript preview, and task summaries
- Placeholder avatar-state presentation plus optional scene-level `AvatarConversationBridge` integration

## Current Limitations

- The default LLM path is API-backed through Groq and Gemini. A fully local LLM path is not implemented.
- Unity visual behavior still requires Unity Editor or a built client for full verification.
- Some UI areas are still placeholder-driven, especially the Home avatar stage, the lack of a real calendar grid, and the shell-owned schedule-side helper panel.
- The mini-assistant window described in design docs is not implemented.
- Speech quality and availability still depend on machine-local runtime setup.
- Mesh AI handoff manifests do not mean Unity wardrobe or room registries are already complete; those remain planned or manual-gate work.

## Quick Start

### Windows helper flow

From the repo root:

```powershell
.\scripts\setup_windows.ps1
.\scripts\run_all.ps1
python .\scripts\smoke_backend.py
```

This is the recommended path for setup, health validation, and smoke checks.

### Manual backend start

```powershell
cd local-backend
python -m pip install -r requirements.txt
python run_local.py
```

Default backend URL:

- `http://127.0.0.1:8096`

### Unity client

Open `ai-dev-system/clients/unity-client/` in Unity and run from the Editor, or pass a built executable to:

```powershell
.\scripts\run_all.ps1 -UnityExecutablePath "D:\Builds\TroLy.exe"
```

## Key Runtime Configuration

The backend reads `.env` in `local-backend/` and shell environment variables prefixed with `assistant_`.

Common settings:

- `assistant_llm_provider`
- `assistant_routing_mode`
- `assistant_fast_provider`
- `assistant_deep_provider`
- `assistant_groq_api_key`
- `assistant_gemini_api_key`
- `assistant_stt_provider`
- `assistant_faster_whisper_model_path`
- `assistant_whisper_command`
- `assistant_whisper_model_path`
- `assistant_tts_provider`
- `assistant_piper_command`
- `assistant_piper_model_path`
- `assistant_chattts_compile`

Current backend-supported LLM providers are `hybrid`, `groq`, and `gemini`.
Ollama-related settings still exist for preflight and future work, but Ollama is not an active routed provider in the current backend code.

## Verification Status

- Historical backend verification was recorded on 2026-03-26 with `pytest -q`: `62 passed`.
- Latest backend terminal rerun on 2026-04-04 reported `69 passed, 1 failed`; the failing test is `tests/test_tts_service.py::test_chattts_synthesize_writes_cached_wav`.
- Latest Unity verification notes are tracked in `tasks/task-people.md`, including 2026-03-28 EditMode `21 passed`, 2026-03-28 PlayMode `33 passed`, and a 2026-03-29 PlayMode rerun with `35 passed`.

## Project Structure Overview

Start with [docs/roadmap.md](docs/roadmap.md) for the quickest repo map.
It summarizes the active runtime, main flow, module ownership, status labels, and where to change UI, backend, planner, chat, and avatar code.
Use it before diving into the deeper architecture, API, UI, runbook, or task-tracking docs.
If you need the proposed root-level migration plan, read [docs/migration/ai-dev-system-unification-phase0.md](docs/migration/ai-dev-system-unification-phase0.md) as the freeze baseline, not as shipped structure.
For the next architecture step that establishes the `ai-dev-system/` layer layout, read [docs/migration/ai-dev-system-unification-phase1.md](docs/migration/ai-dev-system-unification-phase1.md).
For the context-absorption step, read [docs/migration/ai-dev-system-unification-phase2.md](docs/migration/ai-dev-system-unification-phase2.md).
For the client-absorption step that moves the Unity project under `ai-dev-system/clients/`, read [docs/migration/ai-dev-system-unification-phase3.md](docs/migration/ai-dev-system-unification-phase3.md).
For the control-plane unification step that makes `ai-dev-system/control-plane/` the current automation runtime home, read [docs/migration/ai-dev-system-unification-phase4.md](docs/migration/ai-dev-system-unification-phase4.md).
For the domain pass that makes `ai-dev-system/domain/` the shared avatar or customization or room contract root, read [docs/migration/ai-dev-system-unification-phase5.md](docs/migration/ai-dev-system-unification-phase5.md).
For the workbench and asset-pipeline pass that establishes current inventory and validation ownership, read [docs/migration/ai-dev-system-unification-phase6.md](docs/migration/ai-dev-system-unification-phase6.md).
For the current Mesh AI -> Blender -> Unity handoff boundary, read [docs/architecture/mesh-ai-blender-unity-integration.md](docs/architecture/mesh-ai-blender-unity-integration.md).
For the scripts and tests standardization pass, read [docs/migration/ai-dev-system-unification-phase7.md](docs/migration/ai-dev-system-unification-phase7.md).
For the docs and task-governance rewrite, read [docs/migration/ai-dev-system-unification-phase8.md](docs/migration/ai-dev-system-unification-phase8.md).
For the architecture-lock cleanup that removes temporary import shims and stale absorbed-root references, read [docs/migration/ai-dev-system-unification-phase9.md](docs/migration/ai-dev-system-unification-phase9.md).

## Documentation Index

- [docs/index.md](docs/index.md)
- [docs/roadmap.md](docs/roadmap.md)
- [docs/migration/ai-dev-system-unification-phase0.md](docs/migration/ai-dev-system-unification-phase0.md)
- [docs/migration/ai-dev-system-unification-phase1.md](docs/migration/ai-dev-system-unification-phase1.md)
- [docs/migration/ai-dev-system-unification-phase2.md](docs/migration/ai-dev-system-unification-phase2.md)
- [docs/migration/ai-dev-system-unification-phase3.md](docs/migration/ai-dev-system-unification-phase3.md)
- [docs/migration/ai-dev-system-unification-phase4.md](docs/migration/ai-dev-system-unification-phase4.md)
- [docs/migration/ai-dev-system-unification-phase5.md](docs/migration/ai-dev-system-unification-phase5.md)
- [docs/migration/ai-dev-system-unification-phase6.md](docs/migration/ai-dev-system-unification-phase6.md)
- [docs/migration/ai-dev-system-unification-phase7.md](docs/migration/ai-dev-system-unification-phase7.md)
- [docs/migration/ai-dev-system-unification-phase8.md](docs/migration/ai-dev-system-unification-phase8.md)
- [docs/migration/ai-dev-system-unification-phase9.md](docs/migration/ai-dev-system-unification-phase9.md)
- [docs/00-context.md](docs/00-context.md)
- [docs/01-scope.md](docs/01-scope.md)
- [docs/02-architecture.md](docs/02-architecture.md)
- [docs/03-api.md](docs/03-api.md)
- [docs/04-ui.md](docs/04-ui.md)
- [docs/05-test-plan.md](docs/05-test-plan.md)
- [docs/07-ai-runtime.md](docs/07-ai-runtime.md)
- [docs/08-architecture-as-is.md](docs/08-architecture-as-is.md)
- [docs/09-runbook.md](docs/09-runbook.md)

## Task Tracking

- `tasks/task-queue.md`: current AI-executable repo work
- `tasks/task-people.md`: manual or off-repo work
- `tasks/done.md`: historical completion log and doc refresh history
