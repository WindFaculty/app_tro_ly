# Phase 1 Boundary Audit

Updated: 2026-04-05
Status: Current implementation audit after the first dependency-cleanup slice

This audit records real code observations from the current repo. It is not a target-state wish list.

## Fixed In This Slice

| File or area | Previous issue | Phase 1 action |
| --- | --- | --- |
| `unity-client/Assets/Scripts/Core/AssistantApp.cs` | Directly owned `SettingsScreenController`, settings toggle mutation handlers, and settings-dirty UI refresh logic | Moved settings UI ownership and toggle mutation behind `ISettingsModule` plus `SettingsModule` |
| `unity-client/Assets/Scripts/Features/Settings/` | Had a controller but no module boundary parallel to Shell, Planner, and Chat | Added `SettingsModule` plus `ISettingsModule` so Settings now has a public boundary |

## Remaining Violations And Risk Areas

### Level A - Needs continued refactor

- `unity-client/Assets/Scripts/Core/AssistantApp.cs`
  - Still coordinates startup, backend transport, voice capture, streaming, planner mutations, settings I/O, subtitle events, avatar-state bridging, and shell refresh.
  - This is still the main runtime "god file" even after the Settings extraction.

### Level B - Should be cleaned in current modularization work

- `unity-client/Assets/Scripts/Core/SettingsViewModelStore.cs`
  - Current ownership is settings-specific, but the file still lives under `Core/`.
  - Keep it there temporarily until the broader shared-core cleanup can move or replace it safely.

- `unity-client/Assets/Scripts/Core/AssistantUiRefs.cs`
  - Still aggregates refs for all screen areas into one runtime composition object.
  - Acceptable for now, but it keeps multiple feature surfaces coupled at composition time.

### Level C - Accept during transition

- `unity-client/Assets/Scripts/App/AppCompositionRoot.cs`
  - Still composes UI, audio, avatar, and bridge objects together.
  - Acceptable because it is composition-only and does not currently absorb feature business rules.

- `unity-client/Assets/Scripts/Core/ShellStageSnapshot.cs`
  - Still consumes multiple feature-facing state sources to render shell-stage summaries.
  - Acceptable because it reads read-only contracts instead of mutating feature state directly.

### Level D - Keep as-is for now

- `local-backend/app/api/routes.py`
  - Still serves as the route truth source and delegates behavior into services.
  - No new Phase 1 split is required until a concrete backend coupling issue appears.

## Next Practical Targets

- Continue shrinking `AssistantApp` by moving more feature-owned reactions behind module APIs or shared event handlers.
- Revisit `SettingsViewModelStore` placement once the repo is ready for a broader `Core/` cleanup without inventing a future folder layout early.
- Keep new dependency rules tied to actual module boundaries that exist today rather than the future `src/modules/...` layout from the original plan.
