# SAP2000 MCP Troubleshooting Guide

Các lỗi thường gặp và cách xử lý khi làm việc với `HPSap2000`.

## 1. Lỗi kết nối (-32003)

- **Nguyên nhân 1: Chưa bấm Attach hoặc chưa chạy SAP2000:**
  - *Triệu chứng:* `execute_sap2000_code` trả về lỗi `"SAP2000 not attached — click Attach in the HPSap2000 MCP Bridge window (start SAP2000 first)."`.
  - *Khắc phục:* Mở SAP2000, mở model, mở `HPSap2000.McpBridge.exe`, bấm **Start listener**, bấm **Attach**.

- **Nguyên nhân 2: SAP2000 không đăng ký COM API trong phiên:**
  - *Triệu chứng:* Bridge báo `not registered for the API in this session`.
  - *Khắc phục:* Đóng SAP2000. Khởi động lại SAP2000 từ shortcut / Start Menu trước, sau đó mới dùng menu `File > Open` để mở model. Không mở bằng cách double-click file `.SDB`.

- **Nguyên nhân 3: Quyền Administrator (Elevation mismatch):**
  - *Triệu chứng:* SAP2000 đang mở nhưng Bridge không nhìn thấy.
  - *Khắc phục:* Đảm bảo cả SAP2000 và Bridge chạy ở cùng mức quyền (thường là non-elevated).

## 2. Lỗi quyền thực thi (-32001)

- **Triệu chứng:** `Code execution is disabled. Ask the user to tick 'Allow AI code execution'...`
- **Khắc phục:** Người dùng cần tích chọn checkbox **"Allow AI code execution"** trên cửa sổ `HPSap2000 MCP Bridge`. Checkbox này tự động tắt mỗi lần bridge khởi động lại.

- **Triệu chứng:** `Destructive operations are disabled. Ask the user to tick 'Allow destructive operations'...`
- **Khắc phục:** Các thao tác như `run_analysis`, `SetModelIsLocked`, `Delete` đòi hỏi người dùng tích thêm checkbox **"Allow destructive operations"**.

## 3. Lỗi PREVIEW

- **Triệu chứng:** Tool trả về `isError: true` và diagnostic có id `PREVIEW`.
- **Nguyên nhân:** Script có hành vi ghi (`Tier W` hoặc `Tier D`) nhưng được gọi với `transaction="none"` hoặc `dryRun=true`.
- **Khắc phục:** Để thực thi thay đổi thật, gửi `transaction="auto"` và `dryRun=false`. Bridge sẽ tự động save và tạo snapshot `.SDB`.
