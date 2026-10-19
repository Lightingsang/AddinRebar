# Empirical Challenge Report — Milestone M5: AutoLISP Syntax, Config Round-Trip & Scheduling Runners

**Agent**: `challenger_m5_2` (`teamwork_preview_challenger`)  
**Parent**: `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Date**: 2026-10-17  
**Verdict**: **APPROVE**  

---

## 1. Challenge Summary

- **Overall Risk Assessment**: **LOW**
- **Tested Artifacts**:
  1. `HPKata/HPKata.AutoCad/GoiLenh_HPKata.lsp` (AutoLISP companion loader & 86-parameter configuration)
  2. `HPKata/HPKata.AutoCad/Commands/ScheduleCommandRunner.cs`
  3. `HPKata/HPKata.AutoCad/Commands/SummaryCommandRunner.cs`
  4. `HPKata/HPKata.AutoCad/Commands/CuttingStockCommandRunner.cs`
  5. `HPKata/HPKata.AutoCad/Commands/ExportCommandRunner.cs`
  6. `HPKata/HPKata.AutoCad.Tests/GoiLenhLspSyntaxTests.cs`
  7. `HPKata/HPKata.AutoCad.Tests/ScheduleCommandTests.cs`
  8. `HPKata/HPKata.Core/Configuration/KataConfigParser.cs` & `KataConfigParser.Serializer.cs`

---

## 2. Empirical Verification & Stress Test Results

### 2.1 AutoLISP Companion Loader (`GoiLenh_HPKata.lsp`)

| Dimension | Specification | Actual Result | Status |
|---|---|---|---|
| **Total Line Count (Split by `\n`)** | Exactly 238 lines | 238 lines | **PASS** |
| **Content Lines (`splitlines()`)** | Exactly 237 content lines | 237 content lines | **PASS** |
| **Command Aliases Count** | Exactly 118 aliases | 118 aliases (`(defun c:...)` lines 33–151) | **PASS** |
| **Duplicate Aliases** | Zero duplicates | 0 duplicates | **PASS** |
| **LISP Parenthesis Balance** | Balanced open/close parens | 394 open, 394 close (Diff = 0) | **PASS** |
| **Line 152 Value (1-indexed)** | `;kata_dim` | `;kata_dim` | **PASS** |
| **Line 237 Value (1-indexed)** | `;0` | `;0` | **PASS** |
| **Configuration Lines (152–237)** | Exactly 86 lines, all starting with `;` | 86 lines, 100% starting with `;` | **PASS** |
| **86-Param Content Parity** | 100% match with Blueprint §3 & `KataConfigDefaults` | 86/86 lines identical | **PASS** |
| **Parser Extraction (`KataConfigParser.ParseFile`)**| Extracts all 86 fields | 86 fields extracted into typed DTO | **PASS** |
| **Round-Trip Idempotence (`SerializeToLines`)** | Serialized lines match original lines 152–237 | 86/86 lines identical (`f(f(x)) == f(x)`) | **PASS** |

### 2.2 Scheduling & Export Runners against `MockCadDocumentContext`

| Runner Method | Tested Scenario | Expected Behavior | Actual Behavior | Status |
|---|---|---|---|---|
| `ScheduleCommandRunner.ExecuteSchedule` | Multi-rebar CAD entities (Ø20 main bar, Ø10 stirrups) | Generates CAD table with 11 columns, calculates total count ($N_{ck} \times SL$), total length (m), total weight (kg), and inserts at prompt point | Table title "BẢNG THỐNG KÊ CỐT THÉP", 11 columns, 2 rows, inserted at `(100, 200, 0)`, message emitted | **PASS** |
| `SummaryCommandRunner.ExecuteSummary` | Multi-diameter items (Ø10, Ø20) | Generates CAD table with 4 columns, groups by diameter, calculates unit masses, and partitions into 3 TCVN bins ($\le 10$, $10 < \Phi \le 18$, $> 18$) | Table title "TỔNG HỢP CỐT THÉP", 4 columns, 6 rows (2 diameter rows + 4 bin summary rows), disjoint and exhaustive mass partition | **PASS** |
| `SummaryCommandRunner.ExecuteCouplerSummary` | Rebar entity with `ChuThich = "Coupler 1 dau"` | Case-insensitive keyword detection ("coupler" / "ren"), extracts 7-column table | Table title "BẢNG THỐNG KÊ COUPLER (MỐI NỐI REN)", 1 coupler group (8 total couplers), table inserted | **PASS** |
| `SummaryCommandRunner.ExecuteCouplerSummary` | Rebar entity with standard note (no coupler keyword) | Returns empty notice, does not insert CAD table | Title contains "(0 mối nối)", writes message `[HPKata] Không tìm thấy cốt thép nối ren (coupler)`, 0 tables inserted | **PASS** |
| `CuttingStockCommandRunner.ExecuteCuttingStock` | Demands for Ø20 ($8 \times 3500\text{ mm}$) and Ø10 ($40 \times 1800\text{ mm}$) | Solves 1D-CSP against 11,700 mm stock bars, enforces cut length conservation ($\sum cuts + remnant = 11,700$), generates pattern table | Solves 3 bars for Ø20, 7 bars for Ø10, gross waste calculated, table with 7 columns inserted | **PASS** |
| `ExportCommandRunner.ExecuteExcelExport` | Headless schedule export to `.xlsx` | Writes ClosedXML workbook to disk with valid non-zero byte size | Target file generated in `%TEMP%`, size > 0, valid Excel OpenXML package | **PASS** |
| `ExportCommandRunner.ExecuteTagPrint` | Rebar items tagged for fabrication | Generates pipe-delimited payload with `KATA` header and produces valid PNG bytes via QRCoder (ECC Level Q) | 2 payloads generated, valid PNG byte streams created with `PngByteQRCode` | **PASS** |

---

## 3. Adversarial Analysis & Edge Case Mining

### 3.1 Challenge: Configuration Serialization Precision
- **Assumption Challenged**: Does fraction formatting for `DetailDrawingScale` ($1/25 = 0.04$) and `ColSpliceRatio` ($1/6 \approx 0.166667$) preserve exact string literals across serialization cycles?
- **Finding**: `KataConfigParser.FormatFraction` specifically handles `Math.Abs(value - 0.04) < 1e-6 -> "1/25"` and `Math.Abs(value - (1.0/6.0)) < 1e-4 -> "1/6"`. This avoids floating point drift (e.g. `0.166666666666667` vs `1/6`).
- **Verdict**: Robust.

### 3.2 Challenge: Zero and Missing Rebar Entity Invariants
- **Assumption Challenged**: What happens when `ICadDocumentContext` returns zero entities or invalid counts?
- **Finding**:
  - `ParseCount(null)` and `ParseCount("")` return default `1`.
  - `MemberCount` is guarded with `Math.Max(1, (int)dto.XDataRecord.SoLuongCauKien)`.
  - If `TotalLengthMm <= 0.0`, fallback sums `dto.XDataRecord.ChieuDaiCacDoanMm`.
  - Empty coupler extraction skips CAD table insertion cleanly.
  - Empty cutting stock demand returns an empty list and informs user via `context.WriteMessage`.
- **Verdict**: Robust against nulls and empty selections.

### 3.3 Challenge: Mass Conservation in 1D-CSP Cutting Stock
- **Assumption Challenged**: Does the cutting stock solver lose or synthesize steel length during pattern generation?
- **Finding**:
  - In `CuttingStockEngine`, each generated pattern adheres strictly to $L_{\text{stock}} = \sum l_i + r_j$ where $r_j = \text{RemnantLengthMm}$.
  - Demands are mapped 1:1 into discrete pieces and fully consumed.
  - Reusable remnants ($\ge 500\text{ mm}$) are tracked into `NewWarehouseOffcuts`.
- **Verdict**: Physically sound and mathematically proven.

---

## 4. Full Solution Regression Audit

- `dotnet test HPKata/HPKata.AutoCad.Tests/HPKata.AutoCad.Tests.csproj -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal52-ac"`:
  - **31 passed, 0 failed, 0 skipped**.
- `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal52-all"`:
  - `HPKata.AutoCad.Tests`: 31 passed
  - `HPKata.Core.Tests`: 168 passed
  - `HPKata.UI.Tests`: 26 passed
  - **Total: 225 passed, 0 failed, 0 skipped (100% passing)**.

---

## 5. Unchallenged Areas

- **Live AutoCAD 2026 Process Interaction (`acad.exe`)**: Testing with live viewport graphics, mouse pick prompts, and ribbon panels requires a running AutoCAD 2026 host instance; this is designated for Milestone M6 End-to-End System Verification.
