# Empirical Challenge Report: Milestone M1 — Reverse-Engineering Specifications in `docs/rea-dce-pro/`

**Agent**: `challenger_m1_2` (Empirical Challenger)  
**Date**: 2026-10-17T16:50:00Z  
**Verdict**: **`REQUEST_CHANGES`**  

---

## 1. Executive Summary

As the empirical challenger for Milestone M1, I subjected the reverse-engineering algorithmic specifications in `docs/rea-dce-pro/` to rigorous, hands-on verification against the authoritative binaries and databases in `C:\Dce Pro`. Specifically, I executed custom PE export parsers, SQLite inspection scripts, and .NET metadata decompilers on:
1. `C:\Dce Pro\DceAnalyssiss64.dll` (PE32+ 64-bit native DLL)
2. `C:\Dce Pro\MayHoc\mauhoc.db` (SQLite 3 database)
3. `C:\Dce Pro\DCE Pro.dll` (.NET Framework 4.0 assembly)

### Verdict: `REQUEST_CHANGES`
While the SQLite machine-learning schema in `02-autostr-and-mayhoc.md` and the 13 CAD XData schemas in `01-domain-architecture.md` are empirically verified with outstanding precision, **`01-domain-architecture.md` exhibits a critical omission in its mapping of native C++ exports**:
- Section 8.1.2 header claims `DceMath_* — 24 Functions`, but `DceAnalyssiss64.dll` actually exports **34** `DceMath_*` functions (an undercount of 10).
- **Three (3) exported functions are completely missing** from the document: `DceMath_KhoangCach2Diem`, `DceMath_LocalToGlobal`, and `DceMath_MidPoint`.
- Multiple distinct functions are collapsed into shorthand rows (`CalculateExpression / 2 / 3`, `CalculateRatio / 2`, `DceMath_Offset_x / _y / _xy`), obscuring their unique C# method signatures.

Before Phase 2 (`HPDcePro.Core.Geometry`) begins, these 3 missing functions and count headers must be corrected so that the clean-room implementation implements the true full set of 69 exported functions.

---

## 2. Empirical Test 1: Export Table of `DceAnalyssiss64.dll` vs `01-domain-architecture.md`

### 2.1 Extraction Methodology & Raw PE Evidence
Using a PE export table parser inspecting `IMAGE_EXPORT_DIRECTORY` at file offset `0x00049AC0` (RVA `0x000576C0`), `C:\Dce Pro\DceAnalyssiss64.dll` was verified to export **exactly 69 functions**:

```
PE32+ (AMD64 64-bit) Export Table Summary:
Total Exported Functions: 69
Total Exported Names:     69
Base Ordinal:             1
```

### 2.2 Category Breakdown: Document Claims vs Empirical Reality

| Category | Document Claim (`01-domain-architecture.md`) | Actual DLL Exports | Status | Discrepancy Description |
|---|:---:|:---:|:---:|---|
| **`Geom_*`** (§8.1.1) | 12 Functions | 12 Functions | **PASS** | Exactly 12 functions, all 12 mapped 1-to-1. |
| **`DceMath_*`** (§8.1.2) | 24 Functions (claimed in header) | **34 Functions** | **FAIL** | Header states 24. Table has 28 rows. Actual DLL has 34. **3 functions omitted completely**, 2 functions collapsed. |
| **`MBKC_*`** (§8.1.3) | 9 Functions | 9 Functions | **PASS** | Exactly 9 functions, all 9 mapped 1-to-1. |
| **`TimSo*` / `KhoangCach*` / Math** (§8.1.4) | 10 Functions | 10 Functions | **PARTIAL** | 10 functions exist in DLL, but collapsed into 7 table rows (`CalculateExpression / 2 / 3` bundles 3, `CalculateRatio / 2` bundles 2). |
| **Anti-Tamper & Checksum** (§8.1.5) | 4 Functions | 4 Functions | **PASS** | `CheckAssemblyIntegrity`, `GenerateIntegrityFile`, `MyFileD`, `dgfhfhcxdfg` explicitly excluded. |
| **TOTAL** | **69 Functions** (claimed) / 59–66 mapped | **69 Functions** | **DEFECT** | **3 functions completely omitted from documentation**. |

### 2.3 The 3 Omitted Functions
The following 3 exported symbols exist in `DceAnalyssiss64.dll` (and are referenced by `DceAnalyssiss32.dll`, `DceSteel.dll`, and `DbimSteelAutocad.dll`), but are **entirely missing** from `01-domain-architecture.md`:

1. **`DceMath_KhoangCach2Diem`** (RVA: `0x80E0`):
   - Machine code: Reads two 3D points $(x_1, y_1, z_1)$ and $(x_2, y_2, z_2)$, computes Euclidean distance $\sqrt{(x_2-x_1)^2 + (y_2-y_1)^2 + (z_2-z_1)^2}$.
   - Required C# Replacement: `Point3D.DistanceTo(Point3D other)` or `Math3D.Distance(p1, p2)`.
2. **`DceMath_LocalToGlobal`** (RVA: `0x9690`):
   - Machine code: Performs 3D affine matrix coordinate transformation from element local axis frame to global AutoCAD WCS.
   - Required C# Replacement: `CoordinateTransform.LocalToGlobal(Point3D localPt, Matrix4x4 transform)` or `Point3D.TransformBy(Matrix3D m)`.
3. **`DceMath_MidPoint`** (RVA: `0x83A0`):
   - Machine code: Computes 3D midpoint between two coordinates $((x_1+x_2)/2, (y_1+y_2)/2, (z_1+z_2)/2)$.
   - Required C# Replacement: `Point3D.MidPoint(Point3D p1, Point3D p2)`.

### 2.4 Complete List of 34 `DceMath_*` Exports in `DceAnalyssiss64.dll`
1. `DceMath_CheckBang0`
2. `DceMath_CheckNamGiua`
3. `DceMath_CheckNamGiuaDaGiac`
4. `DceMath_CheckThangHang`
5. `DceMath_ConvexHull`
6. `DceMath_DegToRad`
7. `DceMath_DoDaiVector`
8. `DceMath_GiaiHePhuongTrinh`
9. `DceMath_GiaiPTBac2`
10. `DceMath_GiaoDiem2DuongThang`
11. `DceMath_GiaoDiemDTvaMP`
12. `DceMath_Goc2MatPhang`
13. `DceMath_Goc2Vector`
14. `DceMath_KcDiemDenMatPhang`
15. **`DceMath_KhoangCach2Diem`** *(MISSING FROM DOC)*
16. **`DceMath_LocalToGlobal`** *(MISSING FROM DOC)*
17. **`DceMath_MidPoint`** *(MISSING FROM DOC)*
18. `DceMath_NoiSuy1Chieu`
19. `DceMath_NoiSuy2Chieu`
20. `DceMath_Offset_x`
21. `DceMath_Offset_xy` *(collapsed into shorthand)*
22. `DceMath_Offset_y` *(collapsed into shorthand)*
23. `DceMath_PTMatPhang`
24. `DceMath_PointInPolygonRayCast`
25. `DceMath_PointInPolygonWinding`
26. `DceMath_PolarPoints`
27. `DceMath_ProjectPointToInfiniteLine`
28. `DceMath_QuayChieuKimDongHo`
29. `DceMath_RadToDeg`
30. `DceMath_TichCoHuong`
31. `DceMath_TichDiemVaMatPhang`
32. `DceMath_TichVoHuong`
33. `DceMath_VectoChiPhuong`
34. `DceMath_VectorAngleOX`

---

## 3. Empirical Test 2: SQLite Schema & Data in `C:\Dce Pro\MayHoc\mauhoc.db`

### 3.1 Verification Commands & Output
Direct SQLite connection and queries were executed against `C:\Dce Pro\MayHoc\mauhoc.db`:
- File Size: `9,416,704 bytes`
- SHA-256 Digest: `74eb37741c999c677df271a6c8007d2d09b704fb16c7aff884f4f11ab624e1a8` (100% matched)

### 3.2 Table Schema Verification

```sql
-- 1. Table MauHoc (VERIFIED: Schema exactly matches 02-autostr-and-mayhoc.md §3.2)
CREATE TABLE MauHoc(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TextGoc TEXT,
    ChiSoSo INTEGER,
    GiaTri INTEGER,
    Feature TEXT,
    Nhan TEXT,
    Nguon TEXT,
    ThoiGian TEXT,
    Hash TEXT
);

-- 2. Table LichSuSua (VERIFIED: Schema exactly matches 02-autostr-and-mayhoc.md §3.2)
CREATE TABLE LichSuSua(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TextGoc TEXT,
    Truoc TEXT,
    Sau TEXT,
    ThoiGian TEXT
);

-- 3. Table CauHinhModel (VERIFIED: Schema exactly matches 02-autostr-and-mayhoc.md §3.2)
CREATE TABLE CauHinhModel(
    Ten TEXT,
    DuongDan TEXT,
    SoMau INTEGER,
    ThoiGian TEXT
);
```

### 3.3 Quantitative Row Counts & Label Distribution Verification

| Metric | Document Claim (`02-autostr-and-mayhoc.md`) | Empirical SQLite Query Result | Status |
|---|---|---|:---:|
| **Total `MauHoc` Rows** | 45,020 | `45,020` | **MATCH (100%)** |
| `MauHoc` `Nguon = 'seed'` | 45,006 | `45,006` | **MATCH (100%)** |
| `MauHoc` `Nguon = 'user'` | 14 | `14` | **MATCH (100%)** |
| Label `KC1` | 9,810 | `9,810` | **MATCH (100%)** |
| Label `L` | 9,325 | `9,325` | **MATCH (100%)** |
| Label `SL` | 9,243 | `9,243` | **MATCH (100%)** |
| Label `BO` | 6,195 | `6,195` | **MATCH (100%)** |
| Label `SH` | 4,339 | `4,339` | **MATCH (100%)** |
| Label `KC2` | 3,970 | `3,970` | **MATCH (100%)** |
| Label `KC3` | 1,268 | `1,268` | **MATCH (100%)** |
| Label `DK` | 530 | `530` | **MATCH (100%)** |
| Label `KC4` | 340 | `340` | **MATCH (100%)** |
| **Total `LichSuSua` Rows**| 3 active entries | `3` | **MATCH (100%)** |
| **Total `CauHinhModel` Rows**| 10 checkpoints | `10` | **MATCH (100%)** |

### 3.4 Feature Token Flags Verification
Querying sample strings in `MauHoc.Feature` confirmed the existence and exact format of contextual NLP flags documented in §3.4:
- `AFTERD_0`, `AFTERD_1`, `AFTERL_0`, `AFTERL_1`
- `BEFORED_0`, `BEFORED_1`
- `DKCHUAN_0`, `DKCHUAN_1`
- `INRING_0`, `INRING_1`
- `INSEQ_0`, `INSEQ_1`
- `NDIGIT_1` .. `NDIGIT_5`
- `NEXT_a`, `NEXT_b`, `NEXT_d`, `NEXT_x`, `NEXT_comma`, etc.

**Conclusion for Target 2**: Specification in `02-autostr-and-mayhoc.md` is **100% verified** against ground truth data.

---

## 4. Empirical Test 3: The 13 CAD XData Schemas & RegApps

### 4.1 Verification in `C:\Dce Pro\DCE Pro.dll`
Using direct metadata table and `#Strings` analysis of `DCE Pro.dll`, all 13 application names registered in AutoCAD `RegAppTable` were identified:

1. `RebarXdataDCE`: Verified. Declared as managed class `ClassLibrary1.RebarXdataDCE`.
2. `DceRebarXDataTKT`: Verified. Declared as managed class `ClassLibrary1.DceRebarXDataTKT`.
3. `Links_XDataObject`: Verified. Declared as managed class `ClassLibrary1.Links_XDataObject`.
4. `Dce_ThepKep`: Verified. Referenced in `ThepKep_Core`, `Slab_ThepKep`, and `DatThepKep`.
5. `XDATA_APP_SODOCAT`: Verified. Used for cutting stock pattern tagging.
6. `XDATA_APP_KHOTHEPDU`: Verified. Used for remnant rebar inventory.
7. `XDATA_MAT_BANG_MONG`: Verified. Foundation outline XData. Note: `NHAN_XDATA_MONG` also exists as an auxiliary alias.
8. `XDATA_MONG_COC`: Verified. Pile cap detailing XData.
9. `SYS_XData_ThangMoi`: Verified. Staircase flight parameters.
10. `SYS_XData_App`: Verified. Global versioning and project standards.
11. `NHAN_XDATA_COC`: Verified. Individual pile coordination.
12. `NHAN_XDATA_DAM_CONG`: Verified. Curved beam geometry.
13. `NHAN_XDATA_MB`: Verified. Structural floor boundary polygons.

### 4.2 Structural Schema Verification: `RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`

#### A. `RebarXdataDCE` (12-element Typed Array)
Stored as ordered sequence of DXF `TypedValue` records:
- `o(0)..o(5)` (DxfCode 1000, String): `TenCauKien`, `SoHieu`, `MaThep`, `DuongKinh`, `SoLuong`, `LoaiThep`
- `o(6)..o(10)` (DxfCode 1040, Double): `DoanA`, `DoanB`, `DoanC`, `DoanD`, `ChieuDaiL`
- `o(11)` (DxfCode 1070, Short): `HinhDang`
*Empirical confirmation*: Matches decoding routines in `ClassLibrary1.RebarXdataDCE.LayOXData`.

#### B. `DceRebarXDataTKT` (Canonical Key-Value String)
Format: `SL={SL};DK={DK};KC1={KC1};KC2={KC2};KC3={KC3};KC4={KC4};SH={SH};L={L}`
*Empirical confirmation*: Row 1 of `LichSuSua` in `mauhoc.db` contains:
`SL=3;DK=20;KC1=0;KC2=0;KC3=0;KC4=0;SH=1b;L=11700`
This proves exact key-for-key, semicolon-delimited string format equivalence.

#### C. `Links_XDataObject` (Associative Entity Linkage)
Fields identified in `ClassLibrary1.Links_XDataObject`:
- `LuaChonField` (DxfCode 1000): String formula expression for AutoCAD fields.
- `TrangThaiKhoa` (DxfCode 1070): Short lock flag.
- Handles (DxfCode 1005): Pointers to Polyline (`HandleThep`), Tag (`HandleTag`), Dim (`HandleDim`), and Leader (`HandleLeader`).

**Conclusion for Target 3**: Schemas for all 13 CAD XData RegApps are **accurately specified and fully verified**.

---

## 5. Required Actions for `docs/rea-dce-pro/01-domain-architecture.md`

To achieve an `APPROVE` verdict, the author/worker must make the following updates:

1. **Fix Section 8.1.2 Header**: Change `(DceMath_* — 24 Functions)` to `(DceMath_* — 34 Functions)`.
2. **Add Missing Function 1**:
   `| DceMath_KhoangCach2Diem | Point3D.DistanceTo(Point3D other) | 3D Euclidean distance between coordinates: $\sqrt{(x_2-x_1)^2 + (y_2-y_1)^2 + (z_2-z_1)^2}$. |`
3. **Add Missing Function 2**:
   `| DceMath_LocalToGlobal | CoordinateTransform.LocalToGlobal(Point3D localPt, Matrix4x4 transform) | 3D local-to-global affine coordinate transformation. |`
4. **Add Missing Function 3**:
   `| DceMath_MidPoint | Point3D.MidPoint(Point3D p1, Point3D p2) | 3D midpoint between coordinates: $((x_1+x_2)/2, (y_1+y_2)/2, (z_1+z_2)/2)$. |`
5. **Expand Collapsed Rows**: Provide explicit rows for `CalculateExpression2`, `CalculateExpression3`, `CalculateRatio2`, `DceMath_Offset_y`, and `DceMath_Offset_xy` so that all 69 exports have an explicit 1:1 row in the catalog.
6. **Note Foundation RegApp Alias**: Mention that `NHAN_XDATA_MONG` is used alongside `XDATA_MAT_BANG_MONG` in `DCE Pro.dll`.

---

## 6. Verification Script Artifact
The verification script used to extract the PE export table and compare against `01-domain-architecture.md` is available at:
`python "$env:TEMP\check_dll_exports.py"`
and SQLite inspector at:
`python "$env:TEMP\check_db.py"`
