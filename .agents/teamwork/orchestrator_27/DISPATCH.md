# DISPATCH — Orchestrator (orchestrator_27)

## Identity & Role
- **Type**: `teamwork_preview_orchestrator`
- **Role**: Project Orchestrator — Kata Pro Phase 2 Deep Decoding & Forensic Audit
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\`
- **Parent / Sentinel**: `8771c19c-b5b6-4a22-8911-c3105ad16588`

## Authoritative Reference
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-18T08:11:37Z`)

## Mission Objectives
Execute Phase 2 Deep Decoding of all 5 remaining items in `C:\kata_pro` (the newly unlocked `C:\kata_pro\Kata.xlsm` VBA project, `Kata_pro64.dll` with duplicate `#GUID` streams, the 5 large CAD DLLs that exceeded REA's 64 MiB single-JSON budget, the obfuscated `Class0..`/`smethod_0..` identifiers, and the native PE32+ `kata_pro.dll`), update `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` and companion evidence files, and synchronize the unlocked `Kata.xlsm` into `C:\HPKata\Kata.xlsm` while keeping `C:\kata_pro` 100% read-only.

## Non-Negotiable Constraints & Stability Rules
1. **REA 6.6.0 CLI Location**:
   `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"` (`rea@6.6.0`).
2. **100% Read-Only on `C:\kata_pro\`**:
   Never modify, overwrite, delete, or touch-timestamp any file in `C:\kata_pro\`. Any binary header normalization (`Kata_pro64.dll`) MUST be performed exclusively on a temporary copy in `%TEMP%` and cleaned up after analysis.
3. **No AutoCAD Native `new Database(true, true)` in Out-of-Process Tests**:
   Never instantiate `new Autodesk.AutoCAD.DatabaseServices.Database(true, true)` in out-of-process xUnit tests (causes native `0xc0000005` popup).
4. **Zero Parallel `dotnet` Execution & Sequential Direct Runner Rule**:
   - Never run multiple `dotnet build` or `dotnet test` concurrently.
   - For tests, follow direct test execution or run single-threaded with `--disable-build-servers -m:1`. Reviewers/challengers/auditors MUST NOT invoke `dotnet` concurrently.
5. **Clean Code Governance**:
   Every new or modified `.cs` file in `HPKata/` must be `<= 300` lines (Rule C2), every method `<= 50` lines (Rule M3), 0 compiler warnings.

## Scope & Requirements Breakdown
### R1. Extract Complete Plain-Text VBA Source Code from Unlocked `C:\kata_pro\Kata.xlsm` & Sync to `C:\HPKata\`
- Parse `xl/vbaProject.bin` (`105,472` bytes) inside newly unlocked `C:\kata_pro\Kata.xlsm` (`474,272` bytes) using standard OLE2 parsing and `[MS-OVBA]` Section 2.4.1 RLE decompression.
- Recover and save complete plain-text VBA source code of all 21 VBA modules (`Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`, `Module1`, `ThisWorkbook`, `Sheet1`..`Sheet15`) into `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md`.
- Copy unlocked `C:\kata_pro\Kata.xlsm` to `C:\HPKata\Kata.xlsm` and `HPKata/Distribution/Assets/Kata.xlsm`.

### R2. Decode `Kata_pro64.dll` via Temporary BSJB Stream Header Normalization + REA 6.6.0
- In temporary copy in `%TEMP%`, normalize `num_streams` from `9` to `5` at `BSJB + 18 + ver_len` (`file_offset: 2555098`).
- Run `rea@6.6.0` (`inspect-managed-artifact`, `inspect-managed-members`, `inspect-managed-native-boundaries`, `inspect-pe-resources`).
- Record resulting `evidence_id`s, 166 Types, 4,720 Methods, 1,613 Fields, 1,212 MemberRefs, 55 CallEdges, 60 FieldAccesses, 13 Assembly References, 30 Embedded Resources, 2,931 Custom Attributes.
- Delete the temporary copy in `%TEMP%`.

### R3. Bypass REA 64 MiB JSON Budget on the 5 CAD DLLs & Build Semantic Deobfuscation Map
- For all 5 large CAD DLLs (`Kata_pro64_Cad2025.dll`, `Kata_pro64_Cad2013.dll`, `Kata_pro64_GstarCad.dll`, `Kata_pro64_GstarCad2025.dll`, `Kata_pro64_ZwCad.dll`), use ECMA-335 metadata/CIL streaming (`System.Reflection.Metadata.MetadataReader` or custom ECMA-335 table parser) to extract exact counts and inventories of `TypeDef`, `MethodDef`, `FieldDef`, `MemberRef`, `ImplMap`, and `#US` user strings without hitting REA's 64 MiB cap.
- Build `docs/rea-kata-pro/KATA_SEMANTIC_DEOBFUSCATION_MAP.md` mapping obfuscated classes (`Class0`..`ClassN`) and methods (`smethod_0`..`smethod_N`) across `Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll`, `Kata_pro64.dll`, and `Kata_pro64_Cad2025.dll` to concrete engineering roles using 193 `[CommandMethod]`s, string literals, DXF group codes, CAD/Revit API calls, and decoded VBA functions.

### R4. Static PE32+ Analysis of Native `kata_pro.dll` & Update Master Audit Dossier
- Perform complete static PE32+ analysis of `C:\kata_pro\kata_pro.dll` (`1,011,712` bytes, x64 native PE): extract PE headers, COFF sections, virtual addresses, raw sizes, entropy, Export Directory, Import Directory, embedded strings, and cross-reference with all `[DllImport]` / `ImplMap` entries in managed DLLs.
- Update `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` integrating all newly decoded components into Mục 1 & Mục 3, and accurately updating Mục 2 (only irreversible items remain).

### Acceptance Criteria
- [ ] `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` contains decompressed plain-text VBA source code of all 21 modules, and `C:\HPKata\Kata.xlsm` is updated.
- [ ] `Kata_pro64.dll` is decoded via temporary BSJB header normalization using `rea@6.6.0`, with exact evidence IDs and counts documented in `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`.
- [ ] All 5 CAD DLLs have their exact metadata counts and command call graphs documented in `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`.
- [ ] `docs/rea-kata-pro/KATA_SEMANTIC_DEOBFUSCATION_MAP.md` maps obfuscated identifiers to concrete domain responsibilities.
- [ ] `kata_pro.dll` (native x64 PE32+) has its PE analysis documented in `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`.
- [ ] All 396 files in `C:\kata_pro\` remain 100% unmodified (SHA-256 verified).
- [ ] `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` passes 100% with zero popup dialogs.
