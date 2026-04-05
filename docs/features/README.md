# Feature Docs

Use this folder as the feature-level navigation layer for current implementation and feature-specific specs.

## Current Feature References

- [../04-ui.md](../04-ui.md) - current Unity UI behavior and placeholder boundaries
- [avatar-asset-spec.md](avatar-asset-spec.md) - Phase 4 asset contract, registry boundary, slot system, and metadata schema
- [avatar-asset-intake-checklist.md](avatar-asset-intake-checklist.md) - intake checklist for new avatar items, presets, and conflict rules
- [room-object-intake-checklist.md](room-object-intake-checklist.md) - intake checklist for room objects and prefab-backed Character Space assets
- [../avatar-spec.md](../avatar-spec.md) - avatar asset and runtime notes that already exist in the repo
- [../avatar-rig-cleanup.md](../avatar-rig-cleanup.md) - avatar rig cleanup notes and supporting detail

## Planned Feature Specs

- [room-world-plan.md](room-world-plan.md) - Phase 0 plan for a room-backed Character Space subsystem inside the current Unity shell
- [room-object-spec.md](room-object-spec.md) - Phase 0 object metadata, registry, naming, and interaction rules for room content

## Notes

- Some feature docs still live at the top of `docs/` today. This page is the stable navigation point for Phase 3 and does not claim those files have already been moved.
- When new feature-specific specs are added, prefer placing them under `docs/features/` unless an existing current-state doc already owns that truth.
