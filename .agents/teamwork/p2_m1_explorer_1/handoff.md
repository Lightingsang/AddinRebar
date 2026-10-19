# Handoff Report — Explorer 1: Milestone 1 (VBA Extraction & Sync)

- **Date**: 2026-10-18T08:26:00Z
- **Author**: Explorer 1 (`teamwork_preview_explorer` / `p2_m1_explorer_1`)
- **Recipient**: Parent Orchestrator (`orchestrator_27` / ID `70f8ea72-c348-4512-8527-7930db5e95da`)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_1\`
- **Milestone**: Milestone 1 (VBA Extraction & Sync)
- **Handoff Type**: Hard Handoff (Investigation Complete & Verified)

---

## 1. Observation

### 1.1 Source Workbook & Target File Characteristics
Direct inspection of `C:\kata_pro\Kata.xlsm` and synchronization destinations yielded the following exact measurements:

- **Source File**: `C:\kata_pro\Kata.xlsm`
  - Size: `474,272` bytes (unlocked workbook)
  - SHA-256: `89d1243cfc91a3dd2254942a41bd5ebabcaa43ebd0d8b758b016d066ab010b3d`
  - Container: Standard PKZip OpenXML (`.xlsm`)
  - Subfile `xl/vbaProject.bin`: Size `105,472` bytes (uncompressed), `37,673` bytes (compressed within zip archive)
  - Pre-run baseline vs post-run hash: `89d1243c...` strictly unchanged (0 bytes modified, 100% read-only preserved).

- **Current Synchronization Targets** (holding outdated locked workbook from previous generation):
  - `C:\HPKata\Kata.xlsm`: Size `474,860` bytes | SHA-256: `bcb285b3316c38d5b319524adcf34492299efea408a37bf291f2fbea2ce5a419`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Kata.xlsm`: Size `474,860` bytes | SHA-256: `bcb285b3316c38d5b319524adcf34492299efea408a37bf291f2fbea2ce5a419`

### 1.2 Python Environment & Library Availability
The system environment was inspected via command execution:
```
python --version -> Python 3.14.6
olefile -> C:\Users\STR-HP03\AppData\Roaming\Python\Python314\site-packages\olefile\__init__.py
oletools -> C:\Users\STR-HP03\AppData\Roaming\Python\Python314\site-packages\oletools\__init__.py
olevba -> C:\Users\STR-HP03\AppData\Roaming\Python\Python314\site-packages\oletools\olevba.py
```
Both `olefile` and `oletools.olevba` are pre-installed in the user Python environment, requiring zero pip downloads or external network access.

### 1.3 OLE2 Compound Document & Stream Hierarchy
Reading `xl/vbaProject.bin` into memory (`io.BytesIO`) revealed the following internal OLE2 storage structure:
- Root Storage:
  - Stream `PROJECT` (1,413 bytes): VBA Project metadata declaring `ID="{E91FADC0-5DD4-42AF-8109-712E5DAC0833}"`, 16 `Document=` entries, 5 `Module=` entries, and workspace layout.
  - Stream `PROJECTwm` (497 bytes): Unicode string names.
  - Sub-storage `VBA/`:
    - Stream `dir` (1,061 bytes compressed, 2,948 bytes decompressed): Record header specifies `PROJECTSYSKIND = 0x01` (32-bit Windows), `PROJECTLCID = 0x0409`, `PROJECTCODEPAGE = 0x04E4` (1252 / Windows-1252), project name `"VBAProject"`, references, and 21 `MODULE` records with individual text offsets.
    - Stream `_VBA_PROJECT` (7,474 bytes): VBA version signature.
    - Streams `__SRP_0` through `__SRP_f` (16 compiled P-code cache streams).
    - Exactly 21 module streams (each containing compiled p-code cache followed by compressed source text at `MODULEOFFSET`).

### 1.4 The 21 Extracted VBA Modules Inventory
Every module was successfully decompressed and extracted in-memory using MS-OVBA 2.4.1 RLE decompression:

| # | VBA Module Name | Filename | Type | OLE Stream Path | Text Offset | Decompressed Raw Bytes | Line Count | Procedures / Types Declared |
|---|---|---|---|---|:---:|:---:|:---:|---|
| 1 | `Ve_dam` | `Ve_dam.bas` | Procedural | `VBA/Ve_dam` | 10,244 | 8,396 | 233 | Funcs: `GetCadApplication`, `bieudo`. Subs: `laychieudai`, `lay_chdai`, `chuyen_thep`, `chuyen_ht`, `abc`, `xemten_bd` |
| 2 | `Tinh_dam` | `Tinh_dam.bas` | Procedural | `VBA/Tinh_dam` | 4,946 | 2,252 | 79 | Funcs: `anfa`, `lamda`, `Att`, `qd`, `Qdb`, `dt_thep`, `tinh_a`, `dt1`. Subs: `chuyen_dai` |
| 3 | `to_hop` | `to_hop.bas` | Procedural | `VBA/to_hop` | 12,482 | 8,639 | 292 | Type: `info_thep_cat`. Funcs: `chuyen_text`, `so_sanh`. Subs: `update_collection`, `lay_kho_thep`, `up_kho_thep`, `update_kho_thep`, `cap_nhat_thep_tp`, `update_bang_cat_thep`, `delete_data` |
| 4 | `save_info` | `save_info.bas` | Procedural | `VBA/save_info` | 7,964 | 4,887 | 164 | Funcs: `small_kata`, `tra_bang_kl`. Subs: `save`, `load_data` |
| 5 | `Module1` | `Module1.bas` | Procedural | `VBA/Module1` | 4,280 | 2,455 | 86 | Subs: `TaoTemThepHangLoat`, `ResetSheetInTemThep`, `CopyColumnWidth`, `CopyRowHeight` |
| 6 | `ThisWorkbook` | `ThisWorkbook.cls` | Document | `VBA/ThisWorkbook` | 1,332 | 495 | 15 | Commented-out `Workbook_Open` referencing COM class `kata_pro.Ve_cau_kien` |
| 7 | `Sheet1` | `Sheet1.cls` | Document | `VBA/Sheet1` | 827 | 299 | 8 | Standard VB attributes (mapped to Tab "save_info") |
| 8 | `Sheet2` | `Sheet2.cls` | Document | `VBA/Sheet2` | 827 | 299 | 8 | Standard VB attributes (mapped to Tab "Cot") |
| 9 | `Sheet3` | `Sheet3.cls` | Document | `VBA/Sheet3` | 827 | 299 | 8 | Standard VB attributes (mapped to Tab "Thong_ke") |
| 10 | `Sheet4` | `Sheet4.cls` | Document | `VBA/Sheet4` | 1,016 | 299 | 8 | Standard VB attributes (mapped to Tab "Dam") |
| 11 | `Sheet5` | `Sheet5.cls` | Document | `VBA/Sheet5` | 827 | 299 | 8 | Standard VB attributes (mapped to Tab "To_hop_thep") |
| 12 | `Sheet6` | `Sheet6.cls` | Document | `VBA/Sheet6` | 827 | 299 | 8 | Standard VB attributes (mapped to Tab "Kho_thep") |
| 13 | `Sheet7` | `Sheet7.cls` | Document | `VBA/Sheet7` | 827 | 299 | 8 | Standard VB attributes (mapped to Tab "Vach") |
| 14 | `Sheet8` | `Sheet8.cls` | Document | `VBA/Sheet8` | 827 | 299 | 8 | Standard VB attributes (mapped to Tab "Kata_QS") |
| 15 | `Sheet9` | `Sheet9.cls` | Document | `VBA/Sheet9` | 827 | 299 | 8 | Standard VB attributes (mapped to Tab "Gan_tag_block") |
| 16 | `Sheet10` | `Sheet10.cls` | Document | `VBA/Sheet10` | 839 | 302 | 9 | Standard VB attributes (mapped to Tab "BEAM DESIGN") |
| 17 | `Sheet11` | `Sheet11.cls` | Document | `VBA/Sheet11` | 827 | 300 | 8 | Standard VB attributes (mapped to Tab "In_tem_thep") |
| 18 | `Sheet12` | `Sheet12.cls` | Document | `VBA/Sheet12` | 827 | 300 | 8 | Standard VB attributes (mapped to Tab "Data") |
| 19 | `Sheet13` | `Sheet13.cls` | Document | `VBA/Sheet13` | 827 | 300 | 8 | Standard VB attributes (mapped to Tab "Thong_ke_QS") |
| 20 | `Sheet14` | `Sheet14.cls` | Document | `VBA/Sheet14` | 827 | 300 | 8 | Standard VB attributes (mapped to Tab "ThepDamAllMBKC") |
| 21 | `Sheet15` | `Sheet15.cls` | Document | `VBA/Sheet15` | 827 | 300 | 8 | Standard VB attributes (mapped to Tab "SLAB DESIGN") |

### 1.5 Excel Worksheet Tab vs VBA CodeName Cross-Mapping
Inspection of `xl/workbook.xml`, `xl/_rels/workbook.xml.rels`, and each `xl/worksheets/sheet*.xml` (`main:sheetPr@codeName`) established the exact cross-reference:

- `worksheets/sheet1.xml` (Tab: **Vach**, sheetId: 8) -> `codeName`: **`Sheet7`**
- `worksheets/sheet2.xml` (Tab: **Cot**, sheetId: 1) -> `codeName`: **`Sheet2`**
- `worksheets/sheet3.xml` (Tab: **Dam**, sheetId: 4) -> `codeName`: **`Sheet4`**
- `worksheets/sheet4.xml` (Tab: **ThepDamAllMBKC**, sheetId: 20) -> `codeName`: **`Sheet14`**
- `worksheets/sheet5.xml` (Tab: **save_info**, sheetId: 3) -> `codeName`: **`Sheet1`**
- `worksheets/sheet6.xml` (Tab: **Thong_ke**, sheetId: 5) -> `codeName`: **`Sheet3`**
- `worksheets/sheet7.xml` (Tab: **Thong_ke_QS**, sheetId: 16) -> `codeName`: **`Sheet13`**
- `worksheets/sheet8.xml` (Tab: **To_hop_thep**, sheetId: 7) -> `codeName`: **`Sheet5`**
- `worksheets/sheet9.xml` (Tab: **Kho_thep**, sheetId: 11) -> `codeName`: **`Sheet6`**
- `worksheets/sheet10.xml` (Tab: **Kata_QS**, sheetId: 9) -> `codeName`: **`Sheet8`**
- `worksheets/sheet11.xml` (Tab: **BEAM DESIGN**, sheetId: 12) -> `codeName`: **`Sheet10`**
- `worksheets/sheet12.xml` (Tab: **SLAB DESIGN**, sheetId: 17) -> `codeName`: **`Sheet15`**
- `worksheets/sheet13.xml` (Tab: **In_tem_thep**, sheetId: 15) -> `codeName`: **`Sheet11`**
- `worksheets/sheet14.xml` (Tab: **Data**, sheetId: 14, hidden) -> `codeName`: **`Sheet12`**
- `worksheets/sheet15.xml` (Tab: **Gan_tag_block**, sheetId: 10, hidden) -> `codeName`: **`Sheet9`**

---

## 2. Logic Chain

1. **Premise**: In the prior REA audit (`REA_6_6_0_KATA_PRO_AUDIT.md` Section 2.5), `xl/vbaProject.bin` was classified as unrecoverable because the legacy workbook was locked by password hashes `DPB="..."`, `CMG="..."`, `GC="..."`.
2. **Event**: The user provided an unlocked version of `C:\kata_pro\Kata.xlsm` (`474,272` bytes, SHA-256 `89d1243cfc91a3dd2254942a41bd5ebabcaa43ebd0d8b758b016d066ab010b3d`).
3. **Deduction from Observation 1.1–1.3**:
   - The new `xl/vbaProject.bin` (`105,472` bytes) is an OLE2 structured storage file.
   - The `PROJECT` stream confirms project ID `{E91FADC0-5DD4-42AF-8109-712E5DAC0833}` and 21 declared module names.
   - The `dir` stream is compressed via standard Microsoft MS-OVBA 2.4.1 compression. Decompressing `dir` reveals `PROJECTCODEPAGE = 1252` and individual `MODULEOFFSET` values ranging from 827 to 12,482.
4. **Deduction from Observation 1.4**:
   - Slicing each module stream at its `MODULEOFFSET` and feeding the slice to MS-OVBA 2.4.1 RLE decompressor (`decompress_stream`) yields 100% valid plain-text VBA source code.
   - Zero password locks or P-code obfuscation barriers were encountered in any of the 21 modules.
5. **Deduction from Observation 1.5**:
   - The Excel UI tab names (e.g. `Dam`, `save_info`) do NOT map 1:1 to numerical sheet numbers (`Sheet1`..`Sheet15`). Specifically, the primary beam design sheet `Dam` is actually `Sheet4` in VBA code (`Sheet4.cls`), and `save_info` is `Sheet1` (`Sheet1.cls`). Any code reading or restoring beam data must respect this exact mapping.
6. **Domain Discovery Deduction**:
   - `Ve_dam.bas` proves AutoCAD/ZWCAD/GstarCAD COM integration via `ActiveDocument.Utility.GetPoint` to read geometric points directly into Excel selection.
   - `Tinh_dam.bas` implements pure TCVN 5574 limit-state concrete bending and shear calculations:
     $$\alpha_m = \frac{100 |M|}{R_b b h_0^2}, \quad \zeta = 1 - \sqrt{1 - 2\alpha_m}, \quad A_{st} = \zeta \frac{R_b}{R_s} b h_0, \quad Q_{sw} = h_0 \sqrt{8 R_{bt} b q_{sw}}$$
   - `to_hop.bas` implements 1D cutting stock grouping and offcut inventory management with 11.7m standard bar stock.
   - `save_info.bas` defines a 33-column per span database format for storing multi-span beams.
7. **Actionable Path Forward**:
   - A single-pass Python script can generate `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` with complete documentation, analysis, and verbatim source code.
   - The unlocked `C:\kata_pro\Kata.xlsm` can then be copied to `C:\HPKata\Kata.xlsm` and `HPKata/Distribution/Assets/Kata.xlsm`.
   - `C:\kata_pro\Kata.xlsm` remains 100% untouched.

---

## 3. Caveats

- **No Caveats** regarding the extraction algorithm or VBA completeness: All 21 modules are 100% decompressed and extracted in plain text with zero truncated lines or missing tokens.
- **Encoding Note**: VBA string literals created in Vietnamese systems were encoded using Windows ANSI (CP1252/CP1258). Some accented comment characters (e.g. `Thử CAD nào đang mở trước`) contain byte `0x3F` (`?`) as entered originally in the VBA IDE. The extracted strings must be written to disk in UTF-8 format without alteration of the underlying characters.
- **Single-Node Dotnet Directive**: When Worker 1 completes the sync to `HPKata/Distribution/Assets/Kata.xlsm`, any subsequent build/test check must strictly obey the system directive (single-node `--disable-build-servers -m:1`, zero parallel dotnet).

---

## 4. Conclusion & Action Plan for Worker 1

Worker 1 can execute Milestone 1 deterministically with 100% success by following these concrete steps:

### Step 1: Execute VBA Extraction & Generate Master Documentation
Run a dedicated Python script (e.g. `scripts/extract_kata_vba.py` or inline worker script) that:
1. Opens `C:\kata_pro\Kata.xlsm` in read-only mode using Python's `zipfile.ZipFile`.
2. Reads `xl/vbaProject.bin` into memory (`io.BytesIO`).
3. Uses `oletools.olevba.VBA_Project` to decompress all 21 modules.
4. Generates `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` with the 6 structured sections:
   - **Mục 1**: Tổng quan & Bằng chứng pháp y (SHA-256 `89d1243c...`, kích thước 474,272B, MS-OVBA 2.4.1 RLE).
   - **Mục 2**: Bảng danh mục & thông số 21 module VBA (bảng tra cứu kích thước, dòng mã, stream offset, codename).
   - **Mục 3**: Phân tích sâu 5 module nghiệp vụ (`Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`, `Module1`).
   - **Mục 4**: Phân tích 16 module tài liệu (`ThisWorkbook` và `Sheet1`..`Sheet15`).
   - **Mục 5**: Toàn văn mã nguồn nguyên bản (Verbatim source code của toàn bộ 21 module).
   - **Mục 6**: Khuyến nghị kiến trúc tích hợp sang C# `HPKata` & `HPRebar.Core`.

### Step 2: Synchronize Unlocked `Kata.xlsm` to Target Locations
Copy `C:\kata_pro\Kata.xlsm` to:
1. `C:\HPKata\Kata.xlsm`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Kata.xlsm`

### Step 3: Verify Integrity & Sync Completeness
Compute and verify SHA-256 hashes across all 3 files:
- `C:\kata_pro\Kata.xlsm` -> must remain `89d1243cfc91a3dd2254942a41bd5ebabcaa43ebd0d8b758b016d066ab010b3d`
- `C:\HPKata\Kata.xlsm` -> must equal `89d1243cfc91a3dd2254942a41bd5ebabcaa43ebd0d8b758b016d066ab010b3d`
- `HPKata/Distribution/Assets/Kata.xlsm` -> must equal `89d1243cfc91a3dd2254942a41bd5ebabcaa43ebd0d8b758b016d066ab010b3d`

---

## 5. Verification Method

To independently verify the investigation findings:

1. **Verify Python Environment**:
   ```powershell
   python -c "import olefile, oletools.olevba; print('OK')"
   ```
   *Expected output*: `OK`.

2. **Verify In-Memory Extraction Prototype**:
   ```powershell
   python "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_1\prototype_extract.py"
   ```
   *Expected output*: Exactly 21 modules printed with non-zero line counts and sizes.

3. **Verify Read-Only Integrity of `C:\kata_pro\Kata.xlsm`**:
   ```powershell
   python -c "import hashlib; print(hashlib.sha256(open(r'C:\kata_pro\Kata.xlsm', 'rb').read()).hexdigest())"
   ```
   *Expected output*: `89d1243cfc91a3dd2254942a41bd5ebabcaa43ebd0d8b758b016d066ab010b3d`.

4. **Invalidation Conditions**:
   - If `C:\kata_pro\Kata.xlsm` SHA-256 changes from `89d1243c...`, read-only integrity was violated.
   - If module count is not exactly 21, the OLE2 stream parser failed to parse all records in `VBA/dir`.
