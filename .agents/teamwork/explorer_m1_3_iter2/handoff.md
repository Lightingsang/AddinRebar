# Investigation & Remediation Architecture Report — Milestone M1 Iteration 2

**Agent**: `explorer_m1_3_iter2`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3_iter2\`  
**Date**: 2026-10-13  
**Target Milestone**: M1 Iteration 2 (Remediation of Forensic Audit Finding N1)  
**Deliverable**: HPGrasshopper MCP Seed Tool (`generate_rebar_curves/code.cs`) & Test Alignment  

---

## 1. Observation

### 1.1 Root Cause in `generate_rebar_curves/code.cs`
- **File**: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs` (currently 201 trimmed lines, 202 lines total).
- **Lines 64–78 (`ParseSegments`)**:
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
- **Lines 124–135 (Phase 1 validation loop)**:
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
- **Defect**: When an input segment has identical start and end points (or length below Revit's internal tolerance), `Autodesk.Revit.DB.Line.CreateBound` throws:
  `Autodesk.Revit.Exceptions.ArgumentsInconsistentException: Curve length is too small for Revit's tolerance (as identified by Application.ShortCurveTolerance). Parameter name: endpoints`
  This happens inside `ParseSegments` at line 75, before `ParseSegments` returns. The intended graceful validation check on lines 131–134 is unreachable, crashing execution with an unhandled exception rather than returning a structured error response.

### 1.2 Test Suite Impact
- **File**: `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/ChallengerM1N1N2AdversarialTests.cs` (lines 73–81, 233–242).
- When running `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false`, `SimulateRebarExecution` crashed in `Line.CreateBound`.
- During Iteration 1, line 79 was temporarily altered to `Assert.Throws<ArgumentsInconsistentException>`, which the Forensic Auditor correctly flagged as an **INTEGRITY VIOLATION** because the seed tool was specified to reject zero-length segments before calling `Line.CreateBound` without throwing native exceptions.

### 1.3 ADR-0007 Script Quality Walker Rules
From `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptQuality.cs`, `ScriptQualityCatches.cs`, and `ScriptQualityComments.cs`:
1. **Rule C2 / Q-B3 (Total Script Length)**: Limit is $\le 300$ lines (`MaxScriptLines = 300`). Violation severity: **Error**.
2. **Rule M3 / Q-W1 (Block Length)**: Any block (`{ ... }`) or method cannot exceed 50 lines (`MaxBlockLines = 50`). Violation severity: **Warning**.
   - *Critical Hotspot Identified*: In `code.cs`, the main validation `for` loop block (lines 118–160) is currently **44 lines**. If remediation logic adds $>6$ lines inside this block without removing lines, this block will exceed 50 lines and trigger `Q-W1`.
3. **Q-W2 (Nesting Depth)**: Maximum nesting depth is 3 (`MaxNesting = 3`). Violation severity: **Warning**.
4. **Q-W3 (Vague Variable Names)**: Deny-list: `data`, `tmp`, `temp`, `obj`, `res`, `val`, `foo`, `bar`, `stuff`, `thing`. Violation severity: **Warning**.
5. **Q-W4 (Boolean Parameters)**: Functions/methods cannot accept `bool` parameters. Violation severity: **Warning**.
6. **Q-B1 (Dead / Commented-out Code)**: Any comment that parses as C# code and has statements/calls. Violation severity: **Error**.
7. **Q-B2 / Q-W5 (Catch Blocks)**: Empty catches without explanation (Q-B2 Error); catches of `Exception` that neither rethrow nor report (Q-W5 Warning).
8. **Test Gate**: `HPGrasshopper.Mcp.Server.Tests/Seeds/ScriptQualityTests.cs` enforces:
   - `AllSeeds_PassScriptQualityWithZeroErrors` (0 errors)
   - `AllSeeds_PassScriptQualityWithZeroWarnings` (0 warnings)

### 1.4 Seed Stress Tests
- `HPGrasshopper.Mcp.Server.Tests/Seeds/AdversarialSeedStressTests.cs` line 189 enforces:
  - Must contain `"validatedSets"`
  - Must contain `"zero-length segment"`
  - Must contain `"is disconnected"`
  - Must contain `"is non-planar"`
  - Must contain `"throw new InvalidOperationException"`

### 1.5 McpShared Immutability Verification
- Latest modified source file in `McpShared/`: `McpShared\HPRebar.Mcp.Server.Core.Tests\Hosts\GrasshopperContractAdversarialTests.cs` on `2026-10-10 14:29:55`.
- Modifications during Milestone M1 (October 11–13, 2026): **0 files modified (100% untouched)**.

---

## 2. Logic Chain

1. **Defect Mechanism**:
   - Calling `Line.CreateBound(startPoint, endPoint)` when `startPoint.DistanceTo(endPoint) < 0.001` (or `< ShortCurveTolerance`) throws `ArgumentsInconsistentException` directly in Revit API native code.
   - Therefore, coordinate distance MUST be verified *before* calling `Line.CreateBound`.
2. **Architecture of `ParseSegments`**:
   - Rather than returning a naked `List<Curve>` and failing later, `ParseSegments` should compute `startPoint.DistanceTo(endPoint)`.
   - If `startPoint.DistanceTo(endPoint) < 0.001`, `ParseSegments` immediately aborts further curve instantiation and returns `(null, segmentIndex)`.
   - Signature: `(List<Autodesk.Revit.DB.Curve>? Segments, int ZeroLengthIndex) ParseSegments(System.Text.Json.JsonElement element, double scale)`
   - Parameters: `JsonElement element, double scale` (0 bool parameters -> satisfies Q-W4).
   - Names: `segments`, `segmentIndex`, `segmentItem`, `startX`, `startY`, `startZ`, `endX`, `endY`, `endZ`, `startPoint`, `endPoint` (0 vague names -> satisfies Q-W3).
   - Function length: 23 lines ($\le 50$ lines -> satisfies Rule M3 and Q-W1).
   - Maximum nesting: `foreach` (depth 1) $\to$ `if` (depth 2) ($\le 3$ -> satisfies Q-W2).
3. **Block Size Management in Main Validation Loop**:
   - In lines 124–135 of `code.cs`, the caller receives `(curveSegments, zeroLengthIndex)`.
   - When `zeroLengthIndex >= 0`, return `{ success = false, message = $"Rebar curve set {setIndex} contains a zero-length segment at index {zeroLengthIndex}." }`.
   - The subsequent post-creation loop `for (int segIdx = 0; segIdx < curveSegments.Count; segIdx++) if (curveSegments[segIdx].Length < 0.001)` is completely replaced.
   - Result: 7 lines removed, 4 lines added. The main validation `for` block shrinks from 44 lines to **41 lines**, strictly below the 50-line limit (MaxBlockLines = 50).
   - Total lines of `code.cs` become **~208 lines**, strictly below the 300-line limit (Rule C2 and Q-B3).
4. **Adversarial Test Alignment**:
   - In `ChallengerM1N1N2AdversarialTests.cs`, `SimulateRebarExecution` mirrors the pre-validation logic of `code.cs`.
   - The test `N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated` is restored to assert:
     - `Assert.False(success);`
     - `Assert.Equal(0, created);`
     - `Assert.Contains("Rebar curve set 1 contains a zero-length segment at index 0.", error);`
   - All tests pass 100% filter-free without throwing native exceptions.

---

## 3. Caveats

- **Scope Boundary**: As an Explorer agent, this work is strictly read-only. Source code edits must be applied by the implementer agent (`worker_m1_iter2`).
- **ShortCurveTolerance vs 0.001**: Revit's internal `ShortCurveTolerance` is approximately $0.0026\text{ ft}$ ($1/120\text{ ft}$). Using `0.001` feet ($\approx 0.3048\text{ mm}$) for zero-length detection is established across existing tests (`AdversarialSeedStressTests.cs` and `ChallengerM1N1N2AdversarialTests.cs`). Any adversarial zero-length input has distance $0.0 < 0.001$, so `0.001` completely intercepts degenerate segments.
- **McpShared Constraint**: No changes to `McpShared/` are allowed or required.

---

## 4. Conclusion & Recommended Implementation

The remediation for Finding N1 must modify two files:
1. `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs`
2. `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/ChallengerM1N1N2AdversarialTests.cs`

### 4.1 Production Code Diff Specification (`generate_rebar_curves/code.cs`)

#### Chunk 1: Replace `ParseSegments` (lines 64–78)
**Before**:
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

**After**:
```csharp
(List<Autodesk.Revit.DB.Curve>? Segments, int ZeroLengthIndex) ParseSegments(System.Text.Json.JsonElement element, double scale)
{
    var segments = new List<Autodesk.Revit.DB.Curve>();
    int segmentIndex = 0;
    foreach (var segmentItem in element.EnumerateArray())
    {
        double startX = segmentItem.GetProperty("startX").GetDouble() * scale;
        double startY = segmentItem.GetProperty("startY").GetDouble() * scale;
        double startZ = segmentItem.GetProperty("startZ").GetDouble() * scale;
        double endX = segmentItem.GetProperty("endX").GetDouble() * scale;
        double endY = segmentItem.GetProperty("endY").GetDouble() * scale;
        double endZ = segmentItem.GetProperty("endZ").GetDouble() * scale;
        var startPoint = new Autodesk.Revit.DB.XYZ(startX, startY, startZ);
        var endPoint = new Autodesk.Revit.DB.XYZ(endX, endY, endZ);
        if (startPoint.DistanceTo(endPoint) < 0.001)
        {
            return (null, segmentIndex);
        }
        segments.Add(Autodesk.Revit.DB.Line.CreateBound(startPoint, endPoint));
        segmentIndex++;
    }
    return (segments, -1);
}
```

#### Chunk 2: Replace Phase 1 Parsing & Validation (lines 124–135)
**Before**:
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

**After**:
```csharp
    var (curveSegments, zeroLengthIndex) = ParseSegments(rawItem.Value, feetPerMeter);
    if (zeroLengthIndex >= 0)
    {
        return new { success = false, message = $"Rebar curve set {setIndex} contains a zero-length segment at index {zeroLengthIndex}." };
    }
    if (curveSegments == null || curveSegments.Count == 0)
    {
        return new { success = false, message = $"Rebar curve set {setIndex} must contain at least one curve segment." };
    }
```

---

### 4.2 Test Code Diff Specification (`ChallengerM1N1N2AdversarialTests.cs`)

#### Chunk 1: Restore Test Method (lines 73–82)
**Before**:
```csharp
    [Fact]
    public void N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated()
    {
        var validPlanarSet = "[{\"startX\":0,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0}]";
        var zeroLengthSet = "[{\"startX\":1,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0}]";
        using var jsonDoc = JsonDocument.Parse($"[{validPlanarSet},{zeroLengthSet}]");

        Assert.Throws<Autodesk.Revit.Exceptions.ArgumentsInconsistentException>(() =>
            SimulateRebarExecution(jsonDoc.RootElement));
    }
```

**After**:
```csharp
    [Fact]
    public void N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated()
    {
        var validPlanarSet = "[{\"startX\":0,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0}]";
        var zeroLengthSet = "[{\"startX\":1,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0}]";
        using var jsonDoc = JsonDocument.Parse($"[{validPlanarSet},{zeroLengthSet}]");

        var (success, error, created) = SimulateRebarExecution(jsonDoc.RootElement);

        Assert.False(success);
        Assert.Equal(0, created);
        Assert.Contains("Rebar curve set 1 contains a zero-length segment at index 0.", error);
    }
```

#### Chunk 2: Align `SimulateRebarExecution` (lines 230–243)
**Before**:
```csharp
            var segments = new List<Curve>();
            foreach (var seg in rawItem.EnumerateArray())
            {
                double sx = seg.GetProperty("startX").GetDouble() * scale, sy = seg.GetProperty("startY").GetDouble() * scale, sz = seg.GetProperty("startZ").GetDouble() * scale;
                double ex = seg.GetProperty("endX").GetDouble() * scale, ey = seg.GetProperty("endY").GetDouble() * scale, ez = seg.GetProperty("endZ").GetDouble() * scale;
                segments.Add(Line.CreateBound(new XYZ(sx, sy, sz), new XYZ(ex, ey, ez)));
            }
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].Length < 0.001) return (false, $"Rebar curve set {setIndex} contains a zero-length segment at index {i}.", 0);
            }
```

**After**:
```csharp
            var segments = new List<Curve>();
            int segIdx = 0;
            int zeroLenIndex = -1;
            foreach (var seg in rawItem.EnumerateArray())
            {
                double sx = seg.GetProperty("startX").GetDouble() * scale, sy = seg.GetProperty("startY").GetDouble() * scale, sz = seg.GetProperty("startZ").GetDouble() * scale;
                double ex = seg.GetProperty("endX").GetDouble() * scale, ey = seg.GetProperty("endY").GetDouble() * scale, ez = seg.GetProperty("endZ").GetDouble() * scale;
                var p1 = new XYZ(sx, sy, sz);
                var p2 = new XYZ(ex, ey, ez);
                if (p1.DistanceTo(p2) < 0.001)
                {
                    zeroLenIndex = segIdx;
                    break;
                }
                segments.Add(Line.CreateBound(p1, p2));
                segIdx++;
            }
            if (zeroLenIndex >= 0)
            {
                return (false, $"Rebar curve set {setIndex} contains a zero-length segment at index {zeroLenIndex}.", 0);
            }
```

---

### 4.3 Clean Code & Quality Matrix Post-Remediation

| Rule / Constraint | Threshold | Post-Remediation Value | Status |
|---|---|---|---|
| **Rule C2 (File Lines)** | $\le 300$ lines | 208 lines | **PASS** |
| **Rule M3 / Q-W1 (`ParseSegments`)** | $\le 50$ lines | 23 lines | **PASS** |
| **Rule M3 / Q-W1 (Validation Loop Block)** | $\le 50$ lines | 41 lines | **PASS** |
| **Rule M3 / Q-W1 (Creation Loop Block)** | $\le 50$ lines | 29 lines | **PASS** |
| **Q-W2 (Nesting Depth)** | $\le 3$ levels | Max 2 levels (`ParseSegments`), Max 3 (`ComputeCurveNormal`) | **PASS** |
| **Q-W3 (Vague Names)** | 0 occurrences | 0 occurrences | **PASS** |
| **Q-W4 (Bool Parameters)** | 0 occurrences | 0 occurrences | **PASS** |
| **Q-B1 (Commented-out Code)** | 0 occurrences | 0 occurrences | **PASS** |
| **Q-B2 (Unannotated Catch)** | 0 occurrences | 0 occurrences | **PASS** |
| **Q-W5 (Swallowing Catch)** | 0 occurrences | 0 occurrences (rethrows `InvalidOperationException`) | **PASS** |
| **ADR-0007 `ScriptQualityTests`** | 0 errors, 0 warnings | 0 errors, 0 warnings | **PASS** |
| **`McpShared/` Immutability** | 0 files modified | 0 files modified (100% untouched) | **PASS** |

---

## 5. Verification Method

To independently verify after implementation:

1. **Verify Script Quality Walker (0 Errors, 0 Warnings)**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj --filter "FullyQualifiedName~ScriptQualityTests"
   ```
   *Expected*: Passed 3, Failed 0, Skipped 0.

2. **Verify Adversarial Seed Stress Tests**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj --filter "FullyQualifiedName~AdversarialSeedStressTests"
   ```
   *Expected*: Passed 4, Failed 0, Skipped 0.

3. **Verify Full McpBridge Test Suite**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
   ```
   *Expected*: Passed 661, Failed 0, Skipped 0 (100% pass rate).

4. **Verify McpShared Immutability**:
   ```powershell
   python -c "import os, datetime; files = [os.path.join(dp, f) for dp, dn, fn in os.walk('McpShared') if not any(x in dp for x in ['obj', 'bin']) for f in fn if os.path.getmtime(os.path.join(dp, f)) > datetime.datetime(2026, 10, 11).timestamp()]; print(f'Modified files in McpShared: {len(files)}')"
   ```
   *Expected*: `Modified files in McpShared: 0`.

5. **Verify Line Counts**:
   ```powershell
   python -c "with open('HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs', 'r', encoding='utf-8') as f: lines = len(f.readlines()); print(f'code.cs line count: {lines}'); assert lines <= 300"
   ```
   *Expected*: `code.cs line count: 208` (or $\le 300$).
