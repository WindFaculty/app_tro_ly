Dưới đây là bản kế hoạch chi tiết theo đúng hướng bạn vừa chốt:

Bỏ local-backend ra khỏi phạm vi tích hợp.
Toàn bộ phần còn lại được gom về dưới ai-dev-system/.
docs/ và tasks/ vẫn giữ ở ngoài cùng repo như lớp governance.
Mục tiêu là biến ai-dev-system thành lõi kỹ thuật duy nhất cho phần non-backend của dự án.

Tôi đang hiểu yêu cầu này theo nghĩa mạnh nhất: đây không phải chỉnh cây thư mục cho đẹp, mà là đổi kiến trúc để mọi phần non-backend đều quy tụ về một integration root duy nhất.

1. Đích kiến trúc sau khi tái cấu trúc

Sau khi hoàn tất, repo nên có hình dạng gần như sau:

app_tro_ly/
├─ ai-dev-system/                # lõi kỹ thuật non-backend duy nhất
│  ├─ control-plane/             # GUI agent, MCP, orchestration, profiles
│  ├─ clients/
│  │  └─ unity-client/           # Unity app được hấp thụ vào đây
│  ├─ domain/
│  │  ├─ avatar/
│  │  ├─ customization/
│  │  ├─ room/
│  │  └─ shared/
│  ├─ context/                   # prompt/context/rules cục bộ cho subsystem
│  ├─ workflows/                 # workflow automation, task spec, demo flows
│  ├─ tools/                     # tool scripts, validators, import/export
│  ├─ asset-pipeline/            # chuẩn hóa avatar, clothing, imports
│  ├─ workbench/                 # Blender, Clothy3D, Meshy imports, lab area
│  ├─ scripts/                   # setup, validate, run, package
│  ├─ tests/                     # test orchestration cấp subsystem
│  ├─ logs/                      # run artifacts
│  └─ dist/                      # output đóng gói nếu cần
├─ docs/                         # ở ngoài
├─ tasks/                        # ở ngoài
├─ AGENTS.md                     # rule gốc toàn repo
├─ lessons.md                    # lessons gốc
└─ local-backend/                # ngoài phạm vi lần này, không đụng
2. Ý nghĩa của kiến trúc mới

Kiến trúc mới phải đạt 4 điều:

ai-dev-system trở thành nơi duy nhất chứa code, tool, asset pipeline và workflow vận hành cho phần non-backend.
unity-client không còn là sibling root độc lập, mà trở thành một client module bên trong ai-dev-system.
Các phần như ai/, tools/, bleder/, Clothy3D_Studio/, import asset folders, release helpers đều được quy về các khu vực hợp lý bên trong ai-dev-system.
docs/ và tasks/ đứng ngoài để giữ vai trò “bản đồ + governance”, không bị trộn với code.
3. Mapping thư mục hiện tại sang cấu trúc mới
3.1 Những thứ nên được hấp thụ trực tiếp
Hiện tại	Đích mới
unity-client/	ai-dev-system/clients/unity-client/
ai/	ai-dev-system/context/
tools/	ai-dev-system/tools/ hoặc ai-dev-system/asset-pipeline/tools/
bleder/	ai-dev-system/workbench/blender/
Clothy3D_Studio/	ai-dev-system/workbench/clothy3d/
Meshy_AI_.../	ai-dev-system/workbench/imports/meshy/...
release/	ai-dev-system/dist/ hoặc ai-dev-system/release/
3.2 Những thứ không nên nhét bừa vào cùng một chỗ
Code runtime và workbench asset không được trộn chung.
Script build và script asset pipeline không được trộn chung.
Prompt/context phục vụ AI coding không được trộn vào runtime Unity.
Logs và generated artifacts phải ở vùng riêng.
3.3 Những thứ giữ ngoài
docs/
tasks/
AGENTS.md
lessons.md
local-backend/ vì bạn đã yêu cầu bỏ qua
4. Nguyên tắc tích hợp

Đây là phần quan trọng nhất để tránh sau này ai-dev-system thành một “sọt rác lớn”.

4.1 Một lõi, nhiều vành

Bên trong ai-dev-system, chia thành 4 vành rõ ràng:

control-plane/
Chứa orchestration, agent runtime, MCP, profile registry, verification, healing, automation.
clients/
Chứa các client thật như Unity app.
domain/
Chứa avatar, customization, room, shared contracts, dữ liệu domain.
workbench/ và asset-pipeline/
Chứa tool authoring, Blender, Clothy3D, imports, sample assets, preprocess scripts.
4.2 Mọi thứ đều phải có ownership rõ

Ví dụ:

Unity UI và gameplay shell thuộc clients/unity-client/
Avatar runtime contract thuộc domain/avatar/
Equip rules thuộc domain/customization/
Import converter FBX/texture thuộc asset-pipeline/
GUI automation và Unity MCP thuộc control-plane/
4.3 Không merge chỉ vì “đều là AI”

ai-dev-system phải là integration root theo kiến trúc, không phải thư mục gom mọi thứ có chữ “AI”.

5. Kế hoạch chi tiết theo phase
Phase 0 — Freeze, audit, và chốt ranh giới
Mục tiêu

Khóa hiện trạng để tránh vừa chuyển vừa lệch sự thật.

Việc cần làm
Chụp baseline toàn bộ phần non-backend hiện tại.
Lập inventory cho từng thư mục root ngoài local-backend/.
Gắn nhãn từng thư mục theo 1 trong 4 loại:
absorb vào ai-dev-system
wrap bằng adapter tạm
archive/workbench
delete sau khi di trú
Viết doc baseline mới trong docs/migration/.
Kết quả đầu ra
Một file baseline mô tả:
cái gì đang là current source of truth
cái gì là optional
cái gì là lab/workbench
Một sơ đồ mapping root cũ sang root mới.
Acceptance

Không còn chỗ nào mơ hồ kiểu “có thể thư mục này còn dùng”.

Phase 1 — Thiết kế kiến trúc đích cho ai-dev-system
Mục tiêu

Biến ai-dev-system từ subsystem phụ thành integration root chính.

Việc cần làm
Tách nội bộ ai-dev-system thành các lớp:
control-plane/
clients/
domain/
context/
asset-pipeline/
workbench/
scripts/
tests/
Viết ai-dev-system/README.md mới theo góc nhìn “main non-backend system”.
Tạo ai-dev-system/AGENTS.md cục bộ cho subsystem.
Giữ root AGENTS.md ngắn hơn, chỉ còn rule ở cấp repo và đường dẫn truth sources.
Kết quả đầu ra

Một kiến trúc đích rõ, trước khi có bất kỳ move lớn nào.

Acceptance

Chỉ cần nhìn ai-dev-system/README.md là hiểu subsystem này bây giờ là trung tâm non-backend.

Phase 2 — Hấp thụ lớp AI governance và context
Mục tiêu

Đưa lớp prompt/context phụ trợ vào đúng vị trí trong ai-dev-system, nhưng vẫn giữ governance repo ở root.

Việc cần làm
Chuyển ai/CONTEXT_SUMMARY.md vào ai-dev-system/context/.
Nếu có prompt files rời rạc, gom vào:
ai-dev-system/context/prompts/
ai-dev-system/context/summaries/
ai-dev-system/context/policies/
Thiết kế cơ chế reference từ root docs sang context mới.
Tách rõ:
repo governance ở root
subsystem context trong ai-dev-system/context/
Không làm ở phase này
Không move tasks/
Không move docs/
Không đổi logic runtime
Acceptance

Mọi file context AI đều có nơi ở hợp lý trong ai-dev-system, nhưng repo governance vẫn còn ngoài.

Phase 3 — Hấp thụ unity-client vào ai-dev-system/clients/
Mục tiêu

Xóa tình trạng unity-client/ là một root song song.

Việc cần làm
Tạo ai-dev-system/clients/unity-client/.
Di trú toàn bộ unity-client/ vào đây.
Chuẩn hóa các path trong:
scripts
test runners
README
docs
CI/local validation
Tạo lớp adapter tạm cho những script cũ còn gọi path unity-client/.
Tách lại ownership:
UI layout thuộc client
shared contracts thuộc domain/shared/ hoặc control-plane/contracts/
avatar shell bridge thuộc domain/avatar/ hoặc clients/unity-client/integration/
Rủi ro

Unity rất nhạy với path, asset database, scene references, package paths.

Cách giảm rủi ro
Move theo bước nhỏ
Rerun EditMode/PlayMode sau mỗi cụm
Có shim path tạm trong scripts
Không rename sâu bên trong Unity tree quá sớm
Acceptance

Unity client chạy được từ vị trí mới, scripts hiện hành không vỡ hàng loạt.

Phase 4 — Tái cấu trúc ai-dev-system thành control plane thống nhất
Mục tiêu

Hợp nhất GUI agent, Unity MCP workflow, Unity hybrid profile và orchestration vào một control plane rõ ràng.

Việc cần làm
Đổi ai-dev-system/app/ thành phần rõ nghĩa hơn, ví dụ:
ai-dev-system/control-plane/app/
hoặc ai-dev-system/control-plane/runtime/
Tách module con:
automation/gui/
automation/unity/
planner/
verifier/
healing/
profiles/
capabilities/
Đồng nhất API giữa GUI profile và Unity profile:
plan
preflight
execute
verify
recover
summarize
Hợp nhất artifact schema:
screenshot
control tree
failure report
unity summary
healing trace
Chuẩn hóa capability matrix theo định dạng chung.
Mục tiêu sâu hơn

unity-editor không còn là profile đặc biệt bị “cắm thêm”, mà là một profile hạng nhất trong control plane chung.

Acceptance

Toàn bộ agent/tooling có vòng đời thống nhất thay vì mỗi nhánh một kiểu.

Phase 5 — Hấp thụ avatar, customization, room và asset pipeline
Mục tiêu

Đưa toàn bộ phần avatar và nội dung 3D vào một domain rõ ràng.

Cấu trúc đề xuất
ai-dev-system/domain/
├─ avatar/
│  ├─ runtime/
│  ├─ contracts/
│  ├─ state/
│  └─ validators/
├─ customization/
│  ├─ wardrobe/
│  ├─ presets/
│  ├─ rules/
│  └─ sample-data/
├─ room/
│  ├─ layout/
│  ├─ interactions/
│  └─ item-registry/
└─ shared/
   ├─ events/
   ├─ models/
   └─ contracts/
Việc cần làm
Chuyển logic avatar shared ra khỏi chỗ dính chặt vào Unity shell nếu có thể.
Chuẩn hóa equip categories:
hair
hair accessory
top
pants
skirt
socks
shoes
gloves
bracelets
Định nghĩa item manifest chuẩn.
Tạo validation pipeline cho:
skeleton compatibility
blendshape expectations
material slot expectations
prefab import rules
Tách sample assets khỏi production assets.
Acceptance

Avatar/customization không còn nằm rải rác giữa Unity client, tools, Blender folders và docs.

Phase 6 — Tổ chức lại tools, bleder, Clothy3D, imports thành workbench chuẩn
Mục tiêu

Biến vùng asset lab thành workbench có quy tắc, không phải đống thư mục thí nghiệm.

Cấu trúc đề xuất
ai-dev-system/workbench/
├─ blender/
├─ clothy3d/
├─ imports/
│  ├─ meshy/
│  ├─ raw-fbx/
│  └─ textures/
├─ conversion/
├─ validation/
└─ reports/
Việc cần làm
Di trú bleder/ vào workbench/blender/.
Di trú Clothy3D_Studio/ vào workbench/clothy3d/.
Di trú các asset import source vào workbench/imports/.
Tạo naming convention chuẩn:
raw
cleaned
validated
export-ready
Viết script converter/validator trong asset-pipeline/.
Nguyên tắc
Workbench là vùng authoring
domain/ là vùng contract và metadata
clients/unity-client/ chỉ nhận output đã chuẩn hóa
Acceptance

Không còn chuyện Unity client ăn trực tiếp asset source bẩn hoặc import folder ngẫu nhiên.

Phase 7 — Chuẩn hóa scripts, tests, packaging quanh ai-dev-system
Mục tiêu

Toàn bộ thao tác chạy, test, package non-backend đều quy về ai-dev-system.

Việc cần làm
Gom scripts liên quan non-backend vào:
ai-dev-system/scripts/run/
ai-dev-system/scripts/validate/
ai-dev-system/scripts/package/
ai-dev-system/scripts/migrate/
Tạo command chuẩn cho:
validate structure
inspect unity profile
run gui agent
run unity automation
validate avatar pipeline
package Unity client
Tổ chức tests thành:
ai-dev-system/tests/control-plane/
ai-dev-system/tests/unity-integration/
ai-dev-system/tests/asset-pipeline/
ai-dev-system/tests/structure/
Acceptance

Người mới vào repo chỉ cần đi qua ai-dev-system/scripts/ là chạy được phần non-backend.

Phase 8 — Viết lại docs và tasks theo kiến trúc mới
Mục tiêu

Đổi ngôn ngữ của repo từ “nhiều root độc lập” sang “một lõi non-backend”.

Việc cần làm với docs/
Viết docs/roadmap.md mới cho cấu trúc mới.
Tạo docs/architecture/non-backend-integration.md.
Tạo docs/migration/ai-dev-system-unification.md.
Tách rõ:
source of truth
workbench
client
control plane
domain
Gắn nhãn cái gì là current implementation, cái gì là planned.
Việc cần làm với tasks/
Bỏ cách theo dõi cũ dựa trên unity-client/ như root riêng.
Tạo lane mới:
Control Plane
Unity Client
Avatar + Customization
Asset Pipeline
Governance + Validation
Thêm phase tasks mới cho migration.
Mọi task phải chỉ vào path mới trong ai-dev-system.
Acceptance

Docs và tasks không còn diễn đạt repo như cấu trúc cũ.

Phase 9 — Legacy shims, cleanup, và khóa kiến trúc
Mục tiêu

Xóa dấu vết “nửa cũ nửa mới”.

Việc cần làm
Xóa các shim path sau khi validation ổn.
Xóa root folders cũ đã được hấp thụ.
Cập nhật toàn bộ references trong docs và scripts.
Thêm validator chống drift:
path cũ còn bị dùng
docs link hỏng
task trỏ folder cũ
release script dùng path cũ
Acceptance

Repo không còn sống dựa trên alias hoặc compatibility hack.

6. Thứ tự triển khai khuyến nghị

Tôi khuyến nghị thứ tự này, không nên đảo:

Phase 0
Phase 1
Phase 2
Phase 4
Phase 3
Phase 5
Phase 6
Phase 7
Phase 8
Phase 9

Lý do:

Phải định nghĩa ai-dev-system là gì trước.
Phải hợp nhất control plane trước khi nhét Unity client vào giữa.
Avatar và asset pipeline nên đi sau khi client/control-plane đã có chỗ đứng rõ.
Docs/tasks rewrite nên đến sau khi path và ownership đã tương đối ổn.
7. Những quyết định kiến trúc tôi khuyên chốt ngay
7.1 ai-dev-system phải đổi vai trò chính thức

Hiện tại docs cũ mô tả nó là tooling phụ. Sau đợt này nó phải được định nghĩa là:

“Lõi non-backend chính của repo.”

7.2 unity-client không còn là root độc lập

Nó phải thành:

ai-dev-system/clients/unity-client/

7.3 ai/ phải bị hấp thụ

Không nên để vừa có root ai/ vừa có ai-dev-system/context/.

7.4 workbench phải tách khỏi runtime

Blender, Clothy3D, imports, raw assets không được nằm lẫn với code chạy.

7.5 docs/ và tasks/ đứng ngoài lâu dài

Điều này đúng với ý bạn và cũng là quyết định kiến trúc tốt.

8. Những rủi ro lớn nhất
8.1 Unity path break

Đây là rủi ro số 1.

Cần:

shim tạm
test sau từng cụm move
không rename sâu quá sớm
8.2 ai-dev-system thành thư mục khổng lồ nhưng vô tổ chức

Giải pháp là tách 4 vành: control-plane, clients, domain, workbench.

8.3 docs/tasks lệch rất nhanh

Giải pháp là phase riêng cho governance và validator drift.

8.4 asset pipeline lấn vào runtime

Giải pháp là client chỉ nhận validated output, không nhận raw source.

9. Definition of done cho toàn bộ đợt tái cấu trúc

Đợt này chỉ nên coi là hoàn tất khi đủ 9 điều sau:

ai-dev-system là source of truth cho toàn bộ non-backend.
unity-client đã nằm dưới ai-dev-system/clients/.
ai/ đã được hấp thụ.
tools và asset-workbench đã có vùng chuẩn.
control plane của GUI agent và Unity automation dùng cùng mô hình orchestration.
docs ở root mô tả đúng kiến trúc mới.
tasks ở root theo dõi đúng lane mới.
không còn script quan trọng nào phụ thuộc path cũ.
validator có thể bắt path drift và doc drift.