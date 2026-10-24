# Phase 0 — ADR-0009 + tham chiếu HPRebar.Core + spike

**Status:** Planned · **Type:** HITL nhẹ (Revit thứ hai)

## Mục tiêu
Chứng minh hai giả định trước khi port: (1) engine Kata của `HPRebar.Core` chạy trong load context `HPBimToolkit` không xung đột với `HPRebar.dll` (đã ILRepack, có bản Core riêng bên trong); (2) Excel COM (ROT `GetActiveObject` + `InvokeMember`) chạy từ toolkit như từ tool WPF.

## Việc
1. `docs/architecture/adr/0009-bim-toolkit-uses-hprebar-core.md`: toolkit tham chiếu `HPRebar.Core` (chỉ thuần, netstandard2.0); adapter Kata port vào toolkit, trùng tạm thời; điều kiện hết trùng = gỡ WPF. Cập nhật `adr/README.md`, `DEPENDENCY_RULES.md`.
2. `HPBimToolkit.Core.csproj` (hoặc add-in) → `ProjectReference ..\HPRebar.Core\HPRebar.Core.csproj`. Quyết định ở đâu: chỉ add-in + Core.Tests nếu Domain module dùng trực tiếp type Kata; ưu tiên `HPBimToolkit.Core` để test được.
3. Spike lệnh tạm `kata.spike` (không commit lệnh): gọi một hàm Core Kata (vd `KataFormat`/segmenter với input cố định) + `ExcelComAttach` dò Excel đang mở → trả JSON. Chạy trong Revit thứ hai cùng lúc HPRebar đã nạp (mở tool Kata WPF 1 lần trước) → không `FileLoadException`, type identity đúng.
4. Ghi `reports/phase-00-spike.md`; gỡ lệnh spike.

## Cổng
`dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`; spike live PASS cả 2 giả định. Fail (1) → dừng, đề xuất copy engine; fail (2) → tách Excel ra process riêng hoặc dùng lại cơ chế WPF.

## Rủi ro
Load context: toolkit `ContextName` riêng nên `HPRebar.Core.dll` của toolkit là bản riêng — đúng ý; cần xác nhận thật.
