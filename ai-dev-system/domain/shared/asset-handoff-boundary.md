# Shared Asset Handoff Boundary

Current implementation: Mesh AI asset handoff metadata belongs to domain semantics, while typed bridge envelopes still belong to `packages/contracts/`.

What belongs here:

- semantic meaning of `target_unity_path`
- lifecycle stages `raw`, `cleaned`, `validated`, `export-ready`
- handoff notes shared by customization and room domains

What does not move here in this phase:

- React or Tauri or Unity transport envelopes
- runtime bridge command additions
- ad-hoc string protocols

Current boundary note:

- No new Unity bridge commands are required for this Mesh AI handoff foundation phase.
