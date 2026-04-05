# ADR-005: Plan Character Space As A Room-Backed World Subsystem

- Status: Accepted as a planning baseline
- Date: 2026-04-05

## Context

- The current assistant shell still renders the center stage through the Home orbit placeholder and does not mount a real room.
- The requested Character Space direction now includes a 3D room, object registry, future interaction points, and avatar placement rules.
- The modularization freeze does not allow a second runtime root, ad-hoc scene ownership, or UI controllers absorbing world logic.

## Decision

- Plan the future Character Space room as a world subsystem inside the current `unity-client/` tree.
- Keep the MVP room mounted in the active assistant Unity scene rather than making Character Space a separate launcher or scene-managed app flow.
- Treat room objects as registry-backed content rather than hand-placed runtime truth.
- Keep Character Space UI overlay thin and separate from room bootstrap, room-object ownership, and interaction state.
- Keep avatar integration behind explicit spawn or attention or bridge contracts rather than embedding room assumptions inside `AssistantApp` or UI controllers.

## Options Considered

### Make Character Space a separate scene-first app flow

- Rejected because the current shell, runtime coordinator, and UI overlay all assume one active assistant scene and do not yet own route-level scene loading.

### Keep room content as ad-hoc scene decoration managed from UI code

- Rejected because that would blur world ownership, make object intake inconsistent, and cause future interaction logic to accumulate in presentation code.

## Consequences

- Later room work can grow inside explicit `World/` and `Features/CharacterSpace/` boundaries without inventing a second runtime.
- The current orthographic placeholder camera is now explicitly a temporary baseline for the center stage.
- Future room implementation still needs tracker scope, docs updates, and Unity smoke before any shipped-behavior claim.
