# Phase 5 - Unity Runtime Facade Scaffold

**Status:** Current implementation (repo-side scaffold landed), legacy shell UI still present  
**Ngày:** 2026-04-07  
**Dependencies:** `ai-dev-system/clients/unity-client/Assets/Scripts/`, `ai-dev-system/clients/unity-client/Assets/AvatarSystem/`

## Mục tiêu

Bắt đầu tách Unity khỏi vai trò shell UI bằng cách dựng các facade runtime mới cho avatar, room, animation, lip sync, interaction, scene state, và bridge command handling.

## Current implementation

- Có namespace runtime mới tại `ai-dev-system/clients/unity-client/Assets/Scripts/Runtime/`.
- Các facade mới đã được thêm:
  - `AvatarRuntime`
  - `AnimationRuntime`
  - `LipSyncRuntime`
  - `RoomRuntime`
  - `InteractionRuntime`
  - `SceneStateController`
  - `UnityBridgeClient`
  - `RuntimeModels`
- `AppCompositionRoot` hiện đã tạo và bind cụm runtime mới từ các truth source hiện có:
  - `AvatarStateMachine`
  - `LipSyncController`
  - `AvatarConversationBridge`
  - `AvatarRootController`
  - `Camera.main`
- `UnityBridgeClient` hiện xử lý repo-side command dispatch cho:
  - `app.pageChanged`
  - `avatar.setMood`
  - `avatar.playEmote`
  - `avatar.speakStart`
  - `avatar.speakStop`
  - `avatar.setIdleState`
  - `avatar.setListeningState`
  - `room.setCameraFocus`
  - `room.focusObject`

## Chưa hoàn tất

- `AssistantApp` vẫn là coordinator lớn của runtime hiện tại; chưa bị tách nhỏ hoặc thay thế.
- Chat UI, planner UI, settings UI, shell layout UI Toolkit vẫn còn tồn tại và chưa bị retire.
- Chưa có WebSocket bridge thật từ Tauri sang Unity runtime.
- Chưa có Unity compile/run validation trong turn này.

## Acceptance coverage

- Có `UnityBridgeClient`: yes
- Có runtime modules chuẩn cho avatar/room/animation/lipsync/scene: yes
- Legacy shell UI đã bị gỡ bỏ: no
- Unity chạy độc lập như 3D engine con: not yet
- Runtime evidence: no, cần Unity Editor hoặc standalone build để xác minh

## Verification

- Repo-side code inspection xác nhận `AppCompositionRoot` đã bind các facade runtime mới vào `AssistantRuntime`.
- Repo-side search xác nhận `UnityBridgeClient` và các facade runtime đã tồn tại trong Unity tree.

## Kết luận

Phase 5 hiện đã có scaffold đúng hướng để Unity dần trở thành 3D runtime riêng. Bước tiếp theo là nối bridge thật vào facade mới và bắt đầu rút bớt trách nhiệm khỏi `AssistantApp`.
