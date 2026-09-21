# Handoff Report: Milestone M2 CAD Commands & CAD Services

**Author**: explorer_m2_cad  
**Date**: 2026-09-20T13:25:00Z  
**Target Recipient**: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)  
**Deliverable Document**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m2_cad_plan.md`  

---

## 1. Observation

1. **Legacy Baseline Analysis**:
   - `HPGeo/HPGeo.AutoCad/Commands/`: Contains 7 files (`HPGeoDialogCommand.cs`, `HPGeoKmzScriptCommand.cs`, `HPGeoImportCommand.cs`, `HPGeoImportScriptCommand.cs`, `HPGeoImageScriptCommand.cs`, `HPGeoInfoCommand.cs`, `ImageryConsole.cs`).
   - `HPGeo/HPGeo.AutoCad/Cad/`: Contains 5 files (`DrawingContext.cs`, `DrawingReader.cs`, `DrawingWriter.cs`, `DocumentSettingsStore.cs`, `UserSettingsStore.cs`).
   - `HPGeo/HPGeo.AutoCad/Imagery/`: Contains 4 files (`ImageryPipeline.cs`, `RasterInserter.cs`, `TileStitcher.cs`, `HelperTileFetcher.cs`).
   - `HPGeo/HPGeo.AutoCad/`: Contains root support files `GoogleEarthLauncher.cs`, `HPGeoLog.cs`, and `Entry.cs`.
2. **Current HPAutoCad Solution State**:
   - `HPAutoCad/HPAutoCad.Core/`: Successfully migrated in M1. Contains 53 C# files under `HPGeoLink/` organized by sub-namespaces (`Catalog`, `Conversion`, `Geometry`, `Imagery`, `Import`, `Kml`, `Model`, `Projection`, `Settings`, `Text`, `Units`, `Validation`).
   - `HPAutoCad/HPAutoCad.TileFetch/`: `HPAutoCad.TileFetch.csproj` targets `net8.0` with `AssemblyName = HPAutoCad.TileFetch`.
   - `HPAutoCad/HPAutoCad.Tests/`: Contains 161 geodetic unit tests. Executed `dotnet test HPAutoCad.Tests`: 158 succeeded, 0 failed, 3 skipped (live tile network tests requiring `HPGEO_LIVE_TILES=1`).
3. **ALC Isolation & Command Invariant**:
   - In `HPGeo/HPGeo.AutoCad/Entry.cs` (lines 8-9) and `HPGeo/HPGeo.AutoCad.Loader/HPGeoCommands.cs` (lines 12-30): no `[CommandMethod]` or `[ExtensionApplication]` attributes exist in the add-in assembly. The loader registers commands and invokes delegates (`dialog`, `kmz-script`, `import`, `import-script`, `image-script`, `info`, `stop`) retrieved from `Entry.Start(...)` via reflection.
4. **Tile Fetch Subprocess Naming**:
   - In `HPGeo/HPGeo.AutoCad/Imagery/HelperTileFetcher.cs` (line 17): `public const string HelperExe = "HPGeo.TileFetch.exe";`.
   - In `HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj` (line 14): `AssemblyName` is `HPAutoCad.TileFetch`. The executable is `HPAutoCad.TileFetch.exe`.
5. **Persistence Paths**:
   - Drawing NOD: key `"HPGEO"` (`GeoSettings.Key`).
   - User settings: `%AppData%\HPGeo\settings.json`.
   - Log directory: `%LocalAppData%\HPGeo\logs\hpgeo-YYYYMMDD.log`.

---

## 2. Logic Chain

1. **Separation of Concerns and Host Isolation**:
   - *Premise*: AutoCAD commands registered via `[CommandMethod]` in secondary ALCs can leak types or cause JIT resolution collisions.
   - *Observation*: `HPGeo.AutoCad.Loader` registers commands in AutoCAD's Default ALC, creates an isolated ALC (`AppLoadContext`), and binds commands to delegates returned by `Entry.Start`.
   - *Deduction*: `HPAutoCad/HPAutoCad/` must retain this exact structure — zero AutoCAD attributes in `HPAutoCad.dll`, all command execution driven via `HPAutoCad.Entry.Start`.
2. **Namespace Migration Consistency**:
   - *Observation*: All domain types migrated to `HPAutoCad.Core.HPGeoLink.*`.
   - *Deduction*: All files in `HPGeo.AutoCad` must migrate:
     - `HPGeo.AutoCad` -> `HPAutoCad.HPGeoLink` (root utilities) and `HPAutoCad` (`Entry.cs`).
     - `HPGeo.AutoCad.Commands` -> `HPAutoCad.HPGeoLink.Commands`.
     - `HPGeo.AutoCad.Cad` -> `HPAutoCad.HPGeoLink.Cad`.
     - `HPGeo.AutoCad.Imagery` -> `HPAutoCad.HPGeoLink.Imagery`.
     - All `using HPGeo.Core.*` -> `using HPAutoCad.Core.HPGeoLink.*`.
3. **Subprocess Compatibility**:
   - *Observation*: The new companion project builds `HPAutoCad.TileFetch.exe`, whereas the legacy code looked for `HPGeo.TileFetch.exe`.
   - *Deduction*: `HelperTileFetcher.cs` must be configured with `HelperExe = "HPAutoCad.TileFetch.exe"`, and provide a fallback check for `HPGeo.TileFetch.exe` to guarantee zero breakages during deployment or migration transitions.
4. **CAD Transaction & Layer Integrity**:
   - *Observation*: In `DrawingWriter.cs` (line 18) and `RasterInserter.cs` (line 23), layers `HPGEO-IMPORT` and `HPGEO-IMAGE` are created on demand, and all entity insertions happen inside single, atomic `Transaction` blocks.
   - *Deduction*: Keeping these exact layer names and transaction scopes guarantees idempotent, undoable CAD modifications (single `U` undoes entire import or image insertion).

---

## 3. Caveats

- **WPF UI Migration Scope**: The WPF view windows (`GeoExportWindow.xaml`, `GeoImportWindow.xaml`) and view models are explored and planned in a parallel sub-track. `HPGeoDialogCommand.cs` and `HPGeoImportCommand.cs` reference these views/viewmodels via clear interface boundaries (`IGeoExportShell`, `IGeoImportShell`, `GeoExportViewModel`, `GeoImportViewModel`). If the worker ports CAD commands before UI, stubs or mock views can be used or the UI files can be created in tandem.
- **AutoCAD 2026 Process Dependency**: Full runtime execution of commands touching AutoCAD database transactions (`TransactionManager.StartTransaction()`) requires a running `acad.exe` instance. However, all domain logic, arguments parsing, and data models remain 100% unit testable host-free.

---

## 4. Conclusion

The technical plan `m2_cad_plan.md` completely defines the implementation roadmap for Milestone M2 CAD Commands & CAD Services:
- Exact file paths, class definitions, and namespaces for all 18 files.
- Precise reflection contract for `HPAutoCad.Entry.Start` matching `PROJECT.md` line 77.
- Complete namespace migration matrix (`HPGeo.AutoCad.*` -> `HPAutoCad.HPGeoLink.*`).
- Concrete, phase-by-phase implementation instructions for the worker agent.

---

## 5. Verification Method

1. **Static Compilation**:
   ```bash
   dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
   ```
   *Expected Result*: 0 errors, 0 warnings treated as errors.
2. **Unit Test Execution**:
   ```bash
   dotnet test HPAutoCad/HPAutoCad.Tests
   ```
   *Expected Result*: 161 tests run, 158 passed, 3 skipped (requiring live tiles env).
3. **No Leaked Command Attributes**:
   Run grep check on `HPAutoCad/HPAutoCad/`:
   ```bash
   git grep -n "CommandMethod" HPAutoCad/HPAutoCad/
   ```
   *Expected Result*: 0 matches in `HPAutoCad/HPAutoCad/` (only present in `HPAutoCad.Loader`).
4. **Mirror Compatibility**:
   ```bash
   dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests
   ```
   *Expected Result*: 55 tests pass, confirming mirror invariants remain intact.
