# Ownership Matrix — Feature Boundary

**Status:** Design target (Phase 1 output)  
**Phase:** Phase 1 — Thiết kế boundary mới  
**Ngày tạo:** 2026-04-07  
**Nguồn sự thật:**
- historical pre-cutover Unity shell tree inventory captured on 2026-04-07
- `local-backend/app/api/routes.py` — API endpoints thực tế
- `docs/migration/rebuild-target.md` — kiến trúc đích
- `docs/migration/rebuild-rules.md` — 8 luật cứng

Historical note: references to `AssistantApp`, shell controllers, and legacy UI rows below describe the retired pre-cutover Unity shell inventory, not the current `apps/unity-runtime/` implementation.

---

## 1. Layer Ownership — Quy tắc chủ sở hữu

| Layer | Owner | Không được can thiệp vào |
|-------|-------|----------|
| Process host, lifecycle | **Tauri** | React, Unity, Backend |
| Business UI, navigation | **React** | Unity, Tauri |
| 3D scene, avatar, animation | **Unity** | React, Tauri |
| Business logic, persistence | **FastAPI Backend** | React, Unity |
| Communication contracts | **`packages/contracts/`** | Tất cả đều phải tuân theo |

---

## 2. Feature Inventory — Unity (hiện tại)

Toàn bộ feature được inventory từ code thực tế ngày 2026-04-07.

### 2.1 Shell & Navigation

| Feature | Script | UI File | Tag | Owner đích |
|---------|--------|---------|-----|-----------|
| App shell layout (4-zone) | `App/AppShellController.cs` | `Shell/AppShell.uxml` | **retire** | React |
| App routing / page switching | `App/AppRouter.cs` | — | **retire** | React |
| App composition root | `App/AppCompositionRoot.cs` | — | **retire** | Tauri + React |
| Bootstrap entry point | `App/AssistantBootstrap.cs` | — | **retire** | Tauri |
| Shell module (IShellModule) | `App/ShellModule.cs`, `ShellModuleContracts.cs` | — | **retire** | React |
| Main UI entry | `Resources/UI/MainUI.uxml` | `MainUI.uxml` | **retire** | React |
| Shell layout tokens | `Resources/UI/Styles/Layout.uss`, `Tokens.uss` | — | **retire** | React (CSS) |

### 2.2 Chat UI

| Feature | Script | UI File | Tag | Owner đích |
|---------|--------|---------|-----|-----------|
| Chat panel controller | `Features/Chat/ChatPanelController.cs` | `Panels/ChatPanel.uxml` | **retire** | React |
| Chat module (IChatModule) | `Features/Chat/ChatModule.cs`, `ChatModuleContracts.cs` | — | **retire** | React |
| Chat request factory | `Features/Chat/ChatRequestFactory.cs` | — | **retire** | React (hoặc shared util) |
| Chat panel refs | `Core/ChatPanelRefs.cs` | — | **retire** | React |
| Chat panel styles | `Resources/UI/Styles/ChatPanel.uss` | — | **retire** | React (CSS) |

### 2.3 Planner / Schedule UI

| Feature | Script | UI File | Tag | Owner đích |
|---------|--------|---------|-----|-----------|
| Schedule screen controller | `Features/Schedule/ScheduleScreenController.cs` | `Screens/ScheduleScreen.uxml` | **retire** | React |
| Planner module (IPlannerModule) | `Features/Schedule/PlannerModule.cs`, `PlannerModuleContracts.cs` | — | **retire** | React |
| Schedule screen refs | `Features/Schedule/ScheduleScreenRefs.cs` | — | **retire** | React |
| Task view model store | `Tasks/TaskViewModelStore.cs` | — | **retire** | React state |
| Planner backend integration | `Tasks/PlannerBackendIntegration.cs` | — | **retire** | React API client |

### 2.4 Home Screen UI

| Feature | Script | UI File | Tag | Owner đích |
|---------|--------|---------|-----|-----------|
| Home screen controller | `Features/Home/HomeScreenController.cs` | `Screens/HomeScreen.uxml` | **retire** | React |
| Home screen refs | `Core/HomeScreenRefs.cs` | — | **retire** | React |
| Home screen styles | `Resources/UI/Styles/HomeScreen.uss`, `Cards.uss`, `Buttons.uss`, `Forms.uss` | — | **retire** | React (CSS) |

### 2.5 Settings UI

| Feature | Script | UI File | Tag | Owner đích |
|---------|--------|---------|-----|-----------|
| Settings screen controller | `Features/Settings/SettingsScreenController.cs` | `Screens/SettingsScreen.uxml` | **retire** | React |
| Settings screen refs | `Core/SettingsScreenRefs.cs` | — | **retire** | React |
| Settings view model store | `Core/SettingsViewModelStore.cs` | — | **retire** | React state |

### 2.6 Overlays

| Feature | Script | UI File | Tag | Owner đích |
|---------|--------|---------|-----|-----------|
| Reminder overlay controller | `Notifications/ReminderOverlayController.cs` | `Overlays/ReminderOverlay.uxml` | **retire** | React |
| Reminder presenter | `Notifications/ReminderPresenter.cs` | — | **retire** | React |
| Subtitle overlay | `Core/SubtitleOverlayRefs.cs` | `Overlays/SubtitleOverlay.uxml` | **partial-keep** | Unity (subtitle over 3D), React (text chat) |
| Overlay styles | `Resources/UI/Styles/Overlays.uss` | — | **retire** | React (CSS) |

### 2.7 Audio & Voice (trong Unity)

| Feature | Script | Tag | Owner đích | Ghi chú |
|---------|--------|-----|-----------|---------|
| Audio playback controller | `Audio/AudioPlaybackController.cs` | **keep** | Unity | Phát TTS audio vào speaker |
| Push-to-talk button | `Audio/PushToTalkButton.cs` | **retire** | React | Button UI sang React |
| WAV encoder | `Audio/WavEncoder.cs` | **keep** | Unity | Encode mic data → WAV bytes |
| Microphone capture | (trong `AssistantApp.cs`) | **keep** | Unity | Unity Microphone API, không thể dùng Web API |

### 2.8 Network / API Client (Unity → Backend)

| Feature | Script | Tag | Owner đích | Ghi chú |
|---------|--------|-----|-----------|---------|
| REST API client | `Network/LocalApiClient.cs` | **retire** | React | React sẽ gọi backend trực tiếp |
| Events WebSocket client | `Network/EventsClient.cs` | **retire** | React | React subscribe events |
| Assistant stream client | `Network/AssistantStreamClient.cs` | **retire** | React | React stream chat |
| Client interfaces | `Network/ClientInterfaces.cs` | **retire** | Bridge/contracts |

### 2.9 Avatar & 3D Runtime

| Feature | Script | Tag | Owner đích | Ghi chú |
|---------|--------|-----|-----------|---------|
| Avatar state machine | `Scripts/Avatar/AvatarStateMachine.cs` | **keep** | Unity | Core avatar logic |
| Lip sync controller | `Scripts/Avatar/LipSyncController.cs` | **keep** | Unity | Lip sync |
| Avatar animator bridge | `AvatarSystem/Core/Scripts/AvatarAnimatorBridge.cs` | **keep** | Unity | Animation control |
| Avatar facial controller | `AvatarSystem/Core/Scripts/AvatarFacialController.cs` | **keep** | Unity | Facial expressions |
| Avatar locomotion | `AvatarSystem/Core/Scripts/AvatarLocomotionController.cs` | **keep** | Unity | Movement |
| Avatar look-at | `AvatarSystem/Core/Scripts/AvatarLookAtController.cs` | **keep** | Unity | Eye gaze |
| Avatar conversation bridge | `AvatarSystem/Core/Scripts/AvatarConversationBridge.cs` | **keep** | Unity | Listening/talking bridge |
| Avatar body visibility | `AvatarSystem/Core/Scripts/AvatarBodyVisibilityManager.cs` | **keep** | Unity | Body part management |
| Avatar equipment manager | `AvatarSystem/Core/Scripts/AvatarEquipmentManager.cs` | **keep** | Unity | Equip items/wardrobe |
| Avatar lip sync driver | `AvatarSystem/Core/Scripts/AvatarLipSyncDriver.cs` | **keep** | Unity | Driver for lip sync |
| Avatar preset manager | `AvatarSystem/Core/Scripts/AvatarPresetManager.cs` | **keep** | Unity | Outfit presets |
| Avatar root controller | `AvatarSystem/Core/Scripts/AvatarRootController.cs` | **keep** | Unity | Root avatar controller |
| Avatar data models | `AvatarSystem/Core/Scripts/Data/` | **keep** | Unity | ScriptableObjects |
| Avatar enums | `AvatarSystem/Core/Scripts/AvatarEnums.cs` | **keep** | Unity |

### 2.10 Main Coordinator

| Feature | Script | Tag | Owner đích | Ghi chú |
|---------|--------|-----|-----------|---------|
| AssistantApp (main MonoBehaviour) | `Core/AssistantApp.cs` (48KB, 1133 lines) | **retire** | Split: Tauri hoặc Unity bridge | Quá lớn, phải tách ra |
| UI document loader | `Core/UiDocumentLoader.cs` | **retire** | React | Không cần nếu không dùng UI Toolkit |
| UI element names | `Core/UiElementNames.cs` | **retire** | React | Không cần |
| UI button action binder | `Core/UiButtonActionBinder.cs` | **retire** | React | Không cần |
| App module events | `Core/AppModuleEvents.cs` | **partial-keep** | Unity bridge | Internal event bus có thể giữ lại |
| Assistant event bus | `Core/AssistantEventBus.cs` | **keep** | Unity | Internal event system |
| Module contracts | `Core/ModuleContracts.cs` | **partial-keep** | → packages/contracts | Cần typed contract thay thế |
| Shell stage snapshot | `Core/ShellStageSnapshot.cs` | **retire** | React state | Shell state sang React |
| Health status utilities | `Core/HealthStatusMapper.cs`, `HealthRecoveryAdvisor.cs`, `HealthResponseNormalizer.cs` | **partially-keep** | Split | Health check logic có thể giữ cho Unity health check |
| API models | `Core/ApiModels.cs` | **retire** | packages/contracts | Typed làm contract |
| App shell refs | `Core/AppShellRefs.cs` | **retire** | React | Shell refs không cần |

---

## 3. Feature Ownership Matrix — Kết quả

| Feature | Owner hiện tại | Tag | Owner đích | Phase chuyển |
|---------|---------------|-----|-----------|-------------|
| App shell layout | Unity | retire | React | 5→7 |
| App routing | Unity | retire | React | Phase 3 |
| Chat UI | Unity | retire | React | Phase 7 |
| Chat state management | Unity | retire | React (Zustand/Context) | Phase 7 |
| Planner UI | Unity | retire | React | Phase 7 |
| Planner state | Unity | retire | React | Phase 7 |
| Home screen | Unity | retire | React (Dashboard page) | Phase 7 |
| Settings UI | Unity | retire | React | Phase 7 |
| Settings state | Unity | retire | React | Phase 7 |
| Reminder overlay (UI) | Unity | retire | React (toast/modal) | Phase 7 |
| Subtitle overlay (UI) | Unity | partial → retire | React + Unity (3D caption) | Phase 7 |
| Voice controls (mic button) | Unity | retire | React | Phase 7 |
| Audio playback (TTS) | Unity | keep | Unity | Giữ nguyên |
| Mic capture / WAV encode | Unity | keep | Unity | Giữ nguyên |
| Backend API client | Unity | retire | React | Phase 3 |
| Event WebSocket | Unity | retire | React | Phase 3 |
| Stream WebSocket | Unity | retire | React | Phase 3 |
| Avatar state machine | Unity | keep | Unity | Giữ nguyên |
| Avatar animation | Unity | keep | Unity | Giữ nguyên |
| Facial expressions | Unity | keep | Unity | Giữ nguyên |
| Lip sync | Unity | keep | Unity | Giữ nguyên |
| Avatar locomotion | Unity | keep | Unity | Giữ nguyên |
| Avatar eye gaze | Unity | keep | Unity | Giữ nguyên |
| Avatar equipment/wardrobe | Unity | keep | Unity + React preview | Phase 7+ |
| Room scene | Unity | keep | Unity | Giữ nguyên |
| Camera | Unity | keep | Unity | Giữ nguyên |
| 3D interaction | Unity | keep | Unity | Giữ nguyên |
| Task CRUD | Backend | keep | Backend | Giữ nguyên |
| Chat orchestration | Backend | keep | Backend | Giữ nguyên |
| Settings persistence | Backend | keep | Backend | Giữ nguyên |
| Memory/history | Backend | keep | Backend | Giữ nguyên |
| Voice pipeline (STT/TTS) | Backend | keep | Backend | Giữ nguyên |
| Scheduler/reminder | Backend | keep | Backend | Giữ nguyên |

---

## 4. API Backend tái sử dụng cho React

Tất cả API hiện tại đều **tái sử dụng được ngay** từ React mà không cần sửa backend:

| Endpoint | Method | React sử dụng cho |
|---------|--------|--------------------|
| `/v1/health` | GET | System Status page, startup health check |
| `/v1/tasks` | POST | Tạo task từ Chat, Planner |
| `/v1/tasks/today` | GET | Planner page — Today view |
| `/v1/tasks/week` | GET | Planner page — Week view |
| `/v1/tasks/overdue` | GET | Planner page — Overdue tab |
| `/v1/tasks/inbox` | GET | Planner page — Inbox tab |
| `/v1/tasks/completed` | GET | Planner page — Completed tab |
| `/v1/tasks/{id}` | PUT | Edit task |
| `/v1/tasks/{id}/complete` | POST | Mark complete |
| `/v1/tasks/{id}/reschedule` | POST | Reschedule |
| `/v1/chat` | POST | Chat page (fallback mode) |
| `/v1/speech/stt` | POST | Voice input (từ React → backend) |
| `/v1/speech/tts` | POST | TTS request |
| `/v1/speech/cache/{file}` | GET | Fetch audio |
| `/v1/settings` | GET/PUT | Settings page |
| `ws://.../v1/events` | WebSocket | Real-time events (reminders, task updates) |
| `ws://.../v1/assistant/stream` | WebSocket | Chat streaming |

**Không cần thêm endpoint mới cho Phase 3.** React có thể kết nối ngay.

---

## 5. Events Unity cần nhận (từ React qua Tauri bridge)

Khi React là UI chính, Unity nhận các command này qua `UnityBridgeClient` (Phase 5-6):

| Event / Command | Nguồn | Mô tả |
|----------------|-------|-------|
| `avatar.setMood` | React | Thay đổi trạng thái cảm xúc |
| `avatar.playEmote` | React | Phát emote animation |
| `avatar.speakStart` | React | Bắt đầu TTS (kích hoạt lip sync) |
| `avatar.speakStop` | React | Kết thúc TTS |
| `avatar.setIdleState` | React | Đặt avatar về idle |
| `avatar.setListeningState` | React | Đặt avatar lắng nghe (mic active) |
| `wardrobe.equipItem` | React | Thay item wardrobe |
| `room.setCameraFocus` | React | Điều khiển camera |
| `room.focusObject` | React | Focus vào vật thể trong phòng |
| `app.pageChanged` | React | Thay đổi page → Unity điều chỉnh state |

---

## 6. Events Unity cần gửi (ra React qua Tauri bridge)

| Event | Đích | Mô tả |
|-------|------|-------|
| `avatar.stateChanged` | React | Avatar state đã thay đổi |
| `avatar.animationFinished` | React | Animation hoàn thành |
| `room.objectClicked` | React | User click vào vật thể |
| `room.interactionTriggered` | React | Trigger interaction 3D |
| `bridge.ready` | React | Unity bridge sẵn sàng nhận command |
| `bridge.error` | React | Lỗi trong Unity runtime |

---

## 7. Sơ đồ boundary

```
┌─────────────────────────────────────────────────────────────────────┐
│                        TAURI (Desktop Host)                         │
│  - Lifecycle: start/stop backend, start/stop Unity                  │
│  - Single window, process manager                                   │
│  - Bridge: React ↔ Unity (forward commands/events)                  │
├──────────────────────┬──────────────────────────────────────────────┤
│   REACT / VITE       │   UNITY (Embedded 3D Runtime)               │
│                      │                                              │
│  Business UI:        │  3D Runtime:                                 │
│  - Chat page         │  - AvatarStateMachine                        │
│  - Planner page      │  - AvatarAnimatorBridge                      │
│  - Settings page     │  - AvatarFacialController                    │
│  - Wardrobe page     │  - LipSyncController/Driver                  │
│  - Dashboard page    │  - AvatarLocomotionController                │
│  - System Status     │  - AvatarLookAtController                    │
│                      │  - AvatarConversationBridge                  │
│  State management:   │  - AvatarEquipmentManager                    │
│  - Task state        │  - AudioPlaybackController (TTS)             │
│  - Chat state        │  - WavEncoder (mic capture)                  │
│  - Settings state    │  - Room scene                                │
│  - Auth state        │  - Camera controller                         │
│                      │  - 3D interaction system                     │
│  API Client:         │                                              │
│  - REST to Backend   │  Bridge Client (NEW - Phase 5):              │
│  - WebSocket events  │  - UnityBridgeClient                         │
│  - Stream WS         │  - Command receiver                          │
│                      │  - Event emitter                             │
├──────────────────────┴──────────────────────────────────────────────┤
│                    FASTAPI BACKEND (local-backend/)                  │
│  - Chat orchestration, task CRUD, settings, memory                  │
│  - STT/TTS pipeline, scheduler, SQLite persistence                  │
│  - Được giữ nguyên, không thay đổi trong phase rebuild              │
└─────────────────────────────────────────────────────────────────────┘

Communication:
  React ↔ Backend:  HTTP REST + WebSocket
  React ↔ Tauri:    Tauri commands/events (invoke, emit)
  Tauri ↔ Unity:    Local WebSocket (port nội bộ, debug-friendly)
```

---

## 8. Summary thống kê

| Tag | Số feature | Ghi chú |
|-----|-----------|---------|
| **retire** | ~32 | Shell UI, Chat UI, Planner UI, Settings UI, Home UI, Overlays UI, Network clients |
| **keep** | ~15 | Avatar system, audio playback, mic capture, animation, room, camera |
| **partial-keep** | ~4 | Subtitle (giữ 3D layer), event bus, health utilities, module contracts |

**Số script cần retire:** ~55 files trong retired pre-cutover Unity shell tree  
**Số script cần giữ:** ~20 files trong `AvatarSystem/` và `Scripts/Avatar/`, `Audio/`

---

*Xem sơ đồ giao tiếp chi tiết: `docs/migration/phase1-boundary.md`*
