# Dispatch Instructions: reviewer_m3_it2_2_gen2

## Identity
- Role: Adversarial Reviewer & Defect Verification Specialist
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_it2_2_gen2
- Target Milestone: Milestone 3 Iteration 2 (Remediation Review)

## Target Verification: Reviewer 2 Previous Findings
Adversarially verify that all 6 findings from the previous review (`reviewer_m3_2_gen2/handoff.md`) are genuinely and completely resolved:
1. **`CompositeBimGenerator` script completeness**:
   - Inspect `CompositeBimGenerator.Stages7To8.cs` lines: Are Stage 7 (Plumbing Fixtures + Pipes) and Stage 8 (Rebars) genuinely included in `GetScriptCode()`?
   - Does `GetCompositeReturnScript()` return `fixtures`, `pipes`, `rebars`?
   - Are there any remaining truncated facades?
2. **`RevitPipeClient.cs` line count**:
   - Measure actual line count via `ReadAllLines`: Is it <= 220 lines?
3. **`CompareCounts` method length**:
   - Count lines of `CompareCounts` in `RevitModelVerifier.cs`: Is it <= 15 lines?
4. **Dead code**:
   - Verify `GetDoorSymbolName` and `GetWindowSymbolName` have active usages in `Stage5OpeningGenerator.cs`.
5. **Concurrency synchronization**:
   - Verify `RevitPipeClient.ConnectAsync` and `DisconnectAsync` are guarded by `_connectLock` (`SemaphoreSlim(1, 1)`).
6. **Build & Tests**:
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release` (216 tests).
7. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.

## 2026-10-06T02:54:39Z
You are reviewer_m3_it2_2_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_it2_2_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_it2_2_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read previous review at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_2_gen2\handoff.md
Read worker handoff at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_fix_gen2\handoff.md

Adversarially verify all 6 findings from previous review:
1. CompositeBimGenerator script completeness (Stages 7 & 8 included, return object)
2. RevitPipeClient.cs line count <= 220 lines
3. CompareCounts method length <= 15 lines
4. Dead code in TypeResolutionRules eliminated
5. ConnectAsync has _connectLock synchronization
6. Run tests: dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release
Write handoff.md with verdict APPROVE or REQUEST_CHANGES and report back via send_message.
