# Avatar Asset Intake Checklist

Use this checklist before treating a new avatar item, preset, or rule asset as part of the current customization foundation.

## Item Intake

- Asset name follows the naming rules in [avatar-asset-spec.md](avatar-asset-spec.md).
- Source mesh or prefab lives under the correct `AvatarProduction/Parts/` slot folder.
- A matching `AvatarItemDefinition` exists under `AvatarProduction/Data/ItemDefinitions/`.
- `itemId`, `displayName`, `slotType`, `requiredBaseVersion`, and `bodyTypeId` are filled.
- `blocksSlots`, `requiresSlots`, `compatibleTags`, `incompatibleTags`, and `hideBodyRegions` are set where needed.
- Attachment fields are filled when the item uses a bone or socket anchor.
- The item is added to the correct `AvatarAssetRegistryDefinition`.
- `Tools > AvatarSystem > Validate All Item Definitions` passes without new errors.
- `Tools > AvatarSystem > Validate Asset Registries` passes without new errors.

## Preset Intake

- Preset asset lives under `AvatarProduction/Presets/OutfitPresets/`.
- `presetId`, `presetName`, and `requiredBaseVersion` are filled.
- Each preset slot references an item with the matching `slotType`.
- Dress versus Top or Bottom combinations are intentional and validation-clean.
- The preset is registered in `AvatarAssetRegistryDefinition`.
- `Tools > AvatarSystem > Validate Outfit Presets` passes without new errors.

## Rule Intake

- Conflict rule asset lives under `AvatarProduction/Data/ConflictRules/`.
- The rule does not require or block its own source slot unintentionally.
- The rule is registered in `AvatarAssetRegistryDefinition`.
- Registry validation passes after the rule is added.

## Manual Validation Required

- Unity scene smoke for visual clipping, hidden body regions, and anchor alignment.
- Animator or lip-sync compatibility if the asset affects face, head, or attached props.
- Production approval under `P04` before any production-avatar claim.
