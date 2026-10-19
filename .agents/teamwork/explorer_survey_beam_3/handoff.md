# Handoff Report: Survey Focus Area 3 (Revit 2026 Rebar API, HelixToolkit.Wpf & ILRepack Build Setup, Excel Export)

**Author:** `explorer_survey_beam_3` (teamwork_preview_explorer)  
**Recipient:** `orchestrator_3`  
**Date:** 2026-10-08  
**Scope:** Investigation of Revit 2026 add-in build setup, HelixToolkit.Wpf compatibility, ILRepack packaging, native Revit Rebar creation infrastructure, ClosedXML report export, and Clean Code governance.

---

## 1. Observation

### 1.1 Project Structure & Build Configuration (`HPRebar.csproj`)
- In `HPRebar/HPRebar/HPRebar.csproj` (lines 1–11):
  ```xml
  <Project Sdk="Nice3point.Revit.Sdk/6.2.3">
      <PropertyGroup>
          <UseWPF>true</UseWPF>
          <DeployAddin>true</DeployAddin>
          <LaunchRevit>true</LaunchRevit>
          <IsRepackable>true</IsRepackable>
          <EnableDynamicLoading>true</EnableDynamicLoading>
          <Configurations>Debug.R23;Debug.R24;Debug.R25;Debug.R26;Debug.R27</Configurations>
          <Configurations>$(Configurations);Release.R23;Release.R24;Release.R25;Release.R26;Release.R27</Configurations>
      </PropertyGroup>
  ```
- Package references present in `HPRebar.csproj`:
  - `CommunityToolkit.Mvvm` Version 8.4.0 (line 25)
  - `MaterialDesignThemes` Version 5.3.2 (line 29)
  - `ClosedXML` Version 0.104.2 (line 40)
  - `ILRepack` Version 2.0.46 (line 37)
- Live build test (`dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false`):
  - Passed cleanly with 0 Errors, 24 Warnings.
  - ILRepack log confirmed automated merging of 19 assemblies into `HPRebar.dll`: `ClosedXML.dll`, `ClosedXML.Parser.dll`, `CommunityToolkit.Mvvm.dll`, `DocumentFormat.OpenXml.dll`, `DocumentFormat.OpenXml.Framework.dll`, `ExcelNumberFormat.dll`, `HPRebar.Core.dll`, `MaterialDesignColors.dll`, `MaterialDesignThemes.Wpf.dll`, `Microsoft.Bcl.HashCode.dll`, `Microsoft.Xaml.Behaviors.dll`, `Nice3point.Revit.Extensions.dll`, `Nice3point.Revit.Toolkit.dll`, `RBush.dll`, `Serilog.dll`, `Serilog.Sinks.Debug.dll`, `Serilog.Sinks.File.dll`, `SixLabors.Fonts.dll`, `System.IO.Packaging.dll`.

### 1.2 HelixToolkit.Wpf Compatibility Matrix
- Querying NuGet v3 registration & flat-container manifests for `HelixToolkit.Wpf`:
  - **`HelixToolkit.Wpf 3.1.2` (Latest official release)**:
    - Target Frameworks: `.NETFramework4.8`, `net6.0-windows7.0`, `net8.0-windows7.0`.
    - Dependencies: `HelixToolkit.Geometry 3.1.2`, `HelixToolkit 3.1.2`, `CommunityToolkit.Common 8.3.2`, `CommunityToolkit.Diagnostics 8.3.2`, `CommunityToolkit.Mvvm 8.3.2`, `System.IO.Compression 4.3.0`.
    - Unification: `CommunityToolkit.Mvvm` is resolved to `8.4.0` as pinned in `HPRebar.csproj`.
    - Compatibility: Directly matches `net8.0-windows7.0` (Revit 2025/2026), `.NETFramework4.8` (Revit 2023/2024), and is binary-compatible with `net10.0-windows7.0` (Revit 2027).
  - **`HelixToolkit.Wpf 2.25.0` / `2.27.3`**:
    - Targets only .NET Framework (`net45`/`net462`). Modern .NET was historically split into a separate package: `HelixToolkit.Core.Wpf`.
    - Requires conditional package references in multi-targeting projects.
  - **`HelixToolkit.Wpf.SharpDX`**:
    - Requires DirectX 11 native runtime and unmanaged C++ assemblies, which are known to cause DLL loading clashes, process crashes, and viewport initialization failures inside `Revit.exe`.
    - In contrast, `HelixToolkit.Wpf` uses pure managed WPF `Media3D` which is safe, stable, and executes without external native C++ graphics dependencies.

### 1.3 Native Revit Rebar Creation Infrastructure (`HPRebar/BeamRebar/Service/`)
- **Master Orchestrator Transaction Pattern** (`BeamRebarOrchestrator.cs` lines 58–101):
  - Opens top-level `TransactionGroup(_document, "Beam Rebar")`.
  - Staged creation: Views -> Dimensions -> Rebars -> Tables.
  - On completion: `group.Assimilate()` collapses inner transactions into a single user-facing undo action.
  - On exception: `group.RollBack()` safely reverts all changes without leaving orphaned elements.
- **Warning Suppression** (`RebarFailureHandling.cs` lines 10–35):
  - Installs `IFailuresPreprocessor` (`SwallowWarnings`) on every inner transaction.
  - Automatically deletes non-fatal warnings (`FailureSeverity.Warning`) such as rebar slightly extending outside host volume or bar overlap, logging them via Serilog and preventing Revit warning popups from interrupting generation.
- **Rebar Shape Resolution & Preflight Validation** (`RebarShapeResolver.cs` lines 16–62):
  - Validates required shapes (`M_T1`, `T1`, `M_T10`) using `FilteredElementCollector(doc).OfClass(typeof(RebarShape))` before creating any transactions.
  - Fails fast with clear user feedback if standard shape families are missing.
- **Catalog Lookup** (`RebarTypeCatalog.cs` lines 32–61):
  - Caches `RebarBarType` and `RebarCoverType`.
  - Matches bar types: (1) exact name match, (2) closest diameter within 0.5 mm tolerance, (3) fallback to closest diameter overall.
- **Rebar Creation Methods**:
  - **Shape-driven Stirrups** (`BeamStirrupCreator.cs` lines 102–115):
    ```csharp
    var rebar = Rebar.CreateFromRebarShape(doc, shape, barType, host, originXyz, xVec, yVec);
    var accessor = rebar.GetShapeDrivenAccessor();
    accessor.ScaleToBox(originXyz, xVec * RevitUnits.MmToFt(widthMm), yVec * RevitUnits.MmToFt(heightMm));
    accessor.SetLayoutAsNumberWithSpacing(count, RevitUnits.MmToFt(spacing), true, true, true);
    ```
  - **Longitudinal Bars & Multi-version Terminations** (`RebarCurveFactory.cs` lines 23–34 & `KataRebarCurveFactory.cs` lines 47–61):
    - Revit 2026 introduced `BarTerminationsData(doc)` and deprecated the 10-parameter `Rebar.CreateFromCurves`.
    ```csharp
    #if REVIT2026_OR_GREATER
        var terminations = new BarTerminationsData(doc)
        {
            HookTypeIdAtStart = hook.Id,
            HookTypeIdAtEnd = hook.Id,
            TerminationOrientationAtStart = startRight ? RebarTerminationOrientation.Right : RebarTerminationOrientation.Left,
            TerminationOrientationAtEnd = endRight ? RebarTerminationOrientation.Right : RebarTerminationOrientation.Left
        };
        var rebar = Rebar.CreateFromCurves(doc, style, barType, host, normal, curves, terminations, true, true);
    #else
        var rebar = Rebar.CreateFromCurves(
            doc, style, barType, hook, hook, host, normal, curves,
            startRight ? RebarHookOrientation.Right : RebarHookOrientation.Left,
            endRight ? RebarHookOrientation.Right : RebarHookOrientation.Left, true, true);
    #endif
    ```

### 1.4 ClosedXML Report Export Infrastructure
- `ClosedXML` 0.104.2 is already in `HPRebar.csproj` and merged into `HPRebar.dll`.
- Reference implementation in `HPExcel/HPExcel.McpBridge/Headless/ClosedXmlWorkbookService.cs` demonstrates:
  - Workbook creation: `using var wb = new XLWorkbook();`
  - Worksheet manipulation: `var ws = wb.Worksheets.Add("ThuyetMinh_TCVN5574");`
  - Cell formatting, range formulas, header coloring, and auto-fit: `worksheet.Columns().AdjustToContents();`
- `DEPENDENCY_RULES.md` Rule D1 enforces that `HPRebar.Core` references only BCL + Polyfill (0 external packages). Therefore, ClosedXML generation logic belongs in `HPRebar` (e.g., `HPRebar/BeamRebar/Service/BeamCalculationReportExporter.cs`), while the data structure (`BeamCalculationReportModel`) lives in `HPRebar.Core`.

---

## 2. Logic Chain

1. **Build & Packaging**:
   - `HPRebar.csproj` relies on `Nice3point.Revit.Sdk` with `<IsRepackable>true</IsRepackable>`.
   - Adding `<PackageReference Include="HelixToolkit.Wpf" Version="3.1.2"/>` satisfies the TFM requirements across all targets: `net8.0-windows7.0` (R25/R26), `.NETFramework4.8` (R23/R24), and `net10.0-windows7.0` (R27).
   - Because `Nice3point.Revit.Sdk` executes ILRepack post-build, `HelixToolkit.Wpf.dll` and its auxiliary assemblies (`HelixToolkit.Geometry.dll`, `HelixToolkit.dll`) are merged into `HPRebar.dll`.
   - Furthermore, `EnableDynamicLoading=true` ensures the add-in runs within an isolated `AssemblyLoadContext` (ALC) in .NET 8 (Revit 2026), preventing type-binding clashes with any other add-ins loaded in the same process.
2. **Rebar Creation Architecture**:
   - Rebar generation must be staged inside a single `TransactionGroup("Beam Rebar")` to produce exactly one undo step for the user.
   - Stirrups require shape-driven placement via `Rebar.CreateFromRebarShape` and `rebar.GetShapeDrivenAccessor().ScaleToBox(...)`.
   - Longitudinal bars (top, bottom, side) require curve-based placement. For Revit 2026 (`REVIT2026_OR_GREATER`), the API requires `BarTerminationsData` for end hooks and orientation.
   - Concrete cover offsets must be computed mathematically in `HPRebar.Core` before curves are transformed to Revit XYZ coordinates via `PointMapper` and `RevitUnits.MmToFt()`.
   - All transactions must apply `RebarFailureHandling.Apply(t)` to swallow minor geometry warnings and prevent user-facing Revit dialogs.
3. **Calculation Report Export via ClosedXML**:
   - Engineering practice under TCVN 5574:2018 requires documenting:
     a) Project metadata & structural parameters (Concrete grade $B$, Steel grades $CB400-V, CB240-T$, covers).
     b) Design forces ($M, Q$) at left support, midspan, right support.
     c) Flexural verification: $\alpha_m \le \alpha_R$, required $A_{s,req}$, provided $A_{s,prov}$, $\mu\%$, $M_{rd}$, Status (ĐẠT / NG).
     d) Shear verification: Concrete capacity $Q_b$, stirrup capacity $Q_{sw}$, $Q \le Q_b + Q_{sw}$, maximum spacing $s \le s_{max}$, Status (ĐẠT / NG).
   - Architectural boundary rule D1 strictly forbids referencing `ClosedXML` inside `HPRebar.Core`.
   - Hence, `HPRebar.Core` models the calculation results into a POCO report DTO (`BeamCalculationReportModel`), and `HPRebar` uses `ClosedXML` to construct and style the `.xlsx` workbook.
4. **Clean Code & Architectural Compliance**:
   - Pure domain models and TCVN algorithms in `HPRebar.Core` (`netstandard2.0`).
   - Revit API interactions, WPF Views, ViewModels, and ClosedXML file I/O in `HPRebar`.
   - Class size $\le 300$ lines, method size $\le 50$ lines, zero empty catches, no direct Revit API calls in ViewModels.

---

## 3. Caveats

1. **HelixToolkit XAML Resource Merging in Merged DLLs**:
   - When ILRepack merges `HelixToolkit.Wpf.dll` into `HPRebar.dll`, standard pack URIs pointing to `HelixToolkit.Wpf;component/...` could fail if Helix internally resolves resources using loose assembly names. However, standard controls like `HelixViewport3D`, `DefaultLights`, `GridLinesVisual3D`, `ViewCube`, and `TubeVisual3D` instantiate procedural meshes and WPF `Media3D` objects directly in code-behind / XAML without external loose dictionaries.
   - Using `HelixToolkit.Wpf` (WPF Media3D) is safe; `HelixToolkit.Wpf.SharpDX` must **never** be used.
2. **Rebar Shape Families in Document**:
   - `Rebar.CreateFromRebarShape` requires standard shape families (such as `M_T1` or `T1`) to be preloaded in the active Revit project template. If absent, `RebarShapeResolver` fails fast. An enhancement could fall back to `Rebar.CreateFromCurves` for closed stirrups if no standard shape is loaded.
3. **Revit 2026 BarTerminationsData**:
   - In Revit 2026, setting hooks via `BarTerminationsData` requires valid `RebarHookType` element IDs. If a beam spec requires straight ends without hooks, pass an empty `new BarTerminationsData(document)` as done in `RebarCurveFactory.CreateWithoutHooks`.

---

## 4. Conclusion

1. **Package Recommendation**:
   - Add `<PackageReference Include="HelixToolkit.Wpf" Version="3.1.2"/>` to `HPRebar/HPRebar/HPRebar.csproj`.
   - Do NOT use `HelixToolkit.Wpf.SharpDX` or split 2.x packages.
   - ILRepack configuration `<IsRepackable>true</IsRepackable>` seamlessly merges it into `HPRebar.dll`.
2. **Revit 2026 Rebar API Architecture**:
   - Reuse and integrate with existing `HPRebar/BeamRebar/Service/`:
     - `BeamRebarOrchestrator` for `TransactionGroup` management.
     - `RebarFailureHandling` for `IFailuresPreprocessor`.
     - `RebarShapeResolver` and `RebarTypeCatalog` for type resolution.
     - `RebarCurveFactory` / `KataRebarCurveFactory` with `#if REVIT2026_OR_GREATER` using `BarTerminationsData`.
3. **ClosedXML Report Layout**:
   - Implement `BeamCalculationReportExporter` in `HPRebar/BeamRebar/Service/` using `ClosedXML 0.104.2`.
   - Produce a professional 4-section report under TCVN 5574:2018: Header -> Material Properties -> Section Calculation Table ($M, Q, \alpha_m, \xi, A_{s,req}, A_{s,prov}, \mu, M_{rd}, Q_{sw}, s_{max}$, ĐẠT/NG) -> Summary & Sign-off.
4. **Architectural Purity**:
   - `HPRebar.Core` remains 100% pure BCL with zero dependencies on Revit, WPF, or ClosedXML.
   - All I/O, UI, and Revit API operations reside strictly in `HPRebar`.

---

## 5. Verification Method

1. **Verify Build**:
   ```powershell
   # In HPRebar directory:
   dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false
   ```
   *Expected result*: Build succeeded with 0 errors.

2. **Verify ILRepack Output**:
   Check output directory `HPRebar/HPRebar/bin/Debug.R26/`:
   Verify `HPRebar.dll` contains merged dependencies and no loose copies of `ClosedXML.dll` or `MaterialDesignThemes.Wpf.dll`.

3. **Verify Pure Core Tests**:
   ```powershell
   dotnet test HPRebar.Core.Tests
   ```
   *Expected result*: 100% green test suite (all 949+ tests passing).

4. **Verify Clean Code Rule Compliance**:
   Run grep checks:
   ```powershell
   # Ensure no forbidden references in Core:
   grep -rn "using Autodesk\|System.Windows\|Serilog\|ClosedXML" HPRebar/HPRebar.Core --include=*.cs
   ```
   *Expected result*: 0 matches.
