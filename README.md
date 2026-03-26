# Local Desktop Assistant

Windows-first local assistant built from a Unity client, a FastAPI backend, and SQLite task data.

## Current State

- The active product in this repo is the local desktop assistant.
- The backend in `local-backend/` is implemented and tested.
- The Unity client in `unity-client/` is implemented as a UI Toolkit shell with task, chat, settings, subtitle, reminder, and voice wiring.
- `agent-platform/` exists next to the assistant repo but is optional and not required for the assistant runtime.
- A production-ready avatar experience is not fully wired end-to-end yet. The repo contains avatar groundwork and prototype assets under `unity-client/Assets/AvatarSystem/`, but live behavior still depends on Unity scene setup and manual validation.

## Repo Layout

```text
.
|- local-backend/   FastAPI backend, SQLite persistence, AI orchestration, speech adapters
|- unity-client/    Unity client project and UI Toolkit shell
|- docs/            Product, architecture, API, runtime, test, and runbook docs
|- tasks/           AI-executable queue, manual blockers, and historical done log
|- scripts/         Windows setup, startup, packaging, and smoke helpers
`- agent-platform/  Optional adjacent tooling, not part of the assistant runtime
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

- UI Toolkit entrypoint at `unity-client/Assets/Resources/UI/MainUI.uxml`
- Shell layout in `unity-client/Assets/Resources/UI/Shell/AppShell.uxml`
- Screen controllers for Home, Schedule, Settings, and Chat
- REST plus WebSocket clients for health, tasks, settings, reminders, chat, and streaming assistant turns
- Subtitle overlay, reminder overlay, audio playback, transcript preview, and task summaries
- Placeholder avatar-state presentation plus optional scene-level `AvatarConversationBridge` integration

## Current Limitations

- The default LLM path is API-backed through Groq and Gemini. A fully local LLM path is not implemented.
- Unity visual behavior still requires Unity Editor or a built client for full verification.
- Some UI areas are still placeholder-driven, especially the schedule center panel and the schedule-side assistant panel.
- The mini-assistant window described in design docs is not implemented.
- Speech quality and availability still depend on machine-local runtime setup.

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

Open `unity-client/` in Unity and run from the Editor, or pass a built executable to:

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

- Backend automated tests were verified locally on 2026-03-26 with `pytest -q`: `62 passed`.
- Unity EditMode and PlayMode test files exist in `unity-client/Assets/Tests/`, but they were not executed from this terminal session.

## Documentation Index

- `docs/00-context.md`
- `docs/01-scope.md`
- `docs/02-architecture.md`
- `docs/03-api.md`
- `docs/04-ui.md`
- `docs/05-test-plan.md`
- `docs/07-ai-runtime.md`
- `docs/08-architecture-as-is.md`
- `docs/09-runbook.md`

## Task Tracking

- `tasks/task-queue.md`: current AI-executable repo work
- `tasks/task-people.md`: manual or off-repo work
- `tasks/done.md`: historical completion log and doc refresh history
