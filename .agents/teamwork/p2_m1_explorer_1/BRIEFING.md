# BRIEFING — 2026-10-18T08:25:00Z

## Mission
Investigate and formulate the technical implementation for extracting all 21 plain-text VBA modules from C:\kata_pro\Kata.xlsm (via OLE2 / MS-OVBA 2.4.1 RLE decompression) and synchronizing to target paths while keeping C:\kata_pro 100% read-only.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, reverse-engineering, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_1\
- Original parent: 70f8ea72-c348-4512-8527-7930db5e95da
- Milestone: M1 (VBA Extraction & Sync)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Non-negotiable: C:\kata_pro\ is 100% READ-ONLY! Never modify, overwrite, or delete any file in C:\kata_pro\.
- ZERO parallel dotnet execution: never run dotnet build/test concurrently. Ban dotnet test (direct .exe runner only when applicable).
- No mutation of project files outside working directory during exploration.

## Current Parent
- Conversation ID: 70f8ea72-c348-4512-8527-7930db5e95da
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `C:\kata_pro\Kata.xlsm` (Size: 474,272 bytes, SHA-256: `89d1243cfc91a3dd2254942a41bd5ebabcaa43ebd0d8b758b016d066ab010b3d`)
  - `xl/vbaProject.bin` (Size: 105,472 bytes in-memory)
  - `PROJECT` and `VBA/dir` OLE2 compound streams
  - `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` (Sections 1.8, 2.5, 3)
  - `C:\HPKata\Kata.xlsm` & `HPKata/Distribution/Assets/Kata.xlsm` (Current size: 474,860 bytes, outdated hash `bcb285b3...`)
- **Key findings**:
  - Unlocked `Kata.xlsm` contains exactly 21 VBA modules: 5 procedural modules (`Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`, `Module1`), 1 workbook module (`ThisWorkbook`), and 15 worksheet document modules (`Sheet1`..`Sheet15`).
  - OLE2 compound document uses standard MS-OVBA 2.4.1 RLE stream compression.
  - Available tools: Python 3.14 with `olefile` and `oletools.olevba` are pre-installed and can decompress all 21 modules in-memory in ~1 second with zero modification to `C:\kata_pro\`.
  - Sheet Codename mapping established: `sheet3.xml` ("Dam") is `Sheet4`, `sheet2.xml` ("Cot") is `Sheet2`, `sheet1.xml` ("Vach") is `Sheet7`, `sheet5.xml` ("save_info") is `Sheet1`, `sheet6.xml` ("Thong_ke") is `Sheet3`, etc.
  - Complete algorithm mechanics mapped: TCVN 5574 beam formulas, CAD COM interop across 8 CAD ProgIDs, 1D rebar cutting stock optimization, beam serialization format (33 columns/span), batch tag printing.
- **Unexplored areas**:
  - None within Milestone 1 scope. Investigation 100% complete and verified.

## Key Decisions Made
- Formulated single-pass in-memory Python extraction script using `zipfile`, `olefile`, and `oletools.olevba` (`VBA_Project` and `decompress_stream`).
- Designed exact layout and content for `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` (6 sections).
- Specified atomic file copy and SHA-256 verification steps for synchronization.

## Artifact Index
- `BRIEFING.md` — Persistent working memory
- `progress.md` — Liveness heartbeat
- `prototype_extract.py` — Fully tested prototype in-memory extraction tool
- `dump_proc_modules.py` / `proc_modules_dump.txt` — Full dump and verification of 5 procedural modules
- `inspect_sheets.py` & `inspect_sheet_codenames.py` — Worksheet XML vs CodeName mapping analysis
- `handoff.md` — Comprehensive 5-component handoff report
