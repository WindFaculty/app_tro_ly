# Desktop Target Architecture

Status: Design target  
Phase: A01 - Reset desktop architecture  
Last updated: 2026-04-07

## Purpose

This document locks the target architecture for the standalone desktop app owned by Workstream A.

It defines who owns what across:

- Tauri desktop host
- React/Vite web UI
- FastAPI backend
- shared contracts
- local persistence boundaries
- Google integrations
- browser automation

## Current Implementation Baseline

Current implementation in this repo is still a transition state:

- `local-backend/` is the current source of truth for backend logic, SQLite persistence, scheduler, speech, and assistant orchestration.
- `apps/unity-runtime/` is the current Unity room and avatar runtime.
- `apps/desktop-shell/` now contains the repo-side Tauri host with backend process ownership, startup status plus retry entry points, window controls, Unity runtime inspection, Unity bridge state, JSON-backed desktop restore files under the app data root, bundle-resource staging for packaged builds, and the current desktop hardening surface in `src-tauri/tauri.conf.json` plus `src-tauri/capabilities/default.json`.
- `apps/web-ui/` now contains the React startup shell, desktop window chrome, runtime status surfaces, route auto-restore, a shared design system, and module-shell page framing on top of the typed backend plus Tauri plus Unity bridge adapters.
- `packages/contracts/` already contains typed Tauri command or event names plus typed Unity bridge envelopes, including desktop restore-state surfaces.

Nothing in this file should be read as shipped behavior unless the code already proves it elsewhere.

## Target Stack

The locked desktop architecture is:

```text
Tauri Desktop Host
|- React/Vite Web UI
|- FastAPI Backend
`- Unity Runtime (not required for A-series completion)
```

## Ownership Matrix

| Layer | Owns | Does not own |
| --- | --- | --- |
| Tauri desktop host | process lifecycle, app paths, window lifecycle, startup gating, recovery entry points, Unity sidecar lifecycle, host events | business UI, business rules, domain persistence |
| React web UI | navigation, layout, pages, interaction flows, feature state, optimistic UI, browser preview support | process hosting, direct Unity transport, domain persistence rules |
| FastAPI backend | business rules, AI routing, tasks, reminders, chat orchestration, settings APIs, memory, speech, integration adapters, data persistence | desktop window lifecycle, React layout, Unity presentation |
| `packages/contracts/` | typed cross-layer command and event shapes, stable names for Tauri and Unity bridge payloads | runtime storage, UI rendering, business logic |
| Unity runtime | room, avatar, animation, lip sync, camera, wardrobe presentation, 3D interaction after sync | desktop shell UI, planner or notes or email or settings business UI |

## Boundary Rules Locked By A01

### Tauri host

Tauri is the only desktop entry point and owns:

- launching and health-checking the backend
- resolving runtime paths for data, cache, logs, and exports
- exposing runtime facts to React through typed commands and events
- deciding whether Unity is absent, inspectable, launchable, or running
- session restore entry orchestration

Tauri does not proxy normal business API traffic. React calls the backend directly over HTTP and WebSocket.

### React web UI

React owns:

- desktop shell layout and all business pages
- feature workflows for dashboard, chat, planner, reminders, tags, notes, calendar, email, settings, diagnostics, and wardrobe manager
- UI state and session restore state above the domain layer
- typed calls to the backend and typed host calls to Tauri

React does not own:

- backend process management
- domain storage semantics
- direct Unity transport without going through Tauri contracts

### FastAPI backend

FastAPI remains the business logic root and owns:

- task, reminder, conversation, memory, and route history behavior
- speech and AI orchestration
- Google integration adapters
- browser automation orchestration and approval-first safety policy
- persistence behavior for structured records

The backend remains reusable from React without a Unity dependency.

### Shared contracts

`packages/contracts/` is the target home for typed cross-layer contracts.

Current implementation already stores:

- Tauri command names and event names in `packages/contracts/src/tauri.ts`
- Unity bridge command and event envelopes in `packages/contracts/src/unity.ts`

Future contract groups should expand here instead of ad-hoc local models.

### Unity runtime

Unity is an optional parallel runtime during Workstream A.

For A-series architecture purposes:

- Unity may be inspected or launched by the host
- Unity may expose typed bridge readiness or status
- Unity must not be required for desktop feature completeness
- Unity must not receive new business UI work

## Integration Boundaries

### Google integrations

Google email and calendar integrations belong to backend adapters plus React feature surfaces.

Boundary rules:

- React owns auth surfaces, settings copy, and module UI
- backend owns provider calls, token handling, sync behavior, and disable or retry logic
- desktop runtime must stay usable when Google integrations are not configured

### Browser automation

Browser automation belongs to backend orchestration plus React approval UI.

Boundary rules:

- React owns action review, approval, progress, audit views, and failure messaging
- backend owns bounded execution plans, safety checks, execution logs, and cancel or recovery semantics
- Tauri may host OS-level affordances later, but it does not own automation business logic

## Config Strategy

The target config strategy is split by ownership:

| Config type | Owner | Target source |
| --- | --- | --- |
| desktop runtime paths and host lifecycle flags | Tauri | host-resolved runtime config and app data paths |
| backend provider and service config | FastAPI backend | environment-driven settings resolved by `local-backend/app/core/config.py` |
| browser preview backend URL | React | explicit `VITE_*` preview config only |
| typed command or event names | shared contracts | `packages/contracts/` |
| user preferences and session restore files | desktop app persistence layer | JSON files under the app data root |

Current implementation note:

- `apps/desktop-shell/src-tauri/src/backend.rs` and `apps/web-ui/src/services/runtimeHost.ts` now share the default backend URL `http://127.0.0.1:8096`
- `apps/web-ui/src/services/runtimeHost.ts` still allows browser-preview overrides through `VITE_BACKEND_URL`
- root `package.json` now provides the repo-level rebuild execution scripts for `apps/web-ui/`, `apps/desktop-shell/`, and `packages/contracts/`
- `apps/desktop-shell/src-tauri/src/lib.rs` now exposes shell runtime facts, backend restart, shell window controls, and desktop restore-state persistence through typed Tauri commands
- `apps/desktop-shell/src-tauri/src/persistence.rs` now owns versioned JSON restore files, corrupted-file quarantine, and main-window state capture or restore
- `scripts/prepare_desktop_bundle_resources.py` plus `scripts/validate_desktop_bundle.py` now stage a bundle-safe copy of `local-backend/` plus the customization contracts needed by wardrobe export or import flows, and validate the desktop package surface before cargo checks
- `apps/desktop-shell/src-tauri/src/backend.rs` now resolves bundled `local-backend/` resources in release mode and injects host-owned app data or cache or log paths into the backend through `ASSISTANT_*` environment variables so packaged builds do not write mutable state into bundle resources
- `apps/desktop-shell/src-tauri/tauri.conf.json` plus `apps/desktop-shell/src-tauri/capabilities/default.json` now lock packaged resource roots, a non-null desktop CSP, and a shell-open-only plugin permission surface for the Tauri bundle
- `apps/web-ui/src/App.tsx`, `apps/web-ui/src/components/ShellLayout.tsx`, and `apps/web-ui/src/pages/StatusPage.tsx` now consume those host persistence surfaces for route auto-restore, session autosave, and diagnostics
- `apps/web-ui/src/services/runtimeHost.ts` now mirrors the same restore contract in browser preview by falling back to `localStorage`
- `apps/web-ui/src/styles/globals.css` plus `apps/web-ui/src/components/PageTemplate.tsx` now define the shared desktop design system and module-shell framing used across dashboard, chat, planner, wardrobe, settings, and diagnostics surfaces, without depending on remote Google font imports in the packaged shell

## Source Of Truth For The Desktop Rebuild

Use these paths first for A-series architecture work:

- repo execution surface:
  - `package.json`
  - `apps/web-ui/package.json`
  - `apps/desktop-shell/package.json`
  - `scripts/validate_desktop_execution_surface.py`
- desktop host:
  - `apps/desktop-shell/src-tauri/src/lib.rs`
  - `apps/desktop-shell/src-tauri/src/backend.rs`
  - `apps/desktop-shell/src-tauri/src/persistence.rs`
  - `apps/desktop-shell/src-tauri/tauri.conf.json`
  - `apps/desktop-shell/src-tauri/src/unity_runtime.rs`
  - `apps/desktop-shell/src-tauri/src/unity_bridge.rs`
- web UI:
  - `apps/web-ui/src/App.tsx`
  - `apps/web-ui/src/components/`
  - `apps/web-ui/src/components/WindowChrome.tsx`
  - `apps/web-ui/src/components/StartupScreen.tsx`
  - `apps/web-ui/src/components/ShellLayout.tsx`
  - `apps/web-ui/src/components/PageTemplate.tsx`
  - `apps/web-ui/src/pages/`
  - `apps/web-ui/src/pages/StatusPage.tsx`
  - `apps/web-ui/src/styles/globals.css`
  - `apps/web-ui/src/services/`
- backend:
  - `local-backend/app/api/routes.py`
  - `local-backend/app/core/config.py`
  - `local-backend/app/db/repository.py`
  - `local-backend/app/services/`
- shared contracts:
  - `packages/contracts/src/tauri.ts`
  - `packages/contracts/src/unity.ts`

## Dependencies And Non-Goals

- This phase depends on A00 only.
- This phase does not start real runtime sync with Unity.
- This phase does not embed Unity as a requirement for desktop readiness.
- This phase does not authorize mini mode or deeper 3D interactions.

## Related Documents

- `docs/architecture/desktop-runtime-flow.md`
- `docs/architecture/local-storage.md`
- `docs/adr/README.md`
- `docs/product/desktop-product-definition.md`
