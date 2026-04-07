Kế hoạch tái thiết hoàn chỉnh
1. Kiến trúc đích

Kiến trúc mới của dự án sẽ là:

Tauri Desktop Host
├── React/Vite Web UI
├── Unity Embedded Runtime
└── Local Backend (FastAPI)
Vai trò từng phần
Tauri

Là vỏ desktop chính, chịu trách nhiệm:

mở một cửa sổ duy nhất
nhúng Web UI
khởi động backend local
khởi động Unity runtime
nhúng Unity vào vùng trung tâm
quản lý lifecycle, log, crash, retry
bridge giao tiếp giữa Web UI và Unity nếu cần
React/Vite

Là giao diện chính cho người dùng:

chat
task
lịch
settings
profile
memory/history
wardrobe panel sau này
trạng thái hệ thống
Unity

Chỉ còn trách nhiệm:

render phòng
render avatar
animation
di chuyển trong phòng
tương tác với vật thể trong phòng
lip sync
emote, mood, camera, idle state
FastAPI backend

Tiếp tục là lõi nghiệp vụ:

chat orchestration
task/reminder
settings persistence
memory
voice pipeline
scheduler
sqlite/data
2. Bố cục cửa sổ chính

App chỉ có một cửa sổ, nhưng bên trong dùng layout có page.

Bố cục tổng thể
thanh điều hướng bên trái hoặc trên
vùng nội dung chính ở giữa
vùng Unity cố định ở trung tâm hoặc chiếm phần lớn middle layout
các page của React bao quanh hoặc overlay hợp lý
không mở nhiều cửa sổ riêng lẻ ở phase đầu
Layout khuyến nghị
┌──────────────────────────────────────────────────────────────┐
│ Topbar / Navigation / Quick Actions                         │
├───────────────┬───────────────────────────────┬──────────────┤
│ Left Panel    │ Center Panel                  │ Right Panel  │
│ React UI Nav  │ Unity Render Region           │ React Context│
│ Pages / Tabs  │ Room + Avatar                 │ Detail Panel │
├───────────────┴───────────────────────────────┴──────────────┤
│ Bottom utility / status / voice / notifications             │
└──────────────────────────────────────────────────────────────┘
Logic dùng page

Các page là page trong React, nhưng Unity region vẫn tồn tại như một phần trung tâm ổn định.
Tức là khi chuyển page:

Unity không biến mất
chỉ đổi panel xung quanh và hành vi context

Ví dụ:

Page Chat: panel chat mở bên phải
Page Planner: panel task/lịch mở bên trái hoặc phải
Page Wardrobe: panel đồ/costume mở ra, Unity vẫn hiển thị avatar để preview
Page Room: panel quản lý phòng hoặc vật phẩm

Đây là hướng đúng cho app trợ lý có nhân vật 3D.

3. Kiến trúc thư mục mới

Tôi khuyên target structure như sau:

app_tro_ly/
├── apps/
│   ├── desktop-shell/         # Tauri
│   ├── web-ui/                # React + Vite
│   └── unity-runtime/         # Unity project
├── services/
│   └── local-backend/         # FastAPI backend hiện tại
├── packages/
│   ├── contracts/             # shared DTO / event schema
│   └── ui-types/              # shared TS types nếu cần
├── docs/
│   ├── architecture/
│   ├── migration/
│   ├── runtime/
│   └── ui/
├── tasks/
├── scripts/
└── tools/
Mapping từ repo hiện tại
unity-client/ → apps/unity-runtime/
local-backend/ → services/local-backend/
Lưu ý quan trọng

Không nên move thư mục vật lý ngay ngày đầu.
Nên làm theo 2 bước:

chuẩn hóa boundary trước
move path sau

Vì hiện repo đang có script và doc phụ thuộc path khá nhiều .

4. Nguyên tắc kiến trúc bắt buộc

Từ thời điểm tái thiết, cần khóa 8 luật cứng này:

Unity không được chứa business UI nữa
React là giao diện nghiệp vụ chính
Backend là nguồn sự thật cho task/chat/settings/data
Tauri là process host duy nhất
Unity chỉ là runtime 3D được nhúng
Không cho React gọi trực tiếp Unity bằng cách ad-hoc
Mọi giao tiếp qua contract typed rõ ràng
Không thêm tính năng mới vào Unity shell UI cũ
5. Kế hoạch theo phase
Phase 0 — Freeze hiện trạng và khóa target-state
Mục tiêu

Ngăn repo tiếp tục phát triển sai hướng.

Việc làm
Chụp lại trạng thái hiện tại của repo
Ghi rõ kiến trúc đích mới
Ghi rõ Unity shell UI cũ sẽ bị retire
Đánh dấu backend được giữ lại
Xác định phần nào của Unity phải giữ:
avatar system
room
animation
camera
interaction logic 3D
Output
docs/migration/rebuild-target.md
docs/migration/rebuild-rules.md
tasks/rebuild-master-plan.md
Acceptance
mọi thành viên/agent đọc repo đều hiểu rằng Unity không còn là shell UI chính
Phase 1 — Thiết kế boundary mới
Mục tiêu

Tách trách nhiệm rõ ràng trước khi viết lại UI.

Việc làm
Vẽ ownership matrix:
Tauri owns lifecycle
React owns business UI
Unity owns 3D
Backend owns business logic
Liệt kê toàn bộ feature hiện có trong Unity
Gắn tag cho từng feature:
keep in Unity
move to React
retire
Liệt kê API nào backend đã có thể tái sử dụng
Liệt kê event nào Unity cần nhận/gửi
Bảng ownership chốt
Feature	Owner mới
Chat UI	React
Planner UI	React
Settings UI	React
Voice controls	React + backend
Reminder display	React
Avatar mood	Unity
Room interaction	Unity
Wardrobe preview	React + Unity
Task CRUD	backend
Memory/history	backend + React
Acceptance
có tài liệu ownership rõ ràng
không còn tranh chấp “cái này để bên nào làm”
Phase 2 — Dựng Tauri shell tối thiểu
Mục tiêu

Có app desktop host chạy được.

Việc làm
Khởi tạo apps/desktop-shell
Tích hợp React/Vite vào Tauri
Tạo main window
Tạo process manager cho backend
Tạo process manager cho Unity
Tạo logging cơ bản
Tạo health-check startup
Kết quả cần đạt

Mở một app desktop duy nhất và thấy:

shell lên
web UI chạy
backend được start tự động
Acceptance
chưa cần Unity embed ở phase này
nhưng Tauri đã là entrypoint chính
Phase 3 — Dựng Web UI skeleton
Mục tiêu

Xây bộ khung giao diện mới.

Các page nên có ngay từ đầu
Dashboard
Chat
Planner
Settings
Wardrobe
System Status
Việc làm
dựng layout page-based
tạo navigation
tạo state management
tạo API client typed
kết nối với backend hiện tại
làm mock panel cho vùng Unity ở center
Cấu trúc frontend khuyến nghị
src/
├── app/
├── pages/
├── widgets/
├── features/
├── entities/
├── shared/
├── services/
└── contracts/
Acceptance
React app tự chạy độc lập
đã có shell điều hướng thật
backend gọi được từ React
Phase 4 — Nhúng Unity vào Tauri
Mục tiêu

Biến Unity thành vùng render thật trong app.

Việc làm
build Unity thành runtime executable
Tauri spawn Unity sidecar
tìm Unity window handle
attach vào vùng native container
đồng bộ resize/focus/minimize/restore
đảm bảo 1 cửa sổ tổng thể duy nhất
Lưu ý

Đây là phase kỹ thuật khó nhất của phần desktop.

Acceptance
Unity xuất hiện trong vùng trung tâm của app
resize theo app
không bị nổi thành cửa sổ riêng
Phase 5 — Tái cấu trúc Unity thành 3D runtime tinh gọn
Mục tiêu

Gỡ toàn bộ shell UI cũ trong Unity.

Việc làm
xóa dần:
chat UI
planner UI
settings UI
shell layout UI Toolkit
giữ lại:
scene room
avatar
camera
interaction
emotion
lip sync
animation runtime
tạo bridge nội bộ cho Unity:
command receiver
event emitter
Các module Unity nên tồn tại sau refactor
AvatarRuntime
RoomRuntime
InteractionRuntime
AnimationRuntime
LipSyncRuntime
UnityBridgeClient
SceneStateController
Acceptance
Unity build vẫn chạy
không cần UI shell cũ để hoạt động
Unity có thể sống như 3D engine con
Phase 6 — Chuẩn hóa communication contracts
Mục tiêu

Giao tiếp sạch giữa React, Tauri, Unity, backend.

Kiểu giao tiếp khuyến nghị
React ↔ Backend
HTTP + WebSocket
React ↔ Tauri
Tauri commands/events
Tauri ↔ Unity
local WebSocket hoặc named pipe

Với dự án của bạn, tôi nghiêng về:

Tauri ↔ Unity: local WebSocket
vì debug dễ hơn giai đoạn đầu.
Contract groups
app.lifecycle.*
backend.health.*
chat.*
task.*
settings.*
voice.*
avatar.*
room.*
wardrobe.*
Ví dụ lệnh cho Unity
avatar.setMood
avatar.playEmote
avatar.speakStart
avatar.speakStop
room.focusObject
room.highlightObject
wardrobe.equipItem
Ví dụ event từ Unity
room.objectClicked
avatar.animationFinished
avatar.stateChanged
room.interactionTriggered
Acceptance
mọi giao tiếp đều có schema typed
không có gọi tắt trực tiếp linh tinh
Phase 7 — Chuyển dần chức năng từ Unity UI sang React UI
Mục tiêu

Hoàn tất migration giao diện nghiệp vụ.

Thứ tự chuyển nên là
Settings
Chat
Planner
Reminder
Status panels
Wardrobe panel
Lý do
Settings và Chat dễ chuyển trước
Planner phức tạp hơn
Wardrobe cần Unity preview nên làm sau
Acceptance
người dùng có thể thao tác toàn bộ nghiệp vụ qua React
Unity chỉ còn phần hình ảnh và tương tác 3D
Phase 8 — Gắn ngữ cảnh page với hành vi Unity
Mục tiêu

App cảm giác thống nhất, không bị “React một nơi, Unity một nơi”.

Quy tắc gợi ý
vào page Chat → camera focus avatar
avatar phản ứng khi assistant trả lời
vào page Planner → camera nhìn bàn làm việc hoặc panel phòng
vào page Wardrobe → avatar đứng giữa, đổi đồ realtime
vào page Dashboard → camera room tổng quát
Acceptance
page của React và trạng thái trong Unity ăn khớp nhau
Phase 9 — Packaging một app hoàn chỉnh
Mục tiêu

Phát hành như một ứng dụng desktop duy nhất.

Thành phần đóng gói
Tauri executable
React static assets
Unity build
backend runtime
config
log dir
data dir
Startup flow
mở Tauri app
Tauri start backend
Tauri check health backend
Tauri start Unity
Tauri attach Unity window
React load page mặc định
app ready
Acceptance
user bấm 1 exe
không phải tự chạy backend hay unity bằng tay
Phase 10 — Cleanup triệt để
Mục tiêu

Dọn code cũ để repo sạch.

Việc làm
xóa Unity shell UI cũ
xóa docs cũ mô tả kiến trúc sai
cập nhật scripts packaging
cập nhật runbook
cập nhật task system
khóa rule không cho tái đưa business UI vào Unity
Acceptance
repo không còn 2 kiến trúc song song
Phase 11 — Production hardening
Mục tiêu

App ổn định để dùng lâu dài.

Việc làm
startup diagnostics
crash recovery cho backend và Unity
health panel
retry policy
structured logs
test smoke tự động
packaging validation
offline behavior rõ ràng
Acceptance
app chịu lỗi tốt
dễ debug
dễ maintain
6. Danh sách những gì nên xóa trong Unity

Các phần nên retire dần:

UI Toolkit shell layout
chat panel cũ
planner/schedule screen cũ
settings screen cũ
shell-owned overlays không còn cần thiết
mọi controller sinh ra chỉ để vận hành UI nghiệp vụ

Các phần nên giữ:

avatar runtime
animation system
lip sync
camera
room scene
click interaction
wardrobe application runtime nếu có thể tái dùng
7. Roadmap phát triển tiếp theo cho mobile

Bạn có nói tương lai có thể phát triển mobile.
Với target hiện tại, cách chuẩn bị tốt là:

Giữ backend contract ổn định
Giữ React logic sạch, tách UI với domain
Không để Tauri-specific logic chui vào domain app
Unity runtime nên coi như optional adapter
Về sau mobile có thể:
dùng backend cũ
dùng frontend khác
hoặc dùng Unity/3D theo chiến lược riêng

Nói ngắn gọn:
desktop hiện tại là primary
mobile tương lai không nên chi phối kiến trúc giai đoạn này

8. Thứ tự triển khai thực tế tôi khuyên dùng

Đây là thứ tự ít rủi ro nhất:

Freeze docs + target-state
Dựng Tauri shell
Dựng React/Vite shell
Nối React với backend
Nhúng Unity vào Tauri
Refactor Unity thành runtime 3D
Chuyển UI nghiệp vụ sang React
Đồng bộ page-context với Unity
Package app
Cleanup repo
Hardening
9. Deliverables nên có sau mỗi phase
Sau Phase 0
tài liệu kiến trúc đích
luật migration
Sau Phase 2
Tauri app chạy được
Sau Phase 3
React app có page và điều hướng
Sau Phase 4
Unity embedded trong app
Sau Phase 5
Unity shell UI cũ không còn là bắt buộc
Sau Phase 7
business UI đã sang React
Sau Phase 9
một exe chạy toàn bộ hệ thống
10. Chốt kiến trúc cuối cùng

Tôi chốt cho dự án của bạn như sau:

Tauri là desktop host chính
React/Vite là UI nghiệp vụ chính
Unity chỉ giữ phòng và nhân vật
FastAPI backend được giữ lại
App có một cửa sổ duy nhất
Bên trong app dùng page-based workflow
Unity shell UI cũ bị loại bỏ dần hoàn toàn

Đây là hướng đúng nhất để:

kiến trúc sạch
dễ mở rộng
dễ production hóa
dễ bảo trì
giữ được phần 3D có giá trị nhất của dự án