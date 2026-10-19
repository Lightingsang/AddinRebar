# Exhaustive Census & Forensic Audit Report — Tier 5 Adversarial Test Suites

**Author**: `explorer_final_it2_2` (Teamwork Preview Explorer)  
**Date**: 2026-10-08  
**Scope**: All 10 Tier 5 Adversarial Test Files across `HPBim.Core.Tests` and `HPBim.Client.Tests`  
**Measurement Tool**: Roslyn Compiler AST Engine (`scratch/RoslynSyntaxAuditor`) + Physical Line Scanner  
**Status**: Investigation Complete  

---

## 1. Executive Summary

An exhaustive, member-by-member census and compliance audit of all 10 newly authored Tier 5 white-box adversarial test files was conducted across `HPBim.Core.Tests` (4 files, 36 tests) and `HPBim.Client.Tests` (6 files, 64 tests).

### Core Audit Verdicts:
1. **Rule C2 (File & Class Length <= 300 Lines)**: **100% COMPLIANT (PASS)**
   - All 10 files strictly comply with Rule C2. Maximum file length is 272 physical lines (`BimModelSchemaValidatorAdversarialCoverageTests.cs`).
   - All 11 classes strictly comply with Rule C2. Maximum class span is 262 lines (L10–L271).
2. **Rule M3 (Method / Member Length <= 50 Lines)**: **1 VIOLATION FOUND / ZERO OTHER HIDDEN VIOLATIONS**
   - **Single Violation**: Method `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately` in `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs` spans **59 physical lines** with `[Fact]` attribute (lines 38–96) / **58 physical lines** from declaration to closing brace (lines 39–96).
   - **Exhaustive Census of All Other 89 Members**: **100% COMPLIANT**. The next longest method in the entire 10-file suite is 38 lines (`ExecutePlanAsync_ElementFailure_IdentifiesFailedElementsAndSetsOverallFailure` in `BimPipelineEngineTier5AdversarialTests.cs`). All other 88 members are between 1 and 37 lines.
   - **Definitive Finding**: There are **ZERO** other hidden Rule M3 violations across all 10 files.
3. **Rule Q-B1 (Zero Commented-Out Code)**: **100% COMPLIANT (CLEAN)**
   - Zero lines of commented-out code, statements, or dead logic were identified across all 10 files.
4. **Rule Q-B2 (Catch Blocks with Explanatory Rationale)**: **100% COMPLIANT (CLEAN)**
   - Exactly 3 catch blocks exist across all 10 files (all within `Dispose()` routines tolerating transient Windows file locks during temporary directory teardown).
   - All 3 catch blocks contain explicit rationale comments explaining why exceptions are safely caught.
5. **Execution & Regression Verification**: **100% GREEN**
   - `HPBim.slnx`: 770/770 passed (417 Core + 311 Client + 42 E2E).
   - `McpShared.slnx`: 908/908 passed (793 Server.Core + 115 Bridge.Net48).
   - Tier 5 Test Count: Exactly 100 test cases (36 Core + 64 Client).

---

## 2. File-by-File Summary Matrix

| # | File Name | Project | Physical Lines (C2 <= 300) | Test Cases | Total Members | Max Member (Lines) | Rule M3 Status | Catch Blocks (Q-B2) | Commented Code (Q-B1) |
|---|---|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | Core | 272 | 10 | 10 | 37 | **PASS** | 0 | 0 |
| 2 | `IdempotentElementResolverAdversarialCoverageTests.cs` | Core | 187 | 7 | 10 | 35 | **PASS** | 0 | 0 |
| 3 | `CompositeSourceKeyAdversarialCoverageTests.cs` | Core | 127 | 11 | 8 | 31 | **PASS** | 0 | 0 |
| 4 | `MeasuredGeometricVerifierAdversarialCoverageTests.cs` | Core | 256 | 8 | 8 | 37 | **PASS** | 0 | 0 |
| 5 | `IntentJournalTier5AdversarialTests.cs` | Client | 181 | 7 | 9 | 32 | **PASS** | 1 (Clean) | 0 |
| 6 | `TimeoutPolicyTier5AdversarialTests.cs` | Client | 130 | 21 | 11 | 19 | **PASS** | 0 | 0 |
| 7 | `ReceiptReaderTier5AdversarialTests.cs` | Client | 192 | 17 | 10 | 29 | **PASS** | 0 | 0 |
| 8 | `ReconcileServiceTier5AdversarialTests.cs` | Client | 186 | 6 | 8 | **59** | **FAIL (1)** | 1 (Clean) | 0 |
| 9 | `BimPlanRunnerScriptTemplateTier5AdversarialTests.cs` | Client | 103 | 6 | 6 | 26 | **PASS** | 0 | 0 |
| 10 | `BimPipelineEngineTier5AdversarialTests.cs` | Client | 239 | 7 | 10 | 38 | **PASS** | 1 (Clean) | 0 |
| **TOTAL** | **10 Files** | **Both** | **1,873** | **100** | **90** | **59 (Max)** | **1 VIOLATION** | **3 (All clean)** | **0 lines** |

---

## 3. Exhaustive Member-by-Member Census (All 90 Members)

All metrics were captured via the Roslyn Compiler AST (`Microsoft.CodeAnalysis.CSharp`), measuring:
- `SpanWithAttr`: Span from the start of the first attribute (e.g. `[Fact]`) to the member closing brace.
- `SpanDecl`: Span from the member declaration token to the member closing brace.

### 3.1 `BimModelSchemaValidatorAdversarialCoverageTests.cs` (272 lines)
- **Class**: `BimModelSchemaValidatorAdversarialCoverageTests` (Lines 10–271, 262 lines) — **PASS (C2)**
- **Members (10 methods)**:
  1. `Validate_RebarHostResolution_UnderscorePrefixAndBrokenRef`: SpanWithAttr: 37 (L211–L247), SpanDecl: 37 — **PASS (M3)**
  2. `Validate_WallOpening_BeyondWallBoundary_Rejected`: SpanWithAttr: 33 (L116–L148), SpanDecl: 33 — **PASS (M3)**
  3. `Validate_UnicodeAndVietnameseDiacriticLevelNames_ResolvesAndPasses`: SpanWithAttr: 29 (L12–L40), SpanDecl: 29 — **PASS (M3)**
  4. `Validate_GridLengthNear1mmBoundary_StrictBoundaryEnforcement`: SpanWithAttr: 28 (L87–L114), SpanDecl: 28 — **PASS (M3)**
  5. `Validate_WallOpening_NegativeOffset_Rejected`: SpanWithAttr: 27 (L150–L176), SpanDecl: 27 — **PASS (M3)**
  6. `Validate_EmojiAndSpecialCharactersInLevelNames_PassesIfMatching`: SpanWithAttr: 25 (L42–L66), SpanDecl: 25 — **PASS (M3)**
  7. `Validate_PipeRunDef_DegeneratePointsList_Rejected`: SpanWithAttr: 22 (L249–L270), SpanDecl: 22 — **PASS (M3)**
  8. `Validate_ExtremeCoordinates_NaNInGrids_Rejected`: SpanWithAttr: 18 (L68–L85), SpanDecl: 18 — **PASS (M3)**
  9. `Validate_CorruptedJsonNodes_NonArrayLevels_Rejected`: SpanWithAttr: 16 (L178–L193), SpanDecl: 16 — **PASS (M3)**
  10. `Validate_CorruptedJsonNodes_TypeMismatchInElevation_Rejected`: SpanWithAttr: 15 (L195–L209), SpanDecl: 15 — **PASS (M3)**

### 3.2 `IdempotentElementResolverAdversarialCoverageTests.cs` (187 lines)
- **Outer Class**: `IdempotentElementResolverAdversarialCoverageTests` (Lines 9–186, 178 lines) — **PASS (C2)**
- **Inner Class**: `DynamicExistenceQuery` (Lines 11–22, 12 lines) — **PASS (C2)**
- **Members (10 methods)**:
  1. `Resolve_MultiHandleProvenanceCollisions_ResolvesConsistently`: SpanWithAttr: 35 (L73–L107), SpanDecl: 35 — **PASS (M3)**
  2. `Resolve_FullRecreateToNoOpLifecycle_EndToEndSimulation`: SpanWithAttr: 32 (L154–L185), SpanDecl: 32 — **PASS (M3)**
  3. `Resolve_CaseInsensitiveTypeNameAndLevel_ResolvesToNoOpWithoutUpdate`: SpanWithAttr: 23 (L109–L131), SpanDecl: 23 — **PASS (M3)**
  4. `Resolve_MissingResourceOrTypeName_GracefulNoOp`: SpanWithAttr: 20 (L133–L152), SpanDecl: 20 — **PASS (M3)**
  5. `Resolve_TombstonedEmptyTargetUniqueId_ResolvesToCreate`: SpanWithAttr: 18 (L24–L41), SpanDecl: 18 — **PASS (M3)**
  6. `Resolve_ElementMissingInHostDocument_ResolvesToRecreate`: SpanWithAttr: 15 (L43–L57), SpanDecl: 15 — **PASS (M3)**
  7. `Resolve_QueryReturnsNull_ResolvesToRecreate`: SpanWithAttr: 13 (L59–L71), SpanDecl: 13 — **PASS (M3)**
  8. `Register` (in `DynamicExistenceQuery`): SpanWithAttr: 2 (L15–L16), SpanDecl: 2 — **PASS (M3)**
  9. `QueryElement` (in `DynamicExistenceQuery`): SpanWithAttr: 2 (L20–L21), SpanDecl: 2 — **PASS (M3)**
  10. `Remove` (in `DynamicExistenceQuery`): SpanWithAttr: 1 (L18–L18), SpanDecl: 1 — **PASS (M3)**

### 3.3 `CompositeSourceKeyAdversarialCoverageTests.cs` (127 lines)
- **Class**: `CompositeSourceKeyAdversarialCoverageTests` (Lines 8–126, 119 lines) — **PASS (C2)**
- **Members (8 methods / 11 test cases)**:
  1. `Stress_MassiveKeyLookupAndCollisionResistance`: SpanWithAttr: 31 (L82–L112), SpanDecl: 31 — **PASS (M3)**
  2. `CompareTo_ConsistentWithEqualsContract`: SpanWithAttr: 14 (L67–L80), SpanDecl: 14 — **PASS (M3)**
  3. `Parse_NumericCategory_ParsesIfDefined_OrRejectsIfNotDefined`: SpanWithAttr: 13 (L43–L55), SpanDecl: 13 — **PASS (M3)**
  4. `Equals_And_HashCode_EmptyKeyContract`: SpanWithAttr: 12 (L114–L125), SpanDecl: 12 — **PASS (M3)**
  5. `Parse_FilenameWithEmbeddedHash_ParsesCorrectlyInBracketedFormat`: SpanWithAttr: 10 (L10–L19), SpanDecl: 10 — **PASS (M3)**
  6. `Parse_MultipleColonsInHandle_PreservesAllColons`: SpanWithAttr: 10 (L21–L30), SpanDecl: 10 — **PASS (M3)**
  7. `Parse_ExoticWhitespaceAndTabsAroundDelimiters_TrimsProperly`: SpanWithAttr: 10 (L32–L41), SpanDecl: 10 — **PASS (M3)**
  8. `Parse_MalformedDelimiters_ThrowsFormatException` (Theory, 4 InlineData): SpanWithAttr: 9 (L57–L65), SpanDecl: 9 — **PASS (M3)**

### 3.4 `MeasuredGeometricVerifierAdversarialCoverageTests.cs` (256 lines)
- **Class**: `MeasuredGeometricVerifierAdversarialCoverageTests` (Lines 11–255, 245 lines) — **PASS (C2)**
- **Members (8 methods)**:
  1. `AuditColumn_DecimalRoundingNearToleranceBoundaries_StrictZeroMm`: SpanWithAttr: 37 (L21–L57), SpanDecl: 37 — **PASS (M3)**
  2. `AuditLinearStructure_VN2000_ExtremeCoordinatesScaleAndOffsets`: SpanWithAttr: 36 (L117–L152), SpanDecl: 36 — **PASS (M3)**
  3. `AuditBeam_ElevationBoundary_Strict1mmThreshold`: SpanWithAttr: 31 (L59–L89), SpanDecl: 31 — **PASS (M3)**
  4. `AuditPlan_ListOverload_DictionaryMappingByUniqueIdAndKey`: SpanWithAttr: 26 (L229–L254), SpanDecl: 26 — **PASS (M3)**
  5. `AuditPile_CenterBoundary_Strict2mmThreshold`: SpanWithAttr: 25 (L91–L115), SpanDecl: 25 — **PASS (M3)**
  6. `AuditLevel_ScaleConversionFromMetersToMillimeters`: SpanWithAttr: 25 (L178–L202), SpanDecl: 25 — **PASS (M3)**
  7. `AuditItem_UnmeasurableCurve_Rule0_Inconclusive_NeverPass`: SpanWithAttr: 24 (L204–L227), SpanDecl: 24 — **PASS (M3)**
  8. `AuditWall_EndpointOffsetExceedsTolerance_FlaggedAsDefect`: SpanWithAttr: 23 (L154–L176), SpanDecl: 23 — **PASS (M3)**

### 3.5 `IntentJournalTier5AdversarialTests.cs` (181 lines)
- **Class**: `IntentJournalTier5AdversarialTests` (Lines 12–180, 169 lines) — **PASS (C2)**
- **Catch Blocks**: 1 catch block at L30–L33 in `Dispose`: contains `// Transient file lock cleanup tolerance` — **PASS (Q-B2)**
- **Members (8 methods + 1 constructor)**:
  1. `ConcurrentOperations_HighLoad_AppendAndReadSimultaneously_PreservesAllOperations`: SpanWithAttr: 32 (L65–L96), SpanDecl: 32 — **PASS (M3)**
  2. `RapidStatusMutations_SameOperation_PersistsAndReadsLatestStateInCacheAndColdRestart`: SpanWithAttr: 28 (L36–L63), SpanDecl: 28 — **PASS (M3)**
  3. `FileCorruption_BlankLinesAndMalformedRecords_GracefullyExtractsValidRecords`: SpanWithAttr: 24 (L98–L121), SpanDecl: 24 — **PASS (M3)**
  4. `DisposedState_CallingAnyMethod_ThrowsObjectDisposedException`: SpanWithAttr: 20 (L123–L142), SpanDecl: 20 — **PASS (M3)**
  5. `UpdateStatus_ForUnrecordedOperation_CreatesNewRecordAndAppendsToDisk`: SpanWithAttr: 18 (L154–L171), SpanDecl: 18 — **PASS (M3)**
  6. `Dispose`: SpanWithAttr: 11 (L24–L34), SpanDecl: 11 — **PASS (M3)**
  7. `PruneOldEntries_WhenFileDoesNotExist_ReturnsZero`: SpanWithAttr: 9 (L144–L152), SpanDecl: 9 — **PASS (M3)**
  8. `RecordIntent_NullArgument_ThrowsArgumentNullException`: SpanWithAttr: 7 (L173–L179), SpanDecl: 7 — **PASS (M3)**
  9. Constructor `IntentJournalTier5AdversarialTests()`: SpanWithAttr: 6 (L17–L22), SpanDecl: 6 — **PASS (M3)**

### 3.6 `TimeoutPolicyTier5AdversarialTests.cs` (130 lines)
- **Class**: `TimeoutPolicyTier5AdversarialTests` (Lines 7–129, 123 lines) — **PASS (C2)**
- **Members (11 methods / 21 test cases)**:
  1. `ComputeExecutionBudget_ClampsStrictlyToMaxExecutionBudget`: SpanWithAttr: 19 (L80–L98), SpanDecl: 19 — **PASS (M3)**
  2. `ComputeTotalTimeout_SumOfQueueDeadlineAndExecutionBudget`: SpanWithAttr: 13 (L100–L112), SpanDecl: 13 — **PASS (M3)**
  3. `ComputeExecutionBudget_ZeroOrNegativeItems_ReturnsDefaultExecutionBudget` (Theory, 4 InlineData): SpanWithAttr: 11 (L59–L69), SpanDecl: 11 — **PASS (M3)**
  4. `Validate_MaxLessThanBase_ThrowsArgumentOutOfRangeException` (Theory, 3 InlineData): SpanWithAttr: 10 (L37–L46), SpanDecl: 10 — **PASS (M3)**
  5. `Validate_ExactEquality_BaseAndMaxBudget_ValidatesSuccessfully`: SpanWithAttr: 10 (L48–L57), SpanDecl: 10 — **PASS (M3)**
  6. `Validate_NonPositiveTransportTimeout_ThrowsArgumentOutOfRangeException` (Theory, 3 InlineData): SpanWithAttr: 9 (L9–L17), SpanDecl: 9 — **PASS (M3)**
  7. `Validate_NonPositiveQueueDeadline_ThrowsArgumentOutOfRangeException` (Theory, 3 InlineData): SpanWithAttr: 8 (L19–L26), SpanDecl: 8 — **PASS (M3)**
  8. `Validate_NonPositiveBaseExecutionBudget_ThrowsArgumentOutOfRangeException` (Theory, 3 InlineData): SpanWithAttr: 8 (L28–L35), SpanDecl: 8 — **PASS (M3)**
  9. `ComputeExecutionBudget_OneItem_ReturnsBasePlusOneIncrement`: SpanWithAttr: 8 (L71–L78), SpanDecl: 8 — **PASS (M3)**
  10. `ComputeExecutionBudget_HugeCountIntMax_CalculatesSafelyWithoutOverflow`: SpanWithAttr: 7 (L114–L120), SpanDecl: 7 — **PASS (M3)**
  11. `CustomPerItemBudget_ZeroPerItem_YieldsConstantBaseBudget`: SpanWithAttr: 7 (L122–L128), SpanDecl: 7 — **PASS (M3)**

### 3.7 `ReceiptReaderTier5AdversarialTests.cs` (192 lines)
- **Class**: `ReceiptReaderTier5AdversarialTests` (Lines 10–191, 182 lines) — **PASS (C2)**
- **Members (10 methods / 17 test cases)**:
  1. `ParseReceipts_ArrayContainingNonObjects_GracefullySkipsNonObjects`: SpanWithAttr: 29 (L43–L71), SpanDecl: 29 — **PASS (M3)**
  2. `FindReceiptAsync_PrioritizesOperationIdOverPlanHash`: SpanWithAttr: 27 (L123–L149), SpanDecl: 27 — **PASS (M3)**
  3. `ParseReceipts_MissingOrCorruptedFields_DefaultsGracefully`: SpanWithAttr: 23 (L73–L95), SpanDecl: 23 — **PASS (M3)**
  4. `FindReceiptAsync_FallsBackToPlanHash_WhenOperationIdMissing`: SpanWithAttr: 20 (L151–L170), SpanDecl: 20 — **PASS (M3)**
  5. `ParseReceipts_ReceiptsArrayMissingOrNonArray_ReturnsEmptyList` (Theory, 3 InlineData): SpanWithAttr: 15 (L27–L41), SpanDecl: 15 — **PASS (M3)**
  6. `ParseReceipts_NullOrNonObjectRoot_ReturnsEmptyList` (Theory, 3 InlineData): SpanWithAttr: 14 (L12–L25), SpanDecl: 14 — **PASS (M3)**
  7. `ParseElementMap_EdgeCasesAndInvalidInputs_ReturnsEmptyDictionary` (Theory, 3 InlineData): SpanWithAttr: 14 (L97–L110), SpanDecl: 14 — **PASS (M3)**
  8. `ParseElementMap_ValidKeyValues_ParsesAllEntries`: SpanWithAttr: 10 (L112–L121), SpanDecl: 10 — **PASS (M3)**
  9. `FindReceiptAsync_WhenNeitherMatches_ReturnsNull` (Theory, 2 InlineData): SpanWithAttr: 9 (L172–L180), SpanDecl: 9 — **PASS (M3)**
  10. `AutoConnect_WhenPipeClientDisconnected_ConnectsBeforeQuery`: SpanWithAttr: 9 (L182–L190), SpanDecl: 9 — **PASS (M3)**

### 3.8 `ReconcileServiceTier5AdversarialTests.cs` (186 lines)
- **Class**: `ReconcileServiceTier5AdversarialTests` (Lines 14–185, 172 lines) — **PASS (C2)**
- **Catch Blocks**: 1 catch block at L32–L35 in `Dispose`: contains `// Transient lock cleanup tolerance` — **PASS (Q-B2)**
- **Members (7 methods + 1 constructor)**:
  1. `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately`:
     - **SpanWithAttr**: **59 lines** (L38–L96)
     - **SpanDecl**: **58 lines** (L39–L96)
     - **Status**: **VIOLATION (> 50 lines) [RULE M3]**
  2. `ReconcileIntent_NetworkSeveredDuringQuery_ReturnsStillUnknownAndPreservesIntentState`: SpanWithAttr: 20 (L128–L147), SpanDecl: 20 — **PASS (M3)**
  3. `ReconcileIntent_PipeTimeoutDuringQuery_ReturnsStillUnknownAndPreservesIntentState`: SpanWithAttr: 20 (L149–L168), SpanDecl: 20 — **PASS (M3)**
  4. `ReconcileIntent_MissingInJournal_ReturnsStillUnknownWithExplanation`: SpanWithAttr: 15 (L112–L126), SpanDecl: 15 — **PASS (M3)**
  5. `AutoConnect_WhenPipeDisconnected_ConnectsAutomatically`: SpanWithAttr: 15 (L170–L184), SpanDecl: 15 — **PASS (M3)**
  6. `ReconcileAllUnknownIntents_WhenNoIncompleteIntents_ReturnsNoOp`: SpanWithAttr: 13 (L98–L110), SpanDecl: 13 — **PASS (M3)**
  7. `Dispose`: SpanWithAttr: 11 (L26–L36), SpanDecl: 11 — **PASS (M3)**
  8. Constructor `ReconcileServiceTier5AdversarialTests()`: SpanWithAttr: 6 (L19–L24), SpanDecl: 6 — **PASS (M3)**

### 3.9 `BimPlanRunnerScriptTemplateTier5AdversarialTests.cs` (103 lines)
- **Class**: `BimPlanRunnerScriptTemplateTier5AdversarialTests` (Lines 10–102, 93 lines) — **PASS (C2)**
- **Members (6 methods)**:
  1. `ScriptTemplate_ContainsAllMandatoryCategories`: SpanWithAttr: 26 (L38–L63), SpanDecl: 26 — **PASS (M3)**
  2. `ScriptTemplate_Epilogue_ReturnsExpectedSummaryStructure`: SpanWithAttr: 14 (L88–L101), SpanDecl: 14 — **PASS (M3)**
  3. `ScriptTemplate_ContainsExtensibleStorageReceiptSchemaDefinition`: SpanWithAttr: 13 (L74–L86), SpanDecl: 13 — **PASS (M3)**
  4. `GetScriptCode_ParsesAsValidCSharpScript_WithZeroSyntaxErrors`: SpanWithAttr: 12 (L12–L23), SpanDecl: 12 — **PASS (M3)**
  5. `Preamble_ContainsMandatoryCoordinateAndPreflightHelpers`: SpanWithAttr: 12 (L25–L36), SpanDecl: 12 — **PASS (M3)**
  6. `ScriptTemplate_ContainsUnsupportedCategoryDefensiveBranch`: SpanWithAttr: 8 (L65–L72), SpanDecl: 8 — **PASS (M3)**

### 3.10 `BimPipelineEngineTier5AdversarialTests.cs` (239 lines)
- **Class**: `BimPipelineEngineTier5AdversarialTests` (Lines 17–238, 222 lines) — **PASS (C2)**
- **Catch Blocks**: 1 catch block at L35–L38 in `Dispose`: contains `// Transient test lock cleanup tolerance` — **PASS (Q-B2)**
- **Members (9 methods + 1 constructor)**:
  1. `ExecutePlanAsync_ElementFailure_IdentifiesFailedElementsAndSetsOverallFailure`: SpanWithAttr: 38 (L174–L211), SpanDecl: 38 — **PASS (M3)**
  2. `ExecutePlanJournaled_ApplyExecution_UnexpectedRollback_FailsWithExplicitMessage`: SpanWithAttr: 26 (L99–L124), SpanDecl: 26 — **PASS (M3)**
  3. `ExecutePlanJournaled_CalculatesTimeoutFromPolicy_WhenPlanTimeoutZero`: SpanWithAttr: 25 (L213–L237), SpanDecl: 25 — **PASS (M3)**
  4. `ExecutePlanAsync_RevitWarnings_PropagatesToResultAndStageResults`: SpanWithAttr: 24 (L149–L172), SpanDecl: 24 — **PASS (M3)**
  5. `ExecutePlanJournaled_DryRun_UnconfirmedRollback_FailsWithExplicitMessage`: SpanWithAttr: 22 (L126–L147), SpanDecl: 22 — **PASS (M3)**
  6. `CreatePlan` (private helper): SpanWithAttr: 19 (L41–L59), SpanDecl: 19 — **PASS (M3)**
  7. `ExecutePlanJournaled_PipeDisconnectDuringExecution_MarksOutcomeUnknownAndRecordsJournal`: SpanWithAttr: 18 (L61–L78), SpanDecl: 18 — **PASS (M3)**
  8. `ExecutePlanJournaled_PipeTimeoutDuringExecution_MarksOutcomeUnknownAndRecordsJournal`: SpanWithAttr: 18 (L80–L97), SpanDecl: 18 — **PASS (M3)**
  9. `Dispose`: SpanWithAttr: 11 (L29–L39), SpanDecl: 11 — **PASS (M3)**
  10. Constructor `BimPipelineEngineTier5AdversarialTests()`: SpanWithAttr: 6 (L22–L27), SpanDecl: 6 — **PASS (M3)**

---

## 4. Technical Analysis: The `scratch/audit_tier5_forensics.py` Parser Bug

During the investigation, execution of `scratch/audit_tier5_forensics.py` produced a spurious warning:
```
FileCorruption_BlankLinesAndMalformedRecords_GracefullyExtractsValidRecords: 83 lines (L98-L180)
```
### Root Cause:
The Python script relied on a simple character loop counting `{` and `}` without state-machine handling for C# string literals:
```python
clean_text = re.sub(r'//.*', '', l_text)
for char in clean_text:
    if char == '{': depth += 1
    elif char == '}': depth -= 1
```
On line 108 of `IntentJournalTier5AdversarialTests.cs`, the test contains the string literal:
```csharp
sb.AppendLine("{\"badJsonNoClose: 1234");
```
Because this string literal contains an open brace `{` with no closing brace inside the string, the Python parser incremented `depth` by 1 and remained desynchronized until the end of the enclosing class file at line 180.

### Roslyn Compiler AST Ground Truth:
The Microsoft Roslyn C# syntax parser (`Microsoft.CodeAnalysis.CSharp`) parses full language tokens, correctly treating `"{\"badJsonNoClose: 1234"` as a string token. Roslyn verifies that `FileCorruption_BlankLinesAndMalformedRecords_GracefullyExtractsValidRecords` ends cleanly at line 121 (24 physical lines total: L98–L121).

**Conclusion**: Roslyn AST is the sole authoritative ground-truth compiler tool. `FileCorruption_BlankLinesAndMalformedRecords_GracefullyExtractsValidRecords` is 100% compliant with Rule M3 (24 lines <= 50 lines).

---

## 5. Concrete Action Plan for Remediation

To achieve 100% compliance across the entire codebase:

### Target File:
`HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs`

### Offending Code (Lines 38–96, 59 lines):
```csharp
    [Fact]
    public async Task ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();

        var opCommitted = Guid.NewGuid();
        var opRolledBack = Guid.NewGuid();
        var opMixedKeys = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opCommitted, PlanHash = "p-com", Status = IntentStatus.OutcomeUnknown });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opRolledBack, PlanHash = "p-rol", Status = IntentStatus.OutcomeUnknown });
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = opMixedKeys, PlanHash = "p-mix", Status = IntentStatus.OutcomeUnknown });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opCommitted,
            PlanHash = "p-com",
            ElementMap = new Dictionary<string, string> { ["Doc#Column:C1"] = "uid-c1" }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opMixedKeys,
            PlanHash = "p-mix",
            ElementMap = new Dictionary<string, string>
            {
                ["Doc#Beam:B1"] = "uid-b1",
                ["invalid-key-no-category"] = "uid-ignored"
            }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var results = await service.ReconcileAllUnknownIntentsAsync();

        Assert.Equal(3, results.Count);
        Assert.Equal(ReconcileStatus.Committed, results[0].Status);
        Assert.Equal(ReconcileStatus.RolledBack, results[1].Status);
        Assert.Equal(ReconcileStatus.Committed, results[2].Status);

        Assert.Equal(2, ledger.Mappings.Count);
        Assert.True(ledger.TryGetTarget(CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"), out var uidC1));
        Assert.Equal("uid-c1", uidC1);
        Assert.True(ledger.TryGetTarget(CompositeSourceKey.Create("Doc", CoordinationCategory.Beam, "B1"), out var uidB1));
        Assert.Equal("uid-b1", uidB1);

        var rec1 = await journal.GetIntentAsync(opCommitted);
        Assert.NotNull(rec1);
        Assert.Equal(IntentStatus.Committed, rec1.Status);

        var rec2 = await journal.GetIntentAsync(opRolledBack);
        Assert.NotNull(rec2);
        Assert.Equal(IntentStatus.Failed, rec2.Status);

        var rec3 = await journal.GetIntentAsync(opMixedKeys);
        Assert.NotNull(rec3);
        Assert.Equal(IntentStatus.Committed, rec3.Status);
    }
```

### Proposed Clean Code Refactoring:
Extract the receipt seeding logic into a private static helper method:
```csharp
    private static void SeedSimulatedReceipts(FakeRevitPipeClient fakePipe, Guid opCommitted, Guid opMixedKeys)
    {
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opCommitted,
            PlanHash = "p-com",
            ElementMap = new Dictionary<string, string> { ["Doc#Column:C1"] = "uid-c1" }
        });
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opMixedKeys,
            PlanHash = "p-mix",
            ElementMap = new Dictionary<string, string>
            {
                ["Doc#Beam:B1"] = "uid-b1",
                ["invalid-key-no-category"] = "uid-ignored"
            }
        });
    }
```
This reduces the test method span from 59 lines down to 42 lines (with attribute), achieving full Rule M3 compliance while keeping the helper at 19 lines (<= 50 lines).
