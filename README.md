# Local Desktop Assistant

Windows-first desktop assistant built around task management, text chat, push-to-talk voice input, reminders, and a Unity client shell.

## Current Status

- Product direction has been reset from the old Character Studio track to the local desktop assistant.
- Milestones M0 through M5 are complete.
- M6 polish and Windows packaging are in progress.
- The app currently prioritizes assistant functions first. The center interaction area in the Unity shell is a placeholder for a future character/model integration.

## What Exists Today

- `local-backend/`
  - FastAPI backend
  - SQLite persistence
  - task, planner, reminder, chat, and speech adapter services
  - health endpoint, logs, and recovery guidance
- `unity-client/`
  - Unity client project
  - runtime-generated assistant UI
  - task planner, chat panel, settings panel, reminder display, subtitle flow
- `docs/`
  - product scope, architecture, API, UI notes, decisions, and test plan
- `tasks/`
  - milestone tracking and done log
- `agent-platform/`
  - optional adjacent service code, not the core assistant runtime

## MVP Focus

- Local task CRUD, complete, and reschedule flows
- Today, week, overdue, inbox, and completed task views
- Text chat with task-aware actions
- Push-to-talk speech input
- Local TTS and subtitles
- Reminder events and planner summaries
- Offline-first operation after setup

## Repo Layout

```text
.
|- local-backend/   FastAPI + SQLite backend
|- unity-client/    Unity desktop client
|- docs/            Product and technical documentation
|- tasks/           Milestones and work tracking
|- scripts/         Windows helper scripts for setup/startup/packaging
`- agent-platform/  Optional adjacent tooling, not required for MVP
```

## Quick Start

### 1. Install backend dependencies

```powershell
cd local-backend
python -m pip install -r requirements.txt
```

### 2. Run the local backend

```powershell
cd local-backend
python run_local.py
```

Backend default URL:

- API: `http://127.0.0.1:8096`

### 3. Open the Unity client

Open `unity-client/` in Unity and run the project from the editor, or create a Windows standalone build from that project.

## Optional Local Runtime Configuration

The backend uses the `assistant_` environment variable prefix. Useful runtime variables include:

- `assistant_llm_provider`
- `assistant_gemini_api_key`
- `assistant_gemini_base_url`
- `assistant_gemini_model`
- `assistant_groq_api_key`
- `assistant_groq_base_url`
- `assistant_groq_model`
- `assistant_whisper_command`
- `assistant_whisper_model_path`
- `assistant_piper_command`
- `assistant_piper_model_path`
- `assistant_enable_ollama`
- `assistant_ollama_base_url`
- `assistant_ollama_model`

Supported LLM providers:

- Gemini: `https://generativelanguage.googleapis.com/v1beta/openai`
- Groq: `https://api.groq.com/openai/v1`
- Ollama: `http://127.0.0.1:11434`

Default local runtime assumptions:

- Backend language: `vi`
- Backend TTS voice: `vi-VN-default`

## Tests

Backend tests:

```powershell
cd local-backend
pytest -q
```

Unity validation still needs to be run from the Unity Editor for EditMode and PlayMode coverage.

## Documentation

- `docs/00-context.md`
- `docs/01-scope.md`
- `docs/02-architecture.md`
- `docs/03-api.md`
- `docs/04-ui.md`
- `docs/05-test-plan.md`
- `docs/08-architecture-as-is.md`

## Notes

- The backend is implemented and tested in this workspace.
- The Unity shell is implemented, but live validation still depends on opening the Unity project locally.
- Windows packaging helpers exist under `scripts/`, but final release-folder validation is still in progress.
- A real avatar/model asset is not checked in yet.
