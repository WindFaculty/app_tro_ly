# Avatar Runtime Contract

Current implementation: the Unity shell composes avatar runtime services in `ai-dev-system/clients/unity-client/Assets/Scripts/App/AppCompositionRoot.cs`.

## Loading Path

1. `AppCompositionRoot.Compose(...)` creates `AvatarStateMachine`, `AvatarRuntime`, `AnimationRuntime`, and `LipSyncRuntime`.
2. The composition root resolves scene-owned components with `FindFirstObjectByType<AvatarRootController>()` and `FindFirstObjectByType<AvatarConversationBridge>()`.
3. `AvatarRuntime.Bind(...)` receives:
   - `AvatarStateMachine`
   - `AvatarConversationBridge`
   - `AvatarRootController`
4. `AnimationRuntime` and `LipSyncRuntime` bind to the same avatar scene objects.
5. `UnityBridgeClient` and `TauriBridgeRuntime` then route typed runtime intent into those avatar runtime services.

## Runtime API Surface

Current implementation in `AvatarRuntime.cs` exposes:

- `SetIdleState()`
- `SetListeningState()`
- `SetThinkingState()`
- `SetTalkingState()`
- `SetMood(string backendState, string animationHint = "")`
- `PlayEmote(string triggerName)`
- `EquipItem(AvatarItemDefinition item)`
- `SetLookAtTarget(Transform target)`
- `CurrentState`
- `StateChanged`

## State Contract

Current implementation uses `LocalAssistant.Core.AvatarState`:

- `Idle`
- `Listening`
- `Thinking`
- `Talking`
- `Confirming`
- `Warning`
- `Greeting`
- `Waiting`
- `Error`
- `Reacting`
- `Dormant`

`AvatarRuntime` emits `StateChanged` as lowercase strings via `CurrentState.ToString().ToLowerInvariant()`.

## Conversation Bridge Contract

Current implementation routes high-level conversation events through `AvatarConversationBridge` rather than directly mutating animator or facial systems from shell code.

Observed bridge entry points:

- `OnListeningStart()`
- `OnListeningEnd()`
- `OnThinkingStart()`
- `OnSpeakingStart()`
- `OnSpeakingEnd()`
- `OnSpeakingVisemeFrame(VisemeType viseme)`
- `OnEmotionHint(EmotionType emotion)`
- `OnGestureHint(GestureType gesture)`
- `OnReacting()`
- `OnIdle()`
- `OnDormant()`

## Current Limits

- Scene setup still depends on `AvatarRootController` and `AvatarConversationBridge` existing in the loaded Unity scene.
- Production-avatar completion is still blocked by `P04`.
- The placeholder-safe shell flow is implemented; a production asset handoff is not yet verified from terminal work.
