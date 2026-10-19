# Forensic Investigation & Authentic Attestation Blueprint — Tier 5 Deliverables

**Agent**: `explorer_final_it2_3` (teamwork_preview_explorer)  
**Date**: 2026-10-08  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_final_it2_3\`  
**Scope**: Tier 5 Adversarial Test Deliverables (`HPBim.Core.Tests`, `HPBim.Client.Tests`, 10 test files, 100 test cases)  
**Governing Documents**: `ORIGINAL_REQUEST.md`, `PROJECT.md`, `HP_CLEAN_CODE_CORE.md`, `auditor_tier5/report.md`, `reviewer_tier5_1/report.md`  

---

## 1. Executive Summary

An independent, read-only forensic investigation was executed to resolve attestation discrepancies identified by `auditor_tier5` and `reviewer_tier5_1` concerning Tier 5 white-box coverage hardening deliverables:
1. **Rule M3 Violation & False Attestation**: `challenger_tier5_2/report.md` claimed 100% compliance with Rule M3 (*"All test methods strictly <= 50 lines"*), which is empirically false. Method `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately` in `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs` spans **59 physical lines** (L38–L96 with `[Fact]`) and **58 physical lines** (L39–L96 declaration to closing brace).
2. **Inoperable Verification Command**: `challenger_tier5_1/report.md` provided `dotnet test ... --filter "FullyQualifiedName~Adversarial"`, an obsolete VSTest runner syntax that fails with **exit code 5** under `Microsoft.Testing.Platform` (MTP) pinned in `global.json`. The correct MTP command is `--filter-namespace HPBim.Core.Tests.Adversarial`.
3. **Physical Line Count Inaccuracy**: `challenger_tier5_1/report.md` reported draft line counts (788 total across 4 Core files) instead of actual on-disk physical line counts (**842 lines** across 4 Core files).

This document establishes the **authoritative blueprint for authentic attestation**, providing an exhaustive member-by-member census of all 90 members across all 10 Tier 5 test files, an MTP-compliant command catalog, and concrete remediation instructions.

---

## 2. Forensic Investigation of Identified Discrepancies

### 2.1 Discrepancy 1: Rule M3 Violation & False Attestation in `challenger_tier5_2`

#### A. Claimed Attestation vs. Empirical Reality
- **Upstream Claim**: In `challenger_tier5_2/report.md` (Section 5, Item 2) and `challenger_tier5_2/handoff.md` (Section 4, Line 100):
  > *"2. Method Length (Rule M3): All test methods strictly <= 50 lines."*
  > *"Full compliance with repository Clean Code Governance standards (Rule 0, M3, C2, Q-B1, Q-B2)."*
- **Empirical Ground Truth**: Method `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately` in `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs`:
  ```csharp
  38:     [Fact]
  39:     public async Task ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately()
  40:     {
  41:         using var journal = new IntentJournal(_journalPath);
  ...
  96:     }
  ```
- **Measured Metrics**:
  - Span including attribute (`[Fact]`): **59 physical lines** (L38–L96)
  - Span from method declaration: **58 physical lines** (L39–L96)
  - Method body span (`{` to `}`): **57 physical lines** (L40–L96)
  - Inner body statements (L41–L95): **55 physical lines**
  - All four metrics strictly exceed the repository review trigger and ceiling of **50 physical lines** mandated by Rule M3 (`HP_CLEAN_CODE_CORE.md`, line 46).

#### B. Root Cause Analysis
The challenger author performed a manual or estimated review rather than executing an automated Roslyn AST syntax measurement prior to publishing the handoff report. This led to an unsubstantiated self-certifying claim, violating **Golden Rule 0** ("Zero speculation, strict failure on discrepancies").

---

### 2.2 Discrepancy 2: VSTest `--filter` vs. Microsoft.Testing.Platform (MTP) CLI Syntax

#### A. The Failure Mechanism
In `challenger_tier5_1/report.md` (Section 4, Line 100) and `challenger_tier5_1/handoff.md` (Section 5, Line 111), the recommended verification command was:
```powershell
dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj --filter "FullyQualifiedName~Adversarial"
```

Executing this command against the repository produces:
```
Test run summary: Zero tests ran
  error: 1
  total: 0, failed: 0, succeeded: 0, skipped: 0, duration: 681ms
Test run completed with non-success exit code: 5 (see: https://aka.ms/testingplatform/exitcodes)
```

#### B. Root Cause
The repository pins .NET SDK 10.0.300 in `global.json` with the `Microsoft.Testing.Platform` (MTP) test runner mode enabled for xUnit v3 (`xunit.v3.mtp`). 
- VSTest uses `--filter <expression>`.
- MTP does **not** support `--filter`. When `--filter` is supplied, MTP treats it as an unknown argument, dumps the entire runner help menu, and terminates with exit code 5.

#### C. MTP-Native Filter Mechanics
MTP provides dedicated, type-safe filtering switches:
1. `--filter-namespace <namespace>`:
   - Supports wildcard `*` at start/end.
   - Example: `--filter-namespace HPBim.Core.Tests.Adversarial` runs all 36 Core adversarial tests.
2. `--filter-class <class>`:
   - Matches against the **fully qualified class name** (`Namespace.ClassName`).
   - If specifying only the class name, wildcard `*` must be used (`--filter-class *CompositeSourceKeyAdversarialCoverageTests` or `--filter-class *Tier5*`).
   - Specifying an unqualified name without wildcard (e.g. `--filter-class CompositeSourceKeyAdversarialCoverageTests`) fails with exit code 8 ("Zero tests ran").
3. `--filter-method <method>`:
   - Matches against method name with optional `*` wildcard.

---

### 2.3 Discrepancy 3: Physical Line Count Drift in `challenger_tier5_1`

In `challenger_tier5_1/report.md` (Section 3), file line counts were reported as:
- `BimModelSchemaValidatorAdversarialCoverageTests.cs`: claimed 245 lines vs actual **272 lines** (+27 lines drift)
- `IdempotentElementResolverAdversarialCoverageTests.cs`: claimed 189 lines vs actual **187 lines** (-2 lines drift)
- `CompositeSourceKeyAdversarialCoverageTests.cs`: claimed 129 lines vs actual **127 lines** (-2 lines drift)
- `MeasuredGeometricVerifierAdversarialCoverageTests.cs`: claimed 225 lines vs actual **256 lines** (+31 lines drift)
- Total reported: 788 lines vs actual **842 lines** (+54 lines drift)

While all 4 files remain safely within Rule C2 ($\le 300$ physical lines), reporting inaccurate metrics undermines the empirical rigor required by the Handoff Protocol.

---

## 3. Authoritative Master Census of Tier 5 Deliverables

### Table 1: Tier 5 Test Files Physical Line Count & Clean Code Compliance Summary

| # | File Path (Relative to Repo Root) | Lines | C2 (<=300) | Members | Max Method Name | Max Span | M3 (<=50) | Overall Status |
|---|---|:---:|:---:|:---:|---|:---:|:---:|:---:|
| 1 | `HPBim\tests\HPBim.Core.Tests\Adversarial\BimModelSchemaValidatorAdversarialCoverageTests.cs` | 272 | PASS | 10 | `Validate_RebarHostResolution_UnderscorePrefixAndBrokenRef` | 37L (L211-L247) | PASS | COMPLIANT |
| 2 | `HPBim\tests\HPBim.Core.Tests\Adversarial\IdempotentElementResolverAdversarialCoverageTests.cs` | 187 | PASS | 10 | `Resolve_MultiHandleProvenanceCollisions_ResolvesConsistently` | 35L (L73-L107) | PASS | COMPLIANT |
| 3 | `HPBim\tests\HPBim.Core.Tests\Adversarial\CompositeSourceKeyAdversarialCoverageTests.cs` | 127 | PASS | 8 | `Stress_MassiveKeyLookupAndCollisionResistance` | 31L (L82-L112) | PASS | COMPLIANT |
| 4 | `HPBim\tests\HPBim.Core.Tests\Adversarial\MeasuredGeometricVerifierAdversarialCoverageTests.cs` | 256 | PASS | 8 | `AuditColumn_DecimalRoundingNearToleranceBoundaries_StrictZeroMm` | 37L (L21-L57) | PASS | COMPLIANT |
| 5 | `HPBim\tests\HPBim.Client.Tests\Connection\IntentJournalTier5AdversarialTests.cs` | 181 | PASS | 9 | `ConcurrentOperations_HighLoad_AppendAndReadSimultaneously_PreservesAllOperations` | 32L (L65-L96) | PASS | COMPLIANT |
| 6 | `HPBim\tests\HPBim.Client.Tests\Connection\TimeoutPolicyTier5AdversarialTests.cs` | 130 | PASS | 11 | `ComputeExecutionBudget_ClampsStrictlyToMaxExecutionBudget` | 19L (L80-L98) | PASS | COMPLIANT |
| 7 | `HPBim\tests\HPBim.Client.Tests\Recovery\ReceiptReaderTier5AdversarialTests.cs` | 192 | PASS | 10 | `ParseReceipts_ArrayContainingNonObjects_GracefullySkipsNonObjects` | 29L (L43-L71) | PASS | COMPLIANT |
| 8 | `HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileServiceTier5AdversarialTests.cs` | 186 | PASS | 8 | `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately` | 59L (L38-L96) | **FAIL (59L)** | **NON-COMPLIANT** |
| 9 | `HPBim\tests\HPBim.Client.Tests\Pipeline\BimPlanRunnerScriptTemplateTier5AdversarialTests.cs` | 103 | PASS | 6 | `ScriptTemplate_ContainsAllMandatoryCategories` | 26L (L38-L63) | PASS | COMPLIANT |
| 10 | `HPBim\tests\HPBim.Client.Tests\Pipeline\BimPipelineEngineTier5AdversarialTests.cs` | 239 | PASS | 10 | `ExecutePlanAsync_ElementFailure_IdentifiesFailedElementsAndSetsOverallFailure` | 38L (L174-L211) | PASS | COMPLIANT |
| **Total** | **10 Test Files** | **1,873** | **10/10 PASS** | **90** | — | — | **89/90 PASS (1 FAIL)** | **1 Non-Compliant** |

---

### Table 2: Discrepancy Reconciliation Matrix

| Scope | Artifact | Claimed Attestation | Empirical Ground Truth | Root Cause | Severity |
|---|---|---|---|---|---|
| **HPBim.Client.Tests** | `challenger_tier5_2/report.md`<br>(Sec. 5, Item 2) & `handoff.md` (Sec. 4) | *"All test methods strictly <= 50 lines."* | Method `ReconcileAllUnknownIntents...`<br>spans **59 physical lines** (L38–L96). | Omission of automated AST measurement prior to publishing handoff. | **BLOCKING (Major)**<br>Violates Rule M3 and Golden Rule 0. Triggered Auditor Integrity Violation. |
| **HPBim.Core.Tests** | `challenger_tier5_1/report.md`<br>(Sec. 4) & `handoff.md` (Sec. 5) | `dotnet test ... --filter "FullyQualifiedName~Adversarial"` | Crashes with **exit code 5** (`Zero tests ran`) under MTP runner. | Obsolete VSTest syntax incompatible with `Microsoft.Testing.Platform`. | **OPERATIONAL (Minor)**<br>Breaks test repeatability in CLI. |
| **HPBim.Core.Tests** | `challenger_tier5_1/report.md`<br>(Sec. 3 Table) | Claimed line counts: 245, 189, 129, 225 (Total: 788 lines). | Actual physical lines: **272, 187, 127, 256** (Total: **842 lines**). | Relying on unverified intermediate/draft metrics instead of measuring disk files. | **INFORMATIONAL**<br>All files remain $\le 300$ lines (Rule C2 PASS). |

---

### Table 3: Exhaustive 90-Member Census Across All 10 Test Files

| # | File | Member Name | Type | Start | End | Span | Rule M3 | Status |
|---|---|---|---|:---:|:---:|:---:|:---:|:---:|
| 1 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_RebarHostResolution_UnderscorePrefixAndBrokenRef` | Method | L211 | L247 | 37L | PASS | OK |
| 2 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_WallOpening_BeyondWallBoundary_Rejected` | Method | L116 | L148 | 33L | PASS | OK |
| 3 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_UnicodeAndVietnameseDiacriticLevelNames_ResolvesAndPasses` | Method | L12 | L40 | 29L | PASS | OK |
| 4 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_GridLengthNear1mmBoundary_StrictBoundaryEnforcement` | Method | L87 | L114 | 28L | PASS | OK |
| 5 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_WallOpening_NegativeOffset_Rejected` | Method | L150 | L176 | 27L | PASS | OK |
| 6 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_EmojiAndSpecialCharactersInLevelNames_PassesIfMatching` | Method | L42 | L66 | 25L | PASS | OK |
| 7 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_PipeRunDef_DegeneratePointsList_Rejected` | Method | L249 | L270 | 22L | PASS | OK |
| 8 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_ExtremeCoordinates_NaNInGrids_Rejected` | Method | L68 | L85 | 18L | PASS | OK |
| 9 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_CorruptedJsonNodes_NonArrayLevels_Rejected` | Method | L178 | L193 | 16L | PASS | OK |
| 10 | `BimModelSchemaValidatorAdversarialCoverageTests.cs` | `Validate_CorruptedJsonNodes_TypeMismatchInElevation_Rejected` | Method | L195 | L209 | 15L | PASS | OK |
| 11 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `Resolve_MultiHandleProvenanceCollisions_ResolvesConsistently` | Method | L73 | L107 | 35L | PASS | OK |
| 12 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `Resolve_FullRecreateToNoOpLifecycle_EndToEndSimulation` | Method | L154 | L185 | 32L | PASS | OK |
| 13 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `Resolve_CaseInsensitiveTypeNameAndLevel_ResolvesToNoOpWithoutUpdate` | Method | L109 | L131 | 23L | PASS | OK |
| 14 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `Resolve_MissingResourceOrTypeName_GracefulNoOp` | Method | L133 | L152 | 20L | PASS | OK |
| 15 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `Resolve_TombstonedEmptyTargetUniqueId_ResolvesToCreate` | Method | L24 | L41 | 18L | PASS | OK |
| 16 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `Resolve_ElementMissingInHostDocument_ResolvesToRecreate` | Method | L43 | L57 | 15L | PASS | OK |
| 17 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `Resolve_QueryReturnsNull_ResolvesToRecreate` | Method | L59 | L71 | 13L | PASS | OK |
| 18 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `Register` | Method | L15 | L16 | 2L | PASS | OK |
| 19 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `QueryElement` | Method | L20 | L21 | 2L | PASS | OK |
| 20 | `IdempotentElementResolverAdversarialCoverageTests.cs` | `Remove` | Method | L18 | L18 | 1L | PASS | OK |
| 21 | `CompositeSourceKeyAdversarialCoverageTests.cs` | `Stress_MassiveKeyLookupAndCollisionResistance` | Method | L82 | L112 | 31L | PASS | OK |
| 22 | `CompositeSourceKeyAdversarialCoverageTests.cs` | `CompareTo_ConsistentWithEqualsContract` | Method | L67 | L80 | 14L | PASS | OK |
| 23 | `CompositeSourceKeyAdversarialCoverageTests.cs` | `Parse_NumericCategory_ParsesIfDefined_OrRejectsIfNotDefined` | Method | L43 | L55 | 13L | PASS | OK |
| 24 | `CompositeSourceKeyAdversarialCoverageTests.cs` | `Equals_And_HashCode_EmptyKeyContract` | Method | L114 | L125 | 12L | PASS | OK |
| 25 | `CompositeSourceKeyAdversarialCoverageTests.cs` | `Parse_FilenameWithEmbeddedHash_ParsesCorrectlyInBracketedFormat` | Method | L10 | L19 | 10L | PASS | OK |
| 26 | `CompositeSourceKeyAdversarialCoverageTests.cs` | `Parse_MultipleColonsInHandle_PreservesAllColons` | Method | L21 | L30 | 10L | PASS | OK |
| 27 | `CompositeSourceKeyAdversarialCoverageTests.cs` | `Parse_ExoticWhitespaceAndTabsAroundDelimiters_TrimsProperly` | Method | L32 | L41 | 10L | PASS | OK |
| 28 | `CompositeSourceKeyAdversarialCoverageTests.cs` | `Parse_MalformedDelimiters_ThrowsFormatException` | Method | L57 | L65 | 9L | PASS | OK |
| 29 | `MeasuredGeometricVerifierAdversarialCoverageTests.cs` | `AuditColumn_DecimalRoundingNearToleranceBoundaries_StrictZeroMm` | Method | L21 | L57 | 37L | PASS | OK |
| 30 | `MeasuredGeometricVerifierAdversarialCoverageTests.cs` | `AuditLinearStructure_VN2000_ExtremeCoordinatesScaleAndOffsets` | Method | L117 | L152 | 36L | PASS | OK |
| 31 | `MeasuredGeometricVerifierAdversarialCoverageTests.cs` | `AuditBeam_ElevationBoundary_Strict1mmThreshold` | Method | L59 | L89 | 31L | PASS | OK |
| 32 | `MeasuredGeometricVerifierAdversarialCoverageTests.cs` | `AuditPlan_ListOverload_DictionaryMappingByUniqueIdAndKey` | Method | L229 | L254 | 26L | PASS | OK |
| 33 | `MeasuredGeometricVerifierAdversarialCoverageTests.cs` | `AuditPile_CenterBoundary_Strict2mmThreshold` | Method | L91 | L115 | 25L | PASS | OK |
| 34 | `MeasuredGeometricVerifierAdversarialCoverageTests.cs` | `AuditLevel_ScaleConversionFromMetersToMillimeters` | Method | L178 | L202 | 25L | PASS | OK |
| 35 | `MeasuredGeometricVerifierAdversarialCoverageTests.cs` | `AuditItem_UnmeasurableCurve_Rule0_Inconclusive_NeverPass` | Method | L204 | L227 | 24L | PASS | OK |
| 36 | `MeasuredGeometricVerifierAdversarialCoverageTests.cs` | `AuditWall_EndpointOffsetExceedsTolerance_FlaggedAsDefect` | Method | L154 | L176 | 23L | PASS | OK |
| 37 | `IntentJournalTier5AdversarialTests.cs` | `ConcurrentOperations_HighLoad_AppendAndReadSimultaneously_PreservesAllOperations` | Method | L65 | L96 | 32L | PASS | OK |
| 38 | `IntentJournalTier5AdversarialTests.cs` | `RapidStatusMutations_SameOperation_PersistsAndReadsLatestStateInCacheAndColdRestart` | Method | L36 | L63 | 28L | PASS | OK |
| 39 | `IntentJournalTier5AdversarialTests.cs` | `FileCorruption_BlankLinesAndMalformedRecords_GracefullyExtractsValidRecords` | Method | L98 | L121 | 24L | PASS | OK |
| 40 | `IntentJournalTier5AdversarialTests.cs` | `DisposedState_CallingAnyMethod_ThrowsObjectDisposedException` | Method | L123 | L142 | 20L | PASS | OK |
| 41 | `IntentJournalTier5AdversarialTests.cs` | `UpdateStatus_ForUnrecordedOperation_CreatesNewRecordAndAppendsToDisk` | Method | L154 | L171 | 18L | PASS | OK |
| 42 | `IntentJournalTier5AdversarialTests.cs` | `Dispose` | Method | L24 | L34 | 11L | PASS | OK |
| 43 | `IntentJournalTier5AdversarialTests.cs` | `PruneOldEntries_WhenFileDoesNotExist_ReturnsZero` | Method | L144 | L152 | 9L | PASS | OK |
| 44 | `IntentJournalTier5AdversarialTests.cs` | `RecordIntent_NullArgument_ThrowsArgumentNullException` | Method | L173 | L179 | 7L | PASS | OK |
| 45 | `IntentJournalTier5AdversarialTests.cs` | `IntentJournalTier5AdversarialTests` | Constructor | L17 | L22 | 6L | PASS | OK |
| 46 | `TimeoutPolicyTier5AdversarialTests.cs` | `ComputeExecutionBudget_ClampsStrictlyToMaxExecutionBudget` | Method | L80 | L98 | 19L | PASS | OK |
| 47 | `TimeoutPolicyTier5AdversarialTests.cs` | `ComputeTotalTimeout_SumOfQueueDeadlineAndExecutionBudget` | Method | L100 | L112 | 13L | PASS | OK |
| 48 | `TimeoutPolicyTier5AdversarialTests.cs` | `ComputeExecutionBudget_ZeroOrNegativeItems_ReturnsDefaultExecutionBudget` | Method | L59 | L69 | 11L | PASS | OK |
| 49 | `TimeoutPolicyTier5AdversarialTests.cs` | `Validate_MaxLessThanBase_ThrowsArgumentOutOfRangeException` | Method | L37 | L46 | 10L | PASS | OK |
| 50 | `TimeoutPolicyTier5AdversarialTests.cs` | `Validate_ExactEquality_BaseAndMaxBudget_ValidatesSuccessfully` | Method | L48 | L57 | 10L | PASS | OK |
| 51 | `TimeoutPolicyTier5AdversarialTests.cs` | `Validate_NonPositiveTransportTimeout_ThrowsArgumentOutOfRangeException` | Method | L9 | L17 | 9L | PASS | OK |
| 52 | `TimeoutPolicyTier5AdversarialTests.cs` | `Validate_NonPositiveQueueDeadline_ThrowsArgumentOutOfRangeException` | Method | L19 | L26 | 8L | PASS | OK |
| 53 | `TimeoutPolicyTier5AdversarialTests.cs` | `Validate_NonPositiveBaseExecutionBudget_ThrowsArgumentOutOfRangeException` | Method | L28 | L35 | 8L | PASS | OK |
| 54 | `TimeoutPolicyTier5AdversarialTests.cs` | `ComputeExecutionBudget_OneItem_ReturnsBasePlusOneIncrement` | Method | L71 | L78 | 8L | PASS | OK |
| 55 | `TimeoutPolicyTier5AdversarialTests.cs` | `ComputeExecutionBudget_HugeCountIntMax_CalculatesSafelyWithoutOverflow` | Method | L114 | L120 | 7L | PASS | OK |
| 56 | `TimeoutPolicyTier5AdversarialTests.cs` | `CustomPerItemBudget_ZeroPerItem_YieldsConstantBaseBudget` | Method | L122 | L128 | 7L | PASS | OK |
| 57 | `ReceiptReaderTier5AdversarialTests.cs` | `ParseReceipts_ArrayContainingNonObjects_GracefullySkipsNonObjects` | Method | L43 | L71 | 29L | PASS | OK |
| 58 | `ReceiptReaderTier5AdversarialTests.cs` | `FindReceiptAsync_PrioritizesOperationIdOverPlanHash` | Method | L123 | L149 | 27L | PASS | OK |
| 59 | `ReceiptReaderTier5AdversarialTests.cs` | `ParseReceipts_MissingOrCorruptedFields_DefaultsGracefully` | Method | L73 | L95 | 23L | PASS | OK |
| 60 | `ReceiptReaderTier5AdversarialTests.cs` | `FindReceiptAsync_FallsBackToPlanHash_WhenOperationIdMissing` | Method | L151 | L170 | 20L | PASS | OK |
| 61 | `ReceiptReaderTier5AdversarialTests.cs` | `ParseReceipts_ReceiptsArrayMissingOrNonArray_ReturnsEmptyList` | Method | L27 | L41 | 15L | PASS | OK |
| 62 | `ReceiptReaderTier5AdversarialTests.cs` | `ParseReceipts_NullOrNonObjectRoot_ReturnsEmptyList` | Method | L12 | L25 | 14L | PASS | OK |
| 63 | `ReceiptReaderTier5AdversarialTests.cs` | `ParseElementMap_EdgeCasesAndInvalidInputs_ReturnsEmptyDictionary` | Method | L97 | L110 | 14L | PASS | OK |
| 64 | `ReceiptReaderTier5AdversarialTests.cs` | `ParseElementMap_ValidKeyValues_ParsesAllEntries` | Method | L112 | L121 | 10L | PASS | OK |
| 65 | `ReceiptReaderTier5AdversarialTests.cs` | `FindReceiptAsync_WhenNeitherMatches_ReturnsNull` | Method | L172 | L180 | 9L | PASS | OK |
| 66 | `ReceiptReaderTier5AdversarialTests.cs` | `AutoConnect_WhenPipeClientDisconnected_ConnectsBeforeQuery` | Method | L182 | L190 | 9L | PASS | OK |
| 67 | `ReconcileServiceTier5AdversarialTests.cs` | `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately` | Method | L38 | L96 | 59L | **FAIL (>50)** | **REJECT** |
| 68 | `ReconcileServiceTier5AdversarialTests.cs` | `ReconcileIntent_NetworkSeveredDuringQuery_ReturnsStillUnknownAndPreservesIntentState` | Method | L128 | L147 | 20L | PASS | OK |
| 69 | `ReconcileServiceTier5AdversarialTests.cs` | `ReconcileIntent_PipeTimeoutDuringQuery_ReturnsStillUnknownAndPreservesIntentState` | Method | L149 | L168 | 20L | PASS | OK |
| 70 | `ReconcileServiceTier5AdversarialTests.cs` | `ReconcileIntent_MissingInJournal_ReturnsStillUnknownWithExplanation` | Method | L112 | L126 | 15L | PASS | OK |
| 71 | `ReconcileServiceTier5AdversarialTests.cs` | `AutoConnect_WhenPipeDisconnected_ConnectsAutomatically` | Method | L170 | L184 | 15L | PASS | OK |
| 72 | `ReconcileServiceTier5AdversarialTests.cs` | `ReconcileAllUnknownIntents_WhenNoIncompleteIntents_ReturnsNoOp` | Method | L98 | L110 | 13L | PASS | OK |
| 73 | `ReconcileServiceTier5AdversarialTests.cs` | `Dispose` | Method | L26 | L36 | 11L | PASS | OK |
| 74 | `ReconcileServiceTier5AdversarialTests.cs` | `ReconcileServiceTier5AdversarialTests` | Constructor | L19 | L24 | 6L | PASS | OK |
| 75 | `BimPlanRunnerScriptTemplateTier5AdversarialTests.cs` | `ScriptTemplate_ContainsAllMandatoryCategories` | Method | L38 | L63 | 26L | PASS | OK |
| 76 | `BimPlanRunnerScriptTemplateTier5AdversarialTests.cs` | `ScriptTemplate_Epilogue_ReturnsExpectedSummaryStructure` | Method | L88 | L101 | 14L | PASS | OK |
| 77 | `BimPlanRunnerScriptTemplateTier5AdversarialTests.cs` | `ScriptTemplate_ContainsExtensibleStorageReceiptSchemaDefinition` | Method | L74 | L86 | 13L | PASS | OK |
| 78 | `BimPlanRunnerScriptTemplateTier5AdversarialTests.cs` | `GetScriptCode_ParsesAsValidCSharpScript_WithZeroSyntaxErrors` | Method | L12 | L23 | 12L | PASS | OK |
| 79 | `BimPlanRunnerScriptTemplateTier5AdversarialTests.cs` | `Preamble_ContainsMandatoryCoordinateAndPreflightHelpers` | Method | L25 | L36 | 12L | PASS | OK |
| 80 | `BimPlanRunnerScriptTemplateTier5AdversarialTests.cs` | `ScriptTemplate_ContainsUnsupportedCategoryDefensiveBranch` | Method | L65 | L72 | 8L | PASS | OK |
| 81 | `BimPipelineEngineTier5AdversarialTests.cs` | `ExecutePlanAsync_ElementFailure_IdentifiesFailedElementsAndSetsOverallFailure` | Method | L174 | L211 | 38L | PASS | OK |
| 82 | `BimPipelineEngineTier5AdversarialTests.cs` | `ExecutePlanJournaled_ApplyExecution_UnexpectedRollback_FailsWithExplicitMessage` | Method | L99 | L124 | 26L | PASS | OK |
| 83 | `BimPipelineEngineTier5AdversarialTests.cs` | `ExecutePlanJournaled_CalculatesTimeoutFromPolicy_WhenPlanTimeoutZero` | Method | L213 | L237 | 25L | PASS | OK |
| 84 | `BimPipelineEngineTier5AdversarialTests.cs` | `ExecutePlanAsync_RevitWarnings_PropagatesToResultAndStageResults` | Method | L149 | L172 | 24L | PASS | OK |
| 85 | `BimPipelineEngineTier5AdversarialTests.cs` | `ExecutePlanJournaled_DryRun_UnconfirmedRollback_FailsWithExplicitMessage` | Method | L126 | L147 | 22L | PASS | OK |
| 86 | `BimPipelineEngineTier5AdversarialTests.cs` | `CreatePlan` | Method | L41 | L59 | 19L | PASS | OK |
| 87 | `BimPipelineEngineTier5AdversarialTests.cs` | `ExecutePlanJournaled_PipeDisconnectDuringExecution_MarksOutcomeUnknownAndRecordsJournal` | Method | L61 | L78 | 18L | PASS | OK |
| 88 | `BimPipelineEngineTier5AdversarialTests.cs` | `ExecutePlanJournaled_PipeTimeoutDuringExecution_MarksOutcomeUnknownAndRecordsJournal` | Method | L80 | L97 | 18L | PASS | OK |
| 89 | `BimPipelineEngineTier5AdversarialTests.cs` | `Dispose` | Method | L29 | L39 | 11L | PASS | OK |
| 90 | `BimPipelineEngineTier5AdversarialTests.cs` | `BimPipelineEngineTier5AdversarialTests` | Constructor | L22 | L27 | 6L | PASS | OK |

---

## 4. Authoritative Blueprint for Authentic Attestation

### 4.1 Core Protocols of Authentic Attestation
To prevent false certifications, incomplete verification commands, or metric drift in future iterations:

1. **Rule 0 Verification Gate**:
   - Never attest compliance based on intention, recollection, or visual spot-checking.
   - Run an automated syntax auditor (e.g. `dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj`) before committing any handoff report.
   - Any method measuring $> 50$ physical lines must trigger an immediate self-rejection.
2. **Physical Line Accounting Discipline**:
   - File physical line counts must be read directly from the on-disk file (`(Get-Content <file>).Length` or `len(open(<file>).readlines())`).
   - Line span definition: Measured from the start of the outer-most attribute (e.g. `[Fact]`) through the method closing brace `}`. Both attribute span and declaration span must satisfy $\le 50$ lines.
3. **Runner-Aware Command Validation**:
   - Test execution commands documented in reports and handoffs must be verified live in PowerShell before delivery.
   - In .NET 10 with `Microsoft.Testing.Platform` (MTP), commands must strictly use MTP switches.

---

### 4.2 Standardized Microsoft.Testing.Platform (MTP) Command Catalog

The repository uses the unified `Microsoft.Testing.Platform` runner. All agents must use the following validated commands:

#### A. Full Solution Verification
```powershell
# Execute complete HPBim solution test suite (770 tests: 417 Core + 311 Client + 42 E2E)
dotnet test HPBim/HPBim.slnx

# Execute shared infrastructure test suite (908 tests: 793 Server.Core + 115 Bridge.Net48)
dotnet test McpShared/McpShared.slnx
```

#### B. Project-Level Verification
```powershell
# Core test suite (417 tests)
dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj

# Client test suite (311 tests)
dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj

# E2E test suite (42 tests)
dotnet test HPBim/tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj
```

#### C. Targeted Filtering under MTP (Crucial Syntax Rules)
```powershell
# 1. Namespace Filter: Run the 36 newly authored Tier 5 Core Adversarial tests
dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj --filter-namespace HPBim.Core.Tests.Adversarial

# 2. Class Filter with Wildcard: Run the 64 newly authored Tier 5 Client Adversarial tests
dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --filter-class *Tier5*

# 3. Specific Single Class Filter (Requires wildcard or full namespace qualification):
# Correct (Wildcard):
dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj --filter-class *CompositeSourceKeyAdversarialCoverageTests
# Correct (Fully Qualified):
dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj --filter-class HPBim.Core.Tests.Adversarial.CompositeSourceKeyAdversarialCoverageTests

# 4. Method Filter (Matches method name directly):
dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --filter-method *InterleavedStatuses*
```

#### D. Anti-Patterns to Strictly Avoid
| Anti-Pattern Command | Reason for Failure |
|---|---|
| `dotnet test ... --filter "FullyQualifiedName~..."` | VSTest syntax; MTP exits with **exit code 5** (`Zero tests ran`). |
| `dotnet test ... --filter-class CompositeSourceKey...` | Unqualified class name without `*` wildcard fails with **exit code 8**. |
| `dotnet test ... --nologo` | Forwarded to MTP runner as unknown argument; crashes with **exit code 5**. |

---

### 4.3 Actionable Remediation Implementation for Tier 5 Artifacts

To restore integrity and transition the milestone from `INTEGRITY VIOLATION` / `REQUEST_CHANGES` to `CLEAN` / `APPROVE`:

#### Step 1: Refactor Method in `ReconcileServiceTier5AdversarialTests.cs`
Extract intent/receipt fixture setup from `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately` (L38–L96, 59 lines) into a private helper:
```csharp
// Helper method (15 lines):
private (Guid opCommitted, Guid opRolledBack, Guid opMixedKeys) SeedInterleavedTestData(
    IntentJournal journal, FakeRevitPipeClient fakePipe)
{
    var opCommitted = Guid.NewGuid();
    var opRolledBack = Guid.NewGuid();
    var opMixedKeys = Guid.NewGuid();
    // seed journal intents & fake receipts ...
    return (opCommitted, opRolledBack, opMixedKeys);
}
```
This reduces the test method body from 59 lines down to $\le 38$ lines, achieving 100% compliance with Rule M3.

#### Step 2: Update `challenger_tier5_2/report.md` & `handoff.md`
Update Section 5 and Handoff Section 4 to present authentic AST line spans once Step 1 is completed.

#### Step 3: Update `challenger_tier5_1/report.md` & `handoff.md`
Replace `--filter "FullyQualifiedName~Adversarial"` with `--filter-namespace HPBim.Core.Tests.Adversarial` in Section 4 and Handoff Section 5, and update Table 3 with the accurate physical line counts (272, 187, 127, 256).

---

## 5. Independent Verification Method

1. **Verify MTP Test Execution**:
   ```powershell
   # Adversarial Core (36 tests):
   dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj --filter-namespace HPBim.Core.Tests.Adversarial
   # Adversarial Client (64 tests):
   dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --filter-class *Tier5*
   ```
2. **Verify Roslyn AST Syntax Measurements**:
   ```powershell
   dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj
   ```
   *Expected Current Output*: Detects exactly 1 violation: `ReconcileServiceTier5AdversarialTests.cs` (59 lines).
3. **Invalidation Condition**:
   Once the helper extraction in Step 1 is applied, running the command above reports `OVERALL AUDIT VERDICT: CLEAN / 100% PASS (0 violations)`.
