# Room Domain

Current implementation: room behavior is still implemented by Unity runtime code, while this folder now owns the shared room-focus contract snapshot.

Current source-of-truth code:

- `../../clients/unity-client/Assets/Scripts/Runtime/RoomRuntime.cs`
- `../../clients/unity-client/Assets/Scripts/Runtime/SceneStateController.cs`

Current scope:

- camera focus presets
- page-to-focus mapping used by the runtime bridge

Planned work:

- richer room item registry and interaction contract beyond the current camera-focus surface
