# Robot MCP Troubleshooting Guide

Các lỗi thường gặp và cách xử lý khi vận hành `hprebar-robot`:

## 1. Lỗi -32001 (Refusal Error / Gating Violation)

- **Nguyên nhân 1:** Chưa bật checkbox `Allow AI code execution` trên Bridge UI.
  - *Khắc phục:* Mở cửa sổ `HPRobot.McpBridge`, tick chọn `Allow AI code execution`.
- **Nguyên nhân 2:** Script thuộc Tier D (Delete/Heavy như `Calculate()` hoặc xóa thanh) nhưng chưa bật `Allow Heavy/Delete operations`.
  - *Khắc phục:* Xác nhận với người dùng trước khi tick bật `Allow Heavy/Delete operations` trên Bridge UI.

## 2. Lỗi -32003 (COM / Attachment Error)

- **Nguyên nhân 1:** Robot Structural Analysis chưa mở hoặc Bridge chưa bấm nút **Attach**.
  - *Khắc phục:* Khởi động Robot 2026, mở một file `.rtd`, sau đó trên Bridge bấm nút **Attach**.
- **Nguyên nhân 2:** Model hiện tại chưa được lưu xuống ổ đĩa (`docPath` rỗng).
  - *Khắc phục:* Lưu file model `.rtd` trong Robot trước khi thực hiện các lệnh Write.

## 3. Lỗi COM STA Threading Exception (0x8001010E)

- **Nguyên nhân:** Gọi API COM từ thread khác luồng STA điều khiển.
  - *Khắc phục:* Toàn bộ lệnh RobotOM đã được điều hướng qua kênh STA Worker Thread chuyên dụng trong Bridge (`RunOnControlLaneAsync`). Nếu gặp lỗi này, kiểm tra xem có thread nào ngoài STA can thiệp hay không.
