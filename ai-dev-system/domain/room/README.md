# Room Domain

Current implementation: room behavior is still implemented by Unity runtime code, while this folder now owns the shared room-focus contract snapshot.

Current source-of-truth code:

- `../../clients/unity-client/Assets/Scripts/App/AssistantBootstrap.cs`
- `../../clients/unity-client/Assets/Scripts/App/StandaloneRoomApp.cs`
- `../../clients/unity-client/Assets/Scripts/App/StandaloneRoomCompositionRoot.cs`
- `../../clients/unity-client/Assets/Scripts/Runtime/RoomRuntime.cs`
- `../../clients/unity-client/Assets/Scripts/Runtime/SceneStateController.cs`

Current scope:

- standalone Unity bootstrap into the room-only runtime without loading the legacy UI Toolkit shell by default
- placeholder-safe room stage and avatar root composition used to keep Workstream B runtime-independent before B02 and B03 land
- camera focus presets
- page-to-focus mapping used by the runtime bridge
- room-item handoff metadata foundation in `contracts/asset-handoff-manifest.md`
- Unity-side room asset registry intake for Mesh AI handoff manifests

Room vision and art direction are now frozen by B00:

- `docs/product/room-vision.md` — room concept, layout, zones, mood, and scope boundaries
- `docs/product/room-art-direction.md` — render pipeline, shaders, lighting, camera, performance budgets
- `docs/product/room-asset-spec.md` — prop inventory, naming conventions, import rules, Unity folder layout

Planned work:

- richer room item registry and interaction contract beyond the current camera-focus-plus-registry-fallback surface
- room blockout and layout foundation (B03)
- prop library and environment art pass (B04)
- lighting and atmosphere polish (B05)
