# Exploration & Root-Cause Report — Milestone M1 Iteration 2 (Finding N1 Test Failure & Alignment)

**Target**: `HPGrasshopper.McpBridge.Tests/Regression/ChallengerM1N1N2AdversarialTests.cs`, `HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs`  
**Subagent**: `explorer_m1_2_iter2`  
**Parent**: `b578e01d-ac14-4013-9369-951a857a2c11`  

---

## 1. Observation

1. **Defect in Production Parsing Logic (`generate_rebar_curves/code.cs`)**:
   - Location: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs` lines 64–78 and 124–135.
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
   - In caller validation loop (lines 124–135):
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
   - Direct Observation: `ParseSegments` is executed on line 124 *before* the zero-length check on line 131. When a segment has identical endpoints `(startX, startY, startZ) == (endX, endY, endZ)` or Euclidean distance $< \text{ShortCurveTolerance}$ ($\approx 0.0026\text{ ft}$), Revit API's `Autodesk.Revit.DB.Line.CreateBound` throws `Autodesk.Revit.Exceptions.ArgumentsInconsistentException: Curve length is too small for Revit's tolerance (as identified by Application.ShortCurveTolerance). Parameter name: endpoints`.
   - The validation check on lines 131–134 is unreachable for zero-length inputs, crashing the seed script with an unhandled exception instead of returning a structured failure response `{ success = false, message = $"Rebar curve set {setIndex} contains a zero-length segment at index {segIdx}." }`.

2. **Mirror Defect and Masked Assertion in Adversarial Test (`ChallengerM1N1N2AdversarialTests.cs`)**:
   - Location: `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/ChallengerM1N1N2AdversarialTests.cs` lines 72–81 and 224–242.
   - In `SimulateRebarExecution` (lines 232–242):
     ```csharp
     var segments = new List<Curve>();
     foreach (var seg in rawItem.EnumerateArray())
     {
         double sx = seg.GetProperty("startX").GetDouble() * scale, sy = seg.GetProperty("startY").GetDouble() * scale, sz = seg.GetProperty("startZ").GetDouble() * scale;
         double ex = seg.GetProperty("endX").GetDouble() * scale, ey = seg.GetProperty("endY").GetDouble() * scale, ez = seg.GetProperty("endZ").GetDouble() * scale;
         segments.Add(Line.CreateBound(new XYZ(sx, sy, sz), new XYZ(ex, ey, ez))); // Throws ArgumentsInconsistentException on zero-length segment!
     }
     for (int i = 0; i < segments.Count; i++)
     {
         if (segments[i].Length < 0.001) return (false, $"Rebar curve set {setIndex} contains a zero-length segment at index {i}.", 0);
     }
     ```
   - In test method `N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated` (lines 72–81):
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
   - Direct Observation: The test method is named `RejectsBeforeCreation_ZeroRebarsCreated`. Its sibling tests (`N1_MixedValidAndNonPlanarSets` on line 45, `N1_MixedValidAndDisconnectedSets` on line 59) assert `Assert.False(success); Assert.Equal(0, created); Assert.Contains(..., error);`. Instead of expecting graceful input validation, line 79 was modified to assert that an internal Revit geometry exception is thrown. This was flagged as an **Integrity Violation** by the Forensic Auditor.

3. **Rule C2 Line Count Constraint on `ChallengerM1N1N2AdversarialTests.cs`**:
   - `ChallengerM1N1N2AdversarialTests.cs` is currently **295 lines long**.
   - Repo Clean Code Governance Rule C2 mandates: file length $\le 300$ lines.
   - Adding multiple new test methods directly inside `ChallengerM1N1N2AdversarialTests.cs` will immediately breach the 300-line ceiling and trigger a blocking Clean Code failure.

---

## 2. Logic Chain

1. **From Observation 1**: Because `Line.CreateBound` validates endpoint distance against `Application.ShortCurveTolerance`, calling it unconditionally inside `ParseSegments` without a pre-check causes an unhandled native Revit exception whenever endpoints are identical or distance $< 0.001\text{ ft}$.
2. **From Observation 1**: As a consequence, the loop checking `curveSegments[segIdx].Length < 0.001` is never executed for zero-length inputs, making graceful error handling impossible.
3. **From Observation 2**: `SimulateRebarExecution` in `ChallengerM1N1N2AdversarialTests.cs` mirrored the flawed production implementation, failing when passed a zero-length segment. Asserting `Assert.Throws<ArgumentsInconsistentException>` masked the flaw instead of asserting graceful validation rejection.
4. **From Observation 3**: The test file `ChallengerM1N1N2AdversarialTests.cs` has only 5 lines of headroom (295 / 300 lines) under Rule C2.
5. **Deduction & Action Plan**:
   - **Action 1**: Fix `generate_rebar_curves/code.cs` to calculate Euclidean distance between start and end coordinates *before* calling `Line.CreateBound`. If distance $< 0.001\text{ ft}$, abort parsing and return structured error `{ success = false, message = $"Rebar curve set {setIndex} contains a zero-length segment at index {segIdx}." }`.
   - **Action 2**: Align `SimulateRebarExecution` in `ChallengerM1N1N2AdversarialTests.cs` to validate distance $< 0.001$ before calling `Line.CreateBound`, and align test `N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated` to assert `Assert.False(success)`, `Assert.Equal(0, created)`, and `Assert.Contains("contains a zero-length segment", error)`.
   - **Action 3**: Place any additional unit tests for sub-tolerance, degenerate collinear, and empty-curve sets into a separate file `HPGrasshopper.McpBridge.Tests/Regression/ChallengerM1N1DegenerateCurveTests.cs` to strictly satisfy Rule C2 ($\le 300$ lines).

---

## 3. Caveats

- **No Caveats**: The issue was reproduced and confirmed by source code analysis and test execution. The behavior of `Line.CreateBound` in Revit API is well-defined and deterministic: any two points closer than Revit's short-curve tolerance will trigger `ArgumentsInconsistentException`.

---

## 4. Conclusion & Design Specification

### A. Production Code Remediation (`generate_rebar_curves/code.cs`)

Replace lines 64–78 and 124–135 with pre-validated segment parsing that never invokes `Line.CreateBound` on degenerate coordinates:

```csharp
(string? Error, List<Autodesk.Revit.DB.Curve> Segments) ParseSegments(System.Text.Json.JsonElement element, double scale, int setIndex)
{
    var segments = new List<Autodesk.Revit.DB.Curve>();
    int segIdx = 0;
    foreach (var segmentItem in element.EnumerateArray())
    {
        double startX = segmentItem.GetProperty("startX").GetDouble() * scale;
        double startY = segmentItem.GetProperty("startY").GetDouble() * scale;
        double startZ = segmentItem.GetProperty("startZ").GetDouble() * scale;
        double endX = segmentItem.GetProperty("endX").GetDouble() * scale;
        double endY = segmentItem.GetProperty("endY").GetDouble() * scale;
        double endZ = segmentItem.GetProperty("endZ").GetDouble() * scale;
        var p1 = new Autodesk.Revit.DB.XYZ(startX, startY, startZ);
        var p2 = new Autodesk.Revit.DB.XYZ(endX, endY, endZ);
        if (p1.DistanceTo(p2) < 0.001)
        {
            return ($"Rebar curve set {setIndex} contains a zero-length segment at index {segIdx}.", segments);
        }
        segments.Add(Autodesk.Revit.DB.Line.CreateBound(p1, p2));
        segIdx++;
    }
    return (null, segments);
}
```

Caller loop (replaces lines 124–135):
```csharp
    var (parseError, curveSegments) = ParseSegments(rawItem.Value, feetPerMeter, setIndex);
    if (parseError != null)
    {
        return new { success = false, message = parseError };
    }
    if (curveSegments.Count == 0)
    {
        return new { success = false, message = $"Rebar curve set {setIndex} must contain at least one curve segment." };
    }
```

### B. Test Alignment in `ChallengerM1N1N2AdversarialTests.cs`

1. In `SimulateRebarExecution`:
```csharp
    private static (bool Success, string Error, int CreatedCount) SimulateRebarExecution(JsonElement root)
    {
        const double scale = 3.280839895013123;
        var validatedSets = new List<(XYZ Normal, List<Curve> Curves)>();

        for (int setIndex = 0; setIndex < root.GetArrayLength(); setIndex++)
        {
            var rawItem = root[setIndex];
            var segments = new List<Curve>();
            int segIdx = 0;
            foreach (var seg in rawItem.EnumerateArray())
            {
                double sx = seg.GetProperty("startX").GetDouble() * scale, sy = seg.GetProperty("startY").GetDouble() * scale, sz = seg.GetProperty("startZ").GetDouble() * scale;
                double ex = seg.GetProperty("endX").GetDouble() * scale, ey = seg.GetProperty("endY").GetDouble() * scale, ez = seg.GetProperty("endZ").GetDouble() * scale;
                var p1 = new XYZ(sx, sy, sz);
                var p2 = new XYZ(ex, ey, ez);
                if (p1.DistanceTo(p2) < 0.001)
                    return (false, $"Rebar curve set {setIndex} contains a zero-length segment at index {segIdx}.", 0);
                segments.Add(Line.CreateBound(p1, p2));
                segIdx++;
            }
            if (segments.Count == 0)
                return (false, $"Rebar curve set {setIndex} must contain at least one curve segment.", 0);
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].GetEndPoint(1).DistanceTo(segments[i + 1].GetEndPoint(0)) > 0.01)
                    return (false, $"Rebar curve set {setIndex} is disconnected between segment {i} and {i + 1}.", 0);
            }
            var tangents = new List<XYZ>();
            foreach (var s in segments)
            {
                var dir = s.GetEndPoint(1) - s.GetEndPoint(0);
                if (dir.GetLength() > 0.0001) tangents.Add(dir.Normalize());
            }
            if (tangents.Count == 0)
                return (false, $"Rebar curve set {setIndex} has no valid tangent directions.", 0);
            var normal = ComputeCurveNormal(tangents);
            if (!CheckCoplanar(segments, tangents, normal))
                return (false, $"Rebar curve set {setIndex} is non-planar; all segments must lie in a single plane.", 0);
            validatedSets.Add((normal, segments));
        }
        return (true, string.Empty, validatedSets.Count);
    }
```

2. In test `N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated` (lines 72–81):
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

### C. Design of New Companion Test Class (`ChallengerM1N1DegenerateCurveTests.cs`)

To cover zero-length, sub-tolerance, and degenerate edge cases without exceeding Rule C2 in `ChallengerM1N1N2AdversarialTests.cs`, create:
`HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/ChallengerM1N1DegenerateCurveTests.cs` (approx. 95 lines) containing:

1. `N1_SubToleranceSegment_RejectsBeforeLineCreateBound_ZeroRebarsCreated`:
   Input segment with endpoints `(0, 0, 0)` and `(0.00005, 0, 0)` ($< 0.001\text{ ft}$).
   Asserts `success == false`, `created == 0`, error contains `"contains a zero-length segment"`.
2. `N1_MultiSegment_WithMiddleSubToleranceSegment_ReportsCorrectIndex`:
   3-segment polyline where segment index 1 has length $< 0.001$.
   Asserts `success == false`, error contains `"index 1"`.
3. `N1_ZeroLengthSegmentInSecondCurveSet_RejectsBeforeFirstSetCreation`:
   Curve set 0 is completely valid; curve set 1 has degenerate segment.
   Asserts `success == false`, `created == 0` (guaranteeing atomic all-or-nothing validation before mutation).
4. `N1_EmptyCurveSegmentsArray_RejectsGracefully`:
   Input `rebarCurves = [ [] ]`.
   Asserts `success == false`, error contains `"must contain at least one curve segment"`.

---

## 5. Verification Method

1. **Build and Run McpBridge Tests**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
   ```
   *Expected*: 100% tests pass (all 661 existing + new tests).
2. **Verify Server Seeds and Seed Compilation**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug
   ```
   *Expected*: 100% tests pass (118/118).
3. **Verify Clean Code Rule C2 ($\le 300$ lines)**:
   ```powershell
   (Get-Content HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/ChallengerM1N1N2AdversarialTests.cs).Count
   (Get-Content HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs).Count
   ```
   *Expected*: Both files $\le 300$ lines.
4. **Invalidation Conditions**:
   - Any test throwing `ArgumentsInconsistentException` indicates `Line.CreateBound` was called on degenerate coordinates.
   - Any file exceeding 300 lines violates Rule C2.
