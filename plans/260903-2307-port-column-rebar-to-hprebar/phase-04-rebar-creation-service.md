---
phase: 4
title: "Rebar creation service (stirrup / main / add / dowels)"
status: pending
priority: P1
effort: "2d"
dependencies: [3]
---

# Phase 4: Rebar creation service

## Overview
Tạo thép thật trong model từ output Core: thép chủ (`CreateFreeForm` polyline), đai chính (`CreateFromRebarShape` + `ScaleToBox` + layout), đai phụ H/V, thép gia cường (AddBar). Command chạy với spec **mặc định hardcode** (chưa UI) → milestone F5 thứ hai: thép xuất hiện đúng vị trí.

## Requirements
- Functional: với `ColumnStack` + `BarLayoutSpec`/`SpliceSpec`/`StirrupSpec`/`AdditionalTieSpec` mỗi cột → Rebar elements; set param `Partition = ColumnsName`; `IProgress<int>` báo tiến độ.
- Non-functional: service **chỉ mở `Transaction`**, không bao giờ mở `TransactionGroup` (chủ sở hữu group là `ColumnRebarOrchestrator`, Phase 5 — quyết định D8). Thiếu RebarShape → fail sớm bằng `ValidationResult` trước khi mở transaction đầu tiên, không throw giữa chừng.

## Architecture
```
Column Rebar/
├── RebarTypeCatalog.cs        RebarBarType list sorted by diameter (mm), RebarCoverType list; port ColumnsModel.GetRebarBarType/GetRebarCoverType
├── RebarShapeResolver.cs      tên **hardcode** như gốc: M_T1 (rect), M_T3 (cyl), anti shape; thiếu → ValidationResult
├── PointMapper.cs             Core Point3 (mm, datum) → XYZ ; port BarModel.GetPoint + p0 (report §3)
├── MainBarCreator.cs          BarPolyline → CurveLoop → Rebar.CreateFreeForm ; port GetCurveLoop 3 nhánh
├── StirrupCreator.cs          origin/xVec/yVec + ScaleToBox + SetLayoutAsNumberWithSpacing theo StirrupRun[]
├── AdditionalTieCreator.cs    port #region Addtitional stirrup (StirrupModel.cs 233–873) — AddH/AddV, TypeH/V, nH/nV, aH/aV
├── RebarCreationService.cs    2 Transaction (Stirrup, Main) + progress ; port CreateRebar.cs (KHÔNG mở TransactionGroup)
└── Models/
    ├── RebarTypeInfo.cs       Name, DiameterMm, RebarBarType
    ├── ColumnRebarSpec.cs     per-column: BarLayoutSpec, SpliceSpec[], StirrupSpec, AdditionalTieSpec, RebarTypeInfo main/stirrup/tie
    └── CreatedRebar.cs        MainBars[], Stirrups[], Ties[] (Rebar refs cho Phase 5 tag/dim)
```

## Related Code Files
- Create: 8 file trên
- Modify: `ColumnRebarCommand.cs` — sau validate: build `ColumnRebarSpec` mặc định (nx=ny=2, bar index 3, stirrup index 0, cover = RebarCoverType[1], TypeDis 0, S = b/2) → `RebarCreationService.Create`
- Modify: `HPRebar.Tests/` — `RebarCreationServiceTests.cs`

## Implementation Steps
1. `RebarTypeCatalog`: `FilteredElementCollector.OfClass(RebarBarType)` → `RebarTypeInfo` với `DiameterMm = RevitUnits.FtToMm(REBAR_BAR_DIAMETER)`; sort. `RebarCoverType` sort `CoverDistance`, default `[1]` như gốc (guard `Count < 2` → `[0]`).
2. `RebarShapeResolver.Resolve(doc, sectionShape)` → tên hardcode `M_T1` (rect) / `M_T3` (cyl) / anti shape, giống gốc (gốc cho chọn shape trong Setting tab nhưng `ConditionButtonOK` ép về đúng 2 tên này → combobox là dead UI, **bỏ**, YAGNI). Trả `ValidationResult` khi thiếu, message "Please load Rebar Shape family M_T1" (gốc MessageBox trong model — chuyển lên đây).
3. `PointMapper` (per column0 datum): rect `p = p0 + X·East.n + Y·Nouth.n + Z·BasisZ`, cyl `p0 + X·BasisX + Y·BasisY + Z·BasisZ`; `p0` = `ProjectToPlane(West.Origin, South)` → `ProjectToPlane(·, datumFace)`. Port `PointModel.ProjectToPlane` vào đây. Tất cả `RevitUnits.MmToFt` tại đây.
4. `MainBarCreator.Create(doc, host, barType, polyline)`:
   - port `GetCurveLoop`: thẳng (mọi XY bằng nhau) → 1 Line; bẻ → nối từng đoạn; nhánh 3 điểm cuối trùng XY → 2 Line.
   - `Rebar.CreateFreeForm(doc, barType, host, [CurveLoop], out var result)`; nếu `result != Success` → log Serilog + throw `InvalidOperationException` (transaction rollback).
   - `LookupParameter("Partition")?.Set(name)` — null-safe (project có thể không có param).
5. `StirrupCreator.Create(doc, host, shape, barType, faces, section, cover, runs)`:
   - port `GetOriginVectorStirrupRectangle` / `GetScaleBoxStirrupRectangle` + bản cyl (report §7).
   - mỗi `StirrupRun` → 1 `Rebar`, rồi **qua accessor** (verified Phase 1 — 2 method này KHÔNG nằm trên `Rebar`):
     ```csharp
     var acc = rebar.GetShapeDrivenAccessor();          // RebarShapeDrivenAccessor
     acc.ScaleToBox(origin, xVec, yVec);
     acc.SetLayoutAsNumberWithSpacing(run.Count, RevitUnits.MmToFt(run.Spacing), true, true, true);
     ```
     Thép chủ tạo bằng `CreateFreeForm` thì dùng `rebar.GetFreeFormAccessor()` — overload chỉ có `(int, double)`.
     Signature giống hệt R23 → R27 (`reports/api-surface-check.md`).
6. `AdditionalTieCreator` — **đọc `StirrupModel.cs` dòng 233–830** khi cook (`#region Addtitional stirrup`, span đã verify). Giữ cấu trúc: origin vector anti H/V, `TypeH/TypeV` (0 = theo khoảng cách `aH`, 1 = theo số `nH`), shape anti.
7. `RebarCreationService.Create(doc, stack, specs, progress) → CreatedRebar`:
   ```
   // KHÔNG có TransactionGroup ở đây — orchestrator (Phase 5) sở hữu.
   Tx "Create Stirrup Bars"  → StirrupCreator + AdditionalTieCreator mỗi cột
   Tx "Create Main Bars"     → MainBarCreator mỗi polyline (main + add bars)
   ```
   Progress = tổng số rebar sẽ tạo (port `GetProgressBarRebar`). Exception trong `using var tx` → tx tự dispose (rollback); để exception bubble lên orchestrator xử lý `tg.RollBack()`.
8. Command tạm (chỉ tồn tại hết Phase 4, Phase 5 thay bằng orchestrator): tự mở `TransactionGroup` quanh `service.Create` → Assimilate. Spec mặc định → TaskDialog "Created N rebars". F5 verify trên Revit 2026: thép chủ 4 góc, đai đều, nối chồng bẻ xiên nếu cột trên nhỏ hơn.
9. TUnit (config `Debug.R26`): fixture 2 cột → `Create` → `FilteredElementCollector.OfClass(Rebar).Count == expected` (main 4×2 + stirrup runs); test tự bọc `TransactionGroup` + `RollBack` để không bẩn fixture.
10. Build gate `Debug.R26` + `Debug.R23`.

## Success Criteria
- [ ] F5 Revit 2026: 2 cột 400×600 → 300×500: 8 thép chủ, thanh cột dưới bẻ xiên dưới đáy dầm, neo lên cột trên 35d/70d so le; đai đều S = b/2
- [ ] Thiếu family `M_T1` → dialog rõ ràng, không exception, model không đổi
- [ ] Undo 1 lần xoá toàn bộ (Assimilate đúng)
- [ ] `grep -rn "TransactionGroup" "Column Rebar"` chỉ khớp ở `ColumnRebarCommand.cs` (tạm) — **không** trong `RebarCreationService`
- [ ] TUnit `RebarCreationServiceTests` pass
- [ ] Không có `#if REVIT` trong phase này (API đã verify chung R23–R26); nếu R27 cần → helper + `// Multi-version:`

## Risk Assessment
- **`CreateFreeForm` validation fail** (curve loop không hợp lệ, đoạn quá ngắn) → log `RebarFreeFormValidationResult` + đơn giản hoá polyline (bỏ đoạn < 1 mm) trong `MainBarCreator`.
- **Hướng `SetLayoutAsNumberWithSpacing(barsOnNormalSide)`** sai → đai rải ngược Z; plan cũ đã ghi rủi ro này; verify F5 và flip bool nếu cần.
- **AddH/AddV logic 640 dòng chưa đọc** → nếu phức tạp hơn dự kiến, tách thành Phase 4.5 riêng, không nhồi.
- **`Partition` param không tồn tại** → null-safe, không fail.
