Dưới đây là kế hoạch chi tiết, tập trung đúng 5 điểm bạn muốn xử lý trước. Tôi viết theo hướng có thể dùng luôn để triển khai bằng agent/Codex, nhưng vẫn đủ chặt để bạn kiểm soát kiến trúc.

Mục tiêu tổng thể

Trong 4 phase đầu, mục tiêu không phải thêm tính năng mới. Mục tiêu là làm cho repo:

dễ tách phần
khó bị code dính chéo
docs bám sát code hơn
asset/avatar có chuẩn
agent làm việc có hàng rào rõ ràng

Nguyên tắc xuyên suốt

Không refactor toàn bộ repo trong một lần
Mỗi phase phải có deliverable rõ
Mỗi thay đổi kiến trúc phải đi kèm docs và task queue
Không cho agent tự ý tạo thêm cấu trúc nếu chưa có rule
Tính năng mới chỉ được phép thêm sau khi qua “cổng kiến trúc” của phase tương ứng

Kế hoạch tổng thể theo phase

Phase 0. Đóng băng mở rộng tính năng và tạo nền quản trị thay đổi
Phase 1. Gỡ code dính logic giữa các phần
Phase 2. Tách UI khỏi domain
Phase 3. Đồng bộ docs với repo bằng quy trình bắt buộc
Phase 4. Chuẩn hóa asset/avatar/customization
Phase 5. Thiết lập agent workflow với task queue và rule file cứng

Tôi sẽ đi chi tiết từng phase.

Chi tiết Phase 0: Đóng băng mở rộng tính năng và tạo nền quản trị thay đổi

Mục tiêu
Chặn việc vừa refactor kiến trúc vừa mở rộng roadmap theo kiểu ad-hoc, để các phase sau có nền quản trị thay đổi rõ và truy vết được.

Nguyên tắc áp dụng cho repo hiện tại

Không tạo thêm runtime gốc ngoài `local-backend/` và `unity-client/` trong phase này
Không hứa hẹn cấu trúc tương lai như đã ship nếu repo chưa có thật
Không kéo thêm feature mới vào active queue nếu chưa đi qua task queue và architecture gate
Ưu tiên việc làm rõ source of truth, task protocol, docs update rule, và validation baseline

Cách làm

Bước 0.1: Chốt phạm vi freeze
Trong phase này chỉ cho phép:

tách boundary
đồng bộ docs với code
siết task governance
thêm validation/checklist
sửa lỗi hồi quy
tạo contract placeholder-safe cho phase sau

Chưa cho phép mặc định:

mở rộng feature roadmap
đổi root layout của repo
tạo thêm source of truth cạnh tranh
claim production avatar hay module structure mới nếu chưa có evidence

Bước 0.2: Chốt source of truth hiện tại
Với repo này cần khóa rõ:

runtime backend: `local-backend/`
runtime client: `unity-client/`
quy tắc agent: `AGENTS.md`
phase 0 baseline và governance: `docs/migration/phase0.md`
AI queue hiện hành: `tasks/task-queue.md`
manual/off-repo gate: `tasks/task-people.md`
lịch sử hoàn tất: `tasks/done.md`
quyết định kiến trúc đang dùng: `docs/06-decisions.md`

Bước 0.3: Tạo architecture gate
Một số thay đổi không được agent tự mở rộng nếu chưa cập nhật đủ artefact:

thêm module boundary lớn
đổi persistence strategy
đổi event bus/state ownership
đổi asset structure hoặc avatar slot rule
đổi root path của runtime bắt buộc

Các thay đổi này phải đi kèm:

cập nhật task tracker
cập nhật docs current-state liên quan
cập nhật `docs/06-decisions.md`

Bước 0.4: Chuẩn hóa task protocol
Tạo template task thống nhất để mọi task đều có tối thiểu:

objective
non-goals
in scope
out of scope
files allowed to change
validation steps
docs updates required
acceptance criteria

Khi kết thúc task, agent phải báo được:

đã sửa gì
đã không sửa ngoài scope gì
đã verify bằng gì
docs nào đã cập nhật
còn rủi ro hoặc manual gate nào

Bước 0.5: Chốt baseline evidence trước phase 1
Trước khi bước sang phase 1 phải có một mốc baseline mô tả:

runtime nào là current implementation
blocker/manual gate nào đang tồn tại
docs nào là current-state, docs nào là design target
test/evidence terminal nào đang là mốc mới nhất

Deliverable của phase này

phase 0 baseline doc
freeze rule
architecture gate rule
task template
tracker được cập nhật theo governance mới

Definition of done

Agent không thể mở rộng feature ngoài task đang được theo dõi
Mọi thay đổi kiến trúc đều có đường truy vết qua task + docs + decision log
Repo có baseline rõ để phase 1 trở đi chỉ tập trung vào boundary/refactor, không tranh cãi lại phạm vi

Xử lý vấn đề: Code dính logic giữa các phần

Mục tiêu
Chặn việc chat, planner, avatar, room, settings, persistence gọi trực tiếp lẫn nhau một cách hỗn loạn.

Triệu chứng thường gặp

Component UI gọi thẳng file xử lý business logic
Module chat tự sửa state planner
Avatar customization truy cập trực tiếp local storage hoặc settings
File helper dùng chung quá nhiều, thành “sọt rác logic”
Một màn hình import quá nhiều thứ từ nhiều domain khác nhau

Đích cần đạt
Repo được chia theo domain rõ, mỗi domain có trách nhiệm riêng, giao tiếp qua interface hoặc application service.

Cách làm

Bước 1.1: Vẽ lại domain map của dự án
Tạo một file kiến trúc gốc, ví dụ:
docs/architecture/domain-map.md

Nội dung phải chốt tối thiểu 5 domain:

world-3d
assistant-chat
planner
avatar-customization
shared/system

Mỗi domain cần ghi rõ:

nó sở hữu state gì
nó cung cấp service gì
nó không được làm gì
nó được phép phụ thuộc vào đâu

Ví dụ:

assistant-chat không được import trực tiếp avatar asset registry
planner không được gọi thẳng UI component
world-3d không được lưu file config trực tiếp, chỉ gọi persistence service

Bước 1.2: Audit import và phụ thuộc
Lập bảng kiểm:

file nào import chéo domain
file nào vừa render UI vừa xử lý logic
file nào vừa load asset vừa xử lý state
file nào là “god file”

Phân loại thành 4 mức:

mức A: cần sửa ngay
mức B: sửa trong phase hiện tại
mức C: chờ tách module
mức D: giữ nguyên tạm thời

Bước 1.3: Định nghĩa module boundary
Tạo cấu trúc chuẩn kiểu:

src/app
src/modules/world-3d
src/modules/assistant-chat
src/modules/planner
src/modules/avatar-customization
src/shared

Trong mỗi module nên có:

components/
domain/
application/
infrastructure/
index.ts

Ý nghĩa:

components: UI của module
domain: entity, type, rule nghiệp vụ
application: use-case/service/orchestrator
infrastructure: storage adapter, API adapter, file loader

Bước 1.4: Tạo anti-corruption layer tạm thời
Không đập hết code cũ ngay. Dùng lớp trung gian:

legacy-adapters/
facades/
services/bridge

Mục đích:

giữ app chạy được
di chuyển dần logic cũ sang module mới
giảm nguy cơ vỡ hàng loạt

Bước 1.5: Cấm import chéo trực tiếp
Đặt rule:

module A không import file internal của module B
chỉ import qua public entry index.ts

Ví dụ đúng:
import { createTask } from '@/modules/planner'

Ví dụ sai:
import { internalPlannerStore } from '@/modules/planner/domain/store/internal'

Deliverable của phase này

domain map
dependency rules
danh sách file vi phạm
module skeleton mới
bridge layer cho code cũ

Definition of done

Không còn file “vừa UI vừa business logic vừa persistence”
Không còn import chéo sâu giữa các domain
Mọi module đều có public entry rõ
Xử lý vấn đề: UI và domain chưa tách sạch

Mục tiêu
UI chỉ lo hiển thị và tương tác, domain xử lý nghiệp vụ, application điều phối use-case.

Đây là lỗi rất hay làm project chết chậm:

UI component chứa logic workflow
click button là chạy nguyên chuỗi logic phức tạp
khó test
khó tái sử dụng
đổi giao diện là vỡ logic

Mô hình đề xuất

Tách 3 lớp rõ:

Presentation
Application
Domain

2.1 Presentation layer
Gồm:

page
layout
component hiển thị
hooks UI
animation binding UI

Chỉ được:

nhận props
phát event
gọi application command

Không được:

viết logic nghiệp vụ chính
truy cập storage trực tiếp
sửa domain object tùy tiện

2.2 Application layer
Gồm:

use case
command handler
query service
orchestration

Ví dụ:

sendMessage
createReminder
equipOutfit
moveAvatarToInteractionPoint

Application layer nhận event từ UI, gọi domain service, rồi gọi infrastructure adapter nếu cần.

2.3 Domain layer
Gồm:

entity
value object
rule
validation
state transition rule

Ví dụ:

outfit slot conflict
task status flow
avatar state machine rule
room interaction permission

Cách triển khai

Bước 2.1: Chọn 3 luồng quan trọng nhất để refactor mẫu
Đừng refactor toàn bộ một lúc. Chọn:

gửi chat
tạo/cập nhật task
thay trang phục/phụ kiện

Đây là 3 use-case đại diện cho 3 domain lớn.

Bước 2.2: Với mỗi luồng, tách thành dạng

UI component
application service
domain model
storage adapter

Ví dụ outfit:

AvatarWardrobePanel.tsx
EquipOutfitItem.ts
OutfitRules.ts
WardrobeRepository.ts

Bước 2.3: Loại bỏ side effect khỏi component
Mọi thứ như:

đọc file
ghi config
sửa local db
quyết định business rule
phải rời khỏi component.

Bước 2.4: Dùng DTO/view model rõ ràng
UI không nên dùng trực tiếp entity raw phức tạp.
Tạo:

TaskViewModel
ChatMessageViewModel
AvatarOutfitViewModel

Bước 2.5: Tạo test tối thiểu cho application/domain
Không cần test UI trước. Test:

rule outfit conflict
task transition
message action routing

Deliverable

3 flow refactor mẫu
convention 3 lớp
component guideline
application service guideline
test nền cho business logic

Definition of done

3 use-case chính không còn business logic nằm trong UI
UI có thể thay đổi mà không phải viết lại rule nghiệp vụ
Xử lý vấn đề: Docs dễ lệch repo

Mục tiêu
Biến docs thành một phần của quá trình phát triển, không phải tài liệu viết sau.

Gốc rễ vấn đề
Docs lệch vì:

không có file nào là nguồn sự thật chính
agent sửa code nhưng không sửa docs
docs quá dài, quá chung
không có checklist cập nhật docs khi merge

Cách làm

Bước 3.1: Thiết lập “documentation hierarchy”
Chia docs thành 4 tầng:

README.md
Giới thiệu dự án, cách chạy, cấu trúc cấp cao
docs/architecture/
Sơ đồ module, dependency rules, patterns
docs/features/
Tài liệu theo tính năng/domain
docs/operations/
Quy trình dev, release, agent workflow, task queue, coding rules

Bước 3.2: Chỉ định source of truth cho từng loại thông tin
Ví dụ:

cấu trúc module: docs/architecture/module-boundaries.md
quy tắc agent: AGENTS.md
task đang làm: docs/project/task-queue.md
trạng thái roadmap: docs/project/roadmap.md
asset format: docs/features/avatar-asset-spec.md

Không để cùng một thông tin xuất hiện ở 4 chỗ khác nhau.

Bước 3.3: Áp dụng “docs changed with code”
Rule cứng:
Mọi PR thay đổi một trong các mục sau bắt buộc cập nhật docs:

cấu trúc thư mục
luồng tính năng
interface giữa module
asset spec
command agent / workflow

Bước 3.4: Tạo file changelog kiến trúc
Ví dụ:
docs/architecture/adr/
Mỗi quyết định lớn có 1 file:

tại sao chọn cách này
lựa chọn bị loại bỏ
tác động

Ví dụ:

ADR-001-module-boundaries.md
ADR-002-avatar-slot-system.md
ADR-003-agent-task-queue-governance.md

Bước 3.5: Tạo doc audit checklist
Mỗi lần agent làm xong phải tự kiểm:

README có bị lỗi thời không
feature doc có đúng flow không
architecture doc có đúng module mới không
task queue có cập nhật trạng thái không

Deliverable

hệ phân tầng docs
source of truth map
ADR folder
doc update checklist
rule docs bắt buộc theo PR/task

Definition of done

Không còn tài liệu trùng vai trò
PR/task nào đổi kiến trúc đều phải cập nhật docs tương ứng
Agent không thể kết thúc task mà bỏ qua docs
Xử lý vấn đề: Asset/avatar/customization dễ thành mớ hỗn hợp nếu không chuẩn hóa

Mục tiêu
Biến avatar system thành một hệ thống có spec rõ, không phải bộ sưu tập file mesh/material/prefab rời rạc.

Rủi ro nếu không chuẩn hóa

file asset đặt tên lộn xộn
thiếu metadata cho item
không rõ item nào dành cho slot nào
model thay đổi skeleton làm hỏng animation
outfit chồng sai layer
phụ kiện không đồng bộ anchor point
agent thêm asset nhưng phá pipeline

Cách làm

Bước 4.1: Thiết kế asset contract
Tạo tài liệu:
docs/features/avatar-asset-spec.md

Trong đó định nghĩa:

naming convention
folder convention
slot system
metadata schema
versioning
preview/render rule
dependency giữa item

Ví dụ slot:

hair
hair_accessory
top
bottom
skirt
socks
shoes
gloves
bracelet_left
bracelet_right

Bước 4.2: Chuẩn hóa thư mục asset
Ví dụ:

assets/avatar/base/
assets/avatar/outfits/
assets/avatar/accessories/
assets/avatar/materials/
assets/avatar/icons/
assets/avatar/registry/

Bước 4.3: Mỗi asset phải có metadata
Ví dụ JSON hoặc TS object:

id
displayName
category
slot
compatibleBodyType
requiredBaseVersion
conflictsWith
previewIcon
modelPath
materialSet
tags

Không cho phép asset “chỉ có file mà không có metadata”.

Bước 4.4: Tạo avatar registry
Một nơi duy nhất quản lý asset đã đăng ký:

không scan tự do khắp repo
không để UI đọc trực tiếp folder

Ví dụ:
AvatarAssetRegistry.ts

Nó chịu trách nhiệm:

liệt kê item hợp lệ
validate metadata
trả item theo slot
kiểm tra conflict

Bước 4.5: Chuẩn hóa outfit rule
Cần rule rõ:

item nào loại trừ nhau
layer render
fallback khi thiếu asset
xử lý item không tương thích
equip atomic hay partial

Ví dụ:

mặc full dress thì vô hiệu hóa top + bottom
tóc dài có thể conflict với áo cổ cao
giày đặc biệt yêu cầu loại tất tương thích

Bước 4.6: Tạo pipeline nhập asset mới
Mỗi asset mới phải đi qua:

kiểm tra naming
kiểm tra metadata
kiểm tra slot
kiểm tra skeleton/anchor
render preview
đăng ký vào registry
update docs

Deliverable

asset spec
registry system
metadata schema
slot and conflict rules
asset intake checklist

Definition of done

Không còn asset “vô danh”
Không còn UI truy cập asset tùy tiện
Avatar customization chạy qua registry và rules
Xử lý vấn đề: Dùng agent mà thiếu task queue và rule file sẽ làm kiến trúc xấu nhanh

Mục tiêu
Biến agent từ “thợ code tự phát” thành “executor trong hệ thống có luật”.

Đây là điểm cực quan trọng với workflow của bạn.

Vấn đề gốc
Nếu không có task queue + rule file:

agent sửa lan
tạo file trùng
docs không cập nhật
cấu trúc repo drift
bug fix làm hỏng kiến trúc
context ngày càng bẩn

Cách làm

Bước 5.1: Tạo AGENTS.md mạnh và ngắn gọn
Không viết lan man. Chỉ viết rule thi hành.

AGENTS.md nên có các phần:

mục tiêu repo
module boundaries
những gì bị cấm
quy trình trước khi code
quy trình sau khi code
quy tắc docs
quy tắc task queue
done criteria

Các rule cứng nên có:

luôn đọc task hiện tại trước khi sửa
không sửa ngoài phạm vi task nếu không ghi rõ
không tạo file mới nếu đã có file đúng vai trò
không import chéo domain trái quy định
mọi thay đổi kiến trúc phải cập nhật docs liên quan
mọi task phải cập nhật trạng thái trước và sau khi làm

Bước 5.2: Tạo task queue chuẩn
Dùng file như:
docs/project/task-queue.md

Mỗi task có:

id
title
type
scope
files expected
constraints
dependencies
acceptance criteria
docs to update
status

Status chuẩn:

backlog
ready
in_progress
blocked
review
done

Bước 5.3: Tách task theo kích thước nhỏ
Agent không nên nhận task mơ hồ kiểu:
“refactor whole app”

Nên tách kiểu:

TSK-001 tạo module skeleton
TSK-002 chuyển planner logic sang application layer
TSK-003 tạo avatar asset registry
TSK-004 viết architecture docs cho module boundary
TSK-005 cấm import chéo và sửa vi phạm mức A

Bước 5.4: Tạo template task cho agent
Mỗi task phải có template cứng:

objective
non-goals
in-scope
out-of-scope
files allowed to change
expected outputs
validation steps
docs updates required

Bước 5.5: Tạo completion protocol
Khi agent kết thúc task phải báo:

đã sửa file nào
đã không sửa file nào ngoài scope
rule nào đã tuân thủ
docs nào đã cập nhật
còn nợ gì
rủi ro gì còn lại

Bước 5.6: Tạo “architecture gate”
Một số thay đổi phải qua gate, không cho agent tự làm rộng:

thêm module mới
đổi cấu trúc asset
đổi persistence strategy
đổi event bus / state model
đổi wardrobe slot spec

Các thay đổi này phải yêu cầu:

cập nhật ADR
cập nhật docs kiến trúc
cập nhật task queue

Deliverable

AGENTS.md
task queue
task template
completion checklist
architecture gate rules

Definition of done

Agent không làm việc ngoài task
Mọi thay đổi đều truy vết được
Repo không bị xấu nhanh do AI sửa tùy hứng
