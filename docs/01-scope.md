# Scope - Local Desktop Assistant

Updated: 2026-03-26

## Current Implementation Baseline

This section describes what the repo currently implements.

### Implemented now

- Local SQLite task storage
- Task create, update, complete, and reschedule APIs
- Today, week, overdue, inbox, and completed task queries
- Text chat through `POST /v1/chat`
- Streaming assistant turns through `WS /v1/assistant/stream`
- Push-to-talk style microphone capture in the Unity client
- Subtitle overlay, reminder overlay, settings panel, and task summaries in the Unity client
- Health diagnostics and degraded-mode messaging

### Partially implemented now

- Avatar behavior
  - Placeholder avatar-state presentation is live in the client
  - `Assets/AvatarSystem/` contains runtime and production-asset groundwork, but end-to-end scene integration still needs manual validation
- Speech runtime support
  - Adapter code is implemented
  - End-to-end quality still depends on machine-local binaries, models, and environment setup
- Settings
  - Backend settings storage exists
  - The current client edits only a subset of available settings groups

## MVP Product Goals

These remain valid product goals even where the current client is still partial.

### G1. Assistant shell

- Show one assistant in the desktop app
- Keep Home, Schedule, and Settings understandable without hidden state
- Surface `ready`, `partial`, and `error` runtime health clearly

### G2. Local task management

- Keep all task data local in SQLite
- Support task CRUD, completion, and reschedule flows
- Support summaries and views for today, week, overdue, inbox, and completed work

### G3. Text and voice conversation

- Support text chat for lookup and safe task actions
- Support microphone capture for voice turns
- Return assistant replies as text and optional speech
- Keep subtitles and avatar state transitions aligned with playback

### G4. Planning and reminders

- Generate daily, weekly, overdue, urgency, and free-time summaries from real task data
- Trigger local reminder events in the app
- Preserve backend ownership of planning and reminder logic

### G5. Local-first delivery

- Run on a single Windows machine
- Keep task logic, routing, and speech adapters behind the local backend
- Provide repeatable setup, startup, packaging, and smoke-check scripts

## Planned But Not Implemented

- Compact mini-assistant mode
- Fully local LLM routing as the main default path
- Fully finished production-avatar experience
- Rich schedule visualization beyond the current placeholder calendar panel
- Design-target UI details from `unity-client/Assets/Resources/UI/ui_feature_map.md`

## Non-Goals For Current Scope

- Calendar sync
- Browser automation
- Desktop control
- Wake-word mode
- Multi-user auth
- Plugin marketplace
- Cross-device sync

## Scope Rules

- Backend code is the source of truth for task and assistant behavior.
- Design docs may describe future UI direction, but they are not proof of implementation.
- Anything requiring Unity Editor interaction, external credentials, or machine-local runtimes must be labeled as manual validation required.
