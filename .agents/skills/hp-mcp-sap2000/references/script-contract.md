# SAP2000 Script Contract

Quy định cấu trúc, globals, units, và luồng chạy khi viết script cho `execute_sap2000_code`.

## 1. Globals & Environment

Mỗi script C# được biên dịch bằng Roslyn trong môi trường:
- `sapModel` (`SAP2000v1.cSapModel`): Giao diện OAPI chính của SAP2000.
- `sap` (`SAP2000v1.cOAPI`): Giao diện ứng dụng CSI SAP2000.
- `units`: Thông tin đơn vị ép buộc (`units.Label == "kN_m_C"`, `units.LengthUnit == "m"`, `units.ForceUnit == "kN"`).
- `args` (`ScriptArgs`): Bộ phân giải tham số JSON mềm dẻo.
- `ct` (`CancellationToken`): Hủy script hợp tác khi timeout.
- `log(string)`: Ghi log vào log file của bridge.
- `progress(int cur, int total, string msg)`: Báo tiến trình cho client MCP.

## 2. Quy tắc bắt buộc

1. **Units kN_m_C:**
   - Toàn bộ tọa độ, chiều dài, diện tích, khoảng cách: **Metre (m)**.
   - Tải trọng tập trung, lực cắt, lực dọc: **Kilonewton (kN)**.
   - Moment uốn, xoắn: **kN·m**.
   - Tải phân bố: **kN/m**.
   - Áp lực / Ứng suất: **kN/m² (kPa)**. Để hiển thị MPa, chia cho 1000.

2. **Kiểm tra giá trị trả về:**
   - Mọi hàm OAPI trả về mã lỗi `int ret`. `0` là thành công.
   - Nếu `ret != 0`, throw `InvalidOperationException($"SAP2000 returned {ret} from <Method>")`.

3. **An toàn & Tiers:**
   - **R (Read-only):** `transaction="none"`.
   - **W (Write):** `transaction="auto"`. Bridge tự động lưu model và copy snapshot `.SDB` vào thư mục snapshots trước khi chạy script.
   - **D (Destructive):** Yêu cầu checkbox `Allow destructive operations` trong bridge app.
