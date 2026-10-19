## 2026-10-09T15:44:46Z
You are Worker W3 M2 for HPBim Wave 3 Remediation Milestone 2: Reconcile Persist-Before-Terminal & Exact Keyset Coverage (R2).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_m2_1
Your parent conversation ID is: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-09T14:54:56Z).
2. Read Explorer 1 findings:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_1\survey_report.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_1\handoff.md
3. Read PROJECT.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Write Ownership:
You EXCLUSIVELY own:
- HPBim/src/HPBim.Client/Recovery/ReconcileService.cs
- Any companion partial class or helper in HPBim/src/HPBim.Client/Recovery/
- HPBim/src/HPBim.Core/Storage/IntentJournalRecord.cs (if adding SourceKeys list)
- HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTests.cs (or new test file ReconcilePersistBeforeTerminalTests.cs)
Do NOT modify any other files outside this scope!

Task Requirements:
1. Upgrade ReconcileService.ApplyCommittedReceiptAsync:
   - Persist BimCoordinationLedger to disk atomically (.tmp -> replace via _ledgerService.SaveToFileAsync) BEFORE updating IntentJournal status to Committed.
   - Exact Keyset Coverage: Verify complete keyset coverage against the original intent (check every SourceKey in the intent, not just scalar ItemCount comparison).
   - If ledger persistence fails, mapping is missing/corrupted, or keyset coverage is incomplete: strictly preserve ReconcileOutcome.Unknown to allow subsequent safe reconciliation.
2. Acceptance Criteria & Tests:
   - Create tests simulating crash or disk write failure during ledger save in reconcile; verify journal is NOT marked Committed when ledger save fails.
   - Create tests verifying exact SourceKey keyset coverage: missing a single SourceKey results in OutcomeUnknown.
   - Verify that successful reconcile saves ledger first, then marks journal Committed.
3. Verification:
   - Run tests: dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   - Run full HPBim test suite: dotnet test HPBim/HPBim.slnx
   - Ensure 100% PASS with 0 regressions.
4. Clean Code Governance:
   - File <= 300 lines (Rule C2).
   - Method <= 50 lines (Rule M3).
   - 0 code comments (Rule Q-B1).
   - 0 empty catch blocks (Rule Q-B2).
   - Pure netstandard2.0 in HPBim.Client and HPBim.Core.

Deliverables:
- Write report.md and handoff.md in your working directory.
- Send completion message to parent (8b5286ec-5528-473c-b9fe-c3867bdf1bf8).
