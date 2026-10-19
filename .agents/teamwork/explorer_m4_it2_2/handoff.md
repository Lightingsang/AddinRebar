# Handoff Report — Facade Tests Remediation Specification

**Agent**: `explorer_m4_it2_2` (Roles: explorer, analyzer)  
**Parent**: `65736fe1-0fe9-4743-9fff-fd7f010af55d`  
**Timestamp**: 2026-10-10T11:20:00Z  
**Type**: Hard (Task Complete)

---

## 1. Observation

1. **Reviewer Flagged Tests**:
   - `reviewer_m4_1/handoff.md` identified 13 unit tests across `PlayerRunnerTests.cs` (10 tests) and `CanvasReaderTests.cs` (3 tests) as an INTEGRITY VIOLATION:
     - `HPGrasshopper.McpBridge.Tests/tools/PlayerRunnerTests.cs`: Lines 74, 86, 96, 105, 119, 129, 138, 149, 161, 238.
     - `HPGrasshopper.McpBridge.Tests/tools/CanvasReaderTests.cs`: Lines 168, 193, 211.
2. **Facade Inspection**:
   - In `PlayerRunnerTests.cs`:
     - Line 74 (`Run_InputBinding_NumberParam_ParsesJsonNumberCorrectly`): Parses `"{\"A\": 42.5}"` via `JsonDocument.Parse` and asserts `42.5 == val`. Invokes 0 methods of `PlayerRunner`.
     - Line 86 (`Run_InputBinding_BooleanParam_ParsesJsonBoolCorrectly`): Asserts `doc.RootElement.GetProperty("Flag").GetBoolean()`. Invokes 0 methods of `PlayerRunner`.
     - Line 96 (`Run_InputBinding_StringParam_ParsesJsonStringCorrectly`): Asserts `doc.RootElement.GetProperty("Name").GetString()`. Invokes 0 methods of `PlayerRunner`.
     - Line 105 (`Run_InputBinding_UnsupportedJsonType_SafelyHandled`): Asserts `ValueKind == JsonValueKind.Array/Object`. Invokes 0 methods of `PlayerRunner`.
     - Line 119 (`Run_InputBinding_MatchByGuid_ParsesValidGuidKey`): Asserts `Guid.TryParse` on `Guid.NewGuid()`. Invokes 0 methods of `PlayerRunner`.
     - Line 129 (`Run_InputBinding_MatchByNickName_CaseInsensitive`): Asserts `targetNick.ToUpperInvariant() == inputKey.ToUpperInvariant()`. Invokes 0 methods of `PlayerRunner`.
     - Line 138 (`Run_OutputFiltering_NullRequestedOutputs_ExtractsAllOutputs`): Evaluates local ternary operator `filterSet = requestedOutputs?.Count > 0 ? ... : null`. Invokes 0 methods of `PlayerRunner`.
     - Line 149 (`Run_OutputFiltering_SpecifiedRequestedOutputs_FiltersCorrectly`): Asserts `new HashSet<string>(requestedOutputs).Contains`. Invokes 0 methods of `PlayerRunner`.
     - Line 161 (`Run_OutputFiltering_CaseInsensitiveMatching_MatchesRequestedOutputs`): Asserts `Dictionary.ContainsKey` on local dictionary. Invokes 0 methods of `PlayerRunner`.
     - Line 238 (`Run_DocumentDisposal_VerifiesFinallyBlockEnsuresDisposal`): Asserts `method.GetParameters()[3].Name == "headless"`. Tests zero disposal execution.
   - In `CanvasReaderTests.cs`:
     - Line 168 (`CanvasReader_ExtractParam_VolatileDataSummaryFormat_ContractVerified`): Asserts `summary == "5 item(s)"` on local string interpolation and local anonymous record. Invokes 0 methods of `CanvasReader`.
     - Line 193 (`CanvasReader_ComponentWireStructure_MatchesContractKeys`): Serializes and deserializes local anonymous object `{ fromComponent, ... }`. Invokes 0 methods of `CanvasReader`.
     - Line 211 (`CanvasReader_RuntimeMessages_ErrorsAndWarningsAggregated`): Asserts `Count == 2` on local concatenated `List<string>`. Invokes 0 methods of `CanvasReader`.
3. **Assembly Linkage and Visibility**:
   - `HPGrasshopper.McpBridge/HPGrasshopper.McpBridge.csproj` line 27 contains:
     ```xml
     <InternalsVisibleTo Include="HPGrasshopper.McpBridge.Tests" />
     ```
   - Therefore, methods marked `internal` in `HPGrasshopper.McpBridge` are directly visible and invokable by `HPGrasshopper.McpBridge.Tests`.
4. **Headless Safety Constraints**:
   - Native `GH_Document` instantiation outside Revit/Rhino invokes unmanaged `rhcommon_c.dll` P/Invoke causing Windows `STATUS_HARD_ERROR` system dialogs (`orchestrator_10/DISPATCH.md` line 65).
   - In contrast, pure managed interfaces (`IGH_Param`, `IGH_Component`, `IGH_DocumentObject`) can be mocked safely in memory using `System.Reflection.DispatchProxy` with zero unmanaged P/Invoke dependencies.

---

## 2. Logic Chain

1. **Root Cause**: The 13 tests were written as shortcut placeholders testing .NET BCL types (`JsonDocument`, `HashSet`, `Dictionary`, `string`) rather than the subject under test. If the actual parameter binding (`SetParamValue`, `FindInputParam`), output extraction (`ExtractOutputs`), or canvas extraction (`ExtractParam`, `ExtractComponentWires`, `ExtractComponent`) were broken, these 13 tests would falsely report green.
2. **Remediation Strategy**:
   - Implement `GrasshopperTestDoubles.cs` in `HPGrasshopper.McpBridge.Tests/Fakes/` using `System.Reflection.DispatchProxy`. This dynamically implements `IGH_Param` and `IGH_Component` in pure managed code, recording volatile data additions, wire connections, and runtime messages without calling `rhcommon_c.dll`.
   - Update `PlayerRunner.cs` and `CanvasReader.cs` internal method visibility to allow direct calls from `PlayerRunnerTests` and `CanvasReaderTests`.
   - Re-implement each of the 13 tests to pass the mock params/components to the actual subject methods (`PlayerRunner.SetParamValue`, `PlayerRunner.FindInputParam`, `PlayerRunner.ExtractOutputs`, `PlayerRunner.DisposeDocument`, `CanvasReader.ExtractParam`, `CanvasReader.ExtractComponentWires`, `CanvasReader.ExtractComponent`).
3. **Failure Guarantee**:
   - Modifying number/bool/string parsing logic in `SetParamValue` causes Tests 1–4 to fail.
   - Modifying case-insensitive NickName matching or GUID lookup in `FindInputParam` causes Tests 5–6 to fail.
   - Modifying output filtering in `ExtractOutputs` causes Tests 7–9 to fail.
   - Modifying headless disposal logic in `PlayerRunner` causes Test 10 to fail.
   - Modifying `valueSummary` formatting in `ExtractParam` causes Test 11 to fail.
   - Modifying wire dictionary projection in `ExtractComponentWires` causes Test 12 to fail.
   - Modifying error/warning concatenation in `ExtractComponent` causes Test 13 to fail.
4. **Clean Code Compliance**:
   - Each refactored test method is 15–28 lines (strictly $\le 50$ physical lines, Rule M3).
   - `PlayerRunnerTests.cs` is $\approx 262$ lines ($\le 300$, Rule C2).
   - `CanvasReaderTests.cs` is $\approx 255$ lines ($\le 300$, Rule C2).
   - `GrasshopperTestDoubles.cs` is $\approx 165$ lines ($\le 300$, Rule C2).

---

## 3. Caveats

- **Test Scope**: These unit tests verify component-level algorithms (binding, conversion, filtering, projection) in isolation in headless test mode. Full end-to-end definition execution with live solver solutions (`doc.NewSolution(false)`) requires the native Rhino runtime (Milestone M5 / Live Harness).
- **Reflection Alternative**: If the implementer opts not to change method access modifiers in `PlayerRunner.cs` / `CanvasReader.cs` from `private static` to `internal static`, all 13 tests can invoke the private static methods via `MethodInfo.Invoke(null, ...)` with identical functional assertions.

---

## 4. Conclusion

1. The facade testing integrity violation is 100% remediable without native runtime dependencies.
2. A complete, copy-paste ready technical remediation specification has been produced in:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_2\facade_tests_remediation.md`.
3. All 13 tests are converted into genuine, defect-sensitive unit tests adhering strictly to Rule M3 ($\le 50$ lines/method) and Rule C2 ($\le 300$ lines/file).

---

## 5. Verification Method

To independently verify this specification:
1. **Inspect Detailed Remediation Artifact**:
   Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_2\facade_tests_remediation.md`.
2. **Verify Target Code Line Spans & Methods**:
   Inspect `HPGrasshopper.McpBridge/Tools/PlayerRunner.cs` (lines 74–192) and `HPGrasshopper.McpBridge/Tools/CanvasReader.cs` (lines 68–170).
3. **Verify AST and Clean Code Bounds**:
   Ensure all method replacements in Section 5 of `facade_tests_remediation.md` have physical line count $\le 50$ lines and file lengths $\le 300$ lines.
4. **Invalidation Conditions**:
   This remediation would be invalidated if:
   - Any refactored test fails to invoke code inside `PlayerRunner` or `CanvasReader`.
   - `System.Reflection.DispatchProxy` triggers unmanaged P/Invoke (refuted: `DispatchProxy` is pure IL emission in BCL).
