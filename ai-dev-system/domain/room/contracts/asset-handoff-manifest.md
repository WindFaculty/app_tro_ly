# Room Item Asset Handoff

Current implementation: room item handoff metadata is now defined by the Mesh AI export handoff manifest shape in `../../../asset-pipeline/schemas/export-handoff-manifest.schema.json`.

Current room-domain additions:

- `room_item` handoff manifests can record:
  - `target_unity_path`
  - `room_focus_preset`
  - validation evidence paths
  - manual gates for placement or pivot review

Planned work:

- The current Unity runtime can intake export-ready `room_item` and `prop` manifests and use `room_focus_preset` as a fallback alias mapping when a concrete scene object is missing.
- A richer runtime room-item registry with live scene-object hookup is still not implemented.
- The current room focus presets remain the only shipped room-facing runtime behavior.
