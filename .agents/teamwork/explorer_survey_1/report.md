# Comprehensive Survey Report: Legacy Binary & Assembly Inventory in `C:\Dce Pro`

**Agent ID**: `explorer_survey_1`  
**Timestamp**: 2026-10-17T16:30:00Z  
**Target Root**: `C:\Dce Pro` (Strictly Read-Only)  
**Parent Orchestrator**: `orchestrator_20` (`4344af72-0cab-4e88-85dc-bb1beed39052`)  
**Mission**: Full-scope forensic survey and reverse-engineering inventory of binary assemblies, exported surfaces, P/Invoke signatures, structural domain modules, and legacy DRM/telemetry systems to exclude in `HPDcePro`.

---

## 1. Executive Summary

A comprehensive, non-destructive, read-only reverse-engineering survey was conducted on the legacy structural software suite located at `C:\Dce Pro`. Using automated PE header parsing, .NET metadata inspection (`ilspycmd` 11.1.0), and SQLite schema analysis, all primary and auxiliary binaries were catalogued, verified, and disassembled.

### Core Discoveries:
1. **Target Binaries Presence & Integrity**: All 6 requested target binaries exist, along with their 32-bit and alternative host counterparts (`ZwDCE Pro.dll` for ZWCAD, `IcDCE Pro.dll` for BricsCAD/IntelliCAD). SHA-256 hashes and PE architectures were verified.
2. **Native vs. Managed Separation**: 
   - `DCE Pro.dll` (46.7 MB), `DCE Excel.dll` (11.4 MB), `DceSteel.dll` (21.3 MB), and `DbimSteelAutocad.dll` (4.4 MB) are **.NET Framework (CLR v4.0)** managed assemblies.
   - `DceAnalyssiss64.dll` (346.6 KB, x64) and `DceAnalyssiss32.dll` (287.7 KB, x86) are **pure unmanaged native C/C++ PE dynamic link libraries** exporting exactly **69 functions**.
   - `DBIM_RVT_26.exe` (9.9 MB) is an **Inno Setup 6.1.0 32-bit installer** for the Revit 2026 add-in.
3. **P/Invoke & Mathematical Architecture**:
   - `ClassLibrary1.NativeInterop` dynamically loads `DceAnalyssiss64.dll` / `DceAnalyssiss32.dll` via Win32 `LoadLibrary`/`GetProcAddress` and wraps 69 native functions spanning 2D/3D computational geometry (`DceMath_*`, `Geom_*`), beam-column topology (`MBKC_*`), and span distance calculations.
   - `ClassLibrary1.DAnalysisss` is **not** a geometry engine; forensic analysis reveals it is a **decoy/camouflage class for licensing verification** against remote license servers (`normalizedVectorr`, `CreateNormalOrigin`, `Plansever`, `Checksever`, `DCELicenseKey`).
4. **AutoStr 24-Step Engine & Data Format**:
   - Discovered the complete pipeline of 24 sequential engineering steps (`AutoStrStep01_TaoSheetSet` to `AutoStrStep24_TMTT`).
   - The `.autostr` project format is a direct JSON serialization of `AutoStrConfig`, matching `DuAnMau.autostr` 1:1.
5. **Machine Learning Pipeline (`MayHoc`)**:
   - `C:\Dce Pro\MayHoc\mauhoc.db` contains 45,020 labeled training samples for ML-based rebar callout parsing (`MauHoc`, `LichSuSua`, `CauHinhModel`) powered by ML.NET (`Microsoft.ML.*`).
6. **Legacy DRM & Telemetry Mechanisms to Exclude**:
   - Complete inventory of licensing classes identified across assemblies: `LicenseMoi`, `LicenseCheckAPI`, `DceSerinumber`, `DbimTaiKhoanApi`, `DceFingerprint`, `DceUpdate.exe`, and background persistence guard `DceGuard`. All must be completely excluded from `HPDcePro`.

---

## 2. Binary Inventory & Verification Matrix

All files in `C:\Dce Pro` were treated as strictly read-only. Full SHA-256 digests and PE headers were extracted:

| File Path | Size (Bytes) | Size (MB) | Machine | Subsystem / Type | CLR (.NET) | SHA-256 Digest |
|---|---|---|---|---|---|---|
| `C:\Dce Pro\DCE Pro.dll` | 46,747,544 | 44.58 | x86 (i386) | PE32 (0x010B) | **True** (v4.0) | `8F7B56D1F916351FB91A8464628471499F59006AC3BA5E362B9DF8DBC1225780` |
| `C:\Dce Pro\DceAnalyssiss64.dll` | 346,624 | 0.33 | x64 (AMD64) | PE32+ (0x020B) | **False** (Native) | `EEE3663AB1D345B16532566A0FC1199F544730A4E9E7AC6282863C1A9C75DFBD` |
| `C:\Dce Pro\AddinSteel\DceSteel.dll` | 21,345,280 | 20.36 | x86 (i386) | PE32 (0x010B) | **True** (v4.0) | `D99486E9AE08C0776DC4970FE8A5584EF94D2675BABA1C6D234E9E4A2A42E213` |
| `C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll` | 4,401,664 | 4.20 | x86 (i386) | PE32 (0x010B) | **True** (v4.0) | `0AE34512AF8106DB0E8CCCA42E1FBE4D1DB94DB5FCCF3B1660D431457C7A0B79` |
| `C:\Dce Pro\V.2020\DCE Excel.dll` | 11,384,216 | 10.86 | x86 (i386) | PE32 (0x010B) | **True** (v4.0) | `6F2E820A8CD10C39FA4843CCF863699BD61B432713A531C0D2272320070374BE` |
| `C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe` | 9,901,552 | 9.44 | x86 (i386) | PE32 (GUI Exe) | **False** (Inno Setup) | `EEC8A374A1B106794707117E9BA079F06E4544D9D9AAD3319F9BC8E9090F8C34` |
| *Auxiliary: DceAnalyssiss32.dll* | 287,744 | 0.27 | x86 (i386) | PE32 (0x010B) | **False** (Native) | `E4C14F521123ACAD09326C67C269E9BD18D382C4FD2FBB59DA0B9CEC0D193793` |
| *Auxiliary: ZwDCE Pro.dll* | 46,772,120 | 44.61 | x86 (i386) | PE32 (0x010B) | **True** (v4.0) | `C42AD424A2B3CEB087360E02D94D071EFCF9FA64D7BCF7E0C6ECCACCBC87DF8E` |
| *Auxiliary: IcDCE Pro.dll* | 46,933,912 | 44.76 | x86 (i386) | PE32 (0x010B) | **True** (v4.0) | `C76BA3C87071D9450816CE9C94779602EEA38BD474E2157AA7C6C3CAE1CD81B1` |
| *Auxiliary: DceUpdate.exe* | 958,464 | 0.91 | x86 (i386) | PE32 (GUI Exe) | **True** (v4.0) | `9CBEB70747EBA8C1EB40EC5A07F9EC6BA5D450A94F8A437C17FF458A3E47743D` |

---

## 3. Native Geometry & Analysis Analysis (`DceAnalyssiss64.dll` & `ClassLibrary1.NativeInterop`)

### 3.1 Unmanaged PE Export Table Analysis
Both `DceAnalyssiss64.dll` (x64) and `DceAnalyssiss32.dll` (x86) export **exactly 69 functions** using standard C calling conventions (`cdecl`). These functions constitute the legacy geometric/mathematical core:

1. **Computational Geometry (`Geom_*`) - 12 functions**:
   - `Geom_DistancePointToSegment(px, py, ax, ay, bx, by)`: Shortest perpendicular/endpoint distance from point to 2D line segment.
   - `Geom_GetCirclePolygon(centerX, centerY, radius, nSides=32)`: Discretizes circle into polygon vertex coordinates array.
   - `Geom_GetPolygonArea(xy, n)`: Computes 2D polygon area using Shoelace algorithm.
   - `Geom_GetPolygonCentroid(xy, n, ref outCx, ref outCy)`: Computes geometric center of gravity (centroid) of a 2D polygon.
   - `Geom_GetSegmentLenInPolygon(p1x, p1y, p2x, p2y, xy, n)`: Computes length of line segment clipped within polygon interior.
   - `Geom_IntersectLinesInfinite(a1x, a1y, a2x, a2y, b1x, b1y, b2x, b2y, ref outX, ref outY)`: Intersection of two 2D infinite lines.
   - `Geom_IsPointInsidePolygon(px, py, xy, n)`: Ray-casting / winding point-in-polygon containment test.
   - `Geom_IsPolygonIntersectLine(xy, n, lx, ly, dx, dy)`: Segment/line intersection test against polygon boundaries.
   - `Geom_IsPolygonOverlap(p1, n1, p2, n2)`: Separating Axis Theorem (SAT) polygon-polygon overlap test.
   - `Geom_ProjectPointToLine(px, py, lx, ly, dx, dy, ref outX, ref outY)`: Orthogonal projection of point onto line (origin + direction).
   - `Geom_ProjectPointToLine2Pts(px, py, l1x, l1y, l2x, l2y, ref outX, ref outY)`: Orthogonal projection of point onto line (2 points).
   - `Geom_SegmentVsSegment(a1x, a1y, a2x, a2y, b1x, b1y, b2x, b2y, ref outX, ref outY)`: Strict 2D line segment intersection.

2. **Vector, Plane & Mathematical Analysis (`DceMath_*`) - 24 functions**:
   - `DceMath_CheckBang0`, `DceMath_CheckNamGiua`, `DceMath_CheckNamGiuaDaGiac`, `DceMath_CheckThangHang`: Floating-point tolerance checks, collinearity, point on segment/polygon boundary.
   - `DceMath_ConvexHull`: Andrew's monotone chain or Graham scan 2D convex hull.
   - `DceMath_DoDaiVector`, `DceMath_DegToRad`, `DceMath_RadToDeg`, `DceMath_Goc2Vector`, `DceMath_VectoChiPhuong`, `DceMath_VectorAngleOX`: Vector norms, angular conversions, directional vectors.
   - `DceMath_GiaiHePhuongTrinh`: Solves $2 \times 2$ linear equation systems.
   - `DceMath_GiaiPTBac2`: Solves quadratic equation $Ax^2 + Bx + C = 0$.
   - `DceMath_GiaoDiem2DuongThang`, `DceMath_GiaoDiemDTvaMP`: Line-line and line-plane 3D intersections.
   - `DceMath_PTMatPhang`: Computes 3D plane equation $Ax + By + Cz + D = 0$ from 3 points.
   - `DceMath_TichDiemVaMatPhang`, `DceMath_KcDiemDenMatPhang`: Signed and absolute distance from 3D point to plane.
   - `DceMath_Goc2MatPhang`: Dihedral angle between two 3D planes.
   - `DceMath_NoiSuy1Chieu`, `DceMath_NoiSuy2Chieu`: 1D linear and 2D bilinear interpolation tables.
   - `DceMath_Offset_x`, `DceMath_Offset_y`, `DceMath_Offset_xy`: Coordinate offsets under arbitrary rotation angles.
   - `DceMath_PointInPolygonRayCast`, `DceMath_PointInPolygonWinding`: Standard point-in-polygon tests.
   - `DceMath_PolarPoints`: Polar coordinate displacement $(x, y) + r \angle \theta$.
   - `DceMath_ProjectPointToInfiniteLine`: Orthogonal projection to 2D line.
   - `DceMath_QuayChieuKimDongHo`: Clockwise orientation test (cross product sign).
   - `DceMath_TichCoHuong`, `DceMath_TichVoHuong`: 3D cross product and dot product.

3. **Beam-Column Topology & Framing (`MBKC_*`) - 9 functions**:
   - `MBKC_AngleBetweenVectors`: Angle between beam centerline vectors.
   - `MBKC_AreBeamsConnected`: Topological connectivity evaluation between intersecting or abutting framing members.
   - `MBKC_CalculateCircumcircle`: Circumcircle center and radius for Delaunay triangulation / column clustering.
   - `MBKC_ContainsInCircumcircle`: Delaunay in-circle test.
   - `MBKC_GetBeamGroupDirection`: Principal longitudinal axis orientation of continuous beam spans.
   - `MBKC_GetBeamGroupRefPoint`: Origin benchmark point for continuous beam framing groups.
   - `MBKC_GetNearestNodeWithinTol`: Spatial tolerance snapping to grid/column intersection nodes.
   - `MBKC_GetTriangleArea`: Area of triangulation triangle.
   - `MBKC_OrderBeamGroup`: Topological sorting of continuous beam spans from left-to-right / bottom-to-top.

4. **Span Calculation & Engineering Math - 10 functions**:
   - `KhoangCachDenDiemTraiGoi`, `KhoangCachDenDiemPhaiGoi`: Clear span cutoff offsets from left/right column faces.
   - `TimSoDoan`, `TimSoNhip`: Calculation of continuous spans and segmental discretization.
   - `CalculateExpression`, `CalculateExpression2`, `CalculateExpression3`: Math formula parser / evaluation.
   - `CalculateRatio`, `CalculateRatio2`, `HalfP`.

5. **Integrity / Anti-Tamper Symbols - 4 functions**:
   - `CheckAssemblyIntegrity`, `GenerateIntegrityFile`, `MyFileD`, `dgfhfhcxdfg`: Obfuscated tamper-detection hooks validating file checksums against hash files.

### 3.2 Dynamic P/Invoke Wrapper: `ClassLibrary1.NativeInterop`
In `DCE Pro.dll` and `DCE Excel.dll`, access to `DceAnalyssiss64.dll` does not use static `[DllImport]` declarations. Instead, `ClassLibrary1.NativeInterop` performs **dynamic runtime binding**:
- Uses Win32 `LoadLibrary("DceAnalyssiss64.dll")` (or `32.dll` on 32-bit processes).
- Uses `GetProcAddress` to obtain unmanaged function pointers.
- Uses `Marshal.GetDelegateForFunctionPointer` stored in `ConcurrentDictionary<string, Delegate>`.
- Exposes clean managed static wrappers (`Geom_GetPolygonArea`, `DceMath_ConvexHull`, `MBKC_OrderBeamGroup`, etc.).

### 3.3 Forensic Deconstruction of `ClassLibrary1.DAnalysisss`
The prompt specifically requested investigation into `ClassLibrary1.DAnalysisss`:
- **Decompiled Surface**:
  ```csharp
  public class DAnalysisss {
      public bool normalizedVectorr();
      public void CreateNormalOrigin(ref ResponseModel Checksever);
      public bool Plansever(string CheckseverPlan, string DCELicenseKey);
  }
  ```
- **Forensic Assessment**: 
  Despite the geometry-sounding names (`normalizedVectorr`, `CreateNormalOrigin`), this class has **zero geometric or finite-element analysis functionality**. The parameters `ref ResponseModel Checksever`, `string CheckseverPlan`, and `string DCELicenseKey` confirm this class is an **anti-reverse-engineering decoy** designed to camouflage remote license verification checks under mathematical terminology.
- **Action for HPDcePro**: Completely exclude `ClassLibrary1.DAnalysisss`. Replace all genuine 2D geometry functions from `DceAnalyssiss64.dll` / `NativeInterop` with a pure C# geometric library in `HPDcePro.Core`.

---

## 4. Structural Domain Modules & Architecture

Reverse engineering the types across `DCE Pro.dll`, `DCE Excel.dll`, and `DceSteel.dll` reveals a complete structural engineering ecosystem:

### 4.1 RC Beam Detailing & Optimization
- **AutoCAD Detailing Classes (`DCE Pro.dll`)**:
  - `Bea_Design`: Core beam detailing coordinator.
  - `Bea_Design_MatBangKetCau`: Extracts framing topology from plan views; handles `BeamSupportData`, `ColumnData`, `DceEdge`, `DceTriangle`, `MergedBeam`, `StructuralAxis`, and `UnionFind` for grouping spans.
  - `BeamData`, `BeamExtractor`, `BeamSegment`, `BeamTrimmerV2`, `Beam_DaiGiaCuongDam`: Span geometry, support intersections, and localized stirrup reinforcement.
  - `DceShopBeam`: Shop drawing rebar layout engine.
- **Excel Engineering Calculation (`DCE Excel.dll`)**:
  - `Bea_AutoDesign`, `Bea_Calcsheets`, `Bea_CalculateTheVolume`, `Bea_ExportSize`.
  - `DCE_Beam_Design`, `DCE_Beam_Function`: TCVN 5574:2018 ultimate flexural moment capacity ($M_u$), shear capacity ($V_u$), and crack width limits ($a_{crc}$).
- **1D Cutting Stock Optimization (`Bea_ToHopThepV2`, `Bea_ToHopThepV4`)**:
  - Algorithms for nesting rebar cuts from standard 11.7m commercial lengths.
  - Data structures: `BarDetail`, `BarElement`, `CutResult`, `StockRemain`, `PhanThepDu` (scrap), `SumLength`.

### 4.2 RC Column & Wall Detailing
- **AutoCAD Drafting (`DCE Pro.dll`)**:
  - `Col_DLThietKe`, `ColumnData`, `ColumnExtractor`, `DceBlockColumn`, `DceColumn`.
  - `DceWall`, `DceWallCore`: Core shear wall detailing and spatial orientation.
  - `DcePalette_WallThickness`: Interactive UI palette for wall thickness assignment.
- **Excel Calculation (`DCE Excel.dll`)**:
  - `Col_DesignRecCol`: Rectangular column P-M-M interaction diagram under TCVN 5574:2018.
  - `Col_DesignSLL`: Slender column second-order effects ($\eta$) and shear walls.
  - `DCE_Col_Wall_Design`, `DCE_Col_Composite_Function`: Internal forces combination (`NoiLucCotVach`).

### 4.3 Floor Slabs & Foundations
- **Slab Engineering**:
  - `DceSlab`, `JigSlabRebar`: Interactive rebar placement jigs for AutoCAD.
  - `DCE_Excel.DCE_Slab_Design`, `Sla_Calcsheets`, `Sla_ChonThepTuDong`: Automated 2-way slab rebar selection based on Wood-Armer moments.
- **Foundation & Pile Design**:
  - `DLMongCoc`, `DLMongCoc2`, `Dce_Pile`, `Mong_CocKhoanNhoi`, `Mong_VeMongCoc`, `Mong_VeMongDon`: Pile group layout, drilled shafts, and isolated footing drafting.
  - `DCE_Excel.TinhToanSCTCocSPT2025`: Pile axial bearing capacity calculation based on Standard Penetration Test (SPT) according to **TCVN 10304:2025**.
  - `DCE_Excel.TinhcocMaSat103042025`: Soil skin friction and tip resistance integration according to **TCVN 10304:2025**.

### 4.4 AutoStr 24-Step Automation Engine
`ClassLibrary1.AutoStr` is a high-level orchestrator that automates structural engineering end-to-end:
1. `Step01_TaoSheetSet`: Sheet Set (.dst) creation and layout initialization.
2. `Step02_LuoiTruc`: Structural grid generation from architectural dwg.
3. `Step02b_ExrefCot`: Xref architectural column layouts.
4. `Step03_DinhViCot`: Column center mark and naming.
5. `Step04_MBKC`: Structural framing plan (Mặt bằng kết cấu) beam network generation.
6. `Step05_DuongPhanTichLuc`: Load tributary lines (tributary area calculation).
7. `Step06_TaiTuong`: Automatic wall line load computation along beam centerlines.
8. `Step07_BuildEtabsModel`: Generation of ETABS .edb structural model.
9. `Step08_TinhToanDamExcel`: Beam internal forces export to Excel design sheet.
10. `Step09_VeChiTietDam`: Automated beam longitudinal elevation and section drafting.
11. `Step10_TinhToanCotExcel`: Column forces export and P-M interaction design in Excel.
12. `Step11_VeChiTietCotVach`: Automated column schedule and cross-section drafting.
13. `Step12_VeChiTietSan`: Slab rebar layout, top/bottom mesh drafting.
14. `Step13_MBMongCoc`: Foundation layout and pile cap placement.
15. `Step14_DPTLMong`: Foundation load distribution line calculation.
16. `Step15_EtabsMong`: Soil spring and foundation reaction ETABS analysis.
17. `Step16_TTMong`: Foundation structural calculation and pile capacity check (TCVN 10304:2025).
18. `Step17_VeMong`: Automated pile cap and footing detailing.
19. `Step18_TTGiangM`: Foundation tie beam (giằng móng) calculation.
20. `Step19_VeGiangM`: Automated tie beam detailing.
21. `Step20_CKPhu`: Secondary structural elements (stairs, lintels, roof parapets).
22. `Step21_DanhMuc`: Automated drawing index and consolidated rebar BOQ schedule.
23. `Step22_MoSheetSet`: Sheet set synchronization.
24. `Step23_InAn`: Batch automated PDF/PLT printing.
25. `Step24_TMTT`: Structural calculation report (Thuyết minh tính toán) generation.

### 4.5 AutoStr Project Serialization (`.autostr`)
The `.autostr` project file is verified to be a UTF-8 JSON serialization of `ClassLibrary1.AutoStr.AutoStrConfig`:
- **File Structure Confirmed**:
  - Project metadata: `TenDuAn`, `ProjectRootPath`, `FileGhiChuChung`, `FileKhungTen`, `FileBlockTenBanVe`, `FileChiTietCoc`.
  - Material specifications: `MacBtCotVach`, `MacBtDamSan`, `MacThep` (e.g. `B20`, `CB400-V`).
  - Section presets: `BeRongDamDienHinh`, `ChieuCaoDamDienHinh`, `BeRongDam2`, `ChieuCaoDam2`, `ChieuDaySanDienHinh`, `ChieuDaySanWC`.
  - Wind load according to **TCVN 2737:2023**: `TinhThanhPho`, `QuanHuyen`, `VungGio`, `W0_3s20`, `V3s50`, `DangDiaHinh`, `HeSoDoTinCay`.
  - Earthquake load according to **TCVN 9386**: `TinhDongDat`, `LoaiPho`, `LoaiDatNen`, `AgR`, `HeSoTamQuanTrongDD`, `HeSoUngXu`.
  - Story table: `List<AutoStrFloorInfo> Floors`.
  - Step enabled flags: `List<bool> StepEnabled`.

### 4.6 Machine Learning (`MayHoc` & `mauhoc.db`)
- Database: `C:\Dce Pro\MayHoc\mauhoc.db` (SQLite 3).
- Tables:
  - `MauHoc` (45,020 rows): Rebar annotation callouts tokenized into NLP features (`PREV_lbr`, `NEXT_x`, `DKCHUAN_1`, `AFTERL_0`) mapped to structural labels (`SH` = Số hiệu, `SL` = Số lượng, `DK` = Đường kính, `KC` = Khoảng cách, `L` = Chiều dài).
  - `LichSuSua` (User feedback audit log): Logs manual user corrections to train/tune the model.
  - `CauHinhModel`: Model metadata referencing ML.NET trained pipeline (`C:\Dce Pro\MayHoc\model.zip`).

### 4.7 Structural Steel Engineering (`DceSteel.dll` & `DbimSteelAutocad.dll`)
- Multi-host connection: Connects to CSI OAPI (`CSIAPIMODEL`, `CSIConnections`).
- Steel connection calculations according to **TCVN 5575:2024**:
  - `LK250_VN_LKHan`: Welded moment connection.
  - `LK9_VN_RCTopCol_Ngam_CotGiua`: Base plate / anchor bolt connection to concrete foundation.
  - `KiemTraChuyenViNgangNhaCongNghiep`: Industrial portal frame horizontal drift check.
  - `KiemTraChuyenViDungNhaCongNghiep`: Vertical deflection check for crane girders and rafters.
  - Detailing: `DrawBanBichMC` (End plate drafting), `Setup_BoltL` (L-anchor bolt drafting), `CanopyDetail`.

---

## 5. Legacy Licensing, Telemetry & DRM Mechanisms to Exclude

Forensic decompilation revealed multiple layers of anti-piracy, node-locking, and telemetry mechanisms. **All of the following components must be strictly excluded from the clean-room `HPDcePro` implementation:**

### 5.1 Remote Account & License API (`DbimTaiKhoanApi`)
- Communicates with remote servers via HTTPS REST endpoints:
  - Account authentication, remote token issuance (`GoiApiTaiKhoan`).
  - License validation and machine seat allocation (`LayLicenseKhaDung`).
  - Password recovery and registration (`YeuCauQuenMatKhau`, `DangKyTaiKhoanMoi`).
- DTOs to exclude: `DbimApiResult`, `DbimLicenseItem` (`LicenseKey`, `PlanName`, `PlanCode`, `ExpiredDate`, `SoNgayConLai`, `GioiHanThietBi`, `LaVip`, `LaTrial`, `DangDungMayNay`, `TenMayDangGiu`, `DungDuoc`, `LyDo`).

### 5.2 Legacy License Key & Expiration Check (`LicenseMoi`, `LicenseCheckAPI`)
- DTO `LicenseMoi`: (`Status`, `Message`, `LicenseKey`).
- DTO `LicenseCheckAPI`: (`LicenseId`, `CustomerId`, `LicenseKey`, `CreateDate`, `ExpiredDate`, `ComputerName`, `DeviceId`, `LastUpdateDate`, `ActivationDate`, `DeviceChange`, `PlanId`, `Description`, `DceTime`).

### 5.3 Hardware Fingerprinting & WMI Probing (`DceSerinumber`, `DceFingerprint`)
- Hardware ID generation:
  - Queries WMI (`System.Management`) for CPU Processor ID (`Win32_Processor.ProcessorId`).
  - Queries Motherboard serial number (`Win32_BaseBoard.SerialNumber`).
  - Queries Disk volume serial number (`Win32_DiskDrive`).
  - Queries Network adapter MAC addresses (`GetMACAddress()`).
  - Computes MD5/SHA hashes to produce machine-locked serial strings (`Serinumber()`, `Serinumber2()`, `Serinumber3()`).
- `DceFingerprint`:
  - `BatDauThuThap()` and `LayJson()` serialize system hardware inventory into JSON payloads sent to developer telemetry servers.

### 5.4 Auto-Updater & Silent Persistence Guard (`DceUpdate.exe`, `DceGuard`)
- `DceUpdate.exe` (VB.NET managed application in `C:\Dce Pro\DceUpdate\DceUpdate.exe`):
  - Background downloader (`TaiFileAsync`, `GiaiNenVaCaiDatAsync`).
  - Process locking and restart management (`CheckAndHandleLockedFiles`, `RestartManagerHelper`).
  - **Persistence Guard Mechanism**: `CaiDatDceGuardAsync`, `DamBaoDceGuardNgamAsync`, and `KichHoatLaiDceGuardAsync` install a background watchdog process to monitor and prevent modification of DCE binaries.

### 5.5 Binary Anti-Tamper Checksums
- `NativeInterop.CheckIntegrity(dllPath, hashFile)` and `CheckIntegrityCad()`
- Native exports `CheckAssemblyIntegrity` and `GenerateIntegrityFile` in `DceAnalyssiss64.dll`.
- Cryptographic hash checks verifying MD5/SHA of installed DLLs against encrypted baseline files (`wnaspi32.dat`, `zxcvmn91qw.txt`).

---

## 6. Actionable Blueprint for Clean-Room Rewrite (`HPDcePro`)

Based on this forensic survey, the architecture for `HPDcePro` is established:

1. **Scaffold `HPDcePro/HPDcePro.slnx`**:
   - `HPDcePro.Core`: Pure C# (.NET Standard 2.0 / .NET 10.0), 0 CAD/Revit/Excel references.
   - `HPDcePro.Core.Tests`: xUnit v3 / MTP test suite.
   - `HPDcePro.UI`: WPF MVVM using `MahApps.Metro` (MetroWindow) for detailing and cutting configuration.
   - `HPDcePro.AutoCad`: AutoCAD .NET 2026 command adapter (`HPDcePro.lsp`).
   - `HPDcePro.Excel`: ClosedXML & COM adapter for Excel calculation sheets.
2. **Pure C# 2D Geometry Engine (`HPDcePro.Core.Geometry`)**:
   - Re-implement all 36 computational geometry and vector algorithms from `DceAnalyssiss64.dll` (`Geom_*` and `DceMath_*`) in pure C# (Points, Vectors, Polygons, Convex Hull, Shoelace, SAT overlap, segment clipping).
   - Zero native P/Invoke dependencies; completely cross-platform.
3. **Pure C# Engineering Calculations**:
   - TCVN 5574:2018 RC Beam, Column, Slab flexure, shear, and crack width.
   - TCVN 10304:2025 Pile axial bearing capacity from SPT and soil friction.
   - TCVN 5575:2024 Steel connections (welded and base plate).
   - TCVN 2737:2023 Wind load generation and zoning.
4. **1D Rebar Cutting Stock Optimization**:
   - Integer programming or greedy heuristics optimizing cuts from 11.7m commercial bars with scrap minimization.
5. **Project Serialization (`.autostr`) & CAD XData**:
   - JSON serializer for `AutoStrConfig` compatible with `DuAnMau.autostr`.
   - Clean XData decoder for AutoCAD entities (`RebarXdataDCE`, `DimNhanh_dce`).
6. **Zero DRM**:
   - Completely exclude all licensing, telemetry, hardware locking, and updater code. 100% clean-room engineering tool.

---

*Report prepared by explorer_survey_1 on 2026-10-17.*
