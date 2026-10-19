# Dispatch Instructions: worker_m3_fix_gen2

## Identity
- Role: Implementation Worker for Milestone 3 Remediation
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_fix_gen2
- Target Milestone: Milestone 3 Iteration 2

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor and adversarial reviewers will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Authoritative Blueprint
Read the full implementation blueprint and drop-in code prepared by explorer_m3_fix_gen2 at:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_fix_gen2\handoff.md`

## Required Actions
1. **Create `PipeMessageReader.cs`** in `HPBim/src/HPBim.Client/Connection/PipeMessageReader.cs` (~105 lines).
2. **Refactor `RevitPipeClient.cs`** in `HPBim/src/HPBim.Client/Connection/RevitPipeClient.cs`:
   - Delegate stream reading to `PipeMessageReader`.
   - Add `SemaphoreSlim _connectLock = new(1, 1)` guarding `ConnectAsync` and `DisconnectAsync`.
   - Ensure `RevitPipeClient.cs` is strictly <= 200 lines (well under 250 limit).
3. **Refactor `CompareCounts`** in `HPBim/src/HPBim.Client/Verification/RevitModelVerifier.cs`:
   - Introduce `ElementCounts` record struct.
   - Decompose into 4 focused helper methods (`GetExpectedCounts`, `GetActualCounts`, `CheckAllDiscrepancies`, `CreateReport`).
   - Ensure `CompareCounts` is 7 lines (<= 35 limit) and whole file <= 130 lines.
4. **Integrate `TypeResolutionRules` into `Stage5OpeningGenerator.cs`**:
   - Use `TypeResolutionRules.GetDoorSymbolName` and `TypeResolutionRules.GetWindowSymbolName` to populate `symbolName`.
   - In script, resolve symbol matching `symbolName` before falling back to first.
   - Eliminates dead code!
5. **Partition `CompositeBimGenerator` into 4 Partial Classes**:
   - `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.cs` (~67 lines)
   - `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.Stages1To3.cs` (~115 lines)
   - `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.Stages4To6.cs` (~105 lines)
   - `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.Stages7To8.cs` (~120 lines)
   - Fully implements Stages 1 through 8 in script! Every file is <= 125 lines.
6. **Extend Tests in `HPBim/tests/HPBim.Client.Tests/`**:
   - Create `TypeResolver/TypeResolutionRulesTests.cs` (~45 lines).
   - Add `CompositeBimGenerator_IncludesAll8StagesInScriptAndArgs` to `ScriptGenerators/StageGeneratorTests.cs`.
   - Add `ConnectAsync_ConcurrentCalls_SynchronizesWithoutThrowingOrLeaking` to `Connection/RevitPipeClientAdversarialTests.cs`.
7. **Build and Test Verification**:
   - Run: `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release /warnaserror` (0 warnings, 0 errors).
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release` (All tests pass 100% green).
   - Verify line counts across all `.cs` files in `HPBim.Client`: every file <= 220 lines!
8. **Deliverable**:
   - Write `handoff.md` in your working directory.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T02:42:04Z
[Message] timestamp=2026-10-06T02:42:04Z sender=18c3ad8c-1fc9-42cc-83e7-40ab9000587e priority=MESSAGE_PRIORITY_HIGH content=You are worker_m3_fix_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_fix_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_fix_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read Explorer fix blueprint at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_fix_gen2\handoff.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor and adversarial reviewers will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Execute the 7 steps detailed in explorer_m3_fix_gen2/handoff.md:
1. Create PipeMessageReader.cs in HPBim/src/HPBim.Client/Connection/
2. Refactor RevitPipeClient.cs with _connectLock and delegate to reader (reduce to ~190 lines)
3. Refactor CompareCounts in RevitModelVerifier.cs using ElementCounts and helper methods (reduce to 7 lines)
4. Integrate TypeResolutionRules into Stage5OpeningGenerator.cs
5. Modularize CompositeBimGenerator into 4 partial class files (all 8 stages in script, all files <= 120 lines)
6. Add unit tests (TypeResolutionRulesTests, CompositeBimGenerator all 8 stages test, Concurrency test)
7. Run dotnet build with /warnaserror and dotnet test in Release mode. Verify all tests pass and all files <= 220 lines.
Write handoff.md and send message back to orchestrator.
