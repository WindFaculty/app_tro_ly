# Task People - Local Desktop Assistant

Updated: 2026-03-14
Status values: TODO | DOING | DONE | BLOCKED
Ownership: `Pxx` = human/manual/off-repo work | `Axx` = Codex/AI-executable repo work tracked in `tasks/task-queue.md`

## How To Use This File

- This file tracks manual work that cannot be completed safely from inside the repo.
- `tasks/task-queue.md` tracks the repo work that Codex or AI can do directly.
- `P01-P05` are current validation and environment tasks.
- `P06-P08` only matter when future roadmap items start.

## Current Manual Tasks

- `P01 | TODO | Open unity-client in the Unity Editor, run EditMode and PlayMode tests, and capture screenshots or notes for failures or a pass result. | Done when: editor test outcomes are recorded and can be referenced by follow-up fixes. | Unblocks: A03 A04 A05 A07 A10 A11 A14 A15`
- `P02 | TODO | Run manual smoke tests on the dev machine for task CRUD, chat, reminders, subtitles, and offline or degraded mode. | Done when: the main user flows have been exercised manually and the results are written down for follow-up. | Unblocks: A03 A04 A07 A09 A10 A11 A14 A15`
- `P03 | TODO | Install and configure faster-whisper or whisper.cpp, Piper, and optional Ollama runtime settings on the demo machine. | Done when: the machine has working runtime paths, binaries, and models that the app can point at for end-to-end testing. | Unblocks: A03 A04 A07 A10 A11 A12 A13 A14 A15`
- `P04 | TODO | Provide the real avatar asset, animator controller, and blendshape or lip-sync expectations. | Done when: the repo has the production avatar inputs needed to replace the current placeholder safely. | Unblocks: A03 A04 A07 A10 A11 A14 A15 A19`
- `P05 | TODO | Build and test the release folder on a clean target Windows machine, then record any startup or runtime issues. | Done when: release-folder behavior has been validated outside the dev environment and any failures are available for follow-up. | Unblocks: A01 A02 A03 A04 A06 A07 A10 A11 A14 A15`

## Future Manual Prerequisites

- `P06 | BLOCKED | When calendar sync starts, provide OAuth credentials, a test calendar, and the related project setup details. | Done when: a usable calendar integration environment exists for implementation and validation. | Unblocks: A16`
- `P07 | BLOCKED | When automation features start, provide the accounts, test environments, and Windows permissions needed for browser automation, wake-word mode, and desktop control. | Done when: the required automation environments and permission assumptions are available for safe implementation. | Unblocks: A17 A20 A21`
- `P08 | BLOCKED | When cross-device sync starts, lock down the device topology, network assumptions, and sync storage location. | Done when: the sync target environment is defined well enough to implement a concrete transport and storage path. | Unblocks: A22 A23`

## Bản Tiếng Việt

Updated: 2026-03-14
Giá trị trạng thái: TODO | DOING | DONE | BLOCKED
Quy ước: `Pxx` = công việc người dùng phải làm thủ công hoặc ngoài repo | `Axx` = công việc Codex/AI có thể thực hiện trong repo, được theo dõi trong `tasks/task-queue.md`

## Công Việc Thủ Công Hiện Tại

- `P01 | TODO | Mở unity-client trong Unity Editor, chạy EditMode và PlayMode tests, sau đó lưu ảnh màn hình hoặc ghi chú nếu có lỗi hay đã pass. | Done when: kết quả test trong editor đã được ghi lại để dùng cho các bước sửa tiếp theo. | Unblocks: A03 A04 A05 A07 A10 A11 A14 A15`
- `P02 | TODO | Chạy manual smoke test trên máy dev cho task CRUD, chat, reminder, subtitle, và chế độ offline hoặc degraded. | Done when: các luồng chính đã được kiểm tra thủ công và kết quả đã được ghi lại để theo dõi. | Unblocks: A03 A04 A07 A09 A10 A11 A14 A15`
- `P03 | TODO | Cài đặt và cấu hình faster-whisper hoặc whisper.cpp, Piper, và các thiết lập Ollama tùy chọn trên máy demo. | Done when: máy đã có runtime, binary, model, và đường dẫn cấu hình đúng để app có thể chạy end-to-end. | Unblocks: A03 A04 A07 A10 A11 A12 A13 A14 A15`
- `P04 | TODO | Cung cấp avatar asset thật, animator controller, và kỳ vọng về blendshape hoặc lip-sync. | Done when: repo đã có đầu vào avatar production cần thiết để thay thế placeholder một cách an toàn. | Unblocks: A03 A04 A07 A10 A11 A14 A15 A19`
- `P05 | TODO | Build và kiểm tra release folder trên một máy Windows đích sạch, sau đó ghi lại lỗi startup hoặc runtime nếu có. | Done when: hành vi của release folder đã được xác minh bên ngoài môi trường dev và mọi lỗi gặp phải đã được ghi lại để xử lý tiếp. | Unblocks: A01 A02 A03 A04 A06 A07 A10 A11 A14 A15`

## Điều Kiện Thủ Công Trong Tương Lai

- `P06 | BLOCKED | Khi bắt đầu calendar sync, cung cấp OAuth credentials, một test calendar, và thông tin project liên quan. | Done when: đã có môi trường calendar integration khả dụng để implement và kiểm tra. | Unblocks: A16`
- `P07 | BLOCKED | Khi bắt đầu các tính năng automation, cung cấp tài khoản, môi trường test, và quyền Windows cần thiết cho browser automation, wake-word mode, và desktop control. | Done when: đã có đầy đủ môi trường automation và giả định về quyền để implement an toàn. | Unblocks: A17 A20 A21`
- `P08 | BLOCKED | Khi bắt đầu cross-device sync, chốt topology thiết bị, giả định về mạng, và nơi lưu dữ liệu đồng bộ. | Done when: môi trường sync đích đã được định nghĩa đủ rõ để implement transport và storage cụ thể. | Unblocks: A22 A23`
