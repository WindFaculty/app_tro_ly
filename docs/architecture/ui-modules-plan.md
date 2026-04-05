# UI Modules Plan

Updated: 2026-04-05
Status: Planned work for future shell-hosted UI surfaces

This document is the Phase 0 planning baseline for the requested Character Space, Planner, Chat, and Wardrobe UI expansion.

It does not describe shipped runtime behavior. Current implementation truth still lives in `docs/02-architecture.md`, `docs/04-ui.md`, `unity-client/Assets/Resources/UI/Shell/AppShell.uxml`, and the current Unity feature modules.

## Purpose

- define how the four requested UI surfaces should fit into the existing Unity shell
- keep the future UI work inside the current `unity-client/` tree during the freeze
- avoid creating four disconnected apps, duplicate stores, or a second design-system source of truth
- give later implementation phases a stable route map, ownership map, and shared-component list

## Current Implementation

- The shipped shell is still a four-zone layout with a left control rail, center-stage Home surface, bottom planner sheet, right chat panel, settings drawer, subtitle overlay, and reminder overlay.
- `AppShell.uxml` mounts `HomeScreen.uxml` in the center stage, `ScheduleScreen.uxml` in the bottom sheet, and `ChatPanel.uxml` in the right panel.
- `AppShellController` currently owns shell focus, planner-sheet visibility, chat visibility, and settings-drawer visibility.
- `AppScreen` is currently the planner-owned enum for `Today`, `Week`, `Inbox`, and `Completed`. It is not a shell-wide surface router yet.
- `HomeModule`, `PlannerModule`, `ChatModule`, and `SettingsModule` are the current feature boundaries.
- `AvatarOutfitApplicationService` and `AvatarAssetRegistryDefinition` exist as placeholder-safe wardrobe foundations, but no wardrobe shell UI is shipped.

## Non-Goals For This Phase 0 Slice

- no runtime route implementation
- no new UXML, USS, or controller files for the four requested surfaces
- no claim that Character Space, full-screen Planner, full-screen Chat, or Wardrobe already exist in the live shell
- no production-avatar or production-wardrobe completion claim

## Planned Navigation Model

### Planned route identities

The future shell should treat the four requested surfaces as primary shell routes:

| Planned route | Purpose | Current baseline it evolves from |
| --- | --- | --- |
| `CharacterSpace` | Hero surface for avatar, room, and interaction hotspots | current center-stage `Home` surface |
| `Planner` | Primary planning surface for task summaries, filters, and detail workflows | current bottom-sheet `Schedule` surface |
| `Chat` | Full conversation surface with composer, transcript preview, and context cards | current right-side `Chat` panel |
| `Wardrobe` | Avatar customization surface for slot and item workflows | current avatar outfit application contract only |

### Route-contract rule

- Planned work should add a shell-surface route contract that is separate from the planner's current `AppScreen` enum.
- Planned work should use one typed shell-route request contract instead of direct feature-to-feature controller calls.
- Planned work should keep `Settings` as a drawer surface and keep subtitle or reminder UI as overlays, not primary routes.

Recommended shape for later implementation:

- `ShellSurfaceRoute`: `CharacterSpace`, `Planner`, `Chat`, `Wardrobe`
- `ShellSurfaceRequestedEvent`: carries the target surface plus optional context payload

That keeps shell routing distinct from planner sub-views such as `Today`, `Week`, `Inbox`, and `Completed`.

## Shell Region Map

| Planned route | Center stage | Right utility panel | Shell notes |
| --- | --- | --- | --- |
| `CharacterSpace` | room viewport, avatar stage, hotspot layer, subtitle overlay, quick actions | character status, current activity, quick actions, environment info | planner sheet should not remain the primary planner UX once this route exists |
| `Planner` | planner summary, filters, main task canvas, quick add | selected-task detail, planner insights, task actions | current shell-owned planner insight copy should migrate behind the planner boundary |
| `Chat` | transcript thread, assistant state, composer, transcript preview | context cards, prompt suggestions, task-action confirmations, memory snippets | embedded mini chat can remain optional later, but the route owns the main chat experience |
| `Wardrobe` | avatar preview, outfit summary, preview controls | slot tabs, item grid, item detail, equip or reset actions | must stay placeholder-safe until `P04` approves production-avatar content |

## Screen Ownership Map

| Planned surface | Planned module owner | Planned presentation controller | Existing code that should be evolved instead of duplicated |
| --- | --- | --- | --- |
| `CharacterSpace` | `unity-client/Assets/Scripts/Features/CharacterSpace/` | `CharacterSpaceScreenController` | current `Features/Home/` stage ownership and avatar-state presentation |
| `Planner` | `unity-client/Assets/Scripts/Features/Planner/` | `PlannerScreenController` | current `Features/Schedule/` boundary, `TaskViewModelStore`, and planner task commands |
| `Chat` | `unity-client/Assets/Scripts/Features/Chat/` | `ChatScreenController` | current `ChatModule`, `ChatPanelController`, `ChatViewModelStore`, and chat application services |
| `Wardrobe` | `unity-client/Assets/Scripts/Features/Wardrobe/` | `WardrobeScreenController` | current `AvatarOutfitApplicationService` and `AvatarAssetRegistryDefinition` |

### Ownership rules

- Character Space should replace the current Home stage boundary rather than becoming a second stage owner.
- Planner should absorb future planner insight and task-detail UI instead of leaving that logic split between shell copy and planner code.
- Chat should keep a single chat-state authority even if the shell later keeps a compact embedded panel.
- Wardrobe should consume the existing avatar outfit contract and registry boundary instead of reading scene objects or folders directly.
- `AssistantApp` remains the top-level coordinator until later implementation phases extract more runtime wiring. Phase 0 does not remove it.

## Planned Route Transitions

| Trigger source | Planned route target | Notes |
| --- | --- | --- |
| left rail or future top navigation | requested surface | shell-owned navigation should publish a shell-route request instead of calling feature controllers directly |
| Character Space quick action `Open planner` | `Planner` | replaces the current dependence on a permanently visible planner sheet |
| Character Space quick action `Talk` | `Chat` | opens the main chat surface |
| Character Space quick action `Wardrobe` | `Wardrobe` | stays placeholder-safe until outfit content and `P04` are ready |
| planner task action `Open in chat` | `Chat` | should pass task context through the planned shell-route request payload |
| Wardrobe close or apply flow | `CharacterSpace` | returns to the avatar-centered hero surface |

## Shared Components To Standardize

The later implementation phases should build shared shell components from the existing style sources in `Assets/Resources/UI/Styles/` rather than creating a second design-system source.

Required shared components:

- top bar or route rail
- screen header row
- panel header
- empty-state card
- loading-state card
- error-state card
- action button row
- status badge
- section card
- center-stage split layout helper
- right-utility panel wrapper

Current reuse baseline:

- tokens: `Tokens.uss`
- layout helpers: `Layout.uss`
- button styling: `Buttons.uss`
- card styling: `Cards.uss`

## Migration Notes

- The future shell-surface router should live under the shell boundary and should not overload the planner-specific `AppScreen` enum.
- Planner implementation work should reuse the existing `PlannerModule`, `PlannerTaskCommandApplicationService`, and `TaskViewModelStore` rather than rebuilding planner data flow from scratch.
- Chat implementation work should reuse the current `ChatModule`, `ChatTurnApplicationService`, and `ChatViewModelStore` so transcript, diagnostics, and task-action summaries stay chat-owned.
- Character Space work should build on the current Home or stage ownership plus avatar-state presentation paths instead of creating a second stage store.
- Wardrobe work can start with placeholder or sample data, but any production-ready claim remains blocked by `P04`.
- Any later implementation that changes module boundaries or shared event contracts must update current-state docs, `docs/06-decisions.md`, and an ADR in the same task.

## Manual Gates For Later Phases

- `P02` is required before route, layout, or interaction changes can be described as smoke-verified in Unity or packaged-client runs.
- `P04` is required before production-avatar or production-wardrobe claims.

## Phase 0 Deliverables Captured Here

- navigation map for `CharacterSpace`, `Planner`, `Chat`, and `Wardrobe`
- shell-region map for center stage and right utility usage
- ownership map for future module and controller boundaries
- shared-component list for the shell-wide UI foundation
