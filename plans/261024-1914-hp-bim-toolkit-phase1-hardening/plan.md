# HP BIM Toolkit — GĐ1 hardening: đóng gói, types TS sinh tự động, test live còn thiếu

2026-10-24. Nối tiếp [plan Module 1](../261024-0640-hp-bim-toolkit-dashboard/plan.md) và [đề xuất kiến trúc](../reports/architect-2026-10-24-revit-addin-modern-stack-direction.md) mục 12.

## Hiện trạng (scout)
- `HPRebar/build/Modules/CreateBundleModule.cs`: bundle cho từng project trong `[HPRebar, HPRebar_McpBridge]` từ các thư mục `bin/**/publish`. Toolkit chưa có trong danh sách.
- `CreateInstallerModule.cs`: MSI chỉ lấy publish của `HPRebar` (McpBridge cũng không có trong MSI). `install/Installer.Generator.cs`: mỗi thư mục tạo một feature tên "<năm>" + ConfigurableDir `INSTALL<năm>`. Hai add-in cùng năm sẽ trùng feature/ConfigurableDir.
- Toolkit publish = `HPBimToolkit.addin` + `HPBimToolkit/` (không có WebView2, frontend đã nhúng trong DLL).
- `dotnet run -- pack` chạy `CleanProjectModule` (dọn mọi `bin`/`obj` dưới git root, cả thư mục `output/` ở gốc), trừ khi chạy trên CI. Không chạy trên repo chính.
- `protocol.ts` được viết tay theo `HPBimToolkit.Core/Contracts`; chỉ phía C# có `WireFormatTests`.

## Phases
| # | Phase | Type | Gate |
|---|---|---|---|
| 1 ✅ | Types TS: generator trong `HPBimToolkit.Core.Tests` (reflection + NullabilityInfo, catalog command → request/response, event → payload) → `src/bridge/contracts.generated.ts`; test so sánh, `HPBIM_UPDATE_CONTRACTS=1` ghi lại; `protocol.ts` giữ envelope + re-export | AFK | Core.Tests xanh; typecheck/lint/vitest/build frontend xanh; mọi `CommandNames`/`EventNames` có trong catalog |
| 2 ✅ | Đóng gói: thêm `Projects.HPBimToolkit` vào bundle; MSI nhận publish của toolkit; Generator: feature năm → feature con theo add-in (tên lấy từ file `.addin`), mặc định cài | AFK | `Build.csproj` + `Installer.csproj` build; `Release.R26` build; chạy installer exe với publish HPRebar + toolkit R26 → 2 MSI, liệt kê đúng file; `dotnet run -- pack` trong git worktree tạm (dọn bin/obj chỉ ở đó) → 3 bundle zip + MSI |
| 3 ✅ | Live (Revit thứ hai, bản sao model): đóng tài liệu → empty state → mở lại; Dynamo mở cùng phiên, toolkit vẫn trả lời; theme sáng/tối (nếu user duyệt) | HITL nhẹ | ghi vào `reports/phase-03-live-verify.md` |
| 4 ✅ | Review diff + checklist, cập nhật CLAUDE.md/AGENTS.md (chèn tay), install guide | AFK | review không còn High |

## Quyết định
- Generator nằm trong test project: không thêm package, không thêm project; drift bị test bắt.
- Ghi đè kiểu ở generator cho chuỗi có tập giá trị cố định (`theme` → `Theme`, `error.code` → `ErrorCode`) thay vì đổi contract C# (wire giữ nguyên byte).
- MSI: toolkit là feature riêng dưới năm 2026, chọn sẵn; bỏ chọn được trong cây feature.

## Không làm
- Ký số DLL (cần chứng thư công ty), R25/R27, đổi pipeline Clean.

Kết quả: [reports/phase-03-live-verify.md](reports/phase-03-live-verify.md).
