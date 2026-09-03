---
phase: 8
title: "Integration, F5 smoke, Release all configs, docs"
status: pending
priority: P1
effort: "1.5d"
dependencies: [4, 5, 7]
---

# Phase 8: Integration, F5 smoke, Release all configs, docs

## Overview
Nối UI (Phase 6/7) với orchestrator (Phase 4/5), bỏ spec hardcode trong command, chạy end-to-end trên Revit thật cho cả 2 kiểu cột, build Release 5 config qua ModularPipelines, code review, cập nhật docs + CLAUDE.md (section "Current State" hiện đang mô tả scaffold rỗng — sẽ sai sau phase này).

## Requirements
- Functional: luồng đầy đủ Pick → Validate → Dialog → OK → views + rebar + dim + tag + detail shop; Cancel rollback sạch; Undo 1 bước.
- Non-functional: `cd build && dotnet run` pass cả `Release.R23..R27`; `dotnet run -- test` pass (xUnit + TUnit config có Revit); không warning mới; docs đồng bộ.

## Architecture
Không thêm component. Chỉ dây nối trong `ColumnRebarCommand`:
```
Execute
  pick → sort → validate (Phase 3)
  catalog + stack + default specs + annotation (Phase 3/4/5)
  session/VM/view (Phase 6) → ShowDialog
  OK → VM gọi orchestrator.Run (orchestrator tự quản TransactionGroup — D8)
  Command KHÔNG chạm TransactionGroup
```

## Related Code Files
- Modify: `Column Rebar/ColumnRebarCommand.cs` (bỏ default hardcode → session)
- Modify: `docs/system-architecture.md` — thêm mục Column Rebar (diagram pipeline + Core/Revit split). File này hiện mô tả *target design* chưa tồn tại → sửa cho khớp thực tế.
- Create: `docs/codebase-summary.md` — **chưa tồn tại**, tạo mới: layout HPRebar + Core + 2 test project
- Create: `docs/project-changelog.md` — **chưa tồn tại**, tạo mới, entry đầu tiên = feature này
- Modify: `CLAUDE.md` — section "HPRebar — Current State" (không còn bare scaffold; Core project; 2 test project; feature Column Rebar; commands `dotnet test HPRebar.Core.Tests`)
- Modify: `plans/260903-2307-…/plan.md` — status, unresolved đã đóng
- Modify: `HPRebar/build/appsettings.json` — `Bundle:VendorName` nếu user muốn (hỏi)

## Implementation Steps
1. Dây nối command; xoá mọi `// TODO Phase X` + spec hardcode. Build `Debug.R26` + `Debug.R23`.
2. F5 matrix trên **Revit 2026** (máy chỉ có Revit 2025 + 2026; R23/R24/R27 build-only — ghi rõ là *chưa verify runtime*):
   | Case | Kỳ vọng |
   |---|---|
   | 1 cột rect, không dầm | thép + đai đều, `hb'=max(b,h)` cho dowel, 2 detail + 1 section |
   | 2 cột rect thu tiết diện + dầm đỉnh | bẻ xiên dưới dầm, so le 35d/70d, dim đúng mặt dầm |
   | 3 cột cyl `nd=8` | thép vòng tròn, đai `M_T3`, dowel theo góc |
   | Cancel giữa chừng | model không đổi, không view rác |
   | Thiếu `M_T1` | dialog, không exception |
   | Cột nghiêng / không liên tục | lỗi code 2 / 4 đúng (chứng minh B1/B2 đã sửa) |
   | Đổi ngôn ngữ VN | nhãn + message lỗi VN |
   | Revit theme Light | dialog + canvas theo Light |
   Chạy lại case 1–3 trên **Revit 2025** (cùng TFM net8, rẻ) để có 2 điểm dữ liệu.
   Ghi kết quả vào `plans/260903-2307-…/reports/f5-smoke-results.md` (screenshot nếu có).
3. `cd HPRebar/build && dotnet run` → 5 Release config; `dotnet run -- test`; optional `dotnet run -- pack` → MSI/bundle trong `HPRebar/output/`.
4. `/bs:code-review` toàn feature folder + Core: ưu tiên transaction safety, null-safety param, file size, `#if` có comment.
5. Sửa finding blocking; re-run test + build.
6. Docs (skill `docs`): 3 file docs + CLAUDE.md. CLAUDE.md phải cập nhật chính xác: Core project path, test command, feature folder đầu tiên áp dụng convention = `Column Rebar` (thay reference `View Sheet Creator` đã mất).
7. `/bs:journal` — ghi quyết định D1–D7, bug B1–B6 đã sửa, cái gì bỏ (TaskBar/YouTube/Account, Start/End section view, tag comment-out).
8. Đóng plan: `status: completed`, cập nhật Unresolved.

## Success Criteria
- [ ] 8 case F5 pass trên Revit 2026, case 1–3 pass trên Revit 2025, kết quả ghi file
- [ ] `dotnet run` (Compile) pass 5 config; `dotnet run -- test` pass
- [ ] Code review không còn finding severity ≥ high
- [ ] `CLAUDE.md`, `docs/system-architecture.md`, `docs/codebase-summary.md`, changelog cập nhật
- [ ] Report nói rõ R23/R24/R27 **build-only, chưa verify runtime**
- [ ] `git status` sạch sau commit `feat(column-rebar): port R01_ColumnsRebar to HPRebar` (conventional, không AI reference)

## Risk Assessment
- **Không có Revit 2023/2024/2027 trên máy** → net48 và net10 runtime chưa bao giờ chạy thật; build pass là bằng chứng duy nhất. Rủi ro thật ở R23 (net48 WPF `FormattedText` ctor, `MinBy` polyfill) và R27 (.NET 10 + package chưa có). Ghi rõ, không tuyên bố "đã hỗ trợ R23–R27".
- **`dotnet run -- pack` cần WixSharp/Autodesk.PackageBuilder** → optional, không block.
- **Regression Phase 7 khi nối** → chạy lại xUnit + F5 case 2 sau mỗi fix.
