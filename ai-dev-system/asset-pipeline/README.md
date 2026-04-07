# Asset Pipeline

Current implementation: `ai-dev-system/asset-pipeline/` now owns the current tool catalog, structure validator, and migration notes for non-backend asset conversion and validation flow.

Important boundary:

- Root `tools/` still owns the current executable helper scripts used for Blender authoring and validation.
- This Phase 6 pass does not move or rewrite the existing Blender helper scripts.

## Current Ownership

- `tool-catalog.json`
  - current source-root map of helper scripts grouped by pipeline role
- `validate-phase6-structure.ps1`
  - repeatable PowerShell validation for the current Phase 6 workbench and pipeline source roots

## Current Truth Sources

- root helper scripts:
  - `../../tools/blender_add_facial_blendshapes.py`
  - `../../tools/blender_asset_audit.py`
  - `../../tools/blender_audit_avatar.py`
  - `../../tools/blender_clean_avatar_rig.py`
  - `../../tools/blender_fit_dress_scene.py`
  - `../../tools/blender_fit_dress_shrinkwrap_transfer.py`
  - `../../tools/blender_render_arm_variants.py`
  - `../../tools/blender_render_asset_front.py`
  - `../../tools/blender_scene_state_report.py`
  - `../../tools/blender_split_body_regions.py`
  - `../../tools/blender_validate_split_avatar.py`
  - `../../tools/blender_verify_dress_scene.py`

## Planned Work Still Not Done

- The helper scripts themselves have not been moved under `ai-dev-system/asset-pipeline/` yet.
- No unified import-normalization command exists yet for all raw asset sources.
- Export-ready packaging helpers are still represented by current reports and manual flow, not a fully centralized pipeline package.
