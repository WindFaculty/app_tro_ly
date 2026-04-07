# AI Dev System Unification Phase 5

Updated: 2026-04-07
Status: Current implementation updated so shared avatar, customization, and room contracts now live under `ai-dev-system/domain/`

## Purpose

This document records the Phase 5 domain pass for the `ai-dev-system` migration plan.

The goal of this phase is to establish `domain/` as the current source of truth for shared avatar, customization, and room contracts before any deeper asset-pipeline or workbench migration.

## What Changed In Phase 5

### Current implementation

- `ai-dev-system/domain/` now contains grounded contract ownership docs for:
  - `avatar/`
  - `customization/`
  - `room/`
  - `shared/`
- The avatar domain now documents the live shell-to-avatar runtime contract backed by:
  - `ai-dev-system/clients/unity-client/Assets/Scripts/App/AppCompositionRoot.cs`
  - `ai-dev-system/clients/unity-client/Assets/Scripts/Runtime/AvatarRuntime.cs`
  - `ai-dev-system/clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarConversationBridge.cs`
- The customization domain now owns:
  - the current slot taxonomy snapshot
  - a planned JSON item-manifest schema for future cross-system use
  - a checked-in snapshot of the current avatar item asset data present in the repo
  - a validator-rules summary grounded in the current Unity editor validator
- The room domain now owns a current snapshot of camera-focus presets and page-context mapping.

### Planned work still not done

- Unity runtime code and assets still execute from `ai-dev-system/clients/unity-client/Assets/...`; they were not moved into `domain/` in this phase.
- `wardrobe.equipItem` remains a typed command without runtime item-registry wiring in Unity.
- `ai-dev-system/asset-pipeline/` is still planned-only; validator and import tooling has not been migrated there yet.
- Production avatar completion is still blocked by `P04` in `tasks/task-people.md`.
- The slot normalization proposed in `tao_lai_agent.md` is not fully implemented; current code still uses `Bottom`, `Dress`, `BraceletL`, and `BraceletR`.

## Source-Of-Truth Shift

| Before Phase 5 | After Phase 5 |
| --- | --- |
| Shared avatar or customization or room contracts were only implied by Unity runtime code and scattered docs. | `ai-dev-system/domain/` is now the current source of truth for shared contract docs, taxonomy, and metadata snapshots. |
| Avatar, customization, and room ownership boundaries were mostly described in trackers or migration notes. | The ownership boundary now lives in subsystem-local domain docs under `ai-dev-system/domain/`. |

## Acceptance Check

Phase 5 is satisfied in repo state when:

- `ai-dev-system/domain/` clearly separates `avatar/`, `customization/`, `room/`, and `shared/`
- those domain files point back to the exact current Unity truth sources instead of inventing new runtime behavior
- subsystem docs and task trackers describe `domain/` as current contract truth without claiming Unity assets already moved there
- planned-only gaps such as production avatar sign-off and wardrobe runtime lookup remain clearly labeled

## Verification Evidence

- Static repo inspection confirmed the live Unity truth sources referenced by the new domain files:
  - `Assets/Scripts/App/AppCompositionRoot.cs`
  - `Assets/Scripts/Runtime/AvatarRuntime.cs`
  - `Assets/Scripts/Runtime/RoomRuntime.cs`
  - `Assets/AvatarSystem/Core/Scripts/Data/AvatarItemDefinition.cs`
  - `Assets/AvatarSystem/Core/Scripts/Data/OutfitPresetDefinition.cs`
  - `Assets/AvatarSystem/AvatarProduction/Editor/Validators/AvatarValidator.cs`
- Phase 5 JSON files were parsed successfully with PowerShell `ConvertFrom-Json`.
- Runtime verification beyond static inspection remains manual because Unity Editor or player execution is required and was not available from terminal work in this change.
