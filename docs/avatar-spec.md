# Avatar Technical Specification

Updated: 2026-03-15

## 1. Naming Conventions

| Category | Pattern | Example |
|----------|---------|---------|
| Base avatar | `CHR_Avatar_Base_v{NNN}.fbx` | `CHR_Avatar_Base_v001.fbx` |
| Hair | `CHR_Hair_{Style}_v{NNN}.fbx` | `CHR_Hair_ShortA_v001.fbx` |
| Hair accessory | `ACC_HairAcc_{Name}_v{NNN}.fbx` | `ACC_HairAcc_RibbonA_v001.fbx` |
| Top | `CHR_Top_{Style}_v{NNN}.fbx` | `CHR_Top_CasualA_v001.fbx` |
| Bottom | `CHR_Bottom_{Style}_v{NNN}.fbx` | `CHR_Bottom_SkirtA_v001.fbx` |
| Dress | `CHR_Dress_{Style}_v{NNN}.fbx` | `CHR_Dress_FormalA_v001.fbx` |
| Socks | `CHR_Socks_{Style}_v{NNN}.fbx` | `CHR_Socks_KneeA_v001.fbx` |
| Shoes | `CHR_Shoes_{Style}_v{NNN}.fbx` | `CHR_Shoes_SneakerA_v001.fbx` |
| Gloves | `CHR_Gloves_{Style}_v{NNN}.fbx` | `CHR_Gloves_FullA_v001.fbx` |
| Bracelet L | `ACC_Bracelet_L_{Name}_v{NNN}.fbx` | `ACC_Bracelet_L_GoldA_v001.fbx` |
| Bracelet R | `ACC_Bracelet_R_{Name}_v{NNN}.fbx` | `ACC_Bracelet_R_GoldA_v001.fbx` |
| Animation | `ANIM_{Action}_v{NNN}.fbx` | `ANIM_Idle_Default_v001.fbx` |

## 2. Equipment Slots

| Slot | Notes |
|------|-------|
| Hair | One active. Tags: short, medium, long, twin-tail, bun |
| HairAccessory | Requires Hair. Anchor variants: left, right, center |
| Top | Blocked by Dress. Tags: short-sleeve, long-sleeve, croptop, loose, jacket |
| Bottom | Blocked by Dress. Types: shorts, skirt-short, trousers |
| Dress | Blocks Top + Bottom. May conflict with long Hair or high-collar items |
| Socks | Optional. Types: short, knee, thigh-high |
| Shoes | Types: sneaker, loafer, heels, boots (declare heel coverage level) |
| Gloves | Types: fingerless, full, sleeve-like. May block BraceletL/R |
| BraceletL | Bone-attached to left wrist. May conflict with thick Gloves |
| BraceletR | Bone-attached to right wrist. May conflict with thick Gloves |
| BodyVariant | Reserved: alternate body meshes |
| FaceVariant | Reserved: alternate face meshes |
| AccessoryExtra | Reserved: glasses, bow, headphones, etc. |

## 3. Conflict Matrix

| Equipped ↓ | Blocks → |
|------------|----------|
| Dress | Top, Bottom |
| Gloves (full) | BraceletL, BraceletR |
| Gloves (fingerless) | — |
| Hair (long) | *Check tag compat with high-collar Dress/Top* |
| HairAccessory | — (requires Hair) |

## 4. Body Regions

Used by `AvatarBodyVisibilityManager` to hide body mesh under clothing.

| Region | Typical hiders |
|--------|---------------|
| Head | — |
| TorsoUpper | Top (all), Dress |
| TorsoLower | Top (long), Dress, Bottom |
| ArmUpperL / R | Top (long-sleeve), Dress (long-sleeve) |
| ForearmL / R | Top (long-sleeve), Gloves (full) |
| HandL / R | Gloves (full) |
| ThighL / R | Bottom (trousers), Dress |
| CalfL / R | Bottom (trousers), Socks (thigh-high/knee) |
| FootL / R | Shoes (all), Socks |

## 5. Minimum Blendshape List (28)

### Eyes (6)
`Blink_L`, `Blink_R`, `SmileEye_L`, `SmileEye_R`, `WideEye_L`, `WideEye_R`

### Brows (5)
`BrowUp_L`, `BrowUp_R`, `BrowDown_L`, `BrowDown_R`, `BrowInnerUp`

### Mouth — Emotion (7)
`Smile`, `Sad`, `Surprise`, `AngryLight`, `MouthOpen`, `MouthNarrow`, `MouthWide`, `MouthRound`

### Lip-sync Visemes (9)
`Viseme_Rest`, `Viseme_AA`, `Viseme_E`, `Viseme_I`, `Viseme_O`, `Viseme_U`, `Viseme_FV`, `Viseme_L`, `Viseme_MBP`

> Note: actual naming is flexible as long as `LipSyncMapDefinition` maps `VisemeType` → blendshape name.

## 6. Animator Parameters

| Parameter | Type | Purpose |
|-----------|------|---------|
| `IsListening` | bool | Listening state |
| `IsThinking` | bool | Thinking state |
| `IsSpeaking` | bool | Speaking state |
| `IsMoving` | bool | Locomotion active |
| `MoveSpeed` | float | Current move speed |
| `TurnAngle` | float | Turn direction |
| `GestureIndex` | int | Active gesture (maps to `GestureType` enum) |
| `EmotionIndex` | int | Active emotion (maps to `EmotionType` enum) |

## 7. Animation List

### Prototype (required)
`Idle_Default`, `Idle_Breathing`, `Listen_Idle`, `Talk_Idle`, `Walk_Forward`, `Turn_Left`, `Turn_Right`, `Approach_Short`, `Step_Back`, `Wave_Small`

### Usable build
`Nod_Yes`, `HeadShake_No`, `HandExplain_01`, `HandExplain_02`, `Thinking_Pose`, `Greeting`, `Happy_React`, `Surprise_React`, `Apology_React`, `Wait_Idle`

### Production polish
`Sit_Idle`, `Sit_Talk`, `Lean_In`, `LookAt_Device`, `Idle_Variants`

## 8. Blender / DCC Import Checklist

Before importing an item FBX into Unity:

- [ ] Uses same skeleton/armature as base avatar
- [ ] No extra bones or renamed bones
- [ ] Origin at world center, scale = 1.0
- [ ] Apply All Transforms in Blender (Ctrl+A)
- [ ] Skin weights match base avatar
- [ ] No isolated vertices or flipped normals
- [ ] Material slots named consistently
- [ ] No animation baked into the file (unless it's an animation FBX)
- [ ] Export settings: FBX Binary, Apply Scalings = All Local
- [ ] Test in Unity: idle animation plays without mesh explosions
- [ ] Run `Tools > AvatarSystem > Validate Items` after creating the item ScriptableObject

## 9. Unity Folder Layout

```
Assets/AvatarSystem/
  Core/Scripts/         ← runtime scripts, enums, SO data types
  Core/Materials/       ← shared materials
  Core/Shaders/         ← custom shaders
  AvatarProduction/
    BaseAvatar/         ← model, textures, materials, prefab, animations, animator, facial
    Parts/{Slot}/       ← per-slot item FBX + prefabs
    Presets/            ← outfit, expression, motion presets
    Data/               ← item definitions, conflict rules, hide rules, lip-sync maps
    Scenes/             ← AvatarSandbox, OutfitTest, FacialTest, ConversationTest
    Editor/             ← validators, import tools
  ThirdParty/           ← external lip-sync or animation packages
```

## 10. Scene Test Requirements

| Scene | Purpose |
|-------|---------|
| AvatarSandbox | Base prefab, idle, basic interaction |
| OutfitTest | Equip/unequip, preset, conflict, body hide |
| FacialAndLipSyncTest | Blink, expression, viseme, amplitude lip-sync |
| ConversationTest | Full Listen→Think→Speak→React flow |
