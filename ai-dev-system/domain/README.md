# Domain

Current implementation: `ai-dev-system/domain/` is now the source of truth for shared non-backend contracts and ownership notes for avatar, customization, room, and cross-domain boundaries.

Important boundary:

- Live Unity runtime code and assets now execute from `apps/unity-runtime/`.
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
  - Mesh AI clothing and accessory handoff notes
- `room/`
  - current camera-focus presets and page-to-focus mapping
  - room-item handoff notes and current Unity registry-intake boundary
- `shared/`
  - boundary notes between domain contracts and transport-specific bridge models
  - shared asset handoff boundary notes

## Current Truth Sources

Use these paths first when the request is about current avatar, customization, or room behavior:

- avatar runtime composition:
  - `../../apps/unity-runtime/Assets/Scripts/App/StandaloneRoomCompositionRoot.cs`
  - `../../apps/unity-runtime/Assets/Scripts/Runtime/AvatarRuntime.cs`
  - `../../apps/unity-runtime/Assets/Scripts/Avatar/AvatarStateMachine.cs`
  - `../../apps/unity-runtime/Assets/AvatarSystem/Core/Scripts/AvatarConversationBridge.cs`
  - `../../apps/unity-runtime/Assets/AvatarSystem/Core/Scripts/AvatarRootController.cs`
- customization runtime and data:
  - `../../apps/unity-runtime/Assets/AvatarSystem/Core/Scripts/AvatarEnums.cs`
  - `../../apps/unity-runtime/Assets/AvatarSystem/Core/Scripts/AvatarEquipmentManager.cs`
  - `../../apps/unity-runtime/Assets/AvatarSystem/Core/Scripts/Data/AvatarItemDefinition.cs`
  - `../../apps/unity-runtime/Assets/Scripts/Runtime/AvatarItemRegistry.cs`
  - `../../apps/unity-runtime/Assets/AvatarSystem/Core/Scripts/Data/OutfitPresetDefinition.cs`
  - `../../apps/unity-runtime/Assets/AvatarSystem/AvatarProduction/Editor/Validators/AvatarValidator.cs`
- room runtime:
  - `../../apps/unity-runtime/Assets/Scripts/Runtime/RoomRuntime.cs`
  - `../../apps/unity-runtime/Assets/Scripts/Runtime/SceneStateController.cs`
- shared bridge transport:
  - `../../packages/contracts/src/unity.ts`
  - `../../apps/unity-runtime/Assets/Scripts/Runtime/RuntimeModels.cs`

## Planned Work Still Not Done

- Production-avatar completion is still blocked by `P04`; the current repo state remains placeholder-safe.
- Registered runtime items now equip through `wardrobe.equipItem`, but the catalog is still placeholder-safe and not broadly populated with production assets yet.
- Validation tooling still lives inside the Unity project or root `tools/`; it has not been moved into `asset-pipeline/` yet.
- The slot normalization proposed in `tao_lai_agent.md` is not fully implemented; current runtime still uses `Bottom`, `Dress`, `BraceletL`, and `BraceletR`.
- Mesh AI handoff manifests now feed a Unity-side metadata registry for wardrobe foundations and room assets, but they do not mean production prefab hookup or broad runtime asset population is done.
