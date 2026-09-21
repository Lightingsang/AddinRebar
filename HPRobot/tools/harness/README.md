# HPRobot Live Verification Harness

Bộ công cụ kiểm thử trực tiếp End-to-End cho `HPRobot` MCP (stdio server + WPF desktop bridge + Autodesk Robot Structural Analysis Professional 2026).

## Cấu trúc

- `live-verify.py`: Kiểm thử qua giao thức MCP stdio đối chiếu với `HPRobot.Mcp.Server.exe`.
  - Kiểm tra số lượng tools xuất bản (chính xác 24 tools).
  - Kiểm tra `get_robot_context`.
  - Kiểm tra từ chối bảo vệ (ScriptGuard, Namespace deny-list).
  - Kiểm tra đủ 12 công cụ mẫu (embedded seed tools).
- `harness-common.ps1`: Thư viện điều khiển UI Automation cho PowerShell 5.1.
- `run-live-verify.ps1`: Kịch bản điều phối khởi chạy Bridge và chạy harness Python.

## Cách chạy

```powershell
# Chạy kiểm tra nhanh phase detached (không cần mở Robot)
python HPRobot/tools/harness/live-verify.py --exe HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe --phase detached

# Chạy tự động kèm khởi chạy Bridge
powershell -ExecutionPolicy Bypass -File HPRobot/tools/harness/run-live-verify.ps1 -Phase detached
```
