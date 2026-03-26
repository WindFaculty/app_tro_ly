# Lessons

- 2026-03-17: Khi thêm script Unity mới và chạy `-executeMethod` trong batchmode, lượt chạy đầu thường chỉ import/recompile script; cần rerun sau khi compile sạch để method thực sự chạy.
- 2026-03-17: Không tự bịa `ExpressionDefinition` cho facial system khi `P04` (blendshape or lip-sync expectations) còn thiếu. Có thể verify face mesh binding và blink riêng, nhưng expression coverage phải để blocked cho đến khi có assets/spec thật.
- 2026-03-17: Với batch validation cho avatar, chạy Unity ở chế độ test riêng (`-codexTestMode`) và dùng trigger file ổn định hơn `-executeMethod`; nếu không, bootstrap scene/runtime khác có thể làm nhiễu kết quả validator.
- 2026-03-17: Test amplitude lip-sync trong Unity cần `AudioListener` hiện diện trong scene probe, nếu không `GetOutputData` sẽ luôn đọc gần 0 và cho kết quả âm tính giả.
- 2026-03-17: Không cập nhật `Dictionary` ngay trong lúc đang `foreach` qua chính collection đó trong `Update`; hãy gom key/value tạm rồi apply sau để tránh `InvalidOperationException`.
- 2026-03-19: Với task Unity PlayMode, chỉ được đánh dấu done khi có bằng chứng chạy test từ Unity Editor/CLI đúng version project; nếu máy không có `Unity.exe` thì dừng ở mức thêm test và báo blocked, không claim đã verify.
- 2026-03-19: Tránh phụ thuộc `Resources.GetBuiltinResource<Sprite>("UI/Skin/...")` trong Unity 6.x batchmode; các path builtin cũ có thể phát sinh unhandled log và làm test ngã, nên ưu tiên sprite fallback tự sinh cho runtime UI/test.
- 2026-03-19: Khi gói preflight Python trong PowerShell 5.1, dùng temp `.py` file đáng tin cậy hơn `python -c` qua `Start-Process`; inline code dễ vấp lỗi quoting và stderr/native-command handling giả làm script fail.
