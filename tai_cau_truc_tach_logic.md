> **Status (2026-04-06):** File này là design notes cho 4 UI modules mới. Mapping sang A-tasks: UI-001→D044(DONE), UI-002/003/004→A43(TODO), UI-005→A28(PARTIAL), UI-006→A30(DONE), UI-007→A09/A31(DOING), UI-008→A32(DONE), UI-009→A10(DOING), UI-010→A33(DONE), UI-011/012→A44(DONE), UI-013→A43(TODO), UI-014→A36(DOING), UI-015→A07(DOING), UI-016→A37(DOING). Task tracker chính là `tasks/task-queue.md`. File này nên được move vào `docs/planning/` trong tương lai.

Dưới đây là kế hoạch code 4 phần UI mới cho dự án hiện tại của bạn, bám theo mục tiêu bạn đã chốt trước:


UI không gian nhân vật
UI lịch trình và quản lý công việc
UI chat với trợ lý
UI thay trang phục và phụ kiện

Tôi sẽ viết theo hướng:

có thể đưa cho agent/Codex chạy
phù hợp với repo hiện tại đang có Unity client shell
không phá kiến trúc bạn vừa làm sạch

Mục tiêu chung

4 phần UI mới không được code kiểu “4 màn hình riêng lẻ”. Chúng phải là 4 module giao diện có thể sống chung trong một shell thống nhất.

Mục tiêu cuối:

chung router
chung design system
chung interaction pattern
tách controller theo module
view chỉ render
logic đi qua service/store
dễ gắn với backend và avatar system

Kiến trúc UI đích

Tôi đề xuất coi 4 UI này là 4 feature surfaces trong cùng một shell:

Character Space
Planner
Chat
Wardrobe

Mỗi phần phải có 4 lớp:

UXML layout
USS style
Controller
ViewModel binding

Cấu trúc nên đi theo hướng này trong Unity:

Assets/Resources/UI/Screens/CharacterSpace/
Assets/Resources/UI/Screens/Planner/
Assets/Resources/UI/Screens/Chat/
Assets/Resources/UI/Screens/Wardrobe/

và tương ứng code:

Assets/Scripts/Features/CharacterSpace/
Assets/Scripts/Features/Planner/
Assets/Scripts/Features/Chat/
Assets/Scripts/Features/Wardrobe/

Nguyên tắc thiết kế trước khi code

4 phần phải dùng chung shell
Không làm 4 app con.
Mỗi phần có controller riêng
Không nhét logic vào AssistantApp hay AppShellController.
Mỗi phần có UXML riêng
Không kéo dài một AppShell.uxml thành file khổng lồ.
Mỗi phần phải có trạng thái loading, empty, error
Để sau này dễ gắn dữ liệu thật.
Chưa cần hoàn thiện feature logic ngay
Ưu tiên dựng UI architecture đúng trước.

Tổng thể layout đề xuất

Shell chính gồm:

top bar
left sidebar
center stage
right utility panel

Mapping 4 UI mới như sau:

Character Space
Center stage: phòng + nhân vật + interaction hotspots
Right panel: thông tin trạng thái, hành động nhanh, nhật ký ngắn
Planner
Center stage: lịch, task list, summary
Right panel: AI suggestions, task detail, action quick panel
Chat
Center stage: conversation thread lớn
Right panel: context cards, memory snippets, quick actions
Wardrobe
Center stage: preview nhân vật
Right panel: danh sách outfit slot, item list, filter, equip actions

Kế hoạch triển khai theo phase

Phase 0. Chốt UI architecture trước khi code

Mục tiêu
Định nghĩa cách 4 phần UI mới cắm vào shell hiện tại.

Việc cần làm

Tạo file:
docs/architecture/ui-modules-plan.md
Chốt:
route names
màn nào dùng center stage thế nào
màn nào dùng right panel thế nào
event nào chuyển route
controller nào sở hữu màn nào
Chốt navigation model:
CharacterSpace
Planner
Chat
Wardrobe
Chốt shared components:
top bar
sidebar
panel header
empty state
loading state
action button row
badge
section card

Deliverable

UI modules plan
navigation map
screen ownership map

Definition of done

có sơ đồ màn hình và controller responsibility rõ

Phase 1. Dựng khung 4 màn hình mới

Mục tiêu
Có đủ 4 màn hình riêng, route được, không cần logic sâu.

Việc cần làm

Tạo 4 UXML root screens:
CharacterSpaceScreen.uxml
PlannerScreen.uxml
ChatScreen.uxml
WardrobeScreen.uxml
Tạo 4 controller:
CharacterSpaceScreenController.cs
PlannerScreenController.cs
ChatScreenController.cs
WardrobeScreenController.cs
Mỗi screen phải có:
header
body
empty/loading/error placeholders
Gắn vào AppRouter
AppShell chỉ chứa slot hiển thị screen hiện tại, không chứa logic riêng của từng screen

Deliverable

4 screen render được
route qua lại được
shell không phình thêm logic

Definition of done

có thể chuyển qua lại 4 UI mới trong app

Kế hoạch chi tiết cho từng phần UI

UI phần 1: Không gian nhân vật

Mục tiêu
Đây là giao diện “hero screen” của sản phẩm. Nó phải làm người dùng thấy đây là trợ lý sống trong một không gian, không phải dashboard bình thường.

Bố cục đề xuất

Center stage:

khu vực phòng
vùng render nhân vật
vùng interaction hotspots
subtitle/assistant state overlay
action dock nhỏ

Right panel:

trạng thái nhân vật
mood/state
activity log ngắn
quick actions:
nói chuyện
mở lịch
thay đồ
tương tác vật thể

Cấu trúc UI nội bộ

Character Space center:

RoomViewport
AvatarStage
InteractionHotspotLayer
CharacterActionDock
SubtitleOverlay

Character Space right panel:

CharacterStatusCard
CurrentActivityCard
QuickActionList
EnvironmentInfoCard

Các component cần code

CharacterSpaceHeader
AvatarViewportCard
InteractionHotspotButton
CharacterQuickActionBar
CharacterStatusBadge
ActivityFeedMini

Controller responsibilities
CharacterSpaceScreenController

bind avatar state
bind room state
bind hotspots
forward quick action events
sync subtitle visibility

Giai đoạn code cho phần này

Step 1
Dựng layout tĩnh phòng + nhân vật placeholder

Step 2
Dựng hotspot layer giả lập

Step 3
Dựng status panel và quick actions

Step 4
Nối với avatar state machine hiện có

Step 5
Chuẩn bị hook cho world interaction sau này

Definition of done

vào màn này thấy rõ phòng, nhân vật, nút tương tác, trạng thái nhân vật
chưa cần AI world simulation hoàn chỉnh
UI phần 2: Lịch trình và quản lý công việc

Mục tiêu
Đây là task/planner hub riêng, không còn chỉ là panel text placeholder như hiện tại.

Bố cục đề xuất

Center stage:

top summary row
task filters
main calendar/list region
quick add bar

Right panel:

AI planner suggestions
selected task details
actions:
complete
reschedule
prioritize
open in chat

Cấu trúc UI nội bộ

Planner center:

PlannerSummaryRow
PlannerFilterTabs
PlannerMainContent
TaskQuickAddBar

Planner main content có 3 mode:

Today
Week
Inbox/Completed

Right panel:

PlannerInsightCard
SelectedTaskDetailCard
TaskActionPanel

Các component cần code

PlannerSummaryCard
TaskFilterTabGroup
TaskListPanel
TaskListItem
TaskEmptyState
TaskQuickAddInput
TaskDetailCard
PlannerInsightPanel

Controller responsibilities
PlannerScreenController

bind task summaries
bind current task view mode
bind selected task
dispatch task actions
giữ planner chỉ là UI surface, không giữ business logic

Giai đoạn code cho phần này

Step 1
Dựng skeleton có summary + filter + list + right detail panel

Step 2
Bind với TaskViewModelStore

Step 3
Tách selected task state riêng

Step 4
Thêm quick add và action bar

Step 5
Thay panel text placeholder hiện tại bằng layout module hóa

Definition of done

planner nhìn như một màn riêng hoàn chỉnh
task list, filters, detail panel tồn tại rõ
không còn cảm giác “text summary tạm bợ”
UI phần 3: Chat với trợ lý

Mục tiêu
Biến chat từ panel phụ thành một surface mạnh hơn, có thể trở thành trung tâm điều phối của assistant.

Bố cục đề xuất

Center stage:

thread chat lớn
composer
transcript preview
assistant state row

Right panel:

context cards
suggested prompts
task actions generated
memory snippets hoặc route diagnostics

Cấu trúc UI nội bộ

Chat center:

ConversationHeader
MessageThread
AssistantStateBar
TranscriptPreview
ChatComposer

Right panel:

ContextSummaryCard
TaskActionResultCard
SuggestedPromptList
MemorySnippetCard

Các component cần code

ChatMessageBubbleUser
ChatMessageBubbleAssistant
ChatThreadView
ChatComposerBar
MicButton
ThinkingStateBar
TranscriptPreviewPanel
SuggestedPromptChip
TaskActionConfirmationCard

Controller responsibilities
ChatScreenController

bind conversation store
bind transcript preview
bind assistant route/state
bind task action confirmations
bind send/mic actions

Giai đoạn code cho phần này

Step 1
Tách chat panel hiện có thành full screen layout

Step 2
Dựng message thread chuẩn

Step 3
Tách composer và transcript preview

Step 4
Thêm context right panel

Step 5
Giữ khả năng embed chat panel nhỏ ở shell nếu cần sau này

Definition of done

chat có thể sống như màn chính, không chỉ panel phụ
đủ chỗ cho voice, transcript, task action confirmations
UI phần 4: Thay trang phục và phụ kiện

Mục tiêu
Tạo giao diện wardrobe rõ ràng, sẵn sàng cho avatar asset pipeline.

Bố cục đề xuất

Center stage:

preview nhân vật lớn
current outfit summary
preview controls

Right panel:

slot selector
item list theo slot
filter/search
equip/unequip/reset/apply

Cấu trúc UI nội bộ

Center:

WardrobeAvatarPreview
CurrentOutfitSummary
PreviewControls

Right:

WardrobeSlotTabs
WardrobeItemGrid
WardrobeItemDetail
WardrobeActionBar

Slots ban đầu:

hair
hair accessory
top
bottom
skirt
socks
shoes
gloves
bracelet left
bracelet right

Các component cần code

WardrobeSlotTab
WardrobeItemCard
WardrobeItemGrid
WardrobeFilterBar
WardrobeItemDetailCard
OutfitSummaryCard
EquipActionBar

Controller responsibilities
WardrobeScreenController

bind selected slot
bind item list của slot
bind selected item
gọi equip/unequip/apply/reset qua wardrobe service
cập nhật preview state

Giai đoạn code cho phần này

Step 1
Dựng preview + slot tabs + item grid

Step 2
Bind với metadata item giả lập hoặc registry thật nếu đã có

Step 3
Thêm selected item detail

Step 4
Thêm action bar equip/reset

Step 5
Chuẩn bị hook để gắn asset registry sau

Definition of done

có thể chuyển slot, xem item, chọn item, xem detail, bấm equip giả lập
sẵn sàng nối vào avatar system

Shared UI system cần làm song song

Để 4 UI nhìn cùng một sản phẩm, cần 1 lớp shared UI:

shared tokens
spacing
radius
card styles
badge styles
text hierarchy
panel styles
shared components
SectionHeader
StatusBadge
PrimaryButton
SecondaryButton
EmptyStateCard
LoadingCard
ErrorCard
InfoCard
shared layout helpers
CenterStageLayout
RightUtilityPanel
SplitPanelContainer
ScreenHeaderRow

Nếu không làm shared layer, 4 phần sẽ rất dễ lệch phong cách và lặp code.

Lộ trình triển khai thực tế

Giai đoạn A. Shared shell foundation

chốt router
chốt screen slots
dựng shared styles/components

Giai đoạn B. 4 screen skeleton

tạo đủ 4 screen
tạo 4 controller
route qua lại được

Giai đoạn C. Planner + Chat trước
Lý do:

2 phần này đã có dữ liệu/backend rõ hơn
nhanh ra kết quả usable

Giai đoạn D. Character Space

dựng visual shell + avatar state binding

Giai đoạn E. Wardrobe

dựng preview + slot system + item list

Thứ tự ưu tiên code

Tôi khuyên làm theo thứ tự này:

Shared UI foundation
Planner UI
Chat UI
Character Space UI
Wardrobe UI

Lý do:

Planner và Chat tận dụng code hiện có nhiều nhất
Character và Wardrobe phụ thuộc avatar/world nhiều hơn

Danh sách task cụ thể cho agent

UI-001
Tạo docs/architecture/ui-modules-plan.md

UI-002
Tạo 4 route mới trong AppRouter

UI-003
Tạo 4 UXML root screens cho CharacterSpace, Planner, Chat, Wardrobe

UI-004
Tạo 4 screen controllers tương ứng

UI-005
Tạo shared style tokens và shared card/button/status components

UI-006
Refactor shell để chỉ chịu trách nhiệm render screen hiện tại và utility panel framing

UI-007
Code Planner screen skeleton với summary, filter, task list, detail panel

UI-008
Bind Planner screen với TaskViewModelStore

UI-009
Code Chat screen skeleton với thread, composer, transcript preview, context panel

UI-010
Bind Chat screen với conversation/chat store hiện có

UI-011
Code Character Space screen skeleton với avatar viewport, hotspot layer, status card, quick actions

UI-012
Bind Character Space với avatar state machine hiện có

UI-013
Code Wardrobe screen skeleton với preview, slot tabs, item grid, item detail

UI-014
Bind Wardrobe với wardrobe mock data hoặc registry nếu đã có

UI-015
Thêm loading/empty/error states cho cả 4 screens

UI-016
Cập nhật docs và task queue cho 4 UI modules mới

Mốc hoàn thành mong muốn

Mốc 1
4 màn hình route được, render được

Mốc 2
Planner và Chat usable

Mốc 3
Character Space có cảm giác sản phẩm rõ

Mốc 4
Wardrobe có workflow chọn slot và item

Mốc 5
4 phần nhìn đồng bộ và có shared design system

Tiêu chí hoàn thành cuối

Xem như đạt nếu:

4 UI mới tồn tại như 4 module rõ ràng
Mỗi UI có controller riêng
Mỗi UI có UXML và USS riêng
Shared shell không bị nhồi logic
Planner và Chat gắn được dữ liệu thật
Character Space gắn được avatar state
Wardrobe gắn được slot/item workflow
Docs và task queue cập nhật đúng