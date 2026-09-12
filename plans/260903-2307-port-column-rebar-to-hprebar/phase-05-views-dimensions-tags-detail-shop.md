---
phase: 5
title: "Views + dimension + tag + detail shop + orchestrator"
status: partial
priority: P2
effort: "1.5d"
dependencies: [4]
---

<!-- Updated: Validation Session 1 — đường IsRebar=false (Detail Item DT*) tách sang plan riêng; orchestrator sở hữu TransactionGroup (D8) -->

# Phase 5: Views + dimension + tag + detail shop + orchestrator

## Overview
Port 4 output annotation: 2 detail view (X/Y), 1 section view mỗi cột, dimension trên detail + section, bảng tag (TextNote + DetailCurve), detail shop family (`DS*`) + schedule. Dựng `ColumnRebarOrchestrator` — **chủ sở hữu duy nhất của `TransactionGroup`** (quyết định D8), là toàn bộ hành động nút OK.

**Ngoài scope (đã tách, Validation Session 1):** đường `IsRebar = false` — thay rebar thật bằng Detail Item family `DT00..DT07A` (`CreateRebarDetailtem` + `ProcessDetailItem` 255 dòng + `DetailItem` 291 dòng ≈ 700 dòng). Lý do: phụ thuộc family riêng của tác giả gốc mà repo không có, dùng cho bản vẽ 2D không model thép — giá trị thấp hơn hẳn phần còn lại. Sẽ có plan riêng sau khi feature chính chạy.

## Requirements
- Functional: giống gốc — tên view `DetailViewName + "X"/"Y"`, `@ColumnDetail`/`@ColumnSection` ViewFamilyType, crop off, view template, dimension type, tag/text type chọn từ `AnnotationSettings`.
- Non-functional: mỗi output 1 `Transaction` có tên, **chỉ orchestrator mở `TransactionGroup`**; hack `SURFACE→LINEAR` cô lập trong 1 method có comment giải thích + try/catch → skip dimension thay vì fail cả lệnh.

## Architecture
```
Column Rebar/
├── DetailViewCreator.cs         port DetailColumnView (2 section box X/Y + CreateSchedule)
├── SectionViewCreator.cs        port SectionColumnView (MidView per column)
├── DimensionCreator.cs          port DimensionView (266 dòng) — 8 hàm rect/cyl detail/section
├── RebarTableTagCreator.cs      port TagColumn (210 dòng) — TextNote.Create + DetailCurve bảng
├── DetailShopCreator.cs         port CreateDetailShop (162) + ItemDivision.GetAllLocation DS00..DS02 (454) + DrawDetailShop (286)
├── ColumnRebarOrchestrator.cs   ★ sở hữu TransactionGroup; OK action đầy đủ
└── Models/
    ├── AnnotationSettings.cs    port SettingModel ctor (dòng 89–161) — POCO, không ObservableObject
    └── CreatedViews.cs          DetailViewX/Y, SectionViews[], Schedule
```

`ColumnRebarOrchestrator` là ranh giới transaction duy nhất:
```csharp
public OrchestratorResult Run(Document doc, ColumnStack stack, IReadOnlyList<ColumnRebarSpec> specs,
                              AnnotationSettings annotation, IProgress<int> progress)
{
    var shapeCheck = shapeResolver.Validate(doc, stack.Shape);      // fail sớm, trước mọi transaction
    if (!shapeCheck.IsOk) return OrchestratorResult.Invalid(shapeCheck);

    using var group = new TransactionGroup(doc, "Column Rebar");
    group.Start();
    try
    {
        var views = detailViewCreator.Create(...);                  // Tx "Create Detail View"
        sectionViewCreator.Create(...);                             // Tx "Create Section View"
        dimensionCreator.CreateDetail(...);                         // Tx "Create Dimension View"
        dimensionCreator.CreateSection(...);                        // Tx "Create Dimension Section"
        var rebar = rebarCreationService.Create(...);               // Tx ×2 (Phase 4)
        tableTagCreator.Create(...);                                // Tx "Create Tag Bars"
        if (detailShopCreator.IsAvailable(doc))
            detailShopCreator.Create(...);                          // Tx "Create Detail Shop"
        group.Assimilate();
        return OrchestratorResult.Ok(views, rebar);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Column Rebar failed");
        group.RollBack();
        throw;
    }
}
```
`ColumnRebarCommand` **không** mở TransactionGroup; ViewModel gọi `orchestrator.Run` trong `OkCommand` (Phase 6). Cancel dialog = không gọi Run = không có gì để rollback.

Thứ tự giữ như gốc (views trước rebar — gốc làm vậy để section view sẵn sàng khi tag).

## Related Code Files
- Create: 7 file trên
- Modify: `ColumnRebarCommand.cs` — bỏ `TransactionGroup` tạm của Phase 4, gọi `ColumnRebarOrchestrator.Run`
- Modify: `HPRebar.Tests/` — `DetailViewCreatorTests.cs`, `DimensionCreatorTests.cs` (config `Debug.R26`)

## Implementation Steps
1. `AnnotationSettings` — port `SettingModel` ctor (`Model/SettingModel.cs` dòng 89–161, span verified; file 178 dòng): collect `ViewFamilyType`, view templates, `DimensionType`, rebar tag `ElementType`, `MultiReferenceAnnotationType`, `TextNoteType`, `RebarHookType`. **Bỏ** `RebarShapes`/`SelectedShapeStirrup` (Phase 4 hardcode `M_T1`/`M_T3`). Defaults `tmin=dmin=b/5`, `DimH=DimV=b`, `TagH=TagV=b/2`, `L1=L2=b`, `ColumnsName="Columns"`, `PrefixSection="MC"`. POCO — Phase 6 wrap lại cho binding.
2. `DetailViewCreator`: port `GetNameDetail` (duplicate VFT nếu thiếu — `ElementType.Duplicate` verified R23+R26), `GetSectionBox` (Transform từ `East.n`/`Nouth.n`, `mid = datum + h/2`), `ViewSection.CreateSection`, tên có fallback `+"A"` nếu trùng (gốc MessageBox → đổi thành Serilog warning + tên fallback), `VIEWER_CROP_REGION_VISIBLE=0`, `ViewTemplateId`. `CreateSchedule` port nguyên.
3. `SectionViewCreator`: 1 `MidView` mỗi cột tại `location2` (`L·0.5` / `L1+L2·0.5`), `Transform.CreateRotation(BasisX, π)`. Bỏ Start/End view (gốc đã comment out).
4. `DimensionCreator` (đọc `DimensionView.cs` 266 dòng khi cook):
   - `ToLinearReference(doc, PlanarFace)` — cô lập hack, comment bắt buộc:
     ```csharp
     // Revit chỉ nhận reference LINEAR khi dimension trong section view, nhưng
     // PlanarFace.Reference là SURFACE. Đổi token trong stable representation là
     // cách duy nhất không cần ReferenceIntersector. Undocumented — có thể vỡ ở
     // version mới, nên mọi caller phải try/catch và bỏ qua dimension thay vì fail.
     ```
   - port 8 hàm `CreateDimensionHorizontal/VerticalDetail/Section Rectangle/Cylindrical` + `…DetailStirrup…`.
   - `document.Create.NewDimension(view, line, refArray, type)` (verified R23+R26); wrap try/catch → Serilog warning, tiếp tục.
5. `RebarTableTagCreator`: port `TagColumn.CreateTable/CreateTableStirrup/CreateCellItem` (210 dòng) — bảng bằng `TextNote.Create` + `DetailCurve`, `HeightCell`, `H1/H2`. Lưu ý: gốc **không** dùng `IndependentTag`/`MultiReferenceAnnotation` (code đó comment out trong `RebarBarModel`) — không port phần comment.
6. `DetailShopCreator`: `IsAvailable` = có family `DS*` (port `ConditionCreateDetailShop`) → `ItemDivision.GetAllLocation` theo `DetailShopStyle DS00/DS01/DS02` → `doc.Create.NewFamilyInstance` + set param. Số liệu lấy từ Core `BarScheduleCalculator` (Phase 2), **không** port lại logic gom.
7. `ColumnRebarOrchestrator` theo skeleton ở Architecture. Progress tổng = views + dims + rebar + tag + detail shop.
8. Command: bỏ TransactionGroup tạm, gọi orchestrator (Phase 6 sẽ chuyển call site vào ViewModel).
9. TUnit (`Debug.R26`): fixture → `Run` → assert 2 detail view + N section view tồn tại, ≥ 1 `Dimension` trong DetailViewX, group đã Assimilate (undo 1 bước).
10. Build gate `Debug.R26` + `Debug.R23`. F5 end-to-end với spec mặc định.

## Success Criteria
- [ ] **BLOCKED (F5 + fixture)** F5 Revit 2026: sau OK có DetailX, DetailY, N section, dimension ngang trên detail, bảng tag trong section view
- [ ] **BLOCKED (runtime)** Thiếu view template / dimension type → lệnh vẫn chạy, log warning — code path co (moi lookup nullable, moi creator co guard + try/catch)
- [x] Hack `SURFACE→LINEAR` chỉ xuất hiện trong 1 method (`DimensionCreator.ToLinearReference`), có comment, mọi caller try/catch
- [x] `grep -rn "new TransactionGroup" "Column Rebar"` chỉ khớp **1 dòng** — `ColumnRebarOrchestrator.cs:56`
- [ ] **BLOCKED (runtime)** Undo 1 bước xoá hết — `group.Assimilate()` đúng chỗ
- [ ] **BLOCKED** TUnit: 5 test `ColumnRebarOrchestratorTests` viết xong + build pass, skip hết vì thiếu fixture

## Risk Assessment
- **`ParseFromStableRepresentation` hack vỡ trên R27** → dimension bị skip (không fail). Không verify được runtime R27 trên máy này (chỉ có Revit 2025/2026) → ghi vào Phase 8 report là chưa verified. Fallback đường dài: `ReferenceIntersector`.
- **`ViewSchedule` field tên "Image"/"Element Host"/"Length" phụ thuộc ngôn ngữ Revit** (gốc so sánh tên chuỗi) → dùng `ParameterId` (`BuiltInParameter.ALL_MODEL_IMAGE`, …) thay tên; không map được thì giữ tên + log.
- **Family `DS*` không có trong repo** → `DetailShopCreator.IsAvailable` trả false, bỏ qua (đúng hành vi gốc). Không block phase. Cần `.rfa` từ user để verify thật — ghi Unresolved.
- **Khối lượng đọc source** (DimensionView 266 + TagColumn 210 + ItemDivision 454 + CreateDetailShop 162 + DrawDetailShop 286 ≈ 1.4k dòng) → ưu tiên views + dims + tag trước; detail shop cuối, cắt được nếu trượt.

## Cook notes (2026-09-04)

### Build gate
`Debug.R23` / `R24` / `R25` / `R26` / `R27` = **0 error, 0 warning CS**. `HPRebar.slnx` R26 + R23 sach. Core test 99/99.

### File tao (9, plan de xuat 7)
| File | Dong |
|---|---|
| `Models/AnnotationSettings.cs` | 91 |
| `Models/CreatedViews.cs` | 19 |
| **`Models/OrchestratorResult.cs`** | 18 (**MOI** — plan nhac trong skeleton nhung khong liet ke) |
| `DetailViewCreator.cs` | 175 |
| `SectionViewCreator.cs` | 119 |
| `DimensionCreator.cs` | 158 |
| `RebarTableTagCreator.cs` | 163 |
| `ColumnRebarOrchestrator.cs` | 175 |
| `RevitRebarRunner.cs` (viet lai) | 33 |

### D8 hoan tat
`new TransactionGroup` bay gio **dung 1 dong** trong toan feature: `ColumnRebarOrchestrator.cs:56`. Da bo group tam cua Phase 4 khoi `ColumnRebarCommand`, va `RevitRebarRunner` (Phase 6) gio chi la adapter mong 33 dong goi orchestrator — **khong con mo transaction nao**. `IColumnRebarRunner` cua Phase 6 khong phai sua: dung nhu du doan, chi thay implementation.

### Hack SURFACE->LINEAR
Co lap trong `DimensionCreator.ToLinearReference` (1 method, comment 6 dong giai thich tai sao + rui ro). Ca 2 caller (`CreateOnElevation`, `CreateSpan`) boc try/catch -> log warning + tra 0, khong nem ra ngoai. Dimension hong khong lam mat cot thep vua dung.

Ghi chu: `CreateOnElevation` dung **reference goc** cua `PlanarFace` (khong qua hack), giong ban goc `CreateDimensionHorizontalDetailRectangle`. Chi dimension tren **section view** moi can LINEAR.

### 3 dieu khac plan
1. **`ViewSchedule` bo han.** Plan buoc 2 noi "CreateSchedule port nguyen". Ban goc loc schedule theo **ten field chuoi** ("Image", "Element Host", "Length") — vo hieu tren Revit khac ngon ngu, va plan Risk da ghi ro. Schedule chi phuc vu detail-shop (`OST_DetailComponents`), ma detail shop **da bi cat** (duoi). Tao schedule rong khong loc duoc = rac trong project browser. Bo, ghi vao Unresolved.
2. **`DetailShopCreator` khong lam.** Plan Risk cho phep cat neu truot. Family `DS*` khong co trong repo -> `IsAvailable` se luon false -> code chua bao gio chay duoc lan nao. Viet code khong the test = rui ro thuan. Doi user cung cap `.rfa`.
3. **Bang tag don gian hoa.** Ban goc `CreateTableStirrup` (127 dong) sinh row theo tung nhanh `TypeDis`/`TypeH`/`TypeV` lap lai. Port thanh `Rows()` tra `IReadOnlyList<(Label, Value)>` roi 1 vong `WriteRow` — cung noi dung, 163 dong thay vi 210, va them/bot row khong phai dong vao code ve.

### Con BLOCKED
5 TUnit test `ColumnRebarOrchestratorTests` (2 elevation + N section, ten view, rebar dung song song, rollback sach view, planned count) build pass nhung **skip het** — thieu fixture. F5 end-to-end cung vay.

**Rui ro chua do duoc:** hack `SURFACE->LINEAR` **chua chay lan nao** tren bat ky version nao. Neu no vo o R26 thi dimension section bi skip im lang (co log). Day la thu can F5 xac nhan som nhat.
