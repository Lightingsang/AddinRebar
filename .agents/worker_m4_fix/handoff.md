# Handoff Report — Milestone M4: Live Verification & Loader Fix

## 1. Observation
- **Reflection Lookup Failure**:
  In `HPAutoCad/HPAutoCad.Loader/HPAutoCadLoaderApplication.cs` line 61, the invocation:
  ```csharp
  var start = entry.GetMethod(EntryMethodName, BindingFlags.Public | BindingFlags.Static)
              ?? throw new MissingMethodException(EntryTypeName, EntryMethodName);
  ```
  threw `System.Reflection.AmbiguousMatchException: Ambiguous match found.` when attempting to load `HPAutoCad.Entry` from `HPAutoCad.dll`. `HPAutoCad.Entry` exposes two public static `Start` methods:
  - `public static IDisposable Start(string appDir, Action<string> log)` (2 parameters)
  - `public static IDisposable Start(string appDir, string product, string acadVersion)` (3 parameters)
- **Autodesk Autoloader XML Schema Requirement**:
  In `HPAutoCad/HPAutoCad.Loader/Bundle/PackageContents.xml`, enclosing two `<ComponentEntry>` tags inside a single `<Components>` block caused AutoCAD 2026 autoloader to reject the bundle silently upon launch.
- **Unit Test Execution Results**:
  - Command: `dotnet test HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj`
    - Result: `Passed! - Failed: 0, Passed: 162, Skipped: 3, Total: 165, Duration: 7.8s`
    - 3 skipped tests are live tile prefetch integration tests requiring network environment flag `HPGEO_LIVE_TILES=1`.
  - Command: `dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj`
    - Result: `Passed! - Failed: 0, Passed: 60, Skipped: 0, Total: 60, Duration: 1.4s` (Civil 3D mirror invariants preserved).
  - Command: `dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj`
    - Result: `Passed! - Failed: 0, Passed: 280, Skipped: 0, Total: 280, Duration: 1.6s`
- **Regression Suite (`run-bridge-unattended.ps1`)**:
  - Command: `powershell -ExecutionPolicy Bypass -File HPAutoCad/tools/harness/run-bridge-unattended.ps1`
  - Result: 21/21 passed (opt-in OFF -32001, 18 main scenarios, nodoc -32003, busy -32002).
  - Pipe `\\.\pipe\hpautocad-mcp-2026` operational, execution latency ~30-50ms.
- **Live Verification Suite (`run-geolink-verify.ps1`) in AutoCAD 2026**:
  - Command: `powershell -ExecutionPolicy Bypass -File HPAutoCad/tools/harness/run-geolink-verify.ps1`
  - Output summary (`HPAutoCad/output/geolink-verify/summary.json`):
    - Total Checks: 45
    - Passed: 45
    - Failed: 0
    - Tier 1 (Feature Coverage): 15/15 Passed
    - Tier 2 (Boundary Cases): 11/11 Passed
    - Tier 3 (Cross-Feature Combinations): 8/8 Passed
    - Tier 4 (Cadastral & System Health): 11/11 Passed
    - Evidence Artifacts Generated:
      - `HPAutoCad/output/geolink-verify/ribbon-tab.png`
      - `HPAutoCad/output/geolink-verify/ribbon-tab-theme1.png`
      - `HPAutoCad/output/geolink-verify/dialog-dark.png`
      - `HPAutoCad/output/geolink-verify/dialog-light.png`
      - `HPAutoCad/output/geolink-verify/dialog-import.png`
      - `HPAutoCad/output/geolink-verify/image-in-autocad.png`
      - `HPAutoCad/output/geolink-verify/summary.json`

---

## 2. Logic Chain
1. **Reflection Disambiguation**:
   Because `HPAutoCad.Entry` provides both a 2-parameter overload (`string appDir, Action<string> log`) and a 3-parameter overload (`string appDir, string product, string acadVersion`), calling `Type.GetMethod(string, BindingFlags)` without specifying parameter types caused CLR reflection to fail with `AmbiguousMatchException`. By filtering `entry.GetMethods(BindingFlags.Public | BindingFlags.Static)` specifically for method name `"Start"` with parameter length 2, the loader cleanly selects the primary logging-enabled entry point without throwing.
2. **Autoloader Schema Compliance**:
   Autodesk's `ApplicationPackage` XML schema validates that each `<Components>` element defines a single runtime component unit. Splitting the declarations into two distinct `<Components>` elements (one for `HPAutoCad.McpBridge` pointing to `HPAutoCad.McpBridge.Loader.dll` and one for `HPAutoCad` pointing to `HPAutoCad.Loader.dll`) satisfies the schema, allowing AutoCAD 2026 to automatically discover and initialize both loaders during startup.
3. **Bridge Window Isolation in Live Harness**:
   During live testing, closing the modeless bridge dialog previously targeted the root window returned for AutoCAD PID, which unexpectedly closed AutoCAD's main application window (`AfxMDIFrame140u`). Isolating the window lookup by checking for the substring `"Bridge"` in the window title ensured that only the modeless dialog was dismissed, preserving AutoCAD's process state for the remainder of the test script.
4. **Verification of GeoLink & MCP Bridge Coexistence**:
   The live execution in AutoCAD 2026 confirmed that:
   - The shared Ribbon tab `HPAUTOCAD_MCP_TAB` was loaded exactly once and contained both panels (`HPAUTOCAD_MCP_PANEL` and `HPGEOLINK_PANEL`).
   - Workspace and theme switching (`COLORTHEME` 0 <-> 1) operated smoothly without duplicating ribbon tabs or crashing WPF UI elements.
   - All 7 registered AutoCAD commands (`HPGEO`, `-HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `HPGEOIMAGE`, `-HPGEOIMAGE`, `HPGEOINFO`) executed correctly across empty drawing guards, coordinate conversion, boundary flattening, out-of-process tile fetching (`HPAutoCad.TileFetch.exe`), KMZ import/export, undo stack rollback (`_.U`), and Named Object Dictionary (`NOD`) state persistence.
   - Named pipe `\\.\pipe\hpautocad-mcp-2026` remained active and responsive throughout the session.

---

## 3. Caveats
- Three integration tests in `HPAutoCad.Tests` (`PrefetchVisibleTiles_WithDirectService_DownloadsAllTiles`, `DownloadMissingAsync_WithMultipleTiles_AllDownloaded`, and `DownloadMissingAsync_ReturnsOnlyDownloadedFiles`) are marked `[Fact(Skip = "...")]` because they test direct network tile retrieval from public tile servers and require setting the environment variable `HPGEO_LIVE_TILES=1`. In the live AutoCAD harness, tile retrieval was tested and passed via `HPAutoCad.TileFetch.exe` (Assertion `T4-CAD-02: PASS`).
- No other caveats; all live assertions passed with 0 errors.

---

## 4. Conclusion
Milestone M4 implementation and verification is complete. The reflection defect in `HPAutoCadLoaderApplication.cs` is resolved, bundle packaging is fully compliant with AutoCAD 2026 Autoloader, all unit test suites pass (162 HPAutoCad tests, 60 Civil 3D mirror tests, 280 MCP server tests), bridge regression tests pass 21/21, and the unified live verification suite in AutoCAD 2026 satisfies 100% of its assertions (45/45 passed, 0 failed).

---

## 5. Verification Method
To independently reproduce and verify this milestone:

1. **Build the Projects**:
   ```powershell
   dotnet build HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj -c Debug
   dotnet build HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj -c Release
   ```
2. **Run Unit Tests**:
   ```powershell
   dotnet test HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj
   dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj
   dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj
   ```
3. **Execute Live Regression Suite**:
   ```powershell
   powershell -ExecutionPolicy Bypass -File HPAutoCad/tools/harness/run-bridge-unattended.ps1
   ```
4. **Execute Live Verification Suite in AutoCAD 2026**:
   ```powershell
   powershell -ExecutionPolicy Bypass -File HPAutoCad/tools/harness/run-geolink-verify.ps1
   ```
5. **Inspect Verification Results**:
   Inspect `HPAutoCad/output/geolink-verify/summary.json` and confirm:
   `total: 45, passed: 45, failed: 0`.
