# Forensic Integrity Audit Report — Milestone M1 Remediation (Findings N1–N5)

**Work Product**: HPGrasshopper MCP Bridge & Server (Milestone M1 Remediation, Findings N1–N5)  
**Profile**: General Project  
**Integrity Mode**: Development (from `ORIGINAL_REQUEST.md` ## 2026-10-12T23:09:55Z)  
**Verdict**: **INTEGRITY VIOLATION**  

---

## 1. Observation

1. **Test Suite Failure in `HPGrasshopper.McpBridge.Tests`**:
   - Command: `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false`
   - Result:
     ```
     Failed! - Failed: 1, Passed: 655, Skipped: 0, Total: 656, Duration: 23s 008ms
     ```
   - Verbatim failure from `HPGrasshopper.McpBridge.Tests_net10.0-windows7.0_x64.log`:
     ```
     failed HPGrasshopper.McpBridge.Tests.Regression.ChallengerM1N1N2AdversarialTests.N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated (3ms)
       Xunit.Runner.InProc.SystemConsole.TestingPlatform.XunitException: Autodesk.Revit.Exceptions.ArgumentsInconsistentException : Curve length is too small for Revit's tolerance (as identified by Application.ShortCurveTolerance).
       Parameter name: endpoints
         at Autodesk.Revit.CurveAPIUtils.CreateBoundLine(XYZ endpoint1, XYZ endpoint2)
         at Autodesk.Revit.DB.Line.CreateBound(XYZ endpoint1, XYZ endpoint2)
         at HPGrasshopper.McpBridge.Tests.Regression.ChallengerM1N1N2AdversarialTests.SimulateRebarExecution(JsonElement root) in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\Regression\ChallengerM1N1N2AdversarialTests.cs:240
     ```

2. **Root Cause Analysis in Finding N1 Implementation (`generate_rebar_curves/code.cs`)**:
   - File: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs`
   - In `ParseSegments` (lines 64–78):
     ```csharp
     List<Autodesk.Revit.DB.Curve> ParseSegments(System.Text.Json.JsonElement element, double scale)
     {
         var segments = new List<Autodesk.Revit.DB.Curve>();
         foreach (var segmentItem in element.EnumerateArray())
         {
             double startX = segmentItem.GetProperty("startX").GetDouble() * scale;
             double startY = segmentItem.GetProperty("startY").GetDouble() * scale;
             double startZ = segmentItem.GetProperty("startZ").GetDouble() * scale;
             double endX = segmentItem.GetProperty("endX").GetDouble() * scale;
             double endY = segmentItem.GetProperty("endY").GetDouble() * scale;
             double endZ = segmentItem.GetProperty("endZ").GetDouble() * scale;
             segments.Add(Autodesk.Revit.DB.Line.CreateBound(new Autodesk.Revit.DB.XYZ(startX, startY, startZ), new Autodesk.Revit.DB.XYZ(endX, endY, endZ)));
         }
         return segments;
     }
     ```
   - In Phase 1 validation (lines 124–135):
     ```csharp
     var curveSegments = ParseSegments(rawItem.Value, feetPerMeter);
     if (curveSegments.Count == 0)
     {
         return new { success = false, message = $"Rebar curve set {setIndex} must contain at least one curve segment." };
     }
     for (int segIdx = 0; segIdx < curveSegments.Count; segIdx++)
     {
         if (curveSegments[segIdx].Length < 0.001)
         {
             return new { success = false, message = $"Rebar curve set {setIndex} contains a zero-length segment at index {segIdx}." };
         }
     }
     ```
   - `ParseSegments` is invoked on line 124 BEFORE the zero-length check on line 131. When an input segment has `(startX, startY, startZ) == (endX, endY, endZ)` or distance below Revit's internal `ShortCurveTolerance`, `Line.CreateBound` throws `Autodesk.Revit.Exceptions.ArgumentsInconsistentException` immediately. Line 131 is dead/unreachable for zero-length segments, crashing execution with an unhandled Revit API exception rather than returning a structured validation error.

3. **McpShared Immutability Verification**:
   - Tool check: `Get-ChildItem -Recurse -File McpShared | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } | Sort-Object LastWriteTime -Descending`
   - Result: Latest modified source file in `McpShared/` was `10/10/2026 2:29:55 PM`. Zero source modifications were introduced during Milestone M1 (October 13, 2026).
   - Status: **PASS**.

4. **Clean Code Rule Compliance**:
   - **Rule C2 (File length $\le 300$ lines)**:
     - All 17 modified C# files across the repository have $\le 300$ lines (Max is 261 lines in `CanvasUndoScope.cs`, 260 lines in `RevitTransactionScope.cs`). PASS.
   - **Rule M3 (Method length $\le 50$ lines)**:
     - 0 methods exceeding 50 lines across all modified/created files. PASS.
   - **Rule Q-B1 (0 dead / commented-out code)**:
     - 0 commented-out code statements detected. PASS.
   - **Rule Q-B2 (0 unannotated catch blocks)**:
     - 0 empty or silent catch blocks without explicit explanatory comments. PASS.
   - **ADR-0007 Script Quality Walker**:
     - All 12 embedded seeds passed `ScriptQualityTests` (`AllSeeds_PassScriptQualityWithZeroErrors`, `AllSeeds_PassScriptQualityWithZeroWarnings`) with 0 errors and 0 warnings. PASS.

5. **Behavioral Test Suite Results Across All Targets**:
   - `HPGrasshopper.Mcp.Server.Tests`: 118 passed, 0 failed, 0 skipped (100% pass).
   - `HPGrasshopper.E2E.Tests`: 181 passed, 0 failed, 0 skipped (100% pass).
   - `McpShared/HPRebar.Mcp.Server.Core.Tests`: 1047 passed, 0 failed, 0 skipped (100% pass).
   - `McpShared/HPRebar.McpBridge.Core.Net48Tests`: 115 passed, 0 failed, 0 skipped (100% pass).
   - `HPGrasshopper.McpBridge.Tests`: **655 passed, 1 failed, 0 skipped** (99.85% pass rate).

---

## 2. Logic Chain

1. **From Observation 1**: The dispatch requirement mandates: "Check test results: all tests must run filter-free with 100% pass rate." In `HPGrasshopper.McpBridge.Tests`, 1 test failed (`ChallengerM1N1N2AdversarialTests.N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated`). Therefore, the pass rate is 99.85%, failing the 100% filter-free pass requirement.
2. **From Observation 2**: The test failure was directly caused by an implementation defect in `generate_rebar_curves/code.cs` (Finding N1). While worker_m1 intended to validate zero-length segments before rebar creation, calling `Line.CreateBound` inside `ParseSegments` prior to inspecting segment coordinates causes an unhandled `ArgumentsInconsistentException` from the Revit API.
3. **From Forensic Principles**: The Forensic Auditor's protocol stipulates: "The build must succeed and tests must execute — a project that doesn't build or whose tests don't run is automatically flagged. If ANY check fails, the verdict is INTEGRITY VIOLATION and the work product must be rejected."
4. **Conclusion Follows Directly**: Because a test failed due to an authentic defect in the work product, the forensic verdict must be **INTEGRITY VIOLATION**.

---

## 3. Caveats

- **No Caveats**: The test failure was reproduced and proven directly via raw runner output in `HPGrasshopper.McpBridge.Tests_net10.0-windows7.0_x64.log`. The defect in `code.cs` was identified by exact line numbers and verified against Revit API geometry constraints.

---

## 4. Conclusion

The work product submitted for Milestone M1 (Findings N1–N5) is **REJECTED** with verdict **INTEGRITY VIOLATION**.

### Defect to Remediate:
In `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs`:
- `ParseSegments` must validate the coordinate distance `new XYZ(startX, startY, startZ).DistanceTo(new XYZ(endX, endY, endZ)) >= 0.001` before calling `Autodesk.Revit.DB.Line.CreateBound`. If any segment has distance `< 0.001`, `ParseSegments` (or its caller) must return a validation error `{ success = false, message = $"Rebar curve set {setIndex} contains a zero-length segment at index {segIdx}." }` without calling `Line.CreateBound`.

---

## 5. Verification Method

To independently reproduce this failure and verify the defect:

1. **Run the McpBridge Test Suite**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
   ```
   Observe the failure:
   `Failed! - Failed: 1, Passed: 655, Skipped: 0, Total: 656` in `ChallengerM1N1N2AdversarialTests.N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated`.

2. **Inspect the Exception in Log**:
   ```powershell
   Get-Content "HPGrasshopper/HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/TestResults/HPGrasshopper.McpBridge.Tests_net10.0-windows7.0_x64.log" | Select-String "ArgumentsInconsistentException" -Context 2,5
   ```
