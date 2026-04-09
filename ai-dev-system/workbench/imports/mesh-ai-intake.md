# Mesh AI Intake

Current implementation: Mesh AI raw imports still arrive in root import folders, while `ai-dev-system/workbench/` now owns the intake guidance and naming expectations for that flow.

Recommended intake flow:

1. Keep the original Mesh AI export in its current root import folder.
2. Create or update an intake manifest under `ai-dev-system/asset-pipeline/manifests/`.
3. Assign one of the current profiles:
   - `unity_prop_v1`
   - `unity_room_item_v1`
   - `unity_avatar_accessory_v1`
   - `unity_avatar_clothing_v1`
4. Track lifecycle state as `raw`, `cleaned`, `validated`, or `export-ready`.
5. Point report and render evidence at the current root artifact paths under `tools/reports/` and `tools/renders/`.
6. If matching root artifacts already exist, refresh the sibling lifecycle manifests with `python ai-dev-system/workflows/mesh_ai_refine.py --intake-manifest <path> --output-dir <dir> --sync-lifecycle-manifests`.
7. On a machine with Blender installed, capture a live wrapper run with `python ai-dev-system/workflows/mesh_ai_refine.py --intake-manifest <path> --output-dir <dir> --execute-wrappers --blender-executable <path-to-blender.exe> --sync-lifecycle-manifests`.

Current root-to-logical ownership mapping:

- `Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/`
  - current raw Mesh AI import root
  - logical ownership in the new flow: `raw`
- `bleder/`
  - current cleaned authoring root
  - logical ownership in the new flow: `cleaned` and `export-ready` authoring intermediates
- `tools/reports/`
  - current validation artifact root
  - logical ownership in the new flow: `validated`
- `tools/renders/`
  - current preview and review artifact root
  - logical ownership in the new flow: review evidence for `validated` and `export-ready`

Manual validation required:

- Do not claim a raw Mesh AI import is validated or export-ready until the matching Blender reports exist.
- Do not move large `.fbx`, `.blend`, `.png`, or generated report files just to match the logical ownership model.
