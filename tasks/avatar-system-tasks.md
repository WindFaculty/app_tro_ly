# Nhiệm Vụ Xây Dựng Hệ Thống Avatar

Cập nhật: 2026-03-17

Historical note: references to `unity-client/Logs/...` and `AssistantApp` below describe pre-cutover evidence captured before `D076`; the live Unity runtime now lives in `apps/unity-runtime/`.

---

## Phase 1 — Chốt chuẩn kỹ thuật và naming
- [x] Chốt cấu trúc skeleton
- [x] Chốt danh sách slot trang phục (13 slot)
- [x] Chốt naming convention cho file FBX
- [x] Chốt body region (15 vùng)
- [x] Chốt blendshape spec tối thiểu (28 shape)
- [x] Chốt cấu trúc thư mục `Assets/AvatarSystem/`
- [x] Tạo tài liệu kỹ thuật `docs/avatar-spec.md`
- [x] Tạo bảng slot và conflict matrix
- [x] Tạo code infrastructure (scripts, enums, ScriptableObjects)

---

## Phase 2 — Chuẩn hóa Base Avatar trong Blender
- [x] Import avatar gốc (FBX) vào Blender (`bleder/CHR_Avatar_Base_v001_work.blend`)
- [x] Làm sạch rig (tên bone ổn định, A-pose chuẩn) (`CHR_Avatar_Base_v001_rigclean.fbx`, `bleder/CHR_Avatar_Base_v001_rigclean.blend`)
- [x] Kiểm tra humanoid mapping đúng chuẩn Unity (`BaseAvatarHumanoidValidation.json`: 0 errors, 0 warnings; mapping đúng cho Hips/Spine/Chest/UpperChest/Neck/Head/tay/chân/toes)
- [x] Tách body thành 15 region mesh riêng biệt (export `CHR_Avatar_Base_v001_split15.fbx`, đồng bộ từ `CHR_Avatar_Base_v001_rigclean.fbx`; re-validate OK: 15 mesh, 123272 polys, `Body_Head` có vertex group `Neck`)
- [x] Chốt chiến lược facial mesh (blendshape trên `Body_Head`; `AvatarRootController.faceMesh` sẽ trỏ vào head renderer, chưa dùng face-only mesh riêng; xem `docs/avatar-spec.md`)
- [x] Tạo blendshape tối thiểu cho mắt (Blink_L/R, SmileEye_L/R, WideEye_L/R) (`Body_Head` trong `CHR_Avatar_Base_v001_split15.fbx`; source Blender: `bleder/CHR_Avatar_Base_v001_split15_facial.blend`)
- [x] Tạo blendshape cho lông mày (BrowUp_L/R, BrowDown_L/R, BrowInnerUp) (tạo bằng `tools/blender_add_facial_blendshapes.py`; xem `tools/reports/avatar_facial_blendshape_report.json`)
- [x] Tạo blendshape miệng cảm xúc (Smile, Sad, Surprise, MouthOpen, v.v.) (`Smile`, `Sad`, `Surprise`, `AngryLight`, `MouthOpen`, `MouthNarrow`, `MouthWide`, `MouthRound`)
- [x] Tạo blendshape lip-sync viseme (AA, E, I, O, U, FV, L, MBP, Rest) (`Viseme_Rest`, `Viseme_AA`, `Viseme_E`, `Viseme_I`, `Viseme_O`, `Viseme_U`, `Viseme_FV`, `Viseme_L`, `Viseme_MBP`)
- [x] Export `CHR_Avatar_Base_v001.fbx` sạch (final export ở `Assets/AvatarSystem/AvatarProduction/BaseAvatar/Model/CHR_Avatar_Base_v001.fbx`; validate OK: 15 mesh, 123272 polys, `Body_Head` có 28 blendshape + Basis)
- [x] Import vào Unity, tạo prefab base, test idle chạy ổn (prefab: `Assets/AvatarSystem/AvatarProduction/BaseAvatar/Prefab/CHR_Avatar_Base_v001.prefab`; idle smoke test pass trong `Logs/BaseAvatarPrefabBuildReport.json` với animator/controller/face mesh hợp lệ)
- [x] Tạo body region map trong `AvatarBodyVisibilityManager` (15 mapping hoàn chỉnh trên prefab base; xác nhận `bodyRegionMappingCount = 15`, `bodyRegionMappingsComplete = true`)

---

## Phase 3 — Làm facial và lip-sync tối thiểu
- [x] Kết nối `AvatarFacialController` với face mesh thật (`AvatarFacialController.faceMesh` và `AvatarRootController.faceMesh` cùng trỏ vào `Body_Head`; auto-discover + builder validation đã thêm trong code/prefab, xác minh bằng `unity-client/Logs/BaseAvatarFacialValidation.json`)
- [x] Test chớp mắt tự nhiên (blink coroutine) (technical pass trong `unity-client/Logs/BaseAvatarFacialValidation.json`: `blinkTestPassed = true`, peak Blink_L/R = 100 trên `Body_Head`; visual sign-off trong Unity Editor vẫn là bước manual nếu cần)
- [x] Test 5–8 expression chính (Happy, Sad, Surprised, Focused, SoftSmile, Curious, Apologetic) (`unity-client/Logs/BaseAvatarFacialValidation.json`: `expressionCoveragePassed = true`, `expressionTestsPassed = 7`; dùng bộ `ExpressionDefinition` prototype trong `Assets/AvatarSystem/AvatarProduction/Presets/ExpressionPresets/`. Lưu ý: `tasks/task-people.md` mục `P04` vẫn là bước sign-off production riêng)
- [x] Tạo `ExpressionDefinition` ScriptableObject cho từng expression (đã tạo 7 asset prototype: `EXP_Happy_Prototype`, `EXP_Sad_Prototype`, `EXP_Surprised_Prototype`, `EXP_Focused_Prototype`, `EXP_SoftSmile_Prototype`, `EXP_Curious_Prototype`, `EXP_Apologetic_Prototype`)
- [x] Tạo `LipSyncMapDefinition` mapping viseme → blendshape (đã tạo `Assets/AvatarSystem/AvatarProduction/Data/LipSyncMaps/LipSyncMap_Prototype_Default.asset` và bind vào base prefab)
- [x] Test lip-sync prototype bằng amplitude (audio TTS → mở miệng) (`unity-client/Logs/BaseAvatarFacialValidation.json`: `amplitudeLipSyncPassed = true`, `amplitudePeakMouthOpen = 60`, `amplitudePeakVisemeAA = 100`, `lipSyncSettledAfterPlayback = true`)
- [x] Đảm bảo không méo mặt khi chuyển giữa expression và lip-sync (`unity-client/Logs/BaseAvatarFacialValidation.json`: `expressionLipSyncLayeringPassed = true`, upper-face giữ `SmileEye_L ~= 32`, lower-face expression `Smile = 0` tại peak lip-sync để tránh chồng méo)

---

## Phase 4 — Làm locomotion và hội thoại sync
- [x] Tạo Animator Controller với parameters chuẩn (IsListening, IsThinking, IsSpeaking, IsMoving, v.v.)
- [x] Tạo/import animation tối thiểu: Idle_Default, Idle_Breathing, Listen_Idle, Talk_Idle
- [x] Tạo/import animation di chuyển: Walk_Forward, Turn_Left, Turn_Right, Approach_Short
- [x] Tạo/import gesture: Wave_Small, Nod_Yes, HandExplain_01
- [x] Thiết lập anchor points trong scene (IdlePoint, TalkPoint, ListenPoint, WanderA, WanderB)
- [x] Kết nối `AvatarLocomotionController` với anchor points
- [x] Kết nối `AvatarConversationBridge` với hệ STT/LLM/TTS hiện có trong `AssistantApp`
- [x] Kết nối `AvatarLookAtController` với camera hoặc điểm nhìn người dùng
- [x] Test luồng hội thoại: Idle → Listening → Thinking → Speaking → Reacting → Idle (PlayMode pass: `AvatarConversationBridgePlayModeTests.AvatarConversationBridgeTransitionsThroughConversationFlow`, xem `unity-client/Logs/PlayModeTests.xml`, 2026-03-19)
- [x] Test avatar phản ứng đúng theo voice loop (PlayMode pass: `AssistantAppPlayModeTests.AssistantAppStreamVoiceLoopDrivesAvatarStates`, xem `unity-client/Logs/PlayModeTests.xml`, 2026-03-19)

---

## Phase 5 — Làm modular outfit system
- [x] Tạo item đầu tiên: Hair_01 (tạo mesh Blender → export FBX → import Unity → tạo ScriptableObject)
- [ ] Tạo Top_01 (áo cơ bản)
- [ ] Tạo Bottom_01 (quần/chân váy cơ bản)
- [ ] Tạo Dress_01 (váy liền thân)
- [ ] Tạo Shoes_01 (giày cơ bản)
- [x] Test equip/unequip từng slot qua `AvatarEquipmentManager` (EditMode pass trong `LocalAssistant.Tests.EditMode.AvatarOutfitSystemTests`, 2026-03-28)
- [x] Test conflict: equip Dress → tự động unequip Top và Bottom (EditMode pass trong `LocalAssistant.Tests.EditMode.AvatarOutfitSystemTests`, 2026-03-28)
- [x] Test hide body regions: áo dài tay che cánh tay, quần dài che đùi (EditMode pass trong `LocalAssistant.Tests.EditMode.AvatarOutfitSystemTests`, 2026-03-28)
- [ ] Tạo thêm: Hair_02, Hair_03, HairAccessory_01, HairAccessory_02
- [ ] Tạo thêm: Socks_01, Socks_02, Gloves_01, Gloves_02
- [ ] Tạo thêm: BraceletL_01, BraceletR_01
- [ ] Test conflict: Gloves full → chặn BraceletL/R
- [ ] Tạo 3 outfit preset (`OutfitPresetDefinition`) và test bấm 1 nút đổi nguyên set
- [x] Test save/load outfit qua `AvatarPresetManager` (EditMode pass trong `LocalAssistant.Tests.EditMode.AvatarOutfitSystemTests`, 2026-03-28)

---

## Phase 6 — Validation và production hardening
- [ ] Test 20–50 lần đổi đồ liên tiếp, kiểm tra memory leak
- [ ] Test nói chuyện khi đang đổi đồ
- [ ] Test animation khi mặc các loại đồ khác nhau (kiểm tra clipping)
- [ ] Fix clipping, material lỗi, missing reference
- [x] Chạy `Tools > AvatarSystem > Validate All Item Definitions` → 0 errors (Unity menu run 2026-03-28: scan 1 item definition, `0 errors`, `0 warnings`; only optional thumbnail note on `ITM_Hair_01`)
- [ ] Chạy `Tools > AvatarSystem > Validate Outfit Presets` → 0 issues (Unity menu run 2026-03-28 returned `0 issues` but scanned `0 outfit presets`, so keep open until preset assets exist)
- [x] Tạo 4 scene test riêng:
  - [x] `AvatarSandbox.unity` — test prefab cơ bản (scene scaffold tạo ngày 2026-03-28 tại `Assets/AvatarSystem/AvatarProduction/Scenes/AvatarSandbox.unity`)
  - [x] `OutfitTest.unity` — test equip/unequip, preset, conflict (scene scaffold tạo ngày 2026-03-28 tại `Assets/AvatarSystem/AvatarProduction/Scenes/OutfitTest.unity`)
  - [x] `FacialAndLipSyncTest.unity` — test blink, expression, viseme (scene scaffold tạo ngày 2026-03-28 tại `Assets/AvatarSystem/AvatarProduction/Scenes/FacialAndLipSyncTest.unity`)
  - [x] `ConversationTest.unity` — test luồng hội thoại đầy đủ (scene scaffold tạo ngày 2026-03-28 tại `Assets/AvatarSystem/AvatarProduction/Scenes/ConversationTest.unity`)
- [ ] Viết README hướng dẫn thêm món đồ mới (quy trình từ Blender → Unity → ScriptableObject)
- [ ] Kiểm tra thay thế placeholder avatar bằng avatar thật mà không sửa core app nhiều

---

## Mốc Prototype Đạt Yêu Cầu

Prototype coi là đạt khi có đủ:
- [ ] Avatar import vào Unity ổn định
- [ ] Có idle, listen, speak, walk nhẹ
- [x] Có blink và 5 expression cơ bản (technical pass qua `unity-client/Logs/BaseAvatarFacialValidation.json`; đã verify 7 expression prototype + blink)
- [x] Có mouth movement khi nói (prototype amplitude lip-sync pass qua `unity-client/Logs/BaseAvatarFacialValidation.json`)
- [ ] Đổi được tóc (2 kiểu), áo (2 cái), quần (2 cái), váy (1 cái), giày (2 đôi)
- [ ] Dress tự chặn Top và Bottom
- [ ] Gloves và bracelet xử lý xung đột đúng
- [ ] Có ít nhất 3 preset outfit
- [ ] Có thể dùng trong scene trò chuyện thật
