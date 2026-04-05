# ADR-002: Avatar Asset Registry And Metadata Boundary

- Status: Accepted
- Date: 2026-04-05

## Context

- The repo already had avatar item, preset, and conflict-rule ScriptableObject types, plus runtime equip logic and editor validators.
- Asset discovery and validation were still fragmented, and Phase 4 requires a standard asset contract plus a single registered catalog instead of UI or tools reading folders ad hoc.
- Production avatar completion remains blocked by `P04`, so the change must stay placeholder-safe and avoid claiming shipped wardrobe UI.

## Decision

- Keep `AvatarItemDefinition`, `OutfitPresetDefinition`, and `ConflictRuleDefinition` as the metadata primitives.
- Add `AvatarAssetRegistryDefinition` as the catalog boundary for allowed items, presets, and conflict rules.
- Extend metadata with explicit `requiredBaseVersion`, `bodyTypeId`, and `presetId` fields so compatibility and registry identity are not implied by folder names alone.
- Let application code load saved outfits through the registry catalog instead of requiring callers to pass arbitrary item arrays.
- Add registry validation to the existing avatar validator menu flow.

## Options Considered

### Keep folder scanning as the discovery mechanism

- Rejected because it lets UI or tools discover content that has not been reviewed or validated.

### Create a second customization system outside `Assets/AvatarSystem/`

- Rejected because it would create a competing source of truth during the modularization freeze.

## Consequences

- Customization data now has a single catalog shape for future shell or wardrobe work.
- Docs can point to one registry owner instead of a folder scan convention.
- Production content and final avatar integration are still not complete; `P04` remains the approval gate.
