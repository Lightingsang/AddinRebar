# Forensic Audit Report: Milestone M1 (Smart Plot Pro)

**Work Product**: `HPAutoCad.Core/SmartPlot/` and `HPAutoCad.Tests/SmartPlot/`  
**Author**: Forensic Auditor (`auditor_m1`)  
**Parent**: `parent` (`5a9f631f-8834-48f7-a8a5-72b226a4b480`)  
**Profile**: General Project (Demo Mode)  
**Date**: 2026-09-20T22:42:00Z  
**Verdict**: **`CLEAN`**

---

## Forensic Audit Summary

### Phase Results
- **Hardcoded test results**: **PASS** — Zero hardcoded constants, expected output literals, or fake branch short-circuits found in domain logic.
- **Facade implementations**: **PASS** — All models, services, and methods are fully implemented with real algorithms. Zero `NotImplementedException`, zero empty stubs.
- **Pre-populated artifacts**: **PASS** — Zero pre-populated test logs, mock files, or attestation artifacts in the workspace.
- **Self-certifying tests**: **PASS** — Unit tests evaluate real mathematical properties, shuffled permutations (3x3 grid), tolerance bands, inverted ranges, and JSON roundtrips against independent invariants.
- **Zero host dependencies**: **PASS** — `HPAutoCad.Core` references 0 external CAD/UI packages; binary reflection confirms exactly 14 native .NET 8 BCL `System.*` assemblies.
- **Build execution**: **PASS** — `dotnet build HPAutoCad/HPAutoCad.slnx` compiles cleanly in both `Debug` and `Release` configurations with 0 errors.
- **Test execution**: **PASS** — 100% of Worker M1's delivered unit tests (62/62) pass cleanly. Solution-wide regression suites remain fully intact.

---

## 1. Observation

### 1.1 Source Code Inventory in `HPAutoCad.Core/SmartPlot/`
Audited all 16 C# source files:
- `Models/FrameSourceType.cs`: Enum (`Block`, `Layer`, `Layout`).
- `Models/OrientationMode.cs`: Enum (`Auto`, `Portrait`, `Landscape`).
- `Models/OutputMode.cs`: Enum (`SingleFiles`, `MergedPdf`).
- `Models/PlotBounds.cs`: `readonly record struct PlotBounds(double MinX, double MinY, double MaxX, double MaxY)` with computed geometric properties (`Width`, `Height`, `CenterX`, `CenterY`, `IsLandscape`, `IsValid`), `VerticalOverlap(other)`, and `OverlapsVertically(other, ratioThreshold)`.
- `Models/PlotItem.cs`: Sealed record with bounds delegation, source handle, layout, order, rotation, selection state, and titleblock metadata.
- `Models/PlotConfiguration.cs`: Sealed record with device, media, plot style, orientation, layout range, and tolerance band ratio.
- `Models/PlotPreset.cs` & `Models/PlotPresetCollection.cs`: Document records for JSON serialization.
- `Models/PlotResult.cs`: Sealed record with `Succeeded` and `Failed` factory methods.
- `Services/IPlotOrderService.cs` & `Services/PlotOrderService.cs`: Spatial ordering service grouping frames into visual rows using vertical overlap ratios and sorting rows Top-to-Bottom, items Left-to-Right.
- `Services/LayoutRangeParser.cs`: Zero-exception parser for sheet ranges with bounds checking and a `HardCap = 10000` memory guard.
- `Services/IFileNameService.cs` & `Services/FileNameService.cs`: File name formatter with token substitution (`{Prefix}`, `{Layout}`, `{SheetNo}`, `{Title}`, `{Order}`, `{DwgName}`, `{Date}`), Windows reserved device name prefixing, and consecutive underscore collapsing.
- `Services/IPresetService.cs` & `Services/PresetService.cs`: JSON persistence at `%AppData%\HPAutoCad\SmartPlot\presets.json` using `System.Text.Json` and atomic `.tmp` file move pattern.

### 1.2 Binary Dependency & Reflection Audit
- **Project File**: `HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj`
  - Targets `net8.0`
  - `<PackageReference>` elements: **0**
  - `<ProjectReference>` elements: **0**
- **Text Search**: `grep -rn "Autodesk"` in `HPAutoCad/HPAutoCad.Core` -> **0 matches**.
- **Binary Reflection via Assembly Inspection**:
  Command:
  ```powershell
  [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes('HPAutoCad/HPAutoCad.Core/bin/Debug/net8.0/HPAutoCad.Core.dll')).GetReferencedAssemblies() | Select-Object -ExpandProperty FullName
  ```
  Result:
  ```
  System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.Text.RegularExpressions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.Collections, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.Linq, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.Text.Json, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
  System.IO.Compression, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
  System.Xml.XDocument, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.Net.Http, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.Threading, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.Net.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.Memory, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
  System.Runtime.InteropServices, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.Text.Encoding.Extensions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  System.IO.Compression.ZipFile, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
  ```
  **0 external or Autodesk assemblies**. Strictly 14 standard .NET 8 BCL assemblies.

### 1.3 Independent Build Execution
- **Debug Configuration**:
  ```powershell
  dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
  ```
  Result: Exit code `0`, `0 Warning(s)`, `0 Error(s)` in Core & Tests (1 legacy ILRepack swatch warning in HPAutoCad.csproj).
- **Release Configuration**:
  ```powershell
  dotnet build HPAutoCad/HPAutoCad.slnx -c Release
  ```
  Result: Exit code `0`, `0 Warning(s)`, `0 Error(s)` in Core & Tests.

### 1.4 Independent Test Suite Execution
- **Worker M1 Test Classes in `HPAutoCad.Tests/SmartPlot/`**:
  1. `PlotBoundsAndModelTests`: 9 tests, **9 passed**, 0 failed.
  2. `PlotOrderServiceTests`: 9 tests, **9 passed**, 0 failed.
  3. `LayoutRangeParserTests`: 21 tests, **21 passed**, 0 failed.
  4. `FileNameServiceTests`: 15 tests, **15 passed**, 0 failed.
  5. `PresetServiceTests`: 8 tests, **8 passed**, 0 failed.
  **Subtotal: 62 passed, 0 failed, 0 skipped (100% pass rate)**.
- **Challenger 1 Stress Tests (`PlotOrderAdversarialStressTests`)**:
  - 18 tests, **18 passed**, 0 failed (100% pass rate on adversarial spatial ordering).
- **Solution Regression Suites**:
  - `HPAutoCad.Tests` (HPGeoLink): 238 passed, 3 skipped (gated on `HPGEO_LIVE_TILES=1`), 0 failed.
  - `HPAutoCad.Aec.Tests`: 225 passed, 0 failed, 0 skipped.
  - `HPAutoCad.Mcp.Server.Tests`: 280 passed, 0 failed, 0 skipped.
  - `HPCivil3d.McpBridge.Tests` (Civil 3D mirror invariant): 60 passed, 0 failed, 0 skipped.

---

## 2. Logic Chain

1. **Host Decoupling**:
   - `ORIGINAL_REQUEST.md` (R1) requires a host-free logic layer with zero AutoCAD references.
   - Observation 1.2 empirically verified that neither the `.csproj` file, the source code, nor the emitted DLL references any CAD, Revit, or third-party UI assembly.
   - Conclusion: The deliverable satisfies domain isolation.

2. **Genuine Implementation vs. Facades**:
   - Every class in `HPAutoCad.Core/SmartPlot/` was reviewed line by line.
   - `PlotOrderService` performs genuine cluster analysis by evaluating relative vertical overlap ratios ($\frac{\text{overlap}}{\min(H_1, H_2)} \ge \text{threshold}$) and updating dynamic row envelopes.
   - `LayoutRangeParser` handles commas, semicolons, dashes, inverted bounds ("5-1"), and garbage characters using `int.TryParse` without throwing exceptions.
   - `FileNameService` evaluates templates dynamically against item properties, strips invalid Windows characters, prefixes DOS reserved device names, and collapses consecutive underscores.
   - `PresetService` handles JSON serialization, disk reads/writes, atomic file replacement, and corrupted JSON fallbacks.
   - Conclusion: No facade, stub, or dummy implementations exist.

3. **Absence of Hardcoded Cheating**:
   - In `PlotOrderServiceTests`, the 3x3 grid test feeds a shuffled list of 9 items (`R3C2`, `R1C1`, `R2C3`, `R1C3`, `R3C1`, `R2C1`, `R1C2`, `R3C3`, `R2C2`) and asserts exact spatial sorting. The algorithm sorts these mathematically based on coordinates, not hardcoded ID matching.
   - In `PresetServiceTests`, tests use dynamic GUID-based temporary directories and fresh instances to verify real disk I/O and JSON deserialization.
   - Conclusion: Zero hardcoded outputs or test-specific shortcuts exist.

4. **Adversarial Resilience**:
   - `PlotOrderAdversarialStressTests` validated 18 adversarial scenarios (collinear frames, extreme aspect ratios, overlapping frames, diagonal steps, tolerance clamping) with 100% success.
   - `LayoutRangeParser` survived integer overflow attempts (`2147483648`), 100,000-character delimiter noise, and negative indices without crashing or memory exhaustion.

---

## 3. Caveats

1. **Downstream CAD Plotting & PDF Merging**:
   - AutoCAD entity extraction (`BlockReference`, `Polyline`, `Layout` iteration) and plotting execution via `PlotEngine` belong to Milestone M2 (`HPAutoCad/SmartPlot/Cad/`).
   - PDF merging via `PdfSharp` belongs to Milestone M2 (`HPAutoCad/SmartPlot/Pdf/`).
   - The boundary between host-free domain logic (M1) and host-dependent CAD/PDF operations (M2) has been strictly respected.
2. **Pre-existing Skipped Tests**:
   - 3 tests in `HPAutoCad.Tests.HPGeoLink` remain skipped because they require `HPGEO_LIVE_TILES=1` to hit live Esri tile servers. This is expected behavior.

---

## 4. Conclusion

**Verdict: `CLEAN`**

The work product delivered for Milestone M1 (`HPAutoCad.Core/SmartPlot/` and `HPAutoCad.Tests/SmartPlot/`) fully satisfies all integrity, architectural, and quality standards:
- Zero integrity violations detected.
- Zero references to `Autodesk.*` or external UI/CAD libraries.
- All algorithms are genuine, robust, and mathematically sound.
- 100% of Worker M1 unit tests pass under independent execution.
- Approved for downstream integration into Milestone M2.

---

## 5. Verification Method

To independently reproduce this forensic audit:

1. **Verify Solution Build**:
   ```powershell
   dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
   dotnet build HPAutoCad/HPAutoCad.slnx -c Release
   ```
   Both must succeed with `0 Error(s)`.

2. **Verify Zero Host References**:
   ```powershell
   powershell -NoProfile -Command "[System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes('HPAutoCad/HPAutoCad.Core/bin/Debug/net8.0/HPAutoCad.Core.dll')).GetReferencedAssemblies() | Select-Object -ExpandProperty FullName"
   ```
   Must list strictly `System.*` assemblies with 0 `Autodesk.*` references.

3. **Execute SmartPlot Unit Tests**:
   ```powershell
   dotnet run --project HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj --no-build -- --filter-class "*PlotBoundsAndModelTests*" --filter-class "*PlotOrderServiceTests*" --filter-class "*LayoutRangeParserTests*" --filter-class "*FileNameServiceTests*" --filter-class "*PresetServiceTests*"
   ```
   Must pass all 62 tests.

4. **Verify Regression Baseline**:
   ```powershell
   dotnet run --project HPAutoCad/HPAutoCad.Aec.Tests/HPAutoCad.Aec.Tests.csproj
   dotnet run --project HPAutoCad/HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj
   dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj
   ```
   All must pass with 0 failures.
