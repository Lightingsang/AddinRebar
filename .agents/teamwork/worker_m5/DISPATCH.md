# Dispatch: Worker M5 (HPKata.AutoCad 2026 Add-in & Lisp Loader Implementation)

**Agent**: `worker_m5` (`teamwork_preview_worker`)
**Parent**: `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5\`
**Date**: 2026-10-17

## Mandatory Reading
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Sections 1, 2, 3, 4, 5, 7, 8, 10)
3. Explorer Reports:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_1\handoff.md` (AutoCAD .NET 8 architecture, lifecycle, WPF hosting)
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_2\handoff.md` (Polyline fillet bulges, XData 12-item stream, handle linking)
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_3\handoff.md` (Lisp loader 238 lines, scheduling runners, headless tests)
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
5. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`

## Mission
Build the production-ready `HPKata.AutoCad` add-in and `HPKata.AutoCad.Tests` suite in `HPKata/HPKata.slnx`:

1. **Project Setup**:
   - Create `HPKata/HPKata.AutoCad/HPKata.AutoCad.csproj` targeting `net8.0-windows` with `AutoCAD.NET 25.1.0` (`ExcludeAssets="runtime" PrivateAssets="all"`), `EnableDynamicLoading=true`, referencing `HPKata.Core` and `HPKata.UI`.
   - Create `HPKata/HPKata.AutoCad.Tests/HPKata.AutoCad.Tests.csproj` targeting `net8.0-windows`, referencing `HPKata.AutoCad`, `HPKata.Core`, `HPKata.UI`, with `xunit.v3` and MTP test runner.
   - Register both projects in `HPKata/HPKata.slnx`.

2. **Core AutoCAD Interop & Services** (in `HPKata/HPKata.AutoCad/Interop/`):
   - `XDataService.cs`: Converts between `KataXDataRecord` and AutoCAD `ResultBuffer` / `TypedValue` with DxfCodes 1001, 1000, 1070, 1005.
   - `FilletArcCalculator.cs`: Pure math helper computing tangent distances and vertex bulges ($b = \pm \tan(\theta/4)$) for rebar corners.
   - `RebarGeometryService.cs`: Generates 2D lightweight `Polyline` entities from `RebarShapeDefinition` and dimensions with layer and thickness assignment.
   - `BlockAttributeService.cs`: Reads/updates block attributes for `kata_block_KHT`, `thep`, `Kata_Block_Beam_Rebar`, `dangCoupler`.
   - `HandleLinkService.cs`: Manages bidirectional handle linking (`link`), clone re-binding (`cl`), and link severing (`dl`).
   - `RebarUpdateService.cs`: Reads modified polyline geometry, recalculates cutting lengths, and updates linked block attributes (`ud`).
   - `CadSelectionService.cs`: Safe user selection prompts (`Editor.GetSelection`, `Editor.GetEntity`, `Editor.GetPoint`).
   - `CadHighlightService.cs`: Highlighting rebar entities and tags (`tim`, `xoa`).

3. **Lifecycle & Commands** (in `HPKata/HPKata.AutoCad/Commands/`):
   - `HPKataExtensionApplication.cs`: Implements `IExtensionApplication`, auto-registers `"KATA"` in `RegAppTableId`, hooks document activation/creation.
   - `AutocadHostTheme.cs`: Implements `IHostTheme` reading AutoCAD `"COLORTHEME"`.
   - `SetupCommands.cs` & `SetupController.cs`: `setupkata`, `setupthep` (opens `SetupView`).
   - `RebarCommands.cs`, `RebarDrawController.cs`, `RebarEditController.cs`: `vt`, `taothep`, `nhan`, `sua` (opens `DetailingView`), `smd`, `dai90`, `dai135`, `dai180`, `dai90135`, `neo`, `cut`, `cattuyy`, `dimthep`, `dmt`.
   - `LinkCommands.cs` & `HandleLinkController.cs`: `link`, `cl`, `dl`, `tim`, `xoa`, `ud`, `gt`, `stt0`, `stt1`, `timthep`, `check_lap`.
   - `ScheduleCommands.cs` & Runners: `tkt` (opens `ScheduleView`), `suatk`, `sstk`, `ththep`, `tkco`, `tohopthep` (opens `CuttingStockView`), `toexcel`, `toexcelqh`, `itt`.
   - `UtilityCommands.cs`: `pukata`, `hideatt`, `unhideatt`.

4. **AutoLISP Loader (`HPKata/HPKata.AutoCad/GoiLenh_HPKata.lsp`)**:
   - Exactly 238 lines.
   - Lines 1–151: Version checks, `.NETLOAD` of `HPKata.AutoCad.dll`, menu loading, and all 118 command aliases.
   - Line 152: `;kata_dim` (start of 86 configuration lines).
   - Lines 152–237: The exact 86 configuration parameters matching KataPro defaults.
   - 100% round-trip idempotence with `KataConfigParser`.

5. **Headless Unit Test Suite (`HPKata/HPKata.AutoCad.Tests/`)**:
   - `GoiLenhLspSyntaxTests.cs`: Tests `GoiLenh_HPKata.lsp` line count (238), alias count (118), line 152 start, line 237 end, and parser round-trip.
   - `XDataServiceTests.cs`: Tests conversion between `KataXDataRecord` and `TypedValue` streams.
   - `FilletArcCalculatorTests.cs`: Tests bulge math for 45°, 90°, 135°, 180° turns and tangent clipping.
   - `HandleLinkingTests.cs`: Tests handle link creation, clone re-binding, and update calculations using mock/DTO models.
   - `ScheduleCommandTests.cs`: Tests `tkt`, `ththep`, `tkco`, `tohopthep`, `toexcel`, `itt` runner logic with `MockCadDocumentContext`.

6. **Clean Code & Governance Standards**:
   - Rule C2: EVERY file $\le 300$ lines.
   - Rule M3: EVERY method body $\le 50$ lines.
   - Rule Q-B1: ZERO dead, commented-out, or obsolete code.
   - Rule Q-B2: ZERO unannotated catch blocks.
   - Clean-Room: `C:\kata_pro` is strictly read-only. ZERO references to `Security.dll` or DRM licensing.

7. **Verification**:
   - Build: `dotnet build HPKata/HPKata.slnx -c Release --disable-build-servers -m:1`
   - Test: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-test-$([guid]::NewGuid())"`
   - AST / Line Check: Verify 0 files $> 300$ lines, 0 methods $> 50$ lines.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.
