---
name: revit-webview-addin
description: "Quy trình thiết kế và phát triển Revit add-in giao diện web: WebView2 + React + TypeScript + Vite + Tailwind trong Dockable Pane, C# chia Domain / Application / Infrastructure, bridge JSON có cấu trúc qua một ExternalEvent, Transaction đúng luật Revit. Mẫu chuẩn là HP BIM Toolkit (HPRebar/HPBimToolkit*). TRIGGER when: user nói 'thêm module cho BIM Toolkit', 'Element Explorer', 'Parameter Manager', 'Reports', 'dockable pane React', 'WebView2 Revit', 'revit react', 'bridge JSON Revit', 'add-in web Revit', hoặc sửa file trong HPRebar/HPBimToolkit*, HPBimToolkit.Frontend. Keywords: revit, webview2, react, typescript, vite, tailwind, externalevent, transaction, dockable pane, bridge."
user-invocable: true
when_to_use: "Khi thêm module (lệnh + trang) cho HP BIM Toolkit, làm add-in Revit mới có giao diện web, hoặc sửa bridge/ExternalEvent/Transaction của một add-in như vậy."
category: revit
keywords: [revit, webview2, react, typescript, vite, tailwind, externalevent, transaction, dockable-pane, bridge]
argument-hint: "[module hoặc việc cần làm]"
metadata:
  author: hoang
  version: "1.0.0"
  stabs: ["C#", ".NET 8", "Revit API 2026", "WebView2 1.0.2478.35", "React 19", "TypeScript 5.8", "Vite 6", "Tailwind 4"]
---

# Revit add-in giao diện web (WebView2 + React) — quy trình

Mẫu chuẩn đang chạy thật: **HP BIM Toolkit** (Revit 2026, live-verified). Mọi việc mới bắt đầu từ code đó, không dựng lại từ đầu. Ngoại lệ UI: ADR-0008 (`docs/architecture/adr/0008-bim-toolkit-webview2-ui.md`) cho phép WebView2 + React **chỉ** với HP BIM Toolkit — tính năng web mới là module của toolkit; add-in mới muốn dùng web phải được người dùng duyệt + ADR mới (mặc định WPF + MaterialDesign). Hướng dẫn chung (không phải lời ADR): web cho công cụ nhiều dữ liệu, hộp thoại nhỏ để WPF.

## Bước 0 — Scout trước, rồi mới quyết

1. Đọc mục "HP BIM Toolkit" trong `CLAUDE.md`, hướng dẫn cài đặt (`docs/hp-bim-toolkit-install-guide.md`), governance C# (`docs/clean-code/CODE_REVIEW_CHECKLIST.md`, `host-appendix/revit.md`).
2. Xem module có sẵn: `HPRebar/HPBimToolkit/Modules/Dashboard/` + `HPRebar/HPBimToolkit.Frontend/src/modules/dashboard/`.
3. Việc ≥ 3 file hoặc module mới → plan trong `plans/<YYMMDD-HHmm>-<slug>/` (route Planning Mode của `.claude/rules/antigravity-workflow.md`). Đổi cấu trúc code đang chạy = refactor → hỏi người dùng trước.

## Kiến trúc — code nào nằm đâu

| Lớp | Nơi đặt | Được phép | Cấm |
|---|---|---|---|
| Frontend | `HPBimToolkit.Frontend/src/modules/<module>/`, UI kit `src/ui/` | state UI, gọi `bridge.request`, nghe event | gọi mạng ngoài, giữ token, logic Revit |
| Bridge (hợp đồng) | `HPBimToolkit.Core/Contracts`, `Protocol/` | DTO `record`, tên lệnh/event, parse, timeout, mã lỗi | tham chiếu `Autodesk.*` |
| Domain | `HPBimToolkit.Core/Domain/<Module>/` (cũ: `Statistics/`) | quy tắc nghiệp vụ thuần, validate | Revit API, I/O |
| Application | `HPBimToolkit.Core/Application/` + `HPBimToolkit/Modules/<Module>/` | điều phối use case, đăng ký lệnh, validate trước khi xếp hàng | thuật toán (đưa xuống Domain), gọi API ngoài queue |
| Infrastructure | `HPBimToolkit/Services/` (adapter Revit → DTO), `ExternalEvents/`, `Bridge/`, `Host/`, `Infrastructure/` | Revit API trên API thread, WebView2, log | trả object Revit cho trang |

Chi tiết + sơ đồ phụ thuộc: [references/architecture-layers.md](references/architecture-layers.md).

## Quy trình một module (mỗi bước có cổng kiểm tra)

| # | Bước | Làm gì | Cổng |
|---|---|---|---|
| 1 | Hợp đồng | Tên lệnh `<module>.<verb>` trong `BridgeNames.cs`, DTO trong `Contracts/`, thêm vào `HPBimToolkit.Core.Tests/TypeScript/ProtocolCatalog.cs`; sinh lại TS: trong `HPRebar/` chạy `HPBIM_UPDATE_CONTRACTS=1 dotnet test HPBimToolkit.Core.Tests` (từ gốc repo = 0 test) | Core.Tests xanh (chạy lần 2 không biến) |
| 2 | Domain | Quy tắc thuần + test xUnit (validate, lọc, gom nhóm, giới hạn). Quy tắc mà đầu vào là kiểu Revit (`Category`, `ViewType`) và chỉ một dòng thì để trong adapter | Core.Tests xanh |
| 3 | Application | `Modules/<Module>/<Module>Module : IToolkitModule`; validate trước queue; lệnh dài dùng `SingleFlightRun`; thêm vào `ToolkitApplication.Compose` | build `Debug.R26 -p:DeployAddin=false` |
| 4 | Infrastructure | Adapter static trong `Services/` nhận `UIApplication`, trả DTO; đọc/ghi theo [references/revit-threading-and-transactions.md](references/revit-threading-and-transactions.md) | build R26 |
| 5 | Frontend | Trang + hook state + dữ liệu mock (`mockData.ts`), đủ loading/empty/error/stale/toast, bật trong `registry.ts` | `npm run typecheck && npm run lint && npm test && npm run build` |
| 6 | Live | Revit thứ hai trên **bản sao** model, thêm check vào harness | toàn PASS, Revit còn responding |
| 7 | Review + docs | `CODE_REVIEW_CHECKLIST` + agent `code-reviewer`; cập nhật CLAUDE.md (chèn cùng nội dung vào AGENTS.md), install guide, report trong plan | không còn High |
| 8 | Phát hành | `pack` → bundle + MSI (xem [references/packaging-and-versions.md](references/packaging-and-versions.md)) | cài thử từ MSI |

Protocol, lỗi, timeout, event: [references/bridge-protocol.md](references/bridge-protocol.md). Test và live harness: [references/testing-and-live-verify.md](references/testing-and-live-verify.md). Bẫy đã gặp thật: [references/gotchas.md](references/gotchas.md).

## Luật cứng

- Mọi Revit API đi qua **một** `RevitRequestQueue` (ExternalEvent). Không tạo ExternalEvent thứ hai.
- Ghi model: preview trước, apply sau; `TransactionGroup` "HP: <tác vụ>" + `Transaction` mở và đóng trong **cùng một** `Execute` (1 Undo). Cần tiến độ/huỷ cho khối rất lớn → mỗi lượt `RunAsync` một Transaction riêng (nhiều Undo, báo trước người dùng). Không giữ group mở qua nhiều lượt.
- Event Revit (`SelectionChanged`, `DocumentChanged`) chỉ làm việc O(1) và coalesce; không quét model trong event.
- Trang không bao giờ nhận object Revit; `ElementId` → `long`, lưu ngoài phiên dùng `UniqueId`.
- Không test trên model thật hay Revit người dùng đang mở; không gõ phím vào hộp thoại chọn file; đổi cài đặt user của Revit (theme) phải hỏi trước và trả lại.
- Build gate: `Debug.R26` (máy dev có Revit 2026). Đa phiên bản chỉ khi biết bản WebView2 Revit đó ship.

## Không làm (tránh overengineering)

- Không tách thêm project Domain/Application/Infrastructure: 2 project C# (Core thuần + add-in) là đủ ở quy mô hiện tại.
- Không thêm Redux/Zustand/React Query, MUI, router lib khi hook + reducer đủ.
- Không thêm backend (NestJS/ASP.NET, PostgreSQL, Docker) trước khi có ≥ 2 nhu cầu dữ liệu dùng chung nhiều người (chuẩn công ty, lịch sử, dashboard đa dự án). Khi cần: ưu tiên ASP.NET Core (cùng ngôn ngữ), add-in gọi qua C# (token không vào JS). Xem `plans/reports/architect-2026-10-24-revit-addin-modern-stack-direction.md`.
- Không viết `RevitWriteRunner`/abstraction khi chưa có module ghi đầu tiên dùng nó.
