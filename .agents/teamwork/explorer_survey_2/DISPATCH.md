# Dispatch: Explorer Survey 2 (Target Solution Inventory & Mocks Audit)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\`
- Parent Orchestrator: `orchestrator_25`
- Role: `teamwork_preview_explorer`

## Mandatory Reference Documents
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Read header `## 2026-10-18T05:27:06Z` carefully)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\DISPATCH.md`

## Mission
Survey the existing `HPDcePro/HPDcePro.slnx` solution and all its projects. Audit existing codebase for mocks, placeholders, and dummy data. Check coverage of AutoCAD Jigs, UI Forms, and build readiness.

## Detailed Tasks
1. Inspect `HPDcePro/HPDcePro.slnx` and all project files (`HPDcePro.Core`, `HPDcePro.UI`, `HPDcePro.AutoCad`, `HPDcePro.Revit`, `HPDcePro.Excel`, `HPDcePro.Mcp`, `HPDcePro.Desktop`, and test projects).
2. Scan for and catalog all existing mocks/placeholders/sample functions (e.g. `CalculateSampleBeam`, `GenerateSampleColumnBuffer`, dummy returns) across all files in `HPDcePro/`.
3. Audit CAD interactive Jigs in `HPDcePro.AutoCad`: verify which of the 11 required Jigs (`VeMBKC_JIG`, `JigSlabRebar`, `TagLeader`, `TagChiaThep`, `ijgRaithepchamtron`, `ijgVeDaiVong`, `JigVeDaiC`, `VePolylineBatKy`, `ijgVeThepVachCT`, `JigVeVachHam`, `DimNhanhMBKC`) currently exist, are partial, or are missing.
4. Audit UI Forms in `HPDcePro.UI`: verify status of specialized dialogs (`DamCong_Frm`, `MongCoc_Frm`, `MongCoc_FrmLopThepThem`, `MongCoc_FrmChonThepTuDong`, `ThangMoi_Frm`, `CocBT_Frm`, `TKT_frmMain`, `AutoStrWindow`, `Bea_ToiUuCatThepV2`, `DcePalette`, `DceRibbon`, etc.).
5. Run a test build `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` and test run `dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` to determine current compiler errors/warnings and test results.
6. Write comprehensive report to `.agents/teamwork/explorer_survey_2/handoff.md`.
