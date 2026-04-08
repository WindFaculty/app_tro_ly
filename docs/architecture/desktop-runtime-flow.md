# Desktop Runtime Flow

Status: Design target with current implementation notes  
Phase: A01 - Reset desktop architecture  
Last updated: 2026-04-07

## Purpose

This document locks the runtime flow for the standalone desktop app and separates:

- current implementation evidence
- target desktop flow for A-series work
- sync work intentionally deferred to S-series

## Current Implementation Evidence

Current repo-side behavior already shows these runtime slices:

1. Root `package.json` now provides repo-level `web:*`, `desktop:*`, and `rebuild:check` scripts across `apps/web-ui/`, `apps/desktop-shell/`, and `packages/contracts/`.
2. `apps/desktop-shell/src-tauri/src/lib.rs` now starts the Tauri host, owns backend restart plus window-control plus desktop-restore commands, starts the Unity bridge listener, and wires shutdown cleanup for backend plus Unity sidecars.
3. `apps/desktop-shell/src-tauri/src/backend.rs` now keeps backend process ownership inside the host, emits typed startup-status plus ready plus error events, retries health checks, and snapshots host-owned runtime facts such as app paths and current backend process metadata.
4. `apps/desktop-shell/src-tauri/src/persistence.rs` now owns versioned JSON restore files for session state, runtime snapshots, theme state, filters, app preferences, and main-window state, with corrupted-file quarantine plus default fallback behavior.
5. `apps/web-ui/src/App.tsx` now restores the last route before the business shell mounts when the user did not explicitly deep-link.
6. `apps/web-ui/src/components/ShellLayout.tsx` now auto-saves the shell session surface through typed Tauri persistence calls as route and runtime status change.
7. `apps/web-ui/src/services/runtimeHost.ts` now exposes the same desktop restore surface to React and mirrors it through `localStorage` in browser preview mode.
8. `apps/web-ui/src/pages/StatusPage.tsx` now shows persistence status, recent-route restore context, and restore-file locations for diagnostics.
9. `apps/web-ui/src/styles/globals.css` plus `apps/web-ui/src/components/PageTemplate.tsx` now provide the shared module-shell design language used by dashboard, chat, planner, wardrobe, settings, and diagnostics surfaces.
10. `local-backend/app/main.py` creates the FastAPI app, configures logging, builds the container, and starts the scheduler loop during lifespan startup.
11. `apps/web-ui/src/services/backendClient.ts` resolves the runtime backend URL before calling REST endpoints.
12. `apps/web-ui/src/services/runtimeHost.ts` and `apps/web-ui/src/services/unityBridge.ts` already separate host lifecycle calls from Unity bridge calls.
13. `apps/web-ui/src/components/StartupScreen.tsx`, `apps/web-ui/src/components/WindowChrome.tsx`, and `apps/web-ui/src/pages/StatusPage.tsx` now surface startup recovery, shell window controls, and host runtime facts through typed Tauri calls.
14. `apps/desktop-shell/src-tauri/src/unity_runtime.rs` and `apps/desktop-shell/src-tauri/src/unity_bridge.rs` already expose Unity runtime and bridge status as host-level concerns.
15. `scripts/prepare_desktop_bundle_resources.py` now stages a bundle-safe `local-backend/` runtime plus the wardrobe customization contracts and sample data under `apps/desktop-shell/.tauri-bundle-resources/` before `tauri build`, without copying live backend data or tests.
16. `apps/desktop-shell/src-tauri/tauri.conf.json` plus `apps/desktop-shell/src-tauri/capabilities/default.json` now lock the desktop package to `nsis` plus `msi`, a non-null desktop CSP, and a reduced plugin surface that keeps only safe external shell open plus logging.
17. `apps/desktop-shell/src-tauri/src/backend.rs` now resolves bundled `local-backend/` resources in release mode, routes backend mutable state into the Tauri app data or cache or log roots through `ASSISTANT_*` environment variables, and suppresses stray Windows console windows during backend startup.

## Target Startup Flow

The target desktop startup flow for Workstream A is:

```text
1. User starts the Tauri desktop app.
2. Tauri resolves app data, cache, log, and export roots.
3. Tauri launches the FastAPI backend.
4. Tauri runs backend health checks.
5. Tauri opens the React shell once the backend is usable or explicitly degraded.
6. React loads host runtime facts from Tauri.
7. React restores session and UI state from JSON-backed restore files.
8. React begins normal feature operation against the backend.
9. Unity remains optional and non-blocking for A-series completion.
```

## Business Data Flow

Normal business traffic follows this path:

```text
React UI -> HTTP or WebSocket -> FastAPI backend -> SQLite or integrations
```

Boundary rules:

- React talks directly to the backend for business data and streaming.
- Tauri does not sit in the middle of normal planner, notes, chat, settings, email, or calendar traffic.
- Backend remains the source of truth for domain data.

## Host Flow

Host-specific traffic follows this path:

```text
React UI -> typed Tauri command or event -> Tauri desktop host
```

Use host traffic only for:

- backend readiness
- runtime path discovery
- window lifecycle
- startup or recovery state
- Unity runtime inspection, launch, stop, and bridge status

## Unity Boundary During A-Series

During Workstream A, the desktop app may show Unity runtime status, but it must not depend on live Unity sync.

Allowed during A-series:

- inspect Unity runtime readiness
- show placeholder or status surfaces for the Unity region
- keep typed bridge contracts ready for later work

Not allowed during A-series:

- require Unity for desktop feature completion
- couple planner, notes, settings, or email workflows to live Unity state
- treat S-series sync as already active

## Browser Preview Flow

React browser preview remains a development-only convenience flow:

```text
Browser Vite preview -> explicit preview backend URL -> FastAPI backend
```

Current implementation note:

- Browser preview now defaults to `http://127.0.0.1:8096`
- Browser preview may still override that default through `VITE_BACKEND_URL`
- Tauri host launches and checks `http://127.0.0.1:8096`
- `scripts/validate_desktop_execution_surface.py` now guards this alignment in repo-side validation
- Tauri host now emits `backend-status`, `backend-ready`, and `backend-error` so the shell can recover without inventing startup state in React
- `scripts/validate_desktop_bundle.py` now guards the packaged resource roots, CSP, and reduced plugin surface before cargo checks

## Failure And Recovery Flow

Target failure behavior:

1. Tauri keeps the shell in control even when backend startup fails.
2. React shows startup, degraded, diagnostics, and recovery surfaces.
3. Backend unavailability does not crash the desktop shell.
4. Session restore files are loaded defensively and may fall back to defaults if corrupted.
5. Unity absence or bridge disconnect is reported as a status issue, not as a fatal desktop failure during A-series.

Current implementation note:

- `apps/web-ui/src/components/StartupScreen.tsx` now offers a typed retry path through `restart_backend`
- `apps/web-ui/src/components/WindowChrome.tsx` now exposes desktop window controls without relying on native window decorations
- `apps/desktop-shell/src-tauri/src/persistence.rs` now quarantines corrupted JSON restore files and rewrites them with defaults instead of crashing shell startup
- `apps/web-ui/src/App.tsx` now applies the last saved route only when the user did not already choose a route through the URL hash
- packaged desktop builds now start the backend from bundled resources rather than the repo root, while still surfacing the same diagnostics and recovery lane if startup fails

## Shutdown Flow

The target shutdown flow is:

```text
1. React flushes pending session or UI state.
2. Tauri requests orderly backend shutdown when appropriate.
3. Tauri stops optional sidecars.
4. Logs and restore files remain available for the next launch.

Current implementation note:

- `apps/web-ui/src/components/ShellLayout.tsx` now auto-saves route and runtime snapshot state while the shell is active
- `apps/desktop-shell/src-tauri/src/lib.rs` plus `apps/desktop-shell/src-tauri/src/persistence.rs` now capture window-state updates on move or resize or focus and again during close handling before backend or Unity cleanup
```

## Deferred Sync Flow

The following flow is explicitly deferred to S-series:

```text
React context -> Tauri bridge -> Unity runtime -> Unity event -> React UI
```

This may exist as scaffold or contract work, but it is not part of the A-series runtime completeness bar.

## Related Documents

- `docs/architecture/desktop-target.md`
- `docs/architecture/local-storage.md`
- `docs/product/desktop-non-goals.md`
