# Phase 1 — Boundary Design

**Status:** DONE (Phase 1 output)  
**Ngày:** 2026-04-07  
**Dependency:** Phase 0 — `docs/migration/rebuild-target.md`, `docs/migration/rebuild-rules.md`  
**Chi tiết:** `docs/architecture/ownership-matrix.md`

---

## Mục tiêu

Tách trách nhiệm rõ ràng giữa 4 layer trước khi viết lại bất kỳ code nào.  
Mọi quyết định ở Phase 1 là nền tảng cho các phase tiếp theo.

---

## Kết quả Phase 1

### 1. Ownership chốt

Xem chi tiết đầy đủ: `docs/architecture/ownership-matrix.md`

| Layer | Owner | Trách nhiệm cụ thể |
|-------|-------|-------------------|
| **Tauri** | Process host | Start/stop backend, start/stop Unity, single window, bridge |
| **React** | Business UI | Chat, Planner, Settings, Wardrobe, Dashboard, System Status |
| **Unity** | 3D runtime | Avatar, room, animation, lip sync, audio playback, mic capture |
| **Backend** | Business logic | Task CRUD, chat orchestration, STT/TTS, settings, memory, scheduler |

### 2. Unity feature inventory

**Tổng số feature Unity đã inventory:** 55 script files + 14 UXML/USS files

| Tag | Số feature |
|-----|-----------|
| retire | ~32 features |
| keep | ~15 features |
| partial-keep | ~4 features |

**Retire:** Shell layout, Chat UI, Planner UI, Settings UI, Home UI, Reminder UI, Subtitle UI (2D), all `*Refs.cs` controllers, Network clients (LocalApiClient, EventsClient, AssistantStreamClient), AssistantApp (main coordinator), đa số `Core/` scripts UI-related.

**Keep:** Toàn bộ `AvatarSystem/Core/Scripts/` (11 controllers), `Scripts/Avatar/` (2 scripts), `Audio/AudioPlaybackController.cs`, `Audio/WavEncoder.cs`, `Core/AssistantEventBus.cs`.

**Partial-keep under review:** Subtitle overlay (3D caption layer), health utilities, internal event bus.

### 3. Backend API tái sử dụng

**17 endpoints/WebSockets hiện có đều tái sử dụng được ngay** bởi React mà không cần sửa backend:
- REST: health, tasks (CRUD), chat, speech (STT/TTS/cache), settings
- WebSocket: `/v1/events`, `/v1/assistant/stream`

Xem bảng đầy đủ: `docs/architecture/ownership-matrix.md` §4

### 4. Events bridge Unity ↔ React

**Commands React → Unity (Phase 5-6):**
```
avatar.setMood, avatar.playEmote, avatar.speakStart, avatar.speakStop
avatar.setIdleState, avatar.setListeningState
wardrobe.equipItem
room.setCameraFocus, room.focusObject
app.pageChanged
```

`avatar.equipItem` chi con duoc xem la legacy alias tam thoi trong Unity bridge scaffold de tranh drift trong luc docs va UI dong bo ve `wardrobe.equipItem`.

**Events Unity → React (Phase 5-6):**
```
avatar.stateChanged, avatar.animationFinished
room.objectClicked, room.interactionTriggered
bridge.ready, bridge.error
```

### 5. Sơ đồ communication

```
React ↔ Backend:  HTTP REST + WebSocket (trực tiếp, không qua Tauri)
React ↔ Tauri:    Tauri commands (invoke) + events (emit)
Tauri ↔ Unity:    Local WebSocket (localhost port nội bộ)
Unity ↔ Backend:  KHÔNG CÒN (Unity không gọi backend nữa sau Phase 7)
```

---

## Acceptance

- [x] Có tài liệu ownership rõ ràng — `docs/architecture/ownership-matrix.md`
- [x] Không còn tranh chấp "cái này để bên nào làm" — ownership matrix đã chốt
- [x] Feature Unity đã được tag đầy đủ keep/retire/partial-keep
- [x] Danh sách API backend tái sử dụng đã xác nhận
- [x] Event list bridge đã định nghĩa
- [x] Sơ đồ boundary đã vẽ

---

## Output files Phase 1

- `docs/architecture/ownership-matrix.md` — Full inventory, ownership matrix, API list, event list, diagram
- `docs/migration/phase1-boundary.md` — File này (summary + acceptance)

---

## Chuyển sang Phase 2

Phase 1 DONE. Tiếp theo là **Phase 2 — Dựng Tauri shell tối thiểu**.

Điều kiện để bắt đầu Phase 2:
- ✅ Ownership matrix chốt
- ✅ React là owner của business UI
- ✅ Tauri là process host
- ✅ Backend được giữ nguyên

Phase 2 sẽ:
1. Khởi tạo `apps/desktop-shell/` (Tauri project)
2. Tích hợp React/Vite vào Tauri webview
3. Tạo process manager cho `local-backend/`
4. Tạo main window duy nhất
5. Health check startup

---

*Xem rebuild master plan: `tasks/rebuild-master-plan.md`*
