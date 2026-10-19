# Handoff Report — Explorer 3: Milestone 1 (VBA Extraction & Sync)

## 1. Observation

### 1.1 Unlocked Workbook Status & Integrity Check
- **Target File**: `C:\kata_pro\Kata.xlsm`
  - Byte length: `474,272` bytes
  - SHA-256: `89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D`
  - Internal VBA Storage: `xl/vbaProject.bin` (size: `105,472` bytes)
  - Zip entries: 138 entries
- **Target Sync Destinations**:
  1. `C:\HPKata\Kata.xlsm` (Currently outdated locked version: `474,860` bytes, SHA-256 `BCB285B3316C38D5B319524ADCF34492299EFEA408A37BF291F2FBEA2CE5A419`)
  2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Kata.xlsm` (Currently outdated locked version: `474,860` bytes, SHA-256 `BCB285B3316C38D5B319524ADCF34492299EFEA408A37BF291F2FBEA2CE5A419`)
- **Extraction Engine**: Verified Python package `oletools.olevba.VBA_Parser` successfully decompresses all 21 modules via `[MS-OVBA]` Section 2.4.1 without any errors or password blocks.

---

### 1.2 Inventory of the 21 VBA Modules in `Kata.xlsm`

| # | VBA Stream Path | Module Name | Type | Lines | Code Lines | Byte Size | Key Exports / Responsibilities |
|:---:|---|---|:---:|:---:|:---:|:---:|---|
| 1 | `VBA/Ve_dam` | `Ve_dam.bas` | Procedural | 233 | 232 | 8,396 | CAD COM Interop (`AutoCAD`, `GstarCAD`, `ZwCAD`), interactive point picking (`GetPoint`), length transfer to row 11, moment diagram & stirrup chart series (`Chart 38`) |
| 2 | `VBA/Tinh_dam` | `Tinh_dam.bas` | Procedural | 79 | 78 | 2,252 | TCVN 5574 concrete beam calculations: $\alpha_m$, $\zeta$ (`lamda`), $A_s$ (`Att`), $q_{sw}$ (`qd`), $Q_b$ (`Qdb`), bar area `dt_thep`, CG distance `tinh_a` |
| 3 | `VBA/to_hop` | `to_hop.bas` | Procedural | 292 | 291 | 8,639 | Type `info_thep_cat`, 1D cutting stock heuristic (`update_bang_cat_thep`), warehouse scrap tracking (`kho_thep`, `lay_kho_thep`, `up_kho_thep`) |
| 4 | `VBA/save_info` | `save_info.bas` | Procedural | 164 | 163 | 4,887 | DB persistence on sheet `save_info`: `save()` (header params + 33-cell stride per span), `load_data()` by beam name, `tra_bang_kl` (steel weight lookup table) |
| 5 | `VBA/Module1` | `Module1.bas` | Procedural | 86 | 85 | 2,455 | Batch rebar tag generator `TaoTemThepHangLoat()` (from `Thong_ke` to `In_tem_thep` via 12-row template), `ResetSheetInTemThep` |
| 6 | `VBA/ThisWorkbook`| `ThisWorkbook.cls`| Document | 15 | 7 | 495 | Workbook Open event (commented hook to COM component `kata_pro.Ve_cau_kien.show_thong_tin`) |
| 7 | `VBA/Sheet1` | `Sheet1.cls` | Document | 8 | 0 | 299 | Worksheet class for `save_info` (`sheetId: 3`, target `xl/worksheets/sheet5.xml`) |
| 8 | `VBA/Sheet2` | `Sheet2.cls` | Document | 8 | 0 | 299 | Worksheet class for `Cot` (`sheetId: 1`, target `xl/worksheets/sheet2.xml`) |
| 9 | `VBA/Sheet3` | `Sheet3.cls` | Document | 8 | 0 | 299 | Worksheet class for `Thong_ke` (`sheetId: 5`, target `xl/worksheets/sheet6.xml`) |
| 10 | `VBA/Sheet4` | `Sheet4.cls` | Document | 8 | 0 | 299 | Worksheet class for `Dam` (`sheetId: 4`, target `xl/worksheets/sheet3.xml`) |
| 11 | `VBA/Sheet5` | `Sheet5.cls` | Document | 8 | 0 | 299 | Worksheet class for `To_hop_thep` (`sheetId: 7`, target `xl/worksheets/sheet8.xml`) |
| 12 | `VBA/Sheet6` | `Sheet6.cls` | Document | 8 | 0 | 299 | Worksheet class for `Kho_thep` (`sheetId: 11`, target `xl/worksheets/sheet9.xml`) |
| 13 | `VBA/Sheet7` | `Sheet7.cls` | Document | 8 | 0 | 299 | Worksheet class for `Vach` (`sheetId: 8`, target `xl/worksheets/sheet1.xml`) |
| 14 | `VBA/Sheet8` | `Sheet8.cls` | Document | 8 | 0 | 299 | Worksheet class for `Kata_QS` (`sheetId: 9`, target `xl/worksheets/sheet10.xml`) |
| 15 | `VBA/Sheet9` | `Sheet9.cls` | Document | 8 | 0 | 299 | Worksheet class for `Gan_tag_block` (`sheetId: 10`, target `xl/worksheets/sheet15.xml`) |
| 16 | `VBA/Sheet10` | `Sheet10.cls` | Document | 9 | 1 | 302 | Worksheet class for `BEAM DESIGN` (`sheetId: 12`, target `xl/worksheets/sheet11.xml`) |
| 17 | `VBA/Sheet11` | `Sheet11.cls` | Document | 8 | 0 | 300 | Worksheet class for `In_tem_thep` (`sheetId: 15`, target `xl/worksheets/sheet13.xml`) |
| 18 | `VBA/Sheet12` | `Sheet12.cls` | Document | 8 | 0 | 300 | Worksheet class for `Data` (`sheetId: 14`, target `xl/worksheets/sheet14.xml`) |
| 19 | `VBA/Sheet13` | `Sheet13.cls` | Document | 8 | 0 | 300 | Worksheet class for `Thong_ke_QS` (`sheetId: 16`, target `xl/worksheets/sheet7.xml`) |
| 20 | `VBA/Sheet14` | `Sheet14.cls` | Document | 8 | 0 | 300 | Worksheet class for `ThepDamAllMBKC` (`sheetId: 20`, target `xl/worksheets/sheet4.xml`) |
| 21 | `VBA/Sheet15` | `Sheet15.cls` | Document | 8 | 0 | 300 | Worksheet class for `SLAB DESIGN` (`sheetId: 17`, target `xl/worksheets/sheet12.xml`) |

---

### 1.3 Worksheet ↔ Document Module Mapping Table

From `xl/workbook.xml`, `xl/_rels/workbook.xml.rels`, and `xl/worksheets/sheet*.xml`:

| Sheet Tab Name | `sheetId` | `rId` | Worksheet Target XML | VBA CodeName | Module Stream |
|---|:---:|:---:|---|---|---|
| `Vach` | 8 | `rId1` | `xl/worksheets/sheet1.xml` | `Sheet7` | `VBA/Sheet7` |
| `Cot` | 1 | `rId2` | `xl/worksheets/sheet2.xml` | `Sheet2` | `VBA/Sheet2` |
| `Dam` | 4 | `rId3` | `xl/worksheets/sheet3.xml` | `Sheet4` | `VBA/Sheet4` |
| `ThepDamAllMBKC` | 20 | `rId4` | `xl/worksheets/sheet4.xml` | `Sheet14` | `VBA/Sheet14` |
| `save_info` | 3 | `rId5` | `xl/worksheets/sheet5.xml` | `Sheet1` | `VBA/Sheet1` |
| `Thong_ke` | 5 | `rId6` | `xl/worksheets/sheet6.xml` | `Sheet3` | `VBA/Sheet3` |
| `Thong_ke_QS` | 16 | `rId7` | `xl/worksheets/sheet7.xml` | `Sheet13` | `VBA/Sheet13` |
| `To_hop_thep` | 7 | `rId8` | `xl/worksheets/sheet8.xml` | `Sheet5` | `VBA/Sheet5` |
| `Kho_thep` | 11 | `rId9` | `xl/worksheets/sheet9.xml` | `Sheet6` | `VBA/Sheet6` |
| `Kata_QS` | 9 | `rId10`| `xl/worksheets/sheet10.xml`| `Sheet8` | `VBA/Sheet8` |
| `BEAM DESIGN` | 12 | `rId11`| `xl/worksheets/sheet11.xml`| `Sheet10` | `VBA/Sheet10` |
| `SLAB DESIGN` | 17 | `rId12`| `xl/worksheets/sheet12.xml`| `Sheet15` | `VBA/Sheet15` |
| `In_tem_thep` | 15 | `rId13`| `xl/worksheets/sheet13.xml`| `Sheet11` | `VBA/Sheet11` |
| `Data` | 14 | `rId14`| `xl/worksheets/sheet14.xml`| `Sheet12` | `VBA/Sheet12` |
| `Gan_tag_block` | 10 | `rId15`| `xl/worksheets/sheet15.xml`| `Sheet9` | `VBA/Sheet9` |
| *(Workbook)* | — | — | `xl/workbook.xml` | `ThisWorkbook`| `VBA/ThisWorkbook` |

---

### 1.4 Detailed Engineering Logic in Procedural Modules

#### 1. `Tinh_dam.bas` (Structural Calculation Formulas)
- **`anfa(M, b, ho) As Double`**:
  - Cell `I10`: Concrete design compression strength $R_b$ (divided by 10).
  - Formula: $\alpha_m = 100 \cdot \frac{|M|}{R_b \cdot b \cdot h_0^2}$.
- **`lamda(anfa) As Double`**:
  - Formula: $\zeta = 1 - \sqrt{1 - 2\alpha_m}$ (relative compression zone factor $\zeta$).
- **`Att(lamda, b, ho) As Double`**:
  - Cell `I11`: Rebar design tension strength $R_s$ (divided by 10).
  - Formula: $A_{stt} = \zeta \cdot \frac{R_b}{R_s} \cdot b \cdot h_0$ ($cm^2$).
- **`chuyen_dai(x, phi, a)`**:
  - Stirrup notation parser: e.g. `d10a100` -> $\phi = 10\text{ mm}$, $a = 100\text{ mm}$.
- **`qd(x, n) As Double`**:
  - Cell `I12`: Transverse rebar strength $R_{sw}$ (divided by 10).
  - Formula: $q_{sw} = \frac{R_{sw} \cdot n \cdot \pi \cdot D^2}{4 \cdot U \cdot 10}$ ($kN/cm$).
- **`Qdb(b, ho, qd) As Double`**:
  - Cell `N10`: Concrete design tensile strength $R_{bt}$ (divided by 10).
  - Formula: $Q_b = h_0 \cdot \sqrt{8 \cdot R_{bt} \cdot b \cdot q_{sw}}$ ($kN$).
- **`dt_thep(x) As Double`**:
  - Evaluates main rebar string notation (e.g. `5f20`, `2f25`): extracts bar count $sl$ and diameter $\phi$, sums total cross-sectional area $A_s$.
- **`tinh_a(abv, h, x) As Double`**:
  - Calculates distance to reinforcement centroid from tension face, accounting for concrete cover $abv$, bar diameters, and $25\text{ mm}$ clear gap between bar layers.
- **`dt1(D) As Double`**:
  - Formula: $\text{dt1} = \frac{\pi D^2}{400}$ ($cm^2$).

#### 2. `Ve_dam.bas` (CAD Integration & Charting)
- **`GetCadApplication() As Object`**: Probes COM instances across AutoCAD (`AutoCAD.Application`, versions 24, 25, 26), GstarCAD (`GCAD.Application`, `GstarCAD.Application`), and ZWCAD (`ZwCAD.Application`).
- **`laychieudai()`**: Invokes AutoCAD `ActiveDocument.Utility.GetPoint` to prompt user for column/span endpoints. Calculates distance $kc = \text{Round}(\sqrt{\Delta x^2 + \Delta y^2} / 5) * 5$ and inputs into Excel selection (row 11). Shifts cell by `Offset(0, 1)` to chain span inputs.
- **`chuyen_thep(x, sl, phi)` / `chuyen_ht(x, delta_h, sl, phi)`**: String parsing for longitudinal rebars and drop/haunch step offsets (e.g., `-50;5f20`).
- **`abc()` & `bieudo(a As Range, sc As Object) As Boolean`**: Dynamically adjusts Chart 38 series for stirrup shapes (`U`-cap vs closed/`C` ties), binding series to range `=Dam!$I$...:$L$...` or `=Dam!$I$...:$M$...`.

#### 3. `save_info.bas` (Sheet Database Persistence)
- **`save()`**: Matches beam name in `Range("B3")` against column E in sheet `save_info`. Saves:
  - Header parameters: dimensions, covers, hooks (`B1:B9`, `G1:G9`, `H3`, `H5`, `I1`, `I5`, `I8`, `J7`, `J9`).
  - Main bars: `B11` (top), `B12` (bottom).
  - Multi-span arrays: starting at column 29, saves support width/span length plus 32 rows of reinforcement parameters per span (stride of 33 cells per span).
- **`load_data()`**: Prompts user for beam name via `InputBox`, locates row, and restores all parameters into active sheet `Dam`.
- **`tra_bang_kl(phi As Object) As Double`**: Steel unit weight table ($kg/m$):
  - $\phi 6$: 0.222, $\phi 8$: 0.395, $\phi 10$: 0.617, $\phi 12$: 0.888, $\phi 14$: 1.21, $\phi 16$: 1.58, $\phi 18$: 2.00, $\phi 20$: 2.47, $\phi 22$: 2.98, $\phi 25$: 3.85, $\phi 28$: 4.83, $\phi 32$: 6.31, $\phi 36$: 7.99, $\phi 38$: 8.90, $\phi 40$: 9.86, $\phi 41$: 10.36, $\phi 43$: 11.40, $\phi 50$: 15.42, $\phi 51$: 16.04.

#### 4. `to_hop.bas` (1D Rebar Cutting Stock Optimization)
- Declares `Type info_thep_cat`: `phi As Integer, dai As Integer, sl As Long, mo_ta As String, du As Integer`.
- Implements First-Fit Decreasing heuristic for standard $11.7\text{ m}$ ($11700\text{ mm}$) stock bars.
- Reads scrap inventory from sheet `Kho_thep` and updates cutting schedule on sheet `To_hop_thep`.

#### 5. `Module1.bas` (Rebar Tag Printing)
- `TaoTemThepHangLoat()`: Copies 12-row template (`A1:F12`) on `In_tem_thep` for every bar schedule entry on `Thong_ke`, pasting into `K:P` with synchronized row heights and column widths.

---

## 2. Logic Chain

1. **Premise 1**: The user replaced `C:\kata_pro\Kata.xlsm` with the unlocked workbook (size: 474,272 bytes, SHA-256 `89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D`).
2. **Premise 2**: Direct inspection with `oletools.olevba.VBA_Parser` confirmed that `xl/vbaProject.bin` inside this workbook contains 21 unencrypted, decompressed VBA module streams (`[MS-OVBA]`).
3. **Premise 3**: Extraction requires zero mutation of `C:\kata_pro\Kata.xlsm`. Reading binary data and running decompressor in memory respects the 100% READ-ONLY rule.
4. **Premise 4**: Analysis of `xl/workbook.xml` and worksheet definitions establishes an exact 1:1 mapping between the 15 worksheet XML elements (`sheetId: 1..20`), their VBA codeNames (`Sheet1`..`Sheet15`), and their user-facing tab names (`Dam`, `Cot`, `Vach`, etc.).
5. **Premise 5**: The currently deployed files at `C:\HPKata\Kata.xlsm` and `HPKata/Distribution/Assets/Kata.xlsm` are identical copies of the old locked file (`474,860` bytes, SHA-256 `BCB285B3...`). Synchronizing them with `C:\kata_pro\Kata.xlsm` satisfies Requirement R1.3 while leaving `C:\kata_pro\` completely untouched.
6. **Premise 6**: The extracted plain-text VBA code across all 21 modules contains ~900 lines of code, which fits completely and cleanly into the structured markdown artifact `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md`.

---

## 3. Caveats

- `C:\kata_pro\` is strictly READ-ONLY. No temporary files or logs may ever be written into `C:\kata_pro\`.
- All extraction and file copying must be performed via Python or PowerShell using source read streams.
- The document modules `Sheet1.cls` through `Sheet15.cls` are standard empty Excel worksheet class modules (8 lines each containing VBA class attributes). They are documented for completeness and 100% audit coverage.
- When running `dotnet test` or `dotnet build` in `HPKata`, the single-node execution rule (`--disable-build-servers -m:1`) must be strictly observed.

---

## 4. Conclusion & Action Plan for Worker

### Concrete Directives for Worker:

#### Step 1: Scripted Extraction & Markdown Generation
Execute an automated Python script (e.g. `scripts/extract_vba_kata.py` or inline runner) that:
1. Reads `C:\kata_pro\Kata.xlsm` with `oletools.olevba.VBA_Parser`.
2. Verifies file size (`474,272`) and SHA-256 (`89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D`).
3. Iterates over all 21 modules, extracting raw VBA text.
4. Generates `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` according to the 7-section schema:
   - §1: Document Header, Metadata & Forensic Hashes
   - §2: 21-Module Executive Summary & Statistics Table
   - §3: Worksheet ↔ Document Module Mapping Table
   - §4: Procedural Modules Deep Dive (`Tinh_dam`, `Ve_dam`, `to_hop`, `save_info`, `Module1`) with complete code listings, parameters, formulas, and cell references
   - §5: Document & Workbook Modules (`ThisWorkbook`, `Sheet1`..`Sheet15`)
   - §6: Comprehensive Cell & Range Reference Dictionary (`B3`, `I10`, `I11`, `N10`, etc.)
   - §7: Forensic Evidence Ledger & Traceability Matrix

#### Step 2: Synchronization of Unlocked Workbook
Execute file copy from `C:\kata_pro\Kata.xlsm` to:
1. `C:\HPKata\Kata.xlsm`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Kata.xlsm`
Verify that both destinations match `474,272` bytes and SHA-256 `89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D`.

#### Step 3: Verification of Build & Tests
Verify that `HPKata` solution compiles and passes tests cleanly:
```powershell
dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build
```

---

## 5. Verification Method

To independently verify this investigation:

1. **Verify Unlocked File Integrity & Size**:
   ```powershell
   python -c "import hashlib, os; p = r'C:\kata_pro\Kata.xlsm'; print('Bytes:', os.path.getsize(p)); print('SHA256:', hashlib.sha256(open(p, 'rb').read()).hexdigest().upper())"
   ```
   - Expected Output:
     - Bytes: `474272`
     - SHA256: `89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D`

2. **Verify 21 VBA Module Stream Extraction**:
   ```powershell
   python -c "from oletools.olevba import VBA_Parser; p = VBA_Parser(r'C:\kata_pro\Kata.xlsm'); print('Total modules:', len(list(p.extract_macros()))); p.close()"
   ```
   - Expected Output:
     - `Total modules: 21`

3. **Verify Destination Synchronization Post-Worker**:
   ```powershell
   python -c "import hashlib; [print(p, hashlib.sha256(open(p, 'rb').read()).hexdigest().upper()) for p in [r'C:\HPKata\Kata.xlsm', r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Kata.xlsm']]"
   ```
   - Expected Output: Both paths match `89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D`.

4. **Verify Generated Documentation**:
   - Check that `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` exists and contains sections for all 21 modules (`Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`, `Module1`, `ThisWorkbook`, `Sheet1`..`Sheet15`).

5. **Invalidation Conditions**:
   - Any file in `C:\kata_pro\` is modified or has a changed hash/timestamp.
   - Any of the 21 modules fails to decompress.
   - Destination `Kata.xlsm` files differ from `C:\kata_pro\Kata.xlsm`.
