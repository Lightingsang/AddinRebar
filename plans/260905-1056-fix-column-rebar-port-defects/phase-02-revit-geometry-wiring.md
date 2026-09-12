---
phase: 2
title: "Nối Core vào MainBarCreator (G6) + đo shape cho ScaleToBox (G4)"
status: implemented
effort: 1d
depends: [1]
---

# Phase 02 — Nối dây phần chạm Revit API

## Context Links

- [plan.md](plan.md) · [phase-01](phase-01-core-geometry-math-and-xunit.md)
- D2 (convert feet ở boundary) · D8 (chỉ orchestrator sở hữu `TransactionGroup`)
- Fixture spec: [HPRebar.Tests/Fixtures/README.md](../../HPRebar/HPRebar.Tests/Fixtures/README.md)
- Nguồn gốc chỉ đọc: `StirrupModel.cs:284-298,378-392` (G4) · `BarModel.cs:341-345` (G6)

## Overview

- **Priority:** High
- **Status:** draft · **BLOCKED verify** (thiếu fixture `.rvt`)
- Thay 4 vector đơn vị trong `StirrupGeometry` bằng cạnh hộp thật, và thay nhánh gộp điểm trong `MainBarCreator` bằng `BarPolylineBuilder.Segments`.
- Mọi thay đổi ở phase này chạm `Autodesk.Revit.DB` → **chỉ TUnit verify được, và TUnit đang skip hết**. Xem §Success Criteria để biết cái gì xác minh được ngay, cái gì BLOCKED.

## Đã thực thi (2026-09-05)

**Probe không cần chạy — tài liệu API đã trả lời.** `RebarShapeDrivenAccessor.ScaleToBox` trong
`RevitAPI.xml` (giống hệt R23 · R26 · R27):

> First the bar is given **the default values of the shape parameters from the shape definition**. Then, if it
> is possible to do so without violating the shape definition, the parameter values are scaled so that the
> width and height of the shape (including bar thickness) match the lengths of xVec and yVec. **If there is no
> way to do this within the shape definition due to overconstraining, a compromise is attempted, such as
> scaling the whole shape until either the width or the height is correct.**

Cross-tie `M_T10*` là thanh đơn, chỉ có span để cho → ép cả hai cạnh là overconstrained, và Revit tự chọn
cạnh nào thắng, hợp đồng không nói cạnh nào. Truyền 1 ft vào cạnh thứ hai là đánh cược vào lựa chọn đó:
nếu chiều cao thắng, cả cây tie bị scale theo 304.8 mm.

**Bản sửa** ([AdditionalTieCreator.cs](../../HPRebar/HPRebar/Column%20Rebar/AdditionalTieCreator.cs)): thêm
`HeightAcross` đo tầm với của thanh dọc theo `placement.Width` bằng `GetCenterlineCurves(false, false, false,
MultiplanarOption.IncludeOnlyPlanarCurves, 0)` — gọi ngay sau `CreateFromRebarShape`, đúng lúc thanh còn mang
tham số mặc định như tài liệu mô tả. Sàn dưới là `barType.BarNominalDiameter` ("including bar thickness";
hộp cao 0 thì không có gì để scale). `ScaleToBox` nhận `(Origin, Height, HeightAcross * across)` → span là
thứ duy nhất còn phải scale, móc giữ nguyên hình học do bar type quy định.

**`StirrupGeometry` không đổi.** Nó vẫn trả hướng đơn vị ở `Width`; việc quy đổi sang độ dài thật thuộc về
lớp tạo thanh, nơi duy nhất cầm được `Rebar` để đo. Docstring `:213-216` viết lại cho khớp.

**G6** ([MainBarCreator.cs](../../HPRebar/HPRebar/Column%20Rebar/MainBarCreator.cs)): `BuildCurves` bỏ cả hai
nhánh gộp, chạy `Simplify` rồi `BarPolylineBuilder.Corners`, mỗi cặp điểm một `Line`. `MovesInPlan` đã xoá
(grep 0 hit). Docstring `Simplify` sửa lại — nó **không** ép giữ điểm cuối như câu cũ nói.

**API đã kiểm tay R23 + R27:** `ScaleToBox`, `GetCenterlineCurves`, `MultiplanarOption`, `Curve.Tessellate`,
`XYZ.Normalize`, `RebarBarType.BarNominalDiameter` — có đủ, chữ ký giống nhau, **không cần `#if REVIT*`**.
Lưu ý enum đúng là `IncludeOnlyPlanarCurves`, không phải `IncludeOnlyPlanarBars`.

**Build:** Debug.R23/R24/R25/R26/R27 + Release.R26 — 0 error, 0 warning CS. `HPRebar.Tests` build theo
project path — 0 error. TUnit vẫn skip vì thiếu fixture.

## Key Insights

### G4 — chưa biết cạnh hộp thứ hai mang nghĩa gì

Xác minh: [StirrupGeometry.cs:117](../../HPRebar/HPRebar/Column%20Rebar/StirrupGeometry.cs#L117) truyền
`east` (= `faces.East!.FaceNormal`, dài đúng 1 ft), `:140` truyền `north`, `:183` `XYZ.BasisY`,
`:204` `XYZ.BasisX`. Tiêu thụ ở [AdditionalTieCreator.cs:231](../../HPRebar/HPRebar/Column%20Rebar/AdditionalTieCreator.cs#L231):
`accessor.ScaleToBox(placement.Origin, placement.Height, placement.Width)` — thứ tự **đảo** so với
[StirrupCreator.cs:54](../../HPRebar/HPRebar/Column%20Rebar/StirrupCreator.cs#L54) `(Origin, Width, Height)`.
Nên vector đơn vị rơi vào tham số `yVec` của `ScaleToBox` → cạnh hộp thứ hai luôn 304.8 mm.

Nhánh đúng để đối chiếu trong cùng file: `:70-71` (rectangle), `:88-90` (circle), `:162` (diagonal tie) — đều tính độ dài thật.

**Nhưng:** shape cross-tie là `M_T10`/`M_T10B`/`M_T10C`
([RebarShapeResolver.cs:45-56](../../HPRebar/HPRebar/Column%20Rebar/RebarShapeResolver.cs#L45-L56)) — thanh
đơn có móc, **không phải** đai kín. Cạnh hộp thứ hai của nó là chiều dài chân móc, không phải kích thước
tiết diện. Docstring hiện tại ở `:93-97` và `:213-216` đang **biện minh** cho vector đơn vị bằng chính lý
do đó. Vậy sửa "cho đúng" cần biết `ScaleToBox` làm gì với cạnh thứ hai trên shape này — chỉ đo được
trong Revit thật. **Bước 1 là probe, và nó gate cả phase.**

### G6 — điều kiện vào nhánh sai, không phải nhánh sai

Xem [phase-01](phase-01-core-geometry-math-and-xunit.md) §Key Insights. Tóm tắt: nhánh
[MainBarCreator.cs:89-95](../../HPRebar/HPRebar/Column%20Rebar/MainBarCreator.cs#L89-L95) chỉ sai khi
`Simplify` đã bỏ điểm neo trên (`LbTop < 1 mm`), làm `points[n-3]`/`points[n-2]` trượt về `chân`/`bendStart`.
Kịch bản "không có cột trên" mà đề bài nêu thì nhánh này cho kết quả **đúng**.

## Requirements

**Chức năng**

1. Không còn vector đơn vị nào đi vào `ScaleToBox` — mọi cạnh hộp là độ dài đo được hoặc tính được.
2. `MainBarCreator.BuildCurves` không bao giờ bỏ một điểm không thẳng hàng.
3. Hành vi hiện đang đúng giữ nguyên: đai kín rectangle/circle, thanh thẳng, thanh vát thường.

**Phi chức năng**

4. Không mở `Transaction` hay `TransactionGroup` mới — D8 giữ nguyên.
5. Không thêm `#if REVIT*` mới trừ khi probe chứng minh API khác nhau giữa các version.

## Architecture

```
StirrupGeometry (Revit XYZ, feet)
   ├── span   ← CrossTieBoxCalculator.SpanAcross*Mm(section, cover)   [Core, mm]
   └── cross  ← CrossTieBoxCalculator.RequireCrossExtentMm(measured)  [Core, chặn ≤ 0]
                measured đo từ RebarShape ở lớp Revit

MainBarCreator.BuildCurves
   Simplify(points)  →  BarPolylineBuilder.Segments(points)  →  Line.CreateBound qua PointMapper
```

`StirrupPlacement` phải mang thêm tiết diện + cover, hoặc nhận sẵn hai độ dài — chọn cách thứ hai để
struct không phình: đổi `Width`/`Height` từ `XYZ` thô sang vector đã nhân đúng độ dài trước khi dựng struct.
Chữ ký `StirrupPlacement` **không đổi** → `StirrupCreator` không phải sửa.

## Related Code Files

**Sửa**

- `HPRebar/HPRebar/Column Rebar/StirrupGeometry.cs` — `:98-118` `CrossTieAcrossDepth`, `:121-141` `CrossTieAcrossWidth`, `:166-184` `CrossTieOnCircleAcrossX`, `:187-205` `CrossTieOnCircleAcrossY`; sửa cả docstring `:93-97`
- `HPRebar/HPRebar/Column Rebar/AdditionalTieCreator.cs` — `:213-237` `PlaceCrossTie`: đo extent của `RebarShape`, truyền xuống; sửa docstring `:213-216`
- `HPRebar/HPRebar/Column Rebar/MainBarCreator.cs` — `:65-103` `BuildCurves`, `:126-127` `MovesInPlan` (xoá nếu hết call-site)
- `HPRebar/HPRebar.Tests/RebarCreationServiceTests.cs` — thêm 2 test TUnit (sẽ skip cho đến khi có fixture)

**Tạo:** không (Core đã tạo ở Phase 01).

**Xoá:** không. `MovesInPlan` chỉ xoá nếu bước 6 khiến nó không còn call-site nào.

## Implementation Steps

1. **PROBE (gate cả phase).** Với Revit 2026 mở fixture (hoặc bất kỳ model nào có `M_T10`): tạo thủ công 1 cross-tie qua `Rebar.CreateFromRebarShape`, log bbox của `shape.GetCurvesForBrowser()` và bbox thanh sau `ScaleToBox` với (a) vector đơn vị hiện tại, (b) vector dài gấp đôi. Ghi kết quả vào `plans/260905-1056-fix-column-rebar-port-defects/reports/g4-scaletobox-probe.md`.
   - `RebarShape.GetCurvesForBrowser` đã xác nhận tồn tại trong `RevitAPI.xml`. **Kiểm chữ ký tay cho R23 và R27** — CLAUDE.md ghi rõ sweep theo tên đã từng bỏ sót `CreateFreeForm` đổi signature.
   - Cân nhắc `RebarShapeDrivenAccessor.ScaleToBoxFor` (cũng có trong API) nếu `ScaleToBox` không cho kiểm soát đủ.
2. **Nhánh probe A — cạnh thứ hai bị bỏ qua / không đổi hình học:** G4 không gây hậu quả hình học. DỪNG phần G4, ghi "không tái hiện ở mức hành vi" vào report, chỉ sửa docstring `:93-97` và `:213-216` cho khớp sự thật. Báo user. Tiếp tục sang bước 6 (G6).
3. **Nhánh probe B — cạnh thứ hai kéo giãn móc:** sửa `StirrupGeometry.cs:117` → thay `east` bằng `measuredCrossMm` (feet) nhân `east`; `:140` `north`; `:183` `XYZ.BasisY`; `:204` `XYZ.BasisX`. Span giữ nguyên nhưng lấy từ `CrossTieBoxCalculator` thay vì tính inline ở `:115`, `:138`, `:181`, `:202`.
4. Bổ sung tham số `double crossExtentMm` vào 4 chữ ký `CrossTie*` trong `StirrupGeometry`; gọi `CrossTieBoxCalculator.RequireCrossExtentMm` ngay đầu mỗi hàm để chặn 0.
5. Trong [AdditionalTieCreator.cs](../../HPRebar/HPRebar/Column%20Rebar/AdditionalTieCreator.cs) đo extent từ `shape.GetCurvesForBrowser()` một lần cho mỗi shape, cache trong biến cục bộ của hàm gọi (`:198` đã lấy `shape` sẵn), truyền vào 4 call-site `StirrupGeometry.CrossTie*` — bao gồm `:203` và `:206`, và các call-site rectangle tương ứng ở đầu file.
6. Trong [MainBarCreator.cs:70-103](../../HPRebar/HPRebar/Column%20Rebar/MainBarCreator.cs#L70-L103): giữ `Simplify` ở dòng 72; xoá cả hai nhánh `:82-87` và `:89-95`; thay vòng lặp `:97-100` bằng vòng lặp trên `BarPolylineBuilder.Segments(points)`, mỗi range → `Line.CreateBound(mapper.ToXyz(points[range.From]), mapper.ToXyz(points[range.To]))`.
7. Xoá `MovesInPlan` ở `:126-127` nếu không còn call-site; `grep -n "MovesInPlan" "HPRebar/HPRebar/Column Rebar"` để chắc.
8. Viết lại docstring `:65-68` cho khớp hành vi mới — bỏ câu biện minh cho việc gộp đoạn đỉnh.
9. Thêm 2 test TUnit vào `HPRebar.Tests/RebarCreationServiceTests.cs` (xem §Success Criteria).
10. Chạy build gate đầy đủ, kể cả `HPRebar.Tests` theo project path.

## Todo List

- [ ] Probe `ScaleToBox` trên Revit 2026, ghi `reports/g4-scaletobox-probe.md`
- [ ] Kiểm chữ ký `GetCurvesForBrowser` trên R23 + R27 (tay, không sweep tên)
- [ ] Quyết nhánh A hay B theo kết quả probe; nhánh A thì báo user trước khi bỏ phần G4
- [ ] `StirrupGeometry` — 4 call-site + docstring
- [ ] `AdditionalTieCreator` — đo extent, truyền xuống, sửa docstring
- [ ] `MainBarCreator.BuildCurves` — dùng `Segments`, xoá 2 nhánh, xoá `MovesInPlan`
- [ ] 2 test TUnit
- [ ] `dotnet build HPRebar.slnx -c Debug.R26`
- [ ] `dotnet build HPRebar.slnx -c Debug.R23`
- [ ] `dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26`
- [ ] `dotnet test HPRebar.Core.Tests` — chống hồi quy

## Success Criteria

**Xác minh được ngay (build + grep, không cần Revit):**

- `dotnet build HPRebar.slnx -c Debug.R26` và `-c Debug.R23` — 0 error, 0 warning CS.
- `dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26` — 0 error. **Phải build theo project path**: `.slnx` đặt `<Build Project="false"/>` cho project này, build qua solution sẽ không đụng tới nó.
- `dotnet test HPRebar.Core.Tests` — vẫn xanh (chống hồi quy Phase 01).
- `grep -n "XYZ.BasisX\|XYZ.BasisY\|FaceNormal)" "HPRebar/HPRebar/Column Rebar/StirrupGeometry.cs"` — không còn dòng nào truyền normal thô làm tham số `width`/`height` của `StirrupPlacement`.
- `grep -n "MovesInPlan" "HPRebar/HPRebar/Column Rebar"` — 0 hit.

**BLOCKED (thiếu `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`; toàn bộ TUnit đang skip):**

- **Test chứng minh G4** — `CrossTieBoxDoesNotUseAFixedOneFootEdge` (TUnit): tạo cross-tie trên `C1-LOWER` (400×600) rồi trên một cột 800×600, đọc bbox thanh; trước khi sửa hai bbox có cùng một cạnh 304.8 mm, sau khi sửa cạnh đó thay đổi theo shape/tiết diện.
- **Test chứng minh G6** — `AMainBarWithABottomHookAndAZeroTopAnchorKeepsItsBendStart` (TUnit): dựng thanh với splice ở §Success Criteria của [phase-01](phase-01-core-geometry-math-and-xunit.md), đọc `rebar.GetCenterlineCurves(...)`; trước khi sửa 2 curve, sau khi sửa 3 curve và đỉnh gãy nằm đúng cao độ `TopPosition - BendDepth`.
- F5 Revit 2026: chạy tool trên stack 2 tầng, mắt thường xác nhận cross-tie không thò ra ngoài bê tông và thanh chính không chạy chéo.
- R23, R24, R27 **không verify runtime được** — máy dev chỉ có Revit 2025 và 2026. Ba version đó là build-only.

## Risk Assessment

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Probe cho ra nhánh A → nửa phase không có việc | Trung bình | Bước 2 xử lý tường minh; không đoán trước, không sửa mù |
| `GetCurvesForBrowser` đổi signature R23/R27 | Trung bình | Bước 1 kiểm tay; nếu lệch thì `#if REVIT*` kèm comment `// Multi-version: <topic>` |
| Sửa `BuildCurves` làm `CurveLoop.Create` từ chối loop | Cao | `Segments` không sinh đoạn dài 0 (điểm trùng đã bị `Simplify` loại trước); test TUnit bắt, nhưng đang BLOCKED → F5 thủ công là lưới an toàn duy nhất |
| Đo extent shape mỗi thanh làm chậm | Thấp | Cache theo shape ở hàm gọi, không đo trong vòng lặp |
| Không có fixture → sửa xong không ai chứng minh được | **Cao** | Nêu thẳng trong report bàn giao; đề nghị user dựng fixture (~30–60 phút, spec đã có sẵn) trước khi merge |

## Security Considerations

Không có bề mặt bảo mật mới: không I/O, không mạng, không credential.
Report probe ghi vào thư mục plan — **không** đính kèm đường dẫn model của khách hàng hay `.rvt` thật.
Log Serilog giữ nguyên mức hiện tại, không thêm dump hình học vào log production.

## Next Steps

- Sau phase này, G4 và G6 khép lại về mặt code; phần verify còn treo theo fixture.
- Phase 3 và 4 độc lập, không chờ phase này.
