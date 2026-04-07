# Customization Domain

Current implementation: customization runtime still lives in the Unity client, while this folder now owns the shared slot taxonomy and contract notes for wardrobe data.

Current source-of-truth code:

- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarEnums.cs`
- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarEquipmentManager.cs`
- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/Data/AvatarItemDefinition.cs`
- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/Data/OutfitPresetDefinition.cs`
- `../../clients/unity-client/Assets/AvatarSystem/AvatarProduction/Editor/Validators/AvatarValidator.cs`

Current implementation notes:

- Runtime equip rules are implemented by `AvatarEquipmentManager`.
- Checked-in avatar item data currently exists as Unity `ScriptableObject` assets under `Assets/AvatarSystem/AvatarProduction/`.
- The typed command `wardrobe.equipItem` exists, but Unity runtime item-registry lookup is still planned work.

Planned work:

- a fully wired runtime item registry for bridge-driven equip commands
- broader sample data beyond the currently checked-in placeholder item assets
- migration of validation tooling into `ai-dev-system/asset-pipeline/`
