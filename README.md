# Local Desktop Assistant

Windows-first desktop assistant built around local task management, task-safe AI chat, voice I/O, reminders, and a Unity client shell.

## Current Status

- Product direction has been reset from the old Character Studio track to the local desktop assistant.
- Milestones M0 through M5 are complete.
- M6 polish and Windows packaging are in progress.
- The backend-side AI orchestration is implemented today:
  - `POST /v1/chat` and `WS /v1/assistant/stream` share one guarded task pipeline.
  - Fast and deep routing currently uses Groq plus Gemini.
  - Ollama remains in config as a future local path, but the current phase reports it as disabled.
- The Unity shell is functional, but the main avatar area is still a placeholder for future production asset integration.

## What Exists Today

- `local-backend/`
  - FastAPI backend
  - SQLite persistence
  - task, planner, reminder, chat, memory, routing, and speech services
  - health endpoint, logs, route logs, and recovery guidance
- `unity-client/`
  - Unity client project
  - runtime-generated assistant UI
  - task planner, chat panel, settings panel, reminder display, subtitle flow
- `docs/`
  - product scope, architecture, AI runtime, API, UI notes, decisions, and test plan
- `tasks/`
  - milestone tracking, AI/manual work split, and done log
- `agent-platform/`
  - optional adjacent service code, not the core assistant runtime

## AI Runtime Today

- Backend validation owns every task mutation before SQLite writes.
- Route selection can choose `groq_fast`, `gemini_deep`, or `hybrid_plan_then_groq`.
- Session state, rolling summaries, long-term memory items, and route logs are stored in SQLite.
- Voice sessions can stream transcript updates, assistant text chunks, and sentence-level TTS audio.
- Health diagnostics expose `ready`, `partial`, or `error` plus recovery actions for missing runtimes.

## Task Tracking

- `tasks/task-queue.md`: repo work Codex or AI can execute directly.
- `tasks/task-people.md`: manual or off-repo work that must be done by a person or target machine owner.
- `tasks/done.md`: completed milestones and documentation updates.

## MVP Focus

- Local task CRUD, complete, and reschedule flows
- Today, week, overdue, inbox, and completed task views
- Text chat and assistant streaming with task-aware actions
- Push-to-talk speech input
- Local TTS and subtitles
- Reminder events, planner summaries, and degraded-runtime guidance
- Windows-first delivery, with a fully local LLM path still pending beyond the current Groq or Gemini default

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

## Optional Runtime Configuration

The backend uses the `assistant_` environment variable prefix. Useful runtime variables include:

- `assistant_llm_provider`
- `assistant_routing_mode`
- `assistant_fast_provider`
- `assistant_deep_provider`
- `assistant_gemini_api_key`
- `assistant_gemini_base_url`
- `assistant_gemini_model`
- `assistant_groq_api_key`
- `assistant_groq_base_url`
- `assistant_groq_model`
- `assistant_stt_provider`
- `assistant_faster_whisper_model_path`
- `assistant_faster_whisper_model_size`
- `assistant_faster_whisper_device`
- `assistant_faster_whisper_compute_type`
- `assistant_whisper_command`
- `assistant_whisper_model_path`
- `assistant_piper_command`
- `assistant_piper_model_path`
- `assistant_tts_provider`
- `assistant_chattts_compile`
- `assistant_ollama_base_url`
- `assistant_ollama_model`

Configured LLM paths:

- Hybrid routing default: fast `groq`, deep `gemini`
- Groq: `https://api.groq.com/openai/v1`
- Gemini: `https://generativelanguage.googleapis.com/v1beta/openai`
- Ollama adapter: `http://127.0.0.1:11434` in config, but currently disabled in health checks for this phase

Configured speech defaults:

- Backend language: `vi`
- STT provider: `faster_whisper` with optional `whisper.cpp` fallback
- Default TTS provider: `piper`
- Backend TTS voice: `vi-VN-default`

ChatTTS notes:

- Set `assistant_tts_provider=chattts` to use the Python ChatTTS backend instead of Piper.
- `assistant_chattts_compile=false` is the safer Windows starting point.
- ChatTTS model download and warmup happen lazily on first synthesis call.

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
- `docs/07-ai-runtime.md`
- `docs/08-architecture-as-is.md`

## Notes

- The backend is implemented and tested in this workspace.
- The current default LLM path is not fully offline because it routes through Groq and Gemini.
- The Unity shell is implemented, but live validation still depends on opening the Unity project locally.
- Windows packaging helpers exist under `scripts/`, but final release-folder validation is still in progress.
- A real avatar or model asset is not checked in yet.
