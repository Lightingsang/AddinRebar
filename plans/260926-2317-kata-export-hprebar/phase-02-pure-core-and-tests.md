---
phase: 2
title: "Pure core + xUnit"
status: completed
priority: P1
effort: "1d"
dependencies: []
---

# Phase 2: Pure core `HPRebar.Core.KataExport` + xUnit

## Overview
Toàn bộ logic từ dữ liệu đã chuẩn hoá (mm, trạm `s` dọc trục) đến `KataSheet` (giá trị ô) — không tham chiếu Revit, test được bằng xUnit.

## Requirements
- Functional: segmenter 1D (hợp gối, phần bù = nhịp, gối 0 ở điểm nối, cờ console F/E); row builder theo hợp đồng P1; Normal/Reverse; định dạng `"bxh"`, `"w;e"`; giới hạn 76 cột (C..BZ).
- Non-functional: netstandard2.0, Nullable, file < 300 dòng, `sealed record` cho model; namespace `HPRebar.Core.KataExport.Models|Calculators`; Tolerance riêng của feature (theo tiền lệ `BeamRebar/Tolerance.cs`).

## Architecture
- `Models/`: `Interval1D` (Start, End, Length, Mid, Overlaps, Contains), `KataBeamPiece` (Interval, B, H, ZOffset, ElementKey), `KataSupport` (Kind Column|Foundation|Beam, Interval, SectionText?, Upper Interval?), `KataGridCrossing` (Name, S), `KataHeader`, `KataRunInput`, `KataSegment` (IsSupport, Interval, Support?, Piece?), `KataSheet` (Header cells B3..B10, Rows 11/19/21/22/23 as `object?[]`, Warnings), `Enums`.
- `Calculators/KataSegmenter`: sort + union khoảng gối (ưu tiên Column > Foundation > Beam), clip theo `[min s0, max s1]` của pieces, phần bù → nhịp, chèn gối 0 tại điểm nối pieces không nằm trong gối.
- `Calculators/KataRowBuilder`: nhịp lấy piece chứa `Mid` (gán theo hình học, không theo chỉ số); gối dầm lấy `SectionText` của chính gối; grid gán cho gối có `S ∈ Interval`; Reverse = đảo thứ tự + đổi dấu lệch (hàng 19 lệch ×−1, hàng 23 và phần gối hàng 21 ×−1).
- `Calculators/KataFormat`: `Math.Round(x, MidpointRounding.AwayFromZero)`, chuỗi định dạng.

## Related Code Files
- Create: `HPRebar/HPRebar.Core/KataExport/Models/*.cs`, `Calculators/KataSegmenter.cs`, `Calculators/KataRowBuilder.cs`, `Calculators/KataFormat.cs`, `Tolerance.cs`
- Create: `HPRebar/HPRebar.Core.Tests/KataExport/KataSegmenterTests.cs`, `KataRowBuilderTests.cs`, `KataGoldenTests.cs`, `TestKataData.cs`

## Implementation Steps
1. Viết test trước (TDD) từ các case của report: console F/E (4 tổ hợp), Reverse, gối dầm 2 tiết diện khác nhau, 1 piece vắt qua nhiều gối, gối chồng nhau, lưới nằm ngoài gối, > 76 cột → lỗi rõ.
2. Models + Interval1D.
3. KataSegmenter → pass test segment.
4. KataRowBuilder + KataFormat → pass test rows.
5. Golden: khi P1 xong, `KataGoldenTests` dựng `KataRunInput` từ fixture (tay, theo hình học model) và so từng ô; khác biệt ✦ assert theo giá trị đúng mong đợi.

## Success Criteria
- [ ] `dotnet test HPRebar/HPRebar.Core.Tests` pass (337 cũ + mới)
- [ ] Mỗi lỗi Dynamo đã sửa (R3, R4, h ≥ 900) có test hồi quy tên mô tả kịch bản

## Tiến độ (2026-09-26)
- ✅ Models (`Interval1D`, `KataRunModels`, `KataSegmentModels`), `KataInputValidator`, `KataSegmenter`, `KataRowBuilder`, `KataFormat`, `KataTolerance` (đổi tên từ `Tolerance` để không đụng `HPRebar.Core.BeamRebar.Tolerance`).
- ✅ 40 test (`KataSegmenterTests` 8, `KataRowBuilderTests` 14, `KataEdgeCaseTests` 18); `dotnet test HPRebar.Core.Tests` 377/377; build Core 0 warning; solution Debug.R26 build pass.
- ✅ Review [code-review-phase-02-pure-core.md](reports/code-review-phase-02-pure-core.md) 7/10 → đã sửa H1 (validator NaN/∞/kích thước ≤ 0/null), M1 (joint chỉ nơi phần tử nối tiếp, không có phần tử chạy qua), M2 (`Warnings`: khe hở, chồng dầm, gối ngoài dải, nhiều trục/gối), M3 (`JointSnapMm` 50), M4 (18 test biên), L1 (gộp gối theo `StationMm`), L2 (h₁ = header), L3 (đếm cột sau khi dựng), L4 (tên), L5 (lỗi rõ).
- ✅ Theo Kata.xlsm: `KataHeader` thêm `SlabThicknessMm`/`AxisGridName`/`AxisOffsetMm` (null → 0 / "" / −b/2 + warning); `KataSupport.CrossingBeamStationMm` (gộp từ dầm giao trùng cột) → hàng 21 gối; `KataText` + `KataFormat.Elevation` → B10 `"+3.300"`. 384/384.
- 🟡 `KataGoldenTests` — chuyển P6 nếu có golden. 76 cột đúng không đạt được (số cột luôn lẻ) → test 75 pass / 77 throw.
- Ghi chú: warnings bằng tiếng Anh ở core; P5 hiển thị/việt hoá trên UI.

## Risk Assessment
Hợp đồng đổi sau P1 → chỉ sửa `KataRowBuilder` + test; segmenter độc lập với layout ô.
