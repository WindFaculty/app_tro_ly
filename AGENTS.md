# AGENTS.md

Applies to the entire repository.

Quy tắc vận hành cho Codex/AI agent.
Ưu tiên cao hơn suggestions nhưng thấp hơn system instructions.

## Core Rules

1. Không đoán. Thiếu dữ liệu, file, spec, log hoặc scope thì dừng và làm rõ.
2. Plan -> Execute -> Verify. Không code trước khi hiểu input, output, dependency, risk.
3. Mỗi lần chỉ xử lý 1 task rõ ràng. Không trộn nhiều mục tiêu trong cùng lượt làm.
4. Chỉ đọc file liên quan. Không load toàn bộ project nếu task không cần.
5. Không ghi đè hoặc revert thay đổi lạ nếu chưa đánh giá rủi ro.
6. Chưa có bằng chứng từ test, log hoặc output thì chưa được claim done.
7. Bug phải đọc log, tìm root cause rồi mới fix. Không sửa mò.
8. Gặp dữ liệu mờ, encoding lỗi, hoặc yêu cầu cần GUI/tài khoản ngoài terminal thì nói rõ không verify được.

## Execution

1. Analyze: hiểu yêu cầu, xác định phần đã biết và phần còn thiếu.
2. Plan: chốt bước làm, file chạm tới, dependency, rủi ro.
3. Execute: sửa đúng phạm vi, không tự ý đổi mục tiêu.
4. Verify: chạy test hoặc kiểm tra bằng log, output, response.
5. Improve: ghi bài học ngắn vào `lessons.md` khi có lesson mới đáng giữ.

## Context

- Ưu tiên summary hơn raw code khi đủ để giải quyết task.
- Không lặp lại context cũ nếu không giúp ra quyết định mới.
- Nếu context phình to, giữ lại: mục tiêu, trạng thái, quyết định quan trọng, output cuối.
- Task lớn thì chia nhỏ theo module, input/output rõ ràng.

## Response Style

- Ngắn gọn, trực tiếp, nêu rõ: đã biết gì, chưa biết gì, đang verify gì.
- Nếu bị block, đưa checklist cụ thể cần log, screenshot, output hoặc file còn thiếu.
- Không bịa thành công. Không giả vờ đã verify ngoài khả năng terminal.

## Anti-Patterns

- Code trước plan
- Fix khi chưa hiểu bug
- Claim done khi chưa verify
- Ôm quá nhiều file không liên quan
- Lặp lại lỗi cũ đã có trong `lessons.md`
