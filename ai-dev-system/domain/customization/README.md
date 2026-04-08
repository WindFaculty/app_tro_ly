# Customization Domain

Current implementation: customization runtime still lives in the Unity client, while this folder now owns the shared slot taxonomy and contract notes for wardrobe data.

Current source-of-truth code:

- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarEnums.cs`
- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarEquipmentManager.cs`
- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/Data/AvatarItemDefinition.cs`
- `../../clients/unity-client/Assets/Scripts/Runtime/AvatarItemRegistry.cs`
- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/Data/OutfitPresetDefinition.cs`
- `../../clients/unity-client/Assets/AvatarSystem/AvatarProduction/Editor/Validators/AvatarValidator.cs`

Current implementation notes:

- Runtime equip rules are implemented by `AvatarEquipmentManager`.
- Checked-in avatar item data currently exists as Unity `ScriptableObject` assets under `Assets/AvatarSystem/AvatarProduction/`.
- `local-backend/app/services/wardrobe.py` now consumes `contracts/slot-taxonomy.json` plus `sample-data/current-item-catalog.json` to seed and validate the desktop wardrobe registry under `local-backend/data/wardrobe/registry.json`.
- The desktop wardrobe registry is exposed through `/v1/wardrobe`, `/v1/wardrobe/export`, and `/v1/wardrobe/import`, but that desktop data layer still does not imply live Unity runtime item lookup.
- Unity now loads Mesh AI clothing and accessory handoff manifests into a runtime metadata registry, and `wardrobe.equipItem` can map those entries into live `AvatarItemDefinition` equip behavior through `AvatarItemRegistry` when a runtime item is registered.
- `Assets/Resources/AvatarItems/AzureSakuraKimono.asset` is the current placeholder-safe sample runtime item proving the bridge path end to end without claiming a production prefab hookup.
- `contracts/asset-handoff-manifest.md` now documents the future Unity-facing asset handoff metadata for clothing and accessory assets.

Planned work:

- broader sample data beyond the currently checked-in placeholder item assets
- migration of validation tooling into `ai-dev-system/asset-pipeline/`
- broader production-ready mapping coverage beyond the current placeholder-safe Azure Sakura sample
