# Project Context

Last Updated: 2026-03-14

## Project
- Name: Local Desktop Assistant
- One-liner: A Windows-first, local-only desktop assistant with an interactive avatar, task planning, text chat, and voice I/O.
- Product focus: task manager first, avatar as interface layer, chat and voice as control surfaces.
- Target stack: Unity client + Python local backend + SQLite + Ollama + whisper.cpp + Piper

## Current Phase
- Phase: IMPLEMENTATION ACTIVE / M6 POLISH IN PROGRESS

## Current Focus
- Keep the runtime fully local and offline after setup.
- Harden the implemented Unity + backend flows for Windows startup, settings, and packaging.
- Keep docs and task tracking aligned with the actual implementation state.

## Target Product Outcomes
- Show one assistant avatar in the desktop app, with a main window and a smaller quick-access mode.
- Let the user review today, tomorrow, next 7 days, overdue, inbox, and completed work from local task data.
- Support create, update, complete, and reschedule task flows by text and by voice.
- Return text plus local speech output with animation hints for the avatar.
- Keep all core flows usable without Internet access.

## Planned Runtime Topology
- Unity client:
  - avatar rendering, animation, subtitle, local audio playback, task UI, chat UI
- Python local backend:
  - task service, conversation orchestration, planner, scheduler, speech adapters, SQLite persistence
- Local runtimes:
  - Ollama at `http://127.0.0.1:11434`
  - whisper.cpp as a local process or wrapped service
  - Piper as a local CLI or local speech server

## Milestone Snapshot
- M0 Product reset and docs rewrite: DONE
- M1 Foundation skeleton: DONE
- M2 Task engine and calendar views: DONE
- M3 Chat and task-aware reasoning: DONE
- M4 Voice and avatar behavior: DONE
- M5 Reminder and planner: DONE
- M6 Polish and Windows packaging: DOING

## Main Risks
- The local voice stack on Windows can be fragile across different machines.
- Natural-language task editing must go through structured actions, not direct free-form model writes.
- The repo still contains legacy subprojects that do not implement the new assistant yet.
- Packaging multiple offline runtimes is harder than building the happy-path app.

## Environments
- Planned local backend API: `http://127.0.0.1:8096`
- Planned Ollama runtime: `http://127.0.0.1:11434`
- Optional agent-platform operator layer: `http://127.0.0.1:8088`
- Unity client: Windows standalone app or local editor play mode
