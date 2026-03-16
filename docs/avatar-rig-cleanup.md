# Avatar Rig Cleanup

Updated: 2026-03-17

## Outputs

- Source FBX: `unity-client/Assets/Avatar/Model/Meshy_AI_Character_output.fbx`
- Blender working file: `bleder/CHR_Avatar_Base_v001_work.blend`
- Rig-clean Blender file: `bleder/CHR_Avatar_Base_v001_rigclean.blend`
- Rig-clean FBX: `unity-client/Assets/AvatarSystem/AvatarProduction/BaseAvatar/Model/CHR_Avatar_Base_v001_rigclean.fbx`
- Synced split avatar FBX: `unity-client/Assets/AvatarSystem/AvatarProduction/BaseAvatar/Model/CHR_Avatar_Base_v001_split15.fbx`
- Audit reports:
  - `tools/reports/avatar_source_audit.json`
  - `tools/reports/avatar_rigclean_report.json`
  - `tools/reports/avatar_rigclean_audit.json`
  - `tools/reports/avatar_split15_validation.json`
  - `tools/reports/avatar_split15_reaudit.json`
  - `unity-client/Logs/BaseAvatarHumanoidValidation.json`

## Rig Cleanup Applied

- Renamed spine chain to match hierarchy:
  - `Spine02` -> `Spine`
  - `Spine01` -> `Spine01`
  - `Spine` -> `Spine02`
- Renamed `neck` -> `Neck`
- Renamed mesh object `char1` -> `Body_Base`
- Blender split script now accepts both `Neck` and `neck`

## Pose / Humanoid Audit

- Current bind pose is `A-pose-like`
- Upper arm angle from horizontal: ~49.9 degrees on both sides
- Minimum Unity Humanoid requirement: PASS
- Missing optional humanoid bones: `LeftEye`, `RightEye`, `Jaw`
- Finger chains: not present

## Recommended Unity Humanoid Mapping

| Unity slot | Bone |
|---|---|
| Hips | `Hips` |
| Spine | `Spine` |
| Chest | `Spine01` |
| UpperChest | `Spine02` |
| Neck | `Neck` |
| Head | `Head` |
| LeftShoulder | `LeftShoulder` |
| LeftUpperArm | `LeftArm` |
| LeftLowerArm | `LeftForeArm` |
| LeftHand | `LeftHand` |
| RightShoulder | `RightShoulder` |
| RightUpperArm | `RightArm` |
| RightLowerArm | `RightForeArm` |
| RightHand | `RightHand` |
| LeftUpperLeg | `LeftUpLeg` |
| LeftLowerLeg | `LeftLeg` |
| LeftFoot | `LeftFoot` |
| LeftToes | `LeftToeBase` |
| RightUpperLeg | `RightUpLeg` |
| RightLowerLeg | `RightLeg` |
| RightFoot | `RightFoot` |
| RightToes | `RightToeBase` |

## Unity Validation Result

- Validated in Unity Editor on 2026-03-17
- Import settings used:
  - `Animation Type = Humanoid`
  - `Avatar Definition = Create From This Model`
  - `Auto Generate Avatar Mapping = true`
- Result: `0 errors, 0 warnings`
- Final mapping in Unity matches the expected mapping table above

## Split15 Sync Result

- `CHR_Avatar_Base_v001_split15.fbx` has been regenerated from `CHR_Avatar_Base_v001_rigclean.fbx`
- Validation result:
  - `15 / 15` region meshes present
  - Total polygons = `123272` (matches source)
  - All split meshes remain bound to `Armature`
  - `Body_Head` now carries `Neck` from the cleaned rig

## Follow-up Note

- Unity Inspector still shows a separate project-level warning about `Skin Weights` quality settings. This does not invalidate humanoid mapping, but it is worth fixing before animation/prefab polish so bone influence is not overly limited at runtime.
