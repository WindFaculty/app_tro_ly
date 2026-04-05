# Avatar Asset Spec

Updated: 2026-04-05
Status: Current implementation baseline plus Phase 4 standardization rules

This page defines the placeholder-safe asset and metadata conventions for avatar customization in the current repo.
It does not claim that production avatar handoff or wardrobe UI is finished.

## Current Implementation

- Runtime avatar code still lives in `unity-client/Assets/Scripts/Avatar/` and `unity-client/Assets/AvatarSystem/`.
- Core asset data types live in `unity-client/Assets/AvatarSystem/Core/Scripts/Data/`.
- The repo now includes `AvatarAssetRegistryDefinition` as the catalog type for registered items, conflict rules, and outfit presets.
- `AvatarOutfitApplicationService` and `AvatarPresetManager` can now load saved outfits from a registry-backed item catalog instead of requiring ad-hoc item arrays.
- The shell still does not ship a wardrobe UI. Registry and spec work is a data foundation only.

## Source Of Truth

- Item metadata type: `unity-client/Assets/AvatarSystem/Core/Scripts/Data/AvatarItemDefinition.cs`
- Outfit preset type: `unity-client/Assets/AvatarSystem/Core/Scripts/Data/OutfitPresetDefinition.cs`
- Conflict rule type: `unity-client/Assets/AvatarSystem/Core/Scripts/Data/ConflictRuleDefinition.cs`
- Registry type: `unity-client/Assets/AvatarSystem/Core/Scripts/Data/AvatarAssetRegistryDefinition.cs`
- Runtime equip or preset logic: `unity-client/Assets/AvatarSystem/Core/Scripts/AvatarEquipmentManager.cs` and `unity-client/Assets/AvatarSystem/Core/Scripts/AvatarPresetManager.cs`
- Application-facing contract: `unity-client/Assets/Scripts/Avatar/AvatarOutfitApplicationService.cs`
- Editor validation entry: `unity-client/Assets/AvatarSystem/AvatarProduction/Editor/Validators/AvatarValidator.cs`

## Naming Convention

Current implementation keeps the existing naming rules from `docs/avatar-spec.md`.

Use these patterns for new assets:

| Category | Pattern | Example |
| --- | --- | --- |
| Base avatar | `CHR_Avatar_Base_v{NNN}.fbx` | `CHR_Avatar_Base_v001.fbx` |
| Hair item asset | `ITM_Hair_{Name}_{NN}` or existing tracked asset name | `ITM_Hair_01.asset` |
| Outfit preset asset | `PRE_Outfit_{Name}_{NN}` | `PRE_Outfit_Starter_01.asset` |
| Conflict rule asset | `RULE_Conflict_{Name}_{NN}` | `RULE_Conflict_DressBlocksTopBottom_01.asset` |
| Registry asset | `REG_AvatarAssets_{Name}_{NN}` | `REG_AvatarAssets_Base_01.asset` |

## Folder Convention

### Current implementation folders

- Base avatar: `unity-client/Assets/AvatarSystem/AvatarProduction/BaseAvatar/`
- Slot meshes and source item assets: `unity-client/Assets/AvatarSystem/AvatarProduction/Parts/`
- Registered data assets: `unity-client/Assets/AvatarSystem/AvatarProduction/Data/`
- Outfit presets: `unity-client/Assets/AvatarSystem/AvatarProduction/Presets/OutfitPresets/`
- Validators and import tools: `unity-client/Assets/AvatarSystem/AvatarProduction/Editor/`

### Standardization rule for new work

- New item definition assets should live under `unity-client/Assets/AvatarSystem/AvatarProduction/Data/ItemDefinitions/`.
- New conflict rule assets should live under `unity-client/Assets/AvatarSystem/AvatarProduction/Data/ConflictRules/`.
- New registry assets should live under `unity-client/Assets/AvatarSystem/AvatarProduction/Data/`.
- New preset assets should live under `unity-client/Assets/AvatarSystem/AvatarProduction/Presets/OutfitPresets/`.
- `Parts/` remains the home for source meshes or prefabs, not the source of truth for what the app may equip.

## Slot System

Current runtime slot enum:

- `Hair`
- `HairAccessory`
- `Top`
- `Bottom`
- `Dress`
- `Socks`
- `Shoes`
- `Gloves`
- `BraceletL`
- `BraceletR`
- `BodyVariant` - reserved
- `FaceVariant` - reserved
- `AccessoryExtra` - reserved

Current rule baseline:

- `Dress` blocks `Top` and `Bottom`.
- `HairAccessory` may require `Hair`.
- `Gloves` may block `BraceletL` and `BraceletR`.
- Tag-based incompatibility remains supported through item-carried tags plus incompatible tags.

Reserved slots must not be treated as production-ready customization surfaces until `P04` provides approved assets and expectations.

## Metadata Schema

### Item metadata

`AvatarItemDefinition` is the required metadata object for every equippable item.

| Field | Current status | Purpose |
| --- | --- | --- |
| `itemId` | required | stable registry key |
| `displayName` | required | user-facing name |
| `slotType` | required | owning equipment slot |
| `requiredBaseVersion` | required for new items | expected base avatar version |
| `bodyTypeId` | required for new items | compatible body type identifier |
| `prefab` | recommended | runtime object to instantiate |
| `materialSet` | optional | material overrides |
| `thumbnail` | optional | preview icon |
| `occupiesSlots` | optional | extra slots occupied |
| `blocksSlots` | optional | slots to force unequip |
| `requiresSlots` | optional | prerequisite slots |
| `compatibleTags` | optional | tags carried by the item |
| `incompatibleTags` | optional | tags that conflict with this item |
| `hideBodyRegions` | optional | body regions hidden while equipped |
| `anchorType` | optional | attachment rule |
| `anchorBoneName` | optional | explicit bone target for `BoneAttach` |

### Preset metadata

`OutfitPresetDefinition` is the required metadata object for full-look presets.

| Field | Current status | Purpose |
| --- | --- | --- |
| `presetId` | required for new presets | stable registry key |
| `presetName` | required | user-facing name |
| `requiredBaseVersion` | required for new presets | expected base avatar version |
| slot assignment fields | optional per slot | item reference per slot |
| `thumbnail` | optional | preview icon |

### Registry metadata

`AvatarAssetRegistryDefinition` is the single catalog object that declares which items, rules, and presets are valid for a given avatar foundation.

| Field | Purpose |
| --- | --- |
| `registryId` | stable registry key |
| `displayName` | user-facing registry name |
| `requiredBaseVersion` | base avatar version expected by the registry |
| `bodyTypeId` | body type namespace for the registry |
| `items` | registered item definitions |
| `conflictRules` | registered cross-slot rules |
| `outfitPresets` | registered outfit presets |

## Registry Rule

- Do not let UI or application code scan folders to discover equippable assets.
- Register allowed items, presets, and conflict rules in `AvatarAssetRegistryDefinition`.
- Use the registry as the catalog boundary for future wardrobe or shell flows.
- Editor validation should run against the registry before manual smoke or content sign-off.

## Validation And Intake

- Use `Tools > AvatarSystem > Validate All Item Definitions` for item-level checks.
- Use `Tools > AvatarSystem > Validate Outfit Presets` for preset checks.
- Use `Tools > AvatarSystem > Validate Asset Registries` for catalog-level checks.
- Follow [avatar-asset-intake-checklist.md](avatar-asset-intake-checklist.md) whenever a new asset enters the repo.

## Non-Goals

- This spec does not claim a shipped wardrobe UI.
- This spec does not claim final production avatar content is present.
- This spec does not replace the manual `P04` gate for production avatar approval.
