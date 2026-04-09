# Avatar Domain

Current implementation: this folder documents the shared avatar contract that already exists in the live Unity runtime.

Current source-of-truth code:

- `../../../apps/unity-runtime/Assets/Scripts/App/StandaloneRoomCompositionRoot.cs`
- `../../../apps/unity-runtime/Assets/Scripts/Runtime/AvatarRuntime.cs`
- `../../../apps/unity-runtime/Assets/Scripts/Avatar/AvatarStateMachine.cs`
- `../../../apps/unity-runtime/Assets/AvatarSystem/Core/Scripts/AvatarConversationBridge.cs`
- `../../../apps/unity-runtime/Assets/AvatarSystem/Core/Scripts/AvatarRootController.cs`

Current scope:

- room-runtime-to-avatar loading path
- runtime state transitions
- conversation-driven handoff into avatar subsystems

Manual validation required:

- final production avatar replacement and sign-off still depend on `tasks/task-people.md` item `P04`
