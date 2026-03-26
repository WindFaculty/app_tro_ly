# Task Queue - Local Desktop Assistant

Updated: 2026-03-19
Status values: TODO | DOING | DONE | BLOCKED
Ownership: `Axx` = Codex/AI-executable repo work | `Pxx` = human/manual/off-repo work tracked in `tasks/task-people.md`

## How To Use This File

- This file tracks work Codex or AI can execute directly inside the repo.
- `tasks/task-people.md` tracks machine setup, manual validation, assets, or approvals that must come from a person.
- Documentation note: AI and task docs were refreshed on 2026-03-14, but `A06` stays open until there is a step-by-step runbook for setup, release validation, and troubleshooting.

## Current AI Themes

- Runtime adapter hardening and degraded-path safety: `A12`
- Runtime hardening: `A01`, `A02`
- Unity degraded or recovery UX polish: `A04`, `A11`
- Validation and regression coverage: `A03`, `A05`
- Runbooks and operational docs: `A06`

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
- `A03 | TODO | Add smoke automation for health, REST task flows, WebSocket /v1/events, and degraded runtime paths. | Done when: a repeatable smoke suite can exercise healthy and degraded local backend behavior during validation. | Blocked by: P02 P03 P04 P05`
- `A04 | DOING | Polish Unity degraded/offline/recovery UX, reconnect handling, and user-facing error messaging. | Done when: the client surfaces partial/error states clearly and reconnects or recovers without confusing the user. | Blocked by: P02 P03 P04 P05`
- `A05 | DOING | Expand backend tests and Unity tests for settings, reminders, subtitles, startup health, assistant streaming, and runtime fallback behavior. | Current state: Unity EditMode and PlayMode suites are green, but backend coverage is still being hardened because health, TTS, and assistant streaming degraded-path tests currently regress under local runtime config leakage. | Done when: automated coverage is added for the main regression-prone flows across backend and Unity test suites.`
- `A06 | TODO | Add runbook-style docs for local AI/runtime setup, release validation, health checks, and troubleshooting. | Done when: a new machine can follow the written steps to install, validate, and diagnose the app. | Blocked by: P05`
- `A12 | DOING | Harden Whisper STT, Piper TTS, ChatTTS, and Ollama-adjacent runtime adapters with stronger config validation, clearer failure modes, and safer fallbacks. | Current state: the backend still has a known degraded-path bug where ChatTTS import or dependency mismatch can crash health, TTS, or assistant streaming tests instead of degrading cleanly. | Done when: adapter failures produce predictable diagnostics and do not take down unrelated app features.`
@
## Future AI Roadmap

- `A07 | TODO | Implement the Luminal/Aetheris 3-column Unity shell migration from unity-client/Assets/Resources/UI/ui_feature_map.md while keeping Today, Week, Inbox, Completed, and Settings flows intact. | Current state: MainUI.uxml is still a monolithic shell, UiFactory binds directly to that one tree, and AssistantApp owns tab/view switching inline without a dedicated layout controller. | Planned scope: split the shell into reusable UXML templates for top bar, sidebar, and right-panel chat; restyle MainStyle.uss around the new Left Sidebar / Center Stage / Right Panel layout; add a MainLayoutController.cs to own tab-driven view switching and panel visibility; then remap UiFactory and AssistantApp bindings to the new named elements without breaking current backend-backed task, chat, reminder, subtitle, and settings flows. | Done when: the assistant shell matches the new layout direction from ui_feature_map.md, preserves current working flows, and has automated coverage for the new layout wiring or tab-state behavior. | Blocked by: P02 P03 P04 P05`
- `A08 | TODO | Add the mini-assistant mode from docs/04-ui.md with open-main-app, mute speech, push-to-talk, and dismiss reminder controls. | Done when: a compact assistant mode exists and can handle the core quick-access flows without opening the full app.`
- `A09 | TODO | Polish task interactions, including quick add, edit, reschedule UX, empty states, and overdue or due-soon emphasis. | Done when: task management flows are faster, clearer, and easier to understand in normal and edge-case states.`
- `A10 | TODO | Polish chat UX with transcript preview, action confirmations, and better thinking/listening/talking feedback. | Done when: chat and voice flows show clear intermediate state and confirm task mutations cleanly. | Blocked by: P02 P03 P04 P05`
- `A11 | TODO | Harden event bus and client behavior, reminder presentation, subtitle sync, and avatar state mapping. | Current state: basic reminder, subtitle, avatar state, and event wiring exist, but reconnect and cross-state hardening are still incomplete. | Done when: reminder and speech-driven UI states stay in sync across normal, reconnect, and degraded flows. | Blocked by: P02 P03 P04 P05`
- `A13 | TODO | Improve audio cache and temp cleanup, retry policy, reminder speech fallback, and related logging. | Done when: audio-related flows are easier to debug and leave less stale data or silent failure behind.`
- `A14 | TODO | Formalize the avatar integration contract for a real asset, including loader hooks, animator parameters, lip-sync hooks, and fallback states. | Current state: avatar spec, validators, prefab build path, and prototype production assets exist, but the assistant shell still lacks the final runtime integration hook for a signed-off production avatar. | Done when: the codebase has a stable contract that a production avatar can plug into without guesswork. | Blocked by: P02 P03 P04 P05`
- `A15 | TODO | Prepare the avatar replacement path so a real model can be dropped in without redesigning the app shell. | Current state: avatar-specific groundwork exists under `Assets/AvatarSystem/`, but the live assistant app still uses placeholder avatar state visuals. | Done when: avatar-specific code is isolated enough that replacing the placeholder does not force broad UI or flow rewrites. | Blocked by: P02 P03 P04 P05`
- `A16 | TODO | Build Google Calendar sync scaffolding across backend, settings or UI surfaces, and sync conflict handling. | Done when: the repo has the integration skeleton, local config surface, and conflict-handling path ready for real credentials. | Blocked by: P06`
- `A17 | TODO | Build a browser or web automation layer with local wrappers, guarded commands, and UI or API hooks. | Done when: web automation capabilities can be invoked through bounded local interfaces instead of ad hoc shelling out. | Blocked by: P07`
- `A18 | TODO | Build a plugin system skeleton for optional tool integrations and capability discovery. | Done when: the app can register optional capabilities through a bounded extension point without changing the core assistant runtime each time.`
- `A19 | TODO | Add multiple avatar support across settings, persistence, loading, and runtime switching. | Done when: users can select among more than one avatar profile without breaking the single-backend task experience.`
- `A20 | TODO | Design and implement wake-word mode architecture, enable or disable UX, permission-safe fallback, and tests. | Done when: the app can support an optional always-listening path without breaking push-to-talk or trusted-local assumptions. | Blocked by: P07`
- `A21 | TODO | Build a desktop control command layer with explicit permission gates, audit trail, and safe-scope actions. | Done when: desktop control actions are constrained, reviewable, and isolated from the normal task assistant path. | Blocked by: P07`
- `A22 | TODO | Design cross-device sync architecture with a local-first conflict model and clear state ownership rules. | Done when: the repo has a documented and implementable sync design that does not break the single-device local-first baseline. | Blocked by: P08`
- `A23 | TODO | Build the first cross-device sync transport and storage baseline plus sync status UI surface. | Done when: a minimal sync path exists end-to-end and surfaces its status clearly in the app. | Blocked by: P08`

## Dependencies

- `P02-P05` gate the remaining hands-on validation and environment-backed UX work for `A03 A04 A07 A10 A11 A14 A15`.
- `P05` is the release-folder validation dependency for `A01 A02 A06`.
- `P06` gates `A16` until calendar credentials and project setup exist.
- `P07` gates `A17 A20 A21` until automation environments and permissions exist.
- `P08` gates `A22 A23` until cross-device topology and storage assumptions are fixed.

## Planned Breakdown Notes

- `A07 source`: `unity-client/Assets/Resources/UI/ui_feature_map.md`
- `A07 step 1`: Replace the current single-tree `MainUI.uxml` shell with a wrapper layout that clearly defines `LeftSidebar`, `CenterStage`, and `RightPanel`, while preserving the existing Home, Schedule, and Settings content entry points.
- `A07 step 2`: Extract reusable pieces into separate UXML templates such as `TopBar.uxml`, `Sidebar.uxml`, and `ChatPanel.uxml`, then update `UiFactory.cs` so element lookup is driven by the new hierarchy instead of the old monolithic names only.
- `A07 step 3`: Add `MainLayoutController.cs` to centralize top-tab switching and right-panel mode changes, then reduce the layout-specific branching that currently lives inside `AssistantApp.cs`.
- `A07 step 4`: Rewrite `MainStyle.uss` around shared design tokens and the new premium shell styling from the feature map, including responsive column sizing, card states, and button hover or emphasis styling that UIToolkit can support safely.
- `A07 step 5`: Rebind live data into the new named UI surfaces so health, avatar state, subtitle, task summaries, schedule content, chat, reminders, and settings status still render from the current stores and API responses.
- `A07 verify`: Run Unity EditMode or PlayMode coverage for the new layout wiring and tab visibility behavior, and keep manual visual smoke validation as a follow-up under blocker `P02` because terminal-only work cannot fully verify final UI polish.

## Tóm Tắt Tiếng Việt

- File này theo dõi các việc Codex hoặc AI có thể làm trực tiếp trong repo.
- Các lane đang chạy là `A01`, `A02`, `A04`, `A05`, và `A12`.
- `A12` được kéo lên ưu tiên cao vì backend hiện còn lỗi degraded-path ở TTS và assistant streaming khi runtime local bị lệch hoặc dependency ChatTTS không tương thích.
- `A06` vẫn chưa xong vì mới chỉ cập nhật tài liệu tổng quan; runbook từng bước cho setup, release validation, và troubleshooting vẫn còn thiếu.
- `P01` đã có bằng chứng Unity test pass; các blocker thủ công còn lại chủ yếu là `P02-P05`.
