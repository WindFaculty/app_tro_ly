# Done Log

## 2026-03-13
- D001: Rebased the active project documentation from Character Studio to the local desktop assistant product.
- D002: Defined the new MVP scope, target architecture, API contract, UI modes, and test plan.
- D003: Reset milestone tracking and task queue around the assistant roadmap.
- D004: Added an `architecture as-is` document that clearly states the repo is still in a transition state.
- D005: Reframed `agent-platform` documentation as optional support for the new assistant instead of the primary runtime path.

## 2026-03-14
- D006: Implemented a backend-backed Settings panel in the Unity shell and wired it to the local settings API.
- D007: Updated milestone tracking to reflect that implementation has already reached M6 polish work rather than the original documentation-reset baseline.
- D008: Added backend log file output, health recovery actions, and Unity degraded-runtime guidance for local recovery paths.

## Notes
- The documentation now targets the local desktop assistant project.
- Core implementation now exists in `unity-client/` and `local-backend/`, with M6 polish work still in progress.
- The repo still contains an existing `agent-platform` subproject that should remain optional relative to the assistant runtime.
