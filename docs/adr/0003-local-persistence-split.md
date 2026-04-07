# ADR 0003: Local Persistence Split

Status: Accepted  
Date: 2026-04-07

## Context

Current backend persistence is SQLite-first and already stores both structured records and some lighter-weight app state.

The rebuild requires a cleaner split:

- SQLite for structured data
- JSON for session, config, UI state, wardrobe registry, and lightweight snapshots

## Decision

- SQLite remains the durable store for structured domain records and history.
- JSON files become the target store for session restore, window state, theme, filters, app preferences, wardrobe registry, and lightweight runtime snapshots.
- Tauri owns root path resolution for these stores in desktop mode.
- Backend migrations and JSON versioning must be explicit.

## Consequences

- Some current SQLite-backed state such as `app_settings` and `session_state` is expected to move in later implementation phases.
- Startup recovery must handle corrupted JSON gracefully.
- Export or import files stay separate from internal restore files.
