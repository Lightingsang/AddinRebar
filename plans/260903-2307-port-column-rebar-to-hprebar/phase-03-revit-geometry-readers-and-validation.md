---
phase: 3
title: "Revit geometry readers + validation (fix 6 bug) + TUnit"
status: pending
priority: P1
effort: "2d"
dependencies: [1, 2]
---

# Phase 3: Revit geometry readers + validation + TUnit

## Overview
Port lớp đọc hình học (`SolidFace`, `ColumnsBoundingBox`, `ProccessInfoClumns`, `InfoModel`) và validator 15 rule (`ErrorColumns`) sang service Revit trong feature folder, output là `ColumnStack` (Core `ColumnSection[]` + PlanarFace refs). Sửa 6 bug B1–B6. Command chạy được end-to-end tới bước validate (chưa tạo gì) — milestone F5 đầu tiên có ý nghĩa.

## Requirements
- Functional: pick N cột → sort Z → validate → `ColumnStack` với b/h/D/hc/hb/zb/positions bằng mm; lỗi báo qua `TaskDialog` với message EN/VN.
- Non-functional: mọi hàm đọc geometry pure (không Transaction); không `MessageBox` trong service; file < 300 dòng.

## Architecture
```
Column Rebar/
├── ColumnRebarCommand.cs             pick → sort → validate → (tạm) TaskDialog tóm tắt stack
├── StructuralColumnSelectionFilter.cs
├── ColumnSolidFaceReader.cs          port SolidFace: GetSolid, Top/Bottom, S/N/W/E, Cylindricals, BeamHorizontalFaces
├── ColumnNeighbourFinder.cs          port ColumnsBoundingBox: beams same top/base level, foundation/floor/wall under column0
├── ColumnStackReader.cs              port ProccessInfoClumns + InfoModel → ColumnStack
├── ColumnStackValidator.cs           port ErrorColumns 14 rule (bỏ 15) → ValidationResult
└── Models/
    ├── ColumnFaces.cs                Element, Top, Bottom, South, North, West, East (PlanarFace), Cylindricals, LocationPoint?
    ├── ColumnStack.cs                Sections (Core), Faces[], DatumFace (planarFace0), South0, West0, DimensionFaces (PlanarFaces list), Shape
    └── ValidationResult.cs           record Code(int), IsOk, MessageKey
```
`ColumnStackReader` là chỗ **duy nhất** gọi `RevitUnits.FtToMm` khi build `ColumnSection`. Từ đó về sau Core chỉ thấy mm.

## Related Code Files
- Create: 8 file trên
- Create: `HPRebar/HPRebar.Tests/` — `dotnet new revit-tunit --name HPRebar.Tests` (short name đúng là **`revit-tunit`**, verified `dotnet new list revit`), `<Configurations>Debug.R25;Debug.R26;Release.R25;Release.R26</Configurations>` (chỉ 2 version có Revit trên máy), thêm `.slnx` mapping
- Create: `HPRebar/HPRebar.Tests/ColumnStackReaderTests.cs`, `ColumnStackValidatorTests.cs`
- Create: `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt` — cook tạo tay trong **Revit 2026** (2 cột 400×600 → 300×500 lệch tâm, dầm 300×500 đỉnh cột dưới, móng dưới cột đáy, đã load rebar shape `M_T1`/`M_T3`)
- Create: `.gitattributes` — `*.rvt filter=lfs diff=lfs merge=lfs -text` (git-lfs 3.7.1 đã cài). Cùng lúc thêm `tests/skill-sync/fixtures/** text eol=lf` để dứt điểm 10 test CRLF fail (xem CLAUDE.md § Known environment failure)
- Modify: `ColumnRebarCommand.cs`

## Implementation Steps
1. `ColumnSolidFaceReader` — port nguyên điều kiện góc (report §2). Sửa **B3**: `GetTopPlanarFaceBeam` trả về `.OrderBy(f => f.Origin.Z).ToList()` thật. `GetSolid` trả `Solid?` + throw `InvalidOperationException` nếu ≠ 1 solid (validator kiểm trước).
2. `ColumnNeighbourFinder` — port 5 hàm bounding box. Sửa **B5**: `GetWallUnderColumn` so `level == level0.Id`. Sửa **B3**: `levels.OrderBy(...)` gán lại. Dùng `FilteredElementCollector` chain, không materialize giữa chừng.
3. `ColumnStackReader.Read(doc, columns) → ColumnStack`:
   - `FindDatumFace` port `GetPlanarFace0` (chain Foundation → Floor → Wall → Beam). Sửa **B3** ở 4 nhánh: dùng `.MinBy(f => f.Origin.Z)` (Polyfill có `MinBy`) thay vòng for + OrderBy no-op.
   - Với mỗi cột: `ColumnFaces` + `ColumnSection` (mm) — port ctor `InfoModel` 2 overload; `hb/zb` port `GetBeams/GetHb`. Sửa **B6**: mọi khoảng cách `RevitUnits.FtToMm(Math.Abs((p - face.Origin).DotProduct(face.FaceNormal)))`.
   - `DimensionFaces` port `GetPlanarFaces` (list mặt dùng cho dimension Phase 5), sửa B3.
4. `ColumnStackValidator.Validate(doc, columns) → ValidationResult`:
   - Sửa **B1**: `foreach rule → if fail return ValidationResult.Fail(code)`; thứ tự giữ nguyên 1→14.
   - Sửa **B2**: `columns.All(IsVertical)`.
   - Bỏ rule 15 `CompareDecimal` (không còn parse string).
   - Rule 6–14 chưa đọc chi tiết — cook phải đọc `ErrorColumns.cs`, line span đã verify: `RotateColumns` 102, `CompareProperty` 136, `CompareOutSide` 181, `CompareOverTopPlanarFaceBeam` 241, `JoinBeamsToColumns` 265, `JoinColumnsToFoundation` 288 → `ErrorString` 399.
   - `MessageKey` → string EN/VN tra ở `LocalizationService` (Phase 6); tạm thời dictionary EN trong validator.
5. `ColumnRebarCommand.Execute`:
   ```csharp
   var refs = UiDocument.Selection.PickObjects(ObjectType.Element, new StructuralColumnSelectionFilter());
   var columns = refs.Select(r => Document.GetElement(r))
                     .OrderBy(e => reader.BottomFace(e).Origin.Z).ToList();
   var result = validator.Validate(Document, columns);
   if (!result.IsOk) { RevitDialogs.Error("Column Rebar", result.Message); return; }
   var stack = reader.Read(Document, columns);
   RevitDialogs.Info("Column Rebar", stack.Summary());   // tạm, Phase 6 thay bằng window
   ```
   `OperationCanceledException` khi user ESC → return im lặng.
6. Fixture: tạo `.rvt` trong Revit 2026 theo spec ở Related Code Files; thêm `.gitattributes` LFS **trước** khi commit file (nếu commit trước, phải `git lfs migrate`).
7. TUnit: `dotnet new revit-tunit --name HPRebar.Tests`, giới hạn `<Configurations>` R25/R26. Test: mở fixture, lấy 2 cột theo tên, `Read` → assert `Sections[0].B == 400`, `Hc`, `Hb == 500`, `Zb`, `Sections[1].WestPosition == 50` (lệch tâm). Validator: cột không thẳng đứng → code 2; 2 cột không liên tục → code 4 (test này chứng minh B1 đã sửa: trước đây trả code cuối cùng).
8. Build gate `Debug.R26` + `Debug.R23`. F5 Revit 2026 → pick 2 cột → dialog tóm tắt đúng số.

## Success Criteria
- [ ] 6 bug B1–B6 không còn (grep pattern: `OrderBy(` không gán, `Equals(level0)`, `double.Parse(UnitFormatUtils`)
- [ ] TUnit ≥ 4 test pass trên **Revit 2026** (config `Debug.R26`)
- [ ] F5: pick 2 cột hợp lệ → summary b/h/hc/hb/zb khớp model; pick cột nghiêng → lỗi code 2 đúng (không phải code cuối)
- [ ] Không `System.Windows.Forms`, không `MessageBox` trong feature folder
- [ ] `.rvt` được LFS track (`git lfs ls-files` có kết quả)

## Risk Assessment
- **TUnit chỉ chạy R25/R26** — R23/R24/R27 không có Revit trên máy → build-only. Ghi rõ trong Phase 8 report, không coi là đã verify runtime.
- **Fixture `.rvt` ~5 MB** → LFS. Nếu user có sẵn file từ tool gốc (kèm family `M_T1`/`M_T3`) thì dùng, tiết kiệm ~1h dựng model.
- **Family cột user dùng có nhiều solid** (rule 3) → giữ hành vi gốc (reject), ghi chú UX trong dialog.
- **`MinBy` trên net48** → Polyfill cung cấp; nếu conflict với `System.Linq` net8 → dùng `OrderBy().First()`.
