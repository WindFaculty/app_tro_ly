# Room Art Direction

Status: Frozen for Workstream B  
Phase: B00 - Freeze room vision and art direction  
Last updated: 2026-04-07

## Purpose

This document locks the technical art specifications for the Unity room runtime. It defines render pipeline, shader approach, lighting setup, camera behavior, material rules, and performance budgets.

All room art production for B01–B11 should follow this spec. Changes to these technical constraints require updating this document first.

## Render Pipeline

- **Pipeline**: Unity Universal Render Pipeline (URP)
- **Rendering path**: Forward rendering
- **Color space**: Linear
- **HDR**: Enabled for post-processing quality
- **MSAA**: 4x or 8x (target smooth edges on prop silhouettes)
- **Depth texture**: Enabled (required for some post-processing effects)

Current implementation note: `apps/unity-runtime/Assets/UniversalRenderPipelineGlobalSettings.asset` already exists. B01 should verify its settings match these targets.

## Art Style Definition

### Style

Semi-stylized anime-inspired 3D. Not photorealistic, not flat cel-shaded.

The visual language sits between:
- **NOT**: Overwatch-style chunky stylized
- **NOT**: Genshin Impact hyperpolished anime
- **YES**: Cozy indie-anime aesthetic — clean shapes, warm palette, soft lighting, gentle detail density

### Shape Language

- Furniture uses soft rounded edges, not hard industrial corners
- Organic props (plants, fabric, plush items) use gentle blobby forms
- Straight lines appear in architectural elements (walls, floor, window frames) but with subtle imperfection
- No perfectly sharp 90-degree edges on objects the camera gets close to

### Silhouette Priority

- Every prop should read clearly at `overview` camera distance (ortho size 5.2)
- Prop shapes should be identifiable even in shadow or low-contrast lighting
- Avoid props that are visually ambiguous at a distance

## Shader Approach

### Primary Shaders

| Surface type | Shader approach | Notes |
| --- | --- | --- |
| Wood surfaces | URP Lit with albedo + subtle normal | Warm tone bias, low metallic, medium smoothness |
| Fabric surfaces | URP Lit with albedo, very low smoothness | Slightly fuzzy look, no specular highlight |
| Wall / ceiling | URP Lit with flat or subtly textured albedo | Clean, light reads; avoid busy patterns |
| Metal accents | URP Lit with metallic channel | Brushed finish, not mirror-chrome |
| Glass (window) | URP Lit or custom transparent | Warm-tinted translucency, no harsh reflections |
| Paper / books | URP Lit with albedo | Matte, simple color blocks with edge detail |
| Screen (computer) | Unlit or emissive | Soft glow, screen content is placeholder texture |
| Floor | URP Lit with albedo + very subtle normal | Warm wood tone, clean planks |

### Shader Rules

- Do not use custom shader graphs unless URP Lit cannot achieve the target look.
- Do not use emission on non-light props (avoid accidental glow on furniture).
- Keep material instance count per prop low (1–3 materials per prop asset).
- Name materials using the convention: `MAT_Room_{Object}_{Variant}.mat`.

## Lighting Specification

### Light Setup

| Light | Type | Color temp | Intensity | Shadow |
| --- | --- | --- | --- | --- |
| Window daylight | Directional | Warm (5500K–6000K bias warm) | Medium-high | Soft shadows, shadow distance capped |
| Ambient fill | Environment or flat ambient | Slightly warm neutral | Enough to prevent pure-black shadows | N/A |
| Desk lamp | Point or spot | Warm (3500K–4000K) | Low-medium | Optional soft shadow |
| Accent (optional) | Point | Warm (4000K) | Very low | No shadow |

### Light Rules

- Maximum 1 real-time directional light.
- Maximum 2 real-time point or spot lights.
- Additional lights can be baked.
- Shadow resolution for the directional light: 2048 or 1024.
- Shadow distance should cover the full room (approximately 8–10 units).
- Light probe group or baked global illumination is recommended for ambient quality.

### Shadow Rules

- Shadows should be soft — avoid pixel-sharp shadow edges.
- Avatar must cast a shadow on the floor for grounding.
- Large furniture props should cast shadows for spatial depth.
- Small props (pens, cups, accessories) do not need individual shadows.

## Post-Processing

### Target Post-Processing Stack

| Effect | Setting | Purpose |
| --- | --- | --- |
| Bloom | Low intensity, wide threshold | Soft window glow, desk lamp warmth |
| Color grading | Slight warm lift in shadows, gentle saturation | Consistent cozy tone |
| Vignette | Very subtle, 0.2–0.3 intensity | Gentle focus draw toward center |
| Ambient occlusion | SSAO or baked, low-medium intensity | Prop grounding without harsh corner darkening |
| Tonemapping | ACES or neutral | Consistent HDR to LDR mapping |

### Post-Processing Rules

- Do not use depth of field — all zones must be in focus for readability.
- Do not use motion blur — this is a static-camera room, not a game.
- Do not use chromatic aberration or film grain — this is not a cinematic look.
- Do not use screen-space reflections — not needed for matte surfaces.

## Camera Specification

### Camera Mode

The frozen camera mode for B-series is **orthographic**.

Rationale:
- Orthographic avoids perspective distortion on the avatar and furniture.
- The existing `RoomRuntime.cs` already implements orthographic focus presets.
- Orthographic gives a clean 2D-like framing that works well with the desktop UI composition.

### Camera Presets

| Preset | Orthographic size | Target area | Transition |
| --- | --- | --- | --- |
| `overview` | 5.2 | Full room | Default state; smooth transition from any preset |
| `avatar` | 3.6 | Avatar center, upper body emphasis | Smooth zoom + pan toward avatar |
| `desk` | 4.2 | Desk area with avatar visible | Smooth pan toward desk zone |
| `wardrobe` | 3.2 | Wardrobe area, avatar centered | Smooth zoom + pan toward wardrobe |

### Camera Rules

- Camera position is always on the near (viewer-facing) side of the room.
- Camera Z depth should be constant; only XY position and ortho size change.
- Camera transitions should use smooth interpolation (ease-in-out, ~0.3–0.5s).
- Camera must never clip into room geometry or props.
- Minimum ortho size is 2.0 (close-up safety cap).
- Maximum ortho size is 7.0 (widest room view safety cap).

## Performance Budget

### Geometry

| Category | Triangle budget | Notes |
| --- | --- | --- |
| Room shell (walls, floor, ceiling) | 2,000–5,000 | Simple box geometry with window opening |
| Large furniture (bed, desk, wardrobe) | 3,000–8,000 each | Moderate detail, soft edges |
| Medium props (chair, shelf, lamp) | 1,000–3,000 each | Clean silhouette, simple form |
| Small props (books, cup, plant) | 200–1,000 each | Low detail, shape-driven |
| Total room scene | 40,000–80,000 | Comfortable for URP on desktop |

### Draw Calls

- Target: under 100 draw calls for the full room scene (excluding avatar).
- Use material sharing and texture atlases where possible.
- Use GPU instancing for repeated small props.

### Texture Memory

| Category | Max resolution | Format |
| --- | --- | --- |
| Room atlas / large diffuse | 2048 × 2048 | BC7 or BC1 compressed |
| Per-prop textures | 512 × 512 to 1024 × 1024 | BC7 compressed |
| Normal maps | 512 × 512 to 1024 × 1024 | BC5 compressed |
| Total texture memory | Under 64 MB for room assets | |

### Frame Rate Target

- Target: stable 60 FPS on a mid-range desktop GPU (GTX 1060 / RX 580 class).
- The room is a single static scene — there is no reason to drop below 60 FPS.
- If avatar animations cause frame drops, optimize avatar LOD before reducing room quality.

## Scale and Proportion Rules

### World Units

- 1 Unity unit = 1 meter.
- Avatar height: approximately 1.6 units (matching avatar spec proportions).
- Desk height: approximately 0.72 units (standard desk height, proportionally adjusted for anime scale).
- Chair seat: approximately 0.44 units.
- Bed height: approximately 0.45 units.
- Door height: approximately 1.95 units.

### Proportion Style

- Furniture proportions are slightly compressed compared to real-world ratios, matching the anime avatar scale.
- Chairs and desks are slightly wider relative to height (comfortable, not cramped).
- The room feels cozy because of proportion, not because walls are too close to camera.

## Naming Conventions

### Textures

```
TEX_Room_{Object}_{Channel}_v{NNN}
```

Examples:
- `TEX_Room_Desk_Albedo_v001.png`
- `TEX_Room_Floor_Normal_v001.png`

### Materials

```
MAT_Room_{Object}_{Variant}
```

Examples:
- `MAT_Room_Desk_Main`
- `MAT_Room_Wall_Default`
- `MAT_Room_Curtain_Lavender`

### Prefabs

```
PFB_Room_{Object}_v{NNN}
```

Examples:
- `PFB_Room_Desk_v001`
- `PFB_Room_Bed_v001`

## Related Documents

- `docs/product/room-vision.md` — room concept, layout, and scope
- `docs/product/room-asset-spec.md` — prop inventory and naming
- `docs/avatar-spec.md` — avatar technical specification
- `ai-dev-system/domain/room/contracts/focus-presets.json` — current focus preset truth
