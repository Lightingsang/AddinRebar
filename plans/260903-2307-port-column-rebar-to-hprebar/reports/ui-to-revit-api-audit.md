# Audit: UI → ViewModel → Core → Revit API

2026-09-04. Kiểm tra toàn tuyến sau khi code xong 9 phase nhưng **chưa chạy lần nào**. Nguồn đối chiếu Revit API: [rvtdocs.com](https://rvtdocs.com/) + XML doc trong package NuGet `Nice3point.Revit.Api.RevitAPI` (2023.1.90 / 2026.4.10 / 2027.2.0).

## Kết quả

**7 lỗi thật, đã sửa hết.** 4 trong số đó gây crash hoặc sai output im lặng.

Sau khi sửa: build 5/5 config `Debug.R23..R27` = 0 error / 0 warning CS · xUnit **102/102** (thêm 3 test mới).

---

## Đính chính tuyên bố sai của tôi ở Phase 6

Tôi đã viết: *"Mọi style/brush key khớp tên trong Theme — build XAML 5/5 config pass, key sai sẽ fail lúc compile BAML"*.

**Sai.** `StaticResource` fail lúc load, nhưng `DynamicResource` **không fail lúc compile hay load** — nó chỉ đơn giản không áp style, control render trần. Vì Phase 6 dùng `DynamicResource` cho tất cả (theo chỉ dẫn skill), build pass **không chứng minh** gì về key.

Đã kiểm lại đúng cách bằng cách so tập hợp:

| Nguồn | Kết quả |
|---|---|
| 27 key `DynamicResource` trong XAML | ✅ đủ trong 99 key định nghĩa |
| 6 key `Brush.Canvas.*` gọi qua `TryFindResource` trong C# | ✅ có ở **cả** ThemeDark lẫn ThemeLight |
| key mà chính file theme tham chiếu chéo | ✅ không thiếu |

Kết luận vẫn đúng, nhưng bằng chứng trước đó không hợp lệ.

---

## Lỗi tìm được

### 1 — `DataGridColumn.Header` + `RelativeSource` không bao giờ resolve · High

`GeometryTabView.xaml:44`

```xml
<DataGridTextColumn Header="{Binding Localization.Strings.ColumnsNumber,
                    RelativeSource={RelativeSource AncestorType=UserControl}}" .../>
```

`DataGridColumn` **không nằm trong visual tree**, nên `FindAncestor` không có gì để đi lên. Binding fail **im lặng** → header rỗng. Không có warning lúc build, không có exception lúc chạy.

Sửa: dùng header literal như 8 cột còn lại trong chính DataGrid đó.

### 2 — Bảng vị trí thanh không refresh khi đổi số thanh · High

`BarsTabViewModel` chỉ subscribe `Session.PropertyChanged` cho `SelectedColumn`. Đổi `nx`/`ny` thay đổi `ColumnSpecEditor` chứ không phải `Session` → `Positions` **giữ nguyên giá trị cũ**, trong khi canvas mặt cắt (có subscribe vào column) lại cập nhật. Hai thứ cạnh nhau hiển thị lệch nhau.

Doc comment của chính property đó ghi *"recomputed whenever the counts change"* — không đúng.

Sửa: theo dõi column đang chọn, re-subscribe khi đổi column.

### 3 — Đổi loại thép không tính lại chiều dài nối chồng · Medium

`LbTop` = 35d hoặc 70d, là **hàm của đường kính thanh**. Không có `OnMainBarTypeChanged`, nên đổi từ D16 sang D25 giữ nguyên lap tính theo D16 → nối chồng ngắn hơn thiết kế mà không có dấu hiệu gì.

Tool gốc dựng lại toàn bộ `BarModels` khi đổi thanh nên không dính.

Sửa: `OnMainBarTypeChanged` / `OnSplitOverlapChanged` / `OnOverlapFactorChanged` → tính lại lap cho mọi splice. Ghi chú rõ: chỉnh sửa từng thanh trước đó bị mất — giống hành vi gốc.

### 4 — `nx=1` / `nd=5` ném exception trên UI thread · Critical

`BarLayoutCalculator.Compute` throw `ArgumentOutOfRangeException` khi `Nx < 2`, `Ny < 2`, hoặc `Nd % 4 != 0`. Gọi ở 5 chỗ:

| Chỗ gọi | Có guard? |
|---|---|
| `ElevationBars.For` | ✅ try/catch |
| `SectionPainter.PaintBars` | ✅ try/catch |
| `RebarCreationService` | ✅ sau validate |
| `BarsTabViewModel.Refresh` | ❌ |
| `BarsDivisionTabViewModel.Refresh` | ❌ |

Gõ `1` vào ô "Bars along width" rồi mở tab Bars Division → exception không ai bắt trên UI thread.

### 5 — `IsValid` để lọt cấu hình mà calculator sẽ từ chối · Critical

`Session.IsValid` chỉ kiểm `BarCount < 1`. Với `Nx=1, Ny=2` thì `BarCount = 2` → **qua validate, nút OK sáng** → `RebarCreationService.Create` throw giữa chừng → rollback + dialog lỗi kỹ thuật, thay vì một câu giải thích trước khi bắt đầu.

Gốc chung của #4 và #5: **luật validate nằm trong calculator dưới dạng throw, còn UI không bao giờ hỏi trước khi gọi.**

### 6 — Quá 1002 đai thì Revit từ chối · Critical

Doc chính thức của `RebarShapeDrivenAccessor.SetLayoutAsNumberWithSpacing`:

> *"the number of bar positions `numberOfBarPositions` is less than 1 or more than 1002"* → throw
> *"The spacing isn't bigger than 0.0"* → throw

Tôi chỉ chặn `spacing > 0`. Gõ khoảng cách đai 2 mm trên cột cao 3 m → 1501 đai → Revit throw **giữa transaction**.

### 7 — Cover quá lớn làm `ScaleToBox` nhận vector độ dài 0 · Critical

Doc `ScaleToBox`: *"Vector representing the first edge of the rectangle. **The length must be positive.**"*

`StirrupGeometry.Rectangle` tính `width = (b - 2·cover - inset) · east`. Nếu `2·cover ≥ b` thì vector bằng 0 hoặc âm → throw. Cùng lúc `BarLayoutCalculator` cho `deltaX` âm → thanh nằm **ngoài** cột, không exception, không dấu hiệu.

---

## Cách sửa

Đặt luật vào **một chỗ UI hỏi được trước khi gọi**, thay vì để calculator throw.

**`ColumnSpecEditor`** — hai cửa:
- `IsLayoutValid` (rẻ, bool) — thứ gì vẽ/thống kê thì hỏi cái này rồi bỏ qua khi đang gõ dở.
- `Validate(out reason)` — đầy đủ, trả **lỗi đầu tiên** kèm câu giải thích cho người dùng: loại thép chưa chọn · số thanh không hợp lệ · cover + đường kính không lọt tiết diện · run đai ≤ 0 · khoảng cách ≤ 0 · vượt 1002 đai · cross-tie thiếu thông số.

**`Session.IsValid`** giờ chỉ ủy quyền cho từng column, prefix tên cột vào message.

**`StirrupDistributionCalculator`** (Core) — thêm `MaxBarPositions = 1002` và throw khi vượt. Core là nguồn sự thật và **test được**; check ở UI chỉ là pre-flight thân thiện. 3 test xUnit mới: vượt ngưỡng bị từ chối · sát ngưỡng vẫn chạy · mỗi vùng của layout dày/thưa/dày đều bị áp giới hạn.

---

## Revit API — đã đối chiếu, port đúng

| API | Nguồn | Kết luận |
|---|---|---|
| `ScaleToBox(origin, xVec, yVec)` | rvtdocs 2025 + XML | Hình chữ nhật `origin, +xVec, +xVec+yVec, +yVec`. Port truyền **vector hiệu** (`(b-2c-inset)·east`), không phải điểm — ✅ đúng |
| `SetLayoutAsNumberWithSpacing(int, double, bool, bool, bool)` | XML | `numberOfBarPositions, spacing, barsOnNormalSide, includeFirstBar, includeLastBar`. Port dùng `(count, spacing, true, true, true)` khớp gốc. Nhánh `false,false` của gốc là **dead code** — mọi call site đều truyền `start: true` |
| `CreateFromRebarShape(doc, shape, barType, host, origin, xVec, yVec)` | XML | `origin` = *"lower-left corner of the shape's bounding box"*. Port dựng từ góc Tây-Nam + cover, khớp gốc — ✅ |
| `Rebar.CreateFreeForm` | XML 3 version | Overload `out RebarFreeFormValidationResult` **bị xóa ở R27**; overload `RebarStyle` chỉ có **từ R26**, trả `RebarFreeFormCreationResult`. Đã guard `#if REVIT2026_OR_GREATER` |
| `RebarStyle.Standard` / `.StirrupTie` | XML 2 version | Có ở mọi version, không cần guard |

## Còn nguyên rủi ro — không audit tĩnh nào chạm tới được

`DimensionCreator.ToLinearReference` đổi token `SURFACE` → `LINEAR` trong stable representation. **Undocumented**, rvtdocs không có gì về nó, chưa chạy lần nào. Hỏng thì dimension mặt cắt bị bỏ qua im lặng (có log warning). Vẫn là thứ F5 cần xác nhận sớm nhất.

Ba cờ `bool` của `SetLayoutAsNumberWithSpacing` được doc mô tả *ý nghĩa* nhưng không nói tổ hợp nào cho ra hướng rải nào — chỉ nhìn model thật mới biết đai rải đúng chiều Z hay ngược.
