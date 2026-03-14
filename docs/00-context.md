# Project Context

Last Updated: 2026-03-14

## Project

- Name: Local Desktop Assistant
- One-liner: A Windows-first desktop assistant with a task-safe backend AI layer, an interactive avatar shell, local task planning, text chat, and voice I/O.
- Product focus: task manager first, avatar as interface layer, and AI as a guarded orchestration layer around real task data.
- Current implementation stack: Unity client + Python local backend + SQLite + Groq or Gemini routing + faster-whisper or whisper.cpp + Piper or ChatTTS
- Deferred local-only LLM path: Ollama remains in config, but is disabled in the current phase

## Current Phase

- Phase: IMPLEMENTATION ACTIVE / M6 POLISH IN PROGRESS

## Current Focus

- Harden the implemented Unity plus backend flows for Windows startup, settings, recovery UX, and packaging.
- Keep the AI contract explainable: validated task actions, route logging, memory summaries, and graceful fallback.
- Keep docs and task tracking aligned with the actual implementation state.

## Target Product Outcomes

- Show one assistant avatar in the desktop app, with a main window and a smaller quick-access mode.
- Let the user review today, tomorrow, next 7 days, overdue, inbox, and completed work from local task data.
- Support create, update, complete, and reschedule task flows by text and by voice.
- Return text plus speech output with animation hints for the avatar.
- Keep task logic authoritative in the backend, even when model providers are unavailable or degraded.

## Implemented Runtime Topology

- Unity client:
  - avatar rendering, animation, subtitle, audio playback, task UI, chat UI
- Python local backend:
  - task service
  - planner summaries
  - assistant orchestration
  - intent validation
  - route selection
  - memory
  - scheduler
  - speech adapters
  - SQLite persistence
- Configured AI and speech runtimes:
  - Groq at `https://api.groq.com/openai/v1`
  - Gemini at `https://generativelanguage.googleapis.com/v1beta/openai`
  - faster-whisper as the default STT path
  - whisper.cpp as the optional STT fallback path
  - Piper or ChatTTS as the TTS runtime
  - Ollama at `http://127.0.0.1:11434` as a future local path, not the active default

## Milestone Snapshot

- M0 Product reset and docs rewrite: DONE
- M1 Foundation skeleton: DONE
- M2 Task engine and calendar views: DONE
- M3 Chat and task-aware reasoning: DONE
- M4 Voice and avatar behavior: DONE
- M5 Reminder and planner: DONE
- M6 Polish and Windows packaging: DOING

## Main Risks

- The current default LLM path is not fully offline because it depends on Groq and Gemini.
- The local voice stack on Windows can be fragile across different machines.
- Natural-language task editing must go through structured actions, not direct free-form model writes.
- The repo still contains legacy or adjacent subprojects that do not implement the assistant runtime.
- Packaging multiple optional runtimes is harder than building the happy-path app.

## Environments

- Local backend API: `http://127.0.0.1:8096`
- Configured Ollama base URL: `http://127.0.0.1:11434`
- Optional agent-platform operator layer: `http://127.0.0.1:8088`
- Unity client: Windows standalone app or local editor play mode
