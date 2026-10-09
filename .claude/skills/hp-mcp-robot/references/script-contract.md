# Robot Structural Analysis Script Contract

Quy định cấu trúc, globals, units, và luồng chạy khi viết script cho `execute_robot_code`.

## 1. Globals & Environment

Mỗi script C# được biên dịch bằng Roslyn trong môi trường:
- `robot` (`RobotOM.IRobotApplication`): Giao diện COM chính của Robot.
- `structure` (`RobotOM.IRobotStructure`): Cấu trúc mô hình hiện thời (`robot.Project.Structure`).
- `units` (`RobotOM.IRobotUnitMngr`): Quản lý đơn vị.
- `args` (`ScriptArgs`): Bộ phân giải tham số JSON mềm dẻo.
- `ct` (`CancellationToken`): Hủy script hợp tác khi timeout.
- `log(string)`: Ghi log vào log file của bridge.
- `progress(int cur, int total, string msg)`: Báo tiến trình cho client MCP.

## 2. Quy tắc bắt buộc

1. **Units Metric Enforced:**
   - Toàn bộ tọa độ, chiều dài, diện tích, khoảng cách: **Metre (m)**.
   - Tải trọng tập trung, phản lực, lực dọc, lực cắt: **Kilonewton (kN)**.
   - Moment uốn, xoắn: **kN·m**.
   - Tải phân bố: **kN/m**.
   - Ứng suất: **Megapascal (MPa)**.

2. **An toàn & Tiers:**
   - **R (Read-only):** `transaction="none"`. Không làm thay đổi dữ liệu mô hình.
   - **W (Write):** `transaction="auto"`. Bridge tự động lưu model và copy snapshot `.rtd` vào thư mục snapshots trước khi chạy script.
   - **D (Destructive / Heavy):** Yêu cầu checkbox `Allow Heavy/Delete operations` trong bridge app. Chặn mọi thao tác xoá cấu kiện (`.Delete`) hoặc kích hoạt tính toán giải FEA (`.Calculate()`).

3. **Cú pháp đối tượng trong RobotOM:**
   - Truy cập nút: `structure.Nodes.Get(number)` hoặc `structure.Nodes.Create(number, x, y, z)`.
   - Truy cập thanh: `structure.Bars.Get(number)` hoặc `structure.Bars.Create(number, startNode, endNode)`.
   - Truy cập trường hợp tải: `structure.Cases.Get(caseNumber)`.
