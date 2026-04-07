# Project Context

Last updated: 2026-03-26

## Project

- Name: Local Desktop Assistant
- Purpose: a Windows-first assistant for local task management, guarded AI chat, reminders, and voice I/O
- Runtime shape: Unity client + Python local backend + SQLite
- Active LLM path: Groq fast responses, Gemini deep planning, and hybrid routing
- Speech path: faster-whisper or whisper.cpp for STT, Piper or ChatTTS for TTS
- Optional adjacent subsystem when present: `agent-platform/`

## Current Implementation

- The backend is implemented in `local-backend/` and owns task logic, orchestration, routing, memory, reminders, settings, and persistence.
- The Unity client is implemented in `ai-dev-system/clients/unity-client/` as a UI Toolkit shell loaded from `Assets/Resources/UI/MainUI.uxml`.
- Current top-level client screens are Home, Schedule, and Settings.
- Current task views exposed through the client are Today, Week, Inbox, and Completed.
- Current voice flow supports microphone capture, STT, streamed or compatibility chat, TTS playback, subtitle overlay, and reminder overlay.
- Avatar presentation is only partially production-ready. The app has placeholder avatar-state UI plus optional integration points for `Assets/AvatarSystem/`.

## Current Phase

- Implementation is active.
- Focus is on polish, validation, runtime hardening, documentation, and production-avatar integration planning.

## Current Focus Areas

- Keep backend contracts explainable and deterministic.
- Keep Unity UI honest about partial, degraded, and error states.
- Reduce stale documentation and stale queue items.
- Preserve the assistant runtime as separate from optional adjacent tooling.

## Target Outcomes

These are product goals, not all of them are implemented in the current repo state:

- A polished desktop assistant shell with a stronger avatar experience
- Reliable local task CRUD, summaries, reminders, and chat-assisted planning
- Clear degraded-mode behavior when optional runtimes are missing
- Better packaging and repeatable Windows validation
- Optional future directions such as a compact mini-assistant mode and a fuller avatar presentation layer

## Risks And Constraints

- The current default LLM path is not fully offline.
- Speech runtime reliability depends on machine-local installs and models.
- Unity client behavior still requires Unity Editor or built-client validation.
- Some design docs describe target-state UI rather than implemented UI.

## Evidence Snapshot

- Backend automated tests were run on 2026-03-26 in `local-backend/` with `pytest -q`: `62 passed`.
- Unity EditMode and PlayMode test files exist, but they were not run from this terminal session.
