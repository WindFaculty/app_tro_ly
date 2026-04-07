# Phase 4 - Unity Embed Scaffold

**Status:** Current implementation (repo-side scaffold landed), native attach still planned work  
**Ngày:** 2026-04-07  
**Dependencies:** `apps/desktop-shell/src-tauri/`, `apps/web-ui/`, Unity project ở `ai-dev-system/clients/unity-client/`

## Mục tiêu

Chuẩn bị lifecycle và trạng thái cho Unity sidecar trong Tauri trước khi làm native window attach thật.

## Current implementation

- Tauri hiện có module runtime mới tại `apps/desktop-shell/src-tauri/src/unity_runtime.rs`.
- Desktop host hiện expose 3 command cho frontend:
  - `get_unity_runtime_status`
  - `launch_unity_runtime`
  - `stop_unity_runtime`
- Desktop host emit event `unity-runtime-status` để React cập nhật center panel.
- React center region giờ không còn là placeholder tĩnh; nó hiển thị:
  - trạng thái Unity runtime
  - executable path nếu tìm thấy
  - build root nếu tìm thấy
  - pid nếu sidecar đang chạy
- Unity runtime resolver hiện hỗ trợ 3 nguồn:
  - env `APP_TRO_LY_UNITY_EXE`
  - env `APP_TRO_LY_UNITY_BUILD_DIR`
  - scan repo trong các vùng build/release dự kiến

## Chưa hoàn tất

- Chưa có native attach của Unity window vào vùng center trong Tauri.
- Chưa có resize/focus/minimize/restore sync.
- Chưa có build Unity executable trong repo để launch thật từ scaffold hiện tại.
- Manual validation required: chưa run được Tauri host vì máy hiện tại thiếu `cargo` và frontend toolchain.

## Acceptance coverage

- Spawn Unity sidecar lifecycle scaffold: yes
- Frontend nhận và hiển thị Unity runtime status: yes
- Resolve build artifact path: yes
- Native window attach: no
- Resize/focus sync: no
- Runtime evidence: no, blocked bởi thiếu Unity build artifact và local toolchain

## Verification

- Repo-side code inspection xác nhận `lib.rs` đã đăng ký command Unity runtime và manage state.
- Repo-side search xác nhận chưa có Unity executable trong `ai-dev-system/clients/unity-client/` hoặc `release/`, nên blocker hiện tại là thật.

## Kết luận

Phase 4 hiện đã có scaffold đúng hướng cho sidecar lifecycle và trạng thái UI. Bước tiếp theo để tiến tới embed thật là tạo hoặc cung cấp Unity build executable, rồi nối sang Windows native attach logic.
