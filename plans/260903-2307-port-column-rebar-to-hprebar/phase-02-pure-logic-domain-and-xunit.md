---
phase: 2
title: "Core pure logic (layout / splice / stirrup / schedule) + xUnit"
status: completed
priority: P1
effort: "2d"
dependencies: [0]
---

# Phase 2: Core pure logic + xUnit

## Overview
Port toàn bộ **toán** của tool sang `HPRebar.Core.ColumnRebar` — không một dòng Revit API. Đây là phần giá trị nhất và test được 100%. Input/Output đều `double` mm + record. Chạy song song với Phase 6.

Công thức chuẩn: `reports/source-analysis-r01-columnsrebar.md` §5, §6, §7, §8 (ItemDivision).

## Requirements
- Functional: tái tạo đúng số học của `BarMainModel`, `BarModel`, `StirrupModel.GetDistribute/CreateStirrupTypeItem1/2`, `ProcessBarsDivision`, `DrawModel.GetScale*`.
- Non-functional: mỗi file < 300 dòng; class `static` + record immutable; không `ObservableObject` trong Core (UI state ở Phase 6); tên method mô tả (không `GetNextX0`).

## Architecture
```
HPRebar.Core/ColumnRebar/
├── Models/
│   ├── SectionShape.cs              enum Rectangle | Circular
│   ├── ColumnSection.cs             record: Index, Shape, B, H, D, Hc, Hb, Zb, Et, Eb,
│   │                                TopPosition, BottomPosition, West/East/South/NorthPosition, CenterX, CenterY
│   ├── BarLayoutSpec.cs             record: Nx, Ny, Nd, BarDiameter, StirrupDiameter, Cover
│   ├── BarPosition.cs               record: BarNumber, X0, Y0, Side
│   ├── BarSide.cs                   enum South | East | North | West | Corner (cyl: Quadrant0..3)
│   ├── SpliceSpec.cs                record: IsTopDowels, TopDowelsType, LaTop, LbTop, IsBottomDowels, BottomDowelsType, LaBottom, LbBottom, LcBottom
│   ├── Point3.cs                    record struct X, Y, Z (mm)
│   ├── BarPolyline.cs               record: BarNumber, Points (IReadOnlyList<Point3>)
│   ├── StirrupSpec.cs               record: TypeDis(0..3), S, S1, S2, IsTiesUp
│   ├── StirrupRun.cs                record: Count, Spacing, StartOffset
│   ├── AdditionalTieSpec.cs         record: AddH, TypeH, NH, AH, AddV, TypeV, NV, AV
│   └── BarScheduleItem.cs           record: Name, Count, Diameter, L, La, Lb, Length
├── BarLayoutCalculator.cs           Compute(section, spec) → IReadOnlyList<BarPosition>
├── BarSideClassifier.cs             SideOf(barNumber, nx, ny) ; QuadrantOf(barNumber, nd)
├── SpliceCalculator.cs              ComputeUpperPositions(sectionUp, spec, bars, splices) → x01/y01 ; BuildPolyline(...)
├── StirrupDistributionCalculator.cs ComputeL(hc,hb,zb,tiesUp) ; Compute(L, spec) → IReadOnlyList<StirrupRun>
├── DefaultOverlap.cs                LapLength(barNumber, d, splitPercent, factor) → 35d / 70d
├── BarScheduleCalculator.cs         Group(polylines, …) → IReadOnlyList<BarScheduleItem>
└── CanvasScaleCalculator.cs         port DrawModel.GetScale/GetScaleSection/GetScaleDowels
```

## Related Code Files
- Create: tất cả file trên trong `HPRebar/HPRebar.Core/ColumnRebar/`
- Create: `HPRebar/HPRebar.Core.Tests/ColumnRebar/BarLayoutCalculatorTests.cs`, `SpliceCalculatorTests.cs`, `StirrupDistributionCalculatorTests.cs`, `BarSideClassifierTests.cs`, `DefaultOverlapTests.cs`, `BarScheduleCalculatorTests.cs`, `CanvasScaleCalculatorTests.cs`
- Delete: `HPRebar/HPRebar.Core.Tests/ColumnRebar/SmokeTests.cs` (Phase 0)

## Implementation Steps
1. Models trước (records, XML doc 1 dòng mỗi field, ghi rõ đơn vị mm).
2. `BarSideClassifier` — port điều kiện `BarNumber <= nx`, `> nx && <= nx+ny-2`, … thành 1 hàm. Test 4 cạnh + góc với nx=3,ny=4 (10 thanh).
3. `BarLayoutCalculator.Compute` — port `GetBarModels` (4 vòng while → 1 vòng theo cạnh). Circular: `Math.Round(Math.Cos(angle), 9)` giữ nguyên để khớp gốc.
   Test golden: `b=400,h=600,cover=25,ds=8,d=20,nx=3,ny=4` → 10 bar, `deltaX=157`, `deltaY=(600-50-16-20)/3=171.33…`, bar 1 = `(West+43, South+43)`.
   Test nx=ny=2 → 4 bar. Circular `nd=8, D=500` → 8 bar bán kính `r=250-25-8-10=207`.
4. `DefaultOverlap.LapLength` — `split==50 ? (n%2==0 ? f·d : 2f·d) : f·d`. Test chẵn/lẻ/split≠50.
5. `StirrupDistributionCalculator`:
   - `ComputeL`: `tiesUp ? l+hb+zb : l` với `l = hc-hb-zb`.
   - `Compute`: TypeDis 0 → 1 run `(floor(L/S)+1, S, (L-(n-1)S)/2)`; TypeDis 1–3 → 3 run theo `δ1, δ2+L1, δ1+L1+L2`.
   Test: `L=3000,S=150` → n=21, offset 0. `L=3000,TypeDis=1,S1=100,S2=200` → L1=750,L2=1500, n1=8, n2=8, kiểm tra offsets.
   Test edge: `S<=0` → throw `ArgumentOutOfRangeException` (source để UI chặn; Core phải tự bảo vệ).
6. `SpliceCalculator`:
   - `BuildPolyline(section, bar, splice, upper?)` port `GetLocationBottom*` + `GetLocationTop*` (report §6). `hb' = zb+hb==0 ? (rect ? max(b,h) : D) : zb+hb`.
   - `ComputeUpperPositions` port `RefreshLocationBarModels` phần `infoModelUp != null`: lọc theo cạnh, chia lại `delta'`, `±d` ở biên. Cyl `nd>4`: `angle1 = 2d/(2r)`, chia `π/2` mỗi phần tư.
   Test: cột trên thu từ 400×600 → 300×500, 3 thanh cạnh Nam đều `IsTopDowels&&Type0` → x01 index0 = `West'+cover+ds+d/2+d`, index2 = `… -d + 2·delta'`. Test thanh `TopDowels=1` không đổi x01.
   Test polyline: `TopDowels=0` cho 3 điểm, Z lần lượt `Top-hb'`, `Top`, `Top+Lb`; thanh chẵn `Lb=35d`, lẻ `70d`.
7. `BarScheduleCalculator.Group` — port `ProcessBarsDivision.GetMain/GetStirrup/GetAddH/GetAddV` + `ConditionSameBarModel` (phải đọc source dòng 365–787 khi cook; report chỉ có signature). Test: 4 thanh giống nhau → 1 item `Count=4`; 2 nhóm khác `Lb` → 2 item.
8. `CanvasScaleCalculator` — port 3 hàm scale (ngưỡng 200/4640/270/190, `Width=600`, `Height=max(850, maxH/scale+160)`). Test 2 ngưỡng.
9. `dotnet test HPRebar.Core.Tests` → 100% pass. Build gate `Debug.R26` + `Debug.R23` (Core được HPRebar ref).

## Success Criteria
- [x] **99** test xUnit pass (yeu cau >= 30), cover mọi nhánh TypeDis 0–3, 4 cạnh + circular, splice stagger, upper redistribution, schedule grouping
- [x] `HPRebar.Core.csproj` vẫn không ref RevitAPI (grep: CLEAN)
- [x] Không file > 300 dòng (lớn nhất `SpliceCalculator.cs` 207)
- [x] Bug B4 (`Bar = Bar`) không tồn tại ở Core — spec là input, không có default ẩn

## Risk Assessment
- **`ProcessBarsDivision` 787 dòng chưa đọc chi tiết** → cook phải đọc; nếu logic gom phụ thuộc Revit (`FamilySymbol`) thì tách phần đó sang Phase 5, Core chỉ giữ grouping thuần.
- **Sai lệch floating so với gốc** → giữ `Math.Round(…, 9)` như gốc; test dùng `Assert.Equal(expected, actual, precision: 6)`.
- **Circular `nd` không chia hết 4** → gốc chỉ cho `{4,8,12,16,20}`; Core validate `nd % 4 == 0` throw.

## Cook notes (2026-09-04)

### Ket qua
99 test xUnit pass (yeu cau >= 30). Build `Debug.R26` + `Debug.R23` 0 error. Core khong ref Revit. File lon nhat 207 dong.

### File thuc te tao (khac plan)
Plan de xuat 8 file calculator; thuc te 10 vi tach de giu < 300 dong va giu 1 trach nhiem / file:

| File | Dong | Vai tro |
|---|---|---|
| `BarLayoutCalculator.cs` | 126 | port `GetBarModels` (4 vong while -> 4 vong theo canh) |
| `BarSideClassifier.cs` | 50 | `SideOf` + `QuadrantOf` |
| `DefaultOverlap.cs` | 21 | stagger 35d / 70d |
| `StirrupDistributionCalculator.cs` | 90 | `GetDistribute` + `CreateStirrupTypeItem1/2` |
| `SpliceCalculator.cs` | 207 | port `RefreshLocationBarModels` (redistribute theo canh / quadrant) |
| **`DefaultUpperPositions.cs`** | 122 | **MOI** — tach `GetNextX0`/`GetNextY0` ra khoi SpliceCalculator |
| `BarPolylineBuilder.cs` | 179 | port `GetLocationBottom*` + `GetLocationTop*` + `ConditionMultiCurve` + `GetLenght` |
| **`BarShapeClassifier.cs`** | 142 | **MOI** — tach phan phan loai shape cua `GetItemDivision` (230 dong nested) |
| `BarScheduleCalculator.cs` | 94 | port `GetMain` loop + `ConditionSameBarModel` |
| `CanvasScaleCalculator.cs` | 111 | port `GetScale` / `GetScaleSection` / `GetScaleDowels` |

Plan doi `SpliceCalculator` chua ca `BuildPolyline`; da tach sang `BarPolylineBuilder` (2 trach nhiem khac nhau, gop lai se > 350 dong).

### Doi chieu golden value voi source
Moi so trong plan da verify lai truc tiep tren source, **khop het**:
- `b=400,h=600,cover=25,ds=8,d=20,nx=3,ny=4` -> 10 bar, `deltaX=157`, `deltaY=514/3`, bar1 = `(West+43, South+43)`
- circular `nd=8, D=500` -> `r=207`
- `L=3000,S=150` -> `n=21`, offset `0`
- `L=3000,TypeDis=1,S1=100,S2=200` -> `L1=750, L2=1500, n1=n2=8`, offset `25 / 800 / 2275`
- splice canh Nam 3 thanh: `x01[0] = West'+cover+ds+d/2+d`, `x01[2] = ... -d + 2*delta'`
- polyline `TopDowels=0` -> 3 diem tren, `Z = Top-hb' / Top / Top+Lb`; thanh chan `Lb=35d`, le `70d`

### 3 dieu report/plan ghi sai — da sua theo source that
1. **`angle1` circular co 2 cong thuc khac nhau, khong phai 1.** Report §6 ghi `angle1 = 2d/(2r)`. Dung cho *redistribute trong quadrant* (`RefreshLocationBarModels`), nhung `GetNextX0`/`GetNextY0` dung **`angle1 = d / (r * 2 * PI)`** — chia cho chu vi, khong phai ban kinh. Da port ca 2 nguyen ban (`DefaultUpperPositions.Circular` vs `SpliceCalculator.CircularTransition`).
2. **`GetNextX0`/`GetNextY0` KHONG phai redistribute theo so thanh.** Report mo ta chung nhu la chia lai theo `count`. That ra chung giu nguyen luoi `nx`/`ny` cu, chi nhich `±d` o goc. Phan chia lai theo `count` nam trong `RefreshLocationBarModels`, la duong khac.
3. **Nhanh `unit.Convert(...)` trong `GetScale*` la dead code voi project mm.** `UnitProject.Convert(200)` = 200mm doi ra **feet** = 0.656, nen `maxWidth < 0.656` luon false. Ket qua thuc: `scale = max > threshold ? max/threshold : 1`. Da port dung dang do.

### Sai sot trong source giu nguyen — CAN USER QUYET
`GetItemDivision` (ProcessBarsDivision.cs:591-604) khi thanh **co moc duoi** map shape khong nhat quan:

| Dau tren | Map |
|---|---|
| `TopDowels==0`, khong lech XY | `LaBottom>0 ? DS04 : DS01` |
| `TopDowels!=0 && LaTop==0` | `LaBottom>0 ? **DS01** : **DS04**` <- nguoc |
| `!IsTopDowels` | `LaBottom>0 ? DS04 : DS01` |

2/3 nhanh cho DS04/DS01, nhanh giua nguoc lai. Rat giong typo cua tac gia goc. **Da port nguyen ban** (`BarShapeClassifier.cs:98-104`) vi doi se doi family detail-shop duoc dat trong ban ve. Neu user xac nhan la bug -> sua 1 dong.

### Chua lam (ngoai scope Phase 2)
`GetAddItemDivision` + `ConditionSameAddBarModel` (thep gia cuong AddBar) — Phase 4 moi tao AddBar nen chua co input de port. `GetStirrup`/`GetAddHorizontal`/`GetAddVertical` cua `ProcessBarsDivision` phu thuoc hinh hoc dai (Phase 3/4).
