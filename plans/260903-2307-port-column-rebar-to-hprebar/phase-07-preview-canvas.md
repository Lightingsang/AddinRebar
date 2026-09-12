---
phase: 7
title: "Preview canvas (elevation + section + dowels + distribution diagrams)"
status: partial
priority: P2
effort: "3d"
dependencies: [6]
---

# Phase 7: Preview canvas

## Overview
Port ~5.4k dòng draw code (`DrawMainCanvas` 762, `DrawImage` 2219, `DrawItem` 1428, `DrawImageRebar` 567, `DrawIcon` 451) thành custom `FrameworkElement` vẽ bằng `DrawingContext.OnRender`, bind qua DependencyProperty tới session. Phase nặng nhất — chia 4 sub-step, mỗi sub-step có build gate + visual check riêng.

## Requirements
- Functional: (a) mặt đứng cả stack: cột, dầm đỉnh cột (hb/zb), móng, thép chủ + dowels, đai (đều / 3 vùng), highlight cột đang chọn; (b) mặt cắt trong tab Bars/Stirrups: tiết diện + thép chủ đánh số + đai; (c) tab Top/Bottom Dowels: mặt cắt cột dưới/trên chồng nhau + thanh tái phân bố; (d) icon phân bố đai `TypeDis 0–3`, hình minh hoạ Add/Dowels.
- Non-functional: không vẽ từ VM; màu/stroke từ Theme brush (`TryFindResource`); scale từ `CanvasScaleCalculator` (Core, Phase 2); mỗi file < 300 dòng; không `Canvas.Children.Add` trong VM.

## Architecture
```
Column Rebar/View/Controls/
├── ColumnElevationCanvas.cs      FrameworkElement; DP: Stack, Specs, SelectedColumnIndex, Strings ; OnRender
├── ColumnSectionCanvas.cs        DP: Section, Spec, HighlightBar ; OnRender
├── DowelsOverlayCanvas.cs        DP: Lower, Upper, Specs ; OnRender (tab Top/Bottom Dowels)
├── DistributionDiagram.cs        DP: TypeDis, IsTiesUp ; OnRender (thay DrawDistribute/DrawDistribute2)
└── Drawing/
    ├── DrawPrimitives.cs         port DrawImage: DimHorizontal/Vertical(+Text), DrawSection, DrawStirrup, DrawHook, DrawOneBarSection, DimText — nhận DrawingContext
    ├── ElevationPainter.cs       port DrawMainCanvas: DrawColumnItem*, BeamTopLevel, FoundationBottom, StirrupItem*, BarMain*
    ├── SectionPainter.cs         port DrawSectionAndStirrups + DrawLayerBar*/DrawLayerMainBar*
    ├── DowelsPainter.cs          port DrawSectionAndStirrupDowelsTop/Bottom, DrawBar*Dowels*
    ├── AddBarPainter.cs          port DrawStart/End/MidAdd*Bar (DrawImage 541–905)
    └── CanvasPalette.cs          resolve Theme brushes → Pen/Brush cache (thay DrawModel Color*/Stroke*)
Resources/Icons/ColumnRebar/NavIcons.xaml   8 PathGeometry — thay DrawIcon (không port code)
```
Session → canvas: VM expose `Session`; XAML `<controls:ColumnElevationCanvas Stack="{Binding Session.Stack}" Specs="{Binding Session.Specs}" SelectedColumnIndex="{Binding Session.SelectedColumnIndex}"/>`. `SpecChangedMessage` → control `InvalidateVisual()` (subscribe trong `OnLoaded`, unsubscribe `OnUnloaded`).

## Related Code Files
- Create: 10 file `Controls/` + `Drawing/`, `NavIcons.xaml`
- Modify: `ColumnRebarView.xaml` (thay placeholder), 5 tab XAML (Bars, Stirrups, AdditionalStirrups, TopDowels, BottomDowels) nhúng section/dowels canvas
- Modify: `HPRebar.Core.Tests/CanvasScaleCalculatorTests.cs` (bổ sung nếu scale cần thêm case)
- Reference: `Library/Draw/*.cs` gốc — **đọc từng file khi cook**, không đọc trước cả 5.4k dòng

## Implementation Steps
**7a — Elevation (1d)**
1. `CanvasPalette`: map `ColorFill/MainBar/Stirrup/Bound/Node/Tag` + `StrokeMain/Stirrup/Bound/Dim` sang key canvas đã thêm ở Phase 0 bước 7: `Brush.Canvas.Fill`, `Brush.Canvas.MainBar`, `Brush.Canvas.MainBar.Selected`, `Brush.Canvas.Stirrup`, `Brush.Canvas.Bound`, `Brush.Canvas.Tag`. Resolve bằng `TryFindResource` + cache `Pen`/`Brush` (Freeze). Thiếu key → fallback hằng số + log warning, không throw.
2. `DrawPrimitives`: port đúng các hàm mà `DrawMainCanvas` gọi (không port cả 2219 dòng — grep call-site trước).
3. `ElevationPainter` + `ColumnElevationCanvas`: port `DrawInfoColumns/DrawStirrup/DrawBarMains/DrawColumnItemLevel/DrawFoundationBottom`. Scroll: control đặt trong `ScrollViewer`, `MeasureOverride` trả `Width/Height` từ `CanvasScaleCalculator`; `ScrollToBottom/LeftEnd` như gốc qua attached behavior.
4. Gate: build `Debug.R26` + F5 Revit 2026 → mặt đứng 2 cột + dầm + đai + thép, chọn cột đổi highlight.

**7b — Section (0.75d)**
5. `SectionPainter` + `ColumnSectionCanvas` (rect + cyl): tiết diện, đai, thép chủ đánh số (`DrawOneBarSection` + `DrawTextOneBarSection`), tag layer. Nhúng vào Bars + Stirrups + AdditionalStirrups tab.
6. Gate: đổi nx/ny → section vẽ lại tức thì.

**7c — Dowels overlay (0.75d)**
7. `DowelsPainter` + `DowelsOverlayCanvas`: cột dưới nét đứt (`DrawSectionDashArray`, `DrawStirrupDashArray`) + cột trên, thanh `IsTopDowels&&Type0` tô màu chọn tại `x01/y01` (từ `SpliceCalculator.ComputeUpperPositions`). `AddBarPainter` cho AddBar top/bottom.
8. Gate: đổi `TopDowels` type → overlay đổi.

**7d — Diagrams + icons (0.5d)**
9. `DistributionDiagram` port `DrawDistribute(type, tiesUp)`; `NavIcons.xaml` vẽ lại 8 icon bằng Path (tham khảo hình `DrawIcon` nhưng không port code).
10. Gate: build `Debug.R26` + `Debug.R23` (net48 WPF `DrawingContext` API giống), full F5 pass.

## Success Criteria
- [ ] **BLOCKED (F5)** 4 control render đúng trong Dark + Light (Phase 6 đã bật Light)
- [x] Không `Canvas.Children` thao tác từ VM; grep `FindChild` = 0 (cả hai CLEAN)
- [x] Mỗi file Controls < 300 dòng (lớn nhất `ElevationPainter.cs` 196) (DrawImage 2219 phải chia ≥ 4 file hoặc bỏ hàm không dùng)
- [ ] **BLOCKED (F5)** Đổi input bất kỳ → canvas cập nhật < 100 ms — có debounce 50 ms (`DispatcherTimer`), chưa đo thật
- [x] `CanvasScaleCalculator` test pass (11 test trong 99); scale khớp gốc (`Width=600`, `Height≥850`)

## Risk Assessment
- **Khối lượng**: 5.4k dòng → cắt bằng cách chỉ port hàm có call-site; ước tính còn ~2.5k. Nếu 7a trượt > 1.5d → tạm chấp nhận elevation không có dim text, ghi nợ.
- **Hit-test / click thanh** (plan cũ có) — gốc R01 **không** có click trên canvas → không làm (YAGNI).
- **Font/text đo bằng `FormattedText`** khác net48 vs net8 (ctor có `pixelsPerDip` từ .NET Core) → `// Multi-version: FormattedText ctor` trong `DrawPrimitives.Text(...)`, 1 chỗ duy nhất.
- **Performance `OnRender` mỗi keystroke** → debounce 50 ms trong control (`DispatcherTimer`) nếu cần.

## Cook notes (2026-09-04)

### Build gate
`Debug.R23` / `R24` / `R25` / `R26` / `R27` = **0 error, 0 warning CS**. Core test 99/99.

### File tao (8, plan de xuat 10 + NavIcons)
| File | Dong | Vai tro |
|---|---|---|
| `CanvasPalette.cs` | 129 | resolve 6 brush canvas tu Theme -> `Pen`/`Brush` frozen, thieu key thi fallback + log |
| `DrawPrimitives.cs` | 117 | Line/Box/Circle/Polyline/DimensionH/V/Caption |
| `ElevationBars.cs` | 54 | goi Core calculator dung y het `RebarCreationService` |
| `ElevationPainter.cs` | 196 | mat dung 2 huong, dam, dai, thep chu, dim |
| `ColumnElevationCanvas.cs` | 129 | `FrameworkElement` + DP + debounce |
| `SectionPainter.cs` | 149 | mat cat + dai + thep danh so |
| `ColumnSectionCanvas.cs` | 144 | DP `Column` + `GhostColumn` (chong 2 tiet dien) |
| `DistributionDiagram.cs` | 97 | so do phan bo dai TypeDis 0-3 |

### KHONG port 5.4k dong draw code — ve lai tu Core
Quyet dinh **D4** noi ro: doi `Canvas.Children` sang `DrawingContext.OnRender`. Do la 2 mo hinh render khac han, nen "port" theo nghia dich tung dong khong ap dung. Thay vao do:

`ElevationBars.For()` goi **dung** `BarLayoutCalculator` + `SpliceCalculator` + `BarPolylineBuilder` ma `RebarCreationService` dung. `ElevationPainter.PaintStirrups` goi **dung** `StirrupDistributionCalculator`. => **Cai user thay tren canvas la cai se duoc dung that**, khong phai 2 duong code song song co the lech nhau (do la diem yeu that su cua ban goc: `DrawStirrupItemRectangle0` tinh lai `n`/`del` bang tay, tach roi khoi `CreateStirrupTypeItem1`).

Da doi chieu: `DrawStirrupItemRectangle0` dung `n = (int)(L/S)`, `del = 0.5*(L - n*S)`, ve `n+1` duong. `CreateStirrupTypeItem1` dung `n' = (int)(L/S)+1`, offset `(L-(n'-1)S)/2`. **Tuong duong** (`n' = n+1`, `offset = del`) — nen dung `StirrupRun` cho ca hai la dung.

He toa do giu nguyen ban goc: `x = left + modelX/scale`, `y = top - modelZ/scale`, 2 mat dung canh nhau.

### Bo duoc 1 `#if` ma plan du kien
Plan Risk ghi "`FormattedText` ctor khac net48 vs net8 -> can `// Multi-version:`". **Khong can.** Overload co `pixelsPerDip` ton tai tu **.NET Framework 4.6.2**, va ctor cu bi `[Obsolete]` o **ca net48 lan net8** (build R23/R24 canh bao CS0618 y het R26). Dung 1 overload cho moi version, DPI lay tu `VisualTreeHelper.GetDpi(this)` moi lan render. Tong `#if REVIT` trong project van la **2** (`CreateFreeForm` Phase 4, `UIThemeManager` Phase 6).

Ban dau da viet `#if NETFRAMEWORK` roi phat hien no van warning tren R23 -> bo han.

### Chua lam
1. **`NavIcons.xaml`** (8 PathGeometry) — nav van chi co text. Can design, khong chan chuc nang.
2. **`AddBarPainter`** — thep gia cuong (AddBar) chua ton tai o Core/Phase 4, nen chua co gi de ve.
3. **Dim text chi tiet tren mat dung** (khoang cach dai tung vung, cao do level) — moi co dim tong chieu cao + be rong. Plan Risk da cho phep "chap nhan elevation khong co dim text, ghi no".
4. **Hit-test click thanh** — ban goc khong co, plan ghi YAGNI. Khong lam.

### Con BLOCKED
Moi thu ve hinh anh: 4 control render dung khong, Dark/Light, toc do redraw. Deu can F5 Revit 2026.
