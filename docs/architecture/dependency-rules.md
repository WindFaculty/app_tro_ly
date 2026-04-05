# Dependency Rules - Phase 1

Updated: 2026-04-05
Status: Current implementation rules for modularization inside the existing repo tree

## Core Rules

- Trust the current repo roots: keep required runtime work inside `unity-client/` plus `local-backend/`.
- Prefer module interfaces over feature-internal controllers when a module boundary exists.
- Prefer feature application services over inline orchestration in `AssistantApp` once a flow has a stable feature boundary.
- `Core/` may host shared contracts and refs, but feature-owned state or rules should move behind feature modules instead of growing inside `Core/`.
- Use backend APIs or typed integrations for persistence-backed changes; Unity feature modules must not bypass backend ownership.
- Shared event flow should be preferred over direct cross-feature controller calls for planner or chat or subtitle or avatar handoff.

## Unity Rules By Area

### `Assets/Scripts/Core/AssistantApp.cs`

- Allowed:
  - compose runtime objects
  - own backend clients and process-lifetime wiring
  - route feature-level events between modules
- Not allowed:
  - own feature-internal toggle mutation logic when a feature module can own it
  - call feature-internal stores directly when the module boundary already exposes what it needs
  - format feature-specific quick-add, chat transport, or planner-mutation wording inline when a feature application service already exists

### `Assets/Scripts/Features/Home/`

- Allowed:
  - center-stage Home rendering
  - quick-add input capture
  - Home-owned quick-add command or status wording through `HomeQuickAddApplicationService`
- Not allowed:
  - direct backend transport calls
  - direct planner store mutation
  - direct chat transport routing

### `Assets/Scripts/App/`

- Allowed:
  - shell composition and shell-state rendering
  - focus, drawer, and planner-sheet visibility control
- Not allowed:
  - planner data shaping
  - chat transcript reduction
  - settings toggle mutation

### `Assets/Scripts/Features/Schedule/`

- Allowed:
  - planner interaction flow
  - planner screen ownership
  - planner-facing requests and rendering
  - planner task-mutation application services that wrap typed backend integrations
- Not allowed:
  - direct calls into chat or settings controllers
  - shell drawer state ownership

### `Assets/Scripts/Features/Chat/`

- Allowed:
  - transcript state ownership
  - chat diagnostics and task-action summary rendering
  - send or mic request publication
  - chat-turn request planning for compatibility or streaming transport
- Not allowed:
  - direct planner mutation
  - direct settings mutation
  - direct shell focus changes

### `Assets/Scripts/Features/Settings/`

- Allowed:
  - settings snapshot ownership within the Unity client
  - dirty-state tracking
  - settings drawer rendering and reload or save request publication
- Not allowed:
  - direct backend transport calls
  - direct chat transcript mutation except through coordinator reactions
  - planner or shell-state ownership

### `Assets/Scripts/Avatar/` and `Assets/AvatarSystem/`

- Allowed:
  - presentation and scene-bridge behavior
  - avatar-state reactions to shared events
  - placeholder-safe outfit application contracts layered on top of `AvatarSystem`
- Not allowed:
  - direct task or settings persistence calls
  - direct planner or chat store ownership

## Backend Rules

- `local-backend/app/api/routes.py` should route into services rather than absorbing business logic.
- `local-backend/app/services/` owns assistant, task, planner, speech, scheduler, and settings behavior.
- `local-backend/app/db/repository.py` stays behind services and container wiring instead of becoming a route-layer dependency.

## Public Entry Guidance

- Prefer importing feature boundaries through:
  - `ShellModule` or `IShellModule`
  - `PlannerModule` or `IPlannerModule`
  - `ChatModule` or `IChatModule`
  - `SettingsModule` or `ISettingsModule`
- Avoid new direct dependencies from `AssistantApp` into feature-internal state or UI handlers when the boundary already exists.
