# Local Storage Strategy

Status: Design target with current implementation notes  
Phase: A01 - Reset desktop architecture  
Last updated: 2026-04-07

## Purpose

This document locks the local storage strategy for the desktop rebuild:

- SQLite for structured business data
- JSON for session, config, UI state, wardrobe registry, and lightweight snapshots

## Current Implementation Evidence

Current backend storage behavior is already proven in code:

- `local-backend/app/core/config.py` resolves `data_dir`, `db_path`, `audio_dir`, `cache_dir`, and `log_dir`
- `local-backend/app/db/repository.py` initializes SQLite tables for tasks, notes, conversations, reminders, settings, session state, assistant sessions, summaries, memory, and route logs
- `local-backend/app/container.py` creates the repository during backend startup

Current default path behavior keeps backend data under:

```text
local-backend/data/
|- app.db
|- audio/
|- cache/
`- logs/
```

Current implementation note:

- `app_settings` and `session_state` are still stored in SQLite today
- that is current behavior, not the locked target split for future phases
- the desktop rebuild shell now also owns separate JSON restore files in `apps/desktop-shell/src-tauri/src/persistence.rs` for shell session restore and host-level recovery state

## Target Ownership

The target desktop app will centralize local paths under a Tauri-owned app data root.

Target ownership:

- Tauri owns root path resolution
- backend owns SQLite and structured data writes
- React owns only in-memory UI state plus requests to persist through backend or host interfaces
- JSON restore or preference files belong to the desktop persistence layer, not ad-hoc page code

## Target Directory Layout

Planned target layout:

```text
<app-data-root>/
|- data/
|  `- app.db
|- state/
|  |- session-state.json
|  |- window-state.json
|  `- runtime-snapshot.json
|- ui/
|  |- theme-state.json
|  `- filters.json
|- config/
|  `- app-preferences.json
|- wardrobe/
|  `- registry.json
|- cache/
|  |- audio/
|  `- backend/
|- logs/
|- exports/
`- imports/
```

Current implementation note:

- `apps/desktop-shell/src-tauri/src/persistence.rs` now creates and maintains `state/session-state.json`, `state/window-state.json`, `state/runtime-snapshot.json`, `ui/theme-state.json`, `ui/filters.json`, and `config/app-preferences.json` under the Tauri app data root
- `apps/web-ui/src/App.tsx` plus `apps/web-ui/src/components/ShellLayout.tsx` now restore and persist shell route or runtime context through those host-managed JSON files
- `apps/web-ui/src/pages/SettingsPage.tsx` plus the host reset command in `apps/desktop-shell/src-tauri/src/lib.rs` can now rebuild those desktop restore files back to defaults as a recovery action
- browser preview uses `localStorage` as a development-only fallback through `apps/web-ui/src/services/runtimeHost.ts`; it is not the desktop source of truth

## SQLite Scope

SQLite is reserved for structured business records and durable history.

### Current implementation already in SQLite

- tasks
- notes
- task occurrences
- conversations
- messages
- reminders
- conversation summaries
- memory items
- route logs

### Planned target additions for SQLite

- calendar cache
- email cache metadata
- automation history

### Planned target migrations out of SQLite

Current SQLite keys that should move to JSON-backed storage in the rebuild:

- `app_settings`
- `session_state`
- lightweight assistant session restore data that belongs to desktop restore state rather than domain history

## JSON Scope

JSON files are reserved for lightweight restore state, config-style preferences, and sync-ready registries.

Target JSON categories:

- session restore state
- window and layout state
- theme and filter state
- app preferences
- wardrobe registry and sync-ready manifest snapshots
- lightweight runtime snapshots for recovery

JSON files must be:

- versioned
- small enough to inspect or repair manually
- validated before use
- recoverable by falling back to defaults when corrupted

## Versioning And Recovery Rules

The target storage rules are:

1. SQLite schema changes use explicit migrations or version markers.
2. JSON files include a schema version field.
3. Broken JSON files are quarantined or replaced with defaults instead of crashing startup.
4. Backup snapshots are optional but must never silently overwrite the primary store during recovery.
5. Export and import formats stay separate from internal restore files.

## Config And Secrets Note

Provider and runtime configuration currently comes from backend environment settings in `local-backend/app/core/config.py`.

A01 locks only the ownership rule:

- runtime provider config belongs to backend or host configuration surfaces
- React must not become the source of truth for provider secrets

Detailed credential handling remains planned work for later implementation phases.

## Related Documents

- `docs/architecture/desktop-target.md`
- `docs/architecture/desktop-runtime-flow.md`
- `docs/adr/0003-local-persistence-split.md`
