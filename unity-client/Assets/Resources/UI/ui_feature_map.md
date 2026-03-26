# UI Feature Map — Trợ Lý Ảo (Thế hệ mới - Luminal / Aetheris Design)

Tài liệu này mô tả chi tiết từng khu vực giao diện dựa trên kiến trúc 3 cột mới (Left Sidebar, Center Stage, Right Panel) với thiết kế hiện đại, cao cấp hơn.

---

## 0. GLOBAL LAYOUT (Bố cục chung)
Giao diện chia làm 3 vùng chính theo chiều ngang + 1 Top Bar cố định bên trên:
- **Top Bar** (Chiều cao ~80px): Chứa Logo/Title, Các Tab điều hướng chính, Nút thông báo và Avatar User.
- **Left Sidebar** (Rộng ~250-300px): Hiển thị Tên hệ thống AI, Trạng thái (Health), Menu theo ngữ cảnh (Tự do/Thiết bị/Bộ nhớ...), Nút Action chính (Làm mới/Kích hoạt), Footer (Trợ giúp, Đăng xuất).
- **Center Stage** (Co giãn flex-grow: 1): Vùng nội dung chính thay đổi dựa vào việc chọn Tab ở Top Bar (VD: Trang chủ hiển thị Avatar 3D, Lịch trình hiển thị Lưới lịch, Cài đặt hiển thị Các tuỳ chọn).
- **Right Panel** (Rộng ~350-400px): Vùng tương tác phụ, thay đổi theo Tab chính. Ở Trang chủ và Cài đặt, đây là vùng Chat (Luồng hội thoại). Ở Lịch trình, đây là vùng Gợi ý AI & Danh sách việc sắp Tới.

---

## 1. TOP BAR (Thanh điều hướng phía trên)

| Phần tử | Loại | Tên (name) | Tính năng |
|---|---|---|---|
| Cụm Logo & Title | Label / Image | `TopBarLogo` | Nhận diện thương hiệu "Trợ Lý Ảo" |
| **Trang chủ** | Button | `HomeTab` | Đổi Center Stage sang màn hình chính (Avatar), Right Panel thành Chat |
| **Lịch trình** | Button | `ScheduleTab` | Đổi Center Stage sang lưới lịch, Right Panel thành thông tin công việc sắp tới |
| **Cài đặt** | Button | `SettingsTab` | Đổi Center Stage sang panel cài đặt, Right Panel thành Chat |
| Nút thông báo | Button | `TopNotifBtn` | Hiển thị chuông thông báo (kèm badge đỏ nếu có) |
| Avatar User | Image | `TopAvatarUser` | Hình đại diện của người dùng (mở menu tuỳ chỉnh tài khoản) |

---

## 2. LEFT SIDEBAR (Thanh điều hướng bên trái)

*Lưu ý: Nội dung thanh này có thể thay đổi nhẹ tuỳ thuộc vào Tab đang chọn trên Top Bar (VD: Tab Lịch trình sẽ hiển thị thẻ Aetheris Engine).*

| Phần tử | Loại | Tên (name) | Tính năng |
|---|---|---|---|
| Thẻ định danh AI | VisualElement | `SidebarIdentity` | Tên của AI (The Luminal / Aetheris) và trạng thái hiện diện (Active Presence) |
| System Health | Card | `SidebarHealthCard` | Header hiển thị % hiệu năng và trạng thái (VD: 98.4% Optimal) |
| Nhóm Menu | ScrollView | `SidebarNavMenu` | Danh sách Nút menu phụ: Tự do, Thiết bị, Thông báo, Phân tích AI, Bộ nhớ... |
| **Refresh / Activate** | Button | `SidebarActionBtn` | Nút kêu gọi hành động (CTA) phụ thuộc ngữ cảnh: "Refresh System" hoặc "Kích hoạt AI" |
| **Trợ giúp (Help)** | Button | `SidebarHelpBtn` | Link mở tài liệu hướng dẫn / popup trợ giúp |
| **Đăng xuất (Logout)** | Button | `SidebarLogoutBtn` | Link đăng xuất khỏi hệ thống |

---

## 3. CENTER STAGE (Khu vực trung tâm)

### A. Màn hình "Trang chủ" (Home)
| Phần tử | Loại | Tên (name) | Tính năng |
|---|---|---|---|
| Khung hiển thị Avatar | VisualElement | `HomeAvatarContainer` | Hiển thị mô hình 3D thực tế ảo hoặc hiệu ứng vòng tròn sáng |
| Badge Trạng thái | Label | `HomeAvatarBadge` | Nổi đè lên Avatar (VD: LISTENING, THINKING) |
| Lời thoại AI (Subtitle) | Label | `HomeSubtitleText` | Hiển thị theo thời gian thực câu nói của AI, phông chữ lớn, nằm dưới Avatar |
| Panel Lịch trình thu gọn | VisualElement | `HomeMiniCalendar` | Hiển thị bộ lọc thời gian (Today/Week/Month) và xem trước sự kiện nhanh chóng |

### B. Màn hình "Lịch trình" (Schedule)
| Phần tử | Loại | Tên (name) | Tính năng |
|---|---|---|---|
| Header Lịch trình | Label | `ScheduleTitle` | Dòng chữ lớn "Lịch trình AI" và phụ đề phân tích tối ưu |
| Bộ chọn khoảng thời gian | Button Group | `ScheduleViewTabs` | Các nút chọn góc nhìn "Ngày", "Tuần", "Tháng" |
| Nút thêm mới | Button | `ScheduleAddBtn` | Nút "Sự kiện mới" nổi bật |
| Thanh điều hướng Tháng | Label / Buttons | `ScheduleNav` | Chữ "Tháng 10, 2024" kèm 2 nút `<` và `>` |
| Khung lưới lịch (Grid) | VisualElement | `ScheduleGrid` | Hiển thị tuần / tháng với các block đồ hoạ dạng thẻ sự kiện (VD: Review code...) |

### C. Màn hình "Cài đặt" (Settings)
| Phần tử | Loại | Tên (name) | Tính năng |
|---|---|---|---|
| Header Cài đặt | Label | `SettingsTitle` | Dòng chữ lớn "Cấu hình Hệ thống" & mô tả |
| Danh sách tuỳ chọn | VisualElement | `SettingsOptionList` | Vùng chứa các nhóm tính năng (Tương tác / Kết nối...) |
| Badge Trạng thái lưu | Label | `SettingsSaveBadge` | Tag trạng thái "Đã lưu" hoặc "Unsaved Changes" |
| Nhóm Toggle Controls | Toggle | *Nhiều Toggles* | "Phát giọng nói", "Xem trước transcript", "Mini assistant", "Đọc reminder" |
| Thanh chức năng dưới | VisualElement | `SettingsActionRow` | Vùng chứa nút Lưu / Tải lại |
| **Reload** | Button | `SettingsReloadBtn` | Phục hồi cài đặt gốc/trước đó |
| **Save Changes** | Button | `SettingsSaveBtn` | Nút bấm để áp dụng cấu hình (hiệu ứng sáng) |

---

## 4. RIGHT PANEL (Bảng điều khiển khung phải)

### A. Chế độ "Luồng hội thoại" (Trang chủ / Cài đặt)
| Phần tử | Loại | Tên (name) | Tính năng |
|---|---|---|---|
| Header Chat | Label | `ChatPanelTitle` | "LUỒNG HỘI THOẠI" kèm tag "GPT-4.0" bên phải |
| Khung danh sách tin nhắn | ScrollView | `ChatScrollView` | Cuộn xem lịch sử chat |
| Bong bóng AI (Trái) | Template | `ChatBubbleAI` | Tin nhắn do Assistant phản hồi kèm avatar mini xám nhạt |
| Bong bóng User (Phải) | Template | `ChatBubbleUser` | Tin nhắn của người dùng kèm màu nền xanh xám đậm |
| Thanh nhập tin nhắn | VisualElement | `ChatInputRow` | Đáy Right Panel |
| Nút thu âm (Mic) | Button | `ChatMicBtn` | Bấm để trò chuyện bằng giọng nói (voice input) |
| Ô nhập liệu (Input) | TextField | `ChatInputTextField` | Khung nhập chữ "Hỏi tôi bất cứ điều gì..." |
| Nút gửi (Send) | Button | `ChatSendBtn` | Nút hình mũi tên hoặc máy bay giấy |

### B. Chế độ "Lịch trình" (Thông tin mở rộng)
| Phần tử | Loại | Tên (name) | Tính năng |
|---|---|---|---|
| Khung Tư vấn AI | VisualElement | `RightConsultCard` | Viền gradient cyan, tiêu đề "Tư vấn từ Aetheris", chứa Text gợi ý dời lịch/tối ưu... |
| Nút Tối ưu ngay | Button | `RightOptimizeBtn` | Nút hành động nhanh ("Tối ưu ngay ->") |
| Header Sắp tới | Label | `RightUpcomingTitle` | Dòng tiêu đề "SẮP TỚI" |
| Thẻ danh sách việc | VisualElement | `RightUpcomingItem` | Mỗi thẻ gồm Ngày (T5 03), Giờ (14:00 - 15:30) và Tên việc |
| Widget Thời tiết | VisualElement | `RightWeatherWidget` | Card góc dưới chứa "24°C Hà Nội, Việt Nam" kèm ảnh nền phong cảnh |
| Nút FAB/Thêm nhanh | Button | `RightQuickAddFab` | Nút hình tròn xanh cyan `+` lơ lửng góc dưới phải |

---

## 5. HẠNG MỤC ƯU TIÊN THỰC THI (MIGRATION PLAN)

1. **Bố cục khung sườn (Layout Wrapper)**: Tháo dỡ `MainUI.uxml` cũ, phân chia lại thành 3 thẻ Flex chính bằng UI Builder: `#LeftSidebar`, `#CenterStage`, `#RightPanel`. Đảm bảo responsive theo tỷ lệ chiều ngang.
2. **Cấu trúc lại Hierarchy**: Tách thành các file `.uxml` riêng biệt cho sạch: `Sidebar.uxml`, `TopBar.uxml`, `ChatPanel.uxml`. Dùng `<ui:Template>` để nhúng vào `MainUI.uxml`.
3. **Cập nhật Stylings**: Viết lại hệ màu của `MainStyle.uss`. Sử dụng biến `var(--color-bg-dark)`, `var(--color-primary-cyan)`, `var(--color-text-white)`...
4. **Viết lại C# Logic**: Sửa `UiFactory` và tạo `MainLayoutController.cs` để quản lý chuyển đổi view khi bấm Tab (Hide/Show Center Stage & Right Panel tương ứng).
5. **Tinh chỉnh Tiểu tiết**: Rounded corners, box-shadow, linear-gradients, hiệu ứng hover button.
