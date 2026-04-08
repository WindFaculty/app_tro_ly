# Mesh AI Reports

Current implementation: report ownership for the Mesh AI refine foundation is split across two layers.

- Root `tools/reports/`
  - current executable Blender helper output root
- Root `tools/renders/`
  - current render artifact root
- `ai-dev-system/asset-pipeline/manifests/`
  - normalized report and handoff projections that point at those root artifacts

Manual validation required:

- Re-running Blender helpers still requires a target machine with Blender installed.
- Unity import plus live prefab or equip mapping remains a separate follow-up after authoring reports and registry intake exist.
