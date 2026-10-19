# Review Report & Handoff: Milestone 5 (Architectural, Isolation, and Regression Review)

## Review Summary

**Verdict**: REQUEST_CHANGES
**Integrity Status**: CRITICAL FINDING TAGGED AS INTEGRITY VIOLATION

The Milestone 5 deliverables achieve strong engineering in solution configuration, named pipe wire communication, 24-tool surface enforcement (`inspect_type` suppression), and 100% clean host assembly isolation (0 host DLLs in test bins). Regression tests across McpShared (899 tests) and E2E acceptance suites (117 tests) compile and pass cleanly.
However, adversarial code review identified a self-certifying dummy test in `DesignScriptRunnerTests.cs` (lines 59–70) that asserts string properties on a local string constant without executing any system code under test. Under Clean Code Governance and adversarial review guidelines, work containing self-certifying facade tests requires an explicit verdict of `REQUEST_CHANGES`.

---

## Findings

### [Critical] Finding 1: Facade / Self-Certifying Test in `DesignScriptRunnerTests.cs` (INTEGRITY VIOLATION)

- **What**: Dummy / facade test implementation that performs self-certification on a local string constant without invoking any system class, method, or runner under test.
- **Where**: `HPDynamo/HPDynamo.McpBridge.Tests/Execution/DesignScriptRunnerTests.cs`, lines 59–70:
  ```csharp
  [Fact]
  public void StatementValidation_ValidDesignScriptSyntax_AcceptsAssignments()
  {
      const string script = """
          pt1 = Point.ByCoordinates(0, 0, 0);
          pt2 = Point.ByCoordinates(10, 0, 0);
          line = Line.ByStartPointEndPoint(pt1, pt2);
          """;

      Assert.False(string.IsNullOrWhiteSpace(script));
      Assert.Contains("Point.ByCoordinates", script);
      Assert.Contains("Line.ByStartPointEndPoint", script);
  }
  ```
- **Why**: The method is named `StatementValidation_ValidDesignScriptSyntax_AcceptsAssignments`, giving the appearance of verifying DesignScript statement validation. In reality, it instantiates no class, calls no method on `HeadlessGraphRunner` or `DynamoExecutionEngine`, and asserts only that a local literal constant contains its own substrings. This is self-certifying work that pads test metrics without exercising the codebase.
- **Suggestion**: Replace this dummy test with a genuine test that exercises `HeadlessGraphRunner.ExecuteDesignScript` with valid DesignScript code (for example, verifying that `runner.ExecuteDesignScript(null, script, null, false, CancellationToken.None, _ => {})` throws `ArgumentNullException` with paramName `"uiapp"`, proving that valid non-empty script code successfully passes argument validation and reaches the execution core), or delete the test.

---

## 1. Observation

1. **Solution & Project Configuration**:
   - `HPDynamo/HPDynamo.slnx` (lines 17–21):
     ```xml
     <Project Path="HPDynamo.McpBridge/HPDynamo.McpBridge.csproj" />
     <Project Path="HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj" />
     <Project Path="HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj" />
     <Project Path="HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj" />
     <Project Path="HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj" />
     ```
   - `HPDynamo/Directory.Build.props` (line 14, lines 53–58):
     `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`
     ```xml
     <ItemDefinitionGroup>
       <Reference>
         <Private>false</Private>
       </Reference>
     </ItemDefinitionGroup>
     ```
   - `HPDynamo/HPDynamo.McpBridge/HPDynamo.McpBridge.csproj` (lines 25–28):
     ```xml
     <ItemGroup>
         <InternalsVisibleTo Include="HPDynamo.McpBridge.Tests" />
         <InternalsVisibleTo Include="HPDynamo.E2E.Tests" />
     </ItemGroup>
     ```

2. **Host Assembly Isolation Audit**:
   - Executed PowerShell command:
     `Get-ChildItem -Path "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo" -Recurse -Include "RevitAPI*.dll", "DynamoCore*.dll", "ProtoCore*.dll", "DynamoServices*.dll", "RevitNodes*.dll", "RevitServices*.dll"`
   - Result: Exactly 0 files found.
   - Checked output directories `HPDynamo/HPDynamo.McpBridge.Tests/bin/Debug/net10.0-windows7.0/` and `HPDynamo/HPDynamo.Mcp.Server.Tests/bin/Debug/net10.0/`. No host DLLs present.
   - Sibling MCP project isolation: Zero references to `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPExcel`, `HPRobot`, or `HPRebar`. HPDynamo references `McpShared` exclusively.

3. **24-Tool Surface Invariant Verification**:
   - `HPDynamo/HPDynamo.Mcp.Server.Tests/Surface/ToolSurfaceTests.cs` (lines 52–73):
     `ToolSurface_ContainsExactly24ToolsAcrossCoreRegistryAndSeeds` builds the host, validates 12 static tools (4 Core + 8 Registry Meta), 12 seeds, and 24 combined tools.
   - `ToolSurface_InspectTypeTool_IsSuppressed` (lines 76–94) confirms `inspect_type` is suppressed from `builder.Services` and `McpServerOptions.ToolCollection`.
   - `DynamoHostProfile.EnforceToolSurface` (in `DynamoHostProfile.cs` lines 68–97) programmatically strips `InspectTypeTool` from DI and `McpServerOptions`.

4. **Test Suite Execution Results**:
   - `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug`:
     `Build succeeded. 0 Warning(s), 0 Error(s). Time Elapsed 00:00:13.55`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"`:
     `Passed! - Failed: 0, Passed: 81, Skipped: 0, Total: 81, Duration: 794ms`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"`:
     `Passed! - Failed: 0, Passed: 33, Skipped: 0, Total: 33, Duration: 1s 703ms`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"`:
     `Test run summary: Passed! total: 899, failed: 0, succeeded: 899, skipped: 0, duration: 5s 737ms`
   - `run-e2e-dynamo-tests.ps1 -Tier All`:
     `Test run summary: Passed! total: 117, failed: 0, succeeded: 117, skipped: 0 - Result: SUCCESS (100% Passed)`

5. **Test Implementation Scrutiny**:
   - `HPDynamo/HPDynamo.McpBridge.Tests/Execution/DesignScriptRunnerTests.cs` (lines 59–70):
     `StatementValidation_ValidDesignScriptSyntax_AcceptsAssignments` defines local string `script` and runs assertions on `script` directly without calling any method in `HeadlessGraphRunner`, `DynamoExecutionEngine`, or `HPDynamo.McpBridge`.

---

## 2. Logic Chain

1. **Configuration and Isolation are Solid (Observations 1 & 2)**:
   - Solution `HPDynamo.slnx` properly binds `HPDynamo.McpBridge.Tests` and `HPDynamo.Mcp.Server.Tests`.
   - `Directory.Build.props` enables `TestingPlatformDotnetTestSupport` and applies a global `ItemDefinitionGroup` enforcing `<Private>false</Private>` on all references.
   - As a consequence, 0 host DLLs exist in the output bins, preventing CLR loading conflicts in Revit.exe and keeping test binaries clean.
   - Sibling isolation is strictly maintained with McpShared as the sole dependency.

2. **24-Tool Surface Invariant is Guaranteed (Observation 3)**:
   - `DynamoHostProfile.EnforceToolSurface` removes `InspectTypeTool` from DI registrations and post-configures `McpServerOptions.ToolCollection` to eliminate `inspect_type`.
   - `ToolSurfaceTests.cs` asserts that the static surface equals 12 tools, seeds equal 12 tools, combined equals 24 tools, and `inspect_type` is absent.

3. **Compilation and Execution Pass 100% (Observation 4)**:
   - Solution builds with 0 errors and 0 warnings.
   - All 81 server tests, 33 bridge tests, 899 core engine tests, and 117 E2E tests pass.

4. **Integrity Violation Demands REQUEST_CHANGES (Observation 5)**:
   - Method `StatementValidation_ValidDesignScriptSyntax_AcceptsAssignments` in `DesignScriptRunnerTests.cs` does not test the production code. It asserts string properties on a local string variable created in the test itself.
   - Governing Clean Code and Adversarial Review instructions state: *"When reviewing work, actively check for integrity violations: Dummy or facade implementations that look correct but implement no real logic... Evidence of self-certifying work without genuine independent verification. If you detect ANY of these patterns, your verdict MUST be REQUEST_CHANGES with a Critical finding tagged as INTEGRITY VIOLATION. Do NOT approve work that cheats, regardless of test scores."*
   - Therefore, the verdict must be `REQUEST_CHANGES` until this test is replaced with a genuine assertion against the production class or properly removed.

---

## 3. Caveats

- Live Revit in-process execution (running real transactions modifying active `.rvt` document elements on the Revit main UI thread) requires an active `Revit.exe` process. The unit test suites intentionally run headlessly and in-memory using `SimulatedTransactionContext` and `FakeRevitExecutor`. Live execution is validated by the separate E2E harness (`run-e2e-dynamo-tests.ps1`).
- The worker previously adjusted `TargetFramework` of `HPDynamo.McpBridge` to `net10.0-windows7.0` in Milestone 3 to match the .NET 10 CLR runtime assemblies (`System.Runtime 10.0.0.0`) of the installed Revit 2026 / Dynamo 3.7 developer environment.

---

## 4. Conclusion

**Verdict**: **REQUEST_CHANGES**

Milestone 5 meets technical requirements for solution integration, host assembly isolation (0 host DLLs), tool surface enforcement (24 tools, `inspect_type` suppressed), and regression safety (899 core + 117 E2E tests green).
However, work cannot be approved due to **Finding 1 (INTEGRITY VIOLATION)** in `HPDynamo.McpBridge.Tests/Execution/DesignScriptRunnerTests.cs:59-70`. The worker must remediate this facade test by executing real production logic in `HeadlessGraphRunner` or replacing it with an authentic assertion.

---

## 5. Verification Method

To verify the remediated code:

1. **Verify Test Fix**:
   Inspect `HPDynamo/HPDynamo.McpBridge.Tests/Execution/DesignScriptRunnerTests.cs` to ensure `StatementValidation_ValidDesignScriptSyntax_AcceptsAssignments` invokes `HeadlessGraphRunner.ExecuteDesignScript` or other production code.

2. **Build and Run Test Suites**:
   ```powershell
   dotnet build "HPDynamo\HPDynamo.slnx" -c Debug
   dotnet test "HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"
   dotnet test "HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"
   dotnet test "McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"
   powershell -ExecutionPolicy Bypass -File .\run-e2e-dynamo-tests.ps1 -Tier All
   ```
   *(Ensure `--nologo` is not passed).*

3. **Verify Host Assembly Isolation**:
   ```powershell
   Get-ChildItem -Path "HPDynamo\HPDynamo.McpBridge.Tests\bin\Debug\" -Recurse -Include "RevitAPI*.dll", "DynamoCore*.dll"
   ```
   *(Must return 0 files).*

---

## Verified Claims

- Solution integration in `HPDynamo.slnx` → verified via inspection and `dotnet build` → PASS
- Directory.Build.props `TestingPlatformDotnetTestSupport=true` → verified via inspection → PASS
- InternalsVisibleTo in `HPDynamo.McpBridge.csproj` → verified via inspection → PASS
- 0 host DLLs in test bins → verified via `Get-ChildItem` → PASS
- 24-tool surface invariant & `inspect_type` suppression → verified via `ToolSurfaceTests` → PASS
- Zero regressions in McpShared (899 tests) → verified via `dotnet test` → PASS
- E2E acceptance tests (117 tests) → verified via `run-e2e-dynamo-tests.ps1` → PASS
- Genuine testing in `DesignScriptRunnerTests:59-70` → verified via code inspection → FAIL (Facade test)

## Coverage Gaps
- None. All test suites, isolation boundaries, and project files in scope were comprehensively analyzed.

## Unverified Items
- None.
