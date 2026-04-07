# Rebuild Master Plan

**Status:** Active planning document  
**Ngày tạo:** 2026-04-07  
**Nguồn gốc:** `tai_cau_truc.md` — Kế hoạch tái thiết hoàn chỉnh  

---

## Tổng quan

Dự án chuyển từ kiến trúc hiện tại (Unity shell UI + Unity app) sang kiến trúc mới:

```
Tauri Desktop Host
├── React/Vite Web UI       ← business UI
├── Unity Embedded Runtime  ← 3D only (room + avatar)
└── Local Backend (FastAPI) ← business logic (được giữ)
```

**Tài liệu nền:**
- `docs/migration/rebuild-target.md` — kiến trúc đích và hiện trạng freeze
- `docs/migration/rebuild-rules.md` — 8 luật cứng bất biến

---

## Trạng thái phases

| Phase | Tên | Trạng thái | Acceptance |
|-------|-----|-----------|-----------|
| **Phase 0** | Freeze hiện trạng + khóa target-state | ✅ **DONE** | docs tạo xong, 8 luật locked |
| **Phase 1** | Thiết kế boundary mới | ✅ **DONE** | ownership matrix chốt, 55 scripts tagged, 17 API xác nhận, event bridge defined |
| **Phase 2** | Dựng Tauri shell tối thiểu | 🟡 **DOING** | Tauri host đã scaffold; còn thiếu runtime validation vì máy hiện tại chưa có Node.js + Rust |
| **Phase 3** | Dựng Web UI skeleton | 🟡 **DOING** | `apps/web-ui/` đã tách ra, có navigation 6 pages và backend client; còn thiếu build/run evidence |
| **Phase 4** | Nhúng Unity vào Tauri | 🟡 **DOING** | Đã có sidecar lifecycle scaffold và status bridge; còn thiếu Unity build artifact + native attach |
| **Phase 5** | Tái cấu trúc Unity thành 3D runtime | 🟡 **DOING** | Đã có runtime facade scaffold; còn thiếu bridge thật và retire shell UI cũ |
| **Phase 6** | Chuẩn hóa communication contracts | 🟡 **DOING** | `packages/contracts/` + typed bridge scaffold landed; còn thiếu runtime handshake evidence |
| **Phase 7** | Chuyển UI nghiệp vụ từ Unity sang React | ⬜ TODO | user thao tác toàn bộ qua React |
| **Phase 8** | Gắn page context với Unity behavior | ⬜ TODO | page React ↔ Unity state đồng bộ |
| **Phase 9** | Packaging một app hoàn chỉnh | ⬜ TODO | 1 exe chạy toàn bộ |
| **Phase 10** | Cleanup triệt để | ⬜ TODO | repo sạch, không 2 kiến trúc song song |
| **Phase 11** | Production hardening | ⬜ TODO | crash recovery, health panel, smoke test |

---

## Phase 0 — DONE ✅

**Mục tiêu:** Freeze hiện trạng, khóa target-state, ngăn repo phát triển sai hướng.

**Việc đã làm:**
- [x] Chụp lại trạng thái hiện tại của repo (xem `rebuild-target.md` §2)
- [x] Ghi rõ kiến trúc đích mới (xem `rebuild-target.md` §1, §3, §4, §7)
- [x] Ghi rõ Unity shell UI cũ sẽ bị retire (xem `rebuild-target.md` §6)
- [x] Đánh dấu backend được giữ lại (xem `rebuild-target.md` §2.3)
- [x] Xác định phần Unity phải giữ: avatar, room, animation, camera, interaction (xem `rebuild-target.md` §5)
- [x] Khóa 8 luật cứng bất biến (xem `rebuild-rules.md`)

**Output files:**
- `docs/migration/rebuild-target.md` ✅
- `docs/migration/rebuild-rules.md` ✅
- `tasks/rebuild-master-plan.md` ✅ (file này)

**Acceptance:**
- ✅ Unity không còn là shell UI chính — điều này được ghi nhận và locked
- ✅ Mọi agent/developer đọc repo phải đọc `rebuild-rules.md` trước khi làm việc với UI

---

## Phase 1 — DONE

**Mục tiêu:** Tách trách nhiệm rõ ràng trước khi viết lại UI.

**Việc cần làm:**
- [x] Liệt kê đầy đủ tất cả feature hiện có trong Unity
- [x] Gắn tag cho từng feature: `keep in Unity` / `move to React` / `retire`
- [x] Liệt kê API backend nào đã có thể tái sử dụng ngay (từ `local-backend/app/api/routes.py`)
- [x] Liệt kê event nào Unity cần nhận và gửi
- [x] Tạo ownership matrix chính thức dạng doc
- [x] Vẽ sơ đồ boundary Tauri ↔ React ↔ Unity ↔ Backend

**Output cần có:**
- `docs/architecture/ownership-matrix.md`
- `docs/migration/phase1-boundary.md`

**Acceptance:**
- Có tài liệu ownership rõ ràng
- Không còn tranh chấp "cái này để bên nào làm"

---

## Phase 2 — DOING

**Mục tiêu:** Có app Tauri desktop host chạy được.

**Việc cần làm:**
- [ ] Tạo `apps/desktop-shell/` với Tauri + Rust
- [ ] Tích hợp React/Vite vào Tauri web view
- [ ] Tạo main window duy nhất
- [ ] Tạo process manager cho local-backend
- [ ] Tạo logging cơ bản
- [ ] Tạo health-check startup

**Chưa cần:** Unity embed ở phase này.

**Output cần có:**
- `apps/desktop-shell/` có thể build và chạy
- Tauri start backend tự động

**Current implementation:**
- `apps/desktop-shell/src-tauri/` đã có Rust host, backend process manager, health-check loop, và startup events.
- Web frontend không còn là source of truth trong `apps/desktop-shell/`; host hiện trỏ sang `apps/web-ui/`.
- Manual validation required: chưa build hoặc run được trong turn này vì `node` và `cargo` chưa có trong PATH.

**Acceptance:**
- Mở Tauri app → thấy shell lên + web UI chạy + backend started

---

## Phase 3 — DOING

**Mục tiêu:** Xây bộ khung giao diện React mới.

**Pages cần có ngay:**
- Dashboard
- Chat
- Planner
- Settings
- Wardrobe
- System Status

**Việc cần làm:**
- [x] Tạo `apps/web-ui/` với React + Vite
- [x] Dựng layout page-based
- [x] Tạo navigation
- [x] Tạo state/service layer tối thiểu cho runtime mode + backend access
- [x] Tạo API client typed kết nối backend
- [x] Làm mock panel cho vùng Unity ở center

**Cấu trúc frontend khuyến nghị:**
```
src/
├── app/
├── pages/
├── widgets/
├── features/
├── entities/
├── shared/
├── services/
└── contracts/
```

**Acceptance:**
- React app tự chạy độc lập
- Đã có shell điều hướng thật
- Backend gọi được từ React
- Manual validation required: chưa có build/run evidence vì thiếu Node.js

---

## Phase 4 — DOING

**Mục tiêu:** Unity embedded như vùng render trong app.

**Việc cần làm:**
- [ ] Build Unity thành runtime executable (window-less)
- [x] Tauri spawn Unity sidecar scaffold
- [ ] Tìm Unity window handle (Windows API)
- [ ] Attach vào native container trong Tauri
- [ ] Đồng bộ resize/focus/minimize/restore

**Lưu ý:** Đây là phase kỹ thuật khó nhất của desktop.

**Current implementation:**
- Tauri host hiện đã có `unity_runtime.rs` để resolve executable path, inspect state, launch sidecar, stop sidecar, và emit `unity-runtime-status`.
- React center panel hiện hiển thị trạng thái sidecar thay vì placeholder tĩnh.
- Manual validation required: repo hiện chưa có Unity build executable và máy hiện tại chưa có toolchain để run host.

**Acceptance:**
- Unity xuất hiện trong vùng trung tâm
- Resize theo app window
- Không nổi thành cửa sổ riêng

---

## Phase 5 — DOING

**Mục tiêu:** Unity trở thành 3D runtime tinh gọn, không cần shell UI cũ.

**Việc cần làm:**
- [ ] Xóa dần: Chat UI, Planner UI, Settings UI, shell layout UI Toolkit
- [ ] Giữ: scene room, avatar, camera, interaction, emotion, lip sync, animation
- [x] Tạo `UnityBridgeClient` — command receiver + event emitter scaffold
- [x] Tạo các module chuẩn: `AvatarRuntime`, `RoomRuntime`, `InteractionRuntime`, `AnimationRuntime`, `LipSyncRuntime`, `SceneStateController`

**Current implementation:**
- Unity tree hiện đã có facade runtime mới trong `Assets/Scripts/Runtime/`.
- `AppCompositionRoot` đã bind facade mới từ `AvatarStateMachine`, `LipSyncController`, `AvatarConversationBridge`, `AvatarRootController`, và scene camera hiện có.
- Manual validation required: chưa có Unity compile/run pass cho scaffold mới trong turn này.

**Acceptance:**
- Unity build vẫn chạy
- Không cần UI shell cũ
- Unity sống được như 3D engine con

---

## Phase 6 — DOING

**Mục tiêu:** Giao tiếp sạch giữa tất cả layers.

**Việc cần làm:**
- [ ] Định nghĩa tất cả contract groups trong `packages/contracts/`
- [ ] Implement bridge typed: React ↔ Tauri (Tauri commands/events)
- [ ] Implement bridge typed: Tauri ↔ Unity (local WebSocket)
- [ ] Enforce: không gọi Unity ad-hoc từ React

**Ví dụ command gửi Unity:**
```
avatar.setMood, avatar.playEmote, avatar.speakStart, avatar.speakStop
room.setCameraFocus, room.focusObject
wardrobe.equipItem
```

**Ví dụ event từ Unity:**
```
bridge.ready, bridge.error, avatar.stateChanged, room.interactionTriggered
```

**Acceptance:**
- Mọi giao tiếp đều có schema typed
- Không có gọi tắt ad-hoc
- Current implementation: `packages/contracts/` đã chứa protocol names + payload shapes; web UI đã dùng typed Tauri command/event names; Tauri host đã có websocket bridge scaffold và Unity runtime đã có `TauriBridgeRuntime` client scaffold.
- Manual validation required: chưa có Node.js + Rust toolchain trong PATH, repo chưa có Unity standalone build artifact, và chưa có websocket handshake evidence trên máy đích.

---

## Phase 7 — TODO

**Mục tiêu:** Hoàn tất migration giao diện nghiệp vụ.

**Thứ tự chuyển:**
1. Settings (dễ nhất)
2. Chat
3. Planner
4. Reminder
5. Status panels
6. Wardrobe panel (cuối, cần Unity preview)

**Acceptance:**
- User thao tác toàn bộ nghiệp vụ qua React
- Unity chỉ còn hình ảnh và 3D interaction

---

## Phase 8 — TODO

**Mục tiêu:** App cảm giác thống nhất, React page đồng bộ với Unity state.

**Các rule page-context:**
- Page Chat → camera focus avatar
- Page Planner → camera nhìn bàn làm việc
- Page Wardrobe → avatar đứng giữa, đổi đồ realtime
- Page Dashboard → camera room tổng quát

**Acceptance:**
- React page và Unity state ăn khớp nhau

---

## Phase 9 — TODO

**Mục tiêu:** Phát hành như một ứng dụng desktop duy nhất.

**Startup flow:**
1. Mở Tauri app
2. Tauri start backend
3. Tauri check health backend
4. Tauri start Unity
5. Tauri attach Unity window
6. React load page mặc định
7. App ready

**Acceptance:**
- User bấm 1 exe → app chạy hoàn chỉnh
- Không cần tự run backend hay Unity tay

---

## Phase 10 — TODO

**Mục tiêu:** Dọn code cũ, repo sạch.

**Việc cần làm:**
- [ ] Xóa Unity shell UI cũ (sau khi React đã thay thế hoàn toàn)
- [ ] Xóa docs cũ mô tả kiến trúc sai
- [ ] Cập nhật scripts packaging
- [ ] Cập nhật AGENTS.md và runbook
- [ ] Khóa rule không cho tái đưa business UI vào Unity

**Acceptance:**
- Repo không còn 2 kiến trúc song song

---

## Phase 11 — TODO

**Mục tiêu:** Production hardening.

**Việc cần làm:**
- [ ] Startup diagnostics
- [ ] Crash recovery cho backend và Unity
- [ ] Health panel
- [ ] Retry policy
- [ ] Structured logs
- [ ] Smoke test tự động
- [ ] Packaging validation
- [ ] Offline behavior rõ ràng

**Acceptance:**
- App chịu lỗi tốt, dễ debug, dễ maintain

---

## Thứ tự ưu tiên

1. Phase 0 ✅ — Freeze docs
2. Phase 2 — Tauri shell
3. Phase 3 — React/Vite shell + kết nối backend
4. Phase 4 — Nhúng Unity vào Tauri
5. Phase 5 — Refactor Unity thành runtime 3D
6. Phase 6 — Contracts
7. Phase 7 — Chuyển UI nghiệp vụ
8. Phase 8 — Page-context sync
9. Phase 9 — Packaging
10. Phase 10 — Cleanup
11. Phase 11 — Hardening

*(Phase 1 song song với Phase 2 hoặc trước Phase 2)*

---

## Blockers và dependencies

| Phase | Dependency |
|-------|-----------|
| Phase 2 | Cần quyết định Rust version + Tauri version |
| Phase 3 | Cần quyết định React state management lib |
| Phase 4 | Cần Unity build artifacts, Windows native API |
| Phase 5 | Phase 4 phải xong trước |
| Phase 6 | Phase 3 + Phase 5 phải xong trước |
| Phase 7 | Phase 6 phải xong trước |
| Phase 10 | Phase 7 phải xong trước |

---

*Tài liệu này được cập nhật sau mỗi phase hoàn thành.*
