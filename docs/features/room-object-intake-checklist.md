# Room Object Intake Checklist

Use this checklist before treating a new Character Space room object as part of the current room foundation.

## Object Intake

- Asset or prefab name follows the naming rules in [room-object-spec.md](room-object-spec.md).
- Prefab lives under the correct `Assets/World/Prefabs/` category folder.
- `RoomObjectDefinition` entry exists in the registry path that owns the target room slice.
- `id`, `displayName`, `category`, `prefabKey`, `prefabPath`, `anchorType`, and `defaultScale` are filled.
- `interactionType`, `selectable`, `hoverable`, and `inspectable` match the intended behavior.
- Collider expectations are intentional for the chosen interaction flags.
- Tags and optional states are de-duplicated and meaningful.
- `Tools > TroLy > Validate Room Object Registry` passes without new errors.
- `Tools > TroLy > Validate Room Object Prefab Intake` passes before calling the object prefab-backed.

## Layout Intake

- The object is referenced by id from `RoomLayoutDefinition` or the layout owner for that room slice.
- Placement anchor and local transform are intentional for the room cluster.
- The room still reads clearly after adding the object; do not crowd the MVP room beyond its current density without updating the room plan.

## Manual Validation Required

- Home stage smoke confirms the object scale, pivot, and silhouette look correct from the stage camera.
- Selection, hover, focus, and action-dock hints match the intended interaction contract.
- `P02` live UI evidence is refreshed if the object changes visible Character Space behavior.
