# Task Queue - Local Desktop Assistant

Updated: 2026-03-14
Status values: TODO | DOING | DONE | BLOCKED
Ownership: `Axx` = Codex/AI-executable repo work | `Pxx` = human/manual/off-repo work tracked in `tasks/task-people.md`

## Milestone Summary
- `M0 - Product reset and documentation baseline`: DONE
- `M1 - Foundation skeleton`: DONE
- `M2 - Task engine and views`: DONE
- `M3 - Chat and reasoning`: DONE
- `M4 - Voice and avatar behavior`: DONE
- `M5 - Reminder and planner`: DONE
- `M6 - Polish and Windows packaging`: DOING

## Active AI Queue
- `A01 | DOING | Expand preflight checks for backend and release scripts to detect missing Python dependencies, missing Whisper/Piper/Ollama runtimes, broken runtime paths, and invalid release folders. | Done when: startup and packaging flows fail fast with actionable diagnostics before partial startup. | Blocked by: P05`
- `A02 | DOING | Harden scripts/setup_windows.ps1, scripts/run_all.ps1, and scripts/package_release.ps1 with clearer logs, stable exit codes, and safe startup/shutdown flow. | Done when: the scripts report failures consistently and leave fewer dangling processes or ambiguous outcomes. | Blocked by: P05`
- `A03 | TODO | Add smoke automation for health, REST task flows, WebSocket /v1/events, and degraded runtime paths. | Done when: a repeatable smoke suite can exercise healthy and degraded local backend behavior during validation. | Blocked by: P01 P02 P03 P04 P05`
- `A04 | DOING | Polish Unity degraded/offline/recovery UX, reconnect handling, and user-facing error messaging. | Done when: the client surfaces partial/error states clearly and reconnects or recovers without confusing the user. | Blocked by: P01 P02 P03 P04 P05`
- `A05 | TODO | Expand backend tests and Unity tests for settings, reminders, subtitles, startup health, and runtime fallback behavior. | Done when: automated coverage is added for the main regression-prone flows across backend and Unity test suites.`
- `A06 | TODO | Add runbook-style docs for local runtime setup, release validation, and troubleshooting. | Done when: a new machine can follow the written steps to install, validate, and diagnose the app. | Blocked by: P05`

## Future AI Roadmap
- `A07 | TODO | Upgrade the runtime-generated Unity UI into a production-ready shell while keeping Today, Week, Inbox, Completed, and Settings flows intact. | Done when: the assistant shell looks intentional, keeps the existing information architecture, and remains wired to the current backend flows. | Blocked by: P01 P02 P03 P04 P05`
- `A08 | TODO | Add the mini-assistant mode from docs/04-ui.md with open-main-app, mute speech, push-to-talk, and dismiss reminder controls. | Done when: a compact assistant mode exists and can handle the core quick-access flows without opening the full app.`
- `A09 | TODO | Polish task interactions, including quick add, edit, reschedule UX, empty states, and overdue or due-soon emphasis. | Done when: task management flows are faster, clearer, and easier to understand in normal and edge-case states.`
- `A10 | TODO | Polish chat UX with transcript preview, action confirmations, and better thinking/listening/talking feedback. | Done when: chat and voice flows show clear intermediate state and confirm task mutations cleanly. | Blocked by: P01 P02 P03 P04 P05`
- `A11 | TODO | Harden event bus and client behavior, reminder presentation, subtitle sync, and avatar state mapping. | Done when: reminder and speech-driven UI states stay in sync across normal, reconnect, and degraded flows. | Blocked by: P01 P02 P03 P04 P05`
- `A12 | TODO | Harden Whisper STT, Piper TTS, and Ollama adapters with stronger config validation, clearer failure modes, and safer fallbacks. | Done when: adapter failures produce predictable diagnostics and do not take down unrelated app features.`
- `A13 | TODO | Improve audio cache and temp cleanup, retry policy, reminder speech fallback, and related logging. | Done when: audio-related flows are easier to debug and leave less stale data or silent failure behind.`
- `A14 | TODO | Formalize the avatar integration contract for a real asset, including loader hooks, animator parameters, lip-sync hooks, and fallback states. | Done when: the codebase has a stable contract that a production avatar can plug into without guesswork. | Blocked by: P01 P02 P03 P04 P05`
- `A15 | TODO | Prepare the avatar replacement path so a real model can be dropped in without redesigning the app shell. | Done when: avatar-specific code is isolated enough that replacing the placeholder does not force broad UI or flow rewrites. | Blocked by: P01 P02 P03 P04 P05`
- `A16 | TODO | Build Google Calendar sync scaffolding across backend, settings or UI surfaces, and sync conflict handling. | Done when: the repo has the integration skeleton, local config surface, and conflict-handling path ready for real credentials. | Blocked by: P06`
- `A17 | TODO | Build a browser or web automation layer with local wrappers, guarded commands, and UI or API hooks. | Done when: web automation capabilities can be invoked through bounded local interfaces instead of ad hoc shelling out. | Blocked by: P07`
- `A18 | TODO | Build a plugin system skeleton for optional tool integrations and capability discovery. | Done when: the app can register optional capabilities through a bounded extension point without changing the core assistant runtime each time.`
- `A19 | TODO | Add multiple avatar support across settings, persistence, loading, and runtime switching. | Done when: users can select among more than one avatar profile without breaking the single-backend task experience.`
- `A20 | TODO | Design and implement wake-word mode architecture, enable or disable UX, permission-safe fallback, and tests. | Done when: the app can support an optional always-listening path without breaking push-to-talk or trusted-local assumptions. | Blocked by: P07`
- `A21 | TODO | Build a desktop control command layer with explicit permission gates, audit trail, and safe-scope actions. | Done when: desktop control actions are constrained, reviewable, and isolated from the normal task assistant path. | Blocked by: P07`
- `A22 | TODO | Design cross-device sync architecture with a local-first conflict model and clear state ownership rules. | Done when: the repo has a documented and implementable sync design that does not break the single-device local-first baseline. | Blocked by: P08`
- `A23 | TODO | Build the first cross-device sync transport and storage baseline plus sync status UI surface. | Done when: a minimal sync path exists end-to-end and surfaces its status clearly in the app. | Blocked by: P08`

## Dependencies
- `P01-P05` gate hands-on validation and environment-backed UX work for `A03 A04 A07 A10 A11 A14 A15`.
- `P05` is the release-folder validation dependency for `A01 A02 A06`.
- `P06` gates `A16` until calendar credentials and project setup exist.
- `P07` gates `A17 A20 A21` until automation environments and permissions exist.
- `P08` gates `A22 A23` until cross-device topology and storage assumptions are fixed.
