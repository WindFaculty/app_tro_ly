# ADR 0001: Desktop Runtime Boundaries

Status: Accepted  
Date: 2026-04-07

## Context

The repo currently contains:

- a Tauri desktop host scaffold
- a React business UI scaffold
- a FastAPI backend that already owns domain logic
- a Unity runtime that still exists in the current end-user shell

Workstream A must produce a standalone desktop app without reintroducing unclear ownership between host, UI, backend, and Unity.

## Decision

The desktop architecture boundaries are:

- Tauri owns lifecycle, process management, app paths, and host events.
- React owns all business UI and desktop interaction workflows.
- FastAPI owns business logic, integrations, and domain persistence.
- `packages/contracts/` owns typed cross-layer envelopes and names.
- Unity remains a separate runtime and is not required for Workstream A completion.

## Consequences

- React calls the backend directly for business traffic.
- Tauri does not become a business API proxy.
- Unity does not receive new business UI work.
- Live React or Unity sync remains deferred until S-series.
