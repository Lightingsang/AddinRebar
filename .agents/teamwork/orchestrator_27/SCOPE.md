# Scope: Kata Pro Phase 2 Deep Decoding & Master Audit Dossier

## Architecture
- Target: Complete decoding of the 5 previously blocked items in `C:\kata_pro` while strictly preserving `C:\kata_pro` as 100% read-only.
- Tools: REA 6.6.0 (`node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"`), Python OLE2/MS-OVBA decompression, ECMA-335 metadata streaming, PE32+ parser.
- Deliverables:
  - `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md`
  - `docs/rea-kata-pro/KATA_SEMANTIC_DEOBFUSCATION_MAP.md`
  - `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` (updated Mục 1, 2, 3)
  - Synchronized `C:\HPKata\Kata.xlsm` and `HPKata/Distribution/Assets/Kata.xlsm`
  - Verification reports confirming zero mutation in `C:\kata_pro` and 100% green tests in `HPKata`.

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | VBA Extraction & Sync | Parse `xl/vbaProject.bin` in unlocked `Kata.xlsm` via OLE2 & MS-OVBA 2.4.1 RLE decompression; extract all 21 VBA modules to `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md`; sync `Kata.xlsm` to `C:\HPKata\` and `HPKata/Distribution/Assets/` | none | IN_PROGRESS |
| M2 | Kata_pro64.dll Decoding | Temporary copy in `%TEMP%`, normalize `num_streams: 9 -> 5` at offset 2555098, run REA 6.6.0, record evidence IDs & member counts, clean up `%TEMP%` | none | PLANNED |
| M3 | CAD DLLs Streaming & Semantic Map | Bypass 64 MiB cap on 5 CAD DLLs using ECMA-335 streaming, extract metadata/CIL stats, construct `docs/rea-kata-pro/KATA_SEMANTIC_DEOBFUSCATION_MAP.md` | M1 | PLANNED |
| M4 | Native kata_pro.dll & Audit Dossier | Static PE32+ analysis of `kata_pro.dll`, update `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` (Mục 1, 2, 3) | M1, M2, M3 | PLANNED |
| M5 | Zero-Mutation & Test Verification | SHA-256 verification of all 396 files in `C:\kata_pro\`, verify single-node `HPKata` test execution passes 100% | M1, M2, M3, M4 | PLANNED |
