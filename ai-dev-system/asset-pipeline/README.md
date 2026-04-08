# Asset Pipeline

Current implementation: `ai-dev-system/asset-pipeline/` now owns the current tool catalog, structure validator, and migration notes for non-backend asset conversion and validation flow.

Important boundary:

- Root `tools/` still owns the current executable helper scripts used for Blender authoring and validation.
- This Phase 6 pass does not move or rewrite the existing Blender helper scripts.

## Current Ownership

- `tool-catalog.json`
  - current source-root map of helper scripts grouped by pipeline role
- `toolchain-map.json`
  - deterministic profile-to-wrapper mapping for the Mesh AI refine foundation
- `profiles/`
  - current Mesh AI lifecycle profiles for props, room items, accessories, and clothing
- `schemas/`
  - current manifest and report shapes for intake, refinement, validation, and export handoff
- `manifests/`
  - normalized lifecycle examples grounded in current repo assets and reports, including ingested execution evidence from root artifacts when present
  - first target-machine wrapper execution summary for the Azure Sakura sample
- `reports/README.md`
  - ownership note for report projections versus root executed artifacts
- `tool-mapping.md`
  - current human-readable mapping between profiles and root Blender helpers
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
- Export-ready packaging helpers are still represented by current reports, manifests, and manual flow, not a fully centralized runtime package.
- Target-machine Blender execution and live Unity prefab or equip mapping remain manual gates even though profile and workflow foundations now exist and Unity now has registry intake for the export handoff manifests.
- The current repo now includes one executed Azure Sakura wrapper pass captured through `workflows/mesh_ai_refine.py --execute-wrappers`, but broader profile coverage and Unity registry consumption still remain follow-up work.

Reminder:

- The Mesh AI foundation still calls into root `tools/` and does not replace that executable helper root.
