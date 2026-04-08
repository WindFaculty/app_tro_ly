# Mesh AI -> Blender -> Unity Integration

Updated: 2026-04-08
Status: Current implementation plus design-target foundation

## Purpose

This document defines the current repo-safe integration boundary for the Mesh AI asset refine flow:

`Mesh AI raw asset -> workbench intake metadata -> asset profile selection -> control-plane orchestration -> Blender refinement wrappers -> validation report -> export-ready handoff -> future Unity import`

This phase builds the production-safe foundation for that flow inside the current repo architecture.
It does not claim that Unity runtime wardrobe equip mapping or room prefab hookup are already complete.

## Current Implementation

- `docs/architecture/blender-mcp-integration.md` now documents the optional Blender MCP interactive lane used for attach or inspect or viewport capture without changing the production-safe wrapper pipeline.
- `ai-dev-system/asset-pipeline/` now owns:
  - lifecycle profiles
  - manifest and schema shapes
  - deterministic toolchain mapping
  - example lifecycle manifests with ingested root artifact evidence
  - target-machine execution summary evidence for the first Azure Sakura wrapper pass
- `ai-dev-system/workbench/` now owns:
  - Mesh AI intake guidance
  - naming guidance for `raw`, `cleaned`, `validated`, and `export-ready`
  - logical ownership notes for current root import and artifact folders
- `ai-dev-system/control-plane/` now owns:
  - Mesh AI Blender wrapper rendering in `control-plane/tools/mesh_ai_blender_wrappers.py`
  - profile and manifest planning in `control-plane/tools/mesh_ai_pipeline.py`
  - executed-artifact ingestion back into the refinement, validation, and handoff manifests
  - planner and executor style foundations in `control-plane/planner/mesh_pipeline_planner.py` and `control-plane/executor/mesh_pipeline_executor.py`
  - workflow runner in `workflows/mesh_ai_refine.py`
  - workflow specs in `workflows/mesh-ai-refine-*.yaml`
- `ai-dev-system/domain/` now documents:
  - clothing and accessory handoff expectations
  - room-item handoff expectations
  - shared boundary notes for asset handoff metadata

## Current Boundaries

### Control plane

- Owns orchestration logic, profile resolution, workflow selection, and deterministic wrapper rendering.
- Does not replace the current root Blender helpers.

### Asset pipeline

- Owns profile, schema, manifest, and tool-mapping truth for the Mesh AI refine foundation.
- Does not claim the root Blender scripts have moved.

### Workbench

- Owns intake guidance, inventory, naming, and logical stage ownership.
- Does not move large `.blend`, `.fbx`, `.png`, or generated report files in this phase.

### Domain

- Owns semantic handoff notes for customization and room assets.
- Does not own typed transport envelopes; those still live in `packages/contracts/`.

### Unity runtime

- Current implementation still uses `ai-dev-system/clients/unity-client/`.
- No new business UI is added in Unity for this phase.
- `Assets/Scripts/Runtime/MeshAssetRegistry.cs` now intakes export-ready handoff manifests for wardrobe foundations plus room assets inside the Unity runtime.
- `Assets/Scripts/Runtime/UnityBridgeClient.cs` now uses that registry for room-focus alias fallback and registry-aware wardrobe diagnostics.
- Live `AvatarItemDefinition` or prefab hookup is still planned work.

## Root Tool Boundary

Current implementation:

- Root `tools/` remains the executable helper root for Blender authoring and validation scripts.
- The new Mesh AI foundation only wraps and catalogs those scripts.
- The optional Blender MCP lane is a separate interactive control surface and does not replace the deterministic wrapper flow.
- This phase does not move or duplicate the Blender helpers into `ai-dev-system/asset-pipeline/`.

## Lifecycle Contract

The current lifecycle states are:

- `raw`
  - Mesh AI import has been received and metadata has been captured
- `cleaned`
  - refinement plan has produced or targeted a cleaned authoring output
- `validated`
  - validation evidence exists or the normalized validation report has been prepared with explicit checks and manual gates
  - lifecycle manifests can ingest existing root reports or renders or cleaned blends back into normalized metadata
- `export-ready`
  - the asset has a future Unity-facing handoff manifest and explicit manual gates for runtime import work

## Planned Work

- A fresh target-machine Blender execution pass for real asset runs still needs manual validation even though current manifests can now ingest existing root artifacts and command specs.
- Unity runtime still does not have end-to-end `AvatarItemDefinition` mapping for clothing or accessory handoff entries.
- Unity runtime still does not have live prefab hookup for `room_item` or `prop` handoff entries beyond `room_focus_preset` fallback behavior.
- `wardrobe.equipItem` remains a typed bridge surface without end-to-end runtime equip behavior.
- Avatar full-body automatic repair is not claimed as complete in this phase.

## Design Target

The design target for this integration is:

1. Mesh AI raw import arrives in a current root import folder.
2. Intake metadata is captured in a normalized manifest under `ai-dev-system/asset-pipeline/manifests/`.
3. The control plane selects a profile and workflow spec deterministically.
4. Blender helper wrappers build the correct command plan against root `tools/`.
5. Validation and handoff manifests are emitted with explicit manual gates plus ingested execution evidence when root artifacts already exist.
6. A future Unity registry consumes the export-ready handoff manifest without ad-hoc bridge changes.

Current implementation note:

- `python ai-dev-system/workflows/mesh_ai_refine.py --intake-manifest <path> --output-dir <dir> --execute-wrappers --blender-executable <path-to-blender.exe> --sync-lifecycle-manifests`
  - runs the current wrapper plan on a target machine
  - writes an execution summary beside the intake manifest
  - syncs the sibling refinement or validation or handoff manifests from the resulting root artifacts

## Manual Validation Required

- Running Blender wrappers against real assets on a target machine with Blender installed
- Verifying the resulting authoring artifacts in Blender or Unity
- Wiring live Unity prefab or `AvatarItemDefinition` mapping for imported assets
- Any claim that a wardrobe or room asset is live in current runtime behavior
