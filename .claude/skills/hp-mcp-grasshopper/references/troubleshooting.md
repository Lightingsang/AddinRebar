# Khắc phục Sự cố HPGrasshopper MCP (Troubleshooting Guide)

Tài liệu này tổng hợp các lỗi runtime và kết nối thường gặp khi điều khiển Grasshopper / Rhino.Inside qua `hprebar-grasshopper`, cùng các bước giải quyết dứt điểm.

---

## 1. Lỗi Kết nối Named Pipe (Pipe Not Found / Connect Timeout)

### Dấu hiệu
- Gọi tool báo lỗi `Pipe connection timed out` hoặc `Cannot find file '\\.\pipe\hpgrasshopper-mcp-2026'`.

### Nguyên nhân & Cách khắc phục
1. **Revit chưa mở hoặc mở nhầm phiên bản:**
   - Hệ thống được cấu hình cho Revit 2026. Hãy đảm bảo Revit 2026 đang mở.
2. **Add-in HPGrasshopper.McpBridge chưa tải:**
   - Kiểm tra xem file manifest `HPGrasshopper.McpBridge.addin` đã có trong `%AppData%\Autodesk\Revit\Addins\2026\` hoặc `%ProgramData%\Autodesk\Revit\Addins\2026\` chưa.
   - Khi Revit khởi động, nếu có hộp thoại bảo mật xuất hiện, bắt buộc chọn **Always Load**.
3. **Rhino.Inside chưa khởi động:**
   - Kiểm tra tab **Rhino.Inside** trên thanh Ribbon của Revit. Bấm vào nút **Start** để nạp môi trường Rhino/Grasshopper.
4. **Kiểm tra trạng thái Pipe bằng PowerShell:**
   ```powershell
   Get-ChildItem \\.\pipe\ | Where-Object Name -like "*hpgrasshopper*"
   ```
   Nếu thấy `hpgrasshopper-mcp-2026`, đường ống IPC đang lắng nghe tốt.

---

## 2. Lỗi Quyền Thực thi AI (Error -32001: Execution Disabled)

### Dấu hiệu
- Lệnh gọi tool trả về: `{"code": -32001, "message": "Execution is disabled in bridge settings..."}`.

### Nguyên nhân & Cách khắc phục
- Đây là cơ chế bảo vệ an toàn chủ động (Opt-in Protection). Mỗi khi Revit khởi động lại, quyền thực thi của AI sẽ tự động tắt để tránh việc script vô tình can thiệp mô hình mà không có sự kiểm soát của người dùng.
- **Khắc phục:**
  1. Trong Revit, chuyển sang tab **HPGrasshopper** trên Ribbon.
  2. Bấm **MCP ▸ Status View** để mở cửa sổ trạng thái.
  3. Tick chọn vào ô checkbox: **"Allow AI execution in this Revit session"**.

---

## 3. Lỗi Hoàn tác / Không Lưu Dữ liệu (dryRun Rollback)

### Dấu hiệu
- Lệnh chạy thành công (`success: true`), nhưng sau khi chạy xong, trên canvas Grasshopper hoặc trong Revit không thấy phần tử nào được thêm mới.

### Nguyên nhân & Cách khắc phục
- Bạn đang truyền tham số `"dryRun": true`.
- Khi `dryRun: true`, hệ thống kích hoạt cơ chế an toàn 2 tầng:
  - Revit sẽ tự động gọi `TransactionGroup.RollBack()`.
  - Grasshopper sẽ gọi `CanvasUndoScope.Rollback()` để hoàn tác components/wires.
- **Khắc phục:** Đặt `"dryRun": false` khi bạn đã chắc chắn muốn lưu lại các thay đổi vào mô hình thật.

---

## 4. Lỗi Grasshopper Canvas / Solver Errors

### Dấu hiệu
- Gọi `run_grasshopper_player` hoặc `batch_edit_grasshopper_canvas` trả về trạng thái cảnh báo hoặc component chuyển sang màu cam/đỏ.

### Nguyên nhân & Cách khắc phục
1. **Thiếu Plugin phụ trợ:** File `.gh` sử dụng các component từ plugin bên ngoài (ví dụ: Karamba, Ladybug, Kangaroo) mà máy chưa cài đặt.
2. **Sai kiểu dữ liệu đầu vào:** Kiểm tra danh sách tham số `inputs` có đúng kiểu (số thực, chuỗi, boolean hay danh sách điểm).
3. **Cách chẩn đoán:** Gọi `read_grasshopper_canvas` với `"includeErrors": true` để trích xuất chính xác thông báo lỗi từ solver và GUID của component bị lỗi.

---

## 5. Lỗi Đơn vị Hình học (Units Mismatch)

### Dấu hiệu
- Đối tượng hình học sinh ra trong Revit quá nhỏ (nhỏ hơn 304.8 lần) hoặc quá lớn.

### Nguyên nhân & Cách khắc phục
- Revit API luôn tính toán hình học theo đơn vị nội bộ là **Feet** ($1\text{ foot} = 304.8\text{ mm}$).
- Rhino và Grasshopper thường được thiết lập theo đơn vị **Millimeters** hoặc **Meters**.
- Khi chuyển toạ độ hoặc curve từ Grasshopper sang Revit:
  - Nếu Rhino ở mm: chia cho `304.8`.
  - Nếu Rhino ở m: chia cho `0.3048`.
