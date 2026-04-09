# Mesh AI Tool Mapping

Current implementation: the Mesh AI refine foundation maps profile-driven workflow planning onto the existing root `tools/` Blender helpers without moving those scripts.

## Active Profiles

| Profile | Asset focus | Current helper mapping | Manual gate boundary |
| --- | --- | --- | --- |
| `unity_prop_v1` | Props in room | `tools/blender_asset_audit.py`, `tools/blender_render_asset_front.py` | Pivot/origin normalization still needs human review because there is no generic prop pivot helper yet |
| `unity_room_item_v1` | Room/static assets | `tools/blender_asset_audit.py`, `tools/blender_render_asset_front.py` | Placement, floor contact, and pivot review are still manual |
| `unity_avatar_accessory_v1` | Accessories foundation | `tools/blender_asset_audit.py`, `tools/blender_render_asset_front.py` | Slot anchoring, attachment review, and live equip mapping are still manual |
| `unity_avatar_clothing_v1` | Clothing foundation | `tools/blender_asset_audit.py`, `tools/blender_fit_dress_scene.py`, `tools/blender_scene_state_report.py`, `tools/blender_verify_dress_scene.py`, `tools/blender_render_asset_front.py` | Target-machine Blender execution and live equip mapping are still manual gates |

## Supporting Helpers

- `tools/blender_clean_avatar_rig.py`
  - supported root helper for avatar body cleanup foundations, but not automatically invoked by the new Mesh AI clothing profile unless a future profile explicitly needs it
- `tools/blender_split_body_regions.py`
  - supported root helper for avatar body-region prep, not for generic props or room items
- `tools/blender_fit_dress_shrinkwrap_transfer.py`
  - alternate clothing-fit helper still available for future profile variants
- `tools/blender_add_facial_blendshapes.py`
  - avatar-head workflow helper and out of scope for the current Mesh AI asset groups

## Out Of Scope Foundation

- Avatar full-body automatic repair is not claimed as complete in current implementation.
- `wardrobe.equipItem` can now resolve Unity handoff registry entries, but live `AvatarItemDefinition` mapping is still planned.
- No profile in this phase generates a mesh from scratch from prompt text.
