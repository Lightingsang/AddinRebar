# PLAN — REA 6.6.0 Kata Pro Audit & Standalone HPKata Reconstruction

## Executive Summary
This project executes a strictly read-only, evidence-backed reverse engineering audit of `C:\kata_pro` using `rea@6.6.0`, produces the comprehensive forensic audit dossier `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` (4-part report: Đã giải mã, Chưa giải mã được, Bằng chứng, Bước tiếp theo), and completes the standalone `C:\HPKata\` suite with `HPKata_Setup.exe` installer, passing 100% automated tests under strict Clean Code governance.

## Non-Negotiable Constraints
1. **REA 6.6.0 CLI**: `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"` (verify `--version` outputs `rea@6.6.0`).
2. **100% Read-Only on `C:\kata_pro\`**: SHA-256 digests recorded before and after; zero modifications.
3. **No Hallucination / Strict Evidence Boundary**: Every claim backed by `evidence_id` (`ev_...`), `sha256`, `file_offset`. Unrecoverable items marked `CHƯA GIẢI MÃ ĐƯỢC` with exact technical cause.
4. **UI Framework**: `System.Windows.Window` + `MaterialDesignThemes` (`v5.2.1`) across `HPKata.UI`, `HPKata.Revit`, `HPKata.AutoCad`. Do NOT switch to `MahApps.Metro`.
5. **Clean Code Governance & Single-Node**: Every `.cs` file `<= 300` lines, methods `<= 50` lines, 0 warnings. Build/test with `--disable-build-servers -m:1`.

---

## Milestone Decomposition

### Milestone 1 (M1): REA 6.6.0 Environment Verification & Full Binary/Asset Inspection
- **Objective**: Verify `rea@6.6.0`, capture pre-audit SHA-256 digests of `C:\kata_pro\`, run all REA 6.6.0 inspection commands (`inspect-managed-artifact`, `inspect-managed-members`, `inspect-managed-native-boundaries`, `inspect-pe-resources`, and `build-reconstruction-obligation-ledger`) across all 9 DLLs, and perform structural decoding of all unencrypted assets.
- **Scope**:
  - Verification of `rea@6.6.0`.
  - 9 DLLs: `Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll`, `Kata_pro64_Cad2025.dll`, `Kata_pro64_Cad2013.dll`, `Kata_pro64_GstarCad.dll`, `Kata_pro64_GstarCad2025.dll`, `Kata_pro64_ZwCad.dll`, `Kata_pro64.dll`, `kata_pro.dll`.
  - Unencrypted assets: `Goi lenh.lsp` & siblings, `menukata.cui*`, `Iso_kata.dwg` & `ISO_KATA.EDB`, `Kata*.xlsm` & `Iso_QHPlus.xls`, `dangthep\`, `Cot\`, `Icon\`, `IMG\`.
- **Output**: Detailed Explorer reports with structured REA output JSONs and evidence IDs.

### Milestone 2 (M2): Forensic Evidence Dossier Synthesis (`REA_6_6_0_KATA_PRO_AUDIT.md`)
- **Objective**: Author the official 4-part audit dossier in `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`.
- **Structure**:
  - **Mục 1 — Đã giải mã**: Decoded components, DLL metadata, public classes/methods, AutoCAD/Revit commands, Excel schemas, DWG blocks/layers/dimstyles, LISP aliases, 86 config items, visual assets.
  - **Mục 2 — Chưa giải mã được**: Strict accounting of unrecoverable items with exact technical causes:
    - `Kata_pro64.dll`: malformed duplicate `#GUID` stream at `file_offset: 2555180`.
    - `kata_pro.dll`: not-managed native PE without CLI header.
    - `Kata_pro64_Cad2025.dll` & siblings: metadata representation budget limit (exceeds 67,108,864 bytes at offset 14,897,632) & obfuscation.
    - Obfuscated internal identifiers (`Class0..`, `smethod_0..`, encrypted strings).
    - Binary VBA macro stream `xl/vbaProject.bin`.
  - **Mục 3 — Bằng chứng**: Comprehensive Evidence Ledger mapping `evidence_id`, `sha256`, `byte_length`, `file_offset`.
  - **Mục 4 — Bước tiếp theo**: Gap Matrix vs. `C:\HPKata\` and completion roadmap.
- **Output**: `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`.

### Milestone 3 (M3): Standalone `C:\HPKata\` Integration & Installer Completion
- **Objective**: Implement and verify all standalone components in `HPKata` to ensure complete decoupling from `C:\kata_pro\`.
- **Scope**:
  - `KataDwgTemplateImporter` in `HPKata.AutoCad/Interop/`.
  - 86-parameter config sync in `SetupViewModel`, `SetupController`, `SetupCommand`.
  - Visual asset loaders (`dangthep\`, `Cot\`, `Icon\`, `IMG\`).
  - Standalone installer `HPKata_Setup.exe` deploying to `C:\HPKata\`.
  - Zero `kata_pro` occurrences in source files.
- **Output**: Standalone binaries and assets in `C:\HPKata\`, working `HPKata_Setup.exe`.

### Milestone 4 (M4): Verification, Quality Gates & Final Victory Audit
- **Objective**: Rigorous verification through Reviewer, Challenger, and Forensic Auditor gates.
- **Scope**:
  - Clean Code audit (Rules C2, M3, Q-B1, Q-B2).
  - Single-node build & test run (`dotnet build/test ... --disable-build-servers -m:1`).
  - Post-audit SHA-256 verification of `C:\kata_pro\` (0 mutations).
  - Auditor integrity check: Clean verdict required.
- **Output**: Final verification report and completion sign-off.
