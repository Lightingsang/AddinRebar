# Milestone M4: Bridge Unit Tests & Test Fixtures Specification

## 1. Executive Summary

This specification defines the comprehensive unit test suites and test fixtures for Milestone M4 of the **HPGrasshopper MCP Subsystem**, satisfying Requirement **R5** from `ORIGINAL_REQUEST.md` (section `## 2026-10-10T06:56:11Z`) and Milestone M4 of `PROJECT.md`.

Currently, `HPGrasshopper.McpBridge.Tests` contains **54 tests** focused primarily on Coordinator concurrency, threading stress, basic script globals, and simulated Revit transactions. Crucial bridge operational surfaces remain completely uncovered:
1. **Canvas Serialization & Deserialization** (`CanvasReader` and `CanvasEditor`): 0 tests currently verify canvas reading, component/param extraction, bounds, or wire topology extraction.
2. **Parameter Binding & Input Validation** (`PlayerRunner`): 0 tests currently verify headless definition loading, type conversion (number, bool, string), or output filtering.
3. **Dual Safety Scopes Execution & Rollback**: `CanvasUndoScope` has only reflection and null checks; no test currently verifies end-to-end component re-addition, wire reconnect LIFO restoration, or joint Level 1 (Revit) + Level 2 (Canvas) rollback synchronization.

This plan details the design of **52 new unit tests** across 4 dedicated test classes, along with 3 standard test fixtures (`MathSlider`, `CurveGenerator`, `RevitDirectShapeSyncMock`) provided in both `.ghx` and `.gh` formats.

---

## 2. Audit of Existing Test Suites (54 Tests)

The existing test suite in `HPGrasshopper/HPGrasshopper.McpBridge.Tests/` targets `net10.0-windows7.0` using xUnit v3 and the Microsoft.Testing.Platform runner. All 54 tests currently pass in ~7.4s.

| Test Class | Existing Test Count | Feature Areas Tested | Limitations / Gaps |
|------------|:-------------------:|----------------------|--------------------|
| `Coordinator/GrasshopperBridgeCoordinatorTests.cs` | 12 | ExternalEvent enqueueing, cancellation tokens, single-flight busy state, error propagation, compiler lazy init | Does not test actual tool execution logic; uses mock raisers and execution hooks |
| `Coordinator/ThreadingAndExecutionStressTests.cs` | 9 | Concurrent work requests (100 items), single-flight hammer (50 calls), sequential rapid runs, token cancellation correlation | Concurrency stress only; no Grasshopper domain objects manipulated |
| `Execution/RevitTransactionScopeTests.cs` | 15 | Null UIApp check, ReadOnly/Family/Modifiable document validation, dryRun true/false with `SimulatedTransactionContext`, exception rollback, timeout | Pure Level 1 Revit transaction simulation; zero Level 2 Grasshopper undo integration |
| `Execution/DualSafetyAdversarialStressTests.cs` | 5 | Transaction mode string normalization, uncommitted manual transaction check, unhandled exception rollback, reflection check of `CanvasUndoScope` fields | Only inspects `CanvasUndoScope` via reflection; does not test actual undo/rollback execution |
| `Execution/CanvasUndoScopeTests.cs` | 6 | Null document check, reflection on public API, dispose without commit flag check, tracking method accumulation, null object check, null doc in `CanvasEditor` | All functional tests catch and swallow `TypeInitializationException` / `DllNotFoundException`; zero canvas mutations actually rolled back |
| `Support/GrasshopperScriptGlobalsTests.cs` | 7 | Globals property names and types reflection, Roslyn script compilation with script globals, syntax diagnostics, security guard rejection | Script compilation only; no runtime execution against live or headless Grasshopper canvas |
| **Total** | **54** | | |

---

## 3. Gap Analysis: Missing Coverage

### 3.1. Canvas Serialization & Deserialization (`CanvasReader` & `CanvasEditor`)
- **`CanvasReader.Read(...)`**:
  - **Null & Empty Document Handling**: Null `GH_Document` should return clean `CanvasReadResult` with `name="None"`, `open=false`, `objectCount=0`, and empty component/wire lists.
  - **Component Structure Extraction**: Extraction of `InstanceGuid`, `ComponentGuid`, `Name`, `NickName`, `Category`, `SubCategory`, `Phase` status, and bounding box `(x, y, width, height)`.
  - **Standalone Parameter Extraction**: Standalone canvas objects (`IGH_Param` such as Number Slider, Panel, Value List) must be correctly serialized as objects with empty `inputs` and self-referential `outputs`.
  - **Parameter Metadata & Summaries**: `guid`, `name`, `nickName`, `typeName`, `sourceGuids`, `recipientGuids`, `optional`. When `includeValues = true` and volatile data exists, `valueSummary` must report `"{count} item(s)"`.
  - **Filtering Mechanisms**: `selectedOnly = true` must filter by `Attributes.Selected`. `categoryFilter` must match component `Category` or `SubCategory` case-insensitively.
  - **Wire Topology Extraction**: Wires must correctly map from `source.Attributes.GetTopLevel.DocObject.InstanceGuid` to target component/param GUIDs.
  - **JSON Serialization Integrity**: Verify `CanvasReadResult` serializes to valid JSON without circular references.

- **`CanvasEditor.ExecuteBatch(...)`**:
  - Adding components by GUID or name string with coordinate positioning (`x`, `y`) and nickname override.
  - Resolving temporary identifiers (`tempId`) for connecting newly added components within the same batch.
  - Connecting wires across output ports and input ports with correct indices.
  - Disconnecting existing wires.
  - Removing components by GUID.
  - `dryRun = true` execution: applying all mutations and executing full rollback, returning `{ success = true, dryRun = true, rolledBack = true }`.
  - Atomic failure rollback: when an operation fails mid-batch, prior operations in that batch must be rolled back cleanly.

### 3.2. Parameter Binding & Headless Execution (`PlayerRunner`)
- **`PlayerRunner.Run(...)`**:
  - **File Accessibility & Validation**: Missing file path returns clean failure `{ success = false, error = "Definition file not found: ..." }`. Corrupted files return deserialization errors.
  - **Archive Deserialization**: Successfully reading both binary `.gh` and XML `.ghx` definitions via `GH_Archive`.
  - **Input Parameter Resolution (`FindInputParam`)**:
    - Resolution by exact instance `Guid`.
    - Resolution by standalone parameter `NickName` (case-insensitive).
    - Resolution by component input port `NickName` (case-insensitive).
    - Unmatched input names gracefully skipped without throwing.
  - **Type Conversion & Binding (`SetParamValue`)**:
    - Number conversion: `JsonValueKind.Number` -> `GH_Number`.
    - Boolean conversion: `JsonValueKind.True` / `False` -> `GH_Boolean`.
    - String conversion: `JsonValueKind.String` -> `GH_String`.
    - Unsupported JSON kinds (arrays, objects) handled without crashing.
  - **Output Extraction & Filtering (`ExtractOutputs`)**:
    - Unconsumed standalone parameters (`Recipients.Count == 0 && VolatileDataCount > 0`).
    - Component output ports with volatile data.
    - Output filtering by `requestedOutputs` list (case-insensitive).
    - Scalar vs list formatting: single-item data unpacked to scalar string/value; multi-item data returned as list.
  - **Lifecycle Management**: Ensuring `headless = true` disposes `GH_Document` in `finally` blocks, stopping execution timer and reporting `durationMs`.

### 3.3. Dual Safety Scopes Rollback Mechanics
- **`CanvasUndoScope` Level 2 Mechanics**:
  - **Added Objects Rollback**: Verifying that every GUID tracked via `TrackAddedObject` is removed from `GH_Document.Objects`.
  - **Removed Objects Rollback (LIFO)**: Verifying that removed objects are restored in reverse order (LIFO), their severed wires re-established, and binary archive state re-applied.
  - **Connected Wires Rollback**: Verifying that wires added via `TrackConnectedWire` are severed (`RemoveSource`).
  - **Disconnected Wires Rollback**: Verifying that wires severed via `TrackDisconnectedWire` are reconnected (`AddSource`).
  - **Commit vs Dispose**: Verifying that calling `Commit()` flags `_isCompleted = true`, preventing rollback on `Dispose()`.
  - **Idempotency**: Verifying that calling `Rollback()` multiple times executes only once.
- **Joint Dual Safety Integration (Level 1 + Level 2)**:
  - When `dryRun = true`: Revit `TransactionGroup` is rolled back AND `CanvasUndoScope` rollback is triggered.
  - When execution throws: Revit transactions are rolled back AND canvas mutations are reverted.
  - Cancellation: Linked cancellation aborts execution and cleanly triggers both rollbacks.

---

## 4. Test Fixtures Specification

### 4.1. Directory Structure & Location Strategy

To support both fast unit testing and automated PowerShell live harness runs, fixtures are dual-located:
1. **Unit Test Fixtures Directory**:
   `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures/`
   Configured in `HPGrasshopper.McpBridge.Tests.csproj` to copy to build output:
   ```xml
   <ItemGroup>
       <None Update="Fixtures\**\*">
           <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
       </None>
   </ItemGroup>
   ```
2. **Automated Live Harness Fixtures Directory**:
   `HPGrasshopper/tools/harness/fixtures/`
   Accessible by `run-live-verify.ps1` for end-to-end pipe testing.

### 4.2. File Format Strategy: `.ghx` (XML) and `.gh` (Binary)

Both formats will be supported and maintained:
- **`.ghx` (Grasshopper XML)**: Human-readable, diffable in Git, cleanly versioned without binary corruption risks.
- **`.gh` (GZip Binary)**: Industry-standard compact binary representation for live deployment and execution speed.

### 4.3. Fixture Definitions

#### Fixture 1: `MathSlider` (`MathSlider.ghx` / `MathSlider.gh`)
- **Category**: Arithmetic / Parameter Binding
- **Description**: Evaluates rectangular area from two input parameters.
- **Parameters & Components**:
  - Input 1: `Number Slider` named `"Length"`, default value: `50.0`.
  - Input 2: `Number Slider` named `"Width"`, default value: `30.0`.
  - Operator: `Multiply` component ($A \times B$).
  - Output: `Panel` / Number named `"Area"`.
- **Expected Outputs**:
  - Default: `Area = 1500.0`
  - Input `{"Length": 100.0, "Width": 25.0}` -> `Area = 2500.0`
  - Input `{"Length": 0.0, "Width": 50.0}` -> `Area = 0.0`

#### Fixture 2: `CurveGenerator` (`CurveGenerator.ghx` / `CurveGenerator.gh`)
- **Category**: Parametric Geometry / List Extraction
- **Description**: Generates an interpolated planar circle/polygon curve from polar coordinates.
- **Parameters & Components**:
  - Input 1: `Number Slider` named `"Radius"`, default value: `10.0`.
  - Input 2: `Integer Slider` named `"PointCount"`, default value: `12`.
  - Input 3: `Boolean Toggle` named `"Closed"`, default value: `true`.
  - Generator: Polar coordinate series evaluating $x = R \cos\theta, y = R \sin\theta$.
  - Output 1: `Curve` named `"GeneratedCurve"`.
  - Output 2: `Number` named `"CurveLength"` ($L \approx 2\pi R = 62.8318$).
- **Expected Outputs**:
  - Default: `CurveLength ≈ 62.83`
  - Input `{"Radius": 20.0}` -> `CurveLength ≈ 125.66`
  - Input `{"Closed": false}` -> Open curve with length $< 62.83$.

#### Fixture 3: `RevitDirectShapeSyncMock` (`RevitDirectShapeSyncMock.ghx` / `RevitDirectShapeSyncMock.gh`)
- **Category**: Revit Mock Synchronization / Multi-Type Binding
- **Description**: Parametrically constructs a bounding box mesh representation and metadata payload ready for Revit DirectShape creation.
- **Parameters & Components**:
  - Input 1: `Text Panel` named `"CategoryName"`, default: `"Generic Models"`.
  - Input 2: `Number Slider` named `"BoxWidth"`, default: `2000.0`.
  - Input 3: `Number Slider` named `"BoxHeight"`, default: `4000.0`.
  - Input 4: `Boolean Toggle` named `"DryRun"`, default: `false`.
  - Geometry: 3D Box geometry construct.
  - Output 1: `Text` named `"SolidSummary"`, value: `"Box [2000 x 2000 x 4000 mm]"`.
  - Output 2: `Number` named `"Volume"`, value: `16.0` ($m^3$).
  - Output 3: `Text` named `"SyncStatus"`, value: `"Ready for Revit DirectShape Bake"`.
- **Expected Outputs**:
  - Default: `SolidSummary = "Box [2000 x 2000 x 4000 mm]"`, `Volume = 16.0`.
  - Input `{"CategoryName": "Structural Columns", "BoxWidth": 1000.0}` -> `SolidSummary = "Box [1000 x 1000 x 4000 mm]"`, `Volume = 4.0`.

### 4.4. Programmatic Fixture Generation (`FixtureBuilder.cs`)

A static C# helper `FixtureBuilder.cs` will be included in the test project to programmatically generate valid `.ghx` XML archives without requiring external Rhino installation. Because `GH_IO.dll` contains `GH_Archive` and `GH_LooseChunk` (which are pure managed .NET types), valid archives can be generated, saved, and tested deterministically on any build machine.

---

## 5. Detailed Test Class & Test Case Design

### 5.1. Class 1: `CanvasReaderTests` (15 Tests)
Located at: `HPGrasshopper.McpBridge.Tests/Tools/CanvasReaderTests.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tests.Tools;

public sealed class CanvasReaderTests
{
    // 1. Null document returns clean empty result
    [Fact] public void Read_NullDocument_ReturnsEmptyResultWithNoneMetadata();

    // 2. Empty document returns valid document info and empty components/wires
    [Fact] public void Read_EmptyDocument_ReturnsDocumentMetadataAndEmptyLists();

    // 3. Extracts component identity, category, subcategory, and status
    [Fact] public void Read_WithComponent_ExtractsIdentityAndCategory();

    // 4. Extracts component bounds correctly (X, Y, Width, Height)
    [Fact] public void Read_WithComponent_ExtractsAccurateBounds();

    // 5. Extracts standalone parameter objects (Number Slider, Panel)
    [Fact] public void Read_WithStandaloneParam_ExtractsParamObjectWithSelfOutput();

    // 6. includeValues=false omits volatile data summary
    [Fact] public void Read_IncludeValuesFalse_OmitsVolatileDataSummary();

    // 7. includeValues=true populates volatile data count summary
    [Fact] public void Read_IncludeValuesTrue_PopulatesVolatileDataSummary();

    // 8. selectedOnly=true filters out unselected objects
    [Fact] public void Read_SelectedOnlyTrue_FiltersOutUnselectedObjects();

    // 9. selectedOnly=false includes all objects
    [Fact] public void Read_SelectedOnlyFalse_IncludesAllObjects();

    // 10. categoryFilter matches component Category exactly
    [Fact] public void Read_CategoryFilter_MatchesCategoryExactly();

    // 11. categoryFilter matches SubCategory case-insensitively
    [Fact] public void Read_CategoryFilter_MatchesSubCategoryCaseInsensitive();

    // 12. Extracts component-to-component wires
    [Fact] public void Read_Wires_ExtractsComponentToComponentConnections();

    // 13. Extracts standalone param-to-component wires
    [Fact] public void Read_Wires_ExtractsParamToComponentConnections();

    // 14. Extracts runtime error and warning messages
    [Fact] public void Read_RuntimeMessages_ExtractsWarningsAndErrors();

    // 15. JSON serialization produces valid schema without circular references
    [Fact] public void Read_ResultSerialization_SerializesCleanlyToJson();
}
```

### 5.2. Class 2: `PlayerRunnerTests` (16 Tests)
Located at: `HPGrasshopper.McpBridge.Tests/Tools/PlayerRunnerTests.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tests.Tools;

public sealed class PlayerRunnerTests
{
    // 1. Missing file path returns clean error
    [Fact] public void Run_NonExistentFile_ReturnsFailureWithError();

    // 2. Corrupt or unreadable file returns deserialization error
    [Fact] public void Run_InvalidFileContent_ReturnsDeserializationError();

    // 3. Valid MathSlider definition executes and returns outputs
    [Fact] public void Run_ValidMathDefinition_ExecutesAndReturnsOutputs();

    // 4. Binds Number input parameters to override default values
    [Fact] public void Run_InputBinding_NumberParam_OverridesDefaultValue();

    // 5. Binds Boolean input parameters correctly
    [Fact] public void Run_InputBinding_BooleanParam_BindsTrueAndFalseCorrectly();

    // 6. Binds String input parameters correctly
    [Fact] public void Run_InputBinding_StringParam_BindsStringCorrectly();

    // 7. Binds input parameter by exact InstanceGuid
    [Fact] public void Run_InputBinding_MatchByGuid_FindsTargetParam();

    // 8. Binds input parameter by case-insensitive NickName
    [Fact] public void Run_InputBinding_CaseInsensitiveNickName_MatchesSuccessfully();

    // 9. Unmatched input parameter names are safely ignored
    [Fact] public void Run_InputBinding_UnmatchedKey_SafelyIgnoredWithoutError();

    // 10. Unsupported JSON types (arrays/objects) are handled gracefully
    [Fact] public void Run_InputBinding_UnsupportedJsonType_SafelyIgnored();

    // 11. requestedOutputs filters out unwanted parameters
    [Fact] public void Run_OutputFiltering_ReturnsOnlyRequestedOutputs();

    // 12. requestedOutputs matching is case-insensitive
    [Fact] public void Run_OutputFiltering_CaseInsensitiveOutputNames_Matches();

    // 13. Multi-item volatile data is formatted as list
    [Fact] public void Run_OutputExtraction_MultiItemVolatileData_ReturnsList();

    // 14. Single-item volatile data is formatted as scalar value
    [Fact] public void Run_OutputExtraction_SingleItemVolatileData_ReturnsScalar();

    // 15. headless=true disposes document safely after execution
    [Fact] public void Run_HeadlessTrue_DisposesDocumentSafely();

    // 16. Archive extraction failure returns appropriate error message
    [Fact] public void Run_CorruptedArchiveExtraction_ReturnsExtractObjectError();
}
```

### 5.3. Class 3: `DualSafetyRollbackTests` (15 Tests)
Located at: `HPGrasshopper.McpBridge.Tests/Execution/DualSafetyRollbackTests.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tests.Execution;

public sealed class DualSafetyRollbackTests
{
    // 1. Added objects are removed from document on Rollback
    [Fact] public void CanvasUndoScope_AddedObjectsRollback_RemovesAllTrackedGuids();

    // 2. Removed objects are restored to document on Rollback
    [Fact] public void CanvasUndoScope_RemovedObjectRollback_RestoresObjectToDocument();

    // 3. Removed objects have severed wires restored on Rollback
    [Fact] public void CanvasUndoScope_RemovedObjectWithWiresRollback_RestoresSeveredWires();

    // 4. Multiple removed objects are restored in reverse order (LIFO)
    [Fact] public void CanvasUndoScope_RemovedObjectsLifoOrder_RestoresInReverseSequence();

    // 5. Newly connected wires are severed on Rollback
    [Fact] public void CanvasUndoScope_ConnectedWiresRollback_RemovesConnectedSources();

    // 6. Severed wires are reconnected on Rollback
    [Fact] public void CanvasUndoScope_DisconnectedWiresRollback_RestoresDisconnectedSources();

    // 7. Commit prevents rollback when Dispose is subsequently called
    [Fact] public void CanvasUndoScope_Commit_PreventsRollbackOnDispose();

    // 8. Exiting scope without Commit triggers automatic rollback
    [Fact] public void CanvasUndoScope_DisposeWithoutCommit_ExecutesRollbackAutomatically();

    // 9. Repeated Rollback calls are idempotent
    [Fact] public void CanvasUndoScope_DoubleRollback_IsIdempotent();

    // 10. CanvasEditor batch add with dryRun=true restores exact initial state
    [Fact] public void CanvasEditor_BatchAddWithDryRunTrue_LeavesCanvasCompletelyUnchanged();

    // 11. CanvasEditor batch remove with dryRun=true restores removed components
    [Fact] public void CanvasEditor_BatchRemoveWithDryRunTrue_RestoresRemovedComponents();

    // 12. CanvasEditor batch rewiring with dryRun=true restores original wire topology
    [Fact] public void CanvasEditor_BatchRewireWithDryRunTrue_RestoresOriginalWireTopology();

    // 13. Exception mid-batch rolls back all prior operations in that batch
    [Fact] public void CanvasEditor_ExceptionMidBatch_RollsBackAllPriorOperations();

    // 14. dryRun=true triggers both Revit TransactionGroup rollback and Canvas rollback
    [Fact] public void DualSafety_DryRunTrue_RevitRollbackGroupAndCanvasRollbackAreBothTriggered();

    // 15. Exception in Revit script compensates and rolls back both Revit and Canvas scopes
    [Fact] public void DualSafety_RevitException_CompensatesBothRevitAndCanvasScopes();
}
```

### 5.4. Class 4: `FixtureDefinitionTests` (6 Tests)
Located at: `HPGrasshopper.McpBridge.Tests/Fixtures/FixtureDefinitionTests.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tests.Fixtures;

public sealed class FixtureDefinitionTests
{
    // 1. MathSlider.ghx loads cleanly via GH_Archive
    [Fact] public void MathSliderFixture_LoadsCleanlyFromArchive();

    // 2. MathSlider.ghx executes and calculates correct area
    [Fact] public void MathSliderFixture_Execution_ProducesExpectedArea();

    // 3. CurveGenerator.ghx loads cleanly via GH_Archive
    [Fact] public void CurveGeneratorFixture_LoadsCleanlyFromArchive();

    // 4. CurveGenerator.ghx executes and calculates correct curve length
    [Fact] public void CurveGeneratorFixture_Execution_CalculatesParametricLength();

    // 5. RevitDirectShapeSyncMock.ghx loads cleanly via GH_Archive
    [Fact] public void RevitDirectShapeSyncMockFixture_LoadsCleanlyFromArchive();

    // 6. RevitDirectShapeSyncMock.ghx executes and outputs DirectShape payload
    [Fact] public void RevitDirectShapeSyncMockFixture_Execution_ProducesBakePayload();
}
```

---

## 6. Implementation Roadmap & Verification Plan

### Phase 1: Test Fixtures Infrastructure
1. Implement `FixtureBuilder.cs` in `HPGrasshopper.McpBridge.Tests/Fixtures/` capable of programmatically constructing and serializing valid `.ghx` XML archives using `GH_IO`.
2. Generate the 3 golden fixture files (`MathSlider.ghx`, `CurveGenerator.ghx`, `RevitDirectShapeSyncMock.ghx`) into both:
   - `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures/`
   - `HPGrasshopper/tools/harness/fixtures/`
3. Update `HPGrasshopper.McpBridge.Tests.csproj` with `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` for `Fixtures\**\*`.

### Phase 2: Unit Test Suite Implementation
1. Implement `CanvasReaderTests.cs` (15 tests).
2. Implement `PlayerRunnerTests.cs` (16 tests).
3. Implement `DualSafetyRollbackTests.cs` (15 tests).
4. Implement `FixtureDefinitionTests.cs` (6 tests).

### Phase 3: Verification & Quality Gate
1. Execute `dotnet test HPGrasshopper.McpBridge.Tests.csproj`.
2. Verify total test count increases from **54** to **106** tests with 100% green pass rate.
3. Verify zero regressions across `HPGrasshopper.Mcp.Server.Tests`, `McpShared`, and other deliverables.
