# Room Asset Specification

Status: Frozen for Workstream B  
Phase: B00 - Freeze room vision and art direction  
Last updated: 2026-04-07

## Purpose

This document locks the room prop inventory, naming conventions, import rules, and Unity folder structure for the room runtime. It is the room-side equivalent of `docs/avatar-spec.md`.

All room prop production, import, and scene-assembly work for B01–B11 should follow this spec.

## 1. Naming Conventions

| Category | Pattern | Example |
| --- | --- | --- |
| Room shell | `ROOM_Shell_{Part}_v{NNN}.fbx` | `ROOM_Shell_WallSet_v001.fbx` |
| Large furniture | `ROOM_Furn_{Name}_v{NNN}.fbx` | `ROOM_Furn_Desk_v001.fbx` |
| Small furniture | `ROOM_Furn_{Name}_v{NNN}.fbx` | `ROOM_Furn_Chair_v001.fbx` |
| Electronics | `ROOM_Elec_{Name}_v{NNN}.fbx` | `ROOM_Elec_Laptop_v001.fbx` |
| Fabric | `ROOM_Fabric_{Name}_v{NNN}.fbx` | `ROOM_Fabric_Curtain_v001.fbx` |
| Decor | `ROOM_Decor_{Name}_v{NNN}.fbx` | `ROOM_Decor_PlantA_v001.fbx` |
| Floor item | `ROOM_Floor_{Name}_v{NNN}.fbx` | `ROOM_Floor_Rug_v001.fbx` |
| Wall item | `ROOM_Wall_{Name}_v{NNN}.fbx` | `ROOM_Wall_PosterA_v001.fbx` |
| Window item | `ROOM_Window_{Name}_v{NNN}.fbx` | `ROOM_Window_CurtainRod_v001.fbx` |
| Lighting fixture | `ROOM_Light_{Name}_v{NNN}.fbx` | `ROOM_Light_DeskLamp_v001.fbx` |

## 2. Prop Categories

### Structural

The room shell — walls, floor, ceiling, window frame, door frame. These define the room boundary and are always visible.

### Furniture

Freestanding objects the avatar and camera interact with spatially. These establish the zone layout.

### Electronics

Screen-bearing or powered props. These have special material needs (emissive screens, cable routing).

### Fabric

Soft items — curtains, bedding, cushions, towels. These have cloth-like materials and softer forms.

### Decor

Personal items that give the room character — plants, framed photos, figurines, accessories.

### Floor Items

Items that sit directly on the floor — rug, slippers, bags, baskets.

### Wall Items

Items mounted on or leaning against walls — posters, shelves, clocks, coat hooks.

### Window Items

Items associated with the window area — curtain rod, window box, window shelf.

### Lighting Fixtures

Props that are also light sources in the scene — desk lamp, ceiling light, fairy lights.

## 3. Required Props (B-Series First Pass)

These are the minimum props needed for B03 (room blockout) and B04 (prop library and environment art).

### B03 — Room Blockout (required for layout and camera testing)

| Prop | Zone | Interaction level | Priority |
| --- | --- | --- | --- |
| Room shell (walls, floor, ceiling, window opening) | All | Static | Required |
| Desk | Desk zone | Static | Required |
| Chair | Desk zone | Static | Required |
| Bed | Bed area | Static | Required |
| Wardrobe / closet | Wardrobe zone | Static | Required |
| Window (glass + frame) | Window wall | Static | Required |

### B04 — Prop Library (required for visual completeness)

| Prop | Zone | Interaction level | Priority |
| --- | --- | --- | --- |
| Laptop or computer monitor | Desk zone | Static | Required |
| Desk lamp | Desk zone | Static (light source) | Required |
| Bookshelf or wall shelf | Near desk | Static | Required |
| Curtains | Window | Static | Required |
| Rug | Center floor | Static | Required |
| Bedding set (sheets, pillow, blanket) | Bed area | Static | Required |
| Small plant (potted) | Desk or shelf | Static | High |
| Books (stacked or shelved) | Shelf or desk | Static | High |
| Clock (wall or desk) | Wall or desk | Static | Medium |
| Picture frame or poster | Wall | Static | Medium |
| Cushion or plush | Bed or chair | Static | Medium |
| Cup or mug | Desk | Static | Medium |
| Pen holder or stationery | Desk | Static | Low |
| Slippers | Floor near bed | Static | Low |
| Small bag or basket | Floor | Static | Low |
| Mirror | Wardrobe zone | Static | High |

### B05 — Lighting and Atmosphere (no new props, lighting polish only)

No additional prop requirements. B05 uses the props from B03 + B04 and focuses on lighting, shadows, and post-processing polish.

## 4. Optional / Future Props

These are NOT required for B-series completion but could be added in future passes:

| Prop | Notes |
| --- | --- |
| Ceiling light or pendant lamp | Could replace or supplement the desk lamp setup |
| Fairy lights or string lights | Decorative; adds personality |
| Guitar or musical instrument | Character detail |
| Headphones stand | Could pair with desk setup |
| Figurine or collectible | Personal shelf decor |
| Calendar (wall) | Could complement the planner page context |
| Whiteboard or corkboard | Could complement the desk zone |
| Coat rack or hook | Near door area |
| Small table or nightstand | Near bed |
| Window box with plants | Window area decor |

## 5. Prop Interaction Levels

Each prop has a defined interaction level that determines its runtime behavior:

| Level | Meaning | B-series scope |
| --- | --- | --- |
| Static | Rendered only; no runtime behavior | All B-series props |
| Focusable | Camera can target this prop on bridge command | Planned for B10 (camera direction) |
| Animated | Has idle or triggered animation (e.g., clock hands, curtain sway) | Planned for B05 (atmosphere polish) |
| Interactive | Responds to user click or avatar approach | Out of scope for B-series |

For B-series, all props start as **Static**. B05 may add subtle animation to 1–2 props (curtain, clock), and B10 may add focus targets.

## 6. Blender / DCC Import Checklist

Before importing a room prop FBX into Unity:

- [ ] Origin at world center, scale = 1.0
- [ ] Apply All Transforms in Blender (Ctrl+A)
- [ ] No extra bones or armatures (room props are static meshes)
- [ ] No isolated vertices or flipped normals
- [ ] Material slots named using `MAT_Room_` convention
- [ ] No animation baked into the file
- [ ] Export settings: FBX Binary, Apply Scalings = All Local
- [ ] Triangle count within the budget for its category (see `room-art-direction.md`)
- [ ] UV unwrap is clean with no overlapping UVs on lightmap UV2 channel
- [ ] Test in Unity: prop renders at correct scale relative to avatar (1 unit = 1 meter)

## 7. Unity Folder Layout

```
Assets/RoomSystem/
  Core/Scripts/         ← room runtime scripts (RoomRuntime, SceneStateController, etc.)
  Core/Materials/       ← shared room materials
  Core/Shaders/         ← custom room shaders (if any)
  RoomProduction/
    Shell/              ← room shell FBX, textures, materials, prefab
    Props/{Category}/   ← per-category prop FBX + prefabs + textures
    Lighting/           ← light prefabs, light probe groups, baked data
    PostProcessing/     ← URP volume profiles for the room
    Data/               ← room layout data, prop placement records
    Scenes/             ← RoomBlockout, RoomLightingTest, RoomFinal
    Editor/             ← import tools, prop validators
  ThirdParty/           ← external packages for room effects (if any)
```

## 8. Scene Test Requirements

| Scene | Purpose | Phase |
| --- | --- | --- |
| RoomBlockout | Test room shell, zone layout, camera presets, avatar placement | B03 |
| RoomPropsTest | Test all props at correct scale, materials rendering, draw call count | B04 |
| RoomLightingTest | Test lighting setup, shadows, post-processing, atmosphere | B05 |
| RoomCameraTest | Test camera transitions, focus presets, avatar visibility in all presets | B10 |
| RoomFinal | Full room with all props, lighting, avatar, and camera presets | B11 |

## 9. Asset Handoff Contract

Room props that originate from the Mesh AI pipeline follow the lifecycle defined in `docs/architecture/mesh-ai-blender-unity-integration.md`:

- `raw` → Mesh AI import received
- `cleaned` → Blender refinement applied
- `validated` → validation report exists
- `export-ready` → handoff manifest exists for Unity import

For B-series, most props will be authored directly in Blender rather than imported from Mesh AI. The Mesh AI pipeline primarily applies to post-B-series room expansion.

## 10. Compatibility With Avatar System

Room props must respect the avatar equipment slot system defined in `docs/avatar-spec.md`:

- Room props do not share materials or shaders with avatar items.
- Room props do not use avatar bones or armatures.
- Room props and avatar items live in separate Unity folder roots (`RoomSystem/` vs `AvatarSystem/`).
- Room textures and avatar textures use separate atlas sets.
- If a room prop is also a wardrobe item (e.g., mirror as a wardrobe-zone prop), the prop version is purely environmental — it does not equip on the avatar.

## Related Documents

- `docs/product/room-vision.md` — room concept, layout, and scope
- `docs/product/room-art-direction.md` — technical art specifications
- `docs/avatar-spec.md` — avatar technical specification
- `docs/architecture/mesh-ai-blender-unity-integration.md` — Mesh AI pipeline integration
- `ai-dev-system/domain/room/contracts/asset-handoff-manifest.md` — asset handoff metadata
