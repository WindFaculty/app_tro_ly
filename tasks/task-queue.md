# Task Queue - Local Desktop Assistant

Updated: 2026-03-26
Status values: TODO | DOING | DONE | BLOCKED
Ownership: `Axx` = AI-executable repo work | `Pxx` = manual or off-repo work tracked in `tasks/task-people.md`

## How To Use This File

- Track only work that can be done directly in this repo.
- Use `tasks/task-people.md` for Unity Editor checks, machine setup, external assets, approvals, or target-machine validation.
- Keep this file current-state focused.
- Move completed work to `tasks/done.md` rather than leaving stale DOING items behind.

## Current AI Themes

- Unity degraded-mode and recovery UX polish
- regression coverage and validation evidence
- optional runtime hardening for speech and related diagnostics
- UI polish toward the design target without pretending the target is already implemented
- production-avatar integration planning

## Milestone Snapshot

- `M0 - Product reset and documentation baseline`: DONE
- `M1 - Foundation skeleton`: DONE
- `M2 - Task engine and views`: DONE
- `M3 - Chat and reasoning`: DONE
- `M4 - Voice and avatar behavior`: DONE
- `M5 - Reminder and planner`: DONE
- `M6 - Polish and Windows packaging`: DOING

## Done Recently

These items are closed and should not stay in the active queue:

- `A01`: Windows preflight hardening
- `A02`: setup or run or package script hardening
- `A03`: repeatable backend smoke automation
- `A06`: runbook completion

See `tasks/done.md` for historical details.

## Active AI Queue

- `A04 | DOING | Polish Unity degraded-mode, recovery, and backend-unavailable UX so the shell stays understandable when health is partial or error. | Current state: health banners, recovery guidance, and interaction gating already exist; remaining work is polish under real Unity runs and packaged-client validation. | Manual dependency: P01 P02`
- `A05 | DOING | Expand or maintain regression coverage for settings, reminders, subtitles, startup health, assistant streaming, and runtime fallback behavior. | Current state: backend automated coverage is present and verified; Unity test files exist but still need editor-run evidence on the target machine. | Manual dependency: P01`
- `A07 | TODO | Refine the existing UI Toolkit shell toward the design target in ui_feature_map.md without rewriting history about what already exists. | Current state: the repo already has `MainUI.uxml`, `AppShell.uxml`, split screen templates, and `AppRouter`; remaining work is layout polish, stronger schedule surfaces, and removal of obvious placeholder content. | Manual dependency: P01 P02`
- `A09 | TODO | Improve task interaction UX in the Unity client, including quick-add clarity, text rendering, empty states, and better schedule presentation. | Current state: task flows work, but several views still render as text-heavy placeholders. | Manual dependency: P02`
- `A10 | TODO | Improve chat UX with clearer transcript preview, action confirmations, and richer thinking or listening or talking feedback. | Current state: transcript preview and route diagnostics exist, but presentation is still basic. | Manual dependency: P02 P03`
- `A11 | DOING | Harden reminder, subtitle, avatar-state, and reconnect behavior under real client runs. | Current state: core event wiring exists; remaining work is live validation and polish across reconnect and degraded conditions. | Manual dependency: P01 P02 P03`
- `A12 | DOING | Continue optional runtime hardening for faster-whisper, whisper.cpp, Piper, ChatTTS, and Ollama-adjacent preflight diagnostics. | Current state: controlled degraded handling and smoke coverage exist; remaining work is machine-level validation, clearer operator guidance, and any follow-up fixes from target-machine evidence. | Manual dependency: P03`
- `A13 | TODO | Improve audio cache or temp cleanup, speech retry diagnostics, and related logging where runtime evidence shows gaps. | Current state: baseline cleanup exists in backend code, but longer-lived target-machine behavior still needs observation. | Manual dependency: P03`
- `A14 | TODO | Formalize the production-avatar integration contract between the assistant shell and `Assets/AvatarSystem/`. | Current state: the repo has avatar runtime controllers, prototype assets, validators, and animation assets, but the shell still needs a signed-off scene integration path. | Manual dependency: P01 P02 P04`
- `A15 | TODO | Prepare the final avatar replacement path so a signed-off avatar can replace placeholder presentation without broad shell rewrites. | Current state: avatar-specific groundwork exists, but live shell integration is still partial. | Manual dependency: P01 P02 P04`

## Future AI Roadmap

- `A08 | TODO | Build a compact mini-assistant mode once product direction and manual validation time are available. | Manual dependency: P02`
- `A16 | TODO | Build Google Calendar sync scaffolding when credentials and project setup exist. | Blocked by: P06`
- `A17 | TODO | Build a bounded browser or web-automation layer when environments and permissions exist. | Blocked by: P07`
- `A18 | TODO | Build a plugin system skeleton for optional capability registration.`
- `A19 | TODO | Add multiple avatar support after the single-avatar production path is stable. | Blocked by: P04`
- `A20 | TODO | Design wake-word mode architecture and safety controls. | Blocked by: P07`
- `A21 | TODO | Build a desktop control command layer with explicit permission gates and audit trail. | Blocked by: P07`
- `A22 | TODO | Design cross-device sync architecture that preserves the local-first baseline. | Blocked by: P08`
- `A23 | TODO | Build the first sync transport and status surface after the topology is fixed. | Blocked by: P08`

## Dependency Notes

- `P01` and `P02` are the main blockers for Unity-side validation work.
- `P03` is the main blocker for speech-runtime and target-machine runtime validation.
- `P04` is the main blocker for final production-avatar integration work.
- `P05` is historical done work, not an active blocker.

## Implementation Notes

- Do not describe the current shell as a missing three-column migration. That structure already exists in code.
- Do not reference `UiFactory.cs` as current code. The active loader is `UiDocumentLoader.cs`.
- Treat `unity-client/Assets/Resources/UI/ui_feature_map.md` as a design target, not implementation truth.
