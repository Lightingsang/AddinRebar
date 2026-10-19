# Handoff Report: Legacy Catalogs & Static Assets Mapping (Explorer Survey 3)

- **Date**: 2026-10-18T05:45:00Z
- **Author**: Explorer Survey 3 (`explorer_survey_3`)
- **Parent Orchestrator**: `orchestrator_25` (`1b2bf217-fcc4-4416-87ff-86b0fe0ef59a`)
- **Milestone**: `hpdcepro_survey_3_catalogs_and_assets`
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\`
- **Authoritative User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (`## 2026-10-18T05:27:06Z`)

---

## 1. Observation

### 1.1 Machine-Readable Catalogs in `C:\Dce Pro\AiAgent\DanhBa\*.json`

Live inspection of `C:\Dce Pro\AiAgent\DanhBa\` confirmed 9 JSON catalog files and 1 text dictionary:

| Catalog File | File Size (Bytes) | Item Count | Catalog Schema / Purpose |
|---|---:|---:|---|
| `danhba_lenh.json` | 376,293 | **311** | Master AutoCAD `<CommandMethod>` catalog (prompts, forms, selections, drawing output flags) |
| `danhba_dce_excel.json` | 110,884 | **73** | Excel Ribbon XML commands and `HostRibbon.Dispatch` entry points |
| `danhba_dce_thep.json` | 79,678 | **49** | Rebar detailing & editing façade functions (`MatTienThep.*`) |
| `danhba_mbkc.json` | 68,725 | **29** | Structural framing plan façade functions (`MatTienMbkc.*`) |
| `danhba_cad.json` | 310,856 | **362** | Low-level CAD entity and geometry functions (`DceCad.vb` + `DceMath.vb`) |
| `danhba_cad_api.json` | 944,835 | **1,307** | CAD Object Model API signatures probed across 5 CAD engines (AutoCAD, ZWCAD, BricsCAD, GstarCAD, IntelliCAD) |
| `danhba_excel.json` | 403,841 | **630** | Excel Interop COM API catalog (`Microsoft.Office.Interop.Excel 15.0`) |
| `danhba_etabs.json` | 1,217,726 | **1,520** | ETABS OAPI function catalog |
| `danhba_kynang.json` | 88,540 | **22** | High-level macro workflows / skill execution pipelines |
| `tudien_viet_anh.txt` | 406 | 7 lines | Vietnamese - English engineering dictionary keywords |

#### A. Master AutoCAD `<CommandMethod>` Catalog (`danhba_lenh.json`, 311 entries)
- **Total entries**: 311 commands.
- **Interactive Flags**:
  - `can_form = true`: 88 commands require a modal/modeless dialog.
  - `can_chon = true`: 244 commands require interactive CAD entity/point selection prompts.
  - `ghi_ban_ve = true`: 245 commands produce or modify entities on the AutoCAD drawing canvas.
  - `co_the_tu_dong = true`: 51 commands can run fully headless/unattended without user prompts.
- **Breakdown by Business Domain (`nhom_nghiep_vu`)**:
  - `dầm` (Beams): 77 commands (e.g. `VeDam_DCE`, `VeDamAll_DCE`, `ShopDrawingBeam`, `DatTenCauKienDam`, `ExportToRevit`, `VeBlockDam2`, `BlockBeam_Size`).
  - `thép` (Rebar Detailing & Schedules): 63 commands (e.g. `ToiUuCatThep`, `SuaCKThep`, `tkt_dce`, `stk_dce`, `TongHopThongKe_dce`, `MaTTThep`, `GanLienKetThep`).
  - `cột` (Columns): 39 commands (e.g. `VeCotDcePro`, `ImportKichThuocCot`, `ColumnREC1`, `ColumnREC2`, `ColumnCi`, `VeMCD_DCE`).
  - `sàn` (Slabs): 26 commands (e.g. `VeThepSanTopX`, `VeThepSanTopY`, `VeThepSanBotX`, `VeThepSanBotY`, `SoHieuThepSan`, `ThongKeThepSan`).
  - `móng` (Foundations & Piles): 26 commands (e.g. `VeMongCoc_DCE`, `VeMongDon_DCE`, `VeBlockCoc`, `DanhTenCoc`, `XuatToaDoCoc`, `StripMong`, `RaiDaiCoc`).
  - `vách` (Shear Walls & Cores): 16 commands (e.g. `VeVachDcePro`, `DceWallCore`, `VeVachCore`).
  - `etabs` (ETABS / Analysis Sync): 15 commands (e.g. `XuatDuLieuModelEtabs`, `UpdateModelEtabs`, `UpdateThepTinhToan`).
  - `autostr` (Automated Pipeline Steps 1–24): 12 commands (e.g. `AutoStr`, `AutoStrDoDam`, `AutoStrDoLayerKienTruc`, `AutoStrMbkcAudit_Trich`).
  - `cầu thang` & `lanh tô` (Stairs & Lintels): 10 commands (e.g. `VeThang_DCE`, `VeBlockLanhTo`, `VeMatCatLanhTo`).
  - `tiện ích & hệ thống` (Utilities, Layouts, Licensing, AI): 27 commands (e.g. `GETFORM`, `taomv`, `DceSetup`, `ChiaKhoangCach`, `Ai_Lenh`).

#### B. Excel Ribbon Commands & Dispatch Actions (`danhba_dce_excel.json`, 73 entries)
- **Source**: `RibbonXml.vb` mapped to `HostRibbon.Dispatch(id, ...)` in `DCE Excel.dll`.
- **Total entries**: 73 commands.
- **Breakdown by Subsystem (`nhom`)**:
  - `dce_excel/dam` (15 commands): `Bea_ExportSize`, `Bea_VeDamExcel`, `Bea_XuatBangNoiLuc`, `Bea_NapDuLieuDam`, `Bea_ChonThepTuDong`, `Bea_TinhThep`, `Bea_ToHopNoiLuc`, `Bea_XuatThuyetMinh`, etc.
  - `dce_excel/cot_vach` (11 commands): `Col_VeCotExcel`, `Col_NapDuLieuCot`, `Col_TinhCot`, `Wal_TinhVach`, `Col_XuatBieuDoTuongTac`, etc.
  - `dce_excel/san` (10 commands): `Sla_TinhSanO`, `Sla_XuatThuyetMinhSan`, `Sla_ToHopNoiLucSan`, `Sla_VeThepSanExcel`, etc.
  - `dce_excel/etabs_safe` (7 commands): `Etb_DocNoiLucEtabs`, `Etb_DongBoTietDien`, `Safe_DocNoiLucSafe`, etc.
  - `dce_excel/tai_khoan` (7 commands): `Acc_DangNhap`, `Acc_DangXuat`, `Acc_KiemTraBanQuyen`, `Acc_KichHoat`, etc.
  - `dce_excel/cot` (6 commands): `Col_ChiTietCot`, `Col_ThongKeThepCot`, etc.
  - `dce_excel/cau_noi_cad` (4 commands): `Cad_GuiDuLieuSangCad`, `Cad_NhanDuLieuTuCad`, `Cad_DongBoHaiChieu`, `Cad_MoBanVeTuongUng`.
  - `dce_excel/ket_noi_api` (3 commands): `Api_GuiTinNhan`, `Api_DongBoDuLieuCloud`, `Api_KiemTraPhienBan`.
  - `dce_excel/mong` (3 commands): `Fnd_TinhMongDon`, `Fnd_TinhMongCoc`, `Fnd_XuatThuyetMinhMong`.
  - `dce_excel/to_hop_thep` (3 commands): `Stl_ToiUuCatThep`, `Stl_TongHopKhoiLuong`, `Stl_XuatBangCatThep`.
  - `dce_excel/mat_tien_doc` (2 commands): `Ai_DocToanBoSheet`, `Ai_DocKetQuaTinh`.
  - `dce_excel/vach` (1 command): `Wal_VeChiTietVach`.
  - `dce_excel/meta` (1 command): `DceExcel_ThongTinPhienBan`.

#### C. DceSteel Facade Functions (`danhba_dce_thep.json`, 49 entries)
- **Source**: `MatTienThep.vb` in `DCE Pro.dll`.
- **Total entries**: 49 functions.
- **Functional Groups**:
  - `thep/sua` (23 functions): `DoiDuongKinhThep`, `DoiSoLuongThep`, `DoiKhoangCachDai`, `DoiLopBaoVe`, `NoiThanhThep`, `CatThanhThep`, `UonMocThep`, `XoayThep`, `OffsetThep`, etc.
  - `thep/xdata` (9 functions): `DocXDataThep`, `GhiXDataThep`, `XoaXDataThep`, `DungChuoiXDataThongKe`, `LietKeCauKienTrongBanVe`, `LietKeThepTheoCauKien`, `XemTruocTimThayTheXData`, etc.
  - `thep/kiem` (4 functions): `DocLienKetCuaThep`, `DocLienKetCuaTag`, `DocDimKhoangRai`, `SoatThepSan`.
  - `thep/thongke` (3 functions): `LietKeBlockThongKeThep`, `ChayThongKeThep`, `TongHopThep`.
  - `cai_dat` (3 functions): `LietKeCaiDat`, `DocCaiDat`, `GhiCaiDat`.
  - `thep/lien_ket` (2 functions): `TimThepChuaLienKet`, `LietKeLienKetTheoCauKien`.
  - `thep/thong_ke` (2 functions): `XuatThongKeExcel`, `ResetBangThongKe`.
  - `thep/meta` (1 function): `PhienBan`.
  - `thep/doc` (1 function): `DocChuTagThep`.
  - `thep/mau` (1 function): `ThongKeTheoDuongKinhVaMau`.

#### D. MBKC Facade Functions (`danhba_mbkc.json`, 29 entries)
- **Source**: `MatTienMbkc.vb` in `DCE Pro.dll`.
- **Total entries**: 29 functions.
- **Functional Groups**:
  - `mbkc/doc` (8 functions): `TomTatMatBang`, `SoatMatBang`, `LietKePhan`, `SoatMatBangDangMo`, `KiemTraLuatMatBang`, `DeXuatTietDienTheoDanhMuc`, `KiemTraLuatMong`, `DocToanBoChuBanVe`.
  - `mbkc/ghi` (4 functions): `QuanLyMatBang`, `QuanLyMong`, `DanhDauLoiMatBang`, `XoaDanhDauLoi`.
  - `mbkc/excel_dam` (4 functions): `NapMotDamVaoSheetVe`, `DocDanhSachDamVe`, `XuatDamDeVe`, `VeDamDangNap`.
  - `mbkc/chuan_hoa` (2 functions): `ChuanHoaMatBangTuVungChon`, `TachMatBangRaFile`.
  - `mbkc/sua` (2 functions): `SuaMatBang`, `SuaMatBangFile`.
  - `autostr/tach_kt` (2 functions): `XemTangKienTruc`, `TachKienTruc`.
  - `mbkc/etabs` (2 functions): `TimGocToaDoEtabs`, `DoiChieuCotEtabs`.
  - `mbkc/meta` (1 function): `PhienBan`.
  - `mbkc/kiem` (1 function): `CanhBaoMatBang`.
  - `mong/ghi` (1 function): `VeMongDon`.
  - `mbkc/khoi_luong` (1 function): `ThongKeBeTong`.
  - `mbkc/nhan_dang` (1 function): `NhanDangMatBangKetCau`.

---

### 1.2 Native C++ Exports from `DceAnalyssiss64.dll` (69 exports)

PE Export Table parsing of `C:\Dce Pro\DceAnalyssiss64.dll` (size 346,624 bytes) confirmed **exactly 69 exported functions** (identical export count in `DceAnalyssiss32.dll`):

| Ordinal | Function Name | RVA | Classification / Subsystem |
|:---:|---|:---:|---|
| 0 | `CalculateExpression` | `0x4d40` | Beam Span/Support Formula |
| 1 | `CalculateExpression2` | `0x4d60` | Beam Span/Support Formula |
| 2 | `CalculateExpression3` | `0x4dc0` | Beam Span/Support Formula |
| 3 | `CalculateRatio` | `0x4d90` | Rebar Cutoff Ratio |
| 4 | `CalculateRatio2` | `0x4da0` | Rebar Cutoff Ratio |
| 5 | `CheckAssemblyIntegrity` | `0xa7b0` | Integrity & Anti-Tamper |
| 6 | `DceMath_CheckBang0` | `0x8380` | Math: Floating Zero Check |
| 7 | `DceMath_CheckNamGiua` | `0x8c00` | Math: Point Between Bounds |
| 8 | `DceMath_CheckNamGiuaDaGiac` | `0xa560` | Math: Point Inside Polygon |
| 9 | `DceMath_CheckThangHang` | `0x8db0` | Math: Collinearity Check |
| 10 | `DceMath_ConvexHull` | `0x9770` | Geometry: Convex Hull 2D |
| 11 | `DceMath_DegToRad` | `0x94d0` | Math: Angle Conversion |
| 12 | `DceMath_DoDaiVector` | `0x80b0` | Math: Vector Magnitude |
| 13 | `DceMath_GiaiHePhuongTrinh` | `0x8f10` | Math: Linear System Solver |
| 14 | `DceMath_GiaiPTBac2` | `0x8f60` | Math: Quadratic Equation Solver |
| 15 | `DceMath_GiaoDiem2DuongThang` | `0x87d0` | Geometry: Line-Line Intersection |
| 16 | `DceMath_GiaoDiemDTvaMP` | `0x9340` | Geometry: Line-Plane Intersection |
| 17 | `DceMath_Goc2MatPhang` | `0x9220` | Geometry: Dihedral Angle |
| 18 | `DceMath_Goc2Vector` | `0x8200` | Geometry: Vector-Vector Angle |
| 19 | `DceMath_KcDiemDenMatPhang` | `0x9190` | Geometry: Point-to-Plane Distance |
| 20 | `DceMath_KhoangCach2Diem` | `0x80e0` | Geometry: Point-to-Point Distance |
| 21 | `DceMath_LocalToGlobal` | `0x9690` | Geometry: Coordinate Transform |
| 22 | `DceMath_MidPoint` | `0x83a0` | Geometry: Midpoint Calculation |
| 23 | `DceMath_NoiSuy1Chieu` | `0x9510` | Math: 1D Linear Interpolation |
| 24 | `DceMath_NoiSuy2Chieu` | `0x9570` | Math: 2D Bilinear Interpolation |
| 25 | `DceMath_Offset_x` | `0x8480` | Geometry: Point Offset X |
| 26 | `DceMath_Offset_xy` | `0x85d0` | Geometry: Point Offset XY |
| 27 | `DceMath_Offset_y` | `0x8520` | Geometry: Point Offset Y |
| 28 | `DceMath_PTMatPhang` | `0x9050` | Geometry: Plane Equation |
| 29 | `DceMath_PointInPolygonRayCast` | `0xa3b0` | Geometry: Raycast Point-in-Poly |
| 30 | `DceMath_PointInPolygonWinding` | `0xa1b0` | Geometry: Winding Number Point-in-Poly |
| 31 | `DceMath_PolarPoints` | `0x83e0` | Geometry: Polar Coordinate Point |
| 32 | `DceMath_ProjectPointToInfiniteLine` | `0x8b30` | Geometry: Point Projection on Line |
| 33 | `DceMath_QuayChieuKimDongHo` | `0x9470` | Geometry: Orientation Check (CW/CCW) |
| 34 | `DceMath_RadToDeg` | `0x94f0` | Math: Angle Conversion |
| 35 | `DceMath_TichCoHuong` | `0x8190` | Math: Vector Cross Product |
| 36 | `DceMath_TichDiemVaMatPhang` | `0x9170` | Geometry: Point-Plane Product |
| 37 | `DceMath_TichVoHuong` | `0x8170` | Math: Vector Dot Product |
| 38 | `DceMath_VectoChiPhuong` | `0x8130` | Geometry: Unit Direction Vector |
| 39 | `DceMath_VectorAngleOX` | `0x8330` | Geometry: Vector Angle with X-Axis |
| 40 | `GenerateIntegrityFile` | `0x5470` | Integrity & Anti-Tamper |
| 41 | `Geom_DistancePointToSegment` | `0x5e10` | Geometry: Point-Segment Distance |
| 42 | `Geom_GetCirclePolygon` | `0x5820` | Geometry: Circle Polygon Discretizer |
| 43 | `Geom_GetPolygonArea` | `0x5c80` | Geometry: 2D Polygon Area |
| 44 | `Geom_GetPolygonCentroid` | `0x5900` | Geometry: 2D Polygon Centroid |
| 45 | `Geom_GetSegmentLenInPolygon` | `0x6740` | Geometry: Segment Clipping in Poly |
| 46 | `Geom_IntersectLinesInfinite` | `0x6630` | Geometry: 2D Infinite Line Intersection |
| 47 | `Geom_IsPointInsidePolygon` | `0x5f40` | Geometry: Point in Polygon Test |
| 48 | `Geom_IsPolygonIntersectLine` | `0x6580` | Geometry: Poly-Line Intersection Test |
| 49 | `Geom_IsPolygonOverlap` | `0x6300` | Geometry: Polygon Overlap / Clash |
| 50 | `Geom_ProjectPointToLine` | `0x6c80` | Geometry: Point Projection on Ray |
| 51 | `Geom_ProjectPointToLine2Pts` | `0x6ce0` | Geometry: Point Projection (2 Points) |
| 52 | `Geom_SegmentVsSegment` | `0x61b0` | Geometry: Segment-Segment Intersect |
| 53 | `HalfP` | `0x4db0` | Beam Span/Support Formula |
| 54 | `KhoangCachDenDiemPhaiGoi` | `0x5030` | Beam Rebar: Distance to Right Support Face |
| 55 | `KhoangCachDenDiemTraiGoi` | `0x4eb0` | Beam Rebar: Distance to Left Support Face |
| 56 | `MBKC_AngleBetweenVectors` | `0x7050` | MBKC: Vector Angle |
| 57 | `MBKC_AreBeamsConnected` | `0x7160` | MBKC: Beam Collinearity & Connectivity |
| 58 | `MBKC_CalculateCircumcircle` | `0x6db0` | MBKC: Delaunay Triangulation Circumcircle |
| 59 | `MBKC_ContainsInCircumcircle` | `0x6fb0` | MBKC: Circumcircle Point Test |
| 60 | `MBKC_GetBeamGroupDirection` | `0x7ca0` | MBKC: Beam Group Dominant Direction |
| 61 | `MBKC_GetBeamGroupRefPoint` | `0x7cc0` | MBKC: Beam Group Origin Datum |
| 62 | `MBKC_GetNearestNodeWithinTol` | `0x7e00` | MBKC: Nearest Node Snapper |
| 63 | `MBKC_GetTriangleArea` | `0x7010` | MBKC: Triangle Area |
| 64 | `MBKC_OrderBeamGroup` | `0x7830` | MBKC: Topological Sort of Beam Spans |
| 65 | `MyFileD` | `0x3170` | Internal File I/O Descriptor |
| 66 | `TimSoDoan` | `0x4e50` | Beam Detailing: Find Rebar Segment Count |
| 67 | `TimSoNhip` | `0x4df0` | Beam Detailing: Find Span Count |
| 68 | `dgfhfhcxdfg` | `0x3ae0` | Obfuscated License / Machine Key Validator |

---

### 1.3 Unpacked Revit 2026 Add-In (`DBIM_RVT_26.exe`)

Execution of `C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe` with silent unpack parameters into `scratch\dce_rvt_unpacked\` successfully extracted:
- `app\2026\DBimRevit.addin`: AddIn manifest registering `DBimRevit.AdskApplication`.
- `app\2026\AddinDbimDcepro\DBimRevit.dll` (size 2,254,336 bytes).
- Accompanying native and managed assemblies: `CSiAPIv1.dll`, `ETABSv1.dll`, `ETABS2016.dll`, `ETABSv17.dll`, `DceAnalyssiss64.dll`, `SQLite.Interop.dll`, `EntityFramework.dll`.

Inspection of CLI metadata in `DBimRevit.dll` via `ilspycmd --dump-table TypeDef / InterfaceImpl` proved **exactly 23 classes implementing `Autodesk.Revit.UI.IExternalCommand`** (Token `0x01000089`) and **1 class implementing `Autodesk.Revit.UI.IExternalApplication`** (Token `0x0100001D`):

| # | Revit Command Class Name | Metadata Token | Subsystem & Functionality |
|:---:|---|:---:|---|
| 0 | `DBimRevit.AdskApplication` | `0x0200000A` | `IExternalApplication` (creates DBIM Revit Ribbon & PushButtons) |
| 1 | `DBimRevit.CommandLogin` | `0x02000011` | Cloud Account Login |
| 2 | `DBimRevit.CommandLogout` | `0x02000012` | Cloud Account Logout |
| 3 | `DBimRevit.CommandAddBeam` | `0x0200001B` | Generate Structural Beams from CAD framing plan coordinates |
| 4 | `DBimRevit.CommandAddColumn` | `0x0200001F` | Generate Structural Columns from CAD framing plan coordinates |
| 5 | `DBimRevit.CommandAddGird` | `0x02000021` | Generate Structural Grid Lines from CAD grid lines |
| 6 | `DBimRevit.CommandAddSlab` | `0x02000022` | Generate Structural Floor Slabs from CAD boundary polylines |
| 7 | `DBimRevit.CommandAddWall` | `0x02000025` | Generate Structural Walls / Cores from CAD wall polylines |
| 8 | `DBimRevit.CommandAutoCut` | `0x02000028` | Auto-Cut intersecting structural framing & wall elements |
| 9 | `DBimRevit.CommandAutoJoin` | `0x0200002A` | Auto-Join framing, columns, slabs, and walls |
| 10 | `DBimRevit.CommandBatchUnjoin` | `0x0200002C` | Batch Unjoin structural elements |
| 11 | `DBimRevit.CommandBeamDisallowJoin` | `0x0200002E` | Toggle Disallow Join at beam end handles |
| 12 | `DBimRevit.CommandCreateCope` | `0x02000031` | Apply structural coping between beams |
| 13 | `DBimRevit.CommandJoinByOrder` | `0x02000034` | Reorder Join precedence (Column > Wall > Beam > Slab) |
| 14 | `DBimRevit.DceBeamBreaks` | `0x0200003A` | Split continuous beams at column/support intersections |
| 15 | `DBimRevit.CommandDatTenDam` | `0x0200004A` | Auto-Name beams sequentially along grid lines |
| 16 | `DBimRevit.CommandDceSetup` | `0x0200004D` | Open DCE Revit Configuration & Family Mapping dialog |
| 17 | `DBimRevit.CommandExportBeamToExcel` | `0x0200004F` | Export beam geometry and parameters to Excel calculation workbook |
| 18 | `DBimRevit.CommandExportToAutocad` | `0x02000051` | Export Revit framing plan view to DWG with DCE XData |
| 19 | `DBimRevit.CommandCreateAnalytical` | `0x02000053` | Generate Analytical Model (Member/Panel) from physical elements |
| 20 | `DBimRevit.CommandEtabsToRevit` | `0x0200005B` | Import geometry and member sections from ETABS into Revit |
| 21 | `DBimRevit.CommandExportToEtabs` | `0x0200005D` | Export Revit framing model to ETABS (.sdb / OAPI) |
| 22 | `DBimRevit.CommandXuatKhoiLuong` | `0x02000064` | Extract Bill of Quantities (concrete volume, formwork, rebar) |
| 23 | `DBimRevit.CommandAddRebarBeam` | `0x02000072` | Automated 3D rebar generation for continuous host beams |

*Decompilation caveat*: `DBimRevit.dll` uses an obfuscation/protection wrapper (`QF3HDhLgvyaOAfQS7A4`). The IL bodies of private worker methods are protected and require static opcode decryption or dynamic tracing.

---

### 1.4 Inventory of Static Engineering Assets in `C:\Dce Pro`

Audited with zero mutation, capturing file size, path, and SHA-256 digests:

#### A. 17 DWG Block & Titleblock Templates
| File Path in `C:\Dce Pro\` | Size (Bytes) | SHA-256 Digest (Prefix) | Description |
|---|---:|:---:|---|
| `Cad\Form 1.dwg` | 376,397 | `3120abb18a7483ae` | Standard Beam Detailing Template 1 |
| `Cad\Form 1_Steel.dwg` | 1,244,728 | `deaca99b371a8fd9` | Steel Framing Detailing Template 1 |
| `Cad\Form 2.dwg` | 400,630 | `9ddd5466441ba6f1` | Standard Beam Detailing Template 2 |
| `Cad\Form 2_2.dwg` | 395,143 | `e5e3eafdd5b60250` | Standard Beam Detailing Template 2 (Variant 2) |
| `Cad\Form 2_2_Circle3.2.dwg` | 382,953 | `c2d4a7ca6d3b1ac6` | Beam Template 2_2 with 3.2mm circular axis tags |
| `Cad\Form 2_Circle3.2.dwg` | 399,377 | `c1ddcb3bc48b2f15` | Beam Template 2 with 3.2mm circular axis tags |
| `Cad\Form 2_Steel.dwg` | 1,592,422 | `7f85a8467327eeba` | Steel Framing Detailing Template 2 |
| `Cad\Form 3.dwg` | 376,561 | `d8b36b1b7ee8cc22` | Standard Column Detailing Template 3 |
| `Cad\Form 3_Steel.dwg` | 1,579,252 | `b0a570a48797f4aa` | Steel Framing Detailing Template 3 |
| `Cad\Form 4.dwg` | 372,047 | `2b5efb1bef5c3d1b` | Standard Slab/Footing Detailing Template 4 |
| `Cad\Form 4_Steel.dwg` | 1,428,355 | `9fd708996d6fc61e` | Steel Truss Detailing Template 4 |
| `Cad\Form 5.dwg` | 374,878 | `6418760f862e68d6` | Shear Wall Detailing Template 5 |
| `Cad\Form 5_Steel.dwg` | 1,423,110 | `f8510bed72824d7b` | Steel Portal Frame Detailing Template 5 |
| `AutoStr\00.FileDungChung\BlockTenBanVe.dwg` | 116,934 | `4c565dde6e8d2f74` | Standard Titleblock Attribute Block |
| `AutoStr\00.FileDungChung\Chi Tiet Coc 250x250.dwg` | 209,264 | `a5ec81f411dc8baf` | 250x250 RC Precast Pile Standard Detail |
| `AutoStr\00.FileDungChung\GhiChuChung 5574 _ A3.dwg` | 1,197,969 | `4f1c2f0e07160f4f` | General Structural Notes (TCVN 5574:2018) A3 Sheet |
| `AutoStr\00.FileDungChung\Khung A3.dwg` | 103,145 | `150b698a6127da6c` | Standard A3 Border & Title Block |

#### B. 132 XLSM Calculation Workbooks
- `Excel\DceSteel\`: **112 workbooks** covering PEB structural steel components (Portal frames, Apex Haunches, Eaves Haunches, Base Plates, Purlins, Trusses, Cranes, Anchor bolts, Wind loads TCVN 2737:2023).
- `Excel\File V.2020\`: **19 workbooks** covering RC concrete components (Beams continuous `TinhDam.xlsm`, Rectangular Columns, Polygonal Columns, Shear walls, Single pad footings, Pile footings, Slab 2-way, Stairs, Retaining walls).
- `Excel\`: **1 workbook** (`DCE_PRO_Setup.xlsm`).
- *Total*: **132 `.xlsm` workbooks**.

#### C. Machine Learning Assets (`MayHoc\`)
- `MayHoc\mauhoc.db`: **8,982,528 bytes**, SHA-256 `74eb37741c999c67...` (SQLite database containing 11 tables: `MauHocDam`, `MauHocCot`, `MauHocSan`, `DuLieuHuanLuyen`, etc.).
- `MayHoc\model.zip`: **26,898 bytes**, SHA-256 `4a71525012fc854b...` (Zipped ML model artifacts: Decision Trees / LightGBM weights for beam rebar arrangement prediction).

#### D. System Configuration Files (`System\`)
- Total items in `C:\Dce Pro\System\`: **178 files** (170 `.txt` config files + 6 `.dat` binary state files + 1 `.exe` binary + 1 `Cache` directory).
- Typical keys: `AdsLi.txt` (License server URL / local path), `AnMatCatDocCot.txt`, `AnMatCatDocVach.txt`, `AnThongKeThepCot.txt`, `BeamShop.txt`, `DceSetup*.txt`.

#### E. Language Localization Dictionaries (`Language\`)
- `Language\Cad\en.txt`: 363,901 bytes (4,710 lines), SHA-256 `0a689796e95fd841...`
- `Language\Cad\vi.txt`: 222,431 bytes (2,853 lines), SHA-256 `77b6949125b9287f...`
- `Language\Cad\zh.txt`: 346,043 bytes (4,755 lines), SHA-256 `56eda0f01b07767a...`
- `Language\Cad\_HUONGDAN.txt`: 1,557 bytes (30 lines).
- `Language\en.txt`: 88,276 bytes (1,258 lines), SHA-256 `393463e7a0ecdc22...`
- `Language\vi.txt`: 85,656 bytes (1,106 lines), SHA-256 `8ad5fff292b276c1...`
- `Language\zh.txt`: 85,204 bytes (1,258 lines), SHA-256 `7b7efd163c7ee887...`
- `Language\_HUONGDAN.txt`: 2,248 bytes (47 lines).

#### F. Visual Assets & Menus
- `Icon\**`: **133 files** (62 `.png` Ribbon icons, 69 `.jpg` dialog previews, 2 `.ico` application icons).
- `.cuix` Menu Customization files: **7 distinct files** (`MenuDCE.cuix`, `MenuDCE_ACAD.cuix`, `MenuDCE_BCAD.cuix`, `MenuDCE_GCAD.cuix`, `MenuDCE_ICAD.cuix`, `MenuDCE_ZWCAD.cuix`, `DBimApps.cuix`) + 2 `.mnr` binary icon resource files.

---

### 1.5 Target Solution Comparison vs `HPDcePro/assets/`

1. **Asset Presence in `HPDcePro/`**:
   - `HPDcePro/assets/` directory **does not exist** currently.
   - 0 out of 17 `.dwg` templates are present in the repository.
   - 0 out of 132 `.xlsm` calculation workbooks are present in the repository.
   - `mauhoc.db` and `model.zip` are absent from `HPDcePro/assets/`.
   - Localization dictionaries and icon files are absent from `HPDcePro/assets/`.
   - **Migration Status**: **0% Migrated** (All static assets must be copied and normalized).

2. **Hardcoded Path Audit**:
   - **LISP Scripts**: Found 15 hardcoded occurrences of `C:\Dce Pro\` in `C:\Dce Pro\DCE Pro.lsp` (e.g. `(setq dce_dll "C:\\DCE Pro\\Dce Pro.dll")`, `(open "C:\\Dce Pro\\DCE Load.log" "a")`), and 4 in `AddinSteelCad\*.lsp`.
   - **System Configs**: Found 34 configuration files in `System\*.txt` containing hardcoded `C:\Dce Pro\` or absolute paths (e.g. `System\fiFaileInfo.txt` contains `C:\Dce Pro\DCE Pro.dll`).
   - **XLSM Calculation Workbooks**: Scanned all 132 `.xlsm` archives; identified **725 hardcoded path references** across formula external links, VBA project headers, and embedded macros pointing to developer paths (`C:\Users\DELL\Desktop\DceSteel\...`, `C:\Dce Pro\...`).
   - **Existing C# Source in `HPDcePro/src/`**: Found **7 hardcoded references** to `C:\Dce Pro`:
     1. `HPDcePro.Core\MachineLearning\MauHocDatabaseReader.cs:47`: `public const string DefaultDatabasePath = @"C:\Dce Pro\MayHoc\mauhoc.db";`
     2. `HPDcePro.Desktop\Program.cs:13`: `public const string DefaultAutoStrSamplePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";`
     3. `HPDcePro.Mcp\Execution\Handlers\AutoStrToolHandler.cs:46`: `string defaultPath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";`
     4. Comments in `MauHocDatabaseReader.cs`, `RebarFeatureExtractor.cs`, `RebarTokenType.cs`, `AutoStrConfig.cs`.

---

## 2. Logic Chain

1. **Catalog Completeness**:
   - The authoritative request specifies that the legacy software surface is cataloged in `C:\Dce Pro\AiAgent\DanhBa\*.json`.
   - Parsing `danhba_lenh.json` yields exactly 311 `<CommandMethod>` items.
   - Parsing `danhba_dce_excel.json` yields exactly 73 Ribbon dispatch items.
   - Parsing `danhba_dce_thep.json` yields exactly 49 `DceSteel` items.
   - Parsing `danhba_mbkc.json` yields exactly 29 `MBKC` items.
   - Direct PE header parsing of `DceAnalyssiss64.dll` reveals exactly 69 exported functions, which divide mathematically into geometry (12), vector math (34), MBKC framing topology (9), beam span calculation (10), and integrity/licensing (4).
   - Direct extraction of `DBIM_RVT_26.exe` reveals `DBimRevit.dll`, which implements exactly 23 `IExternalCommand` classes and 1 `IExternalApplication`.
   - *Inference*: The complete functional universe of legacy DCE Pro comprises 311 CAD commands, 73 Excel Ribbon actions, 49 Steel functions, 29 MBKC functions, 69 native C++ exports, and 23 Revit commands.

2. **Static Asset Ground-Truth**:
   - The file counts in `C:\Dce Pro` match the specification precisely: 17 `.dwg` templates (13 in `Cad/` and 4 in `AutoStr/00.FileDungChung/`), 132 `.xlsm` workbooks (112 in `Excel/DceSteel/`, 19 in `Excel/File V.2020/`, 1 in `Excel/`), `MayHoc/mauhoc.db` (8.98 MB) + `model.zip`, 178 files in `System/` (170 `.txt` + 7 binary/exe files + 1 dir), 8 `Language/` dictionaries, and 133 `Icon/` files + 7 `.cuix` files.
   - *Inference*: `C:\Dce Pro` is a complete, intact legacy deployment.

3. **Standalone Independence Assessment**:
   - `HPDcePro/assets/` currently does not exist. All assets must be migrated into `HPDcePro/assets/`.
   - The presence of 7 hardcoded path strings in `HPDcePro/src/` pointing to `C:\Dce Pro` violates the standalone requirement.
   - The presence of hardcoded paths in `.lsp`, `.xlsm`, and config files proves that a blind file copy is insufficient: paths must be normalized to relative resolution (`AppContext.BaseDirectory` or relative asset loader).
   - *Inference*: A dedicated asset migration script must copy files, normalize `.lsp` and config files, and update C# loader constants to achieve 100% standalone operation.

---

## 3. Caveats

1. **Obfuscation of `DBimRevit.dll`**: The method bodies in `DBimRevit.dll` are encrypted by `QF3HDhLgvyaOAfQS7A4`. The class names and `IExternalCommand` interfaces are fully exposed and enumerated, but method bodies cannot be decompiled directly via `ilspycmd` without memory dumping. In accordance with the project rules, uninspectable method bodies must be recorded as `CHƯA GIẢI MÃ ĐƯỢC` with their metadata tokens.
2. **XLSM Binary VBA Streams (`xl/vbaProject.bin`)**: The 132 `.xlsm` workbooks contain 725 hardcoded path strings in compiled VBA streams. While headless execution in `HPDcePro.Excel` uses `ClosedXml` and pure C# UDFs (`CalculationUdfBridge.cs`), if these workbooks are ever opened in Microsoft Excel, users may encounter broken external link warnings unless paths are cleaned.
3. **ZWCAD / GstarCAD Menus**: The `.cuix` files for ZWCAD and GstarCAD are included in `C:\Dce Pro`, but per R3 exclusions, only AutoCAD 2026, Revit 2026, and Excel are in scope.

---

## 4. Conclusion

1. **Catalog Survey Completed (100% Exact Counts)**:
   - **311** AutoCAD `<CommandMethod>` entries enumerated from `danhba_lenh.json`.
   - **73** Excel Ribbon commands and dispatch actions enumerated from `danhba_dce_excel.json`.
   - **49** `DceSteel` functions enumerated from `danhba_dce_thep.json`.
   - **29** `MBKC` functions enumerated from `danhba_mbkc.json`.
   - **69** Native C++ exports enumerated from `DceAnalyssiss64.dll`.
   - **23** Revit `IExternalCommand` classes and 1 `IExternalApplication` enumerated from `DBIM_RVT_26.exe`.
2. **Static Asset Inventory Completed**:
   - **17** `.dwg` templates (13 in `Cad\`, 4 in `AutoStr\00.FileDungChung\`).
   - **132** `.xlsm` calculation workbooks (112 in `Excel\DceSteel\`, 19 in `Excel\File V.2020\`, 1 in `Excel\Root`).
   - **1** SQLite database `MayHoc\mauhoc.db` (8.98 MB) + 1 model archive `MayHoc\model.zip` (26.9 KB).
   - **178** System files in `System\` (170 `.txt` config files + 7 `.dat`/`.exe` files + 1 `Cache` directory).
   - **8** Language localization dictionaries in `Language\**\*.txt`.
   - **133** Visual assets in `Icon\**` (62 `.png`, 69 `.jpg`, 2 `.ico`) + 7 `.cuix` menus.
3. **Target Gap Identified**:
   - `HPDcePro/assets/` is currently empty / absent (0% migrated).
   - 7 hardcoded `C:\Dce Pro` occurrences in `HPDcePro/src/` must be replaced with relative asset resolution (`AppContext.BaseDirectory` / `HPDcePro/assets/`).
   - A normalized staging plan is ready to populate `docs/rea-dce-pro/TRUTH_MATRIX.md` and copy all static assets into `HPDcePro/assets/`.

---

## 5. Verification Method

To independently verify all findings:

1. **Verify JSON Catalogs**:
   ```bash
   python -c "import json; print('Lenh:', len(json.load(open(r'C:\Dce Pro\AiAgent\DanhBa\danhba_lenh.json', encoding='utf-8'))['muc'])); print('Excel:', len(json.load(open(r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json', encoding='utf-8'))['muc'])); print('Thep:', len(json.load(open(r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_thep.json', encoding='utf-8'))['muc'])); print('MBKC:', len(json.load(open(r'C:\Dce Pro\AiAgent\DanhBa\danhba_mbkc.json', encoding='utf-8'))['muc']))"
   # Output must be: Lenh: 311, Excel: 73, Thep: 49, MBKC: 29
   ```

2. **Verify Native C++ Exports (69 exports)**:
   ```bash
   python "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\extract_pe_exports.py"
   # Output must report: Total exports found: 69
   ```

3. **Verify Revit Commands (23 commands)**:
   ```bash
   python "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\dump_revit_types.py"
   # Output must report: 23 DBimRevit.Command* classes + DBimRevit.AdskApplication
   ```

4. **Verify Static Asset Counts in `C:\Dce Pro`**:
   ```bash
   python "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\inventory_static_assets.py"
   # Output must report: 17 DWG templates, 132 XLSM workbooks, mauhoc.db + model.zip, 170 System *.txt (178 files in System), 8 Language dicts, 133 Icons
   ```

5. **Verify Hardcoded Paths in `HPDcePro/src`**:
   ```bash
   python "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\audit_paths.py"
   # Output reports the 7 exact line numbers in HPDcePro/src
   ```
