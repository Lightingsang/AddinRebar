# Phân tích source R01_ColumnsRebar — tham chiếu cho mọi phase

Source: `F:/1-CONG VIEC/05-AI/01_Revit/02_Csharp/RebarAddin-master/RebarAddin-master/R01_ColumnsRebar` (~19.5k dòng C#, 1.7k XAML). Target Revit 2021, .NET 4.8. Dependency ngoài: `WpfCustomControls` (BaseViewModel, RelayCommand<T>, ProgressModel, TaskBarViewModel), `DSP` (UnitProject).

## 1. Pipeline

```
ColumnsRebarCmd.Execute
  PickObjects(StructuralColumnSelectionFilter)
  → OrderBy(SolidFace.GetBottom(x).Origin.Z)           xếp từ dưới lên
  → ErrorColumns.GetErrorColumns (15 rule)              fail → Cancelled
  → TransactionGroup "Columns Rebar"
      ColumnsViewModel(uidoc, doc, columns)
        ColumnsModel ctor → InfoModel[], StirrupModel[], BarMainModel[], BarsDivisionModel[], PlanarFaces
      ColumnsWindow.ShowDialog()
        OK → TransactionGroup "Action"
              CreateViewDimension.Create   (4 Transaction: detail view, section view, dim detail, dim section)
              CreateRebar.Create           (3 Transaction: stirrup, main, tag)   [IsRebar=true]
              CreateRebarDetailtem.Create  (detail item thay rebar)              [IsRebar=false]
              CreateDetailShop.Create      (nếu có family DS*)
              Commit
      Assimilate()
```

## 2. Hình học — face-based

`SolidFace.cs`: lấy `Solid` từ `element.get_Geometry(Options{ComputeReferences=true})` (Solid trực tiếp hoặc `GeometryInstance.GetInstanceGeometry()`), lọc `Volume != 0`, phải đúng 1 solid.

| Mặt | Điều kiện |
|---|---|
| Bottom / Top | `FaceNormal.AngleTo(BasisZ) ∈ {0, π}` → sort `Origin.Z` → `[0]`, `[1]` |
| South | `AngleTo(BasisZ)=π/2` ∧ `AngleTo(-Y) < π/4` ∧ `AngleTo(-X) > π/4` |
| North | … `AngleTo(+Y) < π/4` ∧ `AngleTo(+X) > π/4` |
| West | … `AngleTo(-X) < π/4` ∧ `AngleTo(+Y) > π/4` |
| East | … `AngleTo(+X) < π/4` ∧ `AngleTo(-Y) > π/4` |

`SectionStyle`: 2 mặt ∥Z + 4 mặt ⊥Z + 0 CylindricalFace = RECTANGLE; có CylindricalFace = CYLINDICAL; khác = ORTHER (reject).

`b = dist(West, East.Origin)`, `h = dist(South, North.Origin)`, `hc = dist(Top, Bottom.Origin)`.

## 3. Datum tương đối

Mọi vị trí quy về datum cột đáy:
- `planarFace0` = mặt trên của element đỡ cột[0], tìm theo chain **Foundation → Floor → Wall → Beam(same base level)**; mỗi bước yêu cầu level == level thấp nhất project, solid đúng 2 mặt ngang. Fallback = `Bottom` cột[0].
- `south0`, `west0` = mặt Nam/Tây cột[0].
- `TopPosition/BottomPosition = dist(planarFace0, Top/Bottom.Origin)`; `WestPosition/EastPosition = dist(west0, …)`; `SouthPosition/NouthPosition = dist(south0, …)`.
- Cột tròn: `PointXPosition/PointYPosition = LocationPoint - point0` của cột[0].

Dựng ngược XYZ (`BarModel.GetPoint`):
```
rect: p = p0 + X·East.FaceNormal + Y·Nouth.FaceNormal + Z·BasisZ    (p0 = West.Origin project lên South rồi lên planarFace0)
cyl : p = p0 + X·BasisX + Y·BasisY + Z·BasisZ                        (p0 = LocationPoint project lên planarFace0)
```

## 4. Dầm giao đầu cột — hb, zb

`ColumnsBoundingBox.GetBeamsBoudingBoxSameTopLevelPerpencularZOneColumn`: `BoundingBoxIntersectsFilter(column bbox)` ∧ `OST_StructuralFraming` ∧ `INSTANCE_REFERENCE_LEVEL_PARAM == column FAMILY_TOP_LEVEL_PARAM` ∧ có mặt ngang.
- `hb` = max distance giữa các mặt ngang của mọi dầm (chiều cao dầm lớn nhất)
- `zb` = dist(Top cột, mặt dầm thấp nhất)
- Không dầm → `hb = zb = 0`; khi tính dowel fallback `hb = max(b,h)` (rect) / `D` (cyl).

## 5. Thép chủ — layout (BarMainModel)

Rect: `nx × ny`, tổng `2nx + 2(ny-2)`, đánh số vòng: Nam `1..nx` → Đông `nx+1..nx+ny-2` → Bắc `..2nx+ny-2` → Tây phần còn lại.
```
ds     = đường kính đai
deltaX = (b - 2·Cover - 2·ds - d) / (nx-1)
deltaY = (h - 2·Cover - 2·ds - d) / (ny-1)
x0     = WestPosition  + Cover + ds + d/2 + i·deltaX
y0     = SouthPosition + Cover + ds + d/2 + j·deltaY
```
Cyl: `nd ∈ {4,8,12,16,20}`, `angle = i·2π/nd`, `r = D/2 - Cover - ds - d/2`, tâm = `(PointXPosition, PointYPosition)`.

Defaults: `nx=ny=2` (rect), `nd=4` (cyl), `SplitOverlap=50`, `Overlap=35`, bar index 3 trong list sorted theo diameter.

## 6. Nối chồng — polyline Location

Mỗi `BarModel.Location : List<(X,Y,Z)>`. Tạo bằng `Rebar.CreateFreeForm(doc, barType, host, [CurveLoop])`.

**Bottom** (`GetLocationBottom*`):
- `!IsBottomDowels` → `(X0,Y0,BottomPosition)`
- `BottomDowels==0` → `(X0,Y0,BottomPosition + Lc)`
- else `La==0` → `(X0,Y0,BottomPosition - Lb)`; `La>0` → thêm điểm móc ngang `±La` theo cạnh rồi `(X0,Y0,BottomPosition - Lb)`

**Top** (`GetLocationTop*`), `hb' = (zb+hb==0) ? max(b,h) : zb+hb`:
- `!IsTopDowels` → `(X0,Y0,TopPosition - Cover)`
- `TopDowels==0` (bẻ xiên chuyển tiếp):
  ```
  (X0,  Y0,  TopPosition - hb')      bắt đầu bẻ dưới đáy dầm
  (x01, y01, TopPosition)            đã dịch sang tiết diện cột trên
  (x01, y01, TopPosition + Lb)       neo lên cột trên
  ```
- `TopDowels!=0` → `(X0,Y0,TopPosition - Cover - zb)` + móc ngang `±La` nếu `La>0`

**x01,y01** (`RefreshLocationBarModels` với `infoModelUp`): chỉ thanh `IsTopDowels && TopDowels==0` được **tái phân bố** trên tiết diện cột trên, theo từng cạnh:
```
barsOnSide = BarModels.Where(cạnh này && IsTopDowels && TopDowels==0).OrderBy(BarNumber)
deltaX'    = (bUp - 2·Cover - 2·dsUp - d) / (count - 1)
x01        = WestPositionUp + Cover + dsUp + d/2 + index·deltaX'  ± d   (± d ở thanh biên để né thép cột trên)
```
Cạnh Đông/Tây chia `(count+1)` khoảng, `-d`. Cyl `nd>4`: chia góc `π/2` mỗi phần tư, offset `angle1 = 2d/(2r)`.

**So le 50%** (ctor `BarModel`):
```
LbTop = (SplitOverlap==50) ? (BarNumber%2==0 ? 35d : 70d) : 35d
```

`GetCurveLoop`: thanh thẳng (`ConditionMultiCurve`) → 1 Line; có bẻ → nối từng đoạn; case đặc biệt 2 đoạn khi 3 điểm cuối trùng XY.

## 7. Đai — phân bố (StirrupModel)

```
L = IsTiesUp ? (l + hb + zb) : l          l = hc - hb - zb
TypeDis 0: L1=L2=0 (đều @S)
TypeDis 1: L1=L/4, L2=L/2
TypeDis 2: L1=L/6, L2=4L/6
TypeDis 3: L1=L/8, L2=6L/8
```
Đều: `n = floor(L/S)+1`, `o1 = (L-(n-1)S)/2` (căn giữa).
3 vùng: `n1=floor(L1/S1)+1`, `n2=floor(L2/S2)+1`, `δ1=(L1-(n1-1)S1)/2`, `δ2=(L2-(n2-1)S2)/2`; offset `o1=δ1`, `o2=δ2+L1`, `o3=δ1+L1+L2`.

Tạo: `Rebar.CreateFromRebarShape(doc, shape, barType, host, origin, xVec, yVec)` → `GetShapeDrivenAccessor().ScaleToBox(origin, vecB, vecH)` → `SetLayoutAsNumberWithSpacing(n, s_ft, true, true, true)`.
- origin rect = `South.Origin` project lên `West` + `(x+cover)·East.n` + `(y+cover)·Nouth.n` → project lên `Bottom` + `location·Z`; xVec/yVec từ `Transform.CreateRotation(East.n, π/2)` → `BasisZ`, `BasisX`.
- box rect: `vecB = (b-2cover)·East.n`, `vecH = (h-2cover)·Nouth.n`.
- Shape bắt buộc: rect `M_T1` (SettingModel.SelectedShapeStirrup), cyl `M_T3`.
- Đai phụ H/V (`AddH/AddV`, `TypeH/TypeV`, `nH/nV`, `aH/aV`) — shape `SelectedShapeAnti`, code trong `#region Addtitional stirrup` (không đọc chi tiết, cần đọc khi cook Phase 4).

## 8. Views / Dimension / Tag / Detail shop

- `DetailColumnView`: `ViewFamilyType` tên `@ColumnDetail` (duplicate nếu chưa có) → `ViewSection.CreateSection(doc, vft, BoundingBoxXYZ)` ×2 (X, Y) với Transform từ `East.n`/`Nouth.n`; crop off; set `ViewTemplateId`.
- `SectionColumnView`: `@ColumnSection`, 1 view Mid mỗi cột tại `location2` (`L·0.5` hoặc `L1 + L2·0.5`); Transform `CreateRotation(BasisX, π)`.
- `DimensionView`: `doc.Create.NewDimension(view, line, ReferenceArray, type)`. Reference lấy từ `PlanarFace.Reference.ConvertToStableRepresentation` rồi **`.Replace("SURFACE","LINEAR")`** → `ParseFromStableRepresentation`. Hack undocumented, fragile.
- `TagColumn`: bảng thống kê bằng `TextNote.Create` + `DetailCurve` (không phải IndependentTag). `RebarBarModel` có code `IndependentTag.Create` + `MultiReferenceAnnotation` nhưng **toàn bộ đang comment out**.
- `DetailColumnView.CreateSchedule`: `ViewSchedule.CreateSchedule(OST_DetailComponents)` + field Image / Element Host / Length + filter BeginsWith ColumnsName.
- `ProcessBarsDivision` → `ItemDivision(Name, NoBar, Diameter, L, La, Lb)`, `Length = L+La+Lb`, merge bằng `ConditionSameBarModel`. `DivisionBar.NumberColumns` gom nhiều cột. `DetailShopStyle DS00/DS01/DS02` → family instance.
- `CreateRebarDetailtem` (`IsRebar=false`): thay rebar bằng Detail Item family `DT00..DT07A` (`DetailItemStyle`). Phần lớn file là code Beams comment out; live chỉ `CreateStirrupBar` (~120 dòng).

## 9. UI gốc

`ColumnsWindow` 196 dòng: ListView menu 8 item (icon vẽ bằng `DrawIcon` lên Canvas) + `ContentControl` bind `SelectedViewModel` (DataTemplate theo type) + ScrollViewer `MainCanvas` mặt đứng + ProgressBar "Progress" + TaskBar (logo/YouTube/Account — **bỏ**).
8 VM: Setting 102 · Geometry 113 · Stirrups 267 · AdditionalStirrups 404 · Bars 273 · TopDowels 517 · BottomDowels 370 · BarsDivision 176. Pattern: `RelayCommand<ColumnsWindow>` nhận cả Window làm param rồi `FindChild<Canvas>` → vẽ. Toàn bộ phải đổi sang binding.
Draw: `DrawMainCanvas` 762 (mặt đứng + section + dowels), `DrawImage` 2219 (primitive: dim, stirrup, hook, layer bar…), `DrawItem` 1428, `DrawImageRebar` 567, `DrawIcon` 451, `DrawDetailShop`. `DrawModel` giữ scale/left/top/color/stroke.

## 10. Bug xác định trong source — PHẢI sửa khi port

| # | Bug | Vị trí | Sửa |
|---|---|---|---|
| B1 | `GetErrorColumns` không `break` → báo lỗi cuối, không phải lỗi đầu | ErrorColumns.cs:13-30 | return ngay rule đầu fail |
| B2 | `IsnotVerticalColumns` `return` trong vòng lặp → chỉ check cột[0] | ErrorColumns.cs:47 | `All(VerticalOneColumn)` |
| B3 | `planarFaces.OrderBy(...)` không gán → no-op | ProccessInfoClumns:40,110,130,155; ColumnsBoundBox:GetWall/GetFloor levels; SolidFace.GetTopPlanarFaceBeam | `= .OrderBy().ToList()` hoặc `Min/Max` |
| B4 | `Bar = Bar;` self-assign, param `rebarBarModel` bị vứt | BarMainModel.cs:63 | `Bar = rebarBarModel` |
| B5 | `!level.Equals(level0)` so `ElementId` với `Level` → luôn true → wall luôn null | ColumnsBoundBox.GetWallBoudingBoxOneColumn | `level0.Id` |
| B6 | `double.Parse(UnitFormatUtils.Format(...))` — parse chuỗi đổi đơn vị, phụ thuộc culture/decimal | PointModel.DistanceTo2, InfoModel, RebarBarModel, ColumnsModel | `UnitUtils.ConvertFromInternalUnits(x, UnitTypeId.Millimeters)` |

Ngoài ra: `MessageBox` trong model layer (StirrupModel:220, DetailColumnView) → TaskDialog ở VM/command; `ViewModel` giữ `TransactionGroup` thứ 2 lồng trong TransactionGroup của command — hợp lệ nhưng thừa, port giữ 1 group ở command.

## 11. API surface đã verify (NuGet XML 2023.1.90 + 2026.4.10)

Có ở cả 2: `Rebar.CreateFreeForm(doc, barType, host, IList<CurveLoop>, out RebarFreeFormValidationResult)`, `Rebar.CreateFromRebarShape`, `RebarShapeDrivenAccessor.ScaleToBox`, `SetLayoutAsNumberWithSpacing`, `Document.Create.NewDimension`, `ViewSection.CreateSection`, `MultiReferenceAnnotation.Create`, `Reference.ParseFromStableRepresentation`, `new ElementId(BuiltInCategory)`, `UnitTypeId.*`, `SpecTypeId.Length`.
`RebarHookOrientation` còn ở 2026 (chỉ mất ở 2027, ảnh hưởng `CreateFromCurves` — không dùng).
**Chưa verify 2027** — package không có trong cache.
