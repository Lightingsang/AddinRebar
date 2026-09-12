---
phase: 1
title: "Toán G6 xuống HPRebar.Core, phủ xUnit"
status: implemented
effort: 1d
depends: []
---

# Phase 01 — Toán thuần G4 + G6 xuống Core

## Context Links

- [plan.md](plan.md)
- D1 (Core `netstandard2.0`, không ref RevitAPI) · D2 (Core tính mm) — [plan port](../260903-2307-port-column-rebar-to-hprebar/plan.md)
- Nguồn gốc chỉ đọc: `RebarAddin-master/.../R01_ColumnsRebar/Model/StirrupModel.cs:284-298,378-392` · `Model/BarModel.cs:341-345`
- File Core mẫu: [BarPolylineBuilder.cs](../../HPRebar/HPRebar.Core/ColumnRebar/BarPolylineBuilder.cs)
- File test mẫu: [BarPolylineBuilderTests.cs](../../HPRebar/HPRebar.Core.Tests/ColumnRebar/BarPolylineBuilderTests.cs)

## Overview

- **Priority:** High
- **Status:** draft
- Bóc phần tính được không cần Revit của G4 và G6 xuống `HPRebar.Core`, phủ xUnit. Đây là phase DUY NHẤT xác minh trọn vẹn được hôm nay — không phụ thuộc fixture `.rvt`.
- Phase này KHÔNG sửa file nào trong `HPRebar/HPRebar/Column Rebar/`. Việc nối dây ở [phase-02](phase-02-revit-geometry-wiring.md).

## Đã thực thi (2026-09-05)

Ship: `BarPolylineBuilder.Corners` + `LiesBetween` trong [BarPolylineBuilder.cs](../../HPRebar/HPRebar.Core/ColumnRebar/BarPolylineBuilder.cs),
13 test mới trong [BarPolylineBuilderTests.cs](../../HPRebar/HPRebar.Core.Tests/ColumnRebar/BarPolylineBuilderTests.cs). **112/112 xUnit pass** (trước 99).

**Không ship `CrossTieBoxCalculator`.** Span của cross-tie ở `StirrupGeometry` (`:115`, `:138`, `:181`, `:202`)
vốn đã tính đúng độ dài thật — G4 chỉ hỏng ở cạnh hộp thứ hai. Chuyển `H-2c` xuống Core không sửa được gì,
chỉ thêm một tầng gián tiếp, nên bỏ theo KISS/YAGNI. G4 sửa hoàn toàn ở Phase 02.

**API trả về khác thiết kế:** `Corners` trả `IReadOnlyList<Point3>` (danh sách điểm đã lọc) thay vì
`IReadOnlyList<PointRange>`. Không cần kiểu mới, `MainBarCreator` lặp thẳng trên kết quả. `PointRange` không ship.

`LiesBetween` bỏ điểm giữa khi nó nằm trên đoạn nối hai điểm kề **và** nằm giữa hai điểm đó — điểm gập
ngược (`along < 0` hoặc `> 1`) luôn được giữ dù thẳng hàng. Ngưỡng `CollinearToleranceMm = 1e-6`.

## Key Insights

### G4 — phần tính được và phần không

Cạnh hộp "span" của cross-tie suy ra hoàn toàn từ tiết diện + cover, tính bằng mm → xuống Core được.
Cạnh hộp thứ hai (hiện là vector đơn vị = 304.8 mm) **không** suy ra từ tiết diện: `M_T10/M_T10B/M_T10C`
là thanh đơn có móc, bề rộng hình học của nó là chiều dài chân móc, thuộc về `RebarShape`.
→ Core chỉ nhận extent đó làm tham số và **chặn giá trị ≤ 0**; đo extent thật là việc của Phase 02.

### G6 — kịch bản trong đề bài KHÔNG tái hiện

Đã mô phỏng đúng `AppendBottom`/`AppendTop`/`Simplify`/`BuildCurves` trên toàn không gian tham số
(`isBot × botType × LaBottom × LbTop × dx × dy × BendDepth`, 288 tổ hợp):

- Kịch bản đề bài nêu — **không có cột trên, điểm vát trùng XY** — cho **0 tổ hợp sai hình học**.
  Lý do: khi không có cột trên, [SpliceCalculator.cs:38-41](../../HPRebar/HPRebar.Core/ColumnRebar/SpliceCalculator.cs#L38-L41)
  trả `upperPosition = (bar.X0, bar.Y0)`, nên `points[1..n-1]` cùng XY. Nhánh `:89` xuất
  `p0→p1` (chính là đoạn bẻ chân dưới — **được giữ**, không mất) và `p1→plast`; hợp lại trùng khớp
  hình học với polyline đầy đủ.
- Sai hình học thật xảy ra ở **12/288** tổ hợp, tất cả cùng một chữ ký:
  `IsBottomDowels=true` ∧ `BottomDowelsType≠0` ∧ `LaBottom` đủ lớn để móc sống sót `Simplify`
  ∧ `IsTopDowels ∧ TopDowelsType=0` ∧ **`LbTop < 1.0 mm`** ∧ cột trên có vị trí plan khác.
  Khi `LbTop < 1 mm`, `Simplify` ([MainBarCreator.cs:106-124](../../HPRebar/HPRebar/Column%20Rebar/MainBarCreator.cs#L106-L124))
  bỏ điểm neo cuối, `points` tụt còn `[móc, chân, bendStart, crossover]`, nên `points[n-3]`/`points[n-2]`
  hoá thành `chân`/`bendStart` — hai điểm luôn cùng XY → nhánh `:89` kích hoạt **bất kể** cột trên ở đâu,
  và **mất `bendStart`**: thanh chạy chéo thẳng từ chân lên điểm vát thay vì lên thẳng rồi mới bẻ.
- **Lỗi gốc vẫn là lỗi:** điều kiện vào nhánh (`p[n-3]`, `p[n-2]` cùng XY) không hàm ý các điểm bị bỏ
  nằm trên đoạn thẳng thay thế. Sửa đúng = chỉ gộp các điểm **thực sự thẳng hàng**.
- `Simplify` docstring nói "keeping the last point whatever happens" nhưng code không ép giữ điểm cuối.
  Ghi nhận, **không sửa ở phase này** (ngoài phạm vi 4 mục).

## Requirements

**Chức năng**

1. Core cung cấp span mm của cross-tie cho 3 trục: qua chiều sâu (rectangle), qua chiều rộng (rectangle), qua đường kính (circle).
2. Core từ chối extent phụ ≤ 0 bằng exception rõ nghĩa — không còn đường nào để hằng số 304.8 mm lọt vào.
3. Core cung cấp phép chia đoạn polyline: gộp một dãy điểm thành một đoạn **chỉ khi** chúng thẳng hàng.
4. Hành vi hiện đang đúng phải giữ nguyên: thanh thẳng đứng vẫn ra 1 đoạn; thanh vát bình thường vẫn ra đủ đoạn.

**Phi chức năng**

5. `HPRebar.Core` giữ `netstandard2.0`, không thêm reference nào (D1).
6. Đơn vị mm, `double` (D2).

## Architecture

```
HPRebar.Core/ColumnRebar/
├── CrossTieBoxCalculator.cs   (mới)  SpanMm(...) + RequireCrossExtentMm(...)
└── BarPolylineBuilder.cs      (sửa)  + Segments(points) → IReadOnlyList<PointRange>
```

`Segments` trả cặp chỉ số, không trả `Curve` — `Curve` là kiểu Revit, không được xuất hiện trong Core (D1).
Phase 02 map cặp chỉ số sang `Line.CreateBound` qua `PointMapper`.

Thuật toán `Segments`: quét từ điểm 0, kéo dài đoạn hiện tại chừng nào điểm kế tiếp còn thẳng hàng với
điểm đầu đoạn và điểm liền trước (tích có hướng ≈ 0 trong `Tolerance`), gặp điểm gãy thì chốt đoạn.

## Related Code Files

**Tạo**

- `HPRebar/HPRebar.Core/ColumnRebar/CrossTieBoxCalculator.cs`
- `HPRebar/HPRebar.Core.Tests/ColumnRebar/CrossTieBoxCalculatorTests.cs`

**Sửa**

- `HPRebar/HPRebar.Core/ColumnRebar/BarPolylineBuilder.cs` — thêm `Segments`; giữ nguyên `Build`/`IsStraight`/`Length`/`Distance`
- `HPRebar/HPRebar.Core.Tests/ColumnRebar/BarPolylineBuilderTests.cs` — thêm test cho `Segments`

**Xoá:** không.

## Implementation Steps

1. Tạo `HPRebar/HPRebar.Core/ColumnRebar/CrossTieBoxCalculator.cs`, namespace `HPRebar.Core.ColumnRebar`, file-scoped namespace, `public static class`.
2. Thêm `SpanAcrossDepthMm(ColumnSection section, double coverMm)` → `section.H - 2 * coverMm`. Đối chiếu công thức đang inline ở [StirrupGeometry.cs:115](../../HPRebar/HPRebar/Column%20Rebar/StirrupGeometry.cs#L115).
3. Thêm `SpanAcrossWidthMm(...)` → `section.B - 2 * coverMm`, đối chiếu [StirrupGeometry.cs:138](../../HPRebar/HPRebar/Column%20Rebar/StirrupGeometry.cs#L138).
4. Thêm `SpanAcrossDiameterMm(...)` → `section.D - 2 * coverMm`, đối chiếu [StirrupGeometry.cs:181](../../HPRebar/HPRebar/Column%20Rebar/StirrupGeometry.cs#L181) và [:202](../../HPRebar/HPRebar/Column%20Rebar/StirrupGeometry.cs#L202).
5. Cả 3 hàm ném `ArgumentOutOfRangeException` khi kết quả ≤ 0 (cover nuốt hết tiết diện).
6. Thêm `RequireCrossExtentMm(double measuredMm)` — trả nguyên giá trị, ném `ArgumentOutOfRangeException` khi ≤ 0. Đây là cổng chặn hằng số 304.8 mm ở Phase 02.
7. Trong [BarPolylineBuilder.cs](../../HPRebar/HPRebar.Core/ColumnRebar/BarPolylineBuilder.cs) thêm `public static IReadOnlyList<PointRange> Segments(IReadOnlyList<Point3> points)`; `PointRange(int From, int To)` đặt trong `Models/PointRange.cs`. Đặt `Segments` ngay sau `IsStraight` (dòng 153) để giữ trật tự đọc.
8. `Segments` ném `ArgumentException` khi `points.Count < 2`; với input thẳng hoàn toàn trả đúng 1 range `(0, Count-1)` — thay thế được nhánh `IsStraight` ở [MainBarCreator.cs:82-87](../../HPRebar/HPRebar/Column%20Rebar/MainBarCreator.cs#L82-L87).
9. Viết test G4 trong `CrossTieBoxCalculatorTests.cs`, dùng `TestSections.Rectangle()` / `TestSections.Circle()` sẵn có: rectangle 400×600 cover 25 → across-depth 550, across-width 350; circle D 500 cover 25 → 450.
10. Test hồi quy G4: `Assert.NotEqual(304.8, span, 6)` cho cả 3 trục, kèm comment nói rõ 304.8 mm là 1 ft — cạnh hộp không bao giờ được là hằng số.
11. Test G4 biên: cover 200 trên tiết diện 400 → `ArgumentOutOfRangeException`; `RequireCrossExtentMm(0)` và `(-1)` → `ArgumentOutOfRangeException`.
12. Viết test G6 chứng minh lỗi (chi tiết ở §Success Criteria) + 3 test giữ hành vi, thêm vào `BarPolylineBuilderTests.cs`.
13. Chạy build gate.

## Todo List

- [ ] `CrossTieBoxCalculator.cs` — 3 hàm span + `RequireCrossExtentMm`
- [ ] `Models/PointRange.cs` + `BarPolylineBuilder.Segments`
- [ ] `CrossTieBoxCalculatorTests.cs` — 3 span + 3 hồi quy 304.8 + 3 biên
- [ ] `BarPolylineBuilderTests.cs` — 1 test chứng minh G6 + 3 test giữ hành vi
- [ ] `dotnet build HPRebar.slnx -c Debug.R26`
- [ ] `dotnet build HPRebar.slnx -c Debug.R23`
- [ ] `dotnet test HPRebar.Core.Tests` — 99 test cũ pass + test mới pass

## Success Criteria

**Xác minh được ngay (xUnit, không cần Revit):**

- `dotnet test HPRebar.Core.Tests` xanh toàn bộ; 99 test hiện có không đổi kết quả.
- **Test chứng minh G6** — `SegmentsKeepsTheBendStartWhenTheTopAnchorIsTooShortToSurvive`:
  ```
  section : TestSections.Rectangle()
  splice  : IsBottomDowels=true, BottomDowelsType=1, LbBottom=100, LaBottom=40,
            IsTopDowels=true,  TopDowelsType=0,   LbTop=0
  upper   : new PlanPoint(bar.X0 + 150, bar.Y0)
  ```
  Dựng polyline qua `BarPolylineBuilder.Build`, lọc qua cùng luật `Simplify` (ngưỡng 1 mm), gọi `Segments`.
  - **Trước khi sửa** (mô phỏng nhánh `MainBarCreator.cs:89-95`): 2 đoạn, điểm `bendStart` không nằm trên đoạn nào → assert thất bại.
  - **Sau khi sửa:** 3 đoạn, mọi điểm giữ lại đều nằm trên một đoạn.
- **Test giữ hành vi:** (a) thanh thẳng đứng → 1 range; (b) thanh vát có cột trên lệch, `LbTop=700` → không gộp mất `bendStart`; (c) thanh có móc dưới, không có cột trên → đúng 2 range (móc + đứng).
- **Test chứng minh G4** — `SpanNeverEqualsOneFoot`: cả 3 span ≠ 304.8 trên 3 tiết diện khác nhau. Đây chính là giá trị mà `StirrupGeometry` đang truyền (vector đơn vị) cho mọi tiết diện.
- `dotnet build HPRebar.slnx -c Debug.R26` và `-c Debug.R23` — 0 error.

**BLOCKED:** không có. Phase 1 độc lập hoàn toàn với fixture `.rvt`.

## Risk Assessment

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| `Segments` đổi hành vi thanh đang đúng → hồi quy im lặng | Cao | 3 test giữ hành vi ở trên; Phase 02 chưa nối dây nên chưa ảnh hưởng runtime |
| Ngưỡng thẳng hàng quá chặt/lỏng | Trung bình | Dùng `Tolerance.Default` (1e-9) đã có; **không** đổi hằng số đó — nó quyết định việc gộp dòng schedule |
| `record struct` không biên dịch trên `netstandard2.0` | Thấp | Core đã có Polyfill (`IsExternalInit`); vướng thì dùng `readonly struct` thường |
| Lệch giữa `Simplify` (lớp Revit) và `Segments` (Core) | Trung bình | Phase 02 bước 4 chốt: chỉ một luật, `Segments` chạy sau `Simplify`; không để hai luật song song |

## Security Considerations

Không có bề mặt bảo mật: không I/O, không mạng, không đọc file, không dữ liệu người dùng.
`ArgumentOutOfRangeException` chỉ mang tên tham số và giá trị số — không rò đường dẫn hay tên model.

## Next Steps

- [phase-02](phase-02-revit-geometry-wiring.md) tiêu thụ cả `CrossTieBoxCalculator` lẫn `Segments`.
- Phase 3 và 4 không phụ thuộc phase này, chạy song song được.
