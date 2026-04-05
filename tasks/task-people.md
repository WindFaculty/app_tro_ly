# Task People - Local Desktop Assistant

Updated: 2026-04-05
Status values: TODO | DOING | DONE | BLOCKED
Ownership: `Pxx` = manual or off-repo work | `Axx` = AI-executable repo work tracked in `tasks/task-queue.md`

## How To Use This File

- Track work that cannot be completed safely from terminal-only repo access.
- Use this file for Unity Editor runs, target-machine checks, runtime installs, external assets, credentials, approvals, and sign-off.
- Keep active manual gates separate from historical done evidence.
- Keep unblock lists aligned with the active lane names in `tasks/task-queue.md`.

## Active Manual Gates

- `P02 | DOING | Run manual client smoke tests for the live UI lanes. P02 is split into two priority tiers below.`
  - `P02a | Core smoke \u2014 sign off these first to unblock UI-1 Shell, UI-2 Planner, UI-3 Chat closure |`
    - Shell navigation (left rail, route switching)
    - Chat send (text input, response round-trip)
    - Backend ready, partial, and unavailable startup states
    - Settings save, reload, and dirty-state behavior
    - Subtitle overlay and reminder overlay
    - `Done when: all five flows above have a clean Game view or packaged-window sign-off entry. Unblocks: A04, A07, A09, A10, A11, UI-1 through UI-3 closure.`
  - `P02b | Extended smoke \u2014 sign off these after P02a to close UI-4 and A34 |`
    - Character Space room overlay: selected-object card, current-activity strip, room action dock, return-to-avatar, hotspot toggle
    - Planner: selected-date navigation, direct complete, Inbox scheduling, Week and Completed flows
    - Packaged-client: at least one clean partial-state window screenshot on the correct app surface
    - Reconnect behavior after backend restart
    - `Done when: all four groups above have clean sign-off entries. Unblocks: A34, A31, A42 full closure, A44 live-smoke closure.`
  - `Current evidence: repo-side runtime UI includes keyboard submit, selected-date navigation, direct complete or inbox-schedule actions, boot-state messaging, status-card handling, runtime text or font validation hardening, chat-owned turn-state rendering from A33, shared-event routing from A34, and the full room-world interaction pipeline from A44 (EditMode 59 passed, PlayMode 41 passed on 2026-04-06). Existing Game view screenshots at unity-client/Assets/Screenshots/ and packaged captures at ai-dev-system/logs/p02/ remain as partial prior evidence. Remaining closure work: sign-off passes in tasks/p02-manual-checklist.md for both P02a and P02b flows.`
  - `Unblocks when fully done: UI-1 Shell + Shared UX, UI-2 Planner + Center Screens, UI-3 Chat + Runtime Feedback, UI-4 Avatar + Customization smoke, A34, A39`

- `P04 | TODO | Provide the signed-off production avatar asset, animator expectations, and lip-sync expectations for the production avatar path. | Current state: the repo already contains placeholder avatar runtime controllers, prototype assets, validators, and probe scenes, but it does not yet have the final approved production asset handoff needed for completion claims. | Done when: the repo has the non-placeholder inputs required for final avatar integration and wardrobe-facing production expectations. | Unblocks: UI-4 Avatar + Customization production work, A19`

## Completed Manual Evidence

- `P01 | DONE | Open `unity-client/` in Unity Editor, run EditMode and PlayMode tests, and capture pass or failure evidence. | Current state: EditMode `21 passed` and PlayMode `33 passed` were validated on 2026-03-28. | Role now: historical evidence record, not an active blocker.`
- `P03 | DONE | Install and configure speech runtimes on the target machine as needed for end-to-end validation. | Current state: STT (faster-whisper) and TTS (ChatTTS) were configured, their models were downloaded from HuggingFace, a PyTorch 2.6 security block during ChatTTS loading was patched, and both services were verified end-to-end through the local API. | Role now: historical evidence and unblock record, not an active blocker.`
- `P05 | DONE | Validate the packaged release folder on a clean Windows machine and record follow-up issues. | Historical note: this is no longer an active blocker. Evidence recorded earlier included a successful release-folder run and follow-up runtime observations.`

## Future Manual Prerequisites

- `P06 | BLOCKED | When calendar sync work starts, provide credentials, a test calendar, and project setup details. | Unblocks: A16`
- `P07 | BLOCKED | When automation work starts, provide accounts, environments, and Windows permissions for safe validation. | Unblocks: A17 A20 A21`
- `P08 | BLOCKED | When sync work starts, define device topology, network assumptions, and storage location. | Unblocks: A22 A23`
- `P09 | TODO | Manually validate the Windows GUI agent on the intended target desktop session, including the emergency-stop hotkey, focus behavior, self-healing behavior, and any future profile selectors beyond Notepad and Calculator. | Done when: at least one selector-failure scenario safely stops on ambiguous healing, at least one deterministic healing hint succeeds with artifact evidence, any configured Vision LLM fallback has been exercised or explicitly left disabled on the target machine, and hotkey plus focus behavior still match expectations during real desktop runs. | Unblocks: A26 future GUI-agent expansion work`
- `P10 | TODO | Manually validate the hybrid `unity-editor` control plane on the target Unity Editor desktop session. | Done when: the live capability matrix has been reviewed on the target machine, representative MCP-backed and GUI-fallback capabilities have been exercised with artifact paths recorded for failures, and planned graph capabilities that still show `unsupported` are recorded as backend follow-up instead of signed off as done. | Unblocks: A26`

## Rule

- If a task needs Unity visuals, a target machine, external binaries, external credentials, external asset handoff, or external approval, keep it here instead of forcing it into the AI queue.
- Only `P02`, `P04`, and the future prerequisites `P06` through `P10` should be described as active or potential blockers now.
- Treat `P01`, `P03`, and `P05` as completed evidence records rather than current gates.
