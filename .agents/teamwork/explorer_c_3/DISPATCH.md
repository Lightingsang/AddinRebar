# DISPATCH — explorer_c_3

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_3
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md

## Mission & Scope
Khảo sát cơ chế phát hiện Revit PID 26264 và cách ly Model/Test Fixtures:
1. Khảo sát việc kiểm tra tiến trình Revit:
   - Cách nhận diện Revit PID 26264 (hoặc bất kỳ process Revit đang chạy nào).
   - Kiểm tra các ràng buộc: Tuyệt đối Zero Touch đối với phiên làm việc hiện hành của người dùng (không can thiệp, không inject code, không mở/đóng model người dùng đang mở trong PID 26264).
2. Khảo sát các test fixtures (.dyn, .rvt) được sử dụng bởi Dynamo tests / benchmarks:
   - Xác định vị trí các file fixture nguồn (ví dụ trong `HPDynamo/tools/`, `HPDynamo.E2E.Tests/Fixtures/`, hoặc `tests/`).
   - Thiết kế cơ chế tự động nhân bản (copy) fixture ra thư mục độc lập tạm thời (ví dụ `%TEMP%/HPDynamoBenchmark/...` hoặc thư mục isolated) trước khi chạy benchmark/tests.
   - Cơ chế dọn dẹp (cleanup) an toàn sau khi chạy xong mà không ảnh hưởng tới bất kỳ file gốc hay model của người dùng.
3. Đề xuất quy trình cô lập fixture hoàn chỉnh cho Worker.

## Output Requirements
Viết báo cáo khảo sát chi tiết vào `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_3\handoff.md`.
Cập nhật `progress.md` định kỳ.
Báo cáo lại cho Orchestrator qua `send_message`.


## 2026-10-09T18:19:21Z
Bạn là explorer_c_3 thuộc dự án HPDynamo MCP Vòng 2.
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_3
Tài liệu chỉ thị:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC TRƯỚC TIÊN)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_3\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md

Nhiệm vụ: Khảo sát cơ chế phát hiện Revit PID 26264 và cách ly Model/Test Fixtures:
1. Khảo sát cách nhận diện Revit PID 26264 an toàn, bảo vệ tuyệt đối session người dùng (Zero Touch).
2. Khảo sát các test fixtures (.dyn, .rvt) được dùng bởi Dynamo tests/benchmarks.
3. Thiết kế giải pháp nhân bản fixture ra thư mục độc lập tạm thời và dọn dẹp an toàn.
Ghi nhận kết quả vào handoff.md trong thư mục của bạn và gửi thông báo qua send_message.
