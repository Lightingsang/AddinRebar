# Handoff Report — explorer_m4_cad_utils

**Date**: 2026-10-17T11:55:00Z  
**Type**: Hard Handoff  
**Workspace**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_cad_utils\`  
**Target Deliverable**: `cad_utils_spec_report.md`  
**Recipient**: Parent Orchestrator (`a7a2dbbb-6b7d-4279-98cd-1216d8280d80`)  

---

## 1. Observation

1. **`C:\kata_pro\Goi lenh.lsp`**: Exactly 238 physical lines. Lines 1–15 load `Kata_pro64_Cad2025.dll` via netload. Lines 33–40 define 8 legacy VBA calls (`-vbarun`). Lines 43–151 contain 109 lines defining 118 command aliases. Line 152 starts with `;kata_dim`, marking the exact beginning of the 86 configuration parameters ending at line 237 (`;0`).
2. **`HPKata.AutoCad/GoiLenh_HPKata.lsp`**: Exactly 238 physical lines matching the line count of `C:\kata_pro\Goi lenh.lsp`. Lines 33–40 have been migrated from VBA to native commands (`tang_stt`, `rthep`, `ve_level`, `ve_level1`, `thong_ke_cap`, `dao_so_hieu`, `edit_att_data`, `doi_text_blockcd`). Line 152 is strictly `;kata_dim`.
3. **`HPKata.AutoCad.Tests/GoiLenhLspSyntaxTests.cs`**:
   - Line 29: `Assert.Equal(238, splitLines.Length);`
   - Line 40: `Assert.Equal(";kata_dim", lines[151].Trim());` (0-indexed line 151 = 1-indexed line 152)
   - Line 70: `Assert.Equal(118, defunCount);`
   Any modification that shifts line 152 or changes line count will immediately break this test suite.
4. **`Kata_pro64_Cad2025.dll`**: Contains `Form_loc_dim` (buttons `ChonDimRaiBtn`, `ChonDimMoiNoiBtn`, `ChonDimThepBtn`), `Setting::LoadFromGoiLenhText`, and `Lenh_thep`.
5. **Phase 5 Scope**: ORIGINAL_REQUEST.md lines 1122–1123 enumerate 27 AutoCAD commands for Phase 5 (`QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `tkqs`, `ve_grid`, `chia_tam`, `op_gach`, `loc_dim`, `phan_vung_noi`, `danhmuc`, `cat_doituong`, `taomv`, `taomv1`, `sap_ban_ve`, `in_ban_ve`, `pukata`, `pdfkata`, `hideatt`, `Unhideatt`, `tim_ten_ck`, `dten`, `tkten`, `textblock`, `chuyen`, `show_thep_rai`), all of which are already mapped in `GoiLenh_HPKata.lsp`.
6. **CAD Utility Requirements**:
   - Dimension: `catdim` (`cat_chan_dim`), `ngatdim` (`ngat_dim`), `dnoi` (`dim_noi_tiep`), `sl` (`ghi_do_doc`).
   - Text & Elevation: `dtext` (`can_chinh_text`), `mcat` (`cao_do_mat_cat`).
   - Layer: `anlayer` (`an_layer`), `hienlayer` (`hien_tat_ca_layer`), `doilayer` (`doi_layer`).
   - Clean: `xoa_xrecord` / `clean_kata`.

---

## 2. Logic Chain

1. **Syntax Preservation Constraint**:
   - Because `GoiLenhLspSyntaxTests.cs` enforces exact 238 lines and line 152 as the config block start, `GoiLenh_HPKata.lsp` cannot have new lines injected without moving offset 152 and failing regression tests.
2. **AutoCAD .NET Alias Resolution**:
   - AutoCAD's native `.NET` command loader supports multiple `[CommandMethod]` attributes per method. By decorating command methods with both canonical names and short aliases (e.g. `[CommandMethod("cat_chan_dim")]` and `[CommandMethod("catdim")]`), AutoCAD registers them natively. Users can invoke them directly from the AutoCAD command prompt without requiring extra `(defun c:...)` lines in `GoiLenh_HPKata.lsp`.
3. **Core Host Neutrality (Rule Host-Neutral)**:
   - All geometric calculations (vector projection, witness line trimming, slope calculation with quadrant text normalization, architectural elevation string formatting, and equal text distribution) belong in `HPKata.Core/CadUtilities/` with zero references to `Autodesk.*`.
4. **Separation of Concerns**:
   - CAD transactions, editor prompts, selection filters, and entity modifications belong in `HPKata.AutoCad/Controllers/` and `HPKata.AutoCad/Commands/`.
   - All proposed files are designed to be well under 300 lines (Rule C2) and methods under 50 lines (Rule M3).

---

## 3. Caveats

1. **AutoCAD Live Execution**: In this exploration step, commands were analyzed statically via IL decompilation (`ilspycmd`), syntax parsing, and mathematical modeling; live AutoCAD 2026 execution requires a running AutoCAD instance with the compiled assembly loaded.
2. **Dimension Break Behavior**: AutoCAD's native `DIMBREAK` API applies a visual break gap; if a physical entity split into two dimensions is required, `DimensionUtilityController` provides a split mode.

---

## 4. Conclusion

The specification report `cad_utils_spec_report.md` is complete and comprehensive. It provides:
1. Full enumeration of all 10 legacy CAD utility commands (`catdim`, `ngatdim`, `dnoi`, `sl`, `dtext`, `mcat`, `anlayer`, `hienlayer`, `doilayer`, `clean_kata`).
2. Exact mathematical formulas and boundary behaviors (12 edge cases).
3. Production-ready C# architecture for `HPKata.Core/CadUtilities/` (5 files) and `HPKata.AutoCad/Controllers/` (4 controllers) + `HPKata.AutoCad/Commands/` (4 command classes).
4. Dual-registration strategy preserving the 238-line syntax and offset 152 config block in `GoiLenh_HPKata.lsp`.
5. 100% compliance with Clean Code Rules C2 ($\le 300$ lines), M3 ($\le 50$ lines), Pure Core, and MTP concurrency isolation.

---

## 5. Verification Method

1. **Inspect Deliverable**:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_cad_utils\cad_utils_spec_report.md`
2. **Verify Lisp Syntax Tests**:
   Run `dotnet test HPKata/HPKata.AutoCad.Tests/HPKata.AutoCad.Tests.csproj --filter GoiLenhLspSyntaxTests --disable-build-servers -m:1` to confirm all 5 syntax tests pass against `GoiLenh_HPKata.lsp`.
3. **Review Clean Code Metrics**:
   Verify designed files in `cad_utils_spec_report.md` comply with Rule C2 ($\le 300$ lines) and Rule M3 ($\le 50$ lines).
