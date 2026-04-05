# Task Queue - Local Desktop Assistant

Updated: 2026-04-06 (task-sync audit pass — D054)
Status values: TODO | DOING | DONE | BLOCKED
Ownership: `Axx` = AI-executable repo work | `Pxx` = manual or off-repo work tracked in `tasks/task-people.md`

## How To Use This File

- Track only work that can be done directly in this repo.
- Keep this file focused on the current AI queue: `TODO`, `DOING`, or `BLOCKED` work only.
- Move completed work to `tasks/done.md` instead of leaving stale done items in the active queue.
- Use `tasks/task-people.md` for Unity Editor smoke, target-machine checks, external assets, approvals, or credentials.
- Use `tasks/module-migration-backlog.md` for phased modularization planning; it is not the active queue.
- Use `tasks/task-template.md` and `docs/operations/agent-workflow.md` when creating or splitting scoped AI work.

## Current Focus

- Keep the current `unity-client/` plus `local-backend/` runtime stable while modularization continues inside the existing tree.
- Respect the Phase 0 freeze: start only boundary, validation, docs, governance, regression-fix, or approved placeholder-safe contract work unless the tracker explicitly approves broader scope.
- Close the remaining live-smoke gates under `P02` without rewriting current implementation history.
- Continue phased module-boundary work only where repo-side evidence already exists.
- Keep docs and task governance aligned with landed code so design-target material is not mistaken for shipped behavior.

## Milestone Snapshot

- `M0 - Product reset and documentation baseline`: DONE
- `M1 - Foundation skeleton`: DONE
- `M2 - Task engine and views`: DONE
- `M3 - Chat and reasoning`: DONE
- `M4 - Voice and avatar behavior`: DONE
- `M5 - Reminder and planner`: DONE
- `M6 - Polish and Windows packaging`: DOING | Done when: P02 is fully signed off, A42 closed with fresh compile evidence, A43 started with 4 screens rendering, packaged-client smoke passes clean with no project-code startup errors.

## Active UI Workstreams

The 4-lane split below supersedes the historical `UI-01` through `UI-09` slice map. Those older UI slices are history only and should now be read through `tasks/done.md`.

### UI-1 | Shell + Shared UX

- Objective: keep shell navigation, degraded-mode messaging, packaged startup surfaces, and shared shell-state copy understandable while the module boundary work settles.
- Completed groundwork reference: `A30` shell module extraction is done and now lives in `tasks/done.md` plus `tasks/module-migration-backlog.md`.
- Completed groundwork reference: `A41` settings boundary extraction and the Phase 1 dependency audit are done and now live in `tasks/done.md` plus `tasks/module-migration-backlog.md`.
- Active tasks:
  - `A04 | DOING | Polish Unity degraded-mode, recovery, and backend-unavailable UX so the shell stays understandable when health is partial or error. | Current state: health banners, recovery guidance, interaction gating, and shell-side status copy exist in repo; remaining work is live-client and packaged-client validation under the current shell boundary. | Manual gate: P02`
  - `A07 | DOING | Refine the existing UI Toolkit shell toward the design target without claiming the design target is already shipped. | Current state: the runtime shell now uses a four-zone layout with a left control rail, center avatar stage, bottom planner sheet, right chat panel, and drawer-based settings; remaining work is final smoke and polish against the current runtime. | Manual gate: P02`
- Shared dependency: `A05` owns cross-cutting regression coverage and supplies UI-facing evidence to this lane without being lane-owned here.
- Planned next: close the remaining shell-side `P02` smoke, confirm packaged unavailable or partial or ready startup surfaces remain readable, and keep shell-owned copy aligned with the `IShellModule` boundary.
- Live manual gate: `P02`
- Lane acceptance: shell navigation and degraded or recovery UX stay readable in real Unity and packaged-client smoke after the current module-boundary refactors.

### UI-2 | Planner + Center Screens

- Objective: keep planner-owned views, task interactions, and center-screen routing inside the schedule feature boundary while the list-first schedule experience remains the current implementation baseline.
- Completed groundwork reference: `A32` planner-facing backend integration is done and now lives in `tasks/done.md` plus `tasks/module-migration-backlog.md`.
- Active tasks:
  - `A09 | DOING | Improve task interaction UX in the Unity client, including quick-add clarity, text rendering, empty states, and better schedule presentation. | Current state: quick add guidance, selected-date navigation, direct complete or inbox-schedule actions, and runtime font-fallback hardening already landed; closure still depends on a fresh `P02` pass. | Manual gate: P02`
  - `A31 | DOING | Harden the planner module boundary so schedule refs, controller wiring, and planner route ownership live under `unity-client/Assets/Scripts/Features/Schedule/`. | Current state: `PlannerModule` and `IPlannerModule` now wrap the existing schedule controller, planner-owned refs moved under the schedule feature boundary, and repo-side Unity verification on 2026-04-04 refreshed coverage with EditMode `26 passed`, PlayMode `41 passed`, and zero project-code console errors. Remaining work is live smoke before closure. | Manual gate: P02`
- Future link: `A16` calendar sync scaffolding stays in the roadmap and remains blocked by `P06`; it is not lane-active work today.
- Planned next: finish the `P02` rerun for Today or Week or Inbox or Completed flows, then carry later planner or chat follow-up work on top of the landed planner backend-integration boundary.
- Live manual gate: `P02`
- Lane acceptance: Today or Week or Inbox or Completed remain planner-owned, interactive, and consistent under repo-side tests plus live manual smoke.

### UI-3 | Chat + Runtime Feedback

- Objective: keep transcript UX, runtime diagnostics, reminder or subtitle behavior, and event-driven chat flow improving without regressing the already landed chat module boundary.
- Completed groundwork reference: `A29` chat boundary setup and `A33` chat-owned turn-state isolation are done and now live in `tasks/done.md` plus `tasks/module-migration-backlog.md`.
- Active tasks:
  - `A10 | DOING | Improve chat UX with clearer transcript preview, action confirmations, and richer thinking or listening or talking feedback. | Current state: the panel now separates transcript, status, transcript preview, and task confirmation surfaces; remaining work is live smoke against the current shell and planner boundaries. | Manual gate: P02`
  - `A11 | DOING | Harden reminder, subtitle, avatar-state, and reconnect behavior under real client runs. | Current state: core event wiring exists; remaining work is live validation and any follow-up polish exposed by those runs. | Manual gate: P02`
  - `A12 | DOING | Continue optional runtime hardening for faster-whisper, whisper.cpp, Piper, ChatTTS, and Ollama-adjacent preflight diagnostics. | Current state: repo-side speech health probes are more honest about failed runtime initialization; follow-up work should come only from new runtime evidence.`
  - `A13 | TODO | Improve audio cache or temp cleanup, speech retry diagnostics, and related logging where runtime evidence shows gaps. | Current state: baseline cleanup exists; keep this evidence-driven and do not reopen it without a concrete observed gap.`
  - `A34 | DOING | Move chat, planner, subtitle, speech, and avatar-state handoff onto explicit shared event flow instead of direct feature coupling. | Current state: repo-side work is done — planner screen or date or task-action requests, subtitle visibility, and local or backend avatar-state handoff now publish through shared event contracts in the Unity client; Unity verification on 2026-04-05 confirmed EditMode `30 passed`, PlayMode `43 passed`, zero project-code console errors. Remaining closure work is the `P02` live smoke rerun for chat, overlays, reconnect, and shell-visible avatar-state behavior; this task is repo-done and awaits P02 sign-off for full closure. | Manual gate when landed: P02`
- Historical evidence note: `P03` is already done and remains an evidence record, not a current blocker.
- Planned next: close current `P02` smoke for chat, overlays, reconnect behavior, and planner-to-shell handoff on top of the landed `A34` repo-side event flow.
- Live manual gate: `P02`
- Lane acceptance: chat and runtime-feedback behavior remain event-safe, readable, and regression-covered while module-owned boundaries continue to harden.

### UI-4 | Avatar + Customization

- Objective: prepare the shell-to-avatar and future wardrobe path without pretending the production avatar handoff already exists.
- Current queue:
  - `A14 | TODO | Formalize the production-avatar integration contract between the assistant shell and `Assets/AvatarSystem/`. | Current state: avatar runtime controllers, prototype assets, validators, and probe scenes exist, but the shell still needs a signed-off scene integration path. | Manual gate: P04`
  - `A15 | TODO | Prepare the final avatar replacement path so a signed-off avatar can replace placeholder presentation without broad shell rewrites. | Current state: avatar groundwork exists, but live shell integration is still partial. | Manual gates: P02 P04`
  - `A35 | BLOCKED | Implement the avatar-experience module boundary while preserving placeholder fallback until approved assets arrive. | Current state: blocked planned work; do not claim production avatar completion before the asset handoff exists. | Manual gate: P04`
  - `A36 | DOING | Implement the wardrobe plus asset-data foundation so placeholder or sample customization can exist before final production content arrives. | Current state: Phase 4 placeholder-safe standardization now includes `AvatarAssetRegistryDefinition`, registry-aware outfit load paths, registry validation in the existing avatar validator menu, and feature docs for the asset contract plus intake checklist. Repo-side baseline is done; remaining work is asset population, Unity-side smoke, and the blocked production handoff under `P04`; this still does not claim production avatar completion or a shipped wardrobe UI.`
  - `A44 | DOING | Build the room-world foundation for Character Space inside the current Unity tree as a placeholder-safe center-stage baseline. | Current state: all 15 ROOM tasks (ROOM-001 through ROOM-015) from the room-world design plan are done via D045–D053; the repo now has room-template bootstrap, 12-object placeholder registry pipeline, interaction layer with click-select and hover highlight, CharacterRoomBridge, Character Space overlay with current-activity strip and room action dock, RoomObjectRegistryValidator, and docs/features/room-object-intake-checklist.md. Repo-side Unity verification on 2026-04-06: EditMode `59 passed`, PlayMode `41 passed`, zero project-code console errors. Remaining work is actual prefab-backed object intake through the established intake path, movement-grade character behavior behind dock actions, richer object-state changes beyond placeholder intent, and `P02` live smoke before the room can be described as fully verified behavior. | Manual gates: P02 P04 for any production-avatar claim`
- Legacy evidence note: `tasks/avatar-system-tasks.md` remains a legacy evidence file for prototype avatar groundwork and is not the active source of truth for this lane.
- Future link: `A19` multiple-avatar support stays in the roadmap and remains blocked by `P04`; it is not lane-active work today.
- Planned next: keep placeholder-safe avatar integration planning moving, finish any remaining shell-side smoke under `P02`, and wait for `P04` before calling production avatar or wardrobe work implementation-ready.
- Live manual gates: `P02` for shell smoke, `P04` for production asset handoff
- Lane acceptance: avatar-related work stays honest about placeholder versus production state, and future customization work hangs off an explicit shell-to-avatar contract instead of a second competing tracker.

## Cross-Cutting / Non-UI

- `A05 | DOING | Expand or maintain regression coverage for settings, reminders, subtitles, startup health, assistant streaming, and runtime fallback behavior. | Current state: backend automated coverage exists, Unity EditMode was re-verified on 2026-04-05 with `30 passed`, and Unity PlayMode was re-verified on 2026-04-05 with `43 passed`; future reruns still belong here as cross-cutting evidence.`
- `A42 | DOING | Execute the first Phase 2 UI-versus-domain split inside the current Unity tree without inventing new runtime surfaces. | Current state: domain split is in place — Home has a module boundary, quick-add command wording lives in `HomeQuickAddApplicationService`, chat-submit planning lives in `ChatTurnApplicationService`, planner task-mutation summaries live in `PlannerTaskCommandApplicationService`, and a placeholder-safe `AvatarOutfitApplicationService` wraps the existing `AvatarSystem` outfit runtime. Remaining closure work is fresh Unity compile plus EditMode evidence (no Unity Editor session was available when this landed) and downstream `P02` live smoke. Once fresh compile evidence is captured this task is closure-ready on the repo side. | Manual gate when landed: P02`
- `A24 | DOING | Build and harden the MCP-driven Unity automation workflow under `ai-dev-system/`. | Current state: local MCP scaffold, stdio connection, smoke tasks, screenshot evidence, and workflow reporting already exist; remaining work is broader live-run evidence and reconnect hardening follow-up.`
- `A26 | DOING | Upgrade `ai-dev-system/app/` from a GUI-only Unity Editor profile to a hybrid Unity control plane. | Current state: the hybrid profile, live capability matrix, MCP-first routing, GUI fallback, and repo-side tests exist; remaining work is target-desktop manual validation under `P09` and `P10`. | Manual gates: P09 P10`
- `A37 | DOING | Execute the docs rewrite for landed modules and migration phases so current implementation and planned work stay clearly separated. | Current state: `docs/roadmap.md`, `docs/index.md`, `docs/architecture/README.md`, `docs/features/README.md`, and `docs/operations/README.md` now provide a Phase 3 docs hierarchy, while `docs/architecture/adr/` plus `docs/operations/documentation-governance.md` add ADR and source-of-truth navigation for current docs. Remaining work is the broader follow-through that keeps module docs current as later modularization slices land.`
- `A38 | DOING | Execute the task-governance rewrite for phased modularization tracking while staying compatible with this repo's tracker rules. | Current state: `docs/migration/phase0.md` now carries the freeze and architecture-gate guardrails, `AGENTS.md` stays the machine-facing rule file, `docs/operations/agent-workflow.md` now defines the current task protocol plus completion reporting, `tasks/task-template.md` defines the scoped-task and completion-report shape, and Phase 3 docs governance still enforces `docs changed with code`. Remaining work is to keep later phased tracker entries aligned as more slices land.`
- `A40 | TODO | Add repeatable validation automation that catches task or doc drift during migration.`

## Future Roadmap

- `A08 | TODO | Build a compact mini-assistant mode once product direction and manual validation time are available. | Manual gate when started: P02`
- `A43 | TODO | Execute the planned shell-surface expansion for CharacterSpace, Planner, Chat, and Wardrobe inside the current Unity tree once implementation work is approved after the freeze. | Current state: `docs/architecture/ui-modules-plan.md` now records the Phase 0 navigation map, shell-region ownership, controller ownership, and shared-component list while current runtime remains the existing Home or Schedule or Chat or Settings shell described in `docs/02-architecture.md`. | Manual gates when started: P02 P04`
- `A16 | TODO | Build Google Calendar sync scaffolding when credentials and project setup exist. | Blocked by: P06`
- `A17 | TODO | Build a bounded browser or web-automation layer when environments and permissions exist. | Blocked by: P07`
- `A18 | TODO | Build a plugin system skeleton for optional capability registration.`
- `A19 | TODO | Add multiple avatar support after the single-avatar production path is stable. | Blocked by: P04`
- `A20 | TODO | Design wake-word mode architecture and safety controls. | Blocked by: P07`
- `A21 | TODO | Build a desktop control command layer with explicit permission gates and audit trail. | Blocked by: P07`
- `A22 | TODO | Design cross-device sync architecture that preserves the local-first baseline. | Blocked by: P08`
- `A23 | TODO | Build the first sync transport and status surface after the topology is fixed. | Blocked by: P08`
- `A39 | BLOCKED | Evaluate any root-level repo structure move, including a possible `clients/unity/` path, only after later modularization slices and manual smoke justify the churn. | Blocked by: A31 A33 A35 A37 A38 P02`

## Active Manual Gates

- `P02` is the main live gate for Unity and packaged-client smoke across `UI-1`, `UI-2`, `UI-3`, and the smoke portion of `UI-4`.
- `P04` is the main production-content gate for `UI-4`.
- `P06` through `P10` remain future prerequisites and should not be treated as active blockers until their roadmap items are pulled forward.
- `P01`, `P03`, and `P05` are historical done records and should no longer be described as active blockers.

## Implementation Notes

- The active assistant runtime still lives in `unity-client/` plus `local-backend/`.
- Current implementation uses a four-zone shell with a left control rail, center stage, bottom planner sheet, and right chat column.
- Do not reference `UiFactory.cs` as current implementation; the active loader is `UiDocumentLoader.cs`.
- Treat `unity-client/Assets/Resources/UI/ui_feature_map.md` as a design target, not implementation truth.
- Treat `tasks/module-migration-backlog.md` as planned phased work; it does not replace the active runtime paths until verified code changes land.
- `them_tinh_moi.md` is a design-notes file for the room-world plan; all 15 ROOM tasks are done — see D045 through D053 and D054 in `tasks/done.md`.
- `tai_cau_truc_tach_logic.md` is a design-notes file for the 4-UI module plan; UI-001, UI-006, UI-008, UI-010, UI-011, and UI-012 are done; UI-002 through UI-004 and UI-013 are scoped under A43 (TODO); remaining items are active DOING tasks.
- `P02` has been logically split into P02a (core flows: shell nav, chat send, backend states, settings, subtitle/reminder) and P02b (extended flows: Character Space overlay, planner Week/Inbox/Completed, packaged partial, reconnect). P02a should be signed off first to unblock UI-1 through UI-3 closure.
