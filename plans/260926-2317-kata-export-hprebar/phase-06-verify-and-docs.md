---
phase: 6
title: "Verify + docs"
status: in-progress
priority: P1
effort: "1d"
dependencies: [1, 5]
---

# Phase 6: Review, xác minh live, docs

## Overview
Review code, chạy thật trong Revit 2026 + Excel, so với golden P1, cập nhật tài liệu.

## Requirements
- Functional: output live khớp golden, trừ mục ✦ đã ghi nhận ở P1/P2.
- Non-functional: không commit `.mcp.json` local; không đụng folder pyRevit ngoài repo.

## Architecture
```text
Revit đóng → build Debug.R26 (DeployAddin) → Revit 2026 mở model mẫu + Excel mở workbook Kata
 → hprebar-revit execute_revit_code: SetElementIds(dầm case) → UIA: HPRebar ▸ Rebar ▸ Kata Export
 → Ghi Excel → hprebar-excel read_range(B3:B10, C11:BZ23) → diff với golden
```

## Related Code Files
- Modify: `CLAUDE.md` (feature thứ 4 `KataExport/`, số test), `AGENTS.md` (regen bằng one-liner trong CLAUDE.md, `cmp` trước), `docs/codebase-summary.md`
- Create: `plans/260926-2317-kata-export-hprebar/reports/phase-06-live-verify.md`

## Golden chuẩn (người dùng chọn 2026-09-26)
Kata Pro có sẵn lệnh **Kata Tools ▸ Beam** (`Export_Kata_beam` → `xuat_excel_dam`) xuất dầm Revit → sheet `Dam`. 👤 Người dùng chạy lệnh này trên cùng các dải dầm (case a–f + 1 dải có vách làm gối), Save As từng workbook → Claude đọc `B3:B10`, `C11:BZ23` và so với KataExport. Mọi khác biệt phải được giải thích (✦ sửa có chủ ý) hoặc sửa code. Tool Dynamo cũ chỉ còn là nguồn phụ.

## Implementation Steps
1. `code-reviewer` agent trên diff → sửa mức cao → test lại.
2. Build + test đầy đủ (mục Success Criteria).
3. Live: mỗi case P1 (Normal + 1 Reverse); Revit hỏi add-in chưa ký → *Load Once* (`plans/260919-1910-materialdesign-xaml-adoption/reports/revit-answer-unsigned-addin.ps1`); mở ribbon theo `revit-open-bridge-window.ps1`. MCP không sẵn → checklist cho người dùng chạy tay.
4. Thử lỗi: Excel không chạy, workbook không có sheet `Dam`, Esc khi pick, dầm cong, view không có lưới.
5. 👤 Người dùng chạy macro Kata trên dữ liệu vừa ghi, xác nhận bản vẽ.
6. Docs + report `Kết quả Triển khai`.

## Success Criteria
- [ ] `dotnet build HPRebar/HPRebar.slnx -c Debug.R26` pass; `dotnet build HPRebar/HPRebar/HPRebar.csproj -c Debug.R24 -p:DeployAddin=false` pass
- [ ] `dotnet test HPRebar/HPRebar.Core.Tests` pass
- [ ] Live: mọi case khớp golden (trừ ✦); 5 đường lỗi báo đúng
- [ ] Không còn finding mức cao từ review; CLAUDE.md/AGENTS.md/docs cập nhật

## Risk Assessment
Revit đang mở khoá DLL → build `-p:DeployAddin=false`, nhờ người dùng đóng Revit trước khi deploy. Không có Kata thật → report ghi "Tested với golden Dynamo, chưa Verified bằng macro Kata".
