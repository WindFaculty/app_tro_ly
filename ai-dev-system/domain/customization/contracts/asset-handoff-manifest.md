# Customization Asset Handoff

Current implementation: clothing and accessory handoff metadata is now defined by the Mesh AI export handoff manifest shape in `../../../asset-pipeline/schemas/export-handoff-manifest.schema.json`.

Current handoff scope:

- `avatar_accessory`
  - future Unity-facing accessory import metadata
- `avatar_clothing`
  - future Unity-facing clothing foundation import metadata

Current boundary:

- The handoff manifest can describe `slot`, `target_unity_path`, validation evidence, and manual gates.
- The current Unity runtime can now intake export-ready clothing and accessory handoff manifests into a lookup registry and resolve them into `AvatarItemDefinition` equip behavior when a matching runtime item is registered in `AvatarItemRegistry`.
- It does not mean the Unity runtime already has production prefab hookup for every handoff entry.
- It does not mean the runtime item catalog is broadly populated beyond the placeholder-safe sample assets checked into the repo.

Manual validation required:

- Prefab hookup and broader production-asset population remain follow-up work after the authoring handoff manifest exists.
