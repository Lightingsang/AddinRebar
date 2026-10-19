# Dispatch Specification — Milestone M2 Implementation Worker (worker_hpkata_m2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Input Explorer Handoff Reports (READ CAREFULLY BEFORE CODING):
1. Feature 5: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_1\handoff.md`
2. Feature 6: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_2\handoff.md`
3. Features 7 & 8: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_3\handoff.md`

## Exclusively Owned Files:
1. `HPKata/HPKata.Core/Excel/**`
2. `HPKata/HPKata.Core.Tests/Excel/**`
3. `HPKata/HPKata.UI/ViewModels/BeamDetailingViewModel.Excel.cs`
4. `HPKata/HPKata.UI/ViewModels/ColumnDetailingViewModel.Excel.cs`
5. `HPKata/HPKata.UI/ViewModels/WallDetailingViewModel.Excel.cs`
6. `HPKata/HPKata.UI/ViewModels/BeamDetailingViewModel.cs`
7. `HPKata/HPKata.UI/ViewModels/ColumnDetailingViewModel.cs`
8. `HPKata/HPKata.UI/ViewModels/WallDetailingViewModel.cs`
9. `HPKata/HPKata.UI/Views/BeamDetailingWindow.xaml`
10. `HPKata/HPKata.UI/Views/ColumnDetailingWindow.xaml`
11. `HPKata/HPKata.UI/Views/WallDetailingWindow.xaml`
12. `HPKata/HPKata.UI.Tests/ViewModels/ExcelIntegrationTests.cs`
13. `HPKata/HPKata.AutoCad/Commands/ContinuousBeamCommands.cs`
14. `HPKata/HPKata.AutoCad/Commands/ColumnCommands.cs`
15. `HPKata/HPKata.AutoCad/Commands/WallCommands.cs`
16. `HPKata/HPKata.AutoCad/Commands/ScheduleCommands.cs`
17. `HPKata/HPKata.AutoCad/Controllers/**`
18. `HPKata/HPKata.AutoCad/Interop/KataLayerHelper.cs`
19. `HPKata/HPKata.AutoCad.Tests/**`
20. `HPKata/HPKata.Revit/Commands/**`
21. `HPKata/HPKata.Revit.Tests/**`

## Work Items:
### 1. Feature 5 — Bi-Directional Excel Engine (`HPKata.Core/Excel/`):
- Implement `IKataExcelSession`, `KataComExcelSession` (COM late-binding reflection for running Excel), `KataClosedXmlSession` (ClosedXML file fallback), `KataExcelSessionProvider`.
- Implement DTOs: `KataBeamSheetData`, `KataColumnSheetData`, `KataWallSheetData`, `KataRebarScheduleItem`.
- Implement Readers & Writers: `KataBeamSheetReader`, `KataBeamSheetWriter`, `KataColumnSheetReader`, `KataColumnSheetWriter`, `KataWallSheetReader`, `KataWallSheetWriter`, `KataQhPlusExporter`, `KataScheduleSheetReader`, `KataScheduleSheetWriter`.
- Implement `IKataExcelAdapter` and `KataExcelAdapter`.
- All files strictly `<= 250` lines (Rule C2 `<= 300`), methods `<= 50` lines (Rule M3). Pure C# in `HPKata.Core`.
- Add unit tests in `HPKata.Core.Tests/Excel/` verifying roundtrip reading & writing with `KataInstallEnvironment.UseRootDirectory(tempDir)`.

### 2. Feature 6 — ViewModel & View Integration (`HPKata.UI`):
- Implement partial classes `BeamDetailingViewModel.Excel.cs`, `ColumnDetailingViewModel.Excel.cs`, `WallDetailingViewModel.Excel.cs`.
- Implement commands: `OpenExcelCommand`, `ReadExcelCommand`, `WriteExcelCommand`, and `AutoSyncOnApply` on `Apply()`.
- Add default constructor parameter `IKataExcelAdapter? excelAdapter = null` to preserve parameterless construction and guarantee that all 13 WPF windows instantiate on STA thread without throwing under both Dark and Light themes.
- Update `BeamDetailingWindow.xaml`, `ColumnDetailingWindow.xaml`, `WallDetailingWindow.xaml` with Row 1 Excel toolbar using `Card.Panel`, `Button.Secondary`, CheckBox `AutoSyncOnApply`, and `ExcelStatus` indicator.
- STRICT UI CONSTRAINT: Retain `System.Windows.Window` + `MaterialDesignThemes` v5.2.1. DO NOT switch to `MahApps.Metro`. Retain all `Theme.xaml` `StaticResource` keys.

### 3. Features 7 & 8 — Host Commands Detailing Excel Sync:
- In `HPKata.AutoCad`:
  - Wire `loadbeam`, `loadcolumn`, `loadwall` to load from `Kata.xlsm` (`Dam`, `Cot`, `Vach`) into registries.
  - Wire detailing commands `ve_dam`/`vd`, `ve_dam_shop`/`vds`, `ve_cot`/`vc`, `ve_cot_shop`/`vcs`, `ve_vach`/`vv`, `ve_vach_shop`/`vvs` to consume loaded data, set canonical layers (`kata_thep chu`, `kata_thep dai`, `kata_net thay`, etc.), and attach `KATA` XData records.
  - Wire `toexcel` and `toexcelqh` to export detailing geometry and schedule back to `Kata.xlsm` and `Iso_QHPlus.xls`.
- In `HPKata.Revit`:
  - Wire `FramingRebarCommands`, `ColumnRebarCommands`, `WallRebarCommands` to read/write `Kata.xlsm` and generate 3D native `Rebar` elements.
- Verify Clean Code rules (<= 300 lines per file, methods <= 50 lines, 0 warnings).

### 4. Build & Test Verification:
- Run `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` (must succeed with 0 warnings, 0 errors).
- Run `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` (must pass 100% of existing 1,019 tests + all new M2 tests).

Write your completion report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md` and message parent when done.


## 2026-10-18T03:36:08Z
[Invoked as Milestone M2 Implementation Worker (worker_hpkata_m2) for HPKata M2: Features 5-8]
