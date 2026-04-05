# Migration Phase 0 Baseline

Updated: 2026-04-04
Status: Current implementation baseline captured before module-boundary work begins

## Purpose

This document is the Phase 0 audit-and-freeze record for the modularization backlog in `tasks/module-migration-backlog.md`.

It captures:

- current implementation truth
- active blockers and manual gates
- placeholder or partial areas that still exist in code
- latest verification evidence available from terminal and tracker history
- the specific gaps that existed between code, docs, and task trackers before this baseline was refreshed

## Runtime Baseline

### Current implementation

- The required assistant runtime still lives in `local-backend/` plus `unity-client/`.
- `local-backend/app/api/routes.py` remains the API truth for health, task, chat, speech, settings, and stream routes.
- `unity-client/Assets/Resources/UI/MainUI.uxml` still boots the UI Toolkit client through `Shell/AppShell.uxml`.
- `unity-client/Assets/Scripts/App/`, `unity-client/Assets/Scripts/Core/`, and `unity-client/Assets/Scripts/Features/` remain the client-side truth for shell routing, typed refs, view-model ownership, and screen controllers.
- `agent-platform/` is still an optional subsystem and is not required for the assistant runtime.

### Current module state

- The repo does not yet ship the planned module folders or root-level `clients/unity/` move described in the restructure plan.
- The current shell already has explicit Home, Schedule, Settings, chat, subtitle, and reminder surfaces inside the existing `unity-client/` tree.
- Modularization work after this phase must start by defining boundaries inside the current tree rather than by moving repo roots first.

## Active Blockers And Gates

### Repo-side blocker

- Backend automated verification is not fully green on the latest terminal rerun. `python -m pytest -q` in `local-backend/` on 2026-04-04 reported `69 passed, 1 failed`.
- The failing test is `tests/test_tts_service.py::test_chattts_synthesize_writes_cached_wav`.
- The observed failure is a ChatTTS mock-signature mismatch after `TtsService._load_chattts()` passed `source=\"huggingface\"` into `chat.load(...)`.

### Manual validation required

- `P02` is still the main manual gate for Unity smoke, packaged-client behavior, degraded-mode readability, and future module-extraction smoke in the real client.
- `P04` is still the manual gate for a production-ready avatar asset handoff and final avatar integration expectations.
- Unity verification notes below are tracker-backed evidence from prior editor runs, not a Unity rerun performed in this terminal session.

## Placeholder And Partial Areas

### Current implementation

- Home is no longer a bare placeholder card, but the center avatar stage still uses placeholder presentation copy until a production scene binding is finalized.
- Schedule is now list-first and card-based inside `ScheduleScreen.uxml`; it is still not a full calendar-grid experience.
- The right-side schedule panel in `AppShell.uxml` is still a shell-owned helper surface with static advisory cards rather than a fully dynamic planner insights module.
- The production avatar path in `Assets/AvatarSystem/` still depends on scene-level setup and manual validation.
- The compact mini-assistant mode remains unimplemented.

## Verification Evidence

### Historical evidence still worth preserving

- `tasks/done.md` and the pre-refresh docs recorded a backend verification run on 2026-03-26 in `local-backend/` with `pytest -q`: `62 passed`.
- `tasks/task-people.md` records `P01` as completed on 2026-03-28 with Unity EditMode `21 passed` and PlayMode `33 passed`.
- `tasks/task-people.md` also records the latest Unity rerun notes under `P02`, including a full PlayMode rerun on 2026-03-29 with `35 passed` plus packaged-client and Game-view evidence artifacts.

### Latest terminal evidence for this baseline

- Backend rerun on 2026-04-04 from `local-backend/`: `python -m pytest -q`
- Result: `69 passed, 1 failed`
- Failure focus: `tests/test_tts_service.py::test_chattts_synthesize_writes_cached_wav`
- Manual validation required: Unity Editor and packaged-client checks were not rerun from this terminal session during Phase 0 capture.

## Gap Audit

| Area | Code truth at baseline | Drift found before refresh | Phase 0 action |
| --- | --- | --- | --- |
| Home UI | `HomeScreen.uxml` now renders an orbit-style shell with task cards, quick add, status cards, and a placeholder stage surface | `docs/04-ui.md` still described Home mostly as a placeholder avatar card with text snapshots | Refreshed UI docs and README summary |
| Schedule UI | `ScheduleScreen.uxml` plus `ScheduleScreenController.cs` render a list-first schedule canvas with day cards, inbox/completed flows, and date navigation | Older docs still framed Schedule mainly as a text placeholder panel | Refreshed architecture/UI docs to describe list-first current behavior |
| Chat header status | `ChatPanel.uxml` now shows themed shell copy with route/status cards; the old fixed `GPT-4.0` badge wording is no longer true | `docs/04-ui.md` still called out the old badge as a current placeholder | Replaced with current chat-state description |
| Verification snapshot | Fresh backend evidence is no longer a clean `62 passed` state | `README.md`, `docs/02-architecture.md`, and `docs/08-architecture-as-is.md` still implied the older backend snapshot was the current terminal truth | Updated verification sections and linked the new baseline context |
| Tracker state | `A27` is now executed, while later modularization phases remain planned | `tasks/task-queue.md` still treated `A27-A40` as uniformly planned | Updated queue and backlog status to separate completed Phase 0 from later planned slices |

## Phase 0 Exit State

- `A27` is complete as a documentation and tracker baseline task.
- The active runtime is still `local-backend/` plus `unity-client/`.
- `A28` and later modularization slices remain planned work.
- The repo should not be described as having shipped module folders or a root-layout move yet.
- Future migration work should account for the currently failing backend ChatTTS test instead of assuming a fully green automation baseline.
