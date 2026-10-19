# Progress — Explorer Survey 1

Last visited: 2026-10-18T05:48:00Z

## Status
- [x] 1. Verify `rea@6.6.0` and `ilspycmd`.
  - Both CLI and mjs confirmed at version `6.6.0`.
  - `ilspycmd` confirmed at `11.1.0.9782` with decompiler engine `11.1.0.9782`.
- [x] 2. Inspect `C:\Dce Pro` (STRICTLY READ-ONLY).
  - Cataloged 190 DLLs across `C:\Dce Pro` and unpacked components.
  - Core managed DLLs: `DCE Pro.dll` (44.58 MB), `DceSteel.dll` (20.36 MB), `DbimSteelAutocad.dll` (4.20 MB), `DCE Excel.dll` (10.86 MB).
  - Verified native DLLs: `DceAnalyssiss64.dll` (338 KB) and `DceAnalyssiss32.dll` (281 KB).
  - Dumped export table: exactly 69 native C++ exports.
- [x] 3. Unpack `DBIM_RVT_26.exe` into scratch workspace.
  - Successfully unpacked Inno Setup archive via `innoextract` into `scratch/dce_rvt_unpacked/`.
  - Extracted 24 DLLs, `DBimRevit.addin`, `DBimRevit.runtimeconfig.json` (.NET 8.0 target), and `DBimRevit.deps.json`.
- [x] 4. Run sample inspection via `rea inspect-managed-artifact` and `ilspycmd`.
  - Captured official REA evidence IDs for all 7 key assemblies.
  - Identified Babel .NET protection: multi-module merge (Module/Assembly row count = 2), out-of-bounds GUID heap, dynamic method stubs/decryption.
  - Cataloged non-obfuscated surface: all 11 DrawJigs/EntityJigs (tokens 0x02000E52-0x02000E5C), all 12 specialized UI forms, all 311 AutoCAD commands, 73 Excel ribbon commands, 49 DceSteel functions, 29 MBKC functions, 24 AutoStr steps, and all Revit commands.
- [x] 5. Record findings in `handoff.md` and communicate to parent.
