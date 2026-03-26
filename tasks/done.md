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
- D009: Refreshed AI runtime documentation to match the implemented backend orchestration, route selection, and assistant streaming API.
- D010: Cleaned task tracking docs, clarified AI versus manual ownership, and repaired the Vietnamese task documentation text.

## 2026-03-19
- D011: Rebaselined `tasks/task-people.md` and `tasks/task-queue.md` against current verified Unity evidence, manual blockers, and active runtime hardening work.
- D012: Hardened backend TTS degraded-path handling so ChatTTS import or dependency mismatch now reports a controlled unavailable state instead of crashing health, TTS, or assistant streaming flows.
- D013: Isolated backend tests from local `.env` overrides, restoring repeatable health, scheduler, stream, chat, LLM, and TTS coverage in a clean test configuration.
- D014: Added shared Windows preflight helpers, a repeatable backend smoke script, release packaging validation, and reconnect loops for the Unity WebSocket clients.
- D015: Expanded `scripts/smoke_backend.py` to cover REST task update or reschedule or complete flows, event stream sequencing, available versus unavailable speech endpoints, and assistant stream completion; validated smoke runs on 2026-03-19 for partial dev-backend mode, ready dev-backend mode with validation runtime overrides, and ready packaged-release mode with the same overrides.
- D016: Added validation-only Piper smoke assets, pinned `transformers==4.41.2`, restored ChatTTS import compatibility for current torch builds, and fixed Piper subprocess Unicode handling so runtime validation can reach a true ready state under controlled overrides.

## 2026-03-20
- D017: Closed `A01` by tightening Windows preflight and release-layout checks so broken runtime paths fail fast, file-vs-directory mistakes are diagnosed clearly, and `local-backend/tests/test_windows_script_preflight.py` passed on 2026-03-20.
- D018: Closed `A02` by documenting stable Windows script exit codes, adding `run_all.ps1 -ShutdownBackendOnExit`, switching failure cleanup to stop backend child process trees, and extending `local-backend/tests/test_windows_script_preflight.py` to cover safe cleanup plus stable setup or run or package failure exits.
- D019: Closed `A06` by completing `docs/09-runbook.md` with step-by-step local setup, startup, smoke validation, release packaging, release-folder validation, troubleshooting, evidence capture, and validation-only startup guidance.

## Notes
- The documentation now targets the local desktop assistant project.
- Core implementation now exists in `unity-client/` and `local-backend/`, with M6 polish work still in progress.
- The repo still contains an existing `agent-platform` subproject that should remain optional relative to the assistant runtime.
