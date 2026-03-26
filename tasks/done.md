# Done Log

This file is a historical log.

Rules:

- Entries reflect what was considered complete at the time.
- Current active status belongs in `tasks/task-queue.md` and `tasks/task-people.md`.
- If history later needs clarification, add a note instead of silently rewriting older entries.

## 2026-03-13

- D001: Rebased the active project documentation from Character Studio to the local desktop assistant product.
- D002: Defined the new MVP scope, target architecture, API contract, UI modes, and test plan.
- D003: Reset milestone tracking and task queue around the assistant roadmap.
- D004: Added an architecture-as-is document to describe the transition state.
- D005: Reframed `agent-platform` as optional support rather than the primary runtime.

## 2026-03-14

- D006: Implemented a backend-backed Settings panel in the Unity shell and wired it to the local settings API.
- D007: Updated milestone tracking to reflect that implementation had already moved into polish work.
- D008: Added backend log output, health recovery actions, and Unity degraded-runtime guidance.
- D009: Refreshed AI runtime documentation to match backend orchestration, route selection, and streaming behavior.
- D010: Cleaned task-tracking docs, clarified AI versus manual ownership, and repaired earlier task-doc text issues.

## 2026-03-19

- D011: Rebaselined `tasks/task-people.md` and `tasks/task-queue.md` against verified Unity evidence, manual blockers, and active runtime hardening work.
- D012: Hardened backend TTS degraded-path handling so ChatTTS import or dependency mismatch reports a controlled unavailable state instead of crashing health, TTS, or streaming flows.
- D013: Isolated backend tests from local `.env` overrides so health, scheduler, stream, chat, LLM, and TTS coverage run repeatably in a clean test configuration.
- D014: Added shared Windows preflight helpers, a repeatable backend smoke script, release packaging validation, and reconnect loops for the Unity WebSocket clients.
- D015: Expanded `scripts/smoke_backend.py` to cover REST task mutation flows, event sequencing, speech endpoint availability checks, and assistant-stream completion.
- D016: Added validation-only Piper smoke assets, restored ChatTTS import compatibility for current torch builds, and fixed Piper subprocess Unicode handling so ready-state validation can succeed under controlled overrides.

## 2026-03-20

- D017: Closed `A01` by tightening Windows preflight and release-layout checks and adding automated coverage for those paths.
- D018: Closed `A02` by documenting stable Windows script exit codes, adding `run_all.ps1 -ShutdownBackendOnExit`, and hardening safe cleanup behavior.
- D019: Closed `A06` by completing `docs/09-runbook.md` with setup, startup, smoke, packaging, release-validation, troubleshooting, and evidence-capture guidance.

## 2026-03-26

- D020: Audited root docs, architecture docs, API docs, UI docs, runbook docs, task trackers, and AI guidance files against the current backend, Unity client, scripts, and avatar groundwork.
- D021: Rewrote stale docs to distinguish current implementation, planned work, optional subsystems, placeholders, and manual-validation-needed areas.
- D022: Rebased task trackers so the active queue no longer leaves already-closed work such as `A01`, `A02`, `A03`, and `A06` marked as active.
- D023: Relabeled `unity-client/Assets/Resources/UI/ui_feature_map.md` as a design-target document instead of implementation truth.

## Notes

- The active assistant runtime lives in `unity-client/` and `local-backend/`.
- `agent-platform/` remains optional.
- Backend tests were re-verified on 2026-03-26 from `local-backend/` with `pytest -q`: `62 passed`.
- Unity Editor validation is still a separate manual-evidence step.
