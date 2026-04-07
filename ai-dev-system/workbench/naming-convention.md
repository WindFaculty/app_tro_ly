# Workbench Naming Convention

Current implementation: the repo currently uses descriptive file names such as `rigclean`, `split15`, `validation`, `report`, and `polishfit`, but it does not yet have a single normalized workbench naming contract.

This Phase 6 document defines the intended status labels for authoring flow without claiming the root files already follow them consistently.

## Status Labels

- `raw`
  - source import received from Meshy or other external tooling
  - example current source: `Meshy_AI_Azure_Sakura_Kimono_0326010047_texture.fbx`
- `cleaned`
  - geometry or rig has been cleaned, renamed, or repaired
  - example current source signal: `CHR_Avatar_Base_v001_rigclean.blend`
- `validated`
  - validator or audit output confirms the asset meets the expected checks
  - example current artifact signal: `avatar_base_v001_validation.json`
- `export-ready`
  - asset is ready to hand off into Unity production paths
  - example current artifact signal: `avatar_base_v001_export_report.json`

## Current Gap

- The current repo mostly encodes these states in filenames rather than directory stages.
- The actual directory migration into `ai-dev-system/workbench/` and `ai-dev-system/asset-pipeline/` is still planned work.
