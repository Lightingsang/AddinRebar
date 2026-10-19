# DISPATCH — Milestone M6: HPKata.Revit 2026 Add-in Implementation

**Assigned Agent**: `worker_m6` (`teamwork_preview_worker`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6\`  
**Target Projects**:
- `HPKata/HPKata.Revit/HPKata.Revit.csproj` (Production Add-in)
- `HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj` (Headless Unit Tests)
- `HPKata/HPKata.slnx` (Solution Manifest)
**Date**: 2026-10-17  

---

## 1. Mission Overview
Implement **Milestone M6: HPKata.Revit 2026 Add-in** for Autodesk Revit 2026 (.NET 8).
HPKata is a dual-host structural rebar detailing, dynamic tag-linking, scheduling, and 1D cutting-stock optimization system.
Following the successful completion of Milestones M1–M5 (`HPKata.Core`, `HPKata.UI`, and `HPKata.AutoCad`), `HPKata.Revit` integrates the suite into Revit 2026 while providing complete headless unit testability.

---

## 2. Mandatory Inputs & References
You MUST thoroughly read these authoritative specifications before writing any code:
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Sections 1, 4, 7, 8, 9, 10)
3. `docs/clean-code/HP_CLEAN_CODE_CORE.md`
4. `docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md` (Rules D1, D4, K6, R1–R13, P1–P4)
5. Explorer Reports:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_1\handoff.md` (and `analysis.md`)
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_2\handoff.md` (and `analysis.md`)
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_3\handoff.md` (and `analysis.md`)

---

## 3. Strict Resource Directive & Tool Directives (MANDATORY)
1. **Compilation & Test Bounds**:
   - Every `dotnet build` or `dotnet test` MUST include `--disable-build-servers -m:1`.
   - Never spawn background build servers or multi-threaded compiler storms (`-m:1` is strictly enforced).
2. **MTP Concurrency & Result Directory Isolation**:
   - For all test executions, you MUST pass `--results-directory "$env:TEMP\mtp-worker6-$([guid]::NewGuid())"` to prevent file locking collisions on `TestResults\*.log`.
3. **Clean-Room DRM Isolation**:
   - `C:\kata_pro` is strictly **read-only** (0 modifications).
   - Zero inspection, decompilation, copying, or circumvention of `Security.dll`, hardware dongles, or licensing checks.

---

## 4. Implementation Requirements

### 4.1 Project Configuration & Solution Registration
1. **`HPKata/HPKata.Revit/HPKata.Revit.csproj`**:
   - Target framework: `net8.0-windows`
   - Properties: `<UseWPF>true</UseWPF>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<EnableDynamicLoading>true</EnableDynamicLoading>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
   - References:
     - `Nice3point.Revit.Api.RevitAPI` (Version `2026.0.4`, `PrivateAssets="all"`, `ExcludeAssets="runtime"`)
     - `Nice3point.Revit.Api.RevitAPIUI` (Version `2026.0.4`, `PrivateAssets="all"`, `ExcludeAssets="runtime"`)
     - `CommunityToolkit.Mvvm` (8.4.0)
     - `MaterialDesignThemes` (5.2.1)
     - `ClosedXML` (0.104.2)
     - Project references to `..\HPKata.Core\HPKata.Core.csproj` and `..\HPKata.UI\HPKata.UI.csproj`.
     - InternalsVisibleTo: `HPKata.Revit.Tests`.
2. **`HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj`**:
   - Target framework: `net8.0-windows`
   - Use xUnit v3 (`xunit.v3`, `Microsoft.Testing.Platform.MSBuild`, `xunit.runner.visualstudio`).
   - Project references to `..\HPKata.Revit\HPKata.Revit.csproj`, `..\HPKata.Core\HPKata.Core.csproj`, `..\HPKata.UI\HPKata.UI.csproj`.
3. **`HPKata/HPKata.slnx`**:
   - Register both `HPKata.Revit/HPKata.Revit.csproj` and `HPKata.Revit.Tests/HPKata.Revit.Tests.csproj`.
4. **`HPKata/HPKata.Revit/HPKata.Revit.addin`**:
   - Add-in manifest pointing to `HPKata.Revit.dll`, `HPKata.Revit.Entry.HPKataApplication`, AddInId `C184E347-734B-4811-9A77-9E49B7423D19`, VendorId `HPRB`.

### 4.2 Decoupled Domain & Interop Services (Headless Testable)
To allow running comprehensive unit tests without an active `Revit.exe` process:
1. **`Units/RevitUnitConverter.cs` (Rule R5 Quarantine)**:
   - Encapsulate all $1\text{ ft} = 304.8\text{ mm}$ and $1\text{ mm} = 1.0 / 304.8\text{ ft}$.
   - Provide `FeetToMm`, `MmToFeet`, vector & point converters, and tolerance comparison helpers (`AreCloseMm`, `AreCloseFeet`).
   - ZERO literal `304.8` factors anywhere else in the entire codebase!
2. **`Abstractions/IRevitDocumentContext.cs` & DTOs**:
   - `IRevitDocumentContext`: `DocumentTitle`, `DocumentPath`, `ActiveViewName`, `GetAllRebars()`, `GetSelectedRebars()`, `WriteNotification(msg)`, `ShowError(msg)`, `CreateOrUpdateScheduleTable(dto)`.
   - `RevitRebarDto`: ElementId, Partition, Mark, Quantity, DiameterMm, SingleBarLengthMm, ShapeName, ParametersMm, Comments, IsFreeform.
   - `RevitScheduleTableDto`: Schedule headers, rows, totals, bins.
3. **`Mocks/MockRevitDocumentContext.cs`**:
   - In-memory mock context for `HPKata.Revit.Tests`, tracking notifications, rebar collections, and schedule tables.
4. **`Bridge/RevitParameterBridge.cs`**:
   - Two-way mapping between `RevitRebarDto` / Revit `Rebar` and `HPKata.Core.Models.RebarItem` / `KataRebarSpec`.
   - `REBAR_PARTITION` <-> `ComponentMark` (`{ten_ck}`).
   - `REBAR_NUMBER` / `ALL_MODEL_MARK` <-> `BarMark` (`{stt}`).
   - `Quantity` <-> `CountPerMember` (`{sl}`).
   - `BarNominalDiameter` (feet to mm) <-> `DiameterMm` (`{phi}`).
   - `REBAR_ELEM_LENGTH` (single-bar length, feet to mm) <-> `CuttingLengthMm` (`{L}`). CRITICAL: NEVER use `rebar.TotalLength` as `CuttingLengthMm`!
5. **`Bridge/RevitShapeAdapter.cs` & `RevitShapeMappingTable.cs`**:
   - Comprehensive two-way mapping between Revit standard shapes (`M_00`..`M_77`, `M_T1`, `M_T10`) and Kata 85 shapes (`dang01`..`dang67`).
   - Disambiguate symmetric U-bar (`dang02`) vs asymmetric U-bar (`dang04`) via $|A - C| < 1.0\text{ mm}$.
   - Extract dimensions $A, B, C, D, E, F$ into mm parameter array.
6. **`Bridge/RevitCurveAnalyzer.cs`**:
   - Geometric fallback engine evaluating centerline curves for Freeform rebars or unmapped shapes.
7. **`Runners/RevitScheduleRunner.cs`**:
   - Pure runner: extracts rebars from `IRevitDocumentContext`, maps to `RebarItem`, invokes `RebarScheduleAggregator.GenerateReport`, formats `RevitScheduleTableDto`, and notifies context.
8. **`Runners/RevitCuttingStockRunner.cs`**:
   - Pure runner: extracts rebars with $\Phi \ge 10\text{ mm}$, groups by diameter, runs `CuttingStockEngine.Solve` for $11,700\text{ mm}$ stock bars, builds cutting pattern summary.
9. **`Runners/RevitExportExcelRunner.cs`**:
   - Pure runner: generates schedule report, resolves path from document, and calls `ClosedXmlExporter.ExportScheduleToFile`.

### 4.3 Live Revit 2026 Host Integration (Production Entry & UI)
1. **`Entry/HPKataApplication.cs` (`IExternalApplication`)**:
   - `OnStartup`: Creates Ribbon Tab `"HPKATA"` and Panel `"Cốt Thép (Rebar)"` via `RibbonBuilder`. Subscribes to `UIControlledApplication.ThemeChanged` for dynamic Dark/Light restyling.
   - `OnShutdown`: Unsubscribes events and cleans up resources.
2. **`Entry/RibbonBuilder.cs` & `RibbonIconFactory.cs`**:
   - Creates 5 push buttons:
     1. `"Thiết Lập"` -> `HPKata.Revit.Commands.SetupCommand`
     2. `"Vẽ & Sửa Thép"` -> `HPKata.Revit.Commands.DetailingCommand`
     3. `"Thống Kê Thép"` -> `HPKata.Revit.Commands.ScheduleCommand`
     4. `"Tổ Hợp Cắt Thép"` -> `HPKata.Revit.Commands.CuttingStockCommand`
     5. `"Xuất Excel"` -> `HPKata.Revit.Commands.ExportExcelCommand`
   - Dynamic vector `DrawingImage` icons that adapt to Dark and Light themes.
3. **`Entry/RevitHostTheme.cs`**:
   - Implements `IHostTheme` wrapping `Autodesk.Revit.UI.UIThemeManager.CurrentTheme == UITheme.Dark`.
4. **`Interop/RevitWindowHelper.cs`**:
   - Safe hosting: `new WindowInteropHelper(window).Owner = uiApp.MainWindowHandle` and `ThemeManager.Attach(window, RevitHostTheme.Instance)`.
5. **Commands (`HPKata.Revit.Commands`)**:
   - `SetupCommand`: Opens `SetupView` with `SetupViewModel` loaded from `KataConfig`.
   - `DetailingCommand`: Opens `DetailingView` with `DetailingViewModel`.
   - `ScheduleCommand`: Opens `ScheduleView` with `ScheduleViewModel` fed by `RevitScheduleRunner`.
   - `CuttingStockCommand`: Opens `CuttingStockView` with `CuttingStockViewModel` fed by `RevitCuttingStockRunner`.
   - `ExportExcelCommand`: Invokes `RevitExportExcelRunner`.
   - In live mode, wraps `UIApplication` / `Document` into `RevitDocumentContext : IRevitDocumentContext` and delegates to the pure runners.
6. **`NativeRebar/RebarCreationService.cs` & `KataRebarCurveFactory.cs`**:
   - Native 3D rebar generation using Revit 2026's modern `BarTerminationsData(doc)` API for curve-based placement.

### 4.4 Comprehensive Headless Unit Test Suite (`HPKata.Revit.Tests`)
Implement at least **30 unit tests** (target: 33 tests) that run 100% headlessly in `dotnet test`:
1. `RevitUnitConverterTests` ($\ge 6$ tests): Feet <-> mm conversions, vector conversions, tolerances, array conversions.
2. `RevitShapeAdapterTests` ($\ge 6$ tests): Standard shapes (`M_00`, `M_02`, `M_11`, `M_T1`, `M_77`) to Kata shapes, symmetric vs asymmetric U-bars, dimension extraction.
3. `RevitParameterBridgeTests` ($\ge 6$ tests): Bidirectional parameter mapping, length integrity (single bar vs total length), diameter, partition, mark.
4. `RevitScheduleRunnerTests` ($\ge 6$ tests): Schedule generation from mock rebars, diameter aggregation, TCVN bins, selection filtering.
5. `RevitCuttingStockRunnerTests` ($\ge 5$ tests): 1D-CSP solver invocation, $\Phi \ge 10\text{ mm}$ filter, scrap computation, cutting pattern building.
6. `RevitExportExcelRunnerTests` ($\ge 4$ tests): ClosedXML spreadsheet export, path resolution, valid output file verification.
7. `RevitHostThemeTests` ($\ge 3$ tests): Theme state property and event notification.

---

## 5. Clean Code Governance (MANDATORY)
1. **Rule C2**: Every C# file MUST be $\le 300$ physical lines.
2. **Rule M3**: Every method body MUST be $\le 50$ physical lines.
3. **Rule Q-B1**: Zero commented-out code.
4. **Rule Q-B2**: Zero unannotated empty catch blocks.
5. **Rule R5**: Zero unit conversion factors (`304.8`) outside `RevitUnitConverter.cs`.
6. **TreatWarningsAsErrors**: Build MUST succeed with 0 Warnings and 0 Errors.

---

## 6. Verification Steps for Worker
1. Build solution:
   ```bash
   dotnet build HPKata/HPKata.slnx -c Release --disable-build-servers -m:1
   ```
2. Run entire solution test suite (including Core, UI, AutoCad, and Revit tests):
   ```bash
   dotnet test HPKata/HPKata.slnx -c Release --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-worker6-$([guid]::NewGuid())"
   ```
   Verify all tests pass (expected: $\ge 260$ tests, 100% green, 0 failed, 0 skipped).
3. Verify clean code line limits using AST tool or script:
   Ensure all classes $\le 300$ lines and all methods $\le 50$ lines.
4. Deliver `handoff.md` in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6\handoff.md`.

---

## 7. Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A `teamwork_preview_auditor` will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.


## 2026-10-17T04:47:24Z
[Message] sender=4300a9ce-e3eb-419c-8493-f2001d2d04d6 priority=MESSAGE_PRIORITY_HIGH
Worker M6 dispatched for Revit 2026 Add-in Implementation for Milestone M6 of HPKata.
Strict directive: `--disable-build-servers -m:1`, unique `--results-directory "$env:TEMP\mtp-worker6-$([guid]::NewGuid())"`.
Build production-ready HPKata.Revit and HPKata.Revit.Tests in HPKata/HPKata.slnx.
Target: 0 warnings, 0 errors, >=30 headless tests passing.
