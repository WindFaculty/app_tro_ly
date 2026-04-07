# Domain

Current implementation: `ai-dev-system/domain/` is now the source of truth for shared non-backend contracts and ownership notes for avatar, customization, room, and cross-domain boundaries.

Important boundary:

- Live Unity runtime code and assets still execute from `ai-dev-system/clients/unity-client/`.
- `domain/` currently owns contract documentation, taxonomy snapshots, and metadata shapes that describe the shared model.
- `packages/contracts/` still owns the typed React or Tauri bridge transport envelopes.

## Current Ownership

- `avatar/`
  - shell-to-avatar runtime contract
  - conversation-state and runtime-handoff notes grounded in current Unity code
- `customization/`
  - current slot taxonomy snapshot
  - planned JSON manifest schema for future cross-system wardrobe exchange
  - current checked-in sample item snapshot
- `room/`
  - current camera-focus presets and page-to-focus mapping
- `shared/`
  - boundary notes between domain contracts and transport-specific bridge models

## Current Truth Sources

Use these paths first when the request is about current avatar, customization, or room behavior:

- avatar runtime composition:
  - `../clients/unity-client/Assets/Scripts/App/AppCompositionRoot.cs`
  - `../clients/unity-client/Assets/Scripts/Runtime/AvatarRuntime.cs`
  - `../clients/unity-client/Assets/Scripts/Avatar/AvatarStateMachine.cs`
  - `../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarConversationBridge.cs`
  - `../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarRootController.cs`
- customization runtime and data:
  - `../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarEnums.cs`
  - `../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarEquipmentManager.cs`
  - `../clients/unity-client/Assets/AvatarSystem/Core/Scripts/Data/AvatarItemDefinition.cs`
  - `../clients/unity-client/Assets/AvatarSystem/Core/Scripts/Data/OutfitPresetDefinition.cs`
  - `../clients/unity-client/Assets/AvatarSystem/AvatarProduction/Editor/Validators/AvatarValidator.cs`
- room runtime:
  - `../clients/unity-client/Assets/Scripts/Runtime/RoomRuntime.cs`
  - `../clients/unity-client/Assets/Scripts/Runtime/SceneStateController.cs`
- shared bridge transport:
  - `../../packages/contracts/src/unity.ts`
  - `../clients/unity-client/Assets/Scripts/Runtime/RuntimeModels.cs`

## Planned Work Still Not Done

- Production-avatar completion is still blocked by `P04`; the current repo state remains placeholder-safe.
- `wardrobe.equipItem` is still a typed command without runtime item-registry wiring in Unity.
- Validation tooling still lives inside the Unity project or root `tools/`; it has not been moved into `asset-pipeline/` yet.
- The slot normalization proposed in `tao_lai_agent.md` is not fully implemented; current runtime still uses `Bottom`, `Dress`, `BraceletL`, and `BraceletR`.
