# Rebuild Rules — Luật migration bất biến

**Status:** Locked — áp dụng từ Phase 0  
**Phase:** Phase 0 Freeze  
**Ngày tạo:** 2026-04-07  
**Tác giả:** Antigravity AI (theo kế hoạch tái thiết `tai_cau_truc.md`)

---

## Tuyên bố

Các luật dưới đây có hiệu lực ngay từ thời điểm Phase 0 hoàn tất.  
**Không agent, không developer nào được vi phạm các luật này** mà không có approval rõ ràng và cập nhật lại file này.

---

## 8 Luật cứng bất biến

### RULE-01: Unity không chứa business UI

- Unity **không được** thêm mới panel, screen, overlay, hay bất kỳ UI nào phục vụ nghiệp vụ.
- Mọi business UI mới phải đặt trong React.
- Áp dụng từ: Phase 0
- Vi phạm: thêm UXML/USS/C# UI script nghiệp vụ mới vào `ai-dev-system/clients/unity-client/`

### RULE-02: React là giao diện nghiệp vụ chính

- Chat, Planner, Settings, Wardrobe panel, Reminder display, System status — tất cả thuộc React.
- Unity chỉ nhận lệnh và emit event; Unity không quyết định giao diện nghiệp vụ.
- Áp dụng từ: Phase 3 (khi React skeleton sẵn sàng)

### RULE-03: Backend là nguồn sự thật cho dữ liệu nghiệp vụ

- Task, Chat, Settings, Memory, Reminder — tất cả đọc/ghi qua backend API.
- Không lưu state nghiệp vụ trong Unity hoặc React local state dài hạn mà không sync về backend.
- Backend hiện tại tại `local-backend/` được giữ nguyên; không viết lại backend trong phase rebuild.
- Áp dụng từ: ngay lập tức

### RULE-04: Tauri là process host duy nhất

- Chỉ có một entry point desktop: Tauri app.
- Không có multi-window không được kiểm soát.
- Tauri chịu trách nhiệm start backend, start Unity, attach Unity window.
- Không dùng Unity làm entry point.
- Áp dụng từ: Phase 2

### RULE-05: Unity chỉ là runtime 3D được nhúng

- Unity tồn tại như một sidecar 3D engine, embedded vào vùng trung tâm của Tauri window.
- Unity không phải là app host.
- Unity không mở cửa sổ riêng lẻ.
- Áp dụng từ: Phase 4

### RULE-06: Không gọi Unity ad-hoc từ React

- React không gọi trực tiếp Unity qua string, eval, hay bất kỳ cơ chế không typed.
- Mọi lệnh gửi đến Unity phải đi qua Tauri bridge, theo contract typed.
- Áp dụng từ: Phase 6

### RULE-07: Mọi giao tiếp phải có contract typed

- Không có JSON ad-hoc tự do giữa các layer.
- Contract groups:
  - `app.lifecycle.*`
  - `backend.health.*`
  - `chat.*`
  - `task.*`
  - `settings.*`
  - `voice.*`
  - `avatar.*`
  - `room.*`
  - `wardrobe.*`
- Các contract nằm trong `packages/contracts/` (hoặc tương đương).
- Áp dụng từ: Phase 6

### RULE-08: Không thêm tính năng mới vào Unity shell UI cũ

- Unity shell UI cũ (UXML/USS based) là legacy, sẽ bị retire.
- Không mở rộng `AppShellController`, `AssistantApp`, `UiDocumentLoader`, các `*ScreenRefs.cs`, `*PanelRefs.cs`.
- Mọi tính năng mới phải bắt đầu từ React.
- Áp dụng từ: Phase 0 (ngay lập tức)

---

## Feature ownership chốt

| Feature | Owner hiện tại | Owner đích | Phase chuyển |
|---------|---------------|-----------|-------------|
| Chat UI | Unity | React | Phase 7 |
| Planner UI | Unity | React | Phase 7 |
| Settings UI | Unity | React | Phase 7 |
| Voice controls | Unity | React + Backend | Phase 7 |
| Reminder display | Unity | React | Phase 7 |
| Subtitle overlay | Unity | React | Phase 7 |
| Avatar mood/state | Unity | Unity | Giữ nguyên |
| Room interaction | Unity | Unity | Giữ nguyên |
| Wardrobe preview | — | React + Unity | Phase 7+ |
| Task CRUD | Backend | Backend | Giữ nguyên |
| Memory/history | Backend | Backend + React | Giữ nguyên |
| Process lifecycle | — | Tauri | Phase 2 |

---

## Nguyên tắc di chuyển path

1. **Không move thư mục vật lý sớm.** Chờ đến khi boundary code đã hoạt động.
2. Move theo thứ tự: chuẩn hóa boundary → verify → move path → update scripts/docs.
3. Sau mỗi move, update `scripts/`, `docs/`, và `AGENTS.md` để phản ánh path mới.
4. Không copy-paste code mà không xóa bản cũ.

---

## Định nghĩa labels

- `Current implementation`: hành vi đã có trong code repo hiện tại
- `Planned work`: việc chưa implement, không được mô tả như đã xong
- `Design target`: kiến trúc mục tiêu, chưa triển khai
- `Placeholder`: UI/behavior tạm thời, sẽ bị thay thế
- `Retired`: đã xóa hoặc vô hiệu hóa, không dùng nữa
- `Manual validation required`: cần Unity Editor, built client, hoặc target machine

---

## Quy trình thêm rule mới

1. Tạo PR hoặc approval rõ ràng từ project owner.
2. Thêm rule với số thứ tự tiếp theo (`RULE-09`, `RULE-10`, ...).
3. Ghi rõ ngày áp dụng và phase áp dụng.
4. Update `tasks/rebuild-master-plan.md` nếu rule ảnh hưởng đến timeline.

---

*Xem `rebuild-target.md` cho chi tiết kiến trúc đích. Xem `tasks/rebuild-master-plan.md` cho roadmap thực thi.*
