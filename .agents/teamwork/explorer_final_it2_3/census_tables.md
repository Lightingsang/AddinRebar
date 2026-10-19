### Table 1: Tier 5 Test Files Physical Line Count & Clean Code Compliance Summary

| # | File Path (Relative to Repo Root) | Lines | C2 (<=300) | Members | Max Method Name | Max Span | M3 (<=50) | Status |
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

---

### Table 2: Discrepancy Reconciliation — Reported Attestation vs. Empirical Ground Truth

| Deliverable / Scope | Artifact Checked | Claimed Attestation | Empirical Ground Truth | Discrepancy Root Cause | Impact & Severity |
|---|---|---|---|---|---|
| **HPBim.Client.Tests** | `challenger_tier5_2/report.md`<br>(Section 5, Item 2) &<br>`challenger_tier5_2/handoff.md`<br>(Section 4, Line 100) | *"Method Length (Rule M3): All test methods strictly <= 50 lines."* | Method `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately`<br>spans **59 lines** (L38-L96 with `[Fact]`) / **58 lines** (L39-L96 declaration to closing brace). | Omission of automated AST/metric measurement prior to certifying handoff. Self-certifying assumption without verification. | **BLOCKING (Major)**<br>Violates Rule M3 and Golden Rule 0 (Zero Speculation / Authenticity). Triggered Forensic Auditor Integrity Violation. |
| **HPBim.Core.Tests** | `challenger_tier5_1/report.md`<br>(Section 4, Line 100) &<br>`challenger_tier5_1/handoff.md`<br>(Section 5, Line 111) | Test command:<br>`dotnet test ... --filter "FullyQualifiedName~Adversarial"` | Command crashes with **exit code 5** (`Test run summary: Zero tests ran`, `error: 1`) under Microsoft.Testing.Platform. | Using obsolete VSTest `--filter` syntax instead of MTP-native `--filter-namespace HPBim.Core.Tests.Adversarial`. | **OPERATIONAL (Minor)**<br>Breaks automated test reproducibility and independent verification in MTP environment. |
| **HPBim.Core.Tests** | `challenger_tier5_1/report.md`<br>(Section 3, Table) | Line counts reported:<br>- Validator: 245 lines<br>- Resolver: 189 lines<br>- Key: 129 lines<br>- Geometric: 225 lines<br>Total: 788 lines | Exact empirical physical line counts:<br>- Validator: **272 lines** (+27)<br>- Resolver: **187 lines** (-2)<br>- Key: **127 lines** (-2)<br>- Geometric: **256 lines** (+31)<br>Total: **842 lines** (+54) | Reporting draft/intermediate file line counts instead of measuring final on-disk files. | **INFORMATIONAL**<br>All files remain <= 300 lines (Rule C2 PASS), but handoff metrics were factually inaccurate. |

---

### Table 3: Complete Member-by-Member Inventory Across All 10 Test Files (90 Members)

| # | File | Member Name | Type | Start Line | End Line | Line Span | Rule M3 (<=50) | Status |
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