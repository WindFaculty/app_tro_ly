# Module Migration Backlog - Local Desktop Assistant

Updated: 2026-04-05
Status values: TODO | DOING | DONE | BLOCKED
Ownership: `A27+` = AI-executable repo work for modularization | `Pxx` = manual or off-repo work tracked in `tasks/task-people.md`

## How To Use This File

- This file is the phased modularization backlog derived from the restructure plan.
- Use `tasks/ai-dev-system-unification-backlog.md` for the separate root-unification tracker tied to `tao_lai_agent.md`.
- Treat every entry here as planned phase work unless it is explicitly marked `DONE`, `DOING`, or `BLOCKED`.
- The active assistant runtime now lives in `local-backend/` plus `ai-dev-system/clients/unity-client/`.
- Do not describe future module folders or later domain or asset moves as shipped behavior until verified code changes land.
- Use `tasks/task-queue.md` for the active queue and `tasks/done.md` for completed history with evidence.

## Lane Map

- `Shell/Shared UX`: shell composition, navigation, health-state rendering, shared shell contracts
- `Planner`: schedule views, planner state ownership, planner-facing backend shaping
- `Chat/Runtime`: chat state, transcript flow, fallback, subtitles, reconnect, event routing
- `Avatar/Customization`: avatar contract, placeholder-safe production path, wardrobe and asset data
- `Governance/Cross-cutting`: baseline capture, docs sync, task governance, validation automation, repo-structure evaluation

## Phase Summary

- `Phase 0`: baseline freeze and evidence capture
- `Phase 1`: shared-core and early boundary setup inside the current `ai-dev-system/clients/unity-client/` tree
- `Phase 2`: shell extraction and route mounting
- `Phase 3`: planner hardening and backend integration cleanup
- `Phase 4`: chat isolation and cross-module event flow
- `Phase 5`: avatar integration contract
- `Phase 6`: wardrobe plus asset-data foundation
- `Phase 7`: docs, task governance, validation, and repo-structure evaluation

## Backlog

- `A27 | DONE | Phase 0 | Lane: Governance/Cross-cutting | Purpose: execute the audit-and-freeze baseline before structural refactors begin. | Scope: capture current implementation truth, blockers, placeholder areas, and verification evidence in migration docs and trackers. | Depends on: none. | Acceptance: `docs/migration/phase0.md` exists, cites the active runtime in `local-backend/` plus `ai-dev-system/clients/unity-client/`, preserves the historical backend `62 passed` note, records the latest Unity verification notes, and separates current implementation from design-target behavior.`
- `A28 | DONE | Phase 1 | Lane: Shell/Shared UX | Purpose: create the initial shared-core boundary inside the current Unity tree. | Scope: add shared contracts, a lightweight event bus, state ownership rules, and reusable shell-stage helpers without moving repo roots. | Depends on: A27. | Acceptance: shared contracts compile in the current Unity project, module-facing interfaces exist for Shell plus Planner plus Chat or Settings integration, and Unity verification on 2026-04-04 refreshed coverage with EditMode `25 passed` and PlayMode `35 passed`.`
- `A29 | DONE | Phase 1 | Lane: Chat/Runtime | Purpose: extract early chat-facing boundaries while keeping the existing chat controller and UXML stable. | Scope: move chat state ownership and request creation toward `IChatModule` plus `ChatRequestFactory` without changing backend route semantics. | Depends on: A27 A28 A10. | Acceptance: chat still loads through the shell, transcript and mic affordances remain intact, and Unity verification on 2026-04-04 refreshed coverage with EditMode `26 passed` and PlayMode `35 passed`.`
- `A30 | DONE | Phase 2 | Lane: Shell/Shared UX | Purpose: isolate shell-only composition and route mounting behind the shell boundary. | Scope: move shell navigation, health-state rendering, panel mount points, and overlay ownership behind `IShellModule` while keeping Home or Schedule or Settings behavior stable. | Depends on: A28 A29 A04 A05 A07. | Acceptance: shell navigation and shell-owned state now flow through `IShellModule`, routes still mount correctly, and Unity verification on 2026-04-04 refreshed coverage with EditMode `26 passed` and PlayMode `39 passed`.`
- `A31 | DOING | Phase 3 | Lane: Planner | Purpose: harden the planner boundary around the existing list-first schedule implementation. | Scope: keep schedule refs, controllers, route ownership, and Today or Week or Inbox or Completed behavior inside planner-owned boundaries before any future calendar-grid work. | Depends on: A28 A30 A09. | Acceptance: planner internals are no longer owned by shell code, the current list-first schedule behavior still works under repo-side coverage, and `P02` live smoke closes the remaining planner sign-off gap.`
- `A32 | DONE | Phase 3 | Lane: Planner | Purpose: establish typed backend-integration boundaries for planner-facing transport and model shaping. | Scope landed in `ai-dev-system/clients/unity-client/Assets/Scripts/Tasks/PlannerBackendIntegration.cs`, `TaskViewModelStore`, `HomeScreenController`, `ScheduleScreenController`, and `AssistantApp` so planner-facing task transport now maps through typed planner snapshots and mutation results instead of leaking raw backend DTO shaping through UI code. | Depends on: A28 A29 A31. | Acceptance: typed API boundaries exist for the current backend endpoints, UI controllers rely less on ad-hoc DTO shaping, and Unity verification on 2026-04-05 refreshed coverage with EditMode `28 passed`, PlayMode `41 passed`, plus zero project-code console errors after refresh.`
- `A33 | DONE | Phase 4 | Lane: Chat/Runtime | Purpose: finalize chat isolation and enhancement after the early chat boundary setup. | Scope landed in `ai-dev-system/clients/unity-client/Assets/Scripts/Features/Chat/ChatModuleContracts.cs`, `ChatModule.cs`, `ai-dev-system/clients/unity-client/Assets/Scripts/Chat/ChatViewModelStore.cs`, and `ai-dev-system/clients/unity-client/Assets/Scripts/Core/AssistantApp.cs` so chat-owned APIs now reduce text-turn start, compatibility replies, assistant-stream transcript or chunk or final events, and planner-action summaries before shell refresh or subtitle playback. | Depends on: A29 A32 A10. | Acceptance: chat state is chat-owned, streaming and fallback remain functional, transcript and diagnostics rendering are unified, and Unity verification on 2026-04-05 refreshed coverage with EditMode `30 passed`, PlayMode `42 passed`, plus zero project-code console errors after refresh.` 
- `A34 | DOING | Phase 4 | Lane: Chat/Runtime | Purpose: move cross-module runtime handoff onto shared event flow instead of direct feature coupling. | Scope landed so far in `ai-dev-system/clients/unity-client/Assets/Scripts/Core/AppModuleEvents.cs`, `AssistantApp.cs`, and `Assets/Scripts/Audio/AudioPlaybackController.cs` where planner screen or date or task-action requests now publish through `AssistantEventBus`, subtitle visibility is bus-driven, and runtime conversation or backend avatar-state signals are forwarded through shared event contracts before reaching presenter or avatar components. | Depends on: A28 A29 A31 A32 A33 A11. | Acceptance: cross-module flows are event-driven, reminder or subtitle or avatar-state behavior still works, and repo-side coverage plus `P02` live smoke validate the routing path. Current repo-side evidence on 2026-04-05: Unity verification refreshed with EditMode `30 passed`, PlayMode `43 passed`, and zero project-code console errors after refresh; remaining closure work is the `P02` live smoke sign-off.` 
- `A35 | BLOCKED | Phase 5 | Lane: Avatar/Customization | Purpose: implement the avatar-experience boundary while preserving placeholder fallback until approved assets arrive. | Scope: define the avatar controller contract, integrate conversation-state hooks, and document the shell-to-avatar loading path required for production avatars. | Depends on: A28 A30 A34 P04. | Acceptance: a documented and testable avatar contract exists, placeholder fallback remains functional, and production completion stays blocked until `P04` provides signed-off assets and expectations.`
- `A36 | TODO | Phase 6 | Lane: Avatar/Customization | Purpose: implement wardrobe plus asset-data foundations on top of the avatar contract. | Scope: define item categories, equip rules, preset data, sample customization flow, and asset-validation conventions that can work on placeholder or sample assets first. | Depends on: A28 A35. | Acceptance: wardrobe metadata and preset flow are formalized, placeholder or sample assets can be equipped through a defined contract, and production-only completion remains clearly blocked by `P04`.`
- `A37 | DOING | Phase 7 | Lane: Governance/Cross-cutting | Purpose: rewrite docs around landed module boundaries and migration phases. | Scope: maintain module docs, migration docs, as-is references, roadmap or index navigation, and design-target labeling so code and docs stay aligned. | Depends on: A27 through the latest landed modularization slice. | Acceptance: docs distinguish current implementation from planned work, each landed module has a maintained doc, and no design-target page is left phrased like shipped behavior. Current progress: `docs/roadmap.md`, `docs/index.md`, and README-level navigation are being added to improve repo entry and doc discoverability without changing runtime truth sources.`
- `A38 | TODO | Phase 7 | Lane: Governance/Cross-cutting | Purpose: keep task governance aligned with phased modularization work. | Scope: standardize phased task fields, keep AI and manual dependencies aligned, and keep active versus historical tracker responsibilities clear. | Depends on: A27 and each landed phase task. | Acceptance: task trackers point to the active migration phase, manual blockers stay mapped to the right AI tasks, and completed slices move into `tasks/done.md` instead of lingering in the queue.`
- `A39 | DOING | Phase 7 | Lane: Governance/Cross-cutting | Purpose: validate and harden the landed Unity root move into `ai-dev-system/clients/unity-client/`. | Scope: keep docs, scripts, Unity references, and validation flow aligned with the moved client path while `P02` closes the remaining live smoke gap. | Depends on: A30 A31 A33 A35 A37 A38 P02. | Acceptance: the absorbed client path is the only current Unity root used by repo docs and scripts, and Unity validation evidence shows the move does not break the active runtime.`
- `A40 | TODO | Phase 7 | Lane: Governance/Cross-cutting | Purpose: add repeatable validation that catches task or doc drift during migration. | Scope: provide a command or script that validates task-format expectations, migration-doc presence, and stale references between docs, trackers, and code paths. | Depends on: A37 A38. | Acceptance: a repeatable validation command exists, runs from repo state without manual editing, and catches at least task-file or doc-link mismatches relevant to the migration backlog.`

## Sequencing Notes

- `A27` is the audit-and-freeze gate; later slices should continue to respect the Phase 0 baseline recorded in `docs/migration/phase0.md`.
- `A28`, `A29`, and `A30` are completed groundwork and should stay out of the active queue except as dependency references.
- The Unity tree now lives under `ai-dev-system/clients/unity-client/`; further root-layout churn should be justified explicitly in `A39`.
- Do planner work as list-first hardening before promising an interactive calendar grid.
- Treat avatar and wardrobe work as placeholder-safe planning and boundary work until `P04` delivers approved production assets.
- Keep docs and tracker governance moving alongside implementation so design-target drift does not return.

