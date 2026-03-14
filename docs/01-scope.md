# Scope - Local Desktop Assistant

Updated: 2026-03-13

## Goals (MVP baseline)

### G1. Local assistant shell
- Show one avatar in the desktop app.
- Support avatar states: `idle`, `listening`, `thinking`, `talking`, `confirming`, `warning`, `greeting`.
- Support a main app window plus a smaller quick-access assistant mode.

### G2. Local task management
- Persist all task data locally in SQLite.
- Support task CRUD plus complete and reschedule flows.
- Support views for:
  - today
  - tomorrow
  - next 7 days
  - overdue
  - inbox
  - completed
- Support task status values:
  - `inbox`
  - `planned`
  - `in_progress`
  - `done`
  - `cancelled`
- Support task priority values:
  - `low`
  - `medium`
  - `high`
  - `critical`

### G3. Text and voice conversation
- Support text chat for task lookup and task-editing commands.
- Support push-to-talk voice input for MVP.
- Return assistant replies in text and local speech.
- Show subtitles and avatar state changes during voice output.

### G4. Planning and reminders
- Generate a daily summary from real task data.
- Generate a weekly summary for the next 7 days.
- Detect overdue work and near-term due items.
- Trigger basic local reminders inside the app.
- Flag obvious time conflicts and urgent work within the next 24 hours.

### G5. Local-first delivery
- Run on one Windows machine without cloud dependencies during normal use.
- Keep Ollama, whisper.cpp, and Piper behind local adapters.
- Ship a repeatable local startup flow for backend, runtimes, and the desktop app.

## Non-goals (MVP)
- Google Calendar sync
- Browser automation
- General autonomous agent behavior
- Multiple avatars or character packs
- Plugin marketplace or extension SDK
- Full desktop or OS control
- Wake word / always-listening mode
- Advanced viseme or phoneme lip sync
- Cloud account, auth, or multi-user features

## Constraints
- Windows-first runtime and packaging.
- Single-user, local machine, trusted environment.
- Push-to-talk only in MVP.
- One avatar only in MVP.
- Avatar lip sync uses audio amplitude only.
- Backend owns task logic, planner logic, and session state.
- Unity is a client for UI, rendering, animation, and playback, not the source of truth for business rules.

## Demo-ready Criteria
1. The desktop app opens and shows the avatar without crashing.
2. The app can load today's tasks and the next 7 days from the local backend.
3. The user can add, edit, complete, and reschedule tasks from the app.
4. Text chat can answer questions such as `What do I have today?` using real database state.
5. Voice input can create a transcript locally and return a reply with local TTS.
6. Reminder events can raise an in-app popup and optional spoken alert.
7. Closing and reopening the app keeps task and conversation state in local storage.
8. Core flows work without Internet access.
