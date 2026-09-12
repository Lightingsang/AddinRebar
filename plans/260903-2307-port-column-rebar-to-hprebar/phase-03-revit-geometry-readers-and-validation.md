---
phase: 3
title: "Revit geometry readers + validation (fix 6 bug) + TUnit"
status: partial
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
- [x] 6 bug B1–B6 không còn (grep pattern: `OrderBy(` không gán, `Equals(level0)`, `double.Parse(UnitFormatUtils`)
- [ ] **BLOCKED** TUnit: 11 test viet xong + build pass, nhung **skip het** vi chua co fixture `.rvt` (phai dung tay trong Revit 2026)
- [ ] **BLOCKED (can user)** F5: pick 2 cột hợp lệ → summary b/h/hc/hb/zb khớp model; pick cột nghiêng → lỗi code 2 đúng (không phải code cuối)
- [x] Không `System.Windows.Forms`, không `MessageBox` trong feature folder (grep CLEAN)
- [ ] **BLOCKED** `.rvt` được LFS track — `.gitattributes` da tao, `git check-attr` xac nhan rule dung, chi thieu file (`git lfs ls-files` có kết quả)

## Risk Assessment
- **TUnit chỉ chạy R25/R26** — R23/R24/R27 không có Revit trên máy → build-only. Ghi rõ trong Phase 8 report, không coi là đã verify runtime.
- **Fixture `.rvt` ~5 MB** → LFS. Nếu user có sẵn file từ tool gốc (kèm family `M_T1`/`M_T3`) thì dùng, tiết kiệm ~1h dựng model.
- **Family cột user dùng có nhiều solid** (rule 3) → giữ hành vi gốc (reject), ghi chú UX trong dialog.
- **`MinBy` trên net48** → Polyfill cung cấp; nếu conflict với `System.Linq` net8 → dùng `OrderBy().First()`.

## Cook notes (2026-09-04)

### Da xong
Build gate `Debug.R26` / `R23` / `R27` = **0 error, 0 warning**. Core test 99/99 van xanh.

| File moi | Dong | Port tu |
|---|---|---|
| `ColumnSolidFaceReader.cs` | 207 | `SolidFace.cs` |
| `ColumnNeighbourFinder.cs` | 121 | `ColumnsBoundBox.cs` |
| `ColumnStackReader.cs` | 214 | `ProccessInfoClumns.cs` + `InfoModel.cs` |
| `ColumnStackValidator.cs` | 295 | `ErrorColumns.cs` (14 rule) |
| `StructuralColumnSelectionFilter.cs` | 15 | filter goc |
| `Models/` x5 | 10-72 | moi |

### 6 bug — vi tri that trong source
| Bug | Vi tri that | Trang thai |
|---|---|---|
| B1 | `ErrorColumns.GetErrorColumns` gan `error =` 15 lan khong `return` -> tra loi **cuoi cung** | Sua: bang rule + `return` ngay lan fail dau |
| B2 | `IsnotVerticalColumns` `return` trong vong `for` -> chi check `columns[0]` | Sua: `columns.All(IsVertical)` |
| B3 | 5 cho `OrderBy` khong gan: `SolidFace.GetTopPlanarFaceBeam`, `ColumnsBoundBox.GetWall/GetFloor` (levels), `ProccessInfoClumns.GetPlanarFaces`, `InfoModel.GetBeams` | Sua het |
| B4 | `BarMainModel.cs:61` `Bar = Bar;` | Khong ton tai o Core (Phase 2) |
| B5 | `GetWallBoudingBoxOneColumn`: `!level.Equals(level0)` so `ElementId` voi `Level` -> **luon true** -> wall luon null | Sua: so `lowest.Id` |
| B6 | `PointModel.DistanceTo2` + 6 cho trong `InfoModel` dung `double.Parse(UnitFormatUtils.Format(...))` | Sua: `RevitUnits.FtToMm` |

**Ghi chu B3 chinh xac hon plan:** trong `GetPlanarFace0` cai `OrderBy` no-op **vo hai** vi ngay sau do co vong `for` tim min bang tay. Bug that nam o `GetPlanarFaces` (lay `[0]` va `[Count-1]` tu list chua sort) va `InfoModel.GetBeams` (`zb` doc tu `planarFaces[0]` chua sort -> lay mat dam bat ky chu khong phai mat thap nhat). Sua B3 o `GetBeams` **doi ket qua** `zb`/`hb` — dung y do, khong phai no-op.

### 4 dieu khac plan
1. **`ElementIdCompat` van khong can.** Filter dung `Category.BuiltInCategory` (co o ca R23 lan R27, verified XML) thay `Category.Id` -> ne split `ElementId.Value` / `IntegerValue`.
2. **`ExternalCommand.UiDocument` / `Document` / `ActiveUiDocument` / `ActiveDocument` deu `[Obsolete]`** trong Toolkit 2026. Dung `Application.ActiveUIDocument` + local `document`. Build sach 0 warning.
3. **`HPRebar.Tests` phai `<Build Project="false"/>` trong `.slnx`.** Map R23/R24/R27 -> R26 lam solution build `Debug.R23` compile test (net8) doi ref `HPRebar` (net48, Polyfill nhung `Span<T>` vao assembly) -> `CS0433 ReadOnlySpan<T> exists in both`. TUnit chay trong Revit nen chay rieng theo project path.
4. **XML comment trong `.slnx` khong duoc chua `--`** (`dotnet run --project`) -> `MSB4025`.

### `.gitattributes` — sua duoc 4/10 test, khong phai 10
Tao `.gitattributes` (LFS cho `*.rvt`/`*.rfa`/`*.rte` + `tests/skill-sync/fixtures/** text eol=lf`), roi re-checkout 70 file fixture de vat chat hoa LF.

`tests/skill-sync`: **41 test, tu 4 failure + 6 error -> 0 failure + 6 error.**

6 error con lai **KHONG phai CRLF** — `CLAUDE.md` chan doan sai. Root cause that la **Windows 8.3 short path**: `tempfile.gettempdir()` tra dang 8.3 (`STR-HP~1.HOA`) trong khi `Path.resolve()` tra dang dai (`STR-HP03.HOANGPHUC`), nen `relative_to` throw `ValueError: ... is not in the subpath of ...`. La bug that cua engine `skill_sync` (phai resolve ca 2 ve cung dang truoc khi `relative_to`), nhung la **deliverable khac**, ngoai scope Phase 3. Chua sua. `CLAUDE.md` § Known environment failure can cap nhat o Phase 8.

### Con BLOCKED — can Revit 2026 thu cong
`HPRebar.Tests` (TUnit) build pass, 11 test viet xong (6 reader + 5 validator), nhung **skip het** qua `Skip.Unless` vi thieu `Fixtures/column-stack-2-storey.rvt`.

Spec day du cua model da viet o **`HPRebar/HPRebar.Tests/Fixtures/README.md`**: 3 level, 6 cot co Mark (`C1-LOWER`, `C1-UPPER`, `C-SLANTED`, `C2-DETACHED`, `C3-LOWER`, `C3-WIDER`), 1 mong, 1 dam 300x500 o dinh `C1-LOWER`, load shape `M_T1`/`M_T3`. Dung xong luu vao `Fixtures/`, LFS tu bat.

Chua chay thu TUnit vi **Revit 2026 dang mo** (PID 1836 khoa `HPRebar.dll` — moi build phai them `-p:DeployAddin=false`). Khong tu spawn Revit thu 2 de tranh pha session dang mo cua user.
