---
phase: 5
title: "Views + dimension + tag + detail shop + orchestrator"
status: pending
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
- [ ] F5 Revit 2026: sau OK có DetailX, DetailY, N section, dimension ngang trên detail, bảng tag trong section view
- [ ] Thiếu view template / dimension type → lệnh vẫn chạy, log warning, không crash
- [ ] Hack `SURFACE→LINEAR` chỉ xuất hiện trong 1 method, có comment, mọi caller try/catch
- [ ] `grep -rn "new TransactionGroup" "Column Rebar"` chỉ khớp **1 dòng** trong `ColumnRebarOrchestrator.cs`
- [ ] Undo 1 bước xoá hết
- [ ] TUnit views/dimension pass

## Risk Assessment
- **`ParseFromStableRepresentation` hack vỡ trên R27** → dimension bị skip (không fail). Không verify được runtime R27 trên máy này (chỉ có Revit 2025/2026) → ghi vào Phase 8 report là chưa verified. Fallback đường dài: `ReferenceIntersector`.
- **`ViewSchedule` field tên "Image"/"Element Host"/"Length" phụ thuộc ngôn ngữ Revit** (gốc so sánh tên chuỗi) → dùng `ParameterId` (`BuiltInParameter.ALL_MODEL_IMAGE`, …) thay tên; không map được thì giữ tên + log.
- **Family `DS*` không có trong repo** → `DetailShopCreator.IsAvailable` trả false, bỏ qua (đúng hành vi gốc). Không block phase. Cần `.rfa` từ user để verify thật — ghi Unresolved.
- **Khối lượng đọc source** (DimensionView 266 + TagColumn 210 + ItemDivision 454 + CreateDetailShop 162 + DrawDetailShop 286 ≈ 1.4k dòng) → ưu tiên views + dims + tag trước; detail shop cuối, cắt được nếu trượt.
