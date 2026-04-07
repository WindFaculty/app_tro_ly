# Phase 6 - Typed Communication Contracts

**Status:** Current implementation (repo-side scaffold landed), runtime validation still pending  
**Ngay:** 2026-04-07  
**Dependencies:** `packages/contracts/`, `apps/web-ui/`, `apps/desktop-shell/src-tauri/`, `ai-dev-system/clients/unity-client/Assets/Scripts/Runtime/`

## Muc tieu

Chuan hoa command/event giua React, Tauri, va Unity thanh mot schema typed chung de giam drift, giam string ad-hoc, va dat nen cho page-context sync o phase sau.

## Current implementation

- Da co source of truth moi tai `packages/contracts/`:
  - `src/unity.ts` cho protocol version, command names, event names, payload shapes, envelope helpers
  - `src/tauri.ts` cho Tauri command names, Tauri event names, runtime/bridge status payloads
- Web UI da dung contract chung thay vi string roi rac:
  - `apps/web-ui/src/services/runtimeHost.ts`
  - `apps/web-ui/src/services/unityBridge.ts`
  - `apps/web-ui/src/components/ShellLayout.tsx`
- Router React hien gui typed command `app.pageChanged` moi khi route thay doi.
- Tauri host da co typed bridge scaffold moi:
  - `apps/desktop-shell/src-tauri/src/unity_bridge.rs`
  - command `get_unity_bridge_status`
  - command `send_unity_bridge_command`
  - event `unity-bridge-status`
  - event `unity-bridge-event`
  - local websocket listener scaffold tai `ws://127.0.0.1:7857/unity-bridge`
- Unity runtime da co websocket client scaffold moi:
  - `Assets/Scripts/Runtime/TauriBridgeRuntime.cs`
  - `Assets/Scripts/Runtime/RuntimeModels.cs`
  - `Assets/Scripts/Runtime/UnityBridgeClient.cs`
- `AppCompositionRoot` hien da bind `TauriBridgeRuntime` vao cum runtime moi.
- Canonical wardrobe command duoc chot la `wardrobe.equipItem`.
- `UnityBridgeClient` tam thoi van chap nhan legacy alias `avatar.equipItem` de tranh drift trong luc docs va UI dang duoc dong bo.

## Chua hoan tat

- Chua co runtime evidence xac nhan websocket handshake React -> Tauri -> Unity tren may co Node.js, Rust, va Unity build.
- Chua co typed event day du cho `avatar.animationFinished` hoac `room.objectClicked`; hien repo-side event scaffold moi phat `bridge.ready`, `bridge.error`, `avatar.stateChanged`, va `room.interactionTriggered`.
- `wardrobe.equipItem` moi co schema typed, nhung item registry/runtime mapping van la planned work.
- Chua xac minh compile cho Rust host hoac Unity client trong turn nay.

## Manual validation required

- Build va chay `apps/web-ui/` + `apps/desktop-shell/` tren may co `node` va `cargo`.
- Chay Unity Editor hoac standalone build co `TauriBridgeRuntime` de xac nhan websocket local connect duoc.
- Ghi artifact cho luong toi thieu:
  - Tauri bridge vao trang thai `connected`
  - React route doi -> `app.pageChanged` duoc deliver
  - Unity gui lai it nhat `bridge.ready` va `avatar.stateChanged`

## Verification

- Repo-side search xac nhan `packages/contracts/` da tro thanh nguon schema typed cho web layer.
- Repo-side search xac nhan Tauri host co command/event bridge moi va Unity runtime co `TauriBridgeRuntime`.
- Verification runtime van con thieu vi may hien tai khong co `node`, `cargo`, va repo chua co Unity standalone build artifact.

## Ket luan

Phase 6 hien da co scaffolding typed end-to-end o muc repo-side. Day la nen giao tiep chung de Phase 7 va Phase 8 chuyen UI/page-context ma khong quay lai kieu command string roi rac.
