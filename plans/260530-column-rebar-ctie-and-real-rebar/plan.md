# Column Rebar Viewer — Đai chữ C + Vẽ thép thật

Feature: `RevitAIApp/MyRevitAIApp/Column Rebar Viewer/`
Active build: `Debug.R27`

## Mục tiêu (user request)
1. Đai chữ C (crosstie / đai phụ): click 1 thanh thép dọc → tự tạo đai phụ hình chữ C móc thanh đó sang mặt đối diện, hiển thị trên giao diện.
2. Nâng cấp preview canvas cho realistic (đai kín có móc 135°).
3. Vẽ thép THẬT vào model Revit (tạo Rebar elements).

## Phân giai đoạn

### Phase 1 — Preview + Đai chữ C + Click tương tác  [build/verify không cần Revit]
- Model: `CrossTie.cs` (đoạn đai phụ 2D), thêm `CrossTies` vào `RebarSection2D`.
- Calculator: tính hình học crosstie cho 1 thanh (xác định cạnh → móc sang mặt đối diện), bỏ qua thanh góc.
- ViewModel: `HashSet<int>` thanh có C-tie + `ToggleCrossTieCommand(int)`, clear khi đổi layout/count/cover/đường kính.
- RebarCanvas: hit-test click thanh (phân biệt click vs drag-pan), `BarClickedCommand` DP, vẽ crosstie + móc, vẽ đai kín có móc 135°.
- XAML: bind `BarClickedCommand` → `ToggleCrossTieCommand`, thêm hint.
- **Gate:** `dotnet build -c Debug.R27` pass.

### Phase 2 — Vẽ Rebar thật vào model  [cần Revit chạy + RebarBarType]
- Mở rộng `ColumnGeometryReader` lấy transform + cao độ thật của cột.
- Service mới `RebarCreationService` (touch Revit API): tạo thép dọc + đai kín + đai chữ C trong Transaction.
- Command/VM: nút "Tạo thép vào model" + ExternalEvent nếu cần.
- Yêu cầu user: project có sẵn RebarBarType, discipline Structure. Verify bằng F5.

## Trạng thái
- [x] Phase 1 — DONE. Build Debug.R27 pass, test pass (5 test crosstie).
- [x] Phase 2 — CODE DONE. Build R27 + R25 pass, 44/44 test pass. **Chờ F5 verify trong Revit thật.**

## Ghi chú API (quan trọng)
- Revit **2027 bỏ** `RebarHookOrientation` + overload `CreateFromCurves` cũ.
- R26+ dùng `Rebar.CreateFromCurves(doc, style, type, host, norm, curves, BarTerminationsData, bool, bool)`.
- R23–R25 dùng overload cũ (RebarHookType + RebarHookOrientation).
- Service guard `#if REVIT2026_OR_GREATER` trong `RebarCreationService.CreateBars()`.

## Rủi ro cần F5 verify (Phase 2)
1. Map toạ độ: origin = `column.GetTransform().Origin` (giả định ở chân cột) + height theo BasisZ. Nếu cột có base offset, thép có thể lệch cao độ.
2. `SetLayoutAsNumberWithSpacing` hướng rải (barsOnNormalSide) — đai stack lên theo BasisZ.
3. Móc đai dùng hook type đầu tiên tìm thấy; orientation mặc định.
