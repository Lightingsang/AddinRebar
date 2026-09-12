---
phase: 4
title: "Rebar creation service (stirrup / main / add / dowels)"
status: partial
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
- [ ] **BLOCKED (can user + fixture)** F5 Revit 2026: 2 cột 400×600 → 300×500: 8 thép chủ, thanh cột dưới bẻ xiên dưới đáy dầm, neo lên cột trên 35d/70d so le; đai đều S = b/2
- [ ] **BLOCKED (runtime)** Thiếu family `M_T1` → dialog rõ ràng — code path co (`RebarShapeResolver.Require` chay TRUOC transaction dau tien), không exception, model không đổi
- [ ] **BLOCKED (runtime)** Undo 1 lần xoá toàn bộ (Assimilate đúng)
- [x] `grep -rn "TransactionGroup" "Column Rebar"` chỉ khớp ở `ColumnRebarCommand.cs` (tạm) — **không** trong `RebarCreationService`
- [ ] **BLOCKED** TUnit `RebarCreationServiceTests`: 5 test viet xong + build pass, skip het vi thieu fixture
- [x] ~~Không có `#if REVIT` trong phase này~~ **SAI GIA DINH — can 1 block**, xem ghi chu (API đã verify chung R23–R26); nếu R27 cần → helper + `// Multi-version:`

## Risk Assessment
- **`CreateFreeForm` validation fail** (curve loop không hợp lệ, đoạn quá ngắn) → log `RebarFreeFormValidationResult` + đơn giản hoá polyline (bỏ đoạn < 1 mm) trong `MainBarCreator`.
- **Hướng `SetLayoutAsNumberWithSpacing(barsOnNormalSide)`** sai → đai rải ngược Z; plan cũ đã ghi rủi ro này; verify F5 và flip bool nếu cần.
- **AddH/AddV logic 640 dòng chưa đọc** → nếu phức tạp hơn dự kiến, tách thành Phase 4.5 riêng, không nhồi.
- **`Partition` param không tồn tại** → null-safe, không fail.

## Cook notes (2026-09-04)

### Build gate
`Debug.R23` / `R24` / `R25` / `R26` / `R27` = **0 error, 0 warning**. Core test 99/99. `HPRebar.slnx` R26 + R23 sach.

### PHAT HIEN LON: `Rebar.CreateFreeForm` KHONG tuong thich R23 -> R27

Plan viet "Khong co `#if REVIT` trong phase nay (API da verify chung R23-R26)". **Gia dinh sai.** Build R26 canh bao `CS0618`, dao sau ra:

| Overload | R23 | R24 | R25 | R26 | R27 |
|---|---|---|---|---|---|
| `(Document, RebarBarType, Element, IList<CurveLoop>, out RebarFreeFormValidationResult)` -> `Rebar` | co | co | co | **deprecated** | **BI XOA** |
| `(Document, RebarBarType, Element, IList<CurveLoop>, RebarStyle)` -> `RebarFreeFormCreationResult` | khong | khong | khong | **moi them** | co |

Khac ca **signature lan return type**. Ban moi tra `RebarFreeFormCreationResult` (co `.Rebar` + `.Error`). **Khong co cach goi nao compile duoc ca 5 version** -> `MainBarCreator.cs` phai co 1 block `#if REVIT2026_OR_GREATER` (kem comment `// Multi-version:`). `RebarStyle.Standard`/`StirrupTie` co o moi version nen doi so khong can guard.

**Tai sao API sweep Phase 1 khong bat duoc:** sweep chi so **ten** type/member, khong so signature — dung nhu muc Caveats cua `reports/api-surface-check.md` da canh bao. Da them muc "Correction" vao report do. `CreateFromRebarShape` re-check full signature: **giong het** R23/R26/R27, khong can guard.

### File tao (11, plan de xuat 8)
| File | Dong | Ghi chu |
|---|---|---|
| `RebarTypeCatalog.cs` | 63 | bar type sort theo duong kinh + cover default `[1]` |
| `RebarShapeResolver.cs` | 84 | ten hardcode `M_T1`/`M_T3`/`M_T10*`; `Require()` chay truoc transaction |
| `PointMapper.cs` | 50 | Core mm -> XYZ; goc = goc Tay-Nam cot day chieu len datum |
| `MainBarCreator.cs` | 139 | `CreateFreeForm` + 3 nhanh curve loop + `#if` multi-version |
| **`StirrupGeometry.cs`** | 206 | **MOI** — tach 8 ham origin/scale-box ra khoi 2 creator (neu gop se > 400 dong) |
| `StirrupCreator.cs` | 63 | dai chinh |
| `AdditionalTieCreator.cs` | 238 | dai phu H/V, rect + cyl |
| `RebarCreationService.cs` | 211 | 2 Transaction, khong Group |
| **`DefaultRebarSpecBuilder.cs`** | 64 | **MOI** — spec mac dinh, plan de trong command |
| `Models/RebarTypeInfo.cs` `ColumnRebarSpec.cs` `CreatedRebar.cs` | 13/28/20 | |

### 2 bug trong source giu nguyen / da sua
1. **`CreateAddVerticalStirrupRectangleType1Item` dung `BarH` thay vi `BarV`** cho shape, bar type, list va partition (StirrupModel.cs:456-471). Ro rang la copy-paste: ham ten "Vertical" nhung ca 4 cho deu tro sang thanh Horizontal. **Da sua** trong port (`AdditionalTieCreator.RectangleVertical` dung dung tie bar type) vi neu giu se tao dai phu doc bang shape cua dai phu ngang -> sai hinh hoc thay ro. Neu user muon giong het ban goc thi noi.
2. **So hieu leg style lech 1 giua rect va cyl.** Rect: `TypeH/V` 1 -> `M_T10B`, 2 -> `M_T10`, 3 -> `M_T10C`. Cyl: `TypeV` 0 -> `M_T10B`, 1 -> `M_T10`, 2 -> `M_T10C`. Giu nguyen (port cong 1 truoc khi tra) va ghi comment.

### Khac plan
- Plan buoc 1 noi bar default "index 3"; da them guard: it hon 4 loai thi lay day nhat, khong crash.
- `SetPartitionRebar` goc goi `LookupParameter("Partition").Set(...)` khong null-check -> `NullReferenceException` neu template khong co param. Port dung `is { IsReadOnly: false }`.
- Progress dung `IProgress<int>` (plan yeu cau) nhung command chua noi vao UI — Phase 6.

### Con BLOCKED
5 TUnit test `RebarCreationServiceTests` (dem thanh, planned vs actual, rollback sach, shape check, host dung) build pass nhung **skip het** — van thieu `Fixtures/column-stack-2-storey.rvt`. F5 + kiem tra undo/thieu-family cung can Revit.
