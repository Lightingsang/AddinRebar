# DISPATCH — Orchestrator (orchestrator_26)

## Identity & Role
- **Type**: `teamwork_preview_orchestrator`
- **Role**: Project Orchestrator — HPKata REA 6.6.0 Audit & Standalone Reconstruction
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_26\`
- **Parent / Sentinel**: `55c6cce2-b409-4479-a3c6-c5e266360554`

## Authoritative Reference
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-18T05:29:18Z`)

## Mission Objectives
Execute a comprehensive, strictly read-only, evidence-backed reverse engineering analysis of `C:\kata_pro` using **REA 6.6.0** (`rea@6.6.0`), produce the complete evidence audit dossier at `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` plus the 4-part executive report (**Đã giải mã – Chưa giải mã – Bằng chứng – Bước tiếp theo**), and complete the standalone `C:\HPKata\` reconstruction & installer (`HPKata_Setup.exe`) grounded in the verified REA 6.6.0 evidence.

## Non-Negotiable Constraints
1. **REA 6.6.0 CLI Location**:
   `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"` (verify `--version` outputs `rea@6.6.0` before running any analysis).
2. **100% Read-Only on `C:\kata_pro\`**:
   Never modify, overwrite, delete, or corrupt any file in `C:\kata_pro\`. Record SHA-256 digests before and after analysis to prove zero mutation.
3. **No Hallucination / Strict Evidence Boundary**:
   Every claim must cite a concrete `evidence_id` (`ev_...`), `sha256` digest, `file_offset`, or exact file/line/sheet reference. Any component that cannot be decompiled or recovered must be explicitly documented under **CHƯA GIẢI MÃ ĐƯỢC** with its exact technical cause (e.g., duplicate `#GUID` stream, native PE without CLI header, metadata representation budget limit, obfuscated method/type identifiers, or compiled VBA P-code).
4. **UI Framework Constraint for `HPKata`**:
   Keep the existing `System.Windows.Window` + `MaterialDesignThemes` (`v5.2.1`) UI framework in `HPKata.UI`, `HPKata.Revit`, and `HPKata.AutoCad`. Do **NOT** switch to `MahApps.Metro`. Keep the existing `Theme.xaml` StaticResource keys intact so all 13 windows instantiate without `StaticResourceExtension` errors.
5. **Clean Code Governance & Single-Node Build**:
   Every `.cs` file in `HPKata/` must be `<= 300` lines (Rule C2), every method `<= 50` lines (Rule M3), 0 compiler warnings. Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1`.

## Scope & Requirements Breakdown
### R1. Verify REA 6.6.0 & Full Static Binary Audit on all 9 DLLs in `C:\kata_pro\`
- Verify `rea@6.6.0`.
- Run REA 6.6.0 commands (`inspect-managed-artifact`, `inspect-managed-members`, `inspect-managed-native-boundaries`, `inspect-pe-resources`, and `build-reconstruction-obligation-ledger` where supported) across all 9 DLLs:
  - `Kata_Class_Lib.dll`
  - `Kata_Class_Lib_Revit.dll`
  - `Kata_pro64_Cad2025.dll`
  - `Kata_pro64_Cad2013.dll`
  - `Kata_pro64_GstarCad.dll`
  - `Kata_pro64_GstarCad2025.dll`
  - `Kata_pro64_ZwCad.dll`
  - `Kata_pro64.dll`
  - `kata_pro.dll`
- Extract exact metadata, SHA-256, classification, references, resources, attributes, PE resources, P/Invoke boundaries.
- Member inventories (`types`, `methods`, `fields`, `member_refs`, `call_edges`, `field_accesses`, `user_strings`) for `Kata_Class_Lib.dll` and `Kata_Class_Lib_Revit.dll`.
- Attribute-level command registrations (`[CommandMethod]` names) and assembly references from `Kata_pro64_Cad2025.dll` and sibling CAD DLLs.

### R2. Complete Inventory & Structural Decoding of All Unencrypted Assets in `C:\kata_pro\`
- AutoLISP & Menu Files: `Goi lenh.lsp` (118 `defun c:` command aliases + 86 configuration lines), `Goi lenh - Gstarcad.lsp`, `Goi lenh - Zwcad.lsp`, `menukata.cui`, `menukata.cuix`, `menukata.mnr`, `menukata_light.mnr`.
- CAD Template & Structural Model: `Iso_kata.dwg` (119 block definitions, 12 layers, 15 dimstyles, text styles) and `ISO_KATA.EDB`.
- Excel Workbooks: `Kata.xlsm`, `Kata ENG.xlsm`, `Kata-GCAD.xlsm`, `Kata-ZwCad.xlsm`, `Iso_QHPlus.xls` (exact sheet names, cell coordinates for `Dam`, `Cot`, `Vach`, `Thong ke`, and status of `xl/vbaProject.bin`).
- Visual Shape & Icon Libraries: `dangthep\` (`00.wmf`..`67.wmf` + `.png`), `Cot\` (`*.wmf`), `Icon\` (`Ribbon\*.png`), `IMG\` (`*.png`).

### R3. Produce Audit Dossier & Complete Standalone `C:\HPKata\` Suite
- Write `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`:
  - **Mục 1 — Đã giải mã (Decoded Components)**
  - **Mục 2 — Chưa giải mã được (CHƯA GIẢI MÃ ĐƯỢC & Nguyên nhân kỹ thuật)**:
    - `Kata_pro64.dll`: malformed duplicate `#GUID` stream at `file_offset: 2555180`.
    - `kata_pro.dll`: not-managed native PE without CLI header.
    - `Kata_pro64_Cad2025.dll` and sibling CAD DLLs: metadata representation budget limit (exceeds 67108864 byte budget at offset 14897632) and obfuscation.
    - Obfuscated internal identifiers (`Class0..`, `smethod_0..`, encrypted string tables) inside `Kata_Class_Lib.dll` and `Kata_Class_Lib_Revit.dll`.
    - Binary VBA macro stream `xl/vbaProject.bin` in `Kata.xlsm`.
  - **Mục 3 — Bằng chứng (Evidence Ledger)**: table with `evidence_id`, `sha256`, `byte_length`, `file_offset`.
  - **Mục 4 — Bước tiếp theo (Next Steps & Gap Matrix vs. `C:\HPKata\`)**.
- Complete any remaining standalone `C:\HPKata\` integration items (`KataDwgTemplateImporter` in `HPKata.AutoCad/Interop/`, 86-parameter `Goi lenh.lsp` sync in `SetupViewModel`/`SetupController`/`SetupCommand`, visual asset loader, and `HPKata.Installer` / `HPKata_Setup.exe` deploying all DLLs and assets to `C:\HPKata\`).
- Verify `dotnet build` & `dotnet test` pass 100% with zero warnings, zero errors.


## 2026-10-18T05:31:53Z
You are Project Orchestrator (orchestrator_26).
Your working directory is g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_26\.
Read your dispatch file at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_26\DISPATCH.md and the authoritative request in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (header ## 2026-10-18T05:29:18Z).
Create your plan.md and progress.md in your working directory.
Follow all non-negotiable constraints:
1. REA 6.6.0 CLI location: node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs" (verify --version outputs rea@6.6.0).
2. 100% Read-Only on C:\kata_pro\ (record SHA-256 before and after).
3. No Hallucination / Strict Evidence Boundary: cite concrete evidence_id (ev_...), sha256, file_offset, or exact file/line/sheet reference. Unrecoverable items marked CHƯA GIẢI MÃ ĐƯỢC with exact technical cause.
4. UI Framework Constraint: keep System.Windows.Window + MaterialDesignThemes (v5.2.1) in HPKata.UI, HPKata.Revit, HPKata.AutoCad. Do NOT switch to MahApps.Metro. Keep Theme.xaml StaticResource keys intact.
5. Clean Code Governance & Single-Node Build: <= 300 lines/file, <= 50 lines/method, 0 compiler warnings, dotnet build/test with --disable-build-servers -m:1.
6. Deliverables: produce docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md (4-part report: Đã giải mã, Chưa giải mã được, Bằng chứng, Bước tiếp theo), and complete standalone C:\HPKata\ suite + HPKata_Setup.exe with 100% test pass.
Decompose into subtasks, dispatch workers/reviewers, continuously update progress.md, and report completion when fully verified.
