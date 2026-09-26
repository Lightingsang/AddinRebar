---
title: "KataExport — xây lại tool Dynamo 'Kata Export to Excel' thành feature C# trong HPRebar"
description: "Dải dầm thẳng → chuỗi gối/nhịp (1D) → preview → ghi workbook Kata đang mở (sheet Dam, B3:B10 + hàng 11/19/21/22/23) qua COM. Giữ layout ô, sửa lỗi logic của graph Dynamo."
status: in-progress
priority: P2
effort: 5.5d
branch: RebarVersion1
tags: [revit, hprebar, feature, excel, com, wpf, beam]
created: 2026-09-26
blockedBy: []
blocks: []
related: [260907-1410-port-beam-rebar-to-hprebar]
---

# KataExport — Dynamo "Kata Export to Excel" → C# HPRebar

> Đây là bước lập kế hoạch. Chưa thay đổi source code.

Nguồn: [analysis-260926-drawingkata-dynamo-export-excel.md](../reports/analysis-260926-drawingkata-dynamo-export-excel.md) (mục 3 bước, mục 4 schema, mục 6 rủi ro R1–R21).

## Quyết định đã chốt (người dùng, 2026-09-26)
- C# HPRebar add-in, feature folder `KataExport/` · chỉ verify **R26** (không dùng API mới hơn R23) · ghi COM vào **workbook Kata đang mở** · **giữ layout ô, sửa lỗi logic**.
- Ngoài phạm vi: folder pyRevit `HPSTRGenaralTool.extension` (ngoài repo), prototype comp1 của Dynamo, dầm cong/gãy khúc.

## Kiến trúc tóm tắt
```text
Ribbon Rebar ▸ Kata Export → Command: selection | PickObjects
 → Service: RunReader + SupportCollector + UpperColumnFinder + GridReader → KataRunInput (mm, trạm s)
 → Core (pure): KataSegmenter → KataRowBuilder → KataSheet → Window preview
 → KataExcelWriter (COM P/Invoke + InvokeMember) → sheet Dam → Highlight dầm (ExternalEvent)
```
Tái dùng pattern (không tham chiếu chéo feature): `BeamStackReader.cs:21-46`, `BeamSupportFinder.cs:311-409`, `BeamSolidFaceReader.cs:170-184`, `ColumnNeighbourFinder.cs:14-28`, `BeamRebarCommand.cs:28-112`, `BeamRebarExternalEventHandler.cs:24-78`, `HPExcel/HPExcel.McpBridge/Com/ComInteropHelper.cs:17-47`.

## Phases
| # | Phase | Status | Effort | Gate |
|---|---|---|---|---|
| 1 | [Spec + golden data](phase-01-spec-and-golden-data.md) | completed (chốt bằng `C:\kata_pro\Kata.xlsm`; golden → P6 tuỳ chọn) | 0.5d + 👤 | fixture + 5 điểm mở chốt |
| 2 | [Pure core + xUnit](phase-02-pure-core-and-tests.md) | completed (47 test; 385/385 pass) | 1d | `dotnet test HPRebar.Core.Tests` |
| 3 | [Đọc dữ liệu Revit](phase-03-revit-data-extraction.md) | completed (review 6.5→fixed; build R26 + R24 pass; chạy Revit CHƯA TEST) | 1.5d | build Debug.R26 |
| 4 | [Excel COM writer](phase-04-excel-com-writer.md) | in-progress (code + review 7→fixed, build pass; ghi thử vào workbook CHƯA TEST) | 0.5d | build + workbook nháp |
| 5 | [UI + ribbon](phase-05-ui-and-ribbon.md) | completed (build R26/R24, ThemeTokenCoverage, icon preview 16/32/64; chạy Revit CHƯA TEST) | 1d | build + ThemeTokenCoverageTests |
| 6 | [Verify + docs](phase-06-verify-and-docs.md) | in-progress (4 lần chạy thật: T1-DX12 ×2, GMX3 ×2 — mọi ô khớp, B10 text, sửa giằng móng không đổi kết quả — [report](reports/phase-06-live-verify.md); còn đường lỗi Excel, vách, ghi Reverse) | 1d | live Revit 2026 khớp golden |
| 7 | [Mặt đứng dải dầm](phase-07-elevation-view.md) | completed (review 8→fixed, 444 test, gallery 22/22; live GMX3 + T1-DX12 ✅ 2026-09-27) | 1.5d | test + gallery dark/light + live |
| 8 | [Zoom/pan 2 chiều kiểu CAD](phase-08-canvas-zoom-pan-2d.md) | completed (review 8→fixed, 448 test, gallery 22/22, live GMX3 ✅ 2026-09-27) | 0.5d | test viewport + gallery + live |

P2 chạy song song P1 (test tổng hợp từ report phân tích; golden cắm vào khi P1 xong). P3–P5 tuần tự.

## Dependencies
- Cross-plan: không blocking. `260907-1410-port-beam-rebar-to-hprebar` cũng sửa `Application.cs` (panel Rebar) → chỉ chung file, thêm 1 dòng.
- 👤 P1 cần file Kata `.xlsm` mẫu + chạy tool Dynamo cũ lấy golden; P6 cần chạy macro Kata trên output.

## Quyết định bổ sung (người dùng, 2026-09-26, sau khi đọc Kata.xlsm)
- B7 h sàn / B8 tên trục dầm / B9 độ lệch trục: **tính từ model**, không thấy → giá trị Dynamo (0 / "" / −b/2) + cảnh báo.
- Hàng 21 tại gối: **lệch dầm giao thật so với tâm cột**, không có dầm giao → lệch lưới.
- Hàng 19 tại gối: giữ `"rộng cột trên;lệch"`. B10: **text** `+3.300`.
- Kata Pro đã có lệnh Revit → `Dam` (`Export_Kata_beam`, [kata-pro-revit-dll-reference.md](reports/kata-pro-revit-dll-reference.md)) → **vẫn xây tool riêng, dùng lệnh Kata làm golden ở P6**. **Vách kết cấu = gối** như Kata.

## Rủi ro chính
Quy ước dấu B9 và hàng 19/21 tại gối chưa đối chiếu bản vẽ Kata thật → P6 kiểm bằng chạy macro Kata; không chặn P3–P5.

## Điều kiện hoàn thành
- [ ] Build Debug.R26 pass; Debug.R24 compile pass (pipeline net48 không gãy)
- [ ] `dotnet test HPRebar.Core.Tests` pass (337 cũ + KataExport + theme)
- [ ] Live Revit 2026: output khớp golden trừ mục ✦ đã ghi nhận
- [ ] code-reviewer không còn lỗi mức cao; CLAUDE.md/AGENTS.md/docs cập nhật
