# ADR 0002: Desktop Config And Path Ownership

Status: Accepted  
Date: 2026-04-07

## Context

Current code shows multiple config surfaces:

- backend environment settings in `local-backend/app/core/config.py`
- host-owned backend spawning in `apps/desktop-shell/src-tauri/src/backend.rs`
- React preview fallback config in `apps/web-ui/src/services/runtimeHost.ts`

The rebuild needs one clear ownership model instead of several competing runtime truths.

## Decision

- Tauri owns runtime path resolution for app data, cache, logs, and export roots in desktop mode.
- FastAPI continues to own provider and service configuration through backend settings.
- React may use `VITE_*` values only for browser preview or local development convenience.
- Typed command and event names belong in `packages/contracts/`.
- Desktop runtime URL discovery in production flows must come from Tauri, not from React defaults.

## Consequences

- Current preview or host URL drift must be removed in later implementation phases.
- React stays free of provider secrets and host path logic.
- Host path resolution can change without rewriting page code.
