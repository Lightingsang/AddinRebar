# Handoff Report — explorer_m4_2: Test Execution & Filter Removal Investigation (Finding F12)

## 1. Observation

### 1.1. Occurrence of `--filter-not-class` in the Repository
Through repository-wide pattern search (`grep_search`), `--filter-not-class` is located exclusively in:
- `plans/reports/261012-1229-hpgrasshopper-antigravity-handoff.md:97`:
  ```powershell
  & './HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' `
    --filter-not-class HPGrasshopper.McpBridge.Tests.Fixtures.FixtureDefinitionTests `
    --no-progress --timeout 120s
  ```
- Correlating audit statements in `261012-1229-hpgrasshopper-antigravity-handoff.md`:
  - Line 83: *"359 test passed. 13 test bị loại khỏi lần chạy, không phải skipped. Toàn bộ lớp `FixtureDefinitionTests` bị loại vì static initialization ghi lại fixtures trong source/harness/definitions."*
  - Line 262 (Finding F12): *"`Fixtures/FixtureDefinitionTests.cs:10,20,31–50` ghi lại fixture trong source khi chạy."*
- Correlating milestone task in `PROJECT.md:28`:
  - Finding F12 / Milestone M4: *"Isolate fixture generation in `FixtureDefinitionTests.cs` to test output/temp directory; eliminate writes to git-tracked directories"*.
- Additional test runner scripts examined:
  - `HPGrasshopper/run-e2e-grasshopper-tests.ps1`: Invokes `HPGrasshopper.E2E.Tests.exe` by tier (`--filter-class *Tier1*`), does not filter bridge fixtures.
  - `HPGrasshopper/tools/harness/run-live-verify.ps1`: Drives stdio live verification via Python, no class filter.
  - `scratch/stress_test_m4_bridge.py:80`: Ran `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests -c Release --no-build --filter FullyQualifiedName~FixtureDefinitionTests`.

### 1.2. Root Cause in Source Code
Direct inspection of `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures/FixtureDefinitionTests.cs`:
- Lines 10–22:
  ```csharp
  private static readonly string FixturesDir = ResolveFixturesDirectory();

  private static string ResolveFixturesDirectory()
  {
      var baseDir = AppContext.BaseDirectory;
      var dir = Path.Combine(baseDir, "Fixtures");
      if (!Directory.Exists(dir))
      {
          Directory.CreateDirectory(dir);
      }
      EnsureFixturesExist(dir);
      return dir;
  }
  ```
- Lines 24–51:
  ```csharp
  private static void EnsureFixturesExist(string targetDir)
  {
      var srcFixturesDir = Path.GetFullPath(Path.Combine(targetDir, "..", "..", "..", "..", "Fixtures"));
      var harnessFixturesDir = Path.GetFullPath(Path.Combine(targetDir, "..", "..", "..", "..", "..", "tools", "harness", "fixtures"));

      var mathArchive = FixtureBuilder.CreateMathSliderArchive();
      FixtureBuilder.SaveFixturePair(targetDir, "MathSlider", mathArchive);
      FixtureBuilder.SaveFixturePair(srcFixturesDir, "MathSlider", mathArchive);
      FixtureBuilder.SaveFixturePair(harnessFixturesDir, "MathSlider", mathArchive);

      var curveArchive = FixtureBuilder.CreateCurveGeneratorArchive();
      FixtureBuilder.SaveFixturePair(targetDir, "CurveGenerator", curveArchive);
      FixtureBuilder.SaveFixturePair(srcFixturesDir, "CurveGenerator", curveArchive);
      FixtureBuilder.SaveFixturePair(harnessFixturesDir, "CurveGenerator", curveArchive);

      var revitArchive = FixtureBuilder.CreateRevitDirectShapeSyncMockArchive();
      FixtureBuilder.SaveFixturePair(targetDir, "RevitDirectShapeSyncMock", revitArchive);
      FixtureBuilder.SaveFixturePair(srcFixturesDir, "RevitDirectShapeSyncMock", revitArchive);
      FixtureBuilder.SaveFixturePair(harnessFixturesDir, "RevitDirectShapeSyncMock", revitArchive);

      var defsDir = Path.GetFullPath(Path.Combine(targetDir, "..", "..", "..", "..", "..", "definitions"));
      var scriptPath = Path.Combine(defsDir, "ContinuousBeamRenumbering.cs");
      var renumberArchive = FixtureBuilder.CreateContinuousBeamRenumberingArchive(scriptPath);
      FixtureBuilder.SaveFixturePair(defsDir, "ContinuousBeamRenumbering", renumberArchive);
      FixtureBuilder.SaveFixturePair(targetDir, "ContinuousBeamRenumbering", renumberArchive);
      FixtureBuilder.SaveFixturePair(srcFixturesDir, "ContinuousBeamRenumbering", renumberArchive);
      FixtureBuilder.SaveFixturePair(harnessFixturesDir, "ContinuousBeamRenumbering", renumberArchive);
  }
  ```

### 1.3. Empirical Observation of File Overwrite Mutation
Upon executing `FixtureDefinitionTests`, file timestamps on disk updated across git-tracked folders:
```powershell
Get-ChildItem -Recurse 'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures', 'HPGrasshopper/tools/harness/fixtures', 'HPGrasshopper/definitions' | Format-Table Name, LastWriteTime -AutoSize
```
Result:
```text
Name                          LastWriteTime        
----                          -------------        
ContinuousBeamRenumbering.gh  12/10/2026 6:56:18 PM
ContinuousBeamRenumbering.ghx 12/10/2026 6:56:18 PM
CurveGenerator.gh             12/10/2026 6:56:18 PM
CurveGenerator.ghx            12/10/2026 6:56:18 PM
MathSlider.gh                 12/10/2026 6:56:18 PM
MathSlider.ghx                12/10/2026 6:56:18 PM
RevitDirectShapeSyncMock.gh   12/10/2026 6:56:18 PM
RevitDirectShapeSyncMock.ghx  12/10/2026 6:56:18 PM
ContinuousBeamRenumbering.gh  12/10/2026 6:56:18 PM
ContinuousBeamRenumbering.ghx 12/10/2026 6:56:18 PM
...
```
All `.gh` and `.ghx` files in source repositories were rewritten simultaneously during test execution.

### 1.4. Empirical Test Results Without Filter
1. **Isolated `FixtureDefinitionTests` execution**:
   - Command: `& 'HPGrasshopper/HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' --filter-class HPGrasshopper.McpBridge.Tests.Fixtures.FixtureDefinitionTests --no-progress`
   - Output:
     ```text
     Test run summary: Passed! - HPGrasshopper.McpBridge.Tests.dll (net10.0|x64)
       total: 13
       failed: 0
       succeeded: 13
       skipped: 0
       duration: 424ms
     ```
   - Exit Code: `0`

2. **Full `HPGrasshopper.McpBridge.Tests` suite execution without filter (Debug)**:
   - Command: `& 'HPGrasshopper/HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' --no-progress`
   - Output:
     ```text
     Test run summary: Passed! - HPGrasshopper.McpBridge.Tests.dll (net10.0|x64)
       total: 474
       failed: 0
       succeeded: 474
       skipped: 0
       duration: 17s 894ms
     ```
   - Exit Code: `0`

3. **Full `HPGrasshopper.McpBridge.Tests` suite execution without filter (Release)**:
   - Command: `& 'HPGrasshopper/HPGrasshopper.McpBridge.Tests/bin/Release/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' --no-progress`
   - Output:
     ```text
     Test run summary: Passed! - HPGrasshopper.McpBridge.Tests.dll (net10.0|x64)
       total: 474
       failed: 0
       succeeded: 474
       skipped: 0
       duration: 16s 501ms
     ```
   - Exit Code: `0`

4. **Execution via `dotnet test` (Microsoft.Testing.Platform runner)**:
   - Command: `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug --no-build`
   - Output:
     ```text
     Passed! - Failed: 0, Passed: 474, Skipped: 0, Total: 474, Duration: 16s 740ms - HPGrasshopper.McpBridge.Tests.dll (net10.0-windows7.0|x64)
     Tests succeeded: 'HPGrasshopper.McpBridge.Tests.dll'
     ```
   - Exit Code: `0`

5. **Broader Solution Suites**:
   - `HPGrasshopper.Mcp.Server.Tests`: 77 / 77 passed (Duration: 4.52s)
   - `HPGrasshopper.E2E.Tests`: 181 / 181 passed (Duration: 7.92s)
   - Combined HPGrasshopper total: **732 passed, 0 failed, 0 skipped**.

### 1.5. Dependency Analysis for `FixtureDefinitionTests`
Inspection of `FixtureDefinitionTests.cs` and `FixtureBuilder.cs`:
- Imports:
  - `System`
  - `System.Collections.Generic`
  - `System.IO`
  - `GH_IO.Serialization` (`GH_Archive`, `GH_Chunk`, `GH_IWriter`)
  - `Xunit`
- Native Host Assembly Requirements:
  - `RhinoCommon.dll`: **NOT REQUIRED**. No geometric calculation, `RhinoDoc`, or native P/Invoke is performed.
  - `Grasshopper.dll` / `GH_Document`: **NOT REQUIRED**. No canvas document model is loaded or simulated.
  - `GH_IO.dll`: **REQUIRED**. Used exclusively for XML and binary archive serialization and inspection. `GH_IO.dll` is pure managed .NET IL located at `C:\Program Files\Rhino 8\Plug-ins\Grasshopper\GH_IO.dll`.
  - Dynamic Assembly Resolution: `GrasshopperAssemblyResolver` in `HPGrasshopper.McpBridge/Host/GrasshopperAssemblyResolver.cs` probes `C:\Program Files\Rhino 8\Plug-ins\Grasshopper\` and dynamically resolves `GH_IO.dll` seamlessly without copying to `bin/`.

---

## 2. Logic Chain

1. **Premise**: The test runner exclusion `--filter-not-class HPGrasshopper.McpBridge.Tests.Fixtures.FixtureDefinitionTests` was introduced during the baseline audit solely because `FixtureDefinitionTests` exhibited an unauthorized side-effect: mutating git-tracked source directories on every run (`Observation §1.1, §1.2`).
2. **Finding**: The tests inside `FixtureDefinitionTests` themselves do NOT fail. When executed directly, all 13 tests pass cleanly in 424ms with 0 errors (`Observation §1.4`).
3. **Finding**: Furthermore, when `HPGrasshopper.McpBridge.Tests.exe` runs completely without any exclusion filter, all 474 tests pass 100% green (`Observation §1.4`).
4. **Analysis of File Coupling**: The 13 test methods in `FixtureDefinitionTests.cs` read exclusively from `FixturesDir` (`targetDir = Path.Combine(AppContext.BaseDirectory, "Fixtures")`). None of the 13 assertions read from or depend on writes to `srcFixturesDir`, `harnessFixturesDir`, or `defsDir` (`Observation §1.2`).
5. **Analysis of Build Pipeline**: `HPGrasshopper.McpBridge.Tests.csproj` already contains:
   ```xml
   <ItemGroup>
       <None Update="Fixtures\**\*">
           <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
       </None>
   </ItemGroup>
   ```
   MSBuild automatically copies the golden fixture files from the source tree to `bin/$(Configuration)/$(TargetFramework)/Fixtures/` during build time.
6. **Deduction**: Overwriting `srcFixturesDir`, `harnessFixturesDir`, and `defsDir` from inside `EnsureFixturesExist` is an unnecessary antipattern. It causes repository pollution and violates Clean Code Rule T1/T4.
7. **Resolution**: By eliminating all writes to `..`, `..` parent directories in `EnsureFixturesExist` and only generating missing files within `targetDir` (`bin/.../Fixtures/`), the side effect is completely eliminated. As a result, the `--filter-not-class` exclusion can be safely removed from all documentation, audit commands, and test runners.

---

## 3. Caveats

1. **Golden Files Synchronization**: If golden fixture definitions in `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures/` ever need programmatic updating in the future, this must be executed through a dedicated, explicit generation script (e.g., `tools/generate-fixtures.ps1`) rather than an implicit side-effect of running unit tests.
2. **Headless CI Environments without Rhino Installed**: `FixtureDefinitionTests` depends on `GH_IO.dll`. On build systems where Rhino 8 is installed at `C:\Program Files\Rhino 8`, `GrasshopperAssemblyResolver` locates `GH_IO.dll` automatically. In environments without Rhino 8 installed, either `HPGRASSHOPPER_RHINO_DIR` must point to a directory containing `GH_IO.dll`, or `GH_IO.dll` should be referenced via NuGet package (`Grasshopper` / `RhinoCommon` NuGet reference).
3. **Execution Time**: Running all 474 bridge tests without filter takes ~16.5–17.9 seconds, well within the standard test timeout of 120 seconds.

---

## 4. Conclusion & Proposed Strategy

### 4.1. Core Conclusion
`FixtureDefinitionTests` is fully functional and passes 100% (13/13). The `--filter-not-class` exclusion was a workaround for source directory mutation, not for test failure. Eliminating writes to git-tracked directories completely resolves Finding F12 and enables 100% clean, filter-free execution of `HPGrasshopper.McpBridge.Tests.exe` (474 tests).

### 4.2. Strategy for Filter Removal (Implementation Plan)

#### Step 1: Refactor `FixtureDefinitionTests.cs` to eliminate source tree writes
Replace `EnsureFixturesExist` with a non-mutating version that only ensures files exist in `targetDir` (`bin/.../Fixtures/`) and never writes to parent/git-tracked directories:

**Proposed Change in `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures/FixtureDefinitionTests.cs`**:
```csharp
<<<<
    private static void EnsureFixturesExist(string targetDir)
    {
        var srcFixturesDir = Path.GetFullPath(Path.Combine(targetDir, "..", "..", "..", "..", "Fixtures"));
        var harnessFixturesDir = Path.GetFullPath(Path.Combine(targetDir, "..", "..", "..", "..", "..", "tools", "harness", "fixtures"));

        var mathArchive = FixtureBuilder.CreateMathSliderArchive();
        FixtureBuilder.SaveFixturePair(targetDir, "MathSlider", mathArchive);
        FixtureBuilder.SaveFixturePair(srcFixturesDir, "MathSlider", mathArchive);
        FixtureBuilder.SaveFixturePair(harnessFixturesDir, "MathSlider", mathArchive);

        var curveArchive = FixtureBuilder.CreateCurveGeneratorArchive();
        FixtureBuilder.SaveFixturePair(targetDir, "CurveGenerator", curveArchive);
        FixtureBuilder.SaveFixturePair(srcFixturesDir, "CurveGenerator", curveArchive);
        FixtureBuilder.SaveFixturePair(harnessFixturesDir, "CurveGenerator", curveArchive);

        var revitArchive = FixtureBuilder.CreateRevitDirectShapeSyncMockArchive();
        FixtureBuilder.SaveFixturePair(targetDir, "RevitDirectShapeSyncMock", revitArchive);
        FixtureBuilder.SaveFixturePair(srcFixturesDir, "RevitDirectShapeSyncMock", revitArchive);
        FixtureBuilder.SaveFixturePair(harnessFixturesDir, "RevitDirectShapeSyncMock", revitArchive);

        var defsDir = Path.GetFullPath(Path.Combine(targetDir, "..", "..", "..", "..", "..", "definitions"));
        var scriptPath = Path.Combine(defsDir, "ContinuousBeamRenumbering.cs");
        var renumberArchive = FixtureBuilder.CreateContinuousBeamRenumberingArchive(scriptPath);
        FixtureBuilder.SaveFixturePair(defsDir, "ContinuousBeamRenumbering", renumberArchive);
        FixtureBuilder.SaveFixturePair(targetDir, "ContinuousBeamRenumbering", renumberArchive);
        FixtureBuilder.SaveFixturePair(srcFixturesDir, "ContinuousBeamRenumbering", renumberArchive);
        FixtureBuilder.SaveFixturePair(harnessFixturesDir, "ContinuousBeamRenumbering", renumberArchive);
    }
====
    private static void EnsureFixturesExist(string targetDir)
    {
        EnsureFixturePair(targetDir, "MathSlider", FixtureBuilder.CreateMathSliderArchive);
        EnsureFixturePair(targetDir, "CurveGenerator", FixtureBuilder.CreateCurveGeneratorArchive);
        EnsureFixturePair(targetDir, "RevitDirectShapeSyncMock", FixtureBuilder.CreateRevitDirectShapeSyncMockArchive);

        var defsDir = Path.GetFullPath(Path.Combine(targetDir, "..", "..", "..", "..", "..", "definitions"));
        var scriptPath = Path.Combine(defsDir, "ContinuousBeamRenumbering.cs");
        EnsureFixturePair(targetDir, "ContinuousBeamRenumbering", () => FixtureBuilder.CreateContinuousBeamRenumberingArchive(scriptPath));
    }

    private static void EnsureFixturePair(string targetDir, string fixtureName, Func<GH_Archive> archiveFactory)
    {
        var ghxPath = Path.Combine(targetDir, fixtureName + ".ghx");
        var ghPath = Path.Combine(targetDir, fixtureName + ".gh");
        if (!File.Exists(ghxPath) || !File.Exists(ghPath))
        {
            var archive = archiveFactory();
            FixtureBuilder.SaveFixturePair(targetDir, fixtureName, archive);
        }
    }
>>>>
```

#### Step 2: Clean Code Compliance Verification
- **Rule C2 (File Length $\le 300$ lines)**:
  - `FixtureDefinitionTests.cs`: Currently 237 lines; after refactoring: ~225 lines (Compliant: $\le 300$).
  - `FixtureBuilder.cs`: 217 lines (Compliant: $\le 300$).
- **Rule M3 (Method Length $\le 50$ lines)**:
  - `EnsureFixturesExist`: 11 lines (Compliant: $\le 50$).
  - `EnsureFixturePair`: 11 lines (Compliant: $\le 50$).
  - All test methods: 10–18 lines (Compliant: $\le 50$).
- **Rules Q-B1 / Q-B2**:
  - Zero commented-out code.
  - Zero empty catch blocks without comments.

#### Step 3: Update documentation and runner commands
In `plans/reports/261012-1229-hpgrasshopper-antigravity-handoff.md:96–98` and test documentation, replace the filtered execution command with:
```powershell
& './HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' `
  --no-progress --timeout 120s
```
or standard:
```powershell
dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug
```

---

## 5. Verification Method

### 5.1. Independent Verification Commands
To verify that all tests pass cleanly without filter and without modifying any git-tracked files:

1. **Verify git working tree state before running**:
   ```powershell
   git status --short HPGrasshopper
   ```
2. **Execute bridge tests directly without any exclusion filter**:
   ```powershell
   & 'HPGrasshopper/HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' --no-progress
   ```
   **Expected**: `total: 474, failed: 0, succeeded: 474, skipped: 0`, exit code 0.
3. **Execute via `dotnet test`**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug --no-build
   ```
   **Expected**: `Passed: 474, Failed: 0, Total: 474`, exit code 0.
4. **Verify git working tree remains clean after proposed refactoring**:
   ```powershell
   git status --short HPGrasshopper
   ```
   **Expected**: No modified `.gh` or `.ghx` files in `Fixtures/`, `tools/harness/fixtures/`, or `definitions/`.

### 5.2. Invalidation Conditions
This analysis would be invalidated if:
- `FixtureDefinitionTests` had failing assertions when run against existing golden files (empirically disproven: 13/13 pass).
- Any bridge test relied on runtime updates to `HPGrasshopper/tools/harness/fixtures/` or `HPGrasshopper/definitions/` (empirically disproven: no test reads from them during test execution).
