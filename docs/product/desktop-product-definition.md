# Desktop Product Definition

Status: Planned work  
Phase: A00 - Freeze desktop product definition  
Last updated: 2026-04-07

## Purpose

This document freezes the product definition for the standalone desktop app owned by Workstream A.

The target desktop product is a Windows 11 personal assistant built on:

- Tauri Desktop Host
- React/Vite Web UI
- FastAPI backend
- Local persistence using SQLite plus JSON state files

Unity is not part of the desktop app done criteria for Workstream A. The desktop app must be usable on its own before any real runtime sync with Unity begins.

## Current Implementation Baseline

Current implementation in this repo is still transition-state software:

- `local-backend/` is the current source of truth for backend business logic and persistence.
- `apps/unity-runtime/` is the current Unity room and avatar runtime.
- `apps/web-ui/` and `apps/desktop-shell/` already exist as rebuild scaffolds, not as the shipped product baseline.

Do not describe the planned desktop product below as already shipped behavior.

## Product Context Locked For Desktop 1.0

- Single-user product for personal use
- Primary platform: Windows 11
- AI runtime uses cloud APIs
- Voice mode v1 is push-to-talk only
- Visual direction: polished, soft anime-inspired desktop product
- Productivity surfaces focus on task and calendar management
- Local data must reopen on app restart
- Google is the first email and calendar provider
- Browser automation must be approval-first
- Wardrobe needs complete taxonomy and data foundations on the desktop side

## Product Outcome

Workstream A is complete only when the desktop app can do all of the following without requiring a real Unity runtime:

- boot as a standalone Windows desktop app
- restore local state after restart
- provide the full business UI in React
- run business logic through FastAPI
- keep local structured and lightweight state persisted
- expose clear settings, diagnostics, and recovery surfaces
- prepare wardrobe data and sync-ready contracts without live Unity sync

## Module Inventory

Desktop app 1.0 includes these top-level modules:

| Module | Primary owner | Product role |
| --- | --- | --- |
| Dashboard | React + Backend | daily overview, quick actions, startup status |
| Chat | React + Backend | primary assistant conversation channel |
| Planner | React + Backend | task and planning workflows |
| Reminders | React + Backend | reminder center and due-soon handling |
| Tags | React + Backend | task and note organization |
| Notes | React + Backend | personal note capture and retrieval |
| Calendar | React + Backend | Google Calendar views and scheduling |
| Email | React + Backend | Google email reading, drafting, and task extraction |
| Browser automation | React + Backend | bounded multi-step automation with approvals |
| Settings | React + Backend | preferences, providers, accounts, paths |
| Diagnostics | React + Backend | health, logs, recovery, data visibility |
| Wardrobe manager | React + Backend | taxonomy, registry, presets, compatibility |
| Session restore | Tauri + React + Backend | reopen local app state and recover previous session |

## Product Principles

- Tauri owns desktop lifecycle, process hosting, app data roots, and shell-level recovery.
- React owns all business UI and user-facing workflows.
- FastAPI owns business rules, orchestration, and persistence behavior.
- Unity does not own desktop business UI and is not required for Workstream A completion.
- External integrations must be mockable or disableable without breaking the app shell.
- Local data recovery and understandable failure states matter as much as feature breadth.

## Release Bar For Workstream A

Workstream A is ready for release review only when:

1. all thirteen desktop modules above exist behind the desktop shell
2. the app runs without a real Unity embed
3. session restore works for local user state
4. Google integrations can be configured, disabled, and diagnosed cleanly
5. browser automation stays approval-first and auditable
6. wardrobe data can be created, stored, and exported using sync-ready contracts

## Related Documents

- `docs/product/desktop-scope-v1.md`
- `docs/product/desktop-non-goals.md`
- `docs/roadmap.md`
- `tasks/task-queue.md`
