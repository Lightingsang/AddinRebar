# Dispatch Instructions: auditor_m3_it2_gen2

## Identity
- Role: Forensic Integrity Auditor
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m3_it2_gen2
- Target Milestone: Milestone 3 Iteration 2 (Remediation Forensic Audit)

## Forensic Integrity Tasks
1. Authenticity of Remediated Code:
   - Verify that `CompositeBimGenerator` genuinely includes Stage 7 (Plumbing Fixtures + Pipes) and Stage 8 (Rebars) in `GetScriptCode()`.
   - Confirm that there are NO facades, dummy returns, or truncated scripts.
   - Verify that `PipeMessageReader` genuinely reads lines and dispatches JSON-RPC envelopes.
2. Clean Code Rules Compliance (HP_CLEAN_CODE_CORE.md):
   - Measure lines of every `.cs` file in `HPBim/src/HPBim.Client/`: verify all files <= 200 lines (strictly <= 300 ceiling).
   - Measure lines of `CompareCounts`: verify <= 15 lines (strictly <= 50 ceiling).
   - Verify 0 dead code (check `TypeResolutionRules.cs` methods are called).
   - Verify 0 empty catch blocks without explanatory comments.
   - Verify 0 commented-out code (Q-B1).
3. Dependency Boundaries:
   - Verify 0 references to `Autodesk.Revit.*` in `HPBim.Client.csproj`.
   - Verify 0 cross-MCP dependencies.
4. Test Verification:
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release` (verify 216/216 green tests).
5. Deliverable:
   - Write `handoff.md` with binary verdict: `CLEAN` or `INTEGRITY VIOLATION`.
   - Send completion message to orchestrator via `send_message`.

## 2026-10-06T02:54:39Z
[Message] timestamp=2026-10-06T02:54:39Z sender=18c3ad8c-1fc9-42cc-83e7-40ab9000587e priority=MESSAGE_PRIORITY_HIGH content=You are auditor_m3_it2_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m3_it2_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m3_it2_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read worker handoff at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_fix_gen2\handoff.md

Perform full forensic integrity audit on Milestone 3 Remediation:
1. Verify genuine CompositeBimGenerator implementing all 8 stages in C# script, zero facades.
2. Verify all files in HPBim.Client <= 200 lines, CompareCounts <= 15 lines.
3. Verify zero dead code, zero unannotated catches, zero commented-out code.
4. Verify zero references to Autodesk.Revit.* and zero cross-MCP dependencies.
5. Verify dotnet test passes with 216/216 tests.
Write handoff.md with binary verdict CLEAN or INTEGRITY VIOLATION and report back via send_message.
