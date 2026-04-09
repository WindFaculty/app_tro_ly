# Room Vision

Status: Frozen for Workstream B  
Phase: B00 - Freeze room vision and art direction  
Last updated: 2026-04-08

## Purpose

This document freezes the room concept, spatial layout, and presentation vision for the Unity room runtime owned by Workstream B.

The room is the 3D living space where the assistant avatar exists. It is the only Unity-rendered visual surface in the final desktop app. Every camera angle, prop placement, and atmosphere decision flows from this document.

## Current Implementation Baseline

Current implementation in this repo:

- `apps/unity-runtime/Assets/Scripts/App/AssistantBootstrap.cs` auto-boots `StandaloneRoomApp`.
- `apps/unity-runtime/Assets/Scripts/App/StandaloneRoomCompositionRoot.cs` builds the current placeholder-safe room stage, warm orthographic camera, directional light, and minimal avatar root used by the standalone Unity runtime.
- `apps/unity-runtime/Assets/Scripts/Runtime/RoomRuntime.cs` implements four orthographic camera focus presets: `overview` (5.2), `avatar` (3.6), `desk` (4.2), `wardrobe` (3.2).
- `apps/unity-runtime/Assets/Scripts/Runtime/SceneStateController.cs` maps React page context to room focus presets.
- `ai-dev-system/domain/room/contracts/focus-presets.json` documents the current focus preset shape.
- Current implementation: the standalone Unity runtime now starts from placeholder-safe runtime composition rather than the old UI Toolkit shell, while full room blockout art and production avatar visuals remain planned follow-on work.

No room concept, art direction, prop inventory, or spatial specification existed before this document.

## Room Concept

### Setting

The room is a cozy, lived-in Japanese-style apartment bedroom that doubles as a personal workspace. It reflects a young person's private space: warm, slightly compact, softly lit, and personally decorated.

The room has the personality of someone organized but creative — neat enough to feel comfortable, personal enough to feel real.

### Emotional Tone

- Warm and inviting, not sterile or corporate
- Slightly intimate, like being in a friend's room
- Calm ambient energy that does not compete with the business UI
- Subtle personality through prop choices and arranged details

### Scale and Proportion

- The room is roughly 4m × 5m in world scale — a compact single room
- Ceiling height is standard residential (about 2.4m)
- The avatar is roughly 1.6m tall, fitting the anime-proportioned character spec
- All furniture scales to the avatar proportionally, following anime-style slightly compressed ratios

## Room Layout

The room is organized into four spatial zones that map directly to the existing camera focus presets and React page contexts.

### Zone Map

```text
+----------------------------------------------+
|                   WINDOW                      |
|   +--------+                  +-----------+   |
|   |  BED   |                  |   DESK    |   |
|   |  AREA  |                  |   AREA    |   |
|   +--------+                  +-----------+   |
|                                               |
|              CENTER FLOOR                     |
|            (avatar stands here)               |
|                                               |
|   +----------+                +-----------+   |
|   | WARDROBE |                | BOOKSHELF |   |
|   |  AREA    |                |  / SHELF  |   |
|   +----------+                +-----------+   |
|                   DOOR                        |
+----------------------------------------------+
```

Camera looks from the bottom of the map toward the window wall.

### Zone Definitions

| Zone | Camera preset | React pages | Contents | Purpose |
| --- | --- | --- | --- | --- |
| Overview | `overview` (5.2) | dashboard, unmapped | Full room visible | Shows the whole room and avatar in context; used as the resting or ambient state |
| Avatar | `avatar` (3.6) | chat | Center floor, avatar front-facing | Focuses on the avatar for conversation; props are background context |
| Desk | `desk` (4.2) | planner, notes | Desk, chair, computer, task boards | Focuses on the work area; avatar may sit or stand near the desk |
| Wardrobe | `wardrobe` (3.2) | wardrobe | Closet, mirror, accessories | Focuses on the dressing area; avatar stands centered for outfit display |

### Avatar Placement Rules

- The avatar's default idle position is the center floor area, facing the camera.
- In `chat` context, the avatar stays at center, closer to camera.
- In `desk` context, the avatar may stand near or sit at the desk.
- In `wardrobe` context, the avatar stands centered in front of the wardrobe area.
- The avatar should never be clipped by room geometry in any camera preset.

## Visual Identity

### Color Palette

The room uses warm, muted tones with occasional accent colors:

| Role | Color direction | HSL range |
| --- | --- | --- |
| Walls | Warm off-white to soft cream | H: 30-45, S: 10-20%, L: 85-92% |
| Floor | Light warm wood | H: 25-40, S: 20-35%, L: 55-70% |
| Furniture main | Natural wood tones, soft white painted | H: 25-45, S: 15-30%, L: 50-75% |
| Fabric (bed, curtains) | Soft pastels — lavender, cream, pale pink | H: varies, S: 15-35%, L: 75-90% |
| Accent props | Muted teal, soft coral, warm gold | S: 25-45%, L: 45-65% |
| Window light | Warm daylight spill | H: 40-55, S: 15-30%, L: 90-95% |

### Material Language

- Surfaces lean toward matte and soft, not glossy or hard
- Wood grain is present but not photorealistic — simplified with clear value reads
- Fabric has gentle soft-shadow curvature, not flat
- Metal accents (lamp, drawer handles) are brushed, not chrome-reflective
- Glass (window) is translucent with a warm refraction tint, not fully transparent

### Texture Approach

- Textures use hand-painted or semi-stylized PBR treatment, not photoscanned
- Texture detail is enough to read at desk-zone camera distance, not hyper-detailed
- Color textures drive most of the visual read; normal maps add subtle depth only
- No high-frequency noise or film grain in base textures

## Mood and Atmosphere

### Lighting Direction

- Primary light source: warm daylight through the window (directional or area light)
- Secondary light: soft ambient fill to prevent hard shadows on the avatar
- Accent lights: desk lamp (warm), optional soft under-shelf glow
- Shadow direction: gentle and soft, consistent with the friendly tone
- No dramatic shadows, high contrast, or horror-style lighting

### Time of Day

- The frozen direction for B-series is **always daytime** — warm afternoon light
- Day/night cycle is explicitly out of scope for B01–B11
- A future extension could add time variants, but this is not part of the current freeze

### Ambient Direction

- The room should feel quietly alive: small dust motes in the window light (optional particle effect), gentle curtain sway (optional subtle animation)
- No moving props or interactive objects for B-series first pass
- Sound direction is out of scope for B00 but should be warm and ambient when added later

## Scope Boundaries

### In scope for B-series

- A complete room environment with the four zones defined above
- All required props placed and lit
- Camera preset transitions matching the current four focus presets
- Enough visual polish that the room feels finished, not a blockout
- Static prop rendering with correct materials and lighting

### Out of scope for B-series

- Day/night cycle
- Weather or seasonal variants
- Interactive props (clickable, movable)
- Dynamic room rearrangement or user customization
- Room items spawned from Mesh AI pipeline (depends on A19)
- Sound or ambient audio

### Non-goals

- Photorealism
- Open-world or multiple rooms
- Destructible or physics-driven props
- Real-time reflection probes or ray tracing
- VR or non-desktop camera modes

## Relationship to Desktop Pages

The room exists as a visual companion to the React web UI. It does not duplicate business information or controls.

| React page | Room behavior | Room does NOT do |
| --- | --- | --- |
| Dashboard | Show full room; avatar idle | Show dashboard cards or metrics in 3D |
| Chat | Focus avatar; avatar reacts to conversation state | Render chat text or input in 3D |
| Planner | Focus desk area; avatar may sit at desk | Show task lists or calendar UI in 3D |
| Notes | Focus desk area or overview | Render note content in 3D |
| Wardrobe | Focus wardrobe area; avatar centered for outfit display | Build wardrobe management UI in 3D |
| Settings | Overview or avatar focus | Render settings UI in 3D |

## Relationship to Avatar Spec

The avatar spec (`docs/avatar-spec.md`) remains the source of truth for:

- avatar skeleton and blendshape requirements
- equipment slot taxonomy
- animation lists and phase requirements
- lip-sync viseme mapping

This document adds room-specific avatar behavior expectations:

- Avatar must be fully visible (not clipped) in all four camera presets
- Avatar placement must respect the zone map above
- Avatar shadows should land naturally on the room floor
- Avatar-to-furniture scale must look proportional (anime-proportioned, not hyperrealistic)

## Related Documents

- `docs/product/room-art-direction.md` — technical art specifications
- `docs/product/room-asset-spec.md` — prop inventory and naming conventions
- `docs/avatar-spec.md` — avatar technical specification
- `ai-dev-system/domain/room/contracts/focus-presets.json` — current focus preset truth
- `docs/architecture/desktop-target.md` — desktop architecture (Unity is optional for A-series)
