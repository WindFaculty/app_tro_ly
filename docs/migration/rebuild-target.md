# Rebuild Target — Kiến trúc đích mới

**Status:** Design target — chưa implemented  
**Phase:** Phase 0 Freeze  
**Ngày tạo:** 2026-04-07  
**Tác giả:** Antigravity AI (theo kế hoạch tái thiết `tai_cau_truc.md`)

---

## 1. Tuyên bố kiến trúc đích

Từ thời điểm này, kiến trúc đích của dự án là:

```
Tauri Desktop Host
├── React/Vite Web UI       ← giao diện nghiệp vụ chính
├── Unity Embedded Runtime  ← chỉ render 3D (phòng, avatar)
└── Local Backend (FastAPI) ← lõi nghiệp vụ, được giữ lại
```

**Unity không còn là shell UI chính. Đây là quy tắc không thể thay đổi từ thời điểm này.**

---

## 2. Hiện trạng repo tại thời điểm freeze (2026-04-07)

### 2.1 Thư mục cấp gốc

```
app_tro_ly/
├── local-backend/          ← FastAPI backend — ĐƯỢC GIỮ LẠI
├── unity-client/           ← Unity project — CẦN TÁI CẤU TRÚC
├── docs/                   ← tài liệu — cần cập nhật liên tục
├── tasks/                  ← task tracking
├── scripts/                ← Windows setup, startup, packaging
├── tools/                  ← tool scripts
├── ai-dev-system/          ← optional subsystem, không bắt buộc
├── release/                ← release artifacts
└── tai_cau_truc.md         ← kế hoạch tái thiết (nguồn sự thật phase này)
```

### 2.2 Unity client — hiện trạng

**Vẫn là shell UI chính hiện tại.** Chứa toàn bộ UI nghiệp vụ chạy bằng UI Toolkit.

Cấu trúc hiện có:
```
unity-client/Assets/
├── Scripts/
│   ├── App/               ← AppShellController, AppCompositionRoot, ShellModule
│   ├── Core/              ← AssistantApp (48KB), UiDocumentLoader, ApiModels, ChatPanelRefs...
│   ├── Features/
│   │   ├── Chat/          ← chat UI feature
│   │   ├── Home/          ← home screen
│   │   ├── Schedule/      ← planner/schedule feature
│   │   └── Settings/      ← settings feature
│   ├── Avatar/            ← AvatarStateMachine, LipSyncController
│   ├── Audio/
│   ├── Network/
│   ├── Notifications/
│   └── Tasks/
├── AvatarSystem/          ← avatar production system, prototype assets
├── Resources/
│   └── UI/                ← MainUI.uxml, Screens/, Panels/, Shell/, Overlays/, Styles/
└── Scenes/
```

**Feature UI hiện có trong Unity:**
| Feature | Script/UI | Ghi chú |
|---------|-----------|---------|
| Shell layout | `AppShellController.cs`, `AppShell.uxml` | 4-zone layout |
| Chat | `Features/Chat/`, `ChatPanelRefs.cs` | panel phải |
| Planner/Schedule | `Features/Schedule/` | center screen |
| Settings | `Features/Settings/`, `SettingsScreenRefs.cs` | drawer |
| Home | `Features/Home/`, `HomeScreenRefs.cs` | home screen |
| Reminder overlay | `ReminderOverlayRefs.cs` | overlay |
| Subtitle overlay | `SubtitleOverlayRefs.cs` | overlay |
| Avatar state | `AvatarStateMachine.cs` | 3D runtime |
| Lip sync | `LipSyncController.cs` | 3D runtime |
| Avatar production | `AvatarSystem/AvatarProduction/` | 3D runtime |

### 2.3 Backend — hiện trạng

**Được giữ lại hoàn toàn.** Backend là nguồn sự thật cho nghiệp vụ.

```
local-backend/app/
├── api/routes.py           ← tất cả API endpoints
├── services/               ← 18 service modules
│   ├── assistant_orchestrator.py  ← chat orchestration
│   ├── tasks.py            ← task CRUD
│   ├── planner.py          ← planner logic
│   ├── scheduler.py        ← scheduler
│   ├── memory.py           ← memory persistence
│   ├── settings.py         ← settings
│   ├── tts.py              ← TTS
│   ├── stt.py              ← STT
│   └── ...
├── models/                 ← data models
├── core/                   ← core utilities
└── db/                     ← SQLite persistence
```

**API endpoints đã có (được giữ lại cho React):**
- `GET /v1/health`
- `GET/POST /v1/tasks`, `/v1/tasks/today`, `/v1/tasks/week`, `/v1/tasks/overdue`, `/v1/tasks/inbox`, `/v1/tasks/completed`
- `PUT /v1/tasks/{id}`, `POST /v1/tasks/{id}/complete`, `POST /v1/tasks/{id}/reschedule`
- `POST /v1/chat`
- `POST /v1/speech/stt`, `POST /v1/speech/tts`, `GET /v1/speech/cache/{filename}`
- `GET/PUT /v1/settings`
- `WebSocket /v1/events`
- `WebSocket /v1/assistant/stream`

---

## 3. Kiến trúc đích — thư mục mục tiêu

```
app_tro_ly/
├── apps/
│   ├── desktop-shell/      [NEW] Tauri app
│   ├── web-ui/             [NEW] React + Vite
│   └── unity-runtime/      [RENAME from unity-client/]
├── services/
│   └── local-backend/      [RENAME from local-backend/]
├── packages/
│   ├── contracts/          [NEW] shared DTO / event schema
│   └── ui-types/           [NEW] shared TypeScript types
├── docs/
│   ├── architecture/
│   ├── migration/
│   ├── runtime/
│   └── ui/
├── tasks/
├── scripts/
└── tools/
```

> **Lưu ý:** Việc rename/move thư mục sẽ xảy ra ở phase sau, **không phải Phase 0**.  
> Phase 0 chỉ ghi lại target, không move gì.

---

## 4. Ownership matrix (target state)

| Layer | Owner | Trách nhiệm |
|-------|-------|-------------|
| Desktop host | Tauri | lifecycle, process management, single window |
| Business UI | React/Vite | chat, planner, settings, wardrobe, status |
| 3D runtime | Unity | room, avatar, animation, camera, lip sync |
| Business logic | FastAPI | task/chat/settings/memory/voice/scheduler |

---

## 5. Phần Unity sẽ giữ lại

Các module Unity sau đây **phải được giữ và tiếp tục phát triển**:

| Module | Path hiện tại | Target module name |
|--------|--------------|-------------------|
| Avatar state machine | `Scripts/Avatar/AvatarStateMachine.cs` | `AvatarRuntime` |
| Lip sync | `Scripts/Avatar/LipSyncController.cs` | `LipSyncRuntime` |
| Avatar production | `AvatarSystem/AvatarProduction/` | `AvatarRuntime` |
| Room scene | `Scenes/` | `RoomRuntime` |
| Camera | (embedded trong scene) | `RoomRuntime` |
| Animation system | (embedded trong avatar system) | `AnimationRuntime` |
| 3D interaction | (embedded trong scene) | `InteractionRuntime` |

**Các module mới cần tạo trong Unity (phase sau):**
- `UnityBridgeClient` — nhận command, emit event
- `SceneStateController` — quản lý trạng thái scene theo tín hiệu từ React

---

## 6. Phần Unity sẽ bị retire

Các phần sau đây **sẽ bị xóa dần**, sau khi React UI thay thế hoàn toàn:

| Phần | Path | Lý do retire |
|------|------|-------------|
| Shell layout UI | `Scripts/App/AppShellController.cs` | React thay thế |
| App composition root | `Scripts/App/AppCompositionRoot.cs` | Tauri thay thế |
| Shell module | `Scripts/App/ShellModule.cs` | React thay thế |
| Chat UI feature | `Scripts/Features/Chat/` | React thay thế |
| Schedule/Planner UI | `Scripts/Features/Schedule/` | React thay thế |
| Settings UI | `Scripts/Features/Settings/` | React thay thế |
| Home screen UI | `Scripts/Features/Home/` | React thay thế |
| Chat panel refs | `Scripts/Core/ChatPanelRefs.cs` | React thay thế |
| Home screen refs | `Scripts/Core/HomeScreenRefs.cs` | React thay thế |
| Settings screen refs | `Scripts/Core/SettingsScreenRefs.cs` | React thay thế |
| Reminder overlay | `Scripts/Core/ReminderOverlayRefs.cs` | React thay thế |
| Subtitle overlay | `Scripts/Core/SubtitleOverlayRefs.cs` | React thay thế |
| UI Toolkit UXML/USS | `Resources/UI/Screens/`, `Panels/`, `Shell/` | React thay thế |
| UiDocumentLoader | `Scripts/Core/UiDocumentLoader.cs` | không còn cần |
| UiElementNames | `Scripts/Core/UiElementNames.cs` | React thay thế |
| UiButtonActionBinder | `Scripts/Core/UiButtonActionBinder.cs` | React thay thế |

> **Không retire ngay.** Retire theo phase 5 và 7 khi React UI đã sẵn sàng thay thế.

---

## 7. Bố cục cửa sổ đích

App chỉ có **một cửa sổ duy nhất**:

```
┌──────────────────────────────────────────────────────────────┐
│ Topbar / Navigation / Quick Actions                          │
├───────────────┬───────────────────────────────┬──────────────┤
│ Left Panel    │ Center Panel                  │ Right Panel  │
│ React Nav     │ Unity Render Region           │ React Context│
│ Pages/Tabs    │ Room + Avatar                 │ Detail Panel │
├───────────────┴───────────────────────────────┴──────────────┤
│ Bottom utility / status / voice / notifications              │
└──────────────────────────────────────────────────────────────┘
```

Unity region ở trung tâm **luôn tồn tại**. Chỉ các panel xung quanh thay đổi theo page.

---

## 8. Giao thức giao tiếp đích

| Kết nối | Phương thức |
|---------|-------------|
| React ↔ Backend | HTTP REST + WebSocket |
| React ↔ Tauri | Tauri commands/events |
| Tauri ↔ Unity | Local WebSocket (port nội bộ) |

---

*File này là Current state snapshot + design target. Xem `rebuild-rules.md` cho luật migration.*
