# Avatar Domain

Current implementation: this folder documents the shared avatar contract that already exists in the Unity client runtime.

Current source-of-truth code:

- `../../clients/unity-client/Assets/Scripts/App/AppCompositionRoot.cs`
- `../../clients/unity-client/Assets/Scripts/Runtime/AvatarRuntime.cs`
- `../../clients/unity-client/Assets/Scripts/Avatar/AvatarStateMachine.cs`
- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarConversationBridge.cs`
- `../../clients/unity-client/Assets/AvatarSystem/Core/Scripts/AvatarRootController.cs`

Current scope:

- shell-to-avatar loading path
- runtime state transitions
- conversation-driven handoff into avatar subsystems

Manual validation required:

- final production avatar replacement and sign-off still depend on `tasks/task-people.md` item `P04`
