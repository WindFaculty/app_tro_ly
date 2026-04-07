# Phase 3 - Web UI Skeleton

**Status:** Current implementation (repo-side scaffold landed), runtime validation blocked by missing local toolchain  
**Ngày:** 2026-04-07  
**Dependencies:** Phase 1 boundary docs, Phase 2 desktop shell host  
**Truth sources:** `apps/web-ui/`, `apps/desktop-shell/src-tauri/tauri.conf.json`, `local-backend/app/api/routes.py`

## Mục tiêu

Dựng Web UI skeleton tách riêng khỏi `apps/desktop-shell/` để React trở thành owner rõ ràng của business UI theo kế hoạch rebuild.

## Current implementation

- `apps/web-ui/` hiện là source of truth mới cho React/Vite UI skeleton.
- `apps/desktop-shell/` được thu gọn về vai trò Tauri host và trỏ sang web app mới qua `beforeDevCommand`, `beforeBuildCommand`, và `frontendDist`.
- Web UI hiện có 6 route skeleton:
  - `Dashboard`
  - `Chat`
  - `Planner`
  - `Wardrobe`
  - `Settings`
  - `Status`
- Frontend hiện có typed client repo-side cho các backend endpoints đang dùng:
  - `GET /v1/health`
  - `GET /v1/tasks/today`
  - `GET /v1/tasks/week`
  - `GET /v1/tasks/inbox`
  - `GET /v1/tasks/completed`
  - `GET /v1/settings`
  - `POST /v1/chat`
- Web UI có browser-preview fallback:
  - Khi chạy ngoài Tauri, app không chờ desktop host auto-start backend.
  - Khi chạy trong Tauri, app vẫn nghe `backend-ready` và `backend-error` từ Rust host.

## Chưa hoàn tất trong phase này

- Manual validation required: build hoặc run `apps/web-ui/` và `apps/desktop-shell/` chưa thực hiện được trong turn này vì máy hiện tại không có `node` và `cargo` trong PATH.
- Chưa có state management đầy đủ cho toàn bộ feature migration; hiện mới có repo-side service layer tối thiểu.
- Chưa có Unity embed, chưa có typed Tauri-to-Unity bridge, chưa có production page flows.

## Acceptance coverage

- React app đã có thư mục độc lập `apps/web-ui/`: yes
- Có navigation thật và 6 pages mục tiêu: yes
- Có backend API connection từ React: yes, repo-side client đã nối vào nhiều page
- React self-runnable về mặt cấu trúc: yes
- Runtime evidence: no, blocked bởi thiếu Node.js và Rust toolchain

## Verification

- Repo structure check xác nhận `apps/web-ui/` tồn tại và `apps/desktop-shell/src-tauri/tauri.conf.json` đã trỏ sang web app mới.
- Static endpoint verification dựa trên code thật ở `local-backend/app/api/routes.py`.

## Kết luận

Phase 3 hiện đã có repo-side scaffold và boundary đúng hướng. Để gọi phase này là hoàn tất hoàn toàn vẫn cần một pass build hoặc dev run sau khi cài Node.js và Rust.
