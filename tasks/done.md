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

## 2026-03-27

- D024: Updated `unity-client/Assets/Tests/PlayMode/AssistantAppPlayModeTests.cs` from legacy UGUI queries to the current UI Toolkit runtime contract and re-verified Unity PlayMode coverage from the active editor session with `25 passed`.
- D025: Landed the first shared UI shell foundation pass by polishing `AppShell.uxml`, adding reusable shell tokens or button or card styles, wiring schedule-side shell insight labels in `AppShellController`, and re-verifying Unity PlayMode coverage from the active editor session with `25 passed`.

## 2026-03-28

- D026: Completed the repo-side `UI-02`, `UI-03`, and `UI-04` pass by rebuilding the Home, Schedule, and Settings UI Toolkit screens around workload cards, quick-add guidance, structured schedule lists, grouped settings summaries, and updated PlayMode coverage; Unity PlayMode was re-verified from the active editor session with `25 passed`.
- D027: Completed manual task `P01` by verifying Unity EditMode tests (21 passed) and PlayMode tests (33 passed).
- D028: Implemented a Windows GUI agent MVP under `ai-dev-system/app/` with a modular controller, pywinauto-first automation, PyAutoGUI fallback wiring, timestamped JSONL and screenshot artifacts, Notepad and Calculator profiles, and a CLI for `list-windows`, `inspect`, and `run`.
- D029: Verified the Windows GUI agent on 2026-03-28 from `ai-dev-system/` with real profile runs for `python -m app.main run --profile calculator --task "compute 125*4"` and `python -m app.main run --profile notepad --task "type hello and save"`, plus automated test evidence from `.\.venv\Scripts\python.exe -m pytest app\tests -q` reporting `2 passed` and `.\.venv\Scripts\python.exe -m pytest tests -q` reporting `15 passed`.
- D030: Implemented the GUI-only `unity-editor` profile under `ai-dev-system/app/` with YAML macro task specs, a `UnityMacroRegistry`, pinned Unity surface and layout mapping, preflight fail-fast checks, per-run `unity-summary.json` artifacts, and unit coverage for macro compilation, preflight, and controller summaries. Verification on 2026-03-28 included `.\.venv\Scripts\python.exe -m pytest tests app\tests\unit -q` reporting `58 passed`, `.\.venv\Scripts\python.exe -m app.main inspect --app unity-editor` completing with control-tree and surface-screenshot artifacts under `ai-dev-system\logs\gui-agent\20260328T171439Z-inspect-unity-editor\`, and `.\.venv\Scripts\python.exe -m app.main --dry-run run --profile unity-editor --task-file %TEMP%\unity-editor-assert-layout.yaml` completing with `unity-summary.json` under `ai-dev-system\logs\gui-agent\20260328T171601Z-unity-editor\`.
- D031: Upgraded `ai-dev-system/app/` on 2026-03-29 from a GUI-only `unity-editor` profile to a hybrid Unity control plane with a per-run Unity MCP runtime, a live capability matrix, MCP-first routing plus GUI fallback for editor-surface tasks, structured `actions[]` and `verify[]` task specs, narrow NL-to-capability parsing, hybrid preflight, and expanded unit coverage. Verification on 2026-03-29 included `.\.venv\Scripts\python.exe -m pytest tests app\tests\unit -q` reporting `66 passed`, `.\.venv\Scripts\python.exe -m app.main list-capabilities --profile unity-editor` returning the live capability matrix from the current Unity session, `.\.venv\Scripts\python.exe -m app.main --dry-run run --profile unity-editor --task-file %TEMP%\unity-hybrid-task.yaml` completing with an MCP-backed dry-run action, and `.\.venv\Scripts\python.exe -m app.main run --profile unity-editor --task-file %TEMP%\unity-hybrid-live-task.yaml` completing a read-only `scene.manage` run with verification against `Assets/Scenes/AIDemoBasic3D.unity`.
- D032: Expanded `ai-dev-system/app/` on 2026-03-29 with platform abstraction contracts, provider-agnostic Vision LLM locator wiring, a bounded `ui_heal` recovery stage, action-level `heal_hints` plus `layout_policy` plus `execution` metadata, background MCP job tracking, MCP-backed `editor.layout.normalize`, surfaced `shader.manage` or `texture.manage` or `vfx.manage` or `animator.graph.manage` capabilities, and explicit `unsupported` statuses for planned graph capabilities such as Timeline and graph-level Shader Graph or VFX Graph mutations. Verification on 2026-03-29 included `.\.venv\Scripts\python.exe -m pytest app\tests\unit -q` reporting `65 passed`, `.\.venv\Scripts\python.exe -m pytest app\tests -q` reporting `67 passed`, `.\.venv\Scripts\python.exe -m pytest tests -q` reporting `15 passed`, and `.\.venv\Scripts\python.exe -m app.main list-capabilities --profile unity-editor` returning the expanded live capability matrix under `ai-dev-system\logs\gui-agent\20260329T161555Z-capabilities-unity-editor\`.

## Notes

- The active assistant runtime lives in `unity-client/` and `local-backend/`.
- `agent-platform/` remains optional.
- Backend tests were re-verified on 2026-03-26 from `local-backend/` with `pytest -q`: `62 passed`.
- Unity Editor validation is still a separate manual-evidence step.
