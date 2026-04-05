> **Status (2026-04-06):** File này là design notes cho hệ thống Room World. Tất cả 15 ROOM tasks (ROOM-001 đến ROOM-015) đã DONE — xem D045 đến D053 và D054 trong `tasks/done.md`. Task tracker chính là `tasks/task-queue.md` (A44) và `tasks/module-migration-backlog.md`. File này nên được move vào `docs/planning/` trong tương lai.

Dưới đây là bản kế hoạch để biến “không gian nhân vật” thành một căn phòng thật sự, có đồ vật 3D, có cấu trúc rõ, và đủ tốt để sau này nhân vật có thể tương tác trong phòng mà không làm dự án rối.

Mục tiêu tổng thể

Phần Character Space hiện tại sẽ được nâng cấp thành một hệ world-room 3D với các đặc điểm:

Có một căn phòng 3D cố định
Có các mô hình 3D làm đồ vật trong phòng
Có hệ quản lý object trong phòng
Có vị trí, loại, trạng thái và metadata cho từng đồ vật
Có lớp interaction để sau này nhân vật hoặc người dùng tương tác
Không nhét logic world vào UI controller
Tách asset pipeline và room logic ngay từ đầu để không thành mớ hỗn hợp

Kết quả mong muốn sau cùng

Người dùng mở Character Space sẽ thấy:

một căn phòng 3D hoàn chỉnh
nhân vật đứng trong phòng
các đồ vật như bàn, ghế, giường, đèn, kệ sách, máy tính, chậu cây
các điểm tương tác cơ bản
camera ổn định
layout phòng rõ ràng
có thể mở rộng về sau thành:
nhân vật đi tới vật thể
click vật thể
trigger animation
thay skin/phòng
thêm vật thể mới dễ dàng

Nguyên tắc kiến trúc

Trước khi code, phải chốt 5 nguyên tắc:

Phòng là một subsystem, không phải chỉ là scene trang trí
Đồ vật phải được quản lý bằng registry/metadata, không phải kéo prefab thủ công vô tội vạ
UI Character Space chỉ hiển thị và điều phối, không chứa world logic
Avatar và room phải tách nhưng có bridge giao tiếp
Asset 3D phải có pipeline nạp vào rõ ràng

Kiến trúc đích

Nên tách phần này thành 4 lớp lớn:

Room World
Quản lý căn phòng, layout, object placement, camera anchors, navigation zones
Room Objects
Quản lý đồ vật 3D, prefab, metadata, interaction hooks, state
Character Integration
Nối nhân vật với căn phòng, vị trí spawn, facing direction, interaction target, activity
Character Space UI
Phần overlay UI của màn Character Space: subtitle, action dock, selected object info, mini controls

Cấu trúc thư mục đề xuất

Unity code:

Assets/Scripts/Features/CharacterSpace/
Assets/Scripts/World/Room/
Assets/Scripts/World/Objects/
Assets/Scripts/World/Interaction/
Assets/Scripts/World/Camera/

Unity assets:

Assets/World/Rooms/
Assets/World/Objects/Furniture/
Assets/World/Objects/Decor/
Assets/World/Objects/Interactive/
Assets/World/Materials/
Assets/World/Prefabs/
Assets/World/Registry/

Docs:

docs/features/room-world-plan.md
docs/features/room-object-spec.md

Phân chia subsystem chi tiết

Room World subsystem

Trách nhiệm:

load căn phòng
quản lý room root
spawn object theo config
quản lý zones
quản lý spawn points
quản lý camera anchors
quản lý light preset nếu cần

Các class chính:

RoomWorldController
RoomLayoutDefinition
RoomZone
RoomSpawnPoint
RoomSceneBootstrap
RoomCameraAnchorRegistry
Room Objects subsystem

Trách nhiệm:

định nghĩa object types
quản lý prefab mapping
metadata cho object
object placement
object state
interaction capability

Các class chính:

RoomObjectRegistry
RoomObjectDefinition
RoomObjectInstance
RoomObjectAnchor
RoomObjectState
RoomObjectFactory
Interaction subsystem

Trách nhiệm:

click/chọn object
hover/highlight object
interaction availability
object action dispatch
interaction event bridge sang UI hoặc character

Các class chính:

RoomInteractionController
InteractableObject
InteractionAction
SelectedRoomObjectStore
ObjectHighlightController
Character Integration subsystem

Trách nhiệm:

đặt nhân vật vào phòng
xác định vị trí đứng
liên kết trạng thái nhân vật với room activity
chuẩn bị hook cho đi lại và interaction animation

Các class chính:

CharacterRoomBridge
CharacterSpawnResolver
CharacterActivityController
CharacterLookAtTarget
CharacterInteractionBridge
Character Space UI overlay

Trách nhiệm:

hiển thị subtitle
action dock
selected object info
room mode controls
environment status nhẹ

Các class chính:

CharacterSpaceScreenController
RoomObjectInfoPanelController
CharacterActionDockController
EnvironmentOverlayController

Kế hoạch triển khai theo phase

Phase 0. Chốt design và ranh giới

Mục tiêu
Không code trước khi chốt căn phòng sẽ hoạt động thế nào trong kiến trúc hiện tại.

Việc cần làm

Tạo tài liệu:
docs/features/room-world-plan.md

Trong đó chốt:

mục tiêu của room system
room là scene riêng hay nằm trong Character Space scene
object được quản lý bằng registry hay kéo tay
character đứng ở đâu
camera kiểu gì
UI overlay nào nằm trên room
Tạo tài liệu:
docs/features/room-object-spec.md

Trong đó chốt:

object categories
prefab rules
metadata rules
naming rules
interaction capability flags
Chốt MVP room
Đừng tham quá. Chỉ nên lấy 1 phòng cơ bản:
sàn
tường
cửa sổ hoặc tranh
giường hoặc sofa
bàn
ghế
đèn
kệ
máy tính hoặc laptop
chậu cây

Deliverable

room-world-plan.md
room-object-spec.md
danh sách object MVP

Definition of done

có spec rõ trước khi nhập asset hoặc dựng prefab

Phase 1. Dựng room foundation

Mục tiêu
Có một căn phòng 3D tối thiểu, chưa cần tương tác sâu.

Việc cần làm

Tạo room root prefab:
Room_Base.prefab
Tạo cấu trúc room:
floor
wall left/right/back
ceiling tùy chọn
decor anchors
furniture anchors
avatar spawn point
camera anchor
Tạo class:
RoomWorldController
RoomSceneBootstrap
Tạo room definition data:
RoomLayoutDefinition
Camera:
dựng 1 góc camera đẹp cho Character Space
có option chuyển nhẹ sang follow/inspect mode sau này

Deliverable

căn phòng cơ bản hiển thị được
nhân vật có chỗ spawn rõ
camera ổn định

Definition of done

vào Character Space thấy phòng thật sự, không còn chỉ là nền trống

Phase 2. Xây object pipeline cho đồ vật 3D

Mục tiêu
Đồ vật phải được đưa vào phòng qua pipeline rõ, không kéo prefab tùy hứng.

Việc cần làm

Tạo object categories:
furniture
decor
interactive
lighting
utility
Tạo metadata schema cho object:
id
displayName
category
prefabPath
defaultScale
anchorType
interactionType
tags
optionalStates
Tạo registry:
RoomObjectRegistry
Tạo factory:
RoomObjectFactory
Tạo placement data:
object nào đặt ở anchor nào
rotation
scale override nếu có
Chốt naming convention
Ví dụ:
OBJ_Furniture_Desk_Wood_A01
OBJ_Decor_Plant_Small_A01
OBJ_Interactive_Laptop_A01

Deliverable

object registry
object definition format
factory spawn object từ config

Definition of done

thêm một đồ vật mới không cần sửa lung tung nhiều file

Phase 3. Populate phòng bằng đồ vật 3D

Mục tiêu
Đặt các mô hình 3D thật vào phòng.

Việc cần làm

Chọn bộ object MVP:
giường hoặc sofa
bàn
ghế
đèn
laptop
kệ sách
chậu cây
tranh
tủ nhỏ
Import asset 3D
Chuẩn hóa scale, pivot, material cơ bản
Tạo prefab cho từng object
Đăng ký vào registry
Spawn qua RoomWorldController

Nguyên tắc placement

không đặt tay trực tiếp trong scene cho toàn bộ object
scene chỉ giữ anchors và root
object chính nên sinh qua definition/config

Deliverable

phòng đủ đồ vật cơ bản
object placement nhìn tự nhiên
không rối hierarchy

Definition of done

phòng nhìn có hồn và có đủ vật thể để gọi là phòng sống

Phase 4. Thêm interaction layer cơ bản

Mục tiêu
Cho room object có thể được chọn và phản hồi cơ bản.

Việc cần làm

Tạo InteractableObject
Mỗi object có:
selectable
hoverable
interactable
inspectable
Tạo RoomInteractionController
Tạo hệ selected object store
Tạo highlight nhẹ khi hover/chọn
Tạo action cơ bản:
inspect
focus camera
trigger character intent sau này

Ví dụ object MVP nên có interaction:

laptop
bàn
giường/sofa
đèn
kệ sách

Deliverable

click object được
object được highlight
UI overlay hiện selected object info

Definition of done

room không còn là nền tĩnh, đã có cảm giác tương tác được

Phase 5. Nối room với nhân vật

Mục tiêu
Nhân vật phải “thuộc” căn phòng chứ không phải đứng chồng lên một background.

Việc cần làm

Tạo CharacterRoomBridge
Chốt:
avatar spawn point
facing direction
idle zone
interaction target point
Khi chọn object:
character có thể quay mặt về object
hoặc ít nhất đổi state “attention”
Chuẩn bị hook cho tương lai:
move to object
sit on chair
use laptop
lie on bed
inspect shelf
Đồng bộ avatar state với room context:
idle
interact_ready
inspecting
talking

Deliverable

nhân vật spawn đúng trong phòng
room và avatar có liên kết logic ban đầu

Definition of done

nhìn nhân vật có cảm giác là sống trong căn phòng, không phải model dán lên nền

Phase 6. Nâng cấp Character Space UI overlay để phục vụ room

Mục tiêu
UI của Character Space phải hỗ trợ room, không chỉ hỗ trợ avatar.

Việc cần làm

Thêm overlay nhẹ:
selected object info
room action dock
current activity
mode toggle nếu có
Khi chọn object:
hiện tên object
loại object
trạng thái
action gợi ý
Quick actions:
đi tới
xem
dùng
quay lại nhân vật
ẩn hiện hotspot
Giữ UI mỏng
Không che mất phòng.

Deliverable

Character Space overlay hoàn chỉnh hơn
object info panel
action dock phù hợp room

Definition of done

room và UI hỗ trợ lẫn nhau thay vì tách rời

Phase 7. Chuẩn hóa asset pipeline để mở rộng lâu dài

Mục tiêu
Tránh tương lai thêm nhiều object rồi loạn.

Việc cần làm

Tạo intake checklist cho object mới:
model import
scale check
pivot check
material check
collider check
prefab creation
registry entry
interaction flags
docs update
Tạo validator script nếu cần
Tạo room object spec cố định
Chốt room style guide:
tỷ lệ object
màu sắc
độ sáng
mức chi tiết

Deliverable

asset intake flow
object onboarding rule
docs chuẩn hóa

Definition of done

thêm object mới vào phòng không tạo hỗn loạn

Danh sách vật thể MVP nên có

Tôi khuyên căn phòng đầu tiên nên dùng các object này:

Nhóm cấu trúc

floor
walls
window
rug

Nhóm furniture

bed hoặc sofa
desk
chair
side table
shelf

Nhóm decor

plant
wall painting
lamp
books
small box hoặc decoration props

Nhóm interactive

laptop
lamp switch giả lập
bookshelf
seat object
wardrobe closet giả lập

Bạn chưa cần làm quá nhiều object ngay. Một phòng đẹp với 8 đến 12 object tốt hơn phòng đầy 30 object nhưng rối.

Cấu trúc dữ liệu object đề xuất

Ví dụ cho một object:

id: desk_main_01
displayName: Bàn làm việc
category: furniture
prefabKey: OBJ_Furniture_Desk_Wood_A01
anchorType: desk_anchor
interactionType: inspect
selectable: true
hoverable: true
tags: ["work","desk","main"]

Về sau có thể thêm:

defaultAnimationHint
preferredCharacterPosition
cameraFocusOffset
stateFlags

Kế hoạch code cụ thể cho agent

ROOM-001
Tạo docs/features/room-world-plan.md

ROOM-002
Tạo docs/features/room-object-spec.md

ROOM-003
Tạo thư mục Assets/World/ và cấu trúc room/object/prefab/registry

ROOM-004
Tạo RoomLayoutDefinition, RoomWorldController, RoomSceneBootstrap

ROOM-005
Tạo Room_Base.prefab với floor, walls, anchors, spawn points, camera anchors

ROOM-006
Tạo RoomObjectDefinition, RoomObjectRegistry, RoomObjectFactory

ROOM-007
Import và chuẩn hóa bộ object MVP đầu tiên

ROOM-008
Tạo prefab cho từng object MVP và đăng ký registry

ROOM-009
Spawn object qua config thay vì kéo tay toàn bộ vào scene

ROOM-010
Tạo InteractableObject và RoomInteractionController

ROOM-011
Tạo highlight/select flow và selected object store

ROOM-012
Tạo CharacterRoomBridge để nối avatar với room spawn và attention target

ROOM-013
Nâng cấp Character Space overlay cho selected object info + room action dock

ROOM-014
Tạo asset intake checklist và validator rule cho object mới

ROOM-015
Cập nhật docs + task queue + AGENTS theo room subsystem mới

Rủi ro cần tránh

Kéo object tay vào scene quá nhiều
Sau này thay đổi rất mệt.
Không có registry
Thêm object mới sẽ loạn naming và logic.
Để UI controller xử lý interaction world
Sẽ làm Character Space controller phình rất nhanh.
Không chuẩn hóa scale/pivot/material
Phòng sẽ trông giả và khó xài.
Làm quá nhiều interaction từ đầu
Nên bắt đầu từ select/inspect trước.
Trộn avatar logic với room logic
Sau này thay avatar hoặc đổi room sẽ rất đau.

Thứ tự ưu tiên thực hiện

Tôi khuyên làm theo thứ tự này:

Room foundation
Object registry
Object population
Interaction layer
Character bridge
UI overlay
Asset pipeline hardening

Mốc hoàn thành

Mốc 1
Có phòng trống + nhân vật spawn đúng

Mốc 2
Có 8 đến 12 đồ vật 3D trong phòng

Mốc 3
Có click/select object và overlay info

Mốc 4
Có liên kết cơ bản giữa nhân vật và đồ vật

Mốc 5
Có pipeline đủ sạch để tiếp tục mở rộng