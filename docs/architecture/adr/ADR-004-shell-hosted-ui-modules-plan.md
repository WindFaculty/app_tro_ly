# ADR-004: Keep Future UI Expansion As Shell-Hosted Feature Surfaces

- Status: Accepted as a planning baseline
- Date: 2026-04-05

## Context

- The requested UI expansion asks for Character Space, Planner, Chat, and Wardrobe as first-class surfaces.
- The current shipped shell already has a stable four-zone runtime inside `unity-client/` with Home in the center stage, Schedule in the bottom sheet, Chat in the right panel, and Settings in a drawer.
- The modularization freeze does not allow a second runtime root, a second shell truth source, or net-new feature implementation outside tracked scope.
- The repo already has feature boundaries for Home, Schedule, Chat, Settings, and avatar outfit contracts that later UI work should evolve instead of duplicating.

## Decision

- Keep the requested UI expansion inside the current Unity shell rather than treating the four requested surfaces as separate apps.
- Plan future shell routes around four primary surfaces: `CharacterSpace`, `Planner`, `Chat`, and `Wardrobe`.
- Keep those future shell-surface routes distinct from the planner's existing `AppScreen` enum for `Today`, `Week`, `Inbox`, and `Completed`.
- Reuse and evolve the current feature boundaries:
  - Home or stage ownership becomes the basis for Character Space
  - Schedule becomes the basis for Planner
  - Chat remains the single chat-state owner even if a compact embedded panel survives later
  - Wardrobe must consume the avatar outfit contract and registry boundary instead of bypassing them
- Keep Settings as a drawer and reminder or subtitle UI as overlays instead of turning them into competing primary routes.

## Options Considered

### Add four separate mini-apps

- Rejected because that would duplicate shell composition, state ownership, and design-system work.

### Leave the current shell untouched and keep adding more content to existing Home, Schedule, and Chat surfaces

- Rejected because the requested expansion needs clearer route ownership and would otherwise keep shell and feature responsibilities blurred.

## Consequences

- Future UI work has a clearer route and ownership map without pretending the routes already exist.
- Later implementation can stay inside the current `unity-client/` tree during the freeze.
- Shell routing, planner sub-views, and avatar customization can evolve without creating a second source of truth.
- Any later implementation that changes runtime boundaries or event contracts must still update current-state docs, `docs/06-decisions.md`, and task trackers in the same slice.
