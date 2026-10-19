# Task Assignment: Reviewer 2 (M3 Excel UDF, M4 GETFORM Hub & M5 Test Suite Review)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_2\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Specification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Objectives
Independently review and verify:
1. **Milestone M3 (R3: Complete DCE Excel & DceSteel UDF Parity)**:
   - Check `HPDcePro.Excel/RcGeotechUdfBridge.cs`: `TinhCotNeUonXien_PMM`, `TinhVachCung`, `TinhSanHaiPhuong`, `TinhDaiCoc`, `TinhMongDon`, `SucChiuTaiCoc_MaSat`, `BeRongVetNutDam`.
   - Check `HPDcePro.Excel/SteelWindUdfBridge.cs`: `TinhDamThep_TCVN5575`, `TinhCotThep_TCVN5575`, `TinhLienKetMoment_TCVN5575`, `TinhBuLongNeo_TCVN5575`, `TinhTaiTrongGio_TCVN2737`.
   - Check `HPDcePro.Excel/CalculationUdfBridge.cs`: Backward-compatible static delegation for all 12 functions.
2. **Milestone M4 (R4: Unified MainControlCenterWindow & AutoCAD Bundle Manifest)**:
   - Check `HPDcePro.UI/ViewModels/MainControlCenterViewModel.cs`: Navigation commands to 6 child windows with `Action<Type>?` test delegate, KPI summaries, `.autostr` loading/saving.
   - Check `HPDcePro.UI/Views/MainControlCenterWindow.xaml` and `.xaml.cs`: MetroWindow markup, styles, cards.
   - Check `HPDcePro.AutoCad/Commands/AutoStrCommands.cs`: `GETFORM` redirection.
   - Check `HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml`: ApplicationPlugins manifest targeting AutoCAD 2025–2026.
3. **Milestone M5 (Test Suite Expansion)**:
   - Verify `HPDcePro.Core.Tests/Drafting/SteelFrameDraftingGeneratorTests.cs` (11 tests)
   - Verify `HPDcePro.Adapters.Tests/McpToolCatalogAndExecutionTests.cs` (14 tests)
   - Verify `HPDcePro.Adapters.Tests/ExcelUdfParityTests.cs` (12 tests)
   - Verify `HPDcePro.Adapters.Tests/SteelCadCommandsTests.cs` (6 tests)
   - Verify `HPDcePro.Adapters.Tests/MainControlCenterTests.cs` (9 tests)
4. **Build & Test Verification**:
   - Run `dotnet build HPDcePro/HPDcePro.slnx -c Debug` -> Must be 0 errors, 0 warnings.
   - Run `dotnet test HPDcePro/HPDcePro.slnx -c Debug` -> Must pass 100% (>= 330 tests, 0 failed, 0 skipped).
5. **Clean Code Verification**:
   - Check every touched file: <= 300 lines, methods <= 50 lines, 0 commented code, 0 unannotated catch.

Deliver verdict: **APPROVE** or **REQUEST_CHANGES** in `handoff.md`.
Notify orchestrator via send_message.


## 2026-10-18T01:18:06Z
You are Reviewer 2 for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_2\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_2\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

Review M3 (RcGeotechUdfBridge, SteelWindUdfBridge, CalculationUdfBridge), M4 (MainControlCenterViewModel, MainControlCenterWindow, AutoStrCommands, PackageContents.xml), and M5 test suite.
Verify:
1. All 12 UDFs match TCVN formulas and backward compatibility is preserved.
2. GETFORM launches MainControlCenterWindow with live stats and navigation commands.
3. PackageContents.xml targets AutoCAD 2025–2026.
4. `dotnet build HPDcePro/HPDcePro.slnx -c Debug` -> 0 errors, 0 warnings.
5. `dotnet test HPDcePro/HPDcePro.slnx -c Debug` -> >= 330 tests pass (0 failed, 0 skipped).
6. Clean code: all files <= 300 lines, methods <= 50 lines, 0 comments, 0 unannotated catch.

Deliver verdict: APPROVE or REQUEST_CHANGES in handoff.md and notify orchestrator via send_message.
