# Customization Validator Rules

Current implementation: the checked-in validator logic currently lives in `ai-dev-system/clients/unity-client/Assets/AvatarSystem/AvatarProduction/Editor/Validators/AvatarValidator.cs`.

## Item Definition Checks

Observed current checks:

- `itemId` is required
- `displayName` missing is a warning
- missing `prefab` is a warning
- equipped item prefab should expose either `SkinnedMeshRenderer` or `MeshRenderer`
- item prefab should not carry its own `Animator`
- an item warning is raised if it blocks its own slot
- an item error is raised if it requires its own slot
- missing thumbnail is logged as optional or recommended, not an error

## Outfit Preset Checks

Observed current checks:

- `presetName` missing is a warning
- a preset warning is raised when `dress` is assigned together with `top` or `bottom`

## Current Limits

- These validators run inside the Unity Editor; they are not yet exposed through `ai-dev-system/asset-pipeline/`.
- The deeper humanoid and facial validators still live in the Unity project and remain manual-editor tooling for now.
