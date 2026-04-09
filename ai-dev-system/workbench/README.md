# Workbench

Current implementation: `ai-dev-system/workbench/` now owns the current inventory, naming guidance, and migration notes for non-runtime asset authoring material.

Important boundary:

- Root `bleder/`, root import folders, and root `tools/renders` or `tools/reports` still hold the current workbench material and artifacts.
- This Phase 6 pass does not move large `.blend`, `.fbx`, `.png`, or generated artifact files.

## Current Ownership

- `inventory/`
  - current source-root snapshot for Blender files, imported source assets, and report roots
- `blender/`
  - ownership note for Blender authoring material
- `imports/`
  - ownership note for raw import sources such as Meshy exports
  - Mesh AI intake guidance for the new lifecycle contract
  - `imports/mesh-ai-intake.md`
- `reports/`
  - ownership note for renders, audits, and review artifacts
- `naming-convention.md`
  - status naming guidance for raw, cleaned, validated, and export-ready asset flow

## Current Truth Sources

- root Blender authoring:
  - `../../bleder/`
- root imported source material:
  - `../../Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/`
- root rendered or report artifacts:
  - `../../tools/renders/`
  - `../../tools/reports/`

## Planned Work Still Not Done

- The actual workbench materials have not been physically moved under `ai-dev-system/workbench/` yet.
- No `Clothy3D_Studio/` root is present in the current repo snapshot, so there is nothing to migrate for that slice in this phase.
- Unity asset consumption rules are still enforced by current code and docs, not by a new automated import pipeline inside `workbench/`.
- Mesh AI lifecycle ownership is documented here, but the current raw imports and Blender outputs still live in their existing root folders.
