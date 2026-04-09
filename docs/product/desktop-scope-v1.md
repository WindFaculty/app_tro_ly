# Desktop Scope v1

Status: Planned work  
Phase: A00 - Freeze desktop product definition  
Last updated: 2026-04-07

## Scope Statement

Desktop app v1 is the standalone business application delivered by Workstream A.

It must:

- run as a Windows desktop app through Tauri
- render its business UI through React
- use the existing FastAPI backend as the business logic root
- persist structured data in SQLite
- persist session, config, UI, and lightweight snapshot state in JSON files
- remain usable without a real Unity runtime

It must not depend on Workstream B runtime completion in order to be considered done.

## Current Baseline Versus Planned Scope

Current implementation already has repo-side evidence for:

- backend health, chat, task, settings, speech, and assistant-stream routes under `local-backend/app/api/routes.py`
- early React shell pages under `apps/web-ui/`
- early desktop host scaffolding under `apps/desktop-shell/`

Planned work still required for desktop v1 includes:

- full desktop shell lifecycle and session restore
- the complete React business UI
- Google email and calendar integration
- notes, tags, diagnostics, and wardrobe manager modules
- approval-first browser automation
- release hardening and packaging

## Module Acceptance Criteria

| Module | In-scope behavior for v1 | Minimum done criteria |
| --- | --- | --- |
| Dashboard | show startup state, day summary, quick links, and important alerts | user can open the app and understand system state plus next actions without entering chat |
| Chat | support conversation threads, streaming states, retries, search, and action cards | chat is a reliable primary assistant channel and can trigger real desktop actions |
| Planner | support task CRUD plus today, week, inbox, overdue, and completed views | daily work can be managed without needing chat for basic operations |
| Reminders | show scheduled reminders, due-soon items, and reminder actions | reminder surfaces are visible, actionable, and persisted locally |
| Tags | create and assign tags across planner and notes workflows | tags can organize, filter, and narrow personal work views |
| Notes | support quick notes, richer note detail, and note links to tasks or chat | user can capture and retrieve personal information outside task flows |
| Calendar | read and update Google Calendar data plus show agenda views | user can manage real calendar events from the desktop app |
| Email | read, search, draft, send, and convert Google email into tasks | user can manage practical email workflows without leaving the app |
| Browser automation | model multi-step actions with approvals, logs, cancel, and recovery notes | automation never runs opaque actions without user review and audit visibility |
| Settings | manage app preferences, voice, providers, Google auth state, and data paths | settings are understandable, persisted, and safe to change or reset |
| Diagnostics | show backend health, integration status, logs, local data paths, and cleanup options | app failures can be diagnosed and recovery steps are visible inside the product |
| Wardrobe manager | manage slot taxonomy, item registry, outfit presets, compatibility, and import or export | desktop side has enough structured wardrobe data to sync later without redesign |
| Session restore | reopen window state, filters, selected views, recent context, and local app preferences | a normal restart restores the local working session without silent data loss |

## Cross-Module Requirements

The following requirements apply across the whole desktop scope:

- React is the only owner of new business UI.
- Local data must auto-save and auto-restore across normal restarts.
- All major integrations need disable, retry, and failure messaging paths.
- The desktop shell must remain understandable when backend health is partial or failed.
- Voice stays push-to-talk for v1.
- Unity integration is represented only as a placeholder or future boundary during Workstream A.

## Out Of Scope For This Scope File

This file does not define:

- the standalone Unity room runtime scope owned by Workstream B
- post-A15 and post-B11 integration sync behavior
- mini assistant mode
- deep 3D interactions

Those concerns belong to later phases and separate workstreams.
