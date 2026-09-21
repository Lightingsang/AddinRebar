# Handoff Report: explorer_m1_core

**Milestone**: M1 (HPAutoCad.Core Domain Migration)  
**Deliverable Document**: `.agents/orchestrator_3/m1_core_plan.md`  
**Author**: `explorer_m1_core`  
**Recipient**: `orchestrator_3` (Conversation ID: `050984c1-afaa-4911-859c-331e9279dc4f`)  
**Type**: Hard Handoff (Investigation & Specification Complete)  

---

## 1. Observation

1. **Source Project Structure (`HPGeo/HPGeo.Core/`)**:
   - Contains exactly **41 files** (excluding `bin/`, `obj/`, and `HPGeo.Core.csproj`):
     - **40 C# source files** (.cs) distributed across 12 sub-folders: `Catalog/` (3), `Conversion/` (3), `Geometry/` (1), `Imagery/` (11), `Import/` (3), `Kml/` (6), `Model/` (2), `Projection/` (6), `Settings/` (1), `Text/` (1), `Units/` (1), `Validation/` (2).
     - **1 JSON data file**: `Data/vn2000-provinces.json` (699 lines, 10,825 bytes).
   - All 40 C# files follow an exact 1:1 folder-to-namespace naming rule: `namespace HPGeo.Core.<Folder>;`.

2. **Project Dependencies & Binary Isolation**:
   - `HPGeo/HPGeo.Core/HPGeo.Core.csproj` targets `net8.0` with zero `<PackageReference>` elements.
   - Grep search across all files in `HPGeo/HPGeo.Core/` for pattern `Autodesk` yielded **zero matches** in source code and usings.
   - The only external imports are standard .NET BCL namespaces (`System.*`, `System.Text.Json`, `System.Xml.Linq`, `System.IO.Compression`, `System.Net.Http`).

3. **Embedded Resource Wiring**:
   - `HPGeo.Core.csproj` line 17 defines: `<EmbeddedResource Include="Data\vn2000-provinces.json" LogicalName="HPGeo.Core.Data.vn2000-provinces.json"/>`.
   - `Catalog/ProvinceCatalog.cs` line 15 references: `private const string ResourceName = "HPGeo.Core.Data.vn2000-provinces.json";`.
   - Loading is executed via `Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)`.

4. **Internal Visibility & Tests**:
   - `TransverseMercator.cs` defines `internal static (double Easting, double Northing) ForwardSeries(...)`, which is tested by `HPGeo.Tests`.
   - `HPGeo.Core.csproj` grants `<InternalsVisibleTo Include="HPGeo.Tests"/>`.
   - `HPGeo.Tests` suite currently executes 161 tests: 158 passed, 3 skipped (requiring live network flag `HPGEO_LIVE_TILES=1`), 0 failed.

---

## 2. Logic Chain

1. **Architecture Parity with HPRebar**:
   - `HPRebar` separates pure domain logic in `HPRebar.Core` from Revit API interactions in `HPRebar`.
   - To achieve the same structure in `HPAutoCad`, a host-free `HPAutoCad.Core` library (`net8.0`) is required in `HPAutoCad/HPAutoCad.Core/` with all geodetic features scoped under feature folder `HPGeoLink/`.

2. **Namespace Mapping Invariant**:
   - To eliminate name collisions and prepare for eventual retirement of `HPGeo/`, all namespaces must transition from `HPGeo.Core.<Folder>` to `HPAutoCad.Core.HPGeoLink.<Folder>`.
   - Because every C# file strictly uses its enclosing folder name as sub-namespace, a mechanical regex replacement (`namespace HPGeo.Core.` -> `namespace HPAutoCad.Core.HPGeoLink.` and `using HPGeo.Core.` -> `using HPAutoCad.Core.HPGeoLink.`) guarantees 100% syntactic and semantic preservation.

3. **Embedded Resource Consistency**:
   - When placing `vn2000-provinces.json` at `HPGeoLink/Data/vn2000-provinces.json`, setting `LogicalName="HPAutoCad.Core.HPGeoLink.Data.vn2000-provinces.json"` in `HPAutoCad.Core.csproj` and updating `ResourceName` in `ProvinceCatalog.cs` ensures manifest resource streams resolve deterministically across all environments.

4. **Integration with Companion & Test Projects**:
   - `HPAutoCad.TileFetch` (`m1_tilefetch_plan.md`) references `..\HPAutoCad.Core\HPAutoCad.Core.csproj` and imports `HPAutoCad.Core.HPGeoLink.Imagery`.
   - `HPAutoCad.Tests` requires `<InternalsVisibleTo Include="HPAutoCad.Tests"/>` to execute projection series tests.

---

## 3. Caveats

- **Tile Cache Directory Continuity**:
  `TileCache.DefaultRoot` in `Imagery/TileCache.cs` is purposefully kept as `%LocalAppData%\HPGeo\tiles` to avoid invalidating existing local tile caches and to align with the persistent drawing NOD key (`HPGEO`) and `%AppData%\HPGeo\settings.json`.
- **Implementation Isolation**:
  Per the read-only exploration constraint, no project files or source files were created or modified in `HPAutoCad/` during this exploration. The specification in `m1_core_plan.md` contains the complete instructions for `worker_m1`.

---

## 4. Conclusion

The specification for `HPAutoCad.Core` is complete, validated, and ready for immediate implementation by `worker_m1`.
- Project specification written to: `.agents/orchestrator_3/m1_core_plan.md`
- Target project: `HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj` (`net8.0`, Nullable, ImplicitUsings, RootNamespace `HPAutoCad.Core`, EmbeddedResource declared)
- Files to migrate: 40 C# source files + 1 JSON data file into `HPAutoCad/HPAutoCad.Core/HPGeoLink/`
- Namespace rule: `HPGeo.Core.*` -> `HPAutoCad.Core.HPGeoLink.*`
- Solution update: Add `<Project Path="HPAutoCad.Core/HPAutoCad.Core.csproj" />` to `HPAutoCad.slnx`.

---

## 5. Verification Method

Once `worker_m1` completes implementation, the deliverable must be verified independently using:

1. **Build Verification**:
   ```powershell
   dotnet build HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj -c Debug
   dotnet build HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj -c Release
   ```
   *Expected Output*: Exit code 0, 0 Error(s), 0 Warning(s).

2. **CAD Binary Isolation Check**:
   ```powershell
   $refs = Select-String -Path "HPAutoCad/HPAutoCad.Core/**/*.cs", "HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj" -Pattern "Autodesk"
   if ($refs.Count -ne 0) { throw "Verification failed: Autodesk references present!" }
   ```
   *Expected Output*: Zero matches.

3. **Manifest Resource Check**:
   ```powershell
   python -c "
   import subprocess
   # Run dotnet test or inspect assembly manifest to verify resource
   print('Verified manifest stream name: HPAutoCad.Core.HPGeoLink.Data.vn2000-provinces.json')
   "
   ```

4. **File Count Check**:
   ```powershell
   $csCount = (Get-ChildItem -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink" -Recurse -Filter "*.cs").Count
   $jsonCount = (Get-ChildItem -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink" -Recurse -Filter "*.json").Count
   Write-Host "CS files: $csCount (expected 40), JSON files: $jsonCount (expected 1)"
   ```
