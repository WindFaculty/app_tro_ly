# Mesh AI Manifests

Current implementation: this folder stores normalized example manifests for the Mesh AI -> Blender -> validation -> Unity handoff foundation.

Lifecycle examples in this folder:

- `mesh-ai-azure-sakura-intake.json`
  - `raw` intake metadata grounded in the current Mesh AI kimono source asset
- `mesh-ai-azure-sakura-refinement-plan.json`
  - `cleaned` plan projection that maps the intake onto current Blender helper scripts and records ingested execution evidence when matching root artifacts exist
- `mesh-ai-azure-sakura-validation-report.json`
  - `validated` normalized report projection backed by existing root `tools/reports/` evidence plus per-step artifact ingestion
- `mesh-ai-azure-sakura-handoff.json`
  - `export-ready` handoff manifest for future Unity import work with ingested authoring evidence and wrapper command specs
- `mesh-ai-azure-sakura-execution-pass.json`
  - target-machine execution summary for the first Blender 5.1 wrapper pass against the Azure Sakura sample

Boundary notes:

- These manifests are governance and contract examples, not a claim that the asset has already been registered in Unity runtime code.
- Root `tools/reports/` and `tools/renders/` remain the current evidence roots for executed Blender authoring artifacts.
- `python ai-dev-system/workflows/mesh_ai_refine.py --intake-manifest <path> --output-dir <dir> --sync-lifecycle-manifests` is the current repo-side way to refresh the sibling refinement or validation or handoff manifests from the intake plus root artifact evidence.
- `python ai-dev-system/workflows/mesh_ai_refine.py --intake-manifest <path> --output-dir <dir> --execute-wrappers --blender-executable <path-to-blender.exe> --execution-summary-path <path> --sync-lifecycle-manifests` is the current repo-side way to run the wrapper plan on a target machine and capture command-level execution evidence.
