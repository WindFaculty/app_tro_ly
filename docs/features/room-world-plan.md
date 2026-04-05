# Room World Plan

Updated: 2026-04-06
Status: Planned work for the future Character Space room subsystem

This document is the Phase 0 planning baseline for expanding the current room-foundation Home stage into a fuller room-backed Character Space subsystem.

It does not describe the full shipped target behavior. Current implementation now includes a placeholder-safe room foundation in the Home center stage, plus basic room-object interaction and a placeholder-safe character-room bridge for spawn plus attention behavior. Current runtime truth still lives in `docs/04-ui.md`, `docs/02-architecture.md`, `unity-client/Assets/Resources/UI/Screens/HomeScreen.uxml`, and `unity-client/Assets/Scripts/Features/Home/HomeScreenController.cs`.

## Purpose

- define how a 3D room should fit into the current Unity shell without creating a second runtime
- separate room-world logic from Character Space UI overlay logic
- keep object placement, metadata, and future interactions under explicit world-owned contracts
- provide one MVP room plan before any prefab or scene population work begins

## Current Baseline

- The live shell still mounts `HomeScreen.uxml` in the center stage and now presents a room-foundation HUD instead of the older orbit-only stage copy.
- `HomeScreenController` now writes room-foundation copy for the center stage rather than the older avatar-stage placeholder text.
- `AppCompositionRoot` now creates or reuses a single runtime camera and hands it to `RoomSceneBootstrap` for the room foundation setup.
- Current implementation now includes a placeholder-safe room template asset at `unity-client/Assets/Resources/World/Rooms/Room_Base.prefab`, which `RoomSceneBootstrap` uses before falling back to a generated room root.
- `AssistantApp` does not instantiate a room or a production avatar scene by itself.
- `AvatarConversationBridge` is only used if one already exists in the active Unity scene.
- `CharacterRoomBridge` now creates a room-bound placeholder avatar proxy when no production avatar root exists in the scene, and it aligns the active avatar presence with the room spawn point plus selected-object attention targets.
- The current Home overlay also includes a lightweight room action dock, current-activity strip, and hotspot-visibility toggle driven by selection snapshots plus app-owned placeholder-safe commands.
- The repo now also includes validator and checklist scaffolding for room-object intake, but current runtime room objects still resolve through primitive placeholder shapes instead of prefab-backed assets.

## Phase 0 Decisions

### Room placement model

- The MVP room should live inside the active assistant Unity scene, not as a separate app or an independent launcher flow.
- The room should be mounted under a dedicated room root owned by Character Space runtime code.
- A later additive-scene workflow can remain a future optimization, but it is not the baseline for the first room-backed Character Space slice.

Why:

- the current shell already expects one active runtime scene with UI Toolkit overlay
- `AssistantApp` and `AppCompositionRoot` do not currently manage route-level scene loading
- keeping room content in the active assistant scene avoids a second source of truth for shell state

### Room ownership model

- The room should be treated as a world subsystem, not as decoration embedded in a UI controller.
- Character Space UI should own overlays only:
  - subtitle
  - action dock
  - selected object info
  - lightweight environment status
- Room bootstrap, object placement, anchors, and selection state should live outside `HomeScreenController` or future `CharacterSpaceScreenController`.

### Object population model

- Room objects should be spawned from registry-backed definitions and layout config.
- The scene should keep anchors, spawn points, and room roots, not hand-placed final furniture hierarchies for every object.
- Manual per-object scene placement should be limited to authoring anchors, not runtime ownership.

## Planned Subsystems

### Room World

Responsibilities:

- own room root lifecycle
- resolve the active room layout definition
- expose avatar spawn point, camera anchor, decor anchors, and furniture anchors
- bootstrap room population and later zone metadata

Planned classes:

- `RoomWorldController`
- `RoomLayoutDefinition`
- `RoomSpawnPoint`
- `RoomCameraAnchorRegistry`
- `RoomSceneBootstrap`

### Room Objects

Responsibilities:

- define object categories and prefab keys
- map layout placement data to instantiated prefabs
- own room-object metadata and optional state flags

Planned classes:

- `RoomObjectRegistry`
- `RoomObjectDefinition`
- `RoomObjectInstance`
- `RoomObjectAnchor`
- `RoomObjectFactory`

### Interaction

Responsibilities:

- own selected-object state
- manage hover or select or inspect eligibility
- bridge world interactions into Character Space UI and later avatar intent

Planned classes:

- `RoomInteractionController`
- `InteractableObject`
- `SelectedRoomObjectStore`
- `InteractionAction`

### Character Integration

Responsibilities:

- resolve where the avatar stands in the room
- keep avatar-facing behavior and later attention logic separate from room placement rules
- expose hooks for later movement and inspect flows

Planned classes:

- `CharacterRoomBridge`
- `CharacterSpawnResolver`
- `CharacterInteractionBridge`
- `CharacterLookAtTarget`

### Character Space UI Overlay

Responsibilities:

- show selected-object info
- show room action dock
- show environment or activity status
- stay thin and world-agnostic beyond route or selection display

Planned classes:

- `CharacterSpaceScreenController`
- `RoomObjectInfoPanelController`
- `CharacterActionDockController`
- `EnvironmentOverlayController`

## Folder Direction

Planned Unity code roots:

- `unity-client/Assets/Scripts/Features/CharacterSpace/`
- `unity-client/Assets/Scripts/World/Room/`
- `unity-client/Assets/Scripts/World/Objects/`
- `unity-client/Assets/Scripts/World/Interaction/`
- `unity-client/Assets/Scripts/World/Camera/`

Planned Unity asset roots:

- `unity-client/Assets/World/Rooms/`
- `unity-client/Assets/World/Objects/Furniture/`
- `unity-client/Assets/World/Objects/Decor/`
- `unity-client/Assets/World/Objects/Interactive/`
- `unity-client/Assets/World/Materials/`
- `unity-client/Assets/World/Prefabs/`
- `unity-client/Assets/World/Registry/`

These are still planned work only. Current runtime room-template truth lives under `unity-client/Assets/Resources/World/Rooms/` so the room foundation can load in builds without an editor-only asset lookup path.

## MVP Room Baseline

The first room-backed Character Space should stay intentionally small:

- one enclosed room root
- one avatar spawn point
- one camera anchor
- one desk anchor cluster
- one rest-area anchor cluster
- one decor anchor cluster

Recommended MVP object list:

- floor
- three walls
- window or framed wall art
- rug
- bed or sofa
- desk
- chair
- lamp
- shelf
- laptop
- plant

## Camera Direction

- The current orthographic placeholder camera in `AppCompositionRoot` is a temporary baseline and is not suitable for the planned room-backed Character Space.
- The room MVP should use a dedicated stage camera setup with a stable hero angle for Character Space.
- Follow or inspect camera modes can remain planned hooks rather than Phase 1 requirements.

## Avatar Placement Rule

- The avatar should spawn from an explicit room-owned spawn point, not from ad-hoc transform assumptions in UI code.
- The first implementation only needs:
  - spawn position
  - facing direction
  - idle zone reference
  - optional attention target hook

## Overlay Rule

The first room-aware Character Space overlay should be lightweight and must not hide the room:

- selected object name
- selected object category
- current object state summary
- quick actions such as inspect or focus or return
- a compact current-activity readout plus hotspot visibility toggle

## Constraints

- do not turn room logic into a second shell or a second runtime root
- do not move required runtime roots outside `unity-client/` during this work
- do not claim production-avatar completion from room planning alone
- do not bypass the current avatar bridge and outfit contracts when later room work needs avatar integration

## Manual Gates For Later Phases

- `P02` is required before room visuals or interactions can be called smoke-verified in Unity or packaged-client runs.
- `P04` is still required before any production-avatar integration claim.

## Phase 0 Deliverables Captured Here

- room ownership model
- active-scene mounting decision
- planned subsystem split
- MVP room scope
- camera and avatar placement rules
