# Chua Mo Hinh Intake Classification

Historical note: the pending assets that were previously dropped into `chua_mo_hinh/` were classified during an earlier Unity-tree intake pass. The former destination paths are no longer the live Unity runtime source of truth after the `apps/unity-runtime` cutover.

## Blender Intake

These assets were treated as avatar or wardrobe-side inputs that should be reviewed or edited in Blender before runtime use:

| Source asset | Classification | Destination |
| --- | --- | --- |
| `Meshy_AI_Aurora_Cascade_0408044848_texture_fbx/` | Avatar clothing candidate | `bleder/intake/avatar/AuroraCascade/` |
| `Meshy_AI_Crown_of_Waves_0408064507_texture_fbx/` | Avatar accessory candidate | `bleder/intake/avatar/CrownOfWaves/` |
| `Meshy_AI_Character_output.fbx` | Avatar reference mesh / character source | `bleder/intake/avatar/CharacterOutput/` |

## Unity Room Props

These assets were treated as room-side props and mapped to the former Unity client tree at the time:

| Source asset | Classification | Destination |
| --- | --- | --- |
| `019d68f0-a0ef-7b4e-960a-b1f5aff0676d/` | Decor plant | historical former Unity room-prop destination |
| `019d68f1-52df-74ca-89b9-c79ae7d22879/` | Furniture wardrobe | historical former Unity room-prop destination |
| `019d68f1-668c-74ca-9a5e-a24e7fb3a055/` | Decor desk accessory | historical former Unity room-prop destination |
| `019d68f1-7d31-7dd7-981b-1bb7040cfc3b/` | Decor plush | historical former Unity room-prop destination |
| `019d68f1-9968-7bad-942b-989ff4ec8988/` | Window prop | historical former Unity room-prop destination |
| `019d68f1-b752-7bae-b1de-7b3cb1a571a8/` | Wall shelf | historical former Unity room-prop destination |
| `019d68f1-cce7-7bae-9e9e-75c14b3ffab4/` | Furniture table | historical former Unity room-prop destination |
| `019d68f1-f995-74f4-82db-92ee07d5dd44/` | Wall art / poster | historical former Unity room-prop destination |
| `019d68f2-4251-7e0e-b76a-3fa4302350a5/` | Furniture bed | historical former Unity room-prop destination |
| `019d68fb-6c79-7d60-bbb3-f4187b4aef1b/` | Electronics monitor | historical former Unity room-prop destination |
| `019d68fb-84d5-7779-ad8a-afbff4222851/` | Electronics keyboard | historical former Unity room-prop destination |
| `019d68fb-a02d-7779-b589-c160954bb78b/` | Electronics mouse | historical former Unity room-prop destination |
| `Meshy_AI_Oak_and_Steel_Desk_0407173224_texture_fbx/` | Furniture desk | historical former Unity room-prop destination |

## Notes

- The classification above is an inference from the Meshy export names because the source folder did not include separate authored metadata.
- Any room-prop path listed above should now be treated as historical evidence, not a live runtime path.
- The Blender-side assets were intentionally routed into `bleder/intake/avatar/` rather than merged into the existing authoring `.blend` files so they can be reviewed before being folded into the production avatar pipeline.
